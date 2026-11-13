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
            "最大HP、HPの回復、防御",
            "灯りの攻撃力、範囲魔法、MP、MPの回復",
        };

        public static string Effect(Attr a) => Loc.Ja ? effectsJa[(int)a] : effects[(int)a];
    }

    /// <summary>レベルと経験値。数値は仮。</summary>
    public static class Progression
    {
        public const int MaxLevel = 99;       // ふつうのレベルの上限（装備のレベルもここまで）
        public const int UnlockedMaxLevel = 120; // 100層のボスを倒すと、ここまで上がれる
        public const int PointsPerLevel = 5;

        /// <summary>そのキャラクターのレベルの上限。100層のボスを倒したら 120 に上がる。</summary>
        public static int LevelCap(CharacterData c) =>
            c != null && c.BossCleared >= GameConfig.MaxFloor ? UnlockedMaxLevel : MaxLevel;

        /// <summary>ふつうの伸び方での、次のレベルまでの経験値（Lv1〜97 はこのまま使う）。</summary>
        static int BaseExpToNext(int level) => Mathf.RoundToInt(15f * Mathf.Pow(level, 1.7f));

        static int expTo98 = -1;
        /// <summary>Lv1 から Lv98 になるまでの経験値の合計。</summary>
        static int ExpTo98
        {
            get
            {
                if (expTo98 < 0)
                {
                    expTo98 = 0;
                    for (int l = 1; l < MaxLevel - 1; l++) expTo98 += BaseExpToNext(l);
                }
                return expTo98;
            }
        }

        /// <summary>Lv1 から Lv99 になるまでの経験値の合計（98→99 の分を含む。Lv1→98 の合計のちょうど2倍）。</summary>
        static int ExpTo99 => ExpTo98 * 2;

        /// <summary>Lv101 から先、1レベルごとに必要な経験値が何倍になるか。</summary>
        public const float LateExpGrowth = 1.05f;

        /// <summary>
        /// 次のレベルまでの経験値（MMO のように、節目の1レベルが大きな壁）。
        /// ・Lv1〜97：ふつうの伸び方。
        /// ・Lv98→99：Lv1→98 の合計と同じ（ここまでの道のりの半分）。
        /// ・Lv99→100：ふつうの伸び方の続き（上限が上がってすぐの1レベルは軽い）。
        /// ・Lv100→101：Lv1→99 の合計と同じ（2つ目の壁）。
        /// ・Lv101 から先：1レベルごとに LateExpGrowth 倍ずつ重くなる。
        /// </summary>
        public static int ExpToNext(int level)
        {
            if (level < MaxLevel - 1) return BaseExpToNext(level);
            if (level == MaxLevel - 1) return ExpTo98;
            if (level == MaxLevel) return BaseExpToNext(level);
            double v = ExpTo99 * System.Math.Pow(LateExpGrowth, level - (MaxLevel + 1));
            return (int)System.Math.Min(int.MaxValue, System.Math.Round(v));
        }

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
            int cap = LevelCap(c);
            if (c.Level >= cap) return 0;
            c.Exp += amount;
            int gained = 0;
            while (c.Level < cap && c.Exp >= ExpToNext(c.Level))
            {
                c.Exp -= ExpToNext(c.Level);
                c.Level++;
                c.MpGrown += MpPerLevel(BaseWis(c)); // 振る前の、今の WIS で決まる
                c.Points += PointsPerLevel;
                gained++;
            }
            if (c.Level >= cap) c.Exp = 0;
            return gained;
        }
    }
}
