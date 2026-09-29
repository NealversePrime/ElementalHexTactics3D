using System;
using UnityEngine;
using ElementalHexTactics3D.Turn;
using ElementalHexTactics3D.UI;
using ElementalHexTactics3D.UI.Hub;
using ElementalHexTactics3D.Combat;

namespace ElementalHexTactics3D.Campaign
{
    public enum CampaignPhase
    {
        Hub,
        Trilemma,
        Battle,
        PostBattle
    }

    /// <summary>
    /// Master singleton state machine for the macro campaign progression.
    /// Manages:
    /// 1. Day / Night cycle progression (Day = Citadel War Council prep, Night = Incursion skirmish).
    /// 2. The Holy Crusade Countdown (Doom clock ticking towards Day 25-30).
    /// 3. Food upkeep and Famine mechanics.
    /// 4. Spoils accounting and seamless return loop from battlefield to Citadel Hub.
    /// </summary>
    public class CampaignManager : MonoBehaviour
    {
        private static CampaignManager instance;
        public static CampaignManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<CampaignManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("CampaignManager");
                        instance = go.AddComponent<CampaignManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        [Header("Calendar & Crusade Doom Clock")]
        [SerializeField] private int currentDay = 1;
        [SerializeField] private int crusadeArrivalDay = 25;
        [SerializeField] private int dailyFoodUpkeep = 5;
        [SerializeField] private int food = 50;

        [Header("State Tracking")]
        [SerializeField] private CampaignPhase currentPhase = CampaignPhase.Hub;

        // Last battle records
        public BattleResult LastBattleResult { get; private set; } = BattleResult.InProgress;
        public ExpeditionMissionData LastMission { get; private set; }
        public int LastAwardedMana { get; private set; }
        public int LastAwardedEmbers { get; private set; }
        public int LastAwardedOutcasts { get; private set; }
        public int LastAwardedFood { get; private set; }
        public int LastDelayedDays { get; private set; }

        public int TotalVictories { get; private set; } = 0;
        public int TotalDefeats { get; private set; } = 0;

        // Properties
        public int CurrentDay => currentDay;
        public int CrusadeArrivalDay => crusadeArrivalDay;
        public int DaysUntilCrusade => Mathf.Max(0, crusadeArrivalDay - currentDay);
        public int Food => food;
        public int DailyFoodUpkeep => dailyFoodUpkeep;
        public bool IsInFamine => food <= 0;
        public CampaignPhase CurrentPhase => currentPhase;

        public int CurrentAct
        {
            get
            {
                if (currentDay <= 10) return 1;
                if (currentDay <= 22) return 2;
                return 3;
            }
        }

        public string ActTitle
        {
            get
            {
                switch (CurrentAct)
                {
                    case 1: return "Act I: Survival & The Ashen Gate";
                    case 2: return "Act II: Expansion & The Broken World";
                    case 3: return "Act III: The Holy Crusade & Deicide";
                    default: return "Act I: Survival";
                }
            }
        }

        // Events
        public event Action<int> OnDayAdvanced;
        public event Action<CampaignPhase> OnPhaseChanged;
        public event Action OnCrusadeArrived;
        public event Action<bool> OnFamineChanged;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                // Attempt to restore saved campaign state
                CampaignSaveManager.LoadAndApplySave();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Registers battle conclusion, computes domain spoils or casualty penalties,
        /// and opens the post-battle summary UI modal.
        /// </summary>
        public void OnBattleCompleted(BattleResult result, ExpeditionMissionData mission)
        {
            LastBattleResult = result;
            LastMission = mission;
            currentPhase = CampaignPhase.PostBattle;
            OnPhaseChanged?.Invoke(currentPhase);

            if (result == BattleResult.Victory)
            {
                TotalVictories++;
            }
            else if (result == BattleResult.Defeat)
            {
                TotalDefeats++;
            }

            if (result == BattleResult.Victory && mission != null)
            {
                LastAwardedMana = mission.RewardMana;
                LastAwardedEmbers = mission.RewardEmbers;
                LastAwardedOutcasts = mission.RewardOutcasts;
                LastAwardedFood = mission.RewardFood;
                LastDelayedDays = mission.RewardDoomClockDays;

                // Award to TownHubManager
                if (TownHubManager.Instance != null)
                {
                    if (LastAwardedMana > 0) TownHubManager.Instance.AddManaCrystals(LastAwardedMana);
                    if (LastAwardedEmbers > 0) TownHubManager.Instance.AddSoulEmbers(LastAwardedEmbers);
                    if (LastAwardedOutcasts > 0) TownHubManager.Instance.AddFreedOutcasts(LastAwardedOutcasts);
                }

                // Award Food
                if (LastAwardedFood > 0)
                {
                    AddFood(LastAwardedFood);
                }

                // Apply Crusade Delay
                if (LastDelayedDays > 0)
                {
                    DelayCrusade(LastDelayedDays);
                }
            }
            else if (result == BattleResult.Defeat)
            {
                // Defeat penalty: zero spoils, but survival remains intact
                LastAwardedMana = 0;
                LastAwardedEmbers = 0;
                LastAwardedOutcasts = 0;
                LastAwardedFood = 0;
                LastDelayedDays = 0;
            }

            // Ensure PostBattleResultsUI is available and display it
            if (PostBattleResultsUI.Instance == null)
            {
                gameObject.AddComponent<PostBattleResultsUI>();
            }
            PostBattleResultsUI.Instance.ShowResults(result, mission, LastAwardedMana, LastAwardedEmbers, LastAwardedOutcasts, LastAwardedFood, LastDelayedDays);
        }

