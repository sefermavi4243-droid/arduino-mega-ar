using System.Collections;
using System.IO;
using UnityEngine;

namespace G6
{
    /// <summary>
    /// README ve sunum için otomatik ekran görüntüsü. Yalnızca oyuncu "-g6shots &lt;klasör&gt;" argümanıyla
    /// başlatılırsa çalışır; normal kullanımda hiçbir şey yapmaz.
    /// </summary>
    public class DemoCapture : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            var args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, "-g6shots");
            if (i < 0 || i + 1 >= args.Length) return;
            var go = new GameObject("DemoCapture");
            go.AddComponent<DemoCapture>().dir = args[i + 1];
        }

        string dir;

        IEnumerator Start()
        {
            Directory.CreateDirectory(dir);
            var app = FindFirstObjectByType<AppController>();
            var twin = FindFirstObjectByType<DigitalTwin>();

            yield return new WaitForSeconds(0.6f);
            yield return Shot("01_araniyor");
            yield return new WaitForSeconds(1.9f);
            yield return Shot("02_tarama");
            yield return new WaitForSeconds(1.2f);
            yield return Shot("03_akim");
            yield return new WaitForSeconds(2.4f);
            yield return Shot("04_etiketler");

            if (twin.GetComponent<PinMap>() != null)
            {
                app.TogglePins();
                yield return new WaitForSeconds(1.5f);
                yield return Shot("05_pinler");
                app.TogglePins();
            }
            if (twin.GetComponent<BlinkDemo>() != null)
            {
                app.ToggleBlink();
                yield return new WaitForSeconds(1.3f);
                yield return Shot("06_blink");
                app.ToggleBlink();
            }

            app.Select(twin.Parts[0]);
            yield return new WaitForSeconds(1.0f);
            yield return Shot("07_bilgi_karti");
            app.CloseInfo();

            app.ToggleExplode();
            yield return new WaitForSeconds(1.6f);
            yield return Shot("08_patlatilmis");
            app.ToggleExplode();

            app.ToggleQuiz();
            yield return new WaitForSeconds(1.2f);
            yield return Shot("09_quiz");

            Application.Quit();
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"));
            yield return null;
        }
    }
}
