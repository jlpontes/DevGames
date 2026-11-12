using System;
using System.Collections.Generic;
using System.Text;
using ComboTactics.Core;
using ComboTactics.Game;

namespace ComboTactics.Tests
{
    /// <summary>
    /// Joga a jornada inteira do Modo História com os serviços reais (espectro, HP da jornada, Altar, missões),
    /// uma IA no lugar do jogador. Serve para calibrar os guardiões e as missões.
    /// </summary>
    public static class CampaignSimulation
    {
        public sealed class Run
        {
            public Element Affinity;
            /// <summary>Tentativas até vencer cada fase (0 = não venceu dentro do limite).</summary>
            public int[] Tries = new int[4];
            public int MissionsWon;
            public int MissionsTried;
            public bool Completed;
            public int Coins;
        }

        /// <summary>Um jogador "humano médio": acerta mais da metade das defesas e erra pouco.</summary>
        public static AIProfile TypicalPlayer()
        {
            return new AIProfile("player", "Jogador", 0.25, 0.70, 2, 0.55, 0.30, 0.05);
        }

        public static Run Play(Element affinity, AIProfile player, bool doMissions, int seed, int maxTriesPerStage = 6)
        {
            var storage = new FakeStorage();
            var game = new GameServices(new TwoSlotSaveStore(storage, new FakeSerializer()));
            var run = new Run { Affinity = affinity };
            game.Journey.ChooseAffinity(affinity);
            int battle = 0;

            for (int stage = 0; stage < game.Content.Opponents.Count; stage++)
            {
                if (doMissions) TryMissions(game, player, seed, ref battle, run);

                var opponent = game.Content.Opponents[stage];
                for (int attempt = 1; attempt <= maxTriesPerStage; attempt++)
                {
                    if (game.Journey.AnyDamaged() && game.Profile.Coins >= game.Economy.HealAllCost && MostlyHurt(game)) game.Journey.HealAll();
                    var team = BestTeam(game, opponent.TeamIds, 3, true);
                    if (team == null)
                    {
                        // Ninguém de pé: paga o Altar se puder; senão a jornada trava.
                        if (game.Journey.HealAll() != PurchaseResult.Ok) break;
                        team = BestTeam(game, opponent.TeamIds, 3, true);
                    }
                    var request = new MatchRequest { Mode = GameMode.Campaign, StageIndex = stage, Team = team, Seed = seed * 1000 + battle++ };
                    var match = game.Launcher.Create(request);
                    var engine = new BattleEngine(match.Setup);
                    BattleRunner.Run(engine, new RuleBasedAIController(player, seed * 7919 + battle), new RuleBasedAIController(match.OpponentAi, seed * 104729 + battle));
                    var result = game.Launcher.Finish(match, engine);
                    if (result.PlayerWon) { run.Tries[stage] = attempt; break; }
                }
                if (run.Tries[stage] == 0) break;
            }
            run.Completed = game.Campaign.IsCompleted;
            run.Coins = game.Profile.Coins;
            return run;
        }

        private static bool MostlyHurt(GameServices game)
        {
            int hurt = 0, total = 0;
            foreach (var id in game.Journey.Spectrum())
            {
                total++;
                if (game.Journey.CurrentHp(id) < game.Journey.MaxHp(id) * 0.5) hurt++;
            }
            return hurt * 2 >= total;
        }

        private static void TryMissions(GameServices game, AIProfile player, int seed, ref int battle, Run run)
        {
            foreach (var mission in game.Missions.All)
            {
                if (game.Missions.State(mission) != MissionState.Available || !game.Missions.RecruitPending(mission)) continue;
                var team = BestTeam(game, mission.EnemyTeamIds, mission.PlayerTeamSize, false);
                var request = new MatchRequest { Mode = GameMode.Mission, MissionId = mission.Id, Team = team, Seed = seed * 1000 + battle++ };
                var match = game.Launcher.Create(request);
                var engine = new BattleEngine(match.Setup);
                BattleRunner.Run(engine, new RuleBasedAIController(player, seed * 31 + battle), new RuleBasedAIController(match.OpponentAi, seed * 37 + battle));
                var result = game.Launcher.Finish(match, engine);
                run.MissionsTried++;
                if (result.MissionCompleted) run.MissionsWon++;
            }
        }

        /// <summary>Escolhe os lutadores do espectro com melhor confronto contra a equipe inimiga (e com HP, na campanha).</summary>
        public static List<string> BestTeam(GameServices game, IReadOnlyList<string> enemyIds, int size, bool useJourneyHp)
        {
            var scored = new List<KeyValuePair<double, string>>();
            foreach (var id in game.Journey.Spectrum())
            {
                if (useJourneyHp && game.Journey.IsExhausted(id)) continue;
                var me = SimulationHelper.Spec(id);
                double score = 0;
                foreach (var enemyId in enemyIds)
                {
                    var enemy = SimulationHelper.Spec(enemyId);
                    score += EffectivenessResolver.CountAdvantages(me, enemy) - EffectivenessResolver.CountAdvantages(enemy, me);
                }
                if (useJourneyHp) score += 1.5 * game.Journey.CurrentHp(id) / (double)game.Journey.MaxHp(id);
                scored.Add(new KeyValuePair<double, string>(score, id));
            }
            if (scored.Count < size) return null;
            scored.Sort((a, b) => b.Key != a.Key ? b.Key.CompareTo(a.Key) : string.CompareOrdinal(a.Value, b.Value));
            var team = new List<string>();
            for (int i = 0; i < size; i++) team.Add(scored[i].Value);
            return team;
        }

        public static string Report(AIProfile player, bool doMissions, int runs)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Jornada completa, " + runs + " jornadas por afinidade, jogador " + player.Name + " (QTE " + (int)(player.QteChance * 100) + "%), missões: " + (doMissions ? "sim" : "não"));
            foreach (var affinity in new[] { Element.Fire, Element.Water, Element.Plant })
            {
                var tries = new double[4];
                var firstTry = new int[4];
                int completed = 0, missionsWon = 0, missionsTried = 0;
                for (int i = 0; i < runs; i++)
                {
                    var run = Play(affinity, player, doMissions, 500 + i);
                    if (run.Completed) completed++;
                    missionsWon += run.MissionsWon;
                    missionsTried += run.MissionsTried;
                    for (int s = 0; s < 4; s++)
                    {
                        tries[s] += run.Tries[s] == 0 ? 7 : run.Tries[s];
                        if (run.Tries[s] == 1) firstTry[s]++;
                    }
                }
                sb.Append("  " + StoryScript.ElementTitle(affinity).PadRight(7) + " completa " + Pct(completed, runs).PadLeft(4));
                for (int s = 0; s < 4; s++) sb.Append("  fase " + (s + 1) + ": 1ª tentativa " + Pct(firstTry[s], runs).PadLeft(4) + " (média " + Math.Round(tries[s] / runs, 1) + ")");
                if (doMissions) sb.Append("  missões " + Pct(missionsWon, Math.Max(1, missionsTried)));
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private static string Pct(int a, int b) { return (int)Math.Round(100.0 * a / b) + "%"; }
    }
}
