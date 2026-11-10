using System.Collections.Generic;
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
        public AttackSystem Attack { get; private set; }
        public SkillSystem Skills { get; private set; }
        public LootSystem Loot { get; private set; }
        public FxSystem Fx { get; private set; }
        public Sfx Sfx { get; private set; }
        public Music Music { get; private set; }
        public Lighting Lighting { get; private set; }
        public Inventory Inventory { get; private set; }
        public PlayerInputReader Input { get; private set; }
        public FloorDirector Floors { get; private set; }
        public Hud Hud { get; private set; }

        /// <summary>鞄を開いている間はゲームを止める。</summary>
        public bool BagOpen { get; set; }

        /// <summary>能力値の画面を開いている間もゲームを止める。</summary>
        public bool StatsOpen { get; set; }

        /// <summary>スキルの画面を開いている間もゲームを止める。</summary>
        public bool SkillsOpen { get; set; }

        /// <summary>階段の前の「帰還する／下りる」の画面。</summary>
        public bool ReturnOpen { get; private set; }

        public bool Paused => BagOpen || StatsOpen || SkillsOpen || ReturnOpen;

        /// <summary>職業を選ぶ画面（挑戦の開始前）。</summary>
        public bool ChoosingClass { get; private set; }

        CameraRig cameraRig;
        Vector2 lastKillPos;
        bool roomRewardPending;
        bool stairsLatch;     // 階段の画面を閉じたら、一度離れるまで出さない
        SpriteRenderer stairs;
        float slowMoTimer;
        bool exploding;
        float lifeStealPool; // 与えたダメージから回復する分の端数

        public bool AutoAttack
        {
            get => Player.AutoAttack;
            set => Player.AutoAttack = value;
        }

        void Awake()
        {
            Application.targetFrameRate = 60;

            Room = AddChild<Room>("Room");
            var stairsGo = new GameObject("Stairs");
            stairsGo.transform.SetParent(transform, false);
            stairs = stairsGo.AddComponent<SpriteRenderer>();
            stairs.sprite = PixelArt.Stairs;
            Lighting.MakeGlow(stairs);

            Enemies = AddChild<EnemySystem>("Enemies");
            Enemies.Init();
            Enemies.Damaged += OnEnemyDamaged;
            Enemies.Killed += OnEnemyKilled;
            Enemies.BossKilled += OnBossKilled;
            Enemies.BossWindup += (pos, radius, time) => Fx.Ring(pos, radius, new Color(1f, 0.2f, 0.2f, 0.7f), time);
            Enemies.BossSlam += OnBossSlam;
            Enemies.BossEnraged += () =>
            {
                Hud.Toast("The boss is enraged!", new Color(1f, 0.4f, 0.3f));
                cameraRig.Shake(0.2f, 0.4f);
            };
            Enemies.EliteKilled += OnEliteKilled;

            Shots = AddChild<ProjectileSystem>("Shots");
            Shots.Init();

            Loot = AddChild<LootSystem>("Loot");
            Inventory = new Inventory();
            Loot.Init(Inventory);
            Loot.Landed += OnLootLanded;
            Loot.PickedUp += OnItemPickedUp;
            Loot.EmbersPickedUp += _ => Sfx.Ember();

            Fx = AddChild<FxSystem>("Fx");
            Attack = AddChild<AttackSystem>("Attack");
            Attack.Init(Enemies, Shots, Fx);
            Skills = AddChild<SkillSystem>("Skills");
            Skills.SkillLeveled += (name, lv) =>
            {
                Hud.Toast($"{name} Lv {lv}", new Color(0.6f, 0.85f, 1f));
                Profile.Save();
            };
            Skills.SkillLearned += name => Hud.Toast($"New skill: {name}", new Color(0.6f, 0.85f, 1f));
            Sfx = gameObject.AddComponent<Sfx>();
            Music = gameObject.AddComponent<Music>();
            Lighting = AddChild<Lighting>("Lighting");
            Lighting.Init();
            Skills.Init(Enemies, Shots, Fx, Sfx, Room);

            Player = AddChild<Player>("Player");
            Player.Sr = Player.gameObject.AddComponent<SpriteRenderer>();
            Player.Sr.sprite = PixelArt.Player;
            Player.Dodged += OnPlayerDodged;

            Inventory.Changed += () => Player.ApplyStats(PlayerStats.From(Inventory.Equipped));

            Hud = gameObject.AddComponent<Hud>();
            Hud.World = this;

            Input = new PlayerInputReader
            {
                IsReserved = p => Paused || ChoosingClass || Hud.IsOverButton(p),
                IsAttackArea = p => !Paused && !AutoAttack && Hud.AttackRect.Contains(p),
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

            Floors = new FloorDirector();
            Floors.RoomActivated += () => roomRewardPending = true;
            StartRun(ClassType.Lampwright);
            ShowClassSelect();
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
            if (Input.MusicPressed) Music.ToggleMute();
            if (Input.HelpPressed) Hud.ShowHelp = !Hud.ShowHelp;
            Music.SetBoss(!ChoosingClass && !Player.IsDead && Enemies.Boss != null);

            if (ChoosingClass)
            {
                if (Input.NumberPressed >= 0 && Input.NumberPressed < ClassInfo.Count) StartRun((ClassType)Input.NumberPressed);
                cameraRig.Tick(Player.Pos, Room, dt);
                return;
            }

            if (Input.ToggleModePressed) AutoAttack = !AutoAttack;
            if (Input.BagPressed && !Player.IsDead) OpenPanel(!BagOpen, false, false);
            if (Input.StatsPressed && !Player.IsDead) OpenPanel(false, !StatsOpen, false);
            if (Input.SkillsPressed && !Player.IsDead) OpenPanel(false, false, !SkillsOpen);
            if (Input.CancelPressed) OpenPanel(false, false, false);

            if (Player.IsDead)
            {
                if (Input.ConfirmPressed)
                {
                    Profile.Save();
                    ShowClassSelect();
                }
            }
            else if (!Paused)
            {
                Player.Tick(dt, Input, Room);
                Attack.Tick(dt, Player, AutoAttack || Input.AttackHeld);
                Skills.Tick(dt, Player, Input.NumberPressed);
                Floors.Tick(dt, Enemies, Player, Room);
                Room.UpdateFlow(Player.Pos);
                Room.Reveal(Player.Pos, Player.EffectiveLight + 2f);
                Room.ApplyMinimap();
                Enemies.Tick(dt, Player, Room);
                Shots.Tick(dt, Enemies, Room);
                Loot.Tick(dt, Player, Inventory);
                Fx.Tick(dt);
                CheckRoomCleared();
                CheckStairs();
            }

            Player.LightPenalty = Enemies.ShroudNear ? GameConfig.ShroudPenalty : 0f;
            Lighting.Tick(Player.Pos, Player.EffectiveLight, dt);
            cameraRig.Tick(Player.Pos, Room, dt);
        }

        /// <summary>精鋭を倒した：小さなドロップの噴水。</summary>
        void OnEliteKilled(Vector2 pos)
        {
            Loot.Fountain(pos, Room, GameConfig.EliteFountainItems, DropLevel, GameConfig.EliteRarityBonus,
                4, GameConfig.EmberAmount(Floors.Tier));
            Fx.Ring(pos, 1.6f, new Color(1f, 0.85f, 0.4f, 0.9f), 0.35f);
            Sfx.Fountain();
            GainExp(Progression.KillExp(Floors.Tier) * 5);
        }

        /// <summary>部屋の敵を全滅させたら、最後に倒した場所からドロップの噴水。</summary>
        void CheckRoomCleared()
        {
            if (!roomRewardPending || !Floors.SpawningDone || Enemies.Active.Count > 0) return;
            roomRewardPending = false;
            int tier = Floors.Tier;
            Loot.Fountain(lastKillPos, Room, Mathf.Max(1, GameConfig.FountainItems(tier) / 2), DropLevel,
                GameConfig.FountainRarityBonus, GameConfig.FountainEmberOrbs(tier) / 2, GameConfig.EmberAmount(tier));
            Fx.Ring(lastKillPos, 2.5f, new Color(1f, 0.85f, 0.4f, 0.9f), 0.4f);
            Sfx.Fountain();
            cameraRig.Shake(0.12f, 0.25f);
            Loot.VacuumAfter(GameConfig.VacuumDelay);
        }

        /// <summary>階段に乗ったら「帰還する／下りる」を聞く。</summary>
        void CheckStairs()
        {
            stairs.enabled = Floors.StairsOpen;
            if (!Floors.StairsOpen) return;
            float d = (Player.Pos - Room.StairsPos).magnitude;
            if (d > 1.5f) stairsLatch = false;
            if (stairsLatch || d > GameConfig.StairsRadius) return;
            stairsLatch = true;
            ReturnOpen = true;
        }

        /// <summary>次の層へ。地面に残ったドロップは拾える分だけ拾ってから下りる。</summary>
        public void Descend()
        {
            Loot.CollectAll(Inventory);
            ReturnOpen = false;
            EnterFloor(Floors.Floor + 1);
            Hud.Toast($"FLOOR {Floors.Floor}" + (Floors.IsBossFloor ? "  -  BOSS" : ""), new Color(1f, 0.85f, 0.5f));
        }

        /// <summary>階段の画面を閉じて、この層に残る。</summary>
        public void StayOnFloor()
        {
            ReturnOpen = false;
        }

        void EnterFloor(int floor)
        {
            Enemies.Clear();
            Shots.Clear();
            Loot.Clear();
            Fx.Clear();
            roomRewardPending = false;
            Floors.Begin(floor, Room);
            Player.Pos = Room.StartPos;
            stairs.transform.position = Iso.ToScreen(Room.StairsPos);
            stairs.sortingOrder = SortOrder.Floor + 2;
            stairs.enabled = Floors.StairsOpen;
            stairsLatch = false;

            var ch = Profile.Current;
            if (ch != null && floor > ch.BestFloor)
            {
                ch.BestFloor = floor;
                Profile.Save();
            }
        }

        void OnEnemyDamaged(Vector2 pos, float amount, bool crit)
        {
            Hud.AddNumber(pos, amount, crit);
            Sfx.Hit();

            float steal = Player.Stats.LifeSteal;
            if (steal > 0f && !Player.IsDead)
            {
                lifeStealPool += amount * steal;
                if (lifeStealPool >= 1f)
                {
                    int whole = Mathf.FloorToInt(lifeStealPool);
                    lifeStealPool -= whole;
                    Player.Heal(whole);
                }
            }
        }

        /// <summary>ボスの叩きつけ。輪の内側にいて、回避の無敵中でなければ当たる。</summary>
        void OnBossSlam(Vector2 pos, float radius, int damage)
        {
            Fx.Ring(pos, radius, new Color(1f, 0.9f, 0.8f, 0.9f), 0.2f);
            Sfx.Boom();
            cameraRig.Shake(0.2f, 0.3f);
            if ((Player.Pos - pos).sqrMagnitude <= radius * radius) Player.TakeHit(damage);
        }

        /// <summary>ボスを倒した：大きな噴水と、低い確率でボス専用の Mythic 装備。</summary>
        void OnBossKilled(Vector2 pos)
        {
            Loot.Fountain(pos, Room, GameConfig.BossFountainItems, DropLevel, GameConfig.BossRarityBonus,
                GameConfig.FountainEmberOrbs(Floors.Tier), GameConfig.EmberAmount(Floors.Tier) * 3);
            if (Random.value < GameConfig.MythicChance) Loot.DropItem(pos, ItemGenerator.RollMythic(DropLevel), Room, 1.2f);
            Fx.Ring(pos, 3f, new Color(1f, 0.4f, 0.3f, 0.9f), 0.6f);
            cameraRig.Shake(0.3f, 0.5f);
            Hud.Toast("BOSS DEFEATED!", new Color(1f, 0.5f, 0.4f));
            GainExp(Progression.KillExp(Floors.Tier) * 30);
            Floors.OpenStairs();
            Hud.Toast("The stairs have appeared", new Color(1f, 0.85f, 0.5f));
        }

        /// <summary>ボス装備の固有効果：回避した場所で炎が弾ける。</summary>
        void OnPlayerDodged(Vector2 pos)
        {
            var st = Player.Stats;
            if (st == null || st.DodgeNovaMult <= 0f) return;
            const float radius = 1.8f;
            Fx.Ring(pos, radius, new Color(0.5f, 0.8f, 1f, 0.9f), 0.3f);
            Enemies.DamageArea(pos, radius, st.Damage * st.DodgeNovaMult);
        }

        void OnEnemyKilled(Vector2 pos)
        {
            lastKillPos = pos;
            GainExp(Progression.KillExp(Floors.Tier));
            var stats = Player.Stats;

            if (Random.value < GameConfig.ItemDropChance) Loot.DropItem(pos, ItemGenerator.Roll(DropLevel, 0f), Room);
            if (Random.value < GameConfig.EmberDropChance) Loot.DropEmbers(pos, GameConfig.EmberAmount(Floors.Tier), Room, 0.6f);
            if (stats.HealOnKill > 0) Player.Heal(stats.HealOnKill);

            // 爆発で倒した敵は、さらに爆発しない（連鎖で群れ全体が一度に消えるのを防ぐ）
            if (stats.ExplodeMult > 0f && !exploding)
            {
                const float radius = 1.4f;
                Fx.Ring(pos, radius, new Color(1f, 0.5f, 0.15f, 0.9f));
                Sfx.Boom();
                exploding = true;
                Enemies.DamageArea(pos, radius, stats.Damage * stats.ExplodeMult);
                exploding = false;
            }
        }

        /// <summary>鞄・能力値・スキルの画面のどれか1つだけを開く（全部 false で閉じる）。</summary>
        public void OpenPanel(bool bag, bool stats, bool skills)
        {
            BagOpen = bag;
            StatsOpen = stats;
            SkillsOpen = skills;
        }

        void GainExp(int amount)
        {
            var ch = Profile.Current;
            int oldLevel = ch.Level;
            int gained = Progression.AddExp(ch, amount);
            if (gained <= 0) return;
            Skills.OnLevelChanged(ch, oldLevel);

            RefreshStats();
            Player.Refill();
            Fx.Ring(Player.Pos, 2f, new Color(1f, 0.95f, 0.6f, 0.9f), 0.5f);
            Sfx.Landed(Rarity.Legendary);
            Hud.Toast($"LEVEL UP!  Lv {ch.Level}  (+{gained * Progression.PointsPerLevel} points)", new Color(1f, 0.95f, 0.5f));
            Profile.Save();
        }

        /// <summary>能力値に1ポイント振る。</summary>
        public void Allocate(Attr attr)
        {
            var ch = Profile.Current;
            if (ch == null || ch.Points <= 0) return;
            ch.Points--;
            ch.Alloc[(int)attr]++;
            RefreshStats();
            Profile.Save();
        }

        /// <summary>振ったポイントを全部戻す（振り直し）。</summary>
        public void ResetPoints()
        {
            var ch = Profile.Current;
            if (ch == null) return;
            for (int i = 0; i < ch.Alloc.Length; i++)
            {
                ch.Points += ch.Alloc[i];
                ch.Alloc[i] = 0;
            }
            RefreshStats();
            Profile.Save();
            Hud.Toast("Points reset", Color.white);
        }

        /// <summary>
        /// 今の職業を完全に最初からやり直す：レベル・能力値・スキルの熟練度・持ち越した装備と倉庫・火種・最深層をすべて消す。
        /// </summary>
        public void ResetLevel()
        {
            Profile.ResetCharacter(ClassInfo.Current);
            Profile.Save();
            ShowClassSelect();
        }

        /// <summary>装備できるか（必要レベルと必要能力値）。できなければ理由を返す。</summary>
        public bool CanEquip(Item item, out string reason)
        {
            var st = Player.Stats;
            if (st.Level < item.ReqLevel)
            {
                reason = $"Need Lv {item.ReqLevel}";
                return false;
            }
            if (item.ReqValue > 0 && st.Attr[(int)item.ReqAttr] < item.ReqValue)
            {
                reason = $"Need {Attributes.ShortName(item.ReqAttr)} {item.ReqValue}";
                return false;
            }
            reason = null;
            return true;
        }

        int DropLevel => GameConfig.DropLevel(Floors.Tier, Profile.Current != null ? Profile.Current.Level : 1);

        void RefreshStats()
        {
            Player.ApplyStats(PlayerStats.From(Inventory.Equipped));
        }

        void OnLootLanded(Item item)
        {
            if (item == null || item.Rarity < Rarity.Rare) return;
            Sfx.Landed(item.Rarity);
            if (item.Rarity == Rarity.Legendary)
            {
                cameraRig.Shake(0.15f, 0.3f);
            }
            else if (item.Rarity >= Rarity.Relic)
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
            if (Player.IsDead && !ChoosingClass)
            {
                Profile.Save();
                ShowClassSelect();
                return;
            }
            Hud.HandleTap(startPos);
        }

        /// <summary>職業を選ぶ画面を出す。選ぶと StartRun が呼ばれる。</summary>
        public void ShowClassSelect()
        {
            ClearField();
            ChoosingClass = true;
        }

        /// <summary>選んだ職業で挑戦を始める。</summary>
        public void StartRun(ClassType type)
        {
            ClassInfo.Current = type;
            Profile.Select(type);
            ChoosingClass = false;
            ClearField();
            Player.SetClass(type);
            Inventory.Reset(type, Profile.Current);
            Player.ResetState(PlayerStats.From(Inventory.Equipped, type));
            EnterFloor(1);
            Input.ClearQueued();
        }

        /// <summary>帰還する：装備と選んだ品と火種を保存して、職業選択に戻る。</summary>
        public void ReturnHome(List<Item> chosen)
        {
            var ch = Profile.Current;
            if (ch == null) return;
            if (chosen.Count > ch.CarrySlots) chosen = chosen.GetRange(0, ch.CarrySlots);
            Inventory.SaveForReturn(ch, chosen);
            Profile.Save();
            ShowClassSelect();
            Hud.Toast($"Returned home with {chosen.Count} new items", new Color(0.7f, 1f, 0.7f));
        }

        void ClearField()
        {
            ReturnOpen = false;
            Time.timeScale = 1f;
            slowMoTimer = 0f;
            OpenPanel(false, false, false);
            roomRewardPending = false;
            lifeStealPool = 0f;
            Enemies.Clear();
            Enemies.Kills = 0;
            Shots.Clear();
            Loot.Clear();
            Fx.Clear();
            Attack.ResetState();
            Skills.ResetState();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Profile.Save(); // スマホでアプリが裏に回ったとき
        }

        void OnApplicationQuit()
        {
            Profile.Save();
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}
