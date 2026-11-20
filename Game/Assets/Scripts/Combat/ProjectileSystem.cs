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

        /// <summary>弾の見た目（スキルごと）。sprite が null ならふつうの火の玉。</summary>
        public struct Look
        {
            public Sprite Sprite;
            public Color Tint;
            public float AngleOffset; // 進む向きに合わせるときの、絵の向きのずれ（度）
            public bool AlignToDir;   // 進む向きに回す（三日月・炎の尾）
            public float Spin;        // 回り続ける（星）
            public float Scale;
        }

        static readonly Color[] VividShotColors =
        {
            new Color(0.45f, 0.9f, 1f), new Color(0.75f, 0.6f, 1f), new Color(1f, 0.5f, 0.9f), new Color(1f, 0.7f, 0.35f), new Color(1f, 0.92f, 0.45f), new Color(0.55f, 1f, 0.65f),
        };
        int vividShot;

        public void Fire(Vector2 pos, Vector2 dir, float damage, bool crit, int pierce, float range) => Fire(pos, dir, damage, crit, pierce, range, default);

        public void Fire(Vector2 pos, Vector2 dir, float damage, bool crit, int pierce, float range, Look look)
        {
            if (look.Sprite == null && ClassInfo.Current == ClassType.Vividbearer)
            {
                // 彩灯者の通常攻撃：色とりどりの結晶（1発ごとに色が変わる）
                look = new Look { Sprite = PixelArt.Crystal, Tint = VividShotColors[vividShot++ % VividShotColors.Length], AlignToDir = true, Scale = 0.55f };
            }
            var p = pool.Get();
            p.Sr.sprite = look.Sprite != null ? look.Sprite : PixelArt.Fireball;
            p.Sr.color = look.Sprite != null ? look.Tint : Color.white;
            p.Spin = look.Spin;
            float ang = 0f;
            if (look.AlignToDir)
            {
                Vector2 sd = Iso.ToScreen(dir);
                ang = Mathf.Atan2(sd.y, sd.x) * Mathf.Rad2Deg + look.AngleOffset;
            }
            p.Angle = ang;
            p.transform.localRotation = Quaternion.Euler(0f, 0f, ang);
            p.Pos = pos;
            p.Velocity = dir * GameConfig.ShotSpeed;
            // 明るさの少し先まで飛んで消える
            p.Life = Mathf.Min(GameConfig.ShotLife, (range + 1f) / GameConfig.ShotSpeed);
            p.Damage = damage;
            p.Crit = crit;
            p.PierceLeft = pierce;
            float sc = (look.Scale > 0f ? look.Scale : 1f) * (crit ? 1.5f : 1f);
            p.transform.localScale = new Vector3(sc, sc, 1f);
            p.LastHit = null;
            p.Homing = false;
            p.Target = null;
            p.AngleOffset = look.AlignToDir ? look.AngleOffset : float.NaN;
            p.Sr.sortingOrder = SortOrder.For(pos) + 1;
            active.Add(p);
        }

        /// <summary>狙った敵を追いかける弾（スキルの矢）。狙った敵が倒れたら、近くの別の敵へ向きを変える。</summary>
        public void FireHoming(Vector2 pos, Vector2 dir, float damage, bool crit, Enemy target, Look look)
        {
            Fire(pos, dir, damage, crit, 0, 99f, look);
            var p = active[active.Count - 1];
            p.Homing = true;
            p.Target = target;
            p.Velocity = dir * GameConfig.ShotSpeed * 0.85f;
            p.Life = HomingLife;
        }

        const float HomingLife = 2.5f;     // 追いかける弾が消えるまで（秒）
        const float HomingTurn = 10f;      // 向きを変える速さ
        const float HomingSearch = 8f;     // 次の敵を探す広さ（マス）

        public void Tick(float dt, EnemySystem enemies, Room room)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var p = active[i];
                p.Life -= dt;
                if (p.Homing)
                {
                    if (p.Target == null || p.Target.Dying || !p.Target.gameObject.activeSelf) p.Target = enemies.FindNearest(p.Pos, HomingSearch);
                    if (p.Target != null)
                    {
                        Vector2 want = p.Target.Pos - p.Pos;
                        if (want.sqrMagnitude > 0.0001f)
                        {
                            float speed = p.Velocity.magnitude;
                            Vector2 dir = Vector2.Lerp(p.Velocity / Mathf.Max(0.001f, speed), want.normalized, Mathf.Clamp01(HomingTurn * dt)).normalized;
                            p.Velocity = dir * speed;
                            if (!float.IsNaN(p.AngleOffset))
                            {
                                Vector2 sd = Iso.ToScreen(dir);
                                p.Angle = Mathf.Atan2(sd.y, sd.x) * Mathf.Rad2Deg + p.AngleOffset;
                                p.transform.localRotation = Quaternion.Euler(0f, 0f, p.Angle);
                            }
                        }
                    }
                }
                Vector2 pos = p.Pos + p.Velocity * dt;
                p.Pos = pos;
                p.Sr.sortingOrder = SortOrder.For(pos) + 1;
                if (p.Spin != 0f) { p.Angle += p.Spin * dt; p.transform.localRotation = Quaternion.Euler(0f, 0f, p.Angle); }

                if (p.Life <= 0f || !room.Contains(pos))
                {
                    ReleaseAt(i);
                    continue;
                }

                bool spent = false;
                foreach (var e in enemies.Active)
                {
                    if (e.Dying || e.Dormant || e == p.LastHit) continue; // 眠っている敵はすり抜ける
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
