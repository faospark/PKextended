using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System;
using PKCore;

namespace PKCore.Patches;

/// <summary>
/// Dedicated patch for handling Summon effect sprite replacements.
/// Summons use Animator components which constantly reset the sprite,
/// requiring a monitor component (LateUpdate) to enforce custom textures.
/// </summary>
public class SummonPatch
{
    private static bool _isRegistered = false;
    
    // Lazy registration - only register when first summon is encountered
    private static void EnsureRegistered()
    {
        if (_isRegistered) return;
        
        try 
        {
            Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<SummonMonitor>();
            Plugin.Log.LogInfo("[SummonPatch] Registered SummonMonitor type (lazy-loaded on first summon encounter)");
            _isRegistered = true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[SummonPatch] Failed to register SummonMonitor: {ex.Message}");
        }
    }
    
    public static void Initialize()
    {
        // Registration is deferred
    }

    /// <summary>
    /// Checks if an object is a known summon effect and attaches a monitor if needed
    /// </summary>
    public static void CheckAndAttachMonitor(GameObject go)
    {
        if (go == null) return;

        bool isSummon = false;

        // Check 1: Name contains Summon
        if (go.name.Contains("Summon", StringComparison.OrdinalIgnoreCase))
        {
            isSummon = true;
        }
        // Check 2: Renderer (Sprite or Material) contains Summon
        else
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Check Sprite
                var sr = renderer as SpriteRenderer;
                if (sr != null && sr.sprite != null && sr.sprite.name.Contains("Summon", StringComparison.OrdinalIgnoreCase))
                {
                    isSummon = true;
                }
                // Check Material
                else if (renderer.material != null && renderer.material.mainTexture != null)
                {
                    if (renderer.material.mainTexture.name.Contains("Summon", StringComparison.OrdinalIgnoreCase))
                    {
                        isSummon = true;
                    }
                }
            }
        }
        
        if (!isSummon) return;

        // Ensure the type is registered
        EnsureRegistered();

        // Avoid adding multiple monitors
        if (go.GetComponent<SummonMonitor>() != null) return;

        if (Plugin.Config.DetailedTextureLog.Value)
        {
            Plugin.Log.LogInfo($"[SummonPatch] Attaching monitor to summon object: {go.name}");
        }
        go.AddComponent<SummonMonitor>();
    }
}

/// <summary>
/// Component that runs every frame to ensure summon sprites are replaced
/// regardless of what the Animator does
/// </summary>
public class SummonMonitor : MonoBehaviour
{
    private Renderer _renderer;
    private SpriteRenderer _spriteRenderer;
    private string _lastSpriteName;
    private string _targetTextureName;
    private Sprite _customSprite;
    private string _lastEnforcedLogName;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Try to identify target texture from initial state
        if (_spriteRenderer != null && _spriteRenderer.sprite != null)
        {
            _lastSpriteName = _spriteRenderer.sprite.name;
        }
        else if (_renderer != null && _renderer.material != null && _renderer.material.mainTexture != null)
        {
            _targetTextureName = _renderer.material.mainTexture.name;
        }
    }

    private void LateUpdate()
    {
        if (_renderer == null) return;

        // Path 1: SpriteRenderer (Dragon/Cow style)
        if (_spriteRenderer != null)
        {
            if (_spriteRenderer.sprite == null) return;

            string currentSpriteName = _spriteRenderer.sprite.name;
            if (currentSpriteName.EndsWith("(Clone)"))
                currentSpriteName = currentSpriteName.Substring(0, currentSpriteName.Length - 7);

            if (_lastSpriteName != currentSpriteName)
            {
                _lastSpriteName = currentSpriteName;
                Sprite replacement = CustomTexturePatch.LoadCustomSprite(currentSpriteName, _spriteRenderer.sprite);
                
                if (replacement != null)
                {
                    _customSprite = replacement;
                    _spriteRenderer.sprite = replacement;
                    LogEnforcement(currentSpriteName);
                }
                else
                {
                    _customSprite = null;
                }
            }
            else if (_customSprite != null && _spriteRenderer.sprite != _customSprite)
            {
                _spriteRenderer.sprite = _customSprite;
            }
        }
        // Path 2: Generic Renderer (Mesh/Particle) - Material Replacement
        else if (!string.IsNullOrEmpty(_targetTextureName))
        {
            if (_renderer.material == null) return;

            // Check if game reset the material texture
            if (_renderer.material.mainTexture == null || _renderer.material.mainTexture.name != _targetTextureName)
            {
                // Re-apply custom texture
                Sprite customSprite = CustomTexturePatch.LoadCustomSprite(_targetTextureName, null);
                if (customSprite != null && customSprite.texture != null)
                {
                    _renderer.material.mainTexture = customSprite.texture;
                    _renderer.material.mainTexture.name = _targetTextureName; // Maintain name for check
                    // Ensure wrapping if needed (Summons usually Clamp, but let's see)
                    // customSprite.texture.wrapMode = TextureWrapMode.Clamp; 
                }
            }
        }
        // Path 3: Auto-detect on generic renderer if target not set
        else if (_renderer.material != null && _renderer.material.mainTexture != null)
        {
             string texName = _renderer.material.mainTexture.name;
             if (texName.Contains("Summon", StringComparison.OrdinalIgnoreCase))
             {
                 _targetTextureName = texName;
             }
        }
    }

    private void LogEnforcement(string name)
    {
        if (Plugin.Config.DetailedTextureLog.Value && _lastEnforcedLogName != name)
        {
            Plugin.Log.LogInfo($"[SummonMonitor] Enforced sprite/texture: {name}");
            _lastEnforcedLogName = name;
        }
    }
}
