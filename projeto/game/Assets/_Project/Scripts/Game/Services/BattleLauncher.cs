using System;
using System.Collections.Generic;
using ComboTactics.Core;

namespace ComboTactics.Game
{
    public enum GameMode { Campaign, VsAi, LocalPvp, Mission }

    /// <summary>O que as telas de menu e seleção de equipe juntam antes de uma batalha.</summary>
    public sealed class MatchRequest
    {
        public GameMode Mode;
        /// <summary>Equipe do jogador (ou do Jogador 1 no PvP); o primeiro começa na arena.</summary>
        public List<string> Team = new List<string>();
        /// <summary>Equipe do Jogador 2, só no PvP.</summary>
        public List<string> Team2 = new List<string>();
        /// <summary>Itens tirados do inventário para esta batalha (até 3). Ignorado no PvP, que usa o kit fixo.</summary>
        public Dictionary<string, int> Items = new Dictionary<string, int>();
        public int StageIndex;
        public string DifficultyId;
        /// <summary>Missão secundária, só no modo Mission.</summary>
        public string MissionId;
        /// <summary>Deixe null para uma seed aleatória; fixe um valor para reproduzir uma batalha.</summary>
        public int? Seed;
    }

    /// <summary>Uma batalha pronta para começar: configuração do motor e quem controla o lado 1.</summary>
    public sealed class Match
    {
        public GameMode Mode;
        public BattleSetup Setup;
        /// <summary>Perfil da IA do lado 1; null no PvP local.</summary>
        public AIProfile OpponentAi;
        public OpponentDef Opponent;
        public MissionDef Mission;
        public int StageIndex;
        public string[] SideNames;
    }

    public sealed class MatchResult
    {
        public GameMode Mode;
        /// <summary>0 ou 1; -1 em caso de empate.</summary>
        public int WinnerSide;
        public int CoinsEarned;
        public bool UnlockedNextStage;
        public bool CampaignCompleted;
        /// <summary>Lutadores que entraram no espectro com esta batalha (guardião vencido ou missão concluída).</summary>
        public List<string> NewFighters = new List<string>();
        /// <summary>Quem recuperou HP descansando fora da vitória na campanha.</summary>
        public List<string> RestReport = new List<string>();
        /// <summary>Missão: objetivos não cumpridos (vazio = cumprida).</summary>
        public List<string> FailedObjectives = new List<string>();
        public bool MissionCompleted;
        /// <summary>Missão concluída pela primeira vez (recompensa completa).</summary>
        public bool MissionFirstClear;
        public string ItemRewardId;
        public bool PlayerWon { get { return WinnerSide == 0; } }
    }

    /// <summary>
    /// Monta a batalha a partir do perfil e do conteúdo e, no fim, paga as moedas, consome os itens usados,
    /// atualiza a campanha, o espectro e as missões (SYSTEM-DESIGN, Fluxo A).
    /// </summary>
    public sealed class BattleLauncher
    {
        private readonly ContentDatabase _content;
        private readonly EconomyConfig _economy;
        private readonly BattleRules _rules;
        private readonly ProfileService _profile;
        private readonly CampaignService _campaign;
        private readonly JourneyService _journey;

        public BattleLauncher(ContentDatabase content, EconomyConfig economy, BattleRules rules,
            ProfileService profile, CampaignService campaign, JourneyService journey)
        {
            _content = content; _economy = economy; _rules = rules; _profile = profile; _campaign = campaign; _journey = journey;
        }

        /// <summary>Quantos lutadores o jogador leva nesta batalha (3, ou o que a missão pedir).</summary>
        public int RequiredTeamSize(MatchRequest request)
        {
            if (request.Mode == GameMode.Mission)
            {
                var mission = _content.Mission(request.MissionId);
                if (mission != null) return mission.PlayerTeamSize;
            }
            return _rules.MaxTeamSize;
        }

