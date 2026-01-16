using BepInEx.Configuration;

namespace PKCore;

public sealed class ModConfiguration
{
    private ConfigFile _config;

    // Sprite Filtering Settings
    public ConfigEntry<int> SpriteFilteringQuality { get; private set; }
    public ConfigEntry<float> SpriteMipmapBias { get; private set; }

    // Display Settings
    public ConfigEntry<bool> EnableResolutionScaling { get; private set; }
    public ConfigEntry<float> ResolutionScale { get; private set; }

    // Visual Settings
    public ConfigEntry<bool> DisableSpritePostProcessing { get; private set; }
    public ConfigEntry<bool> DisableMaskPortraitDialog { get; private set; }

    // Display Settings
    public ConfigEntry<bool> EnableBorderlessWindow { get; private set; }
    public ConfigEntry<bool> ShowMouseCursor { get; private set; }

    // Controller Prompt Settings
    public ConfigEntry<bool> ForceControllerPrompts { get; private set; }
    public ConfigEntry<string> ControllerPromptType { get; private set; }

    // Custom Texture Settings
    public ConfigEntry<bool> EnableCustomTextures { get; private set; }
    public ConfigEntry<bool> LogTextIDs { get; private set; }
    public ConfigEntry<bool> LogReplaceableTextures { get; private set; }
    public ConfigEntry<bool> LogTexturePaths { get; private set; }
    public ConfigEntry<bool> DetailedTextureLog { get; private set; }
    public ConfigEntry<bool> LoadLauncherUITextures { get; private set; }
    public ConfigEntry<bool> LoadBattleTextures { get; private set; }
    public ConfigEntry<bool> EnableProjectKyaroSprites { get; private set; }

    // NPC Portrait Settings
    public ConfigEntry<bool> EnableNPCPortraits { get; private set; }
    public ConfigEntry<bool> EnableDialogOverrides { get; private set; }

    // Save Point Settings
    public ConfigEntry<string> SavePointColor { get; private set; }
    public ConfigEntry<string> TirRunTexture { get; private set; }
    public ConfigEntry<bool> DisableSavePointGlow { get; private set; }

    // Suikoden 2 Classic UI Settings
    public ConfigEntry<bool> S2ClassicSaveWindow { get; private set; }
    public ConfigEntry<string> MercFortFence { get; private set; }
    public ConfigEntry<bool> ColoredIntroAndFlashbacks { get; private set; }

    // UI Settings
    public ConfigEntry<string> DialogBoxScale { get; private set; }
    public ConfigEntry<string> MenuScale { get; private set; }
    public ConfigEntry<string> SMAAQuality { get; private set; }

    // Performance Settings
    public ConfigEntry<bool> EnableTextureManifestCache { get; private set; }
    public ConfigEntry<bool> EnableTextureCompression { get; private set; }
    public ConfigEntry<string> TextureCompressionQuality { get; private set; }
    public ConfigEntry<string> TextureCompressionFormat { get; private set; }
    public ConfigEntry<bool> EnableDDSTextures { get; private set; }

    // EXPERIMENTAL FEATURES (at end of config file)
    public ConfigEntry<bool> EnableObjectDiagnostics { get; private set; }
    public ConfigEntry<bool> EnableCustomObjects { get; private set; }
    public ConfigEntry<bool> DebugCustomObjects { get; private set; }
    public ConfigEntry<bool> LogExistingMapObjects { get; private set; }
    public ConfigEntry<bool> EnableDebugMenu2 { get; private set; }
    public ConfigEntry<bool> EnableWarAbilityMod { get; private set; }




    public ModConfiguration(ConfigFile config)
    {
        _config = config;
    }

