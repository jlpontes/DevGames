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
            if (characterId == SummonerId) return ProceduralSummoner();
            Texture2D sprite = LoadSprite(characterId);
            return sprite != null ? sprite : ProceduralFighter(element, charClass);
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
