using System;
using System.IO;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using BepInEx;

namespace PKCore.Patches
{
    [HarmonyPatch(typeof(MapBGLoader))]
    public static class MapTexturePatch
    {
        private static Dictionary<string, Texture2D> _cachedTextures = new Dictionary<string, Texture2D>();
        private static string _texturePath;

        private static string TexturePath
        {
            get
            {
                if (_texturePath == null)
                {
                    _texturePath = Path.Combine(Paths.GameRootPath, "PKCore", "Textures", "Maps");
                    if (!Directory.Exists(_texturePath))
                    {
                        Directory.CreateDirectory(_texturePath);
                    }
                }
                return _texturePath;
            }
        }

        [HarmonyPatch(nameof(MapBGLoader.GetMaterial))]
        [HarmonyPostfix]
        public static void GetMaterial_Postfix(Il2CppReferenceArray<Material> __result)
        {
            if (__result == null) return;

            foreach (var material in __result)
            {
                if (material == null) continue;

                string matName = material.name.Replace("(Instance)", "").Trim();
                
                // Try to load replacement texture
                Texture2D replacement = LoadReplacementTexture(matName);
                if (replacement != null)
                {
                    // Log only once per material to avoid spam
                    if (material.mainTexture != replacement)
                    {
                        Plugin.Log.LogInfo($"[MapTexturePatch] Replaced texture for material: {matName}");
                        material.mainTexture = replacement;
                    }
                }
            }
        }

        private static Texture2D LoadReplacementTexture(string materialName)
        {
            // Check cache first
            if (_cachedTextures.TryGetValue(materialName, out Texture2D cached))
            {
                return cached;
            }

            string filePath = Path.Combine(TexturePath, materialName + ".png");
            if (File.Exists(filePath))
            {
                try
                {
                    byte[] fileData = File.ReadAllBytes(filePath);
                    Texture2D texture = new Texture2D(2, 2);
                    ImageConversion.LoadImage(texture, fileData);
                    
                    // Compress texture to BC3 (DXT5) for GPU efficiency
                    TextureCompression.CompressTexture(texture, materialName);
                    
                    texture.name = materialName + "_Custom";
                    texture.filterMode = FilterMode.Point; // Keep pixel art sharp? Or Bilinear? Defaulting to Point for now. 
                    // If the user wants smooth textures, they might need a config. But Suikoden is pixel art mostly.
                    
                    // Cache it
                    _cachedTextures[materialName] = texture;
                    return texture;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"[MapTexturePatch] Failed to load texture {filePath}: {ex.Message}");
                }
            }

            // Mark as null in cache to avoid repeated file system checks? 
            // Better to check file existence occasionally? For now, let's not cache failures aggressively to allow hot-reloading.
            return null;
        }
        
        // Optional: Method to clear cache for hot-reloading
        public static void ClearCache()
        {
            foreach (var tex in _cachedTextures.Values)
            {
                UnityEngine.Object.Destroy(tex);
            }
            _cachedTextures.Clear();
            Plugin.Log.LogInfo("[MapTexturePatch] Texture cache cleared.");
        }
    }
}
