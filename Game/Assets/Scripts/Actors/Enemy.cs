using UnityEngine;

namespace VividOfFade
{
    /// <summary>敵1体の状態。動きは EnemySystem がまとめて処理する。</summary>
    public sealed class Enemy : MonoBehaviour
    {
        public SpriteRenderer Sr;
        public float Hp;
        public int ContactDamage;
        public float Speed;
        public float Radius;
        public Vector2 Knock;
        public float FlashTimer;
        public bool Dying;
        public float DyingTimer;

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