        /// <summary>Falso quando a missão proíbe itens. No PvP o kit é fixo, então também não há escolha.</summary>
        public bool ItemsAllowed(MatchRequest request)
        {
            if (request.Mode == GameMode.LocalPvp) return false;
            if (request.Mode != GameMode.Mission) return true;
            var mission = _content.Mission(request.MissionId);
            return mission == null || mission.AllowItems;
        }

        /// <summary>Verdadeiro quando o HP da jornada vale nesta batalha (só na campanha).</summary>
        public bool UsesJourneyHp(MatchRequest request) { return request.Mode == GameMode.Campaign; }

        public Match Create(MatchRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");
            int seed = request.Seed ?? Environment.TickCount;
            var setupRng = new Rng(seed ^ 0x5F3759DF);

            switch (request.Mode)
            {
                case GameMode.Campaign: return CreateCampaign(request, seed);
                case GameMode.VsAi: return CreateVsAi(request, seed, setupRng);
                case GameMode.Mission: return CreateMission(request, seed);
                default: return CreatePvp(request, seed, setupRng);
            }
        }

        private Match CreateCampaign(MatchRequest request, int seed)
        {
            if (!_journey.HasAffinity) throw new GameException("Escolha a afinidade do seu mago antes de começar a jornada.");
            if (!_campaign.IsUnlocked(request.StageIndex)) throw new GameException("Esta fase ainda está bloqueada. Vença a fase anterior.");
            var opponent = _content.Opponents[request.StageIndex];

            var enemyTeam = new List<FighterSpec>();
            foreach (var id in opponent.TeamIds)
                enemyTeam.Add(StatsResolver.ForOpponent(_content.Character(id), _content, opponent.StatBonus));

            var team = PlayerTeam(request.Team, true, _rules.MaxTeamSize);
            var hp = new List<int>();
            foreach (var fighter in team)
            {
                int current = _journey.CurrentHp(fighter.Id);
                if (current <= 0) throw new GameException(fighter.Name + " está exausto. Deixe-o descansar ou cure-o no Altar.");
                hp.Add(current);
            }

            return new Match
            {
                Mode = GameMode.Campaign,
                Opponent = opponent,
                StageIndex = request.StageIndex,
                OpponentAi = _content.AiProfiles[opponent.AiProfileId],
                SideNames = new[] { StoryScript.MageName, opponent.Name.Split(',')[0] },
                // R4: na campanha a batalha começa na arena do guardião.
                Setup = new BattleSetup(team, enemyTeam, PlayerItems(request.Items), opponent.Items,
                    opponent.HomeArena, seed, _rules, _content.Catalog, hp),
            };
        }

        private Match CreateVsAi(MatchRequest request, int seed, Rng rng)
        {
            AIProfile profile;
            if (request.DifficultyId == null || !_content.AiProfiles.TryGetValue(request.DifficultyId, out profile))
                throw new GameException("Escolha uma dificuldade.");

            // Equipe da IA: 3 personagens diferentes sorteados entre os 9, com atributos base (R11: a IA pode usar todos).
            var pool = new List<CharacterDef>(_content.PlayableCharacters);
            var enemyTeam = new List<FighterSpec>();
            while (enemyTeam.Count < _rules.MaxTeamSize && pool.Count > 0)
            {
                int pick = rng.Next(pool.Count);
                enemyTeam.Add(StatsResolver.ForOpponent(pool[pick], _content, 0));
                pool.RemoveAt(pick);
            }

            return new Match
            {
                Mode = GameMode.VsAi,
                OpponentAi = profile,
                SideNames = new[] { "Você", "IA " + profile.Name },
                Setup = new BattleSetup(PlayerTeam(request.Team, true, _rules.MaxTeamSize), enemyTeam, PlayerItems(request.Items), _content.VsAiOpponentKit,
                    ArenaSystem.Draw(rng), seed, _rules, _content.Catalog),
            };
        }

