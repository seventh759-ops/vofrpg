using UnityEngine;

namespace VividOfFade
{
    /// <summary>スキルの効果の種類。</summary>
    public enum SkillEffect
    {
        Nova,     // 自分の周りの敵すべてに当たる
        Barrage,  // 火の玉・飛ぶ斬撃をまとめて撃つ
        Dash,     // 狙った敵へ突進し、通り道の敵を斬る
        Strike,   // 狙った場所に、少し遅れて落ちる
        Buff,     // 一定時間、能力を上げる
        Heal,     // 体力を回復する
    }

    /// <summary>スキル1つの定義。数値はすべて仮で、遊んでから調整する。</summary>
    public sealed class SkillSpec
    {
        public string Name;
        public string Desc;
        public string NameJa, DescJa;
        public int ReqLevel;

        public string DisplayName => Loc.T(Name, NameJa);
        public string DisplayDesc => Loc.T(Desc, DescJa);
        public int Mp;
        public float Cooldown;
        public SkillEffect Effect;

        public float Mult;       // スキルの基準の攻撃力（PlayerStats.SkillBase）に対する倍率
        public float Radius;     // Nova / Strike の範囲（マス）
        public int Count;        // Barrage の数
        public bool AllAround;   // Barrage：全方向（false なら狙った方向に扇形）
        public float Delay;      // Strike：落ちるまでの時間
        public float Range = 6f; // 狙える距離・Dash の長さ

        // Buff / Heal
        public float Duration;
        public float BuffDamage;      // +割合
        public float BuffAttackSpeed; // +割合
        public float BuffArmor;
        public float BuffTaken = 1f;  // 受けるダメージの倍率
        public float BuffLight;
        public int BuffExtra;         // 弾・灯りの数など（職業ごとの「弾の数」の効果）
        public float HealPct;         // 最大体力に対する割合

        public Color Color = new Color(1f, 0.7f, 0.3f, 0.9f);
    }

    /// <summary>職業ごとの6つのスキル（覚えるレベルの順）と、上位職の7つ目のスキル。</summary>
    public static class SkillInfo
    {
        public const int PerClass = 8;
        public const int Slots = 3;
        public const int MaxSkillLevel = 10;

