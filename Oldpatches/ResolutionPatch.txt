using HarmonyLib;
using UnityEngine;

namespace PKCore.Patches;

public class ResolutionPatch
{
    private static bool _enabled = false;
    private static float _resolutionScale = 1.0f;
    private static bool _isApplyingScale = false;
    private static bool _hasAppliedOnce = false;
    private static bool _hasAppliedAfterIntro = false;

    // Intercept ALL SetResolution calls with LAST priority
    [HarmonyPatch(typeof(Screen), nameof(Screen.SetResolution), new[] { typeof(int), typeof(int), typeof(FullScreenMode) })]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    static void SetResolution(ref int width, ref int height, ref FullScreenMode fullscreenMode)
    {
        if (_isApplyingScale) return;
        
        if (_enabled && _resolutionScale != 1.0f)
        {
            int originalWidth = width;
            int originalHeight = height;

            width = (int)(width * _resolutionScale);
            height = (int)(height * _resolutionScale);
            fullscreenMode = FullScreenMode.FullScreenWindow;

            if (!_hasAppliedOnce)
            {
                Plugin.Log.LogInfo($"✓ First SetResolution intercept: {originalWidth}x{originalHeight} -> {width}x{height}");
                _hasAppliedOnce = true;
            }
        }
    }


    [HarmonyPatch(typeof(Screen), nameof(Screen.SetResolution), new[] { typeof(int), typeof(int), typeof(bool) })]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    static void SetResolution2(ref int width, ref int height, ref bool fullscreen)
    {
        if (_isApplyingScale) return;
        
        if (_enabled && _resolutionScale != 1.0f)
        {
            int originalWidth = width;
            int originalHeight = height;

            width = (int)(width * _resolutionScale);
            height = (int)(height * _resolutionScale);
            fullscreen = true;

            // Log the resolution change if it's not the first time
            if (_hasAppliedOnce)
            {
                Plugin.Log.LogInfo($"✓ Second+ SetResolution intercept: {originalWidth}x{originalHeight} -> {width}x{height}");
            }
        }
    }

    // Apply resolution after splash screens complete (when title screen appears)
    // This works whether splash screens are shown or skipped by Suikoden Fix
    [HarmonyPatch(typeof(GSDTitleSelect), nameof(GSDTitleSelect.Main))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    static void OnTitleSelect(GSDTitleSelect __instance)
    {
        if (!_enabled || _resolutionScale == 1.0f || _hasAppliedAfterIntro) return;

        // Check if we've passed the splash screens (Konami logo) and reached sprite loading
        // WaitSpriteLoad happens after splash screens OR immediately if skipped
        // Also apply during later states to catch all cases
        if (__instance.step >= (int)GSDTitleSelect.State.WaitSpriteLoad)
        {
            ApplyResolutionScaling("after splash screens");
        }
    }

    private static void ApplyResolutionScaling(string reason)
    {
        if (_hasAppliedAfterIntro) return;
        _hasAppliedAfterIntro = true;

        // Use native display resolution as base
        int baseWidth = Display.main.systemWidth;
        int baseHeight = Display.main.systemHeight;

        int targetWidth = (int)(baseWidth * _resolutionScale);
        int targetHeight = (int)(baseHeight * _resolutionScale);

        // Clamp to display bounds (disabled to allow supersampling)
        // targetWidth = Mathf.Clamp(targetWidth, 640, baseWidth);
        // targetHeight = Mathf.Clamp(targetHeight, 480, baseHeight);

        Plugin.Log.LogInfo($"✓ Applying resolution {reason}: {baseWidth}x{baseHeight} -> {targetWidth}x{targetHeight}");

        _isApplyingScale = true;
        Screen.SetResolution(targetWidth, targetHeight, FullScreenMode.FullScreenWindow);
        _isApplyingScale = false;
    }

    public static void Initialize()
    {
        _enabled = Plugin.Config.EnableResolutionScaling.Value;
        _resolutionScale = Plugin.Config.ResolutionScale.Value;

        Plugin.Log.LogInfo($"========================================");
        Plugin.Log.LogInfo($"Resolution Scaling Enabled");
        Plugin.Log.LogInfo($"Scale: {_resolutionScale}x (dynamic based on window size)");
        Plugin.Log.LogInfo($"Example: 1920x1080 at {_resolutionScale}x = {(int)(1920 * _resolutionScale)}x{(int)(1080 * _resolutionScale)}");
        Plugin.Log.LogInfo($"========================================");
        Plugin.Log.LogWarning("IMPORTANT: If using Suikoden Fix, set these in d3xMachina.suikoden_fix.cfg:");
        Plugin.Log.LogWarning("  Width = -1");
        Plugin.Log.LogWarning("  Height = -1");
        Plugin.Log.LogWarning("  Fullscreen = -1");
        Plugin.Log.LogWarning("This prevents conflicts between the two resolution systems!");
        Plugin.Log.LogInfo($"========================================");
    }
}
