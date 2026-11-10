namespace ComboTactics.Core
{
    /// <summary>
    /// Todos os números de regra da batalha num só lugar (GDD §3.3 e §5.7).
    /// São pontos de partida: ajuste aqui depois dos playtests, sem mexer no motor.
    /// </summary>
    public sealed class BattleRules
    {
        // Vantagens de elemento e classe (GDD §3.3, R1)
        public double SingleAdvantageAttack = 1.3;
        public double DoubleAdvantageAttack = 1.5;
        public double SingleAdvantageDefense = 0.7;
        public double DoubleAdvantageDefense = 0.5;

        // Defesa: 0,5% de corte por ponto, até 100 pontos
        public double DefenseCutPerPoint = 0.005;
        public int DefenseCap = 100;

        // Arena (GDD §5.6, R4, R5)
        public double ArenaBonus = 1.10;
        public double ArenaPenalty = 0.90;
        public int ArenaDrawInterval = 3;
        public int MoonSlowBelow = 50;
        public int MoonFastFrom = 70;

        // Stamina (R7)
        public int StaminaMax = 100;
        public int StaminaOnDealDamage = 20;
        public int StaminaOnTakeDamage = 15;
        public int StaminaOnReaction = 25;

        // Equipe e itens (R8)
        public int MaxTeamSize = 3;
        public int MaxItemsPerBattle = 3;

        /// <summary>Tempo que o marcador do QTE leva para cruzar a barra. A interface mede o clique.</summary>
        public float ReactionWindowSeconds = 0.6f;
    }
}
