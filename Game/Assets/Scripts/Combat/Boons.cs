using UnityEngine;

namespace VividOfFade
{
    /// <summary>祭壇で火種を払って得る加護（その挑戦のあいだだけ、ずっと効く。重ねがけできる）。</summary>
    public sealed class BoonSpec
    {
        public string Name;
        public string Desc;
        public float Damage;       // +割合
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
    }

    public static class Boons
    {
        public const int OffersPerAltar = 3;

        static readonly BoonSpec[] table =
        {
            new BoonSpec { Name = "Sharp Flame", Desc = "Damage +10%", Damage = 0.1f },
            new BoonSpec { Name = "Quickening", Desc = "Attack speed +10%", AttackSpeed = 0.1f },
            new BoonSpec { Name = "Kindled Wick", Desc = "Light +1", Light = 1f },
            new BoonSpec { Name = "Keen Eye", Desc = "Crit chance +5%", Crit = 0.05f },
            new BoonSpec { Name = "Ruin", Desc = "Crit damage +25%", CritDamage = 0.25f },
            new BoonSpec { Name = "Vigor", Desc = "Max HP +15%", HpPct = 0.15f },
            new BoonSpec { Name = "Deep Well", Desc = "Max MP +20%, MP regen +1/s", MpPct = 0.2f, MpRegen = 1f },
            new BoonSpec { Name = "Swift Step", Desc = "Move speed +8%", Move = 0.08f },
            new BoonSpec { Name = "Bulwark", Desc = "Armor +15", Armor = 15f },
            new BoonSpec { Name = "Leech", Desc = "Heal 0.5% of damage dealt", LifeSteal = 0.005f },
            new BoonSpec { Name = "Echo", Desc = "+1 projectile / flame / slash target", Extra = 1 },
            new BoonSpec { Name = "Ember Burst", Desc = "Enemies explode on death (weak)", Explode = 0.3f },
        };

        public static int Count => table.Length;
        public static BoonSpec Get(int index) => table[Mathf.Clamp(index, 0, table.Length - 1)];

        /// <summary>祭壇で1つ選ぶときの値段（火種）。深い層ほど高い。</summary>
        public static int Cost(int floor) => 25 + floor * 12;
    }

    public sealed partial class PlayerStats
    {
        /// <summary>加護を足した能力を作る。元の能力は変えない。</summary>
        public PlayerStats WithBoon(BoonSpec b)
        {
            var s = (PlayerStats)MemberwiseClone();
            s.Damage *= 1f + b.Damage;
            s.FireInterval /= 1f + b.AttackSpeed;
            s.CritChance = Mathf.Min(s.CritChance + b.Crit, 0.75f);
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
