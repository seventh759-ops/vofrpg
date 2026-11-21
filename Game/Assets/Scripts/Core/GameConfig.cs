using UnityEngine;

namespace VividOfFade
{
    /// <summary>調整用の数値。実際に遊んでから変える前提の仮の値。</summary>
    public static class GameConfig
    {
        // 部屋（マス）。1マス = 16ドット = 1ユニット
        // 塔：5層ごとにボスの層。敵の強さ・経験値・ドロップは「段階」（層×2）で決める（試作の波の数と同じ尺度）
        public const int BossFloorEvery = 5;
        public const float BossTriggerInset = 3.5f;    // ボスの区画の縁から何マス入ったら、ボスとお供が現れるか
        public const float BossIntroHold = 2.2f;       // ボスとお供が現れてから動き出すまで（名前が出て、区画が明るくなる間）
        public const float MinionRespawnHold = 0.8f;   // 沸き直したお供が動き出すまで
        public const float BossMinionRespawn = 12f;    // お供が減っていたら、この秒数ごとに最初の数まで沸き直す（ボスが生きている間）
        // 祭壇：ボスの層の1つ前には必ず1つ。ほかの層に出る確率（0 なら出ない）
        public const float AltarChanceOther = 0f;
        public const float SpringChance = 0.3f; // 癒やしの泉（HP と MP を満タンに戻すだけ）が普通の層に出る確率
        public static bool AltarOnFloor(int floor) =>
            !IsBossFloor(floor) && (IsBossFloor(floor + 1) || (AltarChanceOther > 0f && ((floor * 7919 + 13) % 1000) < AltarChanceOther * 1000f));
        public const int MaxFloor = 100; // 塔の最後の層（ボス）

        // 深淵（アビス）：塔の底のさらに先。101〜120層として扱い、画面には「深淵1〜20層」と出す。
        // 100層のボスを倒し、Lv100 以上なら入れる（町の層選び・100層の階段から）。ここでだけ Lv100〜120 の装備が落ちる
        public const int AbyssFirstFloor = 101;
        public const int AbyssLastFloor = 120;
        public const int AbyssReqLevel = 100;
        public static bool IsAbyss(int floor) => floor > MaxFloor && !Ascent; // 捧げたキャラクターでは、101〜120層は天界
        public static int AbyssDepth(int floor) => floor - MaxFloor;

        // 天界（クリア後）：昇る塔の100層（女神像）を倒すと、その上に開く。深淵と同じ101〜120層の番号を使い、画面には「天界1〜20層」と出す
        public static bool IsHeaven(int floor) => floor > MaxFloor && Ascent;
        /// <summary>塔（1〜100層）の外か（深淵か天界）。</summary>
        public static bool IsBeyond(int floor) => floor > MaxFloor;
        public static bool CanEnterHeaven(CharacterData ch) => ch != null && ch.Ascended && ch.AscentBossCleared >= MaxFloor;
        public static bool CanEnterAbyss(CharacterData ch) => ch != null && !ch.Ascended && ch.Level >= AbyssReqLevel && ch.BossCleared >= MaxFloor; // 捧げたら深淵は閉じる

        // 昇る塔（クリア後）：女神像に火種を捧げたキャラクターは、同じ1〜100層を「昇る」。深淵は閉じる。
        // 捧げるとレベルも Lv1 に戻る。昇る塔の敵は1層で Lv24 相当から始まり、100層で塔の100層と同じ（Lv99 相当）に届く。体力・攻撃はさらに倍率を掛ける。
        // 落ちる装備は段階に合わせる（塔と同じ決め方）。天界（101〜120層）は深淵と同じ強さ・Lv100〜120 の装備。
        public const int AscentStartTier = 24;   // 1層の敵の強さ（前は 10。捧げても装備・転生・スキルは残るので、手応えが出るように上げた）
        /// <summary>昇る塔・天界の敵の体力と攻撃の倍率（捧げたあとは装備・転生・スキルが残っていて楽すぎるため）。</summary>
        public const float AscentHpMult = 1.6f, AscentDamageMult = 1.4f;
        /// <summary>いまのキャラクターが昇る塔か（GameWorld が毎フレーム入れる）。</summary>
        public static bool Ascent;
        /// <summary>その層の「深さ」の目安（敵の数・段階の割合に使う）。昇る塔も塔と同じ。</summary>
        public static int EffectiveFloor(int floor) => floor;

