using HarmonyLib;
using UnityEngine;

namespace PKCore.Patches;

/* EXPERIMENTAL - DISABLED
 * QualitySettings.renderScale is not available in this Unity version (pre-2022)
 * Uncomment and test if game is updated to Unity 2022+
 * 
 * This approach would scale game content while preserving UI at native resolution
 * for better performance without sacrificing UI quality.
 
/// <summary>
/// EXPERIMENTAL: Selective resolution scaling using QualitySettings.renderScale
/// This scales game content while preserving UI at native resolution
/// </summary>
public class SelectiveRenderScalePatch
{
    private static bool _enabled = false;
    private static float _renderScale = 1.0f;
    private static bool _hasApplied = false;

    /// <summary>
    /// Apply resolution scaling using QualitySettings.renderScale
    /// This scales 3D rendering and sprites while keeping UI at native resolution
    /// </summary>
    [HarmonyPatch(typeof(GSDTitleSelect), nameof(GSDTitleSelect.Main))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    static void OnTitleSelect(GSDTitleSelect __instance)
    {
        if (!_enabled || _renderScale == 1.0f || _hasApplied) return;

        // Apply after splash screens when sprite loading begins
        if (__instance.step >= (int)GSDTitleSelect.State.WaitSpriteLoad)
        {
            ApplyRenderScale();
        }
    }

    private static void ApplyRenderScale()
    {
        if (_hasApplied) return;
        _hasApplied = true;

        try
        {
            // Try QualitySettings.renderScale (Unity 2022+)
            // This scales the rendering resolution while keeping UI at native resolution
            QualitySettings.renderScale = _renderScale;
            
            Plugin.Log.LogInfo($"========================================");
            Plugin.Log.LogInfo($"[EXPERIMENTAL] Selective Render Scale");
            Plugin.Log.LogInfo($"✓ Applied QualitySettings.renderScale = {_renderScale}x");
            Plugin.Log.LogInfo($"  Game content: Rendered at {_renderScale}x resolution");
            Plugin.Log.LogInfo($"  UI: Preserved at native resolution");
            Plugin.Log.LogInfo($"========================================");
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"[EXPERIMENTAL] Failed to apply renderScale: {ex.Message}");
            Plugin.Log.LogWarning("Your Unity version may not support QualitySettings.renderScale");
            Plugin.Log.LogWarning("This is expected - the game may not support this feature");
            Plugin.Log.LogWarning("Falling back to no scaling");
        }
    }

    public static void Initialize(float renderScale)
    {
        _enabled = true;
        _renderScale = renderScale;

        Plugin.Log.LogInfo($"========================================");
        Plugin.Log.LogInfo($"[EXPERIMENTAL] Selective Render Scale Initialized");
        Plugin.Log.LogInfo($"Render Scale: {_renderScale}x");
        Plugin.Log.LogInfo($"");
        Plugin.Log.LogInfo($"How it works:");
        Plugin.Log.LogInfo($"  - Game content (sprites, 3D) renders at {_renderScale}x");
        Plugin.Log.LogInfo($"  - UI (menus, text) stays at native resolution");
        Plugin.Log.LogInfo($"  - Result: Better performance with crisp UI!");
        Plugin.Log.LogInfo($"");
        Plugin.Log.LogInfo($"Example at 1920x1080:");
        Plugin.Log.LogInfo($"  Game: {(int)(1920 * _renderScale)}x{(int)(1080 * _renderScale)} ({_renderScale}x)");
        Plugin.Log.LogInfo($"  UI: 1920x1080 (native)");
        Plugin.Log.LogInfo($"========================================");
    }
}
*/
