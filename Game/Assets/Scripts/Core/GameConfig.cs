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
        public static int RoomEnemies(int floor, int roomArea) => Mathf.Min(5 + Mathf.RoundToInt(floor * 1.2f) + roomArea / 14, 45); // 前の約6割の密度
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
        public const float SlimeRadius = 0.32f;
        public const float SlimeSpeedMin = 1.4f;
        public const float SlimeSpeedMax = 2.1f;

        // 波が進むほど、体力も攻撃力も増える（後半ほど伸びが大きい）
        // 体力の複利は 3% → 2.2% にした（後半に敵が硬すぎたため）
        public const float EnemyHpGrowth = 1.022f;
        public static float EnemyHp(int wave) => 20f * (1f + 0.3f * (wave - 1)) * Mathf.Pow(EnemyHpGrowth, wave - 1);
        /// <summary>
        /// 術師の弾のダメージ。体当たりと違い、深い層でも急に強くならない（指数で増えない）。
        /// 避けにくい分、少しずつ削る攻撃にする。
        /// </summary>
        public static int BulletDamage(int tier) => Mathf.Max(1, Mathf.RoundToInt(EnemyDamage(tier) * BulletToContact));

        /// <summary>弾は体当たりの約4割（体当たりが弾の約2.6倍）。</summary>
        public const float BulletToContact = 0.38f;

        // ---- 敵の攻撃力は「基準のキャラ」から決める ----
        // 基準は極振りではなく、バランス型（VIT に2割振り・その段階の装備）。そのキャラが体当たり1回で HP の何％を削られるかで決める。
        // 極振りで VIT を捨てればこれより多く削られ、守りを固めればこれより少なく済む（極振りもスタイルとして残す）。
        public const float RefVitShare = 0.2f;         // 基準のキャラが VIT に振る割合
        public const float RefHitPctStart = 0.045f;    // 序盤：体当たり1回で HP の 4.5%
        public const float RefHitPctEnd = 0.08f;       // 段階40（20層）以降：8%
        public const float RefHitPctRamp = 40f;
        public const float RefPastCapGrowth = 0.015f;  // Lv上限の後（段階99〜）は、強化・3次職などで1段階ごとに 1.5% ずつ強くなる想定

        /// <summary>基準のキャラのレベル（＝その段階で落ちる装備のレベル）。</summary>
        public static int RefLevel(int wave) => Mathf.Clamp(wave, 1, Progression.MaxLevel);

        /// <summary>基準のキャラの最大 HP（体力の土台60、VIT、レベル、レアの鎧、HP の効果1つの半分）。</summary>
        public static float RefHp(int wave)
        {
            int lv = RefLevel(wave);
            float vit = 6f + (lv - 1) * Progression.PointsPerLevel * RefVitShare;
            return 60f + vit * 8f + (lv - 1) * 6f + (20f + 5f * lv) * 1.24f + 0.5f * (14f + 2f * lv) * 1.24f;
        }

        /// <summary>基準のキャラの防御（VIT と、防御の効果1つの半分）。</summary>
        public static float RefArmor(int wave)
        {
            int lv = RefLevel(wave);
            float vit = 6f + (lv - 1) * Progression.PointsPerLevel * RefVitShare;
            return vit * 0.5f + 0.5f * (10f + 2f * lv) * 1.24f;
        }

        /// <summary>基準のキャラの「実質の HP」（防御で減る分も入れる）。</summary>
        public static float RefEffectiveHp(int wave) =>
            RefHp(wave) * (100f + RefArmor(wave)) / 100f * (1f + RefPastCapGrowth * Mathf.Max(0, wave - Progression.MaxLevel));

        /// <summary>基準のキャラが体当たり1回で削られる割合。</summary>
        public static float RefHitPct(int wave) => Mathf.Lerp(RefHitPctStart, RefHitPctEnd, (wave - 1) / RefHitPctRamp);

        /// <summary>
        /// 体当たりのダメージ（防御で減る前）。基準のキャラの実質の HP × 削る割合
        /// （1層は約9、25層で約190、50層で約510、74層で約880）。精鋭・ボス・種類の倍率はこれに掛ける。
        /// </summary>
        public static int EnemyDamage(int wave) => Mathf.Max(1, Mathf.RoundToInt(RefEffectiveHp(wave) * RefHitPct(wave)));

        // 敵の波
        public const int MaxEnemies = 600; // 眠っている敵も含む（層に入った時点で全部の部屋に置く）
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
        // ---- 回復と MP の消費（後半ほどシビアに） ----
        public const float CombatTime = 4f;            // 攻撃を受けるか与えてから、これだけの秒は「戦闘中」
        public const float CombatHpRegenMult = 0.25f;  // 戦闘中の HP の自然回復の倍率
        public const float CombatMpRegenMult = 0.5f;   // 戦闘中の MP の自然回復の倍率
        public const float LifeStealCapPct = 0.02f;    // 吸収で回復できるのは、1秒あたり最大 HP のこの割合まで
        public const float SkillMpPerLevel = 0.02f;    // スキルの消費 MP：1レベルごとに +2%
        public const float SkillMpPerIndex = 0.12f;    // スキルの消費 MP：後ろのスキルほど、1つごとに +12%

        public const float MythicChance = 0.3f;        // ボスから神話（赤）の装備が落ちる確率
        public const float GoldenMythicChance = 0.1f;  // 黄金のスライム（レア）から神話の装備が落ちる確率
        public const float EliteMythicChance = 0.03f;  // 精鋭（レア敵）から神話の装備が落ちる確率（ボスより低い）
        public const float BossEnragedSpeedMult = 1.7f;   // 体力が半分を切ったら
        public const float BossEnragedSlamInterval = 2f;

        // 精鋭（第3波から。1波あたり 1 + 波/10 体くらい）
        public const int EliteFirstWave = 3;
        public static float EliteChance(int wave) => wave < EliteFirstWave ? 0f : Mathf.Min(0.02f + wave * 0.001f, 0.06f);
        public const float EliteHpMult = 4f;
        public const float EliteScale = 1.45f;

        // 敵の種類（体力・攻撃の倍率は、その層のスライムに対して）
        public const float BatHpMult = 0.5f, BatSpeed = 3.2f, BatDamageMult = 0.7f;
        public const float CasterHpMult = 0.8f, CasterSpeed = 1.7f, CasterRange = 4.5f, CasterFireInterval = 2.2f, BulletSpeed = 4.2f, BulletDamageMult = 0.35f; // 術師の弾は避けにくいので弱め
        public const float BruteHpMult = 2.5f, BruteSpeed = 1.1f, BruteDamageMult = 1.4f, BruteScale = 1.25f, BruteChargeSpeed = 7f, BruteChargeTime = 0.45f, BrutePrep = 0.6f, BruteChargeEvery = 3.5f;
        // 敵の段階（下級・中級・上級）。深い層ほど上級が増える
        public static readonly float[] TierHp = { 0.8f, 1.25f, 1.9f };
        public static readonly float[] TierDamage = { 0.85f, 1.1f, 1.3f };
        public static readonly float[] TierSpeed = { 0.95f, 1.05f, 1.12f };
        public static readonly float[] TierExp = { 0.8f, 1.3f, 2f };
        public static readonly float[] TierScale = { 1f, 1.1f, 1.2f };

        public static int RollEnemyTier(int floor)
        {
            float lower = Mathf.Max(0f, 70f - floor);
            float mid = Mathf.Min(50f, 30f + floor * 0.3f);
            float upper = Mathf.Max(0f, floor - 25f);
            float r = Random.value * (lower + mid + upper);
            if (r < lower) return 0;
            return r < lower + mid ? 1 : 2;
        }

        // 命中と回避（DEX で伸びる）。深い層の敵ほど、命中も回避も高い
        public static int EnemyAccuracy(int tier) => 40 + tier * 2;   // 敵の命中（プレイヤーの EVA と比べる）
        public static int EnemyEvasion(int tier) => 12 + tier;         // 敵の回避（プレイヤーの HIT と比べる）
        public const float MaxEvade = 0.6f;

        // 敵の種類ごとの命中・回避の倍率（スライム・コウモリ・術師・岩鬼・爆弾虫・黄金）
        public static readonly float[] KindAcc = { 1f, 0.9f, 1.2f, 1.25f, 0.8f, 0f };
        public static readonly float[] KindEva = { 1f, 1.8f, 1f, 0.5f, 0.7f, 2.5f };
        public static readonly float[] TierAccEva = { 0.9f, 1.05f, 1.2f };
        /// <summary>囲まれ補正：近く（2マス以内）の起きた敵が多いほど、避けにくい。1体で100%、1体増えるごとに -20%。</summary>
        public static float CrowdEvadeMult(int nearby) => Mathf.Clamp01(1f - 0.2f * (Mathf.Max(1, nearby) - 1));
        public const float CrowdRadius = 2f;

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
        public const float EmberMult = 1.5f; // 火種の量の倍率（少なかったので 1.5倍にした）
        // 特別な相手・宝箱の火種（1粒の量の倍率）。強化・再抽選に使う量に見合うように多めにした
        public const int GoldenEmberMult = 6;  // 黄金のスライム（前は2）
        public const int ChestEmberMult = 3;   // 宝箱（前は1）
        public const int BossEmberMult = 5;    // ボス（前は3）
        public const int EliteEmberOrbs = 6;   // 精鋭の火種の粒の数（前は4）
        public static int EmberAmount(int wave) => Mathf.Max(1, Mathf.RoundToInt(EmberMult * (1f + wave / 10f)));
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
