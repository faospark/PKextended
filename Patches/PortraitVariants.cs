using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Unity.IL2CPP;

namespace PKCore.Patches;

/// <summary>
/// Manages portrait variant system - character name mappings and expression variants.
/// Works independently and can be used for any portrait system.
/// Searches for portrait files across all portrait directories (shared, GSD1, GSD2).
/// </summary>
public static class PortraitVariants
{
    // Character name → Portrait filename mapping (e.g., "Luc" → "fp_053")
    private static Dictionary<string, string> portraitMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    // Portrait filename → Expression variants (e.g., "fp_053" → {"angry": "fp_053_angry.png"})
    private static Dictionary<string, Dictionary<string, string>> portraitVariants = new Dictionary<string, Dictionary<string, string>>();

    private static string configDir;
    private static string portraitMappingsPath;
    private static string portraitVariantsPath;

    // All portrait directories to search (in priority order)
    private static List<string> portraitDirectories = new List<string>();

    private static bool isInitialized = false;

    /// <summary>
    /// Initialize the variant portrait system
    /// </summary>
    public static void Initialize()
    {
        if (isInitialized)
            return;

        configDir = Path.Combine(BepInEx.Paths.GameRootPath, "PKCore", "Config");
        Directory.CreateDirectory(configDir);

        portraitMappingsPath = Path.Combine(configDir, "PortraitMappings.json");
        portraitVariantsPath = Path.Combine(configDir, "PortraitVariants.json");

        // Discover all portrait directories under Textures (searches recursively)
        string texturesPath = Path.Combine(BepInEx.Paths.GameRootPath, "PKCore", "Textures");

        portraitDirectories.Clear();

        // Add all subdirectories under Textures/ that could contain portraits
        // Priority: GSD1 folders first, then GSD2, then root-level folders
        if (Directory.Exists(texturesPath))
        {
            // GSD1 folders (highest priority)
            string gsd1Path = Path.Combine(texturesPath, "GSD1");
            if (Directory.Exists(gsd1Path))
            {
                foreach (var dir in Directory.GetDirectories(gsd1Path, "*", SearchOption.AllDirectories))
                {
                    portraitDirectories.Add(dir);
                }
            }

            // GSD2 folders (medium priority)
            string gsd2Path = Path.Combine(texturesPath, "GSD2");
            if (Directory.Exists(gsd2Path))
            {
                foreach (var dir in Directory.GetDirectories(gsd2Path, "*", SearchOption.AllDirectories))
                {
                    portraitDirectories.Add(dir);
                }
            }

            // Root-level folders (lowest priority)
            foreach (var dir in Directory.GetDirectories(texturesPath, "*", SearchOption.TopDirectoryOnly))
            {
                if (!dir.EndsWith("GSD1") && !dir.EndsWith("GSD2"))
                {
                    portraitDirectories.Add(dir);
                    // Also add subdirectories
                    foreach (var subdir in Directory.GetDirectories(dir, "*", SearchOption.AllDirectories))
                    {
                        portraitDirectories.Add(subdir);
                    }
                }
            }

            // Also add Textures root itself
            portraitDirectories.Add(texturesPath);
        }

        LoadPortraitMappings();
        LoadPortraitVariants();

        isInitialized = true;
        // Plugin.Log.LogInfo($"[PortraitVariants] System initialized - searching {portraitDirectories.Count} directories");
    }

