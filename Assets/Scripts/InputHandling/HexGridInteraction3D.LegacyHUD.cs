using UnityEngine;
using ElementalHexTactics3D.Grid;
using ElementalHexTactics3D.Units;
using ElementalHexTactics3D.Combat;
using ElementalHexTactics3D.Turn;
using ElementalHexTactics3D.UI;

namespace ElementalHexTactics3D.InputHandling
{
    public partial class HexGridInteraction3D
    {
        private void EnsureSolidTexture()
        {
            if (solidWhiteTex == null)
            {
                solidWhiteTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[] { Color.white, Color.white, Color.white, Color.white };
                solidWhiteTex.SetPixels(pixels);
                solidWhiteTex.Apply();
            }
        }

        private void DrawSolidPanel(Rect rect, Color bgColor, Color borderColor, int borderWidth = 2)
        {
            EnsureSolidTexture();
            Color prevColor = GUI.color;

            // 1. Draw solid background fill
            GUI.color = bgColor;
            GUI.DrawTexture(rect, solidWhiteTex, ScaleMode.StretchToFill);

            // 2. Draw sharp borders
            if (borderWidth > 0 && borderColor.a > 0f)
            {
                GUI.color = borderColor;
                GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, borderWidth), solidWhiteTex);
                GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - borderWidth, rect.width, borderWidth), solidWhiteTex);
                GUI.DrawTexture(new Rect(rect.x, rect.y, borderWidth, rect.height), solidWhiteTex);
                GUI.DrawTexture(new Rect(rect.x + rect.width - borderWidth, rect.y, borderWidth, rect.height), solidWhiteTex);
            }

            GUI.color = prevColor;
        }

        private bool DrawOpaqueButton(Rect rect, string text, Color normalColor, Color activeColor, bool isActive, bool enabled = true)
        {
            Vector2 mousePos = Event.current.mousePosition;
            bool isHover = enabled && rect.Contains(mousePos);

            Color bgColor;
            Color borderColor;

            if (!enabled)
            {
                bgColor = new Color(0.18f, 0.20f, 0.24f, 0.85f);
                borderColor = new Color(0.28f, 0.30f, 0.35f, 0.5f);
            }
            else if (isActive)
            {
                bgColor = activeColor;
                borderColor = new Color(1.0f, 0.9f, 0.2f, 1f);
            }
            else if (isHover)
            {
                bgColor = Color.Lerp(normalColor, Color.white, 0.25f);
                borderColor = Color.white;
            }
            else
            {
                bgColor = normalColor;
                borderColor = new Color(normalColor.r * 1.35f, normalColor.g * 1.35f, normalColor.b * 1.35f, 0.9f);
            }

            DrawSolidPanel(rect, bgColor, borderColor, isActive ? 3 : (isHover ? 2 : 1));

            Color prevContentColor = GUI.contentColor;
            GUI.contentColor = enabled ? (isActive ? Color.white : (isHover ? Color.yellow : Color.white)) : new Color(0.5f, 0.5f, 0.5f, 1f);

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 12,
                wordWrap = true
            };
            GUI.Label(rect, text, labelStyle);
            GUI.contentColor = prevContentColor;

            if (enabled && Event.current.type == EventType.MouseDown && rect.Contains(mousePos))
            {
                Event.current.Use();
                SoundManager3D.Instance?.PlayButtonClick();
                return true;
            }

            return false;
        }

        private void OnGUI()
        {
            if (!Application.isPlaying) return;

            EnsureSolidTexture();

            if (IsInputBlockedByMenuOrBattleEnd())
            {
                return;
            }

            // Suppress legacy IMGUI HUD when modern uGUI Canvas Combat HUD is active
            if (UI.CombatHudCanvasUI.Instance != null && UI.CombatHudCanvasUI.Instance.gameObject.activeInHierarchy)
            {
                DrawGhostUIPreview();
                return;
            }

            DrawTacticalInfoHUD();
            DrawActionBarHUD();
            DrawGhostUIPreview();
        }

        private void DrawTacticalInfoHUD()
        {
            hudRect = new Rect(16, 16, 420, 185);
            DrawSolidPanel(hudRect, new Color(0.08f, 0.10f, 0.14f, 0.95f), new Color(0.25f, 0.35f, 0.48f, 1f), 2);

            GUILayout.BeginArea(new Rect(hudRect.x + 12, hudRect.y + 10, hudRect.width - 24, hudRect.height - 20));

            DrawTopHeaderControls();
            GUILayout.Label("<color=#FFD54F><b>Camera:</b></color> <b>Q / E</b> Rotate | <b>WASD</b> Pan | <b>Scroll</b> Zoom");
            GUILayout.Space(3);

            if (currentSelectedUnit != null)
            {
                DrawSelectedUnitInfoSection();
            }
            else if (currentHoveredTile != null)
            {
                DrawHoveredTileInfoSection();
            }
            else
            {
                GUILayout.Label("<color=#9E9E9E>Select Commander or Titan to act...</color>");
            }

            GUILayout.EndArea();
        }

        private void DrawTopHeaderControls()
        {
            string roundText = TurnManager3D.Instance != null
                ? $"Round {TurnManager3D.Instance.CurrentRound} - {(TurnManager3D.Instance.IsPlayerTurn ? "<color=#64B5F6>PLAYER TURN</color>" : "<color=#EF5350>ENEMY TURN</color>")}"
                : "Player Turn";

            var mission = ElementalHexTactics3D.Campaign.ExpeditionTrilemmaGenerator.CurrentActiveMission;
            string missionInfo = mission != null ? $" | <color=#FFD54F>{mission.Title}</color> <size=11>({mission.GetThreatStars()})</size>" : "";

            GUILayout.BeginHorizontal();
            GUILayout.Label($"<size=14><b>Incursion:</b></size>{missionInfo} | {roundText}");
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("New Map", GUILayout.Width(82), GUILayout.Height(20)))
            {
                DeselectAll();
                HexGrid3D.Instance?.GenerateRandomizedBattlefield();
            }
            GUILayout.Space(4);

            if (TurnManager3D.Instance != null)
            {
                bool aiOff = TurnManager3D.Instance.DisableEnemyAI;
                string aiLabel = aiOff ? "<color=#EF5350> AI: OFF</color>" : "<color=#81C784> AI: ON</color>";
                if (GUILayout.Button(aiLabel, GUILayout.Width(75), GUILayout.Height(20)))
                {
                    TurnManager3D.Instance.DisableEnemyAI = !TurnManager3D.Instance.DisableEnemyAI;
                }
                GUILayout.Space(4);
            }

            bool isMuted = (SoundManager3D.Instance != null && SoundManager3D.Instance.IsMuted);
            string audioLabel = isMuted ? "Audio" : "Audio";
            if (GUILayout.Button(audioLabel, GUILayout.Width(75), GUILayout.Height(20)))
            {
                if (SoundManager3D.Instance != null)
                {
                    SoundManager3D.Instance.IsMuted = !SoundManager3D.Instance.IsMuted;
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawSelectedUnitInfoSection()
        {
            string fColor = (currentSelectedUnit.Faction == UnitFaction.Player) ? "#64B5F6" : "#EF5350";
            string statusText = currentSelectedUnit.IsExhausted ? "<color=#B0BEC5>[Exhausted]</color>" : "<color=#81C784>[Ready]</color>";
            GUILayout.Label($"<color={fColor}><b>Selected:</b> {currentSelectedUnit.UnitName}</color> ({currentSelectedUnit.Archetype}) {statusText}");
            GUILayout.Label($"<b>HP:</b> {currentSelectedUnit.CurrentHealth}/{currentSelectedUnit.MaxHealth} | <b>ATK:</b> {currentSelectedUnit.EffectiveAttackDamage} | <b>Move:</b> {currentSelectedUnit.EffectiveMoveRange} | <b>Moved:</b> {(currentSelectedUnit.HasMovedThisTurn ? "<color=#B0BEC5></color>" : "<color=#81C784>X</color>")} | <b>Acted:</b> {(currentSelectedUnit.HasActedThisTurn ? "<color=#B0BEC5></color>" : "<color=#81C784>X</color>")}");

            string attuneColor = (currentSelectedUnit.BonusAttackDamage > 0) ? "#FFA726" : (currentSelectedUnit.BonusMoveRange > 0 ? "#29B6F6" : "#CFD8DC");
            GUILayout.Label($"<b>Attunement:</b> <color={attuneColor}>{currentSelectedUnit.CurrentAttunementName}</color> | <b>Cores:</b> <color=#FFD54F>* {currentSelectedUnit.ElementalCores}</color>");

            if (currentHoveredTile != null && AbyssalRiftConduit3D.Instance != null && AbyssalRiftConduit3D.Instance.RiftTile == currentHoveredTile)
            {
                GUILayout.Label("<color=#BA68C8> <b>Hovering Abyssal Rift Conduit</b> (Sacrifice Maw)</color>");
            }
        }

        private void DrawHoveredTileInfoSection()
        {
            if (AbyssalRiftConduit3D.Instance != null && AbyssalRiftConduit3D.Instance.RiftTile == currentHoveredTile)
            {
                GUILayout.Label("<color=#BA68C8><b>ABYSSAL RIFT CONDUIT</b></color> (Sanctuary Gateway)");
                GUILayout.Label($"<b>Coordinates:</b> {currentHoveredTile.Coordinates} | <b>Sacrifices:</b> {AbyssalRiftConduit3D.Instance.TotalSacrifices}");
                GUILayout.Label("<i><color=#FFE082>Shove enemies here to sacrifice for +1 Core!</color></i>");
            }
            else
            {
                GUILayout.Label($"<color=#81C784><b>Hovered Hex:</b></color> {currentHoveredTile.Coordinates} | Elev: {currentHoveredTile.Elevation}");
                GUILayout.Label($"<b>Terrain:</b> {currentHoveredTile.State} (Tier {currentHoveredTile.TierLevel})");
            }

            if (currentHoveredTile.CurrentOccupant is TacticalUnit3D occupant)
            {
                GUILayout.Label($"<color=#FFA726><b>Occupant:</b></color> {occupant.UnitName} (HP: {occupant.CurrentHealth}, {occupant.CurrentAttunementName})");
            }
        }

        private void DrawActionBarHUD()
        {
            bool isBattleActive = (TurnManager3D.Instance == null || TurnManager3D.Instance.Result == BattleResult.InProgress);
            bool isPlayerTurn = isBattleActive && (TurnManager3D.Instance == null || TurnManager3D.Instance.IsPlayerTurn);
            bool canAct = isPlayerTurn && (currentSelectedUnit != null && currentSelectedUnit.Faction == UnitFaction.Player);
            bool canMove = canAct && !currentSelectedUnit.HasMovedThisTurn && currentSelectedUnit.EffectiveMoveRange > 0;
            bool canCombat = canAct && !currentSelectedUnit.HasActedThisTurn;

            float barWidth = 980f;
            float barHeight = 62f;
            float startX = (Screen.width - barWidth) * 0.5f;
            float startY = Screen.height - barHeight - 16f;

            actionBarRect = new Rect(startX, startY, barWidth, barHeight);
            DrawSolidPanel(actionBarRect, new Color(0.08f, 0.10f, 0.14f, 0.96f), new Color(0.25f, 0.35f, 0.45f, 1f), 2);

            float btnY = startY + 8f;
            float btnH = barHeight - 16f;
            float curX = startX + 10f;
            float spacing = 8f;

            // 1. IF AN ENEMY IS SELECTED: Display enemy inspection panel
            if (currentSelectedUnit != null && currentSelectedUnit.Faction == UnitFaction.Enemy)
            {
                DrawEnemyInspectionBar(startX, barWidth, curX, btnY, btnH, spacing, isPlayerTurn);
                return;
            }

            // 2. RIFT NOT YET OPENED: Prompt player to tear open the Rift!
            bool riftExists = (AbyssalRiftConduit3D.Instance != null && AbyssalRiftConduit3D.Instance.RiftTile != null);
            if (!riftExists && currentSelectedUnit == null)
            {
                DrawUnanchoredRiftBar(startX, barWidth, curX, btnY, btnH, spacing, isPlayerTurn);
                return;
            }

            // 3. CITADEL BASE PANEL ACTION BAR
            bool isRiftSelected = (currentSelectedTile != null && AbyssalRiftConduit3D.Instance != null && AbyssalRiftConduit3D.Instance.RiftTile == currentSelectedTile);
            bool isDeployMode = (currentMode == UnitActionMode.DeployCommander || currentMode == UnitActionMode.SummonTitan);
            bool noActiveUnits = !HasActivePlayerUnitsOnField();

            if (currentSelectedUnit == null && (isRiftSelected || isDeployMode || noActiveUnits))
            {
                DrawCitadelBasePanelBar(startX, barWidth, curX, btnY, btnH, spacing, isPlayerTurn, noActiveUnits);
                return;
            }

            // 4. UNIVERSAL MOVE BUTTON
            curX = DrawUniversalMoveButton(curX, btnY, btnH, spacing, canMove);

            // 5. ARCHETYPE ABILITY BUTTONS
            if (currentSelectedUnit != null && currentSelectedUnit.Archetype == UnitArchetype.Titan)
            {
                DrawTitanAbilityButtons(curX, btnY, btnH, spacing, canAct, canCombat);
            }
            else
            {
                DrawCommanderAbilityButtons(curX, btnY, btnH, spacing, isPlayerTurn, canAct, canCombat);
            }

            // 6. END TURN BUTTON
            DrawEndTurnButton(startX, barWidth, btnY, btnH, isPlayerTurn);
        }

        private void DrawEnemyInspectionBar(float startX, float barWidth, float curX, float btnY, float btnH, float spacing, bool isPlayerTurn)
        {
            Rect enemyInspectRect = new Rect(curX, btnY, 440f, btnH);
            curX += 440f + spacing;
            string enemyInfo = $"<color=#EF5350><b>ENEMY: {currentSelectedUnit.UnitName}</b></color> ({currentSelectedUnit.Archetype})\n" +
                               $"<size=11>HP: {currentSelectedUnit.CurrentHealth}/{currentSelectedUnit.MaxHealth} | ATK: {currentSelectedUnit.EffectiveAttackDamage} | Move: {currentSelectedUnit.EffectiveMoveRange} | Affinity: {currentSelectedUnit.Affinity}</size>";
            DrawSolidPanel(enemyInspectRect, new Color(0.18f, 0.08f, 0.08f, 0.95f), new Color(0.85f, 0.25f, 0.25f, 0.8f), 1);
            GUIStyle enemyStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, richText = true };
            GUI.Label(enemyInspectRect, enemyInfo, enemyStyle);

            bool riftPlaced = (AbyssalRiftConduit3D.Instance != null && AbyssalRiftConduit3D.Instance.RiftTile != null);
            if (!riftPlaced)
            {
                Rect tearRect = new Rect(curX, btnY, 260f, btnH);
                string tearLabel = (currentMode == UnitActionMode.TearRift)
                    ? "<b>[Targeting Entry...]</b>\n<size=10>(Click Any Open Hex)</size>"
                    : "<b>Tear Open Abyssal Rift</b>\n<size=10>(Choose Landing Hex & Deploy)</size>";

                if (DrawOpaqueButton(tearRect, tearLabel,
                    new Color(0.45f, 0.15f, 0.70f, 1f), new Color(0.75f, 0.25f, 1.0f, 1f),
                    currentMode == UnitActionMode.TearRift, isPlayerTurn))
                {
                    ClearUnitSelection();
                    SetActionMode(currentMode == UnitActionMode.TearRift ? UnitActionMode.None : UnitActionMode.TearRift);
                }
            }

            DrawEndTurnButton(startX, barWidth, btnY, btnH, isPlayerTurn);
        }

        private void DrawUnanchoredRiftBar(float startX, float barWidth, float curX, float btnY, float btnH, float spacing, bool isPlayerTurn)
        {
            Rect titleRect = new Rect(curX, btnY, 190f, btnH);
            curX += 190f + spacing;
            string titleText = "<color=#BA68C8><b>CITADEL GATEWAY</b></color>\n<size=10>(Rift Unanchored)</size>";
            DrawSolidPanel(titleRect, new Color(0.12f, 0.08f, 0.18f, 0.95f), new Color(0.65f, 0.25f, 0.95f, 0.8f), 1);
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 11 };
            GUI.Label(titleRect, titleText, titleStyle);

            Rect tearRect = new Rect(curX, btnY, 280f, btnH);
            curX += 280f + spacing;
            string tearLabel = (currentMode == UnitActionMode.TearRift)
                ? "<b>[Targeting Entry...]</b>\n<size=10>(Click Any Open Hex)</size>"
                : "<b>Tear Open Abyssal Rift</b>\n<size=10>(Choose Landing Hex & Open Rift)</size>";

            if (DrawOpaqueButton(tearRect, tearLabel,
                new Color(0.45f, 0.15f, 0.70f, 1f), new Color(0.75f, 0.25f, 1.0f, 1f),
                currentMode == UnitActionMode.TearRift, isPlayerTurn))
            {
                SetActionMode(currentMode == UnitActionMode.TearRift ? UnitActionMode.None : UnitActionMode.TearRift);
            }

            Rect randRect = new Rect(curX, btnY, 220f, btnH);
            if (DrawOpaqueButton(randRect, "<b>Randomize Map [R]</b>\n<size=10>(Roll New Incursion)</size>",
                new Color(0.18f, 0.28f, 0.40f, 1f), new Color(0.35f, 0.65f, 0.95f, 1f),
                false, isPlayerTurn))
            {
                DeselectAll();
                HexGrid3D.Instance?.GenerateRandomizedBattlefield();
            }

            DrawEndTurnButton(startX, barWidth, btnY, btnH, isPlayerTurn);
        }

        private void DrawCitadelBasePanelBar(float startX, float barWidth, float curX, float btnY, float btnH, float spacing, bool isPlayerTurn, bool noActiveUnits)
        {
            TacticalUnit3D cmdr = FindCommanderUnit();
            TacticalUnit3D titan = FindTitanUnit();

            bool cmdrInReserve = (cmdr != null && !cmdr.gameObject.activeInHierarchy);
            bool titanInReserve = (titan != null && !titan.gameObject.activeInHierarchy);
            bool canDeployCmdr = isPlayerTurn && (cmdrInReserve || (cmdr != null && cmdr.gameObject.activeInHierarchy));
            bool canSummonTitan = isPlayerTurn && ((noActiveUnits && titanInReserve) || (cmdr != null && cmdr.ElementalCores >= 1) || (!titanInReserve && titan != null));

            Rect titleRect = new Rect(curX, btnY, 180f, btnH);
            curX += 180f + spacing;
            string titleText = "<color=#BA68C8><b>CITADEL RIFT</b></color>\n<size=10>(Base Panel Gateway)</size>";
            DrawSolidPanel(titleRect, new Color(0.12f, 0.08f, 0.18f, 0.95f), new Color(0.65f, 0.25f, 0.95f, 0.8f), 1);
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 11 };
            GUI.Label(titleRect, titleText, titleStyle);

            // 1. Deploy Commander Button
            Rect cmdrRect = new Rect(curX, btnY, 175f, btnH);
            curX += 175f + spacing;
            string cmdrLabel = !cmdrInReserve
                ? "Commander\n<size=10>(On Field - Click to Pick)</size>"
                : (currentMode == UnitActionMode.DeployCommander
                    ? "<b>[Deploying...]</b>\n<size=10>(Click Hex)</size>"
                    : "Deploy Commander\n<size=10>(Free Vanguard)</size>");

            if (DrawOpaqueButton(cmdrRect, cmdrLabel,
                new Color(0.40f, 0.15f, 0.65f, 1f), new Color(0.70f, 0.30f, 1.0f, 1f),
                currentMode == UnitActionMode.DeployCommander, canDeployCmdr))
            {
                if (cmdrInReserve)
                {
                    SetActionMode(currentMode == UnitActionMode.DeployCommander ? UnitActionMode.None : UnitActionMode.DeployCommander);
                }
                else if (cmdr != null && cmdr.gameObject.activeInHierarchy)
                {
                    SelectTile(cmdr.CurrentTile);
                    SelectUnit(cmdr);
                }
            }

            // 2. Summon Titan Button
            Rect titanRect = new Rect(curX, btnY, 175f, btnH);
            string titanLabel;
            if (!titanInReserve)
            {
                titanLabel = "Titan\n<size=10>(On Field - Click to Pick)</size>";
            }
            else if (noActiveUnits)
            {
                titanLabel = (currentMode == UnitActionMode.SummonTitan)
                    ? "<b>[Summoning...]</b>\n<size=10>(Click Hex)</size>"
                    : "Deploy Titan\n<size=10>(Free Vanguard)</size>";
            }
            else if (cmdr != null && cmdr.ElementalCores < 1)
            {
                titanLabel = "<color=#90A4AE> Summon Titan\n<size=10>(Req 1 Core)</size></color>";
            }
            else if (currentMode == UnitActionMode.SummonTitan)
            {
                titanLabel = "<b>[Summoning...]</b>\n<size=10>(Click Hex)</size>";
            }
            else
            {
                titanLabel = "Summon Titan\n<size=10>(1 Core)</size>";
            }

            if (DrawOpaqueButton(titanRect, titanLabel,
                new Color(0.75f, 0.25f, 0.12f, 1f), new Color(1.0f, 0.40f, 0.15f, 1f),
                currentMode == UnitActionMode.SummonTitan, canSummonTitan))
            {
                if (titanInReserve)
                {
                    SetActionMode(currentMode == UnitActionMode.SummonTitan ? UnitActionMode.None : UnitActionMode.SummonTitan);
                }
                else if (titan != null && titan.gameObject.activeInHierarchy)
                {
                    SelectTile(titan.CurrentTile);
                    SelectUnit(titan);
                }
            }

            DrawEndTurnButton(startX, barWidth, btnY, btnH, isPlayerTurn);
        }

        private float DrawUniversalMoveButton(float curX, float btnY, float btnH, float spacing, bool canMove)
        {
            Rect moveRect = new Rect(curX, btnY, 80f, btnH);
            curX += 80f + spacing;
            string moveLabel;

            if (currentSelectedUnit != null && currentSelectedUnit.HasMovedThisTurn)
                moveLabel = "<color=#90A4AE><b>[Moved]</b></color>";
            else if (currentSelectedUnit != null && currentSelectedUnit.IsImmobilized)
                moveLabel = "<color=#EF5350><b>[Trapped]</b></color>";
            else if (currentSelectedUnit != null && currentSelectedUnit.IsCrippled)
                moveLabel = (currentMode == UnitActionMode.Move) ? "<b>[Move 1]</b>" : "Move (1)";
            else
                moveLabel = (currentMode == UnitActionMode.Move) ? "<b>[Moving]</b>" : "Move";

            if (DrawOpaqueButton(moveRect, moveLabel,
                new Color(0.18f, 0.32f, 0.48f, 1f), new Color(0.15f, 0.55f, 0.90f, 1f),
                currentMode == UnitActionMode.Move, canMove))
            {
                SetActionMode(currentMode == UnitActionMode.Move ? UnitActionMode.None : UnitActionMode.Move);
            }

            return curX;
        }

        private void DrawTitanAbilityButtons(float curX, float btnY, float btnH, float spacing, bool canAct, bool canCombat)
        {
            // 1. Heavy Melee Strike
            Rect strikeRect = new Rect(curX, btnY, 120f, btnH);
            curX += 120f + spacing;
            string strikeLabel = (currentSelectedUnit.HasActedThisTurn)
                ? "<color=#90A4AE> Titan Strike\n<size=10>(Acted)</size></color>"
                : $"Titan Strike\n<size=10>(Dmg {currentSelectedUnit.EffectiveAttackDamage})</size>";
            if (DrawOpaqueButton(strikeRect, strikeLabel,
                new Color(0.65f, 0.22f, 0.15f, 1f), new Color(0.95f, 0.35f, 0.15f, 1f),
                currentMode == UnitActionMode.TitanStrike, canCombat))
            {
                SetActionMode(currentMode == UnitActionMode.TitanStrike ? UnitActionMode.None : UnitActionMode.TitanStrike);
            }

            // 2. Tail Shove
            Rect shoveRect = new Rect(curX, btnY, 105f, btnH);
            curX += 105f + spacing;
            string shoveLabel = (currentSelectedUnit.HasActedThisTurn)
                ? "<color=#90A4AE> Tail Shove\n<size=10>(Acted)</size></color>"
                : "Tail Shove\n<size=10>(Shove 1)</size>";
            if (DrawOpaqueButton(shoveRect, shoveLabel,
                new Color(0.38f, 0.18f, 0.52f, 1f), new Color(0.65f, 0.28f, 0.90f, 1f),
                currentMode == UnitActionMode.KineticPush, canCombat))
            {
                SetActionMode(currentMode == UnitActionMode.KineticPush ? UnitActionMode.None : UnitActionMode.KineticPush);
            }

            // 3. Siphon Land
            bool canSiphon = canCombat && !currentSelectedUnit.HasSiphonedThisTurn && HasConsumableTilesNearby(currentSelectedUnit);
            Rect siphonRect = new Rect(curX, btnY, 120f, btnH);
            curX += 120f + spacing;
            string siphonLabel = (currentSelectedUnit.HasSiphonedThisTurn || currentSelectedUnit.HasActedThisTurn)
                ? "<color=#90A4AE> Siphon Land\n<size=10>(Acted)</size></color>"
                : "Siphon Land\n<size=10>(+1 Core)</size>";
            if (DrawOpaqueButton(siphonRect, siphonLabel,
                new Color(0.70f, 0.55f, 0.15f, 1f), new Color(1.0f, 0.82f, 0.20f, 1f),
                currentMode == UnitActionMode.ConsumeLand, canSiphon))
            {
                SetActionMode(currentMode == UnitActionMode.ConsumeLand ? UnitActionMode.None : UnitActionMode.ConsumeLand);
            }

            // 4. Magma Cataclysm
            bool canUlt = canCombat && currentSelectedUnit.ElementalCores >= 1;
            Rect ultRect = new Rect(curX, btnY, 140f, btnH);
            curX += 140f + spacing;
            string ultLabel = (currentSelectedUnit.ElementalCores < 1)
                ? "<color=#90A4AE> Cataclysm\n<size=10>(Req 1 Core)</size></color>"
                : (currentSelectedUnit.HasActedThisTurn
                    ? "<color=#90A4AE> Cataclysm\n<size=10>(Acted)</size></color>"
                    : $"Cataclysm\n<size=10><b>(Core: {currentSelectedUnit.ElementalCores})</b></size>");
            if (DrawOpaqueButton(ultRect, ultLabel,
                new Color(0.80f, 0.15f, 0.12f, 1f), new Color(1.0f, 0.35f, 0.10f, 1f),
                currentMode == UnitActionMode.MagmaCataclysm, canUlt))
            {
                SetActionMode(currentMode == UnitActionMode.MagmaCataclysm ? UnitActionMode.None : UnitActionMode.MagmaCataclysm);
            }

            // 5. Recall
            Rect recallRect = new Rect(curX, btnY, 110f, btnH);
            if (DrawOpaqueButton(recallRect, "Recall\n<size=10>(To Reserve)</size>",
                new Color(0.35f, 0.15f, 0.55f, 1f), new Color(0.65f, 0.25f, 0.95f, 1f),
                false, canAct))
            {
                StartCoroutine(RecallUnitRoutine(currentSelectedUnit));
            }
        }

        private void DrawCommanderAbilityButtons(float curX, float btnY, float btnH, float spacing, bool isPlayerTurn, bool canAct, bool canCombat)
        {
            // 1. Fireball
            Rect fireRect = new Rect(curX, btnY, 90f, btnH);
            curX += 90f + spacing;
            string fireLabel = (currentSelectedUnit != null && currentSelectedUnit.HasActedThisTurn)
                ? "<color=#90A4AE> Fireball\n<size=10>(Acted)</size></color>"
                : "Fireball\n<size=10>(Dmg 3)</size>";
            if (DrawOpaqueButton(fireRect, fireLabel,
                new Color(0.55f, 0.20f, 0.12f, 1f), new Color(0.90f, 0.35f, 0.10f, 1f),
                currentMode == UnitActionMode.Fireball, canCombat))
            {
                SetActionMode(currentMode == UnitActionMode.Fireball ? UnitActionMode.None : UnitActionMode.Fireball);
            }

            // 2. Water Surge
            Rect waterRect = new Rect(curX, btnY, 90f, btnH);
            curX += 90f + spacing;
            string waterLabel = (currentSelectedUnit != null && currentSelectedUnit.HasActedThisTurn)
                ? "<color=#90A4AE> Water\n<size=10>(Acted)</size></color>"
                : "Water\n<size=10>(Dmg 2)</size>";
            if (DrawOpaqueButton(waterRect, waterLabel,
                new Color(0.12f, 0.38f, 0.60f, 1f), new Color(0.20f, 0.60f, 0.95f, 1f),
                currentMode == UnitActionMode.WaterSurge, canCombat))
            {
                SetActionMode(currentMode == UnitActionMode.WaterSurge ? UnitActionMode.None : UnitActionMode.WaterSurge);
            }

            // 3. Earth Pillar
            Rect earthRect = new Rect(curX, btnY, 105f, btnH);
            curX += 105f + spacing;
            string earthLabel = (currentSelectedUnit != null && currentSelectedUnit.HasActedThisTurn)
                ? "<color=#90A4AE> Earth Pillar\n<size=10>(Acted)</size></color>"
                : "Earth Pillar\n<size=10>(Pillar/Mud)</size>";
            if (DrawOpaqueButton(earthRect, earthLabel,
                new Color(0.40f, 0.28f, 0.18f, 1f), new Color(0.70f, 0.52f, 0.30f, 1f),
                currentMode == UnitActionMode.EarthSpire, canCombat))
            {
                SetActionMode(currentMode == UnitActionMode.EarthSpire ? UnitActionMode.None : UnitActionMode.EarthSpire);
            }

            // 4. Kinetic Push
            Rect pushRect = new Rect(curX, btnY, 90f, btnH);
            curX += 90f + spacing;
            string pushLabel = (currentSelectedUnit != null && currentSelectedUnit.HasActedThisTurn)
                ? "<color=#90A4AE> Push\n<size=10>(Acted)</size></color>"
                : "Push\n<size=10>(Shove 1)</size>";
            if (DrawOpaqueButton(pushRect, pushLabel,
                new Color(0.38f, 0.18f, 0.52f, 1f), new Color(0.65f, 0.28f, 0.90f, 1f),
                currentMode == UnitActionMode.KineticPush, canCombat))
            {
                SetActionMode(currentMode == UnitActionMode.KineticPush ? UnitActionMode.None : UnitActionMode.KineticPush);
            }

            // 5. Siphon Land
            bool canSiphon = canCombat && !currentSelectedUnit.HasSiphonedThisTurn && HasConsumableTilesNearby(currentSelectedUnit);
            Rect siphonRect = new Rect(curX, btnY, 110f, btnH);
            curX += 110f + spacing;
            string siphonLabel = (currentSelectedUnit != null && currentSelectedUnit.HasActedThisTurn)
                ? "<color=#90A4AE> Siphon\n<size=10>(Acted)</size></color>"
                : "Siphon\n<size=10>(+1 Core)</size>";
            if (DrawOpaqueButton(siphonRect, siphonLabel,
                new Color(0.70f, 0.55f, 0.15f, 1f), new Color(1.0f, 0.82f, 0.20f, 1f),
                currentMode == UnitActionMode.ConsumeLand, canSiphon))
            {
                SetActionMode(currentMode == UnitActionMode.ConsumeLand ? UnitActionMode.None : UnitActionMode.ConsumeLand);
            }

            // 6. Summon / Warp Titan
            TacticalUnit3D titanUnit = FindTitanUnit();
            bool titanAvailable = titanUnit != null;
            bool titanOnField = titanUnit != null && titanUnit.gameObject.activeInHierarchy;
            bool titanInReserve = titanUnit != null && !titanUnit.gameObject.activeInHierarchy;
            bool canSummonTitan = isPlayerTurn &&
                                  AbyssalRiftConduit3D.Instance != null &&
                                  currentSelectedUnit != null &&
                                  currentSelectedUnit.ElementalCores >= 1 &&
                                  titanAvailable;

            Rect summonRect = new Rect(curX, btnY, 120f, btnH);
            curX += 120f + spacing;
            string summonLabel;
            if (currentSelectedUnit != null && currentSelectedUnit.ElementalCores < 1)
                summonLabel = "<color=#90A4AE> Rift Titan\n<size=10>(Req 1 Core)</size></color>";
            else if (currentSelectedUnit != null && currentSelectedUnit.HasActedThisTurn && !titanInReserve)
                summonLabel = "<color=#90A4AE> Rift Titan\n<size=10>(Acted)</size></color>";
            else if (titanOnField)
                summonLabel = "Warp Titan\n<size=10>(Redeploy & Slam)</size>";
            else
                summonLabel = "Summon Titan\n<size=10>(Call from Rift)</size>";

            if (DrawOpaqueButton(summonRect, summonLabel,
                new Color(0.45f, 0.15f, 0.70f, 1f), new Color(0.75f, 0.25f, 1.0f, 1f),
                currentMode == UnitActionMode.SummonTitan, canSummonTitan))
            {
                SetActionMode(currentMode == UnitActionMode.SummonTitan ? UnitActionMode.None : UnitActionMode.SummonTitan);
            }

            // 7. Recall
            Rect cmdrRecallRect = new Rect(curX, btnY, 100f, btnH);
            if (DrawOpaqueButton(cmdrRecallRect, "Recall\n<size=10>(To Citadel)</size>",
                new Color(0.35f, 0.15f, 0.55f, 1f), new Color(0.65f, 0.25f, 0.95f, 1f),
                false, canAct))
            {
                StartCoroutine(RecallUnitRoutine(currentSelectedUnit));
            }
        }

        private void DrawEndTurnButton(float startX, float barWidth, float btnY, float btnH, bool isPlayerTurn)
        {
            float endTurnX = startX + barWidth - 110f - 10f;
            Rect endTurnRect = new Rect(endTurnX, btnY, 110f, btnH);
            if (DrawOpaqueButton(endTurnRect, "<b>END TURN</b>",
                new Color(0.65f, 0.15f, 0.15f, 1f), new Color(0.85f, 0.20f, 0.20f, 1f),
                false, isPlayerTurn))
            {
                if (TurnManager3D.Instance != null)
                {
                    DeselectAll();
                    TurnManager3D.Instance.EndPlayerTurn();
                }
            }
        }

        private void DrawGhostUIPreview()
        {
            if (currentHoveredTile == null ||
                (currentMode != UnitActionMode.Fireball && currentMode != UnitActionMode.WaterSurge && currentMode != UnitActionMode.EarthSpire) ||
                !activeTargetTiles.Contains(currentHoveredTile))
            {
                isGhostUIVisible = false;
                return;
            }

            isGhostUIVisible = true;
            ElementType spellElement = (currentMode == UnitActionMode.Fireball)
                ? ElementType.Fire
                : (currentMode == UnitActionMode.WaterSurge ? ElementType.Water : ElementType.Earth);
            TerrainReactionResult preview = TerrainReactionSystem.PredictReaction(currentHoveredTile.State, currentHoveredTile.TierLevel, spellElement);

            float w = 360f;
            float h = 115f;
            float x = Screen.width - w - 20f;
            float y = Screen.height - h - 85f;

            ghostUIRect = new Rect(x, y, w, h);

            Color panelBg = new Color(0.08f, 0.10f, 0.14f, 0.95f);
            Color borderColor = preview.TriggeredReaction ? new Color(1.0f, 0.75f, 0.2f, 1f) : new Color(0.3f, 0.7f, 0.4f, 1f);
            DrawSolidPanel(ghostUIRect, panelBg, borderColor, 2);

            GUILayout.BeginArea(new Rect(ghostUIRect.x + 12, ghostUIRect.y + 10, ghostUIRect.width - 24, ghostUIRect.height - 20));

            string titleColor = preview.TriggeredReaction ? "#FFD54F" : "#81C784";
            GUILayout.Label($"<size=13><color={titleColor}><b>GHOST UI: {preview.ReactionName}</b></color></size>");
            GUILayout.Label($"<b>Target:</b> {currentHoveredTile.Coordinates} | Current: {currentHoveredTile.State} (Tier {currentHoveredTile.TierLevel})");
            GUILayout.Label($"<b>Output:</b> <color=#FFA726>{preview.ResultingState} (Tier {preview.ResultingTier})</color>");
            GUILayout.Label($"<color=#B0BEC5><i>{preview.Description}</i></color>");

            GUILayout.EndArea();
        }
    }
}
