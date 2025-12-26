using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace PKCore.Patches;

/// <summary>
/// Patches for bath background texture replacement
/// Handles HDBath component patches and bath sprite preloading
/// </summary>
public partial class CustomTexturePatch
{
    /// <summary>
    /// Patch HDBath.LoadInstance to detect when bath backgrounds are loaded
    /// This catches the in-game bath background switcher
    /// </summary>
    [HarmonyPatch(typeof(HDBath), "LoadInstance")]
    [HarmonyPostfix]
    public static void HDBath_LoadInstance_Postfix(HDBath __instance)
    {
        if (!Plugin.Config.EnableCustomTextures.Value)
            return;

        Plugin.Log.LogInfo("HDBath.LoadInstance called - bath background is being loaded");
        
        // Try to replace bath sprites immediately
        int replaced = TryReplaceBathSprites();
        if (replaced > 0)
        {
            Plugin.Log.LogInfo($"Replaced {replaced} bath sprite(s) in LoadInstance");
        }
    }

    /// <summary>
    /// Patch HDBath.Start to replace bath sprites after HDBath component is fully initialized
    /// This is the key patch for in-game bath switching - Start() is called AFTER the GameObject is instantiated
    /// </summary>
    [HarmonyPatch(typeof(HDBath), "Start")]
    [HarmonyPostfix]
    public static void HDBath_Start_Postfix(HDBath __instance)
    {
        if (!Plugin.Config.EnableCustomTextures.Value)
            return;

        Plugin.Log.LogInfo("HDBath.Start called - scanning for bath sprites");
        ScanAndReplaceBathSprites(__instance);
    }

    /// <summary>
    /// Scans for bath sprites on the HDBath instance and replaces with custom textures
    /// </summary>
    private static void ScanAndReplaceBathSprites(HDBath bathInstance)
    {
        if (bathInstance == null)
            return;

        // Get all SpriteRenderers in the bath GameObject
        var spriteRenderers = bathInstance.GetComponentsInChildren<SpriteRenderer>(true);
        
        int replaced = 0;
        foreach (var sr in spriteRenderers)
        {
            if (sr.sprite != null)
            {
                string spriteName = sr.sprite.name;
                
                // Check if this is a bath sprite (bath_1 through bath_5)
                if (spriteName.StartsWith("bath_"))
                {
                    Plugin.Log.LogInfo($"Found bath sprite: {spriteName}");
                    
                    // Try to replace with custom sprite
                    Sprite customSprite = LoadCustomSprite(spriteName, sr.sprite);
                    if (customSprite != null)
                    {
                        sr.sprite = customSprite;
                        Plugin.Log.LogInfo($"Replaced bath sprite: {spriteName}");
                        replaced++;
                    }
                    else
                    {
                        Plugin.Log.LogInfo($"No custom texture found for: {spriteName}");
                    }
                }
            }
        }
        
        // Force replace known bath decoration atlas textures
        // These are loaded before the bath scene, so we need to replace them in-place
        replaced += ForceReplaceBathDecorationAtlases();
        
        if (replaced > 0)
        {
            Plugin.Log.LogInfo($"Bath sprite scan complete: replaced {replaced} sprite(s)");
        }
        else
        {
            Plugin.Log.LogInfo("Bath sprite scan complete: no sprites replaced");
        }
    }
    
    /// <summary>
    /// Force replace known bath decoration atlas textures that are already loaded
    /// </summary>
    private static int ForceReplaceBathDecorationAtlases()
    {
        int replaced = 0;
        
        // Find all atlas textures in the index that match bath decoration patterns
        foreach (var kvp in texturePathIndex)
        {
            string textureName = kvp.Key;
            
            // Check if this is a bath decoration atlas (hp_book, hp_furo, etc.)
            if (textureName.Contains("hp_book") || textureName.Contains("hp_furo"))
            {
                // Find all loaded textures with this name
                var loadedTextures = Resources.FindObjectsOfTypeAll<Texture2D>();
                foreach (var loadedTex in loadedTextures)
                {
                    if (loadedTex.name == textureName)
                    {
                        if (Plugin.Config.DetailedTextureLog.Value)
                        {
                            Plugin.Log.LogInfo($"[Bath Atlas] Found loaded atlas: {textureName}, replacing...");
                        }
                        
                        bool atlasReplaced = ReplaceTextureInPlace(loadedTex, textureName);
                        
                        if (atlasReplaced)
                        {
                            Plugin.Log.LogInfo($"[Bath Atlas] ✓ Replaced: {textureName}");
                            replaced++;
                        }
                    }
                }
            }
        }
        
        return replaced;
    }

    /// <summary>
    /// Patch HDBath.OnDestroy to detect when bath is destroyed
    /// </summary>
    [HarmonyPatch(typeof(HDBath), "OnDestroy")]
    [HarmonyPrefix]
    public static void HDBath_OnDestroy_Prefix(HDBath __instance)
    {
        Plugin.Log.LogInfo("HDBath.OnDestroy called - bath is being destroyed");
        
        // Reset the last bath BG instance ID so next bath will be treated as new
        lastBathBGInstanceID = -1;
        
        // Reset bath background scan attempts
        bathBackgroundScanAttempts = 0;
    }

    /// <summary>
    /// Preload bath sprites (bath_1 through bath_5) for instant replacement
    /// This ensures bath backgrounds are replaced immediately when the bath scene loads
    /// Only preloads if in GSD2 scene
    /// </summary>
    public static void PreloadBathSprites()
    {
        // Only preload for Suikoden 2 (GSD2)
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "GSD2")
            return;
            
        int preloaded = 0;
        
