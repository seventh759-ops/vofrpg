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
            public float Life = NumberLife;
            public bool Wide;  // スキル名など、長い文字
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
        readonly List<ToastLine> messages = new List<ToastLine>(); // メッセージウィンドウの履歴（消えない。新しい20件まで）
        readonly List<Rect> buttonRects = new List<Rect>();     // 画面座標（左下原点）
        readonly List<Action> buttonActions = new List<Action>();
        readonly List<Rect> pendingRects = new List<Rect>();
        readonly List<Action> pendingActions = new List<Action>();

        BagScreen bag;
        StatsScreen stats;
        SkillsScreen skills;
        ReturnScreen returnScreen;
        MenuScreen menu;

        /// <summary>メニューを開いたとき（設定の画面ではなく最初から出す）。</summary>
        public void ResetMenu() => menu?.Reset();

        /// <summary>町から設定だけを開いたとき。</summary>
        public void OpenSettingsOnly() => menu?.OpenSettingsOnly();
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
                float w = Screen.width * 0.17f;
                float h = TitleRowH;
                float m = BandMargin;
                return new Rect(Screen.width - w - m, Screen.height - h - m, w, h);
            }
        }

        // ---- 上の帯と下の帯（スキルと薬）。真ん中がダンジョン ----
        // 上の帯：1段目 ゲームのロゴ・転生の印・MENU／2段目 HP・MP／3段目 レベルと経験値／3.5段目 層・回避
        static float BandMargin => Screen.height * 0.008f;
        static float TitleRowH => Screen.height * 0.024f;
        static float TopRowH => Screen.height * 0.021f; // HP・MP のバー（細め）
        static float ExpRowH => Screen.height * 0.019f;
        static float FloorRowH => Screen.height * 0.021f;

        /// <summary>上の帯の下端（GUI 座標、上が 0）。</summary>
        public static float TopBandBottom => BandMargin * 4.4f + TitleRowH + TopRowH + ExpRowH + FloorRowH; // 段の間は 0.8 ずつ（枠が重ならないように）

        /// <summary>メッセージウィンドウの行数と、1行の高さ。</summary>
        const int LogLines = 4;
        float LogLineH => (small != null ? small.fontSize : Screen.height * 0.016f) * 1.3f; // 起動直後（文字の設定を作る前）は画面の高さから見積もる

        /// <summary>ショートカット（スキルと薬）の区画（GUI 座標）。下の帯の下側。</summary>
        Rect ShortcutPanel
        {
            get
            {
                float top = Screen.height - (SkillAutoRect(0).yMax + BandMargin * 1.2f);
                return new Rect(BandMargin * 1.5f, top, Screen.width - BandMargin * 3f, Screen.height - BandMargin * 1.2f - top);
            }
        }

        /// <summary>メッセージウィンドウの区画（GUI 座標）。ショートカットの区画のすぐ上。</summary>
        Rect MessageWindow
        {
            get
            {
                float h = LogLines * LogLineH + BandMargin * 1.6f;
                var sc = ShortcutPanel;
                return new Rect(sc.x, sc.y - BandMargin - h, sc.width, h);
            }
        }

        /// <summary>下の帯の上端（GUI 座標）。メッセージウィンドウとショートカットの2つの区画を含む。</summary>
        float BottomBandTop => MessageWindow.y - BandMargin;

        /// <summary>枠線を描く。</summary>
        public static void Frame(Rect r, Color c, float t = 1f)
        {
            Fill(new Rect(r.x, r.y, r.width, t), c);
            Fill(new Rect(r.x, r.yMax - t, r.width, t), c);
            Fill(new Rect(r.x, r.y, t, r.height), c);
            Fill(new Rect(r.xMax - t, r.y, t, r.height), c);
        }

        static readonly Color StudGold = new Color(0.95f, 0.78f, 0.38f, 1f);
        /// <summary>HUD の飾りの枠の色（どこも同じ金色にそろえる）。</summary>
        public static readonly Color FrameGold = new Color(0.85f, 0.72f, 0.42f, 1f);
        static float FrameT => Mathf.Max(2f, Mathf.Round(Screen.height * 0.0012f));

        /// <summary>
        /// 飾りの付いた枠：外側に暗い線、色の線、内側にうっすら明るい線。四隅に金の鋲（暗い縁つき）。
        /// </summary>
        public static void OrnateFrame(Rect r, Color main, float t, bool inset = false, bool studs = true)
        {
            Frame(new Rect(r.x - t, r.y - t, r.width + t * 2f, r.height + t * 2f), new Color(0f, 0f, 0f, 0.85f), t);
            // 枠そのものに凹凸：上と左は明るく、下と右は暗い（光は左上から）
            var lightEdge = Color.Lerp(main, Color.white, 0.45f);
            var darkEdge = Color.Lerp(main, Color.black, 0.45f);
            Fill(new Rect(r.x, r.y, r.width, t), lightEdge);
            Fill(new Rect(r.x, r.y, t, r.height), lightEdge);
            Fill(new Rect(r.x, r.yMax - t, r.width, t), darkEdge);
            Fill(new Rect(r.xMax - t, r.y, t, r.height), darkEdge);
            // 中：inset ならへこみ（上と左に影、下と右に照り返し）、そうでなければ出っぱり（逆）
            var inner = new Rect(r.x + t, r.y + t, r.width - t * 2f, r.height - t * 2f);
            float b = Mathf.Max(1f, t * 0.75f);
            var shade = new Color(0f, 0f, 0f, 0.45f);
            var shine = new Color(1f, 1f, 1f, 0.16f);
            var tl = inset ? shade : shine;
            var br = inset ? shine : shade;
            Fill(new Rect(inner.x, inner.y, inner.width, b), tl);
            Fill(new Rect(inner.x, inner.y, b, inner.height), tl);
            Fill(new Rect(inner.x, inner.yMax - b, inner.width, b), br);
            Fill(new Rect(inner.xMax - b, inner.y, b, inner.height), br);
            if (!studs) return;
            float s = t * 4f;
            foreach (var c in new[] { new Vector2(r.x, r.y), new Vector2(r.xMax, r.y), new Vector2(r.x, r.yMax), new Vector2(r.xMax, r.yMax) })
            {
                var stud = new Rect(c.x - s * 0.5f, c.y - s * 0.5f, s, s);
                Fill(stud, new Color(0.15f, 0.1f, 0.04f, 1f));
                Fill(new Rect(stud.x + t * 0.5f, stud.y + t * 0.5f, s - t, s - t), StudGold);
                Fill(new Rect(stud.x + t * 0.5f, stud.y + t * 0.5f, (s - t) * 0.5f, (s - t) * 0.5f), new Color(1f, 0.95f, 0.75f, 1f)); // 左上の光
            }
        }

        /// <summary>
        /// 飾りの付いたバー（HP・MP）：暗い下地、中身に光沢（上が明るく下が暗い）、25% ごとの目盛り、飾りの枠。
        /// </summary>
        static void OrnateBar(Rect r, float ratio, Color back, Color fill, Color frame)
        {
            float t = Mathf.Max(2f, Mathf.Round(Screen.height * 0.0012f));
            Fill(r, back);
            var f = new Rect(r.x, r.y, r.width * ratio, r.height);
            Fill(f, fill);
            Fill(new Rect(f.x, f.y, f.width, f.height * 0.32f), new Color(1f, 1f, 1f, 0.22f));      // 光沢
            Fill(new Rect(f.x, f.yMax - f.height * 0.22f, f.width, f.height * 0.22f), new Color(0f, 0f, 0f, 0.25f)); // 下の陰
            if (ratio > 0f && ratio < 1f) Fill(new Rect(f.xMax - t * 0.5f, f.y, t * 0.5f, f.height), new Color(1f, 1f, 1f, 0.5f)); // 中身の端の光
            for (int k = 1; k < 4; k++) Fill(new Rect(r.x + r.width * k / 4f, r.y + r.height * 0.55f, 1f, r.height * 0.45f), new Color(0f, 0f, 0f, 0.45f)); // 目盛り
            OrnateFrame(r, frame, t, true, false); // バーはへこみ（段が詰まっているので鋲はなし）
        }

        /// <summary>カメラのずれ（CameraRig.CenterShift）。上下の帯の高さの差の半分。</summary>
        public float ViewCenterShift => ((Screen.height - BottomBandTop) - TopBandBottom) / (2f * Screen.height);

        /// <summary>
        /// 下の段のボタン（左からスキル3つ、少しあけて薬2つ）。0〜2 がスキル、3・4 が回復薬・魔力の薬。画面座標（左下原点）。
        /// </summary>
        Rect SkillRect(int slot)
        {
            // ショートカットの区画の幅いっぱいに、スキル3つと薬2つを中央そろえで並べる
            const float gapK = 0.12f, extraK = 0.35f;     // ボタンどうしの間・スキルと薬の間（ボタンの大きさに対する割合）
            int n = SkillInfo.Slots + 2;
            float avail = Screen.width - BandMargin * 7f;
            float s = Mathf.Min(avail / (n + (n - 1) * gapK + extraK), Screen.height * 0.09f);
            float total = s * (n + (n - 1) * gapK + extraK);
            float x0 = (Screen.width - total) * 0.5f;
            float extra = slot >= SkillInfo.Slots ? s * extraK : 0f;
            return new Rect(x0 + slot * s * (1f + gapK) + extra, BandMargin * 2.5f, s, s);
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
                    OrnateFrame(r, FrameGold, FrameT, false, false);
                    continue;
                }

                var spec = SkillInfo.Get(ClassInfo.Current, index);
                int mpCost = SkillInfo.MpCost(spec, index, ch.Level);
                bool enoughMp = player.Mp >= mpCost;
                Button(r, null, new Color(0f, 0f, 0f, 0.55f), centered, () => World.Skills.Press(slot, World.Player));
                OrnateFrame(r, FrameGold, FrameT, false, false);
                float inset = r.width * 0.12f;
                var iconRect = new Rect(r.x + inset, r.y + inset, r.width - inset * 2f, r.height - inset * 2f);
                var old = GUI.color;
                GUI.color = enoughMp ? Color.white : new Color(1f, 0.45f, 0.45f, 0.6f);
                DrawSprite(iconRect, PixelArt.SkillIcon(spec));
                GUI.color = old;

                // 待ち時間（上から暗くする）
                float cd = World.Skills.CooldownRatio(i);
                if (cd > 0f) Fill(new Rect(r.x, r.y, r.width, r.height * cd), new Color(0f, 0f, 0f, 0.6f));

                // 消費 MP（右下に小さく）
                cost.normal.textColor = enoughMp ? new Color(0.65f, 0.8f, 1f) : new Color(1f, 0.5f, 0.5f);
                GUI.Label(new Rect(r.x, r.y, r.width - r.width * 0.06f, r.height - r.height * 0.02f), mpCost.ToString(), cost);

                // キー（PC は左上に 1・2・3。薬の Q・E と同じ）
                if (!Application.isMobilePlatform)
                {
                    var keyStyle = new GUIStyle(cost) { alignment = TextAnchor.UpperLeft };
                    keyStyle.normal.textColor = new Color(1f, 1f, 1f, 0.8f);
                    GUI.Label(new Rect(r.x + r.width * 0.06f, r.y + r.height * 0.02f, r.width, r.height), (i + 1).ToString(), keyStyle);
                }

                // オート／手動の帯
                bool auto = ch.SlotAuto[i];
                var autoR = ToGui(SkillAutoRect(i));
                Button(autoR, null, auto ? new Color(0.3f, 0.85f, 0.4f, 0.85f) : new Color(1f, 1f, 1f, 0.15f), centered, () =>
                {
                    ch.SlotAuto[slot] = !ch.SlotAuto[slot];
                    Profile.Save();
                });
                if (auto) Fill(new Rect(autoR.x, autoR.y, autoR.width, autoR.height * 0.35f), new Color(1f, 1f, 1f, 0.25f)); // 光沢
                OrnateFrame(autoR, FrameGold, Mathf.Max(1f, FrameT * 0.5f), !auto, false); // 入っているときは出っぱり、切れているときはへこみ
            }

            // かかっている強化：ダンジョンの左上（上の帯のすぐ下）に、スキルのアイコンと残り秒数。残り2秒を切ると点滅して、切れたら消える
            float bs = Screen.height * 0.045f, bgap = bs * 0.15f;
            var secStyle = new GUIStyle(small) { alignment = TextAnchor.LowerRight, fontStyle = FontStyle.Bold, fontSize = Mathf.Max(9, Mathf.RoundToInt(small.fontSize * 0.85f)) };
            for (int b = 0; b < player.BuffCount; b++)
            {
                float left = player.BuffRemaining(b);
                var br = new Rect(BandMargin * 2f + b * (bs + bgap), TopBandBottom + BandMargin * 1.5f, bs, bs);
                float alpha = left < 2f ? 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 8f)) : 1f;
                var old = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                DrawSprite(br, PixelArt.SkillIcon(player.BuffSpec(b)));
                GUI.color = old;
                secStyle.normal.textColor = new Color(1f, 1f, 1f, alpha);
                GUI.Label(new Rect(br.x, br.y, br.width - 1f, br.height + secStyle.fontSize * 0.5f), $"{Mathf.CeilToInt(left)}", secStyle);
            }
            // 弱体効果：強化のアイコンの右に続けて（赤い縁のアイコン）。残り2秒を切ると点滅
            for (int d = 0; d < player.DebuffCount; d++)
            {
                float left = player.DebuffRemaining(d);
                var br = new Rect(BandMargin * 2f + (player.BuffCount + d) * (bs + bgap), TopBandBottom + BandMargin * 1.5f, bs, bs);
                float alpha = left < 2f ? 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 8f)) : 1f;
                var old = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                DrawSprite(br, PixelArt.DebuffIcon(player.DebuffAt(d)));
                GUI.color = old;
                secStyle.normal.textColor = new Color(1f, 0.75f, 0.75f, alpha);
                GUI.Label(new Rect(br.x, br.y, br.width - 1f, br.height + secStyle.fontSize * 0.5f), $"{Mathf.CeilToInt(left)}", secStyle);
            }
        }

        /// <summary>薬のボタン（スキルの右に並ぶ。0 回復薬・1 魔力の薬）。画面座標（左下原点）。</summary>
        Rect PotionRect(int i) => SkillRect(SkillInfo.Slots + i);

        /// <summary>薬のボタン。スキルと同じ形で、瓶のアイコン・右下に残りの数・PC は左上にキー。</summary>
        void DrawPotions(CharacterData ch)
        {
            if (ch == null) return;
            var count = new GUIStyle(small) { alignment = TextAnchor.LowerRight, fontStyle = FontStyle.Bold };
            var key = new GUIStyle(small) { alignment = TextAnchor.UpperLeft, fontStyle = FontStyle.Bold };
            key.normal.textColor = new Color(1f, 1f, 1f, 0.8f);
            for (int i = 0; i < 2; i++)
            {
                bool hp = i == 0;
                int n = hp ? ch.HpPotions : ch.MpPotions;
                var r = ToGui(PotionRect(i));
                Button(r, null, new Color(0f, 0f, 0f, 0.55f), centered, () => World.UsePotion(hp));
                OrnateFrame(r, FrameGold, FrameT, false, false);
                float inset = r.width * 0.12f;
                var old = GUI.color;
                GUI.color = n > 0 ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                DrawSprite(new Rect(r.x + inset, r.y + inset, r.width - inset * 2f, r.height - inset * 2f), PixelArt.PotionIcon(hp));
                GUI.color = old;
                float cd = World.PotionCooldownRatio;
                if (cd > 0f) Fill(new Rect(r.x, r.y, r.width, r.height * cd), new Color(0f, 0f, 0f, 0.6f));
                count.normal.textColor = n > 0 ? Color.white : new Color(1f, 0.5f, 0.5f);
                GUI.Label(new Rect(r.x, r.y, r.width - r.width * 0.06f, r.height - r.height * 0.02f), n.ToString(), count);
                if (!Application.isMobilePlatform) GUI.Label(new Rect(r.x + r.width * 0.06f, r.y + r.height * 0.02f, r.width, r.height), hp ? "Q" : "E", key);
            }
        }

        public Rect AttackRect
        {
            get
            {
                // 手動攻撃のボタン（右下。下の帯の上に置く）
                float s = Mathf.Min(Screen.width, Screen.height) * 0.24f;
                float m = Screen.height * 0.03f;
                float bandTop = Screen.height - BottomBandTop; // 下の帯の上端（画面座標）。メッセージウィンドウと重ならないように、その上に置く
                return new Rect(Screen.width - s - m, bandTop + m, s, s);
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

        /// <summary>スキルを使ったとき、頭の上にスキル名をスキルの色で出す（少し長く残る）。</summary>
        public void AddSkillName(Vector2 world, string name, Color color)
        {
            if (numbers.Count >= MaxNumbers) numbers.RemoveAt(0);
            numbers.Add(new FloatNumber
            {
                World = Iso.ToScreen(world) + new Vector2(0f, 1.1f),
                Text = name,
                Color = new Color(Mathf.Min(1f, color.r + 0.25f), Mathf.Min(1f, color.g + 0.25f), Mathf.Min(1f, color.b + 0.25f), 1f),
                Big = true,
                Life = 0.9f,
                Wide = true,
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
        // レベルアップの札（画面の上の方に大きく）。羽の演出より長く出す
        float plateTime = -1f;
        int plateLevel, platePoints;
        const float PlateLife = 2.6f;

        /// <summary>レベルアップを大きく知らせる（新しいレベルと、もらえたポイント）。</summary>
        public void ShowLevelUp(int level, int points)
        {
            levelUpTime = 0f;
            plateTime = 0f;
            plateLevel = level;
            platePoints = points;
        }

        /// <summary>レベルアップの札が出ている間の光り具合（0〜1）。HUD の LV の欄も光らせる。</summary>
        float LevelGlow => plateTime < 0f ? 0f : Mathf.Clamp01(1f - plateTime / PlateLife) * (0.6f + 0.4f * Mathf.Sin(plateTime * 10f));

        void DrawLevelPlate()
        {
            if (plateTime < 0f) return;
            float W = Screen.width, H = Screen.height;
            float t = plateTime;
            float alpha = Mathf.Clamp01(t / 0.12f) * Mathf.Clamp01((PlateLife - t) / 0.5f);
            float pop = 1f + 0.25f * Mathf.Clamp01(1f - t / 0.18f) * Mathf.Clamp01(t / 0.06f); // 出た瞬間にふくらんで戻る
            float pw = W * 0.74f * pop, ph = H * 0.085f * pop;
            var r = new Rect((W - pw) * 0.5f, TopBandBottom + H * 0.06f - (ph - H * 0.085f) * 0.5f, pw, ph);
            var old = GUI.color;
            // 後ろの金色の光（脈打つ）
            GUI.color = new Color(1f, 0.8f, 0.3f, 0.55f * alpha * (0.8f + 0.2f * Mathf.Sin(t * 8f)));
            GUI.DrawTexture(new Rect(r.x - pw * 0.2f, r.y - ph * 0.9f, pw * 1.4f, ph * 2.8f), TitleArt.SoftGlow);
            GUI.color = old;
            Fill(r, new Color(0.06f, 0.04f, 0.02f, 0.9f * alpha));
            Fill(new Rect(r.x, r.y, r.width, r.height * 0.45f), new Color(1f, 0.85f, 0.4f, 0.12f * alpha)); // 上の照り
            // 走る光（左から右へ1回）
            float sweep = Mathf.Clamp01((t - 0.1f) / 0.6f);
            if (sweep > 0f && sweep < 1f) Fill(new Rect(r.x + r.width * sweep - r.width * 0.06f, r.y, r.width * 0.06f, r.height), new Color(1f, 1f, 0.85f, 0.35f * alpha));
            var gold = FrameGold;
            gold.a = alpha;
            OrnateFrame(r, gold, Mathf.Max(2f, FrameT * 1.5f));
            var big = new GUIStyle(centered) { fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(r.height * 0.42f) };
            big.normal.textColor = new Color(1f, 0.9f, 0.4f, alpha);
            OutlinedLabel(new Rect(r.x, r.y + r.height * 0.06f, r.width, r.height * 0.56f), "LEVEL UP!", big, new Color(0.3f, 0.12f, 0f, 0.9f * alpha));
            var sub = new GUIStyle(centered) { fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(r.height * 0.22f) };
            sub.normal.textColor = new Color(1f, 0.97f, 0.85f, alpha);
            OutlinedLabel(new Rect(r.x, r.y + r.height * 0.6f, r.width, r.height * 0.32f),
                Loc.T($"Lv {plateLevel}    +{platePoints} points", $"Lv {plateLevel}    ポイント +{platePoints}"), sub, new Color(0f, 0f, 0f, 0.8f * alpha));
        }

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
            // 副題は画面の幅で折り返す（長くても切れないように）。帯の高さは中身に合わせる
            float textW = W * 0.88f;
            var st = new GUIStyle(centered) { wordWrap = true };
            st.normal.textColor = new Color(1f, 1f, 1f, 0.9f * a);
            float subH = string.IsNullOrEmpty(bannerSub) ? 0f : st.CalcHeight(new GUIContent(bannerSub), textW);
            float titleH = H * 0.06f;
            float pad = H * 0.012f;
            float top = H * 0.3f;
            var band = new Rect(0, top, W, pad * 2f + titleH + subH);
            Fill(band, new Color(0f, 0f, 0f, 0.6f * a));
            var c = bannerColor;
            c.a = a;
            var line = new Color(c.r, c.g, c.b, 0.8f * a); // 帯の上下に、題の色の細い線
            Fill(new Rect(W * 0.08f, band.y, W * 0.84f, Mathf.Max(1f, FrameT * 0.75f)), line);
            Fill(new Rect(W * 0.08f, band.yMax - Mathf.Max(1f, FrameT * 0.75f), W * 0.84f, Mathf.Max(1f, FrameT * 0.75f)), line);
            var t = new GUIStyle(centered) { fontSize = Mathf.RoundToInt(label.fontSize * 1.5f), fontStyle = FontStyle.Bold };
            t.normal.textColor = c;
            OutlinedLabel(new Rect((W - textW) * 0.5f, band.y + pad, textW, titleH), bannerTitle, t, new Color(0f, 0f, 0f, 0.8f * a));
            if (subH > 0f) GUI.Label(new Rect((W - textW) * 0.5f, band.y + pad + titleH, textW, subH), bannerSub, st);
        }

        public void Toast(string text, Color color)
        {
            if (toasts.Count >= 5) toasts.RemoveAt(0);
            toasts.Add(new ToastLine { Text = text, Color = color });
            if (messages.Count >= 20) messages.RemoveAt(0);
            messages.Add(new ToastLine { Text = text, Color = color });
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
            if (plateTime >= 0f && (plateTime += dt) >= PlateLife) plateTime = -1f;

            for (int i = numbers.Count - 1; i >= 0; i--)
            {
                numbers[i].T += dt;
                if (numbers[i].T >= numbers[i].Life) numbers.RemoveAt(i);
            }
            for (int i = toasts.Count - 1; i >= 0; i--)
            {
                toasts[i].T += dt;
                if (toasts[i].T >= ToastLife) toasts.RemoveAt(i);
            }
            foreach (var mline in messages) mline.T += dt; // 履歴は消さず、明るさを決めるために時間だけ進める
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
                DrawLevelPlate();
                DrawBoonGet();
                DrawEliteLabels();
                DrawHud();
            }

            if (World.OnTitle) DrawTitle();
            else if (World.ChoosingClass && World.PendingClass.HasValue) DrawFloorSelect(World.PendingClass.Value);
            else if (World.ChoosingClass && World.InTown && World.BagOpen) bag.Draw(World);
            else if (World.ChoosingClass && World.InTown && World.StatsOpen) stats.Draw(World);
            else if (World.ChoosingClass && World.InTown && World.SkillsOpen) skills.Draw(World);
            else if (World.ChoosingClass && World.InTown && World.MenuOpen) menu.Draw(World); // 町から開いた設定
            else if (World.ChoosingClass && World.InTown) Town.Draw(World);
            else if (World.ChoosingClass) DrawClassSelect();
            else if (World.Player.IsDead) DrawGameOver();
            else if (World.BagOpen) bag.Draw(World);
            else if (World.StatsOpen) stats.Draw(World);
            else if (World.SkillsOpen) skills.Draw(World);
            else if (World.ReturnOpen) returnScreen.Draw(World);
            else if (World.MenuOpen) menu.Draw(World);
            else if (World.AltarOpen) altarScreen.Draw(World);
            if (World.EndingOpen) DrawEnding();
            else if (!World.ChoosingClass || World.InTown) Guide.Draw();
            DrawFlash();

            if (repaint)
            {
                // 描き終えたボタンの位置を、次のタップの判定に使う
                buttonRects.Clear();
                buttonRects.AddRange(pendingRects);
                buttonActions.Clear();
                buttonActions.AddRange(pendingActions);
            }
        }

        // ---------------- 画面の白い閃光 ----------------
        float flashTime = -1f, flashLife = 1f;

        /// <summary>画面全体を白く光らせて、ゆっくり戻す（アビスが崩れきったとき）。</summary>
        public void Flash(float duration)
        {
            flashTime = 0f;
            flashLife = Mathf.Max(0.1f, duration);
        }

        void DrawFlash()
        {
            if (flashTime < 0f) return;
            if (Event.current.type == EventType.Repaint) flashTime += Time.unscaledDeltaTime;
            float a = 1f - flashTime / flashLife;
            if (a <= 0f) { flashTime = -1f; return; }
            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(1f, 1f, 1f, a * a));
        }

        // ---------------- エンディング（1回だけ。見たあとは設定の「クレジット」から見返せる） ----------------
        float endingTime = -1f;
        // 流れ：白い閃光が引く → 深淵の塔 → ふつうの塔 → 夜明けの塔（世界が晴れていく）と言葉 → エンドロール → ロゴとお礼と記録
        const float RollStart = 12f, RollLength = 26f;
        const float EndingTapReady = RollStart + RollLength + 6f;

        /// <summary>エンディングを始める。creditsOnly なら、夜明けの塔のエンドロールから（設定の「クレジット」）。</summary>
        public void StartEnding(bool creditsOnly = false) => endingTime = creditsOnly ? RollStart - 1f : 0f;

        List<(string text, int size)> creditLines;

        /// <summary>エンドロールの中身：この世界の場所・立ちはだかった者・灯りを運んだ者たち。</summary>
        List<(string text, int size)> CreditLines()
        {
            var l = new List<(string, int)>();
            void Head(string t) { l.Add(("", 0)); l.Add((t, 2)); }
            l.Add(("VIVID OF FADE", 3));
            Head(Loc.T("- The Tower -", "― 塔 ―"));
            foreach (var n in Zones.TowerNames) l.Add((n, 1));
            Head(Loc.T("- The Abyss -", "― 深淵 ―"));
            foreach (var n in Zones.AbyssNames) l.Add((n, 1));
            Head(Loc.T("- Those Who Stood in the Way -", "― 立ちはだかった者たち ―"));
            foreach (BossKind k in System.Enum.GetValues(typeof(BossKind))) l.Add((BossInfo.Name(k), 1));
            Head(Loc.T("- Those Who Carried the Light -", "― 灯りを運んだ者たち ―"));
            for (int c = 0; c < ClassInfo.Count; c++)
            {
                var type = (ClassType)c;
                l.Add((ClassInfo.Get(type).DisplayName, 1));
                for (int a = 0; a < 2; a++) l.Add(($"{AdvancedClasses.Get(type, a).DisplayName}  /  {ThirdClasses.Get(type, a).DisplayName}", 1));
            }
            Head(Loc.T("- Guide -", "― 案内役 ―"));
            l.Add(("？？？", 1));
            l.Add(("", 0));
            l.Add(("", 0));
            return l;
        }

        /// <summary>遊んだ時間を「12時間34分」の形に。</summary>
        static string PlayTime(float seconds)
        {
            int m = Mathf.FloorToInt(seconds / 60f);
            return Loc.T($"{m / 60}h {m % 60:00}m", $"{m / 60}時間{m % 60:00}分");
        }

        void DrawEnding()
        {
            if (endingTime < 0f) endingTime = 0f;
            if (Event.current.type == EventType.Repaint) endingTime += Time.unscaledDeltaTime;
            float t = endingTime;
            float W = Screen.width, H = Screen.height;
            Fill(new Rect(0, 0, W, H), Color.black);

            // 背景：深淵の塔 → ふつうの塔 → 夜明けの塔（灯りが戻り、世界が晴れていく）
            float ps = Mathf.Ceil(Mathf.Max(W / TitleScene.W, H / TitleScene.H));
            var bg = new Rect((W - TitleScene.W * ps) * 0.5f, (H - TitleScene.H * ps) * 0.5f, TitleScene.W * ps, TitleScene.H * ps);
            float toNight = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 2.5f) / 3f));
            float toDawn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 6f) / 4f));
            var old = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(bg, TitleScene.AbyssTexture);
            GUI.color = new Color(1f, 1f, 1f, toNight);
            GUI.DrawTexture(bg, TitleScene.Texture);
            GUI.color = new Color(1f, 1f, 1f, toDawn);
            GUI.DrawTexture(bg, TitleScene.DawnTexture);
            GUI.color = old;
            // 夜明けの光：下から上へ、ゆっくり差し込む
            if (toDawn > 0f)
            {
                GUI.color = new Color(1f, 0.92f, 0.7f, 0.35f * toDawn * (0.85f + 0.15f * Mathf.Sin(t * 0.8f)));
                GUI.DrawTexture(new Rect(-W * 0.3f, H * 0.35f, W * 1.6f, H * 1.1f), TitleArt.SoftGlow);
                GUI.color = old;
            }
            // 最初は、崩壊の白い閃光が引いていく
            if (t < 1.5f) Fill(new Rect(0, 0, W, H), new Color(1f, 1f, 1f, 1f - t / 1.5f));
            // 言葉とエンドロールが読めるよう、うっすら暗く
            Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0.03f, 0.35f + 0.15f * Mathf.Clamp01((t - RollStart + 1f) / 2f)));

            var line = new GUIStyle(centered) { wordWrap = true, fontSize = Mathf.RoundToInt(label.fontSize * 1.05f) };
            void Line(float at, float until, float y, string text, Color color, GUIStyle style = null)
            {
                float a = Mathf.Clamp01((t - at) / 1.2f) * (until > 0f ? Mathf.Clamp01((until - t) / 1.2f) : 1f);
                if (a <= 0f) return;
                var st = style ?? line;
                st.normal.textColor = new Color(color.r, color.g, color.b, a);
                OutlinedLabel(new Rect(W * 0.06f, y, W * 0.88f, H * 0.05f), text, st, new Color(0f, 0f, 0f, 0.8f * a));
            }
            var soft = new Color(0.95f, 0.92f, 1f);
            // 世界が晴れていく間の言葉（エンドロールが始まる前に消える）
            Line(1.5f, RollStart, H * 0.2f, Loc.T("The light reached deeper than the bottom of the tower.", "灯りは、塔のいちばん底よりも、さらに深くへ届いた。"), soft);
            Line(4.5f, RollStart, H * 0.26f, Loc.T("The white shadow waiting in the dark quietly came undone.", "闇の底で待っていた白い影は、静かにほどけていった。"), soft);
            Line(7.5f, RollStart, H * 0.32f, Loc.T("Colors that were fading glow vivid once more.", "消えかけた色が、もう一度あざやかに灯る。"), new Color(1f, 0.9f, 0.6f));

            // エンドロール：下から上へ流れる
            if (t > RollStart && t < RollStart + RollLength + 1f)
            {
                if (creditLines == null || Event.current.type == EventType.Layout) creditLines = CreditLines();
                float lh = label.fontSize * 1.6f;
                float total = 0f;
                foreach (var c in creditLines) total += c.size == 3 ? lh * 2f : lh;
                float k = (t - RollStart) / RollLength;
                float y = Mathf.Lerp(H, -total, k);
                foreach (var c in creditLines)
                {
                    float h = c.size == 3 ? lh * 2f : lh;
                    if (c.size > 0 && y > -h && y < H)
                    {
                        float fade = Mathf.Clamp01(Mathf.Min(y - H * 0.08f, H * 0.92f - y) / (H * 0.12f)); // 上下の端では薄く
                        var st = new GUIStyle(centered) { fontStyle = c.size >= 2 ? FontStyle.Bold : FontStyle.Normal,
                            fontSize = c.size == 3 ? label.fontSize * 2 : c.size == 2 ? Mathf.RoundToInt(label.fontSize * 1.1f) : label.fontSize };
                        var col = c.size == 3 ? new Color(1f, 0.9f, 0.6f) : c.size == 2 ? new Color(1f, 0.82f, 0.45f) : new Color(0.95f, 0.95f, 1f);
                        st.normal.textColor = new Color(col.r, col.g, col.b, fade);
                        OutlinedLabel(new Rect(W * 0.06f, y, W * 0.88f, h), c.text, st, new Color(0f, 0f, 0f, 0.8f * fade));
                    }
                    y += h;
                }
            }

            // 最後：ロゴ・お礼・記録
            float end = RollStart + RollLength;
            float la = Mathf.Clamp01((t - end) / 1.5f);
            if (la > 0f)
            {
                var logo = TitleArt.Logo;
                float sc = Mathf.Max(1f, Mathf.Floor(W * 0.85f / logo.width));
                var lr = new Rect((W - logo.width * sc) * 0.5f, H * 0.34f, logo.width * sc, logo.height * sc);
                GUI.color = new Color(1f, 1f, 1f, la * (0.75f + 0.25f * Mathf.Sin(t * 2f)));
                GUI.DrawTexture(new Rect(lr.x - sc, lr.y - sc, lr.width + sc * 2f, lr.height + sc * 2f), TitleArt.Glow);
                GUI.color = new Color(1f, 1f, 1f, la);
                GUI.DrawTexture(lr, logo);
                GUI.color = old;
            }
            var thanks = new GUIStyle(centered) { fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(label.fontSize * 1.3f) };
            Line(end + 1.5f, -1f, H * 0.5f, "Thank you for playing", new Color(1f, 0.85f, 0.45f), thanks);
            var ch = Profile.Current;
            if (ch != null && t > end + 3f)
            {
                string record = $"{AdvancedClasses.NameFor(ch)}   Lv {ch.Level}" + (ch.Rebirths > 0 ? Loc.T($"   Reborn x{ch.Rebirths}", $"   転生 {ch.Rebirths}回") : "")
                    + "\n" + Loc.T($"Play time {PlayTime(ch.PlaySeconds)}   Defeated {ch.Kills:N0}", $"プレイ時間 {PlayTime(ch.PlaySeconds)}   倒した敵 {ch.Kills:N0}体")
                    + "\n" + Loc.T($"Fell {ch.Deaths} times", $"力尽きた回数 {ch.Deaths}回");
                var rs = new GUIStyle(centered) { wordWrap = true };
                rs.normal.textColor = new Color(0.9f, 0.9f, 0.95f, Mathf.Clamp01((t - end - 3f) / 1.2f));
                GUI.Label(new Rect(W * 0.06f, H * 0.58f, W * 0.88f, H * 0.1f), record, rs);
            }

            if (t >= EndingTapReady)
            {
                var tap = new GUIStyle(centered);
                tap.normal.textColor = new Color(1f, 1f, 1f, 0.5f + 0.5f * Mathf.Sin(t * 3f));
                GUI.Label(new Rect(0, H * 0.86f, W, H * 0.04f), Loc.T("Tap to continue", "タップで続ける"), tap);
            }
            // 全面のタップ：途中なら最後（ロゴ）まで飛ばし、最後なら閉じる
            Button(new Rect(0, 0, W, H), null, new Color(0f, 0f, 0f, 0f), centered, () =>
            {
                if (endingTime < end) endingTime = end;
                else if (endingTime >= EndingTapReady)
                {
                    endingTime = -1f;
                    World.CloseEnding();
                }
            });
        }

        void DrawHud()
        {
            var player = World.Player;
            var inv = World.Inventory;

            var ch = Profile.Current;
            float W = Screen.width, H = Screen.height, mm = BandMargin;
            // 帯はダンジョンより明るい色にし、境目に明るい線と暗い線を重ねて、暗い層でもくっきり分かれるようにする
            var bandColor = new Color(0.15f, 0.14f, 0.2f, 1f);
            var edgeColor = new Color(0.58f, 0.52f, 0.72f, 1f);
            var edgeDark = new Color(0f, 0f, 0f, 0.85f);
            float edgeT = Mathf.Max(2f, Mathf.Round(H * 0.0015f));

            // 上の帯と下の帯（真ん中はダンジョン）
            float topBottom = TopBandBottom, bottomTop = BottomBandTop;
            Fill(new Rect(0, 0, W, topBottom), bandColor);
            Fill(new Rect(0, topBottom - edgeT, W, edgeT), edgeColor);
            Fill(new Rect(0, topBottom, W, edgeT), edgeDark);
            Fill(new Rect(0, bottomTop, W, H - bottomTop), bandColor);
            Fill(new Rect(0, bottomTop, W, edgeT), edgeColor);
            Fill(new Rect(0, bottomTop - edgeT, W, edgeT), edgeDark);
            // 帯にも凹凸：帯の内側の縁に、照り返しの線と影の線
            Fill(new Rect(0, bottomTop + edgeT, W, edgeT), new Color(1f, 1f, 1f, 0.08f));
            Fill(new Rect(0, topBottom - edgeT * 2f, W, edgeT), new Color(0f, 0f, 0f, 0.35f));

            // 1段目：ゲームのロゴ（左）と MENU（右。あとで描く）
            var titleRow = new Rect(mm, mm, W - mm * 2f, TitleRowH);
            {
                var logo = TitleArt.Logo;
                float lh = titleRow.height * 0.92f, lw = logo.width * lh / logo.height;
                var logoRect = new Rect(titleRow.x + mm * 0.5f, titleRow.y + (titleRow.height - lh) * 0.5f, lw, lh);
                var oldColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 0.6f);
                GUI.DrawTexture(new Rect(logoRect.x - lh * 0.05f, logoRect.y - lh * 0.05f, logoRect.width + lh * 0.1f, logoRect.height + lh * 0.1f), TitleArt.Glow);
                GUI.color = oldColor;
                GUI.DrawTexture(logoRect, logo);
                if (ch != null && ch.Rebirths > 0)
                {
                    // 転生の印：MENU の左に、オーラの色の宝石と「転生N」
                    var aura = ThirdClasses.AuraColor(ch.Rebirths);
                    var countStyle = new GUIStyle(small) { alignment = TextAnchor.MiddleRight, fontStyle = FontStyle.Bold };
                    countStyle.normal.textColor = Color.Lerp(aura, Color.white, 0.35f);
                    string count = Loc.T($"Reborn x{ch.Rebirths}", $"転生{ch.Rebirths}");
                    float cw = countStyle.CalcSize(new GUIContent(count)).x;
                    float right = ToGui(MenuRect).x - mm * 1.2f;
                    GUI.Label(new Rect(right - cw, titleRow.y, cw, titleRow.height), count, countStyle);
                    float gem = titleRow.height * 0.8f;
                    var gemRect = new Rect(right - cw - mm * 0.4f - gem, titleRow.y + (titleRow.height - gem) * 0.5f, gem, gem);
                    var old2 = GUI.color;
                    GUI.color = aura;
                    DrawSprite(gemRect, PixelArt.RebirthGem);
                    GUI.color = old2;
                }
            }

            // 2段目：HP と MP（横いっぱい）
            float rowY = titleRow.yMax + mm * 0.8f, rowH = TopRowH;
            float barW = (W - mm * 3f) * 0.5f;
            var barText = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            barText.fontSize = Mathf.Min(barText.fontSize, Mathf.RoundToInt(rowH * 0.72f)); // 細いバーに収まる大きさ
            var hpBack = new Rect(mm, rowY, barW, rowH);
            float ratio = player.MaxHp > 0 ? Mathf.Clamp01((float)player.Hp / player.MaxHp) : 0f;
            OrnateBar(hpBack, ratio, new Color(0.15f, 0.04f, 0.05f, 1f), new Color(0.8f, 0.1f, 0.12f, 1f), FrameGold);
            OutlinedLabel(hpBack, $"HP  {player.Hp}/{player.MaxHp}", barText, new Color(0f, 0f, 0f, 0.85f));
            var mpBack = new Rect(hpBack.xMax + mm, rowY, barW, rowH);
            float mpRatio = player.MaxMp > 0 ? Mathf.Clamp01((float)player.Mp / player.MaxMp) : 0f;
            OrnateBar(mpBack, mpRatio, new Color(0.05f, 0.08f, 0.18f, 1f), new Color(0.2f, 0.35f, 0.75f, 1f), FrameGold);
            OutlinedLabel(mpBack, $"MP  {player.Mp}/{player.MaxMp}", barText, new Color(0f, 0f, 0f, 0.85f));

            // 3段目：レベル（左。「LV120★」まで入る幅）と経験値のバー
            float expY = rowY + rowH + mm * 0.8f;
            var lvBox = new Rect(mm, expY, W * 0.19f, ExpRowH);
            Fill(lvBox, new Color(0.04f, 0.04f, 0.07f, 1f));
            float lvGlow = LevelGlow;
            if (lvGlow > 0f) Fill(lvBox, new Color(1f, 0.8f, 0.3f, 0.45f * lvGlow)); // レベルアップの直後は光る
            OrnateFrame(lvBox, lvGlow > 0f ? Color.Lerp(FrameGold, new Color(1f, 1f, 0.8f, 1f), lvGlow) : FrameGold, FrameT, true, false);
            int level = ch != null ? ch.Level : 1;
            var lvStyle = new GUIStyle(centered) { fontStyle = FontStyle.Bold };
            string lvText = ch != null && ch.Level >= Progression.LevelCap(ch) ? $"LV{level}★" : $"LV{level}";
            FitLabel(new Rect(lvBox.x + lvBox.width * 0.06f, lvBox.y, lvBox.width * 0.88f, lvBox.height), lvText, lvStyle);
            var expBack = new Rect(lvBox.xMax + mm, expY, W - mm * 2f - lvBox.width - mm, ExpRowH);
            Fill(expBack, new Color(0.03f, 0.1f, 0.09f, 1f));
            if (ch != null)
            {
                float expRatio = ch.Level >= Progression.LevelCap(ch) ? 1f : Mathf.Clamp01((float)ch.Exp / Progression.ExpToNext(ch.Level));
                Fill(new Rect(expBack.x, expBack.y, expBack.width * expRatio, expBack.height), new Color(0.1f, 0.6f, 0.52f, 1f)); // 碧（HP の赤・MP の青と並んでも区別でき、白い文字も読める）
                Fill(new Rect(expBack.x, expBack.y, expBack.width * expRatio, expBack.height * 0.35f), new Color(0.8f, 1f, 0.95f, 0.22f)); // 光沢
                for (int k = 1; k < 10; k++) Fill(new Rect(expBack.x + expBack.width * k / 10f, expBack.y + expBack.height * 0.6f, 1f, expBack.height * 0.4f), new Color(0f, 0f, 0f, 0.3f)); // 10% ごとの目盛り
                var expText = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = Mathf.Max(8, Mathf.RoundToInt(ExpRowH * 0.7f)) };
                expText.normal.textColor = new Color(1f, 0.97f, 0.9f);
                OutlinedLabel(expBack, $"EXP {Mathf.FloorToInt(expRatio * 100f)}%", expText, new Color(0.1f, 0.07f, 0.04f, 0.9f));
            }
            OrnateFrame(expBack, FrameGold, FrameT, true, false);

            // 3.5段目：層（ボスのときは添える）と、右に回避の待ち時間
            float lineH = small.fontSize * 1.35f;
            var floorRow = new Rect(mm, expBack.yMax + mm * 0.8f, W - mm * 2f, FloorRowH);
            Fill(floorRow, new Color(0.05f, 0.05f, 0.08f, 1f));
            OrnateFrame(floorRow, FrameGold, FrameT, true, false);
            string floorText = GameConfig.FloorNameEn(World.Floors.Floor) + (World.Floors.IsBossFloor ? "   BOSS" : ""); // HUD では「71F」と短く（MENU・HP・EXP とそろえる）
            var floorStyle = new GUIStyle(small) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };
            float floorX = floorRow.x + mm;
            FitLabel(new Rect(floorX, floorRow.y, floorRow.width * 0.7f - (floorX - floorRow.x), floorRow.height), floorText, floorStyle);
            var dodge = new Rect(floorRow.xMax - W * 0.22f - mm, floorRow.y + floorRow.height * 0.35f, W * 0.22f, floorRow.height * 0.3f);
            Fill(dodge, new Color(0f, 0f, 0f, 0.6f));
            Fill(new Rect(dodge.x, dodge.y, dodge.width * player.DodgeReady, dodge.height), new Color(0.75f, 0.85f, 1f, 0.9f));
            OrnateFrame(dodge, FrameGold, Mathf.Max(1f, FrameT * 0.5f), true, false);
            float y = topBottom + mm;

            // FPS（ダンジョンの右上）
            if (ShowFps)
            {
                var fpsStyle = new GUIStyle(small) { alignment = TextAnchor.UpperRight };
                GUI.Label(new Rect(0, y, W - mm * 2f, lineH), $"FPS:{fps:0}", fpsStyle);
            }

            // 下の帯の2つの区画：メッセージウィンドウ（上）とショートカット（下）。それぞれ背景と枠で仕切る
            var frameColor = FrameGold;
            float frameT = Mathf.Max(2f, Mathf.Round(H * 0.0012f));
            var msgR = MessageWindow;
            Fill(msgR, new Color(0.03f, 0.03f, 0.05f, 1f));
            OrnateFrame(msgR, frameColor, frameT);
            var scR = ShortcutPanel;
            Fill(scR, new Color(0.08f, 0.075f, 0.11f, 1f));
            OrnateFrame(scR, frameColor, frameT);

            // メッセージ：直近の行を残す（新しいものほど下）。来たばかりの行は明るく、古い行は少し暗い
            {
                float logH = LogLineH;
                int start = Mathf.Max(0, messages.Count - LogLines);
                for (int k = start; k < messages.Count; k++)
                {
                    var t = messages[k];
                    var c = t.Color;
                    c.a = t.T < ToastLife ? 1f : 0.55f;
                    var style = new GUIStyle(small);
                    style.normal.textColor = c;
                    // 右下に「H：操作」がある行だけ、その幅をあける。収まらない文は縮めて1行に収める（切らない）
                    int row = k - start;
                    float reserve = 0f;
                    if (row == LogLines - 1 && !Application.isMobilePlatform)
                        reserve = Mathf.Max(reserve, small.CalcSize(new GUIContent(Loc.T("H: Controls", "H：操作"))).x + mm);
                    FitLabel(new Rect(msgR.x + mm, msgR.y + mm * 0.8f + row * logH, msgR.width - mm * 2f - reserve, logH), "+ " + t.Text, style);
                }
            }

            DrawBossBar();
            DrawGoldenMarker();
            DrawMinimap();
            DrawBanner();

            if (World.Paused || player.IsDead || World.ChoosingClass) return;

            // 右上は MENU だけ（ポイントが余っている・鞄が満杯なら「!」）
            int points = ch != null ? ch.Points : 0;
            bool notice = points > 0 || inv.BagFull;
            // MENU：いつも同じ暗いボタン。お知らせ（ポイントが余っている・鞄が満杯）があるときは、右上に脈打つ赤い丸の「!」
            var menuR = ToGui(MenuRect);
            Fill(menuR, new Color(0.05f, 0.05f, 0.08f, 1f));
            Button(menuR, null, new Color(0f, 0f, 0f, 0.3f), centered, () => World.OpenMenu());
            var menuStyle = new GUIStyle(centered) { fontStyle = FontStyle.Bold };
            FitLabel(new Rect(menuR.x + menuR.width * 0.08f, menuR.y, menuR.width * 0.84f, menuR.height), "MENU", menuStyle);
            OrnateFrame(menuR, FrameGold, FrameT);
            if (notice)
            {
                float bsz = menuR.height * 0.95f * (1f + 0.08f * Mathf.Sin(Time.unscaledTime * 6f));
                // 右上の角に重ねる。画面の上端・右端からはみ出さないように内側へ寄せる
                var badge = new Rect(Mathf.Min(menuR.xMax - bsz * 0.55f, W - bsz * 0.9f), Mathf.Max(menuR.y - bsz * 0.35f, -bsz * 0.1f), bsz, bsz);
                var oldC = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.8f);
                GUI.DrawTexture(new Rect(badge.x - 1f, badge.y - 1f, badge.width + 2f, badge.height + 2f), TitleArt.SoftGlow);
                GUI.color = oldC;
                Fill(new Rect(badge.x + bsz * 0.15f, badge.y + bsz * 0.15f, bsz * 0.7f, bsz * 0.7f), new Color(0.9f, 0.15f, 0.15f, 1f));
                Frame(new Rect(badge.x + bsz * 0.15f, badge.y + bsz * 0.15f, bsz * 0.7f, bsz * 0.7f), new Color(1f, 0.85f, 0.5f, 1f), Mathf.Max(1f, FrameT * 0.5f));
                var bang = new GUIStyle(centered) { fontStyle = FontStyle.Bold, fontSize = Mathf.Max(8, Mathf.RoundToInt(bsz * 0.6f)) };
                GUI.Label(badge, "!", bang);
            }

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
                var mw = MessageWindow;
                GUI.Label(new Rect(mw.x, mw.yMax - lineH - mm * 0.4f, mw.width - mm, lineH), Loc.T("H: Controls", "H：操作"), hint);
                if (ShowHelp) DrawHelp();
            }
        }

        /// <summary>操作の一覧（H で開閉）。</summary>
        public bool ShowHelp;

        /// <summary>FPS を表示する（メニューの設定）。</summary>
        /// <summary>FPS を出すか（設定。保存する）。</summary>
        public bool ShowFps
        {
            get => Profile.ShowFpsSetting;
            set => Profile.ShowFpsSetting = value;
        }

        static readonly string[] helpLines =
        {
            "Move ........ WASD / arrows (phone: drag)",
            "Dodge ....... Space (phone: flick)",
            "Skills ...... 1 / 2 / 3",
            "Attack mode . T  (HOLD: J or right click)",
            "Bag ......... B      Stats ... C",
            "Skill list .. K      Music ... M (on/off)  N (change)",
            "Potions ..... Q (HP)   E (MP)",
            "Map ......... F (or tap the minimap)",
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
            "薬 .......... Q（HP）  E（MP）",
            "MAP ......... F（小さな地図をタップでも）",
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

        /// <summary>大きな地図（画面中央に、半透明で地図全体を重ねる）を出しているか。ゲームは止めない。</summary>
        public static bool BigMap;

        /// <summary>右上の小さな地図の拡大率（自分のまわりを、地図全体のこの分の1だけ切り出して見せる）。</summary>
        const float MiniZoom = 2f;

        /// <summary>
        /// 小さな地図（右上のボタンの下）。自分のまわりを拡大して、歩いて見た場所・自分・階段・祭壇を出す。
        /// タップ（PC は F キー）で、地図全体を画面中央に大きく半透明で重ねる。もう一度で閉じる。
        /// </summary>
        void DrawMinimap()
        {
            var room = World.Room;
            if (room.Minimap == null) return;
            var below = new Rect(0, 0, Screen.width, TopBandBottom); // 上の帯の下から
            float w = Screen.width * 0.38f;
            float h = w * Room.MiniH / Room.MiniW;
            var r = new Rect(Screen.width - w - Screen.height * 0.015f, below.yMax + Screen.height * 0.01f, w, h);
            bool canTap = !(World.Paused || World.Player.IsDead || World.ChoosingClass);
            if (!canTap) BigMap = false; // 祭壇・鞄・メニューなどを開いたら、大きな地図は閉じる（重なって操作がわかりにくくならないように）

            if (BigMap)
            {
                // 画面中央に、地図全体を大きく（背景は少しだけ暗く、地図は半透明）
                float bw = Screen.width * 0.96f, bh = bw * Room.MiniH / Room.MiniW;
                var big = new Rect((Screen.width - bw) * 0.5f, Screen.height * 0.45f - bh * 0.5f, bw, bh);
                Fill(big, new Color(0f, 0f, 0f, 0.25f));
                DrawMapView(big, new Rect(0f, 0f, 1f, 1f), 0.6f, Mathf.Max(5f, bw * 0.014f));
                // 閉じるのは、大きな地図の右上の角の小さな「×」（地図そのものはボタンにしない。指の操作のじゃまをしないように）
                float cs = Screen.height * 0.04f;
                var close = new Rect(big.xMax - cs, big.y, cs, cs);
                if (canTap) Button(close, "×", new Color(0f, 0f, 0f, 0.65f), centered, () => BigMap = false);
                return;
            }

            // 自分のまわりを切り出す（地図の端では、はみ出さないように止める）
            Vector2 me = room.MinimapPixel(World.Player.Pos);
            float uw = 1f / MiniZoom, vh = 1f / MiniZoom;
            float u0 = Mathf.Clamp(me.x / Room.MiniW - uw * 0.5f, 0f, 1f - uw);
            float v0 = Mathf.Clamp(me.y / Room.MiniH - vh * 0.5f, 0f, 1f - vh);
            Fill(r, new Color(0f, 0f, 0f, 0.45f));
            DrawMapView(r, new Rect(u0, v0, uw, vh), 0.85f, Mathf.Max(4f, w * 0.045f));
            if (canTap) Button(r, null, new Color(0f, 0f, 0f, 0f), centered, () => BigMap = true);
        }

        /// <summary>地図の一部（view は地図全体を 0〜1 とした範囲、下が 0）を r に描き、自分・階段・祭壇の点を重ねる。</summary>
        void DrawMapView(Rect r, Rect view, float alpha, float dot)
        {
            var room = World.Room;
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTextureWithTexCoords(r, room.Minimap, view);
            GUI.color = old;

            void Dot(Vector2 logical, float size, Color c)
            {
                Vector2 m = room.MinimapPixel(logical);
                float fx = (m.x / Room.MiniW - view.x) / view.width, fy = (m.y / Room.MiniH - view.y) / view.height;
                if (fx < 0f || fx > 1f || fy < 0f || fy > 1f) return; // 切り出した範囲の外
                Fill(new Rect(r.x + fx * r.width - size * 0.5f, r.yMax - fy * r.height - size * 0.5f, size, size), c);
            }
            if (World.Floors.StairsOpen) Dot(room.StairsPos, dot * 1.3f, new Color(1f, 0.8f, 0.3f));
            if (room.HasAltar && !World.AltarUsed) Dot(room.AltarPos, dot, new Color(1f, 0.5f, 0.2f));
            Dot(World.Player.Pos, dot, Color.white);
        }

        /// <summary>
        /// 黄金のスライムの目印。画面の中にいれば頭の上で揺れる金色の「▼」（壁の陰でも見えるよう、画面の一番手前に描く）、
        /// 画面の外なら、ダンジョンの領域の端にその方向を指す金色の矢印。
        /// </summary>
        void DrawGoldenMarker()
        {
            var g = World.Enemies.Golden;
            var cam = Camera.main;
            if (g == null || cam == null || World.Paused) return;
            Vector2 world = Iso.ToScreen(g.Pos) + new Vector2(0f, 0.9f);
            Vector3 sp = cam.WorldToScreenPoint(new Vector3(world.x, world.y, 0f));
            var p = new Vector2(sp.x, Screen.height - sp.y); // GUI 座標
            float size = Screen.height * 0.022f;
            float top = TopBandBottom + size * 1.5f, bottom = BottomBandTop - size * 1.5f;
            float leftX = size * 1.5f, rightX = Screen.width - size * 1.5f;
            var gold = new Color(1f, 0.85f, 0.3f, 1f);
            var dark = new Color(0.1f, 0.06f, 0f, 0.9f);
            if (p.x >= leftX && p.x <= rightX && p.y >= top && p.y <= bottom)
            {
                float bob = Mathf.Sin(Time.unscaledTime * 6f) * size * 0.3f;
                var c = new Vector2(p.x, p.y - size + bob);
                var upSide = new Vector2(0f, -1f);          // 底辺は上（GUI 座標は下が +）→ 先が下を向く「▼」
                Triangle(c, upSide, size * 1.3f, dark);     // 縁取り
                Triangle(c, upSide, size, gold);
                return;
            }
            // 画面の外：ダンジョンの領域の中心から、スライムの方向へ伸ばして、端で止める
            var center = new Vector2(Screen.width * 0.5f, (TopBandBottom + BottomBandTop) * 0.5f);
            Vector2 d = p - center;
            if (d.sqrMagnitude < 1f) return;
            float tx = d.x > 0 ? (rightX - center.x) / d.x : d.x < 0 ? (leftX - center.x) / d.x : float.MaxValue;
            float ty = d.y > 0 ? (bottom - center.y) / d.y : d.y < 0 ? (top - center.y) / d.y : float.MaxValue;
            var edge = center + d * Mathf.Min(tx, ty);
            var dir = d.normalized;
            float pulse = 1f + 0.15f * Mathf.Sin(Time.unscaledTime * 8f);
            Triangle(edge, -dir, size * 1.5f * pulse, dark); // 先が外を向く三角
            Triangle(edge, -dir, size * 1.2f * pulse, gold);
        }

        /// <summary>
        /// 三角形を小さな四角を並べて描く（GUI 座標、下が +）。back は底辺のある側の向きで、先はその反対側を向く。
        /// </summary>
        static void Triangle(Vector2 center, Vector2 back, float size, Color color)
        {
            // 先 = center - back * size*0.6、底辺の中心 = center + back * size*0.4
            Vector2 tip = center - back * size * 0.6f;
            Vector2 baseC = center + back * size * 0.4f;
            Vector2 side = new Vector2(-back.y, back.x);
            float step = Mathf.Max(1f, size / 10f);
            int n = Mathf.CeilToInt(size / step);
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                Vector2 c = Vector2.Lerp(tip, baseC, t);
                float half = size * 0.55f * t;
                int m = Mathf.Max(1, Mathf.CeilToInt(half * 2f / step));
                for (int j = 0; j <= m; j++)
                {
                    Vector2 q = c + side * Mathf.Lerp(-half, half, j / (float)m);
                    Fill(new Rect(q.x - step * 0.6f, q.y - step * 0.6f, step * 1.2f, step * 1.2f), color);
                }
            }
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
            GUI.Label(new Rect(r.x, r.y - small.fontSize * 1.5f, r.width, small.fontSize * 1.4f), $"{BossInfo.Name(boss.BossKind)}  -  " + GameConfig.FloorName(World.Floors.Floor), small);
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
                float t = n.T / n.Life;
                Vector3 sp = cam.WorldToScreenPoint(new Vector3(n.World.x, n.World.y + t * (n.Wide ? 0.35f : 0.6f), 0f));
                var style = n.Big ? big : number;
                var c = n.Color;
                c.a = 1f - t * t;
                style.normal.textColor = c;
                float w = style.fontSize * (n.Wide ? 12f : 4f);
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
                GameConfig.FloorName(World.Floors.Floor) + Loc.T($"   KILL {World.Enemies.Kills}   EMBERS {World.Inventory.Embers}", $"   撃破 {World.Enemies.Kills}   火種 {World.Inventory.Embers}"), centered);
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
        bool floorTabAbyss; // 層選びで、深淵のタブを見ているか

        /// <summary>層選びを開いたとき：深淵に入れるなら、深淵のタブから始める。</summary>
        public void ResetFloorTab(CharacterData ch)
        {
            var all = GameWorld.Checkpoints(ch);
            floorTabAbyss = all.Count > 0 && GameConfig.IsAbyss(all[all.Count - 1]);
        }

        /// <summary>いま見ているタブの層（塔か深淵か）。数字キーもこの並びで選ぶ。</summary>
        public List<int> VisibleFloors(CharacterData ch)
        {
            var list = new List<int>();
            foreach (int f in GameWorld.Checkpoints(ch)) if (GameConfig.IsAbyss(f) == floorTabAbyss) list.Add(f);
            return list;
        }

        void DrawFloorSelect(ClassType type)
        {
            float W = Screen.width;
            float H = Screen.height;
            Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.8f));
            var ch = Profile.Get(type);
            var spec = ClassInfo.Get(type);

            var title = new GUIStyle(centered) { fontSize = label.fontSize * 2, fontStyle = FontStyle.Bold };
            Hud.FitLabel(new Rect(0, H * 0.12f, W, H * 0.06f), Loc.T("Choose a starting floor", "始める層を選ぶ"), title);
            GUI.Label(new Rect(0, H * 0.19f, W, H * 0.04f), $"{AdvancedClasses.NameOf(type)}   Lv {ch.Level}   " + Loc.T("Best ", "最深 ") + GameConfig.FloorName(ch.BestFloor), centered);

            var all = GameWorld.Checkpoints(ch);
            bool hasAbyss = all.Exists(GameConfig.IsAbyss);
            if (!hasAbyss) floorTabAbyss = false;
            var floors = VisibleFloors(ch);
            const int cols = 3;
            float pad = W * 0.08f;
            float gap = W * 0.03f;
            float bw = (W - pad * 2f - gap * (cols - 1)) / cols;
            float bh = H * 0.07f;
            float y = H * 0.27f;

            // 中断した挑戦があれば、いちばん上に「続きから」
            if (ch.Run.Active)
            {
                Button(new Rect(pad, y, W - pad * 2f, bh), Loc.T("CONTINUE  -  ", "続きから  -  ") + GameConfig.FloorName(ch.Run.Floor), new Color(0.2f, 0.5f, 0.25f, 0.85f), centered,
                    () => confirmFloor = 0); // 0 は「続きから」
                y += bh + gap;
                var warn = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter };
                warn.normal.textColor = new Color(1f, 0.7f, 0.5f);
                GUI.Label(new Rect(0, y, W, small.fontSize * 1.5f), Loc.T("Starting a new run abandons the suspended one.", "新しく始めると、中断中の挑戦は消えます。"), warn);
                y += small.fontSize * 2f;
            }
            // 塔と深淵のタブ（深淵に入れるときだけ）。選んでいるタブは明るく
            if (hasAbyss)
            {
                float tw = (W - pad * 2f - gap) * 0.5f, th = H * 0.05f;
                Button(new Rect(pad, y, tw, th), Loc.T("TOWER", "塔"), !floorTabAbyss ? new Color(0.5f, 0.4f, 0.1f, 0.85f) : new Color(1f, 1f, 1f, 0.06f), centered, () => floorTabAbyss = false);
                Button(new Rect(pad + tw + gap, y, tw, th), Loc.T("ABYSS", "深淵"), floorTabAbyss ? new Color(0.55f, 0.1f, 0.42f, 0.9f) : new Color(1f, 1f, 1f, 0.06f), centered, () => floorTabAbyss = true);
                y += th + gap * 1.5f;
            }
            // 行が多いときは、ボタンを低くして画面に収める
            int rowCount = (floors.Count + cols - 1) / cols;
            bh = Mathf.Min(bh, (H * 0.92f - y - H * 0.05f - gap * 2f) / Mathf.Max(1, rowCount) - gap);
            for (int i = 0; i < floors.Count; i++)
            {
                int floor = floors[i];
                var r = new Rect(pad + (i % cols) * (bw + gap), y + (i / cols) * (bh + gap), bw, bh);
                string key = Application.isMobilePlatform || i >= 9 ? "" : $"[{i + 1}] ";
                bool latest = i == floors.Count - 1; // そのタブでいちばん深い層は目立たせる
                Button(r, key + GameConfig.FloorName(floor), GameConfig.IsAbyss(floor) ? new Color(0.45f, 0.08f, 0.35f, latest ? 0.95f : 0.55f) : latest ? new Color(0.5f, 0.4f, 0.1f, 0.7f) : new Color(1f, 1f, 1f, 0.12f), centered,
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
                resume ? Loc.T($"Continue from {GameConfig.FloorName(floor)}?", $"{GameConfig.FloorName(floor)}から続けますか？") : Loc.T($"Start from {GameConfig.FloorName(floor)}?", $"{GameConfig.FloorName(floor)}から始めますか？"), title);
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
        /// タイトル画面。霧の洞窟の中央に、大きな逆さの塔が沈んでいる（背景は TitleScene が描く 180×320 ドットの絵）。
        /// 手前に大きくロゴ（VIVID は虹色に光り、FADE は崩れて粒が舞い散る）と副題。窓と最深部の灯りがゆらめき、塵が舞う。
        /// どこを押しても始まる（言語のボタンだけは別）。
        /// </summary>
        void DrawTitle()
        {
            float W = Screen.width;
            float H = Screen.height;
            float t = Time.unscaledTime;
            Button(new Rect(0, 0, W, H), null, new Color(0f, 0f, 0f, 0f), centered, World.LeaveTitle);

            // 背景：ドットの大きさがそろうように整数倍で拡大し、画面を覆う（はみ出す分は切れる）
            bool abyss = Profile.AbyssUnlocked; // 深淵へ行けるようになったら、深淵版の背景
            var bgTex = abyss ? TitleScene.AbyssTexture : TitleScene.Texture;
            float ps = Mathf.Ceil(Mathf.Max(W / TitleScene.W, H / TitleScene.H));
            var bg = new Rect((W - TitleScene.W * ps) * 0.5f, (H - TitleScene.H * ps) * 0.5f, TitleScene.W * ps, TitleScene.H * ps);
            GUI.DrawTexture(bg, bgTex);

            // 灯った窓のゆらめき
            foreach (var w in TitleScene.Windows)
            {
                if (!w.lit) continue;
                float r = TitleScene.TowerRadius(w.y + 4);
                float wx = bg.x + (TitleScene.TowerX + w.u * r) * ps, wy = bg.y + (w.y + 4) * ps;
                float f = 0.6f + 0.4f * Mathf.PerlinNoise(t * 3f, w.y * 0.37f + w.u * 5f);
                Glow(wx, wy, ps * 9f, abyss ? new Color(1f, 0.2f, 0.4f, 0.4f * f) : new Color(1f, 0.65f, 0.3f, 0.35f * f));
            }
            // 最深部の灯り（尖塔の先）
            float flick = 0.55f + 0.45f * Mathf.PerlinNoise(t * 2.2f, 0.3f);
            float bx = bg.x + TitleScene.TowerX * ps, by = bg.y + (TitleScene.SpireTip - 1) * ps;
            if (abyss)
            {
                // 深淵の口が脈打つ
                float beat = 0.6f + 0.4f * Mathf.Sin(t * 1.6f);
                Glow(bx, bg.y + 318f * ps, ps * 120f, new Color(1f, 0.15f, 0.45f, 0.35f * beat));
                Glow(bx, by, ps * 18f, new Color(1f, 0.4f, 0.6f, 0.45f * flick));
                Glow(bx, by, ps * 5f, new Color(1f, 0.85f, 0.9f, 0.85f * flick));
            }
            else
            {
                Glow(bx, by, ps * 22f, new Color(1f, 0.8f, 0.45f, 0.45f * flick));
                Glow(bx, by, ps * 6f, new Color(1f, 0.95f, 0.8f, 0.9f * flick));
            }

            // 漂う塵
            for (int i = 0; i < 30; i++)
            {
                float life = Mathf.Repeat(t * (0.02f + 0.03f * Hash01(i * 3 + 2)) + Hash01(i * 11 + 7), 1f);
                float ex = Hash01(i * 5 + 3) * W + Mathf.Sin(t * 0.5f + i) * W * 0.03f;
                float ey = H * (0.95f - 0.8f * life);
                float a = Mathf.Sin(life * Mathf.PI) * 0.5f;
                Fill(new Rect(ex, ey, ps * 0.5f, ps * 0.5f), abyss ? new Color(1f, 0.35f, 0.55f, a) : new Color(0.85f, 0.88f, 0.95f, a)); // 深淵では、下から立ちのぼる赤い火の粉
            }

            // 下側を暗くして文字を読みやすく
            for (int b = 0; b < 8; b++)
                Fill(new Rect(0, H * (0.82f + b * 0.0225f), W, H * 0.0225f + 1f), new Color(0f, 0f, 0f, 0.07f * (b + 1)));

            // ロゴ：後ろにぼかした影を敷き、VIVID のまわりの光、本体、FADE から崩れて舞い散る粒
            var logoTex = TitleArt.Logo;
            float sc = Mathf.Max(1f, Mathf.Floor(W * 0.95f / logoTex.width));
            var logoRect = new Rect((W - logoTex.width * sc) * 0.5f, H * 0.3f, logoTex.width * sc, logoTex.height * sc);
            var old = GUI.color;
            GUI.color = new Color(0.015f, 0.015f, 0.03f, 0.85f);
            float shw = W * 1.3f, shh = logoRect.height * 3.4f;
            GUI.DrawTexture(new Rect((W - shw) * 0.5f, logoRect.center.y + logoRect.height * 0.3f - shh * 0.5f, shw, shh), TitleArt.SoftGlow);
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

            // 副題（ロゴの下、両側に線）
            float ly = logoRect.yMax + H * 0.03f, lineW = W * 0.14f;
            Fill(new Rect(W * 0.5f - W * 0.24f - lineW, ly, lineW, 2f), new Color(0.85f, 0.8f, 0.9f, 0.6f));
            Fill(new Rect(W * 0.5f + W * 0.24f, ly, lineW, 2f), new Color(0.85f, 0.8f, 0.9f, 0.6f));
            var sub = new GUIStyle(centered);
            sub.normal.textColor = new Color(0.92f, 0.88f, 0.96f, 0.95f);
            FitLabel(new Rect(W * 0.26f, ly - H * 0.017f, W * 0.48f, H * 0.035f), (abyss ? Loc.T("Into the abyss no light can reach", "灯りの届かぬ深淵へ") : Loc.T("Into the tower where light fades", "灯りの消えゆく塔へ")), sub);

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
        public static void Glow(float cx, float cy, float r, Color color)
        {
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(cx - r, cy - r, r * 2f, r * 2f), TitleArt.SoftGlow);
            GUI.color = old;
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
            // 背景：タイトルと同じ塔の絵（深淵へ行けるようになったら深淵版）を、文字が読めるように暗くして敷く
            var bgTex = Profile.AbyssUnlocked ? TitleScene.AbyssTexture : TitleScene.Texture;
            float ps = Mathf.Ceil(Mathf.Max(W / TitleScene.W, H / TitleScene.H));
            GUI.DrawTexture(new Rect((W - TitleScene.W * ps) * 0.5f, (H - TitleScene.H * ps) * 0.5f, TitleScene.W * ps, TitleScene.H * ps), bgTex);
            Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0.02f, 0.62f));

            // タイトル画面と同じドットのロゴ（小さめ。「キャラクターを選ぶ」の上に収める）
            var logoTex = TitleArt.Logo;
            float sc = Mathf.Max(1f, Mathf.Floor(Mathf.Min(W * 0.8f / logoTex.width, H * 0.11f / logoTex.height)));
            var logoRect = new Rect((W - logoTex.width * sc) * 0.5f, H * 0.175f - logoTex.height * sc, logoTex.width * sc, logoTex.height * sc);
            var oldColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 2f));
            GUI.DrawTexture(new Rect(logoRect.x - sc, logoRect.y - sc, logoRect.width + sc * 2f, logoRect.height + sc * 2f), TitleArt.Glow);
            GUI.color = oldColor;
            GUI.DrawTexture(logoRect, logoTex);
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
            Fill(musicRect, new Color(0.03f, 0.03f, 0.06f, 0.85f));
            Button(musicRect, $"BGM: {World.Music.ThemeName}", new Color(1f, 1f, 1f, 0.1f), centered, () => World.NextMusic());
            OrnateFrame(musicRect, FrameGold, FrameT, false, false);
            var langRect = new Rect(musicRect.xMax + W * 0.02f, musicRect.y, half, musicRect.height);
            Fill(langRect, new Color(0.03f, 0.03f, 0.06f, 0.85f));
            Button(langRect, $"Language: {Loc.LanguageName}", new Color(1f, 1f, 1f, 0.1f), centered, () => Loc.Next());
            OrnateFrame(langRect, FrameGold, FrameT, false, false);
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
                    Fill(r, new Color(0.03f, 0.03f, 0.06f, 0.85f)); // 塔の絵（灯った窓）が透けて文字の邪魔をしないように
                    Button(r, null, new Color(1f, 1f, 1f, 0.06f), centered, () => newSlot = slot);
                    OrnateFrame(r, FrameGold, FrameT, false, false);
                    var empty = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
                    empty.normal.textColor = new Color(0.75f, 0.75f, 0.8f);
                    GUI.Label(r, key + Loc.T("+  New character", "＋  新しいキャラクター"), empty);
                }
                else
                {
                    Fill(r, new Color(0.03f, 0.03f, 0.06f, 0.85f));
                    Button(r, null, new Color(1f, 1f, 1f, 0.12f), centered, () => World.ChooseCharacter(slot));
                    OrnateFrame(r, FrameGold, FrameT);
                    float icon = r.height - inner * 2f;
                    DrawSprite(new Rect(r.x + inner, r.y + inner, icon, icon), PixelArt.PlayerFor(ch));
                    float tx = r.x + icon + inner * 2f;
                    float tw = r.xMax - tx - inner;
                    float lh = small.fontSize * 1.35f;
                    FitLabel(new Rect(tx, r.y + inner, tw * 0.7f, nameStyle.fontSize * 1.4f), key + AdvancedClasses.NameFor(ch), nameStyle);
                    var spec = ClassInfo.Get((ClassType)ch.ClassId);
                    // 1行に収まらなければ字を小さくする（火種の数が右端で切れないように。枠の右の余白の内側まで）
                    FitLabel(new Rect(tx, r.y + inner + nameStyle.fontSize * 1.5f, r.xMax - inner - tx, lh),
                        $"Lv {ch.Level}  " + Loc.T("Best ", "最深 ") + GameConfig.FloorName(ch.BestFloor) + Loc.T($"  Embers {ch.Embers}", $"  火種 {ch.Embers}"), new GUIStyle(small) { alignment = TextAnchor.MiddleLeft });
                    var sub = new GUIStyle(small);
                    sub.normal.textColor = new Color(0.75f, 0.75f, 0.8f);
                    GUI.Label(new Rect(tx, r.y + inner + nameStyle.fontSize * 1.5f + lh, tw, lh),
                        spec.DisplayName + (ch.Run.Active ? Loc.T("   (suspended: ", "   （中断中：") + GameConfig.FloorName(ch.Run.Floor) + Loc.T(")", "）") : ""), sub);

                    // 消す（2回押し）。消しても倉庫は残る
                    string id = "del" + slot;
                    bool armed = armedDelete == id && Time.unscaledTime < armedDeleteUntil;
                    // 右上の小さなごみ箱。1回押すと赤くなり「消す？」と出るので、もう一度押すと消える
                    float ds = r.height * 0.26f;
                    var del = new Rect(r.xMax - ds - inner * 0.5f, r.y + inner * 0.5f, ds, ds);
                    if (armed)
                    {
                        var ask = new GUIStyle(small) { alignment = TextAnchor.MiddleRight, fontStyle = FontStyle.Bold };
                        ask.normal.textColor = new Color(1f, 0.5f, 0.5f);
                        GUI.Label(new Rect(del.x - r.width * 0.3f - inner * 0.3f, del.y, r.width * 0.3f, ds), Loc.T("Delete?", "消す？"), ask);
                    }
                    Button(del, null, armed ? new Color(0.75f, 0.15f, 0.15f, 0.9f) : new Color(1f, 1f, 1f, 0.08f), centered, () =>
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
                    float ip = ds * 0.12f;
                    DrawSprite(new Rect(del.x + ip, del.y + ip, ds - ip * 2f, ds - ip * 2f), PixelArt.TrashIcon);
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
            // 文字のあるボタンは、どの画面でも同じ見た目：暗い下地＋色＋金の飾り枠（出っぱり）。文字のない当たり判定だけのボタンはそのまま
            bool decorate = text != null && background.a > 0.02f;
            if (decorate) Fill(guiRect, new Color(0.04f, 0.04f, 0.07f, 0.92f));
            Fill(guiRect, background);
            if (decorate) OrnateFrame(guiRect, FrameGold, Mathf.Max(1f, Mathf.Round(FrameT * 0.75f)), false, false);
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

        /// <summary>縁取りの付いた文字（バーの上の数字など。明るい所にも暗い所にもかかって読めるように）。</summary>
        public static void OutlinedLabel(Rect guiRect, string text, GUIStyle style, Color outline)
        {
            var s = new GUIStyle(style) { wordWrap = false, clipping = TextClipping.Overflow };
            var content = new GUIContent(text);
            float max = guiRect.width * 0.94f;
            while (s.fontSize > 8 && s.CalcSize(content).x > max) s.fontSize--;
            var shadow = new GUIStyle(s);
            shadow.normal.textColor = outline;
            float o = Mathf.Max(1f, Mathf.Round(s.fontSize * 0.08f));
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (dx != 0 || dy != 0) GUI.Label(new Rect(guiRect.x + dx * o, guiRect.y + dy * o, guiRect.width, guiRect.height), content, shadow);
            GUI.Label(guiRect, content, s);
        }

        public static void Fill(Rect guiRect, Color color)
        {
            // 端を整数のドットにそろえる（割合で決めた半端な座標のせいで、枠の線の太さが1ドットずれないように）。
            // 幅や高さが1ドット未満の線は消えないよう、最低1ドット残す
            float x0 = Mathf.Round(guiRect.x), y0 = Mathf.Round(guiRect.y);
            float x1 = Mathf.Max(x0 + (guiRect.width > 0f ? 1f : 0f), Mathf.Round(guiRect.xMax));
            float y1 = Mathf.Max(y0 + (guiRect.height > 0f ? 1f : 0f), Mathf.Round(guiRect.yMax));
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(x0, y0, x1 - x0, y1 - y0), Texture2D.whiteTexture);
            GUI.color = old;
        }

        /// <summary>強化した品なら、アイコンの右下に強化の紋章を重ねる。</summary>
        /// <summary>深淵の装備の枠：赤紫の二重の枠が、ゆっくり明滅する。</summary>
        public static void DrawAbyssFrame(Rect cell, Item item)
        {
            if (item == null || !item.Abyss) return;
            float t = Mathf.Max(2f, cell.width * 0.05f);
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
            var outer = RarityInfo.AbyssColor;
            outer.a = 0.6f + 0.4f * pulse;
            var inner = RarityInfo.AbyssColorDark;
            inner.a = 0.9f;
            void Frame(Rect r, Color c, float w)
            {
                Fill(new Rect(r.x, r.y, r.width, w), c);
                Fill(new Rect(r.x, r.yMax - w, r.width, w), c);
                Fill(new Rect(r.x, r.y, w, r.height), c);
                Fill(new Rect(r.xMax - w, r.y, w, r.height), c);
            }
            Frame(cell, outer, t);
            Frame(new Rect(cell.x + t, cell.y + t, cell.width - t * 2f, cell.height - t * 2f), inner, t * 0.6f);
        }

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
