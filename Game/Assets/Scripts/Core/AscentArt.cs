using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    // 昇る塔（クリア後）の敵の絵。灯りの戻った世界の、明るい色の敵。どれもこのゲーム用のオリジナルの意匠。
    public static partial class PixelArt
    {
        /// <summary>
        /// 昇る塔の敵（種類ごとに専用の絵）。光の雫（スライム）・灯り蛾（コウモリ）・虹晶の精（術師）・白磁の守像（岩鬼）・陽の種（爆弾虫）。
        /// 段階が上がるほど、まわりの光の縁が強くなる（中級は金、上級は白）。
        /// </summary>
        public static Sprite AscentEnemy(EnemyKind kind, int tier = 0)
        {
            tier = Mathf.Clamp(tier, 0, 2);
            string[] rows;
            switch (kind)
            {
                case EnemyKind.Bat: rows = MothRows; break;
                case EnemyKind.Caster: rows = PrismRows; break;
                case EnemyKind.Brute: rows = IvoryRows; break;
                case EnemyKind.Bomber: rows = SunSeedRows; break;
                default: rows = LumenRows; break;
            }
            string key = $"Ascent32_{kind}_{tier}";
            if (cache.TryGetValue(key, out var cached)) return cached;
            Color32? aura = tier == 0 ? (Color32?)null : tier == 1 ? new Color32(255, 214, 120, 255) : new Color32(255, 255, 240, 255);
            return Shaded(key, rows, AscentOver, aura, 0, tier == 2);
        }

        /// <summary>昇る塔の敵の色（明るい色ばかり）。C 水色・c 濃い水色・k 桃・K 濃い桃・y 淡い黄・O 橙・X 白磁の継ぎ目。</summary>
        static readonly Dictionary<char, Color32[]> AscentOver = new Dictionary<char, Color32[]>
        {
            { 'C', Ramp(214, 248, 255, 168, 226, 248, 120, 190, 232, 84, 148, 204) },
            { 'c', Ramp(150, 214, 250, 110, 182, 236, 78, 146, 214, 54, 108, 176) },
            { 'k', Ramp(255, 214, 230, 250, 178, 208, 226, 140, 182, 186, 104, 150) },
            { 'K', Ramp(250, 184, 210, 226, 146, 186, 194, 110, 160, 150, 80, 126) },
            { 'y', Ramp(255, 248, 196, 255, 230, 150, 236, 200, 110, 200, 160, 80) },
            { 'O', Ramp(255, 214, 110, 255, 176, 70, 236, 136, 46, 196, 98, 30) },
            { 'X', Ramp(226, 214, 186, 200, 186, 156, 168, 152, 122, 132, 116, 92) },
        };

        static readonly string[] LumenRows =
        {
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            ".............lllllll............",
            "...........l.........l..........",
            "................................",
            "................................",
            "...............CC...............",
            "...............CC...............",
            "..............CCCC..............",
            "..............CCCC..............",
            ".............CCCCCC.............",
            "............CCCCCCCC............",
            "............CCCCCCCC............",
            "...........CxxxCCCCCC...........",
            "...........CxxxCCCCCC...........",
            "..........CCxxxCCCCCCC..........",
            ".........CCCxxxCCCCCCCC.........",
            "........CCCCCCCCCCCCCCCC........",
            "........CCCCCecoocceCCCC........",
            "........CCCcceoooocecCCC........",
            "........CCCcccoooocccCCC........",
            "........CCCccccooccccCCC........",
            "........CCCccccccccccCCC........",
            ".........CCCCccccccCCCC.........",
            "..........CCCCCCCCCCCC..........",
            "...........CCCCCCCCCC...........",
            "................................",
            "................................",
        };
        static readonly string[] MothRows =
        {
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "...k.......................k....",
            "..kkk.....................kkk...",
            "..kkkkk.................kkkkk...",
            "..kkkllk.....H....H....kllkkk...",
            ".kkkllllkk....H..H...kkllllkkk..",
            ".kkkllllkkk....y....kkkllllkkk..",
            ".kkkkllkkkkkk.yyy.kkkkkkllkkkk..",
            "..kkkkkkkkkkkkeyyekkkkkkkkkkk...",
            "...kkkkkkkkkkyyyyykkkkkkkkkk....",
            "....kkkkkkkkkyyyyykkkkkkkkk.....",
            ".....kkkkkkkkyyyyykkkkkkkk......",
            ".............yyyyy..............",
            "........KKKKKyyyyyKKKKK.........",
            "......KKKKKKKKyyyKKKKKKKK.......",
            ".......KKKKKKKyyyKKKKKKK........",
            ".......KKKKK...y...KKKKK........",
            ".......KKKK.........KKKK........",
            "........K.............K.........",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
        };
        static readonly string[] PrismRows =
        {
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "...............xC...............",
            "..............xxCC..............",
            ".............xxxCCC.............",
            ".....pp.....xxxxCCCC............",
            ".....p......xxxxCCCC.......B....",
            "...........xxxxxCCCCC.....BB....",
            "..........xxxxxxCCCCCC..........",
            ".........xxxxxxxCCCCCCC.........",
            ".........xxxxexxCCeCCCC.........",
            "..........xxxexxCCeCCC..........",
            "...........xxxxxCCCCC...........",
            "...........xxxxxCCCCC...........",
            "............xxxooCCC............",
            "............xxxooCCC............",
            ".............xxxCCC.............",
            ".......WW.....xxCC..............",
            "..............xxCC.......l......",
            "...............xC........l......",
            "................................",
            "................................",
            "................................",
            "..............ccccc.............",
            "...............ccc..............",
            "................................",
            "................................",
        };
        static readonly string[] IvoryRows =
        {
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "............xxxxxxxx............",
            "...........xxxxxxxxxx...........",
            "...........xxxxxxxxxx...........",
            "...........gggggggggg...........",
            "...........xxxxxxxxxx...........",
            "...........xxBBxxBBxx...........",
            "...........xxxxxxxxxx...........",
            "...........xxxxxxxxxx...........",
            ".........xxxxxxxxxxxxxx.........",
            "....xxxxxxxxxxXXXXxxxxxxxxxx....",
            "....xxxxxxxxxxlllXxxxxxxxxxx....",
            "....xxxxxxxxxxlllXxxxxxxxxxx....",
            "....xxxxxxxxxxXXXXxxxxxxxxxx....",
            "....xxxxggggggXXXXggggggxxxx....",
            "....xxxxxxxxxxXXXXxxxxxxxxxx....",
            "....ggggxxxxxxXXXXxxxxxxgggg....",
            "....xxxxxxxxxxXXXXxxxxxxxxxx....",
            "....xxxxxxxxxxXXXXxxxxxxxxxx....",
            "....xxxxxxxxxxXXXXxxxxxxxxxx....",
            "...xxxxxxxxxxxXXXXxxxxxxxxxxx...",
            "...xxxxx.xxxxxx..xxxxxx.xxxxx...",
            "...xxxxx.xxxxxx..xxxxxx.xxxxx...",
            ".........xxxxxx..xxxxxx.........",
            ".........xxxxxx..xxxxxx.........",
            ".........xxxxxx..xxxxxx.........",
            ".........gggggg..gggggg.........",
            "................................",
        };
        static readonly string[] SunSeedRows =
        {
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................l.l.............",
            ".................l..............",
            ".................gl.............",
            "...............yg...............",
            "..............yygy..............",
            ".............yyyyyy.............",
            "........yyy..yyyyyy..yyy........",
            ".......yyyyy.OOOOOO.yyyyy.......",
            ".......yyyyyOOOOOOOOyyyyy.......",
            ".......yyyyOOOOOOOOOOyyyy.......",
            "........yyOOllllOOOOOOyy........",
            "......yy.OOOllllOOOOOOO.yy......",
            ".....yyyyOOOllllOOOOOOOyyyy.....",
            "....yyyyyOOOOOOOOOOOOOOyyyyy....",
            "....yyyyyOOOOeOOOOeOOOOyyyyy....",
            ".....yyyyOOOOeOOOOeOOOOyyyy.....",
            "......yy.OOOOOOOOOOOOOO.yy......",
            "........yyOOOOOeeeOOOOyy........",
            ".......yyyyOOOOOOOOOOyyyy.......",
            ".......yyyyyOOOOOOOOyyyyy.......",
            ".......yyyyy.OOOOOO.yyyyy.......",
            "........yyy..yyyyyy..yyy........",
            ".............yyyyyy.............",
            "..............yyyy..............",
            "...............yy...............",
        };
    }
}
