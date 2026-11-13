using UnityEngine;

namespace VividOfFade
{
    /// <summary>上位職1つ。ずっと効く能力の補正（加護と同じ形）と、7つ目のスキル。</summary>
    public sealed class AdvancedSpec
    {
        public string Name;
        public string NameJa;
        public string Desc;
        public string DescJa;
        public BoonSpec Passive;
        public SkillSpec Skill;

        public string DisplayName => Loc.T(Name, NameJa);
        public string DisplayDesc => Loc.T(Desc, DescJa);
    }

    /// <summary>
    /// 転職（町の師匠）。Lv30 から、職業ごとに2つの上位職から1つを選ぶ（1回だけ）。
    /// 転職すると Lv1 に戻り（振った能力値も戻る）、ボーナスとして 30 ポイントを振れる。
    /// どれもこのゲーム用のオリジナルの名前。数値はすべて仮で、遊んでから調整する。
    /// </summary>
    public static class AdvancedClasses
    {
        public const int ReqLevel = 30;
        public const int Cost = 500;      // 火種
        public const int SkillIndex = 6;  // 7つ目のスキルの番号
        public const int BonusPoints = 30; // 転職すると Lv1 に戻り、このポイントを振れる

        static readonly AdvancedSpec[][] table =
        {
            // 灯術師
            new[]
            {
                new AdvancedSpec
                {
                    Name = "Flarecaller", NameJa = "炎呼び",
                    Desc = "Damage +20%, crit damage +30%, enemies explode on death.", DescJa = "ダメージ+20%、会心ダメージ+30%、倒した敵が爆発する。",
                    Passive = new BoonSpec { Damage = 0.2f, CritDamage = 0.3f, Explode = 0.3f },
                    Skill = new SkillSpec { Name = "Starfall", NameJa = "星降り", Desc = "A huge star falls on the nearest enemy.", DescJa = "一番近い敵に、大きな星が落ちる。",
                        ReqLevel = 1, Mp = 35, Cooldown = 8f, Effect = SkillEffect.Strike, Mult = 15f, Radius = 2.4f, Delay = 0.7f, Range = 9f,
                        Color = new Color(1f, 0.6f, 0.3f, 0.9f) },
                },
                new AdvancedSpec
                {
                    Name = "Beaconkeeper", NameJa = "灯台守",
                    Desc = "Light +1.5, +1 projectile, max MP +20%, MP regen +1/s.", DescJa = "明るさ+1.5、弾+1、最大MP+20%、MP回復+1/秒。",
                    Passive = new BoonSpec { Light = 1.5f, Extra = 1, MpPct = 0.2f, MpRegen = 1f },
                    Skill = new SkillSpec { Name = "Beacon", NameJa = "灯台", Desc = "Light +4, +2 projectiles, damage +15% for 8s.", DescJa = "8秒、明るさ+4・弾+2・ダメージ+15%。",
                        ReqLevel = 1, Mp = 30, Cooldown = 18f, Effect = SkillEffect.Buff, Duration = 8f, BuffLight = 4f, BuffExtra = 2, BuffDamage = 0.15f,
                        Color = new Color(1f, 0.95f, 0.65f, 0.9f) },
                },
            },
            // 影刃
            new[]
            {
                new AdvancedSpec
                {
                    Name = "Nightreaver", NameJa = "夜狩り",
                    Desc = "Crit chance +10%, crit damage +40%, attack speed +10%.", DescJa = "会心率+10%、会心ダメージ+40%、攻撃速度+10%。",
                    Passive = new BoonSpec { Crit = 0.1f, CritDamage = 0.4f, AttackSpeed = 0.1f },
                    Skill = new SkillSpec { Name = "Eclipse Rush", NameJa = "蝕の突進", Desc = "A long dash that tears through enemies.", DescJa = "長く突進し、敵を切り裂く。",
                        ReqLevel = 1, Mp = 25, Cooldown = 6f, Effect = SkillEffect.Dash, Mult = 12.5f, Radius = 1.2f, Range = 6f,
                        Color = new Color(0.55f, 0.4f, 0.95f, 0.9f) },
                },
                new AdvancedSpec
                {
                    Name = "Bloodedge", NameJa = "血刃",
                    Desc = "Heal 1% of damage dealt, max HP +20%, damage +10%.", DescJa = "与えたダメージの1%を回復、最大体力+20%、ダメージ+10%。",
                    Passive = new BoonSpec { LifeSteal = 0.01f, HpPct = 0.2f, Damage = 0.1f },
                    Skill = new SkillSpec { Name = "Crimson Spiral", NameJa = "紅の渦", Desc = "Cut everything around you; heals 10%.", DescJa = "周りを斬り、体力10%回復。",
                        ReqLevel = 1, Mp = 30, Cooldown = 9f, Effect = SkillEffect.Nova, Mult = 10f, Radius = 2.6f, HealPct = 0.1f,
                        Color = new Color(1f, 0.3f, 0.35f, 0.9f) },
                },
            },
            // 灯番
            new[]
            {
                new AdvancedSpec
                {
                    Name = "Sunbinder", NameJa = "陽縛り",
                    Desc = "+1 flame, damage +15%, light +1.", DescJa = "灯り+1、ダメージ+15%、明るさ+1。",
                    Passive = new BoonSpec { Extra = 1, Damage = 0.15f, Light = 1f },
                    Skill = new SkillSpec { Name = "Sunburst", NameJa = "日輪", Desc = "A blazing ring of sunlight.", DescJa = "まぶしい日の光の輪。",
                        ReqLevel = 1, Mp = 35, Cooldown = 10f, Effect = SkillEffect.Nova, Mult = 10f, Radius = 3.5f,
                        Color = new Color(1f, 0.85f, 0.35f, 0.9f) },
                },
                new AdvancedSpec
                {
                    Name = "Bastion", NameJa = "城壁",
                    Desc = "Armor +40, max HP +30%.", DescJa = "防御+40、最大体力+30%。",
                    Passive = new BoonSpec { Armor = 40f, HpPct = 0.3f },
                    Skill = new SkillSpec { Name = "Iron Vigil", NameJa = "鉄の守り", Desc = "Armor +120, damage taken -50% for 8s.", DescJa = "8秒、防御+120・被ダメージ-50%。",
                        ReqLevel = 1, Mp = 25, Cooldown = 20f, Effect = SkillEffect.Buff, Duration = 8f, BuffArmor = 120f, BuffTaken = 0.5f,
                        Color = new Color(0.75f, 0.8f, 0.9f, 0.9f) },
                },
            },
        };

