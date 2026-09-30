using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ElementalHexTactics3D.Campaign;
using ElementalHexTactics3D.Grid;

namespace ElementalHexTactics3D.UI.Hub
{
    /// <summary>
    /// Interactive Expedition Trilemma Modal for the Abyssal Portal.
    /// Presents 3 contrasting incursion cards (5:8 aspect ratio) with threat levels,
    /// elemental hazard affixes, domain loot, and full keyboard/mouse navigation.
    /// </summary>
    public class ExpeditionPortalModalUI : MonoBehaviour
    {
        public static ExpeditionPortalModalUI Instance { get; private set; }

        [SerializeField] private bool isOpen = false;
        private List<ExpeditionMissionData> currentCards = new List<ExpeditionMissionData>();
        private int selectedCardIndex = 0;
        private int hoveredCardIndex = -1;

        private Texture2D solidTex;
        private Rect modalWindowRect;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(this);

            EnsureSolidTexture();
        }

        public bool IsTutorialDispatchMode()
        {
            if (Tutorial.TutorialScenarioManager.Instance != null && Tutorial.TutorialScenarioManager.Instance.IsTutorialActive)
            {
                return !CampaignSaveManager.IsTutorialStage2Complete;
            }
            return false;
        }

        private void Start()
        {
            if (currentCards == null || currentCards.Count == 0)
            {
                GenerateNewTrilemma();
            }
        }

        public void OpenModal()
        {
            isOpen = true;
            selectedCardIndex = 0;
            hoveredCardIndex = -1;
            GenerateNewTrilemma();
        }

        public void CloseModal()
        {
            isOpen = false;
        }

        public void GenerateNewTrilemma()
        {
            if (IsTutorialDispatchMode())
            {
                GenerateTutorialTrilemma();
                return;
            }

            int cycle = (CampaignManager.Instance != null) ? CampaignManager.Instance.CurrentDay : 1;
            currentCards = ExpeditionTrilemmaGenerator.GenerateTrilemma(cycle);
            selectedCardIndex = 0;
        }

        private void GenerateTutorialTrilemma()
        {
            currentCards = new List<ExpeditionMissionData>();

            // Card 1: Highlighted Tutorial Mission (Defend Abyssal Gate)
            ExpeditionMissionData tutorialMission = new ExpeditionMissionData
            {
                MissionId = "TUTORIAL_STAGE2",
                Archetype = MissionArchetype.VanguardSabotage,
                Title = "[TUTORIAL] PERTAHANKAN ABYSSAL GATE",
                Description = "Hadang pasukan pelopor Holy Empire di ambang gerbang santuari! Kuasai sihir Fireball untuk membakar rumput jadi Magma, dan dorong prajurit suci menabrak Pilar Batu bersama Basalt Vanguard.",
                ThreatLevel = 1,
                Biome = BiomeTheme.VerdantHighlands,
                Modifier = StageModifier.StoneFortress,
                RewardMana = 100,
                RewardEmbers = 60,
                RewardOutcasts = 0,
                Seed = 2002
            };

            // Card 2: Locked
            ExpeditionMissionData lockedMine = new ExpeditionMissionData
            {
                MissionId = "TUTORIAL_LOCKED_MINE",
                Archetype = MissionArchetype.ResourceScavenge,
                Title = "🔒 TAMBANG KRISTAL MANA",
                Description = "Rute dimensi belum stabil. Selesaikan misi pertahanan tutorial pada Kartu #1 untuk menstabilkan Abyssal Rift.",
                ThreatLevel = 2,
                Biome = BiomeTheme.AncientCrusadeRuins,
                Modifier = StageModifier.None,
                RewardMana = 120,
                RewardEmbers = 30,
                RewardOutcasts = 2,
                Seed = 3001
            };

            // Card 3: Locked
            ExpeditionMissionData lockedRescue = new ExpeditionMissionData
            {
                MissionId = "TUTORIAL_LOCKED_RESCUE",
                Archetype = MissionArchetype.RescueRecruit,
                Title = "🔒 PENYELAMATAN TAWANAN",
                Description = "Rute dimensi belum stabil. Selesaikan misi pertahanan tutorial pada Kartu #1 untuk menstabilkan Abyssal Rift.",
                ThreatLevel = 2,
                Biome = BiomeTheme.FloodedBasin,
                Modifier = StageModifier.None,
                RewardMana = 50,
                RewardEmbers = 50,
                RewardOutcasts = 5,
                Seed = 3002
            };

            currentCards.Add(tutorialMission);
            currentCards.Add(lockedMine);
            currentCards.Add(lockedRescue);
            selectedCardIndex = 0;
        }

