# PKextended - Project Kyaro Extended

**By faospark**  
**Current Version: 1.5**

A companion mod to **[Project Kyaro](https://www.nexusmods.com/suikoden1and2hdremaster/mods/6)** that adds sprite smoothing, anti-aliasing, performance scaling, and controller prompt customization for **Suikoden I & II HD Remaster**.

Built to enhance the upscaled sprites from Project Kyaro with proper texture filtering and modern rendering features.

## Requirements

- **BepInEx 6.0.0-pre.2 IL2CPP**
- **Suikoden I & II HD Remaster** (Unity 2022.3.28f1)
- **Highly Recommended**: [Suikoden Fix](https://github.com/d3xMachina/BepInEx.Suikoden) for best experience

## Features

### Controller Prompt Override (v1.5+)

Force specific controller button icons regardless of detected controller:

- **PlayStation Support**: PS4 (DualShock 4) and PS5 (DualSense) button prompts
- **Xbox Support**: Xbox controller button prompts
- **Smart Swapping**: Automatically swaps button icons throughout the entire game
- **Minigame Support**: Intelligent button cycling for minigame prompts

### Sprite Post Processing Correction

- **Post-Processing Control**: Disable effects on sprites while keeping them on backgrounds, This is crucial to disable the game effects that affects the sprites Eg: Sand storm in Effect in battle appearing on seams of the battle sprites. 

### Sprite Smoothing

Designed for **Project Kyaro's upscaled sprites** - adds a more granular texture filtering Options:

- **High Quality** (default): Trilinear + 8x Anisotropic filtering
- **Medium**: Trilinear + 4x Anisotropic  
- **Low**: Bilinear + 2x Anisotropic
- **Off**: Pure pixel art (no filtering for the base game)
- Includes mipmap bias control to prevent white outlines, Really more about your choice on how you want Project Kyaro Sprites to look

### Additional Features

- **Resolution Scaling**: Lower internal resolution for better performance (0.5x - 2.0x)
- **Borderless Window Mode**: Instant alt-tab and better multi-monitor support, Only use this if you dont want to use the in-game full screen option

## Installation

1. Install **BepInEx 6.0.0-pre.2 IL2CPP**
2. Install **[Suikoden Fix](https://github.com/d3xMachina/BepInEx.Suikoden)** (highly recommended)
3. Install **[Project Kyaro](https://www.nexusmods.com/suikoden1and2hdremaster/mods/6)** sprites (optional, but this mod is designed for them)
4. Place **PKextended.dll** in `BepInEx\plugins\`
5. Launch game (config auto-generates in `BepInEx\config\faospark.pkextended.cfg`)

## Configuration

```ini
[Sprite Filtering]
# Texture filtering quality: 0 (off), 1 (low), 2 (medium), 3 (high)
# Default: 3 (best for Project Kyaro sprites)
SpriteFilteringQuality = 3

# Mipmap bias: -0.5 (sharper), 0 (neutral), 0.5 (softer)
SpriteMipmapBias = -0.5

[Display]
# Enable performance scaling (true/false)
EnableResolutionScaling = true

# Scale: 0.5 (faster), 0.75 (balanced), 1.0 (native), 1.5+ (quality)
ResolutionScale = 1.0

# Enable borderless fullscreen window mode
EnableBorderlessWindow = false

[Visual]
# Remove post-processing effects from sprites (true/false)
DisableSpritePostProcessing = true

[Controller]
# Force specific controller button prompts (true/false)
ForceControllerPrompts = false

# Controller type:
# - PS4: "PlayStation", "PlayStation4", "DS4", "PS4"
# - PS5: "PlayStation5", "DualSense", "PS5"
# - Xbox/Generic: "Xbox", "Generic"
ControllerPromptType = PlayStation
```

## Quick Presets

**Default (Recommended for Project Kyaro)**
```ini
SpriteFilteringQuality = 3
ResolutionScale = 1.0
```

**Performance Mode**
```ini
SpriteFilteringQuality = 2
ResolutionScale = 0.75
```

**Pure Pixel Art (No Smoothing)**
```ini
SpriteFilteringQuality = 0
DisableSpritePostProcessing = true
```

## Important Notes

**Using with Suikoden Fix?**  
Disable its resolution settings if using `ResolutionScale`:
```ini
# In d3xMachina.suikoden_fix.cfg
Width = -1
Height = -1
Fullscreen = -1
```

## Troubleshooting

**Sprites look blurry?** → Increase `SpriteMipmapBias` to `0` or `-0.25`  
**White outlines on sprites?** → Decrease `SpriteMipmapBias` to `-1.0`  
**Performance issues?** → Set `ResolutionScale = 0.75` or `0.5`  
**Want original look?** → Set `SpriteFilteringQuality = 0`  
**Controller prompts not changing?** → Check `ForceControllerPrompts = true` and verify `ControllerPromptType`

## Changelog

### Version 1.5 (Current)
**New Features:**
- **Controller Prompt Override**: Force specific controller button icons
  - PlayStation (PS4/PS5), Xbox, and Generic controller support
  - Global sprite swapping system works throughout entire game
  - Smart minigame button cycling for Xbox → PlayStation conversion
  - All UI contexts supported (battle, menus, dialogue, configuration)

**Improvements:**
- Improved configuration documentation

### Version 1.0 (Initial Release)
**Features:**
- **Sprite Smoothing**: Texture filtering for upscaled sprites
  - 4 quality levels: High, Medium, Low, Off
  - Mipmap bias control
  - Anisotropic filtering (2x - 8x)
- **Resolution Scaling**: Performance optimization (0.5x - 2.0x)
- **Post-Processing Control**: Disable sprite effects
- **Borderless Window Mode**: Fullscreen windowed mode support

## Development

This mod uses BepInEx 6 IL2CPP and Harmony for runtime patching.

## License

MIT License - See LICENSE.txt

## Credits

**Author**: faospark  
**For**: [Project Kyaro](https://www.nexusmods.com/suikoden1and2hdremaster/mods/6) HD sprite pack  
**Compatible with**: [Suikoden Fix](https://github.com/d3xMachina/BepInEx.Suikoden) by d3xMachina
