using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 特別な職「彩灯者」（どの基本職でも共通の1つ）。
    /// 基本職のまま（転職せずに）Lv99 まで育てて転生すると、基本職に戻らずに、この職の Lv1 になる。
    /// 強さは3次職と同じくらい（補正＋8つ目のスキル、経験値は1.2倍）。ここから先の転職はなく、Lv99 でまた転生すると基本職に戻る。
    /// どの職業の武器も使える（落ちる武器も全職業のもの）。遠近両用：武器の攻撃に加えて、近くの敵は斬り、遠くの敵には弾を撃つ。
    /// 見た目は、もとの職業の絵を白と鮮やかな色に塗り替え、桃色の光の縁を付ける。
    /// タイトルの「VIVID」にちなんだ、このゲーム用のオリジナルの名前。数値は仮で、遊んでから調整する。
    /// </summary>
    public static class SpecialClass
    {
        public static readonly AdvancedSpec Spec = new AdvancedSpec
        {
            Name = "Vividbearer", NameJa = "彩灯者",
            Desc = "Uses every class's weapons. Attacks at any range: cuts nearby enemies and shoots distant ones. Damage +35%, attack speed +15%, crit +8%, max HP +25%, light +2, +1 projectile.",
            DescJa = "どの職業の武器も使える。遠近両用：近くの敵は斬り、遠くの敵には弾を撃つ。ダメージ+35%、攻撃速度+15%、会心率+8%、最大HP+25%、明るさ+2、弾+1。",
            Passive = new BoonSpec { Damage = 0.35f, AttackSpeed = 0.15f, Crit = 0.08f, HpPct = 0.25f, Light = 2f, Extra = 1 },
            Skill = new SkillSpec
            {
                Name = "Prism Burst", NameJa = "彩光", Desc = "A burst of vivid light around you; heals 10%.", DescJa = "周りに鮮やかな光を放つ。HP10%回復。",
                ReqLevel = 1, Mp = 50, Cooldown = 11f, Effect = SkillEffect.Nova, Mult = 16f, Radius = 4.5f, HealPct = 0.1f,
                Color = new Color(1f, 0.4f, 0.85f, 0.9f),
            },
        };

        /// <summary>見た目の光の縁の色（タイトルの「VIVID」の桃色）。</summary>
        public static readonly Color32 AuraColor = new Color32(255, 90, 215, 255);

        /// <summary>そのキャラクターが彩灯者なら、その職（でなければ null）。</summary>
        public static AdvancedSpec Of(CharacterData ch) => ch != null && ch.Special ? Spec : null;

        /// <summary>転生したら彩灯者になるか（基本職のまま、まだ彩灯者でない）。</summary>
        public static bool RebirthBecomesSpecial(CharacterData ch) => ch != null && ch.Advanced < 0 && ch.ClassId < ClassInfo.PlayableCount;
    }
}
