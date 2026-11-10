using UnityEngine;

namespace VividOfFade
{
    /// <summary>能力値。STR（力）・DEX（敏捷）・INT（知力）・VIT（体力）・WIS（精神）。</summary>
    public enum Attr { Str = 0, Dex = 1, Int = 2, Vit = 3, Wis = 4 }

    public static class Attributes
    {
        public const int Count = 5;

        static readonly string[] shortNames = { "STR", "DEX", "INT", "VIT", "WIS" };
        static readonly string[] effects =
        {
            "Physical power, armor",
            "Attack speed, crit",
            "Magic power",
            "Max HP, HP regen",
            "Flame power, MP, MP regen",
        };

        public static string ShortName(Attr a) => shortNames[(int)a];
        public static string Effect(Attr a) => effects[(int)a];
    }

    /// <summary>レベルと経験値。数値は仮。</summary>
    public static class Progression
    {
        public const int MaxLevel = 99;
        public const int PointsPerLevel = 5;

        public static int ExpToNext(int level) => Mathf.RoundToInt(15f * Mathf.Pow(level, 1.7f));

        public static int KillExp(int wave) => 1 + wave;

        /// <summary>経験値を足す。上がったレベルの数を返す。</summary>
        public static int AddExp(CharacterData c, int amount)
        {
            if (c.Level >= MaxLevel) return 0;
            c.Exp += amount;
            int gained = 0;
            while (c.Level < MaxLevel && c.Exp >= ExpToNext(c.Level))
            {
                c.Exp -= ExpToNext(c.Level);
                c.Level++;
                c.Points += PointsPerLevel;
                gained++;
            }
            if (c.Level >= MaxLevel) c.Exp = 0;
            return gained;
        }
    }
}
