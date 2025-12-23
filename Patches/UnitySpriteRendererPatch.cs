using HarmonyLib;
using UnityEngine;

namespace PKCore.Patches;

/// <summary>
/// Patches for Unity's standard SpriteRenderer component
/// Handles objects that use UnityEngine.SpriteRenderer instead of the game's custom GRSpriteRenderer
/// Examples: Save points, certain effects, etc.
/// </summary>
public class UnitySpriteRendererPatch
{
    /// <summary>
    /// Intercept UnityEngine.SpriteRenderer.sprite setter to replace with custom sprites
    /// This catches objects using standard Unity SpriteRenderer (like save points)
    /// </summary>
    [HarmonyPatch(typeof(UnityEngine.SpriteRenderer), nameof(UnityEngine.SpriteRenderer.sprite), MethodType.Setter)]
    [HarmonyPrefix]
    public static void UnitySpriteRenderer_set_sprite_Prefix(UnityEngine.SpriteRenderer __instance, ref Sprite value)
    {
        if (value == null || !Plugin.Config.EnableCustomTextures.Value)
            return;

        string spriteName = value.name;
        
        // DIAGNOSTIC: Log save point sprites specifically
        bool isSavePoint = spriteName.Contains("savePoint", System.StringComparison.OrdinalIgnoreCase);
        if (isSavePoint)
        {
            Plugin.Log.LogInfo($"[UnitySpriteRenderer] Save point sprite assignment: {spriteName}");
            if (value.texture != null)
            {
                Plugin.Log.LogInfo($"[UnitySpriteRenderer]   Original texture: {value.texture.name} ({value.texture.width}x{value.texture.height})");
            }
        }
        
        // Log replaceable sprite if enabled
        if (Plugin.Config.LogReplaceableTextures.Value && !CustomTexturePatch.IsTextureLogged(spriteName))
        {
            CustomTexturePatch.LogReplaceableTexture(spriteName, "Sprite - UnitySpriteRenderer");
        }

        // Try to replace with custom sprite
        Sprite customSprite = CustomTexturePatch.LoadCustomSprite(spriteName, value);
        if (customSprite != null)
        {
            value = customSprite;
            
            if (Plugin.Config.DetailedTextureLog.Value || isSavePoint)
            {
                Plugin.Log.LogInfo($"[UnitySpriteRenderer] Replaced sprite: {spriteName}");
                if (isSavePoint && customSprite.texture != null)
                {
                    Plugin.Log.LogInfo($"[UnitySpriteRenderer]   Custom texture: {customSprite.texture.name} ({customSprite.texture.width}x{customSprite.texture.height})");
                }
            }
        }
        else if (isSavePoint)
        {
            Plugin.Log.LogWarning($"[UnitySpriteRenderer] No custom sprite found for: {spriteName}");
        }
    }

    public static void Initialize()
    {
        Plugin.Log.LogInfo("Unity SpriteRenderer patches initialized");
    }
}
