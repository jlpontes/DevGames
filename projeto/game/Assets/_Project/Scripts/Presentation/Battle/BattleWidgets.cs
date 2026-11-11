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
        /// <summary>Clique a até 30 ms do alvo: contra-ataque.</summary>
        public static float PerfectSeconds = 0.03f;
        /// <summary>Clique a até 80 ms do alvo: desvio.</summary>
        public static float GoodSeconds = 0.08f;
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

    /// <summary>Lutador ativo na arena: sombra no chão, sprite e pedestal compacto sob os pés.</summary>
    public sealed class FighterWidget
    {
        private const float BodySize = 200f;
        private const float PlateWidth = 268f;

        private readonly RectTransform _body;
        private readonly RawImage _sprite;
        private readonly Image _plateBorder;
        private readonly Text _letter;
        private readonly Text _name;
        private readonly Text _sub;
        private readonly Text _hpText;
        private readonly Text _effects;
        private readonly UiBar _hp;
        private readonly Vector2 _home;
        private int _maxHp = 1;

        public float CenterX { get; private set; }
        public float CenterY { get; private set; }
        public float ShownHp { get; private set; }

        public FighterWidget(Transform parent, float centerX, float centerY)
        {
            CenterX = centerX;
            CenterY = centerY;

            // Sombra elíptica projetada no piso da arena para ancorar o personagem no cenário.
            var shadow = Ui.Picture(parent, "Shadow", Art.VfxOrb, 0, 0, 168f, 34f);
            shadow.color = new Color(0f, 0f, 0f, 0.48f);
            Ui.PlaceCentered(shadow.rectTransform, centerX, centerY + 84f, 168f, 34f);

            _body = Ui.Node(parent, "Body", 0, 0, BodySize, BodySize);
            Ui.PlaceCentered(_body, centerX, centerY, BodySize, BodySize);
            _home = _body.anchoredPosition;
            _sprite = Ui.Picture(_body, "Sprite", null, 0, 0, BodySize, BodySize);
            // O lutador da direita é espelhado para os dois ficarem de frente um para o outro.
            if (centerX > Theme.Width * 0.5f) _sprite.uvRect = new Rect(1f, 0f, -1f, 1f);
            _letter = Ui.Label(_body, "", 0, 0, BodySize, BodySize, 64, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);

            // Pedestal de status posicionado sob os pés do lutador, deixando o céu da arena livre.
            float left = centerX - PlateWidth * 0.5f;
            float plateY = centerY + 94f;
            var borderBox = Ui.Box(parent, "PlateFrame", left, plateY, PlateWidth, 68f, Theme.BorderGold);
            _plateBorder = borderBox;
            var plate = Ui.Box(borderBox.transform, "Plate", 2f, 2f, PlateWidth - 4f, 64f, Theme.Panel);
            Ui.Box(plate.transform, "Gloss", 0f, 0f, PlateWidth - 4f, 2f, new Color(1f, 0.92f, 0.72f, 0.16f));

            _name = Ui.Label(plate.transform, "", 8, 2, 152, 20, 15, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            _sub = Ui.Label(plate.transform, "", 160, 3, PlateWidth - 170, 18, 11, Theme.Muted, TextAnchor.MiddleRight, FontStyle.Bold);
            _hp = new UiBar(plate.transform, 8, 24, PlateWidth - 20, 18, Theme.Good);
            _hpText = Ui.Label(plate.transform, "", 8, 24, PlateWidth - 20, 18, 12, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            _effects = Ui.Label(plate.transform, "", 8, 44, PlateWidth - 20, 18, 12, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        public void Show(BattleSnapshot.FighterView fighter, BattleCatalog catalog)
        {
            var spec = fighter.Spec;
            _maxHp = spec.MaxHp;
            _sprite.texture = Art.Fighter(spec.Id, spec.Element, spec.Class);
            _letter.text = Art.HasSprite(spec.Id) ? "" : Theme.ClassLetter(spec.Class);
            _name.text = spec.Name;
            _sub.text = Theme.ElementName(spec.Element) + " · " + Theme.ClassName(spec.Class);
            _sub.color = Theme.ElementColor(spec.Element);
            _plateBorder.color = Theme.ElementColor(spec.Element);
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
        public void SetRotation(float degrees) { _body.localRotation = Quaternion.Euler(0f, 0f, degrees); }
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
            SetRotation(0f);
            SetTint(Color.white);
        }
    }

    /// <summary>Painel de um jogador: barra de stamina e os três lutadores (ativo destacado, banco com HP e status).</summary>
    public sealed class TeamPanel
    {
        private const float ChipWidth = 132f;

        private readonly Text _title;
        private readonly UiBar _stamina;
        private readonly Text _staminaText;
        private readonly Image[] _chips;
        private readonly RawImage[] _icons;
        private readonly Text[] _names;
        private readonly UiBar[] _hp;
        private readonly Text[] _effects;

        public TeamPanel(Transform parent, float x, float y, string title, int teamSize)
        {
            var panel = Ui.Frame(parent, "TeamPanel", x, y, 428, 104, Theme.Panel, Theme.BorderGold, 2f);
            _title = Ui.Label(panel, title, 10, 4, 184, 24, 16, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            _stamina = new UiBar(panel, 198, 6, 218, 20, Theme.Stamina);
            _staminaText = Ui.Label(panel, "", 198, 6, 218, 20, 12, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);

            _chips = new Image[teamSize];
            _icons = new RawImage[teamSize];
            _names = new Text[teamSize];
            _hp = new UiBar[teamSize];
            _effects = new Text[teamSize];
            for (int i = 0; i < teamSize; i++)
            {
                float chipX = 10 + i * (ChipWidth + 6);
                var border = Ui.Box(panel, "ChipBorder" + i, chipX, 32, ChipWidth, 64, Theme.BorderSubtle);
                _chips[i] = Ui.Box(border.transform, "Chip" + i, 1.5f, 1.5f, ChipWidth - 3f, 61f, Theme.PanelLight);
                var t = _chips[i].transform;
                _icons[i] = Ui.Picture(t, "Icon", null, 4, 4, 34, 34);
                _names[i] = Ui.Label(t, "", 42, 3, ChipWidth - 48, 18, 12, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold, true);
                _hp[i] = new UiBar(t, 42, 23, ChipWidth - 50, 12, Theme.Good);
                _effects[i] = Ui.Label(t, "", 4, 41, ChipWidth - 10, 16, 11, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
        }

        public void Refresh(BattleSnapshot.SideView side, BattleCatalog catalog, int staminaMax)
        {
            _stamina.Set(side.Stamina / (float)staminaMax);
            bool ready = side.Stamina >= staminaMax;
            _staminaText.text = ready ? "★ ESPECIAL PRONTO!" : "Stamina " + side.Stamina + " / " + staminaMax;
            _stamina.SetColor(ready ? Theme.ButtonPrimary : Theme.Stamina);

            for (int i = 0; i < _chips.Length; i++)
            {
                var fighter = side.Team[i];
                bool active = i == side.ActiveIndex;
                float fraction = fighter.Hp / (float)fighter.Spec.MaxHp;
                _icons[i].texture = Art.Fighter(fighter.Spec.Id, fighter.Spec.Element, fighter.Spec.Class);
                _icons[i].color = fighter.Fainted ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
                _names[i].text = fighter.Spec.Name;
                _names[i].color = fighter.Fainted ? Theme.Muted : Theme.Text;
                _hp[i].Set(fraction);
                _hp[i].SetColor(Theme.HpColor(fraction));
                string statusText = fighter.Fainted ? "DERROTADO" : BattleText.Effects(fighter.Effects, catalog);
                if (!fighter.Fainted && statusText.Length == 0 && active) statusText = "EM COMBATE";
                _effects[i].text = statusText;
                _effects[i].color = fighter.Fainted ? Theme.Bad : active && fighter.Effects.Count == 0 ? Theme.Good : Theme.Gold;
                Color color = fighter.Fainted
                    ? new Color(0.08f, 0.07f, 0.08f, 0.92f)
                    : active
                        ? Color.Lerp(Theme.PanelLight, Theme.ElementColor(fighter.Spec.Element), 0.38f)
                        : Theme.PanelLight;
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
