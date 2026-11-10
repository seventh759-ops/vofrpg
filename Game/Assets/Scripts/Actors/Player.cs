using UnityEngine;

namespace VividOfFade
{
    /// <summary>プレイヤー。移動・回避。通常攻撃は AttackSystem。能力は職業と装備から計算した PlayerStats を使う。</summary>
    public sealed class Player : MonoBehaviour
    {
        public SpriteRenderer Sr;
        public int Hp { get; private set; }
        public int MaxHp { get; private set; } = GameConfig.PlayerMaxHp;
        public int Mp => Mathf.FloorToInt(mp);
        public int MaxMp { get; private set; }
        public bool IsDead => Hp <= 0;
        public bool AutoAttack = true;
        public PlayerStats Stats { get; private set; }

        /// <summary>向いている方向（論理座標）。押している間だけ攻撃で、敵がいないときに使う。</summary>
        public Vector2 FacingLogical => Iso.ToLogical(facing).normalized;

        /// <summary>回避した（論理座標の位置）。ボス装備の固有効果に使う。</summary>
        public event System.Action<Vector2> Dodged;

        public float DodgeReady => 1f - Mathf.Clamp01(dodgeCooldown / (Stats != null ? Stats.DodgeCooldown : GameConfig.DodgeCooldown));

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

        // 入力は画面の向き（上を押すと画面の上へ）。移動するときに論理座標へ戻す
        Vector2 facing = Vector2.down;
        Vector2 dashDir;
        float dashTimer;
        float dodgeCooldown;
        float invuln;
        float hurtTimer;
        float mp;
        float regenHp; // 体力の自然回復の端数

        public void ResetState(PlayerStats stats)
        {
            Stats = stats;
            MaxHp = stats.MaxHp;
            Hp = MaxHp;
            MaxMp = stats.MaxMp;
            mp = MaxMp;
            regenHp = 0f;
            Pos = Vector2.zero;
            facing = Vector2.down;
            dashTimer = 0f;
            dodgeCooldown = 0f;
            invuln = 0f;
            hurtTimer = 0f;
            Sr.color = Color.white;
            Sr.sortingOrder = SortOrder.For(Vector2.zero);
        }

        /// <summary>装備が変わったとき。最大体力が増えた分は今の体力にも足す。</summary>
        public void ApplyStats(PlayerStats stats)
        {
            int gained = stats.MaxHp - MaxHp;
            int gainedMp = stats.MaxMp - MaxMp;
            Stats = stats;
            MaxHp = stats.MaxHp;
            Hp = Mathf.Clamp(Hp + Mathf.Max(0, gained), IsDead ? 0 : 1, MaxHp);
            MaxMp = stats.MaxMp;
            mp = Mathf.Clamp(mp + Mathf.Max(0, gainedMp), 0f, MaxMp);
        }

        /// <summary>レベルが上がったとき。体力と MP を全回復する。</summary>
        public void Refill()
        {
            if (IsDead) return;
            Hp = MaxHp;
            mp = MaxMp;
        }

        /// <summary>MP を使う。足りなければ false（スキルで使う）。</summary>
        public bool SpendMp(int amount)
        {
            if (mp < amount) return false;
            mp -= amount;
            return true;
        }

        public void Heal(int amount)
        {
            if (IsDead) return;
            Hp = Mathf.Min(MaxHp, Hp + amount);
        }

        /// <summary>ダメージを受けたら true。無敵中は受けない。防御で減らす。</summary>
        public bool TakeHit(int damage)
        {
            if (invuln > 0f || IsDead) return false;
            float reduced = damage * Stats.DamageTaken * 100f / (100f + Stats.Armor);
            Hp = Mathf.Max(0, Hp - Mathf.Max(1, Mathf.RoundToInt(reduced)));
            invuln = GameConfig.HitInvuln;
            hurtTimer = GameConfig.HitInvuln;
            return true;
        }

        public void Tick(float dt, PlayerInputReader input, Room room)
        {
            dodgeCooldown = Mathf.Max(0f, dodgeCooldown - dt);
            invuln = Mathf.Max(0f, invuln - dt);
            hurtTimer = Mathf.Max(0f, hurtTimer - dt);

            // 自然回復
            mp = Mathf.Min(MaxMp, mp + Stats.MpRegen * dt);
            regenHp += Stats.HpRegen * dt;
            if (regenHp >= 1f)
            {
                int whole = Mathf.FloorToInt(regenHp);
                regenHp -= whole;
                Heal(whole);
            }

            Vector2 move = input.Move;
            if (move.sqrMagnitude > 0.0001f) facing = move.normalized;

            if (input.ConsumeDodge(out var dodgeDir) && dodgeCooldown <= 0f && dashTimer <= 0f)
            {
                dashDir = dodgeDir.sqrMagnitude > 0.0001f ? dodgeDir.normalized : facing;
                dashTimer = GameConfig.DodgeTime;
                dodgeCooldown = Stats.DodgeCooldown;
                invuln = Mathf.Max(invuln, GameConfig.DodgeInvuln);
                Dodged?.Invoke(Pos);
            }

            bool dashing = dashTimer > 0f;
            Vector2 screenVelocity = dashing ? dashDir * GameConfig.DodgeSpeed : move * Stats.MoveSpeed;
            dashTimer = Mathf.Max(0f, dashTimer - dt);
            Pos = room.Clamp(Pos + Iso.ToLogical(screenVelocity) * dt, GameConfig.PlayerRadius);

            if (Mathf.Abs(screenVelocity.x) > 0.01f) Sr.flipX = screenVelocity.x < 0f;
            Sr.sortingOrder = SortOrder.For(Pos);
            if (dashing)
            {
                Sr.color = new Color(0.7f, 0.85f, 1f, 0.8f);
            }
            else if (hurtTimer > 0f)
            {
                Sr.color = Mathf.Repeat(Time.time * 20f, 1f) < 0.5f ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
            }
            else
            {
                Sr.color = Color.white;
            }
        }

        /// <summary>職業の見た目に切り替える。</summary>
        public void SetClass(ClassType type)
        {
            Sr.sprite = PixelArt.PlayerFor(type);
        }
    }
}
