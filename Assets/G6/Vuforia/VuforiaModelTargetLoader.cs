using System.Collections;
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
        int viewIndex;
        float viewTimer;

        void Start()
        {
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

            // İkiz, takip kaynağının taşıdığı çapanın altına girer.
            var anchor = new GameObject("TakipCapasi");
            source = anchor.AddComponent<VuforiaTrackingSource>();
            twin.transform.SetParent(anchor.transform, false);
            twin.transform.localPosition = Vector3.zero;
            twin.transform.localRotation = Quaternion.identity;

            modelTarget = VuforiaBehaviour.Instance.ObserverFactory.CreateModelTarget(dataSetPath, targetName);
            if (modelTarget != null)
            {
                modelTarget.GuideViewMode = ModelTargetBehaviour.GuideViewDisplayMode.NoGuideView;
                source.Add(modelTarget, Vector3.zero, Quaternion.identity);
                Debug.Log("[G6] Model Target hazır: " + targetName);
            }
            else Debug.LogWarning($"[G6] Model Target oluşturulamadı: {dataSetPath} / {targetName}");

            app.UseSource(source);
            if (!string.IsNullOrEmpty(imagePath)) StartCoroutine(CreateImageTarget());
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

            var tex = DownloadHandlerTexture.GetContent(req);
            var target = VuforiaBehaviour.Instance.ObserverFactory.CreateImageTarget(tex, imageWidth, "UstGorunum");
            if (target == null)
            {
                Debug.LogWarning("[G6] Image Target oluşturulamadı.");
                yield break;
            }
            source.Add(target, imageOffset, Quaternion.identity);
            Debug.Log("[G6] Image Target hazır.");
        }

        void Update()
        {
            if (modelTarget == null || guideViews.Length == 0 || source.IsTracked)
            {
                viewTimer = 0f;
                return;
            }
            // Model Target yalnızca etkin kılavuz açısına yakın bakışta tanır; açıları görünmeden sırayla dene.
            viewTimer += Time.deltaTime;
            if (viewTimer > cycleSeconds)
            {
                viewTimer = 0f;
                viewIndex = (viewIndex + 1) % guideViews.Length;
                modelTarget.SetActiveGuideViewName(guideViews[viewIndex]);
            }
        }
    }
}
