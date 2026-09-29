using System.Collections.Generic;
using UnityEngine;
using ElementalHexTactics3D.Campaign;
using ElementalHexTactics3D.Units;
using ElementalHexTactics3D.Combat;
using ElementalHexTactics3D.CameraControl;
using ElementalHexTactics3D.Turn;

namespace ElementalHexTactics3D.Grid
{
    /// <summary>
    /// Master grid manager that procedurally generates and tracks the 3D hexagonal battlefield.
    /// Manages the elemental material library so every terrain state displays its authentic square texture.
    /// </summary>
    public class HexGrid3D : MonoBehaviour
    {
        public static HexGrid3D Instance { get; private set; }

        [Header("Grid Dimensions")]
        [SerializeField] private int gridRadius = 4;
        [SerializeField] private float hexRadius = 1.0f;
        [SerializeField] private float elevationHeight = 0.6f;
        [SerializeField] private float pillarDepth = 1.2f;

        [Header("Elemental Materials Library (Square Textures)")]
        [SerializeField] private Material barrenMaterial;
        [SerializeField] private Material grassMaterial;
        [SerializeField] private Material scorchedMaterial;
        [SerializeField] private Material magmaMaterial;
        [SerializeField] private Material shallowWaterMaterial;
        [SerializeField] private Material deepWaterMaterial;
        [SerializeField] private Material steamMaterial;
        [SerializeField] private Material sidePillarMaterial;

        [Header("Initial Elevation & Biome Settings")]
        [SerializeField] private bool generateCenterPlateau = true;
        [SerializeField] private bool generateSampleBiomes = true;

        private readonly Dictionary<HexCoordinates, HexTile3D> tiles = new Dictionary<HexCoordinates, HexTile3D>();
        private Mesh sharedPillarMesh;

        public int GridRadius => gridRadius;
        public float HexRadius => hexRadius;
        public float ElevationHeight => elevationHeight;
        public IReadOnlyDictionary<HexCoordinates, HexTile3D> Tiles => tiles;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            RegisterExistingTiles();
        }

        private void Start()
        {
            if (tiles.Count == 0)
            {
                RegisterExistingTiles();
            }

            if (tiles.Count == 0)
            {
                GenerateGrid();
            }
        }

        /// <summary>
        /// Ensures tile (0, -3) hosts the Abyssal Rift Conduit gateway back to the Citadel.
        /// </summary>
        public void EnsureAbyssalRiftConduit()
        {
            if (Combat.AbyssalRiftConduit3D.Instance != null) return;

            HexCoordinates riftCoord = new HexCoordinates(0, -3);
            if (tiles.TryGetValue(riftCoord, out HexTile3D riftTile))
            {
                var conduit = riftTile.GetComponent<Combat.AbyssalRiftConduit3D>();
                if (conduit == null)
                {
                    conduit = riftTile.gameObject.AddComponent<Combat.AbyssalRiftConduit3D>();
                }
                conduit.Initialize(riftTile, 2);
            }
        }

        /// <summary>
        /// Registers existing HexTile3D children already in the scene, preventing accidental destruction on play.
        /// </summary>
        public void RegisterExistingTiles()
        {
            tiles.Clear();
            HexTile3D[] existingTiles = GetComponentsInChildren<HexTile3D>(true);
            foreach (var tile in existingTiles)
            {
                if (tile != null)
                {
                    tiles[tile.Coordinates] = tile;
                }
            }
            if (tiles.Count > 0)
            {
                Debug.Log($"<color=#4CAF50><b>[HexGrid3D]</b></color> Registered {tiles.Count} existing tiles from scene.");
            }
        }

        public Material GetTopMaterial(TileState state, int tier = 1)
        {
            switch (state)
            {
                case TileState.Barren:
                    return barrenMaterial;
                case TileState.Grass:
                    return grassMaterial ?? barrenMaterial;
                case TileState.Scorched:
                    return scorchedMaterial ?? barrenMaterial;
                case TileState.Magma:
                    return magmaMaterial ?? scorchedMaterial ?? barrenMaterial;
                case TileState.Water:
                    return (tier >= 2) ? (deepWaterMaterial ?? shallowWaterMaterial ?? barrenMaterial)
                                       : (shallowWaterMaterial ?? barrenMaterial);
                case TileState.Steam:
                    return steamMaterial ?? barrenMaterial;
                case TileState.Mud:
                case TileState.StonePillar:
                    return barrenMaterial;
                default:
                    return barrenMaterial;
            }
        }

