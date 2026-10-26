using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace G6.EditorTools
{
    /// <summary>
    /// Ekibin üç nesnesini tek sahnede birleştirir: Arduino Mega (bu proje, hedefler çalışma anında kurulur),
    /// kitap ve dinozor (ekip üyelerinin sahnelerindeki Model Target'lar ve etiketleri).
    /// Ekip sahnelerinden yalnızca hedefler ve onlara bağlı içerik taşınır; kamera, ışık ve EventSystem
    /// bizim sahnemizden gelir.
    /// </summary>
    public static class EkipSahnesi
    {
        public const string ScenePath = "Assets/G6/Scenes/G6_UcNesne.unity";

        static readonly (string path, string rename)[] TeamScenes =
        {
            ("Assets/Ekip/Kitap_Takibi.unity", "Kitap"),
            ("Assets/Ekip/Dinozor_Takibi.unity", "Dinozor"),
        };

        // Ekip sahnelerinden alınmayacak kök nesneler: bunların karşılığı zaten bizim sahnemizde var.
        static readonly string[] Skip = { "ARCamera", "Directional Light", "Global Volume", "EventSystem", "Main Camera" };

        [MenuItem("G6/Ekip/Üç nesneli sahneyi kur", priority = 30)]
        public static void Build()
        {
            if (!File.Exists(VuforiaWiring.ArScenePath)) VuforiaWiring.BuildArScene();
            var main = EditorSceneManager.OpenScene(VuforiaWiring.ArScenePath, OpenSceneMode.Single);

            foreach (var (path, rename) in TeamScenes)
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarning("[G6] Ekip sahnesi yok, atlandı: " + path);
                    continue;
                }
                var team = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                var group = new GameObject("Ekip_" + rename);
                SceneManager.MoveGameObjectToScene(group, main);
                foreach (var root in team.GetRootGameObjects())
                {
                    if (Skip.Contains(root.name)) continue;
                    SceneManager.MoveGameObjectToScene(root, main);
                    root.transform.SetParent(group.transform, true);
                    if (root.name == "ModelTarget") root.name = "ModelTarget_" + rename;
                    foreach (var mt in root.GetComponentsInChildren<Vuforia.ModelTargetBehaviour>(true))
                    {
                        // Kapalı başlar: Vuforia aynı anda tek Model Target etkin tutar, sırayı
                        // VuforiaModelTargetLoader yönetir. 360° (Advanced) hedefte silüete gerek yok; tek açılı
                        // hedefler (dinozor) ise silüet olmadan kolay bulunamaz, onlarda ekip üyesinin ayarı kalır.
                        if (new SerializedObject(mt).FindProperty("mDataSetPath").stringValue.Contains("Advanced"))
                            mt.GuideViewMode = Vuforia.ModelTargetBehaviour.GuideViewDisplayMode.NoGuideView;
                        mt.enabled = false;
                        EditorUtility.SetDirty(mt);
                    }
                }
                EditorSceneManager.CloseScene(team, true);
                Debug.Log($"[G6] {rename} sahnesi eklendi: {group.transform.childCount} kök nesne");
            }

            EditorSceneManager.SaveScene(main, ScenePath);
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[G6] Üç nesneli sahne kaydedildi: " + ScenePath);
        }

        /// <summary>Komut satırı için: sahneyi kur ve Android APK'sını derle.</summary>
        public static void BuildAndBuildAndroid()
        {
            Build();
            var report = UnityEditor.BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Android/G6_UcNesne.apk",
                target = BuildTarget.Android,
            });
            Debug.Log("[G6] Üç nesne Android derlemesi: " + report.summary.result + " " + report.summary.totalErrors + " hata");
            if (Application.isBatchMode && report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
