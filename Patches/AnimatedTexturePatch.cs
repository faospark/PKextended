using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace PKCore.Patches;

/// <summary>
/// Dedicated patch for handling animated sprite replacements.
/// Animated objects (like dragons or cows) use Animator components which constantly reset the sprite,
/// requiring a monitor component (LateUpdate) to enforce custom textures.
/// </summary>
public class AnimatedTexturePatch
{
    private static bool _isRegistered = false;

    // Just add new prefixes/substrings here as you find more animated objects that need replacing
    private static readonly string[] AnimatedObjectKeywords = {
        "t_gsd1_ve2_19_sword_",
        "dragon",
        "t_gsd1_vaa_00_obj_ushi_"
    };

    // Lazy registration - only register when first animated object is encountered
    private static void EnsureRegistered()
    {
        if (_isRegistered) return;

        try
        {
            Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<AnimatedSpriteMonitor>();
            if (Plugin.Config.DetailedLogs.Value)
            {
                Plugin.Log.LogInfo("[AnimatedTexturePatch] Registered AnimatedSpriteMonitor type (lazy-loaded)");
            }
            _isRegistered = true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[AnimatedTexturePatch] Failed to register AnimatedSpriteMonitor: {ex.Message}");
        }
    }

    // Called at startup - no longer registers immediately
    public static void Initialize()
    {
        // Registration is deferred
    }

    /// <summary>
    /// Checks if an object is a known animated part and attaches a monitor if needed
    /// </summary>
    public static void CheckAndAttachMonitor(GameObject go)
    {
        // Optimization: Only run for Suikoden 1
        if (!GameDetection.IsGSD1()) return;

        if (go == null) return;

        bool needsMonitor = false;

        // Check object name or sprite name against our keywords
        foreach (var keyword in AnimatedObjectKeywords)
        {
            if (go.name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                needsMonitor = true;
                break;
            }
        }

        // Output of name check failure, try sprite name
        if (!needsMonitor)
        {
            var renderer = go.GetComponent<SpriteRenderer>();
            if (renderer != null && renderer.sprite != null)
            {
                foreach (var keyword in AnimatedObjectKeywords)
                {
                    if (renderer.sprite.name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        needsMonitor = true;
                        break;
                    }
                }
            }
        }

        if (needsMonitor)
        {
            EnsureRegistered();

            if (go.GetComponent<AnimatedSpriteMonitor>() == null)
            {
                if (Plugin.Config.DetailedLogs.Value)
                {
                    Plugin.Log.LogInfo($"[AnimatedTexturePatch] Attaching monitor to animated object: {go.name}");
                }
                go.AddComponent<AnimatedSpriteMonitor>();
            }
        }
    }
}

/// <summary>
/// Component that runs every frame to ensure custom sprites are replaced
/// regardless of what the Animator does
/// </summary>
public class AnimatedSpriteMonitor : MonoBehaviour
{
    private SpriteRenderer _renderer;
    private string _lastSpriteName;
    private Sprite _customSprite;
    private string _lastEnforcedLogName;

    // Cache for custom sprites to avoid repeated lookups
    private Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (_renderer == null || _renderer.sprite == null) return;

        string currentSpriteName = _renderer.sprite.name;

        // If the sprite name ends with "(Clone)", strip it to get the real texture name
        if (currentSpriteName.EndsWith("(Clone)"))
        {
            currentSpriteName = currentSpriteName.Substring(0, currentSpriteName.Length - 7);
        }

        if (IsCustomSprite(_renderer.sprite))
            return; // already our custom sprite

        // Optimization: Only try to load/replace if the sprite effectively changed
        if (_lastSpriteName != currentSpriteName)
        {
            _lastSpriteName = currentSpriteName;
            ReplaceSprite(currentSpriteName);
        }
        else if (_customSprite != null && _renderer.sprite != _customSprite)
        {
            // If we have a valid custom sprite for this state, force it back
            // This handles cases where Animator resets the sprite property to the original *after* we set it
            _renderer.sprite = _customSprite;
        }
    }

    private bool IsCustomSprite(Sprite sprite)
    {
        return _spriteCache.ContainsValue(sprite);
    }

    private void ReplaceSprite(string spriteName)
    {
        if (_spriteCache.TryGetValue(spriteName, out Sprite customSprite))
        {
            // Use cached custom sprite
            if (customSprite != null)
            {
                _customSprite = customSprite;
                _renderer.sprite = customSprite;
            }
            return;
        }

        // Try to load a custom replacement
        Sprite replacement = CustomTexturePatch.LoadCustomSprite(spriteName, _renderer.sprite);

        if (replacement != null)
        {
            _customSprite = replacement;
            _renderer.sprite = replacement;

            // Cache it
            _spriteCache[spriteName] = replacement;

            // Only log if detailed logging is on AND we haven't just logged this specific enforcement
            if (Plugin.Config.DetailedLogs.Value && _lastEnforcedLogName != spriteName)
            {
                Plugin.Log.LogInfo($"[AnimatedSpriteMonitor] Enforced animated sprite: {spriteName}");
                _lastEnforcedLogName = spriteName;
            }
        }
        else
        {
            _customSprite = null;
            _lastEnforcedLogName = null;
        }
    }
}
