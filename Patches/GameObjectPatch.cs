using HarmonyLib;
using UnityEngine;
using System;
using ShareUI.Menu;

namespace PKCore.Patches;

/// <summary>
/// Patches for GameObject.SetActive and UI menu operations
/// Handles detection and replacement of textures when GameObjects are activated
/// Also handles TopMenuPartyList refresh for MenuTopPartyStatus texture fix
/// </summary>
public partial class CustomTexturePatch
{
    // Track processed sprite instances to prevent redundant replacements
    private static System.Collections.Generic.HashSet<int> _processedSpriteInstances = new System.Collections.Generic.HashSet<int>();

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
        bool isBgManager = objectPath.Contains("bgManagerHD") || objectPath.Contains("MapBackGround") || objectPath.Contains("3D") || objectPath.Contains("HDEffect") || objectPath.Contains("HDFishingBG") || objectPath.Contains("M_GATE");
        
        // Handle background manager activation - scan for sprites to replace
        if (isBgManager)
        {
            if (Plugin.Config.DetailedTextureLog.Value)
            {
                Plugin.Log.LogInfo($"Background manager activated: {objectPath}");
            }
            
            // Check for MeshRenderers and replace their textures
            var meshRenderers = __instance.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var mr in meshRenderers)
            {
                if (mr.material != null && mr.material.HasProperty("_MainTex") && mr.material.mainTexture is Texture2D texture)
                {
                    // Check and attach Suikozu monitor if applicable (for MeshRenderers too)
                    SuikozuPatch.CheckAndAttachMonitor(mr.gameObject);

                    string textureName = texture.name;
                    if (!string.IsNullOrEmpty(textureName))
                    {
                        if (ReplaceTextureInPlace(texture, textureName))
                        {
                            if (Plugin.Config.DetailedTextureLog.Value)
                            {
                                Plugin.Log.LogInfo($"Replaced MeshRenderer texture on activation: {textureName} (from {objectPath})");
                            }
                        }
                    }
                }
            }
            
            // Check main instance for Suikozu
            SuikozuPatch.CheckAndAttachMonitor(__instance);
            
            // Scan for SpriteRenderers (Unity standard)
            var spriteRenderers = __instance.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in spriteRenderers)
            {
                if (sr.sprite != null)
                {
                    int instanceId = sr.GetInstanceID();
                    
                    // Skip if already processed
                    if (_processedSpriteInstances.Contains(instanceId))
                        continue;
                    
                    string spriteName = sr.sprite.name;
                    
                    // Check and attach Dragon monitor if applicable
                    DragonPatch.CheckAndAttachMonitor(sr.gameObject);
                    
                    // Check and attach Cow monitor if applicable
                    CowTexturePatch.CheckAndAttachMonitor(sr.gameObject);
                    
                    // Skip save point sprites - they're handled in SavePointPatch.cs
                    if (spriteName.Contains("savePoint", StringComparison.OrdinalIgnoreCase))
                        continue;
                    
                    Sprite customSprite = LoadCustomSprite(spriteName, sr.sprite);
                    if (customSprite != null)
                    {
                        sr.sprite = customSprite;
                        _processedSpriteInstances.Add(instanceId);
                        
                        if (Plugin.Config.DetailedTextureLog.Value)
                        {
                            Plugin.Log.LogInfo($"Replaced sprite on activation: {spriteName} (from {objectPath})");
                        }
                    }
                }
            }
            
            // Scan for GRSpriteRenderers (game's custom renderer - used in S1)
            var grSpriteRenderers = __instance.GetComponentsInChildren<GRSpriteRenderer>(true);
            foreach (var gr in grSpriteRenderers)
            {
                if (gr.sprite != null)
                {
                    int instanceId = gr.GetInstanceID();
                    
                    // Skip if already processed
                    if (_processedSpriteInstances.Contains(instanceId))
                        continue;
                    
                    string spriteName = gr.sprite.name;
                    
                    // Check and attach Dragon monitor if applicable
                    DragonPatch.CheckAndAttachMonitor(gr.gameObject);
                    
                    // Check and attach Cow monitor if applicable
                    CowTexturePatch.CheckAndAttachMonitor(gr.gameObject);
                    
                    _processedSpriteInstances.Add(instanceId);
                }
            }
            
        }
    }

    /// <summary>
    /// Patch UIMainMenu.Open to refresh TopMenuPartyList for MenuTopPartyStatus texture fix
    /// This forces texture replacement by toggling the UI element
    /// </summary>
    [HarmonyPatch(typeof(UIMainMenu), nameof(UIMainMenu.Open))]
    [HarmonyPostfix]
    public static void UIMainMenu_Open_Postfix(UIMainMenu __instance)
    {
        // Only process if custom textures are enabled
        if (!Plugin.Config.EnableCustomTextures.Value) return;
        
        // Find and refresh TopMenuPartyList to force texture replacement
        var topMenuPartyList = FindTopMenuPartyList(__instance.gameObject);
        if (topMenuPartyList != null)
        {
            RefreshTopMenuPartyList(topMenuPartyList);
        }
    }

    /// <summary>
    /// Find TopMenuPartyList in the UIMainMenu hierarchy
    /// </summary>
    private static GameObject FindTopMenuPartyList(GameObject uiMainMenu)
    {
        // Try direct path: UIMainMenu -> UI_Set -> TopMenuPartyList
        var uiSet = uiMainMenu.transform.Find("UI_Set");
        if (uiSet != null)
        {
            var topMenuPartyList = uiSet.Find("TopMenuPartyList");
            if (topMenuPartyList != null)
                return topMenuPartyList.gameObject;
        }
        
        // Fallback: search all children
        var allTransforms = uiMainMenu.GetComponentsInChildren<Transform>(true);
        foreach (var transform in allTransforms)
        {
            if (transform.name == "TopMenuPartyList")
                return transform.gameObject;
        }
        
        return null;
    }

    /// <summary>
    /// Refresh TopMenuPartyList by toggling it to force texture replacement
    /// </summary>
    private static void RefreshTopMenuPartyList(GameObject topMenuPartyList)
    {
        // Toggle the entire TopMenuPartyList
        topMenuPartyList.SetActive(false);
        topMenuPartyList.SetActive(true);
        
        // Also refresh individual Img_BG objects with MenuTopPartyStatus
        var allImages = topMenuPartyList.GetComponentsInChildren<UnityEngine.UI.Image>(true);
        foreach (var image in allImages)
        {
            if (image.gameObject.name == "Img_BG")
            {
                // Check if this image has MenuTopPartyStatus texture
                bool hasMenuTopPartyStatus = false;
                
                if (image.sprite?.texture?.name == "MenuTopPartyStatus" || 
                    image.sprite?.name.Contains("MenuTopPartyStatus") == true)
                    hasMenuTopPartyStatus = true;
                    
                if (image.overrideSprite?.texture?.name == "MenuTopPartyStatus" ||
                    image.overrideSprite?.name.Contains("MenuTopPartyStatus") == true)
                    hasMenuTopPartyStatus = true;
                
                if (hasMenuTopPartyStatus)
                {
                    image.gameObject.SetActive(false);
                    image.gameObject.SetActive(true);
                }
            }
        }
    }
}
