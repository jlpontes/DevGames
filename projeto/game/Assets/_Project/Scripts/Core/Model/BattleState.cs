using System.Collections.Generic;

namespace ComboTactics.Core
{
    public sealed class ActiveEffect
    {
        public readonly EffectSpec Spec;
        /// <summary>Dano ou cura por turno, já calculado na aplicação. Zero para modificadores.</summary>
        public readonly int Value;
        public int TurnsLeft;
        public int TurnsActive;

        public ActiveEffect(EffectSpec spec, int value)
        {
            Spec = spec; Value = value; TurnsLeft = spec.Turns; TurnsActive = 0;
        }
    }

    public sealed class Fighter
    {
        public readonly FighterSpec Spec;
        public int Hp;
        /// <summary>Os efeitos ficam no lutador e continuam valendo no banco (GDD §5.2).</summary>
        public readonly List<ActiveEffect> Effects = new List<ActiveEffect>();

        /// <summary>Verdadeiro depois que a segunda fase (PhaseShiftSpec) foi ativada nesta batalha.</summary>
        public bool PhaseShifted;

        public Fighter(FighterSpec spec) { Spec = spec; Hp = spec.MaxHp; }

        public bool IsFainted { get { return Hp <= 0; } }

        public double AttackMultiplier { get { return Multiplier(EffectKind.AttackModifier); } }
        public double DefenseMultiplier { get { return Multiplier(EffectKind.DefenseModifier); } }

        public bool HasNegativeEffect
        {
            get
            {
                for (int i = 0; i < Effects.Count; i++) if (Effects[i].Spec.IsNegative) return true;
                return false;
            }
        }

        public ActiveEffect FindEffect(string effectId)
        {
            for (int i = 0; i < Effects.Count; i++) if (Effects[i].Spec.Id == effectId) return Effects[i];
            return null;
        }

        private double Multiplier(EffectKind kind)
        {
            double m = 1.0;
            for (int i = 0; i < Effects.Count; i++)
                if (Effects[i].Spec.Kind == kind) m *= 1.0 + Effects[i].Spec.Value;
            return m;
        }
    }

    public sealed class Side
    {
        public readonly Fighter[] Team;
        public int ActiveIndex;
        /// <summary>Uma barra por jogador, compartilhada pela equipe (R7).</summary>
        public int Stamina;
        public readonly Dictionary<string, int> Items;
        /// <summary>Itens gastos nesta batalha, para descontar do inventário no fim.</summary>
        public readonly List<string> UsedItems = new List<string>();

        public Side(Fighter[] team, Dictionary<string, int> items) { Team = team; Items = items; }

        public Fighter Active { get { return Team[ActiveIndex]; } }

        public bool AllFainted
        {
            get
            {
                for (int i = 0; i < Team.Length; i++) if (!Team[i].IsFainted) return false;
                return true;
            }
        }

        public bool HasBenchAlive
        {
            get
            {
                for (int i = 0; i < Team.Length; i++) if (i != ActiveIndex && !Team[i].IsFainted) return true;
                return false;
            }
        }

        public int ItemCount(string itemId)
        {
            int count;
            return Items.TryGetValue(itemId, out count) ? count : 0;
        }
    }

    /// <summary>Estado completo de uma batalha. Só o BattleEngine altera; UI e IA apenas leem.</summary>
    public sealed class BattleState
    {
        public readonly Side[] Sides;
        public int Round = 1;
        public ArenaId Arena;
        public int RoundsUntilArenaDraw;

        public BattleState(Side side0, Side side1, ArenaId arena, int roundsUntilArenaDraw)
        {
            Sides = new[] { side0, side1 };
            Arena = arena;
            RoundsUntilArenaDraw = roundsUntilArenaDraw;
        }
    }
}
