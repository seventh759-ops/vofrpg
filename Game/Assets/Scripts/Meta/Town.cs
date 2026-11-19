using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 町（メニュー型）での売り買い・出し入れ。数値はすべて仮で、遊んでから調整する。
    /// 倉庫番：倉庫（全職業で共有）と「次の挑戦に持ち込む品」（職業ごと）の出し入れ。
    /// 商人：回復薬・魔力の薬、装備の箱。 女神像：転職・転生の儀。 荷運び：持ち帰り枠を増やす。
    /// 支払いはその職業が持ち帰った火種（CharacterData.Embers）。
    /// </summary>
    public static class Town
    {
        public const int StorageCapacity = 60;
        public const int MaxCarrySlots = 8;
        public const int MaxPotions = 99; // ボスの攻撃が厳しくなったので、たくさん持てる
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

        /// <summary>まとめて買う（買えるだけ。上限と火種で止まる）。買えた数を返す。</summary>
        public static int BuyPotions(CharacterData ch, bool hp, int count)
        {
            int n = 0;
            for (int i = 0; i < count; i++)
            {
                int have = hp ? ch.HpPotions : ch.MpPotions;
                if (have >= MaxPotions || !Pay(ch, PotionPrice(ch))) break;
                if (hp) ch.HpPotions++;
                else ch.MpPotions++;
                n++;
            }
            if (n > 0) Profile.Save();
            return n;
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
                item = ItemGenerator.Roll(Mathf.Min(ch.Level, Progression.MaxLevel), 2.5f);
                if (item.Rarity >= Rarity.Rare && item.Rarity <= Rarity.Relic) break;
            }
            if (item.Rarity < Rarity.Rare || item.Rarity > Rarity.Relic) item = ItemGenerator.Roll(Mathf.Min(ch.Level, Progression.MaxLevel), 2.5f);
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

        /// <summary>町（倉庫番）で装備できないわけ。装備できるなら null。</summary>
        public static string CannotEquip(CharacterData ch, Item item)
        {
            if (item == null || ch == null) return "";
            if (OtherClassWeapon(item, (ClassType)ch.ClassId)) return Loc.T("OTHER CLASS", "ほかの職業");
            if (item.ReqLevel > ch.Level) return Loc.T($"NEED LV {item.ReqLevel}", $"Lv{item.ReqLevel}から");
            return null;
        }

        /// <summary>その品を装備したら入れ替わる、今つけている品（なければ null）。指輪は空いている方、両方埋まっていれば1つ目。</summary>
        public static Item EquippedFor(CharacterData ch, Item item)
        {
            int slot = EquipSlotFor(ch, item);
            if (slot < 0 || slot >= ch.Loadout.Count) return null;
            var cur = ch.Loadout[slot];
            return cur == null || cur.IsEmptyMarker ? null : cur;
        }

        static int EquipSlotFor(CharacterData ch, Item item)
        {
            var slot = Inventory.DefaultSlot(item);
            if (slot == EquipSlot.Ring1)
            {
                bool r1 = (int)EquipSlot.Ring1 < ch.Loadout.Count && ch.Loadout[(int)EquipSlot.Ring1] != null && !ch.Loadout[(int)EquipSlot.Ring1].IsEmptyMarker;
                bool r2 = (int)EquipSlot.Ring2 < ch.Loadout.Count && ch.Loadout[(int)EquipSlot.Ring2] != null && !ch.Loadout[(int)EquipSlot.Ring2].IsEmptyMarker;
                if (r1 && !r2) slot = EquipSlot.Ring2;
            }
            return (int)slot;
        }

        /// <summary>
        /// 倉庫・持ち込む品から、そのまま装備する。今つけている品は、装備した品があった所（倉庫か持ち込む品）の同じ位置へ入れ替わる。
        /// </summary>
        public static bool EquipFromTown(CharacterData ch, Item item, int slotOverride = -1)
        {
            if (CannotEquip(ch, item) != null) return false;
            List<Item> from = ch.Stash.Contains(item) ? ch.Stash : Profile.Storage.Contains(item) ? Profile.Storage : null;
            if (from == null) return false;
            while (ch.Loadout.Count < 5) ch.Loadout.Add(Item.EmptyMarker());
            int slot = slotOverride >= 0 && Inventory.Fits(item, (EquipSlot)slotOverride) ? slotOverride : EquipSlotFor(ch, item);
            var old = ch.Loadout[slot];
            int index = from.IndexOf(item);
            from.RemoveAt(index);
            ch.Loadout[slot] = item;
            if (old != null && !old.IsEmptyMarker) from.Insert(index, old);
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
            ch.Embers += SalvageValue(item);
            Profile.Save();
            return true;
        }

        /// <summary>
        /// Lv1・経験値0に戻し、振った能力値も戻して、そのときのボーナスのポイントだけを振れる状態にする（転職・3次職・転生で共通）。
        /// ボーナスは持ち越さない（上位職 30・3次職 60・転生 0。転生は全能力値+5 がずっと積み上がる）。
        /// スキルの熟練度・装備・火種・最深層はそのまま。必要レベルに届かなくなった装備は倉庫へ。
        /// </summary>
        static void BackToLevel1(CharacterData ch, int bonusPoints)
        {
            ch.Level = 1;
            ch.Exp = 0;
            for (int i = 0; i < ch.Alloc.Length; i++) ch.Alloc[i] = 0;
            ch.Points = bonusPoints;
            ch.MpGrown = 0;
            StoreUnusableGear(ch);
        }

        /// <summary>転職：基本職 Lv50 から、2つの上位職のどちらかへ。ボーナスで 30 ポイント振れる。7つ目のスキルを覚える。</summary>
        public static bool ChangeClass(CharacterData ch, int index)
        {
            if (!AdvancedClasses.CanChange(ch)) return false;
            ch.Embers -= AdvancedClasses.Cost;
            ch.Advanced = Mathf.Clamp(index, 0, 1);
            ch.Third = false;
            BackToLevel1(ch, AdvancedClasses.BonusPoints);
            Profile.Save();
            return true;
        }

        /// <summary>3次職への転職：上位職 Lv50 から、上位職ごとに決まった3次職へ。ボーナスで 60 ポイント振れる。8つ目のスキルを覚える。</summary>
        public static bool PromoteThird(CharacterData ch)
        {
            if (!ThirdClasses.CanPromote(ch)) return false;
            ch.Embers -= ThirdClasses.PromoteCost;
            ch.Third = true;
            BackToLevel1(ch, ThirdClasses.BonusPoints);
            Profile.Save();
            return true;
        }

        /// <summary>
        /// 転生：どの職でも Lv99 から（何回でも、火種は毎回倍）。基本職に戻って Lv1。転生の回数が1つ増える（全能力値+5）。
        /// ボーナスのポイントは持ち越さない。上位職・3次職のスキルはボタンから外す（転職し直すと、熟練度はそのままで戻る）。
        /// </summary>
        public static bool Rebirth(CharacterData ch)
        {
            if (!ThirdClasses.CanRebirth(ch)) return false;
            ch.Embers -= ThirdClasses.Cost(ch);
            ch.Rebirths++;
            if (SpecialClass.RebirthBecomesSpecial(ch))
            {
                // 基本職のまま転生した：彩灯者になる（職業そのものが変わる。スキルも別）
                ch.OriginClassId = ch.ClassId;
                ch.ClassId = (int)ClassType.Vividbearer;
                SwapSkills(ch);
            }
            else if (ch.Special)
            {
                // 彩灯者から転生した：もとの基本職へ戻る
                ch.ClassId = Mathf.Clamp(ch.OriginClassId, 0, ClassInfo.PlayableCount - 1);
                SwapSkills(ch);
            }
            ch.Advanced = -1;
            ch.Third = false;
            for (int i = 0; i < ch.Slots.Length; i++)
                if (ch.Slots[i] == AdvancedClasses.SkillIndex || ch.Slots[i] == ThirdClasses.SkillIndex) ch.Slots[i] = -1;
            BackToLevel1(ch, 0);
            Profile.Save();
            return true;
        }

        /// <summary>
        /// もとの職業と彩灯者を行き来するとき：スキルの熟練度を入れ替え（使っていない方は取っておく）、ボタンは1つ目のスキルだけにする。
        /// </summary>
        static void SwapSkills(CharacterData ch)
        {
            var lv = ch.StashedSkillLv;
            var uses = ch.StashedSkillUses;
            ch.StashedSkillLv = ch.SkillLv;
            ch.StashedSkillUses = ch.SkillUses;
            ch.SkillLv = lv != null && lv.Length == SkillInfo.PerClass ? lv : NewArray(SkillInfo.PerClass, 1);
            ch.SkillUses = uses != null && uses.Length == SkillInfo.PerClass ? uses : NewArray(SkillInfo.PerClass, 0);
            ch.Slots = new[] { 0, -1, -1 };
            ch.SlotAuto = new bool[SkillInfo.Slots];
        }

        static int[] NewArray(int n, int fill)
        {
            var a = new int[n];
            for (int i = 0; i < n; i++) a[i] = fill;
            return a;
        }

        /// <summary>直前の転職・転生で倉庫へ送った品の数（知らせるため）。</summary>
        public static int LastStored { get; private set; }

        /// <summary>
        /// 転職・転生で Lv1 に戻ったとき、必要レベルに届かなくなった装備を外して倉庫へ送る。
        /// 鞄に入れると「持ち込んだ品」として毎回ついて回ってしまうため。倉庫がいっぱいなら付けたままにする
        /// （次の挑戦の始めに鞄へ移る）。
        /// </summary>
        static void StoreUnusableGear(CharacterData ch)
        {
            LastStored = 0;
            for (int i = 0; i < ch.Loadout.Count; i++)
            {
                var it = ch.Loadout[i];
                if (it == null || it.IsEmptyMarker || (it.ReqLevel <= ch.Level && !OtherClassWeapon(it, (ClassType)ch.ClassId))) continue; // レベルが足りない品と、使えなくなった職業の武器
                if (Profile.Storage.Count >= StorageCapacity) break;
                it.Carried = false;
                Profile.Storage.Add(it);
                ch.Loadout[i] = Item.EmptyMarker();
                LastStored++;
            }
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
        public static int EnhancePrice(Item it) => EnhancePriceAt(it, it.Enhance);

        /// <summary>強化の火種の倍率（+10・+15 に素材の品が要るようにした分、軽くした）。</summary>
        public const float EnhanceEmberMult = 0.6f;

        static int EnhancePriceAt(Item it, int enhance) =>
            Mathf.RoundToInt((20f + it.Level * 3f) * RarityCost(it.Rarity) * Mathf.Pow(enhance + 1, 1.4f) * EnhanceEmberMult);

        // ---------------- 持ち帰った品を素材にする（鍛冶屋） ----------------
        // ・強化の節目（+10・+15 に上げるとき）には、同じ部位・同じ珍しさ（深淵・天界の品は同じ種類）の品が1つ要る
        // ・部位の変換：持ち帰った品を、同じ珍しさ・同じレベルの別の部位の品に作り直す
        // ・合成：同じ珍しさの品を3つで、1つ上の珍しさの品1つにする（紫→橙、橙→赤）
        // 素材にできるのは、持ち込む品（前回持ち帰った品）と倉庫の品。装備中の品・ロック中の品は使わない。

        /// <summary>次の強化で素材が要るか（+9→+10、+14→+15）。</summary>
        public static bool NeedsMaterial(Item it) => it != null && (it.Enhance + 1) % 10 == 0 || it != null && it.Enhance + 1 == 15;

        static bool SameKind(Item a, Item b) => a.Rarity == b.Rarity && a.Abyss == b.Abyss && a.Heaven == b.Heaven;

        /// <summary>素材にしてよい品（持ち込む品と倉庫。装備中・ロック中・target 自身は除く）。</summary>
        static IEnumerable<(List<Item> list, Item item)> Spare(CharacterData ch, Item target)
        {
            foreach (var it in ch.Stash) if (it != null && it != target && !it.Locked && !it.IsEmptyMarker) yield return (ch.Stash, it);
            foreach (var it in Profile.Storage) if (it != null && it != target && !it.Locked && !it.IsEmptyMarker) yield return (Profile.Storage, it);
        }

        /// <summary>強化の素材にする品（同じ部位・同じ珍しさで、いちばんレベルの低いもの）。なければ null。</summary>
        public static Item MaterialFor(CharacterData ch, Item target)
        {
            Item best = null;
            foreach (var (_, it) in Spare(ch, target))
                if (it.Slot == target.Slot && SameKind(it, target) && (best == null || it.Level < best.Level)) best = it;
            return best;
        }

        public static int MaterialCount(CharacterData ch, Item target)
        {
            int n = 0;
            foreach (var (_, it) in Spare(ch, target)) if (it.Slot == target.Slot && SameKind(it, target)) n++;
            return n;
        }

        static void RemoveSpare(CharacterData ch, Item it)
        {
            if (!ch.Stash.Remove(it)) Profile.Storage.Remove(it);
        }

        /// <summary>品が持ち込む品か倉庫にあるか（部位の変換・合成ができるのは、装備していない品だけ）。</summary>
        public static bool IsSpare(CharacterData ch, Item it) => it != null && (ch.Stash.Contains(it) || Profile.Storage.Contains(it));

        /// <summary>部位の変換・合成ができる珍しさ（素材になる橙・赤。合成は紫から）。</summary>
        public static bool Convertible(Item it) => it != null && it.Rarity >= Rarity.Relic;
        public static int ConvertPrice(Item it) => Mathf.RoundToInt((20f + it.Level * 3f) * RarityCost(it.Rarity) * 1.5f);

        static readonly ItemSlot[] slotOrder = { ItemSlot.Weapon, ItemSlot.Lantern, ItemSlot.Armor, ItemSlot.Ring };

        /// <summary>部位の変換：次の部位（武器→ランタン→鎧→指輪→武器）の、同じ珍しさ・同じレベルの品に作り直す。</summary>
        public static Item Convert(CharacterData ch, Item it)
        {
            if (!Convertible(it) || !IsSpare(ch, it) || !Pay(ch, ConvertPrice(it))) return null;
            int si = System.Array.IndexOf(slotOrder, it.Slot);
            var next = slotOrder[(si + 1) % slotOrder.Length];
            var made = ItemGenerator.RollExact(it.Level, it.Rarity, next, it.Heaven);
            Replace(ch, it, made);
            Profile.Save();
            return made;
        }

        static void Replace(CharacterData ch, Item old, Item made)
        {
            int i = ch.Stash.IndexOf(old);
            if (i >= 0) { ch.Stash[i] = made; return; }
            i = Profile.Storage.IndexOf(old);
            if (i >= 0) Profile.Storage[i] = made;
        }

        /// <summary>合成できるか：紫・橙の品で、同じ種類の品があと2つある。</summary>
        public static bool Synthesizable(Item it) => it != null && (it.Rarity == Rarity.Legendary || it.Rarity == Rarity.Relic);
        public static int SynthPrice(Item it) => Mathf.RoundToInt((20f + it.Level * 3f) * RarityCost(it.Rarity) * 3f);

        /// <summary>合成の相手（同じ珍しさ・同じ種類で、レベルの低い順に2つ）。部位は問わない。</summary>
        public static List<Item> SynthPartners(CharacterData ch, Item it)
        {
            var list = new List<Item>();
            foreach (var (_, o) in Spare(ch, it)) if (SameKind(o, it)) list.Add(o);
            list.Sort((a, b) => a.Level.CompareTo(b.Level));
            if (list.Count > 2) list.RemoveRange(2, list.Count - 2);
            return list;
        }

        /// <summary>合成：選んだ品と、同じ種類の品2つを使い、選んだ品と同じ部位・同じレベルで1つ上の珍しさの品を作る。</summary>
        public static Item Synthesize(CharacterData ch, Item it)
        {
            if (!Synthesizable(it) || !IsSpare(ch, it)) return null;
            var partners = SynthPartners(ch, it);
            if (partners.Count < 2 || !Pay(ch, SynthPrice(it))) return null;
            var made = ItemGenerator.RollExact(it.Level, it.Rarity + 1, it.Slot, it.Heaven);
            Replace(ch, it, made);
            foreach (var p in partners) RemoveSpare(ch, p);
            Profile.Save();
            return made;
        }

        /// <summary>分解・売却で戻る、強化に使った火種の割合。</summary>
        public const float EnhanceRefund = 0.5f;

        /// <summary>その品の強化に使った火種の合計（+0 から今の段まで）。</summary>
        public static int EnhanceSpent(Item it)
        {
            int sum = 0;
            for (int e = 0; e < it.Enhance; e++) sum += EnhancePriceAt(it, e);
            return sum;
        }

        /// <summary>分解・売却で得る火種。珍しさの分（レベルが高いほど多い：Lv1 で1倍、Lv50 で2倍、Lv100 で3倍）に、強化に使った火種の半分を足す。</summary>
        public static int SalvageValue(Item it) =>
            Mathf.RoundToInt(RarityInfo.SalvageOf(it.Rarity) * (1f + Mathf.Max(0, it.Level - 1) / 49.5f)) + (it.Enhance > 0 ? Mathf.RoundToInt(EnhanceSpent(it) * EnhanceRefund) : 0);

        /// <summary>効果の再抽選の値段。</summary>
        /// <summary>再抽選の値段。ロックした効果が1つ増えるごとに倍になる（0:x1 1:x2 2:x4 3:x8）。</summary>
        public static int RerollPrice(Item it, int locks = 0) => Mathf.RoundToInt((30f + it.Level * 4f) * RarityCost(it.Rarity)) << Mathf.Clamp(locks, 0, MaxRerollLocks);

        public const int MaxRerollLocks = 3;

        /// <summary>効果の再抽選ができるのは神話（赤）の装備だけ。</summary>
        public static bool Smithable(Item it) => it != null && it.Rarity == Rarity.Mythic;

        /// <summary>強化できるのは灯宝（橙）以上（橙は+10、赤と深淵の品は+15まで）。</summary>
        public static bool Enhanceable(Item it) => it != null && it.EnhanceCap > 0;

        /// <summary>
        /// 捧げる（クリア後の女神像）：持っている火種をすべてと、レベルを捧げる（Lv1 に戻る）と、塔が反転して「昇る塔」になる。
        /// 深淵は閉じ、昇る塔は1層から（塔の記録・レベルの上限・職業・転生の回数はそのまま）。中断した挑戦は消える。1回だけ。
        /// </summary>
        public static bool CanOffer(CharacterData ch) => ch != null && ch.EndingSeen && !ch.Ascended && ch.Embers > 0;

        public static bool Offer(CharacterData ch)
        {
            if (!CanOffer(ch)) return false;
            ch.Embers = 0;
            // レベルも捧げる：Lv1 に戻る（職業・転生の回数・装備・スキルはそのまま。付けられなくなった装備は倉庫へ）。
            // 上位職・3次職の儀のボーナスのポイントは、職業を失わないので、もう一度もらえる
            BackToLevel1(ch, ch.Third ? ThirdClasses.BonusPoints : ch.Advanced >= 0 ? AdvancedClasses.BonusPoints : 0);
            ch.Ascended = true;
            ch.AscentBest = 0;
            ch.AscentBossCleared = 0;
            ch.Run = new RunSave();
            Profile.Save();
            return true;
        }

        /// <summary>深淵の品の強化に要る深淵の欠片：1段ごとに1個、最後の +14→+15 だけ15個。</summary>
        public static int ShardPrice(Item it) => !it.Abyss && !it.Heaven ? 0 : it.Enhance == Item.MaxEnhance - 1 ? 15 : 1;

        public static bool EnhanceItem(CharacterData ch, Item it)
        {
            if (!Enhanceable(it) || it.Enhance >= it.EnhanceCap) return false;
            int shards = ShardPrice(it);
            if (ch.AbyssShards < shards || ch.Embers < EnhancePrice(it)) return false;
            Item material = null;
            if (NeedsMaterial(it))
            {
                material = MaterialFor(ch, it);
                if (material == null) return false; // 節目の強化には、同じ部位・同じ珍しさの品が要る
            }
            if (!Pay(ch, EnhancePrice(it))) return false;
            ch.AbyssShards -= shards;
            if (material != null) RemoveSpare(ch, material);
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
            item.Slot == ItemSlot.Weapon && ClassInfo.Weapon(item.Weapon).Class != type && type != ClassType.Vividbearer; // 彩灯者は、どの職業の武器も使える
    }
}
