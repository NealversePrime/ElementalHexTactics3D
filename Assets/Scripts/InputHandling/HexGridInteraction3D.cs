using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ElementalHexTactics3D.Grid;
using ElementalHexTactics3D.Units;
using ElementalHexTactics3D.Combat;
using ElementalHexTactics3D.Turn;
using ElementalHexTactics3D.CameraControl;
using ElementalHexTactics3D.UI;

namespace ElementalHexTactics3D.InputHandling
{
    public enum UnitActionMode
    {
        None,
        Move,
        Fireball,
        WaterSurge,
        EarthSpire,
        KineticPush,
        TitanStrike,
        ConsumeLand,
        MagmaCataclysm,
        SummonTitan,
        DeployCommander,
        TearRift
    }

    /// <summary>
    /// Handles 3D mouse raycasting, unit selection, movement, abilities,
    /// predictive Ghost UI preview, and solid non-transparent combat action bar
    /// with full UI click-through blocking.
    /// Split across modular partial classes:
    /// - HexGridInteraction3D.cs (Input, Raycasting & Selection)
    /// - HexGridInteraction3D.Targeting.cs (Action Mode & Range Highlighting)
    /// - HexGridInteraction3D.Actions.cs (Combat & Elemental Execution)
    /// - HexGridInteraction3D.Deployment.cs (Abyssal Rift & Reserve Units)
    /// - HexGridInteraction3D.LegacyHUD.cs (Ghost UI Preview & Fallback IMGUI)
    /// </summary>
    public partial class HexGridInteraction3D : MonoBehaviour
    {
        [Header("Raycast Settings")]
        [SerializeField] private LayerMask tileLayerMask = ~0;

        private HexTile3D currentHoveredTile;
        private HexTile3D currentSelectedTile;
        private TacticalUnit3D currentSelectedUnit;

        private UnitActionMode currentMode = UnitActionMode.None;
        private readonly HashSet<HexTile3D> activeTargetTiles = new HashSet<HexTile3D>();

        private UnityEngine.Camera mainCamera;

        // UI Rectangles for click-through blocking
        private Rect hudRect;
        private Rect actionBarRect;
        private Rect ghostUIRect;
        private bool isGhostUIVisible = false;

        // Solid texture for 100% opaque UI rendering
        private Texture2D solidWhiteTex;

        public static HexGridInteraction3D Instance { get; private set; }

        public HexTile3D HoveredTile => currentHoveredTile;
        public HexTile3D SelectedTile => currentSelectedTile;
        public TacticalUnit3D SelectedUnit => currentSelectedUnit;
        public UnitActionMode CurrentMode => currentMode;

