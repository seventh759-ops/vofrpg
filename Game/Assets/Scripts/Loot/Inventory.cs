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
        public bool AutoSalvageCommon;

        public bool BagFull => Bag.Count >= BagCapacity;

        /// <summary>装備が変わった。能力の再計算に使う。</summary>
        public event Action Changed;

        public void Reset()
        {
            Array.Clear(Equipped, 0, Equipped.Length);
            Bag.Clear();
            Embers = 0;
            Equipped[(int)EquipSlot.Weapon] = ItemGenerator.StarterWeapon();
            Equipped[(int)EquipSlot.Lantern] = ItemGenerator.StarterLantern();
            Changed?.Invoke();
        }

        /// <summary>拾ったときに呼ぶ。入らなければ false。</summary>
        public bool Add(Item item)
        {
            if (AutoSalvageCommon && item.Rarity == Rarity.Common)
            {
                Embers += RarityInfo.SalvageOf(item.Rarity);
                return true;
            }
            if (BagFull) return false;
            Bag.Add(item);
            return true;
        }

        /// <summary>鞄が満杯でも、拾えるものか（自動分解される白など）。</summary>
        public bool CanTake(Item item) => !BagFull || (AutoSalvageCommon && item.Rarity == Rarity.Common);

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

        public int SalvageAllCommon()
        {
            int n = 0;
            for (int i = Bag.Count - 1; i >= 0; i--)
            {
                if (Bag[i].Rarity != Rarity.Common) continue;
                Embers += RarityInfo.SalvageOf(Rarity.Common);
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
