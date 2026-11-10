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
        /// <summary>ボスを倒した（位置）。</summary>
        public event Action<Vector2> BossKilled;
        /// <summary>ボスが力をため始めた（位置・範囲・ためる時間）。</summary>
        public event Action<Vector2, float, float> BossWindup;
        /// <summary>ボスが叩きつけた（位置・範囲・ダメージ）。</summary>
        public event Action<Vector2, float, int> BossSlam;
        /// <summary>ボスが怒った（体力が半分を切った）。</summary>
        public event Action BossEnraged;
        /// <summary>精鋭を倒した（位置）。</summary>
        public event Action<Vector2> EliteKilled;

        struct Split
        {
            public Vector2 Pos;
            public float Hp;
            public int Damage;
        }

        // 分裂は一覧を回している最中に起きるので、次の Tick の最初に出す
        readonly List<Split> pendingSplits = new List<Split>();

        /// <summary>Shroud の精鋭がプレイヤーの近くにいるか（明るさを奪う）。</summary>
        public bool ShroudNear { get; private set; }

        /// <summary>生きているボス（いなければ null）。体力バーの表示に使う。</summary>
        public Enemy Boss
        {
            get
            {
                foreach (var e in Active)
                {
                    if (e.IsBoss && !e.Dying) return e;
                }
                return null;
            }
        }

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

        public void Spawn(Vector2 pos, float hp, int contactDamage)
        {
            if (Active.Count >= GameConfig.MaxEnemies) return;
            var e = pool.Get();
            e.Hp = hp;
            e.ContactDamage = contactDamage;
            e.Speed = UnityEngine.Random.Range(GameConfig.SlimeSpeedMin, GameConfig.SlimeSpeedMax);
            e.Radius = GameConfig.SlimeRadius;
            e.Knock = Vector2.zero;
            e.FlashTimer = 0f;
            e.Dying = false;
            e.DyingTimer = 0f;
            e.Sr.color = Color.white;
            e.IsBoss = false;
            e.Enraged = false;
            e.Elite = EliteKind.None;
            e.Tint = Color.white;
            e.MaxHp = hp;
            e.BaseScale = 1f;
            e.SlamTimer = 0f;
            e.Windup = 0f;
            e.transform.localScale = Vector3.one;
            e.Pos = pos;
            e.Sr.sortingOrder = SortOrder.For(pos);
            Active.Add(e);
        }

        /// <summary>ボス：大きく、硬く、遅い。数秒ごとに周りを叩きつける。</summary>
        public void SpawnBoss(Vector2 pos, float hp, int contactDamage)
        {
            int before = Active.Count;
            Spawn(pos, hp, contactDamage);
            if (Active.Count == before) return; // 上限で出せなかった
            var e = Active[Active.Count - 1];
            e.IsBoss = true;
            e.BaseScale = GameConfig.BossScale;
            e.Radius = GameConfig.SlimeRadius * GameConfig.BossScale;
            e.Speed = GameConfig.BossSpeed;
            e.SlamTimer = GameConfig.BossSlamInterval * 0.6f;
            e.transform.localScale = new Vector3(e.BaseScale, e.BaseScale, 1f);
            e.Sr.color = GameConfig.BossTint;
            e.Tint = GameConfig.BossTint;
        }

        /// <summary>精鋭にする。大きく、色が変わり、種類ごとの性質が付く。</summary>
        public void MakeElite(Enemy e, EliteKind kind)
        {
            e.Elite = kind;
            e.Hp *= GameConfig.EliteHpMult;
            e.BaseScale = GameConfig.EliteScale;
            e.Radius = GameConfig.SlimeRadius * GameConfig.EliteScale;
            switch (kind)
            {
                case EliteKind.Swift:
                    e.Speed *= 2f;
                    e.Tint = new Color(0.55f, 1f, 1f);
                    break;
                case EliteKind.Hardened:
                    e.Hp *= 2.5f;
                    e.Speed *= 0.75f;
                    e.Tint = new Color(0.75f, 0.75f, 0.8f);
                    break;
                case EliteKind.Splitter:
                    e.Tint = new Color(1f, 0.85f, 0.35f);
                    break;
                case EliteKind.Shroud:
                    e.Tint = new Color(0.45f, 0.3f, 0.6f);
                    break;
            }
            e.MaxHp = e.Hp;
            e.Sr.color = e.Tint;
            e.transform.localScale = new Vector3(e.BaseScale, e.BaseScale, 1f);
        }

        /// <summary>まだ出ていない精鋭の種類をランダムに選ぶ。</summary>
        public static EliteKind RandomElite() => (EliteKind)UnityEngine.Random.Range(1, 5);

        public void Damage(Enemy e, float amount, Vector2 dir, bool crit)
        {
            if (e.Dying) return;
            e.Hp -= amount;
            e.FlashTimer = 0.08f;
            e.Sr.color = HitColor;
            e.Knock += dir * (crit ? 5f : 3f) * (e.IsBoss ? 0.05f : 1f); // ボスはほとんどのけぞらない
            Damaged?.Invoke(e.Pos, amount, crit);
            if (e.Hp <= 0f)
            {
                e.Dying = true;
                e.DyingTimer = e.IsBoss ? DeathTime * 4f : DeathTime;
                Kills++;
                Killed?.Invoke(e.Pos);
                if (e.IsBoss) BossKilled?.Invoke(e.Pos);
                if (e.Elite != EliteKind.None) EliteKilled?.Invoke(e.Pos);
                if (e.Elite == EliteKind.Splitter)
                {
                    for (int k = 0; k < GameConfig.SplitCount; k++)
                        pendingSplits.Add(new Split { Pos = e.Pos, Hp = e.MaxHp / GameConfig.EliteHpMult * 0.5f, Damage = e.ContactDamage });
                }
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
            // 分裂（前の Tick で倒れた Splitter の分）
            foreach (var s in pendingSplits)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * 0.6f;
                Spawn(room.Clamp(s.Pos + offset, GameConfig.SlimeRadius), s.Hp, s.Damage);
                if (Active.Count == 0) continue;
                var small = Active[Active.Count - 1];
                small.BaseScale = 0.75f;
                small.Radius = GameConfig.SlimeRadius * 0.75f;
                small.Speed *= 1.3f;
                small.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
            }
            pendingSplits.Clear();

            ShroudNear = false;
            Vector2 target = player.Pos;
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                var e = Active[i];

                if (e.Dying)
                {
                    // 倒れたら少し膨らんで消える
                    e.DyingTimer -= dt;
                    float t = 1f - Mathf.Clamp01(e.DyingTimer / (e.IsBoss ? DeathTime * 4f : DeathTime));
                    e.transform.localScale = Vector3.one * (e.BaseScale * (1f + t * 0.6f));
                    var c = e.Sr.color;
                    c.a = 1f - t;
                    e.Sr.color = c;
                    if (e.DyingTimer <= 0f) ReleaseAt(i);
                    continue;
                }

                if (e.FlashTimer > 0f)
                {
                    e.FlashTimer -= dt;
                    if (e.FlashTimer <= 0f) e.Sr.color = e.Tint;
                }

                Vector2 pos = e.Pos;
                Vector2 to = target - pos;
                float dist = to.magnitude;
                Vector2 dir = dist > 0.001f ? to / dist : Vector2.zero;

                if (e.IsBoss && UpdateBoss(e, dt, dist)) dir = Vector2.zero; // 力をためている間は止まる
                if (e.Elite == EliteKind.Shroud && dist < GameConfig.ShroudRange) ShroudNear = true;

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

        /// <summary>ボスの叩きつけ。力をためている間は true（動かない）。</summary>
        bool UpdateBoss(Enemy e, float dt, float distToPlayer)
        {
            // 体力が半分を切ったら怒る：速くなり、叩きつけの間隔が短くなる
            if (!e.Enraged && e.Hp < e.MaxHp * 0.5f)
            {
                e.Enraged = true;
                e.Speed *= GameConfig.BossEnragedSpeedMult;
                e.Tint = new Color(1f, 0.25f, 0.2f);
                e.Sr.color = e.Tint;
                BossEnraged?.Invoke();
            }

            if (e.Windup > 0f)
            {
                e.Windup -= dt;
                if (e.Windup <= 0f)
                {
                    BossSlam?.Invoke(e.Pos, GameConfig.BossSlamRadius, e.ContactDamage * 2);
                    e.SlamTimer = e.Enraged ? GameConfig.BossEnragedSlamInterval : GameConfig.BossSlamInterval;
                }
                return true;
            }
            e.SlamTimer -= dt;
            if (e.SlamTimer <= 0f && distToPlayer < GameConfig.BossSlamRadius + 2f)
            {
                e.Windup = GameConfig.BossWindup;
                BossWindup?.Invoke(e.Pos, GameConfig.BossSlamRadius, GameConfig.BossWindup);
                return true;
            }
            return false;
        }

        public void Clear()
        {
            pendingSplits.Clear();
            ShroudNear = false;
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
