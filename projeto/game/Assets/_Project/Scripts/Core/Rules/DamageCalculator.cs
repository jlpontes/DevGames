using System;

namespace ComboTactics.Core
{
    public struct DamageResult
    {
        public int Amount;
        public double AdvantageMultiplier;
        public double ArenaMultiplier;
        public Effectiveness Effectiveness;
    }

    /// <summary>
    /// Fórmula de dano do GDD §3.3 e ARCHITECTURE §3.2:
    /// dano = poder/100 × Ataque × buffs × vantagens × arena × (1 − Defesa × 0,5%), com corte máximo de 50%.
    /// </summary>
    public static class DamageCalculator
    {
        public static DamageResult Compute(Fighter attacker, Fighter defender, SkillSpec skill, ArenaSpec arena, BattleRules rules)
        {
            return Compute(attacker.Spec, defender.Spec, skill, arena, rules, attacker.AttackMultiplier, defender.DefenseMultiplier);
        }

        public static DamageResult Compute(FighterSpec attacker, FighterSpec defender, SkillSpec skill, ArenaSpec arena,
            BattleRules rules, double attackBuff = 1.0, double defenseBuff = 1.0)
        {
            double raw = skill.Power / 100.0 * attacker.Attack * attackBuff;
            double advantage = EffectivenessResolver.Multiplier(attacker, defender, rules);
            double env = arena == null ? 1.0 : ArenaSystem.Multiplier(arena, attacker, rules);
            double defense = Math.Min(defender.Defense * defenseBuff, rules.DefenseCap);
            double defenseCut = 1.0 - defense * rules.DefenseCutPerPoint;

            var result = new DamageResult();
            result.AdvantageMultiplier = advantage;
            result.ArenaMultiplier = env;
            result.Amount = Math.Max(1, (int)Math.Round(raw * advantage * env * defenseCut, MidpointRounding.AwayFromZero));
            result.Effectiveness = advantage > 1.0001 ? Effectiveness.SuperEffective
                : advantage < 0.9999 ? Effectiveness.NotEffective : Effectiveness.Neutral;
            return result;
        }
    }
}
