#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace VividOfFade.EditorTools
{
    /// <summary>
    /// 配布に向けた設定を、エディタを開いたときに自動でそろえる（手で設定し忘れないように）。
    /// ・Android：画面の切り欠き（カメラの穴・ノッチ）の外にだけ描く（Render outside safe area をオフ）。
    ///   画面の上の LV・HP の段や MENU が、切り欠きに隠れないようにするため。
    ///   こうすると Unity が切り欠きの下から画面を始めるので、ゲームの側（Screen.height など）は何も変えなくてよい。
    /// ・画面は縦向きに固定する（横にしても回らない）。
    /// ・パッケージ名・バージョン・アイコンは、まだ初期値のままのときだけ入れる（あとで手で変えたら、それを優先する）。
    ///   今はまだ正式版（1.0）ではなく、テスト版（0.9.x）として扱う。
    /// </summary>
    [InitializeOnLoad]
    static class ReleaseSettings
    {
        const string PackageName = "com.tomoshibiworks.vividoffade";
        const string TestVersion = "0.9.1";
        const string IconPath = "Assets/Icons/AppIcon.png";

        static ReleaseSettings()
        {
            if (PlayerSettings.Android.renderOutsideSafeArea)
            {
                PlayerSettings.Android.renderOutsideSafeArea = false;
                Debug.Log("[ReleaseSettings] Android: Render outside safe area -> off (HUD stays clear of the camera cutout).");
            }

            if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait)
            {
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
                Debug.Log("[ReleaseSettings] Orientation -> Portrait.");
            }

            string id = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            if (string.IsNullOrEmpty(id) || id.StartsWith("com.DefaultCompany") || id.StartsWith("com.Company"))
            {
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);
                Debug.Log("[ReleaseSettings] Android package name -> " + PackageName);
            }

            if (PlayerSettings.bundleVersion == "1.0" || PlayerSettings.bundleVersion == "0.1" || PlayerSettings.bundleVersion == "0.1.0")
            {
                PlayerSettings.bundleVersion = TestVersion;
                Debug.Log("[ReleaseSettings] Version -> " + TestVersion + " (test build)");
            }

            // アイコンは画像の読み込みが済んでから入れる
            EditorApplication.delayCall += SetIcon;
        }

        static void SetIcon()
        {
            var importer = AssetImporter.GetAtPath(IconPath) as TextureImporter;
            if (importer == null) return;

            // ドット絵なので、ぼかさない・圧縮しない
            if (importer.filterMode != FilterMode.Point || importer.textureCompression != TextureImporterCompression.Uncompressed
                || importer.mipmapEnabled || importer.maxTextureSize < 1024)
            {
                importer.textureType = TextureImporterType.Default;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 1024;
                importer.SaveAndReimport();
            }

            var current = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Any);
            if (current != null && current.Length > 0 && current[0] != null) return;

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (tex == null) return;
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { tex }, IconKind.Any);
            Debug.Log("[ReleaseSettings] Default app icon -> " + IconPath);
        }
    }
}
#endif
