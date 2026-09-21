using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ElementalHexTactics3D.Grid;
using ElementalHexTactics3D.Units;
using ElementalHexTactics3D.CameraControl;

namespace ElementalHexTactics3D.Combat
{
    /// <summary>
    /// Interactive Abyssal Rift Conduit on the tactical 3D hex battlefield.
    /// Acts as the dimensional bridge back to the Citadel of the Outcasts.
    /// Features:
    /// 1. Radial Vanguard Summoning (Range 1-2 around Rift - no Disgaea single-tile bottlenecks).
    /// 2. The Kinetic Sacrifice Maw: Enemies pushed into the Rift are consumed, executed, and award +1 Elemental Core.
    /// 3. Mid-Battle Titan Eruption: Spending an Elemental Core summons the Titan with a radial kinetic shockwave.
    /// </summary>
    public class AbyssalRiftConduit3D : MonoBehaviour
    {
        public static AbyssalRiftConduit3D Instance { get; private set; }

        [Header("Rift Landmark Settings")]
        [SerializeField] private HexTile3D attachedTile;
        [SerializeField] private int summonRadius = 2;
        [SerializeField] private Color riftGlowColor = new Color(0.65f, 0.1f, 1.0f, 1.0f); // Radiant Violet
        [SerializeField] private bool autoAttachVisual = true;

        private GameObject ambientVortexObj;

        public HexTile3D AttachedTile => attachedTile;
        public HexTile3D RiftTile => attachedTile;
        public HexCoordinates Coordinates => attachedTile != null ? attachedTile.Coordinates : default;
        public int SummonRadius => summonRadius;
        public int TotalSacrifices { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this)
            {
                Destroy(this);
                return;
            }

            if (attachedTile == null)
            {
                attachedTile = GetComponent<HexTile3D>() ?? GetComponentInParent<HexTile3D>();
            }
        }

        private void Start()
        {
            if (attachedTile == null)
            {
                attachedTile = GetComponent<HexTile3D>() ?? GetComponentInParent<HexTile3D>();
            }

            if (autoAttachVisual && ambientVortexObj == null && attachedTile != null)
            {
                CreateRiftVisuals();
            }
        }

        public void Initialize(HexTile3D tile, int radius = 2)
        {
            attachedTile = tile;
            summonRadius = radius;
            CreateRiftVisuals();
        }

        /// <summary>
        /// Dynamically tears open the Abyssal Rift Conduit on targetTile, establishing the Citadel Gateway.
        /// </summary>
        public static AbyssalRiftConduit3D OpenRiftAt(HexTile3D targetTile, int radius = 2)
        {
            if (targetTile == null) return null;

            AbyssalRiftConduit3D conduit = Instance;
            if (conduit != null)
            {
                conduit.RelocateTo(targetTile, radius);
            }
            else
            {
                conduit = targetTile.gameObject.AddComponent<AbyssalRiftConduit3D>();
                conduit.Initialize(targetTile, radius);
            }

            Vector3 spawnPos = targetTile.GetTopCenterPosition();

            // Dimensional Breach VFX & Feedback
            TacticalCameraController.Instance?.Shake(0.5f, 0.45f);
            SoundManager3D.Instance?.PlaySpellCast(false);
            CombatVFXManager.Instance?.PlayRiftSacrifice(spawnPos);
            CombatFeedbackManager.Instance?.SpawnDamageText(spawnPos + Vector3.up * 1.2f, "🌀 ABYSSAL RIFT TORN OPEN!", new Color(0.7f, 0.2f, 1.0f), 2.2f);
            CombatFeedbackManager.Instance?.ShowBanner("🌀 DIMENSIONAL BREACH", $"Citadel Rift Gateway anchored at {targetTile.Coordinates}!", 1.5f, new Color(0.6f, 0.2f, 0.95f));

            return conduit;
        }

