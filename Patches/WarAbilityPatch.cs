using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

using PKCore.IL2CPP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine.SceneManagement;

namespace PKCore.Patches
{
    /// <summary>
    /// Patches war battle character abilities.
    /// Only active in Suikoden 2 (GSD2).
    /// </summary>
    public class WarAbilityPatch
    {
        private static ManualLogSource Logger;
        private static bool isPatched = false;
        private static WarAbilityConfig Config;
        private static bool hasModifiedThisBattle = false;

        // Configuration loaded from JSON file
        private static Dictionary<int, CharacterAbilityConfig> CharacterConfigs = new();
        private static war_data_h.tagSPECIAL_ABILITY[] GlobalAbilities = Array.Empty<war_data_h.tagSPECIAL_ABILITY>();

        public static void Initialize(ManualLogSource logger)
        {
            Logger = logger;
            
            if (isPatched) return;

            try
            {
                LoadConfiguration();

                // Register scene listener to apply patches when GSD2 loads
                SceneManager.sceneLoaded += (System.Action<Scene, LoadSceneMode>)OnSceneLoaded;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[WarAbilityPatch] Failed to initialize: {ex}");
            }
        }

        private static void LoadConfiguration()
        {
            // Load configuration from JSON in the PKCore/Config directory
            var pkCoreDir = Path.Combine(BepInEx.Paths.GameRootPath, "PKCore");
            var configDir = Path.Combine(pkCoreDir, "Config");
            var legacyConfigPath = Path.Combine(pkCoreDir, "S2WarAbilities.json");
            var newConfigPath = Path.Combine(configDir, "S2WarAbilities.json");

            // Create Config directory if it doesn't exist
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            // Migrate legacy config if needed
            if (File.Exists(legacyConfigPath) && !File.Exists(newConfigPath))
            {
                try 
                {
                    Logger.LogInfo($"[WarAbilityPatch] Migrating S2WarAbilities.json to {configDir}...");
                    File.Move(legacyConfigPath, newConfigPath);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to migrate S2WarAbilities.json: {ex.Message}");
                    // Fallback to legacy path
                    Config = WarAbilityConfigLoader.LoadConfig(pkCoreDir, Logger);
                    return;
                }
            }

            Config = WarAbilityConfigLoader.LoadConfig(configDir, Logger);
            
            // Parse global abilities
            GlobalAbilities = WarAbilityConfigLoader.ParseAbilities(Config.GlobalAbilities);
            
            // Parse character-specific abilities
            foreach (var kvp in Config.CharacterAbilities)
            {
                if (int.TryParse(kvp.Key, out int characterIndex))
                {
                    var abilities = WarAbilityConfigLoader.ParseAbilities(kvp.Value.Abilities);
                    if (abilities.Length > 0 || kvp.Value.Attack.HasValue || kvp.Value.Defense.HasValue)
                    {
                        CharacterConfigs[characterIndex] = kvp.Value;
                    }
                }
            }
        }
        
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Debug log to confirm event firing
            // Logger.LogInfo($"[WarAbilityPatch] OnSceneLoaded: {scene.name}");

            // Only apply patches when GSD2 scene loads
            // Check scene name directly to avoid race conditions with GameDetection update loop
            if (!scene.name.Contains("GSD2"))
                return;
                
            if (isPatched)
                return;
     
