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
    /// <summary>
    /// Intercept SpriteRenderer.sprite setter to replace with custom textures
    /// </summary>
    [HarmonyPatch(typeof(SpriteRenderer), nameof(SpriteRenderer.sprite), MethodType.Setter)]
    [HarmonyPrefix]
    public static void SpriteRenderer_set_sprite_Prefix(SpriteRenderer __instance, ref Sprite value)
    {
        if (value == null)
            return;

        string originalName = value.name;
        
        // NEW: Ignore atlas (sactx) sprites immediately.
        // These are handled by SpriteAtlasPatch which intercepts Sprite.texture getter.
        // Replacing them here blindly causes "Mesh.uv out of bounds" errors.
        if (originalName.StartsWith("sactx-")) 
            return;
        
        // DIAGNOSTIC: ALWAYS log save point sprites to see if Animator calls this
        bool isSavePoint = originalName.Contains("savePoint", StringComparison.OrdinalIgnoreCase);
        if (isSavePoint)
        {
            Plugin.Log.LogInfo($"[SavePoint SETTER] SpriteRenderer.sprite setter called for: {originalName}");
            if (value.texture != null)
            {
                Plugin.Log.LogInfo($"[SavePoint SETTER]   Original texture: {value.texture.name} ({value.texture.width}x{value.texture.height})");
            }
            
            // Try to replace with custom sprite
            Sprite customSprite = LoadCustomSprite(originalName, value);
            if (customSprite != null)
            {
                // Replacing save point sprite
                value = customSprite;
                return; // Early return - sprite replaced
            }
            else
            {
                Plugin.Log.LogWarning($"[SavePoint SETTER] ✗ LoadCustomSprite returned null for {originalName}");
            }
        }
        
        // Get object path for context
        string objectPath = GetGameObjectPath(__instance.gameObject);
        bool isBathBackground = objectPath.Contains("BathBG");
        bool isBgManager = objectPath.Contains("bgManagerHD");
        
        // If this is a bath sprite being assigned, check if it's a new BathBG instance
        if (isBathBackground && originalName.StartsWith("bath_"))
        {
            Plugin.Log.LogInfo($"[SpriteRendererPatch] Sprite setter called for bath sprite: {originalName} (path: {objectPath})");
            
            if (Plugin.Config.EnableCustomTextures.Value)
            {
                var bathBG = GameObject.Find("AppRoot/BathBG");
                if (bathBG != null)
                {
                    int currentInstanceID = bathBG.GetInstanceID();
                    if (currentInstanceID != lastBathBGInstanceID)
                    {
                        Plugin.Log.LogInfo($"[SpriteRendererPatch] New BathBG instance detected via sprite setter (ID: {currentInstanceID}, previous: {lastBathBGInstanceID})");
                        lastBathBGInstanceID = currentInstanceID;
                    }
                    
                    // Replace this bath sprite with custom texture
                    if (texturePathIndex.ContainsKey(originalName))
                    {
                        Sprite customSprite = LoadCustomSprite(originalName, value);
                        if (customSprite != null)
                        {
                            Plugin.Log.LogInfo($"[SpriteRendererPatch] Replaced bath sprite via setter: {originalName}");
                            value = customSprite;
                            return; // Early return - we've replaced the sprite
                        }
                    }
                    else
                    {
                        Plugin.Log.LogInfo($"[SpriteRendererPatch] Bath sprite {originalName} not found in texture index");
                    }
                }
                else
                {
                    Plugin.Log.LogInfo("[SpriteRendererPatch] BathBG not found when trying to replace sprite");
                }
            }
        }
        
        // Log replaceable textures if enabled (only once per texture)
        if (Plugin.Config.LogReplaceableTextures.Value && !loggedTextures.Contains(originalName))
        {
            string context = null;
            if (isBathBackground || isBgManager)
            {
                context = $"from {objectPath}";
            }
            
            LogReplaceableTexture(originalName, "Sprite", context);
        }
        
        // Try to load custom sprite replacement
        if (Plugin.Config.EnableCustomTextures.Value)
        {


            Sprite customSprite = LoadCustomSprite(originalName, value);
            if (customSprite != null)
            {
                // Skip logging for character sprites and portraits to reduce spam
                bool shouldSkipReplacementLog = originalName.StartsWith("fp_");
                if (!shouldSkipReplacementLog && texturePathIndex.TryGetValue(originalName, out string replacementTexPath))
                {
                    shouldSkipReplacementLog = replacementTexPath.ToLower().Contains("characters");
                }
                
                if (!shouldSkipReplacementLog)
                {
                    if (Plugin.Config.LogReplaceableTextures.Value)
                    {
                        // Include GameObject path in detailed logs or if explicitly enabled
                        Plugin.Log.LogInfo($"[SpriteRendererPatch] Replaced sprite: {originalName} (from {objectPath})");
                    }
                    else if (isBathBackground || isBgManager)
                    {
                        // Show path only for bath/bgManager sprites in normal mode
                        Plugin.Log.LogInfo($"[SpriteRendererPatch] Replaced sprite: {originalName} (from {objectPath})");
                    }
                    else
                    {
                        Plugin.Log.LogInfo($"[SpriteRendererPatch] Replaced sprite: {originalName}");
                    }
                }
                value = customSprite;
            }
        }
    }
}
