using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using ElementalHexTactics3D.Combat;

namespace ElementalHexTactics3D.UI
{
    /// <summary>
    /// RPG Maker-style audio settings popup (BGM / SFX volume sliders + mute toggle).
    /// Features ornate 9-slice Windowskin box, custom fantasy buttons, and dynamic theme skinning
    /// (Holy Synod or Demon Rebellion). Opens via AudioSettingsPanelUI.Open().
    /// </summary>
    public class AudioSettingsPanelUI : MonoBehaviour
    {
        private static AudioSettingsPanelUI instance;

        private GameObject backdrop;
        private GameObject panelSolidBg;
        private GameObject panel;
        private Slider bgmSlider;
        private Slider sfxSlider;
        private Toggle muteToggle;
        private Text titleLabel;
        private Image dividerImage;
        private Text bgmLabel;
        private Text sfxLabel;
        private Button closeBtn;
        private Image bgmFillImg;
        private Image sfxFillImg;
        private Font font;

        public static bool IsOpen => instance != null && instance.panel != null && instance.panel.activeSelf;

        public static void Open()
        {
            if (instance == null)
            {
                var go = new GameObject("AudioSettingsPanelUI");
                instance = go.AddComponent<AudioSettingsPanelUI>();
                DontDestroyOnLoad(go);
            }
            instance.Show();
        }

        public static void Close()
        {
            if (instance != null)
            {
                if (instance.panel != null) instance.panel.SetActive(false);
                if (instance.panelSolidBg != null) instance.panelSolidBg.SetActive(false);
                if (instance.backdrop != null) instance.backdrop.SetActive(false);
            }
        }

        private void Show()
        {
            var sm = SoundManager3D.Instance;
            bgmSlider.SetValueWithoutNotify(sm.BgmVolume);
            sfxSlider.SetValueWithoutNotify(sm.SfxVolume);
            muteToggle.SetIsOnWithoutNotify(sm.IsMuted);
            RefreshLabels();
            ApplyTheme();

            if (backdrop != null) backdrop.SetActive(true);
            if (panelSolidBg != null) panelSolidBg.SetActive(true);
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
        }

        private void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            EnsureEventSystem();
            BuildUI();
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
            DontDestroyOnLoad(es);
        }

