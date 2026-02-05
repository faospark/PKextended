using HarmonyLib;
using UnityEngine;
using System;

namespace PKCore.Patches;

/// <summary>
/// Patch to disable world map effects like clouds and sunrays
/// Clouds:
///   Suikoden 1: AppRoot/Map/MapBackGround/b_data(Clone)/Rot/Scroll/Scaler/sm_wk_cloud
///   Suikoden 2: bgManagerHD/wb(Clone)/Rot/Scroll/Scaler/sm_wk_cloud
/// Sunrays:
///   Suikoden 1: AppRoot/Map/MapBackGround/b_data(Clone)/eff_world_sunLight
///   Suikoden 2: bgManagerHD/wb(Clone)/eff_world_sunLight
/// </summary>
public static class WorldMapEffectsPatch
{
    private static bool _isProcessing = false;
    
    [HarmonyPatch(typeof(GameObject), nameof(GameObject.SetActive))]
    [HarmonyPostfix]
    public static void GameObject_SetActive_Postfix(GameObject __instance, bool value)
    {
        // Re-entry guard to prevent infinite recursion
        if (_isProcessing)
            return;
            
        // Early exit: logic only runs when activating and config is enabled
        if (!value || !Plugin.Config.DisableWorldMapClouds.Value)
            return;

        try
        {
            _isProcessing = true;
            
            // 1. Check if the object itself is a cloud or sunray
            if (__instance.name == "sm_wk_cloud" && Plugin.Config.DisableWorldMapClouds.Value)
            {
                __instance.SetActive(false);
                LogEffectDisabled(__instance, "Cloud", "Direct Activation");
                return;
            }
            
            if (__instance.name == "eff_world_sunLight" && Plugin.Config.DisableWorldMapSunrays.Value)
            {
                __instance.SetActive(false);
                LogEffectDisabled(__instance, "Sunray", "Direct Activation");
                return;
            }

            // 2. Check if the object is the world map container
            // S2: bgManagerHD
            // S1: MapBackGround or any *_data prefab (b_data, f_data, etc.)
            // Note: Unity adds (Clone) suffix to instantiated prefabs
            string name = __instance.name;
            bool isMapContainer = name.StartsWith("bgManagerHD") || 
                                 name.StartsWith("MapBackGround") || 
                                 name.Contains("_data");
            
            if (isMapContainer)
            {
                 // Diagnostic logging
                 if (Plugin.Config.DetailedTextureLog.Value)
                 {
                     Plugin.Log.LogInfo($"[WorldMapEffects] Detected map container activation: {name}");
                 }
                 
                 // Scan for cloud and sunray effects in children
                 Transform[] transforms = __instance.GetComponentsInChildren<Transform>(true);
                 foreach (Transform t in transforms)
                 {
                     // Disable clouds
                     if (Plugin.Config.DisableWorldMapClouds.Value && t.name == "sm_wk_cloud" && t.gameObject.activeSelf)
                     {
                         t.gameObject.SetActive(false);
                         LogEffectDisabled(t.gameObject, "Cloud", "Parent Activation");
                     }
                     
                     // Disable sunrays
                     if (Plugin.Config.DisableWorldMapSunrays.Value && t.name == "eff_world_sunLight" && t.gameObject.activeSelf)
                     {
                         t.gameObject.SetActive(false);
                         LogEffectDisabled(t.gameObject, "Sunray", "Parent Activation");
                     }
                 }
            }
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private static void LogEffectDisabled(GameObject obj, string effectType, string reason)
    {
        string logKey = $"sm_wk_{effectType.ToLower()}_{obj.GetInstanceID()}";
        if (Plugin.Config.DetailedTextureLog.Value && !CustomTexturePatch.IsTextureLogged(logKey))
        {
            Plugin.Log.LogInfo($"[WorldMapEffects] Disabled {effectType} ({reason}): {CustomTexturePatch.GetGameObjectPath(obj)}");
            CustomTexturePatch.LogReplaceableTexture(logKey, "WorldMapObject", $"Disabled {effectType}");
        }
    }
}
