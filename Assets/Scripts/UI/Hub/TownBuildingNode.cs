using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ElementalHexTactics3D.UI.Hub
{
    public enum HubFacilityType
    {
        DemonCastle,
        EmancipationForge,
        AbyssalPortal,
        MonsterBarracks,
        ManaMine,
        AncientDeityShrine
    }

    /// <summary>
    /// Interactive building hotspot on the Citadel Town Hub screen.
    /// Handles pointer hover highlight, scale bounce, audio stingers,
    /// dynamic tooltip banner triggering, and facility modal opening.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class TownBuildingNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Facility Configuration")]
        [SerializeField] private HubFacilityType facilityType;
        [SerializeField] private string facilityTitle = "Facility Name";
        [TextArea(2, 4)]
        [SerializeField] private string facilityLore = "Facility description...";
        [SerializeField] private string facilityStatus = "Active - Ready";

        [Header("Visual Feedback")]
        [SerializeField] private Graphic targetGraphic;
        [SerializeField] private Transform scaleRoot;
        [SerializeField] private Transform groundShadow;
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0f);
        [SerializeField] private Color hoverColor = new Color(1.18f, 1.12f, 0.9f, 0.35f); // Subtle highlight
        [SerializeField] private float hoverScaleFactor = 1.04f;

        [Header("Audio")]
        [SerializeField] private AudioClip hoverSfx;
        [SerializeField] private AudioClip clickSfx;

        private Vector3 originalScale = Vector3.one;
        private Vector3 originalShadowScale = Vector3.one;
        private Coroutine scaleCoroutine;
        private bool isHovered = false;

        public HubFacilityType FacilityType => facilityType;
        public string FacilityTitle => facilityTitle;
        public string FacilityLore => facilityLore;
        public string FacilityStatus => facilityStatus;

        private void Awake()
        {
            if (targetGraphic == null) targetGraphic = GetComponent<Graphic>();
            if (scaleRoot == null) scaleRoot = transform;
            originalScale = scaleRoot.localScale;
            if (groundShadow != null) originalShadowScale = groundShadow.localScale;
            normalColor = new Color(1f, 1f, 1f, 0f);

            // Enable per-pixel alpha raycasting so the clickable / hoverable area
            // matches the exact silhouette of the building artwork (ignoring transparent margins)
            Image img = GetComponent<Image>();
            if (img != null && img.sprite != null)
            {
                img.alphaHitTestMinimumThreshold = 0.15f;
            }
        }

        private void OnEnable()
        {
            ResetVisualState();
        }

        private void OnDisable()
        {
            ResetVisualState();
        }

        private bool CanTintTargetGraphic()
        {
            // Never tint the duplicate cutout sprite on top of Background_Landscape (full.png).
            // Drawing a semi-transparent scaled duplicate over the painted background causes
            // a double-vision (diplopia/ghosting) blur that triggers eye strain and headaches.
            return false;
        }

        private void Update()
        {
            if (targetGraphic != null && targetGraphic.color.a > 0f)
            {
                targetGraphic.color = new Color(1f, 1f, 1f, 0f);
            }

            var tut = Tutorial.TutorialScenarioManager.Instance;
            Transform badgeTrans = transform.Find("FacilityTagBadge");
            Image badgeImg = badgeTrans != null ? badgeTrans.GetComponent<Image>() : null;
            Outline badgeOutline = badgeTrans != null ? badgeTrans.GetComponent<Outline>() : null;
            Text badgeTxt = badgeTrans != null ? badgeTrans.Find("Text_Badge")?.GetComponent<Text>() : null;

            // Tutorial Highlight Pulse: When player is prompted to click Abyssal Rift, highlight the badge cleanly
            if (facilityType == HubFacilityType.AbyssalPortal &&
                tut != null && tut.IsHubAwakeningActive && tut.CurrentStep == Tutorial.TutorialStep.Hub_ClickAbyssalRift)
            {
                float pulse = (Mathf.Sin(Time.unscaledTime * 3.0f) + 1f) * 0.5f;
                if (badgeTxt != null) badgeTxt.text = "[!] CLICK TO ENTER BATTLE";
                if (badgeImg != null) badgeImg.color = Color.Lerp(new Color(0.08f, 0.18f, 0.12f, 0.96f), new Color(0.18f, 0.34f, 0.16f, 0.98f), pulse);
                if (badgeOutline != null) badgeOutline.effectColor = Color.Lerp(new Color(0.4f, 0.95f, 0.55f, 0.9f), new Color(1f, 0.92f, 0.35f, 1f), pulse);
                if (badgeTrans != null) badgeTrans.localScale = Vector3.one;
            }
            // Tutorial Highlight Pulse: When player is prompted to awaken a Titan, highlight the Primordial Altar badge cleanly
            else if (facilityType == HubFacilityType.AncientDeityShrine &&
                     tut != null && tut.IsHubAwakeningActive && tut.CurrentStep == Tutorial.TutorialStep.Hub_ClickPrimordialStatue)
            {
                float pulse = (Mathf.Sin(Time.unscaledTime * 3.0f) + 1f) * 0.5f;
                if (badgeTxt != null) badgeTxt.text = "[!] AWAKEN TITAN HERE";
                if (badgeImg != null) badgeImg.color = Color.Lerp(new Color(0.22f, 0.08f, 0.04f, 0.96f), new Color(0.40f, 0.16f, 0.05f, 0.98f), pulse);
                if (badgeOutline != null) badgeOutline.effectColor = Color.Lerp(new Color(1f, 0.55f, 0.2f, 0.9f), new Color(1f, 0.9f, 0.35f, 1f), pulse);
                if (badgeTrans != null) badgeTrans.localScale = Vector3.one;
            }
            else if (badgeTrans != null)
            {
                bool isActiveFacility = (facilityType == HubFacilityType.AbyssalPortal || facilityType == HubFacilityType.AncientDeityShrine);
                if (badgeTxt != null)
                {
                    if (facilityType == HubFacilityType.AbyssalPortal) badgeTxt.text = "ABYSSAL RIFT [ENTER BATTLE]";
                    else if (facilityType == HubFacilityType.AncientDeityShrine) badgeTxt.text = "PRIMORDIAL ALTAR";
                    else badgeTxt.text = "UNDER CONSTRUCTION";
                }
                if (badgeImg != null)
                {
                    badgeImg.color = isHovered
                        ? (isActiveFacility ? new Color(0.14f, 0.30f, 0.22f, 0.98f) : new Color(0.16f, 0.14f, 0.12f, 0.96f))
                        : (isActiveFacility ? new Color(0.06f, 0.14f, 0.12f, 0.92f) : new Color(0.07f, 0.07f, 0.09f, 0.88f));
                }
                if (badgeOutline != null)
                {
                    badgeOutline.effectColor = isHovered
                        ? (isActiveFacility ? new Color(0.55f, 1.0f, 0.78f, 1.0f) : new Color(0.95f, 0.78f, 0.38f, 0.95f))
                        : (isActiveFacility ? new Color(0.35f, 0.90f, 0.65f, 0.85f) : new Color(0.55f, 0.45f, 0.30f, 0.55f));
                }
                if (badgeTrans != null)
                {
                    badgeTrans.localScale = Vector3.one;
                }
            }
        }

        public void Setup(HubFacilityType type, string title, string lore, string status, Graphic graphic, Transform shadow = null, Color? normal = null, Color? hover = null, float scale = 1.03f)
        {
            facilityType = type;
            facilityTitle = title;
            facilityLore = lore;
            facilityStatus = status;
            targetGraphic = graphic;
            groundShadow = shadow;
            if (normal.HasValue) normalColor = normal.Value;
            if (hover.HasValue) hoverColor = hover.Value;
            hoverScaleFactor = scale;
            if (scaleRoot == null) scaleRoot = transform;
            originalScale = scaleRoot.localScale;
            if (groundShadow != null) originalShadowScale = groundShadow.localScale;
            ResetVisualState();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Block hover interactions while story cutscene / dialogue is active
            if (StoryDialogueUI.Instance != null && StoryDialogueUI.Instance.IsPlayingDialogue) return;
            if (Tutorial.TutorialScenarioManager.Instance != null &&
                Tutorial.TutorialScenarioManager.Instance.IsHubAwakeningActive &&
                (Tutorial.TutorialScenarioManager.Instance.CurrentStep == Tutorial.TutorialStep.Hub_CitadelAwakeningDialogue ||
                 Tutorial.TutorialScenarioManager.Instance.CurrentStep == Tutorial.TutorialStep.Hub_TitanCrisisDialogue)) return;

            isHovered = true;

            // Keep targetGraphic invisible so it never creates a ghosted double-image over full.png
            if (targetGraphic != null)
            {
                targetGraphic.color = new Color(1f, 1f, 1f, 0f);
            }

            // Play hover sound
            PlayAudio(hoverSfx, 0.85f, 1.1f);

            // 4. Trigger top header tooltip banner
            if (TownHubManager.Instance != null)
            {
                if (facilityType == HubFacilityType.DemonCastle ||
                    facilityType == HubFacilityType.EmancipationForge ||
                    facilityType == HubFacilityType.MonsterBarracks ||
                    facilityType == HubFacilityType.ManaMine)
                {
                    TownHubManager.Instance.ShowTooltip(
                        "FACILITY UNDER CONSTRUCTION",
                        "This Citadel facility is currently being rebuilt and does not have an active effect in this prototype demo.",
                        "<color=#FFB74D> Under Construction - Prototype Demo</color>"
                    );
                }
                else if (facilityType == HubFacilityType.AncientDeityShrine)
                {
                    var tut = Tutorial.TutorialScenarioManager.Instance;
                    bool isTitanTutorial = tut != null && tut.IsHubAwakeningActive &&
                                           (tut.CurrentStep == Tutorial.TutorialStep.Hub_ClickPrimordialStatue ||
                                            tut.CurrentStep == Tutorial.TutorialStep.Hub_AwakenMagmaDragon);
                    if (isTitanTutorial)
                    {
                        TownHubManager.Instance.ShowTooltip(
                            "PRIMORDIAL ALTAR [AWAKEN TITAN]",
                            "The ancient cliffside idol resonates with draconic flame! Click to awaken the Magma Dragon Titan!",
                            "<color=#FF7043> Primordial Resonance - Click to Awaken</color>"
                        );
                    }
                    else
                    {
                        TownHubManager.Instance.ShowTooltip(
                            "PRIMORDIAL ALTAR",
                            "Magma Dragon Titan has been awakened. Further primordial rituals are under construction in this demo.",
                            "<color=#FFB74D> Further Rituals Under Construction</color>"
                        );
                    }
                }
                else
                {
                    TownHubManager.Instance.ShowTooltip(facilityTitle, facilityLore, facilityStatus);
                }
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            ResetVisualState();

            if (TownHubManager.Instance != null)
            {
                TownHubManager.Instance.HideTooltip();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Block click interactions while story cutscene / dialogue is active
            if (StoryDialogueUI.Instance != null && StoryDialogueUI.Instance.IsPlayingDialogue) return;
            if (Tutorial.TutorialScenarioManager.Instance != null &&
                Tutorial.TutorialScenarioManager.Instance.IsHubAwakeningActive &&
                (Tutorial.TutorialScenarioManager.Instance.CurrentStep == Tutorial.TutorialStep.Hub_CitadelAwakeningDialogue ||
                 Tutorial.TutorialScenarioManager.Instance.CurrentStep == Tutorial.TutorialStep.Hub_TitanCrisisDialogue)) return;

            if (eventData.button != PointerEventData.InputButton.Left) return;

            PlayAudio(clickSfx, 1.0f, 1.0f);

            if (TownHubManager.Instance != null)
            {
                TownHubManager.Instance.OnBuildingClicked(this);
            }
        }

        private void ResetVisualState()
        {
            if (targetGraphic != null)
            {
                targetGraphic.color = new Color(1f, 1f, 1f, 0f);
            }

            if (scaleCoroutine != null)
            {
                StopCoroutine(scaleCoroutine);
                scaleCoroutine = null;
            }

            if (scaleRoot != null)
            {
                scaleRoot.localScale = originalScale;
            }

            if (groundShadow != null)
            {
                groundShadow.localScale = originalShadowScale;
            }
        }

        private void StartScaleTween(Vector3 targetScale, float duration)
        {
            if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
            scaleCoroutine = StartCoroutine(AnimateScale(targetScale, duration));
        }

        private IEnumerator AnimateScale(Vector3 targetScale, float duration)
        {
            if (scaleRoot == null) yield break;

            Vector3 startScale = scaleRoot.localScale;
            Vector3 startShadowScale = groundShadow != null ? groundShadow.localScale : Vector3.one;
            Vector3 targetShadowScale = originalShadowScale * (1f + (hoverScaleFactor - 1f) * 0.5f);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Smooth ease out
                t = Mathf.Sin(t * Mathf.PI * 0.5f);

                scaleRoot.localScale = Vector3.Lerp(startScale, targetScale, t);
                if (groundShadow != null)
                {
                    groundShadow.localScale = Vector3.Lerp(startShadowScale, targetShadowScale, t);
                }

                yield return null;
            }

            scaleRoot.localScale = targetScale;
            if (groundShadow != null) groundShadow.localScale = targetShadowScale;
            scaleCoroutine = null;
        }

        private void PlayAudio(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : Vector3.zero, volume);
            }
        }
    }
}

