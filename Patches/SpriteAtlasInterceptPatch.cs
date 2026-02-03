using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.U2D;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using BepInEx.Configuration;

namespace PKCore.Patches
{
    public static class SpriteAtlasInterceptPatch
    {
        public static void Initialize()
        {
            Plugin.Log.LogInfo("SpriteAtlas Interception Initialized (Postfix Mode)");
        }

        // Patch: public Sprite GetSprite(string name)
        [HarmonyPatch(typeof(SpriteAtlas), nameof(SpriteAtlas.GetSprite), new Type[] { typeof(string) })]
        [HarmonyPostfix]
        public static void GetSprite_Postfix(SpriteAtlas __instance, string name, ref Sprite __result)
        {
            if (__result == null) return;
            // Plugin.Log.LogDebug($"[SpriteAtlas] GetSprite('{name}') -> {__result.name}");

            Sprite replacement = SpriteAtlasCache.GetReplacementSprite(__result);
            if (replacement != null)
            {
                __result = replacement;
            }
        }

        // Patch: public int GetSprites(Sprite[] sprites)
        // In IL2CPP, array arguments are often Il2CppReferenceArray<T>
        [HarmonyPatch(typeof(SpriteAtlas), nameof(SpriteAtlas.GetSprites), new Type[] { typeof(Il2CppReferenceArray<Sprite>) })]
        [HarmonyPostfix]
        public static void GetSprites_Array_Postfix(SpriteAtlas __instance, Il2CppReferenceArray<Sprite> sprites, ref int __result)
        {
            if (__result <= 0) return;
            // Plugin.Log.LogDebug($"[SpriteAtlas] GetSprites (Array) found {__result} sprites");

            for (int i = 0; i < __result; i++)
            {
                Sprite original = sprites[i];
                if (original != null)
                {
                    Sprite replacement = SpriteAtlasCache.GetReplacementSprite(original);
                    if (replacement != null)
                    {
                        sprites[i] = replacement;
                    }
                }
            }
        }

        // Patch: public int GetSprites(Sprite[] sprites, string name)
        [HarmonyPatch(typeof(SpriteAtlas), nameof(SpriteAtlas.GetSprites), new Type[] { typeof(Il2CppReferenceArray<Sprite>), typeof(string) })]
        [HarmonyPostfix]
        public static void GetSprites_ArrayName_Postfix(SpriteAtlas __instance, Il2CppReferenceArray<Sprite> sprites, string name, ref int __result)
        {
            if (__result <= 0) return;
            // Plugin.Log.LogDebug($"[SpriteAtlas] GetSprites (Array+Name '{name}') found {__result} sprites");

            for (int i = 0; i < __result; i++)
            {
                Sprite original = sprites[i];
                if (original != null)
                {
                    Sprite replacement = SpriteAtlasCache.GetReplacementSprite(original);
                    if (replacement != null)
                    {
                        sprites[i] = replacement;
                    }
                }
            }
        }
        
        // Note: There is also GetSprites(List<Sprite>). 
        // IL2CPP handling of generic Lists in arguments can be tricky (Il2CppSystem.Collections.Generic.List<Sprite>).
        // Let's stick to Arrays for now as that's what most internal Unity UI uses.
    }
}
