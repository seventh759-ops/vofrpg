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
        public int MaxHp;
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
        public AttackKind Attack;
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
                Type = ClassType.Lampwright, Name = "Lampwright",
                Line1 = "Hurls fire from afar.",
                Line2 = "Stronger with wide light.",
                MaxHp = 100, Speed = 4.5f, BaseLight = 3f, Crit = 0.05f, Armor = 0f,
                Starter = WeaponKind.Wand,
                Weapons = new[] { WeaponKind.Wand, WeaponKind.Repeater, WeaponKind.Fan },
            },
            new ClassSpec
            {
                Type = ClassType.Shadeblade, Name = "Shadeblade",
                Line1 = "Cuts down crowds up close.",
                Line2 = "Fast, sharp, fragile.",
                MaxHp = 120, Speed = 5.2f, BaseLight = 2.5f, Crit = 0.1f, Armor = 5f,
                Starter = WeaponKind.Sword,
                Weapons = new[] { WeaponKind.Sword, WeaponKind.Dagger, WeaponKind.Greatblade },
            },
            new ClassSpec
            {
                Type = ClassType.Warden, Name = "Warden",
                Line1 = "Circling lights burn all near.",
                Line2 = "Slow but very tough.",
                MaxHp = 160, Speed = 4f, BaseLight = 3f, Crit = 0.05f, Armor = 20f,
                Starter = WeaponKind.Censer,
                Weapons = new[] { WeaponKind.Censer, WeaponKind.Chain, WeaponKind.Brazier },
            },
        };

        static readonly Dictionary<WeaponKind, WeaponSpec> weapons = new Dictionary<WeaponKind, WeaponSpec>
        {
            // 灯術師：火の玉
            { WeaponKind.Wand, new WeaponSpec { Kind = WeaponKind.Wand, Class = ClassType.Lampwright, Name = "Wand", Desc = "Piercing shot",
                Attack = AttackKind.Projectile, BaseDamage = 10f, Interval = 0.18f, Pierce = 1 } },
            { WeaponKind.Repeater, new WeaponSpec { Kind = WeaponKind.Repeater, Class = ClassType.Lampwright, Name = "Repeater", Desc = "Rapid shots",
                Attack = AttackKind.Projectile, BaseDamage = 6f, Interval = 0.11f, Mult = 0.6f } },
            { WeaponKind.Fan, new WeaponSpec { Kind = WeaponKind.Fan, Class = ClassType.Lampwright, Name = "Fan Staff", Desc = "3-way shots",
                Attack = AttackKind.Projectile, BaseDamage = 7f, Interval = 0.3f, Mult = 0.8f, Shots = 3, Spread = 22f } },

            // 影刃：斬撃
            { WeaponKind.Sword, new WeaponSpec { Kind = WeaponKind.Sword, Class = ClassType.Shadeblade, Name = "Sword", Desc = "Wide slash",
                Attack = AttackKind.Slash, BaseDamage = 14f, Interval = 0.35f, Reach = 1.6f, Arc = 120f } },
            { WeaponKind.Dagger, new WeaponSpec { Kind = WeaponKind.Dagger, Class = ClassType.Shadeblade, Name = "Dagger", Desc = "Quick stabs, more crits",
                Attack = AttackKind.Slash, BaseDamage = 8f, Interval = 0.18f, Reach = 1.25f, Arc = 70f, CritBonus = 0.1f } },
            { WeaponKind.Greatblade, new WeaponSpec { Kind = WeaponKind.Greatblade, Class = ClassType.Shadeblade, Name = "Greatblade", Desc = "Slow, huge sweep",
                Attack = AttackKind.Slash, BaseDamage = 26f, Interval = 0.6f, Reach = 2f, Arc = 180f } },

            // 灯番：回る灯り・周りを焼く
            { WeaponKind.Censer, new WeaponSpec { Kind = WeaponKind.Censer, Class = ClassType.Warden, Name = "Censer", Desc = "2 circling flames",
                Attack = AttackKind.Orbit, BaseDamage = 8f, Interval = 0.25f, Orbs = 2, OrbitSpeed = 3f } },
            { WeaponKind.Chain, new WeaponSpec { Kind = WeaponKind.Chain, Class = ClassType.Warden, Name = "Chain Lamps", Desc = "4 fast flames",
                Attack = AttackKind.Orbit, BaseDamage = 5f, Interval = 0.2f, Orbs = 4, OrbitSpeed = 4.5f } },
            { WeaponKind.Brazier, new WeaponSpec { Kind = WeaponKind.Brazier, Class = ClassType.Warden, Name = "Brazier", Desc = "Burning pulse around you",
                Attack = AttackKind.Pulse, BaseDamage = 12f, Interval = 0.5f } },
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