        /// <summary>
        /// Returns the player to the Citadel Hub, advances the campaign day,
        /// consumes sustenance upkeep, checks famine/crusade conditions, and updates views.
        /// </summary>
        public void ReturnToCitadel()
        {
            // Close post battle screen
            if (PostBattleResultsUI.Instance != null)
            {
                PostBattleResultsUI.Instance.CloseModal();
            }

            // Recall and hide all player units back to Citadel reserve across the Rift!
            Units.TacticalUnitSpawner.ResetPlayerReserveUnits();

            // Clear any lingering enemy units from the battlefield
            Units.TacticalUnitSpawner.ClearAllEnemies();

            // Clear the active Abyssal Rift Conduit if one exists
            if (Combat.AbyssalRiftConduit3D.Instance != null)
            {
                Destroy(Combat.AbyssalRiftConduit3D.Instance.gameObject);
            }

            // Deselect all tiles and units in interaction system
            if (InputHandling.HexGridInteraction3D.Instance != null)
            {
                InputHandling.HexGridInteraction3D.Instance.DeselectAll();
            }

            // Switch UI views to Citadel Hub
            if (TitleMenuCanvasUI.Instance != null)
            {
                TitleMenuCanvasUI.Instance.ShowTownHub();
            }

            // Advance to the dawn of the next day
            AdvanceDay();

            // Auto-save the campaign run at Dawn
            CampaignSaveManager.SaveCurrentCampaign();

            currentPhase = CampaignPhase.Hub;
            OnPhaseChanged?.Invoke(currentPhase);

            // Announce Dawn Arrival banner
            if (CombatFeedbackManager.Instance != null)
            {
                string famineText = IsInFamine ? "\n<color=#EF5350>⚠️ FAMINE: Food depleted! Troops weakened.</color>" : "";
                CombatFeedbackManager.Instance.ShowBanner(
                    $"☀️ DAWN OF DAY {currentDay}",
                    $"Crusade arrives in {DaysUntilCrusade} days. Upkeep: -{dailyFoodUpkeep} Food (Stock: {food}).{famineText}",
                    2.8f,
                    new Color(1f, 0.88f, 0.35f)
                );
            }
        }

        /// <summary>
        /// Advances the calendar by 1 day, deducts food upkeep, and checks campaign triggers.
        /// </summary>
        public void AdvanceDay()
        {
            currentDay++;

            int prevFood = food;
            food = Mathf.Max(0, food - dailyFoodUpkeep);

            if (prevFood > 0 && food == 0)
            {
                OnFamineChanged?.Invoke(true);
            }
            else if (prevFood == 0 && food > 0)
            {
                OnFamineChanged?.Invoke(false);
            }

            // Check if Crusade has arrived at the gates
            if (DaysUntilCrusade <= 0)
            {
                OnCrusadeArrived?.Invoke();
                Debug.LogWarning("<color=#D32F2F><b>[CRUSADE ARRIVED!]</b></color> The Holy Crusade has breached the Ashen Gate!");
            }

            OnDayAdvanced?.Invoke(currentDay);

            // Refresh Trilemma cards for the new day
            if (ExpeditionPortalModalUI.Instance != null)
            {
                ExpeditionPortalModalUI.Instance.GenerateNewTrilemma();
            }
        }

        /// <summary>
        /// Delays the Crusade arrival day by sabotaging vanguard encampments and ballistas.
        /// </summary>
        public void DelayCrusade(int days)
        {
            if (days <= 0) return;
            crusadeArrivalDay += days;
            Debug.Log($"<color=#FFD54F><b>[Crusade Delayed!]</b></color> Deadline extended by +{days} days! New arrival day: {crusadeArrivalDay} (in {DaysUntilCrusade} days)");
        }

        public void AddFood(int amount)
        {
            if (amount <= 0) return;
            bool wasInFamine = IsInFamine;
            food += amount;
            if (wasInFamine && !IsInFamine)
            {
                OnFamineChanged?.Invoke(false);
            }
        }

        public void ConsumeFood(int amount)
        {
            if (amount <= 0) return;
            food = Mathf.Max(0, food - amount);
            if (food <= 0)
            {
                OnFamineChanged?.Invoke(true);
            }
        }

        /// <summary>
        /// Restores state from a loaded CampaignSaveData model.
        /// </summary>
        public void LoadFromSaveData(CampaignSaveData data)
        {
            if (data == null) return;

            currentDay = Mathf.Max(1, data.currentDay);
            crusadeArrivalDay = Mathf.Max(1, data.crusadeArrivalDay);
            food = Mathf.Max(0, data.food);
            dailyFoodUpkeep = Mathf.Max(1, data.dailyFoodUpkeep);
            TotalVictories = data.totalVictories;
            TotalDefeats = data.totalDefeats;

            OnDayAdvanced?.Invoke(currentDay);
            OnFamineChanged?.Invoke(IsInFamine);
        }

        /// <summary>
        /// Wipes the active campaign run and resets to Day 1 fresh start.
        /// </summary>
        public void ResetCampaign()
        {
            currentDay = 1;
            crusadeArrivalDay = 25;
            dailyFoodUpkeep = 5;
            food = 50;
            TotalVictories = 0;
            TotalDefeats = 0;

            CampaignSaveManager.DeleteSaveFile();

            if (TownHubManager.Instance != null)
            {
                TownHubManager.Instance.SetResources(450, 180, 16);
            }

            if (ExpeditionPortalModalUI.Instance != null)
            {
                ExpeditionPortalModalUI.Instance.GenerateNewTrilemma();
            }

            OnDayAdvanced?.Invoke(currentDay);
            OnFamineChanged?.Invoke(false);

            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.ShowBanner(
                    "🔄 CAMPAIGN RESET",
                    "A fresh incursion begins on Day 1. The Holy Crusade is 24 days away.",
                    2.8f,
                    new Color(0.4f, 0.8f, 1f)
                );
            }
        }
    }
}
