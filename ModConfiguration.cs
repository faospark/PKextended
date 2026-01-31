using BepInEx.Configuration;

namespace PKCore;

public sealed class ModConfiguration
{
    private ConfigFile _config;

    // Sprite Filtering Settings
    public ConfigEntry<bool> SpriteFilteringEnabled { get; private set; }
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
    public ConfigEntry<bool> LogReplaceableTextures { get; private set; }
    public ConfigEntry<bool> LogTexturePaths { get; private set; }
    public ConfigEntry<bool> DetailedTextureLog { get; private set; }
    public ConfigEntry<bool> LoadLauncherUITextures { get; private set; }
    public ConfigEntry<bool> EnableProjectKyaroSprites { get; private set; }
    public ConfigEntry<bool> MinimalUI { get; private set; }

    // NPC Portrait Settings
    public ConfigEntry<bool> LogTextIDs { get; private set; }
    public ConfigEntry<bool> EnableNPCPortraits { get; private set; }


    // Save Point Settings
    public ConfigEntry<string> SavePointColor { get; private set; }
    public ConfigEntry<string> TirRunTexture { get; private set; }
    public ConfigEntry<bool> DisableSavePointGlow { get; private set; }

    // Suikoden 2 Classic UI Settings
    public ConfigEntry<bool> S2ClassicSaveWindow { get; private set; }
    public ConfigEntry<string> MercFortFence { get; private set; }
    public ConfigEntry<bool> ColoredIntroAndFlashbacks { get; private set; }

    // War Ability Mod
    public ConfigEntry<bool> EnableWarAbilityMod { get; private set; }

    // Suikoden 1 UI Settings
    public ConfigEntry<bool> S1ScaledDownWorldMap { get; private set; }

    // UI Settings
    public ConfigEntry<bool> DialogBoxScale { get; private set; }
    public ConfigEntry<string> ScaledDownMenu { get; private set; }
    public ConfigEntry<string> SMAAQuality { get; private set; }

    // Performance Settings
    public ConfigEntry<bool> EnableTextureManifestCache { get; private set; }
    public ConfigEntry<bool> EnableMemoryCaching { get; private set; }

    // EXPERIMENTAL FEATURES (at end of config file)
    public ConfigEntry<bool> EnableDebugMenu2 { get; private set; }

    // Hidden internal settings - not exposed in config file at all
    // These are hardcoded to false and cannot be changed by users
    // If you need to enable them for development, uncomment the bindings in Init()
    public class HiddenConfigEntry<T>
    {
        private T _value;
        public T Value => _value;
        public HiddenConfigEntry(T defaultValue) { _value = defaultValue; }
    }

    public HiddenConfigEntry<bool> EnableObjectDiagnostics { get; private set; }
    public HiddenConfigEntry<bool> EnableCustomObjects { get; private set; }
    public HiddenConfigEntry<bool> DebugCustomObjects { get; private set; }
    public HiddenConfigEntry<bool> LogExistingMapObjects { get; private set; }
    public HiddenConfigEntry<bool> EnableDialogOverrides { get; private set; }


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

        SpriteFilteringEnabled = _config.Bind(
            "Visual",
            "SpriteFilteringEnabled",
            false,
            "Enable texture filtering for sprites. false = Disabled (pure pixels), true = Enabled (Bilinear + 2x Aniso). Best for Project Kyaro's upscaled sprites."
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
            "Show mouse cursor when hovering over the game window. Useful for debugging with Unity Explorer or accessing overlays."
        );

        ForceControllerPrompts = _config.Bind(
            "Controller",
            "ForceControllerPrompts",
            true,
            "Force specific controller button prompts regardless of detected controller. Must be on for controller prompts to work."
        );

        ControllerPromptType = _config.Bind(
            "Controller",
            "ControllerPromptType",
            "PlayStation",
            "Controller type to display:\n" +
            "- PS4: 'PlayStation', 'PlayStation4', 'DS4', 'PS4' (_01 suffix)\n" +
            "- PS5: 'PlayStation5', 'DualSense', 'PS5' (_02 suffix)\n" +
            "- /Generic/PC: 'Generic', 'PC', 'Keyboard' (_00 suffix)\n" +
            "- XboxNative: 'XboxNative' (_03 suffix - use with custom textures)\n" +
            "- Switch: 'Switch', 'Nintendo' (_04 suffix - use with custom textures)\n" +
            "- Custom: 'Custom' (_05 suffix - use with custom textures)\n" +
            "Only applies if ForceControllerPrompts is enabled."
        );

        EnableCustomTextures = _config.Bind(
            "00 General",
            "EnableCustomTextures",
            true,
            "Enable custom texture replacement. Place PNG files in PKCore/Textures/ (in game root folder) with the same name as the game texture (e.g., hp_telepo_00.png)."
        );

