using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// M1 の遊び場全体をまとめる。部屋・プレイヤー・敵・火の玉・HUD をコードから組み立て、
    /// 毎フレームの処理順（入力 → プレイヤー → 敵の波 → 敵 → 火の玉 → カメラ）を決める。
    /// </summary>
    public sealed class GameWorld : MonoBehaviour
    {
        public Room Room { get; private set; }
        public Player Player { get; private set; }
        public EnemySystem Enemies { get; private set; }
        public ProjectileSystem Shots { get; private set; }
        public PlayerInputReader Input { get; private set; }
        public WaveDirector Waves { get; private set; }
        public Hud Hud { get; private set; }

        CameraRig cameraRig;

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

            Shots = AddChild<ProjectileSystem>("Shots");
            Shots.Init();

            Player = AddChild<Player>("Player");
            Player.Sr = Player.gameObject.AddComponent<SpriteRenderer>();
            Player.Sr.sprite = PixelArt.Player;

            Hud = gameObject.AddComponent<Hud>();
            Hud.World = this;

            Input = new PlayerInputReader
            {
                IsReserved = p => Hud.IsReserved(p),
                IsAttackArea = p => !AutoAttack && Hud.AttackRect.Contains(p),
            };
            Input.Tapped += OnTapped;

            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
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
            float dt = Mathf.Min(Time.deltaTime, 1f / 20f);
            Input.Poll();

            if (Input.ToggleModePressed) AutoAttack = !AutoAttack;

            if (Player.IsDead)
            {
                if (Input.ConfirmPressed) Restart();
            }
            else
            {
                Player.Tick(dt, Input, Room, Enemies, Shots);
                Waves.Tick(dt, Enemies, Player, Room);
                Enemies.Tick(dt, Player, Room);
                Shots.Tick(dt, Enemies, Room);
            }

            cameraRig.Tick(Player.Pos, Room, dt);
        }

        void OnTapped(Vector2 startPos)
        {
            if (Player.IsDead)
            {
                Restart();
                return;
            }
            if (Hud.ToggleRect.Contains(startPos)) AutoAttack = !AutoAttack;
        }

        void Restart()
        {
            Enemies.Clear();
            Enemies.Kills = 0;
            Shots.Clear();
            Player.ResetState();
            Waves.Reset();
            Input.ClearQueued();
        }
    }
}
