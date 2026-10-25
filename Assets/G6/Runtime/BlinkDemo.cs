using UnityEngine;

namespace G6
{
    /// <summary>
    /// Gerçek kartın 13 ve GND pinlerine "takılı" sanal LED. Arduino'nun ilk örneği Blink'i canlandırır:
    /// LED ve kart üzerindeki L LED'i bir saniye yanıp bir saniye söner; arayüz o an çalışan kod satırını vurgular.
    /// Bacak aralığı 2,54 mm olduğu için LED tam olarak yan yana iki pine oturur.
    /// </summary>
    public class BlinkDemo : MonoBehaviour
    {
        public Vector3 pin13;        // ikizin yerel uzayında, başlığın tepesi
        public Vector3 pinGnd;
        public Material glowMaterial;
        public Material ledBodyMaterial;
        public string onboardLedPartId = "led";
        [Tooltip("İşlemciden 13. pine giden akım (yalnızca bu yol)")]
        public EnergyFlow flow;

        public static readonly string[] CodeLines =
        {
            "void loop() {",
            "  digitalWrite(13, HIGH);",
            "  delay(1000);",
            "  digitalWrite(13, LOW);",
            "  delay(1000);",
            "}",
        };

        public bool Running { get; private set; }
        /// <summary>Şu an çalışan satırın CodeLines içindeki indeksi.</summary>
        public int CurrentLine { get; private set; } = -1;
        public bool LedOn { get; private set; }

        GameObject root;
        Material glowInstance;
        Material haloMaterial;
        Transform halo;
        PartView onboardLed;
        float t;
        float appear;

        void Start()
        {
            foreach (var p in GetComponentsInChildren<PartView>())
                if (p.partId == onboardLedPartId) onboardLed = p;
            Build();
            root.SetActive(false);
        }

        void Build()
        {
            root = new GameObject("SanalLED");
            root.transform.SetParent(transform, false);
            Vector3 mid = (pin13 + pinGnd) / 2f;
            root.transform.localPosition = mid;

            // Bacaklar: başlığın içinden yukarı çıkan iki ince tel.
            foreach (var p in new[] { pin13 - mid, pinGnd - mid })
            {
                var leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(leg.GetComponent<Collider>());
                leg.transform.SetParent(root.transform, false);
                leg.transform.localPosition = p + Vector3.up * 0.007f;
                leg.transform.localScale = new Vector3(0.0005f, 0.007f, 0.0005f);
                leg.GetComponent<Renderer>().sharedMaterial = ledBodyMaterial;
            }

            // Gövde (5 mm LED): silindir + kubbe.
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.up * 0.0165f;
            body.transform.localScale = new Vector3(0.005f, 0.0025f, 0.005f);
            var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(dome.GetComponent<Collider>());
            dome.transform.SetParent(root.transform, false);
            dome.transform.localPosition = Vector3.up * 0.019f;
            dome.transform.localScale = Vector3.one * 0.005f;

            glowInstance = new Material(glowMaterial);
            body.GetComponent<Renderer>().sharedMaterial = glowInstance;
            dome.GetComponent<Renderer>().sharedMaterial = glowInstance;

            // Yanınca etrafa yayılan ışık halesi.
            var haloGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(haloGo.GetComponent<Collider>());
            haloGo.transform.SetParent(root.transform, false);
            haloGo.transform.localPosition = Vector3.up * 0.018f;
            haloMaterial = new Material(glowMaterial) { color = new Color(1f, 0.2f, 0.1f, 0.18f) };
            haloGo.GetComponent<Renderer>().sharedMaterial = haloMaterial;
            halo = haloGo.transform;
        }

        public void SetRunning(bool on)
        {
            Running = on;
            root.SetActive(on);
            t = 0f;
            appear = 0f;
            CurrentLine = on ? 1 : -1;
            if (!on && onboardLed != null) onboardLed.SetHighlighted(false);
        }

        void Update()
        {
            if (!Running) return;
            appear = Mathf.MoveTowards(appear, 1f, Time.deltaTime * 2.5f);
            root.transform.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, appear);

            // 2 saniyelik döngü: HIGH satırı, delay, LOW satırı, delay.
            t += Time.deltaTime;
            float c = t % 2f;
            int line = c < 0.15f ? 1 : c < 1f ? 2 : c < 1.15f ? 3 : 4;
            bool on = c < 1f;
            if (on && !LedOn && flow != null) flow.Play(0.6f); // HIGH: işlemciden pine akım
            LedOn = on;
            CurrentLine = line;

            var red = new Color(1f, 0.15f, 0.1f);
            glowInstance.color = on ? red : new Color(0.35f, 0.05f, 0.05f, 0.6f);
            halo.localScale = Vector3.one * (on ? 0.016f + Mathf.Sin(Time.time * 20f) * 0.0006f : 0.0001f);
            if (onboardLed != null) onboardLed.Flash(on ? new Color(1f, 0.85f, 0.2f) : onboardLed.color, on ? 0.05f : 0f);
        }
    }
}
