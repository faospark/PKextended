using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace PKCore.Patches
{
    public class SaveWindowPatch
    {
        // Hook into GameObject.SetActive to catch save menu activation
        [HarmonyPatch(typeof(GameObject), nameof(GameObject.SetActive))]
        [HarmonyPostfix]
        public static void GameObject_SetActive_Postfix(GameObject __instance, bool value)
        {
            if (!value || !Plugin.Config.EnableCustomTextures.Value)
                return;

            // Only apply if ClassicSaveWindow is enabled
            if (!Plugin.Config.ClassicSaveWindow.Value)
                return;

            // Check which game we're in
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            bool isSuikoden1 = sceneName == "GSD1";
            bool isSuikoden2 = sceneName == "GSD2";
            
            if (!isSuikoden1 && !isSuikoden2)
                return;

            // Check if this is a save/load set (UI_System_SaveLoad_Set00, Set01, etc.)
            if (__instance.name.StartsWith("UI_System_SaveLoad_"))
            {
                // Apply scaling as requested (0.9, 0.9, 1)
                __instance.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
                
                // Find Img_BG child and make it black with 75% transparency (25% opacity)
                // Find Img_Frame first
                Transform imgFrame = __instance.transform.Find("Img_Frame");
                if (imgFrame != null)
                {
                    // Then find Img_BG as a child of Img_Frame
                    Transform imgBgChild = imgFrame.Find("Img_BG");
                    if (imgBgChild != null)
                    {
                        Image bgImage = imgBgChild.GetComponent<Image>();
                        if (bgImage != null)
                        {
                            bgImage.color = new Color(0f, 0f, 0f, 0.8f); // Black with 50% opacity
                        }
                    }
                }

                if (Plugin.Config.DetailedTextureLog.Value)
                {
                    // Processed save window
                }
                
                // Continue with searching for the main window to insert background
                // Find UI_System_SaveLoad1 (S1) or UI_System_SaveLoad2 (S2) - traverse up the hierarchy
                string saveLoadContainerName = isSuikoden1 ? "UI_System_SaveLoad1" : "UI_System_SaveLoad2";
                Transform current = __instance.transform;
                Transform uiSystemSaveLoad = null;
                
                // Try to find the appropriate UI_System_SaveLoad container by going up the hierarchy
                while (current != null && uiSystemSaveLoad == null)
                {
                    // Check if any sibling is the target container
                    if (current.parent != null)
                    {
                        // Use indexed loop instead of foreach to avoid IL2CPP cast issues
                        for (int i = 0; i < current.parent.childCount; i++)
                        {
                            Transform sibling = current.parent.GetChild(i);
                            if (sibling.name.StartsWith(saveLoadContainerName))
                            {
                                uiSystemSaveLoad = sibling;

                                break;
                            }
                        }
                    }
                    current = current.parent;
                }
                
                if (uiSystemSaveLoad != null)
                {
                    // Find Window01 inside the container
                    Transform window01 = uiSystemSaveLoad.Find("Window01");
                    if (window01 != null)
                    {

                        TryInsertBackground(window01.gameObject, isSuikoden1);

                        // Adjust Scrollbar Vertical
                        // Path: Window01/Panel/Panel/Scrollbar Vertical
                        Transform scrollbar = window01.Find("Panel/Panel/Scrollbar Vertical");
                        if (scrollbar != null)
                        {
            
                            scrollbar.localPosition = new Vector3(720.1979f, 368.3988f, 0f);
                            scrollbar.localScale = new Vector3(1.8f, 0.9f, 1f);

                        }
                    }
                    else
                    {
                        Plugin.Log.LogWarning($"[SaveWindowPatch] Could not find Window01, using {saveLoadContainerName} as fallback");
                        TryInsertBackground(uiSystemSaveLoad.gameObject, isSuikoden1);
                    }
                }
                else
                {

                    // Fallback: use the parent of UI_System_SaveLoad_Set00
                    if (__instance.transform.parent != null)
                    {

                        TryInsertBackground(__instance.transform.parent.gameObject, isSuikoden1);
                    }
                }
            }
        }

        private static void TryInsertBackground(GameObject saveLoadWindow, bool isSuikoden1)
        {
            // Use different object names per game to allow both to coexist
            string customObjName = isSuikoden1 ? "ClassicSaveBackground_S1" : "ClassicSaveBackground_S2";
            if (saveLoadWindow.transform.Find(customObjName) != null)
            {
                if (Plugin.Config.DetailedTextureLog.Value)
                {
                    // Background already exists
                }
                return;
            }

            if (Plugin.Config.DetailedTextureLog.Value)
            {
                // Creating custom background
            }

            // Modify the default HD Remaster Img_bg to black with 20% alpha
            Transform imgBg = saveLoadWindow.transform.Find("Img_bg");
            if (imgBg != null)
            {
                imgBg.gameObject.SetActive(false);
                if (Plugin.Config.DetailedTextureLog.Value)
                {

                }
            }
            
            Transform imgFlame = saveLoadWindow.transform.Find("Img_Flame");
            if (imgFlame != null)
            {
                imgFlame.gameObject.SetActive(false);
                if (Plugin.Config.DetailedTextureLog.Value)
                {

                }
            }

            // Create custom object as child of the save/load window
            GameObject customObj = new GameObject(customObjName);
            
            // Set parent with worldPositionStays=false to prevent destruction
            customObj.transform.SetParent(saveLoadWindow.transform, false);
            
            // Place it as the FIRST child (index 0) to render BEHIND everything else
            // In Unity UI, lower sibling index = rendered first = appears behind
            customObj.transform.SetSiblingIndex(0);
            
            // Setup RectTransform to cover fullscreen from Window01
            RectTransform rt = customObj.AddComponent<RectTransform>();
            
            // Use center anchors with explicit size for fullscreen coverage (1920x1080)
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(1920f, 1080f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localPosition = new Vector3(0, 0, 100); // Z=100 to match other UI elements
            if (Plugin.Config.DetailedTextureLog.Value)
            {

            }

            // Add Image component (this automatically adds CanvasRenderer)
            Image img = customObj.AddComponent<Image>();
            img.color = Color.white; // Ensure image is fully opaque
            img.raycastTarget = false; // Don't block input
            if (Plugin.Config.DetailedTextureLog.Value)
            {

            }
            
            // Load appropriate texture based on game
            string textureName = isSuikoden1 ? "hp_classicmap_01" : "hp_classicmap_02";
            Texture2D tex = CustomTexturePatch.LoadCustomTexture(textureName);
            if (tex != null)
            {
                Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                img.sprite = sprite;
                if (Plugin.Config.DetailedTextureLog.Value)
                {
                    Plugin.Log.LogInfo($"[SaveWindowPatch] ✓ Applied {textureName} to save window background");
                }
            }
            else
            {
                Plugin.Log.LogError($"[SaveWindowPatch] Failed to load {textureName}");
            }
        }
    }
}
