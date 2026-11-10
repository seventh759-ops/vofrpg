using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 敵の波を出す。全滅させると少し間を置いて、前より多い次の波が来る。
    /// 敵は部屋の端の、プレイヤーから離れた場所に出る。
    /// </summary>
    public sealed class WaveDirector
    {
        public int Wave { get; private set; }
        public bool SpawningDone => toSpawn == 0;

        int toSpawn;
        float spawnTimer;
        float spawnInterval;
        float gapTimer;

        public void Reset()
        {
            Wave = 0;
            toSpawn = 0;
            spawnTimer = 0f;
            gapTimer = 1f;
        }

        public void Tick(float dt, EnemySystem enemies, Player player, Room room)
        {
            if (toSpawn > 0)
            {
                spawnTimer -= dt;
                while (spawnTimer <= 0f && toSpawn > 0)
                {
                    enemies.Spawn(PickSpawn(player.Pos, room), GameConfig.EnemyHp(Wave));
                    toSpawn--;
                    spawnTimer += spawnInterval;
                }
                return;
            }

            if (enemies.Active.Count > 0) return;

            gapTimer -= dt;
            if (gapTimer > 0f) return;

            Wave++;
            toSpawn = GameConfig.WaveSize(Wave);
            spawnInterval = GameConfig.WaveSpawnDuration / toSpawn;
            spawnTimer = 0f;
            gapTimer = GameConfig.WaveGap;
        }

        static Vector2 PickSpawn(Vector2 player, Room room)
        {
            for (int i = 0; i < 10; i++)
            {
                var p = RandomEdge(room);
                if ((p - player).sqrMagnitude > 25f) return p; // 5マス以上離す
            }
            return RandomEdge(room);
        }

        static Vector2 RandomEdge(Room room)
        {
            const float margin = 0.6f;
            float x = Random.Range(-room.HalfW + margin, room.HalfW - margin);
            float y = Random.Range(-room.HalfH + margin, room.HalfH - margin);
            switch (Random.Range(0, 4))
            {
                case 0: return new Vector2(x, room.HalfH - margin);
                case 1: return new Vector2(x, -room.HalfH + margin);
                case 2: return new Vector2(-room.HalfW + margin, y);
                default: return new Vector2(room.HalfW - margin, y);
            }
        }
    }
}
