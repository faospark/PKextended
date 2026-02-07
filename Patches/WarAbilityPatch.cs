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
            var legacyConfigPath = Path.Combine(pkCoreDir, "war_abilities.json");
            var newConfigPath = Path.Combine(configDir, "war_abilities.json");

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
                    Logger.LogInfo($"[WarAbilityPatch] Migrating war_abilities.json to {configDir}...");
                    File.Move(legacyConfigPath, newConfigPath);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to migrate war_abilities.json: {ex.Message}");
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
                
                // Apply custom config
                if (CharacterConfigs.TryGetValue(nameIndex, out var config))
                {
                    // Abilities
                    if (config.Abilities != null && config.Abilities.Count > 0)
                    {
                        var abilities = WarAbilityConfigLoader.ParseAbilities(config.Abilities);
                        ApplyAbilities(character, abilities);
                    }
                    
                    // Stats
                    if (config.Attack.HasValue) character.attack = config.Attack.Value;
                    if (config.Defense.HasValue) character.defense = config.Defense.Value;
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
    }
}
