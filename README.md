# PKCore - Project Kyaro Core

**By faospark**
**Current Version: 2026.01.0**

The **core DLL component** for **[Project Kyaro](https://www.nexusmods.com/suikoden1and2hdremaster/mods/6)** that provides a variety of features such as texture replacement framework, sprite smoothing, anti-aliasing, graphical and visual enhancement, and some gameplay enhancement for **Suikoden I & II HD Remaster**.

> **Note**: This repository contains the **source code for the PKCore.dll** file. The complete Project Kyaro mod package (including upscaled textures) is available on [Nexus Mods](https://www.nexusmods.com/suikoden1and2hdremaster/mods/6).

Built as the foundational enhancement suite for Project Kyaro's upscaled sprites, replacing the previous Special K dependency and now with native BepInEx integration.

## Requirements

- **BepInEx 6.0.0-pre.2 IL2CPP**
- **Suikoden I & II HD Remaster** (U`nity 2022.3.28f1)
- **Highly Recommended**: [Suikoden Fix](https://github.com/d3xMachina/BepInEx.Suikoden) for best experience

## Features

### Sprite Smoothing & Anti-Aliasing (SMAA)

Designed for **Project Kyaro's upscaled sprites** - adds granular texture filtering with 4 quality levels (High/Medium/Low/Off) and mipmap bias control. Now includes native **SMAA (Subpixel Morphological Anti-Aliasing)** for the smoothest possible edges.

### Custom Texture Replacement (PNG & DDS)

> **🆕 NOW SUPPORTS DDS**: Texture replacement support in any Project Kyaro. Support for pre-compressed **DDS files** (BC1/BC3/BC7) for zero-stall loading and reduced VRAM.

Replace game textures with custom PNG or DDS files in `BepInEx/plugins/PKCore/Textures/`. Use `00-Mods/` subfolder for highest priority custom mods.

**Performance Optimization**: Built-in BC1/BC3 runtime compression and manifest caching dramatically improve boot times and eliminate runtime stuttering. See **[Caching.md](Caching.md)** for technical details.

### Advanced Customization

- **NPC Portraits**: Inject custom high-resolution portraits for any NPC.
- **War Battle Modding**: Customize character stats and abilities in Suikoden 2 war battles via JSON.
- **UI Scaling**: Presets for dialog box size and menu layout scaling.
- **Classic UI**: Revert Suikoden 2 save windows to the classic PSX look.
- **Controller Prompt Override**: Force specific button icons (Xbox/PS4/PS5/Switch) regardless of detected controller.
- **Experimental Object Insertion**: Add new static objects to scenes via configuration.

## Installation

1. Install **BepInEx 6.0.0-pre.2 IL2CPP**
2. Install **[Suikoden Fix](https://github.com/d3xMachina/BepInEx.Suikoden)** (highly recommended)
3. Install **[Project Kyaro](https://www.nexusmods.com/suikoden1and2hdremaster/mods/6)** sprites (optional, but this mod is designed for them)
4. Place **PKCore.dll** in `BepInEx\plugins\`
5. Launch game (config auto-generates in `BepInEx\config\faospark.pkcore.cfg`)

## Configuration

Config file auto-generates at `BepInEx\config\faospark.pkcore.cfg` on first launch.



**Latest (v2.1.0):**
- **Native DDS Support**: Load pre-compressed DDS files for faster loading.
- **Runtime Compression**: Automatic BC1/BC3 compression for PNGs.
- **SMAA Anti-Aliasing**: High-quality edge smoothing for the main camera.
- **S2 Summon Replacement**: Support for replacing summon effect textures.
- **NPC Portraits**: Inject custom portraits into the dialog system.
- **War Battle Modding**: Configurable stats for Suikoden 2 war battles.
- **UI scaling**: Presets for dialog box and menu sizes.

- **Save Point Customization**: Choose from 5 color variants (blue/red/yellow/pink/green) and disable glow effect
- **Config-Aware Manifest Cache**: Texture index automatically rebuilds when config settings change
- **Texture Variant System**: Centralized texture variant handling in TextureOptions.cs

## Migration Guide (PKextended → PKCore)

If you have **PKextended.dll** installed from a previous version:

1. **Remove Old files**:
   - Delete `PKextended.dll` from `BepInEx/plugins/`
   - Old config: `BepInEx/config/faospark.pkextended.cfg`
   - Delete or Back-up `dxgi.dll` or `d3d11.dll` of what every .dll name you loaded SpecialK

2. **Config File** (optional - will auto-generate):

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

**Special Thanks**: d3xMachina for [Suikoden Fix](https://github.com/d3xMachina/Suikoden-Fix) - NPC portrait functionality is based on his text database patches for dialog detection and speaker identification.
