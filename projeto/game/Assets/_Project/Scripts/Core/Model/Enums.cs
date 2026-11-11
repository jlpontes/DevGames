namespace ComboTactics.Core
{
    /// <summary>Elementos do GDD §3.1. Neutral fica fora do ciclo (usado pelo Boss).</summary>
    public enum Element { Neutral = 0, Fire = 1, Water = 2, Plant = 3 }

    /// <summary>Classes do GDD §3.2. Neutral fica fora do ciclo (usado pelo Boss).</summary>
    public enum CharClass { Neutral = 0, Warrior = 1, Mage = 2, Tank = 3 }

    public enum Stat { Hp = 0, Attack = 1, Defense = 2, Speed = 3 }

    public enum ArenaId { Sea = 0, Volcano = 1, Forest = 2, Moon = 3 }

    public enum EffectKind { DamageOverTime, HealOverTime, AttackModifier, DefenseModifier }

    /// <summary>Quem recebe o efeito de uma habilidade.</summary>
    public enum EffectTarget { Self, Enemy }

    public enum ItemKind { Heal, Cleanse, ApplyEffect }

    /// <summary>Resultado da janela de resposta (QTE), GDD R3.</summary>
    public enum ReactionResult { Miss, Dodge, Counter }

    public enum Effectiveness { NotEffective, Neutral, SuperEffective }

    /// <summary>O que o motor está esperando para continuar.</summary>
    public enum PendingKind { Actions, Reaction, ForcedSwitch, None }
}
