using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 町（メニュー型）。職業を選んだあとに来る。出撃・倉庫番・商人・転職の師匠・荷運び・職業を変える。
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
            bool abyss = Profile.AbyssUnlocked; // 深淵へ行けるようになったら、町も深淵版の絵
            var tex = abyss ? TownScene.AbyssTexture : TownScene.Texture;
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
                Hud.Glow(bg.x + l.x * ps, bg.y + l.y * ps, ps * 16f, abyss ? new Color(1f, 0.3f, 0.5f, 0.35f * f) : new Color(1f, 0.75f, 0.4f, 0.35f * f));
            }
            float g = 0.75f + 0.25f * Mathf.PerlinNoise(t * 1.5f, 0.5f);
            Hud.Glow(bg.x + TownScene.GateX * ps, bg.y + (TownScene.GateY - 14) * ps, ps * 30f, abyss ? new Color(1f, 0.2f, 0.45f, 0.2f * g) : new Color(1f, 0.7f, 0.35f, 0.18f * g)); // 門の光は控えめに
            // 上の帯（題・名前・火種）と、タイルの並ぶあたりを少し暗く
            for (int b = 0; b < 10; b++)
                Hud.Fill(new Rect(0, H * 0.02f * b, W, H * 0.02f + 1f), new Color(0f, 0f, 0.02f, 0.06f * (10 - b)));
        }

        // ================= 町の入口 =================

        void DrawMain(GameWorld world, CharacterData ch)
        {
            float W = Screen.width, H = Screen.height;
            float pad = W * 0.07f;
            var centered = hud.CenteredStyle;
            var small = hud.SmallStyle;
            var type = ClassInfo.Current;

            var title = new GUIStyle(centered) { fontSize = hud.LabelStyle.fontSize * 2, fontStyle = FontStyle.Bold };
            title.normal.textColor = new Color(1f, 0.85f, 0.45f);
            Hud.FitLabel(new Rect(0, H * 0.05f, W, H * 0.06f), Loc.T("TOWN", "町"), title);
            // 右上に設定（攻撃・音楽・BGM・FPS・言語・クレジット）
            {
                float gearS = H * 0.045f;
                var gearR = new Rect(W - pad - gearS, H * 0.06f, gearS, gearS);
                Hud.Fill(gearR, new Color(0.04f, 0.04f, 0.07f, 0.92f));
                hud.Button(gearR, null, new Color(1f, 1f, 1f, 0.08f), hud.CenteredStyle, world.OpenSettings);
                Hud.OrnateFrame(gearR, Hud.FrameGold, Mathf.Max(1f, Mathf.Round(H * 0.0009f)), false, false);
                float ip = gearS * 0.14f;
                Hud.DrawSprite(new Rect(gearR.x + ip, gearR.y + ip, gearS - ip * 2f, gearS - ip * 2f), PixelArt.GearIcon); // 歯車：押すと設定
            }
            GUI.Label(new Rect(0, H * 0.11f, W, H * 0.035f), $"{ClassTitle(type, ch)}   Lv {ch.Level}" + (ch.Rebirths > 0 ? Loc.T($"   Reborn x{ch.Rebirths}", $"   転生 {ch.Rebirths}回") : ""), centered);
            var info = new GUIStyle(centered) { fontSize = small.fontSize };
            info.normal.textColor = new Color(1f, 0.85f, 0.5f);
            GUI.Label(new Rect(0, H * 0.145f, W, H * 0.03f),
                Loc.T($"Embers {ch.Embers}   HP potions {ch.HpPotions}   MP potions {ch.MpPotions}", $"火種 {ch.Embers}   回復薬 {ch.HpPotions}   魔力の薬 {ch.MpPotions}"), info);

            float y = H * 0.19f;
            float bh = H * 0.075f;
            float gap = H * 0.012f;
            var bw = W - pad * 2f;

            hud.Button(new Rect(pad, y, bw, bh), (GameConfig.CanEnterAbyss(ch) ? Loc.T("DEPART  -  tower / abyss", "出撃  -  塔・深淵へ") : Loc.T("DEPART  -  into the tower", "出撃  -  塔へ")), new Color(0.2f, 0.5f, 0.25f, 0.85f), centered, world.Depart);
            y += bh + gap * 1.5f;

            // 施設はアイコンのタイル（3列×2段）。6つ目は「職業を変える」
            var adv = AdvancedClasses.Of(type, ch);
            bool masterNotice = (adv == null && ch.Level >= AdvancedClasses.ReqLevel) || ThirdClasses.MeetsRequirements(ch);
            string masterLine = adv != null && ThirdClasses.MeetsRequirements(ch) ? Loc.T("Rebirth ready!", "転生できる！")
                : ch.Third ? Loc.T($"Reborn x{ch.Rebirths}", $"転生 {ch.Rebirths}回")
                : adv != null ? adv.DisplayName
                : ch.Level >= AdvancedClasses.ReqLevel ? Loc.T("Class change!", "転職できる！")
                : Loc.T($"From Lv {AdvancedClasses.ReqLevel}", $"Lv{AdvancedClasses.ReqLevel}から");
            const int cols = 3;
            float tgap = W * 0.03f;
            float tw = (bw - tgap * (cols - 1)) / cols;
            float th = Mathf.Min(tw * 1.1f, H * 0.17f);
            void Tile(int i, Sprite icon, string name, string line, System.Action onTap, bool notice = false)
            {
                var r = new Rect(pad + (i % cols) * (tw + tgap), y + (i / cols) * (th + tgap), tw, th);
                Hud.Fill(r, new Color(0.02f, 0.02f, 0.05f, 0.62f)); // 町の絵の上でも読めるように、暗い下地
                hud.Button(r, null, notice ? gold : row, centered, onTap);
                var edge = new Color(1f, 0.8f, 0.45f, 0.35f); // 灯りの色の細い縁
                Hud.Fill(new Rect(r.x, r.y, r.width, 1f), edge);
                Hud.Fill(new Rect(r.x, r.yMax - 1f, r.width, 1f), edge);
                Hud.Fill(new Rect(r.x, r.y, 1f, r.height), edge);
                Hud.Fill(new Rect(r.xMax - 1f, r.y, 1f, r.height), edge);
                float size = Mathf.Min(r.width * 0.5f, r.height * 0.48f);
                Hud.DrawSprite(new Rect(r.x + (r.width - size) * 0.5f, r.y + r.height * 0.08f, size, size), icon);
                var nameStyle = new GUIStyle(hud.LabelStyle) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                Hud.FitLabel(new Rect(r.x, r.y + r.height * 0.6f, r.width, r.height * 0.2f), name, nameStyle);
                var lineStyle = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter };
                lineStyle.normal.textColor = new Color(0.8f, 0.8f, 0.85f);
                Hud.FitLabel(new Rect(r.x, r.y + r.height * 0.79f, r.width, r.height * 0.17f), line, lineStyle);
            }
            Tile(0, PixelArt.TownIcon(0), Loc.T("Storage", "倉庫番"), $"{Profile.Storage.Count}/{Town.StorageCapacity}", () => Open(Panel.Storage));
            Tile(1, PixelArt.TownIcon(1), Loc.T("Merchant", "商人"), Loc.T("Potions / gear", "薬・装備"), () => Open(Panel.Merchant));
            // 師匠は、転職できるようになるまで「？？？」（転職・転生は条件を満たしたら明かす）
            if (adv == null && ch.Level < AdvancedClasses.ReqLevel)
            {
                var r2 = new Rect(pad + (2 % cols) * (tw + tgap), y, tw, th);
                Hud.Fill(r2, new Color(0.02f, 0.02f, 0.05f, 0.62f));
                Hud.Fill(r2, dim);
                var q = new GUIStyle(hud.LabelStyle) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                q.normal.textColor = new Color(0.55f, 0.55f, 0.6f);
                GUI.Label(r2, "？？？", q);
            }
            else Tile(2, PixelArt.TownIcon(2), Loc.T("Master", "師匠"), masterLine, () => Open(Panel.Master), masterNotice);
            Tile(3, PixelArt.TownIcon(3), Loc.T("Porter", "荷運び"), Loc.T($"Carry {ch.CarrySlots}/{Town.MaxCarrySlots}", $"持ち帰り {ch.CarrySlots}/{Town.MaxCarrySlots}"), () => Open(Panel.Porter));
            Tile(4, PixelArt.TownIcon(4), Loc.T("Blacksmith", "鍛冶屋"), Loc.T("Mythic only", "神話（赤）だけ"), () => Open(Panel.Smith));
            Tile(5, PixelArt.PlayerFor(ch), Loc.T("Characters", "キャラ選択"), Loc.T("Switch", "切り替え"), world.ShowClassSelect);

            // 装備・能力値・スキル（町でも付け替えたり、振ったり、ボタンに付けたりできる）
            y += 2f * (th + tgap);
            float third = (bw - tgap * 2f) / 3f;
            float sbh = H * 0.05f;
            bool points = ch.Points > 0;
            for (int k = 0; k < 3; k++) Hud.Fill(new Rect(pad + (third + tgap) * k, y, third, sbh), new Color(0.02f, 0.02f, 0.05f, 0.62f)); // 町の絵の上の暗い下地
            hud.Button(new Rect(pad, y, third, sbh), Loc.T("GEAR", "装備"), row, centered, world.OpenTownBag);
            hud.Button(new Rect(pad + third + tgap, y, third, sbh), points ? Loc.T($"STATS +{ch.Points}", $"能力値 +{ch.Points}") : Loc.T("STATS", "能力値"),
                points ? gold : row, centered, () => world.OpenTownPanel(true));
            hud.Button(new Rect(pad + (third + tgap) * 2f, y, third, sbh), Loc.T("SKILLS", "スキル"), row, centered, () => world.OpenTownPanel(false));

            // 案内役（左下）
            float gs = W * 0.2f;
            var gr = new Rect(W * 0.04f, H - gs - H * 0.02f, gs, gs);
            Hud.DrawSprite(gr, PixelArt.Guide);
            var bubble = new Rect(gr.xMax + W * 0.02f, gr.y + gs * 0.1f, W - gr.xMax - W * 0.06f, gs * 0.75f);
            Hud.Fill(bubble, new Color(1f, 1f, 1f, 0.9f));
            var say = new GUIStyle(small) { wordWrap = true, alignment = TextAnchor.MiddleLeft };
            say.normal.textColor = new Color(0.1f, 0.08f, 0.12f);
            GUI.Label(new Rect(bubble.x + W * 0.02f, bubble.y, bubble.width - W * 0.04f, bubble.height), Greeting(ch), say);
        }

        static string ClassTitle(ClassType type, CharacterData ch)
        {
            var spec = ClassInfo.Get(type);
            var adv = AdvancedClasses.Of(type, ch);
            return adv != null ? adv.DisplayName : spec.DisplayName;
        }

        static string Greeting(CharacterData ch)
        {
            if (ch.Advanced < 0 && ch.Level >= AdvancedClasses.ReqLevel)
                return Loc.T("You have grown strong. Go see the master.", "強くなったね。師匠のところへ行ってみな。");
            if (ch.HpPotions == 0)
                return Loc.T("No potions? The merchant has some.", "薬がないのかい？ 商人が売ってるよ。");
            return Loc.T("Ready? The tower is waiting.", "準備はいいかい？ 塔が待ってる。");
        }

        void Open(Panel p)
        {
            panel = p;
            selected = null;
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
            GUI.Label(new Rect(pad, y, W - pad * 2f, lineH), Loc.T($"Embers {embers}", $"火種 {embers}"), ember);
            y += lineH + Screen.height * 0.008f;
        }

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
            float by = H - pad - btnH;
            if (selected == null || (!ch.Stash.Contains(selected) && !Profile.Storage.Contains(selected)))
            {
                selected = null;
                GUI.Label(new Rect(pad, y, W - pad * 2f, lineH), Loc.T("Tap an item to move it or salvage it.", "品をタップすると、移したり分解したりできる。"), small);
                return;
            }

            // 選んだ品の中身
            var name = new GUIStyle(hud.LabelStyle) { fontStyle = FontStyle.Bold };
            name.normal.textColor = RarityInfo.ColorOf(selected.Rarity);
            GUI.Label(new Rect(pad, y, W - pad * 2f, lineH * 1.2f), selected.Name, name);
            y += lineH * 1.2f;
            if (Town.OtherClassWeapon(selected, type))
            {
                var warn = new GUIStyle(small);
                warn.normal.textColor = new Color(1f, 0.6f, 0.5f);
                GUI.Label(new Rect(pad, y, W - pad * 2f, lineH), Loc.T($"Weapon for {ClassInfo.Get(ClassInfo.Weapon(selected.Weapon).Class).DisplayName}", $"{ClassInfo.Get(ClassInfo.Weapon(selected.Weapon).Class).DisplayName}の武器"), warn);
                y += lineH;
            }
            foreach (var line in selected.Describe())
            {
                if (y + lineH > by - btnH - W * 0.03f - H * 0.01f) break; // 2段のボタンの上まで
                GUI.Label(new Rect(pad, y, W - pad * 2f, lineH), line, small);
                y += lineH;
            }

            float gap = W * 0.03f;
            float half = (W - pad * 2f - gap) * 0.5f;
            bool inStash = ch.Stash.Contains(selected);
            var item = selected;
            float by1 = by - btnH - gap * 0.5f; // 上の段：移す・ロック  下の段：売却・破棄
            if (inStash)
            {
                bool room = Profile.Storage.Count < Town.StorageCapacity;
                hud.Button(new Rect(pad, by1, half, btnH), room ? Loc.T("TO STORAGE", "倉庫へ") : Loc.T("STORAGE FULL", "倉庫が満杯"), row, hud.CenteredStyle,
                    () => { if (Town.Store(ch, item)) selected = null; });
            }
            else
            {
                bool room = ch.Stash.Count < Inventory.BagCapacity;
                var r = new Rect(pad, by1, half, btnH);
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
            hud.Button(new Rect(pad + half + gap, by1, half, btnH), item.Locked ? Loc.T("UNLOCK", "ロック解除") : Loc.T("LOCK", "ロック"),
                item.Locked ? gold : row, hud.CenteredStyle, () => { item.Locked = !item.Locked; Profile.Save(); });

            if (item.Locked)
            {
                var lr = new Rect(pad, by, W - pad * 2f, btnH);
                Hud.Fill(lr, dim);
                Hud.FitLabel(lr, Loc.T("Locked: unlock to sell or discard", "ロック中：外すと売却・破棄できる"), hud.CenteredStyle);
            }
            else
            {
                Confirm(new Rect(pad, by, half, btnH), "salvage", Loc.T($"SELL +{Town.SalvageValue(item)} embers", $"売却 火種+{Town.SalvageValue(item)}"),
                    () => { if (Town.Salvage(ch, item)) selected = null; });
                Confirm(new Rect(pad + half + gap, by, half, btnH), "discard", Loc.T("DISCARD", "破棄"),
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
                var c = RarityInfo.ColorOf(item.Rarity);
                var bg = item == selected ? new Color(1f, 1f, 1f, 0.45f) : new Color(c.r, c.g, c.b, 0.22f);
                hud.Button(r, null, bg, hud.CenteredStyle, () => { selected = item; armed = null; });
                float inset = r.width * 0.15f;
                var old = GUI.color;
                if (Town.OtherClassWeapon(item, type)) GUI.color = new Color(1f, 1f, 1f, 0.35f);
                Hud.DrawSprite(new Rect(r.x + inset, r.y + inset, r.width - inset * 2f, r.height - inset * 2f), PixelArt.ItemIcon(item));
                Hud.DrawAbyssFrame(r, item);
                GUI.color = old;
                Hud.DrawEnhance(r, item);
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

        // ================= 転職の師匠 =================

        void DrawMaster(GameWorld world, CharacterData ch)
        {
            float W = Screen.width, H = Screen.height;
            float pad = W * 0.05f;
            var type = ClassInfo.Current;
            var small = hud.SmallStyle;
            float lineH = small.fontSize * 1.4f;
            Header(Loc.T("MASTER", "転職の師匠"), ch.Embers, W, pad, out float y);

            var current = AdvancedClasses.Of(type, ch);
            if (current != null)
            {
                DrawRebirth(world, ch, type, current, y);
                return;
            }
            string status = current != null ? Loc.T($"You are a {current.DisplayName}. Class change is done once.", $"あなたは{current.DisplayName}。転職は1回だけ。")
                : Loc.T($"From Lv {AdvancedClasses.ReqLevel}, for {AdvancedClasses.Cost} embers. You go back to Lv 1 (stat points reset) and get {AdvancedClasses.BonusPoints} bonus points. Once only, cannot be undone (except RESET ALL).",
                    $"Lv{AdvancedClasses.ReqLevel}から、火種 {AdvancedClasses.Cost}。Lv1 に戻り（振った能力値も戻る）、ボーナスで {AdvancedClasses.BonusPoints} ポイント振れる。1回だけで、戻せない（すべてリセットを除く）。");
            var wrap = new GUIStyle(small) { wordWrap = true };
            float sh = wrap.CalcHeight(new GUIContent(status), W - pad * 2f);
            GUI.Label(new Rect(pad, y, W - pad * 2f, sh), status, wrap);
            y += sh + H * 0.015f;

            float cardH = H * 0.3f;
            for (int i = 0; i < 2; i++)
            {
                var adv = AdvancedClasses.Get(type, i);
                bool mine = ch.Advanced == i;
                var r = new Rect(pad, y, W - pad * 2f, cardH);
                Hud.Fill(r, mine ? new Color(0.5f, 0.4f, 0.1f, 0.5f) : row);
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
                if (mine)
                {
                    GUI.Label(btn, Loc.T("YOUR CLASS", "今の職業"), hud.CenteredStyle);
                }
                else if (current == null && AdvancedClasses.CanChange(ch))
                {
                    int pick = i;
                    Confirm(btn, "adv" + i, Loc.T($"CHANGE CLASS  ({AdvancedClasses.Cost} embers)", $"転職する（火種 {AdvancedClasses.Cost}）"), () =>
                    {
                        if (Town.ChangeClass(ch, pick)) world.OnAdvanced();
                    });
                }
                else if (current == null)
                {
                    Hud.Fill(btn, dim);
                    string why = ch.Level < AdvancedClasses.ReqLevel ? Loc.T($"Need Lv {AdvancedClasses.ReqLevel}", $"Lv{AdvancedClasses.ReqLevel}が必要")
                        : Loc.T($"Need {AdvancedClasses.Cost} embers", $"火種 {AdvancedClasses.Cost} が必要");
                    var s = new GUIStyle(hud.CenteredStyle);
                    s.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
                    GUI.Label(btn, why, s);
                }
                y += cardH + H * 0.015f;
            }
        }

        /// <summary>上位職になったあと：転生（3次職）の欄。</summary>
        void DrawRebirth(GameWorld world, CharacterData ch, ClassType type, AdvancedSpec current, float y)
        {
            float W = Screen.width, H = Screen.height;
            float pad = W * 0.05f;
            var small = hud.SmallStyle;
            float lineH = small.fontSize * 1.4f;
            var wrap = new GUIStyle(small) { wordWrap = true };
            var third = ThirdClasses.Get(type, ch.Advanced);

            // 転生は、条件（Lv99）を満たすまで明かさない（一度転生したら、ずっと見せる）
            if (!ch.Third && !ThirdClasses.MeetsRequirements(ch))
            {
                var plain = new GUIStyle(hud.LabelStyle) { wordWrap = true };
                GUI.Label(new Rect(pad, y, W - pad * 2f, H * 0.1f), Loc.T($"You are a {current.DisplayName}. Keep growing stronger.", $"あなたは{current.DisplayName}。さらに強くなりな。"), plain);
                return;
            }
            string status = ch.Third
                ? Loc.T($"You are a {third.DisplayName}, reborn {ch.Rebirths} time(s). At Lv {ThirdClasses.ReqLevel} you can be reborn again ({ThirdClasses.Cost(ch)} embers): back to Lv 1, {ThirdClasses.BonusPoints} bonus points, all stats +{ThirdClasses.StatsPerRebirth} more.",
                    $"あなたは{third.DisplayName}（転生 {ch.Rebirths}回）。Lv{ThirdClasses.ReqLevel}になると、火種 {ThirdClasses.Cost(ch)} でまた転生できる。Lv1 に戻り、ボーナスで {ThirdClasses.BonusPoints} ポイント振れて、全能力値がさらに+{ThirdClasses.StatsPerRebirth}。")
                : Loc.T($"You are a {current.DisplayName}. At Lv {ThirdClasses.ReqLevel} you can be reborn ({ThirdClasses.Cost(ch)} embers). You go back to Lv 1 and get {ThirdClasses.BonusPoints} bonus points. You can be reborn again and again (the cost doubles each time).",
                    $"あなたは{current.DisplayName}。Lv{ThirdClasses.ReqLevel}になると、火種 {ThirdClasses.Cost(ch)} で転生できる。Lv1 に戻り、ボーナスで {ThirdClasses.BonusPoints} ポイント振れる。何回でも転生できる（火種は毎回倍になる）。");
            float sh = wrap.CalcHeight(new GUIContent(status), W - pad * 2f);
            GUI.Label(new Rect(pad, y, W - pad * 2f, sh), status, wrap);
            y += sh + H * 0.015f;

            var r = new Rect(pad, y, W - pad * 2f, H * 0.42f);
            Hud.Fill(r, ch.Third ? new Color(0.5f, 0.4f, 0.1f, 0.5f) : row);
            float inner = r.width * 0.04f;
            float ty = r.y + inner;
            float portrait = r.width * 0.22f;
            Hud.DrawSprite(new Rect(r.xMax - inner - portrait, ty, portrait, portrait), PixelArt.ThirdPreview(type, ch.Advanced));
            var nameStyle = new GUIStyle(hud.LabelStyle) { fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(hud.LabelStyle.fontSize * 1.3f) };
            nameStyle.normal.textColor = new Color(1f, 0.8f, 0.35f);
            GUI.Label(new Rect(r.x + inner, ty, r.width, lineH * 1.5f), Loc.T("Rebirth: ", "転生：") + third.DisplayName, nameStyle);
            ty += lineH * 1.6f;
            float textW = r.width - inner * 3f - portrait;
            string desc = third.DisplayDesc + Loc.T($"  All stats +{ThirdClasses.StatsPerRebirth} per rebirth, permanently (now +{ThirdClasses.BonusStats(ch)}).",
                $"  転生のたびに全能力値+{ThirdClasses.StatsPerRebirth}（ずっと。今 +{ThirdClasses.BonusStats(ch)}）。");
            float dh = wrap.CalcHeight(new GUIContent(desc), textW);
            GUI.Label(new Rect(r.x + inner, ty, textW, dh), desc, wrap);
            ty = Mathf.Max(ty + dh, r.y + inner + portrait) + lineH * 0.4f;

            var skill = third.Skill;
            float icon = lineH * 2f;
            Hud.DrawSprite(new Rect(r.x + inner, ty, icon, icon), PixelArt.SkillIcon(skill));
            var sk = new GUIStyle(small) { fontStyle = FontStyle.Bold };
            GUI.Label(new Rect(r.x + inner + icon * 1.2f, ty, r.width, lineH), Loc.T($"Skill: {skill.DisplayName}   MP {skill.Mp}", $"スキル：{skill.DisplayName}   MP {skill.Mp}"), sk);
            GUI.Label(new Rect(r.x + inner + icon * 1.2f, ty + lineH, r.width - inner * 2f - icon * 1.2f, lineH), skill.DisplayDesc, small);
            ty += icon + lineH * 0.5f;

            // 条件（満たしたものは緑）
            if (!ch.Third || ThirdClasses.MeetsRequirements(ch))
            {
                var ok = new Color(0.5f, 1f, 0.5f);
                var ng = new Color(1f, 0.55f, 0.5f);
                void Req(string text, bool met)
                {
                    var st = new GUIStyle(small);
                    st.normal.textColor = met ? ok : ng;
                    GUI.Label(new Rect(r.x + inner, ty, r.width - inner * 2f, lineH), (met ? "OK  " : "--  ") + text, st);
                    ty += lineH;
                }
                Req(Loc.T($"Lv {ThirdClasses.ReqLevel}   (now Lv {ch.Level})", $"Lv{ThirdClasses.ReqLevel}   （今 Lv{ch.Level}）"), ch.Level >= ThirdClasses.ReqLevel);
                Req(Loc.T($"{ThirdClasses.Cost(ch)} embers   (have {ch.Embers})", $"火種 {ThirdClasses.Cost(ch)}   （持っている {ch.Embers}）"), ch.Embers >= ThirdClasses.Cost(ch));
            }

            float btnH = H * 0.055f;
            var btn = new Rect(r.x + inner, r.yMax - inner - btnH, r.width - inner * 2f, btnH);
            if (ThirdClasses.CanRebirth(ch))
            {
                string again = ch.Third ? Loc.T($"REBIRTH #{ch.Rebirths + 1}", $"{ch.Rebirths + 1}回目の転生") : Loc.T("REBIRTH", "転生する");
                Confirm(btn, "rebirth", again + Loc.T($"  ({ThirdClasses.Cost(ch)} embers)", $"（火種 {ThirdClasses.Cost(ch)}）"), () =>
                {
                    if (Town.Rebirth(ch)) world.OnRebirth();
                });
            }
            else if (ch.Third && !ThirdClasses.MeetsRequirements(ch))
            {
                var s = new GUIStyle(hud.CenteredStyle);
                s.normal.textColor = ThirdClasses.AuraColor(ch.Rebirths);
                GUI.Label(btn, Loc.T($"YOUR CLASS  -  reborn x{ch.Rebirths}  (again at Lv {ThirdClasses.ReqLevel})", $"今の職業  ・  転生 {ch.Rebirths}回（Lv{ThirdClasses.ReqLevel}でまた転生できる）"), s);
            }
            else
            {
                Hud.Fill(btn, dim);
                var s = new GUIStyle(hud.CenteredStyle);
                s.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
                GUI.Label(btn, Loc.T("Requirements not met", "条件を満たしていない"), s);
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
                string help = Loc.T($"Tap an item. Enhancing (Relic/orange up to +10, Mythic/red and Abyssal up to +{Item.MaxEnhance}) raises its base value and effects by {Item.EnhanceStep * 100f:0}% per level. Abyssal gear also needs Abyss Shards (1 per level, 15 for +15). Rerolling (Mythic only) draws new effects at the item's level.   Abyss Shards: {ch.AbyssShards}",
                    $"品をタップ。強化（灯宝＝橙は+10、神話＝赤と深淵の品は+{Item.MaxEnhance}まで）すると、1段ごとに基本の数値と効果の値が{Item.EnhanceStep * 100f:0}%上がる。深淵の品は深淵の欠片も要る（1段ごとに1個、+15だけ15個）。再抽選（神話だけ）すると、その品のレベルで効果を引き直す。　深淵の欠片：{ch.AbyssShards}個");
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
            if (item.Enhance > 0) Hud.DrawSprite(new Rect(W - pad - emblem, y - lineH * 0.2f, emblem, emblem), PixelArt.EnhanceEmblem(item.Enhance));
            var nameRect = new Rect(pad, y, W - pad * 2f - emblem, lineH * 1.2f);
            var name = new GUIStyle(hud.LabelStyle) { fontStyle = FontStyle.Bold };
            name.normal.textColor = RarityInfo.ColorOf(item.Rarity);
            Hud.FitLabel(nameRect, item.Name, name);
            y += lineH * 1.2f;
            var info = new GUIStyle(small);
            info.normal.textColor = new Color(1f, 0.85f, 0.5f);
            GUI.Label(new Rect(pad, y, W - pad * 2f, lineH),
                Loc.T($"Enhance +{item.Enhance}/{item.EnhanceCap}   (values x{item.EnhanceMult:0.00})" + (item.Abyss ? $"   Shards {ch.AbyssShards}" : ""),
                      $"強化 +{item.Enhance}/{item.EnhanceCap}   （値 x{item.EnhanceMult:0.00}）" + (item.Abyss ? $"   欠片 {ch.AbyssShards}個" : "")), info);
            y += lineH;

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
                if (y + rowLine > by - btnH - H * 0.02f) break;
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
                bool can = ch.Embers >= price && ch.AbyssShards >= shards;
                string shardNote = shards > 0 ? Loc.T($", {shards} shard" + (shards > 1 ? "s" : ""), $"・欠片 {shards}") : "";
                string label = Loc.T($"ENHANCE  +{item.Enhance} → +{item.Enhance + 1}   ({price} embers{shardNote})", $"強化  +{item.Enhance} → +{item.Enhance + 1}   （火種 {price}{shardNote}）");
                if (can) hud.Button(enhanceRect, label, gold, hud.CenteredStyle, () =>
                {
                    if (!Town.EnhanceItem(ch, item)) return;
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
