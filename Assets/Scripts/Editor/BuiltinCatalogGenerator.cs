#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using YooAsset;

namespace Game.EditorTools
{
    /// <summary>
    /// 一键生成 YooAsset 空的 BuiltinCatalog（用于 HostPlayMode 初始化）
    /// Menu: Tools/YooAsset/Generate Empty BuiltinCatalog
    /// </summary>
    public static class BuiltinCatalogGenerator
    {
        private const string PackageName = "DefaultPackage";
        private static readonly string PackageDir = Path.Combine(Application.streamingAssetsPath, "yoo", PackageName);

        [MenuItem("Tools/YooAsset/Generate Empty BuiltinCatalog (for HostPlayMode)")]
        public static void GenerateEmpty()
        {
            if (!Directory.Exists(PackageDir))
            {
                EditorUtility.DisplayDialog("Error", "Package dir not found:\n" + PackageDir, "OK");
                return;
            }

            string versionFile = Path.Combine(PackageDir, $"{PackageName}.version");
            string version = File.Exists(versionFile)
                ? File.ReadAllText(versionFile).Trim()
                : "0";

            bool ok = BuiltinCatalogHelper.CreateEmptyFile(PackageName, version, PackageDir);
            if (ok)
            {
                AssetDatabase.Refresh();
                Debug.Log($"[BuiltinCatalogGenerator] OK: empty catalog saved to {PackageDir}, version={version}");
            }
            else
            {
                Debug.LogError("[BuiltinCatalogGenerator] Failed to create catalog.");
            }
        }
    }
}
#endif
