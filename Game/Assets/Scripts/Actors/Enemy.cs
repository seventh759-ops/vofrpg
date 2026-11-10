using UnityEngine;

namespace VividOfFade
{
    /// <summary>精鋭の種類。None は普通の敵。</summary>
    public enum EliteKind { None, Swift, Hardened, Splitter, Shroud }

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

        // ボス
        public bool IsBoss;
        public float MaxHp;
        public float BaseScale = 1f;
        public float SlamTimer;   // 次に叩きつけるまで
        public float Windup;      // 力をためている残り時間（0 より大きい間は動かない）
        public bool Enraged;      // 体力が半分を切って怒った

        // 精鋭
        public EliteKind Elite;
        public Color Tint = Color.white;

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
