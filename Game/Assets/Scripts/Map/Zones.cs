using UnityEngine;

namespace VividOfFade
{
    /// <summary>床の模様の種類（区画ごとに変える）。</summary>
    public enum FloorPattern { Stone, Moss, Lava, Ice, Gold, Void, Rift, Starless, Rune, Throne, Cloud, Garden, Choir, Sanctum } // Rift〜Throne は深淵、Cloud〜Sanctum は天界

    /// <summary>塔の区画（5層ごと）の見た目。どれもこのゲーム用のオリジナルの名前と配色。</summary>
    public sealed class ZoneSpec
    {
        public string Name;
        public string Subtitle;
        public string NameJa, SubtitleJa;
        public FloorPattern Pattern;
        public Color32 Floor;       // 床の石
        public Color32 FloorAlt;    // 市松のもう一方
        public Color32 Edge;        // 床の縁
        public Color32 Accent;      // 模様の色（苔・溶岩・霜・金・星）
        public Color32 WallTop;
        public Color32 WallLeft;
        public Color32 WallRight;
        public Color AmbientColor;  // 全体の光の色
        public float Ambient = 0.22f;
        public Color Background;    // 迷宮の外の色
        public Color32 Slime;       // その区画のスライムの色
        public Color32 SlimeDark;
        public int Cycle;           // 何周目か（周を重ねるほど深淵に侵され、名前の頭に言葉が付く）
        public bool Bright;         // 昇る塔（クリア後）：明暗が反転した区画

        public string Title => (Bright ? Loc.T("Inverted ", "反転した") : CyclePrefix(Cycle)) + Loc.T(Name, NameJa);

        /// <summary>周ごとに区画の名前の頭に付ける言葉（深淵に近づいていく）。</summary>
        public static string CyclePrefix(int cycle)
        {
            switch (cycle)
            {
                case 0: return "";
                case 1: return Loc.T("Deeper ", "深き");
                case 2: return Loc.T("Corrupted ", "蝕まれた");
                default: return Loc.T("Abyss-Drowned ", "深淵に呑まれた");
            }
        }
        public string DisplaySubtitle => Loc.T(Subtitle, SubtitleJa);
        public string Key => $"{Name}_{Cycle}" + (Bright ? "_bright" : ""); // 床・壁の絵は区画ごとに作って覚えておくので、明るい版は別の名前で

    }

