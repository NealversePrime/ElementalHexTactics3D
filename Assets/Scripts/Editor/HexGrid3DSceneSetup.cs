#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ElementalHexTactics3D.Grid;
using ElementalHexTactics3D.CameraControl;
using ElementalHexTactics3D.InputHandling;
using ElementalHexTactics3D.Units;
using ElementalHexTactics3D.Turn;
using ElementalHexTactics3D.Combat;

namespace ElementalHexTactics3D.Editor
{
    /// <summary>
    /// Automation script that configures the 3D Hex Battlefield, distinct elemental materials,
    /// square textures, lighting, tactical camera, and spawns Player (Actor3_3) & Enemy (Dracomancer) standees.
    /// </summary>
    [InitializeOnLoad]
    public static class HexGrid3DSceneSetup
    {
        private const string MaterialsFolder = "Assets/Materials";
        private const string TilesFolder = "Assets/Sprites/Tiles";
        private const string BattlersFolder = "Assets/Sprites/Battlers";

        [MenuItem("Elemental Hex 3D/Advanced/Regenerate 3D Hex Battlefield", false, 50)]
        public static void SetupBattlefieldInScene()
        {
            Debug.Log("<color=#FF9800><b>[ElementalHex3D]</b></color> Configuring authentic square elemental materials and battlefield...");

            // 1. Create or load distinct materials for every elemental state with their square textures
            Material barrenMat = EnsureElementalMaterial("Mat_HexTop_Barren", "Barren Earth.png", Color.white);
            Material grassMat = EnsureElementalMaterial("Mat_HexTop_Grass", "Grass.png", Color.white);
            Material scorchedMat = EnsureElementalMaterial("Mat_HexTop_Scorched", "Scorched Earth.png", Color.white);
            Material magmaMat = EnsureElementalMaterial("Mat_HexTop_Magma", "Magma.png", Color.white, isEmissive: true, emissionColor: new Color(1.2f, 0.45f, 0.1f));
            Material shallowWaterMat = EnsureElementalMaterial("Mat_HexTop_ShallowWater", "Shallow Water.png", Color.white, smoothness: 0.85f);
            Material deepWaterMat = EnsureElementalMaterial("Mat_HexTop_DeepWater", "Deep Water.png", Color.white, smoothness: 0.90f);
            Material steamMat = EnsureElementalMaterial("Mat_HexTop_Steam", "Steam.png", Color.white);
            Material sideCliffMat = EnsureElementalMaterial("Mat_HexSide_Cliff", "Cliff_Soil.png", new Color(0.85f, 0.75f, 0.65f), smoothness: 0.1f);

            // 2. Setup Directional Light for crisp diorama sunlight
            SetupLighting();

            // 3. Setup Tactical Orbit Camera
            SetupCamera();

            // 4. Setup Hex Grid Manager & Game Space with distinct materials
            HexGrid3D grid = SetupHexGrid(barrenMat, grassMat, scorchedMat, magmaMat, shallowWaterMat, deepWaterMat, steamMat, sideCliffMat);

            // 5. Generate the initial grid
            if (grid != null)
            {
                grid.GenerateGrid();
            }

            // 6. Spawn 2.5D Billboard Standees: Player (Actor3_3) & Enemy (Dracomancer)
            SetupUnits(grid);

            // 7. Setup Mouse Interaction
            SetupInteraction();

            // 8. Setup Turn Manager
            SetupTurnManager();

            // 9. Setup Combat Feedback Manager (Overhead HP bars, banners, floating numbers)
            SetupCombatFeedback();

            // 10. Setup Sound Manager (Procedural Audio & SFX)
            SetupSoundManager();

            // 11. Setup Combat VFX Manager (Procedural Particle Systems)
            SetupCombatVFX();

            // 12. Setup HD-2D Post-Processing Volume (Bloom, Tilt-Shift DoF, Tonemapping)
            SetupPostProcessingVolume();

            // Mark scene dirty so user can save
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=#4CAF50><b>[ElementalHex3D]</b></color> 3D Hex Battlefield, 2.5D Units & Combat Systems setup complete!");
        }

        private static void SetupAbyssalRiftConduit(HexGrid3D grid)
        {
            if (grid == null) return;
            HexTile3D riftTile = grid.GetTile(new HexCoordinates(0, -3));
            if (riftTile != null)
            {
                AbyssalRiftConduit3D conduit = riftTile.GetComponent<AbyssalRiftConduit3D>();
                if (conduit == null)
                {
                    conduit = riftTile.gameObject.AddComponent<AbyssalRiftConduit3D>();
                }
                conduit.Initialize(riftTile, 2);
                EditorUtility.SetDirty(riftTile);
                Debug.Log("<color=#BA68C8><b>[AbyssalRift]</b></color> Configured Abyssal Rift Conduit at (0, -3).");
            }
        }