        [ContextMenu("Generate Randomized Battlefield")]
        public void GenerateRandomizedBattlefield(ExpeditionMissionData mission = null, int seed = 0)
        {
            if (mission == null)
            {
                if (seed == 0) seed = UnityEngine.Random.Range(1000, 99999);
                mission = ExpeditionTrilemmaGenerator.GenerateQuickSkirmish(seed);
            }
            else if (seed != 0)
            {
                mission.Seed = seed;
            }

            ExpeditionTrilemmaGenerator.CurrentActiveMission = mission;

            ClearGrid();

            // Clear active rift conduit if one existed
            if (AbyssalRiftConduit3D.Instance != null)
            {
                Destroy(AbyssalRiftConduit3D.Instance);
            }

            // Generate or cache procedural 3D hex pillar mesh
            if (sharedPillarMesh == null)
            {
                sharedPillarMesh = HexMeshBuilder.CreateHexPillarMesh(hexRadius, pillarDepth);
            }

            GeneratedBattlefieldData data = HexBattlefieldGenerator.Generate(mission, gridRadius);

            GameObject container = new GameObject("Tiles_Container");
            container.transform.SetParent(transform, false);

            foreach (var kvp in data.Tiles)
            {
                var coord = kvp.Key;
                var spec = kvp.Value;

                Vector3 worldPos = coord.ToWorldPosition(hexRadius, elevationHeight, spec.ElevationTier);

                GameObject tileObj = new GameObject($"HexTile_{coord.Q}_{coord.R}");
                tileObj.transform.SetParent(container.transform, false);
                tileObj.transform.position = worldPos;

                HexTile3D tile = tileObj.AddComponent<HexTile3D>();
                Material topMat = GetTopMaterial(spec.State, spec.TierLevel);
                tile.Initialize(coord, spec.ElevationTier, sharedPillarMesh, topMat, sidePillarMaterial);
                tile.SetState(spec.State, spec.TierLevel);

                tiles[coord] = tile;
            }

            // Clear previous enemies and spawn new procedural squad
            TacticalUnitSpawner.ClearAllEnemies();
            foreach (var enemySpec in data.Enemies)
            {
                if (tiles.TryGetValue(enemySpec.Coordinates, out HexTile3D eTile))
                {
                    Sprite sSprite = TacticalUnitSpawner.LoadBattlerSprite(enemySpec.BattlerSpriteName);
                    TacticalUnitSpawner.SpawnUnitStandee(
                        enemySpec.UnitName,
                        UnitFaction.Enemy,
                        sSprite,
                        eTile,
                        enemySpec.HP,
                        enemySpec.Range,
                        enemySpec.Archetype,
                        enemySpec.Affinity,
                        enemySpec.BaseAtk,
                        enemySpec.Scale
                    );
                }
            }

            // Reset player units to Citadel reserve across the Rift
            TacticalUnitSpawner.ResetPlayerReserveUnits();

            // Reset Turn Manager to Round 1, Player Turn
            if (TurnManager3D.Instance != null)
            {
                TurnManager3D.Instance.ResetBattleState();
            }

            // Reset Camera to framing view
            if (TacticalCameraController.Instance != null)
            {
                TacticalCameraController.Instance.ResetToTacticalView();
            }

            // Announce mission briefing banner
            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.ShowBanner(
                    $"✦ {mission.Title.ToUpper()} ✦",
                    $"{mission.GetArchetypeName()} ({mission.GetThreatStars()}) | {mission.GetModifierTag()}\nSurvey the battlefield, then click [Tear Open Abyssal Rift]!",
                    3.5f,
                    new Color(0.35f, 0.85f, 1.0f)
                );
            }

