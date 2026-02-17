# Changelog

All notable changes to PKCore (formerly PKextended) will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

> **Note**: This changelog primarily documents changes specific to PKCore. Version numbers may not align with the Project Kyaro Suite releases on NexusMods, as PKCore development may progress independently.

## [2026.02.2] - 2026-02-17

Added

* Parsing of text placeholders to reflect custom names of protagonists and headquarters from save files.
* Restored option to toggle`LogReplaceableTexture`, allowing control over detailed log volume when paired with text logs.

Improved

* Borderless window patch now provides a true borderless windowed mode, distinct from the render scaling patch activator.
* Suikoden 1 Dialog Scaling implementation
* Log reporting for the custom texture patch now displays actual file index names instead of indexed variants.
* Logging for the custom texture patch has been further refined for clarity.

Renamed

* `NPCPortraits` class →`PortraitSystemPatch` to better reflect its expanded functionality beyond NPC portraits.
* `DisableMask` →`DisablePortraitDialogMask` for clearer naming consistency.

Removed

* Texture file name sanitation, which previously caused collisions with files that had identical names once stripped with hash and prefix.
* Redundant`ReactionMonitor`

## [2026.02.0] - 2026-02-09

### Added

- **Reaction Portrait UI**: Enhanced visual presentation with fade-in animation, removed purple background, and fixed dimensions (300x300 at x:636.85, y:-262.34).

### Changed

- **Log Cleanup**: Suppressed `DisablePortraitDialogMask` log spam; messages now only appear when `DetailedTextureLog` is enabled.
- **Sprite Logging**: Reduced log spam for `GRSpriteRenderer` by coalescing sprite variations (e.g. `person_vj10_01_XX`) into a single log entry.
- **Jowy Disguise**: Reverted multi-character support for Jowy's disguise.

### Fixed

- **Hidden Config**: Fixed CS0029 compilation error in `ModConfiguration.cs`.

### Removed

- **Dialog Chaining**: Removed dialog chaining functionality (Interceptor Pattern).

## [2026.01.1] - 2026-02-05

### Changed

- **Configuration Cleanup**: Moved debug options (`LogReplaceableTextures`, `LogTexturePaths`) to hidden settings to declutter the user config file.
- **Documentation**: Comprehensive update to `Caching.md` detailing technical priority layers, DDS handling, and memory persistence.

### Added

- **World Map Clouds**: Disabled `sm_wk_cloud` objects to remove visual clutter on the world map.
- **World Map Sunrays**: Corrected sunray effects on the world map for consistent visuals.

## [2026.01.0] - 2026-01-31

### Added

- **SpriteAtlas Support**: Implemented post-fix interception for Unity SpriteAtlas to allow texture replacement for packed sprites using loose files.
- **MinimalUI Configuration**: New boolean option to control minimal UI texture loading for cleaner game experience.
- **Revised Texture Filtering System**: Selective loading system for texture mods (Project Kyaro, Launcher, Minimal UI) via configuration options.
- **Enhanced UI Refresh**: Improved MenuTopPartyStatus texture replacement with UIMainMenu.Open patch for proper atlas refresh.

### Changed

- **Version Numbering**: Updated to year-based versioning format (2026.01.0) for better release tracking.
- **Dialog System**: Refactored DialogBoxScale to boolean ScaleDownDialogBox (false=Large, true=Medium) for simplified configuration.
- **Texture Caching**: Enhanced cache invalidation logic to include MinimalUI setting for proper texture reloading.
- **NPC Portrait Loading**: Streamlined portrait directory scanning and loading logic for better performance.

### Fixed

- **Duplicate Text Logging**: Added HashSet tracking in TextDatabasePatch to prevent duplicate log entries.
- **Save Point Messages**: Corrected log message formatting for save point sprite replacement.
- **Battle Sprite Detection**: Simplified and improved battle sprite detection logic in DisableSpritePostProcessingPatch.
- **War Character Stats**: Enhanced configurable war abilities and character stats system for Suikoden 2.

### Refactored

- **Sprite Filtering Configuration**: Renamed and simplified sprite filtering settings for better clarity.
- **Menu Transformations**: Enhanced UI menu transformation logic and footer container handling.
- **Asset Loading System**: Improved core asset loading with better asynchronous operations and DDS support.

