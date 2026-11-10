using UnityEngine;

namespace VividOfFade
{
    /// <summary>プレイヤー。移動・回避・通常攻撃（自動／押している間だけ）。能力は装備から計算した PlayerStats を使う。</summary>
    public sealed class Player : MonoBehaviour
    {
        public SpriteRenderer Sr;
        public int Hp { get; private set; }
        public int MaxHp { get; private set; } = GameConfig.PlayerMaxHp;
        public bool IsDead => Hp <= 0;
        public bool AutoAttack = true;
        public PlayerStats Stats { get; private set; }

        public float DodgeReady => 1f - Mathf.Clamp01(dodgeCooldown / GameConfig.DodgeCooldown);

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
        float fireTimer;

        public void ResetState(PlayerStats stats)
        {
            Stats = stats;
            MaxHp = stats.MaxHp;
            Hp = MaxHp;
            Pos = Vector2.zero;
            facing = Vector2.down;
            dashTimer = 0f;
            dodgeCooldown = 0f;
            invuln = 0f;
            hurtTimer = 0f;
            fireTimer = 0f;
            Sr.color = Color.white;
            Sr.sortingOrder = SortOrder.For(Vector2.zero);
        }

        /// <summary>装備が変わったとき。最大体力が増えた分は今の体力にも足す。</summary>
        public void ApplyStats(PlayerStats stats)
        {
            int gained = stats.MaxHp - MaxHp;
            Stats = stats;
            MaxHp = stats.MaxHp;
            Hp = Mathf.Clamp(Hp + Mathf.Max(0, gained), IsDead ? 0 : 1, MaxHp);
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
            float reduced = damage * 100f / (100f + Stats.Armor);
            Hp = Mathf.Max(0, Hp - Mathf.Max(1, Mathf.RoundToInt(reduced)));
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

            // 通常攻撃：明るさの範囲内の一番近い敵を狙う
            fireTimer -= dt;
            bool wantFire = AutoAttack || input.AttackHeld;
            if (!wantFire || fireTimer > 0f) return;

            var target = enemies.FindNearest(Pos, Stats.Light);
            Vector2 aim;
            if (target != null) aim = (target.Pos - Pos).normalized;
            else if (!AutoAttack) aim = Iso.ToLogical(facing).normalized; // 押している間は、敵がいなくても向いている方へ撃つ
            else return;

            FireVolley(aim, shots);
            fireTimer = Stats.FireInterval;
        }

        void FireVolley(Vector2 aim, ProjectileSystem shots)
        {
            int n = Stats.Shots;
            for (int k = 0; k < n; k++)
            {
                float angle = (k - (n - 1) * 0.5f) * Stats.SpreadDeg;
                Vector2 dir = Rotate(aim, angle);
                bool crit = Random.value < Stats.CritChance;
                float damage = Stats.Damage * (crit ? Stats.CritMult : 1f);
                shots.Fire(Pos + dir * 0.3f, dir, damage, crit, Stats.Pierce, Stats.Light);
            }
        }

        static Vector2 Rotate(Vector2 v, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(r);
            float s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
    }
}
