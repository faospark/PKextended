using System;
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

    /// <summary>
    /// Intercept TextMasterData.GetSystemText to detect language from launcher system text entries (e.g. add_message:495 Language Settings)
    /// </summary>
    [HarmonyPatch(typeof(TextMasterData), nameof(TextMasterData.GetSystemText))]
    [HarmonyPostfix]
    public static void GetSystemText_Postfix(string id, int index, ref string __result)
    {
        if (string.IsNullOrEmpty(__result)) return;

        // "add_message:495" is the "Language Settings" title text queried by TitlePanel on launcher load
        if (id != null && id.Equals("add_message", StringComparison.OrdinalIgnoreCase) && index == 495)
        {
            DetectLanguageFromText(__result, "TextMasterData [add_message:495]");
        }
        // Fallback check on "add_message:508" ("Settings")
        else if (id != null && id.Equals("add_message", StringComparison.OrdinalIgnoreCase) && index == 508 && ActiveLanguageCode == "unknown")
        {
            DetectLanguageFromText(__result, "TextMasterData [add_message:508]");
        }
    }

    /// <summary>
    /// Intercept TitlePanel.SetLocalizeText when launcher UI initializes text strings
    /// </summary>
    [HarmonyPatch(typeof(TitlePanel), nameof(TitlePanel.SetLocalizeText))]
    [HarmonyPostfix]
    public static void SetLocalizeText_Postfix(TitlePanel __instance)
    {
        if (__instance == null) return;

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
        OnLanguageSelected?.Invoke(selectNo);
    }

    [HarmonyPatch(typeof(TitlePanel), nameof(TitlePanel.SelectLanguageName))]
    [HarmonyPostfix]
    public static void SelectLanguageName_Postfix(byte selectNo)
    {
        LastSelectedLanguageIndex = selectNo;
        Plugin.Log.LogInfo($"[LanguageSelection] User confirmed language name byte: {selectNo}");
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
        string newCode = "unknown";
        string newName = "Unknown";

        if (text.Contains("語言") || text.Contains("繁體"))
        {
            newCode = "zh-TW";
            newName = "Traditional Chinese";
        }
        else if (text.Contains("语言") || text.Contains("简体"))
        {
            newCode = "zh-CN";
            newName = "Simplified Chinese";
        }
        else if (text.Contains("言語") || text.Contains("日本語"))
        {
            newCode = "ja";
            newName = "Japanese";
        }
        else if (text.Contains("Language") || text.Contains("English"))
        {
            newCode = "en";
            newName = "English";
        }
        else if (text.Contains("Langue"))
        {
            newCode = "fr";
            newName = "French";
        }
        else if (text.Contains("Sprache"))
        {
            newCode = "de";
            newName = "German";
        }
        else if (text.Contains("Idioma"))
        {
            newCode = "es";
            newName = "Spanish";
        }
        else if (text.Contains("Lingua"))
        {
            newCode = "it";
            newName = "Italian";
        }
        else if (text.Contains("언어"))
        {
            newCode = "ko";
            newName = "Korean";
        }

        if (newCode != "unknown" && (ActiveLanguageCode != newCode || !_hasLoggedInitialLanguage))
        {
            ActiveLanguageCode = newCode;
            ActiveLanguageName = newName;
            _hasLoggedInitialLanguage = true;

            Plugin.Log.LogInfo($"[LanguageSelection] Active launcher language detected via {sourceLabel}: {ActiveLanguageName} ({ActiveLanguageCode}) [Text: \"{text}\"]");
            OnLanguageDetected?.Invoke(ActiveLanguageCode, ActiveLanguageName);
        }
    }
}
