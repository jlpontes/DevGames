using ComboTactics.Core;
using UnityEngine;

namespace ComboTactics.UI
{
    /// <summary>Cores e textos de interface num só lugar. Trocar a identidade visual começa aqui.</summary>
    public static class Theme
    {
        public const float Width = 1280f;
        public const float Height = 720f;

        // ── Paleta "Relicário de Bronze e Cristal" (ART-DIRECTION.md §1) ─────────
        public static readonly Color Obsidian = new Color(0.043f, 0.039f, 0.059f, 1f);
        public static readonly Color Panel = new Color(0.075f, 0.067f, 0.090f, 0.90f);
        public static readonly Color PanelSolid = new Color(0.085f, 0.077f, 0.102f, 0.97f);
        public static readonly Color PanelLight = new Color(0.135f, 0.122f, 0.155f, 0.94f);
        public static readonly Color BorderGold = new Color(0.72f, 0.54f, 0.31f, 1f);
        public static readonly Color BorderSubtle = new Color(0.40f, 0.33f, 0.26f, 0.95f);
        public static readonly Color Button = new Color(0.19f, 0.175f, 0.215f, 1f);
        public static readonly Color ButtonPrimary = new Color(0.64f, 0.43f, 0.17f, 1f);
        public static readonly Color ButtonDanger = new Color(0.52f, 0.17f, 0.15f, 1f);
        public static readonly Color Text = new Color(0.95f, 0.92f, 0.86f, 1f);
        public static readonly Color Muted = new Color(0.70f, 0.66f, 0.60f, 1f);
        public static readonly Color Good = new Color(0.42f, 0.86f, 0.52f, 1f);
        public static readonly Color Bad = new Color(0.95f, 0.36f, 0.30f, 1f);
        public static readonly Color Gold = new Color(0.98f, 0.80f, 0.38f, 1f);
        public static readonly Color Stamina = new Color(0.96f, 0.72f, 0.26f, 1f);
        public static readonly Color BarBack = new Color(0.035f, 0.032f, 0.045f, 0.92f);
        public static readonly Color Ghost = new Color(1f, 0.93f, 0.82f, 0.85f);

        // ── Tipografia (ART-DIRECTION.md §3) ─────────────────────────────────────
        public const int FontDisplay = 34;
        public const int FontTitle = 26;
        public const int FontHeading = 18;
        public const int FontBody = 15;
        public const int FontSmall = 13;
        public const int FontCaption = 11;

        // ── Tempos de movimento em segundos (ART-DIRECTION.md §4) ────────────────
        public const float UiFade = 0.20f;
        public const float UiHover = 0.10f;
        public const float HitStopLight = 0.045f;
        public const float HitStopHeavy = 0.09f;
        public const float ShakeMax = 9f;

        /// <summary>Cor do elemento mais clara (núcleo de brilho, texto sobre fundo escuro).</summary>
        public static Color ElementLight(Element element) { return Color.Lerp(ElementColor(element), Color.white, 0.45f); }

        /// <summary>Cor do elemento escurecida para fundos de destaque (nunca chapada em área grande).</summary>
        public static Color ElementDark(Element element, float alpha = 0.95f)
        {
            var c = ElementColor(element);
            return new Color(c.r * 0.26f, c.g * 0.26f, c.b * 0.26f, alpha);
        }

        public static Color WithAlpha(Color color, float alpha) { color.a = alpha; return color; }

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

        public static Color ArenaColor(ArenaId arena)
        {
            switch (arena)
            {
                case ArenaId.Volcano: return new Color(0.95f, 0.40f, 0.14f, 1f);
                case ArenaId.Sea: return new Color(0.20f, 0.66f, 0.96f, 1f);
                case ArenaId.Forest: return new Color(0.30f, 0.78f, 0.38f, 1f);
                default: return new Color(0.68f, 0.52f, 0.96f, 1f);
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
