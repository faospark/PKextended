using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace PKCore.Patches;

/// <summary>
/// Portrait index caching system - separate from main texture cache
/// Speeds up boot time by caching portrait file locations
/// </summary>
public static class PortraitIndexCache
{
    private static string cachePath;
    private static string manifestPath;

    [Serializable]
    public class PortraitManifest
    {
        public int FileCount;
        public long LastModified; // Timestamp of last folder modification
        public List<string> PortraitNames = new List<string>();
    }

    /// <summary>
    /// Initialize cache paths
    /// </summary>
    public static void Initialize()
    {
        cachePath = Path.Combine(BepInEx.Paths.GameRootPath, "PKCore", "Cache");

        if (!Directory.Exists(cachePath))
        {
            Directory.CreateDirectory(cachePath);
        }
    }
    
    /// <summary>
    /// Get manifest path for specific game
    /// </summary>
    private static string GetManifestPath(string currentGame)
    {
        if (currentGame == "GSD1" || currentGame == "GSD2")
            return Path.Combine(cachePath, $"npc_portrait_index_{currentGame}.xml");
        else
            return Path.Combine(cachePath, "npc_portrait_index.xml");
    }

    /// <summary>
    /// Try to load portrait index from cache
    /// </summary>
    public static bool TryLoadIndex(string portraitsPath, string currentGame, out HashSet<string> portraitNames)
    {
        portraitNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        manifestPath = GetManifestPath(currentGame);

        if (!File.Exists(manifestPath))
            return false;

        try
        {
            XmlSerializer serializer = new XmlSerializer(typeof(PortraitManifest));
            using (FileStream stream = new FileStream(manifestPath, FileMode.Open))
            {
                PortraitManifest manifest = (PortraitManifest)serializer.Deserialize(stream);

                if (manifest != null && manifest.PortraitNames != null && manifest.PortraitNames.Count > 0)
                {
                    // Check if folder has been modified since cache was created
                    long currentModified = GetFolderLastModified(portraitsPath);
                    
                    if (manifest.LastModified == currentModified)
                    {
                        foreach (var name in manifest.PortraitNames)
                        {
                            portraitNames.Add(name);
                        }

                        if (Plugin.Config.DetailedTextureLog.Value)
                        {
                            Plugin.Log.LogInfo($"✓ Loaded portrait index from cache ({portraitNames.Count} portraits)");
                        }
                        return true;
                    }
                    else
                    {
                        Plugin.Log.LogInfo("Portrait folder modified - rebuilding portrait index");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Failed to load portrait index: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// Save portrait index to cache
    /// </summary>
    public static void SaveIndex(string portraitsPath, string currentGame, HashSet<string> portraitNames)
    {
        manifestPath = GetManifestPath(currentGame);
        try
        {
            PortraitManifest manifest = new PortraitManifest
            {
                FileCount = portraitNames.Count,
                LastModified = GetFolderLastModified(portraitsPath),
                PortraitNames = new List<string>(portraitNames)
            };

            XmlSerializer serializer = new XmlSerializer(typeof(PortraitManifest));
            using (FileStream stream = new FileStream(manifestPath, FileMode.Create))
            {
                serializer.Serialize(stream, manifest);
            }

            if (Plugin.Config.DetailedTextureLog.Value)
            {
                Plugin.Log.LogInfo($"✓ Saved portrait index cache ({manifest.FileCount} portraits)");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Failed to save portrait index: {ex.Message}");
        }
    }

    /// <summary>
    /// Get the last modified timestamp of a folder and its subdirectories
    /// </summary>
    private static long GetFolderLastModified(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            return 0;

        long maxTicks = Directory.GetLastWriteTimeUtc(folderPath).Ticks;

        // Check GSD1 and GSD2 subdirectories
        string gsd1Path = Path.Combine(folderPath, "GSD1");
        if (Directory.Exists(gsd1Path))
        {
            long gsd1Ticks = Directory.GetLastWriteTimeUtc(gsd1Path).Ticks;
            if (gsd1Ticks > maxTicks) maxTicks = gsd1Ticks;
        }

        string gsd2Path = Path.Combine(folderPath, "GSD2");
        if (Directory.Exists(gsd2Path))
        {
            long gsd2Ticks = Directory.GetLastWriteTimeUtc(gsd2Path).Ticks;
            if (gsd2Ticks > maxTicks) maxTicks = gsd2Ticks;
        }

        return maxTicks;
    }
}
