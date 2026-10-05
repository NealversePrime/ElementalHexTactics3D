using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ElementalHexTactics3D.InputHandling;
using ElementalHexTactics3D.Units;
using ElementalHexTactics3D.Turn;
using ElementalHexTactics3D.Combat;

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

        [Header("Unit Status Card (Top-Left)")]
        [SerializeField] private GameObject unitCardRoot;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Text unitNameText;
        [SerializeField] private Text unitArchetypeText;
        [SerializeField] private Slider hpSlider;
        [SerializeField] private Image hpFillImage;
        [SerializeField] private Text hpValueText;
        [SerializeField] private Text statsText;
        [SerializeField] private Text turnStatusText;
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
        [SerializeField] private Button btnRandomizeMap;

        // Rift / Base Panel Buttons
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

        [SerializeField] private GameObject commandMenuRoot;
        [SerializeField] private Text txtCmdHeader;
        [SerializeField] private Button btnCmdMove;
        [SerializeField] private Text txtCmdMove;
        [SerializeField] private Button btnCmdAttack;
        [SerializeField] private Text txtCmdAttack;
        [SerializeField] private Button btnCmdSkill;
        [SerializeField] private Text txtCmdSkill;
        [SerializeField] private Button btnCmdPush;
        [SerializeField] private Text txtCmdPush;
        [SerializeField] private Button btnCmdSiphon;
        [SerializeField] private Text txtCmdSiphon;
        [SerializeField] private Button btnCmdCataclysm;
        [SerializeField] private Text txtCmdCataclysm;
        [SerializeField] private Button btnCmdRecall;
        [SerializeField] private Text txtCmdRecall;
        [SerializeField] private Button btnCmdWait;
        [SerializeField] private Button btnCmdCancel;

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

            ApplyRpgMakerWindowStyle(unitCardRoot, new Color(0.08f, 0.07f, 0.14f, 0.95f));
            ApplyRpgMakerWindowStyle(dispatchModalRoot, new Color(0.08f, 0.07f, 0.14f, 0.96f));
            ApplyRpgMakerWindowStyle(commandMenuRoot, new Color(0.07f, 0.06f, 0.12f, 0.96f));
        }

        private void HookExistingContainer(Transform container)
        {
            Transform card = container.Find("Card_UnitStatus");
            if (card != null)
            {
                unitCardRoot = card.gameObject;
                portraitImage = card.Find("Portrait_Frame/Portrait_Image")?.GetComponent<Image>();
                unitNameText = card.Find("Txt_UnitName")?.GetComponent<Text>();
                unitArchetypeText = card.Find("Txt_Archetype")?.GetComponent<Text>();
                turnStatusText = card.Find("Txt_TurnStatus")?.GetComponent<Text>();
                statsText = card.Find("Txt_Stats")?.GetComponent<Text>();
                elementalCoresText = card.Find("Txt_Cores")?.GetComponent<Text>();

                Transform slObj = card.Find("Slider_HP");
                if (slObj != null)
                {
                    hpSlider = slObj.GetComponent<Slider>();
                    hpFillImage = slObj.Find("Fill Area/Fill")?.GetComponent<Image>();
                    hpValueText = slObj.Find("Txt_HPValue")?.GetComponent<Text>();
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

                btnEndTurn = bar.Find("Btn_EndTurn")?.GetComponent<Button>();
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

            // Ensure RPG Maker 2-layer composite styling is applied to all key windows
            ApplyRpgMakerWindowStyle(unitCardRoot, new Color(0.08f, 0.07f, 0.14f, 0.95f));
            ApplyRpgMakerWindowStyle(dispatchModalRoot, new Color(0.08f, 0.07f, 0.14f, 0.96f));
            ApplyRpgMakerWindowStyle(commandMenuRoot, new Color(0.07f, 0.06f, 0.12f, 0.96f));
        }

        private void HookExistingCommandMenu(Transform cmd)
        {
            if (cmd == null) return;
            commandMenuRoot = cmd.gameObject;
            txtCmdHeader = cmd.Find("Txt_CmdHeader")?.GetComponent<Text>();
            btnCmdMove = cmd.Find("Btn_CmdMove")?.GetComponent<Button>();
            txtCmdMove = btnCmdMove?.GetComponentInChildren<Text>();
            btnCmdAttack = cmd.Find("Btn_CmdAttack")?.GetComponent<Button>();
            txtCmdAttack = btnCmdAttack?.GetComponentInChildren<Text>();
            btnCmdSkill = cmd.Find("Btn_CmdSkill")?.GetComponent<Button>();
            txtCmdSkill = btnCmdSkill?.GetComponentInChildren<Text>();
            btnCmdPush = cmd.Find("Btn_CmdPush")?.GetComponent<Button>();
            txtCmdPush = btnCmdPush?.GetComponentInChildren<Text>();
            btnCmdSiphon = cmd.Find("Btn_CmdSiphon")?.GetComponent<Button>();
            txtCmdSiphon = btnCmdSiphon?.GetComponentInChildren<Text>();
            btnCmdCataclysm = cmd.Find("Btn_CmdCataclysm")?.GetComponent<Button>();
            txtCmdCataclysm = btnCmdCataclysm?.GetComponentInChildren<Text>();
            btnCmdRecall = cmd.Find("Btn_CmdRecall")?.GetComponent<Button>();
            txtCmdRecall = btnCmdRecall?.GetComponentInChildren<Text>();
            btnCmdWait = cmd.Find("Btn_CmdWait")?.GetComponent<Button>();
            btnCmdCancel = cmd.Find("Btn_CmdCancel")?.GetComponent<Button>();

            if (btnCmdMove != null) { btnCmdMove.onClick.RemoveAllListeners(); btnCmdMove.onClick.AddListener(() => OnActionClicked(UnitActionMode.Move)); }
            if (btnCmdAttack != null) { btnCmdAttack.onClick.RemoveAllListeners(); btnCmdAttack.onClick.AddListener(() => OnActionClicked(UnitActionMode.TitanStrike)); }
            if (btnCmdSkill != null) { btnCmdSkill.onClick.RemoveAllListeners(); btnCmdSkill.onClick.AddListener(OnCmdSkillClicked); }
            if (btnCmdPush != null) { btnCmdPush.onClick.RemoveAllListeners(); btnCmdPush.onClick.AddListener(() => OnActionClicked(UnitActionMode.KineticPush)); }
            if (btnCmdSiphon != null) { btnCmdSiphon.onClick.RemoveAllListeners(); btnCmdSiphon.onClick.AddListener(() => OnActionClicked(UnitActionMode.ConsumeLand)); }
            if (btnCmdCataclysm != null) { btnCmdCataclysm.onClick.RemoveAllListeners(); btnCmdCataclysm.onClick.AddListener(() => OnActionClicked(UnitActionMode.MagmaCataclysm)); }
            if (btnCmdRecall != null) { btnCmdRecall.onClick.RemoveAllListeners(); btnCmdRecall.onClick.AddListener(OnRecallClicked); }
            if (btnCmdWait != null) { btnCmdWait.onClick.RemoveAllListeners(); btnCmdWait.onClick.AddListener(OnCmdWaitClicked); }
            if (btnCmdCancel != null) { btnCmdCancel.onClick.RemoveAllListeners(); btnCmdCancel.onClick.AddListener(OnCmdCancelClicked); }
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

            // ================= 1. UNIT STATUS CARD (Top-Left) ================= //
            unitCardRoot = CreateUI("Card_UnitStatus", container.transform);
            RectTransform cardRt = unitCardRoot.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0f, 1f);
            cardRt.anchorMax = new Vector2(0f, 1f);
            cardRt.pivot = new Vector2(0f, 1f);
            cardRt.sizeDelta = new Vector2(365f, 130f);
            cardRt.anchoredPosition = new Vector2(20f, -20f);

            ApplyRpgMakerWindowStyle(unitCardRoot, new Color(0.08f, 0.07f, 0.14f, 0.95f));

            // Portrait Avatar
            GameObject portObj = CreateUI("Portrait_Frame", unitCardRoot.transform);
            RectTransform portRt = portObj.GetComponent<RectTransform>();
            portRt.anchorMin = new Vector2(0f, 0.5f);
            portRt.anchorMax = new Vector2(0f, 0.5f);
            portRt.pivot = new Vector2(0f, 0.5f);
            portRt.sizeDelta = new Vector2(68f, 68f);
            portRt.anchoredPosition = new Vector2(16f, 0f);
            portObj.AddComponent<Image>().color = new Color(0.18f, 0.16f, 0.28f, 1f);

            GameObject portImgObj = CreateUI("Portrait_Image", portObj.transform);
            SetStretch(portImgObj.GetComponent<RectTransform>());
            portraitImage = portImgObj.AddComponent<Image>();
            portraitImage.preserveAspect = true;

            unitNameText = CreateTxt("Txt_UnitName", unitCardRoot.transform, "Commander", font, 16, new Color(1.0f, 0.86f, 0.35f, 1f), TextAnchor.UpperLeft);
            SetRect(unitNameText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(96f, -14f), new Vector2(175f, 22f));

            unitArchetypeText = CreateTxt("Txt_Archetype", unitCardRoot.transform, "Vanguard • Commander", font, 11, new Color(0.72f, 0.78f, 0.84f, 1f), TextAnchor.UpperLeft);
            SetRect(unitArchetypeText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(96f, -34f), new Vector2(175f, 18f));

            turnStatusText = CreateTxt("Txt_TurnStatus", unitCardRoot.transform, "✓ READY", font, 12, new Color(0.41f, 0.94f, 0.68f, 1f), TextAnchor.UpperRight);
            SetRect(turnStatusText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -14f), new Vector2(85f, 20f));

            // Slider
            GameObject slObj = CreateUI("Slider_HP", unitCardRoot.transform);
            SetRect(slObj.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(96f, -56f), new Vector2(252f, 16f));
            hpSlider = slObj.AddComponent<Slider>();
            hpSlider.interactable = false;

            GameObject slBg = CreateUI("Background", slObj.transform);
            SetStretch(slBg.GetComponent<RectTransform>());
            slBg.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f, 0.95f);

            GameObject fa = CreateUI("Fill Area", slObj.transform);
            SetStretch(fa.GetComponent<RectTransform>());

            GameObject fill = CreateUI("Fill", fa.transform);
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one; fillRt.sizeDelta = Vector2.zero;
            hpFillImage = fill.AddComponent<Image>();
            hpFillImage.color = colHpFull;
            hpSlider.fillRect = fillRt;

            hpValueText = CreateTxt("Txt_HPValue", slObj.transform, "10 / 10", font, 11, Color.white, TextAnchor.MiddleCenter);
            SetStretch(hpValueText.rectTransform);

            statsText = CreateTxt("Txt_Stats", unitCardRoot.transform, "⚔️ ATK: 3    🏃 MOV: 3", font, 12, new Color(0.92f, 0.94f, 0.96f, 1f), TextAnchor.UpperLeft);
            SetRect(statsText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(96f, -78f), new Vector2(160f, 20f));

            // Attunement Badge
            GameObject attObj = CreateUI("Badge_Attunement", unitCardRoot.transform);
            SetRect(attObj.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(96f, 10f), new Vector2(130f, 24f));
            attunementBadgeBg = attObj.AddComponent<Image>();
            attunementBadgeBg.sprite = panelFrame;
            attunementBadgeBg.type = Image.Type.Sliced;
            attunementBadgeBg.color = new Color(0.20f, 0.22f, 0.28f, 0.90f);
            attunementBadgeText = CreateTxt("Text", attObj.transform, "⚪ Neutral", font, 11, Color.white, TextAnchor.MiddleCenter);
            SetStretch(attunementBadgeText.rectTransform);

            elementalCoresText = CreateTxt("Txt_Cores", unitCardRoot.transform, "✦ 0 Cores", font, 12, new Color(1.0f, 0.84f, 0.20f, 1f), TextAnchor.MiddleRight);
            SetRect(elementalCoresText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-16f, 10f), new Vector2(110f, 24f));

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

            // ================= 3. BOTTOM ACTION BAR (Bottom-Center) ================= //
            actionBarRoot = CreateUI("Bar_CombatActions", container.transform);
            SetRect(actionBarRoot.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(1090f, 72f));
            Image actBg = actionBarRoot.AddComponent<Image>();
            actBg.sprite = panelFrame;
            actBg.type = Image.Type.Sliced;
            actBg.color = new Color(0.08f, 0.08f, 0.12f, 0.94f);

            // Section: Unanchored Rift
            unanchoredSection = CreateUI("Section_Unanchored", actionBarRoot.transform);
            SetRect(unanchoredSection.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-60f, 0f), new Vector2(-140f, 0f));
            HorizontalLayoutGroup unanchoredHlg = unanchoredSection.AddComponent<HorizontalLayoutGroup>();
            unanchoredHlg.childAlignment = TextAnchor.MiddleCenter;
            unanchoredHlg.spacing = 16f;
            unanchoredHlg.childForceExpandWidth = false;
            unanchoredHlg.childForceExpandHeight = false;

            Text txtUnanchoredHeader = CreateTxt("Txt_UnanchoredHeader", unanchoredSection.transform, "🌀 CITADEL GATEWAY\n<size=10>(Rift Unanchored)</size>", font, 12, new Color(0.85f, 0.55f, 0.95f, 1f), TextAnchor.MiddleCenter);
            txtUnanchoredHeader.gameObject.AddComponent<LayoutElement>().minWidth = 150f;

            btnTearRift = CreateBtn("Btn_TearRift", unanchoredSection.transform, "🌀 Tear Open Abyssal Rift\n<size=11>(Choose Landing Hex)</size>", font, 12, btnNormal, new Color(0.45f, 0.15f, 0.70f, 1f), 280f, 50f);
            txtTearRift = btnTearRift.GetComponentInChildren<Text>();

            btnRandomizeMap = CreateBtn("Btn_RandomizeMap", unanchoredSection.transform, "🎲 Randomize Map [R]\n<size=11>(Roll Incursion)</size>", font, 12, btnNormal, new Color(0.18f, 0.28f, 0.40f, 1f), 220f, 50f);

            // Section: Rift Panel
            riftPanelSection = CreateUI("Section_RiftPanel", actionBarRoot.transform);
            SetRect(riftPanelSection.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-60f, 0f), new Vector2(-140f, 0f));
            HorizontalLayoutGroup riftHlg = riftPanelSection.AddComponent<HorizontalLayoutGroup>();
            riftHlg.childAlignment = TextAnchor.MiddleCenter;
            riftHlg.spacing = 16f;
            riftHlg.childForceExpandWidth = false;
            riftHlg.childForceExpandHeight = false;

            Text txtRiftHeader = CreateTxt("Txt_RiftHeader", riftPanelSection.transform, "🌀 CITADEL RIFT\n<size=10>(Base Panel)</size>", font, 12, new Color(0.85f, 0.55f, 0.95f, 1f), TextAnchor.MiddleCenter);
            txtRiftHeader.gameObject.AddComponent<LayoutElement>().minWidth = 140f;

            btnDeployCommander = CreateBtn("Btn_DeployCommander", riftPanelSection.transform, "👤 Deploy Commander\n<size=11>(Free Vanguard)</size>", font, 12, btnNormal, new Color(0.40f, 0.15f, 0.65f, 1f), 220f, 50f);
            txtDeployCommander = btnDeployCommander.GetComponentInChildren<Text>();

            btnSummonTitanRift = CreateBtn("Btn_SummonTitanRift", riftPanelSection.transform, "🌋 Summon Titan\n<size=11>(Req 1 Core)</size>", font, 12, btnNormal, new Color(0.65f, 0.25f, 0.12f, 1f), 220f, 50f);
            txtSummonTitanRift = btnSummonTitanRift.GetComponentInChildren<Text>();

            // Section: Unit Abilities
            unitAbilitiesSection = CreateUI("Section_UnitAbilities", actionBarRoot.transform);
            SetRect(unitAbilitiesSection.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-60f, 0f), new Vector2(-140f, 0f));
            HorizontalLayoutGroup unitHlg = unitAbilitiesSection.AddComponent<HorizontalLayoutGroup>();
            unitHlg.childAlignment = TextAnchor.MiddleCenter;
            unitHlg.spacing = 6f;
            unitHlg.childForceExpandWidth = false;
            unitHlg.childForceExpandHeight = false;

            btnMove = CreateBtn("Btn_Move", unitAbilitiesSection.transform, "🚶 Move\n<size=10>(Tactical)</size>", font, 11, btnNormal, new Color(0.18f, 0.35f, 0.58f, 1f), 85f, 50f);
            txtMove = btnMove.GetComponentInChildren<Text>();

            btnFireball = CreateBtn("Btn_Fireball", unitAbilitiesSection.transform, "🔥 Fireball\n<size=10>(3 Dmg)</size>", font, 11, btnNormal, new Color(0.65f, 0.22f, 0.10f, 1f), 92f, 50f);
            txtFireball = btnFireball.GetComponentInChildren<Text>();

            btnWaterSurge = CreateBtn("Btn_WaterSurge", unitAbilitiesSection.transform, "💧 Water\n<size=10>(2 Dmg)</size>", font, 11, btnNormal, new Color(0.10f, 0.42f, 0.68f, 1f), 90f, 50f);
            txtWaterSurge = btnWaterSurge.GetComponentInChildren<Text>();

            btnEarthPillar = CreateBtn("Btn_EarthPillar", unitAbilitiesSection.transform, "⛰️ Earth\n<size=10>(Pillar/Mud)</size>", font, 11, btnNormal, new Color(0.48f, 0.32f, 0.18f, 1f), 94f, 50f);
            txtEarthPillar = btnEarthPillar.GetComponentInChildren<Text>();

            btnPush = CreateBtn("Btn_Push", unitAbilitiesSection.transform, "💨 Push\n<size=10>(Shove 1)</size>", font, 11, btnNormal, new Color(0.38f, 0.18f, 0.52f, 1f), 85f, 50f);
            txtPush = btnPush.GetComponentInChildren<Text>();

            btnHarvestCore = CreateBtn("Btn_HarvestCore", unitAbilitiesSection.transform, "⚡ Siphon\n<size=10>(+1 Core)</size>", font, 11, btnNormal, new Color(0.65f, 0.55f, 0.12f, 1f), 88f, 50f);
            txtHarvestCore = btnHarvestCore.GetComponentInChildren<Text>();

            btnTitanStrike = CreateBtn("Btn_TitanStrike", unitAbilitiesSection.transform, "🐾 Strike\n<size=10>(Crit)</size>", font, 11, btnNormal, new Color(0.55f, 0.20f, 0.15f, 1f), 86f, 50f);
            txtTitanStrike = btnTitanStrike.GetComponentInChildren<Text>();

            btnMagmaCataclysm = CreateBtn("Btn_Cataclysm", unitAbilitiesSection.transform, "🌋 Cataclysm\n<size=10>(1 Core Ult)</size>", font, 11, btnNormal, new Color(0.72f, 0.22f, 0.08f, 1f), 98f, 50f);
            txtMagmaCataclysm = btnMagmaCataclysm.GetComponentInChildren<Text>();

            btnSummonTitanAbility = CreateBtn("Btn_SummonTitanAbility", unitAbilitiesSection.transform, "🌋 Titan\n<size=10>(1 Core)</size>", font, 11, btnNormal, new Color(0.65f, 0.25f, 0.12f, 1f), 94f, 50f);
            txtSummonTitanAbility = btnSummonTitanAbility.GetComponentInChildren<Text>();

            btnRecall = CreateBtn("Btn_Recall", unitAbilitiesSection.transform, "🌀 Recall\n<size=10>(Citadel)</size>", font, 11, btnNormal, new Color(0.42f, 0.18f, 0.58f, 1f), 82f, 50f);
            txtRecall = btnRecall.GetComponentInChildren<Text>();

            // Universal End Turn Button
            btnEndTurn = CreateBtn("Btn_EndTurn", actionBarRoot.transform, "⏳ END TURN", font, 13, btnNormal, new Color(0.78f, 0.25f, 0.18f, 1f), 125f, 52f);
            SetRect(btnEndTurn.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(125f, 52f));

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

            // Rift / Base Panel
            if (btnDeployCommander != null) btnDeployCommander.onClick.AddListener(OnDeployCommanderClicked);
            if (btnSummonTitanRift != null) btnSummonTitanRift.onClick.AddListener(OnSummonTitanRiftClicked);

            // Universal End Turn
            if (btnEndTurn != null) btnEndTurn.onClick.AddListener(OnEndTurnClicked);

            // Disgaea Command Menu
            if (btnCmdMove != null) btnCmdMove.onClick.AddListener(() => OnActionClicked(UnitActionMode.Move));
            if (btnCmdAttack != null) btnCmdAttack.onClick.AddListener(() => OnActionClicked(UnitActionMode.TitanStrike));
            if (btnCmdSkill != null) btnCmdSkill.onClick.AddListener(OnCmdSkillClicked);
            if (btnCmdPush != null) btnCmdPush.onClick.AddListener(() => OnActionClicked(UnitActionMode.KineticPush));
            if (btnCmdSiphon != null) btnCmdSiphon.onClick.AddListener(() => OnActionClicked(UnitActionMode.ConsumeLand));
            if (btnCmdCataclysm != null) btnCmdCataclysm.onClick.AddListener(() => OnActionClicked(UnitActionMode.MagmaCataclysm));
            if (btnCmdRecall != null) btnCmdRecall.onClick.AddListener(OnRecallClicked);
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

        private void OnDeployCommanderClicked()
        {
            SoundManager3D.Instance?.PlayButtonClick();
            HexGridInteraction3D interaction = HexGridInteraction3D.Instance;
            if (interaction == null) return;

            TacticalUnit3D cmdr = HexGridInteraction3D.FindCommanderUnit();
            if (cmdr != null && !cmdr.gameObject.activeInHierarchy)
            {
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
            if (titan != null && !titan.gameObject.activeInHierarchy)
            {
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
            }
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

            // Unit Name & Archetype
            if (unitNameText != null) unitNameText.text = selected.UnitName;
            if (unitArchetypeText != null)
            {
                string factionPrefix = selected.Faction == UnitFaction.Player ? "Vanguard" : "Invader";
                unitArchetypeText.text = $"{factionPrefix} • {selected.Archetype}";
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

            // Stats Grid
            if (statsText != null)
            {
                int atk = selected.EffectiveAttackDamage;
                int mov = selected.EffectiveMoveRange;
                statsText.text = $"⚔️ ATK: {atk}    🏃 MOV: {mov}";
            }

            // Turn Readiness
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

            // Attunement Badge
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

            // Elemental Cores (Commander only)
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

            bool isTutorial1 = Tutorial.TutorialScenarioManager.Instance != null && Tutorial.TutorialScenarioManager.Instance.IsStage1Active;

            // Determine if Unanchored Rift, Base Panel (Rift), or Unit Abilities should show
            bool riftExists = (AbyssalRiftConduit3D.Instance != null && AbyssalRiftConduit3D.Instance.RiftTile != null);
            bool isRiftSelected = (interaction.CurrentSelectedTile != null && riftExists && AbyssalRiftConduit3D.Instance.RiftTile == interaction.CurrentSelectedTile);
            bool isDeployMode = (currentMode == UnitActionMode.DeployCommander || currentMode == UnitActionMode.SummonTitan);
            bool noActiveUnits = !HexGridInteraction3D.HasActivePlayerUnitsOnField();

            if (isTutorial1)
            {
                // In Tutorial 1 (Holy Crusade), Abyssal Rift is completely hidden!
                if (unanchoredSection != null) unanchoredSection.SetActive(false);
                if (riftPanelSection != null) riftPanelSection.SetActive(false);
                if (dispatchModalRoot != null) dispatchModalRoot.SetActive(false);
                if (unitAbilitiesSection != null) unitAbilitiesSection.SetActive(false);

                if (selectedUnit != null && selectedUnit.Faction == UnitFaction.Player)
                {
                    UpdateDisgaeaCommandMenu(selectedUnit, interaction, isPlayerTurn, currentMode);
                }
                else
                {
                    if (commandMenuRoot != null) commandMenuRoot.SetActive(false);
                }
            }
            else if (!riftExists && selectedUnit == null)
            {
                if (unanchoredSection != null) unanchoredSection.SetActive(true);
                if (riftPanelSection != null) riftPanelSection.SetActive(false);
                if (unitAbilitiesSection != null) unitAbilitiesSection.SetActive(false);
                if (dispatchModalRoot != null) dispatchModalRoot.SetActive(false);
                if (commandMenuRoot != null) commandMenuRoot.SetActive(false);

                if (btnTearRift != null)
                {
                    btnTearRift.interactable = isPlayerTurn;
                    SetButtonActiveHighlight(btnTearRift, currentMode == UnitActionMode.TearRift);
                    if (txtTearRift != null)
                    {
                        txtTearRift.text = (currentMode == UnitActionMode.TearRift)
                            ? "<b>[Targeting Entry...]</b>\n<size=11>(Click Open Hex)</size>"
                            : "🌀 Tear Open Abyssal Rift\n<size=11>(Choose Landing Hex)</size>";
                    }
                }
                if (btnRandomizeMap != null)
                {
                    bool isTut = Tutorial.TutorialScenarioManager.Instance != null && Tutorial.TutorialScenarioManager.Instance.IsTutorialActive;
                    btnRandomizeMap.gameObject.SetActive(!isTut);
                    btnRandomizeMap.interactable = isPlayerTurn && !isTut;
                }
            }
            else
            {
                if (unanchoredSection != null) unanchoredSection.SetActive(false);
                if (riftPanelSection != null) riftPanelSection.SetActive(false);
                if (unitAbilitiesSection != null) unitAbilitiesSection.SetActive(false);

                // Disgaea Rift Dispatch Modal (Opens when Rift is clicked!)
                bool isRiftDispatchActive = (selectedUnit == null && isRiftSelected && !isDeployMode);
                if (isRiftDispatchActive)
                {
                    UpdateDisgaeaDispatchModal(interaction);
                }
                else if (dispatchModalRoot != null && dispatchModalRoot.activeSelf)
                {
                    dispatchModalRoot.SetActive(false);
                }

                // Disgaea Unit Command Menu (Opens when Unit is clicked!)
                bool isUnitCommandActive = (selectedUnit != null && selectedUnit.Faction == UnitFaction.Player);
                if (isUnitCommandActive)
                {
                    UpdateDisgaeaCommandMenu(selectedUnit, interaction, isPlayerTurn, currentMode);
                }
                else if (commandMenuRoot != null && commandMenuRoot.activeSelf)
                {
                    commandMenuRoot.SetActive(false);
                }
            }

            // Universal End Turn Button
            if (btnEndTurn != null)
            {
                btnEndTurn.interactable = isPlayerTurn;
            }
        }

        private void UpdateRiftPanelButtons(HexGridInteraction3D interaction, bool isPlayerTurn, bool noActiveUnits, UnitActionMode currentMode)
        {
            TacticalUnit3D cmdr = HexGridInteraction3D.FindCommanderUnit();
            TacticalUnit3D titan = HexGridInteraction3D.FindTitanUnit();

            bool cmdrInReserve = (cmdr != null && !cmdr.gameObject.activeInHierarchy);
            bool titanInReserve = (titan != null && !titan.gameObject.activeInHierarchy);

            bool canDeployCmdr = isPlayerTurn && (cmdrInReserve || (cmdr != null && cmdr.gameObject.activeInHierarchy));
            bool canSummonTitan = isPlayerTurn && ((noActiveUnits && titanInReserve) || (cmdr != null && cmdr.ElementalCores >= 1) || (!titanInReserve && titan != null));

            bool isTut = Tutorial.TutorialScenarioManager.Instance != null && Tutorial.TutorialScenarioManager.Instance.IsTutorialActive;

            if (btnDeployCommander != null)
            {
                btnDeployCommander.interactable = canDeployCmdr;
                if (txtDeployCommander != null)
                {
                    if (!cmdrInReserve)
                        txtDeployCommander.text = isTut ? "👤 Demon Lord\n<size=11>(On Field)</size>" : "👤 Pick Commander\n<size=11>(On Field)</size>";
                    else if (currentMode == UnitActionMode.DeployCommander)
                        txtDeployCommander.text = "<b>[Deploying...]</b>\n<size=11>(Click Hex)</size>";
                    else
                        txtDeployCommander.text = isTut ? "👤 Deploy Demon Lord\n<size=11>(Commander / Free)</size>" : "👤 Deploy Commander\n<size=11>(Free Vanguard)</size>";
                }
            }

            if (btnSummonTitanRift != null)
            {
                bool isStage2 = isTut && Tutorial.TutorialScenarioManager.Instance != null && Tutorial.TutorialScenarioManager.Instance.IsStage2Active;
                btnSummonTitanRift.gameObject.SetActive(!isStage2);
                btnSummonTitanRift.interactable = canSummonTitan && !isStage2;
                if (txtSummonTitanRift != null)
                {
                    string titanName = isTut ? "Magma Dragon" : (titan != null ? titan.UnitName : "Titan");
                    if (!titanInReserve)
                        txtSummonTitanRift.text = $"🌋 {titanName}\n<size=11>(On Field)</size>";
                    else if (noActiveUnits)
                        txtSummonTitanRift.text = (currentMode == UnitActionMode.SummonTitan) ? "<b>[Summoning...]</b>\n<size=11>(Click Hex)</size>" : $"🌋 Summon {titanName}\n<size=11>(Free Titan)</size>";
                    else if (cmdr != null && cmdr.ElementalCores < 1)
                        txtSummonTitanRift.text = $"🌋 Summon {titanName}\n<size=11>(Req 1 Core)</size>";
                    else if (currentMode == UnitActionMode.SummonTitan)
                        txtSummonTitanRift.text = "<b>[Summoning...]</b>\n<size=11>(Click Hex)</size>";
                    else
                        txtSummonTitanRift.text = $"🌋 Summon {titanName}\n<size=11>(1 Core Titan)</size>";
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

            // Elemental Spells: Fireball for Fire Commander / Demon Lord. Water and Earth are NOT on Demon Lord!
            bool isFireUnit = isCommander || (unit.Affinity == ElementalAffinity.Fire);
            bool showFireball = isFireUnit && !isTutorial1;
            bool showWaterSurge = !isCommander && (unit.Affinity == ElementalAffinity.Water);
            bool showEarthPillar = !isCommander && (unit.Affinity == ElementalAffinity.Earth);

            if (btnFireball != null)
            {
                btnFireball.gameObject.SetActive(showFireball);
                btnFireball.interactable = canCombat;
                SetButtonActiveHighlight(btnFireball, currentMode == UnitActionMode.Fireball);
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
            Button targetBtn = null;
            if (activeTutorialButtonKey == "Move") targetBtn = btnMove;
            else if (activeTutorialButtonKey == "Strike" || activeTutorialButtonKey == "TitanStrike") targetBtn = btnTitanStrike;
            else if (activeTutorialButtonKey == "Fireball") targetBtn = btnFireball;
            else if (activeTutorialButtonKey == "Push") targetBtn = btnPush;
            else if (activeTutorialButtonKey == "HarvestCore" || activeTutorialButtonKey == "Siphon") targetBtn = btnHarvestCore;
            else if (activeTutorialButtonKey == "Cataclysm") targetBtn = btnMagmaCataclysm;
            else if (activeTutorialButtonKey == "EndTurn") targetBtn = btnEndTurn;
            else if (activeTutorialButtonKey == "TearRift") targetBtn = btnTearRift;
            else if (activeTutorialButtonKey == "DeployCommander") targetBtn = btnDeployCommander;
            else if (activeTutorialButtonKey == "DeployTitan" || activeTutorialButtonKey == "SummonTitan") targetBtn = (btnSummonTitanAbility != null && btnSummonTitanAbility.gameObject.activeInHierarchy) ? btnSummonTitanAbility : btnSummonTitanRift;

            // Strict TRPG Training Wheels:
            // Lock and dim all action buttons other than the target button!
            Button[] allActionButtons = new Button[]
            {
                btnMove, btnTitanStrike, btnFireball, btnWaterSurge, btnEarthPillar,
                btnPush, btnHarvestCore, btnMagmaCataclysm, btnSummonTitanAbility,
                btnRecall, btnEndTurn, btnTearRift, btnRandomizeMap,
                btnDeployCommander, btnSummonTitanRift
            };

            foreach (var btn in allActionButtons)
            {
                if (btn == null) continue;
                bool isAllowed = (btn == targetBtn) || 
                                 (isPushOrStrike && (btn == btnPush || btn == btnTitanStrike)) ||
                                 (isStrikeOrMove && (btn == btnTitanStrike || btn == btnMove)) ||
                                 (btn == btnMove) ||
                                 (btn == btnTitanStrike && (activeTutorialButtonKey == "Strike" || activeTutorialButtonKey == "TitanStrike" || activeTutorialButtonKey == "Move" || activeTutorialButtonKey == "Push"));
                if (isAllowed)
                {
                    btn.interactable = true;
                    if (btn == targetBtn)
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

            // Also apply training wheels & highlight to Disgaea Command Menu buttons when active!
            if (commandMenuRoot != null && commandMenuRoot.activeSelf)
            {
                Button targetCmdBtn = null;
                if (activeTutorialButtonKey == "Move") targetCmdBtn = btnCmdMove;
                else if (activeTutorialButtonKey == "Strike" || activeTutorialButtonKey == "TitanStrike") targetCmdBtn = btnCmdAttack;
                else if (activeTutorialButtonKey == "Push") targetCmdBtn = btnCmdPush;
                else if (activeTutorialButtonKey == "Fireball" || activeTutorialButtonKey == "WaterSurge" || activeTutorialButtonKey == "EarthPillar") targetCmdBtn = btnCmdSkill;
                else if (activeTutorialButtonKey == "HarvestCore" || activeTutorialButtonKey == "Siphon") targetCmdBtn = btnCmdSiphon;
                else if (activeTutorialButtonKey == "Cataclysm") targetCmdBtn = btnCmdCataclysm;

                Button[] allCmdButtons = new Button[]
                {
                    btnCmdMove, btnCmdAttack, btnCmdSkill, btnCmdPush,
                    btnCmdSiphon, btnCmdCataclysm, btnCmdRecall, btnCmdWait, btnCmdCancel
                };

                foreach (var cmdBtn in allCmdButtons)
                {
                    if (cmdBtn == null) continue;
                    bool isCmdAllowed = (targetCmdBtn == null) || (cmdBtn == targetCmdBtn) || (cmdBtn == btnCmdCancel);
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

            ApplyRpgMakerWindowStyle(dispatchModalRoot, new Color(0.08f, 0.07f, 0.14f, 0.96f));

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
            leftBg.color = new Color(0.04f, 0.04f, 0.08f, 0.85f);

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
            rightBg.color = new Color(0.04f, 0.04f, 0.08f, 0.85f);

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
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 1f),
                new Vector2(0f, 0f), new Vector2(175f, 260f));

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
            LayoutElement leH = txtCmdHeader.gameObject.AddComponent<LayoutElement>();
            leH.preferredHeight = 22f;

            // Commands
            btnCmdMove = CreateBtn("Btn_CmdMove", commandMenuRoot.transform, "👣 Move", font, 12, btnSprite, new Color(0.18f, 0.35f, 0.55f, 1f), 154f, 32f);
            txtCmdMove = btnCmdMove.GetComponentInChildren<Text>();
            btnCmdMove.onClick.RemoveAllListeners();
            btnCmdMove.onClick.AddListener(() => OnActionClicked(UnitActionMode.Move));

            btnCmdAttack = CreateBtn("Btn_CmdAttack", commandMenuRoot.transform, "⚔️ Attack", font, 12, btnSprite, new Color(0.68f, 0.22f, 0.20f, 1f), 154f, 32f);
            txtCmdAttack = btnCmdAttack.GetComponentInChildren<Text>();
            btnCmdAttack.onClick.RemoveAllListeners();
            btnCmdAttack.onClick.AddListener(() => OnActionClicked(UnitActionMode.TitanStrike));

            btnCmdSkill = CreateBtn("Btn_CmdSkill", commandMenuRoot.transform, "🔥 Skill", font, 12, btnSprite, new Color(0.72f, 0.40f, 0.15f, 1f), 154f, 32f);
            txtCmdSkill = btnCmdSkill.GetComponentInChildren<Text>();
            btnCmdSkill.onClick.RemoveAllListeners();
            btnCmdSkill.onClick.AddListener(OnCmdSkillClicked);

            btnCmdPush = CreateBtn("Btn_CmdPush", commandMenuRoot.transform, "💨 Push", font, 12, btnSprite, new Color(0.45f, 0.20f, 0.60f, 1f), 154f, 32f);
            txtCmdPush = btnCmdPush.GetComponentInChildren<Text>();
            btnCmdPush.onClick.RemoveAllListeners();
            btnCmdPush.onClick.AddListener(() => OnActionClicked(UnitActionMode.KineticPush));

            btnCmdSiphon = CreateBtn("Btn_CmdSiphon", commandMenuRoot.transform, "⚡ Siphon", font, 12, btnSprite, new Color(0.70f, 0.55f, 0.15f, 1f), 154f, 32f);
            txtCmdSiphon = btnCmdSiphon.GetComponentInChildren<Text>();
            btnCmdSiphon.onClick.RemoveAllListeners();
            btnCmdSiphon.onClick.AddListener(() => OnActionClicked(UnitActionMode.ConsumeLand));

            btnCmdCataclysm = CreateBtn("Btn_CmdCataclysm", commandMenuRoot.transform, "🌋 Cataclysm", font, 12, btnSprite, new Color(0.85f, 0.25f, 0.10f, 1f), 154f, 32f);
            txtCmdCataclysm = btnCmdCataclysm.GetComponentInChildren<Text>();
            btnCmdCataclysm.onClick.RemoveAllListeners();
            btnCmdCataclysm.onClick.AddListener(() => OnActionClicked(UnitActionMode.MagmaCataclysm));

            btnCmdRecall = CreateBtn("Btn_CmdRecall", commandMenuRoot.transform, "🌀 Recall", font, 12, btnSprite, new Color(0.38f, 0.18f, 0.52f, 1f), 154f, 32f);
            txtCmdRecall = btnCmdRecall.GetComponentInChildren<Text>();
            btnCmdRecall.onClick.RemoveAllListeners();
            btnCmdRecall.onClick.AddListener(OnRecallClicked);

            btnCmdWait = CreateBtn("Btn_CmdWait", commandMenuRoot.transform, "⏳ Wait", font, 12, btnSprite, new Color(0.35f, 0.35f, 0.40f, 1f), 154f, 32f);
            btnCmdWait.onClick.RemoveAllListeners();
            btnCmdWait.onClick.AddListener(OnCmdWaitClicked);

            btnCmdCancel = CreateBtn("Btn_CmdCancel", commandMenuRoot.transform, "✕ Cancel", font, 12, btnSprite, new Color(0.25f, 0.25f, 0.28f, 1f), 154f, 30f);
            btnCmdCancel.onClick.RemoveAllListeners();
            btnCmdCancel.onClick.AddListener(OnCmdCancelClicked);

            commandMenuRoot.transform.SetAsLastSibling();
            commandMenuRoot.SetActive(false);
        }

        private void UpdateDisgaeaDispatchModal(HexGridInteraction3D interaction)
        {
            if (dispatchModalRoot == null) return;
            dispatchModalRoot.SetActive(true);

            if (dispatchListContent == null) return;

            TacticalUnit3D[] all = UnityEngine.Object.FindObjectsByType<TacticalUnit3D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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

            foreach (var unit in roster)
            {
                TacticalUnit3D u = unit;
                bool inReserve = !u.gameObject.activeInHierarchy;
                string statusText = inReserve ? "<color=#FFD54F>[RESERVE]</color>" : "<color=#81C784>[ON FIELD]</color>";
                string costText = (u.Archetype == UnitArchetype.Titan) ? "1 Core" : "FREE";
                string icon = (u.Archetype == UnitArchetype.Commander) ? "👑" : (u.Archetype == UnitArchetype.Titan ? "🌋" : "🛡️");
                string label = $"{icon} <b>{u.UnitName}</b> {statusText}\n<size=10><color=#CFD8DC>Cost: {costText} • {u.Affinity}</color></size>";

                Color btnColor = inReserve ? new Color(0.20f, 0.22f, 0.32f, 1f) : new Color(0.15f, 0.28f, 0.20f, 1f);
                Button rowBtn = CreateBtn("Btn_UnitRow_" + u.UnitName, dispatchListContent, label, font, 11, btnNormal, btnColor, 265f, 48f);
                rowBtn.onClick.AddListener(() =>
                {
                    UpdateDispatchPreview(u);
                });
            }

            if (currentPreviewDispatchUnit == null || !roster.Contains(currentPreviewDispatchUnit))
            {
                if (roster.Count > 0) UpdateDispatchPreview(roster[0]);
            }
            else
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

            // While unit is actively traversing tiles, hide menu until it arrives
            if (unit != null && unit.IsMoving)
            {
                commandMenuRoot.SetActive(false);
                return;
            }

            commandMenuRoot.SetActive(true);
            commandMenuRoot.transform.SetAsLastSibling();

            if (txtCmdHeader != null)
            {
                txtCmdHeader.text = $"<b>{unit.UnitName.ToUpper()}</b>";
            }

            UpdateDisgaeaCommandMenuPosition(unit);

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

            // 2. Attack (Melee)
            if (btnCmdAttack != null)
            {
                btnCmdAttack.interactable = canCombat;
                SetButtonActiveHighlight(btnCmdAttack, currentMode == UnitActionMode.TitanStrike);
                if (txtCmdAttack != null)
                {
                    if (unit.UnitName.Contains("Paladin")) txtCmdAttack.text = "⚔️ Holy Strike";
                    else if (unit.UnitName.Contains("Basalt") || unit.UnitName.Contains("Golem")) txtCmdAttack.text = "👊 Boulder Smash";
                    else if (isTitan) txtCmdAttack.text = "🐾 Titan Strike";
                    else txtCmdAttack.text = "⚔️ Attack";
                }
            }

            // 3. Skill / Spell
            bool showFireball = (isCommander || unit.Affinity == ElementalAffinity.Fire) && !isTutorial1 && !isTitan;
            bool showWater = !isCommander && unit.Affinity == ElementalAffinity.Water;
            bool showEarth = !isCommander && unit.Affinity == ElementalAffinity.Earth;

            if (btnCmdSkill != null)
            {
                bool hasSkill = showFireball || showWater || showEarth;
                btnCmdSkill.gameObject.SetActive(hasSkill);
                if (hasSkill)
                {
                    btnCmdSkill.interactable = canCombat;
                    if (showFireball)
                    {
                        txtCmdSkill.text = "🔥 Fireball (Dmg 3)";
                        SetButtonActiveHighlight(btnCmdSkill, currentMode == UnitActionMode.Fireball);
                    }
                    else if (showWater)
                    {
                        txtCmdSkill.text = "💧 Water Surge";
                        SetButtonActiveHighlight(btnCmdSkill, currentMode == UnitActionMode.WaterSurge);
                    }
                    else if (showEarth)
                    {
                        txtCmdSkill.text = "⛰️ Earth Pillar";
                        SetButtonActiveHighlight(btnCmdSkill, currentMode == UnitActionMode.EarthSpire);
                    }
                }
            }

            // 4. Push
            if (btnCmdPush != null)
            {
                btnCmdPush.interactable = canCombat;
                SetButtonActiveHighlight(btnCmdPush, currentMode == UnitActionMode.KineticPush);
                if (txtCmdPush != null)
                {
                    if (isTitan) txtCmdPush.text = "🐊 Tail Shove";
                    else if (unit.UnitName.Contains("Basalt") || unit.UnitName.Contains("Golem")) txtCmdPush.text = "💥 Golem Slam";
                    else if (unit.UnitName.Contains("Paladin")) txtCmdPush.text = "⚜️ Holy Repel";
                    else txtCmdPush.text = "💨 Push";
                }
            }

            // 5. Siphon Land
            if (btnCmdSiphon != null)
            {
                bool showSiphon = !isTutorial1 && !isTitan;
                btnCmdSiphon.gameObject.SetActive(showSiphon);
                if (showSiphon)
                {
                    btnCmdSiphon.interactable = canCombat && !unit.HasSiphonedThisTurn;
                    SetButtonActiveHighlight(btnCmdSiphon, currentMode == UnitActionMode.ConsumeLand);
                }
            }

            // 6. Cataclysm (Titan only)
            if (btnCmdCataclysm != null)
            {
                btnCmdCataclysm.gameObject.SetActive(isTitan && !isTutorial1);
                if (isTitan && !isTutorial1)
                {
                    bool canCataclysm = canCombat && unit.ElementalCores >= 1;
                    btnCmdCataclysm.interactable = canCataclysm;
                    SetButtonActiveHighlight(btnCmdCataclysm, currentMode == UnitActionMode.MagmaCataclysm);
                    if (txtCmdCataclysm != null)
                    {
                        txtCmdCataclysm.text = (unit.ElementalCores >= 1) ? "🌋 Cataclysm" : "🌋 Cataclysm (Req 1)";
                    }
                }
            }

            // 7. Recall
            if (btnCmdRecall != null)
            {
                bool showRecall = !isTutorial1;
                btnCmdRecall.gameObject.SetActive(showRecall);
                btnCmdRecall.interactable = isPlayerTurn;
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

            ApplyDisgaeaTutorialPulse();
        }

        private void OnCmdSkillClicked()
        {
            SoundManager3D.Instance?.PlayButtonClick();
            HexGridInteraction3D interaction = HexGridInteraction3D.Instance;
            if (interaction == null) return;

            TacticalUnit3D sel = interaction.CurrentSelectedUnit;
            if (sel == null) return;

            if (sel.Archetype == UnitArchetype.Commander || sel.Affinity == ElementalAffinity.Fire)
            {
                OnActionClicked(UnitActionMode.Fireball);
            }
            else if (sel.Affinity == ElementalAffinity.Water)
            {
                OnActionClicked(UnitActionMode.WaterSurge);
            }
            else if (sel.Affinity == ElementalAffinity.Earth)
            {
                OnActionClicked(UnitActionMode.EarthSpire);
            }
            else if (sel.Archetype == UnitArchetype.Titan)
            {
                OnActionClicked(UnitActionMode.MagmaCataclysm);
            }
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
                interaction.ClearUnitSelection();
                interaction.SetActionMode(UnitActionMode.None);
            }
        }

        private void OnCmdCancelClicked()
        {
            SoundManager3D.Instance?.PlayButtonClick();
            HexGridInteraction3D interaction = HexGridInteraction3D.Instance;
            if (interaction == null) return;

            if (interaction.CurrentActionMode != UnitActionMode.None)
            {
                interaction.SetActionMode(UnitActionMode.None);
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
            else if (activeTutorialButtonKey == "Fireball") targetBtn = btnCmdSkill;
            else if (activeTutorialButtonKey == "Push") targetBtn = btnCmdPush;
            else if (activeTutorialButtonKey == "HarvestCore" || activeTutorialButtonKey == "Siphon") targetBtn = btnCmdSiphon;
            else if (activeTutorialButtonKey == "Cataclysm") targetBtn = btnCmdCataclysm;

            Button[] menuButtons = new Button[]
            {
                btnCmdMove, btnCmdAttack, btnCmdSkill, btnCmdPush,
                btnCmdSiphon, btnCmdCataclysm, btnCmdRecall, btnCmdWait, btnCmdCancel
            };

            bool isPushOrStrike = (activeTutorialButtonKey == "PushOrStrike");
            bool isStrikeOrMove = (activeTutorialButtonKey == "TitanStrike" || activeTutorialButtonKey == "Strike" || activeTutorialButtonKey == "StrikeOrMove");

            foreach (var btn in menuButtons)
            {
                if (btn == null) continue;
                bool isAllowed = (btn == targetBtn) ||
                                 (isPushOrStrike && (btn == btnCmdPush || btn == btnCmdAttack)) ||
                                 (isStrikeOrMove && (btn == btnCmdAttack || btn == btnCmdMove)) ||
                                 (btn == btnCmdMove) ||
                                 (btn == btnCmdCancel) ||
                                 (btn == btnCmdAttack && (activeTutorialButtonKey == "Strike" || activeTutorialButtonKey == "TitanStrike" || activeTutorialButtonKey == "Move" || activeTutorialButtonKey == "Push"));

                if (isAllowed)
                {
                    btn.interactable = true;
                    if (btn == targetBtn)
                    {
                        float pulse = (Mathf.Sin(Time.unscaledTime * 6f) + 1f) * 0.5f;
                        Color goldPulse = Color.Lerp(Color.white, new Color(1.0f, 0.85f, 0.2f, 1f), pulse);
                        var colors = btn.colors;
                        colors.normalColor = goldPulse;
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
            Camera cam = Camera.main;
            if (cam == null) return;

            // Project world point above standee chest/head level (1.5m) to screen space
            Vector3 worldPos = unit.transform.position + Vector3.up * 1.5f;
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

            // Hide if behind camera frustum
            if (screenPos.z <= 0)
            {
                commandMenuRoot.SetActive(false);
                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            Camera uiCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

            RectTransform parentRect = commandMenuRoot.transform.parent as RectTransform;
            if (parentRect == null) parentRect = GetComponent<RectTransform>();

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPos, uiCamera, out Vector2 localPoint))
            {
                RectTransform menuRect = commandMenuRoot.GetComponent<RectTransform>();
                menuRect.anchorMin = new Vector2(0.5f, 0.5f);
                menuRect.anchorMax = new Vector2(0.5f, 0.5f);
                menuRect.pivot = new Vector2(0f, 1f); // Top-left pivot

                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(menuRect);

                float menuWidth = menuRect.rect.width > 0 ? menuRect.rect.width : 175f;
                float menuHeight = menuRect.rect.height > 0 ? menuRect.rect.height : 260f;

                float edgeMargin = 20f;
                float xMin = parentRect.rect.xMin + edgeMargin;
                float xMax = parentRect.rect.xMax - edgeMargin - menuWidth;
                float yMin = parentRect.rect.yMin + edgeMargin + menuHeight;
                float yMax = parentRect.rect.yMax - edgeMargin;

                float offsetX = 55f;
                float offsetY = 20f;

                // Disgaea TRPG Smart Flip:
                // Default: display to the right of the unit
                // If overflowing the right screen edge, flip to the left of the unit
                float targetX;
                if (localPoint.x + offsetX + menuWidth <= parentRect.rect.xMax - edgeMargin)
                {
                    targetX = localPoint.x + offsetX;
                }
                else if (localPoint.x - offsetX - menuWidth >= xMin)
                {
                    targetX = localPoint.x - offsetX - menuWidth;
                }
                else
                {
                    targetX = Mathf.Clamp(localPoint.x + offsetX, xMin, xMax);
                }

                float targetY = Mathf.Clamp(localPoint.y + offsetY, yMin, yMax);
                menuRect.anchoredPosition = new Vector2(targetX, targetY);
            }
        }

        private void LateUpdate()
        {
            if (commandMenuRoot != null && commandMenuRoot.activeSelf)
            {
                HexGridInteraction3D interaction = HexGridInteraction3D.Instance;
                if (interaction != null && interaction.CurrentSelectedUnit != null)
                {
                    TacticalUnit3D sel = interaction.CurrentSelectedUnit;
                    if (sel.IsMoving)
                    {
                        commandMenuRoot.SetActive(false);
                    }
                    else
                    {
                        UpdateDisgaeaCommandMenuPosition(sel);
                    }
                }
            }
        }
    }
}
