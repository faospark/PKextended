using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using PKextended.Patches;
using UnityEngine;

namespace PKextended;

[BepInPlugin("faospark.pkextended", "PKextended - Project Kyaro Extended", "1.0.0")]
[BepInDependency("d3xMachina.suikoden_fix", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BasePlugin
{
    public static new ManualLogSource Log;
    public static new ModConfiguration Config;

    public override void Load()
    {
        Log = base.Log;

        Log.LogInfo("Loading PKextended (Project Kyaro Extended) by faospark...");

        Config = new ModConfiguration(base.Config);
        Config.Init();

        ApplyPatches();

        Log.LogInfo("PKextended loaded successfully!");
    }

    private void ApplyPatches()
    {
        var harmony = new Harmony("faospark.pkextended");

        // Apply Sprite Filtering patch independently
        if (Config.SpriteFilteringQuality.Value > 0)
        {
            Log.LogInfo("Applying Sprite Filtering patches...");
            harmony.PatchAll(typeof(SpriteFilteringPatch));
            SpriteFilteringPatch.Initialize();
        }

        // Apply Resolution patch independently
        if (Config.EnableResolutionScaling.Value && Config.ResolutionScale.Value != 1.0f)
        {
            Log.LogInfo("Applying Resolution patches...");
            harmony.PatchAll(typeof(ResolutionPatch));
            ResolutionPatch.Initialize();
        }

        // Apply Sprite Post-Processing patch independently
        if (Config.DisableSpritePostProcessing.Value)
        {
            Log.LogInfo("Applying Disable Sprite Post-Processing patches...");
            harmony.PatchAll(typeof(DisableSpritePostProcessingPatch));
            DisableSpritePostProcessingPatch.Initialize();
        }

        // Borderless Window Mode
        if (Config.EnableBorderlessWindow.Value)
        {
            BorderlessWindowPatch.Initialize();
        }

        // Controller Prompt Override
        if (Config.ForceControllerPrompts.Value)
        {
            Log.LogInfo($"Applying Global Controller Prompt patches (type: {Config.ControllerPromptType.Value})...");
            harmony.PatchAll(typeof(GlobalControllerPromptPatch));
        }
    }
}
