using System;
using System.Collections.Generic;

namespace VividOfFade
{
    [Serializable]
    public struct Affix
    {
        public StatType Stat;
        public float Value;

        public Affix(StatType stat, float value)
        {
            Stat = stat;
            Value = value;
        }
    }

    /// <summary>拾った装備1つ。種類・珍しさ・付いた効果と数値をデータとして持つ。保存できる。</summary>
    [Serializable]
    public sealed class Item
    {
        public ItemSlot Slot;
        public Rarity Rarity;
        public WeaponKind Weapon;
        public int Level = 1;

        /// <summary>深淵で落ちた装備（Lv100以上）。数値が高く、深淵だけの固有効果が1つ付く。</summary>
        public bool Abyss;

        /// <summary>ロック中は一括分解・自動分解の対象にしない。</summary>
        public bool Locked;

        /// <summary>装備に必要なレベル。</summary>
        public int ReqLevel = 1;
        /// <summary>武器に必要な能力値（職業の主能力）。0 なら条件なし。</summary>
        public Attr ReqAttr;
        public int ReqValue;
        /// <summary>能力値の条件が要るか（神話の武器だけ。前の版で付いた、ほかの装備の条件は数えない）。</summary>
        public bool NeedsAttr => ReqValue > 0 && Slot == ItemSlot.Weapon && Rarity == Rarity.Mythic;

        /// <summary>武器：基本ダメージ／ランタン：明るさ（マス）／防具：最大体力。指輪は0。</summary>
        public float BaseValue;

        /// <summary>町の鍛冶屋での強化（+0〜+15）。1段ごとに基本の数値と効果の値が +6%（弾・貫通の数は変わらない）。</summary>
        public int Enhance;
        public const int MaxEnhance = 15;
        public const float EnhanceStep = 0.06f;
        public float EnhanceMult => 1f + EnhanceStep * Enhance;

        /// <summary>強化込みの基本の数値。</summary>
        public float EffBase => BaseValue * EnhanceMult;

        /// <summary>強化込みの効果の値（数で数える効果は強化しない）。</summary>
        public float Eff(Affix a) => a.Stat == StatType.Pierce || a.Stat == StatType.Multishot ? a.Value : a.Value * EnhanceMult;

        public List<Affix> Affixes = new List<Affix>();
        public List<UniqueType> Uniques = new List<UniqueType>();

        /// <summary>前の挑戦から持ち込んだ品（帰還すると、選ばなくても持ち帰れる）。保存はしない。</summary>
        [NonSerialized] public bool Carried;

        /// <summary>保存用の「空き」の印（装備欄の空きを表す）。</summary>
        public bool IsEmptyMarker => Level <= 0;
        public static Item EmptyMarker() => new Item { Level = 0 };

        public void Repair()
        {
            if (Affixes == null) Affixes = new List<Affix>();
            if (Uniques == null) Uniques = new List<UniqueType>();
            if (Enhance < 0) Enhance = 0;
            // 前の版の「経験値・拾う火種・珍しい品」は「幸運」1つにまとめる（いちばん大きい値を残す）
            int luckAt = -1;
            for (int i = 0; i < Affixes.Count; i++)
            {
                var a = Affixes[i];
                if (a.Stat != StatType.ExpGain && a.Stat != StatType.EmberFind && a.Stat != StatType.MagicFind && a.Stat != StatType.Luck) continue;
                if (luckAt < 0)
                {
                    luckAt = i;
                    Affixes[i] = new Affix(StatType.Luck, a.Value);
                }
                else
                {
                    if (a.Value > Affixes[luckAt].Value) Affixes[luckAt] = new Affix(StatType.Luck, a.Value);
                    Affixes.RemoveAt(i);
                    i--;
                }
            }
            if (Enhance > MaxEnhance) Enhance = MaxEnhance;
        }

        public string BaseName
        {
            get
            {
                switch (Slot)
                {
                    case ItemSlot.Weapon: return ClassInfo.Weapon(Weapon).DisplayName;
                    case ItemSlot.Lantern: return Loc.T("Lantern", "ランタン");
                    case ItemSlot.Armor: return Loc.T("Coat", "外套");
                    default: return Loc.T("Ring", "指輪");
                }
            }
        }

