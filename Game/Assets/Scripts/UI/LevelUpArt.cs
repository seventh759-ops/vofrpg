using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// レベルアップのときに頭の上に出す「LV UP」の文字の絵（ドット絵）。
    /// タイトルの「VIVID」と同じ色合い：左から水色・紫・桃・橙へ変わるグラデーションの細い文字に、暗いふちと、文字の色の淡い光。
    /// </summary>
    public static class LevelUpArt
    {
        static readonly string[][] Glyphs =
        {
            new[] { "##...", "##...", "##...", "##...", "##...", "##...", "#####" }, // L
            new[] { "##.##", "##.##", "##.##", "##.##", "##.##", ".###.", "..#.." }, // V
            null, // 空き
            new[] { "##.##", "##.##", "##.##", "##.##", "##.##", "##.##", ".###." }, // U
            new[] { "####.", "##.##", "##.##", "####.", "##...", "##...", "##..." }, // P
        };
        const int GlyphH = 7, Gap = 1, Space = 3, Border = 3; // ふちと影の分の余白

        static Texture2D tex;

        public static Texture2D Texture
        {
            get
            {
                if (tex == null) tex = Build();
                return tex;
            }
        }

        // タイトルの「VIVID」と同じ色：左から水色・紫・桃・橙のグラデーション（上ほど白っぽく、下ほど深く）
        static readonly Color32[] LetterColors =
        {
            new Color32(80, 220, 255, 255),
            new Color32(150, 120, 255, 255),
            new Color32(255, 90, 215, 255),
            new Color32(255, 170, 60, 255),
        };

        /// <summary>左から右へ、水色 → 紫 → 桃 → 橙 となめらかに変わる色。</summary>
        static Color32 Grad(int x, int W)
        {
            float u = Mathf.Clamp01((x - Border) / (float)Mathf.Max(1, W - Border * 2 - 1)) * (LetterColors.Length - 1);
            int i = Mathf.Min(LetterColors.Length - 2, Mathf.FloorToInt(u));
            return Color32.Lerp(LetterColors[i], LetterColors[i + 1], u - i);
        }

        static Texture2D Build()
        {
            int w = 0;
            foreach (var g in Glyphs) w += (g == null ? Space : g[0].Length) + Gap;
            w -= Gap;
            int W = w + Border * 2, H = GlyphH + Border * 2;
            var on = new bool[W, H];  // 文字のドット（y は上から）
            var which = new int[W, H]; // 何文字目か
            int x0 = Border, letter = 0;
            foreach (var g in Glyphs)
            {
                if (g == null) { x0 += Space + Gap; continue; }
                for (int y = 0; y < GlyphH; y++)
                    for (int x = 0; x < g[y].Length; x++)
                        if (g[y][x] == '#') { on[x0 + x, Border + y] = true; which[x0 + x, Border + y] = letter; }
                x0 += g[0].Length + Gap;
                letter++;
            }

            var white = new Color32(255, 255, 255, 255);
            var dark = new Color32(20, 14, 30, 255); // タイトルのロゴと同じ暗いふち
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    Color32 c = new Color32(0, 0, 0, 0);
                    if (on[x, y])
                    {
                        var b = Grad(x, W);
                        float k = (y - Border) / (float)(GlyphH - 1);
                        c = k < 0.5f ? Color32.Lerp(b, white, 0.6f * (1f - k * 2f)) : Color32.Lerp(b, new Color32(0, 0, 0, 255), 0.25f * (k - 0.5f) * 2f);
                    }
                    else if (Near(on, x, y, W, H, 1, out _, out _)) c = dark; // ダメージの数字と同じ、まわり1ドットの暗いふち
                    else if (Near(on, x, y, W, H, 2, out int lx, out int ly))
                    {
                        var g = Grad(lx, W); // 外側は、近い文字の色でうっすら光る
                        c = new Color32(g.r, g.g, g.b, 120);
                    }
                    px[(H - 1 - y) * W + x] = c; // テクスチャは下から
                }
            }
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            t.SetPixels32(px);
            t.Apply();
            return t;
        }

        static bool Side(bool[,] on, int x, int y, int W, int H) =>
            (x > 0 && on[x - 1, y]) || (x < W - 1 && on[x + 1, y]) || (y > 0 && on[x, y - 1]) || (y < H - 1 && on[x, y + 1]);

        static bool Near(bool[,] on, int x, int y, int W, int H, int reach, out int lx, out int ly)
        {
            lx = ly = 0;
            for (int dy = -reach; dy <= reach; dy++)
                for (int dx = -reach; dx <= reach; dx++)
                {
                    if (reach > 1 && Mathf.Abs(dx) == reach && Mathf.Abs(dy) == reach) continue; // 角は丸く
                    int nx = x + dx, ny = y + dy;
                    if (nx >= 0 && ny >= 0 && nx < W && ny < H && on[nx, ny]) { lx = nx; ly = ny; return true; }
                }
            return false;
        }
    }
}
