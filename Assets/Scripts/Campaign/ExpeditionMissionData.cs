using System;
using UnityEngine;

namespace ElementalHexTactics3D.Campaign
{
    public enum MissionArchetype
    {
        RescueRecruit,
        ResourceScavenge,
        VanguardSabotage,
        WildTitanHunt
    }

    public enum BiomeTheme
    {
        VerdantHighlands,
        VolcanicRupture,
        FloodedBasin,
        AncientCrusadeRuins
    }

    public enum StageModifier
    {
        None,
        VolcanicSurge,
        HeavyDeluge,
        DenseMist,
        StoneFortress
    }

    /// <summary>
    /// Holds the definition, environmental parameters, enemy composition,
    /// and domain rewards for a procedurally generated expedition incursion.
    /// </summary>
    [Serializable]
    public class ExpeditionMissionData
    {
        public string MissionId;
        public string Title;
        public string Description;
        public MissionArchetype Archetype;
        public int ThreatLevel; // 1 to 5 Stars
        public BiomeTheme Biome;
        public StageModifier Modifier;
        public int Seed;

        [Header("Domain Rewards")]
        public int RewardMana;
        public int RewardEmbers;
        public int RewardOutcasts;
        public int RewardDoomClockDays;

        public string GetArchetypeName()
        {
            switch (Archetype)
            {
                case MissionArchetype.RescueRecruit:
                    return "⛓️ Slave Convoy Ambush";
                case MissionArchetype.ResourceScavenge:
                    return "⛏️ Crystal Vein Raid";
                case MissionArchetype.VanguardSabotage:
                    return "⏳ Vanguard Sabotage";
                case MissionArchetype.WildTitanHunt:
                    return "🗿 Wild Titan Hunt";
                default:
                    return "⚔️ Expedition Incursion";
            }
        }

        public string GetThreatStars()
        {
            int filled = Mathf.Clamp(ThreatLevel, 1, 5);
            return new string('★', filled) + new string('☆', 5 - filled);
        }

        public string GetModifierTag()
        {
            switch (Modifier)
            {
                case StageModifier.VolcanicSurge:
                    return "🌋 Volcanic Surge (+50% Magma Hazards)";
                case StageModifier.HeavyDeluge:
                    return "🌊 Heavy Deluge (Water Basins Expand)";
                case StageModifier.DenseMist:
                    return "🌫️ Dense Mist (Max Range Capped)";
                case StageModifier.StoneFortress:
                    return "🏰 Stone Fortress (+Dense Cover Pillars)";
                default:
                    return "✨ Normal Conditions";
            }
        }

        public string GetRewardsSummary()
        {
            string s = "";
            if (RewardMana > 0) s += $"<color=#00E5FF>💎 +{RewardMana} Mana</color>  ";
            if (RewardEmbers > 0) s += $"<color=#FF7043>🔥 +{RewardEmbers} Embers</color>  ";
            if (RewardOutcasts > 0) s += $"<color=#81C784>👥 +{RewardOutcasts} Outcasts</color>  ";
            if (RewardDoomClockDays > 0) s += $"<color=#FFD54F>⏳ +{RewardDoomClockDays}d Crusade Delay</color>";
            return s.Trim();
        }
    }
}