        private Match CreatePvp(MatchRequest request, int seed, Rng rng)
        {
            // R6: no Modo Justo valem os atributos base. O kit de itens é sempre o fixo, igual para os dois.
            bool useUpgrades = !_profile.FairPvp;
            return new Match
            {
                Mode = GameMode.LocalPvp,
                SideNames = new[] { "Jogador 1", "Jogador 2" },
                Setup = new BattleSetup(PlayerTeam(request.Team, useUpgrades, _rules.MaxTeamSize), PlayerTeam(request.Team2, useUpgrades, _rules.MaxTeamSize),
                    _content.FairKit, _content.FairKit, ArenaSystem.Draw(rng), seed, _rules, _content.Catalog),
            };
        }

        private Match CreateMission(MatchRequest request, int seed)
        {
            var mission = _content.Mission(request.MissionId);
            if (mission == null) throw new GameException("Missão desconhecida.");
            if (!_journey.HasAffinity || _profile.CampaignStage < mission.RequiredStagesCleared)
                throw new GameException("Esta missão ainda não apareceu no quadro.");

            var enemyTeam = new List<FighterSpec>();
            foreach (var id in mission.EnemyTeamIds)
                enemyTeam.Add(StatsResolver.ForOpponent(_content.Character(id), _content, mission.EnemyStatBonus));

            var rules = _rules;
            if (mission.FixedArena)
            {
                rules = CopyRules(_rules);
                rules.ArenaDrawInterval = int.MaxValue;
            }

            var recruit = _content.Character(mission.RecruitId);
            return new Match
            {
                Mode = GameMode.Mission,
                Mission = mission,
                OpponentAi = _content.AiProfiles[mission.AiProfileId],
                SideNames = new[] { StoryScript.MageName, recruit != null ? recruit.Name : mission.Title },
                Setup = new BattleSetup(PlayerTeam(request.Team, true, mission.PlayerTeamSize), enemyTeam,
                    mission.AllowItems ? PlayerItems(request.Items) : new Dictionary<string, int>(), new Dictionary<string, int>(),
                    mission.Arena, seed, rules, _content.Catalog),
            };
        }

        private List<FighterSpec> PlayerTeam(List<string> ids, bool useUpgrades, int size)
        {
            if (ids == null || ids.Count != size)
                throw new GameException(size == 1 ? "Escolha 1 lutador para o duelo." : "Escolha " + size + " lutadores para a equipe.");

            var spectrum = _journey.Spectrum();
            if (spectrum.Count == 0) throw new GameException("Comece o Modo História e escolha a afinidade do mago para formar o seu espectro.");

            var team = new List<FighterSpec>();
            var seen = new HashSet<string>();
            foreach (var id in ids)
            {
                var def = _content.Character(id);
                if (def == null || !def.Playable) throw new GameException("Personagem indisponível: " + id);
                if (!spectrum.Contains(id)) throw new GameException(def.Name + " ainda não está no seu espectro. " + _journey.UnlockHint(id) + ".");
                if (!seen.Add(id)) throw new GameException("A equipe não pode repetir personagens.");
                string charId = id;
                team.Add(StatsResolver.ForPlayer(def, _content, _economy,
                    useUpgrades ? (Func<Stat, int>)(stat => _profile.UpgradeLevel(charId, stat)) : null));
            }
            return team;
        }

        private Dictionary<string, int> PlayerItems(Dictionary<string, int> requested)
        {
            var items = new Dictionary<string, int>();
            int total = 0;
            if (requested != null)
            {
                foreach (var pair in requested)
                {
                    if (pair.Value <= 0) continue;
                    if (_content.Item(pair.Key) == null) throw new GameException("Item indisponível: " + pair.Key);
                    if (pair.Value > _profile.ItemCount(pair.Key)) throw new GameException("Você não tem itens suficientes no inventário.");
                    items[pair.Key] = pair.Value;
                    total += pair.Value;
                }
            }
            if (total > _rules.MaxItemsPerBattle) throw new GameException("Leve no máximo " + _rules.MaxItemsPerBattle + " itens por batalha.");
            return items;
        }