    /// <summary>区画の表。5層ごとに次の区画へ。最後まで行くと、少し暗くした最初の区画に戻る。</summary>
    public static class Zones
    {
        static readonly ZoneSpec[] table =
        {
            new ZoneSpec
            {
                Name = "Ashen Halls", NameJa = "灰の回廊", SubtitleJa = "灰色の石、冷たい光", Subtitle = "Grey stone, cold light", Pattern = FloorPattern.Stone,
                Floor = new Color32(56, 52, 72, 255), FloorAlt = new Color32(50, 46, 64, 255), Edge = new Color32(30, 27, 40, 255),
                Accent = new Color32(70, 66, 88, 255),
                WallTop = new Color32(64, 58, 84, 255), WallLeft = new Color32(40, 35, 55, 255), WallRight = new Color32(28, 24, 40, 255),
                AmbientColor = new Color(0.55f, 0.6f, 0.85f), Ambient = 0.22f, Background = new Color(0.04f, 0.03f, 0.06f),
                Slime = new Color32(79, 191, 90, 255), SlimeDark = new Color32(47, 138, 60, 255),
            },
            new ZoneSpec
            {
                Name = "Mossy Depths", NameJa = "苔むす底", SubtitleJa = "湿った石、這い寄る緑", Subtitle = "Damp stone, creeping green", Pattern = FloorPattern.Moss,
                Floor = new Color32(48, 60, 56, 255), FloorAlt = new Color32(42, 54, 50, 255), Edge = new Color32(24, 32, 30, 255),
                Accent = new Color32(80, 130, 70, 255),
                WallTop = new Color32(56, 74, 62, 255), WallLeft = new Color32(36, 48, 42, 255), WallRight = new Color32(24, 34, 30, 255),
                AmbientColor = new Color(0.5f, 0.8f, 0.6f), Ambient = 0.2f, Background = new Color(0.02f, 0.05f, 0.04f),
                Slime = new Color32(70, 190, 180, 255), SlimeDark = new Color32(40, 130, 125, 255),
            },
            new ZoneSpec
            {
                Name = "Ember Forge", NameJa = "残り火の炉", SubtitleJa = "焦げたレンガ、溶けた継ぎ目", Subtitle = "Scorched brick, molten seams", Pattern = FloorPattern.Lava,
                Floor = new Color32(66, 44, 40, 255), FloorAlt = new Color32(58, 38, 34, 255), Edge = new Color32(34, 20, 18, 255),
                Accent = new Color32(255, 120, 40, 255),
                WallTop = new Color32(82, 52, 44, 255), WallLeft = new Color32(54, 32, 28, 255), WallRight = new Color32(38, 22, 20, 255),
                AmbientColor = new Color(1f, 0.55f, 0.4f), Ambient = 0.22f, Background = new Color(0.07f, 0.02f, 0.02f),
                Slime = new Color32(220, 80, 60, 255), SlimeDark = new Color32(150, 45, 35, 255),
            },
            new ZoneSpec
            {
                Name = "Frozen Crypt", NameJa = "凍てる墓所", SubtitleJa = "すべての石に霜", Subtitle = "Frost on every stone", Pattern = FloorPattern.Ice,
                Floor = new Color32(70, 86, 108, 255), FloorAlt = new Color32(62, 78, 100, 255), Edge = new Color32(34, 44, 62, 255),
                Accent = new Color32(200, 230, 255, 255),
                WallTop = new Color32(90, 110, 138, 255), WallLeft = new Color32(56, 70, 92, 255), WallRight = new Color32(40, 52, 72, 255),
                AmbientColor = new Color(0.55f, 0.75f, 1f), Ambient = 0.24f, Background = new Color(0.02f, 0.04f, 0.08f),
                Slime = new Color32(130, 210, 255, 255), SlimeDark = new Color32(80, 150, 200, 255),
            },
            new ZoneSpec
            {
                Name = "Sunken Archive", NameJa = "沈んだ書庫", SubtitleJa = "砂岩と古い金", Subtitle = "Sandstone and old gold", Pattern = FloorPattern.Gold,
                Floor = new Color32(84, 72, 52, 255), FloorAlt = new Color32(76, 64, 46, 255), Edge = new Color32(44, 36, 24, 255),
                Accent = new Color32(220, 180, 80, 255),
                WallTop = new Color32(104, 88, 62, 255), WallLeft = new Color32(70, 58, 40, 255), WallRight = new Color32(52, 42, 28, 255),
                AmbientColor = new Color(1f, 0.85f, 0.55f), Ambient = 0.22f, Background = new Color(0.06f, 0.05f, 0.02f),
                Slime = new Color32(230, 200, 70, 255), SlimeDark = new Color32(160, 135, 40, 255),
            },
            new ZoneSpec
            {
                Name = "Void Hollow", NameJa = "虚ろの淵", SubtitleJa = "光が消えゆく場所", Subtitle = "Where the light fades", Pattern = FloorPattern.Void,
                Floor = new Color32(30, 22, 40, 255), FloorAlt = new Color32(26, 18, 34, 255), Edge = new Color32(12, 8, 18, 255),
                Accent = new Color32(220, 140, 255, 255),
                WallTop = new Color32(44, 30, 60, 255), WallLeft = new Color32(28, 18, 40, 255), WallRight = new Color32(18, 12, 28, 255),
                AmbientColor = new Color(0.7f, 0.4f, 0.9f), Ambient = 0.17f, Background = new Color(0.02f, 0.0f, 0.04f),
                Slime = new Color32(170, 90, 230, 255), SlimeDark = new Color32(110, 50, 160, 255),
            },
        };

