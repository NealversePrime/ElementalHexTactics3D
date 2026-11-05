using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ElementalHexTactics3D.Combat;
using ElementalHexTactics3D.UI;

namespace ElementalHexTactics3D.Tutorial
{
    /// <summary>
    /// Crisp, high-contrast uGUI Tutorial Guidance Banner.
    /// Pinned below the Turn Ribbon at top-center.
    /// Displays the gamer protagonist's diegetic inner monologue,
    /// tactical step progression badges, and high-visibility player hints.
    /// Guarantees clean single-instance creation with zero stacked/overlapping text.
    /// </summary>
    public class TutorialGuidanceBannerUI : MonoBehaviour
    {
        public static TutorialGuidanceBannerUI Instance { get; private set; }

        [Header("UI Roots")]
        [SerializeField] private GameObject rootBanner;
        [SerializeField] private Image bannerBackground;
        [SerializeField] private Outline bannerOutline;

        [Header("Text Fields")]
        [SerializeField] private Text txtStepBadge;
        [SerializeField] private Text txtMonologue;
        [SerializeField] private Text txtHint;
        [SerializeField] private Text txtSpeakerTag;
        [SerializeField] private Text txtAvatarIcon;
        [SerializeField] private Image imgAvatarPortrait;

        private Coroutine pulseCoroutine;
        private Canvas rootCanvas;
        private bool isBuilt = false;
        private CanvasGroup bannerCanvasGroup;