        private static void SetupSoundManager()
        {
            SoundManager3D snd = Object.FindFirstObjectByType<SoundManager3D>();
            if (snd == null)
            {
                GameObject sndObj = new GameObject("SoundManager3D");
                snd = sndObj.AddComponent<SoundManager3D>();
            }
        }

        private static void SetupCombatFeedback()
        {
            CombatFeedbackManager feedback = Object.FindFirstObjectByType<CombatFeedbackManager>();
            if (feedback == null)
            {
                GameObject fbObj = new GameObject("CombatFeedbackManager");
                feedback = fbObj.AddComponent<CombatFeedbackManager>();
            }
        }

        private static void SetupTurnManager()
        {
            TurnManager3D turnMgr = Object.FindFirstObjectByType<TurnManager3D>();
            if (turnMgr == null)
            {
                GameObject turnObj = new GameObject("TurnManager");
                turnMgr = turnObj.AddComponent<TurnManager3D>();
            }
        }

        private static void SetupCombatVFX()
        {
            CombatVFXManager vfx = Object.FindFirstObjectByType<CombatVFXManager>();
            if (vfx == null)
            {
                GameObject vfxObj = new GameObject("CombatVFXManager");
                vfx = vfxObj.AddComponent<CombatVFXManager>();
            }
        }

        private static void SetupPostProcessingVolume()
        {
            Volume volume = Object.FindFirstObjectByType<Volume>();
            if (volume == null)
            {
                GameObject volObj = new GameObject("Global PostProcess Volume");
                volume = volObj.AddComponent<Volume>();
            }
            volume.isGlobal = true;

            string profilePath = "Assets/Settings/HD2D_TacticsProfile.asset";
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }

            // 1. Bloom (Makes lava embers, glowing runes, and magic burst radiate)
            if (!profile.TryGet<Bloom>(out var bloom))
            {
                bloom = profile.Add<Bloom>(true);
            }
            bloom.active = true;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.85f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 1.15f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.65f;

            // 2. Depth Of Field (HD-2D Tilt-Shift diorama tabletop blur)
            if (!profile.TryGet<DepthOfField>(out var dof))
            {
                dof = profile.Add<DepthOfField>(true);
            }
            dof.active = true;
            dof.mode.overrideState = true;
            dof.mode.value = DepthOfFieldMode.Bokeh;
            dof.focusDistance.overrideState = true;
            dof.focusDistance.value = 12.0f; // Center plateau focus
            dof.focalLength.overrideState = true;
            dof.focalLength.value = 65f;
            dof.aperture.overrideState = true;
            dof.aperture.value = 3.2f;

            // 3. Tonemapping (ACES Fantasy saturation and contrast)
            if (!profile.TryGet<Tonemapping>(out var tonemapping))
            {
                tonemapping = profile.Add<Tonemapping>(true);
            }
            tonemapping.active = true;
            tonemapping.mode.overrideState = true;
            tonemapping.mode.value = TonemappingMode.ACES;

            // 4. Vignette (Dramatic border shading)
            if (!profile.TryGet<Vignette>(out var vignette))
            {
                vignette = profile.Add<Vignette>(true);
            }
            vignette.active = true;
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.22f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.35f;

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            volume.sharedProfile = profile;
        }

        private static Material EnsureElementalMaterial(
            string matName,
            string textureFileName,
            Color baseColor,
            bool isEmissive = false,
            Color emissionColor = default,
            float smoothness = 0.2f)
        {
            if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }

            string matPath = $"{MaterialsFolder}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }

            mat.color = baseColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

