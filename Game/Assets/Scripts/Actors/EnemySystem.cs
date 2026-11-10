using System;
using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 敵の群れをまとめて動かす。1体ずつ Update を持たせず、ここで一括処理する。
    /// 当たり判定は物理エンジンを使わず、円同士の距離で計算する。
    /// </summary>
    public sealed class EnemySystem : MonoBehaviour
    {
        const float DeathTime = 0.15f;
        static readonly Color HitColor = new Color(1f, 0.55f, 0.55f, 1f);

        public readonly List<Enemy> Active = new List<Enemy>();
        public int Kills { get; set; }

        /// <summary>ダメージを与えた（位置・量・会心か）。数字の表示と音に使う。</summary>
        public event Action<Vector2, float, bool> Damaged;
        /// <summary>倒した（位置）。ドロップや撃破時の効果に使う。</summary>
        public event Action<Vector2> Killed;

        Pool<Enemy> pool;

        public int AliveCount
        {
            get
            {
                int n = 0;
                foreach (var e in Active)
                {
                    if (!e.Dying) n++;
                }
                return n;
            }
        }

        public void Init()
        {
            pool = new Pool<Enemy>(Create);
        }

        Enemy Create()
        {
            var go = new GameObject("Slime");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PixelArt.Slime;
            var e = go.AddComponent<Enemy>();
            e.Sr = sr;
            go.SetActive(false);
            return e;
        }

        public void Spawn(Vector2 pos, float hp)
        {
            if (Active.Count >= GameConfig.MaxEnemies) return;
            var e = pool.Get();
            e.Hp = hp;
            e.ContactDamage = GameConfig.SlimeContactDamage;
            e.Speed = UnityEngine.Random.Range(GameConfig.SlimeSpeedMin, GameConfig.SlimeSpeedMax);
            e.Radius = GameConfig.SlimeRadius;
            e.Knock = Vector2.zero;
            e.FlashTimer = 0f;
            e.Dying = false;
            e.DyingTimer = 0f;
            e.Sr.color = Color.white;
            e.transform.localScale = Vector3.one;
            e.Pos = pos;
            e.Sr.sortingOrder = SortOrder.For(pos);
            Active.Add(e);
        }

        public void Damage(Enemy e, float amount, Vector2 dir, bool crit)
        {
            if (e.Dying) return;
            e.Hp -= amount;
            e.FlashTimer = 0.08f;
            e.Sr.color = HitColor;
            e.Knock += dir * (crit ? 5f : 3f);
            Damaged?.Invoke(e.Pos, amount, crit);
            if (e.Hp <= 0f)
            {
                e.Dying = true;
                e.DyingTimer = DeathTime;
                Kills++;
                Killed?.Invoke(e.Pos);
            }
        }

        /// <summary>範囲内の敵すべてにダメージ（撃破時の爆発など）。</summary>
        public void DamageArea(Vector2 center, float radius, float amount)
        {
            float r2 = radius * radius;
            // 連鎖で倒れても一覧の並びは変わらない（消えるのは Tick の中だけ）
            for (int i = 0; i < Active.Count; i++)
            {
                var e = Active[i];
                if (e.Dying) continue;
                Vector2 d = e.Pos - center;
                if (d.sqrMagnitude > r2) continue;
                Damage(e, amount, d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.up, false);
            }
        }

        public Enemy FindNearest(Vector2 from, float range)
        {
            Enemy best = null;
            float bestSq = range * range;
            foreach (var e in Active)
            {
                if (e.Dying) continue;
                float sq = (e.Pos - from).sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = e;
                }
            }
            return best;
        }

        public void Tick(float dt, Player player, Room room)
        {
            Vector2 target = player.Pos;
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                var e = Active[i];

                if (e.Dying)
                {
                    // 倒れたら少し膨らんで消える
                    e.DyingTimer -= dt;
                    float t = 1f - Mathf.Clamp01(e.DyingTimer / DeathTime);
                    e.transform.localScale = Vector3.one * (1f + t * 0.6f);
                    var c = e.Sr.color;
                    c.a = 1f - t;
                    e.Sr.color = c;
                    if (e.DyingTimer <= 0f) ReleaseAt(i);
                    continue;
                }

                if (e.FlashTimer > 0f)
                {
                    e.FlashTimer -= dt;
                    if (e.FlashTimer <= 0f) e.Sr.color = Color.white;
                }

                Vector2 pos = e.Pos;
                Vector2 to = target - pos;
                float dist = to.magnitude;
                Vector2 dir = dist > 0.001f ? to / dist : Vector2.zero;

                // 重なりすぎないように押し合う
                Vector2 separation = Vector2.zero;
                for (int j = 0; j < Active.Count; j++)
                {
                    if (j == i) continue;
                    var o = Active[j];
                    if (o.Dying) continue;
                    Vector2 diff = pos - o.Pos;
                    float min = e.Radius + o.Radius;
                    float sq = diff.sqrMagnitude;
                    if (sq < min * min && sq > 0.0001f)
                    {
                        float d = Mathf.Sqrt(sq);
                        separation += diff / d * (min - d);
                    }
                }

                pos += (dir * e.Speed + e.Knock) * dt + separation * 0.5f;
                e.Knock = Vector2.MoveTowards(e.Knock, Vector2.zero, 20f * dt);
                pos = room.Clamp(pos, e.Radius);
                e.Pos = pos;
                e.Sr.sortingOrder = SortOrder.For(pos);

                if (!player.IsDead && dist < e.Radius + GameConfig.PlayerRadius)
                {
                    if (player.TakeHit(e.ContactDamage)) e.Knock -= dir * 4f;
                }
            }
        }

        public void Clear()
        {
            for (int i = Active.Count - 1; i >= 0; i--) ReleaseAt(i);
        }

        void ReleaseAt(int i)
        {
            var e = Active[i];
            int last = Active.Count - 1;
            Active[i] = Active[last];
            Active.RemoveAt(last);
            pool.Release(e);
        }
    }
}
