using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>火の玉をまとめて動かし、敵との当たりを調べる。</summary>
    public sealed class ProjectileSystem : MonoBehaviour
    {
        readonly List<Projectile> active = new List<Projectile>();
        Pool<Projectile> pool;

        public void Init()
        {
            pool = new Pool<Projectile>(Create);
        }

        Projectile Create()
        {
            var go = new GameObject("Fireball");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PixelArt.Fireball;
            Lighting.MakeGlow(sr);
            var p = go.AddComponent<Projectile>();
            p.Sr = sr;
            go.SetActive(false);
            return p;
        }

        public void Fire(Vector2 pos, Vector2 dir, float damage, bool crit, int pierce, float range)
        {
            var p = pool.Get();
            p.Pos = pos;
            p.Velocity = dir * GameConfig.ShotSpeed;
            // 明るさの少し先まで飛んで消える
            p.Life = Mathf.Min(GameConfig.ShotLife, (range + 1f) / GameConfig.ShotSpeed);
            p.Damage = damage;
            p.Crit = crit;
            p.PierceLeft = pierce;
            p.transform.localScale = crit ? new Vector3(1.5f, 1.5f, 1f) : Vector3.one;
            p.LastHit = null;
            p.Sr.sortingOrder = SortOrder.For(pos) + 1;
            active.Add(p);
        }

        public void Tick(float dt, EnemySystem enemies, Room room)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var p = active[i];
                p.Life -= dt;
                Vector2 pos = p.Pos + p.Velocity * dt;
                p.Pos = pos;
                p.Sr.sortingOrder = SortOrder.For(pos) + 1;

                if (p.Life <= 0f || !room.Contains(pos))
                {
                    ReleaseAt(i);
                    continue;
                }

                bool spent = false;
                foreach (var e in enemies.Active)
                {
                    if (e.Dying || e == p.LastHit) continue;
                    float r = GameConfig.ShotRadius + e.Radius;
                    if ((e.Pos - pos).sqrMagnitude > r * r) continue;

                    enemies.Damage(e, p.Damage, p.Velocity.normalized, p.Crit);
                    p.LastHit = e;
                    if (p.PierceLeft <= 0)
                    {
                        spent = true;
                        break;
                    }
                    p.PierceLeft--;
                }
                if (spent) ReleaseAt(i);
            }
        }

        public void Clear()
        {
            for (int i = active.Count - 1; i >= 0; i--) ReleaseAt(i);
        }

        void ReleaseAt(int i)
        {
            var p = active[i];
            int last = active.Count - 1;
            active[i] = active[last];
            active.RemoveAt(last);
            pool.Release(p);
        }
    }
}
