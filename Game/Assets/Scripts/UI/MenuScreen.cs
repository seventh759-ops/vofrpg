using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// メニュー（右上の MENU。PC は Esc でも開く）。開いている間はゲームが止まる。
    /// 鞄・能力値・スキル・設定への入口。攻撃のしかた・音楽・BGM・FPS 表示・言語・操作の一覧は「設定」の画面にまとめる。
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
            if (settings)
            {
                DrawSettings(world, W, H, pad, gap, centered);
                return;
            }
            if (codex)
            {
                DrawCodex(W, H, pad, gap, centered);
                return;
            }
            var ch = Profile.Current;
            var inv = world.Inventory;
            int points = ch != null ? ch.Points : 0;

            // 題とボタンの塊を、画面の上下の真ん中に置く
            float bw = W - pad * 2f;
            float bh = H * 0.08f;
            float sh0 = H * 0.06f;
            float block = H * 0.07f + 5f * bh + 4f * gap + gap * 2f + sh0;
            float top = (H - block) * 0.5f;
            var title = new GUIStyle(centered) { fontSize = hud.LabelStyle.fontSize * 2, fontStyle = FontStyle.Bold };
            Hud.FitLabel(new Rect(0, top, W, H * 0.06f), "MENU", title);
            float y = top + H * 0.07f;
            hud.Button(new Rect(pad, y, bw, bh), Loc.T($"BAG   {inv.Bag.Count}/{Inventory.BagCapacity}   Embers {inv.Embers}", $"鞄   {inv.Bag.Count}/{Inventory.BagCapacity}   火種 {inv.Embers}"),
                inv.BagFull ? highlight : main, centered, () => world.OpenPanel(true, false, false));
            y += bh + gap;
            hud.Button(new Rect(pad, y, bw, bh), points > 0 ? Loc.T($"STATS   +{points} points", $"能力値   ポイント +{points}") : Loc.T("STATS", "能力値"),
                points > 0 ? highlight : main, centered, () => world.OpenPanel(false, true, false));
            y += bh + gap;
            hud.Button(new Rect(pad, y, bw, bh), Loc.T("SKILLS", "スキル"), main, centered, () => world.OpenPanel(false, false, true));
            y += bh + gap;
            hud.Button(new Rect(pad, y, bw, bh), Loc.T($"CODEX   {Codex.Found}/{Codex.Total}", $"図鑑   {Codex.Found}/{Codex.Total}"), main, centered, () => codex = true);
            y += bh + gap * 2f;

            // 設定の画面への入口（攻撃のしかた・音楽・BGM・FPS・言語・操作の一覧）
            float sh = sh0;
            hud.Button(new Rect(pad, y, bw, bh), Loc.T("SETTINGS", "設定"), main, centered, () => settings = true);
            y += bh + gap * 2f;
            hud.Button(new Rect(pad, y, bw, sh), Loc.T("CLOSE", "閉じる"), sub, centered, () => world.OpenPanel(false, false, false));
        }

        /// <summary>メニューを開き直したときは、設定ではなくメニューの最初から。</summary>
        public void Reset()
        {
            settings = false;
            settingsOnly = false;
            codex = false;
        }

        bool codex;          // 図鑑の画面を開いているか
        Codex.Category codexTab;

        /// <summary>
        /// 図鑑：上に分類のタブ、真ん中に見つけたものの絵（まだのものは黒い影）、下に進み具合と次の報酬。
        /// </summary>
        void DrawCodex(float W, float H, float pad, float gap, GUIStyle centered)
        {
            float bw = W - pad * 2f;
            float top = H * 0.08f;
            var title = new GUIStyle(centered) { fontSize = hud.LabelStyle.fontSize * 2, fontStyle = FontStyle.Bold };
            Hud.FitLabel(new Rect(0, top, W, H * 0.06f), Loc.T("CODEX", "図鑑"), title);
            float y = top + H * 0.07f;

            // 進み具合と報酬
            int found = Codex.Found, total = Codex.Total;
            int next = (found / Codex.PerReward + 1) * Codex.PerReward;
            string progress = Loc.T($"{found}/{total}   All stats +{Codex.AttrBonus}", $"{found}/{total}   全能力値 +{Codex.AttrBonus}");
            Hud.FitLabel(new Rect(pad, y, bw, H * 0.035f), progress, centered);
            y += H * 0.035f;
            var small = new GUIStyle(hud.SmallStyle) { alignment = TextAnchor.MiddleCenter };
            small.normal.textColor = new Color(0.8f, 0.8f, 0.85f);
            string hint = found >= total ? Loc.T("Complete!", "すべて埋まった！")
                : Loc.T($"Next +1 at {Mathf.Min(next, total)}  (every {Codex.PerReward} entries)", $"次の +1 まで あと {Mathf.Min(next, total) - found}（{Codex.PerReward}つごとに +1）");
            if (next > total && found < total) hint = Loc.T("No more rewards, but there is more to find", "報酬はここまで。まだ埋まっていないものがある");
            Hud.FitLabel(new Rect(pad, y, bw, H * 0.03f), hint, small);
            y += H * 0.03f + gap;

            // 分類のタブ
            string[] names = { Loc.T("Enemies", "敵"), Loc.T("Bosses", "ボス"), Loc.T("Uniques", "固有効果"), Loc.T("Abyss Sets", "深淵のセット") };
            float tw = (bw - gap * (Codex.CategoryCount - 1)) / Codex.CategoryCount;
            float th = H * 0.05f;
            for (int i = 0; i < Codex.CategoryCount; i++)
            {
                var c = (Codex.Category)i;
                bool on = c == codexTab;
                hud.Button(new Rect(pad + i * (tw + gap), y, tw, th), $"{names[i]} {Codex.FoundIn(c)}/{Codex.TotalIn(c)}",
                    on ? new Color(0.5f, 0.4f, 0.05f, 0.75f) : sub, small, () => codexTab = c);
            }
            y += th + gap;

            // 中身
            float backH = H * 0.06f;
            float areaH = H - y - backH - H * 0.06f;
            var list = new System.Collections.Generic.List<Codex.Entry>();
            foreach (var e in Codex.All) if (e.Cat == codexTab) list.Add(e);
            bool textRows = codexTab == Codex.Category.Uniques; // 固有効果は文が長いので、1行ずつ
            int cols = textRows ? 1 : codexTab == Codex.Category.Bosses ? 3 : 4;
            int rows = Mathf.CeilToInt(list.Count / (float)cols);
            float cw = (bw - gap * (cols - 1)) / cols;
            float ch = Mathf.Min(textRows ? H * 0.07f : cw * 1.25f, (areaH - gap * (rows - 1)) / Mathf.Max(1, rows));
            var nameStyle = new GUIStyle(hud.SmallStyle) { alignment = textRows ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter, wordWrap = textRows };
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                bool has = Profile.CodexHas(e.Id);
                var cell = new Rect(pad + (i % cols) * (cw + gap), y + (i / cols) * (ch + gap), cw, ch);
                Hud.Fill(cell, new Color(1f, 1f, 1f, has ? 0.08f : 0.04f));
                Hud.OrnateFrame(cell, has ? Hud.FrameGold : new Color(0.4f, 0.38f, 0.34f, 1f), Mathf.Max(1f, Mathf.Round(H * 0.0008f)), true, false);
                float lineH = hud.SmallStyle.fontSize * 1.3f;
                Rect pic = textRows ? new Rect(cell.x + ch * 0.1f, cell.y + ch * 0.1f, ch * 0.8f, ch * 0.8f)
                                    : new Rect(cell.x + cw * 0.12f, cell.y + ch * 0.04f, cw * 0.76f, ch - lineH * 2.2f);
                var old = GUI.color;
                GUI.color = has ? Color.white : new Color(0f, 0f, 0f, 0.85f); // まだのものは黒い影
                Hud.DrawSprite(pic, e.Icon());
                GUI.color = old;
                nameStyle.normal.textColor = has ? Color.white : new Color(0.55f, 0.55f, 0.6f);
                if (textRows)
                {
                    GUI.Label(new Rect(pic.xMax + gap, cell.y, cell.xMax - pic.xMax - gap * 2f, ch), has ? e.Name() : "？？？", nameStyle);
                }
                else
                {
                    Hud.FitLabel(new Rect(cell.x, cell.yMax - lineH * 2.1f, cw, lineH), has ? e.Name() : "？？？", nameStyle);
                    string d = e.Desc();
                    if (!string.IsNullOrEmpty(d))
                    {
                        var ds = new GUIStyle(nameStyle) { fontSize = Mathf.Max(8, Mathf.RoundToInt(nameStyle.fontSize * 0.8f)) };
                        ds.normal.textColor = new Color(0.7f, 0.7f, 0.75f);
                        Hud.FitLabel(new Rect(cell.x, cell.yMax - lineH * 1.1f, cw, lineH), d, ds);
                    }
                }
            }

            hud.Button(new Rect(pad, H - backH - H * 0.04f, bw, backH), Loc.T("BACK", "戻る"), sub, centered, () => codex = false);
        }

        bool settings; // 設定の画面を開いているか
        bool settingsOnly; // 町から設定だけを開いた（戻ると閉じる）

        /// <summary>設定の画面だけを開く（町から）。「戻る」で閉じる。</summary>
        public void OpenSettingsOnly()
        {
            settings = true;
            settingsOnly = true;
        }

        /// <summary>設定：音楽・BGM・FPS・言語・操作の一覧。</summary>
        void DrawSettings(GameWorld world, float W, float H, float pad, float gap, GUIStyle centered)
        {
            float bw = W - pad * 2f;
            float sh = H * 0.06f;
            int rows = (Application.isMobilePlatform ? 6 : 7) + (Profile.CurrentEndingSeen ? 1 : 0); // 攻撃・音楽・BGM・FPS・言語・（エンディング）・（操作の一覧）・戻る
            float block = H * 0.07f + rows * sh + rows * gap;
            float top = (H - block) * 0.5f; // 画面の上下の真ん中に
            var title = new GUIStyle(centered) { fontSize = hud.LabelStyle.fontSize * 2, fontStyle = FontStyle.Bold };
            Hud.FitLabel(new Rect(0, top, W, H * 0.06f), Loc.T("SETTINGS", "設定"), title);
            float y = top + H * 0.07f;
            hud.Button(new Rect(pad, y, bw, sh), world.AutoAttack ? Loc.T("Attack: AUTO", "攻撃：自動") : Loc.T("Attack: HOLD", "攻撃：押す間"), sub, centered,
                () => world.AutoAttack = !world.AutoAttack);
            y += sh + gap;
            hud.Button(new Rect(pad, y, bw, sh), Loc.T("Music volume: ", "音楽の音量：") + world.Music.VolumeLabel, sub, centered, () => world.Music.NextVolume()); // 押すたびに、なし → 1 〜 5
            y += sh + gap;
            hud.Button(new Rect(pad, y, bw, sh), $"BGM: {world.Music.ThemeName}", sub, centered, () => world.NextMusic());
            y += sh + gap;
            hud.Button(new Rect(pad, y, bw, sh), hud.ShowFps ? Loc.T("FPS: ON", "FPS：表示") : Loc.T("FPS: OFF", "FPS：非表示"), sub, centered, () => hud.ShowFps = !hud.ShowFps);
            y += sh + gap;
            hud.Button(new Rect(pad, y, bw, sh), $"Language: {Loc.LanguageName}", sub, centered, () => Loc.Next());
            y += sh + gap;
            if (Profile.CurrentEndingSeen)
            {
                // エンディングは1回だけ。見たあとは、ここからクレジット（エンドロール）を見返せる
                hud.Button(new Rect(pad, y, bw, sh), Loc.T("Credits", "クレジット"), sub, centered, () =>
                {
                    settings = false;
                    world.ShowEnding(true);
                });
                y += sh + gap;
            }
            if (!Application.isMobilePlatform)
            {
                hud.Button(new Rect(pad, y, bw, sh), Loc.T("Controls (H)", "操作の一覧（H）"), sub, centered, () =>
                {
                    hud.ShowHelp = true;
                    settings = false;
                    world.OpenPanel(false, false, false);
                });
                y += sh + gap;
            }
            y += gap;
            hud.Button(new Rect(pad, y, bw, sh), Loc.T("BACK", "戻る"), sub, centered, () =>
            {
                settings = false;
                if (settingsOnly)
                {
                    settingsOnly = false;
                    world.OpenPanel(false, false, false); // 町から開いたときは、そのまま閉じる
                }
            });
        }
    }
}
