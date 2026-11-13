using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 転生（3次職）。上位職で Lv99 になり、塔の100層にたどり着いたら、町の師匠で火種を払って転生できる（1回だけ）。
    /// 上位職ごとに決まった3次職になる。Lv1 に戻り、ボーナスのポイントを振れる。
    /// 3次職の補正（上位職の補正に重なる）、全能力値+10（ずっと）、8つ目のスキル、見た目と名前が変わる。
    /// どれもこのゲーム用のオリジナルの名前。数値はすべて仮で、遊んでから調整する。
    /// </summary>
    public static class ThirdClasses
    {
        public const int ReqLevel = 99;
        public const int ReqFloor = 100;
        public const int Cost = 2000;        // 火種
        public const int BonusPoints = 60;
        public const int AllStats = 10;      // 全能力値にずっと足す
        public const int SkillIndex = 7;     // 8つ目のスキルの番号

        // [職業][上位職]
        static readonly AdvancedSpec[][] table =
        {
            new[]
            {
                new AdvancedSpec
                {
                    Name = "Pyrelord", NameJa = "焔王",
                    Desc = "Damage +25%, crit damage +30%, bigger explosions on kill.", DescJa = "ダメージ+25%、会心ダメージ+30%、倒した敵の爆発が大きくなる。",
                    Passive = new BoonSpec { Damage = 0.25f, CritDamage = 0.3f, Explode = 0.5f },
                    Skill = new SkillSpec { Name = "Cataclysm", NameJa = "天変", Desc = "A vast eruption of fire around you.", DescJa = "周りに巨大な炎を噴き上げる。",
                        ReqLevel = 1, Mp = 50, Cooldown = 12f, Effect = SkillEffect.Nova, Mult = 17.5f, Radius = 4.5f, Color = new Color(1f, 0.45f, 0.2f, 0.9f) },
                },
                new AdvancedSpec
                {
                    Name = "Starwarden", NameJa = "星守",
                    Desc = "Light +2, +2 projectiles, max MP +30%.", DescJa = "明るさ+2、弾+2、最大MP+30%。",
                    Passive = new BoonSpec { Light = 2f, Extra = 2, MpPct = 0.3f },
                    Skill = new SkillSpec { Name = "Constellation", NameJa = "星座", Desc = "24 stars burst out in all directions.", DescJa = "全方向に24の星を放つ。",
                        ReqLevel = 1, Mp = 45, Cooldown = 8f, Effect = SkillEffect.Barrage, Mult = 3.5f, Count = 24, AllAround = true, Color = new Color(0.8f, 0.9f, 1f, 0.9f) },
                },
            },
            new[]
            {
                new AdvancedSpec
                {
                    Name = "Eclipse", NameJa = "日蝕",
                    Desc = "Crit chance +10%, crit damage +60%, attack speed +15%.", DescJa = "会心率+10%、会心ダメージ+60%、攻撃速度+15%。",
                    Passive = new BoonSpec { Crit = 0.1f, CritDamage = 0.6f, AttackSpeed = 0.15f },
                    Skill = new SkillSpec { Name = "Umbral Storm", NameJa = "影嵐", Desc = "9 shadow blades fly at the enemy.", DescJa = "敵へ9つの影の刃を飛ばす。",
                        ReqLevel = 1, Mp = 40, Cooldown = 7f, Effect = SkillEffect.Barrage, Mult = 5.5f, Count = 9, Color = new Color(0.6f, 0.4f, 1f, 0.9f) },
                },
                new AdvancedSpec
                {
                    Name = "Bloodsovereign", NameJa = "血の君主",
                    Desc = "Heal 1.5% of damage dealt, max HP +30%, damage +15%.", DescJa = "与えたダメージの1.5%を回復、最大体力+30%、ダメージ+15%。",
                    Passive = new BoonSpec { LifeSteal = 0.015f, HpPct = 0.3f, Damage = 0.15f },
                    Skill = new SkillSpec { Name = "Sanguine Tide", NameJa = "血潮", Desc = "A wave of blood around you; heals 20%.", DescJa = "周りに血の波。体力20%回復。",
                        ReqLevel = 1, Mp = 45, Cooldown = 11f, Effect = SkillEffect.Nova, Mult = 15f, Radius = 3f, HealPct = 0.2f, Color = new Color(1f, 0.2f, 0.3f, 0.9f) },
                },
            },
            new[]
            {
                new AdvancedSpec
                {
                    Name = "Dawnbringer", NameJa = "暁光",
                    Desc = "+2 flames, damage +20%, light +1.5.", DescJa = "灯り+2、ダメージ+20%、明るさ+1.5。",
                    Passive = new BoonSpec { Extra = 2, Damage = 0.2f, Light = 1.5f },
                    Skill = new SkillSpec { Name = "Daybreak", NameJa = "夜明け", Desc = "Light floods everything around you.", DescJa = "周りすべてを光で満たす。",
                        ReqLevel = 1, Mp = 50, Cooldown = 12f, Effect = SkillEffect.Nova, Mult = 15f, Radius = 5f, Color = new Color(1f, 0.9f, 0.5f, 0.9f) },
                },
                new AdvancedSpec
                {
                    Name = "Aegis", NameJa = "神盾",
                    Desc = "Armor +80, max HP +40%, damage +10%.", DescJa = "防御+80、最大体力+40%、ダメージ+10%。",
                    Passive = new BoonSpec { Armor = 80f, HpPct = 0.4f, Damage = 0.1f },
                    Skill = new SkillSpec { Name = "Unbreakable", NameJa = "不壊", Desc = "Armor +250, damage taken -65% for 10s.", DescJa = "10秒、防御+250・被ダメージ-65%。",
                        ReqLevel = 1, Mp = 40, Cooldown = 24f, Effect = SkillEffect.Buff, Duration = 10f, BuffArmor = 250f, BuffTaken = 0.35f, Color = new Color(0.8f, 0.9f, 1f, 0.9f) },
                },
            },
        };

        static readonly SkillSpec locked = new SkillSpec
        {
            Name = "(Rebirth skill)", NameJa = "（3次職のスキル）",
            Desc = "Rebirth at the Master in town (Lv 99, floor 100).", DescJa = "町の師匠で転生すると覚える（Lv99・100層到達）。",
            ReqLevel = ReqLevel, Mp = 0, Cooldown = 1f, Effect = SkillEffect.Nova, Color = new Color(0.6f, 0.6f, 0.6f, 0.9f),
        };

        /// <summary>上位職 index から転生した3次職。</summary>
        public static AdvancedSpec Get(ClassType type, int advanced) => table[(int)type][Mathf.Clamp(advanced, 0, 1)];

        /// <summary>そのキャラクターの3次職（まだなら null）。</summary>
        public static AdvancedSpec Of(ClassType type, CharacterData ch) => ch != null && ch.Third && ch.Advanced >= 0 ? Get(type, ch.Advanced) : null;

        public static SkillSpec SkillFor(ClassType type)
        {
            var third = Of(type, Profile.Get(type));
            return third != null ? third.Skill : locked;
        }

        public static bool MeetsRequirements(CharacterData ch) =>
            ch != null && ch.Advanced >= 0 && !ch.Third && ch.Level >= ReqLevel && ch.BestFloor >= ReqFloor;

        public static bool CanRebirth(CharacterData ch) => MeetsRequirements(ch) && ch.Embers >= Cost;
    }
}
