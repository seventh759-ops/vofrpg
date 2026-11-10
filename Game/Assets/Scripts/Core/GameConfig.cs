using UnityEngine;

namespace VividOfFade
{
    /// <summary>調整用の数値。実際に遊んでから変える前提の仮の値。</summary>
    public static class GameConfig
    {
        // 部屋（マス）。1マス = 16ドット = 1ユニット
        // 部屋（論理のマス）。クォータービューでは画面上でひし形になる
        public const int RoomWidth = 16;
        public const int RoomHeight = 16;

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
    }
}
