using System.Collections.Generic;
using ComboTactics.Core;
using UnityEngine;

namespace ComboTactics.UI
{
    /// <summary>
    /// Imagens do jogo. Os fundos e os lutadores vêm de Resources/Art; se a imagem de um lutador não estiver no
    /// projeto, uma forma gerada por código (colorida por elemento e com um formato por classe) entra no lugar.
    /// </summary>
    public static class Art
    {
        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();
        private static readonly HashSet<string> MissingSprites = new HashSet<string>();

        public static Texture2D Menu { get { return Load("Art/menu", new Color(0.10f, 0.09f, 0.14f)); } }
        public static Texture2D CrystalWhole { get { return Load("Art/crystal_whole", new Color(0.95f, 0.80f, 0.35f)); } }
        public static Texture2D CrystalShattered { get { return Load("Art/crystal_shattered", new Color(0.75f, 0.55f, 0.95f)); } }

        public static Texture2D Arena(ArenaId arena)
        {
            switch (arena)
            {
                case ArenaId.Sea: return Load("Art/mar", new Color(0.20f, 0.50f, 0.75f));
                case ArenaId.Volcano: return Load("Art/vulcao", new Color(0.50f, 0.18f, 0.10f));
                case ArenaId.Forest: return Load("Art/floresta", new Color(0.16f, 0.42f, 0.22f));
                default: return Load("Art/lua", new Color(0.14f, 0.14f, 0.30f));
            }
        }

        /// <summary>Se a imagem não estiver no projeto, o jogo segue com uma cor lisa no lugar.</summary>
        private static Texture2D Load(string path, Color fallback)
        {
            Texture2D texture;
            if (Cache.TryGetValue(path, out texture) && texture != null) return texture;
            texture = Resources.Load<Texture2D>(path);
            if (texture == null)
            {
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.SetPixels(new[] { fallback, fallback, fallback, fallback });
                texture.Apply();
            }
            Cache[path] = texture;
            return texture;
        }

        public const string SummonerId = "summoner_mage";

        /// <summary>Verdadeiro quando existe um sprite do personagem em Resources/Art/Characters (arquivo com o id dele).</summary>
        public static bool HasSprite(string characterId)
        {
            return characterId == SummonerId || LoadSprite(characterId) != null;
        }

        /// <summary>Sprite do personagem; sem arquivo para o id, entra a forma gerada por código.</summary>
        public static Texture2D Fighter(string characterId, Element element, CharClass charClass)
        {
            Texture2D sprite = LoadSprite(characterId);
            if (sprite != null) return sprite;
            return characterId == SummonerId ? ProceduralSummoner() : ProceduralFighter(element, charClass);
        }

