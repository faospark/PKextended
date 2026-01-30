using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace PKCore.Patches;

/// <summary>
/// Patches for Material.mainTexture replacement - handles ALL material texture setters
/// Provides comprehensive DDS and standard texture support for any material
/// </summary>
public partial class CustomTexturePatch
{
    // Track logged materials to prevent duplicate logging
    private static readonly HashSet<string> loggedMaterials = new HashSet<string>();

    /// <summary>
    /// Intercept Material.mainTexture setter to replace textures for all materials
    /// HIGH PRIORITY to run before summon-specific patches
    /// </summary>
    [HarmonyPatch(typeof(Material), nameof(Material.mainTexture), MethodType.Setter)]
    [HarmonyPrefix, HarmonyPriority(Priority.High)]
    public static void Material_set_mainTexture_General_Prefix(Material __instance, ref Texture value)
    {
        if (value == null || !Plugin.Config.EnableCustomTextures.Value)
            return;

        string textureName = value.name;
        if (string.IsNullOrEmpty(textureName))
            return;

        // DEBUG: Log material texture assignments only once and only when detailed logs are enabled
        if (Plugin.Config.DetailedTextureLog.Value)
        {
            string materialKey = $"{__instance.name}:{textureName}";
            if (loggedMaterials.Add(materialKey))
            {
                Plugin.Log.LogInfo($"[Material Debug] Setting mainTexture: {textureName} on material: {__instance.name}");
            }
        }

        // Skip if already processed or modified
        if (textureName.EndsWith("_Custom"))
            return;

        // Try in-place replacement first (works for most standard formats)
        if (value is Texture2D texture2D)
        {
            if (ReplaceTextureInPlace(texture2D, textureName))
            {
                if (Plugin.Config.DetailedTextureLog.Value)
                {
                    Plugin.Log.LogInfo($"[Material] In-place replaced: {textureName}");
                }
                return;
            }
            
            // Fallback: Load custom texture and replace reference (works for DDS and all formats)
            Texture2D customTexture = LoadCustomTexture(textureName);
            if (customTexture != null && customTexture != texture2D)
            {
                value = customTexture;
                if (Plugin.Config.DetailedTextureLog.Value)
                {
                    Plugin.Log.LogInfo($"[Material] Replaced mainTexture: {textureName} ({customTexture.format})");
                }
            }
            else if (Plugin.Config.DetailedTextureLog.Value && HasCustomTexture(textureName))
            {
                Plugin.Log.LogWarning($"[Material] Custom texture found but failed to load: {textureName}");
            }
        }
    }

    /// <summary>
    /// Intercept Material.mainTexture getter to catch textures loaded before our patches
    /// </summary>
    [HarmonyPatch(typeof(Material), nameof(Material.mainTexture), MethodType.Getter)]
    [HarmonyPostfix]
    public static void Material_get_mainTexture_General_Postfix(Material __instance, ref Texture __result)
    {
        if (__result == null || !Plugin.Config.EnableCustomTextures.Value)
            return;

        string textureName = __result.name;
        if (string.IsNullOrEmpty(textureName) || textureName.EndsWith("_Custom"))
            return;

        // Try to replace if we have a custom version
        if (__result is Texture2D texture2D)
        {
            // First try in-place replacement
            if (ReplaceTextureInPlace(texture2D, textureName))
            {
                if (Plugin.Config.DetailedTextureLog.Value)
                {
                    Plugin.Log.LogInfo($"[Material Get] In-place replaced: {textureName}");
                }
                return;
            }

            // Fallback: Load custom texture (supports DDS)
            Texture2D customTexture = LoadCustomTexture(textureName);
            if (customTexture != null && customTexture != texture2D)
            {
                __result = customTexture;
                __instance.mainTexture = customTexture; // Update the material too
                if (Plugin.Config.DetailedTextureLog.Value)
                {
                    Plugin.Log.LogInfo($"[Material Get] Replaced texture: {textureName} ({customTexture.format})");
                }
            }
        }
    }
}