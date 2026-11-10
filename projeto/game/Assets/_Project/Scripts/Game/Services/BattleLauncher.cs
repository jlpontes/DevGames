using System;
using System.Collections.Generic;
using ComboTactics.Core;

namespace ComboTactics.Game
{
    public enum GameMode { Campaign, VsAi, LocalPvp }

    /// <summary>O que as telas de menu e seleção de equipe juntam antes de uma batalha.</summary>
    public sealed class MatchRequest
    {
        public GameMode Mode;
        /// <summary>Equipe do jogador (ou do Jogador 1 no PvP): 3 ids diferentes; o primeiro começa na arena.</summary>
        public List<string> Team = new List<string>();
        /// <summary>Equipe do Jogador 2, só no PvP.</summary>
        public List<string> Team2 = new List<string>();
        /// <summary>Itens tirados do inventário para esta batalha (até 3). Ignorado no PvP, que usa o kit fixo.</summary>
        public Dictionary<string, int> Items = new Dictionary<string, int>();
        public int StageIndex;
        public string DifficultyId;
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
        public bool PlayerWon { get { return WinnerSide == 0; } }
    }

    /// <summary>
    /// Monta a batalha a partir do perfil e do conteúdo e, no fim, paga as moedas, consome os itens usados
    /// e atualiza a campanha (SYSTEM-DESIGN, Fluxo A).
    /// </summary>
    public sealed class BattleLauncher
    {
        private readonly ContentDatabase _content;
        private readonly EconomyConfig _economy;
        private readonly BattleRules _rules;
        private readonly ProfileService _profile;
        private readonly CampaignService _campaign;

        public BattleLauncher(ContentDatabase content, EconomyConfig economy, BattleRules rules,
            ProfileService profile, CampaignService campaign)
        {
            _content = content; _economy = economy; _rules = rules; _profile = profile; _campaign = campaign;
        }

        public Match Create(MatchRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");
            int seed = request.Seed ?? Environment.TickCount;
            var setupRng = new Rng(seed ^ 0x5F3759DF);

            switch (request.Mode)
            {
                case GameMode.Campaign: return CreateCampaign(request, seed);
                case GameMode.VsAi: return CreateVsAi(request, seed, setupRng);
                default: return CreatePvp(request, seed, setupRng);
            }
        }

        private Match CreateCampaign(MatchRequest request, int seed)
        {
            if (!_campaign.IsUnlocked(request.StageIndex)) throw new GameException("Esta fase ainda está bloqueada. Vença a fase anterior.");
            var opponent = _content.Opponents[request.StageIndex];

            var enemyTeam = new List<FighterSpec>();
            foreach (var id in opponent.TeamIds)
                enemyTeam.Add(StatsResolver.ForOpponent(_content.Character(id), _content, opponent.StatBonus));

            return new Match
            {
                Mode = GameMode.Campaign,
                Opponent = opponent,
                StageIndex = request.StageIndex,
                OpponentAi = _content.AiProfiles[opponent.AiProfileId],
                SideNames = new[] { "Você", opponent.Name },
                // R4: na campanha a batalha começa na arena do oponente.
                Setup = new BattleSetup(PlayerTeam(request.Team, true), enemyTeam, PlayerItems(request.Items), opponent.Items,
                    opponent.HomeArena, seed, _rules, _content.Catalog),
            };
        }

        private Match CreateVsAi(MatchRequest request, int seed, Rng rng)
        {
            AIProfile profile;
            if (request.DifficultyId == null || !_content.AiProfiles.TryGetValue(request.DifficultyId, out profile))
                throw new GameException("Escolha uma dificuldade.");

            // Equipe da IA: 3 personagens diferentes sorteados, com atributos base.
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
                Setup = new BattleSetup(PlayerTeam(request.Team, true), enemyTeam, PlayerItems(request.Items), _content.VsAiOpponentKit,
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
                Setup = new BattleSetup(PlayerTeam(request.Team, useUpgrades), PlayerTeam(request.Team2, useUpgrades),
                    _content.FairKit, _content.FairKit, ArenaSystem.Draw(rng), seed, _rules, _content.Catalog),
            };
        }

        private List<FighterSpec> PlayerTeam(List<string> ids, bool useUpgrades)
        {
            if (ids == null || ids.Count != _rules.MaxTeamSize)
                throw new GameException("Escolha " + _rules.MaxTeamSize + " personagens para a equipe.");

            var team = new List<FighterSpec>();
            var seen = new HashSet<string>();
            foreach (var id in ids)
            {
                var def = _content.Character(id);
                if (def == null || !def.Playable) throw new GameException("Personagem indisponível: " + id);
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

        /// <summary>Fecha a batalha: recompensa, itens consumidos, progresso da campanha e gravação do save.</summary>
        public MatchResult Finish(Match match, IBattleEngine engine)
        {
            if (!engine.IsOver) throw new InvalidOperationException("A batalha ainda não terminou.");

            var result = new MatchResult { Mode = match.Mode, WinnerSide = engine.WinnerSide };
            if (match.Mode == GameMode.LocalPvp)
            {
                result.CoinsEarned = _economy.PvpReward;
            }
            else
            {
                // R8: os itens usados saem do inventário; os que sobraram voltam.
                foreach (var itemId in engine.State.Sides[0].UsedItems) _profile.RemoveItem(itemId, 1);

                bool won = engine.WinnerSide == 0;
                if (match.Mode == GameMode.Campaign && won)
                {
                    result.CoinsEarned = match.Opponent.Reward;
                    result.UnlockedNextStage = _profile.CompleteStage(match.StageIndex);
                    result.CampaignCompleted = result.UnlockedNextStage && _campaign.IsCompleted;
                }
                else
                {
                    result.CoinsEarned = won ? _economy.VictoryReward : _economy.DefeatReward;
                }
            }

            _profile.AddCoins(result.CoinsEarned);
            _profile.Save();
            return result;
        }
    }
}
