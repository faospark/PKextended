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
            if (!Plugin.Config.SmallerDialogBox.Value)
                return;

            GameObject dialogWindow = __instance.gameObject;
            
            Plugin.Log.LogInfo($"[DialogPatch] OpenMessageWindow called");
            Plugin.Log.LogInfo($"[DialogPatch] GameObject name: {dialogWindow.name}");
            Plugin.Log.LogInfo($"[DialogPatch] Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            Plugin.Log.LogInfo($"[DialogPatch] Current Position: {dialogWindow.transform.localPosition}");
            Plugin.Log.LogInfo($"[DialogPatch] Current Scale: {dialogWindow.transform.localScale}");
            
            // Apply transform
            ApplyTransform(dialogWindow);
            
            Plugin.Log.LogInfo($"[DialogPatch] Transform applied!");
            Plugin.Log.LogInfo($"[DialogPatch] New Position: {dialogWindow.transform.localPosition}");
            Plugin.Log.LogInfo($"[DialogPatch] New Scale: {dialogWindow.transform.localScale}");
        }

        // Hook into UIMessageWindow.SetCharacterFace
        // This appears to be used by Suikoden 1 for dialog display
        [HarmonyPatch(typeof(UIMessageWindow), nameof(UIMessageWindow.SetCharacterFace))]
        [HarmonyPostfix]
        public static void SetCharacterFace_Postfix(UIMessageWindow __instance)
        {
            if (!Plugin.Config.SmallerDialogBox.Value)
                return;

            GameObject dialogWindow = __instance.gameObject;
            
            Plugin.Log.LogInfo($"[DialogPatch] SetCharacterFace called");
            Plugin.Log.LogInfo($"[DialogPatch] GameObject name: {dialogWindow.name}");
            Plugin.Log.LogInfo($"[DialogPatch] Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            Plugin.Log.LogInfo($"[DialogPatch] Current Position: {dialogWindow.transform.localPosition}");
            Plugin.Log.LogInfo($"[DialogPatch] Current Scale: {dialogWindow.transform.localScale}");
            
            // Apply transform
            ApplyTransform(dialogWindow);
            
            Plugin.Log.LogInfo($"[DialogPatch] Transform applied via SetCharacterFace!");
            Plugin.Log.LogInfo($"[DialogPatch] New Position: {dialogWindow.transform.localPosition}");
            Plugin.Log.LogInfo($"[DialogPatch] New Scale: {dialogWindow.transform.localScale}");
        }

        private static void ApplyTransform(GameObject obj)
        {
            // Apply position offset (0, -84, 0)
            obj.transform.localPosition = new Vector3(0f, -94f, 0f);
            
            // Apply scale (0.8, 0.8, 1)
            obj.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
        }
    }
}