        EnableProjectKyaroSprites = _config.Bind(
            "00 General",
            "EnableProjectKyaroSprites",
            true,
            "Enable Project Kyaro sprite textures from PKS1 and PKS2 folders. Set to false to use original sprites."
        );
        
        LoadLauncherUITextures = _config.Bind(
            "01 Interface",
            "LoadLauncherUITextures",
            true,
            "Load custom textures from Textures/launcher folder. Set to false to use original launcher UI."
        );

        MinimalUI = _config.Bind(
            "01 Interface",
            "MinimalUI",
            true,
            "Load minimal UI textures. Set to false to skip loading textures with 'minimal' in the path."
        );

        EnableNPCPortraits = _config.Bind(
            "00 General",
            "EnableNPCPortraits",
            true,
            "Enable custom NPC portrait injection. Place PNG files in PKCore/NPCPortraits/ (in game root folder) named after the NPC (e.g., Ace.png, Yuri.png). Case-insensitive. For now Full Support of S2 and Partial S1"
        );

        SavePointColor = _config.Bind(
            "00 General",
            "SavePointColor",
            "default",
            "Save point orb color. Options: blue, red, yellow, pink, green, cyan, white, dark, purple, navy, default. Place color variants in Textures/SavePoint/ folder as 't_obj_savePoint_ball_<color>.png'."
        );

        DisableSavePointGlow = _config.Bind(
            "00 General",
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

        EnableWarAbilityMod = _config.Bind(
            "Suikoden 2",
            "EnableWarAbilityMod",
            false,
            "Enable war battle ability modification. Allows you to customize character abilities in Suikoden 2's war battles. Wont appear on the game menu of Apple but will have an effect on battle. Base is already boosted but can be further configures in PKCore/Config/war_abilities.json"
        );

        S1ScaledDownWorldMap = _config.Bind(
            "Suikoden 1",
            "S1ScaledDownWorldMap",
            true,
            "Scale down the Suikoden 1 world map UI to 80% with adjusted positioning for better visibility. When enabled, applies scale (0.8, 0.8, 1) and position (652.0001, -355.3, 0) to the smap element."
        );

        TirRunTexture = _config.Bind(
            "Suikoden 1",
            "TirRunTexture",
            "default",
            "Texture variant for Tir running animation (shu_field_01_atlas). Options: default, alt. 'alt' will look for '..._alt.png'. ONLY FOR PROJECT KYARO SPRITES"
        );

        DialogBoxScale = _config.Bind(
            "01 Interface",
            "DialogBoxScale",
            false,
            "Compact dialog box. false = Large (full size, default), true = Medium (80% size with adjusted position)."
        );

        ScaledDownMenu = _config.Bind(
            "01 Interface",
            "ScaledDownMenu",
            "true",
            "Main menu layout preset. Options: default (original layout), alt (scaled down 80% with adjusted position for better visibility)."
        );

        SMAAQuality = _config.Bind(
            "Graphics",
            "SMAAQuality",
            "High",
            "SMAA anti-aliasing quality Mainly for battle effects. Options: Off, Low, Medium, High. Higher quality = better visuals but lower performance."
        );


        EnableTextureManifestCache = _config.Bind(
            "Performance",
            "EnableTextureManifestCache",
            true,
            "Enable texture manifest caching for faster startup. Caches the texture index to skip re-scanning the Textures folder on every launch. Disable if you're actively adding/removing textures and want changes detected immediately."
        );

        EnableMemoryCaching = _config.Bind(
            "Performance",
            "EnableMemoryCaching",
            true,
            "Enable intelligent texture memory caching. Automatically clears textures from memory when transitioning between areas to free up RAM. Textures are rebuilt when you enter new scenes. Keeps persistent UI textures in memory. Recommended for performance optimization on lower-end systems."
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

        EnableDebugMenu2 = _config.Bind(
            "zz - Experimental",
            "EnableDebugMenu2",
            false,
            "[EXPERIMENTAL] Enable the DebugMenu2 object which is normally disabled in the game. This may provide access to developer debug features."
        );

        // ========================================
        // HIDDEN SETTINGS (Internal Use Only)
        // ========================================
        // These settings are NOT written to the config file and are hardcoded to false
        // If you need to enable them for development, replace these with _config.Bind() calls

        EnableDialogOverrides = new HiddenConfigEntry<bool>(true);
        EnableObjectDiagnostics = new HiddenConfigEntry<bool>(false);
        EnableCustomObjects = new HiddenConfigEntry<bool>(false);
        DebugCustomObjects = new HiddenConfigEntry<bool>(false);
        LogExistingMapObjects = new HiddenConfigEntry<bool>(false);

    }
}
