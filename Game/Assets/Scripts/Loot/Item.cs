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

        /// <summary>ロック中は一括分解・自動分解の対象にしない。</summary>
        public bool Locked;

        /// <summary>装備に必要なレベル。</summary>
        public int ReqLevel = 1;
        /// <summary>武器に必要な能力値（職業の主能力）。0 なら条件なし。</summary>
        public Attr ReqAttr;
        public int ReqValue;

        /// <summary>武器：基本ダメージ／ランタン：明るさ（マス）／防具：最大体力。指輪は0。</summary>
        public float BaseValue;

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
        }

        public string BaseName
        {
            get
            {
                switch (Slot)
                {
                    case ItemSlot.Weapon: return ClassInfo.Weapon(Weapon).Name;
                    case ItemSlot.Lantern: return "Lantern";
                    case ItemSlot.Armor: return "Coat";
                    default: return "Ring";
                }
            }
        }

        public string Name => $"{RarityInfo.LabelOf(Rarity)} {BaseName}";

        public List<string> Describe()
        {
            var lines = new List<string>();
            switch (Slot)
            {
                case ItemSlot.Weapon:
                    lines.Add($"Damage {BaseValue:0}");
                    lines.Add(ClassInfo.Weapon(Weapon).Desc);
                    break;
                case ItemSlot.Lantern:
                    lines.Add($"Light +{BaseValue:0.0}");
                    break;
                case ItemSlot.Armor:
                    lines.Add($"Max HP +{BaseValue:0}");
                    break;
            }
            foreach (var a in Affixes) lines.Add(StatText.Format(a.Stat, a.Value));
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
                case StatType.DamagePct: return $"+{v * 100f:0}% Damage";
                case StatType.AttackSpeedPct: return $"+{v * 100f:0}% Attack Speed";
                case StatType.CritChance: return $"+{v * 100f:0}% Crit Chance";
                case StatType.CritDamage: return $"+{v * 100f:0}% Crit Damage";
                case StatType.MoveSpeedPct: return $"+{v * 100f:0}% Move Speed";
                case StatType.MaxHp: return $"+{v:0} Max HP";
                case StatType.Armor: return $"+{v:0} Armor";
                case StatType.LightRadius: return $"+{v:0.0} Light";
                case StatType.Pierce: return $"+{v:0} Pierce";
                case StatType.Multishot: return MultiText(v);
                case StatType.Str: return $"+{v:0} STR";
                case StatType.Dex: return $"+{v:0} DEX";
                case StatType.Int: return $"+{v:0} INT";
                case StatType.Vit: return $"+{v:0} VIT";
                case StatType.Wis: return $"+{v:0} WIS";
                case StatType.MaxMp: return $"+{v:0} Max MP";
                default: return t.ToString();
            }
        }

        /// <summary>弾の数の効果は、職業によって意味が変わる。</summary>
        static string MultiText(float v)
        {
            switch (ClassInfo.Current)
            {
                case ClassType.Shadeblade: return $"+{v * 15f:0} deg Arc, +{v:0} Target";
                case ClassType.Warden: return $"+{v:0} Flame";
                default: return $"+{v:0} Projectile";
            }
        }

        public static string Unique(UniqueType u)
        {
            switch (u)
            {
                case UniqueType.ExplodeOnKill: return "Enemies explode on death";
                case UniqueType.HealOnKill: return "Heal 1 HP on kill";
                case UniqueType.SplitShot: return MultiText(2f) + " (unique)";
                case UniqueType.DodgeNova: return "Dodging releases a burst of flame";
                case UniqueType.DoubleStrike: return "25% chance to attack again";
                case UniqueType.Vampiric: return "Heal 1% of damage dealt";
                default: return "";
            }
        }
    }
}
