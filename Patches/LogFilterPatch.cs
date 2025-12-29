using HarmonyLib;
using UnityEngine;

namespace PKCore.Patches;

/// <summary>
/// Filters out noisy/harmless Unity log messages
/// </summary>
[HarmonyPatch]
public class LogFilterPatch
{
    [HarmonyPatch(typeof(Debug), nameof(Debug.LogWarning), typeof(object))]
    [HarmonyPrefix]
    public static bool Debug_LogWarning_Prefix(object message)
    {
        if (message == null)
            return true;

        string msg = message.ToString();

        // Suppress Addressables.Release warning (harmless)
        if (msg.Contains("Addressables.Release was called on an object that Addressables was not previously aware of"))
            return false; // Skip logging

        return true; // Allow logging
    }
}