        /// <summary>深淵（101〜120層）の区画。黒い岩に、区画ごとの色で光る模様（裂け目・燐光・紋様・金と血の玉座）。</summary>
        static readonly ZoneSpec[] abyss =
        {
            new ZoneSpec
            {
                Name = "Abyss Gate", NameJa = "深淵の門", SubtitleJa = "黒い岩を裂く、赤い光", Subtitle = "Red light splitting black rock", Pattern = FloorPattern.Rift,
                Floor = new Color32(24, 18, 24, 255), FloorAlt = new Color32(20, 15, 20, 255), Edge = new Color32(8, 4, 8, 255),
                Accent = new Color32(255, 50, 60, 255),
                WallTop = new Color32(34, 24, 32, 255), WallLeft = new Color32(20, 12, 18, 255), WallRight = new Color32(12, 6, 10, 255),
                AmbientColor = new Color(1f, 0.35f, 0.4f), Ambient = 0.2f, Background = new Color(0.05f, 0f, 0.01f),
                Slime = new Color32(200, 30, 50, 255), SlimeDark = new Color32(90, 10, 25, 255),
            },
            new ZoneSpec
            {
                Name = "Starless Deep", NameJa = "星喰いの淵", SubtitleJa = "星を呑んだ闇に、青い燐光", Subtitle = "Blue phosphor in a sky that ate the stars", Pattern = FloorPattern.Starless,
                Floor = new Color32(14, 20, 30, 255), FloorAlt = new Color32(11, 16, 25, 255), Edge = new Color32(3, 6, 10, 255),
                Accent = new Color32(60, 230, 255, 255),
                WallTop = new Color32(22, 32, 46, 255), WallLeft = new Color32(12, 18, 28, 255), WallRight = new Color32(6, 10, 18, 255),
                AmbientColor = new Color(0.35f, 0.85f, 1f), Ambient = 0.19f, Background = new Color(0f, 0.02f, 0.05f),
                Slime = new Color32(40, 200, 230, 255), SlimeDark = new Color32(10, 70, 100, 255),
            },
            new ZoneSpec
            {
                Name = "Inverted Moon", NameJa = "逆さ月の祭壇", SubtitleJa = "床一面に、紫に燃える紋様", Subtitle = "Violet sigils burning across the floor", Pattern = FloorPattern.Rune,
                Floor = new Color32(26, 16, 36, 255), FloorAlt = new Color32(21, 12, 30, 255), Edge = new Color32(8, 3, 14, 255),
                Accent = new Color32(220, 90, 255, 255),
                WallTop = new Color32(38, 24, 54, 255), WallLeft = new Color32(22, 12, 34, 255), WallRight = new Color32(13, 6, 22, 255),
                AmbientColor = new Color(0.8f, 0.4f, 1f), Ambient = 0.19f, Background = new Color(0.03f, 0f, 0.06f),
                Slime = new Color32(190, 70, 255, 255), SlimeDark = new Color32(70, 15, 110, 255),
            },
            new ZoneSpec
            {
                Name = "Abyssal Throne", NameJa = "深淵の玉座", SubtitleJa = "金と血に彩られた、最奥", Subtitle = "The innermost dark, gilded in gold and blood", Pattern = FloorPattern.Throne,
                Floor = new Color32(26, 18, 14, 255), FloorAlt = new Color32(22, 14, 11, 255), Edge = new Color32(8, 4, 2, 255),
                Accent = new Color32(255, 200, 60, 255),
                WallTop = new Color32(40, 26, 18, 255), WallLeft = new Color32(24, 14, 10, 255), WallRight = new Color32(14, 8, 5, 255),
                AmbientColor = new Color(1f, 0.6f, 0.35f), Ambient = 0.2f, Background = new Color(0.05f, 0.01f, 0f),
                Slime = new Color32(255, 170, 40, 255), SlimeDark = new Color32(120, 30, 20, 255),
            },
        };

