using HarmonyLib;
using UnityEngine;
using Share.UI.Window;

namespace PKCore.Patches
{
    /// <summary>
    /// Dialog box scaling patches for both Suikoden 1 and Suikoden 2
    /// S1: Patches OpenMessageWindow(Vector3) - coroutine version
    /// S2: Patches OpenMessageWindow(Sprite, string, string, Vector3, bool)
    /// </summary>
    public class DialogBoxScalePatch
    {
        private static bool _hasLoggedFirstApplication = false;
        
        /// <summary>
        /// Patch OpenMessageWindow coroutine - Suikoden 1 dialog method
        /// Returns IEnumerator, used for dialog animations
        /// </summary>
        [HarmonyPatch(typeof(UIMessageWindow), nameof(UIMessageWindow.OpenMessageWindow))]
        [HarmonyPatch(new[] { typeof(Vector3) })]
        [HarmonyPostfix]
        public static void OpenMessageWindow_S1_Postfix(UIMessageWindow __instance)
        {
            ApplyScaling(__instance);
        }
        
        /// <summary>
        /// Patch OpenMessageWindow - Suikoden 2 dialog method with portrait
        /// </summary>
        [HarmonyPatch(typeof(UIMessageWindow), nameof(UIMessageWindow.OpenMessageWindow))]
        [HarmonyPatch(new[] { typeof(Sprite), typeof(string), typeof(string), typeof(Vector3), typeof(bool) })]
        [HarmonyPostfix]
        public static void OpenMessageWindow_S2_Postfix(UIMessageWindow __instance)
        {
            ApplyScaling(__instance);
        }

        /// <summary>
        /// Shared scaling logic
        /// </summary>
        private static void ApplyScaling(UIMessageWindow instance)
        {
            if (!Plugin.Config.ScaleDownDialogBox.Value)
                return;

            float scale = 0.8f;
            GameObject dialogWindow = instance.gameObject;
            ApplyTransform(dialogWindow, scale);
            
            if (!_hasLoggedFirstApplication)
            {
                Plugin.Log.LogInfo($"[DialogBoxScalePatch] Applied dialog transform to '{dialogWindow.name}' (scale: {scale})");
                _hasLoggedFirstApplication = true;
            }
            else if (Plugin.Config.DetailedLogs.Value)
            {
                Plugin.Log.LogDebug($"[DialogBoxScalePatch] Applied dialog transform to '{dialogWindow.name}' (scale: {scale})");
            }
        }

        /// <summary>
        /// Apply scale and position transformation to dialog box
        /// </summary>
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
