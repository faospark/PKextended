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

        // Try to get an override from NPCPortraitPatch (which holds the dictionary)
        // Format for ID key: "id:index" (e.g. "sys_01:5")
        string key = $"{id}:{index}";
        
        // We access the dictionary via a public method on NPCPortraitPatch (we'll need to add this)
        // Or we can move the dictionary to a shared location. For now, let's add a public accessor to NPCPortraitPatch.
        string replacement = NPCPortraitPatch.GetDialogOverride(key);
        
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
        string speakerName = NPCPortraitPatch.GetSpeakerOverride(speakerKey);
        
        if (!string.IsNullOrEmpty(speakerName))
        {
            // Inject the tag at the start of the text
            // The text might already have a tag if we used DialogOverrides, but this postfix handles the ID-based SpeakerOverrides case
            // If we used DialogOverrides, we already have the custom text.
            // But if we didn't, we want to add the tag to the current text.
            
            // Avoid double tagging if the text already starts with a speaker tag that matches
            if (!__result.StartsWith($"<speaker:{speakerName}>"))
            {
                 __result = $"<speaker:{speakerName}>{__result}";
                 
                 if (Plugin.Config.LogTextIDs.Value)
                    Plugin.Log.LogInfo($"[TextDebug] Injected Speaker: {speakerKey} -> {speakerName}");
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
        string speakerName = NPCPortraitPatch.GetSpeakerOverride(speakerKey);
        
        if (!string.IsNullOrEmpty(speakerName))
        {
            if (!__result.StartsWith($"<speaker:{speakerName}>"))
            {
                 __result = $"<speaker:{speakerName}>{__result}";
                 
                 if (Plugin.Config.LogTextIDs.Value)
                    Plugin.Log.LogInfo($"[TextDebug] Injected Speaker: {speakerKey} -> {speakerName}");
            }
        }
    }
}
