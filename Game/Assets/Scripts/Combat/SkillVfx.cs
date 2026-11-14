using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// スキルごとの演出（名前の文字は出さず、見た目で何のスキルか分かるように）。
    /// 当たり判定・ダメージ・音・画面の揺れは SkillSystem のまま。ここは見た目だけ。
    /// 演出がないスキルは false を返し、SkillSystem の共通の演出になる。
    /// </summary>
    public static class SkillVfx
    {
        static readonly Color White = Color.white;

        /// <summary>使った瞬間の演出。</summary>
        public static bool Cast(FxSystem fx, SkillSpec spec, Player player, Vector2 origin, Vector2 aim, float radius, Vector2 dashStart, Vector2 dashEnd)
        {
            Color c = spec.Color;
            switch (spec.Name)
            {
                // ---------------- 灯術師 ----------------
                case "Flame Ring": // 炎が輪になって外へ広がる
                    FlameRing(fx, origin, radius, 16, White);
                    fx.Ring(origin, radius, Fade(c, 0.7f), 0.35f);
                    fx.Disc(origin, radius * 0.8f, Fade(c, 0.35f), 0.25f);
                    return true;

                case "Fire Barrage": // 体のまわりに火の玉がふくらんで、はじける
                    FlameRing(fx, origin, 1.1f, 12, White, 0.3f);
                    fx.Disc(origin, 1.2f, Fade(c, 0.5f), 0.2f);
                    return true;

                case "Flare": // 頭の上に小さな太陽が昇り、光が広がる
                    fx.Emit(origin, 0.6f, PixelArt.Star, new Color(1f, 0.95f, 0.7f, 1f), 0.9f, Vector2.zero, 2.2f, 0f, 0.8f, 3f, 220f);
                    fx.Disc(origin, 3f, new Color(1f, 0.95f, 0.7f, 0.35f), 0.6f);
                    fx.After(0.25f, () => fx.Ring(origin, 2.4f, new Color(1f, 0.95f, 0.7f, 0.7f), 0.4f));
                    fx.Sparks(origin, 14, new Color(1f, 0.95f, 0.7f), 1f, 0.9f, 2.4f, true);
                    return true;

                case "Ember Shield": // 残り火がぐるりと体を回って守る
                    Orbit(fx, player, 8, 0.9f, 0.9f, PixelArt.Flame, White, 0.8f);
                    fx.Disc(origin, 1.1f, new Color(1f, 0.5f, 0.2f, 0.45f), 0.5f);
                    fx.Ring(origin, 1.1f, Fade(c, 0.8f), 0.4f);
                    return true;

                case "Inferno": // 炎の輪が3重に広がり、輪の上に火柱が立つ
                    for (int k = 0; k < 3; k++)
                    {
                        float r = radius * (0.45f + 0.275f * k);
                        fx.After(k * 0.12f, () => FlameRing(fx, origin, r, 14 + k * 4, White));
                    }
                    for (int k = 0; k < 8; k++)
                    {
                        Vector2 p = origin + Dir(k * 45f + 22f) * radius * 0.75f;
                        fx.After(0.18f + k * 0.03f, () => { fx.Pillar(p, new Color(1f, 0.55f, 0.2f, 0.9f), 0.5f, 3f, 0.4f); FlameColumn(fx, p, 2f, 4); });
                    }
                    fx.Disc(origin, radius, new Color(1f, 0.45f, 0.15f, 0.4f), 0.5f);
                    return true;

                // ---------------- 影刃 ----------------
                case "Whirlwind": // 刃がぐるりと3回まわる
                    for (int k = 0; k < 3; k++)
                    {
                        float ang = k * 120f;
                        fx.After(k * 0.05f, () => fx.Slash(origin, Dir(ang), radius, 150f, Fade(c, 0.9f)));
                    }
                    fx.Ring(origin, radius, Fade(c, 0.5f), 0.3f);
                    Wind(fx, origin, radius, 10, new Color(0.85f, 0.9f, 1f, 0.8f));
                    return true;

                case "Shadow Dash": // 通り道に紫の残像
                    Ghosts(fx, player, dashStart, dashEnd, 6, new Color(0.55f, 0.4f, 0.95f, 0.7f));
                    fx.Slash(dashEnd, aim, 1.8f, 160f, c);
                    fx.Disc(dashEnd, 1.2f, Fade(c, 0.4f), 0.25f);
                    return true;

                case "Frenzy": // 赤い気が炎のように立ちのぼる
                    for (int k = 0; k < 14; k++)
                    {
                        Vector2 p = origin + Random.insideUnitCircle * 0.6f;
                        fx.After(k * 0.03f, () => fx.Emit(p, 0.1f, PixelArt.Flame, new Color(1f, 0.35f, 0.35f, 0.95f), 0.55f, Vector2.zero, 2.2f, 0f, 1.2f, 0.4f));
                    }
                    fx.Ring(origin, 1.3f, Fade(c, 0.8f), 0.3f);
                    fx.After(0.15f, () => fx.Ring(origin, 1.7f, Fade(c, 0.5f), 0.35f));
                    return true;

                case "Crescent Wave": // 三日月の刃が扇に飛ぶ
                    for (int k = 0; k < spec.Count; k++)
                    {
                        Vector2 d = Rotate(aim, (k - (spec.Count - 1) * 0.5f) * 12f);
                        CrescentShot(fx, origin, d, new Color(0.85f, 0.9f, 1f, 1f), 1.4f);
                    }
                    return true;

                case "Second Wind": // 緑の葉が渦を巻いて舞い上がる
                    LeafSwirl(fx, origin, 16, new Color(0.5f, 1f, 0.6f, 1f));
                    fx.Disc(origin, 1.4f, new Color(0.5f, 1f, 0.6f, 0.4f), 0.45f);
                    return true;

                case "Thousand Cuts": // まわりのあちこちに斬撃が走る
                    for (int k = 0; k < 18; k++)
                    {
                        Vector2 p = origin + Random.insideUnitCircle * radius * 0.8f;
                        float ang = Random.value * 360f;
                        fx.After(k * 0.03f, () => fx.Slash(p, Dir(ang), 1.1f, 60f, new Color(0.9f, 0.95f, 1f, 0.95f)));
                    }
                    fx.Ring(origin, radius, Fade(c, 0.5f), 0.5f);
                    return true;

                // ---------------- 灯番 ----------------
                case "Holy Pulse": // 光の十字が昇り、光の波が広がる
                    fx.Emit(origin, 0.8f, PixelArt.HolyCross, new Color(1f, 0.92f, 0.6f, 1f), 0.6f, Vector2.zero, 1.2f, 0f, 1f, 2.2f);
                    fx.Ring(origin, radius, Fade(c, 0.85f), 0.35f);
                    fx.Disc(origin, radius, Fade(c, 0.3f), 0.3f);
                    Rays(fx, origin, radius * 0.7f, 6, new Color(1f, 0.92f, 0.6f, 0.7f));
                    return true;

                case "Mend": // 緑の十字がふわふわ昇る
                    for (int k = 0; k < 6; k++)
                    {
                        Vector2 p = origin + Random.insideUnitCircle * 0.8f;
                        fx.After(k * 0.06f, () => fx.Emit(p, 0.2f, PixelArt.HolyCross, new Color(0.5f, 1f, 0.6f, 1f), 0.8f, Vector2.zero, 1.4f, 0f, 0.7f, 0.9f));
                    }
                    fx.Disc(origin, 1.3f, new Color(0.5f, 1f, 0.6f, 0.4f), 0.45f);
                    return true;

                case "Lantern Ward": // 金色の光の盾が体を包む
                    Orbit(fx, player, 6, 1f, 0.9f, PixelArt.Star, new Color(1f, 0.85f, 0.4f, 1f), 0.9f);
                    fx.Disc(origin, 1.2f, new Color(1f, 0.85f, 0.4f, 0.5f), 0.6f);
                    fx.Ring(origin, 1.2f, Fade(c, 0.9f), 0.3f);
                    fx.After(0.15f, () => fx.Ring(origin, 1.2f, Fade(c, 0.6f), 0.4f));
                    return true;

                case "Kindle": // まわりに小さな灯りが3つ灯る
                    for (int k = 0; k < 3; k++)
                    {
                        Vector2 p = origin + Dir(k * 120f + 90f) * 0.9f;
                        fx.After(k * 0.08f, () =>
                        {
                            fx.Emit(p, 0.5f, PixelArt.Flame, White, 1f, Vector2.zero, 0.6f, 0f, 1.4f, 0.6f);
                            fx.Disc(p, 0.5f, new Color(1f, 0.8f, 0.4f, 0.5f), 0.4f);
                        });
                    }
                    fx.Sparks(origin, 10, new Color(1f, 0.85f, 0.5f), 1f, 0.8f, 2f, true);
                    return true;

                case "Judgment": // 空から光の柱が何本も降り、大きな十字
                    for (int k = 0; k < 9; k++)
                    {
                        Vector2 p = origin + Random.insideUnitCircle * radius * 0.85f;
                        fx.After(k * 0.05f, () =>
                        {
                            fx.Pillar(p, new Color(1f, 0.95f, 0.75f, 0.95f), 0.5f, 7f, 0.45f);
                            fx.Disc(p, 0.9f, new Color(1f, 0.95f, 0.7f, 0.5f), 0.3f);
                        });
                    }
                    fx.Emit(origin, 1f, PixelArt.HolyCross, new Color(1f, 0.95f, 0.75f, 1f), 0.9f, Vector2.zero, 1f, 0f, 1.5f, 3.2f);
                    fx.Ring(origin, radius, Fade(c, 0.8f), 0.5f);
                    return true;

                // ---------------- 上位職 ----------------
                case "Beacon": // 灯台のように光の柱が立ち、光がぐるりと回る
                    fx.Pillar(origin, new Color(1f, 0.95f, 0.7f, 0.9f), 1f, 9f, 0.8f);
                    Orbit(fx, player, 4, 1.4f, 1.6f, PixelArt.Star, new Color(1f, 0.95f, 0.7f, 1f), 1f);
                    fx.Disc(origin, 2.4f, new Color(1f, 0.95f, 0.7f, 0.35f), 0.6f);
                    return true;

                case "Eclipse Rush": // 黒い残像と、着いた所に蝕の輪
                    Ghosts(fx, player, dashStart, dashEnd, 8, new Color(0.3f, 0.2f, 0.55f, 0.75f));
                    fx.Disc(dashEnd, 1.6f, new Color(0.1f, 0.05f, 0.2f, 0.7f), 0.4f);
                    fx.Ring(dashEnd, 1.6f, Fade(c, 0.9f), 0.35f);
                    CrescentShot(fx, dashEnd, aim, Fade(c, 1f), 1.6f);
                    return true;

                case "Crimson Spiral": // 紅い炎が渦を巻いて外へ
                    for (int k = 0; k < 24; k++)
                    {
                        float ang = k * 30f;
                        float r = radius * (0.2f + 0.8f * k / 24f);
                        fx.After(k * 0.015f, () => fx.Emit(origin + Dir(ang) * r, 0.1f, PixelArt.Flame, new Color(1f, 0.35f, 0.4f, 1f), 0.45f, Dir(ang + 90f) * 2f, 1.2f, 0f, 1.3f, 0.4f));
                    }
                    fx.Slash(origin, Dir(0f), radius, 180f, Fade(c, 0.8f));
                    fx.After(0.08f, () => fx.Slash(origin, Dir(180f), radius, 180f, Fade(c, 0.8f)));
                    return true;

                case "Sunburst": // 日の輪：光の筋が放射状に広がる
                    fx.Disc(origin, radius, new Color(1f, 0.85f, 0.35f, 0.45f), 0.4f);
                    Rays(fx, origin, radius, 12, new Color(1f, 0.9f, 0.5f, 0.85f));
                    fx.Ring(origin, radius, Fade(c, 0.9f), 0.4f);
                    fx.Emit(origin, 0.8f, PixelArt.Star, new Color(1f, 0.9f, 0.5f, 1f), 0.6f, Vector2.zero, 0f, 0f, 2f, 4f, 180f);
                    return true;

                case "Iron Vigil": // 鉄色の盾が何重にも重なる
                    for (int k = 0; k < 3; k++) fx.After(k * 0.1f, () => fx.Ring(origin, 1.3f, new Color(0.75f, 0.8f, 0.9f, 0.85f), 0.35f));
                    fx.Disc(origin, 1.3f, new Color(0.75f, 0.8f, 0.9f, 0.45f), 0.6f);
                    fx.Sparks(origin, 14, new Color(0.85f, 0.9f, 1f), 2.5f, 0.4f, 1.5f);
                    Orbit(fx, player, 4, 1f, 0.8f, PixelArt.HolyCross, new Color(0.8f, 0.85f, 0.95f, 1f), 0.8f);
                    return true;

                // ---------------- 3次職 ----------------
                case "Cataclysm": // 空から隕石がいくつも降りそそぐ
                    for (int k = 0; k < 7; k++)
                    {
                        Vector2 p = origin + Random.insideUnitCircle * radius * 0.8f;
                        fx.After(k * 0.06f, () => MeteorFall(fx, p, 0.35f, () =>
                        {
                            FlameRing(fx, p, 1.4f, 10, White);
                            fx.Disc(p, 1.3f, new Color(1f, 0.5f, 0.2f, 0.5f), 0.3f);
                        }));
                    }
                    return true;

                case "Constellation": // まわりに星が灯り、ひとつずつ瞬く
                    for (int k = 0; k < 12; k++)
                    {
                        Vector2 p = origin + Dir(k * 30f) * (1.2f + (k % 2) * 0.6f);
                        fx.After(k * 0.03f, () => fx.Emit(p, 0.6f, PixelArt.Star, new Color(0.85f, 0.92f, 1f, 1f), 0.6f, Vector2.zero, 0f, 0f, 0.5f, 1.6f, 200f));
                    }
                    fx.Ring(origin, 1.8f, Fade(c, 0.7f), 0.4f);
                    return true;

                case "Umbral Storm": // 影の三日月が渦を巻いて飛ぶ
                    for (int k = 0; k < spec.Count; k++)
                    {
                        Vector2 d = Rotate(aim, (k - (spec.Count - 1) * 0.5f) * 12f);
                        fx.After(k * 0.02f, () => CrescentShot(fx, origin, d, new Color(0.6f, 0.4f, 1f, 1f), 1.5f));
                    }
                    fx.Disc(origin, 1.4f, new Color(0.15f, 0.05f, 0.3f, 0.6f), 0.35f);
                    return true;

                case "Sanguine Tide": // 血の波が3重に押し寄せ、しぶきが散る
                    for (int k = 0; k < 3; k++)
                    {
                        float r = radius * (0.5f + 0.25f * k);
                        fx.After(k * 0.1f, () => fx.Ring(origin, r, new Color(1f, 0.2f, 0.3f, 0.85f), 0.4f));
                    }
                    fx.Disc(origin, radius, new Color(0.7f, 0.05f, 0.15f, 0.45f), 0.45f);
                    fx.Sparks(origin, 30, new Color(1f, 0.25f, 0.3f), radius * 3f, 0.6f, 3.5f);
                    return true;

                case "Daybreak": // 夜明け：大きな光の柱と、放射する光、何重もの輪
                    fx.Pillar(origin, new Color(1f, 0.9f, 0.55f, 0.95f), 2f, 10f, 0.8f);
                    Rays(fx, origin, radius, 16, new Color(1f, 0.9f, 0.55f, 0.85f));
                    for (int k = 0; k < 3; k++) fx.After(k * 0.12f, () => fx.Ring(origin, radius * (0.5f + 0.25f * k), Fade(c, 0.8f), 0.45f));
                    fx.Disc(origin, radius, new Color(1f, 0.9f, 0.55f, 0.4f), 0.6f);
                    return true;

                case "Unbreakable": // まぶしい白い盾と、まわりに立つ光の柱
                    fx.Disc(origin, 1.5f, new Color(0.85f, 0.92f, 1f, 0.6f), 0.7f);
                    for (int k = 0; k < 4; k++)
                    {
                        Vector2 p = origin + Dir(k * 90f + 45f) * 1.3f;
                        fx.After(k * 0.05f, () => fx.Pillar(p, new Color(0.85f, 0.92f, 1f, 0.9f), 0.4f, 4f, 0.6f));
                    }
                    fx.Emit(origin, 1.2f, PixelArt.HolyCross, new Color(0.9f, 0.95f, 1f, 1f), 0.9f, Vector2.zero, 0.8f, 0f, 1.2f, 2.4f);
                    fx.Ring(origin, 1.5f, Fade(c, 0.9f), 0.4f);
                    return true;
            }
            return false;
        }

        /// <summary>狙って落とすスキル：落ちるまでの演出（予告）。</summary>
        public static bool StrikeWarn(FxSystem fx, SkillSpec spec, Vector2 at, float radius, float delay)
        {
            switch (spec.Name)
            {
                case "Meteor": // 空から隕石が落ちてくる
                    fx.Ring(at, radius, new Color(1f, 0.35f, 0.2f, 0.5f), delay);
                    MeteorFall(fx, at, delay, null);
                    return true;
                case "Flame Pillar": // 足元がじわりと赤く光る
                    fx.Disc(at, radius, new Color(1f, 0.4f, 0.15f, 0.4f), delay);
                    fx.Ring(at, radius, new Color(1f, 0.4f, 0.2f, 0.6f), delay);
                    for (int k = 0; k < 6; k++)
                    {
                        Vector2 p = at + Random.insideUnitCircle * radius * 0.7f;
                        fx.After(k * delay / 6f, () => fx.Emit(p, 0f, PixelArt.Flame, White, 0.35f, Vector2.zero, 0.8f, 0f, 0.6f, 0.2f));
                    }
                    return true;
                case "Starfall": // 星がいくつも降ってくる
                    fx.Ring(at, radius, new Color(1f, 0.95f, 0.7f, 0.5f), delay);
                    for (int k = 0; k < 5; k++)
                    {
                        Vector2 p = at + Random.insideUnitCircle * radius * 0.6f;
                        float d = delay * (0.7f + 0.3f * k / 4f);
                        fx.Emit(p + new Vector2(-1.2f, 1.2f), 6f, PixelArt.Star, new Color(1f, 0.95f, 0.75f, 1f), d,
                            new Vector2(1.2f, -1.2f) / d, -6f / d, 0f, 1.4f, 1f, 360f, 0f);
                    }
                    return true;
            }
            return false;
        }

        /// <summary>狙って落とすスキル：落ちた瞬間の演出。</summary>
        public static bool StrikeImpact(FxSystem fx, SkillSpec spec, Vector2 at, float radius)
        {
            switch (spec.Name)
            {
                case "Meteor": // 爆発：炎が外へ、破片が飛び、焦げ跡
                    FlameRing(fx, at, radius * 1.3f, 16, White);
                    fx.Disc(at, radius * 1.3f, new Color(1f, 0.55f, 0.2f, 0.65f), 0.35f);
                    fx.Ring(at, radius * 1.2f, new Color(1f, 0.6f, 0.3f, 0.9f), 0.3f);
                    fx.Sparks(at, 18, new Color(0.6f, 0.4f, 0.3f), radius * 3f, 0.6f, 4f); // 破片
                    fx.Disc(at, radius, new Color(0.1f, 0.05f, 0.03f, 0.5f), 0.8f);       // 焦げ跡
                    return true;
                case "Flame Pillar": // 火柱が噴き上がる
                    fx.Pillar(at, new Color(1f, 0.55f, 0.2f, 0.95f), radius * 0.7f, 5f, 0.5f);
                    FlameColumn(fx, at, 4f, 14);
                    fx.Ring(at, radius, new Color(1f, 0.5f, 0.2f, 0.9f), 0.3f);
                    return true;
                case "Starfall": // 星がはじけて、きらめきが散る
                    for (int k = 0; k < 10; k++)
                    {
                        Vector2 d = Dir(k * 36f);
                        fx.Emit(at, 0.3f, PixelArt.Star, new Color(1f, 0.95f, 0.75f, 1f), 0.5f, d * radius * 5f, 1.5f, 3f, 1f, 0.3f, 300f, 6f);
                    }
                    fx.Disc(at, radius * 1.2f, new Color(1f, 0.95f, 0.75f, 0.55f), 0.3f);
                    fx.Ring(at, radius, new Color(1f, 0.95f, 0.75f, 0.9f), 0.3f);
                    return true;
            }
            return false;
        }

        // ================= 部品 =================

        static Color Fade(Color c, float a) => new Color(c.r, c.g, c.b, a);

        static Vector2 Dir(float deg) => new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad));

        static Vector2 Rotate(Vector2 v, float deg)
        {
            float r = deg * Mathf.Deg2Rad, cs = Mathf.Cos(r), sn = Mathf.Sin(r);
            return new Vector2(v.x * cs - v.y * sn, v.x * sn + v.y * cs);
        }

        /// <summary>中心から炎が輪になって広がる（radius まで）。</summary>
        static void FlameRing(FxSystem fx, Vector2 center, float radius, int n, Color tint, float life = 0.45f)
        {
            const float drag = 6f;
            for (int k = 0; k < n; k++)
            {
                Vector2 d = Dir(360f * k / n + Random.Range(-8f, 8f));
                fx.Emit(center + d * 0.25f, 0.05f, PixelArt.Flame, tint, life, d * radius * drag, 0.8f, 0f, 1.4f, 0.5f, 0f, drag);
            }
        }

        /// <summary>その場から炎が縦に噴き上がる。</summary>
        static void FlameColumn(FxSystem fx, Vector2 at, float height, int n)
        {
            for (int k = 0; k < n; k++)
            {
                float h = height * k / n;
                Vector2 p = at + Random.insideUnitCircle * 0.25f;
                fx.Emit(p, h * 0.4f, PixelArt.Flame, White, 0.5f, Vector2.zero, 2.5f + height * 0.5f, 0f, 1.6f, 0.4f);
            }
        }

        /// <summary>体のまわりを絵の粒が回る（player についていく）。</summary>
        static void Orbit(FxSystem fx, Player player, int n, float radius, float duration, Sprite sprite, Color tint, float h)
        {
            const float step = 0.04f;
            int steps = Mathf.RoundToInt(duration / step);
            for (int s = 0; s < steps; s++)
            {
                float t = s * step;
                fx.After(t, () =>
                {
                    for (int k = 0; k < n; k++)
                    {
                        Vector2 p = player.Pos + Dir(360f * k / n + t * 360f) * radius;
                        fx.Emit(p, h, sprite, tint, 0.12f, Vector2.zero, 0f, 0f, 1f, 0.7f);
                    }
                });
            }
        }

        /// <summary>中心から放射状に光の筋（小さな星が外へ走る）。</summary>
        static void Rays(FxSystem fx, Vector2 center, float radius, int n, Color tint)
        {
            const float drag = 5f;
            for (int k = 0; k < n; k++)
            {
                Vector2 d = Dir(360f * k / n);
                for (int j = 0; j < 3; j++)
                    fx.Emit(center + d * (0.3f + j * 0.25f), 0.3f, PixelArt.Star, tint, 0.4f, d * radius * drag * (1f - j * 0.15f), 0f, 0f, 1f, 0.3f, 0f, drag);
            }
        }

        /// <summary>渦を巻く風（白い筋の粒）。</summary>
        static void Wind(FxSystem fx, Vector2 center, float radius, int n, Color tint)
        {
            for (int k = 0; k < n; k++)
            {
                float ang = 360f * k / n;
                fx.Emit(center + Dir(ang) * radius * 0.6f, 0.4f, PixelArt.Leaf, tint, 0.35f, Dir(ang + 90f) * 6f, 0.5f, 0f, 1f, 0.5f, 720f, 4f);
            }
        }

        /// <summary>葉が渦を巻いて舞い上がる。</summary>
        static void LeafSwirl(FxSystem fx, Vector2 center, int n, Color tint)
        {
            for (int k = 0; k < n; k++)
            {
                float ang = 360f * k / n;
                fx.After(k * 0.02f, () => fx.Emit(center + Dir(ang) * 0.9f, 0.1f, PixelArt.Leaf, tint, 0.9f, Dir(ang + 100f) * 2.5f, 1.8f, 0f, 1.2f, 0.6f, 400f, 2f));
            }
        }

        /// <summary>三日月の刃を、向きに合わせて飛ばす（見た目だけ。当たりは弾のほう）。</summary>
        static void CrescentShot(FxSystem fx, Vector2 from, Vector2 dir, Color tint, float scale)
        {
            Vector2 sd = Iso.ToScreen(dir);
            float ang = Mathf.Atan2(sd.y, sd.x) * Mathf.Rad2Deg;
            fx.Emit(from + dir * 0.4f, 0.35f, PixelArt.Crescent, tint, 0.35f, dir * GameConfig.ShotSpeed, 0f, 0f, scale, scale * 0.8f, 0f, 0f, false, ang);
        }

        /// <summary>通り道に、プレイヤーの絵の残像を並べる。</summary>
        static void Ghosts(FxSystem fx, Player player, Vector2 from, Vector2 to, int n, Color tint)
        {
            var sprite = player.Sr.sprite;
            bool flip = player.Sr.flipX;
            float lift = (GameConfig.PlayerScale - 1f) * 0.5f; // 大きく描いた分、絵の中心は少し上
            for (int k = 0; k < n; k++)
            {
                Vector2 p = Vector2.Lerp(from, to, (k + 0.5f) / n);
                fx.Emit(p, lift, sprite, tint, 0.25f + 0.04f * k, Vector2.zero, 0f, 0f, GameConfig.PlayerScale, GameConfig.PlayerScale, 0f, 3f, flip);
            }
        }

        /// <summary>隕石が左上の空から at へ落ちる（time 秒で着く）。着いたら onLand。</summary>
        static void MeteorFall(FxSystem fx, Vector2 at, float time, System.Action onLand)
        {
            Vector2 start = at + new Vector2(-1.5f, 1.5f);
            const float height = 6f;
            fx.Emit(start, height, PixelArt.Meteor, White, time, (at - start) / time, -height / time, 0f, 1.6f, 1.6f, 0f, 0f);
            // 炎の尾
            int trail = Mathf.Max(3, Mathf.RoundToInt(time / 0.04f));
            for (int k = 0; k < trail; k++)
            {
                float t = time * k / trail;
                Vector2 p = Vector2.Lerp(start, at, t / time);
                float h = height * (1f - t / time);
                fx.After(t, () => fx.Emit(p, h, PixelArt.Flame, White, 0.3f, Vector2.zero, 0f, 0f, 1.2f, 0.3f));
            }
            if (onLand != null) fx.After(time, onLand);
        }
    }
}
