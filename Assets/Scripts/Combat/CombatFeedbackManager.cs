using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ElementalHexTactics3D.Units;
using ElementalHexTactics3D.Turn;
using ElementalHexTactics3D.UI;

namespace ElementalHexTactics3D.Combat
{
    /// <summary>
    /// Manages real-time combat visual feedback:
    /// - Floating combat damage numbers with pop-up animations
    /// - Overhead 2.5D unit health bars
    /// - Dramatic turn phase announcements & action banners
    /// - 3D arc spell projectiles
    /// </summary>
    public class CombatFeedbackManager : MonoBehaviour
    {
        public static CombatFeedbackManager Instance { get; private set; }

        private class FloatingTextItem
        {
            public string Text;
            public Vector3 WorldPosition;
            public Color Color;
            public float Elapsed;
            public float Duration;
            public float VerticalOffset;
        }

        private readonly List<FloatingTextItem> activeFloatingTexts = new List<FloatingTextItem>();

        // Turn Announcement Banner State
        private string bannerTitle = "";
        private string bannerSubtitle = "";
        private float bannerTimer = 0f;
        private float bannerDuration = 0f;
        private Color bannerColor = Color.white;

        private Texture2D solidTex;
        private UnityEngine.Camera mainCamera;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            EnsureSolidTexture();
        }

        private void Start()
        {
            mainCamera = UnityEngine.Camera.main;
        }

        private void EnsureSolidTexture()
        {
            if (solidTex == null)
            {
                solidTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                Color[] px = new Color[] { Color.white, Color.white, Color.white, Color.white };
                solidTex.SetPixels(px);
                solidTex.Apply();
            }
        }

        private void Update()
        {
            if (mainCamera == null) mainCamera = UnityEngine.Camera.main;

            // Update floating texts
            for (int i = activeFloatingTexts.Count - 1; i >= 0; i--)
            {
                var item = activeFloatingTexts[i];
                item.Elapsed += Time.deltaTime;
                item.VerticalOffset += Time.deltaTime * 0.75f; // Float upwards

                if (item.Elapsed >= item.Duration)
                {
                    activeFloatingTexts.RemoveAt(i);
                }
            }

            // Update banner timer
            if (bannerTimer > 0f)
            {
                bannerTimer -= Time.deltaTime;
            }
        }

        /// <summary>
        /// Spawns a floating damage or status text popping up above a world position.
        /// </summary>
        public void SpawnDamageText(Vector3 worldPos, string text, Color color, float duration = 1.3f)
        {
            activeFloatingTexts.Add(new FloatingTextItem
            {
                Text = text,
                WorldPosition = worldPos + Vector3.up * 1.5f,
                Color = color,
                Elapsed = 0f,
                Duration = duration,
                VerticalOffset = 0f
            });
        }

        /// <summary>
        /// Displays a dramatic turn or combat action banner across the top center of the screen.
        /// </summary>
        public void ShowBanner(string title, string subtitle, float duration = 1.2f, Color? headerColor = null)
        {
            bannerTitle = title;
            bannerSubtitle = subtitle;
            bannerDuration = duration;
            bannerTimer = duration;
            bannerColor = headerColor ?? new Color(1.0f, 0.85f, 0.2f);
        }

        /// <summary>
        /// Spawns a glowing elemental spell orb that arcs smoothly from caster to target over time.
        /// </summary>
        public IEnumerator SpawnSpellProjectile(Vector3 start, Vector3 target, Color projectileColor, float duration = 0.28f)
        {
            GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "SpellProjectile";
            orb.transform.localScale = Vector3.one * 0.35f;

            // Simple glowing material
            var renderer = orb.GetComponent<MeshRenderer>();
            var col = orb.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = projectileColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", projectileColor);
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", projectileColor * 2.5f);
            renderer.sharedMaterial = mat;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                Vector3 currentXZ = Vector3.Lerp(start, target, t);
                float arcY = Mathf.Sin(t * Mathf.PI) * 0.8f;
                float currentY = Mathf.Lerp(start.y, target.y, t) + arcY;

                orb.transform.position = new Vector3(currentXZ.x, currentY, currentXZ.z);
                yield return null;
            }

