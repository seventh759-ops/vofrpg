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
        /// <summary>
        /// 職業ごとの見た目（32ドット。1マスに収まるよう半分の縮尺で描く）。
        /// 形は部品（素材の文字）で描き、濃淡・影・輪郭は Shaded で自動で付ける。
        /// </summary>
        public static Sprite PlayerFor(ClassType type)
        {
            switch (type)
            {
                case ClassType.Shadeblade: return Shaded("PlayerHd_Shadeblade", ShadebladeHdRows);
                case ClassType.Warden: return Shaded("PlayerHd_Warden", WardenHdRows);
                default: return Shaded("PlayerHd_Lampwright", LampwrightHdRows);
            }
        }

        /// <summary>素材ごとの4段階の色（明るい縁、ふつう、影、深い影）。</summary>
        static readonly Dictionary<char, Color32[]> ramps = new Dictionary<char, Color32[]>
        {
            { 's', Ramp(255, 226, 196, 240, 196, 156, 206, 150, 116, 160, 108, 88) },   // 肌
            { 'c', Ramp(110, 132, 214, 70, 92, 170, 48, 62, 124, 32, 40, 86) },         // 青いローブ
            { 'd', Ramp(88, 106, 180, 56, 72, 140, 38, 48, 104, 26, 32, 72) },          // ローブの合わせ
            { 'w', Ramp(176, 128, 80, 132, 92, 54, 98, 66, 38, 70, 46, 26) },           // 木
            { 'm', Ramp(236, 240, 250, 184, 192, 210, 132, 140, 160, 92, 98, 116) },    // 鉄
            { 'g', Ramp(255, 234, 140, 226, 186, 72, 170, 130, 40, 120, 88, 24) },      // 金
            { 'r', Ramp(236, 96, 96, 192, 54, 60, 140, 34, 42, 96, 22, 30) },           // 赤
            { 'n', Ramp(96, 90, 120, 66, 60, 88, 46, 42, 64, 32, 28, 46) },             // 黒い布
            { 'b', Ramp(132, 100, 74, 96, 70, 50, 70, 50, 36, 48, 34, 24) },            // 革・靴
            { 'v', Ramp(200, 96, 72, 160, 64, 48, 118, 44, 34, 84, 30, 24) },           // 陣羽織
            { 'e', Ramp(30, 22, 40, 30, 22, 40, 30, 22, 40, 30, 22, 40) },              // 目
        };

        /// <summary>影を付けずに光らせる素材（ランタン・香炉の火）。</summary>
        static readonly Dictionary<char, Color32> glows = new Dictionary<char, Color32>
        {
            { 'l', new Color32(255, 230, 130, 255) },
            { 'L', new Color32(255, 170, 60, 255) },
            { 'o', new Color32(255, 250, 220, 255) },
        };

        static Color32[] Ramp(params int[] v)
        {
            var r = new Color32[4];
            for (int i = 0; i < 4; i++) r[i] = new Color32((byte)v[i * 3], (byte)v[i * 3 + 1], (byte)v[i * 3 + 2], 255);
            return r;
        }

        /// <summary>
        /// 部品で描いた絵に、濃淡と影と輪郭を付ける。光は左上から：
        /// 左か上が別の部品なら明るい縁、右か下が別の部品なら影、右下の角はさらに暗く、大きな面の右下寄りも暗くする。
        /// </summary>
        static Sprite Shaded(string key, string[] rows)
        {
            if (cache.TryGetValue(key, out var cached)) return cached;
            int h = rows.Length, w = rows[0].Length;
            var outline = new Color32(22, 16, 30, 255);
            var clear = new Color32(0, 0, 0, 0);
            var px = new Color32[w * h];

            char At(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && x < rows[y].Length ? rows[y][x] : '.';
            bool Same(int x, int y, char ch) => At(x, y) == ch;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    char ch = At(x, y);
                    Color32 c;
                    if (ch == '.')
                    {
                        bool edge = At(x + 1, y) != '.' || At(x - 1, y) != '.' || At(x, y + 1) != '.' || At(x, y - 1) != '.';
                        c = edge ? outline : clear;
                    }
                    else if (glows.TryGetValue(ch, out var glow))
                    {
                        c = glow;
                    }
                    else if (ramps.TryGetValue(ch, out var ramp))
                    {
                        bool lit = !Same(x - 1, y, ch) || !Same(x, y - 1, ch);
                        bool dark = !Same(x + 1, y, ch) || !Same(x, y + 1, ch);
                        bool deep = dark && !Same(x + 1, y + 1, ch);
                        int k = 1;
                        while (k < 6 && Same(x - k, y - k, ch)) k++;
                        int j = 1;
                        while (j < 6 && Same(x + j, y + j, ch)) j++;
                        int tone = 1;
                        if (lit && !dark) tone = 0;
                        else if (deep) tone = 3;
                        else if (dark || k > j + 2) tone = 2;
                        c = ramp[tone];
                    }
                    else
                    {
                        c = clear;
                    }
                    px[(h - 1 - y) * w + x] = c; // テクスチャは下から上へ並ぶ
                }
            }
            // 32ドットを1マスに収める（ほかの絵の半分の縮尺）
            return Store(key, MakeTexture(key, w, h, px, TextureWrapMode.Clamp), w);
        }

        static readonly string[] LampwrightHdRows =
        {
            "..............cc.......mm.......",
            "..............cc.......mm.......",
            "..............ccc....mmmmmm.....",
            "...............cc....mmmmmm.....",
            ".............cccc....mllllm.....",
            "...........cccccccc..mloolm.....",
            "..........cccccccccc.mloolm.....",
            "..........cccccccccc.mloolm.....",
            ".........cccccsscccccmllllm.....",
            ".........cccsssssscccmmmmmm.....",
            ".........cccsssssscccmmmmmm.....",
            ".........ccssessesscc..ww.......",
            ".........ccsssssssscc..ww.......",
            "..........ccsssssscc...ww.......",
            "..........ccsssssscc...ww.......",
            "...........cccccccc....ww.......",
            "..........cccccccccc...ww.......",
            ".........ddccccccccccc.ww.......",
            ".........ddcccccccccccc.ww......",
            "........dddcccccccccccc.ww......",
            "........ddccccccccccccc.ww......",
            ".......dddccccccccccccccww......",
            ".......dsssccddddccccsssww......",
            "........sssccddddccccsssww......",
            "........cccccddddccccsssww......",
            ".......ccccccddddcccccc.ww......",
            ".......cccccdddddcccccc.ww......",
            ".......cccccddddddccccc.ww......",
            ".......cccccddddddccccc.ww......",
            "......ccccccddddddccccccww......",
            "......cccbbbbddddbbbbcccww......",
            ".........bbbb....bbbb...ww......",
        };

        static readonly string[] ShadebladeHdRows =
        {
            "................................",
            "................................",
            "................................",
            "...............n................",
            "............nnnnnnn.............",
            "...........nnnnnnnnn............",
            "...........nnnnnnnnn..........mm",
            "..........nnnnsssnnnn........mm.",
            "..........nnnsssssnnn........mm.",
            "..........nnsesssesnn.......mm..",
            "..........nnsssssssnn.......mm..",
            "..........nrrrrrrrrrn......mm...",
            "...........rrrrrrrrr.......mm...",
            "....nnnnnn.rrrrrrrrr......mm....",
            "....nnnnnn.rrrrrrrrr.....mm.....",
            "...nnnnnnn.....n....nn...mm.....",
            "...nnnnnnnn.........nn..mm......",
            "...nnnnnnnnnnbbbbbnnnn..mm......",
            "..nnnnnnnnnnnbbbbbnnnn.mm.......",
            "..nnnnnnnnnnnbbbbbnnnnnmm.......",
            "..nnnnnn.nnnnbbbbbnnnnmm........",
            ".nnnnnnn.nnnnnnnnnnnsgs.........",
            ".nnnnnnn...nnnnnnnnnsbb.........",
            "...........bbbbbbbbb.bbg........",
            "...........nnnnnnnnn.bb.g.......",
            "............nnn.nnn.............",
            "............nnn.nnn.............",
            "............nnn.nnn.............",
            "............nnn.nnn.............",
            "...........bbbb.bbbb............",
            "...........bbbb.bbbb............",
            "...........bbbb.bbbb............",
        };

        static readonly string[] WardenHdRows =
        {
            "................................",
            "...............r................",
            "...............r................",
            "............mmmrmmm.............",
            "...........mmmmmmmmm............",
            "...........mmmmmmmmm............",
            "..........mmmmmmmmmmm...........",
            "..........mmmmmmmmmmm...........",
            "..........mmmmmmmmmmm...........",
            "..........meeeeeeeeem...........",
            "..........meeeeeeeeem...........",
            "...........mmmmmmmmm............",
            "...........mmmmmmmmm............",
            ".......mmm..mmmmmmm..mmm........",
            "......ggggg....m....ggggg.......",
            ".....mmmmmmm.......mmmmmmm......",
            ".....mmmmmmmvvvvvvvmmmmmmm......",
            "......vvvmmvvvvgvvvvmmvvv.......",
            "......vvvmvvvvvgvvvvvmvvv.......",
            "......vvvvvvvgggggvvvvvvv.......",
            "......vvvvvvvvvgvvvvvvvvv.......",
            "......vvvvvvvvvgvvvvvvvvv.......",
            "......vvvvvvvvvgvvvvvvmmmm......",
            "......mmmmmmmmmmmmmmmmmmm.m.....",
            "......mmmmmmmmmmmmmmmm....m.....",
            ".........vmmmmmvmmmmmv....m.....",
            "..........mmmmm.mmmmm.....mmmm..",
            "..........mmmmm.mmmmm.....mLLm..",
            "..........mmmmm.mmmmm.....mLLm..",
            ".........bbbbbb.bbbbbb....mmmm..",
            ".........bbbbbb.bbbbbb.....L....",
            ".........bbbbbb.bbbbbb..........",
        };

        /// <summary>スキルのアイコン（効果の種類ごとの形を、スキルの色で塗る）。</summary>
        public static Sprite SkillIcon(SkillEffect effect, Color color)
        {
            string[] rows;
            switch (effect)
            {
                case SkillEffect.Barrage: rows = IconBarrageRows; break;
                case SkillEffect.Dash: rows = IconDashRows; break;
                case SkillEffect.Strike: rows = IconStrikeRows; break;
                case SkillEffect.Buff: rows = IconBuffRows; break;
                case SkillEffect.Heal: rows = IconHealRows; break;
                default: rows = IconNovaRows; break;
            }
            Color32 w = new Color(Mathf.Min(1f, color.r * 1.1f + 0.1f), Mathf.Min(1f, color.g * 1.1f + 0.1f), Mathf.Min(1f, color.b * 1.1f + 0.1f), 1f);
            Color32 k = new Color(color.r * 0.55f, color.g * 0.55f, color.b * 0.55f, 1f);
            string key = $"Skill_{effect}_{w.r}_{w.g}_{w.b}";
            return FromArt(key, rows, new Dictionary<char, Color32> { { 'w', w }, { 'k', k } });
        }

        static readonly string[] IconNovaRows =
        {
            "................",
            ".....kkkkkk.....",
            "...kkwwwwwwkk...",
            "..kww......wwk..",
            "..kw..k..k..wk..",
            ".kw....kk....wk.",
            ".kw..k.ww.k..wk.",
            ".kw...wwww...wk.",
            ".kw...wwww...wk.",
            ".kw..k.ww.k..wk.",
            ".kw....kk....wk.",
            "..kw..k..k..wk..",
            "..kww......wwk..",
            "...kkwwwwwwkk...",
            ".....kkkkkk.....",
            "................",
        };

        static readonly string[] IconBarrageRows =
        {
            "................",
            ".......ww.......",
            "..w....ww....w..",
            "...w...ww...w...",
            "....w..ww..w....",
            ".....w.ww.w.....",
            "......wwww......",
            ".wwwwwwwwwwwwww.",
            ".wwwwwwwwwwwwww.",
            "......wwww......",
            ".....w.ww.w.....",
            "....w..ww..w....",
            "...w...ww...w...",
            "..w....ww....w..",
            ".......ww.......",
            "................",
        };

        static readonly string[] IconDashRows =
        {
            "................",
            "................",
            "................",
            "..........w.....",
            "..........ww....",
            "...kkwwwwwwww...",
            "..........wwww..",
            "wwwwwwwwwwwwwww.",
            "wwwwwwwwwwwwwww.",
            "..........wwww..",
            "...kkwwwwwwww...",
            "..........ww....",
            "..........w.....",
            "................",
            "................",
            "................",
        };

        static readonly string[] IconStrikeRows =
        {
            "..............w.",
            ".............w..",
            "...........kw...",
            "..........w.....",
            "........kw......",
            ".......w........",
            ".....wwww.......",
            "....wwwwww......",
            "...wwwwwwww.....",
            "...wwwwwwww.....",
            "...wwwwwwww.....",
            "....wwwwww......",
            ".....wwww.......",
            "................",
            "..k..........k..",
            "kkkkkkkkkkkkkkkk",
        };

        static readonly string[] IconBuffRows =
        {
            ".......ww.......",
            "......wwww......",
            ".....wwwwww.....",
            "....wwwwwwww....",
            "...wwwwwwwwww...",
            "..wwww.ww.wwww..",
            "......wwww......",
            "......wwww......",
            "......wwww......",
            "......wwww......",
            "......wwww......",
            "................",
            "..k..........k..",
            ".kwk........kwk.",
            "..k..........k..",
            "................",
        };

        static readonly string[] IconHealRows =
        {
            "................",
            "................",
            "......wwww......",
            "......wwww......",
            "......wwww......",
            "......wwww......",
            "..wwwwwwwwwwww..",
            "..wwwwwwwwwwww..",
            "..wwwwwwwwwwww..",
            "..wwwwwwwwwwww..",
            "......wwww......",
            "......wwww......",
            "......wwww......",
            "......wwww......",
            "................",
            "................",
        };

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

        /// <summary>区画ごとの色のスライム。</summary>
        public static Sprite SlimeFor(ZoneSpec z) => FromArt($"Slime_{z.Name}", SlimeRows, new Dictionary<char, Color32>
        {
            { 'k', Mix(z.SlimeDark, new Color32(0, 0, 0, 255), 0.6f) },
            { 'g', z.Slime },
            { 'w', Mix(z.Slime, new Color32(255, 255, 255, 255), 0.65f) },
            { 'e', new Color32(16, 16, 24, 255) },
            { 'd', z.SlimeDark },
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
        public static Sprite IsoFloor => IsoFloorFor(Zones.For(1), false);
        public static Sprite IsoFloorAlt => IsoFloorFor(Zones.For(1), true);

        /// <summary>区画ごとの床（色と模様）。alt は市松のもう一方。</summary>
        public static Sprite IsoFloorFor(ZoneSpec z, bool alt) =>
            IsoTile($"Floor_{z.Name}_{alt}", alt ? z.FloorAlt : z.Floor, z.Edge, z.Accent, z.Pattern, alt ? 7 : 0);

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

        /// <summary>迷宮の壁（高さ10ドットの段。どこも同じ高さ）。足元のひし形の中心が原点。</summary>
        public static Sprite IsoWallLow => IsoWallFor(Zones.For(1));

        /// <summary>区画ごとの壁（高さ10ドット）。</summary>
        public static Sprite IsoWallFor(ZoneSpec z)
        {
            {
                string key = $"Wall_{z.Name}";
                if (cache.TryGetValue(key, out var cached)) return cached;
                const int w = 32, h = 26, side = 10;
                var px = new Color32[w * h];
                var clear = new Color32(0, 0, 0, 0);
                var top = z.WallTop;
                var left = z.WallLeft;
                var right = z.WallRight;
                var edge = Mix(z.WallRight, new Color32(0, 0, 0, 255), 0.5f);
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
                        else if (y < lowerEdge && y >= lowerEdge - side)
                        {
                            c = y < lowerEdge - side + 1 ? edge : (x < 16 ? left : right);
                            if (y == (int)(lowerEdge - side * 0.5f)) c = edge; // 石積みの目地
                        }
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

        /// <summary>案内役（オリジナルキャラ）。40ドットを、プレイヤーと同じ縮尺（1マス32ドット）で描く。</summary>
        public static Sprite Guide
        {
            get
            {
                const string key = "Guide";
                if (cache.TryGetValue(key, out var cached)) return cached;
                var pal = new Dictionary<char, Color32>
                {
                    { 'k', new Color32(14, 10, 16, 255) },
                    { 'f', new Color32(70, 62, 72, 255) },
                    { 'w', new Color32(250, 246, 236, 255) },
                    { 's', new Color32(90, 80, 84, 255) },
                };
                int h = GuideRows.Length, w = GuideRows[0].Length;
                var px = new Color32[w * h];
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        char ch = GuideRows[y][x];
                        px[(h - 1 - y) * w + x] = pal.TryGetValue(ch, out var c) ? c : new Color32(0, 0, 0, 0);
                    }
                }
                return Store(key, MakeTexture(key, w, h, px, TextureWrapMode.Clamp), 32);
            }
        }

        static readonly string[] GuideRows =
        {
            ".kkk................................kkk.",
            ".kffkk...........................kkkffk.",
            ".kffffkkk......................kkkffffk.",
            "..kfffffkk....................kkfffffk..",
            "..kfffkffkk..................kkffkfffk..",
            ".kkkffkkfffk................kfffkkffkkk.",
            "kfkkkffkkfffk..............kfffkkffkkkfk",
            "kfffkkffkkfffk............kfffkkffkkfffk",
            ".kffffffkkkfffk..........kfffkkkffffffk.",
            ".kkfffkkkkkfffk..........kfffkkkkkfffkk.",
            "..kkfffkkkkkfff..........fffkkkkkfffkk..",
            ".kkkkkffkkkkkffk..kk....kffkkkkkffkkkkk.",
            ".kffkkkkkkkkkffk..kkkkk.kffkkkkkkkkkffk.",
            ".kkffffkkkkkkffkkkkffkkkkffkkkkkkffffkk.",
            "..kkffffkkkkkkkffffffffffkkkkkkkffffkk..",
            "....kkkkkkkfffkffffffffffkfffkkkkkkk....",
            "...kkkffkkffkffffffffffffffkffkkffkkk...",
            "...kfffffkkkffffffffffffffffkkkfffffk...",
            "....kkkfkkkfffkkfffkfffffkfffkkkfkkk....",
            "......kkkffffkkffffkfkfffkkffffkkk......",
            ".......kffkffkkfkfkkkkkkffkkfkffk.......",
            "......kkkkffkkkkkfkkkkkkkfkkffkkkk......",
            ".......kkffkkkkkkkkkkkkkkkkkkffkk.......",
            "......kkffkkkkkkkkkkkkkkkkkkkkffkk......",
            ".....kkffwwkkkkkkkkkkkkkwwkkkkfffkk.....",
            ".....kkkfwwkwwkwwkwwkwwkwwkkkkffkkk.....",
            "......kkfffkwwkwwkwwkwwkkkkkkfffkk......",
            "......kkkffkkkkkkkkkkkkkkkkkkfkkfk......",
            "......kkkfffwwkwwkwwkwwkkkkkfffkkk......",
            "........kkkfwwkwwkwwkwwkkkkffkkk........",
            "........kkffffkkkkkkkkkkkkffffkk........",
            ".........kkkkffkkkkkkkkkfffkkkk.........",
            "..........kkkkffffkkkkffffkkkk..........",
            ".........kkffkkkffffffffkkkffkk.........",
            "........kkfkfffkkkkffkkkkfffkfkk........",
            "........kkkkffffkkkkkkkkffkfkk..........",
            ".........kffkkkkffkkkkfffkkfffk.........",
            ".........kfffkkffffkkffffkkfffk.........",
            "........kkkkfkkfkkfkkkkkfkkfkkkk........",
            "........fkkkkffkkkkffkkkkffkkkkf........",
        };

        /// <summary>祭壇（石の台の上に火が灯る）。足元が原点。</summary>
        public static Sprite Altar => FromArt("Altar", AltarRows, new Dictionary<char, Color32>
        {
            { 'k', new Color32(16, 12, 22, 255) },
            { 'y', new Color32(255, 230, 140, 255) },
            { 'r', new Color32(255, 130, 50, 255) },
            { 's', new Color32(120, 112, 140, 255) },
            { 'd', new Color32(80, 74, 98, 255) },
            { 'g', new Color32(220, 180, 70, 255) },
        });

        static readonly string[] AltarRows =
        {
            ".......y........",
            "......yry.......",
            ".....yrrry......",
            ".....yrrry......",
            "......yry.......",
            "....kkkkkkkk....",
            "...kggggggggk...",
            "...kssssssssk...",
            "....kssssssk....",
            "....kdsssdsk....",
            "....ksdssdsk....",
            "....ksssssdk....",
            "...kssssssssk...",
            "..kddddddddddk..",
            "..kkkkkkkkkkkk..",
            "................",
        };

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

        static Sprite IsoTile(string key, Color32 stone, Color32 edge, Color32 accent, FloorPattern pattern, int salt)
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
                    uint hsh = Hash(x + salt * 31, y + salt * 17);
                    int n = (int)(hsh % 9) - 4;
                    Color32 c = new Color32(
                        (byte)Mathf.Clamp(stone.r + n, 0, 255),
                        (byte)Mathf.Clamp(stone.g + n, 0, 255),
                        (byte)Mathf.Clamp(stone.b + n, 0, 255),
                        255);
                    // 区画ごとの模様
                    switch (pattern)
                    {
                        case FloorPattern.Moss: // 苔の点と小さな固まり
                            if (hsh % 13 == 0 || (Hash(x / 2 + salt, y / 2) % 9 == 0 && hsh % 3 == 0)) c = accent;
                            break;
                        case FloorPattern.Lava: // 細いひび
                            if (Hash(x / 3 + salt, y) % 17 == 0 && d < 0.8f) c = accent;
                            break;
                        case FloorPattern.Ice: // 斜めのつやと、きらめき
                            if ((x - 2 * y + salt + 64) % 14 == 0 && d < 0.75f) c = Mix(c, accent, 0.45f);
                            if (hsh % 47 == 0) c = accent;
                            break;
                        case FloorPattern.Gold: // 縁に沿った金の筋
                            if (d > 0.7f && d < 0.76f) c = Mix(c, accent, 0.7f);
                            break;
                        case FloorPattern.Void: // 星のような点
                            if (hsh % 41 == 0) c = accent;
                            else if (hsh % 23 == 0) c = Mix(c, accent, 0.35f);
                            break;
                    }
                    px[y * w + x] = c;
                }
            }
            return Store(key, MakeTexture(key, w, h, px, TextureWrapMode.Clamp));
        }

        static Color32 Mix(Color32 a, Color32 b, float t) => new Color32(
            (byte)(a.r + (b.r - a.r) * t), (byte)(a.g + (b.g - a.g) * t), (byte)(a.b + (b.b - a.b) * t), 255);

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

        static Sprite Store(string key, Texture2D tex, int pixelsPerUnit = PixelsPerUnit)
        {
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f),
                pixelsPerUnit, 0, SpriteMeshType.FullRect);
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
