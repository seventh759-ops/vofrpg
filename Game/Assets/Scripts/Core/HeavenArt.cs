using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    // 天界（クリア後、昇る塔の上）の敵の絵。白い羽と金の光輪の、天使のような敵。どれもこのゲーム用のオリジナルの意匠。
    public static partial class PixelArt
    {
        /// <summary>
        /// 天界の敵（種類ごとに専用の絵）。小天使（スライム）・羽の精（コウモリ）・聖歌の天使（術師）・天の衛兵（岩鬼）・光輪の玉（爆弾虫）。
        /// まわりの光の縁は区画の色。段階が上がるほど縁が強く白い。
        /// </summary>
        public static Sprite HeavenEnemy(EnemyKind kind, ZoneSpec z, int tier = 0)
        {
            tier = Mathf.Clamp(tier, 0, 2);
            string[] rows;
            switch (kind)
            {
                case EnemyKind.Bat: rows = WispRows; break;
                case EnemyKind.Caster: rows = ChoirRows; break;
                case EnemyKind.Brute: rows = SentinelRows; break;
                case EnemyKind.Bomber: rows = HaloOrbRows; break;
                default: rows = CherubRows; break;
            }
            string key = $"Heaven32_{kind}_{(z != null ? z.Name : "")}_{tier}";
            if (cache.TryGetValue(key, out var cached)) return cached;
            var accent = z != null ? z.Accent : new Color32(255, 226, 140, 255);
            // 白い床に埋もれないよう、縁は区画の色を濃くしたもの（上級ほど濃い）
            var aura = Mix(accent, new Color32(120, 80, 30, 255), 0.25f + 0.1f * tier);
            return Shaded(key, rows, HeavenOver, aura, 0, tier >= 1);
        }

        /// <summary>天界の敵の色。x 白い羽と衣・X 羽の筋（淡い金）・y 淡い黄・k 頬の桃。</summary>
        static readonly Dictionary<char, Color32[]> HeavenOver = new Dictionary<char, Color32[]>
        {
            { 'x', Ramp(255, 255, 255, 236, 238, 248, 204, 208, 228, 160, 164, 192) },
            { 'X', Ramp(255, 240, 200, 240, 216, 160, 210, 180, 120, 170, 140, 90) },
            { 'y', Ramp(255, 248, 196, 255, 230, 150, 236, 200, 110, 200, 160, 80) },
            { 'k', Ramp(255, 200, 214, 250, 170, 190, 226, 136, 164, 186, 100, 130) },
        };

        static readonly string[] CherubRows =
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
            "................................",
            "................................",
            "................................",
            ".............lllllll............",
            "............ll.....ll...........",
            "................................",
            ".........x.....x......x.........",
            ".......Xxx..xxxxxxx...xxX.......",
            "......xxxxxxxxxxxxxxx.xxxx......",
            ".....xxXxxxxxxxxxxxxx.xxXxx.....",
            "....xxxxxxxooooxxxxxxxxxxxxx....",
            "...xXxxXxxxooooxxxxxxxxxXxxXx...",
            ".....x...xxxxxxxxxxxxx....x.....",
            ".......x.xxxxxxxxxxxxx..x.......",
            ".........xxxxexxxxexxxx.........",
            ".........xxxxxxxxxxxxx..........",
            "..........xxxxxkkxxxx...........",
            "..........xxxxxxxxxxx...........",
            "............xxxxxxx.............",
            "...............x................",
            "................................",
            "................................",
            "................................",
        };
        static readonly string[] WispRows =
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
            "................................",
            "...........xx......xx...........",
            "..........xxx......xxx..........",
            "........xxXxx......xxXxx........",
            "......xxxxxxx......xxxxxxx......",
            ".....xxXxxXxx......xxXxxXxx.....",
            "....xxxxxxxxx.BBB..xxxxxxxxx....",
            "...xXxxXxxXxxBBoBB.xxXxxXxxXx...",
            "..xxxxxxxxxxBBoooBBxxxxxxxxxxx..",
            ".XxxXxxXxxXxBBoooBBxxXxxXxxXxxX.",
            "...x.........BBoBB..........x...",
            ".....x........BBB.........x.....",
            ".......x................x.......",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
        };
        static readonly string[] ChoirRows =
        {
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            ".............lllllll............",
            "............ll.....ll...........",
            "................................",
            "............HHHHHHHH............",
            "............HHHHHHHH............",
            "..........x.HHHHHHHH.x..........",
            ".........xx.HssssssH.xx.........",
            ".......xxxx.HsessesH.xxxx.......",
            "......xxXsx..sssss...xsXxx......",
            ".....xxxxsx..sssss...xsxxxx.....",
            "....xXxxXxx....x.....xxXxxXx....",
            "....xxxxxxx...xxx....xxxxxxx....",
            "...xxXxxXxx...xxx....xxXxxXxx...",
            ".....x........gggg........x.....",
            ".......x.....xxxxx......x.......",
            ".........x...xxxxx....x.........",
            ".............xxxxx..............",
            "............xxxxxxx.............",
            "............xxxxxxx.............",
            "............xxxxxxx.............",
            "...........xxxxxxxxx............",
            "...........xxxxxxxxx............",
            "...........xxxxxxxxx............",
            "..........xxxxxxxxxxx...........",
            "..........xyyyyyyyyyy...........",
            "................................",
            "................................",
        };
        static readonly string[] SentinelRows =
        {
            "................................",
            "................................",
            "................................",
            "................................",
            "...............gg...............",
            "...............gg...............",
            "...............gg...............",
            "............mmmmmmmm............",
            "........x...mmmmmmmm...x........",
            ".......xx...mmmmmmmm...xx.......",
            "......Xxx...gggggggg...xoX......",
            ".....xxxx...mmBmmBmm...xoxx.....",
            "....xxXxx...mmmmmmmm...xoXxx....",
            "...xxxxxx...mmmmmmmm...xoxxxx...",
            "..xXxxXxx..mmmmmmmmmm..xoXxxXx..",
            "..xxxxxxx..mmmmmmmmmm..xoxxxxx..",
            ".xxXxxXxx..mmmmmmmmmm..xoXxxXxx.",
            "...x...x...mmmmmmmmmm...o...x...",
            ".....xxxxx.mmmmmmmmmm...o.x.....",
            ".....xxxxx.gggggggggg...o.......",
            "....xxxgxxxmmmmmmmmmm...o.......",
            "....xxgggxxmmmmmmmmmm...o.......",
            "....xxgggxxmmmmmmmmmm.ggggg.....",
            "....xxxgxxxmmmmmmmmmm...b.......",
            ".....xxxxx.mmmmmmmmmm...b.......",
            ".....xxxxx.mmmmmmmmmm...b.......",
            ".......x...mmmm..mmmm...........",
            "...........mmmm..mmmm...........",
            "...........mmmm..mmmm...........",
            "...........mmmm..mmmm...........",
            "...........mmmm..mmmm...........",
            "................................",
        };
        static readonly string[] HaloOrbRows =
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
            "................................",
            "................................",
            "................................",
            ".............llllll.............",
            "...........llll..llll...........",
            "..........ll........ll..........",
            ".........ll..........ll.........",
            "......x..l...yyyyyy...l..x......",
            "....Xxx.ll..yyyyyyyy..ll.xxX....",
            "...xxxx.ll..yyooyyyy..ll.xxxx...",
            "..xxXxx.l...yyooyyyy...l.xxXxx..",
            "....x...l...yyeyyyey...l...x....",
            "......x.ll..yyyyyyyy..ll.x......",
            "........xl..yyyyyyyy..lx........",
            ".........l...yyyyyy...l.........",
            ".........ll..........ll.........",
            "..........ll........ll..........",
            "...........llll..llll...........",
            ".............llllll.............",
            "................................",
            "................................",
            "................................",
            "................................",
        };
        /// <summary>
        /// 天界のボス。雲の門衛（槍の騎士）・花園の聖女（花の杖）・聖歌の指揮者（光輪と音符）・熾天の王（3対の羽、48ドットの大きな絵）。
        /// </summary>
        public static Sprite HeavenBossSprite(BossKind kind)
        {
            string[] rows;
            switch (kind)
            {
                case BossKind.GardenSaint: rows = GardenSaintRows; break;
                case BossKind.HymnConductor: rows = ConductorRows; break;
                case BossKind.SeraphKing: rows = SeraphRows; break;
                default: rows = CloudWardenRows; break;
            }
            string key = "HeavenBoss_" + kind;
            if (cache.TryGetValue(key, out var cached)) return cached;
            var aura = kind == BossKind.SeraphKing ? new Color32(255, 214, 110, 255) : new Color32(230, 180, 90, 255);
            // 熾天の王は 48 ドットを 32 ドットで1マスとして描く（ほかのボスより一回り大きい）
            return Shaded(key, rows, HeavenOver, aura, kind == BossKind.SeraphKing ? 32 : 0, true);
        }

        static readonly string[] CloudWardenRows =
        {
            "................................",
            ".......................oo.......",
            ".......................oo.......",
            "......................oooo......",
            "...............rr.......w.......",
            "...............rr.......w.......",
            "...............rr.......w.......",
            ".............mmmmmm.....w.......",
            "...xxXxxXX...mmmmmm...XxwXXxx...",
            "...xxXxxXXX..mmmmmm..XXxwXxx....",
            "...XxxXxXXx..gggggg..XxxwxxXX...",
            "...XXxxxxxx..mBmmBm..xxxwxXX....",
            "...xXxxxxxxx.mmmmmm.xxxxwxxxx...",
            "...xxxxxxxxx.mmmmmm.xxxxwXXX....",
            "....XxxxxxxxmmmmmmmmxxxxwXX.....",
            ".....XxxxxxxmmmmmmmmxxxxwX......",
            ".......xxxxxmmmmmmmmx...w.......",
            ".......xxxx.mmmmmmmm....w.......",
            "......xxxxxxgggggggg....w.......",
            "......xxxxxxmmmmmmmm....w.......",
            ".......xxxx.mmmmmmmm....w.......",
            ".......xxxx.mmmmmmmm....w.......",
            "............mmmmmmmm....w.......",
            "............mmmmmmmm....w.......",
            "............mmmmmmmm....w.......",
            "........xxxxxxxxxxxxxxxxw.......",
            ".......xxxxxxxxxxxxxxxxxw.......",
            ".......xxxxxxxxxxxxxxxxxx.......",
            ".......xxxxxxxxxxxxxxxxxx.......",
            "........xxxxxxxxxxxxxxxx........",
            "...........xxxxxxxxxx...........",
            "................................",
        };
        static readonly string[] GardenSaintRows =
        {
            "................................",
            "................................",
            "................................",
            "................................",
            "....kkkk........................",
            "....kkkk........................",
            "....kkyk....kkyykkyy............",
            "....kkkk....HHHHHHHH............",
            "....kkwxkk..HHHHHHHH..kkxxkk....",
            "...xkkwxkk..HHHHHHHH..kxxkkxx...",
            "...xxkwxkkx.HssssssH.xxxkxxx....",
            "..kkxxwxkxx.HsessesH.xxxxxkkkk..",
            "..kkkxwxxxx.Hsssss.H.xxxxxkxx...",
            "...xkxwxxxxxHsssss.Hxxxxxxxxx...",
            "...xxxwxxxxxH..x...Hxxxxxkkk....",
            "....kkwxxxxxx.xxx...xxxxxkk.....",
            "......w....x..xxx...x...........",
            "......w.......xxx...............",
            "......w......xgggg..............",
            "......w......xxxxx..............",
            "......w......xxxxx..............",
            "......w.....xxxxxxx.............",
            "......w.....xxxxxxx.............",
            "......w.....xxxxxxx.............",
            "......w....xxxxxxxxx............",
            "......w....xxxxxxxxx............",
            "......w....xxxxxxxxx............",
            "......w...xxxxxxxxxxx...........",
            "......w...xxxxxxxxxxx...........",
            "......w...xxxxxxxxxxx...........",
            "......w..xkkkkkkkkkkkk..........",
            "................................",
        };
        static readonly string[] ConductorRows =
        {
            "................................",
            "................................",
            "................................",
            "................................",
            "............lllllllll.....ll....",
            "...........l.........l....l.....",
            "............lllllllll....ll.....",
            "....ll..........................",
            "....l.......HHHHHHHH............",
            "...llxxXX...HHHHHHHH...oXxxXX...",
            ".xxXXxxXX...HHHHHHHH..oXxxXX.xx.",
            ".xxxXXxXxx..sssssss...oxxXXxxx..",
            ".XxxxXxxxxx.ssesses..xoxxxxllXX.",
            ".XXXxxxxxxx..sssss...xoxxxxlXX..",
            "..xXxxxxxxxx.sssss..xxoxxxllxx..",
            "..xxxxxxxxxx...x....xxoxxxxxx...",
            "...XXxxxxxxxx..x....xxxxxxXXX...",
            "...XXXxxxxxx..axa...xxxxxxXX....",
            "..............axa...............",
            "..............xxx...............",
            ".............axxxa..............",
            ".............axxxa..............",
            ".............axxxa..............",
            "............aaxxxaa.............",
            "............aaxxxaa.............",
            "............aaxxxaa.............",
            "...........aaaxxxaaa............",
            "...........aaxxxxxaa............",
            "...........aaxxxxxaa............",
            "..........aaaxxxxxaaa...........",
            "..........aggxxxxxggg...........",
            "................................",
        };
        static readonly string[] SeraphRows =
        {
            "................................................",
            "................................................",
            "................................................",
            "................................................",
            ".............xxXX.lllllllllllll.XXxx............",
            "...........XXxxXXlxx.........xxlXxxxXX..........",
            "...........XXxxxXXlllllllllllllXXxxXXX..........",
            "..........xXXXxxXXxXXX.....XXxXXxxXXXxx.........",
            "..........xxXXxxXXxxXX.....XXxXXxxXXxxx.........",
            ".........XxxxXxxxXxxXg.g.g.gxxXxxxXxxxXX........",
            ".........XXXxxxxxxxxXgggggggxxxxxxxxXXX.........",
            ".........xXXXxxxxxxxHHHHHHHHHxxxxxxXXXxx........",
            ".........xxXXxxxxxxxHHHHHHHHHxxxxxxXxxxx........",
            ".........xxxxxxxxxxxHHHHHHHHHxxxxxxxxxx.........",
            "..........XxxxxxxxxxHssssssxHxxxxxxXXXX.........",
            "..........XXXxxxxxxxHsssssssHxxxxxXXXX..........",
            ".............XxxxxxxHsBsssBsHxxxxxX.............",
            "...............xxxxxHssssssxHxxxx...............",
            ".........xx.......xxHssssssxHx........XX........",
            "......XXXxxxx.......Hx.ss..xH.......XXXxxxx.....",
            "....xxXXXxxxxxx......xxxxxx.......xxxxxxXXXXX...",
            "....xxxxXXXxxxxxx....xxxgxx.....xxxxxxxXXXxxx...",
            "....XXXxxxxxxxxxxxx.xxxxgxxx..xxxxxxxxxxxxxx....",
            "....xXXXXxxxxxxxxxxxxxxxgxxxxxxxxxxxxxxXXXXXX...",
            "....xxxxxxxxxxxxxxxxxxgggggxxxxxxxxxxxxxxxxxx...",
            "....XXXXXxxxxxxxxxxxxxxxgxxxxxxxxxxxxxXXXxxx....",
            "......xxxxxxxxxxx..xxxxxgxxxx..xxxxxxxxxxXX.....",
            "......xxXXXXx......xggggggggg......xxxxxxX......",
            "........X..........xxxxxgxxxx..........x........",
            "..................xxxxxxgxxxxx..................",
            "...............xxxxxxxxxgxxxxxxxxx..............",
            "...........xxxxxxxxxxxxxgxxxxxxxxxxXXX..........",
            "...........xxxxxxxxxxxxxgxxxxxxxxxxxxx..........",
            "..........XXXxxxxxxxxxxxgxxxxxxxxxxXXxx.........",
            "..........XxxxxxxxxxxxxxgxxxxxxxxxxxxXX.........",
            ".........xxxXXxxxxxxxxxxgxxxxxxxxxxxxxXX........",
            ".........xXXXxxXxxxxxxxxgxxxxxxxxXxXXxxx........",
            ".........XXxxxXXxxxxxxxxgxxxxxxxxXXxXXx.........",
            "..........xxxXXxxxxxxxxxgxxxxxxxxxXxxXX.........",
            "..........x.X.xxxxxxxxxxgxxxxxxxxXXx.X..........",
            "..............xxxxxxxxxxgxxxxxxxxX..............",
            "...............xxxxxxxxxgxxxxxxxx...............",
            "...............xxxxxxxxxgxxxxxxxx...............",
            "...............xxxxxxxxxgxxxxxxxx...............",
            "..............xxxxxxxxxxgxxxxxxxxx..............",
            "..............xggggggggggggggggggg..............",
            "................................................",
            "................................................",
        };
    }
}
