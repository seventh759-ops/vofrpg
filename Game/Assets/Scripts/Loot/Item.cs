using System.Collections.Generic;

namespace VividOfFade
{
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

    /// <summary>拾った装備1つ。種類・珍しさ・付いた効果と数値をデータとして持つ。</summary>
    public sealed class Item
    {
        public ItemSlot Slot;
        public Rarity Rarity;
        public WeaponKind Weapon;
        public int Level = 1;

        /// <summary>ロック中は一括分解・自動分解の対象にしない。</summary>
        public bool Locked;

        /// <summary>武器：基本ダメージ／ランタン：明るさ（マス）／防具：最大体力。指輪は0。</summary>
        public float BaseValue;

        public readonly List<Affix> Affixes = new List<Affix>();
        public readonly List<UniqueType> Uniques = new List<UniqueType>();

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
            var lines = new List<string> { $"Lv {Level}" };
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
                default: return t.ToString();
            }
        }

        /// <summary>弾の数の効果は、職業によって意味が変わる。</summary>
        static string MultiText(float v)
        {
            switch (ClassInfo.Current)
            {
                case ClassType.Shadeblade: return $"+{v * 15f:0} deg Slash Arc";
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
                default: return "";
            }
        }
    }
}
