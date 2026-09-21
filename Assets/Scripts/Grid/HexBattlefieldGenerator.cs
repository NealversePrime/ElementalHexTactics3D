using System;
using System.Collections.Generic;
using UnityEngine;
using ElementalHexTactics3D.Campaign;
using ElementalHexTactics3D.Units;

namespace ElementalHexTactics3D.Grid
{
    public class GeneratedTileSpec
    {
        public HexCoordinates Coordinates;
        public int ElevationTier; // 0, 1, or 2
        public TileState State;
        public int TierLevel;     // 1 or 2
        public bool IsPillarObstacle;
    }

    public class GeneratedEnemySpec
    {
        public HexCoordinates Coordinates;
        public string UnitName;
        public string BattlerSpriteName;
        public UnitArchetype Archetype;
        public ElementalAffinity Affinity;
        public int HP;
        public int Range;
        public int BaseAtk;
        public float Scale;
    }

    public class GeneratedBattlefieldData
    {
        public ExpeditionMissionData Mission;
        public int Seed;
        public int Radius;
        public Dictionary<HexCoordinates, GeneratedTileSpec> Tiles = new Dictionary<HexCoordinates, GeneratedTileSpec>();
        public List<GeneratedEnemySpec> Enemies = new List<GeneratedEnemySpec>();
        public List<HexCoordinates> CandidateRiftTiles = new List<HexCoordinates>();
    }

    /// <summary>
    /// Pure C# procedural generation engine that computes organic 3D hex elevations,
    /// elemental hazard biomes, and fair enemy encounters.
    /// </summary>
    public static class HexBattlefieldGenerator
    {
        public static GeneratedBattlefieldData Generate(ExpeditionMissionData mission, int radius = 4)
        {
            int seed = (mission != null && mission.Seed != 0) ? mission.Seed : Environment.TickCount;
            System.Random rng = new System.Random(seed);

            GeneratedBattlefieldData data = new GeneratedBattlefieldData
            {
                Mission = mission,
                Seed = seed,
                Radius = radius
            };

            // 1. Generate Hex Coordinates inside radial boundary
            HexCoordinates center = new HexCoordinates(0, 0);
            List<HexCoordinates> allCoords = center.GetRange(radius);

            // 2. Generate Organic Elevation Tiers (0, 1, 2)
            float noiseScale = 0.35f;
            float offsetX = (float)(rng.NextDouble() * 100f);
            float offsetZ = (float)(rng.NextDouble() * 100f);

            foreach (var coord in allCoords)
            {
                int dist = coord.DistanceTo(center);
                float normalizedDist = dist / (float)radius;

                // Deterministic pseudo-noise
                float noiseVal = SampleNoise(coord.Q * noiseScale + offsetX, coord.R * noiseScale + offsetZ);

                // Elevation bias: slightly lower on the player side (r < 0) and slightly elevated on the enemy side (r > 0)
                float elevationBias = (coord.R > 0 ? 0.12f : -0.08f) - (normalizedDist * 0.15f);
                float rawHeight = Mathf.Clamp01(noiseVal + elevationBias);

                int elev = 1; // Default ground
                if (rawHeight < 0.28f) elev = 0; // Basins & trenches
                else if (rawHeight > 0.68f) elev = 2; // High plateaus

                data.Tiles[coord] = new GeneratedTileSpec
                {
                    Coordinates = coord,
                    ElevationTier = elev,
                    State = TileState.Barren,
                    TierLevel = 0,
                    IsPillarObstacle = false
                };
            }

            // 3. Smooth Elevations with Cellular Automata (2 passes of 6-hex majority)
            SmoothElevations(data, allCoords, passes: 2);

            // 4. Assign Biome States & Elemental Hazards
            AssignBiomesAndHazards(data, allCoords, mission, rng);

            // 5. Gather Candidate Player Rift Tiles (South side: r <= -1, safe ground)
            foreach (var coord in allCoords)
            {
                if (coord.R <= -1 && data.Tiles.TryGetValue(coord, out var tile))
                {
                    if (!tile.IsPillarObstacle && tile.State != TileState.Magma && !(tile.State == TileState.Water && tile.TierLevel >= 2))
                    {
                        data.CandidateRiftTiles.Add(coord);
                    }
                }
            }

            // 6. Spawn Dynamic Enemy Squad
            SpawnEnemySquad(data, allCoords, mission, rng);

            return data;
        }

