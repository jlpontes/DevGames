using System;
using System.Text;
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

        /// <summary>Camada acima das telas (dicas, avisos). O App define na inicialização.</summary>
        public static RectTransform OverlayRoot;

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

        /// <summary>
        /// Painel do relicário: moldura de bronze chanfrada (9-slice) com preenchimento de pedra, luz no topo e sombra
        /// na base. Devolve o miolo; <c>.parent</c> é a moldura (a Image dela recebe a cor da borda).
        /// </summary>
        public static RectTransform Frame(Transform parent, string name, float x, float y, float w, float h, Color fill, Color border, float thickness = 2f)
        {
            var outer = Box(parent, name + "Border", x, y, w, h, border);
            outer.sprite = Art.FrameSprite;
            outer.type = Image.Type.Sliced;
            outer.pixelsPerUnitMultiplier = thickness >= 2.5f ? 0.9f : thickness <= 1.5f ? 1.6f : 1.25f;

            var inner = Box(outer.transform, name, thickness, thickness, w - thickness * 2f, h - thickness * 2f, fill);
            if (fill.a > 0.01f)
            {
                inner.sprite = Art.FillSprite;
                inner.type = Image.Type.Sliced;
                float iw = w - thickness * 2f, ih = h - thickness * 2f;
                if (ih >= 28f)
                {
                    var sheen = Picture(inner.transform, "Sheen", Art.GradientUp, 0f, 0f, iw, Mathf.Min(ih * 0.45f, 46f));
                    sheen.uvRect = new Rect(0f, 1f, 1f, -1f);
                    sheen.color = new Color(1f, 0.93f, 0.80f, 0.06f);
                    var shade = Picture(inner.transform, "Shade", Art.GradientUp, 0f, ih - Mathf.Min(ih * 0.4f, 40f), iw, Mathf.Min(ih * 0.4f, 40f));
                    shade.color = new Color(0f, 0f, 0f, 0.22f);
                }
                if (w >= 220f && h >= 90f && thickness >= 2f)
                {
                    // Rebites em losango nos cantos de cima: o fragmento do cristal preso no bronze.
                    Color rivet = Color.Lerp(border, Color.white, 0.25f);
                    var left = Picture(inner.transform, "RivetL", Art.Diamond, 0f, 0f, 9f, 9f);
                    PlaceCentered(left.rectTransform, 4f, 4f, 9f, 9f);
                    left.color = rivet;
                    var right = Picture(inner.transform, "RivetR", Art.Diamond, 0f, 0f, 9f, 9f);
                    PlaceCentered(right.rectTransform, iw - 4f, 4f, 9f, 9f);
                    right.color = rivet;
                }
            }
            return inner.rectTransform;
        }

        /// <summary>Sombra macia projetada sob um painel grande (desenhe antes do painel).</summary>
        public static Image Shadow(Transform parent, float x, float y, float w, float h, float alpha = 0.55f)
        {
            var shadow = Box(parent, "DropShadow", x - 10f, y - 4f, w + 20f, h + 20f, new Color(0f, 0f, 0f, alpha));
            shadow.sprite = Art.GlowSprite;
            shadow.type = Image.Type.Sliced;
            return shadow;
        }

        /// <summary>Fundo atmosférico usando a arte do torneio ou de uma arena com movimento suave e fagulhas de ambiente.</summary>
        public static void Backdrop(Transform parent, Texture customArt = null, float brightness = 0.26f)
        {
            Box(parent, "Base", 0, 0, Theme.Width, Theme.Height, Theme.Obsidian);
            var pic = Picture(parent, "BackdropArt", customArt != null ? customArt : Art.Menu, -24, -14, 1328, 748);
            pic.color = new Color(brightness, brightness * 0.95f, brightness * 1.05f, 1f);
            AddAmbientSparks(parent, pic.rectTransform, new Color(1f, 0.78f, 0.36f, 0.45f), 14);
            var top = Picture(parent, "TopVignette", Art.GradientUp, 0, 0, Theme.Width, 150);
            top.uvRect = new Rect(0f, 1f, 1f, -1f);
            top.color = Theme.WithAlpha(Theme.Obsidian, 0.85f);
            var bottom = Picture(parent, "BottomVignette", Art.GradientUp, 0, Theme.Height - 190, Theme.Width, 190);
            bottom.color = Theme.WithAlpha(Theme.Obsidian, 0.80f);
        }

        /// <summary>Adiciona partículas flutuantes e leve respiração de câmera a um fundo de menu.</summary>
        public static void AddAmbientSparks(Transform parent, RectTransform driftingArt, Color sparkColor, int count = 16)
        {
            var container = Node(parent, "AmbientSparks", 0, 0, Theme.Width, Theme.Height);
            var anim = container.gameObject.AddComponent<AmbientBackdropAnimator>();
            anim.Initialize(driftingArt, sparkColor, count);
        }

        public static RawImage Picture(Transform parent, string name, Texture texture, float x, float y, float w, float h)
        {
            var rt = Node(parent, name, x, y, w, h);
            var image = rt.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>
        /// Arte de arena (16:9) numa caixa de outra proporção: recorta o centro em vez de esticar, como "cover" no CSS.
        /// </summary>
        public static RawImage ArenaPicture(Transform parent, string name, ComboTactics.Core.ArenaId arena, float x, float y, float w, float h)
        {
            var image = Picture(parent, name, Art.Arena(arena), x, y, w, h);
            Cover(image, w, h, 16f / 9f);
            return image;
        }

        public static void Cover(RawImage image, float w, float h, float textureAspect)
        {
            float boxAspect = w / Mathf.Max(1f, h);
            if (boxAspect > textureAspect)
            {
                float v = textureAspect / boxAspect;
                image.uvRect = new Rect(0f, (1f - v) * 0.5f, 1f, v);
            }
            else
            {
                float u = boxAspect / textureAspect;
                image.uvRect = new Rect((1f - u) * 0.5f, 0f, u, 1f);
            }
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
            label.text = Safe(text);
            if (outline)
            {
                var effect = rt.gameObject.AddComponent<Outline>();
                effect.effectColor = new Color(0f, 0f, 0f, 0.9f);
                effect.effectDistance = new Vector2(1.5f, -1.5f);
            }
            return label;
        }

        /// <summary>
        /// Botão de placa forjada. A cor escolhe o estilo: <see cref="Theme.ButtonPrimary"/> ganha aro dourado,
        /// <see cref="Theme.ButtonDanger"/> fica em brasa, transparente vira área de clique invisível; o resto é ferro tingido.
        /// Hover, clique, desabilitado e selecionado ficam no <see cref="ButtonFx"/>.
        /// </summary>
        public static Button Button(Transform parent, string label, float x, float y, float w, float h, Action onClick,
            Color color, int fontSize = 20)
        {
            bool invisible = color.a < 0.01f;
            bool isPrimary = Near(color, Theme.ButtonPrimary);
            var image = Box(parent, "Button", x, y, w, h, color);
            image.raycastTarget = true;

            Image glow = null;
            if (!invisible)
            {
                image.sprite = Art.ButtonSprite;
                image.type = Image.Type.Sliced;
                glow = Box(image.transform, "Glow", -7f, -7f, w + 14f, h + 14f, Theme.WithAlpha(isPrimary ? Theme.Gold : new Color(1f, 0.86f, 0.58f), 0f));
                glow.sprite = Art.GlowSprite;
                glow.type = Image.Type.Sliced;
                var rim = Box(image.transform, "Rim", 0f, 0f, w, h, isPrimary ? Theme.WithAlpha(Theme.Gold, 0.95f) : new Color(0.62f, 0.50f, 0.36f, 0.75f));
                rim.sprite = Art.FrameSprite;
                rim.type = Image.Type.Sliced;
                rim.pixelsPerUnitMultiplier = 1.6f;
            }

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.10f, 1.08f, 1.04f, 1f);
            colors.pressedColor = new Color(0.82f, 0.80f, 0.76f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.46f, 0.44f, 0.48f, 0.72f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            if (onClick != null) button.onClick.AddListener(() => Click(onClick));
            Text text = null;
            if (!string.IsNullOrEmpty(label))
            {
                text = Label(image.transform, label, 8f, 0f, w - 16f, h, fontSize, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                var drop = text.gameObject.AddComponent<UnityEngine.UI.Shadow>();
                drop.effectColor = new Color(0f, 0f, 0f, 0.85f);
                drop.effectDistance = new Vector2(1f, -1.5f);
            }
            if (!invisible) image.gameObject.AddComponent<ButtonFx>().Setup(button, glow, text, isPrimary ? 0.85f : 0.55f);
            return button;
        }

        /// <summary>Marca um botão como selecionado (brilho fixo).</summary>
        public static void MarkSelected(Button button, bool selected = true)
        {
            var fx = button.GetComponent<ButtonFx>();
            if (fx != null) fx.Selected = selected;
        }

        /// <summary>Dica que aparece ao deixar o mouse sobre o elemento (precisa receber raycast).</summary>
        public static void Tooltip(Component target, string title, string body)
        {
            var trigger = target.gameObject.GetComponent<TooltipTrigger>() ?? target.gameObject.AddComponent<TooltipTrigger>();
            trigger.Title = Safe(title);
            trigger.Body = Safe(body);
        }

        private static bool Near(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.06f && Mathf.Abs(a.g - b.g) < 0.06f && Mathf.Abs(a.b - b.b) < 0.06f;
        }

        /// <summary>
        /// A fonte embutida (a única que funciona no WebGL) não tem emoji nem vários símbolos. Troca os que aparecem
        /// nos textos do jogo por equivalentes da própria fonte e remove o resto, para nunca sobrar um quadrado vazio.
        /// </summary>
        public static string Safe(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            StringBuilder sb = null;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                string swap = null;
                bool drop = false;
                switch (c)
                {
                    case '\u2605': case '\u2726': case '\u2727': case '\u2B50': swap = "\u2666"; break; // ★ ✦ ✧ ⭐ → ♦
                    case '\u263E': case '\u263D': swap = "\u25CA"; break;                                   // ☾ → ◊
                    case '\u25B8': case '\u25B6': swap = "\u25BA"; break;                                   // ▸ ▶ → ►
                    case '\u25C2': case '\u25C0': swap = "\u25C4"; break;                                   // ◂ ◀ → ◄
                    case '\u2714': case '\u2713': swap = "\u2022"; break;                                   // ✓ → •
                    case '\u2794': case '\u279C': case '\u27A4': swap = "\u2192"; break;                     // ➔ → →
                    case '\u2694': case '\u26A1': case '\u23ED': case '\uFE0F': case '\u2728': drop = true; break;
                    default:
                        if (char.IsSurrogate(c)) drop = true; // emoji
                        break;
                }
                if (swap == null && !drop) { if (sb != null) sb.Append(c); continue; }
                if (sb == null) sb = new StringBuilder(text.Substring(0, i));
                if (swap != null) sb.Append(swap);
                else if (i + 1 < text.Length && text[i + 1] == ' ' && (sb.Length == 0 || sb[sb.Length - 1] == ' ' || sb[sb.Length - 1] == '\n')) i++;
            }
            return sb == null ? text : sb.ToString().TrimEnd();
        }

        /// <summary>Canto superior esquerdo do elemento em coordenadas do palco (1280×720, y para baixo).</summary>
        public static Vector2 StagePosition(RectTransform rt)
        {
            float x = 0f, y = 0f;
            var current = rt;
            while (current != null)
            {
                var parent = current.parent as RectTransform;
                if (parent == null || parent == OverlayRoot || parent.name == "Screens" || parent.name == "Stage") { x += Left(current, Theme.Width); y += Top(current, Theme.Height); break; }
                x += Left(current, parent.sizeDelta.x);
                y += Top(current, parent.sizeDelta.y);
                current = parent;
            }
            return new Vector2(x, y);
        }

        private static float Left(RectTransform rt, float parentWidth)
        {
            return rt.anchorMin.x * parentWidth + rt.anchoredPosition.x - rt.pivot.x * rt.sizeDelta.x;
        }

        private static float Top(RectTransform rt, float parentHeight)
        {
            return (1f - rt.anchorMin.y) * parentHeight - rt.anchoredPosition.y - (1f - rt.pivot.y) * rt.sizeDelta.y;
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

        public static UiBar Bar(Transform parent, float x, float y, float w, float h, float fraction, Color fillColor, string text = "")
        {
            var bar = new UiBar(parent, x, y, w, h, fillColor);
            bar.Set(fraction);
            if (!string.IsNullOrEmpty(text))
                Label(parent, text, x, y, w, h, Mathf.Max(10, Mathf.RoundToInt(h - 6f)), Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            return bar;
        }
    }

    /// <summary>
    /// Barra horizontal (HP, stamina) em canaleta de bronze: preenchimento com brilho, rastro claro da parte perdida
    /// e marcas opcionais a cada quarto.
    /// </summary>
    public sealed class UiBar
    {
        private readonly RectTransform _fill;
        private readonly Image _fillImage;
        private readonly RectTransform _ghost;
        private readonly Image _ghostImage;
        private readonly BarGhostDriver _driver;
        private readonly float _width;
        private readonly float _height;

        public float Fraction { get; private set; }
        public float GhostFraction { get; private set; }

        public UiBar(Transform parent, float x, float y, float w, float h, Color fillColor, bool quarterTicks = false)
        {
            var back = Ui.Box(parent, "Bar", x, y, w, h, Theme.BarBack);
            back.sprite = Art.BarSprite;
            back.type = Image.Type.Sliced;
            var rim = back.gameObject.AddComponent<Outline>();
            rim.effectColor = new Color(0.50f, 0.40f, 0.27f, 0.70f);
            rim.effectDistance = new Vector2(1f, -1f);
            _width = w - 4f;
            _height = h - 4f;

            var ghost = Ui.Box(back.transform, "Ghost", 2f, 2f, _width, _height, Theme.Ghost);
            ghost.sprite = Art.BarSprite;
            ghost.type = Image.Type.Sliced;
            _ghost = ghost.rectTransform;
            _ghostImage = ghost;

            _fillImage = Ui.Box(back.transform, "Fill", 2f, 2f, _width, _height, fillColor);
            _fillImage.sprite = Art.BarSprite;
            _fillImage.type = Image.Type.Sliced;
            _fill = _fillImage.rectTransform;

            var gloss = Ui.Picture(back.transform, "Gloss", Art.GradientUp, 2f, 2f, _width, Mathf.Max(2f, _height * 0.5f));
            gloss.uvRect = new Rect(0f, 1f, 1f, -1f);
            gloss.color = new Color(1f, 1f, 1f, 0.22f);

            if (quarterTicks)
                for (int i = 1; i < 4; i++)
                    Ui.Box(back.transform, "Tick" + i, 2f + _width * i / 4f - 0.75f, 2f, 1.5f, _height, new Color(0f, 0f, 0f, 0.45f));

            _driver = back.gameObject.AddComponent<BarGhostDriver>();
            _driver.Setup(this);
            Fraction = 1f;
            GhostFraction = 1f;
        }

        public void Set(float fraction)
        {
            float next = Mathf.Clamp01(fraction);
            if (next < Fraction - 0.0001f) _driver.Hit();
            Fraction = next;
            _fill.sizeDelta = new Vector2(_width * Fraction, _height);
            _fillImage.enabled = _width * Fraction >= 1f;
            if (GhostFraction < Fraction) SetGhost(Fraction);
        }

        /// <summary>Muda o valor sem rastro (troca de lutador, início de tela).</summary>
        public void SetInstant(float fraction)
        {
            Fraction = Mathf.Clamp01(fraction);
            _fill.sizeDelta = new Vector2(_width * Fraction, _height);
            _fillImage.enabled = _width * Fraction >= 1f;
            SetGhost(Fraction);
        }

        internal void SetGhost(float fraction)
        {
            GhostFraction = Mathf.Clamp01(fraction);
            _ghost.sizeDelta = new Vector2(_width * GhostFraction, _height);
            _ghostImage.enabled = _width * GhostFraction >= 1f;
        }

        public void SetColor(Color color) { _fillImage.color = color; }
    }

    /// <summary>Animação contínua de partículas e leve deriva de câmera para os fundos de menu.</summary>
    public sealed class AmbientBackdropAnimator : MonoBehaviour
    {
        private RectTransform _driftingArt;
        private Vector2 _artHome;
        private RawImage[] _motes;
        private float[] _seed;
        private Color _baseColor;

        public void Initialize(RectTransform driftingArt, Color color, int count)
        {
            _driftingArt = driftingArt;
            if (_driftingArt != null) _artHome = _driftingArt.anchoredPosition;
            _baseColor = color;
            _motes = new RawImage[count];
            _seed = new float[count];
            for (int i = 0; i < count; i++)
            {
                _seed[i] = (i * 1.37f) % 10f;
                float size = 10f + (i % 4) * 6f;
                var img = Ui.Picture(transform, "Mote" + i, Art.VfxOrb, 0, 0, size, size);
                img.color = color;
                _motes[i] = img;
            }
        }

        private void Update()
        {
            float time = Time.unscaledTime;
            if (_driftingArt != null)
            {
                float dx = Mathf.Sin(time * 0.35f) * 10f;
                float dy = Mathf.Cos(time * 0.25f) * 6f;
                _driftingArt.anchoredPosition = _artHome + new Vector2(dx, dy);
            }

            if (_motes == null) return;
            for (int i = 0; i < _motes.Length; i++)
            {
                var mote = _motes[i];
                if (mote == null) continue;
                float s = _seed[i];
                float cycle = Mathf.Repeat(time * (0.045f + (i % 3) * 0.015f) + s * 0.1f, 1f);
                float x = Mathf.Repeat(s * 137f + Mathf.Sin(time * 0.6f + s) * 45f, Theme.Width);
                float y = Mathf.Lerp(Theme.Height + 20f, -20f, cycle);
                float size = 10f + (i % 4) * 6f;
                Ui.PlaceCentered(mote.rectTransform, x, y, size, size);
                var c = _baseColor;
                c.a = _baseColor.a * Mathf.Sin(cycle * Mathf.PI) * (0.65f + 0.35f * Mathf.Sin(time * 2.4f + s));
                mote.color = c;
            }
        }
    }
}