        public void SetBannerVisibility(bool visible)
        {
            if (bannerCanvasGroup == null)
            {
                bannerCanvasGroup = GetComponent<CanvasGroup>();
                if (bannerCanvasGroup == null)
                {
                    bannerCanvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (bannerCanvasGroup != null)
            {
                bannerCanvasGroup.alpha = visible ? 1f : 0f;
                bannerCanvasGroup.blocksRaycasts = visible;
                bannerCanvasGroup.interactable = visible;
            }
        }

        public bool IsScreenPointOccluded(Vector2 screenPos)
        {
            if (rootBanner == null || !rootBanner.activeInHierarchy) return false;
            if (bannerCanvasGroup != null && bannerCanvasGroup.alpha <= 0.01f) return false;
            RectTransform rt = rootBanner.GetComponent<RectTransform>();
            return rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos);
        }

        public static void SafeHideGuidance()
        {
            if (Instance != null)
            {
                Instance.HideGuidance();
            }
        }

        private static GameObject CreateUI(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            if (parent != null)
            {
                obj.transform.SetParent(parent, false);
            }
            return obj;
        }

        public static TutorialGuidanceBannerUI EnsureInstance()
        {
            if (Instance != null && Instance.GetComponent<RectTransform>() != null)
            {
                return Instance;
            }

            TutorialGuidanceBannerUI found = FindAnyObjectByType<TutorialGuidanceBannerUI>(FindObjectsInactive.Include);
            if (found != null)
            {
                if (found.GetComponent<RectTransform>() != null)
                {
                    Instance = found;
                    return found;
                }
                else
                {
                    // Destroy legacy/malformed instance lacking RectTransform
                    if (Application.isPlaying) Destroy(found.gameObject);
                    else DestroyImmediate(found.gameObject);
                }
            }

            Canvas canvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                GameObject canvasObj = CreateUI("TutorialCanvas", null);
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Create with RectTransform from the start!
            GameObject bannerObj = CreateUI("Panel_TutorialGuidanceBanner", canvas.transform);
            // Note: AddComponent invokes Awake(), which builds the hierarchy cleanly once!
            Instance = bannerObj.AddComponent<TutorialGuidanceBannerUI>();
            return Instance;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (!isBuilt)
            {
                Canvas canvas = GetComponentInParent<Canvas>() ?? FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
                if (canvas != null)
                {
                    BuildUIHierarchy(canvas);
                }
            }
        }

        private void OnEnable()
        {
            StoryDialogueUI.OnDialogueStarted += HandleDialogueStarted;
            StoryDialogueUI.OnDialogueFinished += HandleDialogueFinished;
        }

        private void OnDisable()
        {
            StoryDialogueUI.OnDialogueStarted -= HandleDialogueStarted;
            StoryDialogueUI.OnDialogueFinished -= HandleDialogueFinished;
        }

        private void OnDestroy()
        {
            StoryDialogueUI.OnDialogueStarted -= HandleDialogueStarted;
            StoryDialogueUI.OnDialogueFinished -= HandleDialogueFinished;
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void HandleDialogueStarted()
        {
            SetBannerVisibility(false);
        }

        private void HandleDialogueFinished()
        {
            SetBannerVisibility(true);
        }

        private void Update()
        {
            if (StoryDialogueUI.Instance != null && StoryDialogueUI.Instance.IsPlayingDialogue)
            {
                SetBannerVisibility(false);
            }
            else
            {
                SetBannerVisibility(true);
            }
        }

        private void BuildUIHierarchy(Canvas canvas)
        {
            if (isBuilt && txtStepBadge != null && txtMonologue != null && txtHint != null)
            {
                return;
            }

            rootCanvas = canvas;
            transform.SetParent(canvas.transform, false);

            // Clean up any stale or duplicate child objects before building
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#if UNITY_EDITOR
            Font fontBold = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Font_Bold.ttf");
            if (fontBold != null) font = fontBold;
#endif

            // Main Banner Rect: Top-Center, anchored below Turn Ribbon
            RectTransform rect = GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = gameObject.AddComponent<RectTransform>();
            }
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -76f);
            rect.sizeDelta = new Vector2(1040f, 136f); // Generous, highly legible dimensions (upgraded from 720x96)

            rootBanner = gameObject;

            // Background Frame (Modern Sleek RPG Window Skin)
            Sprite bgSprite = ElementalHexTactics3D.Units.TacticalUnitSpawner.LoadUISprite("UI_WindowBg_RPG.png");
            Sprite frameSprite = ElementalHexTactics3D.Units.TacticalUnitSpawner.LoadUISprite("UI_WindowFrame_RPG.png");

            bannerBackground = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            if (bgSprite != null)
            {
                bannerBackground.sprite = bgSprite;
                bannerBackground.type = Image.Type.Sliced;
            }
            bannerBackground.color = new Color(0.06f, 0.08f, 0.12f, 0.98f); // Deep solid slate
            bannerBackground.raycastTarget = false;

            // Sleek Outer Frame Overlay
            Transform frameTrans = transform.Find("Frame_Overlay");
            GameObject frameObj = frameTrans != null ? frameTrans.gameObject : null;
            if (frameObj == null)
            {
                frameObj = CreateUI("Frame_Overlay", transform);
                frameObj.transform.SetAsFirstSibling();
            }
            RectTransform foRect = frameObj.GetComponent<RectTransform>();
            foRect.anchorMin = Vector2.zero;
            foRect.anchorMax = Vector2.one;
            foRect.offsetMin = Vector2.zero;
            foRect.offsetMax = Vector2.zero;
            Image foImg = frameObj.GetComponent<Image>() ?? frameObj.AddComponent<Image>();
            if (frameSprite != null)
            {
                foImg.sprite = frameSprite;
                foImg.type = Image.Type.Sliced;
                foImg.color = Color.white;
                foImg.raycastTarget = false;
            }

            // Glowing border & Drop Shadow
            bannerOutline = gameObject.GetComponent<Outline>() ?? gameObject.AddComponent<Outline>();
            bannerOutline.effectColor = new Color(1.0f, 0.82f, 0.25f, 0.85f); // Gold border
            bannerOutline.effectDistance = new Vector2(2f, -2f);

            Shadow bannerShadow = gameObject.GetComponent<Shadow>() ?? gameObject.AddComponent<Shadow>();
            bannerShadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            bannerShadow.effectDistance = new Vector2(3f, -3f);

            // Left Avatar Box (Icon + Speaker Tag)
            GameObject avatarBox = CreateUI("Box_Avatar", transform);
            RectTransform avRect = avatarBox.GetComponent<RectTransform>();
            avRect.anchorMin = new Vector2(0f, 0f);
            avRect.anchorMax = new Vector2(0f, 1f);
            avRect.pivot = new Vector2(0f, 0.5f);
            avRect.anchoredPosition = new Vector2(12f, 0f);
            avRect.sizeDelta = new Vector2(110f, -16f);

            Image avBg = avatarBox.AddComponent<Image>();
            avBg.color = new Color(0.10f, 0.14f, 0.22f, 0.95f);
            avBg.raycastTarget = false;

            Outline avOutline = avatarBox.AddComponent<Outline>();
            avOutline.effectColor = new Color(0.35f, 0.85f, 1.0f, 0.7f);
            avOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Avatar Portrait Image
            GameObject imgObj = CreateUI("Img_AvatarPortrait", avatarBox.transform);
            RectTransform imgRect = imgObj.GetComponent<RectTransform>();
            imgRect.anchorMin = new Vector2(0.06f, 0.32f);
            imgRect.anchorMax = new Vector2(0.94f, 0.98f);
            imgRect.anchoredPosition = Vector2.zero;
            imgRect.sizeDelta = Vector2.zero;

            imgAvatarPortrait = imgObj.AddComponent<Image>();
            imgAvatarPortrait.preserveAspect = true;
            imgAvatarPortrait.raycastTarget = false;
            Sprite mcPort = ElementalHexTactics3D.Units.TacticalUnitSpawner.LoadPortraitSprite("mc_silhouette.png");
            if (mcPort != null)
            {
                imgAvatarPortrait.sprite = mcPort;
                imgAvatarPortrait.color = Color.white;
            }
            else
            {
                imgAvatarPortrait.color = Color.clear;
            }

            // Avatar Icon / Emoji (fallback)
            GameObject iconObj = CreateUI("Txt_AvatarIcon", avatarBox.transform);
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.32f);
            iconRect.anchorMax = new Vector2(1f, 1f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = Vector2.zero;

            txtAvatarIcon = iconObj.AddComponent<Text>();
            txtAvatarIcon.font = font;
            txtAvatarIcon.fontSize = 28;
            txtAvatarIcon.alignment = TextAnchor.MiddleCenter;
            txtAvatarIcon.text = mcPort != null ? "" : "✦";
            txtAvatarIcon.color = Color.white;
            txtAvatarIcon.raycastTarget = false;

            // Speaker Tag
            GameObject tagObj = CreateUI("Txt_SpeakerTag", avatarBox.transform);
            RectTransform tagRect = tagObj.GetComponent<RectTransform>();
            tagRect.anchorMin = new Vector2(0f, 0f);
            tagRect.anchorMax = new Vector2(1f, 0.30f);
            tagRect.anchoredPosition = Vector2.zero;
            tagRect.sizeDelta = Vector2.zero;

            txtSpeakerTag = tagObj.AddComponent<Text>();
            txtSpeakerTag.font = font;
            txtSpeakerTag.fontSize = 12;
            txtSpeakerTag.fontStyle = FontStyle.Bold;
            txtSpeakerTag.alignment = TextAnchor.MiddleCenter;
            txtSpeakerTag.text = "MC (TACTICAL THOUGHT)";
            txtSpeakerTag.color = new Color(0.35f, 0.85f, 1.0f); // Cyan
            txtSpeakerTag.raycastTarget = false;

            // Right Content Area (Step Badge + Monologue + Hint in distinct non-overlapping vertical slots)
            GameObject contentBox = CreateUI("Box_Content", transform);
            RectTransform contRect = contentBox.GetComponent<RectTransform>();
            contRect.anchorMin = new Vector2(0f, 0f);
            contRect.anchorMax = new Vector2(1f, 1f);
            contRect.pivot = new Vector2(0f, 0.5f);
            contRect.anchoredPosition = new Vector2(136f, 0f);
            contRect.sizeDelta = new Vector2(-152f, -14f);

            // Slot 1: Step Badge (Top: Height 26px)
            GameObject stepObj = CreateUI("Txt_StepBadge", contentBox.transform);
            RectTransform stepRect = stepObj.GetComponent<RectTransform>();
            stepRect.anchorMin = new Vector2(0f, 1f);
            stepRect.anchorMax = new Vector2(1f, 1f);
            stepRect.pivot = new Vector2(0f, 1f);
            stepRect.anchoredPosition = new Vector2(0f, 0f);
            stepRect.sizeDelta = new Vector2(0f, 26f);

            txtStepBadge = stepObj.AddComponent<Text>();
            txtStepBadge.font = font;
            txtStepBadge.fontSize = 16; // Upgraded from 13 -> 16
            txtStepBadge.fontStyle = FontStyle.Bold;
            txtStepBadge.alignment = TextAnchor.MiddleLeft;
            txtStepBadge.text = "";
            txtStepBadge.color = new Color(1.0f, 0.88f, 0.35f); // Gold
            txtStepBadge.raycastTarget = false;

            // Slot 2: Monologue Body (Middle: Y from -28px, Height 54px, wraps cleanly)
            GameObject monoObj = CreateUI("Txt_Monologue", contentBox.transform);
            RectTransform monoRect = monoObj.GetComponent<RectTransform>();
            monoRect.anchorMin = new Vector2(0f, 1f);
            monoRect.anchorMax = new Vector2(1f, 1f);
            monoRect.pivot = new Vector2(0f, 1f);
            monoRect.anchoredPosition = new Vector2(0f, -28f);
            monoRect.sizeDelta = new Vector2(0f, 54f);

            txtMonologue = monoObj.AddComponent<Text>();
            txtMonologue.font = font;
            txtMonologue.fontSize = 15; // Upgraded from 12 -> 15
            txtMonologue.lineSpacing = 1.20f;
            txtMonologue.fontStyle = FontStyle.Normal;
            txtMonologue.alignment = TextAnchor.UpperLeft;
            txtMonologue.horizontalOverflow = HorizontalWrapMode.Wrap;
            txtMonologue.verticalOverflow = VerticalWrapMode.Truncate;
            txtMonologue.text = "";
            txtMonologue.color = Color.white;
            txtMonologue.raycastTarget = false;

            // Slot 3: Hint Footer (Bottom: Height 26px)
            GameObject hintObj = CreateUI("Txt_Hint", contentBox.transform);
            RectTransform hintRect = hintObj.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0f, 0f);
            hintRect.anchorMax = new Vector2(1f, 0f);
            hintRect.pivot = new Vector2(0f, 0f);
            hintRect.anchoredPosition = new Vector2(0f, 2f);
            hintRect.sizeDelta = new Vector2(0f, 26f);

