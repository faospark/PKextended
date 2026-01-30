using HarmonyLib;
using UnityEngine;
using ShareUI.Menu;
using ShareUI.Battle;
using DG.Tweening;

namespace PKCore.Patches;

/// <summary>
/// Patches to apply custom scaling and positioning to UI menu elements
/// Hooks into both UIMainTopMenu and UIMainItemMenu OpenWindow to apply transformations
/// </summary>
public class MenuScalePatch
{
    /// <summary>
    /// Shared method to apply menu transformations
    /// </summary>
    private static void ApplyMenuTransformations(GameObject menuObject, string menuType)
    {
        // Check configuration setting
        string menuScaleSetting = Plugin.Config.ScaledDownMenu.Value.ToLower();
        if (menuScaleSetting != "true")
            return;
        
        // Navigate to UI_Set: UIMainMenu -> UI_Canvas_Root -> UIMainMenu(Clone) -> UI_Set
        // First go up to UI_Canvas_Root
        Transform uiCanvasRoot = menuObject.transform.parent;
        // In battle, the parent might be different or we might be deeper. 
        // Let's try to find UI_Canvas_Root by name if parent is not it.
        if (uiCanvasRoot == null || uiCanvasRoot.name != "UI_Canvas_Root")
        {
            Transform current = menuObject.transform;
            while (current.parent != null)
            {
                if (current.parent.name == "UI_Canvas_Root")
                {
                    uiCanvasRoot = current.parent;
                    break;
                }
                current = current.parent;
            }
        }

        if (uiCanvasRoot == null)
        {
            Plugin.Log.LogWarning($"[MenuScale] Could not find UI_Canvas_Root for {menuObject.name}");
            return;
        }

        // Apply transformations to elements directly under UI_Canvas_Root
        
        // Header: Apply scale and position
        Transform header = uiCanvasRoot.Find("UI_Com_Header(Clone)") ?? uiCanvasRoot.Find("UI_Com_Header");
        if (header != null)
        {
            Vector3 targetScale = new Vector3(0.8f, 0.8f, 1f);
            Vector3 targetPosition = new Vector3(194.4f, 108f, 0f);
            header.DOKill();
            header.DOScale(targetScale, 0.2f).SetEase(Ease.OutCubic);
            header.DOLocalMove(targetPosition, 0.2f).SetEase(Ease.OutCubic);
        }

        // BackLog: Apply scale and position to Set01 child
        Transform backLog = uiCanvasRoot.Find("UI_Com_BackLog01(Clone)") ?? uiCanvasRoot.Find("UI_Com_BackLog01");
        if (backLog != null)
        {
            Transform set01 = backLog.Find("Set01");
            if (set01 != null)
            {
                Vector3 targetScale = new Vector3(0.8f, 0.8f, 1f);
                Vector3 targetPosition = new Vector3(0f, 86.4f, 0f);
                set01.DOKill();
                set01.DOScale(targetScale, 0.2f).SetEase(Ease.OutCubic);
                set01.DOLocalMove(targetPosition, 0.2f).SetEase(Ease.OutCubic);
            }
        }

        // Config: Apply scale and position
        Transform config = uiCanvasRoot.Find("UI_Config_01(Clone)") ?? uiCanvasRoot.Find("UI_Config_01");
        if (config != null)
        {
            Vector3 targetScale = new Vector3(0.8f, 0.8f, 1f);
            Vector3 targetPosition = new Vector3(0f, 86.4f, 0f);
            config.DOKill();
            config.DOScale(targetScale, 0.2f).SetEase(Ease.OutCubic);
            config.DOLocalMove(targetPosition, 0.2f).SetEase(Ease.OutCubic);
        }

        // Battle Result: Apply scale and position to Result_layout child
        Transform battleresult = uiCanvasRoot.Find("UI_Battle_Result_Main(Clone)") ?? uiCanvasRoot.Find("UI_Battle_Result_Main");
        if (battleresult != null)
        {
            Transform resultLayout = battleresult.Find("Result_layout");
            if (resultLayout != null)
            {
                Vector3 targetScale = new Vector3(0.8f, 0.8f, 1f);
                Vector3 targetPosition = new Vector3(0f, 86.4f, 0f);
                resultLayout.DOKill();
                resultLayout.DOScale(targetScale, 0.2f).SetEase(Ease.OutCubic);
                resultLayout.DOLocalMove(targetPosition, 0.2f).SetEase(Ease.OutCubic);
            }
        }

        // Now look for UIMainMenu(Clone) -> UI_Set for specific menu items
        Transform uiMainMenuClone = uiCanvasRoot.Find("UIMainMenu(Clone)");
        if (uiMainMenuClone != null)
        {
            Transform uiSet = uiMainMenuClone.Find("UI_Set");
            if (uiSet != null)
            {
                // TopMenu: Apply scale only (no position change)
                Transform topMenu = uiSet.Find("TopMenu");
                if (topMenu != null)
                {
                    Vector3 targetScale = new Vector3(0.8f, 0.8f, 1f);
                    topMenu.DOKill();
                    topMenu.DOScale(targetScale, 0.2f).SetEase(Ease.OutCubic);
                }
                
                // Other menus: Apply both scale and position
                string[] menuNames = { "ItemMenu", "EmblemMenu", "Equipment_Set", "State_Set", "Formation_Set" };
                
                foreach (string menuName in menuNames)
                {
                    Transform menuTransform = uiSet.Find(menuName);
                    if (menuTransform != null)
                    {
                        // Apply transformations with smooth animation
                        Vector3 targetScale = new Vector3(0.8f, 0.8f, 1f);
                        Vector3 targetPosition = new Vector3(0f, 75.6f, 0f);
                        
                        // Kill any existing tweens on this transform
                        menuTransform.DOKill();
                        
                        // Animate scale and position smoothly
                        menuTransform.DOScale(targetScale, 0.2f).SetEase(Ease.OutCubic);
                        menuTransform.DOLocalMove(targetPosition, 0.2f).SetEase(Ease.OutCubic);
                    }
                }
                
                // Handle Formation_Set's Set_01 child after parent transformations
                Transform formationSet = uiSet.Find("Formation_Set");
                if (formationSet != null)
                {
                    Transform set01 = formationSet.Find("Set_01");
                    if (set01 != null)
                    {
                        Vector3 set01Position = new Vector3(0f, -162f, 0f);
                        set01.DOKill();
                        set01.DOLocalMove(set01Position, 0.2f).SetEase(Ease.OutCubic);
                    }
                }
                
                // Footer Container: Apply scale and position to UI_Com_Footer/Img_Bg/Container
                Transform footerPath = uiSet.Find("UI_Com_Footer");
                if (footerPath != null)
                {
                    Transform imgBg = footerPath.Find("Img_Bg");
                    if (imgBg != null)
                    {
                        Transform container = imgBg.Find("Container");
                        if (container != null)
                        {
                            Vector3 targetScale = new Vector3(0.8f, 0.8f, 1f);
                            Vector3 targetPosition = new Vector3(-171.5999f, 0f, 0f);
                            
                            container.DOKill();
                            container.DOScale(targetScale, 0.2f).SetEase(Ease.OutCubic);
                            container.DOLocalMove(targetPosition, 0.2f).SetEase(Ease.OutCubic);
                        }
                    }
                }
            }
        }
        
    }
    