            Debug.Log($"<color=#4CAF50><b>[HexGrid3D]</b></color> Procedural battlefield initialized: {mission.Title} (Seed: {mission.Seed}, Tiles: {tiles.Count}, Enemies: {data.Enemies.Count}).");
        }

        [ContextMenu("Regenerate Grid")]
        public void GenerateGrid()
        {
            ClearGrid();

            // Generate or cache procedural 3D hex pillar mesh with edge-to-edge square UV mapping
            if (sharedPillarMesh == null)
            {
                sharedPillarMesh = HexMeshBuilder.CreateHexPillarMesh(hexRadius, pillarDepth);
            }

            HexCoordinates origin = new HexCoordinates(0, 0);
            List<HexCoordinates> allCoords = origin.GetRange(gridRadius);

            GameObject container = new GameObject("Tiles_Container");
            container.transform.SetParent(transform, false);

            foreach (var coord in allCoords)
            {
                int elev = 0;
                if (generateCenterPlateau)
                {
                    int dist = coord.DistanceTo(origin);
                    if (dist <= 1) elev = 2;
                    else if (dist <= 2) elev = 1;
                }

                Vector3 worldPos = coord.ToWorldPosition(hexRadius, elevationHeight, elev);

                GameObject tileObj = new GameObject($"HexTile_{coord.Q}_{coord.R}");
                tileObj.transform.SetParent(container.transform, false);
                tileObj.transform.position = worldPos;

                HexTile3D tile = tileObj.AddComponent<HexTile3D>();

                // Determine state for this tile
                TileState state = TileState.Barren;
                int tier = 0;

                if (generateSampleBiomes)
                {
                    DetermineSampleBiome(coord, out state, out tier);
                }

                Material topMat = GetTopMaterial(state, tier);
                tile.Initialize(coord, elev, sharedPillarMesh, topMat, sidePillarMaterial);
                tile.SetState(state, tier);

                tiles[coord] = tile;
            }

            Debug.Log($"<color=#4CAF50><b>[HexGrid3D]</b></color> Generated {tiles.Count} 3D hex tiles (Radius: {gridRadius}).");
        }

        private void DetermineSampleBiome(HexCoordinates coord, out TileState state, out int tier)
        {
            int dist = coord.DistanceTo(new HexCoordinates(0, 0));
            if (dist == 0)
            {
                state = TileState.Grass;
                tier = 1;
            }
            else if (coord.Q > 1 && coord.R < 0)
            {
                // North-East / East sector: Scorched & Magma
                if (coord.Q == 2 && coord.R == -1)
                {
                    state = TileState.Magma;
                    tier = 2;
                }
                else
                {
                    state = TileState.Scorched;
                    tier = 1;
                }
            }
            else if (coord.Q < -1 && coord.R > 0)
            {
                // South-West / West sector: Shallow & Deep Water
                if (coord.Q == -2 && coord.R == 2)
                {
                    state = TileState.Water;
                    tier = 2;
                }
                else
                {
                    state = TileState.Water;
                    tier = 1;
                }
            }
            else if (dist <= 2)
            {
                state = TileState.Grass;
                tier = 1;
            }
            else
            {
                state = TileState.Barren;
                tier = 0;
            }
        }

        public void ClearGrid()
        {
            tiles.Clear();
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        public HexTile3D GetTile(HexCoordinates coords)
        {
            tiles.TryGetValue(coords, out HexTile3D tile);
            return tile;
        }

        public List<HexTile3D> GetNeighbors(HexCoordinates coords)
        {
            List<HexTile3D> neighbors = new List<HexTile3D>();
            foreach (var dir in HexCoordinates.Directions)
            {
                HexCoordinates neighborCoord = coords + dir;
                if (tiles.TryGetValue(neighborCoord, out HexTile3D neighborTile))
                {
                    neighbors.Add(neighborTile);
                }
            }
            return neighbors;
        }

        public List<HexTile3D> GetTilesInRange(HexCoordinates center, int range)
        {
            List<HexTile3D> results = new List<HexTile3D>();
            List<HexCoordinates> coords = center.GetRange(range);
            foreach (var c in coords)
            {
                if (tiles.TryGetValue(c, out HexTile3D tile))
                {
                    results.Add(tile);
                }
            }
            return results;
        }
    }
}
