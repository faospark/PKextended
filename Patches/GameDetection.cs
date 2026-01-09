using UnityEngine;
using UnityEngine.SceneManagement;

namespace PKCore.Patches;

/// <summary>
/// Utility to detect which game (Suikoden 1 or 2) is currently running
/// Used for game-specific texture loading
/// </summary>
public static class GameDetection
{
    private static string _cachedGameId = null;
    
    /// <summary>
    /// Get the current game identifier (GSD1, GSD2, or Unknown)
    /// Result is cached after first detection
    /// </summary>
    public static string GetCurrentGame()
    {
        // Return cached result if available
        if (_cachedGameId != null)
            return _cachedGameId;
        
        // Detect game from active scene name
        string sceneName = SceneManager.GetActiveScene().name;
        
        if (sceneName == "GSD1")
        {
            _cachedGameId = "GSD1";
            Plugin.Log.LogInfo("[GameDetection] ✓ Suikoden 1 (GSD1) detected");
        }
        else if (sceneName == "GSD2")
        {
            _cachedGameId = "GSD2";
            Plugin.Log.LogInfo("[GameDetection] ✓ Suikoden 2 (GSD2) detected");
        }
        else
        {
            _cachedGameId = "Unknown";
            // Don't log warning during launcher - it's expected
            if (!string.IsNullOrEmpty(sceneName))
            {
                Plugin.Log.LogInfo($"[GameDetection] Launcher active - waiting for game selection");
            }
        }
        
        return _cachedGameId;
    }
    
    /// <summary>
    /// Check if we're currently running Suikoden 1
    /// </summary>
    public static bool IsGSD1() => GetCurrentGame() == "GSD1";
    
    /// <summary>
    /// Check if we're currently running Suikoden 2
    /// </summary>
    public static bool IsGSD2() => GetCurrentGame() == "GSD2";
}
