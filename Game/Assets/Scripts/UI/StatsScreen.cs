using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 能力値の画面（開いている間はゲームが止まる）。レベル・経験値、5つの能力値と「+」ボタン、
    /// 計算後の能力（攻撃力・体力・MP など）を表示する。
    /// </summary>
    public sealed class StatsScreen
    {
        static readonly Color panel = new Color(0f, 0f, 0f, 0.88f);
        static readonly Color buttonBack = new Color(1f, 1f, 1f, 0.15f);
        static readonly Color plusBack = new Color(1f, 0.85f, 0.3f, 0.45f);

        readonly Hud hud;

        // 仮に振ったポイント（「確定」で反映。画面を閉じても残る）
        int[] pending = new int[Attributes.Count];
        CharacterData pendingFor;

        // 「すべての能力」の一覧（開いているか、スクロール量）
        bool showAll;
        float allScroll;
        // 「職業」の説明（開いているか、スクロール量）
        bool classView;
        float classScroll;

        // 2回押しで実行する確認（押してから数秒だけ有効）
        string armed;
        float armedUntil;

        public StatsScreen(Hud hud)
        {
            this.hud = hud;
        }

        public void Draw(GameWorld world)
        {
            var ch = Profile.Current;
            var st = world.Player.Stats;
            if (ch == null || st == null) return;

            float W = Screen.width;
            float H = Screen.height;
            float pad = W * 0.05f;
            float btnH = H * 0.045f;
            var label = hud.LabelStyle;
            var small = hud.SmallStyle;
            var centered = hud.CenteredStyle;
            float lineH = label.fontSize * 1.4f;

            Hud.Fill(new Rect(0, 0, W, H), panel);

            var spec = ClassInfo.Get(st.Class);
            var title = new GUIStyle(label) { fontStyle = FontStyle.Bold };
            var adv = AdvancedClasses.Of(st.Class, ch);
            Hud.FitLabel(new Rect(pad, pad, W - pad * 2f - W * 0.48f, btnH), (adv != null ? adv.DisplayName : spec.DisplayName) + $"   Lv {ch.Level}" + (ch.Rebirths > 0 ? Loc.T($"   Reborn x{ch.Rebirths}", $"   転生 {ch.Rebirths}回") : ""), title);
            hud.Button(new Rect(W - pad - W * 0.22f, pad, W * 0.22f, btnH), Loc.T("CLOSE", "閉じる"), buttonBack, centered, () => world.StatsOpen = false);
            // 職業の説明（特徴・主能力・武器・補正・スキル・この先の成長）を開く／閉じる
            hud.Button(new Rect(W - pad - W * 0.46f, pad, W * 0.22f, btnH), classView ? Loc.T("STATS", "能力値") : Loc.T("CLASS", "職業"), classView ? plusBack : buttonBack, centered, () => { classView = !classView; classScroll = 0f; });
            if (classView)
            {
                float cy = pad + btnH + H * 0.015f;
                DrawClassInfo(world, ch, st, new Rect(pad, cy, W - pad * 2f, H - pad - cy));
                return;
            }

            // 経験値
            float y = pad + btnH + H * 0.015f;
            int need = Progression.ExpToNext(ch, ch.Level);
            var bar = new Rect(pad, y, W - pad * 2f, H * 0.012f);
            Hud.Fill(bar, new Color(1f, 1f, 1f, 0.12f));
            Hud.Fill(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01((float)ch.Exp / need), bar.height), new Color(1f, 0.85f, 0.3f, 0.9f));
            y += bar.height + H * 0.008f;
            GUI.Label(new Rect(pad, y, W, lineH), $"EXP {ch.Exp} / {need}", small);
            y += lineH * 1.2f;

            // 能力値：[+10][+][-][-10] で仮に振り、「確定」で反映する
            if (pending.Length != Attributes.Count) pending = new int[Attributes.Count];
            if (pendingFor != ch) { System.Array.Clear(pending, 0, pending.Length); pendingFor = ch; } // キャラクターが変わったら消す
            int pendingSum = 0;
            foreach (int v in pending) pendingSum += v;
            if (pendingSum > ch.Points) { System.Array.Clear(pending, 0, pending.Length); pendingSum = 0; } // リセットなどで足りなくなったら仮振りを消す
            int left = ch.Points - pendingSum;
            var pointsStyle = new GUIStyle(label) { fontStyle = FontStyle.Bold };
            pointsStyle.normal.textColor = ch.Points > 0 ? new Color(1f, 0.85f, 0.3f) : Color.white;
            GUI.Label(new Rect(pad, y, W * 0.5f, lineH), Loc.T($"Points: {left}", $"ポイント：{left}"), pointsStyle);
            if (pendingSum > 0)
            {
                float cw = W * 0.18f;
                hud.Button(new Rect(W - pad - cw, y - lineH * 0.1f, cw, lineH * 1.2f), Loc.T("APPLY", "確定"), plusBack, centered, () =>
                {
                    world.AllocateMany(pending);
                    System.Array.Clear(pending, 0, pending.Length);
                });
                hud.Button(new Rect(W - pad - cw * 2f - W * 0.02f, y - lineH * 0.1f, cw, lineH * 1.2f), Loc.T("CLEAR", "取り消し"), buttonBack, centered,
                    () => System.Array.Clear(pending, 0, pending.Length));
            }
            y += lineH * 1.3f;

            float rowH = H * 0.06f;
            float plusW = rowH * 1.2f;
            // 数値の欄の文字の大きさは、5つの能力値でそろえる（いちばん長い行に合わせる。行ごとに大きさが変わらないように）
            int valueFont = label.fontSize;
            {
                float vw = (ch.Points > 0 ? (W - pad) - 4f * (rowH * 0.95f + W * 0.008f) : (W - pad)) - (pad + W * 0.17f) - W * 0.01f;
                var probe = new GUIStyle(label) { fontStyle = FontStyle.Bold };
                for (int i = 0; i < Attributes.Count; i++)
                {
                    int bv = spec.BaseStats[i] + ch.Alloc[i] + ThirdClasses.BonusStats(ch);
                    int fg = st.Attr[i] - bv;
                    var c = new GUIContent($"{st.Attr[i] + pending[i]}" + (fg > 0 ? $"  (+{fg})" : ""));
                    probe.fontSize = valueFont;
                    while (probe.fontSize > 8 && probe.CalcSize(c).x > vw * 0.94f) probe.fontSize--;
                    valueFont = probe.fontSize;
                }
            }
            for (int i = 0; i < Attributes.Count; i++)
            {
                var attr = (Attr)i;
                int baseValue = spec.BaseStats[i] + ch.Alloc[i] + ThirdClasses.BonusStats(ch);
                int total = st.Attr[i];
                int fromGear = total - baseValue;
                var row = new Rect(pad, y, W - pad * 2f, rowH);
                Hud.Fill(row, new Color(1f, 1f, 1f, i % 2 == 0 ? 0.06f : 0.03f));

                var nameStyle = new GUIStyle(label) { fontStyle = FontStyle.Bold };
                if (st.AllSub) nameStyle.normal.textColor = new Color(0.95f, 0.75f, 0.5f);             // 彩灯者：主能力はなく、すべてが副能力
                else if (attr == st.MainAttr) nameStyle.normal.textColor = new Color(1f, 0.85f, 0.3f); // 今の武器の主能力
                else if (attr == st.SubAttr || attr == st.ThirdAttr) nameStyle.normal.textColor = new Color(0.95f, 0.75f, 0.5f); // 副能力・3次職の追加の能力値
                GUI.Label(new Rect(row.x + pad * 0.3f, row.y, W * 0.18f, rowH * 0.6f), Attributes.ShortName(attr), nameStyle);
                string gear = fromGear > 0 ? $"  (+{fromGear})" : "";
                int idx = i;
                // 仮に振っていれば、確定後の値を金色で出す（ボタンと重ならないよう、数値の欄の中だけで）
                var valueStyle = new GUIStyle(label) { fontSize = valueFont };
                if (pending[i] > 0)
                {
                    valueStyle.fontStyle = FontStyle.Bold;
                    valueStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);
                }
                float valueW = (ch.Points > 0 ? row.xMax - 4f * (rowH * 0.95f + W * 0.008f) : row.xMax) - (row.x + W * 0.17f) - W * 0.01f;
                Hud.FitLabel(new Rect(row.x + W * 0.17f, row.y, valueW, rowH * 0.6f), $"{total + pending[i]}{gear}", valueStyle);
                float bw = rowH * 0.95f, bg = W * 0.008f;
                Hud.FitLabel(new Rect(row.x + pad * 0.3f, row.y + rowH * 0.55f, row.width - pad * 0.6f, rowH * 0.45f), Attributes.Effect(attr), small); // 説明は全幅

                if (ch.Points > 0)
                {
                    // [+10][+][-][-10]（残りのポイントと、仮に振った分の範囲で）
                    float bx = row.xMax - 4f * (bw + bg);
                    var by = row.y + rowH * 0.04f;
                    void Step(int k, string text, int delta)
                    {
                        var r = new Rect(bx + k * (bw + bg), by, bw, rowH * 0.5f); // 行の上半分（下の説明と重ならない）
                        bool can = delta > 0 ? left > 0 : pending[idx] > 0;
                        if (can) hud.Button(r, text, delta > 0 ? plusBack : buttonBack, centered, () =>
                        {
                            int sum = 0;
                            foreach (int v in pending) sum += v;
                            int room = ch.Points - sum;
                            pending[idx] = delta > 0 ? pending[idx] + Mathf.Min(delta, room) : Mathf.Max(0, pending[idx] + delta);
                        });
                        else
                        {
                            Hud.Fill(r, new Color(0.04f, 0.04f, 0.07f, 0.92f));
                            Hud.OrnateFrame(r, new Color(0.45f, 0.4f, 0.3f, 1f), 1f, true, false); // 押せないときは暗い金でへこみ
                            var dimStyle = new GUIStyle(centered);
                            dimStyle.normal.textColor = new Color(0.45f, 0.45f, 0.45f);
                            Hud.FitLabel(r, text, dimStyle);
                        }
                    }
                    Step(0, "+10", 10);
                    Step(1, "+", 1);
                    Step(2, "-", -1);
                    Step(3, "-10", -10);
                }
                y += rowH + H * 0.006f;
            }
            y += H * 0.01f;

            // 仮に振っている間は、確定したあとの能力を見せる（加護も込み）
            bool preview = pendingSum > 0;
            if (preview)
            {
                for (int k = 0; k < Attributes.Count; k++) ch.Alloc[k] += pending[k];
                var pv = PlayerStats.From(world.Inventory.Equipped, st.Class, ch);
                for (int k = 0; k < Attributes.Count; k++) ch.Alloc[k] -= pending[k];
                foreach (int b in world.Player.Boons) pv = pv.WithBoon(Boons.Get(b));
                st = pv;
            }
            int shownHp = preview ? st.MaxHp : world.Player.MaxHp;
            int shownMp = preview ? st.MaxMp : world.Player.MaxMp;
            float shownLight = preview ? st.Light : world.Player.EffectiveLight;

            // 計算後の能力
            string kind;
            switch (st.Kind)
            {
                case AttackKind.Slash: kind = Loc.T($"Slash  reach {st.Reach:0.0}  arc {st.ArcDeg:0}  targets {st.MaxTargets}", $"斬撃  届く距離 {st.Reach:0.0}  広さ {st.ArcDeg:0}度  当たる数 {st.MaxTargets}"); break;
                case AttackKind.Orbit: kind = Loc.T($"Flames x{st.Orbs}", $"回る灯り x{st.Orbs}"); break;
                case AttackKind.Pulse: kind = Loc.T($"Pulse  radius {st.PulseRadius:0.0}", $"周りを焼く  範囲 {st.PulseRadius:0.0}"); break;
                default: kind = Loc.T($"Shots x{st.Shots}  pierce {st.Pierce}", $"弾 x{st.Shots}  貫通 {st.Pierce}"); break;
            }
            // 計算後の能力：2列の表（左に名前、右に値）
            float bottomY = H - pad - btnH;
            string[,] cells =
            {
                { "HP", $"{shownHp}", Loc.T("HP regen", "HP回復"), Loc.T($"{st.HpRegen:0.0}/s", $"{st.HpRegen:0.0}/秒") },
                { "MP", $"{shownMp}", Loc.T("MP regen", "MP回復"), Loc.T($"{st.MpRegen:0.0}/s", $"{st.MpRegen:0.0}/秒") },
                { Loc.T("Damage", "ダメージ"), $"{st.Damage:0}", Loc.T("Attacks/s", "攻撃/秒"), $"{st.AttacksPerSecond:0.0}" },
                { "DPS", $"{st.Damage * st.AttacksPerSecond * (1f + st.EffectiveCrit * (st.CritMult - 1f)):0}", Loc.T("Crit", "会心率"),
                    GameConfig.CritResist > 0f ? $"{st.CritChance * 100f:0}%→{st.EffectiveCrit * 100f:0}%" : $"{st.CritChance * 100f:0}%" }, // 敵の会心耐性があれば、実際の会心率も
                { Loc.T("Armor", "防御"), $"{st.Armor:0}", Loc.T("Crit dmg", "会心ダメージ"), $"x{st.CritMult:0.00}" },
                { Loc.T("Light", "明るさ"), $"{shownLight:0.0}", Loc.T("Speed", "速さ"), $"{st.MoveSpeed:0.0}" },
                { Loc.T("Life steal", "吸収"), $"{st.LifeSteal * 100f:0.0}%", Loc.T("MP next Lv", "次のLvのMP"), $"+{Progression.MpPerLevel(Progression.BaseWis(ch))}" },
                { "HIT", Loc.T($"{st.Hit} ({st.HitChance(world.Enemies.FloorTier) * 100f:0}%)", $"{st.Hit}（{st.HitChance(world.Enemies.FloorTier) * 100f:0}%）"),
                  "EVA", Loc.T($"{st.Eva} ({st.EvadeChance(world.Enemies.FloorTier) * 100f:0}%)", $"{st.Eva}（{st.EvadeChance(world.Enemies.FloorTier) * 100f:0}%）") },
            };
            var head = new GUIStyle(label) { fontStyle = FontStyle.Bold };
            head.normal.textColor = new Color(1f, 0.85f, 0.4f);
            GUI.Label(new Rect(pad, y, W, lineH), preview ? Loc.T("RESULT (after APPLY)", "最終的な能力（確定後）") : Loc.T("RESULT", "最終的な能力"), head);
            {
                // 「すべての能力」の切り替え（固有効果・セット効果まで、計算後の能力を全部並べる）
                float tw = W * 0.3f;
                hud.Button(new Rect(W - pad - tw, y - lineH * 0.1f, tw, lineH * 1.2f), showAll ? Loc.T("BACK", "もどる") : Loc.T("ALL STATS", "すべての能力"),
                    showAll ? plusBack : buttonBack, centered, () => { showAll = !showAll; allScroll = 0f; });
            }
            y += lineH * 1.1f;
            if (showAll)
            {
                DrawAll(world, st, ch, shownHp, shownMp, shownLight, kind, new Rect(pad, y, W - pad * 2f, bottomY - y - H * 0.01f));
                DrawResetButtons(world, W, pad, bottomY, btnH);
                return;
            }
            var key = new GUIStyle(small);
            key.normal.textColor = new Color(0.65f, 0.65f, 0.7f);
            var val = new GUIStyle(label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight };
            float colW = (W - pad * 2f - W * 0.04f) * 0.5f;
            float cellH = label.fontSize * 1.5f;
            for (int r = 0; r < cells.GetLength(0); r++)
            {
                Hud.Fill(new Rect(pad, y, W - pad * 2f, cellH), new Color(1f, 1f, 1f, r % 2 == 0 ? 0.05f : 0.02f));
                for (int c = 0; c < 2; c++)
                {
                    float x = pad + c * (colW + W * 0.04f);
                    GUI.Label(new Rect(x + W * 0.01f, y, colW, cellH), cells[r, c * 2], new GUIStyle(key) { alignment = TextAnchor.MiddleLeft });
                    GUI.Label(new Rect(x, y, colW - W * 0.01f, cellH), cells[r, c * 2 + 1], val);
                }
                y += cellH;
            }
            y += lineH * 0.3f;
            GUI.Label(new Rect(pad, y, W - pad * 2f, lineH), kind, small);
            y += lineH * 1.1f;

            // 加護（祭壇で得たもの）。同じものはまとめて「x2」、折り返して全部見せる
            GUI.Label(new Rect(pad, y, W, lineH), Loc.T("BOONS", "加護"), head);
            y += lineH * 1.1f;
            DrawBoons(world.Player, new Rect(pad, y, W - pad * 2f, bottomY - y - H * 0.01f));

            DrawResetButtons(world, W, pad, bottomY, btnH);
        }

        /// <summary>振り直しとレベルのリセット（2回押しで実行）。</summary>
        void DrawResetButtons(GameWorld world, float W, float pad, float bottomY, float btnH)
        {
            float gap = W * 0.03f;
            float half = (W - pad * 2f - gap) * 0.5f;
            ConfirmButton(new Rect(pad, bottomY, half, btnH), "points", Loc.T("RESET POINTS", "ポイントを振り直す"), world.ResetPoints);
            ConfirmButton(new Rect(pad + half + gap, bottomY, half, btnH), "level", Loc.T("RESET ALL", "すべてリセット"), world.ResetLevel);
        }

        /// <summary>
        /// すべての能力：装備・能力値・加護・職の補正・セット効果をすべて計算したあとの値を、分類ごとに並べる。
        /// 固有効果・セット効果は、持っているものだけ出す。入りきらなければスクロールする。
        /// </summary>
        void DrawAll(GameWorld world, PlayerStats st, CharacterData ch, int hp, int mp, float light, string kind, Rect area)
        {
            // (名前, 値)。値が null の行は見出し、名前が空の行は1行まるごとの説明
            var rows = new System.Collections.Generic.List<(string k, string v, Color c)>();
            Color white = Color.white, gold = new Color(1f, 0.85f, 0.4f);
            void H(string t) => rows.Add((t, null, gold));
            void R(string k, string v) => rows.Add((k, v, white));
            void Line(string t, Color c) => rows.Add(("", t, c));
            string Pct(float f) => $"{f * 100f:0.#}%";
            int tier = world.Enemies.FloorTier;

            H(Loc.T("Attack", "攻撃"));
            R(Loc.T("Damage", "ダメージ"), $"{st.Damage:0}");
            R(Loc.T("Attacks/s", "攻撃/秒"), $"{st.AttacksPerSecond:0.00}");
            R("DPS", $"{st.Damage * st.AttacksPerSecond * (1f + st.EffectiveCrit * (st.CritMult - 1f)):0}");
            R(Loc.T("Crit", "会心率"), GameConfig.CritResist > 0f ? $"{Pct(st.CritChance)}→{Pct(st.EffectiveCrit)}" : Pct(st.CritChance));
            R(Loc.T("Crit dmg", "会心ダメージ"), $"x{st.CritMult:0.00}");
            R("HIT", Loc.T($"{st.Hit} ({st.HitChance(tier) * 100f:0}%)", $"{st.Hit}（{st.HitChance(tier) * 100f:0}%）"));
            R(Loc.T("Double strike", "連続攻撃の確率"), Pct(st.DoubleStrikeChance));
            R(Loc.T("Elite/boss dmg", "精鋭・ボスへのダメージ"), "+" + Pct(st.EliteDamage));
            Line(kind, new Color(0.8f, 0.8f, 0.85f));
            if (st.Hybrid) Line(Loc.T("Any range: cuts near, shoots far", "遠近両用：近くは斬り、遠くは撃つ"), new Color(0.8f, 0.8f, 0.85f));

            H(Loc.T("Skills", "スキル"));
            R(Loc.T("Skill power base", "スキルの基準の攻撃力"), $"{st.SkillBase:0}");
            R(Loc.T("Skill damage", "スキルのダメージ"), "+" + Pct(st.SkillDamage));
            R(Loc.T("Cooldown", "待ち時間"), "-" + Pct(1f - st.CooldownMult));
            R(Loc.T("Skill area", "スキルの範囲"), $"x{st.AreaMult:0.00}");

            H(Loc.T("Defense", "守り"));
            R("HP", $"{hp}");
            R(Loc.T("HP regen", "HP回復"), Loc.T($"{st.HpRegen:0.0}/s", $"{st.HpRegen:0.0}/秒"));
            R("MP", $"{mp}");
            R(Loc.T("MP regen", "MP回復"), Loc.T($"{st.MpRegen:0.0}/s", $"{st.MpRegen:0.0}/秒"));
            R(Loc.T("Armor", "防御"), $"{st.Armor:0}");
            R(Loc.T("Damage taken", "受けるダメージ"), st.DamageTaken < 1f ? "-" + Pct(1f - st.DamageTaken) : st.DamageTaken > 1f ? "+" + Pct(st.DamageTaken - 1f) : "±0%");
            R("EVA", Loc.T($"{st.Eva} ({st.EvadeChance(tier) * 100f:0}%)", $"{st.Eva}（{st.EvadeChance(tier) * 100f:0}%）"));
            R(Loc.T("Dodge cooldown", "回避の待ち時間"), Loc.T($"{st.DodgeCooldown:0.0}s", $"{st.DodgeCooldown:0.0}秒"));
            R(Loc.T("Life steal", "吸収"), Pct(st.LifeSteal));
            R(Loc.T("HP on kill", "倒したときのHP回復"), $"{st.HealOnKill}");

            H(Loc.T("Other", "そのほか"));
            R(Loc.T("Speed", "速さ"), $"{st.MoveSpeed:0.0}");
            R(Loc.T("Light", "明るさ"), $"{light:0.0}");
            R(Loc.T("EXP gain", "経験値"), "+" + Pct(st.ExpGain));
            R(Loc.T("Ember find", "拾う火種"), "+" + Pct(st.EmberFind));
            R(Loc.T("Magic find", "珍しい品の出やすさ"), "+" + Pct(st.MagicFind));
            R(Loc.T("MP next Lv", "次のLvのMP"), $"+{Progression.MpPerLevel(Progression.BaseWis(ch))}");

            H(Loc.T("Unique effects", "固有効果"));
            int before = rows.Count;
            if (st.ExplodeMult > 0f) R(Loc.T("Explode on kill", "倒したときの爆発"), $"x{st.ExplodeMult:0.00}");
            if (st.DodgeNovaMult > 0f) R(Loc.T("Flame on dodge", "回避したときの炎"), $"x{st.DodgeNovaMult:0.00}");
            if (st.AbyssBlastChance > 0f) R(Loc.T("Abyss blast", "大爆発の確率"), Pct(st.AbyssBlastChance));
            if (st.VoidRift > 0) R(Loc.T("Void rift", "裂け目"), $"x{st.VoidRift}");
            if (st.StarfallChance > 0f) R(Loc.T("Starfall on crit", "会心で星が落ちる確率"), Pct(st.StarfallChance));
            if (st.RetributionMult > 0f) R(Loc.T("Retribution", "受けたときの反撃"), $"x{st.RetributionMult:0.00}");
            if (st.Judgment > 0) R(Loc.T("Pillar of judgment", "裁きの光の柱"), Loc.T($"every {6f / st.Judgment:0.0}s", $"{6f / st.Judgment:0.0}秒ごと"));
            if (rows.Count == before) Line(Loc.T("None", "なし"), new Color(0.6f, 0.6f, 0.65f));

            H(Loc.T("Boss sets", "ボスのセット"));
            int beforeB = rows.Count;
            for (int k = 0; k < BossSets.Count6; k++)
            {
                int have = BossSets.Count(world.Inventory.Equipped, k);
                if (have <= 0) continue;
                var bc = BossSets.ColorOf(k);
                R(Loc.T($"{BossSets.Name(k)} set", $"{BossSets.Name(k)}のセット"), $"{have}/5");
                foreach (int t in BossSets.Tiers)
                    Line(Loc.T($"({t}) ", $"（{t}つ）") + BossSets.BonusText(k, t), have >= t ? bc : new Color(0.45f, 0.45f, 0.5f));
            }
            if (rows.Count == beforeB) Line(Loc.T("None", "なし"), new Color(0.6f, 0.6f, 0.65f));

            H(Loc.T("Abyss sets", "深淵のセット"));
            before = rows.Count;
            foreach (var set in AbyssSets.All)
            {
                int have = AbyssSets.Count(world.Inventory.Equipped, set);
                if (have <= 0) continue;
                var sc = AbyssSets.ColorOf(set);
                R(Loc.T($"{AbyssSets.Name(set)} set", $"{AbyssSets.Name(set)}のセット"), $"{have}/5");
                foreach (int t in AbyssSets.Tiers)
                {
                    bool on = have >= t;
                    Line(Loc.T($"({t}) ", $"（{t}つ）") + AbyssSets.BonusText(set, t), on ? sc : new Color(0.45f, 0.45f, 0.5f));
                }
            }
            if (rows.Count == before) Line(Loc.T("None", "なし"), new Color(0.6f, 0.6f, 0.65f));

            DrawRowList(rows, area, ref allScroll);
        }

        /// <summary>
        /// 職業の説明：今の職業（上位職・3次職・彩灯者）の特徴、主能力と副能力、能力値ごとの働き、職業の基本の数値、
        /// 使える武器と攻撃の形、職の補正、スキル（覚えるレベル）、この先の成長（転職・3次職・転生）。
        /// </summary>
        void DrawClassInfo(GameWorld world, CharacterData ch, PlayerStats st, Rect area)
        {
            var rows = new System.Collections.Generic.List<(string k, string v, Color c)>();
            Color white = Color.white, gold = new Color(1f, 0.85f, 0.4f), soft = new Color(0.85f, 0.85f, 0.9f), dimC = new Color(0.6f, 0.6f, 0.65f);
            void H(string t) => rows.Add((t, null, gold));
            void R(string k, string v) => rows.Add((k, v, white));
            void L(string t, Color c) => rows.Add(("", t, c));
            var type = (ClassType)ch.ClassId;
            var cls = ClassInfo.Get(type);
            var adv = AdvancedClasses.Of(type, ch);
            var third = ThirdClasses.Of(type, ch);
            var special = SpecialClass.Of(ch);

            H(AdvancedClasses.NameFor(ch));
            L(cls.DisplayName + Loc.T(": ", "：") + Loc.T(cls.Line1, cls.Line1Ja) + " " + Loc.T(cls.Line2, cls.Line2Ja), soft);
            L(Loc.T("Light aptitude: ", "灯りの適性：") + cls.DisplayAptitude, new Color(1f, 0.85f, 0.55f));
            if (special != null) L(special.DisplayDesc, soft);
            if (adv != null) L(adv.DisplayName + Loc.T(": ", "：") + adv.DisplayDesc, soft);
            if (third != null) L(third.DisplayName + Loc.T(": ", "：") + third.DisplayDesc, soft);

            H(Loc.T("Attributes", "能力値"));
            if (st.AllSub) L(Loc.T("No main attribute: every attribute is a sub attribute (+1% attack each).", "主能力はなく、5つの能力値すべてが副能力（1ポイントで攻撃力 +1%）。"), soft);
            else
            {
                R(Loc.T("Main", "主能力"), Loc.T($"{Attributes.ShortName(st.MainAttr)}  (+2% attack each)", $"{Attributes.ShortName(st.MainAttr)}（1ポイントで攻撃力 +2%）"));
                R(Loc.T("Sub", "副能力"), Loc.T($"{Attributes.ShortName(st.SubAttr)}  (+1% attack each)", $"{Attributes.ShortName(st.SubAttr)}（1ポイントで攻撃力 +1%）"));
                if (st.ThirdAttr.HasValue) R(Loc.T("3rd class", "3次職の追加"), Loc.T($"{Attributes.ShortName(st.ThirdAttr.Value)}  (+1% attack each)", $"{Attributes.ShortName(st.ThirdAttr.Value)}（1ポイントで攻撃力 +1%）"));
            }
            for (int a = 0; a < Attributes.Count; a++) L($"{Attributes.ShortName((Attr)a)}" + Loc.T(": ", "：") + Attributes.Effect((Attr)a), dimC);

            H(Loc.T("Base", "職業の基本"));
            R(Loc.T("Base HP", "基本のHP"), $"{cls.BaseHp}");
            R(Loc.T("Speed", "速さ"), $"{cls.Speed:0.0}");
            R(Loc.T("Light", "明るさ"), $"{cls.BaseLight:0.0}");
            R(Loc.T("Crit", "会心率"), $"{cls.Crit * 100f:0}%");
            if (cls.Armor > 0f) R(Loc.T("Armor", "防御"), $"+{cls.Armor:0}");
            if (cls.DamageTaken < 1f) R(Loc.T("Damage taken", "受けるダメージ"), $"-{(1f - cls.DamageTaken) * 100f:0}%");
            if (cls.DodgeCooldownMult < 1f) R(Loc.T("Dodge cooldown", "回避の待ち時間"), $"-{(1f - cls.DodgeCooldownMult) * 100f:0}%");

            H(Loc.T("Weapons", "使える武器"));
            foreach (var wk in cls.Weapons)
            {
                var w = ClassInfo.Weapon(wk);
                string attr = w.Main.HasValue ? Loc.T($"  [{Attributes.ShortName(w.Main.Value)}]", $"（{Attributes.ShortName(w.Main.Value)}で伸びる）") : "";
                L(w.DisplayName + Loc.T(": ", "：") + w.DisplayDesc + attr, soft);
            }
            if (st.Hybrid) L(Loc.T("Any range: cuts near, shoots far.", "遠近両用：近くの敵は斬り、遠くの敵には弾を撃つ。"), soft);

            H(Loc.T("Skills", "スキル"));
            for (int i = 0; i < SkillInfo.PerClass; i++)
            {
                if (i == AdvancedClasses.SkillIndex && ch.Advanced < 0 && !ch.Special) continue;
                if (i == ThirdClasses.SkillIndex && !ch.Third && !ch.Special) continue;
                var sp = SkillInfo.Get(type, i);
                bool learned = SkillInfo.Learned(type, i, ch.Level);
                L(Loc.T($"Lv{sp.ReqLevel}  ", $"Lv{sp.ReqLevel}  ") + sp.DisplayName + Loc.T(": ", "：") + sp.DisplayDesc, learned ? soft : dimC);
            }

            H(Loc.T("Growth", "この先の成長"));
            if (ch.Special) L(Loc.T("No further class changes. Rebirth at Lv 99 keeps you a Vividbearer (all stats +5 each time).", "この先の転職はない。Lv99 で転生しても彩灯者のまま（転生ごとに全能力値 +5）。"), soft);
            else
            {
                if (adv == null) L(Loc.T($"Class change at Lv {AdvancedClasses.ReqLevel} ({AdvancedClasses.Cost} embers): choose one of two advanced classes.", $"Lv{AdvancedClasses.ReqLevel}で転職（火種 {AdvancedClasses.Cost}）：2つの上位職から選ぶ。"), ch.Level >= AdvancedClasses.ReqLevel ? gold : soft);
                else if (!ch.Third) L(Loc.T($"3rd class at Lv {ThirdClasses.PromoteLevel} ({ThirdClasses.PromoteCost} embers).", $"上位職でLv{ThirdClasses.PromoteLevel}になると3次職へ（火種 {ThirdClasses.PromoteCost}）。"), ch.Level >= ThirdClasses.PromoteLevel ? gold : soft);
                L(Loc.T($"Rebirth at Lv {ThirdClasses.ReqLevel}: back to your base class at Lv 1, all stats +5 each time.", $"Lv{ThirdClasses.ReqLevel}で転生：基本職の Lv1 に戻り、転生ごとに全能力値 +5。"), soft);
                if (SpecialClass.RebirthBecomesSpecial(ch)) L(Loc.T("You have never changed class... rebirth as you are, and you may become something special.", "一度も転職していない……このまま転生すれば、特別な何かになれるかもしれない。"), new Color(1f, 0.6f, 0.9f));
            }

            DrawRowList(rows, area, ref classScroll);
        }

        /// <summary>
        /// (名前, 値, 色) の行を並べて描く（値が null は見出し、名前が空は1行まるごとの文）。入りきらなければスクロールする。
        /// </summary>
        void DrawRowList(System.Collections.Generic.List<(string k, string v, Color c)> rows, Rect area, ref float scroll)
        {
            Color gold = new Color(1f, 0.85f, 0.4f);
            // 描く（見えている範囲だけ。スクロールできる）
            var small = hud.SmallStyle;
            var label = hud.LabelStyle;
            float rowH = label.fontSize * 1.45f;
            // 1行まるごとの文は折り返す（長い説明が縮んで読めなくならないように）。行ごとの高さを先に決める
            var line = new GUIStyle(small) { alignment = TextAnchor.MiddleLeft, wordWrap = true };
            var heights = new float[rows.Count];
            float contentH = 0f;
            for (int i = 0; i < rows.Count; i++)
            {
                var (k0, v0, _) = rows[i];
                heights[i] = v0 != null && k0.Length == 0 ? Mathf.Max(rowH, line.CalcHeight(new GUIContent(v0), area.width * 0.96f) + rowH * 0.3f) : rowH;
                contentH += heights[i];
            }
            float maxScroll = Mathf.Max(0f, contentH - area.height);
            scroll = Mathf.Clamp(scroll + ScrollInput.Delta(), 0f, maxScroll);
            var key = new GUIStyle(small) { alignment = TextAnchor.MiddleLeft };
            key.normal.textColor = new Color(0.7f, 0.7f, 0.75f);
            var val = new GUIStyle(label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight };
            var head = new GUIStyle(label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            head.normal.textColor = gold;
            GUI.BeginGroup(area);
            float y = -scroll;
            int stripe = 0;
            for (int ri = 0; ri < rows.Count; ri++)
            {
                var (k, v, c) = rows[ri];
                float h = heights[ri];
                if (y + h >= 0f && y <= area.height)
                {
                    var r = new Rect(0f, y, area.width, h);
                    if (v == null)
                    {
                        Hud.Fill(new Rect(0f, y + rowH - 2f, area.width, 2f), new Color(1f, 0.85f, 0.4f, 0.35f));
                        GUI.Label(r, k, head);
                        stripe = 0;
                    }
                    else
                    {
                        Hud.Fill(r, new Color(1f, 1f, 1f, stripe % 2 == 0 ? 0.05f : 0.02f));
                        if (k.Length == 0)
                        {
                            line.normal.textColor = c;
                            GUI.Label(new Rect(area.width * 0.02f, y, area.width * 0.96f, h), v, line);
                        }
                        else
                        {
                            Hud.FitLabel(new Rect(area.width * 0.02f, y, area.width * 0.6f, rowH), k, key);
                            GUI.Label(new Rect(area.width * 0.4f, y, area.width * 0.58f, rowH), v, val);
                        }
                    }
                }
                if (v != null) stripe++;
                y += h;
            }
            GUI.EndGroup();
            // スクロールできるときは、右端に位置の目安
            if (maxScroll > 0f)
            {
                float sbH = area.height * area.height / contentH;
                float sbY = area.y + (area.height - sbH) * (scroll / maxScroll);
                Hud.Fill(new Rect(area.xMax + area.width * 0.005f, sbY, Mathf.Max(3f, area.width * 0.008f), sbH), new Color(1f, 1f, 1f, 0.3f));
            }
        }

        int selectedBoon = -1;

        /// <summary>
        /// 加護の一覧：アイコンを並べ（同じ加護は数を右下に）、押すとその下に名前と説明を出す。
        /// </summary>
        void DrawBoons(Player p, Rect area)
        {
            var small = hud.SmallStyle;
            float lineH = small.fontSize * 1.35f;
            if (p.Boons.Count == 0)
            {
                GUI.Label(new Rect(area.x, area.y, area.width, lineH), Loc.T("None yet. Offer embers at an altar.", "まだない。祭壇で火種を払うと得られる。"), small);
                return;
            }
            var order = new System.Collections.Generic.List<int>();
            var count = new System.Collections.Generic.Dictionary<int, int>();
            foreach (int b in p.Boons)
            {
                if (!count.ContainsKey(b)) { order.Add(b); count[b] = 0; }
                count[b]++;
            }
            if (!count.ContainsKey(selectedBoon)) selectedBoon = order[0];

            // アイコン（加護は12種類なので、1行に入るだけ並べて折り返す）
            float detailH = lineH * 2.2f;
            float gap = area.width * 0.015f;
            int cols = 8;
            float cell = (area.width - gap * (cols - 1)) / cols;
            int rows = (order.Count + cols - 1) / cols;
            float maxCell = (area.height - detailH) / rows - gap;
            if (maxCell < cell) cell = Mathf.Max(lineH, maxCell);
            var badge = new GUIStyle(small) { alignment = TextAnchor.LowerRight, fontStyle = FontStyle.Bold };
            badge.normal.textColor = new Color(1f, 0.9f, 0.5f);
            for (int i = 0; i < order.Count; i++)
            {
                int b = order[i];
                var r = new Rect(area.x + (i % cols) * (cell + gap), area.y + (i / cols) * (cell + gap), cell, cell);
                hud.Button(r, null, b == selectedBoon ? new Color(1f, 0.85f, 0.4f, 0.4f) : new Color(1f, 1f, 1f, 0.08f), hud.CenteredStyle, () => selectedBoon = b);
                float inset = cell * 0.12f;
                Hud.DrawSprite(new Rect(r.x + inset, r.y + inset, cell - inset * 2f, cell - inset * 2f), PixelArt.BoonIcon(b));
                if (count[b] > 1) GUI.Label(new Rect(r.x, r.y, r.width - cell * 0.05f, r.height), $"x{count[b]}", badge);
            }

            // 選んだ加護の名前と説明
            float y = area.y + rows * (cell + gap);
            var spec = Boons.Get(selectedBoon);
            var name = new GUIStyle(small) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            name.normal.textColor = new Color(1f, 0.9f, 0.6f);
            string n = count[selectedBoon] > 1 ? $"{spec.DisplayName} x{count[selectedBoon]}" : spec.DisplayName;
            Hud.FitLabel(new Rect(area.x, y, area.width, lineH), n, name);
            Hud.FitLabel(new Rect(area.x, y + lineH, area.width, lineH), spec.DisplayDesc, new GUIStyle(small) { alignment = TextAnchor.MiddleLeft });
        }

        void ConfirmButton(Rect r, string id, string text, System.Action action)
        {
            bool isArmed = armed == id && Time.unscaledTime < armedUntil;
            var back = isArmed ? new Color(0.7f, 0.15f, 0.15f, 0.8f) : new Color(1f, 1f, 1f, 0.12f);
            hud.Button(r, isArmed ? Loc.T("TAP AGAIN", "もう一度押す") : text, back, hud.CenteredStyle, () =>
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
