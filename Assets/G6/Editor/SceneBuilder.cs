using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace G6.EditorTools
{
    /// <summary>
    /// Demo sahnesini sıfırdan kurar: kamera, ışık, masa, simülasyondaki "gerçek" nesne, hologram ikiz ve uygulama.
    /// Hangi nesnenin kurulacağı Assets/G6/Data/NesneAyarlari.asset dosyasından okunur.
    /// </summary>
    public static class SceneBuilder
    {
        public const string ScenePath = "Assets/G6/Scenes/G6_NesneTakibi.unity";
        const string SpecPath = "Assets/G6/Data/ZimbaOlculeri.asset";
        const string SettingsPath = "Assets/G6/Data/NesneAyarlari.asset";
        const string MatDir = "Assets/G6/Materials";

        /// <summary>Ayarlarda seçili nesnenin profilini döndürür (parçalar, veritabanı adı, kılavuz açıları).</summary>
        public static ObjectProfile CurrentProfile()
        {
            var settings = AssetDatabase.LoadAssetAtPath<NesneAyarlari>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<NesneAyarlari>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }
            return settings.nesne == TrackedObjectKind.Zimba ? StaplerProfile(LoadOrCreateSpec()) : ArduinoMegaModel.Profile();
        }

        static ObjectProfile StaplerProfile(StaplerSpec spec) => new ObjectProfile
        {
            DisplayName = "Zımba",
            DatasetName = "G6_Zimba",
            TargetName = "Zimba",
            FileStem = "zimba",
            BodyColor = spec.bodyColor,
            SimDistance = 0.32f, SimPitch = 28f, SimYaw = 145f,
            GuideViews = new[]
            {
                ("OndenSol", "Önden-soldan", new Vector3(-0.55f, 0.6f, 1f)),
                ("OndenSag", "Önden-sağdan", new Vector3(0.55f, 0.6f, 1f)),
                ("SolYan", "Sol yandan", new Vector3(-1f, 0.45f, 0.1f)),
                ("SagYan", "Sağ yandan", new Vector3(1f, 0.45f, 0.1f)),
                ("Arkadan", "Arkadan", new Vector3(0.3f, 0.6f, -1f)),
            },
            Parts = Stapler(spec),
        };

        [MenuItem("G6/Sahneyi Kur", priority = 0)]
        public static void Build()
        {
            var spec = LoadOrCreateSpec();
            var profile = CurrentProfile();
            var mats = CreateMaterials(spec);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.62f, 0.7f);
            RenderSettings.ambientEquatorColor = new Color(0.32f, 0.33f, 0.36f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.1f, 0.09f);
            RenderSettings.skybox = null;

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.045f, 0.06f, 0.08f);
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 20f;
            cam.fieldOfView = 60f;
            camGo.AddComponent<AudioListener>();
            camGo.transform.SetPositionAndRotation(new Vector3(0.2f, 0.2f, -0.3f), Quaternion.Euler(28f, -35f, 0f));

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(55f, -40f, 0f);

            // Simülasyon kökü: Vuforia'ya geçince "G6/Vuforia/Model Target'a bağla" bunu kapatır.
            var root = new GameObject("TakipEdilenNesne_Simulasyon");
            var sim = root.AddComponent<SimulatedTrackingSource>();
            var simSo = new SerializedObject(sim);
            simSo.FindProperty("distance").floatValue = profile.SimDistance;
            simSo.FindProperty("pitch").floatValue = profile.SimPitch;
            simSo.FindProperty("yaw").floatValue = profile.SimYaw;
            simSo.ApplyModifiedPropertiesWithoutUndo();

            var twinGo = new GameObject("DigitalTwin");
            twinGo.transform.SetParent(root.transform, false);
            var twin = twinGo.AddComponent<DigitalTwin>();

            var real = new GameObject("SimRealObject");
            real.transform.SetParent(root.transform, false);

            var desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            desk.name = "Masa";
            desk.transform.SetParent(real.transform, false);
            desk.transform.localPosition = new Vector3(0, -0.01f, 0);
            desk.transform.localScale = new Vector3(3f, 0.02f, 2f);
            desk.GetComponent<Renderer>().sharedMaterial = mats["Masa"];
            Object.DestroyImmediate(desk.GetComponent<Collider>());

            var realStapler = new GameObject(profile.TargetName);
            realStapler.transform.SetParent(real.transform, false);

            foreach (var def in profile.Parts)
            {
                var part = BuildPart(def, twinGo.transform, mats["Hologram"], keepColliders: true);
                var view = part.AddComponent<PartView>();
                view.partId = def.Id;
                view.title = def.Title;
                view.description = def.Description;
                view.funFact = def.FunFact;
                view.color = def.Color;
                view.primary = def.Primary;
                view.explodeOffset = def.Explode;
                view.labelDirection = def.LabelDir.normalized;
                view.labelPoint = TopCenterLocal(part);
                foreach (var r in part.GetComponentsInChildren<Renderer>())
                    r.shadowCastingMode = ShadowCastingMode.Off;

                var realPart = BuildPart(def, realStapler.transform, mats[def.RealMaterial], keepColliders: false);
                realPart.name = def.Id + "_gercek";
            }

            AddFeatures(twinGo, profile, mats);

            var app = new GameObject("G6App");
            var labelsGo = new GameObject("Etiketler");
            labelsGo.transform.SetParent(app.transform, false);
            var labels = labelsGo.AddComponent<LabelManager>();
            var controller = app.AddComponent<AppController>();

            var so = new SerializedObject(controller);
            so.FindProperty("twin").objectReferenceValue = twin;
            so.FindProperty("labels").objectReferenceValue = labels;
            so.FindProperty("glowMaterial").objectReferenceValue = mats["Glow"];
            so.FindProperty("objectName").stringValue = profile.DisplayName;
            so.ApplyModifiedPropertiesWithoutUndo();

            so = new SerializedObject(labels);
            so.FindProperty("glowMaterial").objectReferenceValue = mats["Glow"];
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log("[G6] Sahne kuruldu: " + ScenePath);
        }

        /// <summary>Pin haritası, akım animasyonu ve Blink demosu (profil destekliyorsa).</summary>
        static void AddFeatures(GameObject twinGo, ObjectProfile profile, Dictionary<string, Material> mats)
        {
            if (profile.Pins != null)
            {
                var map = twinGo.AddComponent<PinMap>();
                map.pins = profile.Pins.ToArray();
                map.glowMaterial = mats["Glow"];
            }
            if (profile.EnergyPaths != null)
            {
                var flow = twinGo.AddComponent<EnergyFlow>();
                flow.paths = profile.EnergyPaths;
                flow.glowMaterial = mats["Glow"];
            }
            if (profile.HasBlink)
            {
                var blinkFlowGo = new GameObject("BlinkAkimi");
                blinkFlowGo.transform.SetParent(twinGo.transform, false);
                var blinkFlow = blinkFlowGo.AddComponent<EnergyFlow>();
                blinkFlow.paths = new[] { profile.BlinkPath };
                blinkFlow.glowMaterial = mats["Glow"];
                blinkFlow.spawnInterval = 0.08f;

                var blink = twinGo.AddComponent<BlinkDemo>();
                blink.pin13 = profile.Pin13;
                blink.pinGnd = profile.PinGnd;
                blink.glowMaterial = mats["Glow"];
                blink.ledBodyMaterial = mats["MetalParlak"];
                blink.flow = blinkFlow;
            }
        }

        static GameObject BuildPart(PartDef def, Transform parent, Material mat, bool keepColliders)
        {
            var part = new GameObject(def.Id);
            part.transform.SetParent(parent, false);
            int i = 0;
            foreach (var (type, pos, euler, scale) in def.Shapes)
            {
                var shape = GameObject.CreatePrimitive(type);
                shape.name = def.Id + "_" + i++;
                shape.transform.SetParent(part.transform, false);
                shape.transform.localPosition = pos;
                shape.transform.localRotation = Quaternion.Euler(euler);
                shape.transform.localScale = scale;
                shape.GetComponent<Renderer>().sharedMaterial = mat;
                if (!keepColliders) Object.DestroyImmediate(shape.GetComponent<Collider>());
            }
            foreach (var (mesh, pos) in def.Meshes)
            {
                var go = new GameObject(def.Id + "_" + i++, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(part.transform, false);
                go.transform.localPosition = pos;
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                go.GetComponent<MeshRenderer>().sharedMaterial = mat;
                if (keepColliders) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            }
            return part;
        }

        /// <summary>Etiket çizgisinin başlayacağı nokta: parçanın üst yüzeyine yakın merkez.</summary>
        static Vector3 TopCenterLocal(GameObject part)
        {
            var renderers = part.GetComponentsInChildren<Renderer>();
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return part.transform.InverseTransformPoint(b.center + Vector3.up * b.extents.y * 0.6f);
        }

        // Gipta F0556'nın yandan görünüşü (santimetre; z = önden arkaya, 0 = tabanın ortası, + = ön; y = masadan yükseklik).
        // Değerler yan fotoğraftan ölçüldü (1 cm ≈ 83 piksel). Kol öne doğru yükselir, arkası aşağı kıvrılır.
        static readonly Vector2[] ArmProfileCm =
        {
            new(-6.85f, 3.0f), new(-6.8f, 2.6f), new(-6.4f, 2.35f), new(-5.6f, 2.3f), new(-1.6f, 3.4f),
            new(4.6f, 3.75f), new(5.15f, 3.85f), new(5.15f, 4.8f), new(4.9f, 5.0f), new(-1.6f, 4.6f),
            new(-6.1f, 4.1f), new(-6.6f, 3.7f),
        };

        // Taban: arkası 1,5 cm, önü 1,2 cm kalınlığında; ön ucu yuvarlatılmış.
        static readonly Vector2[] BaseProfileCm =
        {
            new(-6f, 0f), new(5.6f, 0f), new(6f, 0.4f), new(6f, 0.9f), new(5.6f, 1.2f), new(-5.6f, 1.5f), new(-6f, 1.2f),
        };

        static List<PartDef> Stapler(StaplerSpec s)
        {
            // Fotoğraftaki ölçüler 12 cm boy ve 5 cm yükseklik içindir; spec farklıysa orantılı ölçekle.
            float sz = s.length / 0.12f * 0.01f;  // cm -> m (boy)
            float sy = s.height / 0.05f * 0.01f;  // cm -> m (yükseklik)
            float W = s.width;
            float k = s.length / 0.16f; // patlatma mesafeleri boyutla orantılı büyüsün
            Vector3 P(float z, float y) => new Vector3(0, y * sy, z * sz);
            Vector3 Box(float z0, float z1, float y0, float y1, float w) => new Vector3(w, (y1 - y0) * sy, (z1 - z0) * sz);
            Vector3 Mid(float z0, float z1, float y0, float y1) => P((z0 + z1) / 2f, (y0 + y1) / 2f);
            var parts = new List<PartDef>();

            var arm = new PartDef
            {
                Id = "ust_kol", Title = "Üst Kol",
                Description = "Kuvveti uyguladığın kaldıraç kolu. Arkadaki menteşe etrafında döner; bastırınca içindeki sürücü bıçak en öndeki teli şarjörden aşağı iter.",
                FunFact = "Kaldıraç ilkesi: Elini menteşeden ne kadar uzağa koyarsan zımbalamak için o kadar az kuvvet gerekir.",
                Color = new Color(1f, 0.35f, 0.85f), Explode = new Vector3(0, 0.06f, 0) * k, LabelDir = new Vector3(0.3f, 1f, 0.2f),
                RealMaterial = "PlastikGovde"
            };
            arm.Meshes.Add((SaveMesh(ProfileMesh.Extrude("UstKol", Scale(ArmProfileCm, sz, sy), W)), Vector3.zero));
            parts.Add(arm);

            var mag = new PartDef
            {
                Id = "sarjor", Title = "Şarjör",
                Description = "Zımba tellerini sıra hâlinde tutan metal kanal. Çoğu modelde üst kol kaldırılıp arkaya açılarak doldurulur.",
                FunFact = "Kanalın genişliği makinenin hangi tel boyutunu (ör. 24/6 veya 26/6) kabul edeceğini belirler.",
                Color = new Color(0.25f, 0.9f, 1f), Explode = new Vector3(0, 0.035f, 0) * k, LabelDir = new Vector3(-1f, 0.25f, 0),
                RealMaterial = "Metal"
            };
            mag.Shapes.Add((PrimitiveType.Cube, Mid(-2.15f, 4.65f, 2.2f, 3.75f), Vector3.zero, Box(-2.15f, 4.65f, 2.2f, 3.75f, W * 0.62f)));
            parts.Add(mag);

            var staples = new PartDef
            {
                Id = "zimba_teli", Title = "Zımba Teli",
                Description = "Birbirine yapıştırılmış U biçimli tellerden oluşan dizi. Her basışta en öndeki tel diziden kopup kağıdı deler.",
                FunFact = "26/6 kodunda 26 tel kalınlığını, 6 ise tel bacağının milimetre cinsinden uzunluğunu gösterir.",
                Color = new Color(1f, 0.75f, 0.25f), Explode = new Vector3(0, 0.03f, 0.09f) * k, LabelDir = new Vector3(1f, 0.4f, 0.3f),
                RealMaterial = "MetalParlak"
            };
            staples.Shapes.Add((PrimitiveType.Cube, Mid(-1.2f, 4.2f, 2.6f, 3.2f), Vector3.zero, Box(-1.2f, 4.2f, 2.6f, 3.2f, W * 0.4f)));
            parts.Add(staples);

            var spring = new PartDef
            {
                Id = "itici_yay", Title = "İtici Yay",
                Description = "Tel dizisini sürekli öne doğru iter; böylece her basıştan sonra yeni tel bıçağın altına gelir.",
                FunFact = "Yay zayıflarsa son birkaç tel ileri gelmez ve makine içi doluyken bile boş gibi davranır.",
                Color = new Color(0.55f, 1f, 0.35f), Explode = new Vector3(0, 0.03f, -0.09f) * k, LabelDir = new Vector3(-1f, 0.5f, -0.3f),
                RealMaterial = "Metal"
            };
            spring.Shapes.Add((PrimitiveType.Cylinder, P(-1.5f, 2.9f), new Vector3(90, 0, 0), new Vector3(0.006f, 0.6f * sz, 0.006f)));
            parts.Add(spring);

            var hinge = new PartDef
            {
                Id = "mentese", Title = "Menteşe Pimi",
                Description = "Üst kolun ve şarjörün etrafında döndüğü mil. İçindeki geri dönüş yayı, bıraktığında kolu yukarı kaldırır.",
                FunFact = "Menteşenin konumu kaldıraç oranını belirler; uzun kollu zımbalar kalın dosyaları daha kolay deler.",
                Color = new Color(0.65f, 0.5f, 1f), Explode = new Vector3(0.05f, 0.04f, 0) * k, LabelDir = new Vector3(0.8f, 0.3f, -0.6f),
                RealMaterial = "Metal"
            };
            // Arka metal ayak (kolu tabana bağlayan parça) ve içinden geçen pim.
            hinge.Shapes.Add((PrimitiveType.Cube, Mid(-5.3f, -2.15f, 1.5f, 2.9f), Vector3.zero, Box(-5.3f, -2.15f, 1.5f, 2.9f, W * 0.7f)));
            hinge.Shapes.Add((PrimitiveType.Cylinder, P(-4.6f, 2.3f), new Vector3(0, 0, 90), new Vector3(0.006f, (W + 0.002f) / 2f, 0.006f)));
            parts.Add(hinge);

            var anvil = new PartDef
            {
                Id = "ors", Title = "Örs",
                Description = "Tel bacaklarının kağıdın altında büküldüğü oluklu metal plaka. Basınçla bacakları içe doğru kıvırır.",
                FunFact = "Birçok zımbada örs döndürülebilir: Bacaklar dışa kıvrılırsa kolayca sökülen 'geçici zımbalama' yapılır.",
                Color = new Color(1f, 0.5f, 0.25f), Explode = new Vector3(0, -0.015f, 0.05f) * k, LabelDir = new Vector3(1f, 0.1f, 0.6f),
                RealMaterial = "MetalParlak"
            };
            anvil.Shapes.Add((PrimitiveType.Cube, Mid(3.4f, 5.4f, 1.2f, 1.35f), Vector3.zero, Box(3.4f, 5.4f, 1.2f, 1.35f, W * 0.55f)));
            parts.Add(anvil);

            var basePart = new PartDef
            {
                Id = "taban", Title = "Taban",
                Description = "Makinenin masaya oturan gövdesi. Örsü taşır ve basma kuvvetini masaya aktarır.",
                FunFact = "Altındaki kaydırmaz pedler, bastırırken makinenin masada kaymasını önler.",
                Color = new Color(0.25f, 1f, 0.75f), Explode = new Vector3(0, -0.03f, 0) * k, LabelDir = new Vector3(-1f, -0.2f, 0.2f),
                RealMaterial = "PlastikGovde"
            };
            basePart.Meshes.Add((SaveMesh(ProfileMesh.Extrude("Taban", Scale(BaseProfileCm, sz, sy), W)), Vector3.zero));
            parts.Add(basePart);

            return parts;
        }

        static Vector2[] Scale(Vector2[] cm, float sz, float sy)
        {
            var r = new Vector2[cm.Length];
            for (int i = 0; i < cm.Length; i++) r[i] = new Vector2(cm[i].x * sz, cm[i].y * sy);
            return r;
        }

        /// <summary>Üretilen mesh'i sahne kaydında kaybolmasın diye varlık (asset) olarak saklar.</summary>
        static Mesh SaveMesh(Mesh mesh)
        {
            string dir = "Assets/G6/Data/Meshes";
            if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/G6/Data", "Meshes");
            string path = $"{dir}/{mesh.name}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static StaplerSpec LoadOrCreateSpec()
        {
            var spec = AssetDatabase.LoadAssetAtPath<StaplerSpec>(SpecPath);
            if (spec != null) return spec;
            spec = ScriptableObject.CreateInstance<StaplerSpec>();
            AssetDatabase.CreateAsset(spec, SpecPath);
            return spec;
        }

        static Dictionary<string, Material> CreateMaterials(StaplerSpec spec)
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var result = new Dictionary<string, Material>
            {
                ["Hologram"] = GetMat("Hologram", Shader.Find("G6/Hologram"), m => { }),
                ["Glow"] = GetMat("Glow", Shader.Find("G6/Glow"), m => { }),
                ["Masa"] = GetMat("Masa", lit, m => Lit(m, new Color(0.32f, 0.22f, 0.15f), 0f, 0.35f)),
                ["PlastikGovde"] = GetMat("PlastikGovde", lit, m => Lit(m, spec.bodyColor, 0f, 0.6f)),
                ["PlastikKoyu"] = GetMat("PlastikKoyu", lit, m => Lit(m, new Color(0.08f, 0.08f, 0.09f), 0f, 0.45f)),
                ["Metal"] = GetMat("Metal", lit, m => Lit(m, new Color(0.62f, 0.64f, 0.67f), 0.9f, 0.55f)),
                ["MetalParlak"] = GetMat("MetalParlak", lit, m => Lit(m, new Color(0.85f, 0.86f, 0.88f), 1f, 0.8f)),
                ["Siyah"] = GetMat("Siyah", lit, m => Lit(m, new Color(0.05f, 0.05f, 0.06f), 0f, 0.4f)),
                ["PCB"] = GetMat("PCB", lit, m => Lit(m, new Color(0.0f, 0.42f, 0.55f), 0f, 0.55f)),
            };
            AssetDatabase.SaveAssets();
            return result;
        }

        static Material GetMat(string name, Shader shader, System.Action<Material> setup)
        {
            string path = $"{MatDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            setup(mat);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void Lit(Material m, Color c, float metallic, float smoothness)
        {
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smoothness);
        }
    }
}
