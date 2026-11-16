#if UNITY_EDITOR
using UnityEditor;

namespace VividOfFade.EditorTools
{
    /// <summary>
    /// 配布に向けた設定を、エディタを開いたときに自動でそろえる（手で設定し忘れないように）。
    /// ・Android：画面の切り欠き（カメラの穴・ノッチ）の外にだけ描く（Render outside safe area をオフ）。
    ///   画面の上の LV・HP の段や MENU が、切り欠きに隠れないようにするため。
    ///   こうすると Unity が切り欠きの下から画面を始めるので、ゲームの側（Screen.height など）は何も変えなくてよい。
    /// </summary>
    [InitializeOnLoad]
    static class ReleaseSettings
    {
        static ReleaseSettings()
        {
            if (PlayerSettings.Android.renderOutsideSafeArea)
            {
                PlayerSettings.Android.renderOutsideSafeArea = false;
                UnityEngine.Debug.Log("[ReleaseSettings] Android: Render outside safe area -> off (HUD stays clear of the camera cutout).");
            }
        }
    }
}
#endif
