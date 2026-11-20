using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 町の画面の真ん中に大きく出す、自分のキャラクターの絵（立ち姿）。
    /// 絵は素材の文字を並べた行（テキストアート）で描き、陰影は PortraitPixels で細かく付ける：
    /// 素材ごとに「縁からの距離」をふくらみ（高さ）とみなして面の向きを出し、左上からの光で明暗を決め、
    /// 段階の間は市松（4×4 のディザ）でつなぐ。金属・金・白には照りの点、光る素材のまわりにはにじむ光。
    /// 見た目ごとの専用の絵（PortraitRows）がまだないものは、ダンジョンの絵を Scale2x で4倍にして同じ陰影を付ける。
    /// ドットの大きさは町の絵と同じ（TownScreen で町の絵と同じ倍率で拡大する）。
    /// </summary>
    public static partial class PixelArt
    {
        /// <summary>町の画面の大きな立ち姿。</summary>
        public static Sprite TownPortrait(CharacterData ch)
        {
            var type = (ClassType)ch.ClassId;
            string form = PortraitForm(type, ch);
            string key = "Portrait_" + form;
            if (cache.TryGetValue(key, out var cached)) return cached;

            PlayerArt(type, ch, out var small, out var over, out var aura);
            var rows = Portraits.TryGetValue(form, out var own) ? own : Scale2x(Scale2x(small));
            int h = rows.Length, w = rows[0].Length;
            if (own != null && PortraitPalette.TryGetValue(form, out var pal))
            {
                var merged = over != null ? new Dictionary<char, Color32[]>(over) : new Dictionary<char, Color32[]>();
                foreach (var kv in pal) merged[kv.Key] = kv.Value;
                over = merged;
            }
            var px = PortraitPixels(rows, over, aura);
            return Store(key, MakeTexture(key, w, h, px, TextureWrapMode.Clamp), w);
        }

        /// <summary>見た目の名前（専用の絵の鍵）。例：Lampwright／Lampwright_Adv0／Lampwright_Adv1_3rd／Vividbearer。</summary>
        static string PortraitForm(ClassType type, CharacterData ch)
        {
            if (type == ClassType.Vividbearer) return "Vividbearer";
            if (ch != null && ch.Advanced >= 0) return $"{type}_Adv{Mathf.Clamp(ch.Advanced, 0, 1)}" + (ch.Third ? "_3rd" : "");
            return type.ToString();
        }

        /// <summary>ダンジョンの絵の行と、色の差し替え・光の縁（PlayerFrame と同じ選び方）。</summary>
        static void PlayerArt(ClassType type, CharacterData ch, out string[] rows, out Dictionary<char, Color32[]> over, out Color32? aura)
        {
            over = null;
            aura = null;
            if (type == ClassType.Vividbearer) { rows = VividbearerHdRows; over = VividOver; aura = SpecialClass.AuraColor; return; }
            if (ch != null && ch.Advanced >= 0)
            {
                int k = (int)type * 2 + Mathf.Clamp(ch.Advanced, 0, 1);
                if (ch.Third) { rows = ThirdRows[k]; over = ThirdOver[k]; aura = ThirdOutline[k]; }
                else { rows = AdvRows[k]; over = AdvOver[k]; }
                return;
            }
            rows = type == ClassType.Shadeblade ? ShadebladeHdRows : type == ClassType.Warden ? WardenHdRows : LampwrightHdRows;
        }

        static readonly float[] Bayer4 =
        {
            0f / 16f, 8f / 16f, 2f / 16f, 10f / 16f,
            12f / 16f, 4f / 16f, 14f / 16f, 6f / 16f,
            3f / 16f, 11f / 16f, 1f / 16f, 9f / 16f,
            15f / 16f, 7f / 16f, 13f / 16f, 5f / 16f,
        };

        /// <summary>照りの点を付ける素材（金属・金・白・金の髪）。</summary>
        static bool Shiny(char c) => c == 'm' || c == 'g' || c == 'x' || c == 'H' || c == 'u' || c == 'i';

        /// <summary>宙に浮かぶきらめき・火の粉（数字の文字）。輪郭も光の縁も付けず、最後に光として描く。</summary>
        static readonly Dictionary<char, char> SparkleGlow = new Dictionary<char, char> { { '1', 'o' }, { '2', 'p' }, { '3', 'B' }, { '4', 'W' }, { '5', 'L' }, { '6', 'R' } };

        static Color32[] PortraitPixels(string[] src, Dictionary<char, Color32[]> overrides, Color32? auraColor)
        {
            int h = src.Length, w = src[0].Length;
            // きらめきは取り出して、絵の行からは消しておく
            var sparkles = new List<(int x, int y, Color32 c)>();
            var rows = new string[h];
            for (int y = 0; y < h; y++)
            {
                var line = src[y].ToCharArray();
                for (int x = 0; x < line.Length; x++)
                    if (SparkleGlow.TryGetValue(line[x], out var gch)) { sparkles.Add((x, y, glows[gch])); line[x] = '.'; }
                rows[y] = new string(line);
            }
            char At(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && x < rows[y].Length ? rows[y][x] : '.';

            // 素材ごとの、縁からの距離（4方向で数える）。縁は 1
            var dist = new int[w * h];
            var queue = new Queue<int>();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    char c = At(x, y);
                    if (c == '.') continue;
                    if (At(x + 1, y) != c || At(x - 1, y) != c || At(x, y + 1) != c || At(x, y - 1) != c) { dist[y * w + x] = 1; queue.Enqueue(y * w + x); }
                }
            while (queue.Count > 0)
            {
                int i = queue.Dequeue();
                int x = i % w, y = i / w;
                char c = At(x, y);
                void Visit(int nx, int ny)
                {
                    if (At(nx, ny) != c) return;
                    int j = ny * w + nx;
                    if (dist[j] != 0) return;
                    dist[j] = dist[i] + 1;
                    queue.Enqueue(j);
                }
                Visit(x + 1, y); Visit(x - 1, y); Visit(x, y + 1); Visit(x, y - 1);
            }

            const float R = 7f; // ふくらみが平らになるまでの距離
            float Height(int x, int y, char c)
            {
                if (At(x, y) != c) return 0f;
                float t = Mathf.Min(dist[y * w + x], R) / R;
                return Mathf.Sqrt(1f - (1f - t) * (1f - t));
            }
            // 左上からの光（行は下が +）
            float lx = -0.55f, ly = -0.65f, lz = 0.55f;
            float ll = Mathf.Sqrt(lx * lx + ly * ly + lz * lz);
            lx /= ll; ly /= ll; lz /= ll;

            var outline = new Color32(22, 16, 30, 255);
            var clear = new Color32(0, 0, 0, 0);
            var px = new Color32[w * h];
            var glowPx = new List<(int x, int y, Color32 c)>();
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
                        glowPx.Add((x, y, glow));
                    }
                    else if ((overrides != null && overrides.TryGetValue(ch, out var ramp)) || ramps.TryGetValue(ch, out ramp))
                    {
                        float hc = Height(x, y, ch);
                        float gx = Height(x + 1, y, ch) - Height(x - 1, y, ch);
                        float gy = Height(x, y + 1, ch) - Height(x, y - 1, ch);
                        float nx = -gx * 1.6f, ny = -gy * 1.6f, nl = Mathf.Sqrt(nx * nx + ny * ny + 1f);
                        float lum = (nx * lx + ny * ly + lz) / nl;              // 0.55（正面）〜 1（光の方を向く）〜 0（裏）
                        lum = (lum - 0.2f) / 0.75f;
                        lum += 0.1f * (1f - 2f * y / (float)h);                   // 上ほど少し明るい
                        // ほかの素材の下・右に接している所は、少し影になる（重なりの影）
                        char up = At(x, y - 1), left = At(x - 1, y);
                        if ((up != ch && up != '.') || (left != ch && left != '.')) lum -= 0.12f;
                        if (At(x, y - 2) != ch && At(x, y - 2) != '.' && up == ch) lum -= 0.06f;
                        float tone = Mathf.Clamp((1f - lum) * 3.2f, 0f, 3f);
                        int t0 = Mathf.FloorToInt(tone);
                        float frac = Mathf.Clamp01((tone - t0 - 0.5f) * 2.2f + 0.5f); // 段の変わり目の近くだけ市松に混ぜる（ざらつかないように）
                        if (t0 < 3 && frac > Bayer4[(y & 3) * 4 + (x & 3)]) t0++;
                        c = ramp[Mathf.Clamp(t0, 0, 3)];
                        if (Shiny(ch) && lum > 0.93f && hc > 0.3f) c = Mix(c, new Color32(255, 255, 255, 255), 0.55f); // 照り
                    }
                    else c = clear;
                    px[(h - 1 - y) * w + x] = c;
                }
            }

            // 光る素材のまわりに、にじむ光（輪郭の外側だけに、半透明で）
            foreach (var g in glowPx)
            {
                for (int dy = -3; dy <= 3; dy++)
                    for (int dx = -3; dx <= 3; dx++)
                    {
                        int x = g.x + dx, y = g.y + dy;
                        if (x < 0 || y < 0 || x >= w || y >= h) continue;
                        int i = (h - 1 - y) * w + x;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        if (d > 3.2f) continue;
                        byte a = (byte)(110 * (1f - d / 3.6f));
                        if (px[i].a == 0) px[i] = new Color32(g.c.r, g.c.g, g.c.b, a);
                        else if (px[i].a < 255 && px[i].a < a) px[i].a = a;
                    }
            }

            // 3次職・彩灯者：暗い輪郭の外側に、半透明の光の縁を2ドット
            if (auraColor.HasValue)
            {
                for (int ring = 0; ring < 2; ring++)
                {
                    var ac = auraColor.Value;
                    ac.a = (byte)(ring == 0 ? 190 : 90);
                    var halo = (Color32[])px.Clone();
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                        {
                            int i = y * w + x;
                            if (px[i].a > 0) continue;
                            bool near = (x > 0 && px[i - 1].a > 0) || (x < w - 1 && px[i + 1].a > 0) || (y > 0 && px[i - w].a > 0) || (y < h - 1 && px[i + w].a > 0);
                            if (near) halo[i] = ac;
                        }
                    px = halo;
                }
            }
            // きらめき：芯は不透明、まわりに半透明の光
            foreach (var g in sparkles)
            {
                for (int dy = -2; dy <= 2; dy++)
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        int x = g.x + dx, y = g.y + dy;
                        if (x < 0 || y < 0 || x >= w || y >= h) continue;
                        int i = (h - 1 - y) * w + x;
                        if (dx == 0 && dy == 0) { px[i] = g.c; continue; }
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        byte a = (byte)(90 * (1f - d / 2.9f));
                        if (px[i].a == 0) px[i] = new Color32(g.c.r, g.c.g, g.c.b, a);
                    }
            }
            return px;
        }
    }
}
