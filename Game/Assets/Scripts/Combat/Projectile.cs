using UnityEngine;

namespace VividOfFade
{
    /// <summary>ランタンから飛ばす火の玉1つの状態。動きは ProjectileSystem がまとめて処理する。</summary>
    public sealed class Projectile : MonoBehaviour
    {
        public SpriteRenderer Sr;
        public Vector2 Velocity;
        public float Life;
        public float Damage;
        public bool Crit;
        public int PierceLeft;
        public Enemy LastHit;
        public float Spin;   // 回る速さ（度/秒。スキルの星など）
        public float Angle;  // 今の向き（度）
        public bool Homing;  // 狙った敵を追いかける（倒れたら近くの別の敵へ）
        public Enemy Target;
        public float AngleOffset; // 追いかけて向きが変わるとき、絵の向きのずれ（度）。AlignToDir でなければ NaN

        Vector2 pos;

        /// <summary>論理座標。画面にはクォータービューに変換して描く。</summary>
        public Vector2 Pos
        {
            get => pos;
            set
            {
                pos = value;
                transform.position = Iso.ToScreen(value);
            }
        }
    }
}
