#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ElementalHexTactics3D.UI;

namespace ElementalHexTactics3D.Editor
{
    /// <summary>
    /// Builder utility to generate the complete Visual Novel dialogue canvas in SampleScene.
    /// Accessible via menu: "Elemental Hex 3D" -> "Generate Story Dialogue Canvas".
    /// </summary>
    public static class StoryDialogueCanvasBuilder
    {
        private const string PanelFramePath = "Assets/Sprites/UI/UI_WindowFrame_RPG.png";
        private const string PanelBgPath = "Assets/Sprites/UI/UI_WindowBg_RPG.png";
        private const string PauseIconPath = "Assets/Sprites/UI/UI_PauseIcon_RPG.png";
        private const string BtnNormalPath = "Assets/Sprites/UI/UI_Button_Normal.png";
        private const string FontBoldPath = "Assets/Fonts/Font_Bold.ttf";
        private const string FontRegularPath = "Assets/Fonts/Font_Regular.ttf";

        private const string McSilhouettePath = "Assets/Sprites/Portraits/mc_silhouette.png";
        private const string PaladinPortraitPath = "Assets/Sprites/Portraits/paladinportrait.png";
        private const string HolyShielderPortraitPath = "Assets/Sprites/Portraits/holyshielderportrait.png";
        private const string DemonLordPortraitPath = "Assets/Sprites/Portraits/demonlordportrait.png";
        private const string MinotaurPortraitPath = "Assets/Sprites/Portraits/minotaurportrait.png";

        private const string BgMcRoomPath = "Assets/Sprites/Backgrounds/bg_mc_room.jpg";
        private const string BgCathedralPath = "Assets/Sprites/Backgrounds/bg_holyland_cathedral.jpg";
        private const string BgDemonLordPath = "Assets/Sprites/Backgrounds/bg_demonlord_throne.jpg";

