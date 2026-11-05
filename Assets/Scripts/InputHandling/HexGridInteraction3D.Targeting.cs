using System.Collections.Generic;
using UnityEngine;
using ElementalHexTactics3D.Grid;
using ElementalHexTactics3D.Units;
using ElementalHexTactics3D.Combat;

namespace ElementalHexTactics3D.InputHandling
{
    public partial class HexGridInteraction3D
    {
        public void SetActionModeWithSkill(UnitActionMode mode, string skillId, int customDamage, int manaCost)
        {
            SetActionMode(mode);
            PendingSkillId = skillId;
            PendingSkillDamage = customDamage;
            PendingSkillManaCost = manaCost;
        }

        public void SetActionMode(UnitActionMode mode)
        {
            currentMode = mode;
            PendingSkillId = null;
            PendingSkillDamage = -1;
            PendingSkillManaCost = 0;
            ClearTargetHighlights();

            bool isDeployMode = (mode == UnitActionMode.DeployCommander || mode == UnitActionMode.SummonTitan || mode == UnitActionMode.TearRift);
            if (!isDeployMode && currentSelectedUnit == null) return;
            if (HexGrid3D.Instance == null) return;

            switch (mode)
            {
                case UnitActionMode.Move:
                    HighlightMoveTargets();
                    break;

                case UnitActionMode.Fireball:
                case UnitActionMode.WaterSurge:
                case UnitActionMode.EarthSpire:
                    HighlightTilesInRange(currentSelectedUnit.Coordinates, 3);
                    break;

                case UnitActionMode.KineticPush:
                    HighlightNeighborTiles(currentSelectedUnit.Coordinates);
                    break;

                case UnitActionMode.TitanStrike:
                    int strikeRange = (currentSelectedUnit != null) ? currentSelectedUnit.AttackRange : 1;
                    HighlightTilesInRange(currentSelectedUnit.Coordinates, strikeRange, excludeCenter: true);
                    break;

                case UnitActionMode.ConsumeLand:
                    HighlightConsumableTerrainTargets();
                    break;

                case UnitActionMode.MagmaCataclysm:
                    HighlightTilesInRange(currentSelectedUnit.Coordinates, 2);
                    break;

                case UnitActionMode.SummonTitan:
                case UnitActionMode.DeployCommander:
                    HighlightRiftSummonTargets();
                    break;

                case UnitActionMode.TearRift:
                    HighlightTearRiftCandidates();
                    break;
            }
        }

        private void HighlightMoveTargets()
        {
            if (currentSelectedUnit.HasMovedThisTurn)
            {
                Debug.Log($"[Move Mode] {currentSelectedUnit.UnitName} has already moved this turn.");
                return;
            }

            if (currentSelectedUnit.IsImmobilized || currentSelectedUnit.EffectiveMoveRange <= 0)
            {
                Debug.Log($"<color=#FF7043><b>[Move Mode]</b></color> {currentSelectedUnit.UnitName} is immobilized and cannot move!");
                return;
            }

            HexTile3D startTile = ResolveUnitCurrentTile(currentSelectedUnit);
            if (startTile == null)
            {
                Debug.LogWarning($"[Move Mode] Could not resolve CurrentTile for {currentSelectedUnit.UnitName} at {currentSelectedUnit.Coordinates}");
                return;
            }

            int effectiveRange = currentSelectedUnit.EffectiveMoveRange;
            var reachable = HexPathfinder3D.GetReachableTiles(HexGrid3D.Instance, startTile, effectiveRange);
            Debug.Log($"<color=#00E5FF><b>[Move Mode]</b></color> Unit: {currentSelectedUnit.UnitName} at {startTile.Coordinates}, EffectiveRange: {effectiveRange} (Base: {currentSelectedUnit.MoveRange}), Found {reachable.Count} reachable tiles.");

            foreach (var tile in reachable)
            {
                MarkTileAsReachable(tile);
            }
        }

        private void HighlightTilesInRange(HexCoordinates center, int range, bool excludeCenter = false)
        {
            var tiles = HexGrid3D.Instance.GetTilesInRange(center, range);
            foreach (var tile in tiles)
            {
                if (excludeCenter && currentSelectedUnit != null && tile == currentSelectedUnit.CurrentTile)
                    continue;

                MarkTileAsReachable(tile);
            }
        }

        private void HighlightNeighborTiles(HexCoordinates center)
        {
            var adjTiles = HexGrid3D.Instance.GetNeighbors(center);
            foreach (var tile in adjTiles)
            {
                MarkTileAsReachable(tile);
            }
        }

        private void HighlightConsumableTerrainTargets()
        {
            // Highlight unit's current tile and neighbors if they have active elements
            if (currentSelectedUnit.CurrentTile != null && IsTileConsumable(currentSelectedUnit.CurrentTile))
            {
                MarkTileAsReachable(currentSelectedUnit.CurrentTile);
            }

            var harvestNeighbors = HexGrid3D.Instance.GetNeighbors(currentSelectedUnit.Coordinates);
            foreach (var tile in harvestNeighbors)
            {
                if (tile != null && IsTileConsumable(tile))
                {
                    MarkTileAsReachable(tile);
                }
            }
        }

        private void HighlightRiftSummonTargets()
        {
            if (AbyssalRiftConduit3D.Instance == null || HexGrid3D.Instance == null) return;

            var validSummonTiles = AbyssalRiftConduit3D.Instance.GetValidSummonTiles(HexGrid3D.Instance);
            foreach (var tile in validSummonTiles)
            {
                MarkTileAsReachable(tile);
            }
        }

        private void HighlightTearRiftCandidates()
        {
            if (HexGrid3D.Instance == null) return;

            var candidateTiles = AbyssalRiftConduit3D.GetCandidateRiftTiles(HexGrid3D.Instance);
            Debug.Log($"<color=#BA68C8><b>[Tear Rift Mode]</b></color> Found {candidateTiles.Count} candidate landing hexes across battlefield.");

            foreach (var tile in candidateTiles)
            {
                MarkTileAsReachable(tile);
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

        private void MarkTileAsReachable(HexTile3D tile)
        {
            if (tile == null) return;
            activeTargetTiles.Add(tile);
            tile.SetReachable(true);
        }

        private HexTile3D ResolveUnitCurrentTile(TacticalUnit3D unit)
        {
            if (unit == null) return null;
            if (unit.CurrentTile == null)
            {
                unit.ReacquireCurrentTile();
            }
            return unit.CurrentTile;
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
    }
}
