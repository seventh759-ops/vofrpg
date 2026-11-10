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
        int selectedEquip = -1;

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
            GUI.Label(new Rect(pad, pad, W, btnH), $"BAG {inv.Bag.Count}/{Inventory.BagCapacity}   EMBERS {inv.Embers}", hud.LabelStyle);
            hud.Button(new Rect(W - pad - W * 0.22f, pad, W * 0.22f, btnH), "CLOSE", buttonBack, centered, () => world.BagOpen = false);

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
                GUI.Label(new Rect(r.x, r.yMax, r.width, lineH), slotLabels[i], CenteredSmall());
            }

            // 鞄
            y += cell + lineH + H * 0.015f;
            const int cols = 6;
            float bcell = (W - pad * 2f - gap * (cols - 1)) / cols;
            var current = world.Player.Stats;
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

            // 下のボタン：一括分解（ロック中の品は残る）と自動分解の段階
            float bottomY = H - pad - btnH;
            float rowY = bottomY - btnH - gap;
            float third = (W - pad * 2f - gap * 2f) / 3f;
            float half = (W - pad * 2f - gap) * 0.5f;
            for (int r = 0; r <= (int)Rarity.Rare; r++)
            {
                int max = r;
                var c = RarityInfo.ColorOf((Rarity)r);
                hud.Button(new Rect(pad + (third + gap) * r, rowY, third, btnH), r == 0 ? "SALVAGE WHITE" : $"SALVAGE <= {Short((Rarity)r)}",
                    new Color(c.r, c.g, c.b, 0.3f), centered, () => SalvageBulk(inv, it => (int)it.Rarity <= max));
            }
            hud.Button(new Rect(pad, bottomY, half, btnH), "SALVAGE NO \"+\"", buttonBack, centered,
                () => SalvageBulk(inv, it => it.Rarity < Rarity.Legendary && !IsUpgrade(inv, it, current)));
            hud.Button(new Rect(pad + half + gap, bottomY, half, btnH), AutoLabel(inv.AutoSalvageMax), buttonBack, centered,
                () => inv.CycleAutoSalvage());

            // 選んだ品の詳細と操作
            float actionY = rowY - btnH - gap;
            var detail = new Rect(pad, y, W - pad * 2f, actionY - gap - y);
            if (selectedItem != null) DrawBagItem(world, inv, selectedItem, detail, actionY, btnH, gap, pad);
            else if (selectedEquip >= 0) DrawEquipped(inv, selectedEquip, detail, actionY, btnH, pad);
            else GUI.Label(detail, "Tap an item to see details.", small);
        }

        void DrawBagItem(GameWorld world, Inventory inv, Item item, Rect area, float actionY, float btnH, float gap, float pad)
        {
            bool canEquip = world.CanEquip(item, out var reason);
            DrawDescription(item, new Rect(area.x, area.y, area.width * 0.55f, area.height), world);

            var candidate = BestCandidate(inv, item, out var slot);
            DrawComparison(world.Player.Stats, candidate, new Rect(area.x + area.width * 0.57f, area.y, area.width * 0.43f, area.height));

            float W = Screen.width;
            var centered = hud.CenteredStyle;
            string lockText = item.Locked ? "UNLOCK" : "LOCK";
            if (item.Slot == ItemSlot.Ring)
            {
                float w = (W - pad * 2f - gap * 3f) / 4f;
                if (canEquip)
                {
                    hud.Button(new Rect(pad, actionY, w, btnH), "EQUIP R1", buttonBack, centered, () => Equip(inv, item, EquipSlot.Ring1));
                    hud.Button(new Rect(pad + (w + gap), actionY, w, btnH), "EQUIP R2", buttonBack, centered, () => Equip(inv, item, EquipSlot.Ring2));
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
                if (canEquip) hud.Button(new Rect(pad, actionY, w, btnH), "EQUIP", buttonBack, centered, () => Equip(inv, item, slot));
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
                GUI.Label(area, $"{slotLabels[slot]}: empty", hud.SmallStyle);
                return;
            }
            DrawDescription(item, area, null);
            string text = inv.BagFull ? "BAG FULL" : "UNEQUIP";
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
                GUI.Label(r, "LOCKED", hud.CenteredStyle);
                return;
            }
            hud.Button(r, $"SALVAGE +{RarityInfo.SalvageOf(item.Rarity)}", buttonBack, hud.CenteredStyle, () => Salvage(inv, item));
        }

        void SalvageBulk(Inventory inv, System.Func<Item, bool> match)
        {
            int n = inv.SalvageWhere(match);
            if (n > 0) hud.Toast($"Salvaged {n} items", Color.white);
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
                case Rarity.Common: return "WHITE";
                case Rarity.Magic: return "BLUE";
                case Rarity.Rare: return "YELLOW";
                case Rarity.Legendary: return "ORANGE";
                case Rarity.Relic: return "PURPLE";
                default: return "RED";
            }
        }

        static string AutoLabel(int max)
        {
            if (max < 0) return "AUTO SALVAGE: OFF";
            return max == 0 ? "AUTO SALVAGE: WHITE" : $"AUTO SALVAGE: <= {Short((Rarity)max)}";
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
            Hud.DrawSprite(new Rect(r.x + inset, r.y + inset, r.width - inset * 2f, r.height - inset * 2f), PixelArt.ItemIcon(item.Slot, item.Rarity));
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
        void DrawDescription(Item item, Rect area, GameWorld world)
        {
            float lineH = hud.SmallStyle.fontSize * 1.3f;
            var title = new GUIStyle(hud.LabelStyle) { fontStyle = FontStyle.Bold };
            title.normal.textColor = RarityInfo.ColorOf(item.Rarity);
            GUI.Label(new Rect(area.x, area.y, area.width, lineH * 1.3f), item.Name, title);

            float y = area.y + lineH * 1.4f;

            // 必要条件
            var st = world != null ? world.Player.Stats : null;
            var req = new GUIStyle(hud.SmallStyle);
            bool lvOk = st == null || st.Level >= item.ReqLevel;
            req.normal.textColor = lvOk ? new Color(0.75f, 0.75f, 0.75f) : new Color(1f, 0.45f, 0.45f);
            string reqText = $"Lv {item.Level}   Req Lv {item.ReqLevel}";
            if (item.ReqValue > 0)
            {
                bool attrOk = st == null || st.Attr[(int)item.ReqAttr] >= item.ReqValue;
                if (!attrOk) req.normal.textColor = new Color(1f, 0.45f, 0.45f);
                reqText += $"   {Attributes.ShortName(item.ReqAttr)} {item.ReqValue}";
            }
            GUI.Label(new Rect(area.x, y, area.width, lineH), reqText, req);
            y += lineH;

            foreach (var line in item.Describe())
            {
                if (y + lineH > area.yMax) break;
                GUI.Label(new Rect(area.x, y, area.width, lineH), line, hud.SmallStyle);
                y += lineH;
            }
        }

        void DrawComparison(PlayerStats now, PlayerStats next, Rect area)
        {
            float lineH = hud.SmallStyle.fontSize * 1.3f;
            float y = area.y;
            GUI.Label(new Rect(area.x, y, area.width, lineH), "If equipped:", hud.SmallStyle);
            y += lineH * 1.2f;

            float power = now.Power > 0f ? next.Power / now.Power - 1f : 0f;
            Line(ref y, area, "POWER", power * 100f, "0", "%", true);
            Line(ref y, area, "DMG", next.Damage - now.Damage, "0.0", "", false);
            Line(ref y, area, "ATK/s", next.AttacksPerSecond - now.AttacksPerSecond, "0.0", "", false);
            Line(ref y, area, "SHOTS", next.Shots - now.Shots, "0", "", false);
            Line(ref y, area, "CRIT", (next.CritChance - now.CritChance) * 100f, "0", "%", false);
            Line(ref y, area, "HP", next.MaxHp - now.MaxHp, "0", "", false);
            Line(ref y, area, "ARMOR", next.Armor - now.Armor, "0", "", false);
            Line(ref y, area, "LIGHT", next.Light - now.Light, "0.0", "", false);
            Line(ref y, area, "SPEED", (next.MoveSpeed / now.MoveSpeed - 1f) * 100f, "0", "%", false);
        }

        void Line(ref float y, Rect area, string name, float delta, string format, string unit, bool always)
        {
            if (!always && Mathf.Abs(delta) < 0.05f) return;
            float lineH = hud.SmallStyle.fontSize * 1.3f;
            if (y + lineH > area.yMax) return;
            var style = new GUIStyle(hud.SmallStyle);
            style.normal.textColor = delta > 0.05f ? up : delta < -0.05f ? down : Color.white;
            string sign = delta > 0f ? "+" : "";
            GUI.Label(new Rect(area.x, y, area.width, lineH), $"{name} {sign}{delta.ToString(format)}{unit}", style);
            y += lineH;
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
