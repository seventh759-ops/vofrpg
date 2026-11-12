using System;
using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 仮の画面表示（IMGUI）。体力・波・撃破数・火種・FPS、ダメージの数字、拾った品の通知、
    /// 攻撃モード切替・鞄・攻撃ボタン、仮想スティック、鞄の画面。
    /// ボタンは自前で当たり判定する（タッチもマウスも PlayerInputReader 経由の Tapped で届く）。
    /// 日本語フォントの用意がまだないため、表示は英語にしている。
    /// </summary>
    public sealed class Hud : MonoBehaviour
    {
        sealed class FloatNumber
        {
            public Vector2 World;
            public string Text;
            public Color Color;
            public float T;
            public bool Big;
        }

        sealed class ToastLine
        {
            public string Text;
            public Color Color;
            public float T;
        }

        const int MaxNumbers = 80;
        const float NumberLife = 0.6f;
        const float ToastLife = 2.5f;

        public GameWorld World;

        readonly List<FloatNumber> numbers = new List<FloatNumber>();
        readonly List<ToastLine> toasts = new List<ToastLine>();
        readonly List<Rect> buttonRects = new List<Rect>();     // 画面座標（左下原点）
        readonly List<Action> buttonActions = new List<Action>();
        readonly List<Rect> pendingRects = new List<Rect>();
        readonly List<Action> pendingActions = new List<Action>();

        BagScreen bag;
        StatsScreen stats;
        SkillsScreen skills;
        ReturnScreen returnScreen;
        MenuScreen menu;
        AltarScreen altarScreen;
        public GuideBox Guide { get; private set; }
        public TownScreen Town { get; private set; }
        GUIStyle label;
        GUIStyle small;
        GUIStyle centered;
        GUIStyle big;
        GUIStyle number;
        float fps = 60f;

        public GUIStyle LabelStyle => label;
        public GUIStyle SmallStyle => small;
        public GUIStyle CenteredStyle => centered;

        // ---- 画面のピクセル座標（左下が原点）。入力の判定と描画の両方で使う ----

        /// <summary>右上の MENU ボタン（鞄・能力値・スキル・設定はここから）。</summary>
        Rect MenuRect
        {
            get
            {
                float w = Screen.width * 0.22f;
                float h = Screen.height * 0.045f;
                float m = Screen.height * 0.015f;
                return new Rect(Screen.width - w - m, Screen.height - h - m, w, h);
            }
        }

        /// <summary>スキルのボタン（画面左下に3つ）。画面座標（左下原点）。</summary>
        Rect SkillRect(int slot)
        {
            float s = Mathf.Min(Screen.width, Screen.height) * 0.17f;
            float m = Screen.height * 0.015f;
            float gap = s * 0.15f;
            return new Rect(m + slot * (s + gap), m + Screen.height * 0.04f, s, s);
        }

        /// <summary>スキルのボタンの上の細い帯（タップでオート／手動を切り替え。緑ならオート）。</summary>
        Rect SkillAutoRect(int slot)
        {
            var r = SkillRect(slot);
            return new Rect(r.x, r.yMax + r.height * 0.05f, r.width, r.height * 0.16f);
        }

        void DrawSkillButtons(CharacterData ch)
        {
            if (ch == null) return;
            var player = World.Player;
            var cost = new GUIStyle(small) { alignment = TextAnchor.LowerRight, fontStyle = FontStyle.Bold, fontSize = Mathf.Max(9, Mathf.RoundToInt(small.fontSize * 0.8f)) };
            for (int i = 0; i < SkillInfo.Slots; i++)
            {
                int slot = i;
                int index = ch.Slots[i];
                var r = ToGui(SkillRect(i));
                if (index < 0 || !SkillInfo.Learned(ClassInfo.Current, index, ch.Level))
                {
                    Button(r, null, new Color(0f, 0f, 0f, 0.3f), centered, () => World.OpenPanel(false, false, true));
                    continue;
                }

                var spec = SkillInfo.Get(ClassInfo.Current, index);
                bool enoughMp = player.Mp >= spec.Mp;
                Button(r, null, new Color(0f, 0f, 0f, 0.55f), centered, () => World.Skills.Press(slot, World.Player));
                float inset = r.width * 0.12f;
                var iconRect = new Rect(r.x + inset, r.y + inset, r.width - inset * 2f, r.height - inset * 2f);
                var old = GUI.color;
                GUI.color = enoughMp ? Color.white : new Color(1f, 0.45f, 0.45f, 0.6f);
                DrawSprite(iconRect, PixelArt.SkillIcon(spec.Effect, spec.Color));
                GUI.color = old;

                // 待ち時間（上から暗くする）
                float cd = World.Skills.CooldownRatio(i);
                if (cd > 0f) Fill(new Rect(r.x, r.y, r.width, r.height * cd), new Color(0f, 0f, 0f, 0.6f));

                // 消費 MP（右下に小さく）
                cost.normal.textColor = enoughMp ? new Color(0.65f, 0.8f, 1f) : new Color(1f, 0.5f, 0.5f);
                GUI.Label(new Rect(r.x, r.y, r.width - r.width * 0.06f, r.height - r.height * 0.02f), spec.Mp.ToString(), cost);

                // オート／手動の帯
                bool auto = ch.SlotAuto[i];
                Button(ToGui(SkillAutoRect(i)), null, auto ? new Color(0.3f, 0.85f, 0.4f, 0.85f) : new Color(1f, 1f, 1f, 0.15f), centered, () =>
                {
                    ch.SlotAuto[slot] = !ch.SlotAuto[slot];
                    Profile.Save();
                });
            }

            // かかっている強化
            float y = Screen.height - SkillAutoRect(0).yMax - small.fontSize * 1.6f * (player.BuffCount + 0.2f);
            for (int b = 0; b < player.BuffCount; b++)
            {
                GUI.Label(new Rect(Screen.height * 0.015f, y, Screen.width * 0.6f, small.fontSize * 1.5f),
                    $"{player.BuffName(b)}  {player.BuffRemaining(b):0.0}s", small);
                y += small.fontSize * 1.5f;
            }
        }

        /// <summary>薬のボタン（右下、攻撃ボタンの上に2つ）。画面座標（左下原点）。</summary>
        Rect PotionRect(int i)
        {
            float s = Mathf.Min(Screen.width, Screen.height) * 0.13f;
            float m = Screen.height * 0.015f;
            var atk = AttackRect;
            return new Rect(Screen.width - m - s - i * (s + s * 0.15f), atk.yMax + m, s, s);
        }

        void DrawPotions(CharacterData ch)
        {
            if (ch == null) return;
            var style = new GUIStyle(centered) { fontStyle = FontStyle.Bold };
            for (int i = 0; i < 2; i++)
            {
                bool hp = i == 0;
                int count = hp ? ch.HpPotions : ch.MpPotions;
                var r = ToGui(PotionRect(i));
                var c = hp ? new Color(0.75f, 0.2f, 0.25f, 0.7f) : new Color(0.2f, 0.35f, 0.8f, 0.7f);
                if (count <= 0) c = new Color(0f, 0f, 0f, 0.3f);
                string key = Application.isMobilePlatform ? "" : (hp ? "Q " : "E ");
                Button(r, $"{key}{(hp ? "HP" : "MP")}\n{count}", c, style, () => World.UsePotion(hp));
                float cd = World.PotionCooldownRatio;
                if (cd > 0f) Fill(new Rect(r.x, r.y, r.width, r.height * cd), new Color(0f, 0f, 0f, 0.5f));
            }
        }

        public Rect AttackRect
        {
            get
            {
                float s = Mathf.Min(Screen.width, Screen.height) * 0.24f;
                float m = Screen.height * 0.03f;
                return new Rect(Screen.width - s - m, m + Screen.height * 0.04f, s, s);
            }
        }

        public bool IsOverButton(Vector2 screenPos)
        {
            foreach (var r in buttonRects)
            {
                if (r.Contains(screenPos)) return true;
            }
            return false;
        }

        /// <summary>短く触ったとき。ボタンの上なら押したことにする。</summary>
        public bool HandleTap(Vector2 screenPos)
        {
            for (int i = buttonRects.Count - 1; i >= 0; i--)
            {
                if (!buttonRects[i].Contains(screenPos)) continue;
                buttonActions[i]?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>「MISS」を出す（攻撃が外れた・避けた）。</summary>
        public void AddMiss(Vector2 world, bool playerEvaded)
        {
            if (numbers.Count >= MaxNumbers) numbers.RemoveAt(0);
            numbers.Add(new FloatNumber
            {
                World = Iso.ToScreen(world) + new Vector2(UnityEngine.Random.Range(-0.1f, 0.1f), playerEvaded ? 0.7f : 0.4f),
                Text = "MISS",
                Color = playerEvaded ? new Color(0.5f, 0.95f, 1f) : new Color(0.7f, 0.7f, 0.75f),
                Big = false,
            });
        }

        public void AddNumber(Vector2 world, float amount, bool crit)
        {
            if (numbers.Count >= MaxNumbers) numbers.RemoveAt(0);
            numbers.Add(new FloatNumber
            {
                World = Iso.ToScreen(world) + new Vector2(UnityEngine.Random.Range(-0.15f, 0.15f), 0.4f),
                Text = Mathf.RoundToInt(amount).ToString(),
                Color = crit ? new Color(1f, 0.85f, 0.2f) : Color.white,
                Big = crit,
            });
        }

        float levelUpTime = -1f;

        // 加護を得たときの表示（画面の上寄りにアイコン・名前・説明。少しして消える）
        const float BoonGetLife = 2.5f;
        int boonGetIndex;
        int boonGetStacks;
        float boonGetTime = -1f;

        public void ShowBoonGet(int boon, int stacks)
        {
            boonGetIndex = boon;
            boonGetStacks = stacks;
            boonGetTime = 0f;
        }

        void DrawBoonGet()
        {
            if (boonGetTime < 0f) return;
            float t = boonGetTime;
            float alpha = Mathf.Clamp01(t / 0.15f) * Mathf.Clamp01((BoonGetLife - t) / 0.5f);
            float W = Screen.width, H = Screen.height;
            var spec = Boons.Get(boonGetIndex);
            float boxW = W * 0.84f, boxH = H * 0.09f;
            float pop = 1f + 0.08f * Mathf.Clamp01(1f - t / 0.2f);
            var box = new Rect((W - boxW * pop) * 0.5f, H * 0.2f, boxW * pop, boxH * pop);
            Fill(box, new Color(0.08f, 0.06f, 0.12f, 0.85f * alpha));
            Fill(new Rect(box.x, box.y, box.width, box.height * 0.04f), new Color(1f, 0.8f, 0.35f, alpha));
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            float icon = box.height * 0.75f;
            DrawSprite(new Rect(box.x + box.height * 0.15f, box.y + box.height * 0.13f, icon, icon), PixelArt.BoonIcon(boonGetIndex));
            GUI.color = old;
            float tx = box.x + box.height * 0.15f + icon + box.width * 0.03f;
            float tw = box.xMax - tx - box.width * 0.03f;
            var title = new GUIStyle(label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            title.normal.textColor = new Color(1f, 0.88f, 0.5f, alpha);
            string n = boonGetStacks > 1 ? $"{spec.DisplayName} x{boonGetStacks}" : spec.DisplayName;
            FitLabel(new Rect(tx, box.y + box.height * 0.08f, tw, box.height * 0.45f), Loc.T("Boon: ", "加護：") + n, title);
            var desc = new GUIStyle(small) { alignment = TextAnchor.MiddleLeft };
            desc.normal.textColor = new Color(1f, 1f, 1f, alpha);
            FitLabel(new Rect(tx, box.y + box.height * 0.5f, tw, box.height * 0.42f), spec.DisplayDesc, desc);
        }

        /// <summary>頭の上の羽の間に「LV UP」を出す（羽と一緒に上がりながら消える）。</summary>
        public void ShowLevelUp() => levelUpTime = 0f;

        void DrawLevelUp()
        {
            if (levelUpTime < 0f) return;
            var cam = Camera.main;
            if (cam == null) return;
            float t = levelUpTime;
            float rise = t > 1f ? (t - 1f) * 0.8f : 0f;
            float alpha = t > 1f ? 1f - (t - 1f) / (FxSystem.LevelUpDuration - 1f) : Mathf.Clamp01(t / 0.15f);
            var head = Iso.ToScreen(World.Player.Pos) + new Vector2(0f, 0.8f + rise); // 羽の付け根と同じ高さ
            Vector3 sp = cam.WorldToScreenPoint(new Vector3(head.x, head.y, 0f));
            var style = new GUIStyle(number) { fontSize = Mathf.Max(9, Mathf.RoundToInt(number.fontSize * 0.7f)) }; // 羽の間に収まる小ささ
            float pop = 1f + 0.15f * Mathf.Clamp01(1f - t / 0.2f); // 出た瞬間だけ少し大きく
            style.fontSize = Mathf.RoundToInt(style.fontSize * pop);
            float w = style.fontSize * 8f, h = style.fontSize * 1.6f;
            var r = new Rect(sp.x - w * 0.5f, Screen.height - sp.y - h * 0.5f, w, h);
            style.normal.textColor = new Color(0.25f, 0.15f, 0f, alpha * 0.8f); // 影
            GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), "LV UP", style);
            style.normal.textColor = new Color(1f, 0.88f, 0.35f, alpha);
            GUI.Label(r, "LV UP", style);
        }

        string bannerTitle;
        string bannerSub;
        Color bannerColor;
        float bannerTime = -1f;
        const float BannerLife = 3.5f;

        /// <summary>画面の中央に大きく出す（新しい区画に入ったとき）。</summary>
        public void ShowBanner(string title, string subtitle, Color color)
        {
            bannerTitle = title;
            bannerSub = subtitle;
            bannerColor = color;
            bannerTime = 0f;
        }

        void DrawBanner()
        {
            if (bannerTime < 0f || bannerTime > BannerLife) return;
            float a = Mathf.Clamp01(bannerTime / 0.4f) * Mathf.Clamp01((BannerLife - bannerTime) / 0.8f);
            float H = Screen.height, W = Screen.width;
            Fill(new Rect(0, H * 0.3f, W, H * 0.11f), new Color(0f, 0f, 0f, 0.55f * a));
            var t = new GUIStyle(centered) { fontSize = Mathf.RoundToInt(label.fontSize * 1.5f), fontStyle = FontStyle.Bold };
            var c = bannerColor;
            c.a = a;
            t.normal.textColor = c;
            GUI.Label(new Rect(0, H * 0.305f, W, H * 0.06f), bannerTitle, t);
            var st = new GUIStyle(centered);
            st.normal.textColor = new Color(1f, 1f, 1f, 0.85f * a);
            GUI.Label(new Rect(0, H * 0.36f, W, H * 0.04f), bannerSub, st);
        }

        public void Toast(string text, Color color)
        {
            if (toasts.Count >= 5) toasts.RemoveAt(0);
            toasts.Add(new ToastLine { Text = text, Color = color });
        }

        void Awake()
        {
            bag = new BagScreen(this);
            stats = new StatsScreen(this);
            skills = new SkillsScreen(this);
            returnScreen = new ReturnScreen(this);
            menu = new MenuScreen(this);
            altarScreen = new AltarScreen(this);
            Guide = new GuideBox(this);
            Town = new TownScreen(this);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt > 0f) fps = Mathf.Lerp(fps, 1f / dt, 0.05f);
            if (bannerTime >= 0f) bannerTime += dt;
            if (boonGetTime >= 0f && World != null && !World.Paused && (boonGetTime += dt) >= BoonGetLife) boonGetTime = -1f;
            if (levelUpTime >= 0f && World != null && !World.Paused && (levelUpTime += dt) >= FxSystem.LevelUpDuration) levelUpTime = -1f;

            for (int i = numbers.Count - 1; i >= 0; i--)
            {
                numbers[i].T += dt;
                if (numbers[i].T >= NumberLife) numbers.RemoveAt(i);
            }
            for (int i = toasts.Count - 1; i >= 0; i--)
            {
                toasts[i].T += dt;
                if (toasts[i].T >= ToastLife) toasts.RemoveAt(i);
            }
        }

        void OnGUI()
        {
            if (World == null || World.Player == null) return;
            EnsureStyles();

            bool repaint = Event.current.type == EventType.Repaint;
            if (repaint)
            {
                pendingRects.Clear();
                pendingActions.Clear();
            }

            // 鞄・能力値・職業選択の画面を開いている間は、後ろに透けないよう通常の表示を出さない
            if (!World.Paused && !World.ChoosingClass)
            {
                DrawNumbers();
                DrawLevelUp();
                DrawBoonGet();
                DrawEliteLabels();
                DrawHud();
            }

            if (World.OnTitle) DrawTitle();
            else if (World.ChoosingClass && World.PendingClass.HasValue) DrawFloorSelect(World.PendingClass.Value);
            else if (World.ChoosingClass && World.InTown && World.StatsOpen) stats.Draw(World);
            else if (World.ChoosingClass && World.InTown && World.SkillsOpen) skills.Draw(World);
            else if (World.ChoosingClass && World.InTown) Town.Draw(World);
            else if (World.ChoosingClass) DrawClassSelect();
            else if (World.Player.IsDead) DrawGameOver();
            else if (World.BagOpen) bag.Draw(World);
            else if (World.StatsOpen) stats.Draw(World);
            else if (World.SkillsOpen) skills.Draw(World);
            else if (World.ReturnOpen) returnScreen.Draw(World);
            else if (World.MenuOpen) menu.Draw(World);
            else if (World.AltarOpen) altarScreen.Draw(World);
            if (!World.ChoosingClass || World.InTown) Guide.Draw();

            if (repaint)
            {
                // 描き終えたボタンの位置を、次のタップの判定に使う
                buttonRects.Clear();
                buttonRects.AddRange(pendingRects);
                buttonActions.Clear();
                buttonActions.AddRange(pendingActions);
            }
        }

        void DrawHud()
        {
            var player = World.Player;
            var inv = World.Inventory;
            float m = Screen.height * 0.015f;

            // 体力と MP（数字はバーの中に）
            float barW = Screen.width * 0.5f;
            var hpBack = new Rect(m, m, barW, Screen.height * 0.024f);
            Fill(hpBack, new Color(0f, 0f, 0f, 0.6f));
            float ratio = player.MaxHp > 0 ? (float)player.Hp / player.MaxHp : 0f;
            Fill(new Rect(hpBack.x, hpBack.y, hpBack.width * ratio, hpBack.height), new Color(0.85f, 0.2f, 0.25f, 0.95f));
            var barText = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = Mathf.Max(9, Mathf.RoundToInt(small.fontSize * 0.85f)) };
            GUI.Label(hpBack, $"{player.Hp} / {player.MaxHp}", barText);

            var mpBack = new Rect(m, hpBack.yMax + m * 0.25f, barW, hpBack.height * 0.75f);
            Fill(mpBack, new Color(0f, 0f, 0f, 0.6f));
            float mpRatio = player.MaxMp > 0 ? (float)player.Mp / player.MaxMp : 0f;
            Fill(new Rect(mpBack.x, mpBack.y, mpBack.width * mpRatio, mpBack.height), new Color(0.3f, 0.5f, 1f, 0.95f));
            GUI.Label(mpBack, $"{player.Mp} / {player.MaxMp}", barText);

            // 経験値と回避（細い線）
            var ch = Profile.Current;
            var expBack = new Rect(m, mpBack.yMax + m * 0.25f, barW, hpBack.height * 0.22f);
            Fill(expBack, new Color(0f, 0f, 0f, 0.6f));
            if (ch != null)
            {
                float expRatio = Mathf.Clamp01((float)ch.Exp / Progression.ExpToNext(ch.Level));
                Fill(new Rect(expBack.x, expBack.y, expBack.width * expRatio, expBack.height), new Color(1f, 0.85f, 0.3f, 0.9f));
            }
            var dodge = new Rect(m, expBack.yMax + m * 0.2f, barW * 0.4f, expBack.height);
            Fill(dodge, new Color(0f, 0f, 0f, 0.6f));
            Fill(new Rect(dodge.x, dodge.y, dodge.width * player.DodgeReady, dodge.height), new Color(0.75f, 0.85f, 1f, 0.9f));

            // レベルと層だけを1行で
            float lineH = small.fontSize * 1.35f;
            float textW = MenuRect.x - m * 2f;
            float y = dodge.yMax + m * 0.4f;
            int level = ch != null ? ch.Level : 1;
            string lvText = ch != null && ch.Level >= Progression.MaxLevel ? $"Lv {level} (MAX)" : $"Lv {level}";
            GUI.Label(new Rect(m, y, textW, lineH), Loc.T($"{lvText}    FLOOR {World.Floors.Floor}", $"{lvText}    {World.Floors.Floor}層") + (World.Floors.IsBossFloor ? Loc.T("  BOSS", "  ボス") : ""), small);
            y += lineH;
            if (player.LightPenalty > 0f)
            {
                var drained = new GUIStyle(small) { fontStyle = FontStyle.Bold };
                drained.normal.textColor = new Color(0.8f, 0.6f, 1f);
                GUI.Label(new Rect(m, y, textW, lineH), Loc.T("Light drained!", "灯りを奪われている！"), drained);
                y += lineH;
            }

            // 拾った品の通知
            float ty = y + lineH * 0.4f;
            foreach (var t in toasts)
            {
                var c = t.Color;
                c.a = Mathf.Clamp01((ToastLife - t.T) / 0.5f);
                var style = new GUIStyle(small);
                style.normal.textColor = c;
                GUI.Label(new Rect(m, ty, Screen.width, lineH), "+ " + t.Text, style);
                ty += small.fontSize * 1.3f;
            }

            DrawBossBar();
            DrawMinimap();
            DrawBanner();

            if (World.Paused || player.IsDead || World.ChoosingClass) return;

            // 右上は MENU だけ（ポイントが余っている・鞄が満杯なら「!」）
            int points = ch != null ? ch.Points : 0;
            bool notice = points > 0 || inv.BagFull;
            Button(ToGui(MenuRect), notice ? Loc.T("MENU !", "メニュー !") : Loc.T("MENU", "メニュー"), notice ? new Color(0.5f, 0.4f, 0.05f, 0.75f) : new Color(0f, 0f, 0f, 0.55f), centered,
                () => World.OpenMenu());

            DrawSkillButtons(ch);
            DrawPotions(ch);

            // 攻撃ボタン（押している間だけ、のとき）
            if (!World.AutoAttack)
            {
                var atk = ToGui(AttackRect);
                Fill(atk, World.Input.AttackHeld ? new Color(1f, 0.6f, 0.2f, 0.55f) : new Color(1f, 1f, 1f, 0.2f));
                GUI.Label(atk, "ATK", centered);
            }

            // 仮想スティック
            var input = World.Input;
            if (input.StickActive)
            {
                float r = input.StickRadius;
                Fill(ToGui(new Rect(input.StickOrigin.x - r, input.StickOrigin.y - r, r * 2f, r * 2f)), new Color(1f, 1f, 1f, 0.12f));
                float k = r * 0.35f;
                Fill(ToGui(new Rect(input.StickKnob.x - k, input.StickKnob.y - k, k * 2f, k * 2f)), new Color(1f, 1f, 1f, 0.4f));
            }

            // 操作の説明（PC・エディタ向け）。ふだんは「H: Controls」だけ出し、H で一覧を開く
            if (!Application.isMobilePlatform)
            {
                var hint = new GUIStyle(small) { alignment = TextAnchor.LowerRight };
                hint.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
                GUI.Label(new Rect(0, Screen.height - lineH * 1.3f, Screen.width - m, lineH), ShowFps ? $"FPS {fps:0}   " + Loc.T("H: Controls", "H：操作") : Loc.T("H: Controls", "H：操作"), hint);
                if (ShowHelp) DrawHelp();
            }
        }

        /// <summary>操作の一覧（H で開閉）。</summary>
        public bool ShowHelp;

        /// <summary>FPS を表示する（メニューの設定）。</summary>
        public bool ShowFps;

        static readonly string[] helpLines =
        {
            "Move ........ WASD / arrows (phone: drag)",
            "Dodge ....... Space (phone: flick)",
            "Skills ...... 1 / 2 / 3",
            "Attack mode . T  (HOLD: J or right click)",
            "Bag ......... B      Stats ... C",
            "Skill list .. K      Music ... M (on/off)  N (change)",
            "Potions ..... Q (HP)   E (MP)",
            "Menu ........ Esc (also closes screens)",
        };

        static readonly string[] helpLinesJa =
        {
            "移動 ........ WASD / 矢印（スマホ：押してずらす）",
            "回避 ........ Space（スマホ：はじく）",
            "スキル ...... 1 / 2 / 3",
            "攻撃モード .. T（押す間：J か右クリック）",
            "鞄 .......... B      能力値 ... C",
            "スキル一覧 .. K      音楽 ... M（オン/オフ）  N（変える）",
            "薬 .......... Q（体力）  E（MP）",
            "メニュー .... Esc（画面を閉じるのも）",
        };

        void DrawHelp()
        {
            var helpLines = Loc.Ja ? helpLinesJa : Hud.helpLines;
            float w = Screen.width * 0.86f;
            float lh = small.fontSize * 1.45f;
            float h = lh * (helpLines.Length + 1.2f);
            var r = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.35f, w, h);
            Fill(r, new Color(0f, 0f, 0f, 0.8f));
            for (int i = 0; i < helpLines.Length; i++)
            {
                GUI.Label(new Rect(r.x + lh * 0.6f, r.y + lh * (0.6f + i), r.width, lh), helpLines[i], small);
            }
        }

        /// <summary>小さな地図（右上のボタンの下）。歩いて見た場所、自分、階段。</summary>
        void DrawMinimap()
        {
            var room = World.Room;
            if (room.Minimap == null) return;
            var below = ToGui(MenuRect);
            float w = Screen.width * 0.34f;
            float h = w * Room.MiniH / Room.MiniW;
            var r = new Rect(Screen.width - w - Screen.height * 0.015f, below.yMax + Screen.height * 0.01f, w, h);
            Fill(r, new Color(0f, 0f, 0f, 0.45f));
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.85f);
            GUI.DrawTexture(r, room.Minimap);
            GUI.color = old;

            float sx = w / Room.MiniW, sy = h / Room.MiniH;
            Rect Dot(Vector2 logical, float size)
            {
                Vector2 m = room.MinimapPixel(logical);
                return new Rect(r.x + m.x * sx - size * 0.5f, r.yMax - m.y * sy - size * 0.5f, size, size);
            }
            float s = Mathf.Max(3f, w * 0.035f);
            if (World.Floors.StairsOpen) Fill(Dot(room.StairsPos, s * 1.3f), new Color(1f, 0.8f, 0.3f));
            if (room.HasAltar && !World.AltarUsed) Fill(Dot(room.AltarPos, s), new Color(1f, 0.5f, 0.2f));
            Fill(Dot(World.Player.Pos, s), Color.white);
        }

        /// <summary>ボスがいる間、画面の下に体力バーを出す。</summary>
        void DrawBossBar()
        {
            var boss = World.Enemies.Boss;
            if (boss == null) return;
            float w = Screen.width * 0.8f;
            float h = Screen.height * 0.018f;
            var r = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.72f, w, h);
            Fill(r, new Color(0f, 0f, 0f, 0.7f));
            Fill(new Rect(r.x, r.y, r.width * Mathf.Clamp01(boss.Hp / boss.MaxHp), r.height), new Color(0.9f, 0.2f, 0.25f, 0.95f));
            GUI.Label(new Rect(r.x, r.y - small.fontSize * 1.5f, r.width, small.fontSize * 1.4f), Loc.T($"BOSS  -  FLOOR {World.Floors.Floor}", $"ボス  -  {World.Floors.Floor}層"), small);
        }

        /// <summary>精鋭の頭の上に、名前と小さな体力バー。</summary>
        void DrawEliteLabels()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var style = new GUIStyle(small) { alignment = TextAnchor.LowerCenter, fontStyle = FontStyle.Bold };
            foreach (var e in World.Enemies.Active)
            {
                if (e.Dying || e.Elite == EliteKind.None) continue;
                Vector2 head = Iso.ToScreen(e.Pos) + new Vector2(0f, 0.55f * e.BaseScale);
                Vector3 sp = cam.WorldToScreenPoint(new Vector3(head.x, head.y, 0f));
                if (sp.z < 0f) continue;
                float w = small.fontSize * 6f;
                float gy = Screen.height - sp.y;
                style.normal.textColor = e.Tint;
                GUI.Label(new Rect(sp.x - w * 0.5f, gy - small.fontSize * 1.8f, w, small.fontSize * 1.4f), EnemySystem.EliteName(e.Elite), style);
                var bar = new Rect(sp.x - w * 0.3f, gy - small.fontSize * 0.35f, w * 0.6f, Mathf.Max(3f, small.fontSize * 0.25f));
                Fill(bar, new Color(0f, 0f, 0f, 0.7f));
                Fill(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(e.Hp / e.MaxHp), bar.height), e.Tint);
            }
        }

        void DrawNumbers()
        {
            var cam = Camera.main;
            if (cam == null) return;
            foreach (var n in numbers)
            {
                float t = n.T / NumberLife;
                Vector3 sp = cam.WorldToScreenPoint(new Vector3(n.World.x, n.World.y + t * 0.6f, 0f));
                var style = n.Big ? big : number;
                var c = n.Color;
                c.a = 1f - t * t;
                style.normal.textColor = c;
                float w = style.fontSize * 4f;
                float h = style.fontSize * 1.5f;
                GUI.Label(new Rect(sp.x - w * 0.5f, Screen.height - sp.y - h * 0.5f, w, h), n.Text, style);
            }
            number.normal.textColor = Color.white;
        }

        void DrawGameOver()
        {
            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.6f));
            float h = Screen.height;
            var title = new GUIStyle(centered) { fontSize = label.fontSize * 3, fontStyle = FontStyle.Bold };
            Hud.FitLabel(new Rect(0, h * 0.38f, Screen.width, h * 0.08f), Loc.T("GAME OVER", "力尽きた"), title);
            GUI.Label(new Rect(0, h * 0.47f, Screen.width, h * 0.05f),
                Loc.T($"FLOOR {World.Floors.Floor}   KILL {World.Enemies.Kills}   EMBERS {World.Inventory.Embers}", $"{World.Floors.Floor}層   撃破 {World.Enemies.Kills}   火種 {World.Inventory.Embers}"), centered);
            GUI.Label(new Rect(0, h * 0.53f, Screen.width, h * 0.05f), Loc.T("Tap / Enter to continue", "タップか Enter で続ける"), centered);
        }

        static readonly string[] greetingsEn =
        {
            "Welcome, bearer of light. Ready to head down the tower?",
            "Pick a class. Lampwright fights from afar, Shadeblade up close, Warden burns all around.",
            "Embers become boons at the altars. Hold back, and you will not get deep.",
            "Beat a boss and you can start beyond it. Remember that.",
        };

        static readonly string[] greetings =
        {
            "ようこそ、灯りを持つ者。塔の底へ下りる準備はいいかい？",
            "職業を選びな。灯術師は遠くから、影刃は近くで、灯番は周りを焼く。",
            "火種は祭壇で加護に変えられる。ケチると深くは行けないよ。",
            "ボスを倒せば、その先から始められる。覚えておきな。",
        };

        string GuideGreeting()
        {
            var list = Loc.Ja ? greetings : greetingsEn;
            int i = (int)(Time.unscaledTime / 6f) % list.Length;
            return list[i];
        }

        /// <summary>開始する層を選ぶ画面（1層と、倒したボスの次の層）。PC は数字キーでも選べる。</summary>
        void DrawFloorSelect(ClassType type)
        {
            float W = Screen.width;
            float H = Screen.height;
            Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.8f));
            var ch = Profile.Get(type);
            var spec = ClassInfo.Get(type);

            var title = new GUIStyle(centered) { fontSize = label.fontSize * 2, fontStyle = FontStyle.Bold };
            Hud.FitLabel(new Rect(0, H * 0.12f, W, H * 0.06f), Loc.T("Choose a starting floor", "始める層を選ぶ"), title);
            GUI.Label(new Rect(0, H * 0.19f, W, H * 0.04f), Loc.T($"{AdvancedClasses.NameOf(type)}   Lv {ch.Level}   Best floor {ch.BestFloor}", $"{AdvancedClasses.NameOf(type)}   Lv {ch.Level}   最深 {ch.BestFloor}層"), centered);

            var floors = GameWorld.Checkpoints(ch);
            const int cols = 3;
            float pad = W * 0.08f;
            float gap = W * 0.03f;
            float bw = (W - pad * 2f - gap * (cols - 1)) / cols;
            float bh = H * 0.07f;
            float y = H * 0.27f;

            // 中断した挑戦があれば、いちばん上に「続きから」
            if (ch.Run.Active)
            {
                Button(new Rect(pad, y, W - pad * 2f, bh), Loc.T($"CONTINUE  -  Floor {ch.Run.Floor}", $"続きから  -  {ch.Run.Floor}層"), new Color(0.2f, 0.5f, 0.25f, 0.85f), centered,
                    () => confirmFloor = 0); // 0 は「続きから」
                y += bh + gap;
                var warn = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter };
                warn.normal.textColor = new Color(1f, 0.7f, 0.5f);
                GUI.Label(new Rect(0, y, W, small.fontSize * 1.5f), Loc.T("Starting a new run abandons the suspended one.", "新しく始めると、中断中の挑戦は消えます。"), warn);
                y += small.fontSize * 2f;
            }
            for (int i = 0; i < floors.Count; i++)
            {
                int floor = floors[i];
                var r = new Rect(pad + (i % cols) * (bw + gap), y + (i / cols) * (bh + gap), bw, bh);
                string key = Application.isMobilePlatform || i >= 9 ? "" : $"[{i + 1}] ";
                Button(r, Loc.T($"{key}Floor {floor}", $"{key}{floor}層"), i == floors.Count - 1 ? new Color(0.5f, 0.4f, 0.1f, 0.7f) : new Color(1f, 1f, 1f, 0.12f), centered,
                    () => confirmFloor = floor);
            }
            int rows = (floors.Count + cols - 1) / cols;
            var back = new Rect(pad, y + rows * (bh + gap) + gap, W - pad * 2f, H * 0.05f);
            Button(back, Loc.T("BACK", "戻る"), new Color(1f, 1f, 1f, 0.08f), centered, () => World.CancelFloorSelect());

            if (confirmFloor >= 0) DrawFloorConfirm(type, ch);
        }

        /// <summary>層を選んだときの確認（-1 は確認なし、0 は「続きから」、1以上は始める層）。</summary>
        int confirmFloor = -1;

        /// <summary>PC の数字キーで層を選んだとき（GameWorld から）。確認を出す。</summary>
        public void AskFloor(int floor) => confirmFloor = floor;

        public bool ConfirmingFloor => confirmFloor >= 0;

        void DrawFloorConfirm(ClassType type, CharacterData ch)
        {
            float W = Screen.width, H = Screen.height;
            // 後ろのボタンを押せないよう、全体をふさぐ（外を押すとやめる）
            Button(new Rect(0, 0, W, H), null, new Color(0f, 0f, 0f, 0.6f), centered, () => confirmFloor = -1);
            var box = new Rect(W * 0.08f, H * 0.36f, W * 0.84f, H * 0.24f);
            Fill(box, new Color(0.1f, 0.08f, 0.14f, 0.97f));
            Fill(new Rect(box.x, box.y, box.width, box.height * 0.03f), new Color(1f, 0.8f, 0.35f));
            Button(box, null, new Color(0f, 0f, 0f, 0f), centered, () => { }); // 箱の中を押しても閉じない

            bool resume = confirmFloor == 0;
            int floor = resume ? ch.Run.Floor : confirmFloor;
            var title = new GUIStyle(label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            title.normal.textColor = new Color(1f, 0.88f, 0.5f);
            FitLabel(new Rect(box.x, box.y + box.height * 0.1f, box.width, box.height * 0.2f),
                resume ? Loc.T($"Continue from floor {floor}?", $"{floor}層から続けますか？") : Loc.T($"Start from floor {floor}?", $"{floor}層から始めますか？"), title);
            var note = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
            note.normal.textColor = new Color(1f, 0.7f, 0.5f);
            string warn = !resume && ch.Run.Active ? Loc.T("The suspended run will be lost.", "中断中の挑戦は消えます。") : "";
            GUI.Label(new Rect(box.x + box.width * 0.05f, box.y + box.height * 0.32f, box.width * 0.9f, box.height * 0.22f), warn, note);

            float bw = box.width * 0.4f, bh = box.height * 0.26f;
            float by = box.yMax - bh - box.height * 0.1f;
            Button(new Rect(box.x + box.width * 0.07f, by, bw, bh), Loc.T("START", "始める"), new Color(0.2f, 0.5f, 0.25f, 0.9f), centered, () =>
            {
                confirmFloor = -1;
                if (resume) World.ResumeRun(type);
                else World.StartRun(type, floor);
            });
            Button(new Rect(box.xMax - box.width * 0.07f - bw, by, bw, bh), Loc.T("CANCEL", "やめる"), new Color(1f, 1f, 1f, 0.15f), centered, () => confirmFloor = -1);
        }

        /// <summary>
        /// タイトル画面。上に副題とロゴ（VIVID は虹色に光り、FADE は崩れて粒が舞い散る）。
        /// 背景は Resources/TitleBackground の画像（なければコードで描いた霧の洞窟）。左の崖に灯術師が後ろ姿で立ち、漂う塵。
        /// どこを押しても始まる（言語のボタンだけは別）。
        /// </summary>
        void DrawTitle()
        {
            float W = Screen.width;
            float H = Screen.height;
            float t = Time.unscaledTime;
            Button(new Rect(0, 0, W, H), null, new Color(0f, 0f, 0f, 0f), centered, World.LeaveTitle);

            // 背景：Resources/TitleBackground の画像があればそれを画面いっぱいに（はみ出す分は切る）。なければコードで描く
            if (!titleBgLoaded)
            {
                titleBg = Resources.Load<Texture2D>("TitleBackground");
                titleBgLoaded = true;
            }
            if (titleBg != null)
            {
                float sc0 = Mathf.Max(W / titleBg.width, H / titleBg.height);
                var bg = new Rect((W - titleBg.width * sc0) * 0.5f, (H - titleBg.height * sc0) * 0.5f, titleBg.width * sc0, titleBg.height * sc0);
                GUI.DrawTexture(bg, titleBg);
                // 灯術師は画像の中の位置に合わせて立たせる（画像の左上が 0,0、右下が 1,1）
                float lw = bg.width * TitleHeroWidth, lh = lw * 56f / 40f;
                DrawTitleHero(new Rect(bg.x + bg.width * TitleHeroX, bg.y + bg.height * TitleHeroFeetY - lh * 52f / 56f, lw, lh), t);
            }
            else DrawTitleScene(W, H, t);

            // 漂う塵（霧の中でゆっくり舞う）
            for (int i = 0; i < 30; i++)
            {
                float life = Mathf.Repeat(t * (0.02f + 0.03f * Hash01(i * 3 + 2)) + Hash01(i * 11 + 7), 1f);
                float ex = Hash01(i * 5 + 3) * W + Mathf.Sin(t * 0.5f + i) * W * 0.03f;
                float ey = H * (0.9f - 0.7f * life);
                float a = Mathf.Sin(life * Mathf.PI) * 0.5f;
                Fill(new Rect(ex, ey, 2f, 2f), new Color(0.85f, 0.88f, 0.95f, a));
            }

            // 下側を暗くして文字を読みやすく
            for (int b = 0; b < 8; b++)
                Fill(new Rect(0, H * (0.82f + b * 0.0225f), W, H * 0.0225f + 1f), new Color(0f, 0f, 0f, 0.08f * (b + 1)));

            // 副題（上に線と一緒に）
            float ly = H * 0.05f, lineW = W * 0.16f;
            Fill(new Rect(W * 0.5f - W * 0.22f - lineW, ly, lineW, 2f), new Color(0.85f, 0.8f, 0.9f, 0.6f));
            Fill(new Rect(W * 0.5f + W * 0.22f, ly, lineW, 2f), new Color(0.85f, 0.8f, 0.9f, 0.6f));
            var sub = new GUIStyle(centered);
            sub.normal.textColor = new Color(0.9f, 0.85f, 0.95f, 0.9f);
            FitLabel(new Rect(W * 0.28f, ly - H * 0.017f, W * 0.44f, H * 0.035f), Loc.T("Into the tower where light fades", "灯りの消えゆく塔へ"), sub);

            // ロゴ：VIVID のまわりの光、本体、FADE から崩れて舞い散る粒
            var logoTex = TitleArt.Logo;
            float sc = Mathf.Max(1f, Mathf.Floor(W * 0.95f / logoTex.width));
            var logoRect = new Rect((W - logoTex.width * sc) * 0.5f, H * 0.085f, logoTex.width * sc, logoTex.height * sc);
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.75f + 0.25f * Mathf.Sin(t * 2f));
            GUI.DrawTexture(new Rect(logoRect.x - sc, logoRect.y - sc, logoRect.width + sc * 2f, logoRect.height + sc * 2f), TitleArt.Glow);
            GUI.color = old;
            GUI.DrawTexture(logoRect, logoTex);
            var crumbs = TitleArt.Crumbs;
            for (int i = 0; i < crumbs.Count; i++)
            {
                float phase = Mathf.Repeat(t * 0.16f + Hash01(i * 31 + 3), 1f);
                float ox = logoRect.x + crumbs[i].x * sc, oy = logoRect.y + crumbs[i].y * sc;
                float dx = phase * W * 0.12f * (0.5f + Hash01(i * 7 + 2));
                float dy = phase * phase * H * 0.1f + phase * H * 0.01f;
                float a = phase < 0.08f ? phase / 0.08f : 1f - phase;
                Fill(new Rect(ox + dx, oy + dy, sc, sc), new Color(0.6f, 0.58f, 0.65f, a * 0.9f));
            }

            // 点滅する「タップで始める」
            var tap = new GUIStyle(centered) { fontStyle = FontStyle.Bold };
            float a2 = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(t * 2f));
            tap.normal.textColor = new Color(1f, 0.95f, 0.85f, a2);
            string start = Application.isMobilePlatform ? Loc.T("TAP TO START", "タップで始める") : Loc.T("TAP / ENTER TO START", "タップか Enter で始める");
            FitLabel(new Rect(0, H * 0.86f, W, H * 0.04f), start, tap);

            float bw = W * 0.5f;
            Button(new Rect((W - bw) * 0.5f, H * 0.92f, bw, H * 0.04f), $"Language: {Loc.LanguageName}", new Color(1f, 1f, 1f, 0.1f), centered, () => Loc.Next());
        }

        /// <summary>(cx, cy) を中心に半径 r のまるい光を重ねる。</summary>
        static void Glow(float cx, float cy, float r, Color color)
        {
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(cx - r, cy - r, r * 2f, r * 2f), TitleArt.SoftGlow);
            GUI.color = old;
        }

        /// <summary>背景画像の上で、灯術師を立たせる位置（画像の幅・高さに対する割合）。左端・足元の高さ・幅。</summary>
        const float TitleHeroX = 0.15f, TitleHeroFeetY = 0.5f, TitleHeroWidth = 0.33f;

        static Texture2D titleBg;
        static bool titleBgLoaded;

        /// <summary>タイトルの灯術師（後ろ姿）。杖の水晶が虹色に光る。</summary>
        void DrawTitleHero(Rect lamp, float t)
        {
            DrawSprite(lamp, PixelArt.TitleLampwright);
            var hue = Color.HSVToRGB(Mathf.Repeat(t * 0.12f, 1f), 0.55f, 1f);
            float gx = lamp.x + lamp.width * 33.5f / 40f, gy = lamp.y + lamp.height * 4.5f / 56f;
            Glow(gx, gy, lamp.width * 0.5f, new Color(hue.r, hue.g, hue.b, 0.5f));
            Glow(gx, gy, lamp.width * 0.15f, new Color(1f, 1f, 1f, 0.6f));
        }

        /// <summary>背景画像がないときのタイトルの場面（霧の洞窟・左の崖の灯術師・右の逆さの塔）。</summary>
        void DrawTitleScene(float W, float H, float t)
        {
            // 霧の洞窟（上と下は暗く、中ほどが霧で白む）
            const int bands = 40;
            for (int b = 0; b < bands; b++)
            {
                float k = b / (float)(bands - 1);
                float mist = Mathf.Exp(-Mathf.Pow((k - 0.45f) / 0.28f, 2f));
                var c = Color.Lerp(new Color(0.04f, 0.04f, 0.06f), new Color(0.3f, 0.33f, 0.38f), mist);
                Fill(new Rect(0, H * b / bands, W, H / bands + 1f), c);
            }
            float drift = Mathf.Sin(t * 0.15f) * W * 0.04f;
            Glow(W * 0.66f + drift, H * 0.42f, W * 0.7f, new Color(0.62f, 0.66f, 0.72f, 0.35f));

            // 天井から垂れる鍾乳石
            for (int i = 0; i < 9; i++)
            {
                float sx = Hash01(i * 41 + 3) * W, len = H * (0.03f + 0.05f * Hash01(i * 43 + 1)), sw = W * (0.03f + 0.03f * Hash01(i * 47));
                var sc2 = new Color(0.05f, 0.05f, 0.07f, 0.95f);
                for (int j = 0; j < 6; j++)
                {
                    float f = j / 6f;
                    Fill(new Rect(sx + sw * f * 0.5f, len * f, sw * (1f - f), len / 6f + 1f), sc2);
                }
            }
            // 右端の岩壁
            for (int b = 0; b < bands; b++)
            {
                float k = b / (float)(bands - 1);
                float rw = W * (0.06f + 0.08f * Mathf.PerlinNoise(k * 6f, 2.2f) + 0.06f * k);
                Fill(new Rect(W - rw, H * b / bands, rw, H / bands + 1f), new Color(0.06f, 0.06f, 0.08f, 0.9f));
            }

            // 逆さの塔（右の霧の中に沈む）。まわりを螺旋階段がめぐる
            float tw2 = W * 0.46f, th = tw2 * 112f / 48f;
            var tower = new Rect(W * 0.66f - tw2 * 0.5f, H * 0.2f, tw2, th);
            float stairTop = tower.y + th * 0.25f, stairLen = th * 0.45f;
            TitleStairs(tower, stairTop, stairLen, false);
            var oldTint = GUI.color;
            GUI.color = new Color(0.82f, 0.86f, 0.92f); // 霧で少しかすむ
            DrawSprite(tower, PixelArt.TitleTower);
            GUI.color = oldTint;
            TitleStairs(tower, stairTop, stairLen, true);
            float flick = 0.55f + 0.45f * Mathf.PerlinNoise(t * 2.2f, 0.3f);
            float beaconY = tower.yMax - th * 0.022f;
            Glow(tower.center.x, beaconY, tw2 * 0.5f, new Color(1f, 0.8f, 0.45f, 0.45f * flick));
            Glow(tower.center.x, beaconY, tw2 * 0.16f, new Color(1f, 0.95f, 0.75f, 0.8f * flick));
            // 塔の壁の松明
            {
                float f = 0.7f + 0.3f * Mathf.PerlinNoise(t * 6f, 1.7f);
                float cx = tower.x + tw2 * 0.12f, cy = tower.y + th * 0.12f;
                Fill(new Rect(cx - W * 0.003f, cy, W * 0.006f, H * 0.014f), new Color(0.25f, 0.18f, 0.12f));
                Glow(cx, cy - H * 0.004f, W * 0.16f, new Color(1f, 0.55f, 0.2f, 0.5f * f));
                Fill(new Rect(cx - W * 0.007f, cy - H * 0.012f * f, W * 0.014f, H * 0.012f * f), new Color(1f, 0.55f, 0.15f));
                Fill(new Rect(cx - W * 0.003f, cy - H * 0.007f, W * 0.006f, H * 0.006f), new Color(1f, 0.92f, 0.55f));
            }

            // 折れた橋と、その下へ下りる階段
            float bridgeY = H * 0.64f;
            Fill(new Rect(W * 0.45f, bridgeY, W * 0.2f, H * 0.014f), new Color(0.27f, 0.26f, 0.3f));
            Fill(new Rect(W * 0.65f, bridgeY + H * 0.004f, W * 0.025f, H * 0.008f), new Color(0.27f, 0.26f, 0.3f)); // 折れた先
            Fill(new Rect(W * 0.45f, bridgeY + H * 0.014f, W * 0.22f, H * 0.005f), new Color(0.09f, 0.09f, 0.11f));
            Fill(new Rect(W * 0.57f, bridgeY + H * 0.019f, W * 0.035f, H * 0.17f), new Color(0.2f, 0.2f, 0.23f));
            Fill(new Rect(W * 0.595f, bridgeY + H * 0.019f, W * 0.01f, H * 0.17f), new Color(0.12f, 0.12f, 0.14f));
            for (int i = 0; i < 8; i++)
                Fill(new Rect(W * (0.6f + i * 0.012f), H * (0.7f + i * 0.015f), W * 0.08f, H * 0.011f), new Color(0.18f, 0.18f, 0.21f));

            // 左の崖（上が平ら、右の縁は霧の光を受けて明るい）
            float cliffTop = H * 0.5f;
            for (int b = 0; b < 20; b++) // 奥にそびえる岩
            {
                float k = b / 19f, y = H * (0.2f + 0.3f * k);
                float rw = W * (0.06f + 0.05f * k + 0.04f * Mathf.PerlinNoise(k * 6f, 9.3f));
                Fill(new Rect(0, y, rw, H * 0.3f / 20f + 1f), new Color(0.2f, 0.16f, 0.13f));
                Fill(new Rect(rw, y, W * 0.015f, H * 0.3f / 20f + 1f), new Color(0.3f, 0.24f, 0.19f));
            }
            const int crows = 36;
            float cdepth = H - cliffTop;
            for (int b = 0; b < crows; b++)
            {
                float k = b / (float)(crows - 1);
                float y = cliffTop + cdepth * b / crows, bh = cdepth / crows + 1f;
                float u = Mathf.Clamp01((k - 0.45f) / 0.35f);
                u = u * u * (3f - 2f * u); // 下のほうで崖が奥へ引っ込む
                float R = W * (0.56f - 0.1f * k - 0.12f * u + 0.04f * Mathf.PerlinNoise(k * 7f, 4.1f));
                Fill(new Rect(0, y, R, bh), Color.Lerp(new Color(0.36f, 0.27f, 0.2f), new Color(0.07f, 0.06f, 0.06f), k));
                Fill(new Rect(R - W * 0.02f, y, W * 0.02f, bh), Color.Lerp(new Color(0.5f, 0.4f, 0.31f), new Color(0.12f, 0.1f, 0.09f), k));
            }
            // 岩の割れ目
            for (int i = 0; i < 40; i++)
            {
                float k = Hash01(i * 53 + 7);
                float maxX = W * (0.5f - 0.2f * k);
                Fill(new Rect(Hash01(i * 59) * maxX, cliffTop + cdepth * k, W * (0.04f + 0.08f * Hash01(i * 61)), H * 0.004f), new Color(0.08f, 0.06f, 0.05f, 0.6f * (1f - k)));
            }
            // 崖の上の土と苔
            Fill(new Rect(0, cliffTop - H * 0.008f, W * 0.56f, H * 0.012f), new Color(0.42f, 0.32f, 0.22f));
            for (int i = 0; i < 12; i++)
            {
                float mx = Hash01(i * 67 + 1) * W * 0.52f, mw = W * (0.04f + 0.06f * Hash01(i * 71));
                Fill(new Rect(mx, cliffTop - H * 0.011f, mw, H * 0.006f), new Color(0.3f, 0.44f, 0.22f));
                if (i % 3 == 0) Fill(new Rect(mx, cliffTop, W * 0.008f, H * (0.01f + 0.03f * Hash01(i * 73))), new Color(0.24f, 0.36f, 0.18f, 0.8f));
            }

            // 結晶（ほのかに光る）
            float[,] crystals = { { 0.06f, 0f, 1f }, { 0.47f, 0f, 0.8f }, { 0.12f, 0.3f, 0.9f }, { 0.22f, 0.45f, 0.7f } };
            for (int i = 0; i < crystals.GetLength(0); i++)
            {
                float cs = W * 0.08f * crystals[i, 2];
                float cx = W * crystals[i, 0], cy = cliffTop + cdepth * crystals[i, 1];
                Glow(cx + cs * 0.5f, cy - cs * 0.4f, cs * 1.4f, new Color(0.6f, 0.5f, 1f, 0.25f + 0.1f * Mathf.Sin(t * 1.5f + i)));
                DrawSprite(new Rect(cx, cy - cs, cs, cs), PixelArt.TitleCrystal);
            }

            float lw = W * 0.33f, lh = lw * 56f / 40f;
            DrawTitleHero(new Rect(W * 0.15f, cliffTop - lh * 52f / 56f, lw, lh), t);
        }

        /// <summary>タイトルの塔をめぐる螺旋階段。front=false で塔の後ろ側、true で手前側の段だけを描く。</summary>
        void TitleStairs(Rect tower, float top, float length, bool front)
        {
            const int steps = 90;
            for (int i = 0; i < steps; i++)
            {
                float a = 1.1f * Mathf.PI + i / (float)steps * 2.6f * Mathf.PI;
                bool isFront = Mathf.Sin(a) > 0f;
                if (isFront != front) continue;
                float x = tower.center.x + Mathf.Cos(a) * tower.width * 0.62f;
                float y = top + i / (float)steps * length + Mathf.Sin(a) * tower.width * 0.08f;
                var c = isFront ? new Color(0.2f, 0.2f, 0.24f) : new Color(0.13f, 0.13f, 0.16f);
                Fill(new Rect(x - tower.width * 0.05f, y, tower.width * 0.1f, tower.width * 0.045f), c);
                Fill(new Rect(x - tower.width * 0.05f, y, tower.width * 0.1f, tower.width * 0.01f), isFront ? new Color(0.3f, 0.3f, 0.35f) : new Color(0.18f, 0.18f, 0.21f)); // 踏み面
            }
        }

        /// <summary>0〜1 の決まった乱数（タイトルの星・火の粉の位置に使う。毎回同じ並び）。</summary>
        static float Hash01(int n)
        {
            unchecked
            {
                uint h = (uint)n * 2654435761u;
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;
                return (h & 0xffff) / 65535f;
            }
        }

        /// <summary>職業を選ぶ画面。3つの大きなボタン（PC は数字キー 1〜3 でも選べる）。</summary>
        void DrawClassSelect()
        {
            float W = Screen.width;
            float H = Screen.height;
            Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.75f));

            var title = new GUIStyle(centered) { fontSize = label.fontSize * 2, fontStyle = FontStyle.Bold };
            Hud.FitLabel(new Rect(0, H * 0.12f, W, H * 0.06f), "VIVID OF FADE", title);
            float pad = W * 0.08f;
            float gap = H * 0.02f;
            float y = H * 0.27f;
            if (newSlot >= 0) y = DrawNewCharacter(pad, gap);
            else y = DrawSlots(pad, gap);

            // 案内役（左下で話しかけてくる）
            float gs = W * 0.22f;
            var gr = new Rect(W * 0.04f, H - gs - H * 0.02f, gs, gs);
            DrawSprite(gr, PixelArt.Guide);
            var bubble = new Rect(gr.xMax + W * 0.02f, gr.y + gs * 0.1f, W - gr.xMax - W * 0.06f, gs * 0.75f);
            Fill(bubble, new Color(1f, 1f, 1f, 0.9f));
            var say = new GUIStyle(small) { wordWrap = true, alignment = TextAnchor.MiddleLeft };
            say.normal.textColor = new Color(0.1f, 0.08f, 0.12f);
            GUI.Label(new Rect(bubble.x + W * 0.02f, bubble.y, bubble.width - W * 0.04f, bubble.height), GuideGreeting(), say);

            // BGM と言語は1行に並べる（下の案内役と重ならないように。N キーでも BGM を変えられる）
            float half = (W - pad * 2f - W * 0.02f) * 0.5f;
            var musicRect = new Rect(pad, y + gap * 0.5f, half, H * 0.045f);
            Button(musicRect, $"BGM: {World.Music.ThemeName}", new Color(1f, 1f, 1f, 0.1f), centered, () => World.NextMusic());
            var langRect = new Rect(musicRect.xMax + W * 0.02f, musicRect.y, half, musicRect.height);
            Button(langRect, $"Language: {Loc.LanguageName}", new Color(1f, 1f, 1f, 0.1f), centered, () => Loc.Next());
        }

        // ---- キャラクター選択（3つの枠） ----

        int newSlot = -1;          // 新しく作る枠（-1 は枠の一覧）
        string armedDelete;        // 消す確認（2回押し）
        float armedDeleteUntil;

        /// <summary>数字キー：枠の一覧なら枠を、職業の選択なら職業を選ぶ。</summary>
        public void PressSlotKey(int n)
        {
            if (newSlot >= 0)
            {
                if (n < ClassInfo.Count)
                {
                    World.CreateCharacter(newSlot, (ClassType)n);
                    newSlot = -1;
                }
                return;
            }
            if (n >= Profile.SlotCount) return;
            if (Profile.InSlot(n) != null) World.ChooseCharacter(n);
            else newSlot = n;
        }

        /// <summary>職業の選択を閉じる（Esc）。閉じたら true。</summary>
        public bool CancelNewCharacter()
        {
            if (newSlot < 0) return false;
            newSlot = -1;
            return true;
        }

        float DrawSlots(float pad, float gap)
        {
            float W = Screen.width, H = Screen.height;
            GUI.Label(new Rect(0, H * 0.19f, W, H * 0.04f), Loc.T("Choose a character", "キャラクターを選ぶ"), centered);
            float y = H * 0.25f;
            float bh = H * 0.14f;
            var nameStyle = new GUIStyle(label) { fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(label.fontSize * 1.25f) };
            for (int i = 0; i < Profile.SlotCount; i++)
            {
                int slot = i;
                var ch = Profile.InSlot(slot);
                var r = new Rect(pad, y, W - pad * 2f, bh);
                float inner = r.height * 0.12f;
                string key = Application.isMobilePlatform ? "" : $"[{i + 1}] ";
                if (ch == null)
                {
                    Button(r, null, new Color(1f, 1f, 1f, 0.06f), centered, () => newSlot = slot);
                    var empty = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
                    empty.normal.textColor = new Color(0.75f, 0.75f, 0.8f);
                    GUI.Label(r, key + Loc.T("+  New character", "＋  新しいキャラクター"), empty);
                }
                else
                {
                    Button(r, null, new Color(1f, 1f, 1f, 0.12f), centered, () => World.ChooseCharacter(slot));
                    float icon = r.height - inner * 2f;
                    DrawSprite(new Rect(r.x + inner, r.y + inner, icon, icon), PixelArt.PlayerFor(ch));
                    float tx = r.x + icon + inner * 2f;
                    float tw = r.xMax - tx - inner;
                    float lh = small.fontSize * 1.35f;
                    FitLabel(new Rect(tx, r.y + inner, tw * 0.7f, nameStyle.fontSize * 1.4f), key + AdvancedClasses.NameFor(ch), nameStyle);
                    var spec = ClassInfo.Get((ClassType)ch.ClassId);
                    GUI.Label(new Rect(tx, r.y + inner + nameStyle.fontSize * 1.5f, tw, lh),
                        Loc.T($"Lv {ch.Level}   Best {ch.BestFloor}F   Embers {ch.Embers}", $"Lv {ch.Level}   最深 {ch.BestFloor}層   火種 {ch.Embers}"), small);
                    var sub = new GUIStyle(small);
                    sub.normal.textColor = new Color(0.75f, 0.75f, 0.8f);
                    GUI.Label(new Rect(tx, r.y + inner + nameStyle.fontSize * 1.5f + lh, tw, lh),
                        spec.DisplayName + (ch.Run.Active ? Loc.T($"   (suspended: floor {ch.Run.Floor})", $"   （中断中：{ch.Run.Floor}層）") : ""), sub);

                    // 消す（2回押し）。消しても倉庫は残る
                    string id = "del" + slot;
                    bool armed = armedDelete == id && Time.unscaledTime < armedDeleteUntil;
                    var del = new Rect(r.xMax - r.width * 0.22f - inner * 0.5f, r.y + inner * 0.5f, r.width * 0.22f, r.height * 0.28f);
                    Button(del, armed ? Loc.T("Delete?", "消す？") : Loc.T("Delete", "消す"), armed ? new Color(0.7f, 0.15f, 0.15f, 0.85f) : new Color(1f, 1f, 1f, 0.08f), centered, () =>
                    {
                        if (armedDelete == id && Time.unscaledTime < armedDeleteUntil)
                        {
                            armedDelete = null;
                            Profile.DeleteSlot(slot);
                        }
                        else
                        {
                            armedDelete = id;
                            armedDeleteUntil = Time.unscaledTime + 3f;
                        }
                    });
                }
                y += bh + gap;
            }
            return y;
        }

        /// <summary>新しいキャラクターの職業を選ぶ。職業の数に合わせて並べる（増えても入るよう、高さを詰める）。</summary>
        float DrawNewCharacter(float pad, float gap)
        {
            float W = Screen.width, H = Screen.height;
            GUI.Label(new Rect(0, H * 0.19f, W, H * 0.04f), Loc.T($"New character (slot {newSlot + 1}): choose a class", $"新しいキャラクター（枠 {newSlot + 1}）：職業を選ぶ"), centered);
            float y = H * 0.25f;
            int n = ClassInfo.Count;
            float area = H * 0.5f;
            float bh = Mathf.Min(H * 0.14f, (area - gap * (n - 1)) / n);
            var nameStyle = new GUIStyle(label) { fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(label.fontSize * 1.2f) };
            for (int i = 0; i < n; i++)
            {
                var spec = ClassInfo.Get((ClassType)i);
                var type = spec.Type;
                var r = new Rect(pad, y, W - pad * 2f, bh);
                Button(r, null, new Color(1f, 1f, 1f, 0.12f), centered, () =>
                {
                    World.CreateCharacter(newSlot, type);
                    newSlot = -1;
                });
                float inner = r.height * 0.1f;
                float icon = r.height - inner * 2f;
                DrawSprite(new Rect(r.x + inner, r.y + inner, icon, icon), PixelArt.PlayerFor(new CharacterData { ClassId = (int)type }) /* 元の職業の見た目 */);
                float tx = r.x + icon + inner * 2f;
                float tw = r.xMax - tx - inner;
                float lh = small.fontSize * 1.3f;
                string key = Application.isMobilePlatform ? "" : $"[{i + 1}] ";
                FitLabel(new Rect(tx, r.y + inner, tw, nameStyle.fontSize * 1.3f), key + spec.DisplayName, nameStyle);
                FitLabel(new Rect(tx, r.y + inner + nameStyle.fontSize * 1.4f, tw, lh), spec.DisplayLine1, small);
                FitLabel(new Rect(tx, r.y + inner + nameStyle.fontSize * 1.4f + lh, tw, lh), spec.DisplayLine2 + $"   {Attributes.ShortName(spec.MainAttr)}/{Attributes.ShortName(spec.SubAttr)}", small);
                y += bh + gap;
            }
            Button(new Rect(pad, y, W - pad * 2f, H * 0.045f), Loc.T("BACK", "戻る"), new Color(1f, 1f, 1f, 0.08f), centered, () => newSlot = -1);
            return y + H * 0.045f + gap;
        }

        void EnsureStyles()
        {
            int size = Mathf.Max(12, Mathf.RoundToInt(Screen.height * 0.02f));
            if (label != null && label.fontSize == size) return;

            label = new GUIStyle(GUI.skin.label) { fontSize = size };
            label.normal.textColor = Color.white;
            small = new GUIStyle(label) { fontSize = Mathf.Max(10, Mathf.RoundToInt(size * 0.8f)) };
            centered = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
            number = new GUIStyle(centered) { fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(size * 0.9f) };
            big = new GUIStyle(number) { fontSize = Mathf.RoundToInt(size * 1.4f) };
        }

        // ---- 描画の補助（BagScreen からも使う） ----

        /// <summary>ボタンを描いて、次のタップの判定に登録する。rect は GUI 座標（左上原点）。</summary>
        public void Button(Rect guiRect, string text, Color background, GUIStyle style, Action onTap)
        {
            Fill(guiRect, background);
            if (text != null) FitLabel(guiRect, text, style); // 折り返さず、入らなければ文字を小さく
            if (Event.current.type == EventType.Repaint)
            {
                pendingRects.Add(new Rect(guiRect.x, Screen.height - guiRect.y - guiRect.height, guiRect.width, guiRect.height));
                pendingActions.Add(onTap);
            }
        }

        public static Rect ToGui(Rect r) => new Rect(r.x, Screen.height - r.y - r.height, r.width, r.height);

        /// <summary>1行に収まるよう、必要なら文字を小さくして描く（長い見出しが折り返して切れないように）。</summary>
        public static void FitLabel(Rect guiRect, string text, GUIStyle style)
        {
            var s = new GUIStyle(style) { wordWrap = false, clipping = TextClipping.Overflow };
            var content = new GUIContent(text);
            float max = guiRect.width * 0.94f;
            while (s.fontSize > 8 && s.CalcSize(content).x > max) s.fontSize--;
            GUI.Label(guiRect, content, s);
        }

        public static void Fill(Rect guiRect, Color color)
        {
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(guiRect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        /// <summary>強化した品なら、アイコンの右下に強化の紋章を重ねる。</summary>
        public static void DrawEnhance(Rect cell, Item item)
        {
            if (item == null || item.Enhance <= 0) return;
            // 枠の内側の右下に収める（はみ出さない）
            float s = Mathf.Min(cell.width, cell.height) * 0.42f;
            float m = cell.width * 0.03f;
            DrawSprite(new Rect(cell.xMax - s - m, cell.yMax - s - m, s, s), PixelArt.EnhanceEmblem(item.Enhance));
        }

        public static void DrawSprite(Rect guiRect, Sprite sprite)
        {
            if (sprite == null) return;
            GUI.DrawTexture(guiRect, sprite.texture, ScaleMode.ScaleToFit);
        }
    }
}
