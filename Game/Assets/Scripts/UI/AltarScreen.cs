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
            GUI.Label(new Rect(0, H * 0.1f, W, H * 0.06f), "ALTAR", title);

            int cost = Boons.Cost(world.Floors.Floor);
            int embers = world.Inventory.Embers;
            GUI.Label(new Rect(0, H * 0.17f, W, H * 0.035f), $"Offer {cost} embers for one boon   (you have {embers})", centered);
            GUI.Label(new Rect(0, H * 0.2f, W, H * 0.035f), "Boons last until the run ends.", centered);

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
                name.normal.textColor = afford ? new Color(1f, 0.9f, 0.6f) : new Color(0.55f, 0.55f, 0.55f);
                GUI.Label(new Rect(r.x, r.y + bh * 0.15f, r.width, bh * 0.4f), spec.Name, name);
                desc.normal.textColor = afford ? Color.white : new Color(0.55f, 0.55f, 0.55f);
                GUI.Label(new Rect(r.x, r.y, r.width, bh * 0.85f), spec.Desc, desc);
                y += bh + gap;
            }
            if (!afford) GUI.Label(new Rect(0, y, W, H * 0.035f), "Not enough embers.", centered);

            // これまでに得た加護
            if (world.Player.Boons.Count > 0)
            {
                var names = new System.Text.StringBuilder("Your boons: ");
                for (int i = 0; i < world.Player.Boons.Count; i++)
                {
                    if (i > 0) names.Append(", ");
                    names.Append(Boons.Get(world.Player.Boons[i]).Name);
                }
                var list = new GUIStyle(small) { wordWrap = true, alignment = TextAnchor.UpperCenter };
                GUI.Label(new Rect(pad, y + H * 0.04f, W - pad * 2f, H * 0.1f), names.ToString(), list);
            }

            hud.Button(new Rect(pad, H - H * 0.1f, W - pad * 2f, H * 0.05f), "LEAVE", new Color(1f, 1f, 1f, 0.1f), centered,
                () => world.OpenPanel(false, false, false));
        }
    }
}