        private static void SmoothElevations(GeneratedBattlefieldData data, List<HexCoordinates> coords, int passes)
        {
            for (int p = 0; p < passes; p++)
            {
                Dictionary<HexCoordinates, int> nextTiers = new Dictionary<HexCoordinates, int>();

                foreach (var coord in coords)
                {
                    int[] counts = new int[3];
                    var neighbors = coord.GetNeighbors();

                    foreach (var n in neighbors)
                    {
                        if (data.Tiles.TryGetValue(n, out var neighborSpec))
                        {
                            counts[Mathf.Clamp(neighborSpec.ElevationTier, 0, 2)]++;
                        }
                    }

                    int current = data.Tiles[coord].ElevationTier;
                    int adopted = current;

                    // If 4 or more neighbors have a specific elevation, adopt it!
                    for (int t = 0; t < 3; t++)
                    {
                        if (counts[t] >= 4)
                        {
                            adopted = t;
                            break;
                        }
                    }

                    nextTiers[coord] = adopted;
                }

                foreach (var kvp in nextTiers)
                {
                    data.Tiles[kvp.Key].ElevationTier = kvp.Value;
                }
            }
        }

        private static void AssignBiomesAndHazards(GeneratedBattlefieldData data, List<HexCoordinates> coords, ExpeditionMissionData mission, System.Random rng)
        {
            BiomeTheme theme = mission != null ? mission.Biome : BiomeTheme.VerdantHighlands;
            StageModifier modifier = mission != null ? mission.Modifier : StageModifier.None;

            bool isVolcanic = (theme == BiomeTheme.VolcanicRupture || modifier == StageModifier.VolcanicSurge);
            bool isDeluge = (theme == BiomeTheme.FloodedBasin || modifier == StageModifier.HeavyDeluge);
            bool isFortress = (theme == BiomeTheme.AncientCrusadeRuins || modifier == StageModifier.StoneFortress);

            foreach (var coord in coords)
            {
                var tile = data.Tiles[coord];
                int elev = tile.ElevationTier;

                if (isVolcanic)
                {
                    if (elev == 0)
                    {
                        tile.State = (rng.NextDouble() < 0.65) ? TileState.Magma : TileState.Scorched;
                        tile.TierLevel = (tile.State == TileState.Magma) ? 2 : 1;
                    }
                    else if (elev == 2)
                    {
                        tile.State = TileState.Scorched;
                        tile.TierLevel = 1;
                    }
                    else
                    {
                        double roll = rng.NextDouble();
                        if (roll < 0.35) { tile.State = TileState.Scorched; tile.TierLevel = 1; }
                        else if (roll < 0.50 && coord.R > 0) { tile.State = TileState.Magma; tile.TierLevel = 1; }
                        else { tile.State = TileState.Barren; tile.TierLevel = 0; }
                    }
                }
                else if (isDeluge)
                {
                    if (elev == 0)
                    {
                        // Deep Water (push-drown lethal hazard) or Shallow Water
                        tile.State = TileState.Water;
                        tile.TierLevel = (rng.NextDouble() < 0.45) ? 2 : 1;
                    }
                    else if (elev == 1)
                    {
                        double roll = rng.NextDouble();
                        if (roll < 0.30) { tile.State = TileState.Water; tile.TierLevel = 1; }
                        else if (roll < 0.55) { tile.State = TileState.Mud; tile.TierLevel = 1; }
                        else { tile.State = TileState.Grass; tile.TierLevel = 1; }
                    }
                    else
                    {
                        tile.State = TileState.Grass;
                        tile.TierLevel = 1;
                    }
                }
                else if (isFortress)
                {
                    // Ruins with stone pillar cover
                    if (elev == 2 && rng.NextDouble() < 0.25)
                    {
                        tile.State = TileState.StonePillar;
                        tile.TierLevel = 1;
                        tile.IsPillarObstacle = true;
                    }
                    else if (elev == 1 && rng.NextDouble() < 0.12 && coord.R >= 0)
                    {
                        tile.State = TileState.StonePillar;
                        tile.TierLevel = 1;
                        tile.IsPillarObstacle = true;
                    }
                    else if (elev == 0)
                    {
                        tile.State = TileState.Mud;
                        tile.TierLevel = 1;
                    }
                    else
                    {
                        tile.State = (rng.NextDouble() < 0.55) ? TileState.Barren : TileState.Grass;
                        tile.TierLevel = 1;
                    }
                }
                else // Default Verdant Highlands
                {
                    if (elev == 0)
                    {
                        tile.State = (rng.NextDouble() < 0.6) ? TileState.Water : TileState.Mud;
                        tile.TierLevel = 1;
                    }
                    else
                    {
                        tile.State = (rng.NextDouble() < 0.75) ? TileState.Grass : TileState.Barren;
                        tile.TierLevel = 1;
                    }

                    // Scattered stone pillar on high plateaus
                    if (elev == 2 && rng.NextDouble() < 0.15)
                    {
                        tile.State = TileState.StonePillar;
                        tile.TierLevel = 1;
                        tile.IsPillarObstacle = true;
                    }
                }
            }
        }

