using System;

namespace ElementalHexTactics3D.Campaign
{
    /// <summary>
    /// Serializable POCO data container representing a saved tactical campaign run.
    /// Persisted cleanly as JSON in Application.persistentDataPath.
    /// </summary>
    [Serializable]
    public class CampaignSaveData
    {
        public int saveVersion = 1;
        public string saveTimestamp = "";

        // Calendar & Crusade Doom Clock
        public int currentDay = 1;
        public int crusadeArrivalDay = 25;
        public int food = 50;
        public int dailyFoodUpkeep = 5;

        // Domain Resources (TownHubManager)
        public int manaCrystals = 450;
        public int soulEmbers = 180;
        public int freedOutcasts = 16;

        // Trilemma Seed
        public int trilemmaSeed = 0;

        // Run Statistics & History
        public int totalVictories = 0;
        public int totalDefeats = 0;
    }
}
