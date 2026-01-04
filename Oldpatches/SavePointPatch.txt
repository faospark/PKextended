using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.Injection;

namespace PKCore.Patches
{
    /// <summary>
    /// Dedicated patch for handling Save Point sprite replacements.
    /// Handles monitoring, preloading, and Resources.Load interception for save points.
    /// </summary>
    public static class SavePointPatch
    {
        // Dictionary to hold preloaded sprites for this patch
        internal static Dictionary<string, Sprite> preloadedSavePointSprites = new Dictionary<string, Sprite>();

        public static void Initialize()
        {
            try 
            {
                ClassInjector.RegisterTypeInIl2Cpp<SavePointSpriteMonitor>();
                Plugin.Log.LogInfo("[SavePointPatch] Registered SavePointSpriteMonitor type");
                
                // Initialize the Resources.Load patch manually since we aren't using Harmony.PatchAll() on this specific static class 
                // typically, but the original code used [HarmonyPatch] attributes. 
                // If Plugin.cs uses Harmony.CreateAndPatchAll(Assembly), it will find this class if we add attributes?
                // The original SavePointPatch logic was just a helper. The patch was in CustomTexturePatch partial.
                // We should probably rely on manual patching or ensure this class is scanned.
                // For safety, let's keep the nested patch class with attributes so Harmony finds it if scanning assembly.
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[SavePointPatch] Failed to register SavePointSpriteMonitor: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if an object is a save point logic part and attaches a monitor if needed
        /// </summary>
        public static void CheckAndAttachMonitor(GameObject go)
        {
            if (go == null) return;

            // Check against known save point texture names
            bool isSavePoint = false;
            
            // Name check
            if (go.name.Contains("savePoint", StringComparison.OrdinalIgnoreCase))
                isSavePoint = true;
                
            // Sprite check (if name is generic)
            if (!isSavePoint)
            {
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null && sr.sprite.name.Contains("savePoint", StringComparison.OrdinalIgnoreCase))
                    isSavePoint = true;
            }

            if (!isSavePoint) return;

            // Only attach if not already present
            if (go.GetComponent<SavePointSpriteMonitor>() != null) return;

            if (Plugin.Config.DetailedTextureLog.Value)
            {
                Plugin.Log.LogInfo($"[SavePointPatch] Attaching monitor to: {go.name}");
            }
            
            // If it's the ball/orb, attach the monitor
            // The monitor will handle disable glow logic too if it finds parents
            go.AddComponent<SavePointSpriteMonitor>();
        }

        /// <summary>
        /// Preload save point animation frames (t_obj_savePoint_ball_0 through _10).
        /// Replaces the old PreloadSavePointSprites method.
        /// </summary>
        public static void PreloadSprites()
        {
            if (Plugin.Config.DetailedTextureLog.Value)
                 Plugin.Log.LogInfo("[SavePoint Preload] Checking for atlas...");

            string atlasName = "t_obj_savePoint_ball";
            // We need to access CustomTexturePatch's helper for loading.
            // Ideally we move those helpers to a shared Utility class, but for now calling public/internal static methods on CustomTexturePatch is fine.
            string atlasLookupName = TextureOptions.GetTextureNameWithVariant(atlasName);
            
            // Accessing internal dictionary from CustomTexturePatch might be restricted if not in same assembly (they are).
            if (!CustomTexturePatch.texturePathIndex.ContainsKey(atlasLookupName)) return;

            Texture2D atlasTexture = CustomTexturePatch.LoadCustomTexture(atlasName);
            if (atlasTexture == null) return;

            // Atlas specs
            int frameWidth = 100;
            int frameHeight = 100;
            int columns = 4;
            
            for (int i = 0; i <= 10; i++)
            {
                string frameName = $"t_obj_savePoint_ball_{i}";
                int frameIndex = i % 8; 
                int col = frameIndex % columns;
                int row = frameIndex / columns;
                
                float x = col * frameWidth;
                float y = atlasTexture.height - (row + 1) * frameHeight; 
                
                Sprite sprite = Sprite.Create(
                    atlasTexture,
                    new Rect(x, y, frameWidth, frameHeight),
                    new Vector2(0.5f, 0.5f), 
                    100f, 
                    0,
                    SpriteMeshType.FullRect
                );

                if (sprite != null)
                {
                    UnityEngine.Object.DontDestroyOnLoad(sprite);
                    preloadedSavePointSprites[frameName] = sprite;
                }
            }
            UnityEngine.Object.DontDestroyOnLoad(atlasTexture);
            if (Plugin.Config.DetailedTextureLog.Value) Plugin.Log.LogInfo($"[SavePoint Preload] Preloaded {preloadedSavePointSprites.Count} frames.");
        }

        /// <summary>
        /// Patch Resources.Load to intercept save point sprites.
        /// Moved from CustomTexturePatch partial.
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
                 if (__result == null || !(__result is Sprite)) return;

                 Sprite sprite = __result as Sprite;
                 string spriteName = sprite.name;

                 if (spriteName.StartsWith("t_obj_savePoint_ball_") && preloadedSavePointSprites.TryGetValue(spriteName, out Sprite customSprite))
                 {
                     if (Plugin.Config.DetailedTextureLog.Value)
                        Plugin.Log.LogInfo($"[SavePoint] Replacing Resources.Load result: {spriteName}");
                     __result = customSprite;
                 }
            }
        }
    }

    /// <summary>
    /// Monitor component to enforce custom save point sprites and disable effects
    /// </summary>
    public class SavePointSpriteMonitor : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private bool _hasLoggedGlowDisable = false;
        private string _lastSpriteName;
        private Sprite _customSprite;
        private string _lastEnforcedLogName;

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        void Start()
        {
            // One-time logic: Disable glow if configured
            if (Plugin.Config.DisableSavePointGlow.Value)
            {
                 DisableGlowEffect();
            }
        }

        private void DisableGlowEffect()
        {
            // Navigate up to find root, then down for glow
            Transform current = transform;
            while (current != null && !current.name.Contains("savePoint") && !current.name.Contains("MapBackGround"))
            {
                current = current.parent;
                // Safety break for root
                if (current != null && current.parent == null) break;
            }
            
            if (current != null)
            {
                // S2 Structure: .../Fire_add/Glow_add or Particle_add/Glow_add
                Transform glowTransform = current.Find("Fire_add/Glow_add"); // Typical for S2
                if (glowTransform == null) glowTransform = current.Find("Particle_add/Glow_add");
                
                if (glowTransform != null)
                {
                    glowTransform.gameObject.SetActive(false);
                    if (!_hasLoggedGlowDisable)
                    {
                        Plugin.Log.LogInfo("[SavePoint] ✓ Disabled save point glow effect");
                        _hasLoggedGlowDisable = true;
                    }
                }
            }
        }

        void LateUpdate()
        {
             if (_renderer == null || _renderer.sprite == null) return;

            string currentSpriteName = _renderer.sprite.name;
            
            // Strip (Clone) if present
            if (currentSpriteName.EndsWith("(Clone)"))
            {
                currentSpriteName = currentSpriteName.Substring(0, currentSpriteName.Length - 7);
            }

            // Only act on save point sprites
            if (!currentSpriteName.Contains("savePoint")) return;

            // Optimization: Check for change
            if (_lastSpriteName != currentSpriteName)
            {
                _lastSpriteName = currentSpriteName;
                
                // Try load custom sprite
                Sprite replacement = CustomTexturePatch.LoadCustomSprite(currentSpriteName, _renderer.sprite);
                
                if (replacement != null)
                {
                    _customSprite = replacement;
                    _renderer.sprite = replacement;

                    if (Plugin.Config.DetailedTextureLog.Value && _lastEnforcedLogName != currentSpriteName)
                    {
                        Plugin.Log.LogInfo($"[SavePointMonitor] Enforced: {currentSpriteName}");
                        _lastEnforcedLogName = currentSpriteName;
                    }
                }
                else
                {
                     _customSprite = null;
                     _lastEnforcedLogName = null;
                }
            }
            else if (_customSprite != null && _renderer.sprite != _customSprite)
            {
                // Animator reset it -> force back
                 _renderer.sprite = _customSprite;
            }
        }
    }
}
