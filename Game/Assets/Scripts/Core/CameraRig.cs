using UnityEngine;

namespace VividOfFade
{
    /// <summary>プレイヤーを追うカメラ。縦画面では横に約11マス見えるように合わせる。</summary>
    public sealed class CameraRig
    {
        readonly Camera cam;

        public CameraRig(Camera cam)
        {
            this.cam = cam;
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.03f, 0.06f);
        }

        public void Tick(Vector2 target, Room room, float dt)
        {
            float aspect = Mathf.Max(0.1f, cam.aspect);
            // 縦画面：横幅を固定。横画面（エディタやPC）：縦に14マス見せる
            float size = aspect < 1f ? GameConfig.ViewWidthTiles / (2f * aspect) : 7f;
            cam.orthographicSize = size;

            float halfW = size * aspect;
            float halfH = size;
            float limitX = room.HalfW + 1f - halfW; // 壁の外側1マスまで
            float limitY = room.HalfH + 1f - halfH;
            float x = limitX > 0f ? Mathf.Clamp(target.x, -limitX, limitX) : 0f;
            float y = limitY > 0f ? Mathf.Clamp(target.y, -limitY, limitY) : 0f;

            Vector3 p = cam.transform.position;
            float k = 1f - Mathf.Exp(-10f * dt);
            cam.transform.position = new Vector3(Mathf.Lerp(p.x, x, k), Mathf.Lerp(p.y, y, k), -10f);
        }
    }
}
