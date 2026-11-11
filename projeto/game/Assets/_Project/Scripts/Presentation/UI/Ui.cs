using System;
using ComboTactics.Game;
using UnityEngine;
using UnityEngine.UI;

namespace ComboTactics.UI
{
    /// <summary>
    /// Fábrica dos elementos de interface (uGUI montado por código). As posições são em pixels de um palco
    /// de 1280×720, medidas a partir do canto superior esquerdo do elemento pai.
    /// </summary>
    public static class Ui
    {
        private static Font _font;

        /// <summary>Chamado quando um clique gera um erro de regra com mensagem para o jogador.</summary>
        public static Action<string> OnPlayerMessage;

        public static Font Font
        {
            get
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 16);
                return _font;
            }
        }

        public static RectTransform Node(Transform parent, string name, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            Place(rt, x, y, w, h);
            return rt;
        }

        public static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        /// <summary>Posiciona pelo centro, com o pivô no meio: bom para o que gira, cresce ou treme.</summary>
        public static void PlaceCentered(RectTransform rt, float centerX, float centerY, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(centerX, -centerY);
            rt.sizeDelta = new Vector2(w, h);
        }

        public static Image Box(Transform parent, string name, float x, float y, float w, float h, Color color)
        {
            var rt = Node(parent, name, x, y, w, h);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Painel com moldura metálica/elemental e brilho interno no topo, integrado à estética de arena.</summary>
        public static RectTransform Frame(Transform parent, string name, float x, float y, float w, float h, Color fill, Color border, float thickness = 2f)
        {
            var outer = Box(parent, name + "Border", x, y, w, h, border);
            var inner = Box(outer.transform, name, thickness, thickness, w - thickness * 2f, h - thickness * 2f, fill);
            Box(inner.transform, "Highlight", 0f, 0f, w - thickness * 2f, 1.5f, new Color(1f, 0.95f, 0.85f, 0.10f));
            return inner.rectTransform;
        }

        /// <summary>Fundo atmosférico usando a arte do torneio escurecida, evitando telas chapadas.</summary>
        public static void Backdrop(Transform parent, Texture customArt = null, float brightness = 0.24f)
        {
            Box(parent, "Base", 0, 0, Theme.Width, Theme.Height, new Color(0.05f, 0.04f, 0.07f, 1f));
            var pic = Picture(parent, "BackdropArt", customArt != null ? customArt : Art.Menu, -20, 0, 1320, 720);
            pic.color = new Color(brightness, brightness * 0.95f, brightness * 1.05f, 1f);
            Box(parent, "TopVignette", 0, 0, Theme.Width, 84, new Color(0.04f, 0.03f, 0.06f, 0.72f));
            Box(parent, "BottomBar", 0, 78, Theme.Width, 2, Theme.BorderSubtle);
        }

        public static RawImage Picture(Transform parent, string name, Texture texture, float x, float y, float w, float h)
        {
            var rt = Node(parent, name, x, y, w, h);
            var image = rt.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            return image;
        }

        public static Text Label(Transform parent, string text, float x, float y, float w, float h, int size, Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal, bool outline = false)
        {
            var rt = Node(parent, "Label", x, y, w, h);
            var label = rt.gameObject.AddComponent<Text>();
            label.font = Font;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = anchor;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.supportRichText = true;
            label.raycastTarget = false;
            label.text = text;
            if (outline)
            {
                var effect = rt.gameObject.AddComponent<Outline>();
                effect.effectColor = new Color(0f, 0f, 0f, 0.9f);
                effect.effectDistance = new Vector2(1.5f, -1.5f);
            }
            return label;
        }

        public static Button Button(Transform parent, string label, float x, float y, float w, float h, Action onClick,
            Color color, int fontSize = 20)
        {
            var image = Box(parent, "Button", x, y, w, h, color);
            image.raycastTarget = true;

            bool isPrimary = Mathf.Abs(color.r - Theme.ButtonPrimary.r) < 0.08f && Mathf.Abs(color.g - Theme.ButtonPrimary.g) < 0.08f;
            Color rim = isPrimary ? Theme.Gold : new Color(0.58f, 0.50f, 0.38f, 0.55f);
            var outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = rim;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            Box(image.transform, "TopGloss", 2f, 2f, Mathf.Max(0f, w - 4f), Mathf.Min(3f, h * 0.12f), new Color(1f, 0.95f, 0.85f, isPrimary ? 0.22f : 0.08f));

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.10f, 1.05f, 1f);
            colors.pressedColor = new Color(0.75f, 0.72f, 0.68f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.42f, 0.40f, 0.44f, 0.65f);
            colors.fadeDuration = 0.06f;
            button.colors = colors;
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            if (onClick != null) button.onClick.AddListener(() => Click(onClick));
            if (!string.IsNullOrEmpty(label))
                Label(image.transform, label, 6f, 0f, w - 12f, h, fontSize, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            return button;
        }

        /// <summary>Erros de regra viram um aviso para o jogador; o resto segue para o relatório de erros.</summary>
        private static void Click(Action onClick)
        {
            try
            {
                onClick();
            }
            catch (GameException e)
            {
                if (OnPlayerMessage != null) OnPlayerMessage(e.Message);
            }
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
        }
    }

    /// <summary>Barra horizontal (HP, stamina) preenchida da esquerda para a direita.</summary>
    public sealed class UiBar
    {
        private readonly RectTransform _fill;
        private readonly Image _fillImage;
        private readonly float _width;
        private readonly float _height;

        public float Fraction { get; private set; }

        public UiBar(Transform parent, float x, float y, float w, float h, Color fillColor)
        {
            var back = Ui.Box(parent, "Bar", x, y, w, h, Theme.BarBack);
            var rim = back.gameObject.AddComponent<Outline>();
            rim.effectColor = new Color(0.45f, 0.38f, 0.28f, 0.65f);
            rim.effectDistance = new Vector2(1f, -1f);
            _width = w - 4f;
            _height = h - 4f;
            _fillImage = Ui.Box(back.transform, "Fill", 2f, 2f, _width, _height, fillColor);
            _fill = _fillImage.rectTransform;
            Fraction = 1f;
        }

        public void Set(float fraction)
        {
            Fraction = Mathf.Clamp01(fraction);
            _fill.sizeDelta = new Vector2(_width * Fraction, _height);
        }

        public void SetColor(Color color) { _fillImage.color = color; }
    }
}
