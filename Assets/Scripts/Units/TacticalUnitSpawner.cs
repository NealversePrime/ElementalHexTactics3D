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

            // 1. Resources.Load (Standalone build & Editor)
            cachedRingSprite = Resources.Load<Sprite>("UnitRing_Circle");
            if (cachedRingSprite == null) cachedRingSprite = Resources.Load<Sprite>("Sprites/UnitRing_Circle");
            if (cachedRingSprite != null) return cachedRingSprite;

#if UNITY_EDITOR
            cachedRingSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UnitRing_Circle.png");
            if (cachedRingSprite != null) return cachedRingSprite;
#endif

            // 2. StreamingAssets fallback
            string streamingPath = Path.Combine(Application.streamingAssetsPath, "Sprites/UnitRing_Circle.png");
            if (File.Exists(streamingPath))
            {
                byte[] bytes = File.ReadAllBytes(streamingPath);
                Texture2D tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    tex.name = "UnitRing_Circle";
                    cachedRingSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    return cachedRingSprite;
                }
            }

            // 3. Application.dataPath fallback
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

            string baseName = Path.GetFileNameWithoutExtension(fileName);

            // 1. Resources.Load (Bundled inside standalone build & Editor)
            Sprite resSprite = Resources.Load<Sprite>("Battlers/" + baseName);
            if (resSprite == null) resSprite = Resources.Load<Sprite>("Sprites/Battlers/" + baseName);
            if (resSprite != null)
            {
                spriteCache[fileName] = resSprite;
                return resSprite;
            }

            Texture2D resTex = Resources.Load<Texture2D>("Battlers/" + baseName);
            if (resTex == null) resTex = Resources.Load<Texture2D>("Sprites/Battlers/" + baseName);
            if (resTex != null)
            {
                Sprite sp = Sprite.Create(resTex, new Rect(0, 0, resTex.width, resTex.height), new Vector2(0.5f, 0.5f), 100f);
                spriteCache[fileName] = sp;
                return sp;
            }

#if UNITY_EDITOR
            // 2. AssetDatabase (Editor fast path)
            string assetPath = "Assets/Sprites/Battlers/" + fileName;
            Sprite edSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (edSprite == null)
            {
                Object[] allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
                foreach (var a in allAssets)
                {
                    if (a is Sprite s)
                    {
                        edSprite = s;
                        break;
                    }
                }
            }
            if (edSprite != null)
            {
                spriteCache[fileName] = edSprite;
                return edSprite;
            }
