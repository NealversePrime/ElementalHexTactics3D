using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ElementalHexTactics3D.Combat;

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

        private Coroutine pulseCoroutine;
        private Canvas rootCanvas;
        private bool isBuilt = false;

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

            TutorialGuidanceBannerUI found = FindFirstObjectByType<TutorialGuidanceBannerUI>(FindObjectsInactive.Include);
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

            Canvas canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
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
                Canvas canvas = GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
                if (canvas != null)
                {
                    BuildUIHierarchy(canvas);
                }
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
            rect.sizeDelta = new Vector2(720f, 96f);

            rootBanner = gameObject;

            // Background Frame
            bannerBackground = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            bannerBackground.color = new Color(0.06f, 0.08f, 0.12f, 0.96f); // Deep solid slate
            bannerBackground.raycastTarget = false;

            // Glowing border
            bannerOutline = gameObject.GetComponent<Outline>() ?? gameObject.AddComponent<Outline>();
            bannerOutline.effectColor = new Color(1.0f, 0.82f, 0.25f, 0.85f); // Gold border
            bannerOutline.effectDistance = new Vector2(2f, -2f);

            // Left Avatar Box (Icon + Speaker Tag)
            GameObject avatarBox = CreateUI("Box_Avatar", transform);
            RectTransform avRect = avatarBox.GetComponent<RectTransform>();
            avRect.anchorMin = new Vector2(0f, 0f);
            avRect.anchorMax = new Vector2(0f, 1f);
            avRect.pivot = new Vector2(0f, 0.5f);
            avRect.anchoredPosition = new Vector2(10f, 0f);
            avRect.sizeDelta = new Vector2(85f, -12f);

            Image avBg = avatarBox.AddComponent<Image>();
            avBg.color = new Color(0.10f, 0.14f, 0.22f, 0.95f);
            avBg.raycastTarget = false;

            Outline avOutline = avatarBox.AddComponent<Outline>();
            avOutline.effectColor = new Color(0.35f, 0.85f, 1.0f, 0.7f);
            avOutline.effectDistance = new Vector2(1f, -1f);

            // Avatar Icon / Emoji
            GameObject iconObj = CreateUI("Txt_AvatarIcon", avatarBox.transform);
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.35f);
            iconRect.anchorMax = new Vector2(1f, 1f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = Vector2.zero;

            txtAvatarIcon = iconObj.AddComponent<Text>();
            txtAvatarIcon.font = font;
            txtAvatarIcon.fontSize = 26;
            txtAvatarIcon.alignment = TextAnchor.MiddleCenter;
            txtAvatarIcon.text = "💡";
            txtAvatarIcon.color = Color.white;
            txtAvatarIcon.raycastTarget = false;

            // Speaker Tag
            GameObject tagObj = CreateUI("Txt_SpeakerTag", avatarBox.transform);
            RectTransform tagRect = tagObj.GetComponent<RectTransform>();
            tagRect.anchorMin = new Vector2(0f, 0f);
            tagRect.anchorMax = new Vector2(1f, 0.38f);
            tagRect.anchoredPosition = Vector2.zero;
            tagRect.sizeDelta = Vector2.zero;

            txtSpeakerTag = tagObj.AddComponent<Text>();
            txtSpeakerTag.font = font;
            txtSpeakerTag.fontSize = 11;
            txtSpeakerTag.fontStyle = FontStyle.Bold;
            txtSpeakerTag.alignment = TextAnchor.MiddleCenter;
            txtSpeakerTag.text = "MC (BATIN)";
            txtSpeakerTag.color = new Color(0.35f, 0.85f, 1.0f); // Cyan
            txtSpeakerTag.raycastTarget = false;

            // Right Content Area (Step Badge + Monologue + Hint in distinct non-overlapping vertical slots)
            GameObject contentBox = CreateUI("Box_Content", transform);
            RectTransform contRect = contentBox.GetComponent<RectTransform>();
            contRect.anchorMin = new Vector2(0f, 0f);
            contRect.anchorMax = new Vector2(1f, 1f);
            contRect.pivot = new Vector2(0f, 0.5f);
            contRect.anchoredPosition = new Vector2(105f, 0f);
            contRect.sizeDelta = new Vector2(-115f, -10f);

            // Slot 1: Step Badge (Top: Height 22px)
            GameObject stepObj = CreateUI("Txt_StepBadge", contentBox.transform);
            RectTransform stepRect = stepObj.GetComponent<RectTransform>();
            stepRect.anchorMin = new Vector2(0f, 1f);
            stepRect.anchorMax = new Vector2(1f, 1f);
            stepRect.pivot = new Vector2(0f, 1f);
            stepRect.anchoredPosition = new Vector2(0f, 0f);
            stepRect.sizeDelta = new Vector2(0f, 22f);

            txtStepBadge = stepObj.AddComponent<Text>();
            txtStepBadge.font = font;
            txtStepBadge.fontSize = 13;
            txtStepBadge.fontStyle = FontStyle.Bold;
            txtStepBadge.alignment = TextAnchor.MiddleLeft;
            txtStepBadge.text = "";
            txtStepBadge.color = new Color(1.0f, 0.88f, 0.35f); // Gold
            txtStepBadge.raycastTarget = false;

            // Slot 2: Monologue Body (Middle: Y from -24px, Height 42px, wraps cleanly)
            GameObject monoObj = CreateUI("Txt_Monologue", contentBox.transform);
            RectTransform monoRect = monoObj.GetComponent<RectTransform>();
            monoRect.anchorMin = new Vector2(0f, 1f);
            monoRect.anchorMax = new Vector2(1f, 1f);
            monoRect.pivot = new Vector2(0f, 1f);
            monoRect.anchoredPosition = new Vector2(0f, -23f);
            monoRect.sizeDelta = new Vector2(0f, 42f);

            txtMonologue = monoObj.AddComponent<Text>();
            txtMonologue.font = font;
            txtMonologue.fontSize = 12;
            txtMonologue.lineSpacing = 1.15f;
            txtMonologue.fontStyle = FontStyle.Normal;
            txtMonologue.alignment = TextAnchor.UpperLeft;
            txtMonologue.horizontalOverflow = HorizontalWrapMode.Wrap;
            txtMonologue.verticalOverflow = VerticalWrapMode.Truncate;
            txtMonologue.text = "";
            txtMonologue.color = Color.white;
            txtMonologue.raycastTarget = false;

            // Slot 3: Hint Footer (Bottom: Height 20px)
            GameObject hintObj = CreateUI("Txt_Hint", contentBox.transform);
            RectTransform hintRect = hintObj.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0f, 0f);
            hintRect.anchorMax = new Vector2(1f, 0f);
            hintRect.pivot = new Vector2(0f, 0f);
            hintRect.anchoredPosition = new Vector2(0f, 2f);
            hintRect.sizeDelta = new Vector2(0f, 20f);

            txtHint = hintObj.AddComponent<Text>();
            txtHint.font = font;
            txtHint.fontSize = 11;
            txtHint.fontStyle = FontStyle.Italic;
            txtHint.alignment = TextAnchor.MiddleLeft;
            txtHint.horizontalOverflow = HorizontalWrapMode.Wrap;
            txtHint.verticalOverflow = VerticalWrapMode.Truncate;
            txtHint.text = "";
            txtHint.color = new Color(0.45f, 0.95f, 0.65f); // Mint green
            txtHint.raycastTarget = false;

            isBuilt = true;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Displays the guidance banner with animated entrance pop and sound effect.
        /// </summary>
        public void ShowGuidance(string stepBadge, string monologue, string hint, Color? badgeColor = null)
        {
            if (!isBuilt || rootBanner == null || txtStepBadge == null)
            {
                Canvas canvas = GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
                if (canvas != null) BuildUIHierarchy(canvas);
            }

            gameObject.SetActive(true);

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