            try
            {
                var harmony = new Harmony("faospark.pkcore.warability");
                
                // Manual patching to avoid HarmonyX assembly scanning warnings
                
                // 1. Patch WarChapter.Update
                var warChapterType = FindTypeInAssemblies("WarChapter", new[] { "GSD2", "GSD1", "Assembly-CSharp" });
                
                if (warChapterType != null)
                {
                    var originalUpdate = AccessTools.Method(warChapterType, "Update");
                    var postfixUpdate = AccessTools.Method(typeof(WarAbilityPatch), nameof(AfterWarChapterUpdate));
                    
                    if (originalUpdate != null && postfixUpdate != null)
                    {
                        harmony.Patch(originalUpdate, postfix: new HarmonyMethod(postfixUpdate));
                    }
                    else
                    {
                        Logger.LogWarning("[WarAbilityPatch] Could not find WarChapter.Update or patch method");
                    }
                }
                else
                {
                    Logger.LogWarning("[WarAbilityPatch] Could not find type WarChapter");
                }

                // 2. Patch w_chara.charaInit
                var charaInitMethod = AccessTools.Method(typeof(w_chara), nameof(w_chara.charaInit));
                var postfixCharaInit = AccessTools.Method(typeof(WarAbilityPatch), nameof(AfterCharaInit));

                if (charaInitMethod != null && postfixCharaInit != null)
                {
                    harmony.Patch(charaInitMethod, postfix: new HarmonyMethod(postfixCharaInit));
                }
                else
                {
                    Logger.LogWarning("[WarAbilityPatch] Could not find w_chara.charaInit or patch method");
                }
                
                isPatched = true;
                if (Plugin.Config.DetailedLogs.Value)
                    Logger.LogInfo("[WarAbilityPatch] Applied successfully for GSD2");
            }
            catch (Exception ex)
            {
                Logger.LogError($"[WarAbilityPatch] Failed to apply patches: {ex}");
            }
        }

        /// <summary>
        /// Patch WarChapter.Update - called every frame during war battle
        /// </summary>
        public static void AfterWarChapterUpdate(int __result)
        {
            // If result != 0, battle is ending/ended
            if (__result != 0)
            {
                hasModifiedThisBattle = false;
                return;
            }
            
            // Battle is active, modify abilities on first frame
            if (!hasModifiedThisBattle)
            {
                hasModifiedThisBattle = true;
                ModifyAllWarCharacters();
            }
        }
        
        /// <summary>
        /// Harmony postfix for w_chara.charaInit - called when war characters are initialized
        /// </summary>
        public static void AfterCharaInit()
        {
             ModifyAllWarCharacters();
        }

        private static void ModifyAllWarCharacters()
        {
            if (Config == null) return;
            if (!GameDetection.IsGSD2()) return; // Extra safety check

            try
            {
                Logger.LogInfo($"[WarAbilityPatch] Modifying all war characters...");
                
                // Log battle status flags
                LogBattleStatusFlags();
                
                // Iterate through all 108 war characters
                for (int i = 0; i < 108; i++)
                {
                    try
                    {
                        var chara = w_chara.chara(i);
                        if (chara != null)
                        {
                            ModifyCharacterAbilities(i, chara);
                        }
                    }
                    catch
                    {
                        // Ignore individual character access errors (e.g. index out of range)
                        continue;
                    }
                }
                
                Logger.LogInfo($"[WarAbilityPatch] Finished modifying all characters");
            }
            catch (Exception ex)
            {
                Logger.LogError($"[WarAbilityPatch] Error modifying characters: {ex.Message}");
            }
        }

