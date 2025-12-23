using System;
using System.IO;

namespace PKCore.Patches;

/// <summary>
/// Texture category filtering options - separate file for easy customization
/// Add your texture folder filters here without modifying CustomTexturePatch.cs
/// </summary>
public static class TextureOptions
{
    /// <summary>
    /// Check if a texture should be loaded based on its folder path
    /// Returns false to skip loading the texture
    /// </summary>
    public static bool ShouldLoadTexture(string filePath)
    {
        // Disable launcher UI textures
        if (!Plugin.Config.LoadLauncherUITextures.Value && 
            filePath.Contains("\\launcher\\", StringComparison.OrdinalIgnoreCase))
            return false;

        // Disable battle effect textures
        if (!Plugin.Config.LoadBattleEffectTextures.Value && 
            filePath.Contains("\\battle\\", StringComparison.OrdinalIgnoreCase))
            return false;

        // Disable character textures
        if (!Plugin.Config.LoadCharacterTextures.Value && 
            filePath.Contains("\\characters\\", StringComparison.OrdinalIgnoreCase))
            return false;

        // Add more filters here as needed:
        // if (filePath.Contains("\\yourfolder\\", StringComparison.OrdinalIgnoreCase))
        //     return false;

        return true; // Load the texture
    }

    /// <summary>
    /// Get the texture name with variant suffix applied (e.g., color variants for save points)
    /// Returns the variant name if it exists in the index, otherwise returns the original name
    /// </summary>
    public static string GetTextureNameWithVariant(string textureName)
    {
        // Save point color variants
        if (textureName == "t_obj_savePoint_ball")
        {
            string colorSuffix = Plugin.Config.SavePointColor.Value.ToLower();
            if (!string.IsNullOrEmpty(colorSuffix) && colorSuffix != "default")
            {
                string colorVariant = $"{textureName}_{colorSuffix}";
                // Check if color variant exists in index
                if (CustomTexturePatch.texturePathIndex.ContainsKey(colorVariant))
                {
                    if (Plugin.Config.DetailedTextureLog.Value)
                    {
                        Plugin.Log.LogInfo($"[SavePoint Color] Using color variant: {colorVariant}");
                    }
                    return colorVariant;
                }
                else if (Plugin.Config.DetailedTextureLog.Value)
                {
                    Plugin.Log.LogWarning($"[SavePoint Color] Color variant '{colorVariant}' not found, using default");
                }
            }
        }

        // Add more variant types here as needed
        
        return textureName; // Return original name if no variant
    }
}
