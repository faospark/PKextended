using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using BepInEx.Logging;
using PKCore.IL2CPP;

namespace PKCore.Config
{
    /// <summary>
    /// Configuration structure for war abilities
    /// </summary>
    public class WarAbilityConfig
    {
        [JsonPropertyName("globalAbilities")]
        public List<string> GlobalAbilities { get; set; } = new();

        [JsonPropertyName("characterAbilities")]
        public Dictionary<string, CharacterAbilityConfig> CharacterAbilities { get; set; } = new();
    }

    public class CharacterAbilityConfig
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("abilities")]
        public List<string> Abilities { get; set; } = new();
        
        [JsonPropertyName("attack")]
        public byte? Attack { get; set; } = null;
        
        [JsonPropertyName("defense")]
        public byte? Defense { get; set; } = null;
    }

    /// <summary>
    /// Loads and parses war ability configuration from JSON
    /// </summary>
    public static class WarAbilityConfigLoader
    {
        private static readonly string ConfigFileName = "war_abilities.json";
        private static ManualLogSource Logger;

        public static WarAbilityConfig LoadConfig(string configPath, ManualLogSource logger)
        {
            Logger = logger;
            var fullPath = Path.Combine(configPath, ConfigFileName);

            try
            {
                if (!File.Exists(fullPath))
                {
                    Logger.LogWarning($"War abilities config not found at: {fullPath}");
                    Logger.LogInfo("Creating default configuration file...");
                    CreateDefaultConfig(fullPath);
                    return new WarAbilityConfig();
                }

                Logger.LogInfo($"Loading war abilities from: {fullPath}");
                var json = File.ReadAllText(fullPath);
                
                // Parse with comments support
                var options = new JsonSerializerOptions
                {
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                    PropertyNameCaseInsensitive = true
                };

                var config = JsonSerializer.Deserialize<WarAbilityConfig>(json, options);
                
                if (config == null)
                {
                    Logger.LogWarning("Failed to parse config, using defaults");
                    return new WarAbilityConfig();
                }

                Logger.LogInfo($"Loaded {config.CharacterAbilities.Count} character configurations");
                Logger.LogInfo($"Global abilities: {config.GlobalAbilities.Count}");

                return config;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error loading war abilities config: {ex.Message}");
                Logger.LogError($"Stack trace: {ex.StackTrace}");
                return new WarAbilityConfig();
            }
        }

        private static void CreateDefaultConfig(string path)
        {
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var defaultConfig = new WarAbilityConfig();
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(defaultConfig, options);
                File.WriteAllText(path, json);
                
                Logger.LogInfo($"Created default config at: {path}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to create default config: {ex}");
            }
        }

        /// <summary>
        /// Converts ability name string to enum
        /// </summary>
        public static war_data_h.tagSPECIAL_ABILITY ParseAbility(string abilityName)
        {
            if (string.IsNullOrWhiteSpace(abilityName))
                return war_data_h.tagSPECIAL_ABILITY.SP_NONE;

            try
            {
                if (Enum.TryParse<war_data_h.tagSPECIAL_ABILITY>(abilityName, true, out var ability))
                {
                    return ability;
                }

                Logger?.LogWarning($"Unknown ability: {abilityName}");
                return war_data_h.tagSPECIAL_ABILITY.SP_NONE;
            }
            catch
            {
                return war_data_h.tagSPECIAL_ABILITY.SP_NONE;
            }
        }

        /// <summary>
        /// Converts list of ability names to enum array
        /// </summary>
        public static war_data_h.tagSPECIAL_ABILITY[] ParseAbilities(List<string> abilityNames)
        {
            if (abilityNames == null || abilityNames.Count == 0)
                return Array.Empty<war_data_h.tagSPECIAL_ABILITY>();

            var abilities = new List<war_data_h.tagSPECIAL_ABILITY>();
            foreach (var name in abilityNames)
            {
                var ability = ParseAbility(name);
                if (ability != war_data_h.tagSPECIAL_ABILITY.SP_NONE)
                {
                    abilities.Add(ability);
                }
            }

            return abilities.ToArray();
        }
    }
}
