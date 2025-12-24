using HarmonyLib;
using UnityEngine;
using System;

namespace PKCore.Patches;

/// <summary>
/// Patches for summon effect texture replacement
/// Handles M_GATE summon objects and their particle effect textures
/// Target textures: Eff_tex_Summon_01, Eff_tex_Summon_02_head_ren_01, Eff_tex_Summon_07, 
///                  Eff_tex_Summon_10, Eff_tex_Summon_11, Eff_tex_Summon_12, Eff_tex_Summon_13
/// </summary>
public partial class CustomTexturePatch
{
    /// <summary>
    /// Intercept GameObject.SetActive to detect when M_GATE summon objects are activated
    /// User hierarchy: Field > Character > HDeffect > M_GATE... > ... > ParticleSystemRenderer
    /// </summary>
    [HarmonyPatch(typeof(GameObject), nameof(GameObject.SetActive))]
    [HarmonyPostfix]
    public static void GameObject_SetActive_Summon_Postfix(GameObject __instance, bool value)
    {
        // Only scan when activating
        if (!value || !Plugin.Config.EnableCustomTextures.Value)
            return;

        // Check if this is an M_GATE summon object
        if (!__instance.name.StartsWith("M_GATE"))
            return;

        if (Plugin.Config.DetailedTextureLog.Value)
        {
            Plugin.Log.LogInfo($"[Summon] M_GATE object activated: {__instance.name}");
        }

        // Scan this M_GATE object's children for renderers with summon textures
        ScanAndReplaceSummonTextures(__instance);
    }

    /// <summary>
    /// Scan a M_GATE GameObject hierarchy for particle system renderers with summon textures
    /// Based on user hierarchy: M_GATE > ... > ParticleSystemRenderer > Material > mainTexture
    /// </summary>
    private static void ScanAndReplaceSummonTextures(GameObject mGateObject)
    {
        // Get all renderers in the M_GATE hierarchy
        var renderers = mGateObject.GetComponentsInChildren<Renderer>(true);
        
        int replaced = 0;
        foreach (var renderer in renderers)
        {
            if (renderer == null || renderer.sharedMaterial == null)
                continue;

            string matName = renderer.sharedMaterial.name;
            
            // Check if this is a summon material (contains M_GATE in the material name)
            if (!matName.Contains("M_GATE"))
                continue;

            // Access the material instance
            Material mat = renderer.material;
            if (mat == null || mat.mainTexture == null)
                continue;

            Texture mainTex = mat.mainTexture;
            string textureName = mainTex.name;

            // Check if this is a Texture2D we can replace
            if (mainTex is Texture2D tex2D)
            {
                if (HasCustomTexture(textureName))
                {
                    int textureId = tex2D.GetInstanceID();
                    if (!processedTextureIds.Contains(textureId))
                    {
                        processedTextureIds.Add(textureId);
                        bool success = ReplaceTextureInPlace(tex2D, textureName);
                        
                        if (success)
                        {
                            replaced++;
                            if (Plugin.Config.DetailedTextureLog.Value)
                            {
                                Plugin.Log.LogInfo($"[Summon] Replaced texture '{textureName}' on renderer '{renderer.name}' (Material: {matName})");
                            }
                        }
                    }
                }
            }
        }

        if (replaced > 0)
        {
            Plugin.Log.LogInfo($"[Summon] Replaced {replaced} summon texture(s) in {mGateObject.name}");
        }
    }
}
