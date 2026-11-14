using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// レベルアップのときに頭の上に出す「LV UP」の文字の絵（ドット絵）。
    /// 太い2ドットの線の文字に、上が明るい金・下が橙のグラデーション、こげ茶のふち取りと下の影。
    /// </summary>
    public static class LevelUpArt
    {
        static readonly string[][] Glyphs =
        {
            new[] { "11....", "11....", "11....", "11....", "11....", "11....", "11....", "111111", "111111" }, // L
            new[] { "11...11", "11...11", "11...11", "11...11", ".11.11.", ".11.11.", ".11.11.", "..111..", "...1..." }, // V
            null, // 空き
            new[] { "11..11", "11..11", "11..11", "11..11", "11..11", "11..11", "11..11", "111111", ".1111." }, // U
            new[] { "11111.", "111111", "11..11", "11..11", "111111", "11111.", "11....", "11....", "11...." }, // P
        };
        const int GlyphH = 9, Gap = 1, Space = 3, Border = 2; // ふちと影の分の余白

        static Texture2D tex;

        public static Texture2D Texture
        {
            get
            {
                if (tex == null) tex = Build();
                return tex;
            }
        }

        static Texture2D Build()
        {
            int w = 0;
            foreach (var g in Glyphs) w += (g == null ? Space : g[0].Length) + Gap;
            w -= Gap;
            int W = w + Border * 2, H = GlyphH + Border * 2 + 1; // 下に影の1ドット
            var on = new bool[W, H]; // 文字のドット（y は上から）
            int x0 = Border;
            foreach (var g in Glyphs)
            {
                if (g == null) { x0 += Space + Gap; continue; }
                for (int y = 0; y < GlyphH; y++)
                    for (int x = 0; x < g[y].Length; x++)
                        if (g[y][x] == '1') on[x0 + x, Border + y] = true;
                x0 += g[0].Length + Gap;
            }

            var top = new Color32(255, 248, 190, 255);
            var mid = new Color32(255, 214, 80, 255);
            var bottom = new Color32(232, 130, 30, 255);
            var outline = new Color32(58, 28, 6, 255);
            var shadow = new Color32(20, 8, 0, 170);
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    Color32 c = new Color32(0, 0, 0, 0);
                    if (on[x, y])
                    {
                        float k = (y - Border) / (float)(GlyphH - 1);
                        c = k < 0.45f ? Color32.Lerp(top, mid, k / 0.45f) : Color32.Lerp(mid, bottom, (k - 0.45f) / 0.55f);
                        if (y > 0 && !on[x, y - 1]) c = new Color32(255, 255, 235, 255); // 線の上のふちは白く光る
                    }
                    else if (Near(on, x, y, W, H)) c = outline;
                    else if (y > 1 && Near(on, x, y - 1, W, H)) c = shadow; // ふちの1ドット下に影
                    px[(H - 1 - y) * W + x] = c; // テクスチャは下から
                }
            }
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            t.SetPixels32(px);
            t.Apply();
            return t;
        }

        static bool Near(bool[,] on, int x, int y, int W, int H)
        {
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx >= 0 && ny >= 0 && nx < W && ny < H && on[nx, ny]) return true;
                }
            return false;
        }
    }
}
