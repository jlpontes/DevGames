using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ComboTactics.UI
{
    /// <summary>Curvas de movimento usadas por toda a interface e pela batalha (ART-DIRECTION.md §4).</summary>
    public static class Ease
    {
        public static float OutCubic(float t) { t = Mathf.Clamp01(t); float u = 1f - t; return 1f - u * u * u; }
        public static float InCubic(float t) { t = Mathf.Clamp01(t); return t * t * t; }
        public static float InQuad(float t) { t = Mathf.Clamp01(t); return t * t; }
        public static float OutQuad(float t) { t = Mathf.Clamp01(t); return 1f - (1f - t) * (1f - t); }
        public static float InOutSine(float t) { t = Mathf.Clamp01(t); return 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI); }

        /// <summary>Passa um pouco do alvo e volta: entrada de cartas, números de dano, invocações.</summary>
        public static float OutBack(float t, float overshoot = 1.7f)
        {
            t = Mathf.Clamp01(t) - 1f;
            return 1f + t * t * ((overshoot + 1f) * t + overshoot);
        }

        /// <summary>0 → 1 → 0, com o pico em <paramref name="peak"/>.</summary>
        public static float Bell(float t, float peak = 0.5f)
        {
            t = Mathf.Clamp01(t);
            return t < peak ? OutQuad(t / peak) : 1f - InQuad((t - peak) / (1f - peak));
        }

        /// <summary>Oscilação amortecida para recuos e tremores curtos.</summary>
        public static float Wobble(float t, float frequency = 3f)
        {
            t = Mathf.Clamp01(t);
            return Mathf.Sin(t * Mathf.PI * 2f * frequency) * (1f - t);
        }
    }

    /// <summary>
    /// Estados visuais de um botão: hover levanta e acende o brilho, clique afunda, desabilitado apaga o texto,
    /// selecionado mantém o brilho dourado. Anima em tempo real (não depende da velocidade da batalha).
    /// </summary>
    public sealed class ButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private Button _button;
        private Image _glow;
        private Text _label;
        private Color _labelColor;
        private float _glowStrength;
        private bool _hover;
        private bool _pressed;
        private float _scale = 1f;
        private float _glowAlpha;

        /// <summary>Selecionado (carta escolhida, aba ativa): brilho fixo mesmo sem o mouse em cima.</summary>
        public bool Selected { get; set; }

        public void Setup(Button button, Image glow, Text label, float glowStrength)
        {
            _button = button;
            _glow = glow;
            _label = label;
            _labelColor = label != null ? label.color : Color.white;
            _glowStrength = glowStrength;
        }

        public void OnPointerEnter(PointerEventData eventData) { _hover = true; }
        public void OnPointerExit(PointerEventData eventData) { _hover = false; _pressed = false; }
        public void OnPointerDown(PointerEventData eventData) { _pressed = true; }
        public void OnPointerUp(PointerEventData eventData) { _pressed = false; }

        private void Update()
        {
            if (_button == null) return;
            bool live = _button.interactable;
            float targetScale = !live ? 1f : _pressed ? 0.965f : _hover ? 1.03f : 1f;
            float targetGlow = !live ? 0f : Selected ? 0.75f : _hover ? _glowStrength : 0f;
            float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime / Mathf.Max(0.001f, Theme.UiHover * 0.45f));
            _scale = Mathf.Lerp(_scale, targetScale, k);
            _glowAlpha = Mathf.Lerp(_glowAlpha, targetGlow, k);
            transform.localScale = new Vector3(_scale, _scale, 1f);
            if (_glow != null) _glow.color = Theme.WithAlpha(_glow.color, _glowAlpha);
            if (_label != null) _label.color = live ? _labelColor : Theme.WithAlpha(_labelColor, 0.45f);
        }
    }

    /// <summary>Barra com "rastro": a parte perdida fica clara por um instante e depois escorre até o valor novo.</summary>
    public sealed class BarGhostDriver : MonoBehaviour
    {
        private UiBar _bar;
        private float _hold;

        public void Setup(UiBar bar) { _bar = bar; }

        public void Hit() { _hold = 0.38f; }

        private void Update()
        {
            if (_bar == null) return;
            float ghost = _bar.GhostFraction;
            float target = _bar.Fraction;
            if (ghost <= target) { _bar.SetGhost(target); return; }
            if (_hold > 0f) { _hold -= Time.unscaledDeltaTime; return; }
            _bar.SetGhost(Mathf.Max(target, ghost - Time.unscaledDeltaTime * 0.9f));
        }
    }

    /// <summary>Entrada de tela: a tela nova surge do escuro e assenta alguns pixels (0,2 s).</summary>
    public sealed class ScreenFader : MonoBehaviour
    {
        private CanvasGroup _group;
        private RectTransform _rect;
        private Vector2 _home;
        private float _t;

        public void Begin()
        {
            _group = gameObject.GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            _rect = (RectTransform)transform;
            _home = _rect.anchoredPosition;
            _group.alpha = 0f;
        }

        private void Update()
        {
            if (_group == null) return;
            _t += Time.unscaledDeltaTime / Theme.UiFade;
            float e = Ease.OutCubic(_t);
            _group.alpha = e;
            _rect.anchoredPosition = _home + new Vector2(0f, -10f * (1f - e));
            if (_t >= 1f)
            {
                _group.alpha = 1f;
                _rect.anchoredPosition = _home;
                Destroy(this);
            }
        }
    }

    /// <summary>Aviso curto que desce do topo, fica e some sozinho.</summary>
    public sealed class ToastAnimator : MonoBehaviour
    {
        private const float Life = 3.0f;
        private CanvasGroup _group;
        private RectTransform _rect;
        private Vector2 _home;
        private float _t;

        public void Begin()
        {
            _group = gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _rect = (RectTransform)transform;
            _home = _rect.anchoredPosition;
        }

        private void Update()
        {
            if (_group == null) return;
            _t += Time.unscaledDeltaTime;
            float enter = Ease.OutBack(_t / 0.28f, 1.2f);
            float leave = Mathf.Clamp01((_t - (Life - 0.35f)) / 0.35f);
            _rect.anchoredPosition = _home + new Vector2(0f, 46f * (1f - enter) + 12f * leave);
            _group.alpha = Mathf.Clamp01(_t / 0.12f) * (1f - leave);
            if (_t >= Life) Destroy(gameObject);
        }
    }

    /// <summary>Dica de mouse: espera um instante sobre o elemento e mostra um painel com título e texto.</summary>
    public sealed class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        public string Title;
        public string Body;
        private float _wait = -1f;
        private bool _shown;

        public void OnPointerEnter(PointerEventData eventData) { _wait = 0.28f; }
        public void OnPointerExit(PointerEventData eventData) { Hide(); }
        public void OnPointerDown(PointerEventData eventData) { Hide(); }

        private void Hide()
        {
            _wait = -1f;
            if (_shown) TooltipView.Hide(this);
            _shown = false;
        }

        private void Update()
        {
            if (_wait < 0f) return;
            _wait -= Time.unscaledDeltaTime;
            if (_wait > 0f) return;
            _wait = -1f;
            _shown = true;
            TooltipView.Show(this, (RectTransform)transform, Title, Body);
        }

        private void OnDisable() { Hide(); }
        private void OnDestroy() { if (_shown) TooltipView.Hide(this); }
    }

    /// <summary>O painel único de dicas, desenhado na camada de sobreposição do App.</summary>
    public static class TooltipView
    {
        private const float Width = 300f;
        private static GameObject _current;
        private static TooltipTrigger _owner;

        public static void Show(TooltipTrigger owner, RectTransform target, string title, string body)
        {
            Hide(_owner);
            if (Ui.OverlayRoot == null) return;
            _owner = owner;

            int lines = 1 + (string.IsNullOrEmpty(body) ? 0 : Mathf.CeilToInt(body.Length / 40f) + body.Split('\n').Length - 1);
            float height = 40f + lines * 17f;
            Vector2 pos = Ui.StagePosition(target);
            float x = Mathf.Clamp(pos.x + target.sizeDelta.x * 0.5f - Width * 0.5f, 8f, Theme.Width - Width - 8f);
            float y = pos.y - height - 10f;
            if (y < 8f) y = pos.y + target.sizeDelta.y + 10f;

            var frame = Ui.Frame(Ui.OverlayRoot, "Tooltip", x, y, Width, height, Theme.PanelSolid, Theme.BorderGold, 1.5f);
            Ui.Label(frame, title, 10, 6, Width - 20, 20, Theme.FontSmall + 1, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            if (!string.IsNullOrEmpty(body))
                Ui.Label(frame, body, 10, 28, Width - 20, height - 34, Theme.FontSmall, Theme.Text, TextAnchor.UpperLeft);
            _current = frame.parent.gameObject;
            var group = _current.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
        }

        public static void Hide(TooltipTrigger owner)
        {
            if (owner != _owner || _current == null) return;
            Object.Destroy(_current);
            _current = null;
            _owner = null;
        }
    }
}
