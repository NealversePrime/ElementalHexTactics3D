using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ElementalHexTactics3D.Grid;

namespace ElementalHexTactics3D.StageEnvironment
{
    /// <summary>
    /// Manages the 3D environmental architecture, continuous battlemap floor projection,
    /// and Gothic Obsidian Throne for Tutorial Stage 1 (Prologue Raid).
    /// Features:
    ///   - Continuous Planar World-Space Battlemap Projection across all hexes (royal red velvet runner & dark marble).
    ///   - Stepped Elevation: Ground Tier 0 -> Steps Tier 1 -> High Throne Dais Tier 2!
    ///   - 3D Gothic Obsidian Throne placed on Tier 2 dais (0, 2) facing South towards Paladin.
    ///   - Pillar Capitals & Braziers with warm flickering torchlights framing the throne.
    /// </summary>
    public class ThroneRoomEnvironment3D : MonoBehaviour
    {
        public static ThroneRoomEnvironment3D Instance { get; private set; }

        private const string CONTAINER_NAME = "ThroneRoom_Props_Container";
        private GameObject propsContainer;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(this);
        }

        /// <summary>
        /// Instantiates or builds the 3D Throne and architectural lighting for Stage 1.
        /// </summary>
        public static void SetupThroneRoomEnvironment(HexGrid3D grid)
        {
            if (grid == null) return;

            ThroneRoomEnvironment3D env = grid.GetComponent<ThroneRoomEnvironment3D>();
            if (env == null)
            {
                env = grid.gameObject.AddComponent<ThroneRoomEnvironment3D>();
            }

            env.BuildEnvironment(grid);
        }

        /// <summary>
        /// Clears all throne room props from the scene.
        /// </summary>
        public static void ClearEnvironment(HexGrid3D grid = null)
        {
            if (grid != null)
            {
                Transform old = grid.transform.Find(CONTAINER_NAME);
                if (old != null)
                {
                    if (Application.isPlaying) Destroy(old.gameObject);
                    else DestroyImmediate(old.gameObject);
                }
            }
            else if (Instance != null && Instance.propsContainer != null)
            {
                if (Application.isPlaying) Destroy(Instance.propsContainer);
                else DestroyImmediate(Instance.propsContainer);
            }
        }

