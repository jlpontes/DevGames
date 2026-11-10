using System.Collections.Generic;
using ComboTactics.Core;
using ComboTactics.Game;
using NUnit.Framework;

namespace ComboTactics.Tests
{
    /// <summary>As regras de prioridade do Level Design §4, uma a uma.</summary>
    [TestFixture]
    public class AIControllerTests
    {
        // Perfil determinístico: sempre troca quando a regra 2 permite, nunca erra.
        private static AIProfile Profile(double survival = 0.20, double switchChance = 1.0, double qte = 0.0, double counterShare = 0.25, double mistake = 0.0)
        {
            return new AIProfile("test", "Teste", survival, switchChance, 2, qte, counterShare, mistake);
        }

        private static ICommand Decide(RuleBasedAIController ai, BattleEngine engine, int side = 1)
        {
            ICommand command = null;
            ai.RequestAction(engine, side, c => command = c);
            return command;
        }

        [Test]
        public void Rule1_UsesMedKitBelowSurvivalThreshold()
        {
            var items = new Dictionary<string, int> { { BuiltInContent.MedKit, 1 } };
            var engine = TestKit.Engine(TestKit.Team(TestKit.Plain("a", attack: 850, speed: 90)), TestKit.Team(TestKit.Plain("b", speed: 10)), 7, null, items);
            TestKit.Round(engine, TestKit.Attack(0), TestKit.Attack(1)); // 1000 → 150 (15%)

            var ai = new RuleBasedAIController(Profile(), 1);
            var command = Decide(ai, engine) as UseItemCommand;
            Assert.IsNotNull(command);
            Assert.AreEqual(BuiltInContent.MedKit, command.ItemId);
            Assert.AreEqual(1, ai.LastRule);
        }

        [Test]
        public void Rule2_SwitchesToABenchFighterWithAdvantage()
        {
            // Ativo da IA: Mago de Fogo (sem vantagem sobre o Guerreiro de Água). No banco: Guerreiro de Planta (Planta → Água).
            var engine = TestKit.Engine(SimulationHelper.Team("water_warrior"), SimulationHelper.Team("fire_mage", "plant_warrior"));
            var ai = new RuleBasedAIController(Profile(), 1);
            var command = Decide(ai, engine) as SwitchCommand;

            Assert.IsNotNull(command);
            Assert.AreEqual(1, command.TeamIndex);
            Assert.AreEqual(2, ai.LastRule);
        }

        [Test]
        public void Rule2_DoesNotSwitchWhenTheActiveAlreadyHasAnAdvantage()
        {
            var engine = TestKit.Engine(SimulationHelper.Team("water_warrior"), SimulationHelper.Team("plant_mage", "plant_warrior"));
            var ai = new RuleBasedAIController(Profile(), 1);
            Assert.IsInstanceOf<AttackCommand>(Decide(ai, engine));
        }

        [Test]
        public void NeverSwitchesTwoTurnsInARow()
        {
            var engine = TestKit.Engine(SimulationHelper.Team("water_warrior", "fire_warrior"), SimulationHelper.Team("fire_mage", "plant_warrior"));
            var ai = new RuleBasedAIController(Profile(), 1);

            var first = Decide(ai, engine);
            Assert.IsInstanceOf<SwitchCommand>(first);
            // O jogador troca para o Guerreiro de Fogo, que vence o Guerreiro de Planta recém-entrado.
            TestKit.Round(engine, TestKit.Switch(0, 1), first);
            Assert.IsInstanceOf<AttackCommand>(Decide(ai, engine), "troca em dois turnos seguidos");
        }

        [Test]
        public void Rule3_CleansesADamageOverTimeAfterTwoTurns()
        {
            var items = new Dictionary<string, int> { { BuiltInContent.Medicine, 1 } };
            var engine = TestKit.Engine(
                TestKit.Team(TestKit.Plain("a", speed: 90, secondEffect: BuiltInContent.Poison)),
                TestKit.Team(TestKit.Plain("b", hp: 5000, speed: 10)), 7, null, items);
            var ai = new RuleBasedAIController(Profile(), 1);

            TestKit.Round(engine, TestKit.Attack(0, 1), TestKit.Attack(1));
            Assert.IsInstanceOf<AttackCommand>(Decide(ai, engine), "veneno há só 1 turno");
            TestKit.Round(engine, TestKit.Attack(0), TestKit.Attack(1));

            var command = Decide(ai, engine) as UseItemCommand;
            Assert.IsNotNull(command);
            Assert.AreEqual(BuiltInContent.Medicine, command.ItemId);
            Assert.AreEqual(3, ai.LastRule);
        }

