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
            { 'h', Ramp(150, 100, 70, 112, 72, 50, 80, 50, 36, 56, 34, 26) },           // 髪（茶）
            { 'H', Ramp(255, 236, 180, 230, 200, 130, 190, 150, 90, 140, 104, 60) },    // 髪・飾り（金）
            { 'a', Ramp(140, 96, 200, 104, 66, 160, 74, 46, 120, 50, 30, 86) },         // 外套（紫）
            { 'x', Ramp(250, 250, 252, 210, 214, 224, 160, 166, 180, 110, 116, 130) },  // 白
            { 'f', Ramp(190, 60, 60, 150, 40, 44, 110, 26, 32, 76, 16, 22) },           // 深紅
            { 'u', Ramp(90, 120, 190, 60, 86, 150, 40, 60, 110, 26, 40, 76) },          // 青い鎧
            { 'q', Ramp(168, 160, 150, 128, 120, 112, 92, 86, 80, 60, 56, 54) },        // 石（岩の巨人）
            { 'i', Ramp(120, 124, 144, 84, 88, 108, 58, 60, 78, 38, 40, 54) },          // 鉄（爆弾）
            { 'j', Ramp(110, 190, 130, 70, 150, 100, 44, 108, 72, 28, 72, 50) },        // 緑の衣（術士）
            { 'G', Ramp(150, 230, 140, 100, 200, 100, 64, 150, 70, 40, 100, 48) },      // スライム
            { 'D', Ramp(70, 160, 80, 48, 120, 58, 34, 88, 42, 22, 60, 30) },            // スライムの底
            { 'P', Ramp(150, 100, 190, 112, 70, 150, 80, 48, 112, 52, 30, 78) },        // 蝙蝠
            { 'M', Ramp(96, 70, 120, 70, 50, 92, 50, 34, 68, 34, 22, 48) },             // 蝙蝠の膜
        };

        /// <summary>影を付けずに光らせる素材（ランタン・香炉の火、上位職の光る刃・光輪）。</summary>
        static readonly Dictionary<char, Color32> glows = new Dictionary<char, Color32>
        {
            { 'l', new Color32(255, 230, 130, 255) },
            { 'L', new Color32(255, 170, 60, 255) },
            { 'o', new Color32(255, 250, 220, 255) },
            { 'p', new Color32(200, 130, 255, 255) }, // 紫に光る刃
            { 'R', new Color32(255, 70, 80, 255) },   // 赤く光る刃
            { 'B', new Color32(150, 210, 255, 255) }, // 青白く光る目
        };

        /// <summary>
        /// 上位職の見た目。元の職業の絵をもとに、服の色を変え（overrides）、目印の小物を描き足す（paints）。
        /// どれもこのゲーム用のオリジナルの意匠。
        /// </summary>
        static Sprite AdvancedLook(ClassType type, int index, bool third = false)
        {
            string key = $"PlayerHd_{type}_Adv{index}" + (third ? "_3rd" : "");
            if (cache.TryGetValue(key, out var cached)) return cached;
            int k = (int)type * 2 + Mathf.Clamp(index, 0, 1);
            if (third) return Shaded(key, ThirdRows[k], ThirdOver[k], ThirdOutline[k]); // 3次職は輪郭の外側が光る
            return Shaded(key, AdvRows[k], AdvOver[k]);
        }

        /// <summary>3次職の見た目（町の師匠の画面で、転生する前に見せる）。</summary>
        public static Sprite ThirdPreview(ClassType type, int advanced) => AdvancedLook(type, Mathf.Clamp(advanced, 0, 1), true);

        // 上位職と3次職の絵（1人ずつ描いたもの）。並びは 職業×2＋上位職（炎呼び・灯台守・夜狩り・血刃・陽縛り・城壁）
        static readonly string[][] AdvRows =
        {
            new[] // Flarecaller
            {
                ".........oL............L..L.....",
                "..........LLc.........LLoLL.....",
                "...........ccc........LoooL.....",
                "............cccc......LoooL.....",
                "............ccccc......LLL......",
                "......dddddddddddddd...gmmg.....",
                ".........cddddddddc....gwwg.....",
                ".........cdhhhhhhdc....gwwg.....",
                ".........cdhshhshdc.....ww......",
                ".........cdsessesdc.....ww......",
                ".........cdssssssdc.....ww......",
                "..........cdssrssd......ww......",
                "..........ccdsssdcc.....ww......",
                ".........cccdddccccc....ww......",
                "........cccccdccccccc...ww......",
                ".......ccccccdcccccccsssww......",
                ".......cccccdddcccccssssww......",
                ".......ccccbbgbbcccsss..ww......",
                "........cccbbgbbccccs...ww......",
                "........ccccdcdccccc....ww......",
                ".......cccccdccdccccc...ww......",
                ".......ccccdccccdcccc...ww......",
                "......cccccdccccdccccc..ww......",
                "......ccccdcccccdcccc...ww......",
                "......cccdccccccdcccc...ww......",
                ".....ccccdcccccccdcccc..ww......",
                ".....cccdccccccccdccc...ww......",
                ".....ddddddddddddddddd..ww......",
                "........bbbb...bbbb.....ww......",
                "........bbbb...bbbb.....ww......",
                "........................ww......",
                "........................gg......",
            },
            new[] // Beaconkeeper
            {
                "........................gg......",
                ".....................gggggggg...",
                ".....................gllllllg...",
                ".....................glLoLLlg...",
                "...........HHHHHHH...glLooLlg...",
                "..........HggogggH...glLLLLlg...",
                ".........HHddddddHH..gllllllg...",
                ".........cdhhhhhhdc..gggggggg...",
                ".........cdhshhshdc.....ww......",
                ".........cdsessesdc.....ww......",
                ".........cdssssssdc.....ww......",
                "..........cdssrssd......ww......",
                "..........ccdsssdcc.....ww......",
                ".......ggggggggggggg....ww......",
                "......gggccccdcccgggc...ww......",
                ".......ccccccdcccccccsssww......",
                ".......cccccdddcccccssssww......",
                ".......ccccbbgbbcccsss..ww......",
                "........cccbbgbbccccs...ww......",
                "........ccccdcdccccc....ww......",
                ".......cccccdccdccccc...ww......",
                ".......ccccdccccdcccc...ww......",
                "......cccccdccccdccccc..ww......",
                "......ccccdcccccdcccc...ww......",
                "......cccdccccccdcccc...ww......",
                ".....ccccdcccccccdcccc..ww......",
                ".....cccdccccccccdccc...ww......",
                ".....ddddddddddddddddd..ww......",
                "........bbbb...bbbb.....ww......",
                "........bbbb...bbbb.....ww......",
                "........................ww......",
                "........................gg......",
            },
            new[] // Nightreaver
            {
                "................................",
                "...........n......n.............",
                "............n....n..............",
                ".............nnnn...............",
                "............nnnnnn..............",
                "...........nnnnnnnn.............",
                "..........nnhhhhhhnn............",
                "..........nhhshhshhn............",
                "..........nhsesseshn............",
                "..........nnsssssssn............",
                "...........nsssssss.............",
                "..........rrrrrrrrr.............",
                ".....rrrrrrrrrrrrrr.............",
                "...rrrr...nnnnnnnnnn............",
                "..rr.....nnnbbbbbbnnn...........",
                ".......s.nnnbbbbbbnnnss.........",
                ".......s.nnnbbggbbnnnss.........",
                "......gg.nnnggggggnnngg.........",
                "......pmbnnnnnnnnnnnn.p.........",
                ".....p.bnnnnnn..nnnnnnp.........",
                ".....p..nnnnnn..nnnnnn.p........",
                "....p..nnnnnnn..nnnnnnnp........",
                ".......nnnnnnn..nnnnnnn.........",
                "......nnnnnnn....nnnnnnn........",
                "...........bbb..bbb.............",
                "...........bbb..bbb.............",
                "..........bbbb..bbbb............",
                "..........bbbb..bbbb............",
                "................................",
                "................................",
                "................................",
                "................................",
            },
            new[] // Bloodedge
            {
                "................................",
                "................................",
                "...........x.x.x.x..............",
                "...........xxxxxxxx.............",
                "..........xxxxxxxxxx............",
                "..........xxxxxxxxxx............",
                "..........nxxxxxxxxn............",
                "..........nxxsxxsxxn............",
                "..........nhsesseshn............",
                "..........nnsssssssn............",
                "...........nsssssss.............",
                "..........rrrrrrrrr.............",
                ".....rrrrrrrrrrrrrr.............",
                "...rrrr...nnnnnnnnnn............",
                "..rr.....nnnbbbbbbnnn.g.........",
                ".........nnnbbbbbbnnnss.........",
                ".........nnnbbggbbnnnss.........",
                ".......m.nnnggggggnnggggg.......",
                ".......mbnnnnnnnnnnnnRRR........",
                ".......bnnnnnn..nnnnnRRR........",
                "........nnnnnn..nnnnnnRRR.......",
                ".......nnnnnnn..nnnnnnRRR.......",
                ".......nnnnnnn..nnnnnnnRRR......",
                "......nnnnnnn....nnnnnnRRR......",
                "...........bbb..bbb.....RRR.....",
                "...........bbb..bbb.....RRR.....",
                "..........bbbb..bbbb.....RRR....",
                "..........bbbb..bbbb.....RRR....",
                "................................",
                "................................",
                "................................",
                "................................",
            },
            new[] // Sunbinder
            {
                "...........o.o.l.o.o............",
                ".........o.....ll....o..........",
                "........o.....lll.....o.........",
                "............mmmmmmm.............",
                "...........mmmmmmmmm............",
                "..........mmgggggggmm...........",
                "..........mmhhhhhhhmm...........",
                "..........mhssssssshm...........",
                "..........mhsesssesmm...........",
                "..........mmsssssssmm...........",
                "...........mmssrssmm............",
                "............mmmmmmm.............",
                ".......gggggmmmmmmmggggg........",
                "......mmmmmmmmmgmmmmmmmmm.......",
                "......mmmmmmmmgggmmmmmmmm.......",
                "........ssmmmmmgmmmmmss.........",
                "........ssbbbbbgbbbbbs..........",
                "..........vvvvvvvvvvv.m.........",
                ".........vvvvvvgvvvvvv.m........",
                ".........vvvvvgggvvvvv..m.......",
                "........vvvvvvvgvvvvvvv.m.......",
                "........vvvvvvvgvvvvvvmmmmm.....",
                ".......vvvvvvvvvvvvvvmLLLLm.....",
                ".......vvvvvvvvvvvvvvmLooLm.....",
                "......gggggggggggggggmLLLLm.....",
                "..........mmmm..mmmm..mmmm......",
                "..........mmmm..mmmm...L........",
                ".........bbbbb..bbbbb...........",
                ".........bbbbb..bbbbb...........",
                "................................",
                "................................",
                "................................",
            },
            new[] // Bastion
            {
                "................................",
                "..........g.........g...........",
                "..........gg.......gg...........",
                "............mmmmmmm.............",
                "...........mmmmmmmmm............",
                "..........mmgggggggmm...........",
                "..........mmhhhhhhhmm...........",
                "..........mhssssssshm...........",
                "..........mhsesssesmm...........",
                "..........mmsssssssmm...........",
                "...........mmssrssmm............",
                ".ggggg......mmmmmmm.............",
                "guuuuuggggggmmmmmmmggggg........",
                "guuuuugmmmmmmmmgmmmmmmmmm.......",
                "guuuuugmmmmmmmgggmmmmmmmm.......",
                "guuguug.ssmmmmmgmmmmmss.........",
                "gugggug.ssbbbbbgbbbbbs..........",
                "guuguug...vvvvvvvvvvv.m.........",
                "guuguug..vvvvvvgvvvvvv.m........",
                "guuuuug..vvvvvgggvvvvv..m.......",
                "guuuuug.vvvvvvvgvvvvvvv.m.......",
                "guuuuug.vvvvvvvgvvvvvvmmmmm.....",
                "guuuuugvvvvvvvvvvvvvvmLLLLm.....",
                "guuuuugvvvvvvvvvvvvvvmLooLm.....",
                "guuuuugggggggggggggggmLLLLm.....",
                "guuuuug...mmmm..mmmm..mmmm......",
                ".guuug....mmmm..mmmm...L........",
                "..ggg....bbbbb..bbbbb...........",
                ".........bbbbb..bbbbb...........",
                "................................",
                "................................",
                "................................",
            },
        };

        static readonly Dictionary<char, Color32[]>[] AdvOver =
        {
            new Dictionary<char, Color32[]> { { 'c', Ramp(220, 70, 70, 176, 40, 48, 128, 26, 34, 88, 16, 24) }, { 'd', Ramp(255, 214, 110, 214, 160, 56, 160, 110, 30, 110, 72, 18) } }, // Flarecaller
            new Dictionary<char, Color32[]> { { 'c', Ramp(250, 248, 236, 222, 216, 196, 178, 170, 150, 130, 122, 104) }, { 'd', Ramp(255, 230, 130, 226, 186, 72, 170, 130, 40, 120, 88, 24) } }, // Beaconkeeper
            new Dictionary<char, Color32[]> { { 'n', Ramp(110, 80, 150, 72, 50, 108, 50, 34, 78, 32, 22, 52) }, { 'r', Ramp(190, 120, 255, 150, 80, 220, 106, 52, 170, 72, 34, 120) } }, // Nightreaver
            new Dictionary<char, Color32[]> { { 'n', Ramp(120, 52, 62, 86, 34, 44, 60, 22, 30, 40, 14, 20) } }, // Bloodedge
            new Dictionary<char, Color32[]> { { 'v', Ramp(255, 244, 200, 236, 210, 120, 196, 160, 70, 140, 108, 40) }, { 'm', Ramp(255, 250, 230, 222, 214, 186, 170, 160, 130, 118, 110, 86) } }, // Sunbinder
            new Dictionary<char, Color32[]> { { 'm', Ramp(170, 190, 230, 110, 130, 176, 74, 90, 130, 46, 56, 86) }, { 'v', Ramp(70, 96, 170, 44, 64, 130, 30, 44, 94, 20, 28, 64) } }, // Bastion
        };

        static readonly string[][] ThirdRows =
        {
            new[] // Pyrelord
            {
                "........L.o.L..........L..L.....",
                "........LoLoL.........LLoLL.....",
                "...........ccc........LoooL.....",
                "....L.......cccc......LoooL.....",
                "............ccccc......LLL......",
                "......dddddddddddddd...gmmg.....",
                ".........cddddddddc....gwwg.....",
                ".........cdhhhhhhdc....gwwg.....",
                ".........cdhshhshdc.....ww......",
                ".........cdsessesdc.....ww......",
                "..o......cdssssssdc.....ww......",
                "..........cdssrssd......ww......",
                "..........ccdsssdcc.....ww..o...",
                ".........cccLLLccccc....ww......",
                "........cccccLccccccc...ww......",
                ".......ccccccLcccccccsssww......",
                ".......cccccLLLcccccssssww......",
                ".......ccccbbgbbcccsss..ww......",
                "........cccbbgbbccccs...ww......",
                "........ccccLcLccccc....ww......",
                ".L.....cccccLccLccccc...ww......",
                ".......ccccLccccLcccc...ww......",
                "......cccccLccccLccccc..ww......",
                "......ccccLcccccLcccc...ww......",
                "......cccLccccccLcccc...ww..L...",
                ".....ccccLcccccccLcccc..ww......",
                ".....cccLccccccccLccc...ww......",
                ".....LLLLLLLLLLLLLLLLL..ww......",
                "........bbbb...bbbb.....ww......",
                "........bbbb...bbbb.....ww......",
                "........................ww......",
                "........................gg......",
            },
            new[] // Starwarden
            {
                "........................o.......",
                ".......................ooo......",
                ".....................ooooooo....",
                "...l..................ooooo.....",
                "...........HHHHHHH....oo.oo.....",
                "..........HggogggH...o.....o....",
                ".........HHddddddHH....gwwg.....",
                ".........cdhhhhhhdc....gwwg.....",
                ".........cdhshhshdc.....ww......",
                ".........cdsessesdc.....ww......",
                ".........cdssssssdc.....ww......",
                "..........cdssrssd......ww......",
                ".o........ccdsssdcc.....ww......",
                ".......ggggggggggggg....ww......",
                "......gggccccdcccgggc...ww......",
                ".......ccccccdcccccccsssww......",
                ".......ccoccdddcccccssssww......",
                ".......ccccbbgbbcccsss..ww......",
                "........cccbbgbbcoccs...ww......",
                "........ccccdcoccccc....ww......",
                ".......cccccdccdccccc...ww......",
                ".......ccccdccccdcccc...ww......",
                "......ccccodccccdccccc..ww......",
                "......ccccdcccccdcccc...ww......",
                "......cccdccccccocccc...ww......",
                ".....cccodcccccccdcccc..ww......",
                ".....cccdccccccccdccc...ww.o....",
                ".....ddddddddddddddddd..ww......",
                "........bbbb...bbbb.....ww......",
                "........bbbb...bbbb.....ww......",
                "........................ww......",
                "........................gg......",
            },
            new[] // Eclipse
            {
                "............ppppp...............",
                "..........pp.....pp.............",
                ".........p..n....n.p............",
                ".........p...nnnn..p............",
                "............nnnnnn..............",
                "...........nnnnnnnn.............",
                "...p......nnhhhhhhnn............",
                "..........nhhshhshhn............",
                "..........nhspsspshn............",
                "..........nnsssssssn............",
                "...........nsssssss.............",
                "..........rrrrrrrrr.............",
                ".....rrrrrrrrrrrrrr.............",
                "...rrrr...nnnnnnnnnn............",
                "..rr.....nnnbbbbbbnnn.....o.....",
                ".......s.nnnbbbbbbnnnss.........",
                ".......s.nnnbbggbbnnnss.........",
                "......gg.nnnggggggnnngg.........",
                "......pmbnnnnnnnnnnnn.p.........",
                ".....p.bnnnnnn..nnnnnnp.........",
                ".....p..nnnnnn..nnnnnn.p........",
                "....p..nnnnnnn..nnnnnnnp........",
                ".......nnnnnnn..nnnnnnn.........",
                "......nnnnnnn....nnnnnnn........",
                "...........bbb..bbb.............",
                "...........bbb..bbb.............",
                "..........bbbb..bbbb......p.....",
                "..........bbbb..bbbb............",
                "................................",
                "................................",
                "................................",
                "................................",
            },
            new[] // Bloodsovereign
            {
                "...........g.g.g.g..............",
                "...........ggggggg..............",
                "...........gRgggRg..............",
                "...........xxxxxxxx.............",
                "..........xxxxxxxxxx............",
                "..........xxxxxxxxxx............",
                "...R......nxxxxxxxxn............",
                "..........nxxsxxsxxn............",
                "..........nhsRssRshn............",
                "..........nnsssssssn............",
                "...........nsssssss.............",
                "..........rrrrrrrrr.............",
                "....ffrrrrrrrrrrrrr.............",
                "...fffr...nnnnnnnnnn............",
                "..rfff...nnnbbbbbbnnn.g.........",
                "....ff...nnnbbbbbbnnnss.........",
                ".........nnnbbggbbnnnss.........",
                ".......m.nnnggggggnnggggg.......",
                ".......mbnnnnnnnnnnnnRRR........",
                ".......bnnnnnn..nnnnnRRR........",
                "........nnnnnn..nnnnnnRRR..R....",
                ".......nnnnnnn..nnnnnnRRR.......",
                ".......nnnnnnn..nnnnnnnRRR......",
                "......nnnnnnn....nnnnnnRRR......",
                "...........bbb..bbb.....RRR.....",
                "...........bbb..bbb.....RRR.....",
                "..........bbbb..bbbb.....RRR....",
                "..........bbbb..bbbb.....RRR....",
                "................................",
                "................................",
                "................................",
                "................................",
            },
            new[] // Dawnbringer
            {
                "...........o.o.o.o.o............",
                ".........o.....ll....o..........",
                "........o.....lll.....o.........",
                "............mmmmmmm.............",
                "...........mmmmmmmmm............",
                "..........mmgggggggmm...........",
                "..........mmhhhhhhhmm...........",
                ".oo.......mhssssssshm........oo.",
                "oool......mhslssslsmm.......looo",
                "ooool.....mmsssssssmm......loooo",
                ".oool......mmssrssmm.......looo.",
                "..ool.......mmmmmmm........loo..",
                ".......gggggmmmmmmmggggg........",
                "......mmmmmmmmmgmmmmmmmmm.......",
                "......mmmmmmmmgggmmmmmmmm.......",
                "........ssmmmmmgmmmmmss.........",
                "........ssbbbbbgbbbbbs..........",
                "..........vvvvvvvvvvv.m.........",
                ".........vvvvvvgvvvvvv.m........",
                ".........vvvvvgggvvvvv..m.......",
                "........vvvvvvvgvvvvvvv.m.......",
                "........vvvvvvvgvvvvvvmmmmm.....",
                ".......vvvvvvvvvvvvvvmLLLLm.....",
                ".......vvvvvvvvvvvvvvmLooLm.....",
                "......gggggggggggggggmLLLLm.....",
                "..........mmmm..mmmm..mmmm......",
                "..........mmmm..mmmm...L........",
                ".........bbbbb..bbbbb...........",
                ".........bbbbb..bbbbb...........",
                "................................",
                "................................",
                "................................",
            },
            new[] // Aegis
            {
                "................................",
                "..........g.........g...........",
                "..o.......gg.......gg...........",
                "............mmmmmmm.............",
                ".......xxx.mmmmmmmmm.xxx........",
                "......xxxxmmgggggggmmxxxx.......",
                ".......xxxmmhhhhhhhmmxxx........",
                "..........mhssssssshm...........",
                "..........mhsBsssBsmm...........",
                "..........mmsssssssmm...........",
                "...........mmssrssmm............",
                ".ggggg......mmmmmmm.............",
                "guuuuuggggggmmmmmmmggggg........",
                "guuuuugmmmmmmmmgmmmmmmmmm.......",
                "guuuuugmmmmmmmgggmmmmmmmm.......",
                "ouuouuo.ssmmmmmgmmmmmss.........",
                "ouooouo.ssbbbbbgbbbbbs..........",
                "ouuouuo...vvvvvvvvvvv.m.........",
                "ouuouuo..vvvvvvgvvvvvv.m........",
                "guuuuug..vvvvvgggvvvvv..m.......",
                "guuuuug.vvvvvvvgvvvvvvv.m.......",
                "guuuuug.vvvvvvvgvvvvvvmmmmm.....",
                "guuuuugvvvvvvvvvvvvvvmLLLLm.....",
                "guuuuugvvvvvvvvvvvvvvmLooLm.....",
                "guuuuugggggggggggggggmLLLLm.....",
                "guuuuug...mmmm..mmmm..mmmm......",
                ".guuug....mmmm..mmmm...L........",
                "..ggg....bbbbb..bbbbb...........",
                ".........bbbbb..bbbbb.......o...",
                "................................",
                "................................",
                "................................",
            },
        };

        static readonly Dictionary<char, Color32[]>[] ThirdOver =
        {
            new Dictionary<char, Color32[]> { { 'c', Ramp(150, 40, 46, 110, 24, 32, 76, 14, 22, 50, 8, 14) }, { 'd', Ramp(255, 214, 110, 214, 160, 56, 160, 110, 30, 110, 72, 18) } }, // Pyrelord
            new Dictionary<char, Color32[]> { { 'c', Ramp(70, 86, 170, 44, 56, 130, 30, 38, 94, 18, 24, 64) }, { 'd', Ramp(255, 230, 130, 226, 186, 72, 170, 130, 40, 120, 88, 24) } }, // Starwarden
            new Dictionary<char, Color32[]> { { 'n', Ramp(70, 54, 100, 44, 32, 70, 30, 20, 50, 18, 12, 32) }, { 'r', Ramp(190, 120, 255, 150, 80, 220, 106, 52, 170, 72, 34, 120) } }, // Eclipse
            new Dictionary<char, Color32[]> { { 'n', Ramp(120, 52, 62, 86, 34, 44, 60, 22, 30, 40, 14, 20) } }, // Bloodsovereign
            new Dictionary<char, Color32[]> { { 'v', Ramp(255, 244, 200, 236, 210, 120, 196, 160, 70, 140, 108, 40) }, { 'm', Ramp(255, 250, 230, 222, 214, 186, 170, 160, 130, 118, 110, 86) } }, // Dawnbringer
            new Dictionary<char, Color32[]> { { 'm', Ramp(170, 190, 230, 110, 130, 176, 74, 90, 130, 46, 56, 86) }, { 'v', Ramp(70, 96, 170, 44, 64, 130, 30, 44, 94, 20, 28, 64) } }, // Aegis
        };

        static readonly Color32[] ThirdOutline =
        {
            new Color32(255, 130, 40, 255), // Pyrelord
            new Color32(150, 200, 255, 255), // Starwarden
            new Color32(170, 100, 255, 255), // Eclipse
            new Color32(255, 60, 70, 255), // Bloodsovereign
            new Color32(255, 210, 90, 255), // Dawnbringer
            new Color32(190, 225, 255, 255), // Aegis
        };

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
        static Sprite Shaded(string key, string[] rows, Dictionary<char, Color32[]> overrides = null, Color32? auraColor = null, int ppu = 0)
        {
            if (cache.TryGetValue(key, out var cached)) return cached;
            int h = rows.Length, w = rows[0].Length;
            var outline = new Color32(22, 16, 30, 255); // 輪郭はいつも暗い線（3次職の光は、その外側に足す）
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
            // 3次職：暗い輪郭のさらに外側に、半透明の光の縁を1ドット
            if (auraColor.HasValue)
            {
                var ac = auraColor.Value;
                ac.a = 160;
                var halo = (Color32[])px.Clone();
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int i = y * w + x;
                        if (px[i].a > 0) continue;
                        bool near = (x > 0 && px[i - 1].a > 0) || (x < w - 1 && px[i + 1].a > 0) || (y > 0 && px[i - w].a > 0) || (y < h - 1 && px[i + w].a > 0);
                        if (near) halo[i] = ac;
                    }
                }
                px = halo;
            }
            // ppu を指定しなければ、絵の幅をちょうど1マスに収める（キャラ・敵は 32 ドットで 1 マス）
            return Store(key, MakeTexture(key, w, h, px, TextureWrapMode.Clamp), ppu > 0 ? ppu : w);
        }

        static readonly string[] LampwrightHdRows =
        {
            "........................mm......",
            ".............cc.......mmmmmm....",
            ".............ccc......mlllLm....",
            "............cccc......mlLoLm....",
            "...........ccccccc....mlLLlm....",
            "..........ccccccccc...mllllm....",
            ".........cddddddddc...mmmmmm....",
            ".........cdhhhhhhdc....gwwg.....",
            ".........cdhshhshdc.....ww......",
            ".........cdsessesdc.....ww......",
            ".........cdssssssdc.....ww......",
            "..........cdssrssd......ww......",
            "..........ccdsssdcc.....ww......",
            ".........cccdddccccc....ww......",
            "........cccccdccccccc...ww......",
            ".......ccccccdcccccccsssww......",
            ".......cccccdddcccccssssww......",
            ".......ccccbbgbbcccsss..ww......",
            "........cccbbgbbccccs...ww......",
            "........ccccdcdccccc....ww......",
            ".......cccccdccdccccc...ww......",
            ".......ccccdccccdcccc...ww......",
            "......cccccdccccdccccc..ww......",
            "......ccccdcccccdcccc...ww......",
            "......cccdccccccdcccc...ww......",
            ".....ccccdcccccccdcccc..ww......",
            ".....cccdccccccccdccc...ww......",
            ".....ddddddddddddddddd..ww......",
            "........bbbb...bbbb.....ww......",
            "........bbbb...bbbb.....ww......",
            "........................ww......",
            "........................gg......",
        };

        static readonly string[] ShadebladeHdRows =
        {
            "................................",
            "................................",
            "................................",
            ".............nnnn...............",
            "............nnnnnn..............",
            "...........nnnnnnnn.............",
            "..........nnhhhhhhnn............",
            "..........nhhshhshhn............",
            "..........nhsesseshn............",
            "..........nnsssssssn............",
            "...........nsssssss.............",
            "..........rrrrrrrrr.............",
            ".....rrrrrrrrrrrrrr.............",
            "...rrrr...nnnnnnnnnn............",
            "..rr.....nnnbbbbbbnnn.g.........",
            ".........nnnbbbbbbnnnss.........",
            ".........nnnbbggbbnnnss.........",
            ".......m.nnnggggggnnngggg.......",
            ".......mbnnnnnnnnnnnn.mm........",
            ".......bnnnnnn..nnnnnnmm........",
            "........nnnnnn..nnnnnn.mm.......",
            ".......nnnnnnn..nnnnnnnmm.......",
            ".......nnnnnnn..nnnnnnn.mm......",
            "......nnnnnnn....nnnnnnnmm......",
            "...........bbb..bbb.......mm....",
            "...........bbb..bbb.......m.....",
            "..........bbbb..bbbb.......m....",
            "..........bbbb..bbbb............",
            "................................",
            "................................",
            "................................",
            "................................",
        };

        static readonly string[] WardenHdRows =
        {
            "...............r................",
            "...............rr...............",
            "..............rrr...............",
            "............mmmmmmm.............",
            "...........mmmmmmmmm............",
            "..........mmgggggggmm...........",
            "..........mmhhhhhhhmm...........",
            "..........mhssssssshm...........",
            "..........mhsesssesmm...........",
            "..........mmsssssssmm...........",
            "...........mmssrssmm............",
            "............mmmmmmm.............",
            ".......gggggmmmmmmmggggg........",
            "......mmmmmmmmmgmmmmmmmmm.......",
            "......mmmmmmmmgggmmmmmmmm.......",
            "........ssmmmmmgmmmmmss.........",
            "........ssbbbbbgbbbbbs..........",
            "..........vvvvvvvvvvv.m.........",
            ".........vvvvvvgvvvvvv.m........",
            ".........vvvvvgggvvvvv..m.......",
            "........vvvvvvvgvvvvvvv.m.......",
            "........vvvvvvvgvvvvvvmmmmm.....",
            ".......vvvvvvvvvvvvvvmLLLLm.....",
            ".......vvvvvvvvvvvvvvmLooLm.....",
            "......gggggggggggggggmLLLLm.....",
            "..........mmmm..mmmm..mmmm......",
            "..........mmmm..mmmm...L........",
            ".........bbbbb..bbbbb...........",
            ".........bbbbb..bbbbb...........",
            "................................",
            "................................",
            "................................",
        };

        /// <summary>スキルのアイコン（効果の種類ごとの形を、スキルの色で塗る）。</summary>
        public static Sprite SkillIcon(SkillEffect effect, Color color)
        {
            int i;
            switch (effect)
            {
                case SkillEffect.Barrage: i = 1; break;
                case SkillEffect.Dash: i = 2; break;
                case SkillEffect.Strike: i = 3; break;
                case SkillEffect.Buff: i = 4; break;
                case SkillEffect.Heal: i = 5; break;
                default: i = 0; break;
            }
            Color32 c = new Color(color.r, color.g, color.b, 1f);
            string key = $"Skill16_{effect}_{c.r}_{c.g}_{c.b}";
            return Shaded(key, SkillIconRows[i], new Dictionary<char, Color32[]> { { 'K', RampFrom(c) } });
        }







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

        public static Sprite Fireball => Shaded("Fireball12", FireballRows, null, null, 32);

        public static Sprite Ember => Shaded("Ember10", EmberRows, null, null, 32);

        /// <summary>
        /// 装備のアイコン（16ドット）。武器は種類ごとに形を描き分け、ランタン・防具（外套）・指輪もそろえた絵にする。
        /// r の部分が珍しさの色になる。
        /// </summary>
        public static Sprite ItemIcon(Item item)
        {
            if (item == null) return null;
            string[] rows;
            string kind;
            switch (item.Slot)
            {
                case ItemSlot.Lantern: rows = LanternIcon16; kind = "Lantern"; break;
                case ItemSlot.Armor: rows = ArmorIcon16; kind = "Armor"; break;
                case ItemSlot.Ring: rows = RingIcon16; kind = "Ring"; break;
                default:
                    if (!WeaponIcons.TryGetValue(item.Weapon, out rows)) return ItemIcon(item.Slot, item.Rarity);
                    kind = item.Weapon.ToString();
                    break;
            }
            Color32 c = RarityInfo.ColorOf(item.Rarity);
            string key = $"ItemIcon16_{kind}_{item.Rarity}";
            if (cache.TryGetValue(key, out var cached)) return cached;
            var pal = new Dictionary<char, Color32>
            {
                { 'k', new Color32(16, 12, 22, 255) },
                { 'w', new Color32(220, 226, 238, 255) },
                { 'b', new Color32(150, 100, 60, 255) },
                { 'y', new Color32(240, 190, 70, 255) },
                { 'o', new Color32(255, 140, 40, 255) },
                { 'l', new Color32(255, 236, 150, 255) },
                { 'r', c },
            };
            int h = rows.Length, w = rows[0].Length;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[(h - 1 - y) * w + x] = pal.TryGetValue(rows[y][x], out var col) ? col : new Color32(0, 0, 0, 0);
            return Store(key, MakeTexture(key, w, h, px, TextureWrapMode.Clamp), 32); // 8ドットのアイコンと同じ大きさ
        }

        static readonly string[] LanternIcon16 =
        {
            "......kkkk......",
            ".....ky..yk.....",
            "......kkkk......",
            ".....kyyyyk.....",
            "....kyyyyyyk....",
            "....krllllrk....",
            "....krloolrk....",
            "....krloolrk....",
            "....kroooork....",
            "....krloolrk....",
            "....kyyyyyyk....",
            ".....kyyyyk.....",
            "......kkkk......",
            "................",
            "................",
            "................",
        };

        static readonly string[] ArmorIcon16 =
        {
            "................",
            "..kkk......kkk..",
            ".kyyyk....kyyyk.",
            ".kyyrrkkkkrryyk.",
            "..kkrrrkkrrrkk..",
            "...krrwkkrrrk...",
            "...krwrrrrrrk...",
            "...krrrkkrrrk...",
            "...krrrkkrrrk...",
            "...kyyyyyyyyk...",
            "...kryrkkryrk...",
            "...krrk..krrk...",
            "...kkk....kkk...",
            "................",
            "................",
            "................",
        };

        static readonly string[] RingIcon16 =
        {
            "................",
            "................",
            "......kkkk......",
            ".....krwrrk.....",
            ".....krrrrk.....",
            "......kyyk......",
            "....kkyyyykk....",
            "...kyyk..kyyk...",
            "..kyk......kyk..",
            "..kyk......kyk..",
            "..kyk......kyk..",
            "...kyk....kyk...",
            "....kyykkyyk....",
            ".....kkkkkk.....",
            "................",
            "................",
        };

        static readonly Dictionary<WeaponKind, string[]> WeaponIcons = new Dictionary<WeaponKind, string[]>
        {
            { WeaponKind.Wand, new[]
            {
                "............kkk.",
                "...........krrrk",
                "..........krlrrk",
                "..........krrrrk",
                "..........kkrrk.",
                ".........kbkkk..",
                "........kbk.....",
                ".......kbk......",
                "......kbk.......",
                ".....kbk........",
                "....kbk.........",
                "...kbk..........",
                "..kbk...........",
                ".kbk............",
                ".kk.............",
                "................",
            } },
            { WeaponKind.Repeater, new[]
            {
                "...........kk...",
                "..........krk.kk",
                "...........k.krk",
                "........kk..k.k.",
                ".......krrkkbk..",
                "........kkkbk...",
                ".......kbbbk....",
                "......kbykbk....",
                ".....kbk.kk.....",
                "....kbk.........",
                "...kbk..........",
                "..kbk...........",
                ".kbk............",
                ".kk.............",
                "................",
                "................",
            } },
            { WeaponKind.Fan, new[]
            {
                "..........k.k.k.",
                ".........krkrkrk",
                "..........krrrk.",
                ".........krlrk..",
                ".........kyyk...",
                "........kbkk....",
                ".......kbk......",
                "......kbk.......",
                ".....kbk........",
                "....kbk.........",
                "...kbk..........",
                "..kbk...........",
                ".kbk............",
                ".kk.............",
                "................",
                "................",
            } },
            { WeaponKind.Sword, new[]
            {
                "............kkk.",
                "...........kwwk.",
                "..........kwwrk.",
                ".........kwwrk..",
                "........kwwrk...",
                ".......kwwrk....",
                "......kwwrk.....",
                "..k..kwwrk......",
                "..kykwwrk.......",
                "...kyyrk........",
                "...kbyyk........",
                "..kbkkyk........",
                ".kbk..kk........",
                ".kyk............",
                "..k.............",
                "................",
            } },
            { WeaponKind.Dagger, new[]
            {
                "................",
                "................",
                "................",
                "..........kkk...",
                ".........kwwk...",
                "........kwwrk...",
                ".......kwwrk....",
                "....k.kwwrk.....",
                "....kykwrk......",
                ".....kyyk.......",
                ".....kbyyk......",
                "....kbk.kk......",
                "...kbk..........",
                "...kyk..........",
                "....k...........",
                "................",
            } },
            { WeaponKind.Greatblade, new[]
            {
                "..........kkkkk.",
                ".........kwwwwk.",
                "........kwwwwrk.",
                ".......kwwwwrk..",
                "......kwwwwrk...",
                ".....kwwwwrk....",
                "....kwwwwrk.....",
                ".k.kwwwwrk......",
                ".kykwwwrk.......",
                "..kyyyrk........",
                "..kbyyyk........",
                ".kbk.kyk........",
                "kbk...kk........",
                "kyk.............",
                ".k..............",
                "................",
            } },
            { WeaponKind.Censer, new[]
            {
                ".......kk.......",
                ".......kwk......",
                "........kw......",
                ".......kwk......",
                "........kw......",
                ".......kkkk.....",
                "......kyyyyk....",
                ".....kyrrrryk...",
                "....kyolololyk..",
                "....kyollllyk...",
                "....kyyyyyyyk...",
                ".....kyrrrryk...",
                "......kyyyyk....",
                ".......kkkk.....",
                "........kk......",
                "................",
            } },
            { WeaponKind.Chain, new[]
            {
                "..kk............",
                "..kwk...........",
                "...kwk..........",
                "..kkkkk.........",
                "..krlrk.........",
                "..krrrk.........",
                "...kkkwk........",
                "......kwk.kk....",
                ".......kwkwk....",
                "........kkkkk...",
                "........krlrk...",
                "........krrrk...",
                ".........kkkwk..",
                "............kwk.",
                ".............kk.",
                "................",
            } },
            { WeaponKind.Brazier, new[]
            {
                "................",
                "......k..k......",
                ".....kok.kok....",
                "....kolok.olk...",
                "....kollolllk...",
                "...kolllllllok..",
                "..kkkkkkkkkkkkk.",
                "..kyrrrrrrrrryk.",
                "...kyyyyyyyyyk..",
                "....kyyyyyyyk...",
                "......kbbbk.....",
                ".......kbk......",
                ".......kbk......",
                ".....kkbbbkk....",
                "....kbbbbbbbk...",
                "....kkkkkkkkk...",
            } },
        };

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

        /// <summary>南京錠（閉じている＝金色／開いている＝灰色）。鍛冶屋の再抽選で効果を固定するボタンに使う。</summary>
        public static Sprite Padlock(bool closed) => closed ? FromArt("PadlockClosed", PadlockClosedRows, PadlockPalette) : FromArt("PadlockOpen", PadlockOpenRows, PadlockPalette);

        static readonly Dictionary<char, Color32> PadlockPalette = new Dictionary<char, Color32>
        {
            { 'k', new Color32(30, 24, 20, 255) },
            { 'm', new Color32(200, 205, 215, 255) },
            { 'y', new Color32(255, 205, 70, 255) },
            { 'g', new Color32(130, 130, 140, 255) },
        };

        static readonly string[] PadlockClosedRows =
        {
            "....kkkk....",
            "...kmmmmk...",
            "..kmk..kmk..",
            "..km....mk..",
            "..km....mk..",
            ".kkkkkkkkkk.",
            ".kyyyyyyyyk.",
            ".kyyykkyyyk.",
            ".kyyykkyyyk.",
            ".kyyyykyyyk.",
            ".kyyyyyyyyk.",
            ".kkkkkkkkkk.",
        };

        static readonly string[] PadlockOpenRows =
        {
            "....kkkk....",
            "...kmmmmk...",
            "..kmk..kmk..",
            "..km....mk..",
            "........mk..",
            ".kkkkkkkkkk.",
            ".kggggggggk.",
            ".kgggkkgggk.",
            ".kgggkkgggk.",
            ".kggggkgggk.",
            ".kggggggggk.",
            ".kkkkkkkkkk.",
        };


        /// <summary>宝の間の宝箱（閉じている／開いた）。</summary>
        public static Sprite Chest(bool open) => open ? Shaded("ChestOpen28", ChestOpenRows, null, null, 32) : Shaded("ChestClosed28", ChestClosedRows, null, null, 32);



        // ---- 置き物と弾（陰影つき、1 マス = 32 ドットの細かさ）----
        static readonly string[] ChestClosedRows =
        {
            "............................",
            "...wwggwwwwwwwwwwwwwwggww...",
            "..wwwggwwwwwwwwwwwwwwggwww..",
            ".wwwwggwwwwwwwwwwwwwwggwwww.",
            ".wwwwggwwwwwwwwwwwwwwggwwww.",
            ".wwwwggwwwwwwwwwwwwwwggwwww.",
            ".wwwwggwwwwggggggwwwwggwwww.",
            ".gggggggggggggggggggggggggg.",
            ".gggggggggggggggggggggggggg.",
            ".ggggggggggggnngggggggggggg.",
            ".wwwwggwwwwggnnggwwwwggwwww.",
            ".wwwwggwwwwggggggwwwwggwwww.",
            ".bbbbggbbbbggggggbbbbggbbbb.",
            ".wwwwggwwwwwwwwwwwwwwggwwww.",
            ".wwwwggwwwwwwwwwwwwwwggwwww.",
            ".wwwwggwwwwwwwwwwwwwwggwwww.",
            ".bbbbggbbbbbbbbbbbbbbggbbbb.",
            ".wwwwggwwwwwwwwwwwwwwggwwww.",
            ".wwwwggwwwwwwwwwwwwwwggwwww.",
            ".gggggggggggggggggggggggggg.",
        };

        static readonly string[] ChestOpenRows =
        {
            "..wwwggwwwwwwwwwwwwwwggwww..",
            "..wwwggwwwwwwwwwwwwwwggwww..",
            "..wwwggwwwwwwwwwwwwwwggwww..",
            "..wwwggwwwwwwwwwwwwwwggwww..",
            "..gggggggggggggggggggggggg..",
            "..lllllllolllllllollllllll..",
            "..llllllgglllollgglllllggl..",
            "..llggllllllggllllllggllll..",
            "..llllllllggllllllggllllll..",
            ".gggggggggggggggggggggggggg.",
            ".wwwwggwwwwggggggwwwwggwwww.",
            ".wwwwggwwwwggggggwwwwggwwww.",
            ".bbbbggbbbbggggggbbbbggbbbb.",
            ".wwwwggwwwwwwwwwwwwwwggwwww.",
            ".wwwwggwwwwwwwwwwwwwwggwwww.",
            ".wwwwggwwwwwwwwwwwwwwggwwww.",
            ".bbbbggbbbbbbbbbbbbbbggbbbb.",
            ".wwwwggwwwwwwwwwwwwwwggwwww.",
            ".wwwwggwwwwwwwwwwwwwwggwwww.",
            ".gggggggggggggggggggggggggg.",
        };

        static readonly string[] AltarRows =
        {
            "................................",
            "................L...............",
            "..............LLLL..............",
            "............LLLLLLLL............",
            "............LLLllLLL............",
            "...........LLLllllLLL...........",
            "...........LLllllllLL...........",
            "...........LLlloollLL...........",
            "...........LLloooolLL...........",
            "............LloooolL............",
            "............LLloolLL............",
            ".........mmmmmLllLmmmmm.........",
            ".........mmmmmmmmmmmmmm.........",
            ".......qqqqmmmmmmmmmmqqqq.......",
            ".......qqqqqqqqqqqqqqqqqq.......",
            ".......qqqqqqqqqqqqqqqqqq.......",
            ".......gggggggggggggggggg.......",
            ".........qqqqqqqqqqqqqq.........",
            ".........qqqqqqqqqqqqqq.........",
            ".........qqqqqqnnqqqqqq.........",
            ".........qqqqqqnnqqqqqq.........",
            ".........qqqqnnnnnnqqqq.........",
            ".........qqqqnnnnnnqqqq.........",
            ".........qqqqqqnnqqqqqq.........",
            ".........qqqqqqnnqqqqqq.........",
            ".........qqqqqqqqqqqqqq.........",
            ".........qqqqqqqqqqqqqq.........",
            "......gggggggggggggggggggg......",
            "......qqqqqqqqqqqqqqqqqqqq......",
            "......qqqqqqqqqqqqqqqqqqqq......",
            "....qqqqqqqqqqqqqqqqqqqqqqqq....",
            "....qqqqqqqqqqqqqqqqqqqqqqqq....",
        };

        static readonly string[] FireballRows =
        {
            "............",
            "...LLLLLL...",
            "..LLLLLLLL..",
            ".LLLLlllLLL.",
            ".LLLlllllLL.",
            ".LLllloollL.",
            ".LLlloooolL.",
            ".LLllooollL.",
            ".LLLllollLL.",
            "..LLLlllLL..",
            "...LLLLLL...",
            "............",
        };

        static readonly string[] EmberRows =
        {
            "..........",
            "....LL....",
            "...LllL...",
            "..LllllL..",
            ".LlloollL.",
            ".LlloollL.",
            "..LllllL..",
            "...LllL...",
            "....LL....",
            "..........",
        };

        // ---- アイコン（16 ドット、縁取りと陰影つき）----
        // スキルは K の素材をスキルの色で塗る。並びは Nova, Barrage, Dash, Strike, Buff, Heal。
        static readonly string[][] SkillIconRows =
        {
            new[]
            {
                "................",
                "....KKKKKKKK....",
                "...KKKKKKKKKK...",
                "..KKK......KKK..",
                ".KKK........KKK.",
                ".KK..K.....K.KK.",
                ".KK...Koo.K..KK.",
                ".KK...oooo...KK.",
                ".KK...oooo...KK.",
                ".KK....ooK...KK.",
                ".KK...K...K..KK.",
                ".KKK.K.....KKKK.",
                "..KKK......KKK..",
                "...KKKKKKKKKK...",
                "....KKKKKKKK....",
                "................",
            },
            new[]
            {
                "................",
                "...........KK...",
                "..........KKKK..",
                "..KKKKKKKKKKoK..",
                "...........KK...",
                "................",
                ".........KK.....",
                "........KKKK....",
                "KKKKKKKKKKoK....",
                ".........KK.....",
                "................",
                "............KK..",
                "...........KKKK.",
                "...KKKKKKKKKKoK.",
                "............KK..",
                "................",
            },
            new[]
            {
                "................",
                "................",
                ".........K......",
                ".........KK.....",
                ".........KKK....",
                "...KKKKKKKKKK...",
                "KK.KKKKKKKKKKK..",
                "KK.KKKKKKKKKKKK.",
                "KK.KKKKKKKKoKKK.",
                "KK.KKKKKKKKKKK..",
                ".........KKKK...",
                ".........KKK....",
                ".........KK.....",
                ".........K......",
                "................",
                "................",
            },
            new[]
            {
                ".KK.K...........",
                "..KK.K..........",
                "...KK.K.........",
                "....KK.K........",
                ".....KK.K.......",
                "......KK.KK.....",
                ".......KKKKKK...",
                ".......KKKKKK...",
                "......KKKooKKK..",
                "......KKKKKKKK..",
                ".......KKKKKK...",
                ".......KKKKKK...",
                ".........KK.....",
                "................",
                "...nnnnnnnnnnnnn",
                "................",
            },
            new[]
            {
                "................",
                ".......KK.......",
                "......KKKK......",
                ".....KKKKKK.....",
                "....KKKKoKKK....",
                "...KKKKKoKKKK...",
                "..KKKKKKKKKKKK..",
                ".KKKKKKKKKKKKKK.",
                ".....KKKKK......",
                ".....KKKKK......",
                ".....KKKKK......",
                ".....KKKKK......",
                ".....KKKKK......",
                ".....KKKKK......",
                ".....KKKKK......",
                "................",
            },
            new[]
            {
                "................",
                ".....KKKKKK.....",
                ".....KoKKKK.....",
                ".....KoKKKK.....",
                ".....KKKKKK.....",
                ".KKKKKKKKKKKKKK.",
                ".KKKKKKKKKKKKKK.",
                ".KKKKKKKKKKKKKK.",
                ".KKKKKKKKKKKKKK.",
                ".KKKKKKKKKKKKKK.",
                ".KKKKKKKKKKKKKK.",
                ".....KKKKKK.....",
                ".....KKKKKK.....",
                ".....KKKKKK.....",
                ".....KKKKKK.....",
                "................",
            },
        };

        static readonly string[][] TownIconRows =
        {
            new[]
            {
                "................",
                "................",
                "....wwwwwwww....",
                "..wwwwwwwwwwww..",
                "..wwwwwwwwwwww..",
                ".wwwwwwwwwwwwww.",
                ".wwwwwwwwwwwwww.",
                ".wwwwwggggwwwww.",
                ".nnnnnggggnnnnn.",
                ".ggggggnngggggg.",
                ".wwwwwggggwwwww.",
                ".wwwwwwwwwwwwww.",
                ".wwwwwwwwwwwwww.",
                ".wwwwwwwwwwwwww.",
                ".wwwwwwwwwwwwww.",
                "................",
            },
            new[]
            {
                "................",
                "................",
                ".....b.....b....",
                "......bbbbb.....",
                "......bbbbb.....",
                ".....nnnnnnn....",
                "....bbbbbbbb....",
                "...bbbbggbbbb...",
                "..bbbbggggbbbb..",
                "..bbbggglggbbb..",
                "..bbbggglggbbb..",
                "..bbbbggggbbbb..",
                "...bbbbggbbbb...",
                "....bbbbbbbb....",
                ".....bbbbbb.....",
                "................",
            },
            new[]
            {
                "................",
                "................",
                "..nnaaaaaaaaaa..",
                "..nnaaaaaaaaaa..",
                "..nnaaaagaaaaa..",
                "..nnaaaagaaaaa..",
                "..nnaaagggaaaa..",
                "..nnaagggggaaa..",
                "..nnaagglggaaa..",
                "..nnaaagggaaaa..",
                "..nnaaaagaaaaa..",
                "..nnaaaagaaaaa..",
                "..nnaaaaaaaaaa..",
                "..nnxxxxxxxxxx..",
                "..nnxxxxxxxxxx..",
                "................",
            },
            new[]
            {
                "................",
                ".....bbbbbb.....",
                "....jjjjjjj.....",
                "...jjjjjjjjj....",
                "...jjjjjjjjjj...",
                "...jjjjjjjjjj...",
                "...jjjjjjjjjj...",
                "...jjjjjjjjjj...",
                "...nnnnnnnnnn...",
                "...jjbbggbbjj...",
                "...jjjjggjjjj...",
                "...jjjjggjjjj...",
                "...jjjjjjjjjj...",
                "...jjjjjjjjjj...",
                "...jjjjjjjjjj...",
                "................",
            },
            new[]
            {
                "................",
                ".........ww.....",
                ".......mmmmw....",
                ".......mmmmww...",
                ".......mmmm.ww..",
                ".............ww.",
                "................",
                "................",
                ".mmmmmmmmmmmmm..",
                "..mmmmmmmmmmmm..",
                "......mmmm......",
                "......mmmm......",
                "......mmmm......",
                "....mmmmmmmm....",
                "....mmmmmmmm....",
                "................",
            },
        };

        static readonly string[][] BoonIconRows =
        {
            new[]
            {
                "................",
                "........r.......",
                "........r.......",
                ".......rrr......",
                ".......rrrr.....",
                ".....r.rrrr.....",
                "....rr.LLrrr....",
                "...rrrrLLLrrr...",
                "...rrrLLLLrrr...",
                "...rrrLLLLrrr...",
                "...rrrLllLrrr...",
                "...rrrLllLrrr...",
                "....rrLllLrr....",
                "....rrrLLrrr....",
                "................",
                "................",
            },
            new[]
            {
                "..........g.....",
                ".........gg.....",
                "........gl......",
                "........lg......",
                ".......gl.......",
                "......glg.......",
                ".....gglggggg...",
                "....gglggggg....",
                "...gggggggg.....",
                ".......ggg......",
                ".......gg.......",
                ".......g........",
                "......gg........",
                "......g.........",
                ".....g..........",
                "................",
            },
            new[]
            {
                "................",
                ".......L........",
                "......LLL.......",
                "......LlL.......",
                ".......l........",
                ".......n........",
                "......xxxx......",
                "......xxxx......",
                "......xxxx......",
                "......xxxx......",
                "......xxxx......",
                "......xxxx......",
                "......xxxx......",
                "....gggggggg....",
                "....gggggggg....",
                "................",
            },
            new[]
            {
                "................",
                "................",
                "................",
                "................",
                "....xxxxxxxx....",
                "...xxxccccxxx...",
                "..xxxccocccxxx..",
                ".xxxxccnnccxxxx.",
                ".xxxxccnnccxxxx.",
                "..xxxccccccxxx..",
                "...xxxccccxxx...",
                "....xxxxxxxx....",
                "................",
                "................",
                "................",
                "................",
            },
            new[]
            {
                "................",
                "......xxxx......",
                "....xxxxxxxn....",
                "...xxxxxxxxxn...",
                "...xxxxxxxxxx...",
                "..xxxxxxxxxxxx..",
                "..xxnnnxxxnnnx..",
                "..xxnnnxxxnnnx..",
                "..xxnnnxxxnnnx..",
                "...xxxxxnxxxx...",
                "...xxxxnxxxxx...",
                "....xxxxxxxx....",
                ".....xxxxxxx....",
                ".....xnxnxnx....",
                ".....xnxnxnx....",
                "................",
            },
            new[]
            {
                "................",
                "................",
                "....rr....rr....",
                "..rrrrrrrrrrrr..",
                "..rrrorrrrrrrr..",
                ".rrrorrrrrrrrrr.",
                ".rrrrrrrrrrrrrr.",
                "..rrrrrrrrrrrr..",
                "..rrrrrrrrrrrr..",
                "....rrrrrrrr....",
                ".....rrrrrr.....",
                ".....rrrrrr.....",
                "......rrrr......",
                ".......rr.......",
                "................",
                "................",
            },
            new[]
            {
                "................",
                ".......cc.......",
                ".......cc.......",
                "......cccc......",
                "......cccc......",
                ".....cccccc.....",
                "....cccccccc....",
                "....cccccccc....",
                "...ccccBccccc...",
                "...cccBcccccc...",
                "...cccBcccccc...",
                "...cccccccccc...",
                "....cccccccc....",
                "....cccccccc....",
                "......cccc......",
                "................",
            },
            new[]
            {
                "................",
                "................",
                "................",
                ".............xx.",
                "......bbbbbxxxx.",
                "......bbbbxxxx..",
                "......bbbbxxxx..",
                "......bbbbxxxx..",
                "......bbbbxxx...",
                "......bbbbb.....",
                "......bbbbb.....",
                "...bbbbbbbbbb...",
                "...bbbbbbbbbb...",
                "...bbbbbbbbbb...",
                "...nnnnnnnnnn...",
                "................",
            },
            new[]
            {
                "................",
                "..gggggggggggg..",
                "..gggggggggggg..",
                "..gmmmmmmmmmgg..",
                "..gmmmmggmmmgg..",
                "..gmmmmggmmmgg..",
                "..gmmggggggmgg..",
                "..ggmggggggmgg..",
                "...ggmmggmmgg...",
                "....gmmggmmg....",
                ".....gmggmg.....",
                ".....gggggg.....",
                "......gggg......",
                ".......gg.......",
                "................",
                "................",
            },
            new[]
            {
                "................",
                ".ffffffffffffff.",
                ".ffffffffffffff.",
                "..xxxxx..xxxxx..",
                "...xxx....xxx...",
                "...xxx....xxx...",
                "...xxx....xxx...",
                "....x......x....",
                "....x......x....",
                "....x......x....",
                "....x...f..x....",
                "......ffff......",
                "......foff......",
                "......ffff......",
                "......ffff......",
                "................",
            },
            new[]
            {
                "................",
                "....aaaaaaaa....",
                "...aaa....aaa...",
                "..aa........aa..",
                ".aa..aaaaaa..aa.",
                ".aa.aa....aa.aa.",
                ".a..a..pp..a..a.",
                ".a..a.pppp.a..a.",
                ".a..a.pppp.a..a.",
                ".a..a..pp..a..a.",
                ".aa.aa....aa.aa.",
                ".aa..aaaaaa..aa.",
                "..aa........aa..",
                "...aaa....aaa...",
                "....aaaaaaaa....",
                "................",
            },
            new[]
            {
                "................",
                ".......rr.......",
                ".......rr.......",
                ".......rr.......",
                "....rrLLLLrr....",
                "....rLLLLLLr....",
                "....LLLllLLL....",
                ".rrrLLllllLLrrr.",
                ".rrrLLllllLLrrr.",
                "....LLLllLLL....",
                "....rLLLLLLr....",
                "....rrLLLLrr....",
                ".......rr.......",
                ".......rr.......",
                ".......rr.......",
                "................",
            },
        };


        // ---- 敵（32 ドット、キャラと同じ陰影つきの描き方）----
        // 段階 0 下級・1 中級・2 上級。上級ほど角・牙・とげ・光る赤い目が付き、体の色が暗く厳つくなる。

        /// <summary>段階に応じて体の色を暗くする（上級ほど暗い）。</summary>
        static Dictionary<char, Color32[]> TierDarken(int tier, params char[] parts)
        {
            if (tier <= 0) return null;
            float f = tier == 1 ? 0.85f : 0.7f;
            var o = new Dictionary<char, Color32[]>();
            foreach (var p in parts) o[p] = Scale(ramps[p], f);
            return o;
        }

        static Color32[] Scale(Color32[] r, float f)
        {
            var o = new Color32[r.Length];
            for (int i = 0; i < r.Length; i++) o[i] = new Color32((byte)(r[i].r * f), (byte)(r[i].g * f), (byte)(r[i].b * f), 255);
            return o;
        }

        /// <summary>1色から4段階の濃淡を作る（明るい・元・暗い・いちばん暗い）。</summary>
        static Color32[] RampFrom(Color32 c, float f = 1f)
        {
            var b = new Color32((byte)(c.r * f), (byte)(c.g * f), (byte)(c.b * f), 255);
            return new[] { Mix(b, new Color32(255, 255, 255, 255), 0.3f), b, Mix(b, new Color32(0, 0, 0, 255), 0.28f), Mix(b, new Color32(0, 0, 0, 255), 0.5f) };
        }

        /// <summary>敵の絵（段階つき）。スライムはここでは緑（区画の色は SlimeFor）。</summary>
        public static Sprite EnemySprite(EnemyKind kind, int tier = 0)
        {
            tier = Mathf.Clamp(tier, 0, 2);
            switch (kind)
            {
                case EnemyKind.Bat: return Shaded($"Bat32_{tier}", BatRows32[tier], TierDarken(tier, 'P', 'M'));
                case EnemyKind.Caster: return Shaded($"Caster32_{tier}", CasterRows32[tier], TierDarken(tier, 'j'));
                case EnemyKind.Brute: return Shaded($"Brute32_{tier}", BruteRows32[tier], TierDarken(tier, 'q'));
                case EnemyKind.Bomber: return Shaded($"Bomber32_{tier}", BomberRows32[tier], TierDarken(tier, 'i'));
                case EnemyKind.Golden: return GoldenSlime;
                default: return Shaded($"Slime32_{tier}", SlimeRows32[tier], TierDarken(tier, 'G', 'D'));
            }
        }

        /// <summary>区画の色のスライム（段階つき。上級ほど暗い）。</summary>
        public static Sprite SlimeFor(ZoneSpec z, int tier = 0)
        {
            tier = Mathf.Clamp(tier, 0, 2);
            float f = tier == 0 ? 1f : tier == 1 ? 0.85f : 0.7f;
            return Shaded($"Slime32_{z.Name}_{tier}", SlimeRows32[tier], new Dictionary<char, Color32[]>
            {
                { 'G', RampFrom(z.Slime, f) },
                { 'D', RampFrom(z.SlimeDark, f) },
            });
        }

        public static Sprite Slime => EnemySprite(EnemyKind.Slime);

        /// <summary>黄金のスライム（出会うと逃げる、倒すとドロップがあふれる）。銀の王冠ときらめきで、金色の区画のスライムと見分ける。</summary>
        public static Sprite GoldenSlime => Shaded("GoldenSlime32", GoldenRows32, new Dictionary<char, Color32[]>
        {
            { 'G', Ramp(255, 236, 140, 240, 196, 60, 196, 148, 30, 140, 100, 20) },
            { 'D', Ramp(214, 160, 40, 170, 120, 24, 128, 88, 16, 90, 60, 10) },
        });

        static readonly string[] GoldenRows32 =
        {
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "...............mm...............",
            "............m..mm..m............",
            "............m..mm..m....o.......",
            "............mmmRRmmm............",
            "............mmmmmmmm............",
            "................................",
            "............GGGGGGGG............",
            "....o....GGGGGGGGGGGGGG.........",
            "...o.o..GGGGGGGGGGGGGGGG........",
            "....o..GGGGGGGGGGGGGGGGGG.......",
            "......GGGooGGGGGGGGGGGGGGG.o....",
            ".....GGGoGGGGGGGGGGGGGGGGGo.o...",
            "....GGGGGGGGGGGGGGGGGGGGGGGo....",
            "....GGGGGGGoeGGGGGGoeGGGGGGG....",
            "....GGGGGGGeeGGGGGGeeGGGGGGG....",
            "....GGGGGGGeeGGGGGGeeGGGGGGG....",
            "....GGGGGGGGGGGGGGGGGGGGGGGG....",
            "....GGGGGGGGGGGeeGGGGGGGGGGG....",
            ".....GGGGGGGGGGGGGGGGGGGGGG.....",
            "......DDDDDDDDDDDDDDDDDDDD......",
            ".......DDDDDDDDDDDDDDDDDD.......",
            "........DDDDDDDDDDDDDDDD........",
            ".....DDDDDDDDDDDDDDDDDDDDDD.....",
            "................................",
            "................................",
        };

        static readonly string[][] SlimeRows32 =
        {
            new[]
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
                "................................",
                "............GGGGGGGG............",
                ".........GGGGGGGGGGGGGG.........",
                "........GGGGGGGGGGGGGGGG........",
                ".......GGGGGGGGGGGGGGGGGG.......",
                "......GGGooGGGGGGGGGGGGGGG......",
                ".....GGGoGGGGGGGGGGGGGGGGGG.....",
                "....GGGGGGGGGGGGGGGGGGGGGGGG....",
                "....GGGGGGGoeGGGGGGoeGGGGGGG....",
                "....GGGGGGGeeGGGGGGeeGGGGGGG....",
                "....GGGGGGGeeGGGGGGeeGGGGGGG....",
                "....GGGGGGGGGGGGGGGGGGGGGGGG....",
                "....GGGGGGGGGGGeeGGGGGGGGGGG....",
                ".....GGGGGGGGGGGGGGGGGGGGGG.....",
                "......DDDDDDDDDDDDDDDDDDDD......",
                ".......DDDDDDDDDDDDDDDDDD.......",
                "........DDDDDDDDDDDDDDDD........",
                ".....DDDDDDDDDDDDDDDDDDDDDD.....",
                "................................",
                "................................",
            },
            new[]
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
                "........x..............x........",
                ".........x............x.........",
                ".........x..GGGGGGGG..x.........",
                ".........GGGGGGGGGGGGGG.........",
                "........GGGGGGGGGGGGGGGG........",
                ".......GGGGGGGGGGGGGGGGGG.......",
                "......GGGooGGGGGGGGGGGGGGG......",
                ".....GGGoGeGGGGGGGGGGeGGGGG.....",
                "....GGGGGGGeGGGGGGGGeGGGGGGG....",
                "....GGGGGGGoeGGGGGGoeGGGGGGG....",
                "....GGGGGGGeeGGGGGGeeGGGGGGG....",
                "....GGGGGGGeeGGGGGGeeGGGGGGG....",
                "....GGGGGGGGGGGGGGGGGGGGGGGG....",
                "....GGGGGGGGGexeexeGGGGGGGGG....",
                ".....GGGGGGGGeeeeeeGGGGGGGG.....",
                "......DDDDDDDDDDDDDDDDDDDD......",
                ".......DDDDDDDDDDDDDDDDDD.......",
                "........DDDDDDDDDDDDDDDD........",
                ".....DDDDDDDDDDDDDDDDDDDDDD.....",
                "................................",
                "................................",
            },
            new[]
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
                ".......xx..............xx.......",
                "........xx............xx........",
                "........xx............xx........",
                ".........xx..........xx.........",
                ".........xx.GGGGGGGG.xx.........",
                ".........GGGGGGGGGGGGGG.........",
                "........GGGGGGGGGGGGGGGG........",
                ".......GGGGGGGGGGGGGGGGGG.......",
                "......GGGooGGGGGGGGGGGGGGG......",
                ".....GGGoGeGGGGGGGGGGeGGGGG.....",
                "....GGGGGGGeGGGGGGGGeGGGGGGG....",
                "....GGGGGGGRRGGGGGGRRGGGGGGG....",
                "....GGGGGGGRRGGGGGGRRGGGGGGG....",
                "....GGGGGGGRRGGGGGGRRGGGGGGG....",
                "....GGGGGGGGGGGGGGGGGGGGGGGG....",
                "....GGGGGGGGGexeexeGGGGGGGGG....",
                ".....GGGGGGGGxeeeexGGGGGGGG.....",
                "......DDDDDDDDDDDDDDDDDDDD......",
                ".......DDDDDDDDDDDDDDDDDD.......",
                "........DDDDDDDDDDDDDDDD........",
                ".....DDDDDDDDDDDDDDDDDDDDDD.....",
                "................................",
                "................................",
            },
        };

        static readonly string[][] BatRows32 =
        {
            new[]
            {
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                ".............P....P.............",
                ".PP..........P....P..........PP.",
                ".MMPPPP.....PP....PP.....PPPPMM.",
                "..MMMMMPPPP.PPPPPPPP.PPPPMMMMM..",
                "..MMMMMMMMMPPPPPPPPPPMMMMMMMMM..",
                "..MMMMMMMMMMPPPPPPPPMMMMMMMMMM..",
                "..MMMMMM.MMPPPPPPPPPPMM.MMMMMM..",
                "..MMMMM...MPPPPPPPPPPM...MMMMM..",
                "...MM..MPPPPPPRPPRPPPPPPM..MM...",
                "...MMPPPMMPPPPPPPPPPPPMMPPPMM...",
                "...PPMMMMMPPPPPPPPPPPPMMMMMPP...",
                ".....MMMMP.PPPPPPPPPP.PMMMM.....",
                "......MMP...PPPxxPPP...PMM......",
                ".......P.....PPPPPP.....P.......",
                "..............PPPP..............",
                "..............n..n..............",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
            },
            new[]
            {
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                ".............P....P.............",
                ".PP..........P....P..........PP.",
                ".MMPPPP.....PP....PP.....PPPPMM.",
                "..MMMMMPPPP.PPPPPPPP.PPPPMMMMM..",
                "..MMMMMMMMMPPPPPPPPPPMMMMMMMMM..",
                "..MMMMMMMMMMPPPPPPPPMMMMMMMMMM..",
                "..MMMMMM.MMPPPPPPPPPPMM.MMMMMM..",
                "..MMMMM...MPPPRPPRPPPM...MMMMM..",
                "...MM..MPPPPPPRPPRPPPPPPM..MM...",
                "...MMPPPMMPPPPPPPPPPPPMMPPPMM...",
                "...PPMMMMMPPPPPPPPPPPPMMMMMPP...",
                ".....MMMMP.PPPPPPPPPP.PMMMM.....",
                "......MMP...PPPxxPPP...PMM......",
                ".......P.....PPxxPP.....P.......",
                "..............PPPP..............",
                "..............n..n..............",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
            },
            new[]
            {
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "...........x........x...........",
                "...........x........x...........",
                "x...........xP....Px...........x",
                ".PP.........xP....Px.........PP.",
                ".MMPPPP.....PP....PP.....PPPPMM.",
                "..MMMMMPPPP.PPPPPPPP.PPPPMMMMM..",
                "..MMMMMMMMMPPPPPPPPPPMMMMMMMMM..",
                "..MMMMMMMMMMPPPPPPPPMMMMMMMMMM..",
                "..MMMMMM.MMPPPPPPPPPPMM.MMMMMM..",
                "..MMMMM...MPPPRPPRPPPM...MMMMM..",
                "...MM..MPPPPPPRPPRPPPPPPM..MM...",
                "...MMPPPMMPPPPPPPPPPPPMMPPPMM...",
                "...PPMMMMMPPPPPPPPPPPPMMMMMPP...",
                "..x..MMMMP.PPPPPPPPPP.PMMMM..x..",
                "......MMP...PPPxxPPP...PMM......",
                ".......P.....PPxxPP.....P.......",
                "..............PPPP..............",
                "..............n..n..............",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
            },
        };

        static readonly string[][] CasterRows32 =
        {
            new[]
            {
                "................................",
                "................................",
                "................................",
                ".............jjjjjj.............",
                "............jjjjjjjj............",
                "...........jjjjjjjjjj...........",
                "..........jjjjjjjjjjjj..........",
                ".........jjjjjjjjjjjjjj.........",
                ".........jjjjjnnnnjjjjj.........",
                ".........jjjjnnnnnnjjjj.........",
                ".........jjjnnnnnnnnjjj.........",
                ".........jjjnnnnnnnnjjj.........",
                ".........jjjnnlnnlnnjjj.........",
                "..........jjnnnnnnnnjj..........",
                ".........jjjjnnnnnnjjjj.........",
                "...BB....jjjjjnnnnjjjjj.........",
                "..BBBB..jjjjjjjjjjjjjjjj........",
                ".BBBBBB.jjjjjjjjjjjjjjjj........",
                ".BBBoBBjjjjjjjjjjjjjjjjjj.......",
                "..BBBBjjjjddddddddddddjjj.......",
                "...BBjjjjjjjjjjjjjjjjjjjjj......",
                "....ssjjjjjjjjjjjjjjjjjjjj......",
                ".....jjjjjjjjjjjjjjjjjjjjjj.....",
                ".....jjjjjjjjjjjjjjjjjjjjjj.....",
                "....jjjjjjjjjjjjjjjjjjjjjjjj....",
                "....jjjjjjjjjjjjjjjjjjjjjjjj....",
                "...jjjjjjjjjjjjjjjjjjjjjjjjjj...",
                "...jjjjjjjjjjjjjjjjjjjjjjjjjj...",
                "..jjjjjjjjjjjjjjjjjjjjjjjjjjjj..",
                "..dddddddddddddddddddddddddddd..",
                "................................",
                "................................",
            },
            new[]
            {
                "................................",
                "................................",
                "................................",
                ".............jjjjjj.............",
                "............jjjjjjjj............",
                "...........jjjjjjjjjj...........",
                "..........jjjjjjjjjjjj..........",
                ".........jjjjjjjjjjjjjj.........",
                ".........jjjjjnnnnjjjjj.........",
                ".........jjjjnnnnnnjjjj.........",
                ".........jjjnnnnnnnnjjj.........",
                ".........jjjnnnnnnnnjjj.........",
                ".........jjjnnlnnlnnjjj.........",
                "..........jjnnnnnnnnjj..........",
                ".........jjjjnnnnnnjjjj.........",
                "...BB....jjjjjnnnnjjjjj.........",
                "..BBBB..jggggggggggggggj........",
                ".BBBBBB.jjjjjjjjjjjjjjjj........",
                ".BBBoBBjjjjjjjjjjjjjjjjjj.......",
                "..BBBBjjjjggggggggggggjjj.......",
                "...BBjjjjjjjjjjjjjjjjjjjjj......",
                "....ssjjjjjjjjjjjjjjjjjjjj......",
                ".....jjjjjjjjjjjjjjjjjjjjjj.....",
                ".....jjjjjjjjjjjjjjjjjjjjjj.....",
                "....jjjjjjjjjjjjjjjjjjjjjjjj....",
                "....jjjjjjjjjjjjjjjjjjjjjjjj....",
                "...jjjjjjjjjjjjjjjjjjjjjjjjjj...",
                "...jjjjjjjjjjjjjjjjjjjjjjjjjj...",
                "..jjjjjjjjjjjjjjjjjjjjjjjjjjjj..",
                "..gggggggggggggggggggggggggggg..",
                "................................",
                "................................",
            },
            new[]
            {
                "................................",
                "........x.......x......x........",
                ".........x......x.....x.........",
                ".........x...jjjjjj...x.........",
                "..........x.jjjjjjjj.x..........",
                "..........xjjjjjjjjjjx..........",
                "..........jjjjjjjjjjjj..........",
                ".........jjjjjjjjjjjjjj.........",
                ".........jjjjjnnnnjjjjj.........",
                ".........jjjjnnnnnnjjjj.........",
                ".........jjjnnnnnnnnjjj.........",
                ".........jjjnnnnnnnnjjj.........",
                ".........jjjnnRnnRnnjjj.........",
                "..........jjnnnnnnnnjj..........",
                ".........jjjjnnnnnnjjjj.........",
                "...RR....jjjjjnnnnjjjjj.........",
                "..RRRR..jggggggggggggggj........",
                ".RRRRRR.jjjjjjjjjjjjjjjj........",
                ".RRRoRRjjjjjjjjjjjjjjjjjj.......",
                "..RRRRjjjjggggggggggggjjj.......",
                "...RRjjjjjjjjjjjjjjjjjjjjj......",
                "....ssjjjjjjjjjjjjjjjjjjjj......",
                ".....jjjjjjjjjjjjjjjjjjjjjj.....",
                ".....jjjjjjjjjjjjjjjjjjjjjj.....",
                "....jjjjjjjjjjjjjjjjjjjjjjjj....",
                "....jjjjjjjjjjjjjjjjjjjjjjjj....",
                "...jjjjjjjjjjjjjjjjjjjjjjjjjj...",
                "...jjjjjjjjjjjjjjjjjjjjjjjjjj...",
                "..jjjjjjjjjjjjjjjjjjjjjjjjjjjj..",
                "..gggggggggggggggggggggggggggg..",
                "................................",
                "................................",
            },
        };

        static readonly string[][] BruteRows32 =
        {
            new[]
            {
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                ".........qqqqqqqqqqqqqq.........",
                ".........qqqqqqqqqqqqqq.........",
                ".........qqqqqqqqqqqqqq.........",
                ".........qqqLLqqqqLLqqq.........",
                ".........qqqqqqqqqqqqqq.........",
                ".........qqqqqqqqqqqqqq.........",
                "........qqqqqqqqqqqqqqqq........",
                "...qqqqqqqqqqqqqqqqqqqqqqqqqq...",
                "...qqqqqqqqqqqqqqqqqqqqqqqqqq...",
                "...qqqqqqqqnqqqqqqqqqqqqqqqqq...",
                "...qqqqqqqqqnqqLLqqqqqqqqqqqq...",
                "...qqqqqqqqqnqqLLqqqqqqqqqqqq...",
                "...qqqqqqqqqqqqLLqqqqqqqqqqqq...",
                "...qqqqqqqqqqqqqqqqqnqqqqqqqq...",
                "...qqqqqqqqqqqqqqqqqqnqqqqqqq...",
                "...qqqqqqqqqqqqqqqqnqqqqqqqqq...",
                "...qqqqqqqqqqqqqqqqqqqqqqqqqq...",
                "...qqqqqqqqqqqqqqqqqqqqqqqqqq...",
                "..qqqqqqqqqqqqqqqqqqqqqqqqqqqq..",
                "..qqqqqqqqqqqqqqqqqqqqqqqqqqqq..",
                "..qqqqqqqqqqqqq..qqqqqqqqqqqqq..",
                "..qqqqqqqqqqqqq..qqqqqqqqqqqqq..",
                "..........qqqqq..qqqqq..........",
                "..........qqqqq..qqqqq..........",
                "..........qqqqq..qqqqq..........",
                "................................",
            },
            new[]
            {
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                ".........qqqqqqqqqqqqqq.........",
                ".........qqqqqqqqqqqqqq.........",
                ".........qqqqqqqqqqqqqq.........",
                ".........qqqLLqqqqLLqqq.........",
                ".........qqqqqqqqqqqqqq.........",
                ".........qqqqqqqqqqqqqq.........",
                "...GGGGGGqqqqqqqqqqqqqqGGGGGG...",
                "...GGGGGGqqqqqqqqqqqqqqGGGGGG...",
                "...qqqqqqqqqqqqqqqqqqqqqqqqqq...",
                "...qqqqqqqqnqqqqqqqqqqqqqqqqq...",
                "...qqqqqqqqqnqqLLqqqqqqqqqqqq...",
                "...qqqqqqqqqnqqLLqqqqqqqqqqqq...",
                "...qqqqqqqqqqqqLLqqqqqqqqqqqq...",
                "...qqqqqqqqqqqqqqqqqnqqqqqqqq...",
                "...qqqqqqqqqqqqqqqqqqnqqqqqqq...",
                "...qqqqqqqqqqqqqqqqnqqqqqqqqq...",
                "...qqqqqqqqqqqqqqqqqqqqqqqqqq...",
                "...qqqqqqqqqqqqqqqqqqqqqqqqqq...",
                "..qqqqqqqqqqqqqqqqqqqqqqqqqqqq..",
                "..qqqqqqqqqqqqqqqqqqqqqqqqqqqq..",
                "..qqqqqqqqqqqqq..qqqqqqqqqqqqq..",
                "..qqqqqqqqqqqqq..qqqqqqqqqqqqq..",
                "..........qqqqq..qqqqq..........",
                "..........qqqqq..qqqqq..........",
                "..........qqqqq..qqqqq..........",
                "................................",
            },
            new[]
            {
                "................................",
                "................................",
                "................................",
                "................................",
                "..........x..x....x..x..........",
                "..........x..x....x..x..........",
                ".........qqqqqqqqqqqqqq.........",
                ".........qqqqqqqqqqqqqq.........",
                ".........qqqqqqqqqqqqqq.........",
                ".........qqqRRqqqqRRqqq.........",
                "....x..x.qqqqqqqqqqqqqq.x..x....",
                "....x..x.qqqqqqqqqqqqqq.x..x....",
                "...GGGGGGqqqqqqqqqqqqqqGGGGGG...",
                "...GGGGGGqqqqqqqqqqqqqqGGGGGG...",
                "...qqqqqqqqqqqqqqqqqqqqqqqqqq...",
                "...qqqqqqqqnqqqqqqqqqqqqqqqqq...",
                "...qqqqqqqqqnqqRRqqqqqqqqqqqq...",
                "...qqqqqqqqqnqqRRqqqqqqqqqqqq...",
                "...qqqqqqqqqqqqRRqqqqqqqqqqqq...",
                "...qqqqqqqqqqqqqqqqqnqqqqqqqq...",
                "...qqqqqqqqqqqqqqqqqqnqqqqqqq...",
                "...qqqqqqqqqqqqqqqqnqqqqqqqqq...",
                "...qqqqqqqqqqqqqqqqqqqqqqqqqq...",
                "...qqqqqqqqqqqqqqqqqqqqqqqqqq...",
                "..qqqqqqqqqqqqqqqqqqqqqqqqqqqq..",
                "..qqqqqqqqqqqqqqqqqqqqqqqqqqqq..",
                "..qqqqqqqqqqqqq..qqqqqqqqqqqqq..",
                "..qqqqqqqqqqqqq..qqqqqqqqqqqqq..",
                "..........qqqqq..qqqqq..........",
                "..........qqqqq..qqqqq..........",
                "..........qqqqq..qqqqq..........",
                "................................",
            },
        };

        static readonly string[][] BomberRows32 =
        {
            new[]
            {
                "................................",
                ".....................Lo.........",
                ".....................lL.........",
                "...................ww...........",
                "..................w.............",
                ".................w..............",
                "..............iiii..............",
                "..............iiii..............",
                ".............iiiiii.............",
                "...........iiiiiiiiii...........",
                ".........iiiiiiiiiiiiii.........",
                "........iiiiiiiiiiiiiiii........",
                "........iiiiiiiiiiiiiiii........",
                ".......iiiiiiiiiiiiiiiiii.......",
                ".......iiiiiiiiiiiiiiiiii.......",
                "......iiiiiiiiiiiiiiiiiiii......",
                "......iiiiiiRRiiiiRRiiiiii......",
                "......iiiiiiRRiiiiRRiiiiii......",
                "......iiiiiiiiiiiiiiiiiiii......",
                "......iiiiiiiiiiiiiiiiiiii......",
                "......iiiiiiiiiiiiiiiiiiii......",
                ".......iiiiiiinnnniiiiiii.......",
                ".......iiiiiiiiiiiiiiiiii.......",
                "........iiiiiiiiiiiiiiii........",
                "........iiiiiiiiiiiiiiii........",
                ".........iiiiiiiiiiiiii.........",
                "...........iiiiiiiiii...........",
                "..........bbbiiiiiibbb..........",
                "..........bbb......bbb..........",
                "..........bbb......bbb..........",
                "................................",
                "................................",
            },
            new[]
            {
                "................................",
                ".....................Lo.........",
                ".....................lL.........",
                "...................ww...........",
                "..................w.............",
                ".................w..............",
                "..............iiii..............",
                "..............iiii..............",
                ".............iiiiii.............",
                "...........iiiiiiiiii...........",
                ".........iiiiiiiiiiiiii.........",
                "........iiiiiiiiiiiiiiii........",
                "........iiiiiiiiiiiiiiii........",
                ".......iiiiiiiiiiiiiiiiii.......",
                ".......iigiiiiiiiiiiiiigi.......",
                "......iiiiiniiiiiiiiniiiii......",
                "......iiiiiiRRiiiiRRiiiiii......",
                "......iiiiiiRRiiiiRRiiiiii......",
                "......iiiiiiiiiiiiiiiiiiii......",
                "......iiiiiiiiiiiiiiiiiiii......",
                "......iigiiiiiiiiiiiiiiigi......",
                ".......iiiiiiinnnniiiiiii.......",
                ".......iiiiiiiiiiiiiiiiii.......",
                "........iiiiiiiiiiiiiiii........",
                "........iiiiiiiiiiiiiiii........",
                ".........igiiiiiiiiiiig.........",
                "...........iiiiiiiiii...........",
                "..........bbbiiiiiibbb..........",
                "..........bbb......bbb..........",
                "..........bbb......bbb..........",
                "................................",
                "................................",
            },
            new[]
            {
                "................................",
                ".....................Lo.........",
                ".....................lL.........",
                "...................ww...........",
                "..................w.............",
                ".................w..............",
                "..............iiii..............",
                "..............iixi..............",
                ".............iiiiii.............",
                "........x..iiiiiiiiii...x.......",
                ".........xiiiiiiiiiiiiix........",
                "........iiiiiiiiiiiiiiii........",
                "........iiiiiiiiiiiiiiii........",
                ".......iiiiiiiiiiiiiiiiii.......",
                ".......iigiiiiiiiiiiiiigi.......",
                "......iiiiiniiiiiiiiniiiii......",
                "......iiiiiiRRiiiiRRiiiiii......",
                "......iiiiiiRRiiiiRRiiiiii......",
                "....xxiiiiiiiiiiiiiiiiiiiixx....",
                "......iiiiiiiiiiiiiiiiiiii......",
                "......iigiiiiiiiiiiiiiiigi......",
                ".......iiiiiiinnnniiiiiii.......",
                ".......iiiiiiixxxxiiiiiii.......",
                "........iiiiiiiiiiiiiiii........",
                "........iiiiiiiiiiiiiiii........",
                ".........igiiiiiiiiiiig.........",
                "...........iiiiiiiiii...........",
                "..........bbbiiiiiibbb..........",
                "..........bbb......bbb..........",
                "..........bbb......bbb..........",
                "................................",
                "................................",
            },
        };

        /// <summary>町の施設のアイコン（16ドット、陰影つき）。0 倉庫番・1 商人・2 師匠・3 荷運び・4 鍛冶屋。</summary>
        public static Sprite TownIcon(int index)
        {
            index = Mathf.Clamp(index, 0, TownIconRows.Length - 1);
            return Shaded("Town16_" + index, TownIconRows[index]);
        }



        /// <summary>加護のアイコン（16ドット、陰影つき。Boons の表と同じ順）。</summary>
        public static Sprite BoonIcon(int index)
        {
            index = Mathf.Clamp(index, 0, BoonIconRows.Length - 1);
            return Shaded("Boon16_" + index, BoonIconRows[index]);
        }



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

        /// <summary>
        /// 区画ごとの壁（64 ドット幅、側面の高さ 20 ドット）。上面は笠石（縁が光る）、側面はずらして積んだレンガ。
        /// 足元のひし形の中心が原点。
        /// </summary>
        public static Sprite IsoWallFor(ZoneSpec z)
        {
            string key = $"Wall64_{z.Name}";
            if (cache.TryGetValue(key, out var cached)) return cached;
            const int w = 64, side = 20, h = 32 + side;
            var px = new Color32[w * h];
            var clear = new Color32(0, 0, 0, 0);
            var edge = Mix(z.WallRight, new Color32(0, 0, 0, 255), 0.55f);
            float cy = 15.5f + side;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs(x - 31.5f) / 32f;
                    float dTop = dx + Mathf.Abs(y - cy) / 16f;
                    float lowerEdge = cy - 16f * (1f - dx);
                    Color32 c = clear;
                    if (dTop <= 1f)
                    {
                        // 上面：笠石。外側の縁は暗く、その内側の上の縁は光る
                        float n = TileNoise(x / 4f, y / 2f, 61);
                        c = Shade(z.WallTop, (int)((n - 0.5f) * 14f));
                        if (dTop > 0.93f) c = edge;
                        else if (dTop > 0.82f) c = y > cy ? Shade(z.WallTop, 22) : Shade(z.WallTop, -12);
                        else if (Hash(x, y + 5) % 71 == 0) c = Shade(c, -16);
                    }
                    else if (y < lowerEdge && y >= lowerEdge - side)
                    {
                        bool left = x < 32;
                        var face = left ? z.WallLeft : z.WallRight;
                        float depth = lowerEdge - y;                // 上の縁からの深さ（0〜side）
                        int row = (int)(depth / 5f);
                        float inRow = depth - row * 5f;
                        int along = left ? x : 63 - x;
                        bool vjoint = (along + row * 5) % 12 == 0;
                        bool hjoint = inRow < 1f;
                        int brick = (along + row * 5) / 12 + row * 17 + (left ? 0 : 400);
                        c = Shade(face, (int)(Hash(brick, 9) % 13) - 6 + (int)((TileNoise(x / 3f, y / 2f, 63) - 0.5f) * 10f));
                        if (hjoint || vjoint) c = Mix(face, edge, 0.6f);
                        else if (inRow < 2f) c = Shade(c, 12);      // レンガの上の縁は光る
                        if (depth < 1f) c = Shade(z.WallTop, -6);   // 上面との境目
                        if (depth > side - 1f) c = edge;            // 足元
                        if (left && x == 31) c = Shade(face, 18);   // 角は少し明るい
                    }
                    px[y * w + x] = c;
                }
            }
            var tex = MakeTexture(key, w, h, px, TextureWrapMode.Clamp);
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, (cy - side) / h), 32, 0, SpriteMeshType.FullRect);
            sprite.name = key;
            cache[key] = sprite;
            return sprite;
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
        public static Sprite Altar => Shaded("Altar32", AltarRows, null, null, 32);


        /// <summary>下り階段（64×32 のひし形。奥の暗がりへ段が下りていき、縁は金色）。</summary>
        public static Sprite Stairs
        {
            get
            {
                const string key = "Stairs64";
                if (cache.TryGetValue(key, out var cached)) return cached;
                const int w = 64, h = 32;
                var px = new Color32[w * h];
                var stone = new Color32(96, 82, 66, 255);
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
                            // a：左上（0）から右下（2）へ。段は a に沿って下りていき、奥ほど暗い
                            float a = u + v + 1f;
                            int step = (int)(a * 3.5f);
                            float f = a * 3.5f - step;
                            float dark = Mathf.Clamp01(a / 2f);
                            var c0 = Shade(stone, -(int)(dark * 70f));
                            if (f < 0.22f) c0 = Shade(c0, 22);        // 段の縁（光）
                            else if (f > 0.8f) c0 = Shade(c0, -22);   // 次の段との影
                            c0 = Shade(c0, (int)((TileNoise(x / 3f, sy / 2f, 81) - 0.5f) * 8f));
                            c = c0;
                            if (d > 0.9f) c = d > 0.96f ? new Color32(120, 84, 30, 255) : new Color32(255, 205, 90, 255);
                        }
                        px[ty * w + x] = c;
                    }
                }
                return Store(key, MakeTexture(key, w, h, px, TextureWrapMode.Clamp), 32);
            }
        }

        // ---- 床と壁（64 ドット幅。キャラと同じ細かさ：1 マス = 32 ドット）----

        static float TileNoise(float x, float y, int s)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float R(int a, int b) => (Hash(a + s * 131, b + s * 71) & 0xffff) / 65535f;
            float top = Mathf.Lerp(R(xi, yi), R(xi + 1, yi), fx), bottom = Mathf.Lerp(R(xi, yi + 1), R(xi + 1, yi + 1), fx);
            return Mathf.Lerp(top, bottom, fy);
        }

        static Color32 Shade(Color32 c, int d) => new Color32(
            (byte)Mathf.Clamp(c.r + d, 0, 255), (byte)Mathf.Clamp(c.g + d, 0, 255), (byte)Mathf.Clamp(c.b + d, 0, 255), 255);

        /// <summary>
        /// 床（64×32 のひし形）。4 枚の敷石に分かれ、目地・面取りの光と影・石ごとの色むら・細かな凹凸がある。
        /// その上に区画ごとの模様（苔・溶岩のひび・氷のつや・金の象嵌・星）を重ねる。
        /// </summary>
        static Sprite IsoTile(string key, Color32 stone, Color32 edge, Color32 accent, FloorPattern pattern, int salt)
        {
            if (cache.TryGetValue(key, out var cached)) return cached;
            const int w = 64, h = 32;
            var px = new Color32[w * h];
            var clear = new Color32(0, 0, 0, 0);
            var mortar = Mix(stone, edge, 0.55f);
            for (int ty = 0; ty < h; ty++)
            {
                int sy = h - 1 - ty; // 上から数えた行
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f - 32f) / 32f, v = (sy + 0.5f - 16f) / 16f;
                    float d = Mathf.Abs(u) + Mathf.Abs(v);
                    if (d > 1f) { px[ty * w + x] = clear; continue; }
                    // ひし形の中の座標（0〜2）：a は右下へ、b は左下へ
                    float a = (u + v + 1f), b = (v - u + 1f);
                    int ia = Mathf.Clamp((int)a, 0, 1), ib = Mathf.Clamp((int)b, 0, 1);
                    float fa = a - ia, fb = b - ib;
                    int stoneId = ia * 2 + ib + salt * 4;
                    int vary = (int)(Hash(stoneId, salt * 7 + 3) % 11) - 5;
                    float n = TileNoise(x / 3f, sy / 2f, salt + 5);
                    var c = Shade(stone, vary + (int)((n - 0.5f) * 6f));
                    bool seam = d > 0.95f;
                    bool joint = fa < 0.045f || fb < 0.045f || fa > 0.97f || fb > 0.97f;
                    if (seam || joint) c = mortar;
                    else
                    {
                        if (fa < 0.12f || fb < 0.12f) c = Shade(c, 9);        // 上側の面取り（光）
                        else if (fa > 0.88f || fb > 0.88f) c = Shade(c, -9);  // 下側の面取り（影）
                        if ((Hash(x + salt * 13, sy) % 97) == 0) c = Shade(c, -18); // 小さなくぼみ
                    }

                    switch (pattern)
                    {
                        case FloorPattern.Stone: // ところどころのひび
                            if (!seam && Mathf.Abs(TileNoise(x / 7f, sy / 4f, salt + 11) - 0.5f) < 0.018f) c = Shade(c, -26);
                            break;
                        case FloorPattern.Moss: // 目地からはみ出す苔
                        {
                            float m = TileNoise(x / 6f, sy / 3f, salt + 21);
                            if ((joint && m > 0.55f) || m > 0.8f) c = Mix(c, Shade(accent, (int)((TileNoise(x / 2f, sy, salt + 22) - 0.5f) * 24f)), 0.75f);
                            break;
                        }
                        case FloorPattern.Lava: // 目地の一部が赤く光る
                            if (joint && TileNoise(x / 5f, sy / 3f, salt + 31) > 0.64f) c = Mix(accent, new Color32(255, 230, 150, 255), 0.2f);
                            else if ((joint || seam) && TileNoise(x / 5f, sy / 3f, salt + 31) > 0.5f) c = Mix(mortar, accent, 0.45f);
                            break;
                        case FloorPattern.Ice: // 斜めのつやと、きらめき
                            if (!joint && !seam && Mathf.Abs(fa - fb * 0.3f - 0.45f) < 0.05f) c = Mix(c, accent, 0.4f);
                            if (Hash(x + salt, sy * 3) % 89 == 0) c = Mix(c, accent, 0.7f);
                            break;
                        case FloorPattern.Gold: // 目地に金の象嵌
                            if (joint) c = Mix(accent, mortar, 0.3f);
                            break;
                        case FloorPattern.Void: // 星のような点と、ぼんやりした色
                            if (Hash(x + salt * 5, sy) % 71 == 0) c = accent;
                            else if (TileNoise(x / 9f, sy / 5f, salt + 41) > 0.7f) c = Mix(c, accent, 0.12f);
                            break;
                    }
                    px[ty * w + x] = c;
                }
            }
            return Store(key, MakeTexture(key, w, h, px, TextureWrapMode.Clamp), 32);
        }

        static Color32 Mix(Color32 a, Color32 b, float t) => new Color32(
            (byte)(a.r + (b.r - a.r) * t), (byte)(a.g + (b.g - a.g) * t), (byte)(a.b + (b.b - a.b) * t), 255);




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
