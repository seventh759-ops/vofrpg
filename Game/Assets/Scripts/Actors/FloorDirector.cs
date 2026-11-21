using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 塔の層の進行。部屋に入ると、その部屋に敵が出る（はじめの部屋は安全）。
    /// 普通の層は、階段のある部屋の敵をすべて倒すと階段の封印が解けて通れる（黄金のスライムは数えない）。5層ごとのボスの層は、ボスを倒すと階段が現れる。
    /// </summary>
    public sealed class FloorDirector
    {
        public int Floor { get; private set; }
        public bool IsBossFloor => GameConfig.IsBossFloor(Floor);
        /// <summary>敵の強さ・経験値・ドロップの尺度。</summary>
        public int Tier => GameConfig.Tier(Floor);
        public bool StairsOpen { get; private set; }
        /// <summary>敵はすべて層に入った時点で置くので、出し終わるのを待つことはない。</summary>
        public bool SpawningDone => true;
        /// <summary>最後に入った（敵が起きた）部屋。片付けたかの判定に使う。</summary>
        public RoomArea CurrentArea { get; private set; }

        /// <summary>部屋に入って、敵が起きた（ドロップの噴水の判定に使う）。</summary>
        public event System.Action RoomActivated;
        /// <summary>宝の間に入った。</summary>
        public event System.Action TreasureEntered;
        /// <summary>ボスの区画の中ほどまで入って、ボスが現れた（ボスの位置）。</summary>
        public event System.Action<Vector2> BossAppeared;
        /// <summary>ボスのお供が1体現れた（位置）。はじめと、沸き直しのたび。</summary>
        public event System.Action<Vector2> MinionSpawned;

        /// <summary>まだ現れていないボスの区画（入るまでは誰もいない・暗い）。</summary>
        RoomArea bossPending;
        /// <summary>ボスと戦っている区画（お供の沸き直しに使う）。ボスが倒れたら null。</summary>
        RoomArea bossFight;
        int minionTarget;
        float minionTimer;
        /// <summary>ボスの区画に入るのを待っているか（暗くしておく）。</summary>
        public bool BossWaiting => bossPending != null;

        public void Begin(int floor, Room room)
        {
            Floor = floor;
            CurrentArea = null;
            bossPending = bossFight = null;
            room.Generate(floor, IsBossFloor, Random.Range(int.MinValue, int.MaxValue));
            StairsOpen = false; // 普通の層も、階段のある部屋の敵を全部倒すまでは通れない
        }

        /// <summary>層に入った時点で、はじめの部屋と宝の間以外のすべての部屋に、眠った敵を置く。</summary>
        public void Populate(EnemySystem enemies, Room room, bool bossDefeated = false)
        {
            // 敵の上限を部屋で分け合う（深い層で部屋が多くても、奥の部屋が空にならないように）
            int rooms = 0;
            foreach (var a in room.Areas) if (!a.IsStart && !a.IsTreasure) rooms++;
            int perRoom = rooms > 0 ? (GameConfig.MaxEnemies - 20) / rooms : 0;
            foreach (var area in room.Areas)
            {
                if (area.IsStart || area.IsTreasure) continue;
                int count = Mathf.Min(GameConfig.RoomEnemies(Floor, area.W * area.H), perRoom);
                if (area.IsBoss && bossDefeated)
                {
                    area.Activated = true; // 倒したあとに中断・再開したとき：ボスとお供は出さず、入っても「部屋を片付けた」ご褒美は出ない
                    continue;
                }
                if (area.IsBoss)
                {
                    bossPending = area; // ボスとお供は、区画の中ほどまで入ったときに出す
                    continue;
                }
                for (int i = 0; i < count; i++)
                {
                    int before = enemies.Active.Count;
                    enemies.SpawnMixed(room.RandomPointCentral(area), GameConfig.EnemyHp(Tier), GameConfig.EnemyDamage(Tier), GameConfig.RollEnemyTier(Floor));
                    if (enemies.Active.Count == before) return; // 上限
                    if (Random.value < GameConfig.EliteChance(Tier) * (1f + 0.3f * Zones.For(Floor).Cycle)) // 深淵に侵された周ほど、精鋭が出やすい
                        enemies.MakeElite(enemies.Active[enemies.Active.Count - 1], EnemySystem.RandomElite());
                    Sleep(enemies, area);
                }
            }
        }

        /// <summary>ボスの区画の中ほどまで来た：ボスとお供を、起きた状態で出す。お供は経験値・ドロップなし。</summary>
        void SpawnBossArea(EnemySystem enemies, Room room)
        {
            var area = bossPending;
            bossPending = null;
            area.Activated = true;
            CurrentArea = area;
            int rooms = 0;
            foreach (var a in room.Areas) if (!a.IsStart && !a.IsTreasure) rooms++;
            int perRoom = rooms > 0 ? (GameConfig.MaxEnemies - 20) / rooms : 0;
            minionTarget = Mathf.Min(GameConfig.RoomEnemies(Floor, area.W * area.H), perRoom) / 2; // お供
            bool goddess = GameConfig.Ascent && Floor == GameConfig.MaxFloor; // 昇る塔の最上（100層）は女神像
            bool final = Floor == GameConfig.AbyssLastFloor || goddess; // 深淵の最奥（アビス）・女神像は、ほかのボスより強い
            float bite = GameConfig.EarlyBite(Floor); // 序盤（5〜20層）のボスは少し硬く、痛い
            enemies.SpawnBoss(area.Center, GameConfig.EnemyHp(Tier) * GameConfig.BossHpMult * (final ? GameConfig.FinalBossHpMult : 1f) * (1f + GameConfig.EarlyBossHp * bite),
                Mathf.RoundToInt(GameConfig.EnemyDamage(Tier) * GameConfig.BossDamageMult * (final ? GameConfig.FinalBossDamageMult : 1f) * (1f + GameConfig.EarlyBossDamage * bite)),
                goddess ? BossKind.Goddess : (BossKind?)null);
            if (enemies.Active.Count > 0) enemies.Active[enemies.Active.Count - 1].Home = area;
            for (int i = 0; i < minionTarget; i++)
                if (!SpawnMinion(enemies, area, room.RandomPointCentral(area))) break;
            bossFight = area;
            minionTimer = GameConfig.BossMinionRespawn;
            BossAppeared?.Invoke(area.Center);
        }

        /// <summary>お供を1体出す（経験値・ドロップなし）。上限で出せなければ false。</summary>
        bool SpawnMinion(EnemySystem enemies, RoomArea area, Vector2 pos)
        {
            int before = enemies.Active.Count;
            enemies.SpawnMixed(pos, GameConfig.EnemyHp(Tier), GameConfig.EnemyDamage(Tier), GameConfig.RollEnemyTier(Floor));
            if (enemies.Active.Count == before) return false;
            var m = enemies.Active[enemies.Active.Count - 1];
            if (Random.value < GameConfig.EliteChance(Tier) * (1f + 0.3f * Zones.For(Floor).Cycle))
                enemies.MakeElite(m, EnemySystem.RandomElite());
            m.Home = area;
            m.NoReward = true;
            MinionSpawned?.Invoke(m.Pos);
            return true;
        }

        /// <summary>ボスが生きている間、一定時間ごとに、減ったお供を最初の数まで沸き直す（プレイヤーのすぐそばには出さない）。</summary>
        void TickMinions(float dt, EnemySystem enemies, Player player, Room room)
        {
            if (bossFight == null) return;
            if (enemies.Boss == null) { bossFight = null; return; } // ボスが倒れたら終わり
            minionTimer -= dt;
            if (minionTimer > 0f) return;
            minionTimer = GameConfig.BossMinionRespawn;
            int alive = 0;
            foreach (var e in enemies.Active)
                if (!e.Dying && !e.IsBoss && e.NoReward && e.Home == bossFight) alive++;
            for (int i = alive; i < minionTarget; i++)
                if (!SpawnMinion(enemies, bossFight, room.RandomPointIn(bossFight, player.Pos, 4f))) break;
        }

        static void Sleep(EnemySystem enemies, RoomArea area)
        {
            if (enemies.Active.Count == 0) return;
            var e = enemies.Active[enemies.Active.Count - 1];
            if (e.Home != null) return;
            e.Home = area;
            e.Dormant = true;
        }

        public void OpenStairs()
        {
            StairsOpen = true;
        }

        /// <summary>普通の層で、階段のある部屋に敵が残っていなければ階段の封印を解く。解いたら true。</summary>
        public bool TryUnsealStairs(EnemySystem enemies, Room room)
        {
            if (StairsOpen || IsBossFloor || enemies.RemainingIn(room.StairsArea) > 0) return false;
            StairsOpen = true;
            return true;
        }

        public void Tick(float dt, EnemySystem enemies, Player player, Room room)
        {
            // 部屋の入口（部屋の縁から1マス外まで）に来たら、その部屋の敵が起きる
            // ボスの区画：入り口では何も起きず、中ほど（縁から数マス）まで来たらボスとお供が現れる
            if (bossPending != null && bossPending.Contains(player.Pos, GameConfig.BossTriggerInset))
                SpawnBossArea(enemies, room);
            TickMinions(dt, enemies, player, room);
            foreach (var area in room.Areas)
            {
                if (area == bossPending || area.Activated || area.IsStart || !area.Contains(player.Pos, -1f)) continue;
                Activate(area, enemies, player, room);
            }
            // 念のため：眠っている敵のすぐ近く（3マス）まで来たら、その部屋ごと起こす
            foreach (var e in enemies.Active)
            {
                if (!e.Dormant) continue;
                if (e.Home == null || e.Home.Activated)
                {
                    e.Dormant = false; // 起きた部屋に眠ったまま残った敵がいれば起こす
                    continue;
                }
                if ((e.Pos - player.Pos).sqrMagnitude < 9f)
                {
                    Activate(e.Home, enemies, player, room);
                    break;
                }
            }
        }

        void Activate(RoomArea area, EnemySystem enemies, Player player, Room room)
        {
            area.Activated = true;
            if (area.IsTreasure)
            {
                TreasureEntered?.Invoke();
                return;
            }
            CurrentArea = area;
            enemies.Wake(area);
            // まれに黄金の敵（ボスの部屋には出ない）
            if (!area.IsBoss && Random.value < GameConfig.GoldenChance)
            {
                enemies.SpawnGolden(room.RandomPointIn(area, player.Pos, 4f), GameConfig.EnemyHp(Tier)); // 体力の倍率は Spawn で掛ける
                if (enemies.Active.Count > 0) enemies.Active[enemies.Active.Count - 1].Home = area;
            }
            RoomActivated?.Invoke();
        }
    }
}
