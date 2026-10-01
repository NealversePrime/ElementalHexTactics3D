using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using ElementalHexTactics3D.Combat;

namespace ElementalHexTactics3D.UI
{
    public enum StorySequenceId
    {
        RealWorldPrologue,      // MC at midnight desk playing Omniterra
        HolyRaidIntro,          // Battle 1 Paladin taunting Demon Lord
        HolyRaidVictoryGlitch,  // Post-battle glitch & Developer/God transmigration
        HubAwakeningDemonLord,  // Waking up in Citadel Town Hub with Basalt Vanguard general
        Stage2FrontierIntro,    // Battle 2 Demon Lord & Basalt Vanguard teaching terrain alchemy & hazards
        Stage2Victory,          // Victory against scouts, war horn echoes
        HubTitanCrisisDialogue, // Hub dialogue explaining the Heavy Crusade and Titan summoning
        Stage3TitanIntro,       // Battle 3 opening the Abyssal Rift and summoning Earth Golem
        Stage3Victory,          // Stage 3 victory claiming 1st Primordial Titan Core

        // Compatibility aliases
        DemonAwakeningIntro = Stage2FrontierIntro,
        Tutorial2Victory = Stage3Victory
    }

    [Serializable]
    public class DialogueLine
    {
        public string speakerName;
        [TextArea(2, 5)]
        public string text;
        public Sprite speakerPortrait;
        public bool isLeftSpeaker = true;
        public Sprite backgroundArt;
        public Color nameColor = Color.white;
        public bool triggerGlitch = false;
        public float typewriterSpeed = 0.022f;

        public DialogueLine() { }

        public DialogueLine(string speaker, string body, Sprite portrait = null, bool left = true, Sprite bg = null, Color? color = null, bool glitch = false)
        {
            speakerName = speaker;
            text = body;
            speakerPortrait = portrait;
            isLeftSpeaker = left;
            backgroundArt = bg;
            nameColor = color ?? Color.white;
            triggerGlitch = glitch;
        }
    }

    /// <summary>
    /// Visual Novel Dialogue & Cutscene Overlay Engine.
    /// Supports typewriter text effects, left/right animated portraits,
    /// dynamic full-screen backgrounds, glitch distortions, and narrative sequence hooks.
    /// </summary>
    public class StoryDialogueUI : MonoBehaviour
    {
        public static StoryDialogueUI Instance { get; private set; }

        [Header("UI Roots & Panels")]
        [SerializeField] private GameObject rootDialoguePanel;
        [SerializeField] private Image backgroundArtImage;
        [SerializeField] private Image glitchOverlayImage;
        [SerializeField] private RectTransform dialogueBoxRect;

        [Header("Portraits")]
        [SerializeField] private Image portraitLeft;
        [SerializeField] private Image portraitRight;
        [SerializeField] private CanvasGroup portraitLeftGroup;
        [SerializeField] private CanvasGroup portraitRightGroup;

        [Header("Dialogue Box Texts")]
        [SerializeField] private Text txtSpeakerName;
        [SerializeField] private Text txtDialogueBody;
        [SerializeField] private GameObject continueIndicator;
        [SerializeField] private Button btnSkip;

        [Header("Key Portait Sprites")]
        [SerializeField] private Sprite mcSilhouetteSprite;
        [SerializeField] private Sprite paladinPortrait;
        [SerializeField] private Sprite holyShielderPortrait;
        [SerializeField] private Sprite demonLordPortrait;
        [SerializeField] private Sprite basaltVanguardPortrait;

        [Header("Key Background Sprites")]
        [SerializeField] private Sprite bgMcRoom;
        [SerializeField] private Sprite bgCathedral;
        [SerializeField] private Sprite bgDemonLordThrone;

        public bool IsPlayingDialogue { get; private set; } = false;

        private List<DialogueLine> currentLines = new List<DialogueLine>();
        private int currentLineIndex = -1;
        private Action onSequenceCompleteCallback;
        private Coroutine typingCoroutine;
        private bool isTyping = false;
        private string currentTargetText = "";

        public event Action<StorySequenceId> OnStorySequenceStarted;
        public event Action<StorySequenceId> OnStorySequenceFinished;

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

            LoadSpriteFallbacks();
            EnsureUIHierarchy();

            if (btnSkip != null)
            {
                btnSkip.onClick.AddListener(SkipSequence);
            }

