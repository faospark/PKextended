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
        
        // 2. Speaker Injection
        if (!Plugin.Config.EnableDialogOverrides.Value)
            return;
            
        // Check for Speaker Override by ID
        string speakerKey = $"{id}:{index}";
        string speakerData = PortraitSystemPatch.GetSpeakerOverride(speakerKey);
        
        if (!string.IsNullOrEmpty(speakerData))
        {
            // Parse to separate character name from expression (e.g., "Luca|blood" -> "Luca", "blood")
            // Keep full string for portrait system, but only inject character name in text
            string displayName = speakerData;
            if (speakerData.Contains("|"))
            {
                displayName = speakerData.Split('|')[0].Trim();
            }
            
            // Inject the tag with FULL speaker data (includes expression for portrait loading)
            // but the display name in the game will be clean
            string speakerTag = $"<speaker:{speakerData}>";
            
            // Avoid double tagging
            if (!__result.StartsWith(speakerTag))
            {
                 __result = $"{speakerTag}{__result}";
                 
                 if (Plugin.Config.LogTextIDs.Value)
                    Plugin.Log.LogInfo($"[TextDebug] Injected Speaker: {speakerKey} -> {displayName}" + 
                        (speakerData.Contains("|") ? $" (variant: {speakerData.Split('|')[1]})" : ""));
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
        
        // 2. Speaker Injection
        if (!Plugin.Config.EnableDialogOverrides.Value)
            return;
            
        string speakerKey = $"{id}:{index}";
        string speakerData = PortraitSystemPatch.GetSpeakerOverride(speakerKey);
        
        if (!string.IsNullOrEmpty(speakerData))
        {
            // Parse to separate character name from expression
            string displayName = speakerData;
            if (speakerData.Contains("|"))
            {
                displayName = speakerData.Split('|')[0].Trim();
            }
            
            // Inject full speaker tag (with expression for portrait system)
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
