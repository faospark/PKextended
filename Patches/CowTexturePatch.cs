using HarmonyLib;
using UnityEngine;
using System;

namespace PKCore.Patches;

/// <summary>
/// Patch to handle the animated cows in Gregminster (vaa_00).
/// These objects are named t_gsd1_vaa_00_obj_ushi_01 through 06.
/// They have animated textures that need to be replaced and enforced.
/// </summary>
[HarmonyPatch]
public static class CowTexturePatch
{
    private const string COW_PREFIX = "t_gsd1_vaa_00_obj_ushi_";
    private static bool _isRegistered = false;

    // Lazy registration - only register when first cow is encountered
    private static void EnsureRegistered()
    {
        if (_isRegistered) return;

        try
        {
            Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<CowMonitor>();
            if (Plugin.Config.DetailedTextureLog.Value)
                Plugin.Log.LogInfo("[CowTexturePatch] Registered CowMonitor type (lazy-loaded on first cow encounter)");
            _isRegistered = true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[CowTexturePatch] Failed to register CowMonitor: {ex.Message}");
        }
    }

    // Called at startup - no longer registers immediately
    public static void Initialize()
    {
        // Registration is now deferred until first cow is encountered (silent)
    }

    public static void CheckAndAttachMonitor(GameObject go)
    {
        if (go == null) return;

        // Check if object name matches cow prefix
        bool isCow = go.name.StartsWith(COW_PREFIX, StringComparison.OrdinalIgnoreCase);

        // If not matched by name, check sprite name if a SpriteRenderer exists
        if (!isCow)
        {
            var renderer = go.GetComponent<SpriteRenderer>();
            if (renderer != null && renderer.sprite != null)
            {
                isCow = renderer.sprite.name.StartsWith(COW_PREFIX, StringComparison.OrdinalIgnoreCase);
            }
        }

        if (isCow)
        {
            // Ensure the type is registered before trying to add the component
            EnsureRegistered();
            AttachMonitor(go);
        }
    }

    private static void AttachMonitor(GameObject obj)
    {
        if (obj.GetComponent<CowMonitor>() == null)
        {
            if (Plugin.Config.DetailedTextureLog.Value)
                Plugin.Log.LogInfo($"[CowPatch] Detected cow object: {obj.name}. Attaching monitor.");
            obj.AddComponent<CowMonitor>();
        }
    }
}

/// <summary>
/// Monitor component to enforce custom textures on animated cows.
/// </summary>
public class CowMonitor : MonoBehaviour
{
    private SpriteRenderer _renderer;


    // Cache for custom sprites to avoid repeated lookups
    private System.Collections.Generic.Dictionary<string, Sprite> _spriteCache = new System.Collections.Generic.Dictionary<string, Sprite>();

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (_renderer == null || _renderer.sprite == null)
            return;

        string currentSpriteName = _renderer.sprite.name;

        // Optimization: If the sprite hasn't changed (by name) and we already replaced it, do nothing.
        // But if the animation system swapped the sprite, the name might be the original name again.
        // We check if it matches the pattern we expect to replace.

        // The animation loops through textures like t_gsd1_vaa_00_obj_ushi_01, _02, _03
        if (currentSpriteName.StartsWith("t_gsd1_vaa_00_obj_ushi_", StringComparison.OrdinalIgnoreCase))
        {
            // Only try to replace if we haven't already replaced this specific sprite instance frame
            // or if the name indicates it's a new frame from the animation.

            // Note: When we replace the sprite, the new sprite usually shares the same name if we set it up that way,
            // or we can rely on object reference equality if we want to be strict.
            // CustomTexturePatch.LoadCustomSprite returns a sprite that might have the same name.

            // To properly detect if the game's animation system has put a standard sprite back,
            // we can check if the current sprite is one of our cached custom ones.

            if (IsCustomSprite(_renderer.sprite))
                return; // already our custom sprite

            // It's a game sprite, try to replace it
            ReplaceSprite(currentSpriteName);
        }
    }

    private bool IsCustomSprite(Sprite sprite)
    {
        // Simple check: is this sprite in our local cache?
        // This assumes LoadCustomSprite returns consistent references or we cache them here.
        return _spriteCache.ContainsValue(sprite);
    }

    private void ReplaceSprite(string spriteName)
    {
        if (_spriteCache.TryGetValue(spriteName, out Sprite customSprite))
        {
            // Use cached custom sprite
            if (customSprite != null)
            {
                _renderer.sprite = customSprite;
            }
            return;
        }

        // Not in local cache, try to load it
        customSprite = CustomTexturePatch.LoadCustomSprite(spriteName, _renderer.sprite);

        if (customSprite != null)
        {
            if (Plugin.Config.DetailedTextureLog.Value)
            {
                Plugin.Log.LogInfo($"[CowMonitor] Replaced cow frame: {spriteName}");
            }

            // Cache it
            _spriteCache[spriteName] = customSprite;

            // Apply it
            _renderer.sprite = customSprite;
        }
        else
        {
            // If valid custom sprite not found, maybe cache null so we don't keep trying?
            // For now, let's just attempt less frequently or just fail silently after one log
            // But here we'll just not cache it to retry in case the file appears or loads later (unlikely but safe)
        }
    }
}
