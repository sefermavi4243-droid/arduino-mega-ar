using UnityEngine;

namespace G6
{
    /// <summary>
    /// Dijital ikizdeki tek bir parça: bilgi metinleri, etiket noktası, patlatılmış görünümdeki
    /// konumu ve hologram malzemesi.
    /// </summary>
    public class PartView : MonoBehaviour
    {
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int AlphaId = Shader.PropertyToID("_Alpha");
        static readonly int RevealId = Shader.PropertyToID("_Reveal");
        static readonly int MinYId = Shader.PropertyToID("_RevealMinY");
        static readonly int MaxYId = Shader.PropertyToID("_RevealMaxY");
        static readonly int HighlightId = Shader.PropertyToID("_Highlight");

        [Header("Bilgi")]
        public string partId;
        public string title;
        [TextArea(2, 5)] public string description;
        [TextArea(2, 4)] public string funFact;
        public Color color = new Color(0.25f, 0.9f, 1f);
        [Tooltip("Ana bölge mi? Ana bölgelerin etiketi baştan görünür, diğerleri seçilince.")]
        public bool primary = true;

        [Header("Yerleşim (ikizin yerel uzayında)")]
        [Tooltip("Patlatılmış görünümde parçanın kayacağı miktar (metre).")]
        public Vector3 explodeOffset;
        [Tooltip("Etiket çizgisinin başladığı nokta (parçanın yerel uzayında).")]
        public Vector3 labelPoint;
        [Tooltip("Etiketin nesneden uzaklaşacağı yön (ikizin yerel uzayında).")]
        public Vector3 labelDirection = Vector3.up;

        Vector3 restPosition;
        Vector3 velocity;
        float explodeTarget;
        float smoothTime;
        float highlight;
        float highlightTarget;
        Color flashColor;
        float flashUntil;
        Material[] materials;

        public Vector3 LabelAnchorWorld => transform.TransformPoint(labelPoint);

        public Vector3 LabelDirectionWorld =>
            (transform.parent != null ? transform.parent.TransformDirection(labelDirection) : labelDirection).normalized;

        void Awake()
        {
            restPosition = transform.localPosition;
            // Parçalar farklı hızlarda açılsın: patlama tek blok değil, dalga gibi görünür.
            smoothTime = 0.22f + transform.GetSiblingIndex() * 0.035f;

            var renderers = GetComponentsInChildren<Renderer>();
            materials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                materials[i] = renderers[i].material; // örnek kopya: her parça kendi rengini taşır
        }

        public void SetExploded(bool exploded) => explodeTarget = exploded ? 1f : 0f;

        public void SetHighlighted(bool on) => highlightTarget = on ? 1f : 0f;

        /// <summary>Quiz geri bildirimi: parçayı kısa süre başka renkte yakıp söndürür.</summary>
        public void Flash(Color c, float duration = 0.8f)
        {
            flashColor = c;
            flashUntil = Time.time + duration;
        }

        void Update()
        {
            Vector3 target = restPosition + explodeOffset * explodeTarget;
            transform.localPosition = Vector3.SmoothDamp(transform.localPosition, target, ref velocity, smoothTime);
            highlight = Mathf.MoveTowards(highlight, highlightTarget, Time.deltaTime * 4f);
        }

        /// <summary>DigitalTwin her karede ortak görünüm değerlerini buradan iletir.</summary>
        public void ApplyVisual(float alpha, float reveal, float minY, float maxY)
        {
            bool flashing = Time.time < flashUntil;
            Color c = flashing ? flashColor : color;
            float h = flashing ? 1f : highlight;
            foreach (var m in materials)
            {
                m.SetColor(ColorId, c);
                m.SetFloat(AlphaId, flashing ? Mathf.Max(alpha, 0.5f) : alpha);
                m.SetFloat(RevealId, reveal);
                m.SetFloat(MinYId, minY);
                m.SetFloat(MaxYId, maxY);
                m.SetFloat(HighlightId, h);
            }
        }
    }
}