        // Public aliases for CombatHudCanvasUI
        public HexTile3D CurrentHoveredTile => currentHoveredTile;
        public HexTile3D CurrentSelectedTile => currentSelectedTile;
        public TacticalUnit3D CurrentSelectedUnit => currentSelectedUnit;
        public UnitActionMode CurrentActionMode => currentMode;
        public TacticalUnit3D PendingDeployUnit { get; set; }
        public string PendingSkillId { get; set; } = null;
        public int PendingSkillDamage { get; set; } = -1;
        public int PendingSkillManaCost { get; set; } = 0;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(this); return; }
        }

        private void Start()
        {
            mainCamera = UnityEngine.Camera.main;
            EnsureSolidTexture();
            UI.CombatHudCanvasUI.EnsureInstance();

            InitializeReserveUnitsIfNeeded();
            StartCoroutine(ShowBattleStartBannerRoutine());
        }

        private void InitializeReserveUnitsIfNeeded()
        {
            // Dynamic Inception Failsafe: Ensure player units wait in Citadel reserve if Rift is not yet opened
            if (AbyssalRiftConduit3D.Instance == null || AbyssalRiftConduit3D.Instance.RiftTile == null)
            {
                if (Tutorial.TutorialScenarioManager.Instance == null || !Tutorial.TutorialScenarioManager.Instance.IsTutorialActive)
                {
                    TacticalUnitSpawner.ResetPlayerReserveUnits();
                }
            }
        }

        private IEnumerator ShowBattleStartBannerRoutine()
        {
            yield return null; // Wait 1 frame for grid initialization

            if (Tutorial.TutorialScenarioManager.Instance != null && Tutorial.TutorialScenarioManager.Instance.IsTutorialActive)
            {
                yield break;
            }

            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.ShowBanner(
                    "👑 SURVEY THE BATTLEFIELD",
                    "Inspect enemy forces and terrain, then open the Abyssal Rift to lead your vanguard!",
                    2.8f,
                    new Color(0.7f, 0.35f, 1.0f)
                );
            }
        }

        private void Update()
        {
            if (!EnsureMainCamera()) return;

            if (IsInputBlockedByMenuOrBattleEnd())
            {
                ClearHoveredTile();
                return;
            }

            if (TryHandleDebugHotkeys()) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 mousePos = mouse.position.ReadValue();
            Vector2 guiMousePos = new Vector2(mousePos.x, Screen.height - mousePos.y);

            if (IsPointerOverUI(guiMousePos))
            {
                ClearHoveredTile();
                return;
            }

            HandleMouseHover(mousePos);
            HandleMouseClick(mouse);
        }

        private bool EnsureMainCamera()
        {
            if (mainCamera == null)
            {
                mainCamera = UnityEngine.Camera.main;
            }
            return mainCamera != null;
        }

        private bool IsInputBlockedByMenuOrBattleEnd()
        {
            bool isMenuBlocking = TitleMenuCanvasUI.Instance != null
                ? (!TitleMenuCanvasUI.Instance.IsInGame || TitleMenuCanvasUI.Instance.IsPaused)
                : (TitleMenuManager3D.Instance != null && (!TitleMenuManager3D.Instance.IsInGame || TitleMenuManager3D.Instance.IsPaused));

            if (isMenuBlocking) return true;

            if (TurnManager3D.Instance != null && TurnManager3D.Instance.Result != BattleResult.InProgress)
                return true;

            return false;
        }

        private void ClearHoveredTile()
        {
            if (currentHoveredTile != null)
            {
                currentHoveredTile.SetHovered(false);
                currentHoveredTile = null;
            }
        }

        private bool TryHandleDebugHotkeys()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame)
            {
                if (currentMode == UnitActionMode.None || currentMode == UnitActionMode.TearRift)
                {
                    DeselectAll();
                    HexGrid3D.Instance?.GenerateRandomizedBattlefield();
                    return true;
                }
            }
            return false;
        }

        private bool IsPointerOverUI(Vector2 guiMousePos)
        {
            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return true;
            if (hudRect.Contains(guiMousePos)) return true;
            if (actionBarRect.Contains(guiMousePos)) return true;
            if (isGhostUIVisible && ghostUIRect.Contains(guiMousePos)) return true;
            return false;
        }

        private void RaycastTileAndUnit(Vector2 mousePos, out HexTile3D hitTile, out TacticalUnit3D hitUnit)
        {
            hitTile = null;
            hitUnit = null;

            Ray ray = mainCamera.ScreenPointToRay(mousePos);
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f, tileLayerMask))
                return;

            hitUnit = hit.collider.GetComponent<TacticalUnit3D>() ?? hit.collider.GetComponentInParent<TacticalUnit3D>();
            if (hitUnit != null)
            {
                hitTile = hitUnit.CurrentTile ?? (HexGrid3D.Instance != null ? HexGrid3D.Instance.GetTile(hitUnit.Coordinates) : null);
            }
            else if (hit.normal.y > 0.35f)
            {
                // Rejecting vertical cliff walls (hit.normal.y <= 0.35f) prevents raycasting through void onto distant cliff sides
                hitTile = hit.collider.GetComponent<HexTile3D>() ?? hit.collider.GetComponentInParent<HexTile3D>();
            }
        }

        private void HandleMouseHover(Vector2 mousePos)
        {
            RaycastTileAndUnit(mousePos, out HexTile3D hitTile, out _);

            if (hitTile != currentHoveredTile)
            {
                if (currentHoveredTile != null)
                {
                    currentHoveredTile.SetHovered(false);
                }

                currentHoveredTile = hitTile;

                if (currentHoveredTile != null)
                {
                    currentHoveredTile.SetHovered(true);
                }
            }
        }

        private void HandleMouseClick(Mouse mouse)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                HandleLeftMouseClick(mouse.position.ReadValue());
            }
            else if (mouse.rightButton.wasPressedThisFrame || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
            {
                HandleRightClickOrCancel();
            }
        }

        private void HandleLeftMouseClick(Vector2 mousePos)
        {
            RaycastTileAndUnit(mousePos, out HexTile3D raycastTile, out TacticalUnit3D directHitUnit);
            HexTile3D clickedTile = raycastTile ?? currentHoveredTile;

            if (clickedTile == null && directHitUnit == null) return;

            // Don't accept actions during Enemy Turn
            if (TurnManager3D.Instance != null && !TurnManager3D.Instance.IsPlayerTurn) return;

            if (!ValidateTutorialClick(clickedTile)) return;

            // 1. ACTION RESOLUTION (Move, Spells, Push, Deploy Commander, Summon Titan, Tear Rift)
            if (TryResolveActiveActionMode(clickedTile)) return;

            // 2. UNIT SELECTION ON CLICKED TILE (Prioritize living unit over empty Rift)
            if (TrySelectLivingUnitOnTile(clickedTile, directHitUnit)) return;

            // 3. ABYSSAL RIFT BASE PANEL SELECTION (Empty Rift tile clicked)
            if (TrySelectAbyssalRiftTile(clickedTile)) return;

            // 4. EMPTY TILE / FIELD CLICKED
            HandleEmptyFieldClick(clickedTile);
        }

        private bool ValidateTutorialClick(HexTile3D clickedTile)
        {
            if (Tutorial.TutorialScenarioManager.Instance != null && Tutorial.TutorialScenarioManager.Instance.IsTutorialActive)
            {
                if (currentMode != UnitActionMode.None && !activeTargetTiles.Contains(clickedTile))
                {
                    Combat.SoundManager3D.Instance?.PlayButtonClick();
                    return false;
                }
            }
            return true;
        }

        private bool TryResolveActiveActionMode(HexTile3D clickedTile)
        {
            if (currentMode == UnitActionMode.None) return false;

            bool isDeploying = (currentMode == UnitActionMode.DeployCommander || currentMode == UnitActionMode.SummonTitan || currentMode == UnitActionMode.TearRift);
            if ((currentSelectedUnit != null || isDeploying) && clickedTile != null && activeTargetTiles.Contains(clickedTile))
            {
                ExecuteAction(clickedTile);
            }
            else
            {
                Combat.SoundManager3D.Instance?.PlayButtonClick();
            }
            return true;
        }

        private bool TrySelectLivingUnitOnTile(HexTile3D clickedTile, TacticalUnit3D directHitUnit)
        {
            TacticalUnit3D unitOnClickedTile = directHitUnit ?? (clickedTile != null ? clickedTile.GetOccupant() : null);
            if (unitOnClickedTile != null && unitOnClickedTile.gameObject.activeInHierarchy && unitOnClickedTile.CurrentHealth > 0)
            {
                SelectTile(clickedTile);
                SelectUnit(unitOnClickedTile);
                return true;
            }
            return false;
        }

        private bool TrySelectAbyssalRiftTile(HexTile3D clickedTile)
        {
            if (clickedTile != null && AbyssalRiftConduit3D.Instance != null && AbyssalRiftConduit3D.Instance.RiftTile == clickedTile)
            {
                ClearUnitSelection();
                SelectTile(clickedTile);
                SetActionMode(UnitActionMode.None);
                return true;
            }
            return false;
        }

        private void HandleEmptyFieldClick(HexTile3D clickedTile)
        {
            if (clickedTile != null)
            {
                SelectTile(clickedTile);
                ClearUnitSelection();
            }
            else
            {
                DeselectAll();
            }
        }

        private void HandleRightClickOrCancel()
        {
            Combat.SoundManager3D.Instance?.PlayButtonClick();

            if (UI.CombatHudCanvasUI.Instance != null && UI.CombatHudCanvasUI.Instance.IsSubmenuOpen)
            {
                UI.CombatHudCanvasUI.Instance.CloseAllSubmenus();
            }

            if (currentMode != UnitActionMode.None)
            {
                CancelActiveTargetingMode();
            }
            else
            {
                DeselectAll();
            }
        }

        private void CancelActiveTargetingMode()
        {
            SetActionMode(UnitActionMode.None);
            if (currentSelectedUnit != null)
            {
                TacticalCameraController.Instance?.FocusOnPosition(currentSelectedUnit.transform.position);
            }
            else if (AbyssalRiftConduit3D.Instance != null && AbyssalRiftConduit3D.Instance.RiftTile != null)
            {
                TacticalCameraController.Instance?.FocusOnPosition(AbyssalRiftConduit3D.Instance.RiftTile.transform.position);
            }
        }

        public void SelectTile(HexTile3D tile)
        {
            if (currentSelectedTile != null)
            {
                currentSelectedTile.SetSelected(false);
            }

            currentSelectedTile = tile;

            if (currentSelectedTile != null)
            {
                currentSelectedTile.SetSelected(true);
            }
        }

        public void SelectUnit(TacticalUnit3D unit)
        {
            if (currentSelectedUnit != null && currentSelectedUnit != unit)
            {
                currentSelectedUnit.SetSelected(false);
            }

            currentSelectedUnit = unit;
            currentSelectedUnit.SetSelected(true);

            TacticalCameraController.Instance?.FocusOnPosition(currentSelectedUnit.transform.position);
            SetActionMode(UnitActionMode.None);
        }

        private void ClearTargetHighlights()
        {
            foreach (var tile in activeTargetTiles)
            {
                if (tile != null) tile.SetReachable(false);
            }
            activeTargetTiles.Clear();
        }

        public void ClearUnitSelection()
        {
            ClearTargetHighlights();
            currentMode = UnitActionMode.None;
            if (currentSelectedUnit != null)
            {
                currentSelectedUnit.SetSelected(false);
                currentSelectedUnit = null;
            }
        }

        public void DeselectAll()
        {
            ClearUnitSelection();
            if (currentSelectedTile != null)
            {
                currentSelectedTile.SetSelected(false);
                currentSelectedTile = null;
            }
        }

        private void OnDestroy()
        {
            if (solidWhiteTex != null) Destroy(solidWhiteTex);
        }
    }
}
