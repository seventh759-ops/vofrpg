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

        /// <summary>
        /// 挑戦の開始。前回帰還したときの装備を付け、倉庫の品を鞄に入れた状態で始める
        /// （どちらもなければ初期装備）。持ち込んだ品には Carried の印を付ける。
        /// </summary>
        public void Reset(ClassType type, CharacterData ch)
        {
            Array.Clear(Equipped, 0, Equipped.Length);
            Bag.Clear();
            Embers = 0;

            if (ch != null)
            {
                for (int i = 0; i < Equipped.Length && i < ch.Loadout.Count; i++)
                {
                    var it = ch.Loadout[i];
                    if (it == null || it.IsEmptyMarker || !Fits(it, (EquipSlot)i)) continue;
                    it.Carried = true;
                    Equipped[i] = it;
                }
                foreach (var it in ch.Stash)
                {
                    if (Bag.Count >= BagCapacity) break;
                    it.Carried = true;
                    Bag.Add(it);
                }
            }

            if (Equipped[(int)EquipSlot.Weapon] == null) Equipped[(int)EquipSlot.Weapon] = ItemGenerator.StarterWeapon(type);
            if (Equipped[(int)EquipSlot.Lantern] == null) Equipped[(int)EquipSlot.Lantern] = ItemGenerator.StarterLantern();
            Changed?.Invoke();
        }

        /// <summary>帰還：装備中の5つと、持ち込んだ品と、選んだ品（今回拾った物）を保存する。</summary>
        public void SaveForReturn(CharacterData ch, List<Item> chosen)
        {
            ch.Loadout.Clear();
            foreach (var it in Equipped) ch.Loadout.Add(it ?? Item.EmptyMarker());
            ch.Stash.Clear();
            foreach (var it in Bag)
            {
                if (it.Carried || chosen.Contains(it)) ch.Stash.Add(it);
            }
            ch.Embers += Embers;
        }

        /// <summary>中断のために、今の装備・鞄・火種を書き出す。</summary>
        public void SaveRun(RunSave run)
        {
            run.Equipped.Clear();
            foreach (var it in Equipped) run.Equipped.Add(it ?? Item.EmptyMarker());
            run.Bag.Clear();
            run.BagCarried.Clear();
            foreach (var it in Bag)
            {
                run.Bag.Add(it);
                run.BagCarried.Add(it.Carried);
            }
            run.Embers = Embers;
        }

        /// <summary>中断した挑戦の装備・鞄・火種に戻す。</summary>
        public void RestoreRun(RunSave run)
        {
            Array.Clear(Equipped, 0, Equipped.Length);
            Bag.Clear();
            for (int i = 0; i < Equipped.Length && i < run.Equipped.Count; i++)
            {
                var it = run.Equipped[i];
                if (it == null || it.IsEmptyMarker || !Fits(it, (EquipSlot)i)) continue;
                it.Carried = true; // 装備中の品は帰還でいつも持ち帰れる
                Equipped[i] = it;
            }
            for (int i = 0; i < run.Bag.Count && Bag.Count < BagCapacity; i++)
            {
                var it = run.Bag[i];
                if (it == null || it.IsEmptyMarker) continue;
                it.Carried = i < run.BagCarried.Count && run.BagCarried[i];
                Bag.Add(it);
            }
            Embers = run.Embers;
            Changed?.Invoke();
        }

        /// <summary>今回拾った品（持ち込んだ品ではない）。</summary>
        public List<Item> NewThisRun()
        {
            var list = new List<Item>();
            foreach (var it in Bag)
            {
                if (!it.Carried) list.Add(it);
            }
            return list;
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
