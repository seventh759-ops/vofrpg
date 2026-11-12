using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 塔の層の進行。部屋に入ると、その部屋に敵が出る（はじめの部屋は安全）。
    /// 普通の層は最初から階段が使える。5層ごとのボスの層は、ボスを倒すと階段が現れる。
    /// </summary>
    public sealed class FloorDirector
    {
        const float SpawnPerSecond = 30f;

        readonly Queue<Vector2> pending = new Queue<Vector2>();
        float spawnTimer;

        public int Floor { get; private set; }
        public bool IsBossFloor => GameConfig.IsBossFloor(Floor);
        /// <summary>敵の強さ・経験値・ドロップの尺度。</summary>
        public int Tier => GameConfig.Tier(Floor);
        public bool StairsOpen { get; private set; }
        public bool SpawningDone => pending.Count == 0;

        /// <summary>部屋の敵が出た（ドロップの噴水の判定に使う）。</summary>
        public event System.Action RoomActivated;

        public void Begin(int floor, Room room)
        {
            Floor = floor;
            pending.Clear();
            spawnTimer = 0f;
            room.Generate(floor, IsBossFloor, Random.Range(int.MinValue, int.MaxValue));
            StairsOpen = !IsBossFloor;
        }

        public void OpenStairs()
        {
            StairsOpen = true;
        }

        public void Tick(float dt, EnemySystem enemies, Player player, Room room)
        {
            var area = room.AreaAt(player.Pos);
            if (area != null && !area.Activated && !area.IsStart) Activate(area, enemies, player, room);

            if (pending.Count == 0) return;
            spawnTimer -= dt;
            while (spawnTimer <= 0f && pending.Count > 0)
            {
                Vector2 p = pending.Dequeue();
                int before = enemies.Active.Count;
                enemies.SpawnMixed(p, GameConfig.EnemyHp(Tier), GameConfig.EnemyDamage(Tier));
                if (enemies.Active.Count > before && Random.value < GameConfig.EliteChance(Tier))
                    enemies.MakeElite(enemies.Active[enemies.Active.Count - 1], EnemySystem.RandomElite());
                spawnTimer += 1f / SpawnPerSecond;
            }
        }

        void Activate(RoomArea area, EnemySystem enemies, Player player, Room room)
        {
            area.Activated = true;
            int count = GameConfig.RoomEnemies(Floor, area.W * area.H);
            if (area.IsBoss)
            {
                enemies.SpawnBoss(area.Center, GameConfig.EnemyHp(Tier) * GameConfig.BossHpMult,
                    Mathf.RoundToInt(GameConfig.EnemyDamage(Tier) * GameConfig.BossDamageMult));
                count /= 2; // お供
            }
            for (int i = 0; i < count; i++) pending.Enqueue(room.RandomPointIn(area, player.Pos, 3f));
            spawnTimer = 0.3f; // 部屋に入ってから少しだけ間を置く
            RoomActivated?.Invoke();
        }
    }
}
