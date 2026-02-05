using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace PKCore.Patches;

/// <summary>
/// Specialized support for Suikoden 2 Summon effects
/// Summon effects use ParticleSystemRenderer and materials that are often loaded
/// before our standard patches can intercept them. This class provides the 
/// necessary hooks for in-place texture replacement.
/// </summary>
[HarmonyPatch]
public partial class CustomTexturePatch
{
    private static bool hasScannedParticleEffects = false;
    private static float lastSummonTriggerTime = 0f;
    private static bool _isRegistered = false;

    private static void EnsureRegistered()
    {
        if (_isRegistered) return;
        try
        {
            Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<SummonMonitor>();
            _isRegistered = true;
        }
        catch { }
    }

    public class SummonMonitor : MonoBehaviour
    {
        private float _lastScanTime = 0f;
        
        void Awake()
        {
            if (Plugin.Config.DetailedTextureLog.Value)
                Plugin.Log.LogInfo($"[SummonMonitor] Attached to {gameObject.name}");
        }

        void Update()
        {
            // Scan every 0.3s instead of every frame for performance
            if (Time.time - _lastScanTime < 0.3f) return;
            _lastScanTime = Time.time;

            // Find all renderers in children - some might be added dynamically
            var renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers == null) return;

            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                
                // Fast path: check sharedMaterial first to avoid creating material instance
                Material mat = renderer.sharedMaterial;
                if (mat == null) continue;

