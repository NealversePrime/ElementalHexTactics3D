using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using ElementalHexTactics3D.Combat;
using ElementalHexTactics3D.Turn;
using ElementalHexTactics3D.CameraControl;

namespace ElementalHexTactics3D.UI
{
    /// <summary>
    /// UGUI 2D Canvas controller for the Game Title Screen, Story Prologue modal,
    /// How To Play guide, Options modal, and In-Game Pause menu.
    /// Title: "I Reincarnated as a Beast Talker, Now I'm Collecting Beasts!"
    /// </summary>
    public class TitleMenuCanvasUI : MonoBehaviour
    {
        public static TitleMenuCanvasUI Instance { get; private set; }

        [Header("Root Panels")]
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private GameObject townHubPanel;
        [SerializeField] private GameObject inGameHudPanel;
        [SerializeField] private GameObject storyModal;
        [SerializeField] private GameObject howToPlayModal;
        [SerializeField] private GameObject optionsModal;
        [SerializeField] private GameObject pauseModal;

        [Header("Main Menu Buttons")]
        [SerializeField] private Button btnPlay;
        [SerializeField] private Button btnStory;
        [SerializeField] private Button btnHowToPlay;
        [SerializeField] private Button btnOptions;
        [SerializeField] private Button btnExit;

        [Header("In-Game & Pause Buttons")]
        [SerializeField] private Button btnInGameMenu;
        [SerializeField] private Button btnResume;
        [SerializeField] private Button btnPauseOptions;
        [SerializeField] private Button btnRestart;
        [SerializeField] private Button btnReturnTitle;

        [Header("Modal Back Buttons")]
        [SerializeField] private Button btnBackStory;
        [SerializeField] private Button btnBackHowToPlay;
        [SerializeField] private Button btnBackOptions;

        public enum TitleThemeMode
        {
            PaladinHolyland,
            DemonLordRebellion
        }

        [Header("Title Theme & Mode")]
        [SerializeField] private TitleThemeMode currentThemeMode = TitleThemeMode.PaladinHolyland;
        [SerializeField] private Image titleBackgroundImage;
        [SerializeField] private Sprite paladinModeBgSprite;
        [SerializeField] private Sprite demonLordModeBgSprite;

        [Header("Header Texts")]
        [SerializeField] private Text txtBadge;
        [SerializeField] private Text txtTitle1;
        [SerializeField] private Text txtTitle2;
        [SerializeField] private Text txtSubtitle;
        [SerializeField] private Text txtVersion;

        [Header("Custom Graphic Title Logo (Canva / Photoshop Graphic)")]
        [SerializeField] private Image imgCustomTitleLogo;
        [SerializeField] private Sprite customLogoSprite;
        [SerializeField] private Sprite holyLogoSprite;
        [SerializeField] private Sprite demonLogoSprite;

        [Header("Modal Dynamic Texts")]
        [SerializeField] private Text txtStoryTitle;
        [SerializeField] private Text txtStoryBody;
        [SerializeField] private Text txtHowToPlayTitle;
        [SerializeField] private Text txtHowToPlayBody;

        [Header("Options Controls")]
        [SerializeField] private Button btnAudioToggle;
        [SerializeField] private Text txtAudioStatus;
        [SerializeField] private Button btnAiToggle;
        [SerializeField] private Text txtAiStatus;
        [SerializeField] private Button btnDisplayToggle;
        [SerializeField] private Text txtDisplayStatus;
        [SerializeField] private Button btnThemeModeToggle;
        [SerializeField] private Text txtThemeModeStatus;

        [Header("Developer Options")]
        [Tooltip("When enabled, pressing Play directly opens Citadel Town Hub with starter reserve units ready.")]
        [SerializeField] private bool skipTutorialToHub = false;

        public const string PREF_SKIP_TUTORIAL = "EHT3D_SkipTutorialToHub";

        // State Tracking
        private bool isInGame = false;
        private bool isInTownHub = false;
        private bool isPaused = false;
        private GameObject activeModal = null;
        private bool returnToPauseFromOptions = false;

        public bool IsInGame => isInGame;
        public bool IsInTownHub => isInTownHub;
        public bool IsPaused => isPaused || activeModal != null;
        public bool IsOnTitleScreen => !isInGame && !isInTownHub && activeModal == null;

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

            // If legacy TitleMenuManager3D exists, destroy it so it never causes state conflicts or reloads
            GameObject legacy = GameObject.Find("TitleMenuManager3D");
            if (legacy != null && legacy != gameObject)
            {
                Destroy(legacy);
            }

            if (titlePanel == null)
            {
                Transform t = transform.Find("Panel_TitleScreenRoot") ?? transform.Find("Panel_Title");
                if (t != null) titlePanel = t.gameObject;
            }

            if (townHubPanel == null)
            {
                Transform hub = transform.Find("Panel_TownHub");
                if (hub != null) townHubPanel = hub.gameObject;
            }

            if (townHubPanel != null)
            {
                Transform oldShadows = townHubPanel.transform.Find("Container_GroundShadows");
                if (oldShadows != null) DestroyImmediate(oldShadows.gameObject);
                Transform oldSpills = townHubPanel.transform.Find("Container_LightSpills");
                if (oldSpills != null) DestroyImmediate(oldSpills.gameObject);
            }

            // Determine initial theme mode based on persistent meta-state or developer skip toggle
            bool transmigrated = ElementalHexTactics3D.Campaign.CampaignSaveManager.HasTransmigrated();
            bool isSkipActive = skipTutorialToHub || PlayerPrefs.GetInt(PREF_SKIP_TUTORIAL, 0) == 1;
            currentThemeMode = (transmigrated || isSkipActive) ? TitleThemeMode.DemonLordRebellion : TitleThemeMode.PaladinHolyland;

            AdjustModalLayouts();
            AdjustTitleScreenLayout();
            ApplyTitleThemeMode(currentThemeMode, false);
        }

