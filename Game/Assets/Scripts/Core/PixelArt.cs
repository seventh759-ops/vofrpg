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
        public static Sprite PlayerFor(ClassType type) => PlayerFor(type, Profile.Get(type));

        /// <summary>そのキャラクターの見た目（キャラクター選択の画面で、枠ごとに使う）。</summary>
        public static Sprite PlayerFor(CharacterData c) => PlayerFor((ClassType)c.ClassId, c);

        static Sprite PlayerFor(ClassType type, CharacterData ch)
        {
            // 上位職なら、その見た目（色を変えて、目印の小物を足す）
            if (ch != null && ch.Advanced >= 0) return AdvancedLook(type, ch.Advanced, ch.Third);
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

        /// <summary>影を付けずに光らせる素材（ランタン・香炉の火、上位職の光る刃・光輪）。</summary>
        static readonly Dictionary<char, Color32> glows = new Dictionary<char, Color32>
        {
            { 'l', new Color32(255, 230, 130, 255) },
            { 'L', new Color32(255, 170, 60, 255) },
            { 'o', new Color32(255, 250, 220, 255) },
            { 'p', new Color32(200, 130, 255, 255) }, // 紫に光る刃
            { 'R', new Color32(255, 70, 80, 255) },   // 赤く光る刃
        };

        /// <summary>
        /// 上位職の見た目。元の職業の絵をもとに、服の色を変え（overrides）、目印の小物を描き足す（paints）。
        /// どれもこのゲーム用のオリジナルの意匠。
        /// </summary>
        static Sprite AdvancedLook(ClassType type, int index, bool third = false)
        {
            string key = $"PlayerHd_{type}_Adv{index}" + (third ? "_3rd" : "");
            if (cache.TryGetValue(key, out var cached)) return cached;
            AdvancedParts(type, index, out var rows, out var over);
            if (!third) return Shaded(key, rows, over);
            ThirdParts(type, index, ref rows, over);
            return Shaded(key, rows, over, ThirdAura(type, index)); // 3次職は輪郭が光る
        }

        /// <summary>3次職の見た目（町の師匠の画面で、転生する前に見せる）。</summary>
        public static Sprite ThirdPreview(ClassType type, int advanced) => AdvancedLook(type, Mathf.Clamp(advanced, 0, 1), true);

        static void AdvancedParts(ClassType type, int index, out string[] rows, out Dictionary<char, Color32[]> over)
        {
            switch (type)
            {
                case ClassType.Lampwright when index == 0: // 炎呼び：深紅のローブと金の縁、燃える頭巾の先、橙の火のランタン
                    rows = Edit(LampwrightHdRows, c => c == 'l' ? 'L' : c, (0, 14, "oL"), (1, 14, "LL"), (2, 14, "LLc"));
                    over = Over(('c', Ramp(220, 70, 70, 176, 40, 48, 128, 26, 34, 88, 16, 24)), ('d', Ramp(255, 214, 110, 214, 160, 56, 160, 110, 30, 110, 72, 18)));
                    return;
                case ClassType.Lampwright: // 灯台守：白と金のローブ、金の枠のランタン、頭の上の光
                    rows = Edit(LampwrightHdRows, c => c == 'm' ? 'g' : c, (0, 12, "o"), (0, 17, "o"), (1, 11, "l"), (1, 18, "l"));
                    over = Over(('c', Ramp(250, 248, 236, 222, 216, 196, 178, 170, 150, 130, 122, 104)), ('d', Ramp(255, 230, 130, 226, 186, 72, 170, 130, 40, 120, 88, 24)));
                    return;
                case ClassType.Shadeblade when index == 0: // 夜狩り：紫がかった黒、角のある頭巾、紫に光る刃
                    rows = Edit(ShadebladeHdRows, c => c == 'm' ? 'p' : c, (2, 10, "n"), (3, 11, "n"), (4, 11, "n"), (2, 20, "n"), (3, 19, "n"), (4, 19, "n"));
                    over = Over(('n', Ramp(110, 80, 150, 72, 50, 108, 50, 34, 78, 32, 22, 52)), ('r', Ramp(190, 120, 255, 150, 80, 220, 106, 52, 170, 72, 34, 120)));
                    return;
                case ClassType.Shadeblade: // 血刃：赤黒い服、赤く光る刃、長い襟巻き
                    rows = Edit(ShadebladeHdRows, c => c == 'm' ? 'R' : c, (12, 7, "rrrr"), (13, 6, "rr"), (14, 5, "r"));
                    over = Over(('n', Ramp(120, 52, 62, 86, 34, 44, 60, 22, 30, 40, 14, 20)));
                    return;
                case ClassType.Warden when index == 0: // 陽縛り：金と白の陣羽織、頭の上の光輪、光る羽根飾り
                    rows = Edit(WardenHdRows, c => c == 'r' ? 'l' : c, (0, 12, "ooooooo"));
                    over = Over(('v', Ramp(255, 244, 200, 236, 210, 120, 196, 160, 70, 140, 108, 40)), ('m', Ramp(255, 250, 230, 222, 214, 186, 170, 160, 130, 118, 110, 86)));
                    return;
                default: // 城壁：濃い青の鎧、左手に大きな盾
                    rows = Edit(WardenHdRows, c => c,
                        (13, 1, "ggggg"),
                        (14, 0, "gvvvvvg"), (15, 0, "gvvvvvg"), (16, 0, "gvvgvvg"), (17, 0, "gvgggvg"), (18, 0, "gvvgvvg"),
                        (19, 0, "gvvgvvg"), (20, 0, "gvvvvvg"), (21, 0, "gvvvvvg"), (22, 0, "gvvvvvg"), (23, 1, "gvvvg"), (24, 2, "ggg"));
                    over = Over(('m', Ramp(170, 190, 230, 110, 130, 176, 74, 90, 130, 46, 56, 86)), ('v', Ramp(70, 96, 170, 44, 64, 130, 30, 44, 94, 20, 28, 64)));
                    return;
            }
        }

        /// <summary>3次職の輪郭の色（体のまわりが光って見える）。</summary>
        static Color32 ThirdAura(ClassType type, int index)
        {
            switch (type)
            {
                case ClassType.Lampwright: return index == 0 ? new Color32(255, 130, 40, 255) : new Color32(150, 200, 255, 255);
                case ClassType.Shadeblade: return index == 0 ? new Color32(170, 100, 255, 255) : new Color32(255, 60, 70, 255);
                default: return index == 0 ? new Color32(255, 210, 90, 255) : new Color32(190, 225, 255, 255);
            }
        }

        /// <summary>3次職：上位職の見た目に、光る目・模様・周りの光の粒を足す。</summary>
        static void ThirdParts(ClassType type, int index, ref string[] rows, Dictionary<char, Color32[]> over)
        {
            switch (type)
            {
                case ClassType.Lampwright when index == 0: // 焔王：黒に近い深紅のローブに、溶けた金の筋
                    rows = Edit(rows, c => c == 'd' ? 'L' : c, (3, 3, "L"), (8, 29, "o"), (18, 1, "L"), (26, 29, "L"), (29, 3, "o"));
                    over['c'] = Ramp(150, 40, 46, 110, 24, 32, 76, 14, 22, 50, 8, 14);
                    break;
                case ClassType.Lampwright: // 星守：夜空の紺のローブに星
                    rows = Edit(rows, c => c, (20, 15, "o"), (24, 19, "o"), (27, 13, "o"), (17, 18, "l"), (3, 3, "o"), (8, 29, "l"), (18, 1, "o"), (26, 29, "o"));
                    over['c'] = Ramp(70, 86, 170, 44, 56, 130, 30, 38, 94, 18, 24, 64);
                    break;
                case ClassType.Shadeblade when index == 0: // 日蝕：紫に光る目、黒い装束
                    rows = Edit(rows, c => c == 'e' ? 'p' : c, (3, 3, "p"), (12, 30, "o"), (18, 1, "p"), (26, 28, "p"), (29, 3, "o"));
                    over['n'] = Ramp(70, 54, 100, 44, 32, 70, 30, 20, 50, 18, 12, 32);
                    break;
                case ClassType.Shadeblade: // 血の君主：赤く光る目、金の飾り
                    rows = Edit(rows, c => c == 'e' ? 'R' : c, (11, 10, "g"), (11, 20, "g"), (3, 3, "R"), (12, 30, "R"), (18, 1, "o"), (26, 28, "R"), (29, 3, "R"));
                    break;
                case ClassType.Warden when index == 0: // 暁光：光る兜の目、大きな光輪
                    rows = Edit(rows, c => c == 'e' ? 'l' : c, (0, 10, "ooooooooooo"), (1, 10, "o"), (1, 20, "o"), (8, 2, "l"), (20, 29, "o"), (28, 3, "l"));
                    break;
                default: // 神盾：光る盾の紋章と兜の目
                    rows = Edit(rows, c => c == 'e' ? 'o' : c, (16, 3, "o"), (17, 2, "ooo"), (18, 3, "o"), (19, 3, "o"), (2, 3, "o"), (8, 28, "o"), (29, 2, "o"));
                    break;
            }
        }

        /// <summary>絵の文字を置き換え（map）、指定の位置に描き足した（paints）新しい絵を作る。</summary>
        static string[] Edit(string[] src, System.Func<char, char> map, params (int y, int x, string s)[] paints)
        {
            var rows = new char[src.Length][];
            for (int y = 0; y < src.Length; y++)
            {
                rows[y] = src[y].ToCharArray();
                for (int x = 0; x < rows[y].Length; x++) rows[y][x] = map(rows[y][x]);
            }
            foreach (var p in paints)
            {
                for (int i = 0; i < p.s.Length; i++)
                {
                    int x = p.x + i;
                    if (p.y >= 0 && p.y < rows.Length && x >= 0 && x < rows[p.y].Length && p.s[i] != ' ') rows[p.y][x] = p.s[i];
                }
            }
            var result = new string[rows.Length];
            for (int y = 0; y < rows.Length; y++) result[y] = new string(rows[y]);
            return result;
        }

        static Dictionary<char, Color32[]> Over(params (char ch, Color32[] ramp)[] list)
        {
            var d = new Dictionary<char, Color32[]>();
            foreach (var e in list) d[e.ch] = e.ramp;
            return d;
        }

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
        static Sprite Shaded(string key, string[] rows, Dictionary<char, Color32[]> overrides = null, Color32? outlineColor = null)
        {
            if (cache.TryGetValue(key, out var cached)) return cached;
            int h = rows.Length, w = rows[0].Length;
            var outline = outlineColor ?? new Color32(22, 16, 30, 255);
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
                    else if ((overrides != null && overrides.TryGetValue(ch, out var ramp)) || ramps.TryGetValue(ch, out ramp))
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

        /// <summary>
        /// 敵の段階（0 下級・1 中級・2 上級）の見た目。上級ほど角・牙・光る目が付き、色が暗く厳つくなる。
        /// スライムは区画の色を使う。
        /// </summary>
        public static Sprite EnemySprite(EnemyKind kind, int tier)
        {
            if (tier <= 0 || kind == EnemyKind.Golden || kind == EnemyKind.Slime) return EnemySprite(kind);
            switch (kind)
            {
                case EnemyKind.Bat: return tier == 1 ? FromArt("Enemy_Bat_1", BatRows1, BatPalette1) : FromArt("Enemy_Bat_2", BatRows2, BatPalette2);
                case EnemyKind.Caster: return tier == 1 ? FromArt("Enemy_Caster_1", CasterRows1, CasterPalette1) : FromArt("Enemy_Caster_2", CasterRows2, CasterPalette2);
                case EnemyKind.Brute: return tier == 1 ? FromArt("Enemy_Brute_1", BruteRows1, BrutePalette1) : FromArt("Enemy_Brute_2", BruteRows2, BrutePalette2);
                case EnemyKind.Bomber: return tier == 1 ? FromArt("Enemy_Bomber_1", BomberRows1, BomberPalette1) : FromArt("Enemy_Bomber_2", BomberRows2, BomberPalette2);
                default: return EnemySprite(kind);
            }
        }

        /// <summary>区画の色のスライム（段階つき）。</summary>
        public static Sprite SlimeFor(ZoneSpec z, int tier)
        {
            if (tier <= 0) return SlimeFor(z);
            float f = tier == 1 ? 0.82f : 0.65f;
            Color32 Dark(Color32 c) => new Color32((byte)(c.r * f), (byte)(c.g * f), (byte)(c.b * f), 255);
            return FromArt($"Slime_{z.Name}_{tier}", tier == 1 ? SlimeRows1 : SlimeRows2, new Dictionary<char, Color32>
            {
                { 'k', new Color32(20, 30, 26, 255) },
                { 'g', Dark(z.Slime) },
                { 'w', new Color32(220, 255, 228, 255) },
                { 'e', new Color32(16, 24, 20, 255) },
                { 'd', Dark(z.SlimeDark) },
                { 'h', new Color32(230, 220, 200, 255) },
                { 'o', new Color32(255, 80, 60, 255) },
            });
        }

        static readonly string[] SlimeRows1 =
        {
            "............",
            "...h....h...",
            "...hk..kh...",
            "....kkkk....",
            "..kkggggkk..",
            ".kgwggggggk.",
            ".kwwggggggk.",
            "kgggkeekgggk",
            "kgggeggegggk",
            "kggggggggggk",
            ".kddddddddk.",
            "..kkkkkkkk..",
        };

        static readonly string[] SlimeRows2 =
        {
            "..h......h..",
            "..hh....hh..",
            "...hk..kh...",
            "....kkkk....",
            "..kkggggkk..",
            ".kgwggggggk.",
            ".kwwggggggk.",
            "kgggkggkgggk",
            "kgggoggogggk",
            "kggwkwkwkwgk",
            ".kddddddddk.",
            "..kkkkkkkk..",
        };

        static readonly Dictionary<char, Color32> BatPalette1 = new Dictionary<char, Color32>
        {
            { 'k', new Color32(19, 13, 24, 255) },
            { 'p', new Color32(90, 57, 114, 255) },
            { 'r', new Color32(255, 70, 70, 255) },
            { 'h', new Color32(230, 220, 200, 255) },
            { 'o', new Color32(255, 250, 210, 255) },
        };

        static readonly string[] BatRows1 =
        {
            "................",
            "................",
            "................",
            "......h..h......",
            "k.....hkkh.....k",
            "kk.....kk.....kk",
            "kpk...kppk...kpk",
            "kppk.kprrpk.kppk",
            "kpppkppppppkpppk",
            ".kppppppppppppk.",
            "..kpk.kwwk.kpk..",
            "...k..kppk..k...",
            "......k..k......",
            "................",
            "................",
            "................",
        };

        static readonly Dictionary<char, Color32> BatPalette2 = new Dictionary<char, Color32>
        {
            { 'k', new Color32(15, 10, 19, 255) },
            { 'p', new Color32(71, 45, 91, 255) },
            { 'r', new Color32(255, 70, 70, 255) },
            { 'h', new Color32(230, 220, 200, 255) },
            { 'o', new Color32(255, 250, 210, 255) },
            { 'a', new Color32(230, 180, 70, 255) },
        };

        static readonly string[] BatRows2 =
        {
            "................",
            ".....h....h.....",
            ".....hh..hh.....",
            "h.....hkkh.....h",
            "k..............k",
            "kk.....kk.....kk",
            "kpk...kppk...kpk",
            "kppk.kpoopk.kppk",
            "kpppkppppppkpppk",
            ".kppppppppppppk.",
            "..kpk.w.ww.kpk..",
            ".h.k..kppk..k.h.",
            "......k..k......",
            "................",
            "................",
            "................",
        };

        static readonly Dictionary<char, Color32> CasterPalette1 = new Dictionary<char, Color32>
        {
            { 'k', new Color32(16, 19, 18, 255) },
            { 'g', new Color32(57, 122, 90, 255) },
            { 'd', new Color32(32, 82, 57, 255) },
            { 'y', new Color32(255, 240, 140, 255) },
            { 'o', new Color32(255, 250, 210, 255) },
            { 'h', new Color32(230, 220, 200, 255) },
        };

        static readonly string[] CasterRows1 =
        {
            ".....h....h.....",
            ".....hkkkkh.....",
            ".....kggggk.....",
            "....kggggggk....",
            "....kgkkkkgk....",
            "...kgkyyyykgk...",
            "...kgkyryykgk...",
            "...kggkkkkggk...",
            "..kgggggggggk...",
            "..kggdggggdggk..",
            "..kgdggggggdgk..",
            "..kggggggggggk..",
            "...kggggggggk...",
            "....kgkgkgkgk...",
            ".....k.k.k.k....",
            "................",
        };

        static readonly Dictionary<char, Color32> CasterPalette2 = new Dictionary<char, Color32>
        {
            { 'k', new Color32(13, 15, 14, 255) },
            { 'g', new Color32(45, 97, 71, 255) },
            { 'd', new Color32(26, 65, 45, 255) },
            { 'y', new Color32(255, 240, 140, 255) },
            { 'o', new Color32(255, 250, 210, 255) },
            { 'h', new Color32(230, 220, 200, 255) },
            { 'a', new Color32(230, 180, 70, 255) },
        };

        static readonly string[] CasterRows2 =
        {
            "....h..h..hh....",
            "....hkkkkkkh....",
            ".....kggggk.....",
            "....kggggggk....",
            "....kgkkkkgk....",
            "...kgkyyyykgk...",
            "...kgkrorykgk...",
            "...kggkkkkggk...",
            "..kgggggggggk...",
            "..kgadggggdagk..",
            "..kgaggggggagk..",
            "..kggggggggggk..",
            "...kggggggggk...",
            "....kakakakak...",
            ".....k.k.k.k....",
            "................",
        };

        static readonly Dictionary<char, Color32> BrutePalette1 = new Dictionary<char, Color32>
        {
            { 'k', new Color32(21, 18, 16, 255) },
            { 's', new Color32(106, 98, 90, 255) },
            { 'd', new Color32(73, 67, 60, 255) },
            { 'y', new Color32(255, 140, 60, 255) },
            { 'h', new Color32(230, 220, 200, 255) },
            { 'o', new Color32(255, 250, 210, 255) },
        };

        static readonly string[] BruteRows1 =
        {
            "................",
            "....h......h....",
            "....hkkkkkkh....",
            "....kssssssk....",
            "...kssyssyssk...",
            "...kssssssssk...",
            "..kssssssssssk..",
            ".kssskssssksssk.",
            "kssssksssskssssk",
            "kssdssssssssdssk",
            "kssdsysssssddssk",
            ".kkkssssssssskk.",
            "...ksssk.kssk...",
            "...kddk...kddk..",
            "...kkkk...kkkk..",
            "................",
        };

        static readonly Dictionary<char, Color32> BrutePalette2 = new Dictionary<char, Color32>
        {
            { 'k', new Color32(16, 14, 13, 255) },
            { 's', new Color32(84, 78, 71, 255) },
            { 'd', new Color32(58, 53, 48, 255) },
            { 'y', new Color32(255, 140, 60, 255) },
            { 'h', new Color32(230, 220, 200, 255) },
            { 'o', new Color32(255, 250, 210, 255) },
            { 'a', new Color32(230, 180, 70, 255) },
        };

        static readonly string[] BruteRows2 =
        {
            "...h........h...",
            "...hh......hh...",
            "....hkkkkkkh....",
            "....kssssssk....",
            "...kssossossk...",
            ".h.kssssssssk.h.",
            ".hksssssssssskh.",
            ".kssskssssksssk.",
            "ksssskyssskssssk",
            "kssdsssyysysdssk",
            "kssdssyssssddssk",
            ".kkkssssssssskk.",
            "...ksssk.kssk...",
            "...kddk...kddk..",
            "...kkkk...kkkk..",
            "................",
        };

        static readonly Dictionary<char, Color32> BomberPalette1 = new Dictionary<char, Color32>
        {
            { 'k', new Color32(16, 14, 19, 255) },
            { 'b', new Color32(49, 47, 62, 255) },
            { 'w', new Color32(160, 160, 180, 255) },
            { 'r', new Color32(255, 90, 60, 255) },
            { 'y', new Color32(255, 220, 90, 255) },
            { 'o', new Color32(255, 90, 60, 255) },
            { 'h', new Color32(230, 220, 200, 255) },
        };

        static readonly string[] BomberRows1 =
        {
            "..........y.....",
            ".........yoy....",
            "..........w.....",
            ".........w......",
            ".....hkkkhk.....",
            "....hkbbbbkhk...",
            "...kbbbwbbbbbk..",
            "...kbbwbbbbbbk..",
            "..kbbbbbbbbbbbk.",
            "..kbbrrbbbrrbbk.",
            "..kbbbbbbbbbbbk.",
            "...kbbbbbbbbbk..",
            "...kbbbkkkbbbk..",
            "....kkk...kkk...",
            "....kk.....kk...",
            "................",
        };

        static readonly Dictionary<char, Color32> BomberPalette2 = new Dictionary<char, Color32>
        {
            { 'k', new Color32(13, 11, 15, 255) },
            { 'b', new Color32(39, 37, 49, 255) },
            { 'w', new Color32(160, 160, 180, 255) },
            { 'r', new Color32(255, 90, 60, 255) },
            { 'y', new Color32(255, 220, 90, 255) },
            { 'o', new Color32(255, 90, 60, 255) },
            { 'h', new Color32(230, 220, 200, 255) },
            { 'a', new Color32(230, 180, 70, 255) },
        };

        static readonly string[] BomberRows2 =
        {
            "..........y.....",
            ".........yoy....",
            "..........w.....",
            "....h....w.h....",
            "....hkkkkkkh....",
            "..h.kkbbbbbkkh..",
            "...kbbbwbbbbbk..",
            "...kbbwbbbbbbk..",
            ".hkbbbbbbbbbbbh.",
            "..kboorbbboobbk.",
            "..kbbbbbbbbbbbk.",
            "...kbbwkwbbbbk..",
            "...kbbbkkkbbbk..",
            "....kkk...kkk...",
            "....kk.....kk...",
            "................",
        };

        /// <summary>
        /// 強化の紋章（24ドット）。+1〜+4 で黄色のひし形が上・下・右・左に1つずつ、+5 で中央の橙の四角、
        /// +6〜+9 で先のとがった花びらが左上・右上・右下・左下に1枚ずつ、+10 で中央の光。+11〜+15 は花びらの色が
        /// 赤・深紅・紫・青・白金と変わる。
        /// </summary>
        public static Sprite EnhanceEmblem(int level)
        {
            level = Mathf.Clamp(level, 1, Item.MaxEnhance);
            string key = "Emblem" + level;
            if (cache.TryGetValue(key, out var cached)) return cached;
            const int n = 24;
            const float c = n / 2f;
            var yellow = new Color32(255, 236, 40, 255);
            var orange = new Color32(236, 128, 50, 255);
            var outline = new Color32(40, 44, 70, 255);
            Color32 petal;
            switch (level)
            {
                case 11: petal = new Color32(235, 80, 70, 255); break;
                case 12: petal = new Color32(205, 45, 95, 255); break;
                case 13: petal = new Color32(170, 95, 235, 255); break;
                case 14: petal = new Color32(90, 150, 255, 255); break;
                case 15: petal = new Color32(255, 250, 225, 255); break;
                default: petal = new Color32(244, 178, 130, 255); break;
            }
            var light = level >= 15 ? new Color32(255, 255, 255, 255) : new Color32(255, 220, 110, 255);
            int k = Mathf.Min(level, 10);

            var layer = new int[n * n];
            var col = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    int id = -1;
                    Color32 cc = default;
                    for (int i = 0; i < Mathf.Min(k, 4); i++)
                    {
                        bool hit;
                        switch (i)
                        {
                            case 0: hit = Mathf.Abs(px - c) / 2.3f + Mathf.Abs(py - 6f) / 6f <= 1f; break;       // 上
                            case 1: hit = Mathf.Abs(px - c) / 2.3f + Mathf.Abs(py - (n - 6f)) / 6f <= 1f; break; // 下
                            case 2: hit = Mathf.Abs(py - c) / 2.3f + Mathf.Abs(px - (n - 6f)) / 6f <= 1f; break; // 右
                            default: hit = Mathf.Abs(py - c) / 2.3f + Mathf.Abs(px - 6f) / 6f <= 1f; break;     // 左
                        }
                        if (hit) { id = i; cc = yellow; }
                    }
                    if (k >= 5 && Mathf.Abs(px - c) + Mathf.Abs(py - c) <= 5.6f) { id = 4; cc = orange; }
                    for (int i = 0; i < Mathf.Clamp(k - 5, 0, 4); i++)
                    {
                        float sx = i == 0 || i == 3 ? -1f : 1f; // 左上・右上・右下・左下
                        float sy = i < 2 ? -1f : 1f;
                        float dx = (px - c) * sx, dy = (py - c) * sy;
                        float u = (dx + dy) * 0.7071f, v = (dx - dy) * 0.7071f;
                        // 大きな花びら。外へ向かって鋭くとがる（少しだけ片側に傾ける）
                        float w = v - 0.12f * u;
                        if (u < 2f || u > 10.6f) continue;
                        float half = u < 4.6f ? 1.6f + (u - 2f) / 2.6f * 1.6f : 3.2f * (10.6f - u) / 6f;
                        if (Mathf.Abs(w) < half) { id = 5 + i; cc = petal; }
                    }
                    if (k >= 10 && Mathf.Abs(px - c) + Mathf.Abs(py - c) <= 2.2f) { id = 9; cc = light; }
                    layer[y * n + x] = id;
                    col[y * n + x] = cc;
                }
            }
            var px32 = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    int i = y * n + x, id = layer[i];
                    bool edgeEmpty = false, edgeLower = false;
                    for (int d = 0; d < 4; d++)
                    {
                        int ax = x + (d == 0 ? 1 : d == 1 ? -1 : 0), ay = y + (d == 2 ? 1 : d == 3 ? -1 : 0);
                        int nid = ax >= 0 && ay >= 0 && ax < n && ay < n ? layer[ay * n + ax] : -1;
                        if (nid >= 0 && id < 0) edgeEmpty = true;
                        if (id >= 4 && nid >= 0 && nid < id) edgeLower = true;
                    }
                    Color32 o = id < 0 ? (edgeEmpty ? outline : new Color32(0, 0, 0, 0)) : (edgeLower ? outline : col[i]);
                    px32[(n - 1 - y) * n + x] = o; // テクスチャは下から上へ並ぶ
                }
            }
            return Store(key, MakeTexture(key, n, n, px32, TextureWrapMode.Clamp), n);
        }

        /// <summary>黄金のスライム（出会うと逃げる、倒すとドロップがあふれる）。</summary>
        public static Sprite GoldenSlime => FromArt("GoldenSlime", SlimeRows, new Dictionary<char, Color32>
        {
            { 'k', new Color32(70, 46, 10, 255) },
            { 'g', new Color32(255, 204, 60, 255) },
            { 'w', new Color32(255, 252, 220, 255) },
            { 'e', new Color32(60, 36, 8, 255) },
            { 'd', new Color32(200, 140, 30, 255) },
        });

        static readonly Dictionary<char, Color32> ChestPalette = new Dictionary<char, Color32>
        {
            { 'k', new Color32(30, 18, 12, 255) },
            { 'b', new Color32(140, 86, 44, 255) },
            { 'g', new Color32(230, 180, 70, 255) },
            { 'y', new Color32(255, 230, 120, 255) },
            { 'w', new Color32(255, 255, 230, 255) },
        };

        /// <summary>宝の間の宝箱（閉じている／開いた）。</summary>
        public static Sprite Chest(bool open) => open ? FromArt("ChestOpen", ChestOpenRows, ChestPalette) : FromArt("ChestClosed", ChestClosedRows, ChestPalette);

        static readonly string[] ChestClosedRows =
        {
            "..kkkkkkkkkk..",
            ".kbbbbbbbbbbk.",
            "kbbbbbbbbbbbbk",
            "kggggggggggggk",
            "kbbbbbgybbbbbk",
            "kbbbbbyybbbbbk",
            "kbbbbbbbbbbbbk",
            "kbbbbbbbbbbbbk",
            "kggggggggggggk",
            ".kkkkkkkkkkkk.",
        };

        static readonly string[] ChestOpenRows =
        {
            "..kkkkkkkkkk..",
            ".kbbbbbbbbbbk.",
            "kggggggggggggk",
            "kyyyyyyyyyyyyk",
            "kyywyyyyywyyyk",
            "kggggggggggggk",
            "kbbbbbbbbbbbbk",
            "kbbbbbbbbbbbbk",
            "kggggggggggggk",
            ".kkkkkkkkkkkk.",
        };

        /// <summary>スライム以外の敵の絵（16ドット）。どれもこのゲーム用のオリジナル。</summary>
        public static Sprite EnemySprite(EnemyKind kind)
        {
            switch (kind)
            {
                case EnemyKind.Bat: return FromArt("Enemy_Bat", BatRows, BatPalette);
                case EnemyKind.Caster: return FromArt("Enemy_Caster", CasterRows, CasterPalette);
                case EnemyKind.Brute: return FromArt("Enemy_Brute", BruteRows, BrutePalette);
                case EnemyKind.Bomber: return FromArt("Enemy_Bomber", BomberRows, BomberPalette);
                case EnemyKind.Golden: return GoldenSlime;
                default: return Slime;
            }
        }

        static readonly Dictionary<char, Color32> BatPalette = new Dictionary<char, Color32>
        {
            { 'k', new Color32(24, 16, 30, 255) },
            { 'p', new Color32(110, 70, 140, 255) },
            { 'r', new Color32(255, 70, 70, 255) },
        };

        static readonly string[] BatRows =
        {
            "................",
            "................",
            "................",
            "................",
            "k..............k",
            "kk.....kk.....kk",
            "kpk...kppk...kpk",
            "kppk.kprrpk.kppk",
            "kpppkppppppkpppk",
            ".kppppppppppppk.",
            "..kpk.kppk.kpk..",
            "...k..kppk..k...",
            "......k..k......",
            "................",
            "................",
            "................",
        };

        static readonly Dictionary<char, Color32> CasterPalette = new Dictionary<char, Color32>
        {
            { 'k', new Color32(20, 24, 22, 255) },
            { 'g', new Color32(70, 150, 110, 255) },
            { 'd', new Color32(40, 100, 70, 255) },
            { 'y', new Color32(255, 240, 140, 255) },
            { 'o', new Color32(255, 255, 255, 255) },
        };

        static readonly string[] CasterRows =
        {
            "................",
            "......kkkk......",
            ".....kggggk.....",
            "....kggggggk....",
            "....kgkkkkgk....",
            "...kgkyyyykgk...",
            "...kgkyoyykgk...",
            "...kggkkkkggk...",
            "..kgggggggggk...",
            "..kggdggggdggk..",
            "..kgdggggggdgk..",
            "..kggggggggggk..",
            "...kggggggggk...",
            "....kgkgkgkgk...",
            ".....k.k.k.k....",
            "................",
        };

        static readonly Dictionary<char, Color32> BrutePalette = new Dictionary<char, Color32>
        {
            { 'k', new Color32(26, 22, 20, 255) },
            { 's', new Color32(130, 120, 110, 255) },
            { 'd', new Color32(90, 82, 74, 255) },
            { 'y', new Color32(255, 140, 60, 255) },
        };

        static readonly string[] BruteRows =
        {
            "................",
            "................",
            ".....kkkkkk.....",
            "....kssssssk....",
            "...kssyssyssk...",
            "...kssssssssk...",
            "..kssssssssssk..",
            ".kssskssssksssk.",
            "kssssksssskssssk",
            "kssdssssssssdssk",
            "kssdsssssssddssk",
            ".kkkssssssssskk.",
            "...ksssk.kssk...",
            "...kddk...kddk..",
            "...kkkk...kkkk..",
            "................",
        };

        static readonly Dictionary<char, Color32> BomberPalette = new Dictionary<char, Color32>
        {
            { 'k', new Color32(20, 18, 24, 255) },
            { 'b', new Color32(60, 58, 76, 255) },
            { 'w', new Color32(160, 160, 180, 255) },
            { 'r', new Color32(255, 90, 60, 255) },
            { 'y', new Color32(255, 220, 90, 255) },
            { 'o', new Color32(255, 255, 230, 255) },
        };

        static readonly string[] BomberRows =
        {
            "..........y.....",
            ".........yoy....",
            "..........w.....",
            ".........w......",
            "......kkkkk.....",
            "....kkbbbbbkk...",
            "...kbbbwbbbbbk..",
            "...kbbwbbbbbbk..",
            "..kbbbbbbbbbbbk.",
            "..kbbrrbbbrrbbk.",
            "..kbbbbbbbbbbbk.",
            "...kbbbbbbbbbk..",
            "...kbbbkkkbbbk..",
            "....kkk...kkk...",
            "....kk.....kk...",
            "................",
        };

        /// <summary>町の施設のアイコン（12ドット）。0 倉庫番・1 商人・2 師匠・3 荷運び・4 鍛冶屋。</summary>
        public static Sprite TownIcon(int index)
        {
            index = Mathf.Clamp(index, 0, TownIconRows.Length - 1);
            return FromArt("Town" + index, TownIconRows[index], TownIconPalette);
        }

        static readonly Dictionary<char, Color32> TownIconPalette = new Dictionary<char, Color32>
        {
            { 'a', new Color32(255, 210, 90, 255) },
            { 'b', new Color32(150, 95, 50, 255) },
            { 'w', new Color32(255, 250, 235, 255) },
            { 's', new Color32(190, 200, 215, 255) },
            { 'p', new Color32(140, 90, 200, 255) },
            { 'g', new Color32(90, 160, 90, 255) },
            { 'k', new Color32(70, 70, 84, 255) },
        };

        static readonly string[][] TownIconRows =
        {
            new[] // Storage
            {
                "............",
                "..aaaaaaaa..",
                ".abbbbbbbba.",
                ".abbbbbbbba.",
                ".aaaaawaaaa.",
                ".abbbbwbbba.",
                ".abbbbbbbba.",
                ".abbbbbbbba.",
                ".aaaaaaaaaa.",
                "............",
                "............",
                "............",
            },
            new[] // Merchant
            {
                "............",
                ".....aa.....",
                "....a..a....",
                ".....bb.....",
                "....bbbb....",
                "...bbbbbb...",
                "..bbbwabbb..",
                "..bbbaabbb..",
                "..bbbbbbbb..",
                "...bbbbbb...",
                "............",
                "............",
            },
            new[] // Master
            {
                "............",
                "..pppppppp..",
                "..pppppppp..",
                "..ppppappp..",
                "..pppaaapp..",
                "..ppaaaaap..",
                "..pppaaapp..",
                "..ppapppap..",
                "..pppppppp..",
                "..wwwwwwww..",
                "............",
                "............",
            },
            new[] // Porter
            {
                "............",
                ".....bb.....",
                "....b..b....",
                "...gggggg...",
                "..gggggggg..",
                "..gggaaggg..",
                "..gggggggg..",
                "..ggkkkkgg..",
                "..gggggggg..",
                "...gggggg...",
                "............",
                "............",
            },
            new[] // Smith
            {
                "............",
                ".......ss...",
                "......ss....",
                ".....bs.....",
                "....b.......",
                "...b........",
                "..ssssssss..",
                "...ssssss...",
                "....ssss....",
                "...kkkkkk...",
                "..kkkkkkkk..",
                "............",
            },
        };

        /// <summary>加護のアイコン（12ドット。Boons の表と同じ順）。a=主の色 b=影 w=光。</summary>
        public static Sprite BoonIcon(int index)
        {
            index = Mathf.Clamp(index, 0, BoonIconRows.Length - 1);
            var c = BoonIconColors[index];
            return FromArt("Boon" + index, BoonIconRows[index], new Dictionary<char, Color32>
            {
                { 'a', c },
                { 'b', new Color32((byte)(c.r * 0.55f), (byte)(c.g * 0.55f), (byte)(c.b * 0.55f), 255) },
                { 'w', new Color32(255, 250, 235, 255) },
            });
        }

        static readonly Color32[] BoonIconColors =
        {
            new Color32(255, 140, 60, 255), // SharpFlame
            new Color32(255, 230, 90, 255), // Quickening
            new Color32(255, 210, 120, 255), // KindledWick
            new Color32(120, 220, 255, 255), // KeenEye
            new Color32(230, 90, 110, 255), // Ruin
            new Color32(240, 80, 90, 255), // Vigor
            new Color32(90, 140, 255, 255), // DeepWell
            new Color32(140, 230, 150, 255), // SwiftStep
            new Color32(170, 180, 210, 255), // Bulwark
            new Color32(200, 60, 80, 255), // Leech
            new Color32(190, 140, 255, 255), // Echo
            new Color32(255, 120, 40, 255), // EmberBurst
        };

        static readonly string[][] BoonIconRows =
        {
            new[] // SharpFlame
            {
                "............",
                "......w.....",
                ".....aw.....",
                ".....aa.....",
                "....aaab....",
                "...aawab....",
                "...awwaab...",
                "..aawwwab...",
                "..aawwwab...",
                "...aawab....",
                "....aab.....",
                "............",
            },
            new[] // Quickening
            {
                "............",
                ".......aa...",
                "......aab...",
                ".....aab....",
                "....aab.....",
                "...aaaaaa...",
                "....baab....",
                "....aab.....",
                "...aab......",
                "..aab.......",
                "..ab........",
                "............",
            },
            new[] // KindledWick
            {
                "............",
                ".....w......",
                "....waw.....",
                "....aaa.....",
                "...aawaa....",
                "...awwwa....",
                "....aaa.....",
                ".....b......",
                "....bbb.....",
                "...bbbbb....",
                "...bbbbb....",
                "............",
            },
            new[] // KeenEye
            {
                "............",
                "............",
                "....aaaa....",
                "..aa....aa..",
                ".a...bb...a.",
                "a...bwwb...a",
                "a...bwwb...a",
                ".a...bb...a.",
                "..aa....aa..",
                "....aaaa....",
                "............",
                "............",
            },
            new[] // Ruin
            {
                "............",
                "....aaaa....",
                "...aaaaaa...",
                "..aaaaaaaa..",
                "..abbaabba..",
                "..abbaabba..",
                "..aaaaaaaa..",
                "...aaabaa...",
                "....aaaa....",
                "....a.a.a...",
                "............",
                "............",
            },
            new[] // Vigor
            {
                "............",
                "............",
                "..aa...aa...",
                ".awwa.aaab..",
                ".awaaaaaab..",
                ".aaaaaaaab..",
                "..aaaaaab...",
                "...aaaab....",
                "....aab.....",
                ".....b......",
                "............",
                "............",
            },
            new[] // DeepWell
            {
                "............",
                ".....a......",
                ".....a......",
                "....aaa.....",
                "....awa.....",
                "...awaaa....",
                "..aawaaab...",
                "..awaaaab...",
                "..aaaaaab...",
                "...aaaab....",
                "....bbb.....",
                "............",
            },
            new[] // SwiftStep
            {
                "............",
                "....aaa.....",
                "....aaa.....",
                "....aaa.....",
                "....aaa.....",
                "....aaab....",
                "....aaaab...",
                "w...aaaaaaa.",
                ".w..aaaaaaab",
                "w...bbbbbbbb",
                "............",
                "............",
            },
            new[] // Bulwark
            {
                "............",
                "..aaaaaaaa..",
                "..awwwwwwa..",
                "..awaaaaab..",
                "..awaawaab..",
                "..awawwwab..",
                "...aaawaab..",
                "...aaawab...",
                "....aaab....",
                ".....ab.....",
                "............",
                "............",
            },
            new[] // Leech
            {
                "............",
                "...a....a...",
                "...a....a...",
                "...aa..aa...",
                "....a..a....",
                "............",
                ".....aa.....",
                "....awab....",
                "....aaab....",
                ".....bb.....",
                "............",
                "............",
            },
            new[] // Echo
            {
                "............",
                "............",
                "..aaa..aaa..",
                ".a...aa...a.",
                "a...a..a...a",
                "a..a.ww.a..a",
                "a..a.ww.a..a",
                "a...a..a...a",
                ".a...aa...a.",
                "..aaa..aaa..",
                "............",
                "............",
            },
            new[] // EmberBurst
            {
                "............",
                ".....a......",
                ".a...a...a..",
                "..a..a..a...",
                "...awwwa....",
                "aaaawwwaaaa.",
                "...awwwa....",
                "..a..a..a...",
                ".a...a...a..",
                ".....a......",
                "............",
                "............",
            },
        };

        /// <summary>レベルアップの天使の羽（左の羽。右は反転して使う）。細長い形で、付け根は右端の上寄り。</summary>
        public static Sprite Wing => FromArt("Wing", WingRows, new Dictionary<char, Color32>
        {
            { 'o', new Color32(255, 214, 120, 255) },
            { 'w', new Color32(255, 255, 250, 255) },
            { 'g', new Color32(206, 214, 240, 255) },
        });

        static readonly string[] WingRows =
        {
            "............oooooooo..",
            "........ooooowwwwwwwo.",
            ".....ooowwwwwwwwwwwwwo",
            "..ooowwwwgwwwwgwwwwwo.",
            "oowwwwgwwwwgwwwwwoo...",
            ".oooowwwogwwwogwoo....",
            "....oowwo.owwo.oo.....",
            "......oo...oo.........",
        };

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
            ".....kkfffwwkkkkkkkkkkkkkkkkwwfffkk.....",
            ".....kkkffwwkwwkwwkwwkwwkwwkwwffkkk.....",
            "......kkffkkkwwkwwkwwkwwkwwkkkffkk......",
            "......kkkffkkkkkkkkkkkkkkkkkkfkkfk......",
            "......kkkfkkkwwkwwkwwkwwkwwkkkfkkk......",
            "........kkkkkwwkwwkwwkwwkwwkkkkk........",
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
