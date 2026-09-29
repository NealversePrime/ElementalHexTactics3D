using System;
using System.Collections.Generic;
using UnityEngine;

namespace ElementalHexTactics3D.Campaign
{
    /// <summary>
    /// Generates 3 contrasting expedition cards per cycle (the Expedition Trilemma)
    /// based on rogue-lite progression heuristics (anti-clustering, orthogonal modifiers,
    /// dynamic reward scaling).
    /// </summary>
    public static class ExpeditionTrilemmaGenerator
    {
        private static ExpeditionMissionData currentActiveMission;

        public static ExpeditionMissionData CurrentActiveMission
        {
            get
            {
                if (currentActiveMission == null)
                {
                    currentActiveMission = GenerateQuickSkirmish(UnityEngine.Random.Range(1000, 99999));
                }
                return currentActiveMission;
            }
            set => currentActiveMission = value;
        }

        /// <summary>
        /// Generates a set of 3 contrasting mission cards for the given campaign day/cycle.
        /// Incorporates rogue-lite guardrails (guaranteed sabotage when doom clock is low,
        /// guaranteed food when famine threatens, day-based threat scaling).
        /// </summary>
        public static List<ExpeditionMissionData> GenerateTrilemma(int cycle = 1)
        {
            List<ExpeditionMissionData> cards = new List<ExpeditionMissionData>();

            int day = (CampaignManager.Instance != null) ? CampaignManager.Instance.CurrentDay : cycle;
            int daysUntilCrusade = (CampaignManager.Instance != null) ? CampaignManager.Instance.DaysUntilCrusade : 24;
            int currentFood = (CampaignManager.Instance != null) ? CampaignManager.Instance.Food : 50;

            int baseSeed = Environment.TickCount + day * 37 + cycle * 13;
            System.Random rng = new System.Random(baseSeed);

            // Compute threat level based on campaign progression
            int baseThreat = (day <= 8) ? 1 : (day <= 18) ? 2 : 3;

            // Card 1: Resource Scavenge / Crystal & Ration Vein Raid (Economy & Food)
            int seed1 = rng.Next(10000, 99999);
            bool lowFoodAlert = currentFood < 20;
            int foodReward = lowFoodAlert ? (25 + rng.Next(20)) : (15 + rng.Next(15));

            cards.Add(new ExpeditionMissionData
            {
                MissionId = $"EXP_{day}_SCAVENGE",
                Title = lowFoodAlert ? "Emergency Supply Depot Raid" : "Crystal Vein Extraction",
                Description = lowFoodAlert 
                    ? "Imperial supply wagons loaded with grain and cured meats. Seize rations before Citadel famine strikes!"
                    : "A subterranean mana fissure exposed by crusader drilling. Raid before their supply train arrives.",
                Archetype = MissionArchetype.ResourceScavenge,
                ThreatLevel = Mathf.Clamp(baseThreat, 1, 3),
                Biome = (seed1 % 2 == 0) ? BiomeTheme.FloodedBasin : BiomeTheme.VerdantHighlands,
                Modifier = (seed1 % 3 == 0) ? StageModifier.HeavyDeluge : StageModifier.None,
                Seed = seed1,
                RewardMana = 90 + rng.Next(40),
                RewardEmbers = 25 + rng.Next(15),
                RewardOutcasts = 1 + rng.Next(2),
                RewardFood = foodReward,
                RewardDoomClockDays = 0
            });

            // Card 2: Slave Convoy Ambush / Rescue (Workforce & Recruits)
            int seed2 = rng.Next(10000, 99999);
            cards.Add(new ExpeditionMissionData
            {
                MissionId = $"EXP_{day}_RESCUE",
                Title = "Slave Convoy Ambush",
                Description = "Holy crusaders transporting caged beastkin and demonic outcasts across the rocky crags. Ambush the escort!",
                Archetype = MissionArchetype.RescueRecruit,
                ThreatLevel = Mathf.Clamp(baseThreat + 1, 2, 4),
                Biome = (seed2 % 2 == 0) ? BiomeTheme.AncientCrusadeRuins : BiomeTheme.VerdantHighlands,
                Modifier = (seed2 % 3 == 0) ? StageModifier.StoneFortress : StageModifier.None,
                Seed = seed2,
                RewardMana = 40 + rng.Next(20),
                RewardEmbers = 40 + rng.Next(20),
                RewardOutcasts = 3 + rng.Next(3),
                RewardFood = 5 + rng.Next(10),
                RewardDoomClockDays = 0
            });

            // Card 3: High Threat - Vanguard Sabotage OR Wild Titan Hunt
            // GUARDRAIL: If Crusade is 3 days or fewer away, ALWAYS offer Vanguard Sabotage!
            int seed3 = rng.Next(10000, 99999);
            bool forceSabotage = (daysUntilCrusade <= 3);
            bool isTitanHunt = !forceSabotage && ((day % 2 == 0) || (rng.Next(100) < 45));

            if (isTitanHunt)
            {
                cards.Add(new ExpeditionMissionData
                {
                    MissionId = $"EXP_{day}_TITAN_HUNT",
                    Title = "Ancient Dragon Apex Incursion",
                    Description = "A primordial dragon slumbering near a volatile rift. Slay or dominate the beast for supreme power!",
                    Archetype = MissionArchetype.WildTitanHunt,
                    ThreatLevel = Mathf.Clamp(baseThreat + 2, 3, 5),
                    Biome = BiomeTheme.VolcanicRupture,
                    Modifier = StageModifier.VolcanicSurge,
                    Seed = seed3,
                    RewardMana = 150 + rng.Next(60),
                    RewardEmbers = 120 + rng.Next(50),
                    RewardOutcasts = 0,
                    RewardFood = 10 + rng.Next(10),
                    RewardDoomClockDays = 1
                });
            }
            else
            {
                cards.Add(new ExpeditionMissionData
                {
                    MissionId = $"EXP_{day}_SABOTAGE",
                    Title = forceSabotage ? "⚠️ CRITICAL: Vanguard Encampment Assault" : "Crusader Vanguard Outpost Sabotage",
                    Description = forceSabotage
                        ? "The Crusade vanguard is at our gates! Slay their vanguard captains immediately to buy the Citadel crucial days!"
                        : "Holy Inquisitors constructing siege ballistas. Infiltrate their camp, execute their officers, and stall the crusade.",
                    Archetype = MissionArchetype.VanguardSabotage,
                    ThreatLevel = Mathf.Clamp(baseThreat + 1, 2, 4),
                    Biome = BiomeTheme.AncientCrusadeRuins,
                    Modifier = (seed3 % 2 == 0) ? StageModifier.DenseMist : StageModifier.None,
                    Seed = seed3,
                    RewardMana = 75 + rng.Next(30),
                    RewardEmbers = 80 + rng.Next(30),
                    RewardOutcasts = 1 + rng.Next(2),
                    RewardFood = 10 + rng.Next(10),
                    RewardDoomClockDays = forceSabotage ? 3 : 2 // Bonus delay when urgent!
                });
            }

            return cards;
        }

