with open('Assets/Scripts/Editor/CombatHudCanvasBuilder.cs', 'r', encoding='utf-8') as f:
    text = f.read()

text = text.replace(
    'private const string BtnNormalPath = "Assets/Sprites/UI/UI_Button_Holy.png";',
    'private const string BtnNormalPath = "Assets/Sprites/UI/UI_Button_Normal.png";'
)
text = text.replace(
    'private const string BtnHighlightPath = "Assets/Sprites/UI/UI_Button_Holy.png";',
    'private const string BtnHighlightPath = "Assets/Sprites/UI/UI_Button_Normal.png";'
)
text = text.replace(
    'private const string PanelFramePath = "Assets/Sprites/UI/UI_Box_Holy_9Slice.png";',
    'private const string PanelFramePath = "Assets/Sprites/UI/UI_WindowFrame_RPG.png";'
)

# Update actionBarObj padding and spacing, remove unanchoredSecObj/riftSecObj from inside actionBarObj
old_block = '''            // Direct Children in Right-Side Command Dock
            Text txtUnanchoredHeader = CreateLabel("Txt_UnanchoredHeader", actionBarObj.transform, "🌀 <b>CITADEL GATEWAY</b>", fontBold, 11, new Color(0.85f, 0.65f, 1.0f, 1f), TextAnchor.MiddleCenter);
            txtUnanchoredHeader.GetComponent<LayoutElement>().preferredHeight = 22f;

            Button btnTearRift = CreateActionButton("Btn_TearRift", actionBarObj.transform, "🌀 <b>Tear Open Rift</b>", fontBold, 11, btnNormal, new Color(0.45f, 0.15f, 0.70f, 1f), width: 155f, height: 38f);
            Button btnRandomizeMap = CreateActionButton("Btn_RandomizeMap", actionBarObj.transform, "🎲 <b>Roll Map [R]</b>", fontBold, 11, btnNormal, new Color(0.18f, 0.28f, 0.40f, 1f), width: 155f, height: 32f);

            Text txtRiftHeader = CreateLabel("Txt_RiftHeader", actionBarObj.transform, "🌀 <b>CITADEL RIFT</b>", fontBold, 11, new Color(0.85f, 0.65f, 1.0f, 1f), TextAnchor.MiddleCenter);
            txtRiftHeader.GetComponent<LayoutElement>().preferredHeight = 22f;

            Button btnOpenRiftDispatch = CreateActionButton("Btn_OpenRiftDispatch", actionBarObj.transform, "📦 <b>Squad Dispatch</b>", fontBold, 11, btnNormal, new Color(0.40f, 0.15f, 0.65f, 1f), width: 155f, height: 38f);

            GameObject unanchoredSecObj = CreateUIObject("Section_Unanchored", actionBarObj.transform);
            unanchoredSecObj.SetActive(false);
            GameObject riftSecObj = CreateUIObject("Section_RiftPanel", actionBarObj.transform);
            riftSecObj.SetActive(false);

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

            Button btnMove = CreateActionButton("Btn_Move", unitSecObj.transform, "🚶 Move\\n<size=10>(Tactical)</size>", fontBold, 11, btnNormal, new Color(0.18f, 0.35f, 0.58f, 1f), width: 85f, height: 50f);
            Button btnFire = CreateActionButton("Btn_Fireball", unitSecObj.transform, "🔥 Fireball\\n<size=10>(3 Dmg)</size>", fontBold, 11, btnNormal, new Color(0.65f, 0.22f, 0.10f, 1f), width: 92f, height: 50f);
            Button btnWater = CreateActionButton("Btn_WaterSurge", unitSecObj.transform, "💧 Water\\n<size=10>(2 Dmg)</size>", fontBold, 11, btnNormal, new Color(0.10f, 0.42f, 0.68f, 1f), width: 90f, height: 50f);
            Button btnEarth = CreateActionButton("Btn_EarthPillar", unitSecObj.transform, "⛰️ Earth\\n<size=10>(Pillar/Mud)</size>", fontBold, 11, btnNormal, new Color(0.48f, 0.32f, 0.18f, 1f), width: 94f, height: 50f);
            Button btnPush = CreateActionButton("Btn_Push", unitSecObj.transform, "💨 Push\\n<size=10>(Shove 1)</size>", fontBold, 11, btnNormal, new Color(0.38f, 0.18f, 0.52f, 1f), width: 85f, height: 50f);
            Button btnHarvest = CreateActionButton("Btn_HarvestCore", unitSecObj.transform, "⚡ Siphon\\n<size=10>(+1 Core)</size>", fontBold, 11, btnNormal, new Color(0.65f, 0.55f, 0.12f, 1f), width: 88f, height: 50f);
            Button btnStrike = CreateActionButton("Btn_TitanStrike", unitSecObj.transform, "🐾 Strike\\n<size=10>(Crit)</size>", fontBold, 11, btnNormal, new Color(0.55f, 0.20f, 0.15f, 1f), width: 86f, height: 50f);
            Button btnCata = CreateActionButton("Btn_Cataclysm", unitSecObj.transform, "🌋 Cataclysm\\n<size=10>(1 Core Ult)</size>", fontBold, 11, btnNormal, new Color(0.72f, 0.22f, 0.08f, 1f), width: 98f, height: 50f);
            Button btnSummonAbility = CreateActionButton("Btn_SummonTitanAbility", unitSecObj.transform, "🌋 Titan\\n<size=10>(1 Core)</size>", fontBold, 11, btnNormal, new Color(0.65f, 0.25f, 0.12f, 1f), width: 94f, height: 50f);
            Button btnRecall = CreateActionButton("Btn_Recall", unitSecObj.transform, "🌀 Recall\\n<size=10>(Citadel)</size>", fontBold, 11, btnNormal, new Color(0.42f, 0.18f, 0.58f, 1f), width: 82f, height: 50f);

            // Universal End Turn Button
            Button btnEndTurn = CreateActionButton("Btn_EndTurn", actionBarObj.transform, "⏳ END TURN", fontBold, 13, btnNormal, new Color(0.78f, 0.25f, 0.18f, 1f), width: 125f, height: 52f);
            RectTransform endTurnRect = btnEndTurn.GetComponent<RectTransform>();
            endTurnRect.anchorMin = new Vector2(1f, 0.5f);
            endTurnRect.anchorMax = new Vector2(1f, 0.5f);
            endTurnRect.pivot = new Vector2(1f, 0.5f);
            endTurnRect.anchoredPosition = new Vector2(-12f, 0f);'''

