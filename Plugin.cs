using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using PKCore.Patches;
using UnityEngine;

namespace PKCore;

[BepInPlugin("faospark.pkcore", "PKCore", "1.6.0")]
[BepInDependency("d3xMachina.suikoden_fix", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BasePlugin
{
    public static Plugin Instance { get; private set; }
    public static new ManualLogSource Log;
    public static new ModConfiguration Config;

    public override void Load()
    {
        Instance = this;
        Log = base.Log;

        Log.LogInfo("Loading PKCore...");
        
        // Log build timestamp for symlink verification
        var buildDate = System.IO.File.GetLastWriteTime(typeof(Plugin).Assembly.Location);
        Log.LogInfo($"Build timestamp: {buildDate:yyyy-MM-dd HH:mm:ss}");

        Config = new ModConfiguration(base.Config);
        Config.Init();

        // Suppress harmless Addressables warning after texture replacement
        Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);
        
        ApplyPatches();

        Log.LogInfo("PKCore loaded successfully!");
    }

    private void ApplyPatches()
    {
        var harmony = new Harmony("faospark.pkcore");

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

        // Custom Texture Replacement
        if (Config.EnableCustomTextures.Value || Config.LogReplaceableTextures.Value)
        {
            if (Config.EnableCustomTextures.Value)
            {
                Log.LogInfo("Applying Custom Texture patches...");
            }
            else
            {
                Log.LogInfo("Applying Custom Texture patches (logging only)...");
            }
            
            harmony.PatchAll(typeof(CustomTexturePatch));
            CustomTexturePatch.Initialize();
            
            // Apply GRSpriteRenderer patches for better sprite interception
            harmony.PatchAll(typeof(GRSpriteRendererPatch));
            GRSpriteRendererPatch.Initialize();
        }

        // NPC Portrait Injection
        if (Config.EnableNPCPortraits.Value)
        {
            Log.LogInfo("Applying NPC Portrait patches...");
            harmony.PatchAll(typeof(NPCPortraitPatch));
            NPCPortraitPatch.Initialize();
        }

        // ParticleSystem Diagnostics (Research)
        if (Config.EnableParticleSystemDiagnostics.Value)
        {
            Log.LogInfo("Applying ParticleSystem diagnostic patches...");
            harmony.PatchAll(typeof(ParticleSystemResearch));
            ParticleSystemResearch.Initialize(Config.EnableParticleSystemDiagnostics.Value);
        }
    }
}