        /// <summary>
        /// Generates a quick single mission profile for instant skirmish testing.
        /// </summary>
        public static ExpeditionMissionData GenerateQuickSkirmish(int seed)
        {
            System.Random rng = new System.Random(seed);
            MissionArchetype[] archs = (MissionArchetype[])Enum.GetValues(typeof(MissionArchetype));
            MissionArchetype arch = archs[rng.Next(archs.Length)];

            BiomeTheme[] biomes = (BiomeTheme[])Enum.GetValues(typeof(BiomeTheme));
            BiomeTheme biome = biomes[rng.Next(biomes.Length)];

            StageModifier[] mods = (StageModifier[])Enum.GetValues(typeof(StageModifier));
            StageModifier mod = mods[rng.Next(mods.Length)];

            int threat = 1 + rng.Next(3);

            return new ExpeditionMissionData
            {
                MissionId = $"SKIRMISH_{seed}",
                Title = $"Tactical Incursion: Sector {seed % 1000:D3}",
                Description = "Randomized tactical encounter with dynamic terrain and hostile forces.",
                Archetype = arch,
                ThreatLevel = threat,
                Biome = biome,
                Modifier = mod,
                Seed = seed,
                RewardMana = 50 + rng.Next(50),
                RewardEmbers = 40 + rng.Next(40),
                RewardOutcasts = 2 + rng.Next(2),
                RewardDoomClockDays = (arch == MissionArchetype.VanguardSabotage) ? 1 : 0
            };
        }
    }
}

