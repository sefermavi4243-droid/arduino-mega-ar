using G6;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Vuforia;

namespace G6.EditorTools
{
    /// <summary>
    /// Gerçek cihaz için AR sahnesini kurar. Simülasyon sahnesi (sunum için) olduğu gibi kalır.
    /// AR sahnesinde: kamera Vuforia kamerasıdır, simülasyon yoktur, Model Target çalışma anında
    /// VuforiaModelTargetLoader tarafından veritabanından oluşturulur.
    /// </summary>
    public static class VuforiaWiring
    {
        public const string ArScenePath = "Assets/G6/Scenes/G6_NesneTakibi_AR.unity";

        [MenuItem("G6/Vuforia/AR sahnesini kur", priority = 20)]
        public static void BuildArScene()
        {
            SceneBuilder.Build(); // önce simülasyon sahnesini güncel ölçülerle yeniden kur
            var scene = EditorSceneManager.OpenScene(SceneBuilder.ScenePath, OpenSceneMode.Single);

            var twin = Object.FindFirstObjectByType<DigitalTwin>();
            var sim = Object.FindFirstObjectByType<SimulatedTrackingSource>();
            twin.transform.SetParent(null, false);
            twin.name = "DigitalTwin (Model Target'a bağlanır)";
            Object.DestroyImmediate(sim.gameObject); // masa ve sahte zımba da gider

            var cam = Camera.main;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            cam.gameObject.AddComponent<VuforiaBehaviour>();
            cam.gameObject.AddComponent<DefaultInitializationErrorHandler>(); // lisans/kamera hatalarını ekranda gösterir

            var app = Object.FindFirstObjectByType<AppController>();
            var loader = app.gameObject.AddComponent<VuforiaModelTargetLoader>();
            var so = new SerializedObject(loader);
            so.FindProperty("twin").objectReferenceValue = twin;
            so.FindProperty("app").objectReferenceValue = app;
            var profile = SceneBuilder.CurrentProfile();
            so.FindProperty("dataSetPath").stringValue = "Vuforia/" + profile.DatasetName + ".xml";
            so.FindProperty("targetName").stringValue = profile.TargetName;
            var names = so.FindProperty("guideViews");
            var titles = so.FindProperty("guideViewTitles");
            names.arraySize = titles.arraySize = profile.GuideViews.Length;
            for (int i = 0; i < profile.GuideViews.Length; i++)
            {
                names.GetArrayElementAtIndex(i).stringValue = profile.GuideViews[i].name;
                titles.GetArrayElementAtIndex(i).stringValue = profile.GuideViews[i].title;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ArScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ArScenePath, true),
                new EditorBuildSettingsScene(SceneBuilder.ScenePath, false),
            };
            Debug.Log("[G6] AR sahnesi kuruldu: " + ArScenePath);
        }

        /// <summary>Ölçülere göre sahneleri ve Model Target dosyalarını tek seferde hazırlar (komut satırından da çağrılır).</summary>
        [MenuItem("G6/Vuforia/Hepsini hazırla (ölçüler, DAE, AR sahnesi)", priority = 0)]
        public static void BatchPrepare()
        {
            SceneBuilder.Build();
            EditorSceneManager.OpenScene(SceneBuilder.ScenePath, OpenSceneMode.Single);
            ObjExporter.PrepareModelTarget();
            BuildArScene();
        }
    }
}
