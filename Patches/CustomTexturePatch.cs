using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using BepInEx;

namespace PKCore.Patches;

/// <summary>
/// Core texture replacement system - shared state and helper functions
/// Actual patches are in separate files: SpriteRendererPatch, SpriteAtlasPatch, etc.
/// </summary>
[HarmonyPatch]
public partial class CustomTexturePatch
{
    #region Shared State
    
    // Caches
    internal static Dictionary<string, Sprite> customSpriteCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
    internal static Dictionary<string, Texture2D> customTextureCache = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
    internal static Dictionary<string, string> texturePathIndex = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // Maps texture name -> full file path
    
    // Tracking for persistent textures that shouldn't be cleared on scene change
    internal static HashSet<string> persistentTextures = new HashSet<string> { "window_", "t_obj_savePoint_ball", "sactx" };
    
    // Tracking
    internal static HashSet<string> loggedTextures = new HashSet<string>();
    internal static HashSet<string> replacedTextures = new HashSet<string>();
    internal static HashSet<int> processedTextureIds = new HashSet<int>();
    internal static HashSet<string> processedAtlases = new HashSet<string>();
    
    // Preloaded sprites
    internal static Dictionary<string, Sprite> preloadedBathSprites = new Dictionary<string, Sprite>();
    internal static Dictionary<string, Sprite> preloadedSavePointSprites = new Dictionary<string, Sprite>();
    
    // State tracking
    internal static int lastBathBGInstanceID = -1;
    internal static int bathBackgroundScanAttempts = 0;
    internal static string customTexturesPath;
    
    #endregion
    
    #region Helper Functions
    
    /// <summary>
    /// Check if a texture has already been logged
    /// </summary>
    internal static bool IsTextureLogged(string textureName) => loggedTextures.Contains(textureName);
    
    /// <summary>
    /// Check if we have a custom texture for the given name
    /// </summary>
    internal static bool HasCustomTexture(string textureName) => texturePathIndex.ContainsKey(textureName);
    
    /// <summary>
    /// Log a replaceable texture/sprite (only once per texture)
    /// </summary>
    internal static void LogReplaceableTexture(string textureName, string category, string context = null)
    {
        if (!Plugin.Config.LogReplaceableTextures.Value || loggedTextures.Contains(textureName))
            return;

        loggedTextures.Add(textureName);
        string message = string.IsNullOrEmpty(context) 
            ? $"[Replaceable {category}] {textureName}"
            : $"[Replaceable {category}] {textureName} ({context})";
        Plugin.Log.LogInfo(message);
    }
    
    /// <summary>
    /// Get full hierarchy path of a GameObject for debugging
    /// </summary>
    internal static string GetGameObjectPath(GameObject obj)
    {
        if (obj == null) return "null";
        string path = obj.name;
        Transform current = obj.transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }

    /// <summary>
    /// Clean sactx- style texture names to their base names
    /// e.g. sactx-0-128x128-Uncompressed-m_gat1_00_atlas-b4ac1ef3 -> m_gat1_00_atlas
    /// </summary>
    internal static string CleanSactxName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        if (!name.StartsWith("sactx-")) return name;

        string[] parts = name.Split('-');
        if (parts.Length < 6) return name; // Basic sactx has at least 6 parts (sactx, layer, res, comp, name, hash)

        // The texture name is everything after the first 4 parts and before the last part (the hash)
        // Skip sactx, layer, resolution, compression
        int nameStart = 4;
        int nameEnd = parts.Length - 2;
        
        if (nameStart > nameEnd) return name;
        