    /// <summary>
    /// Patch for GameObject.SetActive to catch menu activations
    /// This approach catches UI elements when they become active, providing broader coverage
    /// </summary>
    [HarmonyPatch(typeof(GameObject), nameof(GameObject.SetActive))]
    [HarmonyPostfix]
    public static void GameObject_SetActive_Postfix(GameObject __instance, bool value)
    {
        // Only process when activating (not deactivating)
        if (!value) return;
        
        // Check configuration setting first
        string menuScaleSetting = Plugin.Config.ScaledDownMenu.Value.ToLower();
        if (menuScaleSetting != "true") return;
        
        // Check if this is a UI object we care about
        string objectName = __instance.name;
        
        bool isTargetUI = objectName.Contains("UIMainMenu") || 
                         objectName.Contains("UI_Battle_Result") ||
                         objectName.Contains("UI_Com_Header") ||
                         objectName.Contains("UI_Com_BackLog") ||
                         objectName.Contains("UI_Config_01") ||
                         objectName.Contains("UI_Com_Footer") ||
                         objectName.StartsWith("TopMenu") ||
                         objectName.StartsWith("ItemMenu") ||
                         objectName.StartsWith("EmblemMenu") ||
                         objectName.StartsWith("Equipment_Set") ||
                         objectName.StartsWith("State_Set") ||
                         objectName.StartsWith("Formation_Set") ||
                         objectName == "Set_01" ||
                         (objectName == "Container" && IsFooterContainer(__instance));
        
        if (!isTargetUI) return;
        
        if (Plugin.Config.DetailedTextureLog.Value)
        {
            Plugin.Log.LogInfo($"[MenuScale] Detected UI activation: {objectName}");
        }
        
        // Apply transformations directly
        ApplyMenuTransformations(__instance, objectName);
    }
    
    /// <summary>
    /// Check if a Container object is the footer container we care about
    /// </summary>
    private static bool IsFooterContainer(GameObject container)
    {
        // Check if this Container is in the footer hierarchy:
        // UI_Com_Footer/Img_Bg/Container
        Transform parent = container.transform.parent;
        if (parent != null && parent.name == "Img_Bg")
        {
            Transform grandparent = parent.parent;
            return grandparent != null && grandparent.name == "UI_Com_Footer";
        }
        return false;
    }
}
