using HarmonyLib;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Linq;
using BepInEx;

namespace PKCore.Patches
{
    /// <summary>
    /// Extracts and caches protagonist and HQ names from save data for use in text placeholder replacement.
    /// Reads from save files - supports both Suikoden Fix decrypted saves and native encrypted saves.
    /// </summary>
    [HarmonyPatch]
    public static class SaveDataProcessor
    {
        // Cached name values from save data
        private static string s_s2ProtagonistName = null;
        private static string s_s1ProtagonistName = null;
        private static string s_s2HQName = null;
        private static string s_s1HQName = null;
        
        private static bool? s_isSuikodenFixActive = null;

        // Default fallback names
        private const string DEFAULT_S2_PROTAGONIST = "Hero";
        private const string DEFAULT_S1_PROTAGONIST = "Tir";
        private const string DEFAULT_S2_HQ = "Dunan";
        private const string DEFAULT_S1_HQ = "Liberation";

        /// <summary>
        /// Check if Suikoden Fix's EditSavePatch is active.
        /// </summary>
        private static bool IsSuikodenFixActive()
        {
            if (s_isSuikodenFixActive.HasValue)
                return s_isSuikodenFixActive.Value;

            try
            {
                // Check if Suikoden_Fix assembly is loaded and EditSavePatch exists
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Suikoden_Fix");
                
                if (assembly != null)
                {
                    var editSavePatchType = assembly.GetType("Suikoden_Fix.Patches.EditSavePatch");
                    if (editSavePatchType != null)
                    {
                        s_isSuikodenFixActive = true;
                        Plugin.Log.LogInfo("[SaveDataProcessor] Suikoden Fix detected - using their decrypted saves");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogDebug($"[SaveDataProcessor] Suikoden Fix check failed: {ex.Message}");
            }

            s_isSuikodenFixActive = false;
            Plugin.Log.LogInfo("[SaveDataProcessor] Suikoden Fix not detected - using PKCore fallback decryption");
            return false;
        }

        /// <summary>
        /// Clean up PKCore fallback saves if Suikoden Fix is active.
        /// </summary>
        private static void CleanupFallbackSaves()
        {
            try
            {
                string fallbackDir = Path.Combine(Paths.GameRootPath, "PKCore", "Fallback_save", "gsd2");
                if (Directory.Exists(fallbackDir))
                {
                    Directory.Delete(fallbackDir, true);
                    Plugin.Log.LogInfo("[SaveDataProcessor] Cleaned up PKCore fallback saves (Suikoden Fix active)");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogDebug($"[SaveDataProcessor] Failed to clean up fallback saves: {ex.Message}");
            }
        }

        /// <summary>
        /// Refresh names from the current GAME_DATA instance.
        /// Called automatically when a save is loaded.
        /// <summary>
        /// Refresh cached names from save data.
        /// Checks Suikoden Fix status first - if active, uses their decrypted saves and cleans up PKCore fallback.
        /// Otherwise, uses PKCore's own fallback decryption system.
        /// </summary>
        public static void RefreshNames()
        {
            try
            {
                bool suikodenFixActive = IsSuikodenFixActive();

                // If Suikoden Fix is active, use their decrypted saves and clean up our fallback
                if (suikodenFixActive)
                {
                    CleanupFallbackSaves();
                    
                    string decryptedPath = Path.Combine(Paths.GameRootPath, "SuikodenFix", "Decrypted", "gsd2");
                    if (TryLoadFromDecryptedSaves(decryptedPath))
                    {
                        return;
                    }
                    
                    Plugin.Log.LogWarning("[SaveDataProcessor] Suikoden Fix active but no decrypted saves found");
                }
                else
                {
                    // No Suikoden Fix - first try our own decrypted fallback saves
                    string pkcoreFallbackPath = Path.Combine(Paths.GameRootPath, "PKCore", "Fallback_save", "gsd2");
                    if (TryLoadFromDecryptedSaves(pkcoreFallbackPath))
                    {
                        return;
                    }

                    // Decrypt native saves to our fallback location
                    string nativeSavePath = Path.Combine(Paths.GameRootPath, "Save");
                    if (TryDecryptNativeSavesToFallback(nativeSavePath, pkcoreFallbackPath))
                    {
                        return;
                    }
                }

                Plugin.Log.LogWarning("[SaveDataProcessor] No save files found, using defaults");
                ResetToDefaults();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[SaveDataProcessor] Failed to refresh names: {ex.Message}");
                ResetToDefaults();
            }
        }

        /// <summary>
        /// Try to load from Suikoden Fix decrypted saves.
        /// </summary>
        private static bool TryLoadFromDecryptedSaves(string saveDir)
        {
            try
            {
                if (!Directory.Exists(saveDir))
                    return false;

                var saveFiles = Directory.GetFiles(saveDir, "Data*.json");
                if (saveFiles.Length == 0)
                    return false;

                // Find most recently modified save
                string latestSaveFile = saveFiles.OrderByDescending(f => File.GetLastWriteTime(f)).First();

                // Parse JSON and extract names
                string json = File.ReadAllText(latestSaveFile);
                if (ParseGameData(json, Path.GetFileName(latestSaveFile)))
                {
                    Plugin.Log.LogInfo($"[SaveDataProcessor] Loaded from Suikoden Fix decrypted save: {Path.GetFileName(latestSaveFile)}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogDebug($"[SaveDataProcessor] Failed to load decrypted saves: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Decrypt native encrypted saves to PKCore fallback location.
        /// Creates decrypted JSON copies for faster future access (similar to Suikoden Fix).
        /// </summary>
        private static bool TryDecryptNativeSavesToFallback(string saveDir, string fallbackDir)
        {
            try
            {
                if (!Directory.Exists(saveDir))
                    return false;

                // Ensure fallback directory exists
                Directory.CreateDirectory(fallbackDir);

                bool anyDecrypted = false;

                // Find user save directory (format: Save/[userid]/gsd2/)
                var userDirs = Directory.GetDirectories(saveDir);
                foreach (var userDir in userDirs)
                {
                    string gsd2Path = Path.Combine(userDir, "gsd2");
                    if (!Directory.Exists(gsd2Path))
                        continue;

                    var saveFiles = Directory.GetFiles(gsd2Path, "Data*.sav");
                    if (saveFiles.Length == 0)
                        continue;

                    Plugin.Log.LogInfo($"[SaveDataProcessor] Decrypting {saveFiles.Length} native saves to PKCore/Fallback_save/gsd2/");

                    foreach (var saveFile in saveFiles)
                    {
                        try
                        {
                            // Read and decrypt the save file
                            string encryptedData = File.ReadAllText(saveFile, System.Text.Encoding.UTF8);
                            
                            // Strip SystemSave.HEADER prefix
                            if (!encryptedData.StartsWith(SystemSave.HEADER))
                            {
                                Plugin.Log.LogDebug($"[SaveDataProcessor] Save file missing header: {Path.GetFileName(saveFile)}");
                                continue;
                            }
                            
                            string encryptedContent = encryptedData.Substring(SystemSave.HEADER.Length);
                            
                            // Decrypt using SystemSave encryption
                            string json = Encrypter.Decrypt(encryptedContent, SystemSave.ENCRYPT_PASSWORD);
                            
                            if (!string.IsNullOrEmpty(json))
                            {
                                // Save decrypted JSON to fallback location
                                string fileName = Path.GetFileNameWithoutExtension(saveFile) + ".json";
                                string fallbackFile = Path.Combine(fallbackDir, fileName);
                                File.WriteAllText(fallbackFile, json, System.Text.Encoding.UTF8);
                                
                                // Preserve original file timestamps for proper ordering
                                File.SetLastWriteTime(fallbackFile, File.GetLastWriteTime(saveFile));
                                
                                anyDecrypted = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Plugin.Log.LogDebug($"[SaveDataProcessor] Failed to decrypt {Path.GetFileName(saveFile)}: {ex.Message}");
                        }
                    }
                }

                if (anyDecrypted)
                {
                    Plugin.Log.LogInfo("[SaveDataProcessor] Decryption complete, loading from fallback saves");
                    // Now load from the decrypted fallback saves
                    return TryLoadFromDecryptedSaves(fallbackDir);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogDebug($"[SaveDataProcessor] Failed to decrypt native saves: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Parse game_data from JSON and extract names.
        /// </summary>
        private static bool ParseGameData(string json, string fileName)
        {
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    
                    if (root.TryGetProperty("game_data", out JsonElement gameData))
                    {
                        s_s2ProtagonistName = gameData.TryGetProperty("bozu_name", out JsonElement bozu) 
                            ? GetValidName(bozu.GetString(), DEFAULT_S2_PROTAGONIST) 
                            : DEFAULT_S2_PROTAGONIST;
                            
                        s_s1ProtagonistName = gameData.TryGetProperty("macd_name", out JsonElement macd) 
                            ? GetValidName(macd.GetString(), DEFAULT_S1_PROTAGONIST) 
                            : DEFAULT_S1_PROTAGONIST;
                            
                        s_s2HQName = gameData.TryGetProperty("base_name", out JsonElement baseN) 
                            ? GetValidName(baseN.GetString(), DEFAULT_S2_HQ) 
                            : DEFAULT_S2_HQ;
                            
                        s_s1HQName = gameData.TryGetProperty("m_base_name", out JsonElement mbase) 
                            ? GetValidName(mbase.GetString(), DEFAULT_S1_HQ) 
                            : DEFAULT_S1_HQ;

                        Plugin.Log.LogInfo($"[SaveDataProcessor] Names: S2='{s_s2ProtagonistName}', S1='{s_s1ProtagonistName}', S2HQ='{s_s2HQName}', S1HQ='{s_s1HQName}'");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogDebug($"[SaveDataProcessor] Failed to parse {fileName}: {ex.Message}");
            }
            return false;
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
