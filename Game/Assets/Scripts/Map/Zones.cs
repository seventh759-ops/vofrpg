using UnityEngine;

namespace VividOfFade
{
    /// <summary>床の模様の種類（区画ごとに変える）。</summary>
    public enum FloorPattern { Stone, Moss, Lava, Ice, Gold, Void }

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
        public int Cycle;           // 何周目か（2周目以降は少し暗く、名前に II を付ける）

        public string Title => Cycle > 0 ? $"{Loc.T(Name, NameJa)} {Roman(Cycle + 1)}" : Loc.T(Name, NameJa);
        public string DisplaySubtitle => Loc.T(Subtitle, SubtitleJa);
        public string Key => $"{Name}_{Cycle}";

        static string Roman(int n) => n == 2 ? "II" : n == 3 ? "III" : n == 4 ? "IV" : n == 5 ? "V" : n.ToString();
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

        public static int Count => table.Length;

        /// <summary>その層の区画。2周目以降は全体の光を少し暗くする。</summary>
        public static ZoneSpec For(int floor)
        {
            int index = Mathf.Max(0, floor - 1) / GameConfig.BossFloorEvery;
            int cycle = index / table.Length;
            var b = table[index % table.Length];
            if (cycle == 0) return b;
            return new ZoneSpec
            {
                Name = b.Name, Subtitle = b.Subtitle, NameJa = b.NameJa, SubtitleJa = b.SubtitleJa, Pattern = b.Pattern,
                Floor = b.Floor, FloorAlt = b.FloorAlt, Edge = b.Edge, Accent = b.Accent,
                WallTop = b.WallTop, WallLeft = b.WallLeft, WallRight = b.WallRight,
                AmbientColor = b.AmbientColor, Ambient = Mathf.Max(0.12f, b.Ambient - 0.03f * cycle), Background = b.Background,
                Slime = b.Slime, SlimeDark = b.SlimeDark, Cycle = cycle,
            };
        }

        /// <summary>層が変わったとき、区画も変わったか（その層が区画の最初の層か）。</summary>
        public static bool IsZoneStart(int floor) => (floor - 1) % GameConfig.BossFloorEvery == 0;
    }
}
