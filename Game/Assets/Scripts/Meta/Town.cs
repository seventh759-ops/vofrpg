using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 町（メニュー型）での売り買い・出し入れ。数値はすべて仮で、遊んでから調整する。
    /// 倉庫番：倉庫（全職業で共有）と「次の挑戦に持ち込む品」（職業ごと）の出し入れ。
    /// 商人：回復薬・魔力の薬、装備の箱。 師匠：転職。 荷運び：持ち帰り枠を増やす。
    /// 支払いはその職業が持ち帰った火種（CharacterData.Embers）。
    /// </summary>
    public static class Town
    {
        public const int StorageCapacity = 60;
        public const int MaxCarrySlots = 8;
        public const int MaxPotions = 5;
        public const float HpPotionHeal = 0.4f;  // 最大体力に対する割合
        public const float MpPotionRestore = 0.5f;
        public const float PotionCooldown = 1f;

        /// <summary>回復薬・魔力の薬の値段（深く潜っているほど高い）。</summary>
        public static int PotionPrice(CharacterData ch) => 20 + ch.BestFloor * 2;

        /// <summary>装備の箱の値段（中身はレベルに合った「稀」以上の装備）。</summary>
        public static int GearPrice(CharacterData ch) => 150 + ch.Level * 8;

        /// <summary>持ち帰り枠をもう1つ増やす値段（増やすほど倍になる）。</summary>
        public static int CarryPrice(CharacterData ch) => 300 << Mathf.Max(0, ch.CarrySlots - 3);

        static bool Pay(CharacterData ch, int price)
        {
            if (ch.Embers < price) return false;
            ch.Embers -= price;
            return true;
        }

        public static bool BuyPotion(CharacterData ch, bool hp)
        {
            int have = hp ? ch.HpPotions : ch.MpPotions;
            if (have >= MaxPotions || !Pay(ch, PotionPrice(ch))) return false;
            if (hp) ch.HpPotions++;
            else ch.MpPotions++;
            Profile.Save();
            return true;
        }

        /// <summary>装備の箱を買う。中身は「次の挑戦に持ち込む品」へ（いっぱいなら倉庫へ）。入らなければ買えない。</summary>
        public static Item BuyGear(ClassType type, CharacterData ch)
        {
            if (!HasRoom(ch) || ch.Embers < GearPrice(ch)) return null;
            ClassInfo.Current = type; // 武器はその職業のものが出る
            Item item = null;
            for (int i = 0; i < 30; i++)
            {
                item = ItemGenerator.Roll(ch.Level, 2.5f);
                if (item.Rarity >= Rarity.Rare && item.Rarity <= Rarity.Relic) break;
            }
            if (item.Rarity < Rarity.Rare || item.Rarity > Rarity.Relic) item = ItemGenerator.Roll(ch.Level, 2.5f);
            if (item.Rarity < Rarity.Rare) item.Rarity = Rarity.Rare;
            Pay(ch, GearPrice(ch));
            if (ch.Stash.Count < Inventory.BagCapacity) ch.Stash.Add(item);
            else Profile.Storage.Add(item);
            Profile.Save();
            return item;
        }

        public static bool HasRoom(CharacterData ch) =>
            ch.Stash.Count < Inventory.BagCapacity || Profile.Storage.Count < StorageCapacity;

        public static bool BuyCarrySlot(CharacterData ch)
        {
            if (ch.CarrySlots >= MaxCarrySlots || !Pay(ch, CarryPrice(ch))) return false;
            ch.CarrySlots++;
            Profile.Save();
            return true;
        }

        /// <summary>持ち込む品 → 倉庫。</summary>
        public static bool Store(CharacterData ch, Item item)
        {
            if (Profile.Storage.Count >= StorageCapacity || !ch.Stash.Remove(item)) return false;
            Profile.Storage.Add(item);
            Profile.Save();
            return true;
        }

        /// <summary>倉庫 → 持ち込む品。</summary>
        public static bool Take(CharacterData ch, Item item)
        {
            if (ch.Stash.Count >= Inventory.BagCapacity || OtherClassWeapon(item, (ClassType)ch.ClassId)) return false;
            if (!Profile.Storage.Remove(item)) return false;
            ch.Stash.Add(item);
            Profile.Save();
            return true;
        }

        /// <summary>町で分解する（火種はその職業に入る）。ロック中の品は分解しない。</summary>
        public static bool Salvage(CharacterData ch, Item item)
        {
            if (item.Locked) return false;
            if (!ch.Stash.Remove(item) && !Profile.Storage.Remove(item)) return false;
            ch.Embers += RarityInfo.SalvageOf(item.Rarity);
            Profile.Save();
            return true;
        }

        /// <summary>転職（1回だけ）。7つ目のスキルを覚える。</summary>
        public static bool ChangeClass(CharacterData ch, int index)
        {
            if (!AdvancedClasses.CanChange(ch)) return false;
            ch.Embers -= AdvancedClasses.Cost;
            ch.Advanced = Mathf.Clamp(index, 0, 1);
            Profile.Save();
            return true;
        }

        /// <summary>その武器を今の職業で使えないか（ほかの職業の武器）。</summary>
        public static bool OtherClassWeapon(Item item, ClassType type) =>
            item.Slot == ItemSlot.Weapon && ClassInfo.Weapon(item.Weapon).Class != type;
    }
}
