namespace ComboTactics.Core
{
    /// <summary>Os dois ciclos de vantagem do GDD §3: Fogo → Planta → Água → Fogo e Guerreiro → Mago → Tanque → Guerreiro.</summary>
    public static class EffectivenessResolver
    {
        public static bool Beats(Element attacker, Element defender)
        {
            return (attacker == Element.Fire && defender == Element.Plant)
                || (attacker == Element.Plant && defender == Element.Water)
                || (attacker == Element.Water && defender == Element.Fire);
        }

        public static bool Beats(CharClass attacker, CharClass defender)
        {
            return (attacker == CharClass.Warrior && defender == CharClass.Mage)
                || (attacker == CharClass.Mage && defender == CharClass.Tank)
                || (attacker == CharClass.Tank && defender == CharClass.Warrior);
        }

        /// <summary>Quantas vantagens (0 a 2) o primeiro lutador tem sobre o segundo.</summary>
        public static int CountAdvantages(FighterSpec fighter, FighterSpec opponent)
        {
            int n = 0;
            if (Beats(fighter.Element, opponent.Element)) n++;
            if (Beats(fighter.Class, opponent.Class)) n++;
            return n;
        }

        /// <summary>Bônus de quem ataca multiplicado pela redução de quem defende (R1: 1,3 × 0,7 = 0,91).</summary>
        public static double Multiplier(FighterSpec attacker, FighterSpec defender, BattleRules rules)
        {
            int attackerAdv = CountAdvantages(attacker, defender);
            int defenderAdv = CountAdvantages(defender, attacker);
            double attack = attackerAdv == 2 ? rules.DoubleAdvantageAttack : attackerAdv == 1 ? rules.SingleAdvantageAttack : 1.0;
            double defense = defenderAdv == 2 ? rules.DoubleAdvantageDefense : defenderAdv == 1 ? rules.SingleAdvantageDefense : 1.0;
            return attack * defense;
        }
    }
}
