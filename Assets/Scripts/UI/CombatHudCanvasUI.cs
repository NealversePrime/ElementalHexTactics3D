using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ElementalHexTactics3D.InputHandling;
using ElementalHexTactics3D.Units;
using ElementalHexTactics3D.Turn;
using ElementalHexTactics3D.Combat;
using ElementalHexTactics3D.CameraControl;

namespace ElementalHexTactics3D.UI
{
    /// <summary>
    /// Crisp, modern uGUI Canvas Combat HUD.
    /// Provides a high-contrast Unit Status Card (HP bar, elemental attunement, cores),
    /// dynamic Round & Phase Ribbon, and a tactile Bottom Action Bar for moves, spells, pushes, and Rift summons.
    /// Self-healing: Automatically builds its UI hierarchy if not present in the scene.
    /// </summary>
    public class CombatHudCanvasUI : MonoBehaviour
    {
        public static CombatHudCanvasUI Instance { get; private set; }

        public static CombatHudCanvasUI EnsureInstance()
        {
            if (Instance != null) return Instance;

            CombatHudCanvasUI existing = Object.FindAnyObjectByType<CombatHudCanvasUI>(FindObjectsInactive.Include);
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }

            Canvas canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas != null)
            {
                Transform foundHud = canvas.transform.Find("Panel_InGameHUD");
                if (foundHud != null)
                {
                    var comp = foundHud.GetComponent<CombatHudCanvasUI>() ?? foundHud.gameObject.AddComponent<CombatHudCanvasUI>();
                    return comp;
                }

                GameObject hud = new GameObject("Panel_InGameHUD");
                hud.transform.SetParent(canvas.transform, false);
                return hud.AddComponent<CombatHudCanvasUI>();
            }

