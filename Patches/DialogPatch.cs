using HarmonyLib;
using UnityEngine;
using Share.UI.Window;

namespace PKCore.Patches
{
    public class DialogPatch
    {
        // Hook into UIMessageWindow.OpenMessageWindow (5-parameter overload)
        // This is used by Suikoden 2
        [HarmonyPatch(typeof(UIMessageWindow), nameof(UIMessageWindow.OpenMessageWindow))]
        [HarmonyPatch(new[] { typeof(Sprite), typeof(string), typeof(string), typeof(Vector3), typeof(bool) })]
        [HarmonyPostfix]
        public static void OpenMessageWindow_Postfix(UIMessageWindow __instance)
        {
            float scale = GetScaleFromPreset(Plugin.Config.DialogBoxScale.Value);
            
            // Only apply if scale is not default (1.0)
            if (scale >= 0.99f)
                return;

            GameObject dialogWindow = __instance.gameObject;
            ApplyTransform(dialogWindow, scale);
            Plugin.Log.LogInfo($"[DialogPatch] Applied dialog transform (scale: {scale}) via OpenMessageWindow");
        }

        // Hook into UIMessageWindow.SetCharacterFace
        // This appears to be used by Suikoden 1 for dialog display
        [HarmonyPatch(typeof(UIMessageWindow), nameof(UIMessageWindow.SetCharacterFace))]
        [HarmonyPostfix]
        public static void SetCharacterFace_Postfix(UIMessageWindow __instance)
        {
            float scale = GetScaleFromPreset(Plugin.Config.DialogBoxScale.Value);
            
            // Only apply if scale is not default (1.0)
            if (scale >= 0.99f)
                return;

            GameObject dialogWindow = __instance.gameObject;
            ApplyTransform(dialogWindow, scale);
            Plugin.Log.LogInfo($"[DialogPatch] Applied dialog transform (scale: {scale}) via SetCharacterFace");
        }

        /// <summary>
        /// Convert string preset to float scale value
        /// </summary>
        private static float GetScaleFromPreset(string preset)
        {
            switch (preset.ToLower())
            {
                case "small":
                    return 0.5f;
                case "medium":
                    return 0.8f;
                case "large":
                default:
                    return 1.0f;
            }
        }

        private static void ApplyTransform(GameObject obj, float scale)
        {
            // Calculate position offset based on scale
            // At 0.5 scale: -208 offset (very compact)
            // At 0.8 scale: -104 offset (smaller)
            // At 1.0 scale: 0 offset (no change)
            // Linear interpolation
            float positionOffset = Mathf.Lerp(-208f, 0f, (scale - 0.5f) / 0.5f);
            
            // Apply position offset
            obj.transform.localPosition = new Vector3(0f, positionOffset, 0f);
            
            // Apply scale (scale X and Y, keep Z at 1)
            obj.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