        /// <summary>
        /// Relocates the conduit and its visuals to a new tile.
        /// </summary>
        public void RelocateTo(HexTile3D newTile, int radius = 2)
        {
            if (newTile == null) return;
            attachedTile = newTile;
            summonRadius = radius;
            transform.position = newTile.transform.position;
            CreateRiftVisuals();
        }

        /// <summary>
        /// Returns all candidate hex tiles across the battlefield where the player can choose to tear open the Rift.
        /// Valid tiles are unoccupied and not impassable Stone Pillars.
        /// </summary>
        public static List<HexTile3D> GetCandidateRiftTiles(HexGrid3D grid)
        {
            List<HexTile3D> candidates = new List<HexTile3D>();
            if (grid == null) return candidates;

            foreach (var kvp in grid.Tiles)
            {
                HexTile3D tile = kvp.Value;
                if (tile == null) continue;
                if (tile.State == TileState.StonePillar) continue;

                TacticalUnit3D occ = tile.GetOccupant();
                if (occ != null && occ.gameObject.activeInHierarchy && occ.CurrentHealth > 0) continue;

                candidates.Add(tile);
            }

            return candidates;
        }

        /// <summary>
        /// Creates a procedural glowing vortex and rune circle above the tile without external assets.
        /// </summary>
        public void CreateRiftVisuals()
        {
            if (attachedTile == null) return;
            if (ambientVortexObj != null) SafeDestroy(ambientVortexObj);

            ambientVortexObj = new GameObject("VFX_AbyssalRift_Portal");
            ambientVortexObj.layer = LayerMask.NameToLayer("Ignore Raycast");
            ambientVortexObj.transform.SetParent(attachedTile.transform, false);
            ambientVortexObj.transform.position = attachedTile.GetTopCenterPosition() + Vector3.up * 0.05f;

            // 1. Procedural Particle Vortex
            if (CombatVFXManager.Instance != null)
            {
                CombatVFXManager.Instance.AttachRiftVortex(ambientVortexObj.transform);
            }

            // 2. Glowing base disc (quad)
            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Quad);
            disc.name = "RiftFloorDisc";
            disc.layer = LayerMask.NameToLayer("Ignore Raycast");
            disc.transform.SetParent(ambientVortexObj.transform, false);
            disc.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            disc.transform.localScale = Vector3.one * 1.6f;

            // Remove collider from visual quad so clicks pass straight to the tile
            Collider c = disc.GetComponent<Collider>();
            if (c != null) SafeDestroy(c);

