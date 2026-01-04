using HarmonyLib;
using UnityEngine;
using System;

namespace PKCore.Patches;

/// <summary>
/// Patches for GameObject.SetActive
/// Handles detection and replacement of textures when GameObjects are activated
/// </summary>
public partial class CustomTexturePatch
{
    /// <summary>
    /// Intercept GameObject.SetActive to catch sprites when objects are activated
    /// This catches sprites in objects that are instantiated/activated after scene load
    /// Specifically handles bath backgrounds and bgManagerHD objects
    /// </summary>
    [HarmonyPatch(typeof(GameObject), nameof(GameObject.SetActive))]
    [HarmonyPostfix]
    public static void GameObject_SetActive_Postfix(GameObject __instance, bool value)
    {
        // Only scan when activating (not deactivating)
        if (!value || !Plugin.Config.EnableCustomTextures.Value && !Plugin.Config.LogReplaceableTextures.Value)
            return;

        // Get the full path of the activated object
        string objectPath = GetGameObjectPath(__instance);
        
        // DEBUG: Log all activations to see what we're missing
        if (__instance.name.Contains("dragon", StringComparison.OrdinalIgnoreCase) || 
            __instance.name.Contains("ushi", StringComparison.OrdinalIgnoreCase) ||
            __instance.name.Contains("MapBackGround", StringComparison.OrdinalIgnoreCase))
        {
            Plugin.Log.LogInfo($"[DEBUG] GameObject activated: {objectPath}");
        }
        
        // Check if this is a background manager object
        // Suikoden 2: bgManagerHD
        // Suikoden 1: MapBackGround
        // Also scan 3D objects (contains FieldObject MeshRenderers) HDEffect
        bool isBgManager = objectPath.Contains("bgManagerHD") || objectPath.Contains("MapBackGround") || objectPath.Contains("3D") || objectPath.Contains("HDEffect") || objectPath.Contains("HDFishingBG");
        
        // Handle background manager activation - scan for sprites to replace
        if (isBgManager)
        {
            if (Plugin.Config.DetailedTextureLog.Value)
            {
                Plugin.Log.LogInfo($"Background manager activated: {objectPath}");
            }
            
            // Scan for SpriteRenderers (Unity standard)
            var spriteRenderers = __instance.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in spriteRenderers)
            {
                if (sr.sprite != null)
                {
                    string spriteName = sr.sprite.name;
                    
                    // Check and attach Dragon monitor if applicable
                    DragonPatch.CheckAndAttachMonitor(sr.gameObject);
                    
                    // Check and attach Cow monitor if applicable
                    CowTexturePatch.CheckAndAttachMonitor(sr.gameObject);
                    
                    // Skip save point sprites - they're handled in SavePointPatch.cs
                    if (spriteName.Contains("savePoint", StringComparison.OrdinalIgnoreCase))
                        continue;
                    
                    Sprite customSprite = LoadCustomSprite(spriteName, sr.sprite);
                    if (customSprite != null)
                    {
                        sr.sprite = customSprite;
                        Plugin.Log.LogInfo($"Replaced sprite on activation: {spriteName} (from {objectPath})");
                    }
                }
            }
            
            // Scan for GRSpriteRenderers (game's custom renderer - used in S1)
            var grSpriteRenderers = __instance.GetComponentsInChildren<GRSpriteRenderer>(true);
            foreach (var gr in grSpriteRenderers)
            {
                if (gr.sprite != null)
                {
                    string spriteName = gr.sprite.name;
                    
                    Plugin.Log.LogInfo($"[DEBUG] Found GRSpriteRenderer: {spriteName} on {gr.gameObject.name}");
                    
                    // Check and attach Dragon monitor if applicable
                    DragonPatch.CheckAndAttachMonitor(gr.gameObject);
                    
                    // Check and attach Cow monitor if applicable
                    CowTexturePatch.CheckAndAttachMonitor(gr.gameObject);
                }
            }
        }
    }
}
