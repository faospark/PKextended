using UnityEngine;

namespace PKCore.Patches;

public class BorderlessWindowPatch
{
    public static void Initialize()
    {
        if (!Plugin.Config.EnableBorderlessWindow.Value)
        {
            Plugin.Log.LogInfo("Borderless window disabled");
            return;
        }

        try
        {
            // Just change the fullscreen mode without changing resolution
            // The resolution patch will handle the actual resolution
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;

            Plugin.Log.LogInfo($"✓ Borderless window enabled");
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"✗ Failed to enable borderless window: {ex.Message}");
        }
    }
}
