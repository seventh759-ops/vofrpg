using UnityEngine;

namespace VividOfFade
{
    /// <summary>プレイヤーを追うカメラ。縦画面では横に約11マス見えるように合わせる。</summary>
    public sealed class CameraRig
    {
        readonly Camera cam;
        float shakeTime;
        float shakeDuration;
        float shakeAmount;
        Vector2 lastShake;

        /// <summary>
        /// 画面の上下の帯の高さが違うとき、プレイヤーを真ん中の領域の中心に置くためのずれ（画面の高さに対する割合）。
        /// 正なら真ん中の領域の中心が画面の中心より上にある（カメラを下げる）。
        /// </summary>
        public float CenterShift;

        /// <summary>迷宮の外の色（区画ごと）。</summary>
        public void SetBackground(Color color)
        {
            cam.backgroundColor = color;
        }

        public CameraRig(Camera cam)
        {
            this.cam = cam;
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.03f, 0.06f);
        }

        /// <summary>画面を揺らす。強さはユニット。</summary>
        public void Shake(float amount, float duration)
        {
            if (amount < shakeAmount && shakeTime > 0f) return;
            shakeAmount = amount;
            shakeDuration = duration;
            shakeTime = duration;
        }

        /// <summary>target は論理座標。画面上の位置に変換して追う。</summary>
        public void Tick(Vector2 target, Room room, float dt)
        {
            float aspect = Mathf.Max(0.1f, cam.aspect);
            // 縦画面：横幅を固定。横画面（エディタやPC）：縦に14ユニット見せる
            float size = aspect < 1f ? GameConfig.ViewWidthTiles / (2f * aspect) : 7f;
            cam.orthographicSize = size;

            float halfW = size * aspect;
            float halfH = size;
            // 迷宮は画面より広いので、そのまま追う
            Vector2 screen = Iso.ToScreen(target);
            float x = screen.x;
            float y = screen.y - CenterShift * 2f * size;

            Vector3 p = cam.transform.position - (Vector3)lastShake;
            float k = 1f - Mathf.Exp(-10f * dt);

            lastShake = Vector2.zero;
            if (shakeTime > 0f)
            {
                shakeTime -= Time.unscaledDeltaTime;
                float s = shakeAmount * Mathf.Clamp01(shakeTime / shakeDuration);
                lastShake = Random.insideUnitCircle * s;
            }
            cam.transform.position = new Vector3(Mathf.Lerp(p.x, x, k) + lastShake.x, Mathf.Lerp(p.y, y, k) + lastShake.y, -10f);
        }
    }
}
