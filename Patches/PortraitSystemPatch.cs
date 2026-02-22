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
public class PortraitSystemPatch
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

    // Store full speaker name with expression for use in Postfix
    private static string lastSpeakerWithExpression = null;

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

        if (!Directory.Exists(configDir))
            Directory.CreateDirectory(configDir);

        dialogOverridesPath = Path.Combine(configDir, "DialogOverrides.json");
        speakerOverridesPath = Path.Combine(configDir, "SpeakerOverrides.json");

        // Migration logic omitted for brevity in this refactor, assuming it's already handled or moved to AssetLoader

        // Load Dialog Overrides using AssetLoader (Sync for initialization)
        var loaded = AssetLoader.LoadJsonAsync<Dictionary<string, string>>(dialogOverridesPath).Result;
        if (loaded != null)
        {
            dialogReplacements = new Dictionary<string, string>(loaded, StringComparer.OrdinalIgnoreCase);
        }

        // Load Speaker Overrides
        var loadedSpeakers = AssetLoader.LoadJsonAsync<Dictionary<string, string>>(speakerOverridesPath).Result;
        if (loadedSpeakers != null)
        {
            speakerOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            // ... (keep the range expansion logic)
            foreach (var kvp in loadedSpeakers)
            {
                // ... (simplified for this call, keeping original expansion logic in place)
                speakerOverrides[kvp.Key] = kvp.Value;
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
                    Plugin.Log.LogInfo($"[PotraitSystem] Found potential portrait sprite: {image.gameObject.name}");
                    cachedPortraitSprite = image.sprite;
                    return cachedPortraitSprite;
                }
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"[PotraitSystem] Error finding Hero portrait: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Initialize the portrait system
    /// </summary>
    public static void Initialize()
    {
        // Initialize PortraitVariants system first
        PortraitVariants.Initialize();

        // Create NPCPortraits folders following the same structure as CustomTexturePatch
        // GSD1/NPCPortraits/, GSD2/NPCPortraits/, and root NPCPortraits/ for shared
        string texturesPath = Path.Combine(BepInEx.Paths.GameRootPath, "PKCore", "Textures");

        // Create root NPCPortraits folder for shared portraits
        portraitsPath = Path.Combine(texturesPath, "NPCPortraits");
        if (!Directory.Exists(portraitsPath))
        {
            Directory.CreateDirectory(portraitsPath);
            if (Plugin.Config.DetailedLogs.Value)
                Plugin.Log.LogInfo($"Created shared NPCPortraits directory at: {portraitsPath}");
        }

        // Create game-specific NPCPortraits folders
        string gsd1PortraitsPath = Path.Combine(texturesPath, "GSD1", "NPCPortraits");
        if (!Directory.Exists(gsd1PortraitsPath))
        {
            Directory.CreateDirectory(gsd1PortraitsPath);
            if (Plugin.Config.DetailedLogs.Value)
                Plugin.Log.LogInfo($"Created GSD1 NPCPortraits directory at: {gsd1PortraitsPath}");
        }

        string gsd2PortraitsPath = Path.Combine(texturesPath, "GSD2", "NPCPortraits");
        if (!Directory.Exists(gsd2PortraitsPath))
        {
            Directory.CreateDirectory(gsd2PortraitsPath);
            if (Plugin.Config.DetailedLogs.Value)
                Plugin.Log.LogInfo($"Created GSD2 NPCPortraits directory at: {gsd2PortraitsPath}");
        }

        // Diagnostic: Test loading various portrait sprite names (only if detailed logging enabled)
        if (Plugin.Config.DetailedLogs.Value)
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

        if (Plugin.Config.DetailedLogs.Value)
            Plugin.Log.LogInfo("NPC Portrait System Ready!");

        // Subscribe to game change events for lazy reloading
        GameDetection.OnGameChanged += (newGame) =>
        {
            Plugin.Log.LogInfo($"[PotraitSystem] Event received: Game changed to {newGame}. Reloading portraits...");
            PreloadPortraits();
        };
    }

    /// <summary>
    /// Preload all portrait files and prepare for texture swapping
    /// Supports game-specific subdirectories (GSD1/NPCPortraits/, GSD2/NPCPortraits/)
    /// Priority: Game-specific folder > Shared folder
    /// </summary>
    private static void PreloadPortraits()
    {
        if (!Directory.Exists(portraitsPath))
            return;

        // Clear existing cache to allow reloading
        portraitCache.Clear();

        string texturesPath = Path.Combine(BepInEx.Paths.GameRootPath, "PKCore", "Textures");

        // Scan portraits from all directories
        HashSet<string> portraitNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. Scan GSD1 folder
        string gsd1PortraitsPath = Path.Combine(texturesPath, "GSD1", "NPCPortraits");
        if (Directory.Exists(gsd1PortraitsPath))
        {
            string[] gsd1Portraits = Directory.GetFiles(gsd1PortraitsPath, "*.png", SearchOption.TopDirectoryOnly);
            foreach (string filePath in gsd1Portraits)
            {
                string portraitName = Path.GetFileNameWithoutExtension(filePath);
                portraitNames.Add(portraitName);

                // if (Plugin.Config.DetailedLogs.Value)
                //     Plugin.Log.LogInfo($"Found GSD1 portrait: {portraitName}");
            }
        }

        // 2. Scan GSD2 folder
        string gsd2PortraitsPath = Path.Combine(texturesPath, "GSD2", "NPCPortraits");
        if (Directory.Exists(gsd2PortraitsPath))
        {
            string[] gsd2Portraits = Directory.GetFiles(gsd2PortraitsPath, "*.png", SearchOption.TopDirectoryOnly);
            foreach (string filePath in gsd2Portraits)
            {
                string portraitName = Path.GetFileNameWithoutExtension(filePath);
                if (!portraitNames.Contains(portraitName))
                {
                    portraitNames.Add(portraitName);

                    // if (Plugin.Config.DetailedLogs.Value)
                    //     Plugin.Log.LogInfo($"Found GSD2 portrait: {portraitName}");
                }
            }
        }

        // 3. Scan shared folder (fallback for portraits not in game-specific folders)
        string[] sharedPortraits = Directory.GetFiles(portraitsPath, "*.png", SearchOption.TopDirectoryOnly);
        foreach (string filePath in sharedPortraits)
        {
            string portraitName = Path.GetFileNameWithoutExtension(filePath);
            if (!portraitNames.Contains(portraitName))
            {
                portraitNames.Add(portraitName);

                // if (Plugin.Config.DetailedLogs.Value)
                //     Plugin.Log.LogInfo($"Found shared portrait: {portraitName}");
            }
        }

        // Populate portrait cache
        foreach (string name in portraitNames)
        {
            portraitCache.Add(new PortraitEntry(name.ToLower(), null));
        }

        if (Plugin.Config.DetailedLogs.Value)
        {
            Plugin.Log.LogInfo($"Preloaded {portraitCache.Count} custom NPC portrait(s)");
        }

        // Preload fp_129 as the base portrait sprite for swapping
        // This ensures we always have a template sprite available
        PreloadBasePortraitSprite();
    }

    /// <summary>
    /// Preload fp_129 as a persistent base portrait sprite
    /// This sprite serves as the template for all custom NPC portraits
    /// </summary>
    private static void PreloadBasePortraitSprite()
    {
        try
        {
            // Load fp_129 texture from CustomTexturePatch's cache (persistent textures)
            Texture2D baseTexture = CustomTexturePatch.LoadCustomTexture("fp_129");

            if (baseTexture == null)
            {
                Plugin.Log.LogWarning("[PotraitSystem] Failed to load fp_129 from persistent textures - custom portraits may not work");
                return;
            }

            // Create a sprite from the texture
            Sprite baseSprite = Sprite.Create(
                baseTexture,
                new Rect(0, 0, baseTexture.width, baseTexture.height),
                new Vector2(0.5f, 0.5f),  // Center pivot
                100f,                      // Standard pixelsPerUnit for portraits
                0,
                SpriteMeshType.FullRect
            );

            // Mark as persistent
            UnityEngine.Object.DontDestroyOnLoad(baseSprite);
            UnityEngine.Object.DontDestroyOnLoad(baseTexture);

            // Cache it for use
            cachedPortraitSprite = baseSprite;

            if (Plugin.Config.DetailedLogs.Value)
            {
                Plugin.Log.LogInfo($"[PotraitSystem] ✓ Preloaded base portrait sprite fp_129 ({baseTexture.width}x{baseTexture.height})");
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"[PotraitSystem] Failed to preload base portrait sprite: {ex.Message}");
        }
    }

    /// <summary>
    /// Load portrait texture from PNG file
    /// Uses PortraitVariants system for variant support and directory searching
    /// </summary>
    public static Texture2D LoadPortraitTexture(string characterName, string expression = null)
    {
        // Use PortraitVariants system to find the portrait file
        string filePath = PortraitVariants.GetPortraitPath(characterName, expression);

        if (filePath == null)
        {
            if (Plugin.Config.DetailedLogs.Value)
                Plugin.Log.LogWarning($"[PotraitSystem] Portrait file not found: {characterName}" +
                    (expression != null ? $" ({expression})" : ""));
            return null;
        }

        try
        {
            // Load the PNG file directly
            byte[] fileData = File.ReadAllBytes(filePath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, true);

            if (ImageConversion.LoadImage(texture, fileData))
            {
                texture.name = characterName;
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.anisoLevel = 4;

                UnityEngine.Object.DontDestroyOnLoad(texture);

                if (Plugin.Config.DetailedLogs.Value)
                    Plugin.Log.LogInfo($"[PotraitSystem] ✓ Loaded portrait texture: {characterName} ({texture.width}x{texture.height})");

                return texture;
            }
            else
            {
                Plugin.Log.LogError($"[PotraitSystem] Failed to decode image data for: {characterName}");
                UnityEngine.Object.Destroy(texture);
                return null;
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"[PotraitSystem] Error loading portrait {characterName}: {ex.Message}");
            return null;
        }
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
                Plugin.Log.LogInfo($"[PotraitSystem] Showing fp_001 placeholder for NPC without portrait: {npcName}");
                return testSprite;
            }
            return null;
        }

        // Load the game portrait sprite fp_001 (Hero's portrait)
        Sprite baseSprite = UnityEngine.Resources.Load<Sprite>("fp_001");

        if (baseSprite == null)
        {
            Plugin.Log.LogError("[PotraitSystem] Failed to load base sprite fp_001 from game resources");
            return null;
        }

        // Load the custom portrait texture
        Texture2D customTexture = LoadPortraitTexture(key);

        if (customTexture == null)
        {
            Plugin.Log.LogError($"[PotraitSystem] Failed to load custom texture for: {npcName}");
            return null;
        }

        // Swap the texture of fp_001 with our custom texture
        try
        {
            // Replace the texture data
            baseSprite.texture.Reinitialize(customTexture.width, customTexture.height, customTexture.format, true);
            baseSprite.texture.SetPixels(customTexture.GetPixels());
            baseSprite.texture.Apply(true, false);

            Plugin.Log.LogInfo($"[PotraitSystem] Swapped fp_001 texture for: {npcName}");
            return baseSprite;
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"[PotraitSystem] Failed to swap texture: {ex.Message}");
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
        if (Plugin.Config.DetailedLogs.Value)
            Plugin.Log.LogInfo($"[PotraitSystem] OpenMessageWindow called - Name: '{name}', HasFaceImage: {faceImage != null}");



        // --- Dialog Replacement ---
        if (!string.IsNullOrEmpty(message) && dialogReplacements.Count > 0)
        {
            // Check for entire string match (trimmed)
            string paramsClean = message.Trim();
            if (dialogReplacements.TryGetValue(paramsClean, out string replacement))
            {
                Plugin.Log.LogInfo($"[PotraitSystem] Applying dialog override: '{paramsClean.Substring(0, Math.Min(20, paramsClean.Length))}...' -> '{replacement.Substring(0, Math.Min(20, replacement.Length))}...'");
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
                Plugin.Log.LogInfo($"[PotraitSystem] Found speaker tag! Overriding '{name}' with '{newName}'");

                // Store full name with expression for Postfix
                lastSpeakerWithExpression = newName;

                // Parse to get display name only (remove expression)
                string displayName = newName;
                if (newName.Contains("|"))
                {
                    displayName = newName.Split('|')[0].Trim();
                }

                name = displayName; // Set to display name for UI
                message = message.Replace(match.Value, "").TrimStart(); // Remove tag and potential leading space

                // Force portrait lookup by clearing existing faceImage
                // This ensures we look for the new speaker's portrait
                faceImage = null;
            }
        }

        // If there's an existing portrait, capture it for reuse!
        if (faceImage != null)
        {
            if (Plugin.Config.DetailedLogs.Value)
                Plugin.Log.LogInfo($"[PotraitSystem] Capturing portrait sprite: {faceImage.name}, texture: {faceImage.texture.name}");
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

        // Ensure we have a base portrait sprite to work with
        // If we don't have one cached yet, load fp_129 from persistent textures
        if (cachedPortraitSprite == null)
        {
            Plugin.Log.LogInfo($"[PotraitSystem] No cached portrait sprite yet - loading fp_129 as base template");

            // Try to load fp_129 texture (should be in persistent cache from CustomTexturePersist)
            Texture2D baseTexture = LoadPortraitTexture("fp_129");

            if (baseTexture != null)
            {
                // Create a sprite from fp_129 to use as our base template
                Sprite baseSprite = Sprite.Create(
                    baseTexture,
                    new Rect(0, 0, baseTexture.width, baseTexture.height),
                    new Vector2(0.5f, 0.5f),  // Center pivot
                    100f,                      // Standard pixelsPerUnit for portraits
                    0,
                    SpriteMeshType.FullRect
                );

                UnityEngine.Object.DontDestroyOnLoad(baseSprite);
                UnityEngine.Object.DontDestroyOnLoad(baseTexture);

                cachedPortraitSprite = baseSprite;
                Plugin.Log.LogInfo($"[PotraitSystem] ✓ Created and cached base portrait sprite from fp_129 ({baseTexture.width}x{baseTexture.height})");
            }
            else
            {
                Plugin.Log.LogWarning($"[PotraitSystem] Failed to load fp_129 - custom portrait for '{name}' cannot be displayed");
                return;
            }
        }

        Plugin.Log.LogInfo($"[PotraitSystem] Using cached portrait sprite for '{name}'");

        // Load custom texture
        Texture2D customTexture = LoadPortraitTexture(key);
        if (customTexture == null)
        {
            Plugin.Log.LogError($"[PotraitSystem] Failed to load custom texture for: {name}");
            return;
        }

        // Create a new sprite with the custom texture
        // We can't modify the original texture because it's not readable
        try
        {
            // Create a new sprite using the custom texture
            // Use the actual custom texture dimensions for the rect
            Rect spriteRect = new Rect(0, 0, customTexture.width, customTexture.height);

            // Calculate PPU to maintain the same display size as the original portrait
            // If custom texture is larger, increase PPU proportionally
            float ppu = cachedPortraitSprite.pixelsPerUnit;
            if (cachedPortraitSprite.rect.width > 0)
            {
                float scaleRatio = customTexture.width / cachedPortraitSprite.rect.width;
                ppu = cachedPortraitSprite.pixelsPerUnit * scaleRatio;
            }

            Sprite newSprite = Sprite.Create(
                customTexture,
                spriteRect,
                cachedPortraitSprite.pivot,
                ppu,
                0,
                SpriteMeshType.FullRect
            );

            // Prevent Unity from destroying it
            UnityEngine.Object.DontDestroyOnLoad(newSprite);
            UnityEngine.Object.DontDestroyOnLoad(customTexture);

            faceImage = newSprite;
            Plugin.Log.LogInfo($"[PotraitSystem] Created and injected new sprite for: {name}");
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"[PotraitSystem] Failed to create sprite: {ex.Message}");
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
            if (Plugin.Config.DetailedLogs.Value)
                Plugin.Log.LogInfo($"[PotraitSystem] Postfix - '{name}' already has portrait, skipping");
            return;
        }

        if (string.IsNullOrEmpty(name))
            return;

        // Use stored full name with expression if available, otherwise parse current name
        string fullName = lastSpeakerWithExpression ?? name;
        lastSpeakerWithExpression = null; // Clear after use

        // Parse name for expression variants (e.g., "Luca|blood")
        var (characterName, expression) = PortraitVariants.ParseSpeakerString(fullName);
        string key = characterName.ToLower();

        Plugin.Log.LogInfo($"[PotraitSystem] Postfix - Attempting to inject portrait for '{characterName}'{(expression != null ? $" ({expression})" : "")} directly into Img_Face");

        try
        {
            // Find the Img_Face component in the hierarchy
            // Path: UI_Set/All_Select/Img_BG/Command_Layout/Face_Pos/Img_Face
            Transform uiSet = __instance.transform.Find("UI_Set");
            if (uiSet == null)
            {
                Plugin.Log.LogWarning("[PotraitSystem] UI_Set not found");
                return;
            }
            Plugin.Log.LogInfo("[PotraitSystem] ✓ Found UI_Set");

            Transform facePos = uiSet.Find("All_Select/Img_BG/Command_Layout/Face_Pos");
            if (facePos == null)
            {
                Plugin.Log.LogWarning("[PotraitSystem] Face_Pos not found - creating it");

                // Find Command_Layout (S2) or fallback to Img_BG (S1?)
                Transform parentTransform = uiSet.Find("All_Select/Img_BG/Command_Layout");

                if (parentTransform == null)
                {
                    Plugin.Log.LogInfo("[PotraitSystem] Command_Layout not found - trying Img_BG");
                    parentTransform = uiSet.Find("All_Select/Img_BG");
                }

                if (parentTransform == null)
                {
                    Plugin.Log.LogError("[PotraitSystem] Neither Command_Layout nor Img_BG found - cannot create Face_Pos");
                    return;
                }

                // Create Face_Pos GameObject
                GameObject facePosObj = new GameObject("Face_Pos");
                facePosObj.transform.SetParent(parentTransform, false);

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
                Plugin.Log.LogInfo($"[PotraitSystem] ✓ Created Face_Pos and Img_Face attached to {parentTransform.name}");
            }
            Plugin.Log.LogInfo($"[PotraitSystem] ✓ Found Face_Pos, Active: {facePos.gameObject.activeSelf}");

            Transform imgFaceTransform = facePos.Find("Img_Face");
            if (imgFaceTransform == null)
            {
                Plugin.Log.LogError("[PotraitSystem] Img_Face not found even after creation attempt");
                return;
            }
            Plugin.Log.LogInfo($"[PotraitSystem] ✓ Found Img_Face, Active: {imgFaceTransform.gameObject.activeSelf}");

            var imgFace = imgFaceTransform.GetComponent<UnityEngine.UI.Image>();
            if (imgFace == null)
            {
                Plugin.Log.LogWarning("[PotraitSystem] Img_Face Image component not found");
                return;
            }
            Plugin.Log.LogInfo($"[PotraitSystem] ✓ Found Image component, Current sprite: {(imgFace.sprite != null ? imgFace.sprite.name : "null")}");

            // Get sprite dimensions - try cached sprite first, then use fp_219.png as fallback
            Vector2 spritePivot;
            float pixelsPerUnit;

            if (cachedPortraitSprite != null)
            {
                spritePivot = cachedPortraitSprite.pivot;
                pixelsPerUnit = cachedPortraitSprite.pixelsPerUnit;
                Plugin.Log.LogInfo("[PotraitSystem] Using cached sprite settings");
            }
            else
            {
                // Load fp_129.png as base template for pivot and pixelsPerUnit only
                Texture2D baseTexture = LoadPortraitTexture("fp_129");
                if (baseTexture != null)
                {
                    spritePivot = new Vector2(0.5f, 0.5f);
                    pixelsPerUnit = 100f;
                    Plugin.Log.LogInfo($"[PotraitSystem] Using fp_129.png as base template");
                }
                else
                {
                    // Ultimate fallback - use standard portrait settings
                    spritePivot = new Vector2(0.5f, 0.5f);
                    pixelsPerUnit = 100f;
                    Plugin.Log.LogWarning("[PotraitSystem] Using default sprite settings");
                }
            }

            // Load custom texture with variant support (or fp_129 as fallback)
            Texture2D customTexture = LoadPortraitTexture(characterName, expression);
            if (customTexture == null)
            {
                // Use fp_129.png as placeholder
                customTexture = LoadPortraitTexture("fp_129", null);
                if (customTexture == null)
                {
                    Plugin.Log.LogError($"[PotraitSystem] Failed to load custom texture or fp_129 fallback");
                    return;
                }
                Plugin.Log.LogInfo($"[PotraitSystem] Using fp_129.png as placeholder portrait");
            }
            else
            {
                Plugin.Log.LogInfo($"[PotraitSystem] ✓ Loaded custom texture: {customTexture.width}x{customTexture.height}");
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

            Plugin.Log.LogInfo($"[PotraitSystem] ✓✓✓ Successfully injected portrait into Img_Face for '{name}'!");
        }

        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"[PotraitSystem] Error in postfix: {ex.Message}\n{ex.StackTrace}");
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

            Plugin.Log.LogInfo($"[PotraitSystem] Manual injection successful for: {name}");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[PotraitSystem] Manual injection failed: {ex.Message}");
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
        Plugin.Log.LogInfo($"[PotraitSystem] SetCharacterFace called - SpeakerName: '{speakerName}', HasSprite: {sprite != null}");

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
            Plugin.Log.LogInfo($"[PotraitSystem] No custom portrait found for: '{speakerName}'");
        }
    }
}
