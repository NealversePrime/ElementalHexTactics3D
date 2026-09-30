using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ElementalHexTactics3D.Grid;
using ElementalHexTactics3D.Units;
using ElementalHexTactics3D.Turn;
using ElementalHexTactics3D.Combat;
using ElementalHexTactics3D.UI;
using ElementalHexTactics3D.UI.Hub;
using ElementalHexTactics3D.Campaign;
using ElementalHexTactics3D.InputHandling;
using ElementalHexTactics3D.CameraControl;

namespace ElementalHexTactics3D.Tutorial
{
    public enum TutorialStage
    {
        None,
        Stage1_HolyCrusade,
        Hub_CitadelAwakening,
        Stage2_FrontierHazards,
        Hub_TitanCrisis,
        Stage3_AbyssalTitans,

        // Backwards compatibility alias
        Stage2_AbyssalAwakening = Stage2_FrontierHazards
    }

    public enum TutorialStep
    {
        None,
        // ================= STAGE 1: PROLOGUE RAID IN OMNITERRA ================= //
        Stage1_SelectPaladin,
        Stage1_MovePaladin,
        Stage1_HolyStrikeDemonLord,
        Stage1_MoveShielderOrEndTurn,
        Stage1_EnemyTurnReaction,
        Stage1_FinishDemonLord,

        // ================= HUB 1: CITADEL AWAKENING & SKIRMISH BRIEFING ================= //
        Hub_CitadelAwakeningDialogue,
        Hub_ClickAbyssalRift,
        Hub_SelectStage2MissionCard,

        // ================= STAGE 2: FRONTIER HAZARDS (NO TITANS YET) ================= //
        Stage2_SelectDemonLord,
        Stage2_CastFireballScorched,
        Stage2_CastFireballMagma,
        Stage2_SelectBasaltVanguard,
        Stage2_PushEnemyIntoHazard,
        Stage2_VanquishScouts,

        // ================= HUB 2: TITAN CRISIS & EXPEDITION GATEWAY ================= //
        Hub_TitanCrisisDialogue,
        Hub_SelectStage3MissionCard,

        // ================= STAGE 3: ABYSSAL RIFT & TITAN AWAKENING ================= //
        Stage3_TearOpenRift,
        Stage3_DeployDemonLord,
        Stage3_SiphonElementalCore,
        Stage3_SummonEarthGolemTitan,
        Stage3_EarthGolemCataclysm,
        Stage3_ObliterateCrusaders,

        // ================= COMPLETION ================= //
        Completed,

        // Compatibility Aliases
        Stage2_CastFireballOnGrass = Stage2_CastFireballScorched,
        Hub_SelectTutorialMissionCard = Hub_SelectStage2MissionCard,
        Stage2_TearOpenRift = Stage3_TearOpenRift,
        Stage2_DeployDemonLord = Stage3_DeployDemonLord,
        Stage2_DeployEarthGolem = Stage3_SummonEarthGolemTitan,
        Stage2_DeployBasaltVanguard = Stage2_SelectBasaltVanguard,
        Stage2_EarthGolemWallSlam = Stage3_EarthGolemCataclysm,
        Stage2_BasaltVanguardWallSlam = Stage2_PushEnemyIntoHazard,
        Stage2_VanquishHolyInvaders = Stage3_ObliterateCrusaders
    }

    /// <summary>
    /// Master controller for the 3-Stage Progressive Tutorial Arc:
    /// 
    /// Stage 1: The Holy Crusade (Omniterra Prologue Raid)
    /// - Paladin & Holy Shielder vs Arrogant Demon Lord Malakor.
    /// - Teaches: Movement, Attack Ranges, and Kinetic Shove.
    /// - Glitch & Transmigration: Inversion of the chessboard by The Architect.
    /// 
    /// Hub 1: Awakening in Citadel
    /// - MC wakes up in Demon Lord's body, greeted by Basalt Vanguard.
    /// - Reports scout raid at outer frontier.
    /// 
    /// Stage 2: Frontier Hazards (Elemental Alchemy & Terrain Hazards - NO TITANS!)
    /// - Demon Lord MC + Basalt Vanguard (infantry tank) vs Radiant Synod Scouts.
    /// - Teaches: Fireball on Grass -> Magma transformation, Burn DoT, Mud slow, and Shoving enemies into hazards.
    /// - War horns sound: The heavy crusade approaches!
    /// 
    /// Hub 2: The Titan Crisis
    /// - Heavy crusade has divine barrier shields that physical weapons cannot pierce.
    /// - Need Primordial Titans summoned through the Abyssal Rift!
    /// 
    /// Stage 3: Abyssal Rift & Titan Awakening
    /// - Teaches: Tear Open Abyssal Rift, Deploy Demon Lord, Siphon Elemental Core,
    ///   Summon Earth Golem Titan, and unleash Seismic Cataclysm!
    /// - Awards 1st Primordial Titan Core and unlocks full Citadel Hub!
    /// </summary>
    public class TutorialScenarioManager : MonoBehaviour
    {
        public static TutorialScenarioManager Instance { get; private set; }

        public TutorialStage CurrentStage { get; private set; } = TutorialStage.None;
        public TutorialStep CurrentStep { get; private set; } = TutorialStep.None;

        public bool IsTutorialActive => CurrentStage != TutorialStage.None && CurrentStage != TutorialStage.Completed;
        public bool IsStage1Active => CurrentStage == TutorialStage.Stage1_HolyCrusade;
        public bool IsHubAwakeningActive => CurrentStage == TutorialStage.Hub_CitadelAwakening || CurrentStage == TutorialStage.Hub_TitanCrisis;
        public bool IsStage2Active => CurrentStage == TutorialStage.Stage2_FrontierHazards;
        public bool IsStage3Active => CurrentStage == TutorialStage.Stage3_AbyssalTitans;

        // References to active tutorial units
        public TacticalUnit3D PaladinUnit { get; private set; }
        public TacticalUnit3D HolyShielderUnit { get; private set; }
        public TacticalUnit3D DemonLordBoss { get; private set; }
        public TacticalUnit3D DemonLordPlayer { get; private set; }
        public TacticalUnit3D BasaltVanguardPlayer { get; private set; }
        public TacticalUnit3D EarthGolemPlayer { get; private set; }

        private HexTile3D currentHighlightedTile = null;
        public HexTile3D CurrentHighlightedTile => currentHighlightedTile;

