using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// M1 用の仮のドット絵。画像ファイルを使わず、文字の並びからスプライトを作る。
    /// 本番の絵ができたら差し替える。
    /// </summary>
    public static partial class PixelArt
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

        static Sprite PlayerFor(ClassType type, CharacterData ch) => PlayerFrame(type, ch, 0);

        /// <summary>歩きなどのコマの数（0 立ち／1 左足を上げる／2 立ち／3 右足を上げる／4 息を吸う）。</summary>
        public const int PlayerFrameCount = 5;

        /// <summary>
        /// プレイヤーの絵の1コマ。どの職業の絵からも、足元（いちばん下の数段）を左右に分けて持ち上げ、
        /// 体を1ドット弾ませて歩きのコマを作る。4 は立っているときの呼吸（上半身が1ドット沈む）。
        /// </summary>
        public static Sprite PlayerFrame(ClassType type, CharacterData ch, int frame)
        {
            frame = Mathf.Clamp(frame, 0, PlayerFrameCount - 1);
            string suffix = frame == 0 ? "" : "_f" + frame;
            // 特別な職「彩灯者」：専用の絵に、桃色の光の縁
            if (type == ClassType.Vividbearer)
                return Shaded("PlayerHd_Vividbearer" + suffix, AnimRows(VividbearerHdRows, frame), VividOver, SpecialClass.AuraColor);
            // 上位職なら、その見た目（色を変えて、目印の小物を足す）
            if (ch != null && ch.Advanced >= 0)
            {
                int index = Mathf.Clamp(ch.Advanced, 0, 1);
                int k = (int)type * 2 + index;
                string key = $"PlayerHd_{type}_Adv{index}" + (ch.Third ? "_3rd" : "") + suffix;
                if (cache.TryGetValue(key, out var cached)) return cached;
                if (ch.Third) return Shaded(key, AnimRows(ThirdRows[k], frame), ThirdOver[k], ThirdOutline[k]);
                return Shaded(key, AnimRows(AdvRows[k], frame), AdvOver[k]);
            }
            switch (type)
            {
                case ClassType.Shadeblade: return Shaded("PlayerHd_Shadeblade" + suffix, AnimRows(ShadebladeHdRows, frame));
                case ClassType.Warden: return Shaded("PlayerHd_Warden" + suffix, AnimRows(WardenHdRows, frame));
                default: return Shaded("PlayerHd_Lampwright" + suffix, AnimRows(LampwrightHdRows, frame));
            }
        }

        /// <summary>絵の行から、歩き・呼吸のコマを作る（足元の4段を左右に分けて扱う）。</summary>
        static string[] AnimRows(string[] src, int frame)
        {
            if (frame == 0 || frame == 2) return src;
            int h = src.Length, w = src[0].Length;
            char At(int x, int y) => x >= 0 && y >= 0 && y < h && x < src[y].Length ? src[y][x] : '.';
            int bottom = -1;
            for (int y = h - 1; y >= 0 && bottom < 0; y--) for (int x = 0; x < w; x++) if (At(x, y) != '.') { bottom = y; break; }
            if (bottom < 4) return src;
            int feetTop = bottom - 3;
            // 足は靴（b）のある列だけ。武器や杖が足元まで伸びていても、足と一緒に切り離さない（武器は体と一緒に動く）
            int bootMin = w, bootMax = -1;
            for (int y = feetTop - 1; y <= bottom; y++) for (int x = 0; x < w; x++) if (At(x, y) == 'b') { bootMin = Mathf.Min(bootMin, x); bootMax = Mathf.Max(bootMax, x); }
            if (bootMax < 0) { bootMin = 0; bootMax = w - 1; }
            // 足元の真ん中（左右の足の境目）
            float sum = 0f; int cnt = 0;
            for (int y = feetTop; y <= bottom; y++) for (int x = bootMin; x <= bootMax; x++) if (At(x, y) != '.') { sum += x; cnt++; }
            float mid = cnt > 0 ? sum / cnt : w / 2f;
            var g = new char[h][];
            for (int y = 0; y < h; y++) g[y] = new string('.', w).ToCharArray();
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    char c = At(x, y);
                    if (c == '.') continue;
                    int ny = y;
                    bool feet = y >= feetTop && x >= bootMin - 1 && x <= bootMax + 1;
                    if (frame == 4) ny = feet || y >= feetTop ? y : Mathf.Min(feetTop - 1, y + 1); // 呼吸：上半身が1ドット沈む（足元の高さの武器は動かさない）
                    else
                    {
                        bool left = x < mid;
                        if (!feet) ny = y - 1;                                          // 足を上げたコマは、体が1ドット弾む
                        else if ((frame == 1 && left) || (frame == 3 && !left)) ny = y - 2; // 上げた足
                    }
                    if (ny >= 0 && ny < h && g[ny][x] == '.') g[ny][x] = c;
                }
            }
            var outRows = new string[h];
            for (int y = 0; y < h; y++) outRows[y] = new string(g[y]);
            return outRows;
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
            { 'W', new Color32(150, 255, 200, 255) }, // 癒やしの泉の光る水
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

        /// <summary>3次職の見た目（町の女神像の画面で、3次職になる前に見せる）。</summary>
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
                ".........cdLddLLdLc....gwwg.....",
                ".........cdhhhhhhdc....gwwg.....",
                ".........cdhshhshdc.....ww......",
                ".........cdsessesdc.....ww......",
                ".........cdssssssdc.....ww......",
                "..........cdssrssd......ww......",
                "..........ccdsssdcc.....ww......",
                ".........cccFFFccccc....ww......",
                "........cccccFccccccc...ww......",
                ".......dcccccFcccccddsssww......",
                ".......dccccFFFcccddssssww......",
                ".......ccccbbgbbcccsss..ww......",
                "........cccbbLbbccccs...ww......",
                "........ccccFcFccccc....ww......",
                ".......cccccFccFccccc...ww......",
                ".......ccccFccccFcccc...ww......",
                "......cccccFccccFccccc..ww......",
                "......ccccFcccccFcccc...ww......",
                "......ccLFcccLccFcLcc...ww......",
                ".....cLcLFcLcLccLFLccc..ww......",
                ".....LLdLLLdLLLdLLLdL...ww......",
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
                "......gggcTccFccTgggc...ww......",
                ".......cccTcclccTccccsssww......",
                ".......cccTclLlcTcccssssww......",
                ".......cccTbbgbbTccsss..ww......",
                "........ccTbbgbbTcccs...ww......",
                "........ccTcFcFcTccc....ww......",
                ".......cccTcFccFTcccc...ww......",
                ".......cccTFccccTcccc...ww......",
                "......ccccTFccccTccccc..ww......",
                "......ccccTcccccTcccc...ww......",
                "......cccFTcccccTcccc...ww......",
                ".....ccccFTcccccTFcccc..ww......",
                ".....cdcFcgcdcdcgFdcd...ww......",
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
                "..........rrTrrTrrr.............",
                ".....rTrrTrrrrrrrrr.............",
                "...rTrr...innnnnnnni............",
                "..rT.....nnnbbbbbbnnn...........",
                ".......s.nnnbbbbbbnnnss.........",
                ".......s.nnnbbggbbnnnss.........",
                "......gg.nnnggggggnnngg.........",
                "......pmbnnnnnnnnnnnn.p.........",
                ".....p.bnnFnnn..nnnFnnp.........",
                ".....p..nnFnnn..nnnnFn.p........",
                "....p..nnFnnnn..nnnnFnnp........",
                ".......nnFnnnn..nnnnnFn.........",
                "......nnFTnnT....nTnnFnn........",
                "...........bbb..bbb.............",
                "...........bnb..bnb.............",
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
                "..........rrTrrTrrr.............",
                ".....rTrrTrrrrrrrrr.............",
                "...rTrr...innnnnnnni............",
                "..rT.....nnnbbbbbbnnn.g.........",
                ".........nnnbbbbbbnnnss.........",
                ".........nnnbbggbbnnnss.........",
                ".......m.nnnggggggnnggggg.......",
                ".......mbnnnnnnnnnnnnRRR........",
                ".......bnnFnnn..nnnFnRRR........",
                "........nnFnnn..nnnnFnRRR.......",
                ".......nnFnnnn..nnnnFnRRR.......",
                ".......nnFnnnn..nnnnnFnRRR......",
                "......nnFTnnT....nTnnFnRRR......",
                "...........bbb..bbb.....RRR.....",
                "...........bnb..bnb.....RRR.....",
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
                ".......gggggxmmmmmmggggg........",
                "......mmxmmmmmmlxmmmmmmmm.......",
                "......xmmmmmmmlolxmmmmmmm.......",
                "........ssmmmmmlmmmmmss.........",
                "........ssbbbbbgbbbbbs..........",
                "..........vvvvvvvvFFF.m.........",
                ".........vvvvFFgvvvvFF.m........",
                ".........vFvvvgggvvvFv..m.......",
                "........vvvvvFFgvvvvvFF.m.......",
                "........vvFvvvFgvvvFFvmmmmm.....",
                ".......vvvFvvvvvvFFFvmLLLLm.....",
                ".......vvvvvvvvvvFFFFmLooLm.....",
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
                "guuuuuggggggxmmmmmmggggg........",
                "guuuuugmxmmmmmmgxmmmmmmmm.......",
                "guuuuugmmmmmmmgggxmmmmmmm.......",
                "guuguug.ssmmmmmgmmmmmss.........",
                "gugggug.ssbbbbbgbbbbbs..........",
                "guuguug...vvvvvvvvFFF.m.........",
                "guuguug..vvvvFFgvvvvFF.m........",
                "guuuuug..vFvvvgggvvvFv..m.......",
                "guuuuug.vvvvvFFgvvvvvFF.m.......",
                "guuuuug.vvFvvvFgvvvFFvmmmmm.....",
                "guuuuugvvvFvvvvvvFFFvmLLLLm.....",
                "guuuuugvvvvvvvvvvFFFFmLooLm.....",
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
            new Dictionary<char, Color32[]> { { 'c', Ramp(220, 70, 70, 176, 40, 48, 128, 26, 34, 88, 16, 24) }, { 'd', Ramp(255, 214, 110, 214, 160, 56, 160, 110, 30, 110, 72, 18) }, { 'F', Ramp(140, 34, 40, 110, 24, 30, 84, 16, 22, 60, 10, 16) } }, // Flarecaller
            new Dictionary<char, Color32[]> { { 'c', Ramp(250, 248, 236, 222, 216, 196, 178, 170, 150, 130, 122, 104) }, { 'd', Ramp(255, 230, 130, 226, 186, 72, 170, 130, 40, 120, 88, 24) }, { 'F', Ramp(214, 206, 182, 190, 180, 154, 160, 150, 124, 126, 116, 94) }, { 'T', Ramp(110, 140, 220, 70, 100, 190, 48, 70, 140, 30, 46, 96) } }, // Beaconkeeper
            new Dictionary<char, Color32[]> { { 'n', Ramp(110, 80, 150, 72, 50, 108, 50, 34, 78, 32, 22, 52) }, { 'r', Ramp(190, 120, 255, 150, 80, 220, 106, 52, 170, 72, 34, 120) }, { 'F', Ramp(56, 38, 86, 42, 28, 66, 30, 20, 50, 20, 12, 34) }, { 'T', Ramp(130, 80, 190, 104, 60, 160, 80, 44, 124, 56, 30, 90) } }, // Nightreaver
            new Dictionary<char, Color32[]> { { 'n', Ramp(120, 52, 62, 86, 34, 44, 60, 22, 30, 40, 14, 20) }, { 'F', Ramp(70, 26, 34, 56, 20, 28, 42, 14, 20, 28, 8, 14) }, { 'T', Ramp(150, 30, 40, 120, 20, 30, 90, 12, 20, 60, 8, 14) } }, // Bloodedge
            new Dictionary<char, Color32[]> { { 'v', Ramp(255, 244, 200, 236, 210, 120, 196, 160, 70, 140, 108, 40) }, { 'm', Ramp(255, 250, 230, 222, 214, 186, 170, 160, 130, 118, 110, 86) }, { 'F', Ramp(220, 190, 100, 200, 166, 80, 168, 134, 56, 124, 96, 36) } }, // Sunbinder
            new Dictionary<char, Color32[]> { { 'm', Ramp(170, 190, 230, 110, 130, 176, 74, 90, 130, 46, 56, 86) }, { 'v', Ramp(70, 96, 170, 44, 64, 130, 30, 44, 94, 20, 28, 64) }, { 'F', Ramp(40, 56, 116, 32, 46, 98, 24, 34, 76, 16, 22, 54) } }, // Bastion
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
                ".......dcccccLcccccddsssww......",
                ".......dccccLLLcccddssssww......",
                ".......ccccbbgbbcccsss..ww......",
                "........cccbbLbbccccs...ww......",
                "........ccccLcLccccc....ww......",
                ".L.....cccccLccLccccc...ww......",
                ".......ccccLccccLcccc...ww......",
                "......cccccLccccLccccc..ww......",
                "......ccccLcccccLcccc...ww......",
                "......ccLLcccLccLcLcc...ww..L...",
                ".....cLcLLcLcLccLLLccc..ww......",
                ".....LLdLLLdLLLdLLLdL...ww......",
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
                "......gggcTccFccTgggc...ww......",
                ".......cccTcclccTccccsssww......",
                ".......ccoTclLlcTcccssssww......",
                ".......cccTbbgbbTccsss..ww......",
                "........ccTbbgbbToccs...ww......",
                "........ccTcFcocTccc....ww......",
                ".......cccTcFccFTcccc...ww......",
                ".......cccTFccccTcccc...ww......",
                "......ccccoFccccTccccc..ww......",
                "......ccccTcccccTcccc...ww......",
                "......cccFTcccccocccc...ww......",
                ".....cccoFTcccccTFcccc..ww......",
                ".....cdcFcgcdcdcgFdcd...ww.o....",
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
                "..........rrTrrTrrr.............",
                ".....rTrrTrrrrrrrrr.............",
                "...rTrr...innnnnnnni............",
                "..rT.....nnnbbbbbbnnn.....o.....",
                ".......s.nnnbbbbbbnnnss.........",
                ".......s.nnnbbggbbnnnss.........",
                "......gg.nnnggggggnnngg.........",
                "......pmbnnnnnnnnnnnn.p.........",
                ".....p.bnnFnnn..nnnFnnp.........",
                ".....p..nnFnnn..nnnnFn.p........",
                "....p..nnFnnnn..nnnnFnnp........",
                ".......nnFnnnn..nnnnnFn.........",
                "......nnFTnnT....nTnnFnn........",
                "...........bbb..bbb.............",
                "...........bnb..bnb.............",
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
                "..........rrTrrTrrr.............",
                "....ffTrrTrrrrrrrrr.............",
                "...fffr...innnnnnnni............",
                "..rfff...nnnbbbbbbnnn.g.........",
                "....ff...nnnbbbbbbnnnss.........",
                ".........nnnbbggbbnnnss.........",
                ".......m.nnnggggggnnggggg.......",
                ".......mbnnnnnnnnnnnnRRR........",
                ".......bnnFnnn..nnnFnRRR........",
                "........nnFnnn..nnnnFnRRR..R....",
                ".......nnFnnnn..nnnnFnRRR.......",
                ".......nnFnnnn..nnnnnFnRRR......",
                "......nnFTnnT....nTnnFnRRR......",
                "...........bbb..bbb.....RRR.....",
                "...........bnb..bnb.....RRR.....",
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
                ".......gggggxmmmmmmggggg........",
                "......mmxmmmmmmlxmmmmmmmm.......",
                "......xmmmmmmmlolxmmmmmmm.......",
                "........ssmmmmmlmmmmmss.........",
                "........ssbbbbbgbbbbbs..........",
                "..........vvvvvvvvFFF.m.........",
                ".........vvvvFFgvvvvFF.m........",
                ".........vFvvvgggvvvFv..m.......",
                "........vvvvvFFgvvvvvFF.m.......",
                "........vvFvvvFgvvvFFvmmmmm.....",
                ".......vvvFvvvvvvFFFvmLLLLm.....",
                ".......vvvvvvvvvvFFFFmLooLm.....",
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
                "guuuuuggggggxmmmmmmggggg........",
                "guuuuugmxmmmmmmgxmmmmmmmm.......",
                "guuuuugmmmmmmmgggxmmmmmmm.......",
                "ouuouuo.ssmmmmmgmmmmmss.........",
                "ouooouo.ssbbbbbgbbbbbs..........",
                "ouuouuo...vvvvvvvvFFF.m.........",
                "ouuouuo..vvvvFFgvvvvFF.m........",
                "guuuuug..vFvvvgggvvvFv..m.......",
                "guuuuug.vvvvvFFgvvvvvFF.m.......",
                "guuuuug.vvFvvvFgvvvFFvmmmmm.....",
                "guuuuugvvvFvvvvvvFFFvmLLLLm.....",
                "guuuuugvvvvvvvvvvFFFFmLooLm.....",
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
            new Dictionary<char, Color32[]> { { 'c', Ramp(150, 40, 46, 110, 24, 32, 76, 14, 22, 50, 8, 14) }, { 'd', Ramp(255, 214, 110, 214, 160, 56, 160, 110, 30, 110, 72, 18) }, { 'F', Ramp(90, 18, 24, 70, 12, 18, 52, 8, 12, 36, 4, 8) } }, // Pyrelord
            new Dictionary<char, Color32[]> { { 'c', Ramp(70, 86, 170, 44, 56, 130, 30, 38, 94, 18, 24, 64) }, { 'd', Ramp(255, 230, 130, 226, 186, 72, 170, 130, 40, 120, 88, 24) }, { 'F', Ramp(40, 50, 116, 30, 38, 96, 22, 28, 74, 14, 18, 52) }, { 'T', Ramp(240, 244, 255, 200, 210, 235, 150, 160, 196, 100, 110, 150) } }, // Starwarden
            new Dictionary<char, Color32[]> { { 'n', Ramp(70, 54, 100, 44, 32, 70, 30, 20, 50, 18, 12, 32) }, { 'r', Ramp(190, 120, 255, 150, 80, 220, 106, 52, 170, 72, 34, 120) }, { 'F', Ramp(36, 26, 58, 28, 20, 46, 20, 14, 34, 12, 8, 24) }, { 'T', Ramp(130, 80, 190, 104, 60, 160, 80, 44, 124, 56, 30, 90) } }, // Eclipse
            new Dictionary<char, Color32[]> { { 'n', Ramp(120, 52, 62, 86, 34, 44, 60, 22, 30, 40, 14, 20) }, { 'F', Ramp(70, 26, 34, 56, 20, 28, 42, 14, 20, 28, 8, 14) }, { 'T', Ramp(150, 30, 40, 120, 20, 30, 90, 12, 20, 60, 8, 14) } }, // Bloodsovereign
            new Dictionary<char, Color32[]> { { 'v', Ramp(255, 244, 200, 236, 210, 120, 196, 160, 70, 140, 108, 40) }, { 'm', Ramp(255, 250, 230, 222, 214, 186, 170, 160, 130, 118, 110, 86) }, { 'F', Ramp(220, 190, 100, 200, 166, 80, 168, 134, 56, 124, 96, 36) } }, // Dawnbringer
            new Dictionary<char, Color32[]> { { 'm', Ramp(170, 190, 230, 110, 130, 176, 74, 90, 130, 46, 56, 86) }, { 'v', Ramp(70, 96, 170, 44, 64, 130, 30, 44, 94, 20, 28, 64) }, { 'F', Ramp(40, 56, 116, 32, 46, 98, 24, 34, 76, 16, 22, 54) } }, // Aegis
        };

        /// <summary>彩灯者の色：外套の裏は淡い紫、髪は銀。</summary>
        static readonly Dictionary<char, Color32[]> VividOver = new Dictionary<char, Color32[]>
        {
            { 'q', Ramp(232, 234, 248, 190, 194, 216, 142, 148, 178, 96, 100, 134) },   // 銀の髪
            { 'x', Ramp(255, 255, 255, 236, 238, 246, 200, 204, 222, 150, 156, 182) },  // 白い上着
            { 'u', Ramp(232, 240, 255, 196, 214, 246, 150, 170, 220, 104, 120, 176) },  // 淡い青の肩掛け
            { 'v', Ramp(214, 208, 244, 176, 168, 220, 134, 124, 184, 92, 84, 140) },   // 長い外套（淡い紫を帯びた白。上着と見分けがつくよう少し濃く）
            { 'T', Ramp(240, 242, 250, 210, 214, 228, 168, 172, 192, 120, 124, 146) },  // 白いズボン
            { 'b', Ramp(255, 255, 255, 228, 230, 240, 186, 190, 206, 136, 140, 160) },  // 白い長靴
            { 'K', Ramp(236, 170, 170, 224, 150, 150, 206, 132, 132, 180, 112, 112) },  // 口（淡い紅）
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
        static Sprite Shaded(string key, string[] rows, Dictionary<char, Color32[]> overrides = null, Color32? auraColor = null, int ppu = 0, bool strongAura = false)
        {
            if (cache.TryGetValue(key, out var cached)) return cached;
            int h = rows.Length, w = rows[0].Length;
            var px = ShadePixels(rows, overrides, auraColor, strongAura);
            // ppu を指定しなければ、絵の幅をちょうど1マスに収める（キャラ・敵は 32 ドットで 1 マス）
            return Store(key, MakeTexture(key, w, h, px, TextureWrapMode.Clamp), ppu > 0 ? ppu : w);
        }

        /// <summary>
        /// アイコンのバッジ（スキル・状態異常と同じ形：暗い下地に色の縁）に、陰影付きの記号（16 ドット）を収める。20 ドット四方。
        /// 加護・固有効果・絵のないスキルのアイコンをこれでそろえる。
        /// </summary>
        static Sprite Badge(string key, string[] glyph, Dictionary<char, Color32[]> overrides, Color32 bg, Color32 edge)
        {
            if (cache.TryGetValue(key, out var cached)) return cached;
            // 32 ドットの記号（16 ドットの絵を Scale2x でなめらかに倍にして、陰影を付け直す）を、40 ドットのバッジに収める
            const int n = 40;
            var big = Scale2x(glyph);
            int gh = big.Length, gw = big[0].Length;
            var g = ShadePixels(big, overrides, null, false);
            var px = new Color32[n * n];
            var edgeLight = Mix(edge, new Color32(255, 255, 255, 255), 0.3f);
            var edgeDark = Mix(edge, new Color32(0, 0, 0, 255), 0.35f);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int ring = Mathf.Min(Mathf.Min(x, y), Mathf.Min(n - 1 - x, n - 1 - y));
                    // 縁は2ドット：外側は色そのまま、内側は左上を明るく・右下を暗く（ふくらんで見えるように）
                    px[y * n + x] = ring == 0 ? edge : ring == 1 ? (x + (n - 1 - y) < n ? edgeDark : edgeLight) : bg;
                }
            int ox = (n - gw) / 2, oy = (n - gh) / 2;
            for (int y = 0; y < gh; y++)
                for (int x = 0; x < gw; x++)
                {
                    var c = g[y * gw + x];
                    int tx = ox + x, ty = oy + y;
                    if (c.a == 0 || tx <= 1 || ty <= 1 || tx >= n - 2 || ty >= n - 2) continue;
                    px[ty * n + tx] = c;
                }
            return Store(key, MakeTexture(key, n, n, px, TextureWrapMode.Clamp), n);
        }

        /// <summary>
        /// 16 ドットの平塗りのバッジ（スキル・薬・状態異常。縁 E と下地 B は palette で決まる）を、32 ドットにする。
        /// Scale2x でなめらかに倍にし、記号には左上の明るい縁と右下の影を付け、縁は外側を色そのまま・内側を明暗で2ドットにする。
        /// </summary>
        static Sprite FlatBadge32(string key, string[] rows16, Dictionary<char, Color32> palette)
        {
            if (cache.TryGetValue(key, out var cached)) return cached;
            var big = Scale2x(rows16);
            int h = big.Length, w = big[0].Length;
            char At(int x, int y) => x >= 0 && y >= 0 && x < w && y < h ? big[y][x] : 'E';
            var clear = new Color32(0, 0, 0, 0);
            var white = new Color32(255, 255, 255, 255);
            var black = new Color32(0, 0, 0, 255);
            var px = new Color32[w * h];
            palette.TryGetValue('E', out var edge);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int ring = Mathf.Min(Mathf.Min(x, y), Mathf.Min(w - 1 - x, h - 1 - y));
                    char ch = big[y][x];
                    Color32 c;
                    if (ring == 0) c = edge;
                    else if (ring == 1) c = x + (h - 1 - y) < w ? Mix(edge, black, 0.35f) : Mix(edge, white, 0.3f);
                    else if (!palette.TryGetValue(ch, out c)) c = clear;
                    else if (ch != 'B' && ch != 'E')
                    {
                        // 記号の陰影：上か左が別の色なら明るく、下か右が別の色なら暗く
                        bool lit = At(x - 1, y) != ch || At(x, y - 1) != ch;
                        bool dark = At(x + 1, y) != ch || At(x, y + 1) != ch;
                        if (lit && !dark) c = Mix(c, white, 0.28f);
                        else if (dark && !lit) c = Mix(c, black, 0.22f);
                    }
                    px[(h - 1 - y) * w + x] = c;
                }
            }
            return Store(key, MakeTexture(key, w, h, px, TextureWrapMode.Clamp), w);
        }

        /// <summary>
        /// ドット絵の拡大（Scale2x）：1ドットを2×2にするとき、まわりの並びを見て斜めの線をなめらかにする。文字の並びのまま倍にする。
        /// </summary>
        static string[] Scale2x(string[] src)
        {
            int h = src.Length, w = src[0].Length;
            char P(int x, int y) => src[Mathf.Clamp(y, 0, h - 1)][Mathf.Clamp(x, 0, w - 1)];
            var o = new char[h * 2][];
            for (int i = 0; i < h * 2; i++) o[i] = new char[w * 2];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    char e = P(x, y), b = P(x, y - 1), d = P(x - 1, y), f = P(x + 1, y), hh = P(x, y + 1);
                    char e0 = e, e1 = e, e2 = e, e3 = e;
                    if (b != hh && d != f)
                    {
                        if (d == b) e0 = d;
                        if (b == f) e1 = f;
                        if (d == hh) e2 = d;
                        if (hh == f) e3 = f;
                    }
                    o[y * 2][x * 2] = e0; o[y * 2][x * 2 + 1] = e1;
                    o[y * 2 + 1][x * 2] = e2; o[y * 2 + 1][x * 2 + 1] = e3;
                }
            }
            var r = new string[h * 2];
            for (int i = 0; i < h * 2; i++) r[i] = new string(o[i]);
            return r;
        }

        /// <summary>Shaded の色付け（テクスチャの下の行から並んだ色）。</summary>
        static Color32[] ShadePixels(string[] rows, Dictionary<char, Color32[]> overrides, Color32? auraColor, bool strongAura)
        {
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
            // 3次職：暗い輪郭のさらに外側に、半透明の光の縁を1ドット。
            // strongAura（深淵の敵・ボス）は、透けない縁と、その外にもう1ドットの半透明の縁（同じ色の床でも浮き出すように）
            if (auraColor.HasValue)
            {
                int rings = strongAura ? 2 : 1;
                for (int ring = 0; ring < rings; ring++)
                {
                    var ac = auraColor.Value;
                    ac.a = strongAura ? (byte)(ring == 0 ? 255 : 120) : (byte)160;
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
            }
            return px;
        }

        /// <summary>彩灯者（専用の絵）：長い銀の髪に結晶の額飾り、虹色の目、金の縁取りと淡い宝石の付いた白い詰め襟の上着と重ねた肩掛け、左右に長く流れる白い外套、白いズボンと長靴。胸元に虹色の結晶を持つ。</summary>
        static readonly string[] VividbearerHdRows =
        {
            "................................",
            "............qqqqqqqq............",
            "..........qqqqHBBHqqqq..........",
            ".........qqqqHqqqqHqqqq.........",
            "........qqqqqqqqqqqqqqqq........",
            "........qqqqqsqqqqsqqqqq........",
            "........qqqqssssssssqqqq........",
            "........qqqseesssseesqqq........",
            "........qqqsRBsssspBsqqq........",
            "........qqqqssssssssqqqq........",
            "........qqqqqssKKssqqqqq........",
            "........qqqqq.ssss.qqqqq........",
            ".......vqqqquugxxguuqqqqv.......",
            "......vvquuuuugBBguuuuuqvv......",
            "......vvuuuguuxxxxuuguuuvv......",
            "......vvuuuuggxxxxgguuuuvv......",
            ".....vvvxxxxgxxpBxxgxxxxvvv.....",
            ".....vvvxxxsgxRBpjxgsxxxvvv.....",
            ".....vvvxxxssggggggssxxxvvv.....",
            ".....vvvxxxgxxxxxxxxgxxxvvv.....",
            ".....vvvxxgxBxxxxxxBxgxxvvv.....",
            ".....vvvxxgxxxxTTxxxxgxxvvv.....",
            "....vvvvxgxxxpTTTTpxxxgxvvvv....",
            "....vvvvxggggTTTTTTggggxvvvv....",
            "....vvv.....TTT..TTT.....vvv....",
            "...vvv......TTT..TTT......vvv...",
            "...vv.......TTT..TTT.......vv...",
            "...........bbbb..bbbb...........",
            "...........bbbb..bbbb...........",
            "................................",
            "................................",
            "................................",
        };

        static readonly string[] LampwrightHdRows =
        {
            "........................mm......",
            ".............cc.......mmmmmm....",
            ".............ccc......mlllLm....",
            "............cdcc......mlLoLm....",
            "...........ccdcccc....mlLLlm....",
            "..........ccdcccccc...mllllm....",
            ".........cggggggggc...mmmmmm....",
            ".........cdhhhhhhdc....gwwg.....",
            ".........cdhshhshdc.....ww......",
            ".........cdsessesdc.....ww......",
            ".........cdssssssdc.....ww......",
            "..........cdssrssd......ww......",
            "..........ccdsssdcc.....ww......",
            ".........cccdgdccccd....ww......",
            "........ccccdduccccdd...ww......",
            ".......ucccddducccddgsssww......",
            ".......ccccddddcccdgssssww......",
            ".......cccdbbgbbcccsss..gg......",
            "........cccbbbbbcccds...ww......",
            "........cbbcdcdccccd....ww......",
            ".......cbbbcdccdccccd...ww......",
            ".......cbbccdcccdcccd...ww......",
            "......ccccddcccddccccd..ww......",
            "......cccddccccddcccd...ww......",
            "......cccducccdddcccd...gg......",
            ".....cccdduccccdddcccd..ww......",
            ".....cccducccccdddccc...ww......",
            ".....ggggggggggggggggg..ww......",
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
            "...........nnnMnnnn.............",
            "..........nnhhhhhhnn............",
            "..........nhhshhshhn............",
            "..........nhsesseshn............",
            "..........nnsssssssn............",
            "...........nsssssss.............",
            "..........rrrrrrrrr.............",
            ".....rrfrrrrrrrrrrr.............",
            "...rrfr...innnnnnMMM............",
            "..rf.....nnnbmbbmbnnn.g.........",
            ".........nnnbbbbbbnnnss.........",
            ".........nnnbbggbbnnnss.........",
            ".......m.nnnggggggnnngggg.......",
            ".......mbinnnnnnnnMMM.mm........",
            ".......binnnMM..innnMMmm........",
            "........nnMnnn..nnnMnn.mm.......",
            ".......innnnMM..innnnMMmm.......",
            ".......nnnMnnn..nnnMMnn.mm......",
            "......innnnMM....innnnMMmm......",
            "...........bbb..bbb.......mm....",
            "...........nbn..nbn.......m.....",
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
            "...............rf...............",
            "..............rrf...............",
            "............mmmmmmm.............",
            "...........mmmmmmmmm............",
            "..........mmgggggggmm...........",
            "..........mmhhhhhhhmm...........",
            "..........mhssssssshm...........",
            "..........mhsesssesmm...........",
            "..........mmsssssssmm...........",
            "...........mmssrssmm............",
            "............mmmmmmm.............",
            ".......gggggxmmmmmmggggg........",
            "......mgxmmmmmmgxmmmmmmgm.......",
            "......xmmmmmmmgggxmmmmmmm.......",
            "........ssmmmmmgmmmmmss.........",
            "........ssbbbbbgbbbbbs..........",
            "..........vvvvvvvvfff.m.........",
            ".........vvvvffgvvvvff.m........",
            ".........vfvvvgggvvvfv..m.......",
            "........vvvvvffgvvvvvff.m.......",
            "........vvfvvvfgvvvffvmmmmm.....",
            ".......vvvfvvvvvvfffvmLLLLm.....",
            ".......vvvvvvvvvvffffmLooLm.....",
            "......gggggggggggggggmLLLLm.....",
            "..........mmmm..mmmm..mmmm......",
            "..........mgmm..mmgm...L........",
            ".........bbbbb..bbbbb...........",
            ".........bbbbb..bbbbb...........",
            "................................",
            "................................",
            "................................",
        };

        /// <summary>
        /// スキルごとのアイコン（16 ドットのバッジ、平塗り）。背景（B）と縁（E）は職業の色、中央にひとつの太い記号。
        /// 灯術師：焦げ茶に橙の縁、影刃：暗い紫に紫の縁、灯番：紺に金の縁。
        /// 絵がないスキル（まだ覚えていない上位職の欄など）は、効果の種類の形をスキルの色で塗る。
        /// </summary>
        public static Sprite SkillIcon(SkillSpec spec)
        {
            int i = System.Array.IndexOf(SkillArtNames, spec.Name);
            if (i < 0) return SkillIcon(spec.Effect, spec.Color);
            var pal = new Dictionary<char, Color32>(SkillBadgePalette)
            {
                { 'B', SkillBadgeClass[SkillArtClass[i]][0] },
                { 'E', SkillBadgeClass[SkillArtClass[i]][1] },
            };
            return FlatBadge32("SkillBadge32_" + i, SkillArtRows[i], pal);
        }

        /// <summary>回復薬・魔力の薬のアイコン（スキルと同じバッジ、平塗り）。赤い瓶に赤い縁／青い瓶に青い縁。</summary>
        public static Sprite PotionIcon(bool hp)
        {
            var pal = new Dictionary<char, Color32>(SkillBadgePalette)
            {
                { 'B', hp ? new Color32(52, 16, 20, 255) : new Color32(16, 24, 56, 255) },
                { 'E', hp ? new Color32(220, 64, 64, 255) : new Color32(80, 130, 240, 255) },
            };
            return FlatBadge32(hp ? "PotionHp32" : "PotionMp32", PotionRows[hp ? 0 : 1], pal);
        }

        static readonly string[][] PotionRows =
        {
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBddBBBBBBE",
                "EBBBBBwwwwBBBBBE",
                "EBBBBBwwwwBBBBBE",
                "EBBBBwwwwwBBBBBE",
                "EBBBwwwrwwwBBBBE",
                "EBBwwwrrrrwwBBBE",
                "EBBwrwrrrrrwBBBE",
                "EBBwrrrrrrrwBBBE",
                "EBBwrrrrrrrwBBBE",
                "EBBBwrrrrrwBBBBE",
                "EBBBBwrrrwBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBddBBBBBBE",
                "EBBBBBwwwwBBBBBE",
                "EBBBBBwwwwBBBBBE",
                "EBBBBwwwwwBBBBBE",
                "EBBBwwwbwwwBBBBE",
                "EBBwwwbbbbwwBBBE",
                "EBBwbwbbbbbwBBBE",
                "EBBwbbbbbbbwBBBE",
                "EBBwbbbbbbbwBBBE",
                "EBBBwbbbbbwBBBBE",
                "EBBBBwbbbwBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
        };

        /// <summary>弱体効果のアイコン（スキルと同じバッジ、平塗り。赤い縁）。DebuffKind の順。</summary>
        public static Sprite DebuffIcon(DebuffKind kind)
        {
            var pal = new Dictionary<char, Color32>(SkillBadgePalette)
            {
                { 'B', new Color32(44, 12, 16, 255) },
                { 'E', new Color32(214, 56, 56, 255) },
            };
            int i = Mathf.Clamp((int)kind, 0, DebuffRows.Length - 1);
            return FlatBadge32("Debuff32_" + i, DebuffRows[i], pal);
        }

        static readonly string[][] DebuffRows =
        {
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBByBBBBBBBByBBE",
                "EBByBBddddBByBBE",
                "EyywyyBBBByywyyE",
                "EBdyBBBBBBBBydBE",
                "EBdyBBBBBBBBydBE",
                "EBdBBBBByBBBBdBE",
                "EBddBBBByBBBddBE",
                "EBBBddyywyydBBBE",
                "EBBBBBddydBBBBBE",
                "EBBBBBBByBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBccBBccBBBBE",
                "EBBBBccBBccBBBBE",
                "EBBBBBcBBcBBBBBE",
                "EBBBBBBccBBBBBBE",
                "EBBcBBBwwBBBcBBE",
                "EBcccccwwcccccBE",
                "EBBBBBBccBBBBBBE",
                "EBBBBBcBBcBBBBBE",
                "EBBBBccBBccBBBBE",
                "EBBBBccBBccBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBswrrrBE",
                "EBBBBBBBswBrrrBE",
                "EBBBBBBswBBrrrBE",
                "EBBBBBBswBBrrrBE",
                "EBBBBBswBBBrrrBE",
                "EBBBBswBBrrrrrrE",
                "EBBBswBBBBrrrrrE",
                "EoBBswBBBBrrrrrE",
                "EBoswBBBBBBrrrBE",
                "EBsoBBBBBBBrrrBE",
                "EBBBoBBBBBBBrBBE",
                "EoBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EsssssssssBBBBBE",
                "EsssssssssBrrrBE",
                "EssssddsssBrrrBE",
                "EssssddsssBrrrBE",
                "EssssddsssBrrrBE",
                "EBsssddsssBrrrBE",
                "EBBssddssrrrrrrE",
                "EBBBsddsBBrrrrrE",
                "EBBBssssBBrrrrrE",
                "EBBBBssBBBBrrrBE",
                "EBBBBBBBBBBrrrBE",
                "EBBBBBBBBBBBrBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBrrE",
                "EBBBBBBBBBBBrrBE",
                "EBBBdddddddrrBBE",
                "EBBBdddkkdrrBBBE",
                "EBBdddkskrrddBBE",
                "EBdddkkkrrkdddBE",
                "EBdddkkrrkkdddBE",
                "EBBdddrrkkdddBBE",
                "EBBBdrrkkdddBBBE",
                "EBBBrrdddddBBBBE",
                "EBBrrBBBBBBBBBBE",
                "EBrrBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
        };

        static readonly string[] SkillArtNames = { "Flame Ring", "Fire Barrage", "Flare", "Meteor", "Ember Shield", "Inferno", "Starfall", "Beacon", "Cataclysm", "Constellation", "Whirlwind", "Shadow Dash", "Frenzy", "Crescent Wave", "Second Wind", "Thousand Cuts", "Eclipse Rush", "Crimson Spiral", "Umbral Storm", "Sanguine Tide", "Holy Pulse", "Mend", "Lantern Ward", "Kindle", "Flame Pillar", "Judgment", "Sunburst", "Iron Vigil", "Daybreak", "Unbreakable", "Prism Ring", "Spectrum Volley", "Vivid Step", "Rainbow Pillar", "Vivid Grace", "Prism Burst", "Prism Twin", "Rainbow Rain" };

        /// <summary>そのスキルの職業（0 灯術師・1 影刃・2 灯番）。背景と縁の色を決める。</summary>
        static readonly int[] SkillArtClass = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3 };

        /// <summary>職業ごとの背景と縁の色。</summary>
        static readonly Color32[][] SkillBadgeClass =
        {
            new[] { new Color32(60, 26, 18, 255), new Color32(236, 124, 44, 255) },
            new[] { new Color32(34, 22, 48, 255), new Color32(146, 100, 216, 255) },
            new[] { new Color32(22, 28, 54, 255), new Color32(232, 192, 84, 255) },
            new[] { new Color32(44, 20, 58, 255), new Color32(255, 110, 220, 255) }, // 彩灯者：深い紫に桃色の縁
        };

        /// <summary>記号の色（平塗り）。</summary>
        static readonly Dictionary<char, Color32> SkillBadgePalette = new Dictionary<char, Color32>
        {
            { 'w', new Color32(250, 248, 240, 255) },
            { 'y', new Color32(255, 214, 72, 255) },
            { 'o', new Color32(244, 124, 40, 255) },
            { 'r', new Color32(214, 52, 52, 255) },
            { 'p', new Color32(170, 110, 240, 255) },
            { 'b', new Color32(96, 150, 240, 255) },
            { 'c', new Color32(120, 220, 240, 255) },
            { 'g', new Color32(96, 210, 110, 255) },
            { 's', new Color32(186, 190, 204, 255) },
            { 'k', new Color32(16, 12, 20, 255) },
            { 'd', new Color32(80, 84, 100, 255) },
        };

        static readonly string[][] SkillArtRows =
        {
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBooyyooBBBBE",
                "EBBBoyyooyyoBBBE",
                "EBBoyoBBBBoyoBBE",
                "EBBoyBBBBBByoBBE",
                "EBByoBBBBBBoyBBE",
                "EBByoBBBBBBoyBBE",
                "EBBoyBBBBBByoBBE",
                "EBBoyoBBBBoyoBBE",
                "EBBBoyyooyyoBBBE",
                "EBBBBooyyooBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBooBBBE",
                "EBBoooBBBooooBBE",
                "EBooyooBBoyooBBE",
                "EBoooooBBooooBBE",
                "EBBoooBBBBooBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBooooBBBBBE",
                "EBBBBBoyooBBBBBE",
                "EBBBBBooooBBBBBE",
                "EBBBBBooooBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBByBBBBBBBE",
                "EBByBBByBBBByBBE",
                "EBBByBByBBByBBBE",
                "EBBBByBwwByBBBBE",
                "EBBBBBwwwwBBBBBE",
                "EBBBBwwwwwwBBBBE",
                "EByyywwwwwwyyyyE",
                "EBBBBBwwwwBBBBBE",
                "EBBBByBwwByBBBBE",
                "EBBByBBByBByBBBE",
                "EBByBBBByBBByBBE",
                "EBBBBBBByBBBBBBE",
                "EBBBBBBByBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EByyBBBBBBBBBBBE",
                "EBByyBBBBBBBBBBE",
                "EBBByyBBBBBBBBBE",
                "EBBBByyBBBBBBBBE",
                "EBBBBByyBBBBBBBE",
                "EBBBBBByooooBBBE",
                "EBBBBBBooooooBBE",
                "EBBBBBBooyoooBBE",
                "EBBBBBBooooooBBE",
                "EBBBBBBooooooBBE",
                "EBBBBBBBooooBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBooooooooooBBE",
                "EBBooooooooooBBE",
                "EBBooooooooooBBE",
                "EBBooooooooooBBE",
                "EBBooooyyooooBBE",
                "EBBooooyyooooBBE",
                "EBBoooyyyyoooBBE",
                "EBBBooyyyyooBBBE",
                "EBBBBoyyyyoBBBBE",
                "EBBBBBooooBBBBBE",
                "EBBBBBBooBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBrrBBBBBBE",
                "EBBBBBrrrrBBBBBE",
                "EBBBBBrrrrBBBBBE",
                "EBBBBrrrrrrBBBBE",
                "EBBBrrroorrrBBBE",
                "EBBrrroooorrrBBE",
                "EBBrrroooorrrBBE",
                "EBBrroooooorrBBE",
                "EBBrrooyyoorrBBE",
                "EBBrrooyyoorrBBE",
                "EBBrrooyyoorrBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBooBBBBBBBBBBBE",
                "EBBooBBBBBBBBBBE",
                "EBBBooBBBBBBBBBE",
                "EBBBBooBByBBBBBE",
                "EBBBBBooByBBBBBE",
                "EBBBBBBByyyBBBBE",
                "EBBBBByyyyyyyBBE",
                "EBBBBBByyyyyBBBE",
                "EBBBBBByyyyyBBBE",
                "EBBBBBByyyyyBBBE",
                "EBBBBBByBBByBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBByyyyBBBBBE",
                "EBBBBByyyyBBBBBE",
                "EBBBBByyyyBBBBBE",
                "EBBBBByyyyBBBBBE",
                "EBBBBBwwwwBBBBBE",
                "EBBBBBwwwwBBBBBE",
                "EBBBrrrrrrrrBBBE",
                "EBBBrrrrrrrrBBBE",
                "EBBBBwwwwwwBBBBE",
                "EBBBBwwwwwwBBBBE",
                "EBBBBwwwwwwBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBooBBBBBBE",
                "EBBBBBooooBBBBBE",
                "EBBBBooooooBBBBE",
                "EBBBBoooyooBBBBE",
                "EBBBBooyoooBBBBE",
                "EBBBBddddddBBBBE",
                "EBBBddddddddBBBE",
                "EBBBddddddddBBBE",
                "EBBddddddddddBBE",
                "EBddddddddddddBE",
                "EBddddddddddddBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBByyyBE",
                "EBBBByyyBBByyyBE",
                "EBBBByyyBBByyyBE",
                "EBBBByyyBBBbBBBE",
                "EBBBBbBBbyyyBBBE",
                "EBBBBbBBByyyBBBE",
                "EBBBbBBBByyyBBBE",
                "EBBBbBBBBBBBBBBE",
                "EByyyBBBBBBBBBBE",
                "EByyyBBBBBBBBBBE",
                "EByyyBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBssBBBBBE",
                "EBBBBBssBBBssBBE",
                "EBBBBsBBBBBBBBBE",
                "EBBBsBBssssBBsBE",
                "EBBBsBsBBBsBBsBE",
                "EBBBsBsBBBBsBBBE",
                "EBBBsBsBBsBsBBBE",
                "EBsBBsBBBsBsBBBE",
                "EBsBBssssBBsBBBE",
                "EBBBBBBBBBsBBBBE",
                "EBBssBBBssBBBBBE",
                "EBBBBBssBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBpBBBBBBBE",
                "EBBBBBBppBBBBBBE",
                "EpppBBBpppBBBBBE",
                "EBBBppppppppBBBE",
                "EBBBpppppppppBBE",
                "EppppppppppppBBE",
                "EBBBppppppppBBBE",
                "EBBBBBBpppBBBBBE",
                "EpppBBBppBBBBBBE",
                "EBBBBBBpBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBoBBBBBBE",
                "EBBBBoBoooBoBBBE",
                "EBBoBoooyoooBoBE",
                "EBBooyoooooyooBE",
                "EBBwoooooooowoBE",
                "EBBowrrrrrrwooBE",
                "EBBBrrrrrrrrBBBE",
                "EBBBrrrrrrrrBBBE",
                "EBBrrkkrrkkrrBBE",
                "EBBrrryrryrrrBBE",
                "EBBBrrrrrrrrBBBE",
                "EBBBrrwkkwrrBBBE",
                "EBBBBrrrrrrBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBwwwwwBBBBE",
                "EBBBBBBBwwwwBBBE",
                "EBBBBBBBBwwwwBBE",
                "EBBBBBBBBBwwwBBE",
                "EBBBBBBBBBwwwBBE",
                "EBBBBBBBBBwwwBBE",
                "EBBBBBBBBBwwwBBE",
                "EBBBBBBBBwwwwBBE",
                "EBBBBBBBwwwwBBBE",
                "EBBBBBwwwwwBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBcBBE",
                "EBccccccccccBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBcE",
                "EBccccccccccccBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBcBBBBE",
                "EBccccccccBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBwBBBBBBBE",
                "EBwBBBBwBBBBBwBE",
                "EBBwBBBwBBBBwBBE",
                "EBBBwwBwBBwwBBBE",
                "EBBBBBwwBwBBBBBE",
                "EBBBBBBwwBBBBBBE",
                "EBBBBBBwwBBBBBBE",
                "EBBBBBwBwwBBBBBE",
                "EBBBwwBBwBwwBBBE",
                "EBBwBBBBwBBBwBBE",
                "EBwBBBBBwBBBBwBE",
                "EBBBBBBBwBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBByyBBBBBBE",
                "EBBBByykkkkBBBBE",
                "EBBByykkkkkkBBBE",
                "EBByykkkkkkkkBBE",
                "EBByykkkkkkkkBBE",
                "EByykkkkkkkkkkBE",
                "EByyykkkkkkkkyBE",
                "EBByykkkkkkkkBBE",
                "EBByyykkkkkkyBBE",
                "EBBByyykkkkyBBBE",
                "EBBBByyyyyyBBBBE",
                "EBBBBBByyBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBrrBrBBBBE",
                "EBBBBrBBBBBrBBBE",
                "EBBBrBBrrrBBBBBE",
                "EBBrBBrBBBrBrBBE",
                "EBBBBBrBBBrBBBBE",
                "EBBrBBrBrBBrBrBE",
                "EBBBrBBrrBrBBBBE",
                "EBBBBrBBBBrBBrBE",
                "EBBBBBrrBrBBrBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBrBrBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBppppppppBBBE",
                "EBBppppppppppBBE",
                "EBppppppppppppBE",
                "EBBppppppppppBBE",
                "EBBBppppppppBBBE",
                "EBBBBBBBByBBBBBE",
                "EBBBBBBByBBBBBBE",
                "EBBBBBByyyBBBBBE",
                "EBBBBBBByBBBBBBE",
                "EBBBBBByBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBrrBBBBBBrrE",
                "EBBBBrrrBBBBBrrE",
                "EBBBrrrrrBBBrrrE",
                "ErBrrrrrrBBrrrrE",
                "ErrrrrrrrrrrrrrE",
                "ErrrrrrrrrrrrrrE",
                "ErrrrrrrrrrrrrrE",
                "ErrrrrrrrrrrrrrE",
                "ErrrrrrrrrrrrrrE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBByyyyBBBBBE",
                "EBBByyyyyyyyBBBE",
                "EBByyBBBBBByyBBE",
                "EBByBBBBBBBByBBE",
                "EByyBBwwwwBByyBE",
                "EByyBBwwwwBByyBE",
                "EByyBBwwwwBByyBE",
                "EByyBBwwwwBByyBE",
                "EBByBBBBBBBByBBE",
                "EBByyBBBBBByyBBE",
                "EBBByyyyyyyyBBBE",
                "EBBBBByyyyBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBggggBBBBBE",
                "EBBBBBggggBBBBBE",
                "EBBBBBggggBBBBBE",
                "EBBggggggggggBBE",
                "EBBggggggggggBBE",
                "EBBggggggggggBBE",
                "EBBggggggggggBBE",
                "EBBBBBggggBBBBBE",
                "EBBBBBggggBBBBBE",
                "EBBBBBggggBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBssssssssssBBE",
                "EBBssssssssssBBE",
                "EBBsssyyyysssBBE",
                "EBBsssyyyysssBBE",
                "EBBsssyyyysssBBE",
                "EBBsssyyyysssBBE",
                "EBBsssyyyysssBBE",
                "EBBBssyyyyssBBBE",
                "EBBBBssssssBBBBE",
                "EBBBBBssssBBBBBE",
                "EBBBBBBssBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBoBBBBBBBE",
                "EBBBBBBooBBBBBBE",
                "EBBBoBBooBBoBBBE",
                "EBBBoBoooBBoBBBE",
                "EBBooooooooooBBE",
                "EBBoyooyyooyoBBE",
                "EBBoyooyyooyoBBE",
                "EBBoyooyyooyoBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBooyyooBBBBE",
                "EBBBBooyyooBBBBE",
                "EBBBBooyyooBBBBE",
                "EBBBBooyyooBBBBE",
                "EBBBBooyyooBBBBE",
                "EBBBBooyyooBBBBE",
                "EBBBBooyyooBBBBE",
                "EBBBBooyyooBBBBE",
                "EBBBBooyyooBBBBE",
                "EBBBBooyyooBBBBE",
                "EBBBBooyyooBBBBE",
                "EBBBBooyyooBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBwwBBBBBBE",
                "EBBBBBBwwBBBBBBE",
                "EBBBBBBwwBBBBBBE",
                "EBBBBBBwwBBBBBBE",
                "EBBBBBBwwBBBBBBE",
                "EBBBBBBwwBBBBBBE",
                "EBBBBBBwwBBBBBBE",
                "EBBBBBBwwBBBBBBE",
                "EBBByyyyyyyyBBBE",
                "EBBByyyyyyyyBBBE",
                "EBBBBBByyBBBBBBE",
                "EBBBBBByyBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBoBBBBBBBE",
                "EBBBBBBoBBBBBBBE",
                "EBBoBBBoBBBBoBBE",
                "EBBBoBBoBBBoBBBE",
                "EBBBBoyyyyoBBBBE",
                "EBBBByyyyyyBBBBE",
                "EBBBByyyyyyBBBBE",
                "EooooyyyyyyBoooE",
                "EBBBByyyyyyBBBBE",
                "EBBBBoyyyyoBBBBE",
                "EBBBoBBBBBBoBBBE",
                "EBBoBBBBoBBBoBBE",
                "EBBBBBBBoBBBBBBE",
                "EBBBBBBBoBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBssssssssssBBE",
                "EBBssssssssssBBE",
                "EBBssssddssssBBE",
                "EBBssssddssssBBE",
                "EBBssddddddssBBE",
                "EBBssddddddssBBE",
                "EBBssssddssssBBE",
                "EBBBsssddsssBBBE",
                "EBBBBssddssBBBBE",
                "EBBBBBsddsBBBBBE",
                "EBBBBBBssBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBByyyyBBBBBE",
                "EBBByyyyyyyyBBBE",
                "EBBByyyyyyyyBBBE",
                "EBByyyyyyyyyyBBE",
                "EBByyyyyyyyyyBBE",
                "EBooooooooooooBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[]
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBsBBsBBsBBsBBE",
                "EBBsBBsBBsBBsBBE",
                "EBBssssssssssBBE",
                "EBBssssssssssBBE",
                "EBBssssssssssBBE",
                "EBBssssssssssBBE",
                "EBBsssskkssssBBE",
                "EBBsssskkssssBBE",
                "EBBsssskkssssBBE",
                "EBBsssskkssssBBE",
                "EBBsssskkssssBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[] // Prism Ring（彩灯者）
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBrrooBBBBBE",
                "EBBBBrBBBByBBBBE",
                "EBBBpBBBBBByBBBE",
                "EBBpBBBBBBBBgBBE",
                "EBBpBBBBBBBBgBBE",
                "EBBbBBBwwBBBcBBE",
                "EBBbBBBwwBBBcBBE",
                "EBBbBBBBBBBBcBBE",
                "EBBBcBBBBBBgBBBE",
                "EBBBBcBBBBgBBBBE",
                "EBBBBBcggyBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[] // Spectrum Volley（彩灯者）
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBrrBBE",
                "EBBBBBBBBBrrrBBE",
                "EBBBBBBBBrrBBBBE",
                "EBBBBBBBoyBBggBE",
                "EBBBBBBoyBBgggBE",
                "EBBBBByoBBggBBBE",
                "EBBBBwyBBccBBBBE",
                "EBBBwwBBccbbBBBE",
                "EBBwwBBccBbbBBBE",
                "EBwwBBcbBBBBBBBE",
                "EBwBBccBBBBBBBBE",
                "EBBBpbBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[] // Vivid Step（彩灯者）
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBrrrrrBBBBwBBBE",
                "EBBBBBBBBBBwwBBE",
                "EBooooooBBBwwwBE",
                "EBBBBBBBBBBwwwwE",
                "EByyyyyyywwwwwwE",
                "EBBBBBBBBBBwwwwE",
                "EBggggggBBBwwwBE",
                "EBBBBBBBBBBwwBBE",
                "EBccccBBBBBwBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBppBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[] // Rainbow Pillar（彩灯者・虹の十字）
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBbbBBBBBBE",
                "EBBBBBBccBBBBBBE",
                "EBBBBBBggBBBBBBE",
                "EBBBBBByyBBBBBBE",
                "EBBBBBBwwBBBBBBE",
                "EBbcgywwwwygcbBE",
                "EBbcgywwwwygcbBE",
                "EBBBBBBwwBBBBBBE",
                "EBBBBBByyBBBBBBE",
                "EBBBBBBggBBBBBBE",
                "EBBBBBBccBBBBBBE",
                "EBBBBBBbbBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[] // Vivid Grace（彩灯者）
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBwBBBBBBBE",
                "EBBBBBBwBBBBBBBE",
                "EBBBBpppppppBBBE",
                "EBBBpwwwwwwwpBBE",
                "EBBBpwrryggwpBBE",
                "EBBBpwrryggwpBBE",
                "EBBBpwoyycbwpBBE",
                "EBBBBpwyccwpBBBE",
                "EBBBBpwwbwwpBBBE",
                "EBBBBBpwwwpBBBBE",
                "EBBBBBBpwpBBBBBE",
                "EBBBBBBBpBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[] // Prism Burst（彩灯者・6つ目：光の輪＋回復）
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBrBBBBBBBE",
                "EBBpBBBrBBBoBBBE",
                "EBBBpBBrBBoBBBBE",
                "EBBBBpBrBoBBBBBE",
                "EBBBBBpwwBBBBBBE",
                "EbbbbbwwwwyyyyyE",
                "EBBBBBwwwwBBBBBE",
                "EBBBBBBwwgBBBBBE",
                "EBBBBBcBgBgBBBBE",
                "EBBBBcBBgBBgBBBE",
                "EBBBcBBBgBBBgBBE",
                "EBBBBBBBgBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[] // Prism Twin（彩灯者・8つ目：分身）
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBBppBBBBBBBBBE",
                "EBBBppBBBBwwBBBE",
                "EBBBBBBBBBwwBBBE",
                "EBBppppBBBBBBBBE",
                "EBppppppBwwwwBBE",
                "EBpBppBpwwwwwwBE",
                "EBpBppBpwBwwBwBE",
                "EBBBppBBwBwwBwBE",
                "EBBpBBpBBBwwBBBE",
                "EBBpBBpBBwBBwBBE",
                "EBppBBppBwBBwBBE",
                "EBBBBBBBwwBBwwBE",
                "EBBBBBBBBBBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
            new[] // Rainbow Rain（彩灯者・7つ目）
            {
                "EEEEEEEEEEEEEEEE",
                "EBBBBBBBBBBBBBBE",
                "EBBrrBBBBBBBBBBE",
                "EBBrrBBBBBBccBBE",
                "EBBooBBBBBBccBBE",
                "EBBooBByyBBbbBBE",
                "EBByyBByyBBbbBBE",
                "EBBwwBBggBBppBBE",
                "EBBBBBBggBBwwBBE",
                "EBBBBBBccBBBBBBE",
                "EBBBpBBwwBBBBBBE",
                "EBBpwpBBBBBBpBBE",
                "EBBBBBBBBBBpwpBE",
                "EBBBBBBBpBBBBBBE",
                "EBBBBBBpwpBBBBBE",
                "EEEEEEEEEEEEEEEE",
            },
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
            string key = $"SkillBadgeFx_{effect}_{c.r}_{c.g}_{c.b}";
            var dark = new Color32((byte)(c.r * 0.2f), (byte)(c.g * 0.2f), (byte)(c.b * 0.2f), 255);
            return Badge(key, SkillIconRows[i], new Dictionary<char, Color32[]> { { 'K', RampFrom(c) } }, dark, c); // 縁はスキルの色

        }

        /// <summary>固有効果のアイコン（図鑑など）。加護やスキルの記号を流用し、伝説の色の縁のバッジに収める。</summary>
        public static Sprite UniqueIcon(UniqueType u)
        {
            Color32 edge = RarityInfo.ColorOf(Rarity.Legendary);
            var bg = new Color32(44, 22, 12, 255);
            string key = "UniqueBadge_" + u;
            switch (u)
            {
                case UniqueType.ExplodeOnKill: return Badge(key, BoonIconRows[Mathf.Min(11, BoonIconRows.Length - 1)], null, bg, edge);
                case UniqueType.HealOnKill: return Badge(key, BoonIconRows[5], null, bg, edge);
                case UniqueType.Vampiric: return Badge(key, BoonIconRows[6], null, bg, edge);
                case UniqueType.SplitShot: return Badge(key, SkillIconRows[1], new Dictionary<char, Color32[]> { { 'K', RampFrom(edge) } }, bg, edge);
                case UniqueType.DodgeNova: return Badge(key, SkillIconRows[0], new Dictionary<char, Color32[]> { { 'K', RampFrom(new Color32(255, 140, 60, 255)) } }, bg, edge);
                default: return Badge(key, SkillIconRows[3], new Dictionary<char, Color32[]> { { 'K', RampFrom(new Color32(230, 230, 240, 255)) } }, bg, edge);
            }
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
        /// 装備のアイコン（32ドット）。武器は種類ごとに形を描き分け、ランタン・防具（外套）・指輪もそろえた絵にする。
        /// r の部分が珍しさの色になる。
        /// </summary>
        public static Sprite ItemIcon(Item item)
        {
            if (item == null) return null;
            string[] rows;
            string kind;
            switch (item.Slot)
            {
                case ItemSlot.Lantern: rows = Icon32_Lantern; kind = "Lantern"; break;
                case ItemSlot.Armor: rows = Icon32_Armor; kind = "Armor"; break;
                case ItemSlot.Ring: rows = Icon32_Ring; kind = "Ring"; break;
                default:
                    if (!WeaponIcons32.TryGetValue(item.Weapon, out rows)) return ItemIcon(item.Slot, item.Rarity);
                    kind = item.Weapon.ToString();
                    break;
            }
            Color32 c = RarityInfo.ColorOf(item.Rarity);
            var abyssSet = AbyssSets.SetOf(item);
            string key = $"ItemIcon32_{kind}_{item.Rarity}" + (item.Abyss ? "_Abyss" + abyssSet : "") + (item.Heaven ? "_Heaven" : "");
            if (item.Abyss) rows = AbyssMotif(rows, abyssSet); // 深淵の品は、セットごとの意匠を描き足す
            // m 金属・w 木・g 金・L と l 光・e 内側の線・A 珍しさの色（縁取りと陰影は Shaded が付ける）
            // 深淵の装備は、金属が黒ずみ、まわりに赤紫のもやがかかる
            var over = new Dictionary<char, Color32[]> { { 'A', RampFrom(c) } };
            if (item.Abyss) over['m'] = RampFrom((Color32)Color.Lerp(new Color32(70, 40, 80, 255), Color.Lerp(Color.black, AbyssSets.ColorOf(abyssSet), 0.6f), 0.45f)); // 黒ずんだ金属を、セットの色に少し寄せる
            if (item.Heaven) over['m'] = Ramp(255, 252, 240, 236, 226, 200, 206, 190, 150, 160, 140, 100); // 天界の装備は、白金の金属
            Color32? haze = item.Abyss ? (Color32?)(Color32)AbyssSets.ColorOf(abyssSet) : item.Heaven ? (Color32?)(Color32)RarityInfo.HeavenColor : null;
            return Shaded(key, rows, over, haze, 64); // もやはセットの色（天界は金）。32 ドットで、地面では今までと同じ半マス
        }

        /// <summary>
        /// 深淵の品の意匠（セットごと。どの装備の絵にも描き足せるよう、形から自動で付ける）：
        /// 業火＝上へ立ちのぼる炎／裂け目＝品を斜めに走る紫の亀裂と、浮かぶ欠片／星落とし＝まわりに瞬く星と、金属に散る星屑／報い＝外へ突き出す紅いとげ。
        /// </summary>
        static string[] AbyssMotif(string[] src, UniqueType set)
        {
            int h = src.Length, w = src[0].Length;
            var g = new char[h][];
            for (int y = 0; y < h; y++) g[y] = src[y].PadRight(w, '.').ToCharArray();
            bool Solid(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && src[y].Length > x && src[y][x] != '.';
            void Put(int x, int y, char c) { if (x >= 0 && y >= 0 && x < w && y < h && g[y][x] == '.') g[y][x] = c; }
            int k = Mathf.Max(1, w / 16); // 絵の大きさに合わせて意匠も大きく（32 ドットなら2倍）
            switch (set)
            {
                case UniqueType.AbyssBlast:
                    // いちばん上の縁から、炎の舌が立ちのぼる（根元は橙、先は黄）
                    for (int x = 0; x < w; x++)
                    {
                        int top = -1;
                        for (int y = 0; y < h; y++) if (Solid(x, y)) { top = y; break; }
                        if (top < 1 || (x * 7 + top) % 3 == 0) continue;
                        int len = k + (x * 5 + top) % (k + 2);
                        for (int d = 1; d <= len; d++) Put(x, top - d, d <= (len + 1) / 2 ? 'L' : 'l');
                    }
                    break;
                case UniqueType.VoidRift:
                {
                    // 左上から右下へ、品の上だけを走るぎざぎざの亀裂（紫に、ところどころ白い光）
                    for (int t = 0; t < (w + h) * 2; t++)
                    {
                        int x = t / 2 + 2 * k, y = t / 2 + ((t / k) % 4 < 2 ? k : 2 * k);
                        for (int dx = 0; dx < k; dx++)
                            if (Solid(x + dx, y)) g[y][x + dx] = t % 5 == 0 ? 'o' : 'p';
                    }
                    // まわりに浮かぶ欠片（k ドット角）
                    void Shard(int cx, int cy) { for (int a = 0; a < k; a++) for (int b = 0; b < k; b++) Put(cx + a, cy + b, 'p'); Put(cx + k, cy + k, 'p'); }
                    Shard(k, 3 * k); Shard(w - 3 * k, h - 4 * k); Shard(w - 2 * k, 2 * k); Shard(k, h - 3 * k);
                    break;
                }
                case UniqueType.Starfall:
                {
                    // まわりに瞬く星（十字、腕の長さは k）と、金属に散る星屑
                    void Star(int cx, int cy) { Put(cx, cy, 'o'); for (int a = 1; a <= k; a++) { char c = a == k ? 'B' : 'o'; Put(cx - a, cy, c); Put(cx + a, cy, c); Put(cx, cy - a, c); Put(cx, cy + a, c); } }
                    Star(2 * k, 2 * k); Star(w - 3 * k, h - 4 * k); Star(w - 2 * k, k); Star(k + 1, h - 3 * k);
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                            if ((g[y][x] == 'm' || g[y][x] == 'w') && (x * 3 + y * 5) % 7 == 0) g[y][x] = 'o';
                    break;
                }
                case UniqueType.Retribution:
                    // 縁から外へ突き出すとげ（長さは k＋1。根元は紅、先は白く光る）
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                        {
                            if (!Solid(x, y) || (x * 3 + y * 7) % (4 + k) != 0) continue;
                            int dx = !Solid(x - 1, y) ? -1 : !Solid(x + 1, y) ? 1 : 0;
                            int dy = dx == 0 && !Solid(x, y - 1) ? -1 : 0;
                            if (dx == 0 && dy == 0) continue;
                            for (int d = 1; d <= k + 1; d++) Put(x + dx * d, y + dy * d, d == k + 1 ? 'o' : 'R');
                        }
                    break;
            }
            var outRows = new string[h];
            for (int y = 0; y < h; y++) outRows[y] = new string(g[y]);
            return outRows;
        }

        static readonly string[] LanternIcon16 =
        {
            "................",
            "......g..g......",
            "................",
            "......gggg......",
            ".....gggggg.....",
            ".....AllllA.....",
            ".....AlLLlA.....",
            ".....AlLLlA.....",
            ".....ALLLLA.....",
            ".....AlLLlA.....",
            ".....gggggg.....",
            "......gggg......",
            "................",
            "................",
            "................",
            "................",
        };

        static readonly string[] ArmorIcon16 =
        {
            "................",
            "................",
            "..ggg......ggg..",
            "..ggAA....AAgg..",
            "...eAAAeeAAAe...",
            "....AAmeeAAA....",
            "....AmAAAAAA....",
            "....AAAeeAAA....",
            "....AAAeeAAA....",
            "....gggggggg....",
            "....AgA..AgA....",
            "....AA....AA....",
            "................",
            "................",
            "................",
            "................",
        };

        static readonly string[] RingIcon16 =
        {
            "................",
            "................",
            "................",
            "......AmAA......",
            "......AAAA......",
            ".......gg.......",
            "......gggg......",
            "....gg....gg....",
            "...g........g...",
            "...g........g...",
            "...g........g...",
            "....g......g....",
            ".....gg..gg.....",
            "................",
            "................",
            "................",
        };

        static readonly Dictionary<WeaponKind, string[]> WeaponIcons = new Dictionary<WeaponKind, string[]>
        {
            { WeaponKind.Wand, new[]
            {
                "................",
                "............AAA.",
                "...........AlAA.",
                "...........AAAA.",
                "...........eAA..",
                "..........w.....",
                ".........w......",
                "........w.......",
                ".......w........",
                "......w.........",
                ".....w..........",
                "....w...........",
                "...w............",
                "..w.............",
                "................",
                "................",
            } },
            { WeaponKind.Repeater, new[]
            {
                "................",
                "...........A....",
                "..............A.",
                "................",
                "........AA..w...",
                ".........eew....",
                "........www.....",
                ".......wgew.....",
                "......w.........",
                ".....w..........",
                "....w...........",
                "...w............",
                "..w.............",
                "................",
                "................",
                "................",
            } },
            { WeaponKind.Fan, new[]
            {
                "................",
                "..........A.A.A.",
                "...........AAA..",
                "..........AlA...",
                "..........gg....",
                ".........w......",
                "........w.......",
                ".......w........",
                "......w.........",
                ".....w..........",
                "....w...........",
                "...w............",
                "..w.............",
                "................",
                "................",
                "................",
            } },
            { WeaponKind.Sword, new[]
            {
                "................",
                "............mm..",
                "...........mmA..",
                "..........mmA...",
                ".........mmA....",
                "........mmA.....",
                ".......mmA......",
                "......mmA.......",
                "...g.mmA........",
                "....ggA.........",
                "....wgg.........",
                "...w..g.........",
                "..w.............",
                "..g.............",
                "................",
                "................",
            } },
            { WeaponKind.Dagger, new[]
            {
                "................",
                "................",
                "................",
                "................",
                "..........mm....",
                ".........mmA....",
                "........mmA.....",
                ".......mmA......",
                ".....gemA.......",
                "......gg........",
                "......wgg.......",
                ".....w..........",
                "....w...........",
                "....g...........",
                "................",
                "................",
            } },
            { WeaponKind.Greatblade, new[]
            {
                "................",
                "..........mmmm..",
                ".........mmmmA..",
                "........mmmmA...",
                ".......mmmmA....",
                "......mmmmA.....",
                ".....mmmmA......",
                "....mmmmA.......",
                "..gemmmA........",
                "...gggA.........",
                "...wggg.........",
                "..w...g.........",
                ".w..............",
                ".g..............",
                "................",
                "................",
            } },
            { WeaponKind.Censer, new[]
            {
                "................",
                "........m.......",
                ".........m......",
                "........m.......",
                ".........m......",
                "........ee......",
                ".......gggg.....",
                "......gAAAAg....",
                ".....gLlLlLlg...",
                ".....gLllllg....",
                ".....ggggggg....",
                "......gAAAAg....",
                ".......gggg.....",
                "........ee......",
                "................",
                "................",
            } },
            { WeaponKind.Chain, new[]
            {
                "................",
                "...m............",
                "....m...........",
                "...eee..........",
                "...AlA..........",
                "...AAA..........",
                "......m.........",
                ".......m........",
                "........m.m.....",
                ".........eee....",
                ".........AlA....",
                ".........AAA....",
                "............m...",
                ".............m..",
                "................",
                "................",
            } },
            { WeaponKind.Brazier, new[]
            {
                "................",
                "................",
                "......L...L.....",
                ".....LlL..Ll....",
                ".....LllLlll....",
                "....LlllllllL...",
                "...eeeeeeeeeee..",
                "...gAAAAAAAAAg..",
                "....ggggggggg...",
                ".....ggggggg....",
                ".......www......",
                "........w.......",
                "........w.......",
                ".......www......",
                ".....wwwwwww....",
                "................",
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
        /// <summary>
        /// やわらかい光の柱（レベルアップ）。幅16ドット・高さ32ドット、足元が原点。
        /// 中心ほど明るく横へ段々に薄れ、上へ行くほど消える（ドット絵らしく、明るさは数段の段差で）。
        /// </summary>
        public static Sprite SoftPillar
        {
            get
            {
                const string key = "SoftPillar";
                if (cache.TryGetValue(key, out var cached)) return cached;
                const int w = 16, h = 32;
                var px = new Color32[w * h];
                for (int y = 0; y < h; y++)
                {
                    float v = Mathf.Pow(1f - (float)y / h, 1.3f);
                    v *= Mathf.Clamp01((y + 1f) / 5f); // 上へ行くほど薄く。下の数段もぼかす（足元より下まで、ふわっと光が広がる）
                    for (int x = 0; x < w; x++)
                    {
                        float d = Mathf.Abs(x + 0.5f - w * 0.5f) / (w * 0.5f);
                        float hz = Mathf.Clamp01(1f - d * d);
                        float a = Mathf.Round(hz * v * 5f) / 5f; // 5段の段差
                        px[y * w + x] = new Color32(255, 255, 255, (byte)(255 * a));
                    }
                }
                var tex = MakeTexture(key, w, h, px, TextureWrapMode.Clamp);
                var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), PixelsPerUnit, 0, SpriteMeshType.FullRect);
                sprite.name = key;
                cache[key] = sprite;
                return sprite;
            }
        }

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
        public static Sprite Chest(bool open) => open ? Shaded("ChestOpen28", ChestOpenRows, new Dictionary<char, Color32[]> { { 'K', Ramp(70, 44, 34, 52, 32, 26, 38, 22, 18, 26, 14, 12) } }, null, 32) : Shaded("ChestClosed28", ChestClosedRows, null, null, 32);



        // ---- 置き物と弾（陰影つき、1 マス = 32 ドットの細かさ）----
        // 閉じた宝箱（開いた箱と同じ高さ。上は空き）
        static readonly string[] ChestClosedRows =
        {
            "............................",
            "............................",
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

        // 開いた宝箱：ふたを後ろへ大きく立てて裏側を見せ、中の宝が光る（閉じた箱と同じ高さにそろえ、箱の位置がずれないように）
        static readonly string[] ChestOpenRows =
        {
            "....wwggwwwwwwwwwwwwggww....",
            "....wwggKKKKKKKKKKKKggww....",
            "...wwwggKKKKKKKKKKKKggwww...",
            "...wwwggKKKKKKKKKKKKggwww...",
            "...wwwggKKKKKKKKKKKKggwww...",
            "..wwwwggKKKKKKKKKKKKggwwww..",
            "..ggggggggggggggggggggggggo.",
            ".gKKKKKKKoKKKKKKlLlKKKKKKKg.",
            ".gKKKlLlKKKlLlKlLoLlKKlLKKg.",
            ".gKlLoLlKlLoLLlLLLLLlLLoLlg.",
            ".glLLLLLlLLLLLoLLLLLLLLLLLg.",
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

        /// <summary>区域ごとのボスの絵（32 ドット、陰影つき。BossKind の順）。敵の絵を土台に、色と飾りを変えている。</summary>
        public static Sprite BossSprite(BossKind kind) => BossSprite(kind, 0);

        /// <summary>ボスの絵。塔の2周目からは、周の数だけ深淵に侵された姿。</summary>
        public static Sprite BossSprite(BossKind kind, int cycle)
        {
            if (kind == BossKind.Goddess) return GoddessBleeding; // 昇る塔の100層：血の涙を流す女神像
            if (BossInfo.IsHeaven(kind)) return HeavenBossSprite(kind); // 天界のボス
            if (BossInfo.IsAbyss(kind)) return AbyssBossSprite(kind);
            int i = Mathf.Clamp((int)kind, 0, BossRows.Length - 1);
            if (cycle > 0) return Corrupted($"Boss32_{i}_c{cycle}", BossRows[i], BossColors[i], cycle);
            return Shaded("Boss32_" + i, BossRows[i], BossColors[i]);
        }

        /// <summary>深淵のボス。暗い体に、区画の色で光る目・裂け目と光の縁。k 体・K 暗い所・z 骨・Z 光・M 膜。</summary>
        static Sprite AbyssBossSprite(BossKind kind)
        {
            if (kind == BossKind.AbyssKing) return GuideAbyss; // 最奥のボス「アビス」は、案内役の白い姿
            int i = Mathf.Clamp((int)kind - (int)BossKind.RiftWarden, 0, AbyssBossRows.Length - 1);
            var z = Zones.For(GameConfig.AbyssFirstFloor + i * GameConfig.BossFloorEvery);
            var glow = z.Accent;
            var over = new Dictionary<char, Color32[]>
            {
                { 'k', RampFrom(Mix(new Color32(12, 8, 16, 255), z.Slime, 0.42f)) },
                { 'K', RampFrom(Mix(new Color32(6, 3, 8, 255), z.SlimeDark, 0.45f)) },
                { 'M', RampFrom(Mix(new Color32(10, 5, 12, 255), z.Accent, 0.3f)) },
                { 'n', RampFrom(Mix(new Color32(12, 8, 18, 255), z.SlimeDark, 0.35f)) },
                { 'z', Ramp(214, 206, 196, 170, 160, 150, 120, 112, 106, 80, 74, 70) },
                { 'Z', new[] { glow, glow, glow, glow } },
            };
            return Shaded("AbyssBoss32_" + i, AbyssBossRows[i], over, Mix(z.Accent, new Color32(255, 255, 255, 255), 0.4f), 0, true);
        }

        static string[][] AbyssBossRows => new[] { BossRiftWardenRows, BossStarEaterRows, BossMoonPriestRows };

        static readonly string[] BossRiftWardenRows =
        {
            "......z..................z......",
            ".......zz..............zz.......",
            "........zz............zz........",
            ".........zzz........zzz.........",
            "..........zzzKKKKKKzzz......zZ..",
            "...........zzKKKKKKzz.......zZ..",
            "............KKKKKKKK........zZz.",
            "............KKKKKKKK........zZz.",
            "............KZZZoZZZ........zZz.",
            "............KKKKKKKK........zZz.",
            "......K.....KKKKKKKK.....K..zZz.",
            "....KKKKK.KKKKKKKKKKKK.KKKKKzZz.",
            "...KKKKKKKKKKKKKKKKKKKKKKKKKzZz.",
            "...KKKKKKKKKKKKKKKKKKKKKKKKKzZz.",
            "...KKKKKKKKKKKKKZKKKKKKKKKKKzZz.",
            "....kkkkK.kkKKKKZKKKkk.KkkkkzZz.",
            "....kkkk..kkkkkkZkkkkk..kkkkzZz.",
            "....kkkk..kkkkkkZkkkkk..kkkkzZz.",
            "....kkkk..kkkkkkZkkkkk..kkkkzZz.",
            "....kkkk..kkkkkkZkkkkk..kkkkzZz.",
            "....kkkk..kkkkkkZkkkkk..kkkkzZz.",
            "....kkkk..kkkkkkZkkkkk..kkkkzZz.",
            "....kkkk..kkkkkkZkkkkk..kkkkzZz.",
            "....kkkk..kkkkkkkkkkkk....gggggg",
            "..........kkkkkkkkkkkk......KK..",
            "..........KKKKK..KKKKK......KK..",
            "..........KKKKK..KKKKK......KK..",
            "..........KKKKK..KKKKK..........",
            "..........KKKKK..KKKKK..........",
            "..........KKKKK..KKKKK..........",
            "..........KKKKK..KKKKK..........",
            "................................",
        };

        static readonly string[] BossStarEaterRows =
        {
            "................................",
            "................................",
            "................................",
            "............kkkkkkkk............",
            "..........kZkkkkkkkkkZ..........",
            "........kkkkkkZZZZkkkkkk........",
            ".......kkkkkZZooooZZkkkkZ.......",
            "......kkZkkZZooeeooZZkkkkk......",
            "......kkkkkZZooeeooZZkkkkk......",
            ".....kkkkkkZZooeeooZZkkkkkk.....",
            ".....kkkkkkZZooeeooZZkkkkkk.....",
            ".....kkkkkkkZZooooZZkkkkkkk.....",
            ".....kkkkkkkkkZZZZkkkkkkkkk.....",
            ".....kZkkkkkkkeeeekkkkkkkkk.....",
            ".....kkkkkkzezezezezezkkkkZ.....",
            "......kkkkeeeeeeeeeeeekkkk......",
            "......kkkkeeeeeeeeeeeekkkk......",
            ".......kkkkeeeeeeeeeekkkk.......",
            "........kkkkzkzezezkzkzk........",
            "..........kkkkkkkkkkkk..........",
            "........K...KkkkKkkkK...K.......",
            "........K...K...K...K...K.......",
            "........K...K...K...K...K.......",
            ".......K...K...K...K...K........",
            ".......K...K...K...K...K........",
            ".......K...K...K...K...K........",
            "........K...K...K...K...K.......",
            "........K...K...K...K...K.......",
            "........K...K...K...K...K.......",
            ".........K...K...K...K...K......",
            ".........Z...Z...Z...Z...Z......",
            "................................",
        };

        static readonly string[] BossMoonPriestRows =
        {
            "................................",
            "................................",
            "...........Z........Z...........",
            "...........ZZ......ZZ...........",
            "............ZZZZZZZZ............",
            "...ZZZZ........ZZ...............",
            "...ZZZZ......nnnnnn.............",
            "...ZZoZ.....nnnnnnnn............",
            "...ZZZZ.....nnzzzznn............",
            ".....w......nzzzzzzn............",
            ".....w......nzzzzzzn............",
            ".....w......nzzZzZzn............",
            ".....w......nzzzzzzn............",
            ".....w.......nzeeen.............",
            ".....w.......nnnnnn.......ZZ....",
            ".....w......nnnnKnnn.....ZZZZ...",
            ".....wnnn...nnnnKnnn....nnnoZ...",
            ".....w...nnnnnnnKnnnnnnn..ZZ....",
            ".....w.....ggggggggggg..........",
            ".....w....nnnnnnKnnnnn..........",
            ".....w....nnnnnnKnnnnn..........",
            ".....w...nnnnnnnKnnnnnn.........",
            ".....w...nnnnnnnKnnnnnn.........",
            ".....w..nnnnnnnnKnnnnnnn........",
            ".....w..nnnnnnnnKnnnnnnn........",
            ".....w..nnnnnnnnKnnnnnnn........",
            ".....w.nnnnnnnnnKnnnnnnnn.......",
            ".....w.nnnnnnnnnKnnnnnnnn.......",
            ".....wnnnnnnnnnnKnnnnnnnnn......",
            "................................",
            "................................",
            "................................",
        };

        static readonly Dictionary<char, Color32[]>[] BossColors =
        {
                new Dictionary<char, Color32[]> { { 'q', Ramp(150, 146, 142, 112, 108, 106, 80, 77, 76, 52, 50, 50) }, { 'Q', Ramp(70, 66, 74, 56, 52, 60, 42, 40, 48, 30, 28, 36) } },
                new Dictionary<char, Color32[]> { { 'G', Ramp(140, 200, 110, 96, 160, 76, 64, 118, 52, 40, 80, 34) }, { 'D', Ramp(80, 130, 60, 58, 100, 44, 40, 74, 32, 26, 50, 22) }, { 'r', Ramp(255, 160, 200, 230, 104, 156, 180, 70, 116, 120, 40, 72) } },
                new Dictionary<char, Color32[]> { { 'i', Ramp(116, 104, 104, 84, 74, 76, 60, 52, 56, 40, 34, 38) } },
                new Dictionary<char, Color32[]> { { 'u', Ramp(206, 232, 255, 146, 192, 242, 100, 142, 202, 62, 96, 150) } },
                new Dictionary<char, Color32[]> { { 'j', Ramp(170, 120, 220, 130, 86, 180, 92, 58, 136, 60, 36, 96) }, { 'w', Ramp(250, 244, 224, 226, 214, 184, 190, 176, 140, 140, 126, 96) }, { 'b', Ramp(204, 84, 72, 162, 54, 50, 120, 36, 36, 82, 22, 24) } },
                new Dictionary<char, Color32[]> { { 'P', Ramp(70, 50, 96, 50, 34, 72, 34, 22, 52, 22, 14, 36) }, { 'M', Ramp(30, 20, 44, 22, 14, 34, 16, 10, 26, 10, 6, 18) } },
        };

        static readonly string[][] BossRows =
        {
            new[] // 岩の巨人：苔のない灰色の石の体、胸と目に青い光の紋
            {
                "................................",
                "................................",
                "...........qqqqqqqqqq...........",
                "..........qqqqqqqqqqqq..........",
                "..........qqQqqqqqqQqq..........",
                "..........qqBBqqqqBBqq..........",
                "..........qqqqqqqqqqqq..........",
                "....qqqqq..qqQQQQQQqq..qqqqq....",
                "..qqqqqqqqqqqqqqqqqqqqqqqqqqqq..",
                ".qqqqqqqqqqqqqqqqqqqqqqqqqqqqqq.",
                ".qqqQqqqqqqqqqqBBqqqqqqqqqqQqqq.",
                ".qqqqQqqqqqqqqBBBBqqqqqqqqQqqqq.",
                ".qqqqqqqqQqqqqqBBqqqqQqqqqqqqqq.",
                ".qqqqqq.qqqqqqqqqqqqqqqqq.qqqqq.",
                ".qqqqqq.qqqqqQqqqqqqqqqqq.qqqqq.",
                ".qqQqqq.qqqqqqQqqqqqqqqqq.qqqQq.",
                ".qqqqqq.qqqqqqqqqqqqqQqqq.qqqqq.",
                ".qqqqqq..qqqqqqqqqqqqqqq..qqqqq.",
                "qqqqqqqq.qqqqqqqqqqqqqqq.qqqqqqq",
                "qqqQqqqq.qqqqqqqqqqqqqqq.qqqqQqq",
                "qqqqqqqq..qqqqqqqqqqqqq..qqqqqqq",
                "qqqqqqqq..qqqqqq.qqqqqq..qqqqqqq",
                ".qqqqqq...qqqqqq.qqqqqq...qqqqq.",
                "..........qqqqqq.qqqqqq.........",
                ".........qqqqqqq.qqqqqqq........",
                ".........qqqQqqq.qqqQqqq........",
                ".........qqqqqqq.qqqqqqq........",
                "........qqqqqqqq.qqqqqqqq.......",
                "........qqqqqqqq.qqqqqqqq.......",
                "........QQQQQQQQ.QQQQQQQQ.......",
                "................................",
                "................................",
            },
            new[] // 苔の母：苔の丘のような体、頭に花、蔓の根
            {
                "................................",
                "..........r.r....r.r............",
                ".........rrrrr..rrrrr...........",
                "........rrrlrrr.rrlrrr..........",
                ".........rrrrr..rrrrr...........",
                "..........r.GGGGGG.r............",
                "...........GGGGGGGGGG...........",
                "..........GGGGGGGGGGGG..........",
                ".........GGDDDDDDDDDDGG.........",
                ".........GDDWDDDDDDWDDG.........",
                ".........GDDDDDDDDDDDDG.........",
                "........GGDDDDDwwDDDDDGG........",
                ".......GGGGDDDwwwwDDDGGGG.......",
                "......GGGGGGGGwwwwGGGGGGGG......",
                ".....GGGGGGGGGGwwGGGGGGGGGG.....",
                "....GGDGGGGGGGGwwGGGGGGGDGGG....",
                "...GGDGGGGrGGGGwwGGGGrGGGGDGG...",
                "...GDGGGGrrrGGGwwGGGrrrGGGGDG...",
                "..GGDGGGGGrGGGGwwGGGGrGGGGGDGG..",
                "..GDGGGGGGGGGGGwwGGGGGGGGGGGDG..",
                "..GDGGGGGGGGGGGGGGGGGGGGGGGGDG..",
                ".GGDGGGGGGGGGGGGGGGGGGGGGGGGDGG.",
                ".GDGGGGGDGGGGGGGGGGGGGGDGGGGGDG.",
                ".GDGGGGDGGGGGGGGGGGGGGGGDGGGGDG.",
                ".GGGGGDGGGGGGGGGGGGGGGGGGDGGGGG.",
                "GGGGGDDGGGGGGGGGGGGGGGGGGDDGGGGG",
                "GDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDG",
                "w..w....w....w....w....w....w..w",
                ".w..w....w..w......w..w....w..w.",
                "................................",
                "................................",
                "................................",
            },
            new[] // 炉の巨人：鉄の鎧と角の兜、腹の炉が燃え、大槌を持つ
            {
                "................................",
                "......m..................m......",
                "......mm................mm......",
                ".......mm..iiiiiiiiii..mm.......",
                "........mmiiiiiiiiiiiimm........",
                ".........iiimmmmmmmmiii.........",
                ".........iiLLiiiiiiLLii.........",
                ".........iiiiiiiiiiiiii.........",
                "..........iiiiiiiiiiii..........",
                "....iiiiiiimmmmmmmmmmiiiiiii....",
                "...iiimmmiiiiiiiiiiiiiimmmiii...",
                "..iiiiiiiiiiiiiiiiiiiiiiiiiiii..",
                "..iiiii.iiiimmmmmmmmiiii.iiiii..",
                "..iiiii.iiimLoLoLoLomiii.iiiii..",
                "..iiiii.iiimoLoLoLoLmiii.iiiii..",
                "..iiiii.iiimLoLoLoLomiii.iiiii..",
                "..iiiii.iiimmmmmmmmmmiii.iiiii..",
                "..bbbbb.iiiiiiiiiiiiiiii.bbbbb..",
                "..iiiii.bbbbbbbgbbbbbbbb.iiiiiw.",
                "..iiiii.iiiiiiiiiiiiiiii.iiiiiw.",
                "..mmmmm..iiiiiiiiiiiiii..mmmmmw.",
                ".........iiiiii..iiiiii.......w.",
                ".........iiiiii..iiiiii.......w.",
                ".........iiiiii..iiiiii....mmmmm",
                ".........iiiiii..iiiiii....mmmmm",
                "........biiiiii..iiiiiib...mmmmm",
                "........bbbbbbb..bbbbbbb...mmmmm",
                ".......iiiiiiii..iiiiiiii.......",
                ".......iiiiiiii..iiiiiiii.......",
                "................................",
                "................................",
                "................................",
            },
            new[] // 霜の王：氷の冠と長いひげ、淡い青の外套、氷の杖
            {
                "............x..x..x.............",
                "...........xx..x..xx............",
                "...........xuxxuxxux............",
                "...........xxxxxxxxx............",
                "..........uuuuuuuuuuu...........",
                "..........usssssssssu......BxB..",
                "..........usBssssBssu......xBx..",
                "..........usssssssssu......BxB..",
                "..........uxxxxxxxxxu.......x...",
                "...........xxxxxxxxx........x...",
                "......uuuuuuxxxxxxxuuuuuu...x...",
                ".....uuuuuuuuxxxxxuuuuuuuu..x...",
                "....uuuuxuuuuuxxxuuuuuxuuuussx..",
                "....uuuuuuuuuuuxuuuuuuuuuuuss...",
                "....uuu.uuuuuuuxuuuuuuu.uuu.x...",
                "....uuu.uuuuuugggguuuuu.uuu.x...",
                "....ss..uuuuuuuxuuuuuuu..ss.x...",
                "........uuuuuuuxuuuuuuuu....x...",
                ".......uuuuuuuuxuuuuuuuuu...x...",
                ".......uuuuuuuuxuuuuuuuuu...x...",
                "......uuuuuuuuuxuuuuuuuuuu..x...",
                "......uuuuuuuuuxuuuuuuuuuu..x...",
                ".....uuuuuuuuuuxuuuuuuuuuuu.x...",
                ".....uuuuuuuuuuxuuuuuuuuuuu.x...",
                "....uuuuuuuuuuuxuuuuuuuuuuuux...",
                "....xxxxxxxxxxxxxxxxxxxxxxxxx...",
                ".....x..x...x....x....x...x.....",
                ".....x......x....x........x.....",
                "................................",
                "................................",
                "................................",
                "................................",
            },
            new[] // 書庫の司書：紫の頭巾に光る目、開いた本、まわりに浮かぶ本
            {
                "................................",
                "..bbb......................bbb..",
                ".bwwwb....................bwwwb.",
                ".bwwwb.......jjjjjj.......bwwwb.",
                "..bbb.......jjjjjjjj.......bbb..",
                "...........jjjjjjjjjj...........",
                "..........jjjnnnnnnjjj..........",
                "..........jjnnonnonnjj..........",
                "..........jjnnnnnnnnjj..........",
                "..........jjjnnnnnnjjj..........",
                "...........jjjjjjjjjj...........",
                ".........jjjjjjggjjjjjj.........",
                "........jjjjjjjggjjjjjjjj.......",
                ".......jjjjjjjjggjjjjjjjjj......",
                "......sjjjjjjjjggjjjjjjjjjs.....",
                "......ssbbbbbbbbbbbbbbbbbss.....",
                "........bwwwwwwwbwwwwwwwb.......",
                "........bwwwwwwwbwwwwwwwb.......",
                "........bbbbbbbbbbbbbbbbb.......",
                ".........jjjjjjjggjjjjjjj.......",
                "........jjjjjjjjggjjjjjjjj......",
                "........jjjjjjjjggjjjjjjjj......",
                ".......jjjjjjjjjggjjjjjjjjj.....",
                ".......jjjjjjjjjggjjjjjjjjj.....",
                "......jjjjjjjjjjggjjjjjjjjjj....",
                "......jjjjjjjjjjggjjjjjjjjjj....",
                ".....jjjjjjjjjjjggjjjjjjjjjjj...",
                ".....ggggggggggggggggggggggggg..",
                "..bbb.........................bb",
                ".bwwwb.......................bwb",
                "..bbb........................bb.",
                "................................",
            },
            new[] // 虚ろの影：足のない影、光る紫の目、裾がほどけて漂う
            {
                "................................",
                "..............PPPP..............",
                "............PPPPPPPP............",
                "...........PPPPPPPPPP...........",
                "..........PPPMMMMMMPPP..........",
                "..........PPMMMMMMMMPP..........",
                "..........PPMppMMppMPP..........",
                "..........PPMMMMMMMMPP..........",
                "..........PPPMMMMMMPPP..........",
                "...........PPPMMMMPPP...........",
                ".......PPPPPPPPPPPPPPPPPP.......",
                ".....PPPPPPPPPPPPPPPPPPPPPP.....",
                "...PPPPP.PPPPPPPPPPPPPP.PPPPP...",
                "..PPPP...PPPPPPPPPPPPPP...PPPP..",
                ".PPP.....PPPPPMMPPPPPPP.....PPP.",
                ".MPM.....PPPPMMMMPPPPPP.....MPM.",
                "M.M.M....PPPPPMMPPPPPPP....M.M.M",
                ".........PPPPPPPPPPPPPP.........",
                "........PPPPPPPPPPPPPPPP........",
                "........PPPPPPPPPPPPPPPP........",
                ".......PPPPPPPPPPPPPPPPPP.......",
                ".......PPPPPMPPPPPPMPPPPP.......",
                "......PPPPPMPPPPPPPPMPPPPP......",
                "......PPPPMPPPPPPPPPPMPPPP......",
                ".....PPPPMPPPPMPPMPPPPMPPPP.....",
                ".....PPP.PPP.PPPPPP.PPP.PPP.....",
                "....PP...PP...PPPP...PP...PP....",
                "....P....P.....PP.....P....P....",
                "...P.....P......P.....P.....P...",
                "................................",
                "................................",
                "................................",
            },
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

        /// <summary>大理石（女神像）の色：x 体と衣（象牙色）・k 顔と腕（明るい）・q 彫りの影と台座。</summary>
        static readonly Dictionary<char, Color32[]> MarbleOver = new Dictionary<char, Color32[]>
        {
            { 'x', Ramp(222, 216, 196, 192, 186, 164, 152, 146, 126, 108, 102, 88) },
            { 'k', Ramp(252, 250, 242, 238, 234, 220, 204, 198, 180, 160, 154, 136) },
            { 'q', Ramp(178, 174, 162, 146, 142, 130, 112, 108, 98, 80, 76, 68) },
            { 'g', Ramp(214, 208, 190, 188, 182, 164, 150, 144, 128, 108, 102, 90) },
        };

        static readonly string[][] TownIconRows =
        {
            new[] // 倉庫番：石のアーチに収まった、鉄の帯と輪の取っ手の付いた木の両開きの扉
            {
                "................................",
                "................................",
                "................................",
                "..........qqqqqqqqqq............",
                ".......qqqqqqqqqqqqqqqq.........",
                ".....qqqqwwwwwwwwwwwwwwqqqq.....",
                "....qqqwwwwwbwwwwwwbwwwwwqqq....",
                "...qqqwwwwwwbwwwwwwbwwwwwwqqq...",
                "...qqwwwwwwwbwwnnwwbwwwwwwwqq...",
                "..qqqwwwwwwwbwwnnwwbwwwwwwwqqq..",
                "..qqwmmmmmmmmmmnnmmmmmmmmmmmqq..",
                "..qqwwwwbwwwwwwnnwwwwwwbwwwwqq..",
                "..qqwwwwbwwwwwwnnwwwwwwbwwwwqq..",
                "..qqwwwwbwwwwwwnnwwwwwwbwwwwqq..",
                "..qqwwwwbwwwwggnnggwwwwbwwwwqq..",
                "..qqwwwwbwwwgwwnnwwgwwwbwwwwqq..",
                "..qqwwwwbwwwgwwnnwwgwwwbwwwwqq..",
                "..qqwwwwbwwwwggnnggwwwwbwwwwqq..",
                "..qqwwwwbwwwwwwnnwwwwwwbwwwwqq..",
                "..qqwwwwbwwwwwwnnwwwwwwbwwwwqq..",
                "..qqwmmmmmmmmmmnnmmmmmmmmmmmqq..",
                "..qqwwwwbwwwwwwnnwwwwwwbwwwwqq..",
                "..qqwwwwbwwwwwwnnwwwwwwbwwwwqq..",
                "..qqwwwwbwwwwwwnnwwwwwwbwwwwqq..",
                "..qqwwwwbwwwwwwnnwwwwwwbwwwwqq..",
                "..qqwmmmmmmmmmmnnmmmmmmmmmmmqq..",
                "..qqwwwwbwwwwwwnnwwwwwwbwwwwqq..",
                "..qqwwwwbwwwwwwnnwwwwwwbwwwwqq..",
                "..qqqqqqqqqqqqqqqqqqqqqqqqqqqq..",
                "................................",
                "................................",
                "................................",
            },
            new[] // 商人：赤い回復薬と青い魔力の薬の瓶
            {
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "...........ww...................",
                "..........wwww..................",
                "..........wwww.......ww.........",
                "...........xx.......wwww........",
                "...........xx.......wwww........",
                "..........xxxx.......xx.........",
                ".........xxrrxx......xx.........",
                "........xxrrrrxx....xxxx........",
                ".......xxrrrrrrxx..xxuuxx.......",
                "......xxrrrrrrrrxx.xuuuux.......",
                "......xrrxrrrrrrrx.xuxuux.......",
                ".....xrrxxrrrrrrrrxxuxuuux......",
                ".....xrrxrrrrrrrrrxuuuuuux......",
                ".....xrrrrrrrrrrrrxuuuuuux......",
                ".....xrrrrrrrrrrrrxuuuuuux......",
                "......xrrrrrrrrrrx.xuuuux.......",
                "......xxrrrrrrrrxx.xxxxxx.......",
                ".......xxxrrrrxxx...............",
                ".........xxxxxx.................",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
            },
            new[] // 女神像：月桂冠を戴き、目を閉じて胸の前で手を組んで祈る大理石の胸像。台座に太陽と月の印（原画 docs/art/goddess_reference.jpg）
            {
                "............xxxxxxxx............",
                "..........xxxxxxxxxxxx..........",
                ".........xqqxxxxxxxxqqx.........",
                "........xqxqxxxxxxxxqxqx........",
                "........xxqxxkkkkkkxxqxx........",
                ".......xxxxxkkkkkkkkxxxxx.......",
                ".......xxxxkkkkkkkkkkxxxx.......",
                ".......xxxxkqqkkkkqqkxxxx.......",
                ".......xqxxkkkkkkkkkkxxqx.......",
                ".......xqxxkkkkqkkkkkxxqx.......",
                ".......xxqxxkkkkkkkkxxqxx.......",
                ".......xxqxxkkkqqkkkxxqxx.......",
                "........xqxxxkkkkkkxxxqx........",
                "........xxqxxxkkkkxxxqxx........",
                ".......xxqxxxxkkkkxxxxqxx.......",
                "....xxxxxxxxxxkkkkxxxxxxxxxx....",
                "...xxxxxxxxxxxkkkkxxxxxxxxxxx...",
                "..xxxxxxqxxxxqkkkkqxxxxqxxxxxx..",
                "..xxxxxqxxxxxkkkkkkxxxxxqxxxxx..",
                "..xxxxqxxxxxkkkqqkkkxxxxxqxxxx..",
                "..xxxxqxxxxxkkkqqkkkxxxxxqxxxx..",
                "..xxxqxxxxxkkkxqqxkkkxxxxxqxxx..",
                "...xxqxxxxkkkxxqqxxkkkxxxxqxx...",
                "...xxxkkkkkkxxxqqxxxkkkkkkxxx...",
                "....xxkkkkkxxxxqqxxxxkkkkkxx....",
                ".....xxxxxxxxxxqqxxxxxxxxxx.....",
                ".......qqqqqqqqqqqqqqqqqq.......",
                "........qxxxxxxxxxxxxxxq........",
                "........qxxgggggggggggxq........",
                "........qxxgLggggggBggxq........",
                "........qxxgggggggggggxq........",
                "........qqqqqqqqqqqqqqqq........",
            },
            new[] // 荷運び：緑の背負い袋
            {
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "...........bbbbbbbbb............",
                "..........b.........b...........",
                ".........jjjjjjjjjjjjj..........",
                "........jjjjjjjjjjjjjjj.........",
                ".......jjjjjjjjjjjjjjjjj........",
                ".......jjjjjjjjjjjjjjjjj........",
                ".......jjjjjjjjjjjjjjjjj........",
                ".......jjjjjjjjjjjjjjjjj........",
                ".......nnnnnnnnnnnnnnnnn........",
                ".......jjjbjjjjjjjjjbjjj........",
                ".......jjjbjjjjjjjjjbjjj........",
                ".......jjjgjjjjjjjjjgjjj........",
                ".......jjjbjjjjjjjjjbjjj........",
                ".......jjjbjjjjjjjjjbjjj........",
                ".......jjjbjjjjjjjjjbjjjj.......",
                ".......jjjbjDDDDDDDjbjjjj.......",
                ".......jjjbjDDDDDDDjbjjjj.......",
                ".......jjjbjDDDgDDDjbjjjj.......",
                ".......jjjbjDDDDDDDjbjjjj.......",
                ".......jjjjjjjjjjjjjjjjjj.......",
                "........jjjjjjjjjjjjjjjj........",
                ".........bbbb......bbbb.........",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
            },
            new[] // 鍛冶屋：金床と、その上に斜めに浮かぶふつうの金づち
            {
                "................................",
                "................................",
                "................................",
                "................................",
                "...................mmm..........",
                "..................mmmmm.........",
                ".................mmmmmmm........",
                "..................mmmmmmm.......",
                ".................wmmmmmmmm......",
                "................wwwmmmmmmmm.....",
                "...............wwwww.mmmmmm.....",
                "..............wwwww...mmmm......",
                ".............wwwww..............",
                "............wwwww...............",
                "...........wwwww................",
                "...........www..................",
                "................................",
                "...mmmmmmmmmmmmmmmmmmmmmmmmmm...",
                "....mmmmmmmmmmmmmmmmmmmmmmmmm...",
                "......mmmmmmmmmmmmmmmmmmmmmmm...",
                "..........mmmmmmmmmmmmmmmmm.....",
                ".............mmmmmmmmmm.........",
                ".............mmmmmmmmmm.........",
                ".............mmmmmmmmmm.........",
                "............mmmmmmmmmmmm........",
                "..........mmmmmmmmmmmmmmmm......",
                "..........mmmmmmmmmmmmmmmm......",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
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
        /// <summary>
        /// 深淵に侵された姿（塔の2周目から）。色を周の数だけ深淵の赤紫へ寄せ、3周目からは赤い光の縁が付く。目は元のまま。
        /// </summary>
        static Sprite Corrupted(string key, string[] rows, Dictionary<char, Color32[]> over, int cycle)
        {
            if (cache.TryGetValue(key, out var cached)) return cached;
            var target = Ramp(176, 52, 92, 124, 32, 72, 82, 18, 52, 50, 10, 32);
            float k = Mathf.Min(0.16f * cycle, 0.5f);
            var o = new Dictionary<char, Color32[]>();
            foreach (var row in rows)
            {
                foreach (char ch in row)
                {
                    if (ch == '.' || ch == 'e' || o.ContainsKey(ch) || glows.ContainsKey(ch)) continue;
                    Color32[] r = null;
                    if (over != null && over.TryGetValue(ch, out var or)) r = or;
                    else if (!ramps.TryGetValue(ch, out r)) continue;
                    var m = new Color32[4];
                    for (int i = 0; i < 4; i++) m[i] = Color32.Lerp(r[i], target[i], k);
                    o[ch] = m;
                }
            }
            Color32? aura = cycle >= 2 ? new Color32(230, 40, 80, 255) : (Color32?)null;
            return Shaded(key, rows, o, aura);
        }

        /// <summary>周の数だけ深淵に侵された敵（0 ならふつうの絵）。</summary>
        public static Sprite EnemySprite(EnemyKind kind, int tier, int cycle)
        {
            if (cycle <= 0 || kind == EnemyKind.Golden) return EnemySprite(kind, tier);
            tier = Mathf.Clamp(tier, 0, 2);
            switch (kind)
            {
                case EnemyKind.Bat: return Corrupted($"Bat32_{tier}_c{cycle}", BatRows32[tier], TierDarken(tier, 'P', 'M'), cycle);
                case EnemyKind.Caster: return Corrupted($"Caster32_{tier}_c{cycle}", CasterRows32[tier], TierDarken(tier, 'j'), cycle);
                case EnemyKind.Brute: return Corrupted($"Brute32_{tier}_c{cycle}", BruteRows32[tier], TierDarken(tier, 'q'), cycle);
                case EnemyKind.Bomber: return Corrupted($"Bomber32_{tier}_c{cycle}", BomberRows32[tier], TierDarken(tier, 'i'), cycle);
                default: return Corrupted($"Slime32_{tier}_c{cycle}", SlimeRows32[tier], TierDarken(tier, 'G', 'D'), cycle);
            }
        }

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

        /// <summary>
        /// 深淵の敵（種類ごとに専用の絵）。体は区画の色を黒く沈めた色、目や裂け目は区画の光の色で光り、まわりに光の縁が付く。
        /// k 体・K 体の暗い所・z 骨や角・Z 光（区画の色）・M 膜。上級ほど暗く、光の縁が強い。
        /// </summary>
        public static Sprite AbyssEnemy(EnemyKind kind, ZoneSpec z, int tier = 0)
        {
            tier = Mathf.Clamp(tier, 0, 2);
            string[] rows;
            switch (kind)
            {
                case EnemyKind.Bat: rows = AbyssWingRows; break;
                case EnemyKind.Caster: rows = AbyssSeerRows; break;
                case EnemyKind.Brute: rows = AbyssFiendRows; break;
                case EnemyKind.Bomber: rows = AbyssHuskRows; break;
                default: rows = AbyssOozeRows; break;
            }
            string key = $"Abyss32_{kind}_{z.Name}_{tier}";
            if (cache.TryGetValue(key, out var cached)) return cached;
            float f = tier == 0 ? 0.62f : tier == 1 ? 0.54f : 0.46f; // 床より少し明るく（同じ色の床に埋もれないように）
            Color32 body = Mix(new Color32(10, 6, 14, 255), z.Slime, f);
            Color32 dark = Mix(new Color32(6, 3, 8, 255), z.SlimeDark, f);
            var glow = z.Accent;
            var aura = Mix(z.Accent, new Color32(255, 255, 255, 255), 0.5f + tier * 0.1f); // 白寄りの明るい縁で、同じ色の床からも浮き出す
            var over = new Dictionary<char, Color32[]>
            {
                { 'k', RampFrom(body) },
                { 'K', RampFrom(dark) },
                { 'M', RampFrom(Mix(dark, z.Accent, 0.25f)) },
                { 'n', RampFrom(Mix(new Color32(14, 10, 20, 255), z.SlimeDark, 0.3f)) },
                { 'z', Ramp(214, 206, 196, 170, 160, 150, 120, 112, 106, 80, 74, 70) },
                { 'Z', new[] { glow, glow, glow, glow } },
            };
            return Shaded(key, rows, over, aura, 0, true);
        }

        static readonly string[] AbyssOozeRows =
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
            "........z..............z........",
            "........zz............zz........",
            "........zz............zz........",
            ".........zz..kkkkkk..zz.........",
            ".........zzzkkkkkkkkzzz.........",
            ".........zzkkkkkkkkkkzz.........",
            "........kkkkkkkZZkkkkkkk........",
            "........kkkkkZZooZZkkkkk........",
            ".......kkkkkZZoeeoZZkkkkk.......",
            ".....kkkkkkkZooeeooZkkkkkkk.....",
            ".....kkkkkkkZZoeeoZZkkkkkkk.....",
            "....kkkkkkkkkZZooZZkkkkkkkkk....",
            "....kkkZkkkkkkkZZkkkkkkkkZkk....",
            "...kkkkkZkkkkkkkkkkkkkkkZkkkk...",
            "....kkkkZKKKKKKKKKKKKKKkZkkk....",
            "....kKKKKZKKKKZKKKKKKKKZKKKk....",
            ".....KKKKKZKKKKZZKKKKKZKKKK.....",
            ".....KKKKKKKKKKKKZKKKKKKKKK.....",
            "....KKKKKKKKKKKKKKKKKKKKKKKK....",
            "....KKKKKKKKKKKKKKKKKKKKKKKK....",
            ".....KKK.KKKkkKKKkkKKK..KKK.....",
            "................................",
        };

        static readonly string[] AbyssWingRows =
        {
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            ".MZ..........................ZM.",
            ".MZzz......................zzZM.",
            "..ZMMzz......zz..zz......zzMMZ..",
            "..ZMMMMz.....zz..zz.....zMMMMZ..",
            "..MZMMMMzz...zzzzzz...zzMMMMZM..",
            "..MZMMMMzMzz.zzZZzz.zzMzMMMMZM..",
            "..MZMMMMzMMMzzZzzZzzMMMzMMMMZM..",
            "...ZMMMzMMMMkzzeezzkMMMMzMMMZ...",
            "...ZMMMzMMMMkzzzzzzkMMMMzMMMZ...",
            "...ZMMMzMMMMkkkkkkkkMMMMzMMMZ...",
            "...ZMMMzMM..kkkkkkkk..MMzMMMZ...",
            "...MZMMz..MMkkkkkkkkMM..zMMZM...",
            "....ZMMzMMMMkkkkkkkkMMMMzMMZ....",
            "....ZMzMMMMM.kkkkkk.MMMMMzMZ....",
            "....ZMzMMMM..kkkkkk..MMMMzMZ....",
            ".....ZZMMM....k..k....MMMZZ.....",
            ".......ZZ.....k..k.....ZZ.......",
            ".........Z....k...k...Z.........",
            ".............k....k.............",
            ".............k....k.............",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
        };

        static readonly string[] AbyssSeerRows =
        {
            "................................",
            "...............ZZ...............",
            "..............ZZZZ..............",
            "..............ZZZZ..............",
            "...............ZZ...............",
            "................................",
            ".............nnnnnn.............",
            "............nnnnnnnn............",
            "...........nnnneennnn...........",
            "...........nneeeeeenn...........",
            "...........nneeeeeenn...........",
            "...........nneeeeeenn...........",
            "...........nneZZeZZnn...........",
            "...ZZZ.....nneeoeoenn.....ZZZ...",
            "...ZZZ......neeeeeen......ZZZ...",
            "...ZoZ......nnneennn......ZoZ...",
            "...ZZZ.....nnnnnnnnnn.....ZZZ...",
            ".....k.....nnnnnknnnn......k....",
            "......k...kkkkkkkkkkkkk...k.....",
            ".......k..nnnnnnknnnnn...k......",
            ".......k..nnnnnnknnnnn...k......",
            "........knnnnnnnknnnnnn.k.......",
            ".........knnnnnnknnnnnnk........",
            "........nnnnnnnnknnnnnnn........",
            "........nnnnnnnnknnnnnnn........",
            "........nnnnnnnnknnnnnnn........",
            ".......nnnnnnnnnknnnnnnnn.......",
            "................................",
            ".........n..n..n..n..n..n.......",
            "................................",
            "................................",
            "................................",
        };

        static readonly string[] AbyssFiendRows =
        {
            "........z..............z........",
            ".........z............z.........",
            ".........zz..........zz.........",
            "..........zz........zz..........",
            "..........zzz......zzz..........",
            "...........zzz....zzz...........",
            "...........zzzKKKKzzz...........",
            "............zKKKKKKz............",
            "............KKKKKKKK............",
            "...........KKZZKKKZZK...........",
            "............KKKKKKKK............",
            "............KKzKKKzK............",
            "...KKK.....kkKeeeeekk.....KKK...",
            "..KKKKK...kkkkkkkkkkkk...KKKKK..",
            "..KKKKK..kkkkkkkZkkkkkk..KKKKK..",
            "..KKKKK..kkkkkkkZkkkkkk..KKKKK..",
            "..KKKKK.kkkkkkkZkkkkkkkk.KKKKK..",
            "..KKKKK.kkkkkkkZokkkkkkk.KKKKK..",
            "..kkkkk.kkkkkkkkZkkkkkkk.Kkkkkk.",
            "..kkkkk.kkkkkkkkZkkkkkkk..kkkkk.",
            "..kkkkk.kkkkkkkkoZkkkkkk..kkkkk.",
            "..kkkkk..kkkkkkkZkkkkkk...kkkkk.",
            "..kkkkk..kkkkkkkZkkkkkk...kkkkk.",
            "..kkkkk...kkkkkZkkkkkk....kkkkk.",
            "..kkkkk....kkkkkkkkkk.....kkkkk.",
            "..z.z.z...KKKKKkkkKKKKK...z.z.z.",
            "..........KKKKK...KKKKK.........",
            "..........KKKKK...KKKKK.........",
            "..........KKKKK...KKKKK.........",
            "..........KKKKK...KKKKK.........",
            "..........KKKKK...KKKKK.........",
            "................................",
        };

        static readonly string[] AbyssHuskRows =
        {
            "................................",
            "................................",
            "................................",
            ".................Z..............",
            "................ZZZ.............",
            "................ZZZ.............",
            "................ZoZ.............",
            "................ZZZ.............",
            ".................Z..............",
            "...........z.....z...z..........",
            "...........z....z....z..........",
            "....z.......z...z...z.......z...",
            ".....z......zKKKzKK.z......z....",
            "......z...KKkkkkZkkkKK....z.....",
            ".......z.KkkkkkkZkkkkkK..z......",
            "........KkkkkkkkZkkkkkkK........",
            ".......KkkkeekkkZkkeekkkK.......",
            ".......KkkkeekkkZkkeekkkK.......",
            "......KKkkkkkkkkZkkkkkkkKK......",
            "......KKkkkkkkkkZkkkkkkkKK......",
            "......KKZZZZZZZZoZZZZZZZZK......",
            "......KKkkkkkkkkZkkkkkkkKK......",
            "..zzzzzKkkkkkkkkZkkkkkkkKKzzzzz.",
            "......KKkkkkkkkkZkkkkkkkKK......",
            ".......KKkkkkkkkZkkkkkkKK.......",
            ".......KKKkkkkkkZkkkkkKKK.......",
            "........KKKKkkkkZkkkKKKK........",
            ".........KzKKKKKZKKKKKz.........",
            ".........zKKKKKKZKKKKK.z........",
            ".........KKKKKKKKKK.KKKK........",
            "........zKKKK.......KKKKz.......",
            "................................",
        };

        /// <summary>区画の色のスライム（段階つき。上級ほど暗い）。</summary>
        /// <summary>
        /// さまよう本（沈んだ書庫の区画で、スライムの代わりに出る）。表紙に目のある古い本が、ぱたぱた跳ねて寄ってくる。
        /// 動きと強さはスライムと同じ。上級ほど表紙が暗く、しおり（2段目から）と赤く光る目（3段目）が付く。
        /// </summary>
        public static Sprite TomeFor(ZoneSpec z, int tier = 0)
        {
            tier = Mathf.Clamp(tier, 0, 2);
            float f = tier == 0 ? 1f : tier == 1 ? 0.85f : 0.7f;
            var over = new Dictionary<char, Color32[]>
            {
                { 'b', Scale(Ramp(176, 70, 56, 136, 46, 40, 98, 30, 28, 64, 18, 18), f) },
                { 'w', Ramp(250, 240, 210, 224, 208, 168, 186, 166, 124, 136, 118, 84) },
            };
            var rows = (string[])TomeRows32.Clone();
            if (tier >= 1) rows = Edit(rows, c => c, (29, 12, "uu"), (30, 12, "u"), (30, 14, "u")); // 青いしおりが垂れる
            if (tier >= 2) rows = Edit(rows, c => c == 'x' ? 'R' : c); // 目が赤く光る
            string key = $"Tome32_{z.Key}_{tier}";
            if (z.Cycle > 0) return Corrupted(key, rows, over, z.Cycle);
            return Shaded(key, rows, over);
        }

        static readonly string[] TomeRows32 =
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
            "........bbbbbbbbbbbbbbbbb.......",
            ".......bbgbbbbbbbbbbbbbgbw......",
            ".......bbbbbbbbbbbbbbbbbbww.....",
            ".......bbbggggggggggggbbbww.....",
            ".......bbbgbbbbbbbbbbgbbbww.....",
            ".......bbbgbxxbbbbxxgbbbww.....",
            ".......bbbgbxebbbbxegbbbww.....",
            ".......bbbgbxebbbbxegbbbww.....",
            ".......bbbgbbbbbbbbbbgbbbww.....",
            ".......bbbgbbbbeebbbbgbbbww.....",
            ".......bbbggggggggggggbbbww.....",
            ".......bbbbbbbbbbbbbbbbbbww.....",
            ".......bbgbbbbbbbbbbbbbgbww.....",
            "........wwwwwwwwwwwwwwwwwww.....",
            "........wwwwwwwwwwwwwwwwww......",
            ".........bbbbbbbbbbbbbbbb.......",
            "................................",
            "................................",
            "................................",
        };

        public static Sprite SlimeFor(ZoneSpec z, int tier = 0)
        {
            if (z.Pattern == FloorPattern.Gold) return TomeFor(z, tier); // 沈んだ書庫はスライムではなく、さまよう本
            tier = Mathf.Clamp(tier, 0, 2);
            float f = tier == 0 ? 1f : tier == 1 ? 0.85f : 0.7f;
            var over = new Dictionary<char, Color32[]>
            {
                { 'G', RampFrom(z.Slime, f) },
                { 'D', RampFrom(z.SlimeDark, f) },
            };
            if (z.Cycle > 0) return Corrupted($"Slime32_{z.Key}_{tier}", SlimeRows32[tier], over, z.Cycle);
            return Shaded($"Slime32_{z.Name}_{tier}", SlimeRows32[tier], over);
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

        /// <summary>町の施設のアイコン（32ドット、陰影つき）。0 倉庫番・1 商人・2 女神像・3 荷運び・4 鍛冶屋。</summary>
        public static Sprite TownIcon(int index)
        {
            index = Mathf.Clamp(index, 0, TownIconRows.Length - 1);
            return Shaded("Town32_" + index, TownIconRows[index], index == 2 ? MarbleOver : null);
        }

        /// <summary>
        /// 血の涙を流す女神像（クリア後）。閉じた両目の下から、紅い筋が頬を伝って落ちる。
        /// </summary>
        public static Sprite GoddessBleeding
        {
            get
            {
                if (cache.TryGetValue("Town32_2_blood", out var c)) return c;
                var rows = (string[])TownIconRows[2].Clone();
                void Tear(int col, int from, int to)
                {
                    for (int y = from; y <= to; y++)
                    {
                        var a = rows[y].ToCharArray();
                        if (a[col] != '.') a[col] = 'R';
                        rows[y] = new string(a);
                    }
                }
                Tear(12, 8, 12); // 左目の下から
                Tear(19, 8, 11); // 右目の下から（少し短く）
                var over = new Dictionary<char, Color32[]>(MarbleOver) { { 'R', Ramp(170, 16, 26, 134, 10, 20, 98, 6, 14, 66, 4, 10) } };
                return Shaded("Town32_2_blood", rows, over);
            }
        }



        /// <summary>加護のアイコン（16ドット、陰影つき。Boons の表と同じ順）。</summary>
        public static Sprite BoonIcon(int index)
        {
            index = Mathf.Clamp(index, 0, BoonIconRows.Length - 1);
            return Badge("BoonBadge_" + index, BoonIconRows[index], null, new Color32(40, 30, 18, 255), new Color32(232, 192, 84, 255)); // 祭壇の加護：金の縁
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

        /// <summary>
        /// 転生した人の足元のオーラ（床に寝かせた楕円。幅 2・高さ 1 ユニット）。縁が明るく、内側はうっすら。白で作り、色は SpriteRenderer で付ける。
        /// </summary>
        public static Sprite AuraDisc
        {
            get
            {
                const string key = "AuraDisc";
                if (cache.TryGetValue(key, out var cached)) return cached;
                const int w = 64, h = 32;
                var px = new Color32[w * h];
                float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float dx = (x - cx) / cx, dy = (y - cy) / cy;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        if (d >= 1f) { px[y * w + x] = new Color32(0, 0, 0, 0); continue; }
                        float rim = Mathf.Clamp01(1f - Mathf.Abs(d - 0.82f) / 0.16f); // 縁の明るい輪
                        float fill = 0.35f * (1f - d) + 0.25f * d * d;               // 内側のうっすらした光
                        float a = Mathf.Clamp01(fill + rim * rim * 0.9f);
                        px[y * w + x] = new Color32(255, 255, 255, (byte)(255 * a));
                    }
                var tex = MakeTexture(key, w, h, px, TextureWrapMode.Clamp);
                tex.filterMode = FilterMode.Bilinear;
                return Store(key, tex, 32);
            }
        }

        /// <summary>歯車のアイコン（設定のボタン）。</summary>
        public static Sprite GearIcon => Shaded("Gear16", new[]
        {
            "................",
            "......mmmm......",
            "..mm..mmmm..mm..",
            "..mmmmmmmmmmmm..",
            "...mmmmmmmmmm...",
            ".mmmmm....mmmmm.",
            ".mmmm......mmmm.",
            ".mmmm......mmmm.",
            ".mmmm......mmmm.",
            ".mmmm......mmmm.",
            ".mmmmm....mmmmm.",
            "...mmmmmmmmmm...",
            "..mmmmmmmmmmmm..",
            "..mm..mmmm..mm..",
            "......mmmm......",
            "................",
        }, new Dictionary<char, Color32[]> { { 'm', Ramp(255, 236, 170, 226, 190, 100, 170, 132, 60, 120, 88, 36) } }, null, 16);

        /// <summary>ごみ箱のアイコン（キャラクターを消すボタン）。</summary>
        public static Sprite TrashIcon => Shaded("Trash12", new[]
        {
            "..............",
            ".....mmmm.....",
            "..mmmmmmmmmm..",
            "..............",
            "...mmmmmmmm...",
            "...mm.mm.mm...",
            "...mm.mm.mm...",
            "...mm.mm.mm...",
            "...mm.mm.mm...",
            "...mm.mm.mm...",
            "...mm.mm.mm...",
            "....mmmmmm....",
            "..............",
            "..............",
        }, null, null, 14);

        /// <summary>転生の印（小さなひし形の宝石。白と灰色で作り、色は GUI.color で付ける）。</summary>
        public static Sprite RebirthGem
        {
            get
            {
                const string key = "RebirthGem";
                if (cache.TryGetValue(key, out var cached)) return cached;
                string[] rows =
                {
                    "....o....",
                    "...owo...",
                    "..owwlo..",
                    ".owwllmo.",
                    "owwllmmdo",
                    ".olllmdo.",
                    "..olmdo..",
                    "...odo...",
                    "....o....",
                };
                const int n = 9;
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        char c = rows[n - 1 - y][x];
                        byte v = c == 'w' ? (byte)255 : c == 'l' ? (byte)225 : c == 'm' ? (byte)185 : c == 'd' ? (byte)140 : (byte)60;
                        px[y * n + x] = c == '.' ? new Color32(0, 0, 0, 0) : new Color32(v, v, v, 255);
                    }
                return Store(key, MakeTexture(key, n, n, px, TextureWrapMode.Clamp));
            }
        }

        /// <summary>床に広がる閃光（中心ほど濃い白の円。直径 2 ユニット、Ring と同じ大きさ）。</summary>
        public static Sprite SoftDisc
        {
            get
            {
                const string key = "SoftDisc";
                if (cache.TryGetValue(key, out var cached)) return cached;
                const int size = 32;
                var px = new Color32[size * size];
                float c = (size - 1) * 0.5f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                        float a = Mathf.Clamp01(1f - d);
                        px[y * size + x] = new Color32(255, 255, 255, (byte)(255 * a * Mathf.Sqrt(a)));
                    }
                var tex = MakeTexture(key, size, size, px, TextureWrapMode.Clamp);
                tex.filterMode = FilterMode.Bilinear;
                return Store(key, tex);
            }
        }

        /// <summary>塗りつぶした扇（半径 1 ユニット、右向きに ±60 度）。ボスの扇状の攻撃の予告。縁が少し濃い。</summary>
        public static Sprite Fan
        {
            get
            {
                const string key = "Fan";
                if (cache.TryGetValue(key, out var cached)) return cached;
                const int size = 64;
                var px = new Color32[size * size];
                float c = (size - 1) * 0.5f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x - c, dy = y - c;
                        float d = Mathf.Sqrt(dx * dx + dy * dy) / c;
                        float ang = Mathf.Abs(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg);
                        byte a = 0;
                        if (d <= 1f && ang <= 60f) a = (byte)(d > 0.93f || ang > 56f ? 255 : 110);
                        px[y * size + x] = new Color32(255, 255, 255, a);
                    }
                return Store(key, MakeTexture(key, size, size, px, TextureWrapMode.Clamp), 32);
            }
        }

        /// <summary>火花（4 ドットのひし形。中心が白く光る）。</summary>
        public static Sprite Spark
        {
            get
            {
                const string key = "Spark";
                if (cache.TryGetValue(key, out var cached)) return cached;
                const int size = 4;
                var px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float d = Mathf.Abs(x - 1.5f) + Mathf.Abs(y - 1.5f);
                        px[y * size + x] = d <= 1f ? new Color32(255, 255, 255, 255) : d <= 2f ? new Color32(255, 255, 255, 140) : new Color32(0, 0, 0, 0);
                    }
                return Store(key, MakeTexture(key, size, size, px, TextureWrapMode.Clamp), 32);
            }
        }

        /// <summary>クォータービューの床（32×16 のひし形）。</summary>
        public static Sprite IsoFloor => IsoFloorFor(Zones.For(1), false);
        public static Sprite IsoFloorAlt => IsoFloorFor(Zones.For(1), true);

        /// <summary>区画ごとの床（色と模様）。alt は市松のもう一方。</summary>
        public static Sprite IsoFloorFor(ZoneSpec z, bool alt) =>
            IsoTile($"Floor_{z.Key}_{alt}", alt ? z.FloorAlt : z.Floor, z.Edge, z.Accent, z.Pattern, alt ? 7 : 0);

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
            string key = $"Wall64_{z.Key}";
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

        /// <summary>
        /// 案内役（オリジナルキャラ）。40ドットを、プレイヤーと同じ縮尺（1マス32ドット）で描く。
        /// 黒い影の体（Q）と灰色の毛（F）に陰影を付け、口は口角の上がった笑みの形で、中（K）は真っ暗、上下の歯の間の帯（Y）だけとても暗い赤、上下に象牙色の歯（T）が並ぶ。
        /// </summary>
        public static Sprite Guide => Shaded("Guide40", GuideRows, new Dictionary<char, Color32[]>
        {
            { 'Q', Ramp(44, 40, 52, 28, 24, 34, 19, 15, 25, 12, 8, 16) },
            { 'F', Ramp(112, 100, 114, 82, 72, 86, 60, 52, 64, 44, 38, 48) },
            { 'T', Ramp(255, 255, 250, 244, 238, 222, 230, 222, 202, 212, 202, 180) }, // 歯：左上が白く光る象牙色
            { 'K', Ramp(10, 6, 12, 10, 6, 12, 10, 6, 12, 10, 6, 12) },                 // 口の中（真っ暗）
            { 'Y', Ramp(46, 12, 18, 40, 10, 16, 34, 8, 13, 28, 6, 11) },               // 上下の歯の間の帯（とても暗い赤）
        }, null, 32);

        /// <summary>
        /// 深淵の最奥のボス「アビス」。案内役と同じ姿の色違い：体と毛は白く、口の中の帯は血の色に光り、まわりに赤い光の縁。
        /// </summary>
        public static Sprite GuideAbyss => Shaded("GuideAbyss40", GuideRows, new Dictionary<char, Color32[]>
        {
            { 'Q', Ramp(250, 248, 255, 222, 218, 232, 186, 180, 200, 140, 134, 156) },
            { 'F', Ramp(255, 255, 255, 240, 238, 246, 210, 206, 222, 170, 166, 186) },
            { 'T', Ramp(60, 50, 66, 44, 36, 50, 32, 26, 38, 22, 18, 28) },            // 歯：白い体に映える黒
            { 'K', Ramp(10, 6, 12, 10, 6, 12, 10, 6, 12, 10, 6, 12) },
            { 'Y', new[] { new Color32(255, 40, 60, 255), new Color32(255, 40, 60, 255), new Color32(230, 30, 50, 255), new Color32(200, 20, 40, 255) } }, // 光る血の色
        }, new Color32(255, 40, 70, 255), 32);

        static readonly string[] GuideRows =
        {
            "........................................",
            "..FF................................FF..",
            "..FFFF...........................QFFFF..",
            "...FFFFFQ......................QFFFFF...",
            "...FFFQFFQ....................QFFQFFF...",
            "..QQFFQQFFF..................FFFQQFFQQ..",
            ".FQQQFFQQFFF................FFFQQFFQQQF.",
            ".FFFQQFFQQFFF..............FFFQQFFQQFFF.",
            "..FFFFFFQQQFFF............FFFQQQFFFFFF..",
            "..QFFFQQQQQFFF............FFFQQQQQFFFQ..",
            "...QFFFQQQQQFFF..........FFFQQQQQFFFQ...",
            "..QQQQFFQQQQQFF..........FFQQQQQFFQQQQ..",
            "..FFQQQQQQQQQFF....Q.....FFQQQQQQQQQFF..",
            "..QFFFFQQQQQQFFQ..QFFQQ.QFFQQQQQQFFFFQ..",
            "....FFFFQQQQQQQFFFFFFFFFFQQQQQQQFFFF....",
            ".....QQQQQQFFFQFFFFFFFFFFQFFFQQQQQQ.....",
            "....QQFFQQFFQFFFFFFFFFFFFFFQFFQQFFQQ....",
            "....FFFFFQQQFFFFFFFFFFFFFFFFQQQFFFFF....",
            "......QFQQQFFFQQFFFQFFFFFQFFFQQQFQ......",
            ".......QQFFFFQQFFFFQFQFFFQQFFFFQQ.......",
            "........FFQFFQQFQFQQQQQQFFQQFQFF........",
            ".......QQQFFQQQQQFQQQQQQQFQQFFQQQ.......",
            "........QFFQQQQQQQQQQQQQQQQQQFFQ........",
            ".......QFFQQQQQQQQQQQQQQQQQQQQFFQ.......",
            "......QFFFKKKTQQQQQQQQQQQQTKKKFFFQ......",
            "......QQFFKKKTTKTTKTTKTTKTTKKKFFQQ......",
            ".......QFFQKKKTKTTKTTKTTKTKKKQFFQ.......",
            ".......QQFFQKTYYYYYYYYYYYYTKQFQQF.......",
            "........QFQQQQTKKKKKKKKKKTQQQQFQ........",
            ".........QQQQQQQTTKTTKTTQQQQQQQ.........",
            ".........QFFFFQQQQQQQQQQQQFFFFQ.........",
            "..........QQQFFQQQQQQQQQFFFQQQ..........",
            "...........QQQFFFFQQQQFFFFQQQ...........",
            "..........QFFQQQFFFFFFFFQQQFFQ..........",
            ".........QFQFFFQQQQFFQQQQFFFQF..........",
            ".........QQQFFFFQQQQQQQQFFQFQ...........",
            "..........FFQQQQFFQQQQFFFQQFFF..........",
            "..........FFFQQFFFFQQFFFFQQFFF..........",
            ".........QQQFQQFQQFQQQQQFQQFQQQ.........",
            "........F....FF....FF....FF....F........",
        };

        /// <summary>祭壇（石の台の上に火が灯る）。足元が原点。</summary>
        public static Sprite Altar => Shaded("Altar32", AltarRows, null, null, 32);

        /// <summary>火の消えた祭壇（使ったあと）。炎を消し、器に燃えさしだけ残す。</summary>
        public static Sprite AltarUnlit
        {
            get
            {
                var rows = new string[AltarRows.Length];
                for (int y = 0; y < rows.Length; y++)
                {
                    var r = AltarRows[y].ToCharArray();
                    for (int x = 0; x < r.Length; x++)
                        if (r[x] == 'L' || r[x] == 'l' || r[x] == 'o') r[x] = y >= 11 ? 'n' : '.'; // 器の口は燃えさし（黒）
                    rows[y] = new string(r);
                }
                return Shaded("Altar32Unlit", rows, null, null, 32);
            }
        }


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
                        // ---- 深淵 ----
                        case FloorPattern.Rift: // 黒い岩を走る、赤く光る裂け目（細く少なく、つながった線。芯だけ明るい）
                        {
                            float k = Mathf.Abs(TileNoise(x / 13f, sy / 6.5f, salt + 51) - 0.5f);
                            if (k < 0.004f) c = Mix(accent, new Color32(255, 200, 180, 255), 0.2f);
                            else if (k < 0.009f) c = Mix(c, accent, 0.55f);
                            else if (k < 0.016f) c = Mix(c, accent, 0.12f);
                            break;
                        }
                        case FloorPattern.Starless: // 青い燐光の細い筋（少なく、つながった線）と、まれに光る粒
                        {
                            float k = Mathf.Abs(TileNoise(x / 15f, sy / 7.5f, salt + 61) - 0.5f);
                            if (k < 0.004f) c = Mix(accent, new Color32(220, 255, 255, 255), 0.25f);
                            else if (k < 0.009f) c = Mix(c, accent, 0.5f);
                            else if (k < 0.016f) c = Mix(c, accent, 0.12f);
                            if (Hash(x + salt * 3, sy * 5) % 331 == 0) c = Mix(c, new Color32(220, 255, 255, 255), 0.7f);
                            break;
                        }
                        case FloorPattern.Rune: // 敷石ごとに、紫に燃える丸い紋様（輪と十字）
                        {
                            float ra = fa - 0.5f, rb = fb - 0.5f;
                            float r = Mathf.Sqrt(ra * ra + rb * rb);
                            bool rune = (Hash(stoneId, salt + 71) % 5) == 0;
                            if (rune && !joint && (Mathf.Abs(r - 0.3f) < 0.035f || (r < 0.26f && (Mathf.Abs(ra) < 0.025f || Mathf.Abs(rb) < 0.025f))))
                                c = Mix(accent, new Color32(255, 230, 255, 255), 0.2f);
                            else if (rune && r < 0.36f) c = Mix(c, accent, 0.14f);
                            if (joint) c = Mix(mortar, accent, 0.25f);
                            break;
                        }
                        case FloorPattern.Throne: // 目地に金、ところどころ血の色の裂け目
                        {
                            if (joint) c = Mix(accent, mortar, 0.7f); // 目地の金は控えめに
                            float k = Mathf.Abs(TileNoise(x / 13f, sy / 6.5f, salt + 81) - 0.5f);
                            if (!joint && k < 0.005f) c = new Color32(220, 40, 50, 255);
                            else if (!joint && k < 0.011f) c = Mix(c, new Color32(220, 40, 50, 255), 0.35f);
                            break;
                        }
                        // ---- 天界 ----
                        case FloorPattern.Cloud: // 白い敷石に、雲のようなやわらかい明るみ。目地は淡い金
                        {
                            float k = TileNoise(x / 10f, sy / 5f, salt + 91);
                            if (!joint && k > 0.62f) c = Mix(c, new Color32(255, 255, 255, 255), (k - 0.62f) * 1.6f);
                            if (joint) c = Mix(mortar, accent, 0.35f);
                            break;
                        }
                        case FloorPattern.Garden: // 散った花びら（区画の色）と、ところどころ若葉の色
                            if (!joint && Hash(x + salt * 7, sy * 3) % 97 == 0) c = accent;
                            else if (!joint && Hash(x * 3 + salt, sy + 5) % 211 == 0) c = new Color32(150, 220, 140, 255);
                            if (joint) c = Mix(mortar, new Color32(150, 200, 140, 255), 0.25f);
                            break;
                        case FloorPattern.Choir: // 目地に金の線、敷石の中ほどに細い金の五線のような筋
                            if (joint) c = Mix(accent, mortar, 0.25f);
                            else if (Mathf.Abs(fb - 0.5f) < 0.03f && (Hash(stoneId, salt + 95) % 3) == 0) c = Mix(c, accent, 0.5f);
                            break;
                        case FloorPattern.Sanctum: // 敷石ごとに、淡い金の光輪
                        {
                            float ra = fa - 0.5f, rb = fb - 0.5f;
                            float r = Mathf.Sqrt(ra * ra + rb * rb);
                            bool halo = (Hash(stoneId, salt + 99) % 4) == 0;
                            if (halo && !joint && Mathf.Abs(r - 0.3f) < 0.03f) c = Mix(c, accent, 0.6f);
                            if (joint) c = Mix(mortar, accent, 0.4f);
                            break;
                        }
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
