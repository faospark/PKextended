using System;
using System.Collections.Generic;
using HarmonyLib;
using Share.UI.Panel;

namespace PKCore.Patches;

/// <summary>
/// Patch for TitlePanel and TextMasterData to detect active launcher language on startup and on manual change.
/// </summary>
public static class LanguageSelectionPatch
{
    /// <summary>
    /// Event fired when the active language is detected at startup or changed.
    /// Parameters: (languageCode, languageName)
    /// </summary>
    public static event Action<string, string> OnLanguageDetected;

    /// <summary>
    /// Event fired whenever the user manually selects a language button/index.
    /// </summary>
    public static event Action<int> OnLanguageSelected;

    /// <summary>
    /// Detected language code ("en", "zh-TW", "zh-CN", "ja", "fr", "de", "es", "it", "ko", or "unknown")
    /// </summary>
    public static string ActiveLanguageCode { get; private set; } = "unknown";

    /// <summary>
    /// Human-readable detected language name
    /// </summary>
    public static string ActiveLanguageName { get; private set; } = "Unknown";

    /// <summary>
    /// Most recently selected language button index (-1 if none selected yet)
    /// </summary>
    public static int LastSelectedLanguageIndex { get; private set; } = -1;

    private static bool _hasLoggedInitialLanguage = false;
    private static bool _isProbing = false;

    /// <summary>
    /// Known language tags recognized as delimiters or wrapper sections in override JSON files.
    /// </summary>
    public static readonly HashSet<string> RecognizedLanguageTags = new(StringComparer.OrdinalIgnoreCase)
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

    public static string NormalizeLanguageTag(string tag)
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

