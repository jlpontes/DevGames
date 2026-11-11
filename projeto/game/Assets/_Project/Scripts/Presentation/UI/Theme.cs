using ComboTactics.Core;
using UnityEngine;

namespace ComboTactics.UI
{
    /// <summary>Cores e textos de interface num só lugar. Trocar a identidade visual começa aqui.</summary>
    public static class Theme
    {
        public const float Width = 1280f;
        public const float Height = 720f;

        public static readonly Color Panel = new Color(0.07f, 0.06f, 0.10f, 0.88f);
        public static readonly Color PanelSolid = new Color(0.09f, 0.08f, 0.12f, 0.96f);
        public static readonly Color PanelLight = new Color(0.14f, 0.13f, 0.19f, 0.92f);
        public static readonly Color BorderGold = new Color(0.76f, 0.56f, 0.24f, 0.88f);
        public static readonly Color BorderSubtle = new Color(0.36f, 0.32f, 0.28f, 0.75f);
        public static readonly Color Button = new Color(0.18f, 0.17f, 0.24f, 0.96f);
        public static readonly Color ButtonPrimary = new Color(0.78f, 0.38f, 0.10f, 1f);
        public static readonly Color ButtonDanger = new Color(0.58f, 0.16f, 0.16f, 1f);
        public static readonly Color Text = new Color(0.96f, 0.94f, 0.89f, 1f);
        public static readonly Color Muted = new Color(0.74f, 0.71f, 0.66f, 1f);
        public static readonly Color Good = new Color(0.34f, 0.84f, 0.44f, 1f);
        public static readonly Color Bad = new Color(0.94f, 0.34f, 0.28f, 1f);
        public static readonly Color Gold = new Color(0.98f, 0.80f, 0.28f, 1f);
        public static readonly Color Stamina = new Color(0.96f, 0.74f, 0.18f, 1f);
        public static readonly Color BarBack = new Color(0.03f, 0.03f, 0.05f, 0.82f);

        public static Color ElementColor(Element element)
        {
            switch (element)
            {
                case Element.Fire: return new Color(0.92f, 0.38f, 0.16f, 1f);
                case Element.Water: return new Color(0.20f, 0.56f, 0.94f, 1f);
                case Element.Plant: return new Color(0.30f, 0.72f, 0.33f, 1f);
                default: return new Color(0.62f, 0.46f, 0.86f, 1f);
            }
        }

        public static Color HpColor(float fraction)
        {
            if (fraction > 0.5f) return Good;
            return fraction > 0.2f ? Gold : Bad;
        }

        public static string ElementName(Element element)
        {
            switch (element)
            {
                case Element.Fire: return "Fogo";
                case Element.Water: return "Água";
                case Element.Plant: return "Planta";
                default: return "Neutro";
            }
        }

        public static string ClassName(CharClass charClass)
        {
            switch (charClass)
            {
                case CharClass.Warrior: return "Guerreiro";
                case CharClass.Mage: return "Mago";
                case CharClass.Tank: return "Tanque";
                default: return "Chefe";
            }
        }

        public static string ClassLetter(CharClass charClass)
        {
            switch (charClass)
            {
                case CharClass.Warrior: return "G";
                case CharClass.Mage: return "M";
                case CharClass.Tank: return "T";
                default: return "B";
            }
        }

        public static string StatName(Stat stat)
        {
            switch (stat)
            {
                case Stat.Hp: return "HP";
                case Stat.Attack: return "Ataque";
                case Stat.Defense: return "Defesa";
                default: return "Velocidade";
            }
        }

        /// <summary>Quem a arena favorece e prejudica, em uma linha (GDD §5.6).</summary>
        public static string ArenaEffect(ArenaSpec arena, BattleRules rules)
        {
            if (arena.UsesSpeedBands)
                return "Vel. < " + rules.MoonSlowBelow + ": +10% de dano  ·  Vel. ≥ " + rules.MoonFastFrom + ": −10%";
            return ElementName(arena.Favoured) + ": +10% de dano  ·  " + ElementName(arena.Penalised) + ": −10%";
        }

        public static string SkillHint(SkillSpec skill, BattleCatalog catalog)
        {
            if (skill.EffectId == null) return "";
            string name = catalog.Effects[skill.EffectId].Name;
            string target = skill.EffectTarget == EffectTarget.Self ? "em si" : "no alvo";
            return skill.EffectChance >= 1 ? name + " " + target : Mathf.RoundToInt((float)skill.EffectChance * 100) + "% " + name;
        }
    }
}
