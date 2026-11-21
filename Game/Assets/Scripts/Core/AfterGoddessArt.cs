using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    // 昇る塔の100層で女神像を壊したあと：町の「残り火の台座」、100層のボス「天の門番」、昇る塔の上り階段、天界へ続く光の柱。
    // どれもこのゲーム用のオリジナルの意匠。
    public static partial class PixelArt
    {
        /// <summary>残り火の台座：砕けた女神像の下半分（台座は同じ）の上に、女神の光の残り火が浮かぶ。</summary>
        public static Sprite EmberPedestal => cache.TryGetValue("Town32_ember", out var c) ? c : Shaded("Town32_ember", EmberPedestalRows, MarbleOver);

        /// <summary>天の門番：光輪と金の兜、白い翼、白金の鎧に青い腰布、鍵の形の大きな杖。</summary>
        public static Sprite HeavenGatekeeper => cache.TryGetValue("HeavenBoss_Gatekeeper", out var c) ? c
            : Shaded("HeavenBoss_Gatekeeper", GatekeeperRows, HeavenOver, new Color32(255, 230, 150, 255), 0, true);

        static readonly string[] EmberPedestalRows =
        {
            "................................",
            "...............LL...............",
            "...............LL...............",
            "..............LLLL.....l........",
            "..............LllL..............",
            ".........l...LLllLL.............",
            ".............LloolL.............",
            ".............LloolL.............",
            "............LLloolLL............",
            "............LLloolLL..l.........",
            ".............LloolL.............",
            "..........l..LloolL.............",
            "..............LllL..............",
            "........................l.......",
            "................................",
            "...............k...k............",
            ".........kk...kkk.kxk..k........",
            ".....kk.kxxk.kkkkkqxxkkqk.......",
            "...kkxxkxxxxkkkkkkkxxxxxqkk.....",
            "..kxxxqxxxxxkkkqqkkkxxxxxqxk....",
            "..xxxxqxxxxxkkkqqkkkxxxxxqxxkk..",
            "..xxxqxxxxxkkkxqqxkkkxxxxxqxxx..",
            "...xxqxxxxkkkxxqqxxkkkxxxxqxx...",
            "...xxxkkkkkkxxxqqxxxkkkkkkxxx...",
            "....xxkkkkkxxxxqqxxxxkkkkkxx....",
            ".....xxxxxxxxxxqqxxxxxxxxxx.....",
            ".......qqqqqqqqqqqqqqqqqq.......",
            "........qxxxxxxxxxxxxxxq........",
            "........qxxgggggggggggxq........",
            "....x...qxxgLggggggBggxq....x...",
            "...xx...qxxgggggggggggxq...xx...",
            "..x.....qqqqqqqqqqqqqqqq........",
        };

        static readonly string[] GatekeeperRows =
        {
            "............oooooooo............",
            "...........o...yy...o..yyy......",
            "..............yyyy....y.o.y.....",
            ".x..........XxxyyxxX..y.o.y...x.",
            ".xxx........XxxyyxxX...yXy..xxx.",
            ".xxxxxx.....XnBnnBnX....Xxxxxxx.",
            "xxxxxxxx....XnnnnnnX....Xxxxxxxx",
            "xxxXxxxxx...XxxxxxxX...xXxxxXxxx",
            "xxxxXXxxxx..XXXXXXXX..xxXxXXxxxx",
            ".xxxxxXxxxXXXXXXXXXXXXxxXXxxxxx.",
            ".xxxxxxXxxXXXXXXXXXXXXxxXxxxxxx.",
            ".xxxxxxxXXxxxxxyyxxxxxXXXxxxxxx.",
            "..XXxxxxxxxxxxxBBxxxxxxxXxxxXX..",
            "..xxXXxxxxxxxxxyyxxxxxxxXxXXxx..",
            "..xxxxXXXXxxxxxyyxxxxxXXXXxxxx..",
            "..xxxxxxxXXxxxxyyxxxxXXxXxxxxx..",
            ".xxxxxxxxxxxxxxyyxxxxxxxXxxxxxx.",
            ".xxXXXXxxxxXXXXXXXXXXxxxXXXXXxx.",
            "..xxxxxXXXXuuuuXXuuuuXXXXxxxxx..",
            "...xxxxxxxxuuuuXXuuuuxxxXxxxx...",
            "...xxxxxxxuuuuuXXuuuuuxxXxxxx...",
            "....xxxxxxuuuuuXXuuuuuxxXxxx....",
            "........x.uuuuuXXuuuuu.xX.......",
            "..........uuuuuXXuuuuu..X.......",
            ".........uuuuuuXXuuuuuu.X.......",
            ".........uuuuuuXXuuuuuu.X.......",
            "............xxx..xxx....Xyy.....",
            "............xxx..xxx....X.......",
            "............xxx..xxx....Xyyy....",
            "............xxx..xxx....X.......",
            "...........XXXX..XXXX...X.......",
            "................................",
        };

        /// <summary>
        /// 上り階段（昇る塔）：64×64。ひし形の床の上に、奥へ向かって高くなる白い石の段。段の縁は金、いちばん上は光る。
        /// 足元のひし形の中心が、マスの中心に来るように置く（下から 16 ドットが中心）。
        /// </summary>
        public static Sprite StairsUp
        {
            get
            {
                const string key = "StairsUp64";
                if (cache.TryGetValue(key, out var cached)) return cached;
                const int w = 64, h = 64, baseY = 16; // ひし形の中心の高さ（下から）
                var px = new Color32[w * h];
                var stone = new Color32(206, 196, 176, 255);
                const int steps = 4;
                // 足元のひし形（金の縁）
                for (int sy = 0; sy < 32; sy++)
                    for (int x = 0; x < w; x++)
                    {
                        float u = (x + 0.5f - 32f) / 32f, v = (sy + 0.5f - 16f) / 16f;
                        float d = Mathf.Abs(u) + Mathf.Abs(v);
                        if (d > 1f) continue;
                        px[(sy + baseY - 16) * w + x] = d > 0.92f ? new Color32(255, 205, 90, 255) : Shade(stone, -50);
                    }
                // 段：奥（a が大きい）ほど高い。奥の段から手前へ、側面を塗ってから上の面を塗る
                for (int k = 0; k < steps; k++)
                {
                    int rise = 4 + (steps - 1 - k) * 5;                 // k=0 がいちばん奥で高い
                    float aHi = 1.85f - k * (1.7f / steps), aLo = aHi - 1.7f / steps; // a が大きい＝画面の奥（テクスチャは下から上）
                    for (int pass = 0; pass < 2; pass++)
                        for (int sy = 0; sy < 32; sy++)
                            for (int x = 0; x < w; x++)
                            {
                                float u = (x + 0.5f - 32f) / 32f, v = (sy + 0.5f - 16f) / 16f;
                                if (Mathf.Abs(u) + Mathf.Abs(v) > 0.86f) continue;
                                float a = u + v + 1f;
                                if (a < aLo || a > aHi) continue;
                                int floorY = sy + baseY - 16;
                                if (pass == 0)
                                {
                                    for (int z = 0; z < rise; z++) // 側面（手前の面。右側は暗く）
                                    {
                                        int py = floorY + z;
                                        if (py >= 0 && py < h) px[py * w + x] = Shade(stone, u > -v ? -46 : -24);
                                    }
                                }
                                else
                                {
                                    int py = floorY + rise;                       // 上の面
                                    if (py < 0 || py >= h) continue;
                                    bool lip = a < aLo + 0.09f;                   // 段の手前の縁は金
                                    var top = Shade(stone, 10 + (steps - 1 - k) * 6 + (int)((TileNoise(x / 3f, sy / 2f, 91) - 0.5f) * 10f));
                                    px[py * w + x] = lip ? new Color32(255, 214, 110, 255) : top;
                                }
                            }
                }
                // いちばん上の段に、上から差す光
                for (int y = 30; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float dx = (x + 0.5f - 44f) / 9f, t = (y - 30f) / (h - 30f);
                        float al = Mathf.Clamp01(1f - Mathf.Abs(dx)) * (1f - t) * 0.55f;
                        if (al <= 0f) continue;
                        var p = px[y * w + x];
                        var glow = new Color32(255, 246, 210, 255);
                        if (p.a == 0) px[y * w + x] = new Color32(glow.r, glow.g, glow.b, (byte)(al * 255f));
                        else px[y * w + x] = Color32.Lerp(p, glow, al);
                    }
                var tex = MakeTexture(key, w, h, px, TextureWrapMode.Clamp);
                var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, baseY / (float)h), 32, 0, SpriteMeshType.FullRect);
                sprite.name = key;
                cache[key] = sprite;
                return sprite;
            }
        }

        /// <summary>
        /// 天界へ続く光の柱：64×160。足元の光の輪から、上へ細くなりながら空へ伸びる白金の光。
        /// 足元のひし形の中心が、マスの中心に来るように置く。
        /// </summary>
        public static Sprite LightPillar
        {
            get
            {
                const string key = "LightPillar";
                if (cache.TryGetValue(key, out var cached)) return cached;
                const int w = 64, h = 160, baseY = 16;
                var px = new Color32[w * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float u = (x + 0.5f - 32f) / 32f;
                        float al = 0f;
                        Color32 col = new Color32(255, 244, 200, 255);
                        // 足元の光の輪（ひし形）
                        float v = (y + 0.5f - baseY) / 16f;
                        float d = Mathf.Abs(u) + Mathf.Abs(v);
                        if (d <= 1f) al = Mathf.Max(al, (1f - d) * 0.9f + (d > 0.85f ? 0.5f : 0f));
                        // 柱：上へ行くほど細く・淡く。芯は白
                        if (y >= baseY - 4)
                        {
                            float t = Mathf.Clamp01((y - baseY) / (float)(h - baseY));
                            float half = Mathf.Lerp(0.42f, 0.22f, t);
                            float q = Mathf.Abs(u) / half;
                            if (q < 1f)
                            {
                                float a2 = (1f - q * q) * Mathf.Lerp(0.95f, 0.0f, Mathf.Pow(t, 1.4f));
                                if (q < 0.3f) col = new Color32(255, 255, 250, 255);
                                al = Mathf.Max(al, a2);
                            }
                        }
                        if (al > 0f) px[y * w + x] = new Color32(col.r, col.g, col.b, (byte)(Mathf.Clamp01(al) * 255f));
                    }
                var tex = MakeTexture(key, w, h, px, TextureWrapMode.Clamp);
                var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, baseY / (float)h), 32, 0, SpriteMeshType.FullRect);
                sprite.name = key;
                cache[key] = sprite;
                return sprite;
            }
        }
    }
}
