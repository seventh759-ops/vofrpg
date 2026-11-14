using UnityEngine;

namespace VividOfFade
{
    /// <summary>職業・レベル・能力値・装備から計算したプレイヤーの能力。どれかが変わるたびに作り直す。</summary>
    public sealed partial class PlayerStats
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
        /// <summary>今の武器で攻撃力が伸びる主能力・副能力。</summary>
        public Attr MainAttr, SubAttr;
        /// <summary>3次職の追加の能力値（なければ null）。副能力と同じく 1 ポイントで攻撃力 +1%。</summary>
        public Attr? ThirdAttr;
        /// <summary>
        /// スキルの基準の攻撃力。武器ごとの手数の差（1 回の倍率×弾の数÷攻撃の間隔）を、職業の最初の武器を基準にならす。
        /// 攻撃速度は入れない（攻撃速度を上げてもスキルは強くならない）。
        /// </summary>
        public float SkillBase;
        public float FireInterval;
        public float CritChance;
        /// <summary>実際に会心が出る確率（会心率から、いまの敵の会心耐性を引く）。</summary>
        public float EffectiveCrit => Mathf.Clamp01(CritChance - GameConfig.CritResist);
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
        public int MaxTargets;

        // 回る灯り・周りを焼く（灯番）
        public int Orbs;
        public float OrbitRadius;
        public float OrbitSpeed;
        public float PulseRadius;

        public float ExplodeMult; // 撃破時の爆発（攻撃力に対する倍率）
        // 深淵の装備の固有効果（同じ効果を重ねると強くなる）
        public float AbyssBlastChance;   // 倒したときの大爆発の確率
        public int VoidRift;             // 裂け目（重ねるほど間隔が短い）
        public float StarfallChance;     // 会心で星が落ちる確率
        public float RetributionMult;    // 受けたときの反撃の倍率
        // 深淵のセット（AbyssSets）。そろえた数
        public int SetBlast, SetRift, SetStar, SetRetribution;
        public float BlastRadiusMult = 1f;  // 大爆発の範囲
        public bool BlastChain;             // 大爆発で倒した敵も、もう1回大爆発する
        public float RiftIntervalMult = 1f; // 裂け目の間隔
        public float RiftPullMult = 1f;     // 裂け目の吸い込み（強さと広さ）
        public float RiftPowerMult = 1f;    // 裂け目の切り裂く力
        public float RiftLife = 2f;         // 裂け目の開いている時間
        public int StarCount = 1;           // 落ちる星の数
        public bool StarScatter;            // 星が落ちた所に小さな星が散る
        public float RetributionRadiusMult = 1f;
        public float RetributionHeal;       // 反撃のたびの回復（最大HPに対する割合）
        public int HealOnKill;
        public float DodgeNovaMult;       // 回避したときの炎（攻撃力に対する倍率）
        public float DoubleStrikeChance;
        public float LifeSteal;           // 与えたダメージのうち回復する割合
        public float DamageTaken = 1f;    // 受けるダメージの倍率
        public float DodgeCooldown;

        // 装備の効果（M6 で足した分）
        public float SkillDamage;          // スキルのダメージ +割合
        public float CooldownMult = 1f;    // スキルの待ち時間の倍率
        public float ExpGain;              // 経験値 +割合
        public float EmberFind;            // 拾う火種 +割合
        public float MagicFind;            // 珍しい品の出やすさ（RollRarity の bonus に足す）
        public float EliteDamage;          // 精鋭・ボスへのダメージ +割合
        public float AreaMult = 1f;        // スキルの範囲の倍率

        /// <summary>命中（近距離の攻撃が当たるか）。DEX＋装備。</summary>
        public int Hit;
        /// <summary>回避（敵の攻撃を避けるか）。DEX＋装備。</summary>
        public int Eva;

        /// <summary>近距離の攻撃が当たる確率（その層の敵の回避と比べる。60%〜100%）。</summary>
        public float HitChance(int floorTier) => HitChanceVs(GameConfig.EnemyEvasion(floorTier));

        /// <summary>その敵の回避と比べた、近距離の攻撃が当たる確率（60%〜100%）。</summary>
        public float HitChanceVs(int enemyEva) => Mathf.Clamp(0.4f + Hit / (float)(Hit + Mathf.Max(1, enemyEva)), 0.6f, 1f);

        /// <summary>敵の攻撃を避ける確率（その層の敵の命中と比べる。上限60%）。囲まれ補正は別に掛ける。</summary>
        public float EvadeChance(int floorTier) => EvadeChanceVs(GameConfig.EnemyAccuracy(floorTier));

