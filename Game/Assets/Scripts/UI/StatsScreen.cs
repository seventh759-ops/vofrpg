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
            var adv = AdvancedClasses.Of(st.Class, ch);
            GUI.Label(new Rect(pad, pad, W, btnH), (adv != null ? adv.DisplayName : spec.DisplayName) + $"   Lv {ch.Level}", title);
            hud.Button(new Rect(W - pad - W * 0.22f, pad, W * 0.22f, btnH), Loc.T("CLOSE", "閉じる"), buttonBack, centered, () => world.StatsOpen = false);

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
            GUI.Label(new Rect(pad, y, W, lineH), Loc.T($"Points: {ch.Points}", $"ポイント：{ch.Points}"), pointsStyle);
            y += lineH * 1.3f;

            float rowH = H * 0.06f;
            float plusW = rowH * 1.2f;
            for (int i = 0; i < Attributes.Count; i++)
            {
                var attr = (Attr)i;
                int baseValue = spec.BaseStats[i] + ch.Alloc[i] + (ch.Third ? ThirdClasses.AllStats : 0);
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
                case AttackKind.Slash: kind = Loc.T($"Slash  reach {st.Reach:0.0}  arc {st.ArcDeg:0}  targets {st.MaxTargets}", $"斬撃  届く距離 {st.Reach:0.0}  広さ {st.ArcDeg:0}度  当たる数 {st.MaxTargets}"); break;
                case AttackKind.Orbit: kind = Loc.T($"Flames x{st.Orbs}", $"回る灯り x{st.Orbs}"); break;
                case AttackKind.Pulse: kind = Loc.T($"Pulse  radius {st.PulseRadius:0.0}", $"周りを焼く  範囲 {st.PulseRadius:0.0}"); break;
                default: kind = Loc.T($"Shots x{st.Shots}  pierce {st.Pierce}", $"弾 x{st.Shots}  貫通 {st.Pierce}"); break;
            }
            // 計算後の能力：2列の表（左に名前、右に値）
            float bottomY = H - pad - btnH;
            string[,] cells =
            {
                { Loc.T("HP", "体力"), $"{world.Player.MaxHp}", Loc.T("HP regen", "体力回復"), Loc.T($"{st.HpRegen:0.0}/s", $"{st.HpRegen:0.0}/秒") },
                { "MP", $"{world.Player.MaxMp}", Loc.T("MP regen", "MP回復"), Loc.T($"{st.MpRegen:0.0}/s", $"{st.MpRegen:0.0}/秒") },
                { Loc.T("Damage", "ダメージ"), $"{st.Damage:0}", Loc.T("Attacks/s", "攻撃/秒"), $"{st.AttacksPerSecond:0.0}" },
                { Loc.T("DPS", "秒間ダメージ"), $"{st.Damage * st.AttacksPerSecond * (1f + st.CritChance * (st.CritMult - 1f)):0}", Loc.T("Crit", "会心"), $"{st.CritChance * 100f:0}%  x{st.CritMult:0.00}" },
                { Loc.T("Armor", "防御"), $"{st.Armor:0}", Loc.T("Speed", "速さ"), $"{st.MoveSpeed:0.0}" },
                { Loc.T("Light", "明るさ"), $"{world.Player.EffectiveLight:0.0}", Loc.T("MP next Lv", "次のLvのMP"), $"+{Progression.MpPerLevel(Progression.BaseWis(ch))}" },
                { "HIT", Loc.T($"{st.Hit} ({st.HitChance(world.Enemies.FloorTier) * 100f:0}%)", $"{st.Hit}（{st.HitChance(world.Enemies.FloorTier) * 100f:0}%）"),
                  "EVA", Loc.T($"{st.Eva} ({st.EvadeChance(world.Enemies.FloorTier) * 100f:0}%)", $"{st.Eva}（{st.EvadeChance(world.Enemies.FloorTier) * 100f:0}%）") },
            };
            var head = new GUIStyle(label) { fontStyle = FontStyle.Bold };
            head.normal.textColor = new Color(1f, 0.85f, 0.4f);
            GUI.Label(new Rect(pad, y, W, lineH), Loc.T("RESULT", "最終的な能力"), head);
            y += lineH * 1.1f;
            var key = new GUIStyle(small);
            key.normal.textColor = new Color(0.65f, 0.65f, 0.7f);
            var val = new GUIStyle(label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight };
            float colW = (W - pad * 2f - W * 0.04f) * 0.5f;
            float cellH = label.fontSize * 1.5f;
            for (int r = 0; r < cells.GetLength(0); r++)
            {
                Hud.Fill(new Rect(pad, y, W - pad * 2f, cellH), new Color(1f, 1f, 1f, r % 2 == 0 ? 0.05f : 0.02f));
                for (int c = 0; c < 2; c++)
                {
                    float x = pad + c * (colW + W * 0.04f);
                    GUI.Label(new Rect(x + W * 0.01f, y, colW, cellH), cells[r, c * 2], new GUIStyle(key) { alignment = TextAnchor.MiddleLeft });
                    GUI.Label(new Rect(x, y, colW - W * 0.01f, cellH), cells[r, c * 2 + 1], val);
                }
                y += cellH;
            }
            y += lineH * 0.3f;
            GUI.Label(new Rect(pad, y, W - pad * 2f, lineH), kind, small);
            y += lineH * 1.1f;

            // 加護（祭壇で得たもの）。同じものはまとめて「x2」、折り返して全部見せる
            GUI.Label(new Rect(pad, y, W, lineH), Loc.T("BOONS", "加護"), head);
            y += lineH * 1.1f;
            DrawBoons(world.Player, new Rect(pad, y, W - pad * 2f, bottomY - y - H * 0.01f));

            // 振り直しとレベルのリセット（2回押しで実行）
            float gap = W * 0.03f;
            float half = (W - pad * 2f - gap) * 0.5f;
            ConfirmButton(new Rect(pad, bottomY, half, btnH), "points", Loc.T("RESET POINTS", "ポイントを振り直す"), world.ResetPoints);
            ConfirmButton(new Rect(pad + half + gap, bottomY, half, btnH), "level", Loc.T("RESET ALL", "すべてリセット"), world.ResetLevel);
        }

        int selectedBoon = -1;

        /// <summary>
        /// 加護の一覧：アイコンを並べ（同じ加護は数を右下に）、押すとその下に名前と説明を出す。
        /// </summary>
        void DrawBoons(Player p, Rect area)
        {
            var small = hud.SmallStyle;
            float lineH = small.fontSize * 1.35f;
            if (p.Boons.Count == 0)
            {
                GUI.Label(new Rect(area.x, area.y, area.width, lineH), Loc.T("None yet. Offer embers at an altar.", "まだない。祭壇で火種を払うと得られる。"), small);
                return;
            }
            var order = new System.Collections.Generic.List<int>();
            var count = new System.Collections.Generic.Dictionary<int, int>();
            foreach (int b in p.Boons)
            {
                if (!count.ContainsKey(b)) { order.Add(b); count[b] = 0; }
                count[b]++;
            }
            if (!count.ContainsKey(selectedBoon)) selectedBoon = order[0];

            // アイコン（加護は12種類なので、1行に入るだけ並べて折り返す）
            float detailH = lineH * 2.2f;
            float gap = area.width * 0.015f;
            int cols = 8;
            float cell = (area.width - gap * (cols - 1)) / cols;
            int rows = (order.Count + cols - 1) / cols;
            float maxCell = (area.height - detailH) / rows - gap;
            if (maxCell < cell) cell = Mathf.Max(lineH, maxCell);
            var badge = new GUIStyle(small) { alignment = TextAnchor.LowerRight, fontStyle = FontStyle.Bold };
            badge.normal.textColor = new Color(1f, 0.9f, 0.5f);
            for (int i = 0; i < order.Count; i++)
            {
                int b = order[i];
                var r = new Rect(area.x + (i % cols) * (cell + gap), area.y + (i / cols) * (cell + gap), cell, cell);
                hud.Button(r, null, b == selectedBoon ? new Color(1f, 0.85f, 0.4f, 0.4f) : new Color(1f, 1f, 1f, 0.08f), hud.CenteredStyle, () => selectedBoon = b);
                float inset = cell * 0.12f;
                Hud.DrawSprite(new Rect(r.x + inset, r.y + inset, cell - inset * 2f, cell - inset * 2f), PixelArt.BoonIcon(b));
                if (count[b] > 1) GUI.Label(new Rect(r.x, r.y, r.width - cell * 0.05f, r.height), $"x{count[b]}", badge);
            }

            // 選んだ加護の名前と説明
            float y = area.y + rows * (cell + gap);
            var spec = Boons.Get(selectedBoon);
            var name = new GUIStyle(small) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            name.normal.textColor = new Color(1f, 0.9f, 0.6f);
            string n = count[selectedBoon] > 1 ? $"{spec.DisplayName} x{count[selectedBoon]}" : spec.DisplayName;
            Hud.FitLabel(new Rect(area.x, y, area.width, lineH), n, name);
            Hud.FitLabel(new Rect(area.x, y + lineH, area.width, lineH), spec.DisplayDesc, new GUIStyle(small) { alignment = TextAnchor.MiddleLeft });
        }

        void ConfirmButton(Rect r, string id, string text, System.Action action)
        {
            bool isArmed = armed == id && Time.unscaledTime < armedUntil;
            var back = isArmed ? new Color(0.7f, 0.15f, 0.15f, 0.8f) : new Color(1f, 1f, 1f, 0.12f);
            hud.Button(r, isArmed ? Loc.T("TAP AGAIN", "もう一度押す") : text, back, hud.CenteredStyle, () =>
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
