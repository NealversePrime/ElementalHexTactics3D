using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ElementalHexTactics3D.Combat;

namespace ElementalHexTactics3D.UI.Hub
{
    /// <summary>
    /// Master controller for the Citadel Town Hub ("Edge of the World" sanctuary).
    /// Manages domain resources, dynamic tooltip banners, facility modals,
    /// and the expedition transition into the 3D Hex Tactical battlefield.
    /// </summary>
    public class TownHubManager : MonoBehaviour
    {
        public static TownHubManager Instance { get; private set; }

        [Header("Domain Resources")]
        [SerializeField] private int manaCrystals = 450;
        [SerializeField] private int soulEmbers = 180;
        [SerializeField] private int freedOutcasts = 16;

        [Header("Resource UI Displays")]
        [SerializeField] private Text txtManaCrystals;
        [SerializeField] private Text txtSoulEmbers;
        [SerializeField] private Text txtFreedOutcasts;

        [Header("Tooltip Banner")]
        [SerializeField] private GameObject tooltipBannerPanel;
        [SerializeField] private Text txtTooltipTitle;
        [SerializeField] private Text txtTooltipDesc;
        [SerializeField] private Text txtTooltipStatus;
        [SerializeField] private CanvasGroup tooltipCanvasGroup;

        [Header("Facility Modals")]
        [SerializeField] private GameObject modalCastle;
        [SerializeField] private GameObject modalForge;
        [SerializeField] private GameObject modalBarracks;
        [SerializeField] private GameObject modalMine;
        [SerializeField] private GameObject modalShrine;

        [Header("Interactive Buttons")]
        [SerializeField] private Button btnReturnTitle;

        [Header("Audio")]
        [SerializeField] private AudioClip sfxOpenModal;
        [SerializeField] private AudioClip sfxCloseModal;
        [SerializeField] private AudioClip sfxActionSuccess;
        [SerializeField] private AudioClip sfxPortalEmbark;

        private GameObject activeModal = null;
        private Coroutine tooltipFadeCoroutine;

        public int ManaCrystals => manaCrystals;
        public int SoulEmbers => soulEmbers;
        public int FreedOutcasts => freedOutcasts;

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

            UpdateResourceDisplays();
            HideTooltipImmediate();
            CloseAllModals();
        }

        private void Start()
        {
            if (btnReturnTitle != null)
            {
                btnReturnTitle.onClick.AddListener(OnReturnTitleClicked);
            }
        }

        public void UpdateResourceDisplays()
        {
            if (txtManaCrystals != null) txtManaCrystals.text = $"💎 {manaCrystals} Mana";
            if (txtSoulEmbers != null) txtSoulEmbers.text = $"🔥 {soulEmbers} Embers";
            if (txtFreedOutcasts != null) txtFreedOutcasts.text = $"🛡️ {freedOutcasts} Outcasts";
        }

        public void AddManaCrystals(int amount)
        {
            manaCrystals = Mathf.Max(0, manaCrystals + amount);
            UpdateResourceDisplays();
        }

        public void AddSoulEmbers(int amount)
        {
            soulEmbers = Mathf.Max(0, soulEmbers + amount);
            UpdateResourceDisplays();
        }

        public void AddFreedOutcasts(int amount)
        {
            freedOutcasts = Mathf.Max(0, freedOutcasts + amount);
            UpdateResourceDisplays();
        }

        #region Tooltip Banner

        public void ShowTooltip(string title, string description, string status)
        {
            if (activeModal != null) return; // Don't show building tooltips while modal is open

            if (txtTooltipTitle != null) txtTooltipTitle.text = title;
            if (txtTooltipDesc != null) txtTooltipDesc.text = description;
            if (txtTooltipStatus != null) txtTooltipStatus.text = status;

            if (tooltipBannerPanel != null)
            {
                tooltipBannerPanel.SetActive(true);
                FadeTooltip(1.0f, 0.15f);
            }
        }

        public void HideTooltip()
        {
            FadeTooltip(0.0f, 0.12f);
        }

        private void HideTooltipImmediate()
        {
            if (tooltipCanvasGroup != null) tooltipCanvasGroup.alpha = 0f;
            if (tooltipBannerPanel != null) tooltipBannerPanel.SetActive(false);
        }

        private void FadeTooltip(float targetAlpha, float duration)
        {
            if (tooltipFadeCoroutine != null) StopCoroutine(tooltipFadeCoroutine);
            tooltipFadeCoroutine = StartCoroutine(AnimateTooltipFade(targetAlpha, duration));
        }

        private IEnumerator AnimateTooltipFade(float targetAlpha, float duration)
        {
            if (tooltipCanvasGroup == null) yield break;

            float startAlpha = tooltipCanvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                tooltipCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }

