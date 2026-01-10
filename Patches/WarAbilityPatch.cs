using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using PKCore.Config;
using PKCore.IL2CPP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;

namespace PKCore.Patches
{
    /// <summary>
    /// Patches war battle character abilities
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
            
            if (isPatched)
            {
                Logger.LogWarning("WarAbilityPatch already applied!");
                return;
            }

            try
            {
                // Load configuration from JSON in the PKCore directory
                var pkCoreDir = Path.Combine(BepInEx.Paths.GameRootPath, "PKCore");
                Config = WarAbilityConfigLoader.LoadConfig(pkCoreDir, Logger);
                
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
                            Logger.LogInfo($"Configured character {characterIndex} ({kvp.Value.Name}): {abilities.Length} abilities" + 
                                (kvp.Value.Attack.HasValue ? $", ATK={kvp.Value.Attack.Value}" : "") +
                                (kvp.Value.Defense.HasValue ? $", DEF={kvp.Value.Defense.Value}" : ""));
                        }
                    }
                }
                
                Logger.LogInfo("War Ability Modification initialized - will activate when GSD2 scene loads");
                
                // Register scene listener to apply patches when GSD2 loads
                SceneManager.sceneLoaded += (System.Action<Scene, LoadSceneMode>)OnSceneLoaded;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to initialize War Ability Patch: {ex}");
            }
        }
        
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Only apply patches when GSD2 scene loads
            if (!scene.name.StartsWith("GSD2"))
                return;
                
            if (isPatched)
                return;
                
            try
            {
                Logger.LogInfo("=== War Ability Patch Initializing ===");
                Logger.LogInfo($"Ability overrides configured: {Config?.CharacterAbilities?.Count ?? 0}");
                Logger.LogInfo($"Global abilities enabled: {(Config?.GlobalAbilities?.Count > 0)}");
                
                var harmony = new Harmony("faospark.pkcore.warability");
                harmony.PatchAll(typeof(WarAbilityPatch));
                isPatched = true;
                Logger.LogInfo("War Ability Patch applied");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to apply War Ability Patch: {ex}");
            }
        }

        /// <summary>
        /// Modifies a war character's abilities
        /// </summary>
        private static void ModifyCharacterAbilities(int charIndex, WAR_CHARA_TYPE character)
        {
            try
            {
                if (character == null)
                    return;

                var nameIndex = character.name;
                var currentAbilities = character.nouryoku;
                
                if (currentAbilities == null)
                {
                    Logger.LogWarning($"Character {nameIndex} has null abilities array");
                    return;
                }

                Logger.LogInfo($"Character {nameIndex} - ATK: {character.attack}, DEF: {character.defense}, Abilities: {string.Join(", ", currentAbilities.ToArray())}");

                // Check if we have specific config for this character
                if (CharacterConfigs.TryGetValue(nameIndex, out var config))
                {
                    Logger.LogInfo($"Applying custom configuration to character {nameIndex}");
                    
                    // Apply abilities if configured
                    if (config.Abilities != null && config.Abilities.Count > 0)
                    {
                        var abilities = WarAbilityConfigLoader.ParseAbilities(config.Abilities);
                        ApplyAbilities(character, abilities);
                    }
                    
                    // Apply stat modifications if configured
                    if (config.Attack.HasValue)
                    {
                        byte oldAtk = character.attack;
                        character.attack = config.Attack.Value;
                        Logger.LogInfo($"  Character {nameIndex}: ATK {oldAtk} → {config.Attack.Value}");
                    }
                    
                    if (config.Defense.HasValue)
                    {
                        byte oldDef = character.defense;
                        character.defense = config.Defense.Value;
                        Logger.LogInfo($"  Character {nameIndex}: DEF {oldDef} → {config.Defense.Value}");
                    }
                }
                // Otherwise, apply global abilities if any
                else if (GlobalAbilities.Length > 0)
                {
                    Logger.LogInfo($"Applying global abilities to character {nameIndex}");
                    AddAbilities(character, GlobalAbilities);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error modifying character abilities: {ex}");
            }
        }

        /// <summary>
        /// Replaces all abilities with new ones
        /// </summary>
        private static void ApplyAbilities(WAR_CHARA_TYPE character, war_data_h.tagSPECIAL_ABILITY[] abilities)
        {
            try
            {
                var currentAbilities = character.nouryoku;
                int maxSlots = currentAbilities?.Length ?? abilities.Length;

                // Create new ability array
                var newAbilityArray = new Il2CppStructArray<war_data_h.tagSPECIAL_ABILITY>(maxSlots);
                
                for (int i = 0; i < maxSlots; i++)
                {
                    if (i < abilities.Length)
                        newAbilityArray[i] = abilities[i];
                    else
                        newAbilityArray[i] = war_data_h.tagSPECIAL_ABILITY.SP_NONE;
                }

                character.nouryoku = newAbilityArray;
                
                // Initialize usage counts for the replaced abilities
                var abilityList = abilities.Where(a => a != war_data_h.tagSPECIAL_ABILITY.SP_NONE).ToList();
                InitializeAbilityUsageCounts(character, abilityList);
                
                Logger.LogInfo($"Replaced abilities: {string.Join(", ", abilities)}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error applying abilities: {ex}");
            }
        }

        /// <summary>
        /// Adds abilities to existing ones (doesn't replace)
        /// </summary>
        private static void AddAbilities(WAR_CHARA_TYPE character, war_data_h.tagSPECIAL_ABILITY[] abilitiesToAdd)
        {
            try
            {
                var currentAbilities = character.nouryoku;
                if (currentAbilities == null)
                    return;

                var existingAbilities = currentAbilities.ToArray()
                    .Where(a => a != war_data_h.tagSPECIAL_ABILITY.SP_NONE)
                    .ToList();

                // Add new abilities that aren't already present
                foreach (var ability in abilitiesToAdd)
                {
                    if (!existingAbilities.Contains(ability))
                        existingAbilities.Add(ability);
                }

                // Create new array with combined abilities
                var newAbilityArray = new Il2CppStructArray<war_data_h.tagSPECIAL_ABILITY>(
                    Math.Max(currentAbilities.Length, existingAbilities.Count));
                
                for (int i = 0; i < newAbilityArray.Length; i++)
                {
                    if (i < existingAbilities.Count)
                        newAbilityArray[i] = existingAbilities[i];
                    else
                        newAbilityArray[i] = war_data_h.tagSPECIAL_ABILITY.SP_NONE;
                }

                character.nouryoku = newAbilityArray;
                
                // Initialize usage counts for the abilities
                // The game stores usage counts separately - we need to initialize them
                // Default to 3 uses per ability (can be configured later)
                InitializeAbilityUsageCounts(character, existingAbilities);
                
                Logger.LogInfo($"Added abilities. New total: {string.Join(", ", existingAbilities)}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error adding abilities: {ex}");
            }
        }

        private static void InitializeAbilityUsageCounts(WAR_CHARA_TYPE character, List<war_data_h.tagSPECIAL_ABILITY> abilities)
        {
            try
            {
                if (character == null)
                    return;
                
                // Default usage count - most abilities can be used 9 times per battle
                const byte DEFAULT_USES = 9;
                
                // Access the usage count arrays directly
                var kaisuArray = character.kaisu;  // Current usage counts
                var kaisuMaxArray = character.kaisu_max;  // Max usage counts
                
                if (kaisuArray == null || kaisuMaxArray == null)
                {
                    Logger.LogWarning($"Character {character.name} has null usage count arrays");
                    return;
                }
                
                // Set usage counts for each ability slot (up to 3 abilities)
                for (int abilitySlot = 0; abilitySlot < Math.Min(abilities.Count, 3); abilitySlot++)
                {
                    // Set both current and max usage counts
                    kaisuArray[abilitySlot] = DEFAULT_USES;
                    kaisuMaxArray[abilitySlot] = DEFAULT_USES;
                }
                
                Logger.LogInfo($"✓ Initialized usage counts ({DEFAULT_USES} uses each) for {Math.Min(abilities.Count, 3)} abilities on character {character.name}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error initializing ability usage counts: {ex.Message}");
            }
        }

        /// <summary>
        /// HOOK POINT NEEDED: Find the game method that loads war characters
        /// The constructor patch doesn't work for IL2CPP classes.
        /// 
        /// Use dnSpy to search for:
        /// 1. Methods that set WAR_CHARA_TYPE.nouryoku
        /// 2. Methods that reference WAR_DATA.leader_no or WAR_DATA.sub_no
        /// 3. Classes like WarBattle, WarManager, WarController
        /// 
        /// Once found, uncomment and update one of the examples below:
        /// </summary>
        
        // EXAMPLE 1: Patch a method that loads a single character
        // [HarmonyPatch(typeof(WarBattleManager), "LoadCharacter")]
        // [HarmonyPostfix]
        // private static void OnCharacterLoaded(WAR_CHARA_TYPE character)
        // {
        //     if (character != null)
        //     {
        //         Logger.LogInfo($"Character loaded: {character.name}");
        //         ModifyCharacterAbilities(character);
        //     }
        // }
        
        // EXAMPLE 2: Patch a method that loads all characters
        // [HarmonyPatch(typeof(WarBattleManager), "InitializeCharacters")]
        // [HarmonyPostfix]
        // private static void OnCharactersInitialized(List<WAR_CHARA_TYPE> characters)
        // {
        //     if (characters != null)
        //     {
        //         Logger.LogInfo($"War characters initialized: {characters.Count}");
        //         foreach (var character in characters)
        //         {
        //             ModifyCharacterAbilities(character);
        //         }
        //     }
        // }
        
        // EXAMPLE 3: Patch the nouryoku setter
        // [HarmonyPatch(typeof(WAR_CHARA_TYPE), nameof(WAR_CHARA_TYPE.nouryoku), MethodType.Setter)]
        // [HarmonyPostfix]
        // private static void OnAbilitiesSet(WAR_CHARA_TYPE __instance)
        // {
        //     if (__instance != null)
        //     {
        //         Logger.LogInfo($"Abilities set for character: {__instance.name}");
        //         ModifyCharacterAbilities(__instance);
        //     }
        // }
        
        
        // ============================================================================
        // HARMONY PATCHES - WarChapter hooks
        // ============================================================================
        
        /// <summary>
        /// Patch WarChapter.Update - called every frame during war battle
        /// We modify abilities on the first frame only
        /// </summary>
        [HarmonyPatch("WarChapter", "Update")]
        [HarmonyPostfix]
        public static void AfterWarChapterUpdate(int __result)
        {
            // __result is the return value from Update()
            // If it returns non-zero, the battle is ending/ended
            if (__result != 0)
            {
                // Battle is ending, reset flag for next battle
                hasModifiedThisBattle = false;
                return;
            }
            
            // Battle is active, modify abilities on first frame
            if (!hasModifiedThisBattle)
            {
                hasModifiedThisBattle = true;
                Logger.LogInfo("[War Ability] ===== WAR BATTLE STARTED - WarChapter.Update called =====");
                ModifyAllWarCharacters();
            }
        }
        
        /// <summary>
        /// Modifies all war character abilities
        /// </summary>
        private static void ModifyAllWarCharacters()
        {
            try
            {
                Logger.LogInfo("[War Ability] Modifying character abilities...");
                
                if (Config == null || Config.CharacterAbilities == null)
                {
                    Logger.LogWarning("[War Ability] Config not loaded, skipping ability modifications");
                    return;
                }

                int modifiedCount = 0;

                // Iterate through all 108 war characters
                for (int i = 0; i < 108; i++)
                {
                    try
                    {
                        // Get the character data for this index
                        var chara = w_chara.chara(i);
                        
                        if (chara == null)
                        {
                            continue;
                        }

                        // Modify this character's abilities
                        ModifyCharacterAbilities(i, chara);
                        modifiedCount++;
                    }
                    catch (IndexOutOfRangeException)
                    {
                        // Some indices may not be valid, skip them
                        continue;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"[War Ability] Error processing character {i}: {ex.Message}");
                        continue;
                    }
                }

                Logger.LogInfo($"[War Ability] Ability modifications complete! Modified: {modifiedCount}");
            }
            catch (Exception)
            {
                // Silently catch any errors during character modification
            }
        }
        
        /// <summary>
        /// Harmony postfix for w_chara.charaInit - called when war characters are initialized
        /// This is where we modify the character abilities based on our configuration
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(w_chara), nameof(w_chara.charaInit))]
        public static void AfterCharaInit()
        {
            try
            {
                Logger.LogInfo("[War Ability] ===== AfterCharaInit CALLED! =====");
                Logger.LogInfo("[War Ability] w_chara.charaInit hook triggered!");
                
                if (Config == null || Config.CharacterAbilities == null)
                {
                    Logger.LogWarning("[War Ability] Config not loaded, skipping ability modifications");
                    return;
                }

                int modifiedCount = 0;

                // Iterate through all 108 war characters
                for (int i = 0; i < 108; i++)
                {
                    // Get the character data for this index
                    var chara = w_chara.chara(i);
                    
                    if (chara == null)
                    {
                        continue;
                    }

                    // Modify this character's abilities
                    ModifyCharacterAbilities(i, chara);
                    modifiedCount++;
                }

                Logger.LogInfo($"[War Ability] Ability modifications complete! Modified: {modifiedCount}");
            }
            catch (Exception)
            {
                // Silently catch any errors during character initialization
            }
        }
    }
}
