using System;
using System.Collections.Generic;
using System.Text;
using ComboTactics.Core;
using ComboTactics.Game;
using NUnit.Framework;

namespace ComboTactics.Tests
{
    /// <summary>
    /// Simulação de balanceamento (ADR-002, SYSTEM-DESIGN §4.4): milhares de batalhas IA contra IA.
    /// Rode PrintBalanceReport depois de mexer em atributos, habilidades ou perfis de IA.
    /// </summary>
    [TestFixture]
    public class BalanceSimulationTests
    {
        private static readonly ContentDatabase Content = SimulationHelper.Content;

        [Test]
        public void MirrorMatchesAreFair()
        {
            foreach (var character in Content.PlayableCharacters)
            {
                var stats = SimulationHelper.Duels(character.Id, character.Id, 400, 0.5);
                double decided = stats.Wins0 + stats.Wins1;
                Assert.That(stats.Wins0 / decided, Is.InRange(0.42, 0.58), character.Id);
            }
        }

        [Test]
        public void MatchupsWhereAdvantagesCancelOut_StayBetween40And60Percent()
        {
            // Cada lado tem exatamente uma vantagem (ex.: Guerreiro de Fogo contra Mago de Água, R1).
            // Com atributos equivalentes (GDD §4), nenhum dos dois pode dominar.
            var failures = new List<string>();
            var characters = Content.PlayableCharacters;
            for (int i = 0; i < characters.Count; i++)
            {
                for (int j = i + 1; j < characters.Count; j++)
                {
                    var a = SimulationHelper.Spec(characters[i].Id);
                    var b = SimulationHelper.Spec(characters[j].Id);
                    if (EffectivenessResolver.CountAdvantages(a, b) != 1 || EffectivenessResolver.CountAdvantages(b, a) != 1) continue;

                    // Joga dos dois lados para o resultado não depender de quem é o lado 0.
                    var asSide0 = SimulationHelper.Duels(a.Id, b.Id, 300, 0.5);
                    var asSide1 = SimulationHelper.Duels(b.Id, a.Id, 300, 0.5);
                    double winRate = (asSide0.Wins0 + asSide1.Wins1) / (double)(asSide0.Games + asSide1.Games);
                    if (winRate < 0.40 || winRate > 0.60)
                        failures.Add(a.Id + " contra " + b.Id + ": " + Math.Round(winRate * 100) + "%");
                }
            }
            Assert.AreEqual(0, failures.Count, string.Join("; ", failures));
        }

        [Test]
        public void EveryCharacterWinsAboutHalfOfItsDuels()
        {
            foreach (var a in Content.PlayableCharacters)
            {
                double sum = 0;
                foreach (var b in Content.PlayableCharacters) sum += SimulationHelper.Duels(a.Id, b.Id, 120, 0.5).WinRate0;
                Assert.That(sum / Content.PlayableCharacters.Count, Is.InRange(0.42, 0.58), a.Id);
            }
        }

        [Test]
        public void BattlesAlwaysEnd_AndTheBaselineStaysUnder40Rounds()
        {
            // Sem QTE é o piso de duração: só as regras de dano. O alerta do NEXT-STEPS §6 é passar de 40 rodadas.
            var baseline = SimulationHelper.TeamBattles(SimulationHelper.DuelProfile(0), SimulationHelper.DuelProfile(0), 300);
            Assert.AreEqual(0, baseline.TimedOut);
            Assert.That(baseline.AverageRounds, Is.LessThan(40));

            foreach (var profile in Content.Difficulties)
            {
                var stats = SimulationHelper.TeamBattles(profile, profile, 200);
                Assert.AreEqual(0, stats.TimedOut, "batalha sem fim com o perfil " + profile.Name);
            }
        }

        [Test]
        public void HardBeatsEasyInMoreThan75PercentOfMirroredBattles()
        {
            // ADR-004, item de ação 4.
            var hard = Content.AiProfiles[BuiltInContent.Hard];
            var easy = Content.AiProfiles[BuiltInContent.Easy];
            int wins = 0;
            const int games = 200;
            for (int i = 0; i < games; i++)
            {
                var team = SimulationHelper.RandomTeam(new Rng(3000 + i));
                bool hardIsSide0 = i % 2 == 0;
                var outcome = SimulationHelper.Run(team, team, hardIsSide0 ? hard : easy, hardIsSide0 ? easy : hard, 3000 + i, Content.FairKit, Content.FairKit);
                if (outcome.WinnerSide == (hardIsSide0 ? 0 : 1)) wins++;
            }
            Assert.That(wins / (double)games, Is.GreaterThan(0.75));
        }

        [Test]
        public void TheThreeTrainersGetProgressivelyHarder()
        {
            var player = Content.AiProfiles[BuiltInContent.Normal];
            double previous = 1.01;
            for (int stage = 0; stage < 3; stage++)
            {
                double winRate = SimulationHelper.CampaignStage(stage, player, 300).WinRate0;
                Assert.That(winRate, Is.LessThan(previous), "fase " + stage);
                previous = winRate;
            }
        }

        [Test]
        public void PrintBalanceReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine(SimulationHelper.DuelMatrixReport(200, 0.5));

            sb.AppendLine("Batalhas 3v3 com equipes sorteadas (média de rodadas; alerta acima de 40):");
            AppendTeamLine(sb, "sem QTE", SimulationHelper.DuelProfile(0));
            foreach (var profile in Content.Difficulties) AppendTeamLine(sb, profile.Name + " contra " + profile.Name, profile);

            sb.AppendLine();
            sb.AppendLine("Campanha jogada por uma IA no lugar do jogador (equipe sorteada, atributos base):");
            foreach (var profile in Content.Difficulties)
            {
                sb.Append("  jogador " + profile.Name.PadRight(8));
                for (int stage = 0; stage < Content.Opponents.Count; stage++)
                {
                    var stats = SimulationHelper.CampaignStage(stage, profile, 200);
                    sb.Append("  " + Content.Opponents[stage].Name + " " + Math.Round(stats.WinRate0 * 100) + "% (" + Math.Round(stats.AverageRounds) + " rod.)");
                }
                sb.AppendLine();
            }
            // Assert.Pass leva o relatório para o painel de detalhes do Test Runner do Unity.
            Assert.Pass(sb.ToString());
        }

        private static void AppendTeamLine(StringBuilder sb, string label, AIProfile profile)
        {
            var stats = SimulationHelper.TeamBattles(profile, profile, 300);
            sb.AppendLine("  " + label.PadRight(24) + Math.Round(stats.AverageRounds, 1).ToString().PadLeft(6) + " rodadas"
                + (stats.AverageRounds > 40 ? "   ALERTA: acima de 40" : ""));
        }
    }
}
