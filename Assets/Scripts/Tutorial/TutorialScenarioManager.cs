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

    /// <summary>
    /// Master controller for the narrative prologue and 2-stage tutorial inversion:
    /// Stage 1: The Holy Crusade (Holyland Evolve) - Paladin & Holy Shielder vs Demon Lord boss.
    /// Transmigration Glitch cutscene upon victory.
    /// Stage 2: The Abyssal Awakening (Elemental Hex Tactics) - Demon Lord MC & Basalt Vanguard vs Holy Empire invaders.
    /// Awards 1st Primordial Titan Core and unlocks the Citadel Hub.
    /// </summary>
    public class TutorialScenarioManager : MonoBehaviour
    {
        public static TutorialScenarioManager Instance { get; private set; }

        public TutorialStage CurrentStage { get; private set; } = TutorialStage.None;
        public bool IsTutorialActive => CurrentStage != TutorialStage.None;
        public bool IsStage1Active => CurrentStage == TutorialStage.Stage1_HolyCrusade;
        public bool IsStage2Active => CurrentStage == TutorialStage.Stage2_AbyssalAwakening;

        // References to active tutorial units
        public TacticalUnit3D PaladinUnit { get; private set; }
        public TacticalUnit3D HolyShielderUnit { get; private set; }
        public TacticalUnit3D DemonLordBoss { get; private set; }
        public TacticalUnit3D DemonLordPlayer { get; private set; }
        public TacticalUnit3D BasaltVanguardPlayer { get; private set; }

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

        #region Stage 1: The Holy Crusade (Holyland Evolve)

        /// <summary>
        /// Starts Stage 1: The player plays as the Chosen Paladin raiding the Demon Lord's abyssal chamber.
        /// </summary>
        public void StartStage1HolyCrusade()
        {
            CurrentStage = TutorialStage.Stage1_HolyCrusade;
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
                HexTile3D paladinTile = grid.GetTile(new HexCoordinates(0, -2));
                Sprite paladinSprite = TacticalUnitSpawner.LoadBattlerSprite("paladin.png");
                PaladinUnit = TacticalUnitSpawner.SpawnUnitStandee(
                    "Paladin (Chosen Hero)",
                    UnitFaction.Player,
                    paladinSprite,
                    paladinTile,
                    12, 2,
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
                    14, 2,
                    UnitArchetype.Minion,
                    ElementalAffinity.None,
                    2,
                    0.75f
                );

                // 6. Spawn Enemy Units (Demon Lord Boss & Demon Slimes)
                HexTile3D demonLordTile = grid.GetTile(new HexCoordinates(0, 2));
                Sprite demonLordSprite = TacticalUnitSpawner.LoadBattlerSprite("DemonLord.png");
                DemonLordBoss = TacticalUnitSpawner.SpawnUnitStandee(
                    "Demon Lord",
                    UnitFaction.Enemy,
                    demonLordSprite,
                    demonLordTile,
                    10, 2,
                    UnitArchetype.Commander,
                    ElementalAffinity.Fire,
                    3,
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

            // 9. Auto-select Paladin so reachable tiles are immediately highlighted
            if (PaladinUnit != null && HexGridInteraction3D.Instance != null)
            {
                HexGridInteraction3D.Instance.SelectUnit(PaladinUnit);
            }

            // 10. Display Mission Objective Banner
            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.ShowBanner(
                    "⚜️ THE HOLY CRUSADE: SLAY THE DEMON LORD ⚜️",
                    "Paladin of Radiance: Advance across the chamber and strike down the Demon Lord!",
                    3.2f,
                    new Color(1f, 0.88f, 0.35f)
                );
            }

            // 11. Trigger in-battle banter dialogue after a short delay
            StartCoroutine(PlayDialogueDelayed(StorySequenceId.HolyRaidIntro, 0.8f));
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

                    // Demon Lord Throne at (0, 2)
                    if (q == 0 && r == 2)
                    {
                        spec.ElevationTier = 1;
                        spec.State = TileState.Barren;
                    }
                    else if ((q == -1 && r == 2) || (q == 1 && r == 1) || (q == 0 && r == 1))
                    {
                        spec.State = TileState.Scorched;
                    }
                    else if ((q == -2 && r == 1) || (q == 1 && r == 0))
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
                    16, 2,
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
                    14, 2,
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

            // 9. Auto-select Demon Lord
            if (DemonLordPlayer != null && HexGridInteraction3D.Instance != null)
            {
                HexGridInteraction3D.Instance.SelectUnit(DemonLordPlayer);
            }

            // 10. Display Mission Objective Banner
            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.ShowBanner(
                    "👑 TUTORIAL 2: THE ABYSSAL AWAKENING 👑",
                    "🔥 Ignite Grass into Magma with Fireball! 💥 Shove Holy invaders into Stone Pillars!",
                    3.5f,
                    new Color(1f, 0.45f, 0.25f)
                );
            }

            // 11. Trigger in-battle dialogue sequence
            StartCoroutine(PlayDialogueDelayed(StorySequenceId.DemonAwakeningIntro, 0.8f));
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
                    "⚠️ DEFEATED ⚠️",
                    "Your vanguard fell! Re-deploying holy forces...",
                    2.0f,
                    new Color(0.95f, 0.3f, 0.25f)
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

        private IEnumerator PlayDialogueDelayed(StorySequenceId sequenceId, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (StoryDialogueUI.Instance != null)
            {
                StoryDialogueUI.Instance.PlayPrebuiltSequence(sequenceId);
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
