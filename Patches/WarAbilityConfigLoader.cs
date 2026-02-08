using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using BepInEx.Logging;
using PKCore.IL2CPP;

namespace PKCore.Patches
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

        [JsonPropertyName("bonusAttack")]
        public byte? BonusAttack { get; set; } = null;

        [JsonPropertyName("bonusDefense")]
        public byte? BonusDefense { get; set; } = null;
    }

    /// <summary>
    /// Loads and parses war ability configuration from JSON.
    /// 
    /// JSON Format Example:
    /// {
    ///   "globalAbilities": ["SP_MOUNT", "SP_AIMING"],
    ///   "characterAbilities": {
    ///     "3347": {
    ///       "name": "Riou",
    ///       "abilities": ["SP_FLAME_SPEAR", "SP_CHARGE"],
    ///       "attack": 10,
    ///       "defense": 11,
    ///       "bonusAttack": 2,      // Assist/bonus attack from subunits
    ///       "bonusDefense": 1      // Assist/bonus defense from subunits
    ///     }
    ///   }
    /// }
    /// </summary>
    public static class WarAbilityConfigLoader
    {
        private static readonly string ConfigFileName = "S2WarAbilities.json";
        private static ManualLogSource Logger;

        // Abilities that are known to crash or malfunction - blocklisted from config
        private static readonly HashSet<string> BlocklistedAbilities = new(StringComparer.OrdinalIgnoreCase)
        {
            "SP_KIN_SLAYER",    // Crashes game if used
            "SP_MAGIC_WIND2"    // Non-functional/broken ability
        };

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

                Logger.LogInfo($"[WarAbilityConfigLoader] Loading war abilities from: {fullPath}");
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

                Logger.LogInfo($"[WarAbilityConfigLoader] Loaded {config.CharacterAbilities.Count} character configurations");
                Logger.LogInfo($"[WarAbilityConfigLoader] Global abilities: {config.GlobalAbilities.Count}");

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
                
                Logger.LogInfo($"[WarAbilityConfigLoader] Created default config at: {path}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to create default config: {ex}");
            }
        }

        /// <summary>
        /// Converts ability name string to enum
        /// Blocklists dangerous abilities (SP_KIN_SLAYER, SP_MAGIC_WIND2) that crash or don't work
        /// </summary>
        public static war_data_h.tagSPECIAL_ABILITY ParseAbility(string abilityName)
        {
            if (string.IsNullOrWhiteSpace(abilityName))
                return war_data_h.tagSPECIAL_ABILITY.SP_NONE;

            try
            {
                // Check blocklist first
                if (BlocklistedAbilities.Contains(abilityName))
                {
                    Logger?.LogWarning($"⚠️ Ability '{abilityName}' is blocklisted (crashes or doesn't work). Using SP_NONE instead.");
                    return war_data_h.tagSPECIAL_ABILITY.SP_NONE;
                }

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