            tooltipCanvasGroup.alpha = targetAlpha;
            if (targetAlpha <= 0.01f && tooltipBannerPanel != null)
            {
                tooltipBannerPanel.SetActive(false);
            }
            tooltipFadeCoroutine = null;
        }

        #endregion

        #region Building Interactions & Modals

        public void OnBuildingClicked(TownBuildingNode building)
        {
            HideTooltipImmediate();

            switch (building.FacilityType)
            {
                case HubFacilityType.DemonCastle:
                    OpenModal(modalCastle);
                    break;
                case HubFacilityType.EmancipationForge:
                    OpenModal(modalForge);
                    break;
                case HubFacilityType.AbyssalPortal:
                    EmbarkToExpedition();
                    break;
                case HubFacilityType.MonsterBarracks:
                    OpenModal(modalBarracks);
                    break;
                case HubFacilityType.ManaMine:
                    OpenModal(modalMine);
                    break;
                case HubFacilityType.AncientDeityShrine:
                    OpenModal(modalShrine);
                    break;
            }
        }

        public void OpenModal(GameObject modal)
        {
            if (modal == null) return;

            CloseAllModals();
            activeModal = modal;
            activeModal.SetActive(true);
            PlaySound(sfxOpenModal, 0.9f);
        }

        public void CloseActiveModal()
        {
            if (activeModal != null)
            {
                activeModal.SetActive(false);
                activeModal = null;
                PlaySound(sfxCloseModal, 0.8f);
            }
        }

        public void CloseAllModals()
        {
            if (modalCastle != null) modalCastle.SetActive(false);
            if (modalForge != null) modalForge.SetActive(false);
            if (modalBarracks != null) modalBarracks.SetActive(false);
            if (modalMine != null) modalMine.SetActive(false);
            if (modalShrine != null) modalShrine.SetActive(false);
            activeModal = null;
        }

        #endregion

        #region Facility Actions (Interactive Gameplay Buttons)

        public void ActionAssignMineWorker()
        {
            if (freedOutcasts >= 1)
            {
                AddFreedOutcasts(-1);
                AddManaCrystals(50);
                PlaySound(sfxActionSuccess, 1.0f);
                ShowNoticeBanner("⛏️ Laborer Assigned", "Assigned 1 Kobold Miner. Gained +50 Mana Crystals!");
            }
            else
            {
                ShowNoticeBanner("⚠️ Insufficient Outcasts", "Free more demi-humans from Holy Empire in battle!");
            }
        }

        public void ActionShatterCollar()
        {
            if (soulEmbers >= 30)
            {
                AddSoulEmbers(-30);
                AddFreedOutcasts(1);
                PlaySound(sfxActionSuccess, 1.0f);
                ShowNoticeBanner("⚒️ Cursed Collar Shattered", "Dark Elf unbound! +1 Freed Outcast joined the Sanctuary!");
            }
            else
            {
                ShowNoticeBanner("⚠️ Need 30 Soul Embers", "Harvest more embers from holy inquisitors in battle!");
            }
        }

        public void ActionRecruitMinion()
        {
            if (manaCrystals >= 60)
            {
                AddManaCrystals(-60);
                PlaySound(sfxActionSuccess, 1.0f);
                ShowNoticeBanner("🐺 Minion Recruited", "Goblin Skirmisher armed and assigned to Vanguard Roster!");
            }
            else
            {
                ShowNoticeBanner("⚠️ Need 60 Mana Crystals", "Mine more magic crystals or harvest from expeditions!");
            }
        }

        public void ActionUnlockPerk()
        {
            if (soulEmbers >= 50)
            {
                AddSoulEmbers(-50);
                PlaySound(sfxActionSuccess, 1.0f);
                ShowNoticeBanner("👑 Demon Lord Perk Unlocked", "[Territory Domain] Elemental skills cost -1 AP on corrupted hexes!");
            }
            else
            {
                ShowNoticeBanner("⚠️ Need 50 Soul Embers", "Earn more embers by winning tactical incursions!");
            }
        }

        public void ActionPerformGachaSummon()
        {
            if (soulEmbers >= 80)
            {
                AddSoulEmbers(-80);
                PlaySound(sfxActionSuccess, 1.0f);
                ShowNoticeBanner("🔮 Ancient Deity Summoned", "✦ S-RANK BEAST: OBSIDIAN GOLEM ✦ awoken from the abyss!");
            }
            else
            {
                ShowNoticeBanner("⚠️ Need 80 Soul Embers", "Gather soul embers from defeated high-tier angels!");
            }
        }

        private void ShowNoticeBanner(string title, string msg)
        {
            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.ShowBanner(title, msg, 1.6f, new Color(1f, 0.85f, 0.35f));
            }
            else
            {
                Debug.Log($"<color=#FFD54F><b>[{title}]</b></color> {msg}");
            }
        }

        #endregion

        #region Portal Embarkation

        public void EmbarkToExpedition()
        {
            PlaySound(sfxPortalEmbark, 1.0f);
            HideTooltipImmediate();
            CloseAllModals();

            if (TitleMenuCanvasUI.Instance != null)
            {
                TitleMenuCanvasUI.Instance.EnterHexBattlefield();
            }
        }

        private void OnReturnTitleClicked()
        {
            CloseAllModals();
            HideTooltipImmediate();

            if (TitleMenuCanvasUI.Instance != null)
            {
                TitleMenuCanvasUI.Instance.ShowTitleScreen();
            }
        }

        #endregion

        private void PlaySound(AudioClip clip, float volume)
        {
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : Vector3.zero, volume);
            }
        }
    }
}
