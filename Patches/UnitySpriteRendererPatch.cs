using HarmonyLib;
using UnityEngine;

namespace PKCore.Patches;

/// <summary>
/// Patches for Unity's standard SpriteRenderer component
/// Handles objects that use UnityEngine.SpriteRenderer instead of the game's custom GRSpriteRenderer
/// Examples: Save points, certain effects, etc.
/// </summary>
public class UnitySpriteRendererPatch
{
    [HarmonyPatch(typeof(UnityEngine.SpriteRenderer), nameof(UnityEngine.SpriteRenderer.sprite), MethodType.Setter)]
    [HarmonyPrefix]
    public static void UnitySpriteRenderer_set_sprite_Prefix(UnityEngine.SpriteRenderer __instance, ref Sprite value)
    {
        if (value == null)
            return;


        string spriteName = value.name;
        
        // DIAGNOSTIC: Log dragon sprites specifically
        bool isDragon = spriteName.Contains("dragon", System.StringComparison.OrdinalIgnoreCase);
        if (isDragon)
        {
            Plugin.Log.LogInfo($"[UnitySpriteRenderer] Dragon sprite assignment: {spriteName}");
            Plugin.Log.LogInfo($"[UnitySpriteRenderer]   GameObject: {__instance.gameObject.name}");
            Plugin.Log.LogInfo($"[UnitySpriteRenderer]   EnableCustomTextures: {Plugin.Config.EnableCustomTextures.Value}");
            if (value.texture != null)
            {
                Plugin.Log.LogInfo($"[UnitySpriteRenderer]   Original texture: {value.texture.name} ({value.texture.width}x{value.texture.height})");
            }
        }
        
        // DIAGNOSTIC: Log save point sprites specifically
        bool isSavePoint = spriteName.Contains("savePoint", System.StringComparison.OrdinalIgnoreCase);
        if (isSavePoint)
        {
            Plugin.Log.LogInfo($"[UnitySpriteRenderer] Save point sprite assignment: {spriteName}");
            if (value.texture != null)
            {
                Plugin.Log.LogInfo($"[UnitySpriteRenderer]   Original texture: {value.texture.name} ({value.texture.width}x{value.texture.height})");
            }
        }
        
        // Log replaceable sprite if enabled
        if (Plugin.Config.DetailedLogs.Value && Plugin.Config.LogReplaceableTextures.Value && !CustomTexturePatch.IsTextureLogged(spriteName))
        {
            CustomTexturePatch.LogReplaceableTexture(spriteName, "Sprite - UnitySpriteRenderer");
        }

        if (!Plugin.Config.EnableCustomTextures.Value)
            return;

        // Try to replace with custom sprite
        Sprite customSprite = CustomTexturePatch.LoadCustomSprite(spriteName, value);
        if (customSprite != null)
        {
            value = customSprite;
            
            if (Plugin.Config.DetailedLogs.Value || isSavePoint || isDragon)
            {
                Plugin.Log.LogInfo($"[UnitySpriteRenderer] ✓ Replaced sprite: {spriteName}");
                if ((isSavePoint || isDragon) && customSprite.texture != null)
                {
                    Plugin.Log.LogInfo($"[UnitySpriteRenderer]   Custom texture: {customSprite.texture.name} ({customSprite.texture.width}x{customSprite.texture.height})");
                }
            }
        }
        else if (isSavePoint || isDragon)
        {
            // No custom sprite found (expected for many sprites)
        }
    }

    public static void Initialize()
    {
        if (Plugin.Config.DetailedLogs.Value)
        {
            Plugin.Log.LogInfo("Applying Unity SpriteRenderer patches");
        }
    }
}
