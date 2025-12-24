using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

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
    /// </summary>
    private static void PreloadBathSprites()
    {
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
}