        /// <summary>層の名前（「74層」「深淵3層」）。</summary>
        public static string FloorName(int floor) => FloorName(floor, Ascent);
        public static string FloorName(int floor, bool ascent) =>
            floor > MaxFloor ? (ascent ? Loc.T($"HEAVEN {AbyssDepth(floor)}", $"天界{AbyssDepth(floor)}層") : Loc.T($"ABYSS {AbyssDepth(floor)}", $"深淵{AbyssDepth(floor)}層"))
            : Loc.T($"FLOOR {floor}", $"{floor}層"); // 昇る塔も、層の名前は塔と同じ

        /// <summary>層の短い名前（HUD の層の行：「71F」「ABYSS 3F」）。言語に関係なく同じ。</summary>
        public static string FloorNameEn(int floor) => IsHeaven(floor) ? $"HEAVEN {AbyssDepth(floor)}F" : IsAbyss(floor) ? $"ABYSS {AbyssDepth(floor)}F" : $"{floor}F";

        /// <summary>そのキャラクターが下りられる最後の層（深淵に入れなければ100層）。</summary>
        public static int LastFloorFor(CharacterData ch) => CanEnterAbyss(ch) || CanEnterHeaven(ch) ? AbyssLastFloor : MaxFloor;
        public static bool IsBossFloor(int floor) => floor > 0 && floor % BossFloorEvery == 0;
        public static int Tier(int floor) => Ascent && floor <= MaxFloor
            ? Mathf.RoundToInt(AscentStartTier + (Mathf.Max(1, floor) - 1) * (MaxFloor * 2f - AscentStartTier) / (MaxFloor - 1)) // 昇る塔：1層 24 → 100層 200
            : floor * 2;
        /// <summary>
        /// 経験値の目安にする段階。昇る塔は敵を強くした（1層 Lv24 相当から）が、もらえる経験値は前のまま（1層 Lv10 相当から）にする。
        /// </summary>
        public const int AscentExpStartTier = 10;
        public static int ExpTier(int floor) => Ascent && floor <= MaxFloor
            ? Mathf.RoundToInt(AscentExpStartTier + (Mathf.Max(1, floor) - 1) * (MaxFloor * 2f - AscentExpStartTier) / (MaxFloor - 1))
            : floor * 2;
        /// <summary>
        /// 格下の敵の経験値を減らす倍率。敵の強さ（経験値の段階＝層×2）より自分のレベルが高いほど減る。
        /// 差が ExpGraceLevels までは減らない。そこから1レベルごとに ExpPenaltyPerLevel ずつ減り、ExpMinMult より下にはならない。格上の敵は増やさない（1倍）。
        /// </summary>
        public const int ExpGraceLevels = 5;
        public const float ExpPenaltyPerLevel = 0.05f;
        public const float ExpMinMult = 0.1f;
        public static float ExpLevelMult(int playerLevel, int enemyTier)
        {
            int over = playerLevel - enemyTier - ExpGraceLevels;
            return over <= 0 ? 1f : Mathf.Max(ExpMinMult, 1f - over * ExpPenaltyPerLevel);
        }
        public static int RoomEnemies(int floor, int roomArea) => Mathf.Min(5 + Mathf.RoundToInt(EffectiveFloor(floor) * 1.2f) + roomArea / 14, 45); // 前の約6割の密度
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
        public static float EnemyHp(int wave) => 20f * (1f + 0.3f * (wave - 1)) * Mathf.Pow(EnemyHpGrowth, wave - 1) * (Ascent ? AscentHpMult : 1f);
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
        public const float RefHitPctStart = 0.055f;    // 序盤：体当たり1回で HP の 5.5%（前は 4.5%。序盤から守りの装備・VIT が効くように）
        public const float RefHitPctEnd = 0.08f;       // 段階40（20層）以降：8%
        public const float RefHitPctRamp = 40f;
        public const float RefPastCapGrowth = 0.015f;  // Lv上限の後（段階99〜）は、強化・3次職などで1段階ごとに 1.5% ずつ強くなる想定