        private void BuildUI()
        {
            var canvasObj = new GameObject("AudioCanvas");
            canvasObj.transform.SetParent(transform, false);
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 950;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // 1. Dimmer Backdrop (covers full screen behind popup)
            backdrop = new GameObject("AudioBackdrop", typeof(RectTransform), typeof(Image), typeof(Button));
            backdrop.transform.SetParent(canvasObj.transform, false);
            var bRt = backdrop.GetComponent<RectTransform>();
            bRt.anchorMin = Vector2.zero;
            bRt.anchorMax = Vector2.one;
            bRt.sizeDelta = Vector2.zero;
            bRt.anchoredPosition = Vector2.zero;
            var bImg = backdrop.GetComponent<Image>();
            bImg.color = new Color(0.02f, 0.03f, 0.05f, 0.85f);
            var bBtn = backdrop.GetComponent<Button>();
            bBtn.transition = Selectable.Transition.None;
            bBtn.onClick.AddListener(Close);

            // 2. Solid Opaque Card Fill (guarantees non-transparent background behind frame)
            panelSolidBg = new GameObject("AudioPanel_SolidBg", typeof(RectTransform), typeof(Image));
            panelSolidBg.transform.SetParent(canvasObj.transform, false);
            var sRt = panelSolidBg.GetComponent<RectTransform>();
            sRt.anchorMin = sRt.anchorMax = sRt.pivot = new Vector2(0.5f, 0.5f);
            sRt.sizeDelta = new Vector2(480f, 420f);
            sRt.anchoredPosition = Vector2.zero;
            var sImg = panelSolidBg.GetComponent<Image>();
            sImg.color = new Color(0.07f, 0.08f, 0.12f, 1.0f);
            sImg.raycastTarget = true;

            // 3. Main Windowskin Card Frame (RPG Maker modular box)
            panel = new GameObject("AudioPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasObj.transform, false);
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(480f, 420f);
            prt.anchoredPosition = Vector2.zero;

            var pImg = panel.GetComponent<Image>();
            pImg.color = Color.white;
            pImg.type = Image.Type.Sliced;

            // 4. Header Title & Divider
            titleLabel = MakeLabel(panel.transform, "Title", "✦ SOUND & MUSIC SETTINGS ✦", 20, new Vector2(0, -32), new Vector2(400, 32), TextAnchor.MiddleCenter);
            titleLabel.fontStyle = FontStyle.Bold;
            var tShadow = titleLabel.gameObject.AddComponent<Shadow>();
            tShadow.effectColor = new Color(0f, 0f, 0f, 0.95f);
            tShadow.effectDistance = new Vector2(1.5f, -1.5f);

            var divObj = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            divObj.transform.SetParent(panel.transform, false);
            var divRt = divObj.GetComponent<RectTransform>();
            divRt.anchorMin = divRt.anchorMax = divRt.pivot = new Vector2(0.5f, 1f);
            divRt.sizeDelta = new Vector2(390f, 2f);
            divRt.anchoredPosition = new Vector2(0f, -68f);
            dividerImage = divObj.GetComponent<Image>();
            dividerImage.color = new Color(0.85f, 0.72f, 0.38f, 0.6f);

            // 5. Sliders & Labels
            bgmLabel = MakeLabel(panel.transform, "BgmLabel", "", 16, new Vector2(0, -95), new Vector2(380, 24), TextAnchor.MiddleLeft);
            bgmSlider = MakeSlider(panel.transform, new Vector2(0, -128), out bgmFillImg);

            sfxLabel = MakeLabel(panel.transform, "SfxLabel", "", 16, new Vector2(0, -170), new Vector2(380, 24), TextAnchor.MiddleLeft);
            sfxSlider = MakeSlider(panel.transform, new Vector2(0, -203), out sfxFillImg);

            muteToggle = MakeToggle(panel.transform, "Mute all audio", new Vector2(0, -255));

            // 6. Fantasy RPG Maker Close Button
            closeBtn = MakeButton(panel.transform, "CloseButton", "🔙 CLOSE", new Vector2(220, 48));
            var cbRt = closeBtn.GetComponent<RectTransform>();
            cbRt.anchorMin = cbRt.anchorMax = cbRt.pivot = new Vector2(0.5f, 1f);
            cbRt.anchoredPosition = new Vector2(0, -325);
            closeBtn.onClick.AddListener(Close);

            // 7. Connect SoundManager3D logic
            var sm = SoundManager3D.Instance;
            bgmSlider.SetValueWithoutNotify(sm.BgmVolume);
            sfxSlider.SetValueWithoutNotify(sm.SfxVolume);
            muteToggle.SetIsOnWithoutNotify(sm.IsMuted);
            RefreshLabels();

            bgmSlider.onValueChanged.AddListener(v => { SoundManager3D.Instance.BgmVolume = v; ApplyBgmNow(v); RefreshLabels(); });
            sfxSlider.onValueChanged.AddListener(v => { SoundManager3D.Instance.SfxVolume = v; RefreshLabels(); });
            muteToggle.onValueChanged.AddListener(m => SoundManager3D.Instance.IsMuted = m);

            backdrop.SetActive(false);
            panelSolidBg.SetActive(false);
            panel.SetActive(false);
        }

        private void ApplyTheme()
        {
            bool isDemon = false;
            if (TitleMenuCanvasUI.Instance != null)
            {
                isDemon = (TitleMenuCanvasUI.Instance.CurrentThemeMode == TitleMenuCanvasUI.TitleThemeMode.DemonLordRebellion);
            }
            else if (ElementalHexTactics3D.Campaign.CampaignSaveManager.HasSaveFile())
            {
                isDemon = ElementalHexTactics3D.Campaign.CampaignSaveManager.HasTransmigrated();
            }

            Sprite boxSprite = isDemon
                ? TitleMenuCanvasUI.LoadBoxSprite("UI_WindowFrame_RPG")
                : TitleMenuCanvasUI.LoadBoxSprite("UI_Box_Holy_9Slice");

            Sprite btnSprite = isDemon
                ? TitleMenuCanvasUI.LoadButtonSprite("UI_Button_Demon")
                : TitleMenuCanvasUI.LoadButtonSprite("UI_Button_Holy");

            if (panelSolidBg != null)
            {
                var sImg = panelSolidBg.GetComponent<Image>();
                if (sImg != null)
                {
                    sImg.color = isDemon
                        ? new Color(0.06f, 0.05f, 0.09f, 1.0f)
                        : new Color(0.07f, 0.08f, 0.12f, 1.0f);
                }
            }

            if (panel != null && boxSprite != null)
            {
                var pImg = panel.GetComponent<Image>();
                if (pImg != null)
                {
                    pImg.sprite = boxSprite;
                    pImg.type = Image.Type.Sliced;
                    pImg.preserveAspect = false;
                    pImg.color = Color.white;
                }
            }

            if (dividerImage != null)
            {
                dividerImage.color = isDemon
                    ? new Color(0.95f, 0.45f, 0.25f, 0.6f)
                    : new Color(0.85f, 0.72f, 0.38f, 0.6f);
            }

            if (titleLabel != null)
            {
                titleLabel.color = isDemon
                    ? new Color(1f, 0.78f, 0.25f)
                    : new Color(1f, 0.88f, 0.38f);
            }

            if (closeBtn != null && btnSprite != null)
            {
                var bImg = closeBtn.GetComponent<Image>();
                if (bImg != null)
                {
                    bImg.sprite = btnSprite;
                    bImg.type = (btnSprite.border != Vector4.zero) ? Image.Type.Sliced : Image.Type.Simple;
                    bImg.preserveAspect = false;
                    bImg.color = Color.white;
                }

                var bTxt = closeBtn.GetComponentInChildren<Text>();
                if (bTxt != null)
                {
                    bTxt.color = isDemon ? new Color(1f, 0.90f, 0.85f) : new Color(1f, 0.94f, 0.75f);
                    bTxt.fontSize = 17;
                    bTxt.fontStyle = FontStyle.Bold;
                    bTxt.alignment = TextAnchor.MiddleCenter;

                    var s = bTxt.GetComponent<Shadow>();
                    if (s == null) s = bTxt.gameObject.AddComponent<Shadow>();
                    s.effectColor = new Color(0f, 0f, 0f, 0.95f);
                    s.effectDistance = new Vector2(1f, -1f);
                }
            }

            Color fillColor = isDemon
                ? new Color(0.98f, 0.45f, 0.20f, 1f)
                : new Color(0.95f, 0.75f, 0.25f, 1f);

            if (bgmFillImg != null) bgmFillImg.color = fillColor;
            if (sfxFillImg != null) sfxFillImg.color = fillColor;
        }

        private static void ApplyBgmNow(float v)
        {
            foreach (var src in SoundManager3D.Instance.GetComponents<AudioSource>())
            {
                if (src.loop) src.volume = v;
            }
        }

        private void RefreshLabels()
        {
            bgmLabel.text = "🎵 Music (BGM):  " + Mathf.RoundToInt(bgmSlider.value * 100f) + "%";
            sfxLabel.text = "⚔️ Effects (SFX):  " + Mathf.RoundToInt(sfxSlider.value * 100f) + "%";
        }

        private Button MakeButton(Transform parent, string name, string text, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = Color.white;
            MakeLabel(go.transform, "Text", text, 17, Vector2.zero, size, TextAnchor.MiddleCenter, true);
            return go.GetComponent<Button>();
        }

        private Text MakeLabel(Transform parent, string name, string text, int fontSize, Vector2 pos, Vector2 size, TextAnchor anchor, bool stretch = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            if (stretch)
            {
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
            else
            {
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = size; rt.anchoredPosition = pos;
            }
            var t = go.GetComponent<Text>();
            t.font = font; t.fontSize = fontSize; t.alignment = anchor; t.text = text;
            t.color = new Color(1f, 0.93f, 0.7f);
            t.raycastTarget = false;
            return t;
        }

        private Slider MakeSlider(Transform parent, Vector2 pos, out Image fillImg)
        {
            var root = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            root.transform.SetParent(parent, false);
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(380, 26); rt.anchoredPosition = pos;

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(root.transform, false);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0, 0.3f); bgRt.anchorMax = new Vector2(1, 0.7f);
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0.08f, 0.07f, 0.12f, 0.95f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(root.transform, false);
            var faRt = fillArea.GetComponent<RectTransform>();
            faRt.anchorMin = new Vector2(0, 0.3f); faRt.anchorMax = new Vector2(1, 0.7f);
            faRt.offsetMin = new Vector2(5, 0); faRt.offsetMax = new Vector2(-15, 0);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            fillImg = fill.GetComponent<Image>();
            fillImg.color = new Color(0.95f, 0.75f, 0.25f, 1f);
            fill.GetComponent<RectTransform>().sizeDelta = new Vector2(10, 0);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(root.transform, false);
            var haRt = handleArea.GetComponent<RectTransform>();
            haRt.anchorMin = Vector2.zero; haRt.anchorMax = Vector2.one;
            haRt.offsetMin = new Vector2(10, 0); haRt.offsetMax = new Vector2(-10, 0);
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            handle.GetComponent<RectTransform>().sizeDelta = new Vector2(22, 0);
            handle.GetComponent<Image>().color = new Color(1f, 0.95f, 0.85f, 1f);

            var s = root.GetComponent<Slider>();
            s.fillRect = fill.GetComponent<RectTransform>();
            s.handleRect = handle.GetComponent<RectTransform>();
            s.targetGraphic = handle.GetComponent<Image>();
            s.direction = Slider.Direction.LeftToRight;
            s.minValue = 0f; s.maxValue = 1f;
            return s;
        }

        private Toggle MakeToggle(Transform parent, string label, Vector2 pos)
        {
            var root = new GameObject("MuteToggle", typeof(RectTransform), typeof(Toggle));
            root.transform.SetParent(parent, false);
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(380, 28); rt.anchoredPosition = pos;

            var box = new GameObject("Box", typeof(RectTransform), typeof(Image));
            box.transform.SetParent(root.transform, false);
            var brt = box.GetComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0, 0.5f);
            brt.sizeDelta = new Vector2(24, 24); brt.anchoredPosition = Vector2.zero;
            box.GetComponent<Image>().color = new Color(0.12f, 0.10f, 0.16f, 0.95f);

            var check = new GameObject("Check", typeof(RectTransform), typeof(Image));
            check.transform.SetParent(box.transform, false);
            var crt = check.GetComponent<RectTransform>();
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = new Vector2(4, 4); crt.offsetMax = new Vector2(-4, -4);
            check.GetComponent<Image>().color = new Color(0.95f, 0.75f, 0.25f, 1f);

            var txt = MakeLabel(root.transform, "Label", label, 16, new Vector2(34, 0), new Vector2(340, 28), TextAnchor.MiddleLeft, true);
            txt.GetComponent<RectTransform>().offsetMin = new Vector2(36, 0);

            var tg = root.GetComponent<Toggle>();
            tg.targetGraphic = box.GetComponent<Image>();
            tg.graphic = check.GetComponent<Image>();
            return tg;
        }
    }
}
