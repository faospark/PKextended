using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace PKCore.Patches;

/// <summary>
/// Patches for UnityEngine.UI.Image component
/// Handles UI sprite replacement (cursor, UI elements, etc.)
/// </summary>
public partial class CustomTexturePatch
{
    [HarmonyPatch(typeof(Image), nameof(Image.sprite), MethodType.Setter)]
    [HarmonyPrefix]
    public static void Image_set_sprite_Prefix(Image __instance, ref Sprite value)
    {
        if (value == null || !Plugin.Config.EnableCustomTextures.Value)
            return;

        string originalName = value.name;
        
        // Special handling for cursor sprites - detect by GameObject name "Cursor"
        bool isUnnamedSprite = string.IsNullOrEmpty(originalName) || originalName == "untitled";
        if (isUnnamedSprite && __instance.gameObject.name == "Cursor")
        {
            string objectPath = GetGameObjectPath(__instance.gameObject);
            Plugin.Log.LogInfo($"[UI Cursor] Detected cursor sprite ('{originalName}') on {objectPath}");
            
            // Load custom cursor texture
            // old texture UI_Com_Arrow03_001
            string cursorTextureName = "UI_Com_Arrow03_001";
            Sprite cursorSprite = LoadCustomSprite(cursorTextureName, value);
            
            if (cursorSprite != null)
            {
                Plugin.Log.LogInfo($"[UI Cursor] Replaced with custom cursor: {cursorTextureName}");
                value = cursorSprite;
            }
            else
            {
                Plugin.Log.LogWarning($"[UI Cursor] Failed to load {cursorTextureName}, keeping original");
            }
            return;
        }

        // Skip unnamed sprite names (avoid log spam)
        if (string.IsNullOrEmpty(originalName) || originalName == "untitled")
            return;
        
        // Log replaceable UI sprites if enabled
        if (Plugin.Config.LogReplaceableTextures.Value && !loggedTextures.Contains(originalName))
        {
            loggedTextures.Add(originalName);
            string objectPath = GetGameObjectPath(__instance.gameObject);
            Plugin.Log.LogInfo($"[Replaceable UI Sprite] {originalName} (from {objectPath})");
        }
        
        // Try to replace with custom sprite
        Sprite customSprite = LoadCustomSprite(originalName, value);
        if (customSprite != null)
        {
            if (Plugin.Config.DetailedTextureLog.Value)
            {
                string objectPath = GetGameObjectPath(__instance.gameObject);
                Plugin.Log.LogInfo($"[UI Image] Replaced sprite: {originalName} on {objectPath}");
            }
            value = customSprite;
        }
    }
}