        /// <summary>基準のキャラのレベル（＝その段階で落ちる装備のレベル）。</summary>
        // 敵の強さの基準のキャラの装備レベル。深淵では、落ちる装備は5刻みだが、敵はなめらかに強くなるよう 100→120 を層ごとに少しずつ
        public static int RefLevel(int wave) => wave / 2 > MaxFloor // 深淵・昇る塔・天界（段階 202〜）
            ? Progression.MaxLevel + 1 + Mathf.RoundToInt((wave / 2 - AbyssFirstFloor) * (Progression.UnlockedMaxLevel - Progression.MaxLevel - 1) / (float)(AbyssLastFloor - AbyssFirstFloor))
            : Mathf.Clamp(wave, 1, Progression.MaxLevel);

        /// <summary>基準のキャラの最大 HP（体力の土台60、VIT、レベル、レアの鎧、HP の効果1つの半分）。</summary>
        public static float RefHp(int wave)
        {
            int lv = RefLevel(wave);
            float vit = 6f + (lv - 1) * Progression.PointsPerLevel * RefVitShare;
            float gear = 1.24f * GearLevelMult(lv); // 深淵では、深淵の装備（数値が高い）を付けている想定
            return 60f + vit * 8f + (lv - 1) * 6f + (20f + 5f * lv) * gear + 0.5f * (14f + 2f * lv) * gear;
        }

        /// <summary>基準のキャラの防御（VIT と、防御の効果1つの半分）。</summary>
        public static float RefArmor(int wave)
        {
            int lv = RefLevel(wave);
            float vit = 6f + (lv - 1) * Progression.PointsPerLevel * RefVitShare;
            return vit * 0.5f + 0.5f * (10f + 2f * lv) * 1.24f * GearLevelMult(lv);
        }

        /// <summary>基準のキャラの「実質の HP」（防御で減る分も入れる）。</summary>
        public static float RefEffectiveHp(int wave) =>
            RefHp(wave) * (100f + RefArmor(wave)) / 100f * (1f + RefPastCapGrowth * Mathf.Clamp(wave - Progression.MaxLevel, 0, Tier(MaxFloor) - Progression.MaxLevel)); // 深淵では、伸びは装備とレベルで見る

        /// <summary>基準のキャラが体当たり1回で削られる割合。</summary>
        public static float RefHitPct(int wave) => Mathf.Lerp(RefHitPctStart, RefHitPctEnd, (wave - 1) / RefHitPctRamp);

        /// <summary>
        /// 体当たりのダメージ（防御で減る前）。基準のキャラの実質の HP × 削る割合
        /// （1層は約9、25層で約190、50層で約510、74層で約880）。精鋭・ボス・種類の倍率はこれに掛ける。
        /// </summary>
        public static int EnemyDamage(int wave) => Mathf.Max(1, Mathf.RoundToInt(RefEffectiveHp(wave) * RefHitPct(wave) * (Ascent ? AscentDamageMult : 1f)));

        // 敵の波
        public const int MaxEnemies = 600; // 眠っている敵も含む（層に入った時点で全部の部屋に置く）
        public const float WaveSpawnDuration = 2.5f;
        public const float WaveGap = 3f;

        public static int WaveSize(int wave) => Mathf.Min(15 + wave * 10, 120);

