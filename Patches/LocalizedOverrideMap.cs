using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using UnityEngine;

namespace PKCore.Patches;

/// <summary>
/// Thread-safe localized text map supporting language tag wrappers ("zh-TW", "en", "ja", "fr", "de", "es", "it", "ko"),
/// top-level language sections, per-entry language dictionaries, and flat string fallbacks.
/// </summary>
public class LocalizedOverrideMap
{
    private static readonly HashSet<string> LanguageTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "zh-TW", "zh-Hant", "zh-CN", "zh-Hans", "zh",
        "en", "en-US", "en-GB", "english",
        "ja", "ja-JP", "japanese",
        "fr", "fr-FR", "french",
        "de", "de-DE", "german",
        "es", "es-ES", "spanish",
        "it", "it-IT", "italian",
        "ko", "ko-KR", "korean"
    };

    private readonly Dictionary<string, Dictionary<string, string>> _map = new(StringComparer.OrdinalIgnoreCase);

    public void Clear()
    {
        _map.Clear();
    }

    public int Count => _map.Count;

    public static string GetActiveLanguageTag()
    {
        return LanguageSelectionPatch.ActiveLanguageCode;
    }

    public static string NormalizeTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return "default";
        string t = tag.Trim().Replace('_', '-');

        if (string.Equals(t, "english", StringComparison.OrdinalIgnoreCase) || string.Equals(t, "en-US", StringComparison.OrdinalIgnoreCase) || string.Equals(t, "en-GB", StringComparison.OrdinalIgnoreCase))
            return "en";
        if (string.Equals(t, "spanish", StringComparison.OrdinalIgnoreCase) || string.Equals(t, "es-ES", StringComparison.OrdinalIgnoreCase))
            return "es";
        if (string.Equals(t, "french", StringComparison.OrdinalIgnoreCase) || string.Equals(t, "fr-FR", StringComparison.OrdinalIgnoreCase))
            return "fr";
        if (string.Equals(t, "german", StringComparison.OrdinalIgnoreCase) || string.Equals(t, "de-DE", StringComparison.OrdinalIgnoreCase))
            return "de";
        if (string.Equals(t, "italian", StringComparison.OrdinalIgnoreCase) || string.Equals(t, "it-IT", StringComparison.OrdinalIgnoreCase))
            return "it";
        if (string.Equals(t, "japanese", StringComparison.OrdinalIgnoreCase) || string.Equals(t, "ja-JP", StringComparison.OrdinalIgnoreCase))
            return "ja";
        if (string.Equals(t, "korean", StringComparison.OrdinalIgnoreCase) || string.Equals(t, "ko-KR", StringComparison.OrdinalIgnoreCase))
            return "ko";
        if (string.Equals(t, "zh-Hant", StringComparison.OrdinalIgnoreCase) || string.Equals(t, "traditional chinese", StringComparison.OrdinalIgnoreCase) || string.Equals(t, "traditionalchinese", StringComparison.OrdinalIgnoreCase))
            return "zh-TW";
        if (string.Equals(t, "zh-Hans", StringComparison.OrdinalIgnoreCase) || string.Equals(t, "simplified chinese", StringComparison.OrdinalIgnoreCase) || string.Equals(t, "simplifiedchinese", StringComparison.OrdinalIgnoreCase))
            return "zh-CN";

        return t;
    }

    public void AddOrUpdate(string key, string langTag, string value)
    {
        if (string.IsNullOrEmpty(key) || value == null) return;
        string normTag = NormalizeTag(langTag);

        if (!_map.TryGetValue(key, out var langDict))
        {
            langDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _map[key] = langDict;
        }

        langDict[normTag] = value;
    }

    public bool TryGetValue(string key, out string result)
    {
        result = null;
        if (string.IsNullOrEmpty(key) || !_map.TryGetValue(key, out var langDict))
            return false;

        string activeLang = NormalizeTag(GetActiveLanguageTag());

        // Extract language embedded directly in text ID prefix (e.g., "text_gsd1_es:100" -> "es")
        if (key.Contains(":"))
        {
            int colonIndex = key.IndexOf(':');
            string prefix = key.Substring(0, colonIndex);
            if (prefix.StartsWith("text_gsd1_", StringComparison.OrdinalIgnoreCase))
            {
                activeLang = NormalizeTag(prefix.Substring("text_gsd1_".Length));
            }
            else if (prefix.StartsWith("text_add_", StringComparison.OrdinalIgnoreCase))
            {
                activeLang = NormalizeTag(prefix.Substring("text_add_".Length));
            }
        }

        // 1. Match active language tag (e.g. "zh-TW", "es", "en")
        if (!string.IsNullOrEmpty(activeLang) && !string.Equals(activeLang, "unknown", StringComparison.OrdinalIgnoreCase))
        {
            if (langDict.TryGetValue(activeLang, out result) && !string.IsNullOrEmpty(result))
                return true;

            // 2. Language family fallbacks (e.g. "zh-TW" -> "zh")
            int dashIdx = activeLang.IndexOf('-');
            if (dashIdx > 0)
            {
                string baseLang = activeLang.Substring(0, dashIdx);
                if (langDict.TryGetValue(baseLang, out result) && !string.IsNullOrEmpty(result))
                    return true;
            }
        }

        // 3. Flat/untagged override fallback ("default")
        if (langDict.TryGetValue("default", out result) && !string.IsNullOrEmpty(result))
            return true;

        // Strict language scoping: Do NOT leak unmatched language-tagged overrides!
        return false;
    }

    public void LoadFromFile(string filePath, string logLabel)
    {
        if (!File.Exists(filePath)) return;

        try
        {
            string json = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(json)) return;

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return;

            int entryCount = 0;
            foreach (var rootKvp in doc.RootElement.EnumerateObject())
            {
                string name = rootKvp.Name;
                var val = rootKvp.Value;

                if (val.ValueKind == JsonValueKind.Object)
                {
                    if (LanguageTags.Contains(name) || LanguageTags.Contains(NormalizeTag(name)))
                    {
                        // Top-level language section wrapper: "zh-TW": { "key": "val" }
                        foreach (var subKvp in val.EnumerateObject())
                        {
                            if (subKvp.Value.ValueKind == JsonValueKind.String)
                            {
                                AddOrUpdate(subKvp.Name, name, subKvp.Value.GetString());
                                entryCount++;
                            }
                        }
                    }
                    else
                    {
                        // Per-entry language wrapper: "key": { "zh-TW": "val", "en": "val" }
                        foreach (var subKvp in val.EnumerateObject())
                        {
                            if (subKvp.Value.ValueKind == JsonValueKind.String)
                            {
                                AddOrUpdate(name, subKvp.Name, subKvp.Value.GetString());
                                entryCount++;
                            }
                        }
                    }
                }
                else if (val.ValueKind == JsonValueKind.String)
                {
                    // Standard flat format: "key": "val"
                    AddOrUpdate(name, "default", val.GetString());
                    entryCount++;
                }
            }

            Plugin.Log.LogInfo($"[TextOverride] [{logLabel}] Loaded {entryCount} entries from {Path.GetFileName(filePath)}");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[TextOverride] [{logLabel}] Failed to parse {Path.GetFileName(filePath)}: {ex.Message}");
        }
    }
}
