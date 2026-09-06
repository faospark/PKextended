using UnityEngine;
using System.IO;

namespace PKCore.Patches;

/// <summary>
/// DDS file loader for pre-compressed textures
/// Supports BC1 (DXT1), BC3 (DXT5), and BC7 formats (including DX10 extended headers)
/// Note: BC7 can be loaded from DDS files but cannot be created at runtime via Texture2D.Compress()
/// </summary>
public static class DDSLoader
{
    private const uint DDS_MAGIC = 0x20534444;   // "DDS " in little-endian
    private const uint FOURCC_DXT1 = 0x31545844; // "DXT1"
    private const uint FOURCC_DXT5 = 0x35545844; // "DXT5"
    private const uint FOURCC_BC7  = 0x00374342; // "BC7\0"
    private const uint FOURCC_DX10 = 0x30315844; // "DX10"
    
    private const int BASE_HEADER_SIZE = 128;
    private const int DX10_HEADER_SIZE = 20;
    
    /// <summary>
    /// Load a DDS file from a byte array as a pre-compressed texture
    /// Automatically pads dimensions to multiples of 4 if needed (required for DDS compressed formats)
    /// </summary>
    public static Texture2D LoadDDSFromBytes(byte[] ddsBytes, string textureName = null)
    {
        try
        {
            if (ddsBytes.Length < BASE_HEADER_SIZE)
            {
                Plugin.Log.LogError($"DDS data too small{(textureName != null ? $" for {textureName}" : "")}");
                return null;
            }
            
            // Parse DDS header
            DDSHeader header = ParseDDSHeader(ddsBytes);
            
            if (header == null)
            {
                Plugin.Log.LogError($"Invalid DDS data{(textureName != null ? $" for {textureName}" : "")}");
                return null;
            }

            if (ddsBytes.Length < header.HeaderSize)
            {
                Plugin.Log.LogError($"DDS data too small for header size {header.HeaderSize}{(textureName != null ? $" for {textureName}" : "")}");
                return null;
            }
            
            // Validate and pad dimensions to multiples of 4 (required for DDS compressed formats)
            int originalWidth = header.Width;
            int originalHeight = header.Height;
            header.Width = RoundUpTo4(header.Width);
            header.Height = RoundUpTo4(header.Height);
            
            if (originalWidth != header.Width || originalHeight != header.Height)
            {
                Plugin.Log.LogWarning($"DDS texture {textureName ?? "unknown"}: dimensions {originalWidth}x{originalHeight} are not multiples of 4. Padding to {header.Width}x{header.Height}.");
            }
            
            // Create texture with appropriate format
            Texture2D texture = new Texture2D(header.Width, header.Height, header.Format, header.MipMapCount > 1);
            
            // Load raw texture data (skip DDS header)
            int dataSize = ddsBytes.Length - header.HeaderSize;
            byte[] textureData = new byte[dataSize];
            System.Array.Copy(ddsBytes, header.HeaderSize, textureData, 0, dataSize);
            
            texture.LoadRawTextureData(textureData);
            texture.Apply(false, false);
            
            if (textureName != null)
                texture.name = textureName;
            
            return texture;
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"Failed to load DDS data: {ex.Message}");
            return null;
        }
    }

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
            return LoadDDSFromBytes(ddsBytes, Path.GetFileNameWithoutExtension(filePath));
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"Failed to load DDS file {filePath}: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Parse DDS header to extract format and dimensions
    /// Handles standard DDS headers (128 bytes) and DX10 extended headers (148 bytes)
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
        
        header.HeaderSize = BASE_HEADER_SIZE;

        // Read pixel format FourCC (offset 84)
        uint fourCC = System.BitConverter.ToUInt32(data, 84);

        if (fourCC == FOURCC_DX10)
        {
            if (data.Length < BASE_HEADER_SIZE + DX10_HEADER_SIZE)
            {
                Plugin.Log.LogWarning("DDS header claims DX10 extended format, but file is too small.");
                return null;
            }

            header.HeaderSize = BASE_HEADER_SIZE + DX10_HEADER_SIZE;
            uint dxgiFormat = System.BitConverter.ToUInt32(data, 128);
            header.Format = GetDxgiTextureFormat(dxgiFormat);

            if (header.Format == TextureFormat.RGBA32)
            {
                Plugin.Log.LogWarning($"Unsupported DDS DX10 format (DXGI_FORMAT: {dxgiFormat})");
                return null;
            }
        }
        else
        {
            header.Format = GetTextureFormat(fourCC);

            if (header.Format == TextureFormat.RGBA32)
            {
                Plugin.Log.LogWarning($"Unsupported DDS format: FourCC {FourCCToString(fourCC)} (0x{fourCC:X8})");
                return null;
            }
        }
        
        return header;
    }
    
    /// <summary>
    /// Convert standard FourCC code to Unity TextureFormat
    /// </summary>
    private static TextureFormat GetTextureFormat(uint fourCC)
    {
        if (fourCC == FOURCC_DXT1)
            return TextureFormat.DXT1;
        
        if (fourCC == FOURCC_DXT5)
            return TextureFormat.DXT5;
        
        if (fourCC == FOURCC_BC7)
            return TextureFormat.BC7;
        
        // Unsupported format - return RGBA32 as indicator
        return TextureFormat.RGBA32;
    }

    /// <summary>
    /// Convert DXGI Format ID (from DX10 extension header) to Unity TextureFormat
    /// </summary>
    private static TextureFormat GetDxgiTextureFormat(uint dxgiFormat)
    {
        return dxgiFormat switch
        {
            70 or 71 or 72 => TextureFormat.DXT1, // BC1_TYPELESS, BC1_UNORM, BC1_UNORM_SRGB
            73 or 74 or 75 => TextureFormat.DXT5, // BC2
            76 or 77 or 78 => TextureFormat.DXT5, // BC3_TYPELESS, BC3_UNORM, BC3_UNORM_SRGB
            79 or 80 or 81 => TextureFormat.BC4,  // BC4_TYPELESS, BC4_UNORM, BC4_SNORM
            82 or 83 or 84 => TextureFormat.BC5,  // BC5_TYPELESS, BC5_UNORM, BC5_SNORM
            94 or 95 or 96 => TextureFormat.BC6H, // BC6H_TYPELESS, BC6H_UF16, BC6H_SF16
            97 or 98 or 99 => TextureFormat.BC7,  // BC7_TYPELESS, BC7_UNORM, BC7_UNORM_SRGB
            _ => TextureFormat.RGBA32
        };
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
    /// Round up a dimension to the nearest multiple of 4 (required for DDS compressed formats)
    /// </summary>
    private static int RoundUpTo4(int value)
    {
        int remainder = value % 4;
        return remainder == 0 ? value : value + (4 - remainder);
    }
    
    /// <summary>
    /// DDS header data
    /// </summary>
    private class DDSHeader
    {
        public int Width;
        public int Height;
        public int MipMapCount;
        public int HeaderSize;
        public TextureFormat Format;
    }
}
