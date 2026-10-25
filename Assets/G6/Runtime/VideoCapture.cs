using System.Collections;
using System.IO;
using UnityEngine;

namespace G6
{
    /// <summary>
    /// Sunum videoları için simülasyondan kare kare görüntü kaydı. Yalnızca oyuncu "-g6video &lt;klasör&gt;"
    /// argümanıyla başlatılırsa çalışır. Her sahne ayrı bir alt klasöre 30 fps JPG kareleri olarak yazılır;
    /// kareler sonra ffmpeg ile videoya çevrilir.
    /// </summary>
    public class VideoCapture : MonoBehaviour
    {
        const int Fps = 30;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            var args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, "-g6video");
            if (i < 0 || i + 1 >= args.Length) return;
            var go = new GameObject("VideoCapture");
            go.AddComponent<VideoCapture>().dir = args[i + 1];
        }

        string dir;
        string clip;   // şu an kaydedilen sahnenin adı (null = kayıt yok)
        int frame;

        AppController app;
        DigitalTwin twin;
        SimulatedTrackingSource sim;

        IEnumerator Start()
        {
            Time.captureFramerate = Fps; // oyun zamanı kare başına sabit ilerler: yavaş kayıt bile akıcı olur
            app = FindFirstObjectByType<AppController>();
            twin = FindFirstObjectByType<DigitalTwin>();
            sim = FindFirstObjectByType<SimulatedTrackingSource>();
            float yaw0 = sim.Yaw, pitch0 = sim.Pitch, dist0 = sim.Distance;

            // 1) Arama, bulma, tarama, akım ve etiketler
            Begin("01_bulma");
            yield return Seconds(9f);

            // 2) Etiketler takılı kalırken kamera kartın etrafında döner
            Begin("02_yorunge");
            yield return Orbit(yaw0, yaw0 + 360f, pitch0, 40f, dist0, 10f);

            // 3) Pin haritası: önce hepsi, sonra yalnızca dijital başlık
            sim.SetView(yaw0, 62f, 0.12f);
            Begin("03_pinler");
            app.TogglePins();
            yield return Seconds(4f);
            app.Select(Part("dijital"));
            yield return Seconds(3f);
            app.Select(null);
            app.TogglePins();
            yield return Seconds(0.5f);
            sim.SetView(yaw0, pitch0, dist0);

            // 4) Akım
            Begin("04_akim");
            app.ToggleEnergy();
            yield return Seconds(5f);
            app.ToggleEnergy();

            // 5) Blink
            Begin("05_blink");
            app.ToggleBlink();
            yield return Seconds(6f);
            app.ToggleBlink();

            // 6) Bilgi kartı
            Begin("06_bilgi");
            yield return Seconds(0.8f);
            app.Select(Part("mcu"));
            yield return Seconds(3.5f);
            app.Select(Part("usb_b"));
            yield return Seconds(3.5f);
            app.CloseInfo();
            yield return Seconds(0.8f);

            // 7) Patlat
            Begin("07_patlat");
            app.ToggleExplode();
            yield return Orbit(sim.Yaw, sim.Yaw + 60f, pitch0, 30f, dist0 * 1.2f, 5f);
            app.ToggleExplode();
            yield return Seconds(1.5f);
            sim.SetView(yaw0, pitch0, dist0);

            // 8) Quiz: bir doğru, bir yanlış cevap
            Begin("08_quiz");
            app.ToggleQuiz();
            yield return Seconds(2f);
            app.AnswerQuizWith(app.QuizCurrent);
            yield return Seconds(2f);
            var wrong = System.Array.Find(twin.Parts, p => p != app.QuizCurrent);
            app.AnswerQuizWith(wrong);
            yield return Seconds(2.5f);
            app.ToggleQuiz();
            app.ToggleExplode();
            yield return Seconds(1f);

            // 9) Takip kaybı ve yeniden bulma
            Begin("09_kayip");
            yield return Seconds(1f);
            app.SimToggle();
            yield return Seconds(3f);
            app.SimToggle();
            yield return Seconds(5f);

            // 10) Kılavuz açıları: hologram ve arayüz kapalı, yalnızca "gerçek" kart
            app.SimToggle();
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) c.enabled = false;
            var views = new (string name, Vector3 dir)[]
            {
                ("UstOn", new Vector3(0f, 1.6f, -1f)),
                ("UsbTarafi", new Vector3(-1f, 1.3f, -0.2f)),
                ("PinTarafi", new Vector3(1f, 1.3f, 0.2f)),
                ("UstArka", new Vector3(0f, 1.6f, 1f)),
                ("Capraz", new Vector3(-0.8f, 1.4f, -0.8f)),
            };
            foreach (var v in views)
            {
                // Kamera, nesneden v.dir yönünde durur ve nesneye bakar.
                var d = v.dir.normalized;
                sim.SetView(Mathf.Atan2(-d.x, -d.z) * Mathf.Rad2Deg, Mathf.Asin(d.y) * Mathf.Rad2Deg, 0.2f);
                Begin("10_kilavuz_" + v.name);
                yield return Seconds(1.5f);
            }

            Begin(null);
            yield return null;
            Application.Quit();
        }

        PartView Part(string id) => System.Array.Find(twin.Parts, p => p.partId == id);

        void Begin(string name)
        {
            clip = name;
            frame = 0;
            if (name != null) Directory.CreateDirectory(Path.Combine(dir, name));
        }

        static IEnumerator Seconds(float s)
        {
            for (int i = 0; i < Mathf.RoundToInt(s * Fps); i++) yield return null;
        }

        IEnumerator Orbit(float yawFrom, float yawTo, float pitchFrom, float pitchTo, float distance, float seconds)
        {
            int n = Mathf.RoundToInt(seconds * Fps);
            for (int i = 0; i <= n; i++)
            {
                float t = Mathf.SmoothStep(0f, 1f, i / (float)n);
                float p = Mathf.Lerp(pitchFrom, pitchTo, Mathf.Sin(t * Mathf.PI)); // yarıda alçalıp geri yükselir
                sim.SetView(Mathf.Lerp(yawFrom, yawTo, t), p, distance);
                yield return null;
            }
        }

        void LateUpdate()
        {
            if (clip != null) StartCoroutine(Grab(clip, frame++));
        }

        IEnumerator Grab(string clipName, int index)
        {
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(dir, clipName, index.ToString("D5") + ".jpg"), tex.EncodeToJPG(92));
            Destroy(tex);
        }
    }
}