        /// <summary>その敵の命中と比べた、避ける確率（上限60%）。</summary>
        public float EvadeChanceVs(int enemyAcc) => Mathf.Min(GameConfig.MaxEvade, Eva / (float)(Eva + Mathf.Max(1, enemyAcc)));

        public float AttacksPerSecond => 1f / FireInterval;

        /// <summary>斬撃の2体目以降に入る威力の割合。</summary>
        public const float SecondaryHit = 0.5f;

        /// <summary>1回の攻撃で何体ぶん当たりやすいか（強さの比較用の目安）。</summary>
        public float Multiplicity
        {
            get
            {
                switch (Kind)
                {
                    case AttackKind.Slash: return 1f + (Mathf.Max(1, MaxTargets) - 1) * SecondaryHit;
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
                float dps = Damage * (1f + EffectiveCrit * (CritMult - 1f)) * Multiplicity / FireInterval;
                // 命中・回避（DEX）：レベルの半分を層とみなして、その層の敵と比べる
                int tier = GameConfig.Tier(Mathf.Max(1, Level / 2));
                if (Kind == AttackKind.Slash) dps *= HitChance(tier);                 // 近接だけ命中の判定がある
                // 守り：実際に耐えられる量（受けるダメージ = 元 × 被ダメージ倍率 × 100 /（100 + 防御））を、職業ごとの重みで累乗する
                // （灯術師 0.35・影刃 0.45・灯番 0.6）。攻撃（そのまま掛ける）と釣り合わせ、体力の実数値を優遇しすぎないように
                float ehp = Mathf.Max(1f, MaxHp) * (100f + Mathf.Max(0f, Armor)) / 100f / Mathf.Max(0.1f, DamageTaken);
                ehp /= 1f - 0.5f * EvadeChance(tier);                                   // 囲まれると避けにくいので、回避は半分として数える
                float survive = Mathf.Pow(ehp, spec.DefenseWeight) * (1f + MaxMp * 0.0005f + HpRegen * 0.02f + MpRegen * 0.01f); // 守りの重みは職業ごと
                float reach = 1f + (Light - spec.BaseLight) * 0.05f;
                // M6 の効果も少しずつ数える（スキル・精鋭・範囲・稼ぎ）。被ダメージは守りの中で数える
                float extras = (1f + SkillDamage * 0.3f) * (1f + (1f - CooldownMult) * 0.3f)
                    * (1f + EliteDamage * 0.2f) * (1f + (AreaMult - 1f) * 0.2f) * (1f + (ExpGain + EmberFind + MagicFind) * 0.1f);
                // その職業が重視する能力値（主・副。影刃なら STR と DEX）を少しだけ上乗せ。ほかの能力値は上乗せしない
                float favored = 1f + 0.001f * (Attr[(int)spec.MainAttr] + Attr[(int)spec.SubAttr] + (ThirdAttr.HasValue ? Attr[(int)ThirdAttr.Value] : 0));
                return dps * survive * reach * favored * (1f + ExplodeMult * 0.5f) * (1f + DoubleStrikeChance)
                    * (1f + DodgeNovaMult * 0.1f) * (1f + LifeSteal * 10f) * extras;
            }
        }

        /// <summary>スキルの強化（一定時間）を足した能力を作る。元の能力は変えない。</summary>
        public PlayerStats WithBuff(SkillSpec b, float power)
        {
            var s = (PlayerStats)MemberwiseClone();
            s.Damage *= 1f + b.BuffDamage * power;
            s.FireInterval /= 1f + b.BuffAttackSpeed * power;
            s.Armor += b.BuffArmor * power;
            if (b.BuffTaken < 1f) s.DamageTaken *= Mathf.Max(0.3f, 1f - (1f - b.BuffTaken) * power);
            s.Light = Mathf.Min(s.Light + b.BuffLight * power, GameConfig.MaxLight + 3f);
            int e = b.BuffExtra;
            if (e > 0)
            {
                s.Shots += e;
                if (s.SpreadDeg <= 0f) s.SpreadDeg = 12f;
                s.Orbs += e;
                s.ArcDeg = Mathf.Min(s.ArcDeg + 15f * e, 300f);
                s.MaxTargets += e;
                s.PulseRadius += 0.3f * e;
            }
            return s;
        }

        /// <summary>深淵のセットの効果（同じ深淵の固有効果の品を 2・3・5 つそろえる）。</summary>
        static void ApplyAbyssSets(PlayerStats s, Item[] equipped)
        {
            s.SetBlast = AbyssSets.Count(equipped, UniqueType.AbyssBlast);
            s.SetRift = AbyssSets.Count(equipped, UniqueType.VoidRift);
            s.SetStar = AbyssSets.Count(equipped, UniqueType.Starfall);
            s.SetRetribution = AbyssSets.Count(equipped, UniqueType.Retribution);
            if (s.SetBlast >= 2) s.AbyssBlastChance += 0.15f;
            if (s.SetBlast >= 3) s.BlastRadiusMult = 1.4f;
            if (s.SetBlast >= 5) s.BlastChain = true;
            if (s.SetRift >= 2) s.RiftIntervalMult = 0.7f;
            if (s.SetRift >= 3) s.RiftPullMult = 1.5f;
            if (s.SetRift >= 5) { s.RiftPowerMult = 2f; s.RiftLife = 3f; }
            if (s.SetStar >= 2) s.CritChance = Mathf.Min(s.CritChance + 0.1f, GameConfig.MaxCritChance);
            if (s.SetStar >= 3) s.StarCount = 2;
            if (s.SetStar >= 5) s.StarScatter = true;
            if (s.SetRetribution >= 2) s.DamageTaken *= 0.9f;
            if (s.SetRetribution >= 3) s.RetributionRadiusMult = 1.5f;
            if (s.SetRetribution >= 5) s.RetributionHeal = 0.08f;
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
            for (int i = 0; i < Attributes.Count; i++) s.Attr[i] = cls.BaseStats[i] + ch.Alloc[i] + ThirdClasses.BonusStats(ch); // 転生1回ごとに全能力値+5
            int bonusHp = 0, bonusMp = 0;
            float hpRegen = 0f, mpRegen = 0f, reduction = 0f, cdr = 0f, dodgeCdr = 0f, hpPct = 0f;
            int hitBonus = 0, evaBonus = 0;
            float flatDamage = 0f;

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
                        baseDamage = item.EffBase;
                        break;
                    case ItemSlot.Lantern:
                        s.Light += item.EffBase;
                        break;
                    case ItemSlot.Armor:
                        bonusHp += Mathf.RoundToInt(item.EffBase);
                        break;
                }

                foreach (var a0 in item.Affixes)
                {
                    var a = new Affix(a0.Stat, item.Eff(a0)); // 強化込みの値
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
                        case StatType.HpRegen: hpRegen += a.Value; break;
                        case StatType.MpRegen: mpRegen += a.Value; break;
                        case StatType.LifeSteal: s.LifeSteal += a.Value; break;
                        case StatType.DamageReduction: reduction += a.Value; break;
                        case StatType.SkillDamagePct: s.SkillDamage += a.Value; break;
                        case StatType.CooldownReduction: cdr += a.Value; break;
                        case StatType.ExpGain: s.ExpGain += a.Value; break;
                        case StatType.EmberFind: s.EmberFind += a.Value; break;
                        case StatType.MagicFind: s.MagicFind += a.Value; break;
                        case StatType.EliteDamage: s.EliteDamage += a.Value; break;
                        case StatType.ExplodePower: s.ExplodeMult += a.Value; break;
                        case StatType.DodgeCooldown: dodgeCdr += a.Value; break;
                        case StatType.AllStats:
                            for (int k = 0; k < Attributes.Count; k++) s.Attr[k] += Mathf.RoundToInt(a.Value);
                            break;
                        case StatType.AreaPct: s.AreaMult += a.Value; break;
                        case StatType.MaxHpPct: hpPct += a.Value; break;
                        case StatType.Hit: hitBonus += Mathf.RoundToInt(a.Value); break;
                        case StatType.Evasion: evaBonus += Mathf.RoundToInt(a.Value); break;
                        case StatType.FlatDamage: flatDamage += a.Value; break;
                        case StatType.Luck: s.ExpGain += a.Value; s.EmberFind += a.Value; s.MagicFind += a.Value; break;
                    }
                }

