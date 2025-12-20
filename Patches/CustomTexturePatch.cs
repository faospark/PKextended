using HarmonyLib;
using UnityEngine;
using UnityEngine.UI; // UI components (Image, RawImage)
using UnityEngine.U2D; // SpriteAtlas support
using UnityEngine.SceneManagement; // Scene loading
using UnityEngine.AddressableAssets; // Addressables support
using UnityEngine.ResourceManagement.AsyncOperations; // AsyncOperationHandle
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace PKCore.Patches;

/// <summary>
/// Replaces game textures/sprites with custom PNG files from a folder
/// </summary>
public class CustomTexturePatch
{
    private static Dictionary<string, Sprite> customSpriteCache = new Dictionary<string, Sprite>();
    private static Dictionary<string, Texture2D> customTextureCache = new Dictionary<string, Texture2D>();
    private static Dictionary<string, string> texturePathIndex = new Dictionary<string, string>(); // Maps texture name -> full file path
    private static HashSet<string> loggedTextures = new HashSet<string>(); // Track logged textures to prevent duplicates
    private static HashSet<string> replacedTextures = new HashSet<string>(); // Track replaced textures to prevent duplicate replacement logs
    private static Dictionary<string, Sprite> preloadedBathSprites = new Dictionary<string, Sprite>(); // Preloaded bath_1 to bath_5
    private static int lastBathBGInstanceID = -1; // Track the last BathBG instance we replaced
    private static string customTexturesPath;

    #region Helper Functions

    /// <summary>
    /// Log a replaceable texture/sprite (only once per texture)
    /// </summary>
    private static void LogReplaceableTexture(string textureName, string category, string context = null)
    {
        if (!Plugin.Config.LogReplaceableTextures.Value || loggedTextures.Contains(textureName))
            return;

        loggedTextures.Add(textureName);
        string message = string.IsNullOrEmpty(context) 
            ? $"[Replaceable {category}] {textureName}"
            : $"[Replaceable {category}] {textureName} ({context})";
        Plugin.Log.LogInfo(message);
    }

