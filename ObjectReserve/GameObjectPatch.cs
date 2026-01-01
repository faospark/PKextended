using HarmonyLib;
using UnityEngine;
using System;

namespace PKCore.Patches;

/// <summary>
/// Patches for GameObject.SetActive
/// Handles detection and replacement of textures when GameObjects are activated
/// </summary>
public partial class CustomTexturePatch
{
    private static System.Collections.Generic.HashSet<string> _loggedSpriteReplacements = new System.Collections.Generic.HashSet<string>();
    /// <summary>
    /// Intercept GameObject.SetActive to catch sprites when objects are activated
    /// This catches sprites in objects that are instantiated/activated after scene load
    /// Specifically handles bath backgrounds and bgManagerHD objects
    /// </summary>
    [HarmonyPatch(typeof(GameObject), nameof(GameObject.SetActive))]
    [HarmonyPostfix]
    public static void GameObject_SetActive_Postfix(GameObject __instance, bool value)
    {
        // Only scan when activating (not deactivating)
        if (!value || !Plugin.Config.EnableCustomTextures.Value && !Plugin.Config.LogReplaceableTextures.Value)
            return;

        // Get the full path of the activated object
        string objectPath = GetGameObjectPath(__instance);
        
        
        // Check if this is a background manager object
        // Suikoden 2: bgManagerHD
        // Suikoden 1: MapBackGround
        // Also scan 3D objects (contains FieldObject MeshRenderers) HDEffect
        bool isBgManager = objectPath.Contains("bgManagerHD") || objectPath.Contains("MapBackGround") || objectPath.Contains("3D") || objectPath.Contains("HDEffect") || objectPath.Contains("HDFishingBG");
        
        // Handle background manager activation - scan for sprites to replace
        if (isBgManager)
        {
            if (Plugin.Config.DetailedTextureLog.Value)
            {
                Plugin.Log.LogInfo($"Background manager activated: {objectPath}");
            }
            
            // Scan for SpriteRenderers and replace sprites (excluding save points, handled in SavePointPatch.cs)
            var spriteRenderers = __instance.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in spriteRenderers)
            {
                if (sr.sprite != null)
                {
                    string spriteName = sr.sprite.name;
                    
                    // Attach dragon monitor if applicable
                    DragonPatch.CheckAndAttachMonitor(sr.gameObject);

                    // Attach save point monitor if applicable (Handles both S1 and S2)
                    SavePointPatch.CheckAndAttachMonitor(sr.gameObject);

                    // Skip processing if we already handled it as a specialized type
                    // Actually, let's allow it to fall through to generic replacement if not handled?
                    // But typically monitors handle their own replacement.
                    
                    // For now, removing the "continue" that skipped save points allows generic replacement 
                    // to happen IN ADDITION to the monitor if the monitor finds it. 
                    // However, SavePointPatch logic is robust enough to handle it.
                    // But to be safe and avoid fighting, let's utilize the monitor exclusively if it attaches.
                    
                    if (sr.GetComponent<SavePointSpriteMonitor>() != null)
                        continue;
                    
                    Sprite customSprite = LoadCustomSprite(spriteName, sr.sprite);
                    if (customSprite != null)
                    {
                        sr.sprite = customSprite;
                        
                        // Only log once per sprite name to prevent spam
                        if (!_loggedSpriteReplacements.Contains(spriteName))
                        {
                            _loggedSpriteReplacements.Add(spriteName);
                            Plugin.Log.LogInfo($"Replaced sprite on activation: {spriteName} (from {objectPath})");
                        }
                    }
                    
            // Attach cow monitor if applicable
                    CowTexturePatch.CheckAndAttachMonitor(sr.gameObject);
                }
            }
            
            // Check for MeshRenderers and replace their textures
            var meshRenderers = __instance.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var mr in meshRenderers)
            {
                if (mr.material != null && mr.material.HasProperty("_MainTex") && mr.material.mainTexture is Texture2D texture)
                {
                    string textureName = texture.name;
                    if (!string.IsNullOrEmpty(textureName))
                    {
                        if (ReplaceTextureInPlace(texture, textureName))
                        {
                            if (!_loggedSpriteReplacements.Contains(textureName))
                            {
                                _loggedSpriteReplacements.Add(textureName);
                                Plugin.Log.LogInfo($"Replaced MeshRenderer texture on activation: {textureName} (from {objectPath})");
                            }
                        }
                    }
                }
            }
            
            // Check for SuikozuObj and replace its MeshRenderer texture
            if (__instance.name.Contains("Suikozu", StringComparison.OrdinalIgnoreCase))
            {
                var meshRenderer = __instance.GetComponent<MeshRenderer>();
                if (meshRenderer != null && meshRenderer.material != null && meshRenderer.material.mainTexture is Texture2D texture)
                {
                    string textureName = texture.name;
                    if (textureName != null && textureName.StartsWith("suikozu_", StringComparison.OrdinalIgnoreCase))
                    {
                        if (ReplaceTextureInPlace(texture, textureName))
                        {
                            Plugin.Log.LogInfo($"[Suikozu] ✓ Replaced texture on activation: {textureName}");
                        }
                    }
                }
            }

            // Create custom objects when the scene clone is activated or found in children
            if (Plugin.Config.EnableCustomObjects.Value)
            {
                // Skip our own custom objects to prevent recursion
                if (objectPath.Contains("custom_test_object"))
                {
                    return;
                }
                
                // Only trigger on the actual Scene Root (e.g. vk16_00(Clone))
                // effectively ignoring children like "xzon" even if they are in the clone hierarchy
                if (__instance.name.EndsWith("(Clone)"))
                {
                    PKCore.Patches.CustomObjectInsertion.TryCreateCustomObjects(__instance);
                    
                    // Discover existing objects if logging is enabled
                    if (Plugin.Config.LogExistingMapObjects.Value)
                    {
                        PKCore.Utils.ObjectDiscovery.DiscoverObjectsInScene(__instance);
                    }
                }
                else if (objectPath.EndsWith("bgManagerHD"))
                {
                    // If bgManagerHD is activated, look for the Clone child (use GetChild for IL2CPP)
                    for (int i = 0; i < __instance.transform.childCount; i++)
                    {
                        Transform child = __instance.transform.GetChild(i);
                        if (child.name.EndsWith("(Clone)"))
                        {
                            PKCore.Patches.CustomObjectInsertion.TryCreateCustomObjects(child.gameObject);
                            
                            // Discover existing objects if logging is enabled
                            if (Plugin.Config.LogExistingMapObjects.Value)
                            {
                                PKCore.Utils.ObjectDiscovery.DiscoverObjectsInScene(child.gameObject);
                            }
                            break; // Assume only one map clone active
                        }
                    }
                }
            }
        }
    }
}