        [Test]
        public void Rule4_UsesTheSpecialWhenStaminaIsFull()
        {
            var engine = TestKit.Engine(TestKit.Team(TestKit.Plain("a", hp: 5000)), TestKit.Team(TestKit.Plain("b", hp: 5000)));
            for (int i = 0; i < 3; i++) TestKit.Round(engine, TestKit.Attack(0), TestKit.Attack(1));

            var ai = new RuleBasedAIController(Profile(), 1);
            var command = Decide(ai, engine) as AttackCommand;
            Assert.AreEqual(2, command.SkillIndex);
            Assert.AreEqual(4, ai.LastRule);
        }

        [Test]
        public void Rule5_PicksTheNormalSkillWithMoreDamage()
        {
            var engine = TestKit.Engine(TestKit.Team(TestKit.Plain("a")), TestKit.Team(TestKit.Plain("b")));
            var ai = new RuleBasedAIController(Profile(), 1);
            var command = Decide(ai, engine) as AttackCommand;

            Assert.AreEqual(0, command.SkillIndex); // poder 100 contra poder 50
            Assert.AreEqual(5, ai.LastRule);
        }

        [Test]
        public void ReactionChanceFollowsTheProfile()
        {
            var engine = TestKit.Engine(TestKit.Team(TestKit.Plain("a")), TestKit.Team(TestKit.Plain("b")));
            var ai = new RuleBasedAIController(Profile(qte: 0.40, counterShare: 0.25), 99);
            int dodges = 0, counters = 0;
            const int rolls = 4000;
            for (int i = 0; i < rolls; i++)
            {
                ai.RequestReaction(engine, 1, 0.6f, r =>
                {
                    if (r == ReactionResult.Dodge) dodges++;
                    else if (r == ReactionResult.Counter) counters++;
                });
            }
            // Level Design: 40% de acerto; 3 em cada 4 acertos são desvios e 1 em cada 4 é contra-ataque.
            Assert.That((dodges + counters) / (double)rolls, Is.InRange(0.37, 0.43));
            Assert.That(counters / (double)(dodges + counters), Is.InRange(0.21, 0.29));
        }

        [Test]
        public void ForcedSwitchPicksTheBestMatchup()
        {
            // Contra o Guerreiro de Água: o Tanque de Fogo tem 1 vantagem e 1 desvantagem; o Tanque de Planta tem 2 vantagens.
            var engine = TestKit.Engine(
                TestKit.Team(SimulationHelper.Spec("water_warrior")),
                SimulationHelper.Team("fire_mage", "fire_tank", "plant_tank"));
            int choice = -1;
            new RuleBasedAIController(Profile(), 1).RequestForcedSwitch(engine, 1, i => choice = i);
            Assert.AreEqual(2, choice);
        }

        [Test]
        public void BossProfileNeverTriesToSwitchOrHeal()
        {
            var content = SimulationHelper.Content;
            var boss = content.Opponents[3];
            var engine = TestKit.Engine(SimulationHelper.Team("fire_mage", "water_warrior", "plant_tank"),
                TestKit.Team(SimulationHelper.Spec(BuiltInContent.BossId)), 5, content.FairKit, boss.Items, boss.HomeArena);
            var outcome = BattleRunner.Run(engine,
                new RuleBasedAIController(content.AiProfiles[BuiltInContent.Normal], 1),
                new RuleBasedAIController(content.AiProfiles[boss.AiProfileId], 2));

            Assert.IsFalse(outcome.TimedOut);
            foreach (var line in engine.CommandLog) Assert.IsFalse(line.Contains("Switch(1"), line);
        }
    }
}
