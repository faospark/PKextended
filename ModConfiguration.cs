using BepInEx.Configuration;

namespace PKCore;

public sealed class ModConfiguration
{
    private ConfigFile _config;

    // Sprite Filtering Settings
    public ConfigEntry<bool> SpriteFilteringEnabled { get; private set; }

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
    public HiddenConfigEntry<bool> ForceControllerPrompts { get; private set; }
    public ConfigEntry<string> ControllerPromptType { get; private set; }

    // Custom Texture Settings
    public HiddenConfigEntry<bool> EnableCustomTextures { get; private set; }
    public ConfigEntry<bool> DetailedLogs { get; private set; }
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
    public ConfigEntry<bool> ClassicSaveWindow { get; private set; }
    public ConfigEntry<string> MercFortFence { get; private set; }
    public ConfigEntry<bool> ColoredIntroAndFlashbacks { get; private set; }

    // War Ability Mod
    public ConfigEntry<bool> EnableWarAbilityMod { get; private set; }
    
    // World Map Settings
    public ConfigEntry<bool> DisableWorldMapClouds { get; private set; }
    public ConfigEntry<bool> DisableWorldMapSunrays { get; private set; }

    // Suikoden 1 UI Settings
    public ConfigEntry<bool> S1ScaledDownWorldMap { get; private set; }

    // UI Settings
    public ConfigEntry<bool> ScaleDownDialogBox { get; private set; }
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
    public HiddenConfigEntry<float> SpriteMipmapBias { get; private set; }
    public HiddenConfigEntry<bool> LogReplaceableTextures { get; private set; }
    public HiddenConfigEntry<bool> LogTexturePaths { get; private set; }


    public ModConfiguration(ConfigFile config)
    {
        _config = config;
    }

    public void Init()
    {


        EnableProjectKyaroSprites = _config.Bind(
            "01 Project Kyaro Sprites",
            "EnableProjectKyaroSprites",
            true,
            "Enable Project Kyaro sprite textures from PKS1 and PKS2 folders. Set to false to use original Pixel Based Sprites"
        );

        DisableSpritePostProcessing = _config.Bind(
            "01 Project Kyaro Sprites",
            "DisableSpritePostProcessing",
            true,
            "Prevent post-processing effects (bloom, vignette, depth of field, etc.) from affecting sprites and Prevents white outlines on edges and exposing sprite seams"
        );

        SpriteFilteringEnabled = _config.Bind(
            "01 Project Kyaro Sprites",
            "SpriteFilteringEnabled",
            true,
            "Enable texture filtering for sprites. false = to retain the Point Filtering, true = Enabled (Bilinear + 2x Aniso). Turn this off if you want to retain the original pixelated look of the game"
        );

        SMAAQuality = _config.Bind(
            "01 Project Kyaro Sprites",
            "SMAAQuality",
            "Low",
            "SMAA anti-aliasing quality for sprites. Options: Off, Low, Medium, High. NOTE: 'Low' is recommended. While this mod can be techincally under general. this feature is best used the project Kyaro sprites options. this also wont effect the game UI"
        );


        ControllerPromptType = _config.Bind(
            "02 User Interface",
            "ControllerPromptType",
            "PlayStation",
            "Controller type to display:\n" +
            "- PS4: 'PlayStation', 'PlayStation4', 'DS4', 'PS4' (_01 suffix)\n" +
            "- PS5: 'PlayStation5', 'DualSense', 'PS5' (_02 suffix)\n" +
            "-Generic/PC: 'Generic', 'PC', 'Keyboard' (_00 suffix)\n" +
            "- Xbox: 'Xbox' (_03 suffix - use with custom textures)\n" +
            "- Switch: 'Switch', 'Nintendo' (_04 suffix - use with custom textures)\n" +
            "Only applies if ForceControllerPrompts is enabled."
        );
        
        LoadLauncherUITextures = _config.Bind(
            "02 User Interface",
            "LoadLauncherUITextures",
            true,
            "Load custom textures from Textures/launcher folder. Set to false to use original launcher UI. This is based of the unused assets from the game files"
        );

        MinimalUI = _config.Bind(
            "02 User Interface",
            "MinimalUI",
            true,
            "Load minimal UI textures. Set to false to skip loading textures with 'minimal' in the path."
        );

        ClassicSaveWindow = _config.Bind(
            "02 User Interface",
            "ClassicSaveWindow",
            true,
            "Mimics the feel of the PSX version of Save/Load window for Both Games giving the save/load window a more nostalgic feel instead of the very generic looking window"
        );

        DisableMaskPortraitDialog = _config.Bind(
            "02 User Interface",
            "DisableMaskPortraitDialog",
            false,
            "Disable the Face_Mask_01 texture overlay on character portraits in dialog windows. This removes the mask effect that appears on portrait displays during conversations."
        );

        ScaleDownDialogBox = _config.Bind(
            "02 User Interface",
            "ScaleDownDialogBox",
            true,
            "Compact dialog box. false = Normal, true = 80% size with adjusted position"
        );

        ScaledDownMenu = _config.Bind(
            "02 User Interface",
            "ScaledDownMenu",
            "true",
            "Main menu layout preset. false = Normal , true: scaled down 80% with adjusted position)."
        );

        SavePointColor = _config.Bind(
            "03 General",
            "SavePointColor",
            "default",
            "Save point orb color. Options: blue, red, yellow, pink, green, cyan, white, dark, purple, navy, default. Place color variants in Textures/SavePoint/ folder as 't_obj_savePoint_ball_<color>.png'."
        );

        DisableSavePointGlow = _config.Bind(
            "03 General",
            "DisableSavePointGlow",
            true,
            "Disable the glow effect on save point orbs as it can actualy wash out the color of the orb. Set to false to keep the original glow."
        );

        DisableWorldMapClouds = _config.Bind(
            "03 General",
            "DisableWorldMapClouds",
            true,
            "Disable cloud effects on the world map for Suikoden 1 and 2. Set to false to keep the original cloud effects."
        );

        DisableWorldMapSunrays = _config.Bind(
            "03 General",
            "DisableWorldMapSunrays",
            true,
            "Disable sunray glow effects on the world map for Suikoden 1 and 2. Set to false to keep the original sunray glow effects."
        );
        // Game Specific
        S1ScaledDownWorldMap = _config.Bind(
            "04 Game : Suikoden 1",
            "S1ScaledDownWorldMap",
            true,
            "Scale down the Suikoden 1 world map UI to 80% with adjusted positioning for better visibility. When enabled, applies scale (0.8, 0.8, 1) and position (652.0001, -355.3, 0) to the smap element."
        );

        TirRunTexture = _config.Bind(
            "04 Game : Suikoden 1",
            "TirRunTexture",
            "default",
            "Texture variant for Tir running animation (shu_field_01_atlas). Options: default, alt. 'alt' will look for '..._alt.png'. ONLY FOR PROJECT KYARO SPRITES"
        );

        // Game Specific
        EnableNPCPortraits = _config.Bind(
            "05 Game : Suikoden 2",
            "EnableNPCPortraits",
            true,
            "Enable custom NPC portrait injection. Place PNG files in PKCore/NPCPortraits/ (in game root folder) named after the NPC (e.g., Ace.png, Yuri.png). Case-insensitive. For now Full Support of S2 and Partial S1"
        );

        MercFortFence = _config.Bind(
            "05 Game : Suikoden 2",
            "MercFortFence",
            "default",
            "Mercenary Fortress Fence texture variant. Enter any suffix (e.g. 'bamboo', 'green', 'custom'). Looks for suffix '_<value>'."
        );

        ColoredIntroAndFlashbacks = _config.Bind(
            "05 Game : Suikoden 2",
            "ColoredIntroAndFlashbacks",
            true,
            "Enable colored intro and flashback sequences for Suikoden 2. When enabled, disables the CustomPostEffect component to restore color to these scenes."
        );

        EnableWarAbilityMod = _config.Bind(
            "05 Game : Suikoden 2",
            "EnableWarAbilityMod",
            true,
            "Enable war battle ability modification. Allows you to customize character abilities in Suikoden 2's war battles. Wont appear on the game menu of Apple but will have an effect on battle. Base is already boosted but can be further configures in PKCore/Config/war_abilities.json"
        );

        // Performance section - all performance related settings
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
            "Enable intelligent texture memory caching. Automatically clears textures from memory when transitioning between areas to free up RAM. Textures are rebuilt when you enter new scenes. Keeps persistent UI textures in memory. Recommended for performance optimization on lower-end systems. Never turn this off"
        );