#endif

            // 3. StreamingAssets (Always copied into build folder)
            string streamingPath = Path.Combine(Application.streamingAssetsPath, "Sprites/Battlers", fileName);
            if (File.Exists(streamingPath))
            {
                byte[] bytes = File.ReadAllBytes(streamingPath);
                Texture2D tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    tex.name = baseName;
                    Sprite sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    spriteCache[fileName] = sp;
                    return sp;
                }
            }

            // 4. Application.dataPath fallback
            string filePath = Path.Combine(Application.dataPath, "Sprites/Battlers", fileName);
            if (File.Exists(filePath))
            {
                byte[] bytes = File.ReadAllBytes(filePath);
                Texture2D tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    tex.name = baseName;
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

            if (!fileName.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
            {
                fileName += ".png";
            }

            string cacheKey = "Portrait_" + fileName;
            if (spriteCache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
            {
                return cached;
            }

            string baseName = Path.GetFileNameWithoutExtension(fileName);

            // 1. Resources.Load (Bundled inside standalone build & Editor)
            Sprite resSprite = Resources.Load<Sprite>("Portraits/" + baseName);
            if (resSprite == null) resSprite = Resources.Load<Sprite>("Sprites/Portraits/" + baseName);
            if (resSprite != null)
            {
                spriteCache[cacheKey] = resSprite;
                return resSprite;
            }

            Texture2D resTex = Resources.Load<Texture2D>("Portraits/" + baseName);
            if (resTex == null) resTex = Resources.Load<Texture2D>("Sprites/Portraits/" + baseName);
            if (resTex != null)
            {
                Sprite sp = Sprite.Create(resTex, new Rect(0, 0, resTex.width, resTex.height), new Vector2(0.5f, 0.5f), 100f);
                spriteCache[cacheKey] = sp;
                return sp;
            }

#if UNITY_EDITOR
            // 2. AssetDatabase
            string assetPath = "Assets/Sprites/Portraits/" + fileName;
            EnsureSpriteImporter(assetPath);
            Sprite edSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (edSprite == null)
            {
                Object[] allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
                foreach (var a in allAssets)
                {
                    if (a is Sprite s)
                    {
                        edSprite = s;
                        break;
                    }
                }
            }
            if (edSprite != null)
            {
                spriteCache[cacheKey] = edSprite;
                return edSprite;
            }
#endif

            // 3. StreamingAssets
            string streamingPath = Path.Combine(Application.streamingAssetsPath, "Sprites/Portraits", fileName);
            if (File.Exists(streamingPath))
            {
                byte[] bytes = File.ReadAllBytes(streamingPath);
                Texture2D tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    tex.name = baseName;
                    Sprite sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    spriteCache[cacheKey] = sp;
                    return sp;
                }
            }

            // 4. Application.dataPath fallback
            string filePath = Path.Combine(Application.dataPath, "Sprites/Portraits", fileName);
            if (File.Exists(filePath))
            {
                byte[] bytes = File.ReadAllBytes(filePath);
                Texture2D tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    tex.name = baseName;
                    Sprite sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    spriteCache[cacheKey] = sp;
                    return sp;
                }
            }

            Debug.LogWarning($"[TacticalUnitSpawner] Could not load portrait sprite: {fileName}");
            return null;
        }

        public static Sprite LoadBackgroundSprite(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;

            string cacheKey = "BG_" + fileName;
            if (spriteCache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
            {
                return cached;
            }

            string baseName = Path.GetFileNameWithoutExtension(fileName);

            // 1. Resources.Load
            Sprite resSprite = Resources.Load<Sprite>("Backgrounds/" + baseName);
            if (resSprite == null) resSprite = Resources.Load<Sprite>("Sprites/Backgrounds/" + baseName);
            if (resSprite != null)
            {
                spriteCache[cacheKey] = resSprite;
                return resSprite;
            }

            Texture2D resTex = Resources.Load<Texture2D>("Backgrounds/" + baseName);
            if (resTex == null) resTex = Resources.Load<Texture2D>("Sprites/Backgrounds/" + baseName);
            if (resTex != null)
            {
                Sprite sp = Sprite.Create(resTex, new Rect(0, 0, resTex.width, resTex.height), new Vector2(0.5f, 0.5f), 100f);
                spriteCache[cacheKey] = sp;
                return sp;
            }

#if UNITY_EDITOR
            string assetPath = "Assets/Sprites/Backgrounds/" + fileName;
            Sprite edSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (edSprite != null)
            {
                spriteCache[cacheKey] = edSprite;
                return edSprite;
            }
#endif

            // 3. StreamingAssets
            string streamingPath = Path.Combine(Application.streamingAssetsPath, "Sprites/Backgrounds", fileName);
            if (File.Exists(streamingPath))
            {
                byte[] bytes = File.ReadAllBytes(streamingPath);
                Texture2D tex = new Texture2D(512, 512, TextureFormat.RGB24, false);
                if (tex.LoadImage(bytes))
                {
                    tex.name = baseName;
                    Sprite sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    spriteCache[cacheKey] = sp;
                    return sp;
                }
            }

            return null;
        }

        public static Sprite LoadUISprite(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;

            string cacheKey = "UI_" + fileName;
            if (spriteCache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
            {
                return cached;
            }

            string baseName = Path.GetFileNameWithoutExtension(fileName);

            // 1. Resources.Load
            Sprite resSprite = Resources.Load<Sprite>("UI/" + baseName);
            if (resSprite == null) resSprite = Resources.Load<Sprite>("Sprites/UI/" + baseName);
            if (resSprite != null)
            {
                spriteCache[cacheKey] = resSprite;
                return resSprite;
            }

            Texture2D resTex = Resources.Load<Texture2D>("UI/" + baseName);
            if (resTex == null) resTex = Resources.Load<Texture2D>("Sprites/UI/" + baseName);
            if (resTex != null)
            {
                Vector4 border = Vector4.zero;
                if (fileName.Contains("WindowFrame")) border = new Vector4(24f, 24f, 24f, 24f);
                else if (fileName.Contains("SelectBox")) border = new Vector4(12f, 12f, 12f, 12f);
                else if (fileName.Contains("WindowBg")) border = new Vector4(12f, 12f, 12f, 12f);
                else if (fileName.Contains("Panel_Frame")) border = new Vector4(16f, 16f, 16f, 16f);
                Sprite sp = Sprite.Create(resTex, new Rect(0, 0, resTex.width, resTex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.Tight, border);
                spriteCache[cacheKey] = sp;
                return sp;
            }

#if UNITY_EDITOR
            string assetPath = "Assets/Sprites/UI/" + fileName;
            Sprite edSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (edSprite != null)
            {
                spriteCache[cacheKey] = edSprite;
                return edSprite;
            }
#endif

            // 3. StreamingAssets
            string streamingPath = Path.Combine(Application.streamingAssetsPath, "Sprites/UI", fileName);
            if (File.Exists(streamingPath))
            {
                byte[] bytes = File.ReadAllBytes(streamingPath);
                Texture2D tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    Vector4 border = Vector4.zero;
                    if (fileName.Contains("WindowFrame")) border = new Vector4(24f, 24f, 24f, 24f);
                    else if (fileName.Contains("SelectBox")) border = new Vector4(12f, 12f, 12f, 12f);
                    else if (fileName.Contains("WindowBg")) border = new Vector4(12f, 12f, 12f, 12f);
                    else if (fileName.Contains("Panel_Frame")) border = new Vector4(16f, 16f, 16f, 16f);
                    else if (fileName.Contains("Button")) border = new Vector4(8f, 8f, 8f, 8f);

                    Sprite sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
                    spriteCache[cacheKey] = sp;
                    return sp;
                }
            }

            return null;
        }

#if UNITY_EDITOR
        private static void EnsureSpriteImporter(string path)
        {
            UnityEditor.TextureImporter importer = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
            if (importer != null && importer.textureType != UnityEditor.TextureImporterType.Sprite)
            {
                importer.textureType = UnityEditor.TextureImporterType.Sprite;
                importer.SaveAndReimport();
            }
        }
#endif

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
            if (lower.Contains("imp") || lower.Contains("saboteur"))
                return LoadPortraitSprite("magmaimp.png");

            // Titans & Wild Monsters Roster (Specific Titans First)
            if (lower.Contains("sea") || lower.Contains("leviathan") || lower.Contains("sealeviathan") || lower.Contains("water titan"))
                return LoadPortraitSprite("sealeviathanportrait.png");
            if (lower.Contains("earth") || lower.Contains("behemoth") || lower.Contains("earthbehemoth") || lower.Contains("earth titan") || lower.Contains("terra"))
                return LoadPortraitSprite("earthbehemothportrait.png");
            if (lower.Contains("shadow") || lower.Contains("hound") || lower.Contains("shadowhound") || lower.Contains("wolf") || lower.Contains("alpha") || lower.Contains("stalker"))
                return LoadPortraitSprite("shadowhoundportrait.png");
            if (lower.Contains("dragon") || lower.Contains("magmadragon") || lower.Contains("flame") || lower.Contains("magma") || lower.Contains("titan"))
                return LoadPortraitSprite("magmadragonportrait.png");

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
            Sprite portrait = GetPortraitForUnit(unitName);
            if (portrait != null) unit.SetPortraitSprite(portrait);

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
                if (primaryTitan.StandeeSprite == null || primaryTitan.StandeeSprite.name.Contains("Dragon") || primaryTitan.UnitName.Contains("Dragon") || primaryTitan.StandeeSprite.name.Contains("FlameFrost"))
                {
                    Sprite titanSprite = LoadBattlerSprite("magmadragon.png");
                    if (titanSprite != null)
                    {
                        primaryTitan.SetStandeeSprite(titanSprite);
                        primaryTitan.SetUnitName("Flame Titan");
                        Transform st = primaryTitan.transform.Find("Standee");
                        if (st != null) st.localScale = Vector3.one * 1.15f;
                    }
                    Sprite pSprite = LoadPortraitSprite("magmadragonportrait.png");
                    if (pSprite != null)
                    {
                        primaryTitan.SetPortraitSprite(pSprite);
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
                if (u.UnitName == "Earth Golem" || u.UnitName == "Basalt Vanguard") hasBasalt = true;
                if (u.UnitName == "Siren Sorceress") hasSiren = true;
                if (u.UnitName == "Magma Imp") hasMagma = true;
            }

            if (!hasBasalt)
            {
                Sprite sp = LoadBattlerSprite("BasaltVanguard.png");
                if (sp != null)
                {
                    TacticalUnit3D u = SpawnUnitStandee("Earth Golem", UnitFaction.Player, sp, null, 16, 2, UnitArchetype.Titan, ElementalAffinity.Earth, 3, 0.85f);
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

