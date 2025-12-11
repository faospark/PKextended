using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace PKextended.Patches;

public class ControllerPromptPatch
{
    private static bool _forceControllerType = false;
    private static int _controllerTypeIndex = 0;

    /// <summary>
    /// Patch UIConfigController.Start to override button sprites with preferred controller type
    /// </summary>
    [HarmonyPatch(typeof(UIConfigController), "Start")]
    [HarmonyPostfix]
    static void Start_Postfix(UIConfigController __instance)
    {
        if (!_forceControllerType)
            return;

        ApplyControllerPrompts(__instance);
    }

    /// <summary>
    /// Patch UIConfigController.SetLocalizeText to reapply button sprites when language changes
    /// </summary>
    [HarmonyPatch(typeof(UIConfigController), "SetLocalizeText")]
    [HarmonyPostfix]
    static void SetLocalizeText_Postfix(UIConfigController __instance)
    {
        if (!_forceControllerType)
            return;

        ApplyControllerPrompts(__instance);
    }

    private static void ApplyControllerPrompts(UIConfigController instance)
    {
        try
        {
            // Get the sprite arrays
            var leftSprites = instance.leftButtonSprites;
            var rightSprites = instance.rightButtonSprites;
            var leftButtons = instance.leftButtons;
            var rightButtons = instance.rightButtons;

            if (leftSprites == null || rightSprites == null || leftButtons == null || rightButtons == null)
                return;

            // Check if we have valid sprites at the selected index
            if (_controllerTypeIndex >= leftSprites.Length || _controllerTypeIndex >= rightSprites.Length)
            {
                Plugin.Log.LogWarning($"Controller type index {_controllerTypeIndex} out of range (max: {leftSprites.Length - 1})");
                return;
            }

            // Apply left button sprites
            for (int i = 0; i < leftButtons.Length; i++)
            {
                if (leftButtons[i] != null && i < leftSprites.Length)
                {
                    var sprite = GetSpriteForButton(leftSprites, i, _controllerTypeIndex);
                    if (sprite != null)
                    {
                        leftButtons[i].sprite = sprite;
                    }
                }
            }

            // Apply right button sprites
            for (int i = 0; i < rightButtons.Length; i++)
            {
                if (rightButtons[i] != null && i < rightSprites.Length)
                {
                    var sprite = GetSpriteForButton(rightSprites, i, _controllerTypeIndex);
                    if (sprite != null)
                    {
                        rightButtons[i].sprite = sprite;
                    }
                }
            }

            Plugin.Log.LogInfo($"Applied controller prompts for type index: {_controllerTypeIndex}");
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"Error applying controller prompts: {ex.Message}");
        }
    }

    private static Sprite GetSpriteForButton(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Sprite> sprites, int buttonIndex, int controllerTypeIndex)
    {
        // The sprite array structure might be:
        // - Multiple sprites per button (one for each controller type)
        // - Or might need to figure out the exact layout
        
        // Try direct index first (if sprites are organized as [button0_type0, button0_type1, button1_type0, button1_type1...])
        if (sprites != null && buttonIndex < sprites.Length)
        {
            return sprites[buttonIndex];
        }

        return null;
    }

    /// <summary>
    /// Discover available controller types by scanning sprite arrays
    /// </summary>
    public static void DiscoverControllerTypes()
    {
        try
        {
            Plugin.Log.LogInfo("Discovering controller button sprite sets...");
            
            var configController = Object.FindObjectOfType<UIConfigController>();
            
            if (configController == null)
            {
                Plugin.Log.LogInfo("UIConfigController not found yet (will discover when config menu opens)");
                return;
            }

            var leftSprites = configController.leftButtonSprites;
            var rightSprites = configController.rightButtonSprites;

            if (leftSprites != null)
            {
                Plugin.Log.LogInfo($"Left button sprites available: {leftSprites.Length}");
                for (int i = 0; i < leftSprites.Length && i < 10; i++)
                {
                    if (leftSprites[i] != null)
                    {
                        Plugin.Log.LogInfo($"  [{i}] {leftSprites[i].name}");
                    }
                }
            }

            if (rightSprites != null)
            {
                Plugin.Log.LogInfo($"Right button sprites available: {rightSprites.Length}");
                for (int i = 0; i < rightSprites.Length && i < 10; i++)
                {
                    if (rightSprites[i] != null)
                    {
                        Plugin.Log.LogInfo($"  [{i}] {rightSprites[i].name}");
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"Error discovering controller types: {ex.Message}");
        }
    }

    public static void Initialize()
    {
        _forceControllerType = Plugin.Config.ForceControllerPrompts.Value;
        
        if (!_forceControllerType)
        {
            Plugin.Log.LogInfo("Controller prompt override disabled");
            return;
        }

        string controllerType = Plugin.Config.ControllerPromptType.Value.ToLower();
        
        // Map controller names to likely indices
        // This might need adjustment based on actual game sprite organization
        _controllerTypeIndex = controllerType switch
        {
            "playstation" or "ps4" or "ps5" or "ps" => 0,
            "xbox" or "xb" => 1,
            "switch" or "nintendo" => 2,
            "pc" or "keyboard" => 3,
            _ => int.TryParse(controllerType, out int index) ? index : 0
        };

        Plugin.Log.LogInfo($"Controller prompt override enabled: {controllerType} (index: {_controllerTypeIndex})");
        Plugin.Log.LogInfo("Note: Button prompts will update when config menu is opened");
    }

    public static void ForceRefresh()
    {
        var configController = Object.FindObjectOfType<UIConfigController>();
        if (configController != null)
        {
            ApplyControllerPrompts(configController);
        }
    }
}
