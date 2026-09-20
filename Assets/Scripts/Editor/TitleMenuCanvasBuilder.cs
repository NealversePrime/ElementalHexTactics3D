#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using ElementalHexTactics3D.UI;

namespace ElementalHexTactics3D.Editor
{
    /// <summary>
    /// Automation builder to generate a complete, high-fidelity 2D UGUI Canvas hierarchy
    /// in SampleScene with custom AI-generated fantasy RPG button & panel sprites.
    /// Accessible via menu: "Elemental Hex 3D" -> "Generate 2D Title Canvas".
    /// </summary>
    public static class TitleMenuCanvasBuilder
    {
        private const string BtnNormalPath = "Assets/Sprites/UI/UI_Button_Normal.png";
        private const string BtnHighlightPath = "Assets/Sprites/UI/UI_Button_Highlight.png";
        private const string PanelFramePath = "Assets/Sprites/UI/UI_Panel_Frame.png";
        private const string FontBoldPath = "Assets/Fonts/Font_Bold.ttf";
        private const string FontRegularPath = "Assets/Fonts/Font_Regular.ttf";

        [MenuItem("Elemental Hex 3D/Advanced/Regenerate Base Title Canvas", false, 51)]
        public static void GenerateTitleCanvas()
        {
            Debug.Log("<color=#FFD54F><b>[2D UI Builder]</b></color> Configuring UI sprite textures & generating 2D Canvas...");

            // 1. Refresh AssetDatabase and configure sprite textures
            AssetDatabase.Refresh();
            ConfigureSpriteImporter(BtnNormalPath);
            ConfigureSpriteImporter(BtnHighlightPath);
            ConfigureSpriteImporter(PanelFramePath);

            Sprite btnNormal = AssetDatabase.LoadAssetAtPath<Sprite>(BtnNormalPath);
            Sprite btnHighlight = AssetDatabase.LoadAssetAtPath<Sprite>(BtnHighlightPath);
            Sprite panelFrame = AssetDatabase.LoadAssetAtPath<Sprite>(PanelFramePath);

            // 2. Load custom fonts
            Font fontBold = AssetDatabase.LoadAssetAtPath<Font>(FontBoldPath);
            Font fontRegular = AssetDatabase.LoadAssetAtPath<Font>(FontRegularPath);
            if (fontBold == null) fontBold = GetFallbackFont();
            if (fontRegular == null) fontRegular = fontBold;

            // 3. Ensure EventSystem exists
            EnsureEventSystem();

            // 4. Remove existing Canvas if present to recreate cleanly
            GameObject existingCanvas = GameObject.Find("Canvas_TitleMenu");
            if (existingCanvas != null)
            {
                Undo.DestroyObjectImmediate(existingCanvas);
            }

            // Remove legacy TitleMenuManager3D to avoid conflicting state
            GameObject legacyMgr = GameObject.Find("TitleMenuManager3D");
            if (legacyMgr != null)
            {
                Undo.DestroyObjectImmediate(legacyMgr);
                Debug.Log("<color=#FFD54F><b>[2D UI Builder]</b></color> Cleaned up redundant TitleMenuManager3D GameObject.");
            }

            // 5. Create Canvas Root (Screen Space - Overlay)
            GameObject canvasObj = new GameObject("Canvas_TitleMenu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            TitleMenuCanvasUI menuUI = canvasObj.AddComponent<TitleMenuCanvasUI>();

            // 6. Root Title Panel Container (Transparent - NO OPAQUE BLACK BOX, 3D DIORAMA IS VISIBLE!)
            GameObject titleRootObj = CreateUIObject("Panel_TitleScreenRoot", canvasObj.transform);
            SetStretchAll(titleRootObj.GetComponent<RectTransform>());

            // Soft Diorama Dimmer / Vignette (Keeps 3D diorama visible while giving UI dramatic contrast)
            GameObject dimmerObj = CreateUIObject("DioramaDimmer", titleRootObj.transform);
            SetStretchAll(dimmerObj.GetComponent<RectTransform>());
            Image dimmerImg = dimmerObj.AddComponent<Image>();
            dimmerImg.color = new Color(0.04f, 0.06f, 0.09f, 0.45f);
            dimmerImg.raycastTarget = false;

            // Top Gold Decorative Stripe (Thin 6px bar at very top)
            GameObject stripeObj = CreateUIObject("TopGoldStripe", titleRootObj.transform);
            RectTransform stripeRect = stripeObj.GetComponent<RectTransform>();
            stripeRect.anchorMin = new Vector2(0f, 1f);
            stripeRect.anchorMax = new Vector2(1f, 1f);
            stripeRect.pivot = new Vector2(0.5f, 1f);
            stripeRect.sizeDelta = new Vector2(0f, 6f);
            stripeRect.anchoredPosition = Vector2.zero;
            Image stripeImg = stripeObj.AddComponent<Image>();
            stripeImg.color = new Color(0.98f, 0.75f, 0.15f, 0.95f);

            // 7. Panel: Title Card (Header with UI_Panel_Frame)
            GameObject headerObj = CreateUIObject("Panel_TitleHeader", titleRootObj.transform);
            RectTransform headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.5f, 1f);
            headerRect.anchorMax = new Vector2(0.5f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.sizeDelta = new Vector2(880f, 260f);
            headerRect.anchoredPosition = new Vector2(0f, -30f);

            Image headerImg = headerObj.AddComponent<Image>();
            if (panelFrame != null)
            {
                headerImg.sprite = panelFrame;
                headerImg.color = Color.white;
                headerImg.type = Image.Type.Simple;
            }
            else
            {
                headerImg.color = new Color(0.08f, 0.11f, 0.16f, 0.95f);
            }

            // Header Texts inside Title Card
            CreateUIText("Text_Badge", headerObj.transform, "✦ LIGHT NOVEL / MANHWA EDITION • PROLOGUE: RANK-F ✦", fontBold, 15, FontStyle.Bold, new Color(0.25f, 0.78f, 1.0f), new Vector2(0f, 75f), new Vector2(840f, 28f));
            CreateUIText("Text_Title1", headerObj.transform, "I REINCARNATED AS A BEAST TALKER,", fontBold, 28, FontStyle.Bold, Color.white, new Vector2(0f, 32f), new Vector2(840f, 48f));
            CreateUIText("Text_Title2", headerObj.transform, "NOW I'M COLLECTING BEASTS!", fontBold, 34, FontStyle.Bold, new Color(1f, 0.85f, 0.25f), new Vector2(0f, -14f), new Vector2(840f, 52f));
            CreateUIText("Text_Subtitle", headerObj.transform, "〜 2.5D Tactical Hex Battle & Primordial Resonance 〜", fontRegular, 15, FontStyle.Italic, new Color(0.80f, 0.85f, 0.92f), new Vector2(0f, -58f), new Vector2(840f, 26f));
            CreateUIText("Text_Version", headerObj.transform, "Elemental Hex Tactics 3D • TGFI Pre-Alpha v0.2.0 • Solo Dev by Neal Sage", fontRegular, 12, FontStyle.Normal, new Color(0.60f, 0.65f, 0.72f), new Vector2(0f, -86f), new Vector2(840f, 22f));

            // 8. Panel: Menu Buttons (Center)
            GameObject menuBoxObj = CreateUIObject("Panel_MenuButtons", titleRootObj.transform);
            RectTransform menuBoxRect = menuBoxObj.GetComponent<RectTransform>();
            menuBoxRect.anchorMin = new Vector2(0.5f, 0.5f);
            menuBoxRect.anchorMax = new Vector2(0.5f, 0.5f);
            menuBoxRect.pivot = new Vector2(0.5f, 0.5f);
            menuBoxRect.sizeDelta = new Vector2(380f, 310f);
            menuBoxRect.anchoredPosition = new Vector2(0f, -75f);

            VerticalLayoutGroup vlg = menuBoxObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            Button btnPlay = CreateCustomButton("Btn_PlayAdventure", menuBoxObj.transform, "⚔️ PLAY ADVENTURE", fontBold, 20, btnHighlight != null ? btnHighlight : btnNormal, new Color(1f, 0.92f, 0.45f), height: 52f);
            Button btnStory = CreateCustomButton("Btn_StoryLore", menuBoxObj.transform, "📖 STORY & LORE (Rank-F)", fontBold, 17, btnNormal, Color.white, height: 50f);
            Button btnHowToPlay = CreateCustomButton("Btn_HowToPlay", menuBoxObj.transform, "🎮 HOW TO PLAY (Tactics)", fontBold, 17, btnNormal, Color.white, height: 50f);
            Button btnOptions = CreateCustomButton("Btn_Options", menuBoxObj.transform, "⚙️ OPTIONS (Settings)", fontBold, 17, btnNormal, Color.white, height: 50f);
            Button btnExit = CreateCustomButton("Btn_Exit", menuBoxObj.transform, "🚪 EXIT GAME", fontBold, 17, btnNormal, new Color(0.95f, 0.55f, 0.55f), height: 50f);

            // 9. Modals Container
            GameObject modalsContainer = CreateUIObject("Panel_ModalsContainer", canvasObj.transform);
            SetStretchAll(modalsContainer.GetComponent<RectTransform>());

            // Modal: Story Prologue
            GameObject storyCardObj;
            GameObject storyModalObj = CreateModalPanel("Modal_StoryPrologue", modalsContainer.transform, "📖 STORY PROLOGUE: THE WEAKEST TAMER'S AWAKENING", fontBold, panelFrame, 860f, 560f, 195f, out storyCardObj);

            // Subtle gold divider
            GameObject storyDivider = CreateUIObject("Divider", storyCardObj.transform);
            SetAnchoredPos(storyDivider.GetComponent<RectTransform>(), new Vector2(0f, 170f), new Vector2(760f, 2f));
            Image sDivImg = storyDivider.AddComponent<Image>();
            sDivImg.color = new Color(0.85f, 0.72f, 0.38f, 0.5f);

            string storyContent = 
                "<b><color=#38BDF8>[ The Reincarnation ]</color></b>\n" +
                "You were summoned from modern Earth into <b>Terranox</b>, a brutal fantasy realm where nobility and authority belong exclusively to beast summoners.\n\n" +
                "<b><color=#EF5350>[ The Tyrant's Cruelty: Cursed Collars ]</color></b>\n" +
                "Imperial aristocrats treat beasts as disposable war tools, torturing them into obedience with painful <b>Cursed Collars</b>. Having zero physical strength or destructive battle mana, the Adventurer's Guild stamped you as a pathetic <b>Rank-F Trash Tamer</b>.\n\n" +
                "<b><color=#FBBF24>[ The Divine Cheat: Beast Resonance ]</color></b>\n" +
                "Unknown to anyone, you possess the God-Given Cheat: <b>[Beast Talker & Primordial Resonance]</b>. You understand the souls and cries of beasts! When treated with empathy, your beasts awaken dormant elemental powers, physically terraforming the 3D hex earth!\n\n" +
                "<b><color=#34D399>[ Your Mission: Break Chains & Awaken Titans ]</color></b>\n" +
                "Enter dangerous 3D dungeon plateaus, defeat corrupt summoners to shatter their cursed collars, gather ancient <b>Elemental Cores</b>, and hatch apocalyptic Titans!";
            CreateUIText("Text_StoryBody", storyCardObj.transform, storyContent, fontRegular, 15, FontStyle.Normal, new Color(0.85f, 0.88f, 0.94f), new Vector2(0f, -5f), new Vector2(760f, 320f), TextAnchor.UpperLeft);
            Button btnBackStory = CreateCustomButton("Btn_BackStory", storyCardObj.transform, "🔙 BACK TO MENU", fontBold, 18, btnNormal, Color.white, width: 260f, height: 48f);
            SetAnchoredPos(btnBackStory.GetComponent<RectTransform>(), new Vector2(0f, -220f), new Vector2(260f, 48f));

            // Modal: How to Play
            GameObject howToPlayCardObj;
            GameObject howToPlayModalObj = CreateModalPanel("Modal_HowToPlay", modalsContainer.transform, "🎮 TACTICAL COMBAT & TERRAFORM GUIDE", fontBold, panelFrame, 860f, 580f, 205f, out howToPlayCardObj);

            // Subtle gold divider
            GameObject htpDivider = CreateUIObject("Divider", howToPlayCardObj.transform);
            SetAnchoredPos(htpDivider.GetComponent<RectTransform>(), new Vector2(0f, 180f), new Vector2(760f, 2f));
            Image hDivImg = htpDivider.AddComponent<Image>();
            hDivImg.color = new Color(0.85f, 0.72f, 0.38f, 0.5f);

            string howToPlayContent = 
                "<b><color=#38BDF8>1. Camera Controls:</color></b> <b>Q / E</b> Rotate 60° | <b>WASD</b> Pan Diorama | <b>Scroll</b> Zoom in/out\n" +
                "<b><color=#38BDF8>2. Tactical Orders:</color></b> <b>Left-Click</b> to Select Unit / Cast Spell | <b>Right-Click</b> to Cancel\n\n" +
                "<b><color=#FBBF24>3. Dynamic Terraforming (Divinity Style):</color></b>\n" +
                "• 🔥 <b>Fireball:</b> Scorches grass into molten <b>Magma</b> (damages enemies, gives Fire Titan +2 ATK!).\n" +
                "• 💧 <b>Water Blast:</b> Extinguishes lava into <b>Steam Smokescreens</b>; forms water pools.\n" +
                "• 🪨 <b>Earth Spire:</b> Raises high <b>Stone Pillars</b> (creates physical barriers & collision surfaces).\n\n" +
                "<b><color=#EF5350>4. Kinetic Push & Wall Slams (Into the Breach):</color></b>\n" +
                "• 💥 <b>Kinetic Shove:</b> Push enemies 1 hex away. Slamming an enemy into a Stone Pillar, cliff, or another unit triggers <b>💥 -2 HP WALL SLAM damage</b>!\n" +
                "• Shove fragile enemy Tamers into lava or water to neutralize them instantly!\n\n" +
                "<b><color=#34D399>5. Siphon Land & Cataclysm:</color></b>\n" +
                "• ⚡ <b>Siphon:</b> Your Titan drains active lava into Barren Earth to harvest <b>Elemental Cores</b>.\n" +
                "• 🌋 <b>Magma Cataclysm:</b> Spend 3 Cores to trigger a screen-shattering volcanic blast wiping the field!";
            CreateUIText("Text_HowToPlayBody", howToPlayCardObj.transform, howToPlayContent, fontRegular, 15, FontStyle.Normal, new Color(0.85f, 0.88f, 0.94f), new Vector2(0f, -5f), new Vector2(760f, 340f), TextAnchor.UpperLeft);
            Button btnBackHowToPlay = CreateCustomButton("Btn_BackHowToPlay", howToPlayCardObj.transform, "🔙 BACK TO MENU", fontBold, 18, btnNormal, Color.white, width: 260f, height: 48f);
            SetAnchoredPos(btnBackHowToPlay.GetComponent<RectTransform>(), new Vector2(0f, -230f), new Vector2(260f, 48f));

            // Modal: Options
            GameObject optionsCardObj;
            GameObject optionsModalObj = CreateModalPanel("Modal_Options", modalsContainer.transform, "⚙️ GAME OPTIONS & SETTINGS", fontBold, panelFrame, 600f, 470f, 160f, out optionsCardObj);

            // Subtle gold divider
            GameObject optDivider = CreateUIObject("Divider", optionsCardObj.transform);
            SetAnchoredPos(optDivider.GetComponent<RectTransform>(), new Vector2(0f, 136f), new Vector2(500f, 2f));
            Image oDivImg = optDivider.AddComponent<Image>();
            oDivImg.color = new Color(0.85f, 0.72f, 0.38f, 0.5f);

            Button btnAudioToggle = CreateCustomButton("Btn_AudioToggle", optionsCardObj.transform, "Sound: ON", fontBold, 18, btnNormal, Color.white, width: 380f, height: 48f);
            SetAnchoredPos(btnAudioToggle.GetComponent<RectTransform>(), new Vector2(0f, 80f), new Vector2(380f, 48f));
            Text txtAudioStatus = btnAudioToggle.GetComponentInChildren<Text>();

            Button btnAiToggle = CreateCustomButton("Btn_AiToggle", optionsCardObj.transform, "Enemy AI: ENABLED", fontBold, 18, btnNormal, Color.white, width: 380f, height: 48f);
            SetAnchoredPos(btnAiToggle.GetComponent<RectTransform>(), new Vector2(0f, 22f), new Vector2(380f, 48f));
            Text txtAiStatus = btnAiToggle.GetComponentInChildren<Text>();

            Button btnDisplayToggle = CreateCustomButton("Btn_DisplayToggle", optionsCardObj.transform, "Display: Windowed", fontBold, 18, btnNormal, Color.white, width: 380f, height: 48f);
            SetAnchoredPos(btnDisplayToggle.GetComponent<RectTransform>(), new Vector2(0f, -36f), new Vector2(380f, 48f));
            Text txtDisplayStatus = btnDisplayToggle.GetComponentInChildren<Text>();

            CreateUIText("Text_AiTip", optionsCardObj.transform, "Tip: You can also toggle Enemy AI during battle with hotkey F1.", fontRegular, 14, FontStyle.Italic, new Color(0.6f, 0.65f, 0.72f), new Vector2(0f, -96f), new Vector2(500f, 25f));
            Button btnBackOptions = CreateCustomButton("Btn_BackOptions", optionsCardObj.transform, "🔙 BACK", fontBold, 18, btnNormal, Color.white, width: 220f, height: 46f);
            SetAnchoredPos(btnBackOptions.GetComponent<RectTransform>(), new Vector2(0f, -162f), new Vector2(220f, 46f));

            // Modal: In-Game Pause
            GameObject pauseCardObj;
            GameObject pauseModalObj = CreateModalPanel("Modal_InGamePause", modalsContainer.transform, "✦ BATTLE PAUSED ✦", fontBold, panelFrame, 480f, 450f, 126f, out pauseCardObj);

            // Subtle gold divider under title on dark slate
            GameObject pauseDivider = CreateUIObject("Divider", pauseCardObj.transform);
            SetAnchoredPos(pauseDivider.GetComponent<RectTransform>(), new Vector2(0f, 102f), new Vector2(360f, 2f));
            Image pDivImg = pauseDivider.AddComponent<Image>();
            pDivImg.color = new Color(0.85f, 0.72f, 0.38f, 0.5f);

            Button btnResume = CreateCustomButton("Btn_Resume", pauseCardObj.transform, "▶️ RESUME BATTLE", fontBold, 19, btnHighlight != null ? btnHighlight : btnNormal, new Color(1f, 0.92f, 0.45f), width: 360f, height: 48f);
            SetAnchoredPos(btnResume.GetComponent<RectTransform>(), new Vector2(0f, 60f), new Vector2(360f, 48f));

            Button btnPauseOptions = CreateCustomButton("Btn_PauseOptions", pauseCardObj.transform, "⚙️ OPTIONS", fontBold, 17, btnNormal, Color.white, width: 360f, height: 48f);
            SetAnchoredPos(btnPauseOptions.GetComponent<RectTransform>(), new Vector2(0f, 4f), new Vector2(360f, 48f));

            Button btnRestart = CreateCustomButton("Btn_Restart", pauseCardObj.transform, "🔄 RESTART BATTLE", fontBold, 17, btnNormal, new Color(0.98f, 0.65f, 0.25f), width: 360f, height: 48f);
            SetAnchoredPos(btnRestart.GetComponent<RectTransform>(), new Vector2(0f, -52f), new Vector2(360f, 48f));

            Button btnReturnTitle = CreateCustomButton("Btn_ReturnTitle", pauseCardObj.transform, "🏠 MAIN MENU", fontBold, 17, btnNormal, new Color(0.95f, 0.45f, 0.45f), width: 360f, height: 48f);
            SetAnchoredPos(btnReturnTitle.GetComponent<RectTransform>(), new Vector2(0f, -108f), new Vector2(360f, 48f));

            // Hide modals by default (Hides modalRoot AND its Backdrop completely!)
            storyModalObj.SetActive(false);
            howToPlayModalObj.SetActive(false);
            optionsModalObj.SetActive(false);
            pauseModalObj.SetActive(false);

            // 10. Panel: In-Game HUD
            GameObject hudPanelObj = CreateUIObject("Panel_InGameHUD", canvasObj.transform);
            SetStretchAll(hudPanelObj.GetComponent<RectTransform>());

            Button btnInGameMenu = CreateCustomButton("Btn_InGameMenu", hudPanelObj.transform, "⚙️ Menu (Esc)", fontBold, 15, btnNormal, Color.white, width: 150f, height: 42f);
            RectTransform inGameMenuRect = btnInGameMenu.GetComponent<RectTransform>();
            inGameMenuRect.anchorMin = new Vector2(1f, 1f);
            inGameMenuRect.anchorMax = new Vector2(1f, 1f);
            inGameMenuRect.pivot = new Vector2(1f, 1f);
            inGameMenuRect.sizeDelta = new Vector2(150f, 42f);
            inGameMenuRect.anchoredPosition = new Vector2(-20f, -20f);

            hudPanelObj.SetActive(false);

            // 11. Wire references to TitleMenuCanvasUI via SerializedObject
            SerializedObject so = new SerializedObject(menuUI);
            so.FindProperty("titlePanel").objectReferenceValue = titleRootObj;
            so.FindProperty("inGameHudPanel").objectReferenceValue = hudPanelObj;
            so.FindProperty("storyModal").objectReferenceValue = storyModalObj;
            so.FindProperty("howToPlayModal").objectReferenceValue = howToPlayModalObj;
            so.FindProperty("optionsModal").objectReferenceValue = optionsModalObj;
            so.FindProperty("pauseModal").objectReferenceValue = pauseModalObj;

            so.FindProperty("btnPlay").objectReferenceValue = btnPlay;
            so.FindProperty("btnStory").objectReferenceValue = btnStory;
            so.FindProperty("btnHowToPlay").objectReferenceValue = btnHowToPlay;
            so.FindProperty("btnOptions").objectReferenceValue = btnOptions;
            so.FindProperty("btnExit").objectReferenceValue = btnExit;

            so.FindProperty("btnInGameMenu").objectReferenceValue = btnInGameMenu;
            so.FindProperty("btnResume").objectReferenceValue = btnResume;
            so.FindProperty("btnPauseOptions").objectReferenceValue = btnPauseOptions;
            so.FindProperty("btnRestart").objectReferenceValue = btnRestart;
            so.FindProperty("btnReturnTitle").objectReferenceValue = btnReturnTitle;

            so.FindProperty("btnBackStory").objectReferenceValue = btnBackStory;
            so.FindProperty("btnBackHowToPlay").objectReferenceValue = btnBackHowToPlay;
            so.FindProperty("btnBackOptions").objectReferenceValue = btnBackOptions;

            so.FindProperty("btnAudioToggle").objectReferenceValue = btnAudioToggle;
            so.FindProperty("txtAudioStatus").objectReferenceValue = txtAudioStatus;
            so.FindProperty("btnAiToggle").objectReferenceValue = btnAiToggle;
            so.FindProperty("txtAiStatus").objectReferenceValue = txtAiStatus;
            so.FindProperty("btnDisplayToggle").objectReferenceValue = btnDisplayToggle;
            so.FindProperty("txtDisplayStatus").objectReferenceValue = txtDisplayStatus;

            so.ApplyModifiedProperties();

            // 12. Build or refresh Citadel Town Hub within Canvas_TitleMenu
            TownHubCanvasBuilder.BuildTownHub();

            // Mark Scene Dirty
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=#4CAF50><b>[2D UI Builder] SUCCESS!</b></color> 2D Canvas hierarchy, custom fantasy buttons, and modals created cleanly in scene!");
        }

        public static void RemoveTitleCanvas()
        {
            GameObject canvas = GameObject.Find("Canvas_TitleMenu");
            if (canvas != null)
            {
                Undo.DestroyObjectImmediate(canvas);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                Debug.Log("<color=#FF9800><b>[2D UI Builder]</b></color> Canvas_TitleMenu removed from scene.");
            }
        }

        private static void ConfigureSpriteImporter(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                bool dirty = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    dirty = true;
                }
                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    dirty = true;
                }
                if (importer.wrapMode != TextureWrapMode.Clamp)
                {
                    importer.wrapMode = TextureWrapMode.Clamp;
                    dirty = true;
                }
                if (dirty)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static void EnsureEventSystem()
        {
            EventSystem es = Object.FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<InputSystemUIInputModule>();
            }
            else
            {
                if (es.GetComponent<InputSystemUIInputModule>() == null && es.GetComponent<StandaloneInputModule>() == null)
                {
                    es.gameObject.AddComponent<InputSystemUIInputModule>();
                }
            }
        }

