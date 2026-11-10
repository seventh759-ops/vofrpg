using UnityEngine;

namespace VividOfFade
{
    /// <summary>見下ろし視点の前後関係。画面の下にいるものほど手前に描く。</summary>
    public static class SortOrder
    {
        public const int Floor = -30000;

        public static int ForY(float y) => Mathf.Clamp(Mathf.RoundToInt(-y * 100f), -20000, 20000);
    }
}
