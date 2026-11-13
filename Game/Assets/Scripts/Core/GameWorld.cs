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

        /// <summary>メニューを開いている間もゲームを止める。</summary>
        public bool MenuOpen { get; private set; }

        /// <summary>祭壇の画面（加護を選ぶ）。</summary>
        public bool AltarOpen { get; private set; }
        /// <summary>この層の祭壇に並んだ加護（番号）。</summary>
        public readonly List<int> AltarOffers = new List<int>();
        /// <summary>この層の祭壇はもう使った。</summary>
        public bool AltarUsed { get; private set; }

        public bool Paused => BagOpen || StatsOpen || SkillsOpen || ReturnOpen || MenuOpen || AltarOpen || (Hud != null && Hud.Guide.Active);

        /// <summary>職業を選ぶ画面（挑戦の開始前）。</summary>
        public bool ChoosingClass { get; private set; }

        /// <summary>タイトル画面（起動したとき）。タップか Enter で職業選択へ。</summary>
        public bool OnTitle { get; private set; }

        /// <summary>タイトルから職業選択へ。</summary>
        public void LeaveTitle()
        {
            OnTitle = false;
            Input.ClearQueued();
        }

        /// <summary>町にいる（職業を選んだあと。出撃・倉庫・商人・転職・荷運び）。</summary>
        public bool InTown { get; private set; }

        /// <summary>職業を選んだあと、開始する層を選んでいるときの職業（それ以外は null）。</summary>
        public ClassType? PendingClass { get; private set; }

        /// <summary>始められる層：1層と、倒したボスの次の層（6・11・16…）。</summary>
        public static List<int> Checkpoints(CharacterData ch)
        {
            var list = new List<int> { 1 };
            int last = GameConfig.LastFloorFor(ch); // 深淵の層は、入れるときだけ（Lv100以上・100層制覇）
            for (int boss = GameConfig.BossFloorEvery; boss <= ch.BossCleared && boss + 1 <= last; boss += GameConfig.BossFloorEvery) list.Add(boss + 1);
            return list;
        }

        /// <summary>キャラクターを選んだ（枠）：そのキャラクターで町へ。空の枠なら何もしない（新しく作る画面は Hud が出す）。</summary>
        public void ChooseCharacter(int slot)
        {
            var ch = Profile.InSlot(slot);
            if (ch == null) return;
            Profile.SelectSlot(slot);
            ClassInfo.Current = (ClassType)ch.ClassId;
            InTown = true;
            PendingClass = null;
            Guide("town", Loc.T("This is the town. Get ready here before heading into the tower.", "ここは町。塔へ出る前に、ここで支度をしな。"),
                Loc.T("The storage keeper holds gear for every class. The merchant sells potions and gear. Pay with embers.", "倉庫番は全職業で使える倉庫。商人は薬と装備を売ってる。支払いは火種だよ。"));
        }

        /// <summary>空いた枠に、選んだ職業でキャラクターを作って町へ。</summary>
        public void CreateCharacter(int slot, ClassType type)
        {
            Profile.CreateInSlot(slot, type);
            ChooseCharacter(slot);
        }

        /// <summary>
        /// 町で能力値（stats）かスキルの画面を開く。装備は前回帰還したときのもので計算し直す
        /// （加護は挑戦の中だけのものなので入らない）。
        /// </summary>
        public void OpenTownPanel(bool stats)
        {
            var type = ClassInfo.Current;
            Inventory.Reset(type, Profile.Current);
            Player.SetClass(type);
            Player.ResetState(PlayerStats.From(Inventory.Equipped, type));
            OpenPanel(false, stats, !stats);
        }

        /// <summary>町で鞄（装備の付け替え）を開いているか。閉じるときに、装備と持ち込む品を保存する。</summary>
        public bool TownBag { get; private set; }

        /// <summary>
        /// 町で装備を付け替える。前回帰還したときの装備と「次の挑戦に持ち込む品」を鞄の画面に読み込む。
        /// 閉じたら装備・持ち込む品を保存し、町で分解した火種はそのキャラクターの火種に入れる。
        /// </summary>
        public void OpenTownBag()
        {
            var type = ClassInfo.Current;
            Inventory.Reset(type, Profile.Current);
            Player.SetClass(type);
            Player.ResetState(PlayerStats.From(Inventory.Equipped, type));
            TownBag = true;
            OpenPanel(true, false, false);
        }

        void CloseTownBag()
        {
            TownBag = false;
            var ch = Profile.Current;
            if (ch == null) return;
            Inventory.SaveForReturn(ch, new List<Item>()); // 鞄の品はすべて「持ち込む品」なので、そのまま残る
            Profile.Save();
        }

        /// <summary>町から出撃する。チェックポイントか中断した挑戦があれば、開始する層を選ぶ画面へ。</summary>
        public void Depart()
        {
            Hud.BigMap = false;
            var type = ClassInfo.Current;
            var ch = Profile.Get(type);
            if (Checkpoints(ch).Count > 1 || ch.Run.Active) PendingClass = type;
            else StartRun(type, 1);
        }

        /// <summary>町に戻る（帰還・死亡のあと）。</summary>
        public void ShowTown()
        {
            ClearField();
            ChoosingClass = true;
            PendingClass = null;
            InTown = true;
        }

        /// <summary>転職した：7つ目のスキルを覚えて、空いているボタンに付ける。</summary>
        public void OnAdvanced()
        {
            var ch = Profile.Current;
            if (ch == null) return;
            Skills.Learn(ch, AdvancedClasses.SkillIndex);
            Profile.Save();
            var adv = AdvancedClasses.Of(ClassInfo.Current, ch);
            if (adv != null) Hud.Toast(Loc.T($"Class changed: {adv.DisplayName}  (Lv 1, +{AdvancedClasses.BonusPoints} points)", $"転職：{adv.DisplayName}（Lv1、ポイント +{AdvancedClasses.BonusPoints}）") + StoredNote(), new Color(1f, 0.85f, 0.4f));
            Sfx.Landed(Rarity.Legendary);
        }

        /// <summary>転生した：8つ目のスキルを覚えて、空いているボタンに付ける。</summary>
        public void OnRebirth()
        {
            var ch = Profile.Current;
            if (ch == null) return;
            if (ch.Rebirths == 1) Skills.Learn(ch, ThirdClasses.SkillIndex); // 8つ目のスキルは1回目の転生で覚える
            Profile.Save();
            Player.SetClass(ClassInfo.Current); // 足元のオーラの色を変える
            var third = ThirdClasses.Of(ClassInfo.Current, ch);
            if (third != null) Hud.Toast(Loc.T($"Reborn x{ch.Rebirths}: {third.DisplayName}  (Lv 1, +{ThirdClasses.BonusPoints} points, all stats +{ThirdClasses.BonusStats(ch)})",
                $"転生 {ch.Rebirths}回目：{third.DisplayName}（Lv1、ポイント +{ThirdClasses.BonusPoints}、全能力値 +{ThirdClasses.BonusStats(ch)}）") + StoredNote(), ThirdClasses.AuraColor(ch.Rebirths));
            Sfx.Landed(Rarity.Mythic);
        }

        float auraMoteTimer;

        /// <summary>転生した人の足元から、オーラの色の光の粒が立ちのぼる（回数が多いほど多い）。</summary>
        void TickAuraMotes(float dt)
        {
            int n = Player.Rebirths;
            if (n <= 0) return;
            auraMoteTimer -= dt;
            if (auraMoteTimer > 0f) return;
            auraMoteTimer = 0.45f / Mathf.Sqrt(Mathf.Min(n, 6));
            var c = ThirdClasses.AuraColor(n);
            c.a = 0.8f;
            Fx.Sparks(Player.Pos, 1, c, 0.6f, 0.9f, 1.1f, true);
        }

        /// <summary>転職・転生で装備できなくなった品を倉庫へ送ったときのひとこと。</summary>
        static string StoredNote() => Town.LastStored > 0
            ? Loc.T($"  /  {Town.LastStored} item(s) too high-level now sent to storage", $"  /  装備できなくなった品 {Town.LastStored} 個を倉庫へ")
            : "";

        float potionCooldown;

        /// <summary>回復薬（hp）か魔力の薬を使う。持ち数は町の商人で買える。</summary>
        public void UsePotion(bool hp)
        {
            var ch = Profile.Current;
            if (ch == null || Player.IsDead || Paused || ChoosingClass || potionCooldown > 0f) return;
            if (hp)
            {
                if (ch.HpPotions <= 0 || Player.Hp >= Player.MaxHp) return;
                ch.HpPotions--;
                Player.Heal(Mathf.RoundToInt(Player.MaxHp * Town.HpPotionHeal));
                Fx.Ring(Player.Pos, 1.2f, new Color(1f, 0.4f, 0.4f, 0.9f), 0.4f);
            }
            else
            {
                if (ch.MpPotions <= 0 || Player.Mp >= Player.MaxMp) return;
                ch.MpPotions--;
                Player.RestoreMp(Player.MaxMp * Town.MpPotionRestore);
                Fx.Ring(Player.Pos, 1.2f, new Color(0.4f, 0.6f, 1f, 0.9f), 0.4f);
            }
            Sfx.Pickup();
            potionCooldown = Town.PotionCooldown;
            Profile.Save();
        }

        public float PotionCooldownRatio => Mathf.Clamp01(potionCooldown / Town.PotionCooldown);

        public void CancelFloorSelect()
        {
            PendingClass = null;
        }

        CameraRig cameraRig;
        Vector2 lastKillPos;
        bool roomRewardPending;
        bool stairsLatch;     // 階段の画面を閉じたら、一度離れるまで出さない
        SpriteRenderer stairs;
        SpriteRenderer altar;
        SpriteRenderer guideNpc; // 1層目のはじめの部屋に立つ案内役
        readonly List<SpriteRenderer> chests = new List<SpriteRenderer>(); // 宝の間の宝箱
        readonly List<bool> chestOpened = new List<bool>();
        Vector2 guidePos;
        bool guideLatch;         // 話し終えたら、一度離れるまで話さない
        int guideTip;            // 次に話す豆知識

        /// <summary>1層の案内役に近づくと話す豆知識（順番に1つずつ）。</summary>
        static readonly string[][] guideTips =
        {
            new[] { "Elites drop a flood of loot. Their names tell you what they do.", "精鋭はドロップがあふれる。名前で性質がわかるよ。" },
            new[] { "Return home at the stairs to keep what you found. Die, and the new loot is gone.", "階段で帰還すれば拾った物を持ち帰れる。死んだら新しい物はなくなる。" },
            new[] { "Embers buy boons at altars, and potions and gear in town.", "火種は祭壇で加護に、町で薬や装備に変えられる。" },
            new[] { "Every 5 floors a boss waits. Beat it and you can start beyond it.", "5層ごとにボスがいる。倒せば、その先から始められる。" },
            new[] { "Tap the strip above a skill button to cast it automatically.", "スキルのボタンの上の帯を押すと、オートで使ってくれる。" },
            new[] { "A Shroud blinds you: less light, worse hit and evasion. Take it out first.", "覆いは暗闇にする。暗くなって、命中も回避も落ちる。先に倒しな。" },
            new[] { "The storage in town is shared by every class.", "町の倉庫は、どの職業でも使えるよ。" },
        };
        bool altarLatch;
        bool holdSave; // 起動中・再開の準備中は、中断の保存をしない（上書きを防ぐ）
        float slowMoTimer;
        float goldenSparkTimer;
        float stunSparkTimer;
        int bossAnnouncedFloor = -1;
        float lifeStealBudget; // 吸収で回復できる残り（1秒あたり最大 HP の 2% ずつたまる）
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
            var altarGo = new GameObject("Altar");
            altarGo.transform.SetParent(transform, false);
            altar = altarGo.AddComponent<SpriteRenderer>();
            altar.sprite = PixelArt.Altar;
            Lighting.MakeGlow(altar);
            var guideGo = new GameObject("Guide");
            guideGo.transform.SetParent(transform, false);
            guideNpc = guideGo.AddComponent<SpriteRenderer>();
            guideNpc.sprite = PixelArt.Guide;

            Enemies = AddChild<EnemySystem>("Enemies");
            Enemies.Init();
            Enemies.Damaged += OnEnemyDamaged;
            Enemies.Missed += pos => Hud.AddMiss(pos, false);
            Enemies.Killed += OnEnemyKilled;
            Enemies.BossKilled += OnBossKilled;
            Enemies.BossWindup += (pos, radius, time) => Fx.Ring(pos, radius, new Color(1f, 0.2f, 0.2f, 0.7f), time);
            Enemies.BossSlam += OnBossSlam;
            Enemies.BossTelegraph += OnBossTelegraph;
            Enemies.BossStrike += OnBossStrike;
            Enemies.BossEnraged += () =>
            {
                Hud.Toast(Loc.T("The boss is enraged!", "ボスが怒り狂った！"), new Color(1f, 0.4f, 0.3f));
                cameraRig.Shake(0.2f, 0.4f);
            };
            Enemies.EliteKilled += OnEliteKilled;
            Enemies.GoldenAppeared += () =>
            {
                Hud.Toast(Loc.T("A golden slime appeared! Catch it before it escapes!", "黄金のスライムが現れた！逃げられる前に倒せ！"), new Color(1f, 0.85f, 0.3f));
                Hud.ShowBanner(Loc.T("GOLDEN SLIME!", "黄金のスライム！"), Loc.T("Catch it before it escapes", "逃げられる前に倒せ"), new Color(1f, 0.85f, 0.3f));
                var g = Enemies.Golden;
                if (g != null)
                {
                    Fx.Pillar(g.Pos, new Color(1f, 0.85f, 0.3f, 0.9f), 1.2f, 5f, 0.8f);
                    Fx.Sparks(g.Pos, 24, new Color(1f, 0.9f, 0.4f, 1f), 3f, 0.7f, 3f);
                }
                cameraRig.Shake(0.12f, 0.3f);
                Sfx.Landed(Rarity.Legendary);
                Guide("golden", Loc.T("A golden one! It runs away and vanishes after a while.", "黄金のやつだ！逃げ回って、しばらくすると消えちゃう。"),
                    Loc.T("Beat it in time and it bursts with loot and embers.", "時間内に倒せば、装備と火種があふれ出すよ。"));
            };
            Enemies.GoldenKilled += pos =>
            {
                Loot.Fountain(pos, Room, GameConfig.GoldenFountainItems, DropLevel, GameConfig.GoldenRarityBonus,
                    GameConfig.FountainEmberOrbs(Floors.Tier), GameConfig.EmberAmount(Floors.Tier) * GameConfig.GoldenEmberMult);
                if (Random.value < GameConfig.GoldenMythicChance) Loot.DropItem(pos, ItemGenerator.RollMythic(DropLevel), Room, 1.2f);
                Fx.Ring(pos, 2.5f, new Color(1f, 0.85f, 0.3f, 0.9f), 0.5f);
                Sfx.Fountain();
                cameraRig.Shake(0.2f, 0.4f);
                GainExp(Progression.KillExp(Floors.Tier) * 10);
            };
            Enemies.GoldenEscaped += () => Hud.Toast(Loc.T("The golden slime escaped...", "黄金のスライムに逃げられた……"), new Color(0.8f, 0.7f, 0.5f));

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
            Skills.Casted += spec => Hud.AddSkillName(Player.Pos, spec.DisplayName, spec.Color);
            Skills.Shake = (amount, time) => cameraRig.Shake(amount, time);
            Skills.SkillLearned += name =>
            {
                Hud.Toast(Loc.T($"New skill: {name}", $"新しいスキル：{name}"), new Color(0.6f, 0.85f, 1f));
                Guide("skill", Loc.T("A new skill! Use it with the buttons at the bottom left (1-3 on PC).", "新しいスキルだ。左下のボタンで使える（PC は 1〜3 キー）。"),
                    Loc.T("Tap the strip above a button to cast it automatically. Skills grow stronger the more you use them.", "ボタンの上の帯を押すと、オートで勝手に使ってくれる。使うほど強くなるよ。"));
            };
            Sfx = gameObject.AddComponent<Sfx>();
            Music = gameObject.AddComponent<Music>();
            Lighting = AddChild<Lighting>("Lighting");
            Lighting.Init();
            Skills.Init(Enemies, Shots, Fx, Sfx, Room);

            Player = AddChild<Player>("Player");
            Player.Sr = Player.gameObject.AddComponent<SpriteRenderer>();
            Player.Sr.sprite = PixelArt.Player;
            Player.Dodged += OnPlayerDodged;
            Player.Hurt += OnPlayerHurt;
            Player.Evaded += pos => Hud.AddMiss(pos, true);

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
            Floors.TreasureEntered += () =>
            {
                Hud.Toast(Loc.T("A treasure room!", "宝の間だ！"), new Color(1f, 0.85f, 0.4f));
                Guide("treasure", Loc.T("A treasure room. No enemies here, just chests. Walk up to open them.", "宝の間だよ。敵はいなくて、宝箱だけ。近づくと開く。"));
            };
            Floors.RoomActivated += () =>
            {
                roomRewardPending = true;
                Guide("enemies", Loc.T("Enemies appear when you enter a room. You attack on your own once they reach your light.", "部屋に入ると敵が湧く。灯りの届く所まで来たら、勝手に攻撃してくれるよ。"),
                    Loc.T("If you get surrounded, dodge (Space, or flick the screen). Clear the room and the loot comes pouring out.", "囲まれたら回避（Space か、画面をはじく）。全滅させると、ドロップがあふれ出す。"));
            };
            holdSave = true;
            StartRun(ClassType.Lampwright, 1, false); // 画面の後ろに出しておくだけ（中断した挑戦は消さない）
            holdSave = false;
            ShowClassSelect();
            OnTitle = true;
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
            // 案内役の会話中は、決定キーで次へ。ほかの操作には使わない
            bool talking = Hud.Guide.Active;
            if (talking && Input.ConfirmPressed)
            {
                Hud.Guide.Next();
                Input.ClearQueued();
            }
            if (Input.HelpPressed) Hud.ShowHelp = !Hud.ShowHelp;
            if (Input.MapPressed && !InTown && !OnTitle) Hud.BigMap = !Hud.BigMap;
            if (Input.NextMusicPressed) NextMusic();
            Music.SetBoss(!ChoosingClass && !Player.IsDead && Enemies.Boss != null);
            Music.SetGolden(!ChoosingClass && !Player.IsDead && Enemies.Golden != null); // 黄金のスライムがいる間はボーナスタイムの曲
            Music.SetTitle(OnTitle);
            // 装備の効果のうち、ほかの仕組みが読むもの（町では効かせない）
            var fx = ChoosingClass ? null : Player.Stats;
            ItemGenerator.MagicFind = fx != null ? fx.MagicFind : 0f;
            // 敵の会心耐性：塔の中ではいまの層、町では最深層の値（装備の比べ方・能力値の表示に使う）
            var crCh = Profile.Current;
            GameConfig.CritResist = GameConfig.EnemyCritResist(ChoosingClass ? GameConfig.Tier(crCh != null ? crCh.BestFloor : 0) : Floors.Tier);
            LootSystem.EmberMult = fx != null ? 1f + fx.EmberFind : 1f;
            Enemies.EliteDamageMult = fx != null ? 1f + fx.EliteDamage : 1f;
            Music.SetTown(InTown && ChoosingClass);

            potionCooldown = Mathf.Max(0f, potionCooldown - dt);
            if (ChoosingClass)
            {
                if (OnTitle)
                {
                    if (Input.ConfirmPressed) LeaveTitle();
                }
                else if (InTown && !PendingClass.HasValue)
                {
                    if (StatsOpen || SkillsOpen || BagOpen)
                    {
                        if (Input.CancelPressed || (StatsOpen && Input.StatsPressed) || (SkillsOpen && Input.SkillsPressed) || (BagOpen && Input.BagPressed)) OpenPanel(false, false, false);
                    }
                    else if (Input.BagPressed) OpenTownBag();
                    else if (Input.StatsPressed) OpenTownPanel(true);
                    else if (Input.SkillsPressed) OpenTownPanel(false);
                    else if (Input.CancelPressed) Hud.Town.Back();
                }
                else if (PendingClass.HasValue)
                {
                    var cps = Checkpoints(Profile.Get(PendingClass.Value));
                    if (Hud.ConfirmingFloor)
                    {
                        if (Input.CancelPressed) Hud.AskFloor(-1); // 確認をやめる
                    }
                    else if (Input.NumberPressed >= 0 && Input.NumberPressed < cps.Count) Hud.AskFloor(cps[Input.NumberPressed]);
                    else if (Input.CancelPressed) CancelFloorSelect();
                }
                else if (Input.NumberPressed >= 0)
                {
                    Hud.PressSlotKey(Input.NumberPressed); // キャラクター選択の画面（数字キー）
                }
                else if (Input.CancelPressed)
                {
                    Hud.CancelNewCharacter(); // 職業の選択から枠の一覧へ
                }
                cameraRig.CenterShift = Hud.ViewCenterShift; // 上下の帯の間の中心にプレイヤーを置く
                cameraRig.Tick(Player.Pos, Room, dt);
                return;
            }

            if (Input.ToggleModePressed) AutoAttack = !AutoAttack;
            if (Input.HpPotionPressed) UsePotion(true);
            if (Input.MpPotionPressed) UsePotion(false);
            if (Input.BagPressed && !Player.IsDead) OpenPanel(!BagOpen, false, false);
            if (Input.StatsPressed && !Player.IsDead) OpenPanel(false, !StatsOpen, false);
            if (Input.SkillsPressed && !Player.IsDead) OpenPanel(false, false, !SkillsOpen);
            if (Input.CancelPressed)
            {
                // 何か開いていれば閉じる。何も開いていなければメニューを開く
                if (BagOpen || StatsOpen || SkillsOpen || MenuOpen || AltarOpen) OpenPanel(false, false, false);
                else if (!ReturnOpen && !Player.IsDead) OpenMenu();
            }

            if (Player.IsDead)
            {
                Guide("death", Loc.T("Oops, you went down.", "あらら、やられちゃったね。"), Loc.T("What you found this run is lost, but your level stays. Return home first and your gear is safe.", "今回拾った物はなくなるけど、レベルは残る。帰還してから死ねば、装備も守れるよ。"));
                if (!talking && Input.ConfirmPressed) EndRunByDeath();
            }
            else if (!Paused)
            {
                Player.Tick(dt, Input, Room);
                TickAuraMotes(dt);
                TickAbyssGear(dt);
                Attack.Tick(dt, Player, !Player.Stunned && (AutoAttack || Input.AttackHeld)); // 気絶中は攻撃しない
                Skills.Tick(dt, Player, Input.NumberPressed);
                Floors.Tick(dt, Enemies, Player, Room);
                Room.UpdateFlow(Player.Pos);
                Room.Reveal(Player.Pos, Player.EffectiveLight + 2f);
                Room.FadeWallsNear(Player.Pos, dt); // 手前の壁にキャラクターが隠れないよう、重なった壁を透かす
                Room.ApplyMinimap();
                Enemies.Tick(dt, Player, Room);
                Shots.Tick(dt, Enemies, Room);
                Loot.Tick(dt, Player, Inventory);
                Fx.Anchor = Player.Pos;
                Fx.Tick(dt);
                CheckRoomCleared();
                CheckStairs();
                CheckAltar();
                CheckChests();
                CheckGuideNpc();
                CheckEliteTip();
            }

            float stealCap = Player.MaxHp * GameConfig.LifeStealCapPct;
            lifeStealBudget = Mathf.Min(stealCap, lifeStealBudget + stealCap * dt);

            // ボスが目を覚ましたら、名前を大きく出す（その層で1回）
            var boss = Enemies.Boss;
            if (boss != null && bossAnnouncedFloor != Floors.Floor)
            {
                bossAnnouncedFloor = Floors.Floor;
                Hud.ShowBanner(BossInfo.Name(boss.BossKind), GameConfig.FloorName(Floors.Floor) + Loc.T(" boss", "のボス"), new Color(1f, 0.4f, 0.35f));
            }

            // 気絶中は、頭の上に黄色い火花が回る
            if (Player.Stunned)
            {
                stunSparkTimer -= dt;
                if (stunSparkTimer <= 0f)
                {
                    stunSparkTimer = 0.15f;
                    float a = Time.time * 9f;
                    Fx.Sparks(Player.Pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.35f, 1, new Color(1f, 0.9f, 0.3f, 1f), 0.1f, 0.35f, 4f, true);
                }
            }

            // 黄金のスライムがいる間、足元からときどき金のきらめき
            var golden = Enemies.Golden;
            if (golden != null)
            {
                goldenSparkTimer -= dt;
                if (goldenSparkTimer <= 0f)
                {
                    goldenSparkTimer = 0.35f;
                    Fx.Sparks(golden.Pos, 4, new Color(1f, 0.88f, 0.35f, 1f), 0.8f, 0.8f, 1.8f, true);
                }
            }

            // 覆いの精鋭の近くにいる間は「暗闇」（離れて少しで切れる）：暗くなり、命中・回避も下がる
            if (Enemies.ShroudNear) Player.AddDebuff(DebuffKind.Blind, 0.5f);
            Player.LightPenalty = Player.HasDebuff(DebuffKind.Blind) ? Debuffs.BlindLight : 0f;
            Lighting.Tick(Player.Pos, Player.EffectiveLight, dt);
            cameraRig.CenterShift = Hud.ViewCenterShift; // 上下の帯の間の中心にプレイヤーを置く
            cameraRig.Tick(Player.Pos, Room, dt);
        }

        /// <summary>精鋭を倒した：小さなドロップの噴水。</summary>
        void OnEliteKilled(Vector2 pos)
        {
            Loot.Fountain(pos, Room, GameConfig.EliteFountainItems, DropLevel, GameConfig.EliteRarityBonus,
                GameConfig.EliteEmberOrbs, GameConfig.EmberAmount(Floors.Tier));
            // まれに神話（赤）の装備（ボスより低い確率）
            if (Random.value < GameConfig.EliteMythicChance) Loot.DropItem(pos, ItemGenerator.RollMythic(DropLevel), Room, 1.2f);
            Fx.Ring(pos, 1.6f, new Color(1f, 0.85f, 0.4f, 0.9f), 0.35f);
            Sfx.Fountain();
            GainExp(Progression.KillExp(Floors.Tier) * 5);
        }

        /// <summary>部屋の敵を全滅させたら、最後に倒した場所からドロップの噴水。</summary>
        void CheckRoomCleared()
        {
            if (!roomRewardPending || Enemies.AliveIn(Floors.CurrentArea) > 0) return;
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
            if (d < 5f) Guide("stairs", Loc.T("The stairs. Step on them to descend or return home.", "階段だ。乗ると、下りるか、帰還するかを選べる。"),
                Loc.T("Returning keeps your gear and embers. Going deeper finds better loot... but die, and it is all gone.", "帰還すれば装備と火種を持ち帰れる。下りれば、もっといい物が出るけど……死んだら全部なくすよ。"));
            if (d > 1.5f) stairsLatch = false;
            if (stairsLatch || d > GameConfig.StairsRadius) return;
            stairsLatch = true;
            ReturnOpen = true;
        }

        /// <summary>精鋭が初めて近くに来たとき。</summary>
        void CheckEliteTip()
        {
            if (Profile.TipSeen("elite")) return;
            float r = Player.EffectiveLight + 2f;
            foreach (var e in Enemies.Active)
            {
                if (e.Dying || e.Elite == EliteKind.None || (e.Pos - Player.Pos).sqrMagnitude > r * r) continue;
                Guide("elite", Loc.T("The big ones with names are elites. Tough, but they drop a flood of loot.", "名前の付いた大きいのは精鋭。硬いけど、倒せばドロップがあふれる。"),
                    Loc.T("A purple Shroud blinds you when close: less light, worse hit and evasion. Take it out first.", "紫の「覆い」は近くにいると暗闇にする。暗くなって、命中も回避も落ちる。先に倒しな。"));
                return;
            }
        }

        /// <summary>祭壇に乗ったら、加護を選ぶ画面を出す（1つの層で1回だけ選べる）。</summary>
        void CheckAltar()
        {
            altar.enabled = Room.HasAltar;
            if (!Room.HasAltar) return;
            altar.color = AltarUsed ? new Color(0.45f, 0.45f, 0.5f) : Color.white;
            float d = (Player.Pos - Room.AltarPos).magnitude;
            if (d < 5f) Guide("altar", Loc.T("An altar. Offer embers for a boon that lasts the whole run.", "祭壇だよ。火種を払うと、この挑戦のあいだずっと効く加護をくれる。"),
                Loc.T("Once per floor. Your pick changes how you fight.", "1つの層で1回だけ。どれを選ぶかで、戦い方が変わるよ。"));
            if (d > 1.5f) altarLatch = false;
            if (AltarUsed || altarLatch || d > GameConfig.StairsRadius) return;
            altarLatch = true;
            OpenPanel(false, false, false);
            AltarOpen = true;
        }

        /// <summary>祭壇で加護を1つ選ぶ。火種が足りなければ何もしない。</summary>
        public void BuyBoon(int boon)
        {
            int cost = Boons.Cost(Floors.Floor);
            if (AltarUsed || Inventory.Embers < cost) return;
            Inventory.Embers -= cost;
            Player.AddBoon(boon);
            AltarUsed = true;
            AltarOpen = false;
            Fx.Ring(Player.Pos, 2f, new Color(1f, 0.8f, 0.4f, 0.9f), 0.5f);
            Sfx.Landed(Rarity.Legendary);
            int stacks = 0;
            foreach (int b in Player.Boons) if (b == boon) stacks++;
            Hud.ShowBoonGet(boon, stacks);
        }

        /// <summary>次の層へ。地面に残ったドロップは拾える分だけ拾ってから下りる。</summary>
        public void Descend()
        {
            if (Floors.Floor >= GameConfig.LastFloorFor(Profile.Current)) return; // 塔は100層まで（深淵に入れるなら120層まで）
            Loot.CollectAll(Inventory);
            ReturnOpen = false;
            EnterFloor(Floors.Floor + 1);
            Hud.Toast(GameConfig.FloorName(Floors.Floor) + (Floors.IsBossFloor ? Loc.T("  -  BOSS", "  -  ボス") : ""), new Color(1f, 0.85f, 0.5f));
        }

        /// <summary>階段の画面を閉じて、この層に残る。</summary>
        public void StayOnFloor()
        {
            ReturnOpen = false;
        }

        string currentZone;

        /// <summary>区画の見た目（光・背景・スライムの色）を当てる。新しい区画に入ったら名前を大きく出す。</summary>
        void ApplyZone(ZoneSpec zone, int floor)
        {
            Lighting.SetAmbient(zone.AmbientColor, zone.Ambient);
            cameraRig.SetBackground(zone.Background);
            Enemies.SetZone(zone);
            Music.SetZone(zone.Pattern);
            if (zone.Key != currentZone)
            {
                currentZone = zone.Key;
                Hud.ShowBanner($"{GameConfig.FloorName(floor)}  -  {zone.Title}", zone.DisplaySubtitle, zone.Accent);
                if (GameConfig.IsAbyss(floor))
                {
                    // 深淵の区画に入った：区画の色の衝撃の輪と、大きな揺れ
                    Fx.Ring(Room.StartPos, 3f, zone.Accent, 0.6f);
                    Fx.Ring(Room.StartPos, 6f, new Color(zone.Accent.r / 255f, zone.Accent.g / 255f, zone.Accent.b / 255f, 0.6f), 1f);
                    Fx.Sparks(Room.StartPos, 24, zone.Accent, 4f, 1.2f, 2.5f, true);
                    cameraRig.Shake(0.3f, 0.6f);
                    Sfx.Landed(Rarity.Mythic);
                }
            }
        }

        void EnterFloor(int floor)
        {
            Enemies.Clear();
            Shots.Clear();
            Loot.Clear();
            Fx.Clear();
            roomRewardPending = false;
            Floors.Begin(floor, Room);
            Enemies.FloorTier = Floors.Tier;
            Enemies.SetZone(Room.Zone); // 敵を置く前に、区画の敵の割合と色を合わせる
            Floors.Populate(Enemies, Room);
            if (Floors.IsBossFloor) Guide("boss", Loc.T("A boss waits deep in this floor. When the red ring spreads, get outside it or dodge!", "この層の奥にはボスがいる。赤い輪が広がったら、輪の外へ逃げるか回避！"),
                Loc.T("Beat it and the stairs appear. Next time you can start from the floor beyond.", "倒せば階段が現れて、次からはその先の層から始められるようになるよ。"));
            ApplyZone(Room.Zone, floor);
            Player.Pos = Room.StartPos;
            stairs.transform.position = Iso.ToScreen(Room.StairsPos);
            stairs.sortingOrder = SortOrder.Floor + 2;
            stairs.enabled = Floors.StairsOpen;
            stairsLatch = false;

            // 祭壇：並べる加護を3つ選んでおく
            AltarUsed = false;
            AltarOpen = false;
            altarLatch = false;
            AltarOffers.Clear();
            if (Room.HasAltar)
            {
                altar.transform.position = Iso.ToScreen(Room.AltarPos);
                altar.sortingOrder = SortOrder.For(Room.AltarPos);
                var pool = new List<int>();
                for (int k = 0; k < Boons.Count; k++) if (Boons.Offerable(k)) pool.Add(k);
                for (int k = 0; k < Boons.OffersPerAltar && pool.Count > 0; k++)
                {
                    int pick = Random.Range(0, pool.Count);
                    AltarOffers.Add(pool[pick]);
                    pool.RemoveAt(pick);
                }
            }
            altar.enabled = Room.HasAltar;
            PlaceChests();

            // 1層目だけ、はじめの部屋に案内役が立っている
            guideNpc.enabled = floor == 1;
            if (floor == 1)
            {
                Vector2 gp = Room.Clamp(Room.StartPos + new Vector2(1.4f, 1.4f), 0.5f);
                guideNpc.transform.position = Iso.ToScreen(gp) + new Vector2(0f, 0.15f);
                guideNpc.sortingOrder = SortOrder.For(gp);
                guidePos = gp;
                guideLatch = false;
            }
            SaveRun(); // 層の最初で、挑戦の状態を保存する

            var ch = Profile.Current;
            if (ch != null && floor > ch.BestFloor)
            {
                ch.BestFloor = floor;
                Profile.Save();
            }
        }

        void OnEnemyDamaged(Vector2 pos, float amount, bool crit)
        {
            // 深淵の星：会心のときに、その場所へ星が落ちてくる（少し遅れて）
            if (crit && starfallCooldown <= 0f && Player.Stats != null && Player.Stats.StarfallChance > 0f && Random.value < Player.Stats.StarfallChance)
            {
                starfallCooldown = 0.5f;
                starfalls.Add(new Vector3(pos.x, pos.y, 0.35f));
                Fx.Marker(pos, 1.8f, new Color(0.6f, 0.95f, 1f, 0.45f), 0.35f);
            }
            Hud.AddNumber(pos, amount, crit);
            Sfx.Hit();

            Player.MarkCombat();
            float steal = Player.Stats.LifeSteal;
            if (steal > 0f && !Player.IsDead)
            {
                lifeStealPool += amount * steal;
                if (lifeStealPool >= 1f)
                {
                    // 吸収で回復できるのは、1秒あたり最大 HP の 2% まで（殴っているだけで回復し続けないように）
                    int whole = Mathf.Min(Mathf.FloorToInt(lifeStealPool), Mathf.FloorToInt(lifeStealBudget));
                    lifeStealPool = 0f;
                    if (whole > 0)
                    {
                        lifeStealBudget -= whole;
                        Player.Heal(whole);
                    }
                }
            }
        }

        /// <summary>ボスの叩きつけ。輪の内側にいて、回避の無敵中でなければ当たる。</summary>
        void OnBossSlam(Vector2 pos, float radius, int damage)
        {
            Fx.Ring(pos, radius, new Color(1f, 0.9f, 0.8f, 0.9f), 0.2f);
            Sfx.Boom();
            cameraRig.Shake(0.2f, 0.3f);
            if ((Player.Pos - pos).sqrMagnitude <= radius * radius) Player.TakeHit(damage, Enemies.LastBlastSource);
        }

        static readonly Color BossRed = new Color(1f, 0.25f, 0.2f, 0.85f);
        static readonly Color BossBlue = new Color(0.4f, 0.75f, 1f, 0.85f);
        static readonly Color SafeGreen = new Color(0.35f, 1f, 0.5f, 0.9f);

        /// <summary>ボスの攻撃の予告：範囲を赤（気絶は青）で脈打たせる。全体攻撃は部屋を赤く、安全な円を緑で。</summary>
        void OnBossTelegraph(Enemy e, BossAttack atk, float time)
        {
            switch (atk)
            {
                case BossAttack.Slam:
                    Fx.Marker(e.Pos, BossInfo.SlamRadius, BossRed, time);
                    break;
                case BossAttack.StunNova:
                    Fx.Marker(e.Pos, BossInfo.NovaRadius, BossBlue, time);
                    break;
                case BossAttack.Cone:
                    Fx.Fan(e.Pos, e.AttackDir, BossInfo.ConeRange, BossInfo.ConeHalfAngle * 2f, BossRed, time, true);
                    break;
                case BossAttack.Charge:
                    Fx.Fan(e.Pos, e.AttackDir, 7f, 18f, new Color(1f, 0.5f, 0.2f, 0.7f), time, true);
                    break;
                case BossAttack.RoomWide:
                {
                    float r = e.Home != null ? Mathf.Max(e.Home.W, e.Home.H) * 0.75f : 9f;
                    Fx.Marker(e.Home != null ? e.Home.Center : e.Pos, r, new Color(1f, 0.15f, 0.15f, 0.35f), time);
                    foreach (var p in e.SafeSpots) Fx.Marker(p, BossInfo.SafeRadius, SafeGreen, time);
                    Hud.Toast(Loc.T("Get into a green circle!", "緑の円の中へ！"), new Color(0.5f, 1f, 0.6f));
                    break;
                }
                case BossAttack.Blink:
                    Fx.Sparks(e.Pos, 16, new Color(0.7f, 0.5f, 1f, 1f), 1.5f, 0.5f, 2f, true);
                    break;
                default: // 弾・呼び出し：小さな合図
                    Fx.Ring(e.Pos, 2f, new Color(1f, 0.85f, 0.5f, 0.8f), time);
                    break;
            }
        }

        /// <summary>ボスの攻撃が決まった：範囲攻撃の当たり判定と弱体効果、演出。</summary>
        void OnBossStrike(Enemy e, BossAttack atk)
        {
            int damage = Mathf.RoundToInt(e.ContactDamage * BossInfo.DamageMult(atk));
            var debuff = BossInfo.DebuffOf(e.BossKind, atk);
            bool hit = false;
            Vector2 to = Player.Pos - e.Pos;
            switch (atk)
            {
                case BossAttack.Slam:
                    Fx.Ring(e.Pos, BossInfo.SlamRadius, new Color(1f, 0.9f, 0.8f, 0.9f), 0.2f);
                    Fx.Sparks(e.Pos, 18, new Color(0.8f, 0.75f, 0.7f, 1f), 4f, 0.45f);
                    cameraRig.Shake(0.2f, 0.3f);
                    hit = to.sqrMagnitude <= BossInfo.SlamRadius * BossInfo.SlamRadius;
                    break;
                case BossAttack.StunNova:
                    Fx.Disc(e.Pos, BossInfo.NovaRadius, new Color(0.5f, 0.8f, 1f, 0.6f), 0.35f);
                    Fx.Ring(e.Pos, BossInfo.NovaRadius, BossBlue, 0.3f);
                    Fx.Sparks(e.Pos, 20, new Color(0.7f, 0.9f, 1f, 1f), 4f, 0.5f);
                    cameraRig.Shake(0.15f, 0.25f);
                    hit = to.sqrMagnitude <= BossInfo.NovaRadius * BossInfo.NovaRadius;
                    break;
                case BossAttack.Cone:
                {
                    Fx.Fan(e.Pos, e.AttackDir, BossInfo.ConeRange, BossInfo.ConeHalfAngle * 2f, new Color(1f, 0.6f, 0.2f, 0.9f), 0.3f, false);
                    for (int k = 1; k <= 5; k++) Fx.Sparks(e.Pos + e.AttackDir * (k * BossInfo.ConeRange / 5f), 4, new Color(1f, 0.6f, 0.2f, 1f), 1.5f, 0.4f);
                    cameraRig.Shake(0.15f, 0.25f);
                    float dist = to.magnitude;
                    hit = dist <= BossInfo.ConeRange && (dist < 0.5f || Vector2.Angle(e.AttackDir, to) <= BossInfo.ConeHalfAngle);
                    break;
                }
                case BossAttack.RoomWide:
                {
                    bool safe = false;
                    foreach (var p in e.SafeSpots) if ((Player.Pos - p).sqrMagnitude <= BossInfo.SafeRadius * BossInfo.SafeRadius) safe = true;
                    float r = e.Home != null ? Mathf.Max(e.Home.W, e.Home.H) * 0.75f : 9f;
                    Fx.Disc(e.Home != null ? e.Home.Center : e.Pos, r, new Color(1f, 0.3f, 0.2f, 0.5f), 0.4f);
                    cameraRig.Shake(0.3f, 0.4f);
                    hit = !safe;
                    break;
                }
                case BossAttack.Blink:
                    Fx.Ring(e.Pos, 1.5f, new Color(0.7f, 0.5f, 1f, 0.9f), 0.3f);
                    Fx.Sparks(e.Pos, 16, new Color(0.7f, 0.5f, 1f, 1f), 2f, 0.5f);
                    break;
                case BossAttack.Summon:
                    Fx.Ring(e.Pos, 2.5f, new Color(0.5f, 1f, 0.5f, 0.8f), 0.4f);
                    break;
            }
            if (atk == BossAttack.Slam || atk == BossAttack.StunNova || atk == BossAttack.Cone || atk == BossAttack.RoomWide) Sfx.Boom();
            if (hit && Player.TakeHit(damage, $"Boss {atk}") && debuff.HasValue)
                Player.AddDebuff(debuff.Value, BossInfo.DebuffTimeOf(debuff.Value));
        }

        /// <summary>ボスを倒した：大きな噴水と、低い確率でボス専用の Mythic 装備。</summary>
        void OnBossKilled(Vector2 pos)
        {
            // ボスが倒れたら、残りの雑魚もまとめて消える（煙のように）
            foreach (var p in Enemies.BanishAll())
            {
                Fx.Ring(p, 0.8f, new Color(0.85f, 0.8f, 0.95f, 0.8f), 0.35f);
                Fx.Sparks(p, 6, new Color(0.75f, 0.7f, 0.85f, 1f), 1.2f, 0.6f, 2f, true);
            }
            Loot.Fountain(pos, Room, GameConfig.BossFountainItems, DropLevel, GameConfig.BossRarityBonus,
                GameConfig.FountainEmberOrbs(Floors.Tier), GameConfig.EmberAmount(Floors.Tier) * GameConfig.BossEmberMult);
            if (Random.value < GameConfig.MythicChance) Loot.DropItem(pos, ItemGenerator.RollMythic(DropLevel), Room, 1.2f);
            Fx.Ring(pos, 3f, new Color(1f, 0.4f, 0.3f, 0.9f), 0.6f);
            cameraRig.Shake(0.3f, 0.5f);
            Hud.Toast(Loc.T("BOSS DEFEATED!", "ボスを倒した！"), new Color(1f, 0.5f, 0.4f));
            GainExp(Progression.KillExp(Floors.Tier) * 30);
            Floors.OpenStairs();
            var ch = Profile.Current;
            if (ch != null && Floors.Floor > ch.BossCleared)
            {
                ch.BossCleared = Floors.Floor;
                Profile.Save();
                if (Floors.Floor != GameConfig.MaxFloor && Floors.Floor < GameConfig.AbyssLastFloor)
                    Hud.Toast(Loc.T($"Checkpoint unlocked: {GameConfig.FloorName(Floors.Floor + 1)}", $"{GameConfig.FloorName(Floors.Floor + 1)}から始められるようになった"), new Color(0.7f, 1f, 0.7f));
                else if (Floors.Floor == GameConfig.MaxFloor) // 100層を初めて制覇：レベルの上限が 120 に上がる（この戦いの経験値の後なので、上がるのは次に経験値を得てから）
                    Hud.Toast(Loc.T($"Level cap raised to {Progression.UnlockedMaxLevel}!", $"レベルの上限が {Progression.UnlockedMaxLevel} に上がった！"), new Color(1f, 0.85f, 0.4f));
            }
            if (Floors.Floor == GameConfig.MaxFloor)
            {
                bool abyss = GameConfig.CanEnterAbyss(ch);
                Hud.ShowBanner(Loc.T("TOWER CONQUERED", "塔を制覇した"),
                    abyss ? Loc.T("Below the bottom, the Abyss opens.", "底のさらに下に、深淵が口を開けた。")
                          : Loc.T($"The light reaches the very bottom. At Lv {GameConfig.AbyssReqLevel}, something lies below...", $"灯りが、塔のいちばん底まで届いた。Lv{GameConfig.AbyssReqLevel}になれば、その先へ…"),
                    new Color(1f, 0.85f, 0.4f));
                Sfx.Landed(Rarity.Mythic);
            }
            else if (Floors.Floor >= GameConfig.AbyssLastFloor)
            {
                Hud.ShowBanner(Loc.T("ABYSS CONQUERED", "深淵を制覇した"), Loc.T("Even the darkness below has been lit.", "底なしの闇にまで、灯りがともった。"), new Color(1f, 0.3f, 0.5f));
                Sfx.Landed(Rarity.Mythic);
            }
            Hud.Toast(Loc.T("The stairs have appeared", "階段が現れた"), new Color(1f, 0.85f, 0.5f));
        }

        /// <summary>ボス装備の固有効果：回避した場所で炎が弾ける。</summary>
        // ---------------- 深淵の装備の固有効果 ----------------
        bool abyssBlasting;
        float riftTimer, riftLife, riftTick, retributionCooldown;
        Vector2 riftPos;
        readonly List<Vector3> starfalls = new List<Vector3>(); // 落ちる星（x, y, 落ちるまでの秒）
        float starfallCooldown;

        /// <summary>裂け目（一定時間ごと）と、落ちてくる星の処理。</summary>
        void TickAbyssGear(float dt)
        {
            var st = Player.Stats;
            if (st == null) return;
            retributionCooldown = Mathf.Max(0f, retributionCooldown - dt);
            starfallCooldown = Mathf.Max(0f, starfallCooldown - dt);

            // 落ちる星：予告の光の柱 → 着弾で爆発
            for (int i = starfalls.Count - 1; i >= 0; i--)
            {
                var s = starfalls[i];
                s.z -= dt;
                var p = new Vector2(s.x, s.y);
                if (s.z > 0f) { starfalls[i] = s; continue; }
                starfalls.RemoveAt(i);
                const float radius = 1.8f;
                var star = new Color(0.6f, 0.95f, 1f, 0.95f);
                Fx.Pillar(p, star, 0.9f, 6f, 0.35f);
                Fx.Disc(p, radius, new Color(0.8f, 1f, 1f, 0.7f), 0.25f);
                Fx.Ring(p, radius, star, 0.35f);
                Fx.Sparks(p, 14, star, 5f, 0.5f, 3.5f);
                Sfx.Boom();
                cameraRig.Shake(0.08f, 0.2f);
                Enemies.DamageArea(p, radius, st.Damage * 4f);
            }

            // 裂け目：近くの敵の場所に開き、2秒のあいだ吸い込んで切り裂く
            if (st.VoidRift <= 0) { riftLife = 0f; return; }
            if (riftLife > 0f)
            {
                riftLife -= dt;
                riftTick -= dt;
                Enemies.Pull(riftPos, 3.2f, 2.2f);
                if (riftTick <= 0f)
                {
                    riftTick = 0.25f;
                    var violet = new Color(0.75f, 0.2f, 1f, 0.9f);
                    Fx.Marker(riftPos, 0.9f, new Color(0.2f, 0f, 0.3f, 0.85f), 0.3f);
                    Fx.Ring(riftPos, 2.2f, violet, 0.25f);
                    Fx.Sparks(riftPos, 4, RarityInfo.AbyssColor, 1.5f, 0.5f, 1.5f, true);
                    Enemies.DamageArea(riftPos, 2.2f, st.Damage * 0.6f);
                }
                return;
            }
            riftTimer -= dt;
            if (riftTimer > 0f) return;
            var target = Enemies.FindNearest(Player.Pos, 7f);
            if (target == null) { riftTimer = 0.5f; return; }
            riftTimer = 5f / st.VoidRift;
            riftLife = 2f;
            riftTick = 0f;
            riftPos = target.Pos;
            Fx.Ring(riftPos, 3.2f, new Color(1f, 0.4f, 1f, 0.9f), 0.3f);
            Sfx.Boom();
        }

        /// <summary>深淵の反撃：受けたら闇の波動（2.5秒に1回まで）。</summary>
        void OnPlayerHurt(int dealt)
        {
            var st = Player.Stats;
            if (st == null || st.RetributionMult <= 0f || retributionCooldown > 0f) return;
            retributionCooldown = 2.5f;
            var pos = Player.Pos;
            const float radius = 3.2f;
            Fx.Disc(pos, radius, new Color(0.3f, 0f, 0.35f, 0.7f), 0.3f);
            Fx.Ring(pos, radius, RarityInfo.AbyssColor, 0.4f);
            Fx.Ring(pos, radius * 0.55f, new Color(1f, 0.8f, 1f, 0.9f), 0.25f);
            Fx.Sparks(pos, 16, RarityInfo.AbyssColor, 5f, 0.5f, 2.5f);
            cameraRig.Shake(0.12f, 0.25f);
            Enemies.DamageArea(pos, radius, st.Damage * st.RetributionMult);
        }

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
            // 深淵の敵は、区画の色の光の粒になって散る
            if (GameConfig.IsAbyss(Floors.Floor))
            {
                var accent = (Color)Zones.For(Floors.Floor).Accent;
                Fx.Sparks(pos, 6, accent, 3f, 0.5f, 2f);
                Fx.Sparks(pos, 3, accent, 1f, 0.8f, 1.5f, true);
            }
            GainExp(Mathf.Max(1, Mathf.RoundToInt(Progression.KillExp(Floors.Tier) * Enemies.LastKillExpMult)));
            var stats = Player.Stats;

            if (Random.value < GameConfig.ItemDropChance) Loot.DropItem(pos, ItemGenerator.Roll(DropLevel, 0f), Room);
            if (Random.value < GameConfig.EmberDropChance) Loot.DropEmbers(pos, GameConfig.EmberAmount(Floors.Tier), Room, 0.6f);
            if (stats.HealOnKill > 0) Player.Heal(stats.HealOnKill);

            // 深淵の業火：倒した場所で大爆発（業火で倒した敵からは、さらに起きない）
            if (stats.AbyssBlastChance > 0f && !abyssBlasting && Random.value < stats.AbyssBlastChance)
            {
                const float radius = 2.6f;
                var crimson = new Color(1f, 0.15f, 0.3f, 0.95f);
                Fx.Disc(pos, radius, new Color(0.25f, 0f, 0.15f, 0.8f), 0.35f);
                Fx.Ring(pos, radius * 0.6f, new Color(1f, 0.75f, 0.85f, 0.95f), 0.2f);
                Fx.Ring(pos, radius, crimson, 0.4f);
                Fx.Sparks(pos, 18, crimson, 6f, 0.6f, 3f);
                Fx.Sparks(pos, 8, RarityInfo.AbyssColorDark, 2f, 0.9f, 2f, true);
                Sfx.Boom();
                cameraRig.Shake(0.12f, 0.25f);
                abyssBlasting = true;
                Enemies.DamageArea(pos, radius, stats.Damage * 3f);
                abyssBlasting = false;
            }

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

        /// <summary>メニューを開く。</summary>
        public void OpenMenu()
        {
            OpenPanel(false, false, false);
            MenuOpen = true;
        }

        /// <summary>BGM のテーマを次へ。</summary>
        public void NextMusic()
        {
            Music.NextTheme();
            Hud.Toast($"BGM: {Music.ThemeName}", new Color(0.8f, 0.8f, 1f));
        }

        /// <summary>鞄・能力値・スキルの画面のどれか1つだけを開く（全部 false で閉じる）。</summary>
        public void OpenPanel(bool bag, bool stats, bool skills)
        {
            if (TownBag && !bag) CloseTownBag();
            MenuOpen = false;
            AltarOpen = false;
            BagOpen = bag;
            StatsOpen = stats;
            SkillsOpen = skills;
            if (skills) Guide("skillsScreen", Loc.T("Tap a skill to pick it, then tap one of the buttons at the bottom to set it.", "スキルをタップして選んで、下のボタンをタップすると付けられるよ。"),
                Loc.T("Tap the same skill's button again to remove it, or pick another skill to swap.", "付いているボタンを同じスキルでもう一度タップすると外れる。別のスキルなら付け替えだ。"));
        }

        void GainExp(int amount)
        {
            var ch = Profile.Current;
            int oldLevel = ch.Level;
            if (Player.Stats != null) amount = Mathf.RoundToInt(amount * (1f + Player.Stats.ExpGain)); // 経験値の効果
            int gained = Progression.AddExp(ch, amount);
            if (gained <= 0) return;
            Skills.OnLevelChanged(ch, oldLevel);

            RefreshStats();
            Player.Refill();
            Fx.Ring(Player.Pos, 2f, new Color(1f, 0.95f, 0.6f, 0.9f), 0.5f);
            Fx.Anchor = Player.Pos;
            Fx.LevelUp();
            Hud.ShowLevelUp();
            Sfx.Landed(Rarity.Legendary);
            Hud.Toast(Loc.T($"LEVEL UP!  Lv {ch.Level}  (+{gained * Progression.PointsPerLevel} points)", $"レベルアップ！  Lv {ch.Level}  （ポイント +{gained * Progression.PointsPerLevel}）"), new Color(1f, 0.95f, 0.5f));
            Guide("levelup", Loc.T("Level up! Spend points with the C key or STATS in the MENU.", "レベルが上がった！ ポイントは C キーかメニューの「能力値」で振れる。"),
                Loc.T("Want more MP? Put a little into WIS early. It changes how much you gain each level.", "MP が欲しいなら、早めに WIS を少し振っておくといい。レベルアップのたびに伸びが変わるからね。"));
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

        /// <summary>仮に振ったポイントをまとめて確定する（能力値の画面の「確定」）。</summary>
        public void AllocateMany(int[] add)
        {
            var ch = Profile.Current;
            if (ch == null) return;
            int sum = 0;
            foreach (int v in add) sum += Mathf.Max(0, v);
            if (sum <= 0 || sum > ch.Points) return;
            for (int i = 0; i < add.Length && i < ch.Alloc.Length; i++) ch.Alloc[i] += Mathf.Max(0, add[i]);
            ch.Points -= sum;
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
            Hud.Toast(Loc.T("Points reset", "ポイントを振り直した"), Color.white);
        }

        /// <summary>
        /// 今の職業を完全に最初からやり直す：レベル・能力値・スキルの熟練度・持ち越した装備と倉庫・火種・最深層をすべて消す。
        /// </summary>
        public void ResetLevel()
        {
            Profile.ResetCurrent();
            Profile.Save();
            ShowClassSelect();
        }

        /// <summary>装備できるか（必要レベルと必要能力値）。できなければ理由を返す。</summary>
        public bool CanEquip(Item item, out string reason)
        {
            var st = Player.Stats;
            if (Town.OtherClassWeapon(item, ClassInfo.Current))
            {
                reason = Loc.T("Other class", "ほかの職業の武器");
                return false;
            }
            if (st.Level < item.ReqLevel)
            {
                reason = Loc.T($"Need Lv {item.ReqLevel}", $"Lv{item.ReqLevel}が必要");
                return false;
            }
            if (item.NeedsAttr && st.Attr[(int)item.ReqAttr] < item.ReqValue)
            {
                reason = Loc.T($"Need {Attributes.ShortName(item.ReqAttr)} {item.ReqValue}", $"{Attributes.ShortName(item.ReqAttr)} {item.ReqValue}が必要");
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

        void OnLootLanded(Item item, Vector2 pos)
        {
            if (item != null && item.Abyss)
            {
                // 深淵の装備：赤紫の輪と、立ちのぼる闇の粒
                Fx.Ring(pos, 1.3f, new Color(RarityInfo.AbyssColor.r, RarityInfo.AbyssColor.g, RarityInfo.AbyssColor.b, 0.9f), 0.35f);
                Fx.Sparks(pos, 10, RarityInfo.AbyssColor, 1.2f, 0.9f, 2f, true);
            }
            if (item == null || item.Rarity < Rarity.Rare) return;
            Sfx.Landed(item.Rarity);
            if (item.Rarity == Rarity.Legendary)
            {
                cameraRig.Shake(0.15f, 0.3f);
            }
            else if (item.Rarity == Rarity.Mythic)
            {
                // 神話：3重の衝撃の輪、強い揺れ、長めのスロー、画面中央に大きく知らせる
                var red = RarityInfo.ColorOf(Rarity.Mythic);
                Fx.Ring(pos, 1.6f, new Color(red.r, red.g, red.b, 0.95f), 0.35f);
                Fx.Ring(pos, 3f, new Color(1f, 0.85f, 0.4f, 0.9f), 0.6f);
                Fx.Ring(pos, 4.5f, new Color(red.r, red.g, red.b, 0.8f), 0.9f);
                Sfx.Fountain();
                cameraRig.Shake(0.35f, 0.7f);
                slowMoTimer = 0.9f;
                Time.timeScale = 0.25f;
                Hud.ShowBanner(Loc.T("MYTHIC DROP!", "神話の装備！"), item.Name, red);
            }
            else if (item.Rarity >= Rarity.Relic)
            {
                cameraRig.Shake(0.25f, 0.5f);
                slowMoTimer = 0.6f; // 一瞬スローにする
                Time.timeScale = 0.3f;
            }
        }

        /// <summary>宝の間の宝箱を置く（宝の間がない層ではすべて隠す）。</summary>
        void PlaceChests()
        {
            while (chests.Count < Room.ChestPositions.Count)
            {
                var go = new GameObject("Chest");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                Lighting.MakeGlow(sr);
                chests.Add(sr);
                chestOpened.Add(false);
            }
            for (int i = 0; i < chests.Count; i++)
            {
                bool on = i < Room.ChestPositions.Count;
                chests[i].enabled = on;
                chestOpened[i] = false;
                if (!on) continue;
                var p = Room.ChestPositions[i];
                chests[i].sprite = PixelArt.Chest(false);
                chests[i].transform.position = Iso.ToScreen(p) + new Vector2(0f, 0.2f);
                chests[i].sortingOrder = SortOrder.For(p);
            }
        }

        /// <summary>宝箱に近づいたら開ける：珍しい装備と火種の噴水。</summary>
        void CheckChests()
        {
            for (int i = 0; i < Room.ChestPositions.Count && i < chests.Count; i++)
            {
                if (chestOpened[i]) continue;
                var p = Room.ChestPositions[i];
                if ((Player.Pos - p).sqrMagnitude > 0.9f * 0.9f) continue;
                chestOpened[i] = true;
                chests[i].sprite = PixelArt.Chest(true);
                Loot.Fountain(p, Room, GameConfig.ChestItems, DropLevel, GameConfig.ChestRarityBonus,
                    GameConfig.FountainEmberOrbs(Floors.Tier) / 2, GameConfig.EmberAmount(Floors.Tier) * GameConfig.ChestEmberMult);
                Fx.Ring(p, 1.6f, new Color(1f, 0.85f, 0.4f, 0.9f), 0.4f);
                Sfx.Fountain();
            }
        }

        /// <summary>1層の案内役に近づいたら、豆知識を1つ話す（何度でも。離れてまた近づくと次の話）。</summary>
        void CheckGuideNpc()
        {
            if (!guideNpc.enabled) return;
            float d = (Player.Pos - guidePos).magnitude;
            if (d > 2.2f) guideLatch = false;
            if (guideLatch || d > 1.1f || Hud.Guide.Active) return;
            guideLatch = true;
            var tip = guideTips[guideTip % guideTips.Length];
            guideTip++;
            Hud.Guide.Say(Loc.T(tip[0], tip[1]));
        }

        /// <summary>案内役の説明（その場面が初めてのときだけ）。</summary>
        void Guide(string id, params string[] lines) => Hud.Guide.Show(id, lines);

        void OnItemPickedUp(Item item)
        {
            Sfx.Pickup();
            Hud.Toast(item.Name, RarityInfo.ColorOf(item.Rarity));
            Guide("loot", Loc.T("Oh, you found some gear. Open your bag with the B key or BAG in the MENU.", "お、装備を拾ったね。B キーかメニューの「鞄」で開けるよ。"),
                Loc.T("A green UPGRADE means it is time to swap. Salvage what you do not need for embers.", "緑の「強くなる」なら付け替え時。いらない物は分解すると火種になる。"));
        }

        void UpdateSlowMo()
        {
            if (slowMoTimer <= 0f) return;
            slowMoTimer -= Time.unscaledDeltaTime;
            if (slowMoTimer <= 0f) Time.timeScale = 1f;
        }

        void OnTapped(Vector2 startPos)
        {
            if (Hud.Guide.Active)
            {
                Hud.HandleTap(startPos); // 会話を進める
                return;
            }
            if (Player.IsDead && !ChoosingClass)
            {
                EndRunByDeath();
                return;
            }
            Hud.HandleTap(startPos);
        }

        /// <summary>職業を選ぶ画面を出す。選ぶと StartRun が呼ばれる。</summary>
        public void ShowClassSelect()
        {
            ClearField();
            ChoosingClass = true;
            PendingClass = null;
            InTown = false;
        }

        /// <summary>選んだ職業で挑戦を始める。</summary>
        public void StartRun(ClassType type, int startFloor = 1, bool fresh = true)
        {
            PendingClass = null;
            InTown = false;
            if (fresh) ClearRun(Profile.Get(type)); // 新しく始めると、中断した挑戦は消える
            ClassInfo.Current = type;
            Profile.Select(type);
            ChoosingClass = false;
            ClearField();
            Player.SetClass(type);
            Inventory.Reset(type, Profile.Current);
            Player.ResetState(PlayerStats.From(Inventory.Equipped, type));
            EnterFloor(Mathf.Max(1, startFloor));
            Input.ClearQueued();
            if (!holdSave && startFloor <= 1)
            {
                Guide("welcome", Loc.T("Hey there. This is a tower where no light reaches. Head down, defeat enemies, and collect gear.", "やあ。ここは灯りの届かない塔。底へ下りながら、敵を倒して装備を集めるんだ。"),
                    Loc.T("Press and drag to walk (WASD on PC). Your attacks fly at enemies within your light on their own.", "画面を押してずらすと歩ける（PC は WASD）。攻撃は灯りの届く敵へ勝手に飛ぶよ。"),
                    Loc.T("Explore the rooms and find the stairs. The map at the top right fills in as you look around.", "部屋をまわって、どこかにある階段を探しな。右上の地図に、見た場所が描かれていく。"));
            }
        }

        /// <summary>帰還する：装備と選んだ品と火種を保存して、職業選択に戻る。</summary>
        public void ReturnHome(List<Item> chosen)
        {
            var ch = Profile.Current;
            if (ch == null) return;
            if (chosen.Count > ch.CarrySlots) chosen = chosen.GetRange(0, ch.CarrySlots);
            Inventory.SaveForReturn(ch, chosen);
            ClearRun(ch);
            Profile.Save();
            ShowTown();
            Hud.Toast(Loc.T($"Returned home with {chosen.Count} new items", $"新しい品 {chosen.Count} 個を持ち帰った"), new Color(0.7f, 1f, 0.7f));
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
            if (!paused) return;
            SaveRun(); // スマホでアプリが裏に回ったとき
            Profile.Save();
        }

        void OnApplicationQuit()
        {
            SaveRun();
            Profile.Save();
        }

        /// <summary>
        /// 今の挑戦を中断用に保存する（層・装備・鞄・火種・加護・体力の割合）。再開はその層の最初から。
        /// 職業選択中・死亡後は保存しない。
        /// </summary>
        void SaveRun()
        {
            var ch = Profile.Current;
            if (holdSave || ch == null || ChoosingClass || Player.IsDead) return;
            var run = ch.Run;
            run.Active = true;
            run.Floor = Floors.Floor;
            Inventory.SaveRun(run);
            run.Boons.Clear();
            run.Boons.AddRange(Player.Boons);
            run.HpRatio = Player.MaxHp > 0 ? (float)Player.Hp / Player.MaxHp : 1f;
            run.MpRatio = Player.MaxMp > 0 ? (float)Player.Mp / Player.MaxMp : 1f;
            Profile.Save();
        }

        /// <summary>死んだ：中断した挑戦を消して、職業選択へ。</summary>
        void EndRunByDeath()
        {
            ClearRun(Profile.Current);
            Profile.Save();
            ShowTown();
        }

        /// <summary>中断した挑戦を消す（死亡・帰還・新しく始めたとき）。</summary>
        void ClearRun(CharacterData ch)
        {
            if (ch == null) return;
            ch.Run = new RunSave();
        }

        /// <summary>中断した挑戦を、保存した層の最初から続ける。</summary>
        public void ResumeRun(ClassType type)
        {
            var ch = Profile.Get(type);
            if (!ch.Run.Active)
            {
                StartRun(type, 1);
                return;
            }
            var run = ch.Run;
            holdSave = true;
            StartRun(type, run.Floor, false);
            Inventory.RestoreRun(run);
            Player.RestoreRun(run.Boons, run.HpRatio, run.MpRatio);
            holdSave = false;
            SaveRun();
            Hud.Toast(Loc.T($"Resumed on floor {run.Floor}", $"{run.Floor}層から再開"), new Color(0.7f, 1f, 0.7f));
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}
