#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

[InitializeOnLoad]
public static class EffekseerMetaUpgrader
{
    private const string MigrationFlag = "Temp/EffekseerMetaUpgraded_v2";

    static EffekseerMetaUpgrader()
    {
        EditorApplication.delayCall += CheckAndUpgradeMetas;
    }

    [MenuItem("Elemental Hex 3D/Fix Effekseer Plugin Meta Warnings")]
    public static void ManualUpgrade()
    {
        PerformUpgrade(force: true);
    }

    private static void CheckAndUpgradeMetas()
    {
        if (File.Exists(MigrationFlag))
            return;

        PerformUpgrade(force: false);
    }

    private static void PerformUpgrade(bool force)
    {
        var allImporters = PluginImporter.GetAllImporters();
        int count = 0;

        foreach (var importer in allImporters)
        {
            if (importer != null && !string.IsNullOrEmpty(importer.assetPath))
            {
                string norm = importer.assetPath.Replace('\\', '/');
                if (norm.Contains("org.effekseer.effekseerforunity/Plugins") || norm.Contains("Packages/Effekseer/Plugins"))
                {
                    // Re-saving through Unity Editor automatically updates the .meta format to serializedVersion: 2
                    importer.SaveAndReimport();
                    count++;
                }
            }
        }

        try
        {
            string dir = Path.GetDirectoryName(MigrationFlag);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(MigrationFlag, $"Upgraded {count} plugin importers.");
        }
        catch { }

        if (count > 0 || force)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"<color=#55ff88>[EffekseerMetaUpgrader] Successfully re-saved and upgraded {count} Effekseer plugin meta files to serializedVersion: 2!</color>");
        }
    }
}
#endif
