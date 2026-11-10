using UnityEngine;

namespace VividOfFade
{
    /// <summary>調整用の数値。実際に遊んでから変える前提の仮の値。</summary>
    public static class GameConfig
    {
        // 部屋（マス）。1マス = 16ドット = 1ユニット
        public const int RoomWidth = 13;
        public const int RoomHeight = 21;

        // 縦画面で横に見えるマス数（内部解像度 180 ドット相当）
        public const float ViewWidthTiles = 11.25f;

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
        public const int SlimeContactDamage = 8;
        public const float SlimeRadius = 0.32f;
        public const float SlimeSpeedMin = 1.4f;
        public const float SlimeSpeedMax = 2.1f;

        public static float EnemyHp(int wave) => 20f * (1f + 0.25f * (wave - 1));

        // 敵の波
        public const int MaxEnemies = 150;
        public const float WaveSpawnDuration = 2.5f;
        public const float WaveGap = 3f;

        public static int WaveSize(int wave) => Mathf.Min(15 + wave * 10, 120);

        // ドロップ
        public const float ItemDropChance = 0.04f;   // 雑魚1体あたり
        public const float EmberDropChance = 0.3f;
        public const float MagnetRange = 1.6f;
        public const float PickupRange = 0.35f;

        public static int FountainItems(int wave) => 3 + wave / 2;
        public static int FountainEmbers(int wave) => 6 + wave;
        public const float FountainRarityBonus = 0.6f;
    }
}