## [2.1.0] - 2026-01-23

### Added - Performance & Advanced Systems

- **Native DDS Support**: Load pre-compressed `.dds` files (BC1/BC3/BC7) for reduced VRAM usage and faster load times.
- **Manifest Caching**: XML-based texture index cache system dramatically reduces startup time.
- **SMAA Anti-Aliasing**: High-quality **Subpixel Morphological Anti-Aliasing** can now be applied to the main camera.
- **Experimental Object Insertion**: Framework to add entirely new static objects to scenes via JSON configuration (`fixed_objects.json`).
- **Config Folder Management**: All local configuration JSONs (War stats, Dialog overrides) now reside in `PKCore/Config/`.

### Added - Expanded Feature Set

#### Enhanced Dialogue System

- **Custom NPC Portraits**: Inject custom high-resolution portraits for NPCs that don't have them in the base game.
- **Text & Speaker Overrides**: Advanced JSON-based system to replace any line of dialogue in the game and inject custom speaker names using `<speaker:Name>` tags.
- **Portraits Storage**: Place PNG files named after the NPC in `PKCore/NPCPortraits/`.

#### Visual Improvements

- **Portrait Mask Removal**: Option to disable the Face_Mask_01 overlay on character portraits (`DisablePortraitDialogMaskPortraitDialog`), providing a cleaner look for custom portraits.
- **Colored Intro and Flashbacks**: Restores full color to Suikoden 2's intro and flashback sequences by disabling the game's sepia/monochrome filter.
- **Classic Save Window**: Mimic the feel of the PSX version with a simpler, fullscreen Save/Load interface for Suikoden 2.
- **Save Point Customization**: Choose from 5 color variants and optionally disable the glow effect.

#### Specialized Suikoden 2 Support

- **Summon Effect Replacement**: New `SummonMonitor` system proactively replaces textures for summon effects (Eff_tex_Summon_*) which was previously unsupported.
- **War Battle Modding**: Modify character stats and abilities for Suikoden 2 war battles via `S2WarAbilities.json`.
- **Texture Variants**: Toggle alternate textures for Tir's run cycle (`TirRunTexture`) and the Mercenary Fortress fence (`MercFortFence`).

#### UI Scaling & Customization

- **Dialog Box Presets**: Adjust dialog window size (Large/Medium/Small) for a less obstructive view.
- **Menu Layout Scaling**: Smaller layout preset for main game menus (`ScaledDownMenu`).
- **Borderless Window Mode**: Full support for borderless fullscreen window mode.
- **Cursor Visibility**: Option to show/hide the system mouse cursor over the game window.

---

## [2.0.0] - 2025-12-23

### Added - Save Point Customization

#### Save Point Color Variants

Choose from 5 color options for save point orbs:

- **Available Colors**: blue, red, yellow, pink, green, default
- **Configuration**: `SavePointColor = pink` in `[Custom Textures]` section
- **Implementation**: Texture variant system in `TextureOptions.cs`
- **Usage**: Place color variants in `Textures/SavePoint/` folder as `t_obj_savePoint_ball_<color>.png`
- **Automatic Fallback**: If selected color variant not found, uses default texture

#### Save Point Glow Disable

Remove the glow effect from save point orbs:

- **Configuration**: `DisableSavePointGlow = true` (default: enabled)
- **Implementation**: `SavePointMonitor.cs` disables `Glow_add` GameObject
- **Benefit**: Cleaner appearance for custom save point textures

### Improved - Manifest Cache System

#### Config-Aware Cache Invalidation

Texture manifest cache now tracks configuration changes:

- **Tracked Settings**: `LoadLauncherUITextures`, `SavePointColor`, `EnableProjectKyaroSprites`
- **Automatic Rebuild**: Texture index rebuilds when any tracked config setting changes
- **User Experience**: No more manual cache deletion when changing texture settings
- **Implementation**: `ConfigHash` field in `TextureManifest` class
- **Logging**: Shows "Config changed - rebuilding texture index" when settings change

### Added - Texture Variant System

#### Centralized Variant Handling

New `TextureOptions.GetTextureNameWithVariant()` method:

- **Purpose**: Handle texture variants (like save point colors) in one place
- **Extensible**: Easy to add new texture variant types
- **Organization**: All texture-related options visible at a glance in `TextureOptions.cs`

