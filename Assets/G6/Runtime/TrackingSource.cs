using System;
using UnityEngine;

namespace G6
{
    /// <summary>
    /// Takip kaynağının ortak sözleşmesi. Uygulamanın geri kalanı yalnızca "nesne bulundu / kayboldu"
    /// bilgisini bilir; bilginin Vuforia'dan mı simülasyondan mı geldiğini umursamaz.
    /// </summary>
    public abstract class TrackingSource : MonoBehaviour
    {
        public event Action<bool> TrackingChanged;

        public bool IsTracked { get; private set; }

        /// <summary>Editörde Vuforia olmadan çalışan sahte kaynak mı?</summary>
        public virtual bool IsSimulation => false;

        protected void SetTracked(bool tracked)
        {
            if (tracked == IsTracked) return;
            IsTracked = tracked;
            TrackingChanged?.Invoke(tracked);
        }
    }
}
