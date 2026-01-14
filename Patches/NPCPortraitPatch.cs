using HarmonyLib;
using UnityEngine;
using System.Text.RegularExpressions;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Encodings.Web;
using Share.UI.Window;
using BepInEx.Unity.IL2CPP;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PKCore.Patches;

/// <summary>
/// Injects custom NPC portraits into the dialogue/message window system.
/// This allows adding portraits for NPCs that don't have them in the base game.
/// </summary>
[HarmonyPatch]
public class NPCPortraitPatch
{
    // Custom class to hold portrait data (IL2CPP doesn't handle tuples well with Unity objects)
    private class PortraitEntry
    {
        public string name;
        public Sprite sprite;
        
        public PortraitEntry(string n, Sprite s)
        {
            name = n;
            sprite = s;
        }
    }
    
    private static string portraitsPath;
    private static List<PortraitEntry> portraitCache = new List<PortraitEntry>();
    private static Sprite cachedPortraitSprite = null; // Cache the Hero's portrait sprite for reuse
    
    // --- Dialog Replacement System ---
    private static Dictionary<string, string> dialogReplacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private static string dialogOverridesPath;
    
    // --- Speaker Injection System ---
    private static Dictionary<string, string> speakerOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private static string speakerOverridesPath;
    
    private static void LoadDialogOverrides()
    {
        string baseDir = Path.Combine(BepInEx.Paths.GameRootPath, "PKCore");
        string configDir = Path.Combine(baseDir, "Config");
        
        // Ensure Config directory exists
        if (!Directory.Exists(configDir))
            Directory.CreateDirectory(configDir);

        dialogOverridesPath = Path.Combine(configDir, "DialogOverrides.json");
        speakerOverridesPath = Path.Combine(configDir, "SpeakerOverrides.json");
        
        // Migration: Move files from old location if they exist there but not in new location
        string oldDialogPath = Path.Combine(baseDir, "DialogOverrides.json");
        if (File.Exists(oldDialogPath) && !File.Exists(dialogOverridesPath))
        {
            try 
            { 
                File.Move(oldDialogPath, dialogOverridesPath); 
                Plugin.Log.LogInfo($"Migrated DialogOverrides.json to Config folder.");
            }
            catch (Exception ex) { Plugin.Log.LogError($"Failed to migrate DialogOverrides.json: {ex.Message}"); }
        }
        
        string oldSpeakerPath = Path.Combine(baseDir, "SpeakerOverrides.json");
        if (File.Exists(oldSpeakerPath) && !File.Exists(speakerOverridesPath))
        {
            try 
            { 
                File.Move(oldSpeakerPath, speakerOverridesPath); 
                Plugin.Log.LogInfo($"Migrated SpeakerOverrides.json to Config folder.");
            }
            catch (Exception ex) { Plugin.Log.LogError($"Failed to migrate SpeakerOverrides.json: {ex.Message}"); }
        }
        
        // Load Dialog Overrides
        if (!File.Exists(dialogOverridesPath))
        {
            // Create default file with examples
            var defaultData = new Dictionary<string, string>
            {
                { "Example original text", "<speaker:Viktor>Example replacement text" },
                { "Another line to replace", "<speaker:Flik>Replaced!" }
            };
            
            try
            {
                string json = JsonSerializer.Serialize(defaultData, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
                File.WriteAllText(dialogOverridesPath, json);
                Plugin.Log.LogInfo($"Created default DialogOverrides.json at {dialogOverridesPath}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to create default DialogOverrides.json: {ex.Message}");
            }
        }
        else
        {
            try
            {
                string json = File.ReadAllText(dialogOverridesPath);
                var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (loaded != null)
                {
                    dialogReplacements = new Dictionary<string, string>(loaded, StringComparer.OrdinalIgnoreCase);
                    Plugin.Log.LogInfo($"Loaded {dialogReplacements.Count} dialog overrides.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to load DialogOverrides.json: {ex.Message}");
            }
        }

        // Load Speaker Overrides
        if (!File.Exists(speakerOverridesPath))
        {
            // Create default file with examples
            var defaultSpeakerData = new Dictionary<string, string>
            {
                { "sys_01:5", "Guard" },
                { "sys_02:10", "Villager" }
            };
            
            try
            {
                string json = JsonSerializer.Serialize(defaultSpeakerData, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
                File.WriteAllText(speakerOverridesPath, json);
                Plugin.Log.LogInfo($"Created default SpeakerOverrides.json at {speakerOverridesPath}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to create default SpeakerOverrides.json: {ex.Message}");
            }
        }
        else
        {
            try
            {
                string json = File.ReadAllText(speakerOverridesPath);
                var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (loaded != null)
                {
                    speakerOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    
                    foreach (var kvp in loaded)
                    {
                        string key = kvp.Key;
                        string value = kvp.Value;
                        
                        // Check for range syntax (e.g., "sys_01:5-10" or "message:100-200")
                        int lastColon = key.LastIndexOf(':');
                        bool isRange = false;
                        
                        if (lastColon != -1 && lastColon < key.Length - 1)
                        {
                            string idPart = key.Substring(0, lastColon);
                            string rangePart = key.Substring(lastColon + 1);
                            
                            // Use Regex to handle negative numbers in ranges (e.g. -50--10)
                            // Matches: (Start) - (End) where Start/End can be signed integers
                            var rangeMatch = Regex.Match(rangePart, @"^(-?\d+)-(-?\d+)$");
                            
                            if (rangeMatch.Success)
                            {
                                if (int.TryParse(rangeMatch.Groups[1].Value, out int start) && 
                                    int.TryParse(rangeMatch.Groups[2].Value, out int end))
                                {
                                    if (start <= end)
                                    {
                                        isRange = true;
                                        int count = 0;
                                        // Limit range size to prevent accidental massive loops (e.g. 1-999999)
                                        if (end - start > 5000)
                                        {
                                            Plugin.Log.LogWarning($"Range {key} is too large (>5000), skipping expansion.");
                                            continue; 
                                        }

                                        for (int i = start; i <= end; i++)
                                        {
                                            string newKey = $"{idPart}:{i}";
                                            speakerOverrides[newKey] = value;
                                            count++;
                                        }
                                        Plugin.Log.LogInfo($"Expanded range '{key}' into {count} entries for speaker '{value}'.");
                                    }
                                }
                            }
                        }
                        
                        if (!isRange)
                        {
                            speakerOverrides[key] = value;
                        }
                    }
                    
                    Plugin.Log.LogInfo($"Loaded {speakerOverrides.Count} active speaker overrides.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to load SpeakerOverrides.json: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Get a dialog override by key (can be text or ID:Index)
    /// </summary>
    public static string GetDialogOverride(string key)
    {
        if (dialogReplacements == null || dialogReplacements.Count == 0)
            return null;
            
        if (dialogReplacements.TryGetValue(key, out string replacement))
            return replacement;
            
        return null;
    }
    
    /// <summary>
    /// Get a speaker override by ID key
    /// </summary>
    public static string GetSpeakerOverride(string key)
    {
        if (speakerOverrides == null || speakerOverrides.Count == 0)
            return null;
            
        if (speakerOverrides.TryGetValue(key, out string speakerName))
            return speakerName;
            
        return null;
    }
    // -------------------------------
    
    /// <summary>
    /// Find and cache the Hero's portrait sprite from the UI
    /// </summary>
    private static Sprite FindHeroPortraitSprite()
    {
        if (cachedPortraitSprite != null)
            return cachedPortraitSprite;
            
        // Search for the Hero's portrait in common UI locations
        // The Hero's portrait is often in UI_Root or similar hierarchies
        try
        {
            // Try to find any Image component with a sprite that looks like a portrait
            var allImages = UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Image>();
            
            foreach (var image in allImages)
            {
                if (image.sprite != null && image.gameObject.name.Contains("Face"))
                {
                    Plugin.Log.LogInfo($"[NPCPortrait] Found potential portrait sprite: {image.gameObject.name}");
                    cachedPortraitSprite = image.sprite;
                    return cachedPortraitSprite;
                }
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"[NPCPortrait] Error finding Hero portrait: {ex.Message}");
        }
        
        return null;
    }
    
    /// <summary>
    /// Initialize the portrait system
    /// </summary>
    public static void Initialize()
    {
        // Create NPCPortraits folder inside PKCore/Textures directory (game root)
        portraitsPath = Path.Combine(BepInEx.Paths.GameRootPath, "PKCore", "Textures", "NPCPortraits");
        
        if (!Directory.Exists(portraitsPath))
        {
            Directory.CreateDirectory(portraitsPath);
            Plugin.Log.LogInfo($"Created NPCPortraits directory at: {portraitsPath}");
        }
        
        // Diagnostic: Test loading various portrait sprite names (only if detailed logging enabled)
        if (Plugin.Config.DetailedTextureLog.Value)
        {
            Plugin.Log.LogInfo("Testing portrait sprite names...");
            
            string[] testNames = { 
                "fp_001", "fp_001.png", "FP_001", "Fp_001",
                "fp_100", "fp_129", "fp_219",
                "FacePortrait_001", "portrait_001", "face_001"
            };
            
            foreach (string testName in testNames)
            {
                // Try loading as Sprite
                var testSprite = UnityEngine.Resources.Load<Sprite>(testName);
                if (testSprite != null)
                {
                    Plugin.Log.LogInfo($"✓ Found SPRITE: {testName}");
                }
                
                // Try loading as Texture2D
                var testTexture = UnityEngine.Resources.Load<Texture2D>(testName);
                if (testTexture != null)
                {
                    Plugin.Log.LogInfo($"✓ Found TEXTURE2D: {testName}");
                }
            }
            
            // Also try to find ALL resources with "fp" in the name
            try
            {
                var allSprites = UnityEngine.Resources.FindObjectsOfTypeAll<Sprite>();
                int fpCount = 0;
                foreach (var sprite in allSprites)
                {
                    if (sprite.name.ToLower().StartsWith("fp_") && fpCount < 10)
                    {
                        Plugin.Log.LogInfo($"✓ Found sprite in scene: {sprite.name}");
                        fpCount++;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error finding sprites: {ex.Message}");
            }
        }
        
        // Preload all portraits for performance
        PreloadPortraits();
        
        // Load dialog overrides
        LoadDialogOverrides();
        
        Plugin.Log.LogInfo("NPC Portrait System Ready!");
    }
    
    /// <summary>
    /// Preload all portrait files and prepare for texture swapping
    /// </summary>
    private static void PreloadPortraits()
    {
        if (!Directory.Exists(portraitsPath))
            return;
            
        string[] portraitFiles = Directory.GetFiles(portraitsPath, "*.png", SearchOption.TopDirectoryOnly);
        
        foreach (string filePath in portraitFiles)
        {
            string portraitName = Path.GetFileNameWithoutExtension(filePath);
            
            try
            {
                // Load the PNG file data
                byte[] fileData = File.ReadAllBytes(filePath);
                Texture2D customTexture = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                
                if (ImageConversion.LoadImage(customTexture, fileData))
                {
                    // Store the texture (not a sprite) for later swapping
                    portraitCache.Add(new PortraitEntry(portraitName.ToLower(), null));
                    // We'll create the sprite dynamically by swapping fp_219's texture
                    if (Plugin.Config.DetailedTextureLog.Value)
                    {
                        Plugin.Log.LogInfo($"Loaded portrait texture: {portraitName}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Failed to load portrait {portraitName}: {ex.Message}");
            }
        }
        
        if (Plugin.Config.DetailedTextureLog.Value)
        {
            Plugin.Log.LogInfo($"Preloaded {portraitCache.Count} custom NPC portrait texture(s)");
        }
    }
    
    /// <summary>
    /// Load portrait texture from PNG file
    /// </summary>
    private static Texture2D LoadPortraitTexture(string npcName)
    {
        string filePath = Path.Combine(portraitsPath, $"{npcName}.png");
        
        if (!File.Exists(filePath))
            return null;
            
        try
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false); // false = no mipmaps
            
            if (ImageConversion.LoadImage(texture, fileData))
            {
                // Set filter mode to prevent white outline on transparent images
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.anisoLevel = 0;
                
                texture.Apply(true, false);
                return texture;
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"Failed to load portrait texture for {npcName}: {ex.Message}");
        }
        
        return null;
    }
    
    /// <summary>
    /// Try to get a custom portrait by NPC name
    /// </summary>
    /// <param name="npcName">Name of the NPC (case-insensitive)</param>
    /// <returns>Custom portrait sprite, or null if not found</returns>
    public static Sprite GetCustomPortrait(string npcName)
    {
        if (string.IsNullOrEmpty(npcName))
            return null;
            
        string key = npcName.ToLower();
        
        // Check if we have a custom portrait for this NPC
        bool hasCustomPortrait = false;
        foreach (var entry in portraitCache)
        {
            if (entry.name.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                hasCustomPortrait = true;
                break;
            }
        }
        
        if (!hasCustomPortrait)
        {
            // DIAGNOSTIC: Load fp_001 for ANY NPC without a portrait to identify them
            // This helps you see which NPCs lack portraits in the game
            Sprite testSprite = UnityEngine.Resources.Load<Sprite>("fp_001");
            if (testSprite != null)
            {
                Plugin.Log.LogInfo($"[NPCPortrait] Showing fp_001 placeholder for NPC without portrait: {npcName}");
                return testSprite;
            }
            return null;
        }
            
        // Load the game portrait sprite fp_001 (Hero's portrait)
        Sprite baseSprite = UnityEngine.Resources.Load<Sprite>("fp_001");
        
        if (baseSprite == null)
        {
            Plugin.Log.LogError("[NPCPortrait] Failed to load base sprite fp_001 from game resources");
            return null;
        }
        
        // Load the custom portrait texture
        Texture2D customTexture = LoadPortraitTexture(key);
        
        if (customTexture == null)
        {
            Plugin.Log.LogError($"[NPCPortrait] Failed to load custom texture for: {npcName}");
            return null;
        }
        
        // Swap the texture of fp_001 with our custom texture
        try
        {
            // Replace the texture data
            baseSprite.texture.Reinitialize(customTexture.width, customTexture.height, customTexture.format, true);
            baseSprite.texture.SetPixels(customTexture.GetPixels());
            baseSprite.texture.Apply(true, false);
            
            Plugin.Log.LogInfo($"[NPCPortrait] Swapped fp_001 texture for: {npcName}");
            return baseSprite;
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"[NPCPortrait] Failed to swap texture: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Intercept UIMessageWindow.OpenMessageWindow to inject custom portraits
    /// This patches the method that opens dialogue with a character portrait
    /// </summary>
    [HarmonyPatch(typeof(UIMessageWindow), nameof(UIMessageWindow.OpenMessageWindow))]
    [HarmonyPatch(new[] { typeof(Sprite), typeof(string), typeof(string), typeof(Vector3), typeof(bool) })]
    [HarmonyPrefix]
    public static void OpenMessageWindow_Prefix(ref Sprite faceImage, ref string name, ref string message)
    {
        if (Plugin.Config.LogReplaceableTextures.Value)
            Plugin.Log.LogInfo($"[NPCPortrait] OpenMessageWindow called - Name: '{name}', HasFaceImage: {faceImage != null}");
        
        // --- Dialog Replacement ---
        if (!string.IsNullOrEmpty(message) && dialogReplacements.Count > 0)
        {
            // Check for entire string match (trimmed)
            string paramsClean = message.Trim();
            if (dialogReplacements.TryGetValue(paramsClean, out string replacement))
            {
                Plugin.Log.LogInfo($"[NPCPortrait] Applying dialog override: '{paramsClean.Substring(0, Math.Min(20, paramsClean.Length))}...' -> '{replacement.Substring(0, Math.Min(20, replacement.Length))}...'");
                message = replacement;
            }
        }
        // -------------------------
        
        // Parsing <speaker:Name> tag from text to override speaker
        if (!string.IsNullOrEmpty(message))
        {
            var match = Regex.Match(message, @"<speaker:([^>]+)>");
            if (match.Success)
            {
                string newName = match.Groups[1].Value;
                Plugin.Log.LogInfo($"[NPCPortrait] Found speaker tag! Overriding '{name}' with '{newName}'");
                
                name = newName;
                message = message.Replace(match.Value, "").TrimStart(); // Remove tag and potential leading space
                
                // Force portrait lookup by clearing existing faceImage
                // This ensures we look for the new speaker's portrait
                faceImage = null;
            }
        }
        
        // If there's an existing portrait, capture it for reuse!
        if (faceImage != null)
        {
            if (Plugin.Config.LogReplaceableTextures.Value)
                Plugin.Log.LogInfo($"[NPCPortrait] Capturing portrait sprite: {faceImage.name}, texture: {faceImage.texture.name}");
            cachedPortraitSprite = faceImage;
            return;
        }
        
        // For NPCs without portraits, try to inject a custom one
        if (string.IsNullOrEmpty(name))
            return;
            
        string key = name.ToLower();
        bool hasCustomPortrait = false;
        
        foreach (var entry in portraitCache)
        {
            if (entry.name.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                hasCustomPortrait = true;
                break;
            }
        }
        
        if (!hasCustomPortrait)
            return;
            
        // We have a custom portrait, but need a base sprite to swap
        if (cachedPortraitSprite == null)
        {
            Plugin.Log.LogWarning($"[NPCPortrait] No cached portrait sprite yet - talk to an NPC with a portrait first (like Viktor)");
            return;
        }
        
        Plugin.Log.LogInfo($"[NPCPortrait] Using cached portrait sprite for '{name}'");
        
        // Load custom texture
        Texture2D customTexture = LoadPortraitTexture(key);
        if (customTexture == null)
        {
            Plugin.Log.LogError($"[NPCPortrait] Failed to load custom texture for: {name}");
            return;
        }
        
        // Create a new sprite with the custom texture
        // We can't modify the original texture because it's not readable
        try
        {
            // Create a new sprite using the custom texture
            Sprite newSprite = Sprite.Create(
                customTexture,
                cachedPortraitSprite.rect,
                cachedPortraitSprite.pivot,
                cachedPortraitSprite.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect
            );
            
            // Prevent Unity from destroying it
            UnityEngine.Object.DontDestroyOnLoad(newSprite);
            UnityEngine.Object.DontDestroyOnLoad(customTexture);
            
            faceImage = newSprite;
            Plugin.Log.LogInfo($"[NPCPortrait] Created and injected new sprite for: {name}");
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"[NPCPortrait] Failed to create sprite: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Postfix to directly manipulate the portrait Image component
    /// </summary>
    [HarmonyPatch(typeof(UIMessageWindow), nameof(UIMessageWindow.OpenMessageWindow))]
    [HarmonyPatch(new[] { typeof(Sprite), typeof(string), typeof(string), typeof(Vector3), typeof(bool) })]
    [HarmonyPostfix]
    public static void OpenMessageWindow_Postfix(UIMessageWindow __instance, Sprite faceImage, string name)
    {
        // Only try to inject if there's no existing portrait
        if (faceImage != null)
        {
            if (Plugin.Config.LogReplaceableTextures.Value)
                Plugin.Log.LogInfo($"[NPCPortrait] Postfix - '{name}' already has portrait, skipping");
            return;
        }
            
        if (string.IsNullOrEmpty(name))
            return;
        
        string key = name.ToLower();
            
        Plugin.Log.LogInfo($"[NPCPortrait] Postfix - Attempting to inject portrait for '{name}' directly into Img_Face");
        
        try
        {
            // Find the Img_Face component in the hierarchy
            // Path: UI_Set/All_Select/Img_BG/Command_Layout/Face_Pos/Img_Face
            Transform uiSet = __instance.transform.Find("UI_Set");
            if (uiSet == null)
            {
                Plugin.Log.LogWarning("[NPCPortrait] UI_Set not found");
                return;
            }
            Plugin.Log.LogInfo("[NPCPortrait] ✓ Found UI_Set");
            
            Transform facePos = uiSet.Find("All_Select/Img_BG/Command_Layout/Face_Pos");
            if (facePos == null)
            {
                Plugin.Log.LogWarning("[NPCPortrait] Face_Pos not found - creating it");
                
                // Find Command_Layout
                Transform commandLayout = uiSet.Find("All_Select/Img_BG/Command_Layout");
                if (commandLayout == null)
                {
                    Plugin.Log.LogError("[NPCPortrait] Command_Layout not found - cannot create Face_Pos");
                    return;
                }
                
                // Create Face_Pos GameObject
                GameObject facePosObj = new GameObject("Face_Pos");
                facePosObj.transform.SetParent(commandLayout, false);
                
                // Add RectTransform (required for UI)
                RectTransform facePosRect = facePosObj.AddComponent<RectTransform>();
                facePosRect.anchorMin = new Vector2(0, 0);
                facePosRect.anchorMax = new Vector2(0, 1);
                facePosRect.pivot = new Vector2(0, 0.5f);
                facePosRect.anchoredPosition = Vector2.zero;
                facePosRect.sizeDelta = new Vector2(100, 100);
                
                // Create Img_Face child
                GameObject imgFaceObj = new GameObject("Img_Face");
                imgFaceObj.transform.SetParent(facePosObj.transform, false);
                
                RectTransform imgFaceRect = imgFaceObj.AddComponent<RectTransform>();
                imgFaceRect.anchorMin = Vector2.zero;
                imgFaceRect.anchorMax = Vector2.one;
                imgFaceRect.pivot = new Vector2(0.5f, 0.5f);
                imgFaceRect.anchoredPosition = Vector2.zero;
                imgFaceRect.sizeDelta = Vector2.zero;
                
                // Add Image component
                var imgFaceComponent = imgFaceObj.AddComponent<UnityEngine.UI.Image>();
                imgFaceComponent.raycastTarget = false;
                
                facePos = facePosObj.transform;
                Plugin.Log.LogInfo("[NPCPortrait] ✓ Created Face_Pos and Img_Face");
            }
            Plugin.Log.LogInfo($"[NPCPortrait] ✓ Found Face_Pos, Active: {facePos.gameObject.activeSelf}");
            
            Transform imgFaceTransform = facePos.Find("Img_Face");
            if (imgFaceTransform == null)
            {
                Plugin.Log.LogError("[NPCPortrait] Img_Face not found even after creation attempt");
                return;
            }
            Plugin.Log.LogInfo($"[NPCPortrait] ✓ Found Img_Face, Active: {imgFaceTransform.gameObject.activeSelf}");
            
            var imgFace = imgFaceTransform.GetComponent<UnityEngine.UI.Image>();
            if (imgFace == null)
            {
                Plugin.Log.LogWarning("[NPCPortrait] Img_Face Image component not found");
                return;
            }
            Plugin.Log.LogInfo($"[NPCPortrait] ✓ Found Image component, Current sprite: {(imgFace.sprite != null ? imgFace.sprite.name : "null")}");
            
            // Get sprite dimensions - try cached sprite first, then use fp_219.png as fallback
            Vector2 spritePivot;
            float pixelsPerUnit;
            
            if (cachedPortraitSprite != null)
            {
                spritePivot = cachedPortraitSprite.pivot;
                pixelsPerUnit = cachedPortraitSprite.pixelsPerUnit;
                Plugin.Log.LogInfo("[NPCPortrait] Using cached sprite settings");
            }
            else
            {
                // Load fp_129.png as base template for pivot and pixelsPerUnit only
                Texture2D baseTexture = LoadPortraitTexture("fp_129");
                if (baseTexture != null)
                {
                    spritePivot = new Vector2(0.5f, 0.5f);
                    pixelsPerUnit = 100f;
                    Plugin.Log.LogInfo($"[NPCPortrait] Using fp_129.png as base template");
                }
                else
                {
                    // Ultimate fallback - use standard portrait settings
                    spritePivot = new Vector2(0.5f, 0.5f);
                    pixelsPerUnit = 100f;
                    Plugin.Log.LogWarning("[NPCPortrait] Using default sprite settings");
                }
            }
            
            // Load custom texture (or fp_129 as fallback)
            Texture2D customTexture = LoadPortraitTexture(key);
            if (customTexture == null)
            {
                // Use fp_129.png as placeholder
                customTexture = LoadPortraitTexture("fp_129");
                if (customTexture == null)
                {
                    Plugin.Log.LogError($"[NPCPortrait] Failed to load custom texture or fp_129 fallback");
                    return;
                }
                Plugin.Log.LogInfo($"[NPCPortrait] Using fp_129.png as placeholder portrait");
            }
            else
            {
                Plugin.Log.LogInfo($"[NPCPortrait] ✓ Loaded custom texture: {customTexture.width}x{customTexture.height}");
            }
            
            // Create new sprite using actual texture dimensions
            Sprite newSprite = Sprite.Create(
                customTexture,
                new Rect(0, 0, customTexture.width, customTexture.height),
                spritePivot,
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect
            );
            
            UnityEngine.Object.DontDestroyOnLoad(newSprite);
            UnityEngine.Object.DontDestroyOnLoad(customTexture);
            
            // Activate the entire hierarchy to ensure portrait displays
            // Path: UI_Set/All_Select/Img_BG/Command_Layout/Face_Pos/Img_Face
            uiSet.gameObject.SetActive(true);
            
            Transform allSelect = uiSet.Find("All_Select");
            if (allSelect != null) allSelect.gameObject.SetActive(true);
            
            Transform imgBG = uiSet.Find("All_Select/Img_BG");
            if (imgBG != null) imgBG.gameObject.SetActive(true);
            
            Transform cmdLayout = uiSet.Find("All_Select/Img_BG/Command_Layout");
            if (cmdLayout != null) cmdLayout.gameObject.SetActive(true);
            
            facePos.gameObject.SetActive(true);
            imgFaceTransform.gameObject.SetActive(true);
            
            // Set the sprite
            imgFace.sprite = newSprite;
            
            Plugin.Log.LogInfo($"[NPCPortrait] ✓✓✓ Successfully injected portrait into Img_Face for '{name}'!");
        }

        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"[NPCPortrait] Error in postfix: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Manually injects a portrait for a given NPC name into the specified parent transform.
    /// This is used by SpeakerTagMonitor for non-standard UI elements.
    /// </summary>
    public static void InjectPortraitManual(Transform parent, string name)
    {
        if (parent == null || string.IsNullOrEmpty(name)) return;

        try
        {
            // Try to find reasonable parent to attach to if "parent" is the text object itself
            // Usually we want to go up to the container
            Transform targetParent = parent;
            
            // Heuristic: If we are modifying a Text object, we probably want to attach to its parent panel
            // But for safety, we can try to find an existing structure or create one relative to the text
            
            // For the Cooking Contest specifically, the text might be in a wrapper. 
            // Let's create a dedicated "Portrait_Container" if it doesn't exist.
            
            Transform facePos = targetParent.Find("Face_Pos");
            if (facePos == null)
            {
                // Create Face_Pos GameObject
                GameObject facePosObj = new GameObject("Face_Pos");
                facePosObj.transform.SetParent(targetParent, false);
                
                // Add RectTransform
                RectTransform facePosRect = facePosObj.AddComponent<RectTransform>();
                // Position it to the left of the text, assumed
                facePosRect.anchorMin = new Vector2(0, 0); 
                facePosRect.anchorMax = new Vector2(0, 1);
                facePosRect.pivot = new Vector2(0, 0.5f);
                facePosRect.anchoredPosition = new Vector2(-150, 0); // Offset to left
                facePosRect.sizeDelta = new Vector2(100, 100);
                
                // Create Img_Face child
                GameObject imgFaceObj = new GameObject("Img_Face");
                imgFaceObj.transform.SetParent(facePosObj.transform, false);
                
                RectTransform imgFaceRect = imgFaceObj.AddComponent<RectTransform>();
                imgFaceRect.anchorMin = Vector2.zero;
                imgFaceRect.anchorMax = Vector2.one;
                imgFaceRect.sizeDelta = Vector2.zero;
                
                var imgFaceComponent = imgFaceObj.AddComponent<UnityEngine.UI.Image>();
                imgFaceComponent.raycastTarget = false;
                
                facePos = facePosObj.transform;
            }
            
            var imgFace = facePos.Find("Img_Face")?.GetComponent<UnityEngine.UI.Image>();
            if (imgFace == null) return;
            
            // Load portrait
            string key = name.ToLower();
            
            // Get sprite dimensions - using default fallback as we don't have the nice cached sprite here usually
            Vector2 spritePivot = new Vector2(0.5f, 0.5f);
            float pixelsPerUnit = 100f;
            
            Texture2D customTexture = LoadPortraitTexture(key);
            if (customTexture == null)
            {
                // Try fallback
                customTexture = LoadPortraitTexture("fp_129");
                if (customTexture == null) return;
            }
            
            Sprite newSprite = Sprite.Create(
                customTexture,
                new Rect(0, 0, customTexture.width, customTexture.height),
                spritePivot,
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect
            );
            
            UnityEngine.Object.DontDestroyOnLoad(newSprite);
            UnityEngine.Object.DontDestroyOnLoad(customTexture);
            
            facePos.gameObject.SetActive(true);
            imgFace.gameObject.SetActive(true);
            imgFace.sprite = newSprite;
            
            Plugin.Log.LogInfo($"[NPCPortrait] Manual injection successful for: {name}");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[NPCPortrait] Manual injection failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Intercept UIMessageWindow.SetCharacterFace to inject custom portraits
    /// This catches direct face changes during dialogue
    /// </summary>
    [HarmonyPatch(typeof(UIMessageWindow), nameof(UIMessageWindow.SetCharacterFace))]
    [HarmonyPrefix]
    public static void SetCharacterFace_Prefix(UIMessageWindow __instance, ref Sprite sprite)
    {
        string speakerName = __instance.speakerName;
        Plugin.Log.LogInfo($"[NPCPortrait] SetCharacterFace called - SpeakerName: '{speakerName}', HasSprite: {sprite != null}");
        
        // If there's already a sprite, don't override it
        if (sprite != null)
            return;
            
        // Try to get the speaker name from the message window
        if (string.IsNullOrEmpty(speakerName))
            return;
            
        // Try to inject a custom portrait
        Sprite customPortrait = GetCustomPortrait(speakerName);
        
        if (customPortrait != null)
        {
            sprite = customPortrait;
            Plugin.Log.LogInfo($"Injected custom portrait for: {speakerName}");
        }
        else
        {
            Plugin.Log.LogInfo($"[NPCPortrait] No custom portrait found for: '{speakerName}'");
        }
    }
}
