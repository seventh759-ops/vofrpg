using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// スキルの演出に使う小さな絵（炎・星・三日月・十字・隕石・葉）。
    /// 炎と隕石は色付き。ほかは白で描き、SpriteRenderer の色で染める。どれもこのゲーム用のオリジナル。
    /// </summary>
    public static partial class PixelArt
    {
        static readonly Dictionary<char, Color32> FlamePal = new Dictionary<char, Color32>
        {
            { 'w', new Color32(255, 255, 235, 255) },
            { 'y', new Color32(255, 230, 120, 255) },
            { 'o', new Color32(255, 150, 50, 255) },
            { 'r', new Color32(210, 60, 30, 230) },
        };

        /// <summary>炎のひと揺れ（7×10）。</summary>
        public static Sprite Flame => FromArt("FxFlame", new[]
        {
            "...r...",
            "...r...",
            "..rr...",
            "..roro.",
            ".rooor.",
            ".roygor",
            "roywyor",
            "roywwor",
            ".oywyo.",
            "..ooo..",
        }, FlamePal);

        /// <summary>青い炎（7×10。階段の封印が解けたしるし）。</summary>
        public static Sprite BlueFlame => FromArt("FxBlueFlame", new[]
        {
            "...r...",
            "...r...",
            "..rr...",
            "..roro.",
            ".rooor.",
            ".roygor",
            "roywyor",
            "roywwor",
            ".oywyo.",
            "..ooo..",
        }, new Dictionary<char, Color32>
        {
            { 'w', new Color32(240, 250, 255, 255) },
            { 'y', new Color32(170, 225, 255, 255) },
            { 'o', new Color32(80, 150, 255, 255) },
            { 'r', new Color32(50, 70, 210, 220) },
        });

        /// <summary>
        /// 階段の封印（階段と同じ 64×32 のひし形に重ねる）：鉄の格子、紅い縁取り、真ん中に金の錠前。
        /// </summary>
        public static Sprite StairsSeal
        {
            get
            {
                const string key = "StairsSeal64";
                if (cache.TryGetValue(key, out var cached)) return cached;
                const int w = 64, h = 32;
                var px = new Color32[w * h];
                var iron = new Color32(120, 122, 140, 255);
                var ironLit = new Color32(175, 178, 195, 255);
                var rim = new Color32(170, 35, 55, 255);
                var rimDark = new Color32(90, 15, 30, 255);
                var gold = new Color32(240, 200, 90, 255);
                var goldDark = new Color32(150, 105, 35, 255);
                for (int ty = 0; ty < h; ty++)
                {
                    int sy = h - 1 - ty;
                    for (int x = 0; x < w; x++)
                    {
                        float u = (x + 0.5f - 32f) / 32f, v = (sy + 0.5f - 16f) / 16f;
                        float d = Mathf.Abs(u) + Mathf.Abs(v);
                        Color32 c = new Color32(0, 0, 0, 0);
                        if (d <= 1f)
                        {
                            float a = (u + v) * 2.5f, b = (u - v) * 2.5f;
                            float fa = a - Mathf.Floor(a), fb = b - Mathf.Floor(b);
                            if (fa < 0.12f || fb < 0.12f) c = (fa < 0.06f || fb < 0.06f) ? ironLit : iron; // 鉄の格子
                            if (d > 0.84f) c = d > 0.93f ? rimDark : rim;                                       // 紅い縁
                            // 錠前（真ん中）：上に輪、下に四角い本体
                            float lx = Mathf.Abs(u) * 32f, ly = v * 16f;
                            if (lx <= 4f && ly >= -4f && ly <= 1.5f) c = (lx >= 3f || ly <= -3f) ? goldDark : gold;
                            if (ly > 1.5f && ly <= 5f && lx <= 3f && lx >= 1.5f) c = goldDark;
                            if (ly > 4f && ly <= 5f && lx < 1.5f) c = goldDark;
                            if (lx < 0.8f && ly > -2.6f && ly < -0.4f) c = new Color32(40, 25, 15, 255); // 鍵穴
                        }
                        px[ty * w + x] = c;
                    }
                }
                return Store(key, MakeTexture(key, w, h, px, TextureWrapMode.Clamp), 32);
            }
        }

        static readonly Dictionary<char, Color32> WhitePal = new Dictionary<char, Color32>
        {
            { 'w', new Color32(255, 255, 255, 255) },
            { 'g', new Color32(255, 255, 255, 150) },
            { 'f', new Color32(255, 255, 255, 70) },
        };

        /// <summary>4方向に光る星（9×9）。</summary>
        public static Sprite Star => FromArt("FxStar", new[]
        {
            "....f....",
            "....g....",
            "....w....",
            "...gwg...",
            "fgwwwwwgf",
            "...gwg...",
            "....w....",
            "....g....",
            "....f....",
        }, WhitePal);

        /// <summary>三日月の刃（12×12。右を向いた弧）。</summary>
        public static Sprite Crescent => FromArt("FxCrescent", new[]
        {
            "....gww.....",
            "......wwg...",
            ".......wwg..",
            "........wwg.",
            "........gwwf",
            ".........wwg",
            ".........wwg",
            "........gwwf",
            "........wwg.",
            ".......wwg..",
            "......wwg...",
            "....gww.....",
        }, WhitePal);

        /// <summary>光の十字（7×10）。</summary>
        public static Sprite HolyCross => FromArt("FxCross", new[]
        {
            "..gwg..",
            "..www..",
            "fgwwwgf",
            "wwwwwww",
            "fgwwwgf",
            "..www..",
            "..www..",
            "..www..",
            "..www..",
            "..gwg..",
        }, WhitePal);

        /// <summary>燃える隕石（10×10。左上へ炎の尾）。</summary>
        public static Sprite Meteor => FromArt("FxMeteor", new[]
        {
            "r.........",
            ".ro.......",
            "..roo.....",
            "...oyyo...",
            "...oywss..",
            "....ysSSs.",
            "....sSSSs.",
            ".....sSSs.",
            "......ss..",
            "..........",
        }, new Dictionary<char, Color32>
        {
            { 'r', new Color32(210, 60, 30, 200) },
            { 'o', new Color32(255, 150, 50, 255) },
            { 'y', new Color32(255, 230, 120, 255) },
            { 'w', new Color32(255, 255, 235, 255) },
            { 's', new Color32(120, 70, 50, 255) },
            { 'S', new Color32(80, 46, 36, 255) },
        });

        /// <summary>舞う葉（5×5）。</summary>
        public static Sprite Leaf => FromArt("FxLeaf", new[]
        {
            "...gw",
            "..gww",
            ".gwwg",
            "gwwg.",
            "wg...",
        }, WhitePal);
    }
}
