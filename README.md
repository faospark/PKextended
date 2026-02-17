# PKCore - Project Kyaro Core

**By faospark**
**Current Version: 2026.2.2**

The **core DLL component** for **[Project Kyaro](https://www.nexusmods.com/suikoden1and2hdremaster/mods/6)** that provides a variety of features such as texture replacement framework, sprite smoothing, anti-aliasing, graphical and visual enhancement, and some gameplay enhancement for **Suikoden I & II HD Remaster**. As of 2026, PKExtended has been Transformed to PKCore as this is now the backbone of Project Kyaro . Special K has now been removed as a Dependecy .

> **Note**: This repository contains the **source code for the PKCore.dll** file. The complete Project Kyaro mod package (including upscaled textures) is available on [Nexus Mods](https://www.nexusmods.com/suikoden1and2hdremaster/mods/6).

## Features


**[Can Be Toggled]**

* Re-Texture Framework
* Upscaled Smooth Sprites (can be toggled off)
* Sprite Filtering and Anti-Aliasing (SMAA)
* NPC Portraits Framework
* Comprehensive Portrait Variant Support
* Colored Intros and Flashbacks
* Boosted War Battles (S2)
* Button Controller Prompt Support (expanded to include more options)
* Alternative Launcher (based on unused in‑game assets)
* Minimal UI
* Classic‑ish Save Menu UI
* Disable Dialogue Portrait Mask
* Scaled‑Down Dialogue Box
* Scaled‑Down Menu
* Customizable Save Point Crystal Color
* Disable White Save Point Glow (reduces the washed‑out look of the crystal)
* Disable World Map Clouds
* Disable World Map Sun Rays
* Scaled‑Down World Map of Suikoden 1
* Alternative Run Animation for Tir (Project Kyaro sprites)
* Restore Bamboo Fence on Mercenary Fortress

**[Performance Features]**

* Texture Manifest Cache
* Texture Memory Caching
* Resolution Scaling

**[Baked In]**

* Automatic Text Placeholder Replacement (protagonist/HQ names from save data)
* Added Headquarters Signage (S1)
* Fixed Backgrounds with Bad Baked DOF (S2)
* Fixed Inconsistent Telescope on HQ (S2)
* Replaced Low‑Quality Upscaled Doors in the Inn Area (S2)

**[Utility]**
* Show Mouse Cursor
* Clean Exit on ALT+F4 (Force immediate shutdown instead of hanging)

> **Borderless Mode**: For borderless fullscreen window, use Unity's native `-popupwindow` launch argument:
> - **Steam**: Right-click game → Properties → Launch Options → Add `-popupwindow`
> - **Epic Games**: Create a shortcut to the game executable and add `-popupwindow` to the Target field

**[Additionals from Nexus Bundle]**

* Restore the Dash in the Letter “Z” via Custom Font
* Alternate CRC Bypass for Version 1.0.4



Replace game textures with custom PNG or DDS files in `BepInEx/plugins/PKCore/Textures/`. Use `00-Mods/` subfolder for highest priority custom mods.

**Performance Optimization**: Built-in manifest caching dramatically improves boot times. DDS format support allows pre-compressed textures (BC1/BC3/BC7) for reduced VRAM usage and faster loading.

### Advanced Customization

- **Placeholder Text Replacement**: Automatic replacement of protagonist and HQ name placeholders in dialogs. See [Placeholder Text Guide](Documentation/PlaceholderText.md).
- **NPC Portraits**: Inject custom high-resolution portraits for any NPC.
- **War Battle Modding**: Customize character stats and abilities in Suikoden 2 war battles via JSON.
- **UI Scaling**: Presets for dialog box size and menu layout scaling.
- **Classic UI**: Revert Suikoden 2 save windows to the classic PSX look.
- **Controller Prompt Override**: Force specific button icons (Xbox/PS4/PS5/Switch) regardless of detected controller.
- **Experimental Object Insertion**: Add new static objects to scenes via configuration.


## Requirements

- **BepInEx 6.0.0-pre.2 IL2CPP**
- **Suikoden I & II HD Remaster** (U`nity 2022.3.28f1)
- **Highly Recommended**: [Suikoden Fix](https://github.com/d3xMachina/BepInEx.Suikoden) for best experience

## Installation

1. Install **BepInEx 6.0.0-pre.2 IL2CPP**
2. Install **[Project Kyaro](https://www.nexusmods.com/suikoden1and2hdremaster/mods/6)**
3. Launch game (config auto-generates in `BepInEx\config\faospark.pkcore.cfg`)

## Configuration

Config file auto-generates at `BepInEx\config\faospark.pkcore.cfg` on first launch.

## Migration Guide (PKextended → PKCore)

If you have **PKextended.dll** or Older Versions of Project Kyaro installed from a previous version:

1. **Remove Old files**:

   - Delete `PKextended.dll` from `BepInEx/plugins/`
   - Old config: `BepInEx/config/faospark.pkextended.cfg`
   - Delete or Back-up `dxgi.dll` or `d3d11.dll` of what every .dll name you loaded SpecialK
   - Delete all of the texture file `SK_Res\inject\textures` as they are now necessesary
2. **Config File** (optional - will auto-generate):

   - New config: `BepInEx/config/faospark.pkcore.cfg`

## Development

This mod uses BepInEx 6 IL2CPP and Harmony for runtime patching.

## License

MIT License - See LICENSE.txt

## Credits

**Author**: faospark
**For**: [Project Kyaro](https://www.nexusmods.com/suikoden1and2hdremaster/mods/6) HD sprite pack
**Compatible with**: [Suikoden Fix](https://github.com/d3xMachina/SuikodenFix) by d3xMachina

**Special Thanks & Credits**:

- **[d3xMachina](https://github.com/d3xMachina)**: For the excellent [Suikoden Fix](https://github.com/d3xMachina/SuikodenFix) mod. PKCore is built to work alongside it.
  - The **NPC Portrait** feature leverages his `TextDatabase` research and speaker identification logic.
