using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 階段の前の「帰還する／下りる／とどまる」の画面（開いている間はゲームが止まる）。
    /// 帰還すると、装備中の5つと持ち込んだ品はそのまま、今回拾った品からは持ち帰り枠の数まで選んで持ち帰れる。
    /// 選び忘れを防ぐため、開いたときにレア度の高い順（同じならレベルの高い順）で枠の数まで選んでおく。
    /// 枠が余っていて選べる品が残っているまま帰還しようとしたら、確認を出す。
    /// </summary>
    public sealed class ReturnScreen
    {
        static readonly Color panel = new Color(0f, 0f, 0f, 0.9f);
        static readonly Color buttonBack = new Color(1f, 1f, 1f, 0.15f);

        readonly Hud hud;
        readonly List<Item> chosen = new List<Item>();
        Item focused;
        bool opened;      // 開いたときの自動選択を済ませたか
        bool confirming;  // 「このまま帰還する？」の確認中か
        int lastFrame = -10;

        public ReturnScreen(Hud hud)
        {
            this.hud = hud;
        }

        public void Draw(GameWorld world)
        {
            var ch = Profile.Current;
            if (ch == null) return;
            var inv = world.Inventory;
            var candidates = inv.NewThisRun();
            chosen.RemoveAll(it => !candidates.Contains(it));
            if (Time.frameCount - lastFrame > 1) { opened = false; confirming = false; } // 閉じていた → 開き直した
            lastFrame = Time.frameCount;
            if (!opened)
            {
                opened = true;
                chosen.Clear();
                var best = new List<Item>(candidates);
                best.Sort((a, b) => a.Rarity != b.Rarity ? b.Rarity.CompareTo(a.Rarity) : b.Level.CompareTo(a.Level));
                for (int i = 0; i < best.Count && chosen.Count < ch.CarrySlots; i++) chosen.Add(best[i]);
            }

            float W = Screen.width;
            float H = Screen.height;
            float pad = W * 0.05f;
            float gap = W * 0.015f;
            float btnH = H * 0.05f;
            var label = hud.LabelStyle;
            var small = hud.SmallStyle;
            var centered = hud.CenteredStyle;
            float lineH = small.fontSize * 1.35f;

            Hud.Fill(new Rect(0, 0, W, H), panel);

            var title = new GUIStyle(label) { fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(label.fontSize * 1.3f) };
            title.normal.textColor = new Color(1f, 0.85f, 0.4f);
            float y = pad;
            int next = world.Floors.Floor + 1;
            Hud.FitLabel(new Rect(pad, y, W, lineH * 1.6f), Loc.T("Stairs  -  ", "階段  -  ") + GameConfig.FloorName(world.Floors.Floor), title);
            y += lineH * 1.8f;
            bool last = world.Floors.Floor >= GameConfig.LastFloorFor(ch);
            string lead = last ? Loc.T("This is the last floor. Return home to keep your gear.", "ここが最後の層。帰還して装備を持ち帰ろう。")
                : next == GameConfig.AbyssFirstFloor ? Loc.T("Return home, or descend into the ABYSS.", "帰還するか、深淵へ下りるか。")
                : Loc.T($"Return home to keep your gear, or descend to {GameConfig.FloorName(next)}" + (GameConfig.IsBossFloor(next) ? " (BOSS)." : "."), $"帰還して装備を持ち帰るか、{GameConfig.FloorName(next)}へ下りるか" + (GameConfig.IsBossFloor(next) ? "（ボス）。" : "。"));
            GUI.Label(new Rect(pad, y, W - pad * 2f, lineH), lead, small);
            y += lineH;
            GUI.Label(new Rect(pad, y, W - pad * 2f, lineH), Loc.T("Equipped gear and carried-in items are always kept.", "装備中の品と、持ち込んだ品はいつも持ち帰れます。"), small);
            y += lineH;
            var pick = new GUIStyle(small) { fontStyle = FontStyle.Bold };
            pick.normal.textColor = new Color(0.7f, 1f, 0.7f);
            GUI.Label(new Rect(pad, y, W - pad * 2f, lineH),
                Loc.T($"Pick new items to bring back: {chosen.Count} / {ch.CarrySlots}   Embers +{inv.Embers}", $"持ち帰る新しい品を選ぶ：{chosen.Count} / {ch.CarrySlots}   火種 +{inv.Embers}"), pick);
            y += lineH * 1.4f;

            // 今回拾った品（タップで選ぶ／外す）
            const int cols = 6;
            float cell = (W - pad * 2f - gap * (cols - 1)) / cols;
            if (candidates.Count == 0)
            {
                GUI.Label(new Rect(pad, y, W, lineH), Loc.T("(No new items in the bag)", "（鞄に新しい品はありません）"), small);
                y += lineH * 1.5f;
            }
            for (int i = 0; i < candidates.Count; i++)
            {
                var item = candidates[i];
                var r = new Rect(pad + (i % cols) * (cell + gap), y + (i / cols) * (cell + gap), cell, cell);
                bool on = chosen.Contains(item);
                var c = RarityInfo.ColorOf(item.Rarity);
                var back = on ? new Color(0.3f, 0.8f, 0.3f, 0.6f) : new Color(c.r, c.g, c.b, 0.22f);
                hud.Button(r, null, back, centered, () =>
                {
                    focused = item;
                    if (chosen.Contains(item)) chosen.Remove(item);
                    else if (chosen.Count < ch.CarrySlots) chosen.Add(item);
                });
                float inset = r.width * 0.15f;
                Hud.DrawSprite(new Rect(r.x + inset, r.y + inset, r.width - inset * 2f, r.height - inset * 2f), PixelArt.ItemIcon(item));
                Hud.DrawAbyssFrame(r, item);
                Hud.DrawEnhance(r, item);
            }
            int rows = (candidates.Count + cols - 1) / cols;
            y += rows * (cell + gap) + H * 0.01f;

            // 最後に触った品の内容
            if (focused != null && candidates.Contains(focused))
            {
                var name = new GUIStyle(label) { fontStyle = FontStyle.Bold };
                name.normal.textColor = RarityInfo.ColorOf(focused.Rarity);
                GUI.Label(new Rect(pad, y, W - pad * 2f, lineH * 1.3f), focused.Name, name);
                y += lineH * 1.3f;
                foreach (var line in focused.Describe())
                {
                    if (y + lineH > H - pad - btnH * 1.3f) break;
                    GUI.Label(new Rect(pad, y, W - pad * 2f, lineH), line, small);
                    y += lineH;
                }
            }

            // 帰還する／下りる／とどまる
            float third = (W - pad * 2f - gap * 2f) / 3f;
            float by = H - pad - btnH;
            int canPick = Mathf.Min(ch.CarrySlots, candidates.Count) - chosen.Count;
            if (confirming)
            {
                DrawConfirm(world, canPick, W, H, pad, gap, btnH, label, small, centered);
                return;
            }
            hud.Button(new Rect(pad, by, third, btnH), Loc.T($"RETURN ({chosen.Count})", $"帰還 ({chosen.Count})"), new Color(0.2f, 0.5f, 0.25f, 0.85f), centered, () =>
            {
                if (canPick > 0) confirming = true;
                else Return(world);
            });
            if (last)
            {
                Hud.Fill(new Rect(pad + third + gap, by, third, btnH), new Color(1f, 1f, 1f, 0.05f));
                GUI.Label(new Rect(pad + third + gap, by, third, btnH), Loc.T("LAST FLOOR", "最後の層"), centered);
            }
            else hud.Button(new Rect(pad + third + gap, by, third, btnH), Loc.T("DESCEND", "下りる"), new Color(0.5f, 0.35f, 0.15f, 0.85f), centered, () =>
            {
                Close();
                world.Descend();
            });
            hud.Button(new Rect(pad + (third + gap) * 2f, by, third, btnH), Loc.T("STAY", "とどまる"), buttonBack, centered, () =>
            {
                Close();
                world.StayOnFloor();
            });
        }

        void Close()
        {
            chosen.Clear();
            focused = null;
            opened = false;
            confirming = false;
        }

        void Return(GameWorld world)
        {
            var list = new List<Item>(chosen);
            Close();
            world.ReturnHome(list);
        }

        /// <summary>枠が余っているのに帰還しようとしたときの確認。</summary>
        void DrawConfirm(GameWorld world, int canPick, float W, float H, float pad, float gap, float btnH, GUIStyle label, GUIStyle small, GUIStyle centered)
        {
            Hud.Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.6f));
            var box = new Rect(pad, H * 0.36f, W - pad * 2f, H * 0.26f);
            Hud.Fill(box, new Color(0.08f, 0.07f, 0.12f, 0.97f));
            var head = new GUIStyle(centered) { fontStyle = FontStyle.Bold };
            head.normal.textColor = new Color(1f, 0.85f, 0.4f);
            Hud.FitLabel(new Rect(box.x, box.y + box.height * 0.08f, box.width, box.height * 0.2f), Loc.T("Return with room to spare?", "まだ持ち帰れます"), head);
            var body = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
            GUI.Label(new Rect(box.x + gap, box.y + box.height * 0.3f, box.width - gap * 2f, box.height * 0.3f),
                Loc.T($"You can still pick {canPick} more item(s). Items you leave behind are lost.", $"あと {canPick} 個選べます。選ばなかった品は置いていくことになります。"), body);
            float bw = (box.width - gap * 3f) / 2f, by = box.yMax - btnH - box.height * 0.08f;
            hud.Button(new Rect(box.x + gap, by, bw, btnH), Loc.T("RETURN ANYWAY", "このまま帰還"), new Color(0.2f, 0.5f, 0.25f, 0.85f), centered, () => Return(world));
            hud.Button(new Rect(box.x + gap * 2f + bw, by, bw, btnH), Loc.T("PICK MORE", "選び直す"), new Color(1f, 1f, 1f, 0.15f), centered, () => confirming = false);
        }
    }
}
