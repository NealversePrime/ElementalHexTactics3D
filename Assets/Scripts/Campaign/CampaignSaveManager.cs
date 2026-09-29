using System;
using System.IO;
using UnityEngine;
using ElementalHexTactics3D.UI.Hub;

namespace ElementalHexTactics3D.Campaign
{
    /// <summary>
    /// Master persistent storage utility for saving and loading the rogue-lite campaign run.
    /// Provides atomic JSON serialization, fallback backup protection, and direct synchronization
    /// across CampaignManager, TownHubManager, and ExpeditionPortalModalUI.
    /// </summary>
    public static class CampaignSaveManager
    {
        private const string SAVE_FILE_NAME = "campaign_save.json";
        private const string BACKUP_FILE_NAME = "campaign_save.bak";

        public static string SaveFilePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
        public static string BackupFilePath => Path.Combine(Application.persistentDataPath, BACKUP_FILE_NAME);

        /// <summary>
        /// Returns true if a valid campaign save file exists on disk.
        /// </summary>
        public static bool HasSaveFile()
        {
            return File.Exists(SaveFilePath) || File.Exists(BackupFilePath);
        }

        /// <summary>
        /// Saves current campaign, domain economy, and calendar state to JSON.
        /// Uses atomic writing (temp -> backup -> target) to prevent corrupted files.
        /// </summary>
        public static bool SaveCurrentCampaign()
        {
            try
            {
                CampaignSaveData data = new CampaignSaveData();
                data.saveVersion = 1;
                data.saveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                if (CampaignManager.Instance != null)
                {
                    data.currentDay = CampaignManager.Instance.CurrentDay;
                    data.crusadeArrivalDay = CampaignManager.Instance.CrusadeArrivalDay;
                    data.food = CampaignManager.Instance.Food;
                    data.dailyFoodUpkeep = CampaignManager.Instance.DailyFoodUpkeep;
                    data.totalVictories = CampaignManager.Instance.TotalVictories;
                    data.totalDefeats = CampaignManager.Instance.TotalDefeats;
                }

                if (TownHubManager.Instance != null)
                {
                    data.manaCrystals = TownHubManager.Instance.ManaCrystals;
                    data.soulEmbers = TownHubManager.Instance.SoulEmbers;
                    data.freedOutcasts = TownHubManager.Instance.FreedOutcasts;
                }

                if (ExpeditionPortalModalUI.Instance != null)
                {
                    data.trilemmaSeed = CampaignManager.Instance != null ? CampaignManager.Instance.CurrentDay : 1;
                }

                string json = JsonUtility.ToJson(data, true);
                string tempPath = SaveFilePath + ".tmp";

                File.WriteAllText(tempPath, json);

                // Safe atomic replace
                if (File.Exists(SaveFilePath))
                {
                    if (File.Exists(BackupFilePath)) File.Delete(BackupFilePath);
                    File.Copy(SaveFilePath, BackupFilePath);
                    File.Delete(SaveFilePath);
                }

                File.Move(tempPath, SaveFilePath);
                Debug.Log($"<color=#81C784><b>[Save System]</b></color> Campaign run auto-saved successfully! (Day {data.currentDay} | Crusade in {data.crusadeArrivalDay - data.currentDay} Days)");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Save System] Failed to save campaign run: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Reads and deserializes campaign data from disk.
        /// </summary>
        public static CampaignSaveData LoadSaveData()
        {
            if (File.Exists(SaveFilePath))
            {
                try
                {
                    string json = File.ReadAllText(SaveFilePath);
                    CampaignSaveData data = JsonUtility.FromJson<CampaignSaveData>(json);
                    if (data != null && data.currentDay > 0) return data;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Save System] Main save corrupt, attempting backup fallback: {ex.Message}");
                }
            }

            // Fallback backup
            if (File.Exists(BackupFilePath))
            {
                try
                {
                    string bkpJson = File.ReadAllText(BackupFilePath);
                    CampaignSaveData bkpData = JsonUtility.FromJson<CampaignSaveData>(bkpJson);
                    if (bkpData != null && bkpData.currentDay > 0)
                    {
                        Debug.Log("<color=#FFD54F><b>[Save System]</b></color> Successfully recovered campaign run from backup file!");
                        return bkpData;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Save System] Backup recovery failed: {ex.Message}");
                }
            }

            return null;
        }

        /// <summary>
        /// Loads save data from disk and synchronizes all campaign and hub singletons.
        /// </summary>
        public static bool LoadAndApplySave()
        {
            CampaignSaveData data = LoadSaveData();
            if (data == null)
            {
                Debug.Log("[Save System] No existing campaign save found. Starting fresh run on Day 1.");
                return false;
            }

            if (CampaignManager.Instance != null)
            {
                CampaignManager.Instance.LoadFromSaveData(data);
            }

            if (TownHubManager.Instance != null)
            {
                TownHubManager.Instance.SetResources(data.manaCrystals, data.soulEmbers, data.freedOutcasts);
            }

            Debug.Log($"<color=#81C784><b>[Save System]</b></color> Campaign state restored! Day {data.currentDay}, Food {data.food}, Crusade in {data.crusadeArrivalDay - data.currentDay} Days.");
            return true;
        }

        /// <summary>
        /// Deletes persistent save files to wipe the run and start fresh.
        /// </summary>
        public static void DeleteSaveFile()
        {
            try
            {
                if (File.Exists(SaveFilePath)) File.Delete(SaveFilePath);
                if (File.Exists(BackupFilePath)) File.Delete(BackupFilePath);
                Debug.Log("<color=#FFB74D><b>[Save System]</b></color> Saved campaign data erased. Run reset.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Save System] Error erasing save files: {ex.Message}");
            }
        }
    }
}
