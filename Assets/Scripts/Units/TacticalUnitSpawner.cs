using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ElementalHexTactics3D.Grid;

namespace ElementalHexTactics3D.Units
{
    /// <summary>
    /// Runtime and Editor-safe factory for procedural unit standee creation,
    /// battler sprite streaming, and faction roster management.
    /// </summary>
    public static class TacticalUnitSpawner
    {
        private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
        private static Sprite cachedRingSprite;

        public static Sprite GetUnitRingSprite()
        {
            if (cachedRingSprite != null) return cachedRingSprite;

#if UNITY_EDITOR
            cachedRingSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UnitRing_Circle.png");
            if (cachedRingSprite != null) return cachedRingSprite;
#endif

            string ringPath = Path.Combine(Application.dataPath, "Sprites/UnitRing_Circle.png");
            if (File.Exists(ringPath))
            {
                byte[] bytes = File.ReadAllBytes(ringPath);
                Texture2D tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    tex.name = "UnitRing_Circle";
                    cachedRingSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    return cachedRingSprite;
                }
            }

            return null;
        }

        public static Sprite LoadBattlerSprite(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;

            if (spriteCache.TryGetValue(fileName, out Sprite cached) && cached != null)
            {
                return cached;
            }

#if UNITY_EDITOR
            string assetPath = "Assets/Sprites/Battlers/" + fileName;
            Sprite edSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (edSprite != null)
            {
                spriteCache[fileName] = edSprite;
                return edSprite;
            }
#endif

            string filePath = Path.Combine(Application.dataPath, "Sprites/Battlers", fileName);
            if (File.Exists(filePath))
            {
                byte[] bytes = File.ReadAllBytes(filePath);
                Texture2D tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    tex.name = Path.GetFileNameWithoutExtension(fileName);
                    Sprite sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    spriteCache[fileName] = sp;
                    return sp;
                }
            }

            Debug.LogWarning($"[TacticalUnitSpawner] Could not load battler sprite: {fileName}");
            return null;
        }

        public static TacticalUnit3D SpawnUnitStandee(
            string unitName,
            UnitFaction faction,
            Sprite standeeSprite,
            HexTile3D tile,
            int hp,
            int range,
            UnitArchetype archetype = UnitArchetype.Minion,
            ElementalAffinity affinity = ElementalAffinity.None,
            int baseAtk = 2,
            float standeeScale = 0.75f)
        {
            if (tile == null) return null;

            string goName = $"Unit_{faction}_{unitName.Replace(" ", "")}";
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

            standeeObj.AddComponent<Billboard25D>();

            // Base Ring Child
            GameObject ringObj = new GameObject("BaseRing");
            ringObj.transform.SetParent(unitObj.transform, false);
            ringObj.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            ringObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ringObj.transform.localScale = Vector3.one * 1.0f;

            SpriteRenderer ringSr = ringObj.AddComponent<SpriteRenderer>();
            ringSr.sprite = GetUnitRingSprite();
            ringSr.color = (faction == UnitFaction.Player) ? TacticalUnit3D.PlayerColor : TacticalUnit3D.EnemyColor;

            // Unit Controller
            TacticalUnit3D unit = unitObj.AddComponent<TacticalUnit3D>();
            unit.Initialize(unitName, faction, standeeSprite, tile, hp, range, archetype, affinity, baseAtk);

            tile.CurrentOccupant = unit;
            return unit;
        }

        public static void ClearAllEnemies()
        {
            TacticalUnit3D[] units = Object.FindObjectsByType<TacticalUnit3D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var u in units)
            {
                if (u != null && u.Faction == UnitFaction.Enemy)
                {
                    if (u.CurrentTile != null && u.CurrentTile.CurrentOccupant == u)
                    {
                        u.CurrentTile.CurrentOccupant = null;
                    }
                    Object.Destroy(u.gameObject);
                }
            }
        }

        /// <summary>
        /// Ensures only one Commander and one Titan exist in scene, resets them to Citadel reserve,
        /// purges any duplicate instances, and restores their health and readiness.
        /// </summary>
        public static void ResetPlayerReserveUnits()
        {
            TacticalUnit3D[] units = Object.FindObjectsByType<TacticalUnit3D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            TacticalUnit3D primaryCmdr = null;
            TacticalUnit3D primaryTitan = null;

            foreach (var u in units)
            {
                if (u == null || u.Faction != UnitFaction.Player) continue;

                if (u.Archetype == UnitArchetype.Commander)
                {
                    if (primaryCmdr == null)
                    {
                        primaryCmdr = u;
                    }
                    else
                    {
                        // Duplicate commander! Purge from scene
                        if (u.CurrentTile != null && u.CurrentTile.CurrentOccupant == u)
                        {
                            u.CurrentTile.CurrentOccupant = null;
                        }
                        if (Application.isPlaying) Object.Destroy(u.gameObject);
                        else Object.DestroyImmediate(u.gameObject);
                    }
                }
                else if (u.Archetype == UnitArchetype.Titan)
                {
                    if (primaryTitan == null)
                    {
                        primaryTitan = u;
                    }
                    else
                    {
                        // Duplicate titan! Purge from scene
                        if (u.CurrentTile != null && u.CurrentTile.CurrentOccupant == u)
                        {
                            u.CurrentTile.CurrentOccupant = null;
                        }
                        if (Application.isPlaying) Object.Destroy(u.gameObject);
                        else Object.DestroyImmediate(u.gameObject);
                    }
                }
                else
                {
                    // Any other player minion/summon outside Commander/Titan: clear and deactivate or destroy
                    if (u.CurrentTile != null && u.CurrentTile.CurrentOccupant == u)
                    {
                        u.CurrentTile.CurrentOccupant = null;
                    }
                    u.CurrentTile = null;
                    u.gameObject.SetActive(false);
                }
            }

            if (primaryCmdr != null)
            {
                if (primaryCmdr.CurrentTile != null && primaryCmdr.CurrentTile.CurrentOccupant == primaryCmdr)
                {
                    primaryCmdr.CurrentTile.CurrentOccupant = null;
                }
                primaryCmdr.CurrentTile = null;
                primaryCmdr.Revive();
                primaryCmdr.ResetTurnActions();
                primaryCmdr.gameObject.SetActive(false);
            }

            if (primaryTitan != null)
            {
                if (primaryTitan.CurrentTile != null && primaryTitan.CurrentTile.CurrentOccupant == primaryTitan)
                {
                    primaryTitan.CurrentTile.CurrentOccupant = null;
                }
                primaryTitan.CurrentTile = null;
                primaryTitan.Revive();
                primaryTitan.ResetTurnActions();
                primaryTitan.gameObject.SetActive(false);
            }
        }
    }
}

