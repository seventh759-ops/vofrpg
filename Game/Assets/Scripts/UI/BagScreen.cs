using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 鞄の画面（開いている間はゲームが止まる）。装備欄5つ、鞄24マス、選んだ品の詳しい内容と、
    /// 今の装備との比較。今より強くなる品には緑の「+」を付ける。
    /// </summary>
    public sealed class BagScreen
    {
        static readonly string[] slotLabels = { "WPN", "LTN", "ARM", "R1", "R2" };
        static readonly Color panel = new Color(0f, 0f, 0f, 0.88f);
        static readonly Color cellBack = new Color(1f, 1f, 1f, 0.08f);
        static readonly Color buttonBack = new Color(1f, 1f, 1f, 0.15f);
        static readonly Color up = new Color(0.45f, 1f, 0.45f);
        static readonly Color down = new Color(1f, 0.45f, 0.45f);

        readonly Hud hud;
        Item selectedItem;
        bool bulkOpen; // 一括分解の選択欄を開いている
        int selectedEquip = -1;
        bool statsTab; // 選んだ品の下を「能力」（付けたら能力がどう変わるか）にする

        public BagScreen(Hud hud)
        {
            this.hud = hud;
        }

        public void Draw(GameWorld world)
        {
            var inv = world.Inventory;
            if (selectedItem != null && !inv.Bag.Contains(selectedItem)) selectedItem = null;

            float W = Screen.width;
            float H = Screen.height;
            float pad = W * 0.04f;
            float gap = W * 0.015f;
            var small = hud.SmallStyle;
            var centered = hud.CenteredStyle;
            float lineH = small.fontSize * 1.3f;

            Hud.Fill(new Rect(0, 0, W, H), panel);

            // 見出しと閉じるボタン
            float btnH = H * 0.045f;
            GUI.Label(new Rect(pad, pad, W, btnH), Loc.T($"BAG {inv.Bag.Count}/{Inventory.BagCapacity}   EMBERS {inv.Embers}", $"鞄 {inv.Bag.Count}/{Inventory.BagCapacity}   火種 {inv.Embers}"), hud.LabelStyle);
            hud.Button(new Rect(W - pad - W * 0.22f, pad, W * 0.22f, btnH), Loc.T("CLOSE", "閉じる"), buttonBack, centered, () => world.OpenPanel(false, false, false)); // 町で開いたときは、ここで装備を保存する

            // 装備欄
            float y = pad + btnH + H * 0.02f;
            float cell = (W - pad * 2f - gap * 4f) / 5f;
            for (int i = 0; i < 5; i++)
            {
                var r = new Rect(pad + i * (cell + gap), y, cell, cell);
                var item = inv.Equipped[i];
                int index = i;
                DrawCell(r, item, selectedEquip == i, false, () =>
                {
                    selectedEquip = index;
                    selectedItem = null;
                });
                GUI.Label(new Rect(r.x, r.yMax, r.width, lineH), SlotLabel(i), CenteredSmall());
            }

            // 鞄
            y += cell + lineH + H * 0.015f;
            const int cols = 6;
            float bcell = (W - pad * 2f - gap * (cols - 1)) / cols;
            var current = world.Player.BaseStats; // スキルの強化を含まない能力で比べる
            for (int i = 0; i < Inventory.BagCapacity; i++)
            {
                var r = new Rect(pad + (i % cols) * (bcell + gap), y + (i / cols) * (bcell + gap), bcell, bcell);
                var item = i < inv.Bag.Count ? inv.Bag[i] : null;
                bool better = item != null && world.CanEquip(item, out _) && IsUpgrade(inv, item, current);
                DrawCell(r, item, item != null && item == selectedItem, better, item == null ? (System.Action)null : () =>
                {
                    selectedItem = item;
                    selectedEquip = -1;
                });
            }
            y += (Inventory.BagCapacity / cols) * (bcell + gap) + H * 0.01f;

            // 下の段：一括分解（押すと選択欄が開く）と、拾ったときの自動分解
            float bottomY = H - pad - btnH;
            float half = (W - pad * 2f - gap) * 0.5f;
            hud.Button(new Rect(pad, bottomY, half, btnH), Loc.T("BULK SALVAGE", "まとめて分解"), bulkOpen ? new Color(1f, 1f, 1f, 0.3f) : buttonBack, centered,
                () => bulkOpen = !bulkOpen);
            hud.Button(new Rect(pad + half + gap, bottomY, half, btnH), AutoLabel(inv.AutoSalvageMax), buttonBack, centered,
                () => inv.CycleAutoSalvage());

            // 選んだ品の詳細と操作（一括分解の選択欄を開いているときは、そちらを出す）
            float actionY = bottomY - btnH - gap;
            var detail = new Rect(pad, y, W - pad * 2f, actionY - gap - y);
            if (bulkOpen) DrawBulkMenu(inv, current, new Rect(pad, y, W - pad * 2f, bottomY - gap - y), btnH, gap);
            else if (selectedItem != null) DrawBagItem(world, inv, selectedItem, detail, actionY, btnH, gap, pad);
            else if (selectedEquip >= 0) DrawEquipped(inv, selectedEquip, detail, actionY, btnH, pad);
            else GUI.Label(detail, Loc.T("Tap an item to see details.", "品をタップすると詳しく見られます。"), small);
        }

        void DrawBagItem(GameWorld world, Inventory inv, Item item, Rect area, float actionY, float btnH, float gap, float pad)
        {
            bool canEquip = world.CanEquip(item, out var reason);
            var candidate = BestCandidate(inv, item, out var slot);
            var now = world.Player.BaseStats;
            float change = now.Power > 0f ? candidate.Power / now.Power - 1f : 0f;

            // 1行目：名前（左）と総合の判定（右）。2行目：「効果」「能力」のタブ
            float headH = hud.LabelStyle.fontSize * 1.6f;
            DrawName(item, new Rect(area.x, area.y, area.width * 0.6f, headH));
            DrawVerdict(change, new Rect(area.x + area.width * 0.6f, area.y, area.width * 0.4f, headH), true);
            float tabH = hud.SmallStyle.fontSize * 2f;
            float tabW = area.width * 0.25f;
            var tabY = area.y + headH;
            hud.Button(new Rect(area.x, tabY, tabW, tabH), Loc.T("EFFECTS", "効果"), statsTab ? new Color(1f, 1f, 1f, 0.06f) : new Color(1f, 1f, 1f, 0.25f), hud.CenteredStyle, () => statsTab = false);
            hud.Button(new Rect(area.x + tabW + area.width * 0.02f, tabY, tabW, tabH), Loc.T("STATS", "能力"), statsTab ? new Color(1f, 1f, 1f, 0.25f) : new Color(1f, 1f, 1f, 0.06f), hud.CenteredStyle, () => statsTab = true);
            float top = tabY + tabH + area.height * 0.02f;
            var body = new Rect(area.x, top, area.width, area.yMax - top);
            if (statsTab)
            {
                DrawStatTable(world.Player, now, candidate, body);
            }
            else
            {
                DrawDescription(item, new Rect(body.x, body.y, body.width * 0.48f, body.height), world, false);
                DrawItemDiff(item, inv.Equipped[(int)slot], slot, new Rect(body.x + body.width * 0.5f, body.y, body.width * 0.5f, body.height));
            }

            float W = Screen.width;
            var centered = hud.CenteredStyle;
            string lockText = item.Locked ? Loc.T("UNLOCK", "ロック解除") : Loc.T("LOCK", "ロック");
            if (item.Slot == ItemSlot.Ring)
            {
                float w = (W - pad * 2f - gap * 3f) / 4f;
                if (canEquip)
                {
                    hud.Button(new Rect(pad, actionY, w, btnH), Loc.T("EQUIP R1", "指輪1へ"), buttonBack, centered, () => Equip(inv, item, EquipSlot.Ring1));
                    hud.Button(new Rect(pad + (w + gap), actionY, w, btnH), Loc.T("EQUIP R2", "指輪2へ"), buttonBack, centered, () => Equip(inv, item, EquipSlot.Ring2));
                }
                else
                {
                    Disabled(new Rect(pad, actionY, w * 2f + gap, btnH), reason);
                }
                hud.Button(new Rect(pad + (w + gap) * 2f, actionY, w, btnH), lockText, buttonBack, centered, () => item.Locked = !item.Locked);
                DrawSalvageButton(inv, item, new Rect(pad + (w + gap) * 3f, actionY, w, btnH));
            }
            else
            {
                float w = (W - pad * 2f - gap * 2f) / 3f;
                if (canEquip) hud.Button(new Rect(pad, actionY, w, btnH), Loc.T("EQUIP", "装備"), buttonBack, centered, () => Equip(inv, item, slot));
                else Disabled(new Rect(pad, actionY, w, btnH), reason);
                hud.Button(new Rect(pad + (w + gap), actionY, w, btnH), lockText, buttonBack, centered, () => item.Locked = !item.Locked);
                DrawSalvageButton(inv, item, new Rect(pad + (w + gap) * 2f, actionY, w, btnH));
            }
        }

        void DrawEquipped(Inventory inv, int slot, Rect area, float actionY, float btnH, float pad)
        {
            var item = inv.Equipped[slot];
            if (item == null)
            {
                GUI.Label(area, Loc.T($"{SlotLabel(slot)}: empty", $"{SlotLabel(slot)}：なし"), hud.SmallStyle);
                return;
            }
            DrawDescription(item, area, null, true);
            string text = inv.BagFull ? Loc.T("BAG FULL", "鞄が満杯") : Loc.T("UNEQUIP", "外す");
            hud.Button(new Rect(pad, actionY, Screen.width - pad * 2f, btnH), text, buttonBack, hud.CenteredStyle, () =>
            {
                if (inv.Unequip((EquipSlot)slot)) selectedEquip = -1;
            });
        }

        void Disabled(Rect r, string text)
        {
            Hud.Fill(r, new Color(0.5f, 0.1f, 0.1f, 0.35f));
            var style = new GUIStyle(hud.CenteredStyle);
            style.normal.textColor = new Color(1f, 0.6f, 0.6f);
            GUI.Label(r, text, style);
        }

        void DrawSalvageButton(Inventory inv, Item item, Rect r)
        {
            if (item.Locked)
            {
                Hud.Fill(r, new Color(1f, 1f, 1f, 0.05f));
                GUI.Label(r, Loc.T("LOCKED", "ロック中"), hud.CenteredStyle);
                return;
            }
            hud.Button(r, Loc.T($"SALVAGE +{Town.SalvageValue(item)}", $"分解 +{Town.SalvageValue(item)}"), buttonBack, hud.CenteredStyle, () => Salvage(inv, item));
        }

        /// <summary>一括分解の選択欄。ロック中の品は残る。</summary>
        void DrawBulkMenu(Inventory inv, PlayerStats current, Rect area, float btnH, float gap)
        {
            var small = hud.SmallStyle;
            float lineH = small.fontSize * 1.4f;
            GUI.Label(new Rect(area.x, area.y, area.width, lineH), Loc.T("Salvage all at once (locked items are kept):", "まとめて分解（ロック中の品は残ります）："), small);
            float y = area.y + lineH * 1.3f;
            float h = Mathf.Min(btnH, (area.height - lineH * 1.3f - gap * 6f) / 7f);

            void Option(string text, Color back, System.Func<Item, bool> match)
            {
                int count = 0;
                foreach (var it in inv.Bag) if (!it.Locked && match(it)) count++;
                hud.Button(new Rect(area.x, y, area.width, h), $"{text}   ({count})", back, hud.CenteredStyle, () =>
                {
                    SalvageBulk(inv, match);
                    bulkOpen = false;
                });
                y += h + gap;
            }

            for (int r = 0; r <= (int)Rarity.Relic; r++) // 白〜橙まで（赤＝神話は一括では分解しない）
            {
                int max = r;
                var c = RarityInfo.ColorOf((Rarity)r);
                Option(r == 0 ? Loc.T("White", "白だけ") : Loc.T($"White to {Short((Rarity)r).ToLower()}", $"白〜{Short((Rarity)r)}"), new Color(c.r, c.g, c.b, 0.3f), it => (int)it.Rarity <= max);
            }
            Option(Loc.T("Everything that is not an upgrade (up to orange)", "今より強くならない物すべて（橙まで）"), buttonBack, it => it.Rarity <= Rarity.Relic && !IsUpgrade(inv, it, current));
        }

        void SalvageBulk(Inventory inv, System.Func<Item, bool> match)
        {
            int n = inv.SalvageWhere(match);
            if (n > 0) hud.Toast(Loc.T($"Salvaged {n} items", $"{n}個を分解しました"), Color.white);
            if (selectedItem != null && !inv.Bag.Contains(selectedItem)) selectedItem = null;
        }

        static bool IsUpgrade(Inventory inv, Item item, PlayerStats current)
        {
            return BestCandidate(inv, item, out _).Power > current.Power * 1.01f;
        }

        static string Short(Rarity r)
        {
            switch (r)
            {
                case Rarity.Common: return Loc.T("WHITE", "白");
                case Rarity.Magic: return Loc.T("BLUE", "青");
                case Rarity.Rare: return Loc.T("YELLOW", "黄");
                case Rarity.Legendary: return Loc.T("PURPLE", "紫");
                case Rarity.Relic: return Loc.T("ORANGE", "橙");
                default: return Loc.T("RED", "赤");
            }
        }

        static string AutoLabel(int max)
        {
            if (max < 0) return Loc.T("AUTO: OFF", "自動分解：なし");
            return max == 0 ? Loc.T("AUTO: WHITE", "自動分解：白") : Loc.T($"AUTO: <= {Short((Rarity)max)}", $"自動分解：〜{Short((Rarity)max)}");
        }

        void Equip(Inventory inv, Item item, EquipSlot slot)
        {
            if (inv.Equip(item, slot)) selectedItem = null;
        }

        void Salvage(Inventory inv, Item item)
        {
            inv.Salvage(item);
            selectedItem = null;
        }

        void DrawCell(Rect r, Item item, bool selected, bool better, System.Action onTap)
        {
            var back = selected ? new Color(1f, 1f, 1f, 0.45f) : cellBack;
            if (item != null && !selected)
            {
                var c = RarityInfo.ColorOf(item.Rarity);
                back = new Color(c.r, c.g, c.b, 0.25f);
            }
            if (onTap != null) hud.Button(r, null, back, hud.CenteredStyle, onTap);
            else Hud.Fill(r, back);

            if (item == null) return;
            float inset = r.width * 0.15f;
            Hud.DrawSprite(new Rect(r.x + inset, r.y + inset, r.width - inset * 2f, r.height - inset * 2f), PixelArt.ItemIcon(item));
            Hud.DrawEnhance(r, item);
            Hud.DrawAbyssFrame(r, item);
            if (item.Locked)
            {
                var lockStyle = new GUIStyle(hud.SmallStyle) { fontStyle = FontStyle.Bold };
                lockStyle.normal.textColor = new Color(1f, 0.85f, 0.4f);
                GUI.Label(new Rect(r.x + r.width * 0.06f, r.y + r.height * 0.5f, r.width * 0.5f, r.height * 0.5f), "L", lockStyle);
            }
            if (better)
            {
                var style = new GUIStyle(hud.SmallStyle) { fontStyle = FontStyle.Bold };
                style.normal.textColor = up;
                GUI.Label(new Rect(r.x + r.width * 0.62f, r.y - r.height * 0.05f, r.width * 0.5f, r.height * 0.5f), "+", style);
            }
        }

        /// <summary>world を渡すと、満たしていない必要条件を赤で示す。</summary>
        void DrawDescription(Item item, Rect area, GameWorld world, bool withName)
        {
            float lineH = hud.SmallStyle.fontSize * 1.3f;
            float y = area.y;
            if (withName)
            {
                DrawName(item, new Rect(area.x, y, area.width, lineH * 1.3f));
                y += lineH * 1.4f;
            }

            // 必要条件
            var st = world != null ? world.Player.Stats : null;
            var req = new GUIStyle(hud.SmallStyle);
            bool lvOk = st == null || st.Level >= item.ReqLevel;
            req.normal.textColor = lvOk ? new Color(0.75f, 0.75f, 0.75f) : new Color(1f, 0.45f, 0.45f);
            string reqText = Loc.T($"Lv {item.Level}   Req Lv {item.ReqLevel}", $"Lv {item.Level}   必要Lv {item.ReqLevel}");
            if (item.NeedsAttr)
            {
                bool attrOk = st == null || st.Attr[(int)item.ReqAttr] >= item.ReqValue;
                if (!attrOk) req.normal.textColor = new Color(1f, 0.45f, 0.45f);
                reqText += $"   {Attributes.ShortName(item.ReqAttr)} {item.ReqValue}";
            }
            req.wordWrap = true;
            float rh = req.CalcHeight(new GUIContent(reqText), area.width);
            GUI.Label(new Rect(area.x, y, area.width, rh), reqText, req);
            y += rh;

            var text = new GUIStyle(hud.SmallStyle) { wordWrap = true };
            foreach (var line in item.Describe())
            {
                float h = text.CalcHeight(new GUIContent(line), area.width);
                if (y + h > area.yMax) break;
                GUI.Label(new Rect(area.x, y, area.width, h), line, text);
                y += h;
            }

            // 深淵のセット：そろえた数（今の装備）と、2・3・5つの効果（効いているものは明るく）
            var set = AbyssSets.SetOf(item);
            if (set == UniqueType.None || world == null) return;
            int have = AbyssSets.Count(world.Inventory.Equipped, set);
            var sc = AbyssSets.ColorOf(set);
            var head = new GUIStyle(text) { fontStyle = FontStyle.Bold };
            head.normal.textColor = sc;
            string title = Loc.T($"{AbyssSets.Name(set)} set  {have}/5 equipped", $"{AbyssSets.Name(set)}のセット　装備中 {have}/5");
            float th = head.CalcHeight(new GUIContent(title), area.width);
            if (y + th > area.yMax) return;
            y += lineH * 0.3f;
            GUI.Label(new Rect(area.x, y, area.width, th), title, head);
            y += th;
            foreach (int tier in AbyssSets.Tiers)
            {
                bool on = have >= tier;
                var ts = new GUIStyle(text);
                ts.normal.textColor = on ? Color.Lerp(sc, Color.white, 0.5f) : new Color(0.55f, 0.55f, 0.6f);
                string l = Loc.T($"({tier}) ", $"（{tier}つ）") + AbyssSets.BonusText(set, tier);
                float h = ts.CalcHeight(new GUIContent(l), area.width);
                if (y + h > area.yMax) break;
                GUI.Label(new Rect(area.x, y, area.width, h), l, ts);
                y += h;
            }
        }

        static readonly string[] slotNames = { "Weapon", "Lantern", "Armor", "R1", "R2" };
        static readonly string[] slotNamesJa = { "武器", "ランタン", "防具", "指輪1", "指輪2" };
        static readonly string[] slotLabelsJa = { "武器", "灯", "防具", "指1", "指2" };

        static string SlotName(int i) => Loc.Ja ? slotNamesJa[i] : slotNames[i];
        static string SlotLabel(int i) => Loc.Ja ? slotLabelsJa[i] : slotLabels[i];

        /// <summary>
        /// 装備どうしを効果ごとに比べる（今その欄に付けている装備との差）。
        /// 例：+39% Damage と +20% Damage → Damage +19%。片方にしかない効果はそのまま足し引き。
        /// </summary>
        void DrawName(Item item, Rect r)
        {
            var title = new GUIStyle(hud.LabelStyle) { fontStyle = FontStyle.Bold };
            title.normal.textColor = RarityInfo.ColorOf(item.Rarity);
            Hud.FitLabel(r, item.Name, title); // 長い名前は小さくして、右の判定と重ならないように
        }

        /// <summary>総合の判定（強化なしの能力で、付けたら全体でどれだけ強くなるか）。比較の列の先頭。</summary>
        void DrawVerdict(float change, Rect r, bool right = false)
        {
            var verdict = new GUIStyle(hud.LabelStyle) { fontStyle = FontStyle.Bold, alignment = right ? TextAnchor.UpperRight : TextAnchor.UpperLeft };
            string text;
            if (change > 0.01f)
            {
                text = Loc.T($"UPGRADE +{change * 100f:0}%", $"強くなる +{change * 100f:0}%");
                verdict.normal.textColor = up;
            }
            else if (change < -0.01f)
            {
                text = Loc.T($"DOWNGRADE {change * 100f:0}%", $"弱くなる {change * 100f:0}%");
                verdict.normal.textColor = down;
            }
            else
            {
                text = Loc.T("ABOUT THE SAME", "ほぼ同じ");
                verdict.normal.textColor = Color.white;
            }
            Hud.FitLabel(r, text, verdict);
        }

        /// <summary>
        /// 「能力」のタブ：今の能力と、この品を付けたときの能力を並べる（能力値の画面と同じ項目。加護も込み）。
        /// 増える値は緑、減る値は赤。
        /// </summary>
        void DrawStatTable(Player player, PlayerStats nowBase, PlayerStats afterBase, Rect area)
        {
            var now = WithBoons(nowBase, player);
            var after = WithBoons(afterBase, player);
            var rows = new List<(string name, float a, float b, string fmt, string unit)>
            {
                ("HP", now.MaxHp, after.MaxHp, "0", ""),
                ("MP", now.MaxMp, after.MaxMp, "0", ""),
                (Loc.T("Damage", "ダメージ"), now.Damage, after.Damage, "0", ""),
                (Loc.T("Attacks/s", "攻撃/秒"), now.AttacksPerSecond, after.AttacksPerSecond, "0.00", ""),
                ("DPS", Dps(now), Dps(after), "0", ""),
                (Loc.T("Crit", "会心"), now.CritChance * 100f, after.CritChance * 100f, "0", "%"),
                (Loc.T("Crit dmg", "会心ダメージ"), now.CritMult * 100f, after.CritMult * 100f, "0", "%"),
                (Loc.T("Armor", "防御"), now.Armor, after.Armor, "0", ""),
                (Loc.T("Speed", "速さ"), now.MoveSpeed, after.MoveSpeed, "0.0", ""),
                (Loc.T("Light", "明るさ"), now.Light, after.Light, "0.0", ""),
                (Loc.T("HP regen", "HP回復"), now.HpRegen, after.HpRegen, "0.0", ""),
                (Loc.T("MP regen", "MP回復"), now.MpRegen, after.MpRegen, "0.0", ""),
            };
            // 能力値は変わるものだけを、最後の1行にまとめる（品によって行の数・高さが変わらないように）
            var attrLine = new System.Text.StringBuilder();
            for (int i = 0; i < Attributes.Count; i++)
            {
                int d = after.Attr[i] - now.Attr[i];
                if (d == 0) continue;
                if (attrLine.Length > 0) attrLine.Append("   ");
                attrLine.Append(Attributes.ShortName((Attr)i)).Append(d > 0 ? " +" : " ").Append(d);
            }

            var small = hud.SmallStyle;
            // 行の高さはいつも同じ（見出し＋12項目＋能力値の1行）
            float rowH = Mathf.Min(small.fontSize * 1.45f, area.height / (rows.Count + 2));
            var head = new GUIStyle(small) { alignment = TextAnchor.MiddleRight };
            head.normal.textColor = new Color(0.65f, 0.65f, 0.7f);
            float c0 = area.width * 0.36f, c1 = area.width * 0.2f, c2 = area.width * 0.22f, c3 = area.width * 0.22f;
            GUI.Label(new Rect(area.x + c0, area.y, c1, rowH), Loc.T("Now", "今"), head);
            GUI.Label(new Rect(area.x + c0 + c1, area.y, c2, rowH), Loc.T("Equipped", "付けたら"), head);
            float y = area.y + rowH;
            var nameStyle = new GUIStyle(small) { alignment = TextAnchor.MiddleLeft };
            nameStyle.normal.textColor = new Color(0.8f, 0.8f, 0.85f);
            var val = new GUIStyle(small) { alignment = TextAnchor.MiddleRight };
            var diff = new GUIStyle(small) { alignment = TextAnchor.MiddleRight, fontStyle = FontStyle.Bold };
            for (int i = 0; i < rows.Count && y + rowH <= area.yMax; i++)
            {
                var r = rows[i];
                if (i % 2 == 0) Hud.Fill(new Rect(area.x, y, area.width, rowH), new Color(1f, 1f, 1f, 0.04f));
                GUI.Label(new Rect(area.x + area.width * 0.02f, y, c0, rowH), r.name, nameStyle);
                val.normal.textColor = Color.white;
                GUI.Label(new Rect(area.x + c0, y, c1, rowH), r.a.ToString(r.fmt) + r.unit, val);
                float d = r.b - r.a;
                bool same = Mathf.Abs(d) < (r.fmt == "0" ? 0.5f : r.fmt == "0.0" ? 0.05f : 0.005f);
                val.normal.textColor = same ? Color.white : d > 0f ? up : down;
                GUI.Label(new Rect(area.x + c0 + c1, y, c2, rowH), r.b.ToString(r.fmt) + r.unit, val);
                if (!same)
                {
                    diff.normal.textColor = d > 0f ? up : down;
                    GUI.Label(new Rect(area.x + c0 + c1 + c2, y, c3 - area.width * 0.02f, rowH), (d > 0f ? "+" : "") + d.ToString(r.fmt) + r.unit, diff);
                }
                y += rowH;
            }
            if (attrLine.Length > 0 && y + rowH <= area.yMax + 0.5f)
            {
                var attrStyle = new GUIStyle(small) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };
                attrStyle.normal.textColor = new Color(1f, 0.85f, 0.5f);
                Hud.FitLabel(new Rect(area.x + area.width * 0.02f, y, area.width * 0.96f, rowH), attrLine.ToString(), attrStyle);
            }
        }

        static PlayerStats WithBoons(PlayerStats s, Player player)
        {
            foreach (int b in player.Boons) s = s.WithBoon(Boons.Get(b));
            return s;
        }

        static float Dps(PlayerStats s) => s.Damage * s.AttacksPerSecond * (1f + s.EffectiveCrit * (s.CritMult - 1f));

        void DrawItemDiff(Item item, Item equipped, EquipSlot slot, Rect area)
        {
            float lineH = hud.SmallStyle.fontSize * 1.3f;
            float y = area.y;
            GUI.Label(new Rect(area.x, y, area.width, lineH), equipped == null ? Loc.T($"vs {SlotName((int)slot)}: empty", $"{SlotName((int)slot)}と比べて：なし") : Loc.T($"vs {SlotName((int)slot)}:", $"{SlotName((int)slot)}と比べて："), hud.SmallStyle);
            y += lineH * 1.2f;

            var a = Contributions(item);
            var b = equipped != null ? Contributions(equipped) : new Dictionary<string, float>();

            // 武器の種類が違えば、攻撃の形が変わる
            if (item.Slot == ItemSlot.Weapon && equipped != null && equipped.Weapon != item.Weapon)
            {
                DiffText(ref y, area, $"{ClassInfo.Weapon(equipped.Weapon).DisplayName} > {ClassInfo.Weapon(item.Weapon).DisplayName}", Color.white);
            }

            var keys = new List<string>(a.Keys);
            foreach (var k in b.Keys) if (!keys.Contains(k)) keys.Add(k);
            keys.Sort(string.CompareOrdinal);
            bool any = false;
            foreach (var k in keys)
            {
                a.TryGetValue(k, out float va);
                b.TryGetValue(k, out float vb);
                float d = va - vb;
                if (Mathf.Abs(d) < 0.0001f) continue;
                any = true;
                DiffText(ref y, area, FormatDiff(k, d), d > 0f ? up : down);
            }

            // 固有効果：得るもの／失うもの
            foreach (var u in item.Uniques)
            {
                if (equipped != null && equipped.Uniques.Contains(u)) continue;
                any = true;
                DiffText(ref y, area, "+ " + StatText.Unique(u), up);
            }
            if (equipped != null)
            {
                foreach (var u in equipped.Uniques)
                {
                    if (item.Uniques.Contains(u)) continue;
                    any = true;
                    DiffText(ref y, area, "- " + StatText.Unique(u), down);
                }
            }
            if (!any) DiffText(ref y, area, Loc.T("(same effects)", "（効果は同じ）"), Color.white);
        }

        /// <summary>長い行は折り返して書く。</summary>
        void DiffText(ref float y, Rect area, string text, Color color)
        {
            var style = new GUIStyle(hud.SmallStyle) { wordWrap = true };
            style.normal.textColor = color;
            float h = style.CalcHeight(new GUIContent(text), area.width);
            if (y + h > area.yMax) return;
            GUI.Label(new Rect(area.x, y, area.width, h), text, style);
            y += h;
        }

        /// <summary>装備1つの効果を「効果の名前 → 値」にまとめる（基本の数値も含む）。</summary>
        static Dictionary<string, float> Contributions(Item it)
        {
            var map = new Dictionary<string, float>();
            switch (it.Slot)
            {
                case ItemSlot.Weapon: map["0 Damage"] = it.EffBase; break;
                case ItemSlot.Lantern: map["0 Light"] = it.EffBase; break;
                case ItemSlot.Armor: map["0 Max HP"] = it.EffBase; break;
            }
            foreach (var af in it.Affixes)
            {
                string key = "1 " + af.Stat;
                map.TryGetValue(key, out float v);
                map[key] = v + it.Eff(af);
            }
            return map;
        }

        static string FormatDiff(string key, float d)
        {
            string sign = d > 0f ? "+" : "-";
            float v = Mathf.Abs(d);
            switch (key)
            {
                case "0 Damage": return Loc.T($"Weapon damage {sign}{v:0}", $"武器のダメージ {sign}{v:0}");
                case "0 Light": return Loc.T($"Light {sign}{v:0.0}", $"明るさ {sign}{v:0.0}");
                case "0 Max HP": return Loc.T($"Max HP {sign}{v:0}", $"最大HP {sign}{v:0}");
            }
            var stat = (StatType)System.Enum.Parse(typeof(StatType), key.Substring(2));
            // 効果の書き方（+12% Damage など）の先頭の + を、増減の符号に置き換える
            string text = StatText.Format(stat, v);
            // 数字の前の符号を探す。「+」なら増減の符号に、「-」（被ダメージ・待ち時間の短縮など）なら逆向きにする
            for (int i = 0; i < text.Length - 1; i++)
            {
                char c = text[i];
                if ((c != '+' && c != '-') || !char.IsDigit(text[i + 1])) continue;
                string mark = c == '+' ? sign : (d > 0f ? "-" : "+");
                return text.Substring(0, i) + mark + text.Substring(i + 1);
            }
            return sign + text;
        }

        /// <summary>付けたときに一番強くなる装備欄での能力（指輪は R1・R2 の良い方）。</summary>
        static PlayerStats BestCandidate(Inventory inv, Item item, out EquipSlot slot)
        {
            if (item.Slot != ItemSlot.Ring)
            {
                slot = Inventory.DefaultSlot(item);
                return PlayerStats.From(inv.With(item, slot));
            }
            var a = PlayerStats.From(inv.With(item, EquipSlot.Ring1));
            var b = PlayerStats.From(inv.With(item, EquipSlot.Ring2));
            if (b.Power > a.Power)
            {
                slot = EquipSlot.Ring2;
                return b;
            }
            slot = EquipSlot.Ring1;
            return a;
        }

        GUIStyle CenteredSmall()
        {
            return new GUIStyle(hud.SmallStyle) { alignment = TextAnchor.MiddleCenter };
        }
    }
}