        /// <summary>
        /// Ensures titles sit cleanly on the inner dark slate area rather than the gold border,
        /// even if the scene was generated with an earlier layout version.
        /// </summary>
        private void AdjustModalLayouts()
        {
            // 1. Pause Modal
            if (pauseModal != null)
            {
                Transform card = pauseModal.transform.Find("CardFrame");
                if (card != null)
                {
                    RectTransform cRect = card.GetComponent<RectTransform>();
                    if (cRect != null) cRect.sizeDelta = new Vector2(480f, 450f);

                    Transform tTrans = card.Find("Text_ModalTitle");
                    if (tTrans != null)
                    {
                        RectTransform tRect = tTrans.GetComponent<RectTransform>();
                        if (tRect != null)
                        {
                            tRect.anchoredPosition = new Vector2(0f, 126f);
                            tRect.sizeDelta = new Vector2(420f, 34f);
                        }
                        Text t = tTrans.GetComponent<Text>();
                        if (t != null)
                        {
                            t.text = "✦ BATTLE PAUSED ✦";
                            t.color = new Color(1f, 0.88f, 0.38f);
                        }
                    }

                    if (btnResume != null)
                    {
                        RectTransform r = btnResume.GetComponent<RectTransform>();
                        if (r != null) { r.anchoredPosition = new Vector2(0f, 60f); r.sizeDelta = new Vector2(360f, 48f); }
                    }
                    if (btnPauseOptions != null)
                    {
                        RectTransform r = btnPauseOptions.GetComponent<RectTransform>();
                        if (r != null) { r.anchoredPosition = new Vector2(0f, 4f); r.sizeDelta = new Vector2(360f, 48f); }
                    }
                    if (btnRestart != null)
                    {
                        RectTransform r = btnRestart.GetComponent<RectTransform>();
                        if (r != null) { r.anchoredPosition = new Vector2(0f, -52f); r.sizeDelta = new Vector2(360f, 48f); }
                    }
                    if (btnReturnTitle != null)
                    {
                        RectTransform r = btnReturnTitle.GetComponent<RectTransform>();
                        if (r != null) { r.anchoredPosition = new Vector2(0f, -108f); r.sizeDelta = new Vector2(360f, 48f); }
                    }
                }
            }

            // 2. Options Modal
            if (optionsModal != null)
            {
                Transform card = optionsModal.transform.Find("CardFrame");
                if (card != null)
                {
                    RectTransform cRect = card.GetComponent<RectTransform>();
                    if (cRect != null) cRect.sizeDelta = new Vector2(600f, 520f);

                    Transform tTrans = card.Find("Text_ModalTitle");
                    if (tTrans != null)
                    {
                        RectTransform tRect = tTrans.GetComponent<RectTransform>();
                        if (tRect != null)
                        {
                            tRect.anchoredPosition = new Vector2(0f, 145f);
                            tRect.sizeDelta = new Vector2(520f, 36f);
                        }
                    }
                }
            }

            // 3. Story Modal
            if (storyModal != null)
            {
                Transform card = storyModal.transform.Find("CardFrame");
                if (card != null)
                {
                    RectTransform cRect = card.GetComponent<RectTransform>();
                    if (cRect != null) cRect.sizeDelta = new Vector2(860f, 560f);

                    Transform tTrans = card.Find("Text_ModalTitle");
                    if (tTrans != null)
                    {
                        RectTransform tRect = tTrans.GetComponent<RectTransform>();
                        if (tRect != null)
                        {
                            tRect.anchoredPosition = new Vector2(0f, 140f);
                            tRect.sizeDelta = new Vector2(740f, 38f);
                        }
                        Text t = tTrans.GetComponent<Text>();
                        if (t != null)
                        {
                            t.fontSize = 22;
                            t.fontStyle = FontStyle.Bold;
                            t.alignment = TextAnchor.MiddleCenter;
                            t.color = new Color(1f, 0.88f, 0.38f);
                        }
                    }

                    Transform bTrans = card.Find("Text_StoryBody");
                    if (bTrans != null)
                    {
                        RectTransform bRect = bTrans.GetComponent<RectTransform>();
                        if (bRect != null)
                        {
                            bRect.anchoredPosition = new Vector2(0f, -15f);
                            bRect.sizeDelta = new Vector2(740f, 260f);
                        }
                        Text b = bTrans.GetComponent<Text>();
                        if (b != null)
                        {
                            b.fontSize = 17;
                            b.lineSpacing = 1.35f;
                            b.alignment = TextAnchor.UpperLeft;
                        }
                    }

                    Transform backTrans = card.Find("Btn_BackStory");
                    if (backTrans != null)
                    {
                        RectTransform backRect = backTrans.GetComponent<RectTransform>();
                        if (backRect != null)
                        {
                            backRect.anchoredPosition = new Vector2(0f, -205f);
                            backRect.sizeDelta = new Vector2(280f, 48f);
                        }
                    }
                }
            }

            // 4. How To Play Modal
            if (howToPlayModal != null)
            {
                Transform card = howToPlayModal.transform.Find("CardFrame");
                if (card != null)
                {
                    RectTransform cRect = card.GetComponent<RectTransform>();
                    if (cRect != null) cRect.sizeDelta = new Vector2(860f, 580f);

                    Transform tTrans = card.Find("Text_ModalTitle");
                    if (tTrans != null)
                    {
                        RectTransform tRect = tTrans.GetComponent<RectTransform>();
                        if (tRect != null)
                        {
                            tRect.anchoredPosition = new Vector2(0f, 150f);
                            tRect.sizeDelta = new Vector2(740f, 38f);
                        }
                        Text t = tTrans.GetComponent<Text>();
                        if (t != null)
                        {
                            t.fontSize = 22;
                            t.fontStyle = FontStyle.Bold;
                            t.alignment = TextAnchor.MiddleCenter;
                            t.color = new Color(1f, 0.88f, 0.38f);
                        }
                    }

                    Transform bTrans = card.Find("Text_HowToPlayBody");
                    if (bTrans != null)
                    {
                        RectTransform bRect = bTrans.GetComponent<RectTransform>();
                        if (bRect != null)
                        {
                            bRect.anchoredPosition = new Vector2(0f, -15f);
                            bRect.sizeDelta = new Vector2(740f, 275f);
                        }
                        Text b = bTrans.GetComponent<Text>();
                        if (b != null)
                        {
                            b.fontSize = 17;
                            b.lineSpacing = 1.35f;
                            b.alignment = TextAnchor.UpperLeft;
                        }
                    }

                    Transform backTrans = card.Find("Btn_BackHowToPlay");
                    if (backTrans != null)
                    {
                        RectTransform backRect = backTrans.GetComponent<RectTransform>();
                        if (backRect != null)
                        {
                            backRect.anchoredPosition = new Vector2(0f, -215f);
                            backRect.sizeDelta = new Vector2(280f, 48f);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Restores missing title texts, applies overflow to prevent clipping,
        /// sharpens button proportions, and ensures background artwork is configured.
        /// </summary>
        private void AdjustTitleScreenLayout()
        {
            if (titlePanel == null) return;

            Transform header = titlePanel.transform.Find("Panel_TitleHeader");
            if (header != null)
            {
                RectTransform hRect = header.GetComponent<RectTransform>();
                if (hRect != null)
                {
                    hRect.sizeDelta = new Vector2(980f, 320f);
                    hRect.anchoredPosition = new Vector2(0f, -20f);
                }

                Transform tBadge = header.Find("Text_Badge");
                if (tBadge != null)
                {
                    var r = tBadge.GetComponent<RectTransform>();
                    if (r != null) { r.anchoredPosition = new Vector2(0f, 140f); r.sizeDelta = new Vector2(840f, 28f); }
                    txtBadge = tBadge.GetComponent<Text>();
                    if (txtBadge != null) { txtBadge.horizontalOverflow = HorizontalWrapMode.Overflow; txtBadge.verticalOverflow = VerticalWrapMode.Overflow; }
                }

                Transform t1 = header.Find("Text_Title1");
                if (t1 != null)
                {
                    var r = t1.GetComponent<RectTransform>();
                    if (r != null) { r.anchoredPosition = new Vector2(0f, 32f); r.sizeDelta = new Vector2(840f, 48f); }
                    txtTitle1 = t1.GetComponent<Text>();
                    if (txtTitle1 != null) { txtTitle1.horizontalOverflow = HorizontalWrapMode.Overflow; txtTitle1.verticalOverflow = VerticalWrapMode.Overflow; }
                }

                Transform t2 = header.Find("Text_Title2");
                if (t2 != null)
                {
                    var r = t2.GetComponent<RectTransform>();
                    if (r != null) { r.anchoredPosition = new Vector2(0f, -14f); r.sizeDelta = new Vector2(840f, 52f); }
                    txtTitle2 = t2.GetComponent<Text>();
                    if (txtTitle2 != null) { txtTitle2.horizontalOverflow = HorizontalWrapMode.Overflow; txtTitle2.verticalOverflow = VerticalWrapMode.Overflow; }
                }

                Transform sub = header.Find("Text_Subtitle");
                if (sub != null)
                {
                    var r = sub.GetComponent<RectTransform>();
                    if (r != null) { r.anchoredPosition = new Vector2(0f, -58f); r.sizeDelta = new Vector2(840f, 26f); }
                    txtSubtitle = sub.GetComponent<Text>();
                    if (txtSubtitle != null) { txtSubtitle.horizontalOverflow = HorizontalWrapMode.Overflow; txtSubtitle.verticalOverflow = VerticalWrapMode.Overflow; }
                }

                Transform ver = header.Find("Text_Version");
                if (ver != null)
                {
                    var r = ver.GetComponent<RectTransform>();
                    if (r != null) { r.anchoredPosition = new Vector2(0f, -145f); r.sizeDelta = new Vector2(840f, 22f); }
                    txtVersion = ver.GetComponent<Text>();
                    if (txtVersion != null) { txtVersion.horizontalOverflow = HorizontalWrapMode.Overflow; txtVersion.verticalOverflow = VerticalWrapMode.Overflow; }
                }

                // Check for graphic logo overrides (e.g. designed in Canva or Photoshop)
                if (holyLogoSprite == null)
                {
                    holyLogoSprite = LoadLogoSprite("Logo_Omniterra_Holy")
                                  ?? LoadLogoSprite("Logo_Omniterra");
                }
                if (demonLogoSprite == null)
                {
                    demonLogoSprite = LoadLogoSprite("Logo_Omniterra_Demon")
                                   ?? LoadLogoSprite("Logo_Omniterra");
                }

                Transform logoTrans = header.Find("Image_CustomLogo");
                Sprite activeLogo = (currentThemeMode == TitleThemeMode.PaladinHolyland) ? holyLogoSprite : demonLogoSprite;
                Image headerBoxImg = header.GetComponent<Image>();
                if (headerBoxImg != null)
                {
                    headerBoxImg.enabled = (activeLogo == null);
                }

                if (activeLogo != null)
                {
                    if (logoTrans == null)
                    {
                        GameObject logoObj = new GameObject("Image_CustomLogo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                        logoObj.transform.SetParent(header, false);
                        logoTrans = logoObj.transform;
                    }
                    imgCustomTitleLogo = logoTrans.GetComponent<Image>();
                    imgCustomTitleLogo.sprite = activeLogo;
                    imgCustomTitleLogo.preserveAspect = true;
                    imgCustomTitleLogo.raycastTarget = false;
                    imgCustomTitleLogo.gameObject.SetActive(true);

                    RectTransform logoRect = imgCustomTitleLogo.GetComponent<RectTransform>();
                    logoRect.anchoredPosition = new Vector2(0f, -5f);
                    logoRect.sizeDelta = new Vector2(920f, 280f);

                    if (txtBadge != null)
                    {
                        var br = txtBadge.GetComponent<RectTransform>();
                        if (br != null) br.anchoredPosition = new Vector2(0f, 140f);
                    }
                    if (txtVersion != null)
                    {
                        var vr = txtVersion.GetComponent<RectTransform>();
                        if (vr != null) vr.anchoredPosition = new Vector2(0f, -145f);
                    }

                    // When graphic logo image is present, hide procedural text titles and subtitle!
                    if (txtTitle1 != null) txtTitle1.gameObject.SetActive(false);
                    if (txtTitle2 != null) txtTitle2.gameObject.SetActive(false);
                    if (txtSubtitle != null) txtSubtitle.gameObject.SetActive(false);
                }
                else
                {
                    if (logoTrans != null) logoTrans.gameObject.SetActive(false);
                    if (txtTitle1 != null) txtTitle1.gameObject.SetActive(true);
                    if (txtTitle2 != null) txtTitle2.gameObject.SetActive(true);
                    if (txtSubtitle != null) txtSubtitle.gameObject.SetActive(true);
                    if (txtBadge != null)
                    {
                        var br = txtBadge.GetComponent<RectTransform>();
                        if (br != null) br.anchoredPosition = new Vector2(0f, 75f);
                    }
                    if (txtVersion != null)
                    {
                        var vr = txtVersion.GetComponent<RectTransform>();
                        if (vr != null) vr.anchoredPosition = new Vector2(0f, -86f);
                    }
                }
            }

            Transform menuButtons = titlePanel.transform.Find("Panel_MenuButtons");
            if (menuButtons != null)
            {
                RectTransform mRect = menuButtons.GetComponent<RectTransform>();
                if (mRect != null)
                {
                    mRect.sizeDelta = new Vector2(380f, 310f);
                    mRect.anchoredPosition = new Vector2(0f, -75f);
                }
            }

            // Ensure TitleBackgroundArt exists
            if (titleBackgroundImage == null)
            {
                Transform bgTrans = titlePanel.transform.Find("TitleBackgroundArt");
                if (bgTrans != null)
                {
                    titleBackgroundImage = bgTrans.GetComponent<Image>();
                }
                else
                {
                    GameObject bgObj = new GameObject("TitleBackgroundArt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    bgObj.transform.SetParent(titlePanel.transform, false);
                    bgObj.transform.SetAsFirstSibling();
                    RectTransform bRect = bgObj.GetComponent<RectTransform>();
                    bRect.anchorMin = Vector2.zero;
                    bRect.anchorMax = Vector2.one;
                    bRect.sizeDelta = Vector2.zero;
                    bRect.anchoredPosition = Vector2.zero;
                    titleBackgroundImage = bgObj.GetComponent<Image>();
                    titleBackgroundImage.color = Color.white;
                    titleBackgroundImage.raycastTarget = false;
                }
            }

            // Ensure soft dimmer vignette sits directly above background art
            Transform dimmer = titlePanel.transform.Find("DioramaDimmer");
            if (dimmer == null)
            {
                GameObject dimmerObj = new GameObject("DioramaDimmer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                dimmerObj.transform.SetParent(titlePanel.transform, false);
                if (titleBackgroundImage != null)
                {
                    dimmerObj.transform.SetSiblingIndex(titleBackgroundImage.transform.GetSiblingIndex() + 1);
                }
                else
                {
                    dimmerObj.transform.SetAsFirstSibling();
                }
                RectTransform dRect = dimmerObj.GetComponent<RectTransform>();
                dRect.anchorMin = Vector2.zero;
                dRect.anchorMax = Vector2.one;
                dRect.sizeDelta = Vector2.zero;
                dRect.anchoredPosition = Vector2.zero;
                Image dImg = dimmerObj.GetComponent<Image>();
                dImg.color = new Color(0.04f, 0.06f, 0.09f, 0.35f);
                dImg.raycastTarget = false;
            }

            // Find modal text references if not assigned
            if (storyModal != null)
            {
                Transform card = storyModal.transform.Find("CardFrame");
                if (card != null)
                {
                    if (txtStoryTitle == null) txtStoryTitle = card.Find("Text_ModalTitle")?.GetComponent<Text>();
                    if (txtStoryBody == null) txtStoryBody = card.Find("Text_StoryBody")?.GetComponent<Text>();
                }
            }

            if (howToPlayModal != null)
            {
                Transform card = howToPlayModal.transform.Find("CardFrame");
                if (card != null)
                {
                    if (txtHowToPlayTitle == null) txtHowToPlayTitle = card.Find("Text_ModalTitle")?.GetComponent<Text>();
                    if (txtHowToPlayBody == null) txtHowToPlayBody = card.Find("Text_HowToPlayBody")?.GetComponent<Text>();
                }
            }
        }

        /// <summary>
        /// Applies either the Holyland Evolve (Paladin Mode) or Elemental Hex (Demon Lord Mode) visual theme.
        /// </summary>
        public void ApplyTitleThemeMode(TitleThemeMode mode, bool persist = true)
        {
            currentThemeMode = mode;
            if (persist)
            {
                ElementalHexTactics3D.Campaign.CampaignSaveManager.SetTransmigrated(mode == TitleThemeMode.DemonLordRebellion);
            }

            // 1. Background Art
            if (titleBackgroundImage != null)
            {
                if (mode == TitleThemeMode.PaladinHolyland)
                {
                    if (paladinModeBgSprite == null)
                    {
#if UNITY_EDITOR
                        paladinModeBgSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Backgrounds/bg_holyland_cathedral.jpg");
#endif
                        if (paladinModeBgSprite == null)
                            paladinModeBgSprite = ElementalHexTactics3D.Units.TacticalUnitSpawner.LoadBackgroundSprite("bg_holyland_cathedral.jpg");
                    }
                    if (paladinModeBgSprite != null) titleBackgroundImage.sprite = paladinModeBgSprite;
                }
                else
                {
                    if (demonLordModeBgSprite == null)
                    {
#if UNITY_EDITOR
                        demonLordModeBgSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Backgrounds/bg_demonlord_throne.jpg");
#endif
                        if (demonLordModeBgSprite == null)
                            demonLordModeBgSprite = ElementalHexTactics3D.Units.TacticalUnitSpawner.LoadBackgroundSprite("bg_demonlord_throne.jpg");
                    }
                    if (demonLordModeBgSprite != null) titleBackgroundImage.sprite = demonLordModeBgSprite;
                }
                titleBackgroundImage.color = Color.white;
                titleBackgroundImage.gameObject.SetActive(true);
            }

            // 2. Custom Graphic Logo switching (Holy vs Demon Lord)
            if (holyLogoSprite == null)
            {
                holyLogoSprite = LoadLogoSprite("Logo_Omniterra_Holy")
                              ?? LoadLogoSprite("Logo_Omniterra");
            }
            if (demonLogoSprite == null)
            {
                demonLogoSprite = LoadLogoSprite("Logo_Omniterra_Demon")
                               ?? LoadLogoSprite("Logo_Omniterra");
            }

            Sprite targetLogo = (mode == TitleThemeMode.PaladinHolyland) ? holyLogoSprite : demonLogoSprite;

            if (imgCustomTitleLogo == null && titlePanel != null)
            {
                Transform header = titlePanel.transform.Find("Panel_TitleHeader");
                if (header != null)
                {
                    Transform logoTrans = header.Find("Image_CustomLogo");
                    if (logoTrans == null && targetLogo != null)
                    {
                        GameObject logoObj = new GameObject("Image_CustomLogo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                        logoObj.transform.SetParent(header, false);
                        logoTrans = logoObj.transform;
                    }
                    if (logoTrans != null)
                    {
                        imgCustomTitleLogo = logoTrans.GetComponent<Image>();
                    }
                }
            }

            if (titlePanel != null)
            {
                Transform header = titlePanel.transform.Find("Panel_TitleHeader");
                if (header != null)
                {
                    Image headerBoxImg = header.GetComponent<Image>();
                    if (headerBoxImg != null) headerBoxImg.enabled = (targetLogo == null);
                }
            }

            if (imgCustomTitleLogo != null)
            {
                RectTransform logoRect = imgCustomTitleLogo.GetComponent<RectTransform>();
                if (targetLogo != null)
                {
                    imgCustomTitleLogo.sprite = targetLogo;
                    imgCustomTitleLogo.preserveAspect = true;
                    imgCustomTitleLogo.raycastTarget = false;
                    imgCustomTitleLogo.gameObject.SetActive(true);
                    if (logoRect != null)
                    {
                        logoRect.anchoredPosition = new Vector2(0f, -5f);
                        logoRect.sizeDelta = new Vector2(920f, 280f);
                    }

                    if (txtBadge != null)
                    {
                        var br = txtBadge.GetComponent<RectTransform>();
                        if (br != null) br.anchoredPosition = new Vector2(0f, 140f);
                    }
                    if (txtVersion != null)
                    {
                        var vr = txtVersion.GetComponent<RectTransform>();
                        if (vr != null) vr.anchoredPosition = new Vector2(0f, -145f);
                    }

                    if (txtTitle1 != null) txtTitle1.gameObject.SetActive(false);
                    if (txtTitle2 != null) txtTitle2.gameObject.SetActive(false);
                    if (txtSubtitle != null) txtSubtitle.gameObject.SetActive(false);
                }
                else
                {
                    imgCustomTitleLogo.gameObject.SetActive(false);
                    if (txtTitle1 != null) txtTitle1.gameObject.SetActive(true);
                    if (txtTitle2 != null) txtTitle2.gameObject.SetActive(true);
                    if (txtSubtitle != null) txtSubtitle.gameObject.SetActive(true);
                    if (txtBadge != null)
                    {
                        var br = txtBadge.GetComponent<RectTransform>();
                        if (br != null) br.anchoredPosition = new Vector2(0f, 75f);
                    }
                    if (txtVersion != null)
                    {
                        var vr = txtVersion.GetComponent<RectTransform>();
                        if (vr != null) vr.anchoredPosition = new Vector2(0f, -86f);
                    }
                }
            }

            // 3. Texts & Styling
            if (mode == TitleThemeMode.PaladinHolyland)
            {
                if (txtBadge != null)
                {
                    txtBadge.text = "✦ SACRED SYNOD EDITION • PROLOGUE: THE CHOSEN HERO ✦";
                    txtBadge.color = new Color(0.35f, 0.85f, 1f);
                }
                if (txtTitle1 != null)
                {
                    txtTitle1.text = "OMNITERRA";
                    txtTitle1.fontSize = 38;
                    txtTitle1.color = Color.white;
                }
                if (txtTitle2 != null)
                {
                    txtTitle2.text = "ASCENDANCE";
                    txtTitle2.fontSize = 42;
                    txtTitle2.color = new Color(1f, 0.85f, 0.25f);
                }
                if (txtSubtitle != null)
                {
                    txtSubtitle.text = "〜 The Righteous Crusade of the Radiant Synod 〜";
                    txtSubtitle.color = new Color(0.85f, 0.90f, 0.98f);
                }
                if (txtVersion != null)
                {
                    txtVersion.text = "Omniterra: Ascendance • Pre-Alpha v1.0.0 • Sacred Synod Edition";
                }

                if (btnPlay != null)
                {
                    Text pTxt = btnPlay.GetComponentInChildren<Text>();
                    if (pTxt != null) pTxt.text = "⚔️ START HOLY QUEST";
                }
                if (btnStory != null)
                {
                    Text sTxt = btnStory.GetComponentInChildren<Text>();
                    if (sTxt != null) sTxt.text = "📖 SACRED SCRIPTURES (Lore)";
                }
                if (btnHowToPlay != null)
                {
                    Text hTxt = btnHowToPlay.GetComponentInChildren<Text>();
                    if (hTxt != null) hTxt.text = "🎮 HOLY TACTICS (Guide)";
                }

                if (txtStoryTitle != null) txtStoryTitle.text = "📖 SACRED SCRIPTURES: THE RADIANT SYNOD";
                if (txtStoryBody != null)
                {
                    txtStoryBody.text =
                        "<b><color=#FBBF24>[ The Radiant Mandate ]</color></b>\n" +
                        "Under the sacred light of the Holy Empire, the Chosen Hero—the Paladin of Radiance—marches with the consecrated vanguard into the fractured underworld to eradicate all demonic existence.\n\n" +
                        "<b><color=#38BDF8>[ The Final Stronghold ]</color></b>\n" +
                        "Deep within the Ashen Bastion, the reviled Demon Lord cowers upon his blackened throne. Command your Holy Vanguard, smite the monstrous heretics, and cleanse the taint!\n\n" +
                        "<b><color=#34D399>[ The Synod's Promise ]</color></b>\n" +
                        "Purity through fire. Purge the dark underworld and claim immortal salvation in Omniterra!";
                }

                if (txtHowToPlayTitle != null) txtHowToPlayTitle.text = "🎮 HOLY TACTICS: BASIC COMMANDS";
                if (txtHowToPlayBody != null)
                {
                    txtHowToPlayBody.text =
                        "<b><color=#38BDF8>✦ Camera Controls:</color></b> <b>Q / E</b> Rotate 60°  •  <b>WASD</b> Pan  •  <b>Scroll</b> Zoom in/out\n\n" +
                        "<b><color=#FBBF24>✦ Movement & Positioning:</color></b>\n" +
                        "• Click your Paladin or Shielder to view reachable tiles (cyan highlights).\n" +
                        "• Click any highlighted hex to advance toward the demon lines.\n\n" +
                        "<b><color=#EF5350>✦ Righteous Strike & Wall Slams:</color></b>\n" +
                        "• When adjacent to an enemy (red highlights), click to unleash holy damage!\n" +
                        "• Pushing enemies into walls or stone pillars inflicts bonus kinetic slam damage!\n\n" +
                        "<b><color=#34D399>✦ Victory Objective:</color></b>\n" +
                        "• Slay the Demon Lord to purge the underworld and complete the sacred crusade!";
                }
            }
            else // DemonLordRebellion
            {
                if (txtBadge != null)
                {
                    txtBadge.text = "✦ 2.5D TACTICAL HEX BATTLE • DEMON LORD SANCTUARY ✦";
                    txtBadge.color = new Color(1f, 0.45f, 0.25f);
                }
                if (txtTitle1 != null)
                {
                    txtTitle1.text = "OMNITERRA";
                    txtTitle1.fontSize = 38;
                    txtTitle1.color = Color.white;
                }
                if (txtTitle2 != null)
                {
                    txtTitle2.text = "ABYSSAL BASTION";
                    txtTitle2.fontSize = 40;
                    txtTitle2.color = new Color(1f, 0.75f, 0.20f);
                }
                if (txtSubtitle != null)
                {
                    txtSubtitle.text = "〜 Reincarnated as the Demon Lord, Awakening Primordial Titans 〜";
                    txtSubtitle.color = new Color(0.88f, 0.85f, 0.78f);
                }
                if (txtVersion != null)
                {
                    txtVersion.text = "Omniterra • Pre-Alpha v1.0.0 • Ashen Sanctuary Edition";
                }

                UpdatePlayButtonLabel();

                if (btnStory != null)
                {
                    Text sTxt = btnStory.GetComponentInChildren<Text>();
                    if (sTxt != null) sTxt.text = "📖 STORY & LORE (Bastion)";
                }
                if (btnHowToPlay != null)
                {
                    Text hTxt = btnHowToPlay.GetComponentInChildren<Text>();
                    if (hTxt != null) hTxt.text = "🎮 HOW TO PLAY (Tactics)";
                }

                if (txtStoryTitle != null) txtStoryTitle.text = "📖 MEMOIRS OF THE OUTCAST SANCTUARY";
                if (txtStoryBody != null)
                {
                    txtStoryBody.text =
                        "<b><color=#F97316>[ The Transmigrated Sovereign ]</color></b>\n" +
                        "A former grandmaster strategist awakens within the frail body of the fallen Demon Lord. The Radiant Synod preaches holy benevolence while enslaving beastkin and burning peaceful monster villages.\n\n" +
                        "<b><color=#38BDF8>[ The Ashen Sanctuary ]</color></b>\n" +
                        "Surrounded by relentless crusaders, the Citadel is the final refuge for the hunted outcasts. Defend the realm, manage provisions, and rebuild the fallen domain.\n\n" +
                        "<b><color=#A855F7>[ Primordial Titan Awakening ]</color></b>\n" +
                        "Siphon elemental cores through the Abyssal Rift and awaken apocalyptic Primordial Titans to break the Holy Empire's chains!";
                }

                if (txtHowToPlayTitle != null) txtHowToPlayTitle.text = "🎮 CITADEL COMMAND: TACTICAL FIELD GUIDE";
                if (txtHowToPlayBody != null)
                {
                    txtHowToPlayBody.text =
                        "<b><color=#38BDF8>✦ Battlefield Navigation:</color></b> <b>Q / E</b> Rotate 60°  •  <b>WASD</b> Pan  •  <b>Scroll</b> Zoom  •  <b>Right-Click</b> Cancel\n\n" +
                        "<b><color=#F97316>✦ Elemental Terraforming:</color></b>\n" +
                        "• 🔥 <b>Fireball:</b> Chars grass into <b>Scorched Earth</b>; cast again to erupt into molten <b>Magma</b>!\n" +
                        "• 🪨 <b>Kinetic Shove:</b> Push enemies 1 hex away. Slamming foes into obstacles deals bonus damage!\n\n" +
                        "<b><color=#A855F7>✦ Abyssal Rift & Titan Summoning:</color></b>\n" +
                        "• Deploy reserve vanguard units directly onto the hex grid via the Abyssal Rift.\n" +
                        "• Awaken Primordial Titans at the Citadel Shrine to unleash catastrophic Cataclysm powers!";
                }
            }

            UpdateOptionsDisplay();
        }

        public void ToggleThemeMode()
        {
            TitleThemeMode newMode = currentThemeMode == TitleThemeMode.PaladinHolyland
                ? TitleThemeMode.DemonLordRebellion
                : TitleThemeMode.PaladinHolyland;

            bool enableSkip = (newMode == TitleThemeMode.DemonLordRebellion);
            skipTutorialToHub = enableSkip;
            PlayerPrefs.SetInt(PREF_SKIP_TUTORIAL, enableSkip ? 1 : 0);
            PlayerPrefs.Save();

            ApplyTitleThemeMode(newMode, true);
            PlaySoundClick();
        }

        private void Start()
        {
            BindButtonEvents();
            UpdateOptionsDisplay();

            // Default initial state: On Title Screen
            ShowTitleScreen();
        }

        private void Update()
        {
            // Global Escape key
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                HandleEscapeKey();
            }

            // Developer Quick-Test Shortcuts: F1 = Stage 1, F2 = Stage 2, F3 = Stage 3, F4 = Hub 2, F5 = Citadel Hub
            if (Keyboard.current != null)
            {
                if (Keyboard.current.f1Key.wasPressedThisFrame)
                {
                    Debug.Log("<color=#FFD54F><b>[Dev Shortcut]</b></color> Starting Tutorial Stage 1 (Holy Crusade)...");
                    Tutorial.TutorialScenarioManager.EnsureInstance().StartStage1HolyCrusade();
                }
                else if (Keyboard.current.f2Key.wasPressedThisFrame)
                {
                    Debug.Log("<color=#FF7043><b>[Dev Shortcut]</b></color> Starting Tutorial Stage 2 (Frontier Hazards)...");
                    Tutorial.TutorialScenarioManager.EnsureInstance().StartStage2FrontierHazards();
                }
                else if (Keyboard.current.f3Key.wasPressedThisFrame)
                {
                    Debug.Log("<color=#BA68C8><b>[Dev Shortcut]</b></color> Starting Tutorial Stage 3 (Titan Awakening)...");
                    Tutorial.TutorialScenarioManager.EnsureInstance().StartStage3AbyssalTitans();
                }
                else if (Keyboard.current.f4Key.wasPressedThisFrame)
                {
                    Debug.Log("<color=#FF7043><b>[Dev Shortcut]</b></color> Starting Hub 2 (Titan Crisis)...");
                    Tutorial.TutorialScenarioManager.EnsureInstance().StartHubTitanCrisis();
                }
                else if (Keyboard.current.f5Key.wasPressedThisFrame)
                {
                    Debug.Log("<color=#81C784><b>[Dev Shortcut]</b></color> Quick Jump directly into Citadel Hub...");
                    ShowTownHub(true);
                }
            }
        }

        private void BindButtonEvents()
        {
            if (btnPlay != null) btnPlay.onClick.AddListener(OnPlayClicked);
            if (btnStory != null) btnStory.onClick.AddListener(() => OpenModal(storyModal));
            if (btnHowToPlay != null) btnHowToPlay.onClick.AddListener(() => OpenModal(howToPlayModal));
            if (btnOptions != null) btnOptions.onClick.AddListener(() => { returnToPauseFromOptions = false; OpenModal(optionsModal); });
            if (btnExit != null) btnExit.onClick.AddListener(OnExitClicked);

            if (btnInGameMenu != null) btnInGameMenu.onClick.AddListener(OpenPauseMenu);
            if (btnResume != null) btnResume.onClick.AddListener(ResumeGame);
            if (btnPauseOptions != null) btnPauseOptions.onClick.AddListener(() => { returnToPauseFromOptions = true; OpenModal(optionsModal); });
            if (btnRestart != null) btnRestart.onClick.AddListener(OnRestartClicked);
            if (btnReturnTitle != null) btnReturnTitle.onClick.AddListener(OnReturnTitleClicked);

            if (btnBackStory != null) btnBackStory.onClick.AddListener(CloseActiveModal);
            if (btnBackHowToPlay != null) btnBackHowToPlay.onClick.AddListener(CloseActiveModal);
            if (btnBackOptions != null) btnBackOptions.onClick.AddListener(CloseActiveModal);

            if (btnAudioToggle != null) btnAudioToggle.onClick.AddListener(ToggleAudio);
            if (btnAiToggle != null) btnAiToggle.onClick.AddListener(ToggleAI);
            if (btnDisplayToggle != null) btnDisplayToggle.onClick.AddListener(ToggleDisplay);
            if (btnThemeModeToggle != null) btnThemeModeToggle.onClick.AddListener(ToggleThemeMode);
        }

        public void ShowTitleScreen()
        {
            isInGame = false;
            isInTownHub = false;
            isPaused = false;

            if (titlePanel != null) titlePanel.SetActive(true);
            if (townHubPanel != null) townHubPanel.SetActive(false);
            if (inGameHudPanel != null) inGameHudPanel.SetActive(false);
            CloseAllModals();

            UpdatePlayButtonLabel();
            Combat.SoundManager3D.Instance?.PlayHubBGM();
        }

        private void UpdatePlayButtonLabel()
        {
            if (btnPlay == null) return;
            Text playTxt = btnPlay.GetComponentInChildren<Text>();
            if (playTxt == null) return;

            bool isSkipActive = skipTutorialToHub || PlayerPrefs.GetInt(PREF_SKIP_TUTORIAL, 0) == 1;
            if (currentThemeMode == TitleThemeMode.PaladinHolyland && !isSkipActive)
            {
                playTxt.text = "⚔️ START HOLY QUEST";
                return;
            }

            if (ElementalHexTactics3D.Campaign.CampaignSaveManager.HasSaveFile())
            {
                var save = ElementalHexTactics3D.Campaign.CampaignSaveManager.LoadSaveData();
                if (save != null && save.currentDay > 1)
                {
                    playTxt.text = $"⚔️ CONTINUE RUN (DAY {save.currentDay})";
                    return;
                }
            }

            playTxt.text = "👑 ENTER CITADEL HUB";
        }

        public void ShowTownHub(bool skipTutorial = false)
        {
            isInGame = false;
            isInTownHub = true;
            isPaused = false;

            if (titlePanel != null) titlePanel.SetActive(false);
            if (townHubPanel != null) townHubPanel.SetActive(true);
            if (inGameHudPanel != null) inGameHudPanel.SetActive(false);
            CloseAllModals();

            // ONLY mark tutorial completed if explicitly requested (e.g. from developer F5 shortcut or skip option)!
            if (skipTutorial && Tutorial.TutorialScenarioManager.Instance != null)
            {
                Tutorial.TutorialScenarioManager.Instance.SkipTutorialDirectToCitadel();
            }

            // Ensure player units wait in reserve and are hidden while in Citadel Town Hub
            ElementalHexTactics3D.Units.TacticalUnitSpawner.ResetPlayerReserveUnits();

            Combat.SoundManager3D.Instance?.PlayHubBGM();
        }

        public void OnPlayClicked()
        {
            PlaySoundClick();
            bool isSkipActive = skipTutorialToHub || PlayerPrefs.GetInt(PREF_SKIP_TUTORIAL, 0) == 1;
            if (isSkipActive || currentThemeMode == TitleThemeMode.DemonLordRebellion)
            {
                ShowTownHub(true);
            }
            else
            {
                StartHolyPrologueQuest();
            }
        }

        public void StartHolyPrologueQuest()
        {
            if (titlePanel != null) titlePanel.SetActive(false);
            if (townHubPanel != null) townHubPanel.SetActive(false);
            if (inGameHudPanel != null) inGameHudPanel.SetActive(false);
            CloseAllModals();

            if (StoryDialogueUI.Instance != null)
            {
                StoryDialogueUI.Instance.PlayPrebuiltSequence(StorySequenceId.RealWorldPrologue, () =>
                {
                    Tutorial.TutorialScenarioManager.EnsureInstance().StartStage1HolyCrusade();
                });
            }
            else
            {
                Tutorial.TutorialScenarioManager.EnsureInstance().StartStage1HolyCrusade();
            }
        }

        public void EnterHexBattlefield()
        {
            PlaySoundClick();
            isInGame = true;
            isInTownHub = false;
            isPaused = false;

            if (titlePanel != null) titlePanel.SetActive(false);
            if (townHubPanel != null) townHubPanel.SetActive(false);
            if (inGameHudPanel != null)
            {
                inGameHudPanel.SetActive(true);
                var hudUI = inGameHudPanel.GetComponent<CombatHudCanvasUI>() ?? inGameHudPanel.AddComponent<CombatHudCanvasUI>();
                hudUI.EnsureHudBuilt();
            }
            CloseAllModals();
            Combat.SoundManager3D.Instance?.PlayBattleBGM();

            if (TacticalCameraController.Instance != null)
            {
                TacticalCameraController.Instance.ResetToTacticalView();
            }

            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.ShowBanner(
                    "✦ EXPEDITION LAUNCHED ✦", 
                    "Demon Lord vanguard marches through the Abyssal Rift! Crush the holy invaders!", 
                    1.8f, 
                    new Color(0.3f, 0.85f, 1.0f)
                );
            }
        }

        public void ReturnToTownHub()
        {
            PlaySoundClick();
            if (pauseModal != null) pauseModal.SetActive(false);
            ShowTownHub();
        }

        public void OpenPauseMenu()
        {
            PlaySoundClick();
            isPaused = true;
            if (pauseModal != null) pauseModal.SetActive(true);
        }

        public void ResumeGame()
        {
            PlaySoundClick();
            isPaused = false;
            if (pauseModal != null) pauseModal.SetActive(false);
            CloseAllModals();
        }

        public void OpenModal(GameObject modal)
        {
            PlaySoundClick();
            CloseAllModals();
            activeModal = modal;
            if (activeModal != null) activeModal.SetActive(true);
        }

        public void CloseActiveModal()
        {
            PlaySoundClick();
            if (activeModal != null)
            {
                activeModal.SetActive(false);
                activeModal = null;
            }

            if (returnToPauseFromOptions && isInGame)
            {
                returnToPauseFromOptions = false;
                OpenPauseMenu();
            }
        }

        private void CloseAllModals()
        {
            if (storyModal != null) storyModal.SetActive(false);
            if (howToPlayModal != null) howToPlayModal.SetActive(false);
            if (optionsModal != null) optionsModal.SetActive(false);
            if (pauseModal != null) pauseModal.SetActive(false);
            activeModal = null;
        }

        public void HandleEscapeKey()
        {
            if (activeModal != null)
            {
                CloseActiveModal();
            }
            else if (isInTownHub)
            {
                ShowTitleScreen();
            }
            else if (isInGame)
            {
                if (isPaused) ResumeGame();
                else OpenPauseMenu();
            }
        }

        public void OnRestartClicked()
        {
            PlaySoundClick();
            ResumeGame();
            if (TurnManager3D.Instance != null)
            {
                TurnManager3D.Instance.RestartBattle();
            }
        }

        public void OnReturnTitleClicked()
        {
            PlaySoundClick();
            if (TurnManager3D.Instance != null)
            {
                TurnManager3D.Instance.RestartBattle();
            }

            if (townHubPanel != null)
            {
                ShowTownHub();
            }
            else
            {
                ShowTitleScreen();
            }
        }

        public void OnExitClicked()
        {
            PlaySoundClick();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ToggleAudio()
        {
            if (SoundManager3D.Instance != null)
            {
                SoundManager3D.Instance.IsMuted = !SoundManager3D.Instance.IsMuted;
            }
            PlaySoundClick();
            UpdateOptionsDisplay();
        }

        private void ToggleAI()
        {
            if (TurnManager3D.Instance != null)
            {
                TurnManager3D.Instance.DisableEnemyAI = !TurnManager3D.Instance.DisableEnemyAI;
            }
            PlaySoundClick();
            UpdateOptionsDisplay();
        }

        private void ToggleDisplay()
        {
            Screen.fullScreen = !Screen.fullScreen;
            PlaySoundClick();
            UpdateOptionsDisplay();
        }

        private void UpdateOptionsDisplay()
        {
            if (txtAudioStatus != null)
            {
                bool isMuted = SoundManager3D.Instance != null && SoundManager3D.Instance.IsMuted;
                txtAudioStatus.text = isMuted ? "Sound: MUTED" : "Sound: ON";
            }

            if (txtAiStatus != null)
            {
                bool aiOff = TurnManager3D.Instance != null && TurnManager3D.Instance.DisableEnemyAI;
                txtAiStatus.text = aiOff ? "Enemy AI: DISABLED (Testing)" : "Enemy AI: ENABLED (Normal)";
            }

            if (txtDisplayStatus != null)
            {
                txtDisplayStatus.text = Screen.fullScreen ? "Display: Fullscreen" : "Display: Windowed";
            }

            if (txtThemeModeStatus != null)
            {
                txtThemeModeStatus.text = currentThemeMode == TitleThemeMode.PaladinHolyland
                    ? "Mode: HOLYLAND (Play = Tutorial)"
                    : "Mode: DEMON LORD (Play = Citadel Hub)";
            }
        }

        private static readonly Dictionary<string, Sprite> s_logoSpriteCache = new Dictionary<string, Sprite>();

        /// <summary>
        /// Robustly loads a custom UI logo sprite from Resources, AssetDatabase (in editor),
        /// StreamingAssets, or raw PNG file bytes on disk, caching the result.
        /// </summary>
        public static Sprite LoadLogoSprite(string baseName)
        {
            if (string.IsNullOrEmpty(baseName)) return null;
            if (baseName.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
            {
                baseName = Path.GetFileNameWithoutExtension(baseName);
            }

            if (s_logoSpriteCache.TryGetValue(baseName, out Sprite cached) && cached != null)
            {
                return cached;
            }

            // 1. Try Resources.Load<Sprite>
            Sprite sp = Resources.Load<Sprite>($"UI/{baseName}");
            if (sp == null)
            {
                Texture2D resTex = Resources.Load<Texture2D>($"UI/{baseName}");
                if (resTex != null)
                {
                    sp = Sprite.Create(resTex, new Rect(0, 0, resTex.width, resTex.height), new Vector2(0.5f, 0.5f), 100f);
                    sp.name = baseName;
                }
            }
            if (sp != null)
            {
                s_logoSpriteCache[baseName] = sp;
                return sp;
            }

#if UNITY_EDITOR
            // 2. Editor AssetDatabase lookup
            string[] searchPaths = new string[]
            {
                $"Assets/Resources/UI/{baseName}.png",
                $"Assets/Sprites/UI/{baseName}.png",
                $"Assets/UI/{baseName}.png"
            };

            foreach (string p in searchPaths)
            {
                Sprite edSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p);
                if (edSprite != null)
                {
                    s_logoSpriteCache[baseName] = edSprite;
                    return edSprite;
                }
                Texture2D edTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                if (edTex != null)
                {
                    sp = Sprite.Create(edTex, new Rect(0, 0, edTex.width, edTex.height), new Vector2(0.5f, 0.5f), 100f);
                    sp.name = baseName;
                    s_logoSpriteCache[baseName] = sp;
                    return sp;
                }
            }
#endif

            // 3. Direct disk file fallback (Assets/Resources/UI, Assets/Sprites/UI, StreamingAssets)
            string[] diskPaths = new string[]
            {
                Path.Combine(Application.dataPath, "Resources", "UI", $"{baseName}.png"),
                Path.Combine(Application.dataPath, "Sprites", "UI", $"{baseName}.png"),
                Path.Combine(Application.streamingAssetsPath, "UI", $"{baseName}.png")
            };

            foreach (string diskPath in diskPaths)
            {
                if (File.Exists(diskPath))
                {
                    try
                    {
                        byte[] bytes = File.ReadAllBytes(diskPath);
                        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (tex.LoadImage(bytes))
                        {
                            tex.name = baseName;
                            sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                            s_logoSpriteCache[baseName] = sp;
                            return sp;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[TitleMenuCanvasUI] Failed to load logo from {diskPath}: {ex.Message}");
                    }
                }
            }

            return null;
        }

        private void PlaySoundClick()
        {
            if (SoundManager3D.Instance != null)
            {
                SoundManager3D.Instance.PlayButtonClick();
            }
        }
    }
}

