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
            Texture2D battlemapTex = LoadTextureFromFile("Sprites/Tiles/ThroneRoom_Floor_Battlemap.png")
                ?? LoadTextureFromFile("Resources/Sprites/Tiles/ThroneRoom_Floor_Battlemap.png");

            Material floorMat = new Material(litShader);
            floorMat.name = "Mat_ThroneRoom_ProjectedFloor";
            if (battlemapTex != null)
            {
                floorMat.mainTexture = battlemapTex;
                if (floorMat.HasProperty("_BaseMap")) floorMat.SetTexture("_BaseMap", battlemapTex);
            }
            else
            {
                floorMat.color = new Color(0.20f, 0.22f, 0.26f, 1f);
            }

            // Project seamless battlemap across all non-pillar, non-scorched hex tiles
            if (grid.Tiles != null)
            {
                foreach (var kvp in grid.Tiles)
                {
                    HexTile3D tile = kvp.Value;
                    if (tile != null && tile.State != TileState.StonePillar && tile.State != TileState.Scorched)
                    {
                        tile.ApplyWorldProjectedUVs(floorMat, -5.5f, 5.5f, -5.0f, 5.0f);
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

            // 4. Spawn Pillar Capitals & Braziers with Torchlights at (-1, 2) and (1, 2) [Elevation Tier 2]
            HexTile3D leftPillar = grid.GetTile(new HexCoordinates(-1, 2));
            if (leftPillar != null) SpawnPillarBrazier(leftPillar, stoneMat, goldTrimMat, flameMat);

            HexTile3D rightPillar = grid.GetTile(new HexCoordinates(1, 2));
            if (rightPillar != null) SpawnPillarBrazier(rightPillar, stoneMat, goldTrimMat, flameMat);

            HexTile3D rearPillar = grid.GetTile(new HexCoordinates(0, 3));
            if (rearPillar != null) SpawnPillarBrazier(rearPillar, stoneMat, goldTrimMat, flameMat, hasLight: false);
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
                Texture2D throneTex = LoadTextureFromFile("Resources/Models/GothicObsidianThrone_Diffuse.jpg")
                    ?? LoadTextureFromFile("Models/GothicObsidianThrone_Diffuse.jpg");
                if (throneTex != null)
                {
                    Material throneMat = new Material(litShader);
                    throneMat.mainTexture = throneTex;
                    if (throneMat.HasProperty("_BaseMap")) throneMat.SetTexture("_BaseMap", throneTex);

                    foreach (var mr in extObj.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        if (mr != null) mr.sharedMaterial = throneMat;
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

        private void SpawnPillarBrazier(HexTile3D pillarTile, Material stoneMat, Material goldTrimMat, Material flameMat, bool hasLight = true)
        {
            Vector3 topCenter = pillarTile.GetTopCenterPosition();

            GameObject brazierObj = new GameObject($"Brazier_{pillarTile.Coordinates.Q}_{pillarTile.Coordinates.R}");
            brazierObj.transform.SetParent(propsContainer.transform, false);
            brazierObj.transform.position = topCenter;

            // 1. Carved Architectural Stone Capital
            CreateBoxPart(brazierObj, "Pillar_Capital", new Vector3(0f, 0.08f, 0f), new Vector3(0.90f, 0.15f, 0.90f), stoneMat);

            // 2. Bronze / Gilded Brazier Bowl
            CreateCylinderPart(brazierObj, "Brazier_Bowl", new Vector3(0f, 0.22f, 0f), new Vector3(0.55f, 0.14f, 0.55f), goldTrimMat);

            // 3. Glowing Fire Core / Embers
            CreateSpherePart(brazierObj, "Flame_Embers", new Vector3(0f, 0.33f, 0f), new Vector3(0.38f, 0.22f, 0.38f), flameMat);

            // 4. Warm Flickering Torchlight
            if (hasLight)
            {
                GameObject lightObj = new GameObject("Brazier_Light");
                lightObj.transform.SetParent(brazierObj.transform, false);
                lightObj.transform.localPosition = new Vector3(0f, 0.55f, 0f);

                Light ptLight = lightObj.AddComponent<Light>();
                ptLight.type = LightType.Point;
                ptLight.color = new Color(1.0f, 0.65f, 0.28f, 1f); // Warm torch amber
                ptLight.intensity = 1.35f;
                ptLight.range = 5.5f;

                lightObj.AddComponent<BrazierLightFlicker>();
            }
        }

        #region Geometry & Texture Helpers

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
