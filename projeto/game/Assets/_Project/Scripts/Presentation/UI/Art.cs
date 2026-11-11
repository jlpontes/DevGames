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

        /// <summary>Verdadeiro quando existe um sprite do personagem em Resources/Art/Characters (arquivo com o id dele).</summary>
        public static bool HasSprite(string characterId)
        {
            return LoadSprite(characterId) != null;
        }

        /// <summary>Sprite do personagem; sem arquivo para o id, entra a forma gerada por código.</summary>
        public static Texture2D Fighter(string characterId, Element element, CharClass charClass)
        {
            Texture2D sprite = LoadSprite(characterId);
            return sprite != null ? sprite : ProceduralFighter(element, charClass);
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
    }
}
