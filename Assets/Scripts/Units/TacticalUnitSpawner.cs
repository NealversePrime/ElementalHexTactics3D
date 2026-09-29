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

        public static Sprite LoadPortraitSprite(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;

            string cacheKey = "Portrait_" + fileName;
            if (spriteCache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
            {
                return cached;
            }

#if UNITY_EDITOR
            string assetPath = "Assets/Sprites/Portraits/" + fileName;
            Sprite edSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (edSprite != null)
            {
                spriteCache[cacheKey] = edSprite;
                return edSprite;
            }
#endif

            string filePath = Path.Combine(Application.dataPath, "Sprites/Portraits", fileName);
            if (File.Exists(filePath))
            {
                byte[] bytes = File.ReadAllBytes(filePath);
                Texture2D tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    tex.name = Path.GetFileNameWithoutExtension(fileName);
                    Sprite sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    spriteCache[cacheKey] = sp;
                    return sp;
                }
            }

            return null;
        }

        public static Sprite GetPortraitForUnit(string unitName)
        {
            if (string.IsNullOrEmpty(unitName)) return null;
            string lower = unitName.ToLowerInvariant();
            if (lower.Contains("demon") || lower.Contains("lord") || (lower.Contains("commander") && !lower.Contains("paladin") && !lower.Contains("crusade")))
                return LoadPortraitSprite("demonlordportrait.png");
            if (lower.Contains("basalt") || lower.Contains("minotaur") || (lower.Contains("vanguard") && !lower.Contains("crusade")))
                return LoadPortraitSprite("minotaurportrait.png");
            if (lower.Contains("siren") || lower.Contains("sorceress"))
                return LoadPortraitSprite("sirentportrait.png");
            if (lower.Contains("imp"))
                return LoadPortraitSprite("magmaimp.png");
            if (lower.Contains("magma") || lower.Contains("flame") || lower.Contains("dragon") || lower.Contains("ignis"))
                return LoadPortraitSprite("magmadragonportrait.png");
            if (lower.Contains("sea") || lower.Contains("leviathan"))
                return LoadPortraitSprite("sealeviathanportrait.png");
            if (lower.Contains("earth") || lower.Contains("behemoth") || lower.Contains("golem") || lower.Contains("terra"))
                return LoadPortraitSprite("earthbehemothportrait.png");
            if (lower.Contains("shadow") || lower.Contains("hound") || lower.Contains("wolf"))
                return LoadPortraitSprite("shadowhoundportrait.png");

            // Holy Crusade Enemy Roster
            if (lower.Contains("paladin") || lower.Contains("hero") || lower.Contains("crusade commander"))
                return LoadPortraitSprite("paladinportrait.png");
            if (lower.Contains("shielder") || lower.Contains("templar") || lower.Contains("shield"))
                return LoadPortraitSprite("holyshielderportrait.png");
            if (lower.Contains("archer") || lower.Contains("ranger") || lower.Contains("inquisitor"))
                return LoadPortraitSprite("holyarcherportrait.png");
            if (lower.Contains("saintess") || lower.Contains("priestess") || lower.Contains("envoy") || lower.Contains("cleric"))
                return LoadPortraitSprite("holysaintessportrait.png");

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
            string goName = $"Unit_{faction}_{unitName.Replace(" ", "")}";
            GameObject unitObj = new GameObject(goName);
            Vector3 pos = (tile != null) ? tile.GetTopCenterPosition() : Vector3.zero;
            unitObj.transform.position = pos;

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

            if (tile != null) tile.CurrentOccupant = unit;
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
        /// purges any duplicate instances, restores their health and readiness, and ensures all allied units exist.
        /// </summary>
        public static void ResetPlayerReserveUnits()
        {
            TacticalUnit3D[] units = Object.FindObjectsByType<TacticalUnit3D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            TacticalUnit3D primaryCmdr = null;
            TacticalUnit3D primaryTitan = null;
            HashSet<string> seenMinionNames = new HashSet<string>();

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
                    // Deduplicate minions by name
                    if (seenMinionNames.Contains(u.UnitName))
                    {
                        if (u.CurrentTile != null && u.CurrentTile.CurrentOccupant == u)
                        {
                            u.CurrentTile.CurrentOccupant = null;
                        }
                        if (Application.isPlaying) Object.Destroy(u.gameObject);
                        else Object.DestroyImmediate(u.gameObject);
                        continue;
                    }
                    seenMinionNames.Add(u.UnitName);

                    // Player minion/summon outside Commander/Titan: clear, revive, reset and store in reserve
                    if (u.CurrentTile != null && u.CurrentTile.CurrentOccupant == u)
                    {
                        u.CurrentTile.CurrentOccupant = null;
                    }
                    u.CurrentTile = null;
                    u.Revive();
                    u.ResetTurnActions();
                    u.gameObject.SetActive(false);
                }
            }

            if (primaryCmdr != null)
            {
                // Auto-upgrade legacy placeholder sprite to Demon Lord if still using Actor3_3 or named generic Commander
                if (primaryCmdr.StandeeSprite == null || primaryCmdr.StandeeSprite.name.Contains("Actor3_3") || primaryCmdr.UnitName == "Commander")
                {
                    Sprite dlSprite = LoadBattlerSprite("DemonLord.png");
                    if (dlSprite != null)
                    {
                        primaryCmdr.SetStandeeSprite(dlSprite);
                        primaryCmdr.SetUnitName("Demon Lord");
                    }
                }

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
                // Auto-upgrade legacy dragon sprite to Magma Dragon titan
                if (primaryTitan.StandeeSprite == null || primaryTitan.StandeeSprite.name.Contains("Dragon") || primaryTitan.UnitName.Contains("Dragon"))
                {
                    Sprite titanSprite = LoadBattlerSprite("magmadragon.png");
                    if (titanSprite != null)
                    {
                        primaryTitan.SetStandeeSprite(titanSprite);
                        primaryTitan.SetUnitName("Flame Titan");
                    }
                }

                if (primaryTitan.CurrentTile != null && primaryTitan.CurrentTile.CurrentOccupant == primaryTitan)
                {
                    primaryTitan.CurrentTile.CurrentOccupant = null;
                }
                primaryTitan.CurrentTile = null;
                primaryTitan.Revive();
                primaryTitan.ResetTurnActions();
                primaryTitan.gameObject.SetActive(false);
            }

            // Ensure our newly created allied vanguard roster exists in reserve
            EnsureAlliedRosterExists();
        }

        public static void EnsureAlliedRosterExists()
        {
            var allUnits = Object.FindObjectsByType<TacticalUnit3D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            bool hasBasalt = false;
            bool hasSiren = false;
            bool hasMagma = false;

            foreach (var u in allUnits)
            {
                if (u == null || u.Faction != UnitFaction.Player) continue;
                if (u.UnitName == "Basalt Vanguard") hasBasalt = true;
                if (u.UnitName == "Siren Sorceress") hasSiren = true;
                if (u.UnitName == "Magma Imp") hasMagma = true;
            }

            if (!hasBasalt)
            {
                Sprite sp = LoadBattlerSprite("BasaltVanguard.png");
                if (sp != null)
                {
                    TacticalUnit3D u = SpawnUnitStandee("Basalt Vanguard", UnitFaction.Player, sp, null, 14, 2, UnitArchetype.Minion, ElementalAffinity.Earth, 3, 0.85f);
                    if (u != null) { u.gameObject.SetActive(false); }
                }
            }

            if (!hasSiren)
            {
                Sprite sp = LoadBattlerSprite("SirenSorceress.png");
                if (sp != null)
                {
                    TacticalUnit3D u = SpawnUnitStandee("Siren Sorceress", UnitFaction.Player, sp, null, 8, 3, UnitArchetype.Minion, ElementalAffinity.Water, 2, 0.80f);
                    if (u != null) { u.gameObject.SetActive(false); }
                }
            }

            if (!hasMagma)
            {
                Sprite sp = LoadBattlerSprite("MagmaImp.png");
                if (sp != null)
                {
                    TacticalUnit3D u = SpawnUnitStandee("Magma Imp", UnitFaction.Player, sp, null, 6, 2, UnitArchetype.Minion, ElementalAffinity.Fire, 3, 0.70f);
                    if (u != null) { u.gameObject.SetActive(false); }
                }
            }
        }
    }
}

