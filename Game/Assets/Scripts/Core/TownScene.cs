using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 町の画面の背景（180×320 ドット）を、初めて使うときに一度だけ計算で描く。
    /// 夜の町：星と月の空、遠くの山並み、真ん中に塔の頭（胸壁と、灯りのもれる大きな門）、
    /// 両側に灯の点る家並み（切妻屋根・木組み・煙突）、石畳の広場、街灯と光だまり。
    /// 町の画面ではボタンが上半分に並ぶので、塔の門と家並みは下半分に置く。
    /// 色は素材ごとの段階（ramp）から選び、段階の間はディザ（市松に混ぜる）でつなぐ。
    /// </summary>
    public static class TownScene
    {
        public const int W = 180, H = 320;
        public const int GateX = 90, GateY = 250;   // 塔の門の下端の中央（画面の下半分、ボタンの下の空いた所に来るように）
        const int Ground = 254;                      // 広場の地面が始まる高さ
        public const int MoonX = 138, MoonY = 34;

        /// <summary>街灯の灯りの位置（ゆらめきの光を重ねるため）。</summary>
        public static readonly Vector2Int[] Lamps = { new Vector2Int(20, 262), new Vector2Int(160, 262), new Vector2Int(52, 290), new Vector2Int(128, 290) };

        /// <summary>灯った窓の位置（ゆらめきの光を重ねるため）。</summary>
        public static readonly System.Collections.Generic.List<Vector2Int> LitWindows = new System.Collections.Generic.List<Vector2Int>();

        static Texture2D texture, abyssTexture;
        static Color32[] img;
        static bool abyss; // いま描いているのが深淵版か

        /// <summary>町の絵（ふつう）。</summary>
        public static Texture2D Texture
        {
            get
            {
                if (texture == null) texture = Build(false);
                return texture;
            }
        }

        /// <summary>町の絵（深淵版：血の色の空と赤い月、赤紫に灯る門と窓、地面を走る光る裂け目）。深淵へ行けるようになったら使う。</summary>
        public static Texture2D AbyssTexture
        {
            get
            {
                if (abyssTexture == null) abyssTexture = Build(true);
                return abyssTexture;
            }
        }

        // ---- 乱数・ノイズ・ディザ ----

        static uint Hash(int x, int y, int s)
        {
            unchecked
            {
                uint h = (uint)x * 73856093u ^ (uint)y * 19349663u ^ (uint)s * 83492791u;
                h ^= h >> 13;
                h *= 0x5bd1e995u;
                h ^= h >> 15;
                return h;
            }
        }

        static float Rnd(int x, int y, int s) => (Hash(x, y, s) & 0xffff) / 65535f;

        static float VNoise(float x, float y, int s)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Rnd(xi, yi, s), b = Rnd(xi + 1, yi, s), c = Rnd(xi, yi + 1, s), d = Rnd(xi + 1, yi + 1, s);
            float top = a + (b - a) * fx, bottom = c + (d - c) * fx;
            return top + (bottom - top) * fy;
        }

        static float Fbm(float x, float y, int s, int octaves = 4)
        {
            float v = 0f, amp = 0.5f, f = 1f;
            for (int i = 0; i < octaves; i++)
            {
                v += amp * VNoise(x * f, y * f, s + i);
                f *= 2f;
                amp *= 0.5f;
            }
            return v / 0.9375f;
        }

        static readonly int[] bayer = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
        static float Bay(int x, int y) => (bayer[(y & 3) * 4 + (x & 3)] + 0.5f) / 16f;
        static float Sq(float v) => v * v;

        static Color32[] Ramp(params int[] v)
        {
            var r = new Color32[v.Length / 3];
            for (int i = 0; i < r.Length; i++) r[i] = new Color32((byte)v[i * 3], (byte)v[i * 3 + 1], (byte)v[i * 3 + 2], 255);
            return r;
        }

        // 素材ごとの色の段階（暗い → 明るい）
        static readonly Color32[] SkyN = Ramp(6, 6, 18, 10, 11, 28, 16, 16, 40, 24, 22, 54, 34, 28, 66, 48, 34, 78, 66, 42, 86, 90, 52, 92);
        static readonly Color32[] HillN = Ramp(8, 8, 18, 13, 13, 28, 19, 19, 38, 26, 25, 48);
        static readonly Color32[] StoneN = Ramp(12, 12, 20, 22, 21, 32, 34, 32, 46, 48, 45, 60, 64, 60, 76, 82, 77, 92, 104, 98, 112);
        static readonly Color32[] PlasterN = Ramp(26, 22, 32, 42, 36, 46, 60, 52, 60, 80, 70, 74, 104, 92, 90, 132, 118, 110);
        static readonly Color32[] TimberN = Ramp(16, 10, 10, 28, 18, 16, 42, 28, 22, 58, 40, 30);
        static readonly Color32[] RoofN = Ramp(14, 10, 20, 24, 16, 30, 36, 22, 40, 50, 30, 50, 66, 40, 60);
        static readonly Color32[] WarmN = Ramp(70, 30, 12, 140, 66, 20, 214, 120, 40, 255, 180, 80, 255, 226, 160, 255, 246, 220);
        static readonly Color32[] CobbleN = Ramp(10, 10, 16, 18, 17, 26, 28, 26, 36, 40, 37, 48, 54, 50, 60, 70, 64, 72, 92, 80, 80, 120, 100, 86);
        static readonly Color32[] MoonN = Ramp(120, 120, 150, 180, 180, 200, 225, 225, 235, 250, 250, 252);

        // 深淵版の色
        static readonly Color32[] SkyA = Ramp(8, 2, 8, 16, 4, 14, 26, 6, 22, 38, 8, 30, 54, 10, 38, 72, 12, 44, 94, 16, 48, 120, 22, 52);
        static readonly Color32[] HillA = Ramp(8, 3, 8, 14, 5, 12, 20, 7, 16, 28, 9, 22);
        static readonly Color32[] StoneA = Ramp(10, 6, 12, 18, 11, 22, 28, 16, 32, 40, 22, 44, 54, 30, 58, 70, 38, 72, 88, 48, 88);
        static readonly Color32[] PlasterA = Ramp(22, 14, 24, 36, 22, 36, 52, 32, 48, 70, 42, 60, 90, 54, 72, 112, 68, 84);
        static readonly Color32[] TimberA = Ramp(12, 6, 10, 22, 10, 16, 34, 16, 22, 48, 22, 28);
        static readonly Color32[] RoofA = Ramp(16, 4, 12, 28, 8, 20, 42, 12, 28, 58, 16, 34, 76, 22, 40);
        static readonly Color32[] WarmA = Ramp(70, 6, 22, 140, 16, 44, 220, 34, 74, 255, 90, 130, 255, 170, 200, 255, 225, 235);
        static readonly Color32[] CobbleA = Ramp(8, 4, 10, 14, 8, 18, 22, 12, 26, 32, 16, 34, 44, 22, 44, 58, 28, 54, 74, 34, 62, 92, 42, 70);
        static readonly Color32[] MoonA = Ramp(90, 10, 20, 160, 20, 40, 220, 50, 70, 255, 120, 130);

        static Color32[] Sky => abyss ? SkyA : SkyN;
        static Color32[] Hill => abyss ? HillA : HillN;
        static Color32[] Stone => abyss ? StoneA : StoneN;
        static Color32[] Plaster => abyss ? PlasterA : PlasterN;
        static Color32[] Timber => abyss ? TimberA : TimberN;
        static Color32[] Roof => abyss ? RoofA : RoofN;
        static Color32[] Warm => abyss ? WarmA : WarmN;
        static Color32[] Cobble => abyss ? CobbleA : CobbleN;
        static Color32[] Moon => abyss ? MoonA : MoonN;

        static Color32 Pick(Color32[] ramp, float v, int x, int y)
        {
            int n = ramp.Length;
            float f = Mathf.Clamp01(v) * (n - 1);
            int i = (int)f;
            if (f - i > Bay(x, y) && i < n - 1) i++;
            return ramp[i];
        }

        static Color32 Mix(Color32 a, Color32 b, float t) =>
            new Color32((byte)(a.r + (b.r - a.r) * t), (byte)(a.g + (b.g - a.g) * t), (byte)(a.b + (b.b - a.b) * t), 255);

        static void Put(int x, int y, Color32[] ramp, float v)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            img[y * W + x] = Pick(ramp, v, x, y);
        }

        /// <summary>灯りの明るさ（窓・街灯・門の光が、近くの壁や地面を暖かく照らす）。</summary>
        static float WarmLight(int x, int y)
        {
            float v = 0.6f * Mathf.Exp(-(Sq((x - GateX) / 26f) + Sq((y - GateY) / 18f))); // 門の光は控えめに（ボタンより目立たないように）
            foreach (var l in Lamps) v += 0.75f * Mathf.Exp(-(Sq((x - l.x) / 15f) + Sq((y - l.y - 10) / 9f)));
            return v;
        }

        /// <summary>暖かい光を足す（光の強さに応じて、暖色へ寄せる）。</summary>
        static void Warmth(int x, int y, float amount)
        {
            if (amount <= 0.02f || x < 0 || y < 0 || x >= W || y >= H) return;
            var c = img[y * W + x];
            var w = Pick(Warm, 0.25f + amount * 0.5f, x, y);
            float q = Mathf.Floor(Mathf.Min(1f, amount) * 4f + Bay(x, y)) / 4f;
            img[y * W + x] = Mix(c, w, q * 0.55f);
        }

        // ---- 組み立て ----

        static Texture2D Build(bool abyssVersion)
        {
            abyss = abyssVersion;
            img = new Color32[W * H];
            LitWindows.Clear();
            SkyAndStars();
            Hills();
            Plaza();     // 地面を先に描き、その上に塔と家を描く
            TowerTop();
            Houses();
            Lampposts();
            Vignette();

            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    px[(H - 1 - y) * W + x] = img[y * W + x];
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
            {
                name = abyss ? "TownSceneAbyss" : "TownScene",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            img = null;
            abyss = false;
            return tex;
        }

        static void SkyAndStars()
        {
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float v = Mathf.Clamp01(y / 230f) * 0.85f + (Fbm(x / 30f, y / 14f, 3) - 0.5f) * 0.12f;
                    float moon = Mathf.Exp(-(Sq((x - MoonX) / 40f) + Sq((y - MoonY) / 32f))) * 0.3f;
                    Put(x, y, Sky, v + moon);
                    if (y < 200 && Rnd(x, y, 5) > 0.992f) Put(x, y, Moon, 0.3f + Rnd(x, y, 6) * 0.7f * (1f - y / 210f));
                }
            // 月（左上に光が当たる、少し欠けた月）
            for (int y = MoonY - 11; y <= MoonY + 11; y++)
                for (int x = MoonX - 11; x <= MoonX + 11; x++)
                {
                    float d = Mathf.Sqrt(Sq(x - MoonX) + Sq(y - MoonY));
                    if (d > 9.5f) continue;
                    float cut = Mathf.Sqrt(Sq(x - MoonX - 5) + Sq(y - MoonY + 3));
                    if (cut < 8.5f) continue;
                    Put(x, y, Moon, 0.55f + 0.45f * (1f - d / 9.5f) + (Fbm(x / 3f, y / 3f, 8) - 0.5f) * 0.3f);
                }
        }

        static void Hills()
        {
            for (int x = 0; x < W; x++)
            {
                float far = 194f + 18f * Fbm(x / 40f, 0f, 11);
                float near = 212f + 14f * Fbm(x / 26f, 1f, 12);
                for (int y = (int)far; y < H; y++) Put(x, y, Hill, 0.25f + 0.2f * (y - far) / 40f);
                for (int y = (int)near; y < H; y++) Put(x, y, Hill, 0.55f + 0.3f * (Fbm(x / 8f, y / 8f, 13) - 0.5f));
            }
        }

        /// <summary>塔の頭：太い石の円柱と胸壁、灯りのもれる大きな門、両脇の旗。</summary>
        static void TowerTop()
        {
            const float r = 40f;
            const int top = 162, bottom = Ground;
            for (int y = top - 8; y < bottom; y++)
                for (int x = (int)(GateX - r) - 1; x <= (int)(GateX + r) + 1; x++)
                {
                    float u = (x + 0.5f - GateX) / r;
                    if (Mathf.Abs(u) > 1f) continue;
                    if (y < top)
                    {
                        // 胸壁
                        if (Mathf.FloorToInt((u + 1f) * 7f) % 2 == 1) continue;
                    }
                    float nz = Mathf.Sqrt(1f - u * u);
                    float diff = 0.2f + 0.6f * Mathf.Max(0f, 0.45f * u + 0.85f * nz); // 月の光（右上から）
                    int row = (y - top + 8) / 5;
                    float cf = Mathf.Asin(u) / Mathf.PI * 9f + (row % 2) * 0.5f;
                    bool mortar = (y - top + 8) % 5 == 0 || Mathf.Abs(cf - Mathf.Round(cf)) < 0.05f / Mathf.Max(nz, 0.25f);
                    float v = diff + (Rnd(Mathf.FloorToInt(cf), row, 21) - 0.5f) * 0.12f - (mortar ? 0.14f : 0f);
                    if (y < top + 4 && y >= top) v -= 0.18f; // 胸壁の下の影
                    Put(x, y, Stone, v);
                    Warmth(x, y, WarmLight(x, y) * 0.8f);
                }
            // 門（アーチ）と、奥へ下る階段の灯り
            for (int y = GateY - 40; y < GateY; y++)
                for (int x = GateX - 15; x <= GateX + 15; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - GateX);
                    float archTop = GateY - 26 - Mathf.Sqrt(Mathf.Max(0f, 225f - dx * dx)) * 0.9f;
                    if (y < archTop - 3) continue;
                    if (y < archTop || dx > 12.5f) { Put(x, y, Stone, 0.55f + (y < archTop - 1 ? 0.1f : -0.1f)); continue; } // 縁石
                    float depth = (y - archTop) / (GateY - archTop);
                    bool step = (y - (int)archTop) % 4 == 0 && depth > 0.35f;
                    Put(x, y, Warm, 0.68f - depth * 0.4f - dx * 0.025f - (step ? 0.25f : 0f));
                }
            // 門の両脇の旗（深い赤に金の縁）
            foreach (int bx in new[] { GateX - 24, GateX + 20 })
                for (int y = 194; y < 226; y++)
                    for (int x = bx; x < bx + 5; x++)
                    {
                        bool tail = y > 220 && Mathf.Abs(x - bx - 2) < (y - 220) * 0.5f;
                        if (tail) continue;
                        bool edge = x == bx || x == bx + 4;
                        var c = edge ? Pick(Warm, 0.5f, x, y) : Mix(new Color32(110, 20, 30, 255), new Color32(60, 10, 18, 255), (y - 194) / 32f);
                        img[y * W + x] = c;
                    }
        }

        /// <summary>家並み：両側に、切妻屋根・木組み・灯の点る窓・煙突の家。</summary>
        static void Houses()
        {
            // (左端, 幅, 壁の上端, 地面の高さ, 屋根の高さ)
            var houses = new (int x, int w, int top, int ground, int roof)[]
            {
                (-6, 30, 222, 268, 20), (20, 26, 230, 266, 16), (40, 22, 238, 264, 13),
                (140, 26, 230, 266, 16), (158, 30, 222, 268, 20), (126, 20, 238, 264, 13),
            };
            foreach (var hs in houses)
            {
                float cx = hs.x + hs.w * 0.5f;
                // 屋根
                for (int y = hs.top - hs.roof; y < hs.top + 2; y++)
                    for (int x = hs.x - 2; x < hs.x + hs.w + 2; x++)
                    {
                        float half = (y - (hs.top - hs.roof)) / (float)hs.roof * (hs.w * 0.5f + 2f);
                        if (Mathf.Abs(x + 0.5f - cx) > half) continue;
                        bool lit = x + 0.5f > cx; // 月の光は右から
                        bool shingle = (y + (x / 3) % 2) % 3 == 0;
                        Put(x, y, Roof, (lit ? 0.55f : 0.3f) - (shingle ? 0.12f : 0f) + (Rnd(x, y, 31) - 0.5f) * 0.1f);
                    }
                // 煙突
                int chx = (int)(cx + hs.w * 0.22f);
                for (int y = hs.top - hs.roof - 4; y < hs.top - hs.roof + 8; y++)
                    for (int x = chx; x < chx + 4; x++) Put(x, y, Stone, x == chx + 3 ? 0.25f : 0.4f);
                // 壁（漆喰と木組み）
                for (int y = hs.top; y < hs.ground; y++)
                    for (int x = hs.x; x < hs.x + hs.w; x++)
                    {
                        int lx = x - hs.x, ly = y - hs.top;
                        bool beam = lx == 0 || lx == hs.w - 1 || ly == 0 || ly == (hs.ground - hs.top) / 2 || (lx % 9 == 0);
                        float shade = 0.5f + 0.12f * ((x + 0.5f - cx) / hs.w) + (Fbm(x / 4f, y / 4f, 33) - 0.5f) * 0.1f;
                        if (beam) Put(x, y, Timber, shade);
                        else Put(x, y, Plaster, shade);
                        Warmth(x, y, WarmLight(x, y) * 0.6f);
                    }
                // 窓（ほとんど灯っている）と扉
                int floors = (hs.ground - hs.top) / 2;
                for (int f = 0; f < 2; f++)
                {
                    int wy = hs.top + 4 + f * floors;
                    for (int k = 0; k < 3; k++)
                    {
                        int wx = hs.x + 4 + k * 9;
                        if (wx + 3 >= hs.x + hs.w - 1) break;
                        if (f == 1 && k == 1) continue; // 扉の場所
                        bool lit = Rnd(wx, wy, 41) > 0.2f;
                        for (int y = wy; y < wy + 5; y++)
                            for (int x = wx; x < wx + 4; x++)
                            {
                                bool frame = x == wx + 1 && y == wy + 2;
                                if (lit) Put(x, y, Warm, frame ? 0.4f : 0.65f + 0.25f * (1f - (y - wy) / 5f));
                                else Put(x, y, Sky, frame ? 0.05f : 0.2f);
                            }
                        if (lit)
                        {
                            LitWindows.Add(new Vector2Int(wx + 2, wy + 2));
                            for (int x = wx - 1; x < wx + 5; x++) Put(x, wy + 5, Timber, 0.7f); // 窓台
                        }
                    }
                }
                int dx = hs.x + hs.w / 2 - 3;
                for (int y = hs.ground - 9; y < hs.ground; y++)
                    for (int x = dx; x < dx + 6; x++)
                        Put(x, y, Timber, y == hs.ground - 9 || x == dx || x == dx + 5 ? 0.25f : 0.45f + ((x - dx) % 2) * 0.08f);
            }
        }

        /// <summary>石畳の広場：手前ほど大きな敷石、光だまり。</summary>
        static void Plaza()
        {
            for (int y = Ground - 4; y < H; y++)
            {
                float t = (y - Ground + 4) / (float)(H - Ground + 4);
                float rowH = 2f + t * 9f;
                float rowF = Mathf.Pow(t, 0.7f) * 16f;
                int row = Mathf.FloorToInt(rowF);
                float fy = rowF - row;
                for (int x = 0; x < W; x++)
                {
                    float cw = 4f + t * 14f;
                    float colF = (x - GateX) / cw + (row % 2) * 0.5f;
                    int col = Mathf.FloorToInt(colF);
                    float fx = colF - col;
                    bool joint = fy < 0.12f || fx < 0.08f;
                    float v = 0.32f + 0.18f * t + (Rnd(col, row, 51) - 0.5f) * 0.16f - (joint ? 0.2f : 0f);
                    if (fy > 0.75f && !joint) v -= 0.06f;
                    Put(x, y, Cobble, v);
                    Warmth(x, y, WarmLight(x, y));
                    // 深淵版：石畳を走る、赤紫に光る裂け目（門から広がる）
                    if (abyss && Mathf.Abs(Fbm(x / 14f, y / 7f, 61, 2) - 0.5f) < 0.004f + 0.01f * Mathf.Exp(-Sq((x - GateX) / 30f))) Put(x, y, Warm, 0.55f + 0.3f * t);
                }
            }
            // 門から広場へ伸びる光の道
            for (int y = GateY; y < Mathf.Min(H, GateY + 50); y++)
                for (int x = GateX - 14; x <= GateX + 14; x++)
                {
                    float spread = 12f + (y - GateY) * 0.5f;
                    float a = Mathf.Max(0f, 1f - Mathf.Abs(x - GateX) / spread) * (1f - (y - GateY) / 50f);
                    Warmth(x, y, a * 0.6f);
                }
        }

        /// <summary>街灯：細い柱、腕、ガラスの灯り。</summary>
        static void Lampposts()
        {
            foreach (var l in Lamps)
            {
                int baseY = l.y + 22;
                for (int y = l.y + 3; y < baseY; y++) { Put(l.x, y, Stone, 0.2f); Put(l.x + 1, y, Stone, 0.35f); }
                for (int x = l.x - 2; x < l.x + 4; x++) Put(x, baseY, Stone, 0.3f);
                for (int y = l.y - 3; y < l.y + 3; y++)
                    for (int x = l.x - 2; x < l.x + 4; x++)
                    {
                        bool frame = y == l.y - 3 || y == l.y + 2 || x == l.x - 2 || x == l.x + 3;
                        if (frame) Put(x, y, Stone, 0.15f);
                        else Put(x, y, Warm, 0.95f - Mathf.Abs(y - l.y) * 0.08f);
                    }
                Put(l.x, l.y - 4, Stone, 0.15f); Put(l.x + 1, l.y - 4, Stone, 0.15f);
            }
        }

        static void Vignette()
        {
            var dark = new Color32(4, 4, 10, 255);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float vig = Sq((x - W / 2f) / (W * 0.62f)) + Sq((y - H * 0.6f) / (H * 0.62f));
                    if (vig <= 0.45f) continue;
                    float q = Mathf.Floor(Mathf.Min(1f, (vig - 0.45f) * 1.4f) * 4f + Bay(x, y)) / 4f;
                    img[y * W + x] = Mix(img[y * W + x], dark, q * 0.8f);
                }
        }
    }
}