                foreach (var u in item.Uniques)
                {
                    switch (u)
                    {
                        case UniqueType.ExplodeOnKill: s.ExplodeMult += 0.6f; break;
                        case UniqueType.HealOnKill: s.HealOnKill += 1; break;
                        case UniqueType.SplitShot: extra += 2; break;
                        case UniqueType.DodgeNova: s.DodgeNovaMult += 1.5f; break;
                        case UniqueType.DoubleStrike: s.DoubleStrikeChance += 0.25f; break;
                        case UniqueType.Vampiric: s.LifeSteal += 0.01f; break;
                        case UniqueType.AbyssBlast: s.AbyssBlastChance += 0.3f; break;
                        case UniqueType.VoidRift: s.VoidRift++; break;
                        case UniqueType.Starfall: s.StarfallChance += 0.15f; break;
                        case UniqueType.Retribution: s.RetributionMult += 5f; break;
                    }
                }
            }

            // 能力値の効果
            int dex = s.Attr[(int)VividOfFade.Attr.Dex];
            int vit = s.Attr[(int)VividOfFade.Attr.Vit];
            int wis = s.Attr[(int)VividOfFade.Attr.Wis];
            var weaponSpec = ClassInfo.Weapon(kind);     // 攻撃力が伸びる能力値は武器で決まる（短剣は DEX など）
            s.MainAttr = weaponSpec.MainAttr;
            s.SubAttr = weaponSpec.SubAttr;
            int main = s.Attr[(int)s.MainAttr];
            int sub = s.Attr[(int)s.SubAttr];
            s.ThirdAttr = ThirdClasses.Of(type, ch)?.ExtraAttr;
            int thirdAttr = s.ThirdAttr.HasValue ? s.Attr[(int)s.ThirdAttr.Value] : 0;

            s.MaxHp = cls.BaseHp + vit * 8 + (ch.Level - 1) * 6 + bonusHp;
            // MP は WIS だけで増える。レベルアップのたびに、そのときの WIS で上限が積み上がる（MpGrown）
            Progression.EnsureMpGrowth(ch);
            s.MaxMp = 20 + wis * 2 + ch.MpGrown + bonusMp;
            s.MaxHp = Mathf.RoundToInt(s.MaxHp * (1f + hpPct));
            s.HpRegen = vit * 0.05f + hpRegen;
            s.MpRegen = 1f + wis * 0.07f + (ch.Level - 1) * 0.01f + mpRegen;
            s.Armor += vit * 0.5f; // 防御は VIT（STR は攻撃だけ。守りを固めたいなら VIT に振る）
            atkSpd += dex * 0.004f;
            s.Hit = dex + hitBonus;  // 命中・回避は DEX で伸びる
            s.Eva = dex + evaBonus;
            s.CritChance += dex * 0.002f;

            var w = ClassInfo.Weapon(kind);
            s.Kind = w.Attack;
            s.Light = Mathf.Min(s.Light, GameConfig.MaxLight);
            baseDamage += flatDamage; // 「攻撃力 +N」は基礎攻撃力に足す（倍率の前）
            s.Damage = baseDamage * w.Mult * (1f + dmgPct) * (1f + main * 0.02f + sub * 0.01f + thirdAttr * 0.01f) * cls.DamageMult;
            var starter = ClassInfo.Weapon(cls.Starter);
            float Throughput(WeaponSpec x) => x.Mult * Mathf.Max(1, x.Attack == AttackKind.Orbit ? x.Orbs : x.Shots) / Mathf.Max(0.05f, x.Interval);
            s.SkillBase = s.Damage / w.Mult * starter.Mult * Mathf.Clamp(Throughput(w) / Throughput(starter), 0.5f, 2f);
            s.DamageTaken = cls.DamageTaken * (1f - Mathf.Min(0.6f, reduction)); // 被ダメージの軽減は 60% まで
            ApplyAbyssSets(s, equipped);
            s.DodgeCooldown = GameConfig.DodgeCooldown * cls.DodgeCooldownMult * (1f - Mathf.Min(0.5f, dodgeCdr));
            s.CooldownMult = 1f - Mathf.Min(0.5f, cdr); // 待ち時間の短縮は 50% まで
            s.FireInterval = w.Interval / (1f + atkSpd);
            s.MoveSpeed = cls.Speed * (1f + moveSpd);
            s.CritChance = Mathf.Min(s.CritChance + w.CritBonus, GameConfig.MaxCritChance); // 会心率の上限は 150%（敵の会心耐性で減るので、100% を超えても意味がある）

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
                    s.MaxTargets = w.MaxTargets + extra; // 弾の数の効果は、当たる数も1体増やす
                    break;
                case AttackKind.Orbit:
                    s.Orbs = w.Orbs + extra;
                    s.OrbitRadius = 1f + s.Light * 0.15f;
                    s.OrbitSpeed = w.OrbitSpeed * (1f + atkSpd);
                    break;
                case AttackKind.Pulse:
                    s.PulseRadius = 1.6f + s.Light * 0.25f + 0.3f * extra;
                    break;
            }
            s.Shots = Mathf.Max(1, s.Shots);

            // 上位職の補正（加護と同じ形で、ずっと効く）
            var adv = AdvancedClasses.Of(type, ch);
            if (adv != null) s = s.WithBoon(adv.Passive);
            var third = ThirdClasses.Of(type, ch);
            if (third != null) s = s.WithBoon(third.Passive);
            return s;
        }
    }
}
