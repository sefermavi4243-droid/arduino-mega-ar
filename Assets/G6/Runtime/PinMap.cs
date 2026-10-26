using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace G6
{
    /// <summary>
    /// Kartın her pininin adını tam pinin üstüne, kart yüzeyine paralel ve dışarı doğru okunacak şekilde yazar.
    /// Konumlar kartın açık kaynak tasarım dosyasından (.brd) alınır, bu yüzden gerçek pinlerle milimetre düzeyinde örtüşür.
    /// Etiketler ilgili başlık parçasının (PartView) çocuğudur; patlatılmış görünümde başlıkla birlikte hareket eder.
    /// </summary>
    public class PinMap : MonoBehaviour
    {
        [Serializable]
        public struct Pin
        {
            public string name;
            public string partId;
            public Vector3 position;   // ikizin yerel uzayında, pinin tepesi
            public Vector3 outward;    // yazının uzanacağı yön (yatay)
            public Color color;
        }

        public Pin[] pins = Array.Empty<Pin>();
        public Material glowMaterial;
        [Tooltip("Yazı yüksekliği (metre). Pin aralığı 2,54 mm.")]
        public float textHeight = 0.0016f;

        readonly List<(TextMeshPro text, Renderer dot, Material dotMat, string partId, Color color)> items = new();
        bool showAll;
        string showPart;
        float alpha;

        public bool ShowAll => showAll;

        void Start()
        {
            var parts = new Dictionary<string, Transform>();
            foreach (var p in GetComponentsInChildren<PartView>()) parts[p.partId] = p.transform;

            // Tüm pin yazıları aynı ölçekle çizilir: harf boyu gerçek dünyada 1,6 mm (pin aralığı 2,54 mm).
            float scale = MeasureScale();
            foreach (var pin in pins)
            {
                // Pin yazısı ait olduğu başlığın (ör. "dijital") çocuğu olur; parça patlatılınca yazılar da onunla gider.
                Transform parent = parts.TryGetValue(pin.partId, out var t) ? t : transform;

                var go = new GameObject("Pin_" + pin.name);
                go.transform.SetParent(parent, false);
                Vector3 local = parent.InverseTransformPoint(transform.TransformPoint(pin.position));
                go.transform.localPosition = local + parent.InverseTransformVector(transform.TransformVector(pin.outward * 0.0012f + Vector3.up * 0.0004f));
                // Yazı kart yüzeyine paralel (yukarı bakar), okuma yönü dışarı doğru.
                float yaw = Mathf.Atan2(-pin.outward.z, pin.outward.x) * Mathf.Rad2Deg;
                go.transform.localRotation = Quaternion.Euler(90f, yaw, 0f);
                go.transform.localScale = Vector3.one * scale;

                var text = go.AddComponent<TextMeshPro>();
                text.text = pin.name;
                text.fontSize = 10f;
                text.fontStyle = FontStyles.Bold;
                text.alignment = TextAlignmentOptions.MidlineLeft;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.rectTransform.pivot = new Vector2(0f, 0.5f);
                text.rectTransform.sizeDelta = new Vector2(60f, 12f);
                text.outlineWidth = 0.25f;
                text.outlineColor = new Color32(0, 0, 0, 255);
                text.color = new Color(pin.color.r, pin.color.g, pin.color.b, 0f);

                // Pinin tam üstünde küçük parlak nokta.
                var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(dot.GetComponent<Collider>());
                dot.name = "Nokta";
                dot.transform.SetParent(parent, false);
                dot.transform.localPosition = local;
                dot.transform.localScale = Vector3.one * 0.0011f;
                var r = dot.GetComponent<Renderer>();
                var m = new Material(glowMaterial);
                r.sharedMaterial = m;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                items.Add((text, r, m, pin.partId, pin.color));
            }
            Apply(0f);
        }

        /// <summary>TextMeshPro'nun dünya birimindeki yazı boyunu ölçüp istenen yüksekliğe ölçekler.</summary>
        float MeasureScale()
        {
            var probe = new GameObject("olcum").AddComponent<TextMeshPro>();
            probe.fontSize = 10f;
            probe.text = "D8";
            probe.ForceMeshUpdate();
            float h = Mathf.Max(0.0001f, probe.textBounds.size.y);
            Destroy(probe.gameObject);
            return textHeight / h;
        }

        public void SetShowAll(bool on) => showAll = on;

        /// <summary>Yalnızca seçili başlığın pinlerini göster (null: hiçbiri).</summary>
        public void ShowPart(string partId) => showPart = partId;

        public bool HasPinsFor(string partId)
        {
            foreach (var p in pins) if (p.partId == partId) return true;
            return false;
        }

        public void SetVisible(bool on) => alphaTarget = on ? 1f : 0f;
        float alphaTarget = 1f;

        void Update()
        {
            alpha = Mathf.MoveTowards(alpha, alphaTarget, Time.deltaTime * 3f);
            Apply(alpha);
        }

        void Apply(float a)
        {
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * 4f);
            foreach (var it in items)
            {
                bool on = showAll || it.partId == showPart;
                float ia = on ? a : 0f;
                var c = it.color;
                it.text.color = new Color(c.r, c.g, c.b, ia);
                it.text.enabled = ia > 0.01f;
                it.dot.enabled = ia > 0.01f;
                it.dotMat.color = new Color(c.r, c.g, c.b, ia * pulse);
            }
        }
    }
}
