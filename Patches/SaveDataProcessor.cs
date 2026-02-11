using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;

namespace PKCore.Patches
{
    /// <summary>
    /// Extracts and caches protagonist and HQ names from save data for use in text placeholder replacement.
    /// Uses reflection to access GAME_DATA from the GSD2 assembly at runtime.
    /// </summary>
    [HarmonyPatch]
    public static class SaveDataProcessor
    {
        // Cached name values from save data
        private static string s_s2ProtagonistName = null;
        private static string s_s1ProtagonistName = null;
        private static string s_s2HQName = null;
        private static string s_s1HQName = null;

        // Default fallback names
        private const string DEFAULT_S2_PROTAGONIST = "Hero";
        private const string DEFAULT_S1_PROTAGONIST = "Tir";
        private const string DEFAULT_S2_HQ = "Dunan";
        private const string DEFAULT_S1_HQ = "Liberation";

        /// <summary>
        /// Refresh names from the current GAME_DATA instance.
        /// Called automatically when a save is loaded.
        /// </summary>
        public static void RefreshNames()
        {
            try
            {
                // Access GAME_DATA via reflection from GSD2 assembly
                var gsd2Assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "GSD2");
                
                if (gsd2Assembly == null)
                {
                    Plugin.Log.LogWarning("[SaveDataProcessor] GSD2 assembly not found");
                    ResetToDefaults();
                    return;
                }

                // Find GlobalWork.game_data
                var globalWorkType = gsd2Assembly.GetType("GlobalWork");
                if (globalWorkType == null)
                {
                    Plugin.Log.LogWarning("[SaveDataProcessor] GlobalWork type not found");
                    ResetToDefaults();
                    return;
                }

                var gameDataField = globalWorkType.GetField("game_data", BindingFlags.Public | BindingFlags.Static);
                if (gameDataField == null)
                {
                    Plugin.Log.LogWarning("[SaveDataProcessor] game_data field not found");
                    ResetToDefaults();
                    return;
                }

                var gameData = gameDataField.GetValue(null);
                if (gameData == null)
                {
                    Plugin.Log.LogWarning("[SaveDataProcessor] GAME_DATA is null, using default names");
                    ResetToDefaults();
                    return;
                }

                // Extract names using reflection (properties, not fields)
                var gameDataType = gameData.GetType();
                s_s2ProtagonistName = GetPropertyValue(gameData, gameDataType, "bozu_name", DEFAULT_S2_PROTAGONIST);
                s_s1ProtagonistName = GetPropertyValue(gameData, gameDataType, "macd_name", DEFAULT_S1_PROTAGONIST);
                s_s2HQName = GetPropertyValue(gameData, gameDataType, "base_name", DEFAULT_S2_HQ);
                s_s1HQName = GetPropertyValue(gameData, gameDataType, "m_base_name", DEFAULT_S1_HQ);

                Plugin.Log.LogInfo($"[SaveDataProcessor] Names refreshed - S2 Hero: '{s_s2ProtagonistName}', S1 Hero: '{s_s1ProtagonistName}', S2 HQ: '{s_s2HQName}', S1 HQ: '{s_s1HQName}'");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[SaveDataProcessor] Failed to refresh names: {ex.Message}");
                ResetToDefaults();
            }
        }

        /// <summary>
        /// Get a property value using reflection.
        /// </summary>
        private static string GetPropertyValue(object instance, Type type, string propertyName, string defaultValue)
        {
            try
            {
                var property = type.GetProperty(propertyName);
                if (property != null)
                {
                    var value = property.GetValue(instance) as string;
                    return GetValidName(value, defaultValue);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogDebug($"[SaveDataProcessor] Failed to get property '{propertyName}': {ex.Message}");
            }
            return defaultValue;
        }

        /// <summary>
        /// Get a valid name, falling back to default if null or empty.
        /// </summary>
        private static string GetValidName(string name, string defaultName)
        {
            return string.IsNullOrWhiteSpace(name) ? defaultName : name.Trim();
        }

        /// <summary>
        /// Reset all names to defaults.
        /// </summary>
        private static void ResetToDefaults()
        {
            s_s2ProtagonistName = DEFAULT_S2_PROTAGONIST;
            s_s1ProtagonistName = DEFAULT_S1_PROTAGONIST;
            s_s2HQName = DEFAULT_S2_HQ;
            s_s1HQName = DEFAULT_S1_HQ;
        }

        /// <summary>
        /// Get Suikoden 2 protagonist name (bozu_name).
        /// </summary>
        public static string GetS2ProtagonistName()
        {
            if (s_s2ProtagonistName == null)
                RefreshNames();
            return s_s2ProtagonistName ?? DEFAULT_S2_PROTAGONIST;
        }

        /// <summary>
        /// Get Suikoden 1 protagonist name from save transfer (macd_name).
        /// </summary>
        public static string GetS1ProtagonistName()
        {
            if (s_s1ProtagonistName == null)
                RefreshNames();
            return s_s1ProtagonistName ?? DEFAULT_S1_PROTAGONIST;
        }

        /// <summary>
        /// Get Suikoden 2 HQ name (base_name).
        /// </summary>
        public static string GetS2HQName()
        {
            if (s_s2HQName == null)
                RefreshNames();
            return s_s2HQName ?? DEFAULT_S2_HQ;
        }

        /// <summary>
        /// Get Suikoden 1 HQ name from save transfer (m_base_name).
        /// </summary>
        public static string GetS1HQName()
        {
            if (s_s1HQName == null)
                RefreshNames();
            return s_s1HQName ?? DEFAULT_S1_HQ;
        }

        // Harmony patch to refresh names when save is loaded
        [HarmonyPatch(typeof(SystemSave), nameof(SystemSave.Load))]
        [HarmonyPostfix]
        public static void OnSaveLoaded()
        {
            Plugin.Log.LogDebug("[SaveDataProcessor] Save loaded, refreshing names...");
            RefreshNames();
        }
    }
}
