using UnityEngine;

namespace VividOfFade
{
    /// <summary>装備の種類。指輪は2つまで付けられる。</summary>
    public enum ItemSlot { Weapon, Lantern, Armor, Ring }

    /// <summary>装備欄。</summary>
    public enum EquipSlot { Weapon = 0, Lantern = 1, Armor = 2, Ring1 = 3, Ring2 = 4 }

    /// <summary>珍しさ。白・青・黄・橙・紫。</summary>
    public enum Rarity { Common, Magic, Rare, Legendary, Relic }

    /// <summary>武器の種類で通常攻撃の形が変わる。</summary>
    public enum WeaponKind { None, Wand, Repeater, Fan }

    /// <summary>装備に付く効果。</summary>
    public enum StatType { DamagePct, AttackSpeedPct, CritChance, CritDamage, MoveSpeedPct, MaxHp, Armor, LightRadius, Pierce, Multishot }

    /// <summary>橙・紫の装備だけが持つ固有効果。</summary>
    public enum UniqueType { None, ExplodeOnKill, HealOnKill, SplitShot }

    public static class RarityInfo
    {
        static readonly Color[] colors =
        {
            new Color(0.88f, 0.88f, 0.88f),
            new Color(0.35f, 0.6f, 1f),
            new Color(1f, 0.85f, 0.25f),
            new Color(1f, 0.55f, 0.15f),
            new Color(0.78f, 0.42f, 1f),
        };

        static readonly string[] labels = { "Common", "Magic", "Rare", "Legendary", "Relic" };
        static readonly int[] salvage = { 1, 3, 8, 20, 50 };

        public static Color ColorOf(Rarity r) => colors[(int)r];
        public static string LabelOf(Rarity r) => labels[(int)r];
        public static int SalvageOf(Rarity r) => salvage[(int)r];
    }
}
