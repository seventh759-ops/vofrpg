using UnityEngine;

namespace VividOfFade
{
    /// <summary>プレイヤー。移動・回避。通常攻撃は AttackSystem。能力は職業と装備から計算した PlayerStats を使う。</summary>
    public sealed class Player : MonoBehaviour
    {
        public SpriteRenderer Sr;
        public int Hp { get; private set; }
        public int MaxHp { get; private set; } = GameConfig.PlayerMaxHp;
        public int Mp => Mathf.FloorToInt(mp);
        public int MaxMp { get; private set; }
        public bool IsDead => Hp <= 0;
        public bool AutoAttack = true;
        /// <summary>今の能力（スキルの強化を含む）。</summary>
        public PlayerStats Stats { get; private set; }

        /// <summary>職業・レベル・装備から計算した能力（スキルの強化を含まない）。</summary>
        public PlayerStats BaseStats { get; private set; }

        sealed class ActiveBuff
        {
            public SkillSpec Spec;
            public float Power;
            public float Remaining;
        }

        readonly System.Collections.Generic.List<ActiveBuff> buffs = new System.Collections.Generic.List<ActiveBuff>();

        /// <summary>かかっている強化（HUD の表示用）。</summary>
        public int BuffCount => buffs.Count;
        public string BuffName(int i) => buffs[i].Spec.DisplayName;
        public float BuffRemaining(int i) => buffs[i].Remaining;

        /// <summary>向いている方向（論理座標）。押している間だけ攻撃で、敵がいないときに使う。</summary>
        public Vector2 FacingLogical => Iso.ToLogical(facing).normalized;

        /// <summary>回避した（論理座標の位置）。ボス装備の固有効果に使う。</summary>
        public event System.Action<Vector2> Dodged;

        /// <summary>精鋭（Shroud）に奪われている明るさ。</summary>
        public float LightPenalty { get; set; }

        /// <summary>実際の明るさ（通常攻撃の届く範囲・ランタンの光）。</summary>
        public float EffectiveLight => Stats == null ? GameConfig.BaseLight : Mathf.Max(GameConfig.MinLight, Stats.Light - LightPenalty);

        public float DodgeReady => 1f - Mathf.Clamp01(dodgeCooldown / (Stats != null ? Stats.DodgeCooldown : GameConfig.DodgeCooldown));

        Vector2 pos;

        /// <summary>論理座標。画面にはクォータービューに変換して描く。</summary>
        public Vector2 Pos
        {
            get => pos;
            set
            {
                pos = value;
                transform.position = Iso.ToScreen(value);
            }
        }

        // 入力は画面の向き（上を押すと画面の上へ）。移動するときに論理座標へ戻す
        Vector2 facing = Vector2.down;
        Vector2 dashDir;
        float dashTimer;
        float dodgeCooldown;
        float invuln;
        float hurtTimer;
        float mp;
        float regenHp; // 体力の自然回復の端数

        /// <summary>祭壇で得た加護（番号）。挑戦のあいだずっと効く。</summary>
        public readonly System.Collections.Generic.List<int> Boons = new System.Collections.Generic.List<int>();

        public void ResetState(PlayerStats stats)
        {
            buffs.Clear();
            Boons.Clear();
            BaseStats = stats;
            RebuildStats();
            stats = Stats;
            MaxHp = stats.MaxHp;
            Hp = MaxHp;
            MaxMp = stats.MaxMp;
            mp = MaxMp;
            regenHp = 0f;
            Pos = Vector2.zero;
            facing = Vector2.down;
            dashTimer = 0f;
            dodgeCooldown = 0f;
            invuln = 0f;
            hurtTimer = 0f;
            Sr.color = Color.white;
            Sr.sortingOrder = SortOrder.For(Vector2.zero);
        }

        /// <summary>装備が変わったとき。最大体力が増えた分は今の体力にも足す。</summary>
        public void ApplyStats(PlayerStats stats)
        {
            BaseStats = stats;
            RebuildStats();
            ResizePools();
        }

        /// <summary>加護を得る。体力・MP の上限が増えた分は今の値にも足す。</summary>
        public void AddBoon(int index)
        {
            Boons.Add(index);
            RebuildStats();
            ResizePools();
        }

        /// <summary>中断した挑戦を再開するとき、加護と体力・MP の割合を戻す。</summary>
        public void RestoreRun(System.Collections.Generic.List<int> boons, float hpRatio, float mpRatio)
        {
            Boons.Clear();
            Boons.AddRange(boons);
            RebuildStats();
            MaxHp = Stats.MaxHp;
            MaxMp = Stats.MaxMp;
            Hp = Mathf.Clamp(Mathf.RoundToInt(MaxHp * hpRatio), 1, MaxHp);
            mp = Mathf.Clamp(MaxMp * mpRatio, 0f, MaxMp);
        }

        void ResizePools()
        {
            int gained = Stats.MaxHp - MaxHp;
            int gainedMp = Stats.MaxMp - MaxMp;
            MaxHp = Stats.MaxHp;
            Hp = Mathf.Clamp(Hp + Mathf.Max(0, gained), IsDead ? 0 : 1, MaxHp);
            MaxMp = Stats.MaxMp;
            mp = Mathf.Clamp(mp + Mathf.Max(0, gainedMp), 0f, MaxMp);
        }

