using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// スキルの画面（開いている間はゲームが止まる）。職業の6つのスキルと、覚えるレベル・スキルレベル・熟練度。
    /// 覚えたスキルを3つのボタンのどれに付けるかを選ぶ。
    /// </summary>
    public sealed class SkillsScreen
    {
        static readonly Color panel = new Color(0f, 0f, 0f, 0.9f);
        static readonly Color buttonBack = new Color(1f, 1f, 1f, 0.15f);

        readonly Hud hud;
        float scroll; // スキルの行のスクロール量（ピクセル）

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

            float viewTop = pad + btnH + H * 0.015f, viewBottom = H - pad * 0.5f;
            float y = viewTop;
            // 出すのは、今の職業で覚えられるスキルだけ（上位職・3次職のスキルは、転職・転生したときだけ）
            var visible = new System.Collections.Generic.List<int>();
            for (int i = 0; i < SkillInfo.PerClass; i++)
            {
                if (i == AdvancedClasses.SkillIndex && ch.Advanced < 0) continue;
                if (i == ThirdClasses.SkillIndex && !ch.Third) continue;
                visible.Add(i);
            }
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

            foreach (int i in visible)
            {
                if (y + rowH < viewTop || y > viewBottom) { y += rowH + rowGap; continue; } // 見えない行は描かない
                int index = i;
                var spec = SkillInfo.Get(ClassInfo.Current, i);
                bool learned = SkillInfo.Learned(ClassInfo.Current, i, ch.Level);
                var row = new Rect(pad, y, W - pad * 2f, rowH);
                Hud.Fill(row, new Color(spec.Color.r, spec.Color.g, spec.Color.b, learned ? 0.12f : 0.04f));

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
                var nameStyle = new GUIStyle(label) { fontStyle = FontStyle.Bold };
                nameStyle.normal.textColor = learned ? Color.white : new Color(0.55f, 0.55f, 0.55f);
                string header = learned ? $"{spec.DisplayName}   Lv {lv}" : Loc.T($"{spec.DisplayName}   (learn at Lv {spec.ReqLevel})", $"{spec.DisplayName}   （Lv {spec.ReqLevel}で覚える）");
                // 1・2・3 のボタンは右側に縦の真ん中で並べ、文字はその左の幅に1行ずつ収める（折り返さない）
                float bw = row.width * 0.12f;
                float bh = Mathf.Min(btnH, rowH * 0.4f);
                float buttonsW = learned ? SkillInfo.Slots * (bw + inner) + inner : inner;
                float textW = row.width - inner - buttonsW;
                Hud.FitLabel(new Rect(row.x + inner, row.y + inner, textW, lineH * 1.2f), header, nameStyle);

                text.normal.textColor = learned ? new Color(0.85f, 0.85f, 0.85f) : new Color(0.5f, 0.5f, 0.5f);
                float ty = row.y + inner + lineH * 1.25f;
                Hud.FitLabel(new Rect(row.x + inner, ty, textW, lineH), spec.DisplayDesc, text);
                ty += lineH;
                float cd = spec.Cooldown * SkillInfo.CooldownMult(lv);
                string power = Loc.T($"Power x{SkillInfo.Power(lv):0.0}", $"威力 x{SkillInfo.Power(lv):0.0}");
                Hud.FitLabel(new Rect(row.x + inner, ty, textW, lineH), Loc.T($"MP {spec.Mp}   Cooldown {cd:0.0}s   {power}", $"MP {spec.Mp}   待ち時間 {cd:0.0}秒   {power}"), text);
                ty += lineH;

                // ボタンに付ける（1・2・3）
                if (learned)
                {
                    for (int s = 0; s < SkillInfo.Slots; s++)
                    {
                        int slot = s;
                        bool here = ch.Slots[s] == i;
                        var r = new Rect(row.xMax - inner - (SkillInfo.Slots - s) * (bw + inner), row.y + (rowH - bh) * 0.5f, bw, bh);
                        if (r.y < viewTop || r.yMax > viewBottom) continue; // 見える範囲からはみ出したボタンは押せない
                        hud.Button(r, (s + 1).ToString(), here ? new Color(0.3f, 0.6f, 1f, 0.8f) : buttonBack, centered,
                            () => Assign(ch, slot, index));
                    }
                }
                y += rowH + rowGap;
            }

            // はみ出した行を隠すため、上下を塗り直してから見出しと「閉じる」を描く
            Hud.Fill(new Rect(0, 0, W, viewTop), new Color(0.02f, 0.02f, 0.03f, 1f));
            Hud.Fill(new Rect(0, viewBottom, W, H - viewBottom), new Color(0.02f, 0.02f, 0.03f, 1f));
            var title = new GUIStyle(label) { fontStyle = FontStyle.Bold };
            GUI.Label(new Rect(pad, pad, W, btnH), Loc.T($"SKILLS   Lv {ch.Level}", $"スキル   Lv {ch.Level}"), title);
            hud.Button(new Rect(W - pad - W * 0.22f, pad, W * 0.22f, btnH), Loc.T("CLOSE", "閉じる"), buttonBack, centered, () => world.OpenPanel(false, false, false));

            // スクロールできるときは、右端に今の位置を細いバーで出す
            if (maxScroll > 0f)
            {
                float viewH = viewBottom - viewTop;
                float barH = viewH * viewH / contentH;
                float barY = viewTop + (viewH - barH) * (scroll / maxScroll);
                Hud.Fill(new Rect(W - pad * 0.4f, barY, pad * 0.15f, barH), new Color(1f, 1f, 1f, 0.35f));
            }
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