        /// <summary>まだ転職していないときの7つ目のスキルの欄（覚えられない）。</summary>
        static readonly SkillSpec locked = new SkillSpec
        {
            Name = "(Advanced class skill)", NameJa = "（上位職のスキル）",
            Desc = "Change class at the Master in town (Lv 30).", DescJa = "町の師匠で転職すると覚える（Lv30から）。",
            ReqLevel = ReqLevel, Mp = 0, Cooldown = 1f, Effect = SkillEffect.Nova, Color = new Color(0.6f, 0.6f, 0.6f, 0.9f),
        };

        public static AdvancedSpec Get(ClassType type, int index) => table[(int)type][Mathf.Clamp(index, 0, 1)];

        /// <summary>そのキャラクターの上位職（まだなら null）。</summary>
        public static AdvancedSpec Of(ClassType type, CharacterData ch) => ch != null && ch.Advanced >= 0 ? Get(type, ch.Advanced) : null;

        public static SkillSpec SkillFor(ClassType type)
        {
            var adv = Of(type, Profile.Get(type));
            return adv != null ? adv.Skill : locked;
        }

        /// <summary>そのキャラクターの職業の名前（3次職・上位職・元の職業の順）。</summary>
        public static string NameFor(CharacterData ch)
        {
            var type = (ClassType)ch.ClassId;
            var third = ThirdClasses.Of(type, ch);
            if (third != null) return third.DisplayName;
            var adv = Of(type, ch);
            return adv != null ? adv.DisplayName : ClassInfo.Get(type).DisplayName;
        }

        /// <summary>画面に出す職業の名前（転職していれば上位職の名前）。</summary>
        public static string NameOf(ClassType type)
        {
            var ch = Profile.Get(type);
            var third = ThirdClasses.Of(type, ch);
            if (third != null) return third.DisplayName;
            var adv = Of(type, ch);
            return adv != null ? adv.DisplayName : ClassInfo.Get(type).DisplayName;
        }

        public static bool CanChange(CharacterData ch) => ch != null && ch.Advanced < 0 && ch.Level >= ReqLevel && ch.Embers >= Cost;
    }
}
