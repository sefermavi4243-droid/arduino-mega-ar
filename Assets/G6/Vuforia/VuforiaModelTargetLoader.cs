using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Vuforia;

namespace G6
{
    /// <summary>
    /// Vuforia başladığında hedefleri koddan oluşturur ve dijital ikizi onlara bağlar:
    /// 1) Image Target: kartın üstten çekilmiş fotoğrafı. Kamera kartı gördüğü anda, açıdan bağımsız tanır.
    /// 2) Model Target: kartın 3B modeli (Model Target CLI ile üretilen veritabanı). Silüet gösterilmez;
    ///    kılavuz açıları arka planda sırayla denenir.
    /// Hangisi önce bulursa hologram ona oturur (VuforiaTrackingSource).
    /// </summary>
    public class VuforiaModelTargetLoader : MonoBehaviour
    {
        [Header("Model Target")]
        [Tooltip("StreamingAssets'e göre veritabanı yolu")]
        [SerializeField] string dataSetPath = "Vuforia/G6_ArduinoMega.xml";
        [SerializeField] string targetName = "ArduinoMega";
        [Tooltip("Kılavuz açısı adları (Model Target JSON'undaki views ile aynı)")]
        [SerializeField] string[] guideViews = { };
        [SerializeField] string[] guideViewTitles = { };
        [Tooltip("Nesne bulunamazsa kaç saniyede bir sonraki kılavuz açısı denensin")]
        [SerializeField] float cycleSeconds = 3f;

        [Header("Image Target (üstten fotoğraf)")]
        [Tooltip("StreamingAssets'e göre fotoğraf yolu; boşsa veya dosya yoksa yalnızca Model Target kullanılır")]
        [SerializeField] string imagePath = "Vuforia/arduino_ust.jpg";
        [Tooltip("Fotoğraftaki nesnenin gerçek genişliği (metre). Arduino Mega: 0.1016")]
        [SerializeField] float imageWidth = 0.1016f;
        [Tooltip("İkizin fotoğraf düzlemine göre konumu (metre). Fotoğraf kartın üst yüzeyindedir.")]
        [SerializeField] Vector3 imageOffset = new Vector3(0f, -0.0016f, 0f);

        [Header("Bağlantılar")]
        [SerializeField] DigitalTwin twin;
        [SerializeField] AppController app;

        ModelTargetBehaviour modelTarget;
        VuforiaTrackingSource source;

        // Vuforia aynı anda yalnızca bir Model Target'ı etkin tutabilir. Sahnede birden fazla Model Target varsa
        // (ekibin kitap ve dinozor hedefleri) hiçbiri tanınmazken sırayla etkinleştirilir; tanınan açık kalır.
        readonly List<ModelTargetBehaviour> rota = new();
        readonly Dictionary<ModelTargetBehaviour, Status> statuses = new();
        readonly Dictionary<ModelTargetBehaviour, StatusInfo> infos = new();
        int rotaIndex;
        bool otherTracked;
        int viewIndex;
        float timer;

        void Start()
        {
            // Vuforia kamerayı açıp hazır olduğunda OnStarted çalışsın. Hedefler ancak bundan sonra kurulabilir.
            VuforiaApplication.Instance.OnVuforiaStarted += OnStarted;
        }

        void OnDestroy()
        {
            if (VuforiaApplication.Instance != null)
                VuforiaApplication.Instance.OnVuforiaStarted -= OnStarted;
        }

        void OnStarted()
        {
            VuforiaApplication.Instance.OnVuforiaStarted -= OnStarted;

            // ADIM 1 — Çapa: boş bir nesne. Vuforia kartı her gördüğünde bu çapayı kartın konumuna taşır.
            // Hologram (twin) çapanın çocuğu yapılır; böylece çapa nereye giderse hologram ve etiketler de oraya gider.
            var anchor = new GameObject("TakipCapasi");
            source = anchor.AddComponent<VuforiaTrackingSource>();
            twin.transform.SetParent(anchor.transform, false);
            twin.transform.localPosition = Vector3.zero;
            twin.transform.localRotation = Quaternion.identity;

            // ADIM 2 — Ekip hedefleri (kitap, dinozor) sahnede hazır durur. Vuforia aynı anda tek Model Target
            // açık tutabildiği için önce hepsi kapatılır; yoksa ikinci hedef "Failed to activate observer" verir.
            var sceneTargets = FindObjectsByType<ModelTargetBehaviour>(FindObjectsSortMode.None);
            foreach (var mt in sceneTargets) mt.enabled = false;

            // ADIM 3 — Arduino'nun Model Target'ı: StreamingAssets/Vuforia/G6_ArduinoMega.xml veritabanından,
            // Model Target CLI ile kartın 3B modelinden üretilmiş hedef. Silüet gösterilmez (NoGuideView).
            modelTarget = VuforiaBehaviour.Instance.ObserverFactory.CreateModelTarget(dataSetPath, targetName);
            if (modelTarget != null)
            {
                modelTarget.GuideViewMode = ModelTargetBehaviour.GuideViewDisplayMode.NoGuideView;
                source.Add(modelTarget, Vector3.zero, Quaternion.identity);
                AddToRota(modelTarget);
                Debug.Log("[G6] Model Target hazır: " + targetName);
            }
            else Debug.LogWarning($"[G6] Model Target oluşturulamadı: {dataSetPath} / {targetName}");

            // ADIM 4 — Kitap ve dinozor hedefleri sıraya eklenir; Update() onları sırayla açıp kapatır.
            foreach (var mt in sceneTargets)
            {
                AddToRota(mt);
                Debug.Log("[G6] Ek Model Target sıraya alındı: " + mt.TargetName);
            }
            if (modelTarget == null && rota.Count > 0) rota[0].enabled = true;

            // ADIM 5 — Uygulamaya "takip bilgisini bu kaynaktan al" denir, sonra fotoğraf hedefi kurulur.
            app.UseSource(source);
            if (!string.IsNullOrEmpty(imagePath)) StartCoroutine(CreateImageTarget());
        }

