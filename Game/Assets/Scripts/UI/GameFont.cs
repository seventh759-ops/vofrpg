using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 画面の文字に使うフォント。日本語も英語も、太めのゴシック（M PLUS 1p Bold、SIL Open Font License）で表示する。
    /// フォントは Assets/Resources/Fonts/MPLUS1p-Bold.ttf。読めなければ Unity の標準の文字に戻る。
    /// </summary>
    public static class GameFont
    {
        static Font font;
        static bool tried;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() { font = null; tried = false; }

        public static Font Get()
        {
            if (!tried)
            {
                tried = true;
                font = Resources.Load<Font>("Fonts/MPLUS1p-Bold");
            }
            return font;
        }

        /// <summary>OnGUI の中で呼ぶ。標準の見た目（GUI.skin）の文字をこのフォントにする。</summary>
        public static void Apply()
        {
            var f = Get();
            if (f != null && GUI.skin.font != f) GUI.skin.font = f;
        }
    }
}