        public static TutorialScenarioManager EnsureInstance()
        {
            if (Instance != null) return Instance;
            TutorialScenarioManager found = FindFirstObjectByType<TutorialScenarioManager>(FindObjectsInactive.Include);
            if (found != null)
            {
                Instance = found;
                return found;
            }
            GameObject go = new GameObject("TutorialScenarioManager");
            Instance = go.AddComponent<TutorialScenarioManager>();
            return Instance;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (!IsTutorialActive) return;

            // Don't advance steps while Story Dialogue cutscene is playing
            if (StoryDialogueUI.Instance != null && StoryDialogueUI.Instance.IsPlayingDialogue)
            {
                return;
            }

            HexGridInteraction3D interaction = HexGridInteraction3D.Instance;
            TurnManager3D turn = TurnManager3D.Instance;

            // ================= STAGE 1 STEP EVALUATION ================= //
            if (CurrentStage == TutorialStage.Stage1_HolyCrusade)
            {
                switch (CurrentStep)
                {
                    case TutorialStep.Stage1_SelectPaladin:
                        if (interaction != null && interaction.SelectedUnit == PaladinUnit)
                        {
                            SetStep(TutorialStep.Stage1_MovePaladin);
                        }
                        break;

                    case TutorialStep.Stage1_MovePaladin:
                        if (interaction != null && interaction.SelectedUnit != PaladinUnit && PaladinUnit != null && !PaladinUnit.HasMovedThisTurn)
                        {
                            SetStep(TutorialStep.Stage1_SelectPaladin);
                        }
                        else if (PaladinUnit != null && PaladinUnit.HasMovedThisTurn)
                        {
                            SetStep(TutorialStep.Stage1_HolyStrikeDemonLord);
                        }
                        break;

                    case TutorialStep.Stage1_HolyStrikeDemonLord:
                        if (PaladinUnit != null && PaladinUnit.HasActedThisTurn)
                        {
                            SetStep(TutorialStep.Stage1_MoveShielderOrEndTurn);
                        }
                        break;

                    case TutorialStep.Stage1_MoveShielderOrEndTurn:
                        if (turn != null && !turn.IsPlayerTurn)
                        {
                            SetStep(TutorialStep.Stage1_EnemyTurnReaction);
                        }
                        break;

                    case TutorialStep.Stage1_EnemyTurnReaction:
                        if (turn != null && turn.IsPlayerTurn && turn.CurrentRound >= 2)
                        {
                            SetStep(TutorialStep.Stage1_FinishDemonLord);
                        }
                        break;
                }
            }
            // ================= STAGE 2 STEP EVALUATION ================= //
            else if (CurrentStage == TutorialStage.Stage2_FrontierHazards)
            {
                switch (CurrentStep)
                {
                    case TutorialStep.Stage2_SelectDemonLord:
                        if (interaction != null && interaction.SelectedUnit == DemonLordPlayer)
                        {
                            SetStep(TutorialStep.Stage2_CastFireballScorched);
                        }
                        break;

                    case TutorialStep.Stage2_CastFireballScorched:
                        HexGrid3D gridS = HexGrid3D.Instance;
                        HexTile3D centerTileS = gridS != null ? gridS.GetTile(new HexCoordinates(0, 0)) : null;
                        if ((centerTileS != null && centerTileS.State == TileState.Magma) || (DemonLordPlayer != null && DemonLordPlayer.HasActedThisTurn))
                        {
                            SetStep(TutorialStep.Stage2_SelectBasaltVanguard);
                        }
                        break;

                    case TutorialStep.Stage2_CastFireballMagma:
                        SetStep(TutorialStep.Stage2_SelectBasaltVanguard);
                        break;

                    case TutorialStep.Stage2_SelectBasaltVanguard:
                        if (interaction != null && interaction.SelectedUnit == BasaltVanguardPlayer)
                        {
                            SetStep(TutorialStep.Stage2_PushEnemyIntoHazard);
                        }
                        break;

                    case TutorialStep.Stage2_PushEnemyIntoHazard:
                        if (BasaltVanguardPlayer != null && (BasaltVanguardPlayer.HasActedThisTurn || BasaltVanguardPlayer.HasMovedThisTurn))
                        {
                            SetStep(TutorialStep.Stage2_VanquishScouts);
                        }
                        break;
                }
            }
            // ================= STAGE 3 STEP EVALUATION ================= //
            else if (CurrentStage == TutorialStage.Stage3_AbyssalTitans)
            {
                switch (CurrentStep)
                {
                    case TutorialStep.Stage3_TearOpenRift:
                        if (AbyssalRiftConduit3D.Instance != null && AbyssalRiftConduit3D.Instance.RiftTile != null)
                        {
                            SetStep(TutorialStep.Stage3_DeployDemonLord);
                        }
                        break;

                    case TutorialStep.Stage3_DeployDemonLord:
                        if (DemonLordPlayer != null && DemonLordPlayer.gameObject.activeInHierarchy && DemonLordPlayer.CurrentTile != null)
                        {
                            SetStep(TutorialStep.Stage3_SiphonElementalCore);
                        }
                        break;

                    case TutorialStep.Stage3_SiphonElementalCore:
                        if (DemonLordPlayer != null && (DemonLordPlayer.ElementalCores >= 1 || DemonLordPlayer.HasActedThisTurn))
                        {
                            SetStep(TutorialStep.Stage3_SummonEarthGolemTitan);
                        }
                        break;

                    case TutorialStep.Stage3_SummonEarthGolemTitan:
                        if (EarthGolemPlayer != null && EarthGolemPlayer.gameObject.activeInHierarchy && EarthGolemPlayer.CurrentTile != null)
                        {
                            SetStep(TutorialStep.Stage3_EarthGolemCataclysm);
                        }
                        break;

                    case TutorialStep.Stage3_EarthGolemCataclysm:
                        if (EarthGolemPlayer != null && (EarthGolemPlayer.HasActedThisTurn || EarthGolemPlayer.HasMovedThisTurn))
                        {
                            SetStep(TutorialStep.Stage3_ObliterateCrusaders);
                        }
                        break;
                }
            }
        }

        #region Step Tracking & Visual Guidance

