#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ElementalHexTactics3D.UI;
using ElementalHexTactics3D.UI.Hub;

namespace ElementalHexTactics3D.Editor
{
    /// <summary>
    /// Automation builder to construct the Citadel Town Hub ("Edge of the World" sanctuary)
    /// inside Canvas_TitleMenu with user-generated custom sprites, ground shadows,
    /// dynamic tooltip banners, facility modals, and seamless 3D hex combat transitions.
    /// Menu item: "Elemental Hex 3D" -> "Build Town Hub Canvas"
    /// </summary>
    [InitializeOnLoad]
    public static class TownHubCanvasBuilder
    {
        private const string HubDir = "Assets/Sprites/Hub/";
        private const string BgPath = HubDir + "background.png";
        private const string TowerPath = HubDir + "demontower.png";
        private const string BlacksmithPath = HubDir + "blacksmith.png";
        private const string PortalPath = HubDir + "portal.png";
        private const string BarrackPath = HubDir + "barrack.png";
        private const string MineFarmPath = HubDir + "minefarm.png";
        private const string DemonStatuePath = HubDir + "demonstatue.png";

        private const string BtnNormalPath = "Assets/Sprites/UI/UI_Button_Normal.png";
        private const string PanelFramePath = "Assets/Sprites/UI/UI_Panel_Frame.png";
        private const string FontBoldPath = "Assets/Fonts/Font_Bold.ttf";
        private const string FontRegularPath = "Assets/Fonts/Font_Regular.ttf";

        static TownHubCanvasBuilder()
        {
            EditorApplication.delayCall += EnsureTownHubBuilt;
        }

        public static void EnsureTownHubBuilt()
        {
            GameObject canvasObj = GameObject.Find("Canvas_TitleMenu");
            if (canvasObj != null && canvasObj.transform.Find("Panel_TownHub") == null)
            {
                BuildTownHub();
            }
        }