        /// <summary>天界（捧げたあと、昇る塔の上の101〜120層）の区画。白い石と雲、金の光。</summary>
        static readonly ZoneSpec[] heaven =
        {
            new ZoneSpec
            {
                Name = "Cloud Gate", NameJa = "雲の門", SubtitleJa = "雲を踏んで、光の門をくぐる", Subtitle = "Through a gate of light, upon the clouds", Pattern = FloorPattern.Cloud,
                Floor = new Color32(206, 212, 228, 255), FloorAlt = new Color32(192, 200, 220, 255), Edge = new Color32(140, 150, 176, 255),
                Accent = new Color32(255, 226, 140, 255),
                WallTop = new Color32(232, 236, 246, 255), WallLeft = new Color32(196, 204, 224, 255), WallRight = new Color32(168, 178, 204, 255),
                AmbientColor = new Color(1f, 0.97f, 0.9f), Ambient = 0.42f, Background = new Color(0.62f, 0.74f, 0.92f),
                Slime = new Color32(250, 250, 255, 255), SlimeDark = new Color32(190, 200, 230, 255),
            },
            new ZoneSpec
            {
                Name = "Garden of Light", NameJa = "光の花園", SubtitleJa = "白い敷石に、花びらが舞う", Subtitle = "Petals drifting over white stone", Pattern = FloorPattern.Garden,
                Floor = new Color32(214, 220, 206, 255), FloorAlt = new Color32(200, 210, 194, 255), Edge = new Color32(140, 158, 136, 255),
                Accent = new Color32(255, 170, 210, 255),
                WallTop = new Color32(236, 240, 228, 255), WallLeft = new Color32(204, 214, 196, 255), WallRight = new Color32(176, 190, 168, 255),
                AmbientColor = new Color(1f, 0.95f, 0.95f), Ambient = 0.4f, Background = new Color(0.7f, 0.86f, 0.8f),
                Slime = new Color32(255, 230, 240, 255), SlimeDark = new Color32(220, 170, 200, 255),
            },
            new ZoneSpec
            {
                Name = "Hall of Hymns", NameJa = "聖歌の回廊", SubtitleJa = "金の線を、歌が流れていく", Subtitle = "Song flowing along lines of gold", Pattern = FloorPattern.Choir,
                Floor = new Color32(222, 214, 196, 255), FloorAlt = new Color32(210, 200, 180, 255), Edge = new Color32(160, 140, 104, 255),
                Accent = new Color32(255, 200, 80, 255),
                WallTop = new Color32(242, 236, 220, 255), WallLeft = new Color32(214, 204, 182, 255), WallRight = new Color32(186, 172, 146, 255),
                AmbientColor = new Color(1f, 0.93f, 0.78f), Ambient = 0.4f, Background = new Color(0.86f, 0.78f, 0.6f),
                Slime = new Color32(255, 244, 210, 255), SlimeDark = new Color32(220, 190, 130, 255),
            },
            new ZoneSpec
            {
                Name = "Seraphic Sanctum", NameJa = "熾天の聖域", SubtitleJa = "いちばん高い所に、光輪が重なる", Subtitle = "Halos overlapping at the very highest place", Pattern = FloorPattern.Sanctum,
                Floor = new Color32(234, 230, 238, 255), FloorAlt = new Color32(222, 216, 230, 255), Edge = new Color32(170, 156, 186, 255),
                Accent = new Color32(255, 240, 180, 255),
                WallTop = new Color32(250, 248, 252, 255), WallLeft = new Color32(224, 218, 234, 255), WallRight = new Color32(196, 188, 212, 255),
                AmbientColor = new Color(1f, 0.98f, 0.94f), Ambient = 0.46f, Background = new Color(0.9f, 0.88f, 0.96f),
                Slime = new Color32(255, 252, 240, 255), SlimeDark = new Color32(230, 214, 170, 255),
            },
        };

