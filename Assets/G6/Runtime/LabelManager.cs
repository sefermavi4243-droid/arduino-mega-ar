using System;
using System.Collections.Generic;
using UnityEngine;

namespace G6
{
    /// <summary>
    /// Her parça için bir etiket üretir, etiketleri dalga hâlinde açar ve kartları ekranın iki kenarındaki
    /// sütunlara dizer (infografik düzeni). Böylece kartlar hiçbir ekran oranında taşmaz ve üst üste binmez.
    /// </summary>
    public class LabelManager : MonoBehaviour
    {
        [SerializeField] float staggerDelay = 0.12f;
        [SerializeField] Material glowMaterial;
        [Tooltip("Kart genişliğinin ekran genişliğine oranı")]
        [SerializeField] float cardWidthRatio = 0.31f;

        class Slot
        {
            public PartLabel Label;
            public Vector3 ScreenPos;
            public bool Right;
            public float DesiredY;
            public float Y;
            public bool HasY;
        }

        readonly List<Slot> slots = new List<Slot>();
        readonly List<Slot> column = new List<Slot>();
        Camera cam;
        bool visible;
        float visibleSince;

        public PartView Focus { get; set; }

        /// <summary>Bir etiket kartına dokunuldu.</summary>
        public event Action<PartView> PartClicked;

        public void Build(DigitalTwin twin)
        {
            cam = Camera.main;

            // Kartlar için ayrı ekran katmanı: ana arayüzün (sortingOrder 10) altında kalır.
            // CanvasScaler yok: 1 birim = 1 ekran pikseli, yerleşim doğrudan piksel ile yapılır.
            var canvasGo = new GameObject("EtiketKartlari", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var cardRoot = (RectTransform)canvasGo.transform;

            for (int i = 0; i < twin.Parts.Length; i++)
            {
                var label = PartLabel.Create(twin.Parts[i], i, glowMaterial, transform, cardRoot, p => PartClicked?.Invoke(p));
                slots.Add(new Slot { Label = label });
            }
        }

        public void SetVisible(bool on)
        {
            if (on && !visible) visibleSince = Time.time;
            visible = on;
        }

        void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            for (int i = 0; i < slots.Count; i++)
            {
                var label = slots[i].Label;
                float target = 0f;
                // Kalabalık olmasın: başta yalnızca ana bölgeler; diğer parçaların etiketi seçilince belirir.
                bool shown = label.Part.primary || Focus == label.Part;
                if (visible && shown && Time.time >= visibleSince + i * staggerDelay)
                    target = Focus == null || Focus == label.Part ? 1f : 0.25f;
                label.UpdateAlpha(target);
            }

            float cardW = Screen.width * cardWidthRatio;
            float cardH = cardW * PartLabel.CardHeight / PartLabel.CardWidth;
            float step = cardH * 1.18f;
            Rect safe = Screen.safeArea;
            float top = safe.yMax - Screen.height * 0.16f;     // durum çubuğunun altı
            float bottom = safe.yMin + Screen.height * 0.13f;  // araç çubuğunun üstü
            float margin = Screen.width * 0.025f;

            foreach (var s in slots)
            {
                if (s.Label.Alpha <= 0.001f) { s.HasY = false; continue; }
                Vector3 dir = s.Label.Part.LabelDirectionWorld;
                s.ScreenPos = cam.WorldToScreenPoint(s.Label.Part.LabelAnchorWorld);
                s.Right = Vector3.Dot(dir, cam.transform.right) >= 0f;
                s.DesiredY = s.ScreenPos.y + Vector3.Dot(dir, cam.transform.up) * cardH * 1.5f;
            }

            LayoutColumn(true, top, bottom, cardH, step, cardW, margin);
            LayoutColumn(false, top, bottom, cardH, step, cardW, margin);
        }

        /// <summary>
        /// Bir sütundaki (sol ya da sağ) kartları dikey olarak dizer. Her kart önce kendi parçasının hizasına
        /// yerleşmek ister; çakışan kartlar aşağı, ekrandan taşanlar yukarı itilir.
        /// </summary>
        void LayoutColumn(bool right, float top, float bottom, float cardH, float step, float cardW, float margin)
        {
            column.Clear();
            foreach (var s in slots)
                if (s.Label.Alpha > 0.001f && s.ScreenPos.z > 0f && s.Right == right) column.Add(s);
            if (column.Count == 0) return;

            column.Sort((a, b) => b.DesiredY.CompareTo(a.DesiredY)); // yukarıdan aşağı

            // İki geçiş: yukarıdan aşağı itip çakışmayı çöz, sonra alttan taşanı yukarı it.
            float[] ys = new float[column.Count];
            float prev = float.MaxValue;
            for (int i = 0; i < column.Count; i++)
            {
                ys[i] = Mathf.Min(column[i].DesiredY, prev - step, top - cardH / 2f);
                prev = ys[i];
            }
            float next = float.MinValue;
            for (int i = column.Count - 1; i >= 0; i--)
            {
                ys[i] = Mathf.Max(ys[i], next + step, bottom + cardH / 2f);
                next = ys[i];
            }

            float innerX = right ? Screen.width - margin - cardW : margin + cardW;
            float k = 1f - Mathf.Exp(-Time.deltaTime * 12f);
            float pixelScale = cardW / PartLabel.CardWidth;

            for (int i = 0; i < column.Count; i++)
            {
                var s = column[i];
                s.Y = s.HasY ? Mathf.Lerp(s.Y, ys[i], k) : ys[i];
                s.HasY = true;
                s.Label.Layout(cam, new Vector2(innerX, s.Y), pixelScale, right);
            }
        }
    }
}
