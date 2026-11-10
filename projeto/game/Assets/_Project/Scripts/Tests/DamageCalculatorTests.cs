using ComboTactics.Core;
using NUnit.Framework;

namespace ComboTactics.Tests
{
    [TestFixture]
    public class DamageCalculatorTests
    {
        private static readonly BattleRules Rules = new BattleRules();
        private static readonly SkillSpec Power100 = new SkillSpec("p100", "Poder 100", 100, false);

        private static ArenaSpec Arena(ArenaId id) { return SimulationHelper.Content.Catalog.Arenas[id]; }

        [Test]
        public void GddExample_FireMageHitsPlantTank_For135()
        {
            // SYSTEM-DESIGN §3.2: 171 × 1,5 × (1 − 95 × 0,005) = 134,66 → 135
            var result = DamageCalculator.Compute(SimulationHelper.Spec("fire_mage"), SimulationHelper.Spec("plant_tank"), Power100, null, Rules);
            Assert.AreEqual(135, result.Amount);
            Assert.AreEqual(Effectiveness.SuperEffective, result.Effectiveness);
        }

        [Test]
        public void GddExample_PlantTankHitsFireMage_For29()
        {
            // 63 × 0,5 × (1 − 15 × 0,005) = 29,14 → 29
            var result = DamageCalculator.Compute(SimulationHelper.Spec("plant_tank"), SimulationHelper.Spec("fire_mage"), Power100, null, Rules);
            Assert.AreEqual(29, result.Amount);
            Assert.AreEqual(Effectiveness.NotEffective, result.Effectiveness);
        }

        [Test]
        public void R1_MixedAdvantagesMultiply()
        {
            // Guerreiro de Fogo contra Mago de Água: 1,3 (classe) × 0,7 (elemento) = 0,91
            var attacker = SimulationHelper.Spec("fire_warrior");
            var defender = SimulationHelper.Spec("water_mage");
            Assert.AreEqual(0.91, EffectivenessResolver.Multiplier(attacker, defender, Rules), 1e-9);
            // 130 × 0,91 × (1 − 25 × 0,005) = 103,51 → 104
            Assert.AreEqual(104, DamageCalculator.Compute(attacker, defender, Power100, null, Rules).Amount);
        }

        [TestCase(20, 90)]
        [TestCase(50, 75)]
        [TestCase(100, 50)]
        public void DefenseCutsHalfPercentPerPoint(int defense, int expectedDamage)
        {
            var attacker = TestKit.Plain("a", attack: 100);
            var defender = TestKit.Fighter("d", Element.Neutral, CharClass.Neutral, 500, 10, defense, 10);
            Assert.AreEqual(expectedDamage, DamageCalculator.Compute(attacker, defender, Power100, null, Rules).Amount);
        }

        [Test]
        public void DefenseCutIsCappedAt50Percent_EvenWithBuffs()
        {
            var attacker = TestKit.Plain("a", attack: 100);
            var defender = TestKit.Fighter("d", Element.Neutral, CharClass.Neutral, 500, 10, 95, 10);
            Assert.AreEqual(50, DamageCalculator.Compute(attacker, defender, Power100, null, Rules, 1.0, 1.10).Amount);
        }

        [Test]
        public void AttackBuffScalesDamage()
        {
            var attacker = TestKit.Plain("a", attack: 100);
            var defender = TestKit.Plain("d");
            Assert.AreEqual(110, DamageCalculator.Compute(attacker, defender, Power100, null, Rules, 1.10, 1.0).Amount);
        }

        [Test]
        public void DamageIsNeverBelowOne()
        {
            var attacker = TestKit.Plain("a", attack: 1);
            var defender = TestKit.Fighter("d", Element.Neutral, CharClass.Neutral, 500, 10, 100, 10);
            Assert.AreEqual(1, DamageCalculator.Compute(attacker, defender, new SkillSpec("weak", "Fraco", 10, false), null, Rules).Amount);
        }

        [TestCase(ArenaId.Sea, "water_mage", 1.10)]
        [TestCase(ArenaId.Sea, "fire_mage", 0.90)]
        [TestCase(ArenaId.Sea, "plant_mage", 1.00)]
        [TestCase(ArenaId.Volcano, "fire_mage", 1.10)]
        [TestCase(ArenaId.Volcano, "plant_mage", 0.90)]
        [TestCase(ArenaId.Forest, "plant_mage", 1.10)]
        [TestCase(ArenaId.Forest, "water_mage", 0.90)]
        public void ElementArenasFavourAndPenalise(ArenaId arena, string characterId, double expected)
        {
            Assert.AreEqual(expected, ArenaSystem.Multiplier(Arena(arena), SimulationHelper.Spec(characterId), Rules), 1e-9);
        }

        // R5: Lua favorece Velocidade < 50 (os três Tanques e o Mago de Planta) e prejudica Velocidade ≥ 70 (os três Guerreiros).
        [TestCase("fire_tank", 1.10)]
        [TestCase("water_tank", 1.10)]
        [TestCase("plant_tank", 1.10)]
        [TestCase("plant_mage", 1.10)]
        [TestCase("fire_warrior", 0.90)]
        [TestCase("water_warrior", 0.90)]
        [TestCase("plant_warrior", 0.90)]
        [TestCase("fire_mage", 1.00)]
        [TestCase("water_mage", 1.00)]
        public void MoonUsesSpeedBands(string characterId, double expected)
        {
            Assert.AreEqual(expected, ArenaSystem.Multiplier(Arena(ArenaId.Moon), SimulationHelper.Spec(characterId), Rules), 1e-9);
        }

        [Test]
        public void ElementAndClassCyclesMatchTheGdd()
        {
            Assert.IsTrue(EffectivenessResolver.Beats(Element.Fire, Element.Plant));
            Assert.IsTrue(EffectivenessResolver.Beats(Element.Plant, Element.Water));
            Assert.IsTrue(EffectivenessResolver.Beats(Element.Water, Element.Fire));
            Assert.IsFalse(EffectivenessResolver.Beats(Element.Fire, Element.Water));
            Assert.IsFalse(EffectivenessResolver.Beats(Element.Fire, Element.Fire));

            Assert.IsTrue(EffectivenessResolver.Beats(CharClass.Warrior, CharClass.Mage));
            Assert.IsTrue(EffectivenessResolver.Beats(CharClass.Mage, CharClass.Tank));
            Assert.IsTrue(EffectivenessResolver.Beats(CharClass.Tank, CharClass.Warrior));
            Assert.IsFalse(EffectivenessResolver.Beats(CharClass.Mage, CharClass.Warrior));
            Assert.IsFalse(EffectivenessResolver.Beats(CharClass.Neutral, CharClass.Mage));
        }
    }
}
