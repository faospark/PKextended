using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using PKCore.Patches;
using UnityEngine;

namespace PKCore;

[BepInPlugin("faospark.pkcore", "PKCore", "1.6.0")]
[BepInDependency("d3xMachina.suikoden_fix", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("faospark.SquidFam", BepInDependency.DependencyFlags.SoftDependency)]
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
        
        // Register custom MonoBehaviours for IL2CPP
        try
        {
            Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<SavePointSpriteMonitor>();
            Log.LogInfo("Registered SavePointSpriteMonitor in IL2CPP domain.");

            // SuikozuMonitor removed (replaced by Harmony Patch)
        }
        catch (System.Exception ex)
        {
            Log.LogError($"Failed to register custom monitors: {ex.Message}");
        }
        
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

        // Mouse Cursor Visibility (for debugging)
        if (Config.ShowMouseCursor.Value)
        {
            MouseCursorPatch.Initialize(true);
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

            // Patch for Save Window custom background
            harmony.PatchAll(typeof(SaveWindowPatch));
            
            // Apply GRSpriteRenderer patches for better sprite interception
            harmony.PatchAll(typeof(GRSpriteRendererPatch));
            GRSpriteRendererPatch.Initialize();
            
            // Apply Unity SpriteRenderer patches for standard Unity sprites
            harmony.PatchAll(typeof(UnitySpriteRendererPatch));
            UnitySpriteRendererPatch.Initialize();
            
            // Register lazy loader component
            Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<SuikozuTextureEnforcer>();
            Log.LogInfo("Registered SuikozuTextureEnforcer in IL2CPP domain.");

            // Apply Suikozu Internal Patch (Reactive instead of polling)
            Log.LogInfo("Applying Suikozu Internal Patch...");
            harmony.PatchAll(typeof(SuikozuInternalPatch));

            // Apply BGManagerHD.Load patch for bath sprite preloading
            Log.LogInfo("Applying BGManagerHD.Load patch for bath sprite preloading...");
            CustomTexturePatch.BGManagerHD_Load_Patch.Initialize(harmony);
            
            // [NEW] Map Texture Replacement (Native Material Array)
            Log.LogInfo("Applying Native Map Texture patches...");
            harmony.PatchAll(typeof(MapTexturePatch));
        }

        // NPC Portrait Injection
        if (Config.EnableNPCPortraits.Value)
        {
            Log.LogInfo("Applying NPC Portrait patches...");
            harmony.PatchAll(typeof(NPCPortraitPatch));
            NPCPortraitPatch.Initialize();
        }



        // ARCHIVED: MapBGManagerHD Object Diagnostics (moved to ObjectReserve folder)
        // if (Config.EnableObjectDiagnostics.Value)
        // {
        //     Log.LogInfo("Applying MapBGManagerHD diagnostic patches...");
        //     MapBGManagerHDDiagnostics.Initialize(Config.EnableObjectDiagnostics.Value, harmony);
        // }

        // EXPERIMENTAL: Custom Object Insertion
        // Note: Feature is functional but objects are invisible due to MapSpriteHD interference
        // Enable at your own risk for testing/development
        if (Config.EnableCustomObjects.Value)
        {
            Log.LogInfo("Applying Custom Object Insertion patches...");
            CustomObjectInsertion.Initialize(Config.EnableCustomObjects.Value, harmony);
        }

        // Dragon Sprite Patch
        if (Config.EnableCustomTextures.Value)
        {
            Log.LogInfo("Applying Dragon Sprite patches...");
            DragonPatch.Initialize();
        }

        // Cow Texture Patch (Similar to Dragon Patch)
        if (Config.EnableCustomTextures.Value)
        {
             Log.LogInfo("Applying Cow texture patches...");
             CowTexturePatch.Initialize();
        }
    }
}
