using HarmonyLib;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace PKCore.Patches
{
    /// <summary>
    /// Global sprite swap patch that intercepts ALL Image.sprite assignments
    /// and automatically swaps controller button sprites based on user preference
    /// </summary>
    [HarmonyPatch]
    public class GlobalControllerPromptPatch
    {
        private static ManualLogSource Logger => Plugin.Log;
        private static Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
        
        // Counter for cycling through PlayStation face buttons when Xbox uses generic sprite
        private static int xboxGenericButtonCounter = 0;
        private static readonly string[] psButtonCycle = { "05", "06", "07", "08" }; // Square, Triangle, Circle, Cross

        /// <summary>
        /// Intercept all Image.sprite setter calls to swap controller button sprites
        /// </summary>
        [HarmonyPatch(typeof(Image), nameof(Image.sprite), MethodType.Setter)]
        [HarmonyPrefix]
        public static void Image_set_sprite_Prefix(Image __instance, ref Sprite value)
        {
            if (!Plugin.Config.ForceControllerPrompts.Value || value == null)
                return;

            string originalName = value.name;
            
            // Check if this is a controller button sprite (ends with _00, _01, or _02)
            if (!IsControllerSprite(originalName))
                return;

            // Determine desired controller suffix based on config
            string desiredSuffix = GetControllerSuffix(Plugin.Config.ControllerPromptType.Value);
            
            // If sprite already has correct suffix, no need to swap
            if (originalName.EndsWith(desiredSuffix))
                return;

            // Special case: Xbox minigame uses one sprite for all 4 face buttons
            // Cycle through PlayStation's 4 separate sprites based on occurrence order
            string targetName;
            if (IsXboxGenericMinigameButton(originalName) && desiredSuffix != "_00")
            {
                targetName = GetCycledPlayStationButton(desiredSuffix);
                Logger.LogDebug($"Xbox generic button #{xboxGenericButtonCounter} -> {targetName}");
            }
            else
            {
                // Build the target sprite name normally
                targetName = ReplaceControllerSuffix(originalName, desiredSuffix);
            }
            
            // Try to load the swapped sprite
            Sprite swappedSprite = LoadSprite(targetName);
            if (swappedSprite != null)
            {
                Logger.LogDebug($"Swapped controller sprite: {originalName} -> {targetName}");
                value = swappedSprite;
            }
            else
            {
                Logger.LogWarning($"Failed to load swapped sprite: {targetName}");
            }
        }

        /// <summary>
        /// Check if this is the Xbox generic minigame button (UI_MiniGame_Icon05_001_00)
        /// Xbox uses ONE sprite for all 4 face buttons, but PlayStation uses 4 separate sprites.
        /// </summary>
        private static bool IsXboxGenericMinigameButton(string spriteName)
        {
            return spriteName == "UI_MiniGame_Icon05_001_00";
        }

        /// <summary>
        /// Cycle through PlayStation face button sprites in order: Square, Triangle, Circle, Cross
        /// </summary>
        private static string GetCycledPlayStationButton(string suffix)
        {
            // Get the button icon number based on counter (cycles 0-3)
            string iconNumber = psButtonCycle[xboxGenericButtonCounter % 4];
            
            // Build sprite name: UI_MiniGame_Icon05_001_01 -> UI_MiniGame_Icon06_001_01, etc.
            string spriteName = $"UI_MiniGame_Icon{iconNumber}_001{suffix}";
            
            // Increment counter for next occurrence
            xboxGenericButtonCounter++;
            
            return spriteName;
        }

        /// <summary>
        /// Reset the button cycle counter (call when entering/exiting minigames)
        /// </summary>
        public static void ResetButtonCycle()
        {
            xboxGenericButtonCounter = 0;
            Logger.LogDebug("Reset Xbox generic button cycle counter");
        }

        /// <summary>
        /// Check if sprite name follows controller button naming pattern
        /// Pattern: SomeName_XX where XX is 00 (Xbox), 01 (PS4), or 02 (PS5)
        /// </summary>
        private static bool IsControllerSprite(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName) || spriteName.Length < 3)
                return false;

            // Check if sprite ends with controller suffix pattern
            return spriteName.EndsWith("_00") || 
                   spriteName.EndsWith("_01") || 
                   spriteName.EndsWith("_02");
        }

        /// <summary>
        /// Get the sprite suffix for the desired controller type
        /// </summary>
        /// <param name="controllerTypeStr">String: "PlayStation", "Xbox", "Switch", etc.</param>
        private static string GetControllerSuffix(string controllerTypeStr)
        {
            // Try to parse as int first
            if (int.TryParse(controllerTypeStr, out int controllerType))
            {
                return GetControllerSuffixByIndex(controllerType);
            }

            // Parse by name
            string normalized = controllerTypeStr.ToLowerInvariant().Replace(" ", "");
            
            // PS4 variants → _01
            if (normalized == "playstation" || 
                normalized == "playstation4" || 
                normalized == "ds4" || 
                normalized == "ps4")
            {
                return "_01";
            }
            
            // PS5 variants → _02
            if (normalized == "playstation5" || 
                normalized == "dualsense" || 
                normalized == "ps5")
            {
                return "_02";
            }
            
            // Xbox/Generic/Switch → _00
            if (normalized == "xbox" || 
                normalized == "ps" || 
                normalized == "generic" || 
                normalized == "switch" ||
                normalized == "nintendo" ||
                normalized == "pc" ||
                normalized == "keyboard")
            {
                return "_00";
            }
            
            // Default fallback
            Logger.LogWarning($"Unknown controller type: {controllerTypeStr}, defaulting to PS4");
            return "_01";
        }

        /// <summary>
        /// Get suffix by numeric index (for backward compatibility)
        /// </summary>
        private static string GetControllerSuffixByIndex(int controllerType)
        {
            switch (controllerType)
            {
                case 0: // PlayStation
                    return "_02";
                case 1: // Xbox
                    return "_00";
                case 2: // Switch
                    return "_02";
                case 3: // PC
                    return "_00";
                default:
                    return "_02";
            }
        }

        /// <summary>
        /// Replace the controller suffix in a sprite name
        /// </summary>
        private static string ReplaceControllerSuffix(string spriteName, string newSuffix)
        {
            if (spriteName.Length < 3)
                return spriteName;
            
            // Remove last 3 characters (_XX) and add new suffix
            return spriteName.Substring(0, spriteName.Length - 3) + newSuffix;
        }

        /// <summary>
        /// Load a sprite by name from Resources, with caching
        /// </summary>
        private static Sprite LoadSprite(string spriteName)
        {
            // Check cache first
            if (spriteCache.TryGetValue(spriteName, out Sprite cachedSprite) && cachedSprite != null)
                return cachedSprite;

            // Try to load from Resources
            try
            {
                var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
                foreach (var sprite in allSprites)
                {
                    if (sprite != null && sprite.name == spriteName)
                    {
                        spriteCache[spriteName] = sprite;
                        Logger.LogInfo($"Loaded sprite from resources: {spriteName}");
                        return sprite;
                    }
                }

                Logger.LogWarning($"Sprite not found in resources: {spriteName}");
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Error loading sprite {spriteName}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Clear the sprite cache (call this if you want to reload sprites)
        /// </summary>
        public static void ClearCache()
        {
            spriteCache.Clear();
            Logger.LogInfo("Controller sprite cache cleared");
        }
    }
}