                Texture mainTex = mat.mainTexture;
                if (mainTex != null && mainTex is Texture2D texture2D)
                {
                    string name = texture2D.name;
                    if (string.IsNullOrEmpty(name) || name.EndsWith("_Custom")) continue;

                    string cleanedName = CleanSactxName(name);
                    if (summonTextureNames.Contains(cleanedName) || 
                        cleanedName.StartsWith("m_gat", StringComparison.OrdinalIgnoreCase) ||
                        cleanedName.StartsWith("Eff_tex_Summon_", StringComparison.OrdinalIgnoreCase))
                    {
                        if (ReplaceTextureInPlace(texture2D, name))
                        {
                            if (Plugin.Config.DetailedTextureLog.Value)
                                Plugin.Log.LogInfo($"[SummonMonitor] Replaced: {name}");
                        }
                    }
                }
            }
        }
    }
    private static readonly HashSet<string> summonTextureNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        //summon 1
        "Eff_tex_Summon_01",  
        //summon 2
        "Eff_tex_Summon_11", "Eff_tex_Summon_12", "Eff_tex_Summon_13",
        //summon 3
        "Eff_tex_Summon_10",
        //summon 4 
        "Eff_tex_Summon_07",
        "Eff_tex_Summon_02_head_ren_01"
    };

    /// <summary>
    /// Proactively scan and replace particle effect textures in HDeffects GameObject
    /// This catches textures that were set before our patches were active
    /// </summary>
    private static void ScanAndReplaceParticleEffectTextures()
    {
        if (!Plugin.Config.EnableCustomTextures.Value)
            return;

        // Find the HDeffects GameObject (created during summon effects)
        var hdEffects = GameObject.Find("AppRoot/Field/Character/HDEffect");
        if (hdEffects == null)
            return;

        if (Plugin.Config.DetailedTextureLog.Value)
            Plugin.Log.LogInfo("[Proactive Scan] Found HDEffect GameObject, scanning children...");

        // Get all Renderer components in children (includes ParticleSystemRenderer)
        var renderers = hdEffects.GetComponentsInChildren<Renderer>(true);
        int replacedCount = 0;
        
        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;
            
            // For renderers, we check materials
            Material mat = null;
            try { mat = renderer.material; } catch { }
            
            if (mat == null) continue;
            
            Texture mainTex = mat.mainTexture;
            if (mainTex != null && mainTex is Texture2D texture2D)
            {
                string textureName = texture2D.name;
                int textureId = texture2D.GetInstanceID();
                    
                // Only process each texture instance once
                if (!processedTextureIds.Contains(textureId))
                {
                    if (ReplaceTextureInPlace(texture2D, textureName))
                    {
                        processedTextureIds.Add(textureId);
                        replacedCount++;
                        if (Plugin.Config.DetailedTextureLog.Value)
                            Plugin.Log.LogInfo($"[Proactive Scan] ✅ Replaced particle texture: {textureName} (GameObject: {renderer.gameObject.name})");
                    }
                }
            }
        }

        if (replacedCount > 0 && Plugin.Config.DetailedTextureLog.Value)
        {
            Plugin.Log.LogInfo($"[Proactive Scan] ✅ Replaced {replacedCount} particle effect texture(s) in HDeffects");
        }
    }

    private static void CheckAndTriggerSummonReplacement(string textureName)
    {
        if (string.IsNullOrEmpty(textureName)) return;

        // Skip if we just triggered a scan recently (avoid lag)
        if (Time.time - lastSummonTriggerTime < 0.5f) return;

        // Clean name to handle sactx- patterns
        string cleanedName = CleanSactxName(textureName);

        bool isGateRuneTrigger = cleanedName.StartsWith("m_gat", StringComparison.OrdinalIgnoreCase);
        bool isSummonTex = cleanedName.StartsWith("Eff_tex_Summon_", StringComparison.OrdinalIgnoreCase);
        bool isAtlas = cleanedName.IndexOf("atlas", StringComparison.OrdinalIgnoreCase) >= 0;

        if ((isGateRuneTrigger && isAtlas) || isSummonTex)
        {
            lastSummonTriggerTime = Time.time;
            
            if (Plugin.Config.DetailedTextureLog.Value)
            {
                Plugin.Log.LogInfo($"[Summon Trigger] Summon texture detected: {textureName}. forcing batch replacements...");
            }
            
            // Collect all textures to replace in this batch
            HashSet<string> texturesToReplace = new HashSet<string>(summonTextureNames, StringComparer.OrdinalIgnoreCase);
            
            // Also include the trigger atlas itself if it's in our index
            if (texturePathIndex.ContainsKey(cleanedName) && !textureName.EndsWith("_Custom"))
            {
                texturesToReplace.Add(cleanedName);
            }

            // Perform a SINGLE scan of all textures
            BatchForceReplaceTextures(texturesToReplace);

            // Also trigger a proactive scan of HDEffects if it hasn't been done recently
            ScanAndReplaceParticleEffectTextures();
        }
    }

    /// <summary>
    /// Replaces multiple textures in a single scan of Resources.FindObjectsOfTypeAll
    /// </summary>
    private static void BatchForceReplaceTextures(HashSet<string> lookupNames)
    {
        if (!Plugin.Config.EnableCustomTextures.Value || lookupNames.Count == 0)
            return;

        // Find all Texture2D objects in the scene
        var allTextures = Resources.FindObjectsOfTypeAll<Texture2D>();
        int replacedCount = 0;

        foreach (var texture in allTextures)
        {
            if (texture == null) continue;
            
            string originalName = texture.name;
            if (string.IsNullOrEmpty(originalName) || originalName.EndsWith("_Custom")) continue;

            string currentTexName = CleanSactxName(originalName);
            bool isSummonTex = lookupNames.Contains(currentTexName) || 
                               currentTexName.StartsWith("m_gat", StringComparison.OrdinalIgnoreCase) ||
                               currentTexName.StartsWith("m_gate", StringComparison.OrdinalIgnoreCase) ||
                               currentTexName.StartsWith("Eff_tex_Summon_", StringComparison.OrdinalIgnoreCase);
            
            if (isSummonTex)
            {
                int textureId = texture.GetInstanceID();
                if (!processedTextureIds.Contains(textureId))
                {
                    if (ReplaceTextureInPlace(texture, originalName))
                    {
                        processedTextureIds.Add(textureId);
                        replacedCount++;
                        if (Plugin.Config.DetailedTextureLog.Value && originalName.Contains("Summon"))
                            Plugin.Log.LogInfo($"[Batch Replace] ✅ Replaced summon texture: {originalName}");
                    }
                    else if (originalName.Contains("Summon"))
                    {
                        if (Plugin.Config.DetailedTextureLog.Value)
                            Plugin.Log.LogDebug($"[Batch Replace] ❌ Failed to replace summon texture: {originalName} (Not in index or load error)");
                    }
                }
            }
        }

        if (replacedCount > 0 && Plugin.Config.DetailedTextureLog.Value)
            Plugin.Log.LogInfo($"[Batch Replace] ✅ Replaced {replacedCount} summon-related texture(s) in one scan");
    }

    /// <summary>
    /// Additional hook for GameObject.SetActive to trigger summon effect replacements
    /// </summary>
    [HarmonyPatch(typeof(GameObject), nameof(GameObject.SetActive))]
    [HarmonyPostfix]
    public static void S2Summon_GameObject_SetActive_Postfix(GameObject __instance, bool value)
    {
        if (!value || !Plugin.Config.EnableCustomTextures.Value)
            return;

        // Check if this is HDEffect or one of its children - trigger particle scan
        // Also check for M_GATE summon effect objects
        bool isHDEffect = __instance.name != null && (__instance.name.Equals("HDEffect", StringComparison.OrdinalIgnoreCase) || (__instance.transform.parent != null && __instance.transform.parent.name.Equals("HDEffect", StringComparison.OrdinalIgnoreCase)));
        bool isSummonEffect = __instance.name != null && (__instance.name.IndexOf("M_GATE", StringComparison.OrdinalIgnoreCase) >= 0 || __instance.name.IndexOf("M_GAT", StringComparison.OrdinalIgnoreCase) >= 0);
        
        if (isHDEffect || isSummonEffect)
        {
            EnsureRegistered();
            if (__instance.GetComponent<SummonMonitor>() == null)
            {
                __instance.AddComponent<SummonMonitor>();
            }

            if (Plugin.Config.DetailedTextureLog.Value)
                Plugin.Log.LogInfo($"[Summon Effect Activated] GameObject '{__instance.name}' activated, attached SummonMonitor.");
            
            CheckAndTriggerSummonReplacement("m_gat_atlas"); // Dummy trigger to run the list
        }
    }

    /// <summary>
    /// Additional hook for Sprite.texture to trigger summon effect replacements via atlas loading
    /// </summary>
    [HarmonyPatch(typeof(Sprite), nameof(Sprite.texture), MethodType.Getter)]
    [HarmonyPostfix]
    public static void S2Summon_Sprite_get_texture_Postfix(Sprite __instance, ref Texture2D __result)
    {
        if (__result == null || !Plugin.Config.EnableCustomTextures.Value)
            return;

        CheckAndTriggerSummonReplacement(__result.name);
    }

    /// <summary>
    /// Intercept Material.mainTexture setter to replace textures with in-place logic
    /// </summary>
    [HarmonyPatch(typeof(Material), nameof(Material.mainTexture), MethodType.Setter)]
    [HarmonyPrefix]
    public static void Material_set_mainTexture_Prefix(Material __instance, ref Texture value)
    {
        if (value == null || !Plugin.Config.EnableCustomTextures.Value)
            return;

        string textureName = value.name;
        if (string.IsNullOrEmpty(textureName))
            return;

        // Trigger summon replacement if this is an atlas being assigned to a material
        CheckAndTriggerSummonReplacement(textureName);

        // Try in-place replacement first (critical for summon effects!)
        if (value is Texture2D texture2D)
        {
            if (ReplaceTextureInPlace(texture2D, textureName))
            {
                // Success! All references see the new texture
                return;
            }
        }
    }

    /// <summary>
    /// Intercept Material.mainTexture getter to catch textures loaded before our patches
    /// </summary>
    [HarmonyPatch(typeof(Material), nameof(Material.mainTexture), MethodType.Getter)]
    [HarmonyPostfix]
    public static void Material_get_mainTexture_Postfix(Material __instance, ref Texture __result)
    {
        if (__result == null || !Plugin.Config.EnableCustomTextures.Value)
            return;

        if (__result is Texture2D texture2D)
        {
            string textureName = texture2D.name;
            if (string.IsNullOrEmpty(textureName)) return;

            // Trigger summon replacement
            CheckAndTriggerSummonReplacement(textureName);

            int textureId = texture2D.GetInstanceID();

            if (!processedTextureIds.Contains(textureId))
            {
                if (ReplaceTextureInPlace(texture2D, textureName))
                {
                    processedTextureIds.Add(textureId);
                    if (Plugin.Config.DetailedTextureLog.Value)
                        Plugin.Log.LogInfo($"[Getter] Replaced texture in-place: {textureName}");
                }
            }
        }
    }

    /// <summary>
    /// Intercept Renderer.material getters to catch textures in particle systems
    /// </summary>
    [HarmonyPatch(typeof(Renderer), nameof(Renderer.material), MethodType.Getter)]
    [HarmonyPostfix]
    public static void Renderer_get_material_Postfix(Renderer __instance, ref Material __result)
    {
        if (!Plugin.Config.EnableCustomTextures.Value)
            return;

        // Proactively scan for particle effects on first renderer access
        if (!hasScannedParticleEffects)
        {
            hasScannedParticleEffects = true;
            ScanAndReplaceParticleEffectTextures();
        }

        if (__result == null || __result.mainTexture == null)
            return;

        ProcessMaterialTexture(__instance, __result);
    }

    [HarmonyPatch(typeof(Renderer), nameof(Renderer.sharedMaterial), MethodType.Getter)]
    [HarmonyPostfix]
    public static void Renderer_get_sharedMaterial_Postfix(Renderer __instance, ref Material __result)
    {
        if (!Plugin.Config.EnableCustomTextures.Value || __result == null || __result.mainTexture == null)
            return;

        ProcessMaterialTexture(__instance, __result);
    }

    private static void ProcessMaterialTexture(Renderer renderer, Material material)
    {
        if (material.mainTexture is Texture2D texture2D)
        {
            string textureName = texture2D.name;
            if (string.IsNullOrEmpty(textureName)) return;

            // Trigger summon replacement
            CheckAndTriggerSummonReplacement(textureName);

            int textureId = texture2D.GetInstanceID();

            if (!processedTextureIds.Contains(textureId))
            {
                if (ReplaceTextureInPlace(texture2D, textureName))
                {
                    processedTextureIds.Add(textureId);
                    if (Plugin.Config.DetailedTextureLog.Value)
                        Plugin.Log.LogInfo($"[{renderer.GetType().Name}] Replaced texture in-place: {textureName} (GameObject: {renderer.gameObject.name})");
                }
            }
        }
    }
}
