using System.Collections.Generic;
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

        /// <summary>町で捨てる（火種にはならない）。ロック中の品は捨てない。</summary>
        public static bool Discard(CharacterData ch, Item item)
        {
            if (item.Locked) return false;
            if (!ch.Stash.Remove(item) && !Profile.Storage.Remove(item)) return false;
            Profile.Save();
            return true;
        }

        /// <summary>町で売る（分解と同じく火種になる。火種はそのキャラクターに入る）。ロック中の品は売らない。</summary>
        public static bool Salvage(CharacterData ch, Item item)
        {
            if (item.Locked) return false;
            if (!ch.Stash.Remove(item) && !Profile.Storage.Remove(item)) return false;
            ch.Embers += RarityInfo.SalvageOf(item.Rarity);
            Profile.Save();
            return true;
        }

        /// <summary>
        /// 転職（1回だけ）。Lv1・経験値0に戻り、振った能力値も戻して、ボーナスのポイントだけを振れる状態にする。
        /// 7つ目のスキルを覚える。スキルの熟練度・装備・火種・最深層はそのまま。
        /// </summary>
        public static bool ChangeClass(CharacterData ch, int index)
        {
            if (!AdvancedClasses.CanChange(ch)) return false;
            ch.Embers -= AdvancedClasses.Cost;
            ch.Advanced = Mathf.Clamp(index, 0, 1);
            ch.Level = 1;
            ch.Exp = 0;
            for (int i = 0; i < ch.Alloc.Length; i++) ch.Alloc[i] = 0;
            ch.Points = AdvancedClasses.BonusPoints;
            ch.MpGrown = 0;
            Profile.Save();
            return true;
        }

        /// <summary>
        /// 転生（1回だけ）。3次職になり、Lv1・経験値0に戻って、振った能力値も戻し、ボーナスのポイントだけを振れる状態にする。
        /// 8つ目のスキルを覚える。スキルの熟練度・装備・火種・最深層はそのまま。
        /// </summary>
        public static bool Rebirth(CharacterData ch)
        {
            if (!ThirdClasses.CanRebirth(ch)) return false;
            ch.Embers -= ThirdClasses.Cost;
            ch.Third = true;
            ch.Level = 1;
            ch.Exp = 0;
            for (int i = 0; i < ch.Alloc.Length; i++) ch.Alloc[i] = 0;
            ch.Points = ThirdClasses.BonusPoints;
            ch.MpGrown = 0;
            Profile.Save();
            return true;
        }

        // ---- 鍛冶屋：強化と効果の再抽選 ----

        static float RarityCost(Rarity r)
        {
            switch (r)
            {
                case Rarity.Common: return 0.5f;
                case Rarity.Magic: return 0.7f;
                case Rarity.Rare: return 1f;
                case Rarity.Legendary: return 1.5f;
                case Rarity.Relic: return 2f;
                default: return 3f;
            }
        }

        /// <summary>次の強化の値段（高いレベル・珍しい品・強化が進んだ品ほど高い）。</summary>
        public static int EnhancePrice(Item it) =>
            Mathf.RoundToInt((20f + it.Level * 3f) * RarityCost(it.Rarity) * Mathf.Pow(it.Enhance + 1, 1.4f));

        /// <summary>効果の再抽選の値段。</summary>
        /// <summary>再抽選の値段。ロックした効果が1つ増えるごとに倍になる（0:x1 1:x2 2:x4 3:x8）。</summary>
        public static int RerollPrice(Item it, int locks = 0) => Mathf.RoundToInt((30f + it.Level * 4f) * RarityCost(it.Rarity)) << Mathf.Clamp(locks, 0, MaxRerollLocks);

        public const int MaxRerollLocks = 3;

        /// <summary>鍛冶屋で扱えるのは神話（赤）の装備だけ。</summary>
        public static bool Smithable(Item it) => it != null && it.Rarity == Rarity.Mythic;

        public static bool EnhanceItem(CharacterData ch, Item it)
        {
            if (!Smithable(it)) return false;
            if (it.Enhance >= Item.MaxEnhance || !Pay(ch, EnhancePrice(it))) return false;
            it.Enhance++;
            Profile.Save();
            return true;
        }

        public static bool RerollItem(CharacterData ch, Item it, ClassType type, ICollection<int> locked = null)
        {
            int locks = locked != null ? locked.Count : 0;
            if (!Smithable(it) || it.Affixes.Count == 0 || locks > MaxRerollLocks || locks >= it.Affixes.Count) return false;
            if (!Pay(ch, RerollPrice(it, locks))) return false;
            ItemGenerator.RerollAffixes(it, type, locked);
            Profile.Save();
            return true;
        }

        /// <summary>その武器を今の職業で使えないか（ほかの職業の武器）。</summary>
        public static bool OtherClassWeapon(Item item, ClassType type) =>
            item.Slot == ItemSlot.Weapon && ClassInfo.Weapon(item.Weapon).Class != type;
    }
}
