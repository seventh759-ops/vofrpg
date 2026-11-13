using System;
using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>区域ごとのボス（FloorPattern の順）。RiftWarden からは深淵のボス。</summary>
    public enum BossKind { Golem, Mother, Forge, Frost, Librarian, Void, RiftWarden, StarEater, MoonPriest, AbyssKing }

    /// <summary>
    /// ボスの攻撃。どれも予告（Windup）のあとに判定する。
    /// Slam：自分のまわり／Charge：突進／BulletRing：全方向の弾／Summon：手下を呼ぶ／Cone：扇状／
    /// StunNova：気絶する波／BulletFan：扇状の弾／RoomWide：全体（安全な円の中だけ無事）／Blink：瞬間移動。
    /// </summary>
    public enum BossAttack { Slam, Charge, BulletRing, Summon, Cone, StunNova, BulletFan, RoomWide, Blink }

    public static class BossInfo
    {
        public const float SlamRadius = 2.6f;
        public const float NovaRadius = 3.2f;
        public const float ConeRange = 5.5f;
        public const float ConeHalfAngle = 35f;
        public const float SafeRadius = 1.6f;
        public const int SafeSpots = 3;
        public const float StunTime = 1.2f;
        public const float DebuffTime = 6f;
        public const float SlowTime = 2.5f;
        public const float ChargeSpeed = 9f;
        public const float ChargeTime = 0.8f;
        public const float Interval = 2.8f;         // 次の攻撃まで
        public const float EnragedInterval = 1.8f;
        public const float BlinkCooldown = 4f;       // 司書が近づかれて離れる瞬間移動の待ち時間
        public const float KeepAway = 5.5f;          // 司書が保とうとする距離

        public static BossKind For(ZoneSpec z) => z == null ? BossKind.Golem : (BossKind)Mathf.Clamp((int)z.Pattern, 0, (int)BossKind.AbyssKing);

        /// <summary>深淵のボスか（攻撃の間隔が短く、弾が多い）。</summary>
        public static bool IsAbyss(BossKind k) => k >= BossKind.RiftWarden;

        /// <summary>距離を取って撃ち、近づかれると離れた場所へ瞬間移動するボス。</summary>
        public static bool KeepsAway(BossKind k) => k == BossKind.Librarian || k == BossKind.StarEater;

        public static string Name(BossKind k)
        {
            switch (k)
            {
                case BossKind.Mother: return Loc.T("Moss Mother", "苔の母");
                case BossKind.Forge: return Loc.T("Forge Giant", "炉の巨人");
                case BossKind.Frost: return Loc.T("Frost King", "霜の王");
                case BossKind.Librarian: return Loc.T("Archive Keeper", "書庫の司書");
                case BossKind.Void: return Loc.T("Hollow Shade", "虚ろの影");
                case BossKind.RiftWarden: return Loc.T("Rift Warden", "深淵の門番");
                case BossKind.StarEater: return Loc.T("Star Eater", "星喰らい");
                case BossKind.MoonPriest: return Loc.T("Inverted Moon Priest", "逆さ月の司祭");
                case BossKind.AbyssKing: return Loc.T("King of the Abyss", "深淵の王");
                default: return Loc.T("Ash Warden", "灰の番人");
            }
        }

        /// <summary>ボスごとの攻撃の順番（くり返す）。</summary>
        static readonly BossAttack[][] patterns =
        {
            new[] { BossAttack.Slam, BossAttack.Charge, BossAttack.Slam, BossAttack.Charge },
            new[] { BossAttack.BulletRing, BossAttack.Summon, BossAttack.Slam },
            new[] { BossAttack.Cone, BossAttack.Slam, BossAttack.Cone },
            new[] { BossAttack.StunNova, BossAttack.BulletFan, BossAttack.BulletFan },
            new[] { BossAttack.RoomWide, BossAttack.BulletRing, BossAttack.BulletFan, BossAttack.BulletFan },
            new[] { BossAttack.RoomWide, BossAttack.Cone, BossAttack.StunNova, BossAttack.Blink },
            // 深淵：突進と扇で押し込んでくる門番
            new[] { BossAttack.Charge, BossAttack.Cone, BossAttack.Charge, BossAttack.Slam, BossAttack.Summon },
            // 深淵：離れて弾幕を張り、近づくと消える星喰らい
            new[] { BossAttack.BulletRing, BossAttack.BulletFan, BossAttack.BulletRing, BossAttack.RoomWide, BossAttack.BulletFan },
            // 深淵：全体攻撃と気絶で縛る司祭
            new[] { BossAttack.RoomWide, BossAttack.StunNova, BossAttack.BulletFan, BossAttack.Summon, BossAttack.Cone },
            // 深淵の王：すべての技を使う
            new[] { BossAttack.Slam, BossAttack.Cone, BossAttack.BulletRing, BossAttack.Charge, BossAttack.RoomWide, BossAttack.StunNova, BossAttack.BulletFan, BossAttack.Blink },
        };

        public static BossAttack Next(Enemy e) => patterns[(int)e.BossKind][e.AttackStep % patterns[(int)e.BossKind].Length];

        public static float Windup(BossAttack a)
        {
            switch (a)
            {
                case BossAttack.Charge: return 0.7f;
                case BossAttack.BulletRing: return 0.6f;
                case BossAttack.Summon: return 0.8f;
                case BossAttack.Cone: return 0.9f;
                case BossAttack.StunNova: return 1f;
                case BossAttack.BulletFan: return 0.5f;
                case BossAttack.RoomWide: return 2.2f;
                case BossAttack.Blink: return 0.5f;
                default: return 0.8f;
            }
        }

        /// <summary>その攻撃を始めてよい距離か（近づかないと当たらない攻撃は、近いときだけ）。</summary>
        public static bool InRange(BossAttack a, float dist)
        {
            switch (a)
            {
                case BossAttack.Slam: return dist < SlamRadius + 2f;
                case BossAttack.StunNova: return dist < NovaRadius + 2f;
                case BossAttack.Cone: return dist < ConeRange + 1f;
                case BossAttack.Charge: return dist < 8f;
                default: return dist < 14f;
            }
        }

        /// <summary>範囲攻撃・弾で、そのボスがかける弱体効果（なければ null）。</summary>
        public static DebuffKind? DebuffOf(BossKind k, BossAttack a)
        {
            if (a == BossAttack.StunNova) return DebuffKind.Stun;
            switch (k)
            {
                case BossKind.Mother: return a == BossAttack.BulletRing ? DebuffKind.AttackDown : (DebuffKind?)null;
                case BossKind.Forge: return a == BossAttack.Cone ? DebuffKind.DefenseDown : (DebuffKind?)null;
                case BossKind.Frost: return a == BossAttack.BulletFan ? DebuffKind.Slow : (DebuffKind?)null;
                case BossKind.Librarian: return a == BossAttack.RoomWide ? DebuffKind.AttackDown : (DebuffKind?)null;
                case BossKind.Void: return a == BossAttack.RoomWide ? DebuffKind.DefenseDown : a == BossAttack.Cone ? DebuffKind.AttackDown : (DebuffKind?)null;
                case BossKind.RiftWarden: return a == BossAttack.Cone ? DebuffKind.DefenseDown : (DebuffKind?)null;
                case BossKind.StarEater: return a == BossAttack.BulletFan ? DebuffKind.Slow : a == BossAttack.RoomWide ? DebuffKind.Blind : (DebuffKind?)null;
                case BossKind.MoonPriest: return a == BossAttack.RoomWide ? DebuffKind.AttackDown : a == BossAttack.BulletFan ? DebuffKind.Slow : (DebuffKind?)null;
                case BossKind.AbyssKing: return a == BossAttack.RoomWide ? DebuffKind.DefenseDown : a == BossAttack.Cone ? DebuffKind.AttackDown : a == BossAttack.BulletFan ? DebuffKind.Slow : (DebuffKind?)null;
            }
            return null;
        }

        public static float DebuffTimeOf(DebuffKind k) => k == DebuffKind.Stun ? StunTime : k == DebuffKind.Slow ? SlowTime : DebuffTime;

        /// <summary>範囲攻撃のダメージ（接触ダメージに対する倍率）。</summary>
        public static float DamageMult(BossAttack a)
        {
            switch (a)
            {
                case BossAttack.StunNova: return 1.2f;
                case BossAttack.Cone: return 2.2f;
                case BossAttack.RoomWide: return 2.5f;
                default: return 2f;
            }
        }
    }

    public sealed partial class EnemySystem
    {
        /// <summary>ボスが攻撃の予告を始めた（ボス、攻撃、予告の時間）。予告の表示に使う。</summary>
        public event Action<Enemy, BossAttack, float> BossTelegraph;

        /// <summary>ボスの攻撃が決まった（範囲攻撃の当たり判定・弱体効果・演出は GameWorld）。</summary>
        public event Action<Enemy, BossAttack> BossStrike;

        /// <summary>
        /// ボスの1フレーム。止まって予告している間は true。突進中・司書の距離取りは moveDir と speed を書きかえる。
        /// </summary>
        bool UpdateBoss(Enemy e, float dt, float dist, Vector2 dir, Vector2 target, ref Vector2 moveDir, ref float speed, Room room)
        {
            // 体力が半分を切ったら怒る：速くなり、予告と間隔が短くなる
            if (!e.Enraged && e.Hp < e.MaxHp * 0.5f)
            {
                e.Enraged = true;
                e.Speed *= GameConfig.BossEnragedSpeedMult;
                e.Tint = new Color(1f, 0.6f, 0.6f);
                e.Sr.color = e.Tint;
                BossEnraged?.Invoke();
            }

            if (e.Charging > 0f)
            {
                e.Charging -= dt;
                moveDir = e.ChargeDir;
                speed = BossInfo.ChargeSpeed;
                return false;
            }

            if (e.Windup > 0f)
            {
                e.Windup -= dt;
                e.Sr.color = Mathf.Repeat(Time.time * 8f, 1f) < 0.5f ? Color.white : e.Tint; // 予告の間は点滅
                if (e.Windup <= 0f)
                {
                    e.Sr.color = e.Tint;
                    Resolve(e, target, room);
                    e.SlamTimer = (e.Enraged ? BossInfo.EnragedInterval : BossInfo.Interval) * (BossInfo.IsAbyss(e.BossKind) ? 0.8f : 1f); // 深淵のボスは手数が多い
                }
                return true;
            }

            // 書庫の司書：距離を取りながら撃つ。近づかれたら、離れた場所へ瞬間移動（待ち時間つき）
            e.BlinkCooldown -= dt;
            if (BossInfo.KeepsAway(e.BossKind))
            {
                if (dist < 3f && e.BlinkCooldown <= 0f)
                {
                    e.BlinkCooldown = BossInfo.BlinkCooldown;
                    Begin(e, BossAttack.Blink, dir, room);
                    return true;
                }
                if (dist < BossInfo.KeepAway) moveDir = -dir;
            }

            e.SlamTimer -= dt;
            if (e.SlamTimer <= 0f)
            {
                var atk = BossInfo.Next(e);
                if (BossInfo.InRange(atk, dist))
                {
                    e.AttackStep++;
                    Begin(e, atk, dir, room);
                    return true;
                }
            }
            return false;
        }

        void Begin(Enemy e, BossAttack atk, Vector2 dir, Room room)
        {
            e.Attack = atk;
            e.AttackDir = dir.sqrMagnitude > 0.0001f ? dir : Vector2.down;
            e.Windup = BossInfo.Windup(atk) * (e.Enraged ? 0.8f : 1f);
            if (atk == BossAttack.RoomWide)
            {
                // 安全な円：ボスの部屋の中の、ばらけた場所
                e.SafeSpots.Clear();
                for (int k = 0; k < BossInfo.SafeSpots; k++)
                    e.SafeSpots.Add(e.Home != null ? room.RandomPointCentral(e.Home, 0.2f) : e.Pos + UnityEngine.Random.insideUnitCircle * 5f);
            }
            BossTelegraph?.Invoke(e, atk, e.Windup);
        }

        void Resolve(Enemy e, Vector2 target, Room room)
        {
            Vector2 toPlayer = target - e.Pos;
            Vector2 aim = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : e.AttackDir;
            int bulletDamage = Mathf.Max(1, Mathf.RoundToInt(e.ContactDamage * 0.5f));
            var debuff = BossInfo.DebuffOf(e.BossKind, e.Attack);
            fireAcc = e.Acc;
            switch (e.Attack)
            {
                case BossAttack.Charge:
                    e.Charging = BossInfo.ChargeTime;
                    e.ChargeDir = aim;
                    break;
                case BossAttack.BulletRing:
                    int ring = BossInfo.IsAbyss(e.BossKind) ? 24 : 16; // 深淵のボスは弾が多い
                    float spin = UnityEngine.Random.value * Mathf.PI * 2f / ring;
                    for (int k = 0; k < ring; k++)
                    {
                        float a = spin + k * Mathf.PI * 2f / ring;
                        Fire(e.Pos, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * GameConfig.BulletSpeed * 0.8f, bulletDamage, debuff);
                    }
                    break;
                case BossAttack.BulletFan:
                    int half = BossInfo.IsAbyss(e.BossKind) ? 5 : 3;
                    for (int k = -half; k <= half; k++)
                    {
                        float a = Mathf.Atan2(aim.y, aim.x) + k * 10f * Mathf.Deg2Rad;
                        Fire(e.Pos, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * GameConfig.BulletSpeed, bulletDamage, debuff);
                    }
                    break;
                case BossAttack.Summon:
                    for (int k = 0; k < 3; k++)
                    {
                        int before = Active.Count;
                        Vector2 p = room.Clamp(e.Pos + UnityEngine.Random.insideUnitCircle.normalized * 2f, GameConfig.SlimeRadius);
                        Spawn(p, GameConfig.EnemyHp(FloorTier), GameConfig.EnemyDamage(FloorTier), EnemyKind.Slime, 0);
                        if (Active.Count > before) Active[Active.Count - 1].Home = e.Home;
                    }
                    break;
                case BossAttack.Blink:
                    e.Pos = BlinkTarget(e, target, room);
                    break;
            }
            BossStrike?.Invoke(e, e.Attack);
        }

        /// <summary>瞬間移動の行き先。司書はプレイヤーから一番遠い場所、虚ろの影はプレイヤーのすぐそば。</summary>
        Vector2 BlinkTarget(Enemy e, Vector2 target, Room room)
        {
            if (BossInfo.KeepsAway(e.BossKind) && e.Home != null)
            {
                Vector2 best = e.Pos;
                float bestD = -1f;
                for (int k = 0; k < 8; k++)
                {
                    var p = room.RandomPointCentral(e.Home, 0.15f);
                    float d = (p - target).sqrMagnitude;
                    if (d > bestD) { bestD = d; best = p; }
                }
                return best;
            }
            float a = UnityEngine.Random.value * Mathf.PI * 2f;
            return room.Clamp(target + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 2.5f, e.Radius);
        }
    }
}