        /// <summary>Retrato místico do protagonista (Kaelen, o Mago Invocador): manto arcano, olhos astrais e as 3 orbes elementais.</summary>
        private static Texture2D ProceduralSummoner()
        {
            const string key = "fighter/summoner_mage";
            Texture2D texture;
            if (Cache.TryGetValue(key, out texture) && texture != null) return texture;

            const int size = 128;
            var pixels = new Color[size * size];
            Color cloakDark = new Color(0.12f, 0.08f, 0.24f, 1f);
            Color cloakLight = new Color(0.28f, 0.18f, 0.52f, 1f);
            Color gold = new Color(0.98f, 0.80f, 0.32f, 1f);
            Color fireOrb = Theme.ElementColor(Element.Fire);
            Color waterOrb = Theme.ElementColor(Element.Water);
            Color plantOrb = Theme.ElementColor(Element.Plant);

            for (int y = 0; y < size; y++)
            {
                float ny = (y + 0.5f) / size;
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f) / size;
                    float dx = nx - 0.5f;
                    float dy = ny - 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    Color c = Color.clear;

                    // Halo rúnico dourado atrás do Mago Invocador
                    float ringDist = Mathf.Abs(dist - 0.42f);
                    if (ringDist < 0.035f)
                    {
                        float a = Mathf.Clamp01(1f - ringDist / 0.035f);
                        c = new Color(gold.r, gold.g, gold.b, a * 0.85f);
                    }

                    // Silhueta de manto e capuz arcano
                    float hoodWidth = ny < 0.45f ? Mathf.Lerp(0.36f, 0.22f, ny / 0.45f) : Mathf.Lerp(0.24f, 0.04f, (ny - 0.45f) / 0.44f);
                    if (ny >= 0.08f && ny <= 0.88f && Mathf.Abs(dx) <= hoodWidth)
                    {
                        Color robe = Color.Lerp(cloakDark, cloakLight, ny);
                        if (Mathf.Abs(Mathf.Abs(dx) - hoodWidth) < 0.025f) robe = gold;
                        c = robe;

                        // Sombra interna do capuz e olhos astrais brilhantes
                        float faceDist = Mathf.Sqrt(dx * dx + (ny - 0.60f) * (ny - 0.60f) * 1.4f);
                        if (faceDist < 0.11f)
                        {
                            c = new Color(0.05f, 0.03f, 0.10f, 1f);
                            float eyeL = Mathf.Sqrt((dx + 0.04f) * (dx + 0.04f) + (ny - 0.60f) * (ny - 0.60f) * 3f);
                            float eyeR = Mathf.Sqrt((dx - 0.04f) * (dx - 0.04f) + (ny - 0.60f) * (ny - 0.60f) * 3f);
                            if (eyeL < 0.022f || eyeR < 0.022f) c = new Color(0.55f, 0.92f, 1f, 1f);
                        }
                    }

                    // Três orbes elementais (Fogo, Água, Planta) orbitando o Mago
                    float dFire = Mathf.Sqrt((nx - 0.20f) * (nx - 0.20f) + (ny - 0.72f) * (ny - 0.72f));
                    if (dFire < 0.085f) c = Color.Lerp(fireOrb, Color.white, Mathf.Clamp01(1f - dFire / 0.085f) * 0.45f);

                    float dWater = Mathf.Sqrt((nx - 0.80f) * (nx - 0.80f) + (ny - 0.72f) * (ny - 0.72f));
                    if (dWater < 0.085f) c = Color.Lerp(waterOrb, Color.white, Mathf.Clamp01(1f - dWater / 0.085f) * 0.45f);

                    float dPlant = Mathf.Sqrt((nx - 0.50f) * (nx - 0.50f) + (ny - 0.90f) * (ny - 0.90f));
                    if (dPlant < 0.085f) c = Color.Lerp(plantOrb, Color.white, Mathf.Clamp01(1f - dPlant / 0.085f) * 0.45f);

                    pixels[y * size + x] = c;
                }
            }

            texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.SetPixels(pixels);
            texture.Apply();
            Cache[key] = texture;
            return texture;
        }

        private static Texture2D LoadSprite(string characterId)
        {
            if (string.IsNullOrEmpty(characterId) || MissingSprites.Contains(characterId)) return null;
            string path = "Art/Characters/" + characterId;
            Texture2D texture;
            if (Cache.TryGetValue(path, out texture) && texture != null) return texture;
            texture = Resources.Load<Texture2D>(path);
            if (texture == null)
            {
                MissingSprites.Add(characterId);
                return null;
            }
            Cache[path] = texture;
            return texture;
        }

        /// <summary>Fallback procedural: Guerreiro = losango, Mago = círculo, Tanque = octógono, Boss = círculo com anel.</summary>
        private static Texture2D ProceduralFighter(Element element, CharClass charClass)
        {
            string key = "fighter/" + element + "/" + charClass;
            Texture2D texture;
            if (Cache.TryGetValue(key, out texture) && texture != null) return texture;

            const int size = 128;
            float radius = size * 0.46f;
            Color fill = Theme.ElementColor(element);
            Color border = new Color(fill.r * 0.35f, fill.g * 0.35f, fill.b * 0.35f, 1f);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - size * 0.5f);
                    float dy = Mathf.Abs(y + 0.5f - size * 0.5f);
                    float distance;
                    switch (charClass)
                    {
                        case CharClass.Warrior: distance = (dx + dy) * 0.7071f - radius * 0.7071f; break;
                        case CharClass.Tank: distance = Mathf.Max(Mathf.Max(dx, dy) - radius * 0.92f, (dx + dy) * 0.7071f - radius * 0.95f); break;
                        default: distance = Mathf.Sqrt(dx * dx + dy * dy) - radius; break;
                    }

                    float alpha = Mathf.Clamp01(0.5f - distance);
                    float shade = Mathf.Lerp(0.78f, 1.12f, y / (float)size);
                    Color color = new Color(fill.r * shade, fill.g * shade, fill.b * shade, 1f);
                    if (distance > -7f) color = border;
                    else if (charClass == CharClass.Neutral && distance > -22f && distance < -15f) color = border;
                    color.a = alpha;
                    pixels[y * size + x] = color;
                }
            }

            texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.SetPixels(pixels);
            texture.Apply();
            Cache[key] = texture;
            return texture;
        }

        // ── Texturas procedurais de efeitos visuais (VFX) ────────────────────────

        /// <summary>Esfera suave com brilho central (projéteis, bolas de fogo, esporos, partículas).</summary>
        public static Texture2D VfxOrb
        {
            get
            {
                return BuildVfx("vfx/orb", 96, (nx, ny) =>
                {
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    return Mathf.Pow(Mathf.Clamp01(1f - r), 1.6f);
                });
            }
        }

        /// <summary>Anel luminoso (ondas de choque, escudos, pulsos gravitacionais).</summary>
        public static Texture2D VfxRing
        {
            get
            {
                return BuildVfx("vfx/ring", 128, (nx, ny) =>
                {
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    float ring = 1f - Mathf.Abs(r - 0.72f) / 0.18f;
                    float fill = Mathf.Clamp01(0.85f - r) * 0.25f;
                    return Mathf.Clamp01(Mathf.Max(ring, fill));
                });
            }
        }

        /// <summary>Arco diagonal de corte (espada, lança, tridente, machado, garras).</summary>
        public static Texture2D VfxSlash
        {
            get
            {
                return BuildVfx("vfx/slash", 128, (nx, ny) =>
                {
                    // Rotaciona 45 graus para formar um crescente diagonal.
                    float u = (nx + ny) * 0.7071f;
                    float v = (ny - nx) * 0.7071f;
                    float curve = v + 0.28f * u * u - 0.08f;
                    float thickness = 0.16f * (1f - 0.75f * u * u);
                    if (thickness <= 0.01f) return 0f;
                    float alpha = 1f - Mathf.Abs(curve) / thickness;
                    float ends = Mathf.Clamp01((0.92f - Mathf.Abs(u)) * 3.5f);
                    return Mathf.Clamp01(alpha) * ends;
                });
            }
        }

        /// <summary>Coluna/pilar vertical com base larga (gêiser, erupção vulcânica, raízes, pilar lunar).</summary>
        public static Texture2D VfxPillar
        {
            get
            {
                return BuildVfx("vfx/pillar", 128, (nx, ny) =>
                {
                    float y01 = (ny + 1f) * 0.5f;
                    float width = Mathf.Lerp(0.68f, 0.32f, y01);
                    float horiz = 1f - Mathf.Abs(nx) / width;
                    float vert = Mathf.Sin(y01 * Mathf.PI);
                    return Mathf.Clamp01(horiz * 1.4f) * Mathf.Pow(Mathf.Clamp01(vert), 0.6f);
                });
            }
        }

        /// <summary>Cúpula/escudo protetor para habilidades defensivas e barreiras.</summary>
        public static Texture2D VfxShield
        {
            get
            {
                return BuildVfx("vfx/shield", 128, (nx, ny) =>
                {
                    float r = Mathf.Sqrt(nx * nx * 1.15f + ny * ny * 0.9f);
                    if (r > 0.95f) return 0f;
                    float rim = Mathf.Clamp01(1f - Mathf.Abs(r - 0.82f) / 0.14f);
                    float dome = Mathf.Clamp01((0.85f - r) * 0.45f) * Mathf.Clamp01((ny + 0.6f) * 0.9f);
                    return Mathf.Clamp01(rim + dome);
                });
            }
        }

        /// <summary>Lâmina/folha/estilhaço pontiagudo (folhas cortantes, estilhaços de gelo, espinhos).</summary>
        public static Texture2D VfxShard
        {
            get
            {
                return BuildVfx("vfx/shard", 64, (nx, ny) =>
                {
                    float d = Mathf.Abs(ny) / Mathf.Max(0.05f, 0.38f * (1f - nx * nx));
                    float len = Mathf.Clamp01((0.95f - Mathf.Abs(nx)) * 3f);
                    return Mathf.Clamp01(1f - d) * len;
                });
            }
        }

        /// <summary>Círculo rúnico com marcas angulares, eco das plataformas gravadas das arenas (conjuração, seleção, especial).</summary>
        public static Texture2D VfxRuneCircle
        {
            get
            {
                return BuildVfx("vfx/runecircle", 256, (nx, ny) =>
                {
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    float ang = Mathf.Atan2(ny, nx);
                    float outer = Mathf.Clamp01(1f - Mathf.Abs(r - 0.90f) / 0.025f);
                    float inner = Mathf.Clamp01(1f - Mathf.Abs(r - 0.66f) / 0.018f);
                    // Marcas rúnicas: 24 traços curtos entre os dois anéis, alternando comprimento.
                    float tickPhase = Mathf.Abs(Mathf.Repeat(ang / (Mathf.PI * 2f) * 24f, 1f) - 0.5f);
                    float tickBand = (r > 0.72f && r < 0.86f) ? 1f : 0f;
                    float tick = tickBand * Mathf.Clamp01(1f - tickPhase / 0.06f);
                    // Losangos nos 4 pontos cardeais (motivo do brasão das 3 forças + Núcleo).
                    float diamond = 0f;
                    for (int k = 0; k < 4; k++)
                    {
                        float a = k * Mathf.PI * 0.5f;
                        float dx = nx - Mathf.Cos(a) * 0.78f, dy = ny - Mathf.Sin(a) * 0.78f;
                        diamond = Mathf.Max(diamond, Mathf.Clamp01(1f - (Mathf.Abs(dx) + Mathf.Abs(dy)) / 0.09f) * 1.6f);
                    }
                    float glow = Mathf.Clamp01(0.95f - r) * 0.10f;
                    return Mathf.Clamp01(Mathf.Max(Mathf.Max(outer, inner * 0.85f), Mathf.Max(tick * 0.8f, diamond)) + glow);
                });
            }
        }

        /// <summary>Linhas de velocidade horizontais para o cut-in do Golpe Especial.</summary>
        public static Texture2D VfxStreaks
        {
            get
            {
                return BuildVfx("vfx/streaks", 128, (nx, ny) =>
                {
                    float row = Mathf.Floor((ny + 1f) * 22f);
                    float hash = Mathf.Repeat(Mathf.Sin(row * 12.9898f) * 43758.5453f, 1f);
                    if (hash < 0.45f) return 0f;
                    float start = hash * 1.4f - 0.9f;
                    float len = 0.35f + hash * 0.6f;
                    float u = (nx - start) / len;
                    if (u < 0f || u > 1f) return 0f;
                    float band = 1f - Mathf.Abs(Mathf.Repeat((ny + 1f) * 22f, 1f) - 0.5f) * 2.4f;
                    return Mathf.Clamp01(band) * Mathf.Sin(u * Mathf.PI) * (0.5f + hash * 0.5f);
                });
            }
        }

        // ── Sprites procedurais de interface (9-slice): identidade "Bronze Forjado das Arenas" ──
        //
        // Todos são em tons de cinza e recebem a cor pelo Image.color; assim um mesmo desenho serve
        // para a moldura de bronze, a borda elemental de um lutador e o estado de foco dourado.

        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        /// <summary>Moldura chanfrada de metal forjado: aresta escura, bisel claro no topo e sulco interno.</summary>
        public static Sprite FrameSprite
        {
            get
            {
                return BuildSliced("ui/frame", 32, 10, 3.5f, (d, edge, cornerFade) =>
                {
                    if (d < 1f) return new Color(0.16f, 0.14f, 0.12f, 1f);
                    if (d < 3.2f)
                    {
                        // Bisel: a luz vem de cima, a base fica na sombra.
                        float shade = edge == 0 ? 1f : edge == 3 ? 0.58f : 0.80f;
                        float bevel = Mathf.Lerp(1f, 0.82f, (d - 1f) / 2.2f);
                        float v = shade * bevel;
                        return new Color(v, v, v, 1f);
                    }
                    if (d < 4.2f) return new Color(0.22f, 0.20f, 0.18f, 0.95f);
                    return new Color(0f, 0f, 0f, 0f);
                });
            }
        }

        /// <summary>Fundo de painel em pedra escura: leve gradiente vertical e sombra interna nas bordas.</summary>
        public static Sprite FillSprite
        {
            get
            {
                return BuildSliced("ui/fill", 32, 10, 2f, (d, edge, cornerFade) =>
                {
                    float inner = Mathf.Lerp(0.70f, 1f, Mathf.Clamp01(d / 7f));
                    float vertical = edge == 0 ? 1.0f : edge == 3 ? 0.86f : 0.94f;
                    float v = inner * vertical;
                    return new Color(v, v, v, 1f);
                });
            }
        }

        /// <summary>Placa de botão em relevo: topo iluminado, base mais densa e filete de luz superior.</summary>
        public static Sprite ButtonSprite
        {
            get
            {
                return BuildSliced("ui/button", 32, 11, 5f, (d, edge, cornerFade) =>
                {
                    if (d < 1f) return new Color(0.30f, 0.27f, 0.24f, 1f);
                    if (d < 2f && edge == 0) return new Color(1f, 1f, 1f, 1f);
                    if (d < 2.2f && edge == 3) return new Color(0.50f, 0.48f, 0.46f, 1f);
                    float v = edge == 0 ? 1f : edge == 3 ? 0.80f : 0.90f;
                    return new Color(v, v, v, 1f);
                });
            }
        }

        /// <summary>Halo suave de borda para o brilho de foco/hover dos botões.</summary>
        public static Sprite GlowSprite
        {
            get
            {
                return BuildSliced("ui/glow", 48, 18, 10f, (d, edge, cornerFade) =>
                {
                    float a = d < 6f ? Mathf.Clamp01(d / 2.5f) * Mathf.Clamp01((14f - d) / 8f) : Mathf.Clamp01((14f - d) / 8f);
                    return new Color(1f, 1f, 1f, a * 0.85f);
                });
            }
        }

        /// <summary>Canaleta arredondada das barras (HP, Stamina) e preenchimento com brilho superior.</summary>
        public static Sprite BarSprite
        {
            get
            {
                return BuildSliced("ui/bar", 16, 6, 4f, (d, edge, cornerFade) =>
                {
                    float v = edge == 0 ? (d < 3f ? 1f : 0.88f) : edge == 3 ? 0.70f : 0.84f;
                    return new Color(v, v, v, 1f);
                });
            }
        }

        /// <summary>Losango com aresta suave: rebites das molduras, pips de rodada e emblemas de elemento.</summary>
        public static Texture2D Diamond
        {
            get
            {
                return BuildVfx("ui/diamond", 48, (nx, ny) =>
                {
                    float d = Mathf.Abs(nx) + Mathf.Abs(ny);
                    return Mathf.Clamp01((0.92f - d) * 12f);
                });
            }
        }

        /// <summary>Gradiente vertical (opaco embaixo, transparente em cima) para vinhetas e faixas de cena.</summary>
        public static Texture2D GradientUp
        {
            get
            {
                const string key = "ui/gradient_up";
                Texture2D texture;
                if (Cache.TryGetValue(key, out texture) && texture != null) return texture;
                const int h = 64;
                var pixels = new Color[h * 2];
                for (int y = 0; y < h; y++)
                {
                    float t = 1f - y / (float)(h - 1);
                    float a = t * t * (3f - 2f * t);
                    pixels[y * 2] = new Color(1f, 1f, 1f, a);
                    pixels[y * 2 + 1] = new Color(1f, 1f, 1f, a);
                }
                texture = new Texture2D(2, h, TextureFormat.RGBA32, false);
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                texture.SetPixels(pixels);
                texture.Apply();
                Cache[key] = texture;
                return texture;
            }
        }

        /// <summary>
        /// Gera um sprite 9-slice. <paramref name="shade"/> recebe a distância até a borda mais próxima (em pixels),
        /// qual borda é (0 = topo, 1 = esquerda, 2 = direita, 3 = base) e devolve a cor em tons de cinza.
        /// </summary>
        private static Sprite BuildSliced(string key, int size, int border, float cornerRadius, System.Func<float, int, float, Color> shade)
        {
            Sprite sprite;
            if (SpriteCache.TryGetValue(key, out sprite) && sprite != null) return sprite;

            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    // Linha y=0 da textura é a base da imagem no Unity.
                    float dTop = size - py, dBottom = py, dLeft = px, dRight = size - px;
                    float d = Mathf.Min(Mathf.Min(dTop, dBottom), Mathf.Min(dLeft, dRight));
                    int edge = d == dTop ? 0 : d == dBottom ? 3 : d == dLeft ? 1 : 2;

                    // Cantos arredondados: distância até o arco do canto.
                    float cx = Mathf.Clamp(px, cornerRadius, size - cornerRadius);
                    float cy = Mathf.Clamp(py, cornerRadius, size - cornerRadius);
                    float cornerDist = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
                    float coverage = 1f;
                    if (cornerDist > 0.001f)
                    {
                        float rounded = cornerRadius - cornerDist;
                        coverage = Mathf.Clamp01(rounded + 0.5f);
                        d = Mathf.Min(d, Mathf.Max(0f, rounded));
                    }

                    Color c = shade(d, edge, coverage);
                    c.a *= coverage;
                    pixels[y * size + x] = c;
                }
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.SetPixels(pixels);
            texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            SpriteCache[key] = sprite;
            return sprite;
        }

        // ── Silhueta sólida dos lutadores (flash de impacto e aura de contorno) ──

        private static Material _silhouetteMaterial;
        private static bool _silhouetteResolved;

        /// <summary>
        /// Material que pinta só o alfa da textura com a cor do Graphic (shader de texto embutido do Unity).
        /// Permite o "flash branco" clássico de impacto sem precisar de sprites extras. Nulo se o shader não existir.
        /// </summary>
        public static Material SilhouetteMaterial
        {
            get
            {
                if (_silhouetteResolved) return _silhouetteMaterial;
                _silhouetteResolved = true;
                var shader = Shader.Find("GUI/Text Shader");
                // A fonte embutida usa esse mesmo shader no material dela: garante que ele exista no build WebGL.
                if (shader == null && Ui.Font != null && Ui.Font.material != null) shader = Ui.Font.material.shader;
                if (shader != null) _silhouetteMaterial = new Material(shader) { name = "FighterSilhouette" };
                return _silhouetteMaterial;
            }
        }

        private static Texture2D BuildVfx(string key, int size, System.Func<float, float, float> shape)
        {
            Texture2D texture;
            if (Cache.TryGetValue(key, out texture) && texture != null) return texture;

            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                float ny = (y + 0.5f) / (size * 0.5f) - 1f;
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f) / (size * 0.5f) - 1f;
                    float a = Mathf.Clamp01(shape(nx, ny));
                    float core = Mathf.Clamp01((a - 0.55f) * 2.2f);
                    pixels[y * size + x] = new Color(Mathf.Lerp(1f, 1f, core), Mathf.Lerp(0.92f, 1f, core), Mathf.Lerp(0.82f, 1f, core), a);
                }
            }

            texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.SetPixels(pixels);
            texture.Apply();
            Cache[key] = texture;
            return texture;
        }
    }
}
