using UnityEngine;
using System.IO;

namespace PKCore.Patches;

/// <summary>
/// DDS file loader for pre-compressed textures
/// Supports BC1 (DXT1), BC3 (DXT5), and BC7 formats
/// </summary>
public static class DDSLoader
{
    private const uint DDS_MAGIC = 0x20534444; // "DDS " in little-endian
    private const int DDS_HEADER_SIZE = 128;
    
    /// <summary>
    /// Load a DDS file as a pre-compressed texture
    /// </summary>
    public static Texture2D LoadDDS(string filePath)
    {
        if (!File.Exists(filePath))
            return null;
        
        try
        {
            byte[] ddsBytes = File.ReadAllBytes(filePath);
            
            if (ddsBytes.Length < DDS_HEADER_SIZE)
            {
                Plugin.Log.LogError($"DDS file too small: {filePath}");
                return null;
            }
            
            // Parse DDS header
            DDSHeader header = ParseDDSHeader(ddsBytes);
            
            if (header == null)
            {
                Plugin.Log.LogError($"Invalid DDS file: {filePath}");
                return null;
            }
            
            // Create texture with appropriate format
            Texture2D texture = new Texture2D(header.Width, header.Height, header.Format, header.MipMapCount > 1);
            
            // Load raw texture data (skip header)
            byte[] textureData = new byte[ddsBytes.Length - DDS_HEADER_SIZE];
            System.Array.Copy(ddsBytes, DDS_HEADER_SIZE, textureData, 0, textureData.Length);
            
            texture.LoadRawTextureData(textureData);
            texture.Apply(false, false);
            
            texture.name = Path.GetFileNameWithoutExtension(filePath);
            
            return texture;
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"Failed to load DDS file {filePath}: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Parse DDS header to extract format and dimensions
    /// </summary>
    private static DDSHeader ParseDDSHeader(byte[] data)
    {
        // Check magic number: "DDS " (0x20534444)
        uint magic = System.BitConverter.ToUInt32(data, 0);
        if (magic != DDS_MAGIC)
            return null;
        
        DDSHeader header = new DDSHeader();
        
        // Read dimensions (offset 12 and 16)
        header.Height = System.BitConverter.ToInt32(data, 12);
        header.Width = System.BitConverter.ToInt32(data, 16);
        
        // Read mipmap count (offset 28)
        header.MipMapCount = System.BitConverter.ToInt32(data, 28);
        if (header.MipMapCount == 0)
            header.MipMapCount = 1;
        
        // Read pixel format FourCC (offset 84)
        uint fourCC = System.BitConverter.ToUInt32(data, 84);
        header.Format = GetTextureFormat(fourCC);
        
        if (header.Format == TextureFormat.RGBA32)
        {
            // Unsupported format
            Plugin.Log.LogWarning($"Unsupported DDS format: {FourCCToString(fourCC)}");
            return null;
        }
        
        return header;
    }
    
    /// <summary>
    /// Convert FourCC code to Unity TextureFormat
    /// </summary>
    private static TextureFormat GetTextureFormat(uint fourCC)
    {
        // DXT1 = BC1 (0x31545844 = "DXT1")
        if (fourCC == 0x31545844)
            return TextureFormat.DXT1;
        
        // DXT5 = BC3 (0x35545844 = "DXT5")
        if (fourCC == 0x35545844)
            return TextureFormat.DXT5;
        
        // BC7 (0x37304342 = "BC7\0")
        if (fourCC == 0x00374342)
            return TextureFormat.BC7;
        
        // Unsupported format - return RGBA32 as indicator
        return TextureFormat.RGBA32;
    }
    
    /// <summary>
    /// Convert FourCC to readable string for logging
    /// </summary>
    private static string FourCCToString(uint fourCC)
    {
        byte[] bytes = System.BitConverter.GetBytes(fourCC);
        return System.Text.Encoding.ASCII.GetString(bytes);
    }
    
    /// <summary>
    /// DDS header data
    /// </summary>
    private class DDSHeader
    {
        public int Width;
        public int Height;
        public int MipMapCount;
        public TextureFormat Format;
    }
}
