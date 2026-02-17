using HarmonyLib;
using UnityEngine;

namespace PKCore.Patches;

/// <summary>
/// Patches for GRSpriteRenderer - the game's custom sprite renderer component
/// Intercepts sprite assignments to replace with custom textures
/// </summary>
public class GRSpriteRendererPatch
{
    /// <summary>
    /// Intercept GRSpriteRenderer.sprite setter to replace with custom sprites
    /// This is the PRIMARY interception point for sprite assignments
    /// </summary>
    [HarmonyPatch(typeof(GRSpriteRenderer), nameof(GRSpriteRenderer.sprite), MethodType.Setter)]
    [HarmonyPrefix]
    public static void GRSpriteRenderer_set_sprite_Prefix(GRSpriteRenderer __instance, ref Sprite value)
    {
        if (value == null || !Plugin.Config.EnableCustomTextures.Value)
            return;

        string spriteName = value.name;
        
        // DIAGNOSTIC: Log save point sprites specifically
        bool isSavePoint = spriteName.Contains("savePoint", System.StringComparison.OrdinalIgnoreCase);
        if (isSavePoint)
        {
            Plugin.Log.LogInfo($"[SavePoint] GRSpriteRenderer.sprite setter called for: {spriteName}");
            if (value.texture != null)
            {
                Plugin.Log.LogInfo($"[SavePoint]   Texture: {value.texture.name} ({value.texture.width}x{value.texture.height})");
            }
        }
        
        // Log replaceable sprite if enabled
        if (Plugin.Config.LogReplaceableTextures.Value && !CustomTexturePatch.IsTextureLogged(spriteName))
        {
            CustomTexturePatch.LogReplaceableTexture(spriteName, "Sprite - GRSpriteRenderer");
        }

        // Try to replace with custom sprite
        Sprite customSprite = CustomTexturePatch.LoadCustomSprite(spriteName, value);
        if (customSprite != null)
        {
            value = customSprite;
            
            if (Plugin.Config.LogReplaceableTextures.Value || isSavePoint)
            {
                Plugin.Log.LogInfo($"[GRSpriteRenderer] Replaced sprite: {spriteName}");
                if (isSavePoint && customSprite.texture != null)
                {
                    Plugin.Log.LogInfo($"[SavePoint]   Custom texture: {customSprite.texture.name} ({customSprite.texture.width}x{customSprite.texture.height})");
                }
            }
        }
        else if (isSavePoint)
        {
            Plugin.Log.LogWarning($"[SavePoint] No custom sprite found for: {spriteName}");
        }
    }

    /// <summary>
    /// Intercept GRSpriteRenderer.SetForceSprite to catch forced sprite assignments
    /// This method bypasses the normal sprite setter, so we need to patch it separately
    /// </summary>
    [HarmonyPatch(typeof(GRSpriteRenderer), nameof(GRSpriteRenderer.SetForceSprite))]
    [HarmonyPrefix]
    public static void GRSpriteRenderer_SetForceSprite_Prefix(GRSpriteRenderer __instance, ref Sprite spr)
    {
        if (spr == null || !Plugin.Config.EnableCustomTextures.Value)
            return;

        string spriteName = spr.name;
        
        // Log replaceable sprite if enabled
        if (Plugin.Config.LogReplaceableTextures.Value && !CustomTexturePatch.IsTextureLogged(spriteName))
        {
            CustomTexturePatch.LogReplaceableTexture(spriteName, "Sprite - ForceSet");
        }

        // Try to replace with custom sprite
        Sprite customSprite = CustomTexturePatch.LoadCustomSprite(spriteName, spr);
        if (customSprite != null)
        {
            spr = customSprite;
            
            if (Plugin.Config.LogReplaceableTextures.Value)
            {
                Plugin.Log.LogInfo($"[GRSpriteRenderer] Force-replaced sprite: {spriteName}");
            }
        }
    }

    /// <summary>
    /// Intercept GRSpriteRenderer.OnEnable to catch late-enabled sprites
    /// This catches sprites that are enabled after scene load or after being disabled
    /// </summary>
    [HarmonyPatch(typeof(GRSpriteRenderer), nameof(GRSpriteRenderer.OnEnable))]
    [HarmonyPostfix]
    public static void GRSpriteRenderer_OnEnable_Postfix(GRSpriteRenderer __instance)
    {
        if (!Plugin.Config.EnableCustomTextures.Value || __instance == null)
            return;

        // Access the internal sprite field directly to check current state
        Sprite currentSprite = __instance._sprite;
        if (currentSprite == null)
            return;

        string spriteName = currentSprite.name;
        
        // DIAGNOSTIC: Log save point sprites on enable
        bool isSavePoint = spriteName.Contains("savePoint", System.StringComparison.OrdinalIgnoreCase);
        if (isSavePoint)
        {
            Plugin.Log.LogInfo($"[SavePoint] GRSpriteRenderer.OnEnable for: {spriteName}");
        }
        
        // Check if we have a custom texture for this sprite
        if (!CustomTexturePatch.HasCustomTexture(spriteName))
        {
            if (isSavePoint)
            {
                Plugin.Log.LogWarning($"[SavePoint] No custom texture in index for: {spriteName}");
            }
            return;
        }

        // Load and apply custom sprite
        Sprite customSprite = CustomTexturePatch.LoadCustomSprite(spriteName, currentSprite);
        if (customSprite != null)
        {
            __instance.sprite = customSprite; // Use property setter to trigger updates
            
            if (Plugin.Config.LogReplaceableTextures.Value || isSavePoint)
            {
                Plugin.Log.LogInfo($"[GRSpriteRenderer] Replaced sprite on enable: {spriteName}");
            }
        }
        else if (isSavePoint)
        {
            Plugin.Log.LogWarning($"[SavePoint] Failed to load custom sprite for: {spriteName}");
        }
    }

    public static void Initialize()
    {
        if (Plugin.Config.DetailedLogs.Value)
        {
            Plugin.Log.LogInfo("GRSpriteRenderer patches initialized");
        }
    }
}
