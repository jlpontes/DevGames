namespace ComboTactics.Core
{
    /// <summary>Limites e chances das regras de prioridade da IA (ADR-004). Um perfil por oponente e por dificuldade.</summary>
    public sealed class AIProfile
    {
        public readonly string Id;
        public readonly string Name;
        /// <summary>Usa Kit Médico abaixo desta fração do HP máximo.</summary>
        public readonly double SurvivalHpPct;
        public readonly double SwitchChance;
        public readonly int DotTurnsBeforeCleanse;
        public readonly double QteChance;
        /// <summary>Parte dos acertos no QTE que vira contra-ataque; o resto é desvio.</summary>
        public readonly double CounterShare;
        public readonly double MistakeChance;

        public AIProfile(string id, string name, double survivalHpPct, double switchChance, int dotTurnsBeforeCleanse,
            double qteChance, double counterShare, double mistakeChance)
        {
            Id = id; Name = name; SurvivalHpPct = survivalHpPct; SwitchChance = switchChance;
            DotTurnsBeforeCleanse = dotTurnsBeforeCleanse; QteChance = qteChance;
            CounterShare = counterShare; MistakeChance = mistakeChance;
        }
    }
}
