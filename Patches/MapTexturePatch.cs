using System;
using HarmonyLib;
using UnityEngine;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace PKCore.Patches
{
    [HarmonyPatch(typeof(MapBGLoader))]
    public static class MapTexturePatch
    {
        [HarmonyPatch(nameof(MapBGLoader.GetMaterial))]
        [HarmonyPostfix]
        public static void GetMaterial_Postfix(Il2CppReferenceArray<Material> __result)
        {
            if (__result == null || !Plugin.Config.EnableCustomTextures.Value) return;

            foreach (var material in __result)
            {
                if (material == null) continue;

                string matName = material.name.Replace("(Instance)", "").Trim();
                
                // Use centralized texture system
                Texture2D replacement = CustomTexturePatch.LoadCustomTexture(matName);
                if (replacement != null)
                {
                    if (material.mainTexture != replacement)
                    {
                        if (Plugin.Config.DetailedTextureLog.Value)
                            Plugin.Log.LogInfo($"[MapTexturePatch] Replaced texture for material: {matName}");
                        
                        material.mainTexture = replacement;
                    }
                }
            }
        }
        
        // ClearCache is no longer needed here as it's handled by CustomTexturePatch centrally
    }
}
