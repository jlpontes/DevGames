using System;
using System.Collections.Generic;
using System.Text;
using ComboTactics.Core;
using ComboTactics.Game;

namespace ComboTactics.Tests
{
    /// <summary>Batalhas IA contra IA sem interface, para os testes de ponta a ponta e a simulação de balanceamento.</summary>
    public static class SimulationHelper
    {
        public static readonly ContentDatabase Content = BuiltInContent.Create();
        public static readonly BattleRules Rules = new BattleRules();

        /// <summary>Perfil "neutro" da simulação: sem erros, sem trocas, QTE configurável.</summary>
        public static AIProfile DuelProfile(double qteChance)
        {
            return new AIProfile("sim", "Simulação", 0.20, 0.0, 2, qteChance, 0.25, 0.0);
        }

        public static FighterSpec Spec(string id, double statBonus = 0)
        {
            return StatsResolver.ForOpponent(Content.Character(id), Content, statBonus);
        }

        public static List<FighterSpec> Team(params string[] ids)
        {
            var team = new List<FighterSpec>();
            foreach (var id in ids) team.Add(Spec(id));
            return team;
        }

        public static BattleEngine Engine(IReadOnlyList<FighterSpec> team0, IReadOnlyList<FighterSpec> team1, int seed,
            ArenaId arena = ArenaId.Sea, IReadOnlyDictionary<string, int> items0 = null, IReadOnlyDictionary<string, int> items1 = null)
        {
            return new BattleEngine(new BattleSetup(team0, team1, items0, items1, arena, seed, Rules, Content.Catalog));
        }

        public static BattleOutcome Run(IReadOnlyList<FighterSpec> team0, IReadOnlyList<FighterSpec> team1,
            AIProfile ai0, AIProfile ai1, int seed, IReadOnlyDictionary<string, int> items0 = null, IReadOnlyDictionary<string, int> items1 = null,
            ArenaId? arena = null)
        {
            var rng = new Rng(seed);
            var engine = Engine(team0, team1, seed, arena ?? ArenaSystem.Draw(rng), items0, items1);
            return BattleRunner.Run(engine, new RuleBasedAIController(ai0, seed * 31 + 1), new RuleBasedAIController(ai1, seed * 31 + 2));
        }

        public sealed class Stats
        {
            public int Games, Wins0, Wins1, Draws, TimedOut;
            public long Rounds;
            public double WinRate0 { get { return Games == 0 ? 0 : (double)Wins0 / Games; } }
            public double AverageRounds { get { return Games == 0 ? 0 : (double)Rounds / Games; } }

            public void Add(BattleOutcome outcome)
            {
                Games++;
                Rounds += outcome.Rounds;
                if (outcome.TimedOut) TimedOut++;
                else if (outcome.WinnerSide == 0) Wins0++;
                else if (outcome.WinnerSide == 1) Wins1++;
                else Draws++;
            }
        }

        public static Stats Duels(string id0, string id1, int games, double qteChance, int seedBase = 1000)
        {
            var stats = new Stats();
            var profile = DuelProfile(qteChance);
            for (int i = 0; i < games; i++)
                stats.Add(Run(Team(id0), Team(id1), profile, profile, seedBase + i));
            return stats;
        }

        public static List<FighterSpec> RandomTeam(Rng rng, double statBonus = 0)
        {
            var pool = new List<CharacterDef>(Content.PlayableCharacters);
            var team = new List<FighterSpec>();
            while (team.Count < 3)
            {
                int pick = rng.Next(pool.Count);
                team.Add(StatsResolver.ForOpponent(pool[pick], Content, statBonus));
                pool.RemoveAt(pick);
            }
            return team;
        }

        /// <summary>Batalhas 3v3 com equipes sorteadas e o kit fixo de itens para os dois lados.</summary>
        public static Stats TeamBattles(AIProfile ai0, AIProfile ai1, int games, int seedBase = 20000)
        {
            var stats = new Stats();
            for (int i = 0; i < games; i++)
            {
                var rng = new Rng(seedBase + i);
                stats.Add(Run(RandomTeam(rng), RandomTeam(rng), ai0, ai1, seedBase + i, Content.FairKit, Content.FairKit));
            }
            return stats;
        }

        /// <summary>Uma fase da campanha jogada por uma IA no lugar do jogador, com equipes sorteadas e atributos base.</summary>
        public static Stats CampaignStage(int stageIndex, AIProfile player, int games, int seedBase = 40000)
        {
            var opponent = Content.Opponents[stageIndex];
            var enemy = new List<FighterSpec>();
            foreach (var id in opponent.TeamIds) enemy.Add(Spec(id, opponent.StatBonus));
            var stats = new Stats();
            for (int i = 0; i < games; i++)
            {
                var rng = new Rng(seedBase + i);
                stats.Add(Run(RandomTeam(rng), enemy, player, Content.AiProfiles[opponent.AiProfileId], seedBase + i,
                    Content.FairKit, opponent.Items, opponent.HomeArena));
            }
            return stats;
        }

        /// <summary>Matriz 9×9 de taxa de vitória (linha contra coluna) em duelos 1v1, mais a média de rodadas.</summary>
        public static string DuelMatrixReport(int games, double qteChance)
        {
            var ids = new List<string>();
            foreach (var c in Content.PlayableCharacters) ids.Add(c.Id);
            var sb = new StringBuilder();
            sb.AppendLine("Taxa de vitória da linha contra a coluna, " + games + " duelos 1v1 por confronto, QTE " + (int)(qteChance * 100) + "%");
            sb.Append("".PadRight(14));
            foreach (var id in ids) sb.Append(Short(id).PadLeft(7));
            sb.AppendLine("   média  rodadas");
            double totalRounds = 0; int cells = 0;
            foreach (var row in ids)
            {
                sb.Append(Short(row).PadRight(14));
                double sum = 0; double rounds = 0;
                foreach (var col in ids)
                {
                    var stats = Duels(row, col, games, qteChance);
                    sum += stats.WinRate0; rounds += stats.AverageRounds;
                    totalRounds += stats.AverageRounds; cells++;
                    sb.Append(((int)Math.Round(stats.WinRate0 * 100) + "%").PadLeft(7));
                }
                sb.AppendLine(("" + (int)Math.Round(sum / ids.Count * 100) + "%").PadLeft(8) + ("" + Math.Round(rounds / ids.Count, 1)).PadLeft(9));
            }
            sb.AppendLine("Média geral de rodadas por duelo: " + Math.Round(totalRounds / cells, 1));
            return sb.ToString();
        }

        private static string Short(string id)
        {
            return id.Replace("_warrior", "-G").Replace("_mage", "-M").Replace("_tank", "-T");
        }
    }
}
