using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace G6
{
    /// <summary>
    /// Sade AR arayüzü. Nesne aranırken yalnızca yazısız bir tarama halkası görünür; nesne bulununca alttaki
    /// araç çubuğu kayarak gelir. Parça bilgi kartı, quiz bandı ve Blink kod paneli yalnızca gerektiğinde açılır.
    /// Tamamı koddan kurulur; Canvas Scaler 1080x1920 referansıyla her ekran oranına uyar.
    /// </summary>
    public class AppUI : MonoBehaviour
    {
        class ToggleButton
        {
            public Image Background;
            public TextMeshProUGUI Text;

            public void Set(bool on)
            {
                Background.color = on ? UIFactory.Accent : new Color(1f, 1f, 1f, 0.08f);
                Text.color = on ? new Color(0.02f, 0.08f, 0.1f) : Color.white;
            }
        }

        AppController app;

        // Tarama halkası
        CanvasGroup scanGroup;
        RectTransform scanArc;
        RectTransform scanRing;
        bool scanning = true;

        // Araç çubuğu
        RectTransform toolbar;
        CanvasGroup toolbarGroup;
        readonly Dictionary<string, ToggleButton> buttons = new();
        bool toolbarVisible;

        // Bilgi kartı
        RectTransform sheet;
        Image sheetBar;
        TextMeshProUGUI sheetIndex, sheetTitle, sheetDesc, sheetFact;
        bool sheetOpen;

        // Quiz
        CanvasGroup quizGroup;
        TextMeshProUGUI quizText, quizScore;
        bool quizVisible;

        // Blink kod paneli
        CanvasGroup codeGroup;
        TextMeshProUGUI codeText;
        bool codeVisible;

        Image flash;

        public static AppUI Create(AppController app, bool simulation)
        {
            EnsureEventSystem();

            var go = new GameObject("AppUI", typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();

            var ui = go.AddComponent<AppUI>();
            ui.app = app;
            ui.Build(simulation);
            return ui;
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem));
            es.AddComponent<InputSystemUIInputModule>();
        }

        void Build(bool simulation)
        {
            flash = UIFactory.Solid("Flas", transform, new Color(1, 1, 1, 0));
            flash.raycastTarget = false;
            UIFactory.Stretch(flash.rectTransform);

            var safe = UIFactory.Stretch(UIFactory.Rect("GuvenliAlan", transform));
            safe.gameObject.AddComponent<SafeArea>();

            BuildScan(safe);
            BuildCode(safe);
            BuildQuiz(safe);
            BuildToolbar(safe);
            BuildSheet(safe);

            if (simulation)
            {
                // Yalnızca editör simülasyonunda: takibi el ile aç/kapat (klavyede F).
                UIFactory.Button("SimDugme", safe, "F", app.SimToggle, out var bg, out var text, 30);
                bg.sprite = UIFactory.Circle;
                bg.color = new Color(1f, 1f, 1f, 0.12f);
                text.color = new Color(1f, 1f, 1f, 0.6f);
                UIFactory.Place(bg.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-28, -28), new Vector2(72, 72));
            }
        }

        void BuildScan(RectTransform safe)
        {
            var root = UIFactory.Rect("Tarama", safe);
            UIFactory.Place(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220, 220));
            scanGroup = root.gameObject.AddComponent<CanvasGroup>();
            scanGroup.blocksRaycasts = false;

            var ring = UIFactory.Panel("Halka", root, new Color(1f, 1f, 1f, 0.25f), UIFactory.Ring);
            ring.type = Image.Type.Simple;
            scanRing = UIFactory.Stretch(ring.rectTransform);

            var arc = UIFactory.Panel("Yay", root, UIFactory.Accent, UIFactory.Ring);
            arc.type = Image.Type.Filled;
            arc.fillMethod = Image.FillMethod.Radial360;
            arc.fillAmount = 0.22f;
            scanArc = UIFactory.Stretch(arc.rectTransform);

            var dot = UIFactory.Panel("Merkez", root, UIFactory.Accent, UIFactory.Circle);
            UIFactory.Place(dot.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14, 14));
        }

        void BuildCode(RectTransform safe)
        {
            var panel = UIFactory.Panel("Kod", safe, new Color(0.02f, 0.04f, 0.06f, 0.88f));
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(760, 340));
            codeGroup = panel.gameObject.AddComponent<CanvasGroup>();
            codeGroup.alpha = 0;
            codeGroup.blocksRaycasts = false;
            codeText = UIFactory.Text("Satirlar", panel.transform, "", 34, Color.white, TextAlignmentOptions.TopLeft);
            codeText.textWrappingMode = TextWrappingModes.NoWrap;
            UIFactory.Stretch(codeText.rectTransform, 28);
        }

        void BuildQuiz(RectTransform safe)
        {
            var panel = UIFactory.Panel("Quiz", safe, new Color(0.06f, 0.03f, 0.1f, 0.9f));
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(900, 160));
            quizGroup = panel.gameObject.AddComponent<CanvasGroup>();
            quizGroup.alpha = 0;
            quizGroup.blocksRaycasts = false;

            quizScore = UIFactory.Text("Skor", panel.transform, "", 26, new Color(1f, 0.75f, 0.3f), TextAlignmentOptions.Top, FontStyles.Bold);
            UIFactory.Place(quizScore.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(860, 40));
            quizText = UIFactory.Text("Soru", panel.transform, "", 42, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            UIFactory.Place(quizText.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(860, 96));
        }

        void BuildToolbar(RectTransform safe)
        {
            var bar = UIFactory.Panel("AracCubugu", safe, new Color(0.02f, 0.04f, 0.06f, 0.72f), UIFactory.Pill);
            toolbar = bar.rectTransform;
            toolbar.anchorMin = new Vector2(0, 0);
            toolbar.anchorMax = new Vector2(1, 0);
            toolbar.pivot = new Vector2(0.5f, 0);
            toolbar.sizeDelta = new Vector2(-56, 124);
            toolbar.anchoredPosition = new Vector2(0, -200);
            toolbarGroup = bar.gameObject.AddComponent<CanvasGroup>();
            toolbarGroup.alpha = 0;

            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 14, 14);
            layout.spacing = 10;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
        }

        /// <summary>Araç çubuğundaki düğmeleri kurar (nesneye göre değişir: Arduino'da Pinler, Akım ve Blink de var).</summary>
        public void SetButtons(IEnumerable<(string id, string label, Action action)> items)
        {
            foreach (Transform c in toolbar) Destroy(c.gameObject);
            buttons.Clear();
            foreach (var (id, label, action) in items)
            {
                UIFactory.Button(id, toolbar, label, () => action(), out var bg, out var text, 27);
                bg.sprite = UIFactory.Pill;
                var b = new ToggleButton { Background = bg, Text = text };
                b.Set(false);
                buttons[id] = b;
            }
        }

        public void SetButton(string id, bool on)
        {
            if (buttons.TryGetValue(id, out var b)) b.Set(on);
        }

        void BuildSheet(RectTransform safe)
        {
            var panel = UIFactory.Panel("BilgiKarti", safe, new Color(0.03f, 0.06f, 0.09f, 0.97f));
            sheet = panel.rectTransform;
            sheet.anchorMin = new Vector2(0, 0);
            sheet.anchorMax = new Vector2(1, 0);
            sheet.pivot = new Vector2(0.5f, 0);
            sheet.sizeDelta = new Vector2(-48, 600);
            sheet.anchoredPosition = new Vector2(0, -900);

            sheetBar = UIFactory.Solid("Renk", sheet, UIFactory.Accent);
            UIFactory.Place(sheetBar.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(120, 8));

            sheetIndex = UIFactory.Text("Sira", sheet, "", 26, UIFactory.TextDim, TextAlignmentOptions.Left, FontStyles.Bold);
            UIFactory.Place(sheetIndex.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(48, -48), new Vector2(700, 40));

            sheetTitle = UIFactory.Text("Baslik", sheet, "", 56, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            UIFactory.Place(sheetTitle.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(48, -90), new Vector2(820, 76));

            sheetDesc = UIFactory.Text("Aciklama", sheet, "", 34, new Color(0.9f, 0.95f, 1f), TextAlignmentOptions.TopLeft);
            var dRt = sheetDesc.rectTransform;
            dRt.anchorMin = new Vector2(0, 1);
            dRt.anchorMax = new Vector2(1, 1);
            dRt.pivot = new Vector2(0.5f, 1);
            dRt.offsetMin = new Vector2(48, -380);
            dRt.offsetMax = new Vector2(-48, -180);

            var factBox = UIFactory.Panel("BiliyorMuydun", sheet, new Color(0.25f, 0.9f, 1f, 0.1f));
            var fRt = factBox.rectTransform;
            fRt.anchorMin = new Vector2(0, 0);
            fRt.anchorMax = new Vector2(1, 0);
            fRt.pivot = new Vector2(0.5f, 0);
            fRt.offsetMin = new Vector2(36, 36);
            fRt.offsetMax = new Vector2(-36, 36 + 180);
            var factTitle = UIFactory.Text("Baslik", factBox.transform, "BİLİYOR MUYDUN?", 24, UIFactory.Accent, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            UIFactory.Place(factTitle.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -16), new Vector2(600, 36));
            sheetFact = UIFactory.Text("Yazi", factBox.transform, "", 30, Color.white, TextAlignmentOptions.TopLeft);
            var ftRt = sheetFact.rectTransform;
            ftRt.anchorMin = Vector2.zero;
            ftRt.anchorMax = Vector2.one;
            ftRt.offsetMin = new Vector2(28, 12);
            ftRt.offsetMax = new Vector2(-28, -56);

            UIFactory.Button("Kapat", sheet, "×", app.CloseInfo, out var closeBg, out var closeText, 60);
            closeBg.sprite = UIFactory.Circle;
            closeBg.color = new Color(1f, 1f, 1f, 0.1f);
            UIFactory.Place(closeBg.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-28, -28), new Vector2(88, 88));
            closeText.rectTransform.offsetMin = new Vector2(0, 6);
        }

        // ---- Uygulamanın çağırdığı yöntemler ----

        /// <summary>Nesne aranırken tarama halkasını göster, bulununca araç çubuğunu getir.</summary>
        public void SetTracking(bool tracked)
        {
            scanning = !tracked;
            toolbarVisible = tracked;
            toolbarGroup.interactable = tracked;
            toolbarGroup.blocksRaycasts = tracked;
        }

        public void ShowInfo(PartView part, int index, int total)
        {
            sheetOpen = true;
            sheetBar.color = part.color;
            sheetIndex.text = $"PARÇA {index + 1:00} / {total:00}";
            sheetIndex.color = part.color;
            sheetTitle.text = part.title;
            sheetDesc.text = part.description;
            sheetFact.text = part.funFact;
        }

        public void HideInfo() => sheetOpen = false;

        public void SetQuiz(bool visible, string question = null, string score = null)
        {
            quizVisible = visible;
            if (question != null) quizText.text = question;
            if (score != null) quizScore.text = score;
        }

        /// <summary>Blink kod panelini gösterir; çalışan satır vurgulanır (current &lt; 0: panel kapalı).</summary>
        public void SetCode(string[] lines, int current, bool ledOn)
        {
            codeVisible = current >= 0;
            if (!codeVisible) return;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                if (i == current)
                    sb.Append("<mark=#40E6FF33><color=#40E6FF>").Append(lines[i]).Append("</color></mark>");
                else
                    sb.Append("<color=#B8C7D1>").Append(lines[i]).Append("</color>");
                sb.Append('\n');
            }
            sb.Append(ledOn ? "<size=80%><color=#FF5A4A>● LED yanıyor</color></size>" : "<size=80%><color=#55606A>○ LED sönük</color></size>");
            codeText.text = sb.ToString();
        }

        public void Flash(Color c) => flash.color = new Color(c.r, c.g, c.b, 0.25f);

        void Update()
        {
            float dt = Time.deltaTime;

            scanGroup.alpha = Mathf.MoveTowards(scanGroup.alpha, scanning ? 1f : 0f, dt * 3f);
            scanArc.localRotation = Quaternion.Euler(0, 0, -Time.time * 240f);
            float breathe = 1f + Mathf.Sin(Time.time * 2.2f) * 0.06f;
            scanRing.localScale = new Vector3(breathe, breathe, 1f);

            var tp = toolbar.anchoredPosition;
            tp.y = Mathf.Lerp(tp.y, toolbarVisible ? 28f : -200f, 1f - Mathf.Exp(-dt * 10f));
            toolbar.anchoredPosition = tp;
            toolbarGroup.alpha = Mathf.MoveTowards(toolbarGroup.alpha, toolbarVisible ? 1f : 0f, dt * 4f);

            var pos = sheet.anchoredPosition;
            pos.y = Mathf.Lerp(pos.y, sheetOpen ? 24f : -900f, 1f - Mathf.Exp(-dt * 12f));
            sheet.anchoredPosition = pos;

            quizGroup.alpha = Mathf.MoveTowards(quizGroup.alpha, quizVisible ? 1f : 0f, dt * 4f);
            codeGroup.alpha = Mathf.MoveTowards(codeGroup.alpha, codeVisible && !quizVisible ? 1f : 0f, dt * 4f);

            var fc = flash.color;
            fc.a = Mathf.MoveTowards(fc.a, 0f, dt * 0.6f);
            flash.color = fc;
        }
    }

    /// <summary>Çentik ve yuvarlak köşeli ekranlarda arayüzü güvenli alanın içinde tutar.</summary>
    public class SafeArea : MonoBehaviour
    {
        Rect last;

        void Update()
        {
            var area = Screen.safeArea;
            if (area == last || Screen.width == 0 || Screen.height == 0) return;
            last = area;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            rt.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