    public void Init()
    {
        // Visual section - most important sprite rendering settings first
        DisableSpritePostProcessing = _config.Bind(
            "Visual",
            "DisableSpritePostProcessing",
            true,
            "Prevent post-processing effects (bloom, vignette, depth of field, etc.) from affecting sprites. Sprites will render with pure pixel art look while keeping post-processing on 3D elements and backgrounds. Compatible with sprite anti-aliasing."
        );

        DisableMaskPortraitDialog = _config.Bind(
            "Visual",
            "DisableMaskPortraitDialog",
            false,
            "Disable the Face_Mask_01 texture overlay on character portraits in dialog windows. This removes the mask effect that appears on portrait displays during conversations."
        );

        SpriteFilteringQuality = _config.Bind(
            "Visual",
            "SpriteFilteringQuality",
            0,
            "Texture filtering quality for sprites. 0 = Disabled (pure pixels), 1 = Low (Bilinear + 2x Aniso), 2 = Medium (Trilinear + 4x Aniso), 3 = High (Trilinear + 8x Aniso). Best for Project Kyaro's upscaled sprites."
        );

        SpriteMipmapBias = _config.Bind(
            "Visual",
            "SpriteMipmapBias",
            -0.5f,
            new ConfigDescription(
                "Mipmap bias for sprite textures. Negative values (-0.5 to -1.0) make textures sharper and prevent white outlines on edges. Positive values (0.5 to 1.0) make textures softer/blurrier. 0 = neutral.",
                new AcceptableValueRange<float>(-2.0f, 2.0f)
            )
        );

        EnableResolutionScaling = _config.Bind(
            "Display",
            "EnableResolutionScaling",
            false,
            "Enable resolution scaling. When true, the game will render at ResolutionScale multiplier and stretch to fill the window."
        );

        ResolutionScale = _config.Bind(
            "Display",
            "ResolutionScale",
            1.0f,
            new ConfigDescription(
                "Resolution scale multiplier (0.5 to 2.0). 0.5 = half resolution for performance, 1.0 = native, 1.5-2.0 = super-sampling for quality. Only applies when EnableResolutionScaling is enabled.",
                new AcceptableValueRange<float>(0.5f, 2.0f)
            )
        );

        EnableBorderlessWindow = _config.Bind(
            "Display",
            "EnableBorderlessWindow",
            false,
            "Enable borderless fullscreen window mode. Provides instant alt-tab switching and better multi-monitor support."
        );

        ShowMouseCursor = _config.Bind(
            "Display",
            "ShowMouseCursor",
            false,
            "Show mouse cursor when hovering over the game window. Useful for debugging with Unity Explorer or accessing overlays. The game is designed for controller, so this is primarily for development/debugging."
        );

        ForceControllerPrompts = _config.Bind(
            "Controller",
            "ForceControllerPrompts",
            true,
            "Force specific controller button prompts regardless of detected controller. Useful if you prefer PS/Xbox/Switch button icons."
        );

        ControllerPromptType = _config.Bind(
            "Controller",
            "ControllerPromptType",
            "PlayStation",
            "Controller type to display:\n" +
            "- PS4: 'PlayStation', 'PlayStation4', 'DS4', 'PS4'\n" +
            "- PS5: 'PlayStation5', 'DualSense', 'PS5'\n" +
            "- Xbox/Generic/Switch: 'Xbox', 'PS', 'Generic', 'Switch'\n" +
            "Only applies if ForceControllerPrompts is enabled."
        );

        EnableCustomTextures = _config.Bind(
            "Custom Textures",
            "EnableCustomTextures",
            true,
            "Enable custom texture replacement. Place PNG files in PKCore/Textures/ (in game root folder) with the same name as the game texture (e.g., hp_telepo_00.png)."
        );

        LoadLauncherUITextures = _config.Bind(
            "Custom Textures",
            "LoadLauncherUITextures",
            true,
            "Load custom textures from Textures/launcher folder. Set to false to use original launcher UI."
        );

        EnableProjectKyaroSprites = _config.Bind(
            "Custom Textures",
            "EnableProjectKyaroSprites",
            true,
            "Enable Project Kyaro sprite textures from PKS1 and PKS2 folders. Set to false to use original sprites."
        );

        EnableNPCPortraits = _config.Bind(
            "NPC Portraits",
            "EnableNPCPortraits",
            true,
            "Enable custom NPC portrait injection. Place PNG files in PKCore/NPCPortraits/ (in game root folder) named after the NPC (e.g., Viktor.png, Flik.png). Case-insensitive."
        );

        EnableDialogOverrides = _config.Bind(
            "NPC Portraits",
            "EnableDialogOverrides",
            true,
            "Enable dialog text overrides from DialogOverrides.json. Allows replacing specific text lines and injecting custom speaker names."
        );

        SavePointColor = _config.Bind(
            "Custom Textures",
            "SavePointColor",
            "default",
            "Save point orb color. Options: blue, red, yellow, pink, green, cyan, white, dark, purple, navy, default. Place color variants in Textures/SavePoint/ folder as 't_obj_savePoint_ball_<color>.png'."
        );

        TirRunTexture = _config.Bind(
            "Custom Textures",
            "TirRunTexture",
            "default",
            "Texture variant for Tir running animation (shu_field_01_atlas). Options: default, alt. 'alt' will look for '..._alt.png'."
        );

        DisableSavePointGlow = _config.Bind(
            "Custom Textures",
            "DisableSavePointGlow",
            true,
            "Disable the glow effect on save point orbs. Set to false to keep the original glow."
        );

        S2ClassicSaveWindow = _config.Bind(
            "Suikoden 2",
            "S2ClassicSaveWindow",
            false,
            "Mimics the feel of the PSX version of Save/Load window for Suikoden 2. Replaces the HD Remaster's ornate frame with a simple fullscreen background."
        );

        MercFortFence = _config.Bind(
            "Suikoden 2",
            "MercFortFence",
            "default",
            "Mercenary Fortress Fence texture variant. Enter any suffix (e.g. 'bamboo', 'green', 'custom'). Looks for suffix '_<value>'."
        );

        ColoredIntroAndFlashbacks = _config.Bind(
            "Suikoden 2",
            "ColoredIntroAndFlashbacks",
            false,
            "Enable colored intro and flashback sequences for Suikoden 2. When enabled, disables the CustomPostEffect component to restore color to these scenes."
        );

        DialogBoxScale = _config.Bind(
            "UI",
            "DialogBoxScale",
            "Large",
            "Dialog box size preset. Options: Large (full size, default), Medium (80% size), Small (50% size, very compact). Affects both size and position."
        );

        MenuScale = _config.Bind(
            "UI",
            "MenuScale",
            "default",
            "Main menu layout preset. Options: default (original layout), alt (scaled down 80% with adjusted position for better visibility)."
        );

        SMAAQuality = _config.Bind(
            "Graphics",
            "SMAAQuality",
            "Off",
            "SMAA anti-aliasing quality. Options: Off, Low, Medium, High. Higher quality = better visuals but lower performance."
        );



        EnableTextureManifestCache = _config.Bind(
            "Performance",
            "EnableTextureManifestCache",
            true,
            "Enable texture manifest caching for faster startup. Caches the texture index to skip re-scanning the Textures folder on every launch. Disable if you're actively adding/removing textures and want changes detected immediately."
        );

        EnableTextureCompression = _config.Bind(
            "Performance",
            "EnableTextureCompression",
            true,
            "Compress custom textures using BC3 (DXT5) format to reduce VRAM usage by 4-6x. Improves GPU performance with minimal quality loss. Recommended for high-resolution texture packs."
        );

        TextureCompressionQuality = _config.Bind(
            "Performance",
            "TextureCompressionQuality",
            "High",
            "Texture compression quality. Options: High (slower compression, better quality), Normal (faster compression, good quality). Only applies when EnableTextureCompression is true."
        );

        TextureCompressionFormat = _config.Bind(
            "Performance",
            "TextureCompressionFormat",
            "Auto",
            "Texture compression format. Options: Auto (BC1 for RGB, BC3 for RGBA - recommended), BC1 (DXT1, 8:1 compression, RGB only), BC3 (DXT5, 6:1 compression, RGBA). Auto is recommended."
        );

        EnableDDSTextures = _config.Bind(
            "Performance",
            "EnableDDSTextures",
            true,
            "Load pre-compressed DDS files when available (e.g., texture.dds instead of texture.png). DDS files have zero compression cost and load faster. Highly recommended for large texture packs."
        );

        // Diagnostics section - all logging and debugging settings
        LogTextIDs = _config.Bind(
            "zz - Diagnostics",
            "LogTextIDs",
            false,
            "Log all text ID lookups to the console. Enable this to find the ID of dialog lines you want to replace. WARNING: Creates a lot of log output."
        );

        LogReplaceableTextures = _config.Bind(
            "zz - Diagnostics",
            "LogReplaceableTextures",
            false,
            "Log all textures that could be replaced. Each texture name is logged only once. Useful for discovering which textures you can customize."
        );

        LogTexturePaths = _config.Bind(
            "zz - Diagnostics",
            "LogTexturePaths",
            false,
            "Include GameObject hierarchy paths in texture logs. Enable for detailed debugging to see exactly which UI elements use which textures."
        );

        DetailedTextureLog = _config.Bind(
            "zz - Diagnostics",
            "DetailedTextureLog",
            false,
            "Enable detailed texture logging (replacement confirmations and full texture list on startup). Disable for silent operation (only errors will be logged)."
        );

        // ========================================
        // EXPERIMENTAL FEATURES
        // ========================================
        // These features are work-in-progress and may not function correctly.
        // Enable at your own risk for testing purposes.

        EnableObjectDiagnostics = _config.Bind(
            "zz - Experimental",
            "EnableObjectDiagnostics",
            false,
            "[EXPERIMENTAL] Enable diagnostic logging for MapBGManagerHD objects. Logs all objects in field scenes to help understand scene structure. For development/debugging only."
        );

        EnableCustomObjects = _config.Bind(
            "zz - Experimental",
            "EnableCustomObjects",
            false,
            "[EXPERIMENTAL - NOT WORKING] Enable custom object insertion. Objects are created but invisible due to MapSpriteHD interference. Allows you to add custom static objects to game scenes via objects.json configuration."
        );

        DebugCustomObjects = _config.Bind(
            "zz - Experimental",
            "DebugCustomObjects",
            false,
            "[EXPERIMENTAL] Show magenta debug sprites for custom objects when their texture files are missing. Useful for testing object placement and visibility."
        );

        LogExistingMapObjects = _config.Bind(
            "zz - Experimental",
            "LogExistingMapObjects",
            false,
            "[EXPERIMENTAL] Log all existing map objects to ExistingMapObjects.json. This creates a reference file you can copy from when creating custom objects. Disable after collecting the data you need."
        );

        EnableDebugMenu2 = _config.Bind(
            "zz - Experimental",
            "EnableDebugMenu2",
            false,
            "[EXPERIMENTAL] Enable the DebugMenu2 object which is normally disabled in the game. This may provide access to developer debug features."
        );

        EnableWarAbilityMod = _config.Bind(
            "zz - Experimental",
            "EnableWarAbilityMod",
            false,
            "[EXPERIMENTAL] Enable war battle ability modification. Allows you to customize character abilities in Suikoden 2's war battles. Configure abilities in Patches/WarAbilityPatch.cs."
        );

    }
}
