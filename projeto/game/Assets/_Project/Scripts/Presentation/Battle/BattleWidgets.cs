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

    /// <summary>Palco vivo da arena: câmera com respiração e tremor, iluminação atmosférica, partículas do bioma e transição contínua.</summary>
    public sealed class ArenaStage : MonoBehaviour
    {
        private const int ParticleCount = 22;

        private ArenaId _currentArena;
        private RawImage _bgPrimary;
        private RawImage _bgTransition;
        private RawImage _floorGlow;
        private RawImage _skyGlow;
        private RawImage _shockwave;
        private RawImage[] _particles;
        private float[] _seeds;

        private float _shakeIntensity;
        private float _shakeDuration;
        private float _shakeTimer;
        private float _introZoom;

        public static ArenaStage Create(Transform parent, ArenaId initialArena)
        {
            var root = Ui.Node(parent, "ArenaStage", 0, 0, Theme.Width, Theme.Height);
            var stage = root.gameObject.AddComponent<ArenaStage>();
            stage.Build(initialArena);
            return stage;
        }

        private void Build(ArenaId initialArena)
        {
            _currentArena = initialArena;
            _bgPrimary = Ui.Picture(transform, "BgPrimary", Art.Arena(initialArena), 0, 0, 1344f, 756f);
            Ui.PlaceCentered(_bgPrimary.rectTransform, Theme.Width * 0.5f, Theme.Height * 0.5f, 1344f, 756f);

            _bgTransition = Ui.Picture(transform, "BgTransition", Art.Arena(initialArena), 0, 0, 1344f, 756f);
            Ui.PlaceCentered(_bgTransition.rectTransform, Theme.Width * 0.5f, Theme.Height * 0.5f, 1344f, 756f);
            _bgTransition.color = new Color(1f, 1f, 1f, 0f);

            _skyGlow = Ui.Picture(transform, "SkyGlow", Art.VfxOrb, 0, 0, 1240f, 320f);
            Ui.PlaceCentered(_skyGlow.rectTransform, Theme.Width * 0.5f, 130f, 1240f, 320f);

            _floorGlow = Ui.Picture(transform, "FloorGlow", Art.VfxOrb, 0, 0, 1080f, 250f);
            Ui.PlaceCentered(_floorGlow.rectTransform, Theme.Width * 0.5f, 470f, 1080f, 250f);

            _particles = new RawImage[ParticleCount];
            _seeds = new float[ParticleCount];
            for (int i = 0; i < ParticleCount; i++)
            {
                _seeds[i] = (i * 1.618f) % 10f;
                _particles[i] = Ui.Picture(transform, "Particle" + i, Art.VfxOrb, 0, 0, 16f, 16f);
            }

            _shockwave = Ui.Picture(transform, "MorphWave", Art.VfxRing, 0, 0, 100f, 100f);
            _shockwave.color = new Color(1f, 1f, 1f, 0f);

            ConfigureParticlesForArena(initialArena);
            _introZoom = 1f;
        }

        public void SetIntroZoom(float t)
        {
            _introZoom = Mathf.Clamp01(1f - t);
        }

        public void Shake(float intensity, float duration = 0.32f)
        {
            _shakeIntensity = Mathf.Max(_shakeIntensity, intensity);
            _shakeDuration = Mathf.Max(0.05f, duration);
            _shakeTimer = _shakeDuration;
        }

        /// <summary>Pulso fantasma no cenário durante cada giro da roleta de arenas.</summary>
        public void PreviewPulse(ArenaId previewArena, float strength)
        {
            _bgTransition.texture = Art.Arena(previewArena);
            Color tint = Theme.ArenaColor(previewArena);
            _bgTransition.color = new Color(
                Mathf.Lerp(1f, tint.r, 0.25f),
                Mathf.Lerp(1f, tint.g, 0.25f),
                Mathf.Lerp(1f, tint.b, 0.25f),
                Mathf.Clamp01(strength));
        }

        /// <summary>Transição contínua entre a arena antiga e a nova com onda de choque elemental.</summary>
        public void MorphStep(ArenaId targetArena, float progress)
        {
            float p = Mathf.Clamp01(progress);
            _bgTransition.texture = Art.Arena(targetArena);
            float smooth = p * p * (3f - 2f * p);
            _bgTransition.color = new Color(1f, 1f, 1f, smooth);
            float scale = Mathf.Lerp(1.10f, 1.015f, smooth);
            _bgTransition.rectTransform.localScale = new Vector3(scale, scale, 1f);

            Color waveColor = Theme.ArenaColor(targetArena);
            float waveSize = Mathf.Lerp(80f, 1680f, p);
            Ui.PlaceCentered(_shockwave.rectTransform, Theme.Width * 0.5f, 430f, waveSize, waveSize * 0.58f);
            _shockwave.color = new Color(waveColor.r, waveColor.g, waveColor.b, Mathf.Sin(p * Mathf.PI) * 0.85f);

            if (p >= 0.5f && _currentArena != targetArena)
            {
                _currentArena = targetArena;
                ConfigureParticlesForArena(targetArena);
            }
        }

        public void FinishMorph(ArenaId finalArena)
        {
            _currentArena = finalArena;
            _bgPrimary.texture = Art.Arena(finalArena);
            _bgTransition.color = new Color(1f, 1f, 1f, 0f);
            _bgTransition.rectTransform.localScale = Vector3.one;
            _shockwave.color = new Color(1f, 1f, 1f, 0f);
            ConfigureParticlesForArena(finalArena);
        }

        private void ConfigureParticlesForArena(ArenaId arena)
        {
            for (int i = 0; i < ParticleCount; i++)
            {
                if (_particles[i] == null) continue;
                switch (arena)
                {
                    case ArenaId.Volcano:
                        _particles[i].texture = i % 3 == 0 ? Art.VfxShard : Art.VfxOrb;
                        break;
                    case ArenaId.Sea:
                        _particles[i].texture = i % 2 == 0 ? Art.VfxRing : Art.VfxOrb;
                        break;
                    case ArenaId.Forest:
                        _particles[i].texture = i % 2 == 0 ? Art.VfxShard : Art.VfxOrb;
                        break;
                    default:
                        _particles[i].texture = i % 4 == 0 ? Art.VfxShard : Art.VfxOrb;
                        break;
                }
            }
        }

        private void Update()
        {
            float time = Time.time;

            // Câmera viva: respiração suave + tremor quando ativado.
            Vector2 camOffset = new Vector2(Mathf.Sin(time * 0.55f) * 8f, Mathf.Cos(time * 0.40f) * 4.5f);
            if (_shakeTimer > 0f)
            {
                _shakeTimer -= Time.deltaTime;
                float decay = Mathf.Clamp01(_shakeTimer / _shakeDuration);
                float mag = _shakeIntensity * decay;
                camOffset += new Vector2(Mathf.Sin(time * 58f) * mag, Mathf.Cos(time * 49f) * mag * 0.7f);
                if (_shakeTimer <= 0f) _shakeIntensity = 0f;
            }

            float baseScale = 1.015f + 0.012f * Mathf.Sin(time * 0.45f) + 0.065f * _introZoom;
            var center = new Vector2(Theme.Width * 0.5f, -Theme.Height * 0.5f) + camOffset;
            _bgPrimary.rectTransform.anchoredPosition = center;
            _bgPrimary.rectTransform.localScale = new Vector3(baseScale, baseScale, 1f);
            _bgTransition.rectTransform.anchoredPosition = center;

            // Iluminação atmosférica pulsante por bioma.
            Color arenaColor = Theme.ArenaColor(_currentArena);
            float pulse = 0.5f + 0.5f * Mathf.Sin(time * 1.8f);
            _floorGlow.color = new Color(arenaColor.r, arenaColor.g, arenaColor.b, Mathf.Lerp(0.10f, 0.22f, pulse));
            _skyGlow.color = new Color(arenaColor.r, arenaColor.g, arenaColor.b, Mathf.Lerp(0.06f, 0.15f, 1f - pulse));

            // Partículas ambientais específicas de cada arena.
            for (int i = 0; i < ParticleCount; i++)
            {
                var p = _particles[i];
                if (p == null) continue;
                float s = _seeds[i];
                UpdateParticle(p, i, s, time, arenaColor);
            }
        }

        private void UpdateParticle(RawImage p, int index, float seed, float time, Color arenaColor)
        {
            float speed = _currentArena == ArenaId.Volcano ? 0.13f
                : _currentArena == ArenaId.Sea ? 0.08f
                : _currentArena == ArenaId.Forest ? 0.07f : 0.045f;
            float cycle = Mathf.Repeat(time * (speed + (index % 4) * 0.018f) + seed * 0.13f, 1f);
            float xBase = Mathf.Repeat(seed * 157f, Theme.Width);
            float x, y, w, h, alpha;
            float angle = 0f;

            switch (_currentArena)
            {
                case ArenaId.Volcano:
                    // Brasas e fagulhas subindo com turbulência térmica.
                    x = xBase + Mathf.Sin(time * 2.2f + seed) * 38f;
                    y = Mathf.Lerp(620f, 70f, cycle);
                    w = index % 3 == 0 ? 18f : 13f;
                    h = index % 3 == 0 ? 8f : 13f;
                    angle = time * 120f + seed * 40f;
                    alpha = Mathf.Sin(cycle * Mathf.PI) * (0.45f + 0.35f * Mathf.Sin(time * 7f + seed));
                    p.color = Color.Lerp(arenaColor, Theme.Gold, (index % 3) * 0.35f);
                    break;

                case ArenaId.Sea:
                    // Bolhas oceânicas e espuma subindo com ondulação marinha.
                    x = xBase + Mathf.Sin(time * 1.4f + seed * 2f) * 32f;
                    y = Mathf.Lerp(600f, 90f, cycle);
                    float bubbleSize = 10f + (index % 4) * 5f;
                    w = bubbleSize;
                    h = bubbleSize;
                    alpha = Mathf.Sin(cycle * Mathf.PI) * 0.48f;
                    p.color = Color.Lerp(arenaColor, Color.white, 0.45f);
                    break;

                case ArenaId.Forest:
                    // Folhas rodopiando na brisa e vaga-lumes bioluminescentes.
                    if (index % 2 == 0)
                    {
                        x = Mathf.Repeat(xBase + cycle * 260f, Theme.Width);
                        y = Mathf.Lerp(80f, 560f, cycle) + Mathf.Sin(time * 1.8f + seed) * 18f;
                        w = 20f;
                        h = 9f;
                        angle = Mathf.Sin(time * 2.5f + seed) * 55f + cycle * 180f;
                        alpha = Mathf.Sin(cycle * Mathf.PI) * 0.55f;
                        p.color = Color.Lerp(arenaColor, Theme.Gold, 0.25f);
                    }
                    else
                    {
                        x = xBase + Mathf.Sin(time * 0.9f + seed) * 48f;
                        y = 220f + (seed * 31f) % 280f + Mathf.Cos(time * 1.2f + seed) * 26f;
                        w = 14f;
                        h = 14f;
                        alpha = (0.5f + 0.5f * Mathf.Sin(time * 3.2f + seed)) * 0.62f;
                        p.color = new Color(0.65f, 1f, 0.45f, 1f);
                    }
                    break;

                default:
                    // Poeira estelar e fragmentos lunares em baixa gravidade.
                    x = xBase + Mathf.Sin(time * 0.5f + seed) * 24f;
                    y = Mathf.Lerp(560f, 70f, cycle);
                    float starSize = index % 4 == 0 ? 18f : 11f;
                    w = starSize;
                    h = index % 4 == 0 ? 7f : starSize;
                    angle = time * 35f + seed * 30f;
                    alpha = Mathf.Sin(cycle * Mathf.PI) * (0.4f + 0.45f * Mathf.Sin(time * 4.5f + seed * 3f));
                    p.color = Color.Lerp(arenaColor, Color.white, 0.5f);
                    break;
            }

            Ui.PlaceCentered(p.rectTransform, x, y, w, h);
            p.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            var col = p.color;
            col.a = Mathf.Clamp01(alpha);
            p.color = col;
        }
    }

    /// <summary>Animação contínua de respiração do lutador e anel de sinergia com a arena sob seus pés.</summary>
    public sealed class FighterStageAnimator : MonoBehaviour
    {
        private RectTransform _body;
        private RectTransform _shadow;
        private RawImage _arenaRing;
        private Vector2 _home;
        private Vector2 _animOffset;
        private float _animScale = 1f;
        private float _phaseOffset;
        private bool _fainted;
        private bool _arenaBoosted;
        private Color _ringColor;

        public void Setup(RectTransform body, RectTransform shadow, RawImage arenaRing, Vector2 home, float phaseOffset)
        {
            _body = body;
            _shadow = shadow;
            _arenaRing = arenaRing;
            _home = home;
            _phaseOffset = phaseOffset;
        }

        public void SetAnimPose(Vector2 offset, float scale, bool fainted)
        {
            _animOffset = offset;
            _animScale = scale;
            _fainted = fainted;
        }

        public void SetArenaResonance(bool boosted, Color ringColor)
        {
            _arenaBoosted = boosted;
            _ringColor = ringColor;
            if (!boosted && _arenaRing != null) _arenaRing.color = new Color(1f, 1f, 1f, 0f);
        }

        private void Update()
        {
            if (_body == null) return;
            float time = Time.time * 2.6f + _phaseOffset;
            float breathe = _fainted ? 0f : Mathf.Sin(time);
            float idleY = _fainted ? 0f : breathe * 3.2f;
            float sx = _animScale * (1f - 0.014f * breathe);
            float sy = _animScale * (1f + 0.022f * breathe);

            _body.anchoredPosition = _home + _animOffset + new Vector2(0f, idleY);
            _body.localScale = new Vector3(sx, sy, 1f);

            if (_shadow != null)
            {
                float jumpFactor = Mathf.Clamp01(1f - Mathf.Max(0f, _animOffset.y) / 140f);
                float shadowScale = (1f - 0.04f * breathe) * jumpFactor;
                _shadow.localScale = new Vector3(shadowScale, shadowScale, 1f);
            }

            if (_arenaRing != null && _arenaBoosted && !_fainted)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(time * 1.3f);
                float ringScale = Mathf.Lerp(0.94f, 1.08f, pulse);
                _arenaRing.rectTransform.localScale = new Vector3(ringScale, ringScale, 1f);
                _arenaRing.color = new Color(_ringColor.r, _ringColor.g, _ringColor.b, Mathf.Lerp(0.32f, 0.72f, pulse));
            }
            else if (_arenaRing != null)
            {
                _arenaRing.color = new Color(1f, 1f, 1f, 0f);
            }
        }
    }

    /// <summary>Lutador ativo na arena: sombra no chão, anel de sinergia de arena, sprite com respiração e pedestal sob os pés.</summary>
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
        private readonly Text _arenaBadge;
        private readonly UiBar _hp;
        private readonly Vector2 _home;
        private readonly FighterStageAnimator _animator;
        private Vector2 _currentOffset;
        private float _currentScale = 1f;
        private bool _fainted;
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

            // Anel luminoso de sinergia com a arena (ativo quando a arena favorece o lutador).
            var arenaRing = Ui.Picture(parent, "ArenaRing", Art.VfxRing, 0, 0, 198f, 48f);
            arenaRing.color = new Color(1f, 1f, 1f, 0f);
            Ui.PlaceCentered(arenaRing.rectTransform, centerX, centerY + 82f, 198f, 48f);

            _body = Ui.Node(parent, "Body", 0, 0, BodySize, BodySize);
            Ui.PlaceCentered(_body, centerX, centerY, BodySize, BodySize);
            _home = _body.anchoredPosition;
            _sprite = Ui.Picture(_body, "Sprite", null, 0, 0, BodySize, BodySize);
            // O lutador da direita é espelhado para os dois ficarem de frente um para o outro.
            if (centerX > Theme.Width * 0.5f) _sprite.uvRect = new Rect(1f, 0f, -1f, 1f);
            _letter = Ui.Label(_body, "", 0, 0, BodySize, BodySize, 64, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);

            _animator = _body.gameObject.AddComponent<FighterStageAnimator>();
            _animator.Setup(_body, shadow.rectTransform, arenaRing, _home, centerX > Theme.Width * 0.5f ? 1.7f : 0f);

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
            _effects = Ui.Label(plate.transform, "", 8, 44, PlateWidth - 96, 18, 11, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            _arenaBadge = Ui.Label(plate.transform, "", PlateWidth - 92, 44, 80, 18, 11, Theme.Good, TextAnchor.MiddleRight, FontStyle.Bold);
        }

        public void Show(BattleSnapshot.FighterView fighter, BattleCatalog catalog)
        {
            var spec = fighter.Spec;
            _maxHp = spec.MaxHp;
            _fainted = fighter.Fainted;
            _sprite.texture = Art.Fighter(spec.Id, spec.Element, spec.Class);
            _letter.text = Art.HasSprite(spec.Id) ? "" : Theme.ClassLetter(spec.Class);
            _name.text = spec.Name;
            _sub.text = Theme.ElementName(spec.Element) + " · " + Theme.ClassName(spec.Class);
            _sub.color = Theme.ElementColor(spec.Element);
            _plateBorder.color = Theme.ElementColor(spec.Element);
            _effects.text = BattleText.Effects(fighter.Effects, catalog);
            SetHp(fighter.Hp);
            SetAlpha(fighter.Fainted ? 0.25f : 1f);
            _animator.SetAnimPose(_currentOffset, _currentScale, _fainted);
        }

        public void RefreshArenaSynergy(FighterSpec spec, bool fainted, ArenaSpec arena, BattleRules rules)
        {
            _fainted = fainted;
            double mult = ArenaSystem.Multiplier(arena, spec, rules);
            if (mult > 1.001)
            {
                _arenaBadge.text = "▲ ARENA +10%";
                _arenaBadge.color = Theme.Good;
                _animator.SetArenaResonance(true, Theme.ArenaColor(arena.Id));
            }
            else if (mult < 0.999)
            {
                _arenaBadge.text = "▼ ARENA −10%";
                _arenaBadge.color = Theme.Bad;
                _animator.SetArenaResonance(false, Color.clear);
            }
            else
            {
                _arenaBadge.text = "";
                _animator.SetArenaResonance(false, Color.clear);
            }
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

        public void SetOffset(Vector2 offset)
        {
            _currentOffset = offset;
            _animator.SetAnimPose(_currentOffset, _currentScale, _fainted);
        }

        public void SetScale(float scale)
        {
            _currentScale = scale;
            _animator.SetAnimPose(_currentOffset, _currentScale, _fainted);
        }

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
