using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 図鑑。倒した敵・ボス、拾った固有効果の品・深淵のセットの品を、全キャラクターで共有して埋めていく。
    /// 8つ埋めるごとに、全キャラクターの全能力値 +1。
    /// </summary>
    public static class Codex
    {
        public const int PerReward = 8;
        public enum Category { Enemies, Bosses, Uniques, Sets }
        public const int CategoryCount = 4;

        public sealed class Entry
        {
            public string Id;
            public Category Cat;
            public System.Func<Sprite> Icon;
            public System.Func<string> Name;
            public System.Func<string> Desc;
        }

        static List<Entry> all;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() { all = null; Discovered = null; }

        /// <summary>初めて載ったとき（名前、区切りに届いて能力値が上がったか）。受け取るのは1つだけ（ゲームの画面）。</summary>
        static System.Action<string, bool> Discovered;
        public static void SetListener(System.Action<string, bool> listener) => Discovered = listener;

        static readonly EnemyKind[] Kinds = { EnemyKind.Slime, EnemyKind.Bat, EnemyKind.Caster, EnemyKind.Brute, EnemyKind.Bomber };
        static readonly UniqueType[] TowerUniques = { UniqueType.ExplodeOnKill, UniqueType.HealOnKill, UniqueType.SplitShot, UniqueType.DodgeNova, UniqueType.DoubleStrike, UniqueType.Vampiric };
        static readonly ItemSlot[] Slots = { ItemSlot.Weapon, ItemSlot.Lantern, ItemSlot.Armor, ItemSlot.Ring };

        public static string KindName(EnemyKind k)
        {
            switch (k)
            {
                case EnemyKind.Bat: return Loc.T("Bat", "コウモリ");
                case EnemyKind.Caster: return Loc.T("Caster", "術士");
                case EnemyKind.Brute: return Loc.T("Brute", "巨漢");
                case EnemyKind.Bomber: return Loc.T("Bomber", "爆弾虫");
                case EnemyKind.Golden: return Loc.T("Golden Slime", "黄金のスライム");
                default: return Loc.T("Slime", "スライム");
            }
        }

        static string SlotName(ItemSlot s)
        {
            switch (s)
            {
                case ItemSlot.Lantern: return Loc.T("Lantern", "ランタン");
                case ItemSlot.Armor: return Loc.T("Armor", "防具");
                case ItemSlot.Ring: return Loc.T("Ring", "指輪");
                default: return Loc.T("Weapon", "武器");
            }
        }

        static string ZoneName(ZoneSpec z) => Loc.T(z.Name, z.NameJa);

        public static List<Entry> All
        {
            get
            {
                if (all != null) return all;
                all = new List<Entry>();
                // 敵：塔の区画ごとのスライム、ほかの敵、黄金、深淵の区画ごとの敵
                for (int i = 0; i < Zones.Count; i++)
                {
                    var z = Zones.Tower(i);
                    all.Add(new Entry { Id = "e:slime:" + i, Cat = Category.Enemies, Icon = () => PixelArt.SlimeFor(z), Name = () => KindName(EnemyKind.Slime), Desc = () => ZoneName(z) });
                }
                for (int k = 1; k < Kinds.Length; k++)
                {
                    var kind = Kinds[k];
                    all.Add(new Entry { Id = "e:" + kind, Cat = Category.Enemies, Icon = () => PixelArt.EnemySprite(kind), Name = () => KindName(kind), Desc = () => Loc.T("The Tower", "塔") });
                }
                all.Add(new Entry { Id = "e:Golden", Cat = Category.Enemies, Icon = () => PixelArt.EnemySprite(EnemyKind.Golden), Name = () => KindName(EnemyKind.Golden), Desc = () => Loc.T("Rare", "まれ") });
                for (int i = 0; i < Zones.AbyssCount; i++)
                {
                    var z = Zones.AbyssZone(i);
                    foreach (var kind in Kinds)
                    {
                        var kk = kind;
                        all.Add(new Entry { Id = $"a:{kk}:{i}", Cat = Category.Enemies, Icon = () => PixelArt.AbyssEnemy(kk, z), Name = () => KindName(kk), Desc = () => ZoneName(z) });
                    }
                }
                foreach (BossKind b in System.Enum.GetValues(typeof(BossKind)))
                {
                    var bb = b;
                    all.Add(new Entry { Id = "b:" + bb, Cat = Category.Bosses, Icon = () => PixelArt.BossSprite(bb), Name = () => BossInfo.Name(bb), Desc = () => "" });
                }
                foreach (var u in TowerUniques)
                {
                    var uu = u;
                    all.Add(new Entry { Id = "u:" + uu, Cat = Category.Uniques, Icon = () => PixelArt.ItemIcon(new Item { Slot = ItemSlot.Ring, Rarity = Rarity.Legendary, Level = 1 }), Name = () => StatText.Unique(uu), Desc = () => "" });
                }
                foreach (var set in AbyssSets.All)
                {
                    foreach (var slot in Slots)
                    {
                        var ss = set; var sl = slot;
                        var sample = new Item { Slot = sl, Weapon = WeaponKind.Greatblade, Rarity = Rarity.Legendary, Level = GameConfig.AbyssGearLevel(GameConfig.AbyssFirstFloor), Abyss = true };
                        sample.Uniques.Add(ss);
                        all.Add(new Entry { Id = $"s:{ss}:{sl}", Cat = Category.Sets, Icon = () => PixelArt.ItemIcon(sample), Name = () => AbyssSets.Name(ss), Desc = () => SlotName(sl) });
                    }
                }
                return all;
            }
        }

        public static int Total => All.Count;

        public static int Found
        {
            get
            {
                int n = 0;
                foreach (var e in All) if (Profile.CodexHas(e.Id)) n++;
                return n;
            }
        }

        /// <summary>図鑑の報酬：全能力値に足す値。</summary>
        public static int AttrBonus => Found / PerReward;

        public static int FoundIn(Category c)
        {
            int n = 0;
            foreach (var e in All) if (e.Cat == c && Profile.CodexHas(e.Id)) n++;
            return n;
        }

        public static int TotalIn(Category c)
        {
            int n = 0;
            foreach (var e in All) if (e.Cat == c) n++;
            return n;
        }

        static void Mark(string id)
        {
            Entry entry = null;
            foreach (var e in All) if (e.Id == id) { entry = e; break; }
            if (entry == null) return; // 図鑑にない（呼び出された手下など）
            int before = AttrBonus;
            if (!Profile.CodexAdd(id)) return;
            Discovered?.Invoke(entry.Name(), AttrBonus > before);
        }

        /// <summary>敵を倒した。zone はその層の区画。</summary>
        public static void MarkKill(Enemy e, ZoneSpec zone)
        {
            if (e == null) return;
            if (e.IsBoss) { Mark("b:" + e.BossKind); return; }
            if (e.Kind == EnemyKind.Golden) { Mark("e:Golden"); return; }
            if (zone != null && zone.Pattern >= FloorPattern.Rift)
            {
                int ai = Zones.AbyssIndex(zone);
                if (ai >= 0) Mark($"a:{e.Kind}:{ai}");
                return;
            }
            if (e.Kind == EnemyKind.Slime)
            {
                int ti = Zones.TowerIndex(zone);
                if (ti >= 0) Mark("e:slime:" + ti);
                return;
            }
            Mark("e:" + e.Kind);
        }

        /// <summary>装備を拾った。</summary>
        public static void MarkItem(Item it)
        {
            if (it == null || it.Uniques == null) return;
            var set = AbyssSets.SetOf(it);
            if (set != UniqueType.None) Mark($"s:{set}:{it.Slot}");
            foreach (var u in it.Uniques) if (System.Array.IndexOf(TowerUniques, u) >= 0) Mark("u:" + u);
        }
    }
}