        // Preload bath_1 through bath_5
        for (int i = 1; i <= 5; i++)
        {
            string bathName = $"bath_{i}";
            
            if (texturePathIndex.ContainsKey(bathName))
            {
                Texture2D texture = LoadCustomTexture(bathName);
                if (texture != null)
                {
                    // Create a sprite from the texture
                    // Bath backgrounds are full-screen 1920x1080
                    Sprite sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f), // Center pivot
                        100f, // Default pixels per unit
                        0,
                        SpriteMeshType.FullRect
                    );
                    
                    // Prevent Unity from destroying this sprite when changing scenes
                    UnityEngine.Object.DontDestroyOnLoad(sprite);
                    UnityEngine.Object.DontDestroyOnLoad(texture);
                    
                    // Cache the sprite for instant replacement
                    preloadedBathSprites[bathName] = sprite;
                    
                    Plugin.Log.LogInfo($"  Preloaded: {bathName} ({texture.width}x{texture.height})");
                    preloaded++;
                }
            }
        }

        if (preloaded > 0)
        {
            Plugin.Log.LogInfo($"Preloaded {preloaded} bath sprite(s) for instant replacement");
        }
    }

    /// <summary>
    /// Intercept GameObject.SetActive to detect when BathBG objects are activated
    /// This handles bath background activation during scene transitions
    /// </summary>
    [HarmonyPatch(typeof(GameObject), nameof(GameObject.SetActive))]
    [HarmonyPostfix]
    public static void GameObject_SetActive_BathBG_Postfix(GameObject __instance, bool value)
    {
        // Only scan when activating
        if (!value || !Plugin.Config.EnableCustomTextures.Value)
            return;

        // Check if this is a BathBG object
        string objectPath = GetGameObjectPath(__instance);
        if (!objectPath.Contains("BathBG"))
            return;

        if (Plugin.Config.DetailedTextureLog.Value)
        {
            Plugin.Log.LogInfo($"BathBG activated: {objectPath}");
        }
        
        // Try to replace bath sprites
        int replaced = TryReplaceBathSprites();
        if (replaced > 0 && Plugin.Config.DetailedTextureLog.Value)
        {
            Plugin.Log.LogInfo($"Replaced {replaced} bath sprite(s) on activation");
        }
    }
    
    /// <summary>
    /// Patch BGManagerHD.Load to detect when entering areas near the bath
    /// Preload bath sprites for performance when entering these areas (in close proximity to bath):
    /// - vk06_01: Area near bath (triggers preload)
    /// - vk06_00: Area near bath (triggers preload)
    /// - vk02_00: Area near bath (triggers preload)
    /// This ensures sprites are ready when player actually enters the bath
    /// </summary>
    [HarmonyPatch]
    public static class BGManagerHD_Load_Patch
    {
        private static bool _patched = false;
        private static System.Reflection.MethodInfo _targetMethod;
        
        /// <summary>
        /// Dynamically find and patch BGManagerHD.Load method
        /// </summary>
        public static void Initialize(Harmony harmony)
        {
            if (_patched)
                return;
                
            try
            {
                // Find BGManagerHD type
                var bgManagerType = FindBGManagerType();
                if (bgManagerType == null)
                {
                    Plugin.Log.LogWarning("[BathTexturePatch] Could not find BGManagerHD type for bath preloading");
                    return;
                }
                
                // Find Load method
                _targetMethod = bgManagerType.GetMethod("Load", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (_targetMethod == null)
                {
                    Plugin.Log.LogWarning("[BathTexturePatch] Could not find BGManagerHD.Load method");
                    return;
                }
                
                // Patch it
                var postfix = typeof(BGManagerHD_Load_Patch).GetMethod(nameof(Load_Postfix), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                harmony.Patch(_targetMethod, postfix: new HarmonyMethod(postfix));
                
                _patched = true;
                Plugin.Log.LogInfo("[BathTexturePatch] ✓ Patched BGManagerHD.Load for bath sprite preloading");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning($"[BathTexturePatch] Failed to patch BGManagerHD.Load: {ex.Message}");
            }
        }
        
        private static System.Type FindBGManagerType()
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var assemblyName = assembly.GetName().Name;
                    if (assemblyName == "GSD2")
                    {
                        var types = assembly.GetTypes();
                        var bgManagerType = types.FirstOrDefault(t => 
                            t.Name == "MapBGManagerHD" || 
                            t.Name == "bgManagerHD" ||
                            t.Name.Contains("MapBGManager")
                        );
                        
                        if (bgManagerType != null)
                            return bgManagerType;
                    }
                }
                catch { }
            }
            return null;
        }
        
        public static System.Collections.IEnumerator Load_Postfix(System.Collections.IEnumerator __result, object __instance)
        {
            // Let original Load complete
            while (__result.MoveNext())
            {
                yield return __result.Current;
            }
            
            // Check if we're in a bath-related area
            if (!Plugin.Config.EnableCustomTextures.Value)
                yield break;
                
            try
            {
                var managerType = __instance.GetType();
                var assetProp = managerType.GetProperty("asset");
                var asset = assetProp?.GetValue(__instance) as GameObject;
                
                if (asset != null)
                {
                    string areaName = asset.name;
                    
                    // Check if this is an area near the bath (preload for performance)
                    if (areaName == "vk06_01" || areaName == "vk06_00" || areaName == "vk02_00")
                    {
                        if (Plugin.Config.DetailedTextureLog.Value)
                        {
                            Plugin.Log.LogInfo($"[BathTexturePatch] Entering area near bath: {areaName}, preloading bath sprites...");
                        }
                        
                        PreloadBathSprites();
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning($"[BathTexturePatch] Error in BGManagerHD.Load postfix: {ex.Message}");
            }
        }
    }
}

