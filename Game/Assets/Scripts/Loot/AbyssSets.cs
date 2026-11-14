using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 深淵のセット：深淵の装備が持つ固有効果（業火・裂け目・星落とし・報い）が、そのままセットになる。
    /// レベルに関係なく同じ固有効果の品を数えるので、Lv100 の装備だけでもそろえられる。2つ・3つ・5つで効果が付く。
    /// 色もセットごと（枠・光の柱・アイコンのもや）。絵の意匠もセットごと（PixelArt.AbyssMotif）。
    /// </summary>
    public static class AbyssSets
    {
        public static readonly UniqueType[] All = { UniqueType.AbyssBlast, UniqueType.VoidRift, UniqueType.Starfall, UniqueType.Retribution };
        public static readonly int[] Tiers = { 2, 3, 5 };

        /// <summary>その品のセット（深淵の固有効果）。深淵の品でなければ None。</summary>
        public static UniqueType SetOf(Item it)
        {
            if (it == null || !it.Abyss) return UniqueType.None;
            foreach (var u in it.Uniques) if (System.Array.IndexOf(All, u) >= 0) return u;
            return UniqueType.None;
        }

        public static int Count(IEnumerable<Item> equipped, UniqueType set)
        {
            int n = 0;
            foreach (var it in equipped) if (it != null && !it.IsEmptyMarker && SetOf(it) == set) n++;
            return n;
        }

        public static string Name(UniqueType set)
        {
            switch (set)
            {
                case UniqueType.AbyssBlast: return Loc.T("Hellfire", "業火");
                case UniqueType.VoidRift: return Loc.T("Rift", "裂け目");
                case UniqueType.Starfall: return Loc.T("Starfall", "星落とし");
                case UniqueType.Retribution: return Loc.T("Retribution", "報い");
                default: return "";
            }
        }

        /// <summary>深淵の品の名前の頭（セットで変わる。例：紅蓮・稀の大剣）。</summary>
        public static string Prefix(UniqueType set)
        {
            switch (set)
            {
                case UniqueType.AbyssBlast: return Loc.T("Hellfire ", "紅蓮・");
                case UniqueType.VoidRift: return Loc.T("Riftborn ", "裂界・");
                case UniqueType.Starfall: return Loc.T("Starfallen ", "星墜・");
                case UniqueType.Retribution: return Loc.T("Vengeful ", "血報・");
                default: return Loc.T("Abyssal ", "深淵・");
            }
        }

        /// <summary>セットの色（明るい色）。</summary>
        public static Color ColorOf(UniqueType set)
        {
            switch (set)
            {
                case UniqueType.AbyssBlast: return new Color(1f, 0.4f, 0.15f);   // 業火：紅蓮
                case UniqueType.VoidRift: return new Color(0.7f, 0.35f, 1f);     // 裂け目：紫
                case UniqueType.Starfall: return new Color(0.5f, 0.88f, 1f);     // 星落とし：星の水色
                case UniqueType.Retribution: return new Color(0.9f, 0.1f, 0.3f);  // 報い：血の紅
                default: return RarityInfo.AbyssColor;
            }
        }

        /// <summary>セットの暗い色（枠の内側）。</summary>
        public static Color DarkOf(UniqueType set) => Color.Lerp(ColorOf(set), new Color(0.05f, 0f, 0.08f), 0.65f);

        /// <summary>そろえた数ごとの効果の説明。</summary>
        public static string BonusText(UniqueType set, int tier)
        {
            switch (set)
            {
                case UniqueType.AbyssBlast:
                    return tier == 2 ? Loc.T("Blast chance +15%", "大爆発の確率 +15%")
                         : tier == 3 ? Loc.T("Blast radius +40%", "大爆発の範囲 +40%")
                         : Loc.T("Blasts can set off one more blast", "大爆発で倒した敵も、もう1回だけ大爆発する");
                case UniqueType.VoidRift:
                    return tier == 2 ? Loc.T("Rifts open 30% more often", "裂け目の間隔 -30%")
                         : tier == 3 ? Loc.T("Rifts pull harder and wider", "裂け目の吸い込みが強く、広くなる")
                         : Loc.T("Rifts last longer and cut twice as hard", "裂け目が長く開き、切り裂く力が2倍");
                case UniqueType.Starfall:
                    return tier == 2 ? Loc.T("Crit chance +10%", "会心率 +10%")
                         : tier == 3 ? Loc.T("Two stars fall", "星が2つ落ちる")
                         : Loc.T("Stars scatter smaller stars on impact", "星が落ちた所に、小さな星が散る");
                case UniqueType.Retribution:
                    return tier == 2 ? Loc.T("Damage taken -10%", "受けるダメージ -10%")
                         : tier == 3 ? Loc.T("Retribution wave +50% size", "反撃の波動の範囲 +50%")
                         : Loc.T("Retribution heals 8% of max HP", "反撃のたびに、最大HPの8%を回復");
                default: return "";
            }
        }
    }
}
