using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 装備をランダムに作る。数値はすべて仮で、遊んでから調整する。
    /// 本番では効果の定義を ScriptableObject に移す予定（今はコードの表で持つ）。
    /// </summary>
    public static class ItemGenerator
    {
        // 白・青・黄・橙・紫の出やすさ
        static readonly float[] rarityWeights = { 60f, 28f, 9f, 2.5f, 0.5f };

        static readonly StatType[] allStats =
        {
            StatType.DamagePct, StatType.AttackSpeedPct, StatType.CritChance, StatType.CritDamage,
            StatType.MoveSpeedPct, StatType.MaxHp, StatType.Armor, StatType.LightRadius,
            StatType.Pierce, StatType.Multishot,
            StatType.Str, StatType.Dex, StatType.Int, StatType.Vit, StatType.Wis, StatType.MaxMp,
        };

        static readonly UniqueType[] allUniques = { UniqueType.ExplodeOnKill, UniqueType.HealOnKill, UniqueType.SplitShot };
        static readonly UniqueType[] bossUniques = { UniqueType.DodgeNova, UniqueType.DoubleStrike, UniqueType.Vampiric };

        /// <summary>bonus が大きいほど、珍しい段階が出やすくなる（0 で通常）。</summary>
        public static Rarity RollRarity(float bonus)
        {
            var w = new float[rarityWeights.Length];
            float total = 0f;
            for (int i = 0; i < w.Length; i++)
            {
                w[i] = rarityWeights[i] * Mathf.Pow(1f + bonus, i);
                total += w[i];
            }
            float r = Random.value * total;
            for (int i = 0; i < w.Length; i++)
            {
                r -= w[i];
                if (r <= 0f) return (Rarity)i;
            }
            return Rarity.Common;
        }

        public static Item Roll(int level, float bonus)
        {
            var item = new Item
            {
                Level = Mathf.Max(1, level),
                Rarity = RollRarity(bonus),
                Slot = RollSlot(),
            };

            switch (item.Slot)
            {
                case ItemSlot.Weapon:
                    var kinds = ClassInfo.CurrentSpec.Weapons; // 今の職業の武器だけが落ちる
                    item.Weapon = kinds[Random.Range(0, kinds.Length)];
                    item.BaseValue = WeaponDamage(item.Weapon, item.Level, item.Rarity);
                    break;
                case ItemSlot.Lantern:
                    item.BaseValue = Mathf.Min(2f + 0.1f * item.Level, 4f) * Random.Range(0.9f, 1.1f);
                    break;
                case ItemSlot.Armor:
                    item.BaseValue = Mathf.Round((20f + 5f * item.Level) * RarityMult(item.Rarity) * Random.Range(0.9f, 1.1f));
                    break;
            }

            SetRequirements(item);
            int affixes = AffixCount(item.Rarity);
            // 指輪には基本の数値がないので、効果が1つもない指輪は作らない
            if (item.Slot == ItemSlot.Ring && item.Rarity != Rarity.Relic) affixes = Mathf.Max(1, affixes);
            AddAffixes(item, affixes);

            if (item.Rarity == Rarity.Legendary)
            {
                item.Uniques.Add(allUniques[Random.Range(0, allUniques.Length)]);
            }
            else if (item.Rarity == Rarity.Relic)
            {
                var pool = new List<UniqueType>(allUniques);
                for (int i = 0; i < 2 && pool.Count > 0; i++)
                {
                    int k = Random.Range(0, pool.Count);
                    item.Uniques.Add(pool[k]);
                    pool.RemoveAt(k);
                }
            }
            return item;
        }

        /// <summary>
        /// ボス専用の Mythic 装備。効果4つ（強めの値）＋通常の固有効果1つ＋ボス専用の固有効果1つ。
        /// </summary>
        public static Item RollMythic(int level)
        {
            var item = Roll(level, 0f);
            item.Rarity = Rarity.Mythic;
            if (item.Slot == ItemSlot.Weapon) item.BaseValue = WeaponDamage(item.Weapon, item.Level, Rarity.Mythic);
            else if (item.Slot == ItemSlot.Armor) item.BaseValue = Mathf.Round((20f + 5f * item.Level) * RarityMult(Rarity.Mythic));
            item.Affixes.Clear();
            item.Uniques.Clear();
            AddAffixes(item, 4);
            item.Uniques.Add(allUniques[Random.Range(0, allUniques.Length)]);
            item.Uniques.Add(bossUniques[Random.Range(0, bossUniques.Length)]);
            return item;
        }

        public static Item StarterWeapon(ClassType type)
        {
            var spec = ClassInfo.Weapon(ClassInfo.Get(type).Starter);
            return new Item
            {
                Slot = ItemSlot.Weapon,
                Weapon = spec.Kind,
                Rarity = Rarity.Common,
                Level = 1,
                BaseValue = spec.BaseDamage,
            };
        }

        public static Item StarterLantern()
        {
            return new Item
            {
                Slot = ItemSlot.Lantern,
                Rarity = Rarity.Common,
                Level = 1,
                BaseValue = 2f,
            };
        }

        /// <summary>必要レベルは装備のレベルと同じ。武器は職業の主能力も要る。</summary>
        static void SetRequirements(Item item)
        {
            item.ReqLevel = item.Level;
            if (item.Slot == ItemSlot.Weapon)
            {
                item.ReqAttr = ClassInfo.Weapon(item.Weapon).ClassAttr();
                item.ReqValue = 8 + item.Level;
            }
        }

        static ItemSlot RollSlot()
        {
            float r = Random.value;
            if (r < 0.35f) return ItemSlot.Weapon;
            if (r < 0.55f) return ItemSlot.Lantern;
            if (r < 0.78f) return ItemSlot.Armor;
            return ItemSlot.Ring;
        }

        static float RarityMult(Rarity r) => 1f + 0.12f * (int)r;

        static float WeaponDamage(WeaponKind kind, int level, Rarity rarity)
        {
            float b = ClassInfo.Weapon(kind).BaseDamage;
            return Mathf.Round(b * (1f + 0.2f * (level - 1)) * RarityMult(rarity) * Random.Range(0.9f, 1.1f));
        }

        static int AffixCount(Rarity r)
        {
            switch (r)
            {
                case Rarity.Common: return Random.value < 0.5f ? 0 : 1;
                case Rarity.Magic: return Random.Range(1, 3);
                case Rarity.Rare: return Random.Range(3, 5);
                case Rarity.Legendary: return 3;
                default: return 0; // 紫は固有効果のみ
            }
        }

        static void AddAffixes(Item item, int count)
        {
            var candidates = new List<StatType>(allStats);
            if (ClassInfo.Current != ClassType.Lampwright) candidates.Remove(StatType.Pierce); // 貫通は火の玉だけ
            for (int n = 0; n < count && candidates.Count > 0; n++)
            {
                float total = 0f;
                foreach (var s in candidates) total += Weight(s);
                float r = Random.value * total;
                int pick = candidates.Count - 1;
                for (int i = 0; i < candidates.Count; i++)
                {
                    r -= Weight(candidates[i]);
                    if (r <= 0f)
                    {
                        pick = i;
                        break;
                    }
                }
                var stat = candidates[pick];
                candidates.RemoveAt(pick);
                item.Affixes.Add(new Affix(stat, RollValue(stat, item.Level, item.Rarity)));
            }
        }

        static float Weight(StatType s)
        {
            if (s == StatType.Pierce) return 0.4f;
            if (s == StatType.Multishot) return 0.3f;
            return 1f;
        }

        static float RollValue(StatType s, int level, Rarity rarity)
        {
            float m = RarityMult(rarity);
            switch (s)
            {
                case StatType.DamagePct: return (Random.Range(0.05f, 0.15f) + 0.005f * level) * m;
                case StatType.AttackSpeedPct: return Random.Range(0.04f, 0.12f) * m;
                case StatType.CritChance: return Random.Range(0.02f, 0.06f) * m;
                case StatType.CritDamage: return Random.Range(0.1f, 0.3f) * m;
                case StatType.MoveSpeedPct: return Random.Range(0.03f, 0.08f) * m;
                case StatType.MaxHp: return Mathf.Round((Random.Range(8f, 20f) + 2f * level) * m);
                case StatType.Armor: return Mathf.Round((Random.Range(5f, 15f) + 2f * level) * m);
                case StatType.LightRadius: return Random.Range(0.5f, 1f) * m;
                case StatType.Str:
                case StatType.Dex:
                case StatType.Int:
                case StatType.Vit:
                case StatType.Wis: return Mathf.Round((Random.Range(1f, 3f) + level * 0.25f) * m);
                case StatType.MaxMp: return Mathf.Round((Random.Range(5f, 12f) + level) * m);
                default: return 1f; // Pierce / Multishot
            }
        }
    }
}
