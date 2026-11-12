using System;
using System.Collections.Generic;
using System.Text;
using ComboTactics.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ComboTactics.UI
{
    /// <summary>Parâmetros dinâmicos da barra de reação calculados a partir da Velocidade do defensor e do tipo de golpe.</summary>
    public struct QteParams
    {
        public float SweepSeconds;
        public float GoodSeconds;
        public float PerfectSeconds;
        public float TargetNormalized;
        public int SpeedBonusPercent;
        public bool IsSpecial;
    }

    /// <summary>Números do QTE (SYSTEM-DESIGN §3.2). O núcleo só recebe o resultado; o timing é medido aqui.</summary>
    public static class QteConfig
    {
        /// <summary>Base de contra-ataque antes do multiplicador de Velocidade.</summary>
        public static float PerfectSeconds = 0.030f;
        /// <summary>Base de desvio antes do multiplicador de Velocidade.</summary>
        public static float GoodSeconds = 0.078f;
        /// <summary>Posição do alvo na barra, sorteada a cada golpe.</summary>
        public static float TargetMin = 0.52f;
        public static float TargetMax = 0.84f;
        /// <summary>Breve preparação fluida antes do marcador partir.</summary>
        public static float LeadInSeconds = 0.14f;

        public static QteParams Compute(int defenderSpeed, int attackerSpeed, bool isSpecial)
        {
            // Velocidade dos lutadores vai de ~23 (Tanques) até ~90+ (Guerreiros) e ~117 com evoluções.
            float speedNorm = Mathf.Clamp((defenderSpeed - 20f) / 75f, 0f, 1.35f);
            float relative = Mathf.Clamp((defenderSpeed - attackerSpeed) / 110f, -0.18f, 0.25f);
            float windowMult = Mathf.Clamp(0.84f + 0.75f * speedNorm + relative, 0.78f, 1.95f);

            // Quanto maior a velocidade do defensor, mais ampla a zona e mais controlada a travessia do marcador.
            float sweep = Mathf.Lerp(0.72f, 1.04f, Mathf.Clamp01(speedNorm));
            float good = GoodSeconds * windowMult;
            float perfect = PerfectSeconds * windowMult;
            float target = UnityEngine.Random.Range(TargetMin, TargetMax);

            if (isSpecial)
            {
                // Golpe Especial (Super): barra muito mais rápida e zona reduzida!
                sweep *= 0.56f;
                good *= 0.60f;
                perfect *= 0.45f;
                target = UnityEngine.Random.Range(0.60f, 0.88f);
            }

            return new QteParams
            {
                SweepSeconds = sweep,
                GoodSeconds = good,
                PerfectSeconds = perfect,
                TargetNormalized = target,
                SpeedBonusPercent = Mathf.RoundToInt((windowMult - 1f) * 100f),
                IsSpecial = isSpecial
            };
        }

        public static ReactionResult Classify(float errorSeconds)
        {
            return Classify(errorSeconds, PerfectSeconds, GoodSeconds);
        }

        public static ReactionResult Classify(float errorSeconds, float perfectSeconds, float goodSeconds)
        {
            float error = Mathf.Abs(errorSeconds);
            if (error <= perfectSeconds) return ReactionResult.Counter;
            return error <= goodSeconds ? ReactionResult.Dodge : ReactionResult.Miss;
        }
    }

    /// <summary>Captura o clique no instante exato em que o botão do mouse ou toque desce (sem atraso de soltar o botão).</summary>
    public sealed class ReactionInputCatcher : MonoBehaviour, IPointerDownHandler
    {
        public bool Pressed { get; private set; }

        public void OnPointerDown(PointerEventData eventData)
        {
            Pressed = true;
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

        /// <summary>Soco de zoom no impacto: a câmera avança um pouco e volta.</summary>
        public void Punch(float strength) { _punch = Mathf.Max(_punch, Mathf.Clamp(strength, 0f, 0.05f)); }

        public void SetIntroZoom(float t)
        {
            _introZoom = Mathf.Clamp01(1f - t);
        }

        private float _punch;

        /// <summary>Tremor de câmera, com teto (ART-DIRECTION §4): forte o bastante para sentir, nunca para enjoar.</summary>
        public void Shake(float intensity, float duration = 0.32f)
        {
            _shakeIntensity = Mathf.Min(Theme.ShakeMax, Mathf.Max(_shakeIntensity, intensity));
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
                float mag = _shakeIntensity * decay * decay;
                camOffset += new Vector2((Mathf.Sin(time * 61f) + 0.5f * Mathf.Sin(time * 97f)) * mag * 0.7f,
                                         (Mathf.Cos(time * 53f) + 0.5f * Mathf.Cos(time * 83f)) * mag * 0.5f);
                if (_shakeTimer <= 0f) _shakeIntensity = 0f;
            }

            _punch = Mathf.Max(0f, _punch - Time.unscaledDeltaTime * 0.12f);
            float baseScale = 1.015f + 0.012f * Mathf.Sin(time * 0.45f) + 0.065f * _introZoom + _punch;
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

    /// <summary>
    /// Corpo vivo do lutador: idle próprio de cada classe, pose de golpe (deslocamento, escala, achatamento e giro),
    /// congelamento de impacto (hit-stop), aura do elemento e anel de runas da arena sob os pés.
    /// Toda a pose é composta aqui, uma vez por quadro; a batalha só informa os alvos.
    /// </summary>
    public sealed class FighterStageAnimator : MonoBehaviour
    {
        private RectTransform _body;
        private RectTransform _shadow;
        private RawImage _arenaRing;
        private RawImage _aura;
        private Vector2 _home;
        private float _phase;
        private float _clock;
        private float _frozen;

        private CharClass _class = CharClass.Warrior;
        private bool _fainted;
        private bool _lowHp;
        private bool _arenaBoosted;
        private Color _ringColor;
        private Color _auraColor = Color.white;

        public Vector2 Offset;
        public float Scale = 1f;
        public Vector2 Squash = Vector2.one;
        public float Rotation;
        public float AuraBoost;

        public void Setup(RectTransform body, RectTransform shadow, RawImage arenaRing, RawImage aura, Vector2 home, float phaseOffset)
        {
            _body = body;
            _shadow = shadow;
            _arenaRing = arenaRing;
            _aura = aura;
            _home = home;
            _phase = phaseOffset;
        }

        public void SetIdentity(CharClass charClass, Color elementColor)
        {
            _class = charClass;
            _auraColor = elementColor;
        }

        public void SetState(bool fainted, bool lowHp)
        {
            _fainted = fainted;
            _lowHp = lowHp;
        }

        public void SetArenaResonance(bool boosted, Color ringColor)
        {
            _arenaBoosted = boosted;
            _ringColor = ringColor;
        }

        /// <summary>Hit-stop: segura o idle e a pose por alguns centésimos no instante do impacto.</summary>
        public void Freeze(float seconds) { _frozen = Mathf.Max(_frozen, seconds); }

        private void Update()
        {
            if (_body == null) return;
            float dt = Time.deltaTime;
            if (_frozen > 0f) { _frozen -= Time.unscaledDeltaTime; return; }
            _clock += dt * (_lowHp ? 1.35f : 1f);
            float t = _clock + _phase;

            Vector2 idle = Vector2.zero;
            float idleRot = 0f, sx = 1f, sy = 1f, lift = 0f;
            if (!_fainted)
            {
                switch (_class)
                {
                    case CharClass.Mage:
                        // Flutua: sobe e desce devagar, com um leve balanço do manto.
                        lift = 8f + Mathf.Sin(t * 1.7f) * 6f;
                        idle = new Vector2(0f, lift);
                        idleRot = Mathf.Sin(t * 1.15f) * 1.6f;
                        sy = 1f + 0.012f * Mathf.Sin(t * 1.7f + 0.6f);
                        break;
                    case CharClass.Tank:
                        // Respiração pesada: o peito enche, o corpo quase não sai do chão.
                        float breath = Mathf.Sin(t * 1.35f);
                        sx = 1f - 0.012f * breath;
                        sy = 1f + 0.028f * breath;
                        idle = new Vector2(0f, 1.5f * breath);
                        break;
                    case CharClass.Warrior:
                        // Base de luta: quica no lugar, achatando no apoio.
                        float bounce = Mathf.Abs(Mathf.Sin(t * 2.9f));
                        idle = new Vector2(0f, bounce * 5f);
                        sx = 1f + 0.018f * (1f - bounce);
                        sy = 1f - 0.022f * (1f - bounce);
                        idleRot = -1.2f + Mathf.Sin(t * 1.45f) * 0.8f;
                        break;
                    default:
                        // Guardião do Núcleo: flutuação lenta e ampla.
                        lift = 10f + Mathf.Sin(t * 0.95f) * 8f;
                        idle = new Vector2(0f, lift);
                        idleRot = Mathf.Sin(t * 0.7f) * 1.2f;
                        break;
                }
                if (_lowHp) idleRot += -2.5f; // ferido: o corpo pesa para a frente
            }

            float mirror = _home.x > Theme.Width * 0.5f ? -1f : 1f;
            _body.anchoredPosition = _home + Offset + idle;
            _body.localScale = new Vector3(Scale * Squash.x * sx, Scale * Squash.y * sy, 1f);
            _body.localRotation = Quaternion.Euler(0f, 0f, (Rotation + idleRot) * mirror);

            if (_shadow != null)
            {
                float height = Mathf.Max(0f, Offset.y + idle.y);
                float s = Mathf.Clamp(1f - height / 160f, 0.45f, 1.05f) * (_fainted ? 0.7f : 1f);
                _shadow.localScale = new Vector3(s * Squash.x, s, 1f);
                _shadow.anchoredPosition = new Vector2(_home.x + Offset.x, _shadow.anchoredPosition.y);
            }

            if (_aura != null)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(t * 1.6f);
                float a = _fainted ? 0f : Mathf.Lerp(0.10f, 0.20f, pulse) + AuraBoost;
                Color c = _lowHp ? Color.Lerp(_auraColor, Theme.Bad, 0.5f) : _auraColor;
                _aura.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(a));
                _aura.rectTransform.anchoredPosition = _body.anchoredPosition;
            }

            if (_arenaRing != null)
            {
                if (_arenaBoosted && !_fainted)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(t * 1.3f);
                    _arenaRing.color = new Color(_ringColor.r, _ringColor.g, _ringColor.b, Mathf.Lerp(0.38f, 0.70f, pulse));
                }
                else _arenaRing.color = new Color(0.80f, 0.64f, 0.40f, _fainted ? 0f : 0.14f);
                _arenaRing.rectTransform.localRotation = Quaternion.Euler(0f, 0f, t * 6f);
            }
        }
    }

    /// <summary>
    /// Lutador ativo na arena: sombra, anel de runas, aura, sprite com flash de impacto e a placa de status sob os pés
    /// (nome, elemento e classe, HP com rastro de dano, efeitos em pips e o selo da arena).
    /// </summary>
    public sealed class FighterWidget
    {
        private const float BodySize = 210f;
        private const float PlateWidth = 292f;
        private const float PlateHeight = 70f;

        private readonly RectTransform _body;
        private readonly RawImage _sprite;
        private readonly RawImage _flash;
        private readonly Text _letter;
        private readonly Image _plateBorder;
        private readonly RawImage _emblem;
        private readonly Text _name;
        private readonly Text _sub;
        private readonly Text _hpText;
        private readonly Text _arenaBadge;
        private readonly StatusPips _pips;
        private readonly UiBar _hp;
        private readonly FighterStageAnimator _animator;
        private float _alpha = 1f;
        private Color _tint = Color.white;
        private int _maxHp = 1;
        private bool _fainted;

        public float CenterX { get; private set; }
        public float CenterY { get; private set; }
        public float ShownHp { get; private set; }
        public float Facing { get; private set; }
        public Texture Texture { get { return _sprite.texture; } }
        public RectTransform Body { get { return _body; } }

        public FighterWidget(Transform parent, float centerX, float centerY)
        {
            CenterX = centerX;
            CenterY = centerY;
            Facing = centerX > Theme.Width * 0.5f ? -1f : 1f;

            var shadow = Ui.Picture(parent, "Shadow", Art.VfxOrb, 0, 0, 176f, 36f);
            shadow.color = new Color(0f, 0f, 0f, 0.55f);
            Ui.PlaceCentered(shadow.rectTransform, centerX, centerY + 90f, 176f, 36f);

            // Anel de runas achatado no chão: discreto sempre, aceso quando a arena favorece o lutador.
            // O anel gira dentro de um suporte achatado: assim continua uma elipse deitada no chão enquanto roda.
            var ringHolder = Ui.Node(parent, "RingPerspective", 0, 0, 232f, 232f);
            Ui.PlaceCentered(ringHolder, centerX, centerY + 90f, 232f, 232f);
            ringHolder.localScale = new Vector3(1f, 0.26f, 1f);
            var ring = Ui.Picture(ringHolder, "ArenaRing", Art.VfxRuneCircle, 0, 0, 232f, 232f);
            Ui.PlaceCentered(ring.rectTransform, 116f, 116f, 232f, 232f);

            var aura = Ui.Picture(parent, "Aura", Art.VfxOrb, 0, 0, 250f, 250f);
            Ui.PlaceCentered(aura.rectTransform, centerX, centerY, 250f, 250f);
            aura.color = new Color(1f, 1f, 1f, 0f);

            _body = Ui.Node(parent, "Body", 0, 0, BodySize, BodySize);
            Ui.PlaceCentered(_body, centerX, centerY, BodySize, BodySize);
            // Pivô nos pés: o corpo achata e gira apoiado no chão, não no próprio centro.
            _body.pivot = new Vector2(0.5f, 0f);
            _body.anchoredPosition = new Vector2(centerX, -(centerY + BodySize * 0.5f));
            var home = _body.anchoredPosition;

            _sprite = Ui.Picture(_body, "Sprite", null, 0, 0, BodySize, BodySize);
            _flash = Ui.Picture(_body, "Flash", null, 0, 0, BodySize, BodySize);
            _flash.material = Art.SilhouetteMaterial;
            _flash.color = new Color(1f, 1f, 1f, 0f);
            if (Facing < 0f)
            {
                _sprite.uvRect = new Rect(1f, 0f, -1f, 1f);
                _flash.uvRect = new Rect(1f, 0f, -1f, 1f);
            }
            _letter = Ui.Label(_body, "", 0, 0, BodySize, BodySize, 64, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);

            _animator = _body.gameObject.AddComponent<FighterStageAnimator>();
            _animator.Setup(_body, shadow.rectTransform, ring, aura, home, Facing < 0f ? 1.7f : 0f);
            aura.rectTransform.pivot = new Vector2(0.5f, 0.12f);

            // Placa de status sob os pés, deixando o céu da arena livre.
            float left = centerX - PlateWidth * 0.5f;
            float plateY = centerY + 98f;
            var plate = Ui.Frame(parent, "Plate", left, plateY, PlateWidth, PlateHeight, Theme.PanelSolid, Theme.BorderGold, 2f);
            _plateBorder = plate.parent.GetComponent<Image>();
            float iw = PlateWidth - 4f;

            _emblem = Ui.Picture(plate, "Emblem", Art.Diamond, 8, 6, 18, 18);
            _name = Ui.Label(plate, "", 30, 3, iw - 40, 22, Theme.FontHeading - 1, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            _sub = Ui.Label(plate, "", 150, 4, iw - 158, 20, Theme.FontCaption, Theme.Muted, TextAnchor.MiddleRight, FontStyle.Bold);
            _hp = new UiBar(plate, 8, 26, iw - 16, 16, Theme.Good, true);
            _hpText = Ui.Label(plate, "", 8, 26, iw - 22, 16, 12, Color.white, TextAnchor.MiddleRight, FontStyle.Bold, true);
            _pips = new StatusPips(plate, 8, 45, iw - 100, 18);
            _arenaBadge = Ui.Label(plate, "", iw - 96, 45, 88, 18, Theme.FontCaption, Theme.Good, TextAnchor.MiddleRight, FontStyle.Bold);
        }

        public void Show(BattleSnapshot.FighterView fighter, BattleCatalog catalog)
        {
            var spec = fighter.Spec;
            _maxHp = spec.MaxHp;
            _fainted = fighter.Fainted;
            var texture = Art.Fighter(spec.Id, spec.Element, spec.Class);
            _sprite.texture = texture;
            _flash.texture = texture;
            _letter.text = Art.HasSprite(spec.Id) ? "" : Theme.ClassLetter(spec.Class);
            Color element = Theme.ElementColor(spec.Element);
            _name.text = spec.Name;
            // "Guerreiro de Água" já diz classe e elemento; guardiões e chefes ganham a linha de apoio.
            bool selfDescribing = spec.Name.Contains(Theme.ClassName(spec.Class)) && spec.Name.Contains(Theme.ElementName(spec.Element));
            _sub.text = selfDescribing ? "" : Theme.ElementName(spec.Element).ToUpperInvariant() + " · " + Theme.ClassName(spec.Class).ToUpperInvariant();
            _sub.color = Theme.ElementLight(spec.Element);
            _plateBorder.color = Color.Lerp(Theme.BorderGold, element, 0.55f);
            _emblem.color = element;
            _animator.SetIdentity(spec.Class, element);
            _pips.Show(fighter.Effects, catalog);
            _hp.SetInstant(Mathf.Clamp01(fighter.Hp / (float)_maxHp));
            SetHp(fighter.Hp);
            SetAlpha(fighter.Fainted ? 0f : 1f);
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
            _hpText.text = Mathf.RoundToInt(hp) + " / " + _maxHp;
            _animator.SetState(_fainted, !_fainted && fraction <= 0.25f);
        }

        public void SetEffects(Dictionary<string, int> effects, BattleCatalog catalog) { _pips.Show(effects, catalog); }

        private Vector2 _capOffset, _capSquash = Vector2.one;
        private float _capScale = 1f, _capRotation;

        /// <summary>Guarda a pose atual para <see cref="Relax"/> devolvê-la aos poucos para a guarda.</summary>
        public void CapturePose()
        {
            _capOffset = _animator.Offset;
            _capScale = _animator.Scale;
            _capSquash = _animator.Squash;
            _capRotation = _animator.Rotation;
        }

        /// <summary>k = 1 mantém a pose guardada; k = 0 é a guarda.</summary>
        public void Relax(float k)
        {
            _animator.Offset = _capOffset * k;
            _animator.Scale = 1f + (_capScale - 1f) * k;
            _animator.Squash = new Vector2(1f + (_capSquash.x - 1f) * k, 1f + (_capSquash.y - 1f) * k);
            _animator.Rotation = _capRotation * k;
        }

        public void SetOffset(Vector2 offset) { _animator.Offset = offset; }
        public void SetScale(float scale) { _animator.Scale = scale; }
        public void SetSquash(float x, float y) { _animator.Squash = new Vector2(x, y); }
        public void SetRotation(float degrees) { _animator.Rotation = degrees; }
        public void SetAuraBoost(float boost) { _animator.AuraBoost = boost; }
        public void Freeze(float seconds) { _animator.Freeze(seconds); }

        /// <summary>Flash branco de silhueta (impacto). 0 = sem flash.</summary>
        public void Flash(float strength, Color color)
        {
            _flash.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(strength) * _alpha);
        }

        public void SetTint(Color color)
        {
            _tint = color;
            _sprite.color = new Color(color.r, color.g, color.b, _alpha);
        }

        public void SetAlpha(float alpha)
        {
            _alpha = Mathf.Clamp01(alpha);
            _sprite.color = new Color(_tint.r, _tint.g, _tint.b, _alpha);
            var letter = _letter.color;
            letter.a = _alpha;
            _letter.color = letter;
            if (_alpha <= 0f) _flash.color = new Color(1f, 1f, 1f, 0f);
        }

        public void ResetPose()
        {
            SetOffset(Vector2.zero);
            SetScale(1f);
            SetSquash(1f, 1f);
            SetRotation(0f);
            SetTint(Color.white);
            SetAuraBoost(0f);
            Flash(0f, Color.white);
        }
    }

    /// <summary>Efeitos ativos em pips: losango verde/dourado para bônus, vermelho/roxo para efeitos negativos.</summary>
    public sealed class StatusPips
    {
        private const int Max = 3;
        private readonly RawImage[] _gems = new RawImage[Max];
        private readonly Text[] _labels = new Text[Max];
        private readonly float _slot;

        public StatusPips(Transform parent, float x, float y, float w, float h)
        {
            var root = Ui.Node(parent, "Status", x, y, w, h);
            _slot = w / Max;
            for (int i = 0; i < Max; i++)
            {
                _gems[i] = Ui.Picture(root, "Gem" + i, Art.Diamond, i * _slot, 3f, 12f, 12f);
                _labels[i] = Ui.Label(root, "", i * _slot + 14f, 0f, _slot - 16f, h, Theme.FontCaption, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
                _labels[i].horizontalOverflow = HorizontalWrapMode.Overflow;
            }
        }

        public void Show(Dictionary<string, int> effects, BattleCatalog catalog)
        {
            int i = 0;
            if (effects != null)
            {
                foreach (var pair in effects)
                {
                    if (i >= Max) break;
                    var spec = catalog.Effects[pair.Key];
                    Color c = EffectColor(spec);
                    _gems[i].color = c;
                    _labels[i].text = Short(spec.Name) + " " + pair.Value;
                    _labels[i].color = Color.Lerp(c, Color.white, 0.35f);
                    i++;
                }
            }
            for (; i < Max; i++)
            {
                _gems[i].color = new Color(1f, 1f, 1f, 0f);
                _labels[i].text = "";
            }
        }

        public static Color EffectColor(EffectSpec spec)
        {
            if (spec.IsNegative) return spec.Kind == EffectKind.DamageOverTime ? new Color(0.95f, 0.42f, 0.30f, 1f) : new Color(0.72f, 0.48f, 0.95f, 1f);
            return spec.Kind == EffectKind.HealOverTime ? Theme.Good : Theme.Gold;
        }

        /// <summary>Nome do efeito cabendo no pip ("Defesa +10%" vira "Def+").</summary>
        public static string Short(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            string first = name.Split(' ')[0];
            string sign = name.Contains("+") ? "+" : name.Contains("-") || name.Contains("−") ? "−" : "";
            if (first.Length > 7) first = first.Substring(0, 6) + ".";
            return first + sign;
        }
    }

    /// <summary>Painel de um jogador no topo: nome, gauge de stamina e os três lutadores (ativo destacado).</summary>
    public sealed class TeamPanel
    {
        private const float PanelWidth = 414f;
        private const float ChipWidth = 130f;

        private readonly Text _title;
        private readonly UiBar _stamina;
        private readonly Text _staminaText;
        private readonly RawImage _staminaGlow;
        private readonly Image[] _chipBorders;
        private readonly RawImage[] _icons;
        private readonly Text[] _names;
        private readonly UiBar[] _hp;
        private readonly RawImage[][] _effectGems;
        private readonly Text[] _down;
        private readonly RectTransform _root;
        private bool _ready;

        public TeamPanel(Transform parent, float x, float y, string title, int teamSize, bool rightSide)
        {
            Ui.Shadow(parent, x, y, PanelWidth, 102f, 0.45f);
            var panel = Ui.Frame(parent, "TeamPanel", x, y, PanelWidth, 102, Theme.Panel, Theme.BorderGold, 2f);
            _root = panel;
            float iw = PanelWidth - 4f;
            _title = Ui.Label(panel, title, 12, 4, 170, 24, Theme.FontHeading, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold, true);

            _staminaGlow = Ui.Picture(panel, "StaminaGlow", Art.VfxOrb, 170, -6, 240, 44);
            _staminaGlow.color = new Color(1f, 1f, 1f, 0f);
            _stamina = new UiBar(panel, 184, 8, iw - 194, 16, Theme.Stamina, true);
            _staminaText = Ui.Label(panel, "", 184, 8, iw - 194, 16, Theme.FontCaption, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);

            _chipBorders = new Image[teamSize];
            _icons = new RawImage[teamSize];
            _names = new Text[teamSize];
            _hp = new UiBar[teamSize];
            _effectGems = new RawImage[teamSize][];
            _down = new Text[teamSize];
            for (int i = 0; i < teamSize; i++)
            {
                float chipX = 8 + i * (ChipWidth + 6);
                var chip = Ui.Frame(panel, "Chip" + i, chipX, 32, ChipWidth, 62, Theme.PanelLight, Theme.BorderSubtle, 1.5f);
                _chipBorders[i] = chip.parent.GetComponent<Image>();
                _icons[i] = Ui.Picture(chip, "Icon", null, 3, 3, 46, 46);
                _names[i] = Ui.Label(chip, "", 52, 2, ChipWidth - 56, 32, Theme.FontCaption, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
                _hp[i] = new UiBar(chip, 52, 38, ChipWidth - 58, 10, Theme.Good);
                _effectGems[i] = new RawImage[3];
                for (int g = 0; g < 3; g++)
                {
                    _effectGems[i][g] = Ui.Picture(chip, "Fx" + g, Art.Diamond, 4 + g * 11, 48, 9, 9);
                    _effectGems[i][g].color = new Color(1f, 1f, 1f, 0f);
                }
                _down[i] = Ui.Label(chip, "", 0, 0, ChipWidth - 3, 59, Theme.FontSmall, Theme.Bad, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            }
            if (rightSide) _title.alignment = TextAnchor.MiddleLeft;
        }

        public void Refresh(BattleSnapshot.SideView side, BattleCatalog catalog, int staminaMax)
        {
            _stamina.Set(side.Stamina / (float)staminaMax);
            _ready = side.Stamina >= staminaMax;
            _staminaText.text = _ready ? "ESPECIAL PRONTO" : "STAMINA " + side.Stamina + " / " + staminaMax;
            _stamina.SetColor(_ready ? Theme.Gold : Theme.Stamina);
            _staminaGlow.color = _ready ? new Color(Theme.Gold.r, Theme.Gold.g, Theme.Gold.b, 0.30f) : new Color(1f, 1f, 1f, 0f);

            for (int i = 0; i < _chipBorders.Length; i++)
            {
                var fighter = side.Team[i];
                bool active = i == side.ActiveIndex;
                float fraction = fighter.Hp / (float)fighter.Spec.MaxHp;
                _icons[i].texture = Art.Fighter(fighter.Spec.Id, fighter.Spec.Element, fighter.Spec.Class);
                _icons[i].color = fighter.Fainted ? new Color(0.35f, 0.33f, 0.36f, 0.55f) : Color.white;
                _names[i].text = fighter.Spec.Name;
                _names[i].color = fighter.Fainted ? Theme.Muted : active ? Theme.Gold : Theme.Text;
                _hp[i].Set(fraction);
                _hp[i].SetColor(Theme.HpColor(fraction));
                _down[i].text = fighter.Fainted ? "CAÍDO" : "";
                _chipBorders[i].color = fighter.Fainted
                    ? new Color(0.25f, 0.20f, 0.20f, 0.9f)
                    : active ? Theme.Gold : Color.Lerp(Theme.BorderSubtle, Theme.ElementColor(fighter.Spec.Element), 0.45f);

                int g = 0;
                if (!fighter.Fainted)
                    foreach (var pair in fighter.Effects)
                    {
                        if (g >= 3) break;
                        _effectGems[i][g].color = StatusPips.EffectColor(catalog.Effects[pair.Key]);
                        g++;
                    }
                for (; g < 3; g++) _effectGems[i][g].color = new Color(1f, 1f, 1f, 0f);
            }
        }

        /// <summary>Pulso do gauge quando o especial está pronto (chamado pela batalha a cada quadro de animação).</summary>
        public void Pulse(float time)
        {
            if (!_ready) return;
            float p = 0.5f + 0.5f * Mathf.Sin(time * 3.2f);
            _staminaGlow.color = new Color(Theme.Gold.r, Theme.Gold.g, Theme.Gold.b, Mathf.Lerp(0.18f, 0.45f, p));
        }
    }

    /// <summary>Medalhão central: arena atual, o que ela favorece, rodada e contagem do sorteio em losangos.</summary>
    public sealed class ArenaMedallion
    {
        private readonly Image _border;
        private readonly RawImage _rune;
        private readonly Text _name;
        private readonly Text _effect;
        private readonly Text _round;
        private readonly RawImage[] _pips;
        private readonly Text _drawLabel;

        public RectTransform Root { get; private set; }

        public ArenaMedallion(Transform parent, float x, float y, float w, float h, int pipCount)
        {
            Ui.Shadow(parent, x, y, w, h, 0.45f);
            var panel = Ui.Frame(parent, "ArenaMedallion", x, y, w, h, Theme.Panel, Theme.BorderGold, 2.5f);
            Root = panel;
            _border = panel.parent.GetComponent<Image>();
            float iw = w - 5f;
            _rune = Ui.Picture(panel, "Rune", Art.VfxRuneCircle, iw * 0.5f - 70f, -40f, 140f, 140f);
            _rune.color = new Color(1f, 1f, 1f, 0.10f);
            _name = Ui.Label(panel, "", 8, 4, iw - 16, 28, 21, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            _effect = Ui.Label(panel, "", 8, 31, iw - 16, 18, Theme.FontCaption + 1, Theme.Text, TextAnchor.MiddleCenter);
            _round = Ui.Label(panel, "", 12, 54, 120, 22, Theme.FontSmall + 1, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            _drawLabel = Ui.Label(panel, "", 130, 54, iw - 152 - pipCount * 18f, 22, Theme.FontCaption, Theme.Muted, TextAnchor.MiddleRight, FontStyle.Bold);
            _pips = new RawImage[pipCount];
            for (int i = 0; i < pipCount; i++)
                _pips[i] = Ui.Picture(panel, "DrawPip" + i, Art.Diamond, iw - 12 - (pipCount - i) * 18f, 58, 14, 14);
        }

        public void Refresh(ArenaSpec arena, BattleRules rules, int round, int roundsUntilDraw, int interval)
        {
            Color arenaColor = Theme.ArenaColor(arena.Id);
            _border.color = Color.Lerp(Theme.BorderGold, arenaColor, 0.5f);
            _rune.color = new Color(arenaColor.r, arenaColor.g, arenaColor.b, 0.16f);
            _name.text = arena.Name.ToUpperInvariant();
            _name.color = Color.Lerp(Theme.Gold, arenaColor, 0.5f);
            _effect.text = Theme.ArenaEffect(arena, rules);
            _round.text = "RODADA " + round;

            bool fixedArena = interval <= 0 || interval > 99;
            if (fixedArena)
            {
                _drawLabel.text = "ARENA FIXA";
                foreach (var pip in _pips) pip.color = new Color(1f, 1f, 1f, 0f);
                return;
            }
            int elapsed = Mathf.Clamp(interval - roundsUntilDraw, 0, interval);
            _drawLabel.text = roundsUntilDraw <= 1 ? "SORTEIO NA PRÓXIMA" : "SORTEIO EM " + roundsUntilDraw;
            _drawLabel.color = roundsUntilDraw <= 1 ? arenaColor : Theme.Muted;
            for (int i = 0; i < _pips.Length; i++)
            {
                bool lit = i < elapsed + 1 && i < interval;
                bool used = i < interval;
                _pips[i].color = !used ? new Color(1f, 1f, 1f, 0f) : lit ? arenaColor : new Color(0.35f, 0.31f, 0.27f, 0.9f);
            }
        }

        public void SetPreview(string text, Color color)
        {
            _name.text = text;
            _name.color = color;
        }

        public void Spin(float time) { _rune.rectTransform.localRotation = Quaternion.Euler(0f, 0f, time * 8f); }
    }

    /// <summary>Atalho de teclado para um botão (1, 2, 3...). Só dispara se o botão estiver ativo.</summary>
    public sealed class KeyShortcut : MonoBehaviour
    {
        public KeyCode Key;
        private Button _button;

        private void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (_button == null) _button = GetComponent<Button>();
            if (_button != null && _button.interactable && Input.GetKeyDown(Key)) _button.onClick.Invoke();
#endif
        }
    }

    /// <summary>Losango que flutua sobre quem vai agir: deixa claro de quem é a vez.</summary>
    public sealed class TurnMarker : MonoBehaviour
    {
        private RectTransform _rect;
        private Vector2 _home;
        private RawImage _image;

        public void Setup(RawImage image)
        {
            _image = image;
            _rect = image.rectTransform;
            _home = _rect.anchoredPosition;
        }

        private void Update()
        {
            if (_rect == null) return;
            float t = Time.unscaledTime;
            _rect.anchoredPosition = _home + new Vector2(0f, 6f * Mathf.Sin(t * 4f));
            _rect.localRotation = Quaternion.Euler(0f, 0f, 0f);
            var c = _image.color;
            c.a = 0.75f + 0.25f * Mathf.Sin(t * 4f);
            _image.color = c;
        }
    }

    /// <summary>Apaga título e subtítulo de uma carta quando o botão dela está desabilitado.</summary>
    public sealed class CardDimmer : MonoBehaviour
    {
        private Button _button;
        private Text[] _labels;
        private Color[] _colors;

        public void Setup(Button button, params Text[] labels)
        {
            _button = button;
            _labels = labels;
            _colors = new Color[labels.Length];
            for (int i = 0; i < labels.Length; i++) _colors[i] = labels[i].color;
            Update();
        }

        private void Update()
        {
            if (_button == null) return;
            float a = _button.interactable ? 1f : 0.45f;
            for (int i = 0; i < _labels.Length; i++)
                if (_labels[i] != null) _labels[i].color = Theme.WithAlpha(_colors[i], _colors[i].a * a);
        }
    }

    /// <summary>Faixa de narração: cada frase nova entra com um esmaecer rápido.</summary>
    public sealed class TextReveal : MonoBehaviour
    {
        private Text _text;
        private string _last;
        private float _t = 1f;
        private Color _base;

        private void Update()
        {
            if (_text == null) { _text = GetComponent<Text>(); if (_text == null) return; _base = _text.color; _last = _text.text; }
            if (_text.text != _last) { _last = _text.text; _t = 0f; }
            if (_t >= 1f) return;
            _t = Mathf.Min(1f, _t + Time.unscaledDeltaTime / 0.18f);
            _text.color = Theme.WithAlpha(_base, Mathf.Lerp(0.25f, 1f, Ease.OutCubic(_t)));
        }
    }

    /// <summary>Relógio da HUD: chama a função a cada quadro (pulsos e giros sutis).</summary>
    public sealed class HudTicker : MonoBehaviour
    {
        public Action<float> Tick;
        private void Update() { if (Tick != null) Tick(Time.unscaledTime); }
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