        private void BuildEnvironment(HexGrid3D grid)
        {
            ClearEnvironment(grid);

            propsContainer = new GameObject(CONTAINER_NAME);
            propsContainer.transform.SetParent(grid.transform, false);

            // 1. Shaders & Materials setup
            Material baseMat = grid.GetTopMaterial(TileState.Barren);
            Shader litShader = (baseMat != null && baseMat.shader != null) 
                ? baseMat.shader 
                : (Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse"));

            // 2. Continuous Planar Battlemap Floor Projection
            Texture2D battlemapTex = LoadTexture("ThroneRoom_Floor_Battlemap.png")
                ?? LoadTexture("Tiles/ThroneRoom_Floor_Battlemap")
                ?? LoadTextureFromFile("Sprites/Tiles/ThroneRoom_Floor_Battlemap.png");

            Material floorMat = null;
            if (battlemapTex != null)
            {
                floorMat = new Material(litShader);
                floorMat.name = "Mat_ThroneRoom_ProjectedFloor";
                floorMat.mainTexture = battlemapTex;
                if (floorMat.HasProperty("_BaseMap")) floorMat.SetTexture("_BaseMap", battlemapTex);
                if (floorMat.HasProperty("_Color")) floorMat.SetColor("_Color", Color.white);
                if (floorMat.HasProperty("_BaseColor")) floorMat.SetColor("_BaseColor", Color.white);
            }
            else
            {
                // Safety net: If battlemap texture is missing, use base barren material copy
                // so tiles never appear as blank flat white surfaces!
                if (baseMat != null)
                {
                    floorMat = new Material(baseMat);
                    floorMat.name = "Mat_ThroneRoom_ProjectedFloor_Fallback";
                }
                else
                {
                    floorMat = new Material(litShader);
                    Color darkSlate = new Color(0.20f, 0.22f, 0.26f, 1f);
                    floorMat.color = darkSlate;
                    if (floorMat.HasProperty("_BaseColor")) floorMat.SetColor("_BaseColor", darkSlate);
                }
            }

            // Project seamless battlemap across all hex tiles (including StonePillar tiles!)
            // Keep pillar tiles flat on the floor level instead of popping up like outdoor dirt crags
            if (grid.Tiles != null)
            {
                foreach (var kvp in grid.Tiles)
                {
                    HexTile3D tile = kvp.Value;
                    if (tile == null) continue;

                    // Ensure StonePillar tiles remain flat on the floor level
                    tile.SuppressPillarElevationPop = true;

                    if (tile.State != TileState.Scorched)
                    {
                        if (floorMat != null)
                        {
                            tile.ApplyWorldProjectedUVs(floorMat, -5.5f, 5.5f, -5.0f, 5.0f);
                        }
                    }
                }
            }

            // Dark Obsidian / Basalt Stone
            Material stoneMat = new Material(litShader);
            stoneMat.color = new Color(0.18f, 0.19f, 0.23f, 1f);
            if (stoneMat.HasProperty("_BaseColor")) stoneMat.SetColor("_BaseColor", new Color(0.18f, 0.19f, 0.23f, 1f));

            // Royal Crimson Velvet Cushion
            Material cushionMat = new Material(litShader);
            cushionMat.color = new Color(0.68f, 0.12f, 0.18f, 1f);
            if (cushionMat.HasProperty("_BaseColor")) cushionMat.SetColor("_BaseColor", new Color(0.68f, 0.12f, 0.18f, 1f));

            // Gilded Imperial Gold Trim
            Material goldTrimMat = new Material(litShader);
            goldTrimMat.color = new Color(0.92f, 0.74f, 0.30f, 1f);
            if (goldTrimMat.HasProperty("_BaseColor")) goldTrimMat.SetColor("_BaseColor", new Color(0.92f, 0.74f, 0.30f, 1f));

            // Emissive Flame Core
            Material flameMat = new Material(litShader);
            flameMat.color = new Color(1.0f, 0.60f, 0.15f, 1f);
            if (flameMat.HasProperty("_BaseColor")) flameMat.SetColor("_BaseColor", new Color(1.0f, 0.60f, 0.15f, 1f));
            flameMat.EnableKeyword("_EMISSION");
            if (flameMat.HasProperty("_EmissionColor")) flameMat.SetColor("_EmissionColor", new Color(1.5f, 0.7f, 0.1f, 1f));

            // 3. Spawn 3D Gothic Obsidian Throne at Dais (0, 2) [Elevation Tier 2]
            HexTile3D throneTile = grid.GetTile(new HexCoordinates(0, 2));
            if (throneTile != null)
            {
                SpawnThrone(throneTile, litShader, stoneMat, cushionMat, goldTrimMat);
            }

            // 4. Kinetic Shove Obstacle: Broken Pillar at (1, 0) [Elevation Tier 0]
            // Directly East of Demon Slime at (0, 0) - when Holy Shielder at (-1, 0) shoves, slime slams into this broken pillar!
            HexTile3D shovePillar = grid.GetTile(new HexCoordinates(1, 0));
            if (shovePillar != null)
            {
                SpawnPillarColumn(shovePillar, litShader, stoneMat, goldTrimMat, flameMat, isBroken: true, hasBrazier: false);
            }

            // 5. Dais Flanking Columns with Torch Braziers at (-1, 2) and (1, 2) [Elevation Tier 2]
            HexTile3D leftPillar = grid.GetTile(new HexCoordinates(-1, 2));
            if (leftPillar != null)
            {
                SpawnPillarColumn(leftPillar, litShader, stoneMat, goldTrimMat, flameMat, isBroken: false, hasBrazier: true, hasLight: true);
            }

            HexTile3D rightPillar = grid.GetTile(new HexCoordinates(1, 2));
            if (rightPillar != null)
            {
                SpawnPillarColumn(rightPillar, litShader, stoneMat, goldTrimMat, flameMat, isBroken: false, hasBrazier: true, hasLight: true);
            }

            // 6. Rear Dais Pillar behind throne at (0, 3) [Elevation Tier 2]
            HexTile3D rearPillar = grid.GetTile(new HexCoordinates(0, 3));
            if (rearPillar != null)
            {
                SpawnPillarColumn(rearPillar, litShader, stoneMat, goldTrimMat, flameMat, isBroken: false, hasBrazier: true, hasLight: false);
            }

            // 7. Grand Cathedral Colonnade Side Wall Columns at (-2, 1), (-2, -1), (2, 0), (2, -2) [Elevation Tier 0]
            HexCoordinates[] colonnadeCoords = new HexCoordinates[]
            {
                new HexCoordinates(-2, 1),
                new HexCoordinates(-2, -1),
                new HexCoordinates(2, 0),
                new HexCoordinates(2, -2)
            };
            foreach (var cCoord in colonnadeCoords)
            {
                HexTile3D colTile = grid.GetTile(cCoord);
                if (colTile != null)
                {
                    SpawnPillarColumn(colTile, litShader, stoneMat, goldTrimMat, flameMat, isBroken: false, hasBrazier: false);
                }
            }
        }

        private void SpawnThrone(HexTile3D throneTile, Shader litShader, Material stoneMat, Material cushionMat, Material goldTrimMat)
        {
            Vector3 tileCenter = throneTile.GetTopCenterPosition();
            // Position throne slightly toward the back of the hex (+Z offset 0.35f)
            // Facing South (towards Paladin at entrance)
            Vector3 thronePosition = tileCenter + new Vector3(0f, 0f, 0.35f);
            Quaternion throneRotation = Quaternion.Euler(0f, 180f, 0f);

            // 1. Try to load external Gothic Obsidian Throne 3D model
            GameObject externalPrefab = Resources.Load<GameObject>("Models/GothicObsidianThrone")
                ?? Resources.Load<GameObject>("Models/GothicObsidianThrone_Optimized")
                ?? Resources.Load<GameObject>("Models/Throne")
                ?? Resources.Load<GameObject>("Prefabs/Throne");

#if UNITY_EDITOR
            if (externalPrefab == null)
            {
                externalPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Models/GothicObsidianThrone.obj")
                    ?? UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/GothicObsidianThrone.obj");
            }
#endif

            if (externalPrefab != null)
            {
                GameObject extObj = Instantiate(externalPrefab, thronePosition, throneRotation, propsContainer.transform);
                extObj.name = "Gothic_Obsidian_Throne_3D";
                // Scale model to fit the hex dais (~2.2 units high, ~1.5 units wide)
                extObj.transform.localScale = Vector3.one * 0.28f;

                // Bind diffuse texture to mesh materials
                Texture2D throneTex = LoadTexture("GothicObsidianThrone_Diffuse.jpg")
                    ?? LoadTexture("Models/GothicObsidianThrone_Diffuse")
                    ?? LoadTextureFromFile("Resources/Models/GothicObsidianThrone_Diffuse.jpg");
                if (throneTex != null)
                {
                    Material throneMat = new Material(litShader);
                    throneMat.name = "Mat_GothicObsidianThrone";
                    throneMat.mainTexture = throneTex;
                    if (throneMat.HasProperty("_BaseMap")) throneMat.SetTexture("_BaseMap", throneTex);
                    if (throneMat.HasProperty("_Color")) throneMat.SetColor("_Color", Color.white);
                    if (throneMat.HasProperty("_BaseColor")) throneMat.SetColor("_BaseColor", Color.white);

                    foreach (var mr in extObj.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        if (mr != null) mr.sharedMaterial = throneMat;
                    }
                }
                else
                {
                    // Fallback to dark obsidian stone material so the throne is NEVER pure white!
                    foreach (var mr in extObj.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        if (mr != null) mr.sharedMaterial = stoneMat;
                    }
                }

                StripColliders(extObj);
                Debug.Log("<color=#FFD54F><b>[ThroneRoomEnvironment]</b></color> Instantiated Gothic Obsidian Throne 3D Model on Dais (0, 2) at Elevation Tier 2.");
                return;
            }

            // 2. High-Detail Procedural 3D Gothic/Imperial Throne fallback
            GameObject throneObj = new GameObject("Procedural_Gothic_Throne_3D");
            throneObj.transform.SetParent(propsContainer.transform, false);
            throneObj.transform.position = thronePosition;
            throneObj.transform.rotation = throneRotation;

            // --- Stepped Dais Platform ---
            CreateBoxPart(throneObj, "Dais_Step1", new Vector3(0f, 0.05f, 0f), new Vector3(1.4f, 0.10f, 1.4f), stoneMat);
            CreateBoxPart(throneObj, "Dais_Step2", new Vector3(0f, 0.15f, 0f), new Vector3(1.2f, 0.10f, 1.2f), stoneMat);

            // --- Seat Frame & Velvet Cushion ---
            CreateBoxPart(throneObj, "Seat_Base", new Vector3(0f, 0.38f, 0.04f), new Vector3(0.76f, 0.36f, 0.68f), stoneMat);
            CreateBoxPart(throneObj, "Seat_Cushion", new Vector3(0f, 0.58f, 0.04f), new Vector3(0.66f, 0.08f, 0.58f), cushionMat);

            // --- Armrests & Front Gold Finials ---
            CreateBoxPart(throneObj, "Armrest_L", new Vector3(-0.38f, 0.64f, 0.04f), new Vector3(0.12f, 0.22f, 0.64f), stoneMat);
            CreateBoxPart(throneObj, "Armrest_R", new Vector3(0.38f, 0.64f, 0.04f), new Vector3(0.12f, 0.22f, 0.64f), stoneMat);
            CreateCylinderPart(throneObj, "Finial_L", new Vector3(-0.38f, 0.77f, -0.24f), new Vector3(0.12f, 0.08f, 0.12f), goldTrimMat);
            CreateCylinderPart(throneObj, "Finial_R", new Vector3(0.38f, 0.77f, -0.24f), new Vector3(0.12f, 0.08f, 0.12f), goldTrimMat);

            // --- Tall Gothic Backrest & Velvet Inlay ---
            CreateBoxPart(throneObj, "Backrest_Frame", new Vector3(0f, 1.15f, 0.33f), new Vector3(0.82f, 1.15f, 0.12f), stoneMat);
            CreateBoxPart(throneObj, "Backrest_Cushion", new Vector3(0f, 1.10f, 0.28f), new Vector3(0.62f, 0.95f, 0.06f), cushionMat);

            // --- Flanking Backrest Columns ---
            CreateBoxPart(throneObj, "Column_L", new Vector3(-0.43f, 1.15f, 0.33f), new Vector3(0.10f, 1.25f, 0.14f), stoneMat);
            CreateBoxPart(throneObj, "Column_R", new Vector3(0.43f, 1.15f, 0.33f), new Vector3(0.10f, 1.25f, 0.14f), stoneMat);

            // --- Crown Pediment & Imperial Crest ---
            CreateBoxPart(throneObj, "Crown_Pediment", new Vector3(0f, 1.78f, 0.33f), new Vector3(0.88f, 0.15f, 0.15f), stoneMat);
            CreateBoxPart(throneObj, "Crown_ArchCrest", new Vector3(0f, 1.94f, 0.33f), new Vector3(0.32f, 0.22f, 0.15f), goldTrimMat);
            CreateBoxPart(throneObj, "Crown_Spire_L", new Vector3(-0.43f, 1.88f, 0.33f), new Vector3(0.10f, 0.22f, 0.15f), goldTrimMat);
            CreateBoxPart(throneObj, "Crown_Spire_R", new Vector3(0.43f, 1.88f, 0.33f), new Vector3(0.10f, 0.22f, 0.15f), goldTrimMat);

            Debug.Log("<color=#FFD54F><b>[ThroneRoomEnvironment]</b></color> Procedural 3D Gothic Throne assembled at (0, 2) [Elevation Tier 2].");
        }

        private void SpawnPillarColumn(
            HexTile3D tile,
            Shader litShader,
            Material stoneMat,
            Material goldTrimMat,
            Material flameMat,
            bool isBroken,
            bool hasBrazier = false,
            bool hasLight = false)
        {
            Vector3 topCenter = tile.GetTopCenterPosition();
            string pName = isBroken ? $"BrokenPillar_{tile.Coordinates.Q}_{tile.Coordinates.R}" : $"Pillar_{tile.Coordinates.Q}_{tile.Coordinates.R}";

            // 1. Check for external 3D model asset (downloaded low-poly model)
            GameObject externalPrefab = TryLoadExternalPillarModel(isBroken);
            if (externalPrefab != null)
            {
                GameObject extObj = Instantiate(externalPrefab, topCenter, Quaternion.identity, propsContainer.transform);
                extObj.name = $"{pName}_Model";

                float targetHeight = isBroken ? 1.05f : 2.65f;
                AdjustModelScaleToHeight(extObj, targetHeight);
                StripColliders(extObj);

                // If this is a dais pillar requiring a torch brazier on top, attach it
                if (hasBrazier)
                {
                    AttachBrazierHead(extObj, new Vector3(0f, targetHeight, 0f), goldTrimMat, flameMat, hasLight);
                }

                Debug.Log($"<color=#FFD54F><b>[ThroneRoomEnvironment]</b></color> Instantiated external 3D model for {(isBroken ? "Broken Pillar" : "Pillar")} at ({tile.Coordinates.Q}, {tile.Coordinates.R}).");
                return;
            }

            // 2. High-Detail Stylized Procedural 3D Pillar Geometry Fallback
            GameObject pillarObj = new GameObject(pName);
            pillarObj.transform.SetParent(propsContainer.transform, false);
            pillarObj.transform.position = topCenter;

            if (isBroken)
            {
                BuildProceduralBrokenPillar(pillarObj, stoneMat);
            }
            else
            {
                BuildProceduralIntactColumn(pillarObj, stoneMat, goldTrimMat, flameMat, hasBrazier, hasLight);
            }
        }

        private void BuildProceduralBrokenPillar(GameObject parent, Material stoneMat)
        {
            // --- Stepped Square Plinth Base on floor ---
            CreateBoxPart(parent, "Plinth_Step1", new Vector3(0f, 0.08f, 0f), new Vector3(1.10f, 0.16f, 1.10f), stoneMat);
            CreateBoxPart(parent, "Plinth_Step2", new Vector3(0f, 0.20f, 0f), new Vector3(0.95f, 0.10f, 0.95f), stoneMat);

            // --- Lower Intact Cylindrical Shaft ---
            CreateCylinderPart(parent, "Shaft_Lower", new Vector3(0f, 0.50f, 0f), new Vector3(0.72f, 0.50f, 0.72f), stoneMat);

            // --- Jagged Fractured Stone Top (Sheared off in battle) ---
            GameObject jaggedTop1 = CreateBoxPart(parent, "Fracture_Shard_1", new Vector3(0.04f, 0.82f, 0.02f), new Vector3(0.62f, 0.32f, 0.62f), stoneMat);
            jaggedTop1.transform.localRotation = Quaternion.Euler(18f, 25f, -12f);

            GameObject jaggedTop2 = CreateBoxPart(parent, "Fracture_Shard_2", new Vector3(0.12f, 0.96f, -0.08f), new Vector3(0.28f, 0.32f, 0.28f), stoneMat);
            jaggedTop2.transform.localRotation = Quaternion.Euler(-15f, 40f, 20f);

            // --- Fallen Rubble & Stone Debris Chunks around base on marble floor ---
            GameObject rubble1 = CreateBoxPart(parent, "Rubble_Chunk_1", new Vector3(-0.46f, 0.06f, 0.32f), new Vector3(0.22f, 0.12f, 0.26f), stoneMat);
            rubble1.transform.localRotation = Quaternion.Euler(12f, 45f, 20f);

            GameObject rubble2 = CreateBoxPart(parent, "Rubble_Chunk_2", new Vector3(0.42f, 0.05f, -0.36f), new Vector3(0.20f, 0.10f, 0.22f), stoneMat);
            rubble2.transform.localRotation = Quaternion.Euler(-18f, 70f, 10f);

            GameObject rubble3 = CreateBoxPart(parent, "Rubble_Chunk_3", new Vector3(0.20f, 0.04f, 0.44f), new Vector3(0.16f, 0.08f, 0.18f), stoneMat);
            rubble3.transform.localRotation = Quaternion.Euler(30f, 15f, -25f);
        }

        private void BuildProceduralIntactColumn(
            GameObject parent,
            Material stoneMat,
            Material goldTrimMat,
            Material flameMat,
            bool hasBrazier,
            bool hasLight)
        {
            // --- Stepped Square Plinth Base on floor ---
            CreateBoxPart(parent, "Plinth_Step1", new Vector3(0f, 0.08f, 0f), new Vector3(1.10f, 0.16f, 1.10f), stoneMat);
            CreateBoxPart(parent, "Plinth_Step2", new Vector3(0f, 0.20f, 0f), new Vector3(0.95f, 0.10f, 0.95f), stoneMat);
            CreateCylinderPart(parent, "Plinth_Torus", new Vector3(0f, 0.28f, 0f), new Vector3(0.86f, 0.08f, 0.86f), stoneMat);

            // --- Tall Fluted Cylinder Column Shaft ---
            CreateCylinderPart(parent, "Column_Shaft", new Vector3(0f, 1.48f, 0f), new Vector3(0.68f, 2.30f, 0.68f), stoneMat);

            // --- Sculpted Architectural Capital (Head) ---
            CreateCylinderPart(parent, "Capital_Astragal", new Vector3(0f, 2.68f, 0f), new Vector3(0.82f, 0.10f, 0.82f), stoneMat);
            CreateBoxPart(parent, "Capital_Abacus", new Vector3(0f, 2.80f, 0f), new Vector3(1.00f, 0.16f, 1.00f), stoneMat);
            CreateBoxPart(parent, "Capital_GoldTrim", new Vector3(0f, 2.92f, 0f), new Vector3(1.08f, 0.08f, 1.08f), goldTrimMat);

            // --- Top Feature (Brazier or Architectural Cornice) ---
            if (hasBrazier)
            {
                AttachBrazierHead(parent, new Vector3(0f, 2.96f, 0f), goldTrimMat, flameMat, hasLight);
            }
            else
            {
                CreateBoxPart(parent, "Cornice_Crown", new Vector3(0f, 3.08f, 0f), new Vector3(0.82f, 0.24f, 0.82f), stoneMat);
                CreateBoxPart(parent, "Cornice_Finial", new Vector3(0f, 3.24f, 0f), new Vector3(0.40f, 0.16f, 0.40f), goldTrimMat);
            }
        }

        private void AttachBrazierHead(GameObject parent, Vector3 localPos, Material goldTrimMat, Material flameMat, bool hasLight)
        {
            // 1. Bronze / Gilded Brazier Bowl
            CreateCylinderPart(parent, "Brazier_Bowl", localPos + new Vector3(0f, 0.12f, 0f), new Vector3(0.60f, 0.14f, 0.60f), goldTrimMat);

            // 2. Glowing Fire Core / Embers
            CreateSpherePart(parent, "Flame_Embers", localPos + new Vector3(0f, 0.24f, 0f), new Vector3(0.40f, 0.22f, 0.40f), flameMat);

            // 3. Warm Flickering Torchlight
            if (hasLight)
            {
                GameObject lightObj = new GameObject("Brazier_Light");
                lightObj.transform.SetParent(parent.transform, false);
                lightObj.transform.localPosition = localPos + new Vector3(0f, 0.50f, 0f);

                Light ptLight = lightObj.AddComponent<Light>();
                ptLight.type = LightType.Point;
                ptLight.color = new Color(1.0f, 0.65f, 0.28f, 1f); // Warm torch amber
                ptLight.intensity = 1.45f;
                ptLight.range = 5.5f;

                lightObj.AddComponent<BrazierLightFlicker>();
            }
        }

        private static GameObject TryLoadExternalPillarModel(bool isBroken)
        {
            string[] resourceNames = isBroken
                ? new[] { "Models/BrokenPillar", "Models/broken_pillar", "Models/Pillar_Broken", "Prefabs/BrokenPillar" }
                : new[] { "Models/Pillar", "Models/GothicPillar", "Models/Column", "Prefabs/Pillar" };

            foreach (var name in resourceNames)
            {
                GameObject prefab = Resources.Load<GameObject>(name);
                if (prefab != null) return prefab;
            }

#if UNITY_EDITOR
            string[] editorPaths = isBroken
                ? new[] {
                    "Assets/Resources/Models/BrokenPillar.obj", "Assets/Resources/Models/BrokenPillar.fbx",
                    "Assets/Models/BrokenPillar.obj", "Assets/Models/BrokenPillar.fbx",
                    "Assets/Models/broken_pillar.obj", "Assets/Models/broken_pillar.fbx"
                  }
                : new[] {
                    "Assets/Resources/Models/Pillar.obj", "Assets/Resources/Models/Pillar.fbx",
                    "Assets/Resources/Models/GothicPillar.obj", "Assets/Resources/Models/GothicPillar.fbx",
                    "Assets/Models/Pillar.obj", "Assets/Models/Pillar.fbx",
                    "Assets/Models/column.obj", "Assets/Models/column.fbx"
                  };

            foreach (var path in editorPaths)
            {
                GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) return prefab;
            }
#endif

            return null;
        }

