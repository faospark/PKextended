using HarmonyLib;
using UnityEngine;

namespace PKCore.Patches;

/// <summary>
/// Patch to control mouse cursor visibility for debugging
/// </summary>
public class MouseCursorPatch
{
    private static bool _isInitialized = false;

    public static void Initialize(bool enabled)
    {
        if (!enabled || _isInitialized)
            return;

        _isInitialized = true;
        
        // Set cursor visibility and lock state
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        Plugin.Log.LogInfo("Mouse cursor enabled for debugging");
    }
}
