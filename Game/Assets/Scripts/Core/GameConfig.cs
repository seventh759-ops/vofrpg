using UnityEngine;

namespace VividOfFade
{
    /// <summary>調整用の数値。実際に遊んでから変える前提の仮の値。</summary>
    public static class GameConfig
    {
        // 部屋（マス）。1マス = 16ドット = 1ユニット
        // 塔：5層ごとにボスの層。敵の強さ・経験値・ドロップは「段階」（層×2）で決める（試作の波の数と同じ尺度）
        public const int BossFloorEvery = 5;
        public const int MaxFloor = 100; // 塔の最後の層（ボス）
        public static bool IsBossFloor(int floor) => floor > 0 && floor % BossFloorEvery == 0;
        public static int Tier(int floor) => floor * 2;
        public static int RoomEnemies(int floor, int roomArea) => Mathf.Min(6 + floor * 2 + roomArea / 10, 70);
        public const float StairsRadius = 0.7f;

        // 縦画面で横に見える幅（ユニット。1ユニット = 16 ドット。14 なら内部解像度 224 ドット相当）
        public const float ViewWidthTiles = 14f;

        // プレイヤー（攻撃力などは装備から PlayerStats で計算する）
        public const int PlayerMaxHp = 100;
        public const float PlayerSpeed = 4.5f;
        public const float PlayerRadius = 0.3f;
        public const float HitInvuln = 0.6f;

        // 明るさ（通常攻撃の届く範囲）。全員共通の基本値に、ランタンや効果で足す
        public const float BaseLight = 3f;
        public const float MaxLight = 9f;
        public const float MinLight = 1.5f;   // 明るさを奪われても、これより狭くはならない

        // 回避
        public const float DodgeSpeed = 14f;
        public const float DodgeTime = 0.16f;
        public const float DodgeInvuln = 0.3f;
        public const float DodgeCooldown = 0.6f;

        // 火の玉
        public const float ShotSpeed = 13f;
        public const float ShotLife = 0.8f;
        public const float ShotRadius = 0.15f;

        // 雑魚（スライム）
        public const int SlimeContactDamage = 8; // 第1波の値。波が進むと増える（EnemyDamage）
        public const float SlimeRadius = 0.32f;
        public const float SlimeSpeedMin = 1.4f;
        public const float SlimeSpeedMax = 2.1f;

        // 波が進むほど、体力も攻撃力も増える（後半ほど伸びが大きい）
        public static float EnemyHp(int wave) => 20f * (1f + 0.3f * (wave - 1)) * Mathf.Pow(1.03f, wave - 1);
        public static int EnemyDamage(int wave) => Mathf.RoundToInt(SlimeContactDamage * (1f + 0.15f * (wave - 1)) * Mathf.Pow(1.02f, wave - 1));

        // 敵の波
        public const int MaxEnemies = 150;
        public const float WaveSpawnDuration = 2.5f;
        public const float WaveGap = 3f;

        public static int WaveSize(int wave) => Mathf.Min(15 + wave * 10, 120);

        // ボス（10波ごと）
        public static bool IsBossWave(int wave) => wave > 0 && wave % 10 == 0;
        public const float BossHpMult = 120f;          // 同じ波の雑魚の何倍か
        public const float BossDamageMult = 2.5f;
        public const float BossScale = 3f;
        public const float BossSpeed = 1.25f;
        public const float BossSlamInterval = 3.4f;
        public const float BossWindup = 0.8f;
        public const float BossSlamRadius = 2.6f;
        public static readonly Color BossTint = new Color(1f, 0.45f, 0.45f, 1f);
        public const float MythicChance = 0.15f;
        public const float BossEnragedSpeedMult = 1.7f;   // 体力が半分を切ったら
        public const float BossEnragedSlamInterval = 2f;

        // 精鋭（第3波から。1波あたり 1 + 波/10 体くらい）
        public const int EliteFirstWave = 3;
        public static float EliteChance(int wave) => wave < EliteFirstWave ? 0f : Mathf.Min(0.02f + wave * 0.001f, 0.06f);
        public const float EliteHpMult = 4f;
        public const float EliteScale = 1.45f;

        // 敵の種類（体力・攻撃の倍率は、その層のスライムに対して）
        public const float BatHpMult = 0.5f, BatSpeed = 3.2f, BatDamageMult = 0.7f;
        public const float CasterHpMult = 0.8f, CasterSpeed = 1.7f, CasterRange = 4.5f, CasterFireInterval = 2.2f, BulletSpeed = 4.2f, BulletDamageMult = 0.9f;
        public const float BruteHpMult = 2.5f, BruteSpeed = 1.1f, BruteDamageMult = 1.4f, BruteScale = 1.25f, BruteChargeSpeed = 7f, BruteChargeTime = 0.45f, BrutePrep = 0.6f, BruteChargeEvery = 3.5f;
        public const float GoldenChance = 0.06f, GoldenHpMult = 6f, GoldenSpeed = 3.3f, GoldenLife = 12f;
        public const int GoldenFountainItems = 10;
        public const float GoldenRarityBonus = 2.5f;
        public const float TreasureRoomChance = 0.15f;
        public const int TreasureChests = 3, ChestItems = 4;
        public const float ChestRarityBonus = 2f;
        public const float BomberHpMult = 0.7f, BomberSpeed = 2.4f, BomberFuse = 0.8f, BomberRadius = 1.8f, BomberDamageMult = 2.5f;
        public const int EliteFountainItems = 2;
        public const float EliteRarityBonus = 0.8f;
        public const float ShroudRange = 5f;      // この距離にいると明るさを奪う
        public const float ShroudPenalty = 2f;
        public const int SplitCount = 3;       // ボスから Mythic 装備が落ちる確率
        public const int BossFountainItems = 8;
        public const float BossRarityBonus = 1.5f;

        // ドロップ
        public const float ItemDropChance = 0.04f;   // 雑魚1体あたり
        public const float EmberDropChance = 0.3f;
        public const float MagnetRange = 2.2f;
        public const float VacuumDelay = 1.2f;   // 波を全滅させてから、地面のドロップを全部吸い寄せるまで
        public const float PickupRange = 0.35f;

        // 噴水の数は上限を設ける。火種は粒の数を増やしすぎず、1粒の量で増やす
        public static int FountainItems(int wave) => Mathf.Min(3 + wave / 2, 10);
        public static int FountainEmberOrbs(int wave) => Mathf.Min(6 + wave, 16);
        public static int EmberAmount(int wave) => 1 + wave / 10;
        public const float FountainRarityBonus = 0.6f;

        /// <summary>
        /// 落ちる装備のレベル。波の数が基本だが、自分のレベル＋3 を上限にする
        /// （レベルが波に追いつかなくなっても、付けられる装備が落ち続けるように）。
        /// </summary>
        public const int DropLevelAhead = 3;
        public static int DropLevel(int wave, int characterLevel) =>
            Mathf.Clamp(wave, 1, Mathf.Min(characterLevel + DropLevelAhead, Progression.MaxLevel)); // Lv上限を超える装備は出さない
    }
}
