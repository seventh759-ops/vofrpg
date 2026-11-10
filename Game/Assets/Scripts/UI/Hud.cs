using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// M1 用の仮の画面表示（IMGUI）。体力・波・撃破数・FPS、攻撃モード切替、攻撃ボタン、仮想スティック。
    /// ボタンの判定は PlayerInputReader が行い、ここは見た目だけを描く。
    /// 日本語フォントの用意がまだないため、表示は英語にしている。
    /// </summary>
    public sealed class Hud : MonoBehaviour
    {
        public GameWorld World;

        GUIStyle label;
        GUIStyle centered;
        GUIStyle big;
        float fps = 60f;

        // 画面のピクセル座標（左下が原点）。入力の判定と描画の両方で使う
        public Rect ToggleRect
        {
            get
            {
                float w = Screen.width * 0.30f;
                float h = Screen.height * 0.05f;
                float m = Screen.height * 0.015f;
                return new Rect(Screen.width - w - m, Screen.height - h - m, w, h);
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

        public bool IsReserved(Vector2 screenPos) => ToggleRect.Contains(screenPos);

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt > 0f) fps = Mathf.Lerp(fps, 1f / dt, 0.05f);
        }

        void OnGUI()
        {
            if (World == null || World.Player == null) return;
            EnsureStyles();

            var player = World.Player;
            float m = Screen.height * 0.015f;

            // 体力
            var hpBack = new Rect(m, m, Screen.width * 0.45f, Screen.height * 0.02f);
            Fill(hpBack, new Color(0f, 0f, 0f, 0.6f));
            float ratio = (float)player.Hp / GameConfig.PlayerMaxHp;
            Fill(new Rect(hpBack.x, hpBack.y, hpBack.width * ratio, hpBack.height), new Color(0.85f, 0.2f, 0.25f, 0.95f));

            // 回避の準備
            var dodge = new Rect(m, hpBack.yMax + m * 0.4f, hpBack.width * 0.5f, hpBack.height * 0.4f);
            Fill(dodge, new Color(0f, 0f, 0f, 0.6f));
            Fill(new Rect(dodge.x, dodge.y, dodge.width * player.DodgeReady, dodge.height), new Color(0.5f, 0.75f, 1f, 0.9f));

            float lineH = label.fontSize * 1.4f;
            float y = dodge.yMax + m * 0.5f;
            GUI.Label(new Rect(m, y, Screen.width, lineH),
                $"HP {player.Hp}   WAVE {World.Waves.Wave}   ENEMY {World.Enemies.AliveCount}   KILL {World.Enemies.Kills}", label);
            GUI.Label(new Rect(m, y + lineH, Screen.width, lineH), $"FPS {fps:0}", label);

            // 攻撃モードの切り替え
            var toggle = ToGui(ToggleRect);
            Fill(toggle, new Color(0f, 0f, 0f, 0.55f));
            GUI.Label(toggle, World.AutoAttack ? "ATK: AUTO" : "ATK: HOLD", centered);

            // 攻撃ボタン（押している間だけ、のとき）
            if (!World.AutoAttack)
            {
                var atk = ToGui(AttackRect);
                bool held = World.Input.AttackHeld;
                Fill(atk, held ? new Color(1f, 0.6f, 0.2f, 0.55f) : new Color(1f, 1f, 1f, 0.2f));
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
                    "Move: WASD / drag   Dodge: Space / flick\nAttack (HOLD): J or right click   Toggle: T", label);
            }

            // やられたとき
            if (player.IsDead)
            {
                Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.6f));
                float h = Screen.height;
                GUI.Label(new Rect(0, h * 0.38f, Screen.width, h * 0.08f), "GAME OVER", big);
                GUI.Label(new Rect(0, h * 0.47f, Screen.width, h * 0.05f),
                    $"WAVE {World.Waves.Wave}   KILL {World.Enemies.Kills}", centered);
                GUI.Label(new Rect(0, h * 0.53f, Screen.width, h * 0.05f), "Tap / Enter to retry", centered);
            }
        }

        void EnsureStyles()
        {
            int size = Mathf.Max(12, Mathf.RoundToInt(Screen.height * 0.02f));
            if (label != null && label.fontSize == size) return;

            label = new GUIStyle(GUI.skin.label) { fontSize = size };
            label.normal.textColor = Color.white;
            centered = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
            big = new GUIStyle(centered) { fontSize = size * 3, fontStyle = FontStyle.Bold };
        }

        static Rect ToGui(Rect r) => new Rect(r.x, Screen.height - r.y - r.height, r.width, r.height);

        static void Fill(Rect guiRect, Color color)
        {
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(guiRect, Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}
