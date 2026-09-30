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

        public static TutorialGuidanceBannerUI EnsureInstance()
        {
            if (Instance != null) return Instance;

            TutorialGuidanceBannerUI found = FindFirstObjectByType<TutorialGuidanceBannerUI>(FindObjectsInactive.Include);
            if (found != null)
            {
                Instance = found;
                return found;
            }

            Canvas canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("TutorialCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            GameObject bannerObj = new GameObject("Panel_TutorialGuidanceBanner");
            bannerObj.transform.SetParent(canvas.transform, false);
            Instance = bannerObj.AddComponent<TutorialGuidanceBannerUI>();
            Instance.BuildUIHierarchy(canvas);
            return Instance;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (rootBanner == null)
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
            rootCanvas = canvas;
            transform.SetParent(canvas.transform, false);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#if UNITY_EDITOR
            Font fontBold = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Font_Bold.ttf");
            if (fontBold != null) font = fontBold;
#endif

            // Main Banner Rect (Top-Center, right below Turn Ribbon)
            RectTransform rect = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -76f);
            rect.sizeDelta = new Vector2(660f, 80f);

            rootBanner = gameObject;

            // Background
            bannerBackground = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            bannerBackground.color = new Color(0.06f, 0.08f, 0.12f, 0.95f); // Deep solid slate
            bannerBackground.raycastTarget = false;

            // Glowing border
            bannerOutline = gameObject.GetComponent<Outline>() ?? gameObject.AddComponent<Outline>();
            bannerOutline.effectColor = new Color(1.0f, 0.82f, 0.25f, 0.85f); // Gold border
            bannerOutline.effectDistance = new Vector2(2f, -2f);

            // Left Avatar Box (Icon + Speaker Tag)
            GameObject avatarBox = new GameObject("Box_Avatar");
            avatarBox.transform.SetParent(transform, false);
            RectTransform avRect = avatarBox.AddComponent<RectTransform>();
            avRect.anchorMin = new Vector2(0f, 0f);
            avRect.anchorMax = new Vector2(0f, 1f);
            avRect.pivot = new Vector2(0f, 0.5f);
            avRect.anchoredPosition = new Vector2(10f, 0f);
            avRect.sizeDelta = new Vector2(90f, 0f);

            Image avBg = avatarBox.AddComponent<Image>();
            avBg.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);
            avBg.raycastTarget = false;

            Outline avOutline = avatarBox.AddComponent<Outline>();
            avOutline.effectColor = new Color(0.35f, 0.85f, 1.0f, 0.6f);
            avOutline.effectDistance = new Vector2(1f, -1f);

            // Avatar Icon / Emoji
            GameObject iconObj = new GameObject("Txt_AvatarIcon");
            iconObj.transform.SetParent(avatarBox.transform, false);
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.35f);
            iconRect.anchorMax = new Vector2(1f, 1f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = Vector2.zero;

            txtAvatarIcon = iconObj.AddComponent<Text>();
            txtAvatarIcon.font = font;
            txtAvatarIcon.fontSize = 24;
            txtAvatarIcon.alignment = TextAnchor.MiddleCenter;
            txtAvatarIcon.text = "💡";
            txtAvatarIcon.color = Color.white;
            txtAvatarIcon.raycastTarget = false;

            // Speaker Tag
            GameObject tagObj = new GameObject("Txt_SpeakerTag");
            tagObj.transform.SetParent(avatarBox.transform, false);
            RectTransform tagRect = tagObj.AddComponent<RectTransform>();
            tagRect.anchorMin = new Vector2(0f, 0f);
            tagRect.anchorMax = new Vector2(1f, 0.40f);
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

            // Right Content Area (Step Badge + Monologue + Hint)
            GameObject contentBox = new GameObject("Box_Content");
            contentBox.transform.SetParent(transform, false);
            RectTransform contRect = contentBox.AddComponent<RectTransform>();
            contRect.anchorMin = new Vector2(0f, 0f);
            contRect.anchorMax = new Vector2(1f, 1f);
            contRect.pivot = new Vector2(0f, 0.5f);
            contRect.anchoredPosition = new Vector2(110f, 0f);
            contRect.sizeDelta = new Vector2(-120f, -8f);

            // Step Badge (Header)
            GameObject stepObj = new GameObject("Txt_StepBadge");
            stepObj.transform.SetParent(contentBox.transform, false);
            RectTransform stepRect = stepObj.AddComponent<RectTransform>();
            stepRect.anchorMin = new Vector2(0f, 0.68f);
            stepRect.anchorMax = new Vector2(1f, 1f);
            stepRect.anchoredPosition = Vector2.zero;
            stepRect.sizeDelta = Vector2.zero;

            txtStepBadge = stepObj.AddComponent<Text>();
            txtStepBadge.font = font;
            txtStepBadge.fontSize = 13;
            txtStepBadge.fontStyle = FontStyle.Bold;
            txtStepBadge.alignment = TextAnchor.MiddleLeft;
            txtStepBadge.text = "[LANGKAH 1/4] PILIH PALADIN";
            txtStepBadge.color = new Color(1.0f, 0.88f, 0.35f); // Gold
            txtStepBadge.raycastTarget = false;

            // Monologue (Body)
            GameObject monoObj = new GameObject("Txt_Monologue");
            monoObj.transform.SetParent(contentBox.transform, false);
            RectTransform monoRect = monoObj.AddComponent<RectTransform>();
            monoRect.anchorMin = new Vector2(0f, 0.28f);
            monoRect.anchorMax = new Vector2(1f, 0.68f);
            monoRect.anchoredPosition = Vector2.zero;
            monoRect.sizeDelta = Vector2.zero;

            txtMonologue = monoObj.AddComponent<Text>();
            txtMonologue.font = font;
            txtMonologue.fontSize = 13;
            txtMonologue.fontStyle = FontStyle.Normal;
            txtMonologue.alignment = TextAnchor.MiddleLeft;
            txtMonologue.text = "\"Oke... pertama-tama aku hanya perlu tekan Paladinku untuk memilihnya...\"";
            txtMonologue.color = Color.white;
            txtMonologue.raycastTarget = false;

            // Hint (Footer)
            GameObject hintObj = new GameObject("Txt_Hint");
            hintObj.transform.SetParent(contentBox.transform, false);
            RectTransform hintRect = hintObj.AddComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0f, 0f);
            hintRect.anchorMax = new Vector2(1f, 0.28f);
            hintRect.anchoredPosition = Vector2.zero;
            hintRect.sizeDelta = Vector2.zero;

            txtHint = hintObj.AddComponent<Text>();
            txtHint.font = font;
            txtHint.fontSize = 11;
            txtHint.fontStyle = FontStyle.Italic;
            txtHint.alignment = TextAnchor.MiddleLeft;
            txtHint.text = "💡 Petunjuk: Klik Paladin di medan tempur!";
            txtHint.color = new Color(0.45f, 0.95f, 0.65f); // Mint green
            txtHint.raycastTarget = false;

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Displays the guidance banner with animated entrance pop and sound effect.
        /// </summary>
        public void ShowGuidance(string stepBadge, string monologue, string hint, Color? badgeColor = null)
        {
            if (rootBanner == null)
            {
                Canvas canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
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

            // Pop scale 1.05 -> 1.0
            float popElapsed = 0f;
            float popDuration = 0.18f;
            while (popElapsed < popDuration)
            {
                popElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(popElapsed / popDuration);
                float s = Mathf.Lerp(1.06f, 1.0f, Mathf.Sin(t * Mathf.PI * 0.5f));
                if (rt != null) rt.localScale = Vector3.one * s;
                yield return null;
            }
            if (rt != null) rt.localScale = originalScale;

            // Subtle continuous breathing glow
            while (gameObject.activeInHierarchy)
            {
                float glow = (Mathf.Sin(Time.unscaledTime * 4.5f) + 1f) * 0.5f;
                Color glowCol = Color.Lerp(targetColor * 0.7f, targetColor * 1.3f, glow);
                if (bannerOutline != null)
                {
                    bannerOutline.effectColor = new Color(glowCol.r, glowCol.g, glowCol.b, 0.85f);
                }
                yield return null;
            }
        }
    }
}
