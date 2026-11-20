using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>職業。灯術師・影刃・灯番。</summary>
    // 【セーブの決まり】キャラクターの職業は番号で入る。足すときは最後に。
    public enum ClassType { Lampwright, Shadeblade, Warden, Vividbearer } // Vividbearer（彩灯者）は、作るときには選べない特別な職（基本職のまま Lv99 で転生するとなる）

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
        /// <summary>灯りの適性（職業ごとの、灯りとの関わり方。フレーバーの文）。</summary>
        public string Aptitude, AptitudeJa;
        public string DisplayAptitude => Loc.T(Aptitude, AptitudeJa);
        public int BaseHp;              // 体力の土台（VIT とレベルで増える）
        public int[] BaseStats;         // 能力値の初期値（STR, DEX, INT, VIT, WIS）
        public Attr MainAttr;           // 攻撃力が伸びる能力値（1ポイントで +2%）。武器の必要条件にも使う
        public Attr SubAttr;            // 攻撃力が少し伸びる能力値（1ポイントで +1%）
        /// <summary>
        /// 装備の判定で、守り（実際に耐えられる量）をどれだけ重く数えるか（耐えられる量の何乗を掛けるか）。
        /// 遠くから戦う職業は軽く、受けて耐える職業は重く。
        /// </summary>
        public float DefenseWeight = 0.5f;
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

        /// <summary>この武器だけ攻撃力が伸びる能力値を変えるとき（例：短剣は DEX が主・STR が副）。なければ職業のもの。</summary>
        public Attr? Main, Sub;

        /// <summary>この武器で攻撃力が伸びる主能力（1ポイントで +2%）。武器の必要条件にも使う。</summary>
        public Attr MainAttr => Main ?? ClassInfo.Get(Class).MainAttr;

        /// <summary>この武器で攻撃力が少し伸びる副能力（1ポイントで +1%）。</summary>
        public Attr SubAttr => Sub ?? ClassInfo.Get(Class).SubAttr;

        /// <summary>この武器の必要条件に使う能力値（主能力）。</summary>
        public Attr ClassAttr() => MainAttr;
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
                AptitudeJa = "灯りを炎に変え、遠くへ放つ適性。灯りが広いほど、その炎は遠くまで届く。",
                Aptitude = "An aptitude for turning light into flame and casting it afar. The wider the light, the farther the flame reaches.",
                BaseHp = 50, BaseStats = new[] { 4, 6, 12, 6, 8 }, MainAttr = Attr.Int, SubAttr = Attr.Wis, DefenseWeight = 0.35f,
                Speed = 4.5f, BaseLight = 3f, Crit = 0.05f, Armor = 0f,
                Starter = WeaponKind.Wand,
                Weapons = new[] { WeaponKind.Wand, WeaponKind.Repeater, WeaponKind.Fan },
            },
            new ClassSpec
            {
                Type = ClassType.Shadeblade, Name = "Shadeblade", NameJa = "影刃", Line1Ja = "近くの群れをまとめて斬る。", Line2Ja = "速く鋭いが、打たれ弱い。",
                Line1 = "Cuts down crowds up close.",
                Line2 = "Fast, sharp, fragile.",
                AptitudeJa = "灯りが落とす影をまとい、刃に変える適性。灯りのそばで、影はもっとも鋭くなる。",
                Aptitude = "An aptitude for wearing the shadow that light casts, and turning it into a blade. Beside the light, the shadow is sharpest.",
                BaseHp = 60, BaseStats = new[] { 12, 10, 3, 7, 4 }, MainAttr = Attr.Str, SubAttr = Attr.Dex, DefenseWeight = 0.45f,
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
                AptitudeJa = "灯りを身のまわりに留め、守り続ける適性。その灯りは、近づくものを焼き払う。",
                Aptitude = "An aptitude for keeping light close and guarding it. That light burns away whatever draws near.",
                BaseHp = 80, BaseStats = new[] { 6, 4, 6, 12, 10 }, MainAttr = Attr.Wis, SubAttr = Attr.Vit, DefenseWeight = 0.6f,
                Speed = 4.4f, BaseLight = 3f, Crit = 0.05f, Armor = 15f,
                Starter = WeaponKind.Censer,
                Weapons = new[] { WeaponKind.Censer, WeaponKind.Chain, WeaponKind.Brazier },
            },
            // 特別な職：彩灯者（作るときには選べない。基本職のまま Lv99 で転生するとなる）。どの職業の武器も使い、遠近両用
            new ClassSpec
            {
                Type = ClassType.Vividbearer, Name = "Vividbearer", NameJa = "彩灯者", Line1Ja = "どの武器も使い、遠くも近くも戦う。", Line2Ja = "転職せずに転生した者だけがなれる。",
                Line1 = "Wields any weapon, near or far.",
                Line2 = "Only the reborn who never changed class.",
                AptitudeJa = "あらゆる色の灯りを受け入れる適性。どの灯りにも染まらなかった者だけが持つ。",
                Aptitude = "An aptitude for accepting light of every color. Only one never dyed by any single light holds it.",
                BaseHp = 65, BaseStats = new[] { 8, 9, 8, 9, 8 }, MainAttr = Attr.Dex, SubAttr = Attr.Wis, DefenseWeight = 0.45f,
                DodgeCooldownMult = 0.85f,
                Speed = 4.8f, BaseLight = 3.2f, Crit = 0.07f, Armor = 5f,
                Starter = WeaponKind.Wand,
                Weapons = new[] { WeaponKind.Wand, WeaponKind.Repeater, WeaponKind.Fan, WeaponKind.Sword, WeaponKind.Dagger, WeaponKind.Greatblade, WeaponKind.Censer, WeaponKind.Chain, WeaponKind.Brazier },
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
            { WeaponKind.Dagger, new WeaponSpec { Kind = WeaponKind.Dagger, Class = ClassType.Shadeblade, Name = "Dagger", NameJa = "短剣", DescJa = "すばやく突く。会心が出やすい。DEX で強くなる", Desc = "Quick stabs, more crits. Scales with DEX",
                Attack = AttackKind.Slash, BaseDamage = 8f, Interval = 0.2f, Reach = 1.5f, Arc = 70f, CritBonus = 0.1f, MaxTargets = 2, Main = Attr.Dex, Sub = Attr.Str } },
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

        static WeaponKind[] allWeapons;
        /// <summary>どの職業の武器も合わせた一覧（彩灯者に落ちる武器）。</summary>
        public static WeaponKind[] AllWeapons
        {
            get
            {
                if (allWeapons != null) return allWeapons;
                var list = new System.Collections.Generic.List<WeaponKind>();
                for (int i = 0; i < PlayableCount; i++) foreach (var w in classes[i].Weapons) if (!list.Contains(w)) list.Add(w);
                allWeapons = list.ToArray();
                return allWeapons;
            }
        }
        public static int Count => classes.Length;
        /// <summary>作るときに選べる職業の数（彩灯者は除く）。</summary>
        public const int PlayableCount = 3;

        public static WeaponSpec Weapon(WeaponKind kind)
        {
            return weapons.TryGetValue(kind, out var spec) ? spec : weapons[WeaponKind.Wand];
        }
    }
}