        /// <summary>スキルの強化をかける。同じスキルなら時間を延ばす。</summary>
        public void AddBuff(SkillSpec spec, float power)
        {
            foreach (var b in buffs)
            {
                if (b.Spec != spec) continue;
                b.Power = power;
                b.Remaining = spec.Duration;
                RebuildStats();
                return;
            }
            buffs.Add(new ActiveBuff { Spec = spec, Power = power, Remaining = spec.Duration });
            RebuildStats();
        }

        void RebuildStats()
        {
            var s = BaseStats;
            foreach (int k in Boons) s = s.WithBoon(VividOfFade.Boons.Get(k));
            foreach (var b in buffs) s = s.WithBuff(b.Spec, b.Power);
            Stats = s;
        }

        /// <summary>スキルで突進する（論理座標の向き・距離）。少しの間、無敵になる。</summary>
        public void SkillDash(Vector2 dir, float distance, Room room)
        {
            Pos = room.Move(Pos, dir * distance, GameConfig.PlayerRadius); // 壁を突き抜けない
            invuln = Mathf.Max(invuln, 0.3f);
            var screen = Iso.ToScreen(dir);
            if (screen.sqrMagnitude > 0.0001f) facing = screen.normalized;
        }

        /// <summary>レベルが上がったとき。体力と MP を全回復する。</summary>
        public void Refill()
        {
            if (IsDead) return;
            Hp = MaxHp;
            mp = MaxMp;
        }

        /// <summary>MP を使う。足りなければ false（スキルで使う）。</summary>
        public bool SpendMp(int amount)
        {
            if (mp < amount) return false;
            mp -= amount;
            return true;
        }

        /// <summary>MP を回復する（魔力の薬）。</summary>
        public void RestoreMp(float amount)
        {
            if (IsDead) return;
            mp = Mathf.Min(MaxMp, mp + amount);
        }

        public void Heal(int amount)
        {
            if (IsDead) return;
            Hp = Mathf.Min(MaxHp, Hp + amount);
        }

        /// <summary>ダメージを受けたら true。無敵中は受けない。防御で減らす。</summary>
        public bool TakeHit(int damage, string source = "?")
        {
            if (invuln > 0f || IsDead) return false;
            float reduced = damage * Stats.DamageTaken * 100f / (100f + Stats.Armor);
            int dealt = Mathf.Max(1, Mathf.RoundToInt(reduced));
            Hp = Mathf.Max(0, Hp - dealt);
            invuln = GameConfig.HitInvuln;
            hurtTimer = GameConfig.HitInvuln;
            if (LogDamage)
                Debug.Log($"[Damage] {source}: raw {damage} -> {dealt} (armor {Stats.Armor:0}, taken x{Stats.DamageTaken:0.00})  HP {Hp}/{MaxHp}" + (IsDead ? "  DEAD" : ""));
            return true;
        }

        /// <summary>受けたダメージをコンソールに出す（デバッグ用）。</summary>
        public static bool LogDamage = true;


        public void Tick(float dt, PlayerInputReader input, Room room)
        {
            dodgeCooldown = Mathf.Max(0f, dodgeCooldown - dt);
            invuln = Mathf.Max(0f, invuln - dt);
            hurtTimer = Mathf.Max(0f, hurtTimer - dt);

            // スキルの強化の残り時間
            bool expired = false;
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                buffs[i].Remaining -= dt;
                if (buffs[i].Remaining > 0f) continue;
                buffs.RemoveAt(i);
                expired = true;
            }
            if (expired) RebuildStats();

            // 自然回復
            mp = Mathf.Min(MaxMp, mp + Stats.MpRegen * dt);
            regenHp += Stats.HpRegen * dt;
            if (regenHp >= 1f)
            {
                int whole = Mathf.FloorToInt(regenHp);
                regenHp -= whole;
                Heal(whole);
            }

            Vector2 move = input.Move;
            if (move.sqrMagnitude > 0.0001f) facing = move.normalized;

            if (input.ConsumeDodge(out var dodgeDir) && dodgeCooldown <= 0f && dashTimer <= 0f)
            {
                dashDir = dodgeDir.sqrMagnitude > 0.0001f ? dodgeDir.normalized : facing;
                dashTimer = GameConfig.DodgeTime;
                dodgeCooldown = Stats.DodgeCooldown;
                invuln = Mathf.Max(invuln, GameConfig.DodgeInvuln);
                Dodged?.Invoke(Pos);
            }

            bool dashing = dashTimer > 0f;
            Vector2 screenVelocity = dashing ? dashDir * GameConfig.DodgeSpeed : move * Stats.MoveSpeed;
            dashTimer = Mathf.Max(0f, dashTimer - dt);
            Pos = room.Move(Pos, Iso.ToLogical(screenVelocity) * dt, GameConfig.PlayerRadius);

            if (Mathf.Abs(screenVelocity.x) > 0.01f) Sr.flipX = screenVelocity.x < 0f;
            Sr.sortingOrder = SortOrder.For(Pos);
            if (dashing)
            {
                Sr.color = new Color(0.7f, 0.85f, 1f, 0.8f);
            }
            else if (hurtTimer > 0f)
            {
                Sr.color = Mathf.Repeat(Time.time * 20f, 1f) < 0.5f ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
            }
            else
            {
                Sr.color = Color.white;
            }
        }

        /// <summary>職業の見た目に切り替える。</summary>
        public void SetClass(ClassType type)
        {
            Sr.sprite = PixelArt.PlayerFor(type);
        }
    }
}