    /// <summary>
    /// Load character name → portrait filename mappings
    /// </summary>
    private static void LoadPortraitMappings()
    {
        if (!File.Exists(portraitMappingsPath))
        {
            CreateMappingsTemplate();
            return;
        }

        try
        {
            var loaded = AssetLoader.LoadJsonAsync<Dictionary<string, string>>(portraitMappingsPath).Result;
            if (loaded != null)
            {
                portraitMappings = new Dictionary<string, string>(loaded, StringComparer.OrdinalIgnoreCase);
                // Plugin.Log.LogInfo($"[PortraitVariants] Loaded {portraitMappings.Count} portrait mappings");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[PortraitVariants] Error loading portrait mappings: {ex.Message}");
        }
    }

    /// <summary>
    /// Load portrait filename → expression variants mappings
    /// </summary>
    private static void LoadPortraitVariants()
    {
        if (!File.Exists(portraitVariantsPath))
        {
            CreateVariantsTemplate();
            return;
        }

        try
        {
            var loaded = AssetLoader.LoadJsonAsync<Dictionary<string, Dictionary<string, string>>>(portraitVariantsPath).Result;
            if (loaded != null)
            {
                portraitVariants = loaded;
                // Plugin.Log.LogInfo($"[PortraitVariants] Loaded {portraitVariants.Count} portrait variant sets");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[PortraitVariants] Error loading portrait variants: {ex.Message}");
        }
    }

    /// <summary>
    /// Create template PortraitMappings.json file
    /// </summary>
    private static void CreateMappingsTemplate()
    {
        var template = new Dictionary<string, string>
        {
            { "Luc", "fp_053" },
            { "Viktor", "fp_001" },
            { "Flik", "fp_002" },
            { "Nanami", "fp_019" }
        };

        try
        {
            var options = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            File.WriteAllText(portraitMappingsPath, System.Text.Json.JsonSerializer.Serialize(template, options));
            Plugin.Log.LogInfo($"[PortraitVariants] Created template: {portraitMappingsPath}");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[PortraitVariants] Error creating mappings template: {ex.Message}");
        }
    }

    /// <summary>
    /// Create template PortraitVariants.json file
    /// </summary>
    private static void CreateVariantsTemplate()
    {
        var template = new Dictionary<string, Dictionary<string, string>>
        {
            {
                "fp_053",
                new Dictionary<string, string>
                {
                    { "angry", "fp_053_angry.png" },
                    { "sad", "fp_053_sad.png" },
                    { "happy", "fp_053_happy.png" }
                }
            },
            {
                "fp_001",
                new Dictionary<string, string>
                {
                    { "determined", "fp_001_determined.png" },
                    { "worried", "fp_001_worried.png" }
                }
            }
        };

        try
        {
            var options = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            File.WriteAllText(portraitVariantsPath, System.Text.Json.JsonSerializer.Serialize(template, options));
            Plugin.Log.LogInfo($"[PortraitVariants] Created template: {portraitVariantsPath}");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[PortraitVariants] Error creating variants template: {ex.Message}");
        }
    }

    /// <summary>
    /// Convert character name to portrait filename using mappings
    /// Falls back to using the name itself if no mapping exists
    /// </summary>
    public static string GetPortraitFilename(string characterName)
    {
        if (string.IsNullOrEmpty(characterName))
            return null;

        if (portraitMappings.TryGetValue(characterName, out var mappedFile))
            return mappedFile;

        // Fallback: use character name as filename (backwards compatibility)
        return characterName;
    }

    /// <summary>
    /// Get variant filename for a portrait and expression
    /// Returns null if variant doesn't exist
    /// </summary>
    public static string GetVariantFilename(string portraitFile, string expression)
    {
        if (string.IsNullOrEmpty(portraitFile) || string.IsNullOrEmpty(expression))
            return null;

        if (portraitVariants.TryGetValue(portraitFile, out var variants) &&
            variants.TryGetValue(expression, out var variantFileName))
        {
            return variantFileName;
        }

        return null;
    }

    /// <summary>
    /// Find portrait file path across all portrait directories
    /// Searches recursively in ALL folders under PKCore/Textures/
    /// Priority: GSD1 folders > GSD2 folders > Root folders
    /// </summary>
    public static string FindPortraitPath(string filename)
    {
        if (string.IsNullOrEmpty(filename))
            return null;

        // Ensure .png extension
        if (!filename.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            filename += ".png";

        // Search all directories in priority order
        foreach (var dir in portraitDirectories)
        {
            if (!Directory.Exists(dir))
                continue;

            string fullPath = Path.Combine(dir, filename);
            if (File.Exists(fullPath))
            {
                if (Plugin.Config.DetailedLogs.Value)
                {
                    string relativePath = fullPath.Replace(Path.Combine(BepInEx.Paths.GameRootPath, "PKCore", "Textures"), "Textures");
                    Plugin.Log.LogInfo($"[PortraitVariants] Found: {relativePath}");
                }
                return fullPath;
            }
        }

        return null;
    }

    /// <summary>
    /// Get portrait path with full variant support
    /// Handles character name mapping, expression variants, and directory searching
    /// </summary>
    /// <param name="characterName">Character name (will be mapped to portrait filename)</param>
    /// <param name="expression">Optional expression variant (e.g., "angry", "sad")</param>
    /// <returns>Full file path to portrait, or null if not found</returns>
    public static string GetPortraitPath(string characterName, string expression = null)
    {
        if (string.IsNullOrEmpty(characterName))
            return null;

        // Step 1: Convert character name to portrait filename
        string portraitFile = GetPortraitFilename(characterName);
        if (portraitFile == null)
            return null;

        // Step 2: Try variant first if expression specified
        if (!string.IsNullOrEmpty(expression))
        {
            string variantFileName = GetVariantFilename(portraitFile, expression);
            if (variantFileName != null)
            {
                string variantPath = FindPortraitPath(variantFileName);
                if (variantPath != null)
                {
                    if (Plugin.Config.DetailedLogs.Value)
                        Plugin.Log.LogInfo($"[PortraitVariants] Using variant: {characterName} ({expression}) -> {variantFileName}");
                    return variantPath;
                }
                else if (Plugin.Config.DetailedLogs.Value)
                {
                    Plugin.Log.LogWarning($"[PortraitVariants] Variant not found: {variantFileName}");
                }
            }
        }

        // Step 3: Fall back to default portrait
        string defaultPath = FindPortraitPath(portraitFile);
        if (defaultPath != null)
        {
            if (Plugin.Config.DetailedLogs.Value)
                Plugin.Log.LogInfo($"[PortraitVariants] Using default: {characterName} -> {portraitFile}");
            return defaultPath;
        }

        // FALLBACK: If default is missing (e.g. vanilla asset), try to find fp_219 (Question Mark) from Textures
        // This handles cases where user defined a variant but the file is missing
        string fallbackPath = FindPortraitPath("fp_219");
        if (fallbackPath != null)
        {
            if (Plugin.Config.DetailedLogs.Value)
                Plugin.Log.LogWarning($"[PortraitVariants] Portrait file missing for {characterName}, falling back to fp_219 (Question Mark)");
            return fallbackPath;
        }

        if (Plugin.Config.DetailedLogs.Value)
            Plugin.Log.LogWarning($"[PortraitVariants] Portrait not found and fallback failed: {characterName} (mapped to {portraitFile})");

        return null;
    }

    /// <summary>
    /// Parse speaker string with optional expression
    /// Format: "CharacterName" or "CharacterName|expression"
    /// </summary>
    public static (string characterName, string expression) ParseSpeakerString(string speakerData)
    {
        if (string.IsNullOrEmpty(speakerData))
            return (null, null);

        // Check if format includes expression: "SpeakerName|expression"
        if (speakerData.Contains("|"))
        {
            var parts = speakerData.Split('|');
            return (parts[0].Trim(), parts[1].Trim());
        }

        return (speakerData, null);
    }

    /// <summary>
    /// Reload configuration files (useful after editing JSON files)
    /// </summary>
    public static void Reload()
    {
        Plugin.Log.LogInfo("[PortraitVariants] Reloading configuration...");
        portraitMappings.Clear();
        portraitVariants.Clear();
        LoadPortraitMappings();
        LoadPortraitVariants();
    }
}
