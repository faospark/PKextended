using HarmonyLib;
using UnityEngine;

namespace PKCore.Patches;

public class SpriteFilteringPatch
{
    private static bool _spriteFilteringEnabled = false;
    private static float _spriteMipmapBias = -0.5f;

    // Hook into GRSpriteRenderer to apply texture filtering
    [HarmonyPatch(typeof(GRSpriteRenderer), nameof(GRSpriteRenderer.material), MethodType.Setter)]
    [HarmonyPatch(typeof(GRSpriteRenderer), nameof(GRSpriteRenderer.Awake))]
    [HarmonyPostfix]
    static void ApplySpriteFiltering(GRSpriteRenderer __instance)
    {
        if (__instance == null || __instance._mat == null || !_spriteFilteringEnabled)
        {
            return;
        }

        var material = __instance._mat;
        var texture = material.mainTexture;
        
        if (texture != null)
        {
            // Use Bilinear filtering when enabled
            texture.filterMode = FilterMode.Bilinear;
            
            // Use 2x anisotropic filtering
            texture.anisoLevel = 2;
            
            // Apply mipmap bias to control sharpness and prevent white outlines
            texture.mipMapBias = _spriteMipmapBias;
        }
    }

    public static void Initialize()
    {
        _spriteFilteringEnabled = Plugin.Config.SpriteFilteringQuality.Value;
        _spriteMipmapBias = Plugin.Config.SpriteMipmapBias.Value;
    }

    public static bool GetSpriteQuality()
    {
        return _spriteFilteringEnabled;
    }
}