        public void SetStep(TutorialStep newStep)
        {
            CurrentStep = newStep;
            Debug.Log($"<color=#FFD54F><b>[Tutorial Step]</b></color> Advanced to: <b>{newStep}</b>");

            TutorialGuidanceBannerUI banner = TutorialGuidanceBannerUI.EnsureInstance();

            switch (newStep)
            {
                // ================= STAGE 1 GUIDANCE ================= //
                case TutorialStep.Stage1_SelectPaladin:
                    if (PaladinUnit != null) SetTutorialHighlightedTile(PaladinUnit.CurrentTile);
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton(null);
                    banner?.ShowGuidance(
                        "[STEP 1/4] SELECT THE PALADIN",
                        "All right... first, I just need to click my Paladin to issue orders...",
                        "Click the Paladin (Chosen Hero) at (0, -1)!",
                        new Color(1f, 0.88f, 0.35f)
                    );
                    break;

                case TutorialStep.Stage1_MovePaladin:
                    HexGrid3D grid1 = HexGrid3D.Instance;
                    HexTile3D targetForwardTile = grid1 != null ? grid1.GetTile(new HexCoordinates(0, 0)) : null;
                    SetTutorialHighlightedTile(targetForwardTile);
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton("Move");
                    banner?.ShowGuidance(
                        "[STEP 2/4] ADVANCE TO BLUE HEX",
                        "The movement range lights up in cyan. Now click the forward hex to close distance on the demon...",
                        "Click the highlighted blue hex at (0, 0) to step forward!",
                        new Color(0.35f, 0.85f, 1.0f)
                    );
                    break;

                case TutorialStep.Stage1_HolyStrikeDemonLord:
                    if (DemonLordBoss != null) SetTutorialHighlightedTile(DemonLordBoss.CurrentTile);
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton("Strike");
                    banner?.ShowGuidance(
                        "[STEP 3/4] UNLEASH HOLY STRIKE",
                        "The demon is within sword reach! Time to unleash Holy Strike and purge this shadow!",
                        "Click [Holy Strike] on the action bar, then strike the Demon Lord!",
                        new Color(1f, 0.45f, 0.25f)
                    );
                    break;

                case TutorialStep.Stage1_MoveShielderOrEndTurn:
                    ClearTutorialHighlightedTile();
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton("EndTurn");
                    banner?.ShowGuidance(
                        "[STEP 4/4] END YOUR TURN",
                        "Solid hit! My Paladin has expended his action. Now let's pass the turn to see what the boss does...",
                        "Click [END TURN] in the bottom right corner!",
                        new Color(0.45f, 0.95f, 0.65f)
                    );
                    break;

                case TutorialStep.Stage1_EnemyTurnReaction:
                    ClearTutorialHighlightedTile();
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton(null);
                    banner?.ShowGuidance(
                        "[ENEMY PHASE] WITHSTAND RETALIATION",
                        "The demon lord counterattacks! But thanks to my paladin's Divine Blessing armor, his dark claws can't break through!",
                        "Observing enemy turn...",
                        new Color(0.95f, 0.35f, 0.25f)
                    );
                    break;

                case TutorialStep.Stage1_FinishDemonLord:
                    if (DemonLordBoss != null) SetTutorialHighlightedTile(DemonLordBoss.CurrentTile);
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton("Strike");
                    banner?.ShowGuidance(
                        "[CRUSADE FINALE] SLAY THE DEMON LORD",
                        "His defenses are down! One final strike will end the raid and claim the solo clear!",
                        "Click [Holy Strike] and deliver the finishing blow to the Demon Lord!",
                        new Color(1f, 0.88f, 0.35f)
                    );
                    break;

                // ================= HUB 1 GUIDANCE ================= //
                case TutorialStep.Hub_CitadelAwakeningDialogue:
                    ClearTutorialHighlightedTile();
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton(null);
                    banner?.HideGuidance();
                    break;

                case TutorialStep.Hub_ClickAbyssalRift:
                    ClearTutorialHighlightedTile();
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton(null);
                    banner?.ShowGuidance(
                        "[CITADEL SANCTUARY] ENTER EXPEDITION GATEWAY",
                        "A scout detachment of the Radiant Synod is raiding our frontier outpost! I need to access the expedition gateway.",
                        "Click the glowing Abyssal Rift Gateway in the center of the Citadel!",
                        new Color(0.95f, 0.75f, 0.25f)
                    );
                    break;

                case TutorialStep.Hub_SelectStage2MissionCard:
                    ClearTutorialHighlightedTile();
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton(null);
                    banner?.ShowGuidance(
                        "[EXPEDITION GATEWAY] SELECT STAGE 2 SKIRMISH",
                        "Card #1 [Frontier Skirmish] is open! Press [EMBARK EXPEDITION] or hit Space/Enter to ride to the frontier!",
                        "Select Card #1 and click [EMBARK EXPEDITION] to deploy!",
                        new Color(1f, 0.85f, 0.35f)
                    );
                    break;

                // ================= STAGE 2 GUIDANCE (HAZARDS & ALCHEMY - NO TITANS) ================= //
                case TutorialStep.Stage2_SelectDemonLord:
                    if (DemonLordPlayer != null) SetTutorialHighlightedTile(DemonLordPlayer.CurrentTile);
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton(null);
                    banner?.ShowGuidance(
                        "[STAGE 2 - STEP 1/4] SELECT DEMON LORD",
                        "Even with Level 1 mana, my game knowledge is intact. Let's select my Demon Lord to cast Fireball...",
                        "Click the Demon Lord at (1, -2)!",
                        new Color(0.95f, 0.45f, 0.35f)
                    );
                    break;

                case TutorialStep.Stage2_CastFireballScorched:
                    HexGrid3D gScorched = HexGrid3D.Instance;
                    HexTile3D scorchedTile = gScorched != null ? gScorched.GetTile(new HexCoordinates(0, 0)) : null;
                    SetTutorialHighlightedTile(scorchedTile);
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton("Fireball");
                    banner?.ShowGuidance(
                        "[STAGE 2 - STEP 2/4] IGNITE MAGMA FROM SCORCHED EARTH",
                        "The center tile at (0, 0) is already smoldering Scorched Earth (Tier 1). Hitting it with Fireball triggers a thermal breach directly into Molten Magma (Tier 2)!",
                        "Click [Fireball] on the action bar, then target the Scorched tile at (0, 0) to ignite Magma!",
                        new Color(1f, 0.45f, 0.15f)
                    );
                    break;

                case TutorialStep.Stage2_CastFireballMagma:
                    SetStep(TutorialStep.Stage2_SelectBasaltVanguard);
                    break;

                case TutorialStep.Stage2_SelectBasaltVanguard:
                    if (BasaltVanguardPlayer != null) SetTutorialHighlightedTile(BasaltVanguardPlayer.CurrentTile);
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton(null);
                    banner?.ShowGuidance(
                        "[STAGE 2 - STEP 3/4] SELECT BASALT VANGUARD",
                        "Molten Magma (Tier 2) is active! Any unit pushed into Magma suffers 3 Burn damage. Time for Basalt Vanguard to strike!",
                        "Click Basalt Vanguard at (0, -2)!",
                        new Color(0.85f, 0.65f, 0.35f)
                    );
                    break;

                case TutorialStep.Stage2_PushEnemyIntoHazard:
                    HexGrid3D gShove = HexGrid3D.Instance;
                    HexTile3D shielderTile = gShove != null ? gShove.GetTile(new HexCoordinates(0, -1)) : null;
                    SetTutorialHighlightedTile(shielderTile);
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton("Push");
                    banner?.ShowGuidance(
                        "[STAGE 2 - STEP 4/4] KINETIC SHOVE: HAZARD SLAM",
                        "Basalt Vanguard's Golem Slam displaces targets 1 hex. Shove that Scout Defender straight forward into the molten Magma!",
                        "Click [Push / Golem Slam], then shove the Scout Defender at (0, -1) into the Magma pit!",
                        new Color(0.85f, 0.65f, 0.35f)
                    );
                    break;

                case TutorialStep.Stage2_VanquishScouts:
                    ClearTutorialHighlightedTile();
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton(null);
                    banner?.ShowGuidance(
                        "[COMBAT EXECUTION] CLEAR THE FRONTIER",
                        "The scout was shoved into the lava and took heavy burn damage! Now finish off the remaining scouts with basic attacks!",
                        "Attack and eliminate the remaining Holy Synod scouts!",
                        new Color(0.35f, 0.95f, 0.45f)
                    );
                    break;

                // ================= HUB 2 GUIDANCE ================= //
                case TutorialStep.Hub_TitanCrisisDialogue:
                    ClearTutorialHighlightedTile();
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton(null);
                    banner?.HideGuidance();
                    break;

                case TutorialStep.Hub_SelectStage3MissionCard:
                    ClearTutorialHighlightedTile();
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton(null);
                    banner?.ShowGuidance(
                        "[EXPEDITION GATEWAY] SELECT STAGE 3 DEFENSE",
                        "The Holy Synod's heavy crusade is marching on our primary gate! Select Card #1 to mobilize the defense!",
                        "Select Card #1 and click [EMBARK EXPEDITION] to confront the heavy crusade!",
                        new Color(1f, 0.85f, 0.35f)
                    );
                    break;

                // ================= STAGE 3 GUIDANCE (ABYSSAL RIFT & TITANS) ================= //
                case TutorialStep.Stage3_TearOpenRift:
                    HexGrid3D gRift = HexGrid3D.Instance;
                    HexTile3D riftTarget = gRift != null ? gRift.GetTile(new HexCoordinates(0, -2)) : null;
                    SetTutorialHighlightedTile(riftTarget);
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton("TearRift");
                    banner?.ShowGuidance(
                        "[STAGE 3 - STEP 1/5] TEAR OPEN ABYSSAL RIFT",
                        "To deploy our forces and channel primordial energy, I must first anchor the Abyssal Rift on this battlefield.",
                        "Click [Tear Open Abyssal Rift], then choose the purple landing hex at (0, -2)!",
                        new Color(0.75f, 0.35f, 1.0f)
                    );
                    break;

                case TutorialStep.Stage3_DeployDemonLord:
                    HexGrid3D gDl = HexGrid3D.Instance;
                    HexTile3D dlTarget = gDl != null ? gDl.GetTile(new HexCoordinates(0, -1)) : null;
                    SetTutorialHighlightedTile(dlTarget);
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton("DeployCommander");
                    banner?.ShowGuidance(
                        "[STAGE 3 - STEP 2/5] DEPLOY DEMON LORD",
                        "The rift is open! Now deploy my Demon Lord through the gateway onto the frontline.",
                        "Click [Deploy Demon Lord], then place him at (0, -1) in front of the Rift!",
                        new Color(0.95f, 0.45f, 0.35f)
                    );
                    break;

                case TutorialStep.Stage3_SiphonElementalCore:
                    HexGrid3D gSiph = HexGrid3D.Instance;
                    HexTile3D siphTarget = gSiph != null ? gSiph.GetTile(new HexCoordinates(0, 0)) : null;
                    SetTutorialHighlightedTile(siphTarget);
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton("HarvestCore");
                    banner?.ShowGuidance(
                        "[STAGE 3 - STEP 3/5] SIPHON ELEMENTAL CORE",
                        "Summoning a Titan requires an Elemental Core. As Demon Lord, I can siphon elemental energy from the land!",
                        "Select Demon Lord, click [Harvest Core / Siphon], and siphon from the adjacent tile!",
                        new Color(0.35f, 0.85f, 1.0f)
                    );
                    break;

                case TutorialStep.Stage3_SummonEarthGolemTitan:
                    HexGrid3D gBv = HexGrid3D.Instance;
                    HexTile3D titanTarget = gBv != null ? gBv.GetTile(new HexCoordinates(1, -2)) : null;
                    SetTutorialHighlightedTile(titanTarget);
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton("DeployTitan");
                    banner?.ShowGuidance(
                        "[STAGE 3 - STEP 4/5] SUMMON EARTH GOLEM TITAN",
                        "An Elemental Core is forged! Now channel the Abyssal Rift to summon our ancient Titan: The Earth Golem!",
                        "Click [Summon Earth Golem], then deploy the colossal Titan at (1, -2)!",
                        new Color(0.85f, 0.65f, 0.35f)
                    );
                    break;

                case TutorialStep.Stage3_EarthGolemCataclysm:
                    HexGrid3D g3 = HexGrid3D.Instance;
                    HexTile3D wardenTile = g3 != null ? g3.GetTile(new HexCoordinates(0, 1)) : null;
                    SetTutorialHighlightedTile(wardenTile);
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton("Strike");
                    banner?.ShowGuidance(
                        "[STAGE 3 - STEP 5/5] UNLEASH TITAN MIGHT",
                        "The Earth Golem has materialized! Its massive boulder fists will shatter their divine barriers into dust!",
                        "Select Earth Golem, click [Boulder Smash / Attack], and crush the Divine Warden!",
                        new Color(0.85f, 0.65f, 0.35f)
                    );
                    break;

                case TutorialStep.Stage3_ObliterateCrusaders:
                    ClearTutorialHighlightedTile();
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton(null);
                    banner?.ShowGuidance(
                        "[TOTAL VICTORY] OBLITERATE THE CRUSADE",
                        "Their divine barrier is shattered! Wipe out the remaining crusade invaders to claim our first Primordial Core!",
                        "Eliminate all remaining holy invaders to claim victory!",
                        new Color(0.35f, 0.95f, 0.45f)
                    );
                    break;

                case TutorialStep.Completed:
                    ClearTutorialHighlightedTile();
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton(null);
                    banner?.HideGuidance();
                    break;
            }
        }

