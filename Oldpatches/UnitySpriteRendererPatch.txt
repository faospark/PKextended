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
    [HarmonyPatch(typeof(UnityEngine.SpriteRenderer), nameof(UnityEngine.SpriteRenderer.sprite), MethodType.Setter)]
    [HarmonyPrefix]
    public static void UnitySpriteRenderer_set_sprite_Prefix(UnityEngine.SpriteRenderer __instance, ref Sprite value)
    {
        if (value == null)
            return;


        string spriteName = value.name;
        
        // DIAGNOSTIC: Log dragon sprites specifically
        bool isDragon = spriteName.Contains("dragon", System.StringComparison.OrdinalIgnoreCase);
        if (isDragon)
        {
            Plugin.Log.LogInfo($"[UnitySpriteRenderer] Dragon sprite assignment: {spriteName}");
            Plugin.Log.LogInfo($"[UnitySpriteRenderer]   GameObject: {__instance.gameObject.name}");
            Plugin.Log.LogInfo($"[UnitySpriteRenderer]   EnableCustomTextures: {Plugin.Config.EnableCustomTextures.Value}");
            if (value.texture != null)
            {
                Plugin.Log.LogInfo($"[UnitySpriteRenderer]   Original texture: {value.texture.name} ({value.texture.width}x{value.texture.height})");
            }
        }
        
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

        if (!Plugin.Config.EnableCustomTextures.Value)
            return;

        // Try to replace with custom sprite
        Sprite customSprite = CustomTexturePatch.LoadCustomSprite(spriteName, value);
        if (customSprite != null)
        {
            value = customSprite;
            
            if (Plugin.Config.DetailedTextureLog.Value || isSavePoint || isDragon)
            {
                Plugin.Log.LogInfo($"[UnitySpriteRenderer] ✓ Replaced sprite: {spriteName}");
                if ((isSavePoint || isDragon) && customSprite.texture != null)
                {
                    Plugin.Log.LogInfo($"[UnitySpriteRenderer]   Custom texture: {customSprite.texture.name} ({customSprite.texture.width}x{customSprite.texture.height})");
                }
            }
        }
        // If no custom sprite, check if the sprite's atlas texture has a custom replacement
        else if (value.texture != null)
        {
            string atlasTextureName = value.texture.name;
            
            // Skip if texture name is empty
            if (!string.IsNullOrWhiteSpace(atlasTextureName))
            {
                if (Plugin.Config.DetailedTextureLog.Value)
                {
                    Plugin.Log.LogInfo($"[UnitySpriteRenderer] No custom sprite for '{spriteName}', checking atlas texture: {atlasTextureName}");
                }
                
                Texture2D customAtlasTexture = CustomTexturePatch.LoadCustomTexture(atlasTextureName);
                
                if (customAtlasTexture != null)
                {
                    if (Plugin.Config.DetailedTextureLog.Value)
                    {
                        Plugin.Log.LogInfo($"[UnitySpriteRenderer] Found custom atlas texture, replacing in-place...");
                    }
                    
                    // Replace the atlas texture in-place
                    bool replaced = CustomTexturePatch.ReplaceTextureInPlace(value.texture, atlasTextureName);
                    
                    if (replaced)
                    {
                        Plugin.Log.LogInfo($"[UnitySpriteRenderer] ✓ Replaced atlas texture for sprite '{spriteName}': {atlasTextureName}");
                    }
                    else
                    {
                        Plugin.Log.LogWarning($"[UnitySpriteRenderer] ✗ Failed to replace atlas texture: {atlasTextureName}");
                    }
                }
                else if (Plugin.Config.DetailedTextureLog.Value)
                {
                    Plugin.Log.LogInfo($"[UnitySpriteRenderer] No custom atlas texture found for: {atlasTextureName}");
                }
            }
        }
        else if (isSavePoint || isDragon)
        {
            Plugin.Log.LogWarning($"[UnitySpriteRenderer] ✗ No custom sprite found for: {spriteName}");
            Plugin.Log.LogWarning($"[UnitySpriteRenderer]   Checked in texture index: {CustomTexturePatch.texturePathIndex.ContainsKey(spriteName)}");
        }
    }

    public static void Initialize()
    {
        Plugin.Log.LogInfo("Applying Unity SpriteRenderer patches");
    }
}
