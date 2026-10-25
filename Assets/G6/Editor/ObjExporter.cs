using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace G6.EditorTools
{
    /// <summary>
    /// Dijital ikizi Vuforia araçlarının okuyabileceği 3B dosyalara yazar ve Model Target CLI için
    /// JSON yapılandırması üretir. Birim metredir.
    /// Unity sol el, OBJ/Collada/glTF sağ el koordinat sistemi kullanır: X ekseni çevrilir, üçgen sırası ters çevrilir.
    /// </summary>
    public static class ObjExporter
    {
        public const string ModelTargetDir = "ModelTarget";
        public const string ConfigFile = "model_target.json"; // Tools/ModelTargetUret.ps1 bu dosyayı okur

        // Türkçe bölge ayarında ondalık ayırıcı virgüldür; dosya biçimleri nokta ister.
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        class MeshPart
        {
            public string Name;
            public List<Vector3> Positions = new();
            public List<Vector3> Normals = new();
            public List<int> Triangles = new();
        }

        /// <summary>İkizdeki tüm parçaları ikizin yerel uzayında, sağ el koordinatlarında toplar.</summary>
        static List<MeshPart> Collect(out DigitalTwin twin)
        {
            twin = Object.FindFirstObjectByType<DigitalTwin>();
            var parts = new List<MeshPart>();
            if (twin == null)
            {
                Debug.LogError("[G6] Sahnede DigitalTwin yok. Önce G6 > Sahneyi Kur.");
                return parts;
            }

            Matrix4x4 toTwin = twin.transform.worldToLocalMatrix;
            foreach (var mf in twin.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = mf.sharedMesh;
                Matrix4x4 m = toTwin * mf.transform.localToWorldMatrix;
                Matrix4x4 n = m.inverse.transpose;
                var part = new MeshPart { Name = mf.name };
                foreach (var v in mesh.vertices)
                {
                    var p = m.MultiplyPoint3x4(v);
                    part.Positions.Add(new Vector3(-p.x, p.y, p.z));
                }
                foreach (var vn in mesh.normals)
                {
                    var d = n.MultiplyVector(vn).normalized;
                    part.Normals.Add(new Vector3(-d.x, d.y, d.z));
                }
                var tris = mesh.triangles;
                for (int i = 0; i < tris.Length; i += 3)
                {
                    part.Triangles.Add(tris[i]);
                    part.Triangles.Add(tris[i + 2]);
                    part.Triangles.Add(tris[i + 1]);
                }
                parts.Add(part);
            }
            return parts;
        }

        [MenuItem("G6/Model Target için OBJ dışa aktar", priority = 20)]
        public static void ExportObjMenu()
        {
            string path = EditorUtility.SaveFilePanel("Zımba modelini kaydet", "", "zimba.obj", "obj");
            if (!string.IsNullOrEmpty(path)) ExportObj(path);
        }

        public static void ExportObj(string path)
        {
            var parts = Collect(out _);
            if (parts.Count == 0) return;
            var sb = new StringBuilder();
            sb.AppendLine("# G6 Nesne Takibi - zimba dijital ikizi (birim: metre)");
            int offset = 1;
            foreach (var part in parts)
            {
                sb.AppendLine("o " + part.Name);
                foreach (var p in part.Positions) sb.AppendLine(string.Format(Inv, "v {0:F6} {1:F6} {2:F6}", p.x, p.y, p.z));
                foreach (var d in part.Normals) sb.AppendLine(string.Format(Inv, "vn {0:F6} {1:F6} {2:F6}", d.x, d.y, d.z));
                for (int i = 0; i < part.Triangles.Count; i += 3)
                {
                    int a = part.Triangles[i] + offset, b = part.Triangles[i + 1] + offset, c = part.Triangles[i + 2] + offset;
                    sb.AppendLine($"f {a}//{a} {b}//{b} {c}//{c}");
                }
                offset += part.Positions.Count;
            }
            File.WriteAllText(path, sb.ToString());
            Debug.Log("[G6] OBJ yazıldı: " + path);
        }

        /// <summary>
        /// Collada (.dae) birim bilgisini dosyada taşır (OBJ taşımaz). Vuforia, birimsiz biçimlerde
        /// kılavuz görünümün yanlış ölçeklenebileceği konusunda uyarır; bu yüzden Model Target için DAE kullanılır.
        /// </summary>
        public static void ExportDae(string path, Color color)
        {
            var parts = Collect(out _);
            if (parts.Count == 0) return;
            string now = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", Inv);
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<COLLADA xmlns=\"http://www.collada.org/2005/11/COLLADASchema\" version=\"1.4.1\">");
            sb.AppendLine($"<asset><contributor><authoring_tool>G6 Unity</authoring_tool></contributor><created>{now}</created><modified>{now}</modified><unit name=\"meter\" meter=\"1\"/><up_axis>Y_UP</up_axis></asset>");
            sb.AppendLine(string.Format(Inv,
                "<library_effects><effect id=\"fx\"><profile_COMMON><technique sid=\"common\"><lambert><diffuse><color>{0:F3} {1:F3} {2:F3} 1</color></diffuse></lambert></technique></profile_COMMON></effect></library_effects>",
                color.r, color.g, color.b));
            sb.AppendLine("<library_materials><material id=\"mat\" name=\"mat\"><instance_effect url=\"#fx\"/></material></library_materials>");

            sb.AppendLine("<library_geometries>");
            for (int g = 0; g < parts.Count; g++)
            {
                var part = parts[g];
                string id = "g" + g;
                sb.AppendLine($"<geometry id=\"{id}\" name=\"{part.Name}\"><mesh>");
                WriteSource(sb, id + "-pos", part.Positions);
                WriteSource(sb, id + "-nrm", part.Normals);
                sb.AppendLine($"<vertices id=\"{id}-vtx\"><input semantic=\"POSITION\" source=\"#{id}-pos\"/></vertices>");
                sb.Append($"<triangles material=\"mat\" count=\"{part.Triangles.Count / 3}\"><input semantic=\"VERTEX\" source=\"#{id}-vtx\" offset=\"0\"/><input semantic=\"NORMAL\" source=\"#{id}-nrm\" offset=\"1\"/><p>");
                foreach (int t in part.Triangles) sb.Append(t).Append(' ').Append(t).Append(' ');
                sb.AppendLine("</p></triangles>");
                sb.AppendLine("</mesh></geometry>");
            }
            sb.AppendLine("</library_geometries>");

            sb.AppendLine("<library_visual_scenes><visual_scene id=\"Scene\" name=\"Scene\">");
            for (int g = 0; g < parts.Count; g++)
                sb.AppendLine($"<node id=\"n{g}\" name=\"{parts[g].Name}\"><instance_geometry url=\"#g{g}\"><bind_material><technique_common><instance_material symbol=\"mat\" target=\"#mat\"/></technique_common></bind_material></instance_geometry></node>");
            sb.AppendLine("</visual_scene></library_visual_scenes>");
            sb.AppendLine("<scene><instance_visual_scene url=\"#Scene\"/></scene>");
            sb.AppendLine("</COLLADA>");
            File.WriteAllText(path, sb.ToString());
            Debug.Log("[G6] DAE yazıldı: " + path);
        }

        static void WriteSource(StringBuilder sb, string id, List<Vector3> data)
        {
            sb.Append($"<source id=\"{id}\"><float_array id=\"{id}-arr\" count=\"{data.Count * 3}\">");
            foreach (var v in data) sb.Append(string.Format(Inv, "{0:F6} {1:F6} {2:F6} ", v.x, v.y, v.z));
            sb.AppendLine($"</float_array><technique_common><accessor source=\"#{id}-arr\" count=\"{data.Count}\" stride=\"3\"><param name=\"X\" type=\"float\"/><param name=\"Y\" type=\"float\"/><param name=\"Z\" type=\"float\"/></accessor></technique_common></source>");
        }

        /// <summary>
        /// ModelTarget/ klasörüne nesnenin DAE modelini ve Model Target CLI yapılandırmasını (model_target.json) yazar.
        /// </summary>
        [MenuItem("G6/Vuforia/Model Target dosyalarını hazırla (DAE + JSON)", priority = 10)]
        public static void PrepareModelTarget()
        {
            Directory.CreateDirectory(ModelTargetDir);
            var profile = SceneBuilder.CurrentProfile();
            var GuideViews = profile.GuideViews;
            string dae = Path.GetFullPath(Path.Combine(ModelTargetDir, profile.FileStem + ".dae"));
            ExportDae(dae, profile.BodyColor);

            var twin = Object.FindFirstObjectByType<DigitalTwin>();
            var b = twin.WorldBounds();
            Vector3 center = twin.transform.InverseTransformPoint(b.center);
            float dist = Mathf.Max(b.size.x, b.size.y, b.size.z) * 2.6f;

            var views = new StringBuilder();
            for (int i = 0; i < GuideViews.Length; i++)
            {
                Vector3 camU = center + GuideViews[i].dir.normalized * dist;

                // Sağ el (glTF) uzayına çevir; Vuforia kamerası -Z yönüne bakar.
                Vector3 c = new Vector3(-center.x, center.y, center.z);
                Vector3 p = new Vector3(-camU.x, camU.y, camU.z);
                Vector3 zAxis = (p - c).normalized; // kamera -Z ile hedefe baktığı için +Z hedeften kameraya
                Quaternion q = Quaternion.LookRotation(zAxis, Vector3.up); // sütunlar: X=up×Z, Y=Z×X, Z=z (saf matematik)

                views.Append(string.Format(Inv, @"        {{
          ""name"": ""{0}"",
          ""guideViewPosition"": {{
            ""translation"": [{1:F5}, {2:F5}, {3:F5}],
            ""rotation"": [{4:F6}, {5:F6}, {6:F6}, {7:F6}]
          }}
        }}{8}
", GuideViews[i].name, p.x, p.y, p.z, q.x, q.y, q.z, q.w, i < GuideViews.Length - 1 ? "," : ""));
            }

            string json = string.Format(Inv, @"{{
  ""name"": ""{0}"",
  ""models"": [
    {{
      ""name"": ""{1}"",
      ""cadDataUrl"": ""{2}"",
      ""optimizeTrackingFor"": ""default"",
      ""automaticColoring"": ""auto"",
      ""upVector"": [0.0, 1.0, 0.0],
      ""views"": [
{3}      ]
    }}
  ]
}}
", profile.DatasetName, profile.TargetName, dae.Replace('\\', '/'), views);
            File.WriteAllText(Path.Combine(ModelTargetDir, ConfigFile), json);
            Debug.Log("[G6] Model Target yapılandırması yazıldı: " + Path.GetFullPath(ModelTargetDir));
        }
    }
}
