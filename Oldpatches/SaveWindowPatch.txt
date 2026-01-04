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

            // Only apply if S2ClassicSaveWindow is enabled
            if (!Plugin.Config.S2ClassicSaveWindow.Value)
                return;

            // Only apply to Suikoden 2 (GSD2)
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "GSD2")
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

                Plugin.Log.LogInfo($"[SaveWindowPatch] Processed {__instance.name}");
                
                // Continue with searching for the main window to insert background
                // Find UI_System_SaveLoad2 - traverse up the hierarchy
                Transform current = __instance.transform;
                Transform uiSystemSaveLoad2 = null;
                
                // Try to find UI_System_SaveLoad2 by going up the hierarchy
                while (current != null && uiSystemSaveLoad2 == null)
                {
                    // Check if any sibling is UI_System_SaveLoad2
                    if (current.parent != null)
                    {
                        // Use indexed loop instead of foreach to avoid IL2CPP cast issues
                        for (int i = 0; i < current.parent.childCount; i++)
                        {
                            Transform sibling = current.parent.GetChild(i);
                            if (sibling.name.StartsWith("UI_System_SaveLoad2"))
                            {
                                uiSystemSaveLoad2 = sibling;
                                // Plugin.Log.LogInfo($"[SaveWindowPatch] ✓ Found UI_System_SaveLoad2");
                                break;
                            }
                        }
                    }
                    current = current.parent;
                }
                
                if (uiSystemSaveLoad2 != null)
                {
                    // Find Window01 inside UI_System_SaveLoad2
                    Transform window01 = uiSystemSaveLoad2.Find("Window01");
                    if (window01 != null)
                    {
                        // Plugin.Log.LogInfo($"[SaveWindowPatch] Found Window01, inserting fullscreen background there");
                        TryInsertBackground(window01.gameObject);

                        // Adjust Scrollbar Vertical
                        // Path: Window01/Panel/Panel/Scrollbar Vertical
                        Transform scrollbar = window01.Find("Panel/Panel/Scrollbar Vertical");
                        if (scrollbar != null)
                        {
            
                            scrollbar.localPosition = new Vector3(720.1979f, 368.3988f, 0f);
                            scrollbar.localScale = new Vector3(1.8f, 0.9f, 1f);
                            // Plugin.Log.LogInfo("[SaveWindowPatch] Adjusted Scrollbar Vertical transform");
                        }
                    }
                    else
                    {
                        Plugin.Log.LogWarning("[SaveWindowPatch] Could not find Window01, using UI_System_SaveLoad2 as fallback");
                        TryInsertBackground(uiSystemSaveLoad2.gameObject);
                    }
                }
                else
                {
                    // Plugin.Log.LogWarning("[SaveWindowPatch] Could not find UI_System_SaveLoad2, using parent of Set00 as fallback");
                    // Fallback: use the parent of UI_System_SaveLoad_Set00
                    if (__instance.transform.parent != null)
                    {
                        // Plugin.Log.LogInfo($"[SaveWindowPatch] Using parent: {__instance.transform.parent.name}");
                        TryInsertBackground(__instance.transform.parent.gameObject);
                    }
                }
            }
        }

        private static void TryInsertBackground(GameObject saveLoadWindow)
        {
            // Check if we already created our object
            string customObjName = "ClassicSaveBackground";
            if (saveLoadWindow.transform.Find(customObjName) != null)
            {
                Plugin.Log.LogInfo("[SaveWindowPatch] ClassicSaveBackground already exists, skipping");
                return;
            }

            Plugin.Log.LogInfo($"[SaveWindowPatch] Creating custom background in {saveLoadWindow.name}");

            // Modify the default HD Remaster Img_bg to black with 20% alpha
            Transform imgBg = saveLoadWindow.transform.Find("Img_bg");
            if (imgBg != null)
            {
                imgBg.gameObject.SetActive(false);
                Plugin.Log.LogInfo("[SaveWindowPatch] Disabled Img_bg");
            }
            
            Transform imgFlame = saveLoadWindow.transform.Find("Img_Flame");
            if (imgFlame != null)
            {
                imgFlame.gameObject.SetActive(false);
                Plugin.Log.LogInfo("[SaveWindowPatch] Disabled Img_Flame");
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
            Plugin.Log.LogInfo($"[SaveWindowPatch] RectTransform configured for fullscreen");

            // Add Image component (this automatically adds CanvasRenderer)
            Image img = customObj.AddComponent<Image>();
            img.color = Color.white; // Ensure image is fully opaque
            img.raycastTarget = false; // Don't block input
            Plugin.Log.LogInfo($"[SaveWindowPatch] Image component added, color: {img.color}");
            
            // Load texture
            Texture2D tex = CustomTexturePatch.LoadCustomTexture("hp_classicmap_02");
            if (tex != null)
            {
                Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                img.sprite = sprite;
                Plugin.Log.LogInfo("[SaveWindowPatch] ✓ Applied hp_classicmap_02 to save window background");
            }
            else
            {
                Plugin.Log.LogError("[SaveWindowPatch] Failed to load hp_classicmap_02");
            }
        }
    }
}
