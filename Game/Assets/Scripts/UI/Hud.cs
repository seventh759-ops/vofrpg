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

        Rect ToggleRect
        {
            get
            {
                float w = Screen.width * 0.30f;
                float h = Screen.height * 0.045f;
                float m = Screen.height * 0.015f;
                return new Rect(Screen.width - w - m, Screen.height - h - m, w, h);
            }
        }

        Rect BagButtonRect
        {
            get
            {
                var t = ToggleRect;
                return new Rect(t.x, t.y - t.height - Screen.height * 0.01f, t.width, t.height);
            }
        }

        Rect StatsButtonRect
        {
            get
            {
                var b = BagButtonRect;
                return new Rect(b.x, b.y - b.height - Screen.height * 0.01f, b.width, b.height);
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

        public void Toast(string text, Color color)
        {
            if (toasts.Count >= 5) toasts.RemoveAt(0);
            toasts.Add(new ToastLine { Text = text, Color = color });
        }

        void Awake()
        {
            bag = new BagScreen(this);
            stats = new StatsScreen(this);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt > 0f) fps = Mathf.Lerp(fps, 1f / dt, 0.05f);

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

            DrawNumbers();
            DrawHud();

            if (World.ChoosingClass) DrawClassSelect();
            else if (World.Player.IsDead) DrawGameOver();
            else if (World.BagOpen) bag.Draw(World);
            else if (World.StatsOpen) stats.Draw(World);

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

            // 体力
            var hpBack = new Rect(m, m, Screen.width * 0.45f, Screen.height * 0.02f);
            Fill(hpBack, new Color(0f, 0f, 0f, 0.6f));
            float ratio = player.MaxHp > 0 ? (float)player.Hp / player.MaxHp : 0f;
            Fill(new Rect(hpBack.x, hpBack.y, hpBack.width * ratio, hpBack.height), new Color(0.85f, 0.2f, 0.25f, 0.95f));

            // MP
            var mpBack = new Rect(m, hpBack.yMax + m * 0.3f, hpBack.width, hpBack.height * 0.6f);
            Fill(mpBack, new Color(0f, 0f, 0f, 0.6f));
            float mpRatio = player.MaxMp > 0 ? (float)player.Mp / player.MaxMp : 0f;
            Fill(new Rect(mpBack.x, mpBack.y, mpBack.width * mpRatio, mpBack.height), new Color(0.3f, 0.5f, 1f, 0.95f));

            // 経験値
            var ch = Profile.Current;
            var expBack = new Rect(m, mpBack.yMax + m * 0.3f, hpBack.width, hpBack.height * 0.3f);
            Fill(expBack, new Color(0f, 0f, 0f, 0.6f));
            if (ch != null)
            {
                float expRatio = Mathf.Clamp01((float)ch.Exp / Progression.ExpToNext(ch.Level));
                Fill(new Rect(expBack.x, expBack.y, expBack.width * expRatio, expBack.height), new Color(1f, 0.85f, 0.3f, 0.9f));
            }

            // 回避の準備
            var dodge = new Rect(m, expBack.yMax + m * 0.3f, hpBack.width * 0.5f, hpBack.height * 0.3f);
            Fill(dodge, new Color(0f, 0f, 0f, 0.6f));
            Fill(new Rect(dodge.x, dodge.y, dodge.width * player.DodgeReady, dodge.height), new Color(0.75f, 0.85f, 1f, 0.9f));

            // 右上のボタンと重ならないよう、左側の幅に収めて3行にする
            float lineH = label.fontSize * 1.4f;
            float textW = ToggleRect.x - m * 2f;
            float y = dodge.yMax + m * 0.5f;
            int level = ch != null ? ch.Level : 1;
            GUI.Label(new Rect(m, y, textW, lineH), $"Lv {level}   HP {player.Hp}/{player.MaxHp}   MP {player.Mp}", label);
            GUI.Label(new Rect(m, y + lineH, textW, lineH), $"WAVE {World.Waves.Wave}   ENEMY {World.Enemies.AliveCount}   KILL {World.Enemies.Kills}", label);
            GUI.Label(new Rect(m, y + lineH * 2f, textW, lineH), $"EMBERS {inv.Embers}   FPS {fps:0}", label);

            // 拾った品の通知
            float ty = y + lineH * 3.4f;
            foreach (var t in toasts)
            {
                var c = t.Color;
                c.a = Mathf.Clamp01((ToastLife - t.T) / 0.5f);
                var style = new GUIStyle(small);
                style.normal.textColor = c;
                GUI.Label(new Rect(m, ty, Screen.width, lineH), "+ " + t.Text, style);
                ty += small.fontSize * 1.3f;
            }

            if (World.Paused || player.IsDead || World.ChoosingClass) return;

            // 右上のボタン
            Button(ToGui(ToggleRect), World.AutoAttack ? "ATK: AUTO" : "ATK: HOLD", new Color(0f, 0f, 0f, 0.55f), centered,
                () => World.AutoAttack = !World.AutoAttack);
            string bagLabel = inv.BagFull ? $"BAG FULL {inv.Bag.Count}" : $"BAG {inv.Bag.Count}/{Inventory.BagCapacity}";
            Button(ToGui(BagButtonRect), bagLabel, inv.BagFull ? new Color(0.5f, 0.1f, 0.1f, 0.7f) : new Color(0f, 0f, 0f, 0.55f), centered,
                () => World.BagOpen = true);
            int points = ch != null ? ch.Points : 0;
            Button(ToGui(StatsButtonRect), points > 0 ? $"STATS +{points}" : "STATS",
                points > 0 ? new Color(0.5f, 0.4f, 0.05f, 0.75f) : new Color(0f, 0f, 0f, 0.55f), centered,
                () => World.StatsOpen = true);

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

            // 操作の説明（PC・エディタ向け）
            if (!Application.isMobilePlatform)
            {
                GUI.Label(new Rect(m, Screen.height - lineH * 2.2f, Screen.width, lineH * 2f),
                    "Move: WASD / drag   Dodge: Space / flick   Bag: B   Stats: C\nAttack (HOLD): J or right click   Toggle: T", small);
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
            GUI.Label(new Rect(0, h * 0.38f, Screen.width, h * 0.08f), "GAME OVER", title);
            GUI.Label(new Rect(0, h * 0.47f, Screen.width, h * 0.05f),
                $"WAVE {World.Waves.Wave}   KILL {World.Enemies.Kills}   EMBERS {World.Inventory.Embers}", centered);
            GUI.Label(new Rect(0, h * 0.53f, Screen.width, h * 0.05f), "Tap / Enter to continue", centered);
        }

        /// <summary>職業を選ぶ画面。3つの大きなボタン（PC は数字キー 1〜3 でも選べる）。</summary>
        void DrawClassSelect()
        {
            float W = Screen.width;
            float H = Screen.height;
            Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.75f));

            var title = new GUIStyle(centered) { fontSize = label.fontSize * 2, fontStyle = FontStyle.Bold };
            GUI.Label(new Rect(0, H * 0.12f, W, H * 0.06f), "VIVID OF FADE", title);
            GUI.Label(new Rect(0, H * 0.19f, W, H * 0.04f), "Choose your class", centered);

            float pad = W * 0.08f;
            float bh = H * 0.15f;
            float gap = H * 0.025f;
            float y = H * 0.27f;
            var nameStyle = new GUIStyle(label) { fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(label.fontSize * 1.3f) };
            for (int i = 0; i < ClassInfo.Count; i++)
            {
                var spec = ClassInfo.Get((ClassType)i);
                var type = spec.Type;
                var r = new Rect(pad, y, W - pad * 2f, bh);
                Button(r, null, new Color(1f, 1f, 1f, 0.12f), centered, () => World.StartRun(type));

                float inner = r.height * 0.12f;
                float icon = r.height - inner * 2f;
                DrawSprite(new Rect(r.x + inner, r.y + inner, icon, icon), PixelArt.PlayerFor(type));
                float tx = r.x + icon + inner * 2f;
                float tw = r.xMax - tx - inner;
                float lh = small.fontSize * 1.35f;
                string key = Application.isMobilePlatform ? "" : $"[{i + 1}] ";
                GUI.Label(new Rect(tx, r.y + inner, tw, nameStyle.fontSize * 1.4f), key + spec.Name, nameStyle);
                GUI.Label(new Rect(tx, r.y + inner + nameStyle.fontSize * 1.5f, tw, lh), spec.Line1, small);
                GUI.Label(new Rect(tx, r.y + inner + nameStyle.fontSize * 1.5f + lh, tw, lh), spec.Line2, small);
                var ch = Profile.Get(type);
                GUI.Label(new Rect(tx, r.y + inner + nameStyle.fontSize * 1.5f + lh * 2f, tw, lh),
                    $"Lv {ch.Level}   Main: {Attributes.ShortName(spec.MainAttr)}   Weapon: {ClassInfo.Weapon(spec.Starter).Name}", small);
                y += bh + gap;
            }
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
            if (text != null) GUI.Label(guiRect, text, style);
            if (Event.current.type == EventType.Repaint)
            {
                pendingRects.Add(new Rect(guiRect.x, Screen.height - guiRect.y - guiRect.height, guiRect.width, guiRect.height));
                pendingActions.Add(onTap);
            }
        }

        public static Rect ToGui(Rect r) => new Rect(r.x, Screen.height - r.y - r.height, r.width, r.height);

        public static void Fill(Rect guiRect, Color color)
        {
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(guiRect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        public static void DrawSprite(Rect guiRect, Sprite sprite)
        {
            if (sprite == null) return;
            GUI.DrawTexture(guiRect, sprite.texture, ScaleMode.ScaleToFit);
        }
    }
}
