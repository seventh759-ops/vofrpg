using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 祭壇の画面（開いている間はゲームが止まる）。並んだ3つの加護から1つを、火種を払って選ぶ。
    /// 1つの層で1回だけ。加護はその挑戦のあいだずっと効く。
    /// </summary>
    public sealed class AltarScreen
    {
        static readonly Color panel = new Color(0f, 0f, 0f, 0.85f);

        readonly Hud hud;

        public AltarScreen(Hud hud)
        {
            this.hud = hud;
        }

        public void Draw(GameWorld world)
        {
            float W = Screen.width;
            float H = Screen.height;
            float pad = W * 0.08f;
            float gap = H * 0.02f;
            var centered = hud.CenteredStyle;
            var small = hud.SmallStyle;

            Hud.Fill(new Rect(0, 0, W, H), panel);
            var title = new GUIStyle(centered) { fontSize = hud.LabelStyle.fontSize * 2, fontStyle = FontStyle.Bold };
            title.normal.textColor = new Color(1f, 0.85f, 0.45f);
            Hud.FitLabel(new Rect(0, H * 0.1f, W, H * 0.06f), Loc.T("ALTAR", "祭壇"), title);

            int cost = Boons.Cost(world.Floors.Floor);
            int embers = world.Inventory.Embers;
            Hud.FitLabel(new Rect(pad * 0.5f, H * 0.17f, W - pad, H * 0.035f), Loc.T($"Offer {cost} embers for one boon   (found this run: {embers})", $"火種 {cost} で加護を1つ   （今回拾った火種 {embers}）"), centered);
            var note = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter };
            note.normal.textColor = new Color(0.8f, 0.8f, 0.85f);
            Hud.FitLabel(new Rect(pad * 0.5f, H * 0.205f, W - pad, H * 0.03f), Loc.T("Only embers found this run can be offered. Boons last until the run ends.", "払えるのは今回拾った火種だけ。加護はこの挑戦が終わるまで効きます。"), note);

            float y = H * 0.26f;
            float bh = H * 0.1f;
            bool afford = embers >= cost;
            var name = new GUIStyle(hud.LabelStyle) { fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter };
            var desc = new GUIStyle(small) { alignment = TextAnchor.LowerCenter };
            foreach (int boon in world.AltarOffers)
            {
                var spec = Boons.Get(boon);
                var r = new Rect(pad, y, W - pad * 2f, bh);
                int pick = boon;
                if (afford) hud.Button(r, null, new Color(1f, 0.8f, 0.3f, 0.18f), centered, () => world.BuyBoon(pick));
                else Hud.Fill(r, new Color(1f, 1f, 1f, 0.05f));
                float icon = bh * 0.6f;
                var oldColor = GUI.color;
                if (!afford) GUI.color = new Color(1f, 1f, 1f, 0.4f);
                Hud.DrawSprite(new Rect(r.x + bh * 0.2f, r.y + bh * 0.2f, icon, icon), PixelArt.BoonIcon(boon));
                GUI.color = oldColor;
                name.normal.textColor = afford ? new Color(1f, 0.9f, 0.6f) : new Color(0.55f, 0.55f, 0.55f);
                GUI.Label(new Rect(r.x, r.y + bh * 0.15f, r.width, bh * 0.4f), spec.DisplayName, name);
                desc.normal.textColor = afford ? Color.white : new Color(0.55f, 0.55f, 0.55f);
                GUI.Label(new Rect(r.x, r.y, r.width, bh * 0.85f), spec.DisplayDesc, desc);
                y += bh + gap;
            }
            if (!afford) GUI.Label(new Rect(0, y, W, H * 0.035f), Loc.T("Not enough embers.", "火種が足りません。"), centered);

            // これまでに得た加護（アイコンと数。多くても重ならないように）
            if (!afford) y += H * 0.04f; // 「火種が足りません」の下から
            if (world.Player.Boons.Count > 0)
            {
                var head = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(0, y + H * 0.02f, W, H * 0.03f), Loc.T("Your boons", "得た加護"), head);
                DrawOwned(world.Player, new Rect(pad, y + H * 0.055f, W - pad * 2f, H * 0.9f - (y + H * 0.06f)));
            }

            hud.Button(new Rect(pad, H - H * 0.1f, W - pad * 2f, H * 0.05f), Loc.T("LEAVE", "立ち去る"), new Color(1f, 1f, 1f, 0.1f), centered,
                () => world.OpenPanel(false, false, false));
        }
        /// <summary>得た加護をアイコンで並べ、重ねた数を右下に出す。</summary>
        void DrawOwned(Player p, Rect area)
        {
            var order = new System.Collections.Generic.List<int>();
            var count = new System.Collections.Generic.Dictionary<int, int>();
            foreach (int b in p.Boons)
            {
                if (!count.ContainsKey(b)) { order.Add(b); count[b] = 0; }
                count[b]++;
            }
            const int cols = 6;
            float gap = area.width * 0.02f;
            float cell = Mathf.Min((area.width - gap * (cols - 1)) / cols, area.height / 2f - gap);
            float rowW = cell * cols + gap * (cols - 1);
            float x0 = area.x + (area.width - rowW) * 0.5f;
            var badge = new GUIStyle(hud.SmallStyle) { alignment = TextAnchor.LowerRight, fontStyle = FontStyle.Bold };
            badge.normal.textColor = new Color(1f, 0.9f, 0.5f);
            for (int i = 0; i < order.Count; i++)
            {
                var r = new Rect(x0 + (i % cols) * (cell + gap), area.y + (i / cols) * (cell + gap), cell, cell);
                if (r.yMax > area.yMax) break;
                Hud.Fill(r, new Color(1f, 1f, 1f, 0.08f));
                float inset = cell * 0.14f;
                Hud.DrawSprite(new Rect(r.x + inset, r.y + inset, cell - inset * 2f, cell - inset * 2f), PixelArt.BoonIcon(order[i]));
                if (count[order[i]] > 1) GUI.Label(new Rect(r.x, r.y, r.width - cell * 0.06f, r.height), $"x{count[order[i]]}", badge);
            }
        }
    }
}
