using UnityEngine;

namespace VividOfFade
{
    /// <summary>装備から計算したプレイヤーの能力。装備が変わるたびに作り直す。</summary>
    public sealed class PlayerStats
    {
        public float Damage;
        public float FireInterval;
        public float CritChance;
        public float CritMult;
        public float MoveSpeed;
        public int MaxHp;
        public float Armor;
        public float Light;       // 明るさ＝通常攻撃が届く範囲（マス）
        public int Pierce;
        public int Shots;
        public float SpreadDeg;   // 弾と弾の間の角度
        public float ExplodeMult; // 撃破時の爆発（攻撃力に対する倍率）
        public int HealOnKill;

        public float AttacksPerSecond => 1f / FireInterval;

        /// <summary>装備の良し悪しを比べるための大まかな強さ。</summary>
        public float Power
        {
            get
            {
                float dps = Damage * (1f + CritChance * (CritMult - 1f)) * Shots / FireInterval * (1f + 0.15f * Pierce);
                float survive = 1f + (MaxHp - GameConfig.PlayerMaxHp) * 0.004f + Armor * 0.002f;
                float reach = 1f + (Light - 5f) * 0.05f;
                return dps * survive * reach * (1f + ExplodeMult * 0.5f);
            }
        }

        public static PlayerStats From(Item[] equipped)
        {
            var s = new PlayerStats
            {
                CritChance = 0.05f,
                CritMult = 1.5f,
                MaxHp = GameConfig.PlayerMaxHp,
                Light = GameConfig.BaseLight,
            };

            float baseDamage = 6f; // 武器なし
            var kind = WeaponKind.None;
            float dmgPct = 0f, atkSpd = 0f, moveSpd = 0f;
            int pierce = 0, extraShots = 0;

            foreach (var item in equipped)
            {
                if (item == null) continue;
                switch (item.Slot)
                {
                    case ItemSlot.Weapon:
                        kind = item.Weapon;
                        baseDamage = item.BaseValue;
                        break;
                    case ItemSlot.Lantern:
                        s.Light += item.BaseValue;
                        break;
                    case ItemSlot.Armor:
                        s.MaxHp += Mathf.RoundToInt(item.BaseValue);
                        break;
                }

                foreach (var a in item.Affixes)
                {
                    switch (a.Stat)
                    {
                        case StatType.DamagePct: dmgPct += a.Value; break;
                        case StatType.AttackSpeedPct: atkSpd += a.Value; break;
                        case StatType.CritChance: s.CritChance += a.Value; break;
                        case StatType.CritDamage: s.CritMult += a.Value; break;
                        case StatType.MoveSpeedPct: moveSpd += a.Value; break;
                        case StatType.MaxHp: s.MaxHp += Mathf.RoundToInt(a.Value); break;
                        case StatType.Armor: s.Armor += a.Value; break;
                        case StatType.LightRadius: s.Light += a.Value; break;
                        case StatType.Pierce: pierce += Mathf.RoundToInt(a.Value); break;
                        case StatType.Multishot: extraShots += Mathf.RoundToInt(a.Value); break;
                    }
                }

                foreach (var u in item.Uniques)
                {
                    switch (u)
                    {
                        case UniqueType.ExplodeOnKill: s.ExplodeMult += 0.6f; break;
                        case UniqueType.HealOnKill: s.HealOnKill += 1; break;
                        case UniqueType.SplitShot: extraShots += 2; break;
                    }
                }
            }

            // 武器の種類ごとの基本の形
            float interval, kindMult, kindSpread;
            int kindPierce, kindShots;
            switch (kind)
            {
                case WeaponKind.Repeater:
                    interval = 0.11f; kindMult = 0.6f; kindPierce = 0; kindShots = 1; kindSpread = 0f;
                    break;
                case WeaponKind.Fan:
                    interval = 0.3f; kindMult = 0.8f; kindPierce = 0; kindShots = 3; kindSpread = 22f;
                    break;
                default: // Wand / 武器なし
                    interval = 0.18f; kindMult = 1f; kindPierce = 1; kindShots = 1; kindSpread = 0f;
                    break;
            }

            s.Damage = baseDamage * kindMult * (1f + dmgPct);
            s.FireInterval = interval / (1f + atkSpd);
            s.MoveSpeed = GameConfig.PlayerSpeed * (1f + moveSpd);
            s.Pierce = kindPierce + pierce;
            s.Shots = kindShots + extraShots;
            s.SpreadDeg = s.Shots > 1 ? (kindSpread > 0f ? kindSpread : 12f) : 0f;
            s.CritChance = Mathf.Min(s.CritChance, 0.75f);
            s.Light = Mathf.Min(s.Light, GameConfig.MaxLight);
            return s;
        }
    }
}
