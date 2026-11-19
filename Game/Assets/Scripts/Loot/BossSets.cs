using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// ボスのセット：塔のボス6体が、それぞれ自分のセットの品（武器・ランタン・外套・指輪）を約30%で落とす。珍しさは「ボス」。
    /// 装備欄は5つ（指輪は2つ）なので、同じセットの品を2つ・3つ・5つ付けると効果が付く（指輪2つも数える）。
    /// 名前・色・見た目（PixelArt.BossMotif）・効果は、そのボスの姿と技にちなむ。どれもこのゲーム用のオリジナル。
    /// </summary>
    public static class BossSets
    {
        public static readonly int[] Tiers = { 2, 3, 5 };
        /// <summary>セットのあるボス（塔の6体）。</summary>
        public const int Count6 = 6;
        public const float DropChance = 0.3f;

        public static bool HasSet(BossKind k) => (int)k < Count6;

        public static int SetOf(Item it) => it != null && it.IsBossGear ? it.BossSet : -1;

        public static int Count(IEnumerable<Item> equipped, int set)
        {
            int n = 0;
            foreach (var it in equipped) if (it != null && !it.IsEmptyMarker && SetOf(it) == set) n++;
            return n;
        }

        /// <summary>セットの名前（品の名前の頭にも使う）。</summary>
        public static string Name(int set)
        {
            switch ((BossKind)set)
            {
                case BossKind.Golem: return Loc.T("Ashwarden's", "灰守");
                case BossKind.Mother: return Loc.T("Mossmother's", "苔母");
                case BossKind.Forge: return Loc.T("Forgeheart", "炉心");
                case BossKind.Frost: return Loc.T("Frostcrown", "霜冠");
                case BossKind.Librarian: return Loc.T("Archivist's", "書守");
                case BossKind.Void: return Loc.T("Hollowshade", "虚影");
                default: return "";
            }
        }

        /// <summary>セットの色（枠・光の柱・名前・アイコンのもや）。</summary>
        public static Color ColorOf(int set)
        {
            switch ((BossKind)set)
            {
                case BossKind.Golem: return new Color(0.78f, 0.74f, 0.66f);  // 灰の石
                case BossKind.Mother: return new Color(0.45f, 0.9f, 0.4f);   // 苔の緑
                case BossKind.Forge: return new Color(1f, 0.5f, 0.15f);      // 溶けた鉄
                case BossKind.Frost: return new Color(0.55f, 0.88f, 1f);     // 氷
                case BossKind.Librarian: return new Color(0.95f, 0.8f, 0.4f); // 古い紙と金の文字
                case BossKind.Void: return new Color(0.7f, 0.45f, 1f);        // 虚ろの紫
                default: return RarityInfo.ColorOf(Rarity.Boss);
            }
        }

        /// <summary>そろえた数ごとの効果の説明。</summary>
        public static string BonusText(int set, int tier)
        {
            switch ((BossKind)set)
            {
                case BossKind.Golem:
                    return tier == 2 ? Loc.T("Armor +20%", "防御 +20%")
                         : tier == 3 ? Loc.T("Damage taken -10%", "受けるダメージ -10%")
                         : Loc.T("Every 6s, the ground quakes around you (300%)", "6秒ごとに、まわりの地面を揺らす（300%）");
                case BossKind.Mother:
                    return tier == 2 ? Loc.T("HP regen +1% of max HP/s", "HP回復 +最大HPの1%/秒")
                         : tier == 3 ? Loc.T("Heal 1% of damage dealt", "与えたダメージの1%を回復")
                         : Loc.T("Kills heal 2% of max HP", "倒すと最大HPの2%回復");
                case BossKind.Forge:
                    return tier == 2 ? Loc.T("Damage +15%", "ダメージ +15%")
                         : tier == 3 ? Loc.T("Crit damage +30%", "会心ダメージ +30%")
                         : Loc.T("Every 5s, breathe a cone of molten fire at the nearest enemy (300%)", "5秒ごとに、一番近い敵へ扇状に溶けた炎を吐く（300%）");
                case BossKind.Frost:
                    return tier == 2 ? Loc.T("Move speed +10%", "移動速度 +10%")
                         : tier == 3 ? Loc.T("Skill cooldown -10%", "スキルの待ち時間 -10%")
                         : Loc.T("Every 6s, a frost nova bursts around you (250%)", "6秒ごとに、まわりに霜の波が弾ける（250%）");
                case BossKind.Librarian:
                    return tier == 2 ? Loc.T("Skill damage +20%", "スキルのダメージ +20%")
                         : tier == 3 ? Loc.T("MP regen +3/s", "MP回復 +3/秒")
                         : Loc.T("Skill area +30%, skill damage +30%", "スキルの範囲 +30%、スキルのダメージ +30%");
                case BossKind.Void:
                    return tier == 2 ? Loc.T("Crit chance +8%", "会心率 +8%")
                         : tier == 3 ? Loc.T("EVA +40", "EVA +40")
                         : Loc.T("Dodging leaves a burst of hollow shadow (300%)", "回避した場所に、虚ろの影が弾ける（300%）");
                default: return "";
            }
        }
    }
}
