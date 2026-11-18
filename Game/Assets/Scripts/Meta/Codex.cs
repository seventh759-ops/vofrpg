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
            public System.Func<string> Detail; // 詳しい説明（見つけたあと）
            public System.Func<string> Hint;   // どこで見つかるか（見つける前）
        }

        static string KindDetail(EnemyKind k)
        {
            switch (k)
            {
                case EnemyKind.Bat: return Loc.T("Chases you while weaving side to side. Up close, it rises and dives straight at you.", "左右にふらふら揺れながら追ってくる。近くまで来ると、ふわっと舞い上がってから急降下してくる。");
                case EnemyKind.Caster: return Loc.T("Keeps its distance and fires bullets now and then.", "ちょうどよい距離を保ち、ときどき弾を撃ってくる。");
                case EnemyKind.Brute: return Loc.T("Slow and tough. Flashes red while charging up a heavy blow.", "遅くて硬い。赤く点滅して力をためたあと、重い一撃を放つ。");
                case EnemyKind.Bomber: return Loc.T("Runs at you and explodes. Kill it before it reaches you.", "近づいてきて爆発する。たどり着く前に倒したい。");
                case EnemyKind.Golden: return Loc.T("Appears rarely and runs away. Drops lots of embers and gear.", "まれに現れ、逃げ回る。倒すと火種と装備をたくさん落とす。しばらくすると消える。");
                default: return Loc.T("Comes straight at you and pounces when close. Weak alone, dangerous in numbers.", "まっすぐ寄ってきて、近くでぎゅっと縮んでから跳びかかる。1体なら弱いが、群れると手強い。");
            }
        }

        static string BossDetail(BossKind b)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(Loc.T("Attacks:", "使う技："));
            foreach (var a in BossInfo.AttacksOf(b))
            {
                sb.Append("\n・").Append(BossInfo.AttackName(a));
                var d = BossInfo.DebuffOf(b, a);
                if (d.HasValue) sb.Append(Loc.T($" ({Debuffs.Name(d.Value)})", $"（{Debuffs.Name(d.Value)}）"));
            }
            var extra = BossInfo.LoopExtraOf(b);
            if (extra.HasValue) sb.Append(Loc.T("\nFrom the 2nd loop: ", "\n2周目から：")).Append(BossInfo.AttackName(extra.Value));
            return sb.ToString();
        }

        static ZoneSpec BossZone(BossKind b) => (int)b < Zones.Count ? Zones.Tower((int)b) : Zones.AbyssZone(Mathf.Clamp((int)b - Zones.Count, 0, Zones.AbyssCount - 1));

        static string SetDetail(UniqueType set)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(StatText.Unique(set));
            foreach (int t in AbyssSets.Tiers) sb.Append("\n").Append(Loc.T($"{t} pieces: ", $"{t}つ：")).Append(AbyssSets.BonusText(set, t));
            return sb.ToString();
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
                    bool tome = z.Pattern == FloorPattern.Gold; // 沈んだ書庫は、スライムの代わりにさまよう本
                    all.Add(new Entry { Id = "e:slime:" + i, Cat = Category.Enemies, Icon = () => PixelArt.SlimeFor(z),
                        Name = () => tome ? Loc.T("Wandering Tome", "さまよう本") : KindName(EnemyKind.Slime), Desc = () => ZoneName(z),
                        Detail = () => tome ? Loc.T("An old book that came to life. Flaps its pages, hops toward you and pounces when close.", "命を持った古い本。ページをばたつかせて跳ねてきて、近くで跳びかかる。1冊なら弱いが、群れると手強い。") : KindDetail(EnemyKind.Slime), Hint = () => Loc.T($"Found in {ZoneName(z)}.", $"{ZoneName(z)}で出会う。") });
                }
                for (int k = 1; k < Kinds.Length; k++)
                {
                    var kind = Kinds[k];
                    all.Add(new Entry { Id = "e:" + kind, Cat = Category.Enemies, Icon = () => PixelArt.EnemySprite(kind), Name = () => KindName(kind), Desc = () => Loc.T("The Tower", "塔"),
                        Detail = () => KindDetail(kind), Hint = () => Loc.T("Found in the tower.", "塔のどこかで出会う。") });
                }
                all.Add(new Entry { Id = "e:Golden", Cat = Category.Enemies, Icon = () => PixelArt.EnemySprite(EnemyKind.Golden), Name = () => KindName(EnemyKind.Golden), Desc = () => Loc.T("Rare", "まれ"),
                    Detail = () => KindDetail(EnemyKind.Golden), Hint = () => Loc.T("A rare, shining enemy.", "まれに現れる、光る敵。") });
                for (int i = 0; i < Zones.AbyssCount; i++)
                {
                    var z = Zones.AbyssZone(i);
                    foreach (var kind in Kinds)
                    {
                        var kk = kind;
                        all.Add(new Entry { Id = $"a:{kk}:{i}", Cat = Category.Enemies, Icon = () => PixelArt.AbyssEnemy(kk, z), Name = () => KindName(kk), Desc = () => ZoneName(z),
                            Detail = () => KindDetail(kk) + Loc.T("\nTwisted by the Abyss: tougher and harder-hitting.", "\n深淵に染まり、硬く、強くなっている。"),
                            Hint = () => Loc.T($"Found in {ZoneName(z)}.", $"{ZoneName(z)}で出会う。") });
                    }
                }
                foreach (BossKind b in System.Enum.GetValues(typeof(BossKind)))
                {
                    var bb = b;
                    if (bb > BossKind.AbyssKing)
                    {
                        // クリア後のボス（昇る塔の最上の女神像など）
                        all.Add(new Entry { Id = "b:" + bb, Cat = Category.Bosses, Icon = () => PixelArt.BossSprite(bb), Name = () => BossInfo.Name(bb), Desc = () => Loc.T("Top of the rising tower", "昇る塔の最上"),
                            Detail = () => BossDetail(bb), Hint = () => Loc.T("Waits at the very top of the rising tower.", "昇る塔のいちばん上で待っている。") });
                        continue;
                    }
                    all.Add(new Entry { Id = "b:" + bb, Cat = Category.Bosses, Icon = () => PixelArt.BossSprite(bb), Name = () => BossInfo.Name(bb), Desc = () => ZoneName(BossZone(bb)),
                        Detail = () => BossDetail(bb), Hint = () => Loc.T($"Waits at the end of {ZoneName(BossZone(bb))}.", $"{ZoneName(BossZone(bb))}の最後の層で待っている。") });
                }
                foreach (var u in TowerUniques)
                {
                    var uu = u;
                    all.Add(new Entry { Id = "u:" + uu, Cat = Category.Uniques, Icon = () => PixelArt.UniqueIcon(uu), Name = () => StatText.Unique(uu), Desc = () => "",
                        Detail = () => Loc.T("A unique effect found on Legendary or better gear.", "伝説以上の装備に付く固有効果。"),
                        Hint = () => Loc.T("May appear on Legendary or better gear.", "伝説以上の装備に付くことがある。") });
                }
                foreach (var set in AbyssSets.All)
                {
                    foreach (var slot in Slots)
                    {
                        var ss = set; var sl = slot;
                        var sample = new Item { Slot = sl, Weapon = WeaponKind.Greatblade, Rarity = Rarity.Legendary, Level = GameConfig.AbyssGearLevel(GameConfig.AbyssFirstFloor), Abyss = true };
                        sample.Uniques.Add(ss);
                        all.Add(new Entry { Id = $"s:{ss}:{sl}", Cat = Category.Sets, Icon = () => PixelArt.ItemIcon(sample), Name = () => AbyssSets.Name(ss), Desc = () => SlotName(sl),
                            Detail = () => SetDetail(ss), Hint = () => Loc.T($"Abyss gear ({SlotName(sl)}) dropped beyond floor 100.", $"深淵（101層から）で落ちる{SlotName(sl)}。") });
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