            return null;
        }

        [Header("Unit Status Card (Top-Left - Disgaea Style)")]
        [SerializeField] private GameObject unitCardRoot;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Text unitNameText;
        [SerializeField] private Text unitArchetypeText;
        [SerializeField] private Text turnStatusText;
        [SerializeField] private Slider hpSlider;
        [SerializeField] private Image hpFillImage;
        [SerializeField] private Text hpValueText;
        [SerializeField] private Slider spSlider;
        [SerializeField] private Image spFillImage;
        [SerializeField] private Text spValueText;
        [SerializeField] private Text statMatrixCol1;
        [SerializeField] private Text statMatrixCol2;
        [SerializeField] private Text equipStanceText;
        [SerializeField] private Text bottomTacticalText;
        [SerializeField] private Text bottomResistText;
        [SerializeField] private Text statsText;
        [SerializeField] private Text attunementBadgeText;
        [SerializeField] private Image attunementBadgeBg;
        [SerializeField] private Text elementalCoresText;

        [Header("Turn Phase Ribbon (Top-Center)")]
        [SerializeField] private GameObject turnRibbonRoot;
        [SerializeField] private Image turnRibbonBg;
        [SerializeField] private Text roundLabelText;
        [SerializeField] private Text phaseStatusText;

        [Header("Bottom Action Bar")]
        [SerializeField] private GameObject actionBarRoot;
        [SerializeField] private GameObject unanchoredSection;
        [SerializeField] private GameObject riftPanelSection;
        [SerializeField] private GameObject unitAbilitiesSection;

        // Unanchored Section (When Rift is not yet opened)
        [SerializeField] private Button btnTearRift;
        [SerializeField] private Text txtTearRift;
        [SerializeField] private Text txtUnanchoredHeader;
        [SerializeField] private Text txtRiftHeader;
        [SerializeField] private Button btnRandomizeMap;

        // Rift / Base Panel Buttons
        [SerializeField] private Button btnOpenRiftDispatch;
        [SerializeField] private Text txtOpenRiftDispatch;
        [SerializeField] private Button btnDeployCommander;
        [SerializeField] private Text txtDeployCommander;
        [SerializeField] private Button btnSummonTitanRift;
        [SerializeField] private Text txtSummonTitanRift;

        // Unit Ability Buttons
        [SerializeField] private Button btnMove;
        [SerializeField] private Text txtMove;
        [SerializeField] private Button btnFireball;
        [SerializeField] private Text txtFireball;
        [SerializeField] private Button btnWaterSurge;
        [SerializeField] private Text txtWaterSurge;
        [SerializeField] private Button btnEarthPillar;
        [SerializeField] private Text txtEarthPillar;
        [SerializeField] private Button btnPush;
        [SerializeField] private Text txtPush;
        [SerializeField] private Button btnHarvestCore;
        [SerializeField] private Text txtHarvestCore;
        [SerializeField] private Button btnTitanStrike;
        [SerializeField] private Text txtTitanStrike;
        [SerializeField] private Button btnMagmaCataclysm;
        [SerializeField] private Text txtMagmaCataclysm;
        [SerializeField] private Button btnSummonTitanAbility;
        [SerializeField] private Text txtSummonTitanAbility;
        [SerializeField] private Button btnRecall;
        [SerializeField] private Text txtRecall;

        // Universal Button
        [SerializeField] private Button btnEndTurn;
        [SerializeField] private Text txtEndTurn;

        [Header("Disgaea Style Panels")]
        [SerializeField] private GameObject dispatchModalRoot;
        [SerializeField] private Image dispatchPreviewPortrait;
        [SerializeField] private Text dispatchPreviewName;
        [SerializeField] private Text dispatchPreviewClass;
        [SerializeField] private Text dispatchPreviewStats;
        [SerializeField] private Text dispatchPreviewCost;
        [SerializeField] private Transform dispatchListContent;
        [SerializeField] private Button btnCloseDispatch;
        [SerializeField] private Button btnDispatchConfirm;
        [SerializeField] private Text txtDispatchConfirm;
        private TacticalUnit3D currentPreviewDispatchUnit;

        [Header("Disgaea Command Menu")]
        [SerializeField] private GameObject commandMenuRoot;
        [SerializeField] private Text txtCmdHeader;
        [SerializeField] private Button btnCmdMove;
        [SerializeField] private Text txtCmdMove;
        [SerializeField] private Button btnCmdAttack;
        [SerializeField] private Text txtCmdAttack;
        [SerializeField] private Button btnCmdSpecial;
        [SerializeField] private Text txtCmdSpecial;
        [SerializeField] private Button btnCmdDefend;
        [SerializeField] private Text txtCmdDefend;
        [SerializeField] private Button btnCmdPush;
        [SerializeField] private Text txtCmdPush;
        [SerializeField] private Button btnCmdAbsorb;
        [SerializeField] private Text txtCmdAbsorb;
        [SerializeField] private Button btnCmdItem;
        [SerializeField] private Text txtCmdItem;
        [SerializeField] private Button btnCmdWait;
        [SerializeField] private Button btnCmdCancel;

        [Header("Disgaea Submenus")]
        [SerializeField] private GameObject specialSubmenuRoot;
        [SerializeField] private Transform specialSubmenuContent;
        [SerializeField] private Text txtSpecialHeader;
        [SerializeField] private Button btnSpecialBack;

        [SerializeField] private GameObject itemSubmenuRoot;
        [SerializeField] private Transform itemSubmenuContent;
        [SerializeField] private Text txtItemHeader;
        [SerializeField] private Button btnItemBack;

        [Header("Bottom Skill Description Window")]
        [SerializeField] private GameObject skillInfoWindowRoot;
        [SerializeField] private Text txtSkillInfoTitle;
        [SerializeField] private Text txtSkillInfoBadges;
        [SerializeField] private Text txtSkillInfoEffect;
        [SerializeField] private Text txtSkillInfoDesc;
        private string activeHoverSkillTitle = null;

        [Header("Theme Colors")]
        [SerializeField] private Color colHpFull = new Color(0.30f, 0.78f, 0.35f, 1f);
        [SerializeField] private Color colHpMid = new Color(1.0f, 0.65f, 0.15f, 1f);
        [SerializeField] private Color colHpLow = new Color(0.95f, 0.25f, 0.20f, 1f);

        [SerializeField] private Color colPlayerRibbon = new Color(0.08f, 0.25f, 0.38f, 0.95f);
        [SerializeField] private Color colEnemyRibbon = new Color(0.38f, 0.10f, 0.08f, 0.95f);

        private CanvasGroup hudCanvasGroup;

        public void SetHudVisible(bool visible)
        {
            if (hudCanvasGroup == null)
            {
                hudCanvasGroup = GetComponent<CanvasGroup>();
                if (hudCanvasGroup == null)
                {
                    hudCanvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (hudCanvasGroup != null)
            {
                hudCanvasGroup.alpha = visible ? 1f : 0f;
                hudCanvasGroup.interactable = visible;
                hudCanvasGroup.blocksRaycasts = visible;
            }
        }

        public bool IsDispatchModalActive => (dispatchModalRoot != null && dispatchModalRoot.activeInHierarchy);

        public bool IsScreenPointOccludedByHud(Vector2 screenPos)
        {
            if (hudCanvasGroup != null && hudCanvasGroup.alpha <= 0.01f) return false;

            if (dispatchModalRoot != null && dispatchModalRoot.activeInHierarchy)
            {
                RectTransform rt = dispatchModalRoot.GetComponent<RectTransform>();
                if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos))
                    return true;
            }

            if (commandMenuRoot != null && commandMenuRoot.activeInHierarchy)
            {
                RectTransform rt = commandMenuRoot.GetComponent<RectTransform>();
                if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos))
                    return true;
            }

            if (specialSubmenuRoot != null && specialSubmenuRoot.activeInHierarchy)
            {
                RectTransform rt = specialSubmenuRoot.GetComponent<RectTransform>();
                if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos))
                    return true;
            }

            if (itemSubmenuRoot != null && itemSubmenuRoot.activeInHierarchy)
            {
                RectTransform rt = itemSubmenuRoot.GetComponent<RectTransform>();
                if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos))
                    return true;
            }

            if (skillInfoWindowRoot != null && skillInfoWindowRoot.activeInHierarchy)
            {
                RectTransform rt = skillInfoWindowRoot.GetComponent<RectTransform>();
                if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos))
                    return true;
            }

            if (unitCardRoot != null && unitCardRoot.activeInHierarchy)
            {
                RectTransform rt = unitCardRoot.GetComponent<RectTransform>();
                if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos))
                    return true;
            }

            if (btnEndTurn != null && btnEndTurn.gameObject.activeInHierarchy)
            {
                RectTransform rt = btnEndTurn.GetComponent<RectTransform>();
                if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos))
                    return true;
            }

            if (actionBarRoot != null && actionBarRoot.activeInHierarchy)
            {
                RectTransform rt = actionBarRoot.GetComponent<RectTransform>();
                if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos))
                    return true;
            }

            return false;
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }

            hudCanvasGroup = GetComponent<CanvasGroup>();
            if (hudCanvasGroup == null)
            {
                hudCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            EnsureHudBuilt();
            WireButtonListeners();
        }

        private void OnEnable()
        {
            EnsureHudBuilt();
            StoryDialogueUI.OnDialogueStarted += HandleDialogueStarted;
            StoryDialogueUI.OnDialogueFinished += HandleDialogueFinished;
        }

        private void OnDisable()
        {
            StoryDialogueUI.OnDialogueStarted -= HandleDialogueStarted;
            StoryDialogueUI.OnDialogueFinished -= HandleDialogueFinished;
        }

        private void OnDestroy()
        {
            StoryDialogueUI.OnDialogueStarted -= HandleDialogueStarted;
            StoryDialogueUI.OnDialogueFinished -= HandleDialogueFinished;
            if (Instance == this) Instance = null;
        }

        private void HandleDialogueStarted()
        {
            SetHudVisible(false);
        }

        private void HandleDialogueFinished()
        {
            SetHudVisible(true);
        }

        public void EnsureHudBuilt()
        {
            Transform existing = transform.Find("CombatHUD_Container");
            if (existing != null)
            {
                HookExistingContainer(existing);
            }
            else if (actionBarRoot == null || unitCardRoot == null)
            {
                BuildDynamicHud();
            }

            Transform container = existing ?? transform;

            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Sprite pf = TacticalUnitSpawner.LoadUISprite("UI_WindowFrame_RPG.png") ?? TacticalUnitSpawner.LoadUISprite("UI_Panel_Frame.png");
            Sprite bn = TacticalUnitSpawner.LoadUISprite("UI_Button_Normal.png");

            if (dispatchModalRoot == null)
            {
                Transform disp = container.Find("Modal_DisgaeaDispatch");
                if (disp != null) dispatchModalRoot = disp.gameObject;
                else BuildDisgaeaDispatchModal(container, f, pf, bn);
            }

            if (commandMenuRoot == null)
            {
                Transform cmd = container.Find("Menu_DisgaeaCommand");
                if (cmd != null)
                {
                    commandMenuRoot = cmd.gameObject;
                    HookExistingCommandMenu(cmd);
                }
                else
                {
                    BuildDisgaeaCommandMenu(container, f, pf, bn);
                }
            }

            if (unitAbilitiesSection != null) unitAbilitiesSection.SetActive(false);
            if (dispatchModalRoot != null && !dispatchModalRoot.activeSelf) dispatchModalRoot.SetActive(false);
            if (commandMenuRoot != null && !commandMenuRoot.activeSelf) commandMenuRoot.SetActive(false);
            if (specialSubmenuRoot != null && !specialSubmenuRoot.activeSelf) specialSubmenuRoot.SetActive(false);
            if (itemSubmenuRoot != null && !itemSubmenuRoot.activeSelf) itemSubmenuRoot.SetActive(false);
            if (actionBarRoot != null) actionBarRoot.SetActive(false);

            ConfigureActionBarRightDock(actionBarRoot);

            // Clean Disgaea TRPG UX: Ensure single unified Squad Dispatch trigger button is wired
            if (btnOpenRiftDispatch == null && actionBarRoot != null)
            {
                btnOpenRiftDispatch = actionBarRoot.transform.Find("Btn_OpenRiftDispatch")?.GetComponent<Button>();
            }
            if (btnOpenRiftDispatch != null)
            {
                btnOpenRiftDispatch.onClick.RemoveAllListeners();
                btnOpenRiftDispatch.onClick.AddListener(OnOpenRiftDispatchClicked);
            }

            // Dedicated Bottom-Right End Turn Button (Decoupled from bottom action bar)
            Transform foundEndTurn = container.Find("Btn_EndTurn");
            if (foundEndTurn == null && actionBarRoot != null)
            {
                foundEndTurn = actionBarRoot.transform.Find("Btn_EndTurn");
            }
            if (foundEndTurn != null)
            {
                if (foundEndTurn.parent != container)
                {
                    foundEndTurn.SetParent(container, false);
                }
                SetRect(foundEndTurn.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 24f), new Vector2(180f, 56f));
                btnEndTurn = foundEndTurn.GetComponent<Button>();
                txtEndTurn = foundEndTurn.GetComponentInChildren<Text>();
            }
            else if (btnEndTurn == null)
            {
                btnEndTurn = CreateBtn("Btn_EndTurn", container, "<b>⏳ END TURN</b>\n<size=11><color=#CFD8DC>[Space / Click]</color></size>", f, 13, bn, new Color(0.72f, 0.20f, 0.16f, 0.95f), 180f, 56f);
                SetRect(btnEndTurn.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 24f), new Vector2(180f, 56f));
                txtEndTurn = btnEndTurn.GetComponentInChildren<Text>();
            }
            if (btnEndTurn != null)
            {
                btnEndTurn.onClick.RemoveAllListeners();
                btnEndTurn.onClick.AddListener(OnEndTurnClicked);
            }

            Transform skillInfoTrans = container.Find("Panel_SkillDescription");
            if (skillInfoTrans != null)
            {
                if (skillInfoTrans.Find("Txt_SkillInfoEffect") == null || skillInfoTrans.Find("Divider_Line") != null)
                {
                    Destroy(skillInfoTrans.gameObject);
                    skillInfoWindowRoot = null;
                }
                else
                {
                    skillInfoWindowRoot = skillInfoTrans.gameObject;
                    txtSkillInfoTitle = skillInfoTrans.Find("Txt_SkillInfoTitle")?.GetComponent<Text>();
                    txtSkillInfoBadges = skillInfoTrans.Find("Txt_SkillInfoBadges")?.GetComponent<Text>();
                    txtSkillInfoEffect = skillInfoTrans.Find("Txt_SkillInfoEffect")?.GetComponent<Text>();
                    txtSkillInfoDesc = skillInfoTrans.Find("Txt_SkillInfoDesc")?.GetComponent<Text>();
                }
            }
            if (skillInfoWindowRoot == null)
            {
                Font skillFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                BuildSkillDescriptionWindow(container, skillFont);
            }

            ApplyRpgMakerWindowStyle(unitCardRoot, new Color(0.08f, 0.07f, 0.14f, 0.95f));
            ApplyRpgMakerWindowStyle(dispatchModalRoot, new Color(0.08f, 0.07f, 0.14f, 1.0f));
            ApplyRpgMakerWindowStyle(commandMenuRoot, new Color(0.07f, 0.06f, 0.12f, 0.96f));
            ApplyRpgMakerWindowStyle(specialSubmenuRoot, new Color(0.06f, 0.08f, 0.14f, 0.96f));
            ApplyRpgMakerWindowStyle(itemSubmenuRoot, new Color(0.06f, 0.08f, 0.14f, 0.96f));
            ApplyRpgMakerWindowStyle(skillInfoWindowRoot, new Color(0.06f, 0.07f, 0.14f, 0.96f));
        }

        private void HookExistingContainer(Transform container)
        {
            Transform card = container.Find("Card_UnitStatus");
            if (card != null && card.Find("Slider_SP") == null)
            {
                DestroyImmediate(card.gameObject);
                card = null;
            }

            if (card == null)
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#if UNITY_EDITOR
                Font fontBold = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Font_Bold.ttf");
                if (fontBold != null) font = fontBold;
#endif
                Sprite panelFrame = ElementalHexTactics3D.Units.TacticalUnitSpawner.LoadUISprite("UI_Panel_Frame.png");
                Sprite windowFrameRpg = ElementalHexTactics3D.Units.TacticalUnitSpawner.LoadUISprite("UI_WindowFrame_RPG.png") ?? panelFrame;
                BuildDisgaeaUnitCard(container, font, panelFrame, windowFrameRpg);
            }
            else
            {
                unitCardRoot = card.gameObject;
                portraitImage = card.Find("Portrait_Frame/Portrait_Image")?.GetComponent<Image>();
                unitNameText = card.Find("Txt_UnitName")?.GetComponent<Text>();
                unitArchetypeText = card.Find("Txt_Archetype")?.GetComponent<Text>();
                turnStatusText = card.Find("Txt_TurnStatus")?.GetComponent<Text>();
                statMatrixCol1 = card.Find("Txt_StatCol1")?.GetComponent<Text>();
                statMatrixCol2 = card.Find("Txt_StatCol2")?.GetComponent<Text>();
                equipStanceText = card.Find("Panel_Equip/Txt_EquipBody")?.GetComponent<Text>();
                bottomTacticalText = card.Find("Ribbon_Bottom/Txt_BottomTactical")?.GetComponent<Text>();
                bottomResistText = card.Find("Ribbon_Bottom/Txt_BottomResist")?.GetComponent<Text>();
                statsText = card.Find("Txt_Stats")?.GetComponent<Text>();
                elementalCoresText = card.Find("Txt_Cores")?.GetComponent<Text>();

                Transform slObj = card.Find("Slider_HP");
                if (slObj != null)
                {
                    hpSlider = slObj.GetComponent<Slider>();
                    hpFillImage = slObj.Find("Fill Area/Fill")?.GetComponent<Image>();
                    hpValueText = slObj.Find("Txt_HPValue")?.GetComponent<Text>();
                }

                Transform spObj = card.Find("Slider_SP");
                if (spObj != null)
                {
                    spSlider = spObj.GetComponent<Slider>();
                    spFillImage = spObj.Find("Fill Area/Fill")?.GetComponent<Image>();
                    spValueText = spObj.Find("Txt_SPValue")?.GetComponent<Text>();
                }

                Transform badge = card.Find("Badge_Attunement");
                if (badge != null)
                {
                    attunementBadgeBg = badge.GetComponent<Image>();
                    attunementBadgeText = badge.Find("Text")?.GetComponent<Text>();
                }
            }

            Transform rib = container.Find("Ribbon_TurnPhase");
            if (rib != null)
            {
                turnRibbonRoot = rib.gameObject;
                turnRibbonBg = rib.GetComponent<Image>();
                roundLabelText = rib.Find("Txt_Round")?.GetComponent<Text>();
                phaseStatusText = rib.Find("Txt_Phase")?.GetComponent<Text>();
            }

            Transform bar = container.Find("Bar_CombatActions");
            if (bar != null)
            {
                actionBarRoot = bar.gameObject;
                unanchoredSection = bar.Find("Section_Unanchored")?.gameObject;
                riftPanelSection = bar.Find("Section_RiftPanel")?.gameObject;
                unitAbilitiesSection = bar.Find("Section_UnitAbilities")?.gameObject;

                if (unanchoredSection != null)
                {
                    btnTearRift = unanchoredSection.transform.Find("Btn_TearRift")?.GetComponent<Button>();
                    txtTearRift = btnTearRift?.GetComponentInChildren<Text>();
                    btnRandomizeMap = unanchoredSection.transform.Find("Btn_RandomizeMap")?.GetComponent<Button>();
                }
                ConfigureActionBarRightDock(actionBarRoot);

                if (riftPanelSection != null)
                {
                    btnDeployCommander = riftPanelSection.transform.Find("Btn_DeployCommander")?.GetComponent<Button>();
                    txtDeployCommander = btnDeployCommander?.GetComponentInChildren<Text>();
                    btnSummonTitanRift = riftPanelSection.transform.Find("Btn_SummonTitanRift")?.GetComponent<Button>();
                    txtSummonTitanRift = btnSummonTitanRift?.GetComponentInChildren<Text>();
                }

                if (unitAbilitiesSection != null)
                {
                    btnMove = unitAbilitiesSection.transform.Find("Btn_Move")?.GetComponent<Button>();
                    txtMove = btnMove?.GetComponentInChildren<Text>();
                    btnFireball = unitAbilitiesSection.transform.Find("Btn_Fireball")?.GetComponent<Button>();
                    txtFireball = btnFireball?.GetComponentInChildren<Text>();
                    btnWaterSurge = unitAbilitiesSection.transform.Find("Btn_WaterSurge")?.GetComponent<Button>();
                    txtWaterSurge = btnWaterSurge?.GetComponentInChildren<Text>();
                    btnEarthPillar = unitAbilitiesSection.transform.Find("Btn_EarthPillar")?.GetComponent<Button>();
                    txtEarthPillar = btnEarthPillar?.GetComponentInChildren<Text>();
                    btnPush = unitAbilitiesSection.transform.Find("Btn_Push")?.GetComponent<Button>();
                    txtPush = btnPush?.GetComponentInChildren<Text>();
                    btnHarvestCore = unitAbilitiesSection.transform.Find("Btn_HarvestCore")?.GetComponent<Button>();
                    txtHarvestCore = btnHarvestCore?.GetComponentInChildren<Text>();
                    btnTitanStrike = unitAbilitiesSection.transform.Find("Btn_TitanStrike")?.GetComponent<Button>();
                    txtTitanStrike = btnTitanStrike?.GetComponentInChildren<Text>();
                    btnMagmaCataclysm = unitAbilitiesSection.transform.Find("Btn_Cataclysm")?.GetComponent<Button>();
                    txtMagmaCataclysm = btnMagmaCataclysm?.GetComponentInChildren<Text>();
                    btnSummonTitanAbility = unitAbilitiesSection.transform.Find("Btn_SummonTitanAbility")?.GetComponent<Button>();
                    txtSummonTitanAbility = btnSummonTitanAbility?.GetComponentInChildren<Text>();
                    btnRecall = unitAbilitiesSection.transform.Find("Btn_Recall")?.GetComponent<Button>();
                    txtRecall = btnRecall?.GetComponentInChildren<Text>();
                }

                Transform endTr = container.Find("Btn_EndTurn") ?? bar.Find("Btn_EndTurn");
                if (endTr != null)
                {
                    if (endTr.parent != container) endTr.SetParent(container, false);
                    SetRect(endTr.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 24f), new Vector2(180f, 56f));
                    btnEndTurn = endTr.GetComponent<Button>();
                    txtEndTurn = endTr.GetComponentInChildren<Text>();
                }
            }

            Transform dispatch = container.Find("Modal_DisgaeaDispatch");
            if (dispatch != null) dispatchModalRoot = dispatch.gameObject;
            Transform cmdMenu = container.Find("Menu_DisgaeaCommand");
            if (cmdMenu != null)
            {
                commandMenuRoot = cmdMenu.gameObject;
                HookExistingCommandMenu(cmdMenu);
            }
            if (dispatchModalRoot == null || commandMenuRoot == null)
            {
                Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                Sprite pf = TacticalUnitSpawner.LoadUISprite("UI_WindowFrame_RPG.png") ?? TacticalUnitSpawner.LoadUISprite("UI_Panel_Frame.png");
                Sprite bn = TacticalUnitSpawner.LoadUISprite("UI_Button_Normal.png");
                if (dispatchModalRoot == null) BuildDisgaeaDispatchModal(container, f, pf, bn);
                if (commandMenuRoot == null) BuildDisgaeaCommandMenu(container, f, pf, bn);
            }

            if (unitAbilitiesSection != null) unitAbilitiesSection.SetActive(false);
            if (dispatchModalRoot != null) dispatchModalRoot.SetActive(false);
            if (commandMenuRoot != null) commandMenuRoot.SetActive(false);
            if (specialSubmenuRoot != null) specialSubmenuRoot.SetActive(false);
            if (itemSubmenuRoot != null) itemSubmenuRoot.SetActive(false);

            // Ensure RPG Maker 2-layer composite styling is applied to all key windows
            ApplyRpgMakerWindowStyle(unitCardRoot, new Color(0.08f, 0.07f, 0.14f, 0.95f));
            ApplyRpgMakerWindowStyle(dispatchModalRoot, new Color(0.08f, 0.07f, 0.14f, 1.0f));
            ApplyRpgMakerWindowStyle(commandMenuRoot, new Color(0.07f, 0.06f, 0.12f, 0.96f));
            ApplyRpgMakerWindowStyle(specialSubmenuRoot, new Color(0.06f, 0.08f, 0.14f, 0.96f));
            ApplyRpgMakerWindowStyle(itemSubmenuRoot, new Color(0.06f, 0.08f, 0.14f, 0.96f));
        }

        private void HookExistingCommandMenu(Transform cmd)
        {
            if (cmd == null) return;

            // Self-healing check: if the existing menu in scene lacks the new buttons, rebuild it!
            if (cmd.Find("Btn_CmdDefend") == null || cmd.Find("Btn_CmdSpecial") == null)
            {
                Transform parent = cmd.parent;
                Destroy(cmd.gameObject);
                Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                Sprite pf = TacticalUnitSpawner.LoadUISprite("UI_WindowFrame_RPG.png") ?? TacticalUnitSpawner.LoadUISprite("UI_Panel_Frame.png");
                Sprite bn = TacticalUnitSpawner.LoadUISprite("UI_Button_Normal.png");
                BuildDisgaeaCommandMenu(parent, f, pf, bn);
                return;
            }

            commandMenuRoot = cmd.gameObject;
            txtCmdHeader = cmd.Find("Txt_CmdHeader")?.GetComponent<Text>();
            if (txtCmdHeader != null) txtCmdHeader.rectTransform.sizeDelta = new Vector2(154f, 24f);
            btnCmdMove = cmd.Find("Btn_CmdMove")?.GetComponent<Button>();
            txtCmdMove = btnCmdMove?.GetComponentInChildren<Text>();
            btnCmdAttack = cmd.Find("Btn_CmdAttack")?.GetComponent<Button>();
            txtCmdAttack = btnCmdAttack?.GetComponentInChildren<Text>();
            btnCmdSpecial = cmd.Find("Btn_CmdSpecial")?.GetComponent<Button>();
            txtCmdSpecial = btnCmdSpecial?.GetComponentInChildren<Text>();
            btnCmdDefend = cmd.Find("Btn_CmdDefend")?.GetComponent<Button>();
            txtCmdDefend = btnCmdDefend?.GetComponentInChildren<Text>();
            btnCmdPush = cmd.Find("Btn_CmdPush")?.GetComponent<Button>();
            txtCmdPush = btnCmdPush?.GetComponentInChildren<Text>();
            btnCmdAbsorb = cmd.Find("Btn_CmdAbsorb")?.GetComponent<Button>();
            txtCmdAbsorb = btnCmdAbsorb?.GetComponentInChildren<Text>();
            btnCmdItem = cmd.Find("Btn_CmdItem")?.GetComponent<Button>();
            txtCmdItem = btnCmdItem?.GetComponentInChildren<Text>();
            btnCmdWait = cmd.Find("Btn_CmdWait")?.GetComponent<Button>();
            btnCmdCancel = cmd.Find("Btn_CmdCancel")?.GetComponent<Button>();

            if (btnCmdMove != null) { btnCmdMove.onClick.RemoveAllListeners(); btnCmdMove.onClick.AddListener(() => OnActionClicked(UnitActionMode.Move)); }
            if (btnCmdAttack != null) { btnCmdAttack.onClick.RemoveAllListeners(); btnCmdAttack.onClick.AddListener(() => OnActionClicked(UnitActionMode.TitanStrike)); }
            if (btnCmdSpecial != null) { btnCmdSpecial.onClick.RemoveAllListeners(); btnCmdSpecial.onClick.AddListener(OnCmdSpecialClicked); }
            if (btnCmdDefend != null) { btnCmdDefend.onClick.RemoveAllListeners(); btnCmdDefend.onClick.AddListener(OnCmdDefendClicked); }
            if (btnCmdPush != null) { btnCmdPush.onClick.RemoveAllListeners(); btnCmdPush.onClick.AddListener(() => OnActionClicked(UnitActionMode.KineticPush)); }
            if (btnCmdAbsorb != null) { btnCmdAbsorb.onClick.RemoveAllListeners(); btnCmdAbsorb.onClick.AddListener(() => OnActionClicked(UnitActionMode.ConsumeLand)); }
            if (btnCmdItem != null) { btnCmdItem.onClick.RemoveAllListeners(); btnCmdItem.onClick.AddListener(OnCmdItemClicked); }
            if (btnCmdWait != null) { btnCmdWait.onClick.RemoveAllListeners(); btnCmdWait.onClick.AddListener(OnCmdWaitClicked); }
            if (btnCmdCancel != null) { btnCmdCancel.onClick.RemoveAllListeners(); btnCmdCancel.onClick.AddListener(OnCmdCancelClicked); }

            Transform specialSub = cmd.parent.Find("Menu_DisgaeaSpecialSubmenu");
            if (specialSub != null)
            {
                specialSubmenuRoot = specialSub.gameObject;
                specialSubmenuContent = specialSub.Find("Special_Content");
                txtSpecialHeader = specialSub.Find("Txt_SpecialHeader")?.GetComponent<Text>();
                if (txtSpecialHeader != null) txtSpecialHeader.rectTransform.sizeDelta = new Vector2(164f, 24f);
                btnSpecialBack = specialSub.Find("Btn_SpecialBack")?.GetComponent<Button>();
                if (btnSpecialBack != null) { btnSpecialBack.onClick.RemoveAllListeners(); btnSpecialBack.onClick.AddListener(OnSpecialBackClicked); }
            }
            Transform itemSub = cmd.parent.Find("Menu_DisgaeaItemSubmenu");
            if (itemSub != null)
            {
                itemSubmenuRoot = itemSub.gameObject;
                itemSubmenuContent = itemSub.Find("Item_Content");
                txtItemHeader = itemSub.Find("Txt_ItemHeader")?.GetComponent<Text>();
                if (txtItemHeader != null) txtItemHeader.rectTransform.sizeDelta = new Vector2(164f, 24f);
                btnItemBack = itemSub.Find("Btn_ItemBack")?.GetComponent<Button>();
                if (btnItemBack != null) { btnItemBack.onClick.RemoveAllListeners(); btnItemBack.onClick.AddListener(OnItemBackClicked); }
            }
        }

        private void ConfigureActionBarRightDock(GameObject barObj)
        {
            if (barObj == null) return;

            // 1. Locate all buttons and headers wherever they currently reside under barObj
            if (btnTearRift == null)
            {
                Button[] allBtns = barObj.GetComponentsInChildren<Button>(true);
                foreach (var b in allBtns)
                {
                    if (b != null && b.name == "Btn_TearRift") { btnTearRift = b; break; }
                }
            }
            if (btnRandomizeMap == null)
            {
                Button[] allBtns = barObj.GetComponentsInChildren<Button>(true);
                foreach (var b in allBtns)
                {
                    if (b != null && b.name == "Btn_RandomizeMap") { btnRandomizeMap = b; break; }
                }
            }

            // Eliminate any duplicate Btn_OpenRiftDispatch buttons
            List<Button> dispatchBtns = new List<Button>();
            Button[] allBarBtns = barObj.GetComponentsInChildren<Button>(true);
            foreach (var b in allBarBtns)
            {
                if (b != null && b.name == "Btn_OpenRiftDispatch") dispatchBtns.Add(b);
            }
            if (dispatchBtns.Count > 0)
            {
                btnOpenRiftDispatch = dispatchBtns[0];
                for (int i = 1; i < dispatchBtns.Count; i++)
                {
                    if (dispatchBtns[i] != null) DestroyImmediate(dispatchBtns[i].gameObject);
                }
            }

            if (txtUnanchoredHeader == null)
            {
                Text[] allTexts = barObj.GetComponentsInChildren<Text>(true);
                foreach (var t in allTexts)
                {
                    if (t != null && t.name == "Txt_UnanchoredHeader") { txtUnanchoredHeader = t; break; }
                }
            }
            if (txtRiftHeader == null)
            {
                Text[] allTexts = barObj.GetComponentsInChildren<Text>(true);
                foreach (var t in allTexts)
                {
                    if (t != null && t.name == "Txt_RiftHeader") { txtRiftHeader = t; break; }
                }
            }

            // 2. Remove legacy intermediate containers (Section_Unanchored, Section_RiftPanel, Section_UnitAbilities)
            //    Move buttons directly into barObj so VerticalLayoutGroup + ContentSizeFitter wrap them perfectly!
            if (txtUnanchoredHeader != null && txtUnanchoredHeader.transform.parent != barObj.transform)
            {
                txtUnanchoredHeader.transform.SetParent(barObj.transform, false);
            }
            if (btnTearRift != null && btnTearRift.transform.parent != barObj.transform)
            {
                btnTearRift.transform.SetParent(barObj.transform, false);
            }
            if (btnRandomizeMap != null && btnRandomizeMap.transform.parent != barObj.transform)
            {
                btnRandomizeMap.transform.SetParent(barObj.transform, false);
            }
            if (txtRiftHeader != null && txtRiftHeader.transform.parent != barObj.transform)
            {
                txtRiftHeader.transform.SetParent(barObj.transform, false);
            }
            if (btnOpenRiftDispatch != null && btnOpenRiftDispatch.transform.parent != barObj.transform)
            {
                btnOpenRiftDispatch.transform.SetParent(barObj.transform, false);
            }

            if (unanchoredSection != null && unanchoredSection != barObj)
            {
                DestroyImmediate(unanchoredSection);
                unanchoredSection = null;
            }
            if (riftPanelSection != null && riftPanelSection != barObj)
            {
                DestroyImmediate(riftPanelSection);
                riftPanelSection = null;
            }
            if (unitAbilitiesSection != null && unitAbilitiesSection != barObj)
            {
                DestroyImmediate(unitAbilitiesSection);
                unitAbilitiesSection = null;
            }

            // 3. Dock barObj on the Right side (matching Menu_DisgaeaCommand at -20, -72, width 175)
            RectTransform rt = barObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                SetRect(rt,
                    new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(-20f, -72f), new Vector2(175f, 120f));
            }

            // Apply modern Disgaea / RPG window styling with sleek basalt background & window frame
            ApplyRpgMakerWindowStyle(barObj, new Color(0.07f, 0.06f, 0.12f, 0.96f));

            // Clean up any horizontal layout group from legacy HUD
            HorizontalLayoutGroup oldHlg = barObj.GetComponent<HorizontalLayoutGroup>();
            if (oldHlg != null) DestroyImmediate(oldHlg);

            // 4. Format Headers cleanly
            if (txtUnanchoredHeader != null)
            {
                txtUnanchoredHeader.text = "🌀 <b>CITADEL GATEWAY</b>";
                txtUnanchoredHeader.fontSize = 11;
                txtUnanchoredHeader.fontStyle = FontStyle.Bold;
                txtUnanchoredHeader.color = new Color(0.85f, 0.65f, 1.0f, 1f);
                txtUnanchoredHeader.alignment = TextAnchor.MiddleCenter;
                RectTransform hr = txtUnanchoredHeader.rectTransform;
                hr.sizeDelta = new Vector2(155f, 22f);
                LayoutElement le = txtUnanchoredHeader.GetComponent<LayoutElement>() ?? txtUnanchoredHeader.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 22f;
                le.minWidth = 155f;
            }

            if (txtRiftHeader != null)
            {
                txtRiftHeader.text = "🌀 <b>CITADEL RIFT</b>";
                txtRiftHeader.fontSize = 11;
                txtRiftHeader.fontStyle = FontStyle.Bold;
                txtRiftHeader.color = new Color(0.85f, 0.65f, 1.0f, 1f);
                txtRiftHeader.alignment = TextAnchor.MiddleCenter;
                RectTransform hr = txtRiftHeader.rectTransform;
                hr.sizeDelta = new Vector2(155f, 22f);
                LayoutElement le = txtRiftHeader.GetComponent<LayoutElement>() ?? txtRiftHeader.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 22f;
                le.minWidth = 155f;
            }

            // 5. Format Action Buttons to exact Disgaea menu proportions (width 155, height 32-34)
            Sprite btnSprite = ElementalHexTactics3D.Units.TacticalUnitSpawner.LoadUISprite("UI_Button_Normal.png");

            if (btnTearRift != null)
            {
                RectTransform btRt = btnTearRift.GetComponent<RectTransform>();
                btRt.sizeDelta = new Vector2(155f, 34f);
                LayoutElement le = btnTearRift.GetComponent<LayoutElement>() ?? btnTearRift.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 34f;
                le.minHeight = 34f;
                le.preferredWidth = 155f;
                le.minWidth = 0f;
                le.flexibleWidth = 0f;

                Image img = btnTearRift.GetComponent<Image>();
                if (img != null)
                {
                    if (btnSprite != null) img.sprite = btnSprite;
                    img.color = new Color(0.42f, 0.16f, 0.65f, 1f);
                }

                if (txtTearRift == null) txtTearRift = btnTearRift.GetComponentInChildren<Text>();
                if (txtTearRift != null)
                {
                    txtTearRift.fontSize = 11;
                    txtTearRift.fontStyle = FontStyle.Bold;
                    txtTearRift.alignment = TextAnchor.MiddleCenter;
                    txtTearRift.text = "🌀 <b>Tear Open Rift</b>";
                }
            }

            if (btnRandomizeMap != null)
            {
                RectTransform brRt = btnRandomizeMap.GetComponent<RectTransform>();
                brRt.sizeDelta = new Vector2(155f, 30f);
                LayoutElement le = btnRandomizeMap.GetComponent<LayoutElement>() ?? btnRandomizeMap.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 30f;
                le.minHeight = 30f;
                le.preferredWidth = 155f;
                le.minWidth = 0f;
                le.flexibleWidth = 0f;

                Image img = btnRandomizeMap.GetComponent<Image>();
                if (img != null)
                {
                    if (btnSprite != null) img.sprite = btnSprite;
                    img.color = new Color(0.20f, 0.28f, 0.40f, 1f);
                }

                Text txt = btnRandomizeMap.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    txt.fontSize = 10;
                    txt.fontStyle = FontStyle.Bold;
                    txt.text = "🎲 <b>Roll Map [R]</b>";
                    txt.alignment = TextAnchor.MiddleCenter;
                }
            }

            if (btnOpenRiftDispatch != null)
            {
                RectTransform boRt = btnOpenRiftDispatch.GetComponent<RectTransform>();
                boRt.sizeDelta = new Vector2(155f, 34f);
                LayoutElement le = btnOpenRiftDispatch.GetComponent<LayoutElement>() ?? btnOpenRiftDispatch.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 34f;
                le.minHeight = 34f;
                le.preferredWidth = 155f;
                le.minWidth = 0f;
                le.flexibleWidth = 0f;

                Image img = btnOpenRiftDispatch.GetComponent<Image>();
                if (img != null)
                {
                    if (btnSprite != null) img.sprite = btnSprite;
                    img.color = new Color(0.40f, 0.15f, 0.65f, 1f);
                }

                Text txt = btnOpenRiftDispatch.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    txt.fontSize = 11;
                    txt.fontStyle = FontStyle.Bold;
                    txt.text = "📦 <b>Squad Dispatch</b>";
                    txt.alignment = TextAnchor.MiddleCenter;
                }
            }

            // 6. Vertical layout on barObj so the frame wraps around the contents with 8px margin
            VerticalLayoutGroup vl = barObj.GetComponent<VerticalLayoutGroup>() ?? barObj.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(8, 8, 8, 8);
            vl.spacing = 4f;
            vl.childAlignment = TextAnchor.UpperCenter;
            vl.childControlWidth = true;
            vl.childControlHeight = false;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;

            ContentSizeFitter csf = barObj.GetComponent<ContentSizeFitter>() ?? barObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            if (rt != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }
        }

        private void ApplyRpgMakerWindowStyle(GameObject panelObj, Color bgColor)
        {
            if (panelObj == null) return;
            Sprite bgSprite = ElementalHexTactics3D.Units.TacticalUnitSpawner.LoadUISprite("UI_WindowBg_RPG.png");
            Sprite frameSprite = ElementalHexTactics3D.Units.TacticalUnitSpawner.LoadUISprite("UI_WindowFrame_RPG.png");

            Image bgImg = panelObj.GetComponent<Image>() ?? panelObj.AddComponent<Image>();
            if (bgSprite != null)
            {
                bgImg.sprite = bgSprite;
                bgImg.type = Image.Type.Sliced;
                bgImg.color = bgColor;
            }
            else
            {
                bgImg.color = bgColor;
            }

            Transform frameTrans = panelObj.transform.Find("Frame_Overlay");
            GameObject frameObj = frameTrans != null ? frameTrans.gameObject : null;
            if (frameObj == null)
            {
                frameObj = new GameObject("Frame_Overlay", typeof(RectTransform));
                frameObj.transform.SetParent(panelObj.transform, false);
            }
            RectTransform foRect = frameObj.GetComponent<RectTransform>();
            foRect.anchorMin = Vector2.zero;
            foRect.anchorMax = Vector2.one;
            foRect.offsetMin = Vector2.zero;
            foRect.offsetMax = Vector2.zero;
            frameObj.transform.SetAsFirstSibling();

            LayoutElement foLe = frameObj.GetComponent<LayoutElement>() ?? frameObj.AddComponent<LayoutElement>();
            foLe.ignoreLayout = true;

            Image foImg = frameObj.GetComponent<Image>() ?? frameObj.AddComponent<Image>();
            if (frameSprite != null)
            {
                foImg.sprite = frameSprite;
                foImg.type = Image.Type.Sliced;
                foImg.color = Color.white;
            }
            foImg.raycastTarget = false;
        }

        private void BuildDisgaeaUnitCard(Transform container, Font font, Sprite panelFrame, Sprite windowFrameRpg)
        {
            unitCardRoot = CreateUI("Card_UnitStatus", container);
            RectTransform cardRt = unitCardRoot.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0f, 1f);
            cardRt.anchorMax = new Vector2(0f, 1f);
            cardRt.pivot = new Vector2(0f, 1f);
            cardRt.sizeDelta = new Vector2(500f, 146f);
            cardRt.anchoredPosition = new Vector2(20f, -20f);

            ApplyRpgMakerWindowStyle(unitCardRoot, new Color(0.08f, 0.07f, 0.14f, 0.96f));

            // 1. Top Header: Unit Name & Class / Turn Readiness
            unitNameText = CreateTxt("Txt_UnitName", unitCardRoot.transform, "<b>Demon Lord</b> <size=12><color=#90A4AE>/ Commander</color></size>", font, 15, new Color(1.0f, 0.88f, 0.40f, 1f), TextAnchor.UpperLeft);
            SetRect(unitNameText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -10f), new Vector2(340f, 22f));

            turnStatusText = CreateTxt("Txt_TurnStatus", unitCardRoot.transform, "✓ READY", font, 12, new Color(0.41f, 0.94f, 0.68f, 1f), TextAnchor.UpperRight);
            SetRect(turnStatusText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -10f), new Vector2(90f, 22f));

            // 2. Left Column: Portrait Standee Frame
            GameObject portObj = CreateUI("Portrait_Frame", unitCardRoot.transform);
            RectTransform portRt = portObj.GetComponent<RectTransform>();
            portRt.anchorMin = new Vector2(0f, 1f);
            portRt.anchorMax = new Vector2(0f, 1f);
            portRt.pivot = new Vector2(0f, 1f);
            portRt.anchoredPosition = new Vector2(16f, -34f);
            portRt.sizeDelta = new Vector2(74f, 74f);
            Image portFrameImg = portObj.AddComponent<Image>();
            portFrameImg.sprite = panelFrame;
            portFrameImg.type = Image.Type.Sliced;
            portFrameImg.color = new Color(0.18f, 0.16f, 0.28f, 1f);

            GameObject portImgObj = CreateUI("Portrait_Image", portObj.transform);
            RectTransform pImgRt = portImgObj.GetComponent<RectTransform>();
            pImgRt.anchorMin = Vector2.zero;
            pImgRt.anchorMax = Vector2.one;
            pImgRt.offsetMin = new Vector2(3f, 3f);
            pImgRt.offsetMax = new Vector2(-3f, -3f);
            portraitImage = portImgObj.AddComponent<Image>();
            portraitImage.preserveAspect = true;

            // 3. Middle Column: Dual Gauges (HP & SP/Cores) + 2x2 Stat Matrix
            // HP Bar
            Text hpLbl = CreateTxt("Txt_HpLabel", unitCardRoot.transform, "<b>HP</b>", font, 11, new Color(0.45f, 0.95f, 0.55f, 1f), TextAnchor.MiddleLeft);
            SetRect(hpLbl.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(98f, -36f), new Vector2(24f, 16f));

            GameObject slObj = CreateUI("Slider_HP", unitCardRoot.transform);
            SetRect(slObj.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(124f, -36f), new Vector2(164f, 16f));
            hpSlider = slObj.AddComponent<Slider>();
            hpSlider.interactable = false;

            GameObject slBg = CreateUI("Background", slObj.transform);
            SetStretch(slBg.GetComponent<RectTransform>());
            slBg.AddComponent<Image>().color = new Color(0.10f, 0.12f, 0.16f, 0.95f);

            GameObject fa = CreateUI("Fill Area", slObj.transform);
            SetStretch(fa.GetComponent<RectTransform>());

            GameObject fill = CreateUI("Fill", fa.transform);
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one; fillRt.sizeDelta = Vector2.zero;
            hpFillImage = fill.AddComponent<Image>();
            hpFillImage.color = colHpFull;
            hpSlider.fillRect = fillRt;

            hpValueText = CreateTxt("Txt_HPValue", slObj.transform, "18 / 18", font, 10, Color.white, TextAnchor.MiddleCenter);
            SetStretch(hpValueText.rectTransform);

            // SP / Cores Bar
            Text spLbl = CreateTxt("Txt_SpLabel", unitCardRoot.transform, "<b>SP</b>", font, 11, new Color(1.0f, 0.84f, 0.25f, 1f), TextAnchor.MiddleLeft);
            SetRect(spLbl.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(98f, -55f), new Vector2(24f, 16f));

            GameObject spObj = CreateUI("Slider_SP", unitCardRoot.transform);
            SetRect(spObj.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(124f, -55f), new Vector2(164f, 16f));
            spSlider = spObj.AddComponent<Slider>();
            spSlider.interactable = false;

            GameObject spBg = CreateUI("Background", spObj.transform);
            SetStretch(spBg.GetComponent<RectTransform>());
            spBg.AddComponent<Image>().color = new Color(0.10f, 0.12f, 0.16f, 0.95f);

            GameObject spFa = CreateUI("Fill Area", spObj.transform);
            SetStretch(spFa.GetComponent<RectTransform>());

            GameObject spFill = CreateUI("Fill", spFa.transform);
            RectTransform spFillRt = spFill.GetComponent<RectTransform>();
            spFillRt.anchorMin = Vector2.zero; spFillRt.anchorMax = Vector2.one; spFillRt.sizeDelta = Vector2.zero;
            spFillImage = spFill.AddComponent<Image>();
            spFillImage.color = new Color(1.0f, 0.65f, 0.10f, 1f);
            spSlider.fillRect = spFillRt;

            spValueText = CreateTxt("Txt_SPValue", spObj.transform, "✦ 0 / 3 Cores", font, 10, Color.white, TextAnchor.MiddleCenter);
            SetStretch(spValueText.rectTransform);

            // 2-Column Stat Matrix (ATK & MOV, DEF & RNG)
            statMatrixCol1 = CreateTxt("Txt_StatCol1", unitCardRoot.transform, "⚔️ <b>ATK:</b> 3\n🏃 <b>MOV:</b> 3", font, 11, new Color(0.92f, 0.94f, 0.96f, 1f), TextAnchor.UpperLeft);
            statMatrixCol1.lineSpacing = 1.15f;
            SetRect(statMatrixCol1.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(98f, -74f), new Vector2(92f, 34f));

            statMatrixCol2 = CreateTxt("Txt_StatCol2", unitCardRoot.transform, "🛡️ <b>DEF:</b> Base\n🎯 <b>RNG:</b> 1 Hex", font, 11, new Color(0.92f, 0.94f, 0.96f, 1f), TextAnchor.UpperLeft);
            statMatrixCol2.lineSpacing = 1.15f;
            SetRect(statMatrixCol2.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(194f, -74f), new Vector2(94f, 34f));

            // Legacy stat text & badge support
            statsText = statMatrixCol1;

            // 4. Right Column: Tactical Equipment / Stance Slot
            GameObject equipPanel = CreateUI("Panel_Equip", unitCardRoot.transform);
            SetRect(equipPanel.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(296f, -34f), new Vector2(188f, 74f));
            Image epImg = equipPanel.AddComponent<Image>();
            epImg.sprite = panelFrame;
            epImg.type = Image.Type.Sliced;
            epImg.color = new Color(0.06f, 0.05f, 0.10f, 0.85f);

            Text txtEquipHdr = CreateTxt("Txt_EquipHdr", equipPanel.transform, "<color=#FFE082>EQUIP / STANCE</color>", font, 10, Color.white, TextAnchor.UpperLeft);
            SetRect(txtEquipHdr.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(6f, -4f), new Vector2(-12f, 14f));

            equipStanceText = CreateTxt("Txt_EquipBody", equipPanel.transform, "⚔️ Abyssal Cleave\n🛡️ Standard Stance\n🔥 Flame Attuned", font, 10, Color.white, TextAnchor.UpperLeft);
            equipStanceText.lineSpacing = 1.15f;
            SetRect(equipStanceText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(6f, -18f), new Vector2(-12f, 50f));

            // 5. Bottom Ribbon: Tactical Strip (Level, Affinity, Resistances)
            GameObject bottomRibbon = CreateUI("Ribbon_Bottom", unitCardRoot.transform);
            SetRect(bottomRibbon.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -114f), new Vector2(468f, 22f));
            Image brImg = bottomRibbon.AddComponent<Image>();
            brImg.sprite = panelFrame;
            brImg.type = Image.Type.Sliced;
            brImg.color = new Color(0.05f, 0.07f, 0.12f, 0.90f);

            bottomTacticalText = CreateTxt("Txt_BottomTactical", bottomRibbon.transform, "<b>LV 1</b>  |  <b>AFFINITY:</b> 🔥 FIRE", font, 10, new Color(0.85f, 0.88f, 0.92f, 1f), TextAnchor.MiddleLeft);
            SetRect(bottomTacticalText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(200f, 0f));

            bottomResistText = CreateTxt("Txt_BottomResist", bottomRibbon.transform, "<b>RES:</b> <color=#FF8A80>🔥 +50%</color>  <color=#80D8FF>💧 -25%</color>  <color=#A1887F>⛰️ 0%</color>", font, 10, Color.white, TextAnchor.MiddleRight);
            SetRect(bottomResistText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(250f, 0f));
        }

        private void BuildDynamicHud()
        {
            GameObject container = CreateUI("CombatHUD_Container", transform);
            SetStretch(container.GetComponent<RectTransform>());

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#if UNITY_EDITOR
            Font fontBold = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Font_Bold.ttf");
            if (fontBold != null) font = fontBold;
#endif
            Sprite panelFrame = ElementalHexTactics3D.Units.TacticalUnitSpawner.LoadUISprite("UI_Panel_Frame.png");
            Sprite windowFrameRpg = ElementalHexTactics3D.Units.TacticalUnitSpawner.LoadUISprite("UI_WindowFrame_RPG.png") ?? panelFrame;
            Sprite btnNormal = ElementalHexTactics3D.Units.TacticalUnitSpawner.LoadUISprite("UI_Button_Normal.png");

            // ================= 1. UNIT STATUS CARD (Top-Left - Disgaea Style) ================= //
            BuildDisgaeaUnitCard(container.transform, font, panelFrame, windowFrameRpg);

            // ================= 2. TURN PHASE RIBBON (Top-Center) ================= //
            turnRibbonRoot = CreateUI("Ribbon_TurnPhase", container.transform);
            SetRect(turnRibbonRoot.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(330f, 54f));
            turnRibbonBg = turnRibbonRoot.AddComponent<Image>();
            turnRibbonBg.sprite = panelFrame;
            turnRibbonBg.type = Image.Type.Sliced;
            turnRibbonBg.color = colPlayerRibbon;

            roundLabelText = CreateTxt("Txt_Round", turnRibbonRoot.transform, "ROUND 1", font, 11, new Color(0.50f, 0.87f, 0.92f, 1f), TextAnchor.UpperCenter);
            SetRect(roundLabelText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(0f, 16f));

            phaseStatusText = CreateTxt("Txt_Phase", turnRibbonRoot.transform, "⚔️ PLAYER PHASE", font, 16, Color.white, TextAnchor.LowerCenter);
            SetRect(phaseStatusText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(0f, 24f));

            // ================= 3. CITADEL GATEWAY & RIFT PANEL (Right-Side Command Dock) ================= //
            actionBarRoot = CreateUI("Bar_CombatActions", container.transform);
            RectTransform actionRt = actionBarRoot.GetComponent<RectTransform>();
            SetRect(actionRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -72f), new Vector2(175f, 120f));
            ApplyRpgMakerWindowStyle(actionBarRoot, new Color(0.07f, 0.06f, 0.12f, 0.96f));

            VerticalLayoutGroup actVl = actionBarRoot.AddComponent<VerticalLayoutGroup>();
            actVl.padding = new RectOffset(8, 8, 8, 8);
            actVl.spacing = 4f;
            actVl.childAlignment = TextAnchor.UpperCenter;
            actVl.childControlWidth = true;
            actVl.childControlHeight = false;
            actVl.childForceExpandWidth = true;
            actVl.childForceExpandHeight = false;

            ContentSizeFitter actCsf = actionBarRoot.AddComponent<ContentSizeFitter>();
            actCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            actCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Direct Children in Right-Side Command Dock
            txtUnanchoredHeader = CreateTxt("Txt_UnanchoredHeader", actionBarRoot.transform, "🌀 <b>CITADEL GATEWAY</b>", font, 11, new Color(0.85f, 0.65f, 1.0f, 1f), TextAnchor.MiddleCenter);
            txtUnanchoredHeader.fontStyle = FontStyle.Bold;
            LayoutElement leH1 = txtUnanchoredHeader.gameObject.AddComponent<LayoutElement>();
            leH1.preferredHeight = 22f;
            leH1.minWidth = 155f;

            btnTearRift = CreateBtn("Btn_TearRift", actionBarRoot.transform, "🌀 <b>Tear Open Rift</b>", font, 11, btnNormal, new Color(0.42f, 0.16f, 0.65f, 1f), 155f, 34f);
            txtTearRift = btnTearRift.GetComponentInChildren<Text>();
            LayoutElement leTR = btnTearRift.GetComponent<LayoutElement>() ?? btnTearRift.gameObject.AddComponent<LayoutElement>();
            leTR.preferredHeight = 34f;
            leTR.minHeight = 34f;
            leTR.preferredWidth = 155f;
            leTR.minWidth = 0f;

            btnRandomizeMap = CreateBtn("Btn_RandomizeMap", actionBarRoot.transform, "🎲 <b>Roll Map [R]</b>", font, 10, btnNormal, new Color(0.20f, 0.28f, 0.40f, 1f), 155f, 30f);
            LayoutElement leRM = btnRandomizeMap.GetComponent<LayoutElement>() ?? btnRandomizeMap.gameObject.AddComponent<LayoutElement>();
            leRM.preferredHeight = 30f;
            leRM.minHeight = 30f;
            leRM.preferredWidth = 155f;
            leRM.minWidth = 0f;

            txtRiftHeader = CreateTxt("Txt_RiftHeader", actionBarRoot.transform, "🌀 <b>CITADEL RIFT</b>", font, 11, new Color(0.85f, 0.65f, 1.0f, 1f), TextAnchor.MiddleCenter);
            txtRiftHeader.fontStyle = FontStyle.Bold;
            LayoutElement leH2 = txtRiftHeader.gameObject.AddComponent<LayoutElement>();
            leH2.preferredHeight = 22f;
            leH2.minWidth = 155f;

            btnOpenRiftDispatch = CreateBtn("Btn_OpenRiftDispatch", actionBarRoot.transform, "📦 <b>Squad Dispatch</b>", font, 11, btnNormal, new Color(0.40f, 0.15f, 0.65f, 1f), 155f, 34f);
            txtOpenRiftDispatch = btnOpenRiftDispatch.GetComponentInChildren<Text>();
            LayoutElement leRD = btnOpenRiftDispatch.GetComponent<LayoutElement>() ?? btnOpenRiftDispatch.gameObject.AddComponent<LayoutElement>();
            leRD.preferredHeight = 34f;
            leRD.minHeight = 34f;
            leRD.preferredWidth = 155f;
            leRD.minWidth = 0f;

            unanchoredSection = null;
            riftPanelSection = null;
            unitAbilitiesSection = null;

            // Universal End Turn Button (Dedicated Bottom-Right Widget)
            btnEndTurn = CreateBtn("Btn_EndTurn", container.transform, "<b>⏳ END TURN</b>\n<size=11><color=#CFD8DC>[Space / Click]</color></size>", font, 13, btnNormal, new Color(0.72f, 0.20f, 0.16f, 0.95f), 180f, 56f);
            SetRect(btnEndTurn.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 24f), new Vector2(180f, 56f));
            txtEndTurn = btnEndTurn.GetComponentInChildren<Text>();

            // ================= 4. DISGAEA-STYLE PANELS ================= //
            BuildDisgaeaDispatchModal(container.transform, font, windowFrameRpg, btnNormal);
            BuildDisgaeaCommandMenu(container.transform, font, windowFrameRpg, btnNormal);
        }

        private GameObject CreateUI(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private void SetStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        private void SetRect(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private Text CreateTxt(string name, Transform parent, string content, Font font, int size, Color col, TextAnchor align)
        {
            GameObject o = CreateUI(name, parent);
            Text t = o.AddComponent<Text>();
            t.text = content;
            t.font = font;
            t.fontSize = size;
            t.color = col;
            t.alignment = align;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;

            Shadow s = o.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.9f);
            s.effectDistance = new Vector2(1f, -1f);
            return t;
        }

        private Button CreateBtn(string name, Transform parent, string label, Font font, int fontSize, Sprite bg, Color tint, float w, float h)
        {
            GameObject o = CreateUI(name, parent);
            RectTransform rt = o.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(w, h);

            LayoutElement le = o.AddComponent<LayoutElement>();
            le.preferredWidth = w;
            le.preferredHeight = h;
            le.minWidth = w;
            le.minHeight = h;

            Image img = o.AddComponent<Image>();
            img.sprite = bg;
            img.type = Image.Type.Simple;
            img.color = tint;

            Button btn = o.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            cb.disabledColor = new Color(0.35f, 0.35f, 0.38f, 0.65f);
            btn.colors = cb;

            GameObject to = CreateUI("Text", o.transform);
            SetStretch(to.GetComponent<RectTransform>());
            Text txt = to.AddComponent<Text>();
            txt.text = label;
            txt.font = font;
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;

            Shadow s = to.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.95f);
            s.effectDistance = new Vector2(1f, -1f);

            return btn;
        }

        private void WireButtonListeners()
        {
            // Unanchored
            if (btnTearRift != null) btnTearRift.onClick.AddListener(OnTearRiftClicked);
            if (btnRandomizeMap != null) btnRandomizeMap.onClick.AddListener(OnRandomizeMapClicked);

            // Unit Actions
            if (btnMove != null) btnMove.onClick.AddListener(() => OnActionClicked(UnitActionMode.Move));
            if (btnFireball != null) btnFireball.onClick.AddListener(() => OnActionClicked(UnitActionMode.Fireball));
            if (btnWaterSurge != null) btnWaterSurge.onClick.AddListener(() => OnActionClicked(UnitActionMode.WaterSurge));
            if (btnEarthPillar != null) btnEarthPillar.onClick.AddListener(() => OnActionClicked(UnitActionMode.EarthSpire));
            if (btnPush != null) btnPush.onClick.AddListener(() => OnActionClicked(UnitActionMode.KineticPush));
            if (btnHarvestCore != null) btnHarvestCore.onClick.AddListener(() => OnActionClicked(UnitActionMode.ConsumeLand));
            if (btnTitanStrike != null) btnTitanStrike.onClick.AddListener(() => OnActionClicked(UnitActionMode.TitanStrike));
            if (btnMagmaCataclysm != null) btnMagmaCataclysm.onClick.AddListener(() => OnActionClicked(UnitActionMode.MagmaCataclysm));
            if (btnSummonTitanAbility != null) btnSummonTitanAbility.onClick.AddListener(() => OnActionClicked(UnitActionMode.SummonTitan));

            if (btnRecall != null) btnRecall.onClick.AddListener(OnRecallClicked);

            // Rift / Base Panel (Unified Dispatch Trigger)
            if (btnOpenRiftDispatch != null) btnOpenRiftDispatch.onClick.AddListener(OnOpenRiftDispatchClicked);
            if (btnDeployCommander != null) btnDeployCommander.onClick.AddListener(OnOpenRiftDispatchClicked);
            if (btnSummonTitanRift != null) btnSummonTitanRift.onClick.AddListener(OnOpenRiftDispatchClicked);

            // Universal End Turn
            if (btnEndTurn != null) btnEndTurn.onClick.AddListener(OnEndTurnClicked);

            // Disgaea Command Menu
            if (btnCmdMove != null) btnCmdMove.onClick.AddListener(() => OnActionClicked(UnitActionMode.Move));
            if (btnCmdAttack != null) btnCmdAttack.onClick.AddListener(() => OnActionClicked(UnitActionMode.TitanStrike));
            if (btnCmdSpecial != null) btnCmdSpecial.onClick.AddListener(OnCmdSpecialClicked);
            if (btnCmdDefend != null) btnCmdDefend.onClick.AddListener(OnCmdDefendClicked);
            if (btnCmdPush != null) btnCmdPush.onClick.AddListener(() => OnActionClicked(UnitActionMode.KineticPush));
            if (btnCmdAbsorb != null) btnCmdAbsorb.onClick.AddListener(() => OnActionClicked(UnitActionMode.ConsumeLand));
            if (btnCmdItem != null) btnCmdItem.onClick.AddListener(OnCmdItemClicked);
            if (btnCmdWait != null) btnCmdWait.onClick.AddListener(OnCmdWaitClicked);
            if (btnCmdCancel != null) btnCmdCancel.onClick.AddListener(OnCmdCancelClicked);

            // Disgaea Dispatch Modal
            if (btnCloseDispatch != null) btnCloseDispatch.onClick.AddListener(OnCloseDispatchClicked);
        }

        private void OnTearRiftClicked()
        {
            SoundManager3D.Instance?.PlayButtonClick();
            HexGridInteraction3D interaction = HexGridInteraction3D.Instance;
            if (interaction == null) return;

            interaction.SetActionMode(interaction.CurrentActionMode == UnitActionMode.TearRift
                ? UnitActionMode.None
                : UnitActionMode.TearRift);
        }

        private void OnRandomizeMapClicked()
        {
            SoundManager3D.Instance?.PlayButtonClick();
            HexGridInteraction3D interaction = HexGridInteraction3D.Instance;
            if (interaction != null)
            {
                interaction.DeselectAll();
            }
            Grid.HexGrid3D.Instance?.GenerateRandomizedBattlefield();
        }

        private void OnActionClicked(UnitActionMode mode)
        {
            SoundManager3D.Instance?.PlayButtonClick();
            HexGridInteraction3D interaction = HexGridInteraction3D.Instance;
            if (interaction == null) return;

            if (interaction.CurrentActionMode == mode)
            {
                interaction.SetActionMode(UnitActionMode.None);
            }
            else
            {
                interaction.SetActionMode(mode);
                if (commandMenuRoot != null) commandMenuRoot.SetActive(false);
            }
        }

        private void OnRecallClicked()
        {
            SoundManager3D.Instance?.PlayButtonClick();
            HexGridInteraction3D interaction = HexGridInteraction3D.Instance;
            if (interaction == null) return;

            TacticalUnit3D sel = interaction.CurrentSelectedUnit;
            if (sel != null && AbyssalRiftConduit3D.Instance != null)
            {
                interaction.ClearUnitSelection();
                interaction.SetActionMode(UnitActionMode.None);
                StartCoroutine(AbyssalRiftConduit3D.Instance.RecallUnitRoutine(sel));
            }
        }

        private void OnOpenRiftDispatchClicked()
        {
            SoundManager3D.Instance?.PlayButtonClick();
            HexGridInteraction3D interaction = HexGridInteraction3D.Instance;
            if (interaction == null) return;
            if (AbyssalRiftConduit3D.Instance != null && AbyssalRiftConduit3D.Instance.RiftTile != null)
            {
                interaction.ClearUnitSelection();
                interaction.SelectTile(AbyssalRiftConduit3D.Instance.RiftTile);
                interaction.SetActionMode(UnitActionMode.None);
            }
        }

        private void OnDeployCommanderClicked()
        {
            SoundManager3D.Instance?.PlayButtonClick();
            HexGridInteraction3D interaction = HexGridInteraction3D.Instance;
            if (interaction == null) return;

            TacticalUnit3D cmdr = HexGridInteraction3D.FindCommanderUnit();
            if (cmdr == null && Tutorial.TutorialScenarioManager.Instance != null)
            {
                cmdr = Tutorial.TutorialScenarioManager.Instance.DemonLordPlayer;
            }

            if (cmdr != null && !cmdr.gameObject.activeInHierarchy)
            {
                interaction.PendingDeployUnit = cmdr;
                interaction.SetActionMode(interaction.CurrentActionMode == UnitActionMode.DeployCommander 
                    ? UnitActionMode.None 
                    : UnitActionMode.DeployCommander);
            }
            else if (cmdr != null && cmdr.gameObject.activeInHierarchy)
            {
                interaction.SelectTile(cmdr.CurrentTile);
                interaction.SelectUnit(cmdr);
            }
        }

        private void OnSummonTitanRiftClicked()
        {
            SoundManager3D.Instance?.PlayButtonClick();
            HexGridInteraction3D interaction = HexGridInteraction3D.Instance;
            if (interaction == null) return;

            TacticalUnit3D titan = HexGridInteraction3D.FindTitanUnit();
            if (titan == null && Tutorial.TutorialScenarioManager.Instance != null)
            {
                titan = Tutorial.TutorialScenarioManager.Instance.MagmaDragonPlayer;
            }

            if (titan != null && !titan.gameObject.activeInHierarchy)
            {
                interaction.PendingDeployUnit = titan;
                interaction.SetActionMode(interaction.CurrentActionMode == UnitActionMode.SummonTitan 
                    ? UnitActionMode.None 
                    : UnitActionMode.SummonTitan);
            }
            else if (titan != null && titan.gameObject.activeInHierarchy)
            {
                interaction.SelectTile(titan.CurrentTile);
                interaction.SelectUnit(titan);
            }
        }

        private void OnEndTurnClicked()
        {
            SoundManager3D.Instance?.PlayButtonClick();
            HexGridInteraction3D interaction = HexGridInteraction3D.Instance;
            if (interaction != null)
            {
                interaction.DeselectAll();
                interaction.SetActionMode(UnitActionMode.None);
            }
            if (commandMenuRoot != null) commandMenuRoot.SetActive(false);
            CloseAllSubmenus();

            if (TurnManager3D.Instance != null)
            {
                TurnManager3D.Instance.EndPlayerTurn();
            }
        }

        private void Update()
        {
            if (StoryDialogueUI.Instance != null && StoryDialogueUI.Instance.IsPlayingDialogue)
            {
                SetHudVisible(false);
                return;
            }

            SetHudVisible(true);
            UpdateTurnPhaseRibbon();
            UpdateUnitStatusCard();
            UpdateBottomActionBar();
            UpdateEndTurnButton();
        }

        private void UpdateEndTurnButton()
        {
            if (btnEndTurn == null) return;

            TurnManager3D turn = TurnManager3D.Instance;
            bool isPlayerTurn = (turn != null && turn.CurrentPhase == TurnPhase.PlayerTurn && turn.IsPlayerTurn);

            if (!isPlayerTurn)
            {
                if (btnEndTurn.gameObject.activeSelf) btnEndTurn.gameObject.SetActive(false);
                return;
            }

            if (!btnEndTurn.gameObject.activeSelf) btnEndTurn.gameObject.SetActive(true);

            // Check if all living active player units on field have acted
            bool allUnitsActed = true;
            int activePlayerCount = 0;
            TacticalUnit3D[] units = FindObjectsByType<TacticalUnit3D>();
            for (int i = 0; i < units.Length; i++)
            {
                TacticalUnit3D u = units[i];
                if (u != null && u.Faction == UnitFaction.Player && u.gameObject.activeInHierarchy && u.CurrentHealth > 0)
                {
                    activePlayerCount++;
                    if (!u.HasActedThisTurn)
                    {
                        allUnitsActed = false;
                    }
                }
            }

            if (txtEndTurn == null) txtEndTurn = btnEndTurn.GetComponentInChildren<Text>();

            ColorBlock cb = btnEndTurn.colors;
            if (allUnitsActed && activePlayerCount > 0)
            {
                if (txtEndTurn != null)
                {
                    txtEndTurn.text = "<b>⚡ END TURN</b>\n<size=11><color=#69F0AE>(All Units Acted)</color></size>";
                }
                float pulse = (Mathf.Sin(Time.unscaledTime * 5f) + 1f) * 0.5f;
                cb.normalColor = Color.Lerp(new Color(0.85f, 0.25f, 0.20f, 1f), new Color(0.20f, 0.75f, 0.40f, 1f), pulse);
            }
            else
            {
                if (txtEndTurn != null)
                {
                    txtEndTurn.text = "<b>⏳ END TURN</b>\n<size=11><color=#CFD8DC>[Space / Click]</color></size>";
                }
                cb.normalColor = new Color(0.72f, 0.20f, 0.16f, 1f);
            }
            btnEndTurn.colors = cb;
            btnEndTurn.interactable = true;

            // Spacebar shortcut to end turn
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && 
                UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                TriggerSpaceEndTurn();
            }
