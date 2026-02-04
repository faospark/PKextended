using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using PKCore.Patches;
using UnityEngine;

namespace PKCore;

[BepInPlugin("faospark.pkcore", "PKCore", "2026.01.0")]
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

        Config = new ModConfiguration(base.Config);
        Config.Init();

        AssetLoader.Initialize();

        // Force immediate exit on quit to prevent Alt+F4 hangs (Unity 2022/BepInEx 6 issue)
        Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);
        Application.quitting += (System.Action)(() => 
        {
            Log.LogInfo("Shutting down... forcing process exit.");
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        });
        
        // Register custom MonoBehaviours for IL2CPP
        try
        {
            Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<SavePointSpriteMonitor>();


        }
        catch (System.Exception ex)
        {
            Log.LogError($"Failed to register custom monitors: {ex.Message}");
        }
        
        ApplyPatches();

        Log.LogInfo("PKCore loaded successfully!");
    }

    public void Update()
    {
        Patches.GameDetection.Update();
        AssetLoader.Update();
    }

    private void ApplyPatches()
    {
        var harmony = new Harmony("faospark.pkcore");

        // Note: Log suppression via Application.logMessageReceived doesn't actually prevent logs
        // since the event fires AFTER logging. Disabled for now due to IL2CPP Debug method patching issues.
        // SuppressLogs.Initialize();

        // Initialize core texture system first (indexes all custom files)
        if (Config.EnableCustomTextures.Value || Config.LogReplaceableTextures.Value)
        {
            CustomTexturePatch.Initialize();
            CustomTexturePersist.Initialize();
        }

        // Initialize memory caching system for smart texture cleanup
        if (Config.EnableMemoryCaching.Value)
        {
            Log.LogInfo("Initializing Texture Memory Caching System...");
            harmony.PatchAll(typeof(TextureMemoryCachePatch));
            TextureMemoryCachePatch.Initialize();
        }

        // Apply Sprite Filtering patch independently
        if (Config.SpriteFilteringEnabled.Value)
        {
            Log.LogInfo($"Applying Sprite Filtering patches (Enabled: {Config.SpriteFilteringEnabled.Value})...");
            harmony.PatchAll(typeof(SpriteFilteringPatch));
            SpriteFilteringPatch.Initialize();
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
            harmony.PatchAll(typeof(MouseCursorPatch));
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

            // Patch for Save Window custom background
            harmony.PatchAll(typeof(SaveWindowPatch));
            
            // Apply GRSpriteRenderer patches for better sprite interception
            harmony.PatchAll(typeof(GRSpriteRendererPatch));
            GRSpriteRendererPatch.Initialize();
            
            // Apply Unity SpriteRenderer patches for standard Unity sprites
            harmony.PatchAll(typeof(UnitySpriteRendererPatch));
            UnitySpriteRendererPatch.Initialize();


            
            // Suikozu reactive patch (GSD2 world map)
            // IL2CPP registration is now lazy-loaded when first map is opened
            harmony.PatchAll(typeof(SuikozuPatch));

            // NOTE: BGManagerHD_Load_Patch is part of BathTexturePatch in this version
            // Log.LogInfo("Applying BGManagerHD.Load patch ...");
            // CustomTexturePatch.BGManagerHD_Load_Patch.Initialize(harmony);
            
            if (Config.DetailedTextureLog.Value)
            {
                Log.LogInfo("Applying Native Map Texture patches...");
            }
            harmony.PatchAll(typeof(MapTexturePatch));
            
            
            // GameObject activation patches are part of CustomTexturePatch (already applied above)
            // This handles Dragon, Cow, and Suikozu monitor attachment
        }
        
        // Resolution patch
        if (Config.EnableResolutionScaling.Value && Config.ResolutionScale.Value != 1.0f)
        {
            Log.LogInfo("Applying Resolution patches...");
            harmony.PatchAll(typeof(ResolutionPatch));
            ResolutionPatch.Initialize();
        }

        // Sprite Post-Processing patch
        if (Config.DisableSpritePostProcessing.Value)
        {
            Log.LogInfo("Applying DisableSpritePostProcessing Patch");
            harmony.PatchAll(typeof(DisableSpritePostProcessingPatch));
            DisableSpritePostProcessingPatch.Initialize();
        }

        // NPC Portrait Injection
        if (Config.EnableNPCPortraits.Value)
        {
            Log.LogInfo("Applying NPC Portrait patches...");
            harmony.PatchAll(typeof(NPCPortraitPatch));
            NPCPortraitPatch.Initialize();
            
            // Apply Dialog Text ID interceptor (if overrides or logging enabled)
            if (Config.EnableDialogOverrides.Value || Config.LogTextIDs.Value)
            {
                Log.LogInfo("Applying TextDatabase patches...");
                harmony.PatchAll(typeof(TextDatabasePatch));
            }
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

        if (Config.EnableCustomTextures.Value)
        {
            if (Config.DetailedTextureLog.Value)
            {
                Log.LogInfo("Applying Dragon Sprite patches...");
            }
            DragonPatch.Initialize();
        }

        if (Config.EnableCustomTextures.Value)
        {
             if (Config.DetailedTextureLog.Value)
             {
                Log.LogInfo("Applying Cow texture patches...");
             }
             CowTexturePatch.Initialize();
        }

        // Disable CustomPostEffect for colored intro/flashbacks (Suikoden 2)
        if (Config.ColoredIntroAndFlashbacks.Value)
        {
            DisableCustomPostEffectPatch.Initialize(harmony);
        }

        // Apply Dialog Patch (Dialog Box Scaling)
        if (Config.ScaleDownDialogBox.Value)
        {
            Log.LogInfo("Applying Dialog patches (size: Medium)...");
            harmony.PatchAll(typeof(DialogPatch));
        }

        // Apply Suikoden 1 World Map Scale Patch
        if (Config.S1ScaledDownWorldMap.Value)
        {
            Log.LogInfo("Applying S1WorldMapScaleUI patches...");
            S1WorldMapScaleUIPatch.Initialize();
            harmony.PatchAll(typeof(S1WorldMapScaleUIPatch));
        }

        // Apply Voice Acting Patch (Text-to-Speech)


        // Apply Menu Scale Patch (Menu Scaling)
        if (Config.ScaledDownMenu.Value.Equals("true", System.StringComparison.OrdinalIgnoreCase))
        {
            Log.LogInfo($"Applying Menu Scale patches (mode: {Config.ScaledDownMenu.Value})...");
            harmony.PatchAll(typeof(MenuScalePatch));
        }

        // Apply SMAA patches
        if (!Config.SMAAQuality.Value.Equals("Off", System.StringComparison.OrdinalIgnoreCase))
        {
            Log.LogInfo($"Applying SMAA patches (quality: {Config.SMAAQuality.Value})...");
            harmony.PatchAll(typeof(SMAAPatch));
        }

        // Mask Replacement System (Always Active)
        // Replaces _Mask_Map textures with corresponding files from PKCore/Textures
        // Build exclusion list based on config
        var excludedMasks = new System.Collections.Generic.HashSet<string>();
        
        if (!Config.DisableMaskPortraitDialog.Value)
        {
            // If DisableMaskPortraitDialog is OFF, exclude Face_Mask_01
            excludedMasks.Add("Face_Mask_01");
        }
        
        if (Config.DetailedTextureLog.Value)
        {
            Log.LogInfo("Applying Mask Replacement System...");
        }
        DisableMask.Initialize(excludedMasks);
        harmony.PatchAll(typeof(DisableMask));
        // Enable DebugMenu2 (Experimental)
        if (Config.EnableDebugMenu2.Value)
        {
            Log.LogInfo("Initializing EnableDebugMenu2...");
            EnableDebugMenu2.Initialize();
        }

        // War Ability Modification (Experimental)
        if (Config.EnableWarAbilityMod.Value)
        {
            Log.LogInfo("Initializing War Ability Modification...");
            WarAbilityPatch.Initialize(Log);
        }

    }
}
