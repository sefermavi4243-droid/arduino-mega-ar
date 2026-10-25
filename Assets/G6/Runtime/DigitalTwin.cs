using UnityEngine;

namespace G6
{
    /// <summary>
    /// Gerçek nesnenin üstüne oturan hologram kopya. Tarama ile ortaya çıkma, röntgen (saydamlık),
    /// patlatılmış görünüm ve parça seçimi burada yönetilir.
    /// </summary>
    public class DigitalTwin : MonoBehaviour
    {
        [SerializeField] float idleAlpha = 0.12f;
        [SerializeField] float xRayAlpha = 0.45f;
        [SerializeField] float revealDuration = 1.4f;

        public PartView[] Parts { get; private set; }
        public bool IsRevealed => visible && reveal >= 1f;
        public bool Exploded { get; private set; }
        public bool XRay { get; private set; }

        bool visible;
        float reveal;
        float alpha;
        PartView selected;
        Renderer[] renderers;

        void Awake()
        {
            Parts = GetComponentsInChildren<PartView>();
            renderers = GetComponentsInChildren<Renderer>();
        }

        public void Show()
        {
            visible = true;
            reveal = 0f;
        }

        public void Hide()
        {
            visible = false;
        }

        public void SetExploded(bool on)
        {
            Exploded = on;
            foreach (var p in Parts) p.SetExploded(on);
        }

        public void SetXRay(bool on) => XRay = on;

        public void Select(PartView part)
        {
            selected = part;
            foreach (var p in Parts) p.SetHighlighted(p == part);
        }

        public PartView Selected => selected;

        void LateUpdate()
        {
            // Patlatılmış görünümde parçalar gerçek nesneden ayrılır; okunabilsin diye daha görünür yap.
            float targetAlpha = !visible ? 0f : (XRay || Exploded ? xRayAlpha : idleAlpha);
            alpha = Mathf.MoveTowards(alpha, targetAlpha, Time.deltaTime * 1.5f);
            if (visible) reveal = Mathf.MoveTowards(reveal, 1f, Time.deltaTime / revealDuration);

            var b = WorldBounds();
            float minY = b.min.y - 0.002f;
            float maxY = b.max.y + 0.002f;
            // Tarama sırasında model tam görünür olsun ki parlak tarama çizgisi fark edilsin.
            float a = reveal < 1f ? Mathf.Max(alpha, 0.35f) : alpha;

            foreach (var p in Parts)
            {
                float partAlpha = a;
                if (selected != null && p != selected) partAlpha *= 0.35f;
                if (selected != null && p == selected) partAlpha = Mathf.Max(partAlpha, 0.4f);
                if (!visible) partAlpha = alpha;
                p.ApplyVisual(partAlpha, visible ? reveal : 1f, minY, maxY);
            }
        }

        public Bounds WorldBounds()
        {
            if (renderers == null) renderers = GetComponentsInChildren<Renderer>(); // editörde Awake çalışmaz
            var b = new Bounds(transform.position, Vector3.zero);
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
