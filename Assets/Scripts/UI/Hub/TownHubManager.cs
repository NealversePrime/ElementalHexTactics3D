using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ElementalHexTactics3D.Combat;
using ElementalHexTactics3D.Campaign;

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
        [SerializeField] private Text txtDomainTitle;
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
        [SerializeField] private Button btnResetCampaign;

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
        public int Food => CampaignManager.Instance != null ? CampaignManager.Instance.Food : 50;

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

            // Self-healing: purge any legacy shadow/light containers from earlier iterations
            PurgeLegacyContainers();
            EnsureFullBackgroundSprite();
            SnapBuildingNodes();

            UpdateResourceDisplays();
            HideTooltipImmediate();
            WireFacilityModals();
            CloseAllModals();
        }

        private void OnEnable()
        {
            PurgeLegacyContainers();
            EnsureFullBackgroundSprite();
            SnapBuildingNodes();
            WireFacilityModals();
            UpdateResourceDisplays();
            Combat.SoundManager3D.Instance?.PlayHubBGM();
        }

        private void PurgeLegacyContainers()
        {
            Transform oldShadows = transform.Find("Container_GroundShadows");
            if (oldShadows != null) DestroyImmediate(oldShadows.gameObject);
            Transform oldSpills = transform.Find("Container_LightSpills");
            if (oldSpills != null) DestroyImmediate(oldSpills.gameObject);
        }

        private void EnsureFullBackgroundSprite()
        {
            Transform bg = transform.Find("Background_Landscape");
            if (bg != null)
            {
                Image bgImg = bg.GetComponent<Image>();
                if (bgImg != null)
                {
                    if (bgImg.sprite == null || bgImg.sprite.name != "full")
                    {
                        string path = System.IO.Path.Combine(Application.dataPath, "Sprites/Hub/full.png");
                        if (System.IO.File.Exists(path))
                        {
                            byte[] data = System.IO.File.ReadAllBytes(path);
                            Texture2D tex = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
                            if (tex.LoadImage(data))
                            {
                                tex.name = "full";
                                Sprite sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                                sp.name = "full";
                                bgImg.sprite = sp;
                                bgImg.color = Color.white;
                            }
                        }
                    }
                }
            }
        }

        private void SnapBuildingNodes()
        {
            SnapNode("Building_DemonCastle", new Vector2(-753.3f, 122.8f), new Vector2(735.0f, 735.0f));
            SnapNode("Building_EmancipationForge", new Vector2(-395.6f, -72.6f), new Vector2(385.0f, 385.0f));
            SnapNode("Building_AbyssalPortal", new Vector2(-26.5f, -53.5f), new Vector2(427f, 427f));
            SnapNode("Building_MonsterBarracks", new Vector2(313.5f, -55.5f), new Vector2(449f, 449f));
            SnapNode("Building_ManaMineFarm", new Vector2(643.2f, -136.4f), new Vector2(492.5f, 488.8f));
            SnapNode("Building_AncientDeityShrine", new Vector2(786.0f, 174.0f), new Vector2(348f, 348f));
        }

        private void SnapNode(string name, Vector2 pos, Vector2 size)
        {
            Transform t = transform.Find($"Container_Buildings/{name}");
            if (t != null)
            {
                RectTransform rt = t.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = pos;
                    rt.sizeDelta = size;
                }

                Image img = t.GetComponent<Image>();
                if (img != null)
                {
                    img.color = new Color(1f, 1f, 1f, 0f);
                }
            }
        }

        private void Start()
        {
            if (btnReturnTitle != null)
            {
                btnReturnTitle.onClick.RemoveAllListeners();
                btnReturnTitle.onClick.AddListener(OnReturnTitleClicked);
            }

            if (btnResetCampaign == null)
            {
                Transform rBtn = transform.Find("Panel_TopDomainHeader/Btn_ResetRun");
                if (rBtn != null) btnResetCampaign = rBtn.GetComponent<Button>();
            }

            if (btnResetCampaign != null)
            {
                btnResetCampaign.onClick.RemoveAllListeners();
                btnResetCampaign.onClick.AddListener(() => showResetConfirmModal = true);
            }

            WireFacilityModals();
        }

        private void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                if (showResetConfirmModal)
                {
                    showResetConfirmModal = false;
                }
                else if (activeModal != null)
                {
                    CloseActiveModal();
                }
            }
        }

        private void WireFacilityModals()
        {
            WireSingleModal(modalCastle, ActionUnlockPerk);
            WireSingleModal(modalForge, ActionShatterCollar);
            WireSingleModal(modalBarracks, ActionRecruitMinion);
            WireSingleModal(modalMine, ActionAssignMineWorker);
            WireSingleModal(modalShrine, ActionPerformGachaSummon);
        }

        private void WireSingleModal(GameObject modal, UnityEngine.Events.UnityAction action)
        {
            if (modal == null) return;

            // 1. Close Button
            Transform btnCloseTrans = modal.transform.Find("CardFrame/Btn_Close");
            if (btnCloseTrans != null)
            {
                Button btnClose = btnCloseTrans.GetComponent<Button>();
                if (btnClose != null)
                {
                    btnClose.onClick.RemoveAllListeners();
                    btnClose.onClick.AddListener(CloseActiveModal);
                }
            }

            // 2. Action Button
            if (action != null)
            {
                Transform btnActionTrans = modal.transform.Find("CardFrame/Btn_Action");
                if (btnActionTrans != null)
                {
                    Button btnAction = btnActionTrans.GetComponent<Button>();
                    if (btnAction != null)
                    {
                        btnAction.onClick.RemoveAllListeners();
                        btnAction.onClick.AddListener(action);
                    }
                }
            }

            // 3. Backdrop Click to Dismiss
            Transform backdropTrans = modal.transform.Find("Backdrop");
            if (backdropTrans != null)
            {
                Button bdBtn = backdropTrans.GetComponent<Button>();
                if (bdBtn == null) bdBtn = backdropTrans.gameObject.AddComponent<Button>();
                bdBtn.onClick.RemoveAllListeners();
                bdBtn.onClick.AddListener(CloseActiveModal);
            }
        }

        public void UpdateResourceDisplays()
        {
            int day = CampaignManager.Instance != null ? CampaignManager.Instance.CurrentDay : 1;
            int daysUntil = CampaignManager.Instance != null ? CampaignManager.Instance.DaysUntilCrusade : 24;
            int foodStock = CampaignManager.Instance != null ? CampaignManager.Instance.Food : 50;
            string actStr = CampaignManager.Instance != null ? CampaignManager.Instance.ActTitle : "Act I: Survival";

            if (txtDomainTitle == null)
            {
                txtDomainTitle = transform.Find("Panel_TopDomainHeader/Text_DomainTitle")?.GetComponent<Text>();
            }

            if (txtDomainTitle != null)
            {
                string doomColor = (daysUntil <= 3) ? "#FF5252" : (daysUntil <= 7) ? "#FFB74D" : "#81C784";
                string foodColor = (foodStock <= 15) ? "#FF5252" : "#FFA726";
                txtDomainTitle.text = $"☀️ DAY {day} ({actStr})  |  ⏳ CRUSADE: <color={doomColor}>{daysUntil} DAYS</color>  |  🍖 FOOD: <color={foodColor}>{foodStock}</color>";
            }

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

        public void SetResources(int mana, int embers, int outcasts)
        {
            manaCrystals = Mathf.Max(0, mana);
            soulEmbers = Mathf.Max(0, embers);
            freedOutcasts = Mathf.Max(0, outcasts);
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
            // Block facility clicks if story dialogue / cutscene is currently playing
            if (StoryDialogueUI.Instance != null && StoryDialogueUI.Instance.IsPlayingDialogue)
            {
                return;
            }

            HideTooltipImmediate();

            // Tutorial Gating: During Hub Awakening, cutscene must finish first and only Abyssal Portal is permitted!
            if (Tutorial.TutorialScenarioManager.Instance != null && Tutorial.TutorialScenarioManager.Instance.IsHubAwakeningActive)
            {
                if (Tutorial.TutorialScenarioManager.Instance.CurrentStep == Tutorial.TutorialStep.Hub_CitadelAwakeningDialogue)
                {
                    // Cutscene is still playing or initializing, ignore clicks!
                    return;
                }

                if (building.FacilityType == HubFacilityType.AbyssalPortal)
                {
                    if (Tutorial.TutorialScenarioManager.Instance.CurrentStage == Tutorial.TutorialStage.Hub_TitanCrisis)
                    {
                        Tutorial.TutorialScenarioManager.Instance.SetStep(Tutorial.TutorialStep.Hub_SelectStage3MissionCard);
                    }
                    else
                    {
                        Tutorial.TutorialScenarioManager.Instance.SetStep(Tutorial.TutorialStep.Hub_SelectStage2MissionCard);
                    }
                    EmbarkToExpedition();
                    return;
                }
                else
                {
                    PlaySound(sfxCloseModal, 0.8f);
                    ShowNoticeBanner("⚠️ DEFENSE PRIORITY", "Radiant Synod forces are encroaching on the gateway! Enter the Abyssal Portal immediately!");
                    return;
                }
            }

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
            if (ExpeditionPortalModalUI.Instance != null && ExpeditionPortalModalUI.Instance.IsOpen)
            {
                ExpeditionPortalModalUI.Instance.CloseModal();
            }
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
            PlaySound(sfxOpenModal, 0.9f);
            HideTooltipImmediate();
            CloseAllModals();

            if (ExpeditionPortalModalUI.Instance == null)
            {
                gameObject.AddComponent<ExpeditionPortalModalUI>();
            }
            ExpeditionPortalModalUI.Instance.OpenModal();
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

        #region Calendar & Campaign HUD

        private Texture2D hudBgTex;
        private Texture2D solidTex;
        private bool showResetConfirmModal = false;

        private void EnsureHudBgTex()
        {
            if (hudBgTex == null)
            {
                hudBgTex = new Texture2D(1, 1);
                hudBgTex.SetPixel(0, 0, new Color(0.06f, 0.08f, 0.12f, 0.94f));
                hudBgTex.Apply();
            }
            if (solidTex == null)
            {
                solidTex = new Texture2D(1, 1);
                solidTex.SetPixel(0, 0, Color.white);
                solidTex.Apply();
            }
        }

        private void OnGUI()
        {
            // Only render when inside Town Hub and no expedition modal or results modal is open
            if (TitleMenuCanvasUI.Instance == null || !TitleMenuCanvasUI.Instance.IsInTownHub) return;
            if (ExpeditionPortalModalUI.Instance != null && ExpeditionPortalModalUI.Instance.IsOpen) return;
            if (PostBattleResultsUI.Instance != null && PostBattleResultsUI.Instance.IsOpen) return;

            // If modern uGUI Panel_TopDomainHeader is active, suppress legacy IMGUI top bar!
            Transform headerPanel = transform.Find("Panel_TopDomainHeader");
            if (headerPanel != null && headerPanel.gameObject.activeInHierarchy)
            {
                if (showResetConfirmModal)
                {
                    EnsureHudBgTex();
                    DrawResetConfirmModal();
                }
                return;
            }

            EnsureHudBgTex();

            float barW = Mathf.Min(1320f, Screen.width - 40f);
            float barH = 46f;
            float barX = (Screen.width - barW) * 0.5f;
            float barY = 12f;

            Rect barRect = new Rect(barX, barY, barW, barH);

            // Draw Bar Background
            GUI.color = Color.white;
            GUI.DrawTexture(barRect, hudBgTex);

            // Bottom Border
            GUI.color = new Color(0.35f, 0.45f, 0.65f, 0.85f);
            GUI.DrawTexture(new Rect(barRect.x, barRect.y + barRect.height - 2, barRect.width, 2), solidTex);
            GUI.color = Color.white;

            int day = CampaignManager.Instance != null ? CampaignManager.Instance.CurrentDay : 1;
            int daysUntil = CampaignManager.Instance != null ? CampaignManager.Instance.DaysUntilCrusade : 24;
            int foodStock = CampaignManager.Instance != null ? CampaignManager.Instance.Food : 50;
            string actStr = CampaignManager.Instance != null ? CampaignManager.Instance.ActTitle : "Act I: Survival";

            string doomColor = (daysUntil <= 3) ? "#FF5252" : (daysUntil <= 7) ? "#FFB74D" : "#81C784";
            string foodColor = (foodStock <= 15) ? "#FF5252" : "#FFA726";

            GUIStyle hudStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                richText = true
            };
            hudStyle.normal.textColor = Color.white;

            Rect textRect = new Rect(barRect.x + 14f, barRect.y, barRect.width - 230f, barRect.height);
            string info = $"☀️ <b>DAY {day}</b> ({actStr})  |  ⏳ CRUSADE IN: <color={doomColor}><b>{daysUntil} DAYS</b></color>  |  🍖 FOOD: <color={foodColor}><b>{foodStock}</b></color>  |  💎 <b>{manaCrystals}</b>  |  🔥 <b>{soulEmbers}</b>  |  👥 <b>{freedOutcasts}</b>";
            GUI.Label(textRect, info, hudStyle);

            // Auto-Save Indicator
            GUIStyle saveStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 10,
                fontStyle = FontStyle.Italic,
                richText = true
            };
            saveStyle.normal.textColor = new Color(0.6f, 0.85f, 0.6f, 0.9f);
            Rect saveRect = new Rect(barRect.x + barRect.width - 225f, barRect.y, 90f, barRect.height);
            GUI.Label(saveRect, "💾 Auto-Saved", saveStyle);

            // Reset Campaign Button
            Rect btnResetRect = new Rect(barRect.x + barRect.width - 120f, barRect.y + 7f, 105f, 32f);
            Color oldBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.85f, 0.25f, 0.25f, 0.95f);
            if (GUI.Button(btnResetRect, "<b>🔄 Reset Run</b>"))
            {
                showResetConfirmModal = true;
            }
            GUI.backgroundColor = oldBg;

            // Reset Confirmation Modal Dialog
            if (showResetConfirmModal)
            {
                DrawResetConfirmModal();
            }
        }

        private void DrawResetConfirmModal()
        {
            // Dimmer Background
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), solidTex);
            GUI.color = Color.white;

            float modalW = 460f;
            float modalH = 220f;
            float modalX = (Screen.width - modalW) * 0.5f;
            float modalY = (Screen.height - modalH) * 0.5f;
            Rect modalRect = new Rect(modalX, modalY, modalW, modalH);

            // Modal Body
            GUI.color = new Color(0.10f, 0.12f, 0.18f, 0.98f);
            GUI.DrawTexture(modalRect, solidTex);

            // Modal Border
            GUI.color = new Color(0.9f, 0.3f, 0.3f, 1f);
            GUI.DrawTexture(new Rect(modalRect.x, modalRect.y, modalRect.width, 3), solidTex);
            GUI.DrawTexture(new Rect(modalRect.x, modalRect.y + modalRect.height - 3, modalRect.width, 3), solidTex);
            GUI.DrawTexture(new Rect(modalRect.x, modalRect.y, 3, modalRect.height), solidTex);
            GUI.DrawTexture(new Rect(modalRect.x + modalRect.width - 3, modalRect.y, 3, modalRect.height), solidTex);
            GUI.color = Color.white;

            // Header Title
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                richText = true
            };
            titleStyle.normal.textColor = new Color(1f, 0.4f, 0.4f);
            GUI.Label(new Rect(modalX, modalY + 18f, modalW, 30f), "⚠️ RESTART CAMPAIGN RUN?", titleStyle);

            // Warning Desc
            GUIStyle descStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                wordWrap = true,
                richText = true
            };
            descStyle.normal.textColor = new Color(0.9f, 0.9f, 0.95f);
            GUI.Label(new Rect(modalX + 25f, modalY + 55f, modalW - 50f, 75f),
                "Are you sure you want to abandon current progress?\n\nAll days, food stock, domain resources, and crusade timers will be reset back to <b>Day 1</b>.", descStyle);

            // Buttons: Confirm vs Cancel
            float btnW = 160f;
            float btnH = 40f;
            float btnY = modalY + modalH - 58f;

            Color oldBg = GUI.backgroundColor;

            // Confirm Button
            GUI.backgroundColor = new Color(0.85f, 0.20f, 0.20f, 1f);
            if (GUI.Button(new Rect(modalX + 45f, btnY, btnW, btnH), "<b>Yes, Reset Run</b>"))
            {
                showResetConfirmModal = false;
                if (CampaignManager.Instance != null)
                {
                    CampaignManager.Instance.ResetCampaign();
                }
            }

            // Cancel Button
            GUI.backgroundColor = new Color(0.25f, 0.35f, 0.50f, 1f);
            if (GUI.Button(new Rect(modalX + modalW - btnW - 45f, btnY, btnW, btnH), "<b>Cancel</b>"))
            {
                showResetConfirmModal = false;
            }

            GUI.backgroundColor = oldBg;
        }

        #endregion
    }
}

