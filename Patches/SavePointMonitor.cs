using UnityEngine;

namespace PKCore.Patches;

/// <summary>
/// MonoBehaviour that monitors a save point SpriteRenderer and ensures custom sprites are applied
/// This is needed because the Animator uses direct field access, bypassing our Harmony patches
/// </summary>
public class SavePointSpriteMonitor : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private int lastFrameChecked = -1;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Plugin.Log.LogWarning("[SavePoint Monitor] No SpriteRenderer found on GameObject!");
            enabled = false;
        }
        else
        {
            Plugin.Log.LogInfo($"[SavePoint Monitor] Started monitoring: {gameObject.name}");
        }
    }
    
    void LateUpdate()
    {
        // Only check every few frames to reduce performance impact
        if (Time.frameCount - lastFrameChecked < 3)
            return;
            
        lastFrameChecked = Time.frameCount;
        
        if (spriteRenderer == null || spriteRenderer.sprite == null)
            return;
        
        string currentSpriteName = spriteRenderer.sprite.name;
        
        // Check if this is a save point ball sprite
        if (currentSpriteName.StartsWith("t_obj_savePoint_ball_"))
        {
            // Check if it's using the original sprite (not our custom one)
            Texture2D currentTexture = spriteRenderer.sprite.texture;
            
            // If the texture is not our custom texture (check dimensions or name)
            // Our custom atlas is 400x200, original might be different
            if (currentTexture != null && currentTexture.width != 400)
            {
                // Try to get custom sprite from cache
                if (CustomTexturePatch.customSpriteCache.TryGetValue(currentSpriteName, out Sprite customSprite))
                {
                    if (customSprite != null && customSprite.texture != null)
                    {
                        spriteRenderer.sprite = customSprite;
                        Plugin.Log.LogInfo($"[SavePoint Monitor] ✓ Replaced animator sprite: {currentSpriteName}");
                    }
                }
                else
                {
                    // Sprite not in cache, try to load it
                    Sprite loadedSprite = CustomTexturePatch.LoadCustomSprite(currentSpriteName, spriteRenderer.sprite);
                    if (loadedSprite != null)
                    {
                        spriteRenderer.sprite = loadedSprite;
                        Plugin.Log.LogInfo($"[SavePoint Monitor] ✓ Loaded and replaced animator sprite: {currentSpriteName}");
                    }
                }
            }
        }
    }
}
