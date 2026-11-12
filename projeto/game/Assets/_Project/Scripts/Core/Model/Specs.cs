using System;
using System.Collections.Generic;

namespace ComboTactics.Core
{
    /// <summary>Definição imutável de um efeito de 3 turnos (GDD §5.2).</summary>
    public sealed class EffectSpec
    {
        public readonly string Id;
        public readonly string Name;
        public readonly EffectKind Kind;
        /// <summary>
        /// DoT: fração do Ataque de quem aplicou, por turno. HoT: fração do HP máximo do alvo, por turno.
        /// Modificadores: fração somada ao atributo (0,10 = +10%).
        /// </summary>
        public readonly double Value;
        public readonly int Turns;
        /// <summary>Efeitos negativos são removidos pelo Remédio.</summary>
        public readonly bool IsNegative;

        public EffectSpec(string id, string name, EffectKind kind, double value, int turns, bool isNegative)
        {
            Id = id; Name = name; Kind = kind; Value = value; Turns = turns; IsNegative = isNegative;
        }
    }

    /// <summary>Uma habilidade: dano (poder 100 = 100% do Ataque) e um efeito opcional.</summary>
    public sealed class SkillSpec
    {
        public readonly string Id;
        public readonly string Name;
        public readonly int Power;
        public readonly bool IsSpecial;
        public readonly string EffectId;
        public readonly EffectTarget EffectTarget;
        /// <summary>Chance de 0 a 1 de aplicar o efeito quando o golpe acerta.</summary>
        public readonly double EffectChance;

        public SkillSpec(string id, string name, int power, bool isSpecial,
            string effectId = null, EffectTarget effectTarget = EffectTarget.Enemy, double effectChance = 0)
        {
            Id = id; Name = name; Power = power; IsSpecial = isSpecial;
            EffectId = effectId; EffectTarget = effectTarget; EffectChance = effectChance;
        }
    }

    /// <summary>
    /// Segunda fase de um lutador (o Guardião do Núcleo, por exemplo): ao cair até HpFraction do HP máximo,
    /// recebe os efeitos listados e, se FillsStamina, a barra de stamina do lado enche. Acontece uma vez por batalha.
    /// </summary>
    public sealed class PhaseShiftSpec
    {
        public readonly double HpFraction;
        public readonly IReadOnlyList<string> EffectIds;
        public readonly bool FillsStamina;
        public readonly string Title;

        public PhaseShiftSpec(double hpFraction, IReadOnlyList<string> effectIds, bool fillsStamina, string title)
        {
            if (hpFraction <= 0 || hpFraction >= 1) throw new ArgumentException("HpFraction precisa ficar entre 0 e 1.");
            HpFraction = hpFraction;
            EffectIds = effectIds ?? new string[0];
            FillsStamina = fillsStamina;
            Title = title;
        }
    }

    /// <summary>
    /// Lutador pronto para a batalha: atributos já com evoluções ou bônus de fase aplicados.
    /// Skills[0] e Skills[1] são as habilidades normais; a última é o especial.
    /// </summary>
    public sealed class FighterSpec
    {
        public readonly string Id;
        public readonly string Name;
        public readonly Element Element;
        public readonly CharClass Class;
        public readonly int MaxHp;
        public readonly int Attack;
        public readonly int Defense;
        public readonly int Speed;
        public readonly IReadOnlyList<SkillSpec> Skills;
        /// <summary>Segunda fase opcional (null para os lutadores comuns).</summary>
        public readonly PhaseShiftSpec PhaseShift;

        public FighterSpec(string id, string name, Element element, CharClass charClass,
            int maxHp, int attack, int defense, int speed, IReadOnlyList<SkillSpec> skills, PhaseShiftSpec phaseShift = null)
        {
            if (skills == null || skills.Count == 0) throw new ArgumentException("Lutador sem habilidades: " + id);
            if (skills[0].IsSpecial) throw new ArgumentException("A primeira habilidade precisa ser normal: " + id);
            Id = id; Name = name; Element = element; Class = charClass;
            MaxHp = maxHp; Attack = attack; Defense = defense; Speed = speed; Skills = skills; PhaseShift = phaseShift;
        }
    }

    public sealed class ItemSpec
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Description;
        public readonly ItemKind Kind;
        /// <summary>Heal: fração do HP máximo curada.</summary>
        public readonly double Value;
        /// <summary>ApplyEffect: efeito aplicado no lutador ativo.</summary>
        public readonly string EffectId;
        public readonly int Price;

        public ItemSpec(string id, string name, string description, ItemKind kind, double value, string effectId, int price)
        {
            Id = id; Name = name; Description = description; Kind = kind; Value = value; EffectId = effectId; Price = price;
        }
    }

    /// <summary>Arena do GDD §5.6. A Lua usa faixas de Velocidade em vez de elemento (R5).</summary>
    public sealed class ArenaSpec
    {
        public readonly ArenaId Id;
        public readonly string Name;
        public readonly bool UsesSpeedBands;
        public readonly Element Favoured;
        public readonly Element Penalised;

        public ArenaSpec(ArenaId id, string name, Element favoured, Element penalised, bool usesSpeedBands = false)
        {
            Id = id; Name = name; Favoured = favoured; Penalised = penalised; UsesSpeedBands = usesSpeedBands;
        }
    }

    /// <summary>Tabelas de consulta que o motor precisa durante a batalha.</summary>
    public sealed class BattleCatalog
    {
        public readonly IReadOnlyDictionary<string, EffectSpec> Effects;
        public readonly IReadOnlyDictionary<string, ItemSpec> Items;
        public readonly IReadOnlyDictionary<ArenaId, ArenaSpec> Arenas;

        public BattleCatalog(IReadOnlyDictionary<string, EffectSpec> effects,
            IReadOnlyDictionary<string, ItemSpec> items, IReadOnlyDictionary<ArenaId, ArenaSpec> arenas)
        {
            Effects = effects; Items = items; Arenas = arenas;
        }
    }
}