        public static bool IsAbyssPattern(FloorPattern p) => p >= FloorPattern.Rift && p <= FloorPattern.Throne;
        public static bool IsHeavenPattern(FloorPattern p) => p >= FloorPattern.Cloud;
        public static int HeavenCount => heaven.Length;
        public static ZoneSpec HeavenZone(int i) => heaven[Mathf.Clamp(i, 0, heaven.Length - 1)];
        public static int HeavenIndex(ZoneSpec z)
        {
            if (z == null) return -1;
            for (int i = 0; i < heaven.Length; i++) if (heaven[i].Name == z.Name) return i;
            return -1;
        }

        public static int Count => table.Length;
        public static int AbyssCount => abyss.Length;
        public static ZoneSpec Tower(int i) => table[i];
        public static ZoneSpec AbyssZone(int i) => abyss[i];

        /// <summary>塔の区画の番号（2周目の区画も同じ番号）。見つからなければ -1。</summary>
        public static int TowerIndex(ZoneSpec z)
        {
            if (z == null) return -1;
            for (int i = 0; i < table.Length; i++) if (table[i].Name == z.Name) return i;
            return -1;
        }

        public static int AbyssIndex(ZoneSpec z)
        {
            if (z == null) return -1;
            for (int i = 0; i < abyss.Length; i++) if (abyss[i].Name == z.Name) return i;
            return -1;
        }

        /// <summary>塔の区画の名前（エンドロール用）。</summary>
        public static System.Collections.Generic.IEnumerable<string> TowerNames { get { foreach (var z in table) yield return Loc.T(z.Name, z.NameJa); } }

        /// <summary>深淵の区画の名前（エンドロール用）。</summary>
        public static System.Collections.Generic.IEnumerable<string> AbyssNames { get { foreach (var z in abyss) yield return Loc.T(z.Name, z.NameJa); } }

        /// <summary>その層の区画。2周目以降は全体の光を少し暗くする。101層からは深淵の区画。</summary>
        public static ZoneSpec For(int floor)
        {
            if (GameConfig.IsHeaven(floor))
                return heaven[Mathf.Clamp((floor - GameConfig.AbyssFirstFloor) / GameConfig.BossFloorEvery, 0, heaven.Length - 1)];
            if (GameConfig.IsAbyss(floor))
                return abyss[Mathf.Clamp((floor - GameConfig.AbyssFirstFloor) / GameConfig.BossFloorEvery, 0, abyss.Length - 1)];
            int index = Mathf.Max(0, floor - 1) / GameConfig.BossFloorEvery;
            if (GameConfig.Ascent)
            {
                // 昇る塔：塔をさかさまにたどる（1層は塔の底の区画、100層は塔のてっぺんの区画）。灯りが戻った世界なので、深淵には染まらない
                int top = (GameConfig.MaxFloor - 1) / GameConfig.BossFloorEvery;
                return Brighten(table[Mathf.Clamp(top - index, 0, top) % table.Length]);
            }
            int cycle = index / table.Length;
            var b = table[index % table.Length];
            if (cycle == 0) return b;
            // 周を重ねるほど、深淵の赤紫に染まっていく（床・壁・光・模様）。模様は強めに染めて、床の割れ目から赤い光が漏れるように見せる
            float k = Mathf.Min(0.22f * cycle, 0.66f);
            Color32 T(Color32 c, Color32 to, float m) => Color32.Lerp(c, to, m);
            var deep = new Color32(46, 18, 38, 255);
            return new ZoneSpec
            {
                Name = b.Name, Subtitle = b.Subtitle, NameJa = b.NameJa, SubtitleJa = b.SubtitleJa, Pattern = b.Pattern,
                Floor = T(b.Floor, deep, k), FloorAlt = T(b.FloorAlt, new Color32(40, 14, 32, 255), k), Edge = T(b.Edge, new Color32(22, 6, 16, 255), k),
                Accent = T(b.Accent, new Color32(120, 28, 48, 255), Mathf.Min(0.18f * cycle, 0.5f)), // 赤は暗めに。深淵（101層から）の赤い光より目立たせない
                WallTop = T(b.WallTop, new Color32(60, 22, 48, 255), k), WallLeft = T(b.WallLeft, new Color32(38, 12, 30, 255), k), WallRight = T(b.WallRight, new Color32(24, 6, 18, 255), k),
                AmbientColor = Color.Lerp(b.AmbientColor, new Color(0.8f, 0.45f, 0.6f), k * 0.6f), Ambient = Mathf.Max(0.12f, b.Ambient - 0.03f * cycle),
                Background = Color.Lerp(b.Background, new Color(0.06f, 0.01f, 0.04f), k),
                Slime = b.Slime, SlimeDark = b.SlimeDark, Cycle = cycle,
            };
        }