        // ボス（10波ごと）
        public static bool IsBossWave(int wave) => wave > 0 && wave % 10 == 0;
        /// <summary>
        /// ボスの技で、防御で減らせない固定の分（元のダメージに対する割合）。防御・被ダメージの軽減をどれだけ上げても、少なくともこの割合は受ける。
        /// 守りを固めすぎて、時間をかければ楽に勝ててしまうのを防ぐ。ボスの技と、ボスの体当たり・突進に付ける（雑魚と弾には付けない）。
        /// </summary>
        public const float FixedDamageShare = 0.25f;
        /// <summary>受けるダメージの倍率：固定の分 ＋ 残りを被ダメージ倍率と防御で減らした分。</summary>
        public static float TakenMult(float armor, float damageTaken) =>
            FixedDamageShare + (1f - FixedDamageShare) * damageTaken * 100f / (100f + Mathf.Max(0f, armor));
        /// <summary>
        /// ボスの技（叩きつけ・衝撃・扇・部屋全体）で上乗せする、最大 HP の割合のダメージ（防御で減らない）。
        /// 通常の攻撃（体当たり・弾）には付けない。怒ったら増える。呼び出された歴代のボスは少なめ。
        /// </summary>
        public const float BossPctHit = 0.06f, BossPctHitEnraged = 0.09f, SummonedBossPct = 0.03f;
        public static float BossPctDamage(Enemy e) => e == null ? 0f : e.Summoned ? SummonedBossPct : e.Enraged ? BossPctHitEnraged : BossPctHit;
        /// <summary>ボスの通常攻撃（体当たり・突進）の最低ダメージ。防御で減らしてもこれより少なくならない（ただし防御で減らす前のダメージが上限なので、序盤は自然に小さい）。</summary>
        public const int BossMinHit = 100;
        public const float BossHpMult = 180f;          // 同じ波の雑魚の何倍か（前は 120）

        /// <summary>
        /// 序盤の手ごたえ（0〜1）：塔の5〜20層を山に、ボスと精鋭を少し硬く・痛くする（1〜4層で上がり、20〜30層で元に戻る）。
        /// 能力値の振り方や装備の選び方で、序盤から差が出るように。昇る塔・深淵・天界には掛けない。
        /// </summary>
        public static float EarlyBite(int floor)
        {
            if (Ascent || floor > 30 || floor < 1) return 0f;
            if (floor < 5) return floor / 5f;
            if (floor <= 20) return 1f;
            return (30 - floor) / 10f;
        }
        public static float EarlyBiteFromTier(int tier) => EarlyBite(tier / 2);
        public const float EarlyBossHp = 0.4f, EarlyBossDamage = 0.25f, EarlyEliteHp = 0.35f;
        public const float BossDamageMult = 3.75f;     // 前は 2.5
        public const float BossScale = 3f;
        public const float BossSpeed = 1.25f;
        public const float BossSlamInterval = 3.4f;
        public const float BossWindup = 0.8f;
        public const float BossSlamRadius = 2.6f;
        // 深淵の最奥のボス「アビス」：ほかのボスより強い。歴代のボスを呼び出す
        public const float PlayerScale = 1.15f;            // プレイヤーの見た目の大きさ（当たり判定は変えない。足元の位置はそのまま）
        public const float FinalBossHpMult = 2.5f;          // ふつうのボスの何倍か
        public const float FinalBossDamageMult = 1.3f;
        public const float SummonedBossHpShare = 0.06f;     // 呼び出したボスの体力（アビスの最大体力に対して）
        public const float SummonedBossDamageShare = 0.7f;  // 呼び出したボスの攻撃（アビスに対して）
        public const int MaxSummonedBosses = 2;             // 同時に出せる数
        public const float FinalCollapseTime = 3.5f;        // アビスが崩れていく演出の長さ（そのあとエンディング）
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
            floor = EffectiveFloor(floor);
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

        // 会心の耐性：深い層の敵ほど会心を受けにくい。実際の会心率 = 自分の会心率 − 敵の耐性（100% を超えた会心率も無駄にならない）
        public const float CritResistPerTier = 0.0015f; // 50層で15%、100層で30%、深淵の最奥で36%
        public const float MaxCritChance = 1.5f;        // 会心率そのものの上限（150%）
        public static float EnemyCritResist(int tier) => Mathf.Max(0, tier) * CritResistPerTier;
        /// <summary>いまの敵の会心耐性（GameWorld が毎フレーム入れる。町では最深層の値）。</summary>
        public static float CritResist;

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
        // コウモリの急降下：近くで一瞬止まって身構え、まっすぐ速く突っ込む
        public const float BatDiveRange = 4f, BatDiveEvery = 3.5f, BatDivePrep = 0.4f, BatDiveTime = 0.35f, BatDiveSpeed = 7f;
        // スライム（さまよう本も）の跳びかかり：近くで身を縮め、ぴょんと跳ぶ
        public const float SlimePounceRange = 2.4f, SlimePounceEvery = 2.8f, SlimePouncePrep = 0.3f, SlimePounceTime = 0.25f, SlimePounceSpeed = 6f;
        public const float BomberHpMult = 0.7f, BomberSpeed = 2.4f, BomberFuse = 0.8f, BomberRadius = 1.8f, BomberDamageMult = 2.5f;
        public const int EliteFountainItems = 2;
        public const float EliteRarityBonus = 0.8f;
        public const float ShroudRange = 5f;      // この距離にいると明るさを奪う
        public const int SplitCount = 3;       // ボスから Mythic 装備が落ちる確率
        public const int BossFountainItems = 8;
        public const float BossRarityBonus = 1.5f;

