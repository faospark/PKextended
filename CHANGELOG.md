# Changelog

All notable changes to PKextended will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.5.0] - 2025-12-11

### Added

#### Controller Prompt Override System
- **Global Sprite Swapping**: Automatic controller button icon replacement throughout entire game
  - Patches `UnityEngine.UI.Image.sprite` setter for universal coverage
  - Works in all UI contexts: battle, menus, dialogue, configuration, minigames
- **Multi-Platform Support**:
  - **PlayStation 4** (DualShock 4): `_01` sprite suffix
  - **PlayStation 5** (DualSense): `_02` sprite suffix  
  - **Xbox**: `_00` sprite suffix
- **Flexible Configuration Strings**:
  - PS4: "PlayStation", "PlayStation4", "DS4", "PS4"
  - PS5: "PlayStation5", "DualSense", "PS5"
  - Xbox/Generic: "Xbox", "Generic", "Switch"
- **Smart Minigame Button Cycling**: 
- **Sprite Caching System**: Performance optimization for sprite lookups


### Changed
- **Configuration Structure**: Added new `[Controller]` section
- **Documentation**: Expanded README.md with new feature descriptions
- **Plugin Architecture**: Modular patch system for controller features

### Technical Details

#### Controller Prompt Implementation
- **Global Interception**: Harmony prefix patch on `Image.sprite` setter
- **Pattern Detection**: Automatic detection of controller sprites by `_00/_01/_02` suffix
- **Suffix Mapping**:
  - `_00` = Xbox sprites
  - `_01` = PS4 sprites
  - `_02` = PS5 sprites
- **Button Cycle Logic**: Counter-based system for Xbox generic button → PlayStation conversion
- **Resource Discovery**: Uses `Resources.FindObjectsOfTypeAll<Sprite>()` for sprite loading

---

## [1.0.0] - Initial Release

### Added

#### Sprite Filtering System
- **Quality Presets**: 4 levels of texture filtering optimized for upscaled sprites
  - **Level 3 (High)**: Trilinear filtering + 8x Anisotropic filtering
  - **Level 2 (Medium)**: Trilinear filtering + 4x Anisotropic filtering
  - **Level 1 (Low)**: Bilinear filtering + 2x Anisotropic filtering
  - **Level 0 (Off)**: No filtering - pure pixel art mode
- **Mipmap Bias Control**: Fine-tune sharpness (-1.0 to 1.0)
  - Default: -0.5 for sharp, anti-aliased look
  - Prevents white outlines on upscaled sprites
- **Global Sprite Patching**: Automatic application to all game sprites via Harmony

#### Display Features
- **Resolution Scaling**: Internal rendering resolution control (0.5x - 2.0x)
  - Performance optimization for lower-end systems
  - Quality enhancement for high-resolution displays
  - Dynamic scaling without game restart
- **Borderless Window Mode**: Fullscreen windowed mode support
  - Instant alt-tab functionality
  - Better multi-monitor compatibility
  - Native window frame removal

#### Visual Enhancements
- **Sprite Post-Processing Control**: Selective disable for sprite effects
  - Keeps post-processing on backgrounds
  - Removes effects from character sprites
  - Prevents over-processing artifacts

### Technical Details

#### Sprite Filtering Implementation
- **Patches**: `Sprite.texture` getter interception
- **Filter Application**: Runtime texture property modification
  - `filterMode` (Point/Bilinear/Trilinear)
  - `anisoLevel` (0/2/4/8)
  - `mipMapBias` (-1.0 to 1.0)
- **Caching**: Tracks processed textures to avoid redundant operations

#### Resolution Scaling Implementation
- **Patches**: `Screen` resolution getters
- **Dynamic Scaling**: Real-time resolution override without scene reload

#### Post-Processing Implementation
- **Selective Disabling**: Layer-based or sprite-specific effect removal

### Compatibility
- **Framework**: BepInEx 6.0.0-pre.2 IL2CPP
- **Game Version**: Suikoden I & II HD Remaster (Unity 2022.3.28f1)
- **Recommended**: Works best with [Suikoden Fix](https://github.com/d3xMachina/BepInEx.Suikoden) by d3xMachina
- **Designed For**: [Project Kyaro](https://www.nexusmods.com/suikoden1and2hdremaster/mods/6) HD sprite pack

---

## Version Format

Format: `[MAJOR.MINOR.PATCH]`
- **MAJOR**: Incompatible API changes or major feature overhauls
- **MINOR**: New features, backward compatible
- **PATCH**: Bug fixes, backward compatible

## Categories

- **Added**: New features
- **Changed**: Changes to existing functionality
- **Deprecated**: Soon-to-be removed features
- **Removed**: Removed features
- **Fixed**: Bug fixes
- **Security**: Security vulnerability fixes
