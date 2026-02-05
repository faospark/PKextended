using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;

namespace PKCore.Patches;

/// <summary>
/// Intelligent texture caching system that:
/// - Tracks textures per scene using file names and unique hashes
/// - Automatically clears unused textures when transitioning between areas
/// - Prevents memory bloat by only keeping textures relevant to current scene
/// - Preserves essential/persistent textures across scene changes
/// </summary>
[HarmonyPatch]
public static class TextureMemoryCachePatch
{
    #region State Management
    
    // Track which textures are currently loaded and which scene they belong to
    private static Dictionary<int, TextureMetadata> loadedTextureMap = new();
    private static Dictionary<string, int> filenameToTextureId = new();
    private static string currentSceneName = "";
    private static int sceneChangeCounter = 0;
    
    // Persistent texture patterns that should survive scene transitions
    private static HashSet<string> persistentPatterns = new()
    {
        "window_",
        "t_obj_savePoint",
        "menu",
        "ui",
        "dialog"
    };
    
    /// <summary>
    /// Metadata about a loaded texture for memory tracking
    /// </summary>
    private class TextureMetadata
    {
        public string Filename { get; set; }
        public string FilenameHash { get; set; }  // Hash of filename for quick comparison
        public string SceneName { get; set; }     // Scene where texture was first loaded
        public Texture2D TextureRef { get; set; }
        public int MemorySize { get; set; }       // Approximate memory size in bytes
        public long LoadTimestamp { get; set; }
        public int UsageCount { get; set; }       // How many objects reference this
        public bool IsPersistent { get; set; }    // Survives scene changes
    }
    
    #endregion

    #region Initialization & Monitoring
    
    /// <summary>
    /// Initialize the texture memory cache patch
    /// </summary>
    public static void Initialize()
    {
        // Cast to System.Action for IL2CPP compatibility
        SceneManager.sceneLoaded += (System.Action<Scene, LoadSceneMode>)OnSceneLoaded;
        currentSceneName = SceneManager.GetActiveScene().name;
        
        if (Plugin.Config.DetailedTextureLog.Value)
        {
            Plugin.Log.LogInfo("[TextureMemoryCache] Initialized - will track textures per scene");
        }
    }
    
    /// <summary>
    /// Called when a new scene is loaded
    /// </summary>
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PurgeUnusedTexturesOnSceneChange(currentSceneName, scene.name);
        currentSceneName = scene.name;
        sceneChangeCounter++;
        
