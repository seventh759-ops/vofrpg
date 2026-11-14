using System;
using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// スキル。ボタン3つ（PC は 1〜3 キー）で使う。ボタンごとに「手動」「オート（待ち時間が明けたら自動で使う）」を選べる。
    /// レベルで自動で覚え、使った回数でスキルレベルが上がる（威力・回復量・待ち時間が良くなる）。
    /// </summary>
    public sealed class SkillSystem : MonoBehaviour
    {
        sealed class PendingStrike
        {
            public Vector2 Pos;
            public float Timer;
            public float Damage;
            public float Radius;
            public Color Color;
            public SkillSpec Spec;
        }

        EnemySystem enemies;
        ProjectileSystem shots;
        FxSystem fx;
        Sfx sfx;
        Room room;

        readonly float[] cooldowns = new float[SkillInfo.Slots];
        readonly List<PendingStrike> strikes = new List<PendingStrike>();

        /// <summary>スキルレベルが上がった（スキル名・新しいレベル）。</summary>
        public event Action<string, int> SkillLeveled;
        /// <summary>新しいスキルを覚えた（スキル名）。</summary>
        public event Action<string> SkillLearned;

        /// <summary>スキルを使った。</summary>
        public event Action<SkillSpec> Casted;

        /// <summary>画面を揺らす（強さ、時間）。GameWorld が入れる。</summary>
        public Action<float, float> Shake;

        public void Init(EnemySystem enemies, ProjectileSystem shots, FxSystem fx, Sfx sfx, Room room)
        {
            this.enemies = enemies;
            this.shots = shots;
            this.fx = fx;
            this.sfx = sfx;
            this.room = room;
        }

        public void ResetState()
        {
            for (int i = 0; i < cooldowns.Length; i++) cooldowns[i] = 0f;
            strikes.Clear();
        }

        /// <summary>ボタンの待ち時間の残り（0〜1。0 で使える）。</summary>
        public float CooldownRatio(int slot)
        {
            var ch = Profile.Current;
            if (ch == null || ch.Slots[slot] < 0) return 0f;
            var spec = SkillInfo.Get(ClassInfo.Current, ch.Slots[slot]);
            float full = spec.Cooldown * SkillInfo.CooldownMult(ch.SkillLv[ch.Slots[slot]]);
            return full > 0f ? Mathf.Clamp01(cooldowns[slot] / full) : 0f;
        }

        public void Tick(float dt, Player player, int manualSlot)
        {
            for (int i = 0; i < cooldowns.Length; i++) cooldowns[i] = Mathf.Max(0f, cooldowns[i] - dt);
            UpdateStrikes(dt);

            var ch = Profile.Current;
            if (ch == null || player.IsDead) return;

            if (manualSlot >= 0 && manualSlot < SkillInfo.Slots) TryCast(manualSlot, player, ch, false);
            for (int i = 0; i < SkillInfo.Slots; i++)
            {
                if (ch.SlotAuto[i]) TryCast(i, player, ch, true);
            }
        }

        /// <summary>ボタンを押したとき（HUD から）。</summary>
        public void Press(int slot, Player player)
        {
            var ch = Profile.Current;
            if (ch == null || player.IsDead) return;
            TryCast(slot, player, ch, false);
        }

        void TryCast(int slot, Player player, CharacterData ch, bool auto)
        {
            int index = ch.Slots[slot];
            if (index < 0 || cooldowns[slot] > 0f || player.Stunned) return; // 気絶中はスキルも出ない
            if (!SkillInfo.Learned(ClassInfo.Current, index, ch.Level)) return;

            var spec = SkillInfo.Get(ClassInfo.Current, index);
            // オートは、使って意味のあるときだけ使う
            if (auto && !WorthUsing(spec, player)) return;
            if (!player.SpendMp(SkillInfo.MpCost(spec, index, ch.Level))) return;

            int skillLevel = ch.SkillLv[index];
            Cast(spec, SkillInfo.Power(skillLevel), player);
            cooldowns[slot] = spec.Cooldown * SkillInfo.CooldownMult(skillLevel) * player.Stats.CooldownMult;
            GainUse(ch, index, spec);
        }

        bool WorthUsing(SkillSpec spec, Player player)
        {
            switch (spec.Effect)
            {
                case SkillEffect.Heal: return player.Hp < player.MaxHp * 0.6f;
                case SkillEffect.Nova: return enemies.FindNearest(player.Pos, spec.Radius) != null;
                case SkillEffect.Buff: return enemies.FindNearest(player.Pos, 5f) != null;
                default: return enemies.FindNearest(player.Pos, spec.Range) != null;
            }
        }

        void GainUse(CharacterData ch, int index, SkillSpec spec)
        {
            if (ch.SkillLv[index] >= SkillInfo.MaxSkillLevel) return;
            ch.SkillUses[index]++;
            if (ch.SkillUses[index] < SkillInfo.UsesToNext(ch.SkillLv[index])) return;
            ch.SkillUses[index] = 0;
            ch.SkillLv[index]++;
            SkillLeveled?.Invoke(spec.DisplayName, ch.SkillLv[index]);
        }

        /// <summary>レベルが上がったときに呼ぶ。新しく覚えたスキルを空いているボタンへ付ける。</summary>
        public void OnLevelChanged(CharacterData ch, int oldLevel)
        {
            for (int i = 0; i < SkillInfo.PerClass; i++)
            {
                var spec = SkillInfo.Get(ClassInfo.Current, i);
                if (oldLevel >= spec.ReqLevel || !SkillInfo.Learned(ClassInfo.Current, i, ch.Level)) continue;
                Learn(ch, i);
            }
        }

        /// <summary>スキルを覚えた（通知して、空いているボタンへ付ける）。転職のときにも使う。</summary>
        public void Learn(CharacterData ch, int index)
        {
            SkillLearned?.Invoke(SkillInfo.Get(ClassInfo.Current, index).DisplayName);
            if (Array.IndexOf(ch.Slots, index) >= 0) return;
            int empty = Array.IndexOf(ch.Slots, -1);
            if (empty >= 0) ch.Slots[empty] = index;
        }

        /// <summary>
        /// 魔法の職業（灯術師・灯番。主能力が STR ではない）のスキルにかかる能力値の補正。
        /// INT は魔法の威力全体を 1 ポイント +1%、さらに単体向け（狙って落とす・弾を撃つ）を +1%。
        /// WIS は範囲魔法（自分のまわり）を 1 ポイント +2%。物理の職業（影刃）には付かない。
        /// </summary>
        public static float MagicMult(SkillSpec spec, PlayerStats st)
        {
            if (ClassInfo.Get(ClassInfo.Current).MainAttr == Attr.Str) return 1f;
            int intel = st.Attr[(int)Attr.Int], wis = st.Attr[(int)Attr.Wis];
            float m = 1f + intel * 0.01f;
            switch (spec.Effect)
            {
                case SkillEffect.Nova: m *= 1f + wis * 0.02f; break;
                case SkillEffect.Strike:
                case SkillEffect.Barrage: m *= 1f + intel * 0.01f; break;
            }
            return m;
        }

        void Cast(SkillSpec spec, float power, Player player)
        {
            var st = player.Stats;
            Vector2 origin = player.Pos;
            float damage = st.SkillBase * spec.Mult * power * (1f + st.SkillDamage) * MagicMult(spec, st);
            float radius = spec.Radius * st.AreaMult;

            switch (spec.Effect)
            {
                case SkillEffect.Nova:
                    Area(origin, radius, damage, st);
                    // スキルごとの演出。なければ：閃光・大小2重の輪・外へ飛ぶ火花
                    if (!SkillVfx.Cast(fx, spec, player, origin, player.FacingLogical, radius, origin, origin))
                    {
                        fx.Disc(origin, radius, Fade(spec.Color, 0.55f), 0.3f);
                        fx.Ring(origin, radius, spec.Color, 0.35f);
                        fx.Ring(origin, radius * 0.6f, Color.white, 0.2f);
                        fx.Sparks(origin, 14 + (int)(radius * 4f), spec.Color, radius * 2.5f, 0.45f);
                    }
                    Shake?.Invoke(0.08f, 0.15f);
                    sfx.Boom();
                    if (spec.HealPct > 0f) player.Heal(Mathf.RoundToInt(player.MaxHp * spec.HealPct * power));
                    break;

                case SkillEffect.Barrage:
                {
                    Vector2 aim = Aim(player, spec.Range);
                    int n = spec.Count;
                    for (int k = 0; k < n; k++)
                    {
                        float angle = spec.AllAround ? 360f * k / n : (k - (n - 1) * 0.5f) * 12f;
                        Vector2 dir = Rotate(aim, angle);
                        bool crit = UnityEngine.Random.value < st.EffectiveCrit;
                        shots.Fire(origin + dir * 0.3f, dir, damage * (crit ? st.CritMult : 1f), crit, 2, spec.Range);
                    }
                    if (!SkillVfx.Cast(fx, spec, player, origin, aim, radius, origin, origin))
                    {
                        fx.Disc(origin, 1.2f, Fade(spec.Color, 0.6f), 0.2f);
                        fx.Sparks(origin, 10, spec.Color, 3f, 0.3f, 1.5f);
                    }
                    sfx.Fountain();
                    break;
                }

                case SkillEffect.Dash:
                {
                    Vector2 aim = Aim(player, spec.Range);
                    Vector2 start = origin;
                    player.SkillDash(aim, spec.Range, room);
                    Vector2 end = player.Pos;
                    HitAlong(start, end, radius, damage, st);
                    if (!SkillVfx.Cast(fx, spec, player, origin, aim, radius, start, end))
                    {
                        // 通り道に残る火花（残像）と、着いた所の閃光
                        for (int k = 0; k <= 6; k++) fx.Sparks(Vector2.Lerp(start, end, k / 6f), 3, spec.Color, 0.8f, 0.35f, 1f);
                        fx.Slash(end, aim, 1.8f, 160f, spec.Color);
                        fx.Disc(end, 1.4f, Fade(spec.Color, 0.5f), 0.25f);
                    }
                    Shake?.Invoke(0.06f, 0.12f);
                    sfx.Boom();
                    break;
                }

                case SkillEffect.Strike:
                {
                    var target = enemies.FindNearest(origin, spec.Range);
                    Vector2 at = target != null ? target.Pos : origin + Aim(player, spec.Range) * 3f;
                    strikes.Add(new PendingStrike { Pos = at, Timer = spec.Delay, Damage = damage, Radius = radius, Color = spec.Color, Spec = spec });
                    if (!SkillVfx.StrikeWarn(fx, spec, at, radius, spec.Delay))
                        fx.Ring(at, radius, new Color(1f, 0.3f, 0.2f, 0.6f), spec.Delay); // 落ちる場所の予告
                    break;
                }

                case SkillEffect.Buff:
                    player.AddBuff(spec, power);
                    if (!SkillVfx.Cast(fx, spec, player, origin, player.FacingLogical, radius, origin, origin))
                    {
                        // 体のまわりから立ちのぼる火花と、光の柱
                        fx.Pillar(origin, Fade(spec.Color, 0.7f), 0.9f, 2.5f, 0.5f);
                        fx.Ring(origin, 1.2f, spec.Color, 0.4f);
                        fx.Sparks(origin, 16, spec.Color, 1f, 0.8f, 2.2f, true);
                    }
                    sfx.Pickup();
                    break;

                case SkillEffect.Heal:
                    player.Heal(Mathf.RoundToInt(player.MaxHp * spec.HealPct * power));
                    if (!SkillVfx.Cast(fx, spec, player, origin, player.FacingLogical, radius, origin, origin))
                    {
                        // 緑の閃光と、立ちのぼる火花
                        fx.Disc(origin, 1.5f, Fade(spec.Color, 0.5f), 0.4f);
                        fx.Sparks(origin, 18, spec.Color, 1f, 0.9f, 2f, true);
                    }
                    sfx.Pickup();
                    break;
            }
            Casted?.Invoke(spec);
        }

        static Color Fade(Color c, float a) => new Color(c.r, c.g, c.b, a);

        void UpdateStrikes(float dt)
        {
            for (int i = strikes.Count - 1; i >= 0; i--)
            {
                var s = strikes[i];
                s.Timer -= dt;
                if (s.Timer > 0f) continue;
                strikes.RemoveAt(i);
                enemies.DamageArea(s.Pos, s.Radius, s.Damage);
                // 落ちた瞬間：スキルごとの演出。なければ光の柱・閃光・輪・火花
                if (s.Spec == null || !SkillVfx.StrikeImpact(fx, s.Spec, s.Pos, s.Radius))
                {
                    fx.Pillar(s.Pos, Fade(s.Color, 0.9f), s.Radius * 0.8f, 4f, 0.35f);
                    fx.Disc(s.Pos, s.Radius * 1.2f, Fade(s.Color, 0.6f), 0.35f);
                    fx.Ring(s.Pos, s.Radius, s.Color, 0.3f);
                    fx.Sparks(s.Pos, 20, s.Color, s.Radius * 3f, 0.5f, 3f);
                }
                Shake?.Invoke(0.12f, 0.2f);
                sfx.Boom();
            }
        }

        void Area(Vector2 center, float radius, float damage, PlayerStats st)
        {
            var list = enemies.Active;
            float r2 = radius * radius;
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e.Dying || e.Dormant) continue;
                Vector2 d = e.Pos - center;
                if (d.sqrMagnitude > r2) continue;
                bool crit = UnityEngine.Random.value < st.EffectiveCrit;
                enemies.Damage(e, damage * (crit ? st.CritMult : 1f), d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.up, crit);
            }
        }

        void HitAlong(Vector2 a, Vector2 b, float radius, float damage, PlayerStats st)
        {
            var list = enemies.Active;
            Vector2 ab = b - a;
            float len2 = Mathf.Max(ab.sqrMagnitude, 0.0001f);
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e.Dying || e.Dormant) continue;
                float t = Mathf.Clamp01(Vector2.Dot(e.Pos - a, ab) / len2);
                Vector2 closest = a + ab * t;
                float r = radius + e.Radius;
                if ((e.Pos - closest).sqrMagnitude > r * r) continue;
                bool crit = UnityEngine.Random.value < st.EffectiveCrit;
                enemies.Damage(e, damage * (crit ? st.CritMult : 1f), ab.normalized, crit);
            }
        }

        Vector2 Aim(Player player, float range)
        {
            var target = enemies.FindNearest(player.Pos, range);
            return target != null ? (target.Pos - player.Pos).normalized : player.FacingLogical;
        }

        static Vector2 Rotate(Vector2 v, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(r);
            float s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
    }
}
