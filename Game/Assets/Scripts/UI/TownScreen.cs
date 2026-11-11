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
            Hud.Fill(new Rect(0, 0, W, H), back);
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
            GUI.Label(new Rect(0, H * 0.11f, W, H * 0.035f), $"{ClassTitle(type, ch)}   Lv {ch.Level}", centered);
            var info = new GUIStyle(centered) { fontSize = small.fontSize };
            info.normal.textColor = new Color(1f, 0.85f, 0.5f);
            GUI.Label(new Rect(0, H * 0.145f, W, H * 0.03f),
                Loc.T($"Embers {ch.Embers}   HP potions {ch.HpPotions}   MP potions {ch.MpPotions}", $"火種 {ch.Embers}   回復薬 {ch.HpPotions}   魔力の薬 {ch.MpPotions}"), info);

            float y = H * 0.19f;
            float bh = H * 0.075f;
            float gap = H * 0.012f;
            var bw = W - pad * 2f;

            hud.Button(new Rect(pad, y, bw, bh), Loc.T("DEPART  -  into the tower", "出撃  -  塔へ"), new Color(0.2f, 0.5f, 0.25f, 0.85f), centered, world.Depart);
            y += bh + gap * 1.5f;

            NpcRow(new Rect(pad, y, bw, bh), Loc.T("Storage Keeper", "倉庫番"),
                Loc.T($"Storage {Profile.Storage.Count}/{Town.StorageCapacity} (all classes)   Bring {ch.Stash.Count}/{Inventory.BagCapacity}", $"倉庫 {Profile.Storage.Count}/{Town.StorageCapacity}（全職業）   持ち込み {ch.Stash.Count}/{Inventory.BagCapacity}"),
                () => Open(Panel.Storage));
            y += bh + gap;
            NpcRow(new Rect(pad, y, bw, bh), Loc.T("Merchant", "商人"), Loc.T("Potions and gear for embers", "薬と装備を火種で売る"), () => Open(Panel.Merchant));
            y += bh + gap;
            var adv = AdvancedClasses.Of(type, ch);
            string masterLine = adv != null ? Loc.T($"Advanced class: {adv.DisplayName}", $"上位職：{adv.DisplayName}")
                : ch.Level >= AdvancedClasses.ReqLevel ? Loc.T("You can change class now!", "転職できる！")
                : Loc.T($"Advanced class from Lv {AdvancedClasses.ReqLevel}", $"Lv{AdvancedClasses.ReqLevel}から上位職へ転職");
            NpcRow(new Rect(pad, y, bw, bh), Loc.T("Master", "転職の師匠"), masterLine, () => Open(Panel.Master),
                adv == null && ch.Level >= AdvancedClasses.ReqLevel);
            y += bh + gap;
            NpcRow(new Rect(pad, y, bw, bh), Loc.T("Porter", "荷運び"),
                Loc.T($"Carry home {ch.CarrySlots}/{Town.MaxCarrySlots} new items", $"持ち帰り枠 {ch.CarrySlots}/{Town.MaxCarrySlots}"), () => Open(Panel.Porter));
            y += bh + gap;
            NpcRow(new Rect(pad, y, bw, bh), Loc.T("Blacksmith", "鍛冶屋"),
                Loc.T("Enhance Mythic gear (+15) and reroll its effects", "神話（赤）装備の強化（+15まで）と、効果の再抽選"), () => Open(Panel.Smith));
            y += bh + gap * 1.5f;

            hud.Button(new Rect(pad, y, bw, H * 0.05f), Loc.T("Change class", "職業を変える"), dim, centered, world.ShowClassSelect);

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

        void NpcRow(Rect r, string name, string line, System.Action onTap, bool highlight = false)
        {
            hud.Button(r, null, highlight ? gold : row, hud.CenteredStyle, onTap);
            float inner = r.height * 0.12f;
            var nameStyle = new GUIStyle(hud.LabelStyle) { fontStyle = FontStyle.Bold };
            GUI.Label(new Rect(r.x + inner * 2f, r.y + inner, r.width, r.height * 0.45f), name, nameStyle);
            var lineStyle = new GUIStyle(hud.SmallStyle);
            lineStyle.normal.textColor = new Color(0.8f, 0.8f, 0.85f);
            GUI.Label(new Rect(r.x + inner * 2f, r.y + r.height * 0.5f, r.width - inner * 3f, r.height * 0.45f), line, lineStyle);
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
            y = Grid(ch.Stash, Inventory.BagCapacity, y, pad, type) + H * 0.01f;
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
                if (y + lineH > by - H * 0.01f) break;
                GUI.Label(new Rect(pad, y, W - pad * 2f, lineH), line, small);
                y += lineH;
            }

            float gap = W * 0.03f;
            float half = (W - pad * 2f - gap) * 0.5f;
            bool inStash = ch.Stash.Contains(selected);
            var item = selected;
            if (inStash)
            {
                bool room = Profile.Storage.Count < Town.StorageCapacity;
                hud.Button(new Rect(pad, by, half, btnH), room ? Loc.T("TO STORAGE", "倉庫へ") : Loc.T("STORAGE FULL", "倉庫が満杯"), row, hud.CenteredStyle,
                    () => { if (Town.Store(ch, item)) selected = null; });
            }
            else
            {
                bool room = ch.Stash.Count < Inventory.BagCapacity;
                var r = new Rect(pad, by, half, btnH);
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
            if (item.Locked)
            {
                Hud.Fill(new Rect(pad + half + gap, by, half, btnH), dim);
                GUI.Label(new Rect(pad + half + gap, by, half, btnH), Loc.T("LOCKED", "ロック中"), hud.CenteredStyle);
            }
            else
            {
                Confirm(new Rect(pad + half + gap, by, half, btnH), "salvage", Loc.T($"SALVAGE +{RarityInfo.SalvageOf(item.Rarity)}", $"分解 +{RarityInfo.SalvageOf(item.Rarity)}"),
                    () => { if (Town.Salvage(ch, item)) selected = null; });
            }
        }

        /// <summary>品の一覧（8列）。描いた下端の y を返す。</summary>
        float Grid(List<Item> items, int capacity, float y, float pad, ClassType type)
        {
            float W = Screen.width;
            const int cols = 8;
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
                Hud.DrawSprite(new Rect(r.x + inset, r.y + inset, r.width - inset * 2f, r.height - inset * 2f), PixelArt.ItemIcon(item.Slot, item.Rarity));
                GUI.color = old;
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
                Loc.T($"Restores {Town.HpPotionHeal * 100f:0}% HP. Q key or the HP button.", $"体力を{Town.HpPotionHeal * 100f:0}%回復。Q キーか HP ボタン。"),
                potion, ch.HpPotions < Town.MaxPotions && ch.Embers >= potion, () => Town.BuyPotion(ch, true));
            y += bh + gap;
            Offer(new Rect(pad, y, W - pad * 2f, bh), Loc.T($"MP Potion   ({ch.MpPotions}/{Town.MaxPotions})", $"魔力の薬   （{ch.MpPotions}/{Town.MaxPotions}）"),
                Loc.T($"Restores {Town.MpPotionRestore * 100f:0}% MP. E key or the MP button.", $"MPを{Town.MpPotionRestore * 100f:0}%回復。E キーか MP ボタン。"),
                potion, ch.MpPotions < Town.MaxPotions && ch.Embers >= potion, () => Town.BuyPotion(ch, false));
            y += bh + gap;
            int gear = Town.GearPrice(ch);
            Offer(new Rect(pad, y, W - pad * 2f, bh), Loc.T("Gear Crate", "装備の箱"),
                Loc.T($"Random Rare or better gear, Lv {ch.Level}. Goes into the items you bring.", $"Lv{ch.Level}の「稀」以上の装備がランダムで1つ。持ち込む品に入る。"),
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

        void Offer(Rect r, string name, string desc, int price, bool can, System.Action buy)
        {
            Hud.Fill(r, row);
            float inner = r.height * 0.1f;
            var nameStyle = new GUIStyle(hud.LabelStyle) { fontStyle = FontStyle.Bold };
            GUI.Label(new Rect(r.x + inner, r.y + inner, r.width * 0.7f, r.height * 0.4f), name, nameStyle);
            var descStyle = new GUIStyle(hud.SmallStyle) { wordWrap = true };
            descStyle.normal.textColor = new Color(0.8f, 0.8f, 0.85f);
            GUI.Label(new Rect(r.x + inner, r.y + r.height * 0.45f, r.width * 0.68f, r.height * 0.55f), desc, descStyle);
            var btn = new Rect(r.xMax - r.width * 0.28f, r.y + inner, r.width * 0.28f - inner, r.height - inner * 2f);
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
                Hud.DrawSprite(new Rect(r.x + inner, ty, icon, icon), PixelArt.SkillIcon(skill.Effect, skill.Color));
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

            string status = ch.Third
                ? Loc.T($"You are reborn as a {third.DisplayName}.", $"あなたは転生して{third.DisplayName}になった。")
                : Loc.T($"You are a {current.DisplayName}. Reach Lv {ThirdClasses.ReqLevel} and floor {ThirdClasses.ReqFloor} to be reborn ({ThirdClasses.Cost} embers). You go back to Lv 1 and get {ThirdClasses.BonusPoints} bonus points. Once only.",
                    $"あなたは{current.DisplayName}。Lv{ThirdClasses.ReqLevel}で{ThirdClasses.ReqFloor}層にたどり着くと、火種 {ThirdClasses.Cost} で転生できる。Lv1 に戻り、ボーナスで {ThirdClasses.BonusPoints} ポイント振れる。1回だけ。");
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
            string desc = third.DisplayDesc + Loc.T($"  All stats +{ThirdClasses.AllStats} permanently.", $"  全能力値+{ThirdClasses.AllStats}（ずっと）。");
            float dh = wrap.CalcHeight(new GUIContent(desc), textW);
            GUI.Label(new Rect(r.x + inner, ty, textW, dh), desc, wrap);
            ty = Mathf.Max(ty + dh, r.y + inner + portrait) + lineH * 0.4f;

            var skill = third.Skill;
            float icon = lineH * 2f;
            Hud.DrawSprite(new Rect(r.x + inner, ty, icon, icon), PixelArt.SkillIcon(skill.Effect, skill.Color));
            var sk = new GUIStyle(small) { fontStyle = FontStyle.Bold };
            GUI.Label(new Rect(r.x + inner + icon * 1.2f, ty, r.width, lineH), Loc.T($"Skill: {skill.DisplayName}   MP {skill.Mp}", $"スキル：{skill.DisplayName}   MP {skill.Mp}"), sk);
            GUI.Label(new Rect(r.x + inner + icon * 1.2f, ty + lineH, r.width - inner * 2f - icon * 1.2f, lineH), skill.DisplayDesc, small);
            ty += icon + lineH * 0.5f;

            // 条件（満たしたものは緑）
            if (!ch.Third)
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
                Req(Loc.T($"Reach floor {ThirdClasses.ReqFloor}   (best {ch.BestFloor})", $"{ThirdClasses.ReqFloor}層に到達   （最深 {ch.BestFloor}層）"), ch.BestFloor >= ThirdClasses.ReqFloor);
                Req(Loc.T($"{ThirdClasses.Cost} embers   (have {ch.Embers})", $"火種 {ThirdClasses.Cost}   （持っている {ch.Embers}）"), ch.Embers >= ThirdClasses.Cost);
            }

            float btnH = H * 0.055f;
            var btn = new Rect(r.x + inner, r.yMax - inner - btnH, r.width - inner * 2f, btnH);
            if (ch.Third)
            {
                GUI.Label(btn, Loc.T("YOUR CLASS", "今の職業"), hud.CenteredStyle);
            }
            else if (ThirdClasses.CanRebirth(ch))
            {
                Confirm(btn, "rebirth", Loc.T($"REBIRTH  ({ThirdClasses.Cost} embers)", $"転生する（火種 {ThirdClasses.Cost}）"), () =>
                {
                    if (Town.Rebirth(ch)) world.OnRebirth();
                });
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
            var small = hud.SmallStyle;
            var type = ClassInfo.Current;
            float lineH = small.fontSize * 1.4f;
            Header(Loc.T("BLACKSMITH", "鍛冶屋"), ch.Embers, W, pad, out float y);

            // 装備中の品（前回帰還したときの装備）と、次の挑戦に持ち込む品
            var equipped = new List<Item>();
            foreach (var it in ch.Loadout) if (it != null && !it.IsEmptyMarker) equipped.Add(it);
            GUI.Label(new Rect(pad, y, W, lineH), Loc.T("Equipped", "装備中"), small);
            y += lineH;
            y = Grid(equipped, 5, y, pad, type) + H * 0.008f;
            GUI.Label(new Rect(pad, y, W, lineH), Loc.T($"Items you bring: {ch.Stash.Count}/{Inventory.BagCapacity}", $"持ち込む品：{ch.Stash.Count}/{Inventory.BagCapacity}"), small);
            y += lineH;
            y = Grid(ch.Stash, Inventory.BagCapacity, y, pad, type) + H * 0.01f;

            float btnH = H * 0.055f;
            float by = H - pad - btnH;
            if (selected == null || (!equipped.Contains(selected) && !ch.Stash.Contains(selected)))
            {
                selected = null;
                var wrap = new GUIStyle(small) { wordWrap = true };
                string help = Loc.T($"Only Mythic (red) gear can be worked on. Tap an item. Enhancing raises its base value and effects by {Item.EnhanceStep * 100f:0}% per level (up to +{Item.MaxEnhance}). Rerolling draws new effects at the item's level.",
                    $"扱えるのは神話（赤）の装備だけ。品をタップ。強化すると、1段ごとに基本の数値と効果の値が{Item.EnhanceStep * 100f:0}%上がる（+{Item.MaxEnhance}まで）。再抽選すると、その品のレベルで効果を引き直す。");
                GUI.Label(new Rect(pad, y, W - pad * 2f, wrap.CalcHeight(new GUIContent(help), W - pad * 2f)), help, wrap);
                return;
            }

            var item = selected;
            var name = new GUIStyle(hud.LabelStyle) { fontStyle = FontStyle.Bold };
            name.normal.textColor = RarityInfo.ColorOf(item.Rarity);
            Hud.FitLabel(new Rect(pad, y, W - pad * 2f, lineH * 1.2f), item.Name, name);
            y += lineH * 1.2f;
            var info = new GUIStyle(small);
            info.normal.textColor = new Color(1f, 0.85f, 0.5f);
            GUI.Label(new Rect(pad, y, W - pad * 2f, lineH),
                Loc.T($"Enhance +{item.Enhance}/{Item.MaxEnhance}   (values x{item.EnhanceMult:0.00})", $"強化 +{item.Enhance}/{Item.MaxEnhance}   （値 x{item.EnhanceMult:0.00}）"), info);
            y += lineH;
            foreach (var line in item.Describe())
            {
                if (y + lineH > by - btnH - H * 0.02f) break;
                GUI.Label(new Rect(pad, y, W - pad * 2f, lineH), line, small);
                y += lineH;
            }

            if (!Town.Smithable(item))
            {
                var r = new Rect(pad, by, W - pad * 2f, btnH);
                Hud.Fill(r, dim);
                Hud.FitLabel(r, Loc.T("Only Mythic (red) gear can be worked on", "神話（赤）の装備だけ扱える"), hud.CenteredStyle);
                return;
            }

            // 強化（すぐ実行）と再抽選（2回押し）
            float gap = W * 0.03f;
            float half = (W - pad * 2f - gap) * 0.5f;
            var enhanceRect = new Rect(pad, by - btnH - gap * 0.5f, W - pad * 2f, btnH);
            if (item.Enhance >= Item.MaxEnhance)
            {
                Hud.Fill(enhanceRect, dim);
                GUI.Label(enhanceRect, Loc.T("MAX ENHANCED", "強化は最大"), hud.CenteredStyle);
            }
            else
            {
                int price = Town.EnhancePrice(item);
                bool can = ch.Embers >= price;
                string label = Loc.T($"ENHANCE  +{item.Enhance} → +{item.Enhance + 1}   ({price} embers)", $"強化  +{item.Enhance} → +{item.Enhance + 1}   （火種 {price}）");
                if (can) hud.Button(enhanceRect, label, gold, hud.CenteredStyle, () =>
                {
                    if (Town.EnhanceItem(ch, item)) hud.Toast(item.Name, RarityInfo.ColorOf(item.Rarity));
                });
                else
                {
                    Hud.Fill(enhanceRect, dim);
                    Hud.FitLabel(enhanceRect, label, hud.CenteredStyle);
                }
            }
            if (item.Affixes.Count > 0)
            {
                int rp = Town.RerollPrice(item);
                var rr = new Rect(pad, by, W - pad * 2f, btnH);
                if (ch.Embers >= rp)
                    Confirm(rr, "reroll", Loc.T($"REROLL EFFECTS   ({rp} embers)", $"効果を再抽選   （火種 {rp}）"), () => Town.RerollItem(ch, item, type));
                else
                {
                    Hud.Fill(rr, dim);
                    Hud.FitLabel(rr, Loc.T($"REROLL EFFECTS   ({rp} embers)", $"効果を再抽選   （火種 {rp}）"), hud.CenteredStyle);
                }
            }
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
