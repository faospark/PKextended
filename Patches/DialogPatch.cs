using HarmonyLib;
using UnityEngine;
using Share.UI.Window;

namespace PKCore.Patches
{
    public class DialogPatch
    {
        // Hook into UIMessageWindow.OpenMessageWindow (5-parameter overload)
        [HarmonyPatch(typeof(UIMessageWindow), nameof(UIMessageWindow.OpenMessageWindow))]
        [HarmonyPatch(new[] { typeof(Sprite), typeof(string), typeof(string), typeof(Vector3), typeof(bool) })]
        [HarmonyPostfix]
        public static void OpenMessageWindow_Postfix(UIMessageWindow __instance)
        {
            EnsureMonitor(__instance);
        }

        // Hook into UIMessageWindow.SetCharacterFace
        [HarmonyPatch(typeof(UIMessageWindow), nameof(UIMessageWindow.SetCharacterFace))]
        [HarmonyPostfix]
        public static void SetCharacterFace_Postfix(UIMessageWindow __instance)
        {
            EnsureMonitor(__instance);
        }

        // Helper to attach our monitor script
        private static void EnsureMonitor(UIMessageWindow window)
        {
            if (window == null) return;

            // Check if our monitor is already attached
            var monitor = window.gameObject.GetComponent<DialogMonitor>();
            if (monitor == null)
            {
                Plugin.Log.LogInfo("[DialogPatch] Attaching DialogMonitor to UI Window...");
                window.gameObject.AddComponent<DialogMonitor>();
            }
        }
    }
}
