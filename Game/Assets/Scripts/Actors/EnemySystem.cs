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
        Sprite slimeSprite;
        int zoneIndex;

        /// <summary>区画が変わったら、これから出るスライムの色と、敵の種類の割合を変える。</summary>
        public void SetZone(ZoneSpec zone)
        {
            slimeSprite = PixelArt.SlimeFor(zone);
            zoneIndex = (int)zone.Pattern;
        }

        // 区画ごとの敵の割合（スライム・コウモリ・術師・岩鬼・爆弾虫）。FloorPattern の順
        static readonly int[][] zoneMix =
        {
            new[] { 60, 25, 0, 15, 0 },   // 灰の回廊
            new[] { 45, 20, 20, 0, 15 },  // 苔むす底
            new[] { 35, 0, 15, 25, 25 },  // 残り火の炉
            new[] { 35, 20, 30, 15, 0 },  // 凍てる墓所
            new[] { 30, 15, 25, 20, 10 }, // 沈んだ書庫
            new[] { 20, 20, 20, 20, 20 }, // 虚ろの淵
        };

        EnemyKind PickKind()
        {
            var mix = zoneMix[Mathf.Clamp(zoneIndex, 0, zoneMix.Length - 1)];
            int total = 0;
            foreach (int w in mix) total += w;
            int r = UnityEngine.Random.Range(0, total);
            for (int i = 0; i < mix.Length; i++)
            {
                r -= mix[i];
                if (r < 0) return (EnemyKind)i;
            }
            return EnemyKind.Slime;
        }

        // 術師の弾
        sealed class Bullet
        {
            public SpriteRenderer Sr;
            public Vector2 Pos;
            public Vector2 Vel;
            public float Life;
            public int Damage;
        }
        readonly List<Bullet> bullets = new List<Bullet>();
        readonly Stack<Bullet> freeBullets = new Stack<Bullet>();

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

            var aura = new GameObject("Aura");
            aura.transform.SetParent(go.transform, false);
            e.Aura = aura.AddComponent<SpriteRenderer>();
            e.Aura.sprite = PixelArt.Ring;
            Lighting.MakeGlow(e.Aura);
            e.Aura.enabled = false;

            go.SetActive(false);
            return e;
        }

        /// <summary>部屋の敵：区画の割合で種類を選んで出す。</summary>
        public void SpawnMixed(Vector2 pos, float hp, int contactDamage) => Spawn(pos, hp, contactDamage, PickKind());

        public void Spawn(Vector2 pos, float hp, int contactDamage, EnemyKind kind = EnemyKind.Slime)
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
            e.Sr.sprite = slimeSprite != null ? slimeSprite : PixelArt.Slime; // 使い回すので毎回入れ直す
            e.IsBoss = false;
            e.Enraged = false;
            e.Elite = EliteKind.None;
            e.Tint = Color.white;
            e.Aura.enabled = false;
            e.MaxHp = hp;
            e.BaseScale = 1f;
            e.SlamTimer = 0f;
            e.Windup = 0f;
            e.transform.localScale = Vector3.one;
            e.Kind = kind;
            e.Prep = 0f;
            e.Charging = 0f;
            e.Phase = UnityEngine.Random.value * 6.28f;
            e.ActTimer = UnityEngine.Random.Range(0.5f, 1.5f);
            if (kind != EnemyKind.Slime) e.Sr.sprite = PixelArt.EnemySprite(kind);
            switch (kind)
            {
                case EnemyKind.Bat:
                    e.Hp *= GameConfig.BatHpMult;
                    e.Speed = GameConfig.BatSpeed * UnityEngine.Random.Range(0.9f, 1.1f);
                    e.Radius = GameConfig.SlimeRadius * 0.8f;
                    e.ContactDamage = Mathf.Max(1, Mathf.RoundToInt(contactDamage * GameConfig.BatDamageMult));
                    break;
                case EnemyKind.Caster:
                    e.Hp *= GameConfig.CasterHpMult;
                    e.Speed = GameConfig.CasterSpeed;
                    e.ActTimer = UnityEngine.Random.Range(1f, GameConfig.CasterFireInterval);
                    break;
                case EnemyKind.Brute:
                    e.Hp *= GameConfig.BruteHpMult;
                    e.Speed = GameConfig.BruteSpeed;
                    e.ContactDamage = Mathf.RoundToInt(contactDamage * GameConfig.BruteDamageMult);
                    e.BaseScale = GameConfig.BruteScale;
                    e.Radius = GameConfig.SlimeRadius * GameConfig.BruteScale;
                    e.transform.localScale = new Vector3(e.BaseScale, e.BaseScale, 1f);
                    break;
                case EnemyKind.Bomber:
                    e.Hp *= GameConfig.BomberHpMult;
                    e.Speed = GameConfig.BomberSpeed;
                    break;
            }
            e.MaxHp = e.Hp;
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
            ShowAura(e, new Color(1f, 0.25f, 0.2f));
        }

        /// <summary>足元に光る輪を出す（親の大きさに合わせて、床の上の楕円に見えるようにする）。</summary>
        static void ShowAura(Enemy e, Color color)
        {
            float r = e.Radius * 1.5f;                  // 論理座標の半径
            float k = 1f / Mathf.Max(0.01f, e.BaseScale); // 親の拡大を打ち消す
            e.Aura.transform.localScale = new Vector3(r * 1.414f * k, r * 0.707f * k, 1f);
            e.Aura.transform.localPosition = new Vector3(0f, -0.3f * k, 0f);
            color.a = 0.8f;
            e.Aura.color = color;
            e.Aura.enabled = true;
        }

        /// <summary>精鋭の名前（頭の上の表示用）。</summary>
        public static string EliteName(EliteKind kind)
        {
            switch (kind)
            {
                case EliteKind.Swift: return Loc.T("Swift", "俊足");
                case EliteKind.Hardened: return Loc.T("Hardened", "堅牢");
                case EliteKind.Splitter: return Loc.T("Splitter", "分裂");
                case EliteKind.Shroud: return Loc.T("Shroud", "覆い");
                default: return "";
            }
        }

        /// <summary>精鋭にする。大きく、色が変わり、種類ごとの性質が付く。</summary>
        public void MakeElite(Enemy e, EliteKind kind)
        {
            e.Elite = kind;
            e.Hp *= GameConfig.EliteHpMult;
            e.Radius *= GameConfig.EliteScale; // 種類ごとの大きさに対して大きくする
            e.BaseScale *= GameConfig.EliteScale;
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
            ShowAura(e, e.Tint);
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
            TickBullets(dt, player, room);
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

                // 迷路を回り込んで追う（近ければまっすぐ）。種類ごとの動きで変える
                Vector2 moveDir = room.FlowDir(pos, target);
                float speed = e.Speed;
                if (e.IsBoss)
                {
                    if (UpdateBoss(e, dt, dist)) moveDir = Vector2.zero; // 力をためている間は止まる
                }
                else if (UpdateKind(e, dt, dist, dir, ref moveDir, ref speed, player, room))
                {
                    continue; // 爆発して消えた
                }
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

                pos = room.Move(pos, (moveDir * speed + e.Knock) * dt + separation * 0.5f, e.Radius);
                e.Knock = Vector2.MoveTowards(e.Knock, Vector2.zero, 20f * dt);
                e.Pos = pos;
                e.Sr.sortingOrder = SortOrder.For(pos);
                if (e.Aura.enabled)
                {
                    e.Aura.sortingOrder = e.Sr.sortingOrder - 1;
                    var ac = e.Aura.color;
                    ac.a = 0.55f + 0.3f * Mathf.Sin(Time.time * 5f + i);
                    e.Aura.color = ac;
                }

                if (!player.IsDead && dist < e.Radius + GameConfig.PlayerRadius)
                {
                    if (player.TakeHit(e.ContactDamage)) e.Knock -= dir * 4f;
                }
            }
        }

        /// <summary>
        /// 種類ごとの動き。moveDir と speed を変える。爆弾虫が爆発して消えたときは true。
        /// </summary>
        bool UpdateKind(Enemy e, float dt, float dist, Vector2 dir, ref Vector2 moveDir, ref float speed, Player player, Room room)
        {
            switch (e.Kind)
            {
                case EnemyKind.Bat:
                {
                    // 追いながら、横にふらふら揺れる
                    var perp = new Vector2(-moveDir.y, moveDir.x);
                    moveDir = (moveDir + perp * Mathf.Sin(Time.time * 6f + e.Phase) * 0.9f).normalized;
                    break;
                }
                case EnemyKind.Caster:
                {
                    // ちょうどよい距離を保ち、ときどき弾を撃つ
                    if (dist < GameConfig.CasterRange - 1.2f) moveDir = -dir;
                    else if (dist < GameConfig.CasterRange + 1f)
                    {
                        moveDir = new Vector2(-dir.y, dir.x) * Mathf.Sign(Mathf.Sin(e.Phase));
                        speed *= 0.5f;
                    }
                    e.ActTimer -= dt;
                    if (e.ActTimer <= 0f && dist < 8f)
                    {
                        e.ActTimer = GameConfig.CasterFireInterval + UnityEngine.Random.Range(0f, 0.6f);
                        Fire(e.Pos + dir * 0.3f, dir * GameConfig.BulletSpeed, Mathf.Max(1, Mathf.RoundToInt(e.ContactDamage * GameConfig.BulletDamageMult)));
                    }
                    break;
                }
                case EnemyKind.Brute:
                {
                    if (e.Charging > 0f)
                    {
                        e.Charging -= dt;
                        moveDir = e.ChargeDir;
                        speed = GameConfig.BruteChargeSpeed;
                    }
                    else if (e.Prep > 0f)
                    {
                        // 力をためる（赤く点滅して、止まる）
                        e.Prep -= dt;
                        moveDir = Vector2.zero;
                        e.Sr.color = Mathf.Repeat(Time.time * 10f, 1f) < 0.5f ? new Color(1f, 0.5f, 0.4f) : e.Tint;
                        if (e.Prep <= 0f)
                        {
                            e.Sr.color = e.Tint;
                            e.Charging = GameConfig.BruteChargeTime;
                            e.ChargeDir = dir;
                        }
                    }
                    else
                    {
                        e.ActTimer -= dt;
                        if (e.ActTimer <= 0f && dist < 5.5f)
                        {
                            e.ActTimer = GameConfig.BruteChargeEvery;
                            e.Prep = GameConfig.BrutePrep;
                        }
                    }
                    break;
                }
                case EnemyKind.Bomber:
                {
                    if (e.Prep > 0f)
                    {
                        e.Prep -= dt;
                        moveDir = Vector2.zero;
                        float blink = Mathf.Repeat(Time.time * (6f + (GameConfig.BomberFuse - e.Prep) * 20f), 1f);
                        e.Sr.color = blink < 0.5f ? new Color(1f, 0.4f, 0.3f) : Color.white;
                        if (e.Prep <= 0f)
                        {
                            // 爆発（倒したことにはならない：経験値やドロップは出ない）
                            BossSlam?.Invoke(e.Pos, GameConfig.BomberRadius, Mathf.RoundToInt(e.ContactDamage * GameConfig.BomberDamageMult));
                            e.Dying = true;
                            e.DyingTimer = DeathTime;
                            e.Hp = 0f;
                            return true;
                        }
                    }
                    else if (dist < 1.3f)
                    {
                        e.Prep = GameConfig.BomberFuse;
                        BossWindup?.Invoke(e.Pos, GameConfig.BomberRadius, GameConfig.BomberFuse);
                    }
                    break;
                }
            }
            return false;
        }

        void Fire(Vector2 pos, Vector2 vel, int damage)
        {
            var b = freeBullets.Count > 0 ? freeBullets.Pop() : CreateBullet();
            b.Sr.gameObject.SetActive(true);
            b.Pos = pos;
            b.Vel = vel;
            b.Life = 3f;
            b.Damage = damage;
            b.Sr.transform.position = Iso.ToScreen(pos);
            bullets.Add(b);
        }

        Bullet CreateBullet()
        {
            var go = new GameObject("EnemyBullet");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PixelArt.Ember;
            sr.color = new Color(0.55f, 1f, 0.6f);
            Lighting.MakeGlow(sr);
            return new Bullet { Sr = sr };
        }

        /// <summary>術師の弾を動かす。壁に当たるか時間が来たら消える。</summary>
        void TickBullets(float dt, Player player, Room room)
        {
            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                var b = bullets[i];
                b.Life -= dt;
                b.Pos += b.Vel * dt;
                bool hit = !player.IsDead && (b.Pos - player.Pos).sqrMagnitude < (GameConfig.PlayerRadius + 0.15f) * (GameConfig.PlayerRadius + 0.15f);
                if (hit) player.TakeHit(b.Damage);
                if (hit || b.Life <= 0f || !room.Contains(b.Pos))
                {
                    b.Sr.gameObject.SetActive(false);
                    bullets.RemoveAt(i);
                    freeBullets.Push(b);
                    continue;
                }
                b.Sr.transform.position = Iso.ToScreen(b.Pos) + new Vector2(0f, 0.25f);
                b.Sr.sortingOrder = SortOrder.For(b.Pos) + 1;
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
            foreach (var b in bullets)
            {
                b.Sr.gameObject.SetActive(false);
                freeBullets.Push(b);
            }
            bullets.Clear();
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