        private static BattleRules CopyRules(BattleRules source)
        {
            return new BattleRules
            {
                SingleAdvantageAttack = source.SingleAdvantageAttack,
                DoubleAdvantageAttack = source.DoubleAdvantageAttack,
                SingleAdvantageDefense = source.SingleAdvantageDefense,
                DoubleAdvantageDefense = source.DoubleAdvantageDefense,
                DefenseCutPerPoint = source.DefenseCutPerPoint,
                DefenseCap = source.DefenseCap,
                ArenaBonus = source.ArenaBonus,
                ArenaPenalty = source.ArenaPenalty,
                ArenaDrawInterval = source.ArenaDrawInterval,
                MoonSlowBelow = source.MoonSlowBelow,
                MoonFastFrom = source.MoonFastFrom,
                StaminaMax = source.StaminaMax,
                StaminaOnDealDamage = source.StaminaOnDealDamage,
                StaminaOnTakeDamage = source.StaminaOnTakeDamage,
                StaminaOnReaction = source.StaminaOnReaction,
                MaxTeamSize = source.MaxTeamSize,
                MaxItemsPerBattle = source.MaxItemsPerBattle,
                ReactionWindowSeconds = source.ReactionWindowSeconds,
            };
        }

        /// <summary>Fecha a batalha: recompensa, itens consumidos, campanha, espectro, missões e gravação do save.</summary>
        public MatchResult Finish(Match match, IBattleEngine engine)
        {
            if (!engine.IsOver) throw new InvalidOperationException("A batalha ainda não terminou.");

            var result = new MatchResult { Mode = match.Mode, WinnerSide = engine.WinnerSide };
            if (match.Mode == GameMode.LocalPvp)
            {
                result.CoinsEarned = _economy.PvpReward;
                _profile.AddCoins(result.CoinsEarned);
                _profile.Save();
                return result;
            }

            // R8: os itens usados saem do inventário; os que sobraram voltam.
            foreach (var itemId in engine.State.Sides[0].UsedItems) _profile.RemoveItem(itemId, 1);

            var spectrumBefore = _journey.Spectrum();
            bool won = engine.WinnerSide == 0;
            if (match.Mode == GameMode.Campaign)
            {
                if (won)
                {
                    result.CoinsEarned = match.Opponent.Reward;
                    result.UnlockedNextStage = _profile.CompleteStage(match.StageIndex);
                    result.CampaignCompleted = result.UnlockedNextStage && _campaign.IsCompleted;
                    result.RestReport = _journey.RecordCampaignVictory(engine.State.Sides[0].Team, spectrumBefore);
                }
                else
                {
                    // Na derrota o HP da jornada não muda: a fase recomeça como estava antes.
                    result.CoinsEarned = _economy.DefeatReward;
                }
            }
            else if (match.Mode == GameMode.Mission)
            {
                FinishMission(match.Mission, engine, result);
            }
            else
            {
                result.CoinsEarned = won ? _economy.VictoryReward : _economy.DefeatReward;
            }

            foreach (var id in _journey.Spectrum())
                if (!spectrumBefore.Contains(id)) result.NewFighters.Add(id);

            _profile.AddCoins(result.CoinsEarned);
            _profile.Save();
            return result;
        }

        private void FinishMission(MissionDef mission, IBattleEngine engine, MatchResult result)
        {
            result.FailedObjectives = MissionService.FailedObjectives(mission, engine);
            result.MissionCompleted = result.FailedObjectives.Count == 0;
            if (!result.MissionCompleted)
            {
                result.CoinsEarned = _economy.MissionFailReward;
                return;
            }

            result.MissionFirstClear = !_profile.IsMissionDone(mission.Id);
            if (!result.MissionFirstClear)
            {
                result.CoinsEarned = _economy.MissionReplayReward;
                return;
            }

            _profile.MarkMissionDone(mission.Id);
            _profile.AddRecruit(mission.RecruitId);
            result.CoinsEarned = mission.RewardCoins;
            if (mission.RewardItemId != null)
            {
                _profile.AddItem(mission.RewardItemId, 1);
                result.ItemRewardId = mission.RewardItemId;
            }
        }
    }
}