        private static void SpawnEnemySquad(GeneratedBattlefieldData data, List<HexCoordinates> coords, ExpeditionMissionData mission, System.Random rng)
        {
            // Enemy territory is the northern hemisphere (coord.R >= 1)
            List<HexCoordinates> candidateTiles = new List<HexCoordinates>();

            foreach (var c in coords)
            {
                if (c.R >= 1 && data.Tiles.TryGetValue(c, out var t))
                {
                    if (!t.IsPillarObstacle && t.State != TileState.Magma && !(t.State == TileState.Water && t.TierLevel >= 2))
                    {
                        candidateTiles.Add(c);
                    }
                }
            }

            if (candidateTiles.Count == 0) return;

            // Sort candidates for Boss: prioritize distance from south (r >= 2) and high elevation
            candidateTiles.Sort((a, b) =>
            {
                float scoreA = a.R * 2f + data.Tiles[a].ElevationTier * 3f;
                float scoreB = b.R * 2f + data.Tiles[b].ElevationTier * 3f;
                return scoreB.CompareTo(scoreA);
            });

            HexCoordinates bossCoord = candidateTiles[0];
            candidateTiles.RemoveAt(0);

            // 1. Create Boss / Commander
            MissionArchetype arch = (mission != null) ? mission.Archetype : MissionArchetype.VanguardSabotage;
            int threat = (mission != null) ? mission.ThreatLevel : 2;

            GeneratedEnemySpec boss = new GeneratedEnemySpec();
            boss.Coordinates = bossCoord;

            if (arch == MissionArchetype.WildTitanHunt)
            {
                string[] dragonSprites = new string[] { "Sun Dragon.png", "Blue Dragon.png", "Shadow Dragon.png", "Green Dragon.png" };
                boss.BattlerSpriteName = dragonSprites[rng.Next(dragonSprites.Length)];
                boss.UnitName = (boss.BattlerSpriteName.Contains("Sun")) ? "Sun Dragon Titan" :
                               (boss.BattlerSpriteName.Contains("Blue")) ? "Abyssal Sea Dragon" :
                               (boss.BattlerSpriteName.Contains("Shadow")) ? "Void Shadow Dragon" : "Primordial Drake";
                boss.Archetype = UnitArchetype.Titan;
                boss.Affinity = boss.BattlerSpriteName.Contains("Sun") ? ElementalAffinity.Fire :
                                boss.BattlerSpriteName.Contains("Blue") ? ElementalAffinity.Water : ElementalAffinity.None;
                boss.HP = 16 + threat * 2;
                boss.Range = 2;
                boss.BaseAtk = 4 + (threat >= 4 ? 1 : 0);
                boss.Scale = 1.05f;
            }
            else if (arch == MissionArchetype.VanguardSabotage)
            {
                boss.BattlerSpriteName = "Dracomancer.png";
                boss.UnitName = "Inquisitor Dracomancer";
                boss.Archetype = UnitArchetype.Commander;
                boss.Affinity = ElementalAffinity.Fire;
                boss.HP = 12 + threat * 2;
                boss.Range = 3;
                boss.BaseAtk = 3 + (threat >= 3 ? 1 : 0);
                boss.Scale = 0.85f;
            }
            else
            {
                string[] bossSprites = new string[] { "Emperor Slime.png", "Father Slime.png", "Dracomancer.png" };
                boss.BattlerSpriteName = bossSprites[rng.Next(bossSprites.Length)];
                boss.UnitName = boss.BattlerSpriteName.Contains("Emperor") ? "Emperor Slime" :
                                boss.BattlerSpriteName.Contains("Father") ? "Grand Patriarch Slime" : "Outpost Commander";
                boss.Archetype = UnitArchetype.Commander;
                boss.Affinity = ElementalAffinity.None;
                boss.HP = 10 + threat * 2;
                boss.Range = 2;
                boss.BaseAtk = 3;
                boss.Scale = 0.85f;
            }

            data.Enemies.Add(boss);

            // 2. Spawn 1-3 Minions/Escorts
            int minionCount = Mathf.Clamp(1 + (threat / 2), 1, 3);
            string[] minionSprites = new string[] { "Demon Slime.png", "Sword Slime.png", "Bat Slime.png", "Horn Slime.png", "Spikey Slime.png", "Turtle Slime.png" };

            for (int i = 0; i < minionCount && candidateTiles.Count > 0; i++)
            {
                // Score minion candidates: favor positions near hazards or stone pillars for push combos!
                candidateTiles.Sort((a, b) =>
                {
                    int hazA = CountAdjacentHazards(a, data);
                    int hazB = CountAdjacentHazards(b, data);
                    return hazB.CompareTo(hazA);
                });

                int pickIdx = rng.Next(Mathf.Min(candidateTiles.Count, 3));
                HexCoordinates mCoord = candidateTiles[pickIdx];
                candidateTiles.RemoveAt(pickIdx);

                string mSprite = minionSprites[rng.Next(minionSprites.Length)];
                string mName = mSprite.Replace(".png", "");

                data.Enemies.Add(new GeneratedEnemySpec
                {
                    Coordinates = mCoord,
                    UnitName = mName,
                    BattlerSpriteName = mSprite,
                    Archetype = UnitArchetype.Minion,
                    Affinity = ElementalAffinity.None,
                    HP = 6 + (threat >= 3 ? 2 : 0),
                    Range = 2,
                    BaseAtk = 2,
                    Scale = 0.70f
                });
            }
        }

        private static int CountAdjacentHazards(HexCoordinates c, GeneratedBattlefieldData data)
        {
            int count = 0;
            foreach (var n in c.GetNeighbors())
            {
                if (data.Tiles.TryGetValue(n, out var t))
                {
                    if (t.State == TileState.Magma || t.State == TileState.Water || t.IsPillarObstacle)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        private static float SampleNoise(float x, float z)
        {
            float n1 = Mathf.Sin(x * 12.9898f + z * 78.233f) * 43758.5453f;
            float f1 = n1 - Mathf.Floor(n1);
            float n2 = Mathf.Cos(x * 39.346f + z * 11.135f) * 23421.631f;
            float f2 = n2 - Mathf.Floor(n2);
            return (f1 + f2) * 0.5f;
        }
    }
}

