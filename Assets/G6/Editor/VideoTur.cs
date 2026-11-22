using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace G6.EditorTools
{
    /// <summary>
    /// Sunum videoları için Unity Editor'ün kendisini kaydeder. Her klip için editörde bir şeyler seçer,
    /// sahne kamerasını döndürür ve ffmpeg ile ana ekranı kaydeder. Oynatma kipine geçişte alan yeniden
    /// yüklendiği için durum SessionState'te tutulur.
    /// Komut satırı: Unity.exe -projectPath ... -executeMethod G6.EditorTools.VideoTur.Baslat
    /// </summary>
    [InitializeOnLoad]
    public static class VideoTur
    {
        const string FF = @"C:\Users\Sefer\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-9.0.2-full_build\bin\ffmpeg.exe";
        const string AR = "Assets/G6/Scenes/G6_NesneTakibi_AR.unity";
        const string SIM = "Assets/G6/Scenes/G6_NesneTakibi.unity";
        const float Hazirlik = 2.5f;

        class Klip
        {
            public string ad; public float sure;
            public Action kur; public Action<float> akis;
        }

        static readonly Klip[] Klipler =
        {
            new Klip { ad = "e01_sahne", sure = 14f, kur = () => { Sahne(AR); Sec(Bul("DigitalTwin"), true); },
                akis = t => Yorunge(t, 14f, 30f, 150f, 25f) },
            new Klip { ad = "e02_parca", sure = 12f, kur = () => { Sahne(AR); Sec(Bul("mcu"), true); },
                akis = t => { if (Gecti(t, 6f)) Sec(Bul("usb_b"), true); } },
            new Klip { ad = "e03_pinler", sure = 9f, kur = () => { Sahne(AR); Sec(Bul("dijital"), true); },
                akis = t => Yorunge(t, 9f, 200f, 240f, 45f) },
            new Klip { ad = "e04_veritabani", sure = 9f, kur = () => SecVarlik("Assets/StreamingAssets/Vuforia/G6_ArduinoMega.xml"),
                akis = t => { if (Gecti(t, 5f)) SecVarlik("Assets/StreamingAssets/Vuforia/G6_ArduinoMega.dat"); } },
            new Klip { ad = "e05_foto", sure = 7f, kur = () => SecVarlik("Assets/StreamingAssets/Vuforia/arduino_ust.jpg"), akis = t => { } },
            new Klip { ad = "e06_linkxml", sure = 8f, kur = () => SecVarlik("Assets/G6/link.xml"), akis = t => { } },
            new Klip { ad = "e07_loader", sure = 8f, kur = () => SecVarlik("Assets/G6/Vuforia/VuforiaModelTargetLoader.cs"), akis = t => { } },
            new Klip { ad = "e08_oynat", sure = 30f, kur = () => { Sahne(SIM); EditorApplication.EnterPlaymode(); },
                akis = t => { if (Gecti(t, 1f)) OyunPenceresi(); } },
        };

        static VideoTur()
        {
            if (SessionState.GetBool("G6Tur.aktif", false)) EditorApplication.update += Tik;
        }

        public static void Baslat()
        {
            Directory.CreateDirectory(Cikti);
            SessionState.SetBool("G6Tur.aktif", true);
            SessionState.SetInt("G6Tur.klip", -1);
            SessionState.SetInt("G6Tur.surum", Surum);
            EkraniKapla();
            EditorApplication.update -= Tik;
            EditorApplication.update += Tik;
        }

        static string Cikti => Path.GetFullPath("Builds/sunum/medya/editor");
        static double Simdi => EditorApplication.timeSinceStartup;
        static float suankiT, oncekiT;

        const int Surum = 2;

        static void Tik()
        {
            if (SessionState.GetInt("G6Tur.surum", 1) != Surum)
            {
                if (EditorApplication.isPlaying) { EditorApplication.isPlaying = false; return; }
                SessionState.SetInt("G6Tur.surum", Surum);
                SessionState.SetInt("G6Tur.klip", -1);
                Directory.CreateDirectory(Cikti);
            }
            int i = SessionState.GetInt("G6Tur.klip", -1);
            double t0 = SessionState.GetFloat("G6Tur.t0", 0f);
            if (i < 0 || Simdi - t0 > Hazirlik + Klipler[i].sure + 1.5f)
            {
                if (i >= 0 && EditorApplication.isPlaying) { EditorApplication.isPlaying = false; return; }
                i++;
                if (i >= Klipler.Length) { Bitir(); return; }
                SessionState.SetInt("G6Tur.klip", i);
                SessionState.SetFloat("G6Tur.t0", (float)Simdi);
                SessionState.SetBool("G6Tur.kayit", false);
                oncekiT = -1f;
                Debug.Log("[G6Tur] klip " + Klipler[i].ad);
                Klipler[i].kur();
                return;
            }
            float s = (float)(Simdi - t0);
            if (s < Hazirlik) return;
            if (!SessionState.GetBool("G6Tur.kayit", false))
            {
                SessionState.SetBool("G6Tur.kayit", true);
                Kaydet(Klipler[i]);
            }
            suankiT = s - Hazirlik;
            if (suankiT <= Klipler[i].sure) Klipler[i].akis(suankiT);
            oncekiT = suankiT;
            SceneView.RepaintAll();
        }

        static bool Gecti(float t, float an) => oncekiT < an && t >= an;

        static void Kaydet(Klip k)
        {
            var dosya = Path.Combine(Cikti, k.ad + ".mp4");
            // Ana editör penceresinin ekrandaki yeri (hangi monitörde olursa olsun).
            var r = EditorGUIUtility.GetMainWindowPosition();
            int x = Mathf.RoundToInt(r.x), y = Mathf.RoundToInt(r.y);
            int w = Mathf.RoundToInt(r.width) / 2 * 2, h = Mathf.RoundToInt(r.height) / 2 * 2;
            Debug.Log($"[G6Tur] kayıt alanı {x},{y} {w}x{h}");
            var arg = $"-y -loglevel error -f gdigrab -framerate 30 -draw_mouse 0 -offset_x {x} -offset_y {y} -video_size {w}x{h} " +
                      $"-t {k.sure.ToString(System.Globalization.CultureInfo.InvariantCulture)} -i desktop -c:v libx264 -preset veryfast -crf 18 -pix_fmt yuv420p \"{dosya}\"";
            Process.Start(new ProcessStartInfo(FF, arg) { CreateNoWindow = true, UseShellExecute = false });
        }

        static void Bitir()
        {
            EditorApplication.update -= Tik;
            SessionState.SetBool("G6Tur.aktif", false);
            Debug.Log("[G6Tur] bitti");
            File.WriteAllText(Path.Combine(Cikti, "bitti.txt"), DateTime.Now.ToString("s"));
            EditorApplication.Exit(0);
        }

        // ------------------------------------------------------------ editör eylemleri

        static void Sahne(string yol)
        {
            if (EditorSceneManager.GetActiveScene().path != yol) EditorSceneManager.OpenScene(yol);
        }

        static GameObject Bul(string ad) =>
            Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                  .Select(x => x.gameObject).Where(g => g.name.StartsWith(ad)).OrderBy(g => g.name.Length).FirstOrDefault();

        static void Sec(GameObject g, bool cercevele)
        {
            if (g == null) { Debug.LogWarning("[G6Tur] bulunamadı"); return; }
            Selection.activeGameObject = g;
            EditorGUIUtility.PingObject(g);
            var sv = SceneView.lastActiveSceneView;
            if (sv != null) { sv.Focus(); if (cercevele) sv.FrameSelected(); }
        }

        static void SecVarlik(string yol)
        {
            var o = AssetDatabase.LoadMainAssetAtPath(yol);
            if (o == null) { Debug.LogWarning("[G6Tur] yok: " + yol); return; }
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = o;
            EditorGUIUtility.PingObject(o);
        }

        static void Yorunge(float t, float sure, float a0, float a1, float egim)
        {
            var sv = SceneView.lastActiveSceneView;
            if (sv == null) return;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / sure));
            sv.rotation = Quaternion.Euler(egim, Mathf.Lerp(a0, a1, k), 0f);
        }

        static void OyunPenceresi()
        {
            var tip = Type.GetType("UnityEditor.GameView,UnityEditor");
            if (tip != null) EditorWindow.GetWindow(tip).Focus();
        }

        // ------------------------------------------------------------ pencereyi ana ekrana yay

        [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr h, int n);
        [DllImport("user32.dll")] static extern bool MoveWindow(IntPtr h, int x, int y, int w, int hh, bool r);
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);

        static void EkraniKapla()
        {
            var h = Process.GetCurrentProcess().MainWindowHandle;
            if (h == IntPtr.Zero) return;
            ShowWindow(h, 9);
            MoveWindow(h, 0, 0, 1920, 1040, true);
            ShowWindow(h, 3);
            SetForegroundWindow(h);
        }
    }
}
