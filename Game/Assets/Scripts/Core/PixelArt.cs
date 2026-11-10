using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// M1 用の仮のドット絵。画像ファイルを使わず、文字の並びからスプライトを作る。
    /// 本番の絵ができたら差し替える。
    /// </summary>
    public static class PixelArt
    {
        public const int PixelsPerUnit = 16;

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => cache.Clear();

        public static Sprite Player => FromArt("Player", PlayerRows, new Dictionary<char, Color32>
        {
            { 'k', new Color32(27, 20, 36, 255) },
            { 'c', new Color32(59, 74, 138, 255) },
            { 's', new Color32(242, 201, 160, 255) },
            { 'e', new Color32(27, 20, 36, 255) },
            { 'o', new Color32(107, 74, 42, 255) },
            { 'l', new Color32(255, 210, 90, 255) },
            { 'b', new Color32(74, 58, 42, 255) },
        });

        public static Sprite Slime => FromArt("Slime", SlimeRows, new Dictionary<char, Color32>
        {
            { 'k', new Color32(20, 40, 26, 255) },
            { 'g', new Color32(79, 191, 90, 255) },
            { 'w', new Color32(200, 255, 208, 255) },
            { 'e', new Color32(16, 32, 26, 255) },
            { 'd', new Color32(47, 138, 60, 255) },
        });

        public static Sprite Fireball => FromArt("Fireball", FireballRows, new Dictionary<char, Color32>
        {
            { 'r', new Color32(240, 110, 40, 255) },
            { 'y', new Color32(255, 214, 90, 255) },
            { 'w', new Color32(255, 250, 230, 255) },
        });

        public static Sprite FloorTile => Bricks("Floor", new Color32(52, 48, 66, 255), new Color32(24, 21, 32, 255));
        public static Sprite WallTile => Bricks("Wall", new Color32(30, 26, 40, 255), new Color32(12, 10, 18, 255));

        static readonly string[] PlayerRows =
        {
            "................",
            "......kkkk......",
            ".....kcccck.....",
            "....kcccccck....",
            "....kcssssck....",
            "....kcesseck....",
            "....kcssssck....",
            ".....kcccck.....",
            "....kcccccck....",
            "...kcccccccck...",
            "...kcccccccckolo",
            "...kcccccccclll.",
            "....kcccccckolo.",
            "....kbk..kbk....",
            "....kbk..kbk....",
            "....kkk..kkk....",
        };

        static readonly string[] SlimeRows =
        {
            "....kkkk....",
            "..kkggggkk..",
            ".kgwggggggk.",
            ".kwwggggggk.",
            "kggggggggggk",
            "kgggeggegggk",
            "kggggggggggk",
            ".kddddddddk.",
            "..kkkkkkkk..",
        };

        static readonly string[] FireballRows =
        {
            "..rr..",
            ".ryyr.",
            "rywwyr",
            "rywwyr",
            ".ryyr.",
            "..rr..",
        };

        static Sprite FromArt(string key, string[] rows, Dictionary<char, Color32> palette)
        {
            if (cache.TryGetValue(key, out var cached)) return cached;

            int h = rows.Length;
            int w = rows[0].Length;
            var pixels = new Color32[w * h];
            var clear = new Color32(0, 0, 0, 0);
            for (int y = 0; y < h; y++)
            {
                string row = rows[h - 1 - y]; // テクスチャは下から上へ並ぶ
                for (int x = 0; x < w; x++)
                {
                    char c = x < row.Length ? row[x] : '.';
                    pixels[y * w + x] = palette.TryGetValue(c, out var col) ? col : clear;
                }
            }
            return Store(key, MakeTexture(key, w, h, pixels, TextureWrapMode.Clamp));
        }

        static Sprite Bricks(string key, Color32 stone, Color32 mortar)
        {
            if (cache.TryGetValue(key, out var cached)) return cached;

            const int size = 16;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool line = y % 8 == 0 || ((y / 8) % 2 == 0 ? x % 8 == 0 : (x + 4) % 8 == 0);
                    if (line)
                    {
                        pixels[y * size + x] = mortar;
                        continue;
                    }
                    int n = (int)(Hash(x, y) % 9) - 4; // わずかなムラ
                    pixels[y * size + x] = new Color32(
                        (byte)Mathf.Clamp(stone.r + n, 0, 255),
                        (byte)Mathf.Clamp(stone.g + n, 0, 255),
                        (byte)Mathf.Clamp(stone.b + n, 0, 255),
                        255);
                }
            }
            return Store(key, MakeTexture(key, size, size, pixels, TextureWrapMode.Repeat));
        }

        static Texture2D MakeTexture(string name, int w, int h, Color32[] pixels, TextureWrapMode wrap)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = wrap,
            };
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }

        static Sprite Store(string key, Texture2D tex)
        {
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f),
                PixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = key;
            cache[key] = sprite;
            return sprite;
        }

        static uint Hash(int x, int y)
        {
            unchecked
            {
                uint h = (uint)x * 73856093u ^ (uint)y * 19349663u;
                h ^= h >> 13;
                h *= 0x5bd1e995u;
                h ^= h >> 15;
                return h;
            }
        }
    }
}
