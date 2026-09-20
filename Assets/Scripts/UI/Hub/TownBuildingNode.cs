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
        [SerializeField] private string facilityStatus = "Active • Ready";

        [Header("Visual Feedback")]
        [SerializeField] private Graphic targetGraphic;
        [SerializeField] private Transform scaleRoot;
        [SerializeField] private Transform groundShadow;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = new Color(1.2f, 1.15f, 0.95f, 1.0f); // Bright subtle highlight
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
        }

        private void OnEnable()
        {
            ResetVisualState();
        }

        private void OnDisable()
        {
            ResetVisualState();
        }

        public void Setup(HubFacilityType type, string title, string lore, string status, Graphic graphic, Transform shadow = null)
        {
            facilityType = type;
            facilityTitle = title;
            facilityLore = lore;
            facilityStatus = status;
            targetGraphic = graphic;
            groundShadow = shadow;
            if (scaleRoot == null) scaleRoot = transform;
            originalScale = scaleRoot.localScale;
            if (groundShadow != null) originalShadowScale = groundShadow.localScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;

            // 1. Color highlight
            if (targetGraphic != null)
            {
                targetGraphic.color = hoverColor;
            }

            // 2. Smooth scale bounce
            StartScaleTween(originalScale * hoverScaleFactor, 0.12f);

            // 3. Play hover sound
            PlayAudio(hoverSfx, 0.85f, 1.1f);

            // 4. Trigger top header tooltip banner
            if (TownHubManager.Instance != null)
            {
                TownHubManager.Instance.ShowTooltip(facilityTitle, facilityLore, facilityStatus);
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
                targetGraphic.color = normalColor;
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
