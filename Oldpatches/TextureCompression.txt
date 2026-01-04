using UnityEngine;
using System.Diagnostics;

namespace PKCore.Patches;

/// <summary>
/// Centralized texture compression utilities for BC1/BC3/BC7 compression
/// Reduces VRAM usage by 4-8x with minimal quality loss
/// </summary>
public static class TextureCompression
{
    public enum CompressionFormat
    {
        Auto,   // BC1 for RGB, BC3 for RGBA
        BC1,    // DXT1 - RGB, 8:1 compression
        BC3,    // DXT5 - RGBA, 6:1 compression
        BC7     // Highest quality RGBA, 6:1 compression
    }

    /// <summary>
    /// Compress a texture to BC1/BC3/BC7 format for GPU efficiency
    /// Automatically pads textures to multiples of 4 if needed (BC compression requirement)
    /// </summary>
    /// <param name="texture">Texture to compress (must be RGBA32 format)</param>
    /// <param name="textureName">Optional name for logging purposes</param>
    public static void CompressTexture(Texture2D texture, string textureName = null)
    {
        if (!Plugin.Config.EnableTextureCompression.Value)
            return;

        if (texture == null)
            return;

        try
        {
            var sw = Stopwatch.StartNew();
            
            // BC compression requires dimensions to be multiples of 4
            // Check if padding is needed
            int originalWidth = texture.width;
            int originalHeight = texture.height;
            int paddedWidth = RoundUpToMultipleOf4(originalWidth);
            int paddedHeight = RoundUpToMultipleOf4(originalHeight);
            
            bool needsPadding = (paddedWidth != originalWidth || paddedHeight != originalHeight);
            
            if (needsPadding)
            {
                // Resize texture to padded dimensions
                Texture2D paddedTexture = ResizeTexture(texture, paddedWidth, paddedHeight);
                
                // Copy pixels back to original texture
                texture.Reinitialize(paddedWidth, paddedHeight);
                texture.SetPixels(paddedTexture.GetPixels());
                texture.Apply(false, false);
                
                // Clean up temporary texture
                UnityEngine.Object.Destroy(paddedTexture);
                
                if (Plugin.Config.DetailedTextureLog.Value && !string.IsNullOrEmpty(textureName))
                {
                    Plugin.Log.LogInfo($"Padded {textureName} from {originalWidth}x{originalHeight} to {paddedWidth}x{paddedHeight}");
                }
            }
            
            // Determine compression format
            CompressionFormat format = GetCompressionFormat(texture);
            
            // Determine quality
            bool highQuality = Plugin.Config.TextureCompressionQuality.Value.Equals("High", System.StringComparison.OrdinalIgnoreCase);
            
            // Compress based on format
            string formatName = CompressWithFormat(texture, format, highQuality);
            
            sw.Stop();

            if (Plugin.Config.DetailedTextureLog.Value && !string.IsNullOrEmpty(textureName))
            {
                Plugin.Log.LogInfo($"Compressed {textureName} to {formatName} ({texture.width}x{texture.height}) in {sw.ElapsedMilliseconds}ms");
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogWarning($"Failed to compress texture {textureName ?? "unknown"}: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Determine which compression format to use
    /// </summary>
    private static CompressionFormat GetCompressionFormat(Texture2D texture)
    {
        string formatSetting = Plugin.Config.TextureCompressionFormat.Value;
        
        if (formatSetting.Equals("BC1", System.StringComparison.OrdinalIgnoreCase))
            return CompressionFormat.BC1;
        if (formatSetting.Equals("BC3", System.StringComparison.OrdinalIgnoreCase))
            return CompressionFormat.BC3;
        if (formatSetting.Equals("BC7", System.StringComparison.OrdinalIgnoreCase))
            return CompressionFormat.BC7;
        
        // Auto mode: detect based on alpha channel
        if (HasSignificantAlpha(texture))
            return CompressionFormat.BC3; // RGBA
        else
            return CompressionFormat.BC1; // RGB (better compression)
    }
    
    /// <summary>
    /// Check if texture has significant alpha channel (transparency)
    /// Uses sampling for performance on large textures
    /// </summary>
    private static bool HasSignificantAlpha(Texture2D texture)
    {
        int width = texture.width;
        int height = texture.height;
        
        // Sample every 4th pixel for performance
        int step = 4;
        
        for (int y = 0; y < height; y += step)
        {
            for (int x = 0; x < width; x += step)
            {
                Color pixel = texture.GetPixel(x, y);
                if (pixel.a < 0.99f)
                    return true; // Found transparency
            }
        }
        
        return false; // Fully opaque
    }
    
    /// <summary>
    /// Compress texture with specified format
    /// </summary>
    private static string CompressWithFormat(Texture2D texture, CompressionFormat format, bool highQuality)
    {
        switch (format)
        {
            case CompressionFormat.BC1:
                // BC1 (DXT1) - RGB only, 8:1 compression
                texture.Compress(highQuality);
                return "BC1 (DXT1)";
                
            case CompressionFormat.BC3:
                // BC3 (DXT5) - RGBA, 6:1 compression
                texture.Compress(highQuality);
                return "BC3 (DXT5)";
                
            case CompressionFormat.BC7:
                // BC7 - Highest quality RGBA
                // Note: Unity's Compress() doesn't directly support BC7
                // We'll use BC3 as fallback with high quality
                texture.Compress(true); // Always high quality for BC7 request
                return "BC3 (DXT5, BC7 fallback)";
                
            default:
                texture.Compress(highQuality);
                return "BC3 (DXT5)";
        }
    }
    
    /// <summary>
    /// Round up to the nearest multiple of 4
    /// </summary>
    private static int RoundUpToMultipleOf4(int value)
    {
        return (value + 3) & ~3;
    }
    
    /// <summary>
    /// Resize texture using bilinear filtering
    /// </summary>
    private static Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32);
        rt.filterMode = FilterMode.Bilinear;
        
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);
        
        Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        result.Apply();
        
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        
        return result;
    }
}
