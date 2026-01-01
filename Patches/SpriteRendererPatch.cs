using HarmonyLib;
using UnityEngine;
using System;

namespace PKCore.Patches;

/// <summary>
/// Patches for Unity's SpriteRenderer component
/// Handles sprite setter and OnEnable to replace sprites with custom textures
/// </summary>
public partial class CustomTexturePatch
{
    [HarmonyPatch(typeof(SpriteRenderer), nameof(SpriteRenderer.sprite), MethodType.Setter)]
    [HarmonyPrefix]
    public static void SpriteRenderer_set_sprite_Prefix(SpriteRenderer __instance, ref Sprite value)
    {
        if (value == null || !Plugin.Config.EnableCustomTextures.Value)
            return;

        string originalName = value.name;
        
        // DIAGNOSTIC: Log save point sprites
        bool isSavePoint = originalName.Contains("savePoint", StringComparison.OrdinalIgnoreCase);
        if (isSavePoint)
        {
            Plugin.Log.LogInfo($"[SavePoint SETTER] SpriteRenderer.sprite setter called for: {originalName}");
            if (value.texture != null)
            {
                Plugin.Log.LogInfo($"[SavePoint SETTER]   Original texture: {value.texture.name} ({value.texture.width}x{value.texture.height})");
            }
        }
        
        // Get object path for context
        string objectPath = GetGameObjectPath(__instance.gameObject);
        bool isBathBackground = objectPath.Contains("BathBG");
        bool isBgManager = objectPath.Contains("bgManagerHD") || objectPath.Contains("MapBackGround");
        
        // CRITICAL: Skip custom objects created by CustomObjectInsertion
        // They handle their own texture loading and we don't want to interfere
        // Check if this is under the "object" folder in bgManagerHD hierarchy
        if (objectPath.Contains("/object/") && isBgManager)
        {
            // Let custom objects handle their own sprites (silent skip to reduce log spam)
            return;
        }
        
        // Handle bath background instance tracking
        if (isBathBackground && originalName.StartsWith("bath_"))
        {
            Plugin.Log.LogInfo($"Sprite setter called for bath sprite: {originalName} (path: {objectPath})");
            
            var bathBG = GameObject.Find("AppRoot/BathBG");
            if (bathBG != null)
            {
                int currentInstanceID = bathBG.GetInstanceID();
                if (currentInstanceID != lastBathBGInstanceID)
                {
                    Plugin.Log.LogInfo($"New BathBG instance detected via sprite setter (ID: {currentInstanceID}, previous: {lastBathBGInstanceID})");
                    lastBathBGInstanceID = currentInstanceID;
                }
            }
        }
        
        // Log replaceable textures if enabled (only once per texture)
        if (Plugin.Config.LogReplaceableTextures.Value && !loggedTextures.Contains(originalName))
        {
            loggedTextures.Add(originalName);
            if (isBathBackground || isBgManager)
            {
                Plugin.Log.LogInfo($"[Replaceable Sprite] {originalName} (from {objectPath})");
            }
            else
            {
                Plugin.Log.LogInfo($"[Replaceable Sprite] {originalName}");
            }
        }
        
        // UNIVERSAL FALLBACK: Try to replace ANY sprite with custom texture
        Sprite customSprite = LoadCustomSprite(originalName, value);
        if (customSprite != null)
        {
            // Skip logging for sactx and character sprites to reduce spam
            bool shouldSkipReplacementLog = originalName.StartsWith("sactx");
            if (!shouldSkipReplacementLog && texturePathIndex.TryGetValue(originalName, out string replacementTexPath))
            {
                shouldSkipReplacementLog = replacementTexPath.ToLower().Contains("characters");
            }
            
            if (!shouldSkipReplacementLog)
            {
                if (isSavePoint)
                {
                    Plugin.Log.LogInfo($"[SavePoint SETTER] ✓ Replaced {originalName} with custom sprite");
                }
                else if (isBathBackground)
                {
                    Plugin.Log.LogInfo($"Replaced bath sprite via setter: {originalName}");
                }
                else if (isBgManager)
                {
                    Plugin.Log.LogInfo($"Replaced sprite: {originalName} (from {objectPath})");
                }
                else
                {
                    Plugin.Log.LogInfo($"Replaced sprite: {originalName}");
                }
            }
            value = customSprite;
        }
        else if (isSavePoint)
        {
            Plugin.Log.LogWarning($"[SavePoint SETTER] ✗ LoadCustomSprite returned null for {originalName}");
        }
    }
}
