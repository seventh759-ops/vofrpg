using UnityEngine;

namespace VividOfFade
{
    /// <summary>M1 の調整用の数値。実際に遊んでから変える前提の仮の値。</summary>
    public static class GameConfig
    {
        // 部屋（マス）。1マス = 16ドット = 1ユニット
        public const int RoomWidth = 13;
        public const int RoomHeight = 21;

        // 縦画面で横に見えるマス数（内部解像度 180 ドット相当）
        public const float ViewWidthTiles = 11.25f;

        // プレイヤー
        public const int PlayerMaxHp = 100;
        public const float PlayerSpeed = 4.5f;
        public const float PlayerRadius = 0.3f;
        public const float HitInvuln = 0.6f;

        // 回避
        public const float DodgeSpeed = 14f;
        public const float DodgeTime = 0.16f;
        public const float DodgeInvuln = 0.3f;
        public const float DodgeCooldown = 0.6f;

        // 通常攻撃
        public const float FireInterval = 0.18f;
        public const float AttackRange = 6f;
        public const float ShotSpeed = 13f;
        public const float ShotLife = 0.8f;
        public const float ShotRadius = 0.15f;
        public const int ShotDamage = 1;
        public const int ShotPierce = 1; // 何体まで突き抜けるか

        // 雑魚（スライム）
        public const int SlimeHp = 2;
        public const int SlimeContactDamage = 6;
        public const float SlimeRadius = 0.32f;
        public const float SlimeSpeedMin = 1.4f;
        public const float SlimeSpeedMax = 2.1f;

        // 敵の波
        public const int MaxEnemies = 150;
        public const float WaveSpawnDuration = 2.5f;
        public const float WaveGap = 1.5f;

        public static int WaveSize(int wave) => Mathf.Min(15 + wave * 10, 120);
    }
}
