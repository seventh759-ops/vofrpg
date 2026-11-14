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

        public void Begin(int floor, Room room)
        {
            Floor = floor;
            CurrentArea = null;
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
                    bool final = Floor == GameConfig.AbyssLastFloor; // 深淵の最奥（アビス）は、ほかのボスより強い
                    enemies.SpawnBoss(area.Center, GameConfig.EnemyHp(Tier) * GameConfig.BossHpMult * (final ? GameConfig.FinalBossHpMult : 1f),
                        Mathf.RoundToInt(GameConfig.EnemyDamage(Tier) * GameConfig.BossDamageMult * (final ? GameConfig.FinalBossDamageMult : 1f)));
                    Sleep(enemies, area);
                    count /= 2; // お供
                }
                for (int i = 0; i < count; i++)
                {
                    int before = enemies.Active.Count;
                    enemies.SpawnMixed(room.RandomPointCentral(area), GameConfig.EnemyHp(Tier), GameConfig.EnemyDamage(Tier), GameConfig.RollEnemyTier(Floor));
                    if (enemies.Active.Count == before) return; // 上限
                    if (Random.value < GameConfig.EliteChance(Tier))
                        enemies.MakeElite(enemies.Active[enemies.Active.Count - 1], EnemySystem.RandomElite());
                    Sleep(enemies, area);
                }
            }
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
            foreach (var area in room.Areas)
            {
                if (area.Activated || area.IsStart || !area.Contains(player.Pos, -1f)) continue;
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
