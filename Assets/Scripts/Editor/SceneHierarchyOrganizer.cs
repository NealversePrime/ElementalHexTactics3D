#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ElementalHexTactics3D.Units;

namespace ElementalHexTactics3D.Editor
{
    [InitializeOnLoad]
    public static class SceneHierarchyOrganizer
    {
        public const string EnvFolder = "=== ENVIRONMENT ===";
        public const string SysFolder = "=== SYSTEMS ===";
        public const string GridFolder = "=== BATTLEFIELD ===";
        public const string UnitsFolder = "=== UNITS ===";
        public const string CanvasesFolder = "=== CANVASES ===";

        static SceneHierarchyOrganizer()
        {
            EditorApplication.delayCall += AutoOrganizeIfLooseObjectsExist;
        }

        private static void AutoOrganizeIfLooseObjectsExist()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded) return;

            bool hasLooseRootObjects = false;
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go != null && !go.name.StartsWith("==="))
                {
                    hasLooseRootObjects = true;
                    break;
                }
            }

            if (hasLooseRootObjects)
            {
                OrganizeScene();
            }
        }

        [MenuItem("Elemental Hex 3D/Hierarchy/Organize Scene Hierarchy & Clean Duplicates", false, 10)]
        public static void OrganizeScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded) return;

            Debug.Log("<color=#2196F3><b>[HierarchyOrganizer]</b></color> Tidying up scene hierarchy...");

            // 1. Create or get standard parent category folders
            GameObject envParent = GetOrCreateRootFolder(EnvFolder);
            GameObject sysParent = GetOrCreateRootFolder(SysFolder);
            GameObject gridParent = GetOrCreateRootFolder(GridFolder);
            GameObject unitsParent = GetOrCreateRootFolder(UnitsFolder);
            GameObject uiParent = GetOrCreateRootFolder(CanvasesFolder);

            // 2. Clean up duplicate inactive units
            var allUnits = Object.FindObjectsByType<TacticalUnit3D>(FindObjectsInactive.Include);
            var seenUnitNames = new HashSet<string>();
            int removedCount = 0;

            foreach (var unit in allUnits)
            {
                if (unit == null) continue;
                string baseName = unit.gameObject.name.Replace("(Clone)", "").Trim();

                // If it's a player commander or titan that is duplicated, keep only ONE master instance
                if (baseName.StartsWith("Unit_Player_Commander") || baseName.StartsWith("Unit_Player_Titan"))
                {
                    if (seenUnitNames.Contains(baseName))
                    {
                        Object.DestroyImmediate(unit.gameObject);
                        removedCount++;
                        continue;
                    }
                    seenUnitNames.Add(baseName);
                }

                // Parent under === UNITS ===
                if (unit.transform.parent != unitsParent.transform)
                {
                    unit.transform.SetParent(unitsParent.transform, true);
                }
            }

            if (removedCount > 0)
            {
                Debug.Log($"<color=#FF9800><b>[HierarchyOrganizer]</b></color> Removed {removedCount} duplicate unit instances from scene.");
            }

            // 3. Categorize all remaining root objects
            var rootObjs = scene.GetRootGameObjects();
            foreach (var go in rootObjs)
            {
                if (go == null) continue;
                string name = go.name;

                // Don't reparent the category containers themselves
                if (name.StartsWith("===")) continue;

                // Environment
                if (name.Contains("Camera") || name.Contains("Light") || name.Contains("Volume"))
                {
                    go.transform.SetParent(envParent.transform, true);
                }
                // Battlefield
                else if (name.Contains("HexGrid") && !name.Contains("Interaction"))
                {
                    go.transform.SetParent(gridParent.transform, true);
                }
                // Canvases
                else if (name.StartsWith("Canvas_") || go.GetComponent<Canvas>() != null)
                {
                    go.transform.SetParent(uiParent.transform, true);
                }
                // Any remaining units
                else if (name.StartsWith("Unit_") || go.GetComponent<TacticalUnit3D>() != null)
                {
                    go.transform.SetParent(unitsParent.transform, true);
                }
                // Systems & Managers (catch-all for managers, EventSystem, VisualScripting, etc.)
                else
                {
                    go.transform.SetParent(sysParent.transform, true);
                }
            }

            // 4. Enforce clean top-to-bottom sibling order for the 5 root folders
            envParent.transform.SetSiblingIndex(0);
            sysParent.transform.SetSiblingIndex(1);
            gridParent.transform.SetSiblingIndex(2);
            unitsParent.transform.SetSiblingIndex(3);
            uiParent.transform.SetSiblingIndex(4);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("<color=#4CAF50><b>[HierarchyOrganizer]</b></color> Hierarchy tidied, duplicates removed, and scene saved!");
        }

        private static GameObject GetOrCreateRootFolder(string folderName)
        {
            var go = GameObject.Find(folderName);
            if (go == null)
            {
                go = new GameObject(folderName);
                go.transform.position = Vector3.zero;
                go.transform.rotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
            }
            return go;
        }
    }
}
#endif
