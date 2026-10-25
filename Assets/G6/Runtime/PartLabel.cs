using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace G6
{
    /// <summary>
    /// Parçaya iliştirilmiş etiket: parçanın üstünde parlayan nokta, dünya uzayında çizgi ve ekran
    /// kenarındaki kart. Kart ekran (overlay) arayüzünde durur; bu sayede her zaman net okunur ve
    /// sahnedeki hiçbir yüzeyin arkasında kalmaz. Kartın yerini LabelManager hesaplar.
    /// </summary>
    public class PartLabel : MonoBehaviour
    {
        public const float CardWidth = 440f;
        public const float CardHeight = 104f;

        public PartView Part { get; private set; }
        public float Alpha => alpha;

        RectTransform card;
        CanvasGroup group;
        LineRenderer line;
        Transform dot;
        Material dotMaterial;
        float alpha;

        public static PartLabel Create(PartView part, int index, Material glowMaterial, Transform worldParent,
            RectTransform cardParent, Action<PartView> onClick)
        {
            var go = new GameObject("Etiket_" + part.partId);
            go.transform.SetParent(worldParent, false);
            var label = go.AddComponent<PartLabel>();
            label.Build(part, index, glowMaterial, cardParent, onClick);
            return label;
        }

        void Build(PartView part, int index, Material glowMaterial, RectTransform cardParent, Action<PartView> onClick)
        {
            Part = part;

            line = gameObject.AddComponent<LineRenderer>();
            line.sharedMaterial = glowMaterial;
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.numCapVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var dotGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(dotGo.GetComponent<Collider>());
            dotGo.name = "Nokta";
            dotGo.transform.SetParent(transform, false);
            dot = dotGo.transform;
            var dotRenderer = dotGo.GetComponent<Renderer>();
            dotMaterial = new Material(glowMaterial);
            dotRenderer.sharedMaterial = dotMaterial;
            dotRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var bg = UIFactory.Panel("Kart_" + part.partId, cardParent, UIFactory.PanelColor);
            card = bg.rectTransform;
            card.anchorMin = card.anchorMax = Vector2.zero; // konum ekran pikseli olarak verilir
            card.sizeDelta = new Vector2(CardWidth, CardHeight);
            group = bg.gameObject.AddComponent<CanvasGroup>();
            var button = bg.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => onClick(Part));

            var bar = UIFactory.Solid("Renk", card, part.color);
            UIFactory.Place(bar.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(12, CardHeight - 28));

            var num = UIFactory.Text("No", card, (index + 1).ToString("00"), 30, part.color, TextAlignmentOptions.Left, FontStyles.Bold);
            UIFactory.Place(num.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(36, 0), new Vector2(60, CardHeight));

            var title = UIFactory.Text("Ad", card, part.title, 40, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            UIFactory.Place(title.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(96, 0), new Vector2(CardWidth - 112, CardHeight));

            SetAlpha(0f);
        }

        /// <param name="target">0 = gizli, 1 = tam görünür, arası = soluk (başka parça seçiliyken).</param>
        public void UpdateAlpha(float target)
        {
            alpha = Mathf.MoveTowards(alpha, target, Time.deltaTime * 3.5f);
            SetAlpha(alpha);
        }

        /// <summary>
        /// Kartı ekrana yerleştirir ve parçadan karta çizgi çeker.
        /// </summary>
        /// <param name="edgeScreen">Kartın çizgiye bağlanan iç kenarı (ekran pikseli).</param>
        /// <param name="pixelScale">Kartın 440x104 tasarım boyutuna göre ölçeği.</param>
        public void Layout(Camera cam, Vector2 edgeScreen, float pixelScale, bool rightSide)
        {
            if (alpha <= 0.001f) return;
            Vector3 anchor = Part.LabelAnchorWorld;
            float pop = EaseOutBack(Mathf.Clamp01(alpha * 1.4f));

            // Kart iç kenarından dışarı doğru büyür; açılış animasyonunda çizgi kartdan kopmaz.
            card.pivot = new Vector2(rightSide ? 0f : 1f, 0.5f);
            card.position = new Vector3(edgeScreen.x, edgeScreen.y, 0f);
            card.localScale = Vector3.one * (pixelScale * pop);

            // Çizginin ucu: kart kenarının parçayla aynı derinlikteki dünya noktası.
            float depth = cam.WorldToScreenPoint(anchor).z;
            Vector3 end = cam.ScreenToWorldPoint(new Vector3(edgeScreen.x, edgeScreen.y, depth));
            float dist = Vector3.Distance(cam.transform.position, anchor);
            line.SetPosition(0, anchor);
            line.SetPosition(1, Vector3.Lerp(anchor, end, pop));
            float w = dist * 0.004f;
            line.startWidth = w * 0.5f;
            line.endWidth = w;

            dot.position = anchor;
            float pulse = 1f + Mathf.Sin(Time.time * 5f + Part.transform.GetSiblingIndex()) * 0.2f;
            dot.localScale = Vector3.one * (dist * 0.02f * pulse * pop);
        }

        void SetAlpha(float a)
        {
            bool on = a > 0.001f;
            card.gameObject.SetActive(on);
            group.alpha = a;
            group.blocksRaycasts = a > 0.5f;
            line.enabled = on;
            dot.gameObject.SetActive(on);
            Color c = Part.color;
            line.startColor = new Color(c.r, c.g, c.b, a);
            line.endColor = new Color(c.r, c.g, c.b, a * 0.5f);
            dotMaterial.color = new Color(c.r, c.g, c.b, a);
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3) + c1 * Mathf.Pow(x - 1f, 2);
        }
    }
}
