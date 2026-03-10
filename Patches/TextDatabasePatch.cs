using HarmonyLib;
using PKCore.Patches;
using System;
using System.Collections.Generic;

namespace PKCore.Patches;

[HarmonyPatch]
public class TextDatabasePatch
{
    // Track logged text IDs to prevent duplicate logging
    private static readonly HashSet<string> loggedTextIDs = new HashSet<string>();

    // Store the last queried text ID for portrait injection
    public static string LastTextId { get; private set; }

    // Only updated by GetSystemTextEx (dialogue/message text in S1).
    // Isolated from GetSystemText which handles UI elements — avoids timing
    // issues where a UI lookup overwrites the ID before OpenMessageWindow fires.
    public static string LastMessageTextId { get; private set; }

    // Patch GetSystemText to intercept ID-based lookups
    // Using Priority.Last to run after other mods (like SuikodenFix) so we can override their fixes if a custom override exists
    [HarmonyPatch(typeof(TextMasterData), nameof(TextMasterData.GetSystemText))]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPrefix]
    public static bool GetSystemText_Prefix(string id, int index, ref string __result)
    {
        if (!Plugin.Config.EnableDialogOverrides.Value)
            return true;

        // Try to get an override from PortraitSystemPatch (which holds the dictionary)
        // Format for ID key: "id:index" (e.g. "sys_01:5")
        string key = $"{id}:{index}";

        // We access the dictionary via a public method on PortraitSystemPatch (we'll need to add this)
        // Or we can move the dictionary to a shared location. For now, let's add a public accessor to PortraitSystemPatch.
        string replacement = PortraitSystemPatch.GetDialogOverride(key);

        if (replacement != null)
        {
            if (Plugin.Config.LogTextIDs.Value)
                Plugin.Log.LogInfo($"[TextDebug] Applying Override: [{key}] -> \"{replacement}\"");

            __result = replacement;
            return false; // Skip original method and other prefixes if possible (Harmony handles this)
        }

        return true; // Continue execution
    }

    // Postfix for Speaker Injection (SpeakerOverrides.json)
    // Runs AFTER SuikodenFix, so we perform injection on the final text (whether fixed or original)
    // Priority.Last ensures we run after SuikodenFix's potential Postfix (though they likely use Prefix)
    [HarmonyPatch(typeof(TextMasterData), nameof(TextMasterData.GetSystemText))]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPostfix]
    public static void GetSystemText_Postfix(string id, int index, ref string __result)
    {
        // 1. Log ID if enabled
        if (Plugin.Config.LogTextIDs.Value)
        {
            string key = $"{id}:{index}";
            if (!loggedTextIDs.Contains(key))
            {
                loggedTextIDs.Add(key);
                Plugin.Log.LogInfo($"[TextDebug] [{key}] -> \"{__result}\"");
            }
        }

        LastTextId = $"{id}:{index}";

        // 2. Speaker Injection
        if (!Plugin.Config.EnableDialogOverrides.Value)
            return;

        // Check for Speaker Override by ID
        string speakerKey = $"{id}:{index}";
        string speakerData = PortraitSystemPatch.GetSpeakerOverride(speakerKey);

        if (!string.IsNullOrEmpty(speakerData))
        {
            // S1 builds messages char-by-char via AddMessageText — tags are not parsed there
            // and would display as raw text.  AddNameText_S1_Prefix handles name injection for S1.
            if (GameDetection.IsGSD1())
            {
                if (Plugin.Config.LogTextIDs.Value)
                    Plugin.Log.LogInfo($"[TextDebug] S1 speaker '{speakerData}' stored for '{speakerKey}' (handled by AddNameText patch)");
            }
            else
            {
                // S2: inject <speaker:...> tag; OpenMessageWindow_Prefix strips it and sets the name
                string displayName = speakerData.Contains("|")
                    ? speakerData.Split('|')[0].Trim()
                    : speakerData;

                string speakerTag = $"<speaker:{speakerData}>";

                if (!__result.StartsWith(speakerTag))
                {
                    __result = $"{speakerTag}{__result}";

                    if (Plugin.Config.LogTextIDs.Value)
                        Plugin.Log.LogInfo($"[TextDebug] Injected Speaker: {speakerKey} -> {displayName}" +
                            (speakerData.Contains("|") ? $" (variant: {speakerData.Split('|')[1]})" : ""));
                }
            }
        }
    }


    // Patch GetSystemTextEx as well (it seems to be used for game-specific texts)
    [HarmonyPatch(typeof(TextMasterData), nameof(TextMasterData.GetSystemTextEx))]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPostfix]
    public static void GetSystemTextEx_Postfix(string id, int index, int gsd, ref string __result)
    {
        // 1. Log ID if enabled
        if (Plugin.Config.LogTextIDs.Value)
        {
            string key = $"{id}:{index}:GSD{gsd}";
            if (!loggedTextIDs.Contains(key))
            {
                loggedTextIDs.Add(key);
                Plugin.Log.LogInfo($"[TextDebug] [{id}:{index}] (GSD:{gsd}) -> \"{__result}\"");
            }
        }

        LastTextId = $"{id}:{index}";
        LastMessageTextId = $"{id}:{index}"; // dedicated tracker — not polluted by UI text lookups

        // 2. Speaker Injection
        if (!Plugin.Config.EnableDialogOverrides.Value)
            return;

        string speakerKey = $"{id}:{index}";
        string speakerData = PortraitSystemPatch.GetSpeakerOverride(speakerKey);

        if (!string.IsNullOrEmpty(speakerData))
        {
            if (GameDetection.IsGSD1())
            {
                if (Plugin.Config.LogTextIDs.Value)
                    Plugin.Log.LogInfo($"[TextDebug] S1 speaker '{speakerData}' stored for '{speakerKey}' (handled by AddNameText patch)");
            }
            else
            {
                string displayName = speakerData.Contains("|")
                    ? speakerData.Split('|')[0].Trim()
                    : speakerData;

                string speakerTag = $"<speaker:{speakerData}>";

                if (!__result.StartsWith(speakerTag))
                {
                    __result = $"{speakerTag}{__result}";

                    if (Plugin.Config.LogTextIDs.Value)
                        Plugin.Log.LogInfo($"[TextDebug] Injected Speaker: {speakerKey} -> {displayName}" +
                            (speakerData.Contains("|") ? $" (variant: {speakerData.Split('|')[1]})" : ""));
                }
            }
        }
    }
}
