using System.Collections.Generic;
using UnityEngine;
using Vuforia;

namespace G6
{
    /// <summary>
    /// Birden fazla Vuforia hedefini (Model Target ve Image Target) tek bir takip kaynağında birleştirir.
    /// Bu bileşen dijital ikizin bağlı olduğu "çapa" nesnesindedir; her karede o an en iyi gören hedefin
    /// pozunu (hedefe göre ofsetiyle) çapaya kopyalar. Hangi hedef bulursa bulsun hologram aynı yere oturur.
    /// Bu dosya yalnızca Vuforia paketi projede varsa derlenir (G6.Vuforia.asmdef -> VUFORIA_PRESENT).
    /// </summary>
    [DefaultExecutionOrder(1000)] // Vuforia pozları güncelledikten sonra çalışsın
    public class VuforiaTrackingSource : TrackingSource
    {
        class Entry
        {
            public ObserverBehaviour Observer;
            public Vector3 Offset;           // ikizin hedefe göre konumu (hedefin yerel uzayında)
            public Quaternion Rotation;
            public Status Status = Status.NO_POSE;
        }

        [Tooltip("EXTENDED_TRACKED: nesne kadrajdan kısa süre çıksa da Vuforia konumu tahmin etmeye devam eder.")]
        [SerializeField] bool acceptExtendedTracking = true;

        readonly List<Entry> entries = new();

        public void Add(ObserverBehaviour observer, Vector3 offset, Quaternion rotation)
        {
            var e = new Entry { Observer = observer, Offset = offset, Rotation = rotation };
            entries.Add(e);
            observer.OnTargetStatusChanged += (b, s) => e.Status = s.Status;
        }

        void LateUpdate()
        {
            Entry best = null;
            foreach (var e in entries)
            {
                if (e.Status == Status.TRACKED) { best = e; break; }
                if (acceptExtendedTracking && e.Status == Status.EXTENDED_TRACKED && best == null) best = e;
            }

            if (best != null)
            {
                var t = best.Observer.transform;
                transform.SetPositionAndRotation(t.TransformPoint(best.Offset), t.rotation * best.Rotation);
            }
            SetTracked(best != null);
        }
    }
}