---

## [1.6.0] - 2025-12-20

### Changed - Project Rebranding

> **⚠️ BREAKING CHANGE**: This release includes a project rename that requires manual migration for existing users.

- **Project Name**: `PKextended` (Project Kyaro Extended) → **`PKCore`** (Project Kyaro Core)
- **Plugin ID**: `faospark.pkextended` → `faospark.pkcore`
- **DLL Name**: `PKextended.dll` → `PKCore.dll`
- **Config File**: `faospark.pkextended.cfg` → `faospark.pkcore.cfg`
- **Custom Textures Folder**: `BepInEx/plugins/PKextended/Textures/` → `BepInEx/plugins/PKCore/Textures/`

**Migration Steps for Existing Users:**

1. Rename your custom textures folder from `PKextended` to `PKCore` (if you have custom textures)
2. Remove old `PKextended.dll` from `BepInEx/plugins/`
3. Add new `PKCore.dll` to `BepInEx/plugins/`
4. (Optional) Copy settings from old config file to new one, or let it regenerate with defaults

**Rationale:**
The name "Extended" implied adding new features to the game, but this mod is the **core foundation** for how Project Kyaro works now. Previously, Project Kyaro relied on Special K for texture replacement, but PKCore now provides native BepInEx-based texture replacement, sprite filtering, and visual enhancements. "Core" accurately reflects its role as the essential enhancement suite that powers Project Kyaro.

### Improved

- **Reduced Log Spam**: Texture replacements are now logged only once per texture instead of multiple times when accessed through different code paths
- **Simplified Texture Loading**: Removed verbose "Replaced texture", "Texture scaling", and "Created and cached sprite" messages - only essential "Loaded and cached" message remains
- **Concise Texture List**: Startup now shows "Indexed X custom texture(s) ready to use" instead of enumerating all textures (detailed list still available when `DetailedLogs = true`)
- **Priority Override System**: Added `00-Mods/` folder with highest priority - textures here override base textures, allowing users to add custom texture mods without modifying base packs
- **New Config Option**: `DetailedLogs` - enables/disables detailed texture logging (replacement confirmations and full texture list on startup). Disable for silent operation.

### Fixed

- **Bath Background In-Game Switcher**: Custom bath backgrounds now work with in-game switching
  - **Known Limitation**: Visual update requires screen refresh (exit and re-enter bath scene) to display the new custom background immediately. The sprite is replaced in memory correctly, but Unity's render pipeline doesn't automatically redraw until triggered by a scene change or menu interaction.

---

## [1.5.1] - 2025-12-16

### Added - Custom Texture Replacement System

> **⚠️ IMPORTANT**: This feature has **PARTIAL COVERAGE** and works on certain sprites only, not all game textures.

#### PNG Texture Loading

Replace game sprites/textures with custom PNG files:

