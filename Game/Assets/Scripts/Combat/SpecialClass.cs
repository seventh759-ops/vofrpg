using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 特別な職「彩灯者」（どの基本職でも共通の1つ）。
    /// 基本職のまま（転職せずに）Lv99 まで育てて転生すると、基本職に戻らずに、この職の Lv1 になる。
    /// 強さは3次職と同じくらい（補正＋7つ目（Lv50 で覚える）・8つ目のスキル、経験値は1.2倍）。ここから先の転職はなく、Lv99 でまた転生しても彩灯者のまま（一度なったら、ずっと彩灯者）。
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
            // 8つ目（彩灯者になったときに覚える）：自分の半分の強さの分身を呼び出す。分身は同じ武器で攻撃し、付けているスキルも使う
            Skill = new SkillSpec
            {
                Name = "Prism Twin", NameJa = "彩の分身", Desc = "Summon a twin with half your strength (x power) for 60s. It attacks with your weapon and uses your skills. Costs half your max MP.",
                DescJa = "60秒、自分の半分×威力の強さの分身を呼ぶ。同じ武器で攻撃し、スキルも使う。最大MPの半分を使う。",
                ReqLevel = 1, MpPctOfMax = 0.5f, Cooldown = 90f, Effect = SkillEffect.Summon, Duration = 60f, Range = 8f,
                Color = new Color(1f, 0.55f, 0.9f, 0.9f),
            },
        };

        /// <summary>7つ目（Lv50 で覚える）：自分のまわりのあちこちに、虹の柱が7本ばらばらに降る。</summary>
        public static readonly SkillSpec SeventhSkill = new SkillSpec
        {
            Name = "Rainbow Rain", NameJa = "虹の雨", Desc = "7 rainbow pillars rain down at random around you.", DescJa = "自分のまわりのあちこちに、虹の柱が7本降る。",
            ReqLevel = 50, Mp = 70, Cooldown = 9f, Effect = SkillEffect.Strike, Mult = 9f, Radius = 1.6f, Delay = 0.5f, Range = 4.5f, Count = 7,
            Scatter = true, Radius2 = 4.5f, Color = new Color(0.6f, 0.85f, 1f, 0.9f),
        };

        /// <summary>分身の強さ（自分に対する割合）。</summary>
        public const float TwinPower = 0.5f;

        /// <summary>見た目の光の縁の色（タイトルの「VIVID」の桃色）。</summary>
        public static readonly Color32 AuraColor = new Color32(255, 90, 215, 255);

        /// <summary>そのキャラクターが彩灯者なら、その職（でなければ null）。</summary>
        public static AdvancedSpec Of(CharacterData ch) => ch != null && ch.Special ? Spec : null;

        /// <summary>転生したら彩灯者になるか（基本職のまま、まだ彩灯者でない）。</summary>
        public static bool RebirthBecomesSpecial(CharacterData ch) => ch != null && ch.Advanced < 0 && ch.ClassId < ClassInfo.PlayableCount;
    }
}
