using HarmonyLib;
using UnityEngine;
using UnityEngine.U2D;
using System;

namespace PKCore.Patches;

/// <summary>
/// Patches for save point sprite replacement
/// Handles preloading and Resources.Load interception for save point animation frames
/// </summary>
public partial class CustomTexturePatch
{
    /// <summary>
    /// Intercept Resources.Load<Sprite>() to replace save point animation frames
    /// The Animator loads sprite frames via Resources.Load, not through sprite property setters
    /// </summary>
    [HarmonyPatch(typeof(Resources))]
    [HarmonyPatch(nameof(Resources.Load))]
    [HarmonyPatch(new Type[] { typeof(string) }, new ArgumentType[] { ArgumentType.Normal })]
    [HarmonyPatch(MethodType.Normal)]
    public static class Resources_Load_Sprite_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch]
        public static void Postfix(string path, ref UnityEngine.Object __result)
        {
            // Only process if result is a Sprite
            if (__result == null || !(__result is Sprite))
                return;

            Sprite sprite = __result as Sprite;
            string spriteName = sprite.name;

            // DIAGNOSTIC: Log save point sprite loads
            bool isSavePoint = spriteName.Contains("savePoint", StringComparison.OrdinalIgnoreCase);
            if (isSavePoint)
            {
                Plugin.Log.LogInfo($"[SavePoint] Resources.Load<Sprite>() called for: {spriteName}");
                Plugin.Log.LogInfo($"[SavePoint]   Path: {path}");
            }

            // Check if this is a save point animation frame
            if (spriteName.StartsWith("t_obj_savePoint_ball_") && preloadedSavePointSprites.TryGetValue(spriteName, out Sprite customSprite))
            {
                Plugin.Log.LogInfo($"[SavePoint] ✓ Replacing Resources.Load sprite: {spriteName}");
                __result = customSprite;
            }
            else if (isSavePoint)
            {
                Plugin.Log.LogWarning($"[SavePoint] ✗ No preloaded sprite found for: {spriteName}");
            }
        }
    }

    /// <summary>
    /// Preload save point animation frames (t_obj_savePoint_ball_0 through _10)
    /// This ensures all animation frames use the custom texture
    /// </summary>
    private static void PreloadSavePointSprites()
    {
        Plugin.Log.LogInfo("[SavePoint Preload] Starting save point sprite preloading...");
        
        // Check if we have the atlas texture
        string atlasName = "t_obj_savePoint_ball";
        if (!texturePathIndex.ContainsKey(atlasName))
        {
            Plugin.Log.LogWarning($"[SavePoint Preload] Atlas texture '{atlasName}' not found in texture index!");
            return;
        }

        Plugin.Log.LogInfo($"[SavePoint Preload] Found atlas '{atlasName}' in index, loading texture...");
        Texture2D atlasTexture = LoadCustomTexture(atlasName);
        if (atlasTexture == null)
        {
            Plugin.Log.LogError($"[SavePoint Preload] Failed to load atlas texture '{atlasName}'!");
            return;
        }

        Plugin.Log.LogInfo($"[SavePoint Preload] Atlas loaded: {atlasTexture.width}x{atlasTexture.height}");
        int preloaded = 0;
        
        // Atlas is 400x200 with 8 frames in a 4x2 grid (each frame is 100x100)
        int frameWidth = 100;
        int frameHeight = 100;
        int columns = 4;
        
        // Preload frames 0-10 (11 frames total for the animation)
        // Note: Atlas only has 8 unique frames, so some frames may repeat
        for (int i = 0; i <= 10; i++)
        {
            string frameName = $"t_obj_savePoint_ball_{i}";
            
            // Calculate frame position in the atlas (4 columns, 2 rows)
            // Frames are arranged left-to-right, top-to-bottom
            int frameIndex = i % 8; // Cycle through 8 frames if there are more than 8
            int col = frameIndex % columns;
            int row = frameIndex / columns;
            
            // Calculate the rect for this frame
            // Unity's Rect origin is bottom-left, but texture coords are top-left
            // So we need to flip the Y coordinate
            float x = col * frameWidth;
            float y = atlasTexture.height - (row + 1) * frameHeight; // Flip Y
            
            Plugin.Log.LogInfo($"[SavePoint Preload] Creating sprite {frameName}: rect=({x},{y},{frameWidth},{frameHeight})");
            
            Sprite sprite = Sprite.Create(
                atlasTexture,
                new Rect(x, y, frameWidth, frameHeight),
                new Vector2(0.5f, 0.5f), // Center pivot
                100f, // Default ppu for save point
                0,
                SpriteMeshType.FullRect
            );

            if (sprite == null)
            {
                Plugin.Log.LogError($"[SavePoint Preload] Sprite.Create returned NULL for {frameName}!");
            }
            else
            {
                Plugin.Log.LogInfo($"[SavePoint Preload] Created sprite {frameName} successfully");
            }

            UnityEngine.Object.DontDestroyOnLoad(sprite);
            preloadedSavePointSprites[frameName] = sprite;
            preloaded++;
        }
        
        // Don't destroy the atlas texture
        UnityEngine.Object.DontDestroyOnLoad(atlasTexture);

        if (preloaded > 0)
        {
            Plugin.Log.LogInfo($"Preloaded {preloaded} save point animation frame(s) for instant replacement");
        }
    }
}