        if (Plugin.Config.DetailedTextureLog.Value)
        {
            Plugin.Log.LogInfo($"[TextureMemoryCache] Scene loaded: {scene.name} (Change #{sceneChangeCounter})");
        }
    }
    
    #endregion

    #region Texture Registration & Tracking
    
    /// <summary>
    /// Register a texture as loaded (called when texture is created/loaded)
    /// </summary>
    public static void RegisterTexture(Texture2D texture, string sourceFilename)
    {
        if (texture == null) return;
        
        int textureId = texture.GetInstanceID();
        
        // Skip if already registered
        if (loadedTextureMap.ContainsKey(textureId))
        {
            loadedTextureMap[textureId].UsageCount++;
            return;
        }
        
        string hash = ComputeStringHash(sourceFilename);
        bool isPersistent = IsPersistentTexture(sourceFilename);
        int memoryEstimate = EstimateTextureMemory(texture);
        
        var metadata = new TextureMetadata
        {
            Filename = sourceFilename,
            FilenameHash = hash,
            SceneName = currentSceneName,
            TextureRef = texture,
            MemorySize = memoryEstimate,
            LoadTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            UsageCount = 1,
            IsPersistent = isPersistent
        };
        
        loadedTextureMap[textureId] = metadata;
        filenameToTextureId[sourceFilename] = textureId;
        
        if (Plugin.Config.DetailedTextureLog.Value)
        {
            Plugin.Log.LogDebug($"[TextureMemoryCache] Registered texture: {sourceFilename} " +
                $"({memoryEstimate / 1024}KB) in scene {currentSceneName}" +
                (isPersistent ? " [PERSISTENT]" : ""));
        }
    }
    
    /// <summary>
    /// Unregister a texture when it's destroyed
    /// </summary>
    public static void UnregisterTexture(Texture2D texture)
    {
        if (texture == null) return;
        
        int textureId = texture.GetInstanceID();
        if (loadedTextureMap.TryGetValue(textureId, out var metadata))
        {
            loadedTextureMap.Remove(textureId);
            filenameToTextureId.Remove(metadata.Filename);
            
            if (Plugin.Config.DetailedTextureLog.Value)
            {
                Plugin.Log.LogDebug($"[TextureMemoryCache] Unregistered texture: {metadata.Filename} " +
                    $"(freed {metadata.MemorySize / 1024}KB)");
            }
        }
    }
    
    #endregion

    #region Memory Management
    
    /// <summary>
    /// Clear textures that belong to a different scene
    /// Preserves persistent textures and textures still in use
    /// </summary>
    private static void PurgeUnusedTexturesOnSceneChange(string oldSceneName, string newSceneName)
    {
        if (string.IsNullOrEmpty(oldSceneName)) return;
        
        var texturesToRemove = new List<int>();
        long totalMemoryFreed = 0;
        int texturesCleared = 0;
        
        foreach (var kvp in loadedTextureMap)
        {
            int textureId = kvp.Key;
            TextureMetadata metadata = kvp.Value;
            
            // Keep persistent textures
            if (metadata.IsPersistent)
                continue;
            
            // Keep textures still needed in new scene
            if (IsTextureRelevantToScene(metadata.Filename, newSceneName))
                continue;
            
            // Keep textures still in use by renderers
            if (metadata.UsageCount > 0 && IsTextureStillActive(metadata.TextureRef))
                continue;
            
            // Mark for removal
            texturesToRemove.Add(textureId);
            totalMemoryFreed += metadata.MemorySize;
            texturesCleared++;
        }
        
        // Actually remove and destroy textures
        foreach (int textureId in texturesToRemove)
        {
            if (loadedTextureMap.TryGetValue(textureId, out var metadata))
            {
                if (metadata.TextureRef != null)
                {
                    UnityEngine.Object.Destroy(metadata.TextureRef);
                }
                loadedTextureMap.Remove(textureId);
                filenameToTextureId.Remove(metadata.Filename);
            }
        }
        
        if (Plugin.Config.DetailedTextureLog.Value && texturesCleared > 0)
        {
            Plugin.Log.LogInfo($"[TextureMemoryCache] Purged {texturesCleared} textures, " +
                $"freed {totalMemoryFreed / 1024 / 1024}MB memory");
        }
    }
    
    /// <summary>
    /// Force immediate cleanup of a specific texture
    /// </summary>
    public static void ForceCleanupTexture(string filename)
    {
        if (filenameToTextureId.TryGetValue(filename, out int textureId))
        {
            if (loadedTextureMap.TryGetValue(textureId, out var metadata))
            {
                if (metadata.TextureRef != null)
                {
                    UnityEngine.Object.Destroy(metadata.TextureRef);
                }
                loadedTextureMap.Remove(textureId);
                filenameToTextureId.Remove(filename);
                
                Plugin.Log.LogDebug($"[TextureMemoryCache] Force cleaned: {filename}");
            }
        }
    }
    
    /// <summary>
    /// Get current memory usage of all loaded textures
    /// </summary>
    public static long GetTotalMemoryUsage()
    {
        return loadedTextureMap.Values.Sum(m => m.MemorySize);
    }
    
    /// <summary>
    /// Get statistics about cached textures
    /// </summary>
    public static string GetCacheStats()
    {
        int totalTextures = loadedTextureMap.Count;
        int persistentTextures = loadedTextureMap.Values.Count(m => m.IsPersistent);
        long totalMemory = GetTotalMemoryUsage();
        
        return $"[TextureMemoryCache] {totalTextures} textures loaded " +
            $"({persistentTextures} persistent, {totalTextures - persistentTextures} scene-specific) " +
            $"using {totalMemory / 1024 / 1024}MB";
    }
    
    #endregion

    #region Helper Methods
    
    /// <summary>
    /// Check if texture filename matches persistent patterns
    /// </summary>
    private static bool IsPersistentTexture(string filename)
    {
        if (string.IsNullOrEmpty(filename)) return false;
        
        foreach (var pattern in persistentPatterns)
        {
            if (filename.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Check if texture is still referenced by active objects
    /// </summary>
    private static bool IsTextureStillActive(Texture2D texture)
    {
        if (texture == null) return false;
        
        // Check if texture is still assigned to renderers
        var renderers = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        foreach (var renderer in renderers)
        {
            if (renderer != null && renderer.sprite != null && renderer.sprite.texture == texture)
                return true;
        }
        
        // Check UI Images
        var images = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Image>(FindObjectsSortMode.None);
        foreach (var image in images)
        {
            if (image != null && image.sprite != null && image.sprite.texture == texture)
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Determine if a texture is relevant to the target scene
    /// based on filename patterns
    /// </summary>
    private static bool IsTextureRelevantToScene(string filename, string sceneName)
    {
        if (string.IsNullOrEmpty(filename) || string.IsNullOrEmpty(sceneName))
            return false;
        
        // Map scene names to texture prefixes
        var sceneTextureMap = new Dictionary<string, string[]>
        {
            { "GSD1", new[] { "m_", "t_", "sactx", "portrait" } },
            { "GSD2", new[] { "m_", "t_", "sactx", "portrait", "s2_" } },
            { "Main", new[] { "menu", "ui", "window", "button" } },
        };
        
        // Check if scene name pattern matches filename
        foreach (var kvp in sceneTextureMap)
        {
            if (sceneName.Contains(kvp.Key))
            {
                foreach (var prefix in kvp.Value)
                {
                    if (filename.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Compute a quick hash of filename for comparison
    /// </summary>
    private static string ComputeStringHash(string input)
    {
        if (string.IsNullOrEmpty(input)) return "0";
        
        unchecked
        {
            int hash = 17;
            foreach (char c in input)
            {
                hash = hash * 31 + c.GetHashCode();
            }
            return hash.ToString();
        }
    }
    
    /// <summary>
    /// Estimate memory size of a texture
    /// </summary>
    private static int EstimateTextureMemory(Texture2D texture)
    {
        if (texture == null) return 0;
        
        int width = texture.width;
        int height = texture.height;
        int bytesPerPixel = 4; // Assume RGBA32 or similar
        
        // Include mip maps
        int mipLevels = Mathf.Max(1, texture.mipmapCount);
        int estimate = width * height * bytesPerPixel;
        
        // Add ~33% for mipmaps
        estimate = (int)(estimate * 1.33f);
        
        return estimate;
    }
    
    #endregion

    #region Integration with Custom Texture Patch
    
    /// <summary>
    /// Hook this into CustomTexturePatch.LoadCustomTexture to track loaded textures
    /// </summary>
    public static Texture2D TrackLoadedTexture(Texture2D texture, string sourceFilename)
    {
        if (Plugin.Config.EnableMemoryCaching.Value && texture != null)
        {
            RegisterTexture(texture, sourceFilename);
        }
        return texture;
    }
    
    /// <summary>
    /// Clean up all cached textures (called on shutdown)
    /// </summary>
    public static void CleanupAll()
    {
        foreach (var metadata in loadedTextureMap.Values)
        {
            if (metadata.TextureRef != null)
            {
                UnityEngine.Object.Destroy(metadata.TextureRef);
            }
        }
        loadedTextureMap.Clear();
        filenameToTextureId.Clear();
        
        Plugin.Log.LogInfo("[TextureMemoryCache] Cleaned up all cached textures");
    }
    
    #endregion
}
