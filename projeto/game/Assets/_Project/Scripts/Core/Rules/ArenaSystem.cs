namespace ComboTactics.Core
{
    /// <summary>Modificador de arena (+10% / −10% no dano de quem ataca) e sorteio a cada 3 rodadas.</summary>
    public static class ArenaSystem
    {
        public static double Multiplier(ArenaSpec arena, FighterSpec attacker, BattleRules rules)
        {
            if (arena.UsesSpeedBands)
            {
                if (attacker.Speed < rules.MoonSlowBelow) return rules.ArenaBonus;
                if (attacker.Speed >= rules.MoonFastFrom) return rules.ArenaPenalty;
                return 1.0;
            }
            if (attacker.Element == Element.Neutral) return 1.0;
            if (attacker.Element == arena.Favoured) return rules.ArenaBonus;
            if (attacker.Element == arena.Penalised) return rules.ArenaPenalty;
            return 1.0;
        }

        /// <summary>Sorteio uniforme entre as 4 arenas; a atual pode se repetir.</summary>
        public static ArenaId Draw(Rng rng)
        {
            return (ArenaId)rng.Next(4);
        }
    }
}
