using UnityEngine;

namespace VividOfFade
{
    /// <summary>敵1体の状態。動きは EnemySystem がまとめて処理する。</summary>
    public sealed class Enemy : MonoBehaviour
    {
        public SpriteRenderer Sr;
        public int Hp;
        public int ContactDamage;
        public float Speed;
        public float Radius;
        public Vector2 Knock;
        public float FlashTimer;
        public bool Dying;
        public float DyingTimer;

        public Vector2 Pos
        {
            get => transform.position;
            set => transform.position = value;
        }
    }
}
