using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

namespace PKCore.Patches;

/// <summary>
/// Manages persistent custom textures that must survive scene transitions.
/// Preloads game-specific textures when a game scene (GSD1/GSD2) is detected
/// and ensures they remain in memory via DontDestroyOnLoad.
/// 
/// Add new textures to GSD1_PERSISTENT_TEXTURES or GSD2_PERSISTENT_TEXTURES as needed.
/// </summary>
[HarmonyPatch]
public static class CustomTexturePersist
{
    #region Configuration - Add new textures here
    
    /// <summary>
    /// Suikoden 1 (GSD1) textures that must persist across all scenes.
    /// Add texture names (without extension) to this list.
    /// </summary>
    private static readonly string[] GSD1_PERSISTENT_TEXTURES = new[]
    {
        "sactx-0-256x256-Uncompressed-shu_battle_00_atlas-de31d9b4",
        "sactx-0-256x256-Uncompressed-shu_field_00_atlas-3fe772bf",
        "sactx-0-256x256-Uncompressed-shu_field_01_atlas-959a6bf2",
        "sactx-0-256x256-Uncompressed-shu_field_01_atlas-959a6bf2_alt"
    };
    
    /// <summary>
    /// Suikoden 2 (GSD2) textures that must persist across all scenes.
    /// Add texture names (without extension) to this list.
    /// </summary>
    private static readonly string[] GSD2_PERSISTENT_TEXTURES = new[]
    {
        "sactx-0-256x128-Uncompressed-shu_02_atlas-ea00352f",
        "sactx-0-256x128-Uncompressed-shu_03_atlas-551f4eb1",
        "sactx-0-256x128-Uncompressed-shu_04_atlas-4bd4d6a6",
        "sactx-0-256x128-Uncompressed-shu_04_hd_atlas-bd7a8eea",
        "sactx-0-256x256-Uncompressed-shu_atlas-0db86654",
        "sactx-0-256x256-Uncompressed-shu_atlas-e7f71b8e",
        "sactx-0-512x256-Uncompressed-shu_01_atlas-fb0fe61c",
        "fp_129"  // Base portrait sprite for NPC portrait system
    };
    
    #endregion
    
    #region State
    
    /// <summary>
    /// Stores all preloaded persistent textures by name.
    /// These textures will NOT be destroyed on scene change.
    /// </summary>
    private static Dictionary<string, Texture2D> persistentTextureCache = new(StringComparer.OrdinalIgnoreCase);
    
    /// <summary>
    /// Tracks which game's textures have been preloaded to avoid redundant loading.
    /// </summary>
    private static string lastPreloadedGame = null;
    
    /// <summary>
    /// Whether the scene listener has been registered.
    /// </summary>
    private static bool isInitialized = false;
    
    #endregion
    
    #region Initialization
    
    /// <summary>
    /// Initialize the persistent texture system.
    /// Called from Plugin.Load() after CustomTexturePatch.Initialize().
    /// </summary>
    public static void Initialize()
    {
        if (isInitialized) return;
        
        // Listen for scene changes to preload game-specific textures
        SceneManager.sceneLoaded += (Action<Scene, LoadSceneMode>)OnSceneLoaded;
        isInitialized = true;
        
        Plugin.Log.LogInfo("[CustomTexturePersist] Initialized - will preload persistent textures on game scene load");
    }
    
