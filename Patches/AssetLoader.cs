using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace PKCore.Patches;

/// <summary>
/// Centralized service for asynchronous asset loading.
/// Unifies file hierarchy (00-Mods > GamePrefix > Root) and background IO.
/// </summary>
public static class AssetLoader
{
    private static readonly ConcurrentQueue<Action> mainThreadQueue = new();
    private static readonly Dictionary<string, string> pathCache = new(StringComparer.OrdinalIgnoreCase);
    private static string texturesRoot;

    public static void Initialize()
    {
        texturesRoot = Path.Combine(BepInEx.Paths.GameRootPath, "PKCore", "Textures");
        
        pathCache.Clear();
    }

    /// <summary>
    /// Processes queued actions on the main thread.
    /// Should be called from Plugin.Update or similar.
    /// </summary>
    public static void Update()
    {
        while (mainThreadQueue.TryDequeue(out var action))
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[AssetLoader] Main thread error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Run an action on the Unity main thread.
    /// </summary>
    public static Task RunOnMainThread(Action action)
    {
        var tcs = new TaskCompletionSource<bool>();
        mainThreadQueue.Enqueue(() =>
        {
            try
            {
                action();
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }

    /// <summary>
    /// Run a function on the Unity main thread and get its result.
    /// </summary>
    public static Task<T> RunOnMainThread<T>(Func<T> func)
    {
        var tcs = new TaskCompletionSource<T>();
        mainThreadQueue.Enqueue(() =>
        {
            try
            {
                tcs.SetResult(func());
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }

    /// <summary>
    /// Resolves an asset path based on the hierarchy rules.
    /// Prioritizes .dds over .png and 00-Mods over specific folders.
    /// </summary>
    public static string ResolvePath(string assetName)
    {
        if (pathCache.TryGetValue(assetName, out string cachedPath))
            return cachedPath;

        // Note: In a real implementation, we would use the indexed dictionary 
        // from CustomTexturePatch here, but for now we'll do a fresh resolution
        // to maintain independence.
        
        // This is a simplified version - in practice, we should use the 
        // global texturePathIndex already built by CustomTexturePatch.
        return null; 
    }

    /// <summary>
    /// Load a texture asynchronously.
    /// </summary>
    public static async Task<Texture2D> LoadTextureAsync(string assetName, string context = null)
    {
        // 1. Resolve Path from the global index
        if (!CustomTexturePatch.texturePathIndex.TryGetValue(assetName, out string filePath))
        {
            return null;
        }

        try
        {
            // 2. Read file in background
            byte[] fileData = await Task.Run(() => File.ReadAllBytes(filePath));

            // 3. Process on main thread
            return await RunOnMainThread(() => LoadTextureFromBytes(fileData, assetName, filePath, context));
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[AssetLoader] Failed to load {assetName}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Load a texture synchronously (use only when async is not possible, e.g. Harmony prefix).
    /// </summary>
    public static Texture2D LoadTextureSync(string assetName, string context = null)
    {
        if (!CustomTexturePatch.texturePathIndex.TryGetValue(assetName, out string filePath))
            return null;

        try
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            return LoadTextureFromBytes(fileData, assetName, filePath, context);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[AssetLoader] Sync load failed for {assetName}: {ex.Message}");
            return null;
        }
    }

    private static Texture2D LoadTextureFromBytes(byte[] fileData, string assetName, string filePath, string context)
    {
        if (filePath.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
        {
            return DDSLoader.LoadDDSFromBytes(fileData, assetName);
        }
        else
        {
            Texture2D texture = new(2, 2, TextureFormat.RGBA32, true);
            if (ImageConversion.LoadImage(texture, fileData))
            {
                texture.name = assetName + (context != null ? $"_{context}" : "");
                
                bool isWindowUI = CustomTexturePatch.IsWindowUITexture(assetName, filePath);
                bool isMap = filePath.Contains("Maps", StringComparison.OrdinalIgnoreCase);
                bool useBilinear = Plugin.Config.SpriteFilteringQuality.Value || !isMap;

                texture.filterMode = (isWindowUI || (isMap && !useBilinear)) ? FilterMode.Point : FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.anisoLevel = (isWindowUI || (isMap && !useBilinear)) ? 0 : 4;
                
                texture.Apply(false, false);
                
                UnityEngine.Object.DontDestroyOnLoad(texture);
                return texture;
            }
            UnityEngine.Object.Destroy(texture);
            return null;
        }
    }

    /// <summary>
    /// Read a JSON file asynchronously and deserialize it.
    /// </summary>
    public static async Task<T> LoadJsonAsync<T>(string filePath)
    {
        if (!File.Exists(filePath)) return default;

        try
        {
            string json = await Task.Run(() => File.ReadAllText(filePath));
            return System.Text.Json.JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[AssetLoader] JSON error in {Path.GetFileName(filePath)}: {ex.Message}");
            return default;
        }
    }
}