        static readonly SkillSpec[][] table =
        {
            // 灯術師（Lampwright）
            new[]
            {
                new SkillSpec { Name = "Flame Ring", NameJa = "火の輪", DescJa = "周りに火を放つ。", Desc = "Burst of fire around you.", ReqLevel = 1, Mp = 8, Cooldown = 3f,
                    Effect = SkillEffect.Nova, Mult = 4f, Radius = 2.2f },
                new SkillSpec { Name = "Fire Barrage", NameJa = "火の弾幕", DescJa = "全方向に火の玉を12発。", Desc = "12 fireballs in all directions.", ReqLevel = 5, Mp = 12, Cooldown = 5f,
                    Effect = SkillEffect.Barrage, Mult = 2f, Count = 12, AllAround = true },
                new SkillSpec { Name = "Flare", NameJa = "照らし", DescJa = "6秒、明るさ+3・ダメージ+20%。", Desc = "Light +3 and damage +20% for 6s.", ReqLevel = 10, Mp = 15, Cooldown = 15f,
                    Effect = SkillEffect.Buff, Duration = 6f, BuffLight = 3f, BuffDamage = 0.2f, Color = new Color(1f, 0.95f, 0.6f, 0.9f) },
                new SkillSpec { Name = "Meteor", NameJa = "流星", DescJa = "一番近い敵に、少し遅れて落ちる。", Desc = "Falls on the nearest enemy after a moment.", ReqLevel = 18, Mp = 20, Cooldown = 6f,
                    Effect = SkillEffect.Strike, Mult = 10f, Radius = 1.6f, Delay = 0.6f, Range = 8f },
                new SkillSpec { Name = "Ember Shield", NameJa = "残り火の守り", DescJa = "6秒、防御+50・被ダメージ-30%。", Desc = "Armor +50, damage taken -30% for 6s.", ReqLevel = 26, Mp = 20, Cooldown = 18f,
                    Effect = SkillEffect.Buff, Duration = 6f, BuffArmor = 50f, BuffTaken = 0.7f, Color = new Color(1f, 0.5f, 0.2f, 0.9f) },
                new SkillSpec { Name = "Inferno", NameJa = "業火", DescJa = "とても大きな火の輪。", Desc = "Huge ring of fire.", ReqLevel = 35, Mp = 40, Cooldown = 12f,
                    Effect = SkillEffect.Nova, Mult = 7.5f, Radius = 4f },
            },
            // 影刃（Shadeblade）
            new[]
            {
                new SkillSpec { Name = "Whirlwind", NameJa = "旋風", DescJa = "回転して周りを斬る。", Desc = "Spin and cut everything around you.", ReqLevel = 1, Mp = 6, Cooldown = 3f,
                    Effect = SkillEffect.Nova, Mult = 5f, Radius = 1.8f, Color = new Color(0.85f, 0.9f, 1f, 0.9f) },
                new SkillSpec { Name = "Shadow Dash", NameJa = "影走り", DescJa = "敵の中を突き抜けて斬る。", Desc = "Dash through enemies, cutting them.", ReqLevel = 5, Mp = 10, Cooldown = 4f,
                    Effect = SkillEffect.Dash, Mult = 6.2f, Radius = 0.9f, Range = 4f, Color = new Color(0.6f, 0.5f, 0.9f, 0.9f) },
                new SkillSpec { Name = "Frenzy", NameJa = "狂乱", DescJa = "6秒、攻撃速度+50%。", Desc = "Attack speed +50% for 6s.", ReqLevel = 10, Mp = 12, Cooldown = 14f,
                    Effect = SkillEffect.Buff, Duration = 6f, BuffAttackSpeed = 0.5f, Color = new Color(1f, 0.35f, 0.35f, 0.9f) },
                new SkillSpec { Name = "Crescent Wave", NameJa = "三日月の刃", DescJa = "敵へ飛ぶ斬撃を5つ。", Desc = "5 flying blades toward the enemy.", ReqLevel = 18, Mp = 15, Cooldown = 5f,
                    Effect = SkillEffect.Barrage, Mult = 3.8f, Count = 5, Color = new Color(0.85f, 0.9f, 1f, 0.9f) },
                new SkillSpec { Name = "Second Wind", NameJa = "立て直し", DescJa = "最大体力の35%を回復。", Desc = "Recover 35% of max HP.", ReqLevel = 26, Mp = 20, Cooldown = 12f,
                    Effect = SkillEffect.Heal, HealPct = 0.35f, Color = new Color(0.5f, 1f, 0.6f, 0.9f) },
                new SkillSpec { Name = "Thousand Cuts", NameJa = "千の斬撃", DescJa = "周りに刃の嵐。", Desc = "A storm of blades around you.", ReqLevel = 35, Mp = 35, Cooldown = 10f,
                    Effect = SkillEffect.Nova, Mult = 12.5f, Radius = 2.6f, Color = new Color(0.85f, 0.9f, 1f, 0.9f) },
            },
            // 灯番（Warden）
            new[]
            {
                new SkillSpec { Name = "Holy Pulse", NameJa = "聖なる波動", DescJa = "周りに光の波。", Desc = "Wave of light around you.", ReqLevel = 1, Mp = 8, Cooldown = 3f,
                    Effect = SkillEffect.Nova, Mult = 3.8f, Radius = 2.4f, Color = new Color(1f, 0.9f, 0.5f, 0.9f) },
                new SkillSpec { Name = "Mend", NameJa = "癒やし", DescJa = "最大体力の30%を回復。", Desc = "Recover 30% of max HP.", ReqLevel = 5, Mp = 12, Cooldown = 7f,
                    Effect = SkillEffect.Heal, HealPct = 0.3f, Color = new Color(0.5f, 1f, 0.6f, 0.9f) },
                new SkillSpec { Name = "Lantern Ward", NameJa = "灯りの守り", DescJa = "6秒、防御+60・被ダメージ-40%。", Desc = "Armor +60, damage taken -40% for 6s.", ReqLevel = 10, Mp = 15, Cooldown = 16f,
                    Effect = SkillEffect.Buff, Duration = 6f, BuffArmor = 60f, BuffTaken = 0.6f, Color = new Color(1f, 0.85f, 0.4f, 0.9f) },
                new SkillSpec { Name = "Kindle", NameJa = "灯し足し", DescJa = "8秒、灯り+3・ダメージ+20%。", Desc = "+3 flames and damage +20% for 8s.", ReqLevel = 18, Mp = 18, Cooldown = 18f,
                    Effect = SkillEffect.Buff, Duration = 8f, BuffExtra = 3, BuffDamage = 0.2f },
                new SkillSpec { Name = "Flame Pillar", NameJa = "火柱", DescJa = "一番近い敵に火柱。", Desc = "Pillar of fire on the nearest enemy.", ReqLevel = 26, Mp = 22, Cooldown = 7f,
                    Effect = SkillEffect.Strike, Mult = 7.5f, Radius = 2f, Delay = 0.5f, Range = 7f },
                new SkillSpec { Name = "Judgment", NameJa = "裁き", DescJa = "とても広い光の波。体力20%回復。", Desc = "Vast wave of light; heals 20%.", ReqLevel = 35, Mp = 45, Cooldown = 14f,
                    Effect = SkillEffect.Nova, Mult = 7.5f, Radius = 4.5f, HealPct = 0.2f, Color = new Color(1f, 0.95f, 0.7f, 0.9f) },
            },
        };

        public static SkillSpec Get(ClassType type, int index) =>
            index == AdvancedClasses.SkillIndex ? AdvancedClasses.SkillFor(type)
            : index == ThirdClasses.SkillIndex ? ThirdClasses.SkillFor(type)
            : table[(int)type][index];

        public static bool Learned(ClassType type, int index, int level)
        {
            if (index == AdvancedClasses.SkillIndex && Profile.Get(type).Advanced < 0) return false;
            if (index == ThirdClasses.SkillIndex && !Profile.Get(type).Third) return false;
            return level >= Get(type, index).ReqLevel;
        }

        /// <summary>スキルレベルによる強さ（威力・回復量・効果の大きさ）。Lv1 で 1、1つ上がるごとに +10%。</summary>
        public static float Power(int skillLevel) => 1f + 0.1f * (Mathf.Max(1, skillLevel) - 1);

        /// <summary>スキルレベルによる待ち時間の倍率。1つ上がるごとに -3%。</summary>
        public static float CooldownMult(int skillLevel) => 1f - 0.03f * (Mathf.Max(1, skillLevel) - 1);

        /// <summary>次のスキルレベルまでに必要な使用回数。</summary>
        public static int UsesToNext(int skillLevel) => 5 + 5 * skillLevel;
    }
}