- **Works Best For**: Event backgrounds, UI elements, static sprites, bath backgrounds
- **Limited Support**: Animated UI sprites, character sprites, battle backgrounds
- **Not Supported**: Summon effects (`Eff_tex_Summon_*.png`)
- **Legacy Option**: Use with [SpecialK](https://www.special-k.info/) for comprehensive texture replacement including summons

**Supported Formats:**

- PNG, JPG, JPEG, and TGA formats
- Automatic subfolder scanning for organization

**Usage:**

1. Enable `EnableCustomTextures = true` in config
2. Place PNG files in `BepInEx/plugins/PKCore/Textures/` (was `PKextended/Textures/` in v1.5.1)
3. Name files exactly as the original texture (use `LogReplaceableTextures = true` to discover names)
4. Supports subfolders for organization

#### Priority Override System (Added in v1.6.0)

- **`00-Mods/` folder** has **highest priority** - textures here override base textures
- Recommended structure:
  ```
  Textures/
  ├── GSD1/              ← Base textures
  ├── GSD2/              ← Base textures
  └── 00-Mods/           ← Your custom mods (HIGHEST PRIORITY)
      ├── MyMod1/
      └── MyMod2/
  ```
- Example: If both `GSD1/launcher_menu_gs1.png` and `00-Mods/MyMod/launcher_menu_gs1.png` exist, the mod version will be used
- Users can add their own texture mods to `00-Mods/` without modifying base texture packs

#### Texture Discovery Mode

- Enable `LogReplaceableTextures` to see all detectable sprites
- Each texture logged only once to avoid spam
- Helps identify which textures can be customized
- Check BepInEx console for `[Replaceable Sprite]` and `[Replaceable UI Sprite]` messages

#### Scene-Based Detection

- Detects `SpriteRenderer` components (world sprites)
- Detects `Image` components (UI sprites)
- Detects `RawImage` components (UI textures)

### Changed

- Suppressed harmless `Addressables.Release` warnings after texture replacement
- Updated version to 1.5.1

### Technical Details

- **Harmony Patches**: Intercepts sprite/texture setters on multiple components
- **Caching System**: Loaded textures cached for performance
- **Index Building**: Maps texture names to file paths for fast lookup

---

## [1.5.0] - 2025-12-11

### Added - Controller Prompt Override System

Force specific controller button icons regardless of detected controller:

#### Multi-Platform Support

- **PlayStation 4** (DualShock 4): `_01` sprite suffix
- **PlayStation 5** (DualSense): `_02` sprite suffix
- **Xbox**: `_00` sprite suffix

#### Flexible Configuration

Accepts multiple string variations:

- **PS4**: "PlayStation", "PlayStation4", "DS4", "PS4"
- **PS5**: "PlayStation5", "DualSense", "PS5"
- **Xbox/Generic**: "Xbox", "Generic", "Switch"

#### Features

- **Global Sprite Swapping**: Automatic controller button icon replacement throughout entire game
  - Patches `UnityEngine.UI.Image.sprite` setter for universal coverage
  - Works in all UI contexts: battle, menus, dialogue, configuration, minigames
- **Smart Minigame Button Cycling**: Intelligent button sequence conversion for minigames
- **Sprite Caching System**: Performance optimization for sprite lookups

### Changed

- **Configuration Structure**: Added new `[Controller]` section with `ForceControllerPrompts` and `ControllerPromptType` settings
- **Documentation**: Expanded README.md with controller prompt override feature descriptions
- **Plugin Architecture**: Modular patch system for controller features

### Technical Details

#### Implementation

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

### Added - Core Features

#### Sprite Filtering System

Designed for **Project Kyaro's upscaled sprites** - adds granular texture filtering options:

**Quality Presets:**

- **Level 3 (High)** - Default, recommended for Project Kyaro
  - Trilinear filtering + 8x Anisotropic filtering
  - Best quality, smooth appearance
- **Level 2 (Medium)** - Balanced
  - Trilinear filtering + 4x Anisotropic filtering
  - Good quality with better performance
- **Level 1 (Low)** - Performance
  - Bilinear filtering + 2x Anisotropic filtering
  - Faster, slight quality reduction
- **Level 0 (Off)** - Pure pixel art
  - No filtering - original pixel art appearance
  - For base game without upscaled sprites

**Mipmap Bias Control:**

- Fine-tune sharpness (-1.0 to 1.0)
- Default: -0.5 for sharp, anti-aliased look
- Prevents white outlines on upscaled sprites
- Really more about your choice on how you want Project Kyaro Sprites to look

**Global Application:**

- Automatic application to all game sprites via Harmony patches
- No manual sprite tagging required

#### Display Features

**Resolution Scaling:**

- Internal rendering resolution control (0.5x - 2.0x)
- Performance optimization for lower-end systems (0.5x - 0.75x)
- Quality enhancement for high-resolution displays (1.5x - 2.0x)
- Dynamic scaling without game restart
- Recommended: 1.0x (native) for balanced experience

**Borderless Window Mode:**

- Fullscreen windowed mode support
- Instant alt-tab functionality
- Better multi-monitor compatibility
- Native window frame removal
- Only use this if you don't want to use the in-game full screen option

#### Visual Enhancements

**Sprite Post-Processing Control:**

- Selective disable for sprite effects
- Keeps post-processing on backgrounds
- Removes effects from character sprites
- Prevents over-processing artifacts
- **Crucial for battle**: Disables game effects that affect sprites (e.g., sandstorm effects appearing on battle sprite seams)

### Technical Details

#### Sprite Filtering Implementation

- **Patches**: `Sprite.texture` getter interception via Harmony
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
