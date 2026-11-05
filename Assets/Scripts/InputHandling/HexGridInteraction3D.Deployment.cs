using System.Collections;
using UnityEngine;
using ElementalHexTactics3D.Grid;
using ElementalHexTactics3D.Units;
using ElementalHexTactics3D.Combat;

namespace ElementalHexTactics3D.InputHandling
{
    public partial class HexGridInteraction3D
    {
        public static TacticalUnit3D FindCommanderUnit()
        {
            TacticalUnit3D[] all = Object.FindObjectsByType<TacticalUnit3D>(FindObjectsInactive.Include);
            foreach (var u in all)
            {
                if (u.Faction == UnitFaction.Player && u.Archetype == UnitArchetype.Commander)
                    return u;
            }
            return null;
        }

        public static TacticalUnit3D FindTitanUnit()
        {
            TacticalUnit3D[] all = Object.FindObjectsByType<TacticalUnit3D>(FindObjectsInactive.Include);
            foreach (var u in all)
            {
                if (u.Faction == UnitFaction.Player && u.Archetype == UnitArchetype.Titan)
                    return u;
            }
            return null;
        }

        public static bool HasActivePlayerUnitsOnField()
        {
            TacticalUnit3D[] all = Object.FindObjectsByType<TacticalUnit3D>();
            foreach (var u in all)
            {
                if (u.Faction == UnitFaction.Player && u.gameObject.activeInHierarchy)
                    return true;
            }
            return false;
        }

        private void TriggerDeployCommander(HexTile3D targetTile)
        {
            if (AbyssalRiftConduit3D.Instance == null || HexGrid3D.Instance == null) return;

            TacticalUnit3D cmdr = (PendingDeployUnit != null) ? PendingDeployUnit : FindCommanderUnit();
            if (cmdr == null) return;

            ClearTargetHighlights();
            StartCoroutine(ExecuteDeployCommanderRoutine(cmdr, targetTile));
            PendingDeployUnit = null;
            SetActionMode(UnitActionMode.None);
        }

        private void TriggerSummonTitan(HexTile3D targetTile)
        {
            if (AbyssalRiftConduit3D.Instance == null || HexGrid3D.Instance == null) return;

            TacticalUnit3D titan = (PendingDeployUnit != null) ? PendingDeployUnit : FindTitanUnit();
            TacticalUnit3D cmdr = FindCommanderUnit();
            if (titan == null) return;

            if (cmdr != null && cmdr.gameObject.activeInHierarchy)
            {
                if (!cmdr.ConsumeElementalCore(1)) return;
                cmdr.HasActedThisTurn = true;
            }

            ClearTargetHighlights();
            StartCoroutine(ExecuteDeployTitanRoutine(titan, targetTile));
            PendingDeployUnit = null;
            SetActionMode(UnitActionMode.None);
        }

        private void TriggerTearRift(HexTile3D targetTile)
        {
            if (HexGrid3D.Instance == null) return;

            ClearTargetHighlights();
            StartCoroutine(ExecuteTearRiftAndDeployRoutine(targetTile));
            SetActionMode(UnitActionMode.None);
        }

        private IEnumerator ExecuteTearRiftAndDeployRoutine(HexTile3D targetTile)
        {
            if (targetTile == null || HexGrid3D.Instance == null) yield break;

            // 1. Tear open the Abyssal Rift Conduit on the chosen tile!
            AbyssalRiftConduit3D.OpenRiftAt(targetTile, radius: 2);
            SelectTile(targetTile);
            ClearUnitSelection();
            SetActionMode(UnitActionMode.None);

            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.ShowBanner(
                    "🌀 ABYSSAL RIFT ESTABLISHED",
                    "Gateway opened! Click the Rift or choose a Vanguard from the bar below.",
                    2.2f,
                    new Color(0.65f, 0.25f, 0.95f)
                );
            }

            yield return null;
        }

        private IEnumerator ExecuteDeployCommanderRoutine(TacticalUnit3D cmdr, HexTile3D targetTile)
        {
            if (cmdr == null || targetTile == null || AbyssalRiftConduit3D.Instance == null) yield break;

            yield return AbyssalRiftConduit3D.Instance.ExecuteDeployUnitRoutine(cmdr, targetTile, HexGrid3D.Instance);

            cmdr.ResetTurnActions();

            // In Stage 2 tutorial, also deploy Basalt Vanguard through the rift onto the rift landing tile!
            yield return TryAutoDeployTutorialBasaltVanguard();

            SelectTile(targetTile);
            SelectUnit(cmdr);
        }

        private IEnumerator TryAutoDeployTutorialBasaltVanguard()
        {
            if (Tutorial.TutorialScenarioManager.Instance == null || !Tutorial.TutorialScenarioManager.Instance.IsStage2Active)
                yield break;

            var basalt = Tutorial.TutorialScenarioManager.Instance.BasaltVanguardPlayer;
            if (basalt == null || basalt.gameObject.activeInHierarchy)
                yield break;

            HexTile3D basaltTile = FindTutorialBasaltLandingTile(basalt);
            if (basaltTile != null)
            {
                yield return AbyssalRiftConduit3D.Instance.ExecuteDeployUnitRoutine(basalt, basaltTile, HexGrid3D.Instance);
                basalt.ResetTurnActions();
            }
        }

        private HexTile3D FindTutorialBasaltLandingTile(TacticalUnit3D basalt)
        {
            var riftTile = AbyssalRiftConduit3D.Instance.RiftTile;
            if (riftTile != null && (riftTile.CurrentOccupant == null || riftTile.CurrentOccupant == basalt))
            {
                return riftTile;
            }

            if (HexGrid3D.Instance == null) return null;

            HexTile3D fallbackTile = HexGrid3D.Instance.GetTile(new HexCoordinates(0, -2));
            if (fallbackTile == null || (fallbackTile.IsOccupied && fallbackTile.CurrentOccupant != basalt))
            {
                fallbackTile = HexGrid3D.Instance.GetTile(new HexCoordinates(1, -2));
            }
            return fallbackTile;
        }

        private IEnumerator ExecuteDeployTitanRoutine(TacticalUnit3D titan, HexTile3D targetTile)
        {
            if (titan == null || targetTile == null || AbyssalRiftConduit3D.Instance == null) yield break;

            yield return AbyssalRiftConduit3D.Instance.ExecuteTitanSummonRoutine(titan, targetTile, HexGrid3D.Instance);

            titan.ResetTurnActions();
            SelectTile(targetTile);
            SelectUnit(titan);
        }

        private IEnumerator RecallUnitRoutine(TacticalUnit3D unit)
        {
            if (unit == null || AbyssalRiftConduit3D.Instance == null) yield break;
            DeselectAll();
            yield return AbyssalRiftConduit3D.Instance.RecallUnitRoutine(unit);
        }
    }
}
