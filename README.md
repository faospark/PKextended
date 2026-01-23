# PKCore - Project Kyaro Core

**By faospark**  
**Current Version: 2.0.0**

The **core DLL component** for **[Project Kyaro](https://www.nexusmods.com/suikoden1and2hdremaster/mods/6)** that provides sprite smoothing, anti-aliasing, texture replacement, and visual enhancements for **Suikoden I & II HD Remaster**.

> **Note**: This repository contains the **source code for the PKCore.dll** file. The complete Project Kyaro mod package (including upscaled textures) is available on [Nexus Mods](https://www.nexusmods.com/suikoden1and2hdremaster/mods/6).

Built as the foundational enhancement suite for Project Kyaro's upscaled sprites, replacing the previous Special K dependency with native BepInEx integration.

## Requirements

- **BepInEx 6.0.0-pre.2 IL2CPP**
- **Suikoden I & II HD Remaster** (Unity 2022.3.28f1)
- **Highly Recommended**: [Suikoden Fix](https://github.com/d3xMachina/BepInEx.Suikoden) for best experience

## Features

### Sprite Smoothing
Designed for **Project Kyaro's upscaled sprites** - adds granular texture filtering with 4 quality levels (High/Medium/Low/Off) and mipmap bias control to prevent white outlines.

### Custom Texture Replacement

> **🆕 NEW FEATURE**: Texture replacement support in any Project Kyaro. This feature eliminates the need for Special K for texture loading.

Replace game textures with custom PNG files in `BepInEx/plugins/PKCore/Textures/`. Use `00-Mods/` subfolder for highest priority custom mods.

**Performance Optimization**: Built-in caching system dramatically improves boot times and eliminates runtime stuttering. See **[CACHING.md](CACHING.md)** for technical details.

### Controller Prompt Override
Force specific controller button icons (PlayStation/Xbox) regardless of detected controller. Works throughout entire game including minigames.

### Visual Enhancements
- **Post-Processing Control**: Disable effects on sprites while keeping them on backgrounds
- **Resolution Scaling**: Adjust internal resolution for performance (0.5x - 2.0x)
- **Borderless Window Mode**: Instant alt-tab and multi-monitor support

## Installation

1. Install **BepInEx 6.0.0-pre.2 IL2CPP**
2. Install **[Suikoden Fix](https://github.com/d3xMachina/BepInEx.Suikoden)** (highly recommended)
3. Install **[Project Kyaro](https://www.nexusmods.com/suikoden1and2hdremaster/mods/6)** sprites (optional, but this mod is designed for them)
4. Place **PKCore.dll** in `BepInEx\plugins\`
5. Launch game (config auto-generates in `BepInEx\config\faospark.pkcore.cfg`)

## Configuration

Config file auto-generates at `BepInEx\config\faospark.pkcore.cfg` on first launch.

**Key Settings:**
```ini
[Sprite Filtering]
SpriteFilteringQuality = 3        # 0-3: Off/Low/Medium/High (default: 3)
SpriteMipmapBias = -0.5           # -0.5 (sharper) to 0.5 (softer)

[Display]
ResolutionScale = 1.0             # 0.5-2.0: Performance to quality
EnableBorderlessWindow = false

[Visual]
DisableSpritePostProcessing = true

[Controller]
ForceControllerPrompts = false
ControllerPromptType = PlayStation  # PlayStation/PlayStation5/Xbox

[Custom Textures]
EnableCustomTextures = false
LoadLauncherUITextures = true     # Load custom launcher UI
SavePointColor = pink              # blue/red/yellow/pink/green/default
DisableSavePointGlow = true        # Remove glow effect from save points
LogReplaceableTextures = false     # Discovery mode
DetailedTextureLog = false         # Verbose logging
```

**Quick Presets:**
- **Default**: `SpriteFilteringQuality = 3`, `ResolutionScale = 1.0`
- **Performance**: `SpriteFilteringQuality = 2`, `ResolutionScale = 0.75`
- **Pixel Art**: `SpriteFilteringQuality = 0`

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
**Slow boot times?** → Cache builds on first run with custom textures; subsequent boots are much faster (see [CACHING.md](CACHING.md))

## Changelog

See **[CHANGELOG.md](CHANGELOG.md)** for detailed version history and technical implementation details.

**Latest (v2.0.0):**
- **Save Point Customization**: Choose from 5 color variants (blue/red/yellow/pink/green) and disable glow effect
- **Config-Aware Manifest Cache**: Texture index automatically rebuilds when config settings change
- **Texture Variant System**: Centralized texture variant handling in TextureOptions.cs

## Migration Guide (PKextended → PKCore)

If you have **PKextended.dll** installed from a previous version:

1. **Remove Old DLL**:
   - Delete `PKextended.dll` from `BepInEx/plugins/`
   - Add new `PKCore.dll` to `BepInEx/plugins/`

2. **Config File** (optional - will auto-generate):
   - Old config: `BepInEx/config/faospark.pkextended.cfg`
   - New config: `BepInEx/config/faospark.pkcore.cfg`
   - You can copy your old settings to the new file, or let it regenerate with defaults

## Development

This mod uses BepInEx 6 IL2CPP and Harmony for runtime patching.

## License

MIT License - See LICENSE.txt

## Credits

**Author**: faospark  
**For**: [Project Kyaro](https://www.nexusmods.com/suikoden1and2hdremaster/mods/6) HD sprite pack  
**Compatible with**: [Suikoden Fix](https://github.com/d3xMachina/BepInEx.Suikoden) by d3xMachina