            txtHint = hintObj.AddComponent<Text>();
            txtHint.font = font;
            txtHint.fontSize = 14; // Upgraded from 11 -> 14
            txtHint.fontStyle = FontStyle.Bold;
            txtHint.alignment = TextAnchor.MiddleLeft;
            txtHint.horizontalOverflow = HorizontalWrapMode.Wrap;
            txtHint.verticalOverflow = VerticalWrapMode.Truncate;
            txtHint.text = "";
            txtHint.color = new Color(0.40f, 0.95f, 0.65f); // Mint green
            txtHint.raycastTarget = false;

            isBuilt = true;
            gameObject.SetActive(false);
        }

        public void ApplyLayoutSizing()
        {
            RectTransform rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -76f);
                rect.sizeDelta = new Vector2(1040f, 136f);
            }

            Transform avBox = transform.Find("Box_Avatar");
            if (avBox != null)
            {
                RectTransform avRect = avBox.GetComponent<RectTransform>();
                if (avRect != null)
                {
                    avRect.sizeDelta = new Vector2(110f, -16f);
                    avRect.anchoredPosition = new Vector2(12f, 0f);
                }
            }

            if (txtSpeakerTag != null)
            {
                txtSpeakerTag.fontSize = 12;
            }

            Transform contBox = transform.Find("Box_Content");
            if (contBox != null)
            {
                RectTransform contRect = contBox.GetComponent<RectTransform>();
                if (contRect != null)
                {
                    contRect.anchoredPosition = new Vector2(136f, 0f);
                    contRect.sizeDelta = new Vector2(-152f, -14f);
                }
            }

            if (txtStepBadge != null)
            {
                RectTransform stepRect = txtStepBadge.GetComponent<RectTransform>();
                if (stepRect != null)
                {
                    stepRect.sizeDelta = new Vector2(0f, 26f);
                }
                txtStepBadge.fontSize = 16;
            }

            if (txtMonologue != null)
            {
                RectTransform monoRect = txtMonologue.GetComponent<RectTransform>();
                if (monoRect != null)
                {
                    monoRect.anchoredPosition = new Vector2(0f, -28f);
                    monoRect.sizeDelta = new Vector2(0f, 54f);
                }
                txtMonologue.fontSize = 15;
                txtMonologue.lineSpacing = 1.20f;
            }

            if (txtHint != null)
            {
                RectTransform hintRect = txtHint.GetComponent<RectTransform>();
                if (hintRect != null)
                {
                    hintRect.anchoredPosition = new Vector2(0f, 2f);
                    hintRect.sizeDelta = new Vector2(0f, 26f);
                }
                txtHint.fontSize = 14;
                txtHint.fontStyle = FontStyle.Bold;
            }
        }

        /// <summary>
        /// Displays the guidance banner with animated entrance pop and sound effect.
        /// </summary>
        public void ShowGuidance(string stepBadge, string monologue, string hint, Color? badgeColor = null)
        {
            if (!isBuilt || rootBanner == null || txtStepBadge == null)
            {
                Canvas canvas = GetComponentInParent<Canvas>() ?? FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
                if (canvas != null) BuildUIHierarchy(canvas);
            }

            ApplyLayoutSizing();

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            if (txtStepBadge != null)
            {
                txtStepBadge.text = stepBadge;
                txtStepBadge.color = badgeColor ?? new Color(1.0f, 0.88f, 0.35f);
            }

            if (txtMonologue != null)
            {
                txtMonologue.text = $"\"{monologue}\"";
            }

            if (txtHint != null)
            {
                txtHint.text = $"👉 {hint}";
            }

            // Start gentle outline pulse
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
            pulseCoroutine = StartCoroutine(AnimatePopAndPulse(badgeColor ?? new Color(1.0f, 0.85f, 0.25f)));
        }

        public void HideGuidance()
        {
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }
            gameObject.SetActive(false);
        }

        private IEnumerator AnimatePopAndPulse(Color targetColor)
        {
            RectTransform rt = GetComponent<RectTransform>();
            Vector3 originalScale = Vector3.one;

            // Subtle scale pop 1.04 -> 1.0
            float popElapsed = 0f;
            float popDuration = 0.16f;
            while (popElapsed < popDuration)
            {
                popElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(popElapsed / popDuration);
                float s = Mathf.Lerp(1.04f, 1.0f, Mathf.Sin(t * Mathf.PI * 0.5f));
                if (rt != null) rt.localScale = Vector3.one * s;
                yield return null;
            }
            if (rt != null) rt.localScale = originalScale;

            // Subtle continuous breathing glow
            while (gameObject.activeInHierarchy)
            {
                float glow = (Mathf.Sin(Time.unscaledTime * 5f) + 1f) * 0.5f;
                Color glowCol = Color.Lerp(targetColor * 0.75f, targetColor * 1.25f, glow);
                if (bannerOutline != null)
                {
                    bannerOutline.effectColor = new Color(glowCol.r, glowCol.g, glowCol.b, 0.85f);
                }
                yield return null;
            }
        }
    }
}