        /// <summary>
        /// Instantiates a proper UGUI GameObject with both RectTransform and CanvasRenderer.
        /// </summary>
        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static Button CreateCustomButton(string name, Transform parent, string label, Font font, int fontSize, Sprite bgSprite, Color textColor, float width = 420f, float height = 56f)
        {
            GameObject btnObj = CreateUIObject(name, parent);
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);

            // Layout element ensures VerticalLayoutGroup respects button dimensions
            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;
            le.minWidth = width;
            le.minHeight = height;

            Image img = btnObj.AddComponent<Image>();
            if (bgSprite != null)
            {
                img.sprite = bgSprite;
                img.color = Color.white;
                img.type = Image.Type.Simple;
            }
            else
            {
                img.color = new Color(0.12f, 0.16f, 0.22f, 0.95f);
            }

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.selectedColor = Color.white;
            btn.colors = colors;

            GameObject textObj = CreateUIObject("Text", btnObj.transform);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            SetStretchAll(textRect);

            Text text = textObj.AddComponent<Text>();
            text.text = label;
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = textColor;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            Shadow shadow = textObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.95f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);

            return btn;
        }

        private static GameObject CreateModalPanel(string name, Transform parent, string titleText, Font font, Sprite panelFrame, float width, float height, float titleY, out GameObject cardObj)
        {
            GameObject modalRoot = CreateUIObject(name, parent);
            SetStretchAll(modalRoot.GetComponent<RectTransform>());

            // Modal Dimmer Backdrop (Soft dark overlay only behind modals)
            GameObject backdrop = CreateUIObject("Backdrop", modalRoot.transform);
            SetStretchAll(backdrop.GetComponent<RectTransform>());
            Image bdImg = backdrop.AddComponent<Image>();
            bdImg.color = new Color(0f, 0f, 0f, 0.78f);

            // Modal Card Frame
            cardObj = CreateUIObject("CardFrame", modalRoot.transform);
            RectTransform cardRect = cardObj.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(width, height);
            cardRect.anchoredPosition = Vector2.zero;

            Image cardImg = cardObj.AddComponent<Image>();
            if (panelFrame != null)
            {
                cardImg.sprite = panelFrame;
                cardImg.color = Color.white;
                cardImg.type = Image.Type.Simple;
            }
            else
            {
                cardImg.color = new Color(0.08f, 0.11f, 0.16f, 0.98f);
            }

            // Header Title Text placed safely on the inner dark slate area for crystal-clear readability
            Text txtTitle = CreateUIText("Text_ModalTitle", cardObj.transform, titleText, font, 20, FontStyle.Bold, new Color(1f, 0.88f, 0.38f), new Vector2(0f, titleY), new Vector2(width - 60f, 34f));
            Shadow shadow = txtTitle.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.95f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);

            return modalRoot;
        }

        private static Text CreateUIText(string name, Transform parent, string content, Font font, int fontSize, FontStyle style, Color color, Vector2 anchoredPos, Vector2 size, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            GameObject textObj = CreateUIObject(name, parent);

            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;

            Text text = textObj.AddComponent<Text>();
            text.text = content;
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            Shadow shadow = textObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.95f);
            shadow.effectDistance = new Vector2(1f, -1f);

            return text;
        }

        private static void SetStretchAll(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        private static void SetAnchoredPos(RectTransform rect, Vector2 pos, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;
        }

        private static Font GetFallbackFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null)
            {
                Font[] allFonts = Resources.FindObjectsOfTypeAll<Font>();
                if (allFonts != null && allFonts.Length > 0) font = allFonts[0];
            }
            return font;
        }
    }
}
#endif