        static readonly System.Collections.Generic.Dictionary<string, ZoneSpec> brightCache = new System.Collections.Generic.Dictionary<string, ZoneSpec>();

        /// <summary>
        /// 昇る塔の区画：世界が反転した版。色味はそのままで明暗だけをひっくり返す（暗い石は明るく、光っていた模様は暗く沈む）。
        /// 壁は上の面がいちばん明るくなるように並べ直す。迷宮の外（背景）も明るい色になる。
        /// </summary>
        static ZoneSpec Brighten(ZoneSpec b)
        {
            if (brightCache.TryGetValue(b.Name, out var cached)) return cached;
            // 明暗の反転：色相と（少し抑えた）彩度はそのまま、明るさ v を 1 - v へ
            Color32 Inv(Color32 c, float vScale = 1f, float satScale = 0.7f)
            {
                // いちばん明るい成分を v とし、各成分の v からの差（色味）を保ったまま、v だけを反転させる
                float r = c.r / 255f, g = c.g / 255f, bl = c.b / 255f;
                float v = Mathf.Max(r, Mathf.Max(g, bl));
                float nv = Mathf.Clamp01((0.97f - v * 0.9f) * vScale);
                float k = satScale * (v > 0.001f ? Mathf.Min(1.5f, nv / Mathf.Max(v, 0.25f)) : 1f); // 明るくなった分だけ色味も少し広げる
                return new Color32((byte)(Mathf.Clamp01(nv - (v - r) * k) * 255f), (byte)(Mathf.Clamp01(nv - (v - g) * k) * 255f), (byte)(Mathf.Clamp01(nv - (v - bl) * k) * 255f), 255);
            }
            var top = Inv(b.WallTop);
            var floor = Inv(b.Floor);
            var z = new ZoneSpec
            {
                Name = b.Name, Subtitle = b.Subtitle, NameJa = b.NameJa, SubtitleJa = b.SubtitleJa, Pattern = b.Pattern,
                Floor = floor, FloorAlt = Inv(b.FloorAlt, 0.97f), Edge = Inv(b.Floor, 0.8f), // 縁は床より少しだけ暗く（敷石の境目が見えるように）
                Accent = Inv(b.Accent, 1f, 1f), // 光っていた模様は、暗く沈んだ色に
                WallTop = top, WallLeft = Inv(b.WallTop, 0.86f), WallRight = Inv(b.WallTop, 0.74f),
                AmbientColor = new Color(1f, 0.97f, 0.92f),
                Ambient = Mathf.Max(b.Ambient + 0.16f, 0.38f),
                Background = Color.Lerp((Color)Inv(Color32.Lerp(new Color32(0, 0, 0, 255), (Color32)b.Background, 1f)), new Color(0.85f, 0.86f, 0.9f), 0.4f), // 迷宮の外も反転して明るい色に
                Slime = b.Slime, SlimeDark = b.SlimeDark, Cycle = 0, Bright = true,
            };
            brightCache[b.Name] = z;
            return z;
        }

        /// <summary>層が変わったとき、区画も変わったか（その層が区画の最初の層か）。</summary>
        public static bool IsZoneStart(int floor) => (floor - 1) % GameConfig.BossFloorEvery == 0;
    }
}
