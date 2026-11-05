with open('Assets/Scripts/UI/CombatHudCanvasUI.cs', 'r', encoding='utf-8') as f:
    text = f.read()

idx1 = text.find('// ================= 3. BOTTOM ACTION BAR (Bottom-Center) ================= //')
idx2 = text.find('// Universal End Turn Button (Dedicated Bottom-Right Widget)')

if idx1 != -1 and idx2 != -1:
    old_slice = text[idx1:idx2]
    replacement = '''// ================= 3. CITADEL GATEWAY & RIFT PANEL (Right-Side Command Dock) ================= //
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
            LayoutElement leTR = btnTearRift.gameObject.AddComponent<LayoutElement>();
            leTR.preferredHeight = 34f;
            leTR.minHeight = 34f;
            leTR.preferredWidth = 155f;

            btnRandomizeMap = CreateBtn("Btn_RandomizeMap", actionBarRoot.transform, "🎲 <b>Roll Map [R]</b>", font, 10, btnNormal, new Color(0.20f, 0.28f, 0.40f, 1f), 155f, 30f);
            LayoutElement leRM = btnRandomizeMap.gameObject.AddComponent<LayoutElement>();
            leRM.preferredHeight = 30f;
            leRM.minHeight = 30f;
            leRM.preferredWidth = 155f;

            txtRiftHeader = CreateTxt("Txt_RiftHeader", actionBarRoot.transform, "🌀 <b>CITADEL RIFT</b>", font, 11, new Color(0.85f, 0.65f, 1.0f, 1f), TextAnchor.MiddleCenter);
            txtRiftHeader.fontStyle = FontStyle.Bold;
            LayoutElement leH2 = txtRiftHeader.gameObject.AddComponent<LayoutElement>();
            leH2.preferredHeight = 22f;
            leH2.minWidth = 155f;

            btnOpenRiftDispatch = CreateBtn("Btn_OpenRiftDispatch", actionBarRoot.transform, "📦 <b>Squad Dispatch</b>", font, 11, btnNormal, new Color(0.40f, 0.15f, 0.65f, 1f), 155f, 34f);
            txtOpenRiftDispatch = btnOpenRiftDispatch.GetComponentInChildren<Text>();
            LayoutElement leRD = btnOpenRiftDispatch.gameObject.AddComponent<LayoutElement>();
            leRD.preferredHeight = 34f;
            leRD.minHeight = 34f;
            leRD.preferredWidth = 155f;

            unanchoredSection = null;
            riftPanelSection = null;
            unitAbilitiesSection = null;

            '''
    text = text[:idx1] + replacement + text[idx2:]
    with open('Assets/Scripts/UI/CombatHudCanvasUI.cs', 'w', encoding='utf-8') as f:
        f.write(text)
    print("SUCCESS: replaced dynamic HUD slice!")
else:
    print(f"FAILED to find indices: idx1={idx1}, idx2={idx2}")
