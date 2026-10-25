using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace G6
{
    /// <summary>
    /// Uygulamanın beyni. Takip olaylarını dinler, dokunuşları parçalara çevirir,
    /// arayüz düğmelerini dijital ikize, pin haritasına, akım animasyonuna, Blink demosuna ve quiz'e bağlar.
    /// Akış: Aranıyor (tarama halkası) -> nesne bulundu -> tarama efekti + akım -> ana etiketler -> keşif.
    /// </summary>
    public class AppController : MonoBehaviour
    {
        [SerializeField] DigitalTwin twin;
        [SerializeField] LabelManager labels;
        [SerializeField] Material glowMaterial;
        [Tooltip("Arayüzde görünen nesne adı (sahne kurucu doldurur)")]
        [SerializeField] string objectName = "Arduino Mega";

        static readonly Color Correct = new Color(0.3f, 1f, 0.5f);
        static readonly Color Wrong = new Color(1f, 0.3f, 0.35f);

        TrackingSource source;
        AppUI ui;
        SfxPlayer sfx;
        readonly QuizController quiz = new QuizController();
        ParticleSystem confetti;

        // Nesneye göre var olabilen özellikler (Arduino'da hepsi var, zımbada yok).
        PinMap pinMap;
        EnergyFlow energy;
        BlinkDemo blink;

        bool labelsOn = true;
        bool wasRevealed;
        Vector2 pressPos;
        bool pressOverUI;

        void Start()
        {
            pinMap = twin.GetComponent<PinMap>();
            energy = twin.GetComponent<EnergyFlow>();
            blink = twin.GetComponent<BlinkDemo>();

            source = PickSource();
            sfx = gameObject.AddComponent<SfxPlayer>();
            ui = AppUI.Create(this, source != null && source.IsSimulation);
            ui.SetButtons(Buttons());
            labels.Build(twin);
            labels.PartClicked += OnPartTapped;
            confetti = CreateConfetti();

            // AR sahnesinde kaynak, Vuforia başlayınca VuforiaModelTargetLoader tarafından verilir.
            if (source != null) UseSource(source);
            else ui.SetTracking(false);
        }

        IEnumerable<(string, string, Action)> Buttons()
        {
            yield return ("etiket", "Keşfet", ToggleLabels);
            if (pinMap != null) yield return ("pin", "Pinler", TogglePins);
            if (energy != null) yield return ("akim", "Akım", ToggleEnergy);
            yield return ("patlat", "Patlat", ToggleExplode);
            if (blink != null) yield return ("blink", "Blink", ToggleBlink);
            yield return ("quiz", "Quiz", ToggleQuiz);
        }

        /// <summary>Takip kaynağını bağlar. Vuforia kaynağı çalışma anında, kamera açıldıktan sonra gelir.</summary>
        public void UseSource(TrackingSource newSource)
        {
            if (source != null) source.TrackingChanged -= OnTrackingChanged;
            source = newSource;
            source.TrackingChanged += OnTrackingChanged;
            OnTrackingChanged(source.IsTracked);
        }

        void OnDestroy()
        {
            if (source != null) source.TrackingChanged -= OnTrackingChanged;
        }

        /// <summary>Gerçek takip (Vuforia) varsa onu, yoksa simülasyonu kullanır.</summary>
        static TrackingSource PickSource()
        {
            TrackingSource sim = null;
            foreach (var s in FindObjectsByType<TrackingSource>(FindObjectsSortMode.None))
            {
                if (!s.IsSimulation) return s;
                sim = s;
            }
            return sim;
        }

        void OnTrackingChanged(bool tracked)
        {
            ui.SetTracking(tracked);
            if (tracked)
            {
                twin.Show();
                wasRevealed = false;
                sfx.Found();
                Vibrate();
            }
            else
            {
                twin.Hide();
                labels.SetVisible(false);
                if (pinMap != null) pinMap.SetVisible(false);
                ui.HideInfo();
                if (wasRevealed) sfx.Lost();
                wasRevealed = false;
            }
            RefreshToolbar();
        }

        void Update()
        {
            // Tarama bitince: akım kartın üzerinden bir kez akar, ardından ana etiketler dalga hâlinde açılır.
            if (twin.IsRevealed && !wasRevealed)
            {
                wasRevealed = true;
                if (energy != null && !energy.Playing) energy.Play(3.5f);
                labels.SetVisible(labelsOn && !quiz.Active);
                if (pinMap != null) pinMap.SetVisible(true);
            }

            if (blink != null) ui.SetCode(BlinkDemo.CodeLines, blink.Running ? blink.CurrentLine : -1, blink.LedOn);

            HandlePointer();
        }

        void HandlePointer()
        {
            var pointer = Pointer.current;
            if (pointer == null) return;

            if (pointer.press.wasPressedThisFrame)
            {
                pressPos = pointer.position.ReadValue();
                pressOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            }

            if (!pointer.press.wasReleasedThisFrame || pressOverUI) return;
            Vector2 pos = pointer.position.ReadValue();
            if ((pos - pressPos).sqrMagnitude > 15f * 15f) return; // sürükleme, dokunma değil
            if (source == null || !source.IsTracked || !twin.IsRevealed) return;

            HandleTap(pos);
        }

        void HandleTap(Vector2 screenPos)
        {
            var cam = Camera.main;
            PartView part = null;
            if (Physics.Raycast(cam.ScreenPointToRay(screenPos), out var hit, 5f))
                part = hit.collider.GetComponentInParent<PartView>();
            OnPartTapped(part);
        }

        /// <summary>Nesnedeki bir parçaya veya etiket kartına dokunuldu (boşluğa dokunulduysa null).</summary>
        void OnPartTapped(PartView part)
        {
            if (quiz.Active)
            {
                if (part != null) AnswerQuiz(part);
                return;
            }
            Select(part == null || twin.Selected == part ? null : part);
        }

        public void Select(PartView part)
        {
            twin.Select(part);
            labels.Focus = part;
            // Seçilen parça bir pin başlığıysa yalnızca onun pin adları belirir.
            if (pinMap != null) pinMap.ShowPart(part != null ? part.partId : null);
            if (part == null)
            {
                ui.HideInfo();
                return;
            }
            sfx.Tap();
            ui.ShowInfo(part, Array.IndexOf(twin.Parts, part), twin.Parts.Length);
        }

        // ---- Quiz ----

        void AnswerQuiz(PartView tapped)
        {
            var asked = quiz.Current;
            bool ok = quiz.Answer(tapped);
            if (ok)
            {
                sfx.Correct();
                tapped.Flash(Correct);
                ui.Flash(Correct);
                Burst(tapped.LabelAnchorWorld);
            }
            else
            {
                sfx.Wrong();
                tapped.Flash(Wrong);
                asked.Flash(Correct, 1.4f); // doğru cevabı göster
                ui.Flash(Wrong);
            }

            if (quiz.Active) AskNext();
            else FinishQuiz();
        }

        void AskNext()
        {
            ui.SetQuiz(true, "<color=#FFC04D>" + quiz.Current.title + "</color> nerede? Dokun!",
                $"SORU {quiz.Asked + 1}/{quiz.Total}   •   PUAN {quiz.Score}");
        }

        void FinishQuiz()
        {
            string verdict = quiz.Score == quiz.Total ? "Kusursuz! " + objectName + " ustasısın." :
                quiz.Score >= quiz.Total / 2 + 1 ? "Güzel iş!" : "Etiketlere bakıp tekrar dene.";
            ui.SetQuiz(true, verdict, $"SONUÇ: {quiz.Score}/{quiz.Total}");
            if (quiz.Score == quiz.Total) Burst(twin.WorldBounds().center, 120);
            Invoke(nameof(EndQuizView), 3f);
            RefreshToolbar();
        }

        void EndQuizView()
        {
            if (quiz.Active) return;
            ui.SetQuiz(false);
            labels.SetVisible(labelsOn && twin.IsRevealed);
            RefreshToolbar();
        }

        // ---- Araç çubuğu ----

        public void ToggleLabels()
        {
            labelsOn = !labelsOn;
            labels.SetVisible(labelsOn && twin.IsRevealed && !quiz.Active);
            sfx.Tap();
            RefreshToolbar();
        }

        public void TogglePins()
        {
            pinMap.SetShowAll(!pinMap.ShowAll);
            // Pin adları okunurken bölge etiketleri araya girmesin.
            labelsOn = !pinMap.ShowAll;
            labels.SetVisible(labelsOn && twin.IsRevealed && !quiz.Active);
            sfx.Tap();
            RefreshToolbar();
        }

        public void ToggleEnergy()
        {
            if (energy.Playing) energy.Stop();
            else energy.Play();
            sfx.Whoosh();
            RefreshToolbar();
        }

        public void ToggleExplode()
        {
            twin.SetExploded(!twin.Exploded);
            sfx.Whoosh();
            RefreshToolbar();
        }

        public void ToggleBlink()
        {
            blink.SetRunning(!blink.Running);
            sfx.Tap();
            RefreshToolbar();
        }

        public void ToggleQuiz()
        {
            CancelInvoke(nameof(EndQuizView));
            if (quiz.Active)
            {
                quiz.Stop();
                ui.SetQuiz(false);
                labels.SetVisible(labelsOn && twin.IsRevealed);
            }
            else
            {
                Select(null);
                // İçteki parçalar ancak patlatılmış görünümde dokunulabilir hâle gelir.
                twin.SetExploded(true);
                quiz.Begin(twin.Parts);
                labels.SetVisible(false); // etiketler açıkken quiz çok kolay olur
                AskNext();
            }
            RefreshToolbar();
        }

        public void CloseInfo() => Select(null);

        /// <summary>Quiz'de sorulan parça (quiz kapalıysa null). Sunum videosu kaydı için.</summary>
        public PartView QuizCurrent => quiz.Active ? quiz.Current : null;

        /// <summary>Quiz'de bir parçaya dokunulmuş gibi cevap verir. Sunum videosu kaydı için.</summary>
        public void AnswerQuizWith(PartView part) => OnPartTapped(part);

        public void SimToggle()
        {
            if (source is SimulatedTrackingSource sim) sim.ToggleManual();
        }

        void RefreshToolbar()
        {
            ui.SetButton("etiket", labelsOn);
            if (pinMap != null) ui.SetButton("pin", pinMap.ShowAll);
            if (energy != null) ui.SetButton("akim", energy.Playing);
            ui.SetButton("patlat", twin.Exploded);
            if (blink != null) ui.SetButton("blink", blink.Running);
            ui.SetButton("quiz", quiz.Active);
        }

        void LateUpdate()
        {
            // Akım animasyonu kendi kendine bitebilir; düğme durumunu güncel tut.
            if (energy != null) ui.SetButton("akim", energy.Playing);
        }

        // ---- Efektler ----

        ParticleSystem CreateConfetti()
        {
            var go = new GameObject("Konfeti");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.003f, 0.006f);
            main.gravityModifier = 0.3f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.25f, 0.9f, 1f), new Color(1f, 0.75f, 0.3f))
            {
                mode = ParticleSystemGradientMode.TwoColors
            };

            var emission = ps.emission;
            emission.rateOverTime = 0;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.008f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 0.6f), new GradientAlphaKey(0, 1) });
            col.color = g;

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);

            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = glowMaterial;
            return ps;
        }

        void Burst(Vector3 position, int count = 50)
        {
            confetti.transform.position = position;
            confetti.Emit(count);
        }

        static void Vibrate()
        {
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }
}
