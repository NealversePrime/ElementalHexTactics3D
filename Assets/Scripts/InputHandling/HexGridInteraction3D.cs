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
    /// </summary>
    public class HexGridInteraction3D : MonoBehaviour
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

        public HexTile3D HoveredTile => currentHoveredTile;
        public HexTile3D SelectedTile => currentSelectedTile;
        public TacticalUnit3D SelectedUnit => currentSelectedUnit;
        public UnitActionMode CurrentMode => currentMode;

        private void Start()
        {
            mainCamera = UnityEngine.Camera.main;
            EnsureSolidTexture();

            // Dynamic Inception Failsafe: Ensure player units wait in Citadel reserve if Rift is not yet opened
            if (AbyssalRiftConduit3D.Instance == null || AbyssalRiftConduit3D.Instance.RiftTile == null)
            {
                TacticalUnit3D cmdr = FindCommanderUnit();
                if (cmdr != null && cmdr.gameObject.activeInHierarchy)
                {
                    if (cmdr.CurrentTile != null && cmdr.CurrentTile.CurrentOccupant == cmdr) cmdr.CurrentTile.CurrentOccupant = null;
                    cmdr.CurrentTile = null;
                    cmdr.gameObject.SetActive(false);
                }

                TacticalUnit3D titan = FindTitanUnit();
                if (titan != null && titan.gameObject.activeInHierarchy)
                {
                    if (titan.CurrentTile != null && titan.CurrentTile.CurrentOccupant == titan) titan.CurrentTile.CurrentOccupant = null;
                    titan.CurrentTile = null;
                    titan.gameObject.SetActive(false);
                }
            }

            StartCoroutine(ShowBattleStartBannerRoutine());
        }

        private IEnumerator ShowBattleStartBannerRoutine()
        {
            yield return null; // Wait 1 frame for grid initialization
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

        private void EnsureSolidTexture()
        {
            if (solidWhiteTex == null)
            {
                solidWhiteTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[] { Color.white, Color.white, Color.white, Color.white };
                solidWhiteTex.SetPixels(pixels);
                solidWhiteTex.Apply();
            }
        }

        private void Update()
        {
            if (mainCamera == null)
            {
                mainCamera = UnityEngine.Camera.main;
                if (mainCamera == null) return;
            }

            bool isMenuBlocking = TitleMenuCanvasUI.Instance != null
                ? (!TitleMenuCanvasUI.Instance.IsInGame || TitleMenuCanvasUI.Instance.IsPaused)
                : (TitleMenuManager3D.Instance != null && (!TitleMenuManager3D.Instance.IsInGame || TitleMenuManager3D.Instance.IsPaused));

            if (isMenuBlocking)
            {
                if (currentHoveredTile != null)
                {
                    currentHoveredTile.SetHovered(false);
                    currentHoveredTile = null;
                }
                return;
            }

            if (TurnManager3D.Instance != null && TurnManager3D.Instance.Result != BattleResult.InProgress)
            {
                if (currentHoveredTile != null)
                {
                    currentHoveredTile.SetHovered(false);
                    currentHoveredTile = null;
                }
                return;
            }

            var kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame)
            {
                if (currentMode == UnitActionMode.None || currentMode == UnitActionMode.TearRift)
                {
                    DeselectAll();
                    HexGrid3D.Instance?.GenerateRandomizedBattlefield();
                    return;
                }
            }

            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 mousePos = mouse.position.ReadValue();
            Vector2 guiMousePos = new Vector2(mousePos.x, Screen.height - mousePos.y);

            // Check if mouse is hovering over any UI element
            if (IsPointerOverUI(guiMousePos))
            {
                // Clear hover on 3D hexes so buttons don't highlight tiles behind them
                if (currentHoveredTile != null)
                {
                    currentHoveredTile.SetHovered(false);
                    currentHoveredTile = null;
                }
                // Block 3D raycast clicks entirely!
                return;
            }

            HandleMouseHover(mousePos);
            HandleMouseClick(mouse);
        }

        private bool IsPointerOverUI(Vector2 guiMousePos)
        {
            if (hudRect.Contains(guiMousePos)) return true;
            if (actionBarRect.Contains(guiMousePos)) return true;
            if (isGhostUIVisible && ghostUIRect.Contains(guiMousePos)) return true;
            return false;
        }

        private void HandleMouseHover(Vector2 mousePos)
        {
            Ray ray = mainCamera.ScreenPointToRay(mousePos);
            HexTile3D hitTile = null;

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, tileLayerMask))
            {
                // 1. Check if we hit a unit directly
                TacticalUnit3D hitUnit = hit.collider.GetComponent<TacticalUnit3D>() ?? hit.collider.GetComponentInParent<TacticalUnit3D>();
                if (hitUnit != null)
                {
                    hitTile = hitUnit.CurrentTile ?? (HexGrid3D.Instance != null ? HexGrid3D.Instance.GetTile(hitUnit.Coordinates) : null);
                }
                else
                {
                    // 2. Otherwise check if we hit a hex tile
                    hitTile = hit.collider.GetComponent<HexTile3D>() ?? hit.collider.GetComponentInParent<HexTile3D>();
                }
            }

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
                Vector2 mousePos = mouse.position.ReadValue();
                Ray ray = mainCamera.ScreenPointToRay(mousePos);
                HexTile3D clickedTile = currentHoveredTile;
                TacticalUnit3D directHitUnit = null;

                if (Physics.Raycast(ray, out RaycastHit hit, 100f, tileLayerMask))
                {
                    directHitUnit = hit.collider.GetComponent<TacticalUnit3D>() ?? hit.collider.GetComponentInParent<TacticalUnit3D>();
                    if (directHitUnit != null)
                    {
                        clickedTile = directHitUnit.CurrentTile ?? clickedTile;
                    }
                    else
                    {
                        HexTile3D hitTile = hit.collider.GetComponent<HexTile3D>() ?? hit.collider.GetComponentInParent<HexTile3D>();
                        if (hitTile != null) clickedTile = hitTile;
                    }
                }

                if (clickedTile == null && directHitUnit == null) return;

                // Don't accept actions during Enemy Turn
                if (TurnManager3D.Instance != null && !TurnManager3D.Instance.IsPlayerTurn) return;

                // 1. ACTION RESOLUTION (Move, Spells, Push, Deploy Commander, Summon Titan, Tear Rift)
                bool isDeploying = (currentMode == UnitActionMode.DeployCommander || currentMode == UnitActionMode.SummonTitan || currentMode == UnitActionMode.TearRift);
                if ((currentSelectedUnit != null || isDeploying) && activeTargetTiles.Contains(clickedTile))
                {
                    ExecuteAction(clickedTile);
                    return;
                }

                // 2. ABYSSAL RIFT BASE PANEL SELECTION
                if (clickedTile != null && AbyssalRiftConduit3D.Instance != null && AbyssalRiftConduit3D.Instance.RiftTile == clickedTile)
                {
                    ClearUnitSelection();
                    SelectTile(clickedTile);
                    // If Commander is still in reserve, prime deployment immediately!
                    TacticalUnit3D cmdr = FindCommanderUnit();
                    if (cmdr != null && !cmdr.gameObject.activeInHierarchy && currentMode != UnitActionMode.DeployCommander)
                    {
                        SetActionMode(UnitActionMode.DeployCommander);
                    }
                    return;
                }

                // 3. UNIT / TILE SELECTION
                if (clickedTile != null)
                {
                    SelectTile(clickedTile);
                }

                TacticalUnit3D unitToSelect = directHitUnit ?? (clickedTile != null ? clickedTile.GetOccupant() : null);
                if (unitToSelect != null)
                {
                    SelectUnit(unitToSelect);
                }
                else if (currentMode == UnitActionMode.None)
                {
                    ClearUnitSelection();
                }
            }
            else if (mouse.rightButton.wasPressedThisFrame)
            {
                // Right click cancels current skill or clears selection
                if (currentMode != UnitActionMode.None)
                {
                    SetActionMode(UnitActionMode.None);
                }
                else
                {
                    DeselectAll();
                }
            }
        }

        private void SelectTile(HexTile3D tile)
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

        private void SelectUnit(TacticalUnit3D unit)
        {
            if (currentSelectedUnit != null && currentSelectedUnit != unit)
            {
                currentSelectedUnit.SetSelected(false);
            }

            currentSelectedUnit = unit;
            currentSelectedUnit.SetSelected(true);

            // If player unit and can move, default to Move mode
            if (unit.Faction == UnitFaction.Player && !unit.HasMovedThisTurn && unit.EffectiveMoveRange > 0)
            {
                SetActionMode(UnitActionMode.Move);
            }
            else
            {
                SetActionMode(UnitActionMode.None);
            }
        }

        public void SetActionMode(UnitActionMode mode)
        {
            currentMode = mode;
            ClearTargetHighlights();
            bool isDeployMode = (mode == UnitActionMode.DeployCommander || mode == UnitActionMode.SummonTitan || mode == UnitActionMode.TearRift);
            if (!isDeployMode && currentSelectedUnit == null) return;
            if (HexGrid3D.Instance == null) return;

            switch (mode)
            {
                case UnitActionMode.Move:
                    if (currentSelectedUnit.HasMovedThisTurn)
                    {
                        Debug.Log($"[Move Mode] {currentSelectedUnit.UnitName} has already moved this turn.");
                        break;
                    }

                    if (currentSelectedUnit.IsImmobilized || currentSelectedUnit.EffectiveMoveRange <= 0)
                    {
                        Debug.Log($"<color=#FF7043><b>[Move Mode]</b></color> {currentSelectedUnit.UnitName} is immobilized and cannot move!");
                        break;
                    }

                    HexTile3D startTile = currentSelectedUnit.CurrentTile;
                    if (startTile == null)
                    {
                        currentSelectedUnit.ReacquireCurrentTile();
                        startTile = currentSelectedUnit.CurrentTile;
                    }

                    if (startTile != null)
                    {
                        int effectiveRange = currentSelectedUnit.EffectiveMoveRange;
                        var reachable = HexPathfinder3D.GetReachableTiles(HexGrid3D.Instance, startTile, effectiveRange);
                        Debug.Log($"<color=#00E5FF><b>[Move Mode]</b></color> Unit: {currentSelectedUnit.UnitName} at {startTile.Coordinates}, EffectiveRange: {effectiveRange} (Base: {currentSelectedUnit.MoveRange}), Found {reachable.Count} reachable tiles.");
                        foreach (var tile in reachable)
                        {
                            activeTargetTiles.Add(tile);
                            tile.SetReachable(true);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[Move Mode] Could not resolve CurrentTile for {currentSelectedUnit.UnitName} at {currentSelectedUnit.Coordinates}");
                    }
                    break;

                case UnitActionMode.Fireball:
                case UnitActionMode.WaterSurge:
                case UnitActionMode.EarthSpire:
                    var spellTiles = HexGrid3D.Instance.GetTilesInRange(currentSelectedUnit.Coordinates, 3);
                    foreach (var tile in spellTiles)
                    {
                        activeTargetTiles.Add(tile);
                        tile.SetReachable(true);
                    }
                    break;

                case UnitActionMode.KineticPush:
                case UnitActionMode.TitanStrike:
                    var adjTiles = HexGrid3D.Instance.GetNeighbors(currentSelectedUnit.Coordinates);
                    foreach (var tile in adjTiles)
                    {
                        activeTargetTiles.Add(tile);
                        tile.SetReachable(true);
                    }
                    break;

                case UnitActionMode.ConsumeLand:
                    // Highlight unit's current tile and neighbors if they have active elements
                    if (currentSelectedUnit.CurrentTile != null && IsTileConsumable(currentSelectedUnit.CurrentTile))
                    {
                        activeTargetTiles.Add(currentSelectedUnit.CurrentTile);
                        currentSelectedUnit.CurrentTile.SetReachable(true);
                    }
                    var harvestNeighbors = HexGrid3D.Instance.GetNeighbors(currentSelectedUnit.Coordinates);
                    foreach (var tile in harvestNeighbors)
                    {
                        if (tile != null && IsTileConsumable(tile))
                        {
                            activeTargetTiles.Add(tile);
                            tile.SetReachable(true);
                        }
                    }
                    break;

                case UnitActionMode.MagmaCataclysm:
                    // Radial target selection within 2 hexes
                    var cataclysmTiles = HexGrid3D.Instance.GetTilesInRange(currentSelectedUnit.Coordinates, 2);
                    foreach (var tile in cataclysmTiles)
                    {
                        activeTargetTiles.Add(tile);
                        tile.SetReachable(true);
                    }
                    break;

                case UnitActionMode.SummonTitan:
                case UnitActionMode.DeployCommander:
                    if (AbyssalRiftConduit3D.Instance != null && HexGrid3D.Instance != null)
                    {
                        var validSummonTiles = AbyssalRiftConduit3D.Instance.GetValidSummonTiles(HexGrid3D.Instance);
                        foreach (var tile in validSummonTiles)
                        {
                            activeTargetTiles.Add(tile);
                            tile.SetReachable(true);
                        }
                    }
                    break;

                case UnitActionMode.TearRift:
                    if (HexGrid3D.Instance != null)
                    {
                        var candidateTiles = AbyssalRiftConduit3D.GetCandidateRiftTiles(HexGrid3D.Instance);
                        Debug.Log($"<color=#BA68C8><b>[Tear Rift Mode]</b></color> Found {candidateTiles.Count} candidate landing hexes across battlefield.");
                        foreach (var tile in candidateTiles)
                        {
                            activeTargetTiles.Add(tile);
                            tile.SetReachable(true);
                        }
                        if (CombatFeedbackManager.Instance != null)
                        {
                            CombatFeedbackManager.Instance.ShowBanner(
                                "🌀 CHOOSE ENTRY POINT",
                                "Click any open hex to tear open the Abyssal Rift and lead your vanguard!",
                                2.2f,
                                new Color(0.75f, 0.35f, 1.0f)
                            );
                        }
                    }
                    break;
            }
        }

        public static bool IsTileConsumable(HexTile3D tile)
        {
            if (tile == null) return false;
            return tile.State == TileState.Magma || tile.State == TileState.Scorched || tile.State == TileState.Water;
        }

        public static bool HasConsumableTilesNearby(TacticalUnit3D unit)
        {
            if (unit == null) return false;
            if (unit.CurrentTile != null && IsTileConsumable(unit.CurrentTile)) return true;

            HexGrid3D grid = HexGrid3D.Instance;
            if (grid == null) return false;

            foreach (var neighborCoord in unit.Coordinates.GetNeighbors())
            {
                var t = grid.GetTile(neighborCoord);
                if (t != null && IsTileConsumable(t)) return true;
            }
            return false;
        }

        public static TacticalUnit3D FindCommanderUnit()
        {
            TacticalUnit3D[] all = Object.FindObjectsByType<TacticalUnit3D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var u in all)
            {
                if (u.Faction == UnitFaction.Player && u.Archetype == UnitArchetype.Commander)
                    return u;
            }
            return null;
        }

        public static bool HasActivePlayerUnitsOnField()
        {
            TacticalUnit3D[] all = Object.FindObjectsByType<TacticalUnit3D>(FindObjectsSortMode.None);
            foreach (var u in all)
            {
                if (u.Faction == UnitFaction.Player && u.gameObject.activeInHierarchy)
                    return true;
            }
            return false;
        }

        public static TacticalUnit3D FindTitanUnit()
        {
            TacticalUnit3D[] all = Object.FindObjectsByType<TacticalUnit3D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var u in all)
            {
                if (u.Faction == UnitFaction.Player && u.Archetype == UnitArchetype.Titan)
                    return u;
            }
            return null;
        }

        private void ExecuteAction(HexTile3D targetTile)
        {
            switch (currentMode)
            {
                case UnitActionMode.Move:
                    if (currentSelectedUnit.HasMovedThisTurn)
                    {
                        Debug.LogWarning($"[Move] {currentSelectedUnit.UnitName} has already moved this turn!");
                        break;
                    }

                    if (currentSelectedUnit.EffectiveMoveRange <= 0)
                    {
                        Debug.LogWarning($"[Move] {currentSelectedUnit.UnitName} is immobilized and cannot move!");
                        ClearTargetHighlights();
                        SetActionMode(UnitActionMode.None);
                        break;
                    }

                    if (!activeTargetTiles.Contains(targetTile))
                    {
                        Debug.LogWarning($"[Move] Target tile {targetTile.Coordinates} is not in reachable range!");
                        break;
                    }

                    HexTile3D startTile = currentSelectedUnit.CurrentTile;
                    if (startTile == null)
                    {
                        currentSelectedUnit.ReacquireCurrentTile();
                        startTile = currentSelectedUnit.CurrentTile;
                    }
                    var path = HexPathfinder3D.FindPath(HexGrid3D.Instance, startTile, targetTile);
                    if (path != null && path.Count > 0 && path.Count <= currentSelectedUnit.EffectiveMoveRange)
                    {
                        currentSelectedUnit.HasMovedThisTurn = true;
                        ClearTargetHighlights();
                        StartCoroutine(currentSelectedUnit.MoveAlongPath(path));
                        SetActionMode(UnitActionMode.None);
                    }
                    else
                    {
                        Debug.LogWarning($"[Move] No valid path within range found to {targetTile.Coordinates} (Path steps: {path?.Count}, MaxAllowed: {currentSelectedUnit.EffectiveMoveRange})");
                    }
                    break;

                case UnitActionMode.Fireball:
                    currentSelectedUnit.HasActedThisTurn = true;
                    ClearTargetHighlights();
                    StartCoroutine(ExecutePlayerSpell(targetTile, ElementType.Fire, 3, new Color(1.0f, 0.45f, 0.1f)));
                    SetActionMode(UnitActionMode.None);
                    break;

                case UnitActionMode.WaterSurge:
                    currentSelectedUnit.HasActedThisTurn = true;
                    ClearTargetHighlights();
                    StartCoroutine(ExecutePlayerSpell(targetTile, ElementType.Water, 2, new Color(0.2f, 0.7f, 1.0f)));
                    SetActionMode(UnitActionMode.None);
                    break;

                case UnitActionMode.EarthSpire:
                    currentSelectedUnit.HasActedThisTurn = true;
                    ClearTargetHighlights();
                    StartCoroutine(ExecutePlayerSpell(targetTile, ElementType.Earth, 2, new Color(0.65f, 0.48f, 0.28f)));
                    SetActionMode(UnitActionMode.None);
                    break;

                case UnitActionMode.KineticPush:
                    TacticalUnit3D targetUnit = targetTile.CurrentOccupant as TacticalUnit3D;
                    if (targetUnit != null)
                    {
                        currentSelectedUnit.HasActedThisTurn = true;
                        ClearTargetHighlights();
                        StartCoroutine(ExecutePlayerPush(targetUnit));
                        SetActionMode(UnitActionMode.None);
                    }
                    break;

                case UnitActionMode.TitanStrike:
                    TacticalUnit3D strikeTarget = targetTile.CurrentOccupant as TacticalUnit3D;
                    if (strikeTarget != null)
                    {
                        currentSelectedUnit.HasActedThisTurn = true;
                        ClearTargetHighlights();
                        StartCoroutine(ExecuteTitanStrike(strikeTarget));
                        SetActionMode(UnitActionMode.None);
                    }
                    break;

                case UnitActionMode.ConsumeLand:
                    if (IsTileConsumable(targetTile))
                    {
                        currentSelectedUnit.HasActedThisTurn = true;
                        ClearTargetHighlights();
                        StartCoroutine(ExecuteConsumeLand(targetTile));
                        SetActionMode(UnitActionMode.None);
                    }
                    break;

                case UnitActionMode.MagmaCataclysm:
                    currentSelectedUnit.HasActedThisTurn = true;
                    ClearTargetHighlights();
                    StartCoroutine(ExecuteMagmaCataclysm(targetTile));
                    SetActionMode(UnitActionMode.None);
                    break;

                case UnitActionMode.DeployCommander:
                    if (AbyssalRiftConduit3D.Instance != null && HexGrid3D.Instance != null)
                    {
                        TacticalUnit3D cmdr = FindCommanderUnit();
                        if (cmdr != null)
                        {
                            ClearTargetHighlights();
                            StartCoroutine(ExecuteDeployCommanderRoutine(cmdr, targetTile));
                            SetActionMode(UnitActionMode.None);
                        }
                    }
                    break;

                case UnitActionMode.SummonTitan:
                    if (AbyssalRiftConduit3D.Instance != null && HexGrid3D.Instance != null)
                    {
                        TacticalUnit3D titan = FindTitanUnit();
                        TacticalUnit3D cmdr = FindCommanderUnit();
                        if (titan != null)
                        {
                            if (cmdr != null && cmdr.gameObject.activeInHierarchy)
                            {
                                if (!cmdr.ConsumeElementalCore(1)) break;
                                cmdr.HasActedThisTurn = true;
                            }
                            ClearTargetHighlights();
                            StartCoroutine(ExecuteDeployTitanRoutine(titan, targetTile));
                            SetActionMode(UnitActionMode.None);
                        }
                    }
                    break;

                case UnitActionMode.TearRift:
                    if (HexGrid3D.Instance != null)
                    {
                        ClearTargetHighlights();
                        StartCoroutine(ExecuteTearRiftAndDeployRoutine(targetTile));
                        SetActionMode(UnitActionMode.None);
                    }
                    break;
            }
        }

        private IEnumerator ExecuteTearRiftAndDeployRoutine(HexTile3D targetTile)
        {
            if (targetTile == null || HexGrid3D.Instance == null) yield break;

            // 1. Tear open the Abyssal Rift Conduit on the chosen tile!
            AbyssalRiftConduit3D conduit = AbyssalRiftConduit3D.OpenRiftAt(targetTile, radius: 2);
            SelectTile(targetTile);

            yield return new WaitForSeconds(0.35f);

            // 2. Immediately deploy the Commander out of the newly opened Rift!
            TacticalUnit3D cmdr = FindCommanderUnit();
            if (cmdr != null)
            {
                yield return ExecuteDeployCommanderRoutine(cmdr, targetTile);
            }
        }

        private IEnumerator ExecuteDeployCommanderRoutine(TacticalUnit3D cmdr, HexTile3D targetTile)
        {
            if (cmdr == null || targetTile == null || AbyssalRiftConduit3D.Instance == null) yield break;

            yield return AbyssalRiftConduit3D.Instance.ExecuteDeployUnitRoutine(cmdr, targetTile, HexGrid3D.Instance);

            SelectTile(targetTile);
            SelectUnit(cmdr);
        }

        private IEnumerator ExecuteDeployTitanRoutine(TacticalUnit3D titan, HexTile3D targetTile)
        {
            if (titan == null || targetTile == null || AbyssalRiftConduit3D.Instance == null) yield break;

            yield return AbyssalRiftConduit3D.Instance.ExecuteTitanSummonRoutine(titan, targetTile, HexGrid3D.Instance);

            SelectTile(targetTile);
            SelectUnit(titan);
        }

        private IEnumerator RecallUnitRoutine(TacticalUnit3D unit)
        {
            if (unit == null || AbyssalRiftConduit3D.Instance == null) yield break;
            DeselectAll();
            yield return AbyssalRiftConduit3D.Instance.RecallUnitRoutine(unit);
        }

        private IEnumerator ExecuteConsumeLand(HexTile3D targetTile)
        {
            if (currentSelectedUnit == null || targetTile == null) yield break;

            // Subtle lunge toward tile if adjacent
            if (targetTile != currentSelectedUnit.CurrentTile)
            {
                yield return currentSelectedUnit.PlayAttackLunge(targetTile.transform.position, 0.18f);
            }

            // SFX & Camera Feedback
            SoundManager3D.Instance?.PlayConsumeLand();
            TacticalCameraController.Instance?.Shake(0.18f, 0.18f);

            // Spawn ascending energy shockwave
            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.StartCoroutine(
                    CombatFeedbackManager.Instance.SpawnShockwaveEffect(targetTile.transform.position, new Color(1f, 0.85f, 0.2f), maxRadius: 1.4f, duration: 0.35f));
            }

            // Mutate tile: Magma -> Scorched (tier 1); Scorched/Water -> Barren (tier 0)
            TileState prevState = targetTile.State;
            TileState newState = TileState.Barren;
            int newTier = 0;

            if (prevState == TileState.Magma)
            {
                newState = TileState.Scorched;
                newTier = 1;
            }
            else
            {
                newState = TileState.Barren;
                newTier = 0;
            }

            targetTile.SetState(newState, newTier);

            // Grant +1 Elemental Core to the unit
            currentSelectedUnit.AddElementalCore(1);

            // Refresh attunements
            TacticalUnit3D occupant = targetTile.GetOccupant();
            if (occupant != null) occupant.UpdateAttunement();
            currentSelectedUnit.UpdateAttunement();

            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.ShowBanner("⚡ LAND HARVESTED!", $"{currentSelectedUnit.UnitName} harvested energy from {prevState} into an Elemental Core!", 1.3f, new Color(1.0f, 0.85f, 0.2f));
            }

            yield return new WaitForSeconds(0.2f);
        }

        private IEnumerator ExecuteMagmaCataclysm(HexTile3D centerTile)
        {
            if (currentSelectedUnit == null || centerTile == null) yield break;

            // Deduct 1 Elemental Core
            if (!currentSelectedUnit.ConsumeElementalCore(1)) yield break;

            // Titan roar / shudder
            yield return currentSelectedUnit.PlayAttackLunge(centerTile.transform.position, 0.28f);

            // Violent Camera Shake & Seismic Booming SFX
            TacticalCameraController.Instance?.Shake(0.65f, 0.55f);
            SoundManager3D.Instance?.PlayCataclysm();

            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.ShowBanner("🌋 TITAN ULTIMATE: MAGMA CATACLYSM!", "A subterranean cataclysm ruptures the earth with molten fury!", 1.6f, new Color(1.0f, 0.35f, 0.1f));
                CombatFeedbackManager.Instance.StartCoroutine(
                    CombatFeedbackManager.Instance.SpawnShockwaveEffect(centerTile.transform.position, new Color(1.0f, 0.45f, 0.1f), maxRadius: 3.2f, duration: 0.55f));
            }

            // 1-Hex radius explosion (epicenter + 6 adjacent neighbors)
            HexGrid3D grid = HexGrid3D.Instance;
            List<HexCoordinates> blastCoords = centerTile.Coordinates.GetRange(1);
            HashSet<TacticalUnit3D> hitEnemies = new HashSet<TacticalUnit3D>();

            foreach (var coord in blastCoords)
            {
                HexTile3D tile = grid != null ? grid.GetTile(coord) : null;
                if (tile == null) continue;

                // Erupt tiles: Scorched -> Magma; others -> Scorched
                if (tile.State == TileState.Scorched)
                {
                    tile.SetState(TileState.Magma, 2);
                }
                else if (tile.State != TileState.Magma)
                {
                    tile.SetState(TileState.Scorched, 1);
                }

                TacticalUnit3D occupant = tile.GetOccupant();
                if (occupant != null && occupant.Faction != currentSelectedUnit.Faction && !hitEnemies.Contains(occupant))
                {
                    hitEnemies.Add(occupant);
                }
            }

            yield return new WaitForSeconds(0.12f);

            // Deal 8 massive damage to all enemies caught in the eruption!
            foreach (var enemy in hitEnemies)
            {
                if (enemy != null && enemy.CurrentHealth > 0)
                {
                    enemy.TakeDamage(8, "💥 CATACLYSM! -8");
                }
            }

            yield return new WaitForSeconds(0.3f);
        }

        private IEnumerator ExecuteTitanStrike(TacticalUnit3D targetEnemy)
        {
            if (currentSelectedUnit != null)
            {
                yield return currentSelectedUnit.PlayAttackLunge(targetEnemy.transform.position, 0.22f);
                TacticalCameraController.Instance?.Shake(0.25f, 0.25f);
                SoundManager3D.Instance?.PlaySlam(1.1f);
                int totalDmg = currentSelectedUnit.EffectiveAttackDamage;
                string label = (currentSelectedUnit.BonusAttackDamage > 0)
                    ? $"-{totalDmg} CRIT!"
                    : $"-{totalDmg}";
                targetEnemy.TakeDamage(totalDmg, label);
            }
        }

        private IEnumerator ExecutePlayerSpell(HexTile3D targetTile, ElementType element, int dmg, Color projColor)
        {
            if (currentSelectedUnit != null)
            {
                yield return currentSelectedUnit.PlayAttackLunge(targetTile.transform.position, 0.22f);
                Vector3 casterPos = currentSelectedUnit.transform.position + Vector3.up * 0.8f;
                Vector3 targetPos = targetTile.GetTopCenterPosition() + Vector3.up * 0.4f;

                SoundManager3D.Instance?.PlaySpellCast(element == ElementType.Fire);

                if (CombatFeedbackManager.Instance != null)
                {
                    yield return CombatFeedbackManager.Instance.SpawnSpellProjectile(casterPos, targetPos, projColor, 0.26f);
                }
            }
            TerrainReactionSystem.ApplySpell(targetTile, element, damage: dmg);
        }

        private IEnumerator ExecutePlayerPush(TacticalUnit3D targetUnit)
        {
            if (currentSelectedUnit != null)
            {
                yield return currentSelectedUnit.PlayAttackLunge(targetUnit.transform.position, 0.22f);
                yield return PushMechanic3D.ExecutePushRoutine(currentSelectedUnit, targetUnit, HexGrid3D.Instance);
            }
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

        private void DrawSolidPanel(Rect rect, Color bgColor, Color borderColor, int borderWidth = 2)
        {
            EnsureSolidTexture();
            Color prevColor = GUI.color;

            // 1. Draw solid background fill
            GUI.color = bgColor;
            GUI.DrawTexture(rect, solidWhiteTex, ScaleMode.StretchToFill);

            // 2. Draw sharp borders
            if (borderWidth > 0 && borderColor.a > 0f)
            {
                GUI.color = borderColor;
                GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, borderWidth), solidWhiteTex);
                GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - borderWidth, rect.width, borderWidth), solidWhiteTex);
                GUI.DrawTexture(new Rect(rect.x, rect.y, borderWidth, rect.height), solidWhiteTex);
                GUI.DrawTexture(new Rect(rect.x + rect.width - borderWidth, rect.y, borderWidth, rect.height), solidWhiteTex);
            }

            GUI.color = prevColor;
        }

        private bool DrawOpaqueButton(Rect rect, string text, Color normalColor, Color activeColor, bool isActive, bool enabled = true)
        {
            Vector2 mousePos = Event.current.mousePosition;
            bool isHover = enabled && rect.Contains(mousePos);

            Color bgColor;
            Color borderColor;

            if (!enabled)
            {
                bgColor = new Color(0.18f, 0.20f, 0.24f, 0.85f);
                borderColor = new Color(0.28f, 0.30f, 0.35f, 0.5f);
            }
            else if (isActive)
            {
                bgColor = activeColor;
                borderColor = new Color(1.0f, 0.9f, 0.2f, 1f); // Bright yellow tactical active border
            }
            else if (isHover)
            {
                bgColor = Color.Lerp(normalColor, Color.white, 0.25f);
                borderColor = Color.white;
            }
            else
            {
                bgColor = normalColor;
                borderColor = new Color(normalColor.r * 1.35f, normalColor.g * 1.35f, normalColor.b * 1.35f, 0.9f);
            }

            // Draw solid opaque button background & border
            DrawSolidPanel(rect, bgColor, borderColor, isActive ? 3 : (isHover ? 2 : 1));

            // Draw Button Label
            Color prevContentColor = GUI.contentColor;
            GUI.contentColor = enabled ? (isActive ? Color.white : (isHover ? Color.yellow : Color.white)) : new Color(0.5f, 0.5f, 0.5f, 1f);

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 12,
                wordWrap = true
            };
            GUI.Label(rect, text, labelStyle);
            GUI.contentColor = prevContentColor;

            // Handle Input Click with Event Consumption
            if (enabled && Event.current.type == EventType.MouseDown && rect.Contains(mousePos))
            {
                Event.current.Use(); // Consume event to prevent click-through
                SoundManager3D.Instance?.PlayButtonClick();
                return true;
            }

            return false;
        }

        private void OnGUI()
        {
            if (!Application.isPlaying) return;

            EnsureSolidTexture();

            bool isMenuBlocking = TitleMenuCanvasUI.Instance != null
                ? (!TitleMenuCanvasUI.Instance.IsInGame || TitleMenuCanvasUI.Instance.IsPaused)
                : (TitleMenuManager3D.Instance != null && (!TitleMenuManager3D.Instance.IsInGame || TitleMenuManager3D.Instance.IsPaused));

            if (isMenuBlocking)
            {
                return; // Suppress standard combat HUD when Title/Options/Pause menu is active
            }

            if (TurnManager3D.Instance != null && TurnManager3D.Instance.Result != BattleResult.InProgress)
            {
                return; // Suppress standard HUD when end-game modal is active
            }

            DrawTacticalInfoHUD();
            DrawActionBarHUD();
            DrawGhostUIPreview();
        }

        private void DrawTacticalInfoHUD()
        {
            hudRect = new Rect(16, 16, 420, 185);
            DrawSolidPanel(hudRect, new Color(0.08f, 0.10f, 0.14f, 0.95f), new Color(0.25f, 0.35f, 0.48f, 1f), 2);

            GUILayout.BeginArea(new Rect(hudRect.x + 12, hudRect.y + 10, hudRect.width - 24, hudRect.height - 20));

            string roundText = TurnManager3D.Instance != null
                ? $"Round {TurnManager3D.Instance.CurrentRound} - {(TurnManager3D.Instance.IsPlayerTurn ? "<color=#64B5F6>PLAYER TURN</color>" : "<color=#EF5350>ENEMY TURN</color>")}"
                : "Player Turn";

            var mission = ElementalHexTactics3D.Campaign.ExpeditionTrilemmaGenerator.CurrentActiveMission;
            string missionInfo = mission != null ? $" | <color=#FFD54F>{mission.Title}</color> <size=11>({mission.GetThreatStars()})</size>" : "";

            GUILayout.BeginHorizontal();
            GUILayout.Label($"<size=14><b>Incursion:</b></size>{missionInfo} | {roundText}");
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("🎲 New Map", GUILayout.Width(82), GUILayout.Height(20)))
            {
                DeselectAll();
                HexGrid3D.Instance?.GenerateRandomizedBattlefield();
            }
            GUILayout.Space(4);

            if (TurnManager3D.Instance != null)
            {
                bool aiOff = TurnManager3D.Instance.DisableEnemyAI;
                string aiLabel = aiOff ? "<color=#EF5350>🤖 AI: OFF</color>" : "<color=#81C784>🤖 AI: ON</color>";
                if (GUILayout.Button(aiLabel, GUILayout.Width(75), GUILayout.Height(20)))
                {
                    TurnManager3D.Instance.DisableEnemyAI = !TurnManager3D.Instance.DisableEnemyAI;
                }
                GUILayout.Space(4);
            }

            bool isMuted = (SoundManager3D.Instance != null && SoundManager3D.Instance.IsMuted);
            string audioLabel = isMuted ? "🔇 Audio" : "🔊 Audio";
            if (GUILayout.Button(audioLabel, GUILayout.Width(75), GUILayout.Height(20)))
            {
                if (SoundManager3D.Instance != null)
                {
                    SoundManager3D.Instance.IsMuted = !SoundManager3D.Instance.IsMuted;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("<color=#FFD54F><b>Camera:</b></color> <b>Q / E</b> Rotate | <b>WASD</b> Pan | <b>Scroll</b> Zoom");
            GUILayout.Space(3);

            if (currentSelectedUnit != null)
            {
                string fColor = (currentSelectedUnit.Faction == UnitFaction.Player) ? "#64B5F6" : "#EF5350";
                string statusText = currentSelectedUnit.IsExhausted ? "<color=#B0BEC5>[Exhausted]</color>" : "<color=#81C784>[Ready]</color>";
                GUILayout.Label($"<color={fColor}><b>Selected:</b> {currentSelectedUnit.UnitName}</color> ({currentSelectedUnit.Archetype}) {statusText}");
                GUILayout.Label($"<b>HP:</b> {currentSelectedUnit.CurrentHealth}/{currentSelectedUnit.MaxHealth} | <b>ATK:</b> {currentSelectedUnit.EffectiveAttackDamage} | <b>Move:</b> {currentSelectedUnit.EffectiveMoveRange} | <b>Moved:</b> {(currentSelectedUnit.HasMovedThisTurn ? "<color=#B0BEC5>✓</color>" : "<color=#81C784>✗</color>")} | <b>Acted:</b> {(currentSelectedUnit.HasActedThisTurn ? "<color=#B0BEC5>✓</color>" : "<color=#81C784>✗</color>")}");

                string attuneColor = (currentSelectedUnit.BonusAttackDamage > 0) ? "#FFA726" : (currentSelectedUnit.BonusMoveRange > 0 ? "#29B6F6" : "#CFD8DC");
                GUILayout.Label($"<b>Attunement:</b> <color={attuneColor}>{currentSelectedUnit.CurrentAttunementName}</color> | <b>Cores:</b> <color=#FFD54F>★ {currentSelectedUnit.ElementalCores}</color>");

                if (currentHoveredTile != null && AbyssalRiftConduit3D.Instance != null && AbyssalRiftConduit3D.Instance.RiftTile == currentHoveredTile)
                {
                    GUILayout.Label("<color=#BA68C8>🌀 <b>Hovering Abyssal Rift Conduit</b> (Sacrifice Maw)</color>");
                }
            }
            else if (currentHoveredTile != null)
            {
                if (AbyssalRiftConduit3D.Instance != null && AbyssalRiftConduit3D.Instance.RiftTile == currentHoveredTile)
                {
                    GUILayout.Label("<color=#BA68C8><b>🌀 ABYSSAL RIFT CONDUIT</b></color> (Sanctuary Gateway)");
                    GUILayout.Label($"<b>Coordinates:</b> {currentHoveredTile.Coordinates} | <b>Sacrifices:</b> {AbyssalRiftConduit3D.Instance.TotalSacrifices}");
                    GUILayout.Label("<i><color=#FFE082>Shove enemies here to sacrifice for +1 Core!</color></i>");
                }
                else
                {
                    GUILayout.Label($"<color=#81C784><b>Hovered Hex:</b></color> {currentHoveredTile.Coordinates} | Elev: {currentHoveredTile.Elevation}");
                    GUILayout.Label($"<b>Terrain:</b> {currentHoveredTile.State} (Tier {currentHoveredTile.TierLevel})");
                }
                if (currentHoveredTile.CurrentOccupant is TacticalUnit3D occupant)
                {
                    GUILayout.Label($"<color=#FFA726><b>Occupant:</b></color> {occupant.UnitName} (HP: {occupant.CurrentHealth}, {occupant.CurrentAttunementName})");
                }
            }
            else
            {
                GUILayout.Label("<color=#9E9E9E>Select Commander or Titan to act...</color>");
            }

            GUILayout.EndArea();
        }

        private void DrawActionBarHUD()
        {
            bool isBattleActive = (TurnManager3D.Instance == null || TurnManager3D.Instance.Result == BattleResult.InProgress);
            bool isPlayerTurn = isBattleActive && (TurnManager3D.Instance == null || TurnManager3D.Instance.IsPlayerTurn);
            bool canAct = isPlayerTurn && (currentSelectedUnit != null && currentSelectedUnit.Faction == UnitFaction.Player);
            bool canMove = canAct && !currentSelectedUnit.HasMovedThisTurn && currentSelectedUnit.EffectiveMoveRange > 0;
            bool canCombat = canAct && !currentSelectedUnit.HasActedThisTurn;

            float barWidth = 980f;
            float barHeight = 62f;
            float startX = (Screen.width - barWidth) * 0.5f;
            float startY = Screen.height - barHeight - 16f;

            actionBarRect = new Rect(startX, startY, barWidth, barHeight);

            // Draw solid bar background with steel blue tactical border
            DrawSolidPanel(actionBarRect, new Color(0.08f, 0.10f, 0.14f, 0.96f), new Color(0.25f, 0.35f, 0.45f, 1f), 2);

            float btnY = startY + 8f;
            float btnH = barHeight - 16f;
            float curX = startX + 10f;
            float spacing = 8f;

            // 1. IF AN ENEMY IS SELECTED: Display enemy inspection panel
            if (currentSelectedUnit != null && currentSelectedUnit.Faction == UnitFaction.Enemy)
            {
                Rect enemyInspectRect = new Rect(curX, btnY, 440f, btnH);
                curX += 440f + spacing;
                string enemyInfo = $"<color=#EF5350><b>👁️ ENEMY: {currentSelectedUnit.UnitName}</b></color> ({currentSelectedUnit.Archetype})\n" +
                                   $"<size=11>HP: {currentSelectedUnit.CurrentHealth}/{currentSelectedUnit.MaxHealth} | ATK: {currentSelectedUnit.EffectiveAttackDamage} | Move: {currentSelectedUnit.EffectiveMoveRange} | Affinity: {currentSelectedUnit.Affinity}</size>";
                DrawSolidPanel(enemyInspectRect, new Color(0.18f, 0.08f, 0.08f, 0.95f), new Color(0.85f, 0.25f, 0.25f, 0.8f), 1);
                GUIStyle enemyStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, richText = true };
                GUI.Label(enemyInspectRect, enemyInfo, enemyStyle);

                // If Rift is not yet opened, still allow tearing open Rift from here!
                bool riftPlaced = (AbyssalRiftConduit3D.Instance != null && AbyssalRiftConduit3D.Instance.RiftTile != null);
                if (!riftPlaced)
                {
                    Rect tearRect = new Rect(curX, btnY, 260f, btnH);
                    curX += 260f + spacing;
                    string tearLabel = (currentMode == UnitActionMode.TearRift)
                        ? "<b>[Targeting Entry...]</b>\n<size=10>(Click Any Open Hex)</size>"
                        : "<b>🌀 Tear Open Abyssal Rift</b>\n<size=10>(Choose Landing Hex & Deploy)</size>";

                    if (DrawOpaqueButton(tearRect, tearLabel,
                        new Color(0.45f, 0.15f, 0.70f, 1f), new Color(0.75f, 0.25f, 1.0f, 1f),
                        currentMode == UnitActionMode.TearRift, isPlayerTurn))
                    {
                        ClearUnitSelection();
                        SetActionMode(currentMode == UnitActionMode.TearRift ? UnitActionMode.None : UnitActionMode.TearRift);
                    }
                }

                // End Turn Button
                float endX = startX + barWidth - 110f - 10f;
                Rect endRect = new Rect(endX, btnY, 110f, btnH);
                if (DrawOpaqueButton(endRect, "<b>END TURN</b>",
                    new Color(0.65f, 0.15f, 0.15f, 1f), new Color(0.85f, 0.20f, 0.20f, 1f),
                    false, isPlayerTurn))
                {
                    if (TurnManager3D.Instance != null)
                    {
                        DeselectAll();
                        TurnManager3D.Instance.EndPlayerTurn();
                    }
                }
                return;
            }

            // 2. RIFT NOT YET OPENED: Prompt player to tear open the Rift!
            bool riftExists = (AbyssalRiftConduit3D.Instance != null && AbyssalRiftConduit3D.Instance.RiftTile != null);
            if (!riftExists && currentSelectedUnit == null)
            {
                Rect titleRect = new Rect(curX, btnY, 190f, btnH);
                curX += 190f + spacing;
                string titleText = "<color=#BA68C8><b>🌀 CITADEL GATEWAY</b></color>\n<size=10>(Rift Unanchored)</size>";
                DrawSolidPanel(titleRect, new Color(0.12f, 0.08f, 0.18f, 0.95f), new Color(0.65f, 0.25f, 0.95f, 0.8f), 1);
                GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 11 };
                GUI.Label(titleRect, titleText, titleStyle);

                Rect tearRect = new Rect(curX, btnY, 280f, btnH);
                curX += 280f + spacing;
                string tearLabel = (currentMode == UnitActionMode.TearRift)
                    ? "<b>[Targeting Entry...]</b>\n<size=10>(Click Any Open Hex)</size>"
                    : "<b>🌀 Tear Open Abyssal Rift</b>\n<size=10>(Choose Landing Hex & Deploy)</size>";

                if (DrawOpaqueButton(tearRect, tearLabel,
                    new Color(0.45f, 0.15f, 0.70f, 1f), new Color(0.75f, 0.25f, 1.0f, 1f),
                    currentMode == UnitActionMode.TearRift, isPlayerTurn))
                {
                    SetActionMode(currentMode == UnitActionMode.TearRift ? UnitActionMode.None : UnitActionMode.TearRift);
                }

                // Quick Randomize Map Button
                Rect randRect = new Rect(curX, btnY, 220f, btnH);
                curX += 220f + spacing;
                if (DrawOpaqueButton(randRect, "<b>🎲 Randomize Map [R]</b>\n<size=10>(Roll New Incursion)</size>",
                    new Color(0.18f, 0.28f, 0.40f, 1f), new Color(0.35f, 0.65f, 0.95f, 1f),
                    false, isPlayerTurn))
                {
                    DeselectAll();
                    HexGrid3D.Instance?.GenerateRandomizedBattlefield();
                }

                // End Turn Button
                float endX = startX + barWidth - 110f - 10f;
                Rect endRect = new Rect(endX, btnY, 110f, btnH);
                if (DrawOpaqueButton(endRect, "<b>END TURN</b>",
                    new Color(0.65f, 0.15f, 0.15f, 1f), new Color(0.85f, 0.20f, 0.20f, 1f),
                    false, isPlayerTurn))
                {
                    if (TurnManager3D.Instance != null)
                    {
                        DeselectAll();
                        TurnManager3D.Instance.EndPlayerTurn();
                    }
                }
                return;
            }

            // 3. CHECK IF WE SHOULD DISPLAY THE CITADEL BASE PANEL ACTION BAR
            bool isRiftSelected = (currentSelectedTile != null && AbyssalRiftConduit3D.Instance != null && AbyssalRiftConduit3D.Instance.RiftTile == currentSelectedTile);
            bool isDeployMode = (currentMode == UnitActionMode.DeployCommander || currentMode == UnitActionMode.SummonTitan);
            bool noActiveUnits = !HasActivePlayerUnitsOnField();

            if (currentSelectedUnit == null && (isRiftSelected || isDeployMode || noActiveUnits))
            {
                TacticalUnit3D cmdr = FindCommanderUnit();
                TacticalUnit3D titan = FindTitanUnit();

                bool cmdrInReserve = (cmdr != null && !cmdr.gameObject.activeInHierarchy);
                bool titanInReserve = (titan != null && !titan.gameObject.activeInHierarchy);
                bool canDeployCmdr = isPlayerTurn && cmdrInReserve;
                bool canSummonTitan = isPlayerTurn && (cmdr != null && cmdr.ElementalCores >= 1) && (titan != null);

                // Base Panel Title Banner
                Rect titleRect = new Rect(curX, btnY, 180f, btnH);
                curX += 180f + spacing;
                string titleText = "<color=#BA68C8><b>🌀 CITADEL RIFT</b></color>\n<size=10>(Base Panel Gateway)</size>";
                DrawSolidPanel(titleRect, new Color(0.12f, 0.08f, 0.18f, 0.95f), new Color(0.65f, 0.25f, 0.95f, 0.8f), 1);
                GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 11 };
                GUI.Label(titleRect, titleText, titleStyle);

                // 1. Deploy Commander Button
                Rect cmdrRect = new Rect(curX, btnY, 170f, btnH);
                curX += 170f + spacing;
                string cmdrLabel;
                if (!cmdrInReserve)
                {
                    cmdrLabel = "<color=#90A4AE>👤 Commander\n<size=10>(On Field)</size></color>";
                }
                else
                {
                    cmdrLabel = (currentMode == UnitActionMode.DeployCommander)
                        ? "<b>[Deploying...]</b>\n<size=10>(Click Hex)</size>"
                        : "👤 Deploy Commander\n<size=10>(Free Vanguard)</size>";
                }

                if (DrawOpaqueButton(cmdrRect, cmdrLabel,
                    new Color(0.40f, 0.15f, 0.65f, 1f), new Color(0.70f, 0.30f, 1.0f, 1f),
                    currentMode == UnitActionMode.DeployCommander, canDeployCmdr))
                {
                    SetActionMode(currentMode == UnitActionMode.DeployCommander ? UnitActionMode.None : UnitActionMode.DeployCommander);
                }

                // 2. Summon Titan Button
                Rect titanRect = new Rect(curX, btnY, 170f, btnH);
                curX += 170f + spacing;
                string titanLabel;
                if (cmdr != null && cmdr.ElementalCores < 1)
                {
                    titanLabel = "<color=#90A4AE>🌋 Summon Titan\n<size=10>(Req 1 Core)</size></color>";
                }
                else if (currentMode == UnitActionMode.SummonTitan)
                {
                    titanLabel = "<b>[Summoning...]</b>\n<size=10>(Click Hex)</size>";
                }
                else
                {
                    titanLabel = titanInReserve
                        ? "🌋 Summon Titan\n<size=10>(1 Core)</size>"
                        : "🌋 Warp Titan\n<size=10>(Redeploy & Slam)</size>";
                }

                if (DrawOpaqueButton(titanRect, titanLabel,
                    new Color(0.75f, 0.25f, 0.12f, 1f), new Color(1.0f, 0.40f, 0.15f, 1f),
                    currentMode == UnitActionMode.SummonTitan, canSummonTitan))
                {
                    SetActionMode(currentMode == UnitActionMode.SummonTitan ? UnitActionMode.None : UnitActionMode.SummonTitan);
                }

                // End Turn Button placed on far right
                float endX = startX + barWidth - 110f - 10f;
                Rect endRect = new Rect(endX, btnY, 110f, btnH);
                if (DrawOpaqueButton(endRect, "<b>END TURN</b>",
                    new Color(0.65f, 0.15f, 0.15f, 1f), new Color(0.85f, 0.20f, 0.20f, 1f),
                    false, isPlayerTurn))
                {
                    if (TurnManager3D.Instance != null)
                    {
                        DeselectAll();
                        TurnManager3D.Instance.EndPlayerTurn();
                    }
                }
                return;
            }

            // 1. Move Button (Universal)
            Rect moveRect = new Rect(curX, btnY, 80f, btnH);
            curX += 80f + spacing;
            string moveLabel;
            if (currentSelectedUnit != null && currentSelectedUnit.HasMovedThisTurn)
            {
                moveLabel = "<color=#90A4AE><b>[Moved]</b></color>";
            }
            else if (currentSelectedUnit != null && currentSelectedUnit.IsImmobilized)
            {
                moveLabel = "<color=#EF5350><b>[Trapped]</b></color>";
            }
            else if (currentSelectedUnit != null && currentSelectedUnit.IsCrippled)
            {
                moveLabel = (currentMode == UnitActionMode.Move) ? "<b>[Move 1]</b>" : "Move (1)";
            }
            else
            {
                moveLabel = (currentMode == UnitActionMode.Move) ? "<b>[Moving]</b>" : "Move";
            }
            if (DrawOpaqueButton(moveRect, moveLabel,
                new Color(0.18f, 0.32f, 0.48f, 1f), new Color(0.15f, 0.55f, 0.90f, 1f),
                currentMode == UnitActionMode.Move, canMove))
            {
                SetActionMode(currentMode == UnitActionMode.Move ? UnitActionMode.None : UnitActionMode.Move);
            }

            // 2. Contextual Abilities based on Archetype
            if (currentSelectedUnit != null && currentSelectedUnit.Archetype == UnitArchetype.Titan)
            {
                // TITAN ABILITY: Heavy Melee Strike
                Rect strikeRect = new Rect(curX, btnY, 120f, btnH);
                curX += 120f + spacing;
                string strikeLabel = (currentSelectedUnit.HasActedThisTurn)
                    ? "<color=#90A4AE>⚔️ Titan Strike\n<size=10>(Acted)</size></color>"
                    : $"⚔️ Titan Strike\n<size=10>(Dmg {currentSelectedUnit.EffectiveAttackDamage})</size>";
                if (DrawOpaqueButton(strikeRect, strikeLabel,
                    new Color(0.65f, 0.22f, 0.15f, 1f), new Color(0.95f, 0.35f, 0.15f, 1f),
                    currentMode == UnitActionMode.TitanStrike, canCombat))
                {
                    SetActionMode(currentMode == UnitActionMode.TitanStrike ? UnitActionMode.None : UnitActionMode.TitanStrike);
                }

                // TITAN ABILITY: Tail Shove
                Rect shoveRect = new Rect(curX, btnY, 105f, btnH);
                curX += 105f + spacing;
                string shoveLabel = (currentSelectedUnit.HasActedThisTurn)
                    ? "<color=#90A4AE>💨 Tail Shove\n<size=10>(Acted)</size></color>"
                    : "💨 Tail Shove\n<size=10>(Shove 1)</size>";
                if (DrawOpaqueButton(shoveRect, shoveLabel,
                    new Color(0.38f, 0.18f, 0.52f, 1f), new Color(0.65f, 0.28f, 0.90f, 1f),
                    currentMode == UnitActionMode.KineticPush, canCombat))
                {
                    SetActionMode(currentMode == UnitActionMode.KineticPush ? UnitActionMode.None : UnitActionMode.KineticPush);
                }

                // TITAN ABILITY: Siphon Land
                bool canSiphon = canCombat && HasConsumableTilesNearby(currentSelectedUnit);
                Rect siphonRect = new Rect(curX, btnY, 120f, btnH);
                curX += 120f + spacing;
                string siphonLabel = (currentSelectedUnit.HasActedThisTurn)
                    ? "<color=#90A4AE>⚡ Siphon Land\n<size=10>(Acted)</size></color>"
                    : "⚡ Siphon Land\n<size=10>(+1 Core)</size>";
                if (DrawOpaqueButton(siphonRect, siphonLabel,
                    new Color(0.70f, 0.55f, 0.15f, 1f), new Color(1.0f, 0.82f, 0.20f, 1f),
                    currentMode == UnitActionMode.ConsumeLand, canSiphon))
                {
                    SetActionMode(currentMode == UnitActionMode.ConsumeLand ? UnitActionMode.None : UnitActionMode.ConsumeLand);
                }

                // TITAN ULTIMATE ABILITY: Magma Cataclysm
                bool canUlt = canCombat && currentSelectedUnit.ElementalCores >= 1;
                Rect ultRect = new Rect(curX, btnY, 140f, btnH);
                curX += 140f + spacing;
                string ultLabel = (currentSelectedUnit.ElementalCores < 1)
                    ? "<color=#90A4AE>🌋 Cataclysm\n<size=10>(Req 1 Core)</size></color>"
                    : (currentSelectedUnit.HasActedThisTurn
                        ? "<color=#90A4AE>🌋 Cataclysm\n<size=10>(Acted)</size></color>"
                        : $"🌋 Cataclysm\n<size=10><b>(Core: {currentSelectedUnit.ElementalCores})</b></size>");
                if (DrawOpaqueButton(ultRect, ultLabel,
                    new Color(0.80f, 0.15f, 0.12f, 1f), new Color(1.0f, 0.35f, 0.10f, 1f),
                    currentMode == UnitActionMode.MagmaCataclysm, canUlt))
                {
                    SetActionMode(currentMode == UnitActionMode.MagmaCataclysm ? UnitActionMode.None : UnitActionMode.MagmaCataclysm);
                }

                // TITAN UTILITY: Recall back to Citadel Reserve across the Rift
                Rect recallRect = new Rect(curX, btnY, 110f, btnH);
                curX += 110f + spacing;
                if (DrawOpaqueButton(recallRect, "🌀 Recall\n<size=10>(To Reserve)</size>",
                    new Color(0.35f, 0.15f, 0.55f, 1f), new Color(0.65f, 0.25f, 0.95f, 1f),
                    false, canAct))
                {
                    StartCoroutine(RecallUnitRoutine(currentSelectedUnit));
                }
            }
            else
            {
                // COMMANDER ABILITY: Fireball
                Rect fireRect = new Rect(curX, btnY, 90f, btnH);
                curX += 90f + spacing;
                string fireLabel = (currentSelectedUnit != null && currentSelectedUnit.HasActedThisTurn)
                    ? "<color=#90A4AE>🔥 Fireball\n<size=10>(Acted)</size></color>"
                    : "🔥 Fireball\n<size=10>(Dmg 3)</size>";
                if (DrawOpaqueButton(fireRect, fireLabel,
                    new Color(0.55f, 0.20f, 0.12f, 1f), new Color(0.90f, 0.35f, 0.10f, 1f),
                    currentMode == UnitActionMode.Fireball, canCombat))
                {
                    SetActionMode(currentMode == UnitActionMode.Fireball ? UnitActionMode.None : UnitActionMode.Fireball);
                }

                // COMMANDER ABILITY: Water Surge
                Rect waterRect = new Rect(curX, btnY, 90f, btnH);
                curX += 90f + spacing;
                string waterLabel = (currentSelectedUnit != null && currentSelectedUnit.HasActedThisTurn)
                    ? "<color=#90A4AE>💧 Water\n<size=10>(Acted)</size></color>"
                    : "💧 Water\n<size=10>(Dmg 2)</size>";
                if (DrawOpaqueButton(waterRect, waterLabel,
                    new Color(0.12f, 0.38f, 0.60f, 1f), new Color(0.20f, 0.60f, 0.95f, 1f),
                    currentMode == UnitActionMode.WaterSurge, canCombat))
                {
                    SetActionMode(currentMode == UnitActionMode.WaterSurge ? UnitActionMode.None : UnitActionMode.WaterSurge);
                }

                // COMMANDER ABILITY: Earth Spire
                Rect earthRect = new Rect(curX, btnY, 105f, btnH);
                curX += 105f + spacing;
                string earthLabel = (currentSelectedUnit != null && currentSelectedUnit.HasActedThisTurn)
                    ? "<color=#90A4AE>⛰️ Earth Spire\n<size=10>(Acted)</size></color>"
                    : "⛰️ Earth Spire\n<size=10>(Wall/Mud)</size>";
                if (DrawOpaqueButton(earthRect, earthLabel,
                    new Color(0.40f, 0.28f, 0.18f, 1f), new Color(0.70f, 0.52f, 0.30f, 1f),
                    currentMode == UnitActionMode.EarthSpire, canCombat))
                {
                    SetActionMode(currentMode == UnitActionMode.EarthSpire ? UnitActionMode.None : UnitActionMode.EarthSpire);
                }

                // COMMANDER ABILITY: Kinetic Push
                Rect pushRect = new Rect(curX, btnY, 90f, btnH);
                curX += 90f + spacing;
                string pushLabel = (currentSelectedUnit != null && currentSelectedUnit.HasActedThisTurn)
                    ? "<color=#90A4AE>💨 Push\n<size=10>(Acted)</size></color>"
                    : "💨 Push\n<size=10>(Shove 1)</size>";
                if (DrawOpaqueButton(pushRect, pushLabel,
                    new Color(0.38f, 0.18f, 0.52f, 1f), new Color(0.65f, 0.28f, 0.90f, 1f),
                    currentMode == UnitActionMode.KineticPush, canCombat))
                {
                    SetActionMode(currentMode == UnitActionMode.KineticPush ? UnitActionMode.None : UnitActionMode.KineticPush);
                }

                // COMMANDER ABILITY: Siphon Land
                bool canSiphon = canCombat && HasConsumableTilesNearby(currentSelectedUnit);
                Rect siphonRect = new Rect(curX, btnY, 110f, btnH);
                curX += 110f + spacing;
                string siphonLabel = (currentSelectedUnit != null && currentSelectedUnit.HasActedThisTurn)
                    ? "<color=#90A4AE>⚡ Siphon\n<size=10>(Acted)</size></color>"
                    : "⚡ Siphon\n<size=10>(+1 Core)</size>";
                if (DrawOpaqueButton(siphonRect, siphonLabel,
                    new Color(0.70f, 0.55f, 0.15f, 1f), new Color(1.0f, 0.82f, 0.20f, 1f),
                    currentMode == UnitActionMode.ConsumeLand, canSiphon))
                {
                    SetActionMode(currentMode == UnitActionMode.ConsumeLand ? UnitActionMode.None : UnitActionMode.ConsumeLand);
                }

                // COMMANDER ABILITY: Summon / Warp Titan through Abyssal Rift
                TacticalUnit3D titanUnit = FindTitanUnit();
                bool titanAvailable = titanUnit != null;
                bool titanOnField = titanUnit != null && titanUnit.gameObject.activeInHierarchy;
                bool canSummonTitan = canCombat &&
                                      AbyssalRiftConduit3D.Instance != null &&
                                      currentSelectedUnit != null &&
                                      currentSelectedUnit.ElementalCores >= 1 &&
                                      titanAvailable;

                Rect summonRect = new Rect(curX, btnY, 120f, btnH);
                curX += 120f + spacing;
                string summonLabel;
                if (currentSelectedUnit != null && currentSelectedUnit.ElementalCores < 1)
                {
                    summonLabel = "<color=#90A4AE>🌀 Rift Titan\n<size=10>(Req 1 Core)</size></color>";
                }
                else if (currentSelectedUnit != null && currentSelectedUnit.HasActedThisTurn)
                {
                    summonLabel = "<color=#90A4AE>🌀 Rift Titan\n<size=10>(Acted)</size></color>";
                }
                else if (titanOnField)
                {
                    summonLabel = "🌀 Warp Titan\n<size=10>(Redeploy & Slam)</size>";
                }
                else
                {
                    summonLabel = "🌀 Summon Titan\n<size=10>(Call from Rift)</size>";
                }

                if (DrawOpaqueButton(summonRect, summonLabel,
                    new Color(0.45f, 0.15f, 0.70f, 1f), new Color(0.75f, 0.25f, 1.0f, 1f),
                    currentMode == UnitActionMode.SummonTitan, canSummonTitan))
                {
                    SetActionMode(currentMode == UnitActionMode.SummonTitan ? UnitActionMode.None : UnitActionMode.SummonTitan);
                }

                // COMMANDER UTILITY: Recall back to Citadel Reserve across the Rift
                Rect cmdrRecallRect = new Rect(curX, btnY, 100f, btnH);
                curX += 100f + spacing;
                if (DrawOpaqueButton(cmdrRecallRect, "🌀 Recall\n<size=10>(To Citadel)</size>",
                    new Color(0.35f, 0.15f, 0.55f, 1f), new Color(0.65f, 0.25f, 0.95f, 1f),
                    false, canAct))
                {
                    StartCoroutine(RecallUnitRoutine(currentSelectedUnit));
                }
            }

            // 3. End Turn Button placed on far right
            float endTurnX = startX + barWidth - 110f - 10f;
            Rect endTurnRect = new Rect(endTurnX, btnY, 110f, btnH);
            if (DrawOpaqueButton(endTurnRect, "<b>END TURN</b>",
                new Color(0.65f, 0.15f, 0.15f, 1f), new Color(0.85f, 0.20f, 0.20f, 1f),
                false, isPlayerTurn))
            {
                if (TurnManager3D.Instance != null)
                {
                    DeselectAll();
                    TurnManager3D.Instance.EndPlayerTurn();
                }
            }
        }

        private void DrawGhostUIPreview()
        {
            if (currentHoveredTile == null ||
                (currentMode != UnitActionMode.Fireball && currentMode != UnitActionMode.WaterSurge && currentMode != UnitActionMode.EarthSpire) ||
                !activeTargetTiles.Contains(currentHoveredTile))
            {
                isGhostUIVisible = false;
                return;
            }

            isGhostUIVisible = true;
            ElementType spellElement = (currentMode == UnitActionMode.Fireball)
                ? ElementType.Fire
                : (currentMode == UnitActionMode.WaterSurge ? ElementType.Water : ElementType.Earth);
            TerrainReactionResult preview = TerrainReactionSystem.PredictReaction(currentHoveredTile.State, currentHoveredTile.TierLevel, spellElement);

            float w = 360f;
            float h = 115f;
            float x = Screen.width - w - 20f;
            float y = Screen.height - h - 85f;

            ghostUIRect = new Rect(x, y, w, h);

            Color panelBg = new Color(0.08f, 0.10f, 0.14f, 0.95f);
            Color borderColor = preview.TriggeredReaction ? new Color(1.0f, 0.75f, 0.2f, 1f) : new Color(0.3f, 0.7f, 0.4f, 1f);
            DrawSolidPanel(ghostUIRect, panelBg, borderColor, 2);

            GUILayout.BeginArea(new Rect(ghostUIRect.x + 12, ghostUIRect.y + 10, ghostUIRect.width - 24, ghostUIRect.height - 20));

            string titleColor = preview.TriggeredReaction ? "#FFD54F" : "#81C784";
            GUILayout.Label($"<size=13><color={titleColor}><b>⚡ GHOST UI: {preview.ReactionName}</b></color></size>");
            GUILayout.Label($"<b>Target:</b> {currentHoveredTile.Coordinates} | Current: {currentHoveredTile.State} (Tier {currentHoveredTile.TierLevel})");
            GUILayout.Label($"<b>Output:</b> <color=#FFA726>{preview.ResultingState} (Tier {preview.ResultingTier})</color>");
            GUILayout.Label($"<color=#B0BEC5><i>{preview.Description}</i></color>");

            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            if (solidWhiteTex != null) Destroy(solidWhiteTex);
        }
    }
}
