using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using BepInEx;
using System.Xml.Serialization;

namespace PKCore.Patches;

public partial class CustomTexturePatch
{
    private static string cachePath;
    private static string manifestPath;

    [Serializable]
    public class ManifestEntry
    {
        [XmlAttribute]
        public string Key;
        [XmlAttribute]
        public string Value;
    }

    [Serializable]
    public class TextureManifest
    {
        public int FileCount; // Number of texture files indexed
        public string ConfigHash; // Hash of texture-related config settings
        public List<ManifestEntry> Entries = new List<ManifestEntry>();

        public void FromDictionary(Dictionary<string, string> dict)
        {
            Entries.Clear();
            foreach (var kvp in dict)
            {
                Entries.Add(new ManifestEntry { Key = kvp.Key, Value = kvp.Value });
            }
        }

        public Dictionary<string, string> ToDictionary()
        {
            var dict = new Dictionary<string, string>();
            foreach (var entry in Entries)
            {
                if (!dict.ContainsKey(entry.Key))
                    dict.Add(entry.Key, entry.Value);
            }
            return dict;
        }
    }

    /// <summary>
    /// Initialize caching paths
    /// </summary>
    private static void InitializeCaching()
    {
        cachePath = Path.Combine(BepInEx.Paths.GameRootPath, "PKCore", "Cache");
        // Changing extension to .xml
        manifestPath = Path.Combine(cachePath, "texture_manifest.xml");

        if (!Directory.Exists(cachePath))
        {
            Directory.CreateDirectory(cachePath);
        }
    }

    /// <summary>
    /// Compute hash of texture-related config settings
    /// If any of these change, the manifest should be invalidated
    /// </summary>
    private static string ComputeConfigHash()
    {
        // Manifest version - increment to force a cache rebuild when indexing logic changes
        string manifestVersion = "2.2"; 

        // Combine all texture-impacting config values into a string
        string configString = string.Join("|", 
            manifestVersion,
            Plugin.Config.SavePointColor.Value,
            Plugin.Config.LoadLauncherUITextures.Value,
            Plugin.Config.EnableProjectKyaroSprites.Value,
            Plugin.Config.MinimalUI.Value,
            Plugin.Config.ForceControllerPrompts.Value,
            Plugin.Config.ControllerPromptType.Value,
            Plugin.Config.MercFortFence.Value,
            Plugin.Config.ClassicSaveWindow.Value,
            Plugin.Config.TirRunTexture.Value
        );

        // Use deterministic hash instead of string.GetHashCode() which is non-deterministic in .NET 6+
        return GetDeterministicHashCode(configString).ToString();
    }

    /// <summary>
    /// Helper to get a deterministic hash code for a string
    /// </summary>
    private static int GetDeterministicHashCode(string str)
    {
        unchecked
        {
            int hash1 = (5381 << 16) + 5381;
            int hash2 = hash1;

            for (int i = 0; i < str.Length; i += 2)
            {
                hash1 = ((hash1 << 5) + hash1) ^ str[i];
                if (i == str.Length - 1)
                    break;
                hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
            }

            return hash1 + (hash2 * 1566083941);
        }
    }

    /// <summary>
    /// Try to load texture index from manifest (XML)
    /// </summary>
    private static bool TryLoadManifestIndex()
    {
        if (!File.Exists(manifestPath))
            return false;

        try
        {
            XmlSerializer serializer = new XmlSerializer(typeof(TextureManifest));
            using (FileStream stream = new FileStream(manifestPath, FileMode.Open))
            {
                TextureManifest manifest = (TextureManifest)serializer.Deserialize(stream);

                // Check if config has changed since manifest was created
                string currentConfigHash = ComputeConfigHash();

                if (manifest != null &&
                    manifest.ConfigHash == currentConfigHash &&
                    manifest.Entries != null &&
                    manifest.Entries.Count > 0)
                {
                    texturePathIndex = manifest.ToDictionary();
                    if (Plugin.Config.DetailedTextureLog.Value)
                    {
                        Plugin.Log.LogInfo($"✓ Loaded texture index from cache ({texturePathIndex.Count} textures, {manifest.FileCount} files)");
                    }
                    return true;
                }
                else if (manifest != null && manifest.ConfigHash != currentConfigHash)
                {
                    Plugin.Log.LogInfo("Config changed - rebuilding texture index");
                }
                else if (manifest == null || manifest.Entries == null || manifest.Entries.Count == 0)
                {
                    Plugin.Log.LogInfo("Invalid or empty manifest - rebuilding texture index");
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Failed to load manifest: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// Save texture index to manifest (XML)
    /// </summary>
    private static void SaveManifestIndex()
    {
        try
        {
            TextureManifest manifest = new TextureManifest
            {
                FileCount = texturePathIndex.Count,
                ConfigHash = ComputeConfigHash()
            };
            manifest.FromDictionary(texturePathIndex);

            XmlSerializer serializer = new XmlSerializer(typeof(TextureManifest));
            using (FileStream stream = new FileStream(manifestPath, FileMode.Create))
            {
                serializer.Serialize(stream, manifest);
            }
            if (Plugin.Config.DetailedTextureLog.Value)
            {
                Plugin.Log.LogInfo($"✓ Saved texture cache manifest ({manifest.FileCount} files)");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Failed to save manifest: {ex.Message}");
        }
    }
}