            Destroy(orb);
            Destroy(mat);
        }

        /// <summary>
        /// Spawns an expanding glowing shockwave cylinder at a world position.
        /// Used for Titan Ultimate Cataclysm and Land Siphoning.
        /// </summary>
        public IEnumerator SpawnShockwaveEffect(Vector3 center, Color shockColor, float maxRadius = 2.5f, float duration = 0.40f)
        {
            GameObject wave = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wave.name = "ShockwaveEffect";
            wave.transform.position = center + Vector3.up * 0.15f;
            wave.transform.localScale = new Vector3(0.2f, 0.08f, 0.2f);

            var col = wave.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var renderer = wave.GetComponent<MeshRenderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = shockColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", shockColor);
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", shockColor * 3.0f);
            renderer.sharedMaterial = mat;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float radius = Mathf.Lerp(0.3f, maxRadius * 2f, Mathf.Sqrt(t));
                float height = Mathf.Lerp(0.4f, 0.02f, t);

                wave.transform.localScale = new Vector3(radius, height, radius);
                yield return null;
            }

            Destroy(wave);
            Destroy(mat);
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || mainCamera == null) return;

            // Completely suppress overhead health bars, turn banners, and floating numbers during story dialogue cutscenes!
            if (ElementalHexTactics3D.UI.StoryDialogueUI.Instance != null && ElementalHexTactics3D.UI.StoryDialogueUI.Instance.IsPlayingDialogue)
                return;

            bool isMenuBlocking = TitleMenuCanvasUI.Instance != null
                ? (!TitleMenuCanvasUI.Instance.IsInGame || TitleMenuCanvasUI.Instance.IsPaused)
                : (TitleMenuManager3D.Instance != null && (!TitleMenuManager3D.Instance.IsInGame || TitleMenuManager3D.Instance.IsPaused));
            if (isMenuBlocking) return;

            EnsureSolidTexture();

            DrawTurnBanner();
            DrawOverheadHealthBars();
            DrawFloatingTexts();
            // Legacy DrawEndGameModal superseded by CampaignManager and PostBattleResultsUI
        }

        private void DrawTurnBanner()
        {
            if (bannerTimer <= 0f) return;

            float alpha = Mathf.Clamp01(bannerTimer / 0.3f);
            float bannerW = 440f;
            float bannerH = 64f;

            // Ensure banner is horizontally centered on wide displays,
            // but pushed safely to the right of the top-left HUD panel (width 400 + 16 margin) on compact displays!
            float centerX = (Screen.width - bannerW) * 0.5f;
            float minX = 430f;
            float x = (Screen.width > minX + bannerW + 20f) ? Mathf.Max(minX, centerX) : centerX;
            float y = 18f;

            Rect bannerRect = new Rect(x, y, bannerW, bannerH);

            // Draw solid slate panel with glowing border
            DrawSolidRect(bannerRect, new Color(0.08f, 0.10f, 0.14f, 0.95f * alpha));
            DrawOutline(bannerRect, new Color(bannerColor.r, bannerColor.g, bannerColor.b, 0.9f * alpha), 2);

            // Header title
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 17,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = new Color(bannerColor.r, bannerColor.g, bannerColor.b, alpha);

            Rect titleRect = new Rect(bannerRect.x, bannerRect.y + 8, bannerRect.width, 26);
            GUI.Label(titleRect, bannerTitle, titleStyle);

            // Subtitle description
            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Normal
            };
            subStyle.normal.textColor = new Color(0.9f, 0.92f, 0.95f, alpha);

            Rect subRect = new Rect(bannerRect.x, bannerRect.y + 34, bannerRect.width, 24);
            GUI.Label(subRect, bannerSubtitle, subStyle);
        }

        private void DrawOverheadHealthBars()
        {
            TacticalUnit3D[] units = FindObjectsByType<TacticalUnit3D>(FindObjectsSortMode.None);
            if (units == null || units.Length == 0) return;

            float barWidth = 74f;
            float barHeight = 10f;

            foreach (var unit in units)
            {
                if (unit == null || unit.MaxHealth <= 0) continue;

                Vector3 headWorldPos = unit.transform.position + Vector3.up * 1.6f;
                Vector3 screenPos = mainCamera.WorldToScreenPoint(headWorldPos);

                // Ignore if behind camera
                if (screenPos.z <= 0f) continue;

                float screenY = Screen.height - screenPos.y;
                Rect barRect = new Rect(screenPos.x - barWidth * 0.5f, screenY, barWidth, barHeight);

                // Background
                DrawSolidRect(barRect, new Color(0.08f, 0.08f, 0.10f, 0.95f));

                // HP Fill
                float pct = Mathf.Clamp01((float)unit.CurrentHealth / unit.MaxHealth);
                Color fillColor = (unit.Faction == UnitFaction.Player)
                    ? new Color(0.15f, 0.65f, 1.0f, 1f)  // Player Azure
                    : new Color(0.95f, 0.22f, 0.18f, 1f); // Enemy Red

                Rect fillRect = new Rect(barRect.x + 1, barRect.y + 1, (barWidth - 2) * pct, barHeight - 2);
                DrawSolidRect(fillRect, fillColor);

                // Border
                DrawOutline(barRect, new Color(0.40f, 0.45f, 0.55f, 0.9f), 1);

                // Text above bar with shadow
                GUIStyle hpTextStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11,
                    fontStyle = FontStyle.Bold
                };

                // Drop shadow
                hpTextStyle.normal.textColor = Color.black;
                GUI.Label(new Rect(barRect.x + 1, barRect.y - 17, barWidth, 16), $"{unit.CurrentHealth} / {unit.MaxHealth}", hpTextStyle);

                // Main white text
                hpTextStyle.normal.textColor = Color.white;
                GUI.Label(new Rect(barRect.x, barRect.y - 18, barWidth, 16), $"{unit.CurrentHealth} / {unit.MaxHealth}", hpTextStyle);
            }
        }

        private void DrawFloatingTexts()
        {
            GUIStyle textStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 15
            };

            foreach (var item in activeFloatingTexts)
            {
                Vector3 screenPos = mainCamera.WorldToScreenPoint(item.WorldPosition + Vector3.up * item.VerticalOffset);
                if (screenPos.z <= 0f) continue;

                float screenY = Screen.height - screenPos.y;
                float progress = item.Elapsed / item.Duration;
                float alpha = (progress < 0.7f) ? 1f : Mathf.Lerp(1f, 0f, (progress - 0.7f) / 0.3f);

                Rect textRect = new Rect(screenPos.x - 120, screenY - 14, 240, 28);

                // Shadow for contrast
                textStyle.normal.textColor = new Color(0f, 0f, 0f, alpha * 0.85f);
                GUI.Label(new Rect(textRect.x + 1, textRect.y + 1, textRect.width, textRect.height), item.Text, textStyle);

                // Text
                textStyle.normal.textColor = new Color(item.Color.r, item.Color.g, item.Color.b, alpha);
                GUI.Label(textRect, item.Text, textStyle);
            }
        }

        private void DrawSolidRect(Rect rect, Color color)
        {
            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, solidTex, ScaleMode.StretchToFill);
            GUI.color = prev;
        }

        private void DrawOutline(Rect rect, Color color, int thickness)
        {
            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), solidTex);
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), solidTex);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), solidTex);
            GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), solidTex);
            GUI.color = prev;
        }

        private void DrawEndGameModal()
        {
            if (TurnManager3D.Instance == null) return;
            BattleResult result = TurnManager3D.Instance.Result;
            if (result == BattleResult.InProgress) return;

            // 1. Full-screen dimmed backdrop
            DrawSolidRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.72f));

            // 2. Centered Modal Card
            float cardW = 460f;
            float cardH = 280f;
            float cardX = (Screen.width - cardW) * 0.5f;
            float cardY = (Screen.height - cardH) * 0.5f;
            Rect cardRect = new Rect(cardX, cardY, cardW, cardH);

            bool isVictory = (result == BattleResult.Victory);

            Color bgColor = isVictory ? new Color(0.06f, 0.12f, 0.08f, 0.98f) : new Color(0.14f, 0.06f, 0.06f, 0.98f);
            Color borderColor = isVictory ? new Color(1.0f, 0.84f, 0.20f, 1.0f) : new Color(0.95f, 0.25f, 0.20f, 1.0f);
            Color btnColor = isVictory ? new Color(0.15f, 0.65f, 0.35f, 1f) : new Color(0.80f, 0.22f, 0.20f, 1f);
            Color btnHover = isVictory ? new Color(0.25f, 0.85f, 0.45f, 1f) : new Color(1.0f, 0.35f, 0.30f, 1f);

            DrawSolidRect(cardRect, bgColor);
            DrawOutline(cardRect, borderColor, 3);

            // Title
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = isVictory ? new Color(1.0f, 0.85f, 0.2f) : new Color(1.0f, 0.3f, 0.3f);
            Rect titleRect = new Rect(cardX, cardY + 22, cardW, 34);
            GUI.Label(titleRect, isVictory ? "🏆 VICTORY!" : "💀 DEFEAT", titleStyle);

            // Subtitle
            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Normal
            };
            subStyle.normal.textColor = new Color(0.85f, 0.88f, 0.92f);
            Rect subRect = new Rect(cardX + 20, cardY + 58, cardW - 40, 24);
            string subText = isVictory
                ? "Enemy forces have been vanquished from the realm!"
                : "Your squad has fallen in battle. The enemy has prevailed.";
            GUI.Label(subRect, subText, subStyle);

            // Stats box
            Rect statsRect = new Rect(cardX + 24, cardY + 98, cardW - 48, 72);
            DrawSolidRect(statsRect, new Color(0f, 0f, 0f, 0.45f));
            DrawOutline(statsRect, new Color(borderColor.r, borderColor.g, borderColor.b, 0.4f), 1);

            int squadHp = 0;
            int squadMaxHp = 0;
            int aliveCount = 0;
            TacticalUnit3D[] units = FindObjectsByType<TacticalUnit3D>(FindObjectsSortMode.None);
            foreach (var u in units)
            {
                if (u != null && u.Faction == UnitFaction.Player)
                {
                    squadHp += u.CurrentHealth;
                    squadMaxHp += u.MaxHealth;
                    if (u.CurrentHealth > 0) aliveCount++;
                }
            }

            GUIStyle statStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                richText = true
            };
            statStyle.normal.textColor = Color.white;

            Rect statLine1 = new Rect(statsRect.x, statsRect.y + 12, statsRect.width, 22);
            string hpColor = squadHp > 0 ? "#81C784" : "#EF5350";
            GUI.Label(statLine1, $"<b>Rounds Elapsed:</b> {TurnManager3D.Instance.CurrentRound}   |   <b>Squad HP:</b> <color={hpColor}>{squadHp} / {squadMaxHp} ({aliveCount} Alive)</color>", statStyle);

            Rect statLine2 = new Rect(statsRect.x, statsRect.y + 38, statsRect.width, 22);
            string outcomeColor = isVictory ? "#FFD54F" : "#EF5350";
            string outcomeText = isVictory ? "Tactical Triumph" : "Tactical Failure";
            GUI.Label(statLine2, $"<b>Outcome:</b> <color={outcomeColor}>{outcomeText}</color>", statStyle);

            // Action Button
            Rect btnRect = new Rect(cardX + 36, cardY + 195, cardW - 72, 46);
            Vector2 mousePos = Event.current.mousePosition;
            bool isHover = btnRect.Contains(mousePos);

            DrawSolidRect(btnRect, isHover ? btnHover : btnColor);
            DrawOutline(btnRect, isHover ? Color.white : new Color(1f, 1f, 1f, 0.65f), isHover ? 2 : 1);

            GUIStyle btnLabelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            btnLabelStyle.normal.textColor = Color.white;

            string btnText = isVictory ? "RESTART BATTLE" : "RETRY BATTLE";
            GUI.Label(btnRect, btnText, btnLabelStyle);

            if (Event.current.type == EventType.MouseDown && btnRect.Contains(mousePos))
            {
                Event.current.Use();
                TurnManager3D.Instance.RestartBattle();
            }
        }

        private void OnDestroy()
        {
            if (solidTex != null) Destroy(solidTex);
        }
    }
}

