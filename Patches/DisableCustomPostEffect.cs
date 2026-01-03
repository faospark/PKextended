using HarmonyLib;
using UnityEngine;
using System.Reflection;

namespace PKCore.Patches;

/// <summary>
/// Disables the CustomPostEffect component on the Camera GameObject under AppRoot
/// This prevents unwanted post-processing effects from being applied
/// </summary>
[HarmonyPatch]
public static class DisableCustomPostEffectPatch
{
    private static bool hasPatched = false;

    /// <summary>
    /// Manually patch CustomPostEffect.OnRenderImage to skip execution
    /// This is more reliable than trying to disable the component
    /// </summary>
    public static void Initialize(Harmony harmony)
    {
        if (hasPatched)
            return;

        try
        {
            Plugin.Log.LogInfo("[DisableCustomPostEffect] Searching for CustomPostEffect type...");
            
            // Find the CustomPostEffect type
            System.Type customPostEffectType = FindCustomPostEffectType();
            
            if (customPostEffectType == null)
            {
                Plugin.Log.LogWarning("[DisableCustomPostEffect] Could not find CustomPostEffect type");
                return;
            }

            // Find the OnRenderImage method
            var onRenderImageMethod = customPostEffectType.GetMethod("OnRenderImage", 
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (onRenderImageMethod == null)
            {
                Plugin.Log.LogWarning("[DisableCustomPostEffect] Could not find OnRenderImage method");
                return;
            }

            // Create prefix method to skip execution
            var prefixMethod = typeof(DisableCustomPostEffectPatch).GetMethod(nameof(OnRenderImage_Prefix), 
                BindingFlags.Public | BindingFlags.Static);

            // Apply the patch
            harmony.Patch(onRenderImageMethod, prefix: new HarmonyMethod(prefixMethod));
            
            Plugin.Log.LogInfo("[DisableCustomPostEffect] ✓ Successfully patched CustomPostEffect.OnRenderImage");
            hasPatched = true;
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"[DisableCustomPostEffect] Error patching CustomPostEffect: {ex.Message}");
            Plugin.Log.LogError($"[DisableCustomPostEffect] Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Prefix for CustomPostEffect.OnRenderImage - returns false to skip original method
    /// </summary>
    public static bool OnRenderImage_Prefix()
    {
        // Return false to skip the original OnRenderImage method
        // This effectively disables the post-processing effect
        return false;
    }

    /// <summary>
    /// Find the CustomPostEffect type in loaded assemblies
    /// </summary>
    private static System.Type FindCustomPostEffectType()
    {
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var assemblyName = assembly.GetName().Name;
            
            // Check game assemblies
            if (assemblyName == "GSD2" || assemblyName == "GSDShare" || assemblyName == "GSD1" || assemblyName == "Assembly-CSharp")
            {
                var type = assembly.GetType("CustomPostEffect");
                if (type != null)
                {
                    Plugin.Log.LogInfo($"[DisableCustomPostEffect] Found CustomPostEffect in {assemblyName}");
                    return type;
                }
            }
        }
        
        Plugin.Log.LogWarning("[DisableCustomPostEffect] CustomPostEffect type not found in any assembly");
        return null;
    }
}
