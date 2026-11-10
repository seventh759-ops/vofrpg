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

        public static Sprite Player => PlayerFor(ClassType.Lampwright);

        /// <summary>職業ごとの見た目。同じ形で、外套と手に持つ物の色を変える（仮）。</summary>
        public static Sprite PlayerFor(ClassType type)
        {
            Color32 cloak, held, glow;
            switch (type)
            {
                case ClassType.Shadeblade:
                    cloak = new Color32(46, 40, 60, 255); held = new Color32(120, 120, 140, 255); glow = new Color32(220, 225, 240, 255);
                    break;
                case ClassType.Warden:
                    cloak = new Color32(130, 58, 44, 255); held = new Color32(150, 110, 50, 255); glow = new Color32(255, 180, 70, 255);
                    break;
                default:
                    cloak = new Color32(59, 74, 138, 255); held = new Color32(107, 74, 42, 255); glow = new Color32(255, 210, 90, 255);
                    break;
            }
            return FromArt($"Player_{type}", PlayerRows, new Dictionary<char, Color32>
            {
                { 'k', new Color32(27, 20, 36, 255) },
                { 'c', cloak },
                { 's', new Color32(242, 201, 160, 255) },
                { 'e', new Color32(27, 20, 36, 255) },
                { 'o', held },
                { 'l', glow },
                { 'b', new Color32(74, 58, 42, 255) },
            });
        }

        /// <summary>斬撃の扇（120 度、半径 1 ユニット、右向き）。白で作り、色は SpriteRenderer で付ける。</summary>
        public static Sprite SlashArc
        {
            get
            {
                const string key = "SlashArc";
                if (cache.TryGetValue(key, out var cached)) return cached;
                const int size = 32;
                var px = new Color32[size * size];
                float c = (size - 1) * 0.5f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x - c, dy = y - c;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float ang = Mathf.Abs(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg);
                        bool on = d <= c && d >= c * 0.55f && ang <= 60f;
                        byte a = on ? (byte)(255 * Mathf.Clamp01((d - c * 0.55f) / (c * 0.45f))) : (byte)0;
                        px[y * size + x] = new Color32(255, 255, 255, a);
                    }
                }
                return Store(key, MakeTexture(key, size, size, px, TextureWrapMode.Clamp));
            }
        }

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

        public static Sprite Ember => FromArt("Ember", EmberRows, new Dictionary<char, Color32>
        {
            { 'y', new Color32(255, 220, 120, 255) },
            { 'r', new Color32(255, 120, 40, 255) },
        });

        /// <summary>地面に落ちた装備のアイコン。r の部分が珍しさの色になる。</summary>
        public static Sprite ItemIcon(ItemSlot slot, Rarity rarity)
        {
            string[] rows;
            switch (slot)
            {
                case ItemSlot.Weapon: rows = WeaponIconRows; break;
                case ItemSlot.Lantern: rows = LanternIconRows; break;
                case ItemSlot.Armor: rows = ArmorIconRows; break;
                default: rows = RingIconRows; break;
            }
            Color32 c = RarityInfo.ColorOf(rarity);
            return FromArt($"Icon_{slot}_{rarity}", rows, new Dictionary<char, Color32>
            {
                { 'k', new Color32(16, 12, 22, 255) },
                { 'r', c },
                { 'y', new Color32(255, 230, 140, 255) },
                { 'g', new Color32(220, 180, 70, 255) },
            });
        }

        /// <summary>珍しい装備から立ちのぼる光の柱。下が濃く上が薄い白。足元が原点。</summary>
        public static Sprite Beam
        {
            get
            {
                const string key = "Beam";
                if (cache.TryGetValue(key, out var cached)) return cached;
                const int h = 32;
                var px = new Color32[h];
                for (int y = 0; y < h; y++) px[y] = new Color32(255, 255, 255, (byte)(255 * (1f - (float)y / h)));
                var tex = MakeTexture(key, 1, h, px, TextureWrapMode.Clamp);
                var sprite = Sprite.Create(tex, new Rect(0, 0, 1, h), new Vector2(0.5f, 0f), PixelsPerUnit, 0, SpriteMeshType.FullRect);
                sprite.name = key;
                cache[key] = sprite;
                return sprite;
            }
        }

        /// <summary>爆発などの広がる輪。白で作り、色は SpriteRenderer で付ける。</summary>
        public static Sprite Ring
        {
            get
            {
                const string key = "Ring";
                if (cache.TryGetValue(key, out var cached)) return cached;
                const int size = 32;
                var px = new Color32[size * size];
                float c = (size - 1) * 0.5f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                        bool on = d < c && d > c - 3f;
                        px[y * size + x] = on ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                    }
                }
                return Store(key, MakeTexture(key, size, size, px, TextureWrapMode.Clamp));
            }
        }

        /// <summary>クォータービューの床（32×16 のひし形）。</summary>
        public static Sprite IsoFloor => IsoTile("IsoFloor", new Color32(56, 52, 72, 255), new Color32(30, 27, 40, 255));
        public static Sprite IsoFloorAlt => IsoTile("IsoFloorAlt", new Color32(50, 46, 64, 255), new Color32(30, 27, 40, 255));

        /// <summary>クォータービューの壁ブロック（32×32。上面のひし形と左右の側面）。足元のひし形の中心が原点。</summary>
        public static Sprite IsoWall
        {
            get
            {
                const string key = "IsoWall";
                if (cache.TryGetValue(key, out var cached)) return cached;
                const int w = 32, h = 32;
                var px = new Color32[w * h];
                var clear = new Color32(0, 0, 0, 0);
                var top = new Color32(74, 66, 96, 255);
                var left = new Color32(44, 38, 60, 255);
                var right = new Color32(30, 26, 42, 255);
                var edge = new Color32(16, 12, 24, 255);
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        float dx = Mathf.Abs(x - 15.5f) / 16f;
                        // 上面：中心 (15.5, 23.5)、横の半径 16、縦の半径 8
                        float dTop = dx + Mathf.Abs(y - 23.5f) / 8f;
                        float lowerEdge = 23.5f - 8f * (1f - dx); // 上面の下側の縁
                        Color32 c = clear;
                        if (dTop <= 1f) c = dTop > 0.88f ? edge : top;
                        else if (y < lowerEdge && y >= lowerEdge - 16f)
                        {
                            bool rim = y < lowerEdge - 15f || x == 0 || x == w - 1;
                            c = rim ? edge : (x < 16 ? left : right);
                            if (!rim && (int)(lowerEdge - y) % 8 == 0) c = edge; // 石積みの目地
                        }
                        px[y * w + x] = c;
                    }
                }
                var tex = MakeTexture(key, w, h, px, TextureWrapMode.Clamp);
                var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 7.5f / h), PixelsPerUnit, 0, SpriteMeshType.FullRect);
                sprite.name = key;
                cache[key] = sprite;
                return sprite;
            }
        }

        /// <summary>手前側の低い縁（高さ4ドットの段）。足元のひし形の中心が原点。</summary>
        public static Sprite IsoWallLow
        {
            get
            {
                const string key = "IsoWallLow";
                if (cache.TryGetValue(key, out var cached)) return cached;
                const int w = 32, h = 20, side = 4;
                var px = new Color32[w * h];
                var clear = new Color32(0, 0, 0, 0);
                var top = new Color32(64, 58, 84, 255);
                var left = new Color32(40, 35, 55, 255);
                var right = new Color32(28, 24, 40, 255);
                var edge = new Color32(16, 12, 24, 255);
                float cy = 7.5f + side;
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        float dx = Mathf.Abs(x - 15.5f) / 16f;
                        float dTop = dx + Mathf.Abs(y - cy) / 8f;
                        float lowerEdge = cy - 8f * (1f - dx);
                        Color32 c = clear;
                        if (dTop <= 1f) c = dTop > 0.85f ? edge : top;
                        else if (y < lowerEdge && y >= lowerEdge - side) c = y < lowerEdge - side + 1 ? edge : (x < 16 ? left : right);
                        px[y * w + x] = c;
                    }
                }
                var tex = MakeTexture(key, w, h, px, TextureWrapMode.Clamp);
                var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, (cy - side) / h), PixelsPerUnit, 0, SpriteMeshType.FullRect);
                sprite.name = key;
                cache[key] = sprite;
                return sprite;
            }
        }

        /// <summary>下り階段（床のひし形に、段の線と金色の縁）。</summary>
        public static Sprite Stairs
        {
            get
            {
                const string key = "Stairs";
                if (cache.TryGetValue(key, out var cached)) return cached;
                const int w = 32, h = 16;
                var px = new Color32[w * h];
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        float d = Mathf.Abs(x - 15.5f) / 16f + Mathf.Abs(y - 7.5f) / 8f;
                        Color32 c = new Color32(0, 0, 0, 0);
                        if (d <= 1f)
                        {
                            // 奥へ下っていく段：斜めの縞と、中ほどほど暗く
                            bool step = ((x + 2 * y) / 5) % 2 == 0;
                            byte v = (byte)(step ? 70 : 40);
                            c = new Color32(v, (byte)(v * 0.8f), (byte)(v * 0.5f), 255);
                            if (d > 0.82f) c = new Color32(255, 205, 90, 255);
                        }
                        px[y * w + x] = c;
                    }
                }
                return Store(key, MakeTexture(key, w, h, px, TextureWrapMode.Clamp));
            }
        }

        static Sprite IsoTile(string key, Color32 stone, Color32 edge)
        {
            if (cache.TryGetValue(key, out var cached)) return cached;
            const int w = 32, h = 16;
            var px = new Color32[w * h];
            var clear = new Color32(0, 0, 0, 0);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d = Mathf.Abs(x - 15.5f) / 16f + Mathf.Abs(y - 7.5f) / 8f;
                    if (d > 1f)
                    {
                        px[y * w + x] = clear;
                        continue;
                    }
                    if (d > 0.9f)
                    {
                        px[y * w + x] = edge;
                        continue;
                    }
                    int n = (int)(Hash(x, y) % 9) - 4;
                    px[y * w + x] = new Color32(
                        (byte)Mathf.Clamp(stone.r + n, 0, 255),
                        (byte)Mathf.Clamp(stone.g + n, 0, 255),
                        (byte)Mathf.Clamp(stone.b + n, 0, 255),
                        255);
                }
            }
            return Store(key, MakeTexture(key, w, h, px, TextureWrapMode.Clamp));
        }

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

        static readonly string[] EmberRows =
        {
            "..y..",
            ".yry.",
            "yrrry",
            ".yry.",
            "..y..",
        };

        static readonly string[] WeaponIconRows =
        {
            "......kr",
            ".....krr",
            "....krk.",
            "...krk..",
            "..krk...",
            ".krk....",
            "krk.....",
            "kk......",
        };

        static readonly string[] LanternIconRows =
        {
            "..kkkk..",
            "..k..k..",
            ".kkkkkk.",
            ".krrrrk.",
            ".kryyrk.",
            ".krrrrk.",
            ".kkkkkk.",
            "........",
        };

        static readonly string[] ArmorIconRows =
        {
            ".kk..kk.",
            "krrkkrrk",
            "krrrrrrk",
            ".krrrrk.",
            ".krrrrk.",
            ".krrrrk.",
            "..kkkk..",
            "........",
        };

        static readonly string[] RingIconRows =
        {
            "...kk...",
            "..krrk..",
            "...kk...",
            "..kggk..",
            ".kg..gk.",
            ".kg..gk.",
            "..kggk..",
            "...kk...",
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
