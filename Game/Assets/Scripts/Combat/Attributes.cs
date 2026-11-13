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
            "Physical power",
            "Attack speed, crit, HIT, EVA",
            "Magic power (single-target spells most)",
            "Max HP, HP regen, armor",
            "Flame power, area spells, MP, MP regen",
        };

        public static string ShortName(Attr a) => shortNames[(int)a];
        static readonly string[] effectsJa =
        {
            "物理の攻撃力",
            "攻撃の速さ、会心、命中、回避",
            "魔法の攻撃力（単体の魔法は特に）",
            "最大体力、体力の回復、防御",
            "灯りの攻撃力、範囲魔法、MP、MPの回復",
        };

        public static string Effect(Attr a) => Loc.Ja ? effectsJa[(int)a] : effects[(int)a];
    }

    /// <summary>レベルと経験値。数値は仮。</summary>
    public static class Progression
    {
        public const int MaxLevel = 99;
        public const int PointsPerLevel = 5;

        public static int ExpToNext(int level) => Mathf.RoundToInt(15f * Mathf.Pow(level, 1.7f));

        /// <summary>レベルアップ1回で増える最大 MP（そのときの WIS で決まる）。</summary>
        public static int MpPerLevel(int wis) => 2 + wis / 3;

        /// <summary>自分で振った分と職業の初期値の WIS（装備は数えない）。</summary>
        public static int BaseWis(CharacterData c)
        {
            var cls = ClassInfo.Get((ClassType)c.ClassId);
            return cls.BaseStats[(int)Attr.Wis] + c.Alloc[(int)Attr.Wis];
        }

        /// <summary>MP の積み上げを入れる前のセーブを、今の WIS で見積もり直す（一度だけ）。</summary>
        public static void EnsureMpGrowth(CharacterData c)
        {
            if (c.MpGrowthReady) return;
            c.MpGrown = (c.Level - 1) * MpPerLevel(BaseWis(c));
            c.MpGrowthReady = true;
        }

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
                c.MpGrown += MpPerLevel(BaseWis(c)); // 振る前の、今の WIS で決まる
                c.Points += PointsPerLevel;
                gained++;
            }
            if (c.Level >= MaxLevel) c.Exp = 0;
            return gained;
        }
    }
}
