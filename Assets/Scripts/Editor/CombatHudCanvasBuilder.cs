#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ElementalHexTactics3D.UI;

namespace ElementalHexTactics3D.Editor
{
    /// <summary>
    /// Automation builder that constructs a polished, high-contrast uGUI Combat HUD
    /// inside Panel_InGameHUD: Unit Status Card, Round & Turn Phase Ribbon, and Bottom Action Bar.
    /// Accessible via menu: "Elemental Hex 3D" -> "Regenerate In-Game Combat HUD".
    /// </summary>
    public static class CombatHudCanvasBuilder
    {
        private const string BtnNormalPath = "Assets/Sprites/UI/UI_Button_Normal.png";
        private const string BtnHighlightPath = "Assets/Sprites/UI/UI_Button_Highlight.png";
        private const string PanelFramePath = "Assets/Sprites/UI/UI_Panel_Frame.png";
        private const string FontBoldPath = "Assets/Fonts/Font_Bold.ttf";
        private const string FontRegularPath = "Assets/Fonts/Font_Regular.ttf";

        [MenuItem("Elemental Hex 3D/Advanced/Regenerate In-Game Combat HUD", false, 53)]
        public static void GenerateCombatHud()
        {
            Debug.Log("<color=#80DEEA><b>[Combat HUD Builder]</b></color> Building modern uGUI Combat HUD...");

            AssetDatabase.Refresh();

            // 1. Locate Panel_InGameHUD in scene (works even when inactive)
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[Combat HUD Builder] No Canvas found in scene! Run 'Regenerate Base Title Canvas' first.");
                return;
            }

            GameObject hudPanel = null;
            Transform foundHud = canvas.transform.Find("Panel_InGameHUD");
            if (foundHud != null)
            {
                hudPanel = foundHud.gameObject;
            }
            else
            {
                hudPanel = CreateUIObject("Panel_InGameHUD", canvas.transform);
                SetStretchAll(hudPanel.GetComponent<RectTransform>());
            }

            // 2. Load UI Sprites & Fonts
            Sprite btnNormal = AssetDatabase.LoadAssetAtPath<Sprite>(BtnNormalPath);
            Sprite btnHighlight = AssetDatabase.LoadAssetAtPath<Sprite>(BtnHighlightPath);
            Sprite panelFrame = AssetDatabase.LoadAssetAtPath<Sprite>(PanelFramePath);

            Font fontBold = AssetDatabase.LoadAssetAtPath<Font>(FontBoldPath);
            Font fontRegular = AssetDatabase.LoadAssetAtPath<Font>(FontRegularPath);
            if (fontBold == null) fontBold = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (fontRegular == null) fontRegular = fontBold;

            // 3. Purge legacy container if existing
            Transform oldContainer = hudPanel.transform.Find("CombatHUD_Container");
            if (oldContainer != null)
            {
                Object.DestroyImmediate(oldContainer.gameObject);
            }

            // 4. Create root container
            GameObject container = CreateUIObject("CombatHUD_Container", hudPanel.transform);
            SetStretchAll(container.GetComponent<RectTransform>());

            // ================= 1. UNIT STATUS CARD (Top-Left) ================= //
            GameObject unitCardObj = CreateUIObject("Card_UnitStatus", container.transform);
            RectTransform unitCardRect = unitCardObj.GetComponent<RectTransform>();
            unitCardRect.anchorMin = new Vector2(0f, 1f);
            unitCardRect.anchorMax = new Vector2(0f, 1f);
            unitCardRect.pivot = new Vector2(0f, 1f);
            unitCardRect.sizeDelta = new Vector2(365f, 130f);
            unitCardRect.anchoredPosition = new Vector2(20f, -20f);

            Image unitCardBg = unitCardObj.AddComponent<Image>();
            unitCardBg.sprite = panelFrame;
            unitCardBg.type = Image.Type.Sliced;
            unitCardBg.color = new Color(0.08f, 0.07f, 0.14f, 0.92f);

            // Portrait Avatar
            GameObject portraitObj = CreateUIObject("Portrait_Frame", unitCardObj.transform);
            RectTransform portraitRect = portraitObj.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0f, 0.5f);
            portraitRect.anchorMax = new Vector2(0f, 0.5f);
            portraitRect.pivot = new Vector2(0f, 0.5f);
            portraitRect.sizeDelta = new Vector2(68f, 68f);
            portraitRect.anchoredPosition = new Vector2(16f, 0f);