        public void SetTutorialHighlightedTile(HexTile3D tile)
        {
            if (currentHighlightedTile != null && currentHighlightedTile != tile)
            {
                currentHighlightedTile.SetTutorialHighlighted(false);
            }
            currentHighlightedTile = tile;
            if (currentHighlightedTile != null)
            {
                currentHighlightedTile.SetTutorialHighlighted(true);
            }
        }

        public void ClearTutorialHighlightedTile()
        {
            if (currentHighlightedTile != null)
            {
                currentHighlightedTile.SetTutorialHighlighted(false);
                currentHighlightedTile = null;
            }
        }

        #endregion

        #region Stage 1: The Holy Crusade (Omniterra Prologue Raid)

        public void StartStage1HolyCrusade()
        {
            CurrentStage = TutorialStage.Stage1_HolyCrusade;
            CurrentStep = TutorialStep.Stage1_SelectPaladin;
            Debug.Log("<color=#FFD54F><b>[Tutorial Scenario]</b></color> Starting Stage 1: The Holy Crusade...");

            // 1. Enter in-game combat HUD view
            if (TitleMenuCanvasUI.Instance != null)
            {
                TitleMenuCanvasUI.Instance.EnterHexBattlefield();
            }

            // 2. Clear any lingering units from scene
            PurgeAllBattlefieldUnits();

            // 3. Clear any active Abyssal Rift Conduit (no rift in Stage 1!)
            if (AbyssalRiftConduit3D.Instance != null)
            {
                Destroy(AbyssalRiftConduit3D.Instance.gameObject);
            }

            // 4. Setup Stage 1 Battlefield
            SetupStage1Battlefield();

            // 5. Spawn Player Units (Paladin & Holy Shielder)
            HexGrid3D grid = HexGrid3D.Instance;
            if (grid != null)
            {
                HexTile3D paladinTile = grid.GetTile(new HexCoordinates(0, -1));
                Sprite paladinSprite = TacticalUnitSpawner.LoadBattlerSprite("paladin.png");
                PaladinUnit = TacticalUnitSpawner.SpawnUnitStandee(
                    "Paladin (Chosen Hero)",
                    UnitFaction.Player,
                    paladinSprite,
                    paladinTile,
                    14, 2,
                    UnitArchetype.Commander,
                    ElementalAffinity.None,
                    3,
                    0.80f
                );

                HexTile3D shielderTile = grid.GetTile(new HexCoordinates(-1, -1));
                Sprite shielderSprite = TacticalUnitSpawner.LoadBattlerSprite("holyshielder.png");
                HolyShielderUnit = TacticalUnitSpawner.SpawnUnitStandee(
                    "Holy Shielder",
                    UnitFaction.Player,
                    shielderSprite,
                    shielderTile,
                    16, 2,
                    UnitArchetype.Minion,
                    ElementalAffinity.None,
                    2,
                    0.75f
                );

                // 6. Spawn Enemy Units (Demon Lord Boss & Demon Slimes)
                HexTile3D demonLordTile = grid.GetTile(new HexCoordinates(0, 1));
                Sprite demonLordSprite = TacticalUnitSpawner.LoadBattlerSprite("DemonLord.png");
                DemonLordBoss = TacticalUnitSpawner.SpawnUnitStandee(
                    "Demon Lord",
                    UnitFaction.Enemy,
                    demonLordSprite,
                    demonLordTile,
                    8, 2,
                    UnitArchetype.Commander,
                    ElementalAffinity.Fire,
                    2,
                    0.90f
                );

                HexTile3D slime1Tile = grid.GetTile(new HexCoordinates(-1, 2));
                Sprite slimeSprite = TacticalUnitSpawner.LoadBattlerSprite("Demon Slime.png");
                TacticalUnitSpawner.SpawnUnitStandee(
                    "Demon Slime A",
                    UnitFaction.Enemy,
                    slimeSprite,
                    slime1Tile,
                    4, 2,
                    UnitArchetype.Minion,
                    ElementalAffinity.None,
                    1,
                    0.65f
                );

                HexTile3D slime2Tile = grid.GetTile(new HexCoordinates(1, 1));
                TacticalUnitSpawner.SpawnUnitStandee(
                    "Demon Slime B",
                    UnitFaction.Enemy,
                    slimeSprite,
                    slime2Tile,
                    4, 2,
                    UnitArchetype.Minion,
                    ElementalAffinity.None,
                    1,
                    0.65f
                );
            }

            // 7. Reset Turn State
            if (TurnManager3D.Instance != null)
            {
                TurnManager3D.Instance.ResetBattleState();
            }

            // 8. Reset Camera to framing view
            if (TacticalCameraController.Instance != null)
            {
                TacticalCameraController.Instance.ResetToTacticalView();
            }

            // 9. Display Mission Objective Banner
            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.ShowBanner(
                    "⚜️ THE RADIANT CRUSADE: SLAY THE DEMON LORD ⚜️",
                    "Paladin of Radiance: Advance across the chamber and strike down the Demon Lord!",
                    3.0f,
                    new Color(1f, 0.88f, 0.35f)
                );
            }