new_block = '''            // Direct Children in Right-Side Command Dock
            Text txtUnanchoredHeader = CreateLabel("Txt_UnanchoredHeader", actionBarObj.transform, "🌀 <b>CITADEL GATEWAY</b>", fontBold, 11, new Color(0.85f, 0.65f, 1.0f, 1f), TextAnchor.MiddleCenter);
            txtUnanchoredHeader.GetComponent<LayoutElement>().preferredHeight = 22f;

            Button btnTearRift = CreateActionButton("Btn_TearRift", actionBarObj.transform, "🌀 <b>Tear Open Rift</b>", fontBold, 11, btnNormal, new Color(0.42f, 0.16f, 0.65f, 1f), width: 155f, height: 34f);
            Button btnRandomizeMap = CreateActionButton("Btn_RandomizeMap", actionBarObj.transform, "🎲 <b>Roll Map [R]</b>", fontBold, 10, btnNormal, new Color(0.20f, 0.28f, 0.40f, 1f), width: 155f, height: 30f);

            Text txtRiftHeader = CreateLabel("Txt_RiftHeader", actionBarObj.transform, "🌀 <b>CITADEL RIFT</b>", fontBold, 11, new Color(0.85f, 0.65f, 1.0f, 1f), TextAnchor.MiddleCenter);
            txtRiftHeader.GetComponent<LayoutElement>().preferredHeight = 22f;

            Button btnOpenRiftDispatch = CreateActionButton("Btn_OpenRiftDispatch", actionBarObj.transform, "📦 <b>Squad Dispatch</b>", fontBold, 11, btnNormal, new Color(0.40f, 0.15f, 0.65f, 1f), width: 155f, height: 34f);

            // Dummy legacy sections placed outside actionBarObj so layout is never corrupted
            GameObject unanchoredSecObj = CreateUIObject("Section_Unanchored", container.transform);
            unanchoredSecObj.SetActive(false);
            GameObject riftSecObj = CreateUIObject("Section_RiftPanel", container.transform);
            riftSecObj.SetActive(false);

            // Legacy unit abilities placed under container and hidden
            GameObject unitSecObj = CreateUIObject("Section_UnitAbilities", container.transform);
            unitSecObj.SetActive(false);

            Button btnMove = CreateActionButton("Btn_Move", unitSecObj.transform, "🚶 Move", fontBold, 11, btnNormal, new Color(0.18f, 0.35f, 0.58f, 1f), width: 85f, height: 32f);
            Button btnFire = CreateActionButton("Btn_Fireball", unitSecObj.transform, "🔥 Fireball", fontBold, 11, btnNormal, new Color(0.65f, 0.22f, 0.10f, 1f), width: 85f, height: 32f);
            Button btnWater = CreateActionButton("Btn_WaterSurge", unitSecObj.transform, "💧 Water", fontBold, 11, btnNormal, new Color(0.10f, 0.42f, 0.68f, 1f), width: 85f, height: 32f);
            Button btnEarth = CreateActionButton("Btn_EarthPillar", unitSecObj.transform, "⛰️ Earth", fontBold, 11, btnNormal, new Color(0.48f, 0.32f, 0.18f, 1f), width: 85f, height: 32f);
            Button btnPush = CreateActionButton("Btn_Push", unitSecObj.transform, "💨 Push", fontBold, 11, btnNormal, new Color(0.38f, 0.18f, 0.52f, 1f), width: 85f, height: 32f);
            Button btnHarvest = CreateActionButton("Btn_HarvestCore", unitSecObj.transform, "⚡ Siphon", fontBold, 11, btnNormal, new Color(0.65f, 0.55f, 0.12f, 1f), width: 85f, height: 32f);
            Button btnStrike = CreateActionButton("Btn_TitanStrike", unitSecObj.transform, "🐾 Strike", fontBold, 11, btnNormal, new Color(0.55f, 0.20f, 0.15f, 1f), width: 85f, height: 32f);
            Button btnCata = CreateActionButton("Btn_Cataclysm", unitSecObj.transform, "🌋 Cataclysm", fontBold, 11, btnNormal, new Color(0.72f, 0.22f, 0.08f, 1f), width: 85f, height: 32f);
            Button btnSummonAbility = CreateActionButton("Btn_SummonTitanAbility", unitSecObj.transform, "🌋 Titan", fontBold, 11, btnNormal, new Color(0.65f, 0.25f, 0.12f, 1f), width: 85f, height: 32f);
            Button btnRecall = CreateActionButton("Btn_Recall", unitSecObj.transform, "🌀 Recall", fontBold, 11, btnNormal, new Color(0.42f, 0.18f, 0.58f, 1f), width: 85f, height: 32f);

            // Dedicated Bottom-Right End Turn Button (Decoupled from right dock)
            Button btnEndTurn = CreateActionButton("Btn_EndTurn", container.transform, "⏳ <b>END TURN</b>\\n<size=11><color=#CFD8DC>[Space / Click]</color></size>", fontBold, 13, btnNormal, new Color(0.72f, 0.20f, 0.16f, 0.95f), width: 180f, height: 56f);
            RectTransform endTurnRect = btnEndTurn.GetComponent<RectTransform>();
            endTurnRect.anchorMin = new Vector2(1f, 0f);
            endTurnRect.anchorMax = new Vector2(1f, 0f);
            endTurnRect.pivot = new Vector2(1f, 0f);
            endTurnRect.anchoredPosition = new Vector2(-24f, 24f);
            endTurnRect.sizeDelta = new Vector2(180f, 56f);'''

old_normalized = old_block.replace('\r\n', '\n')
text_normalized = text.replace('\r\n', '\n')

if old_normalized in text_normalized:
    text_normalized = text_normalized.replace(old_normalized, new_block.replace('\r\n', '\n'), 1)
    with open('Assets/Scripts/Editor/CombatHudCanvasBuilder.cs', 'w', encoding='utf-8') as f:
        f.write(text_normalized)
    print("SUCCESS: Updated CombatHudCanvasBuilder.cs")
else:
    print("FAILED to find old block in CombatHudCanvasBuilder.cs")
