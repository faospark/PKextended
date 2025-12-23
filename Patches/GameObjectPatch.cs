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
        
        // Check if this is a bath background or bgManagerHD object
        bool isBathBackground = objectPath.Contains("BathBG");
        bool isBgManager = objectPath.Contains("bgManagerHD");

        // Check if this is HDEffect or one of its children - trigger particle scan
        // Also check for M_GATE summon effect objects
        bool isHDEffect = __instance.name == "HDEffect" || __instance.transform.parent?.name == "HDEffect";
        bool isSummonEffect = __instance.name.StartsWith("M_GATE");
        
        if (isHDEffect || isSummonEffect)
        {
            // Trigger force replacement for known summon effect textures
            if (Plugin.Config.DetailedTextureLog.Value)
            {
                Plugin.Log.LogInfo($"HDEffect or summon effect activated: {objectPath}");
            }
            
            // Scan and replace particle effect textures
            ScanAndReplaceParticleEffectTextures();
        }
        
        // Handle bath background activation
        if (isBathBackground)
        {
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
        
        // Handle bgManagerHD activation - scan for sprites to replace
        if (isBgManager)
        {
            if (Plugin.Config.DetailedTextureLog.Value)
            {
                Plugin.Log.LogInfo($"bgManagerHD activated: {objectPath}");
            }
            
            // Scan for SpriteRenderers and replace sprites
            var spriteRenderers = __instance.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in spriteRenderers)
            {
                if (sr.sprite != null)
                {
                    string spriteName = sr.sprite.name;
                    
                    // DIAGNOSTIC: Always log save point sprites
                    bool isSavePoint = spriteName.Contains("savePoint", StringComparison.OrdinalIgnoreCase);
                    if (isSavePoint)
                    {
                        Plugin.Log.LogInfo($"[SavePoint GameObject] Found sprite: {spriteName} in {objectPath}");
                    }
                    
                    Sprite customSprite = LoadCustomSprite(spriteName, sr.sprite);
                    if (customSprite != null)
                    {
                        sr.sprite = customSprite;
                        
                        if (isSavePoint)
                        {
                            Plugin.Log.LogInfo($"[SavePoint GameObject] ✓ SET custom sprite: {spriteName}");
                        }
                        else
                        {
                            Plugin.Log.LogInfo($"Replaced sprite on activation: {spriteName} (from {objectPath})");
                        }
                    }
                    else if (isSavePoint)
                    {
                        Plugin.Log.LogWarning($"[SavePoint GameObject] ✗ LoadCustomSprite returned null for: {spriteName}");
                    }
                }
            }
        }
    }
}
