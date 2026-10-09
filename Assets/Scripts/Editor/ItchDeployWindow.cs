using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;

namespace ElementalHexTactics3D.Editor
{
    /// <summary>
    /// 1-Click Unity WebGL Build & Itch.io Deployment Automation Tool via Butler CLI.
    /// Provides one-click build compilation and delta-patch upload straight to itch.io.
    /// </summary>
    public class ItchDeployWindow : EditorWindow
    {
        private const string PREF_KEY_USERNAME = "ItchDeploy_Username";
        private const string PREF_KEY_SLUG = "ItchDeploy_GameSlug";
        private const string PREF_KEY_CHANNEL = "ItchDeploy_Channel";
        private const string PREF_KEY_BUILDPATH = "ItchDeploy_BuildPath";
        private const string PREF_KEY_AUTO_OPEN = "ItchDeploy_AutoOpenBrowser";

        private string itchUsername = "";
        private string gameSlug = "";
        private string channel = "html5";
        private string relativeBuildPath = "Builds/WebGL";
        private bool autoOpenBrowser = true;

        private Vector2 scrollPos;
        private string lastLogOutput = "";
        private bool isDeploying = false;
        private float deployProgress = 0f;
        private string deployStatusText = "";

        [MenuItem("Elemental Hex 3D/🚀 Deploy to Itch.io (WebGL)", false, 200)]
        [MenuItem("Window/Itch.io Deployer", false, 300)]
        public static void OpenWindow()
        {
            var win = GetWindow<ItchDeployWindow>("Itch.io Deployer");
            win.minSize = new Vector2(460, 520);
            win.Show();
        }

        private void OnEnable()
        {
            itchUsername = EditorPrefs.GetString(PREF_KEY_USERNAME, "");
            gameSlug = EditorPrefs.GetString(PREF_KEY_SLUG, "elemental-hex-tactics-3d");
            channel = EditorPrefs.GetString(PREF_KEY_CHANNEL, "html5");
            relativeBuildPath = EditorPrefs.GetString(PREF_KEY_BUILDPATH, "Builds/WebGL");
            autoOpenBrowser = EditorPrefs.GetBool(PREF_KEY_AUTO_OPEN, true);
            SanitizeInputs();
        }

        private void SanitizeInputs()
        {
            if (string.IsNullOrEmpty(gameSlug) && string.IsNullOrEmpty(itchUsername)) return;

            // Auto-detect and parse full itch.io URLs if pasted into gameSlug or itchUsername
            string checkUrl = gameSlug.Trim();
            if (!checkUrl.Contains("itch.io") && itchUsername.Contains("itch.io"))
            {
                checkUrl = itchUsername.Trim();
            }

            if (checkUrl.Contains("itch.io"))
            {
                try
                {
                    if (!checkUrl.StartsWith("http://") && !checkUrl.StartsWith("https://"))
                    {
                        checkUrl = "https://" + checkUrl;
                    }
                    Uri uri = new Uri(checkUrl);
                    string host = uri.Host; // e.g. nealverse.itch.io
                    if (host.EndsWith(".itch.io"))
                    {
                        string extractedUser = host.Substring(0, host.IndexOf(".itch.io")).Trim();
                        if (!string.IsNullOrEmpty(extractedUser))
                        {
                            itchUsername = extractedUser;
                        }
                    }
                    string extractedSlug = uri.AbsolutePath.Trim('/', ' ');
                    if (!string.IsNullOrEmpty(extractedSlug))
                    {
                        gameSlug = extractedSlug;
                    }
                }
                catch { }
            }

            // Clean username: remove slashes, spaces, display names
            if (itchUsername.Contains("/"))
            {
                itchUsername = itchUsername.Substring(0, itchUsername.IndexOf('/')).Trim();
            }
            itchUsername = itchUsername.Replace(" ", "").Trim();

            // Clean slug: remove http, domain prefixes, and slashes
            gameSlug = gameSlug.Replace("http://", "").Replace("https://", "").Trim();
            if (gameSlug.Contains("itch.io/"))
            {
                gameSlug = gameSlug.Substring(gameSlug.IndexOf("itch.io/") + "itch.io/".Length);
            }
            gameSlug = gameSlug.Trim('/', ' ');
        }

