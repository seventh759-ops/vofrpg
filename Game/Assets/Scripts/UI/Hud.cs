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
        public BagScreen Bag => bag;
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
                var g = MenuGui;
                return new Rect(g.x, Screen.height - g.y - g.height, g.width, g.height);
            }
        }

        /// <summary>上の情報カード（GUI 座標）。町の画面のカードと同じ作り：左 HP・MP・層／真ん中 Lv の丸と経験値の輪／右 MENU・転生・回避。</summary>
        static Rect TopCard => new Rect(BandMargin, BandMargin, Screen.width - BandMargin * 2f, TopCardH);
        static float TopCardH => Screen.height * 0.088f;
        static float TopLvD => TopCardH * 1.02f;
        /// <summary>ミニマップの上端（GUI 座標）。上の帯のすぐ下（メッセージと重なってよい。メッセージは地図の上に出る）。</summary>
        float MiniTop => TopBandBottom + Screen.height * 0.008f;

        /// <summary>MENU の歯車（GUI 座標）。ミニマップのすぐ下、右端。</summary>
        Rect MenuGui
        {
            get
            {
                float w = Screen.width * 0.38f, h = w * Room.MiniH / Room.MiniW;
                float right = Screen.width - Screen.height * 0.015f;
                float s = Screen.height * 0.05f;
                return new Rect(right - s, MiniTop + h + Screen.height * 0.008f, s, s);
            }
        }

        // ---- 上の帯と下の帯（スキルと薬）。真ん中がダンジョン ----
        // 上の帯：1段目 ゲームのロゴ・転生の印・MENU／2段目 HP・MP／3段目 レベルと経験値／3.5段目 層・回避
        static float BandMargin => Screen.height * 0.008f;
        static float TitleRowH => Screen.height * 0.024f;
        static float TopRowH => Screen.height * 0.021f; // HP・MP のバー（細め）
        static float ExpRowH => Screen.height * 0.008f; // 経験値は細いバーだけ
        /// <summary>上の帯の文字（HP・MP・LV・EXP・層・MENU）の1ドットの大きさ。いちばん低い段に合わせて、全部そろえる。</summary>
        static float HudPx => Mathf.Max(1f, Mathf.Round(Screen.height * 0.019f * 0.9f / NumberFont.Height));
        static float FloorRowH => Screen.height * 0.021f;

        /// <summary>まとめた1枚の枠（LV・HP・MP・経験値・層）の下端（GUI 座標）。</summary>
        static float StatusBottom => BandMargin * 1.6f + TopCardH;
        /// <summary>上の帯の下端（GUI 座標、上が 0）。ロゴの段と、まとめた1枚の枠まで（メッセージウィンドウは、その下のダンジョンに半透明で重ねる）。</summary>
        public float TopBandBottom => StatusBottom + BandMargin * 0.6f;
        /// <summary>メッセージウィンドウの下端（GUI 座標）。強化のアイコン・地図・画面の外を指す矢印は、ここより下に置く。自分もここと下の帯の間の真ん中に映す。</summary>
        float ViewTop => MessageWindow.yMax + BandMargin;
        static float PanelPad => BandMargin * 0.6f;
        /// <summary>LV・HP・MP・経験値・層をまとめた枠の高さ。</summary>
        static float DividerT => Mathf.Max(4f, FrameT * 2f); // 枠の2倍の太さ（縮小表示でも見えるように）
        static float StatusPanelH => PanelPad * 2.8f + TopRowH + BandMargin * 0.35f + ExpRowH + FloorRowH + DividerT * 1.5f;

        /// <summary>メッセージウィンドウの行数と、1行の高さ。</summary>
        const int LogLines = 3; // 上の帯に置くので3行（下の帯を薄くして、指で操作できる所を広げるため）
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

        /// <summary>メッセージウィンドウの区画（GUI 座標）。上の帯の、まとめた1枚の枠のすぐ下（下の帯を薄くするため、上へ移した）。</summary>
        Rect MessageWindow
        {
            get
            {
                float h = LogLines * LogLineH + BandMargin * 1.6f;
                float x = BandMargin * 1.5f;
                return new Rect(x, TopBandBottom + BandMargin * 0.5f, Screen.width - x * 2f, h);
            }
        }

        /// <summary>
        /// 鞄・能力値・スキルをすぐ開くボタン（GUI 座標）。スキルと同じ段の、スキルの反対側の空いた所に3つ並べる（スキルに近い方から 鞄・能力値・スキル）。
        /// </summary>
        Rect QuickRect(int i)
        {
            var first = SkillRect(0);
            float s = first.width;
            float gap = BandMargin * 0.8f;
            float left = BandMargin * 2.8f;                                         // 左手のときの並び（スキルが左、ボタンが右）で計算して、右手なら左右を入れかえる
            float from = left + SkillInfo.Slots * s * (1f + SkillGapK) - s * SkillGapK + gap * 1.5f;
            float to = Screen.width - left;
            float w = (to - from - gap * 2f) / 3f;
            float x = from + i * (w + gap);
            if (!LeftHanded) x = Screen.width - x - w;
            float full = s * 1.21f;                                                // スキルのボタンとオートの帯を合わせた高さ
            float h = full * 0.62f;                                                // ボタンは低めで下にそろえる。上の空きに「H：操作」
            return new Rect(x, Screen.height - (first.y + h), w, h);
        }

        /// <summary>下の帯の上端（GUI 座標）。ショートカットの区画（スキルと、鞄・能力値・スキルのボタン）を含む。</summary>
        float BottomBandTop => ShortcutPanel.y - BandMargin * 0.8f;

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
        static void OrnateBar(Rect r, float ratio, Color back, Color fill, Color frame, bool framed = true)
        {
            float t = Mathf.Max(2f, Mathf.Round(Screen.height * 0.0012f));
            Fill(r, back);
            var f = new Rect(r.x, r.y, r.width * ratio, r.height);
            Fill(f, fill);
            Fill(new Rect(f.x, f.y, f.width, f.height * 0.32f), new Color(1f, 1f, 1f, 0.22f));      // 光沢
            Fill(new Rect(f.x, f.yMax - f.height * 0.22f, f.width, f.height * 0.22f), new Color(0f, 0f, 0f, 0.25f)); // 下の陰
            if (ratio > 0f && ratio < 1f) Fill(new Rect(f.xMax - t * 0.5f, f.y, t * 0.5f, f.height), new Color(1f, 1f, 1f, 0.5f)); // 中身の端の光
            for (int k = 1; k < 4; k++) Fill(new Rect(r.x + r.width * k / 4f, r.y + r.height * 0.55f, 1f, r.height * 0.45f), new Color(0f, 0f, 0f, 0.45f)); // 目盛り
            if (framed) OrnateFrame(r, frame, t, true, false); // バーはへこみ（段が詰まっているので鋲はなし）
            else Frame(r, new Color(0f, 0f, 0f, 0.6f), 1f);     // 1枚の枠の中では、暗い細い線だけ
        }

        /// <summary>カメラのずれ（CameraRig.CenterShift）。上下の帯の高さの差の半分に、少し上げる分（ViewRaise）を足す。</summary>
        public float ViewCenterShift => ((Screen.height - BottomBandTop) - ViewTop) / (2f * Screen.height) + ViewRaise;

        /// <summary>ダンジョンの見える所を、帯の間の真ん中より少し上に（画面の高さに対する割合）。右下の薬・攻撃ボタンと重なりにくくするため。</summary>
        const float ViewRaise = 0.03f;

        /// <summary>
        /// スキルのボタン（0〜2）。下の段の、親指の側の端から スキル1・2・3（右手なら右端がスキル1、左手なら左端）。画面座標（左下原点）。
        /// </summary>
        Rect SkillRect(int slot)
        {
            // 大きさは前の並び（スキル3つと薬2つを横に並べた形）と同じ
            int n = SkillInfo.Slots + 2;
            float avail = Screen.width - BandMargin * 7f;
            float s = Mathf.Min(avail / (n + (n - 1) * SkillGapK + 0.35f), Screen.height * 0.09f);
            float x = BandMargin * 2.8f + slot * s * (1f + SkillGapK);
            if (!LeftHanded) x = Screen.width - x - s;
            return new Rect(x, BandMargin * 2.5f, s, s);
        }

        const float SkillGapK = 0.12f; // スキルのボタンどうしの間（ボタンの大きさに対する割合）

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
                int mpCost = SkillInfo.MpCost(spec, index, ch.Level, player.MaxMp);
                bool enoughMp = player.Mp >= mpCost;
                Button(r, null, new Color(0f, 0f, 0f, 0.55f), centered, () => { if (!World.Finale) World.Skills.Press(slot, World.Player); });
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

                // スキルレベルが上がった直後：スキルの色で光って、上に「LV n」が浮かぶ
                if (index == flashSkillIndex && flashSkillT >= 0f && flashSkillT < FlashSkillTime)
                {
                    float ft = flashSkillT / FlashSkillTime;
                    float pulse = (1f - ft) * (0.6f + 0.4f * Mathf.Sin(flashSkillT * 18f));
                    var fc = flashSkillColor; fc.a = 0.35f * pulse;
                    Fill(r, fc);
                    var bc = Color.Lerp(flashSkillColor, Color.white, 0.4f); bc.a = pulse;
                    OrnateFrame(r, bc, FrameT * 1.5f, false, false);
                    float rise = r.height * (0.15f + 0.45f * Mathf.Sqrt(ft));
                    var lc = Color.Lerp(Color.white, flashSkillColor, 0.35f); lc.a = ft < 0.7f ? 1f : 1f - (ft - 0.7f) / 0.3f;
                    NumberFont.DrawIn(new Rect(r.x - r.width * 0.3f, r.y - rise - r.height * 0.3f, r.width * 1.6f, r.height * 0.3f), "LV" + flashSkillLv, lc, TextAnchor.MiddleCenter, 1f);
                }

                // 消費 MP（右下に小さく）
                cost.normal.textColor = enoughMp ? new Color(0.65f, 0.8f, 1f) : new Color(1f, 0.5f, 0.5f);
                NumberFont.DrawIn(new Rect(r.x, r.yMax - r.height * 0.34f, r.width * 0.94f, r.height * 0.32f), mpCost.ToString(), cost.normal.textColor, TextAnchor.MiddleRight, 1f);

                // キー（PC は左上に 1・2・3。薬の Q・E と同じ）
                if (!Application.isMobilePlatform)
                {
                    var keyStyle = new GUIStyle(cost) { alignment = TextAnchor.UpperLeft };
                    keyStyle.normal.textColor = new Color(1f, 1f, 1f, 0.8f);
                    NumberFont.DrawIn(new Rect(r.x + r.width * 0.06f, r.y + r.height * 0.02f, r.width * 0.5f, r.height * 0.3f), (i + 1).ToString(), keyStyle.normal.textColor, TextAnchor.MiddleLeft, 1f);
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

            // かかっている強化：下の帯のすぐ上（薬と反対の側）に、スキルのアイコンと残り秒数。残り2秒を切ると点滅して、切れたら消える
            float bs = BadgeRect(0).width;
            var secStyle = new GUIStyle(small) { alignment = TextAnchor.LowerRight, fontStyle = FontStyle.Bold, fontSize = Mathf.Max(9, Mathf.RoundToInt(small.fontSize * 0.85f)) };
            for (int b = 0; b < player.BuffCount; b++)
            {
                float left = player.BuffRemaining(b);
                var br = BadgeRect(b);
                float alpha = left < 2f ? 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 8f)) : 1f;
                var old = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                DrawSprite(br, PixelArt.SkillIcon(player.BuffSpec(b)));
                GUI.color = old;
                NumberFont.DrawIn(new Rect(br.x, br.yMax - bs * 0.2f, br.width, bs * 0.45f), $"{Mathf.CeilToInt(left)}", new Color(1f, 1f, 1f, alpha), TextAnchor.MiddleRight, 1f);
            }
            // 彩灯者の分身：出ている間は、強化と同じく分身のスキルのアイコンと残り秒数
            int extraIcons = 0;
            var twin = World.Skills.Twin;
            if (twin != null && twin.Active)
            {
                float left = twin.Remaining;
                var br = BadgeRect(player.BuffCount);
                float alpha = left < 2f ? 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 8f)) : 1f;
                var old = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                DrawSprite(br, PixelArt.SkillIcon(SpecialClass.Spec.Skill));
                GUI.color = old;
                NumberFont.DrawIn(new Rect(br.x, br.yMax - bs * 0.2f, br.width, bs * 0.45f), $"{Mathf.CeilToInt(left)}", new Color(1f, 1f, 1f, alpha), TextAnchor.MiddleRight, 1f);
                extraIcons = 1;
            }
            // 弱体効果：強化のアイコンの右に続けて（赤い縁のアイコン）。残り2秒を切ると点滅
            for (int d = 0; d < player.DebuffCount; d++)
            {
                float left = player.DebuffRemaining(d);
                var br = BadgeRect(player.BuffCount + extraIcons + d);
                float alpha = left < 2f ? 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 8f)) : 1f;
                var old = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                DrawSprite(br, PixelArt.DebuffIcon(player.DebuffAt(d)));
                GUI.color = old;
                NumberFont.DrawIn(new Rect(br.x, br.yMax - bs * 0.2f, br.width, bs * 0.45f), $"{Mathf.CeilToInt(left)}", new Color(1f, 0.75f, 0.75f, alpha), TextAnchor.MiddleRight, 1f);
            }
        }

        /// <summary>
        /// 強化・弱体のアイコンの k 番目（GUI 座標）。下の帯のすぐ上に、薬と反対の側の端から並べる（右手なら左端から右へ）。
        /// </summary>
        Rect BadgeRect(int k)
        {
            float bs = Screen.height * 0.036f, gap = bs * 0.15f;
            float x = BandMargin * 2f + k * (bs + gap);
            if (LeftHanded) x = Screen.width - x - bs;
            return new Rect(x, BottomBandTop - BandMargin * 1.2f - bs, bs, bs);
        }

        /// <summary>
        /// 薬のボタン（0 回復薬・1 魔力の薬）。画面座標（左下原点）。親指の側の、下の帯のすぐ上（回復薬が外側）。
        /// 攻撃が「押す間」のときは、攻撃ボタンのすぐ上に置く。
        /// </summary>
        Rect PotionRect(int i)
        {
            float s = SkillRect(0).width;
            float m = Screen.height * 0.03f;
            float gap = BandMargin * 0.8f;
            float x = m + i * (s + gap);
            if (!LeftHanded) x = Screen.width - x - s;
            float y = World != null && !World.AutoAttack ? AttackRect.yMax + gap * 1.5f : Screen.height - BottomBandTop + m;
            return new Rect(x, y, s, s);
        }

        /// <summary>鞄・能力値・スキルのボタン。ポイントが余っていれば能力値を金色に、鞄が満杯なら鞄を赤くする。</summary>
        void DrawQuickButtons(CharacterData ch, bool pointsLeft, bool bagFull)
        {
            string[] names = { Loc.T("BAG", "鞄"), Loc.T("STATS", "能力値"), Loc.T("SKILLS", "スキル") };
            for (int i = 0; i < 3; i++)
            {
                var r = QuickRect(i);
                bool hot = (i == 0 && bagFull) || (i == 1 && pointsLeft);
                var back = i == 0 && bagFull ? new Color(0.55f, 0.12f, 0.12f, 1f) : i == 1 && pointsLeft ? new Color(0.5f, 0.38f, 0.08f, 1f) : new Color(0.08f, 0.075f, 0.11f, 1f);
                if (hot) back = Color.Lerp(back, Color.white, 0.12f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f))); // お知らせがあるボタンはゆっくり明滅
                Fill(r, back);
                int k = i;
                Button(r, null, new Color(0f, 0f, 0f, 0f), centered, () =>
                {
                    if (k == 0) World.OpenPanel(true, false, false);
                    else if (k == 1) World.OpenPanel(false, true, false);
                    else World.OpenPanel(false, false, true);
                });
                var st = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                st.normal.textColor = Color.white;
                FitLabel(r, i == 1 && pointsLeft && ch != null ? names[i] + $"\n+{ch.Points}" : names[i], st); // ボタンが縦長なので、ポイントは2行目に
                OrnateFrame(r, FrameGold, Mathf.Max(1f, FrameT * 0.7f));
            }
        }

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
                NumberFont.DrawIn(new Rect(r.x, r.yMax - r.height * 0.34f, r.width * 0.94f, r.height * 0.32f), n.ToString(), n > 0 ? Color.white : new Color(1f, 0.5f, 0.5f), TextAnchor.MiddleRight, 1f);
                if (!Application.isMobilePlatform) NumberFont.DrawIn(new Rect(r.x + r.width * 0.06f, r.y + r.height * 0.02f, r.width * 0.5f, r.height * 0.3f), hp ? "Q" : "E", key.normal.textColor, TextAnchor.MiddleLeft, 1f);
            }
        }

        public Rect AttackRect
        {
            get
            {
                // 手動攻撃のボタン（右下。下の帯の上に置く）
                float s = Mathf.Min(Screen.width, Screen.height) * 0.18f; // 薬を上に積むので、少し小さめ
                float m = Screen.height * 0.03f;
                float bandTop = Screen.height - BottomBandTop; // 下の帯の上端（画面座標）。メッセージウィンドウと重ならないように、その上に置く
                return new Rect(LeftHanded ? m : Screen.width - s - m, bandTop + m, s, s); // 左手なら左下
            }
        }

        /// <summary>
        /// ダンジョンが映っている所（上の帯と下の帯の間）か。画面座標（左下原点）。
        /// 仮想スティックは、ここで押したときだけ動く（帯の上のボタンを少し外して押しても、移動にならないように）。
        /// </summary>
        public bool IsInDungeonView(Vector2 screenPos)
        {
            float guiY = Screen.height - screenPos.y;
            return guiY > TopBandBottom && guiY < BottomBandTop;
        }

        /// <summary>ボタンの近く（ボタンのまわり margin ピクセルまで）か。ダンジョンの中のボタン（地図など）を少し外して押したときに、移動にしないため。</summary>
        public bool IsNearButton(Vector2 screenPos, float margin)
        {
            foreach (var r in buttonRects)
            {
                if (new Rect(r.x - margin, r.y - margin, r.width + margin * 2f, r.height + margin * 2f).Contains(screenPos)) return true;
            }
            return false;
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

        // ---- ダメージの数字：敵ごとに、ログのように縦に積み上がる（古いものが上、新しいものが下。多くなったら古いものから消える） ----
        sealed class DamageLine
        {
            public string Text;
            public Color Color;
            public bool Big;
            public float T;
            public float Slot = -0.8f; // 下から何行目に見えているか（新しい行は下から跳ね上がってくる）
            public float Vel = 10f;    // Slot の動く速さ（ばね）。はじめは上向きに勢いよく
            public float Leave = -1f;  // 上からあふれて消えていく途中（経過時間）。-1 はまだ
        }

        sealed class DamageStack
        {
            public Vector2 World;  // 一番下（新しい行）の位置（画面のワールド座標）
            public object Target;  // どの敵のログか（敵ごとに分ける）
            public float LastAdd;  // 最後に足した時刻
            public readonly List<DamageLine> Lines = new List<DamageLine>();
        }

        readonly List<DamageStack> damageStacks = new List<DamageStack>();
        const int MaxDamageLines = 6;      // 1つの敵に出す行の数
        const float DamageLineLife = 0.6f; // 1行が消えるまで（短めにして、画面が数字で埋まらないように）
        const float DamageJoinDist = 0.7f; // この近さなら同じ敵の行として積む
        const float DamageJoinTime = 0.9f;
        const int MaxDamageStacks = 40;

        public void AddNumber(Vector2 world, float amount, bool crit, object target = null)
        {
            Vector2 p = Iso.ToScreen(world) + new Vector2(0f, 0.45f);
            float now = Time.unscaledTime;
            DamageStack stack = null;
            float best = DamageJoinDist * DamageJoinDist;
            foreach (var st in damageStacks)
            {
                if (now - st.LastAdd > DamageJoinTime) continue;
                if (target != null)
                {
                    if (st.Target == target) { stack = st; break; } // 同じ敵だけに積む（近くのほかの敵とは混ぜない）
                    continue;
                }
                float d = (st.World - p).sqrMagnitude;
                if (d < best) { best = d; stack = st; }
            }
            if (stack == null)
            {
                if (damageStacks.Count >= MaxDamageStacks) damageStacks.RemoveAt(0);
                stack = new DamageStack { Target = target };
                damageStacks.Add(stack);
            }
            stack.World = p; // 敵が動いたら、ついていく
            stack.LastAdd = now;
            stack.Lines.Add(new DamageLine
            {
                Text = Mathf.RoundToInt(amount).ToString(),
                Color = crit ? new Color(1f, 0.85f, 0.2f) : Color.white,
                Big = crit,
            });
            // あふれたら、一番古い行が上へ抜けながら消えていく
            int live = 0;
            foreach (var l in stack.Lines) if (l.Leave < 0f) live++;
            for (int j = 0; j < stack.Lines.Count && live > MaxDamageLines; j++)
            {
                if (stack.Lines[j].Leave >= 0f) continue;
                stack.Lines[j].Leave = 0f;
                live--;
            }
        }

        void TickDamageStacks(float dt)
        {
            for (int i = damageStacks.Count - 1; i >= 0; i--)
            {
                var st = damageStacks[i];
                // 敵が倒れたら、そのログはさらに少し早く消える
                bool dead = st.Target is Enemy en && (en.Dying || !en.gameObject.activeSelf);
                float lineDt = dead ? dt * 1.5f : dt;
                // 行の位置：新しい行が一番下（0）、古いほど上。ばねで動かすので、下から跳ね上がって少し行き過ぎてから落ち着く
                int rank = 0;
                for (int j = st.Lines.Count - 1; j >= 0; j--)
                {
                    var ln = st.Lines[j];
                    ln.T += lineDt;
                    float target;
                    if (ln.Leave >= 0f) { ln.Leave += dt; target = MaxDamageLines + ln.Leave * 3f; }
                    else target = rank++;
                    ln.Vel += (target - ln.Slot) * 260f * dt;
                    ln.Vel *= Mathf.Exp(-14f * dt);
                    ln.Slot += ln.Vel * dt;
                }
                for (int j = st.Lines.Count - 1; j >= 0; j--)
                {
                    var ln = st.Lines[j];
                    if (ln.T >= DamageLineLife || ln.Leave >= 0.3f) st.Lines.RemoveAt(j);
                }
                if (st.Lines.Count == 0) damageStacks.RemoveAt(i);
            }
        }

        void DrawDamageStacks(Camera cam)
        {
            float px = Mathf.Max(2f, Mathf.Round(number.fontSize / 8.5f)); // 少し小さめ
            float lineH = NumberFont.Height * px * 0.95f;
            foreach (var st in damageStacks)
            {
                Vector3 sp = cam.WorldToScreenPoint(new Vector3(st.World.x, st.World.y, 0f));
                float y = Screen.height - sp.y;
                // 下（新しい）から上へ。Slot の高さに描く（上へ流れていき、上にあふれた行は薄れて消える）
                // 一番下（新しい）ほど大きく、上へ流れるほど小さくなる（滝のように、上が細く積み上がる）
                const float bottomScale = 1.35f, topScale = 0.55f;
                float m = MaxDamageLines - 1;
                foreach (var ln in st.Lines)
                {
                    float x = Mathf.Max(-1f, ln.Slot);
                    float scale = Mathf.Clamp(bottomScale - (bottomScale - topScale) * x / m, 0.45f, 1.8f);
                    // 高さは、下の行の大きさを積み上げた分（大きさの積分）
                    float rise = (bottomScale * x - (bottomScale - topScale) * 0.5f * x * x / m) * lineH;
                    float lp = px * scale * (ln.Big ? 1.25f : 1f);
                    lp *= 1f + (ln.Big ? 0.35f : 0.15f) * Mathf.Exp(-ln.T * 14f); // 出た瞬間、少しふくらむ
                    var c = ln.Color;
                    c.a = Mathf.Clamp01((DamageLineLife - ln.T) / 0.2f) * (ln.Leave >= 0f ? 1f - ln.Leave / 0.3f : 1f) * Mathf.Clamp01(ln.Slot + 1.2f);
                    float h = NumberFont.Height * lp;
                    NumberFont.Draw(new Vector2(sp.x, y - h * 0.5f - rise), ln.Text, lp, c);
                }
            }
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

        // レベルアップ：札（テロップ）は出さず、LV の欄を光らせる時間だけ数える（知らせは羽と光の演出）
        float plateTime = -1f;
        const float PlateLife = 2.6f;

        /// <summary>レベルアップした（頭の上の「LV UP」と、LV の欄の光）。</summary>
        public void ShowLevelUp(int level, int points)
        {
            levelUpTime = 0f;
            plateTime = 0f;
        }

        /// <summary>レベルアップのあとの光り具合（0〜1）。HUD の LV の欄も光らせる。</summary>
        float LevelGlow => plateTime < 0f ? 0f : Mathf.Clamp01(1f - plateTime / PlateLife) * (0.6f + 0.4f * Mathf.Sin(plateTime * 10f));

        void DrawLevelUp()
        {
            if (levelUpTime < 0f) return;
            var cam = Camera.main;
            if (cam == null) return;
            // 羽が頭の上で開いた瞬間に出て、羽と一緒に上がりながら消える
            const float showAt = FxSystem.WingOpenAt + 0.08f; // 羽が開ききったところで
            float t = levelUpTime - showAt;
            if (t < 0f) return;
            const float fadeFrom = 1.15f - showAt;
            float alpha = Mathf.Clamp01(t / 0.06f) * (t > fadeFrom ? Mathf.Clamp01(1f - (t - fadeFrom) / (FxSystem.LevelUpDuration - showAt - fadeFrom)) : 1f);
            var head = Iso.ToScreen(World.Player.Pos) + new Vector2(0f, World.Fx.WingHeight); // 羽の付け根と同じ高さ
            Vector3 sp = cam.WorldToScreenPoint(new Vector3(head.x, head.y, 0f));
            // ドット絵の「LV UP」。画面の大きさに合わせて整数倍で拡大（にじまないように）
            var tex = LevelUpArt.Texture;
            float pop = 1f + 0.35f * Mathf.Clamp01(1f - t / 0.18f); // 出た瞬間だけ大きく
            // 1ドットの大きさは、ダメージの数字（一番下の大きい行）と同じにそろえる（字形も同じなので、太さがそろう）
            float px = Mathf.Max(2f, Mathf.Round(number.fontSize / 8.5f)) * 1.4f * pop; // 字が太くなった分、少しだけ小さめ
            float w = tex.width * px, h = tex.height * px;
            var r = new Rect(Mathf.Round(sp.x - w * 0.5f), Mathf.Round(Screen.height - sp.y - h * 0.5f), w, h);
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(r, tex);
            GUI.color = old;
        }

        float skillUpTime = -1f;
        const float SkillUpLife = 1.2f;

        /// <summary>
        /// スキルレベルが上がったとき、頭の上に「SKILL UP」（ドット文字、スキルの色）。
        /// キャラのレベルアップ（虹色の「LV UP」と翼）と見分けがつくよう、単色で小さめ、ぽんと出てから少し浮かんで消える。
        /// </summary>
        void DrawSkillUp()
        {
            if (skillUpTime < 0f) return;
            var cam = Camera.main;
            if (cam == null) return;
            float t = skillUpTime / SkillUpLife;
            float alpha = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
            float extra = levelUpTime >= 0f ? 0.7f : 0f; // キャラのレベルアップと重なったら、「LV UP」の上に出す
            var head = Iso.ToScreen(World.Player.Pos) + new Vector2(0f, 1.35f * GameConfig.PlayerScale + extra + 0.35f * Mathf.Sqrt(t));
            Vector3 sp = cam.WorldToScreenPoint(new Vector3(head.x, head.y, 0f));
            float pop = 1f + 0.4f * Mathf.Clamp01(1f - skillUpTime / 0.15f);
            float px = Mathf.Max(2f, Mathf.Round(number.fontSize / 8.5f)) * pop;
            var c = Color.Lerp(flashSkillColor, Color.white, 0.25f); c.a = alpha;
            var center = new Vector2(sp.x, Screen.height - sp.y);
            var shadow = new Color(0.05f, 0.03f, 0.08f, 0.9f * alpha);
            float o = Mathf.Max(1f, Mathf.Round(px * 0.5f));
            foreach (var d in new[] { new Vector2(o, 0), new Vector2(-o, 0), new Vector2(0, o), new Vector2(0, -o), new Vector2(o, o) })
                NumberFont.Draw(center + d, "SKILL UP", px, shadow);
            NumberFont.Draw(center, "SKILL UP", px, c);
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

        /// <summary>知らせを、画面には出さず履歴にだけ残す。</summary>
        public void Log(string text, Color color)
        {
            if (messages.Count >= 20) messages.RemoveAt(0);
            messages.Add(new ToastLine { Text = text, Color = color });
        }

        // ---- スキルレベルが上がったときのボタンの光 ----
        int flashSkillIndex = -1, flashSkillLv;
        float flashSkillT = -1f;
        Color flashSkillColor;
        const float FlashSkillTime = 1.4f;

        /// <summary>スキルレベルが上がった：そのスキルのボタンを光らせ、「LV n」を浮かべる。</summary>
        public void FlashSkill(int index, int lv, Color color)
        {
            skillUpTime = 0f;
            flashSkillIndex = index;
            flashSkillLv = lv;
            flashSkillColor = color;
            flashSkillT = 0f;
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
            if (flashSkillT >= 0f && World != null && !World.Paused && (flashSkillT += dt) >= FlashSkillTime) flashSkillT = -1f;
            if (skillUpTime >= 0f && World != null && !World.Paused && (skillUpTime += dt) >= SkillUpLife) skillUpTime = -1f;

            for (int i = numbers.Count - 1; i >= 0; i--)
            {
                numbers[i].T += dt;
                if (numbers[i].T >= numbers[i].Life) numbers.RemoveAt(i);
            }
            TickDamageStacks(dt);
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
            GameFont.Apply();
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
                DrawSkillUp();
                DrawBoonGet();
                DrawEliteLabels();
                DrawHud();
            }

            if (World.OnTitle) DrawTitle();
            else if (World.ChoosingClass && World.PendingClass.HasValue) DrawFloorSelect(World.PendingClass.Value);
            else if (World.ChoosingClass && World.InTown && (World.BagOpen || World.StatsOpen || World.SkillsOpen || World.MenuOpen) && DrawTownPanelBackdrop()) { } // 町から開く画面は、後ろに町の絵（ダンジョンやキャラクターが透けないように）
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
            if (World.PrologueOpen) DrawPrologue();
            else if (World.EndingOpen) DrawEnding();
            else if (!World.ChoosingClass || World.InTown) Guide.Draw();
            DrawSlowMoGlow();
            DrawCelebration();
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
        public void Flash(float duration, Color? color = null)
        {
            flashTime = 0f;
            flashLife = Mathf.Max(0.1f, duration);
            flashColor = color ?? Color.white;
        }

        Color flashColor = Color.white;

        // ---- はじまりの語り（初めて遊ぶとき一度だけ。設定から見返せる） ----
        // 灯りの消えた塔の絵の上に、言葉が1行ずつ浮かぶ。タップで次の行へ、最後のあとのタップで閉じる。
        float prologueTime = -1f;
        int prologueShown; // 出し終えた行の数（タップで進める）
        const float PrologueLineGap = 2.6f;

        static string[] PrologueLines => Loc.Ja ? new[]
        {
            "かつて、世界から灯りが失われた。",
            "灯りを失った世界は、混沌に呑まれた。",
            "あるとき、胸に灯りを秘めた者が現れ、世界にふたたび灯りをともした。",
            "そうして、世界に平和が訪れた。",
            "けれど今、その灯りが、また失われつつある。",
            "女神は、ひとりの者に、ひとつの可能性を見出した。",
            "そして、自らを像に変える代償とひきかえに、その者に戦う力を授けた。",
        } : new[]
        {
            "Long ago, the light was lost from the world.",
            "Without its light, the world fell into chaos.",
            "Then one who carried a light within their heart appeared, and lit the world once more.",
            "And so, peace came to the world.",
            "But now, that light is fading again.",
            "The goddess saw a single possibility in one person.",
            "At the price of turning herself into stone, she granted them the power to fight.",
        };

        public void StartPrologue()
        {
            prologueTime = 0f;
            prologueShown = 0;
        }

        void DrawPrologue()
        {
            if (prologueTime < 0f) prologueTime = 0f;
            if (Event.current.type == EventType.Repaint) prologueTime += Time.unscaledDeltaTime;
            float t = prologueTime;
            float W = Screen.width, H = Screen.height;
            var lines = PrologueLines;
            // 時間がたてば自動で次の行へ（タップで早送り）
            prologueShown = Mathf.Max(prologueShown, Mathf.Min(lines.Length, Mathf.FloorToInt(t / PrologueLineGap) + 1));
            Fill(new Rect(0, 0, W, H), Color.black);
            // 背景：灯りの消えかけた塔（ゆっくり明るくなる）
            float ps = Mathf.Ceil(Mathf.Max(W / TitleScene.W, H / TitleScene.H));
            var bg = new Rect((W - TitleScene.W * ps) * 0.5f, (H - TitleScene.H * ps) * 0.5f, TitleScene.W * ps, TitleScene.H * ps);
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.35f * Mathf.Clamp01(t / 3f));
            GUI.DrawTexture(bg, TitleScene.Texture);
            GUI.color = old;
            Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0.03f, 0.45f));
            // 文字は同じ大きさのまま、長い行は折り返す（縮めない）。行の高さは折り返した分だけ伸ばす
            var st = new GUIStyle(centered) { wordWrap = true, clipping = TextClipping.Overflow, fontSize = Mathf.RoundToInt(label.fontSize * 1.05f) };
            float tw = W * 0.84f, tx = (W - tw) * 0.5f;
            float gap = H * 0.022f;
            var heights = new float[lines.Length];
            float total = 0f;
            for (int i = 0; i < lines.Length; i++) { heights[i] = st.CalcHeight(new GUIContent(lines[i]), tw); total += heights[i] + (i > 0 ? gap : 0f); }
            float y = H * 0.5f - total * 0.5f;
            var shadow = new GUIStyle(st);
            float o = Mathf.Max(1f, Mathf.Round(st.fontSize * 0.08f));
            for (int i = 0; i < lines.Length; i++)
            {
                if (i < prologueShown)
                {
                    float since = t - i * PrologueLineGap;
                    float a = Mathf.Clamp01(since / 1.2f);
                    if (i >= Mathf.FloorToInt(t / PrologueLineGap) + 1) a = 1f; // タップで出した行はすぐに
                    bool last = i == lines.Length - 1;
                    st.normal.textColor = last ? new Color(1f, 0.88f, 0.6f, a) : new Color(0.95f, 0.92f, 1f, a);
                    shadow.normal.textColor = new Color(0f, 0f, 0f, 0.8f * a);
                    var r = new Rect(tx, y, tw, heights[i]);
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                            if (dx != 0 || dy != 0) GUI.Label(new Rect(r.x + dx * o, r.y + dy * o, r.width, r.height), lines[i], shadow);
                    GUI.Label(r, lines[i], st);
                }
                y += heights[i] + gap;
            }
            if (prologueShown >= lines.Length)
            {
                var tap = new GUIStyle(centered);
                tap.normal.textColor = new Color(1f, 1f, 1f, 0.5f + 0.5f * Mathf.Sin(t * 3f));
                GUI.Label(new Rect(0, H * 0.86f, W, H * 0.04f), Loc.T("Tap to continue", "タップで続ける"), tap);
            }
            // 全面のタップ：まだ行が残っていれば次の行を出し、出し終えていれば閉じる
            Button(new Rect(0, 0, W, H), null, new Color(0f, 0f, 0f, 0f), centered, () =>
            {
                if (prologueShown < lines.Length) prologueShown++;
                else { prologueTime = -1f; World.ClosePrologue(); }
            });
        }

        /// <summary>
        /// 町の画面の上の帯。ダンジョンの上の帯と同じ作り：1段目 ロゴ・転生の印・設定の歯車（MENU の場所）／
        /// 1枚の枠に LV・HP・MP（町では満タン）・経験値、仕切りの下に 職業と最高到達、火種と欠片。下端（GUI 座標）を返す。
        /// </summary>
        public float DrawTownHeader(CharacterData ch, PlayerStats st, System.Action openSettings)
        {
            float W = Screen.width, mm = BandMargin;
            // 1段目：ロゴ・転生の印・歯車
            var titleRow = new Rect(mm, mm, W - mm * 2f, TitleRowH);
            // 町では帯の地を塗らない：町の空の絵の上に、ロゴと枠だけを乗せる
            var menu = ToGui(MenuRect);
            float gearS = menu.height * 1.15f;
            var gearR = new Rect(menu.xMax - gearS, menu.y + (menu.height - gearS) * 0.5f, gearS, gearS);
            {
                var logo = TitleArt.Logo;
                float lh = titleRow.height * 0.92f, lw = logo.width * lh / logo.height;
                var logoRect = new Rect(titleRow.x + mm * 0.5f, titleRow.y + (titleRow.height - lh) * 0.5f, lw, lh);
                var oldColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 0.6f);
                GUI.DrawTexture(new Rect(logoRect.x - lh * 0.05f, logoRect.y - lh * 0.05f, logoRect.width + lh * 0.1f, logoRect.height + lh * 0.1f), TitleArt.Glow);
                GUI.color = oldColor;
                GUI.DrawTexture(logoRect, logo);
                if (ch.Rebirths > 0)
                {
                    var aura = ThirdClasses.AuraColor(ch.Rebirths);
                    var countStyle = new GUIStyle(small) { alignment = TextAnchor.MiddleRight, fontStyle = FontStyle.Bold };
                    countStyle.normal.textColor = Color.Lerp(aura, Color.white, 0.35f);
                    string count = Loc.T($"Reborn x{ch.Rebirths}", $"転生{ch.Rebirths}");
                    float cw = countStyle.CalcSize(new GUIContent(count)).x;
                    float right = gearR.x - mm * 1.2f;
                    GUI.Label(new Rect(right - cw, titleRow.y, cw, titleRow.height), count, countStyle);
                    float gem = titleRow.height * 0.8f;
                    var old2 = GUI.color;
                    GUI.color = aura;
                    DrawSprite(new Rect(right - cw - mm * 0.4f - gem, titleRow.y + (titleRow.height - gem) * 0.5f, gem, gem), PixelArt.RebirthGem);
                    GUI.color = old2;
                }
                Button(gearR, null, new Color(1f, 1f, 1f, 0.06f), centered, openSettings);
                DrawSprite(new Rect(gearR.x + gearS * 0.08f, gearR.y + gearS * 0.08f, gearS * 0.84f, gearS * 0.84f), PixelArt.GearIcon);
            }

            // 枠：LV・HP・MP・経験値、仕切り、職業と最高到達、火種と欠片
            float pp = PanelPad, dt = DividerT;
            float sh = Mathf.Max(1f, Mathf.Round(dt * 0.5f));
            float infoRowH = FloorRowH * 1.15f;
            float panelH = PanelPad * 2.8f + TopRowH + mm * 0.35f + ExpRowH + dt * 1.5f + infoRowH * 2f + pp * 0.6f;
            var panel = new Rect(mm, titleRow.yMax + mm * 0.6f, W - mm * 2f, panelH);
            Fill(panel, new Color(0.05f, 0.045f, 0.08f, 0.82f));                 // 枠の中は少し透かして、後ろの空がうっすら見える
            var hi = new Color(1f, 0.95f, 0.75f, 0.9f);
            var lo = new Color(0f, 0f, 0f, 0.75f);
            void HDivider(float x, float y, float w)
            {
                Fill(new Rect(x, y - sh, w, sh), lo); Fill(new Rect(x, y, w, dt), FrameGold);
                Fill(new Rect(x, y, w, Mathf.Max(1f, dt * 0.3f)), hi); Fill(new Rect(x, y + dt, w, sh), lo);
            }
            void VDivider(float x, float y, float h)
            {
                Fill(new Rect(x - sh, y, sh, h), lo); Fill(new Rect(x, y, dt, h), FrameGold);
                Fill(new Rect(x, y, Mathf.Max(1f, dt * 0.3f), h), hi); Fill(new Rect(x + dt, y, sh, h), lo);
            }
            float rowY = panel.y + pp, rowH = TopRowH, expGap = mm * 0.35f;
            var lvBox = new Rect(panel.x + pp, rowY, W * 0.17f, rowH + expGap + ExpRowH);
            string lvText = ch.Level >= Progression.LevelCap(ch) ? $"LV{ch.Level}★" : $"LV{ch.Level}";
            NumberFont.DrawIn(lvBox, lvText, Color.white, TextAnchor.MiddleCenter, 0.9f, HudPx + 1f);
            VDivider(lvBox.xMax + pp * 0.5f, panel.y, lvBox.height + pp * 1.5f);
            float barX = lvBox.xMax + pp * 1.5f;
            float barW = (panel.xMax - pp - barX - pp) * 0.5f;
            int maxHp = st != null ? st.MaxHp : 0, maxMp = st != null ? st.MaxMp : 0;
            var hpBack = new Rect(barX, rowY, barW, rowH);
            OrnateBar(hpBack, 1f, new Color(0.15f, 0.04f, 0.05f, 1f), new Color(0.8f, 0.1f, 0.12f, 1f), FrameGold, false);
            NumberFont.DrawIn(hpBack, $"HP {maxHp}", Color.white, TextAnchor.MiddleCenter, 0.9f, HudPx);
            var mpBack = new Rect(hpBack.xMax + pp, rowY, panel.xMax - pp - (hpBack.xMax + pp), rowH);
            OrnateBar(mpBack, 1f, new Color(0.05f, 0.08f, 0.18f, 1f), new Color(0.2f, 0.35f, 0.75f, 1f), FrameGold, false);
            NumberFont.DrawIn(mpBack, $"MP {maxMp}", Color.white, TextAnchor.MiddleCenter, 0.9f, HudPx);
            var expBack = new Rect(barX, rowY + rowH + expGap, panel.xMax - pp - barX, ExpRowH);
            Fill(expBack, new Color(0.02f, 0.05f, 0.05f, 1f));
            float expRatio = ch.Level >= Progression.LevelCap(ch) ? 1f : Mathf.Clamp01((float)ch.Exp / Mathf.Max(1, Progression.ExpToNext(ch, ch.Level)));
            Fill(new Rect(expBack.x, expBack.y, expBack.width * expRatio, expBack.height), new Color(0.2f, 0.9f, 0.72f, 1f));
            Fill(new Rect(expBack.x, expBack.y, expBack.width * expRatio, expBack.height * 0.4f), new Color(0.85f, 1f, 0.95f, 0.45f));
            for (int k = 1; k < 10; k++) Fill(new Rect(expBack.x + expBack.width * k / 10f, expBack.y, 1f, expBack.height), new Color(0f, 0f, 0f, 0.35f));
            Frame(expBack, new Color(0f, 0f, 0f, 0.6f), 1f);

            float divY = lvBox.yMax + pp * 0.5f;
            HDivider(panel.x, divY, panel.width);
            var txt = new GUIStyle(small) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            // 職業（左）と最高到達（右）
            var row1 = new Rect(panel.x + pp, divY + dt * 1.5f + pp * 0.2f, panel.width - pp * 2f, infoRowH);
            txt.normal.textColor = new Color(1f, 0.9f, 0.6f);
            var adv = AdvancedClasses.Of(ClassInfo.Current, ch);
            FitLabel(new Rect(row1.x, row1.y, row1.width * 0.5f, row1.height), adv != null ? adv.DisplayName : ClassInfo.Get(ClassInfo.Current).DisplayName, txt);
            int best = ch.Ascended ? ch.AscentBest : ch.BestFloor;
            string bestText = best > 0 ? GameConfig.FloorName(best, ch.Ascended) : "-";
            var rt = new GUIStyle(txt) { alignment = TextAnchor.MiddleRight };
            rt.normal.textColor = Color.white;
            FitLabel(new Rect(row1.x + row1.width * 0.5f, row1.y, row1.width * 0.5f, row1.height), Loc.T($"Best {bestText}", $"最高到達 {bestText}"), rt);
            // 火種と欠片（アイコンと数字）
            var row2 = new Rect(row1.x, row1.yMax, row1.width, infoRowH);
            float ic = infoRowH * 0.85f;
            DrawSprite(new Rect(row2.x, row2.y + (infoRowH - ic) * 0.5f, ic, ic), PixelArt.Ember);
            NumberFont.DrawIn(new Rect(row2.x + ic * 1.2f, row2.y, row2.width * 0.5f - ic * 1.4f, infoRowH), ch.Embers.ToString(), new Color(1f, 0.82f, 0.45f), TextAnchor.MiddleLeft, 0.9f, HudPx);
            bool shards = ch.AbyssShards > 0 || GameConfig.CanEnterAbyss(ch);
            var oldC = GUI.color;
            if (!shards) GUI.color = new Color(1f, 1f, 1f, 0.3f);
            DrawSprite(new Rect(row2.x + row2.width * 0.5f, row2.y + (infoRowH - ic) * 0.5f, ic, ic), PixelArt.ShardIcon);
            GUI.color = oldC;
            NumberFont.DrawIn(new Rect(row2.x + row2.width * 0.5f + ic * 1.2f, row2.y, row2.width * 0.5f - ic * 1.4f, infoRowH), shards ? ch.AbyssShards.ToString() : "-", shards ? new Color(0.85f, 0.62f, 1f) : new Color(1f, 1f, 1f, 0.3f), TextAnchor.MiddleLeft, 0.9f, HudPx);
            OrnateFrame(panel, FrameGold, FrameT);
            float bottom = panel.yMax + mm;
            return bottom;
        }

        // ---- 祝いの演出（転職・3次職・転生。町の画面の上にも出る）：回る光の筋、大きな文字、きらめき ----
        float celebTime = -1f;
        const float CelebLife = 2.6f;
        string celebTitle, celebSub;
        Color celebColor = Color.white;

        public void Celebrate(string title, string sub, Color color)
        {
            celebTitle = title;
            celebSub = sub;
            celebColor = color;
            celebTime = 0f;
        }

        void DrawCelebration()
        {
            if (celebTime < 0f) return;
            if (Event.current.type == EventType.Repaint) celebTime += Time.unscaledDeltaTime;
            float t = celebTime;
            if (t >= CelebLife) { celebTime = -1f; return; }
            float W = Screen.width, H = Screen.height;
            float a = Mathf.Clamp01(t / 0.25f) * Mathf.Clamp01((CelebLife - t) / 0.6f);
            var center = new Vector2(W * 0.5f, H * 0.42f);
            // 暗い下地と、中心から回りながら広がる光の筋
            Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0.02f, 0.45f * a));
            var oldM = GUI.matrix;
            int rays = 16;
            float len = Mathf.Max(W, H) * (0.4f + 0.4f * Mathf.Clamp01(t / 0.8f));
            for (int i = 0; i < rays; i++)
            {
                float ang = i * 360f / rays + t * 25f;
                GUIUtility.RotateAroundPivot(ang, center);
                float wdt = W * (i % 2 == 0 ? 0.035f : 0.018f);
                Fill(new Rect(center.x, center.y - wdt * 0.5f, len, wdt), new Color(celebColor.r, celebColor.g, celebColor.b, 0.22f * a));
                GUI.matrix = oldM;
            }
            // 中心の光
            var oldC = GUI.color;
            GUI.color = new Color(celebColor.r, celebColor.g, celebColor.b, 0.7f * a);
            float gr = W * (0.35f + 0.08f * Mathf.Sin(t * 6f));
            GUI.DrawTexture(new Rect(center.x - gr, center.y - gr, gr * 2f, gr * 2f), TitleArt.SoftGlow);
            GUI.color = oldC;
            // きらめき（ばらばらに瞬く点）
            for (int i = 0; i < 40; i++)
            {
                float px = Hash01(i * 13 + 5) * W, py = Hash01(i * 7 + 11) * H;
                float tw = Mathf.Abs(Mathf.Sin(t * (3f + Hash01(i) * 4f) + i));
                float s = W * 0.008f * (0.5f + tw);
                Fill(new Rect(px, py, s, s), new Color(1f, 1f, 1f, 0.8f * a * tw));
            }
            // 大きな文字（ぽんと大きく出て、少し縮んで落ち着く）
            float pop = 1f + 0.25f * Mathf.Exp(-t * 8f);
            var ts = new GUIStyle(centered) { fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(label.fontSize * 2.4f * pop) };
            ts.normal.textColor = new Color(1f, 1f, 1f, a);
            OutlinedLabel(new Rect(0, center.y - H * 0.06f, W, H * 0.08f), celebTitle, ts, new Color(celebColor.r * 0.4f, celebColor.g * 0.4f, celebColor.b * 0.4f, a));
            if (!string.IsNullOrEmpty(celebSub))
            {
                var ss = new GUIStyle(centered) { fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(label.fontSize * 1.3f) };
                ss.normal.textColor = new Color(celebColor.r, celebColor.g, celebColor.b, a);
                OutlinedLabel(new Rect(0, center.y + H * 0.03f, W, H * 0.05f), celebSub, ss, new Color(0f, 0f, 0f, 0.8f * a));
            }
        }

        // スローの間、画面の縁がその品の色でほのかに光る（何が起きたか分かるように）
        float slowGlowTime = -1f, slowGlowLife = 1f;
        Color slowGlowColor = Color.white;

        public void SetSlowMoGlow(Color color, float life)
        {
            slowGlowColor = color;
            slowGlowLife = Mathf.Max(0.1f, life);
            slowGlowTime = 0f;
        }

        void DrawSlowMoGlow()
        {
            if (slowGlowTime < 0f) return;
            if (Event.current.type == EventType.Repaint) slowGlowTime += Time.unscaledDeltaTime;
            float t = slowGlowTime / slowGlowLife;
            if (t >= 1f) { slowGlowTime = -1f; return; }
            float a = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * 0.55f; // ふわっと光って消える
            float W = Screen.width, H = Screen.height;
            const int bands = 6;
            float bw = W * 0.025f, bh = H * 0.015f;
            for (int i = 0; i < bands; i++)
            {
                float k = a * (1f - i / (float)bands);
                var c = new Color(slowGlowColor.r, slowGlowColor.g, slowGlowColor.b, k * 0.6f);
                Fill(new Rect(i * bw, 0, bw, H), c);
                Fill(new Rect(W - (i + 1) * bw, 0, bw, H), c);
                Fill(new Rect(0, i * bh, W, bh), c);
                Fill(new Rect(0, H - (i + 1) * bh, W, bh), c);
            }
        }

        void DrawFlash()
        {
            if (flashTime < 0f) return;
            if (Event.current.type == EventType.Repaint) flashTime += Time.unscaledDeltaTime;
            float a = 1f - flashTime / flashLife;
            if (a <= 0f) { flashTime = -1f; return; }
            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(flashColor.r, flashColor.g, flashColor.b, flashColor.a * a * a));
        }

        // ---------------- エンディング（1回だけ。見たあとは設定の「クレジット」から見返せる） ----------------
        float endingTime = -1f;
        // 流れ：白い閃光が引く → 深淵の塔 → ふつうの塔 → 夜明けの塔（世界が晴れていく）と言葉 → エンドロール → ロゴとお礼と記録
        const float RollStart = 12f, RollLength = 26f;
        const float EndingTapReady = RollStart + RollLength + 6f;

        /// <summary>エンディングを始める。creditsOnly なら、夜明けの塔のエンドロールから（設定の「クレジット」）。</summary>
        public void StartEnding(bool creditsOnly = false)
        {
            endingCreditsOnly = creditsOnly;
            endingTime = creditsOnly ? RollStart - 1.5f : 0f;
        }

        bool endingCreditsOnly; // 設定の「クレジット」から：3行の言葉は出さず、暗転から夜明けの塔とエンドロールへ

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
            foreach (BossKind k in System.Enum.GetValues(typeof(BossKind))) if (k <= BossKind.AbyssKing) l.Add((BossInfo.Name(k), 1)); // クリア後のボスは、エンドロールには出さない
            Head(Loc.T("- Those Who Carried the Light -", "― 灯りを運んだ者たち ―"));
            for (int c = 0; c < ClassInfo.PlayableCount; c++)
            {
                var type = (ClassType)c;
                l.Add((ClassInfo.Get(type).DisplayName, 1));
                for (int a = 0; a < 2; a++) l.Add(($"{AdvancedClasses.Get(type, a).DisplayName}  /  {ThirdClasses.Get(type, a).DisplayName}", 1));
            }
            l.Add((ClassInfo.Get(ClassType.Vividbearer).DisplayName, 1)); // 特別な職
            Head(Loc.T("- Guide -", "― 案内役 ―"));
            l.Add(("？？？", 1));
            Head(Loc.T("- Made by -", "― 制作 ―"));
            l.Add(("TOMOSHIBI WORKS", 1));
            Head(Loc.T("- Development Support -", "― 開発協力 ―"));
            l.Add(("Claude (Anthropic)", 1));
            Head(Loc.T("- Font -", "― フォント ―"));
            l.Add(("M PLUS 1p", 1));
            l.Add(("The M+ Project Authors  (SIL Open Font License)", 1));
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
            if (!endingCreditsOnly && t < 1.5f) Fill(new Rect(0, 0, W, H), new Color(1f, 1f, 1f, 1f - t / 1.5f));
            // クレジットから見るときは、黒から明けていく
            if (endingCreditsOnly) Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 1f - Mathf.Clamp01((t - (RollStart - 1.5f)) / 1.5f)));
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
            if (!endingCreditsOnly)
            {
                Line(1.5f, RollStart, H * 0.2f, Loc.T("The light reached deeper than the bottom of the tower.", "灯りは、塔のいちばん底よりも、さらに深くへ届いた。"), soft);
                Line(4.5f, RollStart, H * 0.26f, Loc.T("The white shadow waiting in the dark quietly came undone.", "闇の底で待っていた白い影は、静かにほどけていった。"), soft);
                Line(7.5f, RollStart, H * 0.32f, Loc.T("Colors that were fading glow vivid once more.", "消えかけた色が、もう一度あざやかに灯る。"), new Color(1f, 0.9f, 0.6f));
            }

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

            // ---- 上の情報カード（町の画面と同じ作り） ----
            float lineH = small.fontSize * 1.35f;
            var card = TopCard;
            float pp = PanelPad;
            // カードの文字は通常のフォント（欄に収まるよう縮む）。縁取りで読みやすく
            var cardText = new GUIStyle(small) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            var cardOutline = new Color(0f, 0f, 0f, 0.85f);
            void CardLabel(Rect r, string t, Color c, TextAnchor a)
            {
                var st0 = new GUIStyle(cardText) { alignment = a };
                st0.normal.textColor = c;
                OutlinedLabel(r, t, st0, cardOutline);
            }
            Fill(card, new Color(0.08f, 0.075f, 0.11f, 1f));
            float lvD = TopLvD;
            var lvR = new Rect(card.center.x - lvD * 0.5f, card.center.y - lvD * 0.5f, lvD, lvD);
            // 左：HP・MP のバー、その下に層と残りの敵
            float lx = card.x + pp * 1.5f, lw = lvR.x - pp * 1.5f - lx;
            var hpBack = new Rect(lx, card.y + card.height * 0.1f, lw, card.height * 0.25f);
            float ratio = player.MaxHp > 0 ? Mathf.Clamp01((float)player.Hp / player.MaxHp) : 0f;
            OrnateBar(hpBack, ratio, new Color(0.15f, 0.04f, 0.05f, 1f), new Color(0.8f, 0.1f, 0.12f, 1f), FrameGold, false);
            CardLabel(hpBack, $"HP {player.Hp}/{player.MaxHp}", Color.white, TextAnchor.MiddleCenter);
            var mpBack = new Rect(lx, card.y + card.height * 0.39f, lw, card.height * 0.25f);
            float mpRatio = player.MaxMp > 0 ? Mathf.Clamp01((float)player.Mp / player.MaxMp) : 0f;
            OrnateBar(mpBack, mpRatio, new Color(0.05f, 0.08f, 0.18f, 1f), new Color(0.2f, 0.35f, 0.75f, 1f), FrameGold, false);
            CardLabel(mpBack, $"MP {player.Mp}/{player.MaxMp}", Color.white, TextAnchor.MiddleCenter);
            var floorRow = new Rect(lx, card.y + card.height * 0.69f, lw, card.height * 0.24f);
            string floorText = GameConfig.FloorNameEn(World.Floors.Floor) + (World.Floors.IsBossFloor ? " BOSS" : ""); // HUD では「71F」と短く
            CardLabel(new Rect(floorRow.x, floorRow.y, floorRow.width * 0.48f, floorRow.height), floorText, Color.white, TextAnchor.MiddleLeft);
            if (!World.Floors.IsBossFloor && World.Room.StairsArea != null)
            {
                // 普通の層：階段の封印までの残りの敵（階段の部屋）。開いたら「OPEN」
                int left = World.Enemies.RemainingIn(World.Room.StairsArea);
                bool open = World.Floors.StairsOpen;
                string t = open ? "OPEN" : $"ENEMY {left}";
                var col = open ? new Color(0.55f, 0.8f, 1f) : new Color(1f, 0.82f, 0.4f);
                CardLabel(new Rect(floorRow.x + floorRow.width * 0.48f, floorRow.y, floorRow.width * 0.52f, floorRow.height), t, col, TextAnchor.MiddleRight);
            }
            // 右：火種と欠片（町の画面と同じ）、その下に回避の待ち時間
            {
                float rx = lvR.xMax + pp * 1.5f, rw = card.xMax - pp * 1.5f - rx;
                bool shards = ch != null && (ch.AbyssShards > 0 || GameConfig.CanEnterAbyss(ch));
                int embers = (ch != null ? ch.Embers : 0) + inv.Embers; // 持ち帰る前の分も足して見せる
                for (int k = 0; k < 2; k++)
                {
                    var pr = new Rect(rx, card.y + card.height * (0.1f + k * 0.37f), rw, card.height * 0.31f);
                    Fill(pr, new Color(0f, 0f, 0f, 0.45f));
                    OrnateFrame(pr, new Color(0.85f, 0.72f, 0.42f, 0.7f), Mathf.Max(1f, FrameT * 0.5f), true, false);
                    float ic = pr.height * 0.86f;
                    var oldC = GUI.color;
                    if (k == 1 && !shards) GUI.color = new Color(1f, 1f, 1f, 0.3f);
                    DrawSprite(new Rect(pr.x + pr.height * 0.1f, pr.y + (pr.height - ic) * 0.5f, ic, ic), k == 0 ? PixelArt.Ember : PixelArt.ShardIcon);
                    GUI.color = oldC;
                    string v = k == 0 ? embers.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) : (shards ? ch.AbyssShards.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) : "-");
                    var vc = k == 0 ? new Color(1f, 0.82f, 0.45f) : (shards ? new Color(0.85f, 0.62f, 1f) : new Color(1f, 1f, 1f, 0.3f));
                    CardLabel(new Rect(pr.x + ic * 1.3f, pr.y, pr.width - ic * 1.3f - pr.height * 0.25f, pr.height), v, vc, TextAnchor.MiddleRight);
                }
                var dodge = new Rect(rx, card.y + card.height * 0.84f, rw, card.height * 0.08f);
                Fill(dodge, new Color(0f, 0f, 0f, 0.6f));
                Fill(new Rect(dodge.x, dodge.y, dodge.width * player.DodgeReady, dodge.height), new Color(0.75f, 0.85f, 1f, 0.9f));
                Frame(dodge, new Color(0f, 0f, 0f, 0.6f), 1f);
            }
            // 真ん中：Lv の丸と経験値の輪（押すと能力値の画面。ポイントが余っていれば金色に明滅）
            TownScreen.DrawDisk(new Rect(lvR.x - FrameT * 2f, lvR.y - FrameT * 2f, lvD + FrameT * 4f, lvD + FrameT * 4f), FrameGold);
            TownScreen.DrawDisk(lvR, new Color(0.05f, 0.045f, 0.09f, 1f));
            float lvGlow = LevelGlow;
            if (lvGlow > 0f) TownScreen.DrawDisk(lvR, new Color(1f, 0.8f, 0.3f, 0.45f * lvGlow)); // レベルアップの直後は光る
            if (ch != null && ch.Points > 0) TownScreen.DrawDisk(lvR, new Color(1f, 0.78f, 0.25f, 0.18f + 0.14f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f))));
            if (!World.Paused && !player.IsDead && !World.ChoosingClass)
                Button(lvR, null, new Color(0f, 0f, 0f, 0f), centered, () => World.OpenPanel(false, true, false));
            if (ch != null)
            {
                float expRatio = ch.Level >= Progression.LevelCap(ch) ? 1f : Mathf.Clamp01((float)ch.Exp / Mathf.Max(1, Progression.ExpToNext(ch, ch.Level)));
                float ringR = lvD * 0.42f, dot = Mathf.Max(2f, lvD * 0.06f);
                const int seg = 90;
                for (int i = 0; i < seg; i++)
                {
                    float ang = (-90f + 360f * i / seg) * Mathf.Deg2Rad;
                    var c = i < seg * expRatio ? new Color(0.25f, 0.92f, 0.75f, 1f) : new Color(1f, 1f, 1f, 0.1f);
                    Fill(new Rect(lvR.center.x + Mathf.Cos(ang) * ringR - dot * 0.5f, lvR.center.y + Mathf.Sin(ang) * ringR - dot * 0.5f, dot, dot), c);
                }
            }
            int level = ch != null ? ch.Level : 1;
            var lvSmall = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter };
            lvSmall.normal.textColor = new Color(0.75f, 0.85f, 1f);
            GUI.Label(new Rect(lvR.x, lvR.y + lvD * 0.16f, lvD, lvD * 0.2f), ch != null && ch.Level >= Progression.LevelCap(ch) ? "Lv★" : "Lv", lvSmall);
            var lvBig = new GUIStyle(cardText) { fontSize = Mathf.RoundToInt(label.fontSize * 1.6f) };
            lvBig.normal.textColor = lvGlow > 0f ? Color.Lerp(Color.white, new Color(1f, 0.9f, 0.5f), lvGlow) : Color.white;
            OutlinedLabel(new Rect(lvR.x + lvD * 0.12f, lvR.y + lvD * 0.34f, lvD * 0.76f, lvD * 0.38f), level.ToString(), lvBig, cardOutline);
            OrnateFrame(card, FrameGold, FrameT, true, false); // まとめて1枚の枠
            float y = topBottom + mm;

            // FPS（ダンジョンの左下。右上はミニマップ、右下は攻撃ボタンがあるので）
            if (ShowFps)
                NumberFont.DrawIn(new Rect(mm * 2f, ViewTop + mm, W * 0.4f, lineH), $"FPS:{fps:0}", new Color(1f, 1f, 1f, 0.75f), TextAnchor.MiddleLeft);

            // 下の帯の2つの区画：メッセージウィンドウ（上）とショートカット（下）。それぞれ背景と枠で仕切る
            var frameColor = FrameGold;
            float frameT = Mathf.Max(2f, Mathf.Round(H * 0.0012f));
            var msgR = MessageWindow;
            // メッセージは枠なし（縁取りした文字だけを、ダンジョンの上に）
            var scR = ShortcutPanel; // ショートカットの区画は枠なし（帯の上に、ボタンだけを置く）
            {
                // 透かしのロゴ（ボタンの後ろにうっすら）
                var logo = TitleArt.Logo;
                float lh = scR.height * 0.55f, lw2 = logo.width * lh / logo.height;
                if (lw2 > scR.width * 0.9f) { lw2 = scR.width * 0.9f; lh = logo.height * lw2 / logo.width; }
                var oldL = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 0.22f);
                GUI.DrawTexture(new Rect(scR.center.x - lw2 * 0.5f, scR.center.y - lh * 0.5f, lw2, lh), logo);
                GUI.color = oldL;
            }

            DrawBossBar();
            DrawGoldenMarker();
            DrawMinimap();
            // メッセージ：地図より手前に（重なってよい）。直近の行を残す（新しいものほど下）。来たばかりの行は明るく、古い行は少し暗い
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
                    // 収まらない文は縮めて1行に収める（切らない）
                    int row = k - start;
                    float textW = msgR.width - mm * 2f;
                    OutlinedLabel(new Rect(msgR.x + mm, msgR.y + mm * 0.8f + row * logH, textW, logH), "+ " + t.Text, style, new Color(0f, 0f, 0f, 0.9f)); // 透けた床の上でも読めるよう縁取り
                }
            }

            DrawBanner();

            if (World.Paused || player.IsDead || World.ChoosingClass) return;

            // 右上は MENU だけ（ポイントが余っている・鞄が満杯なら「!」）
            int points = ch != null ? ch.Points : 0;
            bool notice = points > 0 || inv.BagFull;
            // MENU：いつも同じ暗いボタン。お知らせ（ポイントが余っている・鞄が満杯）があるときは、右上に脈打つ赤い丸の「!」
            var menuR = MenuGui; // ミニマップの下の、丸い歯車のボタン
            TownScreen.DrawDisk(new Rect(menuR.x - FrameT * 2f, menuR.y - FrameT * 2f, menuR.width + FrameT * 4f, menuR.height + FrameT * 4f), FrameGold);
            TownScreen.DrawDisk(menuR, new Color(0.06f, 0.055f, 0.1f, 0.95f));
            Button(menuR, null, new Color(0f, 0f, 0f, 0f), centered, () => World.OpenMenu());
            DrawSprite(new Rect(menuR.x + menuR.width * 0.16f, menuR.y + menuR.height * 0.16f, menuR.width * 0.68f, menuR.height * 0.68f), PixelArt.GearIcon);
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
                NumberFont.DrawIn(badge, "!", Color.white, TextAnchor.MiddleCenter, 0.6f);
            }

            DrawSkillButtons(ch);
            DrawPotions(ch);
            DrawQuickButtons(ch, points > 0, inv.BagFull);
            if (!Application.isMobilePlatform)
            {
                // 操作の案内（PC・エディタ向け）：鞄・能力値・スキルのボタンの下の空きに
                var q0 = QuickRect(0); var q2 = QuickRect(2);
                float qx = Mathf.Min(q0.x, q2.x), qw = Mathf.Max(q0.xMax, q2.xMax) - qx;
                var hint = new GUIStyle(small) { alignment = TextAnchor.MiddleLeft }; // 左寄せ
                hint.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
                float topY = Screen.height - (SkillAutoRect(0).yMax);              // スキルのオートの帯の上端にそろえる
                GUI.Label(new Rect(qx, topY, qw, q0.y - topY), Loc.T("H: Controls", "H：操作"), hint);
            }

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

            // 操作の一覧（PC・エディタ向け。H で開閉）。「H：操作」の案内は、いちばん上の段のロゴの右に出す
            if (!Application.isMobilePlatform && ShowHelp) DrawHelp();
        }

        /// <summary>操作の一覧（H で開閉）。</summary>
        public bool ShowHelp;

        /// <summary>操作：左手（メニューの設定）。下の段のボタンと手動攻撃を左に寄せる。</summary>
        public static bool LeftHanded
        {
            get => Profile.LeftHandedSetting;
            set => Profile.LeftHandedSetting = value;
        }

        /// <summary>FPS を出すか（設定。保存する）。</summary>
        public bool ShowFps
        {
            get => Profile.ShowFpsSetting;
            set => Profile.ShowFpsSetting = value;
        }

        static readonly string[] helpLines =
        {
            "Move ........ WASD / arrows (phone: drag)",
            "Dodge ....... Space (phone: flick, also while moving)",
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
            "回避 ........ Space（スマホ：はじく。移動中も）",
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
            var below = new Rect(0, 0, Screen.width, MiniTop - Screen.height * 0.01f); // 上の帯のすぐ下から（メッセージと重なってよい）
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
            if (World.Floors.StairsOpen) Dot(room.StairsPos, dot * 1.3f, new Color(0.45f, 0.75f, 1f)); // 通れる階段は青
            else if (!World.Floors.IsBossFloor) Dot(room.StairsPos, dot * 1.3f, new Color(1f, 0.8f, 0.3f)); // 封印中は黄色（開くと青）
            if (room.HasAltar && !World.AltarUsed) Dot(room.AltarPos, dot, new Color(1f, 0.5f, 0.2f));
            if (room.HasSpring && !World.SpringUsed) Dot(room.SpringPos, dot, new Color(0.5f, 1f, 0.7f)); // 癒やしの泉は緑
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
            float top = ViewTop + size * 1.5f, bottom = BottomBandTop - size * 1.5f;
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
            var center = new Vector2(Screen.width * 0.5f, (ViewTop + BottomBandTop) * 0.5f - ViewRaise * Screen.height); // 自分のいる所（ダンジョンを少し上げた分）
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
            float mapBottom = MenuGui.yMax; // 地図と、その下の歯車の下。右下の薬・攻撃ボタンと重ならないように
            var r = new Rect((Screen.width - w) * 0.5f, mapBottom + small.fontSize * 2f, w, h);
            Fill(r, new Color(0f, 0f, 0f, 0.7f));
            Fill(new Rect(r.x, r.y, r.width * Mathf.Clamp01(boss.Hp / boss.MaxHp), r.height), new Color(0.9f, 0.2f, 0.25f, 0.95f));
            GUI.Label(new Rect(r.x, r.y - small.fontSize * 1.5f, r.width, small.fontSize * 1.4f), $"{BossInfo.NameAt(boss.BossKind, World.Floors.Floor)}  -  " + GameConfig.FloorName(World.Floors.Floor), small);
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
            DrawDamageStacks(cam);
            foreach (var n in numbers)
            {
                float t = n.T / n.Life;
                Vector3 sp = cam.WorldToScreenPoint(new Vector3(n.World.x, n.World.y + t * (n.Wide ? 0.35f : 0.6f), 0f));
                var style = n.Big ? big : number;
                var c = n.Color;
                c.a = 1f - t * t;
                if (NumberFont.CanDraw(n.Text))
                {
                    // 「MISS」などもドット文字で（ダメージの数字と同じ大きさ。出た瞬間に少しふくらむ）
                    float px = Mathf.Max(2f, Mathf.Round(number.fontSize / 8.5f)) * (n.Big ? 1.4f : 1f);
                    if (n.Big) px *= 1f + 0.35f * Mathf.Clamp01(1f - t * 6f);
                    NumberFont.Draw(new Vector2(sp.x, Screen.height - sp.y), n.Text, px, c);
                    continue;
                }
                style.normal.textColor = c;
                float w = style.fontSize * (n.Wide ? 12f : 4f);
                float h = style.fontSize * 1.5f;
                GUI.Label(new Rect(sp.x - w * 0.5f, Screen.height - sp.y - h * 0.5f, w, h), n.Text, style);
            }
            number.normal.textColor = Color.white;
        }

        void DrawGameOver()
        {
            // キャラクターが崩れ終わるころに、ゆっくり暗くなって文字が出る（文字はタイトルの FADE と同じ石の灰色）
            float k = Mathf.Clamp01((World.DeathFade - 1f) / 0.6f);
            if (World.DeathFade < 0f) k = 1f;
            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.6f * k));
            if (k <= 0f) return;
            float h = Screen.height;
            var title = new GUIStyle(centered) { fontSize = label.fontSize * 3, fontStyle = FontStyle.Bold };
            title.normal.textColor = new Color(0.67f, 0.65f, 0.71f, k);
            Hud.FitLabel(new Rect(0, h * 0.38f, Screen.width, h * 0.08f), Loc.T("GAME OVER", "力尽きた"), title);
            var sub = new GUIStyle(centered);
            sub.normal.textColor = new Color(1f, 1f, 1f, k);
            GUI.Label(new Rect(0, h * 0.47f, Screen.width, h * 0.05f),
                GameConfig.FloorName(World.Floors.Floor) + Loc.T($"   KILL {World.Enemies.Kills}   EMBERS {World.Inventory.Embers}", $"   撃破 {World.Enemies.Kills}   火種 {World.Inventory.Embers}"), sub);
            if (World.DeathFade < 0f || World.DeathFade >= GameWorld.DeathFadeReady)
                GUI.Label(new Rect(0, h * 0.53f, Screen.width, h * 0.05f), Loc.T("Tap / Enter to continue", "タップか Enter で続ける"), sub);
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
            floorTabAbyss = all.Count > 0 && GameConfig.IsBeyond(all[all.Count - 1]);
        }

        /// <summary>いま見ているタブの層（塔か深淵か）。数字キーもこの並びで選ぶ。</summary>
        public List<int> VisibleFloors(CharacterData ch)
        {
            var list = new List<int>();
            foreach (int f in GameWorld.Checkpoints(ch)) if (GameConfig.IsBeyond(f) == floorTabAbyss) list.Add(f);
            return list;
        }

        /// <summary>町から開く画面の背景（町の絵を暗くして敷く）。描いたあと false を返し、続けて画面そのものを描かせる。</summary>
        bool DrawTownPanelBackdrop()
        {
            float W = Screen.width, H = Screen.height;
            Fill(new Rect(0, 0, W, H), Color.black);
            TownScreen.DrawBackdrop(W, H);
            Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0.02f, 0.7f));
            return false;
        }

        void DrawFloorSelect(ClassType type)
        {
            float W = Screen.width;
            float H = Screen.height;
            // 後ろは町の絵（ダンジョンやキャラクターが透けて見えないように）を、文字が読めるよう暗くして敷く
            Fill(new Rect(0, 0, W, H), Color.black);
            TownScreen.DrawBackdrop(W, H);
            Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0.02f, 0.78f));
            var ch = Profile.Get(type);
            var spec = ClassInfo.Get(type);

            var title = new GUIStyle(centered) { fontSize = label.fontSize * 2, fontStyle = FontStyle.Bold };
            Hud.FitLabel(new Rect(0, H * 0.12f, W, H * 0.06f), Loc.T("Choose a starting floor", "始める層を選ぶ"), title);
            GUI.Label(new Rect(0, H * 0.19f, W, H * 0.04f), $"{AdvancedClasses.NameOf(type)}   Lv {ch.Level}   " + (ch.Ascended ? Loc.T("Top ", "最上 ") + GameConfig.FloorName(Mathf.Max(1, ch.AscentBest), true) : Loc.T("Best ", "最深 ") + GameConfig.FloorName(ch.BestFloor, false)), centered);

            var all = GameWorld.Checkpoints(ch);
            bool hasAbyss = all.Exists(GameConfig.IsBeyond); // 深淵か天界
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
                Button(new Rect(pad + tw + gap, y, tw, th), GameConfig.Ascent ? Loc.T("HEAVEN", "天界") : Loc.T("ABYSS", "深淵"), floorTabAbyss ? (GameConfig.Ascent ? new Color(0.75f, 0.62f, 0.25f, 0.9f) : new Color(0.55f, 0.1f, 0.42f, 0.9f)) : new Color(1f, 1f, 1f, 0.06f), centered, () => floorTabAbyss = true);
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
                Button(r, key + GameConfig.FloorName(floor), GameConfig.IsHeaven(floor) ? new Color(0.7f, 0.58f, 0.22f, latest ? 0.95f : 0.55f) : GameConfig.IsAbyss(floor) ? new Color(0.45f, 0.08f, 0.35f, latest ? 0.95f : 0.55f) : latest ? new Color(0.5f, 0.4f, 0.1f, 0.7f) : new Color(1f, 1f, 1f, 0.12f), centered,
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
            // アビスを倒してエンディングを見たら、クリア後の世界（夜明けの塔）。その前に深淵へ行けるようになったら、深淵版の背景
            bool cleared = Profile.AnyEndingSeen;
            bool abyss = !cleared && Profile.AbyssUnlocked;
            var bgTex = cleared ? TitleScene.DawnTexture : abyss ? TitleScene.AbyssTexture : TitleScene.Texture;
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
                Glow(wx, wy, ps * 9f, abyss ? new Color(1f, 0.2f, 0.4f, 0.4f * f) : cleared ? new Color(1f, 0.85f, 0.5f, 0.3f * f) : new Color(1f, 0.65f, 0.3f, 0.35f * f));
            }
            // 最深部の灯り（尖塔の先）
            float flick = 0.55f + 0.45f * Mathf.PerlinNoise(t * 2.2f, 0.3f);
            float bx = bg.x + TitleScene.TowerX * ps, by = bg.y + (TitleScene.SpireTip - 1) * ps;
            if (cleared)
            {
                // クリア後：尖塔の先に朝日のような金色の光。下からは夜明けの光が差し込む
                float rise = 0.85f + 0.15f * Mathf.Sin(t * 0.8f);
                Glow(W * 0.5f, H * 1.05f, W * 0.9f, new Color(1f, 0.9f, 0.65f, 0.3f * rise));
                Glow(bx, by, ps * 34f, new Color(1f, 0.85f, 0.55f, 0.45f * flick));
                Glow(bx, by, ps * 8f, new Color(1f, 0.97f, 0.88f, 0.95f * flick));
            }
            else if (abyss)
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
                Color dc = abyss ? new Color(1f, 0.35f, 0.55f, a) : new Color(0.85f, 0.88f, 0.95f, a); // 深淵では、下から立ちのぼる赤い火の粉
                if (cleared) { var rc = SkillVfx.Rainbow[i % SkillVfx.Rainbow.Length]; dc = new Color(rc.r, rc.g, rc.b, a * 1.2f); } // クリア後は、七色の光の粒
                Fill(new Rect(ex, ey, ps * 0.5f, ps * 0.5f), dc);
            }

            // 下側を暗くして文字を読みやすく
            for (int b = 0; b < 8; b++)
                Fill(new Rect(0, H * (0.82f + b * 0.0225f), W, H * 0.0225f + 1f), new Color(0f, 0f, 0f, 0.07f * (b + 1)));

            // ロゴ：後ろにぼかした影を敷き、VIVID のまわりの光、本体、FADE から崩れて舞い散る粒
            var logoTex = TitleArt.Logo;
            float sc = Mathf.Max(1f, Mathf.Floor(W * 0.95f / logoTex.width));
            var logoRect = new Rect((W - logoTex.width * sc) * 0.5f, H * 0.3f, logoTex.width * sc, logoTex.height * sc);
            var old = GUI.color;
            GUI.color = new Color(0.015f, 0.015f, 0.03f, cleared ? 0.6f : 0.85f); // 明るい背景でもロゴが読めるように
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
                if (cleared)
                {
                    // クリア後：崩れていた粒が、七色に光りながら文字へ戻ってくる（崩れるのを逆に）
                    float back = 1f - phase;
                    float bxo = back * W * 0.12f * (0.5f + Hash01(i * 7 + 2));
                    float byo = -(back * back * H * 0.1f + back * H * 0.01f);
                    float ba = phase < 0.08f ? phase / 0.08f : back;
                    var rc = SkillVfx.Rainbow[i % SkillVfx.Rainbow.Length];
                    Fill(new Rect(ox - bxo, oy + byo, sc, sc), new Color(rc.r, rc.g, rc.b, ba));
                    continue;
                }
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
            FitLabel(new Rect(W * 0.26f, ly - H * 0.017f, W * 0.48f, H * 0.035f), (cleared ? Loc.T("Into a world where the light returned", "灯りの戻った世界へ") : abyss ? Loc.T("Into the abyss no light can reach", "灯りの届かぬ深淵へ") : Loc.T("Into the tower where light fades", "灯りの消えゆく塔へ")), sub);

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
            var bgTex = Profile.AnyEndingSeen ? TitleScene.DawnTexture : Profile.AbyssUnlocked ? TitleScene.AbyssTexture : TitleScene.Texture; // クリア後は夜明けの塔
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

            if (confirmDeleteSlot >= 0 && newSlot < 0) DrawDeleteConfirm(); // いちばん上に重ねる
        }

        // ---- キャラクター選択（3つの枠） ----

        int newSlot = -1;          // 新しく作る枠（-1 は枠の一覧）
        int confirmDeleteSlot = -1; // 消す確認の窓を出している枠（-1 は出していない）

        /// <summary>
        /// キャラクターを消す確認の窓。画面全体を覆う当たり判定を先に置き、下の枠やボタンが押されないようにする。
        /// </summary>
        void DrawDeleteConfirm()
        {
            var ch = Profile.InSlot(confirmDeleteSlot);
            if (ch == null) { confirmDeleteSlot = -1; return; }
            float W = Screen.width, H = Screen.height;
            var all = new Rect(0, 0, W, H);
            Fill(all, new Color(0f, 0f, 0f, 0.6f));
            Button(all, null, new Color(0f, 0f, 0f, 0f), centered, () => { }); // 外を押しても何もしない
            float pw = W * 0.84f, ph = H * 0.24f;
            var panel = new Rect((W - pw) * 0.5f, (H - ph) * 0.5f, pw, ph);
            Fill(panel, new Color(0.05f, 0.04f, 0.08f, 0.97f));
            OrnateFrame(panel, FrameGold, FrameT);
            float inner = ph * 0.08f;
            float icon = ph * 0.38f;
            DrawSprite(new Rect(panel.x + inner, panel.y + inner, icon, icon), PixelArt.PlayerFor(ch));
            var title = new GUIStyle(label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, wordWrap = true };
            title.normal.textColor = new Color(1f, 0.6f, 0.6f);
            float tx = panel.x + inner * 2f + icon;
            GUI.Label(new Rect(tx, panel.y + inner, panel.xMax - inner - tx, icon * 0.55f),
                Loc.T($"Delete {AdvancedClasses.NameFor(ch)} (Lv {ch.Level})?", $"{AdvancedClasses.NameFor(ch)}（Lv {ch.Level}）を\n消しますか？"), title);
            var note = new GUIStyle(small) { alignment = TextAnchor.UpperLeft, wordWrap = true };
            note.normal.textColor = new Color(0.8f, 0.8f, 0.85f);
            GUI.Label(new Rect(tx, panel.y + inner + icon * 0.55f, panel.xMax - inner - tx, icon * 0.6f),
                Loc.T("This cannot be undone. The shared storage stays.", "元には戻せません。倉庫（全職業で共有）は残ります。"), note);
            float bh = ph * 0.26f, gap = W * 0.03f;
            float bw = (pw - inner * 2f - gap) * 0.5f;
            float by = panel.yMax - inner - bh;
            int slot = confirmDeleteSlot;
            void Yes() { confirmDeleteSlot = -1; Profile.DeleteSlot(slot); }
            void No() => confirmDeleteSlot = -1;
            var red = new GUIStyle(centered) { fontStyle = FontStyle.Bold };
            red.normal.textColor = new Color(1f, 0.85f, 0.85f);
            Button(new Rect(panel.x + inner, by, bw, bh), Loc.T("Yes", "はい"), new Color(0.6f, 0.1f, 0.12f, 0.95f), red, Yes);
            Button(new Rect(panel.x + inner + bw + gap, by, bw, bh), Loc.T("No", "いいえ"), new Color(1f, 1f, 1f, 0.1f), centered, No);
        }

        /// <summary>数字キー：枠の一覧なら枠を、職業の選択なら職業を選ぶ。</summary>
        public void PressSlotKey(int n)
        {
            if (confirmDeleteSlot >= 0) return; // 消す確認の窓が出ている間は選ばない
            if (newSlot >= 0)
            {
                if (n < ClassInfo.PlayableCount)
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
            if (confirmDeleteSlot >= 0) { confirmDeleteSlot = -1; return true; } // 消す確認の窓を閉じる
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
                    // 状態は2行に分ける（1行に詰めると字が小さくなりすぎる）。職業の名前は上の名前と重なるので出さない
                    var info = new GUIStyle(small) { alignment = TextAnchor.MiddleLeft };
                    float iy = r.y + inner + nameStyle.fontSize * 1.5f;
                    FitLabel(new Rect(tx, iy, r.xMax - inner - tx, lh), $"Lv {ch.Level}   " + (ch.Ascended ? Loc.T("Top ", "最上 ") + GameConfig.FloorName(Mathf.Max(1, ch.AscentBest), true) : Loc.T("Best ", "最深 ") + GameConfig.FloorName(ch.BestFloor, false)), info);
                    var sub = new GUIStyle(info);
                    sub.normal.textColor = new Color(0.75f, 0.75f, 0.8f);
                    FitLabel(new Rect(tx, iy + lh, r.xMax - inner - tx, lh),
                        Loc.T($"Embers {ch.Embers}", $"火種 {ch.Embers}") + (ch.Run.Active ? Loc.T("   (suspended: ", "   （中断中：") + GameConfig.FloorName(ch.Run.Floor, ch.Ascended) + Loc.T(")", "）") : ""), sub);

                    // 消す：右上の小さなごみ箱。押すと確認の窓が出る（消しても倉庫は残る）
                    float ds = r.height * 0.26f;
                    var del = new Rect(r.xMax - ds - inner * 0.5f, r.y + inner * 0.5f, ds, ds);
                    Button(del, null, new Color(1f, 1f, 1f, 0.08f), centered, () => confirmDeleteSlot = slot);
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
            int n = ClassInfo.PlayableCount; // 作るときに選べるのは基本職の3つ（彩灯者は転生でだけなれる）
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
                // 灯りの適性（職業ごとの、灯りとの関わり方）
                var apt = new GUIStyle(small) { fontStyle = FontStyle.Italic };
                apt.normal.textColor = new Color(1f, 0.85f, 0.55f);
                // 欄が狭いので最初の一文だけ（全文は能力値の画面の「職業」で読める）
                string aptText = spec.DisplayAptitude ?? "";
                int cut = Loc.Ja ? aptText.IndexOf('。') : aptText.IndexOf(". ");
                if (cut > 0) aptText = aptText.Substring(0, cut + 1);
                FitLabel(new Rect(tx, r.y + inner + nameStyle.fontSize * 1.4f + lh * 2.1f, tw, lh), aptText, apt);
                y += bh + gap;
            }
            Button(new Rect(pad, y, W - pad * 2f, H * 0.045f), Loc.T("BACK", "戻る"), new Color(1f, 1f, 1f, 0.08f), centered, () => newSlot = -1);
            return y + H * 0.045f + gap;
        }

        void EnsureStyles()
        {
            int size = Mathf.Max(12, Mathf.RoundToInt(Screen.height * 0.02f));
            var font = GameFont.Get();
            if (label != null && label.fontSize == size && label.font == font) return;

            label = new GUIStyle(GUI.skin.label) { fontSize = size, font = font };
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


        /// <summary>深淵の装備の枠：赤紫の二重の枠が、ゆっくり明滅する。</summary>
        public static void DrawAbyssFrame(Rect cell, Item item)
        {
            if (item != null && item.IsBossGear)
            {
                // ボスのセットの品：セットの色の太い枠が、ゆっくり明滅する
                float bt = Mathf.Max(2f, cell.width * 0.055f);
                float bp = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.8f);
                var bo = BossSets.ColorOf(item.BossSet);
                bo.a = 0.65f + 0.35f * bp;
                Fill(new Rect(cell.x, cell.y, cell.width, bt), bo);
                Fill(new Rect(cell.x, cell.yMax - bt, cell.width, bt), bo);
                Fill(new Rect(cell.x, cell.y, bt, cell.height), bo);
                Fill(new Rect(cell.xMax - bt, cell.y, bt, cell.height), bo);
                return;
            }
            if (item != null && item.Heaven)
            {
                // 天界の装備：白と金の二重の枠が、やわらかく明滅する
                float ht = Mathf.Max(2f, cell.width * 0.05f);
                float hp = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.4f);
                var ho = new Color(1f, 0.88f, 0.5f, 0.65f + 0.35f * hp);
                var hi = new Color(1f, 1f, 0.95f, 0.85f);
                void HFrame(Rect r, Color c, float w)
                {
                    Fill(new Rect(r.x, r.y, r.width, w), c);
                    Fill(new Rect(r.x, r.yMax - w, r.width, w), c);
                    Fill(new Rect(r.x, r.y, w, r.height), c);
                    Fill(new Rect(r.xMax - w, r.y, w, r.height), c);
                }
                HFrame(cell, ho, ht);
                HFrame(new Rect(cell.x + ht, cell.y + ht, cell.width - ht * 2f, cell.height - ht * 2f), hi, ht * 0.5f);
                return;
            }
            if (item == null || !item.Abyss) return;
            float t = Mathf.Max(2f, cell.width * 0.05f);
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
            var set = AbyssSets.SetOf(item); // セットごとの色（業火は紅蓮、裂け目は紫、星落としは水色、報いは赤紫）
            var outer = AbyssSets.ColorOf(set);
            outer.a = 0.6f + 0.4f * pulse;
            var inner = AbyssSets.DarkOf(set);
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

        /// <summary>
        /// 強化した品：アイコンの枠の四隅に飾り金具を付け、段が上がるほど豪華にする。左上の角に「+N」。
        /// +1〜4：金の小さな角金具／+5〜9：大きな角金具と鋲／+10〜14：火種の橙に光る角金具（ゆらめく）／+15：青白く脈打つ角金具。
        /// </summary>
        /// <summary>
        /// 品のマスの右下に、装備のレベルを小さなドット文字で出す。今のキャラクターのレベルが足りない品は赤。
        /// </summary>
        public static void DrawItemLevel(Rect cell, Item item)
        {
            if (item == null || item.IsEmptyMarker) return;
            var ch = Profile.Current;
            bool tooHigh = ch != null && item.ReqLevel > ch.Level;
            var c = tooHigh ? new Color(1f, 0.42f, 0.42f) : new Color(0.95f, 0.95f, 0.98f);
            // 左上に小さく（強化の +N は右上）。小さなマスでも読めるよう、うすい暗い帯を敷く
            string s = item.Level.ToString();
            float px = Mathf.Max(2f, Mathf.Floor(cell.height * 0.2f / NumberFont.Height));
            float w = NumberFont.Width(s, px), h = NumberFont.Height * px;
            float pad = Mathf.Max(1f, Mathf.Round(px * 0.5f));
            var band = new Rect(cell.x + cell.width * 0.04f, cell.y + cell.height * 0.04f, w + pad * 2f, h + pad * 2f);
            Fill(band, new Color(0.03f, 0.02f, 0.06f, 0.6f));
            NumberFont.Draw(band.center + new Vector2(Mathf.Max(1f, px * 0.5f), Mathf.Max(1f, px * 0.5f)), s, px, new Color(0f, 0f, 0f, 0.9f));
            NumberFont.Draw(band.center, s, px, c);
        }

        public static void DrawEnhance(Rect cell, Item item)
        {
            if (item == null || item.Enhance <= 0) return;
            int lv = item.Enhance;
            int tier = lv >= 15 ? 3 : lv >= 10 ? 2 : lv >= 5 ? 1 : 0;
            float t = Time.unscaledTime;
            float u = Mathf.Max(2f, Mathf.Round(cell.width * 0.045f)); // 金具の線の太さ
            float len = cell.width * (tier == 0 ? 0.22f : 0.3f);       // 金具の長さ
            Color main = tier == 3 ? Color.Lerp(new Color(0.55f, 0.8f, 1f), Color.white, 0.5f + 0.5f * Mathf.Sin(t * 3f))
                       : tier == 2 ? Color.Lerp(new Color(1f, 0.5f, 0.15f), new Color(1f, 0.85f, 0.4f), 0.5f + 0.5f * Mathf.Sin(t * 5f) * Mathf.Sin(t * 2.3f))
                       : FrameGold;
            var dark = new Color(0.1f, 0.06f, 0.04f, 0.9f);
            // 光る段は、角のまわりがにじむ（枠の内側だけ。外へはみ出さない）
            if (tier >= 2)
            {
                GUI.BeginGroup(cell);
                var gc = main; gc.a = 0.3f;
                float gs = len * 1.2f;
                foreach (var p in new[] { new Vector2(0f, 0f), new Vector2(cell.width, 0f), new Vector2(0f, cell.height), new Vector2(cell.width, cell.height) })
                    Glow(p.x, p.y, gs, gc);
                GUI.EndGroup();
            }
            // ひし形の留め具（角の飾り）
            void Diamond(float cx, float cy, float r, Color c)
            {
                for (int dy = -(int)r; dy <= (int)r; dy++)
                {
                    float w = r - Mathf.Abs(dy);
                    Fill(new Rect(Mathf.Round(cx - w), Mathf.Round(cy + dy), w * 2f + 1f, 1f), c);
                }
            }
            void Corner(float x, float y, float sx, float sy, bool shortArms)
            {
                float l = shortArms ? len * 0.45f : len;
                // L 字の角金具（暗い縁取り → 色）
                var h = new Rect(sx > 0 ? x : x - l, sy > 0 ? y : y - u, l, u);
                var v = new Rect(sx > 0 ? x : x - u, sy > 0 ? y : y - l, u, l);
                Fill(new Rect(h.x - 1f, h.y - 1f, h.width + 2f, h.height + 2f), dark);
                Fill(new Rect(v.x - 1f, v.y - 1f, v.width + 2f, v.height + 2f), dark);
                Fill(h, main);
                Fill(v, main);
                // 角にひし形の留め具（+5 から。段が上がると大きく）
                if (tier >= 1)
                {
                    float r = u * (tier >= 2 ? 1.8f : 1.4f);
                    float cx = x + sx * u * 0.5f, cy = y + sy * u * 0.5f;
                    Diamond(cx, cy, r + 1f, dark);
                    Diamond(cx, cy, r, Color.Lerp(main, Color.white, 0.4f));
                }
            }
            Corner(cell.x, cell.y, 1, 1, true); // 左上は数字があるので、腕を短く
            Corner(cell.xMax, cell.y, -1, 1, false);
            Corner(cell.x, cell.yMax, 1, -1, false);
            Corner(cell.xMax, cell.yMax, -1, -1, false);
            // 左上の「+N」（ドット文字）：角金具の内側に
            float nh = cell.height * 0.22f;
            var nc = tier == 3 ? new Color(0.8f, 0.92f, 1f) : tier == 2 ? new Color(1f, 0.75f, 0.4f) : new Color(1f, 0.9f, 0.6f);
            NumberFont.DrawIn(new Rect(cell.xMax - u * 3f - cell.width * 0.6f, cell.y + u * 2.2f, cell.width * 0.6f, nh), "+" + lv, nc, TextAnchor.MiddleRight, 1f); // 強化は右上（レベルは左上）
        }

        public static void DrawSprite(Rect guiRect, Sprite sprite)
        {
            if (sprite == null) return;
            GUI.DrawTexture(guiRect, sprite.texture, ScaleMode.ScaleToFit);
        }
    }
}