            Image portraitBg = portraitObj.AddComponent<Image>();
            portraitBg.color = new Color(0.18f, 0.16f, 0.28f, 1f);

            GameObject portraitImgObj = CreateUIObject("Portrait_Image", portraitObj.transform);
            SetStretchAll(portraitImgObj.GetComponent<RectTransform>());
            Image portraitImg = portraitImgObj.AddComponent<Image>();
            portraitImg.preserveAspect = true;
            Sprite cmdrSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Portraits/demonlordportrait.png");
            if (cmdrSprite == null) cmdrSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Battlers/DemonLord.png");
            if (cmdrSprite == null) cmdrSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Battlers/Actor3_3.png");
            if (cmdrSprite != null) portraitImg.sprite = cmdrSprite;

            // Unit Name
            Text txtName = CreateLabel("Txt_UnitName", unitCardObj.transform, "Commander", fontBold, 16, new Color(1.0f, 0.86f, 0.35f, 1f), TextAnchor.UpperLeft);
            RectTransform nameRect = txtName.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(0f, 1f);
            nameRect.pivot = new Vector2(0f, 1f);
            nameRect.anchoredPosition = new Vector2(96f, -14f);
            nameRect.sizeDelta = new Vector2(175f, 22f);

            // Archetype
            Text txtArchetype = CreateLabel("Txt_Archetype", unitCardObj.transform, "Vanguard • Commander", fontRegular, 11, new Color(0.72f, 0.78f, 0.84f, 1f), TextAnchor.UpperLeft);
            RectTransform archRect = txtArchetype.GetComponent<RectTransform>();
            archRect.anchorMin = new Vector2(0f, 1f);
            archRect.anchorMax = new Vector2(0f, 1f);
            archRect.pivot = new Vector2(0f, 1f);
            archRect.anchoredPosition = new Vector2(96f, -34f);
            archRect.sizeDelta = new Vector2(175f, 18f);

            // Turn Readiness Text
            Text txtTurnStatus = CreateLabel("Txt_TurnStatus", unitCardObj.transform, "✓ READY", fontBold, 12, new Color(0.41f, 0.94f, 0.68f, 1f), TextAnchor.UpperRight);
            RectTransform statusRect = txtTurnStatus.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(1f, 1f);
            statusRect.anchorMax = new Vector2(1f, 1f);
            statusRect.pivot = new Vector2(1f, 1f);
            statusRect.anchoredPosition = new Vector2(-16f, -14f);
            statusRect.sizeDelta = new Vector2(85f, 20f);

            // Health Bar Slider
            GameObject hpSliderObj = CreateUIObject("Slider_HP", unitCardObj.transform);
            RectTransform hpSliderRect = hpSliderObj.GetComponent<RectTransform>();
            hpSliderRect.anchorMin = new Vector2(0f, 1f);
            hpSliderRect.anchorMax = new Vector2(0f, 1f);
            hpSliderRect.pivot = new Vector2(0f, 1f);
            hpSliderRect.sizeDelta = new Vector2(252f, 16f);
            hpSliderRect.anchoredPosition = new Vector2(96f, -56f);

            Slider hpSlider = hpSliderObj.AddComponent<Slider>();
            hpSlider.minValue = 0f;
            hpSlider.maxValue = 1f;
            hpSlider.value = 1f;
            hpSlider.interactable = false;