        public string Name => (Abyss ? Loc.T("Abyssal ", "深淵・") : "") + (Loc.Ja ? $"{RarityInfo.LabelOf(Rarity)}の{BaseName}" : $"{RarityInfo.LabelOf(Rarity)} {BaseName}") + (Enhance > 0 ? $" +{Enhance}" : "");

        public List<string> Describe()
        {
            var lines = new List<string>();
            switch (Slot)
            {
                case ItemSlot.Weapon:
                    lines.Add(Loc.T($"Damage {EffBase:0}", $"ダメージ {EffBase:0}"));
                    lines.Add(ClassInfo.Weapon(Weapon).DisplayDesc);
                    break;
                case ItemSlot.Lantern:
                    lines.Add(Loc.T($"Light +{EffBase:0.0}", $"明るさ +{EffBase:0.0}"));
                    break;
                case ItemSlot.Armor:
                    lines.Add(Loc.T($"Max HP +{EffBase:0}", $"最大体力 +{EffBase:0}"));
                    break;
            }
            foreach (var a in Affixes) lines.Add(StatText.Format(a.Stat, Eff(a)));
            foreach (var u in Uniques) lines.Add("* " + StatText.Unique(u));
            return lines;
        }
    }

    public static class StatText
    {
        public static string Format(StatType t, float v)
        {
            switch (t)
            {
                case StatType.DamagePct: return Loc.T($"+{v * 100f:0}% Damage", $"ダメージ +{v * 100f:0}%");
                case StatType.AttackSpeedPct: return Loc.T($"+{v * 100f:0}% Attack Speed", $"攻撃速度 +{v * 100f:0}%");
                case StatType.CritChance: return Loc.T($"+{v * 100f:0}% Crit Chance", $"会心率 +{v * 100f:0}%");
                case StatType.CritDamage: return Loc.T($"+{v * 100f:0}% Crit Damage", $"会心ダメージ +{v * 100f:0}%");
                case StatType.MoveSpeedPct: return Loc.T($"+{v * 100f:0}% Move Speed", $"移動速度 +{v * 100f:0}%");
                case StatType.MaxHp: return Loc.T($"+{v:0} Max HP", $"最大体力 +{v:0}");
                case StatType.Armor: return Loc.T($"+{v:0} Armor", $"防御 +{v:0}");
                case StatType.LightRadius: return Loc.T($"+{v:0.0} Light", $"明るさ +{v:0.0}");
                case StatType.Pierce: return Loc.T($"+{v:0} Pierce", $"貫通 +{v:0}");
                case StatType.Multishot: return MultiText(v);
                case StatType.Str: return $"+{v:0} STR";
                case StatType.Dex: return $"+{v:0} DEX";
                case StatType.Int: return $"+{v:0} INT";
                case StatType.Vit: return $"+{v:0} VIT";
                case StatType.Wis: return $"+{v:0} WIS";
                case StatType.MaxMp: return Loc.T($"+{v:0} Max MP", $"最大MP +{v:0}");
                case StatType.HpRegen: return Loc.T($"+{v:0.0} HP/s", $"体力回復 +{v:0.0}/秒");
                case StatType.MpRegen: return Loc.T($"+{v:0.0} MP/s", $"MP回復 +{v:0.0}/秒");
                case StatType.LifeSteal: return Loc.T($"Heal {v * 100f:0.0}% of damage dealt", $"与えたダメージの{v * 100f:0.0}%を回復");
                case StatType.DamageReduction: return Loc.T($"-{v * 100f:0}% Damage Taken", $"被ダメージ -{v * 100f:0}%");
                case StatType.SkillDamagePct: return Loc.T($"+{v * 100f:0}% Skill Damage", $"スキルのダメージ +{v * 100f:0}%");
                case StatType.CooldownReduction: return Loc.T($"-{v * 100f:0}% Skill Cooldown", $"スキルの待ち時間 -{v * 100f:0}%");
                case StatType.ExpGain: return Loc.T($"+{v * 100f:0}% Experience", $"経験値 +{v * 100f:0}%");
                case StatType.EmberFind: return Loc.T($"+{v * 100f:0}% Embers Found", $"拾う火種 +{v * 100f:0}%");
                case StatType.MagicFind: return Loc.T($"+{v * 100f:0}% Rare Find", $"珍しい品が出やすい +{v * 100f:0}%");
                case StatType.EliteDamage: return Loc.T($"+{v * 100f:0}% Damage to Elites/Bosses", $"精鋭・ボスへのダメージ +{v * 100f:0}%");
                case StatType.ExplodePower: return Loc.T($"Enemies explode on death (+{v * 100f:0}%)", $"倒した敵が爆発（+{v * 100f:0}%）");
                case StatType.DodgeCooldown: return Loc.T($"-{v * 100f:0}% Dodge Cooldown", $"回避の待ち時間 -{v * 100f:0}%");
                case StatType.AllStats: return Loc.T($"+{v:0} All Stats", $"全能力値 +{v:0}");
                case StatType.AreaPct: return Loc.T($"+{v * 100f:0}% Skill Area", $"スキルの範囲 +{v * 100f:0}%");
                case StatType.MaxHpPct: return Loc.T($"+{v * 100f:0}% Max HP", $"最大体力 +{v * 100f:0}%");
                case StatType.Hit: return $"+{v:0} HIT";
                case StatType.Evasion: return $"+{v:0} EVA";
                case StatType.FlatDamage: return Loc.T($"+{v:0} Attack", $"攻撃力 +{v:0}");
                case StatType.Luck: return Loc.T($"+{v * 100f:0}% Luck (exp, embers, rare finds)", $"幸運 +{v * 100f:0}%（経験値・火種・珍しい品）");
                default: return t.ToString();
            }
        }

