using System;
using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 敵の群れをまとめて動かす。1体ずつ Update を持たせず、ここで一括処理する。
    /// 当たり判定は物理エンジンを使わず、円同士の距離で計算する。
    /// </summary>
    public sealed partial class EnemySystem : MonoBehaviour
    {
        const float DeathTime = 0.15f;
        const float MaxKnock = 4f; // のけぞりの速さの上限（毎秒20で減るので、最大で約0.4マス飛ぶ）
        static readonly Color HitColor = new Color(1f, 0.55f, 0.55f, 1f);

        public readonly List<Enemy> Active = new List<Enemy>();
        public int Kills { get; set; }

        /// <summary>ダメージを与えた（位置・量・会心か）。数字の表示と音に使う。</summary>
        public event Action<Vector2, float, bool, Enemy> Damaged; // 位置・ダメージ・会心・当たった敵
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
        /// <summary>黄金の敵が出た／倒された（位置）／逃げた。</summary>
        public event Action GoldenAppeared;

        /// <summary>今いる黄金のスライム（いなければ null）。画面の目印に使う。</summary>
        public Enemy Golden
        {
            get
            {
                foreach (var e in Active)
                    if (e.Kind == EnemyKind.Golden && !e.Dying) return e;
                return null;
            }
        }
        public event Action<Vector2> GoldenKilled;
        public event Action GoldenEscaped;
        /// <summary>精鋭を倒した（位置）。</summary>
        public event Action<Vector2> EliteKilled;

        struct Split
        {
            public RoomArea Home;
            public Vector2 Pos;
            public float Hp;
            public int Damage;
            public EnemyKind Kind;   // 元の精鋭と同じ種類・段階で分かれる
            public int Tier;
            public float Scale;      // 元の大きさ（精鋭の拡大を除いたもの）
            public bool NoReward;    // 元が報酬なしなら分身もなし
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
                    if (e.IsBoss && !e.Summoned && !e.Dying && !e.Dormant) return e; // 眠っているボス・呼び出されたボスは数えない（曲・体力バー）
                }
                return null;
            }
        }

        Pool<Enemy> pool;
        Sprite slimeSprite;
        ZoneSpec zone;
        int zoneIndex;

        /// <summary>直前に倒した敵の経験値の倍率（段階）。Killed の中で読む。</summary>
        public float LastKillExpMult { get; private set; } = 1f;
        /// <summary>直前に倒した敵が、経験値・ドロップなしの敵か（ボスのお供・呼ばれた手下）。Killed の中で読む。</summary>
        public bool LastKillNoReward { get; private set; }

        /// <summary>今の区画での、その種類の敵の絵（深淵では深淵の敵の絵。黄金のスライムはいつも同じ）。</summary>
        Sprite SpriteFor(EnemyKind kind, int tier)
        {
            if (kind == EnemyKind.Golden) return PixelArt.EnemySprite(kind, tier);
            if (zone != null && Zones.IsHeavenPattern(zone.Pattern)) return PixelArt.HeavenEnemy(kind, zone, tier); // 天界は天使の敵
            if (zone != null && Zones.IsAbyssPattern(zone.Pattern)) return PixelArt.AbyssEnemy(kind, zone, tier);
            if (GameConfig.Ascent) return PixelArt.AscentEnemy(kind, tier); // 昇る塔は、明るい色の専用の敵
            if (kind == EnemyKind.Slime && zone != null) return PixelArt.SlimeFor(zone, tier);
            return PixelArt.EnemySprite(kind, tier, zone != null ? zone.Cycle : 0);
        }

        /// <summary>区画が変わったら、これから出るスライムの色と、敵の種類の割合を変える。</summary>
        public void SetZone(ZoneSpec zone)
        {
            this.zone = zone;
            slimeSprite = SpriteFor(EnemyKind.Slime, 0);
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
            new[] { 30, 15, 15, 25, 15 }, // 深淵の門
            new[] { 25, 30, 25, 10, 10 }, // 星喰いの淵
            new[] { 20, 15, 35, 15, 15 }, // 逆さ月の祭壇
            new[] { 20, 20, 20, 25, 15 }, // 深淵の玉座
            new[] { 30, 30, 15, 15, 10 }, // 雲の門
            new[] { 30, 20, 25, 10, 15 }, // 光の花園
            new[] { 20, 20, 35, 15, 10 }, // 聖歌の回廊
            new[] { 20, 20, 20, 25, 15 }, // 熾天の聖域
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
            public int Acc;
            public DebuffKind? Debuff; // 当たったときにかける弱体効果（ボスの弾）
        }
        readonly List<Bullet> bullets = new List<Bullet>();
        readonly Stack<Bullet> freeBullets = new Stack<Bullet>();

        /// <summary>
        /// 階段の封印に数える、その部屋の残りの敵（眠っている敵も数える。黄金のスライムと、呼び出されたボスは数えない）。
        /// area が null なら層全体。
        /// </summary>
        public int RemainingIn(RoomArea area)
        {
            int n = 0;
            foreach (var sp in pendingSplits) if (area == null || sp.Home == area) n++;
            foreach (var e in Active)
            {
                if (e.Dying || e.Kind == EnemyKind.Golden || e.Summoned) continue;
                if (area == null || e.Home == area) n++;
            }
            return n;
        }

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
        public void SpawnMixed(Vector2 pos, float hp, int contactDamage, int tier = 0) => Spawn(pos, hp, contactDamage, PickKind(), tier);

        public void Spawn(Vector2 pos, float hp, int contactDamage, EnemyKind kind = EnemyKind.Slime, int tier = -1)
        {
            if (Active.Count >= GameConfig.MaxEnemies) return;
            var e = pool.Get();
            e.Dormant = false;
            e.Home = null;
            e.Hp = hp;
            e.ContactDamage = contactDamage;
            e.Speed = UnityEngine.Random.Range(GameConfig.SlimeSpeedMin, GameConfig.SlimeSpeedMax);
            e.Radius = GameConfig.SlimeRadius;
            e.Knock = Vector2.zero;
            e.FlashTimer = 0f;
            e.HitPose = 0f;
            e.AttackPose = 0f;
            e.AnimT = 0f;
            e.Dying = false;
            e.DyingTimer = 0f;
            e.DeathLength = 0f;
            e.Sr.color = Color.white;
            e.Sr.sprite = slimeSprite != null ? slimeSprite : PixelArt.Slime; // 使い回すので毎回入れ直す
            e.IsBoss = false;
            e.Summoned = false;
            e.SummonIndex = 0;
            e.NoReward = false;
            e.ChainLeft = 0;
            e.Hold = 0f;
            e.InChain = false;
            e.Enraged = false;
            e.Elite = EliteKind.None;
            e.Tint = Color.white;
            e.Aura.enabled = false;
            e.MaxHp = hp;
            e.BaseScale = 1f;
            e.SlamTimer = 0f;
            e.Windup = 0f;
            e.transform.localScale = Vector3.one;
            e.transform.localRotation = Quaternion.identity;
            e.Kind = kind;
            e.Prep = 0f;
            e.Charging = 0f;
            e.Phase = UnityEngine.Random.value * 6.28f;
            e.ActTimer = UnityEngine.Random.Range(0.5f, 1.5f);
            if (kind != EnemyKind.Slime) e.Sr.sprite = SpriteFor(kind, 0);
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
                case EnemyKind.Golden:
                    e.Hp *= GameConfig.GoldenHpMult;
                    e.Speed = GameConfig.GoldenSpeed;
                    e.ActTimer = GameConfig.GoldenLife;
                    e.ContactDamage = 0;
                    e.Tint = Color.white;
                    break;
            }
            // 段階（下級・中級・上級）：強さと大きさと見た目。-1（ボス・黄金・分裂した子）は段階なし
            e.Tier = Mathf.Clamp(tier, 0, 2);
            e.ExpMult = tier < 0 ? 1f : GameConfig.TierExp[e.Tier];
            if (tier < 0)
            {
                // そのまま
            }
            else if (tier > 0)
            {
                e.Hp *= GameConfig.TierHp[tier] / GameConfig.TierHp[0];
                e.ContactDamage = Mathf.RoundToInt(e.ContactDamage * GameConfig.TierDamage[tier] / GameConfig.TierDamage[0]);
                e.Speed *= GameConfig.TierSpeed[tier] / GameConfig.TierSpeed[0];
                e.BaseScale *= GameConfig.TierScale[tier];
                e.Radius *= GameConfig.TierScale[tier];
                e.transform.localScale = new Vector3(e.BaseScale, e.BaseScale, 1f);
                e.Sr.sprite = SpriteFor(kind, tier);
            }
            else
            {
                e.Hp *= GameConfig.TierHp[0];
                e.ContactDamage = Mathf.Max(1, Mathf.RoundToInt(e.ContactDamage * GameConfig.TierDamage[0]));
                e.Speed *= GameConfig.TierSpeed[0];
            }
            // 命中・回避：層の深さ × 種類 × 段階
            float te = GameConfig.TierAccEva[e.Tier];
            e.Acc = Mathf.RoundToInt(GameConfig.EnemyAccuracy(FloorTier) * GameConfig.KindAcc[(int)kind] * te);
            e.Eva = Mathf.RoundToInt(GameConfig.EnemyEvasion(FloorTier) * GameConfig.KindEva[(int)kind] * te);
            e.MaxHp = e.Hp;
            e.Pos = pos;
            e.Sr.sortingOrder = SortOrder.For(pos);
            Active.Add(e);
        }

        /// <summary>黄金の敵を出す（光る輪付き）。</summary>
        public void SpawnGolden(Vector2 pos, float hp)
        {
            int before = Active.Count;
            Spawn(pos, hp, 0, EnemyKind.Golden);
            if (Active.Count == before) return;
            var e = Active[Active.Count - 1];
            ShowAura(e, new Color(1f, 0.85f, 0.3f));
            GoldenAppeared?.Invoke();
        }

        /// <summary>ボス：大きく、硬く、遅い。数秒ごとに周りを叩きつける。</summary>
        public void SpawnBoss(Vector2 pos, float hp, int contactDamage, BossKind? kind = null)
        {
            int before = Active.Count;
            Spawn(pos, hp, contactDamage);
            if (Active.Count == before) return; // 上限で出せなかった
            var e = Active[Active.Count - 1];
            e.IsBoss = true;
            e.Acc = Mathf.RoundToInt(GameConfig.EnemyAccuracy(FloorTier) * 1.3f);
            e.Eva = Mathf.RoundToInt(GameConfig.EnemyEvasion(FloorTier) * 0.8f);
            e.BaseScale = GameConfig.BossScale;
            e.Radius = GameConfig.SlimeRadius * GameConfig.BossScale;
            e.Speed = GameConfig.BossSpeed;
            e.SlamTimer = BossInfo.Interval * 0.6f;
            e.BossKind = kind ?? BossInfo.For(zone);
            e.LoopCycle = zone != null ? zone.Cycle : 0;
            e.AttackStep = 0;
            e.BlinkCooldown = 0f;
            e.Charging = 0f;
            e.Windup = 0f;
            e.Enraged = false;
            e.SafeSpots.Clear();
            e.Sr.sprite = PixelArt.BossSprite(e.BossKind, zone != null ? zone.Cycle : 0);
            e.transform.localScale = new Vector3(e.BaseScale, e.BaseScale, 1f);
            e.Sr.color = Color.white;
            e.Tint = Color.white;
            if (e.BossKind == BossKind.AbyssKing) e.Speed = GameConfig.BossSpeed * 0.25f; // アビスはほとんど動かず、離れた所から攻撃して歴代のボスを呼ぶ
            if (e.BossKind == BossKind.Goddess) e.Speed = GameConfig.BossSpeed * 0.1f;    // 女神像は石像なので、ほとんど動かない
            ShowAura(e, new Color(1f, 0.25f, 0.2f));
        }

        /// <summary>アビスが呼び出す歴代のボス（灰の番人から逆さ月の司祭まで、順番に）。</summary>
        static readonly BossKind[] summonOrder =
        {
            BossKind.Golem, BossKind.Mother, BossKind.Forge, BossKind.Frost, BossKind.Librarian, BossKind.Void,
            BossKind.RiftWarden, BossKind.StarEater, BossKind.MoonPriest,
        };

        /// <summary>今いる呼び出されたボスの数（同時に出しすぎないため）。</summary>
        public int SummonedAlive
        {
            get
            {
                int n = 0;
                foreach (var e in Active) if (e.Summoned && !e.Dying) n++;
                return n;
            }
        }

        /// <summary>
        /// アビスが歴代のボスを1体呼び出す。体力はアビスの一部、少し小さく、もとのボスの攻撃の順番で戦う。
        /// 呼び出したボスの種類を返す（出せなければ null）。
        /// </summary>
        public BossKind? SummonPastBoss(Enemy master, Room room)
        {
            var kind = summonOrder[master.SummonIndex % summonOrder.Length];
            int before = Active.Count;
            Vector2 p = room.Clamp(master.Pos + UnityEngine.Random.insideUnitCircle.normalized * 3f, GameConfig.SlimeRadius * 2.2f);
            Spawn(p, master.MaxHp * GameConfig.SummonedBossHpShare, Mathf.RoundToInt(master.ContactDamage * GameConfig.SummonedBossDamageShare));
            if (Active.Count == before) return null;
            master.SummonIndex++;
            var e = Active[Active.Count - 1];
            e.IsBoss = true;
            e.Summoned = true;
            e.NoReward = true;
            e.Home = master.Home;
            e.Acc = master.Acc;
            e.Eva = master.Eva;
            e.BaseScale = GameConfig.BossScale * 0.7f;
            e.Radius = GameConfig.SlimeRadius * e.BaseScale;
            e.Speed = GameConfig.BossSpeed;
            e.SlamTimer = BossInfo.Interval;
            e.BossKind = kind;
            e.LoopCycle = 0;
            e.AttackStep = 0;
            e.BlinkCooldown = 0f;
            e.Charging = 0f;
            e.Windup = 0f;
            e.Enraged = false;
            e.SafeSpots.Clear();
            e.Sr.sprite = PixelArt.BossSprite(kind);
            e.transform.localScale = new Vector3(e.BaseScale, e.BaseScale, 1f);
            e.Sr.color = Color.white;
            e.Tint = Color.white;
            ShowAura(e, new Color(1f, 0.85f, 0.95f));
            return kind;
        }

        /// <summary>歴代のボスを呼び出した（呼び出したボスの種類）。演出と知らせは GameWorld。</summary>
        public event Action<Enemy, BossKind> PastBossSummoned;

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
            e.Hp *= GameConfig.EliteHpMult * (1f + GameConfig.EarlyEliteHp * GameConfig.EarlyBiteFromTier(FloorTier)); // 序盤（5〜20層）の精鋭は少し硬い
            e.Radius *= GameConfig.EliteScale; // 種類ごとの大きさに対して大きくする
            e.BaseScale *= GameConfig.EliteScale;
            switch (kind)
            {
                case EliteKind.Swift:
                    e.Speed *= 2f;
                    e.Eva = Mathf.RoundToInt(e.Eva * 1.8f); // 俊足は避けやすい
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

        /// <summary>精鋭・ボスへのダメージの倍率（装備の効果。GameWorld が毎フレーム入れる）。</summary>
        public float EliteDamageMult = 1f;

        /// <summary>今の層の強さの尺度（弾のダメージに使う）。GameWorld が層に入るたびに入れる。</summary>
        public int FloorTier = 1;

        /// <summary>プレイヤーの近く（2マス以内）にいる、起きた敵の数（回避の囲まれ補正）。</summary>
        public int NearbyCount { get; private set; }

        /// <summary>プレイヤーの攻撃が外れた（位置）。「MISS」の表示に使う。</summary>
        public event Action<Vector2> Missed;
        public void ReportMiss(Vector2 pos) => Missed?.Invoke(pos);

        /// <summary>直前の範囲攻撃の出どころ（デバッグ表示用）。</summary>
        public string LastBlastSource { get; private set; } = "?";

        /// <summary>敵の説明（デバッグ表示用）：種類・段階・精鋭・ボス。</summary>
        public static string Describe(Enemy e)
        {
            string s = e.IsBoss ? "Boss" : $"{e.Kind} T{e.Tier}";
            if (e.Elite != EliteKind.None) s += $" Elite({e.Elite})";
            return s;
        }

        /// <summary>その部屋の眠っている敵を起こす（部屋に入ったとき、攻撃されたとき）。</summary>
        public void Wake(RoomArea area)
        {
            if (area == null) return;
            foreach (var e in Active)
            {
                if (e.Home == area && e.Dormant) e.Dormant = false;
            }
        }

        /// <summary>その部屋の、起きていて生きている敵の数。</summary>
        public int AliveIn(RoomArea area)
        {
            int n = 0;
            foreach (var e in Active)
            {
                if (!e.Dying && !e.Dormant && e.Home == area) n++;
            }
            return n;
        }

        public void Damage(Enemy e, float amount, Vector2 dir, bool crit)
        {
            if (e.Dying) return;
            if (e.Dormant) Wake(e.Home); // 外から攻撃されたら、その部屋の敵がみんな起きる
            if (e.Hold > 0f) foreach (var o in Active) if (o.Home == e.Home) o.Hold = 0f; // 登場の間に攻撃されたら、みんな動き出す
            if (e.IsBoss || e.Elite != EliteKind.None) amount *= EliteDamageMult;
            e.Hp -= amount;
            e.FlashTimer = 0.08f;
            e.HitPose = HitPoseTime;
            e.Sr.color = HitColor;
            // のけぞり。攻撃が速いと何度も足されて遠くまで飛ぶので、速さに上限をつける（1回のけぞると最大 0.4 マスほど）
            float knockMult = e.IsBoss ? 0.05f : e.Elite != EliteKind.None ? 0.5f : 1f; // ボスはほとんど、精鋭は半分しかのけぞらない
            e.Knock = Vector2.ClampMagnitude(e.Knock + dir * (crit ? 5f : 3f) * knockMult, MaxKnock * knockMult);
            Damaged?.Invoke(e.Pos, amount, crit, e);
            if (e.Hp <= 0f)
            {
                e.Dying = true;
                e.DyingTimer = e.IsBoss ? DeathTime * 4f : DeathTime;
                if ((e.BossKind == BossKind.AbyssKing || e.BossKind == BossKind.Goddess || e.BossKind == BossKind.SeraphKing) && e.IsBoss && !e.Summoned) // アビス・女神像は、ふるえながら崩れていく
                {
                    e.DeathLength = GameConfig.FinalCollapseTime; // アビスは、ふるえながら崩れていく
                    e.DyingTimer = e.DeathLength;
                }
                Kills++;
                LastKillExpMult = e.ExpMult;
                LastKillNoReward = e.NoReward;
                Codex.MarkKill(e, zone);
                Killed?.Invoke(e.Pos);
                if (e.IsBoss && !e.Summoned) BossKilled?.Invoke(e.Pos); // 呼び出された歴代のボスを倒しても、層は終わらない
                if (e.Elite != EliteKind.None && !e.NoReward) EliteKilled?.Invoke(e.Pos);
                if (e.Kind == EnemyKind.Golden) GoldenKilled?.Invoke(e.Pos);
                if (e.Elite == EliteKind.Splitter)
                {
                    for (int k = 0; k < GameConfig.SplitCount; k++)
                        pendingSplits.Add(new Split { Home = e.Home, Pos = e.Pos, Hp = e.MaxHp / GameConfig.EliteHpMult * 0.5f, Damage = e.ContactDamage,
                            Kind = e.Kind, Tier = e.Tier, Scale = e.BaseScale / GameConfig.EliteScale, NoReward = e.NoReward });
                }
            }
        }

        /// <summary>
        /// ボスを倒したとき、残りの敵・飛んでいる弾・出てくる予定の分身をまとめて消す（クリアの余韻を壊さないように）。
        /// 経験値・ドロップはなし（ボスの噴水で十分なので）。黄金のスライムは残す。
        /// 消した敵の位置を返す（煙の演出用）。
        /// </summary>
        public List<Vector2> BanishAll()
        {
            var at = new List<Vector2>();
            foreach (var e in Active)
            {
                if (e.Dying || (e.IsBoss && !e.Summoned) || e.Kind == EnemyKind.Golden) continue; // 呼び出された歴代のボスも一緒に消える
                e.Dying = true;
                e.DyingTimer = DeathTime;
                at.Add(e.Pos);
            }
            pendingSplits.Clear(); // 分裂する精鋭の分身も出さない
            // 飛んでいる敵の弾も消す（あとから当たって余韻を壊さないように）
            foreach (var b in bullets)
            {
                b.Sr.gameObject.SetActive(false);
                freeBullets.Push(b);
            }
            bullets.Clear();
            return at;
        }

        /// <summary>範囲内の敵すべてにダメージ（撃破時の爆発など）。</summary>
        public void DamageArea(Vector2 center, float radius, float amount)
        {
            float r2 = radius * radius;
            // 連鎖で倒れても一覧の並びは変わらない（消えるのは Tick の中だけ）
            for (int i = 0; i < Active.Count; i++)
            {
                var e = Active[i];
                if (e.Dying || e.Dormant) continue;
                Vector2 d = e.Pos - center;
                if (d.sqrMagnitude > r2) continue;
                Damage(e, amount, d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.up, false);
            }
        }

        /// <summary>範囲内の敵を中心へ引き寄せる（深淵の裂け目）。ボスは動かない。</summary>
        public void Pull(Vector2 center, float radius, float strength)
        {
            float r2 = radius * radius;
            foreach (var e in Active)
            {
                if (e.Dying || e.Dormant || e.IsBoss) continue;
                Vector2 d = center - e.Pos;
                float sq = d.sqrMagnitude;
                if (sq > r2 || sq < 0.04f) continue;
                e.Knock = Vector2.ClampMagnitude(e.Knock + d.normalized * strength, MaxKnock * 1.5f);
            }
        }

        /// <summary>一番近い敵（眠っている敵は狙わない）。</summary>
        /// <summary>近い順に最大 n 体（眠っている・倒れかけの敵は除く）。</summary>
        public List<Enemy> FindNearestN(Vector2 from, float range, int n)
        {
            var list = new List<Enemy>();
            float rangeSq = range * range;
            foreach (var e in Active)
            {
                if (e.Dying || e.Dormant) continue;
                if ((e.Pos - from).sqrMagnitude < rangeSq) list.Add(e);
            }
            list.Sort((a, b) => (a.Pos - from).sqrMagnitude.CompareTo((b.Pos - from).sqrMagnitude));
            if (list.Count > n) list.RemoveRange(n, list.Count - n);
            return list;
        }

        public Enemy FindNearest(Vector2 from, float range)
        {
            Enemy best = null;
            float bestSq = range * range;
            foreach (var e in Active)
            {
                if (e.Dying || e.Dormant) continue;
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
                // 元の精鋭と同じ種類で作り、体力・ダメージ・見た目・大きさは元から決めた値で上書きする（種類・段階の補正を二重に掛けないように）
                Spawn(room.Clamp(s.Pos + offset, GameConfig.SlimeRadius), s.Hp, s.Damage, s.Kind);
                if (Active.Count == 0) continue;
                var small = Active[Active.Count - 1];
                small.Home = s.Home;
                small.Hp = s.Hp;
                small.MaxHp = s.Hp;
                small.ContactDamage = s.Damage;
                small.Tier = s.Tier;
                small.Sr.sprite = SpriteFor(s.Kind, s.Tier);
                small.BaseScale = s.Scale * 0.75f;
                small.Radius = GameConfig.SlimeRadius * small.BaseScale;
                small.Speed *= 1.3f;
                small.NoReward = s.NoReward;
                small.transform.localScale = new Vector3(small.BaseScale, small.BaseScale, 1f);
            }
            pendingSplits.Clear();

            ShroudNear = false;
            // 囲まれ補正のための、プレイヤーの近くの起きた敵の数
            NearbyCount = 0;
            float cr2 = GameConfig.CrowdRadius * GameConfig.CrowdRadius;
            foreach (var o in Active)
            {
                if (!o.Dying && !o.Dormant && (o.Pos - player.Pos).sqrMagnitude <= cr2) NearbyCount++;
            }
            TickBullets(dt, player, room);
            Vector2 target = player.Pos;
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                var e = Active[i];

                if (e.Dying)
                {
                    e.DyingTimer -= dt;
                    if (e.DeathLength > 0f)
                    {
                        // アビスの崩壊：小刻みにふるえ、白く明滅しながら、縮んで薄れていく
                        float tc = 1f - Mathf.Clamp01(e.DyingTimer / e.DeathLength);
                        float shake = 0.06f + 0.12f * tc;
                        e.transform.position = Iso.ToScreen(e.Pos) + UnityEngine.Random.insideUnitCircle * shake;
                        e.transform.localScale = Vector3.one * (e.BaseScale * (1f - 0.35f * tc * tc));
                        e.transform.localRotation = Quaternion.identity;
                        bool flash = Mathf.Repeat(Time.time * (6f + 18f * tc), 1f) < 0.5f;
                        e.Sr.color = flash ? new Color(1f, 1f, 1f, 1f - tc * tc) : new Color(1f, 0.55f, 0.7f, 1f - tc * tc);
                        if (e.DyingTimer <= 0f) ReleaseAt(i);
                        continue;
                    }
                    // 倒れたら、横につぶれて縮みながら消える（大きく膨らむのは「撃破時の爆発」の加護と紛らわしいので、やめた）。ボスは今までどおり膨らむ
                    e.transform.localRotation = Quaternion.identity;
                    float t = 1f - Mathf.Clamp01(e.DyingTimer / (e.IsBoss ? DeathTime * 4f : DeathTime));
                    e.transform.localScale = e.IsBoss ? Vector3.one * (e.BaseScale * (1f + t * 0.6f))
                        : new Vector3(e.BaseScale * (1f + t * 0.25f), e.BaseScale * (1f - t * 0.7f), 1f);
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

                if (e.Hold > 0f)
                {
                    // 現れたばかり：その場で身構えるだけ（ボスの登場の間。攻撃されたら動き出す）
                    e.Hold -= dt;
                    float breathe = 1f + 0.06f * Mathf.Sin(Time.time * 5f + e.Phase);
                    e.transform.localScale = new Vector3(e.BaseScale, e.BaseScale * breathe, 1f);
                    continue;
                }

                if (e.Dormant)
                {
                    // 眠っている：その場でゆっくり揺れるだけ（動き・攻撃の計算はしない）
                    float bob = 1f + 0.04f * Mathf.Sin(Time.time * 2f + e.Phase);
                    e.transform.localScale = new Vector3(e.BaseScale, e.BaseScale * bob, 1f);
                    continue;
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
                    if (UpdateBoss(e, dt, dist, dir, target, ref moveDir, ref speed, room)) moveDir = Vector2.zero; // 予告している間は止まる
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
                AnimateBody(e, dt, speed * moveDir.magnitude);
                e.Sr.sortingOrder = SortOrder.For(pos);
                if (e.Aura.enabled)
                {
                    e.Aura.sortingOrder = e.Sr.sortingOrder - 1;
                    var ac = e.Aura.color;
                    ac.a = 0.55f + 0.3f * Mathf.Sin(Time.time * 5f + i);
                    e.Aura.color = ac;
                }

                if (!player.IsDead && e.ContactDamage > 0 && dist < e.Radius + GameConfig.PlayerRadius)
                {
                    if (player.TryHit(e.ContactDamage, Describe(e) + " contact", e.Acc, NearbyCount, e.IsBoss ? GameConfig.BossMinHit : 0)) e.Knock -= dir * 4f; // ボスの体当たり・突進は最低ダメージあり
                }
            }
        }

        const float HitPoseTime = 0.14f;

        /// <summary>
        /// 体の動き（絵は描き替えず、伸び縮み・傾き・上下で見せる）。足元は床に付けたまま。
        /// スライムは跳ね、蝙蝠は羽ばたき、術士は浮かび、巨漢は体をゆすって歩き、爆弾虫はせわしなく揺れる。
        /// ボスはゆっくり息をし、力をためている間は膨らんでふるえる。当たると一瞬つぶれる。
        /// </summary>
        void AnimateBody(Enemy e, float dt, float moveSpeed)
        {
            bool moving = moveSpeed > 0.05f;
            e.AnimT += dt * (moving ? 1f + Mathf.Min(moveSpeed, 4f) * 0.5f : 0.6f);
            e.HitPose = Mathf.Max(0f, e.HitPose - dt);
            e.AttackPose = Mathf.Max(0f, e.AttackPose - dt);
            float t = e.AnimT + e.Phase;
            float sx = 1f, sy = 1f, angle = 0f, lift = 0f;
            Vector2 shake = Vector2.zero;
            if (e.IsBoss)
            {
                float breath = Mathf.Sin(t * 2.2f);
                sy = 1f + 0.03f * breath; sx = 1f - 0.015f * breath;
                if (e.Windup > 0f)
                {
                    // 力をためる：ぐっと膨らみ、小刻みにふるえる
                    sx *= 1.07f; sy *= 1.05f;
                    shake = UnityEngine.Random.insideUnitCircle * 0.035f;
                }
            }
            else
            {
                switch (e.Kind)
                {
                    case EnemyKind.Bat:
                    {
                        if (e.Prep > 0f)
                        {
                            // 急降下の前：ふわっと舞い上がって、羽を大きく広げる
                            float p = 1f - Mathf.Clamp01(e.Prep / GameConfig.BatDivePrep);
                            sx = 1f + 0.15f * p; sy = 1f - 0.1f * p; lift = 0.1f + 0.15f * p;
                            break;
                        }
                        if (e.Charging > 0f)
                        {
                            // 急降下：羽をたたんで、進む方へ傾く
                            var sd = Iso.ToScreen(e.ChargeDir);
                            angle = -(sd.x >= 0f ? 1f : -1f) * 25f;
                            sx = 0.8f; sy = 1.15f; lift = 0.05f;
                            break;
                        }
                        float flap = Mathf.Sin(t * 16f);
                        sy = 1f + 0.12f * flap; sx = 1f - 0.05f * flap;
                        lift = 0.06f + 0.04f * Mathf.Sin(t * 4f);
                        break;
                    }
                    case EnemyKind.Caster:
                    {
                        lift = 0.05f + 0.04f * Mathf.Sin(t * 3f);
                        angle = 3f * Mathf.Sin(t * 1.5f);
                        // 撃つ前：杖を掲げるように伸び上がる／撃った直後：反動で縮む
                        if (e.ActTimer < 0.35f) { float p = 1f - Mathf.Clamp01(e.ActTimer / 0.35f); sy = 1f + 0.14f * p; sx = 1f - 0.06f * p; lift += 0.05f * p; }
                        if (e.AttackPose > 0f) { float r = e.AttackPose / 0.15f; sy = 1f - 0.12f * r; sx = 1f + 0.1f * r; }
                        break;
                    }
                    case EnemyKind.Brute:
                    {
                        if (e.Prep > 0f)
                        {
                            // 力をためる：身をかがめて、小刻みにふるえる
                            float p = 1f - Mathf.Clamp01(e.Prep / GameConfig.BrutePrep);
                            sy = 1f - 0.16f * p; sx = 1f + 0.12f * p;
                            shake = UnityEngine.Random.insideUnitCircle * 0.025f * p;
                        }
                        else if (e.Charging > 0f)
                        {
                            // 突進：進む方へ前のめりに、低く長く
                            var sd = Iso.ToScreen(e.ChargeDir);
                            angle = -(sd.x >= 0f ? 1f : -1f) * 16f;
                            sx = 1.14f; sy = 0.9f;
                        }
                        else
                        {
                            float step = Mathf.Sin(t * 5f);
                            angle = (moving ? 6f : 2f) * step;
                            sy = 1f - (moving ? 0.04f : 0.02f) * Mathf.Abs(step);
                        }
                        break;
                    }
                    case EnemyKind.Bomber:
                        if (e.Prep > 0f)
                        {
                            // 導火線に火がついた：どんどん膨らみ、激しくふるえる
                            float p = 1f - Mathf.Clamp01(e.Prep / GameConfig.BomberFuse);
                            sx = sy = 1f + 0.3f * p * p;
                            angle = (6f + 10f * p) * Mathf.Sin(Time.time * (30f + 30f * p));
                        }
                        else
                        {
                            angle = 8f * Mathf.Sin(t * 14f);
                            sy = 1f + 0.05f * Mathf.Sin(t * 9f);
                        }
                        break;
                    default:
                    {
                        if (e.Kind == EnemyKind.Slime && e.Prep > 0f)
                        {
                            // 跳びかかる前：ぎゅっと縮む
                            float p = 1f - Mathf.Clamp01(e.Prep / GameConfig.SlimePouncePrep);
                            sy = 1f - 0.25f * p; sx = 1f + 0.18f * p;
                            break;
                        }
                        if (e.Kind == EnemyKind.Slime && e.Charging > 0f)
                        {
                            // 跳びかかり：縦に伸びて、弧を描いて跳ぶ
                            float p = 1f - Mathf.Clamp01(e.Charging / GameConfig.SlimePounceTime);
                            sy = 1.2f; sx = 0.85f; lift = Mathf.Sin(p * Mathf.PI) * 0.35f;
                            break;
                        }
                        // スライム（黄金も）：ぷるんと跳ねる。動いているときは大きく
                        float hop = Mathf.Sin(t * (moving ? 9f : 4f));
                        float amp = moving ? 0.12f : 0.05f;
                        sy = 1f + amp * hop; sx = 1f - amp * 0.7f * hop;
                        if (moving) lift = Mathf.Max(0f, hop) * 0.05f;
                        break;
                    }
                }
            }
            if (e.HitPose > 0f)
            {
                float h = e.HitPose / HitPoseTime;
                float k = e.IsBoss ? 0.4f : 1f;
                sx *= 1f + 0.16f * h * k; sy *= 1f - 0.14f * h * k;
            }
            float b = e.BaseScale;
            e.transform.localScale = new Vector3(b * sx, b * sy, 1f);
            e.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            float hgt = e.Sr.sprite != null ? e.Sr.sprite.bounds.size.y : 1f;
            float offY = (sy - 1f) * 0.5f * hgt * b + lift;
            e.transform.position = Iso.ToScreen(e.Pos) + new Vector2(0f, offY) + shake;
            if (e.Aura.enabled)
            {
                // 足元の輪は体の動きに付いていかず、床に置いたまま
                float r = e.Radius * 1.5f, k = 1f / Mathf.Max(0.01f, b);
                e.Aura.transform.localScale = new Vector3(r * 1.414f * k / sx, r * 0.707f * k / sy, 1f);
                e.Aura.transform.localRotation = Quaternion.Euler(0f, 0f, -angle);
                e.Aura.transform.localPosition = new Vector3(0f, (-0.3f - offY) / Mathf.Max(0.01f, b * sy), 0f);
            }
        }

        /// <summary>
        /// 種類ごとの動き。moveDir と speed を変える。爆弾虫が爆発して消えたときは true。
        /// </summary>
        /// <summary>
        /// 近くで身構えてから、まっすぐ速く突っ込む（コウモリの急降下・スライムの跳びかかり）。突っ込み中・身構え中は true。
        /// </summary>
        static bool Lunge(Enemy e, float dt, float dist, Vector2 dir, ref Vector2 moveDir, ref float speed, float range, float every, float prep, float time, float lungeSpeed)
        {
            if (e.Charging > 0f)
            {
                e.Charging -= dt;
                moveDir = e.ChargeDir;
                speed = lungeSpeed;
                return true;
            }
            if (e.Prep > 0f)
            {
                e.Prep -= dt;
                moveDir = Vector2.zero;
                if (e.Prep <= 0f)
                {
                    e.Charging = time;
                    e.ChargeDir = dir;
                }
                return true;
            }
            e.ActTimer -= dt;
            if (e.ActTimer <= 0f && dist < range && dist > 0.5f)
            {
                e.ActTimer = every + UnityEngine.Random.Range(0f, 1f);
                e.Prep = prep;
                moveDir = Vector2.zero;
                return true;
            }
            return false;
        }

        bool UpdateKind(Enemy e, float dt, float dist, Vector2 dir, ref Vector2 moveDir, ref float speed, Player player, Room room)
        {
            switch (e.Kind)
            {
                case EnemyKind.Bat:
                {
                    if (Lunge(e, dt, dist, dir, ref moveDir, ref speed, GameConfig.BatDiveRange, GameConfig.BatDiveEvery, GameConfig.BatDivePrep, GameConfig.BatDiveTime, GameConfig.BatDiveSpeed)) break;
                    // 追いながら、横にふらふら揺れる
                    var perp = new Vector2(-moveDir.y, moveDir.x);
                    moveDir = (moveDir + perp * Mathf.Sin(Time.time * 6f + e.Phase) * 0.9f).normalized;
                    break;
                }
                case EnemyKind.Slime:
                    Lunge(e, dt, dist, dir, ref moveDir, ref speed, GameConfig.SlimePounceRange, GameConfig.SlimePounceEvery, GameConfig.SlimePouncePrep, GameConfig.SlimePounceTime, GameConfig.SlimePounceSpeed);
                    break;
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
                        float bulletMult = GameConfig.TierDamage[e.Tier] * (e.Elite != EliteKind.None ? 1.3f : 1f);
                        fireAcc = e.Acc;
                        e.AttackPose = 0.15f;
                        Fire(e.Pos + dir * 0.3f, dir * GameConfig.BulletSpeed, Mathf.Max(1, Mathf.RoundToInt(GameConfig.BulletDamage(FloorTier) * bulletMult)));
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
                case EnemyKind.Golden:
                {
                    // 逃げる。しばらくすると消える
                    moveDir = dist < 7f ? -dir : moveDir * 0.3f;
                    e.ActTimer -= dt;
                    if (e.ActTimer < 2f) e.Sr.color = Mathf.Repeat(Time.time * 8f, 1f) < 0.5f ? new Color(1f, 1f, 1f, 0.4f) : Color.white;
                    if (e.ActTimer <= 0f)
                    {
                        GoldenEscaped?.Invoke();
                        e.Dying = true;
                        e.DyingTimer = DeathTime;
                        e.Hp = 0f;
                        return true;
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
                            LastBlastSource = Describe(e) + " blast";
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

        int fireAcc; // 次に撃つ弾の命中（撃った術師の命中）

        void Fire(Vector2 pos, Vector2 vel, int damage, DebuffKind? debuff = null)
        {
            var b = freeBullets.Count > 0 ? freeBullets.Pop() : CreateBullet();
            b.Sr.gameObject.SetActive(true);
            b.Pos = pos;
            b.Vel = vel;
            b.Life = 3f;
            b.Damage = damage;
            b.Acc = fireAcc;
            b.Debuff = debuff;
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
                if (hit && player.TryHit(b.Damage, b.Debuff.HasValue ? "Boss bullet" : "Caster bullet", b.Acc, NearbyCount) && b.Debuff.HasValue)
                    player.AddDebuff(b.Debuff.Value, BossInfo.DebuffTimeOf(b.Debuff.Value));
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