        private static void AdjustModelScaleToHeight(GameObject obj, float targetHeight)
        {
            Renderer[] rends = obj.GetComponentsInChildren<Renderer>();
            if (rends == null || rends.Length == 0)
            {
                obj.transform.localScale = Vector3.one;
                return;
            }

            Bounds bounds = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) bounds.Encapsulate(rends[i].bounds);

            float currentHeight = bounds.size.y;
            if (currentHeight > 0.05f)
            {
                float factor = targetHeight / currentHeight;
                obj.transform.localScale = Vector3.one * factor;
            }
        }

        #region Geometry & Texture Helpers

        public static Texture2D LoadTexture(string pathOrName)
        {
            if (string.IsNullOrEmpty(pathOrName)) return null;

            string baseName = Path.GetFileNameWithoutExtension(pathOrName);

            // 1. Resources.Load (Texture2D) - WebGL & Standalone & Editor
            Texture2D resTex = Resources.Load<Texture2D>("Tiles/" + baseName)
                ?? Resources.Load<Texture2D>("Models/" + baseName)
                ?? Resources.Load<Texture2D>("Sprites/Tiles/" + baseName)
                ?? Resources.Load<Texture2D>(baseName);
            if (resTex != null) return resTex;

            // 1b. Resources.Load (Sprite -> Texture)
            Sprite resSprite = Resources.Load<Sprite>("Tiles/" + baseName)
                ?? Resources.Load<Sprite>("Models/" + baseName)
                ?? Resources.Load<Sprite>("Sprites/Tiles/" + baseName)
                ?? Resources.Load<Sprite>(baseName);
            if (resSprite != null && resSprite.texture != null) return resSprite.texture;

#if UNITY_EDITOR
            // 2. Editor AssetDatabase (Immediate fallback inside Editor)
            string[] searchPaths = new string[]
            {
                "Assets/Sprites/Tiles/" + Path.GetFileName(pathOrName),
                "Assets/Resources/Tiles/" + Path.GetFileName(pathOrName),
                "Assets/Resources/Models/" + Path.GetFileName(pathOrName),
                "Assets/Models/" + Path.GetFileName(pathOrName),
                "Assets/" + pathOrName
            };
            foreach (var p in searchPaths)
            {
                Texture2D edTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                if (edTex != null) return edTex;
            }
#endif

            // 3. StreamingAssets / Disk file fallback (Standalone non-WebGL)
            return LoadTextureFromFile(pathOrName);
        }

        public static Texture2D LoadTextureFromFile(string relativePath)
        {
            try
            {
                string fullPath = Path.Combine(Application.dataPath, relativePath);
                if (File.Exists(fullPath))
                {
                    byte[] bytes = File.ReadAllBytes(fullPath);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    tex.LoadImage(bytes);
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.filterMode = FilterMode.Bilinear;
                    return tex;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ThroneRoomEnvironment] Could not load texture from {relativePath}: {ex.Message}");
            }
            return null;
        }

        private static GameObject CreateBoxPart(GameObject parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent.transform, false);
            box.transform.localPosition = localPos;
            box.transform.localScale = localScale;
            if (mat != null) box.GetComponent<MeshRenderer>().sharedMaterial = mat;
            StripColliders(box);
            return box;
        }

        private static GameObject CreateCylinderPart(GameObject parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cyl.name = name;
            cyl.transform.SetParent(parent.transform, false);
            cyl.transform.localPosition = localPos;
            cyl.transform.localScale = localScale;
            if (mat != null) cyl.GetComponent<MeshRenderer>().sharedMaterial = mat;
            StripColliders(cyl);
            return cyl;
        }

        private static GameObject CreateSpherePart(GameObject parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.transform.SetParent(parent.transform, false);
            sphere.transform.localPosition = localPos;
            sphere.transform.localScale = localScale;
            if (mat != null) sphere.GetComponent<MeshRenderer>().sharedMaterial = mat;
            StripColliders(sphere);
            return sphere;
        }

        private static void StripColliders(GameObject obj)
        {
            Collider[] colliders = obj.GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders)
            {
                if (col != null)
                {
                    if (Application.isPlaying) Destroy(col);
                    else DestroyImmediate(col);
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Subtle, organic light flickering for throne room torch braziers.
    /// </summary>
    public class BrazierLightFlicker : MonoBehaviour
    {
        private Light targetLight;
        private float baseIntensity = 1.35f;
        private float noiseOffset;

        private void Awake()
        {
            targetLight = GetComponent<Light>();
            if (targetLight != null) baseIntensity = targetLight.intensity;
            noiseOffset = Random.Range(0f, 100f);
        }

        private void Update()
        {
            if (targetLight == null) return;
            float noise = Mathf.PerlinNoise(Time.time * 4.5f, noiseOffset);
            targetLight.intensity = baseIntensity + (noise - 0.5f) * 0.35f;
        }
    }
}
