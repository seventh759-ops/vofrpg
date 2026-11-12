using UnityEngine;

namespace VividOfFade
{
    /// <summary>精鋭の種類。None は普通の敵。</summary>
    public enum EliteKind { None, Swift, Hardened, Splitter, Shroud }

    /// <summary>
    /// 敵の種類。スライム：まっすぐ追う／コウモリ：速く、ふらふら飛ぶ／術師：距離を取って弾を撃つ／
    /// 岩鬼：力をためてから突進する／爆弾虫：近づくと導火線に火がつき、少しして爆発する。
    /// </summary>
    public enum EnemyKind { Slime, Bat, Caster, Brute, Bomber }

    /// <summary>敵1体の状態。動きは EnemySystem がまとめて処理する。</summary>
    public sealed class Enemy : MonoBehaviour
    {
        public SpriteRenderer Sr;
        public SpriteRenderer Aura;   // 精鋭・ボスの足元の光る輪
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

        // 種類ごとの動き
        public EnemyKind Kind;
        public float ActTimer;     // 次の行動（弾・突進）まで
        public float Prep;         // 突進の前の溜め／導火線の残り（0 より大きい間は止まる）
        public float Charging;     // 突進している残り時間
        public Vector2 ChargeDir;
        public float Phase;        // ふらふら飛ぶ揺れのずれ

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