        [MenuItem("Elemental Hex 3D/Generate Story Dialogue Canvas", false, 52)]
        public static void BuildStoryDialogueCanvas()
        {
            Debug.Log("<color=#FFD54F><b>[Story VN Builder]</b></color> Generating Visual Novel Dialogue Canvas...");

            // Ensure all portraits, UI, and backgrounds are imported as Sprite 2D & UI
            ConfigureSpriteImporter(PanelFramePath);
            ConfigureSpriteImporter(PanelBgPath);
            ConfigureSpriteImporter(PauseIconPath);
            ConfigureSpriteImporter(McSilhouettePath);
            ConfigureSpriteImporter(PaladinPortraitPath);
            ConfigureSpriteImporter(HolyShielderPortraitPath);
            ConfigureSpriteImporter(DemonLordPortraitPath);
            ConfigureSpriteImporter(MinotaurPortraitPath);
            ConfigureSpriteImporter(BgMcRoomPath);
            ConfigureSpriteImporter(BgCathedralPath);
            ConfigureSpriteImporter(BgDemonLordPath);
            AssetDatabase.Refresh();

            Sprite panelFrame = AssetDatabase.LoadAssetAtPath<Sprite>(PanelFramePath);
            Sprite panelBg = AssetDatabase.LoadAssetAtPath<Sprite>(PanelBgPath);
            Sprite pauseIcon = AssetDatabase.LoadAssetAtPath<Sprite>(PauseIconPath);
            Sprite btnNormal = AssetDatabase.LoadAssetAtPath<Sprite>(BtnNormalPath);

            Sprite mcSilhouette = AssetDatabase.LoadAssetAtPath<Sprite>(McSilhouettePath);
            Sprite paladin = AssetDatabase.LoadAssetAtPath<Sprite>(PaladinPortraitPath);
            Sprite holyShielder = AssetDatabase.LoadAssetAtPath<Sprite>(HolyShielderPortraitPath);
            Sprite demonLord = AssetDatabase.LoadAssetAtPath<Sprite>(DemonLordPortraitPath);
            Sprite minotaur = AssetDatabase.LoadAssetAtPath<Sprite>(MinotaurPortraitPath);

            Sprite bgMc = AssetDatabase.LoadAssetAtPath<Sprite>(BgMcRoomPath);
            Sprite bgCath = AssetDatabase.LoadAssetAtPath<Sprite>(BgCathedralPath);
            Sprite bgDemon = AssetDatabase.LoadAssetAtPath<Sprite>(BgDemonLordPath);

            Font fontBold = AssetDatabase.LoadAssetAtPath<Font>(FontBoldPath);
            Font fontRegular = AssetDatabase.LoadAssetAtPath<Font>(FontRegularPath);
            if (fontBold == null) fontBold = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (fontRegular == null) fontRegular = fontBold;

            // 1. Remove existing Canvas if present to recreate cleanly
            GameObject existingCanvas = GameObject.Find("Canvas_StoryDialogue");
            if (existingCanvas != null)
            {
                Undo.DestroyObjectImmediate(existingCanvas);
            }

            // 2. Create Canvas Root
            GameObject canvasObj = new GameObject("Canvas_StoryDialogue", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200; // Above title menu (100) and combat HUD (50)

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            StoryDialogueUI dialogueUI = canvasObj.AddComponent<StoryDialogueUI>();

            // 3. Root Panel Container (with full-screen raycast blocker)
            GameObject rootPanel = CreateUIObject("Panel_StoryDialogueRoot", canvasObj.transform);
            SetStretchAll(rootPanel.GetComponent<RectTransform>());
            Image rootBlocker = rootPanel.AddComponent<Image>();
            rootBlocker.color = new Color(0f, 0f, 0f, 0f);
            rootBlocker.raycastTarget = true;

            // 4. Background Art (Fullscreen 16:9)
            GameObject bgObj = CreateUIObject("BackgroundArt", rootPanel.transform);
            SetStretchAll(bgObj.GetComponent<RectTransform>());
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = Color.white;
            bgImg.type = Image.Type.Simple;
            bgImg.preserveAspect = false;
            bgImg.raycastTarget = false;
            bgObj.SetActive(false);

            // 5. Glitch Overlay (Fullscreen tint flash)
            GameObject glitchObj = CreateUIObject("GlitchOverlay", rootPanel.transform);
            SetStretchAll(glitchObj.GetComponent<RectTransform>());
            Image glitchImg = glitchObj.AddComponent<Image>();
            glitchImg.color = new Color(0.95f, 0.2f, 0.2f, 0.4f);
            glitchImg.raycastTarget = false;
            glitchObj.SetActive(false);

            // 6. Left & Right Character Portraits (Placed on top of text box, behind the name box)
            GameObject leftPortraitObj = CreateUIObject("Portrait_Left", rootPanel.transform);
            RectTransform lpRect = leftPortraitObj.GetComponent<RectTransform>();
            lpRect.anchorMin = new Vector2(0.5f, 0f);
            lpRect.anchorMax = new Vector2(0.5f, 0f);
            lpRect.pivot = new Vector2(0.5f, 0f);
            lpRect.sizeDelta = new Vector2(350f, 350f);
            lpRect.anchoredPosition = new Vector2(-497f, 280f);
            Image lpImg = leftPortraitObj.AddComponent<Image>();
            lpImg.preserveAspect = true;
            lpImg.raycastTarget = false;
            CanvasGroup lpGroup = leftPortraitObj.AddComponent<CanvasGroup>();
            leftPortraitObj.SetActive(false);

            GameObject rightPortraitObj = CreateUIObject("Portrait_Right", rootPanel.transform);
            RectTransform rpRect = rightPortraitObj.GetComponent<RectTransform>();
            rpRect.anchorMin = new Vector2(0.5f, 0f);
            rpRect.anchorMax = new Vector2(0.5f, 0f);
            rpRect.pivot = new Vector2(0.5f, 0f);
            rpRect.sizeDelta = new Vector2(350f, 350f);
            rpRect.anchoredPosition = new Vector2(495.5f, 281f);
            Image rpImg = rightPortraitObj.AddComponent<Image>();
            rpImg.preserveAspect = true;
            rpImg.raycastTarget = false;
            CanvasGroup rpGroup = rightPortraitObj.AddComponent<CanvasGroup>();
            rightPortraitObj.SetActive(false);

            // 7. Dialogue Box Panel (Bottom Center) - Rendered in front of character bust
            GameObject boxObj = CreateUIObject("DialogueBox", rootPanel.transform);
            RectTransform boxRect = boxObj.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0f);
            boxRect.anchorMax = new Vector2(0.5f, 0f);
            boxRect.pivot = new Vector2(0.5f, 0f);
            boxRect.sizeDelta = new Vector2(1360f, 250f);
            boxRect.anchoredPosition = new Vector2(0f, 30f);

            // Layer 1: Back Section (Solid rich dark slate grey background fill)
            Image boxBg = boxObj.AddComponent<Image>();
            boxBg.sprite = null;
            boxBg.type = Image.Type.Simple;
            boxBg.color = new Color(0.12f, 0.14f, 0.19f, 0.92f); // Semi-translucent dark slate grey, 92% opacity
            boxBg.raycastTarget = true;

            // Layer 2: Frame Section (9-slice hollow golden/blue frame overlay)
            GameObject frameOverlay = CreateUIObject("Frame_Overlay", boxObj.transform);
            SetStretchAll(frameOverlay.GetComponent<RectTransform>());
            Image frameImg = frameOverlay.AddComponent<Image>();
            if (panelFrame != null)
            {
                frameImg.sprite = panelFrame;
                frameImg.type = Image.Type.Sliced;
                frameImg.color = Color.white;
            }
            frameImg.raycastTarget = false;
            LayoutElement foLe = frameOverlay.AddComponent<LayoutElement>();
            foLe.ignoreLayout = true;

            // 8. Nameplate (Top Left of Dialogue Box, overlapping top border directly in front of character bust)
            GameObject nameplateObj = CreateUIObject("Nameplate", boxObj.transform);
            RectTransform npRect = nameplateObj.GetComponent<RectTransform>();
            npRect.anchorMin = new Vector2(0f, 1f);
            npRect.anchorMax = new Vector2(0f, 1f);
            npRect.pivot = new Vector2(0f, 0.5f); // Astride top border
            npRect.sizeDelta = new Vector2(280f, 48f);
            npRect.anchoredPosition = new Vector2(30f, 0f);

            Image npBg = nameplateObj.AddComponent<Image>();
            npBg.sprite = null;
            npBg.type = Image.Type.Simple;
            npBg.color = new Color(0.14f, 0.17f, 0.25f, 0.96f); // Solid dark slate/navy fill

            GameObject npFrame = CreateUIObject("Frame_Overlay", nameplateObj.transform);
            SetStretchAll(npFrame.GetComponent<RectTransform>());
            Image npFrameImg = npFrame.AddComponent<Image>();
            if (panelFrame != null)
            {
                npFrameImg.sprite = panelFrame;
                npFrameImg.type = Image.Type.Sliced;
                npFrameImg.color = Color.white;
            }
            npFrameImg.raycastTarget = false;
            LayoutElement npfoLe = npFrame.AddComponent<LayoutElement>();
            npfoLe.ignoreLayout = true;

            GameObject nameTextObj = CreateUIObject("Text_SpeakerName", nameplateObj.transform);
            SetStretchAll(nameTextObj.GetComponent<RectTransform>());
            Text txtName = nameTextObj.AddComponent<Text>();
            txtName.font = fontBold;
            txtName.fontSize = 20;
            txtName.fontStyle = FontStyle.Bold;
            txtName.color = new Color(0.95f, 0.97f, 1.0f);
            txtName.alignment = TextAnchor.MiddleCenter;
            txtName.text = "Character Name";

            // 9. Dialogue Body Text
            GameObject bodyTextObj = CreateUIObject("Text_DialogueBody", boxObj.transform);
            RectTransform bRect = bodyTextObj.GetComponent<RectTransform>();
            bRect.anchorMin = Vector2.zero;
            bRect.anchorMax = Vector2.one;
            bRect.offsetMin = new Vector2(40f, 25f);
            bRect.offsetMax = new Vector2(-40f, -40f);

            Text txtBody = bodyTextObj.AddComponent<Text>();
            txtBody.font = fontRegular;
            txtBody.fontSize = 22;
            txtBody.color = new Color(0.94f, 0.96f, 0.98f);
            txtBody.lineSpacing = 1.25f;
            txtBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            txtBody.verticalOverflow = VerticalWrapMode.Overflow;
            txtBody.text = "Dialogue text line reveals here character-by-character...";

            // 10. Continue Prompt Indicator (Bottom Right)
            GameObject contObj = CreateUIObject("ContinueIndicator", boxObj.transform);
            RectTransform cRect = contObj.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(1f, 0f);
            cRect.anchorMax = new Vector2(1f, 0f);
            cRect.pivot = new Vector2(1f, 0f);
            cRect.sizeDelta = new Vector2(200f, 36f);
            cRect.anchoredPosition = new Vector2(-25f, 15f);

            GameObject contIconObj = CreateUIObject("Icon_Pause", contObj.transform);
            RectTransform ciRect = contIconObj.GetComponent<RectTransform>();
            ciRect.anchorMin = new Vector2(1f, 0.5f);
            ciRect.anchorMax = new Vector2(1f, 0.5f);
            ciRect.pivot = new Vector2(1f, 0.5f);
            ciRect.sizeDelta = new Vector2(24f, 24f);
            ciRect.anchoredPosition = new Vector2(0f, 0f);
            Image ciImg = contIconObj.AddComponent<Image>();
            if (pauseIcon != null) ciImg.sprite = pauseIcon;
            ciImg.raycastTarget = false;

            GameObject contTextObj = CreateUIObject("Text", contObj.transform);
            RectTransform ctRect = contTextObj.GetComponent<RectTransform>();
            ctRect.anchorMin = new Vector2(0f, 0f);
            ctRect.anchorMax = new Vector2(1f, 1f);
            ctRect.offsetMin = Vector2.zero;
            ctRect.offsetMax = new Vector2(-28f, 0f);
            Text txtCont = contTextObj.AddComponent<Text>();
            txtCont.font = fontRegular;
            txtCont.fontSize = 16;
            txtCont.fontStyle = FontStyle.Italic;
            txtCont.color = new Color(0.85f, 0.88f, 0.95f, 0.85f);
            txtCont.alignment = TextAnchor.MiddleRight;
            txtCont.text = "Klik / Spasi";

            // 11. Skip Button (Top Right of Screen)
            GameObject skipObj = CreateUIObject("Btn_Skip", rootPanel.transform);
            RectTransform sRect = skipObj.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(1f, 1f);
            sRect.anchorMax = new Vector2(1f, 1f);
            sRect.pivot = new Vector2(1f, 1f);
            sRect.sizeDelta = new Vector2(150f, 44f);
            sRect.anchoredPosition = new Vector2(-30f, -30f);

            Image skipImg = skipObj.AddComponent<Image>();
            if (btnNormal != null) skipImg.sprite = btnNormal;
            Button btnSkip = skipObj.AddComponent<Button>();

            GameObject skipTextObj = CreateUIObject("Text", skipObj.transform);
            SetStretchAll(skipTextObj.GetComponent<RectTransform>());
            Text txtSkip = skipTextObj.AddComponent<Text>();
            txtSkip.font = fontBold;
            txtSkip.fontSize = 16;
            txtSkip.fontStyle = FontStyle.Bold;
            txtSkip.color = Color.white;
            txtSkip.alignment = TextAnchor.MiddleCenter;
            txtSkip.text = "⏩ SKIP (Esc)";

            // 12. Wire references into StoryDialogueUI
            SerializedObject so = new SerializedObject(dialogueUI);
            so.FindProperty("rootDialoguePanel").objectReferenceValue = rootPanel;
            so.FindProperty("backgroundArtImage").objectReferenceValue = bgImg;
            so.FindProperty("glitchOverlayImage").objectReferenceValue = glitchImg;
            so.FindProperty("dialogueBoxRect").objectReferenceValue = boxRect;

            so.FindProperty("portraitLeft").objectReferenceValue = lpImg;
            so.FindProperty("portraitRight").objectReferenceValue = rpImg;
            so.FindProperty("portraitLeftGroup").objectReferenceValue = lpGroup;
            so.FindProperty("portraitRightGroup").objectReferenceValue = rpGroup;

            so.FindProperty("txtSpeakerName").objectReferenceValue = txtName;
            so.FindProperty("txtDialogueBody").objectReferenceValue = txtBody;
            so.FindProperty("continueIndicator").objectReferenceValue = contObj;
            so.FindProperty("btnSkip").objectReferenceValue = btnSkip;

            so.FindProperty("mcSilhouetteSprite").objectReferenceValue = mcSilhouette;
            so.FindProperty("paladinPortrait").objectReferenceValue = paladin;
            so.FindProperty("holyShielderPortrait").objectReferenceValue = holyShielder;
            so.FindProperty("demonLordPortrait").objectReferenceValue = demonLord;
            so.FindProperty("basaltVanguardPortrait").objectReferenceValue = minotaur;

            so.FindProperty("bgMcRoom").objectReferenceValue = bgMc;
            so.FindProperty("bgCathedral").objectReferenceValue = bgCath;
            so.FindProperty("bgDemonLordThrone").objectReferenceValue = bgDemon;

            so.ApplyModifiedProperties();

            // Default: root panel inactive until a sequence is triggered
            rootPanel.SetActive(false);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=#4CAF50><b>[Story VN Builder] SUCCESS!</b></color> Canvas_StoryDialogue created cleanly in scene!");
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static void SetStretchAll(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        private static void ConfigureSpriteImporter(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                bool modified = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    modified = true;
                }
                if (importer.alphaIsTransparency != true)
                {
                    importer.alphaIsTransparency = true;
                    modified = true;
                }
                if (importer.maxTextureSize < 2048)
                {
                    importer.maxTextureSize = 2048;
                    modified = true;
                }
                if (modified)
                {
                    importer.SaveAndReimport();
                }
            }
        }
    }
}
#endif
