using UnityEngine;
using UnityEngine.InputSystem;
using ElementalHexTactics3D.Campaign;
using ElementalHexTactics3D.Turn;

namespace ElementalHexTactics3D.UI
{
    /// <summary>
    /// Interactive IMGUI post-battle modal overlay.
    /// Summarizes the tactical skirmish outcome (Victory / Defeat), itemizes secured
    /// domain spoils (Mana, Embers, Outcasts, Food, Crusade delay), displays the upcoming
    /// day progression & doom clock tick, and provides a clean one-click transition back to the Citadel Hub.
    /// </summary>
    public class PostBattleResultsUI : MonoBehaviour
    {
        public static PostBattleResultsUI Instance { get; private set; }

        private bool isOpen = false;
        private BattleResult battleResult = BattleResult.InProgress;
        private ExpeditionMissionData currentMission;

        private int manaGained;
        private int embersGained;
        private int outcastsGained;
        private int foodGained;
        private int daysDelayed;

        private Texture2D solidTex;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(this);
            }
            EnsureSolidTexture();
        }

        private void EnsureSolidTexture()
        {
            if (solidTex == null)
            {
                solidTex = new Texture2D(1, 1);
                solidTex.SetPixel(0, 0, Color.white);
                solidTex.Apply();
            }
        }

        public void ShowResults(BattleResult result, ExpeditionMissionData mission, int mana, int embers, int outcasts, int food, int delay)
        {
            isOpen = true;
            battleResult = result;
            currentMission = mission;
            manaGained = mana;
            embersGained = embers;
            outcastsGained = outcasts;
            foodGained = food;
            daysDelayed = delay;
        }

        public void CloseModal()
        {
            isOpen = false;
        }

        private void Update()
        {
            if (!isOpen) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            // Space, Enter, or Esc returns cleanly to Citadel Hub
            if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)
            {
                OnReturnClicked();
            }
        }

        private void OnReturnClicked()
        {
            CloseModal();
            if (CampaignManager.Instance != null)
            {
                CampaignManager.Instance.ReturnToCitadel();
            }
        }

        private void OnGUI()
        {
            if (!isOpen) return;

            EnsureSolidTexture();

            // 1. Fullscreen Dimmed Backdrop (Dark semi-transparent tint)
            Rect screenRect = new Rect(0, 0, Screen.width, Screen.height);
            GUI.color = new Color(0.04f, 0.05f, 0.08f, 0.88f);
            GUI.DrawTexture(screenRect, solidTex);
            GUI.color = Color.white;

            // 2. Centered Modal Layout
            float winW = Mathf.Min(800f, Screen.width - 40f);
            float winH = Mathf.Min(620f, Screen.height - 40f);
            float winX = (Screen.width - winW) * 0.5f;
            float winY = (Screen.height - winH) * 0.5f;

            Rect modalRect = new Rect(winX, winY, winW, winH);

            bool isVictory = (battleResult == BattleResult.Victory);
            Color borderColor = isVictory ? new Color(0.95f, 0.80f, 0.25f, 1f) : new Color(0.90f, 0.25f, 0.25f, 1f);
            Color bgColor = new Color(0.08f, 0.10f, 0.15f, 0.98f);

            DrawSolidPanel(modalRect, bgColor, borderColor, 3);

            // 3. Header Banner
            float topY = winY + 24f;
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                richText = true
            };
            headerStyle.normal.textColor = isVictory ? new Color(1f, 0.85f, 0.30f) : new Color(1f, 0.35f, 0.35f);

            string headerTitle = isVictory ? "👑 <b>EXPEDITION VICTORIOUS!</b> 👑" : "💀 <b>EXPEDITION DEFEAT</b> 💀";
            GUI.Label(new Rect(winX, topY, winW, 40f), headerTitle, headerStyle);

            // Subtitle
            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Italic,
                richText = true
            };
            subStyle.normal.textColor = new Color(0.80f, 0.86f, 0.94f);

            string missionName = currentMission != null ? currentMission.Title : "Tactical Skirmish";
            string threatStars = currentMission != null ? currentMission.GetThreatStars() : "★★★☆☆";
            string archName = currentMission != null ? currentMission.GetArchetypeName() : "Expedition";
            GUI.Label(new Rect(winX, topY + 42f, winW, 25f), $"\"{missionName}\" — {archName}  [{threatStars}]", subStyle);

            // 4. Panel: Spoils of War
            float spoilsY = topY + 75f;
            float spoilsH = 180f;
            Rect spoilsRect = new Rect(winX + 35f, spoilsY, winW - 70f, spoilsH);
            DrawSolidPanel(spoilsRect, new Color(0.05f, 0.07f, 0.11f, 0.95f), new Color(0.25f, 0.30f, 0.45f, 0.8f), 1);

            GUIStyle panelHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                richText = true
            };
            panelHeaderStyle.normal.textColor = new Color(0.70f, 0.85f, 1f);
            GUI.Label(new Rect(spoilsRect.x + 20f, spoilsRect.y + 12f, spoilsRect.width - 40f, 22f), isVictory ? "📦 <b>SPOILS SECURED FOR CITADEL</b>" : "⚠️ <b>CASUALTY & RECOVERY REPORT</b>", panelHeaderStyle);

            GUIStyle itemStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true };
            itemStyle.normal.textColor = Color.white;

            float itemLineY = spoilsRect.y + 42f;
            float col1X = spoilsRect.x + 25f;
            float col2X = spoilsRect.x + spoilsRect.width * 0.5f;

            if (isVictory)
            {
                GUI.Label(new Rect(col1X, itemLineY, 280f, 22f), $"💎 Mana Crystals: <color=#00E5FF><b>+{manaGained}</b></color>", itemStyle);
                GUI.Label(new Rect(col2X, itemLineY, 280f, 22f), $"🔥 Soul Embers: <color=#FF7043><b>+{embersGained}</b></color>", itemStyle);
                itemLineY += 28f;

                GUI.Label(new Rect(col1X, itemLineY, 280f, 22f), $"👥 Freed Outcasts: <color=#81C784><b>+{outcastsGained}</b></color>", itemStyle);
                string foodStr = (foodGained > 0) ? $"<color=#FFA726><b>+{foodGained}</b></color>" : "<color=#888888>+0</color>";
                GUI.Label(new Rect(col2X, itemLineY, 280f, 22f), $"🍖 Rations Foraged: {foodStr}", itemStyle);
                itemLineY += 28f;

                string delayStr = (daysDelayed > 0) 
                    ? $"<color=#FFD54F><b>+{daysDelayed} DAYS (Crusade Stalled!)</b></color>" 
                    : "<color=#888888>+0 Days</color>";
                GUI.Label(new Rect(col1X, itemLineY, 400f, 22f), $"⏳ Vanguard Sabotage: {delayStr}", itemStyle);
            }
            else
            {
                GUI.Label(new Rect(col1X, itemLineY, spoilsRect.width - 50f, 44f), 
                    "<color=#EF5350>Vanguard forces fell in combat. Units have retreated across the Abyssal Rift.\nNo spoils secured, but Citadel defenses remain intact.</color>", itemStyle);
            }

            // 5. Panel: Citadel Dawn & Calendar Transition
            float calY = spoilsY + spoilsH + 18f;
            float calH = 150f;
            Rect calRect = new Rect(winX + 35f, calY, winW - 70f, calH);
            DrawSolidPanel(calRect, new Color(0.05f, 0.07f, 0.11f, 0.95f), new Color(0.25f, 0.30f, 0.45f, 0.8f), 1);

            GUI.Label(new Rect(calRect.x + 20f, calRect.y + 12f, calRect.width - 40f, 22f), "🌅 <b>DAWN PROGRESSION & CITADEL OUTLOOK</b>", panelHeaderStyle);

            int curDay = CampaignManager.Instance != null ? CampaignManager.Instance.CurrentDay : 1;
            int nextDay = curDay + 1;
            int daysUntil = CampaignManager.Instance != null ? CampaignManager.Instance.DaysUntilCrusade : 24;
            int foodStock = CampaignManager.Instance != null ? CampaignManager.Instance.Food : 50;
            int upkeep = CampaignManager.Instance != null ? CampaignManager.Instance.DailyFoodUpkeep : 5;
            string actStr = CampaignManager.Instance != null ? CampaignManager.Instance.ActTitle : "Act I: Survival";

            float cLineY = calRect.y + 42f;
            GUI.Label(new Rect(col1X, cLineY, 320f, 22f), $"☀️ Campaign Day: <b>Day {curDay}</b> ➔ <color=#FFD54F><b>Day {nextDay} (Dawn)</b></color>", itemStyle);
            GUI.Label(new Rect(col2X, cLineY, 320f, 22f), $"⚔️ {actStr}", itemStyle);
            cLineY += 28f;

            string doomColor = (daysUntil <= 3) ? "#FF5252" : (daysUntil <= 7) ? "#FFB74D" : "#81C784";
            GUI.Label(new Rect(col1X, cLineY, 320f, 22f), $"⏳ Holy Crusade Countdown: <color={doomColor}><b>{daysUntil} Days Until Arrival</b></color>", itemStyle);
            GUI.Label(new Rect(col2X, cLineY, 320f, 22f), $"🍖 Daily Sustenance Upkeep: <color=#FF7043>-{upkeep} Food</color> (Stock: {foodStock})", itemStyle);
            cLineY += 28f;

            if (CampaignManager.Instance != null && CampaignManager.Instance.IsInFamine)
            {
                GUI.Label(new Rect(col1X, cLineY, calRect.width - 50f, 22f), "<color=#FF5252>⚠️ <b>FAMINE IN CITADEL:</b> Outcasts starving! Troops suffer -1 Action Point in battle!</color>", itemStyle);
            }

            // 6. Return Button
            float btnY = winY + winH - 68f;
            Rect btnRect = new Rect(winX + 160f, btnY, winW - 320f, 48f);

            Color btnNorm = isVictory ? new Color(0.18f, 0.45f, 0.28f, 1f) : new Color(0.45f, 0.20f, 0.20f, 1f);
            Color btnHover = isVictory ? new Color(0.28f, 0.65f, 0.38f, 1f) : new Color(0.65f, 0.30f, 0.30f, 1f);

            if (DrawButton(btnRect, "🏰 <b>Return to Citadel (Dawn) [Space / Enter]</b>", btnNorm, btnHover))
            {
                OnReturnClicked();
            }

            // Consume any unhandled mouse down on this modal so background elements don't get clicked
            if (Event.current.type == EventType.MouseDown)
            {
                Event.current.Use();
            }
        }

        private void DrawSolidPanel(Rect r, Color bg, Color border, int borderWidth)
        {
            // Background
            GUI.color = bg;
            GUI.DrawTexture(r, solidTex);

            // Border
            GUI.color = border;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, borderWidth), solidTex);
            GUI.DrawTexture(new Rect(r.x, r.y + r.height - borderWidth, r.width, borderWidth), solidTex);
            GUI.DrawTexture(new Rect(r.x, r.y, borderWidth, r.height), solidTex);
            GUI.DrawTexture(new Rect(r.x + r.width - borderWidth, r.y, borderWidth, r.height), solidTex);

            GUI.color = Color.white;
        }

        private bool DrawButton(Rect r, string text, Color normalColor, Color hoverColor)
        {
            Vector2 mouse = Event.current.mousePosition;
            bool isHovered = r.Contains(mouse);

            Color c = isHovered ? hoverColor : normalColor;
            GUI.color = c;
            GUI.DrawTexture(r, solidTex);

            // Border
            GUI.color = isHovered ? Color.white : new Color(0.4f, 0.5f, 0.65f, 0.9f);
            int bw = 2;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, bw), solidTex);
            GUI.DrawTexture(new Rect(r.x, r.y + r.height - bw, r.width, bw), solidTex);
            GUI.DrawTexture(new Rect(r.x, r.y, bw, r.height), solidTex);
            GUI.DrawTexture(new Rect(r.x + r.width - bw, r.y, bw, r.height), solidTex);

            GUI.color = Color.white;

            GUIStyle btnStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                richText = true
            };
            btnStyle.normal.textColor = Color.white;
            GUI.Label(r, text, btnStyle);

            if (Event.current.type == EventType.MouseDown && isHovered)
            {
                Event.current.Use();
                return true;
            }
            return false;
        }
    }
}

