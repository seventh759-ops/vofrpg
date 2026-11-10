using System;
using System.Collections.Generic;

namespace VividOfFade
{
    /// <summary>装備欄5つと鞄と火種。挑戦をやり直すと初期装備に戻る（持ち帰りは M5）。</summary>
    public sealed class Inventory
    {
        public const int BagCapacity = 24;

        public readonly Item[] Equipped = new Item[5];
        public readonly List<Item> Bag = new List<Item>();
        public int Embers;
        /// <summary>拾った時点で自動分解する珍しさの上限。-1 で自動分解しない。</summary>
        public int AutoSalvageMax = -1;

        public bool BagFull => Bag.Count >= BagCapacity;

        /// <summary>装備が変わった。能力の再計算に使う。</summary>
        public event Action Changed;

        public void Reset(ClassType type)
        {
            Array.Clear(Equipped, 0, Equipped.Length);
            Bag.Clear();
            Embers = 0;
            Equipped[(int)EquipSlot.Weapon] = ItemGenerator.StarterWeapon(type);
            Equipped[(int)EquipSlot.Lantern] = ItemGenerator.StarterLantern();
            Changed?.Invoke();
        }

        /// <summary>拾ったときに呼ぶ。入らなければ false。</summary>
        public bool Add(Item item)
        {
            if (AutoSalvages(item))
            {
                Embers += RarityInfo.SalvageOf(item.Rarity);
                return true;
            }
            if (BagFull) return false;
            Bag.Add(item);
            return true;
        }

        /// <summary>鞄が満杯でも、拾えるものか（自動分解される白など）。</summary>
        public bool CanTake(Item item) => !BagFull || AutoSalvages(item);

        bool AutoSalvages(Item item) => (int)item.Rarity <= AutoSalvageMax;

        /// <summary>自動分解の段階を 切 → 白 → 青まで → 黄まで → 切 の順に切り替える。</summary>
        public void CycleAutoSalvage()
        {
            AutoSalvageMax = AutoSalvageMax >= (int)Rarity.Rare ? -1 : AutoSalvageMax + 1;
        }

        public static bool Fits(Item item, EquipSlot slot)
        {
            switch (item.Slot)
            {
                case ItemSlot.Weapon: return slot == EquipSlot.Weapon;
                case ItemSlot.Lantern: return slot == EquipSlot.Lantern;
                case ItemSlot.Armor: return slot == EquipSlot.Armor;
                default: return slot == EquipSlot.Ring1 || slot == EquipSlot.Ring2;
            }
        }

        public static EquipSlot DefaultSlot(Item item)
        {
            switch (item.Slot)
            {
                case ItemSlot.Weapon: return EquipSlot.Weapon;
                case ItemSlot.Lantern: return EquipSlot.Lantern;
                case ItemSlot.Armor: return EquipSlot.Armor;
                default: return EquipSlot.Ring1;
            }
        }

        /// <summary>鞄の品を装備する。外れた品は同じ場所へ戻る。</summary>
        public bool Equip(Item item, EquipSlot slot)
        {
            if (!Fits(item, slot)) return false;
            int index = Bag.IndexOf(item);
            if (index < 0) return false;

            var old = Equipped[(int)slot];
            Equipped[(int)slot] = item;
            Bag.RemoveAt(index);
            if (old != null) Bag.Insert(index, old);
            Changed?.Invoke();
            return true;
        }

        public bool Unequip(EquipSlot slot)
        {
            var item = Equipped[(int)slot];
            if (item == null || BagFull) return false;
            Equipped[(int)slot] = null;
            Bag.Add(item);
            Changed?.Invoke();
            return true;
        }

        public void Salvage(Item item)
        {
            if (Bag.Remove(item)) Embers += RarityInfo.SalvageOf(item.Rarity);
        }

        /// <summary>条件に合う品をまとめて分解する。ロック中の品は残す。分解した数を返す。</summary>
        public int SalvageWhere(Func<Item, bool> match)
        {
            int n = 0;
            for (int i = Bag.Count - 1; i >= 0; i--)
            {
                var item = Bag[i];
                if (item.Locked || !match(item)) continue;
                Embers += RarityInfo.SalvageOf(item.Rarity);
                Bag.RemoveAt(i);
                n++;
            }
            return n;
        }

        /// <summary>今の装備に item を付けたと仮定した装備欄（比較用）。</summary>
        public Item[] With(Item item, EquipSlot slot)
        {
            var copy = (Item[])Equipped.Clone();
            copy[(int)slot] = item;
            return copy;
        }
    }
}
