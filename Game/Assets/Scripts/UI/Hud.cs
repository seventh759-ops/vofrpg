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
                World = world + new Vector2(UnityEngine.Random.Range(-0.15f, 0.15f), 0.3f),
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

            if (World.BagOpen && !World.Player.IsDead) bag.Draw(World);
            if (World.Player.IsDead) DrawGameOver();

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

            // 回避の準備
            var dodge = new Rect(m, hpBack.yMax + m * 0.4f, hpBack.width * 0.5f, hpBack.height * 0.4f);
            Fill(dodge, new Color(0f, 0f, 0f, 0.6f));
            Fill(new Rect(dodge.x, dodge.y, dodge.width * player.DodgeReady, dodge.height), new Color(0.5f, 0.75f, 1f, 0.9f));

            // 右上のボタンと重ならないよう、左側の幅に収めて3行にする
            float lineH = label.fontSize * 1.4f;
            float textW = ToggleRect.x - m * 2f;
            float y = dodge.yMax + m * 0.5f;
            GUI.Label(new Rect(m, y, textW, lineH), $"HP {player.Hp}/{player.MaxHp}   WAVE {World.Waves.Wave}", label);
            GUI.Label(new Rect(m, y + lineH, textW, lineH), $"ENEMY {World.Enemies.AliveCount}   KILL {World.Enemies.Kills}", label);
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

            if (World.BagOpen || player.IsDead) return;

            // 右上のボタン
            Button(ToGui(ToggleRect), World.AutoAttack ? "ATK: AUTO" : "ATK: HOLD", new Color(0f, 0f, 0f, 0.55f), centered,
                () => World.AutoAttack = !World.AutoAttack);
            string bagLabel = inv.BagFull ? $"BAG FULL {inv.Bag.Count}" : $"BAG {inv.Bag.Count}/{Inventory.BagCapacity}";
            Button(ToGui(BagButtonRect), bagLabel, inv.BagFull ? new Color(0.5f, 0.1f, 0.1f, 0.7f) : new Color(0f, 0f, 0f, 0.55f), centered,
                () => World.BagOpen = true);

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
                    "Move: WASD / drag   Dodge: Space / flick   Bag: B\nAttack (HOLD): J or right click   Toggle: T", small);
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
            GUI.Label(new Rect(0, h * 0.53f, Screen.width, h * 0.05f), "Tap / Enter to retry", centered);
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