            // 10. Trigger in-battle banter dialogue, then initialize step 1 guidance
            StartCoroutine(PlayDialogueDelayed(StorySequenceId.HolyRaidIntro, 0.8f, () =>
            {
                SetStep(TutorialStep.Stage1_SelectPaladin);
            }));
        }

        private void SetupStage1Battlefield()
        {
            HexGrid3D grid = HexGrid3D.Instance;
            if (grid == null) return;

            GeneratedBattlefieldData data = new GeneratedBattlefieldData
            {
                Radius = 3,
                Seed = 1001
            };

            int radius = 3;
            for (int q = -radius; q <= radius; q++)
            {
                int r1 = Mathf.Max(-radius, -q - radius);
                int r2 = Mathf.Min(radius, -q + radius);
                for (int r = r1; r <= r2; r++)
                {
                    HexCoordinates coord = new HexCoordinates(q, r);
                    GeneratedTileSpec spec = new GeneratedTileSpec
                    {
                        Coordinates = coord,
                        ElevationTier = 0,
                        State = TileState.Barren,
                        TierLevel = 1
                    };

                    // Demon Lord Throne at (0, 1)
                    if (q == 0 && r == 1)
                    {
                        spec.ElevationTier = 1;
                        spec.State = TileState.Barren;
                    }
                    else if ((q == -1 && r == 2) || (q == 1 && r == 1) || (q == 0 && r == 2))
                    {
                        spec.State = TileState.Scorched;
                    }

                    data.Tiles[coord] = spec;
                }
            }

            grid.BuildFromBattlefieldData(data);
        }

        private void OnStage1Victory()
        {
            Debug.Log("<color=#FFD54F><b>[Tutorial Scenario]</b></color> Stage 1 Holy Crusade WON! Triggering Transmigration Glitch...");
            SoundManager3D.Instance?.PlayVictory();
            ClearTutorialHighlightedTile();
            CombatHudCanvasUI.Instance?.HighlightTutorialButton(null);
            TutorialGuidanceBannerUI.Instance?.HideGuidance();

            if (StoryDialogueUI.Instance != null)
            {
                StoryDialogueUI.Instance.PlayPrebuiltSequence(StorySequenceId.HolyRaidVictoryGlitch, () =>
                {
                    CampaignSaveManager.SetTransmigrated(true);
                    CampaignSaveManager.SaveTutorialProgress(stage1: true, stage2: false);
                    TitleMenuCanvasUI.Instance?.ApplyTitleThemeMode(TitleMenuCanvasUI.TitleThemeMode.DemonLordRebellion);
                    StartHubCitadelAwakening();
                });
            }
            else
            {
                CampaignSaveManager.SetTransmigrated(true);
                CampaignSaveManager.SaveTutorialProgress(stage1: true, stage2: false);
                TitleMenuCanvasUI.Instance?.ApplyTitleThemeMode(TitleMenuCanvasUI.TitleThemeMode.DemonLordRebellion);
                StartHubCitadelAwakening();
            }
        }

