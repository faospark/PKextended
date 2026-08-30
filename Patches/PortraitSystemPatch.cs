using HarmonyLib;
using UnityEngine;
using System.Text.RegularExpressions;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
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
    private static LocalizedOverrideMap dialogReplacements = new LocalizedOverrideMap();
    private static string dialogOverridesPath;

    // --- Speaker Injection System ---
    private static LocalizedOverrideMap s1SpeakerOverrides = new LocalizedOverrideMap();
    private static LocalizedOverrideMap s2SpeakerOverrides = new LocalizedOverrideMap();
    private static string s1SpeakerOverridesPath;
    private static string s2SpeakerOverridesPath;

    // --- Game-specific Dialog Replacements (from 00-Mods GSD1/ or GSD2/ subfolders) ---
    private static LocalizedOverrideMap gsd1DialogReplacements = new LocalizedOverrideMap();
    private static LocalizedOverrideMap gsd2DialogReplacements = new LocalizedOverrideMap();

    private static bool _dialogOverridesLoaded = false;

    // Tracks the current active message window.
    public static UIMessageWindow ActiveMessageWindow = null;

    // Tracks whether WE activated Name_Set (vs the game activating it for a native name).
    // We must never deactivate a Name_Set the game owns.
    private static bool s_nameSetActivatedByUs = false;

    // Tracks whether WE activated Face_Pos (vs the game activating it natively).
    private static bool s_portraitActivatedByUs = false;

    /// <summary>
    /// Load all dialog/speaker override JSON files from Config/ and 00-Mods/.
    /// Safe to call multiple times — only loads once unless forceReload is true.
    /// </summary>
    public static void LoadDialogOverrides(bool forceReload = false)
    {
        if (_dialogOverridesLoaded && !forceReload) return;
        _dialogOverridesLoaded = true;

        string baseDir = Path.Combine(BepInEx.Paths.GameRootPath, "PKCore");
        string configDir = Path.Combine(baseDir, "Config");

        if (!Directory.Exists(configDir))
            Directory.CreateDirectory(configDir);

        dialogOverridesPath = Path.Combine(configDir, "DialogOverrides.json");
        s1SpeakerOverridesPath = Path.Combine(configDir, "S1SpeakerOverrides.json");
        s2SpeakerOverridesPath = Path.Combine(configDir, "S2SpeakerOverrides.json");

        dialogReplacements.Clear();
        s1SpeakerOverrides.Clear();
        s2SpeakerOverrides.Clear();
        gsd1DialogReplacements.Clear();
        gsd2DialogReplacements.Clear();

        dialogReplacements.LoadFromFile(dialogOverridesPath, "Config/DialogOverrides");
        s1SpeakerOverrides.LoadFromFile(s1SpeakerOverridesPath, "Config/S1SpeakerOverrides");
        s2SpeakerOverrides.LoadFromFile(s2SpeakerOverridesPath, "Config/S2SpeakerOverrides");

        // Load overrides from 00-Mods (highest priority — overrides Config/ files)
        LoadDialogOverridesFromMods();
    }

    /// <summary>
    /// Scans PKCore/00-Mods/&lt;ModName&gt;/ for dialog and speaker override JSON files.
    /// Supports GSD1/ and GSD2/ subfolders for game-specific loading.
    /// Mods are processed in alphabetical order; alphabetically-last mod wins on key conflicts.
    /// Folder conventions:
    ///   00-Mods/&lt;ModName&gt;/DialogOverrides.json          — both games
    ///   00-Mods/&lt;ModName&gt;/GSD1/DialogOverrides.json     — GSD1 only
    ///   00-Mods/&lt;ModName&gt;/GSD2/DialogOverrides.json     — GSD2 only
    ///   00-Mods/&lt;ModName&gt;/S1SpeakerOverrides.json       — GSD1 speakers
    ///   00-Mods/&lt;ModName&gt;/S2SpeakerOverrides.json       — GSD2 speakers
    ///   00-Mods/&lt;ModName&gt;/GSD1/S1SpeakerOverrides.json  — GSD1 speakers (alternate location)
    ///   00-Mods/&lt;ModName&gt;/GSD2/S2SpeakerOverrides.json  — GSD2 speakers (alternate location)
    /// </summary>
    private static void LoadDialogOverridesFromMods()
    {
        string modsRoot = Path.Combine(BepInEx.Paths.GameRootPath, "PKCore", "00-Mods");
        if (!Directory.Exists(modsRoot)) return;

        var modDirs = Directory.GetDirectories(modsRoot)
            .OrderBy(d => d, StringComparer.OrdinalIgnoreCase);

        foreach (var modDir in modDirs)
        {
            string modName = Path.GetFileName(modDir);

            // Shared DialogOverrides (both games)
            MergeJsonIntoDict(Path.Combine(modDir, "DialogOverrides.json"), dialogReplacements, modName);

            // GSD1-specific dialog overrides
            MergeJsonIntoDict(Path.Combine(modDir, "GSD1", "DialogOverrides.json"), gsd1DialogReplacements, modName + "/GSD1");

            // GSD2-specific dialog overrides
            MergeJsonIntoDict(Path.Combine(modDir, "GSD2", "DialogOverrides.json"), gsd2DialogReplacements, modName + "/GSD2");

            // S1 Speaker overrides (root or GSD1 subfolder)
            MergeJsonIntoDict(Path.Combine(modDir, "S1SpeakerOverrides.json"), s1SpeakerOverrides, modName);
            MergeJsonIntoDict(Path.Combine(modDir, "GSD1", "S1SpeakerOverrides.json"), s1SpeakerOverrides, modName + "/GSD1");

            // S2 Speaker overrides (root or GSD2 subfolder)
            MergeJsonIntoDict(Path.Combine(modDir, "S2SpeakerOverrides.json"), s2SpeakerOverrides, modName);
            MergeJsonIntoDict(Path.Combine(modDir, "GSD2", "S2SpeakerOverrides.json"), s2SpeakerOverrides, modName + "/GSD2");
        }
    }

    /// <summary>
    /// Load a JSON dictionary file and merge its entries into <paramref name="target"/>.
    /// Silently skips if the file does not exist.
    /// </summary>
    private static void MergeJsonIntoDict(string filePath, LocalizedOverrideMap target, string logLabel)
    {
        if (target != null && File.Exists(filePath))
        {
            target.LoadFromFile(filePath, logLabel);
        }
    }

    /// <summary>
    /// Get a dialog override by key (can be text or ID:Index).
    /// Game-specific overrides (GSD1/GSD2 subfolders in 00-Mods) take priority over shared ones.
    /// </summary>
    public static string GetDialogOverride(string key)
    {
        // Game-specific overrides take priority over the shared dictionary
        string currentGame = GameDetection.GetCurrentGame();
        if (currentGame == "GSD1" && gsd1DialogReplacements.TryGetValue(key, out string gsd1val))
            return gsd1val;
        if (currentGame == "GSD2" && gsd2DialogReplacements.TryGetValue(key, out string gsd2val))
            return gsd2val;

        if (dialogReplacements == null || dialogReplacements.Count == 0)
            return null;

        if (dialogReplacements.TryGetValue(key, out string replacement))
            return replacement;

        return null;
    }

    private static bool TryGetSpeaker(LocalizedOverrideMap dict, string key, out string value)
    {
        if (dict == null)
        {
            value = null;
            return false;
        }

        if (dict.TryGetValue(key, out value))
            return true;

        // Fallback for S1: if key is "text_gsd1_<lang>:<index>" or "text_add_<lang>:<index>", also try "message:<index>"
        if (key != null && key.Contains(":"))
        {
            int colonIndex = key.IndexOf(':');
            string prefix = key.Substring(0, colonIndex);
            string suffix = key.Substring(colonIndex); // includes the colon

            if (prefix.StartsWith("text_gsd1_", StringComparison.OrdinalIgnoreCase) || 
                prefix.StartsWith("text_add_", StringComparison.OrdinalIgnoreCase))
            {
                string fallbackKey = "message" + suffix;
                if (dict.TryGetValue(fallbackKey, out value))
                    return true;
            }
        }

        return false;
    }


    /// <summary>
    /// Get a speaker override by ID key. Routes to S1SpeakerOverrides.json or S2SpeakerOverrides.json
    /// based on the currently active game, preventing message ID collisions between the two games.
    /// When in the Main scene (launcher/shared), both dictionaries are checked (S1 first).
    /// </summary>
    public static string GetSpeakerOverride(string key)
    {
        if (GameDetection.IsMain())
        {
            // Main scene: check S1 first, then S2
            if (s1SpeakerOverrides != null && TryGetSpeaker(s1SpeakerOverrides, key, out string s1Name))
                return s1Name;
            if (s2SpeakerOverrides != null && TryGetSpeaker(s2SpeakerOverrides, key, out string s2Name))
                return s2Name;
            return null;
        }

        var dict = GameDetection.IsGSD1() ? s1SpeakerOverrides : s2SpeakerOverrides;
        if (dict == null || dict.Count == 0)
            return null;

        if (TryGetSpeaker(dict, key, out string speakerName))
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

        // if (Plugin.Config.DetailedLogs.Value)
        //     Plugin.Log.LogInfo("NPC Portrait System Ready!");

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

            // if (Plugin.Config.DetailedLogs.Value)
            // {
            //     Plugin.Log.LogInfo($"[PotraitSystem] ✓ Preloaded base portrait sprite fp_129 ({baseTexture.width}x{baseTexture.height})");
            // }
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
    public static void OpenMessageWindow_Prefix(UIMessageWindow __instance, ref Sprite faceImage, ref string name, ref string message)
    {
        ActiveMessageWindow = __instance;
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
        ActiveMessageWindow = __instance;
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

        InjectPortraitIntoWindow(__instance, characterName, expression);
    }

    /// <summary>
    /// Suikoden 1: called when the coroutine-based message window opens.
    /// Handles two independent concerns: portrait injection and speaker name injection.
    /// </summary>
    [HarmonyPatch(typeof(UIMessageWindow), nameof(UIMessageWindow.OpenMessageWindow))]
    [HarmonyPatch(new[] { typeof(Vector3) })]
    [HarmonyPostfix]
    public static void OpenMessageWindow_S1_Postfix(UIMessageWindow __instance)
    {
        ActiveMessageWindow = __instance;
        // Use LastMessageTextId (only set by GetSystemTextEx/dialogue text) rather than
        // LastTextId which can be overwritten by any UI text lookup between the message
        // text fetch and OpenMessageWindow firing.
        string textId = TextDatabasePatch.LastMessageTextId ?? TextDatabasePatch.LastTextId;

        try
        {
            Transform uiSet = __instance.transform.Find("UI_Set");
            if (uiSet == null) return;

            // ── Portrait injection ──────────────────────────────────────────────
            S1_InjectPortrait(uiSet, textId);

            // ── Speaker name injection ─────────────────────────────────────────
            // AddNameText may never be called for unnamed NPCs in S1 so we also
            // drive the name UI directly here, which runs for every dialogue open.
            S1_InjectSpeakerName(uiSet, textId);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[PotraitSystem] S1: postfix failed - {ex.Message}");
        }
    }

    /// <summary>
    /// Called in real-time when dialogue text is loaded.
    /// This handles speaker name/portrait swaps for subsequent lines in a conversation.
    /// </summary>
    public static void OnDialogueLineFetched(string textId)
    {
        if (ActiveMessageWindow == null) return;

        try
        {
            Transform uiSet = ActiveMessageWindow.transform.Find("UI_Set");
            if (uiSet == null) return;

            // Update custom portrait if mapping exists (or deactivate it)
            S1_InjectPortrait(uiSet, textId);

            // Update speaker name panel if override exists (or deactivate it)
            S1_InjectSpeakerName(uiSet, textId);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[PortraitSystem] S1: OnDialogueLineFetched failed - {ex.Message}");
        }
    }

    /// <summary>
    /// S1 portrait injection. Priority order:
    ///   1. Native portrait already active — leave it alone.
    ///   2. S1SpeakerOverrides.json mapping — uses the speaker name as the texture filename
    ///      (e.g. "Elf Village Chief" -> looks for "Elf Village Chief.png" in NPCPortraits/).
    /// </summary>
    private static void S1_InjectPortrait(Transform uiSet, string textId)
    {
        Transform facePos = uiSet.Find("All_Select/Img_BG/Command_Layout/Face_Pos");
        if (facePos == null)
        {
            Plugin.Log.LogWarning("[PotraitSystem] S1: Face_Pos not found");
            return;
        }

        // Resolve texture key from S1SpeakerOverrides.json (name part used as PNG filename)
        string textureKey = null;
        string source = null;

        string speakerData = GetSpeakerOverride(textId ?? "");
        if (!string.IsNullOrEmpty(speakerData))
        {
            textureKey = speakerData.Contains("|")
                ? speakerData.Split('|')[0].Trim()
                : speakerData;
            source = "S1SpeakerOverrides.json";
        }

        if (string.IsNullOrEmpty(textureKey))
        {
            // If WE activated the custom portrait, and there is no override for this line,
            // we must deactivate it so it doesn't bleed into the next dialogue.
            if (s_portraitActivatedByUs && facePos.gameObject.activeSelf)
            {
                facePos.gameObject.SetActive(false);
                Plugin.Log.LogInfo("[PotraitSystem] S1: Deactivated Face_Pos (no override for this dialogue)");
            }
            s_portraitActivatedByUs = false;
            return;
        }

        Plugin.Log.LogInfo($"[PotraitSystem] S1: '{textId}' -> '{textureKey}' (via {source})");

        Texture2D tex = LoadPortraitTexture(textureKey);
        if (tex == null)
        {
            Plugin.Log.LogWarning($"[PotraitSystem] S1: texture '{textureKey}' not found");
            return;
        }

        facePos.gameObject.SetActive(true);
        s_portraitActivatedByUs = true;
        Transform imgFaceTransform = facePos.Find("Img_Face");
        if (imgFaceTransform == null)
        {
            Plugin.Log.LogWarning("[PotraitSystem] S1: Img_Face not found");
            return;
        }

        imgFaceTransform.gameObject.SetActive(true);
        var imgFace = imgFaceTransform.GetComponent<UnityEngine.UI.Image>();
        if (imgFace == null) return;

        Vector2 pivot = cachedPortraitSprite != null ? cachedPortraitSprite.pivot : new Vector2(0.5f, 0.5f);
        float ppu   = cachedPortraitSprite != null ? cachedPortraitSprite.pixelsPerUnit : 100f;

        Sprite newSprite = Sprite.Create(tex,
            new Rect(0, 0, tex.width, tex.height),
            pivot, ppu, 0, SpriteMeshType.FullRect);

        UnityEngine.Object.DontDestroyOnLoad(newSprite);
        UnityEngine.Object.DontDestroyOnLoad(tex);

        imgFace.sprite = newSprite;
        Plugin.Log.LogInfo($"[PotraitSystem] S1: ✓ Injected portrait '{textureKey}'");
    }

    /// <summary>
    /// Activate Name_Set and write the speaker name into Txt_Name.
    /// Pass null/empty speakerName to hide the panel (resets between dialogues).
    /// </summary>
    private static void S1_InjectSpeakerName(Transform uiSet, string textId)
    {
        Transform nameSet = uiSet.Find("All_Select/Img_BG/Command_Layout/Text_Layout/Name_Set");
        if (nameSet == null)
        {
            Plugin.Log.LogWarning("[S1Speaker] Name_Set not found at 'All_Select/Img_BG/Command_Layout/Text_Layout/Name_Set'");
            return;
        }

        string speakerData = GetSpeakerOverride(textId ?? "");
        string displayName = null;
        if (!string.IsNullOrEmpty(speakerData))
        {
            displayName = speakerData.Contains("|")
                ? speakerData.Split('|')[0].Trim()
                : speakerData;
        }

        if (string.IsNullOrEmpty(displayName))
        {
            // Only deactivate if WE previously activated this panel.
            // Never touch a Name_Set the game activated for a native character name.
            if (s_nameSetActivatedByUs && nameSet.gameObject.activeSelf)
            {
                nameSet.gameObject.SetActive(false);
                Plugin.Log.LogInfo("[S1Speaker] Deactivated Name_Set (no override for this dialogue)");
            }
            s_nameSetActivatedByUs = false;
            return;
        }

        // Activate the container if not already active
        if (!nameSet.gameObject.activeSelf)
        {
            nameSet.gameObject.SetActive(true);
            s_nameSetActivatedByUs = true;
            Plugin.Log.LogInfo("[S1Speaker] Activated Name_Set");
        }
        else
        {
            // Already active — could be native or ours; mark as ours since we're overriding
            s_nameSetActivatedByUs = true;
        }

        // Write name to Txt_Name text component
        Transform txtNameT = nameSet.Find("Txt_Name");
        if (txtNameT == null)
        {
            Plugin.Log.LogWarning("[S1Speaker] Txt_Name not found inside Name_Set");
            return;
        }

        var tmp = txtNameT.GetComponent<TMPro.TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = displayName;
            Plugin.Log.LogInfo($"[S1Speaker] ✓ Set Txt_Name = '{displayName}' (text ID '{textId}')");
            return;
        }

        var legacyText = txtNameT.GetComponent<UnityEngine.UI.Text>();
        if (legacyText != null)
        {
            legacyText.text = displayName;
            Plugin.Log.LogInfo($"[S1Speaker] ✓ Set Txt_Name (legacy) = '{displayName}' (text ID '{textId}')");
            return;
        }

        Plugin.Log.LogWarning("[S1Speaker] No text component found on Txt_Name");
    }

    /// <summary>
    /// Suikoden 1: Intercept AddNameText to inject a speaker name for NPCs that have none.
    /// S1 builds dialogue incrementally (AddMessageText char-by-char) and calls AddNameText once
    /// with the speaker's name (or empty string for unnamed NPCs).  We look up the current text ID
    /// in SpeakerOverrides.json and override the name when the game would show nothing.
    /// If the game already provides a non-empty name we leave it untouched.
    /// </summary>
    [HarmonyPatch(typeof(UIMessageWindow), nameof(UIMessageWindow.AddNameText))]
    [HarmonyPrefix]
    public static void AddNameText_S1_Prefix(UIMessageWindow __instance, ref string name)
    {
        if (Plugin.Config.DetailedLogs.Value)
            Plugin.Log.LogInfo($"[S1Speaker] AddNameText fired — GSD1={GameDetection.IsGSD1()}, name='{name}'");

        if (!GameDetection.IsGSD1())
            return;

        string textId = TextDatabasePatch.LastMessageTextId ?? TextDatabasePatch.LastTextId;
        if (!string.IsNullOrEmpty(textId))
        {
            string speakerData = GetSpeakerOverride(textId);
            if (!string.IsNullOrEmpty(speakerData))
            {
                string displayName = speakerData.Contains("|")
                    ? speakerData.Split('|')[0].Trim()
                    : speakerData;
                name = displayName;
                s_nameSetActivatedByUs = true;
                Plugin.Log.LogInfo($"[S1Speaker] AddNameText: overriding name with '{displayName}' (text ID '{textId}')");
                return;
            }
        }

        // If the game already assigned a speaker name, mark the panel as game-owned and leave it
        if (!string.IsNullOrEmpty(name))
        {
            s_nameSetActivatedByUs = false; // game owns Name_Set for this dialogue
            if (Plugin.Config.DetailedLogs.Value)
                Plugin.Log.LogInfo($"[S1Speaker] AddNameText: existing name '{name}', leaving untouched");
            return;
        }

    }

    /// <summary>
    /// Postfix: directly drive the S1 name UI after the native AddNameText call.
    /// - When a name is present: activates Name_Set and writes the text to Txt_Name.
    /// - When name is empty: deactivates Name_Set so it resets between dialogues.
    /// </summary>
    [HarmonyPatch(typeof(UIMessageWindow), nameof(UIMessageWindow.AddNameText))]
    [HarmonyPostfix]
    public static void AddNameText_S1_Postfix(UIMessageWindow __instance, string name)
    {
        if (!GameDetection.IsGSD1())
            return;

        try
        {
            Transform uiSet = __instance.transform.Find("UI_Set");
            if (uiSet == null) return;

            Transform nameSet = uiSet.Find("All_Select/Img_BG/Command_Layout/Text_Layout/Name_Set");
            if (nameSet == null)
            {
                Plugin.Log.LogWarning("[S1Speaker] Name_Set not found at expected path");
                return;
            }

            // No speaker — hide the panel so it doesn't bleed into the next dialogue
            if (string.IsNullOrEmpty(name))
            {
                if (nameSet.gameObject.activeSelf)
                    nameSet.gameObject.SetActive(false);
                return;
            }

            // Activate the container
            if (!nameSet.gameObject.activeSelf)
                nameSet.gameObject.SetActive(true);

            // Set the text directly on Txt_Name
            Transform txtNameTransform = nameSet.Find("Txt_Name");
            if (txtNameTransform == null)
            {
                Plugin.Log.LogWarning("[S1Speaker] Txt_Name not found inside Name_Set");
                return;
            }

            var tmp = txtNameTransform.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = name;
                Plugin.Log.LogInfo($"[S1Speaker] ✓ Set Txt_Name = '{name}'");
                return;
            }

            // Fallback: legacy Unity Text component
            var legacyText = txtNameTransform.GetComponent<UnityEngine.UI.Text>();
            if (legacyText != null)
            {
                legacyText.text = name;
                Plugin.Log.LogInfo($"[S1Speaker] ✓ Set Txt_Name (legacy Text) = '{name}'");
                return;
            }

            Plugin.Log.LogWarning("[S1Speaker] No text component found on Txt_Name");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[S1Speaker] Postfix error: {ex.Message}");
        }
    }


    private static void InjectPortraitIntoWindow(UIMessageWindow __instance, string characterName, string expression = null)
    {
        Plugin.Log.LogInfo($"[PotraitSystem] Attempting to inject portrait for '{characterName}'{(expression != null ? $" ({expression})" : "")} directly into Img_Face");

        try
        {
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

            Plugin.Log.LogInfo($"[PotraitSystem] ✓✓✓ Successfully injected portrait into Img_Face for '{characterName}'!");
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

        // S1: resolve portrait texture key from S1SpeakerOverrides.json (name part used as PNG filename)
        string textId = TextDatabasePatch.LastMessageTextId ?? TextDatabasePatch.LastTextId;
        string s1SpeakerData = GetSpeakerOverride(textId);
        string s1MappedPortrait = !string.IsNullOrEmpty(s1SpeakerData)
            ? (s1SpeakerData.Contains("|") ? s1SpeakerData.Split('|')[0].Trim() : s1SpeakerData)
            : null;

        if (!string.IsNullOrEmpty(s1MappedPortrait))
        {
            Plugin.Log.LogInfo($"[PotraitSystem] S1 Matched TextId '{textId}' to portrait '{s1MappedPortrait}'");

            Texture2D customTexture = LoadPortraitTexture(s1MappedPortrait);
            if (customTexture != null)
            {
                Vector2 spritePivot = cachedPortraitSprite != null ? cachedPortraitSprite.pivot : new Vector2(0.5f, 0.5f);
                float pixelsPerUnit = cachedPortraitSprite != null ? cachedPortraitSprite.pixelsPerUnit : 100f;

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

                sprite = newSprite;
                Plugin.Log.LogInfo($"[PotraitSystem] ✓ Injected mapped portrait '{s1MappedPortrait}' directly to sprite parameter");

                // Face_Pos may still be inactive because OpenMessageWindow fired before LastTextId was set.
                // Activate it now so the portrait is actually visible.
                try
                {
                    Transform uiSet2 = __instance.transform.Find("UI_Set");
                    Transform facePos2 = uiSet2?.Find("All_Select/Img_BG/Command_Layout/Face_Pos");
                    if (facePos2 != null && !facePos2.gameObject.activeSelf)
                    {
                        facePos2.gameObject.SetActive(true);
                        Transform imgFace2 = facePos2.Find("Img_Face");
                        if (imgFace2 != null) imgFace2.gameObject.SetActive(true);
                        Plugin.Log.LogInfo("[PotraitSystem] S1: Activated Face_Pos via SetCharacterFace");
                    }
                }
                catch (Exception faceEx)
                {
                    Plugin.Log.LogWarning($"[PotraitSystem] S1: Could not activate Face_Pos: {faceEx.Message}");
                }

                return;
            }
            else
            {
                // Fallback direct load
                Sprite nativeSprite = UnityEngine.Resources.Load<Sprite>(s1MappedPortrait);
                if (nativeSprite != null)
                {
                    sprite = nativeSprite;
                    Plugin.Log.LogInfo($"[PotraitSystem] ✓ Injected NATIVE mapped portrait '{s1MappedPortrait}'");
                    return;
                }
            }
        }

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
