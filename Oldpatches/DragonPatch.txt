using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace PKCore.Patches;

/// <summary>
/// Dedicated patch for handling Dragon sprite replacements.
/// Dragons use Animator components which constantly reset the sprite,
/// requiring a monitor component (LateUpdate) to enforce custom textures.
/// </summary>
public class DragonPatch
{
    private static bool _isRegistered = false;
    
    // Lazy registration - only register when first dragon is encountered
    private static void EnsureRegistered()
    {
        if (_isRegistered) return;
        
        try 
        {
            Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<DragonSpriteMonitor>();
            Plugin.Log.LogInfo("[DragonPatch] Registered DragonSpriteMonitor type (lazy-loaded on first dragon encounter)");
            _isRegistered = true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[DragonPatch] Failed to register DragonSpriteMonitor: {ex.Message}");
        }
    }
    
    // Called at startup - no longer registers immediately
    public static void Initialize()
    {
        // Registration is now deferred until first dragon is encountered (silent)
    }

    /// <summary>
    /// Checks if an object is a known dragon part and attaches a monitor if needed
    /// </summary>
    public static void CheckAndAttachMonitor(GameObject go)
    {
        if (go == null) return;

        // Check if this is a dragon object we care about
        // Based on analysis: t_gsd1_vf1_08_obj_dragon_26, etc.
        bool isDragon = go.name.Contains("dragon", StringComparison.OrdinalIgnoreCase);
        
        if (!isDragon) return;

        // Ensure the type is registered before trying to add the component
        EnsureRegistered();

        // Avoid adding multiple monitors
        if (go.GetComponent<DragonSpriteMonitor>() != null) return;

        if (Plugin.Config.DetailedTextureLog.Value)
        {
            Plugin.Log.LogInfo($"[DragonPatch] Attaching monitor to dragon object: {go.name}");
        }
        go.AddComponent<DragonSpriteMonitor>();
    }
}

/// <summary>
/// component that runs every frame to ensure dragon sprites are replaced
/// regardless of what the Animator does
/// </summary>
public class DragonSpriteMonitor : MonoBehaviour
{
    private SpriteRenderer _renderer;
    private string _lastSpriteName;
    private Sprite _customSprite;
    private string _lastEnforcedLogName;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (_renderer == null || _renderer.sprite == null) return;

        // Check if sprite changed since last frame (Animator likely changed it)
        string currentSpriteName = _renderer.sprite.name;
        
        // If the sprite name ends with "(Clone)", strip it to get the real texture name
        if (currentSpriteName.EndsWith("(Clone)"))
        {
            currentSpriteName = currentSpriteName.Substring(0, currentSpriteName.Length - 7);
        }

        // Optimization: Only try to load/replace if the sprite effectively changed
        if (_lastSpriteName != currentSpriteName)
        {
            _lastSpriteName = currentSpriteName;
            
            // Try to load a custom replacement
            Sprite replacement = CustomTexturePatch.LoadCustomSprite(currentSpriteName, _renderer.sprite);
            
            if (replacement != null)
            {
                _customSprite = replacement;
                _renderer.sprite = replacement;
                
                // Only log if detailed logging is on AND we haven't just logged this specific enforcement
                // This prevents spamminess when fighting the Animator every frame
                if (Plugin.Config.DetailedTextureLog.Value && _lastEnforcedLogName != currentSpriteName)
                {
                    Plugin.Log.LogInfo($"[DragonMonitor] Enforced sprite: {currentSpriteName}");
                    _lastEnforcedLogName = currentSpriteName;
                }
            }
            else
            {
                _customSprite = null;
                _lastEnforcedLogName = null;
            }
        }
        // If we have a valid custom sprite for this state, force it back
        // This handles cases where Animator resets the sprite property to the original *after* we set it
        else if (_customSprite != null && _renderer.sprite != _customSprite)
        {
            _renderer.sprite = _customSprite;
        }
    }
}
