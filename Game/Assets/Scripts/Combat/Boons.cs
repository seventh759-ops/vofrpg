using UnityEngine;

namespace VividOfFade
{
    /// <summary>祭壇で火種を払って得る加護（その挑戦のあいだだけ、ずっと効く。重ねがけできる）。</summary>
    public sealed class BoonSpec
    {
        public string Name;
        public string Desc;
        public string NameJa, DescJa;
        public float Damage;       // +割合

        public string DisplayName => Loc.T(Name, NameJa);
        public string DisplayDesc => Loc.T(Desc, DescJa);
        public float AttackSpeed;  // +割合
        public float Crit;
        public float CritDamage;
        public float HpPct;
        public float MpPct;
        public float MpRegen;
        public float Move;         // +割合
        public float Armor;
        public float Light;
        public float LifeSteal;
        public float Explode;
        public int Extra;          // 弾・灯りの数など（職業ごとの「弾の数」の効果）
        /// <summary>
        /// 引退した加護：祭壇には出ず、持っていても効かない（一覧にも出さない）。
        /// 表の並びは保存データとアイコンの番号なので、消さずに印だけ付ける。
        /// </summary>
        public bool Retired;
    }

    public static class Boons
    {
        public const int OffersPerAltar = 3;

        static readonly BoonSpec[] table =
        {
            new BoonSpec { Name = "Sharp Flame", NameJa = "鋭い炎", DescJa = "ダメージ+10%", Desc = "Damage +10%", Damage = 0.1f },
            new BoonSpec { Name = "Quickening", NameJa = "加速", DescJa = "攻撃速度+10%", Desc = "Attack speed +10%", AttackSpeed = 0.1f, Retired = true },
            new BoonSpec { Name = "Kindled Wick", NameJa = "灯芯", DescJa = "明るさ+1", Desc = "Light +1", Light = 1f },
            new BoonSpec { Name = "Keen Eye", NameJa = "鋭い目", DescJa = "会心率+5%", Desc = "Crit chance +5%", Crit = 0.05f },
            new BoonSpec { Name = "Ruin", NameJa = "破滅", DescJa = "会心ダメージ+25%", Desc = "Crit damage +25%", CritDamage = 0.25f },
            new BoonSpec { Name = "Vigor", NameJa = "活力", DescJa = "最大HP+15%", Desc = "Max HP +15%", HpPct = 0.15f },
            new BoonSpec { Name = "Deep Well", NameJa = "深き泉", DescJa = "最大MP+20%、MP回復+1/秒", Desc = "Max MP +20%, MP regen +1/s", MpPct = 0.2f, MpRegen = 1f },
            new BoonSpec { Name = "Swift Step", NameJa = "軽い足取り", DescJa = "移動速度+8%", Desc = "Move speed +8%", Move = 0.08f },
            new BoonSpec { Name = "Bulwark", NameJa = "防壁", DescJa = "防御+15", Desc = "Armor +15", Armor = 15f },
            new BoonSpec { Name = "Leech", NameJa = "吸収", DescJa = "与えたダメージの0.5%を回復", Desc = "Heal 0.5% of damage dealt", LifeSteal = 0.005f },
            new BoonSpec { Name = "Echo", NameJa = "こだま", DescJa = "弾・灯り・斬る数+1", Desc = "+1 projectile / flame / slash target", Extra = 1, Retired = true },
            new BoonSpec { Name = "Ember Burst", NameJa = "残り火の破裂", DescJa = "倒した敵が爆発する（弱め）", Desc = "Enemies explode on death (weak)", Explode = 0.3f },
        };

        public static int Count => table.Length;
        public static BoonSpec Get(int index) => table[Mathf.Clamp(index, 0, table.Length - 1)];

        /// <summary>
        /// 祭壇に出せる加護か。「加速」（攻撃速度）と「こだま」（弾・灯り・斬る数）は、重ねると通常攻撃だけが
        /// 掛け算で伸びてスキルとの差が開きすぎるため外した。
        /// </summary>
        public static bool Offerable(int index) => index >= 0 && index < table.Length && !table[index].Retired;

        /// <summary>祭壇で1つ選ぶときの値段（火種）。深い層ほど高い。</summary>
        public static int Cost(int floor) => 25 + floor * 12;
    }

    public sealed partial class PlayerStats
    {
        /// <summary>加護を足した能力を作る。元の能力は変えない。</summary>
        public PlayerStats WithBoon(BoonSpec b)
        {
            if (b.Retired) return this;
            var s = (PlayerStats)MemberwiseClone();
            s.Damage *= 1f + b.Damage;
            s.FireInterval /= 1f + b.AttackSpeed;
            s.CritChance = Mathf.Min(s.CritChance + b.Crit, 1f); // 会心率の上限は 100%（会心ビルドができるように、75% の上限はなくした）
            s.CritMult += b.CritDamage;
            s.MaxHp = Mathf.RoundToInt(s.MaxHp * (1f + b.HpPct));
            s.MaxMp = Mathf.RoundToInt(s.MaxMp * (1f + b.MpPct));
            s.MpRegen += b.MpRegen;
            s.MoveSpeed *= 1f + b.Move;
            s.Armor += b.Armor;
            s.Light = Mathf.Min(s.Light + b.Light, GameConfig.MaxLight + 2f);
            s.LifeSteal += b.LifeSteal;
            s.ExplodeMult += b.Explode;
            if (b.Extra > 0)
            {
                s.Shots += b.Extra;
                if (s.SpreadDeg <= 0f) s.SpreadDeg = 12f;
                s.Orbs += b.Extra;
                s.ArcDeg = Mathf.Min(s.ArcDeg + 15f * b.Extra, 300f);
                s.MaxTargets += b.Extra;
                s.PulseRadius += 0.3f * b.Extra;
            }
            return s;
        }
    }
}
