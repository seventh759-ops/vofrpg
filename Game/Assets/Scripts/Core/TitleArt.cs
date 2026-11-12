using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// タイトルのロゴ「VIVID of FADE」をドット文字で作る。
    /// VIVID は1文字ずつ虹色に光り、of と FADE は灰色の石。FADE の右側ほど崩れて欠け、欠けた粒は Crumbs に入る（タイトル画面で舞い散らせる）。
    /// </summary>
    public static class TitleArt
    {
        const int GlyphH = 16;
        const int Pad = 2; // 縁取りと光のための余白

        static readonly Dictionary<char, string[]> glyphs = new Dictionary<char, string[]>
        {
            { 'V', new[]
            {
                "####.......####",
                ".###.......###.",
                ".###.......###.",
                "..###.....###..",
                "..###.....###..",
                "..###.....###..",
                "...###...###...",
                "...###...###...",
                "...###...###...",
                "....###.###....",
                "....###.###....",
                "....###.###....",
                ".....#####.....",
                ".....#####.....",
                "......###......",
                ".......#.......",
            } },
            { 'I', new[]
            {
                "#######",
                "..###..",
                "..###..",
                "..###..",
                "..###..",
                "..###..",
                "..###..",
                "..###..",
                "..###..",
                "..###..",
                "..###..",
                "..###..",
                "..###..",
                "..###..",
                "..###..",
                "#######",
            } },
            { 'D', new[]
            {
                "##########....",
                ".###....####..",
                ".###......###.",
                ".###......###.",
                ".###.......###",
                ".###.......###",
                ".###.......###",
                ".###.......###",
                ".###.......###",
                ".###.......###",
                ".###.......###",
                ".###......###.",
                ".###......###.",
                ".###.....###..",
                ".###...####...",
                "#########.....",
            } },
            { 'F', new[]
            {
                "#############",
                ".###......###",
                ".###.......##",
                ".###........#",
                ".###.........",
                ".###.....#...",
                ".###.....#...",
                ".#########...",
                ".###.....#...",
                ".###.....#...",
                ".###.........",
                ".###.........",
                ".###.........",
                ".###.........",
                ".###.........",
                "#######......",
            } },
            { 'A', new[]
            {
                ".......#.......",
                "......###......",
                "......###......",
                ".....#####.....",
                ".....##.###....",
                "....###.###....",
                "....##...###...",
                "...###...###...",
                "...##.....###..",
                "..###########..",
                "..##.......###.",
                ".###.......###.",
                ".##.........###",
                "###.........###",
                "##...........##",
                "####.......####",
            } },
            { 'E', new[]
            {
                "#############",
                ".###......###",
                ".###.......##",
                ".###........#",
                ".###.........",
                ".###.....#...",
                ".###.....#...",
                ".#########...",
                ".###.....#...",
                ".###.....#...",
                ".###.........",
                ".###........#",
                ".###.......##",
                ".###......###",
                ".###.....####",
                "#############",
            } },
            { 'o', new[]
            {
                "..#####..",
                ".##...##.",
                "##.....##",
                "##.....##",
                "##.....##",
                "##.....##",
                "##.....##",
                "##.....##",
                ".##...##.",
                "..#####..",
            } },
            { 'f', new[]
            {
                "########",
                ".##...##",
                ".##....#",
                ".##.#...",
                ".####...",
                ".##.#...",
                ".##.....",
                ".##.....",
                ".##.....",
                "####....",
            } },
        };

        static readonly Color32[] vividColors =
        {
            new Color32(80, 220, 255, 255),
            new Color32(150, 120, 255, 255),
            new Color32(255, 90, 215, 255),
            new Color32(255, 150, 60, 255),
            new Color32(255, 215, 80, 255),
        };

        static Texture2D logo, glow;
        static readonly List<Vector2Int> crumbs = new List<Vector2Int>();

        /// <summary>ロゴ本体（ドットのまま拡大して描く）。</summary>
        public static Texture2D Logo { get { Build(); return logo; } }

        /// <summary>VIVID のまわりの光（なめらかに拡大して描く）。Logo と同じ大きさ。</summary>
        public static Texture2D Glow { get { Build(); return glow; } }

        /// <summary>FADE から欠けた粒の位置（ロゴの左上を原点にしたドット単位）。</summary>
        public static List<Vector2Int> Crumbs { get { Build(); return crumbs; } }

        static Texture2D softGlow;

        /// <summary>まるくぼけた光（中心ほど濃い白）。色をつけて松明や灯りのまわりに重ねる。</summary>
        public static Texture2D SoftGlow
        {
            get
            {
                if (softGlow != null) return softGlow;
                const int n = 32;
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                        float d = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                        px[y * n + x] = new Color32(255, 255, 255, (byte)(255 * d * d));
                    }
                softGlow = Make("SoftGlow", n, n, px, FilterMode.Bilinear);
                return softGlow;
            }
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

        /// <summary>文字を並べて、1マスごとの印（a〜e: VIVID の各文字、o: of、f: FADE、0: 空き）を作る。</summary>
        static char[,] Compose(out int w)
        {
            var cols = new List<char[]>();
            void Add(char glyph, char tag, int yoff)
            {
                var rows = glyphs[glyph];
                for (int x = 0; x < rows[0].Length; x++)
                {
                    var col = new char[GlyphH];
                    for (int y = 0; y < rows.Length; y++)
                        if (rows[y][x] == '#') col[y + yoff] = tag;
                    cols.Add(col);
                }
            }
            void Space(int n)
            {
                for (int i = 0; i < n; i++) cols.Add(new char[GlyphH]);
            }

            string vivid = "VIVID";
            for (int i = 0; i < vivid.Length; i++) { Add(vivid[i], (char)('a' + i), 0); Space(2); }
            Space(4);
            Add('o', 'o', 5); Space(1); Add('f', 'o', 5); Space(6);
            string fade = "FADE";
            for (int i = 0; i < fade.Length; i++) { Add(fade[i], 'f', 0); if (i < fade.Length - 1) Space(2); }

            w = cols.Count;
            var grid = new char[w, GlyphH];
            for (int x = 0; x < w; x++)
                for (int y = 0; y < GlyphH; y++) grid[x, y] = cols[x][y];
            return grid;
        }

        static void Build()
        {
            if (logo != null) return;
            var grid = Compose(out int w);
            int h = GlyphH;
            int W = w + Pad * 2, H = h + Pad * 2;
            var col = new Color32?[W, H];
            var vivid = new int[W, H]; // VIVID の文字番号+1（0 は VIVID ではない）

            int fadeX0 = w;
            for (int x = 0; x < w && fadeX0 == w; x++)
                for (int y = 0; y < h; y++)
                    if (grid[x, y] == 'f') { fadeX0 = x; break; }

            crumbs.Clear();
            for (int y = 0; y < h; y++)
            {
                float k = y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    char c = grid[x, y];
                    if (c == '\0') continue;
                    Color32 cc;
                    if (c >= 'a' && c <= 'e')
                    {
                        var b = vividColors[c - 'a'];
                        cc = k < 0.5f
                            ? Color32.Lerp(b, new Color32(255, 255, 255, 255), 0.45f * (1f - k)) // 上ほど白っぽく
                            : Color32.Lerp(b, new Color32(0, 0, 0, 255), 0.35f * (k - 0.5f) * 2f);
                        vivid[x + Pad, y + Pad] = c - 'a' + 1;
                    }
                    else
                    {
                        float v = 1f - 0.4f * k;
                        cc = new Color32((byte)(170 * v), (byte)(165 * v), (byte)(182 * v), 255);
                        if (c == 'f')
                        {
                            if (Hash(x, y) % 100 < 10) cc = Color32.Lerp(cc, new Color32(0, 0, 0, 255), 0.3f); // ひび
                            float q = (x - fadeX0) / (float)(w - fadeX0);
                            float p = Mathf.Pow(Mathf.Max(0f, (q - 0.6f) / 0.4f), 1.5f) * 0.8f;
                            if (Hash(x + 7, y * 3) % 1000 / 1000f < p)
                            {
                                crumbs.Add(new Vector2Int(x + Pad, y + Pad));
                                continue;
                            }
                        }
                    }
                    col[x + Pad, y + Pad] = cc;
                }
            }

            var outline = new Color32(20, 14, 30, 255);
            var clear = new Color32(0, 0, 0, 0);
            var px = new Color32[W * H];
            var gpx = new Color32[W * H];
            const int R = 4;
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    int i = (H - 1 - y) * W + x; // テクスチャは下から上へ並ぶ
                    Color32 o = clear;
                    if (col[x, y].HasValue) o = col[x, y].Value;
                    else
                    {
                        for (int dy = -1; dy <= 1 && o.a == 0; dy++)
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int xx = x + dx, yy = y + dy;
                                if (xx >= 0 && yy >= 0 && xx < W && yy < H && col[xx, yy].HasValue) { o = outline; break; }
                            }
                    }
                    px[i] = o;

                    // 光：一番近い VIVID の文字の色で、離れるほど薄く
                    float best = 99f;
                    int which = 0;
                    for (int dy = -R; dy <= R; dy++)
                        for (int dx = -R; dx <= R; dx++)
                        {
                            int xx = x + dx, yy = y + dy;
                            if (xx < 0 || yy < 0 || xx >= W || yy >= H || vivid[xx, yy] == 0) continue;
                            float d = Mathf.Sqrt(dx * dx + dy * dy);
                            if (d <= R && d < best) { best = d; which = vivid[xx, yy]; }
                        }
                    if (which > 0)
                    {
                        var g = vividColors[which - 1];
                        float a = Mathf.Pow(1f - best / (R + 1f), 2f) * 0.8f;
                        gpx[i] = new Color32(g.r, g.g, g.b, (byte)(255 * a));
                    }
                    else gpx[i] = new Color32(0, 0, 0, 0);
                }
            }

            logo = Make("TitleLogo", W, H, px, FilterMode.Point);
            glow = Make("TitleLogoGlow", W, H, gpx, FilterMode.Bilinear);
        }

        static Texture2D Make(string name, int w, int h, Color32[] pixels, FilterMode filter)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = filter,
                wrapMode = TextureWrapMode.Clamp,
            };
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }
    }
}