        /// <summary>Hedefi sıraya ekler ve durumunu (bulundu / kayıp / hazırlanıyor) izlemeye başlar.</summary>
        void AddToRota(ModelTargetBehaviour mt)
        {
            rota.Add(mt);
            statuses[mt] = Status.NO_POSE;
            infos[mt] = StatusInfo.NORMAL;
            mt.OnTargetStatusChanged += (b, s) =>
            {
                statuses[mt] = s.Status;
                infos[mt] = s.StatusInfo;
            };
        }

        /// <summary>Fotoğrafı StreamingAssets'ten okur (Android'de APK içinde olduğu için web isteğiyle).</summary>
        IEnumerator CreateImageTarget()
        {
            string url = System.IO.Path.Combine(Application.streamingAssetsPath, imagePath);
            if (!url.Contains("://")) url = "file://" + url;
            using var req = UnityWebRequestTexture.GetTexture(url);
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("[G6] Image Target fotoğrafı yok, yalnızca Model Target kullanılıyor: " + req.error);
                yield break;
            }

            // Fotoğraf + kartın gerçek genişliği (0,1016 m) ile Image Target kurulur. Vuforia fotoğraftaki
            // doku noktalarını arar; bu yüzden kartı neredeyse her açıdan, silüet olmadan anında tanır.
            var tex = DownloadHandlerTexture.GetContent(req);
            var target = VuforiaBehaviour.Instance.ObserverFactory.CreateImageTarget(tex, imageWidth, "UstGorunum");
            if (target == null)
            {
                Debug.LogWarning("[G6] Image Target oluşturulamadı.");
                yield break;
            }
            // Fotoğraf kartın üst yüzeyinde durur; hologramın tabanı 1,6 mm (kart kalınlığı) aşağıdadır: imageOffset.
            source.Add(target, imageOffset, Quaternion.identity);
            Debug.Log("[G6] Image Target hazır.");
        }

        /// <summary>
        /// Her karede çalışır. Hiçbir hedef nesneyi görmüyorsa birkaç saniyede bir sıradaki Model Target'ı açar:
        /// Arduino, kitap, dinozor, yine Arduino... Bir hedef nesneyi görünce sıra durur.
        /// </summary>
        void Update()
        {
            if (rota.Count == 0) return;

            // Başka bir nesne (kitap, dinozor) takipteyse Arduino'nun tarama halkası gizlenir.
            bool other = false;
            foreach (var kv in statuses)
                if (kv.Key != modelTarget && (kv.Value == Status.TRACKED || kv.Value == Status.EXTENDED_TRACKED)) other = true;
            if (other != otherTracked) app.SetOtherTargetTracked(otherTracked = other);

            // Etkin hedef nesneyi görüyorsa sıra durur.
            var active = rota[rotaIndex];
            if (statuses[active] == Status.TRACKED)
            {
                timer = 0f;
                return;
            }

            // 360° (Advanced) hedefler her etkinleşmede birkaç saniye hazırlanır: hazırlanırken süre yavaş akar
            // (hedefe yaklaşık 3 kat süre tanınır), ama hazırlık bitmese de sıra kilitlenmez.
            timer += Time.deltaTime * (infos[active] == StatusInfo.INITIALIZING ? 0.35f : 1f);
            if (timer < cycleSeconds) return;
            timer = 0f;

            // Sıradaki hedefe geç. Arduino'nun sırası geldiğinde bir sonraki kılavuz açısı denenir
            // (Model Target yalnızca etkin kılavuz açısına yakın bakışta tanır).
            if (rota.Count > 1)
            {
                active.enabled = false;
                rotaIndex = (rotaIndex + 1) % rota.Count;
                rota[rotaIndex].enabled = true;
            }
            if (rota[rotaIndex] == modelTarget && guideViews.Length > 0)
            {
                viewIndex = (viewIndex + 1) % guideViews.Length;
                modelTarget.SetActiveGuideViewName(guideViews[viewIndex]);
            }
        }
    }
}
