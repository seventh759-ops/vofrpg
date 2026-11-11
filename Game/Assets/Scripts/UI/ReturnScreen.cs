using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 階段の前の「帰還する／下りる／とどまる」の画面（開いている間はゲームが止まる）。
    /// 帰還すると、装備中の5つと持ち込んだ品はそのまま、今回拾った品からは持ち帰り枠の数まで選んで持ち帰れる。
    /// </summary>
    public sealed class ReturnScreen
    {
        static readonly Color panel = new Color(0f, 0f, 0f, 0.9f);
        static readonly Color buttonBack = new Color(1f, 1f, 1f, 0.15f);

        readonly Hud hud;
        readonly List<Item> chosen = new List<Item>();
        Item focused;

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
            Hud.FitLabel(new Rect(pad, y, W, lineH * 1.6f), Loc.T($"Stairs  -  Floor {world.Floors.Floor}", $"階段  -  {world.Floors.Floor}層"), title);
            y += lineH * 1.8f;
            bool last = world.Floors.Floor >= GameConfig.MaxFloor;
            string lead = last ? Loc.T("This is the last floor. Return home to keep your gear.", "ここが最後の層。帰還して装備を持ち帰ろう。")
                : Loc.T($"Return home to keep your gear, or descend to floor {next}" + (GameConfig.IsBossFloor(next) ? " (BOSS)." : "."), $"帰還して装備を持ち帰るか、{next}層へ下りるか" + (GameConfig.IsBossFloor(next) ? "（ボス）。" : "。"));
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
                Hud.DrawSprite(new Rect(r.x + inset, r.y + inset, r.width - inset * 2f, r.height - inset * 2f), PixelArt.ItemIcon(item.Slot, item.Rarity));
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
            hud.Button(new Rect(pad, by, third, btnH), Loc.T($"RETURN ({chosen.Count})", $"帰還 ({chosen.Count})"), new Color(0.2f, 0.5f, 0.25f, 0.85f), centered, () =>
            {
                var list = new List<Item>(chosen);
                chosen.Clear();
                focused = null;
                world.ReturnHome(list);
            });
            if (last)
            {
                Hud.Fill(new Rect(pad + third + gap, by, third, btnH), new Color(1f, 1f, 1f, 0.05f));
                GUI.Label(new Rect(pad + third + gap, by, third, btnH), Loc.T("TOP REACHED", "最後の層"), centered);
            }
            else hud.Button(new Rect(pad + third + gap, by, third, btnH), Loc.T("DESCEND", "下りる"), new Color(0.5f, 0.35f, 0.15f, 0.85f), centered, () =>
            {
                chosen.Clear();
                focused = null;
                world.Descend();
            });
            hud.Button(new Rect(pad + (third + gap) * 2f, by, third, btnH), Loc.T("STAY", "とどまる"), buttonBack, centered, () =>
            {
                focused = null;
                world.StayOnFloor();
            });
        }
    }
}