        /// <summary>弾の数の効果は、職業によって意味が変わる。</summary>
        static string MultiText(float v)
        {
            switch (ClassInfo.Current)
            {
                case ClassType.Shadeblade: return Loc.T($"+{v * 15f:0} deg Arc, +{v:0} Target", $"斬る広さ +{v * 15f:0}度、当たる数 +{v:0}");
                case ClassType.Warden: return Loc.T($"+{v:0} Flame", $"灯り +{v:0}");
                default: return Loc.T($"+{v:0} Projectile", $"弾 +{v:0}");
            }
        }

        public static string Unique(UniqueType u)
        {
            switch (u)
            {
                case UniqueType.ExplodeOnKill: return Loc.T("Enemies explode on death", "倒した敵が爆発する");
                case UniqueType.HealOnKill: return Loc.T("Heal 1 HP on kill", "倒すと体力1回復");
                case UniqueType.SplitShot: return MultiText(2f) + Loc.T(" (unique)", "（固有）");
                case UniqueType.DodgeNova: return Loc.T("Dodging releases a burst of flame", "回避すると炎が弾ける");
                case UniqueType.DoubleStrike: return Loc.T("25% chance to attack again", "25%でもう一度攻撃");
                case UniqueType.Vampiric: return Loc.T("Heal 1% of damage dealt", "与えたダメージの1%を回復");
                case UniqueType.AbyssBlast: return Loc.T("[Abyss] 30% on kill: a huge blast of abyssal flame (300%)", "【深淵】倒すと30%で、深淵の業火が大爆発（300%）");
                case UniqueType.VoidRift: return Loc.T("[Abyss] Every 5s, a rift drags enemies in and tears at them", "【深淵】5秒ごとに裂け目が開き、敵を吸い込んで切り裂く");
                case UniqueType.Starfall: return Loc.T("[Abyss] Crits may call down a falling star (400%)", "【深淵】会心でときどき星が落ちる（400%）");
                case UniqueType.Retribution: return Loc.T("[Abyss] When hit, unleash a wave of darkness (500%)", "【深淵】攻撃を受けると、闇の波動で反撃（500%）");
                default: return "";
            }
        }
    }
}
