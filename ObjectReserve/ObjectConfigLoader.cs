using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using BepInEx;
using PKCore.Models;

namespace PKCore.Utils;

public static class ObjectConfigLoader
{
    private static CustomObjectsConfig _config;
    
    // Path: GameRoot/PKCore/CustomObjects
    private static string ConfigDir => Path.Combine(Paths.GameRootPath, "PKCore", "CustomObjects");
    private static string ConfigPath => Path.Combine(ConfigDir, "ExistingMapObjects.json");
    
    public static void Initialize()
    {
        try
        {
            // Ensure directory exists
            if (!Directory.Exists(ConfigDir))
            {
                Directory.CreateDirectory(ConfigDir);
            }
            
            LoadConfig();
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[ObjectConfig] Error initializing: {ex.Message}");
        }
    }
    
    public static void LoadConfig()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                // Create default config if missing
                CreateDefaultConfig();
                return;
            }
            
            string json = File.ReadAllText(ConfigPath);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
            
            _config = JsonSerializer.Deserialize<CustomObjectsConfig>(json, options);
            
            if (_config != null)
            {
                int mapCount = _config.Maps?.Count ?? 0;
                Plugin.Log.LogInfo($"[ObjectConfig] Loaded configuration for {mapCount} maps");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[ObjectConfig] Error loading config: {ex.Message}");
            _config = new CustomObjectsConfig(); // Avoid null reference
        }
    }
    
    private static void CreateDefaultConfig()
    {
        try
        {
            var defaultConfig = new CustomObjectsConfig
            {
                Maps = new Dictionary<string, MapObjectsConfig>
                {
                    ["vk07_01"] = new MapObjectsConfig
                    {
                        Objects = new List<DiscoveredObject>
                        {
                            new DiscoveredObject
                            {
                                Name = "custom_test_object",
                                Texture = "custom_object_test", 
                                Position = new Vector3Config { X = 0, Y = 0, Z = 0 },
                                Scale = new Vector3Config { X = 5, Y = 5, Z = 1 }, // Matches previous hardcoded test
                                SortingOrder = 31,
                                HasCollision = false
                            }
                        }
                    }
                }
            };
            
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            
            string json = JsonSerializer.Serialize(defaultConfig, options);
            File.WriteAllText(ConfigPath, json);
            Plugin.Log.LogInfo($"[ObjectConfig] Created default config at {ConfigPath}");
            
            _config = defaultConfig;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[ObjectConfig] Error creating default config: {ex.Message}");
        }
    }
    
    public static List<DiscoveredObject> GetObjectsForMap(string mapId)
    {
        if (_config == null || _config.Maps == null)
            return new List<DiscoveredObject>();
            
        if (_config.Maps.TryGetValue(mapId, out var mapConfig))
        {
            return mapConfig.Objects ?? new List<DiscoveredObject>();
        }
        
        return new List<DiscoveredObject>();
    }
}
