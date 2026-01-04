using HarmonyLib;
using UnityEngine;

namespace PKCore.Patches;

/// <summary>
/// Patch to control mouse cursor visibility for debugging
/// </summary>
public class MouseCursorPatch
{
    private static bool _isEnabled = false;

    public static void Initialize(bool enabled)
    {
        _isEnabled = enabled;
        if (!_isEnabled)
            return;

        // Set initial state
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        Plugin.Log.LogInfo("Mouse cursor enabled for debugging");
    }

    // Prevent the game from hiding the cursor
    [HarmonyPatch(typeof(Cursor), nameof(Cursor.visible), MethodType.Setter)]
    [HarmonyPrefix]
    public static bool Cursor_set_visible_Prefix(ref bool value)
    {
        // If our debug feature is enabled, ALWAYS force visible
        if (_isEnabled)
        {
            value = true;
            // Optionally, we can return false to skip the original method if we handle it fully,
            // but letting it run with 'true' ensures Unity internal state is updated.
            // However, if the game sets it to false, we change it to true.
        }
        return true;
    }
    
    // Also enforce lock state
    [HarmonyPatch(typeof(Cursor), nameof(Cursor.lockState), MethodType.Setter)]
    [HarmonyPrefix]
    public static bool Cursor_set_lockState_Prefix(ref CursorLockMode value)
    {
        if (_isEnabled)
        {
            value = CursorLockMode.None;
        }
        return true;
    }
}