        EnableResolutionScaling = _config.Bind(
            "Performance",
            "EnableResolutionScaling",
            false,
            "Enable resolution scaling. When true, the game will render at ResolutionScale multiplier and stretch to fill the window. Must Be on for the ResolutionScale setting to take effect. Gives a performance boost by rendering at a lower resolution and stretching to fill the window."
        );

        ResolutionScale = _config.Bind(
            "Performance",
            "ResolutionScale",
            1.0f,
            new ConfigDescription(
                "Resolution scale multiplier (0.5 to 2.0). 0.5 = half resolution for performance, 1.0 = native, 1.5-2.0 = super-sampling for quality. Only applies when EnableResolutionScaling is enabled. Do not use if you are using the resolution changer from Suikoden Fix",
                new AcceptableValueRange<float>(0.5f, 2.0f)
            )
        );

        EnableBorderlessWindow = _config.Bind(
            "Utilty",
            "EnableBorderlessWindow",
            false,
            "Enable borderless fullscreen window mode. Provides instant alt-tab switching and better multi-monitor support."
        );

        ShowMouseCursor = _config.Bind(
            "Utilty",
            "ShowMouseCursor",
            false,
            "Show mouse cursor when hovering over the game window. Useful for debugging with Unity Explorer or accessing overlays."
        );

        // Diagnostics section - all logging and debugging settings
        LogTextIDs = _config.Bind(
            "zz - Diagnostics",
            "LogTextIDs",
            false,
            "Log all text ID lookups to the console. Enable this to find the ID of dialog lines you want to replace. WARNING: Creates a lot of log output."
        );

        DetailedLogs = _config.Bind(
            "zz - Diagnostics",
            "DetailedLogs",
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
        SpriteMipmapBias = new HiddenConfigEntry<float>(-0.5f);
        LogReplaceableTextures = new HiddenConfigEntry<bool>(true);
        LogTexturePaths = new HiddenConfigEntry<bool>(true);
        ForceControllerPrompts = new HiddenConfigEntry<bool>(true);
        EnableCustomTextures = new HiddenConfigEntry<bool>(true);

    }
}
