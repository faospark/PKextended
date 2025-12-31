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
        if (!Plugin.Config.LoadBattleTextures.Value && 
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
        // Save Point Orb colors
        if (textureName == "t_obj_savePoint_ball")
        {
            string colorSuffix = Plugin.Config.SavePointColor.Value.ToLower();
            
            if (colorSuffix == "default")
                return textureName;

            if (!string.IsNullOrEmpty(colorSuffix))
            {
                string colorVariant = $"{textureName}_{colorSuffix}";
                if (CustomTexturePatch.texturePathIndex.ContainsKey(colorVariant))
                {
                    if (Plugin.Config.DetailedTextureLog.Value)
                        Plugin.Log.LogInfo($"[SavePoint Color] Using color variant: {colorVariant}");
                    return colorVariant;
                }
                else if (Plugin.Config.DetailedTextureLog.Value)
                {
                    Plugin.Log.LogWarning($"[SavePoint Color] Color variant '{colorVariant}' not found, using default");
                }
            }
        }

        // Tir Run Animation (shu_field_01_atlas)
        if (textureName == "sactx-0-256x256-Uncompressed-shu_field_01_atlas-959a6bf2")
        {
             // Check if user wants the alt version
             string tirVariant = Plugin.Config.TirRunTexture.Value.ToLower();
             
             if (tirVariant == "default")
                 return textureName;

             if (tirVariant == "alt")
             {
                 string altName = $"{textureName}_alt";
                 if (CustomTexturePatch.texturePathIndex.ContainsKey(altName))
                 {
                     if (Plugin.Config.DetailedTextureLog.Value)
                         Plugin.Log.LogInfo($"[TirRun] Using alt variant: {altName}");
                     return altName;
                 }
                 else if (Plugin.Config.DetailedTextureLog.Value)
                 {
                     Plugin.Log.LogWarning($"[TirRun] Alt variant '{altName}' not found, using default");
                 }
             }
        }

        // Mercenary Fortress Fence
        if (textureName == "t_vb02_00_obj_fence01" ||
            textureName == "t_vb02_00_obj_fence00" ||
            textureName == "t_vb02_00_obj_fence03" ||
            textureName == "t_vb02_00_obj_fence04")
        {
             // Check if user wants a variant
             string fenceVariant = Plugin.Config.MercFortFence.Value.ToLower();
             
             if (fenceVariant == "default" || string.IsNullOrEmpty(fenceVariant))
                 return textureName;

             // Try to use the configured value as a suffix
             string variantName = $"{textureName}_{fenceVariant}";
             if (CustomTexturePatch.texturePathIndex.ContainsKey(variantName))
             {
                 if (Plugin.Config.DetailedTextureLog.Value)
                     Plugin.Log.LogInfo($"[MercFortFence] Using variant: {variantName}");
                 return variantName;
             }
             else if (Plugin.Config.DetailedTextureLog.Value)
             {
                 Plugin.Log.LogWarning($"[MercFortFence] Variant '{variantName}' not found, using default");
             }
        }

        // Add more variant types here as needed
        
        return textureName; // Return original name if no variant
    }
}