        private void SavePrefs()
        {
            SanitizeInputs();
            EditorPrefs.SetString(PREF_KEY_USERNAME, itchUsername.Trim());
            EditorPrefs.SetString(PREF_KEY_SLUG, gameSlug.Trim());
            EditorPrefs.SetString(PREF_KEY_CHANNEL, channel.Trim());
            EditorPrefs.SetString(PREF_KEY_BUILDPATH, relativeBuildPath.Trim());
            EditorPrefs.SetBool(PREF_KEY_AUTO_OPEN, autoOpenBrowser);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            // Header Banner
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("🎮 ITCH.IO WEBGL 1-CLICK DEPLOYER", headerStyle);
            EditorGUILayout.LabelField("Automated Unity WebGL Compilation & Butler Delta-Patching", EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.Space(10);

            // Butler Status Box
            DrawButlerStatusBox();

            EditorGUILayout.Space(10);

            // Configuration Form
            EditorGUILayout.LabelField("⚙️ Itch.io Target Settings", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();

            itchUsername = EditorGUILayout.TextField(new GUIContent("Itch.io Username", "Your itch.io URL username (e.g. nealverse)"), itchUsername);
            gameSlug = EditorGUILayout.TextField(new GUIContent("Game Project Slug", "The URL slug of your game on itch.io (e.g. elemental-hex-tactics-prototype-demo)"), gameSlug);
            channel = EditorGUILayout.TextField(new GUIContent("Channel Name", "Itch channel for web builds. Default is 'html5'"), channel);
            relativeBuildPath = EditorGUILayout.TextField(new GUIContent("WebGL Build Folder", "Folder where Unity outputs the WebGL build"), relativeBuildPath);
            autoOpenBrowser = EditorGUILayout.Toggle(new GUIContent("Auto-Open Browser on Success", "Automatically open the game webpage after successful push"), autoOpenBrowser);

            if (EditorGUI.EndChangeCheck())
            {
                SavePrefs();
            }

            // Target URL Preview
            if (!string.IsNullOrEmpty(itchUsername) && !string.IsNullOrEmpty(gameSlug))
            {
                string targetUrl = $"https://{itchUsername.Trim()}.itch.io/{gameSlug.Trim()}";
                EditorGUILayout.HelpBox($"Target: {itchUsername.Trim()}/{gameSlug.Trim()}:{channel.Trim()}\nLive URL: {targetUrl}", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Please enter your Itch.io Username and Game Slug above.", MessageType.Warning);
            }

            EditorGUILayout.Space(15);

            // Action Buttons
            bool canRun = !isDeploying && !string.IsNullOrEmpty(itchUsername) && !string.IsNullOrEmpty(gameSlug);
            string butlerPath = GetButlerExecutablePath();
            bool butlerAvailable = !string.IsNullOrEmpty(butlerPath);

            GUI.enabled = canRun && butlerAvailable;

            // Main Build & Deploy Button
            GUI.backgroundColor = new Color(0.2f, 0.75f, 0.35f);
            if (GUILayout.Button(new GUIContent("🚀 BUILD WEBGL & PUSH TO ITCH.IO", "Compiles the WebGL build and uploads it to itch.io via Butler"), GUILayout.Height(42)))
            {
                StartBuildAndDeploy(true);
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();

            // Push Existing Build Only
            string fullBuildPath = GetAbsoluteBuildPath();
            bool buildExists = Directory.Exists(fullBuildPath) && File.Exists(Path.Combine(fullBuildPath, "index.html"));
            GUI.enabled = canRun && butlerAvailable && buildExists;

            if (GUILayout.Button(new GUIContent("⚡ Push Existing Build Only", "Uploads the current WebGL folder without re-compiling Unity"), GUILayout.Height(30)))
            {
                StartBuildAndDeploy(false);
            }

            // Open Itch Page Button
            GUI.enabled = !string.IsNullOrEmpty(itchUsername) && !string.IsNullOrEmpty(gameSlug);
            if (GUILayout.Button(new GUIContent("🌐 Open Itch.io Page", "Opens your game's page in the web browser"), GUILayout.Height(30)))
            {
                Application.OpenURL($"https://{itchUsername.Trim()}.itch.io/{gameSlug.Trim()}");
            }

            EditorGUILayout.EndHorizontal();

            GUI.enabled = true;

            EditorGUILayout.Space(15);

            // Progress Bar if running
            if (isDeploying)
            {
                EditorGUILayout.LabelField(deployStatusText, EditorStyles.boldLabel);
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 20), deployProgress, deployStatusText);
                Repaint();
            }

            // Console Output Log
            if (!string.IsNullOrEmpty(lastLogOutput))
            {
                EditorGUILayout.LabelField("Butler Output Log:", EditorStyles.miniBoldLabel);
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(130));
                EditorGUILayout.TextArea(lastLogOutput, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawButlerStatusBox()
        {
            string butlerPath = GetButlerExecutablePath();
            bool hasCreds = CheckButlerCredentials();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (!string.IsNullOrEmpty(butlerPath))
            {
                if (hasCreds)
                {
                    GUI.color = new Color(0.25f, 0.85f, 0.35f);
                    EditorGUILayout.LabelField("● Butler CLI: Ready & Authenticated", EditorStyles.boldLabel);
                    GUI.color = Color.white;
                    EditorGUILayout.LabelField($"Location: {butlerPath}", EditorStyles.miniLabel);
                }
                else
                {
                    GUI.color = new Color(1.0f, 0.75f, 0.2f);
                    EditorGUILayout.LabelField("▲ Butler CLI: Found, but not logged in yet!", EditorStyles.boldLabel);
                    GUI.color = Color.white;
                    if (GUILayout.Button("🔑 Run 'butler login' now", GUILayout.Height(24)))
                    {
                        RunButlerLogin(butlerPath);
                    }
                }
            }
            else
            {
                GUI.color = new Color(0.95f, 0.35f, 0.35f);
                EditorGUILayout.LabelField("✕ Butler CLI: Not Found!", EditorStyles.boldLabel);
                GUI.color = Color.white;
                EditorGUILayout.LabelField("Place 'butler.exe' in 'Tools/butler.exe' or add to Windows PATH.", EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private string GetButlerExecutablePath()
        {
            // 1. Check project Tools folder
            string projectTools = Path.Combine(Directory.GetCurrentDirectory(), "Tools", "butler.exe");
            if (File.Exists(projectTools)) return projectTools;

            // 2. Check in PATH
            string pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathEnv))
            {
                string[] paths = pathEnv.Split(Path.PathSeparator);
                foreach (var p in paths)
                {
                    string candidate = Path.Combine(p.Trim(), "butler.exe");
                    if (File.Exists(candidate)) return candidate;
                }
            }

            return null;
        }

        private bool CheckButlerCredentials()
        {
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string credsFile = Path.Combine(userProfile, ".config", "itch", "butler_creds");
            if (File.Exists(credsFile)) return true;

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string credsFileAppData = Path.Combine(appData, "itch", "butler_creds");
            return File.Exists(credsFileAppData);
        }

        private void RunButlerLogin(string butlerExe)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = butlerExe,
                    Arguments = "login",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[ItchDeploy] Failed to launch butler login: {ex.Message}");
            }
        }

        private string GetAbsoluteBuildPath()
        {
            string clean = relativeBuildPath.Trim().Replace('/', Path.DirectorySeparatorChar);
            if (Path.IsPathRooted(clean)) return clean;
            return Path.Combine(Directory.GetCurrentDirectory(), clean);
        }

        private void StartBuildAndDeploy(bool compileFirst)
        {
            SavePrefs();
            string butlerExe = GetButlerExecutablePath();
            if (string.IsNullOrEmpty(butlerExe))
            {
                EditorUtility.DisplayDialog("Butler Missing", "Could not locate butler.exe. Please ensure it is in Tools/butler.exe or in your PATH.", "OK");
                return;
            }

            string targetBuildDir = GetAbsoluteBuildPath();
            string itchTarget = $"{itchUsername.Trim()}/{gameSlug.Trim()}:{channel.Trim()}";

            if (compileFirst)
            {
                // Save any modified scenes before compiling
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

                deployStatusText = "🔨 Compiling WebGL Build...";
                deployProgress = 0.2f;
                isDeploying = true;
                Repaint();

                bool buildSuccess = BuildWebGL(targetBuildDir);
                if (!buildSuccess)
                {
                    isDeploying = false;
                    deployStatusText = "✕ WebGL Build Failed!";
                    deployProgress = 0f;
                    EditorUtility.DisplayDialog("Build Failed", "Unity WebGL compilation failed. Please check the Console window for errors.", "OK");
                    return;
                }
            }

            // Proceed with Butler Push
            ExecuteButlerPush(butlerExe, targetBuildDir, itchTarget);
        }

        private bool BuildWebGL(string outputDirectory)
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // Gather scenes from EditorBuildSettings
            var sceneList = new List<string>();
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (s.enabled && !string.IsNullOrEmpty(s.path) && File.Exists(s.path))
                {
                    sceneList.Add(s.path);
                }
            }

            if (sceneList.Count == 0)
            {
                var activeScene = EditorSceneManager.GetActiveScene();
                if (activeScene.IsValid())
                {
                    sceneList.Add(activeScene.path);
                }
            }

            if (sceneList.Count == 0)
            {
                UnityEngine.Debug.LogError("[ItchDeploy] No valid scenes found in Build Settings!");
                return false;
            }

            // Configure WebGL settings for optimal Itch.io compatibility
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;

            BuildPlayerOptions opt = new BuildPlayerOptions
            {
                scenes = sceneList.ToArray(),
                locationPathName = outputDirectory,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            UnityEngine.Debug.Log($"<color=#00E5FF><b>[ItchDeploy]</b></color> Starting WebGL Build to <b>{outputDirectory}</b> with {sceneList.Count} scene(s)...");

            BuildReport report = BuildPipeline.BuildPlayer(opt);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                UnityEngine.Debug.Log($"<color=#69F0AE><b>[ItchDeploy]</b></color> WebGL Build Succeeded! Total size: {summary.totalSize / (1024 * 1024):F1} MB, Time: {summary.totalTime.TotalSeconds:F1}s");
                return true;
            }
            else
            {
                UnityEngine.Debug.LogError($"[ItchDeploy] WebGL Build ended with result: {summary.result} ({summary.totalErrors} errors).");
                return false;
            }
        }

        private void ExecuteButlerPush(string butlerExe, string buildDir, string target)
        {
            deployStatusText = $"🚀 Pushing to {target} via Butler...";
            deployProgress = 0.7f;
            isDeploying = true;
            lastLogOutput = $"[Butler] Running: butler push \"{buildDir}\" {target}\n";
            Repaint();

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = butlerExe,
                    Arguments = $"push \"{buildDir}\" {target}",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8
                };

                Process proc = new Process { StartInfo = psi };

                proc.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        lastLogOutput += e.Data + "\n";
                    }
                };