    public static bool IsRecognizedLanguageTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return false;
        string norm = NormalizeLanguageTag(tag);
        return RecognizedLanguageTags.Contains(tag) || RecognizedLanguageTags.Contains(norm);
    }

    /// <summary>
    /// Probes the game's active language immediately if it has not been detected yet.
    /// </summary>
    public static void EnsureLanguageDetected()
    {
        if (ActiveLanguageCode != "unknown" || _isProbing) return;
        _isProbing = true;
        try
        {
            // Probe known title/settings text IDs directly via TextMasterData
            string testText = TextMasterData.GetSystemText("add_message", 495);
            if (!string.IsNullOrEmpty(testText))
            {
                DetectLanguageFromText(testText, "TextMasterData Early Probe [add_message:495]");
            }

            if (ActiveLanguageCode == "unknown")
            {
                string fallbackText = TextMasterData.GetSystemText("add_message", 508);
                if (!string.IsNullOrEmpty(fallbackText))
                {
                    DetectLanguageFromText(fallbackText, "TextMasterData Early Probe [add_message:508]");
                }
            }

            if (ActiveLanguageCode == "unknown")
            {
                string galleryText = TextMasterData.GetSystemText("add_message", 507);
                if (!string.IsNullOrEmpty(galleryText))
                {
                    DetectLanguageFromText(galleryText, "TextMasterData Early Probe [add_message:507]");
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[LanguageSelection] Language probe failed: {ex.Message}");
        }
        finally
        {
            _isProbing = false;
        }
    }

    /// <summary>
    /// Intercept TextMasterData.GetSystemText to detect language from launcher system text entries
    /// </summary>
    [HarmonyPatch(typeof(TextMasterData), nameof(TextMasterData.GetSystemText))]
    [HarmonyPostfix]
    public static void GetSystemText_Postfix(string id, int index, ref string __result)
    {
        if (string.IsNullOrEmpty(__result) || _isProbing) return;
        DetectLanguageFromText(__result, $"TextMasterData [{id}:{index}]");
    }

    /// <summary>
    /// Intercept TitlePanel.SetLocalizeText when launcher UI initializes text strings
    /// </summary>
    [HarmonyPatch(typeof(TitlePanel), nameof(TitlePanel.SetLocalizeText))]
    [HarmonyPostfix]
    public static void SetLocalizeText_Postfix(TitlePanel __instance)
    {
        if (__instance == null || _isProbing) return;

        try
        {
            if (__instance.langText != null && !string.IsNullOrEmpty(__instance.langText.text))
            {
                DetectLanguageFromText(__instance.langText.text, "TitlePanel.langText");
            }
            else if (__instance.langSelText != null && !string.IsNullOrEmpty(__instance.langSelText.text))
            {
                DetectLanguageFromText(__instance.langSelText.text, "TitlePanel.langSelText");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[LanguageSelection] Error inspecting TitlePanel localized text: {ex.Message}");
        }
    }

    [HarmonyPatch(typeof(TitlePanel), nameof(TitlePanel.SelectLanguageButton))]
    [HarmonyPostfix]
    public static void SelectLanguageButton_Postfix(int selectNo)
    {
        LastSelectedLanguageIndex = selectNo;
        Plugin.Log.LogInfo($"[LanguageSelection] User selected language button index: {selectNo}");

        // Map button index to language code:
        // 0: English, 1: French, 2: German, 3: Spanish, 4: Italian, 5: Japanese, 6: Traditional Chinese, 7: Simplified Chinese, 8: Korean
        string code = selectNo switch
        {
            0 => "en",
            1 => "fr",
            2 => "de",
            3 => "es",
            4 => "it",
            5 => "ja",
            6 => "zh-TW",
            7 => "zh-CN",
            8 => "ko",
            _ => "unknown"
        };

        if (code != "unknown")
        {
            ActiveLanguageCode = code;
            Plugin.Log.LogInfo($"[LanguageSelection] Language code updated via SelectLanguageButton({selectNo}): {ActiveLanguageCode}");
        }

        OnLanguageSelected?.Invoke(selectNo);
    }

    [HarmonyPatch(typeof(TitlePanel), nameof(TitlePanel.SelectLanguageName))]
    [HarmonyPostfix]
    public static void SelectLanguageName_Postfix(byte selectNo)
    {
        LastSelectedLanguageIndex = selectNo;
        Plugin.Log.LogInfo($"[LanguageSelection] User confirmed language name byte: {selectNo}");

        string code = selectNo switch
        {
            0 => "en",
            1 => "fr",
            2 => "de",
            3 => "es",
            4 => "it",
            5 => "ja",
            6 => "zh-TW",
            7 => "zh-CN",
            8 => "ko",
            _ => "unknown"
        };

        if (code != "unknown")
        {
            ActiveLanguageCode = code;
            Plugin.Log.LogInfo($"[LanguageSelection] Language code updated via SelectLanguageName({selectNo}): {ActiveLanguageCode}");
        }

        OnLanguageSelected?.Invoke(selectNo);
    }

    [HarmonyPatch(typeof(TitlePanel), nameof(TitlePanel.ShowScreen))]
    [HarmonyPostfix]
    public static void ShowScreen_Postfix(TitlePanel.ScreenType screenType)
    {
        if (screenType == TitlePanel.ScreenType.LangageSelect)
        {
            Plugin.Log.LogInfo("[LanguageSelection] Language selection screen opened.");
        }
    }

    private static void DetectLanguageFromText(string text, string sourceLabel)
    {
        if (string.IsNullOrEmpty(text)) return;

        string newCode = "unknown";
        string newName = "Unknown";

        if (text.Contains("語言") || text.Contains("繁體") || text.Contains("畫廊"))
        {
            newCode = "zh-TW";
            newName = "Traditional Chinese";
        }
        else if (text.Contains("语言") || text.Contains("简体") || text.Contains("画廊"))
        {
            newCode = "zh-CN";
            newName = "Simplified Chinese";
        }
        else if (text.Contains("言語") || text.Contains("日本語") || text.Contains("ギャラリー"))
        {
            newCode = "ja";
            newName = "Japanese";
        }
        else if (text.Contains("Sprachauswahl") || text.Contains("Einstellungen") || text.Contains("Galerie") || text.Contains("Sprache") || text.Contains("Deutsch"))
        {
            newCode = "de";
            newName = "German";
        }
        else if (text.Contains("Idioma") || text.Contains("Ajustes") || text.Contains("Galería") || text.Contains("Español") || text.Contains("Confirma el idioma"))
        {
            newCode = "es";
            newName = "Spanish";
        }
        else if (text.Contains("Langue") || text.Contains("Paramètres") || text.Contains("Français") || text.Contains("Confirmez la langue"))
        {
            newCode = "fr";
            newName = "French";
        }
        else if (text.Contains("Lingua") || text.Contains("Impostazioni") || text.Contains("Galleria") || text.Contains("Italiano") || text.Contains("Conferma la lingua"))
        {
            newCode = "it";
            newName = "Italian";
        }
        else if (text.Contains("언어") || text.Contains("한국어") || text.Contains("갤러리") || text.Contains("설정"))
        {
            newCode = "ko";
            newName = "Korean";
        }
        else if (text.Contains("Language Settings") || text.Contains("English") || text.Contains("Settings") || text.Contains("Gallery") || text.Contains("Game Options"))
        {
            newCode = "en";
            newName = "English";
        }

        if (newCode != "unknown" && (!string.Equals(ActiveLanguageCode, newCode, StringComparison.OrdinalIgnoreCase) || !_hasLoggedInitialLanguage))
        {
            ActiveLanguageCode = newCode;
            ActiveLanguageName = newName;
            _hasLoggedInitialLanguage = true;

            Plugin.Log.LogInfo($"[LanguageSelection] Active launcher language detected via {sourceLabel}: {ActiveLanguageName} ({ActiveLanguageCode}) [Text: \"{text}\"]");
            OnLanguageDetected?.Invoke(ActiveLanguageCode, ActiveLanguageName);
        }
    }
}