        [MenuItem("Elemental Hex 3D/Build Town Hub Canvas", false, 1)]
        public static void BuildTownHub()
        {
            Debug.Log("<color=#7C4DFF><b>[Town Hub Builder]</b></color> Configuring Hub sprites and building Citadel Hub UI...");

            // 1. Configure and refresh sprites
            AssetDatabase.Refresh();
            ConfigureSprite(BgPath, false);
            ConfigureSprite(TowerPath, true);
            ConfigureSprite(BlacksmithPath, true);
            ConfigureSprite(PortalPath, true);
            ConfigureSprite(BarrackPath, true);
            ConfigureSprite(MineFarmPath, true);
            ConfigureSprite(DemonStatuePath, true);
            ConfigureSprite(BtnNormalPath, true);
            ConfigureSprite(PanelFramePath, true);

            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BgPath);
            Sprite towerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TowerPath);
            Sprite blacksmithSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BlacksmithPath);
            Sprite portalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PortalPath);
            Sprite barrackSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BarrackPath);
            Sprite mineFarmSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MineFarmPath);
            Sprite statueSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DemonStatuePath);

            Sprite btnNormal = AssetDatabase.LoadAssetAtPath<Sprite>(BtnNormalPath);
            Sprite panelFrame = AssetDatabase.LoadAssetAtPath<Sprite>(PanelFramePath);

            Font fontBold = AssetDatabase.LoadAssetAtPath<Font>(FontBoldPath);
            Font fontRegular = AssetDatabase.LoadAssetAtPath<Font>(FontRegularPath);
            if (fontBold == null) fontBold = GetFallbackFont();
            if (fontRegular == null) fontRegular = fontBold;

            // 2. Locate or ensure Canvas_TitleMenu exists
            GameObject canvasObj = GameObject.Find("Canvas_TitleMenu");
            if (canvasObj == null)
            {
                Debug.Log("<color=#FF9800><b>[Town Hub Builder]</b></color> Canvas_TitleMenu not found! Generating base canvas first...");
                TitleMenuCanvasBuilder.GenerateTitleCanvas();
                canvasObj = GameObject.Find("Canvas_TitleMenu");
            }

            if (canvasObj == null)
            {
                Debug.LogError("[Town Hub Builder] Failed to find or create Canvas_TitleMenu!");
                return;
            }

            TitleMenuCanvasUI titleMenuUI = canvasObj.GetComponent<TitleMenuCanvasUI>();

            // 3. Clean up existing Panel_TownHub if present
            Transform existingHub = canvasObj.transform.Find("Panel_TownHub");
            if (existingHub != null)
            {
                Undo.DestroyObjectImmediate(existingHub.gameObject);
            }

            // 4. Create Panel_TownHub Root (1920x1080 Stretch)
            GameObject hubRoot = CreateUIObject("Panel_TownHub", canvasObj.transform);
            SetStretchAll(hubRoot.GetComponent<RectTransform>());

            // Ensure TownHubManager component exists on hubRoot
            TownHubManager hubMgr = hubRoot.AddComponent<TownHubManager>();

            // 5. Layer 0: Background Graphic
            GameObject bgObj = CreateUIObject("Background_Landscape", hubRoot.transform);
            SetStretchAll(bgObj.GetComponent<RectTransform>());
            Image bgImg = bgObj.AddComponent<Image>();
            if (bgSprite != null)
            {
                bgImg.sprite = bgSprite;
                bgImg.color = Color.white;
                bgImg.type = Image.Type.Simple;
                bgImg.preserveAspect = false; // Stretches across 1920x1080 cleanly
            }
            else
            {
                bgImg.color = new Color(0.08f, 0.09f, 0.13f, 1f);
            }
            bgImg.raycastTarget = false;

            // 6. Layer 1: Ground Shadows Container (Soft Ambient Occlusion beneath buildings)
            GameObject shadowContainer = CreateUIObject("Container_GroundShadows", hubRoot.transform);
            SetStretchAll(shadowContainer.GetComponent<RectTransform>());

            Transform sTower = CreateGroundShadow("Shadow_Tower", shadowContainer.transform, new Vector2(-715f, -170f), new Vector2(380f, 55f));
            Transform sSmith = CreateGroundShadow("Shadow_Blacksmith", shadowContainer.transform, new Vector2(-390f, -170f), new Vector2(340f, 50f));
            Transform sPortal = CreateGroundShadow("Shadow_Portal", shadowContainer.transform, new Vector2(-15f, -175f), new Vector2(340f, 55f));
            Transform sBarrack = CreateGroundShadow("Shadow_Barrack", shadowContainer.transform, new Vector2(325f, -170f), new Vector2(340f, 50f));
            Transform sMine = CreateGroundShadow("Shadow_Mine", shadowContainer.transform, new Vector2(645f, -170f), new Vector2(280f, 45f));
            Transform sStatue = CreateGroundShadow("Shadow_Statue", shadowContainer.transform, new Vector2(780f, 75f), new Vector2(240f, 40f));

            // 7. Layer 2: Subtle Ambient Light Spills (Glows under glowing fires & portal)
            GameObject glowContainer = CreateUIObject("Container_LightSpills", hubRoot.transform);
            SetStretchAll(glowContainer.GetComponent<RectTransform>());

            CreateGlowSpill("Glow_Forge", glowContainer.transform, new Vector2(-460f, -135f), new Vector2(180f, 180f), new Color(0.2f, 0.6f, 1.0f, 0.35f));
            CreateGlowSpill("Glow_Portal", glowContainer.transform, new Vector2(-15f, -100f), new Vector2(280f, 280f), new Color(0.25f, 0.75f, 1.0f, 0.40f));
            CreateGlowSpill("Glow_Barracks", glowContainer.transform, new Vector2(400f, -145f), new Vector2(140f, 140f), new Color(0.2f, 0.5f, 1.0f, 0.30f));
            CreateGlowSpill("Glow_Mine", glowContainer.transform, new Vector2(665f, -120f), new Vector2(160f, 160f), new Color(0.5f, 0.9f, 1.0f, 0.35f));
            CreateGlowSpill("Glow_Statue", glowContainer.transform, new Vector2(745f, 135f), new Vector2(180f, 180f), new Color(0.7f, 0.25f, 1.0f, 0.45f));

            // 8. Layer 3: The 6 Interactive Buildings Container
            GameObject buildingsContainer = CreateUIObject("Container_Buildings", hubRoot.transform);
            SetStretchAll(buildingsContainer.GetComponent<RectTransform>());

            // Facility 1: Demon Castle
            TownBuildingNode nodeCastle = CreateBuildingNode(
                "Building_DemonCastle",
                buildingsContainer.transform,
                towerSprite,
                new Vector2(-715f, 70f),
                new Vector2(440f, 520f),
                HubFacilityType.DemonCastle,
                "✦ DEMON LORD CITADEL ✦",
                "Sanctum of the exiled sovereign. Unlock Demon Lord System perks, upgrade domain attributes, and issue decrees.",
                "<color=#FFCC00>Rank I Citadel • 3 Perks Available</color>",
                sTower
            );

            // Facility 2: Emancipation Forge
            TownBuildingNode nodeForge = CreateBuildingNode(
                "Building_EmancipationForge",
                buildingsContainer.transform,
                blacksmithSprite,
                new Vector2(-390f, -25f),
                new Vector2(380f, 310f),
                HubFacilityType.EmancipationForge,
                "✦ EMANCIPATION FORGE ✦",
                "The anvil of liberation. Shatter Cursed Slave Collars from rescued demi-humans and forge abyssal dark weaponry.",
                "<color=#FF8844>Collars Pending: 2 Rescued</color>",
                sSmith
            );

            // Facility 3: Abyssal Portal (GATEWAY TO 3D BATTLE)
            TownBuildingNode nodePortal = CreateBuildingNode(
                "Building_AbyssalPortal",
                buildingsContainer.transform,
                portalSprite,
                new Vector2(-15f, -10f),
                new Vector2(380f, 380f),
                HubFacilityType.AbyssalPortal,
                "✦ ABYSSAL RIFT [ENTER BATTLE] ✦",
                "Gateway across the sanctuary border. Deploy your vanguard forces onto the 3D Hex tactical battlefield!",
                "<color=#44FF88>Rift Stable • Click to Embark</color>",
                sPortal
            );

            // Facility 4: Monster Barracks
            TownBuildingNode nodeBarracks = CreateBuildingNode(
                "Building_MonsterBarracks",
                buildingsContainer.transform,
                barrackSprite,
                new Vector2(325f, -25f),
                new Vector2(380f, 330f),
                HubFacilityType.MonsterBarracks,
                "✦ MONSTER BARRACKS & DEN ✦",
                "Warcamp built from leviathan rib bones. Inspect, arm, and recruit companion minions (Goblins, Kobolds, Slimes).",
                "<color=#66CCFF>Squad: 2/4 Active</color>",
                sBarrack
            );

            // Facility 5: Mana Mine & Farm
            TownBuildingNode nodeMine = CreateBuildingNode(
                "Building_ManaMineFarm",
                buildingsContainer.transform,
                mineFarmSprite,
                new Vector2(645f, -55f),
                new Vector2(330f, 275f),
                HubFacilityType.ManaMine,
                "✦ MANA MINE & SPORE FARMS ✦",
                "Subterranean mana crystal veins and blighted dark soil plots. Assign freed outcasts to harvest passive resources.",
                "<color=#AA88FF>Yield: +50 Mana / Expedition</color>",
                sMine
            );

            // Facility 6: Ancient Horned Deity Shrine
            TownBuildingNode nodeStatue = CreateBuildingNode(
                "Building_AncientDeityShrine",
                buildingsContainer.transform,
                statueSprite,
                new Vector2(780f, 220f),
                new Vector2(280f, 280f),
                HubFacilityType.AncientDeityShrine,
                "✦ ANCIENT DEITY SHRINE ✦",
                "Colossal horned idol on the cliff's edge. Offer soul embers in the sacrificial brazier for ancient beast summoning (Gacha)!",
                "<color=#FF55AA>Ritual Ready • 1x Summon Available</color>",
                sStatue
            );

            // 9. Layer 4: Top Domain Header & Resource Bar
            GameObject headerBar = CreateUIObject("Panel_TopDomainHeader", hubRoot.transform);
            RectTransform headerRect = headerBar.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.sizeDelta = new Vector2(0f, 65f);
            headerRect.anchoredPosition = Vector2.zero;

            Image headerBg = headerBar.AddComponent<Image>();
            headerBg.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);

            // Gold accent stripe beneath header
            GameObject headerStripe = CreateUIObject("Stripe", headerBar.transform);
            RectTransform sRect = headerStripe.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0f, 0f);
            sRect.anchorMax = new Vector2(1f, 0f);
            sRect.pivot = new Vector2(0.5f, 0f);
            sRect.sizeDelta = new Vector2(0f, 3f);
            sRect.anchoredPosition = Vector2.zero;
            Image strImg = headerStripe.AddComponent<Image>();
            strImg.color = new Color(0.95f, 0.75f, 0.2f, 0.9f);

            // Domain Title Banner Text
            Text txtDomainTitle = CreateUIText(
                "Text_DomainTitle",
                headerBar.transform,
                "✦ CITADEL OF THE OUTCASTS — EDGE OF THE WORLD ✦",
                fontBold,
                16,
                FontStyle.Bold,
                new Color(1f, 0.9f, 0.45f),
                new Vector2(-380f, 0f),
                new Vector2(500f, 36f),
                TextAnchor.MiddleLeft
            );

            // Resource Displays (Mana Crystals, Soul Embers, Freed Outcasts)
            Text txtMana = CreateUIText(
                "Text_ManaCrystals",
                headerBar.transform,
                "💎 450 Mana",
                fontBold,
                15,
                FontStyle.Bold,
                new Color(0.35f, 0.85f, 1.0f),
                new Vector2(120f, 0f),
                new Vector2(160f, 32f),
                TextAnchor.MiddleCenter
            );

            Text txtEmbers = CreateUIText(
                "Text_SoulEmbers",
                headerBar.transform,
                "🔥 180 Embers",
                fontBold,
                15,
                FontStyle.Bold,
                new Color(1.0f, 0.55f, 0.25f),
                new Vector2(280f, 0f),
                new Vector2(160f, 32f),
                TextAnchor.MiddleCenter
            );

            Text txtOutcasts = CreateUIText(
                "Text_FreedOutcasts",
                headerBar.transform,
                "🛡️ 16 Outcasts",
                fontBold,
                15,
                FontStyle.Bold,
                new Color(0.55f, 1.0f, 0.65f),
                new Vector2(440f, 0f),
                new Vector2(160f, 32f),
                TextAnchor.MiddleCenter
            );

            // Return to Title Screen Button
            Button btnReturnTitle = CreateSmallButton("Btn_ReturnTitle", headerBar.transform, "🏠 TITLE", fontBold, 13, btnNormal, Color.white, new Vector2(850f, 0f), new Vector2(130f, 36f));

            // 10. Layer 5: Dynamic Header Tooltip Banner (Glow banner showing hovered building info)
            GameObject tooltipBanner = CreateUIObject("Panel_TooltipBanner", hubRoot.transform);
            RectTransform ttRect = tooltipBanner.GetComponent<RectTransform>();
            ttRect.anchorMin = new Vector2(0.5f, 1f);
            ttRect.anchorMax = new Vector2(0.5f, 1f);
            ttRect.pivot = new Vector2(0.5f, 1f);
            ttRect.sizeDelta = new Vector2(720f, 95f);
            ttRect.anchoredPosition = new Vector2(0f, -80f);

            CanvasGroup ttGroup = tooltipBanner.AddComponent<CanvasGroup>();
            ttGroup.alpha = 0f;
            ttGroup.blocksRaycasts = false;

            Image ttBg = tooltipBanner.AddComponent<Image>();
            ttBg.color = new Color(0.05f, 0.07f, 0.11f, 0.95f);

            // Thin gold border on tooltip
            Outline ttOutline = tooltipBanner.AddComponent<Outline>();
            ttOutline.effectColor = new Color(0.9f, 0.72f, 0.2f, 0.85f);
            ttOutline.effectDistance = new Vector2(2f, -2f);

            Text txtTtTitle = CreateUIText(
                "Text_TooltipTitle",
                tooltipBanner.transform,
                "✦ BUILDING TITLE ✦",
                fontBold,
                16,
                FontStyle.Bold,
                new Color(1f, 0.88f, 0.35f),
                new Vector2(0f, 26f),
                new Vector2(680f, 26f)
            );

            Text txtTtDesc = CreateUIText(
                "Text_TooltipDesc",
                tooltipBanner.transform,
                "Building description lore and details...",
                fontRegular,
                12,
                FontStyle.Normal,
                new Color(0.9f, 0.92f, 0.95f),
                new Vector2(0f, 4f),
                new Vector2(680f, 24f)
            );

            Text txtTtStatus = CreateUIText(
                "Text_TooltipStatus",
                tooltipBanner.transform,
                "Active Status Tag",
                fontBold,
                12,
                FontStyle.Bold,
                new Color(0.4f, 0.95f, 0.6f),
                new Vector2(0f, -22f),
                new Vector2(680f, 22f)
            );

            tooltipBanner.SetActive(false);

            // 11. Layer 6: Facility Management Modals
            GameObject modalCastle = CreateFacilityModal("Modal_DemonCastle", hubRoot.transform, "👑 DEMON LORD CITADEL", fontBold, fontRegular, panelFrame, btnNormal,
                "The core sanctum of your sovereign authority. Here you manage the territory domain, unlock system perks, and spend soul embers to strengthen your vanguard forces.",
                "Prowess: Tier I\nDomain Radius: Border Wastelands\nDecree: Protected Sanctuary",
                "🔓 UNLOCK PERK (-50 Embers)",
                () => hubMgr.ActionUnlockPerk()
            );

            GameObject modalForge = CreateFacilityModal("Modal_EmancipationForge", hubRoot.transform, "⚒️ EMANCIPATION FORGE", fontBold, fontRegular, panelFrame, btnNormal,
                "A holy blacksmith converted to a liberation workshop. Broken chains and slave collars from captured demi-humans are shattered here to release their repressed power.",
                "Anvil Heat: 1,400°C\nSlave Collars In Queue: 2\nMetal Reserves: High",
                "⛓️ SHATTER COLLAR (-30 Embers)",
                () => hubMgr.ActionShatterCollar()
            );

            GameObject modalBarracks = CreateFacilityModal("Modal_MonsterBarracks", hubRoot.transform, "🐺 MONSTER DEN & BARRACKS", fontBold, fontRegular, panelFrame, btnNormal,
                "Constructed from ancient leviathan rib bones, this encampment trains and equips small demi-human skirmishers (Goblins, Kobolds, Slimes) to fight alongside your champions.",
                "Roster: 2/4 Vanguard Monsters\nMorale: Eager for battle\nDaily Rations: Full",
                "🗡️ RECRUIT MINION (-60 Crystals)",
                () => hubMgr.ActionRecruitMinion()
            );

            GameObject modalMine = CreateFacilityModal("Modal_ManaMine", hubRoot.transform, "⛏️ MANA MINE & SPORE FARMS", fontBold, fontRegular, panelFrame, btnNormal,
                "Deep fissures into obsidian rock yield raw glowing mana crystals, while dark soil terraces cultivate bioluminescent mushrooms for alchemy and magical sustains.",
                "Minecart Status: Operating\nAssigned Miners: 4 Kobolds\nDaily Yield: +50 Crystals",
                "👷 ASSIGN LABORER (-1 Outcast)",
                () => hubMgr.ActionAssignMineWorker()
            );

            GameObject modalShrine = CreateFacilityModal("Modal_AncientDeityShrine", hubRoot.transform, "🔮 ANCIENT HORNED DEITY SHRINE", fontBold, fontRegular, panelFrame, btnNormal,
                "A monumental weathered stone idol of an elder horned god overlooking the abyss. Offer harvested soul embers into the sacrificial cauldron to summon ancient colossal beasts (Gacha).",
                "Sacrificial Flame: Active\nNext Summon Guarantee: 8 Pulls\nAttunement: Abyssal Titan",
                "🔥 SUMMON BEAST (-80 Embers)",
                () => hubMgr.ActionPerformGachaSummon()
            );

            // Hide modals initially
            modalCastle.SetActive(false);
            modalForge.SetActive(false);
            modalBarracks.SetActive(false);
            modalMine.SetActive(false);
            modalShrine.SetActive(false);

            // 12. Wire serialized fields to TownHubManager
            SerializedObject soHub = new SerializedObject(hubMgr);
            soHub.FindProperty("txtManaCrystals").objectReferenceValue = txtMana;
            soHub.FindProperty("txtSoulEmbers").objectReferenceValue = txtEmbers;
            soHub.FindProperty("txtFreedOutcasts").objectReferenceValue = txtOutcasts;

            soHub.FindProperty("tooltipBannerPanel").objectReferenceValue = tooltipBanner;
            soHub.FindProperty("txtTooltipTitle").objectReferenceValue = txtTtTitle;
            soHub.FindProperty("txtTooltipDesc").objectReferenceValue = txtTtDesc;
            soHub.FindProperty("txtTooltipStatus").objectReferenceValue = txtTtStatus;
            soHub.FindProperty("tooltipCanvasGroup").objectReferenceValue = ttGroup;

            soHub.FindProperty("modalCastle").objectReferenceValue = modalCastle;
            soHub.FindProperty("modalForge").objectReferenceValue = modalForge;
            soHub.FindProperty("modalBarracks").objectReferenceValue = modalBarracks;
            soHub.FindProperty("modalMine").objectReferenceValue = modalMine;
            soHub.FindProperty("modalShrine").objectReferenceValue = modalShrine;

            soHub.FindProperty("btnReturnTitle").objectReferenceValue = btnReturnTitle;
            soHub.ApplyModifiedProperties();

            // 13. Wire townHubPanel to TitleMenuCanvasUI
            SerializedObject soMenu = new SerializedObject(titleMenuUI);
            SerializedProperty hubProp = soMenu.FindProperty("townHubPanel");
            if (hubProp != null)
            {
                hubProp.objectReferenceValue = hubRoot;
                soMenu.ApplyModifiedProperties();
            }

            // Initially hide Hub panel until player clicks PLAY on Title Screen
            hubRoot.SetActive(false);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=#4CAF50><b>[Town Hub Builder] SUCCESS!</b></color> Citadel Town Hub with 6 facilities, ground shadows, tooltips, and modals built cleanly!");
        }

        private static TownBuildingNode CreateBuildingNode(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 size, HubFacilityType type, string title, string lore, string status, Transform shadow)
        {
            GameObject obj = CreateUIObject(name, parent);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;

            Image img = obj.AddComponent<Image>();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.color = Color.white;
                img.preserveAspect = true;
            }
            else
            {
                img.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }

            TownBuildingNode node = obj.AddComponent<TownBuildingNode>();
            node.Setup(type, title, lore, status, img, shadow);

            return node;
        }

        private static Transform CreateGroundShadow(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            GameObject shadowObj = CreateUIObject(name, parent);
            RectTransform rect = shadowObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;

            Image img = shadowObj.AddComponent<Image>();
            img.color = new Color(0.01f, 0.01f, 0.02f, 0.65f); // Deep soft contact shadow
            img.raycastTarget = false;

            return shadowObj.transform;
        }

        private static void CreateGlowSpill(string name, Transform parent, Vector2 pos, Vector2 size, Color color)
        {
            GameObject glowObj = CreateUIObject(name, parent);
            RectTransform rect = glowObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;

            Image img = glowObj.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        private static GameObject CreateFacilityModal(string name, Transform parent, string title, Font fontBold, Font fontRegular, Sprite panelFrame, Sprite btnSprite, string lore, string stats, string actionLabel, UnityEngine.Events.UnityAction onAction)
        {
            GameObject modalRoot = CreateUIObject(name, parent);
            SetStretchAll(modalRoot.GetComponent<RectTransform>());

            // Backdrop dimmer
            GameObject backdrop = CreateUIObject("Backdrop", modalRoot.transform);
            SetStretchAll(backdrop.GetComponent<RectTransform>());
            Image bdImg = backdrop.AddComponent<Image>();
            bdImg.color = new Color(0f, 0f, 0f, 0.75f);

            // Modal Card
            GameObject cardObj = CreateUIObject("CardFrame", modalRoot.transform);
            RectTransform cardRect = cardObj.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(560f, 380f);
            cardRect.anchoredPosition = Vector2.zero;

            Image cardImg = cardObj.AddComponent<Image>();
            if (panelFrame != null)
            {
                cardImg.sprite = panelFrame;
                cardImg.color = Color.white;
            }
            else
            {
                cardImg.color = new Color(0.08f, 0.11f, 0.16f, 0.98f);
            }

            // Title
            CreateUIText("Text_Title", cardObj.transform, title, fontBold, 19, FontStyle.Bold, new Color(1f, 0.88f, 0.35f), new Vector2(0f, 140f), new Vector2(500f, 32f));

            // Lore
            CreateUIText("Text_Lore", cardObj.transform, lore, fontRegular, 13, FontStyle.Normal, new Color(0.85f, 0.88f, 0.92f), new Vector2(0f, 65f), new Vector2(480f, 85f));

            // Stats box
            GameObject statsBox = CreateUIObject("StatsBox", cardObj.transform);
            RectTransform sbRect = statsBox.GetComponent<RectTransform>();
            sbRect.anchorMin = new Vector2(0.5f, 0.5f);
            sbRect.anchorMax = new Vector2(0.5f, 0.5f);
            sbRect.pivot = new Vector2(0.5f, 0.5f);
            sbRect.sizeDelta = new Vector2(480f, 75f);
            sbRect.anchoredPosition = new Vector2(0f, -25f);
            Image sbImg = statsBox.AddComponent<Image>();
            sbImg.color = new Color(0.04f, 0.06f, 0.09f, 0.85f);

            CreateUIText("Text_Stats", statsBox.transform, stats, fontRegular, 12, FontStyle.Normal, new Color(0.45f, 0.85f, 1.0f), Vector2.zero, new Vector2(460f, 65f), TextAnchor.MiddleLeft);

            // Action Button
            Button btnAction = CreateSmallButton("Btn_Action", cardObj.transform, actionLabel, fontBold, 14, btnSprite, new Color(1f, 0.9f, 0.35f), new Vector2(-120f, -125f), new Vector2(230f, 44f));
            if (onAction != null) btnAction.onClick.AddListener(onAction);

            // Close Button
            Button btnClose = CreateSmallButton("Btn_Close", cardObj.transform, "✖ CLOSE", fontBold, 14, btnSprite, Color.white, new Vector2(130f, -125f), new Vector2(180f, 44f));
            btnClose.onClick.AddListener(() =>
            {
                if (TownHubManager.Instance != null)
                {
                    TownHubManager.Instance.CloseActiveModal();
                }
            });

            return modalRoot;
        }

        private static Button CreateSmallButton(string name, Transform parent, string label, Font font, int fontSize, Sprite bgSprite, Color textColor, Vector2 pos, Vector2 size)
        {
            GameObject btnObj = CreateUIObject(name, parent);
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;

            Image img = btnObj.AddComponent<Image>();
            if (bgSprite != null)
            {
                img.sprite = bgSprite;
                img.color = Color.white;
            }
            else
            {
                img.color = new Color(0.15f, 0.2f, 0.28f, 0.95f);
            }

            Button btn = btnObj.AddComponent<Button>();

            Text text = CreateUIText("Text", btnObj.transform, label, font, fontSize, FontStyle.Bold, textColor, Vector2.zero, size);
            text.raycastTarget = false;

            return btn;
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

        private static void ConfigureSprite(string assetPath, bool isTransparent)
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
                if (importer.alphaIsTransparency != isTransparent)
                {
                    importer.alphaIsTransparency = isTransparent;
                    dirty = true;
                }
                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    dirty = true;
                }
                if (dirty)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static Font GetFallbackFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return font;
        }
    }
}
#endif
