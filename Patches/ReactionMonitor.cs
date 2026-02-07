using System;
using UnityEngine;
using UnityEngine.UI;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;

namespace PKCore.Patches
{
    /// <summary>
    /// Monitors the game for specific object activation ('r_action') 
    /// and triggers a custom UI reaction overlay.
    /// </summary>
    public class ReactionMonitor : MonoBehaviour
    {
        private static ManualLogSource Logger => Plugin.Log;
        private static ReactionMonitor instance;

        // Configuration
        private const string TRIGGER_OBJECT_PATH = "AppRoot/Map/MapChara/r_action";
        private const string PORTRAIT_NAME = "fu"; // filename: fu.png
        
        // State
        private GameObject triggerObject;
        private GameObject overlayRoot;
        private Image overlayImage; // The purple background
        private Image portraitImage; // The "fu" portrait
        
        private float checkInterval = 0.1f; // Check more frequently (100ms) for UI sync
        private float timer = 0f;
        private bool wasTriggerActive = false;
        private bool wasWindowActive = false;
        
        // Fade effect removed per request

        public static void Initialize()
        {
            if (instance != null) return;

            // Create a hidden GameObject to host this script
            GameObject host = new GameObject("PKCore_ReactionMonitor");
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.hideFlags = HideFlags.HideAndDontSave;
            
            instance = host.AddComponent<ReactionMonitor>();
            Logger.LogInfo("[ReactionMonitor] Initialized");
        }

        private void Update()
        {
            // Throttle checks
            timer += Time.unscaledDeltaTime;
            if (timer < checkInterval) return;
            timer = 0f;

            CheckTrigger();
        }

        private void CheckTrigger()
        {
            // 1. Find the trigger object if we lost it
            if (triggerObject == null)
            {
                triggerObject = GameObject.Find(TRIGGER_OBJECT_PATH);
                if (triggerObject != null)
                     Logger.LogInfo($"[ReactionMonitor] Found '{TRIGGER_OBJECT_PATH}' (Active: {triggerObject.activeInHierarchy})");
            }

            // 2. Check trigger active state
            bool isTriggerActive = triggerObject != null && triggerObject.activeInHierarchy;
            
            // 3. Check window active state
            bool isWindowActive = CheckWindowActive();

            // 4. Combined condition: Trigger MUST be active, AND Window MUST be active
            bool shouldShow = isTriggerActive && isWindowActive;

            // State change detection
            if (shouldShow != (wasTriggerActive && wasWindowActive))
            {
                // If we are showing now, and weren't before (or vice versa on the combined state)
                if (shouldShow)
                {
                    OnTriggerActivated();
                }
                else
                {
                    OnTriggerDeactivated();
                }
            }
            
            wasTriggerActive = isTriggerActive;
            wasWindowActive = isWindowActive;
        }

        private bool CheckWindowActive()
        {
            // Look for "window_big_hd(Clone)" in UI_Root/UI_Canvas_Root/
            // Since we can't easily find clones by path, we'll find the root and iterate children
            GameObject canvasRoot = GameObject.Find("UI_Root/UI_Canvas_Root");
            if (canvasRoot == null) return false;

            int childCount = canvasRoot.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = canvasRoot.transform.GetChild(i);
                if (child.gameObject.activeInHierarchy && child.name.StartsWith("window_big_hd(Clone)"))
                {
                    return true;
                }
            }
            return false;
        }

        private void OnTriggerActivated()
        {
            Logger.LogInfo($"[ReactionMonitor] Trigger '{TRIGGER_OBJECT_PATH}' ACTIVATED! Showing overlay.");
            ShowOverlay();
        }

        private void OnTriggerDeactivated()
        {
            // Logger.LogInfo($"[ReactionMonitor] Trigger Deactivated.");
            HideOverlay();
        }

        private void ShowOverlay()
        {
            // Ensure UI exists
            if (overlayRoot == null)
            {
                CreateOverlay();
            }

            if (overlayRoot != null)
            {
                overlayRoot.SetActive(true);
                // Ensure fully opaque
                if (portraitImage != null)
                {
                    Color c = portraitImage.color;
                    c.a = 1f;
                    portraitImage.color = c;
                }
            }
        }

        private void HideOverlay()
        {
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(false);
            }
        }

        private void CreateOverlay()
        {
            try
            {
                // Find a canvas to attach to. UI_Root/UI_Canvas_Root is standard.
                GameObject canvasRoot = GameObject.Find("UI_Root/UI_Canvas_Root");
                if (canvasRoot == null)
                {
                    Logger.LogWarning("[ReactionMonitor] Could not find UI_Root/UI_Canvas_Root");
                    return; // Try again later
                }

                // 1. Create Root Panel (Purple Background)
                overlayRoot = new GameObject("fu");
                overlayRoot.transform.SetParent(canvasRoot.transform, false);
                
                RectTransform rootRect = overlayRoot.AddComponent<RectTransform>();
                // User requested size 300x300 and specific position.
                rootRect.anchorMin = new Vector2(0.5f, 0.5f);
                rootRect.anchorMax = new Vector2(0.5f, 0.5f);
                rootRect.pivot = new Vector2(0.5f, 0.5f);
                rootRect.sizeDelta = new Vector2(300, 300); 
                rootRect.anchoredPosition = new Vector2(587.9171f, -313.2f); // Updated per user request (587.9171, -313.2, 0)
                // Note: user asked for "local position ... 0", anchoredPosition is safe for Canvas UIs.
                
                // Add Image for background color (DISABLED per request)
                overlayImage = overlayRoot.AddComponent<Image>();
                overlayImage.color = Color.clear; // Invisible
                overlayImage.raycastTarget = false;

                // 2. Create Portrait Image (Child)
                GameObject portraitObj = new GameObject("Portrait");
                portraitObj.transform.SetParent(overlayRoot.transform, false);
                
                RectTransform portraitRect = portraitObj.AddComponent<RectTransform>();
                portraitRect.anchorMin = Vector2.zero;
                portraitRect.anchorMax = Vector2.one;
                portraitRect.offsetMin = Vector2.zero;
                portraitRect.offsetMax = Vector2.zero; // Fill the purple box?
                
                portraitImage = portraitObj.AddComponent<Image>();
                portraitImage.raycastTarget = false;
                portraitImage.preserveAspect = true;

                // Load the texture
                Texture2D tex = NPCPortraitPatch.LoadPortraitTexture(PORTRAIT_NAME);
                if (tex != null)
                {
                    Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    UnityEngine.Object.DontDestroyOnLoad(sprite);
                    UnityEngine.Object.DontDestroyOnLoad(tex);
                    portraitImage.sprite = sprite;
                }
                else
                {
                    Logger.LogWarning($"[ReactionMonitor] Could not load portrait '{PORTRAIT_NAME}'");
                }
                
                Logger.LogInfo("[ReactionMonitor] Overlay UI created successfully.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"[ReactionMonitor] Failed to create overlay: {ex.Message}");
            }
        }
    }
}
