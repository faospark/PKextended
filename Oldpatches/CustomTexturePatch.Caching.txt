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
        public long LastModified;
        public string ConfigHash; // Hash of texture-related config settings
        public List<ManifestEntry> Entries = new List<ManifestEntry>();
        
        public void FromDictionary(Dictionary<string, string> dict)
        {
            Entries.Clear();
            foreach(var kvp in dict)
            {
                Entries.Add(new ManifestEntry { Key = kvp.Key, Value = kvp.Value });
            }
        }
        
        public Dictionary<string, string> ToDictionary()
        {
            var dict = new Dictionary<string, string>();
            foreach(var entry in Entries)
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
        // Combine all texture-related config values into a string
        string configString = $"{Plugin.Config.SavePointColor.Value}";
        
        // Simple hash (GetHashCode is sufficient for cache invalidation)
        return configString.GetHashCode().ToString();
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
            // Check if textures directory has been modified since manifest
            long currentModified = Directory.GetLastWriteTime(customTexturesPath).Ticks;
            
            XmlSerializer serializer = new XmlSerializer(typeof(TextureManifest));
            using (FileStream stream = new FileStream(manifestPath, FileMode.Open))
            {
                TextureManifest manifest = (TextureManifest)serializer.Deserialize(stream);

                // Check if config has changed since manifest was created
                string currentConfigHash = ComputeConfigHash();
                
                if (manifest != null && 
                    manifest.LastModified == currentModified && 
                    manifest.ConfigHash == currentConfigHash &&
                    manifest.Entries != null && 
                    manifest.Entries.Count > 0)
                {
                    texturePathIndex = manifest.ToDictionary();
                    Plugin.Log.LogInfo($"Loaded texture index from manifest ({texturePathIndex.Count} textures)");
                    return true;
                }
                else if (manifest != null && manifest.ConfigHash != currentConfigHash)
                {
                    Plugin.Log.LogInfo("Config changed - rebuilding texture index");
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
                LastModified = Directory.GetLastWriteTime(customTexturesPath).Ticks,
                ConfigHash = ComputeConfigHash()
            };
            manifest.FromDictionary(texturePathIndex);

            XmlSerializer serializer = new XmlSerializer(typeof(TextureManifest));
            using (FileStream stream = new FileStream(manifestPath, FileMode.Create))
            {
                serializer.Serialize(stream, manifest);
            }
            Plugin.Log.LogInfo("Saved texture index validation manifest");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Failed to save manifest: {ex.Message}");
        }
    }
}