            MeshRenderer mr = disc.GetComponent<MeshRenderer>();
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            Material mat = new Material(unlit);
            mat.color = new Color(0.45f, 0.05f, 0.85f, 0.75f);
            mr.material = mat;
        }

        private static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying)
            {
                Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj);
            }
        }

        /// <summary>
        /// Checks if a destination tile is this Abyssal Rift.
        /// </summary>
        public bool IsRiftTile(HexTile3D tile)
        {
            return attachedTile != null && tile != null && tile == attachedTile;
        }

        /// <summary>
        /// Checks if an enemy pushed onto destTile is valid to be sacrificed.
        /// </summary>
        public bool CanSacrificeUnit(TacticalUnit3D unit, HexTile3D destTile)
        {
            return IsRiftTile(destTile) && unit != null && unit.Faction == UnitFaction.Enemy;
        }

        /// <summary>
        /// The Kinetic Sacrifice Maw: Consumes an enemy shoved into the Rift, executes them,
        /// plays void implosion VFX, shakes the camera, and awards an Elemental Core.
        /// </summary>
        public IEnumerator ConsumeSacrificeRoutine(TacticalUnit3D enemy, TacticalUnit3D pusher)
        {
            if (enemy == null) yield break;

            Vector3 pos = (attachedTile != null) ? attachedTile.GetTopCenterPosition() : enemy.transform.position;

            Debug.Log($"<color=#9C27B0><b>[Abyssal Sacrifice!]</b></color> {enemy.UnitName} was shoved into the Abyssal Rift and consumed by the void!");

            // 1. Audio & Camera Shake
            TacticalCameraController.Instance?.Shake(0.45f, 0.45f);
            SoundManager3D.Instance?.PlaySlam(1.5f);

            // 2. Procedural Void Implosion VFX
            CombatVFXManager.Instance?.PlayRiftSacrifice(pos);

            // 3. Floating Text Announcement
            CombatFeedbackManager.Instance?.SpawnDamageText(pos + Vector3.up * 1.0f, "🌀 VOID SACRIFICE! (+1 Core)", new Color(0.7f, 0.2f, 1.0f), 2.0f);

            // 4. Award Elemental Core to player Commander
            TotalSacrifices++;
            TacticalUnit3D playerCommander = FindPlayerCommander(pusher);
            if (playerCommander != null)
            {
                playerCommander.AddElementalCore(1);
            }

            yield return new WaitForSeconds(0.2f);

            // 5. Fatal Execution
            enemy.TakeDamage(999, "🌀 CONSUMED BY THE VOID!");
        }

        private TacticalUnit3D FindPlayerCommander(TacticalUnit3D fallback)
        {
            if (fallback != null && fallback.Faction == UnitFaction.Player && fallback.Archetype == UnitArchetype.Commander)
                return fallback;

            TacticalUnit3D[] all = FindObjectsByType<TacticalUnit3D>(FindObjectsSortMode.None);
            foreach (var u in all)
            {
                if (u.Faction == UnitFaction.Player && u.Archetype == UnitArchetype.Commander)
                    return u;
            }
            return fallback;
        }

        /// <summary>
        /// Retrieves all unoccupied, walkable hex tiles in the radial deployment zone around the Rift.
        /// </summary>
        public List<HexTile3D> GetValidSummonTiles(HexGrid3D grid)
        {
            List<HexTile3D> validTiles = new List<HexTile3D>();
            if (attachedTile == null || grid == null) return validTiles;

            HexCoordinates origin = attachedTile.Coordinates;
            List<HexCoordinates> coordsInRange = origin.GetRange(summonRadius);

            foreach (var coord in coordsInRange)
            {
                HexTile3D tile = grid.GetTile(coord);
                if (tile == null) continue;
                if (tile == attachedTile) continue; // Don't block the rift itself
                
                TacticalUnit3D occ = tile.GetOccupant();
                if (occ != null && occ.gameObject.activeInHierarchy && occ.CurrentHealth > 0) continue;
                if (tile.State == TileState.StonePillar) continue;

                validTiles.Add(tile);
            }

            return validTiles;
        }

        /// <summary>
        /// Deploys any ally (Commander or Titan) from the Citadel across the Abyssal Rift onto targetTile.
        /// </summary>
        public IEnumerator ExecuteDeployUnitRoutine(TacticalUnit3D unit, HexTile3D targetTile, HexGrid3D grid)
        {
            if (unit == null || targetTile == null || grid == null) yield break;

            if (unit.Archetype == UnitArchetype.Titan)
            {
                yield return ExecuteTitanSummonRoutine(unit, targetTile, grid);
                yield break;
            }

            Vector3 spawnPos = targetTile.GetTopCenterPosition();

            // Clear previous tile occupancy if unit was already on the board
            if (unit.CurrentTile != null && unit.CurrentTile.CurrentOccupant == unit)
            {
                unit.CurrentTile.CurrentOccupant = null;
            }

            // 1. Activate, snap to destination, and revive
            unit.gameObject.SetActive(true);
            unit.SnapToTile(targetTile);
            unit.Revive();

            Debug.Log($"<color=#BA68C8><b>[Vanguard Arrival!]</b></color> {unit.UnitName} emerged from the Abyssal Rift onto {targetTile.Coordinates}!");

            // 2. Camera shock & VFX
            TacticalCameraController.Instance?.Shake(0.35f, 0.35f);
            SoundManager3D.Instance?.PlaySpellCast(false);
            CombatVFXManager.Instance?.PlayRiftSacrifice(spawnPos);
            CombatFeedbackManager.Instance?.SpawnDamageText(spawnPos + Vector3.up * 1.2f, "👑 COMMANDER ARRIVED!", new Color(0.7f, 0.3f, 1.0f), 2.0f);

            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.ShowBanner("👑 VANGUARD DEPLOYED", $"{unit.UnitName} has stepped onto the mortal plane!", 1.3f, new Color(0.6f, 0.3f, 0.9f));
            }

            yield return new WaitForSeconds(0.2f);
        }

        /// <summary>
        /// Recalls a unit safely back to the Citadel Reserve across the Rift.
        /// </summary>
        public IEnumerator RecallUnitRoutine(TacticalUnit3D unit)
        {
            if (unit == null) yield break;

            Vector3 pos = unit.transform.position;
            SoundManager3D.Instance?.PlaySpellCast(false);
            CombatVFXManager.Instance?.PlayRiftSacrifice(pos);
            CombatFeedbackManager.Instance?.SpawnDamageText(pos + Vector3.up * 1.0f, "🌀 RECALLED TO CITADEL", new Color(0.7f, 0.4f, 1.0f), 1.5f);

            if (unit.CurrentTile != null && unit.CurrentTile.CurrentOccupant == unit)
            {
                unit.CurrentTile.CurrentOccupant = null;
            }
            unit.CurrentTile = null;

            yield return new WaitForSeconds(0.2f);
            unit.gameObject.SetActive(false);
        }

        /// <summary>
        /// Summons a reserve unit (like the Colossal Titan) onto targetTile, triggering a radial kinetic shockwave!
        /// </summary>
        public IEnumerator ExecuteTitanSummonRoutine(TacticalUnit3D titan, HexTile3D targetTile, HexGrid3D grid)
        {
            if (titan == null || targetTile == null || grid == null) yield break;

            Vector3 spawnPos = targetTile.GetTopCenterPosition();

            // Clear previous tile occupancy if Titan was already on the board
            if (titan.CurrentTile != null && titan.CurrentTile.CurrentOccupant == titan)
            {
                titan.CurrentTile.CurrentOccupant = null;
            }

            // 1. Activate, snap to new tile, and revive if returning from reserve
            titan.gameObject.SetActive(true);
            titan.SnapToTile(targetTile);
            titan.Revive();

            Debug.Log($"<color=#FF5722><b>[Titan Emergence!]</b></color> {titan.UnitName} burst through the Abyssal Rift onto {targetTile.Coordinates}!");

            // 2. Camera shock & VFX
            TacticalCameraController.Instance?.Shake(0.6f, 0.5f);
            SoundManager3D.Instance?.PlaySlam(1.6f);
            CombatVFXManager.Instance?.PlayTitanShockwave(spawnPos);
            CombatFeedbackManager.Instance?.SpawnDamageText(spawnPos + Vector3.up * 1.5f, "🌋 TITAN EMERGENCE!", new Color(1f, 0.4f, 0f), 2.2f);

            yield return new WaitForSeconds(0.25f);

            // 3. Radial Kinetic Shockwave: shove all adjacent enemies outward 1 hex!
            var neighbors = targetTile.Coordinates.GetNeighbors();
            foreach (var nCoord in neighbors)
            {
                HexTile3D neighborTile = grid.GetTile(nCoord);
                if (neighborTile == null || !neighborTile.IsOccupied) continue;

                TacticalUnit3D occupant = neighborTile.GetOccupant();
                if (occupant != null && occupant.Faction == UnitFaction.Enemy)
                {
                    PushMechanic3D.ExecutePush(titan, occupant, grid, this);
                }
            }
        }
    }
}
