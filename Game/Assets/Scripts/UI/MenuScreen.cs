using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// メニュー（右上の MENU。PC は Esc でも開く）。開いている間はゲームが止まる。
    /// 鞄・能力値・スキルへの入口と、攻撃モード・BGM・FPS 表示・操作の一覧の設定をまとめる。
    /// </summary>
    public sealed class MenuScreen
    {
        static readonly Color panel = new Color(0f, 0f, 0f, 0.85f);
        static readonly Color main = new Color(1f, 1f, 1f, 0.14f);
        static readonly Color sub = new Color(1f, 1f, 1f, 0.08f);
        static readonly Color highlight = new Color(0.5f, 0.4f, 0.05f, 0.75f);

        readonly Hud hud;

        public MenuScreen(Hud hud)
        {
            this.hud = hud;
        }

        public void Draw(GameWorld world)
        {
            float W = Screen.width;
            float H = Screen.height;
            float pad = W * 0.08f;
            float gap = W * 0.03f;
            var centered = hud.CenteredStyle;

            Hud.Fill(new Rect(0, 0, W, H), panel);
            var title = new GUIStyle(centered) { fontSize = hud.LabelStyle.fontSize * 2, fontStyle = FontStyle.Bold };
            GUI.Label(new Rect(0, H * 0.1f, W, H * 0.06f), Loc.T("MENU", "メニュー"), title);

            var ch = Profile.Current;
            var inv = world.Inventory;
            int points = ch != null ? ch.Points : 0;

            // 大きなボタン：鞄・能力値・スキル
            float bw = W - pad * 2f;
            float bh = H * 0.075f;
            float y = H * 0.2f;
            hud.Button(new Rect(pad, y, bw, bh), Loc.T($"BAG   {inv.Bag.Count}/{Inventory.BagCapacity}   Embers {inv.Embers}", $"鞄   {inv.Bag.Count}/{Inventory.BagCapacity}   火種 {inv.Embers}"),
                inv.BagFull ? highlight : main, centered, () => world.OpenPanel(true, false, false));
            y += bh + gap;
            hud.Button(new Rect(pad, y, bw, bh), points > 0 ? Loc.T($"STATS   +{points} points", $"能力値   ポイント +{points}") : Loc.T("STATS", "能力値"),
                points > 0 ? highlight : main, centered, () => world.OpenPanel(false, true, false));
            y += bh + gap;
            hud.Button(new Rect(pad, y, bw, bh), Loc.T("SKILLS", "スキル"), main, centered, () => world.OpenPanel(false, false, true));
            y += bh + gap * 2f;

            // 設定（2列）
            float half = (bw - gap) * 0.5f;
            float sh = H * 0.055f;
            hud.Button(new Rect(pad, y, half, sh), world.AutoAttack ? Loc.T("Attack: AUTO", "攻撃：自動") : Loc.T("Attack: HOLD", "攻撃：押す間"), sub, centered,
                () => world.AutoAttack = !world.AutoAttack);
            hud.Button(new Rect(pad + half + gap, y, half, sh), world.Music.Muted ? Loc.T("Music: OFF", "音楽：オフ") : Loc.T("Music: ON", "音楽：オン"), sub, centered,
                () => world.Music.ToggleMute());
            y += sh + gap;
            hud.Button(new Rect(pad, y, half, sh), $"BGM: {world.Music.ThemeName}", sub, centered, () => world.NextMusic());
            hud.Button(new Rect(pad + half + gap, y, half, sh), hud.ShowFps ? Loc.T("FPS: ON", "FPS：表示") : Loc.T("FPS: OFF", "FPS：非表示"), sub, centered,
                () => hud.ShowFps = !hud.ShowFps);
            y += sh + gap;
            hud.Button(new Rect(pad, y, bw, sh), $"Language: {Loc.LanguageName}", sub, centered, () => Loc.Next());
            y += sh + gap;
            if (!Application.isMobilePlatform)
            {
                hud.Button(new Rect(pad, y, bw, sh), Loc.T("Controls (H)", "操作の一覧（H）"), sub, centered, () =>
                {
                    hud.ShowHelp = true;
                    world.OpenPanel(false, false, false);
                });
                y += sh + gap;
            }

            y += gap;
            hud.Button(new Rect(pad, y, bw, sh), Loc.T("CLOSE", "閉じる"), sub, centered, () => world.OpenPanel(false, false, false));
        }
    }
}
