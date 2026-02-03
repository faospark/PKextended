using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.U2D;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace PKCore.Patches
{
    public static class SpriteAtlasCache
    {
        private static ManualLogSource Log => Plugin.Log;
        
        // Cache for generated replacement sprites to reuse them
        private static Dictionary<string, Sprite> _replacementCache = new Dictionary<string, Sprite>();

        public static void Clear()
        {
            _replacementCache.Clear();
        }

        public static Sprite GetReplacementSprite(Sprite original)
        {
            if (original == null) return null;
            
            string name = original.name;
            if (name.EndsWith("(Clone)")) name = name.Substring(0, name.Length - 7);
            
            // Check cache first
            if (_replacementCache.TryGetValue(name, out Sprite cachedSprite))
            {
                // Verify the sprite is still valid (not destroyed)
                if (cachedSprite != null) return cachedSprite;
                _replacementCache.Remove(name);
            }
            
            // Clean suffix for texture search
            string cleanName = CustomTexturePatch.CleanTextureName(name);

            Texture2D tex = CustomTexturePatch.LoadCustomTexture(cleanName);
            if (tex != null)
            {
                // Create new sprite with same properties as original
                Rect rect = original.rect;
                Vector2 pivot = original.pivot;
                float pixelsPerUnit = original.pixelsPerUnit;
                
                // Adjust pivot to 0-1 range relative to rect size
                // rect.width/height usage:
                // If original sprite was packed, rect is small.
                // Our replacement texture is likely full size.
                // If we want to replace correctly, we usually assume the replacement texture
                // matches the dimensions expected for the sprite (or is the sprite source itself).
                
                // For simplified robustness: Use texture center if uncertain, or try to respect original pivot ratio.
                Rect newRect = new Rect(0, 0, tex.width, tex.height);
                
                // Safe pivot calculation avoiding division by zero
                float px = (rect.width > 0) ? pivot.x / rect.width : 0.5f;
                float py = (rect.height > 0) ? pivot.y / rect.height : 0.5f;
                Vector2 normalizedPivot = new Vector2(px, py);

                Sprite newSprite = Sprite.Create(tex, newRect, normalizedPivot, pixelsPerUnit);
                newSprite.name = original.name + "_Custom";
                
                // Ensure it persists
                UnityEngine.Object.DontDestroyOnLoad(newSprite);
                UnityEngine.Object.DontDestroyOnLoad(tex);
                
                // Cache it using the ORIGINAL name as key
                _replacementCache[name] = newSprite;
                
                return newSprite;
            }
            
            // Mark as checked but not found? 
            // If we don't, we'll hit disk every time for missing textures.
            // Let's cache nulls too? Use a separate HashSet for "missing".
            
            return null;
        }
    }
}
