#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Effekseer;
using Effekseer.Editor;

namespace ElementalHexTactics3D.Combat.Editor
{
    /// <summary>
    /// Automatically imports and compiles Effekseer VFX and settings for Elemental Hex Tactics 3D.
    /// Ensures all EVFX Blast effects (.efkefc, .efkmodel) are converted to native EffekseerEffectAsset ScriptableObjects.
    /// </summary>
    [InitializeOnLoad]
    public static class EffekseerImporterHelper
    {
        static EffekseerImporterHelper()
        {
            EditorApplication.delayCall += InitializeEffekseerAssets;
        }

        [MenuItem("Tools/Effekseer/Re-Import All EVFX Blast")]
        public static void ReimportAll()
        {
            InitializeEffekseerAssets();
        }

        public static void InitializeEffekseerAssets()
        {
            EnsureEffekseerSettings();
            ImportModels();
            ImportEffects();
        }

        private static void EnsureEffekseerSettings()
        {
            string settingsPath = "Assets/Resources/EffekseerSettings.asset";
            EffekseerSettings settings = AssetDatabase.LoadAssetAtPath<EffekseerSettings>(settingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<EffekseerSettings>();
                AssetDatabase.CreateAsset(settings, settingsPath);
                Debug.Log("[Effekseer] Created EffekseerSettings at " + settingsPath);
            }

            var preloaded = PlayerSettings.GetPreloadedAssets().ToList();
            if (!preloaded.Contains(settings))
            {
                preloaded.RemoveAll(x => x is EffekseerSettings);
                preloaded.Add(settings);
                PlayerSettings.SetPreloadedAssets(preloaded.ToArray());
                Debug.Log("[Effekseer] Registered EffekseerSettings into PlayerSettings PreloadedAssets.");
            }
        }

        private static void ImportModels()
        {
            string modelDir = "Assets/Resources/VFX/EVFX_Blast/Model";
            if (!Directory.Exists(modelDir)) return;

            string[] modelFiles = Directory.GetFiles(modelDir, "*.efkmodel", SearchOption.AllDirectories);
            foreach (var mf in modelFiles)
            {
                string normPath = mf.Replace('\\', '/');
                string targetAsset = Path.ChangeExtension(normPath, ".asset");
                if (!File.Exists(targetAsset))
                {
                    EffekseerModelAsset.CreateAsset(normPath);
                    Debug.Log("[Effekseer] Generated Model Asset: " + targetAsset);
                }
            }
        }

        private static void ImportEffects()
        {
            string vfxDir = "Assets/Resources/VFX/EVFX_Blast";
            if (!Directory.Exists(vfxDir)) return;

            // The Effekseer package's own EffekseerAssetPostProcessor converts .efkefc into
            // EffekseerEffectAsset on import (Chunk parser is internal), so we just force a reimport.
            string[] efkFiles = Directory.GetFiles(vfxDir, "*.efkefc", SearchOption.TopDirectoryOnly);
            int count = 0;
            foreach (var ef in efkFiles)
            {
                string normPath = ef.Replace('\\', '/');
                string targetAsset = Path.ChangeExtension(normPath, ".asset");
                if (!File.Exists(targetAsset))
                {
                    AssetDatabase.ImportAsset(normPath, ImportAssetOptions.ForceUpdate);
                    count++;
                }
            }

            if (count > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"<color=#00E5FF><b>[Effekseer]</b></color> Successfully compiled {count} EVFX Blast effect assets!");
            }
        }
    }
}
#endif

