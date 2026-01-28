using HarmonyLib;
using UnityEngine;

namespace PKCore.Patches;

/// <summary>
/// Patches to apply custom scaling and positioning to Suikoden 1 world map UI
/// Scales down the world map UI element to 0.8x with adjusted position for better visibility
/// </summary>
public class S1WorldMapScaleUIPatch
{
    public static void Initialize()
    {
        // Check if feature is enabled
        if (!Plugin.Config.S1ScaledDownWorldMap.Value)
        {
            return;
        }
        
        Plugin.Log.LogInfo("[S1WorldMapScaleUI] Initialized - will scale world map UI on next open");
    }

    /// <summary>
    /// Patch for when world map opens - apply transform to smap (world map UI element)
    /// This hooks into the world map scene initialization
    /// </summary>
    [HarmonyPatch(typeof(GameObject), nameof(GameObject.SetActive))]
    [HarmonyPostfix]
    public static void GameObject_SetActive_Postfix(GameObject __instance)
    {
        if (!Plugin.Config.S1ScaledDownWorldMap.Value)
            return;

        // Only process world map related objects
        if (__instance.name != "smap" && __instance.name != "S1_Map")
            return;

        if (__instance.name == "smap")
        {
            ApplyWorldMapTransform(__instance);
        }
    }

    /// <summary>
    /// Apply scale and position transformation to the world map UI element
    /// </summary>
    private static void ApplyWorldMapTransform(GameObject smapObject)
    {
        try
        {
            // Get the transform
            Transform smapTransform = smapObject.transform;
            
            // Apply scale: 0.8 0.8 1
            smapTransform.localScale = new Vector3(0.8f, 0.8f, 1f);
            
            // Apply position: 652.0001 -355.3 0
            smapTransform.localPosition = new Vector3(652.0001f, -355.3f, 0f);
            
            Plugin.Log.LogInfo($"[S1WorldMapScaleUI] Applied transform to smap - scale: (0.8, 0.8, 1), position: (652.0001, -355.3, 0)");
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"[S1WorldMapScaleUI] Error applying transform: {ex.Message}");
        }
    }
}
