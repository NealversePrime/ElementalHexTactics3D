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
        RealWorldPrologue,      // MC at midnight desk playing Holyland Evolve
        HolyRaidIntro,          // Battle 1 Paladin taunting Demon Lord
        HolyRaidVictoryGlitch,  // Post-battle glitch & Developer/God transmigration
        DemonAwakeningIntro,    // Battle 2 Demon Lord wakes up with Basalt Vanguard
        Tutorial2Victory        // Victory awarding 1st Primordial Titan Core
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

            EnsureUIHierarchy();
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
                else if (backgroundArtImage.sprite == null)
                {
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
                // System or anonymous narration: dim both portraits
                if (portraitLeftGroup != null) SetGroupAlpha(portraitLeftGroup, 0.35f);
                if (portraitRightGroup != null) SetGroupAlpha(portraitRightGroup, 0.35f);
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
                        "Jam 23:45... Akhirnya kelar juga lembur kantor yang gila-gilaan hari ini.",
                        mcSilhouetteSprite, true, bgMcRoom, new Color(0.4f, 0.85f, 1f)
                    ));
                    lines.Add(new DialogueLine(
                        "MC (Gamer / Salaryman)",
                        "Tinggal raid boss terakhir di game 'Holyland Evolve' ini sebelum tidur. Sebagai solo ranker, malam ini Demon Lord terakhir harus rata!",
                        mcSilhouetteSprite, true, bgMcRoom, new Color(0.4f, 0.85f, 1f)
                    ));
                    lines.Add(new DialogueLine(
                        "System (Holyland Evolve)",
                        "[ PERINGATAN: ANDA MEMASUKI SINGGASANA KEGELAPAN JURANG - PASUKAN SUCI DIKERAHKAN ]",
                        null, true, bgCathedral, new Color(1f, 0.88f, 0.25f)
                    ));
                    lines.Add(new DialogueLine(
                        "MC (Gamer / Salaryman)",
                        "Bagus. Paladin andalanku sudah siap. Saatnya basmi iblis itu dan tamatkan game ini!",
                        mcSilhouetteSprite, true, bgMcRoom, new Color(0.4f, 0.85f, 1f)
                    ));
                    break;

                case StorySequenceId.HolyRaidIntro:
                    lines.Add(new DialogueLine(
                        "Paladin (Chosen Hero)",
                        "Iblis terkutuk! Singgasana kegelapanmu berakhir malam ini demi kemuliaan Holyland!",
                        paladinPortrait, true, null, new Color(1f, 0.85f, 0.25f)
                    ));
                    lines.Add(new DialogueLine(
                        "Demon Lord",
                        "...Kalian menyebut invasi dan pembantaian ini sebagai 'kemuliaan'? Dunia suci kalian dibangun di atas kepalsuan...",
                        demonLordPortrait, false, null, new Color(0.95f, 0.4f, 0.4f)
                    ));
                    lines.Add(new DialogueLine(
                        "Paladin (Chosen Hero)",
                        "Cukup bicaramu! Pasukan Suci, gerak maju dan bersihkan lantai ini!",
                        paladinPortrait, true, null, new Color(1f, 0.85f, 0.25f)
                    ));
                    break;

                case StorySequenceId.HolyRaidVictoryGlitch:
                    lines.Add(new DialogueLine(
                        "Paladin (Chosen Hero)",
                        "Kemenangan mutlak bagi Holyland! Kegelapan telah dimusnahkan!",
                        paladinPortrait, true, null, new Color(1f, 0.85f, 0.25f)
                    ));
                    lines.Add(new DialogueLine(
                        "MC (Dunia Nyata)",
                        "Huft... akhirnya beres juga! Raid solo berhasil dibabat habis... Tapi kok kepalaku pusing banget ya...?",
                        mcSilhouetteSprite, true, bgMcRoom, new Color(0.4f, 0.85f, 1f)
                    ));
                    lines.Add(new DialogueLine(
                        "MC (Dunia Nyata)",
                        "Eh...? Dadaku sesak... pandanganku buram... Tunggu sebentar... aku cuma butuh tidur...",
                        mcSilhouetteSprite, true, bgMcRoom, new Color(0.4f, 0.85f, 1f), true
                    ));
                    lines.Add(new DialogueLine(
                        "The Architect (Developer God)",
                        "[ PENCAPAIAN TERKONFIRMASI: 100% SOLO CLEAR - 'HOLYLAND EVOLVE' ]",
                        null, true, null, new Color(0.2f, 0.95f, 0.6f), true
                    ));
                    lines.Add(new DialogueLine(
                        "The Architect (Developer God)",
                        "Kamu menepuk dada atas 'keadilan suci' yang kamu banggakan... Tapi pernahkah kamu mendengar rintihan mereka yang kamu sebut 'monster'?",
                        null, true, null, new Color(0.2f, 0.95f, 0.6f)
                    ));
                    lines.Add(new DialogueLine(
                        "The Architect (Developer God)",
                        "Selamat, Pemain #1. Kamu memenuhi kualifikasi. Sekarang... mari kita lihat jika papan catur ini dibalik.",
                        null, true, null, new Color(0.2f, 0.95f, 0.6f), true
                    ));
                    lines.Add(new DialogueLine(
                        "MC (Dunia Nyata)",
                        "T-tunggu! Apa ini?! Monitornya menyerap tanganku... Tubuhku ditarik masuk... WOOOOAAAHHH!!",
                        mcSilhouetteSprite, true, bgMcRoom, new Color(0.95f, 0.3f, 0.3f), true
                    ));
                    break;

                case StorySequenceId.DemonAwakeningIntro:
                    lines.Add(new DialogueLine(
                        "MC / Demon Lord",
                        "Ugh... kepalaku... Dimana ini? Tanganku... cakar hitam berduri? Jubah kegelapan ini...?!",
                        demonLordPortrait, true, bgDemonLordThrone, new Color(0.95f, 0.4f, 0.4f)
                    ));
                    lines.Add(new DialogueLine(
                        "Basalt Vanguard",
                        "Yang Mulia! Syukurlah Anda sadar! Gerbang luar kita jebol oleh gempuran Ksatria Suci!",
                        basaltVanguardPortrait, false, bgDemonLordThrone, new Color(0.85f, 0.7f, 0.4f)
                    ));
                    lines.Add(new DialogueLine(
                        "MC / Demon Lord",
                        "(Tunggu sebentar... Aku bereinkarnasi jadi Demon Lord yang barusan kubunuh?! Dan benteng ini... sedang diserbu oleh pasukan suciku yang dulu?!)",
                        demonLordPortrait, true, bgDemonLordThrone, new Color(0.95f, 0.4f, 0.4f)
                    ));
                    lines.Add(new DialogueLine(
                        "Basalt Vanguard",
                        "Kami tidak akan membiarkan mereka menyentuh Yang Mulia! Perintahkan kami!",
                        basaltVanguardPortrait, false, bgDemonLordThrone, new Color(0.85f, 0.7f, 0.4f)
                    ));
                    lines.Add(new DialogueLine(
                        "MC / Demon Lord",
                        "Tenang. Aku tahu persis formasi dan kelemahan mereka. Saatnya kita ajari mereka apa itu taktik sejati!",
                        demonLordPortrait, true, bgDemonLordThrone, new Color(0.95f, 0.4f, 0.4f)
                    ));
                    break;

                case StorySequenceId.Tutorial2Victory:
                    lines.Add(new DialogueLine(
                        "Basalt Vanguard",
                        "Pasukan Suci terpukul mundur! Yang Mulia... Anda benar-benar menyelamatkan kami semua!",
                        basaltVanguardPortrait, false, bgDemonLordThrone, new Color(0.85f, 0.7f, 0.4f)
                    ));
                    lines.Add(new DialogueLine(
                        "MC / Demon Lord",
                        "Ini baru gelombang pengintai. Pasukan utama Kekaisaran Suci akan datang lebih besar dalam beberapa pekan.",
                        demonLordPortrait, true, bgDemonLordThrone, new Color(0.95f, 0.4f, 0.4f)
                    ));
                    lines.Add(new DialogueLine(
                        "System (Primordial Altar)",
                        "[ ALTAIR PURBA MERESPON KEMENANGAN TAKTIS ANDA ]",
                        null, true, bgDemonLordThrone, new Color(1f, 0.85f, 0.25f)
                    ));
                    lines.Add(new DialogueLine(
                        "System (Primordial Altar)",
                        "[ HADIAH DIPEROLEH: PRIMORDIAL TITAN CORE x1 ]",
                        null, true, bgDemonLordThrone, new Color(0.3f, 0.9f, 1f)
                    ));
                    lines.Add(new DialogueLine(
                        "Basalt Vanguard",
                        "Inti Titan Purba?! Dengan ini kita bisa membangkitkan salah satu dari 4 Dewa Titan Kuno di Citadel Sanctuary!",
                        basaltVanguardPortrait, false, bgDemonLordThrone, new Color(0.85f, 0.7f, 0.4f)
                    ));
                    lines.Add(new DialogueLine(
                        "MC / Demon Lord",
                        "Bagus. Bawa aku ke Citadel Hub. Saatnya kita bangun kembali santuari ini dan bangkitkan Titan pertama kita!",
                        demonLordPortrait, true, bgDemonLordThrone, new Color(0.95f, 0.4f, 0.4f)
                    ));
                    break;
            }

            return lines;
        }

        #endregion
    }
}