        private static void ModifyCharacterAbilities(int charIndex, WAR_CHARA_TYPE character)
        {
            if (character == null) return;

            try
            {
                int nameIndex = character.name;
                
                // Log character state with unit type priority
                var currentAbilities = character.nouryoku;
                var abilityString = FormatAbilities(currentAbilities);
                
                // Get unit type info
                string unitType = GetUnitTypeInfo(character);
                
                // Log character stats including bonuses
                Logger.LogInfo($"[{unitType}] Character {nameIndex} - " +
                    $"ATK: {character.attack} (Bonus: {character.assist_attack}), " +
                    $"DEF: {character.defense} (Bonus: {character.assist_defense}) - " +
                    $"Abilities: {abilityString}");
                
                // Apply custom config
                if (CharacterConfigs.TryGetValue(nameIndex, out var config))
                {
                    Logger.LogInfo($"Applying custom configuration to character {nameIndex}");
                    
                    // Abilities
                    if (config.Abilities != null && config.Abilities.Count > 0)
                    {
                        var abilities = WarAbilityConfigLoader.ParseAbilities(config.Abilities);
                        ApplyAbilities(character, abilities);
                    }
                    
                    // Base Stats
                    if (config.Attack.HasValue)
                    {
                        character.attack = config.Attack.Value;
                        Logger.LogInfo($"  ➜ Updated attack to {config.Attack.Value}");
                    }
                    if (config.Defense.HasValue)
                    {
                        character.defense = config.Defense.Value;
                        Logger.LogInfo($"  ➜ Updated defense to {config.Defense.Value}");
                    }

                    // Bonus Stats (from unit composition)
                    if (config.BonusAttack.HasValue)
                    {
                        character.assist_attack = config.BonusAttack.Value;
                        Logger.LogInfo($"  ➜ Updated assist_attack bonus to {config.BonusAttack.Value}");
                    }
                    if (config.BonusDefense.HasValue)
                    {
                        character.assist_defense = config.BonusDefense.Value;
                        Logger.LogInfo($"  ➜ Updated assist_defense bonus to {config.BonusDefense.Value}");
                    }
                }
                // Or apply global abilities
                else if (GlobalAbilities.Length > 0)
                {
                    AddAbilities(character, GlobalAbilities);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[WarAbilityPatch] Error modifying char {charIndex}: {ex.Message}");
            }
        }

        private static void ApplyAbilities(WAR_CHARA_TYPE character, war_data_h.tagSPECIAL_ABILITY[] abilities)
        {
            var currentAbilities = character.nouryoku;
            int maxSlots = currentAbilities?.Length ?? abilities.Length;

            var newAbilityArray = new Il2CppStructArray<war_data_h.tagSPECIAL_ABILITY>(maxSlots);
            
            for (int i = 0; i < maxSlots; i++)
            {
                newAbilityArray[i] = (i < abilities.Length) ? abilities[i] : war_data_h.tagSPECIAL_ABILITY.SP_NONE;
            }

            character.nouryoku = newAbilityArray;
            
            // Initialize usages
            var abilityList = abilities.Where(a => a != war_data_h.tagSPECIAL_ABILITY.SP_NONE).ToList();
            InitializeAbilityUsageCounts(character, abilityList);
            
            Logger.LogInfo($"✓ Initialized usage counts (9 uses each) for {abilityList.Count} abilities on character {character.name}");
            Logger.LogInfo($"Replaced abilities: {FormatAbilities(newAbilityArray)}");
        }

        private static void AddAbilities(WAR_CHARA_TYPE character, war_data_h.tagSPECIAL_ABILITY[] abilitiesToAdd)
        {
            var currentAbilities = character.nouryoku;
            if (currentAbilities == null) return;

            var existingAbilities = currentAbilities.ToArray()
                .Where(a => a != war_data_h.tagSPECIAL_ABILITY.SP_NONE)
                .ToList();

            foreach (var ability in abilitiesToAdd)
            {
                if (!existingAbilities.Contains(ability))
                    existingAbilities.Add(ability);
            }

            var newAbilityArray = new Il2CppStructArray<war_data_h.tagSPECIAL_ABILITY>(
                Math.Max(currentAbilities.Length, existingAbilities.Count));
            
            for (int i = 0; i < newAbilityArray.Length; i++)
            {
                newAbilityArray[i] = (i < existingAbilities.Count) ? existingAbilities[i] : war_data_h.tagSPECIAL_ABILITY.SP_NONE;
            }

            character.nouryoku = newAbilityArray;
            InitializeAbilityUsageCounts(character, existingAbilities);
        }

        private static void InitializeAbilityUsageCounts(WAR_CHARA_TYPE character, List<war_data_h.tagSPECIAL_ABILITY> abilities)
        {
            if (character == null) return;
            
            const byte DEFAULT_USES = 9;
            var kaisuArray = character.kaisu;
            var kaisuMaxArray = character.kaisu_max;
            
            if (kaisuArray == null || kaisuMaxArray == null) return;
            
            for (int abilitySlot = 0; abilitySlot < Math.Min(abilities.Count, 3); abilitySlot++)
            {
                kaisuArray[abilitySlot] = DEFAULT_USES;
                kaisuMaxArray[abilitySlot] = DEFAULT_USES;
            }
        }

        private static Type FindTypeInAssemblies(string typeName, string[] assemblyNames)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var name = assembly.GetName().Name;
                if (assemblyNames.Contains(name))
                {
                    var type = assembly.GetType(typeName);
                    if (type != null) return type;
                }
            }
            return null;
        }

        private static string FormatAbilities(Il2CppStructArray<war_data_h.tagSPECIAL_ABILITY> abilities)
        {
            if (abilities == null || abilities.Length == 0)
                return "SP_NONE";
            
            var formatted = abilities.Take(3).Select(a => a.ToString()).ToList();
            return string.Join(", ", formatted);
        }

        private static string GetUnitTypeInfo(WAR_CHARA_TYPE character)
        {
            try
            {
                if (character == null) return "UNKNOWN_TYPE";

                // Detect unit type from character properties
                // Try to identify from force_type or attack/defense patterns
                byte forceType = character.force_type;

                // Map units to types based on known character indices and patterns
                // This is a heuristic approach - can be refined with more data
                int charName = character.name;

                // Check against known unit type constants from war_data_h
                byte infantryVal = war_data_h.INFANTRY;
                byte archerVal = war_data_h.ARCHER;
                byte magiciansVal = war_data_h.MAGICIANS;
                byte magicThunderVal = war_data_h.MAGIC_THUNDER;
                byte magicFireVal = war_data_h.MAGIC_FIRE;
                byte magicWindVal = war_data_h.MAGIC_WIND;

                // Log unit type constants for reference (debug)
                Logger.LogDebug($"Unit Type Constants - INFANTRY: {infantryVal}, ARCHER: {archerVal}, MAGICIANS: {magiciansVal}, " +
                    $"MAGIC_THUNDER: {magicThunderVal}, MAGIC_FIRE: {magicFireVal}, MAGIC_WIND: {magicWindVal}");

                // For now, return generic type with force affiliation
                // In future, this can be extended to detect actual unit types
                string typeStr = forceType switch
                {
                    0 => "FORCE_0", // Likely main force
                    1 => "FORCE_1", // Likely enemy force
                    _ => $"FORCE_{forceType}"
                };

                return typeStr;
            }
            catch (Exception ex)
            {
                Logger.LogDebug($"Error detecting unit type: {ex.Message}");
                return "WAR_UNIT";
            }
        }

        private static void LogBattleStatusFlags()
        {
            try
            {
                // Log available battle status flags for debugging
                Logger.LogDebug($"[WarAbilityPatch] Battle Status - DEAD_POSSIBLE: {war_data_h.DEAD_POSSIBLE}, DEAD_UNPOSSIBLE: {war_data_h.DEAD_UNPOSSIBLE}, ANNIHILATION_UNPOSSIBLE: {war_data_h.ANNIHILATION_UNPOSSIBLE}");
                Logger.LogDebug($"[WarAbilityPatch] Unit Flags - WAIT: {war_data_h.UNIT_FLAG_WAIT}, ON_STAGE: {war_data_h.UNIT_FLAG_ON_STAGE}, DESTROY: {war_data_h.UNIT_FLAG_DESTROY}");
            }
            catch (Exception ex)
            {
                Logger.LogDebug($"[WarAbilityPatch] Could not log battle status flags: {ex.Message}");
            }
        }
    }
}