        // Join the name parts back together in case the name contained hyphens
        return string.Join("-", parts.Skip(nameStart).Take(nameEnd - nameStart + 1));
    }
    
    /// <summary>
    /// Try to replace bath sprites in the BathBG GameObject
    /// </summary>
    internal static int TryReplaceBathSprites()
    {
        if (!Plugin.Config.EnableCustomTextures.Value)
            return -1;

        var bathBG = GameObject.Find("AppRoot/BathBG");
        if (bathBG == null)
            return -1;

        int currentInstanceID = bathBG.GetInstanceID();
        
        // Only replace if this is a NEW BathBG instance
        if (currentInstanceID == lastBathBGInstanceID)
            return -1;

        if (Plugin.Config.DetailedTextureLog.Value)
        {
            Plugin.Log.LogInfo($"New BathBG instance detected (ID: {currentInstanceID}, previous: {lastBathBGInstanceID})");
        }
        
        lastBathBGInstanceID = currentInstanceID;
        
        var bathRenderers = bathBG.GetComponentsInChildren<SpriteRenderer>(true);
        int replaced = 0;
        
        foreach (var sr in bathRenderers)
        {
            if (sr.sprite != null)
            {
                string spriteName = sr.sprite.name;
                
                if (spriteName.StartsWith("bath_") && texturePathIndex.ContainsKey(spriteName))
                {
                    Sprite customSprite = LoadCustomSprite(spriteName, sr.sprite);
                    if (customSprite != null)
                    {
                        sr.sprite = customSprite;
                        
                        if (Plugin.Config.DetailedTextureLog.Value)
                        {
                            Plugin.Log.LogInfo($"Replaced bath sprite: {spriteName} in new BathBG instance");
                        }
                        
                        replaced++;
                    }
                }
            }
        }
        
        return replaced;
    }
    
    /// <summary>
    /// Check if a texture is a window-ui texture that should use Point filtering
    /// </summary>
    internal static bool IsWindowUITexture(string textureName, string filePath)
    {
        // Check if texture name starts with "window_"
        if (textureName.StartsWith("window_", StringComparison.OrdinalIgnoreCase))
            return true;
        
        // Check if file path contains "window-ui" folder
        if (filePath.Contains("\\window-ui\\", StringComparison.OrdinalIgnoreCase) ||
            filePath.Contains("/window-ui/", StringComparison.OrdinalIgnoreCase))
            return true;
        
        return false;
    }
    
    /// <summary>
    /// Check if a file path is allowed to be loaded for the current game.
    /// Prevents GSD1 textures from loading in GSD2 and vice versa.
    /// Also applies recursively within 00-Mods subdirectories.
    /// </summary>
    internal static bool IsPathAllowedForCurrentGame(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return false;
        
        string currentGame = GameDetection.GetCurrentGame();
        
        // If not in a specific game (launcher/menu), allow all textures
        if (currentGame != "GSD1" && currentGame != "GSD2")
            return true;
        
        // Normalize path separators for consistent checking
        string normalizedPath = filePath.Replace('/', '\\');
        
        // Check for game-specific restrictions in main folders and recursively in 00-Mods
        bool isGSD1Path = normalizedPath.Contains("\\GSD1\\", StringComparison.OrdinalIgnoreCase);
        bool isGSD2Path = normalizedPath.Contains("\\GSD2\\", StringComparison.OrdinalIgnoreCase);
        
        // Block GSD2 textures when in GSD1
        if (currentGame == "GSD1" && isGSD2Path)
        {
            if (Plugin.Config.DetailedTextureLog.Value)
                Plugin.Log.LogInfo($"[Isolation] Blocked GSD2 texture in GSD1: {Path.GetFileName(filePath)}");
            return false;
        }
        
        // Block GSD1 textures when in GSD2
        if (currentGame == "GSD2" && isGSD1Path)
        {
            if (Plugin.Config.DetailedTextureLog.Value)
                Plugin.Log.LogInfo($"[Isolation] Blocked GSD1 texture in GSD2: {Path.GetFileName(filePath)}");
            return false;
        }
        
        return true;
    }

    
    #endregion
    
    #region Core Texture/Sprite Loading
    
    /// <summary>
    /// Load a custom sprite from PNG file
    /// </summary>
    internal static Sprite LoadCustomSprite(string spriteName, Sprite originalSprite)
    {
        // Try save point sprite loading first (handled by SavePointPatch)
        Sprite savePointSprite = TryLoadSavePointSprite(spriteName, originalSprite);
        if (savePointSprite != null)
            return savePointSprite;
        
        // Check cache
        if (customSpriteCache.TryGetValue(spriteName, out Sprite cachedSprite))
        {
            if (cachedSprite != null && cachedSprite && cachedSprite.texture != null)
            {
                return cachedSprite;
            }
            else
            {
                customSpriteCache.Remove(spriteName);
            }
        }

        // Load texture
        Texture2D texture = LoadCustomTexture(spriteName);
        if (texture == null)
            return null;

        // Preserve original sprite properties
        Vector2 pivot = originalSprite != null ? originalSprite.pivot / originalSprite.rect.size : new Vector2(0.5f, 0.5f);
        float pixelsPerUnit = originalSprite != null ? originalSprite.pixelsPerUnit : 100f;
        Vector4 border = originalSprite != null ? originalSprite.border : Vector4.zero;

        // Auto-scale pixelsPerUnit to maintain original display size
        if (originalSprite != null)
        {
            float scaleRatio = texture.width / originalSprite.rect.width;
            pixelsPerUnit = originalSprite.pixelsPerUnit * scaleRatio;
        }

        // Create sprite
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            pivot,
            pixelsPerUnit,
            0,
            SpriteMeshType.FullRect,
            border
        );

        UnityEngine.Object.DontDestroyOnLoad(sprite);
        UnityEngine.Object.DontDestroyOnLoad(texture);

        customSpriteCache[spriteName] = sprite;
        
        return sprite;
    }

    /// <summary>
    /// Load a custom texture from image file
    /// </summary>
    internal static Texture2D LoadCustomTexture(string textureName)
    {
        if (string.IsNullOrEmpty(textureName)) return null;

        // Check cache with original name first
        if (customTextureCache.TryGetValue(textureName, out Texture2D cachedTexture))
        {
            if (cachedTexture != null && cachedTexture)
                return cachedTexture;
            else
                customTextureCache.Remove(textureName);
        }

        // 1. Try with original name (and its variants)
        string lookupName = TextureOptions.GetTextureNameWithVariant(textureName);

        // Try game-specific texture first (GSD1: or GSD2: prefix)
        string currentGame = GameDetection.GetCurrentGame();
        string targetKey = null;

        if (currentGame == "GSD1" || currentGame == "GSD2")
        {
            string gameSpecificKey = $"{currentGame}:{lookupName}";
            if (texturePathIndex.ContainsKey(gameSpecificKey))
                targetKey = gameSpecificKey;
        }

        if (targetKey == null && texturePathIndex.ContainsKey(lookupName))
            targetKey = lookupName;

        // 2. FALLBACK: Clean name for sactx textures if no exact match found
        if (targetKey == null && textureName.StartsWith("sactx-"))
        {
            string cleanedName = CleanSactxName(textureName);
            if (cleanedName != textureName)
            {
                string cleanedLookup = TextureOptions.GetTextureNameWithVariant(cleanedName);
                
                if (currentGame == "GSD1" || currentGame == "GSD2")
                {
                    string gsCleaned = $"{currentGame}:{cleanedLookup}";
                    if (texturePathIndex.ContainsKey(gsCleaned))
                        targetKey = gsCleaned;
                }
                
                if (targetKey == null && texturePathIndex.ContainsKey(cleanedLookup))
                    targetKey = cleanedLookup;
                
                if (targetKey != null)
                {
                    // Also check if we already cached this under the CLEANED name
                    if (customTextureCache.TryGetValue(cleanedLookup, out Texture2D cleanedCache))
                    {
                        // Cache it under the full name too to speed up next time
                        customTextureCache[textureName] = cleanedCache;
                        return cleanedCache;
                    }
                    lookupName = targetKey; // Use this for the rest of the function
                }
            }
        }

        if (targetKey == null) return null;

        // Verify the resolved path is allowed for current game
        if (!texturePathIndex.TryGetValue(targetKey, out string resolvedPath))
            return null;
            
        if (!IsPathAllowedForCurrentGame(resolvedPath))
            return null;

        // Use decentralized AssetLoader for unified loading logic
        Texture2D texture = AssetLoader.LoadTextureSync(targetKey, textureName != targetKey ? textureName : null);
        
        if (texture != null)
        {
            customTextureCache[textureName] = texture;
            return texture;
        }

        return null;

    }

    /// <summary>
    /// Replace texture pixels in-place (preserves references)
    /// </summary>
    internal static bool ReplaceTextureInPlace(Texture2D originalTexture, string textureName)
    {
        if (originalTexture == null || string.IsNullOrEmpty(textureName))
            return false;

        string targetKey = null;
        string lookupName = TextureOptions.GetTextureNameWithVariant(textureName);

        string currentGame = GameDetection.GetCurrentGame();

        // 1. Try exact match (and game-specific version)
        if (currentGame == "GSD1" || currentGame == "GSD2")
        {
            string gameKey = $"{currentGame}:{lookupName}";
            if (texturePathIndex.ContainsKey(gameKey))
                targetKey = gameKey;
        }

        if (targetKey == null && texturePathIndex.ContainsKey(lookupName))
            targetKey = lookupName;

        // 2. Try fallback to cleaned name for sactx textures
        if (targetKey == null && textureName.StartsWith("sactx-"))
        {
            string cleanedName = CleanSactxName(textureName);
            string cleanedLookup = TextureOptions.GetTextureNameWithVariant(cleanedName);
            
            if (currentGame == "GSD1" || currentGame == "GSD2")
            {
                string gameKey = $"{currentGame}:{cleanedLookup}";
                if (texturePathIndex.ContainsKey(gameKey))
                    targetKey = gameKey;
            }

            if (targetKey == null && texturePathIndex.ContainsKey(cleanedLookup))
                targetKey = cleanedLookup;
        }

        if (targetKey == null || !texturePathIndex.TryGetValue(targetKey, out string filePath))
        {
            if (textureName.Contains("m_gat") || textureName.Contains("Summon"))
            {
                if (Plugin.Config.DetailedTextureLog.Value)
                    Plugin.Log.LogDebug($"[ReplaceInPlace] No match for: {textureName} (Lookup: {lookupName}, Game: {currentGame})");
            }
            return false;
        }

        // Verify the resolved path is allowed for current game
        if (!IsPathAllowedForCurrentGame(filePath))
            return false;

        try

        {
            // Move IO to background thread
            byte[] fileData = System.Threading.Tasks.Task.Run(() => File.ReadAllBytes(filePath)).Result;
            
            // Handle DDS files separately
            bool isDDS = filePath.EndsWith(".dds", StringComparison.OrdinalIgnoreCase);
            bool loaded = false;
            
            if (isDDS)
            {
                // For DDS, we need to load the data into a new texture then copy to original
                // DDS format requires special handling via DDSLoader
                Texture2D ddsTexture = DDSLoader.LoadDDSFromBytes(fileData, textureName);
                if (ddsTexture != null)
                {
                    // Copy DDS texture data to original texture
                    // Note: This preserves the original texture reference but copies pixel data
                    byte[] ddsPixels = ddsTexture.GetRawTextureData();
                    originalTexture.LoadRawTextureData(ddsPixels);
                    originalTexture.Apply(false, false);
                    
                    // Match the DDS format properties
                    originalTexture.name = textureName + "_Custom";
                    UnityEngine.Object.DontDestroyOnLoad(originalTexture);
                    loaded = true;
                    
                    if (Plugin.Config.DetailedTextureLog.Value)
                    {
                        Plugin.Log.LogInfo($"Replaced raw DDS texture in-place: {textureName} ({ddsTexture.width}x{ddsTexture.height}, {ddsTexture.format})");
                    }
                }
                else
                {
                    Plugin.Log.LogError($"Failed to load DDS texture: {filePath}");
                    return false;
                }
            }
            else
            {
                // Standard image formats (PNG, JPG, TGA)
                if (!UnityEngine.ImageConversion.LoadImage(originalTexture, fileData))
                {
                    Plugin.Log.LogError($"Failed to load image data into texture: {filePath}");
                    return false;
                }
                loaded = true;
            }
            
            if (!loaded)
                return false;

            // Window-UI textures use Point filtering to prevent seams
            bool isWindowUI = IsWindowUITexture(textureName, filePath);
            originalTexture.filterMode = isWindowUI ? FilterMode.Point : FilterMode.Bilinear;
            originalTexture.wrapMode = TextureWrapMode.Clamp;
            originalTexture.anisoLevel = isWindowUI ? 0 : 4;
            
            originalTexture.Apply(true, false);
            
            // Rename to avoid repeated replacements and for tracking
            if (!originalTexture.name.EndsWith("_Custom"))
            {
                originalTexture.name = textureName + "_Custom";
            }
            
            UnityEngine.Object.DontDestroyOnLoad(originalTexture);
            
            bool shouldSkipLog = textureName.StartsWith("sactx") || filePath.ToLower().Contains("characters");
            if (!shouldSkipLog && Plugin.Config.DetailedTextureLog.Value)
            {
                Plugin.Log.LogInfo($"Replaced raw texture in-place: {textureName} ({originalTexture.width}x{originalTexture.height})");
            }
            
            return true;
        }
        catch (Exception ex)
        {
            if (!ex.Message.Contains("not readable"))
            {
                Plugin.Log.LogError($"Error replacing texture {textureName} in-place: {ex.Message}");
            }
            return false;
        }
    }
    
    #endregion
    
    #region Initialization
    
    /// <summary>
    /// Build index of all texture files
    /// Prioritizes DDS files over PNG/JPG to avoid duplicate counting
    /// </summary>
    private static void BuildTextureIndex()
    {
        texturePathIndex.Clear();
        
        if (!Directory.Exists(customTexturesPath))
            return;

        // Try to load from manifest cache first
        if (TryLoadManifestIndex())
            return;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        // Scan all files in one pass
        HashSet<string> validExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".tga", ".dds" };
        string[] allFiles = Directory.GetFiles(customTexturesPath, "*.*", SearchOption.AllDirectories);
        
        string modsFolder = Path.Combine(customTexturesPath, "00-Mods");
        string gsd1Folder = Path.Combine(customTexturesPath, "GSD1");
        string gsd2Folder = Path.Combine(customTexturesPath, "GSD2");
        
        int filesIndexed = 0;
        
        // Helper to add texture to index with priority
        void AddToIndex(string path, string key, bool allowOverride)
        {
            if (allowOverride || !texturePathIndex.ContainsKey(key))
            {
                texturePathIndex[key] = path;
                filesIndexed++;

                // Also index by cleaned name for sactx- style filenames
                // This allows matching even if resolutions or hashes differ
                string sactxPart = key;
                string prefix = "";
                if (key.Contains(":"))
                {
                    int idx = key.IndexOf(":");
                    prefix = key.Substring(0, idx + 1);
                    sactxPart = key.Substring(idx + 1);
                }

                if (sactxPart.StartsWith("sactx-"))
                {
                    string cleaned = CleanSactxName(sactxPart);
                    if (cleaned != sactxPart)
                    {
                        string cleanedKey = prefix + cleaned;
                        if (allowOverride || !texturePathIndex.ContainsKey(cleanedKey))
                        {
                            texturePathIndex[cleanedKey] = path;
                        }
                    }
                }
            }
        }

        // Process files in layers to handle overrides correctly
        // Layer 1: All files except special folders
        foreach (var file in allFiles)
        {
            string ext = Path.GetExtension(file);
            if (!validExtensions.Contains(ext)) continue;
            if (!TextureOptions.ShouldLoadTexture(file)) continue;

            string fileName = Path.GetFileNameWithoutExtension(file);
            
            bool isSpecial = file.StartsWith(modsFolder, StringComparison.OrdinalIgnoreCase) ||
                             file.StartsWith(gsd1Folder, StringComparison.OrdinalIgnoreCase) ||
                             file.StartsWith(gsd2Folder, StringComparison.OrdinalIgnoreCase);

            if (!isSpecial)
            {
                // DDS takes priority over other formats if both exist in same folder
                bool isDDS = ext.Equals(".dds", StringComparison.OrdinalIgnoreCase);
                AddToIndex(file, fileName, isDDS);
            }
        }

        // Layer 2: GSD1 / GSD2
        foreach (var file in allFiles)
        {
            string ext = Path.GetExtension(file);
            if (!validExtensions.Contains(ext)) continue;
            
            string fileName = Path.GetFileNameWithoutExtension(file);
            bool isDDS = ext.Equals(".dds", StringComparison.OrdinalIgnoreCase);

            if (file.StartsWith(gsd1Folder, StringComparison.OrdinalIgnoreCase))
            {
                AddToIndex(file, $"GSD1:{fileName}", isDDS);
                AddToIndex(file, fileName, isDDS); // Fallback
            }
            else if (file.StartsWith(gsd2Folder, StringComparison.OrdinalIgnoreCase))
            {
                AddToIndex(file, $"GSD2:{fileName}", isDDS);
                AddToIndex(file, fileName, isDDS); // Fallback (overrides GSD1 if both exist)
            }
        }

        // Layer 3: 00-Mods (Highest priority)
        foreach (var file in allFiles)
        {
            string ext = Path.GetExtension(file);
            if (!validExtensions.Contains(ext)) continue;

            if (file.StartsWith(modsFolder, StringComparison.OrdinalIgnoreCase))
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                bool isDDS = ext.Equals(".dds", StringComparison.OrdinalIgnoreCase);
                AddToIndex(file, fileName, isDDS || !texturePathIndex.ContainsKey(fileName));
            }
        }
        
        sw.Stop();
        if (Plugin.Config.DetailedTextureLog.Value)
        {
            Plugin.Log.LogInfo($"Indexed {texturePathIndex.Count} textures from {allFiles.Length} files in {sw.ElapsedMilliseconds}ms");
        }
        
        SaveManifestIndex();
    }

    /// <summary>
    /// Initialize custom texture system
    /// </summary>
    [HarmonyPatch(typeof(Plugin), "Load")]
    [HarmonyPostfix]
    public static void Initialize()
    {
        customTexturesPath = Path.Combine(Paths.GameRootPath, "PKCore", "Textures");
        
        if (!Plugin.Config.EnableCustomTextures.Value)
        {
            Plugin.Log.LogInfo("Custom textures disabled");
            return;
        }

        InitializeCaching();
        BuildTextureIndex();
        
        if (Plugin.Config.DetailedTextureLog.Value)
        {
            Plugin.Log.LogInfo($"Indexed {texturePathIndex.Count} custom texture(s) ready to use");
        }
        
        // Register for scene loaded to clear caches
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (Action<UnityEngine.SceneManagement.Scene, UnityEngine.SceneManagement.LoadSceneMode>)OnSceneLoaded;

        // Preload bath and save point sprites
        PreloadBathSprites();
        PreloadSavePointSprites();
    }

    /// <summary>
    /// Check if a texture/sprite key matches persistent patterns
    /// </summary>
    private static bool IsPersistentKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        
        foreach (var prefix in persistentTextures)
        {
            if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// Log a summary of all persistent textures and sprites currently in cache
    /// </summary>
    private static void LogPersistentTextureSummary(string sceneName)
    {
        if (!Plugin.Config.DetailedTextureLog.Value) return;
        
        var persistentTextureNames = customTextureCache.Keys.Where(IsPersistentKey).ToList();
        var persistentSpriteNames = customSpriteCache.Keys.Where(IsPersistentKey).ToList();
        
        if (persistentTextureNames.Count > 0 || persistentSpriteNames.Count > 0)
        {
            Plugin.Log.LogInfo($"[Persistent Summary] {persistentTextureNames.Count} textures, {persistentSpriteNames.Count} sprites surviving transition to {sceneName}");
            
            foreach (var name in persistentTextureNames)
                Plugin.Log.LogInfo($"  [Texture] {name}");
            
            foreach (var name in persistentSpriteNames)
                Plugin.Log.LogInfo($"  [Sprite] {name}");
        }
    }
    
    /// <summary>
    /// Clear non-persistent caches on scene change to save memory
    /// </summary>
    private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (mode == UnityEngine.SceneManagement.LoadSceneMode.Additive)
            return;

        // Log persistent textures BEFORE clearing
        LogPersistentTextureSummary(scene.name);

        int texturesCleared = 0;
        int spritesCleared = 0;

        // Identify non-persistent textures
        List<string> texturesToRemove = new List<string>();
        foreach (var kvp in customTextureCache)
        {
            if (!IsPersistentKey(kvp.Key))
                texturesToRemove.Add(kvp.Key);
        }

        // Identify non-persistent sprites
        List<string> spritesToRemove = new List<string>();
        foreach (var kvp in customSpriteCache)
        {
            if (!IsPersistentKey(kvp.Key))
                spritesToRemove.Add(kvp.Key);
        }

        // Perform cleanup
        foreach (var key in texturesToRemove)
        {
            if (customTextureCache.TryGetValue(key, out Texture2D tex) && tex)
            {
                UnityEngine.Object.Destroy(tex);
                texturesCleared++;
            }
            customTextureCache.Remove(key);
        }

        foreach (var key in spritesToRemove)
        {
            if (customSpriteCache.TryGetValue(key, out Sprite sprite) && sprite)
            {
                if (sprite.texture) UnityEngine.Object.Destroy(sprite.texture);
                UnityEngine.Object.Destroy(sprite);
                spritesCleared++;
            }
            customSpriteCache.Remove(key);
        }

        if ((texturesCleared > 0 || spritesCleared > 0) && Plugin.Config.DetailedTextureLog.Value)
        {
            Plugin.Log.LogInfo($"[Memory] Cleared {texturesCleared} textures and {spritesCleared} sprites on scene transition to {scene.name}");
        }

        // Reset tracking
        processedTextureIds.Clear();
        processedAtlases.Clear();
        replacedTextures.Clear();
    }
    
    #endregion
}
