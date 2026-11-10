using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// クォータービューの変換。ゲームの計算（移動・当たり判定・距離）は平面の「論理座標」で行い、
    /// 画面に描く位置だけをここで斜めに変換する。床の1マスは画面で 32×16 ドット（2×1 ユニット）のひし形。
    /// 論理座標の +x は画面の右上、+y は画面の左上へ伸びる。
    /// </summary>
    public static class Iso
    {
        public const float HalfTileW = 1f;   // 論理1マスあたりの画面上の横の広がり（の半分×2）
        public const float HalfTileH = 0.5f;

        public static Vector2 ToScreen(Vector2 p) =>
            new Vector2((p.x - p.y) * HalfTileW, (p.x + p.y) * HalfTileH);

        /// <summary>画面上の向き・距離を論理座標へ戻す（ToScreen の逆）。</summary>
        public static Vector2 ToLogical(Vector2 s) =>
            new Vector2((s.x / HalfTileW + s.y / HalfTileH) * 0.5f, (s.y / HalfTileH - s.x / HalfTileW) * 0.5f);

        /// <summary>画面上の高さ（奥ほど大きい）。描く順番に使う。</summary>
        public static float Depth(Vector2 p) => (p.x + p.y) * HalfTileH;
    }
}