            // Slider Background
            GameObject hpBgObj = CreateUIObject("Background", hpSliderObj.transform);
            SetStretchAll(hpBgObj.GetComponent<RectTransform>());
            Image hpBg = hpBgObj.AddComponent<Image>();
            hpBg.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);

            // Slider Fill Area & Fill
            GameObject fillArea = CreateUIObject("Fill Area", hpSliderObj.transform);
            SetStretchAll(fillArea.GetComponent<RectTransform>());

            GameObject hpFillObj = CreateUIObject("Fill", fillArea.transform);
            RectTransform hpFillRect = hpFillObj.GetComponent<RectTransform>();
            hpFillRect.anchorMin = Vector2.zero;
            hpFillRect.anchorMax = Vector2.one;
            hpFillRect.sizeDelta = Vector2.zero;

            Image hpFill = hpFillObj.AddComponent<Image>();
            hpFill.color = new Color(0.30f, 0.78f, 0.35f, 1f);
            hpSlider.fillRect = hpFillRect;

            // Slider HP Text Overlay
            Text txtHpVal = CreateLabel("Txt_HPValue", hpSliderObj.transform, "10 / 10", fontBold, 11, Color.white, TextAnchor.MiddleCenter);
            SetStretchAll(txtHpVal.GetComponent<RectTransform>());

            // Stats Text
            Text txtStats = CreateLabel("Txt_Stats", unitCardObj.transform, "⚔️ ATK: 3    🏃 MOV: 3", fontBold, 12, new Color(0.92f, 0.94f, 0.96f, 1f), TextAnchor.UpperLeft);
            RectTransform statsRect = txtStats.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0f, 1f);
            statsRect.anchorMax = new Vector2(0f, 1f);
            statsRect.pivot = new Vector2(0f, 1f);
            statsRect.anchoredPosition = new Vector2(96f, -78f);
            statsRect.sizeDelta = new Vector2(160f, 20f);

            // Attunement Badge
            GameObject attunementBadgeObj = CreateUIObject("Badge_Attunement", unitCardObj.transform);
            RectTransform attBadgeRect = attunementBadgeObj.GetComponent<RectTransform>();
            attBadgeRect.anchorMin = new Vector2(0f, 0f);
            attBadgeRect.anchorMax = new Vector2(0f, 0f);
            attBadgeRect.pivot = new Vector2(0f, 0f);
            attBadgeRect.sizeDelta = new Vector2(130f, 24f);
            attBadgeRect.anchoredPosition = new Vector2(96f, 10f);

            Image attBg = attunementBadgeObj.AddComponent<Image>();
            attBg.sprite = panelFrame;
            attBg.type = Image.Type.Sliced;
            attBg.color = new Color(0.20f, 0.22f, 0.28f, 0.90f);

            Text txtAtt = CreateLabel("Text", attunementBadgeObj.transform, "⚪ Neutral", fontBold, 11, Color.white, TextAnchor.MiddleCenter);
            SetStretchAll(txtAtt.GetComponent<RectTransform>());

            // Elemental Cores Text
            Text txtCores = CreateLabel("Txt_Cores", unitCardObj.transform, "✦ 0 Cores", fontBold, 12, new Color(1.0f, 0.84f, 0.20f, 1f), TextAnchor.MiddleRight);
            RectTransform coresRect = txtCores.GetComponent<RectTransform>();
            coresRect.anchorMin = new Vector2(1f, 0f);
            coresRect.anchorMax = new Vector2(1f, 0f);
            coresRect.pivot = new Vector2(1f, 0f);
            coresRect.sizeDelta = new Vector2(110f, 24f);
            coresRect.anchoredPosition = new Vector2(-16f, 10f);

            // ================= 2. TURN PHASE RIBBON (Top-Center) ================= //
            GameObject ribbonObj = CreateUIObject("Ribbon_TurnPhase", container.transform);
            RectTransform ribbonRect = ribbonObj.GetComponent<RectTransform>();
            ribbonRect.anchorMin = new Vector2(0.5f, 1f);
            ribbonRect.anchorMax = new Vector2(0.5f, 1f);
            ribbonRect.pivot = new Vector2(0.5f, 1f);
            ribbonRect.sizeDelta = new Vector2(330f, 54f);
            ribbonRect.anchoredPosition = new Vector2(0f, -16f);

            Image ribbonBg = ribbonObj.AddComponent<Image>();
            ribbonBg.sprite = panelFrame;
            ribbonBg.type = Image.Type.Sliced;
            ribbonBg.color = new Color(0.08f, 0.25f, 0.38f, 0.95f);

            Text txtRound = CreateLabel("Txt_Round", ribbonObj.transform, "ROUND 1", fontBold, 11, new Color(0.50f, 0.87f, 0.92f, 1f), TextAnchor.UpperCenter);
            RectTransform roundRect = txtRound.GetComponent<RectTransform>();
            roundRect.anchorMin = new Vector2(0f, 1f);
            roundRect.anchorMax = new Vector2(1f, 1f);
            roundRect.pivot = new Vector2(0.5f, 1f);
            roundRect.anchoredPosition = new Vector2(0f, -6f);
            roundRect.sizeDelta = new Vector2(0f, 16f);

            Text txtPhase = CreateLabel("Txt_Phase", ribbonObj.transform, "⚔️ PLAYER PHASE", fontBold, 16, Color.white, TextAnchor.LowerCenter);
            RectTransform phaseRect = txtPhase.GetComponent<RectTransform>();
            phaseRect.anchorMin = new Vector2(0f, 0f);
            phaseRect.anchorMax = new Vector2(1f, 0f);
            phaseRect.pivot = new Vector2(0.5f, 0f);
            phaseRect.anchoredPosition = new Vector2(0f, 6f);
            phaseRect.sizeDelta = new Vector2(0f, 24f);

            // ================= 3. BOTTOM ACTION BAR (Bottom-Center) ================= //
            GameObject actionBarObj = CreateUIObject("Bar_CombatActions", container.transform);
            RectTransform actionRect = actionBarObj.GetComponent<RectTransform>();
            actionRect.anchorMin = new Vector2(0.5f, 0f);
            actionRect.anchorMax = new Vector2(0.5f, 0f);
            actionRect.pivot = new Vector2(0.5f, 0f);
            actionRect.sizeDelta = new Vector2(1090f, 72f);
            actionRect.anchoredPosition = new Vector2(0f, 20f);

            Image actionBg = actionObj(actionBarObj, panelFrame);

            // Section: Unanchored Rift (Before Rift is opened)
            GameObject unanchoredSecObj = CreateUIObject("Section_Unanchored", actionBarObj.transform);
            RectTransform unanchoredSecRect = unanchoredSecObj.GetComponent<RectTransform>();
            unanchoredSecRect.anchorMin = new Vector2(0f, 0f);
            unanchoredSecRect.anchorMax = new Vector2(1f, 1f);
            unanchoredSecRect.pivot = new Vector2(0.5f, 0.5f);
            unanchoredSecRect.sizeDelta = new Vector2(-140f, 0f);
            unanchoredSecRect.anchoredPosition = new Vector2(-60f, 0f);

            HorizontalLayoutGroup unanchoredHlg = unanchoredSecObj.AddComponent<HorizontalLayoutGroup>();
            unanchoredHlg.childAlignment = TextAnchor.MiddleCenter;
            unanchoredHlg.spacing = 16f;
            unanchoredHlg.childForceExpandWidth = false;
            unanchoredHlg.childForceExpandHeight = false;

            Text txtUnanchoredHeader = CreateLabel("Txt_UnanchoredHeader", unanchoredSecObj.transform, "🌀 CITADEL GATEWAY\n<size=10>(Rift Unanchored)</size>", fontBold, 12, new Color(0.85f, 0.55f, 0.95f, 1f), TextAnchor.MiddleCenter);
            txtUnanchoredHeader.GetComponent<LayoutElement>().minWidth = 150f;

            Button btnTearRift = CreateActionButton("Btn_TearRift", unanchoredSecObj.transform, "🌀 Tear Open Abyssal Rift\n<size=11>(Choose Landing Hex)</size>", fontBold, 12, btnNormal, new Color(0.45f, 0.15f, 0.70f, 1f), width: 280f, height: 50f);
            Button btnRandomizeMap = CreateActionButton("Btn_RandomizeMap", unanchoredSecObj.transform, "🎲 Randomize Map [R]\n<size=11>(Roll Incursion)</size>", fontBold, 12, btnNormal, new Color(0.18f, 0.28f, 0.40f, 1f), width: 220f, height: 50f);

            // Section: Rift / Base Panel
            GameObject riftSecObj = CreateUIObject("Section_RiftPanel", actionBarObj.transform);
            RectTransform riftSecRect = riftSecObj.GetComponent<RectTransform>();
            riftSecRect.anchorMin = new Vector2(0f, 0f);
            riftSecRect.anchorMax = new Vector2(1f, 1f);
            riftSecRect.pivot = new Vector2(0.5f, 0.5f);
            riftSecRect.sizeDelta = new Vector2(-140f, 0f);
            riftSecRect.anchoredPosition = new Vector2(-60f, 0f);

            HorizontalLayoutGroup riftHlg = riftSecObj.AddComponent<HorizontalLayoutGroup>();
            riftHlg.childAlignment = TextAnchor.MiddleCenter;
            riftHlg.spacing = 16f;
            riftHlg.childForceExpandWidth = false;
            riftHlg.childForceExpandHeight = false;

            Text txtRiftHeader = CreateLabel("Txt_RiftHeader", riftSecObj.transform, "🌀 CITADEL RIFT\n<size=10>(Base Panel)</size>", fontBold, 12, new Color(0.85f, 0.55f, 0.95f, 1f), TextAnchor.MiddleCenter);
            txtRiftHeader.GetComponent<LayoutElement>().minWidth = 140f;

            Button btnDeployCmdr = CreateActionButton("Btn_DeployCommander", riftSecObj.transform, "👤 Deploy Commander\n<size=11>(Free Vanguard)</size>", fontBold, 12, btnNormal, new Color(0.40f, 0.15f, 0.65f, 1f), width: 220f, height: 50f);
            Button btnSummonTitanRift = CreateActionButton("Btn_SummonTitanRift", riftSecObj.transform, "🌋 Summon Titan\n<size=11>(Req 1 Core)</size>", fontBold, 12, btnNormal, new Color(0.65f, 0.25f, 0.12f, 1f), width: 220f, height: 50f);

            // Section: Unit Abilities
            GameObject unitSecObj = CreateUIObject("Section_UnitAbilities", actionBarObj.transform);
            RectTransform unitSecRect = unitSecObj.GetComponent<RectTransform>();
            unitSecRect.anchorMin = new Vector2(0f, 0f);
            unitSecRect.anchorMax = new Vector2(1f, 1f);
            unitSecRect.pivot = new Vector2(0.5f, 0.5f);
            unitSecRect.sizeDelta = new Vector2(-140f, 0f);
            unitSecRect.anchoredPosition = new Vector2(-60f, 0f);

            HorizontalLayoutGroup unitHlg = unitSecObj.AddComponent<HorizontalLayoutGroup>();
            unitHlg.childAlignment = TextAnchor.MiddleCenter;
            unitHlg.spacing = 6f;
            unitHlg.childForceExpandWidth = false;
            unitHlg.childForceExpandHeight = false;

            Button btnMove = CreateActionButton("Btn_Move", unitSecObj.transform, "🚶 Move\n<size=10>(Tactical)</size>", fontBold, 11, btnNormal, new Color(0.18f, 0.35f, 0.58f, 1f), width: 85f, height: 50f);
            Button btnFire = CreateActionButton("Btn_Fireball", unitSecObj.transform, "🔥 Fireball\n<size=10>(3 Dmg)</size>", fontBold, 11, btnNormal, new Color(0.65f, 0.22f, 0.10f, 1f), width: 92f, height: 50f);
            Button btnWater = CreateActionButton("Btn_WaterSurge", unitSecObj.transform, "💧 Water\n<size=10>(2 Dmg)</size>", fontBold, 11, btnNormal, new Color(0.10f, 0.42f, 0.68f, 1f), width: 90f, height: 50f);
            Button btnEarth = CreateActionButton("Btn_EarthPillar", unitSecObj.transform, "⛰️ Earth\n<size=10>(Pillar/Mud)</size>", fontBold, 11, btnNormal, new Color(0.48f, 0.32f, 0.18f, 1f), width: 94f, height: 50f);
            Button btnPush = CreateActionButton("Btn_Push", unitSecObj.transform, "💨 Push\n<size=10>(Shove 1)</size>", fontBold, 11, btnNormal, new Color(0.38f, 0.18f, 0.52f, 1f), width: 85f, height: 50f);
            Button btnHarvest = CreateActionButton("Btn_HarvestCore", unitSecObj.transform, "⚡ Siphon\n<size=10>(+1 Core)</size>", fontBold, 11, btnNormal, new Color(0.65f, 0.55f, 0.12f, 1f), width: 88f, height: 50f);
            Button btnStrike = CreateActionButton("Btn_TitanStrike", unitSecObj.transform, "🐾 Strike\n<size=10>(Crit)</size>", fontBold, 11, btnNormal, new Color(0.55f, 0.20f, 0.15f, 1f), width: 86f, height: 50f);
            Button btnCata = CreateActionButton("Btn_Cataclysm", unitSecObj.transform, "🌋 Cataclysm\n<size=10>(1 Core Ult)</size>", fontBold, 11, btnNormal, new Color(0.72f, 0.22f, 0.08f, 1f), width: 98f, height: 50f);
            Button btnSummonAbility = CreateActionButton("Btn_SummonTitanAbility", unitSecObj.transform, "🌋 Titan\n<size=10>(1 Core)</size>", fontBold, 11, btnNormal, new Color(0.65f, 0.25f, 0.12f, 1f), width: 94f, height: 50f);
            Button btnRecall = CreateActionButton("Btn_Recall", unitSecObj.transform, "🌀 Recall\n<size=10>(Citadel)</size>", fontBold, 11, btnNormal, new Color(0.42f, 0.18f, 0.58f, 1f), width: 82f, height: 50f);

            // Universal End Turn Button
            Button btnEndTurn = CreateActionButton("Btn_EndTurn", actionBarObj.transform, "⏳ END TURN", fontBold, 13, btnNormal, new Color(0.78f, 0.25f, 0.18f, 1f), width: 125f, height: 52f);
            RectTransform endTurnRect = btnEndTurn.GetComponent<RectTransform>();
            endTurnRect.anchorMin = new Vector2(1f, 0.5f);
            endTurnRect.anchorMax = new Vector2(1f, 0.5f);
            endTurnRect.pivot = new Vector2(1f, 0.5f);
            endTurnRect.anchoredPosition = new Vector2(-12f, 0f);

            // 5. Attach & wire CombatHudCanvasUI component
            CombatHudCanvasUI hudUI = hudPanel.GetComponent<CombatHudCanvasUI>();
            if (hudUI == null) hudUI = hudPanel.AddComponent<CombatHudCanvasUI>();

            SerializedObject so = new SerializedObject(hudUI);
            so.FindProperty("unitCardRoot").objectReferenceValue = unitCardObj;
            so.FindProperty("portraitImage").objectReferenceValue = portraitImg;
            so.FindProperty("unitNameText").objectReferenceValue = txtName;
            so.FindProperty("unitArchetypeText").objectReferenceValue = txtArchetype;
            so.FindProperty("hpSlider").objectReferenceValue = hpSlider;
            so.FindProperty("hpFillImage").objectReferenceValue = hpFill;
            so.FindProperty("hpValueText").objectReferenceValue = txtHpVal;
            so.FindProperty("statsText").objectReferenceValue = txtStats;
            so.FindProperty("turnStatusText").objectReferenceValue = txtTurnStatus;
            so.FindProperty("attunementBadgeText").objectReferenceValue = txtAtt;
            so.FindProperty("attunementBadgeBg").objectReferenceValue = attBg;
            so.FindProperty("elementalCoresText").objectReferenceValue = txtCores;

            so.FindProperty("turnRibbonRoot").objectReferenceValue = ribbonObj;
            so.FindProperty("turnRibbonBg").objectReferenceValue = ribbonBg;
            so.FindProperty("roundLabelText").objectReferenceValue = txtRound;
            so.FindProperty("phaseStatusText").objectReferenceValue = txtPhase;

            so.FindProperty("actionBarRoot").objectReferenceValue = actionBarObj;
            so.FindProperty("unanchoredSection").objectReferenceValue = unanchoredSecObj;
            so.FindProperty("btnTearRift").objectReferenceValue = btnTearRift;
            so.FindProperty("txtTearRift").objectReferenceValue = btnTearRift.GetComponentInChildren<Text>();
            so.FindProperty("btnRandomizeMap").objectReferenceValue = btnRandomizeMap;

            so.FindProperty("riftPanelSection").objectReferenceValue = riftSecObj;
            so.FindProperty("unitAbilitiesSection").objectReferenceValue = unitSecObj;

            so.FindProperty("btnDeployCommander").objectReferenceValue = btnDeployCmdr;
            so.FindProperty("txtDeployCommander").objectReferenceValue = btnDeployCmdr.GetComponentInChildren<Text>();
            so.FindProperty("btnSummonTitanRift").objectReferenceValue = btnSummonTitanRift;
            so.FindProperty("txtSummonTitanRift").objectReferenceValue = btnSummonTitanRift.GetComponentInChildren<Text>();

            so.FindProperty("btnMove").objectReferenceValue = btnMove;
            so.FindProperty("txtMove").objectReferenceValue = btnMove.GetComponentInChildren<Text>();
            so.FindProperty("btnFireball").objectReferenceValue = btnFire;
            so.FindProperty("txtFireball").objectReferenceValue = btnFire.GetComponentInChildren<Text>();
            so.FindProperty("btnWaterSurge").objectReferenceValue = btnWater;
            so.FindProperty("txtWaterSurge").objectReferenceValue = btnWater.GetComponentInChildren<Text>();
            so.FindProperty("btnEarthPillar").objectReferenceValue = btnEarth;
            so.FindProperty("txtEarthPillar").objectReferenceValue = btnEarth.GetComponentInChildren<Text>();
            so.FindProperty("btnPush").objectReferenceValue = btnPush;
            so.FindProperty("txtPush").objectReferenceValue = btnPush.GetComponentInChildren<Text>();
            so.FindProperty("btnHarvestCore").objectReferenceValue = btnHarvest;
            so.FindProperty("txtHarvestCore").objectReferenceValue = btnHarvest.GetComponentInChildren<Text>();
            so.FindProperty("btnTitanStrike").objectReferenceValue = btnStrike;
            so.FindProperty("txtTitanStrike").objectReferenceValue = btnStrike.GetComponentInChildren<Text>();
            so.FindProperty("btnMagmaCataclysm").objectReferenceValue = btnCata;
            so.FindProperty("txtMagmaCataclysm").objectReferenceValue = btnCata.GetComponentInChildren<Text>();
            so.FindProperty("btnSummonTitanAbility").objectReferenceValue = btnSummonAbility;
            so.FindProperty("txtSummonTitanAbility").objectReferenceValue = btnSummonAbility.GetComponentInChildren<Text>();
            so.FindProperty("btnRecall").objectReferenceValue = btnRecall;
            so.FindProperty("txtRecall").objectReferenceValue = btnRecall.GetComponentInChildren<Text>();

            so.FindProperty("btnEndTurn").objectReferenceValue = btnEndTurn;

            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=#80DEEA><b>[Combat HUD Builder]</b></color> In-Game Combat HUD successfully generated and wired!");
        }

        // ================= HELPER METHODS ================= //

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static void SetStretchAll(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        private static Text CreateLabel(string name, Transform parent, string content, Font font, int fontSize, Color color, TextAnchor alignment)
        {
            GameObject labelObj = CreateUIObject(name, parent);
            Text txt = labelObj.AddComponent<Text>();
            txt.text = content;
            txt.font = font;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = alignment;
            txt.raycastTarget = false;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;

            Shadow shadow = labelObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            shadow.effectDistance = new Vector2(1f, -1f);

            labelObj.AddComponent<LayoutElement>();
            return txt;
        }

        private static Image actionObj(GameObject parent, Sprite panelFrame)
        {
            Image img = parent.AddComponent<Image>();
            img.sprite = panelFrame;
            img.type = Image.Type.Sliced;
            img.color = new Color(0.08f, 0.08f, 0.12f, 0.94f);
            return img;
        }

        private static Button CreateActionButton(string name, Transform parent, string label, Font font, int fontSize, Sprite bgSprite, Color tint, float width, float height)
        {
            GameObject btnObj = CreateUIObject(name, parent);
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);

            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;
            le.minWidth = width;
            le.minHeight = height;

            Image img = btnObj.AddComponent<Image>();
            img.sprite = bgSprite;
            img.type = Image.Type.Simple;
            img.color = tint;

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.disabledColor = new Color(0.35f, 0.35f, 0.38f, 0.65f);
            btn.colors = colors;

            GameObject textObj = CreateUIObject("Text", btnObj.transform);
            SetStretchAll(textObj.GetComponent<RectTransform>());

            Text text = textObj.AddComponent<Text>();
            text.text = label;
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            Shadow shadow = textObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.95f);
            shadow.effectDistance = new Vector2(1f, -1f);

            return btn;
        }
    }
}
#endif
