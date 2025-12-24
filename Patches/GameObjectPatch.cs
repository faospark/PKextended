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
        
        
        // Check if this is a bgManagerHD object
        bool isBgManager = objectPath.Contains("bgManagerHD");
        
        // Handle bgManagerHD activation - scan for sprites to replace
        if (isBgManager)
        {
            if (Plugin.Config.DetailedTextureLog.Value)
            {
                Plugin.Log.LogInfo($"bgManagerHD activated: {objectPath}");
            }
            
            // Scan for SpriteRenderers and replace sprites (excluding save points, handled in SavePointPatch.cs)
            var spriteRenderers = __instance.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in spriteRenderers)
            {
                if (sr.sprite != null)
                {
                    string spriteName = sr.sprite.name;
                    
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
        }
    }
}
