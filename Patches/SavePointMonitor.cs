using UnityEngine;

namespace PKCore.Patches;

/// <summary>
/// MonoBehaviour that monitors a save point SpriteRenderer and ensures custom sprites are applied
/// This is needed because the Animator uses direct field access, bypassing our Harmony patches
/// </summary>
public class SavePointSpriteMonitor : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private static bool hasLoggedGlowDisable = false; // Track if we've logged glow disable
    
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
            // Plugin.Log.LogInfo($"[SavePoint Monitor] Started monitoring: {gameObject.name}");
            
            // Try to replace the underlying texture directly (better solution)
            if (spriteRenderer.sprite != null && spriteRenderer.sprite.texture != null)
            {
                string textureName = spriteRenderer.sprite.texture.name;
                // Plugin.Log.LogInfo($"[SavePoint Monitor] Original Texture Name: {textureName}");

                if (textureName == "t_obj_savePoint_ball" && CustomTexturePatch.HasCustomTexture(textureName))
                {
                    bool success = CustomTexturePatch.ReplaceTextureInPlace(spriteRenderer.sprite.texture, textureName);
                    if (success)
                    {
                        if (Plugin.Config.DetailedTextureLog.Value)
                        {
                            Plugin.Log.LogInfo("[SavePoint Monitor] ✓ Successfully replaced texture in-place. Disabling monitor.");
                        }
                        enabled = false; // No need to monitor frames anymore!
                    }
                }
            }
            
            // Disable glow effect if configured
            if (Plugin.Config.DisableSavePointGlow.Value)
            {
                // Navigate up to find the save point root, then find Glow_add
                Transform current = transform;
                while (current != null && !current.name.Contains("savePoint"))
                {
                    current = current.parent;
                }
                
                if (current != null)
                {
                    // Look for Glow_add in children
                    Transform glowTransform = current.Find("Fire_add/Glow_add");
                    if (glowTransform == null)
                    {
                        // Try alternate path
                        glowTransform = current.Find("Particle_add/Glow_add");
                    }
                    
                    if (glowTransform != null)
                    {
                        glowTransform.gameObject.SetActive(false);
                        if (!hasLoggedGlowDisable)
                        {
                            Plugin.Log.LogInfo("[SavePoint] ✓ Disabled save point glow effect");
                            hasLoggedGlowDisable = true;
                        }
                    }
                }
            }
        }
    }
    
    void LateUpdate()
    {
        // Must check EVERY frame to fight the Animator
        // Flickering happens if we skip frames (Animator wins on skipped frames)
            
        if (spriteRenderer == null || spriteRenderer.sprite == null)
            return;
        
        string currentSpriteName = spriteRenderer.sprite.name;
        
            // Check if this is a save point ball sprite
        if (currentSpriteName.StartsWith("t_obj_savePoint_ball_"))
        {
            // STRATEGY: Check if the current sprite is our custom sprite instance
            // checking texture width is unreliable because our Harmony patch might intercept Sprite.texture getter
            // for the original sprite too!
            
            bool isWrongSprite = true;
            
            // Try to find the custom sprite in cache
            if (CustomTexturePatch.customSpriteCache.TryGetValue(currentSpriteName, out Sprite cachedCustomSprite))
            {
                // If we have a cached custom sprite, and the current sprite is NOT it, then it's wrong
                if (spriteRenderer.sprite == cachedCustomSprite)
                {
                    isWrongSprite = false;
                }
            }
            
            if (isWrongSprite)
            {
                // Try to get custom sprite (from cache or load it)
                if (cachedCustomSprite != null)
                {
                    // IMPORTANT: Duplicate assignment is intentional and necessary!
                    // Setting the sprite twice ensures it "sticks" in IL2CPP/Unity.
                    // Removing the duplicate breaks texture replacement. Do not remove!
                    //spriteRenderer.sprite = cachedCustomSprite;
                    spriteRenderer.sprite = cachedCustomSprite;
                }
                else
                {
                    // Sprite not in cache, try to load it
                    Sprite loadedSprite = CustomTexturePatch.LoadCustomSprite(currentSpriteName, spriteRenderer.sprite);
                    if (loadedSprite != null)
                    {
                        // IMPORTANT: Duplicate assignment is intentional and necessary!
                        // Setting the sprite twice ensures it "sticks" in IL2CPP/Unity.
                        // Removing the duplicate breaks texture replacement. Do not remove!
                        //spriteRenderer.sprite = loadedSprite;
                        spriteRenderer.sprite = loadedSprite;
                    }
                }
            }
        }
    }
}