    /// <summary>
    /// Called when a scene is loaded. Checks if we need to preload game textures.
    /// </summary>
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive)
            return;
        
        // Use centralized game detection instead of manual scene name checking
        string currentGame = GameDetection.GetCurrentGame();
        
        // Preload textures if we've switched to a different game
        if (currentGame == "GSD1" && lastPreloadedGame != "GSD1")
        {
            PreloadPersistentTextures("GSD1", GSD1_PERSISTENT_TEXTURES);
            lastPreloadedGame = "GSD1";
        }
        else if (currentGame == "GSD2" && lastPreloadedGame != "GSD2")
        {
            PreloadPersistentTextures("GSD2", GSD2_PERSISTENT_TEXTURES);
            lastPreloadedGame = "GSD2";
        }
    }
    
    #endregion
    
    #region Preloading
    
    /// <summary>
    /// Preload all textures for a specific game and mark them as persistent.
    /// </summary>
    private static void PreloadPersistentTextures(string gameId, string[] textureNames)
    {
        if (!Plugin.Config.EnableCustomTextures.Value)
            return;
        
        int loaded = 0;
        int skipped = 0;
        int notFound = 0;
        
        Plugin.Log.LogInfo($"[CustomTexturePersist] Preloading {textureNames.Length} persistent textures for {gameId}...");
        
        foreach (string textureName in textureNames)
        {
            // Skip if already loaded
            if (persistentTextureCache.ContainsKey(textureName))
            {
                skipped++;
                continue;
            }
            
            // Try to load with game-specific prefix first, then without
            string[] lookupNames = new[]
            {
                $"{gameId}:{textureName}",  // Game-specific: GSD2:sactx-...
                textureName                   // Fallback: sactx-...
            };
            
            Texture2D texture = null;
            string foundKey = null;
            
            foreach (string lookupName in lookupNames)
            {
                if (CustomTexturePatch.texturePathIndex.ContainsKey(lookupName))
                {
                    texture = CustomTexturePatch.LoadCustomTexture(lookupName);
                    if (texture != null)
                    {
                        foundKey = lookupName;
                        break;
                    }
                }
            }
            
            if (texture != null)
            {
                // Mark as persistent - Unity will NOT destroy this on scene change
                UnityEngine.Object.DontDestroyOnLoad(texture);
                
                // Cache in our persistent store
                persistentTextureCache[textureName] = texture;
                
                // Register with CustomTexturePatch's cache under MULTIPLE keys
                // This ensures the texture is found regardless of how the game requests it
                CustomTexturePatch.customTextureCache[textureName] = texture;
                CustomTexturePatch.customTextureCache[$"{gameId}:{textureName}"] = texture;
                
                // Register the texture name as a persistent pattern so it won't be cleared on scene change
                if (!CustomTexturePatch.persistentTextures.Contains(textureName))
                {
                    CustomTexturePatch.persistentTextures.Add(textureName);
                }
                
                if (Plugin.Config.DetailedLogs.Value)
                {
                    Plugin.Log.LogInfo($"  [✓] {textureName} ({texture.width}x{texture.height}) from {foundKey}");
                }
                loaded++;
            }
            else
            {
                if (Plugin.Config.DetailedLogs.Value)
                {
                    Plugin.Log.LogWarning($"  [✗] {textureName} not found in texture index");
                }
                notFound++;
            }
        }
        
        Plugin.Log.LogInfo($"[CustomTexturePersist] {gameId}: Loaded {loaded}, Skipped {skipped}, NotFound {notFound}");
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// Check if a texture is in our persistent cache.
    /// </summary>
    public static bool IsPersistent(string textureName)
    {
        return persistentTextureCache.ContainsKey(textureName);
    }
    
    /// <summary>
    /// Get a persistent texture by name, or null if not loaded.
    /// </summary>
    public static Texture2D GetPersistentTexture(string textureName)
    {
        persistentTextureCache.TryGetValue(textureName, out Texture2D texture);
        return texture;
    }
    
    /// <summary>
    /// Add a texture to the persistent cache programmatically.
    /// Use this for textures that need to be loaded at runtime.
    /// </summary>
    public static void AddPersistentTexture(string textureName, Texture2D texture)
    {
        if (texture == null || string.IsNullOrEmpty(textureName))
            return;
        
        UnityEngine.Object.DontDestroyOnLoad(texture);
        persistentTextureCache[textureName] = texture;
        CustomTexturePatch.customTextureCache[textureName] = texture;
        
        if (Plugin.Config.DetailedLogs.Value)
        {
            Plugin.Log.LogInfo($"[CustomTexturePersist] Added persistent texture: {textureName}");
        }
    }
    
    /// <summary>
    /// Get statistics about persistent textures.
    /// </summary>
    public static string GetStats()
    {
        return $"[CustomTexturePersist] {persistentTextureCache.Count} textures in persistent cache (Game: {lastPreloadedGame ?? "None"})";
    }
    
    /// <summary>
    /// Force preload textures for a specific game (can be called manually).
    /// </summary>
    public static void ForcePreload(string gameId)
    {
        switch (gameId.ToUpperInvariant())
        {
            case "GSD1":
                PreloadPersistentTextures("GSD1", GSD1_PERSISTENT_TEXTURES);
                lastPreloadedGame = "GSD1";
                break;
            case "GSD2":
                PreloadPersistentTextures("GSD2", GSD2_PERSISTENT_TEXTURES);
                lastPreloadedGame = "GSD2";
                break;
            default:
                Plugin.Log.LogWarning($"[CustomTexturePersist] Unknown game ID: {gameId}");
                break;
        }
    }
    
    #endregion
}
