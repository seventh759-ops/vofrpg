using UnityEngine;

namespace VividOfFade
{
    /// <summary>プレイヤー。移動・回避・通常攻撃（自動／押している間だけ）。</summary>
    public sealed class Player : MonoBehaviour
    {
        public SpriteRenderer Sr;
        public int Hp { get; private set; }
        public bool IsDead => Hp <= 0;
        public bool AutoAttack = true;

        public float DodgeReady => 1f - Mathf.Clamp01(dodgeCooldown / GameConfig.DodgeCooldown);

        public Vector2 Pos
        {
            get => transform.position;
            set => transform.position = value;
        }

        Vector2 facing = Vector2.down;
        Vector2 dashDir;
        float dashTimer;
        float dodgeCooldown;
        float invuln;
        float hurtTimer;
        float fireTimer;

        public void ResetState()
        {
            Hp = GameConfig.PlayerMaxHp;
            Pos = Vector2.zero;
            facing = Vector2.down;
            dashTimer = 0f;
            dodgeCooldown = 0f;
            invuln = 0f;
            hurtTimer = 0f;
            fireTimer = 0f;
            Sr.color = Color.white;
            Sr.sortingOrder = SortOrder.ForY(0f);
        }

        /// <summary>ダメージを受けたら true。無敵中は受けない。</summary>
        public bool TakeHit(int damage)
        {
            if (invuln > 0f || IsDead) return false;
            Hp = Mathf.Max(0, Hp - damage);
            invuln = GameConfig.HitInvuln;
            hurtTimer = GameConfig.HitInvuln;
            return true;
        }

        public void Tick(float dt, PlayerInputReader input, Room room, EnemySystem enemies, ProjectileSystem shots)
        {
            dodgeCooldown = Mathf.Max(0f, dodgeCooldown - dt);
            invuln = Mathf.Max(0f, invuln - dt);
            hurtTimer = Mathf.Max(0f, hurtTimer - dt);

            Vector2 move = input.Move;
            if (move.sqrMagnitude > 0.0001f) facing = move.normalized;

            if (input.ConsumeDodge(out var dodgeDir) && dodgeCooldown <= 0f && dashTimer <= 0f)
            {
                dashDir = dodgeDir.sqrMagnitude > 0.0001f ? dodgeDir.normalized : facing;
                dashTimer = GameConfig.DodgeTime;
                dodgeCooldown = GameConfig.DodgeCooldown;
                invuln = Mathf.Max(invuln, GameConfig.DodgeInvuln);
            }

            bool dashing = dashTimer > 0f;
            Vector2 velocity = dashing ? dashDir * GameConfig.DodgeSpeed : move * GameConfig.PlayerSpeed;
            dashTimer = Mathf.Max(0f, dashTimer - dt);
            Pos = room.Clamp(Pos + velocity * dt, GameConfig.PlayerRadius);

            if (Mathf.Abs(velocity.x) > 0.01f) Sr.flipX = velocity.x < 0f;
            Sr.sortingOrder = SortOrder.ForY(Pos.y);
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

            // 通常攻撃
            fireTimer -= dt;
            bool wantFire = AutoAttack || input.AttackHeld;
            if (!wantFire || fireTimer > 0f) return;

            var target = enemies.FindNearest(Pos, GameConfig.AttackRange);
            if (target != null)
            {
                Vector2 dir = (target.Pos - Pos).normalized;
                shots.Fire(Pos + dir * 0.3f, dir);
                fireTimer = GameConfig.FireInterval;
            }
            else if (!AutoAttack)
            {
                // 押している間は、敵がいなくても向いている方へ撃つ
                shots.Fire(Pos + facing * 0.3f, facing);
                fireTimer = GameConfig.FireInterval;
            }
        }
    }
}