        #endregion

        #region Hub 1 Transition: Citadel Awakening & Skirmish Briefing

        public void StartHubCitadelAwakening()
        {
            CurrentStage = TutorialStage.Hub_CitadelAwakening;
            CurrentStep = TutorialStep.Hub_CitadelAwakeningDialogue;
            Debug.Log("<color=#FFD54F><b>[Tutorial Scenario]</b></color> Waking up in Citadel Town Hub as Demon Lord...");

            PurgeAllBattlefieldUnits();
            TutorialGuidanceBannerUI.Instance?.HideGuidance();

            if (TitleMenuCanvasUI.Instance != null)
            {
                TitleMenuCanvasUI.Instance.ShowTownHub();
            }

            StartCoroutine(PlayDialogueDelayed(StorySequenceId.HubAwakeningDemonLord, 0.6f, () =>
            {
                OnHubAwakeningDialogueFinished();
            }));
        }

        private void OnHubAwakeningDialogueFinished()
        {
            SetStep(TutorialStep.Hub_ClickAbyssalRift);
            Debug.Log("<color=#FFD54F><b>[Tutorial Scenario]</b></color> Basalt Vanguard awakened Demon Lord! Guided to click Abyssal Gateway...");

            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.ShowBanner(
                    "🌀 DEFEND THE ASHEN FRONTIER 🌀",
                    "Radiant Synod scouts have crossed into our territory! Click the [Abyssal Rift Gateway] in the Citadel center!",
                    4.5f,
                    new Color(1f, 0.85f, 0.35f)
                );
            }
        }

        #endregion

        #region Stage 2: Frontier Hazards (Elemental Alchemy & Kinetic Shove - NO TITANS!)

        public void StartStage2FrontierHazards()
        {
            CurrentStage = TutorialStage.Stage2_FrontierHazards;
            CurrentStep = TutorialStep.Stage2_SelectDemonLord;
            Debug.Log("<color=#FF7043><b>[Tutorial Scenario]</b></color> Starting Stage 2: Frontier Hazards (No Titans)...");

            if (TitleMenuCanvasUI.Instance != null)
            {
                TitleMenuCanvasUI.Instance.EnterHexBattlefield();
            }

            PurgeAllBattlefieldUnits();
            Combat.AbyssalRiftConduit3D.CloseActiveRift();

            SetupStage2Battlefield();

            HexGrid3D grid = HexGrid3D.Instance;
            if (grid != null)
            {
                // Spawn Demon Lord (Commander caster) at (1, -2)
                HexTile3D dlTile = grid.GetTile(new HexCoordinates(1, -2));
                Sprite dlSprite = TacticalUnitSpawner.LoadBattlerSprite("DemonLord.png");
                DemonLordPlayer = TacticalUnitSpawner.SpawnUnitStandee(
                    "Demon Lord",
                    UnitFaction.Player,
                    dlSprite,
                    dlTile,
                    18, 2,
                    UnitArchetype.Commander,
                    ElementalAffinity.Fire,
                    3,
                    0.85f
                );

                // Spawn Basalt Vanguard (Infantry Tank general) at (0, -2)
                HexTile3D bvTile = grid.GetTile(new HexCoordinates(0, -2));
                Sprite basaltSprite = TacticalUnitSpawner.LoadBattlerSprite("BasaltVanguard.png");
                BasaltVanguardPlayer = TacticalUnitSpawner.SpawnUnitStandee(
                    "Basalt Vanguard",
                    UnitFaction.Player,
                    basaltSprite,
                    bvTile,
                    16, 2,
                    UnitArchetype.Minion, // Infantry tank! Not a Titan in Stage 2!
                    ElementalAffinity.Earth,
                    2,
                    0.80f
                );

                // Spawn Radiant Synod Scouts: Defender at (0, -1) right in front of Basalt Vanguard and Magma pit!
                HexTile3D shielderTile = grid.GetTile(new HexCoordinates(0, -1));
                Sprite shielderSprite = TacticalUnitSpawner.LoadBattlerSprite("holyshielder.png");
                TacticalUnitSpawner.SpawnUnitStandee(
                    "Scout Defender",
                    UnitFaction.Enemy,
                    shielderSprite,
                    shielderTile,
                    8, 2,
                    UnitArchetype.Minion,
                    ElementalAffinity.None,
                    2,
                    0.75f
                );

                HexTile3D archerTile = grid.GetTile(new HexCoordinates(-1, 2));
                Sprite archerSprite = TacticalUnitSpawner.LoadBattlerSprite("holyarcher.png");
                TacticalUnitSpawner.SpawnUnitStandee(
                    "Scout Archer",
                    UnitFaction.Enemy,
                    archerSprite,
                    archerTile,
                    6, 2,
                    UnitArchetype.Minion,
                    ElementalAffinity.None,
                    2,
                    0.70f
                );
            }

            if (TurnManager3D.Instance != null)
            {
                TurnManager3D.Instance.ResetBattleState();
            }

            if (TacticalCameraController.Instance != null)
            {
                TacticalCameraController.Instance.ResetToTacticalView();
            }

            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.ShowBanner(
                    "🌲 STAGE 2: FRONTIER SKIRMISH 🌲",
                    "Ignite Magma (Tier 2) from pre-heated Scorched Earth & Shove enemies with Basalt Vanguard!",
                    3.5f,
                    new Color(0.95f, 0.55f, 0.15f)
                );
            }

            StartCoroutine(PlayDialogueDelayed(StorySequenceId.Stage2FrontierIntro, 0.8f, () =>
            {
                SetStep(TutorialStep.Stage2_SelectDemonLord);
            }));
        }

        // Backwards compatibility alias
        public void StartStage2AbyssalAwakening() => StartStage2FrontierHazards();

        private void SetupStage2Battlefield()
        {
            HexGrid3D grid = HexGrid3D.Instance;
            if (grid == null) return;

            GeneratedBattlefieldData data = new GeneratedBattlefieldData
            {
                Radius = 3,
                Seed = 2002
            };

            int radius = 3;
            for (int q = -radius; q <= radius; q++)
            {
                int r1 = Mathf.Max(-radius, -q - radius);
                int r2 = Mathf.Min(radius, -q + radius);
                for (int r = r1; r <= r2; r++)
                {
                    HexCoordinates coord = new HexCoordinates(q, r);
                    GeneratedTileSpec spec = new GeneratedTileSpec
                    {
                        Coordinates = coord,
                        ElevationTier = 0,
                        State = TileState.Barren,
                        TierLevel = 1
                    };

                    // Smoldering Scorched Earth at (0, 0): hitting with Fireball triggers thermal breach directly into Molten Magma (Tier 2)!
                    if (q == 0 && r == 0)
                    {
                        spec.State = TileState.Scorched;
                        spec.TierLevel = 1;
                    }
                    // Water tiles on left flank (slow or quench)
                    else if ((q == -1 && r == 0) || (q == -2 && r == 0))
                    {
                        spec.State = TileState.Water;
                    }
                    // Stone pillars framing the hazard zone!
                    else if ((q == 1 && r == 0) || (q == -1 && r == 1) || (q == 0 && r == 2))
                    {
                        spec.ElevationTier = 0;
                        spec.IsPillarObstacle = true;
                        spec.State = TileState.StonePillar;
                    }
                    // Scorched tiles
                    else if ((q == 0 && r == 1) || (q == -1 && r == 2))
                    {
                        spec.State = TileState.Scorched;
                    }

                    data.Tiles[coord] = spec;
                }
            }

            grid.BuildFromBattlefieldData(data);
        }

        private void OnStage2Victory()
        {
            Debug.Log("<color=#4CAF50><b>[Tutorial Scenario]</b></color> Stage 2 Frontier Skirmish WON! War horns approaching...");
            SoundManager3D.Instance?.PlayVictory();
            ClearTutorialHighlightedTile();
            CombatHudCanvasUI.Instance?.HighlightTutorialButton(null);

            if (StoryDialogueUI.Instance != null)
            {
                StoryDialogueUI.Instance.PlayPrebuiltSequence(StorySequenceId.Stage2Victory, () =>
                {
                    StartHubTitanCrisis();
                });
            }
            else
            {
                StartHubTitanCrisis();
            }
        }

        #endregion

        #region Hub 2 Transition: Titan Crisis

        public void StartHubTitanCrisis()
        {
            CurrentStage = TutorialStage.Hub_TitanCrisis;
            CurrentStep = TutorialStep.Hub_TitanCrisisDialogue;
            Debug.Log("<color=#FF7043><b>[Tutorial Scenario]</b></color> Entering Hub: The Titan Crisis...");

            PurgeAllBattlefieldUnits();
            TutorialGuidanceBannerUI.Instance?.HideGuidance();

            if (TitleMenuCanvasUI.Instance != null)
            {
                TitleMenuCanvasUI.Instance.ShowTownHub();
            }

            StartCoroutine(PlayDialogueDelayed(StorySequenceId.HubTitanCrisisDialogue, 0.6f, () =>
            {
                OnHubTitanCrisisDialogueFinished();
            }));
        }

        private void OnHubTitanCrisisDialogueFinished()
        {
            SetStep(TutorialStep.Hub_SelectStage3MissionCard);
            Debug.Log("<color=#FFD54F><b>[Tutorial Scenario]</b></color> Ready to summon Titans! Guided to expedition gateway...");

            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.ShowBanner(
                    "🌋 AWAKEN THE ANCIENT TITAN 🌋",
                    "The Holy Crusade's heavy shields cannot be pierced by blades! Enter the Abyssal Gateway to summon the Earth Golem!",
                    4.5f,
                    new Color(0.85f, 0.65f, 0.35f)
                );
            }
        }

        #endregion

        #region Stage 3: Abyssal Rift & Titan Awakening (Rift, Core Siphon, Earth Golem)

        public void StartStage3AbyssalTitans()
        {
            CurrentStage = TutorialStage.Stage3_AbyssalTitans;
            CurrentStep = TutorialStep.Stage3_TearOpenRift;
            Debug.Log("<color=#FF7043><b>[Tutorial Scenario]</b></color> Starting Stage 3: Abyssal Rift & Titan Awakening...");

            if (TitleMenuCanvasUI.Instance != null)
            {
                TitleMenuCanvasUI.Instance.EnterHexBattlefield();
            }

            PurgeAllBattlefieldUnits();
            Combat.AbyssalRiftConduit3D.CloseActiveRift();

            SetupStage3Battlefield();

            // Spawn Demon Lord in Reserve
            Sprite dlSprite = TacticalUnitSpawner.LoadBattlerSprite("DemonLord.png");
            DemonLordPlayer = TacticalUnitSpawner.SpawnUnitStandee(
                "Demon Lord",
                UnitFaction.Player,
                dlSprite,
                null, // In reserve behind the rift!
                18, 2,
                UnitArchetype.Commander,
                ElementalAffinity.Fire,
                3,
                0.85f
            );
            if (DemonLordPlayer != null)
            {
                DemonLordPlayer.gameObject.SetActive(false);
            }

            // Spawn Earth Golem Titan in Reserve
            Sprite basaltSprite = TacticalUnitSpawner.LoadBattlerSprite("BasaltVanguard.png");
            EarthGolemPlayer = TacticalUnitSpawner.SpawnUnitStandee(
                "Earth Golem",
                UnitFaction.Player,
                basaltSprite,
                null, // In reserve!
                24, 2,
                UnitArchetype.Titan,
                ElementalAffinity.Earth,
                3,
                0.90f
            );
            if (EarthGolemPlayer != null)
            {
                EarthGolemPlayer.gameObject.SetActive(false);
            }

            // Spawn Radiant Synod Heavy Crusade Vanguard
            HexGrid3D grid = HexGrid3D.Instance;
            if (grid != null)
            {
                HexTile3D wardenTile = grid.GetTile(new HexCoordinates(0, 1));
                Sprite wardenSprite = TacticalUnitSpawner.LoadBattlerSprite("holyshielder.png");
                TacticalUnitSpawner.SpawnUnitStandee(
                    "Divine Warden",
                    UnitFaction.Enemy,
                    wardenSprite,
                    wardenTile,
                    12, 2,
                    UnitArchetype.Minion,
                    ElementalAffinity.None,
                    2,
                    0.80f
                );

                HexTile3D knightTile = grid.GetTile(new HexCoordinates(1, 1));
                Sprite knightSprite = TacticalUnitSpawner.LoadBattlerSprite("paladin.png");
                TacticalUnitSpawner.SpawnUnitStandee(
                    "Crusader Knight",
                    UnitFaction.Enemy,
                    knightSprite,
                    knightTile,
                    10, 2,
                    UnitArchetype.Minion,
                    ElementalAffinity.None,
                    2,
                    0.75f
                );

                HexTile3D inqTile = grid.GetTile(new HexCoordinates(-1, 2));
                Sprite inqSprite = TacticalUnitSpawner.LoadBattlerSprite("holysaintess.png");
                TacticalUnitSpawner.SpawnUnitStandee(
                    "Holy Inquisitor",
                    UnitFaction.Enemy,
                    inqSprite,
                    inqTile,
                    8, 2,
                    UnitArchetype.Minion,
                    ElementalAffinity.None,
                    2,
                    0.70f
                );
            }

            if (TurnManager3D.Instance != null)
            {
                TurnManager3D.Instance.ResetBattleState();
            }

            if (TacticalCameraController.Instance != null)
            {
                TacticalCameraController.Instance.ResetToTacticalView();
            }

            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.ShowBanner(
                    "👑 STAGE 3: THE TITAN AWAKENING 👑",
                    "Tear open the Abyssal Rift, siphon an Elemental Core, and summon the Earth Golem!",
                    3.5f,
                    new Color(0.75f, 0.35f, 1.0f)
                );
            }

            StartCoroutine(PlayDialogueDelayed(StorySequenceId.Stage3TitanIntro, 0.8f, () =>
            {
                SetStep(TutorialStep.Stage3_TearOpenRift);
            }));
        }

        private void SetupStage3Battlefield()
        {
            HexGrid3D grid = HexGrid3D.Instance;
            if (grid == null) return;

            GeneratedBattlefieldData data = new GeneratedBattlefieldData
            {
                Radius = 3,
                Seed = 3003
            };

            int radius = 3;
            for (int q = -radius; q <= radius; q++)
            {
                int r1 = Mathf.Max(-radius, -q - radius);
                int r2 = Mathf.Min(radius, -q + radius);
                for (int r = r1; r <= r2; r++)
                {
                    HexCoordinates coord = new HexCoordinates(q, r);
                    GeneratedTileSpec spec = new GeneratedTileSpec
                    {
                        Coordinates = coord,
                        ElevationTier = 0,
                        State = TileState.Barren,
                        TierLevel = 1
                    };

                    // Scorched / Magma tiles ready to be siphoned for cores!
                    if ((q == 0 && r == 0) || (q == 1 && r == -1))
                    {
                        spec.State = TileState.Scorched;
                    }
                    // Stone pillars for boulder impact
                    else if ((q == 1 && r == 0) || (q == -1 && r == 1))
                    {
                        spec.ElevationTier = 0;
                        spec.IsPillarObstacle = true;
                        spec.State = TileState.StonePillar;
                    }
                    else if (q == 0 && r == 2)
                    {
                        spec.State = TileState.Scorched;
                    }

                    data.Tiles[coord] = spec;
                }
            }

            grid.BuildFromBattlefieldData(data);
        }

        private void OnStage3Victory()
        {
            Debug.Log("<color=#4CAF50><b>[Tutorial Scenario]</b></color> Stage 3 Titan Awakening WON! Claiming 1st Primordial Titan Core...");
            SoundManager3D.Instance?.PlayVictory();
            ClearTutorialHighlightedTile();
            CombatHudCanvasUI.Instance?.HighlightTutorialButton(null);

            if (StoryDialogueUI.Instance != null)
            {
                StoryDialogueUI.Instance.PlayPrebuiltSequence(StorySequenceId.Stage3Victory, () =>
                {
                    CompleteTutorialAndEnterCitadel();
                });
            }
            else
            {
                CompleteTutorialAndEnterCitadel();
            }
        }

        private void CompleteTutorialAndEnterCitadel()
        {
            CurrentStage = TutorialStage.None;
            SetStep(TutorialStep.Completed);

            CampaignSaveManager.SaveTutorialProgress(stage1: true, stage2: true, starterTitan: true);

            if (TownHubManager.Instance != null)
            {
                TownHubManager.Instance.AddManaCrystals(100);
                TownHubManager.Instance.AddSoulEmbers(60);
                TownHubManager.Instance.AddFreedOutcasts(12);
            }

            PurgeAllBattlefieldUnits();
            TacticalUnitSpawner.ResetPlayerReserveUnits();

            if (TitleMenuCanvasUI.Instance != null)
            {
                TitleMenuCanvasUI.Instance.ShowTownHub();
            }

            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.ShowBanner(
                    "🏆 CITADEL SANCTUARY UNLOCKED! 🏆",
                    "Primordial Titan Core acquired! The Ashen Verge expedition gateway is fully operational!",
                    4.0f,
                    new Color(0.35f, 0.95f, 0.45f)
                );
            }
        }

        #endregion

        #region Victory / Defeat Interception

        public void OnTutorialVictory()
        {
            if (CurrentStage == TutorialStage.Stage1_HolyCrusade)
            {
                OnStage1Victory();
            }
            else if (CurrentStage == TutorialStage.Stage2_FrontierHazards)
            {
                OnStage2Victory();
            }
            else if (CurrentStage == TutorialStage.Stage3_AbyssalTitans)
            {
                OnStage3Victory();
            }
        }

        public void OnTutorialDefeat()
        {
            Debug.LogWarning("<color=#EF5350><b>[Tutorial Scenario]</b></color> Tutorial squad fell! Restarting current stage...");
            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.ShowBanner(
                    "⚠️ RETRYING TUTORIAL ⚠️",
                    "Regrouping vanguard forces...",
                    2.0f,
                    new Color(0.95f, 0.5f, 0.25f)
                );
            }

            StartCoroutine(RestartCurrentStageDelayed());
        }

        private IEnumerator RestartCurrentStageDelayed()
        {
            yield return new WaitForSeconds(1.5f);
            if (CurrentStage == TutorialStage.Stage1_HolyCrusade)
            {
                StartStage1HolyCrusade();
            }
            else if (CurrentStage == TutorialStage.Stage2_FrontierHazards)
            {
                StartStage2FrontierHazards();
            }
            else if (CurrentStage == TutorialStage.Stage3_AbyssalTitans)
            {
                StartStage3AbyssalTitans();
            }
        }

        #endregion

        #region Helpers

        private IEnumerator PlayDialogueDelayed(StorySequenceId sequenceId, float delay, Action onFinish = null)
        {
            yield return new WaitForSeconds(delay);
            if (StoryDialogueUI.Instance != null)
            {
                StoryDialogueUI.Instance.PlayPrebuiltSequence(sequenceId, onFinish);
            }
            else
            {
                onFinish?.Invoke();
            }
        }

        public static void PurgeAllBattlefieldUnits()
        {
            TacticalUnit3D[] units = FindObjectsByType<TacticalUnit3D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var u in units)
            {
                if (u != null)
                {
                    if (u.CurrentTile != null && u.CurrentTile.CurrentOccupant == u)
                    {
                        u.CurrentTile.CurrentOccupant = null;
                    }
                    if (Application.isPlaying) Destroy(u.gameObject);
                    else DestroyImmediate(u.gameObject);
                }
            }
        }

        #endregion
    }
}
