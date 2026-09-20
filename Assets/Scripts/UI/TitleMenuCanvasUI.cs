using System.Collections;
using System.Collections.Generic;
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

        [Header("Options Controls")]
        [SerializeField] private Button btnAudioToggle;
        [SerializeField] private Text txtAudioStatus;
        [SerializeField] private Button btnAiToggle;
        [SerializeField] private Text txtAiStatus;
        [SerializeField] private Button btnDisplayToggle;
        [SerializeField] private Text txtDisplayStatus;

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

            AdjustModalLayouts();
            AdjustTitleScreenLayout();
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
                    if (cRect != null) cRect.sizeDelta = new Vector2(600f, 470f);

                    Transform tTrans = card.Find("Text_ModalTitle");
                    if (tTrans != null)
                    {
                        RectTransform tRect = tTrans.GetComponent<RectTransform>();
                        if (tRect != null) tRect.anchoredPosition = new Vector2(0f, 160f);
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
                        if (tRect != null) tRect.anchoredPosition = new Vector2(0f, 195f);
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
                        if (tRect != null) tRect.anchoredPosition = new Vector2(0f, 205f);
                    }
                }
            }
        }

        /// <summary>
        /// Restores missing title texts, applies overflow to prevent clipping,
        /// sharpens button proportions, and adds soft diorama background dimming.
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
                    hRect.sizeDelta = new Vector2(880f, 260f);
                    hRect.anchoredPosition = new Vector2(0f, -30f);
                }

                Transform tBadge = header.Find("Text_Badge");
                if (tBadge != null)
                {
                    var r = tBadge.GetComponent<RectTransform>();
                    if (r != null) { r.anchoredPosition = new Vector2(0f, 75f); r.sizeDelta = new Vector2(840f, 28f); }
                    var txt = tBadge.GetComponent<Text>();
                    if (txt != null) { txt.horizontalOverflow = HorizontalWrapMode.Overflow; txt.verticalOverflow = VerticalWrapMode.Overflow; }
                }

                Transform t1 = header.Find("Text_Title1");
                if (t1 != null)
                {
                    var r = t1.GetComponent<RectTransform>();
                    if (r != null) { r.anchoredPosition = new Vector2(0f, 32f); r.sizeDelta = new Vector2(840f, 48f); }
                    var txt = t1.GetComponent<Text>();
                    if (txt != null)
                    {
                        txt.text = "I REINCARNATED AS A BEAST TALKER,";
                        txt.fontSize = 28;
                        txt.color = Color.white;
                        txt.fontStyle = FontStyle.Bold;
                        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
                        txt.verticalOverflow = VerticalWrapMode.Overflow;
                    }
                }

                Transform t2 = header.Find("Text_Title2");
                if (t2 != null)
                {
                    var r = t2.GetComponent<RectTransform>();
                    if (r != null) { r.anchoredPosition = new Vector2(0f, -14f); r.sizeDelta = new Vector2(840f, 52f); }
                    var txt = t2.GetComponent<Text>();
                    if (txt != null)
                    {
                        txt.text = "NOW I'M COLLECTING BEASTS!";
                        txt.fontSize = 34;
                        txt.color = new Color(1f, 0.85f, 0.25f);
                        txt.fontStyle = FontStyle.Bold;
                        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
                        txt.verticalOverflow = VerticalWrapMode.Overflow;
                    }
                }

                Transform sub = header.Find("Text_Subtitle");
                if (sub != null)
                {
                    var r = sub.GetComponent<RectTransform>();
                    if (r != null) { r.anchoredPosition = new Vector2(0f, -58f); r.sizeDelta = new Vector2(840f, 26f); }
                    var txt = sub.GetComponent<Text>();
                    if (txt != null) { txt.horizontalOverflow = HorizontalWrapMode.Overflow; txt.verticalOverflow = VerticalWrapMode.Overflow; }
                }

                Transform ver = header.Find("Text_Version");
                if (ver != null)
                {
                    var r = ver.GetComponent<RectTransform>();
                    if (r != null) { r.anchoredPosition = new Vector2(0f, -86f); r.sizeDelta = new Vector2(840f, 22f); }
                    var txt = ver.GetComponent<Text>();
                    if (txt != null) { txt.horizontalOverflow = HorizontalWrapMode.Overflow; txt.verticalOverflow = VerticalWrapMode.Overflow; }
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
                var vlg = menuButtons.GetComponent<VerticalLayoutGroup>();
                if (vlg != null) vlg.spacing = 10;

                Text[] buttonTexts = menuButtons.GetComponentsInChildren<Text>(true);
                foreach (var bt in buttonTexts)
                {
                    bt.horizontalOverflow = HorizontalWrapMode.Overflow;
                    bt.verticalOverflow = VerticalWrapMode.Overflow;
                }
            }

            // Ensure soft dimmer vignette behind title panel
            Transform dimmer = titlePanel.transform.Find("DioramaDimmer");
            if (dimmer == null)
            {
                GameObject dimmerObj = new GameObject("DioramaDimmer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                dimmerObj.transform.SetParent(titlePanel.transform, false);
                dimmerObj.transform.SetAsFirstSibling();
                RectTransform dRect = dimmerObj.GetComponent<RectTransform>();
                dRect.anchorMin = Vector2.zero;
                dRect.anchorMax = Vector2.one;
                dRect.sizeDelta = Vector2.zero;
                dRect.anchoredPosition = Vector2.zero;
                Image dImg = dimmerObj.GetComponent<Image>();
                dImg.color = new Color(0.04f, 0.06f, 0.09f, 0.45f);
                dImg.raycastTarget = false;
            }
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
        }

        public void ShowTownHub()
        {
            isInGame = false;
            isInTownHub = true;
            isPaused = false;

            if (titlePanel != null) titlePanel.SetActive(false);
            if (townHubPanel != null) townHubPanel.SetActive(true);
            if (inGameHudPanel != null) inGameHudPanel.SetActive(false);
            CloseAllModals();
        }

        public void OnPlayClicked()
        {
            PlaySoundClick();
            // Enter the Citadel Town Hub first!
            if (townHubPanel != null)
            {
                ShowTownHub();
            }
            else
            {
                EnterHexBattlefield();
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
            if (inGameHudPanel != null) inGameHudPanel.SetActive(true);
            CloseAllModals();

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

