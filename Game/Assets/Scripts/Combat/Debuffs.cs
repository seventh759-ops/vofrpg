namespace VividOfFade
{
    /// <summary>
    /// プレイヤーにかかる弱体効果（主にボスの攻撃から）。気絶：動けず、攻撃もスキルも出ない／鈍足：移動が半分／
    /// 攻撃力ダウン：与えるダメージ -25%／防御ダウン：防御 -40%／
    /// 暗闇：明るさ -3.5、命中・回避 -30%（覆いの精鋭の近くにいる間）。
    /// </summary>
    public enum DebuffKind { Stun, Slow, AttackDown, DefenseDown, Blind }

    public static class Debuffs
    {
        public const float SlowMult = 0.5f;
        public const float AttackDownMult = 0.75f;
        public const float DefenseDownMult = 0.6f;
        public const float BlindLight = 3.5f;     // 暗闇で下がる明るさ
        public const float BlindHitEvaMult = 0.7f; // 暗闇の命中・回避の倍率

        public static string Name(DebuffKind k)
        {
            switch (k)
            {
                case DebuffKind.Stun: return Loc.T("Stunned", "気絶");
                case DebuffKind.Slow: return Loc.T("Slowed", "鈍足");
                case DebuffKind.AttackDown: return Loc.T("Attack down", "攻撃力ダウン");
                case DebuffKind.Blind: return Loc.T("Blinded", "暗闇");
                default: return Loc.T("Defense down", "防御ダウン");
            }
        }
    }

    public sealed partial class PlayerStats
    {
        /// <summary>弱体効果を足した能力を作る。元の能力は変えない。</summary>
        public PlayerStats WithDebuff(DebuffKind k)
        {
            var s = (PlayerStats)MemberwiseClone();
            switch (k)
            {
                case DebuffKind.Slow: s.MoveSpeed *= Debuffs.SlowMult; break;
                case DebuffKind.AttackDown: s.Damage *= Debuffs.AttackDownMult; s.SkillBase *= Debuffs.AttackDownMult; break;
                case DebuffKind.DefenseDown: s.Armor *= Debuffs.DefenseDownMult; break;
                case DebuffKind.Blind:
                    s.Hit = UnityEngine.Mathf.RoundToInt(s.Hit * Debuffs.BlindHitEvaMult);
                    s.Eva = UnityEngine.Mathf.RoundToInt(s.Eva * Debuffs.BlindHitEvaMult);
                    break;
            }
            return s;
        }
    }
}
