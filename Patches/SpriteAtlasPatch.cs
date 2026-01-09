using HarmonyLib;
using UnityEngine;
using UnityEngine.U2D;
using System;

namespace PKCore.Patches;

/// <summary>
/// Patches for SpriteAtlas and Sprite.texture
/// Handles atlas texture replacement for sprites loaded from atlases
/// </summary>
public partial class CustomTexturePatch
{
    /// <summary>
    /// Intercept SpriteAtlas.GetSprite to catch sprites loaded from atlases
    /// NOTE: Individual sprite names from atlases are NOT replaceable - only the atlas texture itself is
    /// </summary>
    [HarmonyPatch(typeof(SpriteAtlas), nameof(SpriteAtlas.GetSprite))]
    [HarmonyPostfix]
    public static void SpriteAtlas_GetSprite_Postfix(SpriteAtlas __instance, string name, ref Sprite __result)
    {
        if (__result == null)
            return;

        string spriteName = __result.name;
        
        // Remove (Clone) suffix if present
        if (spriteName.EndsWith("(Clone)"))
            spriteName = spriteName.Substring(0, spriteName.Length - 7);

        // DO NOT log individual sprite names from atlases - they're not replaceable files
        // Only the atlas texture itself (accessed via Sprite.texture) is replaceable
    }

    /// <summary>
    /// Intercept Sprite.texture getter to replace atlas textures
    /// This is THE KEY PATCH for replacing atlas textures (including summon effects)
    /// </summary>
    [HarmonyPatch(typeof(Sprite), nameof(Sprite.texture), MethodType.Getter)]
    [HarmonyPostfix]
    public static void Sprite_get_texture_Postfix(Sprite __instance, ref Texture2D __result)
    {
        if (__result == null)
            return;

        string textureName = __result.name;
        
        // DIAGNOSTIC: Log save point atlas specifically
        bool isSavePointAtlas = textureName.Contains("savePoint", StringComparison.OrdinalIgnoreCase);
        if (isSavePointAtlas)
        {
            // Texture access logging removed for performance
        }
        
        LogReplaceableTexture(textureName, "Texture - Atlas");

        // Try to replace the atlas texture
        if (Plugin.Config.EnableCustomTextures.Value)
        {
            Texture2D customTexture = LoadCustomTexture(textureName);
            if (customTexture != null)
            {
                // Check if this texture has already been replaced to avoid duplicate logs
                if (!replacedTextures.Contains(textureName))
                {
                    replacedTextures.Add(textureName);
                    
                    // Skip logging for character sprites to reduce spam
                    bool shouldSkipLog = false;
                    if (texturePathIndex.TryGetValue(textureName, out string replacementTexPath))
                    {
                        shouldSkipLog = replacementTexPath.ToLower().Contains("characters");
                    }
                    
                    if (!shouldSkipLog)
                    {
                        Plugin.Log.LogDebug($"Replaced texture: {textureName}");
                    }
                }
                
                __result = customTexture;
            }
        }
    }
}
