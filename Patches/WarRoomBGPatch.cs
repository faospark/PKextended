using System;
using UnityEngine;
using UnityEngine.UI;
using BepInEx.Logging;
using HarmonyLib;

namespace PKCore.Patches
{
    /// <summary>
    /// Monitors the War Room/Message Window state to replace the background (EventBG)
    /// when a specific character (fp_085) is speaking/active.
    /// </summary>
    /// <summary>
    /// Monitors the War Room/Message Window state to replace the background (EventBG)
    /// when message:1059 is called.
    /// </summary>
    /// <summary>
    /// Monitors the War Room/Message Window state to replace the background (EventBG)
    /// when AppRoot/Map/face has the tc_face_draw component.
    /// </summary>
    public class WarRoomBGPatch : MonoBehaviour
    {
        private static WarRoomBGPatch instance;
        private static bool _initialized = false;
        private static ManualLogSource Logger => Plugin.Log;

        // Configuration
        private const string FACE_OBJECT_PATH = "AppRoot/Map/face";
        private const string COMPONENT_NAME = "tc_face_draw";

        private const string BG_OBJECT_NAME = "EventBG";
        private const string OVERLAY_NAME = "WarRoomBG_Overlay";
        private const string NEW_BG_TEXTURE = "hp_classicmap_02";

        // State
        private float checkInterval = 0.5f;
        private float timer = 0f;

        // Cache
        private Sprite cachedBGSprite;

        public static void Initialize()
        {
            if (_initialized) return;

            GameObject host = new GameObject("PKCore_WarRoomBGPatch");
            DontDestroyOnLoad(host);
            host.hideFlags = HideFlags.HideAndDontSave;

            instance = host.AddComponent<WarRoomBGPatch>();
            _initialized = true;

            Logger.LogInfo("[WarRoomBGPatch] Initialized monitor with Overlay creation logic");
        }

        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < checkInterval) return;
            timer = 0f;

            CheckAndCreateOverlay();
        }

        private void CheckAndCreateOverlay()
        {
            // 1. Find the Face object
            GameObject faceObj = GameObject.Find(FACE_OBJECT_PATH);
            if (faceObj == null || !faceObj.activeInHierarchy)
                return;

            // 2. Check for the specific component by name
            // We use GetComponent(string) if it's a MonoBehaviour, or verify via list
            // Since we don't know the exact type/namespace, we iterate components
            bool hasComponent = false;
            foreach (var comp in faceObj.GetComponents<Component>())
            {
                // Unhollower/Il2CppInterop types might have specific naming, usually the type name matches
                if (comp.GetIl2CppType().Name.Contains(COMPONENT_NAME) || comp.GetType().Name.Contains(COMPONENT_NAME))
                {
                    hasComponent = true;
                    break;
                }
            }

            if (!hasComponent) return;

            CreateOverlay();
        }

        private void CreateOverlay()
        {
            // 1. Find EventBG
            GameObject eventBG = GameObject.Find(BG_OBJECT_NAME);
            if (eventBG == null) return;

            // 2. Check if overlay already exists
            Transform existingOverlay = eventBG.transform.Find(OVERLAY_NAME);
            if (existingOverlay != null) return;

            SpriteRenderer parentRenderer = eventBG.GetComponent<SpriteRenderer>();
            Sprite templateSprite = parentRenderer != null ? parentRenderer.sprite : null;

            // 3. Load replacement sprite
            if (cachedBGSprite == null)
            {
                // Use CustomTexturePatch to load, using parent sprite as template for PPU/Pivot
                cachedBGSprite = CustomTexturePatch.LoadCustomSprite(NEW_BG_TEXTURE, templateSprite);

                if (cachedBGSprite == null)
                {
                    // Fallback
                    Texture2D tex = CustomTexturePatch.LoadCustomTexture(NEW_BG_TEXTURE);
                    if (tex != null)
                    {
                        cachedBGSprite = Sprite.Create(
                             tex,
                             new Rect(0, 0, tex.width, tex.height),
                             new Vector2(0.5f, 0.5f),
                             100f
                        );
                        cachedBGSprite.name = NEW_BG_TEXTURE + "_Custom";
                        DontDestroyOnLoad(cachedBGSprite);
                    }
                }
            }

            if (cachedBGSprite != null)
            {
                Logger.LogInfo($"[WarRoomBGPatch] Creating {OVERLAY_NAME} on {BG_OBJECT_NAME} (Triggered by {COMPONENT_NAME})");

                // Create new object
                GameObject overlay = new GameObject(OVERLAY_NAME);
                overlay.transform.SetParent(eventBG.transform, false);
                overlay.transform.localPosition = Vector3.zero;
                overlay.transform.localScale = Vector3.one;
                overlay.transform.localRotation = Quaternion.identity;

                // Add SpriteRenderer
                SpriteRenderer sr = overlay.AddComponent<SpriteRenderer>();
                sr.sprite = cachedBGSprite;

                // Set sorting order higher than parent
                if (parentRenderer != null)
                {
                    sr.sortingLayerID = parentRenderer.sortingLayerID;
                    sr.sortingOrder = parentRenderer.sortingOrder + 1;
                }
                else
                {
                    sr.sortingOrder = 10; // Default high value
                }
            }
        }
    }
}
