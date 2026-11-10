using UnityEngine;

namespace VividOfFade
{
    /// <summary>ランタンから飛ばす火の玉1つの状態。動きは ProjectileSystem がまとめて処理する。</summary>
    public sealed class Projectile : MonoBehaviour
    {
        public SpriteRenderer Sr;
        public Vector2 Velocity;
        public float Life;
        public int Damage;
        public int PierceLeft;
        public Enemy LastHit;

        public Vector2 Pos
        {
            get => transform.position;
            set => transform.position = value;
        }
    }
}
