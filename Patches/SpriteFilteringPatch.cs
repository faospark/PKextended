using HarmonyLib;
using UnityEngine;

namespace PKextended.Patches;

public class SpriteFilteringPatch
{
    private static int _spriteAntiAliasingLevel = 0;
    private static float _spriteMipmapBias = -0.5f;

    // Hook into GRSpriteRenderer to apply texture filtering
    [HarmonyPatch(typeof(GRSpriteRenderer), nameof(GRSpriteRenderer.material), MethodType.Setter)]
    [HarmonyPatch(typeof(GRSpriteRenderer), nameof(GRSpriteRenderer.Awake))]
    [HarmonyPostfix]
    static void ApplySpriteFiltering(GRSpriteRenderer __instance)
    {
        if (__instance == null || __instance._mat == null || _spriteAntiAliasingLevel <= 0)
        {
            return;
        }

        var material = __instance._mat;
        var texture = material.mainTexture;
        
        if (texture != null)
        {
            // Use Trilinear for best quality, Bilinear for medium
            texture.filterMode = _spriteAntiAliasingLevel >= 2 ? FilterMode.Trilinear : FilterMode.Bilinear;
            
            // Set anisotropic filtering level for better quality at angles
            texture.anisoLevel = _spriteAntiAliasingLevel switch
            {
                1 => 2,  // Low - 2x anisotropic
                2 => 4,  // Medium - 4x anisotropic
                3 => 8,  // High - 8x anisotropic
                _ => 0
            };
            
            // Apply mipmap bias to control sharpness and prevent white outlines
            texture.mipMapBias = _spriteMipmapBias;
        }
    }

    public static void Initialize()
    {
        _spriteAntiAliasingLevel = Plugin.Config.SpriteFilteringQuality.Value;
        _spriteMipmapBias = Plugin.Config.SpriteMipmapBias.Value;

        Plugin.Log.LogInfo($"Sprite filtering initialized with quality level: {_spriteAntiAliasingLevel}");
    }

    public static int GetSpriteQuality()
    {
        return _spriteAntiAliasingLevel;
    }
}
