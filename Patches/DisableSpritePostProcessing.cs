using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace PKCore.Patches;

public class DisableSpritePostProcessingPatch
{
    private const int SpriteLayerMask = 1 << 31; // Use layer 31 for sprites to exclude from post-processing
    private static bool _isEnabled = false;

    // Hook into GRSpriteRenderer to move sprites to a non-post-processed layer
    [HarmonyPatch(typeof(GRSpriteRenderer), nameof(GRSpriteRenderer.Awake))]
    [HarmonyPostfix]
    static void SetSpriteLayer(GRSpriteRenderer __instance)
    {
        if (!_isEnabled || __instance == null || __instance.gameObject == null)
        {
            return;
        }

        // Move sprite to a layer that won't be affected by post-processing
        __instance.gameObject.layer = 31;
    }

    // Alternative approach: Disable specific post-processing effects on sprite materials
    // This runs AFTER AntiAliasingPatch to preserve anti-aliasing settings
    [HarmonyPatch(typeof(GRSpriteRenderer), nameof(GRSpriteRenderer.material), MethodType.Setter)]
    [HarmonyPriority(Priority.LowerThanNormal)] // Run after other patches (like AntiAliasing)
    [HarmonyPostfix]
    static void DisablePostProcessOnMaterial(GRSpriteRenderer __instance)
    {
        if (!_isEnabled || __instance == null || __instance._mat == null)
        {
            return;
        }

        var material = __instance._mat;
        
        // Disable post-processing keywords/features on the sprite material
        if (material.HasProperty("_MainTex"))
        {
            // Only set render queue if it hasn't been modified by other patches
            // Default sprite queue is 2000, if it's still default, move it to overlay
            if (material.renderQueue >= 2000 && material.renderQueue < 3000)
            {
                material.renderQueue = 3000; // Overlay queue renders after post-processing
            }
            
            // Disable shader features that would apply post-processing effects
            // These don't affect texture filtering (used by anti-aliasing)
            material.DisableKeyword("BLOOM");
            material.DisableKeyword("BLOOM_LENS_DIRT");
            material.DisableKeyword("CHROMATIC_ABERRATION");
            material.DisableKeyword("DISTORT");
            material.DisableKeyword("VIGNETTE");
            material.DisableKeyword("GRAIN");
        }
    }

    // Ensure post-processing effects don't affect sprite renderers
    [HarmonyPatch(typeof(UnityEngine.Rendering.PostProcessing.PostProcessLayer), nameof(UnityEngine.Rendering.PostProcessing.PostProcessLayer.OnEnable))]
    [HarmonyPostfix]
    static void ConfigurePostProcessLayer(UnityEngine.Rendering.PostProcessing.PostProcessLayer __instance)
    {
        if (!_isEnabled || __instance == null)
        {
            return;
        }

        // Exclude sprite layer from post-processing volume triggers
        __instance.volumeLayer &= ~SpriteLayerMask;
    }

    public static void Initialize()
    {
        _isEnabled = Plugin.Config.DisableSpritePostProcessing.Value;
        // Harmony patches will handle sprites as they're created during gameplay
    }
}
