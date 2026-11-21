using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 彩灯者の分身（8つ目のスキル「彩の分身」で呼ぶ）。
    /// 自分の半分の強さ（SpecialClass.TwinPower。スキルの威力が掛かり、Lv20 でほぼ自分と同じ）で、持っている武器で攻撃し、ボタンに付けている攻撃のスキルも使う（MP は使わない）。
    /// 見た目は自分の絵を桃色に透かしたもの。敵からは狙われない（光の影なので）。時間が来るか、自分が倒れると消える。
    /// </summary>
    public sealed class Phantom : MonoBehaviour
    {
        const float Leash = 4f;          // 自分からこれ以上は離れない（マス）
        const float Engage = 7f;         // 自分からこの距離の敵を狙いに行く
        const float FootRadius = 0.3f;

        SpriteRenderer sr;
        EnemySystem enemies;
        ProjectileSystem shots;
        FxSystem fx;
        Room room;
        SkillSystem skills;

        Vector2 pos;
        float life;
        float atkTimer;
        float t;
        readonly float[] skillCd = new float[SkillInfo.Slots];
        readonly List<Enemy> hits = new List<Enemy>();

        public bool Active => life > 0f;
        /// <summary>分身の強さ（自分に対する割合）。半分 × スキルの威力（スキルレベル）。</summary>
        public float Strength { get; private set; } = SpecialClass.TwinPower;
        public float Remaining => life;
        public Vector2 Pos => pos;
        public Vector2 Facing { get; private set; } = new Vector2(1f, 0f);

        public void Init(EnemySystem enemies, ProjectileSystem shots, FxSystem fx, Room room, SkillSystem skills)
        {
            this.enemies = enemies;
            this.shots = shots;
            this.fx = fx;
            this.room = room;
            this.skills = skills;
            sr = gameObject.AddComponent<SpriteRenderer>();
            gameObject.SetActive(false);
        }

        /// <summary>呼び出す（もういれば、時間を戻して呼び直す）。</summary>
        public void Summon(Player player, float duration, float power)
        {
            life = duration;
            Strength = SpecialClass.TwinPower * power;
            atkTimer = 0.3f;
            for (int i = 0; i < skillCd.Length; i++) skillCd[i] = 1f + i * 0.7f; // 呼んですぐに全部のスキルを撃たないよう、少しずつずらす
            Vector2 side = new Vector2(-0.7f, 0.7f); // 画面の左隣
            pos = room.IsFree(player.Pos + side, FootRadius) ? player.Pos + side : player.Pos;
            gameObject.SetActive(true);
            Place(player);
            fx.Pillar(pos, new Color(1f, 0.6f, 0.95f, 0.8f), 0.8f, 3f, 0.5f);
            fx.Sparks(pos, 18, new Color(1f, 0.7f, 0.95f), 1.2f, 0.7f, 2.2f, true);
        }

        public void Hide()
        {
            life = 0f;
            gameObject.SetActive(false);
        }

        void Vanish()
        {
            if (gameObject.activeSelf) fx.Sparks(pos, 14, new Color(1f, 0.7f, 0.95f), 1f, 0.6f, 2f, true);
            Hide();
        }

        public void Tick(float dt, Player player, CharacterData ch)
        {
            if (!Active) return;
            if (player.IsDead || ch == null || !ch.Special) { Vanish(); return; }
            life -= dt;
            if (life <= 0f) { Vanish(); return; }
            t += dt;
            var st = player.Stats;

            // 動き：近くに敵がいれば、届く所まで寄る。いなければ自分の隣へ戻る。自分から離れすぎない
            var target = enemies.FindNearest(player.Pos, Engage);
            Vector2 want;
            if (target != null)
            {
                Vector2 away = pos - target.Pos;
                float keep = Mathf.Max(0.8f, st.Reach * 0.7f);
                want = target.Pos + (away.sqrMagnitude > 0.01f ? away.normalized : new Vector2(1f, 0f)) * keep;
            }
            else want = player.Pos + new Vector2(-0.7f, 0.7f);
            Vector2 fromPlayer = want - player.Pos;
            if (fromPlayer.magnitude > Leash) want = player.Pos + fromPlayer.normalized * Leash;
            Vector2 step = want - pos;
            if (step.sqrMagnitude > 0.0025f)
            {
                float speed = st.MoveSpeed * 0.75f; // 画面上の速さを、だいたいの床の上の速さに直す
                Vector2 np = pos + Vector2.ClampMagnitude(step, speed * dt);
                if (room.IsFree(np, FootRadius)) pos = np;
                else if (room.IsFree(new Vector2(np.x, pos.y), FootRadius)) pos = new Vector2(np.x, pos.y);
                else if (room.IsFree(new Vector2(pos.x, np.y), FootRadius)) pos = new Vector2(pos.x, np.y);
                Vector2 sd = Iso.ToScreen(step);
                if (Mathf.Abs(sd.x) > 0.01f) sr.flipX = sd.x < 0f;
            }
            if ((pos - player.Pos).sqrMagnitude > 100f) pos = player.Pos; // 次の層へ降りたときなど、遠く離れたら自分の所へ
            Place(player);

            // 武器の攻撃（自分と同じ速さで、威力は半分）
            atkTimer -= dt;
            if (atkTimer <= 0f && target != null && WeaponAttack(st, player, target)) atkTimer = st.FireInterval;

            // スキル：ボタンに付けている攻撃のスキルを、それぞれの待ち時間で使う（強化・回復・分身は使わない）
            for (int i = 0; i < skillCd.Length; i++)
            {
                skillCd[i] -= dt;
                int index = ch.Slots[i];
                if (index < 0 || skillCd[i] > 0f || !SkillInfo.Learned(ClassInfo.Current, index, ch.Level)) continue;
                var spec = SkillInfo.Get(ClassInfo.Current, index);
                if (spec.Effect == SkillEffect.Buff || spec.Effect == SkillEffect.Heal || spec.Effect == SkillEffect.Summon) continue;
                float reach = spec.Effect == SkillEffect.Nova ? spec.Radius : spec.Scatter ? spec.Radius2 : spec.Range;
                if (enemies.FindNearest(pos, reach) == null) continue;
                int lv = ch.SkillLv[index];
                skills.CastTwin(spec, SkillInfo.Power(lv), this, st);
                skillCd[i] = spec.Cooldown * SkillInfo.CooldownMult(lv) * st.CooldownMult;
            }
        }

        /// <summary>スキルの突進：向いた方へ進む（壁の手前で止まる）。</summary>
        public void DashAlong(Vector2 dir, float range)
        {
            Vector2 end = pos;
            for (float d = 0.25f; d <= range; d += 0.25f)
            {
                Vector2 p = pos + dir * d;
                if (!room.IsFree(p, FootRadius)) break;
                end = p;
            }
            pos = end;
            Facing = dir;
        }

        void Place(Player player)
        {
            sr.sprite = player.Sr.sprite;
            sr.sharedMaterial = player.Sr.sharedMaterial;
            transform.localScale = player.transform.localScale;
            transform.position = Iso.ToScreen(pos) + new Vector2(0f, transform.localScale.y * player.HalfHeight - 0.5f); // 本体と同じく、足元を床に付ける（縦長の絵でも）
            sr.sortingOrder = SortOrder.For(pos);
            float a = 0.55f + 0.1f * Mathf.Sin(t * 5f); // ゆらゆらと光る
            sr.color = new Color(1f, 0.62f, 0.95f, a);
        }

        /// <summary>遠近両用：近ければ斬る（周りを焼く武器なら焼く）、遠ければ弾を撃つ。攻撃したら true。</summary>
        bool WeaponAttack(PlayerStats st, Player player, Enemy target)
        {
            float power = Strength;
            Vector2 d = target.Pos - pos;
            float dist = d.magnitude;
            Vector2 dir = dist > 0.001f ? d / dist : Facing;
            Facing = dir;

            if (st.Kind == AttackKind.Pulse && dist <= st.PulseRadius)
            {
                float r2 = st.PulseRadius * st.PulseRadius;
                foreach (var e in enemies.Active)
                {
                    if (e.Dying || e.Dormant) continue;
                    Vector2 ed = e.Pos - pos;
                    if (ed.sqrMagnitude > r2) continue;
                    if (Random.value > st.HitChanceVs(e.Eva)) { enemies.ReportMiss(e.Pos); continue; }
                    bool crit = Random.value < st.EffectiveCrit;
                    enemies.Damage(e, st.Damage * power * (crit ? st.CritMult : 1f), ed.sqrMagnitude > 0.0001f ? ed.normalized : Vector2.up, crit);
                }
                fx.Ring(pos, st.PulseRadius, new Color(1f, 0.6f, 0.95f, 0.7f), 0.3f);
                return true;
            }

            float reach = st.Reach > 0f ? st.Reach : 1.6f;
            if (dist <= reach + target.Radius)
            {
                float cosHalf = Mathf.Cos(Mathf.Max(60f, st.ArcDeg) * 0.5f * Mathf.Deg2Rad);
                hits.Clear();
                foreach (var e in enemies.Active)
                {
                    if (e.Dying || e.Dormant) continue;
                    Vector2 ed = e.Pos - pos;
                    float ex = ed.magnitude;
                    if (ex > reach + e.Radius) continue;
                    if (ex > 0.2f && Vector2.Dot(ed / ex, dir) < cosHalf) continue;
                    hits.Add(e);
                }
                hits.Sort((a, b) => (a.Pos - pos).sqrMagnitude.CompareTo((b.Pos - pos).sqrMagnitude));
                int count = Mathf.Min(hits.Count, Mathf.Max(1, st.MaxTargets));
                for (int i = 0; i < count; i++)
                {
                    var e = hits[i];
                    if (Random.value > st.HitChanceVs(e.Eva)) { enemies.ReportMiss(e.Pos); continue; }
                    bool crit = Random.value < st.EffectiveCrit;
                    float scale = i == 0 ? 1f : PlayerStats.SecondaryHit;
                    Vector2 ed = e.Pos - pos;
                    enemies.Damage(e, st.Damage * power * scale * (crit ? st.CritMult : 1f), ed.sqrMagnitude > 0.0001f ? ed.normalized : dir, crit);
                }
                fx.Slash(pos, dir, reach, Mathf.Max(60f, st.ArcDeg), new Color(1f, 0.7f, 0.95f, 0.85f));
                return true;
            }

            if (dist <= player.EffectiveLight)
            {
                int n = Mathf.Max(1, st.Shots);
                float spread = st.SpreadDeg > 0f ? st.SpreadDeg : 12f;
                for (int k = 0; k < n; k++)
                {
                    Vector2 sdir = Rotate(dir, (k - (n - 1) * 0.5f) * spread);
                    bool crit = Random.value < st.EffectiveCrit;
                    shots.Fire(pos + sdir * 0.3f, sdir, st.Damage * power * (crit ? st.CritMult : 1f), crit, st.Pierce, player.EffectiveLight);
                }
                return true;
            }
            return false;
        }

        static Vector2 Rotate(Vector2 v, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
    }
}
