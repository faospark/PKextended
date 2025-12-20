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
    
    // Display Settings
    public ConfigEntry<bool> EnableBorderlessWindow { get; private set; }

    // Controller Prompt Settings
    public ConfigEntry<bool> ForceControllerPrompts { get; private set; }
    public ConfigEntry<string> ControllerPromptType { get; private set; }

    // Custom Texture Settings
    public ConfigEntry<bool> EnableCustomTextures { get; private set; }
    public ConfigEntry<bool> LogReplaceableTextures { get; private set; }
    public ConfigEntry<bool> LogTexturePaths { get; private set; }
    public ConfigEntry<bool> DetailedTextureLog { get; private set; }

    public ModConfiguration(ConfigFile config)
    {
        _config = config;
    }

    public void Init()
    {
        SpriteFilteringQuality = _config.Bind(
            "Sprite Filtering",
            "SpriteFilteringQuality",
            3,
            "Texture filtering quality for sprites. 0 = Disabled (pure pixels), 1 = Low (Bilinear + 2x Aniso), 2 = Medium (Trilinear + 4x Aniso), 3 = High (Trilinear + 8x Aniso). Best for Project Kyaro's upscaled sprites."
        );

        SpriteMipmapBias = _config.Bind(
            "Sprite Filtering",
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
                "Resolution scale multiplier (0.5 to 2.0). 0.5 = half resolution for performance, 1.0 = native, 1.5-2.0 = super-sampling for quality. Only applies when StretchToWindow=true.",
                new AcceptableValueRange<float>(0.5f, 2.0f)
            )
        );

        DisableSpritePostProcessing = _config.Bind(
            "Visual",
            "DisableSpritePostProcessing",
            true,
            "Prevent post-processing effects (bloom, vignette, depth of field, etc.) from affecting sprites. Sprites will render with pure pixel art look while keeping post-processing on 3D elements and backgrounds. Compatible with sprite anti-aliasing."
        );

        EnableBorderlessWindow = _config.Bind(
            "Display",
            "EnableBorderlessWindow",
            false,
            "Enable borderless fullscreen window mode. Provides instant alt-tab switching and better multi-monitor support."
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
            "Enable custom texture replacement. Place PNG files in BepInEx/plugins/PKCore/Textures/ with the same name as the game texture (e.g., hp_telepo_00.png)."
        );

        LogReplaceableTextures = _config.Bind(
            "Custom Textures",
            "LogReplaceableTextures",
            false,
            "Log all textures that could be replaced. Each texture name is logged only once. Useful for discovering which textures you can customize."
        );

        LogTexturePaths = _config.Bind(
            "Custom Textures",
            "LogTexturePaths",
            false,
            "Include GameObject hierarchy paths in texture logs. Enable for detailed debugging to see exactly which UI elements use which textures."
        );

        DetailedTextureLog = _config.Bind(
            "Custom Textures",
            "DetailedTextureLog",
            false,
            "Enable detailed texture logging (replacement confirmations and full texture list on startup). Disable for silent operation (only errors will be logged)."
        );
    }
}
