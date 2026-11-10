using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 職業ごとの通常攻撃。
    /// 灯術師：火の玉／影刃：前方を扇形に斬る／灯番：体の周りを回る灯り、または周りを一度に焼く。
    /// </summary>
    public sealed class AttackSystem : MonoBehaviour
    {
        const float OrbHitRadius = 0.45f;

        EnemySystem enemies;
        ProjectileSystem shots;
        FxSystem fx;

        readonly List<SpriteRenderer> orbs = new List<SpriteRenderer>();
        readonly List<Enemy> slashHits = new List<Enemy>();
        int activeOrbs;
        float timer;
        float orbAngle;

        public void Init(EnemySystem enemies, ProjectileSystem shots, FxSystem fx)
        {
            this.enemies = enemies;
            this.shots = shots;
            this.fx = fx;
        }

        public void ResetState()
        {
            timer = 0f;
            orbAngle = 0f;
            SetOrbCount(0);
        }

        public void Tick(float dt, Player player, bool wantFire)
        {
            var st = player.Stats;
            timer -= dt;

            if (st.Kind == AttackKind.Orbit) UpdateOrbs(dt, player, st, wantFire);
            else SetOrbCount(0);

            if (!wantFire || timer > 0f) return;

            switch (st.Kind)
            {
                case AttackKind.Projectile:
                    if (TryAim(player, st.Light, out var aim)) FireVolley(player, st, aim);
                    else return;
                    break;
                case AttackKind.Slash:
                    if (TryAim(player, st.Reach + GameConfig.SlimeRadius, out var dir)) Slash(player, st, dir);
                    else return;
                    break;
                case AttackKind.Pulse:
                    // 自動のときは、範囲に敵がいるときだけ焼く
                    if (player.AutoAttack && enemies.FindNearest(player.Pos, st.PulseRadius) == null) return;
                    Pulse(player, st);
                    break;
                default:
                    return; // 回る灯りは UpdateOrbs で当てる
            }
            // ボス装備の固有効果：一定の確率ですぐにもう一度攻撃する
            timer = Random.value < st.DoubleStrikeChance ? 0.05f : st.FireInterval;
        }

        /// <summary>範囲内の一番近い敵へ向ける。押している間だけのときは、敵がいなくても向いている方へ。</summary>
        bool TryAim(Player player, float range, out Vector2 aim)
        {
            var target = enemies.FindNearest(player.Pos, range);
            if (target != null)
            {
                aim = (target.Pos - player.Pos).normalized;
                return true;
            }
            aim = player.FacingLogical;
            return !player.AutoAttack;
        }

        void FireVolley(Player player, PlayerStats st, Vector2 aim)
        {
            int n = st.Shots;
            for (int k = 0; k < n; k++)
            {
                float angle = (k - (n - 1) * 0.5f) * st.SpreadDeg;
                Vector2 dir = Rotate(aim, angle);
                bool crit = Random.value < st.CritChance;
                shots.Fire(player.Pos + dir * 0.3f, dir, st.Damage * (crit ? st.CritMult : 1f), crit, st.Pierce, st.Light);
            }
        }

        /// <summary>前方の扇形の中の敵を、近い順に MaxTargets 体まで斬る。2体目以降は威力が下がる。</summary>
        void Slash(Player player, PlayerStats st, Vector2 aim)
        {
            Vector2 origin = player.Pos;
            float cosHalf = Mathf.Cos(st.ArcDeg * 0.5f * Mathf.Deg2Rad);
            var list = enemies.Active;
            slashHits.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e.Dying) continue;
                Vector2 d = e.Pos - origin;
                float dist = d.magnitude;
                if (dist > st.Reach + e.Radius) continue;
                if (dist > 0.2f && Vector2.Dot(d / dist, aim) < cosHalf) continue;
                slashHits.Add(e);
            }
            slashHits.Sort((a, b) => (a.Pos - origin).sqrMagnitude.CompareTo((b.Pos - origin).sqrMagnitude));

            int count = Mathf.Min(slashHits.Count, Mathf.Max(1, st.MaxTargets));
            for (int i = 0; i < count; i++)
            {
                var e = slashHits[i];
                Vector2 d = e.Pos - origin;
                float dist = d.magnitude;
                bool crit = Random.value < st.CritChance;
                float scale = i == 0 ? 1f : PlayerStats.SecondaryHit;
                enemies.Damage(e, st.Damage * scale * (crit ? st.CritMult : 1f), dist > 0.001f ? d / dist : aim, crit);
            }
            fx.Slash(origin, aim, st.Reach, st.ArcDeg, new Color(0.85f, 0.9f, 1f, 0.9f));
        }

        void Pulse(Player player, PlayerStats st)
        {
            Vector2 origin = player.Pos;
            float r2 = st.PulseRadius * st.PulseRadius;
            var list = enemies.Active;
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e.Dying) continue;
                Vector2 d = e.Pos - origin;
                if (d.sqrMagnitude > r2) continue;
                bool crit = Random.value < st.CritChance;
                enemies.Damage(e, st.Damage * (crit ? st.CritMult : 1f), d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.up, crit);
            }
            fx.Ring(origin, st.PulseRadius, new Color(1f, 0.75f, 0.3f, 0.8f), 0.3f);
        }

        void UpdateOrbs(float dt, Player player, PlayerStats st, bool wantFire)
        {
            SetOrbCount(st.Orbs);
            orbAngle += st.OrbitSpeed * dt;

            bool hitNow = wantFire && timer <= 0f;
            for (int k = 0; k < activeOrbs; k++)
            {
                float a = orbAngle + k * Mathf.PI * 2f / activeOrbs;
                Vector2 p = player.Pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * st.OrbitRadius;
                var sr = orbs[k];
                sr.transform.position = Iso.ToScreen(p) + new Vector2(0f, 0.25f);
                sr.sortingOrder = SortOrder.For(p) + 1;
                sr.color = wantFire ? Color.white : new Color(1f, 1f, 1f, 0.35f);

                if (hitNow) HitAround(p, st);
            }
            if (hitNow) timer = st.FireInterval;
        }

        void HitAround(Vector2 p, PlayerStats st)
        {
            var list = enemies.Active;
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e.Dying) continue;
                Vector2 d = e.Pos - p;
                float r = OrbHitRadius + e.Radius;
                if (d.sqrMagnitude > r * r) continue;
                bool crit = Random.value < st.CritChance;
                enemies.Damage(e, st.Damage * (crit ? st.CritMult : 1f), d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.up, crit);
            }
        }

        void SetOrbCount(int n)
        {
            if (n == activeOrbs) return;
            while (orbs.Count < n)
            {
                var go = new GameObject("Orb");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = PixelArt.Fireball;
                go.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
                orbs.Add(sr);
            }
            for (int i = 0; i < orbs.Count; i++) orbs[i].gameObject.SetActive(i < n);
            activeOrbs = n;
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
