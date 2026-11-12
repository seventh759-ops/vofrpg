using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// タイトル画面の背景（180×320 ドット）を、起動時に一度だけ計算で描く。
    /// 霧の洞窟・遠くの岩・天井の鍾乳石・中央に大きく沈む逆さの塔（円柱の陰影、窓、バルコニー、つらら、螺旋階段）・
    /// 手前の霧と四隅の暗がり。
    /// 色は素材ごとの段階（ramp）から選び、段階の間はディザ（市松に混ぜる）でつなぐ。
    /// </summary>
    public static class TitleScene
    {
        public const int W = 180, H = 320;
        public const int TowerX = 90, TowerTop = 0;
        public const int SpireTip = 295;

        static Texture2D texture;
        static Color32[] img;

        /// <summary>背景の絵。</summary>
        public static Texture2D Texture
        {
            get
            {
                if (texture == null) Build();
                return texture;
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

        static Color32[] Ramp(params int[] v)
        {
            var r = new Color32[v.Length / 3];
            for (int i = 0; i < r.Length; i++) r[i] = new Color32((byte)v[i * 3], (byte)v[i * 3 + 1], (byte)v[i * 3 + 2], 255);
            return r;
        }

        static readonly Color32[] Cave = Ramp(8, 8, 13, 14, 15, 22, 22, 24, 33, 33, 36, 47, 47, 51, 64, 64, 69, 83, 84, 90, 104, 106, 113, 127, 130, 137, 150);
        static readonly Color32[] Stone = Ramp(13, 13, 19, 24, 24, 33, 36, 36, 48, 50, 50, 64, 66, 66, 82, 84, 85, 102, 106, 108, 126, 132, 134, 152);
        static readonly Color32[] Warm = Ramp(80, 34, 12, 150, 70, 20, 220, 130, 40, 255, 190, 90, 255, 232, 170);

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

        /// <summary>その場所の霧の明るさ（中ほど右が明るい）。</summary>
        static float Mist(int x, int y)
        {
            float dx = (x - 90) / 100f, dy = (y - 150) / 130f;
            float v = Mathf.Exp(-(dx * dx + dy * dy) * 1.1f) * 0.85f + 0.12f * Mathf.Exp(-Sq((y - 150) / 90f));
            v += (Fbm(x / 40f, y / 25f, 7) - 0.5f) * 0.12f;
            return v * 0.72f;
        }

        static float Sq(float v) => v * v;

        static void Put(int x, int y, Color32[] ramp, float v, float fog = 0f)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            var c = Pick(ramp, v, x, y);
            if (fog > 0f)
            {
                var m = Pick(Cave, Mist(x, y), x, y);
                float q = Mathf.Floor(fog * 4f + Bay(x, y)) / 4f;
                c = Mix(c, m, Mathf.Min(1f, q));
            }
            img[y * W + x] = c;
        }

        // ---- 組み立て ----

        static void Build()
        {
            img = new Color32[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    img[y * W + x] = Pick(Cave, Mist(x, y), x, y);

            Silhouette(11, true, 52, 14, 40, 0.42f, 0.62f);
            Silhouette(12, false, 70, 16, 34, 0.40f, 0.62f);
            Silhouette(13, true, 30, 10, 28, 0.25f, 0.3f);
            Ceiling();
            Stairs(false);
            Tower();
            Stairs(true);
            FogAndVignette();

            // テクスチャは下から上へ並ぶので、上下を入れ替える
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    px[(H - 1 - y) * W + x] = img[y * W + x];
            texture = new Texture2D(W, H, TextureFormat.RGBA32, false)
            {
                name = "TitleScene",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(px);
            texture.Apply(false, true);
            img = null;
        }

        /// <summary>遠くの岩の影（霧に霞む）。right=true なら右から、false なら左から張り出す。</summary>
        static void Silhouette(int seed, bool right, float baseW, float amp, float scale, float v, float fog)
        {
            for (int y = 40; y < H; y++)
            {
                float n = Fbm(y / scale, seed, seed);
                float edge = baseW + amp * (n - 0.5f) * 2f;
                int x0 = right ? (int)(W - edge) : 0, x1 = right ? W : (int)edge;
                for (int x = x0; x < x1; x++)
                {
                    float tex = Fbm(x / 10f, y / 10f, seed + 3);
                    Put(x, y, Stone, v + 0.12f * (tex - 0.5f), fog);
                }
            }
        }

        static void Ceiling()
        {
            const int count = 22;
            var sx = new int[count];
            var sw = new float[count];
            var sl = new float[count];
            for (int i = 0; i < count; i++)
            {
                sx[i] = (int)(Rnd(i, 1, 5) * W);
                sw[i] = 4f + Rnd(i, 2, 5) * 9f;
                sl[i] = 10f + Rnd(i, 3, 5) * 38f;
            }
            for (int x = 0; x < W; x++)
            {
                float b = 10f + 8f * Fbm(x / 14f, 0, 9);
                for (int i = 0; i < count; i++)
                {
                    float d = Mathf.Abs(x - sx[i]);
                    if (d < sw[i]) b = Mathf.Max(b, 10f + sl[i] * Mathf.Pow(1f - d / sw[i], 1.3f));
                }
                for (int y = 0; y < (int)b; y++)
                {
                    float k = y / Mathf.Max(1f, b);
                    Put(x, y, Stone, 0.12f + 0.18f * k + 0.1f * (Fbm(x / 6f, y / 6f, 4) - 0.5f), 0.15f * k);
                }
            }
        }

        // ---- 逆さの塔 ----

        /// <summary>その高さでの塔の半径（上の大きな胴 → バルコニー → 細くなる本体 → 胸壁 → 尖塔）。</summary>
        public static float TowerRadius(float y)
        {
            if (y < TowerTop) return 0f;
            if (y < 120) return 42f;
            if (y < 127) return 48f;
            if (y < 240) return 37f - (y - 127) * 0.11f;
            if (y < 247) return 28f;
            if (y < SpireTip) return 23f * Mathf.Pow(1f - (y - 247) / 48f, 1.15f);
            return 0f;
        }

        static float TowerFog(float y) => 0.06f + Mathf.Min(0.9f, 0.9f * Mathf.Max(0f, (60 - y) / 50f)) + 0.2f * Mathf.Max(0f, (y - 250) / 40f);

        /// <summary>窓（塔の正面からの位置 -1〜1、上端の高さ、灯っているか）。</summary>
        public static readonly (float u, int y, bool lit)[] Windows =
        {
            (-0.3f, 60, true), (0.25f, 60, true), (-0.5f, 85, true), (0f, 85, true), (0.5f, 85, false),
            (-0.2f, 104, true), (0.35f, 104, true), (-0.5f, 142, true), (0f, 142, true), (0.45f, 142, false),
            (-0.25f, 166, true), (0.25f, 166, false), (-0.4f, 190, false), (0.1f, 190, true), (-0.1f, 214, false), (0.35f, 214, false),
        };

        static void Tower()
        {
            for (int y = TowerTop; y < SpireTip + 2; y++)
            {
                float r = TowerRadius(y);
                if (r <= 0.5f) continue;
                bool balcony = y >= 120 && y < 127, parapet = y >= 240 && y < 247;
                for (int x = (int)(TowerX - r); x <= (int)(TowerX + r); x++)
                {
                    float u = (x + 0.5f - TowerX) / r;
                    if (Mathf.Abs(u) > 1f) continue;
                    float nz = Mathf.Sqrt(1f - u * u);
                    float diff = 0.22f + 0.62f * Mathf.Max(0f, -0.55f * u + 0.83f * nz);
                    float rim = Mathf.Max(0f, (u - 0.72f) / 0.28f) * 0.35f;
                    float v = diff + rim;
                    float theta = Mathf.Asin(u);
                    int row;
                    if (y >= 247)
                    {
                        v -= 0.08f * (1f + u);
                        row = y / 4;
                    }
                    else row = (y - TowerTop) / 5;
                    float cf = theta / Mathf.PI * 10f + (row % 2) * 0.5f;
                    int col = Mathf.FloorToInt(cf);
                    bool mortar = ((y - TowerTop) % 5 == 0 && y < 247) || Mathf.Abs(cf - Mathf.Round(cf)) < 0.06f / Mathf.Max(nz, 0.25f);
                    v += (Rnd(col, row, 21) - 0.5f) * 0.14f + (Fbm(x / 3f, y / 3f, 22) - 0.5f) * 0.06f;
                    v -= 0.5f * Mathf.Max(0f, (60 - y) / 50f);
                    if (mortar && !(balcony || parapet)) v -= 0.16f;
                    if (balcony) v = 0.62f * diff + rim + (y == 120 ? 0.12f : y == 126 ? -0.15f : 0f);
                    if (parapet)
                    {
                        bool notch = Mathf.FloorToInt(theta / Mathf.PI * 12f) % 2 == 0 && y >= 245;
                        if (notch) continue;
                        v = diff * 0.9f + rim;
                    }

                    bool window = false;
                    foreach (var w in Windows)
                    {
                        float wx = TowerX + w.u * r;
                        if (y >= w.y && y < w.y + 8 && Mathf.Abs(x - wx) <= 1.6f && Mathf.Abs(w.u) < 0.75f)
                        {
                            bool arch = y == w.y && Mathf.Abs(x - wx) > 0.9f;
                            if (arch) continue;
                            if (w.lit) Put(x, y, Warm, 0.55f + 0.4f * (1f - (y - w.y) / 7f));
                            else Put(x, y, Stone, 0.05f, 0.1f);
                            window = true;
                            break;
                        }
                    }
                    if (!window) Put(x, y, Stone, v, TowerFog(y));
                }
            }
            // バルコニー下のつらら
            for (int i = 0; i < 14; i++)
            {
                int sx = (int)(TowerX - 46 + Rnd(i, 7, 30) * 92);
                float len = 3f + Rnd(i, 8, 30) * 12f;
                for (int y = 127; y < 127 + (int)len; y++)
                {
                    int w = y > 127 + len * 0.4f ? 1 : 2;
                    for (int x = sx; x < sx + w; x++) Put(x, y, Stone, 0.3f - 0.1f * (y - 127) / len, TowerFog(y));
                }
            }
            // 胸壁下のつらら
            for (int i = 0; i < 8; i++)
            {
                int sx = (int)(TowerX - 26 + Rnd(i, 9, 31) * 52);
                float len = 2f + Rnd(i, 10, 31) * 8f;
                for (int y = 247; y < 247 + (int)len; y++) Put(sx, y, Stone, 0.25f, TowerFog(y));
            }
        }

        /// <summary>塔をめぐる螺旋階段。front=false で塔の後ろ側、true で手前側の段だけを描く。</summary>
        static void Stairs(bool front)
        {
            const int ys = 130, length = 112;
            const float pitch = 92f;
            for (int s = 0; s < length; s++)
            {
                float a = 0.6f * Mathf.PI + s / pitch * 2f * Mathf.PI;
                float depth = Mathf.Sin(a);
                if ((depth > 0f) != front) continue;
                int y = ys + s;
                float r = TowerRadius(y) + 7f;
                float cx = TowerX + Mathf.Cos(a) * r;
                for (int dx = -4; dx <= 4; dx++)
                    for (int dy = 0; dy < 4; dy++)
                    {
                        int xx = (int)(cx + dx), yy = (int)(y + dy + depth * 3f);
                        bool top = dy == 0;
                        float v = (top ? 0.7f : 0.22f) + 0.2f * Mathf.Max(0f, -Mathf.Cos(a)) + (front ? 0.05f : -0.15f);
                        if (s % 3 == 0 && dx == -4) v -= 0.15f;
                        Put(xx, yy, Stone, v, TowerFog(y) * 0.6f + (front ? 0f : 0.12f));
                    }
            }
        }

        /// <summary>手前を漂う霧と、四隅の暗がり。</summary>
        static void FogAndVignette()
        {
            var dark = new Color32(5, 5, 9, 255);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float f = Fbm(x / 50f, y / 12f, 70);
                    float band = Mathf.Exp(-Sq((y - 260) / 40f)) * 0.6f + Mathf.Exp(-Sq((y - 150) / 30f)) * 0.25f;
                    float fog = Mathf.Max(0f, f - 0.45f) * 1.6f * band;
                    float vig = Sq((x - W / 2f) / (W * 0.65f)) + Sq((y - H * 0.45f) / (H * 0.62f));
                    var c = img[y * W + x];
                    if (fog > 0f)
                    {
                        var m = Pick(Cave, Mist(x, y) + 0.1f, x, y);
                        float q = Mathf.Floor(fog * 4f + Bay(x, y)) / 4f;
                        c = Mix(c, m, Mathf.Min(1f, q));
                    }
                    if (vig > 0.4f)
                    {
                        float q = Mathf.Floor(Mathf.Min(1f, (vig - 0.4f) * 1.3f) * 4f + Bay(x, y)) / 4f;
                        c = Mix(c, dark, q * 0.85f);
                    }
                    img[y * W + x] = c;
                }
        }
    }
}
