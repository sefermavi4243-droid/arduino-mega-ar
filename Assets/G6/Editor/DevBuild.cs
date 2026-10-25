using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace G6.EditorTools
{
    /// <summary>Hızlı deneme için Windows derlemesi (simülasyon modunda çalışır).</summary>
    public static class DevBuild
    {
        /// <summary>Komut satırı için: sahneyi yeniden kur, sonra Windows derlemesi al.</summary>
        public static void RebuildSceneAndWindows()
        {
            SceneBuilder.Build();
            BuildWindows();
        }

        [MenuItem("G6/Derle/Android APK", priority = 201)]
        public static void BuildAndroid()
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/G6/Scenes/G6_NesneTakibi_AR.unity" },
                locationPathName = "Builds/Android/G6_NesneTakibi.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None
            };
            EditorUserBuildSettings.buildAppBundle = false;
            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log("[G6] Android derlemesi: " + report.summary.result + " " + report.summary.totalErrors + " hata");
            if (Application.isBatchMode && report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        [MenuItem("G6/Derle/Windows deneme derlemesi", priority = 200)]
        public static void BuildWindows()
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { SceneBuilder.ScenePath },
                locationPathName = "Builds/Windows/G6_NesneTakibi.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log("[G6] Windows derlemesi: " + report.summary.result + " " + report.summary.totalErrors + " hata");
            if (Application.isBatchMode && report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