                proc.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        lastLogOutput += "[ERR] " + e.Data + "\n";
                    }
                };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                // Wait asynchronously via EditorApplication.update
                EditorApplication.CallbackFunction updateCheck = null;
                updateCheck = () =>
                {
                    if (proc.HasExited)
                    {
                        EditorApplication.update -= updateCheck;
                        isDeploying = false;
                        deployProgress = 1.0f;

                        int exitCode = proc.ExitCode;
                        proc.Dispose();

                        if (exitCode == 0)
                        {
                            deployStatusText = "✅ Deployment Succeeded!";
                            UnityEngine.Debug.Log($"<color=#69F0AE><b>[ItchDeploy]</b></color> Successfully deployed to Itch.io: <b>{target}</b>!");

                            string liveUrl = $"https://{itchUsername.Trim()}.itch.io/{gameSlug.Trim()}";
                            bool open = EditorUtility.DisplayDialog(
                                "🚀 Deployment Complete!",
                                $"Your latest WebGL build has been successfully pushed to Itch.io!\n\nTarget: {target}\n\nWould you like to open the game in your browser?",
                                "Open in Browser", "Done"
                            );

                            if (open || autoOpenBrowser)
                            {
                                Application.OpenURL(liveUrl);
                            }
                        }
                        else
                        {
                            deployStatusText = $"✕ Push Failed (Exit Code: {exitCode})";
                            UnityEngine.Debug.LogError($"[ItchDeploy] Butler push failed with exit code {exitCode}.\nLog:\n{lastLogOutput}");
                            EditorUtility.DisplayDialog("Butler Push Failed", $"Failed to push build to {target}.\n\nCheck the window log for details:\n{lastLogOutput}", "OK");
                        }

                        Repaint();
                    }
                };

                EditorApplication.update += updateCheck;
            }
            catch (Exception ex)
            {
                isDeploying = false;
                deployStatusText = "✕ Error Launching Butler";
                lastLogOutput += $"\n[Exception] {ex.Message}";
                UnityEngine.Debug.LogError($"[ItchDeploy] Butler execution exception: {ex}");
                EditorUtility.DisplayDialog("Deployment Error", ex.Message, "OK");
            }
        }
    }
}