#else
            if (Input.GetKeyDown(KeyCode.Space))
            {
                TriggerSpaceEndTurn();
            }
#endif
        }

        private void TriggerSpaceEndTurn()
        {
            HexGridInteraction3D interaction = HexGridInteraction3D.Instance;
            bool isTargeting = interaction != null && interaction.CurrentActionMode != UnitActionMode.None;
            bool isModalOpen = (dispatchModalRoot != null && dispatchModalRoot.activeSelf);
            if (!isTargeting && !isModalOpen)
            {
                OnEndTurnClicked();
            }
        }

        // ================= UNIT STATUS CARD ================= //

        private void UpdateUnitStatusCard()
        {
            HexGridInteraction3D interaction = HexGridInteraction3D.Instance;
            TacticalUnit3D selected = interaction != null ? interaction.CurrentSelectedUnit : null;

            if (selected == null || selected.CurrentHealth <= 0)
            {
                if (unitCardRoot != null && unitCardRoot.activeSelf)
                {
                    unitCardRoot.SetActive(false);
                }
                return;
            }

            if (unitCardRoot != null && !unitCardRoot.activeSelf)
            {
                unitCardRoot.SetActive(true);
            }

            // Portrait Avatar
            if (portraitImage != null)
            {
                portraitImage.sprite = selected.PortraitSprite;
                portraitImage.color = (selected.PortraitSprite != null) ? Color.white : new Color(1f, 1f, 1f, 0f);
            }

            // Disgaea Header: Unit Name & Archetype (e.g. "Demon Lord / Commander")
            string factionColor = selected.Faction == UnitFaction.Player ? "#69F0AE" : "#FF5252";
            string archetypeStr = selected.Archetype.ToString();
            if (unitNameText != null)
            {
                unitNameText.text = $"<b>{selected.UnitName}</b> <size=12><color=#90A4AE>/ {archetypeStr}</color></size>";
            }
            if (unitArchetypeText != null)
            {
                unitArchetypeText.text = $"<color={factionColor}>{(selected.Faction == UnitFaction.Player ? "Vanguard" : "Invader")}</color>";
            }

            // Turn Readiness Badge
            if (turnStatusText != null)
            {
                if (selected.HasActedThisTurn)
                {
                    turnStatusText.text = "<color=#90A4AE>⏳ ACTED</color>";
                }
                else
                {
                    turnStatusText.text = "<color=#69F0AE>✓ READY</color>";
                }
            }

            // Health Bar
            float hpRatio = selected.MaxHealth > 0 ? (float)selected.CurrentHealth / selected.MaxHealth : 0f;
            if (hpSlider != null) hpSlider.value = hpRatio;
            if (hpValueText != null) hpValueText.text = $"{selected.CurrentHealth} / {selected.MaxHealth}";

            if (hpFillImage != null)
            {
                if (hpRatio > 0.50f) hpFillImage.color = colHpFull;
                else if (hpRatio > 0.25f) hpFillImage.color = colHpMid;
                else hpFillImage.color = colHpLow;
            }

            // SP / Mana & Cores Bar
            if (selected.MaxHealth >= 500)
            {
                int maxMp = Mathf.Max(1, selected.MaxMana);
                int curMp = selected.CurrentMana;
                float mpRatio = Mathf.Clamp01((float)curMp / maxMp);
                if (spSlider != null) spSlider.value = mpRatio;
                if (spFillImage != null) spFillImage.color = new Color(0.20f, 0.65f, 0.95f, 1f);
                if (spValueText != null) spValueText.text = $"💧 {curMp} / {maxMp} MP";
            }
            else
            {
                int maxCores = 3;
                int curCores = selected.ElementalCores;
                int maxMp = Mathf.Max(1, selected.MaxMana);
                int curMp = selected.CurrentMana;
                float ratio = curCores > 0 ? Mathf.Clamp01((float)curCores / maxCores) : Mathf.Clamp01((float)curMp / maxMp);
                if (spSlider != null) spSlider.value = ratio;
                if (spValueText != null)
                {
                    spValueText.text = $"💧 {curMp}/{maxMp} MP  •  ✦ {curCores}/{maxCores} Cores";
                }
            }

            // 2-Column Stat Matrix (ATK & MOV on left, DEF & RNG on right)
            int atk = selected.EffectiveAttackDamage;
            int mov = selected.EffectiveMoveRange;
            int rng = selected.AttackRange;
            string defDisplay = selected.IsDefending ? "<color=#69F0AE>GUARD (-50%)</color>" : "BASE (0%)";

            if (statMatrixCol1 != null)
            {
                statMatrixCol1.text = $"⚔️ <b>ATK:</b> {atk}\n🏃 <b>MOV:</b> {mov}";
            }
            if (statMatrixCol2 != null)
            {
                statMatrixCol2.text = $"🛡️ <b>DEF:</b> {defDisplay}\n🎯 <b>RNG:</b> {rng} Hex";
            }
            if (statsText != null)
            {
                statsText.text = $"⚔️ ATK: {atk}    🏃 MOV: {mov}";
            }

            // Tactical Equipment / Stance Slot
            if (equipStanceText != null)
            {
                string weaponStr = selected.IsRangedAttack ? "Sacred Volley" : (selected.UnitName.Contains("Paladin") ? "Excalibur Strike" : (selected.UnitName.Contains("Shielder") ? "Sacred Aegis" : (selected.Archetype == UnitArchetype.Commander ? "Abyssal Cleave" : "Blade Strike")));
                string stanceStr = selected.IsDefending ? "<color=#69F0AE>🛡️ Guard Stance</color>" : "<color=#B0BEC5>⚔️ Standard Stance</color>";

                string attuneStr;
                switch (selected.Affinity)
                {
                    case ElementalAffinity.Fire:
                        attuneStr = "<color=#FF8A80>🔥 Flame Attuned (+1)</color>";
                        break;
                    case ElementalAffinity.Water:
                        attuneStr = "<color=#80D8FF>💧 Wave Attuned (+1)</color>";
                        break;
                    default:
                        attuneStr = "<color=#CFD8DC>⚪ Neutral Attunement</color>";
                        break;
                }
                if (!string.IsNullOrEmpty(selected.CurrentAttunementName) && selected.CurrentAttunementName != "None")
                {
                    attuneStr = $"<color=#FFE082>✨ {selected.CurrentAttunementName}</color>";
                }

                equipStanceText.text = $"{weaponStr}\n{stanceStr}\n{attuneStr}";
            }

            // Bottom Tactical Strip: Level, Affinity & Resistances
            if (bottomTacticalText != null)
            {
                string elemStr = selected.Affinity == ElementalAffinity.Fire ? "🔥 FIRE" : (selected.Affinity == ElementalAffinity.Water ? "💧 WATER" : "⛰️ EARTH");
                string lvStr = selected.MaxHealth >= 500 ? "<color=#FFD54F>LV 99</color>" : "LV 1";
                bottomTacticalText.text = $"<b>{lvStr}</b>  |  <b>AFFINITY:</b> {elemStr}";
            }

            if (bottomResistText != null)
            {
                string resistStr;
                switch (selected.Affinity)
                {
                    case ElementalAffinity.Fire:
                        resistStr = "<b>RES:</b> <color=#FF8A80>🔥 +50%</color>  <color=#80D8FF>💧 -25%</color>  <color=#A1887F>⛰️ 0%</color>";
                        break;
                    case ElementalAffinity.Water:
                        resistStr = "<b>RES:</b> <color=#FF8A80>🔥 -25%</color>  <color=#80D8FF>💧 +50%</color>  <color=#A1887F>⛰️ 0%</color>";
                        break;
                    default:
                        resistStr = "<b>RES:</b> <color=#FF8A80>🔥 0%</color>  <color=#80D8FF>💧 0%</color>  <color=#A1887F>⛰️ 0%</color>";
                        break;
                }
                bottomResistText.text = resistStr;
            }

            // Legacy attunement badge and cores text
            if (attunementBadgeText != null)
            {
                switch (selected.Affinity)
                {
                    case ElementalAffinity.Fire:
                        attunementBadgeText.text = "🔥 Flame Attuned";
                        if (attunementBadgeBg != null) attunementBadgeBg.color = new Color(0.70f, 0.20f, 0.05f, 0.85f);
                        break;
                    case ElementalAffinity.Water:
                        attunementBadgeText.text = "💧 Wave Attuned";
                        if (attunementBadgeBg != null) attunementBadgeBg.color = new Color(0.05f, 0.35f, 0.65f, 0.85f);
                        break;
                    default:
                        attunementBadgeText.text = "⚪ Neutral";
                        if (attunementBadgeBg != null) attunementBadgeBg.color = new Color(0.25f, 0.25f, 0.28f, 0.85f);
                        break;
                }
            }
            if (elementalCoresText != null)
            {
                if (selected.Archetype == UnitArchetype.Commander)
                {
                    elementalCoresText.gameObject.SetActive(true);
                    elementalCoresText.text = $"✦ {selected.ElementalCores} Cores";
                }
                else
                {
                    elementalCoresText.gameObject.SetActive(false);
                }
            }
        }

        // ================= TURN PHASE RIBBON ================= //

        private void UpdateTurnPhaseRibbon()
        {
            if (turnRibbonRoot == null) return;

            TurnManager3D turn = TurnManager3D.Instance;
            if (turn == null) return;

            if (roundLabelText != null)
            {
                roundLabelText.text = $"ROUND {turn.CurrentRound}";
            }

            bool isPlayer = turn.CurrentPhase == TurnPhase.PlayerTurn;
            if (phaseStatusText != null)
            {
                phaseStatusText.text = isPlayer ? "⚔️ PLAYER PHASE" : "⚠️ ENEMY PHASE";
            }

            if (turnRibbonBg != null)
            {
                turnRibbonBg.color = isPlayer ? colPlayerRibbon : colEnemyRibbon;
            }
        }

        // ================= BOTTOM ACTION BAR ================= //

        private void UpdateBottomActionBar()
        {
            if (actionBarRoot == null) return;

            TurnManager3D turn = TurnManager3D.Instance;
            bool isPlayerTurn = (turn != null && turn.CurrentPhase == TurnPhase.PlayerTurn);

            HexGridInteraction3D interaction = HexGridInteraction3D.Instance;
            if (interaction == null) return;

            TacticalUnit3D selectedUnit = interaction.CurrentSelectedUnit;
            UnitActionMode currentMode = interaction.CurrentActionMode;

            bool isNoRiftStage = Tutorial.TutorialScenarioManager.Instance != null && 
                (Tutorial.TutorialScenarioManager.Instance.IsStage1Active || Tutorial.TutorialScenarioManager.Instance.IsStage2Active);

            // PERMANENTLY RETIRED: Old horizontal action buttons are completely disabled in favor of Disgaea Command Menu!
            if (unitAbilitiesSection != null && unitAbilitiesSection.activeSelf)
            {
                unitAbilitiesSection.SetActive(false);
            }

            // Determine if Unanchored Rift or Base Panel (Rift) should show
            bool riftExists = (AbyssalRiftConduit3D.Instance != null && AbyssalRiftConduit3D.Instance.RiftTile != null);
            bool isRiftSelected = (interaction.CurrentSelectedTile != null && riftExists && AbyssalRiftConduit3D.Instance.RiftTile == interaction.CurrentSelectedTile);
            bool isDeployMode = (currentMode == UnitActionMode.DeployCommander || currentMode == UnitActionMode.SummonTitan);
            bool noActiveUnits = !HexGridInteraction3D.HasActivePlayerUnitsOnField();

            bool isPlayerUnitSelected = (selectedUnit != null && selectedUnit.Faction == UnitFaction.Player);

            if (isPlayerUnitSelected)
            {
                // Disgaea TRPG Mode: When a player unit is selected, show ONLY the Disgaea Command Menu!
                // Hide bottom action bar and old unit ability buttons to eliminate double UI clutter.
                if (actionBarRoot != null && actionBarRoot.activeSelf) actionBarRoot.SetActive(false);
                if (unanchoredSection != null) unanchoredSection.SetActive(false);
                if (riftPanelSection != null) riftPanelSection.SetActive(false);
                if (dispatchModalRoot != null && dispatchModalRoot.activeSelf) dispatchModalRoot.SetActive(false);

                UpdateDisgaeaCommandMenu(selectedUnit, interaction, isPlayerTurn, currentMode);
            }
            else
            {
                // No unit selected: Close Disgaea Command Menu and show Hex Tile & Coordinate Inspector at bottom!
                if (commandMenuRoot != null && commandMenuRoot.activeSelf) commandMenuRoot.SetActive(false);
                CloseAllSubmenus();
                ShowHexTileInspector(interaction);

                if (isNoRiftStage)
                {
                    // Stage 1 & Stage 2: All units deployed on field, no rift mechanics needed
                    if (actionBarRoot != null && actionBarRoot.activeSelf) actionBarRoot.SetActive(false);
                    if (txtUnanchoredHeader != null) txtUnanchoredHeader.gameObject.SetActive(false);
                    if (btnTearRift != null) btnTearRift.gameObject.SetActive(false);
                    if (btnRandomizeMap != null) btnRandomizeMap.gameObject.SetActive(false);
                    if (txtRiftHeader != null) txtRiftHeader.gameObject.SetActive(false);
                    if (btnOpenRiftDispatch != null) btnOpenRiftDispatch.gameObject.SetActive(false);
                    if (dispatchModalRoot != null) dispatchModalRoot.SetActive(false);
                }
                else if (!riftExists)
                {
                    // Incursion / Rift mission before placing rift: Show Tear Open Rift on the Right Dock
                    if (actionBarRoot != null && !actionBarRoot.activeSelf) actionBarRoot.SetActive(true);
                    if (txtUnanchoredHeader != null) txtUnanchoredHeader.gameObject.SetActive(true);
                    if (txtRiftHeader != null) txtRiftHeader.gameObject.SetActive(false);
                    if (btnOpenRiftDispatch != null) btnOpenRiftDispatch.gameObject.SetActive(false);
                    if (dispatchModalRoot != null) dispatchModalRoot.SetActive(false);

                    if (btnTearRift != null)
                    {
                        btnTearRift.gameObject.SetActive(true);
                        btnTearRift.interactable = isPlayerTurn;
                        SetButtonActiveHighlight(btnTearRift, currentMode == UnitActionMode.TearRift);
                        if (txtTearRift != null)
                        {
                            txtTearRift.text = (currentMode == UnitActionMode.TearRift)
                                ? "<b>[Targeting Entry]</b>\n<size=9><color=#FFD54F>(Click Hex / Cancel)</color></size>"
                                : "🌀 <b>Tear Open Rift</b>";
                        }
                    }
                    if (btnRandomizeMap != null)
                    {
                        bool isTut = Tutorial.TutorialScenarioManager.Instance != null && Tutorial.TutorialScenarioManager.Instance.IsTutorialActive;
                        btnRandomizeMap.gameObject.SetActive(!isTut);
                        btnRandomizeMap.interactable = isPlayerTurn && !isTut;
                    }

                    if (actionBarRoot != null)
                    {
                        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(actionBarRoot.GetComponent<RectTransform>());
                    }
                }
                else
                {
                    // Rift is placed on the battlefield: Show Rift Base Panel on Right Dock
                    if (txtUnanchoredHeader != null) txtUnanchoredHeader.gameObject.SetActive(false);
                    if (btnTearRift != null) btnTearRift.gameObject.SetActive(false);
                    if (btnRandomizeMap != null) btnRandomizeMap.gameObject.SetActive(false);

                    bool isRiftDispatchActive = (isRiftSelected && !isDeployMode);
                    if (isRiftDispatchActive)
                    {
                        UpdateDisgaeaDispatchModal(interaction);
                        // Clean Disgaea TRPG UX: Hide redundant right dock while Squad Dispatch modal is showing
                        if (actionBarRoot != null && actionBarRoot.activeSelf) actionBarRoot.SetActive(false);
                    }
                    else
                    {
                        if (dispatchModalRoot != null && dispatchModalRoot.activeSelf && !isDeployMode)
                        {
                            dispatchModalRoot.SetActive(false);
                        }
                        if (actionBarRoot != null && !actionBarRoot.activeSelf) actionBarRoot.SetActive(true);
                        if (txtRiftHeader != null) txtRiftHeader.gameObject.SetActive(true);
                        if (btnOpenRiftDispatch != null)
                        {
                            btnOpenRiftDispatch.gameObject.SetActive(true);
                            btnOpenRiftDispatch.interactable = isPlayerTurn;
                        }
                    }
                }

                // End Turn Button
                if (btnEndTurn != null)
                {
                    btnEndTurn.interactable = isPlayerTurn;
                }
            }

            // Always apply tutorial training wheels & pulsing highlights across active buttons!
            ApplyTutorialButtonPulse();
        }

        private void UpdateRiftPanelButtons(HexGridInteraction3D interaction, bool isPlayerTurn, bool noActiveUnits, UnitActionMode currentMode)
        {
            if (btnOpenRiftDispatch == null && riftPanelSection != null)
            {
                btnOpenRiftDispatch = riftPanelSection.transform.Find("Btn_OpenRiftDispatch")?.GetComponent<Button>();
                txtOpenRiftDispatch = btnOpenRiftDispatch?.GetComponentInChildren<Text>();
            }

            // Clean Disgaea TRPG UX: Deactivate legacy duplicate buttons
            if (btnDeployCommander != null) btnDeployCommander.gameObject.SetActive(false);
            if (btnSummonTitanRift != null) btnSummonTitanRift.gameObject.SetActive(false);

            if (btnOpenRiftDispatch != null)
            {
                btnOpenRiftDispatch.gameObject.SetActive(true);
                btnOpenRiftDispatch.interactable = isPlayerTurn;
                if (txtOpenRiftDispatch != null)
                {
                    txtOpenRiftDispatch.text = "🌀 <b>CITADEL RIFT (Base Panel)</b>\n<size=11>[Click to Open Squad Dispatch]</size>";
                }
            }
        }

        private void UpdateUnitAbilityButtons(TacticalUnit3D unit, HexGridInteraction3D interaction, bool isPlayerTurn, UnitActionMode currentMode)
        {
            bool canCombat = isPlayerTurn && !unit.HasActedThisTurn;
            bool isCommander = (unit.Archetype == UnitArchetype.Commander);
            bool isTitan = (unit.Archetype == UnitArchetype.Titan);
            bool isTutorial1 = Tutorial.TutorialScenarioManager.Instance != null && Tutorial.TutorialScenarioManager.Instance.IsStage1Active;

            // Move
            if (btnMove != null)
            {
                btnMove.interactable = isPlayerTurn;
                SetButtonActiveHighlight(btnMove, currentMode == UnitActionMode.Move);
            }

            // Elemental Spells: Fireball for Fire Commander / Demon Lord, Radiant Smite for Paladin!
            bool isFireUnit = isCommander || (unit.Affinity == ElementalAffinity.Fire);
            bool isPaladin = unit.UnitName.Contains("Paladin");
            bool showFireball = isFireUnit && (!isTutorial1 || isPaladin);
            bool showWaterSurge = !isCommander && (unit.Affinity == ElementalAffinity.Water) && !isTutorial1;
            bool showEarthPillar = !isCommander && (unit.Affinity == ElementalAffinity.Earth) && !isTutorial1;

            if (btnFireball != null)
            {
                btnFireball.gameObject.SetActive(showFireball);
                btnFireball.interactable = canCombat;
                SetButtonActiveHighlight(btnFireball, currentMode == UnitActionMode.Fireball);
                if (txtFireball != null)
                {
                    if (isPaladin) txtFireball.text = "☀️ Radiant Smite\n<size=10>(Holy Fire)</size>";
                    else txtFireball.text = "🔥 Fireball\n<size=10>(3 Dmg)</size>";
                }
            }

            if (btnWaterSurge != null)
            {
                btnWaterSurge.gameObject.SetActive(showWaterSurge);
                btnWaterSurge.interactable = canCombat;
                SetButtonActiveHighlight(btnWaterSurge, currentMode == UnitActionMode.WaterSurge);
            }

            if (btnEarthPillar != null)
            {
                btnEarthPillar.gameObject.SetActive(showEarthPillar);
                btnEarthPillar.interactable = canCombat;
                SetButtonActiveHighlight(btnEarthPillar, currentMode == UnitActionMode.EarthSpire);
            }

            // Push / Tail Shove / Holy Repel
            if (btnPush != null)
            {
                btnPush.gameObject.SetActive(true);
                btnPush.interactable = canCombat;
                SetButtonActiveHighlight(btnPush, currentMode == UnitActionMode.KineticPush);
                if (txtPush != null)
                {
                    if (unit.UnitName.Contains("Paladin")) txtPush.text = "⚜️ Holy Repel\n<size=10>(Push 1)</size>";
                    else if (unit.UnitName.Contains("Shielder")) txtPush.text = "🛡️ Shield Shove\n<size=10>(Push 1)</size>";
                    else if (unit.UnitName.Contains("Basalt") || unit.UnitName.Contains("Golem")) txtPush.text = "💥 Golem Slam\n<size=10>(Push 1)</size>";
                    else if (isTitan) txtPush.text = "🐊 Tail Shove\n<size=10>(Push 1)</size>";
                    else txtPush.text = "💨 Push\n<size=10>(Push 1)</size>";
                }
            }

            // Harvest Core (Siphon - disabled in Holy Crusade)
            if (btnHarvestCore != null)
            {
                btnHarvestCore.gameObject.SetActive(!isTutorial1);
                btnHarvestCore.interactable = canCombat && !unit.HasSiphonedThisTurn;
                SetButtonActiveHighlight(btnHarvestCore, currentMode == UnitActionMode.ConsumeLand);
            }

            // Universal Strike (Holy Strike, Shield Bash, Boulder Smash, Dragon Strike, Titan Strike, Melee)
            if (btnTitanStrike != null)
            {
                btnTitanStrike.gameObject.SetActive(true);
                btnTitanStrike.interactable = canCombat;
                SetButtonActiveHighlight(btnTitanStrike, currentMode == UnitActionMode.TitanStrike);
                if (txtTitanStrike != null)
                {
                    if (unit.UnitName.Contains("Paladin")) txtTitanStrike.text = "⚔️ Holy Strike\n<size=10>(3 Dmg)</size>";
                    else if (unit.UnitName.Contains("Shielder")) txtTitanStrike.text = "🛡️ Shield Bash\n<size=10>(2 Dmg)</size>";
                    else if (unit.UnitName.Contains("Basalt") || unit.UnitName.Contains("Golem")) txtTitanStrike.text = "👊 Boulder Smash\n<size=10>(3 Dmg)</size>";
                    else if (unit.UnitName.Contains("Dragon")) txtTitanStrike.text = "🔥 Dragon Strike\n<size=10>(Crit Strike)</size>";
                    else if (isTitan) txtTitanStrike.text = "🐾 Titan Strike\n<size=10>(Crit)</size>";
                    else txtTitanStrike.text = "⚔️ Attack\n<size=10>(Melee)</size>";
                }
            }

            // Magma Cataclysm (Titan Ulti - disabled in Holy Crusade)
            if (btnMagmaCataclysm != null)
            {
                btnMagmaCataclysm.gameObject.SetActive(isTitan && !isTutorial1);
                bool canCastCataclysm = canCombat && unit.ElementalCores >= 1;
                btnMagmaCataclysm.interactable = canCastCataclysm;
                SetButtonActiveHighlight(btnMagmaCataclysm, currentMode == UnitActionMode.MagmaCataclysm);
                if (txtMagmaCataclysm != null)
                {
                    txtMagmaCataclysm.text = (unit.ElementalCores >= 1)
                        ? "🌋 Cataclysm\n<size=11>(1 Core Area)</size>"
                        : "🌋 Cataclysm\n<size=11>(Req 1 Core)</size>";
                }
            }

            // Summon Titan from Commander
            if (btnSummonTitanAbility != null)
            {
                TacticalUnit3D reserveTitan = HexGridInteraction3D.FindTitanUnit();
                bool titanInReserve = (reserveTitan != null && !reserveTitan.gameObject.activeInHierarchy);
                bool shouldShowSummon = isCommander && titanInReserve;

                btnSummonTitanAbility.gameObject.SetActive(shouldShowSummon);
                if (shouldShowSummon)
                {
                    bool canSummon = isPlayerTurn && unit.ElementalCores >= 1;
                    btnSummonTitanAbility.interactable = canSummon;
                    SetButtonActiveHighlight(btnSummonTitanAbility, currentMode == UnitActionMode.SummonTitan);
                    if (txtSummonTitanAbility != null)
                    {
                        txtSummonTitanAbility.text = canSummon
                            ? "🌋 Summon Titan\n<size=11>(1 Core)</size>"
                            : "🌋 Summon Titan\n<size=11>(Req 1 Core)</size>";
                    }
                }
            }

            // Recall Unit
            if (btnRecall != null)
            {
                btnRecall.interactable = isPlayerTurn;
            }

            ApplyTutorialButtonPulse();
        }

        private string activeTutorialButtonKey = null;

        public void HighlightTutorialButton(string buttonKey)
        {
            activeTutorialButtonKey = buttonKey;
        }

        private void ApplyTutorialButtonPulse()
        {
            if (string.IsNullOrEmpty(activeTutorialButtonKey)) return;

            bool isPushOrStrike = (activeTutorialButtonKey == "PushOrStrike");
            bool isStrikeOrMove = (activeTutorialButtonKey == "TitanStrike" || activeTutorialButtonKey == "Strike" || activeTutorialButtonKey == "StrikeOrMove");
            bool isStrikeOrSkill = (activeTutorialButtonKey == "Strike" || activeTutorialButtonKey == "TitanStrike" || activeTutorialButtonKey == "Fireball" || activeTutorialButtonKey == "Skill");
            Button targetBtn = null;
            if (activeTutorialButtonKey == "Move") targetBtn = btnMove;
            else if (activeTutorialButtonKey == "Strike" || activeTutorialButtonKey == "TitanStrike") targetBtn = btnTitanStrike;
            else if (activeTutorialButtonKey == "Fireball") targetBtn = btnFireball;
            else if (activeTutorialButtonKey == "Push") targetBtn = btnPush;
            else if (activeTutorialButtonKey == "HarvestCore" || activeTutorialButtonKey == "Siphon") targetBtn = btnHarvestCore;
            else if (activeTutorialButtonKey == "Cataclysm") targetBtn = btnMagmaCataclysm;
            else if (activeTutorialButtonKey == "EndTurn") targetBtn = btnEndTurn;
            else if (activeTutorialButtonKey == "TearRift") targetBtn = btnTearRift;
            else if (activeTutorialButtonKey == "DeployCommander")
            {
                targetBtn = (dispatchModalRoot != null && dispatchModalRoot.activeSelf) ? btnDispatchConfirm : btnOpenRiftDispatch;
            }
            else if (activeTutorialButtonKey == "DeployTitan" || activeTutorialButtonKey == "SummonTitan")
            {
                targetBtn = (dispatchModalRoot != null && dispatchModalRoot.activeSelf) ? btnDispatchConfirm : btnOpenRiftDispatch;
            }

            // Strict TRPG Training Wheels:
            // Lock and dim all action buttons other than the target button!
            Button[] allActionButtons = new Button[]
            {
                btnMove, btnTitanStrike, btnFireball, btnWaterSurge, btnEarthPillar,
                btnPush, btnHarvestCore, btnMagmaCataclysm, btnSummonTitanAbility,
                btnRecall, btnEndTurn, btnTearRift, btnRandomizeMap,
                btnOpenRiftDispatch
            };

            bool isGuidedPlay = Tutorial.TutorialScenarioManager.Instance != null && Tutorial.TutorialScenarioManager.Instance.IsTutorialActive;

            foreach (var btn in allActionButtons)
            {
                if (btn == null) continue;
                bool isAllowed = isGuidedPlay || (btn == targetBtn) || 
                                 (isPushOrStrike && (btn == btnPush || btn == btnTitanStrike)) ||
                                 (isStrikeOrMove && (btn == btnTitanStrike || btn == btnMove)) ||
                                 (isStrikeOrSkill && (btn == btnTitanStrike || btn == btnFireball)) ||
                                 (btn == btnMove) ||
                                 (btn == btnTitanStrike && (activeTutorialButtonKey == "Strike" || activeTutorialButtonKey == "TitanStrike" || activeTutorialButtonKey == "Move" || activeTutorialButtonKey == "Push"));
                if (isAllowed)
                {
                    btn.interactable = true;
                    if (btn == targetBtn || (isStrikeOrSkill && (btn == btnTitanStrike || btn == btnFireball)))
                    {
                        float pulse = (Mathf.Sin(Time.unscaledTime * 6f) + 1f) * 0.5f;
                        Color goldPulse = Color.Lerp(Color.white, new Color(1.0f, 0.85f, 0.2f, 1f), pulse);
                        var colors = btn.colors;
                        colors.normalColor = goldPulse;
                        btn.colors = colors;
                    }
                    else
                    {
                        var colors = btn.colors;
                        colors.normalColor = Color.white;
                        btn.colors = colors;
                    }
                }
                else
                {
                    btn.interactable = false;
                }
            }

            // Also highlight Disgaea Squad Dispatch Confirm button when dispatching in tutorial!
            if (dispatchModalRoot != null && dispatchModalRoot.activeSelf && btnDispatchConfirm != null)
            {
                if (activeTutorialButtonKey == "DeployCommander" || activeTutorialButtonKey == "DeployTitan" || activeTutorialButtonKey == "SummonTitan")
                {
                    float pulse = (Mathf.Sin(Time.unscaledTime * 6f) + 1f) * 0.5f;
                    Color goldPulse = Color.Lerp(Color.white, new Color(1.0f, 0.85f, 0.2f, 1f), pulse);
                    var colors = btnDispatchConfirm.colors;
                    colors.normalColor = goldPulse;
                    btnDispatchConfirm.colors = colors;
                }
                else
                {
                    var colors = btnDispatchConfirm.colors;
                    colors.normalColor = Color.white;
                    btnDispatchConfirm.colors = colors;
                }
            }

            // Also apply training wheels & highlight to Disgaea Command Menu buttons when active!
            if (commandMenuRoot != null && commandMenuRoot.activeSelf)
            {
                Button targetCmdBtn = null;
                if (activeTutorialButtonKey == "Move") targetCmdBtn = btnCmdMove;
                else if (activeTutorialButtonKey == "Strike" || activeTutorialButtonKey == "TitanStrike") targetCmdBtn = btnCmdAttack;
                else if (activeTutorialButtonKey == "Push") targetCmdBtn = btnCmdPush;
                else if (activeTutorialButtonKey == "Fireball" || activeTutorialButtonKey == "WaterSurge" || activeTutorialButtonKey == "EarthPillar" || activeTutorialButtonKey == "Skill" || activeTutorialButtonKey == "Cataclysm") targetCmdBtn = btnCmdSpecial;
                else if (activeTutorialButtonKey == "HarvestCore" || activeTutorialButtonKey == "Siphon" || activeTutorialButtonKey == "Absorb") targetCmdBtn = btnCmdAbsorb;

                Button[] allCmdButtons = new Button[]
                {
                    btnCmdMove, btnCmdAttack, btnCmdSpecial, btnCmdDefend, btnCmdPush,
                    btnCmdAbsorb, btnCmdItem, btnCmdWait, btnCmdCancel
                };

                foreach (var cmdBtn in allCmdButtons)
                {
                    if (cmdBtn == null) continue;
                    bool isCmdAllowed = isGuidedPlay || (targetCmdBtn == null) || (cmdBtn == targetCmdBtn) || (cmdBtn == btnCmdCancel);
                    cmdBtn.interactable = isCmdAllowed;
                    if (cmdBtn == targetCmdBtn)
                    {
                        float pulse = (Mathf.Sin(Time.unscaledTime * 6f) + 1f) * 0.5f;
                        Color goldPulse = Color.Lerp(Color.white, new Color(1.0f, 0.85f, 0.2f, 1f), pulse);
                        var colors = cmdBtn.colors;
                        colors.normalColor = goldPulse;
                        cmdBtn.colors = colors;
                    }
                    else
                    {
                        var colors = cmdBtn.colors;
                        colors.normalColor = Color.white;
                        cmdBtn.colors = colors;
                    }
                }

                // If Special Submenu is open, highlight the target skill
                if (specialSubmenuRoot != null && specialSubmenuRoot.activeSelf && specialSubmenuContent != null)
                {
                    for (int i = 0; i < specialSubmenuContent.childCount; i++)
                    {
                        Button sb = specialSubmenuContent.GetChild(i).GetComponent<Button>();
                        if (sb != null)
                        {
                            bool isSkillMatch = (activeTutorialButtonKey == "Fireball" && sb.name.Contains("Fireball")) ||
                                                (activeTutorialButtonKey == "Cataclysm" && sb.name.Contains("Cataclysm"));
                            if (isSkillMatch)
                            {
                                float pulse = (Mathf.Sin(Time.unscaledTime * 6f) + 1f) * 0.5f;
                                Color goldPulse = Color.Lerp(Color.white, new Color(1.0f, 0.85f, 0.2f, 1f), pulse);
                                var colors = sb.colors;
                                colors.normalColor = goldPulse;
                                sb.colors = colors;
                            }
                            else
                            {
                                var colors = sb.colors;
                                colors.normalColor = Color.white;
                                sb.colors = colors;
                            }
                        }
                    }
                }
            }
        }

        private void SetButtonActiveHighlight(Button btn, bool isActive)
        {
            if (btn == null) return;
            var colors = btn.colors;
            if (isActive)
            {
                colors.normalColor = new Color(1.0f, 0.88f, 0.45f, 1f); // Gold highlight
            }
            else
            {
                colors.normalColor = Color.white;
            }
            btn.colors = colors;
        }

        // =========================================================================
        // DISGAEA-STYLE PANELS: BASE PANEL (RIFT DISPATCH) & VERTICAL COMMAND MENU
        // =========================================================================

        private void BuildDisgaeaDispatchModal(Transform parent, Font font, Sprite frameSprite, Sprite btnSprite)
        {
            dispatchModalRoot = CreateUI("Modal_DisgaeaDispatch", parent);
            SetRect(dispatchModalRoot.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 25f), new Vector2(620f, 410f));

            ApplyRpgMakerWindowStyle(dispatchModalRoot, new Color(0.08f, 0.07f, 0.14f, 1.0f));

            // Header Bar
            GameObject headObj = CreateUI("HeaderBar", dispatchModalRoot.transform);
            SetRect(headObj.GetComponent<RectTransform>(),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -8f), new Vector2(-24f, 40f));

            Text titleTxt = CreateTxt("Txt_Title", headObj.transform, "🌀 CITADEL RIFT — SQUAD DISPATCH", font, 15, new Color(1.0f, 0.85f, 0.35f), TextAnchor.MiddleLeft);
            titleTxt.fontStyle = FontStyle.Bold;
            SetRect(titleTxt.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(15f, 0f), new Vector2(-60f, 0f));

            btnCloseDispatch = CreateBtn("Btn_Close", headObj.transform, "✕", font, 14, btnSprite, new Color(0.6f, 0.2f, 0.2f, 1f), 32f, 32f);
            SetRect(btnCloseDispatch.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-5f, 0f), new Vector2(32f, 32f));
            btnCloseDispatch.onClick.RemoveAllListeners();
            btnCloseDispatch.onClick.AddListener(OnCloseDispatchClicked);

            // Subtitle
            Text subTxt = CreateTxt("Txt_Subtitle", dispatchModalRoot.transform, "Select a unit in reserve to emerge through the dimensional breach.", font, 11, new Color(0.75f, 0.75f, 0.85f), TextAnchor.MiddleLeft);
            SetRect(subTxt.GetComponent<RectTransform>(),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(20f, -44f), new Vector2(-40f, 18f));

            // Left Pane: Roster List
            GameObject leftPane = CreateUI("Pane_RosterList", dispatchModalRoot.transform);
            SetRect(leftPane.GetComponent<RectTransform>(),
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f),
                new Vector2(18f, 18f), new Vector2(285f, -85f));
            Image leftBg = leftPane.AddComponent<Image>();
            leftBg.sprite = ElementalHexTactics3D.Units.TacticalUnitSpawner.LoadUISprite("UI_WindowBg_RPG.png") ?? frameSprite;
            leftBg.type = Image.Type.Sliced;
            leftBg.color = new Color(0.03f, 0.03f, 0.06f, 1.0f);

            GameObject listContentObj = CreateUI("List_Content", leftPane.transform);
            SetRect(listContentObj.GetComponent<RectTransform>(),
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 0f), new Vector2(-16f, -16f));
            dispatchListContent = listContentObj.transform;
            VerticalLayoutGroup vl = listContentObj.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(6, 6, 8, 8);
            vl.spacing = 6f;
            vl.childAlignment = TextAnchor.UpperCenter;
            vl.childControlWidth = true;
            vl.childControlHeight = false;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;

            // Right Pane: Unit Preview
            GameObject rightPane = CreateUI("Pane_UnitPreview", dispatchModalRoot.transform);
            SetRect(rightPane.GetComponent<RectTransform>(),
                new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0f),
                new Vector2(-18f, 18f), new Vector2(285f, -85f));
            Image rightBg = rightPane.AddComponent<Image>();
            rightBg.sprite = ElementalHexTactics3D.Units.TacticalUnitSpawner.LoadUISprite("UI_WindowBg_RPG.png") ?? frameSprite;
            rightBg.type = Image.Type.Sliced;
            rightBg.color = new Color(0.03f, 0.03f, 0.06f, 1.0f);

            // Portrait
            GameObject portFrame = CreateUI("Portrait_Frame", rightPane.transform);
            SetRect(portFrame.GetComponent<RectTransform>(),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(14f, -14f), new Vector2(74f, 74f));
            Image pFrameImg = portFrame.AddComponent<Image>();
            pFrameImg.sprite = frameSprite;
            pFrameImg.type = Image.Type.Sliced;
            pFrameImg.color = new Color(0.18f, 0.15f, 0.28f, 1f);

            GameObject portImgObj = CreateUI("Img_Portrait", portFrame.transform);
            SetStretch(portImgObj.GetComponent<RectTransform>());
            dispatchPreviewPortrait = portImgObj.AddComponent<Image>();
            dispatchPreviewPortrait.preserveAspect = true;

            // Name & Class
            dispatchPreviewName = CreateTxt("Txt_PrevName", rightPane.transform, "Select Unit", font, 14, new Color(1.0f, 0.88f, 0.45f), TextAnchor.UpperLeft);
            dispatchPreviewName.fontStyle = FontStyle.Bold;
            SetRect(dispatchPreviewName.GetComponent<RectTransform>(),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(98f, -14f), new Vector2(-108f, 24f));

            dispatchPreviewClass = CreateTxt("Txt_PrevClass", rightPane.transform, "---", font, 11, new Color(0.7f, 0.8f, 0.9f), TextAnchor.UpperLeft);
            SetRect(dispatchPreviewClass.GetComponent<RectTransform>(),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(98f, -38f), new Vector2(-108f, 20f));

            // Stats Area
            dispatchPreviewStats = CreateTxt("Txt_PrevStats", rightPane.transform, "HP: --\nATK: --   MOV: --\nRNG: --", font, 12, Color.white, TextAnchor.UpperLeft);
            SetRect(dispatchPreviewStats.GetComponent<RectTransform>(),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -98f), new Vector2(-32f, 110f));

            // Cost Area
            dispatchPreviewCost = CreateTxt("Txt_PrevCost", rightPane.transform, "Cost: FREE", font, 12, new Color(0.4f, 0.9f, 0.5f), TextAnchor.UpperLeft);
            dispatchPreviewCost.fontStyle = FontStyle.Bold;
            SetRect(dispatchPreviewCost.GetComponent<RectTransform>(),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f),
                new Vector2(16f, 62f), new Vector2(-32f, 26f));

            // Confirm Button
            btnDispatchConfirm = CreateBtn("Btn_DispatchConfirm", rightPane.transform, "🚀 DISPATCH TO FIELD", font, 13, btnSprite, new Color(0.25f, 0.60f, 0.28f, 1f), 250f, 42f);
            SetRect(btnDispatchConfirm.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 30f), new Vector2(250f, 42f));
            txtDispatchConfirm = btnDispatchConfirm.GetComponentInChildren<Text>();

            btnDispatchConfirm.onClick.RemoveAllListeners();
            btnDispatchConfirm.onClick.AddListener(() =>
            {
                if (currentPreviewDispatchUnit != null)
                {
                    OnDispatchUnitSelected(currentPreviewDispatchUnit);
                }
            });

            dispatchModalRoot.SetActive(false);
        }

        private void BuildDisgaeaCommandMenu(Transform parent, Font font, Sprite frameSprite, Sprite btnSprite)
        {
            commandMenuRoot = CreateUI("Menu_DisgaeaCommand", parent);
            RectTransform menuRt = commandMenuRoot.GetComponent<RectTransform>();
            SetRect(menuRt,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-20f, -72f), new Vector2(175f, 320f));

            ApplyRpgMakerWindowStyle(commandMenuRoot, new Color(0.07f, 0.06f, 0.12f, 0.96f));

            VerticalLayoutGroup vl = commandMenuRoot.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(8, 8, 10, 10);
            vl.spacing = 4f;
            vl.childAlignment = TextAnchor.UpperCenter;
            vl.childControlWidth = true;
            vl.childControlHeight = false;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;

            ContentSizeFitter csf = commandMenuRoot.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Header
            txtCmdHeader = CreateTxt("Txt_CmdHeader", commandMenuRoot.transform, "COMMAND", font, 12, new Color(1.0f, 0.88f, 0.40f), TextAnchor.MiddleCenter);
            txtCmdHeader.fontStyle = FontStyle.Bold;
            txtCmdHeader.rectTransform.sizeDelta = new Vector2(154f, 24f);
            LayoutElement leH = txtCmdHeader.gameObject.AddComponent<LayoutElement>();
            leH.preferredHeight = 24f;

            // 1. Move
            btnCmdMove = CreateBtn("Btn_CmdMove", commandMenuRoot.transform, "👣 Move", font, 12, btnSprite, new Color(0.18f, 0.35f, 0.55f, 1f), 154f, 32f);
            txtCmdMove = btnCmdMove.GetComponentInChildren<Text>();
            btnCmdMove.onClick.RemoveAllListeners();
            btnCmdMove.onClick.AddListener(() => OnActionClicked(UnitActionMode.Move));

            // 2. Attack (Basic weapon attack: 1 hex melee or 2-3 hexes ranged)
            btnCmdAttack = CreateBtn("Btn_CmdAttack", commandMenuRoot.transform, "⚔️ Attack", font, 12, btnSprite, new Color(0.68f, 0.22f, 0.20f, 1f), 154f, 32f);
            txtCmdAttack = btnCmdAttack.GetComponentInChildren<Text>();
            btnCmdAttack.onClick.RemoveAllListeners();
            btnCmdAttack.onClick.AddListener(() => OnActionClicked(UnitActionMode.TitanStrike));

            // 3. Special (Opens unique skills sub-menu)
            btnCmdSpecial = CreateBtn("Btn_CmdSpecial", commandMenuRoot.transform, "✨ Special", font, 12, btnSprite, new Color(0.72f, 0.38f, 0.15f, 1f), 154f, 32f);
            txtCmdSpecial = btnCmdSpecial.GetComponentInChildren<Text>();
            btnCmdSpecial.onClick.RemoveAllListeners();
            btnCmdSpecial.onClick.AddListener(OnCmdSpecialClicked);

            // 4. Defend (Guard stance: -50% incoming damage)
            btnCmdDefend = CreateBtn("Btn_CmdDefend", commandMenuRoot.transform, "🛡️ Defend", font, 12, btnSprite, new Color(0.20f, 0.45f, 0.65f, 1f), 154f, 32f);
            txtCmdDefend = btnCmdDefend.GetComponentInChildren<Text>();
            btnCmdDefend.onClick.RemoveAllListeners();
            btnCmdDefend.onClick.AddListener(OnCmdDefendClicked);

            // 5. Push (Normal 1-hex kinetic shove)
            btnCmdPush = CreateBtn("Btn_CmdPush", commandMenuRoot.transform, "💨 Push", font, 12, btnSprite, new Color(0.45f, 0.20f, 0.60f, 1f), 154f, 32f);
            txtCmdPush = btnCmdPush.GetComponentInChildren<Text>();
            btnCmdPush.onClick.RemoveAllListeners();
            btnCmdPush.onClick.AddListener(() => OnActionClicked(UnitActionMode.KineticPush));

            // 6. Absorb (Contextual Siphon Core)
            btnCmdAbsorb = CreateBtn("Btn_CmdAbsorb", commandMenuRoot.transform, "⚡ Absorb", font, 12, btnSprite, new Color(0.70f, 0.55f, 0.15f, 1f), 154f, 32f);
            txtCmdAbsorb = btnCmdAbsorb.GetComponentInChildren<Text>();
            btnCmdAbsorb.onClick.RemoveAllListeners();
            btnCmdAbsorb.onClick.AddListener(() => OnActionClicked(UnitActionMode.ConsumeLand));

            // 7. Item (Inventory potion sub-menu)
            btnCmdItem = CreateBtn("Btn_CmdItem", commandMenuRoot.transform, "🎒 Item", font, 12, btnSprite, new Color(0.22f, 0.50f, 0.35f, 1f), 154f, 32f);
            txtCmdItem = btnCmdItem.GetComponentInChildren<Text>();
            btnCmdItem.onClick.RemoveAllListeners();
            btnCmdItem.onClick.AddListener(OnCmdItemClicked);

            // 8. Wait
            btnCmdWait = CreateBtn("Btn_CmdWait", commandMenuRoot.transform, "⏳ Wait", font, 12, btnSprite, new Color(0.35f, 0.35f, 0.40f, 1f), 154f, 32f);
            btnCmdWait.onClick.RemoveAllListeners();
            btnCmdWait.onClick.AddListener(OnCmdWaitClicked);

            // 9. Cancel
            btnCmdCancel = CreateBtn("Btn_CmdCancel", commandMenuRoot.transform, "✕ Cancel", font, 12, btnSprite, new Color(0.25f, 0.25f, 0.28f, 1f), 154f, 30f);
            btnCmdCancel.onClick.RemoveAllListeners();
            btnCmdCancel.onClick.AddListener(OnCmdCancelClicked);

            commandMenuRoot.transform.SetAsLastSibling();
            commandMenuRoot.SetActive(false);

            // Build Submenus alongside Main Menu
            BuildDisgaeaSpecialSubmenu(parent, font, frameSprite, btnSprite);
            BuildDisgaeaItemSubmenu(parent, font, frameSprite, btnSprite);
            if (skillInfoWindowRoot == null)
            {
                BuildSkillDescriptionWindow(parent, font);
            }
        }

        private struct SkillInfoEntry
        {
            public Button Button;
            public string Title;
            public string Cost;
            public string Dmg;
            public string Effect;
            public string Desc;
        }

        private readonly List<SkillInfoEntry> activeSubmenuSkillEntries = new List<SkillInfoEntry>();
        private readonly List<SkillInfoEntry> activeMainCmdSkillEntries = new List<SkillInfoEntry>();

        private static string CleanUnsupportedGlyphs(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            // Remove surrogate pair emojis & symbols that LegacyRuntime.ttf renders as [X] boxes
            string[] strip = new string[]
            {
                "☀️ ", "☀️", "⚡ ", "⚡", "⛰️ ", "⛰️", "🌊 ", "🌊", "⚜️ ", "⚜️",
                "🛡️ ", "🛡️", "✨ ", "✨", "🔥 ", "🔥", "💧 ", "💧", "💥 ", "💥",
                "🌋 ", "🌋", "🐊 ", "🐊", "🧪 ", "🧪", "👣 ", "👣", "⚔️ ", "⚔️",
                "🏹 ", "🏹", "💨 ", "💨", "🎒 ", "🎒", "🎯 ", "🎯"
            };
            string res = input;
            for (int i = 0; i < strip.Length; i++)
            {
                res = res.Replace(strip[i], "");
            }
            return res.Trim();
        }

        private void BuildSkillDescriptionWindow(Transform parent, Font font)
        {
            skillInfoWindowRoot = CreateUI("Panel_SkillDescription", parent);
            RectTransform rt = skillInfoWindowRoot.GetComponent<RectTransform>();
            SetRect(rt,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 18f), new Vector2(760f, 114f));

            ApplyRpgMakerWindowStyle(skillInfoWindowRoot, new Color(0.06f, 0.07f, 0.14f, 0.96f));

            // Row 1 Left: Skill Name (Top-Left, generous width, never collides with right badges)
            txtSkillInfoTitle = CreateTxt("Txt_SkillInfoTitle", skillInfoWindowRoot.transform, "RADIANT SMITE", font, 15, new Color(1.0f, 0.88f, 0.40f, 1f), TextAnchor.MiddleLeft);
            txtSkillInfoTitle.fontStyle = FontStyle.Bold;
            txtSkillInfoTitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetRect(txtSkillInfoTitle.rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(22f, -12f), new Vector2(360f, 22f));

            // Row 1 Right: Mana Cost & Damage ONLY (Top-Right, compact so it never overlaps Title)
            txtSkillInfoBadges = CreateTxt("Txt_SkillInfoBadges", skillInfoWindowRoot.transform,
                "<color=#80D8FF><b>COST:</b> 25 MP</color>    |    <color=#FF8A80><b>DMG:</b> 780</color>",
                font, 13, Color.white, TextAnchor.MiddleRight);
            txtSkillInfoBadges.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetRect(txtSkillInfoBadges.rectTransform,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-22f, -12f), new Vector2(340f, 22f));

            // Row 2: Dedicated Tactical / Terrain Effect Line (Full width, clear spacing below Row 1)
            txtSkillInfoEffect = CreateTxt("Txt_SkillInfoEffect", skillInfoWindowRoot.transform,
                "<color=#FFD54F><b>EFFECT:</b> Scorches Tile (Fire)  •  Range: 1-3</color>",
                font, 12, new Color(1.0f, 0.84f, 0.35f, 1f), TextAnchor.MiddleLeft);
            txtSkillInfoEffect.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetRect(txtSkillInfoEffect.rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(22f, -38f), new Vector2(716f, 20f));

            // Row 3: Full Skill Description Text (Bottom area, 2 spacious lines, no divider line!)
            txtSkillInfoDesc = CreateTxt("Txt_SkillInfoDesc", skillInfoWindowRoot.transform,
                "Calls down a pillar of solar judgment. Deals 780 Holy Fire damage and ignites the target hex into Scorched Earth.",
                font, 12, new Color(0.90f, 0.94f, 0.98f, 1f), TextAnchor.UpperLeft);
            txtSkillInfoDesc.lineSpacing = 1.15f;
            txtSkillInfoDesc.horizontalOverflow = HorizontalWrapMode.Wrap;
            txtSkillInfoDesc.verticalOverflow = VerticalWrapMode.Overflow;
            SetRect(txtSkillInfoDesc.rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(22f, -62f), new Vector2(716f, 40f));

            skillInfoWindowRoot.transform.SetAsLastSibling();
            skillInfoWindowRoot.SetActive(false);
        }

        public void ShowSkillDescriptionWindow(string title, string cost, string dmg, string effect, string desc)
        {
            if (skillInfoWindowRoot == null) return;
            string cleanTitle = CleanUnsupportedGlyphs(title);
            string cleanCost = CleanUnsupportedGlyphs(cost);
            string cleanDmg = CleanUnsupportedGlyphs(dmg);
            string cleanEffect = CleanUnsupportedGlyphs(effect);
            string cleanDesc = CleanUnsupportedGlyphs(desc);

            activeHoverSkillTitle = cleanTitle;
            if (!skillInfoWindowRoot.activeSelf)
            {
                skillInfoWindowRoot.SetActive(true);
            }
            skillInfoWindowRoot.transform.SetAsLastSibling();

            if (txtSkillInfoTitle != null)
            {
                txtSkillInfoTitle.text = $"<b>✦ {cleanTitle}</b>";
            }
            if (txtSkillInfoBadges != null)
            {
                txtSkillInfoBadges.text = $"<color=#80D8FF><b>COST:</b> {cleanCost}</color>    |    <color=#FF8A80><b>DMG:</b> {cleanDmg}</color>";
            }
            if (txtSkillInfoEffect != null)
            {
                txtSkillInfoEffect.text = $"<color=#FFD54F><b>EFFECT:</b> {cleanEffect}</color>";
            }
            if (txtSkillInfoDesc != null)
            {
                txtSkillInfoDesc.text = cleanDesc;
            }
        }

        public void HideSkillDescriptionWindow()
        {
            activeHoverSkillTitle = null;
            if (skillInfoWindowRoot != null && skillInfoWindowRoot.activeSelf)
            {
                skillInfoWindowRoot.SetActive(false);
            }
        }

        private void BindSkillHoverPreview(Button btn, string title, string cost, string dmg, string effect, string desc)
        {
            if (btn == null) return;
            SkillInfoEntry entry = new SkillInfoEntry
            {
                Button = btn,
                Title = title,
                Cost = cost,
                Dmg = dmg,
                Effect = effect,
                Desc = desc
            };

            if (btn.transform.parent == specialSubmenuContent || btn.transform.parent == itemSubmenuContent)
            {
                activeSubmenuSkillEntries.Add(entry);
            }
            else
            {
                activeMainCmdSkillEntries.Add(entry);
            }
        }

        private void EvaluateHoveredSkillDescription()
        {
            Vector2 mousePos = Vector2.zero;
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            }
            else
            {
                mousePos = Input.mousePosition;
            }

            // 1. Check active Submenu buttons first (Special Skills or Inventory Items)
            if (IsSubmenuOpen)
            {
                for (int i = 0; i < activeSubmenuSkillEntries.Count; i++)
                {
                    var entry = activeSubmenuSkillEntries[i];
                    if (entry.Button != null && entry.Button.gameObject.activeInHierarchy)
                    {
                        RectTransform rt = entry.Button.GetComponent<RectTransform>();
                        if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, mousePos, null))
                        {
                            ShowSkillDescriptionWindow(entry.Title, entry.Cost, entry.Dmg, entry.Effect, entry.Desc);
                            return;
                        }
                    }
                }
                // If submenu is open and mouse is not over a specific button yet, show the first submenu entry
                if (activeSubmenuSkillEntries.Count > 0)
                {
                    var first = activeSubmenuSkillEntries[0];
                    ShowSkillDescriptionWindow(first.Title, first.Cost, first.Dmg, first.Effect, first.Desc);
                }
                return;
            }

            // 2. Otherwise check Main Command Menu buttons when hovered
            if (commandMenuRoot != null && commandMenuRoot.activeInHierarchy)
            {
                for (int i = 0; i < activeMainCmdSkillEntries.Count; i++)
                {
                    var entry = activeMainCmdSkillEntries[i];
                    if (entry.Button != null && entry.Button.gameObject.activeInHierarchy)
                    {
                        RectTransform rt = entry.Button.GetComponent<RectTransform>();
                        if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, mousePos, null))
                        {
                            ShowSkillDescriptionWindow(entry.Title, entry.Cost, entry.Dmg, entry.Effect, entry.Desc);
                            return;
                        }
                    }
                }
            }

            // 2.5 Check Gateway / Rift panel buttons on Right Dock when hovered
            if (actionBarRoot != null && actionBarRoot.activeInHierarchy)
            {
                if (btnTearRift != null && btnTearRift.gameObject.activeInHierarchy)
                {
                    RectTransform rt = btnTearRift.GetComponent<RectTransform>();
                    if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, mousePos, null))
                    {
                        ShowSkillDescriptionWindow(
                            "TEAR OPEN ABYSSAL RIFT",
                            "FREE (0 MP)",
                            "SPATIAL INVASION",
                            "Deploy Vanguard",
                            "Channels Citadel ley-lines to anchor an Abyssal Rift conduit onto the battlefield. Once anchored, you can summon commanders, vanguard knights, and primordial titans directly into combat."
                        );
                        return;
                    }
                }
                if (btnRandomizeMap != null && btnRandomizeMap.gameObject.activeInHierarchy)
                {
                    RectTransform rt = btnRandomizeMap.GetComponent<RectTransform>();
                    if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, mousePos, null))
                    {
                        ShowSkillDescriptionWindow(
                            "RANDOMIZE MAP",
                            "KEY: [R]",
                            "INCURSION TOPOLOGY",
                            "Re-roll Terrain",
                            "Rolls a new randomized battlefield topology with procedural elevation steps, magma reservoirs, flooded waters, and tactical choke points."
                        );
                        return;
                    }
                }
                if (btnOpenRiftDispatch != null && btnOpenRiftDispatch.gameObject.activeInHierarchy)
                {
                    RectTransform rt = btnOpenRiftDispatch.GetComponent<RectTransform>();
                    if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, mousePos, null))
                    {
                        ShowSkillDescriptionWindow(
                            "CITADEL SQUAD DISPATCH",
                            "FREE",
                            "BASE PANEL",
                            "Summon Reserve Forces",
                            "Opens the Citadel Rift Deployment Menu to dispatch your chosen hero, vanguard defenders, and summoned titans onto the battlefield."
                        );
                        return;
                    }
                }
            }

            // 3. Not hovering over any command button and no submenu open -> show Hovered / Selected Hex Tile & Coordinate Inspector!
            ShowHexTileInspector(HexGridInteraction3D.Instance);
        }

        public void ShowTileDescriptionWindow(string tileTitle, string elevStr, string occupantStr, string effectStr, string descStr)
        {
            if (skillInfoWindowRoot == null) return;
            if (dispatchModalRoot != null && dispatchModalRoot.activeInHierarchy)
            {
                HideSkillDescriptionWindow();
                return;
            }

            string cleanTitle = CleanUnsupportedGlyphs(tileTitle);
            string cleanElev = CleanUnsupportedGlyphs(elevStr);
            string cleanOcc = CleanUnsupportedGlyphs(occupantStr);
            string cleanEffect = CleanUnsupportedGlyphs(effectStr);
            string cleanDesc = CleanUnsupportedGlyphs(descStr);

            activeHoverSkillTitle = cleanTitle;
            if (!skillInfoWindowRoot.activeSelf)
            {
                skillInfoWindowRoot.SetActive(true);
            }
            skillInfoWindowRoot.transform.SetAsLastSibling();

            if (txtSkillInfoTitle != null)
            {
                txtSkillInfoTitle.text = $"<b>✦ {cleanTitle}</b>";
            }
            if (txtSkillInfoBadges != null)
            {
                txtSkillInfoBadges.text = $"<color=#80D8FF><b>ELEVATION:</b> {cleanElev}</color>    |    <color=#69F0AE><b>UNIT:</b> {cleanOcc}</color>";
            }
            if (txtSkillInfoEffect != null)
            {
                txtSkillInfoEffect.text = $"<color=#FFD54F><b>TERRAIN:</b> {cleanEffect}</color>";
            }
            if (txtSkillInfoDesc != null)
            {
                txtSkillInfoDesc.text = cleanDesc;
            }
        }

        private static string GetReadableTileStateName(Grid.HexTile3D tile)
        {
            if (tile == null) return "UNKNOWN TILE";
            switch (tile.State)
            {
                case Grid.TileState.Scorched: return "SCORCHED EARTH";
                case Grid.TileState.Magma: return "MOLTEN MAGMA";
                case Grid.TileState.Water: return tile.TierLevel >= 2 ? "DEEP WATER" : "FLOODED WATER";
                case Grid.TileState.Steam: return "STEAM MIST";
                case Grid.TileState.StonePillar: return "STONE PILLAR";
                case Grid.TileState.Mud: return "STICKY MUD";
                default: return "BARREN GROUND";
            }
        }

        private void ShowHexTileInspector(HexGridInteraction3D interaction)
        {
            if (interaction == null)
            {
                HideSkillDescriptionWindow();
                return;
            }

            if (dispatchModalRoot != null && dispatchModalRoot.activeInHierarchy)
            {
                HideSkillDescriptionWindow();
                return;
            }

            Grid.HexTile3D tile = interaction.CurrentHoveredTile ?? interaction.CurrentSelectedTile;
            if (tile == null && interaction.CurrentSelectedUnit != null)
            {
                tile = interaction.CurrentSelectedUnit.CurrentTile;
            }

            if (tile == null)
            {
                HideSkillDescriptionWindow();
                return;
            }

            int q = tile.Coordinates.Q;
            int r = tile.Coordinates.R;
            string stateName = GetReadableTileStateName(tile);
            string title = $"{stateName}  •  HEX ({q}, {r})";
            string elevStr = $"Tier {tile.Elevation}";

            TacticalUnit3D occ = tile.GetOccupant();
            string occStr = (occ != null && occ.CurrentHealth > 0)
                ? $"{occ.UnitName} ({occ.CurrentHealth}/{occ.MaxHealth} HP)"
                : "Empty";

            string effectStr;
            string descStr;

            switch (tile.State)
            {
                case Grid.TileState.Scorched:
                    effectStr = "Fire Attuned (+1 ATK to Fire)  •  Hit with Fire -> Ignites into Molten Magma!";
                    descStr = $"Hex ({q}, {r}) is sizzling Scorched Earth. Casting a Fire spell on this tile triggers Elemental Alchemy and erupts it into Molten Magma!";
                    break;
                case Grid.TileState.Magma:
                    effectStr = "Molten Hazard (Burn DMG on Entry & Turn Start)  •  Fire & Titan Units Immune";
                    descStr = $"Hex ({q}, {r}) is active Molten Magma! Shoving or moving non-immune units onto this tile inflicts heavy Burn damage.";
                    break;
                case Grid.TileState.Water:
                    effectStr = "Wave Attuned (+1 ATK to Water, -1 MOV to Others)  •  Hit with Fire -> Steam Mist";
                    descStr = $"Hex ({q}, {r}) is flooded with water, slowing non-aquatic movement. Hitting it with Fire vaporizes it into a protective Steam shroud.";
                    break;
                case Grid.TileState.Steam:
                    effectStr = "Vapor Shroud (Reduces Incoming Damage by 1)  •  Dissipates Over Time";
                    descStr = $"Hex ({q}, {r}) is blanketed in dense Steam mist, shrouding any unit standing here and mitigating incoming attack damage.";
                    break;
                case Grid.TileState.StonePillar:
                    effectStr = "Impassable Monolith Obstacle  •  Push Foe Into Pillar -> Heavy Wall Slam Damage!";
                    descStr = $"Hex ({q}, {r}) is blocked by a solid 3D Stone Pillar. Shoving an enemy into this obstacle triggers devastating Wall Slam collision damage!";
                    break;
                case Grid.TileState.Mud:
                    effectStr = "Sticky Quagmire (Immobilizes / Cripples Movement on Entry)";
                    descStr = $"Hex ({q}, {r}) is a boggy Mud trap that arrests unit movement when stepped or shoved onto.";
                    break;
                default:
                    effectStr = "Standard Tactical Ground  •  Fire -> Scorched  |  Water -> Flooded  |  Earth -> Pillar";
                    descStr = $"Hex ({q}, {r}) is solid Barren Ground at Elevation Tier {tile.Elevation}. Can be transformed by Elemental Spells.";
                    break;
            }

            if (Tutorial.TutorialScenarioManager.Instance != null &&
                Tutorial.TutorialScenarioManager.Instance.IsStage2Active &&
                q == 0 && r == 0 && tile.State == Grid.TileState.Scorched)
            {
                descStr = $"[DIRECTIVE TARGET HEX (0, 0)] Select Demon Lord, open [Special -> Hellfire Blast], and click this Scorched Earth at (0, 0) to ignite Molten Magma!";
            }

            if (occ != null && occ.CurrentHealth > 0)
            {
                string fac = occ.Faction == UnitFaction.Player ? "Allied" : "Enemy";
                descStr += $"  |  Occupied by {fac} {occ.UnitName} (ATK {occ.EffectiveAttackDamage}, MOV {occ.EffectiveMoveRange}).";
            }

            ShowTileDescriptionWindow(title, elevStr, occStr, effectStr, descStr);
        }

        private void BuildDisgaeaSpecialSubmenu(Transform parent, Font font, Sprite frameSprite, Sprite btnSprite)
        {
            specialSubmenuRoot = CreateUI("Menu_DisgaeaSpecialSubmenu", parent);
            RectTransform menuRt = specialSubmenuRoot.GetComponent<RectTransform>();
            SetRect(menuRt,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-203f, -72f), new Vector2(185f, 220f));

            ApplyRpgMakerWindowStyle(specialSubmenuRoot, new Color(0.06f, 0.08f, 0.14f, 0.96f));

            VerticalLayoutGroup vl = specialSubmenuRoot.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(8, 8, 10, 10);
            vl.spacing = 4f;
            vl.childAlignment = TextAnchor.UpperCenter;
            vl.childControlWidth = true;
            vl.childControlHeight = false;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;

            ContentSizeFitter csf = specialSubmenuRoot.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Header
            txtSpecialHeader = CreateTxt("Txt_SpecialHeader", specialSubmenuRoot.transform, "✨ SPECIAL SKILLS", font, 11, new Color(1.0f, 0.88f, 0.40f), TextAnchor.MiddleCenter);
            txtSpecialHeader.fontStyle = FontStyle.Bold;
            txtSpecialHeader.rectTransform.sizeDelta = new Vector2(164f, 24f);
            LayoutElement leH = txtSpecialHeader.gameObject.AddComponent<LayoutElement>();
            leH.preferredHeight = 24f;

            // Dynamic Container
            GameObject contentObj = CreateUI("Special_Content", specialSubmenuRoot.transform);
            specialSubmenuContent = contentObj.transform;
            VerticalLayoutGroup cvl = contentObj.AddComponent<VerticalLayoutGroup>();
            cvl.padding = new RectOffset(0, 0, 0, 0);
            cvl.spacing = 4f;
            cvl.childAlignment = TextAnchor.UpperCenter;
            cvl.childControlWidth = true;
            cvl.childControlHeight = false;
            cvl.childForceExpandWidth = true;
            cvl.childForceExpandHeight = false;
            ContentSizeFitter ccsf = contentObj.AddComponent<ContentSizeFitter>();
            ccsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ccsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Persistent Back Button
            btnSpecialBack = CreateBtn("Btn_SpecialBack", specialSubmenuRoot.transform, "🔙 Back", font, 12, btnSprite, new Color(0.28f, 0.28f, 0.32f, 1f), 164f, 30f);
            btnSpecialBack.onClick.RemoveAllListeners();
            btnSpecialBack.onClick.AddListener(OnSpecialBackClicked);

            specialSubmenuRoot.transform.SetAsLastSibling();
            specialSubmenuRoot.SetActive(false);
        }

        private void BuildDisgaeaItemSubmenu(Transform parent, Font font, Sprite frameSprite, Sprite btnSprite)
        {
            itemSubmenuRoot = CreateUI("Menu_DisgaeaItemSubmenu", parent);
            RectTransform menuRt = itemSubmenuRoot.GetComponent<RectTransform>();
            SetRect(menuRt,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-203f, -72f), new Vector2(185f, 180f));

            ApplyRpgMakerWindowStyle(itemSubmenuRoot, new Color(0.06f, 0.08f, 0.14f, 0.96f));

            VerticalLayoutGroup vl = itemSubmenuRoot.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(8, 8, 10, 10);
            vl.spacing = 4f;
            vl.childAlignment = TextAnchor.UpperCenter;
            vl.childControlWidth = true;
            vl.childControlHeight = false;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;

            ContentSizeFitter csf = itemSubmenuRoot.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Header
            txtItemHeader = CreateTxt("Txt_ItemHeader", itemSubmenuRoot.transform, "🎒 INVENTORY", font, 11, new Color(1.0f, 0.88f, 0.40f), TextAnchor.MiddleCenter);
            txtItemHeader.fontStyle = FontStyle.Bold;
            txtItemHeader.rectTransform.sizeDelta = new Vector2(164f, 24f);
            LayoutElement leH = txtItemHeader.gameObject.AddComponent<LayoutElement>();
            leH.preferredHeight = 24f;

            // Dynamic Container
            GameObject contentObj = CreateUI("Item_Content", itemSubmenuRoot.transform);
            itemSubmenuContent = contentObj.transform;
            VerticalLayoutGroup cvl = contentObj.AddComponent<VerticalLayoutGroup>();
            cvl.padding = new RectOffset(0, 0, 0, 0);
            cvl.spacing = 4f;
            cvl.childAlignment = TextAnchor.UpperCenter;
            cvl.childControlWidth = true;
            cvl.childControlHeight = false;
            cvl.childForceExpandWidth = true;
            cvl.childForceExpandHeight = false;
            ContentSizeFitter ccsf = contentObj.AddComponent<ContentSizeFitter>();
            ccsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ccsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Persistent Back Button
            btnItemBack = CreateBtn("Btn_ItemBack", itemSubmenuRoot.transform, "🔙 Back", font, 12, btnSprite, new Color(0.28f, 0.28f, 0.32f, 1f), 164f, 30f);
            btnItemBack.onClick.RemoveAllListeners();
            btnItemBack.onClick.AddListener(OnItemBackClicked);

            itemSubmenuRoot.transform.SetAsLastSibling();
            itemSubmenuRoot.SetActive(false);
        }

        private void UpdateDisgaeaDispatchModal(HexGridInteraction3D interaction)
        {
            if (dispatchModalRoot == null) return;
            dispatchModalRoot.SetActive(true);

            if (dispatchListContent == null) return;

            TacticalUnit3D[] all = UnityEngine.Object.FindObjectsByType<TacticalUnit3D>(FindObjectsInactive.Include);
            List<TacticalUnit3D> roster = new List<TacticalUnit3D>();
            foreach (var u in all)
            {
                if (u != null && u.Faction == UnitFaction.Player)
                {
                    roster.Add(u);
                }
            }

            roster.Sort((a, b) =>
            {
                int scoreA = (a.Archetype == UnitArchetype.Commander) ? 0 : (a.Archetype == UnitArchetype.Titan ? 2 : 1);
                int scoreB = (b.Archetype == UnitArchetype.Commander) ? 0 : (b.Archetype == UnitArchetype.Titan ? 2 : 1);
                return scoreA.CompareTo(scoreB);
            });

            for (int i = dispatchListContent.childCount - 1; i >= 0; i--)
            {
                Destroy(dispatchListContent.GetChild(i).gameObject);
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Sprite btnNormal = ElementalHexTactics3D.Units.TacticalUnitSpawner.LoadUISprite("UI_Button_Normal.png");

            // Smart auto-selection: prioritize units in reserve (waiting to be dispatched)
            TacticalUnit3D defaultUnit = roster.Find(u => !u.gameObject.activeInHierarchy);
            if (defaultUnit == null && roster.Count > 0) defaultUnit = roster[0];

            if (currentPreviewDispatchUnit == null || !roster.Contains(currentPreviewDispatchUnit) || currentPreviewDispatchUnit.gameObject.activeInHierarchy)
            {
                if (defaultUnit != null) currentPreviewDispatchUnit = defaultUnit;
            }

            foreach (var unit in roster)
            {
                TacticalUnit3D u = unit;
                bool inReserve = !u.gameObject.activeInHierarchy;
                string statusText = inReserve ? "<color=#FFD54F>[RESERVE]</color>" : "<color=#81C784>[ON FIELD]</color>";
                string costText = (u.Archetype == UnitArchetype.Titan) ? "1 Core" : "FREE";
                string icon = (u.Archetype == UnitArchetype.Commander) ? "👑" : (u.Archetype == UnitArchetype.Titan ? "🌋" : "🛡️");
                string label = $"{icon} <b>{u.UnitName}</b> {statusText}\n<size=10><color=#CFD8DC>Cost: {costText} • {u.Affinity}</color></size>";

                bool isSelected = (currentPreviewDispatchUnit == u);
                Color btnColor = isSelected 
                    ? new Color(0.32f, 0.40f, 0.65f, 1f) 
                    : (inReserve ? new Color(0.20f, 0.22f, 0.32f, 1f) : new Color(0.15f, 0.20f, 0.18f, 1f));

                Button rowBtn = CreateBtn("Btn_UnitRow_" + u.UnitName, dispatchListContent, label, font, 11, btnNormal, btnColor, 265f, 48f);
                rowBtn.onClick.AddListener(() =>
                {
                    UpdateDispatchPreview(u);
                    UpdateDisgaeaDispatchModal(interaction);
                });
            }

            if (currentPreviewDispatchUnit != null)
            {
                UpdateDispatchPreview(currentPreviewDispatchUnit);
            }
        }

        private void UpdateDispatchPreview(TacticalUnit3D unit)
        {
            if (unit == null) return;
            currentPreviewDispatchUnit = unit;

            if (dispatchPreviewName != null) dispatchPreviewName.text = unit.UnitName;
            if (dispatchPreviewClass != null) dispatchPreviewClass.text = $"{unit.Archetype} • {unit.Affinity} Element";
            if (dispatchPreviewStats != null)
            {
                string trait = "";
                if (unit.Archetype == UnitArchetype.Commander)
                    trait = "👑 Vanguard Aura: Can cast Fireball spells & commands summons.";
                else if (unit.Archetype == UnitArchetype.Titan)
                    trait = "🌋 Colossal: Naturally immune to Magma, triggers radial knockback on emergence.";
                else if (unit.UnitName.Contains("Basalt"))
                    trait = "🛡️ Iron Wall: Heavy defense, slams foes into walls.";
                else
                    trait = "✨ Allied Vanguard.";

                dispatchPreviewStats.text = $"❤️ HP: {unit.CurrentHealth}/{unit.MaxHealth}\n⚔️ ATK: {unit.EffectiveAttackDamage}   👣 MOV: {unit.MoveRange}\n🎯 Range: 1 Hex\n\n<i><color=#90A4AE>{trait}</color></i>";
            }

            bool inReserve = !unit.gameObject.activeInHierarchy;
            bool isTitan = (unit.Archetype == UnitArchetype.Titan);
            TacticalUnit3D cmdr = HexGridInteraction3D.FindCommanderUnit();
            int cores = (cmdr != null) ? cmdr.ElementalCores : 0;

            if (dispatchPreviewCost != null)
            {
                if (!inReserve)
                {
                    dispatchPreviewCost.text = "<color=#81C784>● Unit is already deployed on the field</color>";
                }
                else if (isTitan)
                {
                    bool canAfford = cores >= 1 || !HexGridInteraction3D.HasActivePlayerUnitsOnField();
                    dispatchPreviewCost.text = canAfford
                        ? $"<color=#FFD54F>💎 Summon Cost: 1 Core (Available: {cores})</color>"
                        : $"<color=#EF5350>💎 Summon Cost: 1 Core (Available: {cores} - Insufficient!)</color>";
                }
                else
                {
                    dispatchPreviewCost.text = "<color=#81C784>✨ Summon Cost: FREE (Citadel Vanguard)</color>";
                }
            }

            if (dispatchPreviewPortrait != null)
            {
                Sprite pSprite = unit.PortraitSprite ?? unit.StandeeSprite;
                if (pSprite != null)
                {
                    dispatchPreviewPortrait.sprite = pSprite;
                    dispatchPreviewPortrait.color = Color.white;
                }
                else
                {
                    dispatchPreviewPortrait.sprite = null;
                    dispatchPreviewPortrait.color = Color.clear;
                }
            }

            if (btnDispatchConfirm != null && txtDispatchConfirm != null)
            {
                if (inReserve)
                {
                    bool canSummon = !isTitan || cores >= 1 || !HexGridInteraction3D.HasActivePlayerUnitsOnField();
                    btnDispatchConfirm.interactable = canSummon;
                    txtDispatchConfirm.text = isTitan ? "🌋 SUMMON TITAN" : "🚀 DISPATCH VANGUARD";
                    var cb = btnDispatchConfirm.colors;
                    cb.normalColor = canSummon ? new Color(0.25f, 0.60f, 0.28f, 1f) : new Color(0.35f, 0.35f, 0.38f, 0.65f);
                    btnDispatchConfirm.colors = cb;
                }
                else
                {
                    btnDispatchConfirm.interactable = true;
                    txtDispatchConfirm.text = "🎯 FOCUS ON FIELD";
                    var cb = btnDispatchConfirm.colors;
                    cb.normalColor = new Color(0.18f, 0.38f, 0.65f, 1f);
                    btnDispatchConfirm.colors = cb;
                }
            }
        }

        private void OnDispatchUnitSelected(TacticalUnit3D unit)
        {
            if (unit == null) return;
            SoundManager3D.Instance?.PlayButtonClick();
            HexGridInteraction3D interaction = HexGridInteraction3D.Instance;
            if (interaction == null) return;

            if (unit.gameObject.activeInHierarchy)
            {
                if (dispatchModalRoot != null) dispatchModalRoot.SetActive(false);
                interaction.SelectTile(unit.CurrentTile);
                interaction.SelectUnit(unit);
                return;
            }

            if (dispatchModalRoot != null) dispatchModalRoot.SetActive(false);
            interaction.PendingDeployUnit = unit;
            if (unit.Archetype == UnitArchetype.Titan)
            {
                interaction.SetActionMode(UnitActionMode.SummonTitan);
            }
            else
            {
                interaction.SetActionMode(UnitActionMode.DeployCommander);
            }
        }

        private void OnCloseDispatchClicked()
        {
            SoundManager3D.Instance?.PlayButtonClick();
            if (dispatchModalRoot != null) dispatchModalRoot.SetActive(false);
            HexGridInteraction3D.Instance?.DeselectAll();
        }

        private void UpdateDisgaeaCommandMenu(TacticalUnit3D unit, HexGridInteraction3D interaction, bool isPlayerTurn, UnitActionMode currentMode)
        {
            if (commandMenuRoot == null) return;

            // 1. While unit is actively traversing tiles, or not player turn, hide menu
            if (!isPlayerTurn || unit == null || unit.IsMoving)
            {
                commandMenuRoot.SetActive(false);
                CloseAllSubmenus();
                HideSkillDescriptionWindow();
                return;
            }

            // 2. Disgaea TRPG UX: While player is in targeting mode, hide command menu immediately,
            //    but keep the Bottom Skill Description Window open so the player sees the active skill's cost, damage & effect!
            if (currentMode != UnitActionMode.None)
            {
                commandMenuRoot.SetActive(false);
                if (specialSubmenuRoot != null) specialSubmenuRoot.SetActive(false);
                if (itemSubmenuRoot != null) itemSubmenuRoot.SetActive(false);
                ShowTargetingModeSkillDescription(unit, interaction, currentMode);
                return;
            }

            // 3. If unit is fully exhausted for this turn, hide command menu
            bool isFullyDone = unit.HasActedThisTurn && (unit.HasMovedThisTurn || unit.IsImmobilized);
            if (isFullyDone)
            {
                commandMenuRoot.SetActive(false);
                CloseAllSubmenus();
                HideSkillDescriptionWindow();
                return;
            }

            commandMenuRoot.SetActive(true);
            commandMenuRoot.transform.SetAsLastSibling();

            if (txtCmdHeader != null)
            {
                txtCmdHeader.text = $"<b>{unit.UnitName.ToUpper()}</b>";
            }

            UpdateDisgaeaCommandMenuPosition(unit);
            if (specialSubmenuRoot != null && specialSubmenuRoot.activeSelf) UpdateSubmenuPosition(specialSubmenuRoot);
            if (itemSubmenuRoot != null && itemSubmenuRoot.activeSelf) UpdateSubmenuPosition(itemSubmenuRoot);

            bool canCombat = isPlayerTurn && !unit.HasActedThisTurn;
            bool isCommander = (unit.Archetype == UnitArchetype.Commander);
            bool isTitan = (unit.Archetype == UnitArchetype.Titan);
            bool isTutorial1 = Tutorial.TutorialScenarioManager.Instance != null && Tutorial.TutorialScenarioManager.Instance.IsStage1Active;

            // 1. Move
            if (btnCmdMove != null)
            {
                btnCmdMove.interactable = isPlayerTurn && !unit.HasMovedThisTurn && !unit.IsImmobilized;
                SetButtonActiveHighlight(btnCmdMove, currentMode == UnitActionMode.Move);
                if (txtCmdMove != null)
                {
                    txtCmdMove.text = unit.HasMovedThisTurn ? "👣 Move (Done)" : (unit.IsCrippled ? "👣 Move (1)" : "👣 Move");
                }
            }

            // 2. Attack (Melee 1 hex or Ranged weapon attack)
            if (btnCmdAttack != null)
            {
                btnCmdAttack.interactable = canCombat;
                SetButtonActiveHighlight(btnCmdAttack, currentMode == UnitActionMode.TitanStrike);
                if (txtCmdAttack != null)
                {
                    txtCmdAttack.text = unit.IsRangedAttack ? "🏹 Attack" : "⚔️ Attack";
                }
            }

            // 3. Special
            if (btnCmdSpecial != null)
            {
                btnCmdSpecial.interactable = canCombat;
                SetButtonActiveHighlight(btnCmdSpecial, specialSubmenuRoot != null && specialSubmenuRoot.activeSelf);
                if (txtCmdSpecial != null) txtCmdSpecial.text = "✨ Special";
            }

            // 4. Defend (Guard stance)
            if (btnCmdDefend != null)
            {
                btnCmdDefend.interactable = isPlayerTurn && !unit.HasActedThisTurn;
                if (txtCmdDefend != null) txtCmdDefend.text = "🛡️ Defend";
            }

            // 5. Push (Normal 1-hex kinetic shove)
            if (btnCmdPush != null)
            {
                btnCmdPush.interactable = canCombat;
                SetButtonActiveHighlight(btnCmdPush, currentMode == UnitActionMode.KineticPush);
                if (txtCmdPush != null)
                {
                    txtCmdPush.text = (isTitan ? "🐊 Tail Shove" : "💨 Push");
                }
            }

            // 6. Absorb (Contextual: visible when unit can harvest/siphon cores, like Demon Lord or Titans)
            if (btnCmdAbsorb != null)
            {
                bool showAbsorb = !isTutorial1 && (isCommander || isTitan || unit.ElementalCores > 0);
                btnCmdAbsorb.gameObject.SetActive(showAbsorb);
                if (showAbsorb)
                {
                    btnCmdAbsorb.interactable = canCombat && !unit.HasSiphonedThisTurn;
                    SetButtonActiveHighlight(btnCmdAbsorb, currentMode == UnitActionMode.ConsumeLand);
                    if (txtCmdAbsorb != null) txtCmdAbsorb.text = "⚡ Absorb";
                }
            }

            // 7. Item (Inventory potions)
            if (btnCmdItem != null)
            {
                btnCmdItem.interactable = isPlayerTurn && !unit.HasActedThisTurn;
                SetButtonActiveHighlight(btnCmdItem, itemSubmenuRoot != null && itemSubmenuRoot.activeSelf);
                if (txtCmdItem != null) txtCmdItem.text = "🎒 Item";
            }

            // 8. Wait
            if (btnCmdWait != null)
            {
                btnCmdWait.interactable = isPlayerTurn && (!unit.HasActedThisTurn || !unit.HasMovedThisTurn);
            }

            // 9. Cancel
            if (btnCmdCancel != null)
            {
                btnCmdCancel.interactable = true;
            }

            // Populate main command hover metadata and evaluate exact button under mouse cursor
            activeMainCmdSkillEntries.Clear();
            bool isLv99 = unit.MaxHealth >= 500;
            int pushBaseDmg = isLv99 ? (unit.UnitName.Contains("Paladin") ? 230 : 215) : 1;
            int slamBonusDmg = isLv99 ? 550 : 2;
            BindSkillHoverPreview(btnCmdMove, "MOVE", "0 MP", "None", $"Traverse up to {unit.EffectiveMoveRange} Hexes",
                $"Move {unit.UnitName} across reachable tiles. Elevation steps of 1 tier can be climbed.");
            BindSkillHoverPreview(btnCmdAttack, unit.IsRangedAttack ? "RANGED ATTACK" : "BASIC ATTACK", "0 MP", $"{unit.EffectiveAttackDamage} DMG", $"Weapon Strike  •  Range: {unit.AttackRange} Hex",
                $"Deliver a direct weapon strike dealing {unit.EffectiveAttackDamage} physical damage with no Mana cost.");
            BindSkillHoverPreview(btnCmdSpecial, "SPECIAL SKILLS", "10-40 MP", "Varies", "Sacred & Elemental Arts",
                "Open this unit's Special Skills menu to cast high-impact elemental spells, stone pillars, and knockback arts.");
            BindSkillHoverPreview(btnCmdDefend, "DEFEND (GUARD STANCE)", "0 MP", "Self Buff", "Reduces Incoming Damage by 50%",
                "Assume a fortified guard stance until next turn, reducing all incoming physical and elemental damage by 50%.");
            BindSkillHoverPreview(btnCmdPush, isTitan ? "TAIL SHOVE" : "KINETIC PUSH", "0 MP", $"{pushBaseDmg} (+{slamBonusDmg} Slam)", "Knockback 1 Hex + Wall Slam  •  Range: 1",
                $"Shove an adjacent unit backward 1 hex ({pushBaseDmg} DMG). If they slam into a Stone Pillar or obstacle, inflicts +{slamBonusDmg} Wall Slam damage!");
            if (btnCmdAbsorb != null && btnCmdAbsorb.gameObject.activeSelf)
            {
                BindSkillHoverPreview(btnCmdAbsorb, "ABSORB ELEMENTAL CORE", "0 MP", "+1 Core", "Siphon Terrain  •  Range: 1",
                    "Siphon elemental energy from an adjacent Scorched, Flooded, or Pillar tile to gain +1 Elemental Core.");
            }
            BindSkillHoverPreview(btnCmdItem, "INVENTORY ITEMS", "1 Item", "Restore HP / MP", "Consumables  •  Self",
                "Open the tactical inventory to use Healing Elixirs or Mana Ethers.");

            EvaluateHoveredSkillDescription();
            ApplyDisgaeaTutorialPulse();
        }

        private void ShowTargetingModeSkillDescription(TacticalUnit3D unit, HexGridInteraction3D interaction, UnitActionMode mode)
        {
            if (unit == null) return;
            bool isLv99 = unit.MaxHealth >= 500;
            string skillId = interaction != null ? interaction.PendingSkillId : null;
            int customDmg = (interaction != null && interaction.PendingSkillDamage > 0) ? interaction.PendingSkillDamage : -1;
            int manaCost = (interaction != null && interaction.PendingSkillManaCost > 0) ? interaction.PendingSkillManaCost : 0;
            string costStr = manaCost > 0 ? $"{manaCost} MP" : "0 MP";

            Grid.HexTile3D hovered = interaction != null ? interaction.CurrentHoveredTile : null;
            string hoverCoordTag = hovered != null
                ? $"  ->  HEX ({hovered.Coordinates.Q}, {hovered.Coordinates.R}) [{GetReadableTileStateName(hovered)}]"
                : "";

            if (mode == UnitActionMode.Move)
            {
                ShowSkillDescriptionWindow("MOVE" + hoverCoordTag, "0 MP", "None",
                    $"Reachable Range: {unit.EffectiveMoveRange} Hexes" + (hovered != null ? $"  •  Elevation: Tier {hovered.Elevation}" : ""),
                    hovered != null
                        ? $"Click HEX ({hovered.Coordinates.Q}, {hovered.Coordinates.R}) to move {unit.UnitName} to that tile (Right-Click to Cancel)."
                        : $"Click a highlighted blue hex within {unit.EffectiveMoveRange} tiles to move {unit.UnitName} (Right-Click to Cancel).");
                return;
            }

            if (skillId == "JudgmentBeam")
            {
                ShowSkillDescriptionWindow("⚡ JUDGMENT BEAM", costStr, "999 DMG", "Orbital Piercing Beam • Rng 1-3",
                    "🎯 [TARGETING] Click a highlighted hex within 3 tiles to call down an orbital satellite beam (999 DMG + Scorches Tile). Right-Click to cancel.");
                return;
            }
            if (skillId == "HeavensPillar")
            {
                ShowSkillDescriptionWindow("⛰️ HEAVEN'S PILLAR", costStr, "640 (+550 Slam)", "Stone Pillar + Launch • Rng 1-3",
                    "🎯 [TARGETING] Click a target hex to erupt a sacred Stone Pillar (640 DMG), launching any occupant 1 hex (+550 Wall Slam if blocked)!");
                return;
            }
            if (skillId == "SacredBulwark")
            {
                ShowSkillDescriptionWindow("⛰️ SACRED BULWARK", costStr, "590 (+550 Slam)", "Stone Pillar + Launch • Rng 1-3",
                    "🎯 [TARGETING] Click a target hex to raise a consecrated Stone Pillar (590 DMG), displacing the foe and creating a Wall Slam obstacle!");
                return;
            }
            if (skillId == "CleansingFlood" || skillId == "SanctifyingWave")
            {
                int wDmg = customDmg > 0 ? customDmg : (isLv99 ? 520 : 2);
                string wTitle = skillId == "CleansingFlood" ? "🌊 CLEANSING FLOOD" : "🌊 SANCTIFYING WAVE";
                ShowSkillDescriptionWindow(wTitle, costStr, $"{wDmg} DMG", "Floods Tile / Steam Mist • Rng 1-3",
                    $"🎯 [TARGETING] Click a target hex to unleash a holy deluge ({wDmg} DMG) that floods the tile or vaporizes fire into Steam.");
                return;
            }

            switch (mode)
            {
                case UnitActionMode.Fireball:
                    int fDmg = customDmg > 0 ? customDmg : (isLv99 ? 780 : 3);
                    string fTitle = (unit.UnitName.Contains("Paladin") ? "RADIANT SMITE" : "HELLFIRE BLAST") + hoverCoordTag;
                    string fReaction = (hovered != null && hovered.State == Grid.TileState.Scorched)
                        ? $"HEX ({hovered.Coordinates.Q}, {hovered.Coordinates.R}) [SCORCHED] -> ERUPTS INTO MOLTEN MAGMA!"
                        : "Scorches Tile (Fire)  •  Range: 1-3";
                    ShowSkillDescriptionWindow(fTitle, manaCost > 0 ? costStr : (isLv99 ? "25 MP" : "15 MP"), $"{fDmg} DMG", fReaction,
                        $"Click a highlighted hex within 3 tiles to deal {fDmg} Fire damage and ignite the terrain (Scorched Earth erupts into Molten Magma!).");
                    break;
                case UnitActionMode.WaterSurge:
                    int wDmg = customDmg > 0 ? customDmg : (isLv99 ? 460 : 2);
                    ShowSkillDescriptionWindow("💧 WATER SURGE", manaCost > 0 ? costStr : "15 MP", $"{wDmg} DMG", "Floods Tile (Water) • Rng 1-3",
                        $"🎯 [TARGETING] Click a highlighted hex within 3 tiles to deal {wDmg} Water damage and flood the tile (or quench Fire into Steam).");
                    break;
                case UnitActionMode.EarthSpire:
                    int eDmg = customDmg > 0 ? customDmg : (isLv99 ? 590 : 2);
                    int eSlam = isLv99 ? 550 : 2;
                    ShowSkillDescriptionWindow("⛰️ EARTH PILLAR", manaCost > 0 ? costStr : "15 MP", $"{eDmg} (+{eSlam} Slam)", "Stone Pillar + Launch • Rng 1-3",
                        $"🎯 [TARGETING] Click a highlighted hex to erupt a 3D Stone Pillar ({eDmg} DMG) and launch any occupant 1 hex.");
                    break;
                case UnitActionMode.KineticPush:
                    int pDmg = customDmg > 0 ? customDmg : (isLv99 ? (unit.UnitName.Contains("Paladin") ? 230 : 215) : 1);
                    int pSlam = isLv99 ? 550 : 2;
                    string pTitle = unit.UnitName.Contains("Paladin") ? "⚜️ HOLY REPEL" : (unit.UnitName.Contains("Shielder") ? "🛡️ SHIELD SHOVE" : "💨 KINETIC PUSH");
                    ShowSkillDescriptionWindow(pTitle, costStr, $"{pDmg} (+{pSlam} Slam)", "Push 1 Hex + Wall Slam • Rng 1",
                        $"🎯 [TARGETING] Click an adjacent unit to shove them 1 hex ({pDmg} DMG). Slams into pillars or obstacles for +{pSlam} bonus damage!");
                    break;
                case UnitActionMode.TitanStrike:
                    ShowSkillDescriptionWindow("⚔️ BASIC ATTACK", "0 MP", $"{unit.EffectiveAttackDamage} DMG", $"Weapon Strike • Rng {unit.AttackRange}",
                        $"🎯 [TARGETING] Click an enemy within {unit.AttackRange} hex(es) to strike for {unit.EffectiveAttackDamage} damage.");
                    break;
                case UnitActionMode.MagmaCataclysm:
                    ShowSkillDescriptionWindow("🌋 MAGMA CATACLYSM", "1 Core", "4 AoE DMG", "3x3 Volcanic Eruption • Rng 1-2",
                        "🎯 [TARGETING] Click a center hex to unleash a 3x3 volcanic cataclysm dealing 4 AoE damage and igniting tiles into Magma!");
                    break;
            }
        }

        private void OnCmdSpecialClicked()
        {
            SoundManager3D.Instance?.PlayButtonClick();
            HexGridInteraction3D interaction = HexGridInteraction3D.Instance;
            if (interaction == null) return;

            TacticalUnit3D sel = interaction.CurrentSelectedUnit;
            if (sel == null) return;

            if (specialSubmenuRoot != null && specialSubmenuRoot.activeSelf)
            {
                specialSubmenuRoot.SetActive(false);
            }
            else
            {
                OpenSpecialSubmenu(sel);
            }
        }

        private void OnCmdDefendClicked()
        {
            SoundManager3D.Instance?.PlayButtonClick();
            HexGridInteraction3D interaction = HexGridInteraction3D.Instance;
            if (interaction == null) return;

            TacticalUnit3D sel = interaction.CurrentSelectedUnit;
            if (sel != null && !sel.HasActedThisTurn)
            {
                sel.ExecuteDefend();
                CloseAllSubmenus();
                if (commandMenuRoot != null) commandMenuRoot.SetActive(false);
                interaction.ClearUnitSelection();
                interaction.SetActionMode(UnitActionMode.None);
            }
        }

        private void OnCmdItemClicked()
        {
            SoundManager3D.Instance?.PlayButtonClick();
            HexGridInteraction3D interaction = HexGridInteraction3D.Instance;
            if (interaction == null) return;

            TacticalUnit3D sel = interaction.CurrentSelectedUnit;
            if (sel == null) return;

            if (itemSubmenuRoot != null && itemSubmenuRoot.activeSelf)
            {
                itemSubmenuRoot.SetActive(false);
            }
            else
            {
                OpenItemSubmenu(sel);
            }
        }

        private void OpenSpecialSubmenu(TacticalUnit3D unit)
        {
            if (unit == null) return;
            if (itemSubmenuRoot != null) itemSubmenuRoot.SetActive(false);
            if (specialSubmenuRoot == null) return;

            specialSubmenuRoot.SetActive(true);
            specialSubmenuRoot.transform.SetAsLastSibling();

            PopulateSpecialSkills(unit);
            UpdateSubmenuPosition(specialSubmenuRoot);
        }

        private void PopulateSpecialSkills(TacticalUnit3D unit)
        {
            if (specialSubmenuContent == null) return;
            activeSubmenuSkillEntries.Clear();

            for (int i = specialSubmenuContent.childCount - 1; i >= 0; i--)
            {
                Destroy(specialSubmenuContent.GetChild(i).gameObject);
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Sprite btnSprite = TacticalUnitSpawner.LoadUISprite("UI_Button_Normal.png");

            bool isPlayerTurn = TurnManager3D.Instance != null && TurnManager3D.Instance.CurrentPhase == TurnPhase.PlayerTurn;
            bool canCombat = isPlayerTurn && !unit.HasActedThisTurn;
            bool isCommander = (unit.Archetype == UnitArchetype.Commander);
            bool isTitan = (unit.Archetype == UnitArchetype.Titan);
            bool isPaladin = unit.UnitName.Contains("Paladin");
            bool isShielder = unit.UnitName.Contains("Shielder") || unit.UnitName.Contains("Holy Shielder");
            bool isTutorial1 = Tutorial.TutorialScenarioManager.Instance != null && Tutorial.TutorialScenarioManager.Instance.IsStage1Active;
            bool isLv99 = unit.MaxHealth >= 500;

            int skillCount = 0;

            // =========================================================================
            // 1. CHOSEN HERO (PALADIN, LV 99) — 5 SACRED CRUSADE SKILLS
            // =========================================================================
            if (isPaladin)
            {
                // Skill 1: Radiant Smite (25 MP, 780 DMG, Scorches Tile)
                skillCount++;
                int smiteDmg = isLv99 ? 780 : 3;
                int smiteMp = 25;
                Button btnSmite = CreateBtn("Btn_Skill_Fireball_Smite", specialSubmenuContent, $"☀️ Radiant Smite ({smiteMp} MP)", font, 11, btnSprite, new Color(0.78f, 0.54f, 0.14f, 1f), 164f, 32f);
                btnSmite.interactable = canCombat && unit.CurrentMana >= smiteMp;
                string sTitle = "☀️ RADIANT SMITE";
                string sCost = $"{smiteMp} MP";
                string sDmg = $"{smiteDmg} DMG";
                string sEff = "Scorches Tile (Fire) • Rng 1-3";
                string sDesc = $"Calls down a pillar of solar judgment. Deals {smiteDmg} Holy Fire damage and ignites the target hex into Scorched Earth.";
                BindSkillHoverPreview(btnSmite, sTitle, sCost, sDmg, sEff, sDesc);
                ShowSkillDescriptionWindow(sTitle, sCost, sDmg, sEff, sDesc); // Default preview on open
                btnSmite.onClick.AddListener(() =>
                {
                    CloseAllSubmenus();
                    SoundManager3D.Instance?.PlayButtonClick();
                    HexGridInteraction3D.Instance?.SetActionModeWithSkill(UnitActionMode.Fireball, "RadiantSmite", smiteDmg, smiteMp);
                });

                // Skill 2: Judgment Beam (40 MP, 999 DMG Ultimate, Orbital Satellite Ray)
                skillCount++;
                int beamDmg = isLv99 ? 999 : 4;
                int beamMp = 40;
                Button btnBeam = CreateBtn("Btn_Skill_JudgmentBeam", specialSubmenuContent, $"⚡ Judgment Beam ({beamMp} MP)", font, 11, btnSprite, new Color(0.82f, 0.64f, 0.16f, 1f), 164f, 32f);
                btnBeam.interactable = canCombat && unit.CurrentMana >= beamMp;
                BindSkillHoverPreview(btnBeam,
                    "⚡ JUDGMENT BEAM", $"{beamMp} MP", $"{beamDmg} DMG (MAX)", "Orbital Piercing Beam • Rng 1-3",
                    $"Ultimate Sacred Art: Channels an orbital satellite beam of pure divinity that hits the {beamDmg} damage cap and scorches the earth.");
                btnBeam.onClick.AddListener(() =>
                {
                    CloseAllSubmenus();
                    SoundManager3D.Instance?.PlayButtonClick();
                    HexGridInteraction3D.Instance?.SetActionModeWithSkill(UnitActionMode.Fireball, "JudgmentBeam", beamDmg, beamMp);
                });

                // Skill 3: Heaven's Pillar (20 MP, 640 DMG + 550 Slam, Stone Pillar Eruption)
                skillCount++;
                int pillarDmg = isLv99 ? 640 : 2;
                int pillarSlam = isLv99 ? 550 : 2;
                int pillarMp = 20;
                Button btnPillar = CreateBtn("Btn_Skill_HeavensPillar", specialSubmenuContent, $"⛰️ Heaven's Pillar ({pillarMp} MP)", font, 11, btnSprite, new Color(0.55f, 0.40f, 0.18f, 1f), 164f, 32f);
                btnPillar.interactable = canCombat && unit.CurrentMana >= pillarMp;
                BindSkillHoverPreview(btnPillar,
                    "⛰️ HEAVEN'S PILLAR", $"{pillarMp} MP", $"{pillarDmg} (+{pillarSlam} Slam)", "Stone Pillar + Launch • Rng 1-3",
                    $"Erupts a sacred monolith beneath the foe, dealing {pillarDmg} damage, launching them 1 hex, and raising a Stone Pillar obstacle.");
                btnPillar.onClick.AddListener(() =>
                {
                    CloseAllSubmenus();
                    SoundManager3D.Instance?.PlayButtonClick();
                    HexGridInteraction3D.Instance?.SetActionModeWithSkill(UnitActionMode.EarthSpire, "HeavensPillar", pillarDmg, pillarMp);
                });

                // Skill 4: Cleansing Flood (15 MP, 520 DMG, Floods Tile / Steam Mist)
                skillCount++;
                int floodDmg = isLv99 ? 520 : 2;
                int floodMp = 15;
                Button btnFlood = CreateBtn("Btn_Skill_CleansingFlood", specialSubmenuContent, $"🌊 Cleansing Flood ({floodMp} MP)", font, 11, btnSprite, new Color(0.16f, 0.48f, 0.72f, 1f), 164f, 32f);
                btnFlood.interactable = canCombat && unit.CurrentMana >= floodMp;
                BindSkillHoverPreview(btnFlood,
                    "🌊 CLEANSING FLOOD", $"{floodMp} MP", $"{floodDmg} DMG", "Floods Tile (Water) / Steam • Rng 1-3",
                    $"Summons a consecrated deluge that deals {floodDmg} Water damage and floods the tile, or vaporizes Scorched Earth into Steam mist.");
                btnFlood.onClick.AddListener(() =>
                {
                    CloseAllSubmenus();
                    SoundManager3D.Instance?.PlayButtonClick();
                    HexGridInteraction3D.Instance?.SetActionModeWithSkill(UnitActionMode.WaterSurge, "CleansingFlood", floodDmg, floodMp);
                });

                // Skill 5: Holy Repel (10 MP, 230 DMG + 550 Wall Slam, Knockback 1 Hex)
                skillCount++;
                int repelDmg = isLv99 ? 230 : 1;
                int repelSlam = isLv99 ? 550 : 2;
                int repelMp = 10;
                Button btnRepel = CreateBtn("Btn_Skill_Repel", specialSubmenuContent, $"⚜️ Holy Repel ({repelMp} MP)", font, 11, btnSprite, new Color(0.38f, 0.52f, 0.68f, 1f), 164f, 32f);
                btnRepel.interactable = canCombat && unit.CurrentMana >= repelMp;
                BindSkillHoverPreview(btnRepel,
                    "⚜️ HOLY REPEL", $"{repelMp} MP", $"{repelDmg} (+{repelSlam} Slam)", "Knockback 1 Hex + Wall Slam • Rng 1",
                    $"Blasts an adjacent enemy backward 1 hex with a radiant shockwave ({repelDmg} DMG). Slams into pillars or obstacles for +{repelSlam} bonus damage!");
                btnRepel.onClick.AddListener(() =>
                {
                    CloseAllSubmenus();
                    SoundManager3D.Instance?.PlayButtonClick();
                    HexGridInteraction3D.Instance?.SetActionModeWithSkill(UnitActionMode.KineticPush, "HolyRepel", repelDmg, repelMp);
                });
            }
            // =========================================================================
            // 2. HOLY SHIELDER (LV 99) — 4 FORTRESS VANGUARD SKILLS
            // =========================================================================
            else if (isShielder)
            {
                // Skill 1: Shield Shove (10 MP, 215 DMG + 550 Wall Slam)
                skillCount++;
                int shoveDmg = isLv99 ? 215 : 1;
                int shoveSlam = isLv99 ? 550 : 2;
                int shoveMp = 10;
                Button btnShove = CreateBtn("Btn_Skill_ShieldShove", specialSubmenuContent, $"🛡️ Shield Shove ({shoveMp} MP)", font, 11, btnSprite, new Color(0.45f, 0.25f, 0.65f, 1f), 164f, 32f);
                btnShove.interactable = canCombat && unit.CurrentMana >= shoveMp;
                string shTitle = "🛡️ SHIELD SHOVE";
                string shCost = $"{shoveMp} MP";
                string shDmg = $"{shoveDmg} (+{shoveSlam} Slam)";
                string shEff = "Knockback 1 Hex + Wall Slam • Rng 1";
                string shDesc = $"Rams an adjacent enemy with a consecrated tower shield ({shoveDmg} DMG), shoving them 1 hex. Inflicts +{shoveSlam} bonus damage on Wall Slam!";
                BindSkillHoverPreview(btnShove, shTitle, shCost, shDmg, shEff, shDesc);
                ShowSkillDescriptionWindow(shTitle, shCost, shDmg, shEff, shDesc); // Default preview on open
                btnShove.onClick.AddListener(() =>
                {
                    CloseAllSubmenus();
                    SoundManager3D.Instance?.PlayButtonClick();
                    HexGridInteraction3D.Instance?.SetActionModeWithSkill(UnitActionMode.KineticPush, "ShieldShove", shoveDmg, shoveMp);
                });

                // Skill 2: Sacred Bulwark (20 MP, 590 DMG + 550 Slam, Erects Stone Pillar)
                skillCount++;
                int bulwarkDmg = isLv99 ? 590 : 2;
                int bulwarkSlam = isLv99 ? 550 : 2;
                int bulwarkMp = 20;
                Button btnBulwark = CreateBtn("Btn_Skill_SacredBulwark", specialSubmenuContent, $"⛰️ Sacred Bulwark ({bulwarkMp} MP)", font, 11, btnSprite, new Color(0.55f, 0.40f, 0.18f, 1f), 164f, 32f);
                btnBulwark.interactable = canCombat && unit.CurrentMana >= bulwarkMp;
                BindSkillHoverPreview(btnBulwark,
                    "⛰️ SACRED BULWARK", $"{bulwarkMp} MP", $"{bulwarkDmg} (+{bulwarkSlam} Slam)", "Stone Pillar + Launch • Rng 1-3",
                    $"Raises a consecrated stone monolith from the ground. Deals {bulwarkDmg} damage, displaces the target 1 hex, and creates a Wall Slam pillar!");
                btnBulwark.onClick.AddListener(() =>
                {
                    CloseAllSubmenus();
                    SoundManager3D.Instance?.PlayButtonClick();
                    HexGridInteraction3D.Instance?.SetActionModeWithSkill(UnitActionMode.EarthSpire, "SacredBulwark", bulwarkDmg, bulwarkMp);
                });

                // Skill 3: Sanctifying Wave (15 MP, 460 DMG, Floods Tile / Steam)
                skillCount++;
                int waveDmg = isLv99 ? 460 : 2;
                int waveMp = 15;
                Button btnWave = CreateBtn("Btn_Skill_SanctifyingWave", specialSubmenuContent, $"🌊 Sanctifying Wave ({waveMp} MP)", font, 11, btnSprite, new Color(0.16f, 0.48f, 0.72f, 1f), 164f, 32f);
                btnWave.interactable = canCombat && unit.CurrentMana >= waveMp;
                BindSkillHoverPreview(btnWave,
                    "🌊 SANCTIFYING WAVE", $"{waveMp} MP", $"{waveDmg} DMG", "Floods Tile (Water) / Steam • Rng 1-3",
                    $"Unleashes a tidal surge of holy water that deals {waveDmg} damage and floods the hex, or vaporizes fire into protective Steam mist.");
                btnWave.onClick.AddListener(() =>
                {
                    CloseAllSubmenus();
                    SoundManager3D.Instance?.PlayButtonClick();
                    HexGridInteraction3D.Instance?.SetActionModeWithSkill(UnitActionMode.WaterSurge, "SanctifyingWave", waveDmg, waveMp);
                });

                // Skill 4: Aegis Bastion (15 MP, Self Heal +350 HP & Guard Stance -50% DMG)
                skillCount++;
                int aegisMp = 15;
                int aegisHeal = isLv99 ? 350 : 4;
                Button btnAegis = CreateBtn("Btn_Skill_AegisBastion", specialSubmenuContent, $"✨ Aegis Bastion ({aegisMp} MP)", font, 11, btnSprite, new Color(0.22f, 0.58f, 0.52f, 1f), 164f, 32f);
                btnAegis.interactable = canCombat && unit.CurrentMana >= aegisMp;
                BindSkillHoverPreview(btnAegis,
                    "✨ AEGIS BASTION", $"{aegisMp} MP", $"+{aegisHeal} HP + Guard", "Guard (-50% DMG) + Heal • Self",
                    $"Channels an impenetrable golden phalanx barrier, restoring {aegisHeal} HP immediately and halving all incoming damage until next turn.");
                btnAegis.onClick.AddListener(() =>
                {
                    unit.ConsumeMana(aegisMp);
                    SpellVFXManager3D.Instance?.PlayAegisBastionVFX(unit.transform.position);
                    unit.Heal(aegisHeal, $"+{aegisHeal} HP");
                    unit.ExecuteDefend();
                    CloseAllSubmenus();
                    HideSkillDescriptionWindow();
                    if (commandMenuRoot != null) commandMenuRoot.SetActive(false);
                    HexGridInteraction3D.Instance?.ClearUnitSelection();
                    HexGridInteraction3D.Instance?.SetActionMode(UnitActionMode.None);
                });
            }
            // =========================================================================
            // 3. DEMON LORD / COMMANDER & ELEMENTAL VANGUARD SKILLS
            // =========================================================================
            else if (isCommander || unit.Affinity == ElementalAffinity.Fire)
            {
                if (!isTitan)
                {
                    skillCount++;
                    int fbMp = 15;
                    Button btnFire = CreateBtn("Btn_Skill_Fireball", specialSubmenuContent, $"🔥 Hellfire Blast ({fbMp} MP)", font, 11, btnSprite, new Color(0.72f, 0.35f, 0.12f, 1f), 164f, 32f);
                    btnFire.interactable = canCombat && unit.CurrentMana >= fbMp;
                    string fTitle = "🔥 HELLFIRE BLAST (FIREBALL)";
                    string fCost = $"{fbMp} MP";
                    string fDmg = "3 DMG";
                    string fEff = "Scorches Tile / Magma • Rng 1-3";
                    string fDesc = "Hurls a volatile orb of abyssal hellfire. Deals 3 Fire damage and scorches the tile (or erupts Scorched Earth into Molten Magma!).";
                    BindSkillHoverPreview(btnFire, fTitle, fCost, fDmg, fEff, fDesc);
                    ShowSkillDescriptionWindow(fTitle, fCost, fDmg, fEff, fDesc);
                    btnFire.onClick.AddListener(() =>
                    {
                        CloseAllSubmenus();
                        SoundManager3D.Instance?.PlayButtonClick();
                        HexGridInteraction3D.Instance?.SetActionModeWithSkill(UnitActionMode.Fireball, "HellfireBlast", 3, fbMp);
                    });

                    skillCount++;
                    int spMp = 15;
                    Button btnObsidian = CreateBtn("Btn_Skill_ObsidianSpire", specialSubmenuContent, $"⛰️ Obsidian Spire ({spMp} MP)", font, 11, btnSprite, new Color(0.45f, 0.32f, 0.18f, 1f), 164f, 32f);
                    btnObsidian.interactable = canCombat && unit.CurrentMana >= spMp;
                    BindSkillHoverPreview(btnObsidian,
                        "⛰️ OBSIDIAN SPIRE", $"{spMp} MP", "2 (+2 Slam)", "Stone Pillar + Launch • Rng 1-3",
                        "Erupts a jagged obsidian pillar beneath the target, dealing 2 damage, displacing them 1 hex, and raising a Wall Slam obstacle.");
                    btnObsidian.onClick.AddListener(() =>
                    {
                        CloseAllSubmenus();
                        SoundManager3D.Instance?.PlayButtonClick();
                        HexGridInteraction3D.Instance?.SetActionModeWithSkill(UnitActionMode.EarthSpire, "ObsidianSpire", 2, spMp);
                    });
                }
            }

            // 4. Water Affinity Skills (Stage 2/3)
            if (!isPaladin && !isShielder && !isCommander && unit.Affinity == ElementalAffinity.Water && !isTutorial1)
            {
                skillCount++;
                int wMp = 12;
                Button btnWater = CreateBtn("Btn_Skill_WaterSurge", specialSubmenuContent, $"💧 Water Surge ({wMp} MP)", font, 11, btnSprite, new Color(0.15f, 0.45f, 0.70f, 1f), 164f, 32f);
                btnWater.interactable = canCombat && unit.CurrentMana >= wMp;
                string wTitle = "💧 WATER SURGE";
                string wCost = $"{wMp} MP";
                string wDmg = "2 DMG";
                string wEff = "Floods Tile / Steam • Rng 1-3";
                string wDesc = "Unleashes a pressurized torrent dealing 2 Water damage. Floods the tile (-1 MOV) or vaporizes Fire into obscuring Steam mist.";
                BindSkillHoverPreview(btnWater, wTitle, wCost, wDmg, wEff, wDesc);
                ShowSkillDescriptionWindow(wTitle, wCost, wDmg, wEff, wDesc);
                btnWater.onClick.AddListener(() =>
                {
                    CloseAllSubmenus();
                    SoundManager3D.Instance?.PlayButtonClick();
                    HexGridInteraction3D.Instance?.SetActionModeWithSkill(UnitActionMode.WaterSurge, "WaterSurge", 2, wMp);
                });
            }

            // 5. Earth Affinity Skills (Stage 2/3, e.g. Basalt Vanguard)
            if (!isPaladin && !isShielder && !isCommander && unit.Affinity == ElementalAffinity.Earth && !isTutorial1)
            {
                skillCount++;
                int eMp = 15;
                Button btnEarth = CreateBtn("Btn_Skill_EarthSpire", specialSubmenuContent, $"⛰️ Earth Pillar ({eMp} MP)", font, 11, btnSprite, new Color(0.45f, 0.32f, 0.18f, 1f), 164f, 32f);
                btnEarth.interactable = canCombat && unit.CurrentMana >= eMp;
                string eTitle = "⛰️ EARTH PILLAR";
                string eCost = $"{eMp} MP";
                string eDmg = "2 (+2 Slam)";
                string eEff = "Stone Pillar + Launch • Rng 1-3";
                string eDesc = "Raises a solid Stone Pillar at the target tile (2 DMG), launching any occupant 1 hex and creating a permanent Wall Slam obstacle.";
                BindSkillHoverPreview(btnEarth, eTitle, eCost, eDmg, eEff, eDesc);
                ShowSkillDescriptionWindow(eTitle, eCost, eDmg, eEff, eDesc);
                btnEarth.onClick.AddListener(() =>
                {
                    CloseAllSubmenus();
                    SoundManager3D.Instance?.PlayButtonClick();
                    HexGridInteraction3D.Instance?.SetActionModeWithSkill(UnitActionMode.EarthSpire, "EarthSpire", 2, eMp);
                });

                skillCount++;
                int gMp = 10;
                Button btnSlam = CreateBtn("Btn_Skill_GolemSlam", specialSubmenuContent, $"💥 Seismic Shove ({gMp} MP)", font, 11, btnSprite, new Color(0.48f, 0.25f, 0.62f, 1f), 164f, 32f);
                btnSlam.interactable = canCombat && unit.CurrentMana >= gMp;
                BindSkillHoverPreview(btnSlam,
                    "💥 SEISMIC SHOVE", $"{gMp} MP", "1 (+2 Wall Slam)", "Knockback 1 Hex + Slam • Rng 1",
                    "Delivers a crushing tectonic shove that knocks an adjacent enemy backward 1 hex into hazards or pillars (+2 Wall Slam damage).");
                btnSlam.onClick.AddListener(() =>
                {
                    CloseAllSubmenus();
                    SoundManager3D.Instance?.PlayButtonClick();
                    HexGridInteraction3D.Instance?.SetActionModeWithSkill(UnitActionMode.KineticPush, "SeismicShove", 1, gMp);
                });
            }

            // 6. Titan Colossal Skills
            if (isTitan && !isTutorial1)
            {
                skillCount++;
                bool canCataclysm = canCombat && unit.ElementalCores >= 1;
                string cataLabel = (unit.ElementalCores >= 1) ? "🌋 Magma Cataclysm" : "🌋 Cataclysm (1 Core)";
                Button btnCata = CreateBtn("Btn_Skill_Cataclysm", specialSubmenuContent, cataLabel, font, 11, btnSprite, new Color(0.85f, 0.22f, 0.10f, 1f), 164f, 32f);
                btnCata.interactable = canCataclysm;
                string cTitle = "🌋 MAGMA CATACLYSM";
                string cCost = "1 Elemental Core";
                string cDmg = "4 AoE DMG";
                string cEff = "3x3 Volcanic Eruption • Rng 1-2";
                string cDesc = "Consumes 1 Elemental Core to detonate a 3x3 volcanic cataclysm, dealing 4 Fire damage to all foes in range and creating Molten Magma!";
                BindSkillHoverPreview(btnCata, cTitle, cCost, cDmg, cEff, cDesc);
                ShowSkillDescriptionWindow(cTitle, cCost, cDmg, cEff, cDesc);
                btnCata.onClick.AddListener(() =>
                {
                    CloseAllSubmenus();
                    OnActionClicked(UnitActionMode.MagmaCataclysm);
                });

                skillCount++;
                Button btnTail = CreateBtn("Btn_Skill_TailShove", specialSubmenuContent, "🐊 Tail Shove (0 MP)", font, 11, btnSprite, new Color(0.50f, 0.25f, 0.65f, 1f), 164f, 32f);
                btnTail.interactable = canCombat;
                BindSkillHoverPreview(btnTail,
                    "🐊 COLOSSAL TAIL SHOVE", "0 MP", "1 (+2 Wall Slam)", "Knockback 1 Hex + Slam • Rng 1",
                    "Sweeps an adjacent enemy backward 1 hex with colossal force. Slams into obstacles or Magma hazards for massive bonus damage!");
                btnTail.onClick.AddListener(() =>
                {
                    CloseAllSubmenus();
                    OnActionClicked(UnitActionMode.KineticPush);
                });
            }

            if (skillCount == 0)
            {
                CreateTxt("Txt_NoSkills", specialSubmenuContent, "<i>(No Special Skills)</i>", font, 11, new Color(0.6f, 0.6f, 0.65f), TextAnchor.MiddleCenter);
            }
        }

        private void OpenItemSubmenu(TacticalUnit3D unit)
        {
            if (unit == null) return;
            if (specialSubmenuRoot != null) specialSubmenuRoot.SetActive(false);
            if (itemSubmenuRoot == null) return;

            itemSubmenuRoot.SetActive(true);
            itemSubmenuRoot.transform.SetAsLastSibling();

            PopulateItems(unit);
            UpdateSubmenuPosition(itemSubmenuRoot);
        }

        private void PopulateItems(TacticalUnit3D unit)
        {
            if (itemSubmenuContent == null) return;
            activeSubmenuSkillEntries.Clear();

            for (int i = itemSubmenuContent.childCount - 1; i >= 0; i--)
            {
                Destroy(itemSubmenuContent.GetChild(i).gameObject);
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Sprite btnSprite = TacticalUnitSpawner.LoadUISprite("UI_Button_Normal.png");

            bool isPlayerTurn = TurnManager3D.Instance != null && TurnManager3D.Instance.CurrentPhase == TurnPhase.PlayerTurn;
            bool isLv99 = unit.MaxHealth >= 500;
            int healAmt = isLv99 ? 500 : 5;
            bool canUseHpPot = isPlayerTurn && !unit.HasActedThisTurn && unit.CurrentHealth < unit.MaxHealth;

            string potLabel = (unit.CurrentHealth >= unit.MaxHealth) ? "🧪 Elixir (HP Full)" : $"🧪 Healing Elixir (+{healAmt} HP)";
            Button btnPot = CreateBtn("Btn_Item_Potion", itemSubmenuContent, potLabel, font, 11, btnSprite, new Color(0.18f, 0.55f, 0.30f, 1f), 164f, 32f);
            btnPot.interactable = canUseHpPot;
            string pTitle = "🧪 HEALING ELIXIR";
            string pCost = "1 Consumable";
            string pDmg = $"+{healAmt} HP";
            string pEff = "Instant Vitality Restore • Self";
            string pDesc = $"Drinks a concentrated alchemical draught that immediately restores {healAmt} HP to {unit.UnitName}.";
            BindSkillHoverPreview(btnPot, pTitle, pCost, pDmg, pEff, pDesc);
            ShowSkillDescriptionWindow(pTitle, pCost, pDmg, pEff, pDesc);
            btnPot.onClick.AddListener(() =>
            {
                SoundManager3D.Instance?.PlayCoreAbsorb();
                unit.Heal(healAmt, $"+{healAmt} HP");
                unit.HasActedThisTurn = true;
                CloseAllSubmenus();
                HideSkillDescriptionWindow();
                if (unit.IsExhausted)
                {
                    HexGridInteraction3D.Instance?.ClearUnitSelection();
                }
            });

            int mpAmt = isLv99 ? 50 : 25;
            bool canUseMpPot = isPlayerTurn && !unit.HasActedThisTurn && unit.CurrentMana < unit.MaxMana;
            string etherLabel = (unit.CurrentMana >= unit.MaxMana) ? "💧 Mana Ether (MP Full)" : $"💧 Mana Ether (+{mpAmt} MP)";
            Button btnEther = CreateBtn("Btn_Item_Ether", itemSubmenuContent, etherLabel, font, 11, btnSprite, new Color(0.16f, 0.42f, 0.68f, 1f), 164f, 32f);
            btnEther.interactable = canUseMpPot;
            BindSkillHoverPreview(btnEther,
                "💧 MANA ETHER", "1 Consumable", $"+{mpAmt} MP", "Instant Mana Restore • Self",
                $"Restores {mpAmt} Mana Points (MP) immediately, allowing {unit.UnitName} to continue casting high-cost Special Skills.");
            btnEther.onClick.AddListener(() =>
            {
                SoundManager3D.Instance?.PlayCoreAbsorb();
                unit.RestoreMana(mpAmt, $"+{mpAmt} MP");
                unit.HasActedThisTurn = true;
                CloseAllSubmenus();
                HideSkillDescriptionWindow();
                if (unit.IsExhausted)
                {
                    HexGridInteraction3D.Instance?.ClearUnitSelection();
                }
            });
        }

        public void UpdateSubmenuPosition(GameObject subMenuRoot)
        {
            if (subMenuRoot == null || commandMenuRoot == null) return;

            RectTransform cmdRect = commandMenuRoot.GetComponent<RectTransform>();
            RectTransform subRect = subMenuRoot.GetComponent<RectTransform>();

            subRect.anchorMin = new Vector2(1f, 1f);
            subRect.anchorMax = new Vector2(1f, 1f);
            subRect.pivot = new Vector2(1f, 1f);

            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(cmdRect);
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(subRect);

            float menuWidth = cmdRect.rect.width > 0 ? cmdRect.rect.width : 175f;

            // Expand sub-menu cleanly to the left of the right-docked Command Menu
            float targetX = cmdRect.anchoredPosition.x - menuWidth - 8f;
            subRect.anchoredPosition = new Vector2(targetX, cmdRect.anchoredPosition.y);
        }

        public void CloseAllSubmenus()
        {
            if (specialSubmenuRoot != null) specialSubmenuRoot.SetActive(false);
            if (itemSubmenuRoot != null) itemSubmenuRoot.SetActive(false);
        }

        public bool IsSubmenuOpen => (specialSubmenuRoot != null && specialSubmenuRoot.activeSelf) || (itemSubmenuRoot != null && itemSubmenuRoot.activeSelf);

        private void OnSpecialBackClicked()
        {
            SoundManager3D.Instance?.PlayButtonClick();
            if (specialSubmenuRoot != null) specialSubmenuRoot.SetActive(false);
        }

        private void OnItemBackClicked()
        {
            SoundManager3D.Instance?.PlayButtonClick();
            if (itemSubmenuRoot != null) itemSubmenuRoot.SetActive(false);
        }

        private void OnCmdWaitClicked()
        {
            SoundManager3D.Instance?.PlayButtonClick();
            HexGridInteraction3D interaction = HexGridInteraction3D.Instance;
            if (interaction == null) return;

            TacticalUnit3D sel = interaction.CurrentSelectedUnit;
            if (sel != null)
            {
                sel.HasActedThisTurn = true;
                sel.HasMovedThisTurn = true;
                CloseAllSubmenus();
                interaction.ClearUnitSelection();
                interaction.SetActionMode(UnitActionMode.None);
            }
        }

        private void OnCmdCancelClicked()
        {
            SoundManager3D.Instance?.PlayButtonClick();
            if (IsSubmenuOpen)
            {
                CloseAllSubmenus();
                return;
            }

            HexGridInteraction3D interaction = HexGridInteraction3D.Instance;
            if (interaction == null) return;

            if (interaction.CurrentActionMode != UnitActionMode.None)
            {
                interaction.SetActionMode(UnitActionMode.None);
                if (interaction.CurrentSelectedUnit != null)
                {
                    TacticalCameraController.Instance?.FocusOnPosition(interaction.CurrentSelectedUnit.transform.position);
                }
            }
            else
            {
                interaction.ClearUnitSelection();
            }
        }

        private void ApplyDisgaeaTutorialPulse()
        {
            if (string.IsNullOrEmpty(activeTutorialButtonKey)) return;

            Button targetBtn = null;
            if (activeTutorialButtonKey == "Move") targetBtn = btnCmdMove;
            else if (activeTutorialButtonKey == "Strike" || activeTutorialButtonKey == "TitanStrike") targetBtn = btnCmdAttack;
            else if (activeTutorialButtonKey == "Fireball" || activeTutorialButtonKey == "WaterSurge" || activeTutorialButtonKey == "EarthPillar" || activeTutorialButtonKey == "Skill" || activeTutorialButtonKey == "Cataclysm") targetBtn = btnCmdSpecial;
            else if (activeTutorialButtonKey == "Push") targetBtn = btnCmdPush;
            else if (activeTutorialButtonKey == "HarvestCore" || activeTutorialButtonKey == "Siphon" || activeTutorialButtonKey == "Absorb") targetBtn = btnCmdAbsorb;

            Button[] menuButtons = new Button[]
            {
                btnCmdMove, btnCmdAttack, btnCmdSpecial, btnCmdDefend, btnCmdPush,
                btnCmdAbsorb, btnCmdItem, btnCmdWait, btnCmdCancel
            };

            bool isPushOrStrike = (activeTutorialButtonKey == "PushOrStrike");
            bool isStrikeOrMove = (activeTutorialButtonKey == "TitanStrike" || activeTutorialButtonKey == "Strike" || activeTutorialButtonKey == "StrikeOrMove");
            bool isStrikeOrSkill = (activeTutorialButtonKey == "Strike" || activeTutorialButtonKey == "TitanStrike" || activeTutorialButtonKey == "Fireball" || activeTutorialButtonKey == "Skill" || activeTutorialButtonKey == "Cataclysm");

            foreach (var btn in menuButtons)
            {
                if (btn == null) continue;
                bool isAllowed = (btn == targetBtn) ||
                                 (isPushOrStrike && (btn == btnCmdPush || btn == btnCmdAttack)) ||
                                 (isStrikeOrMove && (btn == btnCmdAttack || btn == btnCmdMove)) ||
                                 (isStrikeOrSkill && (btn == btnCmdAttack || btn == btnCmdSpecial)) ||
                                 (btn == btnCmdMove) ||
                                 (btn == btnCmdCancel) ||
                                 (btn == btnCmdAttack && (activeTutorialButtonKey == "Strike" || activeTutorialButtonKey == "TitanStrike" || activeTutorialButtonKey == "Move" || activeTutorialButtonKey == "Push"));

                if (isAllowed)
                {
                    btn.interactable = true;
                    if (btn == targetBtn || (isStrikeOrSkill && (btn == btnCmdAttack || btn == btnCmdSpecial)))
                    {
                        float pulse = (Mathf.Sin(Time.unscaledTime * 6f) + 1f) * 0.5f;
                        Color goldPulse = Color.Lerp(Color.white, new Color(1.0f, 0.85f, 0.2f, 1f), pulse);
                        var colors = btn.colors;
                        colors.normalColor = goldPulse;
                        btn.colors = colors;
                    }
                    else
                    {
                        var colors = btn.colors;
                        colors.normalColor = Color.white;
                        btn.colors = colors;
                    }
                }
                else
                {
                    btn.interactable = false;
                }
            }
        }

        public void UpdateDisgaeaCommandMenuPosition(TacticalUnit3D unit)
        {
            if (commandMenuRoot == null || unit == null) return;

            if (txtCmdHeader != null && txtCmdHeader.rectTransform.sizeDelta.y > 30f)
            {
                txtCmdHeader.rectTransform.sizeDelta = new Vector2(154f, 24f);
            }

            RectTransform menuRect = commandMenuRoot.GetComponent<RectTransform>();
            menuRect.anchorMin = new Vector2(1f, 1f);
            menuRect.anchorMax = new Vector2(1f, 1f);
            menuRect.pivot = new Vector2(1f, 1f); // Top-right pivot

            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(menuRect);

            // Dock fixed on the Right side directly underneath the Top-Right 'Menu (Esc)' button (-20, -20, height 42)
            float targetX = -20f;
            float targetY = -72f;

            menuRect.anchoredPosition = new Vector2(targetX, targetY);
        }

        private void LateUpdate()
        {
            if (commandMenuRoot != null && commandMenuRoot.activeSelf)
            {
                HexGridInteraction3D interaction = HexGridInteraction3D.Instance;
                if (interaction != null && interaction.CurrentSelectedUnit != null)
                {
                    TacticalUnit3D sel = interaction.CurrentSelectedUnit;
                    if (sel.IsMoving || interaction.CurrentActionMode != UnitActionMode.None)
                    {
                        commandMenuRoot.SetActive(false);
                        CloseAllSubmenus();
                    }
                    else
                    {
                        UpdateDisgaeaCommandMenuPosition(sel);
                        if (specialSubmenuRoot != null && specialSubmenuRoot.activeSelf)
                        {
                            UpdateSubmenuPosition(specialSubmenuRoot);
                        }
                        if (itemSubmenuRoot != null && itemSubmenuRoot.activeSelf)
                        {
                            UpdateSubmenuPosition(itemSubmenuRoot);
                        }
                    }
                }
            }
        }
    }
}
