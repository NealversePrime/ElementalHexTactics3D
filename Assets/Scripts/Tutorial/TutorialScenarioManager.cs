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
        Stage2_AbyssalAwakening
    }

    public enum TutorialStep
    {
        None,
        // Stage 1: The Holy Crusade (Holyland Evolve)
        Stage1_SelectPaladin,
        Stage1_MovePaladin,
        Stage1_HolyStrikeDemonLord,
        Stage1_MoveShielderOrEndTurn,
        Stage1_EnemyTurnReaction,
        Stage1_FinishDemonLord,

        // Stage 2: The Abyssal Awakening (Elemental Hex Tactics)
        Stage2_CastFireballOnGrass,
        Stage2_BasaltVanguardWallSlam,
        Stage2_VanquishHolyInvaders,

        Completed
    }

    /// <summary>
    /// Master controller for the narrative prologue and 2-stage scripted tutorial inversion:
    /// Stage 1: The Holy Crusade (Holyland Evolve) - Paladin & Holy Shielder vs Demon Lord boss.
    /// Features diegetic MC gamer inner monologue, dynamic tile highlights, and fail-safe plot armor.
    /// Stage 2: The Abyssal Awakening (Elemental Hex Tactics) - Demon Lord MC & Basalt Vanguard vs Holy Empire.
    /// Demonstrates Fireball grass ignition into Magma, Basalt Vanguard kinetic wall slams, and Citadel Hub unlock.
    /// </summary>
    public class TutorialScenarioManager : MonoBehaviour
    {
        public static TutorialScenarioManager Instance { get; private set; }

        public TutorialStage CurrentStage { get; private set; } = TutorialStage.None;
        public TutorialStep CurrentStep { get; private set; } = TutorialStep.None;

        public bool IsTutorialActive => CurrentStage != TutorialStage.None;
        public bool IsStage1Active => CurrentStage == TutorialStage.Stage1_HolyCrusade;
        public bool IsStage2Active => CurrentStage == TutorialStage.Stage2_AbyssalAwakening;

        // References to active tutorial units
        public TacticalUnit3D PaladinUnit { get; private set; }
        public TacticalUnit3D HolyShielderUnit { get; private set; }
        public TacticalUnit3D DemonLordBoss { get; private set; }
        public TacticalUnit3D DemonLordPlayer { get; private set; }
        public TacticalUnit3D BasaltVanguardPlayer { get; private set; }

        private HexTile3D currentHighlightedTile = null;

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
                        // If player accidentally deselects Paladin before moving, gently guide them back
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
            else if (CurrentStage == TutorialStage.Stage2_AbyssalAwakening)
            {
                switch (CurrentStep)
                {
                    case TutorialStep.Stage2_CastFireballOnGrass:
                        HexGrid3D grid = HexGrid3D.Instance;
                        HexTile3D centerTile = grid != null ? grid.GetTile(new HexCoordinates(0, 0)) : null;
                        // Transition when center tile becomes Magma or when Demon Lord has acted
                        if ((centerTile != null && centerTile.State == TileState.Magma) || (DemonLordPlayer != null && DemonLordPlayer.HasActedThisTurn))
                        {
                            SetStep(TutorialStep.Stage2_BasaltVanguardWallSlam);
                        }
                        break;

                    case TutorialStep.Stage2_BasaltVanguardWallSlam:
                        if (BasaltVanguardPlayer != null && (BasaltVanguardPlayer.HasActedThisTurn || BasaltVanguardPlayer.HasMovedThisTurn))
                        {
                            SetStep(TutorialStep.Stage2_VanquishHolyInvaders);
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
                case TutorialStep.Stage1_SelectPaladin:
                    if (PaladinUnit != null) SetTutorialHighlightedTile(PaladinUnit.CurrentTile);
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton(null);
                    banner?.ShowGuidance(
                        "[LANGKAH 1/4] PILIH PALADIN",
                        "Oke... pertama-tama aku hanya perlu tekan Paladinku untuk memilihnya...",
                        "Klik Paladin (Chosen Hero) di petak (0, -1)!",
                        new Color(1f, 0.88f, 0.35f)
                    );
                    break;

                case TutorialStep.Stage1_MovePaladin:
                    // When Paladin is selected, reachable tiles turn cyan/blue!
                    HexGrid3D grid1 = HexGrid3D.Instance;
                    HexTile3D targetForwardTile = grid1 != null ? grid1.GetTile(new HexCoordinates(0, 0)) : null;
                    SetTutorialHighlightedTile(targetForwardTile);
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton("Move");
                    banner?.ShowGuidance(
                        "[LANGKAH 2/4] LANGKAH KE PETAK BIRU",
                        "Nah! Otomatis petak biru (cyan) muncul. Sekarang aku hanya perlu klik salah satu petak biru untuk melangkah mendekati Demon Lord...",
                        "Klik petak biru (cyan) di depan untuk melangkah maju!",
                        new Color(0.35f, 0.85f, 1.0f)
                    );
                    break;

                case TutorialStep.Stage1_HolyStrikeDemonLord:
                    if (DemonLordBoss != null) SetTutorialHighlightedTile(DemonLordBoss.CurrentTile);
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton("Strike");
                    banner?.ShowGuidance(
                        "[LANGKAH 3/4] SERANG DENGAN HOLY STRIKE",
                        "Bagus! Demon Lord sudah dalam jangkauan pedangku. Sekarang tekan tombol [Holy Strike] di bawah atau klik langsung ke Demon Lord untuk melancarkan serangan suci!",
                        "Klik tombol [Holy Strike] di action bar, lalu serang Demon Lord!",
                        new Color(1f, 0.45f, 0.25f)
                    );
                    break;

                case TutorialStep.Stage1_MoveShielderOrEndTurn:
                    ClearTutorialHighlightedTile();
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton("EndTurn");
                    banner?.ShowGuidance(
                        "[LANGKAH 4/4] AKHIRI GILIRAN",
                        "Serangan suci mendarat telak! Paladinku sudah selesai bertindak. Sekarang aku bisa gerakkan Holy Shielder atau langsung klik [END TURN] untuk giliran musuh...",
                        "Klik tombol [END TURN] di kanan bawah untuk mengakhiri giliran!",
                        new Color(0.45f, 0.95f, 0.65f)
                    );
                    break;

                case TutorialStep.Stage1_EnemyTurnReaction:
                    ClearTutorialHighlightedTile();
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton(null);
                    banner?.ShowGuidance(
                        "[FASE MUSUH] BERTAHAN DARI SERANGAN",
                        "Awas! Pasukan iblis mulai membalas... Tapi dengan berkat Divine Blessing, pasukanku takkan bisa ditumbangkan!",
                        "Memperhatikan giliran musuh...",
                        new Color(0.95f, 0.35f, 0.25f)
                    );
                    break;

                case TutorialStep.Stage1_FinishDemonLord:
                    if (DemonLordBoss != null) SetTutorialHighlightedTile(DemonLordBoss.CurrentTile);
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton("Strike");
                    banner?.ShowGuidance(
                        "[PUNCAK CRUSADE] HABISI DEMON LORD!",
                        "Haha, serangan mereka geli-geli berkat Divine Blessing! Sekarang giliranku lagi... Habisi Demon Lord dan selesaikan raid ini!",
                        "Serang Demon Lord sekali lagi untuk memenangkan pertempuran!",
                        new Color(1f, 0.88f, 0.35f)
                    );
                    break;

                case TutorialStep.Stage2_CastFireballOnGrass:
                    HexGrid3D g2 = HexGrid3D.Instance;
                    HexTile3D grassTile = g2 != null ? g2.GetTile(new HexCoordinates(0, 0)) : null;
                    SetTutorialHighlightedTile(grassTile);
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton("Fireball");
                    banner?.ShowGuidance(
                        "[REAKSI ELEMEN] BAKAR RUMPUT JADI MAGMA",
                        "Tunggu sebentar... tubuh dan interface-ku berubah jadi Demon Lord?! Baiklah, analisa battlefield dulu... Di depanku ada petak Rumput (Grass). Kalau kutembakkan [Fireball] ke sana, rumputnya akan terbakar jadi Magma!",
                        "Pilih Demon Lord, klik [Fireball], lalu tembakkan ke petak Rumput (Grass) di depan!",
                        new Color(1f, 0.55f, 0.15f)
                    );
                    break;

                case TutorialStep.Stage2_BasaltVanguardWallSlam:
                    HexGrid3D g3 = HexGrid3D.Instance;
                    HexTile3D pillarTile = g3 != null ? g3.GetTile(new HexCoordinates(1, 0)) : null;
                    SetTutorialHighlightedTile(pillarTile);
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton("Push");
                    banner?.ShowGuidance(
                        "[KINETIC PUSH] DORONG MUSUH MENABRAK PILAR",
                        "Rumputnya benar-benar meleleh jadi Magma pijar! Sekarang giliran Basalt Vanguard. Skill dorongannya [Gore] bisa mendorong prajurit musuh menabrak Pilar Batu (Stone Pillar) untuk bonus Wall Slam damage!",
                        "Pilih Basalt Vanguard, klik [Gore / Push], lalu dorong Holy Shielder menabrak Pilar Batu!",
                        new Color(0.85f, 0.65f, 0.35f)
                    );
                    break;

                case TutorialStep.Stage2_VanquishHolyInvaders:
                    ClearTutorialHighlightedTile();
                    CombatHudCanvasUI.Instance?.HighlightTutorialButton(null);
                    banner?.ShowGuidance(
                        "[KEMENANGAN MUTLAK] HABISI PENYERBU",
                        "Wall Slam sukses! Musuh terkena stun dan damage benturan. Sekarang habisi sisa prajurit Holy Empire ini untuk mengamankan sanctuary dan merebut Primordial Titan Core pertamamu!",
                        "Habisi seluruh penyerbu Holy Empire untuk menyelesaikan tutorial!",
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

        #region Stage 1: The Holy Crusade (Holyland Evolve)

        /// <summary>
        /// Starts Stage 1: The player plays as the Chosen Paladin raiding the Demon Lord's abyssal chamber.
        /// </summary>
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
                // Paladin starts at (0, -1), positioned 2 hexes from Demon Lord at (0, 1)
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
                    "⚜️ THE HOLY CRUSADE: SLAY THE DEMON LORD ⚜️",
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
                    else if ((q == -2 && r == 1) || (q == 2 && r == 0))
                    {
                        spec.ElevationTier = 2;
                        spec.IsPillarObstacle = true;
                        spec.State = TileState.StonePillar;
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

            if (StoryDialogueUI.Instance != null)
            {
                StoryDialogueUI.Instance.PlayPrebuiltSequence(StorySequenceId.HolyRaidVictoryGlitch, () =>
                {
                    // Transmigration Incident Complete!
                    CampaignSaveManager.SetTransmigrated(true);
                    CampaignSaveManager.SaveTutorialProgress(stage1: true, stage2: false);

                    // Transition to Stage 2: The Abyssal Awakening!
                    StartStage2AbyssalAwakening();
                });
            }
            else
            {
                CampaignSaveManager.SetTransmigrated(true);
                CampaignSaveManager.SaveTutorialProgress(stage1: true, stage2: false);
                StartStage2AbyssalAwakening();
            }
        }

        #endregion

        #region Stage 2: The Abyssal Awakening (Elemental Hex Tactics)

        /// <summary>
        /// Starts Stage 2: The player wakes up as the Demon Lord!
        /// Learns Terrain Reactions (Fireball ignites grass into Magma),
        /// Kinetic Pushes & Wall Slams with Basalt Vanguard, and repels invading Holy Knights.
        /// </summary>
        public void StartStage2AbyssalAwakening()
        {
            CurrentStage = TutorialStage.Stage2_AbyssalAwakening;
            CurrentStep = TutorialStep.Stage2_CastFireballOnGrass;
            Debug.Log("<color=#FF7043><b>[Tutorial Scenario]</b></color> Starting Stage 2: The Abyssal Awakening...");

            // 1. Enter in-game combat HUD view
            if (TitleMenuCanvasUI.Instance != null)
            {
                TitleMenuCanvasUI.Instance.EnterHexBattlefield();
            }

            // 2. Clear any lingering units
            PurgeAllBattlefieldUnits();

            // 3. Clear any active rift
            if (AbyssalRiftConduit3D.Instance != null)
            {
                Destroy(AbyssalRiftConduit3D.Instance.gameObject);
            }

            // 4. Setup Stage 2 Battlefield with rich interactive terrain
            SetupStage2Battlefield();

            // 5. Spawn Player Units (Demon Lord & Basalt Vanguard)
            HexGrid3D grid = HexGrid3D.Instance;
            if (grid != null)
            {
                HexTile3D dlTile = grid.GetTile(new HexCoordinates(0, -2));
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
                if (DemonLordPlayer != null)
                {
                    DemonLordPlayer.AddElementalCore(1);
                }

                HexTile3D basaltTile = grid.GetTile(new HexCoordinates(1, -2));
                Sprite basaltSprite = TacticalUnitSpawner.LoadBattlerSprite("BasaltVanguard.png");
                BasaltVanguardPlayer = TacticalUnitSpawner.SpawnUnitStandee(
                    "Basalt Vanguard",
                    UnitFaction.Player,
                    basaltSprite,
                    basaltTile,
                    16, 2,
                    UnitArchetype.Minion,
                    ElementalAffinity.Earth,
                    3,
                    0.85f
                );

                // 6. Spawn Enemy Invaders (Holy Empire forces)
                HexTile3D shielderTile = grid.GetTile(new HexCoordinates(0, 1));
                Sprite shielderSprite = TacticalUnitSpawner.LoadBattlerSprite("holyshielder.png");
                TacticalUnitSpawner.SpawnUnitStandee(
                    "Holy Shielder",
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
                    "Holy Archer",
                    UnitFaction.Enemy,
                    archerSprite,
                    archerTile,
                    6, 2,
                    UnitArchetype.Minion,
                    ElementalAffinity.None,
                    2,
                    0.70f
                );

                HexTile3D saintessTile = grid.GetTile(new HexCoordinates(1, 1));
                Sprite saintessSprite = TacticalUnitSpawner.LoadBattlerSprite("holysaintess.png");
                TacticalUnitSpawner.SpawnUnitStandee(
                    "Holy Saintess",
                    UnitFaction.Enemy,
                    saintessSprite,
                    saintessTile,
                    6, 2,
                    UnitArchetype.Minion,
                    ElementalAffinity.None,
                    2,
                    0.70f
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
                    "👑 TUTORIAL 2: THE ABYSSAL AWAKENING 👑",
                    "🔥 Ignite Grass into Magma with Fireball! 💥 Shove Holy invaders into Stone Pillars!",
                    3.5f,
                    new Color(1f, 0.45f, 0.25f)
                );
            }

            // 10. Trigger in-battle dialogue sequence, then initialize step guidance
            StartCoroutine(PlayDialogueDelayed(StorySequenceId.DemonAwakeningIntro, 0.8f, () =>
            {
                SetStep(TutorialStep.Stage2_CastFireballOnGrass);
            }));
        }

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

                    // Grass tiles in center (ignite with Fireball -> Magma!)
                    if ((q == 0 && r == 0) || (q == 1 && r == -1) || (q == 0 && r == -1))
                    {
                        spec.State = TileState.Grass;
                    }
                    // Water tiles on left flank (extinguish or slow)
                    else if ((q == -1 && r == 0) || (q == -2 && r == 0))
                    {
                        spec.State = TileState.Water;
                    }
                    // Stone pillars for wall slam collisions!
                    else if ((q == 1 && r == 0) || (q == -1 && r == 1))
                    {
                        spec.ElevationTier = 2;
                        spec.IsPillarObstacle = true;
                        spec.State = TileState.StonePillar;
                    }
                    // Scorched tiles near enemy entrance
                    else if ((q == 0 && r == 2) || (q == -1 && r == 2))
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
            Debug.Log("<color=#4CAF50><b>[Tutorial Scenario]</b></color> Stage 2 Abyssal Awakening WON! Claiming 1st Primordial Titan Core...");
            SoundManager3D.Instance?.PlayVictory();
            ClearTutorialHighlightedTile();
            CombatHudCanvasUI.Instance?.HighlightTutorialButton(null);

            if (StoryDialogueUI.Instance != null)
            {
                StoryDialogueUI.Instance.PlayPrebuiltSequence(StorySequenceId.Tutorial2Victory, () =>
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

            // 1. Save tutorial completion flags
            CampaignSaveManager.SaveTutorialProgress(stage1: true, stage2: true, starterTitan: true);

            // 2. Award domain resources to TownHubManager
            if (TownHubManager.Instance != null)
            {
                TownHubManager.Instance.AddManaCrystals(100);
                TownHubManager.Instance.AddSoulEmbers(60);
                TownHubManager.Instance.AddFreedOutcasts(12);
            }

            // 3. Clear battlefield units and return to Citadel Town Hub
            PurgeAllBattlefieldUnits();
            TacticalUnitSpawner.ResetPlayerReserveUnits();

            if (TitleMenuCanvasUI.Instance != null)
            {
                TitleMenuCanvasUI.Instance.ShowTownHub();
            }

            // 4. Welcome banner
            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.ShowBanner(
                    "🏆 CITADEL SANCTUARY UNLOCKED! 🏆",
                    "Primordial Titan Core acquired! Visit the Ancient Deity Shrine to hatch your first Titan!",
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
            else if (CurrentStage == TutorialStage.Stage2_AbyssalAwakening)
            {
                OnStage2Victory();
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
            else if (CurrentStage == TutorialStage.Stage2_AbyssalAwakening)
            {
                StartStage2AbyssalAwakening();
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
