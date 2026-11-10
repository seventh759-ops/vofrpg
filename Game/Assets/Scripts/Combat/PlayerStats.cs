using UnityEngine;

namespace VividOfFade
{
    /// <summary>職業・レベル・能力値・装備から計算したプレイヤーの能力。どれかが変わるたびに作り直す。</summary>
    public sealed class PlayerStats
    {
        public ClassType Class;
        public AttackKind Kind;
        public int Level;

        /// <summary>能力値の合計（初期値＋振ったポイント＋装備）。</summary>
        public readonly int[] Attr = new int[Attributes.Count];

        public int MaxMp;
        public float HpRegen;     // 1秒あたり
        public float MpRegen;

        public float Damage;
        public float FireInterval;
        public float CritChance;
        public float CritMult;
        public float MoveSpeed;
        public int MaxHp;
        public float Armor;
        public float Light;       // 明るさ。灯術師は火の玉の届く範囲、ほかは狙いを付ける範囲

        // 火の玉（灯術師）
        public int Pierce;
        public int Shots;
        public float SpreadDeg;

        // 斬撃（影刃）
        public float Reach;
        public float ArcDeg;

        // 回る灯り・周りを焼く（灯番）
        public int Orbs;
        public float OrbitRadius;
        public float OrbitSpeed;
        public float PulseRadius;

        public float ExplodeMult; // 撃破時の爆発（攻撃力に対する倍率）
        public int HealOnKill;

        public float AttacksPerSecond => 1f / FireInterval;

        /// <summary>1回の攻撃で何体ぶん当たりやすいか（強さの比較用の目安）。</summary>
        public float Multiplicity
        {
            get
            {
                switch (Kind)
                {
                    case AttackKind.Slash: return ArcDeg / 90f * (0.5f + Reach * 0.5f);
                    case AttackKind.Orbit: return Orbs;
                    case AttackKind.Pulse: return PulseRadius;
                    default: return Shots * (1f + 0.15f * Pierce);
                }
            }
        }

        /// <summary>装備の良し悪しを比べるための大まかな強さ。</summary>
        public float Power
        {
            get
            {
                var spec = ClassInfo.Get(Class);
                float dps = Damage * (1f + CritChance * (CritMult - 1f)) * Multiplicity / FireInterval;
                float survive = 1f + MaxHp * 0.002f + Armor * 0.002f;
                float reach = 1f + (Light - spec.BaseLight) * 0.05f;
                return dps * survive * reach * (1f + ExplodeMult * 0.5f);
            }
        }

        public static PlayerStats From(Item[] equipped) => From(equipped, ClassInfo.Current);

        public static PlayerStats From(Item[] equipped, ClassType type) => From(equipped, type, Profile.Get(type));

        public static PlayerStats From(Item[] equipped, ClassType type, CharacterData ch)
        {
            var cls = ClassInfo.Get(type);
            var s = new PlayerStats
            {
                Class = type,
                Level = ch.Level,
                CritChance = cls.Crit,
                CritMult = 1.5f,
                Light = cls.BaseLight,
                Armor = cls.Armor,
            };
            for (int i = 0; i < Attributes.Count; i++) s.Attr[i] = cls.BaseStats[i] + ch.Alloc[i];
            int bonusHp = 0, bonusMp = 0;

            float baseDamage = ClassInfo.Weapon(cls.Starter).BaseDamage * 0.6f; // 武器なし
            var kind = cls.Starter;
            float dmgPct = 0f, atkSpd = 0f, moveSpd = 0f;
            int pierce = 0, extra = 0;

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
                        bonusHp += Mathf.RoundToInt(item.BaseValue);
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
                        case StatType.MaxHp: bonusHp += Mathf.RoundToInt(a.Value); break;
                        case StatType.MaxMp: bonusMp += Mathf.RoundToInt(a.Value); break;
                        case StatType.Str: s.Attr[(int)VividOfFade.Attr.Str] += Mathf.RoundToInt(a.Value); break;
                        case StatType.Dex: s.Attr[(int)VividOfFade.Attr.Dex] += Mathf.RoundToInt(a.Value); break;
                        case StatType.Int: s.Attr[(int)VividOfFade.Attr.Int] += Mathf.RoundToInt(a.Value); break;
                        case StatType.Vit: s.Attr[(int)VividOfFade.Attr.Vit] += Mathf.RoundToInt(a.Value); break;
                        case StatType.Wis: s.Attr[(int)VividOfFade.Attr.Wis] += Mathf.RoundToInt(a.Value); break;
                        case StatType.Armor: s.Armor += a.Value; break;
                        case StatType.LightRadius: s.Light += a.Value; break;
                        case StatType.Pierce: pierce += Mathf.RoundToInt(a.Value); break;
                        case StatType.Multishot: extra += Mathf.RoundToInt(a.Value); break;
                    }
                }

