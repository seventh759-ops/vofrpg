using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 町（メニュー型）。職業を選んだあとに来る。出撃・倉庫番・商人・女神像（転職・転生の儀）・荷運び・職業を変える。
    /// 帰還や死亡のあとも、ここに戻る。
    /// </summary>
    public sealed class TownScreen
    {
        enum Panel { Main, Storage, Merchant, Master, Porter, Smith }

        static readonly Color back = new Color(0.03f, 0.03f, 0.06f, 0.92f);
        static readonly Color row = new Color(1f, 1f, 1f, 0.1f);
        static readonly Color gold = new Color(0.5f, 0.4f, 0.1f, 0.8f);
        static readonly Color dim = new Color(1f, 1f, 1f, 0.05f);

        readonly Hud hud;
        Panel panel;
        Item selected;
        Item matChoice;      // 強化の素材に選んだ品（節目の強化）
        bool matPicking;     // 素材を選ぶ一覧を開いているか
        Item lastBought;
        string armed;
        float armedUntil;

        public TownScreen(Hud hud)
        {
            this.hud = hud;
        }

        /// <summary>Esc：ひとつ前へ（町の入口なら職業選択へ）。</summary>
        public void Back()
        {
            if (panel != Panel.Main)
            {
                panel = Panel.Main;
                selected = null;
            }
            else
            {
                hud.World.ShowClassSelect();
            }
        }

        public void Draw(GameWorld world)
        {
            worldRef = world;
            var ch = Profile.Current;
            if (ch == null) return;
            float W = Screen.width, H = Screen.height;
            DrawBackdrop(W, H);
            if (panel != Panel.Main) Hud.Fill(new Rect(0, 0, W, H), back); // 施設の中は、町の絵の上を暗く覆って文字を読みやすく
            switch (panel)
            {
                case Panel.Storage: DrawStorage(world, ch); break;
                case Panel.Merchant: DrawMerchant(world, ch); break;
                case Panel.Master: DrawMaster(world, ch); break;
                case Panel.Porter: DrawPorter(ch); break;
                case Panel.Smith: DrawSmith(ch); break;
                default: DrawMain(world, ch); break;
            }
        }

        /// <summary>
        /// 町の絵（夜の町並み）を、ドットの大きさがそろうように整数倍で拡大して画面いっぱいに敷く。
        /// 窓と街灯と塔の門の灯りはゆらめく。上の文字の所は暗くして読みやすくする。
        /// </summary>
        public static void DrawBackdrop(float W, float H)
        {
            // アビスを倒してエンディングを見たら、夜明けの町（クリア後の世界）。その前に深淵へ行けるようになったら、深淵版の絵（ほかのキャラクターは関係ない）
            bool dawn = Profile.CurrentEndingSeen;
            bool abyss = !dawn && Profile.CurrentAbyssUnlocked;
            var tex = dawn ? TownScene.DawnTexture : abyss ? TownScene.AbyssTexture : TownScene.Texture;
            float ps = Mathf.Ceil(Mathf.Max(W / TownScene.W, H / TownScene.H));
            var bg = new Rect((W - TownScene.W * ps) * 0.5f, (H - TownScene.H * ps) * 0.5f, TownScene.W * ps, TownScene.H * ps);
            GUI.DrawTexture(bg, tex);
            float t = Time.unscaledTime;
            int i = 0;
            foreach (var w in TownScene.LitWindows)
            {
                float f = 0.6f + 0.4f * Mathf.PerlinNoise(t * 2.5f, i++ * 0.73f);
                Hud.Glow(bg.x + w.x * ps, bg.y + w.y * ps, ps * 7f, abyss ? new Color(1f, 0.25f, 0.45f, 0.25f * f) : new Color(1f, 0.7f, 0.35f, 0.25f * f));
            }
            foreach (var l in TownScene.Lamps)
            {
                float f = 0.7f + 0.3f * Mathf.PerlinNoise(t * 3f, l.x * 0.1f);
                Hud.Glow(bg.x + l.x * ps, bg.y + l.y * ps, ps * 16f, abyss ? new Color(1f, 0.3f, 0.5f, 0.35f * f) : new Color(1f, 0.75f, 0.4f, (dawn ? 0.15f : 0.35f) * f)); // 朝は街灯を控えめに
            }
            float g = 0.75f + 0.25f * Mathf.PerlinNoise(t * 1.5f, 0.5f);
            Hud.Glow(bg.x + TownScene.GateX * ps, bg.y + (TownScene.GateY - 14) * ps, ps * 30f, abyss ? new Color(1f, 0.2f, 0.45f, 0.2f * g) : new Color(1f, 0.7f, 0.35f, 0.18f * g)); // 門の光は控えめに
            // 夜明け版：朝日の光がゆっくり脈打つ
            if (dawn)
            {
                float sun = 0.8f + 0.2f * Mathf.Sin(t * 0.7f);
                Hud.Glow(bg.x + TownScene.MoonX * ps, bg.y + TownScene.MoonY * ps, ps * 60f, new Color(1f, 0.85f, 0.6f, 0.3f * sun));
            }
            // 上の帯（題・名前・火種）と、タイルの並ぶあたりを少し暗く
            for (int b = 0; b < 10; b++)
                Hud.Fill(new Rect(0, H * 0.02f * b, W, H * 0.02f + 1f), new Color(0f, 0f, 0.02f, 0.06f * (10 - b)));
        }

        // ================= 町の入口 =================

        /// <summary>丸（ふちがなめらかな白い円。色は GUI.color で付ける）。丸いボタン・Lv の丸・台座に使う。</summary>
        static Texture2D disk;
        internal static Texture2D Disk
        {
            get
            {
                if (disk != null) return disk;
                const int n = 128;
                disk = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                var px = new Color32[n * n];
                float r = n * 0.5f;
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float d = Mathf.Sqrt((x + 0.5f - r) * (x + 0.5f - r) + (y + 0.5f - r) * (y + 0.5f - r));
                        byte a = (byte)(255 * Mathf.Clamp01(r - d));
                        px[y * n + x] = new Color32(255, 255, 255, a);
                    }
                disk.SetPixels32(px);
                disk.Apply();
                return disk;
            }
        }

        internal static void DrawDisk(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Disk);
            GUI.color = old;
        }

        void DrawMain(GameWorld world, CharacterData ch)
        {
            // 上から：情報のカード（職業・HP/MP｜Lv の丸と経験値の輪｜火種・欠片）／案内役の帯／
            // 真ん中に台座の上の自分（右に丸いボタン：設定・キャラ選択・荷運び）／出撃の門と、左右に能力値・スキル／いちばん下に施設のタブ
            float W = Screen.width, H = Screen.height;
            float pad = W * 0.03f;
            var centered = hud.CenteredStyle;
            var small = hud.SmallStyle;
            var type = ClassInfo.Current;
            float frameT = Mathf.Max(1f, Mathf.Round(H * 0.0009f));
            var st = world.Player.BaseStats ?? world.Player.Stats;
            var panelBack = new Color(0.04f, 0.035f, 0.07f, 0.88f);
            var bold = new GUIStyle(small) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };

            // ---- 情報のカード ----
            var card = new Rect(pad, H * 0.022f, W - pad * 2f, H * 0.1f);
            Hud.Fill(card, panelBack);
            Hud.OrnateFrame(card, Hud.FrameGold, frameT * 2f);
            float lvD = card.height * 1.12f;
            var lvR = new Rect(card.center.x - lvD * 0.5f, card.center.y - lvD * 0.5f, lvD, lvD);
            {
                // 左：職業・転生・HP/MP のバー
                float lx = card.x + card.height * 0.18f, lw = lvR.x - lx - card.height * 0.12f;
                bold.normal.textColor = new Color(1f, 0.88f, 0.55f);
                Hud.FitLabel(new Rect(lx, card.y + card.height * 0.05f, lw, card.height * 0.22f), ClassTitle(type, ch), bold);
                var sub = new GUIStyle(small) { alignment = TextAnchor.MiddleLeft };
                sub.normal.textColor = new Color(0.88f, 0.8f, 1f);
                Hud.FitLabel(new Rect(lx, card.y + card.height * 0.27f, lw, card.height * 0.18f), Loc.T($"Reborn {ch.Rebirths}", $"転生 {ch.Rebirths}回"), sub);
                for (int k = 0; k < 2; k++)
                {
                    bool hpRow = k == 0;
                    var br = new Rect(lx, card.y + card.height * (0.5f + k * 0.245f), lw, card.height * 0.21f);   // バーと数字は読める大きさに
                    Hud.Fill(br, new Color(0f, 0f, 0f, 0.6f));
                    Hud.Fill(new Rect(br.x + 1f, br.y + 1f, br.width - 2f, br.height - 2f), hpRow ? new Color(0.78f, 0.15f, 0.2f, 1f) : new Color(0.22f, 0.38f, 0.82f, 1f));
                    Hud.Fill(new Rect(br.x + 1f, br.y + 1f, br.width - 2f, br.height * 0.35f), new Color(1f, 1f, 1f, 0.2f));
                    NumberFont.DrawIn(br, st != null ? (hpRow ? $"HP {st.MaxHp}" : $"MP {st.MaxMp}") : (hpRow ? "HP" : "MP"), Color.white, TextAnchor.MiddleCenter, 0.85f);
                }
                // 右：火種・欠片
                float rx = lvR.xMax + card.height * 0.12f, rw = card.xMax - rx - card.height * 0.18f;
                bool shards = ch.AbyssShards > 0 || GameConfig.CanEnterAbyss(ch);
                for (int k = 0; k < 2; k++)
                {
                    var pr = new Rect(rx, card.y + card.height * (0.12f + k * 0.42f), rw, card.height * 0.34f);
                    Hud.Fill(pr, new Color(0f, 0f, 0f, 0.45f));
                    Hud.OrnateFrame(pr, new Color(0.85f, 0.72f, 0.42f, 0.7f), frameT, true, false);
                    float ic = pr.height * 0.86f;
                    var oldC = GUI.color;
                    if (k == 1 && !shards) GUI.color = new Color(1f, 1f, 1f, 0.3f);
                    Hud.DrawSprite(new Rect(pr.x + pr.height * 0.1f, pr.y + (pr.height - ic) * 0.5f, ic, ic), k == 0 ? PixelArt.Ember : PixelArt.ShardIcon);
                    GUI.color = oldC;
                    string v = k == 0 ? ch.Embers.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) : (shards ? ch.AbyssShards.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) : "-"); // 区切りはいつもカンマ
                    var vc = k == 0 ? new Color(1f, 0.82f, 0.45f) : (shards ? new Color(0.85f, 0.62f, 1f) : new Color(1f, 1f, 1f, 0.3f));
                    NumberFont.DrawIn(new Rect(pr.x + ic * 1.3f, pr.y, pr.width - ic * 1.3f - pr.height * 0.25f, pr.height), v, vc, TextAnchor.MiddleRight, 0.8f);
                }
                // 真ん中：Lv の丸と経験値の輪
                DrawDisk(new Rect(lvR.x - frameT * 3f, lvR.y - frameT * 3f, lvR.width + frameT * 6f, lvR.height + frameT * 6f), Hud.FrameGold);
                DrawDisk(lvR, new Color(0.05f, 0.045f, 0.09f, 1f));
                float ratio = ch.Level >= Progression.LevelCap(ch) ? 1f : Mathf.Clamp01((float)ch.Exp / Mathf.Max(1, Progression.ExpToNext(ch, ch.Level)));
                float ringR = lvD * 0.42f, dot = Mathf.Max(2f, lvD * 0.055f);
                const int seg = 90;
                for (int i = 0; i < seg; i++)
                {
                    float ang = (-90f + 360f * i / seg) * Mathf.Deg2Rad;
                    var c = i < seg * ratio ? new Color(0.25f, 0.92f, 0.75f, 1f) : new Color(1f, 1f, 1f, 0.1f);
                    Hud.Fill(new Rect(lvR.center.x + Mathf.Cos(ang) * ringR - dot * 0.5f, lvR.center.y + Mathf.Sin(ang) * ringR - dot * 0.5f, dot, dot), c);
                }
                var lvSmall = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter };
                lvSmall.normal.textColor = new Color(0.75f, 0.85f, 1f);
                GUI.Label(new Rect(lvR.x, lvR.y + lvD * 0.18f, lvD, lvD * 0.2f), "Lv", lvSmall);
                NumberFont.DrawIn(new Rect(lvR.x + lvD * 0.15f, lvR.y + lvD * 0.36f, lvD * 0.7f, lvD * 0.36f), ch.Level.ToString(), Color.white, TextAnchor.MiddleCenter, 1f);
            }

            // ---- 案内役の帯（捧げたあとは、案内役はもういない） ----
            float below = card.yMax + H * 0.01f;
            if (!ch.Ascended)
            {
                var ban = new Rect(pad, below, W - pad * 2f, H * 0.052f);
                Hud.Fill(ban, panelBack);
                Hud.OrnateFrame(ban, Hud.FrameGold, frameT, false, false);
                float gd = ban.height * 1.15f;
                var gr = new Rect(ban.x - gd * 0.05f, ban.center.y - gd * 0.5f, gd, gd);
                DrawDisk(gr, new Color(0.85f, 0.72f, 0.42f, 1f));
                DrawDisk(new Rect(gr.x + 3f, gr.y + 3f, gr.width - 6f, gr.height - 6f), new Color(0.22f, 0.18f, 0.3f, 1f));
                Hud.DrawSprite(new Rect(gr.x + gd * 0.1f, gr.y + gd * 0.1f, gd * 0.8f, gd * 0.8f), PixelArt.Guide);
                var say = new GUIStyle(small) { wordWrap = true, alignment = TextAnchor.MiddleLeft };
                say.normal.textColor = new Color(0.95f, 0.92f, 0.85f);
                GUI.Label(new Rect(gr.xMax + W * 0.02f, ban.y, ban.xMax - gr.xMax - W * 0.04f, ban.height), Greeting(ch), say);
                below = ban.yMax + H * 0.008f;
            }

            // ---- いちばん下：施設のタブ ----
            var adv = AdvancedClasses.Of(type, ch);
            bool advReady = adv == null && !ch.Special && ch.Level >= AdvancedClasses.ReqLevel;
            bool thirdReady = adv != null && !ch.Third && ch.Level >= ThirdClasses.PromoteLevel;
            bool rebirthReady = ThirdClasses.MeetsRequirements(ch);
            bool masterNotice = advReady || thirdReady || rebirthReady;
            float tabH = H * 0.095f;
            var tabBar = new Rect(0, H - tabH, W, tabH);

            // ---- 出撃の門と、左右の能力値・スキル ----
            float gateH = H * 0.16f, gateW = gateH * 48f / 52f;
            var gate = new Rect((W - gateW) * 0.5f, tabBar.y - H * 0.012f - gateH, gateW, gateH);
            // 門の左右に2段ずつ：左 能力値／キャラ選択、右 スキル／荷運び
            float sideH = H * 0.046f, sideW = (W - pad * 2f - gateW) * 0.5f - W * 0.03f, sideGap = H * 0.01f;
            float sideTop = gate.y + gateH * 0.55f - sideH - sideGap * 0.5f;
            var statsR = new Rect(pad, sideTop, sideW, sideH);
            var charR = new Rect(pad, sideTop + sideH + sideGap, sideW, sideH);
            var porterR = new Rect(W - pad - sideW, charR.y, sideW, sideH);
            var dock = new Rect(0, sideTop - H * 0.012f, W, H - (sideTop - H * 0.012f)); // 下の台（左右のボタンの上からタブまで）
            var skillR = new Rect(W - pad - sideW, statsR.y, sideW, sideH);

            // ---- 真ん中：台座の上の自分 ----
            {
                float top = below + H * 0.01f, bottom = gate.y - H * 0.03f;
                var big = PixelArt.TownPortrait(ch);
                float px = Mathf.Ceil(Mathf.Max(W / TownScene.W, H / TownScene.H)) * (big.texture.height > 200 ? 0.5f : 1f);
                float bw0 = big.texture.width * px, bh0 = big.texture.height * px;
                float fit = Mathf.Min(1f, Mathf.Min((bottom - top) / bh0, (W - pad * 2f) / bw0));
                bw0 *= fit; bh0 *= fit;
                // 台座（丸い石の舞台）：ふち・面・照り
                float pw = W * 0.62f, ph = pw * 0.24f;
                var ped = new Rect((W - pw) * 0.5f, bottom - ph * 0.62f, pw, ph);
                DrawDisk(new Rect(ped.x, ped.y + ph * 0.12f, pw, ph), new Color(0.18f, 0.15f, 0.14f, 0.95f));
                DrawDisk(ped, new Color(0.55f, 0.5f, 0.45f, 1f));
                DrawDisk(new Rect(ped.x + pw * 0.06f, ped.y + ph * 0.08f, pw * 0.88f, ph * 0.72f), new Color(0.72f, 0.67f, 0.6f, 1f));
                Hud.Glow(ped.center.x, ped.center.y - ph * 0.1f, pw * 0.4f, new Color(1f, 0.85f, 0.6f, 0.18f));
                var pr = new Rect((W - bw0) * 0.5f, ped.center.y - bh0 + ph * 0.05f, bw0, bh0);
                Hud.DrawSprite(pr, big);
            }

            // ---- 右の丸いボタン：設定・キャラ選択・荷運び ----
            {
                float d = W * 0.13f, x = W - pad - d;
                float y = below + H * 0.02f;
                var labels = new[] { Loc.T("Settings", "設定") };
                System.Action[] taps = { world.OpenSettings };
                var lab = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = true };
                lab.normal.textColor = new Color(0.95f, 0.92f, 0.85f);
                for (int k = 0; k < taps.Length; k++)
                {
                    var r = new Rect(x, y + k * (d + H * 0.012f), d, d);
                    DrawDisk(new Rect(r.x - frameT * 2f, r.y - frameT * 2f, d + frameT * 4f, d + frameT * 4f), Hud.FrameGold);
                    DrawDisk(r, new Color(0.06f, 0.055f, 0.1f, 0.95f));
                    hud.Button(r, null, new Color(0f, 0f, 0f, 0f), centered, taps[k]);
                    if (k == 0) Hud.DrawSprite(new Rect(r.x + d * 0.18f, r.y + d * 0.18f, d * 0.64f, d * 0.64f), PixelArt.GearIcon);
                    else if (k == 1) { Hud.DrawSprite(new Rect(r.x + d * 0.2f, r.y + d * 0.06f, d * 0.6f, d * 0.6f), PixelArt.PlayerFor(ch)); Hud.FitLabel(new Rect(r.x, r.y + d * 0.62f, d, d * 0.3f), labels[k], lab); }
                    else GUI.Label(r, labels[k], lab);
                }
            }

            // ---- 出撃の門（押すと出撃）と、能力値・スキル。下の台に乗せる ----
            Hud.Fill(dock, new Color(0.06f, 0.055f, 0.09f, 0.92f));
            Hud.Fill(new Rect(0, dock.y, W, frameT * 2f), Hud.FrameGold);
            Hud.Fill(new Rect(0, dock.y + frameT * 2f, W, frameT), new Color(1f, 0.95f, 0.75f, 0.35f));
            {
                Hud.Glow(gate.center.x, gate.center.y + gateH * 0.05f, gateW * 0.6f, new Color(1f, 0.8f, 0.45f, 0.12f + 0.05f * Mathf.Sin(Time.unscaledTime * 2f))); // 門のまわりにかすかな灯り（中は暗い）
                Hud.DrawSprite(gate, PixelArt.GateIcon);
                hud.Button(gate, null, new Color(0f, 0f, 0f, 0f), centered, world.Depart);
                var dep = new GUIStyle(hud.LabelStyle) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                Hud.OutlinedLabel(new Rect(gate.x, gate.y + gateH * 0.5f, gateW, gateH * 0.18f), Loc.T("DEPART", "出撃"), dep, new Color(0.3f, 0.15f, 0f, 0.9f)); // 暗い扉の中に
                if (GameConfig.CanEnterAbyss(ch))
                {
                    var sub = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                    Hud.OutlinedLabel(new Rect(gate.x, gate.y + gateH * 0.67f, gateW, gateH * 0.12f), Loc.T("tower / abyss", "塔・深淵へ"), sub, new Color(0.3f, 0.15f, 0f, 0.9f));
                }
                bool points = ch.Points > 0;
                Hud.Fill(statsR, new Color(0.02f, 0.02f, 0.05f, 0.85f));
                hud.Button(statsR, points ? Loc.T($"STATS +{ch.Points}", $"能力値 +{ch.Points}") : Loc.T("STATS", "能力値"), points ? gold : row, centered, () => world.OpenTownPanel(true));
                Hud.OrnateFrame(statsR, points ? new Color(1f, 0.85f, 0.4f) : Hud.FrameGold, frameT);
                Hud.Fill(skillR, new Color(0.02f, 0.02f, 0.05f, 0.85f));
                hud.Button(skillR, Loc.T("SKILLS", "スキル"), row, centered, () => world.OpenTownPanel(false));
                Hud.OrnateFrame(skillR, Hud.FrameGold, frameT);
                Hud.Fill(charR, new Color(0.02f, 0.02f, 0.05f, 0.85f));
                hud.Button(charR, Loc.T("Characters", "キャラ選択"), row, centered, world.ShowClassSelect);
                Hud.OrnateFrame(charR, Hud.FrameGold, frameT);
                Hud.Fill(porterR, new Color(0.02f, 0.02f, 0.05f, 0.85f));
                hud.Button(porterR, Loc.T($"Porter {ch.CarrySlots}/{Town.MaxCarrySlots}", $"荷運び {ch.CarrySlots}/{Town.MaxCarrySlots}"), row, centered, () => Open(Panel.Porter));
                Hud.OrnateFrame(porterR, Hud.FrameGold, frameT);
            }

            // ---- 施設のタブ（倉庫・ショップ・女神・カバン・鍛冶） ----
            Hud.Fill(new Rect(pad, tabBar.y, W - pad * 2f, 1f), new Color(0.85f, 0.72f, 0.42f, 0.45f)); // 台の中の仕切り
            {
                float cw = W / 5f;
                var name = new GUIStyle(small) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                void Tab(int i, Sprite icon, string label, System.Action onTap, bool notice = false)
                {
                    var r = new Rect(i * cw, tabBar.y + frameT * 2f, cw, tabH - frameT * 2f);
                    if (notice) Hud.Fill(r, new Color(1f, 0.8f, 0.3f, 0.15f + 0.1f * Mathf.Sin(Time.unscaledTime * 4f)));
                    hud.Button(r, null, new Color(1f, 1f, 1f, 0.03f), centered, onTap);
                    float size = Mathf.Min(r.width * 0.62f, r.height * 0.58f);
                    Hud.DrawSprite(new Rect(r.x + (r.width - size) * 0.5f, r.y + r.height * 0.06f, size, size), icon);
                    name.normal.textColor = notice ? new Color(1f, 0.88f, 0.5f) : Color.white;
                    Hud.FitLabel(new Rect(r.x, r.y + r.height * 0.68f, r.width, r.height * 0.28f), label, name);
                    if (i > 0) Hud.Fill(new Rect(r.x, r.y + r.height * 0.15f, 1f, r.height * 0.7f), new Color(0.85f, 0.72f, 0.42f, 0.3f));
                }
                Tab(0, PixelArt.TownIcon(0), Loc.T("Storage", "倉庫"), () => Open(Panel.Storage));
                Tab(1, PixelArt.TownIcon(1), Loc.T("Shop", "ショップ"), () => Open(Panel.Merchant));
                Tab(2, ch.EndingSeen ? PixelArt.GoddessBleeding : PixelArt.TownIcon(2), Loc.T("Goddess", "女神"), () => Open(Panel.Master), masterNotice); // クリア後は血の涙を流している
                Tab(3, PixelArt.BagIcon, Loc.T("Bag", "カバン"), world.OpenTownBag);
                Tab(4, PixelArt.TownIcon(4), Loc.T("Smith", "鍛冶"), () => Open(Panel.Smith));
            }
        }

        static string ClassTitle(ClassType type, CharacterData ch)
        {
            var spec = ClassInfo.Get(type);
            var adv = AdvancedClasses.Of(type, ch);
            return adv != null ? adv.DisplayName : spec.DisplayName;
        }

        static string Greeting(CharacterData ch)
        {
            if ((ch.Advanced < 0 && !ch.Special && ch.Level >= AdvancedClasses.ReqLevel) || (ch.Advanced >= 0 && !ch.Third && ch.Level >= ThirdClasses.PromoteLevel) || ch.Level >= ThirdClasses.ReqLevel)
                return Loc.T("You have grown strong. Go pray at the goddess statue.", "強くなったね。女神像のところへ行ってみな。");
            if (ch.HpPotions == 0)
                return Loc.T("No potions? The merchant has some.", "薬がないのかい？ 商人が売ってるよ。");
            return Loc.T("Ready? The tower is waiting.", "準備はいいかい？ 塔が待ってる。");
        }

        GameWorld worldRef;

        void Open(Panel p)
        {
            if (p == Panel.Storage && worldRef != null) worldRef.SyncTownGear(); // 比べるために、今の装備を読み直す
            panel = p;
            offerStep = 0; // 捧げる確認は、開き直したら閉じる
            if (p == Panel.Master) masterTab = MasterTab.Class;
            selected = null;
            matChoice = null;
            matPicking = false;
            lastBought = null;
            armed = null;
        }

        /// <summary>見出し（左）と戻るボタン（右）。火種は見出しの下の行に出す（長い見出しとボタンが重ならないように）。</summary>
        void Header(string titleText, int embers, float W, float pad, out float y)
        {
            var label = hud.LabelStyle;
            var title = new GUIStyle(label) { fontStyle = FontStyle.Bold };
            float btnH = Screen.height * 0.045f;
            float btnW = W * 0.22f;
            Hud.FitLabel(new Rect(pad, pad, W - pad * 2f - btnW - W * 0.02f, btnH), titleText, title);
            hud.Button(new Rect(W - pad - btnW, pad, btnW, btnH), Loc.T("BACK", "戻る"), row, hud.CenteredStyle, () => Open(Panel.Main));
            y = pad + btnH + Screen.height * 0.006f;
            var ember = new GUIStyle(hud.SmallStyle) { fontStyle = FontStyle.Bold };
            ember.normal.textColor = new Color(1f, 0.85f, 0.5f);
            float lineH = hud.SmallStyle.fontSize * 1.5f;
            var chH = Profile.Current;
            int shards = chH != null ? chH.AbyssShards : 0;
            // 深淵の欠片は、1つでも持っていれば並べて出す（深淵へ行けるようになってから）
            string shardText = shards > 0 || (chH != null && GameConfig.CanEnterAbyss(chH)) ? Loc.T($"   Abyss Shards {shards}", $"　深淵の欠片 {shards}") : "";
            GUI.Label(new Rect(pad, y, W - pad * 2f, lineH), Loc.T($"Embers {embers}", $"火種 {embers}") + shardText, ember);
            y += lineH + Screen.height * 0.008f;
        }

        static void Sfx_Equip(GameWorld world) => world.Sfx.Pickup();

        // ================= 倉庫番 =================

        void DrawStorage(GameWorld world, CharacterData ch)
        {
            float W = Screen.width, H = Screen.height;
            float pad = W * 0.05f;
            var small = hud.SmallStyle;
            var type = ClassInfo.Current;
            Header(Loc.T("STORAGE KEEPER", "倉庫番"), ch.Embers, W, pad, out float y);

            float lineH = small.fontSize * 1.4f;
            GUI.Label(new Rect(pad, y, W, lineH), Loc.T($"Bring into the next run: {ch.Stash.Count}/{Inventory.BagCapacity}  ({AdvancedClasses.NameOf(type)})", $"次の挑戦に持ち込む：{ch.Stash.Count}/{Inventory.BagCapacity}（{AdvancedClasses.NameOf(type)}）"), small);
            y += lineH;
            y = Grid(ch.Stash, Inventory.BagCapacity, y, pad, type, 6) + H * 0.012f;
            GUI.Label(new Rect(pad, y, W, lineH), Loc.T($"Storage (shared by all classes): {Profile.Storage.Count}/{Town.StorageCapacity}", $"倉庫（全職業で共有）：{Profile.Storage.Count}/{Town.StorageCapacity}"), small);
            y += lineH;
            y = Grid(Profile.Storage, Town.StorageCapacity, y, pad, type) + H * 0.01f;

            float btnH = H * 0.05f;
            if (selected == null || (!ch.Stash.Contains(selected) && !Profile.Storage.Contains(selected)))
            {
                selected = null;
                GUI.Label(new Rect(pad, y, W - pad * 2f, lineH), Loc.T("Tap an item to compare, equip, move or sell it.", "品をタップすると、比べたり、装備・移動・売却ができる。"), small);
                return;
            }

            // 選んだ品は、下から出る窓で詳しく見せる（今の装備と比べられるよう広く）。窓の外を押すと閉じる
            hud.Button(new Rect(0, 0, W, H), null, new Color(0f, 0f, 0f, 0.55f), hud.CenteredStyle, () => { selected = null; armed = null; });
            var panelR = new Rect(pad * 0.5f, H * 0.3f, W - pad, H * 0.7f - pad * 0.5f);
            Hud.Fill(panelR, new Color(0.04f, 0.04f, 0.07f, 0.97f));
            Hud.OrnateFrame(panelR, Hud.FrameGold, Mathf.Max(2f, Mathf.Round(H * 0.0012f)), false, true);
            hud.Button(panelR, null, new Color(0f, 0f, 0f, 0f), hud.CenteredStyle, () => { }); // 窓の中を押しても閉じない
            float inner = W * 0.04f;
            float cx = panelR.x + inner, cw = panelR.width - inner * 2f;
            y = panelR.y + inner;
            float by = panelR.yMax - inner - btnH;

            // 選んだ品の中身
            var name = new GUIStyle(hud.LabelStyle) { fontStyle = FontStyle.Bold };
            name.normal.textColor = RarityInfo.ColorOf(selected.Rarity);
            GUI.Label(new Rect(cx, y, cw, lineH * 1.2f), selected.Name, name);
            y += lineH * 1.2f;
            {
                // 装備のレベルと、今つけている同じ場所の品
                bool tooHigh = selected.ReqLevel > ch.Level;
                var lv = new GUIStyle(small);
                lv.normal.textColor = tooHigh ? new Color(1f, 0.5f, 0.45f) : new Color(0.85f, 0.85f, 0.9f);
                var cur = Town.OtherClassWeapon(selected, type) ? null : world.Inventory.Equipped[(int)BagScreen.BestSlot(world.Inventory, selected)];
                string curText = cur != null ? Loc.T($"   Now: {cur.Name} (Lv {cur.Level})", $"　装備中：{cur.Name}（Lv {cur.Level}）") : Loc.T("   Now: (empty)", "　装備中：なし");
                Hud.FitLabel(new Rect(cx, y, cw, lineH), Loc.T($"Lv {selected.Level}", $"Lv {selected.Level}") + (tooHigh ? Loc.T(" (level too low)", "（レベルが足りない）") : "") + curText, lv);
                y += lineH;
            }
            if (Town.OtherClassWeapon(selected, type))
            {
                var warn = new GUIStyle(small);
                warn.normal.textColor = new Color(1f, 0.6f, 0.5f);
                GUI.Label(new Rect(cx, y, cw, lineH), Loc.T($"Weapon for {ClassInfo.Get(ClassInfo.Weapon(selected.Weapon).Class).DisplayName}", $"{ClassInfo.Get(ClassInfo.Weapon(selected.Weapon).Class).DisplayName}の武器"), warn);
                y += lineH;
            }
            // 今の装備と比べる（鞄の画面と同じ：効果の差・能力の表・強くなるか）
            {
                float bottom = by - btnH - W * 0.03f - H * 0.01f; // 2段のボタンの上まで
                if (Town.OtherClassWeapon(selected, type))
                {
                    foreach (var line in selected.Describe())
                    {
                        if (y + lineH > bottom) break;
                        GUI.Label(new Rect(cx, y, cw, lineH), line, small);
                        y += lineH;
                    }
                }
                else hud.Bag.DrawTownCompare(world, selected, new Rect(cx, y, cw, bottom - y));
            }

            float gap = W * 0.03f;
            float half = (cw - gap) * 0.5f;
            float third = (cw - gap * 2f) / 3f;
            bool inStash = ch.Stash.Contains(selected);
            var item = selected;
            float by1 = by - btnH - gap * 0.5f; // 上の段：移す・装備・ロック  下の段：売却・破棄
            {
                // 装備する：今つけている同じ場所の品と入れ替える（外した品は、この品があった所へ）
                var er = new Rect(cx + third + gap, by1, third, btnH);
                string why = Town.CannotEquip(ch, item);
                if (why != null)
                {
                    Hud.Fill(er, dim);
                    Hud.FitLabel(er, why, hud.CenteredStyle);
                }
                else hud.Button(er, Loc.T("EQUIP", "装備"), gold, hud.CenteredStyle, () => { if (Town.EquipFromTown(ch, item, (int)BagScreen.BestSlot(world.Inventory, item))) { selected = null; world.SyncTownGear(); Sfx_Equip(world); } });
            }
            if (inStash)
            {
                bool room = Profile.Storage.Count < Town.StorageCapacity;
                hud.Button(new Rect(cx, by1, third, btnH), room ? Loc.T("TO STORAGE", "倉庫へ") : Loc.T("STORAGE FULL", "倉庫が満杯"), row, hud.CenteredStyle,
                    () => { if (Town.Store(ch, item)) selected = null; });
            }
            else
            {
                bool room = ch.Stash.Count < Inventory.BagCapacity;
                var r = new Rect(cx, by1, third, btnH);
                if (Town.OtherClassWeapon(item, type))
                {
                    Hud.Fill(r, dim);
                    GUI.Label(r, Loc.T("OTHER CLASS", "ほかの職業"), hud.CenteredStyle);
                }
                else
                {
                    hud.Button(r, room ? Loc.T("BRING IN RUN", "持ち込む") : Loc.T("BAG FULL", "持ち込みが満杯"), row, hud.CenteredStyle,
                        () => { if (Town.Take(ch, item)) selected = null; });
                }
            }
            hud.Button(new Rect(cx + (third + gap) * 2f, by1, third, btnH), item.Locked ? Loc.T("UNLOCK", "ロック解除") : Loc.T("LOCK", "ロック"),
                item.Locked ? gold : row, hud.CenteredStyle, () => { item.Locked = !item.Locked; Profile.Save(); });

            if (item.Locked)
            {
                var lr = new Rect(cx, by, cw, btnH);
                Hud.Fill(lr, dim);
                Hud.FitLabel(lr, Loc.T("Locked: unlock to sell or discard", "ロック中：外すと売却・破棄できる"), hud.CenteredStyle);
            }
            else
            {
                Confirm(new Rect(cx, by, half, btnH), "salvage", Loc.T($"SELL +{Town.SalvageValue(item)} embers", $"売却 火種+{Town.SalvageValue(item)}"),
                    () => { if (Town.Salvage(ch, item)) selected = null; });
                Confirm(new Rect(cx + half + gap, by, half, btnH), "discard", Loc.T("DISCARD", "破棄"),
                    () => { if (Town.Discard(ch, item)) selected = null; });
            }
        }

        /// <summary>品の一覧（8列）。描いた下端の y を返す。</summary>
        float Grid(List<Item> items, int capacity, float y, float pad, ClassType type, int cols = 8)
        {
            float W = Screen.width;
            float gap = W * 0.01f;
            float cell = (W - pad * 2f - gap * (cols - 1)) / cols;
            int shown = Mathf.Max(items.Count, cols);
            shown = Mathf.Min(capacity, ((shown + cols - 1) / cols) * cols);
            for (int i = 0; i < shown; i++)
            {
                var r = new Rect(pad + (i % cols) * (cell + gap), y + (i / cols) * (cell + gap), cell, cell);
                if (i >= items.Count)
                {
                    Hud.Fill(r, dim);
                    continue;
                }
                var item = items[i];
                var c = item.DisplayColor;
                var bg = item == selected ? new Color(1f, 1f, 1f, 0.45f) : new Color(c.r, c.g, c.b, 0.22f);
                hud.Button(r, null, bg, hud.CenteredStyle, () => { selected = item; armed = null; });
                float inset = r.width * 0.15f;
                var old = GUI.color;
                if (Town.OtherClassWeapon(item, type)) GUI.color = new Color(1f, 1f, 1f, 0.35f);
                Hud.DrawSprite(new Rect(r.x + inset, r.y + inset, r.width - inset * 2f, r.height - inset * 2f), PixelArt.ItemIcon(item));
                Hud.DrawAbyssFrame(r, item);
                GUI.color = old;
                Hud.DrawEnhance(r, item);
                Hud.DrawItemLevel(r, item);
            }
            int rows = (shown + cols - 1) / cols;
            return y + rows * (cell + gap);
        }

        // ================= 商人 =================

        void DrawMerchant(GameWorld world, CharacterData ch)
        {
            float W = Screen.width, H = Screen.height;
            float pad = W * 0.05f;
            var type = ClassInfo.Current;
            Header(Loc.T("MERCHANT", "商人"), ch.Embers, W, pad, out float y);
            float bh = H * 0.1f;
            float gap = H * 0.015f;
            int potion = Town.PotionPrice(ch);

            Offer(new Rect(pad, y, W - pad * 2f, bh), Loc.T($"HP Potion   ({ch.HpPotions}/{Town.MaxPotions})", $"回復薬   （{ch.HpPotions}/{Town.MaxPotions}）"),
                Loc.T($"Restores {Town.HpPotionHeal * 100f:0}% HP. Q key or the HP button.", $"HPを{Town.HpPotionHeal * 100f:0}%回復。Q キーか HP ボタン。"),
                potion, ch.HpPotions < Town.MaxPotions && ch.Embers >= potion, () => Town.BuyPotion(ch, true),
                10, () => Town.BuyPotions(ch, true, 10));
            y += bh + gap;
            Offer(new Rect(pad, y, W - pad * 2f, bh), Loc.T($"MP Potion   ({ch.MpPotions}/{Town.MaxPotions})", $"魔力の薬   （{ch.MpPotions}/{Town.MaxPotions}）"),
                Loc.T($"Restores {Town.MpPotionRestore * 100f:0}% MP. E key or the MP button.", $"MPを{Town.MpPotionRestore * 100f:0}%回復。E キーか MP ボタン。"),
                potion, ch.MpPotions < Town.MaxPotions && ch.Embers >= potion, () => Town.BuyPotion(ch, false),
                10, () => Town.BuyPotions(ch, false, 10));
            y += bh + gap;
            int gear = Town.GearPrice(ch);
            Offer(new Rect(pad, y, W - pad * 2f, bh), Loc.T("Gear Crate", "装備の箱"),
                Loc.T($"Random Rare or better gear, Lv {Mathf.Min(ch.Level, Progression.MaxLevel)}. Goes into the items you bring.", $"Lv{Mathf.Min(ch.Level, Progression.MaxLevel)}の「稀」以上の装備がランダムで1つ。持ち込む品に入る。"),
                gear, ch.Embers >= gear && Town.HasRoom(ch), () =>
                {
                    lastBought = Town.BuyGear(type, ch);
                    if (lastBought != null) hud.Toast(lastBought.Name, RarityInfo.ColorOf(lastBought.Rarity));
                });
            y += bh + gap * 2f;

            if (lastBought != null)
            {
                var small = hud.SmallStyle;
                float lineH = small.fontSize * 1.4f;
                var name = new GUIStyle(hud.LabelStyle) { fontStyle = FontStyle.Bold };
                name.normal.textColor = RarityInfo.ColorOf(lastBought.Rarity);
                GUI.Label(new Rect(pad, y, W - pad * 2f, lineH * 1.2f), Loc.T("Bought: ", "買った品：") + lastBought.Name, name);
                y += lineH * 1.2f;
                foreach (var line in lastBought.Describe())
                {
                    if (y + lineH > H - pad) break;
                    GUI.Label(new Rect(pad, y, W - pad * 2f, lineH), line, small);
                    y += lineH;
                }
            }
        }

        /// <summary>
        /// 売り物の行。bulk が 1 より大きければ、右のボタンを上下に分け、下を「まとめて買う」（買えるだけ）にする。
        /// </summary>
        void Offer(Rect r, string name, string desc, int price, bool can, System.Action buy, int bulk = 0, System.Action bulkBuy = null)
        {
            Hud.Fill(r, row);
            float inner = r.height * 0.1f;
            var nameStyle = new GUIStyle(hud.LabelStyle) { fontStyle = FontStyle.Bold };
            GUI.Label(new Rect(r.x + inner, r.y + inner, r.width * 0.7f, r.height * 0.4f), name, nameStyle);
            var descStyle = new GUIStyle(hud.SmallStyle) { wordWrap = true };
            descStyle.normal.textColor = new Color(0.8f, 0.8f, 0.85f);
            GUI.Label(new Rect(r.x + inner, r.y + r.height * 0.45f, r.width * 0.68f, r.height * 0.55f), desc, descStyle);
            var btn = new Rect(r.xMax - r.width * 0.28f, r.y + inner, r.width * 0.28f - inner, r.height - inner * 2f);
            if (bulk > 1 && bulkBuy != null)
            {
                float half = (btn.height - inner) * 0.5f;
                var one = new Rect(btn.x, btn.y, btn.width, half);
                var many = new Rect(btn.x, btn.y + half + inner, btn.width, half);
                if (can)
                {
                    hud.Button(one, Loc.T($"BUY  {price}", $"1個  {price}"), gold, hud.CenteredStyle, buy);
                    hud.Button(many, Loc.T($"BUY x{bulk}  {price * bulk}", $"{bulk}個  {price * bulk}"), gold, hud.CenteredStyle, bulkBuy);
                }
                else
                {
                    var s0 = new GUIStyle(hud.CenteredStyle);
                    s0.normal.textColor = new Color(0.55f, 0.55f, 0.55f);
                    Hud.Fill(one, dim);
                    Hud.Fill(many, dim);
                    GUI.Label(one, $"{price}", s0);
                    GUI.Label(many, $"x{bulk}", s0);
                }
                return;
            }
            if (can) hud.Button(btn, Loc.T($"BUY\n{price}", $"買う\n{price}"), gold, hud.CenteredStyle, buy);
            else
            {
                Hud.Fill(btn, dim);
                var s = new GUIStyle(hud.CenteredStyle);
                s.normal.textColor = new Color(0.55f, 0.55f, 0.55f);
                GUI.Label(btn, $"{price}", s);
            }
        }

        // ================= 女神像（転職・転生の儀） =================

        void DrawMaster(GameWorld world, CharacterData ch)
        {
            float W = Screen.width, H = Screen.height;
            float pad = W * 0.05f;
            var type = ClassInfo.Current;
            var small = hud.SmallStyle;
            float lineH = small.fontSize * 1.4f;
            // 捧げる確認（何度も聞く）の間は、ほかのボタンを出さない（押し間違えないように）
            if (offerStep > 0) { DrawOfferConfirm(world, ch); return; }
            Header(Loc.T("GODDESS STATUE", "女神像"), ch.Embers, W, pad, out float y);

            // 転職・転生・奉納のタブ。転生は最初から見せる（条件の Lv99 も見えるように）。奉納はクリア後だけ
            bool showRebirth = true;
            bool showOffer = ch.EndingSeen;
            if (masterTab == MasterTab.Rebirth && !showRebirth) masterTab = MasterTab.Class;
            if (masterTab == MasterTab.Offer && !showOffer) masterTab = MasterTab.Class;
            float bottom = H - pad;
            if (showRebirth || showOffer)
            {
                var tabs = new System.Collections.Generic.List<(MasterTab tab, string name, Color color)> { (MasterTab.Class, Loc.T("CLASS", "転職"), new Color(0.5f, 0.4f, 0.1f, 0.85f)) };
                if (showRebirth) tabs.Add((MasterTab.Rebirth, Loc.T("REBIRTH", "転生"), new Color(0.4f, 0.25f, 0.55f, 0.85f)));
                if (showOffer) tabs.Add((MasterTab.Offer, Loc.T("OFFERING", "奉納"), new Color(0.55f, 0.08f, 0.12f, 0.9f)));
                float gap = W * 0.02f, th = H * 0.05f;
                float tw = (W - pad * 2f - gap * (tabs.Count - 1)) / tabs.Count;
                for (int i = 0; i < tabs.Count; i++)
                {
                    var t = tabs[i];
                    bool on = masterTab == t.tab;
                    hud.Button(new Rect(pad + i * (tw + gap), y, tw, th), t.name, on ? t.color : new Color(1f, 1f, 1f, 0.07f), hud.CenteredStyle, () => masterTab = t.tab);
                    if (on) Hud.OrnateFrame(new Rect(pad + i * (tw + gap), y, tw, th), new Color(1f, 0.85f, 0.45f, 1f), 1f, false, false);
                }
                y += th + H * 0.015f;
            }
            if (masterTab == MasterTab.Rebirth)
            {
                DrawRebirthBar(world, ch, new Rect(pad, y, W - pad * 2f, H * 0.2f));
                return;
            }
            if (masterTab == MasterTab.Offer)
            {
                DrawOfferPage(world, ch, y);
                return;
            }

            if (ch.Special)
            {
                DrawSpecialCard(ch, type, y, bottom);
                return;
            }
            var current = AdvancedClasses.Of(type, ch);
            if (current != null)
            {
                DrawThird(world, ch, type, current, y, bottom);
                return;
            }
            var wrap = new GUIStyle(small) { wordWrap = true };
            y += GoddessSays(new Rect(pad, y, W - pad * 2f, H * 0.12f),
                Loc.T("You have come far. Choose a new path and you will walk it again from the very beginning. The road ahead is harder than any before.",
                    "よくぞここまで辿り着きました。新たな道を選べば、あなたは一から歩み直すことになります。その先は、これまで以上に険しい道です。")) + H * 0.015f;

            float cardH = Mathf.Min(H * 0.3f, (bottom - y - H * 0.015f) * 0.5f);
            for (int i = 0; i < 2; i++)
            {
                var adv = AdvancedClasses.Get(type, i);
                var r = new Rect(pad, y, W - pad * 2f, cardH);
                Hud.Fill(r, row);
                float inner = r.width * 0.04f;
                float ty = r.y + inner;
                var nameStyle = new GUIStyle(hud.LabelStyle) { fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(hud.LabelStyle.fontSize * 1.2f) };
                nameStyle.normal.textColor = new Color(1f, 0.85f, 0.45f);
                GUI.Label(new Rect(r.x + inner, ty, r.width, lineH * 1.4f), adv.DisplayName, nameStyle);
                ty += lineH * 1.5f;
                float dh = wrap.CalcHeight(new GUIContent(adv.DisplayDesc), r.width - inner * 2f);
                GUI.Label(new Rect(r.x + inner, ty, r.width - inner * 2f, dh), adv.DisplayDesc, wrap);
                ty += dh + lineH * 0.4f;

                var skill = adv.Skill;
                var icon = lineH * 2f;
                Hud.DrawSprite(new Rect(r.x + inner, ty, icon, icon), PixelArt.SkillIcon(skill));
                var sk = new GUIStyle(small) { fontStyle = FontStyle.Bold };
                GUI.Label(new Rect(r.x + inner + icon * 1.2f, ty, r.width, lineH), Loc.T($"Skill: {skill.DisplayName}   MP {skill.Mp}", $"スキル：{skill.DisplayName}   MP {skill.Mp}"), sk);
                GUI.Label(new Rect(r.x + inner + icon * 1.2f, ty + lineH, r.width - inner * 2f - icon * 1.2f, lineH), skill.DisplayDesc, small);

                float btnH = H * 0.05f;
                var btn = new Rect(r.x + inner, r.yMax - inner - btnH, r.width - inner * 2f, btnH);
                if (AdvancedClasses.CanChange(ch))
                {
                    int pick = i;
                    Confirm(btn, "adv" + i, Loc.T($"RECEIVE THE RITE  ({AdvancedClasses.Cost} embers)", $"転職の儀を受ける（火種 {AdvancedClasses.Cost}）"), () =>
                    {
                        if (Town.ChangeClass(ch, pick)) world.OnAdvanced();
                    });
                }
                else
                {
                    Hud.Fill(btn, dim);
                    string why = ch.Level < AdvancedClasses.ReqLevel ? Loc.T($"Need Lv {AdvancedClasses.ReqLevel}", $"Lv{AdvancedClasses.ReqLevel}が必要")
                        : Loc.T($"Need {AdvancedClasses.Cost} embers", $"火種 {AdvancedClasses.Cost} が必要");
                    var st = new GUIStyle(hud.CenteredStyle);
                    st.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
                    GUI.Label(btn, why, st);
                }
                y += cardH + H * 0.015f;
            }
        }

        /// <summary>上位職になったあと：3次職への転職の欄（Lv50 になるまでは明かさない。一度でも3次職になったら、ずっと見せる）。</summary>
        void DrawThird(GameWorld world, CharacterData ch, ClassType type, AdvancedSpec current, float y, float bottom)
        {
            float W = Screen.width, H = Screen.height;
            float pad = W * 0.05f;
            var small = hud.SmallStyle;
            float lineH = small.fontSize * 1.4f;
            var wrap = new GUIStyle(small) { wordWrap = true };
            var third = ThirdClasses.Get(type, ch.Advanced);

            if (!ch.Third && ch.Level < ThirdClasses.PromoteLevel)
            {
                var plain = new GUIStyle(hud.LabelStyle) { wordWrap = true };
                GoddessSays(new Rect(pad, y, W - pad * 2f, H * 0.12f),
                    Loc.T($"One who walks the path of the {current.DisplayName}... You are still halfway. Gain more strength, then come again.", $"{current.DisplayName}の道を歩む者よ。まだ道半ばです。さらなる力を得て、また来なさい。"));
                return;
            }
            var r = new Rect(pad, y, W - pad * 2f, Mathf.Min(H * 0.42f, bottom - y));
            Hud.Fill(r, ch.Third ? new Color(0.5f, 0.4f, 0.1f, 0.5f) : row);
            float inner = r.width * 0.04f;
            float ty = r.y + inner;
            float portrait = r.width * 0.22f;
            Hud.DrawSprite(new Rect(r.xMax - inner - portrait, ty, portrait, portrait), PixelArt.ThirdPreview(type, ch.Advanced));
            var nameStyle = new GUIStyle(hud.LabelStyle) { fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(hud.LabelStyle.fontSize * 1.3f) };
            nameStyle.normal.textColor = new Color(1f, 0.8f, 0.35f);
            GUI.Label(new Rect(r.x + inner, ty, r.width, lineH * 1.5f), Loc.T("3rd class: ", "3次職：") + third.DisplayName, nameStyle);
            ty += lineH * 1.6f;
            float textW = r.width - inner * 3f - portrait;
            float dh = wrap.CalcHeight(new GUIContent(third.DisplayDesc), textW);
            GUI.Label(new Rect(r.x + inner, ty, textW, dh), third.DisplayDesc, wrap);
            ty = Mathf.Max(ty + dh, r.y + inner + portrait) + lineH * 0.4f;

            var skill = third.Skill;
            float icon = lineH * 2f;
            Hud.DrawSprite(new Rect(r.x + inner, ty, icon, icon), PixelArt.SkillIcon(skill));
            var sk = new GUIStyle(small) { fontStyle = FontStyle.Bold };
            GUI.Label(new Rect(r.x + inner + icon * 1.2f, ty, r.width, lineH), Loc.T($"Skill: {skill.DisplayName}   MP {skill.Mp}", $"スキル：{skill.DisplayName}   MP {skill.Mp}"), sk);
            GUI.Label(new Rect(r.x + inner + icon * 1.2f, ty + lineH, r.width - inner * 2f - icon * 1.2f, lineH), skill.DisplayDesc, small);
            ty += icon + lineH * 0.5f;

            float btnH = H * 0.055f;
            var btn = new Rect(r.x + inner, r.yMax - inner - btnH, r.width - inner * 2f, btnH);
            if (ch.Third)
            {
                GUI.Label(btn, Loc.T("YOUR CLASS", "今の職業"), hud.CenteredStyle);
                return;
            }
            // 条件（満たしたものは緑）
            var ok = new Color(0.5f, 1f, 0.5f);
            var ng = new Color(1f, 0.55f, 0.5f);
            void Req(string text, bool met)
            {
                var st = new GUIStyle(small);
                st.normal.textColor = met ? ok : ng;
                GUI.Label(new Rect(r.x + inner, ty, r.width - inner * 2f, lineH), (met ? "OK  " : "--  ") + text, st);
                ty += lineH;
            }
            Req(Loc.T($"Lv {ThirdClasses.PromoteLevel}   (now Lv {ch.Level})", $"Lv{ThirdClasses.PromoteLevel}   （今 Lv{ch.Level}）"), ch.Level >= ThirdClasses.PromoteLevel);
            Req(Loc.T($"{ThirdClasses.PromoteCost} embers   (have {ch.Embers})", $"火種 {ThirdClasses.PromoteCost}   （持っている {ch.Embers}）"), ch.Embers >= ThirdClasses.PromoteCost);
            var note = new GUIStyle(small);
            note.normal.textColor = new Color(0.75f, 0.75f, 0.8f);
            note.fontStyle = FontStyle.Italic;
            note.normal.textColor = new Color(1f, 0.9f, 0.65f);
            Hud.FitLabel(new Rect(r.x + inner, ty, r.width - inner * 2f, lineH), Loc.T("\"Once more from the beginning. From here, the road grows steeper still.\"", "「再び一から。ここから先は、さらに険しい道となるでしょう」"), note);
            if (ThirdClasses.CanPromote(ch))
            {
                Confirm(btn, "third", Loc.T($"RITE: {third.DisplayName}  ({ThirdClasses.PromoteCost} embers)", $"{third.DisplayName}への儀を受ける（火種 {ThirdClasses.PromoteCost}）"), () =>
                {
                    if (Town.PromoteThird(ch)) world.OnThird();
                });
            }
            else
            {
                Hud.Fill(btn, dim);
                var st = new GUIStyle(hud.CenteredStyle);
                st.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
                GUI.Label(btn, Loc.T("Requirements not met", "条件を満たしていない"), st);
            }
        }

        // ================= 捧げる（クリア後） =================

        int offerStep; // 0：閉じている／1〜3：確認の段（くどいほど聞く）

        /// <summary>クリア後の女神像：血の涙を流す女神の言葉と、「捧げる」のボタン。</summary>
        enum MasterTab { Class, Rebirth, Offer }
        MasterTab masterTab; // 女神像の画面で選んでいるタブ（転職・転生・奉納）

        /// <summary>奉納のタブ：血の涙を流す女神の言葉と、「捧げる」のボタン（押すと確認が3回出る）。</summary>
        void DrawOfferPage(GameWorld world, CharacterData ch, float y)
        {
            float W = Screen.width, H = Screen.height;
            float pad = W * 0.05f;
            y += H * 0.01f;
            // 大きな女神像
            float big = W * 0.42f;
            Hud.DrawSprite(new Rect((W - big) * 0.5f, y, big, big), PixelArt.GoddessBleeding);
            y += big + H * 0.025f;
            string says = ch.Ascended
                ? Loc.T("...I have received it. The tower no longer leads down. Climb... climb up to me.",
                        "……確かに、受け取りました。塔はもう、下へは続きません。上へ……私のもとへ、昇っておいでなさい。")
                : Loc.T("...The light has returned. And yet, what flows from my eyes will not stop. The tower does not sleep. Offer me every ember you hold, and the strength you have grown. Then the tower will turn toward the sky.",
                        "……灯りは戻りました。それなのに、この目から流れるものが止まらないのです。塔は、まだ眠ってはいません。あなたの火種をすべて、そして積み上げてきた力を、私に捧げなさい。そうすれば、塔は空へと向きを変えるでしょう。");
            var st = new GUIStyle(hud.LabelStyle) { wordWrap = true, fontStyle = FontStyle.Italic, alignment = TextAnchor.UpperLeft };
            st.normal.textColor = new Color(1f, 0.82f, 0.75f);
            string line = Loc.Ja ? "「" + says + "」" : "\"" + says + "\"";
            float tw = W - pad * 2f;
            float th = st.CalcHeight(new GUIContent(line), tw);
            GUI.Label(new Rect(pad, y, tw, th), line, st);
            y += th + H * 0.03f;

            var btn = new Rect(pad, Mathf.Max(y, H - pad - H * 0.07f), W - pad * 2f, H * 0.06f);
            if (ch.Ascended)
            {
                var done = new GUIStyle(hud.CenteredStyle);
                done.normal.textColor = new Color(1f, 0.6f, 0.6f);
                Hud.FitLabel(btn, Loc.T("Already offered. The tower now reaches for the sky.", "すでに捧げた――塔は、空へと向かっている"), done);
                return;
            }
            if (Town.CanOffer(ch))
                hud.Button(btn, Loc.T($"Offer {ch.Embers} embers and the strength you built", $"火種 {ch.Embers} と、積み上げた力を捧げる"), new Color(0.55f, 0.08f, 0.12f, 0.9f), hud.CenteredStyle, () => offerStep = 1);
            else
            {
                Hud.Fill(btn, dim);
                var ng = new GUIStyle(hud.CenteredStyle);
                ng.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
                GUI.Label(btn, Loc.T("You hold no embers to offer", "捧げるべき火種が、手元にない"), ng);
            }
        }

        /// <summary>捧げる確認：3回聞く。どの段でも「やめる」で閉じる。最後に押したときだけ捧げる。</summary>
        void DrawOfferConfirm(GameWorld world, CharacterData ch)
        {
            float W = Screen.width, H = Screen.height;
            Hud.Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.82f));
            var r = new Rect(W * 0.07f, H * 0.24f, W * 0.86f, H * 0.48f); // 2段目の注意が長いので、少し高くする
            Hud.Fill(r, new Color(0.18f, 0.03f, 0.06f, 0.95f));
            Hud.OrnateFrame(r, new Color(0.8f, 0.2f, 0.25f, 1f), 1f, false, false);
            float inner = r.width * 0.05f;
            var head = new GUIStyle(hud.LabelStyle) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            head.normal.textColor = new Color(1f, 0.55f, 0.55f);
            Hud.FitLabel(new Rect(r.x, r.y + inner, r.width, H * 0.05f), Loc.T($"THE GODDESS ASKS  ({offerStep}/3)", $"女神の問い（{offerStep}/3）"), head);
            // 女神の問い（語り）と、その下に小さく、何が起きるか
            string text, note;
            switch (offerStep)
            {
                case 1:
                    text = Loc.T("Every ember you carry, down to the last. And the strength you have built, step by step, all this way... Even so, will you offer them to me?",
                                 "あなたの火種を、ひとつ残らず。そして、ここまで一歩ずつ積み上げてきた力も――。それでも、私に捧げますか？");
                    note = Loc.T($"(All {ch.Embers} embers / Lv {ch.Level} -> Lv 1. Class, rebirths, gear and skills remain. Gear you can no longer wear goes to storage.)",
                                 $"（火種 {ch.Embers} をすべて／Lv{ch.Level} → Lv1。職・転生・装備・技は残る。身に付けられなくなった装備は倉庫へ）");
                    break;
                case 2:
                    text = Loc.T("Once offered, the way to the Abyss will be sealed, never to open again. The tower will turn over, and you will climb toward the sky from the very first step... Are you truly certain?",
                                 "捧げれば、深淵への道は閉ざされ、二度と開くことはありません。塔はさかさまになり、あなたは最初の一段から、空へ向かって昇ることになるでしょう。……本当に、よいのですね？");
                    note = Loc.T("(The Abyss can never be entered again. You start from floor 1. Your tower records remain. A suspended run will be lost.)",
                                 "（深淵には二度と入れない／1層から登り直す／塔の記録は残る／中断中の挑戦は消える）");
                    break;
                default:
                    text = Loc.T("This is the last time I will ask. What is offered never returns.",
                                 "これが最後の問いです。一度捧げたものは、もう二度と戻りません。");
                    note = Loc.T("(This cannot be undone.)", "（取り消せません）");
                    break;
            }
            var wrap = new GUIStyle(hud.LabelStyle) { wordWrap = true, alignment = TextAnchor.UpperLeft };
            wrap.fontStyle = FontStyle.Italic;
            wrap.normal.textColor = new Color(1f, 0.82f, 0.75f);
            string spoken = Loc.Ja ? "「" + text + "」" : "\"" + text + "\"";
            float textY = r.y + inner + H * 0.065f;
            float textH = wrap.CalcHeight(new GUIContent(spoken), r.width - inner * 2f);
            GUI.Label(new Rect(r.x + inner, textY, r.width - inner * 2f, textH), spoken, wrap);
            var noteStyle = new GUIStyle(hud.SmallStyle) { wordWrap = true };
            noteStyle.normal.textColor = new Color(0.75f, 0.72f, 0.78f);
            GUI.Label(new Rect(r.x + inner, textY + textH + H * 0.012f, r.width - inner * 2f, r.height * 0.3f), note, noteStyle);
            float btnH = H * 0.055f, gap = r.width * 0.04f;
            float bw = (r.width - inner * 2f - gap) * 0.5f;
            float by = r.yMax - inner - btnH;
            // 連打で通り抜けないよう、2段目だけ「捧げる」と「やめる」の左右を入れ替える
            bool flip = offerStep == 2;
            float cx = flip ? r.x + inner + bw + gap : r.x + inner, ox = flip ? r.x + inner : r.x + inner + bw + gap;
            hud.Button(new Rect(cx, by, bw, btnH), Loc.T("NOT YET", "やめておく"), new Color(1f, 1f, 1f, 0.15f), hud.CenteredStyle, () => offerStep = 0);
            hud.Button(new Rect(ox, by, bw, btnH), offerStep < 3 ? Loc.T("I WILL", "捧げます") : Loc.T("OFFER EVERYTHING", "すべてを捧げる"),
                new Color(0.6f, 0.08f, 0.12f, 0.95f), hud.CenteredStyle, () =>
                {
                    if (offerStep < 3) { offerStep++; return; }
                    offerStep = 0;
                    if (Town.Offer(ch)) world.OnOffered();
                });
        }

        /// <summary>女神像の言葉（左に女神像の絵、右にかぎかっこの言葉）。使った高さを返す。</summary>
        float GoddessSays(Rect area, string text)
        {
            float icon = Mathf.Min(area.height, Screen.width * 0.13f);
            var gch = Profile.Current;
            Hud.DrawSprite(new Rect(area.x, area.y, icon, icon), gch != null && gch.EndingSeen ? PixelArt.GoddessBleeding : PixelArt.TownIcon(2));
            var st = new GUIStyle(hud.SmallStyle) { wordWrap = true, fontStyle = FontStyle.Italic };
            st.normal.textColor = new Color(1f, 0.9f, 0.65f);
            string line = Loc.Ja ? "「" + text + "」" : "\"" + text + "\"";
            float tw = area.width - icon * 1.15f;
            float h = st.CalcHeight(new GUIContent(line), tw);
            GUI.Label(new Rect(area.x + icon * 1.15f, area.y, tw, h), line, st);
            return Mathf.Max(icon, h);
        }

        /// <summary>特別な職「彩灯者」になっているとき：今の職業の欄（ここから先の転職はない）。</summary>
        void DrawSpecialCard(CharacterData ch, ClassType type, float y, float bottom)
        {
            float W = Screen.width, H = Screen.height;
            float pad = W * 0.05f;
            var small = hud.SmallStyle;
            float lineH = small.fontSize * 1.4f;
            var wrap = new GUIStyle(small) { wordWrap = true };
            var spec = SpecialClass.Spec;
            y += GoddessSays(new Rect(pad, y, W - pad * 2f, H * 0.12f),
                Loc.T("To reach this far without ever changing your path... No one else can bear that light.", "職を変えずにここまで至る者がいるとは……その光は、誰にも真似できません。")) + H * 0.015f;
            var r = new Rect(pad, y, W - pad * 2f, Mathf.Min(H * 0.36f, bottom - y));
            Hud.Fill(r, new Color(0.45f, 0.2f, 0.42f, 0.5f));
            float inner = r.width * 0.04f;
            float ty = r.y + inner;
            float portrait = r.width * 0.22f;
            Hud.DrawSprite(new Rect(r.xMax - inner - portrait, ty, portrait, portrait), PixelArt.PlayerFor(ch));
            var nameStyle = new GUIStyle(hud.LabelStyle) { fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(hud.LabelStyle.fontSize * 1.3f) };
            nameStyle.normal.textColor = Color.Lerp((Color)SpecialClass.AuraColor, Color.white, 0.3f);
            GUI.Label(new Rect(r.x + inner, ty, r.width, lineH * 1.5f), spec.DisplayName, nameStyle);
            ty += lineH * 1.6f;
            float textW = r.width - inner * 3f - portrait;
            float dh = wrap.CalcHeight(new GUIContent(spec.DisplayDesc), textW);
            GUI.Label(new Rect(r.x + inner, ty, textW, dh), spec.DisplayDesc, wrap);
            ty = Mathf.Max(ty + dh, r.y + inner + portrait) + lineH * 0.4f;
            var skill = spec.Skill;
            float icon = lineH * 2f;
            Hud.DrawSprite(new Rect(r.x + inner, ty, icon, icon), PixelArt.SkillIcon(skill));
            var sk = new GUIStyle(small) { fontStyle = FontStyle.Bold };
            GUI.Label(new Rect(r.x + inner + icon * 1.2f, ty, r.width, lineH), Loc.T($"Skill: {skill.DisplayName}   MP {skill.Mp}", $"スキル：{skill.DisplayName}   MP {skill.Mp}"), sk);
            GUI.Label(new Rect(r.x + inner + icon * 1.2f, ty + lineH, r.width - inner * 2f - icon * 1.2f, lineH), skill.DisplayDesc, small);
            var btn = new Rect(r.x + inner, r.yMax - inner - H * 0.055f, r.width - inner * 2f, H * 0.055f);
            GUI.Label(btn, Loc.T("YOUR CLASS", "今の職業"), hud.CenteredStyle);
        }

        /// <summary>転生の欄（どの職でも Lv99 から。基本職に戻り、全能力値+5。基本職のまま転生すると彩灯者になる）。</summary>
        void DrawRebirthBar(GameWorld world, CharacterData ch, Rect r)
        {
            var small = hud.SmallStyle;
            float lineH = small.fontSize * 1.4f;
            var wrap = new GUIStyle(small) { wordWrap = true };
            Hud.Fill(r, new Color(0.25f, 0.18f, 0.32f, 0.6f));
            Hud.OrnateFrame(r, ThirdClasses.AuraColor(Mathf.Max(1, ch.Rebirths + 1)), Mathf.Max(1f, Mathf.Round(Screen.height * 0.001f)), false, false);
            float inner = r.width * 0.04f;
            float ty = r.y + inner * 0.6f;
            var title = new GUIStyle(hud.LabelStyle) { fontStyle = FontStyle.Bold };
            title.normal.textColor = Color.Lerp(ThirdClasses.AuraColor(Mathf.Max(1, ch.Rebirths + 1)), Color.white, 0.4f);
            string head = ch.Rebirths > 0 ? Loc.T($"REBIRTH  (reborn {ch.Rebirths} time(s), all stats +{ThirdClasses.BonusStats(ch)})", $"転生（{ch.Rebirths}回、全能力値 +{ThirdClasses.BonusStats(ch)}）") : Loc.T("REBIRTH", "転生");
            Hud.FitLabel(new Rect(r.x + inner, ty, r.width - inner * 2f, lineH * 1.2f), head, title);
            ty += lineH * 1.2f;
            // 説明は女神像の言葉で（数値は出さない。条件はボタンに出る）
            string desc = SpecialClass.RebirthBecomesSpecial(ch)
                ? Loc.T("\"You have come all this way without ever changing your path... Be reborn as you are, and you shall become something special.\"",
                    "「職を変えずに、ここまで来たのですね……そのまま生まれ変われば、あなたは特別な何かになれるでしょう」")
                : Loc.T("\"Well done. Reborn, you begin anew, yet the strength you have forged remains within you. Try as many times as you wish.\"",
                    "「見事です。生まれ変われば再び一からですが、鍛えた力はその身に残ります。何度でも挑みなさい」");
            wrap.fontStyle = FontStyle.Italic;
            wrap.normal.textColor = new Color(1f, 0.9f, 0.65f);
            float btnH = Screen.height * 0.045f;
            float dh = Mathf.Min(wrap.CalcHeight(new GUIContent(desc), r.width - inner * 2f), r.yMax - inner * 0.6f - btnH - ty);
            GUI.Label(new Rect(r.x + inner, ty, r.width - inner * 2f, dh), desc, wrap);
            var btn = new Rect(r.x + inner, r.yMax - inner * 0.6f - btnH, r.width - inner * 2f, btnH);
            if (ThirdClasses.CanRebirth(ch))
            {
                Confirm(btn, "rebirth", Loc.T($"RITE OF REBIRTH #{ch.Rebirths + 1}  ({ThirdClasses.Cost(ch)} embers)", $"転生の儀を受ける・{ch.Rebirths + 1}回目（火種 {ThirdClasses.Cost(ch)}）"), () =>
                {
                    if (Town.Rebirth(ch)) world.OnRebirth();
                });
            }
            else
            {
                Hud.Fill(btn, dim);
                var st = new GUIStyle(hud.CenteredStyle);
                st.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
                string why = ch.Level < ThirdClasses.ReqLevel ? Loc.T($"Need Lv {ThirdClasses.ReqLevel}  (now Lv {ch.Level})", $"Lv{ThirdClasses.ReqLevel}が必要（今 Lv{ch.Level}）")
                    : Loc.T($"Need {ThirdClasses.Cost(ch)} embers", $"火種 {ThirdClasses.Cost(ch)} が必要");
                Hud.FitLabel(btn, why, st);
            }
        }

        // ================= 鍛冶屋 =================

        void DrawSmith(CharacterData ch)
        {
            float W = Screen.width, H = Screen.height;
            float pad = W * 0.05f;
            // 文字は一段大きく（ふつうの大きさ）、品のマスも大きくする
            var small = new GUIStyle(hud.LabelStyle);
            var type = ClassInfo.Current;
            float lineH = small.fontSize * 1.4f;
            if (matPicking && selected != null) { DrawMaterialPicker(ch, selected); return; } // 素材を選ぶ一覧を開いている間は、それだけを出す
            Header(Loc.T("BLACKSMITH", "鍛冶屋"), ch.Embers, W, pad, out float y);

            // 装備中の品（前回帰還したときの装備）と、次の挑戦に持ち込む品
            var equipped = new List<Item>();
            foreach (var it in ch.Loadout) if (it != null && !it.IsEmptyMarker) equipped.Add(it);
            GUI.Label(new Rect(pad, y, W, lineH), Loc.T("Equipped", "装備中"), small);
            y += lineH;
            y = Grid(equipped, 5, y, pad, type, 5) + H * 0.01f;
            GUI.Label(new Rect(pad, y, W, lineH), Loc.T($"Items you bring: {ch.Stash.Count}/{Inventory.BagCapacity}", $"持ち込む品：{ch.Stash.Count}/{Inventory.BagCapacity}"), small);
            y += lineH;
            y = Grid(ch.Stash, Inventory.BagCapacity, y, pad, type) + H * 0.01f;

            float btnH = H * 0.055f;
            float by = H - pad - btnH;
            if (selected == null || (!equipped.Contains(selected) && !ch.Stash.Contains(selected)))
            {
                selected = null;
                var wrap = new GUIStyle(small) { wordWrap = true };
                string help = Loc.T($"Tap an item. Enhancing (Relic/orange up to +10, Mythic/red and Abyssal up to +{Item.MaxEnhance}) raises its base value and effects by {Item.EnhanceStep * 100f:0}% per level. Reaching +10 and +15 also uses one brought-home item of the same slot and rarity. Abyssal gear also needs Abyss Shards (1 per level, 15 for +15). Rerolling (Mythic only) draws new effects at the item's level. Brought-home items can be reforged into another slot, or three of a rarity fused into one of the next.   Abyss Shards: {ch.AbyssShards}",
                    $"品をタップ。強化（灯宝＝橙は+10、神話＝赤と深淵の品は+{Item.MaxEnhance}まで）すると、1段ごとに基本の数値と効果の値が{Item.EnhanceStep * 100f:0}%上がる。+10・+15 にするときは、持ち帰った同じ部位・同じ珍しさの品も1つ使う。深淵の品は深淵の欠片も要る（1段ごとに1個、+15だけ15個）。再抽選（神話だけ）すると、その品のレベルで効果を引き直す。持ち帰った品は、別の部位に作り直したり、同じ珍しさ3つを1つ上の珍しさに合成できる。　深淵の欠片：{ch.AbyssShards}個");
                GUI.Label(new Rect(pad, y, W - pad * 2f, wrap.CalcHeight(new GUIContent(help), W - pad * 2f)), help, wrap);
                return;
            }

            var item = selected;
            if (lockFor != item)
            {
                lockFor = item;
                locked.Clear();
            }
            // 強化の紋章（右）と名前（左）
            float emblem = lineH * 2.4f;
            // 強化の段は名前の「+N」と、アイコンの枠の飾りで分かる（印は出さない）
            var nameRect = new Rect(pad, y, W - pad * 2f - emblem, lineH * 1.2f);
            var name = new GUIStyle(hud.LabelStyle) { fontStyle = FontStyle.Bold };
            name.normal.textColor = item.DisplayColor;
            Hud.FitLabel(nameRect, item.Name, name);
            y += lineH * 1.2f;
            var info = new GUIStyle(small);
            info.normal.textColor = new Color(1f, 0.85f, 0.5f);
            GUI.Label(new Rect(pad, y, W - pad * 2f, lineH),
                Loc.T($"Enhance +{item.Enhance}/{item.EnhanceCap}   (values x{item.EnhanceMult:0.00})" + (item.Abyss || item.Heaven ? $"   Shards {ch.AbyssShards}" : ""),
                      $"強化 +{item.Enhance}/{item.EnhanceCap}   （値 x{item.EnhanceMult:0.00}）" + (item.Abyss || item.Heaven ? $"   欠片 {ch.AbyssShards}個" : "")), info);
            y += lineH;

            // 持ち帰った品（装備していない品）は、部位の変換・合成ができる（その分、ボタンの段を1つ増やす）
            bool spareActions = Town.IsSpare(ch, item) && (Town.Convertible(item) || Town.Synthesizable(item));
            float extraRow = spareActions ? btnH + W * 0.03f * 0.5f : 0f;
            // 説明。効果の行には、再抽選のときに残す「ロック」の切り替えを付ける（最大3つ、全部はロックできない）
            bool smithable = Town.Smithable(item);
            var lines = item.Describe();
            int baseLines = lines.Count - item.Affixes.Count - item.Uniques.Count;
            int maxLocks = Mathf.Min(Town.MaxRerollLocks, item.Affixes.Count - 1);
            float lockW = lineH * 1.6f;
            for (int li = 0; li < lines.Count; li++)
            {
                int ai0 = li - baseLines;
                bool affixRow = smithable && ai0 >= 0 && ai0 < item.Affixes.Count;
                float rowLine = affixRow ? lineH * 1.35f : lineH; // 固定ボタンの行は少し高く
                if (y + rowLine > by - btnH - extraRow - H * 0.02f) break;
                int ai = li - baseLines;
                bool isAffix = smithable && ai >= 0 && ai < item.Affixes.Count;
                if (isAffix)
                {
                    int idx = ai;
                    bool on = locked.Contains(idx);
                    bool canLock = on || locked.Count < maxLocks;
                    var lr = new Rect(pad, y + lineH * 0.08f, lockW, lineH * 1.2f);
                    // 南京錠：閉じている＝固定（再抽選で残す）、開いている＝引き直す
                    if (canLock)
                        hud.Button(lr, null, on ? new Color(0.5f, 0.4f, 0.1f, 0.6f) : new Color(1f, 1f, 1f, 0.12f), hud.CenteredStyle, () =>
                        {
                            if (!locked.Remove(idx)) locked.Add(idx);
                        });
                    else
                    {
                        Hud.Fill(lr, dim);
                    }
                    var oldColor = GUI.color;
                    if (!canLock) GUI.color = new Color(1f, 1f, 1f, 0.3f);
                    float li2 = lr.height * 0.12f;
                    Hud.DrawSprite(new Rect(lr.x + li2, lr.y + li2, lr.width - li2 * 2f, lr.height - li2 * 2f), PixelArt.Padlock(on));
                    GUI.color = oldColor;
                    var st = new GUIStyle(small);
                    if (on) st.normal.textColor = new Color(1f, 0.85f, 0.45f);
                    var textRect = new Rect(pad + lockW + W * 0.03f, y + lineH * 0.18f, W - pad * 2f - lockW - W * 0.03f, lineH);
                    Hud.FitLabel(textRect, SlotText(item, idx, lines[li], textRect, st), st);
                }
                else
                {
                    Hud.FitLabel(new Rect(pad, y, W - pad * 2f, lineH), lines[li], small);
                }
                y += rowLine;
            }

            DrawSmithFx(nameRect);

            if (spareActions) DrawSpareActions(ch, item, new Rect(pad, by - btnH * 2f - W * 0.03f, W - pad * 2f, btnH));

            bool enhanceable = Town.Enhanceable(item);
            if (!enhanceable)
            {
                var r = new Rect(pad, by, W - pad * 2f, btnH);
                Hud.Fill(r, dim);
                Hud.FitLabel(r, Loc.T("Only Relic (orange) or better can be enhanced", "強化できるのは灯宝（橙）以上"), hud.CenteredStyle);
                return;
            }

            // 強化（すぐ実行。カンカンと火花）と再抽選（2回押し。ロックの数で値段が倍々）
            float gap = W * 0.03f;
            if (SlotsSpinning)
            {
                var wait = new Rect(pad, by - btnH - gap * 0.5f, W - pad * 2f, btnH * 2f + gap * 0.5f);
                Hud.Fill(wait, dim);
                Hud.FitLabel(wait, Loc.T("Rolling...", "抽選中……"), hud.CenteredStyle);
                return;
            }
            var enhanceRect = new Rect(pad, by - btnH - gap * 0.5f, W - pad * 2f, btnH);
            if (item.Enhance >= item.EnhanceCap)
            {
                Hud.Fill(enhanceRect, dim);
                GUI.Label(enhanceRect, Loc.T("MAX ENHANCED", "強化は最大"), hud.CenteredStyle);
            }
            else
            {
                int price = Town.EnhancePrice(item);
                int shards = Town.ShardPrice(item);
                bool needMat = Town.NeedsMaterial(item);
                var matList = needMat ? Town.MaterialsFor(ch, item) : null;
                int mats = needMat ? matList.Count : 0;
                if (needMat && mats > 0 && (matChoice == null || !matList.Contains(matChoice))) matChoice = matList[0]; // 選んでいなければ、いちばんレベルの低い品
                if (needMat && mats > 0)
                {
                    // 左に素材の品（押すと一覧から選べる）、右に強化のボタン
                    float mw = enhanceRect.height * 2.4f;
                    var matR = new Rect(enhanceRect.x, enhanceRect.y, mw, enhanceRect.height);
                    Hud.Fill(matR, new Color(0.04f, 0.04f, 0.07f, 0.9f));
                    hud.Button(matR, null, new Color(1f, 1f, 1f, 0.08f), hud.CenteredStyle, () => matPicking = true);
                    float ic = matR.height * 0.8f;
                    Hud.DrawSprite(new Rect(matR.x + matR.height * 0.1f, matR.y + matR.height * 0.1f, ic, ic), PixelArt.ItemIcon(matChoice));
                    var ms = new GUIStyle(hud.SmallStyle) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                    ms.normal.textColor = Color.white;
                    Hud.FitLabel(new Rect(matR.x + ic + matR.height * 0.15f, matR.y, mw - ic - matR.height * 0.2f, matR.height * 0.5f), Loc.T("Material", "素材"), ms);
                    ms.normal.textColor = new Color(1f, 0.85f, 0.5f);
                    Hud.FitLabel(new Rect(matR.x + ic + matR.height * 0.15f, matR.y + matR.height * 0.5f, mw - ic - matR.height * 0.2f, matR.height * 0.5f), Loc.T("change", "変える"), ms);
                    Hud.OrnateFrame(matR, Hud.FrameGold, Mathf.Max(1f, Mathf.Round(H * 0.0009f)), false, false);
                    enhanceRect = new Rect(matR.xMax + W * 0.02f, enhanceRect.y, enhanceRect.width - mw - W * 0.02f, enhanceRect.height);
                }
                bool can = ch.Embers >= price && ch.AbyssShards >= shards && (!needMat || mats > 0);
                string shardNote = shards > 0 ? Loc.T($", {shards} shard" + (shards > 1 ? "s" : ""), $"・欠片 {shards}") : "";
                string matNote = needMat ? Loc.T($", 1 same-slot {RarityInfo.LabelOf(item.Rarity)} (have {mats})", $"・同じ部位の{RarityInfo.LabelOf(item.Rarity)} 1（持っている {mats}）") : "";
                string label = Loc.T($"ENHANCE  +{item.Enhance} → +{item.Enhance + 1}   ({price} embers{shardNote}{matNote})", $"強化  +{item.Enhance} → +{item.Enhance + 1}   （火種 {price}{shardNote}{matNote}）");
                if (can) hud.Button(enhanceRect, label, gold, hud.CenteredStyle, () =>
                {
                    if (!Town.EnhanceItem(ch, item, matChoice)) return;
                    matChoice = null;
                    smithFxTime = Time.unscaledTime;
                    smithFxText = $"+{item.Enhance}";
                    hud.World.Sfx.Anvil();
                });
                else
                {
                    Hud.Fill(enhanceRect, dim);
                    Hud.FitLabel(enhanceRect, label, hud.CenteredStyle);
                }
            }
            if (!smithable)
            {
                var rr = new Rect(pad, by, W - pad * 2f, btnH);
                Hud.Fill(rr, dim);
                Hud.FitLabel(rr, Loc.T("Rerolling is for Mythic (red) only", "再抽選は神話（赤）だけ"), hud.CenteredStyle);
            }
            else if (item.Affixes.Count > 0)
            {
                int rp = Town.RerollPrice(item, locked.Count);
                string lockNote = locked.Count > 0 ? Loc.T($", {locked.Count} locked", $"・固定 {locked.Count}") : "";
                string label = Loc.T($"REROLL EFFECTS   ({rp} embers{lockNote})", $"効果を再抽選   （火種 {rp}{lockNote}）");
                var rr = new Rect(pad, by, W - pad * 2f, btnH);
                if (ch.Embers >= rp)
                    Confirm(rr, "reroll", label, () =>
                    {
                        var lockList = new List<int>(locked);
                        if (Town.RerollItem(ch, item, type, lockList)) StartSlots(item, lockList);
                    });
                else
                {
                    Hud.Fill(rr, dim);
                    Hud.FitLabel(rr, label, hud.CenteredStyle);
                }
            }
        }

        /// <summary>強化の素材を選ぶ一覧（鍛冶屋の上に重ねる）。押した品を素材にする。</summary>
        void DrawMaterialPicker(CharacterData ch, Item target)
        {
            if (!matPicking) return;
            float W = Screen.width, H = Screen.height;
            var list = Town.MaterialsFor(ch, target);
            Hud.Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.75f));
            var panel = new Rect(W * 0.06f, H * 0.18f, W * 0.88f, H * 0.62f);
            Hud.Fill(panel, new Color(0.05f, 0.05f, 0.08f, 0.97f));
            Hud.OrnateFrame(panel, Hud.FrameGold, Mathf.Max(1f, Mathf.Round(H * 0.0012f)));
            var title = new GUIStyle(hud.LabelStyle) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            Hud.FitLabel(new Rect(panel.x, panel.y + H * 0.01f, panel.width, H * 0.045f), Loc.T("Choose the material", "素材にする品を選ぶ"), title);
            float rowH = H * 0.07f, gap = H * 0.008f;
            float y = panel.y + H * 0.065f;
            var name = new GUIStyle(hud.SmallStyle) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };
            var sub = new GUIStyle(hud.SmallStyle) { alignment = TextAnchor.MiddleLeft };
            sub.normal.textColor = new Color(0.8f, 0.8f, 0.85f);
            int max = Mathf.Max(1, Mathf.FloorToInt((panel.yMax - y - H * 0.08f) / (rowH + gap)));
            for (int i = 0; i < list.Count && i < max; i++)
            {
                var it = list[i];
                var r = new Rect(panel.x + W * 0.03f, y, panel.width - W * 0.06f, rowH);
                bool chosen = it == matChoice;
                Hud.Fill(r, chosen ? new Color(0.45f, 0.35f, 0.1f, 0.9f) : new Color(1f, 1f, 1f, 0.06f));
                var pick = it;
                hud.Button(r, null, new Color(1f, 1f, 1f, 0.05f), hud.CenteredStyle, () => { matChoice = pick; matPicking = false; });
                float ic = rowH * 0.84f;
                Hud.DrawSprite(new Rect(r.x + rowH * 0.08f, r.y + rowH * 0.08f, ic, ic), PixelArt.ItemIcon(it));
                name.normal.textColor = RarityInfo.ColorOf(it.Rarity);
                float tx = r.x + ic + rowH * 0.25f;
                Hud.FitLabel(new Rect(tx, r.y, r.xMax - tx - W * 0.02f, rowH * 0.55f), it.Name + (it.Enhance > 0 ? $" +{it.Enhance}" : ""), name);
                Hud.FitLabel(new Rect(tx, r.y + rowH * 0.5f, r.xMax - tx - W * 0.02f, rowH * 0.45f), $"Lv {it.Level}   " + (Town.InStorage(it) ? Loc.T("in storage", "倉庫") : Loc.T("carry-in", "持ち込む品")), sub);
                y += rowH + gap;
            }
            if (list.Count > max)
                Hud.FitLabel(new Rect(panel.x, y, panel.width, H * 0.03f), Loc.T($"(+{list.Count - max} more, lowest level first)", $"（ほか {list.Count - max} 個。レベルの低い順）"), sub);
            var back = new Rect(panel.x + panel.width * 0.25f, panel.yMax - H * 0.065f, panel.width * 0.5f, H * 0.05f);
            hud.Button(back, Loc.T("BACK", "戻る"), row, hud.CenteredStyle, () => matPicking = false);
        }

        /// <summary>持ち帰った品の「部位を変える」「合成」のボタン（左右に半分ずつ）。</summary>
        void DrawSpareActions(CharacterData ch, Item item, Rect row)
        {
            float gap = Screen.width * 0.03f;
            float half = (row.width - gap) * 0.5f;
            var left = new Rect(row.x, row.y, half, row.height);
            var right = new Rect(row.x + half + gap, row.y, half, row.height);
            // 部位の変換：武器→ランタン→鎧→指輪→武器 の順に、次の部位へ作り直す（中身は引き直し）
            if (Town.Convertible(item))
            {
                int cp = Town.ConvertPrice(item);
                string label = Loc.T($"REFORGE SLOT  ({cp} embers)", $"部位を変える（火種 {cp}）");
                if (ch.Embers >= cp) Confirm(left, "convert", label, () =>
                {
                    var made = Town.Convert(ch, item);
                    if (made == null) return;
                    selected = made;
                    smithFxTime = Time.unscaledTime;
                    smithFxText = SlotNameOf(made.Slot);
                    hud.World.Sfx.Anvil();
                });
                else { Hud.Fill(left, dim); Hud.FitLabel(left, label, hud.CenteredStyle); }
            }
            else { Hud.Fill(left, dim); Hud.FitLabel(left, Loc.T("Reforge: orange or better", "部位の変換は橙から"), hud.CenteredStyle); }
            // 合成：同じ珍しさ3つ（部位は問わない）で、1つ上の珍しさに
            if (Town.Synthesizable(item))
            {
                int sp = Town.SynthPrice(item);
                int have = Town.SynthPartners(ch, item).Count;
                string label = Loc.T($"FUSE 3 → {RarityInfo.LabelOf(item.Rarity + 1)}  ({sp} embers, {have}/2)", $"3つで合成→{RarityInfo.LabelOf(item.Rarity + 1)}（火種 {sp}・相手 {have}/2）");
                if (ch.Embers >= sp && have >= 2) Confirm(right, "synth", label, () =>
                {
                    var made = Town.Synthesize(ch, item);
                    if (made == null) return;
                    selected = made;
                    smithFxTime = Time.unscaledTime;
                    smithFxText = RarityInfo.LabelOf(made.Rarity);
                    hud.World.Sfx.Anvil();
                });
                else { Hud.Fill(right, dim); Hud.FitLabel(right, label, hud.CenteredStyle); }
            }
            else { Hud.Fill(right, dim); Hud.FitLabel(right, Loc.T("Fusing: purple or orange", "合成は紫・橙"), hud.CenteredStyle); }
        }

        static string SlotNameOf(ItemSlot s)
        {
            switch (s)
            {
                case ItemSlot.Weapon: return Loc.T("Weapon", "武器");
                case ItemSlot.Lantern: return Loc.T("Lantern", "ランタン");
                case ItemSlot.Armor: return Loc.T("Armor", "鎧");
                default: return Loc.T("Ring", "指輪");
            }
        }

        // ---- 再抽選のスロット演出：固定していない効果の行が回り、上から順に止まる ----
        Item slotItem;
        readonly List<int> slotLines = new List<int>(); // 回す効果の番号（上から）
        float slotStart = -1f;
        int slotStopped;                               // 止まった数（音を鳴らすため）
        readonly Dictionary<int, (int frame, string text)> slotShown = new Dictionary<int, (int frame, string text)>();
        const float SlotFirstStop = 0.8f, SlotStopGap = 0.45f;

        bool SlotsSpinning => slotStart >= 0f && Time.unscaledTime - slotStart < SlotFirstStop + SlotStopGap * Mathf.Max(0, slotLines.Count - 1) + 0.05f;

        void StartSlots(Item item, List<int> lockedIdx)
        {
            slotItem = item;
            slotLines.Clear();
            for (int i = 0; i < item.Affixes.Count; i++) if (!lockedIdx.Contains(i)) slotLines.Add(i);
            slotStart = Time.unscaledTime;
            slotStopped = 0;
        }

        /// <summary>効果の行の文字。回っている行はランダムな効果をめまぐるしく出し、止まったら本当の効果を光らせて出す。</summary>
        string SlotText(Item item, int affixIndex, string real, Rect rect, GUIStyle st)
        {
            if (slotStart < 0f || item != slotItem) return real;
            int order = slotLines.IndexOf(affixIndex);
            if (order < 0) return real;
            float elapsed = Time.unscaledTime - slotStart;
            float stopAt = SlotFirstStop + SlotStopGap * order;
            if (elapsed < stopAt)
            {
                // 回っている：枠を光らせ、ランダムな効果を出す（止まる直前はゆっくり）
                Hud.Fill(new Rect(rect.x - 2f, rect.y, rect.width, rect.height), new Color(1f, 0.85f, 0.4f, 0.12f));
                float speed = Mathf.Lerp(0.04f, 0.14f, Mathf.Clamp01((elapsed - (stopAt - 0.4f)) / 0.4f));
                int frame = Mathf.FloorToInt(elapsed / speed);
                // 同じ瞬間（OnGUI は1フレームに何度も呼ばれる）は同じ文字を出す。乱数の種は変えない（ドロップに影響させない）
                if (!slotShown.TryGetValue(affixIndex, out var shown) || shown.frame != frame)
                {
                    shown = (frame, ItemGenerator.RandomAffixText(item));
                    slotShown[affixIndex] = shown;
                    hud.World.Sfx.Tick();
                }
                st.normal.textColor = new Color(0.85f, 0.85f, 0.9f);
                return shown.text;
            }
            // 止まった：その瞬間に音と光
            if (order >= slotStopped)
            {
                slotStopped = order + 1;
                hud.World.Sfx.SlotStop();
            }
            float since = elapsed - stopAt;
            if (since < 0.5f)
            {
                Hud.Fill(new Rect(rect.x - 2f, rect.y, rect.width, rect.height), new Color(1f, 0.95f, 0.6f, 0.45f * (1f - since / 0.5f)));
                st.normal.textColor = new Color(1f, 0.95f, 0.6f);
            }
            if (order == slotLines.Count - 1 && since > 0.6f) slotStart = -1f; // 全部止まって少ししたら終わり
            return real;
        }

        // 再抽選で残す効果（選んだ品ごと。品を変えると外れる）
        readonly HashSet<int> locked = new HashSet<int>();
        Item lockFor;

        // 強化の演出（品の名前のあたりで火花が散り、白く光る）。smithFxTime は始めた時刻（-1 で出していない）
        float smithFxTime = -1f;
        string smithFxText;
        const float SmithFxLife = 0.9f;

        void DrawSmithFx(Rect around)
        {
            if (smithFxTime < 0f) return;
            float elapsed = Time.unscaledTime - smithFxTime; // OnGUI は1フレームに何度も呼ばれるので、始めた時刻からの経過で動かす
            if (elapsed > SmithFxLife)
            {
                smithFxTime = -1f;
                return;
            }
            float t = elapsed / SmithFxLife;
            // 白い光（すぐ消える）
            Hud.Fill(new Rect(around.x - around.width * 0.02f, around.y - around.height * 0.3f, around.width * 1.04f, around.height * 1.6f), new Color(1f, 0.95f, 0.8f, 0.5f * (1f - t) * (1f - t)));
            // 火花：中心から放射状に飛ぶ（2回目の「カン」に合わせてもう一度）
            Vector2 c = new Vector2(around.x + around.width * 0.3f, around.y + around.height * 0.5f);
            float size = Mathf.Max(3f, around.height * 0.18f);
            for (int burst = 0; burst < 2; burst++)
            {
                float bt = (elapsed - burst * 0.17f) / 0.55f;
                if (bt < 0f || bt > 1f) continue;
                for (int k = 0; k < 12; k++)
                {
                    float ang = (k * 30f + burst * 15f) * Mathf.Deg2Rad;
                    float dist = around.height * (0.3f + 2.2f * bt) * (0.7f + 0.3f * ((k * 7) % 5) / 4f);
                    var p = c + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * 0.6f) * dist + new Vector2(0f, around.height * 1.2f * bt * bt); // 少し落ちる
                    var col = k % 3 == 0 ? new Color(1f, 0.95f, 0.6f, 1f - bt) : new Color(1f, 0.6f, 0.2f, 1f - bt);
                    Hud.Fill(new Rect(p.x - size * 0.5f, p.y - size * 0.5f, size, size), col);
                }
            }
            // 「+5」などの文字が跳ねる
            var pop = new GUIStyle(hud.LabelStyle) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight };
            pop.fontSize = Mathf.RoundToInt(hud.LabelStyle.fontSize * (1.3f + 0.4f * Mathf.Clamp01(1f - t * 4f)));
            pop.normal.textColor = new Color(1f, 0.85f, 0.35f, 1f - t * t);
            GUI.Label(new Rect(around.x, around.y - around.height * 0.6f * t, around.width, around.height), smithFxText, pop);
        }

        // ================= 荷運び =================

        void DrawPorter(CharacterData ch)
        {
            float W = Screen.width, H = Screen.height;
            float pad = W * 0.05f;
            Header(Loc.T("PORTER", "荷運び"), ch.Embers, W, pad, out float y);
            var wrap = new GUIStyle(hud.SmallStyle) { wordWrap = true };
            string text = Loc.T("When you return home, you can bring back this many items found in that run (equipped gear and carried-in items are always kept).",
                "帰還するとき、その挑戦で拾った品を、この数まで持ち帰れる（装備中の品と持ち込んだ品はいつも持ち帰れる）。");
            float th = wrap.CalcHeight(new GUIContent(text), W - pad * 2f);
            GUI.Label(new Rect(pad, y, W - pad * 2f, th), text, wrap);
            y += th + H * 0.02f;

            var big = new GUIStyle(hud.CenteredStyle) { fontSize = hud.LabelStyle.fontSize * 2, fontStyle = FontStyle.Bold };
            GUI.Label(new Rect(0, y, W, H * 0.06f), $"{ch.CarrySlots} / {Town.MaxCarrySlots}", big);
            y += H * 0.08f;

            if (ch.CarrySlots >= Town.MaxCarrySlots)
            {
                GUI.Label(new Rect(0, y, W, H * 0.04f), Loc.T("Maxed out.", "これ以上は増やせない。"), hud.CenteredStyle);
                return;
            }
            int price = Town.CarryPrice(ch);
            Offer(new Rect(pad, y, W - pad * 2f, H * 0.1f), Loc.T("+1 carry slot", "持ち帰り枠 +1"),
                Loc.T("For this class.", "この職業だけ。"), price, ch.Embers >= price, () => Town.BuyCarrySlot(ch));
        }

        /// <summary>2回押しで実行するボタン（押してから3秒だけ有効）。</summary>
        void Confirm(Rect r, string id, string text, System.Action action)
        {
            bool isArmed = armed == id && Time.unscaledTime < armedUntil;
            hud.Button(r, isArmed ? Loc.T("TAP AGAIN", "もう一度押す") : text, isArmed ? new Color(0.7f, 0.15f, 0.15f, 0.8f) : gold, hud.CenteredStyle, () =>
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
