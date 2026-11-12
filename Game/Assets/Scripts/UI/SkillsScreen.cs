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
            var title = new GUIStyle(label) { fontStyle = FontStyle.Bold };
            GUI.Label(new Rect(pad, pad, W, btnH), Loc.T($"SKILLS   Lv {ch.Level}", $"スキル   Lv {ch.Level}"), title);
            hud.Button(new Rect(W - pad - W * 0.22f, pad, W * 0.22f, btnH), Loc.T("CLOSE", "閉じる"), buttonBack, centered, () => world.OpenPanel(false, false, false));

            float y = pad + btnH + H * 0.015f;
            // 出すのは、今の職業で覚えられるスキルだけ（上位職・3次職のスキルは、転職・転生したときだけ）
            var visible = new System.Collections.Generic.List<int>();
            for (int i = 0; i < SkillInfo.PerClass; i++)
            {
                if (i == AdvancedClasses.SkillIndex && ch.Advanced < 0) continue;
                if (i == ThirdClasses.SkillIndex && !ch.Third) continue;
                visible.Add(i);
            }
            float rowH = Mathf.Min(H * 0.13f, (H - y - pad) / visible.Count - H * 0.008f);
            float lineH = small.fontSize * 1.3f;

            foreach (int i in visible)
            {
                int index = i;
                var spec = SkillInfo.Get(ClassInfo.Current, i);
                bool learned = SkillInfo.Learned(ClassInfo.Current, i, ch.Level);
                var row = new Rect(pad, y, W - pad * 2f, rowH);
                Hud.Fill(row, new Color(spec.Color.r, spec.Color.g, spec.Color.b, learned ? 0.12f : 0.04f));

                float inner = rowH * 0.06f;

                // アイコン（左）。文字はその右に並べる
                float iconSize = Mathf.Min(rowH * 0.55f, W * 0.14f);
                var old = GUI.color;
                GUI.color = learned ? Color.white : new Color(1f, 1f, 1f, 0.3f);
                Hud.DrawSprite(new Rect(row.x + inner, row.y + inner, iconSize, iconSize), PixelArt.SkillIcon(spec.Effect, spec.Color));
                GUI.color = old;
                row = new Rect(row.x + iconSize + inner, row.y, row.width - iconSize - inner, row.height);
                var nameStyle = new GUIStyle(label) { fontStyle = FontStyle.Bold };
                nameStyle.normal.textColor = learned ? Color.white : new Color(0.55f, 0.55f, 0.55f);
                int lv = ch.SkillLv[i];
                string header = learned ? $"{spec.DisplayName}   Lv {lv}" : Loc.T($"{spec.DisplayName}   (learn at Lv {spec.ReqLevel})", $"{spec.DisplayName}   （Lv {spec.ReqLevel}で覚える）");
                GUI.Label(new Rect(row.x + inner, row.y + inner, row.width, lineH * 1.2f), header, nameStyle);

                var text = new GUIStyle(small);
                text.normal.textColor = learned ? new Color(0.85f, 0.85f, 0.85f) : new Color(0.5f, 0.5f, 0.5f);
                float ty = row.y + inner + lineH * 1.25f;
                GUI.Label(new Rect(row.x + inner, ty, row.width - inner * 2f, lineH), spec.DisplayDesc, text);
                ty += lineH;
                float cd = spec.Cooldown * SkillInfo.CooldownMult(lv);
                string power = Loc.T($"Power x{SkillInfo.Power(lv):0.0}", $"威力 x{SkillInfo.Power(lv):0.0}");
                GUI.Label(new Rect(row.x + inner, ty, row.width - inner * 2f, lineH), Loc.T($"MP {spec.Mp}   Cooldown {cd:0.0}s   {power}", $"MP {spec.Mp}   待ち時間 {cd:0.0}秒   {power}"), text);
                ty += lineH;

                // 熟練度（使った回数）
                if (learned)
                {
                    var bar = new Rect(row.x + inner, ty + lineH * 0.25f, row.width * 0.45f, lineH * 0.4f);
                    Hud.Fill(bar, new Color(1f, 1f, 1f, 0.12f));
                    if (lv >= SkillInfo.MaxSkillLevel)
                    {
                        Hud.Fill(bar, new Color(0.6f, 0.85f, 1f, 0.9f));
                    }
                    else
                    {
                        float ratio = Mathf.Clamp01((float)ch.SkillUses[i] / SkillInfo.UsesToNext(lv));
                        Hud.Fill(new Rect(bar.x, bar.y, bar.width * ratio, bar.height), new Color(0.6f, 0.85f, 1f, 0.9f));
                    }
                }

                // ボタンに付ける（1・2・3）
                if (learned)
                {
                    float bw = row.width * 0.13f;
                    float bh = Mathf.Min(btnH, rowH * 0.35f);
                    for (int s = 0; s < SkillInfo.Slots; s++)
                    {
                        int slot = s;
                        bool here = ch.Slots[s] == i;
                        var r = new Rect(row.xMax - inner - (SkillInfo.Slots - s) * (bw + inner), row.yMax - inner - bh, bw, bh);
                        hud.Button(r, (s + 1).ToString(), here ? new Color(0.3f, 0.6f, 1f, 0.8f) : buttonBack, centered,
                            () => Assign(ch, slot, index));
                    }
                }
                y += rowH + H * 0.008f;
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
