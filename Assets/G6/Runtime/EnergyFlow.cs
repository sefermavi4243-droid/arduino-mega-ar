using System;
using System.Collections.Generic;
using UnityEngine;

namespace G6
{
    /// <summary>
    /// Kart üzerinde akımın yolunu gösteren parlak parçacıklar: USB ve adaptörden gelen güç regülatör ve
    /// işlemciye, oradan pinlere akar. Yollar kartın gerçek bileşen konumlarından geçer.
    /// </summary>
    public class EnergyFlow : MonoBehaviour
    {
        [Serializable]
        public struct Path
        {
            public Vector3[] points;   // ikizin yerel uzayında
            public Color color;
            public float delay;        // akışın bu yolda başlama gecikmesi (saniye)
        }

        public Path[] paths = Array.Empty<Path>();
        public Material glowMaterial;
        public float speed = 0.05f;          // m/s
        public float spawnInterval = 0.12f;

        class Spark
        {
            public Transform t;
            public TrailRenderer trail;
            public int path;
            public float distance;
            public bool alive;
        }

        readonly List<Spark> sparks = new();
        float[] lengths;
        float[] spawnTimers;
        float playTime = -1f;
        float duration;

        public bool Playing => playTime >= 0f;

        void Awake()
        {
            lengths = new float[paths.Length];
            spawnTimers = new float[paths.Length];
            for (int i = 0; i < paths.Length; i++)
                for (int j = 1; j < paths[i].points.Length; j++)
                    lengths[i] += Vector3.Distance(paths[i].points[j - 1], paths[i].points[j]);
        }

        /// <summary>Akışı oynat. duration &lt;= 0 ise Stop çağrılana kadar sürer.</summary>
        public void Play(float seconds = 0f)
        {
            playTime = 0f;
            duration = seconds;
        }

        public void Stop() => playTime = -1f;

        void Update()
        {
            if (playTime >= 0f)
            {
                playTime += Time.deltaTime;
                if (duration > 0f && playTime > duration) playTime = -1f;
                else
                    for (int i = 0; i < paths.Length; i++)
                    {
                        if (playTime < paths[i].delay) continue;
                        spawnTimers[i] -= Time.deltaTime;
                        if (spawnTimers[i] <= 0f)
                        {
                            spawnTimers[i] = spawnInterval;
                            Spawn(i);
                        }
                    }
            }

            foreach (var s in sparks)
            {
                if (!s.alive) continue;
                s.distance += speed * Time.deltaTime;
                if (s.distance >= lengths[s.path])
                {
                    s.alive = false;
                    s.t.gameObject.SetActive(false);
                    continue;
                }
                s.t.position = transform.TransformPoint(Sample(paths[s.path].points, s.distance));
                s.t.localScale = Vector3.one * 0.0018f * transform.lossyScale.x;
            }
        }

        void Spawn(int path)
        {
            Spark s = sparks.Find(x => !x.alive);
            if (s == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(go.GetComponent<Collider>());
                go.name = "Akim";
                go.transform.SetParent(transform, true);
                var r = go.GetComponent<Renderer>();
                r.sharedMaterial = new Material(glowMaterial);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var trail = go.AddComponent<TrailRenderer>();
                trail.sharedMaterial = r.sharedMaterial;
                trail.time = 0.25f;
                trail.minVertexDistance = 0.0005f;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                s = new Spark { t = go.transform, trail = trail };
                sparks.Add(s);
            }
            var c = paths[path].color;
            s.t.GetComponent<Renderer>().sharedMaterial.color = c;
            s.trail.startColor = c;
            s.trail.endColor = new Color(c.r, c.g, c.b, 0f);
            s.trail.startWidth = 0.0014f;
            s.trail.endWidth = 0f;
            s.path = path;
            s.distance = 0f;
            s.alive = true;
            s.t.position = transform.TransformPoint(paths[path].points[0]);
            s.trail.Clear();
            s.t.gameObject.SetActive(true);
        }

        static Vector3 Sample(Vector3[] pts, float d)
        {
            for (int i = 1; i < pts.Length; i++)
            {
                float seg = Vector3.Distance(pts[i - 1], pts[i]);
                if (d <= seg) return Vector3.Lerp(pts[i - 1], pts[i], seg > 0f ? d / seg : 0f);
                d -= seg;
            }
            return pts[pts.Length - 1];
        }
    }
}
