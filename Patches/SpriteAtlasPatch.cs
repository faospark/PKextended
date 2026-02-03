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
