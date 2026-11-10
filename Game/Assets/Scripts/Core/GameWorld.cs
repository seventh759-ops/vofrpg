using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 試作の遊び場全体をまとめる。部屋・プレイヤー・敵・火の玉・ドロップ・HUD をコードから組み立て、
    /// 毎フレームの処理順（入力 → プレイヤー → 敵の波 → 敵 → 火の玉 → ドロップ → 演出 → カメラ）を決める。
    /// </summary>
    public sealed class GameWorld : MonoBehaviour
    {
        public Room Room { get; private set; }
        public Player Player { get; private set; }
        public EnemySystem Enemies { get; private set; }
        public ProjectileSystem Shots { get; private set; }
        public LootSystem Loot { get; private set; }
        public FxSystem Fx { get; private set; }
        public Sfx Sfx { get; private set; }
        public Inventory Inventory { get; private set; }
        public PlayerInputReader Input { get; private set; }
        public WaveDirector Waves { get; private set; }
        public Hud Hud { get; private set; }

        /// <summary>鞄を開いている間はゲームを止める。</summary>
        public bool BagOpen { get; set; }

        CameraRig cameraRig;
        Vector2 lastKillPos;
        int rewardedWave;
        float slowMoTimer;

        public bool AutoAttack
        {
            get => Player.AutoAttack;
            set => Player.AutoAttack = value;
        }

        void Awake()
        {
            Application.targetFrameRate = 60;

            Room = AddChild<Room>("Room");
            Room.Build(GameConfig.RoomWidth, GameConfig.RoomHeight);

            Enemies = AddChild<EnemySystem>("Enemies");
            Enemies.Init();
            Enemies.Damaged += OnEnemyDamaged;
            Enemies.Killed += OnEnemyKilled;

            Shots = AddChild<ProjectileSystem>("Shots");
            Shots.Init();

            Loot = AddChild<LootSystem>("Loot");
            Loot.Init();
            Loot.Landed += OnLootLanded;
            Loot.PickedUp += OnItemPickedUp;
            Loot.EmbersPickedUp += _ => Sfx.Ember();

            Fx = AddChild<FxSystem>("Fx");
            Sfx = gameObject.AddComponent<Sfx>();

            Player = AddChild<Player>("Player");
            Player.Sr = Player.gameObject.AddComponent<SpriteRenderer>();
            Player.Sr.sprite = PixelArt.Player;

            Inventory = new Inventory();
            Inventory.Changed += () => Player.ApplyStats(PlayerStats.From(Inventory.Equipped));

            Hud = gameObject.AddComponent<Hud>();
            Hud.World = this;

            Input = new PlayerInputReader
            {
                IsReserved = p => BagOpen || Hud.IsOverButton(p),
                IsAttackArea = p => !BagOpen && !AutoAttack && Hud.AttackRect.Contains(p),
            };
            Input.Tapped += OnTapped;

            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cameraRig = new CameraRig(cam);

            Waves = new WaveDirector();
            Restart();
        }

        T AddChild<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go.AddComponent<T>();
        }

        void Update()
        {
            UpdateSlowMo();
            float dt = Mathf.Min(Time.deltaTime, 1f / 20f);
            Input.Poll();

            if (Input.ToggleModePressed) AutoAttack = !AutoAttack;
            if (Input.BagPressed && !Player.IsDead) BagOpen = !BagOpen;
            if (Input.CancelPressed) BagOpen = false;

            if (Player.IsDead)
            {
                if (Input.ConfirmPressed) Restart();
            }
            else if (!BagOpen)
            {
                Player.Tick(dt, Input, Room, Enemies, Shots);
                Waves.Tick(dt, Enemies, Player, Room);
                Enemies.Tick(dt, Player, Room);
                Shots.Tick(dt, Enemies, Room);
                Loot.Tick(dt, Player, Inventory);
                Fx.Tick(dt);
                CheckWaveCleared();
            }

            cameraRig.Tick(Player.Pos, Room, dt);
        }

        /// <summary>波を全滅させたら、最後に倒した場所からドロップの噴水。</summary>
        void CheckWaveCleared()
        {
            if (Waves.Wave <= rewardedWave || !Waves.SpawningDone || Enemies.Active.Count > 0) return;
            rewardedWave = Waves.Wave;
            Loot.Fountain(lastKillPos, Room, GameConfig.FountainItems(Waves.Wave), Waves.Wave,
                GameConfig.FountainRarityBonus, GameConfig.FountainEmbers(Waves.Wave));
            Fx.Ring(lastKillPos, 2.5f, new Color(1f, 0.85f, 0.4f, 0.9f), 0.4f);
            Sfx.Fountain();
            cameraRig.Shake(0.12f, 0.25f);
        }

        void OnEnemyDamaged(Vector2 pos, float amount, bool crit)
        {
            Hud.AddNumber(pos, amount, crit);
            Sfx.Hit();
        }

        void OnEnemyKilled(Vector2 pos)
        {
            lastKillPos = pos;
            var stats = Player.Stats;

            if (Random.value < GameConfig.ItemDropChance) Loot.DropItem(pos, ItemGenerator.Roll(Waves.Wave, 0f), Room);
            if (Random.value < GameConfig.EmberDropChance) Loot.DropEmbers(pos, 1, Room, 0.6f);
            if (stats.HealOnKill > 0) Player.Heal(stats.HealOnKill);

            if (stats.ExplodeMult > 0f)
            {
                const float radius = 1.4f;
                Fx.Ring(pos, radius, new Color(1f, 0.5f, 0.15f, 0.9f));
                Sfx.Boom();
                Enemies.DamageArea(pos, radius, stats.Damage * stats.ExplodeMult);
            }
        }

        void OnLootLanded(Item item)
        {
            if (item == null || item.Rarity < Rarity.Rare) return;
            Sfx.Landed(item.Rarity);
            if (item.Rarity == Rarity.Legendary)
            {
                cameraRig.Shake(0.15f, 0.3f);
            }
            else if (item.Rarity == Rarity.Relic)
            {
                cameraRig.Shake(0.25f, 0.5f);
                slowMoTimer = 0.6f; // 一瞬スローにする
                Time.timeScale = 0.3f;
            }
        }

        void OnItemPickedUp(Item item)
        {
            Sfx.Pickup();
            Hud.Toast(item.Name, RarityInfo.ColorOf(item.Rarity));
        }

        void UpdateSlowMo()
        {
            if (slowMoTimer <= 0f) return;
            slowMoTimer -= Time.unscaledDeltaTime;
            if (slowMoTimer <= 0f) Time.timeScale = 1f;
        }

        void OnTapped(Vector2 startPos)
        {
            if (Player.IsDead)
            {
                Restart();
                return;
            }
            Hud.HandleTap(startPos);
        }

        void Restart()
        {
            Time.timeScale = 1f;
            slowMoTimer = 0f;
            BagOpen = false;
            rewardedWave = 0;
            Enemies.Clear();
            Enemies.Kills = 0;
            Shots.Clear();
            Loot.Clear();
            Fx.Clear();
            Inventory.Reset();
            Player.ResetState(PlayerStats.From(Inventory.Equipped));
            Waves.Reset();
            Input.ClearQueued();
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}
