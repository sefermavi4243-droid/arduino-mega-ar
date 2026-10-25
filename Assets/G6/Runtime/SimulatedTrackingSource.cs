using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace G6
{
    /// <summary>
    /// Cihaz ve Vuforia olmadan sınıfta deneme yapmak için sahte takip kaynağı.
    /// Masadaki "gerçek" zımbayı (SimRealObject) gösterir, kamerayı nesnenin etrafında döndürür
    /// ve nesne kadrajdan çıkınca takip kaybı üretir.
    /// Kısayollar: F = bul/kaybet, sürükle = etrafında dön, tekerlek = yakınlaş.
    /// </summary>
    public class SimulatedTrackingSource : TrackingSource
    {
        [SerializeField] float autoFindDelay = 1.8f;
        [SerializeField] float distance = 0.32f;
        [SerializeField] Vector2 minMaxDistance = new Vector2(0.18f, 0.9f);
        [SerializeField] float yaw = 145f; // önden-soldan bakış
        [SerializeField] float pitch = 28f;

        public override bool IsSimulation => true;

        Camera cam;
        bool manualLost;
        bool dragging;
        Vector2 pressPos;
        bool pressStartedOverUI;
        bool autoFindPending = true;
        float startTime;

        void Start()
        {
            cam = Camera.main;
            startTime = Time.time;
            manualLost = true;
        }

        /// <summary>Kamerayı verilen açıya taşır (sunum videosu kaydı için).</summary>
        public void SetView(float newYaw, float newPitch, float newDistance)
        {
            yaw = newYaw;
            pitch = newPitch;
            distance = newDistance;
        }

        public float Yaw => yaw;
        public float Pitch => pitch;
        public float Distance => distance;

        /// <summary>UI'daki simülasyon düğmesi: takibi el ile aç/kapat.</summary>
        public void ToggleManual()
        {
            autoFindPending = false;
            manualLost = !manualLost;
        }

        void Update()
        {
            if (autoFindPending && Time.time - startTime > autoFindDelay)
            {
                autoFindPending = false;
                manualLost = false;
            }

            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                ToggleManual();

            HandleOrbitInput();
            PlaceCamera();

            // Nesne kadrajın içindeyse "takip ediliyor" say: gerçek takipteki gibi kadraj dışı = kayıp.
            Vector3 vp = cam.WorldToViewportPoint(transform.position + Vector3.up * 0.03f);
            bool inView = vp.z > 0f && vp.x > 0.05f && vp.x < 0.95f && vp.y > 0.05f && vp.y < 0.95f;
            SetTracked(!manualLost && inView);
        }

        void HandleOrbitInput()
        {
            var pointer = Pointer.current;
            if (pointer == null) return;

            if (pointer.press.wasPressedThisFrame)
            {
                pressPos = pointer.position.ReadValue();
                pressStartedOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                dragging = false;
            }

            if (pointer.press.isPressed && !pressStartedOverUI)
            {
                Vector2 pos = pointer.position.ReadValue();
                if (!dragging && (pos - pressPos).sqrMagnitude > 15f * 15f) dragging = true;
                if (dragging)
                {
                    Vector2 delta = pointer.delta.ReadValue();
                    yaw += delta.x * 0.25f;
                    pitch = Mathf.Clamp(pitch - delta.y * 0.2f, 5f, 80f);
                }
            }

            if (Mouse.current != null)
            {
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                    distance = Mathf.Clamp(distance * (scroll > 0 ? 0.92f : 1.08f), minMaxDistance.x, minMaxDistance.y);
            }
        }

        void PlaceCamera()
        {
            // Elde tutulan telefon hissi için çok hafif bir sallantı.
            float t = Time.time;
            float sway = Mathf.Sin(t * 0.7f) * 0.6f;
            Quaternion rot = Quaternion.Euler(pitch + Mathf.Sin(t * 0.9f) * 0.4f, yaw + sway, 0f);
            Vector3 target = transform.position + Vector3.up * 0.03f;
            cam.transform.SetPositionAndRotation(target - rot * Vector3.forward * distance, rot);
        }
    }
}
