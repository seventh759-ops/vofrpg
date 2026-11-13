using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace VividOfFade
{
    /// <summary>
    /// 画面のスクロール量（ピクセル、正なら内容を上へ送る）。指・マウスの左ボタンのドラッグと、マウスホイール。
    /// 1 フレームに 1 回だけ読む（OnGUI は 1 フレームに何度も呼ばれるため）。タップの判定は別（PlayerInputReader）。
    /// </summary>
    public static class ScrollInput
    {
        static int lastFrame = -1;
        static float delta;
        static bool down;
        static Vector2 lastPos;

        public static float Delta()
        {
            if (Time.frameCount == lastFrame) return delta;
            lastFrame = Time.frameCount;
            delta = 0f;

            bool pressed = false;
            Vector2 pos = Vector2.zero;
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                foreach (TouchControl t in touchscreen.touches)
                {
                    if (!t.press.isPressed) continue;
                    pressed = true;
                    pos = t.position.ReadValue();
                    break;
                }
            }
            var mouse = Mouse.current;
            if (!pressed && mouse != null && mouse.leftButton.isPressed)
            {
                pressed = true;
                pos = mouse.position.ReadValue();
            }
            if (pressed)
            {
                if (down) delta += pos.y - lastPos.y; // 画面の座標は下が 0。指を上へ動かすと内容が上へ送られる
                lastPos = pos;
            }
            down = pressed;

            if (mouse != null)
            {
                float wheel = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(wheel) > 0.01f) delta -= Mathf.Sign(wheel) * Screen.height * 0.05f;
            }
            return delta;
        }
    }
}