        private void Update()
        {
            if (!isOpen) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.escapeKey.wasPressedThisFrame)
            {
                CloseModal();
            }
            else if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
            {
                selectedCardIndex = 0;
            }
            else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
            {
                if (IsTutorialDispatchMode())
                {
                    ShowLockedFeedback();
                }
                else
                {
                    selectedCardIndex = 1;
                }
            }
            else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame)
            {
                if (IsTutorialDispatchMode())
                {
                    ShowLockedFeedback();
                }
                else
                {
                    selectedCardIndex = 2;
                }
            }
            else if (kb.rKey.wasPressedThisFrame)
            {
                if (!IsTutorialDispatchMode())
                {
                    GenerateNewTrilemma();
                }
                else
                {
                    ShowLockedFeedback("🔒 Reroll terkunci selama misi tutorial!");
                }
            }
            else if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
            {
                if (selectedCardIndex >= 0 && selectedCardIndex < currentCards.Count)
                {
                    EmbarkSelectedCard(currentCards[selectedCardIndex]);
                }
            }
        }

        private void ShowLockedFeedback(string msg = null)
        {
            Combat.SoundManager3D.Instance?.PlayButtonClick();
            if (Combat.CombatFeedbackManager.Instance != null)
            {
                Combat.CombatFeedbackManager.Instance.ShowBanner(
                    "🔒 RUTE TERKUNCI",
                    msg ?? "Selesaikan misi pertahanan tutorial pada Kartu #1 terlebih dahulu!",
                    1.8f,
                    new Color(0.95f, 0.45f, 0.35f)
                );
            }
        }

        private void EmbarkSelectedCard(ExpeditionMissionData card)
        {
            if (card == null) return;

            // Tutorial check: if in tutorial, always embark Card 0 which starts Stage 2!
            if (IsTutorialDispatchMode())
            {
                CloseModal();
                if (Tutorial.TutorialScenarioManager.Instance != null)
                {
                    Tutorial.TutorialScenarioManager.Instance.StartStage2AbyssalAwakening();
                }
                return;
            }

            CloseModal();

            // Set active mission
            ExpeditionTrilemmaGenerator.CurrentActiveMission = card;

            // Enter 3D battlefield
            if (TitleMenuCanvasUI.Instance != null)
            {
                TitleMenuCanvasUI.Instance.EnterHexBattlefield();
            }

            // Generate the procedural battlefield matching this mission
            if (HexGrid3D.Instance != null)
            {
                HexGrid3D.Instance.GenerateRandomizedBattlefield(card, card.Seed);
            }
        }

        private void OnGUI()
        {
            if (!isOpen) return;

            EnsureSolidTexture();

            // 1. Fullscreen Dimmed Backdrop (Blocks 100% click-through)
            Rect screenRect = new Rect(0, 0, Screen.width, Screen.height);
            GUI.color = new Color(0.04f, 0.05f, 0.08f, 0.88f);
            GUI.DrawTexture(screenRect, solidTex);
            GUI.color = Color.white;

            // Block mouse events behind modal
            Event e = Event.current;
            Vector2 mousePos = e.mousePosition;

            // 2. Modal Window Layout (Width 1560, Height 840)
            float winW = Mathf.Min(1560f, Screen.width - 40f);
            float winH = Mathf.Min(840f, Screen.height - 40f);
            float winX = (Screen.width - winW) * 0.5f;
            float winY = (Screen.height - winH) * 0.5f;

            modalWindowRect = new Rect(winX, winY, winW, winH);

            // Draw Window Background
            DrawSolidPanel(modalWindowRect, new Color(0.08f, 0.10f, 0.14f, 0.98f), new Color(0.35f, 0.25f, 0.55f, 1f), 3);

            // 3. Header
            float headerH = 100f;
            Rect headerRect = new Rect(winX, winY + 12f, winW, headerH);
            GUILayout.BeginArea(headerRect);
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                richText = true
            };
            titleStyle.normal.textColor = new Color(1f, 0.85f, 0.35f);
            GUILayout.Label("🌀 <b>ABYSSAL EXPEDITION GATEWAY</b> 🌀", titleStyle);

            // Calendar HUD Strip
            int cDay = CampaignManager.Instance != null ? CampaignManager.Instance.CurrentDay : 1;
            int cUntil = CampaignManager.Instance != null ? CampaignManager.Instance.DaysUntilCrusade : 24;
            int cFood = CampaignManager.Instance != null ? CampaignManager.Instance.Food : 50;
            string actStr = CampaignManager.Instance != null ? CampaignManager.Instance.ActTitle : "Act I: Survival";
            string doomCol = (cUntil <= 3) ? "#FF5252" : (cUntil <= 7) ? "#FFB74D" : "#81C784";
            string foodCol = (cFood <= 15) ? "#FF5252" : "#FFA726";

            GUIStyle hudStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperCenter,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                richText = true
            };
            GUILayout.Label($"☀️ <b>DAY {cDay}</b> ({actStr})   |   ⏳ CRUSADE IN: <color={doomCol}><b>{cUntil} DAYS</b></color>   |   🍖 FOOD: <color={foodCol}><b>{cFood}</b></color>", hudStyle);

            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperCenter,
                fontSize = 12,
                fontStyle = FontStyle.Italic,
                richText = true
            };
            subStyle.normal.textColor = new Color(0.75f, 0.82f, 0.92f);
            GUILayout.Label("Peer through the dimensional rift. Select one incursion destination for today's vanguard campaign.", subStyle);
            GUILayout.EndArea();

            // Close button (Top-Right '✕')
            Rect closeBtnRect = new Rect(winX + winW - 55f, winY + 16f, 40f, 40f);
            if (DrawButton(closeBtnRect, "<b>✕</b>", new Color(0.5f, 0.15f, 0.15f, 1f), new Color(0.85f, 0.25f, 0.25f, 1f), false))
            {
                CloseModal();
            }

            // 4. Render 3 Trilemma Cards (Tarot 5:8 Ratio: ~460w x 640h)
            float cardAreaY = winY + 115f;
            float cardW = (winW - 140f) / 3f;
            float cardH = winH - 185f;
            float cardGap = 35f;

            hoveredCardIndex = -1;

            for (int i = 0; i < currentCards.Count && i < 3; i++)
            {
                float cX = winX + 35f + i * (cardW + cardGap);
                Rect cardRect = new Rect(cX, cardAreaY, cardW, cardH);

                if (cardRect.Contains(mousePos))
                {
                    hoveredCardIndex = i;
                }

                bool isSelected = (selectedCardIndex == i);
                DrawExpeditionCard(cardRect, currentCards[i], i, isSelected, hoveredCardIndex == i);
            }

            // 5. Footer Bar (Reroll Button & Hotkey hints)
            float footerY = winY + winH - 65f;
            Rect rerollRect = new Rect(winX + 40f, footerY, 260f, 45f);
            if (IsTutorialDispatchMode())
            {
                if (DrawButton(rerollRect, "🔒 <b>Reroll Terkunci</b>", new Color(0.12f, 0.14f, 0.18f, 0.8f), new Color(0.25f, 0.30f, 0.38f, 0.5f), false))
                {
                    ShowLockedFeedback("🔒 Fitur reroll terkunci selama misi tutorial!");
                }
            }
            else
            {
                if (DrawButton(rerollRect, "🎲 <b>Reroll Incursions [R]</b>", new Color(0.18f, 0.22f, 0.32f, 1f), new Color(0.35f, 0.45f, 0.65f, 1f), false))
                {
                    GenerateNewTrilemma();
                }
            }

            Rect legendRect = new Rect(winX + 320f, footerY + 12f, winW - 360f, 40f);
            GUIStyle legStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, richText = true };
            legStyle.normal.textColor = new Color(0.6f, 0.65f, 0.72f);
            if (IsTutorialDispatchMode())
            {
                GUI.Label(legendRect, "👉 <b>[1 / Klik Kartu 1]</b> Misi Pertahanan Tutorial  |  <b>[Space / Enter]</b> Berangkat Mempertahankan Gerbang", legStyle);
            }
            else
            {
                GUI.Label(legendRect, "<b>[1, 2, 3]</b> Select Incursion  |  <b>[Space / Enter]</b> Embark  |  <b>[R]</b> Reroll Cards  |  <b>[Esc]</b> Close", legStyle);
            }

            // Consume mouse event so it doesn't click through to town buildings
            if (e.isMouse && modalWindowRect.Contains(mousePos))
            {
                e.Use();
            }
        }

        private void DrawExpeditionCard(Rect rect, ExpeditionMissionData card, int index, bool isSelected, bool isHovered)
        {
            bool isTutorial = IsTutorialDispatchMode();
            bool isLocked = isTutorial && (index != 0);

            // Hover / Selection Visual Styling
            Color cardBg;
            Color cardBorder;
            int borderThickness;

            if (isLocked)
            {
                cardBg = new Color(0.06f, 0.08f, 0.11f, 0.85f);
                cardBorder = new Color(0.25f, 0.28f, 0.35f, 0.6f);
                borderThickness = 1;
            }
            else if (isTutorial && index == 0)
            {
                // Pulsing golden spotlight for tutorial card
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4.5f);
                cardBg = new Color(0.14f, 0.16f, 0.25f, 0.98f);
                cardBorder = Color.Lerp(new Color(1f, 0.72f, 0.15f, 1f), new Color(1f, 0.95f, 0.50f, 1f), pulse);
                borderThickness = 3;
                isSelected = true;
            }
            else
            {
                cardBg = isSelected
                    ? new Color(0.13f, 0.16f, 0.24f, 0.98f)
                    : (isHovered ? new Color(0.11f, 0.13f, 0.19f, 0.98f) : new Color(0.09f, 0.10f, 0.15f, 0.95f));

                cardBorder = isSelected
                    ? new Color(1.0f, 0.82f, 0.25f, 1f) // Golden halo for selected card
                    : (isHovered ? new Color(0.35f, 0.75f, 1.0f, 0.9f) : new Color(0.22f, 0.28f, 0.38f, 0.75f));

                borderThickness = isSelected ? 3 : (isHovered ? 2 : 1);
            }

            DrawSolidPanel(rect, cardBg, cardBorder, borderThickness);

            float pad = 18f;
            float contentW = rect.width - (pad * 2);
            float curY = rect.y + pad;

            // 1. Archetype Banner
            Rect archRect = new Rect(rect.x + pad, curY, contentW, 26f);
            Color archColor = isLocked ? new Color(0.5f, 0.55f, 0.65f) : (isTutorial && index == 0 ? new Color(1f, 0.85f, 0.25f) : GetArchetypeColor(card.Archetype));
            DrawSolidPanel(archRect, archColor * 0.25f, archColor, 1);

            GUIStyle archStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                richText = true
            };
            archStyle.normal.textColor = archColor;
            string bannerText = isLocked ? "🔒 RUTE TERKUNCI" : (isTutorial && index == 0 ? "★ REKOMENDASI TUTORIAL ★" : card.GetArchetypeName().ToUpper());
            GUI.Label(archRect, bannerText, archStyle);
            curY += 34f;

            // 2. Title
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                richText = true
            };
            titleStyle.normal.textColor = isLocked ? new Color(0.6f, 0.65f, 0.7f) : Color.white;
            float titleH = 46f;
            GUI.Label(new Rect(rect.x + pad, curY, contentW, titleH), card.Title, titleStyle);
            curY += titleH + 6f;

            // 3. Threat Stars & Danger Level
            GUIStyle starStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true };
            string threatColor = (card.ThreatLevel >= 4) ? "#FF5252" : (card.ThreatLevel >= 3 ? "#FFA726" : "#81C784");
            string threatStr = isLocked
                ? "<color=#888888><b>Status:</b> Terkunci</color>"
                : $"<b>Danger:</b> <color={threatColor}>{card.GetThreatStars()}</color> <size=11>(Lvl {card.ThreatLevel})</size>";
            GUI.Label(new Rect(rect.x + pad, curY, contentW, 24f), threatStr, starStyle);
            curY += 28f;

            // 4. Environmental Affix Tag
            Rect affixRect = new Rect(rect.x + pad, curY, contentW, 46f);
            Color affixBorder = isLocked ? new Color(0.25f, 0.3f, 0.35f) :
                                (card.Modifier == StageModifier.VolcanicSurge ? new Color(1f, 0.35f, 0.1f) :
                                (card.Modifier == StageModifier.HeavyDeluge ? new Color(0.2f, 0.7f, 1f) :
                                (card.Modifier == StageModifier.StoneFortress ? new Color(0.7f, 0.6f, 0.5f) :
                                new Color(0.3f, 0.4f, 0.5f))));
            DrawSolidPanel(affixRect, new Color(0.06f, 0.08f, 0.11f, 0.9f), affixBorder, 1);

            GUIStyle affixStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 11,
                wordWrap = true,
                richText = true
            };
            affixStyle.normal.textColor = isLocked ? new Color(0.6f, 0.65f, 0.7f) : Color.white;
            GUI.Label(new Rect(affixRect.x + 8f, affixRect.y + 4f, affixRect.width - 16f, affixRect.height - 8f), isLocked ? "🔒 Celah dimensi belum distabilkan" : card.GetModifierTag(), affixStyle);
            curY += 54f;

            // 5. Mission Narrative Brief
            GUIStyle descStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                richText = true
            };
            descStyle.normal.textColor = isLocked ? new Color(0.55f, 0.6f, 0.65f) : new Color(0.78f, 0.84f, 0.92f);
            float descH = 68f;
            GUI.Label(new Rect(rect.x + pad, curY, contentW, descH), card.Description, descStyle);
            curY += descH + 8f;

            // 6. Rewards Box
            Rect rewBox = new Rect(rect.x + pad, curY, contentW, 72f);
            DrawSolidPanel(rewBox, new Color(0.05f, 0.07f, 0.10f, 0.95f), new Color(0.25f, 0.35f, 0.45f, 0.6f), 1);

            GUIStyle rewHeader = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold, richText = true };
            rewHeader.normal.textColor = isLocked ? new Color(0.6f, 0.65f, 0.7f) : new Color(1f, 0.85f, 0.35f);
            GUI.Label(new Rect(rewBox.x + 10f, rewBox.y + 6f, rewBox.width - 20f, 18f), "✦ EXPECTED SPOILS ✦", rewHeader);

            GUIStyle rewBody = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true, richText = true };
            rewBody.normal.textColor = isLocked ? new Color(0.55f, 0.6f, 0.65f) : Color.white;
            GUI.Label(new Rect(rewBox.x + 10f, rewBox.y + 26f, rewBox.width - 20f, 40f), isLocked ? "🔒 Selesaikan misi tutorial untuk membuka ekspedisi ini." : card.GetRewardsSummary(), rewBody);
            curY += 82f;

            // Lock Overlay for Card 2 & 3
            if (isLocked)
            {
                float overlayY = rect.y + 36f;
                float overlayH = (rect.height - 110f);
                Rect lockOverlayRect = new Rect(rect.x + 6f, overlayY, rect.width - 12f, overlayH);
                GUI.color = new Color(0.03f, 0.04f, 0.06f, 0.65f);
                GUI.DrawTexture(lockOverlayRect, solidTex);
                GUI.color = Color.white;

                GUIStyle lockPromptStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    richText = true
                };
                lockPromptStyle.normal.textColor = new Color(1f, 0.85f, 0.4f);
                GUI.Label(lockOverlayRect, "🔒\n<b>RUTE TERKUNCI</b>\n<size=11><color=#cccccc>Pilih Kartu #1 untuk tutorial</color></size>", lockPromptStyle);
            }

            // 7. Embark Action Button
            float btnH = 54f;
            float btnY = rect.y + rect.height - btnH - pad;
            Rect embarkBtnRect = new Rect(rect.x + pad, btnY, contentW, btnH);

            if (isLocked)
            {
                Color btnCol = new Color(0.14f, 0.16f, 0.20f, 0.85f);
                Color btnBord = new Color(0.28f, 0.32f, 0.40f, 0.6f);
                string btnLabel = "<b>🔒 TERKUNCI</b>";
                if (DrawButton(embarkBtnRect, btnLabel, btnCol, btnBord, false))
                {
                    ShowLockedFeedback();
                }
            }
            else
            {
                Color btnCol = (isTutorial && index == 0)
                    ? new Color(0.95f, 0.60f, 0.15f, 1f)
                    : (isSelected ? new Color(0.95f, 0.65f, 0.15f, 1f) : new Color(0.22f, 0.28f, 0.38f, 1f));
                Color btnBord = (isTutorial && index == 0)
                    ? new Color(1f, 0.90f, 0.40f, 1f)
                    : (isSelected ? new Color(1f, 0.90f, 0.40f, 1f) : new Color(0.40f, 0.50f, 0.65f, 1f));
                string btnLabel = (isTutorial && index == 0)
                    ? "<b>🔥 BERANGKAT MEMPERTAHANKAN GERBANG [1]</b>"
                    : (isSelected ? $"<b>🔥 EMBARK INCURSION [{index + 1}]</b>" : $"<b>Select Incursion [{index + 1}]</b>");

                if (DrawButton(embarkBtnRect, btnLabel, btnCol, btnBord, isSelected))
                {
                    if (isSelected)
                    {
                        EmbarkSelectedCard(card);
                    }
                    else
                    {
                        selectedCardIndex = index;
                    }
                }
            }
        }

        private Color GetArchetypeColor(MissionArchetype arch)
        {
            switch (arch)
            {
                case MissionArchetype.ResourceScavenge:
                    return new Color(0.15f, 0.85f, 0.95f, 1f); // Cyan
                case MissionArchetype.RescueRecruit:
                    return new Color(0.40f, 0.88f, 0.45f, 1f); // Emerald
                case MissionArchetype.VanguardSabotage:
                    return new Color(1.0f, 0.70f, 0.20f, 1f); // Amber
                case MissionArchetype.WildTitanHunt:
                    return new Color(0.95f, 0.30f, 0.30f, 1f); // Blood Crimson
                default:
                    return Color.white;
            }
        }

        private bool DrawButton(Rect rect, string text, Color bgColor, Color borderColor, bool isPrimary)
        {
            Event e = Event.current;
            bool isHover = rect.Contains(e.mousePosition);

            if (isHover)
            {
                bgColor = isPrimary ? bgColor * 1.15f : bgColor + new Color(0.1f, 0.12f, 0.15f, 0f);
                borderColor = Color.white;
            }

            DrawSolidPanel(rect, bgColor, borderColor, isPrimary ? 2 : 1);

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = isPrimary ? 15 : 13,
                fontStyle = FontStyle.Bold,
                richText = true
            };
            style.normal.textColor = isPrimary ? new Color(0.06f, 0.08f, 0.10f, 1f) : Color.white;

            GUI.Label(rect, text, style);

            return (e.type == EventType.MouseDown && e.button == 0 && isHover);
        }

        private void DrawSolidPanel(Rect rect, Color bg, Color border, int borderThickness)
        {
            // Border
            GUI.color = border;
            GUI.DrawTexture(rect, solidTex);

            // Center fill
            Rect fillRect = new Rect(
                rect.x + borderThickness,
                rect.y + borderThickness,
                Mathf.Max(0, rect.width - borderThickness * 2),
                Mathf.Max(0, rect.height - borderThickness * 2)
            );
            GUI.color = bg;
            GUI.DrawTexture(fillRect, solidTex);
            GUI.color = Color.white;
        }

        private void EnsureSolidTexture()
        {
            if (solidTex == null)
            {
                solidTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                solidTex.SetPixels(new Color[] { Color.white, Color.white, Color.white, Color.white });
                solidTex.Apply();
            }
        }
    }
}

