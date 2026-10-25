using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace G6
{
    /// <summary>
    /// Arayüzü koddan kurmak için küçük yardımcılar. Yuvarlak köşeli sprite'lar çalışma anında
    /// üretilir; projede tek bir görsel dosyası gerekmez.
    /// </summary>
    public static class UIFactory
    {
        public static readonly Color Accent = new Color(0.25f, 0.9f, 1f);
        public static readonly Color PanelColor = new Color(0.03f, 0.06f, 0.09f, 0.86f);
        public static readonly Color ButtonColor = new Color(0.1f, 0.15f, 0.2f, 0.92f);
        public static readonly Color TextDim = new Color(0.72f, 0.82f, 0.88f);

        static Sprite rounded;
        static Sprite circle;

        public static Sprite Rounded => rounded != null ? rounded : rounded = MakeRounded(64, 24);
        public static Sprite Circle => circle != null ? circle : circle = MakeRounded(64, 32);
        /// <summary>Kapsül biçimi: daire sprite'ı 9 dilimli (sliced) kullanılınca kenarları tam yuvarlak olur.</summary>
        public static Sprite Pill => Circle;
        public static Sprite Ring => ring != null ? ring : ring = MakeRing(256, 5f);

        static Sprite ring;

        /// <summary>İnce halka (tarama göstergesi için).</summary>
        static Sprite MakeRing(int size, float thickness)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            float r = size / 2f - 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f));
                float a = Mathf.Clamp01(thickness / 2f - Mathf.Abs(d - (r - thickness / 2f)) + 0.5f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeRounded(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // Köşedeki çeyrek çemberin merkezine olan uzaklık; kenarda yumuşak geçiş (anti-aliasing).
                float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                byte a = (byte)(Mathf.Clamp01(radius - d + 0.5f) * 255);
                pixels[y * size + x] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Ebeveyni tamamen kaplayan dikdörtgen.</summary>
        public static RectTransform Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Panel(string name, Transform parent, Color color, Sprite sprite = null)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite != null ? sprite : Rounded;
            img.type = Image.Type.Sliced;
            img.color = color;
            return img;
        }

        /// <summary>Köşesi yuvarlatılmamış düz renk; ince çizgiler ve tam ekran katmanlar için.</summary>
        public static Image Solid(string name, Transform parent, Color color)
        {
            var img = Panel(name, parent, color);
            img.sprite = null;
            img.type = Image.Type.Simple;
            return img;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center, FontStyles style = FontStyles.Normal)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        public static Button Button(string name, Transform parent, string label, UnityAction onClick,
            out Image background, out TextMeshProUGUI text, float fontSize = 36f)
        {
            background = Panel(name, parent, ButtonColor);
            var btn = background.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.highlightedColor = Color.white;
            btn.colors = colors;
            btn.onClick.AddListener(onClick);
            text = Text("Label", background.transform, label, fontSize, Color.white, style: FontStyles.Bold);
            Stretch(text.rectTransform, 8f);
            return btn;
        }
    }
}