                foreach (var u in item.Uniques)
                {
                    switch (u)
                    {
                        case UniqueType.ExplodeOnKill: s.ExplodeMult += 0.6f; break;
                        case UniqueType.HealOnKill: s.HealOnKill += 1; break;
                        case UniqueType.SplitShot: extra += 2; break;
                    }
                }
            }

            // 能力値の効果
            int str = s.Attr[(int)VividOfFade.Attr.Str];
            int dex = s.Attr[(int)VividOfFade.Attr.Dex];
            int intel = s.Attr[(int)VividOfFade.Attr.Int];
            int vit = s.Attr[(int)VividOfFade.Attr.Vit];
            int wis = s.Attr[(int)VividOfFade.Attr.Wis];
            int main = s.Attr[(int)cls.MainAttr];

            s.MaxHp = cls.BaseHp + vit * 8 + (ch.Level - 1) * 6 + bonusHp;
            s.MaxMp = 20 + intel * 3 + wis * 4 + (ch.Level - 1) * 2 + bonusMp;
            s.HpRegen = vit * 0.05f;
            s.MpRegen = 0.5f + wis * 0.08f;
            s.Armor += str * 0.5f;
            atkSpd += dex * 0.004f;
            s.CritChance += dex * 0.002f;

            var w = ClassInfo.Weapon(kind);
            s.Kind = w.Attack;
            s.Light = Mathf.Min(s.Light, GameConfig.MaxLight);
            s.Damage = baseDamage * w.Mult * (1f + dmgPct) * (1f + main * 0.02f);
            s.FireInterval = w.Interval / (1f + atkSpd);
            s.MoveSpeed = cls.Speed * (1f + moveSpd);
            s.CritChance = Mathf.Min(s.CritChance + w.CritBonus, 0.75f);

            switch (w.Attack)
            {
                case AttackKind.Projectile:
                    s.Pierce = w.Pierce + pierce;
                    s.Shots = w.Shots + extra;
                    s.SpreadDeg = s.Shots > 1 ? (w.Spread > 0f ? w.Spread : 12f) : 0f;
                    break;
                case AttackKind.Slash:
                    // 明るさが広いほど、少しだけ遠くまで斬れる
                    s.Reach = w.Reach + Mathf.Max(0f, s.Light - cls.BaseLight) * 0.1f;
                    s.ArcDeg = Mathf.Min(w.Arc + 15f * extra, 300f);
                    break;
                case AttackKind.Orbit:
                    s.Orbs = w.Orbs + extra;
                    s.OrbitRadius = 0.8f + s.Light * 0.15f;
                    s.OrbitSpeed = w.OrbitSpeed * (1f + atkSpd);
                    break;
                case AttackKind.Pulse:
                    s.PulseRadius = 1f + s.Light * 0.2f + 0.3f * extra;
                    break;
            }
            s.Shots = Mathf.Max(1, s.Shots);
            return s;
        }
    }
}
