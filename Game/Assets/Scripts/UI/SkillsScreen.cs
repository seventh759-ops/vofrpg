using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// スキルの画面（開いている間はゲームが止まる）。職業の6つのスキルと、覚えるレベル・スキルレベル・熟練度。
    /// スキルの行をタップして選び、いちばん下のボタン（1・2・3）をタップして付ける。同じスキルのボタンをもう一度タップすると外す。
    /// </summary>
    public sealed class SkillsScreen
    {
        static readonly Color panel = new Color(0f, 0f, 0f, 0.9f);
        static readonly Color buttonBack = new Color(1f, 1f, 1f, 0.15f);

        readonly Hud hud;
        float scroll; // スキルの行のスクロール量（ピクセル）
        int selected = -1; // 選んでいるスキル（下のボタンに付けるため）。-1 は選んでいない

        public SkillsScreen(Hud hud)
        {
            this.hud = hud;
        }

        public void Draw(GameWorld world)
        {
            var ch = Profile.Current;
            if (ch == null) return;

            float W = Screen.width;
            float H = Screen.height;
            float pad = W * 0.05f;
            float btnH = H * 0.045f;
            var label = hud.LabelStyle;
            var small = hud.SmallStyle;
            var centered = hud.CenteredStyle;

            Hud.Fill(new Rect(0, 0, W, H), panel);

            // いちばん下に、スキルのボタン（1・2・3）の枠を固定で置く。その上がスキルの一覧（スクロール）
            float slotS = Mathf.Min(W * 0.17f, H * 0.085f);
            float hintH = H * 0.01f; // 一覧と下のボタンの間のすき間（説明は初めて開いたときのヒントで出す）
            float slotTop = H - pad * 0.6f - slotS;
            float viewTop = pad + btnH + H * 0.015f, viewBottom = slotTop - hintH - H * 0.01f;
            float y = viewTop;
            // 出すのは、今の職業で覚えられるスキルだけ（上位職・3次職のスキルは、転職・転生したときだけ）
            var visible = new System.Collections.Generic.List<int>();
            for (int i = 0; i < SkillInfo.PerClass; i++)
            {
                if (i == AdvancedClasses.SkillIndex && ch.Advanced < 0) continue;
                if (i == ThirdClasses.SkillIndex && !ch.Third) continue;
                visible.Add(i);
            }
            if (selected >= 0 && !SkillInfo.Learned(ClassInfo.Current, selected, ch.Level)) selected = -1;
            // 説明と MP などの行は、経験値のバーをアイコンの下に移した分だけ文字を大きくする
            var text = new GUIStyle(small) { fontSize = Mathf.RoundToInt(small.fontSize * 1.15f) };
            float lineH = text.fontSize * 1.3f;
            // 行の高さは中身（名前・説明・MP などの3行）にぴったり合わせる。入りきらなければスクロールする
            float rowH = lineH * 3.35f / 0.88f;
            float rowGap = H * 0.008f;
            float contentH = visible.Count * (rowH + rowGap);
            float maxScroll = Mathf.Max(0f, contentH - (viewBottom - viewTop));
            scroll = Mathf.Clamp(scroll + ScrollInput.Delta(), 0f, maxScroll);
            y = viewTop - scroll;
            var slotTag = new GUIStyle(centered) { fontStyle = FontStyle.Bold };

            foreach (int i in visible)
            {
                if (y + rowH < viewTop || y > viewBottom) { y += rowH + rowGap; continue; } // 見えない行は描かない
                int index = i;
                var spec = SkillInfo.Get(ClassInfo.Current, i);
                bool learned = SkillInfo.Learned(ClassInfo.Current, i, ch.Level);
                bool isSelected = selected == i;
                var row = new Rect(pad, y, W - pad * 2f, rowH);
                Hud.Fill(row, new Color(spec.Color.r, spec.Color.g, spec.Color.b, isSelected ? 0.35f : learned ? 0.12f : 0.04f));
                if (isSelected) Frame(row, new Color(1f, 0.85f, 0.4f, 0.95f), Mathf.Max(2f, rowH * 0.03f));

                // 行をタップしてスキルを選ぶ（もう一度で選ぶのをやめる）。見えている部分だけ押せる
                if (learned)
                {
                    float top = Mathf.Max(row.y, viewTop), bottom = Mathf.Min(row.yMax, viewBottom);
                    if (bottom > top) hud.Button(new Rect(row.x, top, row.width, bottom - top), null, new Color(0f, 0f, 0f, 0f), centered,
                        () => selected = selected == index ? -1 : index);
                }

                float inner = rowH * 0.06f;

                // アイコン（左）と、その下に熟練度（使った回数）のバー。文字はその右に並べる
                float barH = Mathf.Max(3f, rowH * 0.07f);
                float iconSize = Mathf.Min(rowH - inner * 3f - barH, W * 0.15f);
                var iconRect = new Rect(row.x + inner, row.y + inner, iconSize, iconSize);
                var old = GUI.color;
                GUI.color = learned ? Color.white : new Color(1f, 1f, 1f, 0.3f);
                Hud.DrawSprite(iconRect, PixelArt.SkillIcon(spec));
                GUI.color = old;
                int lv = ch.SkillLv[i];
                if (learned)
                {
                    var bar = new Rect(iconRect.x, iconRect.yMax + inner, iconSize, barH);
                    Hud.Fill(bar, new Color(1f, 1f, 1f, 0.12f));
                    float ratio = lv >= SkillInfo.MaxSkillLevel ? 1f : Mathf.Clamp01((float)ch.SkillUses[i] / SkillInfo.UsesToNext(lv));
                    Hud.Fill(new Rect(bar.x, bar.y, bar.width * ratio, bar.height), new Color(0.6f, 0.85f, 1f, 0.9f));
                }
                row = new Rect(row.x + iconSize + inner * 2f, row.y, row.width - iconSize - inner * 2f, row.height);

                // 付いているボタンの番号（右上に小さな札）
                int at = System.Array.IndexOf(ch.Slots, i);
                float tagS = lineH * 1.1f;
                if (at >= 0)
                {
                    var tag = new Rect(row.xMax - inner - tagS, row.y + inner, tagS, tagS);
                    Hud.Fill(tag, new Color(0.3f, 0.6f, 1f, 0.85f));
                    GUI.Label(tag, (at + 1).ToString(), slotTag);
                }

                var nameStyle = new GUIStyle(label) { fontStyle = FontStyle.Bold };
                nameStyle.normal.textColor = learned ? Color.white : new Color(0.55f, 0.55f, 0.55f);
                string header = learned ? $"{spec.DisplayName}   Lv {lv}" : Loc.T($"{spec.DisplayName}   (learn at Lv {spec.ReqLevel})", $"{spec.DisplayName}   （Lv {spec.ReqLevel}で覚える）");
                float textW = row.width - inner * 2f;
                Hud.FitLabel(new Rect(row.x + inner, row.y + inner, textW - (at >= 0 ? tagS + inner : 0f), lineH * 1.2f), header, nameStyle);

                text.normal.textColor = learned ? new Color(0.85f, 0.85f, 0.85f) : new Color(0.5f, 0.5f, 0.5f);
                float ty = row.y + inner + lineH * 1.25f;
                Hud.FitLabel(new Rect(row.x + inner, ty, textW, lineH), spec.DisplayDesc, text);
                ty += lineH;
                float cd = spec.Cooldown * SkillInfo.CooldownMult(lv);
                string power = Loc.T($"Power x{SkillInfo.Power(lv):0.0}", $"威力 x{SkillInfo.Power(lv):0.0}");
                Hud.FitLabel(new Rect(row.x + inner, ty, textW, lineH), Loc.T($"MP {spec.Mp}   Cooldown {cd:0.0}s   {power}", $"MP {spec.Mp}   待ち時間 {cd:0.0}秒   {power}"), text);

                y += rowH + rowGap;
            }

            // はみ出した行を隠すため、上下を塗り直してから見出し・「閉じる」・下のボタンの枠を描く
            var cover = new Color(0.02f, 0.02f, 0.03f, 1f);
            Hud.Fill(new Rect(0, 0, W, viewTop), cover);
            Hud.Fill(new Rect(0, viewBottom, W, H - viewBottom), cover);
            var title = new GUIStyle(label) { fontStyle = FontStyle.Bold };
            GUI.Label(new Rect(pad, pad, W, btnH), Loc.T($"SKILLS   Lv {ch.Level}", $"スキル   Lv {ch.Level}"), title);
            hud.Button(new Rect(W - pad - W * 0.22f, pad, W * 0.22f, btnH), Loc.T("CLOSE", "閉じる"), buttonBack, centered, () => world.OpenPanel(false, false, false));

            // スクロールできるときは、右端に今の位置を細いバーで出す
            if (maxScroll > 0f)
            {
                float viewH = viewBottom - viewTop;
                float sbH = viewH * viewH / contentH;
                float sbY = viewTop + (viewH - sbH) * (scroll / maxScroll);
                Hud.Fill(new Rect(W - pad * 0.4f, sbY, pad * 0.15f, sbH), new Color(1f, 1f, 1f, 0.35f));
            }

            // スキルのボタン（1・2・3）。選んだスキルを付ける／同じスキルなら外す。何も選んでいなければ、付いているスキルを選ぶ
            float slotGap = slotS * 0.3f;
            float sx = (W - slotS * SkillInfo.Slots - slotGap * (SkillInfo.Slots - 1)) * 0.5f;
            var numStyle = new GUIStyle(small) { alignment = TextAnchor.UpperLeft, fontStyle = FontStyle.Bold };
            for (int s2 = 0; s2 < SkillInfo.Slots; s2++)
            {
                int slot = s2;
                var r = new Rect(sx + s2 * (slotS + slotGap), slotTop, slotS, slotS);
                int cur = ch.Slots[s2];
                bool target = selected >= 0;
                hud.Button(r, null, target ? new Color(0.3f, 0.6f, 1f, 0.35f) : new Color(1f, 1f, 1f, 0.1f), centered, () =>
                {
                    if (selected >= 0)
                    {
                        Assign(ch, slot, selected);
                        selected = -1;
                    }
                    else if (cur >= 0) selected = cur;
                });
                if (target) Frame(r, new Color(1f, 0.85f, 0.4f, 0.9f), Mathf.Max(2f, slotS * 0.03f));
                if (cur >= 0 && SkillInfo.Learned(ClassInfo.Current, cur, ch.Level))
                {
                    float ins = slotS * 0.12f;
                    Hud.DrawSprite(new Rect(r.x + ins, r.y + ins, slotS - ins * 2f, slotS - ins * 2f), PixelArt.SkillIcon(SkillInfo.Get(ClassInfo.Current, cur)));
                }
                GUI.Label(new Rect(r.x + slotS * 0.06f, r.y + slotS * 0.02f, slotS, slotS), (s2 + 1).ToString(), numStyle);
            }
        }

        /// <summary>枠線を描く。</summary>
        static void Frame(Rect r, Color c, float t)
        {
            Hud.Fill(new Rect(r.x, r.y, r.width, t), c);
            Hud.Fill(new Rect(r.x, r.yMax - t, r.width, t), c);
            Hud.Fill(new Rect(r.x, r.y, t, r.height), c);
            Hud.Fill(new Rect(r.xMax - t, r.y, t, r.height), c);
        }

        /// <summary>スキルをボタンに付ける。同じスキルが別のボタンにあれば、そちらは空ける。</summary>
        static void Assign(CharacterData ch, int slot, int index)
        {
            if (ch.Slots[slot] == index)
            {
                ch.Slots[slot] = -1; // もう一度押すと外す
            }
            else
            {
                for (int s = 0; s < ch.Slots.Length; s++)
                {
                    if (ch.Slots[s] == index) ch.Slots[s] = -1;
                }
                ch.Slots[slot] = index;
            }
            Profile.Save();
        }
    }
}
