using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace G6.EditorTools
{
    /// <summary>
    /// Projeyi tek komutla hazırlar: URP, TextMeshPro kaynakları, Android oyuncu ayarları ve demo sahnesi.
    /// Komut satırından: Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod G6.EditorTools.ProjectSetup.BatchSetup
    /// </summary>
    public static class ProjectSetup
    {
        const string UrpPath = "Assets/G6/Settings/G6_URP.asset";
        const string RendererPath = "Assets/G6/Settings/G6_Renderer.asset";
        const string TmpPackage = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

        [MenuItem("G6/Kurulum/Tümünü kur", priority = 100)]
        public static void SetupAll()
        {
            SetupUrp();
            ImportTmpEssentials();
            SetupPlayer();
            SceneBuilder.Build();
        }

        public static void BatchSetup() => SetupAll();

        [MenuItem("G6/Kurulum/URP ayarla", priority = 101)]
        public static void SetupUrp()
        {
            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpPath);
            if (urp == null)
            {
                var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, RendererPath);
                urp = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(urp, UrpPath);
            }
            urp.msaaSampleCount = 4;
            urp.shadowDistance = 3f;
            urp.supportsHDR = false; // mobilde bant genişliği tasarrufu
            EditorUtility.SetDirty(urp);

            GraphicsSettings.defaultRenderPipeline = urp;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = urp;
            }
            QualitySettings.SetQualityLevel(current, false);
            AssetDatabase.SaveAssets();
            Debug.Log("[G6] URP ayarlandı.");
        }

        [MenuItem("G6/Kurulum/TextMeshPro kaynaklarını içe aktar", priority = 102)]
        public static void ImportTmpEssentials()
        {
            if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
                return;
            string full = Path.GetFullPath(TmpPackage);
            if (!File.Exists(full))
            {
                Debug.LogError("[G6] TMP paketi bulunamadı: " + full);
                return;
            }
            AssetDatabase.ImportPackage(full, false);
            Debug.Log("[G6] TMP Essential Resources içe aktarıldı.");
        }

        [MenuItem("G6/Kurulum/Android ayarları", priority = 103)]
        public static void SetupPlayer()
        {
            PlayerSettings.companyName = "G6";
            PlayerSettings.productName = "G6 Nesne Takibi";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            var android = NamedBuildTarget.Android;
            PlayerSettings.SetApplicationIdentifier(android, "com.g6.nesnetakibi");
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)29; // Vuforia 11 en az Android 10 ister
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            Debug.Log("[G6] Android oyuncu ayarları yapıldı.");
        }
    }
}