            // Start with overlay hidden
            if (rootDialoguePanel != null)
            {
                rootDialoguePanel.SetActive(false);
            }
        }

        private void Update()
        {
            if (!IsPlayingDialogue) return;

            // Space, Enter, or Left Mouse Click to advance or speed up
            bool advancePressed = false;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)
                {
                    advancePressed = true;
                }
                if (Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    SkipSequence();
                    return;
                }
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                advancePressed = true;
            }

            if (advancePressed)
            {
                OnAdvanceInput();
            }
        }

        /// <summary>
        /// Loads default sprites from project if not set in Inspector.
        /// </summary>
        private void LoadSpriteFallbacks()
        {
#if UNITY_EDITOR
            if (mcSilhouetteSprite == null)
                mcSilhouetteSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Portraits/mc_silhouette.png");
            if (paladinPortrait == null)
                paladinPortrait = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Portraits/paladinportrait.png");
            if (holyShielderPortrait == null)
                holyShielderPortrait = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Portraits/holyshielderportrait.png");
            if (demonLordPortrait == null)
                demonLordPortrait = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Portraits/demonlordportrait.png");
            if (basaltVanguardPortrait == null)
                basaltVanguardPortrait = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Portraits/minotaurportrait.png");

            if (bgMcRoom == null)
                bgMcRoom = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Backgrounds/bg_mc_room.jpg");
            if (bgCathedral == null)
                bgCathedral = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Backgrounds/bg_holyland_cathedral.jpg");
            if (bgDemonLordThrone == null)
                bgDemonLordThrone = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Backgrounds/bg_demonlord_throne.jpg");
#endif
            // Universal runtime fallbacks via TacticalUnitSpawner
            if (mcSilhouetteSprite == null)
                mcSilhouetteSprite = ElementalHexTactics3D.Units.TacticalUnitSpawner.LoadPortraitSprite("mc_silhouette.png");
            if (paladinPortrait == null)
                paladinPortrait = ElementalHexTactics3D.Units.TacticalUnitSpawner.LoadPortraitSprite("paladinportrait.png");
            if (holyShielderPortrait == null)
                holyShielderPortrait = ElementalHexTactics3D.Units.TacticalUnitSpawner.LoadPortraitSprite("holyshielderportrait.png");
            if (demonLordPortrait == null)
                demonLordPortrait = ElementalHexTactics3D.Units.TacticalUnitSpawner.LoadPortraitSprite("demonlordportrait.png");
            if (basaltVanguardPortrait == null)
                basaltVanguardPortrait = ElementalHexTactics3D.Units.TacticalUnitSpawner.LoadPortraitSprite("minotaurportrait.png");

            if (bgMcRoom == null)
                bgMcRoom = ElementalHexTactics3D.Units.TacticalUnitSpawner.LoadBackgroundSprite("bg_mc_room.jpg");
            if (bgCathedral == null)
                bgCathedral = ElementalHexTactics3D.Units.TacticalUnitSpawner.LoadBackgroundSprite("bg_holyland_cathedral.jpg");
            if (bgDemonLordThrone == null)
                bgDemonLordThrone = ElementalHexTactics3D.Units.TacticalUnitSpawner.LoadBackgroundSprite("bg_demonlord_throne.jpg");
        }

        /// <summary>
        /// Ensures all necessary visual components exist.
        /// </summary>
        public void EnsureUIHierarchy()
        {
            if (rootDialoguePanel == null)
            {
                Transform panel = transform.Find("Panel_StoryDialogueRoot");
                if (panel != null) rootDialoguePanel = panel.gameObject;
            }

            if (rootDialoguePanel != null)
            {
                // Ensure full-screen raycast blocker on root panel so clicks during dialogue advance story without hitting background buttons
                Image rootRaycastBlocker = rootDialoguePanel.GetComponent<Image>();
                if (rootRaycastBlocker == null)
                {
                    rootRaycastBlocker = rootDialoguePanel.AddComponent<Image>();
                }
                rootRaycastBlocker.color = new Color(0f, 0f, 0f, 0f);
                rootRaycastBlocker.raycastTarget = true;

                if (backgroundArtImage == null)
                {
                    Transform bg = rootDialoguePanel.transform.Find("BackgroundArt");
                    if (bg != null) backgroundArtImage = bg.GetComponent<Image>();
                }
                if (glitchOverlayImage == null)
                {
                    Transform g = rootDialoguePanel.transform.Find("GlitchOverlay");
                    if (g != null) glitchOverlayImage = g.GetComponent<Image>();
                }
                if (portraitLeft == null)
                {
                    Transform pl = rootDialoguePanel.transform.Find("Portrait_Left");
                    if (pl != null)
                    {
                        portraitLeft = pl.GetComponent<Image>();
                        portraitLeftGroup = pl.GetComponent<CanvasGroup>() ?? pl.gameObject.AddComponent<CanvasGroup>();
                    }
                }
                if (portraitRight == null)
                {
                    Transform pr = rootDialoguePanel.transform.Find("Portrait_Right");
                    if (pr != null)
                    {
                        portraitRight = pr.GetComponent<Image>();
                        portraitRightGroup = pr.GetComponent<CanvasGroup>() ?? pr.gameObject.AddComponent<CanvasGroup>();
                    }
                }
                if (dialogueBoxRect == null)
                {
                    Transform box = rootDialoguePanel.transform.Find("DialogueBox");
                    if (box != null) dialogueBoxRect = box.GetComponent<RectTransform>();
                }
                if (dialogueBoxRect != null)
                {
                    if (txtSpeakerName == null)
                    {
                        Transform tName = dialogueBoxRect.Find("Nameplate/Text_SpeakerName");
                        if (tName != null) txtSpeakerName = tName.GetComponent<Text>();
                    }
                    if (txtDialogueBody == null)
                    {
                        Transform tBody = dialogueBoxRect.Find("Text_DialogueBody");
                        if (tBody != null) txtDialogueBody = tBody.GetComponent<Text>();
                    }
                    if (continueIndicator == null)
                    {
                        Transform ind = dialogueBoxRect.Find("ContinueIndicator");
                        if (ind != null) continueIndicator = ind.gameObject;
                    }
                }
                if (btnSkip == null)
                {
                    Transform s = rootDialoguePanel.transform.Find("Btn_Skip");
                    if (s != null) btnSkip = s.GetComponent<Button>();
                }
            }
        }

        /// <summary>
        /// Plays one of the pre-designed story cutscenes.
        /// </summary>
        public void PlayPrebuiltSequence(StorySequenceId sequenceId, Action onComplete = null)
        {
            LoadSpriteFallbacks();
            List<DialogueLine> lines = BuildSequence(sequenceId);
            OnStorySequenceStarted?.Invoke(sequenceId);

            PlaySequence(lines, () =>
            {
                OnStorySequenceFinished?.Invoke(sequenceId);
                onComplete?.Invoke();
            });
        }

        /// <summary>
        /// Plays an arbitrary list of dialogue lines.
        /// </summary>
        public void PlaySequence(List<DialogueLine> lines, Action onComplete = null)
        {
            if (lines == null || lines.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            LoadSpriteFallbacks();
            EnsureUIHierarchy();

            // Clear any lingering portrait states from previous sequence
            if (portraitLeft != null) { portraitLeft.gameObject.SetActive(false); portraitLeft.sprite = null; }
            if (portraitRight != null) { portraitRight.gameObject.SetActive(false); portraitRight.sprite = null; }
            if (backgroundArtImage != null) { backgroundArtImage.gameObject.SetActive(false); backgroundArtImage.sprite = null; }

            currentLines = new List<DialogueLine>(lines);
            currentLineIndex = -1;
            onSequenceCompleteCallback = onComplete;
            IsPlayingDialogue = true;

            if (rootDialoguePanel != null)
            {
                rootDialoguePanel.SetActive(true);
            }

            DisplayNextLine();
        }

        private void OnAdvanceInput()
        {
            if (isTyping)
            {
                // Speed up: complete typewriter instantly
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                isTyping = false;
                if (txtDialogueBody != null) txtDialogueBody.text = currentTargetText;
                if (continueIndicator != null) continueIndicator.SetActive(true);
            }
            else
            {
                DisplayNextLine();
            }
        }

        private void DisplayNextLine()
        {
            currentLineIndex++;
            if (currentLineIndex >= currentLines.Count)
            {
                FinishSequence();
                return;
            }

            DialogueLine line = currentLines[currentLineIndex];

            // 1. Background Art
            if (backgroundArtImage != null)
            {
                if (line.backgroundArt != null)
                {
                    backgroundArtImage.sprite = line.backgroundArt;
                    backgroundArtImage.color = Color.white;
                    backgroundArtImage.gameObject.SetActive(true);
                }
                else
                {
                    // No 2D background assigned (e.g. in-battle banter):
                    // Hide 2D background so 3D battlefield is clearly visible behind dialog!
                    backgroundArtImage.gameObject.SetActive(false);
                }
            }

            // 2. Portraits (Left vs Right focus)
            UpdatePortraits(line);

            // 3. Speaker Nameplate
            if (txtSpeakerName != null)
            {
                txtSpeakerName.text = line.speakerName;
                txtSpeakerName.color = line.nameColor;
            }

            // 4. Glitch Effect Trigger
            if (line.triggerGlitch)
            {
                StartCoroutine(TriggerGlitchFX());
            }

            // 5. Typewriter Dialogue Body
            if (continueIndicator != null) continueIndicator.SetActive(false);
            currentTargetText = line.text;

            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypewriterRoutine(line.text, line.typewriterSpeed));
        }

        private void UpdatePortraits(DialogueLine line)
        {
            if (line.speakerPortrait != null)
            {
                if (line.isLeftSpeaker)
                {
                    if (portraitLeft != null)
                    {
                        portraitLeft.sprite = line.speakerPortrait;
                        portraitLeft.color = Color.white;
                        portraitLeft.gameObject.SetActive(true);
                        SetGroupAlpha(portraitLeftGroup, 1f);
                        portraitLeft.transform.localScale = Vector3.one * 1.05f;
                    }
                    if (portraitRight != null && portraitRight.gameObject.activeSelf)
                    {
                        SetGroupAlpha(portraitRightGroup, 0.45f);
                        portraitRight.transform.localScale = Vector3.one * 0.95f;
                    }
                }
                else
                {
                    if (portraitRight != null)
                    {
                        portraitRight.sprite = line.speakerPortrait;
                        portraitRight.color = Color.white;
                        portraitRight.gameObject.SetActive(true);
                        SetGroupAlpha(portraitRightGroup, 1f);
                        portraitRight.transform.localScale = Vector3.one * 1.05f;
                    }
                    if (portraitLeft != null && portraitLeft.gameObject.activeSelf)
                    {
                        SetGroupAlpha(portraitLeftGroup, 0.45f);
                        portraitLeft.transform.localScale = Vector3.one * 0.95f;
                    }
                }
            }
            else
            {
                // System or anonymous narration: hide portraits
                if (portraitLeft != null) portraitLeft.gameObject.SetActive(false);
                if (portraitRight != null) portraitRight.gameObject.SetActive(false);
            }
        }

        private void SetGroupAlpha(CanvasGroup group, float alpha)
        {
            if (group != null) group.alpha = alpha;
        }

        private IEnumerator TypewriterRoutine(string fullText, float delay)
        {
            isTyping = true;
            if (txtDialogueBody != null) txtDialogueBody.text = "";

            bool inTag = false;
            for (int i = 0; i < fullText.Length; i++)
            {
                char c = fullText[i];
                if (c == '<') inTag = true;
                if (c == '>') { inTag = false; continue; }

                if (inTag) continue; // Skip waiting inside rich text tags

                if (txtDialogueBody != null)
                {
                    txtDialogueBody.text = fullText.Substring(0, i + 1);
                }

                if (i % 3 == 0 && SoundManager3D.Instance != null)
                {
                    SoundManager3D.Instance.PlayButtonClick();
                }

                yield return new WaitForSecondsRealtime(delay);
            }

            if (txtDialogueBody != null) txtDialogueBody.text = fullText;
            isTyping = false;
            if (continueIndicator != null) continueIndicator.SetActive(true);
        }

        private IEnumerator TriggerGlitchFX()
        {
            if (glitchOverlayImage == null) yield break;

            glitchOverlayImage.gameObject.SetActive(true);
            Color baseColor = new Color(0.95f, 0.2f, 0.2f, 0.45f);

            Vector3 origPos = dialogueBoxRect != null ? dialogueBoxRect.anchoredPosition : Vector3.zero;

            for (int i = 0; i < 6; i++)
            {
                glitchOverlayImage.color = (i % 2 == 0) ? baseColor : new Color(0.1f, 0.8f, 1f, 0.35f);
                if (dialogueBoxRect != null)
                {
                    dialogueBoxRect.anchoredPosition = (Vector2)origPos + new Vector2(UnityEngine.Random.Range(-12f, 12f), UnityEngine.Random.Range(-8f, 8f));
                }
                yield return new WaitForSecondsRealtime(0.06f);
            }

            if (dialogueBoxRect != null) dialogueBoxRect.anchoredPosition = origPos;
            glitchOverlayImage.gameObject.SetActive(false);
        }

        public void SkipSequence()
        {
            if (!IsPlayingDialogue) return;
            FinishSequence();
        }

        private void FinishSequence()
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            isTyping = false;
            IsPlayingDialogue = false;

            if (rootDialoguePanel != null)
            {
                rootDialoguePanel.SetActive(false);
            }

            Action cb = onSequenceCompleteCallback;
            onSequenceCompleteCallback = null;
            cb?.Invoke();
        }

        #region Prebuilt Story Sequences

        private List<DialogueLine> BuildSequence(StorySequenceId id)
        {
            List<DialogueLine> lines = new List<DialogueLine>();

            switch (id)
            {
                case StorySequenceId.RealWorldPrologue:
                    lines.Add(new DialogueLine(
                        "MC (Gamer / Salaryman)",
                        "23:45... Finally punched out after another brutal day of unpaid overtime.",
                        mcSilhouetteSprite, true, bgMcRoom, new Color(0.4f, 0.85f, 1f)
                    ));
                    lines.Add(new DialogueLine(
                        "MC (Gamer / Salaryman)",
                        "Just one last raid boss in Omniterra before I crash. As the server's #1 solo ranker, that Demon Lord falls tonight.",
                        mcSilhouetteSprite, true, bgMcRoom, new Color(0.4f, 0.85f, 1f)
                    ));
                    lines.Add(new DialogueLine(
                        "System (Omniterra)",
                        "[ SYSTEM ALERT: ENTERING THE ABYSSAL THRONE — RADIANT CRUSADE DISPATCHED ]",
                        null, true, bgCathedral, new Color(1f, 0.88f, 0.25f)
                    ));
                    lines.Add(new DialogueLine(
                        "MC (Gamer / Salaryman)",
                        "My Paladin is fully geared. Time to purge the darkness and clear this game once and for all.",
                        mcSilhouetteSprite, true, bgMcRoom, new Color(0.4f, 0.85f, 1f)
                    ));
                    break;

                case StorySequenceId.HolyRaidIntro:
                    lines.Add(new DialogueLine(
                        "Paladin (Chosen Hero)",
                        "Foul fiend! Your reign of shadow ends tonight in the holy name of the Radiant Synod of Aethelgard!",
                        paladinPortrait, true, null, new Color(1f, 0.85f, 0.25f)
                    ));
                    lines.Add(new DialogueLine(
                        "Demon Lord",
                        "Insolent insects! Abyssal spawn, crawl forth and tear their holy flesh apart! Feed upon their bones!",
                        demonLordPortrait, false, null, new Color(0.95f, 0.4f, 0.4f)
                    ));
                    lines.Add(new DialogueLine(
                        "Paladin (Chosen Hero)",
                        "He's sending his vanguard minion forward! Holy Shielder, form ranks—let us purge this beast before taking down the Demon Lord!",
                        paladinPortrait, true, null, new Color(1f, 0.85f, 0.25f)
                    ));
                    break;

                case StorySequenceId.HolyRaidVictoryGlitch:
                    lines.Add(new DialogueLine(
                        "Paladin (Chosen Hero)",
                        "Victory for the Radiant Synod! The lord of shadows has been cleansed from Omniterra!",
                        paladinPortrait, true, null, new Color(1f, 0.85f, 0.25f)
                    ));
                    lines.Add(new DialogueLine(
                        "MC (Real World)",
                        "Phew... solo raid clear complete! That cements my spot as the server's #1 solo ranker... But man, my head is throbbing...",
                        mcSilhouetteSprite, true, bgMcRoom, new Color(0.4f, 0.85f, 1f)
                    ));
                    lines.Add(new DialogueLine(
                        "MC (Real World)",
                        "Wait... my chest feels tight... my monitor's screen is distorting... I just need to lie down...",
                        mcSilhouetteSprite, true, bgMcRoom, new Color(0.4f, 0.85f, 1f), true
                    ));
                    lines.Add(new DialogueLine(
                        "The Architect (System Creator)",
                        "[ ACHIEVEMENT CONFIRMED: 100% SOLO CLEAR — 'OMNITERRA' ]",
                        null, true, null, new Color(0.2f, 0.95f, 0.6f), true
                    ));
                    lines.Add(new DialogueLine(
                        "The Architect (System Creator)",
                        "You boast of 'divine righteousness' while trampling the forgotten... Yet have you ever heard the cries of those you branded 'monsters'?",
                        null, true, null, new Color(0.2f, 0.95f, 0.6f)
                    ));
                    lines.Add(new DialogueLine(
                        "The Architect (System Creator)",
                        "Congratulations, Ranker #1. You have qualified. Now... let us see how you fare when the chessboard is inverted.",
                        null, true, null, new Color(0.2f, 0.95f, 0.6f), true
                    ));
                    lines.Add(new DialogueLine(
                        "MC (Real World)",
                        "W-what is this?! The monitor is pulling my arms in... My body is being dragged into the screen—WAIT, STOP—AAAAAAAGH!!",
                        mcSilhouetteSprite, true, bgMcRoom, new Color(0.95f, 0.3f, 0.3f), true
                    ));
                    break;

                case StorySequenceId.HubAwakeningDemonLord:
                    lines.Add(new DialogueLine(
                        "MC / Demon Lord",
                        "Ugh... my skull is splitting... Where am I? These hands... obsidian talons?! And this mantle of abyssal steel...?!",
                        demonLordPortrait, true, bgDemonLordThrone, new Color(0.95f, 0.4f, 0.4f)
                    ));
                    lines.Add(new DialogueLine(
                        "Basalt Vanguard",
                        "Lord Malakor! Praise the Primordials, your consciousness has returned! The Citadel's core altar held through the dimensional cataclysm!",
                        basaltVanguardPortrait, false, bgDemonLordThrone, new Color(0.85f, 0.7f, 0.4f)
                    ));
                    lines.Add(new DialogueLine(
                        "MC / Demon Lord",
                        "(Wait... this booming voice, this hulking beastkin warrior in jagged basalt plate... Basalt Vanguard?! The frontline general of the outcast sanctuary?! Did I actually transmigrate into Omniterra?! Into the body of the Demon Lord I just killed in the raid?!)",
                        demonLordPortrait, true, bgDemonLordThrone, new Color(0.95f, 0.4f, 0.4f)
                    ));
                    lines.Add(new DialogueLine(
                        "MC / Demon Lord",
                        "Wait... Status Window! Open!\n[ SYSTEM STATUS: MALAKOR — LEVEL 1 ]\n[ HP: 18 / 18  |  MANA: 10 / 10  |  CORES: 0 ]\nWhat is this?! All my maxed endgame gear, god-tier attributes, and 9999 mana pool... stripped clean by The Architect?!",
                        demonLordPortrait, true, bgDemonLordThrone, new Color(0.95f, 0.35f, 0.35f), true
                    ));
                    lines.Add(new DialogueLine(
                        "Basalt Vanguard",
                        "Sire, during the cataclysm your abyssal core fractured! Your mana flow is reduced to fledgling sparks... Even manifesting a single basic Fireball will tax your current reserves.",
                        basaltVanguardPortrait, false, bgDemonLordThrone, new Color(0.85f, 0.7f, 0.4f)
                    ));
                    lines.Add(new DialogueLine(
                        "MC / Demon Lord",
                        "(Level 1... A fractured core and pathetic F-rank mana. But wait. I was the server's #1 solo ranker. I memorized every damage formula, reaction rule, and terrain threshold in Omniterra. Even at Level 1, game knowledge beats raw brute force!)",
                        demonLordPortrait, true, bgDemonLordThrone, new Color(0.4f, 0.85f, 1f)
                    ));
                    lines.Add(new DialogueLine(
                        "Basalt Vanguard",
                        "Sire, an emergency! A scout company of the Radiant Synod of Aethelgard has breached our outer perimeter in the Ashen Verge! They are pillaging the frontier outpost!",
                        basaltVanguardPortrait, false, bgDemonLordThrone, new Color(0.85f, 0.7f, 0.4f)
                    ));
                    lines.Add(new DialogueLine(
                        "MC / Demon Lord",
                        "(The Holy Empire troops I used to command... are invading my sanctuary?! If this citadel falls, I die for real in this world!)",
                        demonLordPortrait, true, bgDemonLordThrone, new Color(0.95f, 0.4f, 0.4f)
                    ));
                    lines.Add(new DialogueLine(
                        "Basalt Vanguard",
                        "Our frontier guards are falling back! Sire, we must ride to the frontier outpost immediately before their inquisitors locate the Citadel's hidden passage!",
                        basaltVanguardPortrait, false, bgDemonLordThrone, new Color(0.85f, 0.7f, 0.4f)
                    ));
                    lines.Add(new DialogueLine(
                        "MC / Demon Lord",
                        "Steady, General. As someone who knows every tactic of the Radiant Synod inside and out... I know their exact blindspots. Lead the way to the frontier clearing!",
                        demonLordPortrait, true, bgDemonLordThrone, new Color(0.95f, 0.4f, 0.4f)
                    ));
                    break;

                case StorySequenceId.Stage2FrontierIntro:
                    lines.Add(new DialogueLine(
                        "MC / Demon Lord",
                        "We've reached the frontier clearing. The Radiant Synod's scout unit has barricaded the passage ahead.",
                        demonLordPortrait, true, null, new Color(0.95f, 0.4f, 0.4f)
                    ));
                    lines.Add(new DialogueLine(
                        "Basalt Vanguard",
                        "Our forces are still in Citadel reserve, Sire. First, we must tear open an Abyssal Rift conduit at the southern clearing (0, -2) to bridge our deployment!",
                        basaltVanguardPortrait, false, null, new Color(0.85f, 0.7f, 0.4f)
                    ));
                    lines.Add(new DialogueLine(
                        "MC / Demon Lord",
                        "Right. Once our portal is anchored, I'll step through at (1, -2) and you advance alongside me. And notice the terrain ahead: the center hex at (0, 0) is already smoldering Scorched Earth (Tier 1)!",
                        demonLordPortrait, true, null, new Color(0.95f, 0.4f, 0.4f)
                    ));
                    lines.Add(new DialogueLine(
                        "Basalt Vanguard",
                        "A thermal chain reaction! Striking pre-heated Scorched ground with your Fireball triggers an instant breach into molten Magma (Tier 2)! Any unit standing on Magma suffers 3 Burn damage!",
                        basaltVanguardPortrait, false, null, new Color(0.85f, 0.7f, 0.4f)
                    ));
                    lines.Add(new DialogueLine(
                        "MC / Demon Lord",
                        "And once that Magma pit is blazing, your Golem Slam can shove their Scout Defender straight into the inferno. Let's tear open the Rift!",
                        demonLordPortrait, true, null, new Color(0.95f, 0.4f, 0.4f)
                    ));
                    break;

                case StorySequenceId.Stage2Victory:
                    lines.Add(new DialogueLine(
                        "Basalt Vanguard",
                        "The scout unit has fallen! Your elemental command was magnificent, Lord Malakor! Even with a Level 1 core, you weaponized the terrain flawlessly!",
                        basaltVanguardPortrait, false, bgDemonLordThrone, new Color(0.85f, 0.7f, 0.4f)
                    ));
                    lines.Add(new DialogueLine(
                        "MC / Demon Lord",
                        "Hold your celebration, General. Look at their armor insignia... this was merely a forward reconnaissance detachment.",
                        demonLordPortrait, true, bgDemonLordThrone, new Color(0.95f, 0.4f, 0.4f)
                    ));
                    lines.Add(new DialogueLine(
                        "Basalt Vanguard",
                        "[Deep brass war horns echo in the distance] ...Sire! That horn! The main crusader vanguard of the Radiant Synod is approaching our Abyssal Gateway!",
                        basaltVanguardPortrait, false, bgDemonLordThrone, new Color(0.85f, 0.7f, 0.4f)
                    ));
                    lines.Add(new DialogueLine(
                        "Basalt Vanguard",
                        "They have deployed Heavy Divine Wardens shielded by consecrated Aegis barriers! Our normal blades cannot pierce their holy enchantments!",
                        basaltVanguardPortrait, false, bgDemonLordThrone, new Color(0.85f, 0.7f, 0.4f)
                    ));
                    lines.Add(new DialogueLine(
                        "MC / Demon Lord",
                        "Then we shall answer divine barriers with primordial might. Look towards the Citadel's cliff—the Primordial Statue is blazing with draconic flame!",
                        demonLordPortrait, true, bgDemonLordThrone, new Color(0.95f, 0.4f, 0.4f)
                    ));
                    lines.Add(new DialogueLine(
                        "Basalt Vanguard",
                        "By the Ancients... the Primordial Statue! It is awakening the dormant slumber of the Magma Dragon! Let us return to the Citadel Hub and awaken our first Titan!",
                        basaltVanguardPortrait, false, bgDemonLordThrone, new Color(0.85f, 0.7f, 0.4f)
                    ));
                    break;

                case StorySequenceId.HubTitanCrisisDialogue:
                    lines.Add(new DialogueLine(
                        "Basalt Vanguard",
                        "Lord Malakor, the Primordial Statue on the cliff is resonating with flame energy! Commune with the statue and awaken our first Titan!",
                        basaltVanguardPortrait, false, bgDemonLordThrone, new Color(0.85f, 0.7f, 0.4f)
                    ));
                    break;

                case StorySequenceId.Stage3TitanIntro:
                    lines.Add(new DialogueLine(
                        "MC / Demon Lord",
                        "The Radiant Synod's heavy crusade line is assembled. First, let us tear open the [Abyssal Rift] to anchor our sanctuary conduit!",
                        demonLordPortrait, true, null, new Color(0.95f, 0.4f, 0.4f)
                    ));
                    lines.Add(new DialogueLine(
                        "Basalt Vanguard",
                        "The dimensional gateway is ready, Sire! Deploy your command presence onto the battlefield!",
                        basaltVanguardPortrait, false, null, new Color(0.85f, 0.7f, 0.4f)
                    ));
                    lines.Add(new DialogueLine(
                        "MC / Demon Lord",
                        "Once deployed, siphon the primordial energy from the scorched earth to forge an Elemental Core, then summon the Magma Dragon Titan to incinerate their vanguard!",
                        demonLordPortrait, true, null, new Color(0.95f, 0.4f, 0.4f)
                    ));
                    lines.Add(new DialogueLine(
                        "Basalt Vanguard",
                        "Remember, Sire! Titans devour active elemental lands to fuel their apocalyptic power. Once summoned, have the Magma Dragon siphon the burning earth to unleash its Magma Cataclysm!",
                        basaltVanguardPortrait, false, null, new Color(0.85f, 0.7f, 0.4f)
                    ));
                    break;

                case StorySequenceId.Stage3Victory:
                    lines.Add(new DialogueLine(
                        "Basalt Vanguard",
                        "The holy crusade vanguard has been obliterated! The Magma Dragon's cataclysm shattered their divine barriers into ash!",
                        basaltVanguardPortrait, false, bgDemonLordThrone, new Color(0.85f, 0.7f, 0.4f)
                    ));
                    lines.Add(new DialogueLine(
                        "MC / Demon Lord",
                        "And with their defeat, our sanctuary gateway is secured.",
                        demonLordPortrait, true, bgDemonLordThrone, new Color(0.95f, 0.4f, 0.4f)
                    ));
                    lines.Add(new DialogueLine(
                        "System (Primordial Altar)",
                        "[ PRIMORDIAL RESONANCE DETECTED: CITADEL DEFENSE TRIUMPHANT ]",
                        null, true, bgDemonLordThrone, new Color(1f, 0.85f, 0.25f)
                    ));
                    lines.Add(new DialogueLine(
                        "System (Primordial Altar)",
                        "[ REWARD ACQUIRED: PRIMORDIAL TITAN CORE x1 ]",
                        null, true, bgDemonLordThrone, new Color(0.3f, 0.9f, 1f)
                    ));
                    lines.Add(new DialogueLine(
                        "Basalt Vanguard",
                        "A Primordial Titan Core! With this ancient relic, our Citadel Sanctuary can now sustain permanent daily expedition gateways across the Ashen Verge!",
                        basaltVanguardPortrait, false, bgDemonLordThrone, new Color(0.85f, 0.7f, 0.4f)
                    ));
                    lines.Add(new DialogueLine(
                        "MC / Demon Lord",
                        "Our survival begins today. Let us return to the Citadel Hub and forge our campaign to liberate this world!",
                        demonLordPortrait, true, bgDemonLordThrone, new Color(0.95f, 0.4f, 0.4f)
                    ));
                    break;
            }

            return lines;
        }

        #endregion
    }
}