        // ドロップ
        // 深淵の欠片（深淵の装備の強化に使う。深淵の精鋭とボスからだけ手に入る）
        // 落ちると 1〜3 個（ランダム）
        public const float ShardChanceBoss = 0.6f;    // 深淵のボス（アビスも）
        public const float ShardChanceElite = 0.1f;   // 深淵の精鋭
        public const int ShardMin = 1, ShardMax = 3;
        public static int RollShards() => UnityEngine.Random.Range(ShardMin, ShardMax + 1);

        public const float ItemDropMult = 0.6f;      // 装備の落ちる量の全体の倍率（多すぎたので減らし、その分火種を増やした）
        public const float ItemDropChance = 0.04f * ItemDropMult;   // 雑魚1体あたり
        public const float EmberDropChance = 0.3f;
        public const float MagnetRange = 2.2f;
        public const float VacuumDelay = 1.2f;   // 波を全滅させてから、地面のドロップを全部吸い寄せるまで
        public const float PickupRange = 0.35f;

        // 噴水の数は上限を設ける。火種は粒の数を増やしすぎず、1粒の量で増やす
        public static int FountainItems(int wave) => Mathf.Min(3 + wave / 2, 10);
        public static int FountainEmberOrbs(int wave) => Mathf.Min(6 + wave, 16);
        public const float EmberMult = 2.1f; // 火種の量の倍率（1.5倍 → 装備の落ちる量を減らした分、2.1倍に）
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
        /// <summary>深淵のその層で落ちる装備のレベル（100・105・110・115・120 のどれか）。</summary>
        public static int AbyssGearLevel(int floor)
        {
            int depth = Mathf.Clamp(floor - AbyssFirstFloor + 1, 1, AbyssLastFloor - AbyssFirstFloor + 1);
            return Mathf.Min(Progression.UnlockedMaxLevel, 100 + 5 * (depth / 5));
        }

        /// <summary>
        /// 1つずつの品のレベルに幅を持たせる：上限（DropLevel）から、自分のレベル −1 までのどれか。
        /// 上限で止まっても（Lv98 など）、いろいろなレベルの品が落ちるように。深淵・天界（Lv100〜）は決まったレベルのまま。
        /// </summary>
        public static int JitterDropLevel(int top)
        {
            if (top > Progression.MaxLevel) return top;
            int ch = Profile.Current != null ? Profile.Current.Level : 1;
            int lo = Mathf.Max(1, Mathf.Min(top, ch - 1));
            return Random.Range(lo, top + 1);
        }

        public static int DropLevel(int wave, int characterLevel)
        {
            int floor = wave / 2;
            if (IsBeyond(floor)) // 深淵・天界
            {
                // 深淵の装備は、レベルが5刻みで固定：深淵1〜4層 Lv100／5〜9層 Lv105／10〜14層 Lv110／15〜19層 Lv115／20層 Lv120
                // （区画の最後のボスの層から、次の段の装備が出る。自分のレベルより上の装備も落ちる＝付けられるようになるのが目標）
                return AbyssGearLevel(floor);
            }
            return Mathf.Clamp(wave, 1, Mathf.Min(characterLevel + DropLevelAhead, Progression.MaxLevel)); // 塔では Lv99 を超える装備は出さない
        }

        /// <summary>深淵の装備（Lv100以上）の数値の倍率。基本の数値と効果の値に掛ける。</summary>
        public const float AbyssGearMult = 1.3f;
        public static float GearLevelMult(int itemLevel) => itemLevel > Progression.MaxLevel ? AbyssGearMult : 1f;
    }
}