            string texPath = $"{TilesFolder}/{textureFileName}";
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                mat.mainTexture = tex;
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            }

            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", isEmissive ? emissionColor : Color.black);
            }

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        private static void SetupLighting()
        {
            Light dirLight = Object.FindFirstObjectByType<Light>();
            GameObject lightObj;

            if (dirLight == null)
            {
                lightObj = new GameObject("Directional Light");
                dirLight = lightObj.AddComponent<Light>();
                dirLight.type = LightType.Directional;
            }
            else
            {
                lightObj = dirLight.gameObject;
            }

            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            dirLight.color = new Color(1f, 0.96f, 0.90f); // Warm sunlight
            dirLight.intensity = 1.25f;
            dirLight.shadows = LightShadows.Soft;
        }

        private static void SetupCamera()
        {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<UnityEngine.Camera>();
                camObj.tag = "MainCamera";
            }

            TacticalCameraController camCtrl = cam.GetComponent<TacticalCameraController>();
            if (camCtrl == null)
            {
                camCtrl = cam.gameObject.AddComponent<TacticalCameraController>();
            }

            cam.fieldOfView = 45f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 150f;
            cam.clearFlags = CameraClearFlags.Skybox;

            // Enable URP Post-Processing on Camera
            var camData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null)
            {
                camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
            camData.renderPostProcessing = true;

            camCtrl.ApplyCameraTransform();
        }

        private static HexGrid3D SetupHexGrid(
            Material barrenMat,
            Material grassMat,
            Material scorchedMat,
            Material magmaMat,
            Material shallowWaterMat,
            Material deepWaterMat,
            Material steamMat,
            Material sideCliffMat)
        {
            HexGrid3D grid = Object.FindFirstObjectByType<HexGrid3D>();
            if (grid == null)
            {
                GameObject gridObj = new GameObject("HexGrid3D");
                grid = gridObj.AddComponent<HexGrid3D>();
            }

            SerializedObject serialized = new SerializedObject(grid);
            serialized.FindProperty("barrenMaterial").objectReferenceValue = barrenMat;
            serialized.FindProperty("grassMaterial").objectReferenceValue = grassMat;
            serialized.FindProperty("scorchedMaterial").objectReferenceValue = scorchedMat;
            serialized.FindProperty("magmaMaterial").objectReferenceValue = magmaMat;
            serialized.FindProperty("shallowWaterMaterial").objectReferenceValue = shallowWaterMat;
            serialized.FindProperty("deepWaterMaterial").objectReferenceValue = deepWaterMat;
            serialized.FindProperty("steamMaterial").objectReferenceValue = steamMat;
            serialized.FindProperty("sidePillarMaterial").objectReferenceValue = sideCliffMat;

            serialized.FindProperty("gridRadius").intValue = 4;
            serialized.FindProperty("hexRadius").floatValue = 1.0f;
            serialized.FindProperty("elevationHeight").floatValue = 0.5f;
            serialized.FindProperty("pillarDepth").floatValue = 1.0f;
            serialized.FindProperty("generateCenterPlateau").boolValue = true;
            serialized.FindProperty("generateSampleBiomes").boolValue = true;
            serialized.ApplyModifiedProperties();

            return grid;
        }

        private static void SetupUnits(HexGrid3D grid)
        {
            if (grid == null) return;

            // Clean up any existing units to prevent duplicates
            var existingUnits = Object.FindObjectsByType<TacticalUnit3D>(FindObjectsSortMode.None);
            foreach (var u in existingUnits)
            {
                Object.DestroyImmediate(u.gameObject);
            }

            // Ensure Unit Ring Sprite exists
            Sprite ringSprite = EnsureSpriteImporter("Assets/Sprites/UnitRing_Circle.png");

            // 1. Spawn Player 1: Commander in Citadel Reserve across the Rift
            Sprite playerSprite = LoadSubSprite($"{BattlersFolder}/Actor3_3.png", "Actor3_3_0");
            HexTile3D playerTile = grid.GetTile(new HexCoordinates(0, -2));
            if (playerTile != null)
            {
                GameObject cmdrObj = CreateUnitStandee("Unit_Player_Commander", "Commander", UnitFaction.Player, playerSprite, ringSprite, playerTile,
                    hp: 10, range: 3, archetype: UnitArchetype.Commander, affinity: ElementalAffinity.None, baseAtk: 3, standeeScale: 0.75f);
                TacticalUnit3D cmdr = cmdrObj.GetComponent<TacticalUnit3D>();
                if (cmdr != null)
                {
                    cmdr.AddElementalCore(1); // Start with 1 Core so player can test summoning Titan immediately!
                }

                // Commander waits in the Citadel Reserve across the Rift until deployed!
                playerTile.CurrentOccupant = null;
                cmdr.CurrentTile = null;
                cmdrObj.SetActive(false);
            }

            // 2. Spawn Player 2: Allied Titan in Citadel Reserve across the Rift
            Sprite titanSprite = LoadSubSprite($"{BattlersFolder}/FlameFrost Dragon.png", "FlameFrost Dragon_0");
            HexTile3D titanTile = grid.GetTile(new HexCoordinates(-1, -1));
            if (titanTile != null)
            {
                GameObject titanObj = CreateUnitStandee("Unit_Player_Titan", "Flame Titan (Dragon)", UnitFaction.Player, titanSprite, ringSprite, titanTile,
                    hp: 14, range: 2, archetype: UnitArchetype.Titan, affinity: ElementalAffinity.Fire, baseAtk: 4, standeeScale: 1.05f);

                // Titan waits in the Citadel Reserve across the Rift until summoned!
                titanTile.CurrentOccupant = null;
                TacticalUnit3D titan = titanObj.GetComponent<TacticalUnit3D>();
                if (titan != null) titan.CurrentTile = null;
                titanObj.SetActive(false);
            }

            // 3. Spawn Enemy 1: Boss Dracomancer at (0, 2)
            Sprite enemySprite = LoadSubSprite($"{BattlersFolder}/Dracomancer.png", "Dracomancer_0");
            HexTile3D enemyTile = grid.GetTile(new HexCoordinates(0, 2));
            if (enemyTile != null)
            {
                CreateUnitStandee("Unit_Enemy_Dracomancer", "Dracomancer", UnitFaction.Enemy, enemySprite, ringSprite, enemyTile,
                    hp: 12, range: 3, archetype: UnitArchetype.Commander, affinity: ElementalAffinity.Fire, baseAtk: 3, standeeScale: 0.80f);
            }

            // 4. Spawn Enemy 2: Minion Demon Slime at (1, 1)
            Sprite slimeSprite = LoadSubSprite($"{BattlersFolder}/Demon Slime.png", "Demon Slime_0");
            HexTile3D slimeTile = grid.GetTile(new HexCoordinates(1, 1));
            if (slimeTile != null)
            {
                CreateUnitStandee("Unit_Enemy_DemonSlime", "Demon Slime", UnitFaction.Enemy, slimeSprite, ringSprite, slimeTile,
                    hp: 8, range: 2, archetype: UnitArchetype.Minion, affinity: ElementalAffinity.None, baseAtk: 2, standeeScale: 0.70f);
            }
        }

        private static GameObject CreateUnitStandee(
            string goName,
            string unitName,
            UnitFaction faction,
            Sprite standeeSprite,
            Sprite ringSprite,
            HexTile3D tile,
            int hp,
            int range,
            UnitArchetype archetype = UnitArchetype.Commander,
            ElementalAffinity affinity = ElementalAffinity.None,
            int baseAtk = 3,
            float standeeScale = 0.75f)
        {
            GameObject unitObj = new GameObject(goName);
            unitObj.transform.position = tile.GetTopCenterPosition();

            // Standee Child (2D Billboard)
            GameObject standeeObj = new GameObject("Standee");
            standeeObj.transform.SetParent(unitObj.transform, false);
            standeeObj.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            standeeObj.transform.localScale = Vector3.one * standeeScale;

            SpriteRenderer sr = standeeObj.AddComponent<SpriteRenderer>();
            sr.sprite = standeeSprite;
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            sr.receiveShadows = true;

            Billboard25D billboard = standeeObj.AddComponent<Billboard25D>();

            // Base Ring Child (Feet indicator)
            GameObject ringObj = new GameObject("BaseRing");
            ringObj.transform.SetParent(unitObj.transform, false);
            ringObj.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            ringObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ringObj.transform.localScale = Vector3.one * 1.0f;

            SpriteRenderer ringSr = ringObj.AddComponent<SpriteRenderer>();
            ringSr.sprite = ringSprite;
            ringSr.color = (faction == UnitFaction.Player) ? TacticalUnit3D.PlayerColor : TacticalUnit3D.EnemyColor;

            // Unit Controller
            TacticalUnit3D unit = unitObj.AddComponent<TacticalUnit3D>();
            unit.Initialize(unitName, faction, standeeSprite, tile, hp, range, archetype, affinity, baseAtk);

            EditorUtility.SetDirty(unit);
            EditorUtility.SetDirty(unitObj);

            return unitObj;
        }

        private static Sprite LoadSubSprite(string path, string subName)
        {
            EnsureSpriteImporter(path);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in assets)
            {
                if (a is Sprite s && (string.IsNullOrEmpty(subName) || s.name == subName))
                {
                    return s;
                }
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite EnsureSpriteImporter(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void SetupInteraction()
        {
            HexGridInteraction3D interaction = Object.FindFirstObjectByType<HexGridInteraction3D>();
            if (interaction == null)
            {
                GameObject interObj = new GameObject("HexGridInteraction");
                interaction = interObj.AddComponent<HexGridInteraction3D>();
            }
        }
    }
}
#endif
