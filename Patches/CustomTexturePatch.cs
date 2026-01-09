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
    internal static Dictionary<string, Sprite> customSpriteCache = new Dictionary<string, Sprite>();
    internal static Dictionary<string, Texture2D> customTextureCache = new Dictionary<string, Texture2D>();
    internal static Dictionary<string, string> texturePathIndex = new Dictionary<string, string>(); // Maps texture name -> full file path
    
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
    
    #endregion
    
    #region Core Texture/Sprite Loading
    
    /// <summary>
    /// Load a custom sprite from PNG file
    /// </summary>
    internal static Sprite LoadCustomSprite(string spriteName, Sprite originalSprite)
    {
        bool isSavePointSprite = spriteName.StartsWith("t_obj_savePoint_ball_");
        
        // For save point sprites, create from atlas on-demand
        if (isSavePointSprite)
        {
            // Check if we have the atlas texture
            string atlasName = "t_obj_savePoint_ball";
            if (texturePathIndex.ContainsKey(atlasName))
            {
                // Extract frame number from sprite name (e.g., "t_obj_savePoint_ball_0" -> 0)
                string frameNumStr = spriteName.Substring("t_obj_savePoint_ball_".Length);
                if (int.TryParse(frameNumStr, out int frameNum))
                {
                    Plugin.Log.LogInfo($"[SavePoint] Creating sprite from atlas for: {spriteName} (frame {frameNum})");
                    
                    // Load the atlas texture
                    Texture2D atlasTexture = LoadCustomTexture(atlasName);
                    if (atlasTexture != null)
                    {
                        // Atlas is 400x200 with 8 frames in a 4x2 grid (each frame is 100x100)
                        int frameWidth = 100;
                        int frameHeight = 100;
                        int columns = 4;
                        
                        // Calculate frame position (cycle through 8 frames if more than 8)
                        int frameIndex = frameNum % 8;
                        int col = frameIndex % columns;
                        int row = frameIndex / columns;
                        
                        // Calculate rect (flip Y for Unity's bottom-left origin)
                        float x = col * frameWidth;
                        float y = atlasTexture.height - (row + 1) * frameHeight;
                        
                        // Preserve original sprite properties if available
                        Vector2 customPivot = originalSprite != null ? originalSprite.pivot / originalSprite.rect.size : new Vector2(0.5f, 0.5f);
                        float customPPU = originalSprite != null ? originalSprite.pixelsPerUnit : 100f;

                        // Auto-scale pixelsPerUnit to maintain original display size
                        if (originalSprite != null)
                        {
                            // Assuming the frame width corresponds to the original width
                            float scaleRatio = frameWidth / originalSprite.rect.width;
                            customPPU = originalSprite.pixelsPerUnit * scaleRatio;
                        }

                        Plugin.Log.LogInfo($"[SavePoint] Creating sprite: rect=({x},{y},{frameWidth},{frameHeight}) from atlas {atlasTexture.width}x{atlasTexture.height} PPU:{customPPU} Pivot:{customPivot}");
                        
                        Sprite customSprite = Sprite.Create(
                            atlasTexture,
                            new Rect(x, y, frameWidth, frameHeight),
                            customPivot,
                            customPPU,
                            0,
                            SpriteMeshType.FullRect
                        );
                        
                        if (customSprite != null)
                        {
                            UnityEngine.Object.DontDestroyOnLoad(customSprite);
                            UnityEngine.Object.DontDestroyOnLoad(atlasTexture);
                            
                            // Cache it for future use
                            customSpriteCache[spriteName] = customSprite;
                            
                            Plugin.Log.LogInfo($"[SavePoint] ✓ Created and cached sprite: {spriteName}");
                            return customSprite;
                        }
                        else
                        {
                            Plugin.Log.LogError($"[SavePoint] Sprite.Create returned null for: {spriteName}");
                        }
                    }
                    else
                    {
                        Plugin.Log.LogError($"[SavePoint] Failed to load atlas texture: {atlasName}");
                    }
                }
            }
            else
            {
                Plugin.Log.LogWarning($"[SavePoint] Atlas texture '{atlasName}' not found in texture index");
            }
        }
        
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
        // Check cache
        if (customTextureCache.TryGetValue(textureName, out Texture2D cachedTexture))
        {
            if (cachedTexture != null && cachedTexture)
                return cachedTexture;
            else
                customTextureCache.Remove(textureName);
        }

        // Check for texture variants (e.g., save point colors)
        string lookupName = TextureOptions.GetTextureNameWithVariant(textureName);

        // Try game-specific texture first (GSD1: or GSD2: prefix)
        string currentGame = GameDetection.GetCurrentGame();
        if (currentGame == "GSD1" || currentGame == "GSD2")
        {
            string gameSpecificKey = $"{currentGame}:{lookupName}";
            if (texturePathIndex.ContainsKey(gameSpecificKey))
            {
                lookupName = gameSpecificKey; // Use game-specific version
            }
        }

        // Try DDS first if enabled
        if (Plugin.Config.EnableDDSTextures.Value)
        {
            // Look for .dds file
            if (texturePathIndex.TryGetValue(lookupName, out string ddsPath) && ddsPath.EndsWith(".dds", System.StringComparison.OrdinalIgnoreCase))
            {
                Texture2D ddsTexture = DDSLoader.LoadDDS(ddsPath);
                if (ddsTexture != null)
                {
                    ddsTexture.filterMode = FilterMode.Bilinear;
                    ddsTexture.wrapMode = TextureWrapMode.Clamp;
                    ddsTexture.anisoLevel = 4;
                    
                    UnityEngine.Object.DontDestroyOnLoad(ddsTexture);
                    customTextureCache[textureName] = ddsTexture;
                    
                    bool shouldSkipLog = textureName.StartsWith("sactx");
                    if (!shouldSkipLog && Plugin.Config.DetailedTextureLog.Value)
                    {
                        Plugin.Log.LogInfo($"Loaded pre-compressed DDS: {textureName} ({ddsTexture.width}x{ddsTexture.height})");
                    }
                    
                    return ddsTexture;
                }
            }
        }

        // Fall back to PNG/JPG loading with runtime compression
        // Look up full path - skip if it's a DDS file (already tried above)
        if (!texturePathIndex.TryGetValue(lookupName, out string filePath) || filePath.EndsWith(".dds", System.StringComparison.OrdinalIgnoreCase))
            return null;

        try
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, true);
            
            if (!UnityEngine.ImageConversion.LoadImage(texture, fileData))
            {
                Plugin.Log.LogError($"Failed to load image: {filePath}");
                UnityEngine.Object.Destroy(texture);
                return null;
            }

            // Compress texture to BC1/BC3/BC7 for GPU efficiency
            TextureCompression.CompressTexture(texture, textureName, filePath);

            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.anisoLevel = 4;
            
            texture.Apply(true, false);
            
            UnityEngine.Object.DontDestroyOnLoad(texture);

            customTextureCache[textureName] = texture;
            
            bool shouldSkipLog = textureName.StartsWith("sactx") || filePath.ToLower().Contains("characters");
            
            if (!shouldSkipLog && Plugin.Config.DetailedTextureLog.Value)
            {
                Plugin.Log.LogInfo($"Loaded and cached custom texture: {textureName} ({texture.width}x{texture.height}) from {Path.GetExtension(filePath)}");
            }
            
            return texture;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Error loading texture {textureName}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Replace texture pixels in-place (preserves references)
    /// </summary>
    internal static bool ReplaceTextureInPlace(Texture2D originalTexture, string textureName)
    {
        if (originalTexture == null)
            return false;

        if (!texturePathIndex.TryGetValue(textureName, out string filePath))
            return false;

        try
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            
            if (!UnityEngine.ImageConversion.LoadImage(originalTexture, fileData))
            {
                Plugin.Log.LogError($"Failed to load image data into texture: {filePath}");
                return false;
            }

            originalTexture.filterMode = FilterMode.Bilinear;
            originalTexture.wrapMode = TextureWrapMode.Clamp;
            originalTexture.anisoLevel = 4;
            
            originalTexture.Apply(true, false);
            
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
    /// </summary>
    private static void BuildTextureIndex()
    {
        texturePathIndex.Clear();
        
        if (!Directory.Exists(customTexturesPath))
            return;

        // Try to load from manifest cache first
        if (TryLoadManifestIndex())
            return;

        string[] extensions = { "*.png", "*.jpg", "*.jpeg", "*.tga", "*.dds" };
        
        string modsFolder = Path.Combine(customTexturesPath, "00-Mods");
        bool hasModsFolder = Directory.Exists(modsFolder);
        
        string gsd1Folder = Path.Combine(customTexturesPath, "GSD1");
        bool hasGSD1Folder = Directory.Exists(gsd1Folder);
        
        string gsd2Folder = Path.Combine(customTexturesPath, "GSD2");
        bool hasGSD2Folder = Directory.Exists(gsd2Folder);
        
        // Phase 1: Scan base textures (all folders except 00-Mods, GSD1, GSD2)
        foreach (string extension in extensions)
        {
            string[] files = Directory.GetFiles(customTexturesPath, extension, SearchOption.AllDirectories);
            
            foreach (string filePath in files)
            {
                // Skip special folders
                if (hasModsFolder && filePath.StartsWith(modsFolder, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (hasGSD1Folder && filePath.StartsWith(gsd1Folder, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (hasGSD2Folder && filePath.StartsWith(gsd2Folder, StringComparison.OrdinalIgnoreCase))
                    continue;
                
                if (!TextureOptions.ShouldLoadTexture(filePath))
                    continue;
                
                string textureName = Path.GetFileNameWithoutExtension(filePath);
                
                if (!texturePathIndex.ContainsKey(textureName))
                {
                    texturePathIndex[textureName] = filePath;
                }
            }
        }
        
        
        // Phase 2: Scan game-specific folders (GSD1 and GSD2) - overrides base textures
        // We scan BOTH folders during initialization since we don't know which game will be played yet
        // The actual game-specific texture will be selected at runtime based on active scene
        
        if (hasGSD1Folder)
        {
            foreach (string extension in extensions)
            {
                string[] gsd1Files = Directory.GetFiles(gsd1Folder, extension, SearchOption.AllDirectories);
                
                foreach (string filePath in gsd1Files)
                {
                    if (!TextureOptions.ShouldLoadTexture(filePath))
                        continue;
                    
                    string textureName = Path.GetFileNameWithoutExtension(filePath);
                    
                    // Store with GSD1 prefix to track game-specific textures
                    string gsd1Key = $"GSD1:{textureName}";
                    texturePathIndex[gsd1Key] = filePath;
                    
                    // Also store without prefix for fallback (will be overridden by GSD2 if it exists)
                    texturePathIndex[textureName] = filePath;
                }
            }
        }
        
        if (hasGSD2Folder)
        {
            foreach (string extension in extensions)
            {
                string[] gsd2Files = Directory.GetFiles(gsd2Folder, extension, SearchOption.AllDirectories);
                
                foreach (string filePath in gsd2Files)
                {
                    if (!TextureOptions.ShouldLoadTexture(filePath))
                        continue;
                    
                    string textureName = Path.GetFileNameWithoutExtension(filePath);
                    
                    // Store with GSD2 prefix to track game-specific textures
                    string gsd2Key = $"GSD2:{textureName}";
                    texturePathIndex[gsd2Key] = filePath;
                    
                    // Also store without prefix (overrides GSD1 fallback)
                    texturePathIndex[textureName] = filePath;
                }
            }
        }
        
        // Phase 3: Scan 00-Mods folder (highest priority - overrides everything)
        if (hasModsFolder)
        {
            foreach (string extension in extensions)
            {
                string[] modFiles = Directory.GetFiles(modsFolder, extension, SearchOption.AllDirectories);
                
                foreach (string filePath in modFiles)
                {
                    if (!TextureOptions.ShouldLoadTexture(filePath))
                        continue;
                    
                    string textureName = Path.GetFileNameWithoutExtension(filePath);
                    texturePathIndex[textureName] = filePath; // Override
                }
            }
        }
        
        // Save manifest for next time
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

        Plugin.Log.LogInfo($"Custom textures directory: {customTexturesPath}");
        
        InitializeCaching();
        BuildTextureIndex();
        
        Plugin.Log.LogInfo($"Indexed {texturePathIndex.Count} custom texture(s) ready to use");
        
        // Preload bath and save point sprites
        PreloadBathSprites();
        PreloadSavePointSprites();
    }
    
    #endregion
}
