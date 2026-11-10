using UnityEngine;

namespace VividOfFade
{
    /// <summary>見下ろし視点の前後関係。画面の下にいるものほど手前に描く。</summary>
    public static class SortOrder
    {
        public const int Floor = -30000;

        /// <summary>論理座標から。クォータービューでは画面の奥（上）ほど後ろに描く。</summary>
        public static int For(Vector2 logical) => ForY(Iso.Depth(logical));

        public static int ForY(float y) => Mathf.Clamp(Mathf.RoundToInt(-y * 100f), -20000, 20000);
    }
}
