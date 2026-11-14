using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 画面の数字のドット文字（0〜9・＋・－と、HUD で使う英字・記号：HP MP LV EXP F ABYSS BOSS FPS / % ★ :）。太い線の白い文字に暗いふち。色は描くときに付ける（会心は金など）。
    /// このゲーム用のオリジナルの字形。
    /// </summary>
    public static class NumberFont
    {
        const string Chars = "0123456789+-ABEFHLMOPSVXYKI/%★: NUQR!WGDT";
        const int GW = 5, GH = 7, Cell = GW + 2, CellH = GH + 2; // 1ドットのふちの分
        public const int Advance = GW + 1;                          // 次の文字までの幅（ドット）
        public const int Height = CellH;

        static readonly string[][] Glyphs =
        {
            new[] { ".###.", "##.##", "##.##", "##.##", "##.##", "##.##", ".###." }, // 0
            new[] { "..##.", ".###.", "..##.", "..##.", "..##.", "..##.", ".####" }, // 1
            new[] { ".###.", "##.##", "...##", "..##.", ".##..", "##...", "#####" }, // 2
            new[] { "####.", "...##", "...##", ".###.", "...##", "...##", "####." }, // 3
            new[] { "...##", "..###", ".#.##", "##.##", "#####", "...##", "...##" }, // 4
            new[] { "#####", "##...", "####.", "...##", "...##", "##.##", ".###." }, // 5
            new[] { ".###.", "##...", "##...", "####.", "##.##", "##.##", ".###." }, // 6
            new[] { "#####", "...##", "..##.", "..##.", ".##..", ".##..", ".##.." }, // 7
            new[] { ".###.", "##.##", "##.##", ".###.", "##.##", "##.##", ".###." }, // 8
            new[] { ".###.", "##.##", "##.##", ".####", "...##", "...##", ".###." }, // 9
            new[] { ".....", "..#..", "..#..", "#####", "..#..", "..#..", "....." }, // +
            new[] { ".....", ".....", ".....", "#####", ".....", ".....", "....." }, // -
            new[] { ".###.", "##.##", "##.##", "#####", "##.##", "##.##", "##.##" }, // A
            new[] { "####.", "##.##", "##.##", "####.", "##.##", "##.##", "####." }, // B
            new[] { "#####", "##...", "##...", "####.", "##...", "##...", "#####" }, // E
            new[] { "#####", "##...", "##...", "####.", "##...", "##...", "##..." }, // F
            new[] { "##.##", "##.##", "##.##", "#####", "##.##", "##.##", "##.##" }, // H
            new[] { "##...", "##...", "##...", "##...", "##...", "##...", "#####" }, // L
            new[] { "#...#", "##.##", "#####", "#.#.#", "#...#", "#...#", "#...#" }, // M
            new[] { ".###.", "##.##", "##.##", "##.##", "##.##", "##.##", ".###." }, // O
            new[] { "####.", "##.##", "##.##", "####.", "##...", "##...", "##..." }, // P
            new[] { ".####", "##...", "##...", ".###.", "...##", "...##", "####." }, // S
            new[] { "##.##", "##.##", "##.##", "##.##", "##.##", ".###.", "..#.." }, // V
            new[] { "##.##", "##.##", ".###.", "..#..", ".###.", "##.##", "##.##" }, // X
            new[] { "##.##", "##.##", "##.##", ".###.", "..#..", "..#..", "..#.." }, // Y
            new[] { "##.##", "##.##", "####.", "###..", "####.", "##.##", "##.##" }, // K
            new[] { "#####", ".##..", ".##..", ".##..", ".##..", ".##..", "#####" }, // I
            new[] { "....#", "...##", "...#.", "..##.", ".##..", "##...", "#...." }, // /
            new[] { "##..#", "##.##", "...#.", "..#..", ".#...", "##.##", "#..##" }, // %
            new[] { "..#..", "..#..", "#####", ".###.", ".#.#.", "##.##", "#...#" }, // ★
            new[] { ".....", ".##..", ".##..", ".....", ".##..", ".##..", "....." }, // :
            new[] { ".....", ".....", ".....", ".....", ".....", ".....", "....." }, // 空白
            new[] { "##..#", "###.#", "#####", "##.##", "##..#", "##..#", "##..#" }, // N
            new[] { "##.##", "##.##", "##.##", "##.##", "##.##", "##.##", ".###." }, // U
            new[] { ".###.", "##.##", "##.##", "##.##", "##.##", ".###.", "...##" }, // Q
            new[] { "####.", "##.##", "##.##", "####.", "##.#.", "##.##", "##.##" }, // R
            new[] { ".##..", ".##..", ".##..", ".##..", ".##..", ".....", ".##.." }, // !
            new[] { "##.##", "##.##", "##.##", "#####", "#####", "##.##", "#...#" }, // W
            new[] { ".####", "##...", "##...", "##.##", "##.##", "##.##", ".####" }, // G
            new[] { "####.", "##.##", "##.##", "##.##", "##.##", "##.##", "####." }, // D
            new[] { "#####", "#####", ".##..", ".##..", ".##..", ".##..", ".##.." }, // T
        };

        const int SpaceAdvance = 3; // 空白は狭く

        static Texture2D tex;

        static Texture2D Texture
        {
            get
            {
                if (tex != null) return tex;
                int W = Cell * Chars.Length, H = CellH;
                var on = new bool[W, H]; // y は上から
                for (int g = 0; g < Glyphs.Length; g++)
                    for (int y = 0; y < GH; y++)
                        for (int x = 0; x < GW; x++)
                            if (Glyphs[g][y][x] == '#') on[g * Cell + 1 + x, 1 + y] = true;
                var px = new Color32[W * H];
                var dark = new Color32(20, 14, 30, 255);
                for (int y = 0; y < H; y++)
                {
                    for (int x = 0; x < W; x++)
                    {
                        Color32 c = new Color32(0, 0, 0, 0);
                        if (on[x, y])
                        {
                            // 下ほど少し暗く（立体感）
                            byte v = (byte)(255 - 50 * Mathf.Clamp01((y - 1) / (float)(GH - 1)));
                            c = new Color32(v, v, v, 255);
                        }
                        else
                        {
                            bool near = false;
                            for (int dy = -1; dy <= 1 && !near; dy++)
                                for (int dx = -1; dx <= 1 && !near; dx++)
                                {
                                    int nx = x + dx, ny = y + dy;
                                    if (nx >= 0 && ny >= 0 && nx < W && ny < H && on[nx, ny] && (nx / Cell) == (x / Cell)) near = true;
                                }
                            if (near) c = dark;
                        }
                        px[(H - 1 - y) * W + x] = c;
                    }
                }
                tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                tex.SetPixels32(px);
                tex.Apply();
                return tex;
            }
        }

        /// <summary>この文字だけでできていれば、ドット文字で描ける。</summary>
        public static bool CanDraw(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (char ch in s) if (Chars.IndexOf(ch) < 0) return false;
            return true;
        }

        static int AdvanceOf(char c) => c == ' ' ? SpaceAdvance : Advance;

        /// <summary>1ドットを px ピクセルとしたときの幅。</summary>
        public static float Width(string s, float px)
        {
            int w = 1;
            foreach (char c in s) w += AdvanceOf(c);
            return w * px;
        }

        /// <summary>
        /// 枠の中に描く（GUI の座標）。枠の高さに合う整数倍で、幅に収まらなければ小さくする。align は MiddleLeft・MiddleCenter・MiddleRight。
        /// </summary>
        public static void DrawIn(Rect r, string s, Color color, TextAnchor align = TextAnchor.MiddleCenter, float heightShare = 0.9f, float fixedPx = 0f)
        {
            // 四捨五入（切り捨てだと、低い欄で1ドット＝1ピクセルまで小さくなってしまう）。fixedPx を渡せば、その大きさにそろえる
            float px = fixedPx > 0f ? fixedPx : Mathf.Max(1f, Mathf.Round(r.height * heightShare / Height));
            while (px > 1f && Width(s, px) > r.width * 0.96f) px--;
            float w = Width(s, px);
            float cx = align == TextAnchor.MiddleLeft ? r.x + w * 0.5f
                     : align == TextAnchor.MiddleRight ? r.xMax - w * 0.5f
                     : r.center.x;
            Draw(new Vector2(cx, r.center.y), s, px, color);
        }

        /// <summary>center を中心に描く（GUI の座標）。color で染める。</summary>
        public static void Draw(Vector2 center, string s, float px, Color color)
        {
            var t = Texture;
            float w = Width(s, px), h = Height * px;
            float x = Mathf.Round(center.x - w * 0.5f), y = Mathf.Round(center.y - h * 0.5f);
            var old = GUI.color;
            GUI.color = color;
            float cw = 1f / Chars.Length;
            float cx = x;
            for (int i = 0; i < s.Length; i++)
            {
                int g = Chars.IndexOf(s[i]);
                if (g >= 0 && s[i] != ' ') GUI.DrawTextureWithTexCoords(new Rect(cx, y, Cell * px, h), t, new Rect(g * cw, 0f, cw, 1f));
                cx += AdvanceOf(s[i]) * px;
            }
            GUI.color = old;
        }
    }
}
