using System.Collections.Generic;
using System.Text;
using ComboTactics.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ComboTactics.UI
{
    /// <summary>Números do QTE (SYSTEM-DESIGN §3.2). O núcleo só recebe o resultado; o timing é medido aqui.</summary>
    public static class QteConfig
    {
        /// <summary>Clique a até 60 ms do alvo: contra-ataque.</summary>
        public static float PerfectSeconds = 0.06f;
        /// <summary>Clique a até 150 ms do alvo: desvio.</summary>
        public static float GoodSeconds = 0.15f;
        /// <summary>Posição do alvo na barra, sorteada a cada golpe.</summary>
        public static float TargetMin = 0.55f;
        public static float TargetMax = 0.85f;
        /// <summary>Espera antes de o marcador partir, sorteada para o clique não virar ritmo decorado.</summary>
        public static float TelegraphMin = 0.45f;
        public static float TelegraphMax = 1.10f;

        public static ReactionResult Classify(float errorSeconds)
        {
            float error = Mathf.Abs(errorSeconds);
            if (error <= PerfectSeconds) return ReactionResult.Counter;
            return error <= GoodSeconds ? ReactionResult.Dodge : ReactionResult.Miss;
        }
    }

    /// <summary>Lutador ativo na arena: forma colorida, placa de nome, barra de HP e efeitos.</summary>
    public sealed class FighterWidget
    {
        private const float BodySize = 190f;
        private const float PlateWidth = 290f;

        private readonly RectTransform _body;
        private readonly RawImage _sprite;
        private readonly Text _letter;
        private readonly Text _name;
        private readonly Text _sub;
        private readonly Text _hpText;
        private readonly Text _effects;
        private readonly UiBar _hp;
        private readonly Vector2 _home;
        private int _maxHp = 1;

        public float CenterX { get; private set; }
        public float ShownHp { get; private set; }

        public FighterWidget(Transform parent, float centerX, float centerY)
        {
            CenterX = centerX;
            float left = centerX - PlateWidth * 0.5f;
            var plate = Ui.Box(parent, "Plate", left, centerY - 196f, PlateWidth, 94f, Theme.Panel);
            _name = Ui.Label(plate.transform, "", 8, 2, PlateWidth - 16, 26, 19, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            _sub = Ui.Label(plate.transform, "", 8, 26, PlateWidth - 16, 18, 13, Theme.Muted);
            _hp = new UiBar(plate.transform, 8, 46, PlateWidth - 16, 22, Theme.Good);
            _hpText = Ui.Label(plate.transform, "", 8, 46, PlateWidth - 16, 22, 14, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            _effects = Ui.Label(plate.transform, "", 8, 70, PlateWidth - 16, 20, 13, Theme.Gold);

            _body = Ui.Node(parent, "Body", 0, 0, BodySize, BodySize);
            Ui.PlaceCentered(_body, centerX, centerY, BodySize, BodySize);
            _home = _body.anchoredPosition;
            _sprite = Ui.Picture(_body, "Sprite", null, 0, 0, BodySize, BodySize);
            _letter = Ui.Label(_body, "", 0, 0, BodySize, BodySize, 64, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
        }

        public void Show(BattleSnapshot.FighterView fighter, BattleCatalog catalog)
        {
            var spec = fighter.Spec;
            _maxHp = spec.MaxHp;
            _sprite.texture = Art.Fighter(spec.Element, spec.Class);
            _letter.text = Theme.ClassLetter(spec.Class);
            _name.text = spec.Name;
            _sub.text = Theme.ElementName(spec.Element) + " · " + Theme.ClassName(spec.Class) + " · ATQ " + spec.Attack + " DEF " + spec.Defense + " VEL " + spec.Speed;
            _effects.text = BattleText.Effects(fighter.Effects, catalog);
            SetHp(fighter.Hp);
            SetAlpha(fighter.Fainted ? 0.25f : 1f);
        }

        public void SetHp(float hp)
        {
            ShownHp = hp;
            float fraction = Mathf.Clamp01(hp / _maxHp);
            _hp.Set(fraction);
            _hp.SetColor(Theme.HpColor(fraction));
            _hpText.text = "HP " + Mathf.RoundToInt(hp) + " / " + _maxHp;
        }

        public void SetEffects(string text) { _effects.text = text; }

        public void SetOffset(Vector2 offset) { _body.anchoredPosition = _home + offset; }
        public void SetScale(float scale) { _body.localScale = new Vector3(scale, scale, 1f); }
        public void SetTint(Color color) { color.a = _sprite.color.a; _sprite.color = color; }

        public void SetAlpha(float alpha)
        {
            var color = _sprite.color;
            color.a = alpha;
            _sprite.color = color;
            var letter = _letter.color;
            letter.a = alpha;
            _letter.color = letter;
        }

        public void ResetPose()
        {
            SetOffset(Vector2.zero);
            SetScale(1f);
            SetTint(Color.white);
        }
    }

    /// <summary>Painel de um jogador: barra de stamina e os três lutadores (ativo destacado, banco com HP e status).</summary>
    public sealed class TeamPanel
    {
        private const float ChipWidth = 136f;

        private readonly Text _title;
        private readonly UiBar _stamina;
        private readonly Text _staminaText;
        private readonly Image[] _chips;
        private readonly Text[] _names;
        private readonly UiBar[] _hp;
        private readonly Text[] _effects;

        public TeamPanel(Transform parent, float x, float y, string title, int teamSize)
        {
            var panel = Ui.Box(parent, "TeamPanel", x, y, 440, 120, Theme.Panel);
            _title = Ui.Label(panel.transform, title, 10, 4, 420, 22, 16, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            _stamina = new UiBar(panel.transform, 10, 28, 420, 22, Theme.Stamina);
            _staminaText = Ui.Label(panel.transform, "", 10, 28, 420, 22, 13, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);

            _chips = new Image[teamSize];
            _names = new Text[teamSize];
            _hp = new UiBar[teamSize];
            _effects = new Text[teamSize];
            for (int i = 0; i < teamSize; i++)
            {
                _chips[i] = Ui.Box(panel.transform, "Chip" + i, 10 + i * (ChipWidth + 6), 56, ChipWidth, 58, Theme.PanelLight);
                var t = _chips[i].transform;
                _names[i] = Ui.Label(t, "", 4, 2, ChipWidth - 8, 18, 12, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                _hp[i] = new UiBar(t, 6, 22, ChipWidth - 12, 12, Theme.Good);
                _effects[i] = Ui.Label(t, "", 4, 36, ChipWidth - 8, 18, 11, Theme.Gold);
            }
        }

        public void Refresh(BattleSnapshot.SideView side, BattleCatalog catalog, int staminaMax)
        {
            _stamina.Set(side.Stamina / (float)staminaMax);
            bool ready = side.Stamina >= staminaMax;
            _staminaText.text = ready ? "ESPECIAL PRONTO!" : "Stamina " + side.Stamina + " / " + staminaMax;
            _stamina.SetColor(ready ? Theme.ButtonPrimary : Theme.Stamina);

            for (int i = 0; i < _chips.Length; i++)
            {
                var fighter = side.Team[i];
                bool active = i == side.ActiveIndex;
                float fraction = fighter.Hp / (float)fighter.Spec.MaxHp;
                _names[i].text = fighter.Spec.Name;
                _names[i].color = fighter.Fainted ? Theme.Muted : Theme.Text;
                _hp[i].Set(fraction);
                _hp[i].SetColor(Theme.HpColor(fraction));
                _effects[i].text = fighter.Fainted ? "derrotado" : BattleText.Effects(fighter.Effects, catalog);
                _effects[i].color = fighter.Fainted ? Theme.Bad : Theme.Gold;
                Color color = fighter.Fainted ? new Color(0.10f, 0.10f, 0.12f, 0.9f) : active ? Theme.ElementColor(fighter.Spec.Element) * 0.6f : Theme.PanelLight;
                color.a = 0.95f;
                _chips[i].color = color;
            }
        }
    }

    /// <summary>Textos da batalha em português.</summary>
    public static class BattleText
    {
        public static string Effects(Dictionary<string, int> effects, BattleCatalog catalog)
        {
            if (effects.Count == 0) return "";
            var sb = new StringBuilder();
            foreach (var pair in effects)
            {
                if (sb.Length > 0) sb.Append(" · ");
                sb.Append(catalog.Effects[pair.Key].Name).Append(" (").Append(pair.Value).Append(')');
            }
            return sb.ToString();
        }

        public static string Reaction(ReactionResult result)
        {
            switch (result)
            {
                case ReactionResult.Counter: return "CONTRA-ATAQUE!";
                case ReactionResult.Dodge: return "DESVIO!";
                default: return "ERROU!";
            }
        }
    }
}
