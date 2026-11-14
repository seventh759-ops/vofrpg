using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// レベルアップのときに頭の上に出す「LV UP」の文字の絵（ドット絵）。
    /// 羽（白い羽に金のふち、薄紫の影）と同じ色合い：太い2ドットの線の白い文字に、下へ向かって薄紫がかる影、
    /// 金のふちと、読みやすいよう外側に細い茶色のふち、下に淡い影。
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
        const int GlyphH = 9, Gap = 1, Space = 3, Border = 3; // ふちと影の分の余白

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

            var top = new Color32(255, 255, 250, 255);    // 羽の白
            var mid = new Color32(250, 248, 240, 255);
            var bottom = new Color32(212, 218, 242, 255); // 羽の影の薄紫
            var outline = new Color32(255, 206, 110, 255); // 羽のふちの金
            var outer = new Color32(110, 64, 18, 220);     // 外側の細いふち（明るい床の上でも読めるように）
            var shadow = new Color32(20, 8, 0, 110);
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
                    }
                    else if (Near(on, x, y, W, H, 1)) c = outline;
                    else if (Near(on, x, y, W, H, 2)) c = outer;
                    else if (y > 2 && Near(on, x, y - 1, W, H, 2)) c = shadow; // 外のふちの1ドット下に影
                    px[(H - 1 - y) * W + x] = c; // テクスチャは下から
                }
            }
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            t.SetPixels32(px);
            t.Apply();
            return t;
        }

        static bool Near(bool[,] on, int x, int y, int W, int H, int reach)
        {
            for (int dy = -reach; dy <= reach; dy++)
                for (int dx = -reach; dx <= reach; dx++)
                {
                    if (reach > 1 && Mathf.Abs(dx) == reach && Mathf.Abs(dy) == reach) continue; // 角は丸く
                    int nx = x + dx, ny = y + dy;
                    if (nx >= 0 && ny >= 0 && nx < W && ny < H && on[nx, ny]) return true;
                }
            return false;
        }
    }
}
