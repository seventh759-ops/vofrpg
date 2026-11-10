using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 能力値の画面（開いている間はゲームが止まる）。レベル・経験値、5つの能力値と「+」ボタン、
    /// 計算後の能力（攻撃力・体力・MP など）を表示する。
    /// </summary>
    public sealed class StatsScreen
    {
        static readonly Color panel = new Color(0f, 0f, 0f, 0.88f);
        static readonly Color buttonBack = new Color(1f, 1f, 1f, 0.15f);
        static readonly Color plusBack = new Color(1f, 0.85f, 0.3f, 0.45f);

        readonly Hud hud;

        // 2回押しで実行する確認（押してから数秒だけ有効）
        string armed;
        float armedUntil;

        public StatsScreen(Hud hud)
        {
            this.hud = hud;
        }

        public void Draw(GameWorld world)
        {
            var ch = Profile.Current;
            var st = world.Player.Stats;
            if (ch == null || st == null) return;

            float W = Screen.width;
            float H = Screen.height;
            float pad = W * 0.05f;
            float btnH = H * 0.045f;
            var label = hud.LabelStyle;
            var small = hud.SmallStyle;
            var centered = hud.CenteredStyle;
            float lineH = label.fontSize * 1.4f;

            Hud.Fill(new Rect(0, 0, W, H), panel);

            var spec = ClassInfo.Get(st.Class);
            var title = new GUIStyle(label) { fontStyle = FontStyle.Bold };
            GUI.Label(new Rect(pad, pad, W, btnH), $"{spec.Name}   Lv {ch.Level}", title);
            hud.Button(new Rect(W - pad - W * 0.22f, pad, W * 0.22f, btnH), "CLOSE", buttonBack, centered, () => world.StatsOpen = false);

            // 経験値
            float y = pad + btnH + H * 0.015f;
            int need = Progression.ExpToNext(ch.Level);
            var bar = new Rect(pad, y, W - pad * 2f, H * 0.012f);
            Hud.Fill(bar, new Color(1f, 1f, 1f, 0.12f));
            Hud.Fill(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01((float)ch.Exp / need), bar.height), new Color(1f, 0.85f, 0.3f, 0.9f));
            y += bar.height + H * 0.008f;
            GUI.Label(new Rect(pad, y, W, lineH), $"EXP {ch.Exp} / {need}", small);
            y += lineH * 1.2f;

            // 能力値
            var pointsStyle = new GUIStyle(label) { fontStyle = FontStyle.Bold };
            pointsStyle.normal.textColor = ch.Points > 0 ? new Color(1f, 0.85f, 0.3f) : Color.white;
            GUI.Label(new Rect(pad, y, W, lineH), $"Points: {ch.Points}", pointsStyle);
            y += lineH * 1.3f;

            float rowH = H * 0.06f;
            float plusW = rowH * 1.2f;
            for (int i = 0; i < Attributes.Count; i++)
            {
                var attr = (Attr)i;
                int baseValue = spec.BaseStats[i] + ch.Alloc[i];
                int total = st.Attr[i];
                int fromGear = total - baseValue;
                var row = new Rect(pad, y, W - pad * 2f, rowH);
                Hud.Fill(row, new Color(1f, 1f, 1f, i % 2 == 0 ? 0.06f : 0.03f));

                var nameStyle = new GUIStyle(label) { fontStyle = FontStyle.Bold };
                if (attr == spec.MainAttr) nameStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);
                else if (attr == spec.SubAttr) nameStyle.normal.textColor = new Color(0.95f, 0.75f, 0.5f);
                GUI.Label(new Rect(row.x + pad * 0.3f, row.y, W * 0.18f, rowH * 0.6f), Attributes.ShortName(attr), nameStyle);
                string gear = fromGear > 0 ? $"  (+{fromGear})" : "";
                GUI.Label(new Rect(row.x + W * 0.2f, row.y, W * 0.4f, rowH * 0.6f), $"{total}{gear}", label);
                GUI.Label(new Rect(row.x + pad * 0.3f, row.y + rowH * 0.55f, row.width - plusW, rowH * 0.45f), Attributes.Effect(attr), small);

                if (ch.Points > 0)
                {
                    var plus = new Rect(row.xMax - plusW, row.y + rowH * 0.1f, plusW, rowH * 0.8f);
                    hud.Button(plus, "+", plusBack, centered, () => world.Allocate(attr));
                }
                y += rowH + H * 0.006f;
            }
            y += H * 0.01f;

            // 計算後の能力
            string kind;
            switch (st.Kind)
            {
                case AttackKind.Slash: kind = $"Slash  reach {st.Reach:0.0}  arc {st.ArcDeg:0}  targets {st.MaxTargets}"; break;
                case AttackKind.Orbit: kind = $"Flames x{st.Orbs}"; break;
                case AttackKind.Pulse: kind = $"Pulse  radius {st.PulseRadius:0.0}"; break;
                default: kind = $"Shots x{st.Shots}  pierce {st.Pierce}"; break;
            }
            string[] lines =
            {
                $"HP {world.Player.MaxHp}   (+{st.HpRegen:0.0}/s)",
                $"MP {world.Player.MaxMp}   (+{st.MpRegen:0.0}/s)   next Lv +{Progression.MpPerLevel(Progression.BaseWis(ch))} MP",
                $"Damage {st.Damage:0.0}   Attacks/s {st.AttacksPerSecond:0.0}",
                $"Crit {st.CritChance * 100f:0}%  x{st.CritMult:0.00}",
                $"Armor {st.Armor:0}   Light {world.Player.EffectiveLight:0.0}   Speed {st.MoveSpeed:0.0}",
                kind,
            };
            float bottomY = H - pad - btnH;
            foreach (var line in lines)
            {
                if (y + lineH > bottomY - H * 0.01f) break;
                GUI.Label(new Rect(pad, y, W - pad * 2f, lineH), line, small);
                y += small.fontSize * 1.4f;
            }

            // 振り直しとレベルのリセット（2回押しで実行）
            float gap = W * 0.03f;
            float half = (W - pad * 2f - gap) * 0.5f;
            ConfirmButton(new Rect(pad, bottomY, half, btnH), "points", "RESET POINTS", world.ResetPoints);
            ConfirmButton(new Rect(pad + half + gap, bottomY, half, btnH), "level", "RESET ALL", world.ResetLevel);
        }

        void ConfirmButton(Rect r, string id, string text, System.Action action)
        {
            bool isArmed = armed == id && Time.unscaledTime < armedUntil;
            var back = isArmed ? new Color(0.7f, 0.15f, 0.15f, 0.8f) : new Color(1f, 1f, 1f, 0.12f);
            hud.Button(r, isArmed ? "TAP AGAIN" : text, back, hud.CenteredStyle, () =>
            {
                if (armed == id && Time.unscaledTime < armedUntil)
                {
                    armed = null;
                    action();
                }
                else
                {
                    armed = id;
                    armedUntil = Time.unscaledTime + 3f;
                }
            });
        }
    }
}
