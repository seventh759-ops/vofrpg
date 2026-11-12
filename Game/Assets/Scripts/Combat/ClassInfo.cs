using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>職業。灯術師・影刃・灯番。</summary>
    public enum ClassType { Lampwright, Shadeblade, Warden }

    /// <summary>通常攻撃の形。</summary>
    public enum AttackKind { Projectile, Slash, Orbit, Pulse }

    /// <summary>職業ごとの基本の能力と、使える武器。</summary>
    public sealed class ClassSpec
    {
        public ClassType Type;
        public string Name;
        public string Line1;
        public string Line2;
        public string NameJa, Line1Ja, Line2Ja;

        public string DisplayName => Loc.T(Name, NameJa);
        public string DisplayLine1 => Loc.T(Line1, Line1Ja);
        public string DisplayLine2 => Loc.T(Line2, Line2Ja);
        public int BaseHp;              // 体力の土台（VIT とレベルで増える）
        public int[] BaseStats;         // 能力値の初期値（STR, DEX, INT, VIT, WIS）
        public Attr MainAttr;           // 攻撃力が伸びる能力値（1ポイントで +2%）。武器の必要条件にも使う
        public Attr SubAttr;            // 攻撃力が少し伸びる能力値（1ポイントで +1%）
        public float DamageTaken = 1f;  // 受けるダメージの倍率
        public float DamageMult = 1f;   // 与えるダメージの倍率（遠くから安全に戦える職業は低め）
        public float DodgeCooldownMult = 1f;
        public float Speed;
        public float BaseLight;
        public float Crit;
        public float Armor;
        public WeaponKind Starter;
        public WeaponKind[] Weapons;
    }

    /// <summary>武器の種類ごとの攻撃の形と基本の数値。</summary>
    public sealed class WeaponSpec
    {
        public WeaponKind Kind;
        public ClassType Class;
        public string Name;
        public string Desc;
        public string NameJa, DescJa;
        public AttackKind Attack;

        public string DisplayName => Loc.T(Name, NameJa);
        public string DisplayDesc => Loc.T(Desc, DescJa);
        public float BaseDamage;
        public float Interval;          // 攻撃の間隔（秒）。回る灯りは当たり判定の間隔
        public float Mult = 1f;
        public int Pierce;              // 火の玉が突き抜ける数
        public int Shots = 1;           // 火の玉の数
        public float Spread;            // 火の玉どうしの角度
        public float Reach;             // 斬撃の届く距離（マス）
        public float Arc;               // 斬撃の広さ（度）
        public int Orbs;                // 回る灯りの数
        public float OrbitSpeed;        // 回る速さ（ラジアン/秒）
        public float CritBonus;
        public int MaxTargets = 1;      // 斬撃が一度に当たる数（近い順）。2体目以降は威力が下がる

        /// <summary>この武器を使う職業の主能力。</summary>
        public Attr ClassAttr() => ClassInfo.Get(Class).MainAttr;
    }

    /// <summary>職業と武器の表。数値はすべて仮で、遊んでから調整する。</summary>
    public static class ClassInfo
    {
        /// <summary>今の挑戦の職業。ドロップする武器や効果の表示に使う。</summary>
        public static ClassType Current = ClassType.Lampwright;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Current = ClassType.Lampwright;

        static readonly ClassSpec[] classes =
        {
            new ClassSpec
            {
                Type = ClassType.Lampwright, Name = "Lampwright", NameJa = "灯術師", Line1Ja = "遠くから火を放つ。", Line2Ja = "灯りが広いほど強い。",
                Line1 = "Hurls fire from afar.",
                Line2 = "Stronger with wide light.",
                BaseHp = 50, BaseStats = new[] { 4, 6, 12, 6, 8 }, MainAttr = Attr.Int, SubAttr = Attr.Wis,
                Speed = 4.5f, BaseLight = 3f, Crit = 0.05f, Armor = 0f,
                Starter = WeaponKind.Wand,
                Weapons = new[] { WeaponKind.Wand, WeaponKind.Repeater, WeaponKind.Fan },
            },
            new ClassSpec
            {
                Type = ClassType.Shadeblade, Name = "Shadeblade", NameJa = "影刃", Line1Ja = "近くの群れをまとめて斬る。", Line2Ja = "速く鋭いが、打たれ弱い。",
                Line1 = "Cuts down crowds up close.",
                Line2 = "Fast, sharp, fragile.",
                BaseHp = 60, BaseStats = new[] { 12, 10, 3, 7, 4 }, MainAttr = Attr.Str, SubAttr = Attr.Dex,
                DamageTaken = 0.8f, DodgeCooldownMult = 0.7f, // 近接で張り付く分、打たれ強く、よく避けられる
                Speed = 5.2f, BaseLight = 2.5f, Crit = 0.1f, Armor = 0f,
                Starter = WeaponKind.Sword,
                Weapons = new[] { WeaponKind.Sword, WeaponKind.Dagger, WeaponKind.Greatblade },
            },
            new ClassSpec
            {
                Type = ClassType.Warden, Name = "Warden", NameJa = "灯番", Line1Ja = "周りを回る灯りで焼き払う。", Line2Ja = "足は遅いが、とても頑丈。",
                Line1 = "Circling lights burn all near.",
                Line2 = "Slow but very tough.",
                BaseHp = 80, BaseStats = new[] { 6, 4, 6, 12, 10 }, MainAttr = Attr.Wis, SubAttr = Attr.Vit,
                Speed = 4.4f, BaseLight = 3f, Crit = 0.05f, Armor = 15f,
                Starter = WeaponKind.Censer,
                Weapons = new[] { WeaponKind.Censer, WeaponKind.Chain, WeaponKind.Brazier },
            },
        };

        static readonly Dictionary<WeaponKind, WeaponSpec> weapons = new Dictionary<WeaponKind, WeaponSpec>
        {
            // 灯術師：火の玉
            { WeaponKind.Wand, new WeaponSpec { Kind = WeaponKind.Wand, Class = ClassType.Lampwright, Name = "Wand", NameJa = "杖", DescJa = "貫通する火の玉", Desc = "Piercing shot",
                Attack = AttackKind.Projectile, BaseDamage = 10f, Interval = 0.18f, Pierce = 1 } },
            { WeaponKind.Repeater, new WeaponSpec { Kind = WeaponKind.Repeater, Class = ClassType.Lampwright, Name = "Repeater", NameJa = "連射杖", DescJa = "すばやい連射", Desc = "Rapid shots",
                Attack = AttackKind.Projectile, BaseDamage = 6f, Interval = 0.11f, Mult = 0.6f } },
            { WeaponKind.Fan, new WeaponSpec { Kind = WeaponKind.Fan, Class = ClassType.Lampwright, Name = "Fan Staff", NameJa = "扇杖", DescJa = "3方向に撃つ", Desc = "3-way shots",
                Attack = AttackKind.Projectile, BaseDamage = 7f, Interval = 0.3f, Mult = 0.8f, Shots = 3, Spread = 22f } },

            // 影刃：斬撃
            { WeaponKind.Sword, new WeaponSpec { Kind = WeaponKind.Sword, Class = ClassType.Shadeblade, Name = "Sword", NameJa = "剣", DescJa = "広く斬る", Desc = "Wide slash",
                Attack = AttackKind.Slash, BaseDamage = 12f, Interval = 0.4f, Reach = 1.8f, Arc = 120f, MaxTargets = 4 } },
            { WeaponKind.Dagger, new WeaponSpec { Kind = WeaponKind.Dagger, Class = ClassType.Shadeblade, Name = "Dagger", NameJa = "短剣", DescJa = "すばやく突く。会心が出やすい", Desc = "Quick stabs, more crits",
                Attack = AttackKind.Slash, BaseDamage = 8f, Interval = 0.2f, Reach = 1.5f, Arc = 70f, CritBonus = 0.1f, MaxTargets = 2 } },
            { WeaponKind.Greatblade, new WeaponSpec { Kind = WeaponKind.Greatblade, Class = ClassType.Shadeblade, Name = "Greatblade", NameJa = "大剣", DescJa = "遅いが、大きく薙ぎ払う", Desc = "Slow, huge sweep",
                Attack = AttackKind.Slash, BaseDamage = 24f, Interval = 0.7f, Reach = 2.2f, Arc = 180f, MaxTargets = 6 } },

            // 灯番：回る灯り・周りを焼く
            { WeaponKind.Censer, new WeaponSpec { Kind = WeaponKind.Censer, Class = ClassType.Warden, Name = "Censer", NameJa = "香炉", DescJa = "回る灯り2つ", Desc = "2 circling flames",
                Attack = AttackKind.Orbit, BaseDamage = 11f, Interval = 0.25f, Orbs = 2, OrbitSpeed = 3f } },
            { WeaponKind.Chain, new WeaponSpec { Kind = WeaponKind.Chain, Class = ClassType.Warden, Name = "Chain Lamps", NameJa = "鎖灯", DescJa = "速く回る灯り4つ", Desc = "4 fast flames",
                Attack = AttackKind.Orbit, BaseDamage = 7f, Interval = 0.2f, Orbs = 4, OrbitSpeed = 4.5f } },
            { WeaponKind.Brazier, new WeaponSpec { Kind = WeaponKind.Brazier, Class = ClassType.Warden, Name = "Brazier", NameJa = "火鉢", DescJa = "周りを一度に焼く", Desc = "Burning pulse around you",
                Attack = AttackKind.Pulse, BaseDamage = 15f, Interval = 0.5f } },
        };

        public static ClassSpec Get(ClassType type) => classes[(int)type];
        public static ClassSpec CurrentSpec => Get(Current);
        public static int Count => classes.Length;

        public static WeaponSpec Weapon(WeaponKind kind)
        {
            return weapons.TryGetValue(kind, out var spec) ? spec : weapons[WeaponKind.Wand];
        }
    }
}
