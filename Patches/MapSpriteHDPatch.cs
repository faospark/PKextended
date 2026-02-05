using HarmonyLib;
using UnityEngine;

namespace PKCore.Patches
{
    /// <summary>
    /// Patch for MapSpriteHD component used in overworld map
    /// This component bypasses normal SpriteRenderer, so we need a dedicated patch
    /// </summary>
    public partial class CustomTexturePatch
    {
        /// <summary>
        /// Intercept MapSpriteHD material changes to replace overworld map textures
        /// MapSpriteHD uses materials directly, not sprites
        /// </summary>
        [HarmonyPatch(typeof(MapSpriteHD), "Awake")]
        [HarmonyPostfix]
        public static void MapSpriteHD_Awake_Postfix(MapSpriteHD __instance)
        {
            if (!Plugin.Config.EnableCustomTextures.Value)
                return;

            // MapSpriteHD likely has a material or renderer component
            // Try to get the material and replace its texture
            var renderer = __instance.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                Material mat = renderer.material;
                if (mat.mainTexture != null)
                {
                    string textureName = mat.mainTexture.name;
                    
                    // Log replaceable textures if enabled
                    if (Plugin.Config.LogReplaceableTextures.Value && !loggedTextures.Contains(textureName))
                    {
                        loggedTextures.Add(textureName);
                        Plugin.Log.LogInfo($"[Replaceable Texture - MapSpriteHD] {textureName}");
                    }

                    // Try to replace the texture
                    Texture2D customTexture = LoadCustomTexture(textureName);
                    if (customTexture != null)
                    {
                        mat.mainTexture = customTexture;
                        
                        if (Plugin.Config.DetailedTextureLog.Value)
                        {
                            Plugin.Log.LogInfo($"Replaced MapSpriteHD texture: {textureName}");
                        }
                    }
                }
            }
            
            // Also check SpriteRenderer (for save points and other sprites)
            var spriteRenderer = __instance.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                Sprite originalSprite = spriteRenderer.sprite;
                string spriteName = originalSprite.name;
                
                // Log for diagnostics
                if (Plugin.Config.DetailedTextureLog.Value)
                {
                    // Only log if it's NOT a generic atlas texture name (to avoid frame spam)
                    if (!spriteName.Contains("atlas", System.StringComparison.OrdinalIgnoreCase))
                    {
                        // Plugin.Log.LogDebug($"[MapSpriteHD] Found SpriteRenderer with sprite: {spriteName}");
                    }
                }
                
                // Try to load custom sprite
                Sprite customSprite = LoadCustomSprite(spriteName, originalSprite);
                if (customSprite != null)
                {
                    spriteRenderer.sprite = customSprite;
                    
                    if (Plugin.Config.DetailedTextureLog.Value)
                    {
                        Plugin.Log.LogInfo($"Replaced MapSpriteHD sprite: {spriteName}");
                    }
                }
            }
        }
    }
}
