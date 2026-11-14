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