    /// <summary>
    /// Check if we should skip logging for spam reduction (sactx and character textures)
    /// </summary>
    private static bool ShouldSkipSpamLog(string textureName)
    {
        if (textureName.StartsWith("sactx"))
            return true;

        if (texturePathIndex.TryGetValue(textureName, out string texPath))
        {
            if (texPath.ToLower().Contains("characters"))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Try to replace bath sprites in the BathBG GameObject
    /// Checks for new BathBG instances and replaces bath_1 through bath_5 sprites
    /// </summary>
    /// <returns>Number of sprites replaced, or -1 if BathBG not found or not a new instance</returns>
    private static int TryReplaceBathSprites()
    {
        if (!Plugin.Config.EnableCustomTextures.Value)
            return -1;

        var bathBG = GameObject.Find("AppRoot/BathBG");
        if (bathBG == null)
            return -1;

        int currentInstanceID = bathBG.GetInstanceID();
        
        // Only replace if this is a NEW BathBG instance (different from last one)
        if (currentInstanceID == lastBathBGInstanceID)
            return -1;

        if (Plugin.Config.DetailedTextureLog.Value)
        {
            Plugin.Log.LogInfo($"New BathBG instance detected (ID: {currentInstanceID}, previous: {lastBathBGInstanceID})");
        }
        
        lastBathBGInstanceID = currentInstanceID;
        
        // Get all SpriteRenderers in BathBG
        var bathRenderers = bathBG.GetComponentsInChildren<SpriteRenderer>(true);
        int replaced = 0;
        
        foreach (var sr in bathRenderers)
        {
            if (sr.sprite != null)
            {
                string srSpriteName = sr.sprite.name;
                
                // Check if this is a bath sprite and we have a custom texture
                if (srSpriteName.StartsWith("bath_") && texturePathIndex.ContainsKey(srSpriteName))
                {
                    // Load custom sprite (this internally handles texture caching)
                    Sprite customSprite = LoadCustomSprite(srSpriteName, sr.sprite);
                    if (customSprite != null)
                    {
                        sr.sprite = customSprite;
                        
                        if (Plugin.Config.DetailedTextureLog.Value)
                        {
                            Plugin.Log.LogInfo($"Replaced bath sprite: {srSpriteName} in new BathBG instance");
                        }
                        
                        replaced++;
                    }
                }
            }
        }
        
        if (replaced > 0 && Plugin.Config.DetailedTextureLog.Value)
        {
            Plugin.Log.LogInfo($"Replaced {replaced} bath sprite(s) in new BathBG instance");
        }
        
        return replaced;
    }

    /// <summary>
    /// Try to replace a texture with a custom version
    /// Returns true if replacement was successful
    /// </summary>
    private static bool TryReplaceTexture(string textureName, ref Texture value, bool logReplacement = true)
    {
        if (!Plugin.Config.EnableCustomTextures.Value)
            return false;

        if (!texturePathIndex.ContainsKey(textureName))
            return false;

        Texture2D customTexture = LoadCustomTexture(textureName);
        if (customTexture == null)
            return false;

        value = customTexture;

        // Only log if enabled, not already logged, and not spam
        if (logReplacement && Plugin.Config.DetailedTextureLog.Value && 
            !replacedTextures.Contains(textureName) && !ShouldSkipSpamLog(textureName))
        {
            Plugin.Log.LogInfo($"Replaced texture: {textureName}");
            replacedTextures.Add(textureName);
        }

        return true;
    }

    /// <summary>
    /// Try to replace a sprite with a custom version
    /// Returns true if replacement was successful
    /// </summary>
    private static bool TryReplaceSprite(string spriteName, ref Sprite value, Sprite original, bool logReplacement = true, string context = null)
    {
        if (!Plugin.Config.EnableCustomTextures.Value)
            return false;

        Sprite customSprite = LoadCustomSprite(spriteName, original);
        if (customSprite == null)
            return false;

        value = customSprite;

        // Only log if enabled, not already logged, and not spam
        if (logReplacement && Plugin.Config.DetailedTextureLog.Value && 
            !replacedTextures.Contains(spriteName) && !ShouldSkipSpamLog(spriteName))
        {
            string message = string.IsNullOrEmpty(context)
                ? $"Replaced sprite: {spriteName}"
                : $"Replaced sprite: {spriteName} ({context})";
            Plugin.Log.LogInfo(message);
            replacedTextures.Add(spriteName);
        }

        return true;
    }

    #endregion
    /// <summary>
    /// Intercept SpriteRenderer.sprite setter to replace with custom textures
    /// </summary>
    [HarmonyPatch(typeof(SpriteRenderer), nameof(SpriteRenderer.sprite), MethodType.Setter)]
    [HarmonyPrefix]
    public static void SpriteRenderer_set_sprite_Prefix(SpriteRenderer __instance, ref Sprite value)
    {
        if (value == null)
            return;

        string originalName = value.name;
        
        // Get object path for context
        string objectPath = GetGameObjectPath(__instance.gameObject);
        bool isBathBackground = objectPath.Contains("BathBG");
        bool isBgManager = objectPath.Contains("bgManagerHD");
        
        // If this is a bath sprite being assigned, check if it's a new BathBG instance
        if (isBathBackground && originalName.StartsWith("bath_"))
        {
            Plugin.Log.LogInfo($"Sprite setter called for bath sprite: {originalName} (path: {objectPath})");
            
            if (Plugin.Config.EnableCustomTextures.Value)
            {
                var bathBG = GameObject.Find("AppRoot/BathBG");
                if (bathBG != null)
                {
                    int currentInstanceID = bathBG.GetInstanceID();
                    if (currentInstanceID != lastBathBGInstanceID)
                    {
                        Plugin.Log.LogInfo($"New BathBG instance detected via sprite setter (ID: {currentInstanceID}, previous: {lastBathBGInstanceID})");
                        lastBathBGInstanceID = currentInstanceID;
                    }
                    
                    // Replace this bath sprite with custom texture
                    if (texturePathIndex.ContainsKey(originalName))
                    {
                        Sprite customSprite = LoadCustomSprite(originalName, value);
                        if (customSprite != null)
                        {
                            Plugin.Log.LogInfo($"Replaced bath sprite via setter: {originalName}");
                            value = customSprite;
                            return; // Early return - we've replaced the sprite
                        }
                    }
                    else
                    {
                        Plugin.Log.LogInfo($"Bath sprite {originalName} not found in texture index");
                    }
                }
                else
                {
                    Plugin.Log.LogInfo("BathBG not found when trying to replace sprite");
                }
            }
        }
        
        // Log replaceable textures if enabled (only once per texture)
        if (Plugin.Config.LogReplaceableTextures.Value && !loggedTextures.Contains(originalName))
        {
            loggedTextures.Add(originalName);
            if (isBathBackground || isBgManager)
            {
                Plugin.Log.LogInfo($"[Replaceable Sprite] {originalName} (from {objectPath})");
            }
            else
            {
                Plugin.Log.LogInfo($"[Replaceable Sprite] {originalName}");
            }
        }
        
        // Try to load custom sprite replacement
        if (Plugin.Config.EnableCustomTextures.Value)
        {
            Sprite customSprite = LoadCustomSprite(originalName, value);
            if (customSprite != null)
            {
                // Skip logging for sactx and character sprites to reduce spam
                bool shouldSkipReplacementLog = originalName.StartsWith("sactx");
                if (!shouldSkipReplacementLog && texturePathIndex.TryGetValue(originalName, out string replacementTexPath))
                {
                    shouldSkipReplacementLog = replacementTexPath.ToLower().Contains("characters");
                }
                
                if (!shouldSkipReplacementLog)
                {
                    if (isBathBackground || isBgManager)
                    {
                        Plugin.Log.LogInfo($"Replaced sprite: {originalName} (from {objectPath})");
                    }
                    else
                    {
                        Plugin.Log.LogInfo($"Replaced sprite: {originalName}");
                    }
                }
                value = customSprite;
            }
        }
    }

    /// <summary>
    /// Intercept SpriteAtlas.GetSprite to catch sprites loaded from atlases
    /// NOTE: Individual sprite names from atlases are NOT replaceable - only the atlas texture itself is
    /// </summary>
    [HarmonyPatch(typeof(SpriteAtlas), nameof(SpriteAtlas.GetSprite))]
    [HarmonyPostfix]
    public static void SpriteAtlas_GetSprite_Postfix(SpriteAtlas __instance, string name, ref Sprite __result)
    {
        if (__result == null)
            return;

        string spriteName = __result.name;
        
        // Remove (Clone) suffix if present
        if (spriteName.EndsWith("(Clone)"))
            spriteName = spriteName.Substring(0, spriteName.Length - 7);

        // DO NOT log individual sprite names from atlases - they're not replaceable files
        // Only the atlas texture itself (accessed via Sprite.texture) is replaceable
        
        // Try to replace with custom sprite (for non-atlas sprites only)
        if (Plugin.Config.EnableCustomTextures.Value)
        {
            // Only attempt replacement if this sprite has a direct PNG file (not from atlas)
            if (texturePathIndex.ContainsKey(spriteName))
            {
                Sprite customSprite = LoadCustomSprite(spriteName, __result);
                if (customSprite != null)
                {
                    __result = customSprite;
                    
                    // Skip logging for sactx and character sprites to reduce spam
                    bool shouldSkipAtlasLog = spriteName.StartsWith("sactx");
                    if (!shouldSkipAtlasLog && texturePathIndex.TryGetValue(spriteName, out string atlasTexPath))
                    {
                        shouldSkipAtlasLog = atlasTexPath.ToLower().Contains("characters");
                    }
                    
                    if (!shouldSkipAtlasLog)
                    {
                        Plugin.Log.LogInfo($"Replaced sprite: {spriteName}");
                    }
                }
            }
        }
        
        // Check if BathBG exists and if it's a NEW instance (for in-game switcher)
        // Check on every atlas load - instance ID prevents duplicate replacements
        if (Plugin.Config.EnableCustomTextures.Value)
        {
            var bathBG = GameObject.Find("AppRoot/BathBG");
            if (bathBG != null)
            {
                int currentInstanceID = bathBG.GetInstanceID();
                
                // Only replace if this is a NEW BathBG instance (different from last one)
                if (currentInstanceID != lastBathBGInstanceID)
                {
                    Plugin.Log.LogInfo($"New BathBG instance detected (ID: {currentInstanceID}, previous: {lastBathBGInstanceID})");
                    lastBathBGInstanceID = currentInstanceID;
                    
                    // Get all SpriteRenderers in BathBG
                    var bathRenderers = bathBG.GetComponentsInChildren<SpriteRenderer>(true);
                    int replaced = 0;
                    
                    foreach (var sr in bathRenderers)
                    {
                        if (sr.sprite != null)
                        {
                            string srSpriteName = sr.sprite.name;
                            
                            // Check if this is a bath sprite and we have a custom texture
                            if (srSpriteName.StartsWith("bath_") && texturePathIndex.ContainsKey(srSpriteName))
                            {
                                // Load custom texture and create sprite with original properties
                                Texture2D customTexture = LoadCustomTexture(srSpriteName);
                                if (customTexture != null)
                                {
                                    Sprite originalSprite = sr.sprite;
                                    Sprite customSprite = LoadCustomSprite(srSpriteName, originalSprite);
                                    if (customSprite != null)
                                    {
                                        sr.sprite = customSprite;
                                        Plugin.Log.LogInfo($"Replaced bath sprite: {srSpriteName} in new BathBG instance");
                                        replaced++;
                                    }
                                }
                            }
                        }
                    }
                    
                    if (replaced > 0)
                    {
                        Plugin.Log.LogInfo($"Bath background replacement: {replaced} sprite(s) in new instance");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Patch HDBath.LoadInstance to detect when bath backgrounds are loaded
    /// This catches the in-game bath background switcher
    /// </summary>
    [HarmonyPatch(typeof(HDBath), nameof(HDBath.LoadInstance))]
    [HarmonyPostfix]
    public static void HDBath_LoadInstance_Postfix(string bathPath)
    {
        if (!Plugin.Config.EnableCustomTextures.Value)
            return;

        Plugin.Log.LogInfo($"HDBath.LoadInstance called with bathPath: {bathPath}");
        
        // Reset the instance ID to force scan
        lastBathBGInstanceID = -1;
    }

    /// <summary>
    /// Patch HDBath.Start to replace bath sprites after HDBath component is fully initialized
    /// This is the key patch for in-game bath switching - Start() is called AFTER the GameObject is instantiated
    /// </summary>
    [HarmonyPatch(typeof(HDBath), "Start")]
    [HarmonyPostfix]
    public static void HDBath_Start_Postfix(HDBath __instance)
    {
        if (!Plugin.Config.EnableCustomTextures.Value)
            return;

        Plugin.Log.LogInfo($"HDBath.Start called - scanning for bath sprites to replace");
        
        // Do an immediate scan
        ScanAndReplaceBathSprites(__instance);
    }
    
    /// <summary>
    /// Scans for bath sprites on the HDBath instance and replaces with custom textures
    /// </summary>
    public static void ScanAndReplaceBathSprites(HDBath instance)
    {
        if (instance == null)
        {
            Plugin.Log.LogWarning("ScanAndReplaceBathSprites: instance is null");
            return;
        }
        
        Plugin.Log.LogInfo($"ScanAndReplaceBathSprites: Checking HDBath instance at {GetGameObjectPath(instance.gameObject)}");
        
        // The HDBath component is attached to the bath_X GameObject
        // Get the SpriteRenderer on the same GameObject
        var sr = instance.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            string spriteName = sr.sprite.name;
            Plugin.Log.LogInfo($"Found bath SpriteRenderer with sprite: {spriteName}");
            
            if (spriteName.StartsWith("bath_") && texturePathIndex.ContainsKey(spriteName))
            {
                Sprite customSprite = LoadCustomSprite(spriteName, sr.sprite);
                if (customSprite != null)
                {
                    sr.sprite = customSprite;
                    Plugin.Log.LogInfo($"✓ Replaced bath sprite: {spriteName}");
                }
            }
            else if (spriteName.StartsWith("bath_"))
            {
                Plugin.Log.LogInfo($"No custom texture found for: {spriteName}");
            }
        }
        else
        {
            Plugin.Log.LogInfo($"No SpriteRenderer on HDBath instance, scanning from parent GameObject...");
            
            // The HDBath instance tells us where BathBG is - use its parent!
            var bathBG = instance.gameObject; // This IS the BathBG GameObject
            if (bathBG != null)
            {
                Plugin.Log.LogInfo($"Scanning BathBG at: {GetGameObjectPath(bathBG)}");
                var bgTransform = bathBG.transform.Find("bg");
                if (bgTransform != null)
                {
                    Plugin.Log.LogInfo($"  Found 'bg' child, scanning for bath sprites...");
                    int found = 0;
                    
                    // Use GetChild() for Il2Cpp compatibility instead of foreach
                    for (int i = 0; i < bgTransform.childCount; i++)
                    {
                        Transform child = bgTransform.GetChild(i);
                        if (child.name.StartsWith("bath_"))
                        {
                            found++;
                            var childSr = child.GetComponent<SpriteRenderer>();
                            if (childSr != null && childSr.sprite != null)
                            {
                                string childSpriteName = childSr.sprite.name;
                                Plugin.Log.LogInfo($"  Found bath sprite: {childSpriteName} (in texture index: {texturePathIndex.ContainsKey(childSpriteName)})");
                                if (texturePathIndex.ContainsKey(childSpriteName))
                                {
                                    Plugin.Log.LogInfo($"  Attempting to load custom sprite for: {childSpriteName}");
                                    Sprite customSprite = LoadCustomSprite(childSpriteName, childSr.sprite);
                                    if (customSprite != null)
                                    {
                                        childSr.sprite = customSprite;
                                        Plugin.Log.LogInfo($"✓ Replaced bath sprite: {childSpriteName}");
                                    }
                                    else
                                    {
                                        Plugin.Log.LogWarning($"LoadCustomSprite returned null for: {childSpriteName}");
                                    }
                                }
                            }
                        }
                    }
                    
                    if (found == 0)
                    {
                        Plugin.Log.LogWarning("BathBG/bg exists but no bath_ GameObjects found as children");
                    }
                }
                else
                {
                    Plugin.Log.LogWarning("BathBG found but 'bg' child not found");
                }
            }
        }
    }


    /// <summary>
    /// Patch HDBath.OnDestroy to detect when bath is destroyed
    /// </summary>
    [HarmonyPatch(typeof(HDBath), "OnDestroy")]
    [HarmonyPrefix]
    public static void HDBath_OnDestroy_Prefix()
    {
        if (!Plugin.Config.EnableCustomTextures.Value)
            return;

        Plugin.Log.LogInfo("HDBath.OnDestroy called - old bath being destroyed");
        lastBathBGInstanceID = -1;

        // Check on every atlas load - instance ID prevents duplicate replacements
        if (Plugin.Config.EnableCustomTextures.Value)
        {
            var bathBG = GameObject.Find("AppRoot/BathBG");
            if (bathBG != null)
            {
                int currentInstanceID = bathBG.GetInstanceID();
                
                // Only replace if this is a NEW BathBG instance (different from last one)
                if (currentInstanceID != lastBathBGInstanceID)
                {
                    Plugin.Log.LogInfo($"New BathBG instance detected (ID: {currentInstanceID}, previous: {lastBathBGInstanceID})");
                    lastBathBGInstanceID = currentInstanceID;
                    
                    // Get all SpriteRenderers in BathBG
                    var bathRenderers = bathBG.GetComponentsInChildren<SpriteRenderer>(true);
                    int replaced = 0;
                    
                    foreach (var sr in bathRenderers)
                    {
                        if (sr.sprite != null)
                        {
                            string srSpriteName = sr.sprite.name;
                            
                            // Check if this is a bath sprite and we have a custom texture
                            if (srSpriteName.StartsWith("bath_") && texturePathIndex.ContainsKey(srSpriteName))
                            {
                                // Load custom sprite (this internally handles texture caching)
                                Sprite customSprite = LoadCustomSprite(srSpriteName, sr.sprite);
                                if (customSprite != null)
                                {
                                    sr.sprite = customSprite;
                                    Plugin.Log.LogInfo($"Replaced bath sprite: {srSpriteName} in new BathBG instance");
                                    replaced++;
                                }
                            }
                        }
                    }
                    
                    if (replaced > 0)
                    {
                        Plugin.Log.LogInfo($"Bath background replacement: {replaced} sprite(s) in new instance");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Intercept GameObject.SetActive to catch sprites when objects are activated
    /// This catches sprites in objects that are instantiated/activated after scene load
    /// Specifically handles bath backgrounds and bgManagerHD objects
    /// </summary>
    [HarmonyPatch(typeof(GameObject), nameof(GameObject.SetActive))]
    [HarmonyPostfix]
    public static void GameObject_SetActive_Postfix(GameObject __instance, bool value)
    {
        // Only scan when activating (not deactivating)
        if (!value || !Plugin.Config.EnableCustomTextures.Value && !Plugin.Config.LogReplaceableTextures.Value)
            return;

        // Get the full path of the activated object
        string objectPath = GetGameObjectPath(__instance);
        
        // Check if this is a bath background or bgManagerHD object
        bool isBathBackground = objectPath.Contains("BathBG");
        bool isBgManager = objectPath.Contains("bgManagerHD");

        // Special handling for BathBG activation
        if (__instance.name == "BathBG" && isBathBackground)
        {
            Plugin.Log.LogInfo($"BathBG GameObject activated: {objectPath}");
            
            int currentInstanceID = __instance.GetInstanceID();
            if (currentInstanceID != lastBathBGInstanceID)
            {
                Plugin.Log.LogInfo($"New BathBG instance detected via SetActive (ID: {currentInstanceID}, previous: {lastBathBGInstanceID})");
                lastBathBGInstanceID = currentInstanceID;
            }
        }
        // Special handling for BathBG activation
        if (__instance.name == "BathBG" && isBathBackground)
        {
            Plugin.Log.LogInfo($"BathBG GameObject activated: {objectPath}");
            
            int currentInstanceID = __instance.GetInstanceID();
            if (currentInstanceID != lastBathBGInstanceID)
            {
                Plugin.Log.LogInfo($"New BathBG instance detected via SetActive (ID: {currentInstanceID}, previous: {lastBathBGInstanceID})");
                lastBathBGInstanceID = currentInstanceID;
            }
        }

        // Scan all SpriteRenderers in this GameObject and its children
        var spriteRenderers = __instance.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in spriteRenderers)
        {
            if (sr.sprite != null)
            {
                string spriteName = sr.sprite.name;

                // Log if not already logged
                if (Plugin.Config.LogReplaceableTextures.Value && !loggedTextures.Contains(spriteName))
                {
                    loggedTextures.Add(spriteName);
                    if (isBathBackground || isBgManager)
                    {
                        Plugin.Log.LogInfo($"[Replaceable Sprite - Activated] {spriteName} (from {objectPath})");
                    }
                    else
                    {
                        Plugin.Log.LogInfo($"[Replaceable Sprite - Activated] {spriteName}");
                    }
                }

                // Try to replace
                if (Plugin.Config.EnableCustomTextures.Value)
                {
                    Sprite customSprite = LoadCustomSprite(spriteName, sr.sprite);
                    if (customSprite != null)
                    {
                        sr.sprite = customSprite;
                        
                        // Skip logging for sactx and character sprites to reduce spam
                        bool shouldSkipActivationLog = spriteName.StartsWith("sactx");
                        if (!shouldSkipActivationLog && texturePathIndex.TryGetValue(spriteName, out string activationTexPath))
                        {
                            shouldSkipActivationLog = activationTexPath.ToLower().Contains("characters");
                        }
                        
                        if (!shouldSkipActivationLog)
                        {
                            if (isBathBackground || isBgManager)
                            {
                                Plugin.Log.LogInfo($"Replaced sprite on activation: {spriteName} (from {objectPath})");
                            }
                            else
                            {
                                Plugin.Log.LogInfo($"Replaced sprite on activation: {spriteName}");
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Intercept Sprite.texture getter to replace atlas textures
    /// This is THE KEY PATCH for replacing atlas textures (including summon effects)
    /// </summary>
    [HarmonyPatch(typeof(Sprite), nameof(Sprite.texture), MethodType.Getter)]
    [HarmonyPostfix]
    public static void Sprite_get_texture_Postfix(Sprite __instance, ref Texture2D __result)
    {
        if (__result == null)
            return;

        string textureName = __result.name;
        LogReplaceableTexture(textureName, "Texture - Atlas");
        
        // Try to replace with custom texture
        Texture tempTexture = __result;
        if (TryReplaceTexture(textureName, ref tempTexture, logReplacement: true))
        {
            __result = (Texture2D)tempTexture;
        }
    }

    /// <summary>
    /// Intercept Material.SetTexture to catch textures set via name/ID (common for custom shaders/effects)
    /// </summary>
    [HarmonyPatch(typeof(Material), nameof(Material.SetTexture), new System.Type[] { typeof(string), typeof(Texture) })]
    [HarmonyPrefix]
    public static void Material_SetTexture_Prefix(Material __instance, string name, ref Texture value)
    {
        if (value == null || string.IsNullOrEmpty(name))
            return;

        string textureName = value.name;
        LogReplaceableTexture(textureName, "Texture - SetTexture", $"property: {name}");
        TryReplaceTexture(textureName, ref value, logReplacement: false); // Don't log - too spammy
    }
    
    [HarmonyPatch(typeof(Material), nameof(Material.SetTexture), new System.Type[] { typeof(int), typeof(Texture) })]
    [HarmonyPrefix]
    public static void Material_SetTexture_ID_Prefix(Material __instance, int nameID, ref Texture value)
    {
        if (value == null)
            return;

        string textureName = value.name;
        LogReplaceableTexture(textureName, "Texture - SetTexture", $"ID: {nameID}");
        TryReplaceTexture(textureName, ref value, logReplacement: false); // Don't log - too spammy
    }

    /// <summary>
    /// Intercept Material.mainTexture setter to replace textures
    /// </summary>
    [HarmonyPatch(typeof(Material), nameof(Material.mainTexture), MethodType.Setter)]
    [HarmonyPrefix]
    public static void Material_set_mainTexture_Prefix(Material __instance, ref Texture value)
    {
        if (value == null)
            return;

        string originalName = value.name;
        
        // Log replaceable textures if enabled (only once per texture)
        if (Plugin.Config.LogReplaceableTextures.Value && !loggedTextures.Contains(originalName))
        {
            loggedTextures.Add(originalName);
            Plugin.Log.LogInfo($"[Replaceable Texture] {originalName}");
        }
        
        // Try to load custom texture replacement
        Texture2D customTexture = LoadCustomTexture(originalName);
        if (customTexture != null)
        {
            // Skip logging for sactx and character textures to reduce spam
            bool shouldSkipMaterialLog = originalName.StartsWith("sactx");
            if (!shouldSkipMaterialLog && texturePathIndex.TryGetValue(originalName, out string materialTexPath))
            {
                shouldSkipMaterialLog = materialTexPath.ToLower().Contains("characters");
            }
            
            // Get original texture dimensions
            Texture2D originalTexture = value as Texture2D;
            if (originalTexture != null)
            {
                // Calculate scale factor
                float scaleX = (float)originalTexture.width / customTexture.width;
                float scaleY = (float)originalTexture.height / customTexture.height;
                
                // Adjust material's texture scale to compensate for size difference
                // This makes the custom texture display at the same visual size as the original
                __instance.mainTextureScale = new Vector2(scaleX, scaleY);
                
                if (!shouldSkipMaterialLog)
                {
                    Plugin.Log.LogInfo($"Replaced texture: {originalName} (scale: {scaleX:F2}x, {scaleY:F2}y)");
                }
            }
            else
            {
                if (!shouldSkipMaterialLog)
                {
                    Plugin.Log.LogInfo($"Replaced texture: {originalName}");
                }
            }
            value = customTexture;
        }
    }

    /// <summary>
    /// Intercept Image.sprite setter to replace UI sprites
    /// </summary>
    [HarmonyPatch(typeof(Image), nameof(Image.sprite), MethodType.Setter)]
    [HarmonyPrefix]
    public static void Image_set_sprite_Prefix(Image __instance, ref Sprite value)
    {
        if (value == null)
            return;

        string originalName = value.name;
        
        // Log with optional path context
        if (Plugin.Config.LogTexturePaths.Value)
        {
            string gameObjectPath = GetGameObjectPath(__instance.gameObject);
            LogReplaceableTexture(originalName, "UI Sprite", $"Path: {gameObjectPath}");
        }
        else
        {
            LogReplaceableTexture(originalName, "UI Sprite");
        }
        
        TryReplaceSprite(originalName, ref value, value, logReplacement: true, context: "UI");
    }


    /// <summary>
    /// Intercept Image.overrideSprite setter (this is what actually sets the sprite)
    /// </summary>
    [HarmonyPatch(typeof(Image), nameof(Image.overrideSprite), MethodType.Setter)]
    [HarmonyPrefix]
    public static void Image_set_overrideSprite_Prefix(Image __instance, ref Sprite value)
    {
        if (value == null)
            return;

        string originalName = value.name;
        
        // Log with optional path context
        if (Plugin.Config.LogTexturePaths.Value)
        {
            string gameObjectPath = GetGameObjectPath(__instance.gameObject);
            LogReplaceableTexture(originalName, "UI Override Sprite", $"Path: {gameObjectPath}");
        }
        else
        {
            LogReplaceableTexture(originalName, "UI Override Sprite");
        }
        
        TryReplaceSprite(originalName, ref value, value, logReplacement: true, context: "UI override");
    }

    /// <summary>
    /// Intercept RawImage.texture setter to replace UI textures
    /// </summary>
    [HarmonyPatch(typeof(RawImage), nameof(RawImage.texture), MethodType.Setter)]
    [HarmonyPrefix]
    public static void RawImage_set_texture_Prefix(RawImage __instance, ref Texture value)
    {
        if (value == null)
            return;

        string originalName = value.name;
        
        // Log replaceable textures if enabled (only once per texture)
        if (Plugin.Config.LogReplaceableTextures.Value && !loggedTextures.Contains(originalName))
        {
            loggedTextures.Add(originalName);
            Plugin.Log.LogInfo($"[Replaceable UI Texture] {originalName}");
        }
        
        // Try to load custom texture replacement
        Texture2D customTexture = LoadCustomTexture(originalName);
        if (customTexture != null)
        {
            Plugin.Log.LogInfo($"Replaced UI texture: {originalName}");
            value = customTexture;
        }
    }

    /// <summary>
    /// Intercept Graphic.mainTexture getter to log UI textures (base class for Image, RawImage, Text, etc.)
    /// This catches textures on GameObjects with RectTransform and CanvasRenderer
    /// </summary>
    [HarmonyPatch(typeof(Graphic), nameof(Graphic.mainTexture), MethodType.Getter)]
    [HarmonyPostfix]
    public static void Graphic_get_mainTexture_Postfix(Graphic __instance, Texture __result)
    {
        if (__result == null)
            return;

        string originalName = __result.name;
        
        // Log replaceable textures if enabled (only once per texture)
        if (Plugin.Config.LogReplaceableTextures.Value && !loggedTextures.Contains(originalName))
        {
            loggedTextures.Add(originalName);
            Plugin.Log.LogInfo($"[Replaceable Graphic Texture] {originalName} (Component: {__instance.GetType().Name})");
        }
    }

    /// <summary>
    /// Intercept Image.OnEnable to catch sprites that were set before patches loaded
    /// This runs when Image components become active, allowing us to detect and replace
    /// sprites that were assigned during Awake/Start or scene initialization
    /// </summary>
    [HarmonyPatch(typeof(Image), "OnEnable")]
    [HarmonyPostfix]
    public static void Image_OnEnable_Postfix(Image __instance)
    {
        if (!Plugin.Config.EnableCustomTextures.Value)
            return;

        // Check both sprite and overrideSprite
        Sprite activeSprite = __instance.overrideSprite ?? __instance.sprite;
        
        if (activeSprite != null)
        {
            string spriteName = activeSprite.name;
            
            // Log if enabled and not already logged
            if (Plugin.Config.LogReplaceableTextures.Value && !loggedTextures.Contains(spriteName))
            {
                loggedTextures.Add(spriteName);
                if (Plugin.Config.LogTexturePaths.Value)
                {
                    string gameObjectPath = GetGameObjectPath(__instance.gameObject);
                    Plugin.Log.LogInfo($"[Replaceable UI Sprite - OnEnable] {spriteName}\n  Path: {gameObjectPath}");
                }
                else
                {
                    Plugin.Log.LogInfo($"[Replaceable UI Sprite - OnEnable] {spriteName}");
                }
            }
            
            // Try to load custom sprite replacement
            Sprite customSprite = LoadCustomSprite(spriteName, activeSprite);
            if (customSprite != null)
            {
                __instance.sprite = customSprite;
                __instance.overrideSprite = customSprite;
                
                // Only log if enabled and not already logged
                if (Plugin.Config.DetailedTextureLog.Value && !replacedTextures.Contains(spriteName))
                {
                    Plugin.Log.LogInfo($"Replaced UI sprite on enable: {spriteName}");
                    replacedTextures.Add(spriteName);
                }
            }
        }
    }

    private static int bathBackgroundScanAttempts = 0; // Track scan attempts
    private const int MAX_BATH_SCAN_ATTEMPTS = 10; // Allow multiple attempts - increased for timing issues

    /// <summary>
    /// Actively scan for and replace bath background sprites that are already active
    /// This is called when bath-related sprites are detected to catch pre-loaded backgrounds
    /// </summary>
    private static void ScanAndReplaceBathBackgrounds()
    {
        // Allow multiple scan attempts since bath backgrounds may load after hp_furo sprites
        if (bathBackgroundScanAttempts >= MAX_BATH_SCAN_ATTEMPTS)
            return;

        bathBackgroundScanAttempts++;
        Plugin.Log.LogInfo($"Triggered bath background scan (attempt {bathBackgroundScanAttempts}/{MAX_BATH_SCAN_ATTEMPTS})...");

        // Try to find bath background container
        string[] bathPaths = new string[] 
        { 
            "CAMERA",
            "CAMERA/bg",
            "AppRoot/CAMERA",
            "AppRoot/CAMERA/bg",
            "AppRoot/BathBG",
            "AppRoot/BathBG/bg",
            "BathBG",
            "BathBG/bg",
            "CAMERA/BathBG",
            "AppRoot/CAMERA/BathBG"
        };

        foreach (var bathPath in bathPaths)
        {
            var bathBG = GameObject.Find(bathPath);
            if (bathBG != null)
            {
                Plugin.Log.LogInfo($"  Found bath background at: {bathPath}");
                var bathSprites = bathBG.GetComponentsInChildren<SpriteRenderer>(true);
                Plugin.Log.LogInfo($"  Scanning {bathSprites.Length} SpriteRenderer(s)...");

                int replaced = 0;
                int nullSprites = 0;
                foreach (var sr in bathSprites)
                {
                    if (sr.sprite != null)
                    {
                        string spriteName = sr.sprite.name;

                        // Try to replace
                        Sprite customSprite = LoadCustomSprite(spriteName, sr.sprite);
                        if (customSprite != null)
                        {
                            sr.sprite = customSprite;
                            Plugin.Log.LogInfo($"  Replaced bath sprite: {spriteName}");
                            replaced++;
                        }
                    }
                    else
                    {
                        nullSprites++;
                    }
                }

                if (nullSprites > 0)
                {
                    Plugin.Log.LogInfo($"  Found {nullSprites} SpriteRenderer(s) with null sprites (not ready yet)");
                }

                if (replaced > 0)
                {
                    Plugin.Log.LogInfo($"Bath background scan complete: replaced {replaced} sprite(s)");
                    bathBackgroundScanAttempts = MAX_BATH_SCAN_ATTEMPTS; // Stop further attempts
                }
                else
                {
                    Plugin.Log.LogInfo("Bath background scan complete: no custom textures found");
                }
                return; // Found and processed
            }
        }

        Plugin.Log.LogInfo("Bath background scan: no bath background found (will retry on next hp_furo sprite)");
    }

    /// <summary>
    /// Get full hierarchy path of a GameObject for debugging
    /// </summary>
    private static string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }

    /// <summary>
    /// Load a custom sprite from PNG file
    /// </summary>
    private static Sprite LoadCustomSprite(string spriteName, Sprite originalSprite)
    {
        bool isBathSprite = spriteName.StartsWith("bath_");
        
        // Check cache first for performance
        if (customSpriteCache.TryGetValue(spriteName, out Sprite cachedSprite))
        {
            if (isBathSprite) Plugin.Log.LogInfo($"    [LoadCustomSprite] Cache hit for {spriteName}");
            
            // Validate that the cached sprite is still valid (not destroyed by Unity)
            if (cachedSprite != null && cachedSprite && cachedSprite.texture != null)
            {
                return cachedSprite;
            }
            else
            {
                // Sprite was destroyed (e.g., map change), remove from cache
                customSpriteCache.Remove(spriteName);
            }
        }

        if (isBathSprite) Plugin.Log.LogInfo($"    [LoadCustomSprite] No cache, calling LoadCustomTexture for {spriteName}");
        
        // Try to load texture from file (will use texture cache internally)
        Texture2D texture = LoadCustomTexture(spriteName);
        if (texture == null)
        {
            if (isBathSprite) Plugin.Log.LogWarning($"    [LoadCustomSprite] LoadCustomTexture returned null for {spriteName}");
            return null;
        }

        // Preserve original sprite properties if available
        Vector2 pivot = originalSprite != null ? originalSprite.pivot / originalSprite.rect.size : new Vector2(0.5f, 0.5f);
        float pixelsPerUnit = originalSprite != null ? originalSprite.pixelsPerUnit : 100f;
        Vector4 border = originalSprite != null ? originalSprite.border : Vector4.zero;

        // Auto-scale pixelsPerUnit to maintain original display size (like Special K)
        // This allows higher resolution textures to display at the same size as originals
        if (originalSprite != null)
        {
            float originalWidth = originalSprite.rect.width;
            float originalHeight = originalSprite.rect.height;
            float customWidth = texture.width;
            float customHeight = texture.height;

            // Calculate scale ratio (use width as primary dimension)
            float scaleRatio = customWidth / originalWidth;
            
            // Adjust pixelsPerUnit proportionally
            // Higher resolution = higher pixelsPerUnit to maintain same display size
            pixelsPerUnit = originalSprite.pixelsPerUnit * scaleRatio;
        }

        // Create sprite from texture with adjusted properties
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            pivot,
            pixelsPerUnit,
            0, // extrude
            SpriteMeshType.FullRect,
            border
        );

        // Prevent Unity from destroying the sprite
        UnityEngine.Object.DontDestroyOnLoad(sprite);
        UnityEngine.Object.DontDestroyOnLoad(texture);

        // Cache the sprite for reuse
        customSpriteCache[spriteName] = sprite;
        
        return sprite;
    }



    /// <summary>
    /// Load a custom texture from image file (supports PNG, JPG, TGA)
    /// </summary>
    private static Texture2D LoadCustomTexture(string textureName)
    {
        // Check cache first for performance
        if (customTextureCache.TryGetValue(textureName, out Texture2D cachedTexture))
        {
            // Only validate cache for dynamic textures (characters, portraits) that get destroyed
            // Static textures (backgrounds) can be trusted in cache
            if (texturePathIndex.TryGetValue(textureName, out string cachedPath))
            {
                string lowerPath = cachedPath.ToLower();
                bool isDynamic = lowerPath.Contains("characters") || 
                                lowerPath.Contains("portraits") || 
                                lowerPath.Contains("character") || 
                                lowerPath.Contains("portrait");
                
                if (isDynamic)
                {
                    // Validate that the cached texture is still valid (not destroyed by Unity)
                    if (cachedTexture != null && cachedTexture)
                        return cachedTexture;
                    else
                        customTextureCache.Remove(textureName); // Clean up invalid cache entry
                }
                else
                {
                    // Static texture - but still validate it's not null/destroyed
                    if (cachedTexture != null && cachedTexture)
                        return cachedTexture;
                    else
                        customTextureCache.Remove(textureName); // Clean up invalid cache entry
                }
            }
            else
            {
                // Fallback: validate before trusting cache
                if (cachedTexture != null && cachedTexture)
                    return cachedTexture;
                else
                    customTextureCache.Remove(textureName);
            }
        }
        
        // Look up full path from index (supports subfolders)
        if (!texturePathIndex.TryGetValue(textureName, out string filePath))
            return null;

        try
        {
            // Load image file
            byte[] fileData = File.ReadAllBytes(filePath);
            
            // Create texture with mipmaps enabled for better quality
            // IMPORTANT: Must be readable for IL2CPP
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, true);
            
            // Use ImageConversion static class (IL2CPP compatible)
            // Supports PNG, JPG, TGA formats automatically
            if (!UnityEngine.ImageConversion.LoadImage(texture, fileData))
            {
                Plugin.Log.LogError($"Failed to load image: {filePath}");
                UnityEngine.Object.Destroy(texture);
                return null;
            }

            // Apply texture settings for quality
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.anisoLevel = 4; // Better quality at angles
            
            // CRITICAL: Apply with mipmaps enabled and keep readable
            texture.Apply(true, false); // updateMipmaps=true, makeNoLongerReadable=false
            
            // Prevent Unity from unloading the texture
            UnityEngine.Object.DontDestroyOnLoad(texture);

            // Cache the texture for reuse
            customTextureCache[textureName] = texture;
            
            // Skip logging for sactx and character textures to reduce spam
            bool shouldSkipLoadLog = textureName.StartsWith("sactx") || filePath.ToLower().Contains("characters");
            
            if (!shouldSkipLoadLog && Plugin.Config.DetailedTextureLog.Value)
            {
                Plugin.Log.LogInfo($"Loaded and cached custom texture: {textureName} ({texture.width}x{texture.height}) from {Path.GetExtension(filePath)}");
            }
            
            return texture;
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"Error loading texture {textureName}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Build index of all texture files (supports subfolders)
    /// </summary>
    private static void BuildTextureIndex()
    {
        texturePathIndex.Clear();
        
        if (!Directory.Exists(customTexturesPath))
            return;

        // Supported image extensions
        string[] extensions = { "*.png", "*.jpg", "*.jpeg", "*.tga" };
        
        // PRIORITY SYSTEM:
        // 1. Scan all folders EXCEPT 00-Mods first (base textures)
        // 2. Scan 00-Mods folder LAST (override textures - highest priority)
        
        string modsFolder = Path.Combine(customTexturesPath, "00-Mods");
        bool hasModsFolder = Directory.Exists(modsFolder);
        
        // Phase 1: Scan base textures (all folders except 00-Mods)
        foreach (string extension in extensions)
        {
            string[] files = Directory.GetFiles(customTexturesPath, extension, SearchOption.AllDirectories);
            
            foreach (string filePath in files)
            {
                // Skip files in 00-Mods folder for now (will process later)
                if (hasModsFolder && filePath.StartsWith(modsFolder, StringComparison.OrdinalIgnoreCase))
                    continue;
                
                string textureName = Path.GetFileNameWithoutExtension(filePath);
                
                // Check for duplicate texture names
                if (texturePathIndex.ContainsKey(textureName))
                {
                    string existingPath = texturePathIndex[textureName];
                    string existingRelative = existingPath.Replace(customTexturesPath, "").TrimStart('\\', '/');
                    string newRelative = filePath.Replace(customTexturesPath, "").TrimStart('\\', '/');
                    
                    if (Plugin.Config.DetailedTextureLog.Value)
                    {
                        Plugin.Log.LogWarning($"Duplicate texture name '{textureName}':");
                        Plugin.Log.LogWarning($"  Using: {existingRelative}");
                        Plugin.Log.LogWarning($"  Ignoring: {newRelative}");
                    }
                }
                else
                {
                    texturePathIndex[textureName] = filePath;
                }
            }
        }
        
        // Phase 2: Scan 00-Mods folder LAST (overrides base textures)
        if (hasModsFolder)
        {
            int overrideCount = 0;
            
            foreach (string extension in extensions)
            {
                string[] modFiles = Directory.GetFiles(modsFolder, extension, SearchOption.AllDirectories);
                
                foreach (string filePath in modFiles)
                {
                    string textureName = Path.GetFileNameWithoutExtension(filePath);
                    string modRelative = filePath.Replace(customTexturesPath, "").TrimStart('\\', '/');
                    
                    // Check if this overrides an existing texture
                    if (texturePathIndex.ContainsKey(textureName))
                    {
                        string existingPath = texturePathIndex[textureName];
                        string existingRelative = existingPath.Replace(customTexturesPath, "").TrimStart('\\', '/');
                        
                        // OVERRIDE: Replace with mod version
                        texturePathIndex[textureName] = filePath;
                        overrideCount++;
                        
                        if (Plugin.Config.DetailedTextureLog.Value)
                        {
                            Plugin.Log.LogInfo($"[Override] '{textureName}':");
                            Plugin.Log.LogInfo($"  Base: {existingRelative}");
                            Plugin.Log.LogInfo($"  Mod:  {modRelative}");
                        }
                    }
                    else
                    {
                        // New texture from mods folder
                        texturePathIndex[textureName] = filePath;
                    }
                }
            }
            
            if (overrideCount > 0)
            {
                Plugin.Log.LogInfo($"Applied {overrideCount} texture override(s) from 00-Mods folder");
            }
        }
    }

    /// <summary>
    /// Preload bath_1 through bath_5 sprites for instant replacement
    /// This bypasses all timing issues by having sprites ready before they're needed
    /// </summary>
    private static void PreloadBathSprites()
    {
        int preloaded = 0;

        for (int i = 1; i <= 5; i++)
        {
            string bathName = $"bath_{i}";
            
            // Check if custom texture exists
            if (texturePathIndex.ContainsKey(bathName))
            {
                // Load texture
                Texture2D texture = LoadCustomTexture(bathName);
                if (texture != null)
                {
                    // Create sprite with default properties (will be adjusted when actually used)
                    Sprite sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f), // Center pivot
                        1f, // Default ppu
                        0,
                        SpriteMeshType.FullRect
                    );

                    UnityEngine.Object.DontDestroyOnLoad(sprite);
                    UnityEngine.Object.DontDestroyOnLoad(texture);

                    preloadedBathSprites[bathName] = sprite;
                    
                    // Only log details if verbose logging enabled
                    if (Plugin.Config.DetailedTextureLog.Value)
                    {
                        Plugin.Log.LogInfo($"  Preloaded: {bathName} ({texture.width}x{texture.height})");
                    }
                    
                    preloaded++;
                }
            }
        }

        if (preloaded > 0)
        {
            Plugin.Log.LogInfo($"Preloaded {preloaded} bath sprite(s) for instant replacement");
        }
    }

    public static void Initialize()
    {
        // Set custom textures folder path
        // BepInEx/plugins/PKCore/Textures/
        customTexturesPath = Path.Combine(
            BepInEx.Paths.PluginPath,
            "PKCore",
            "Textures"
        );

        // Create directory if it doesn't exist
        if (!Directory.Exists(customTexturesPath))
        {
            Directory.CreateDirectory(customTexturesPath);
            Plugin.Log.LogInfo($"Created custom textures directory: {customTexturesPath}");
        }
        else
        {
            Plugin.Log.LogInfo($"Custom textures directory: {customTexturesPath}");
        }

        // Build texture index (supports subfolders)
        BuildTextureIndex();
        
        // Preload bath sprites for instant replacement
        PreloadBathSprites();
        
        // Log indexed textures
        if (texturePathIndex.Count > 0)
        {
            Plugin.Log.LogInfo($"Indexed {texturePathIndex.Count} custom texture(s) ready to use");
            
            // Only show detailed list if verbose logging is enabled
            if (Plugin.Config.DetailedTextureLog.Value)
            {
                // Group by directory for cleaner output
                var groupedByDir = texturePathIndex
                    .GroupBy(kvp => Path.GetDirectoryName(kvp.Value))
                    .OrderBy(g => g.Key);
                
                foreach (var dirGroup in groupedByDir)
                {
                    string relativePath = dirGroup.Key.Replace(customTexturesPath, "").TrimStart('\\', '/');
                    string displayPath = string.IsNullOrEmpty(relativePath) ? "[Root]" : relativePath;
                    
                    Plugin.Log.LogInfo($"  {displayPath}/");
                    foreach (var texture in dirGroup.OrderBy(kvp => kvp.Key))
                    {
                        Plugin.Log.LogInfo($"    - {texture.Key}{Path.GetExtension(texture.Value)}");
                    }
                }
            }
        }
        else
        {
            Plugin.Log.LogInfo("No custom textures found. Place PNG/JPG/TGA files in the Textures folder.");
        }

        // Register scene loaded callback to scan for sprites (IL2CPP compatible)
        SceneManager.sceneLoaded += (System.Action<Scene, LoadSceneMode>)OnSceneLoaded;
    }

    /// <summary>
    /// Preloads ALL bath sprites (bath_1 through bath_5) and directly injects them into the scene
    /// This ensures custom sprites are used IMMEDIATELY when switching baths in-game
    /// </summary>
    private static void PreloadAndInjectBathSprites()
    {
        if (!Plugin.Config.EnableCustomTextures.Value)
            return;

        Plugin.Log.LogInfo("[Bath Preload] Starting bath sprite preload and injection...");

        // Find the BathBG container
        var bathBG = GameObject.Find("AppRoot/BathBG");
        if (bathBG == null)
        {
            Plugin.Log.LogInfo("[Bath Preload] BathBG not found in scene, skipping");
            return;
        }

        var bgTransform = bathBG.transform.Find("bg");
        if (bgTransform == null)
        {
            Plugin.Log.LogInfo("[Bath Preload] BathBG/bg not found, skipping");
            return;
        }

        int injected = 0;

        // Iterate through ALL possible bath numbers (1-5)
        for (int bathNum = 1; bathNum <= 5; bathNum++)
        {
            string bathName = $"bath_{bathNum}";
            
            // Check if we have a custom texture for this bath
            if (!texturePathIndex.ContainsKey(bathName))
                continue;

            // Find the bath GameObject (might not exist yet if not loaded)
            Transform bathTransform = bgTransform.Find(bathName);
            if (bathTransform != null)
            {
                var sr = bathTransform.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    // Create custom sprite
                    Sprite customSprite = LoadCustomSprite(bathName, sr.sprite);
                    if (customSprite != null)
                    {
                        sr.sprite = customSprite;
                        Plugin.Log.LogInfo($"[Bath Preload] ✓ Injected {bathName} custom sprite into scene");
                        injected++;
                    }
                }
            }
            else
            {
                Plugin.Log.LogInfo($"[Bath Preload] {bathName} GameObject not found (not loaded yet)");
            }
        }

        if (injected > 0)
        {
            Plugin.Log.LogInfo($"[Bath Preload] Successfully injected {injected} bath sprite(s)");
        }
        else
        {
            Plugin.Log.LogInfo("[Bath Preload] No bath sprites injected - GameObjects may not be loaded yet");
        }
    }

    /// <summary>
    /// Called when a scene is loaded - scans all sprites in the scene and replaces them
    /// </summary>
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Plugin.Log.LogInfo($"Scanning scene '{scene.name}' for textures...");

        // Reset bath background scan attempts for new scene
        bathBackgroundScanAttempts = 0;
        
        // PRELOAD ALL BATH SPRITES and inject them into the scene
        PreloadAndInjectBathSprites();

        int replacedCount = 0;

        // Scan all SpriteRenderers
        var spriteRenderers = UnityEngine.Object.FindObjectsOfType<SpriteRenderer>();
        foreach (var sr in spriteRenderers)
        {
            if (sr.sprite != null)
            {
                string spriteName = sr.sprite.name;
                
                // Log if enabled and not already logged
                if (Plugin.Config.LogReplaceableTextures.Value && !loggedTextures.Contains(spriteName))
                {
                    loggedTextures.Add(spriteName);
                    Plugin.Log.LogInfo($"[Replaceable Sprite - Scene] {spriteName}");
                }

                // Try to replace with custom sprite if enabled
                if (Plugin.Config.EnableCustomTextures.Value)
                {
                    Sprite customSprite = LoadCustomSprite(spriteName, sr.sprite);
                    if (customSprite != null)
                    {
                        sr.sprite = customSprite;
                        Plugin.Log.LogInfo($"Replaced sprite in scene: {spriteName}");
                        replacedCount++;
                    }
                }
            }
        }

        // Scan all UI Images
        var images = UnityEngine.Object.FindObjectsOfType<Image>();
        Plugin.Log.LogInfo($"Found {images.Length} UI Image components in scene");
        
        foreach (var img in images)
        {
            string gameObjectPath = GetGameObjectPath(img.gameObject);
            
            // Check both sprite and overrideSprite
            Sprite activeSprite = img.overrideSprite ?? img.sprite;
            
            if (activeSprite != null)
            {
                string spriteName = activeSprite.name;
                
                // Log if enabled and not already logged
                if (Plugin.Config.LogReplaceableTextures.Value && !loggedTextures.Contains(spriteName))
                {
                    loggedTextures.Add(spriteName);
                    if (Plugin.Config.LogTexturePaths.Value)
                    {
                        Plugin.Log.LogInfo($"[Replaceable UI Sprite - Scene] {spriteName}\n  Path: {gameObjectPath}");
                    }
                    else
                    {
                        Plugin.Log.LogInfo($"[Replaceable UI Sprite - Scene] {spriteName}");
                    }
                }

                // Try to replace with custom sprite if enabled
                if (Plugin.Config.EnableCustomTextures.Value)
                {
                    Sprite customSprite = LoadCustomSprite(spriteName, activeSprite);
                    if (customSprite != null)
                    {
                        // Set both to ensure replacement works
                        img.sprite = customSprite;
                        img.overrideSprite = customSprite;
                        Plugin.Log.LogInfo($"Replaced UI sprite in scene: {spriteName}");
                        replacedCount++;
                    }
                }
            }
            else if (Plugin.Config.LogReplaceableTextures.Value && Plugin.Config.LogTexturePaths.Value)
            {
                // Log Image components with no sprite (might use material directly)
                Plugin.Log.LogInfo($"[UI Image - No Sprite]\n  Path: {gameObjectPath}");
            }
        }

        // Scan all RawImages
        var rawImages = UnityEngine.Object.FindObjectsOfType<RawImage>();
        foreach (var raw in rawImages)
        {
            if (raw.texture != null)
            {
                string textureName = raw.texture.name;
                
                // Log if enabled and not already logged
                if (Plugin.Config.LogReplaceableTextures.Value && !loggedTextures.Contains(textureName))
                {
                    loggedTextures.Add(textureName);
                    Plugin.Log.LogInfo($"[Replaceable UI Texture - Scene] {textureName}");
                }

                // Try to replace with custom texture if enabled
                if (Plugin.Config.EnableCustomTextures.Value)
                {
                    Texture2D customTexture = LoadCustomTexture(textureName);
                    if (customTexture != null)
                    {
                        raw.texture = customTexture;
                        Plugin.Log.LogInfo($"Replaced UI texture in scene: {textureName}");
                        replacedCount++;
                    }
                }
            }
        }

        // Force replace specific background sprites that aren't caught by automatic detection
        if (Plugin.Config.EnableCustomTextures.Value)
        {
            // Base location codes - will try with common suffixes (_00, _01, _02)
            string[] baseLocations = new string[]
            {
                "bg", // Generic background
                "va06",
                "vb03", "vb04", "vb05", "vb06", "vb07", "vb09", "vb10", "vb11", "vb12",
                "vc01", "vc03", "vc04", "vc06", "vc18", "vc21",
                "vd01", "vd02", "vd03", "vd05", "vd06", "vd07", "vd08", "vd16", "vd17", "vd19",
                "ve01", "ve02", "ve03", "ve04", "ve05", "ve07", "ve10", "ve14",
                "vf01", "vf02", "vf03", "vf04", "vf05",
                "vg01", "vg02", "vg05", "vg08", "vg13", "vg15", "vg16", "vg17",
                "vh01", "vh02", "vh03", "vh04", "vh06", "vh10", "vh11",
                "vi01", "vi05", "vi12",
                "vk01", "vk06", "vk07", "vk08", "vk10", "vk20", "vk22", "vk29", "vk32"
            };

            // Common suffixes to try for each location
            string[] suffixes = new string[] { "", "_00", "_01", "_02", "_03", "_04", "_05", "_06", "_07", "_08" };

            foreach (var baseLocation in baseLocations)
            {
                foreach (var suffix in suffixes)
                {
                    string path = $"bgManagerHD/{baseLocation}{suffix}";
                    var bgManager = GameObject.Find(path);
                    
                    if (bgManager != null)
                    {
                        // Get all child SpriteRenderers
                        var bgSprites = bgManager.GetComponentsInChildren<SpriteRenderer>(true);
                        foreach (var sr in bgSprites)
                        {
                            if (sr.sprite != null)
                            {
                                string spriteName = sr.sprite.name;
                                
                                // Log if not already logged
                                if (Plugin.Config.LogReplaceableTextures.Value && !loggedTextures.Contains(spriteName))
                                {
                                    loggedTextures.Add(spriteName);
                                    Plugin.Log.LogInfo($"[Replaceable Sprite - Forced] {spriteName} (from {path})");
                                }
                                
                                // Try to replace
                                Sprite customSprite = LoadCustomSprite(spriteName, sr.sprite);
                                if (customSprite != null)
                                {
                                    sr.sprite = customSprite;
                                    Plugin.Log.LogInfo($"Force replaced background sprite: {spriteName} (from {path})");
                                    replacedCount++;
                                }
                            }
                        }
                    }
                }
            }

            // Scan bath backgrounds (bath_1 to bath_5) - AppRoot/BathBG is the working path
            string[] bathPaths = new string[] 
            { 
                "AppRoot/BathBG"
            };
            
            foreach (var bathPath in bathPaths)
            {
                var bathBG = GameObject.Find(bathPath);
                if (bathBG != null)
                {
                    Plugin.Log.LogInfo($"Checking bath background path: {bathPath}");
                    
                    var bathSprites = bathBG.GetComponentsInChildren<SpriteRenderer>(true);
                    foreach (var sr in bathSprites)
                    {
                        if (sr.sprite != null && sr.sprite.name.StartsWith("bath_"))
                        {
                            string spriteName = sr.sprite.name;
                            
                            // Log if not already logged
                            if (Plugin.Config.LogReplaceableTextures.Value && !loggedTextures.Contains(spriteName))
                            {
                                loggedTextures.Add(spriteName);
                                Plugin.Log.LogInfo($"[Replaceable Sprite - Bath] {spriteName} (from {bathPath})");
                            }
                            
                            // Try to replace
                            Sprite customSprite = LoadCustomSprite(spriteName, sr.sprite);
                            if (customSprite != null)
                            {
                                sr.sprite = customSprite;
                                Plugin.Log.LogInfo($"Force replaced bath sprite: {spriteName} (from {bathPath})");
                                replacedCount++;
                            }
                        }
                    }
                }
                else
                {
                    Plugin.Log.LogInfo($"Bath background NOT found at: {bathPath}");
                }
            }
            
        }

        if (Plugin.Config.EnableCustomTextures.Value && replacedCount > 0)
        {
            Plugin.Log.LogInfo($"Scene scan complete. Replaced {replacedCount} texture(s).");
        }
        else
        {
            Plugin.Log.LogInfo($"Scene scan complete.");
        }
    }

    /// <summary>
    /// Clear all cached textures (useful for reloading)
    /// </summary>
    public static void ClearCache()
    {
        customSpriteCache.Clear();
        customTextureCache.Clear();
        Plugin.Log.LogInfo("Custom texture cache cleared");
    }
}
