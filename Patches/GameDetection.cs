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
    private static string _lastSceneName = "";

    /// <summary>
    /// Get the current game identifier (GSD1, GSD2, or Unknown)
    /// Handles scene switching and cache invalidation
    /// </summary>
    public static string GetCurrentGame()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        // Invalidate cache if scene changed (e.g. GSD1 -> Main -> GSD2)
        if (sceneName != _lastSceneName)
        {
            _cachedGameId = null;
            _lastSceneName = sceneName;
            // Optional: Log scene change for debug only
            // Plugin.Log.LogDebug($"[GameDetection] Scene changed to: {sceneName}");
        }

        // Return cached result if we have a resolved game ID
        if (_cachedGameId != null)
            return _cachedGameId;
        
        if (sceneName.Contains("GSD1"))
        {
            _cachedGameId = "GSD1";
            Plugin.Log.LogInfo($"[GameDetection] ✓ Suikoden 1 (GSD1) detected (Scene: {sceneName})");
        }
        else if (sceneName.Contains("GSD2"))
        {
            _cachedGameId = "GSD2";
            Plugin.Log.LogInfo($"[GameDetection] ✓ Suikoden 2 (GSD2) detected (Scene: {sceneName})");
        }
        else
        {
            // Do NOT cache "Unknown" to allow retrying later
            // And do NOT log here to prevent spam (this is called every frame by monitors)
            return "Unknown";
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
