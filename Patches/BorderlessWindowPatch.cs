using HarmonyLib;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Collections;

namespace PKCore.Patches;

public class BorderlessWindowPatch
{
    private static bool _enabled = false;

    #region Win32 API
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private const int GWL_STYLE = -16;
    private const int WS_CAPTION = 0x00C00000;
    private const int WS_THICKFRAME = 0x00040000;
    private const int WS_SYSMENU = 0x00080000;
    private const int WS_MINIMIZEBOX = 0x00020000;
    private const int WS_MAXIMIZEBOX = 0x00010000;
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_SHOWWINDOW = 0x0040;
    #endregion

    public static void Initialize()
    {
        _enabled = Plugin.Config.EnableBorderlessWindow.Value;

        if (!_enabled)
        {
            Plugin.Log.LogInfo("Borderless window disabled");
            return;
        }

        try
        {
            // Initial setup - force windowed mode first so we can strip borders
            // If currently full screen, switch to windowed
            if (Screen.fullScreenMode != FullScreenMode.Windowed)
            {
                Screen.fullScreenMode = FullScreenMode.Windowed;
            }
            
            // Apply border removal on next frame to ensure window is ready
            if (PkCoreMainLoop.Instance != null)
            {
                // Use Invoke to delay execution slightly without coroutine complexity
                PkCoreMainLoop.Instance.Invoke(nameof(PkCoreMainLoop.ApplyBorderlessStyle), 0.1f);
            }
            else
                Plugin.Log.LogWarning("PkCoreMainLoop not initialized, cannot run borderless update");

            Plugin.Log.LogInfo($"✓ Windowed Borderless mode enabled");
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"✗ Failed to enable borderless window: {ex.Message}");
        }
    }



    public static void ApplyBorderlessStyle()
    {
        try
        {
            IntPtr hWnd = GetActiveWindow();
            if (hWnd == IntPtr.Zero) return;

            int style = GetWindowLong(hWnd, GWL_STYLE);
            
            // Remove caption, frame, sysmenu, buttons
            style &= ~(WS_CAPTION | WS_THICKFRAME | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
            
            SetWindowLong(hWnd, GWL_STYLE, style);
            
            // Trigger update
            SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED | SWP_SHOWWINDOW);
            
            Plugin.Log.LogInfo("Applied Win32 borderless style.");
        }
        catch (Exception ex)
        {
             Plugin.Log.LogError($"Error applying borderless style: {ex.Message}");
        }
    }

    // Intercept SetResolution to enforce Windowed mode + Borderless Style
    [HarmonyPatch(typeof(Screen), nameof(Screen.SetResolution), new[] { typeof(int), typeof(int), typeof(FullScreenMode) })]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    static void SetResolution(ref int width, ref int height, ref FullScreenMode fullscreenMode)
    {
        if (_enabled)
        {
            // Force Windowed mode so we can control the style
            fullscreenMode = FullScreenMode.Windowed;
            
            // Re-apply style after resolution change
            // Re-apply style after resolution change
            if (PkCoreMainLoop.Instance != null)
                PkCoreMainLoop.Instance.Invoke(nameof(PkCoreMainLoop.ApplyBorderlessStyle), 0.1f);
        }
    }

    // Intercept boolean overload
    [HarmonyPatch(typeof(Screen), nameof(Screen.SetResolution), new[] { typeof(int), typeof(int), typeof(bool) })]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    static bool SetResolution(int width, int height, bool fullscreen)
    {
        if (_enabled)
        {
            // Redirect to the enum version with Windowed mode
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
            
             // Re-apply style
             // Re-apply style
            if (PkCoreMainLoop.Instance != null)
                PkCoreMainLoop.Instance.Invoke(nameof(PkCoreMainLoop.ApplyBorderlessStyle), 0.1f);
            
            return false; // Skip original execution
        }
        return true;
    }
}
