using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using ElementalHexTactics3D.Combat;
using ElementalHexTactics3D.Turn;
using ElementalHexTactics3D.CameraControl;

namespace ElementalHexTactics3D.UI
{
    public enum TitleMenuState
    {
        TitleScreen,
        InGame,
        OptionsModal,
        StoryPrologueModal,
        HowToPlayModal,
        InGamePauseModal
    }

    /// <summary>
    /// Manages the Game Title Screen, Light Novel / Manhwa narrative prologue,
    /// in-game options, and pause menu.
    /// Title: "I Reincarnated as a Beast Talker, Now I'm Collecting Beasts!"
    /// </summary>
    public class TitleMenuManager3D : MonoBehaviour
    {
        private static TitleMenuManager3D instance;
        public static TitleMenuManager3D Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Object.FindFirstObjectByType<TitleMenuManager3D>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("TitleMenuManager3D");
                        instance = go.AddComponent<TitleMenuManager3D>();
                    }
                }
                return instance;
            }
            private set => instance = value;
        }

        [Header("State")]
        [SerializeField] private TitleMenuState currentState = TitleMenuState.TitleScreen;
        [SerializeField] private bool autoOrbitCameraOnTitle = true;
        [SerializeField] private float titleOrbitSpeed = 8f;

        // Audio & Controls cached settings
        private float sfxVolume = 0.8f;
        private bool isMuted = false;
        private bool disableAi = false;
        private float cameraSpeed = 1.0f;

        // GUI Textures
        private Texture2D solidTex;
        private Texture2D outlineTex;

        // Public Accessors
        public bool IsInGame => currentState == TitleMenuState.InGame;
        public bool IsOnTitleScreen => currentState == TitleMenuState.TitleScreen;
        public bool IsPaused => currentState == TitleMenuState.InGamePauseModal || 
                                currentState == TitleMenuState.OptionsModal && previousState == TitleMenuState.InGamePauseModal;

        private TitleMenuState previousState = TitleMenuState.TitleScreen;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            EnsureTextures();

            // Sync initial audio and AI state
            if (SoundManager3D.Instance != null)
            {
                isMuted = SoundManager3D.Instance.IsMuted;
            }

            if (TurnManager3D.Instance != null)
            {
                disableAi = TurnManager3D.Instance.DisableEnemyAI;
            }
        }

        private void Update()
        {
            if (TitleMenuCanvasUI.Instance != null) return; // Yield to modern 2D Canvas UI

            // Title screen camera orbit
            if (currentState == TitleMenuState.TitleScreen && autoOrbitCameraOnTitle)
            {
                if (TacticalCameraController.Instance != null)
                {
                    // Smoothly orbit around center
                    // We access through reflection or input simulation, or direct targetYaw adjustment if public
                }
            }

            // Global Escape key handling
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                HandleEscapeKey();
            }
        }

        public void HandleEscapeKey()
        {
            if (currentState == TitleMenuState.InGame)
            {
                OpenPauseMenu();
            }
            else if (currentState == TitleMenuState.InGamePauseModal)
            {
                ResumeGame();
            }
            else if (currentState == TitleMenuState.OptionsModal || 
                     currentState == TitleMenuState.StoryPrologueModal || 
                     currentState == TitleMenuState.HowToPlayModal)
            {
                CloseModal();
            }
        }

        public void StartGame()
        {
            PlaySoundClick();
            currentState = TitleMenuState.InGame;

            if (TacticalCameraController.Instance != null)
            {
                TacticalCameraController.Instance.ResetToTacticalView();
            }

            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.ShowBanner(
                    "ADVENTURE BEGINS!", 
                    "Rank-F Tamer enters the tactical dungeon. Defeat the tyrant forces!", 
                    1.4f, 
                    new Color(0.2f, 0.8f, 1.0f)
                );
            }
        }

        public void OpenOptions(TitleMenuState fromState)
        {
            PlaySoundClick();
            previousState = fromState;
            currentState = TitleMenuState.OptionsModal;
        }

        public void OpenStoryPrologue()
        {
            PlaySoundClick();
            previousState = currentState;
            currentState = TitleMenuState.StoryPrologueModal;
        }

        public void OpenHowToPlay()
        {
            PlaySoundClick();
            previousState = currentState;
            currentState = TitleMenuState.HowToPlayModal;
        }

        public void OpenPauseMenu()
        {
            PlaySoundClick();
            previousState = TitleMenuState.InGame;
            currentState = TitleMenuState.InGamePauseModal;
        }

        public void ResumeGame()
        {
            PlaySoundClick();
            currentState = TitleMenuState.InGame;
        }

        public void CloseModal()
        {
            PlaySoundClick();
            currentState = previousState;
        }

        public void ReturnToTitleScreen()
        {
            PlaySoundClick();
            currentState = TitleMenuState.TitleScreen;
        }

        public void QuitGame()
        {
            PlaySoundClick();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void PlaySoundClick()
        {
            if (SoundManager3D.Instance != null)
            {
                SoundManager3D.Instance.PlayButtonClick();
            }
        }

        // ================= GUI RENDERING ================= //

        private void OnGUI()
        {
            // If modern 2D Canvas UI is present, completely suppress legacy IMGUI
            if (TitleMenuCanvasUI.Instance != null) return;

            if (!Application.isPlaying) return;
            EnsureTextures();

            switch (currentState)
            {
                case TitleMenuState.TitleScreen:
                    DrawTitleScreen();
                    break;

                case TitleMenuState.OptionsModal:
                    DrawOptionsModal();
                    break;

                case TitleMenuState.StoryPrologueModal:
                    DrawStoryPrologueModal();
                    break;

                case TitleMenuState.HowToPlayModal:
                    DrawHowToPlayModal();
                    break;

                case TitleMenuState.InGamePauseModal:
                    DrawInGamePauseModal();
                    break;

                case TitleMenuState.InGame:
                    DrawInGameMenuButton();
                    break;
            }
        }

        private void DrawTitleScreen()
        {
            // 1. Full-screen subtle dark gradient vignette
            DrawSolidRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.06f, 0.10f, 0.78f));

            // Top cinematic decorative bar
            DrawSolidRect(new Rect(0, 0, Screen.width, 10), new Color(1.0f, 0.82f, 0.2f, 0.9f));

            // 2. Title Card / Header Container (Top-Center)
            float bannerW = Mathf.Min(780f, Screen.width - 40f);
            float bannerH = 175f;
            float bannerX = (Screen.width - bannerW) * 0.5f;
            float bannerY = 45f;
            Rect bannerRect = new Rect(bannerX, bannerY, bannerW, bannerH);

            DrawSolidRect(bannerRect, new Color(0.08f, 0.11f, 0.16f, 0.94f));
            DrawOutline(bannerRect, new Color(1.0f, 0.82f, 0.25f, 0.9f), 2);

            // Webnovel Badge
            GUIStyle badgeStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            badgeStyle.normal.textColor = new Color(0.35f, 0.85f, 1.0f);
            GUI.Label(new Rect(bannerX, bannerY + 12, bannerW, 20), "✦ LIGHT NOVEL / MANHWA EDITION • PROLOGUE: RANK-F ✦", badgeStyle);

            // Main Title: Line 1
            GUIStyle title1Style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 25,
                fontStyle = FontStyle.Bold
            };
            title1Style.normal.textColor = Color.white;
            GUI.Label(new Rect(bannerX, bannerY + 34, bannerW, 36), "I REINCARNATED AS A BEAST TALKER,", title1Style);

            // Main Title: Line 2 (Gold highlight)
            GUIStyle title2Style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 27,
                fontStyle = FontStyle.Bold
            };
            title2Style.normal.textColor = new Color(1.0f, 0.85f, 0.25f);
            GUI.Label(new Rect(bannerX, bannerY + 68, bannerW, 40), "NOW I'M COLLECTING BEASTS!", title2Style);

            // Subtitle
            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Italic
            };
            subStyle.normal.textColor = new Color(0.80f, 0.85f, 0.92f);
            GUI.Label(new Rect(bannerX, bannerY + 112, bannerW, 24), "〜 2.5D Tactical Hex Battle & Primordial Resonance 〜", subStyle);

            // Version info tag
            GUIStyle verStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11
            };
            verStyle.normal.textColor = new Color(0.6f, 0.65f, 0.72f);
            GUI.Label(new Rect(bannerX, bannerY + 140, bannerW, 20), "Elemental Hex Tactics 3D • TGFI Pre-Alpha v0.2.0 • Solo Dev by Neal Sage", verStyle);

            // 3. Center Menu Buttons Card
            float menuW = 380f;
            float menuH = 310f;
            float menuX = (Screen.width - menuW) * 0.5f;
            float menuY = bannerY + bannerH + 35f;
            Rect menuRect = new Rect(menuX, menuY, menuW, menuH);

            DrawSolidRect(menuRect, new Color(0.06f, 0.08f, 0.12f, 0.92f));
            DrawOutline(menuRect, new Color(0.25f, 0.38f, 0.55f, 0.8f), 2);

            float btnW = menuW - 48f;
            float btnH = 44f;
            float startBtnY = menuY + 22f;
            float spacingY = 54f;

            // Button 1: PLAY GAME
            if (DrawCustomButton(new Rect(menuX + 24, startBtnY, btnW, btnH), "⚔️ PLAY ADVENTURE", new Color(0.18f, 0.65f, 0.38f), new Color(0.28f, 0.85f, 0.48f)))
            {
                StartGame();
            }

            // Button 2: STORY PROLOGUE
            if (DrawCustomButton(new Rect(menuX + 24, startBtnY + spacingY, btnW, btnH), "📖 STORY & LORE (Rank-F)", new Color(0.15f, 0.42f, 0.68f), new Color(0.25f, 0.58f, 0.92f)))
            {
                OpenStoryPrologue();
            }

            // Button 3: HOW TO PLAY
            if (DrawCustomButton(new Rect(menuX + 24, startBtnY + spacingY * 2, btnW, btnH), "🎮 HOW TO PLAY (Tactics)", new Color(0.45f, 0.32f, 0.68f), new Color(0.62f, 0.45f, 0.92f)))
            {
                OpenHowToPlay();
            }

            // Button 4: OPTIONS
            if (DrawCustomButton(new Rect(menuX + 24, startBtnY + spacingY * 3, btnW, btnH), "⚙️ OPTIONS (Settings)", new Color(0.30f, 0.35f, 0.42f), new Color(0.42f, 0.48f, 0.58f)))
            {
                OpenOptions(TitleMenuState.TitleScreen);
            }

            // Button 5: EXIT
            if (DrawCustomButton(new Rect(menuX + 24, startBtnY + spacingY * 4, btnW, btnH), "🚪 EXIT GAME", new Color(0.48f, 0.18f, 0.20f), new Color(0.70f, 0.25f, 0.28f)))
            {
                QuitGame();
            }
        }

        private void DrawStoryPrologueModal()
        {
            DrawSolidRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.82f));

            float cardW = Mathf.Min(680f, Screen.width - 40f);
            float cardH = 500f;
            float cardX = (Screen.width - cardW) * 0.5f;
            float cardY = (Screen.height - cardH) * 0.5f;
            Rect cardRect = new Rect(cardX, cardY, cardW, cardH);

            DrawSolidRect(cardRect, new Color(0.07f, 0.10f, 0.15f, 0.98f));
            DrawOutline(cardRect, new Color(0.30f, 0.75f, 1.0f, 0.95f), 2);

            // Title
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
            headerStyle.normal.textColor = new Color(1.0f, 0.85f, 0.3f);
            GUI.Label(new Rect(cardX, cardY + 16, cardW, 30), "📖 STORY PROLOGUE: THE WEAKEST TAMER'S AWAKENING", headerStyle);

            // Content Box
            Rect bodyRect = new Rect(cardX + 24, cardY + 54, cardW - 48, cardH - 130);
            DrawSolidRect(bodyRect, new Color(0f, 0f, 0f, 0.45f));
            DrawOutline(bodyRect, new Color(0.2f, 0.35f, 0.5f, 0.5f), 1);

            GUILayout.BeginArea(new Rect(bodyRect.x + 16, bodyRect.y + 12, bodyRect.width - 32, bodyRect.height - 24));
            
            GUILayout.Label("<color=#64B5F6><b>[ The Reincarnation ]</b></color>");
            GUILayout.Label("You were suddenly transported from modern Earth into the fantasy continent of <b>Terranox</b>—a world where power and nobility are dictated by the ferocious beasts you command.");
            GUILayout.Space(6);

            GUILayout.Label("<color=#EF5350><b>[ The Tyrant's Cruelty: Cursed Collars ]</b></color>");
            GUILayout.Label("Aristocrats and corrupt guild masters treat monsters as disposable weapons, enslaving them with painful <b>Cursed Collars</b>. Because you had zero martial arts or offensive magic, the Guild stamped you as a pathetic <b>Rank-F Trash Tamer</b>.");
            GUILayout.Space(6);

            GUILayout.Label("<color=#FFD54F><b>[ The God-Given Cheat: Beast Resonance ]</b></color>");
            GUILayout.Label("Unknown to anyone, you possess a forbidden divine cheat: <b>[Beast Talker & Primordial Resonance]</b>. You understand the souls and cries of monsters instantly! When you treat a beast with empathy, they resonate with the earth beneath them.");
            GUILayout.Space(6);

            GUILayout.Label("<color=#81C784><b>[ Your Mission: Shatter the Chains & Awaken Titans ]</b></color>");
            GUILayout.Label("Delve into dangerous 3D dungeon plateaus. Defeat tyrant summoners to break their cursed collars. Gather ancient <b>Elemental Cores</b>, feed your beasts the living land, and awaken apocalyptic Primordial Titans to dismantle the corrupt empire!");

            GUILayout.EndArea();

            // Back button
            if (DrawCustomButton(new Rect(cardX + (cardW - 200f) * 0.5f, cardY + cardH - 58f, 200f, 40f), "🔙 BACK TO MENU", new Color(0.25f, 0.35f, 0.45f), new Color(0.4f, 0.55f, 0.7f)))
            {
                CloseModal();
            }
        }

        private void DrawHowToPlayModal()
        {
            DrawSolidRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.82f));

            float cardW = Mathf.Min(720f, Screen.width - 40f);
            float cardH = 510f;
            float cardX = (Screen.width - cardW) * 0.5f;
            float cardY = (Screen.height - cardH) * 0.5f;
            Rect cardRect = new Rect(cardX, cardY, cardW, cardH);

            DrawSolidRect(cardRect, new Color(0.07f, 0.10f, 0.15f, 0.98f));
            DrawOutline(cardRect, new Color(0.85f, 0.50f, 1.0f, 0.95f), 2);

            // Title
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
            headerStyle.normal.textColor = new Color(1.0f, 0.85f, 0.3f);
            GUI.Label(new Rect(cardX, cardY + 16, cardW, 30), "🎮 TACTICAL COMBAT & TERRAFORM GUIDE", headerStyle);

            Rect bodyRect = new Rect(cardX + 24, cardY + 54, cardW - 48, cardH - 130);
            DrawSolidRect(bodyRect, new Color(0f, 0f, 0f, 0.45f));
            DrawOutline(bodyRect, new Color(0.35f, 0.25f, 0.5f, 0.5f), 1);

            GUILayout.BeginArea(new Rect(bodyRect.x + 16, bodyRect.y + 12, bodyRect.width - 32, bodyRect.height - 24));
            
            GUILayout.Label("<color=#64B5F6><b>1. Camera Controls:</b></color> <b>Q / E</b> Rotate 60° | <b>WASD</b> Pan Focus | <b>Mouse Scroll</b> Zoom");
            GUILayout.Label("<color=#64B5F6><b>2. Tactical Selection:</b></color> <b>Left-Click</b> to Select Unit / Cast Spell | <b>Right-Click</b> to Deselect/Cancel");
            GUILayout.Space(6);

            GUILayout.Label("<color=#FFA726><b>3. Dynamic Terraforming (Divinity Style):</b></color>");
            GUILayout.Label("• 🔥 <b>Fireball:</b> Chars grass into <b>Scorched Earth (Tier 1)</b>; a second Fireball melts it into molten <b>Magma (Tier 2)</b> (damages enemies, gives Fire Titan +2 ATK!).");
            GUILayout.Label("• 💧 <b>Water Blast:</b> Extinguishes flames into <b>Steam Smokescreens</b>; creates <b>Puddles/Lakes</b>.");
            GUILayout.Label("• 🪨 <b>Earth Spire:</b> Erupts massive <b>Stone Pillars</b> (creates physical barriers and collision walls).");
            GUILayout.Space(6);

            GUILayout.Label("<color=#EF5350><b>4. Kinetic Push & Wall Slams (Into the Breach):</b></color>");
            GUILayout.Label("• 💥 <b>Kinetic Shove:</b> Push enemies 1 hex away. Slamming an enemy into a Stone Pillar, cliff, or another unit inflicts <b>💥 -2 HP WALL SLAM damage</b>!");
            GUILayout.Label("• Shove fragile enemy Tamers into their own lava or deep water to neutralize them instantly!");
            GUILayout.Space(6);

            GUILayout.Label("<color=#FFD54F><b>5. Siphon Land & Cataclysm:</b></color>");
            GUILayout.Label("• ⚡ <b>Siphon:</b> Your Titan drains active lava, turning it into Barren Earth to harvest <b>Elemental Cores</b>.");
            GUILayout.Label("• 🌋 <b>Magma Cataclysm:</b> Spend 3 Cores to trigger a screen-shattering volcanic explosion wiping the field!");

            GUILayout.EndArea();

            if (DrawCustomButton(new Rect(cardX + (cardW - 200f) * 0.5f, cardY + cardH - 58f, 200f, 40f), "🔙 BACK TO MENU", new Color(0.25f, 0.35f, 0.45f), new Color(0.4f, 0.55f, 0.7f)))
            {
                CloseModal();
            }
        }

        private void DrawOptionsModal()
        {
            DrawSolidRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.82f));

            float cardW = 500f;
            float cardH = 380f;
            float cardX = (Screen.width - cardW) * 0.5f;
            float cardY = (Screen.height - cardH) * 0.5f;
            Rect cardRect = new Rect(cardX, cardY, cardW, cardH);

            DrawSolidRect(cardRect, new Color(0.08f, 0.11f, 0.16f, 0.98f));
            DrawOutline(cardRect, new Color(0.4f, 0.6f, 0.8f, 0.95f), 2);

            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
            headerStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(cardX, cardY + 16, cardW, 30), "⚙️ GAME OPTIONS & SETTINGS", headerStyle);

            Rect bodyRect = new Rect(cardX + 24, cardY + 56, cardW - 48, cardH - 130);
            DrawSolidRect(bodyRect, new Color(0f, 0f, 0f, 0.45f));
            DrawOutline(bodyRect, new Color(0.2f, 0.3f, 0.4f, 0.5f), 1);

            GUILayout.BeginArea(new Rect(bodyRect.x + 16, bodyRect.y + 14, bodyRect.width - 32, bodyRect.height - 24));

            // 1. Audio Mute Toggle
            GUILayout.BeginHorizontal();
            GUILayout.Label("<b>Sound & Audio:</b>", GUILayout.Width(130));
            string audioBtnText = isMuted ? "🔇 Unmute Audio" : "🔊 Mute Audio";
            if (GUILayout.Button(audioBtnText, GUILayout.Height(28)))
            {
                isMuted = !isMuted;
                if (SoundManager3D.Instance != null)
                {
                    SoundManager3D.Instance.IsMuted = isMuted;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(12);

            // 2. Enemy AI Toggle (Testing Mode)
            GUILayout.BeginHorizontal();
            GUILayout.Label("<b>Enemy AI:</b>", GUILayout.Width(130));
            string aiBtnText = disableAi ? "🤖 AI: DISABLED (Testing Mode)" : "🤖 AI: ENABLED (Normal Battle)";
            Color aiCol = disableAi ? new Color(0.9f, 0.4f, 0.2f) : new Color(0.3f, 0.8f, 0.4f);
            GUI.color = aiCol;
            if (GUILayout.Button(aiBtnText, GUILayout.Height(28)))
            {
                disableAi = !disableAi;
                if (TurnManager3D.Instance != null)
                {
                    TurnManager3D.Instance.DisableEnemyAI = disableAi;
                }
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(12);

            // 3. Screen Resolution / Fullscreen
            GUILayout.BeginHorizontal();
            GUILayout.Label("<b>Display Mode:</b>", GUILayout.Width(130));
            string fsBtnText = Screen.fullScreen ? "🖥️ Windowed Mode" : "🔲 Fullscreen Mode";
            if (GUILayout.Button(fsBtnText, GUILayout.Height(28)))
            {
                Screen.fullScreen = !Screen.fullScreen;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(12);
            GUILayout.Label("<color=#B0BEC5><i>Tip: You can also toggle Enemy AI during battle with hotkey <b>F1</b>.</i></color>");

            GUILayout.EndArea();

            if (DrawCustomButton(new Rect(cardX + (cardW - 180f) * 0.5f, cardY + cardH - 58f, 180f, 40f), "🔙 BACK", new Color(0.25f, 0.35f, 0.45f), new Color(0.4f, 0.55f, 0.7f)))
            {
                CloseModal();
            }
        }

        private void DrawInGamePauseModal()
        {
            DrawSolidRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.75f));

            float cardW = 380f;
            float cardH = 320f;
            float cardX = (Screen.width - cardW) * 0.5f;
            float cardY = (Screen.height - cardH) * 0.5f;
            Rect cardRect = new Rect(cardX, cardY, cardW, cardH);

            DrawSolidRect(cardRect, new Color(0.08f, 0.11f, 0.16f, 0.98f));
            DrawOutline(cardRect, new Color(1.0f, 0.82f, 0.25f, 0.9f), 2);

            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold
            };
            headerStyle.normal.textColor = new Color(1.0f, 0.85f, 0.3f);
            GUI.Label(new Rect(cardX, cardY + 18, cardW, 30), "⏸️ BATTLE PAUSED", headerStyle);

            float btnW = cardW - 60f;
            float btnH = 42f;
            float startY = cardY + 68f;
            float spacing = 50f;

            if (DrawCustomButton(new Rect(cardX + 30, startY, btnW, btnH), "▶️ RESUME BATTLE", new Color(0.18f, 0.65f, 0.38f), new Color(0.28f, 0.85f, 0.48f)))
            {
                ResumeGame();
            }

            if (DrawCustomButton(new Rect(cardX + 30, startY + spacing, btnW, btnH), "⚙️ OPTIONS", new Color(0.30f, 0.38f, 0.48f), new Color(0.42f, 0.52f, 0.65f)))
            {
                OpenOptions(TitleMenuState.InGamePauseModal);
            }

            if (DrawCustomButton(new Rect(cardX + 30, startY + spacing * 2, btnW, btnH), "🔄 RESTART BATTLE", new Color(0.72f, 0.45f, 0.15f), new Color(0.92f, 0.60f, 0.25f)))
            {
                ResumeGame();
                if (TurnManager3D.Instance != null)
                {
                    TurnManager3D.Instance.RestartBattle();
                }
            }

            if (DrawCustomButton(new Rect(cardX + 30, startY + spacing * 3, btnW, btnH), "🏠 MAIN MENU", new Color(0.55f, 0.20f, 0.22f), new Color(0.75f, 0.28f, 0.30f)))
            {
                ReturnToTitleScreen();
            }
        }

        private void DrawInGameMenuButton()
        {
            // Top-right compact Menu button (Esc)
            float btnW = 95f;
            float btnH = 26f;
            float btnX = Screen.width - btnW - 16f;
            float btnY = 16f;
            Rect menuBtnRect = new Rect(btnX, btnY, btnW, btnH);

            if (DrawCustomButton(menuBtnRect, "⚙️ Menu (Esc)", new Color(0.12f, 0.16f, 0.22f, 0.95f), new Color(0.22f, 0.32f, 0.45f, 1f), fontSize: 11))
            {
                OpenPauseMenu();
            }
        }

        // ================= DRAWING HELPERS ================= //

        private bool DrawCustomButton(Rect rect, string text, Color baseColor, Color hoverColor, int fontSize = 14)
        {
            Vector2 mousePos = Event.current.mousePosition;
            bool isHover = rect.Contains(mousePos);

            DrawSolidRect(rect, isHover ? hoverColor : baseColor);
            DrawOutline(rect, isHover ? Color.white : new Color(1f, 1f, 1f, 0.45f), isHover ? 2 : 1);

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = fontSize,
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor = Color.white;

            GUI.Label(rect, text, style);

            if (Event.current.type == EventType.MouseDown && isHover)
            {
                Event.current.Use();
                return true;
            }

            return false;
        }

        private void EnsureTextures()
        {
            if (solidTex == null)
            {
                solidTex = new Texture2D(1, 1);
                solidTex.SetPixel(0, 0, Color.white);
                solidTex.Apply();
            }
            if (outlineTex == null)
            {
                outlineTex = new Texture2D(1, 1);
                outlineTex.SetPixel(0, 0, Color.white);
                outlineTex.Apply();
            }
        }

        private void DrawSolidRect(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, solidTex);
            GUI.color = old;
        }

        private void DrawOutline(Rect rect, Color color, int thickness)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), outlineTex);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), outlineTex);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), outlineTex);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), outlineTex);
            GUI.color = old;
        }

        private void OnDestroy()
        {
            if (solidTex != null) Destroy(solidTex);
            if (outlineTex != null) Destroy(outlineTex);
        }
    }
}
