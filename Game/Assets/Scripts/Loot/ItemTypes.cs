using UnityEngine;

namespace VividOfFade
{
    /// <summary>装備の種類。指輪は2つまで付けられる。</summary>
    public enum ItemSlot { Weapon, Lantern, Armor, Ring }

    /// <summary>装備欄。</summary>
    public enum EquipSlot { Weapon = 0, Lantern = 1, Armor = 2, Ring1 = 3, Ring2 = 4 }

    /// <summary>珍しさ。白・青・黄・紫・橙。赤（Mythic）はボスからしか落ちない。</summary>
    public enum Rarity { Common, Magic, Rare, Legendary, Relic, Mythic }

    /// <summary>武器の種類で通常攻撃の形が変わる。職業ごとに3種類（ClassInfo）。</summary>
    public enum WeaponKind { None, Wand, Repeater, Fan, Sword, Dagger, Greatblade, Censer, Chain, Brazier }

    /// <summary>装備に付く効果。</summary>
    public enum StatType { DamagePct, AttackSpeedPct, CritChance, CritDamage, MoveSpeedPct, MaxHp, Armor, LightRadius, Pierce, Multishot, Str, Dex, Int, Vit, Wis, MaxMp }

    /// <summary>橙・紫の装備だけが持つ固有効果。</summary>
    /// <summary>DodgeNova・DoubleStrike・Vampiric はボスの Mythic 装備だけに付く。</summary>
    public enum UniqueType { None, ExplodeOnKill, HealOnKill, SplitShot, DodgeNova, DoubleStrike, Vampiric }

    public static class RarityInfo
    {
        static readonly Color[] colors =
        {
            new Color(0.88f, 0.88f, 0.88f),
            new Color(0.35f, 0.6f, 1f),
            new Color(1f, 0.85f, 0.25f),
            new Color(0.78f, 0.42f, 1f),   // Legendary：紫
            new Color(1f, 0.55f, 0.15f),   // Relic：橙（光り方が一番派手なので、紫より上）
            new Color(1f, 0.25f, 0.3f),
        };

        static readonly string[] labels = { "Common", "Magic", "Rare", "Legendary", "Relic", "Mythic" };
        static readonly int[] salvage = { 1, 3, 8, 20, 50, 100 };

        public static Color ColorOf(Rarity r) => colors[(int)r];
        public static string LabelOf(Rarity r) => labels[(int)r];
        public static int SalvageOf(Rarity r) => salvage[(int)r];
    }
}
