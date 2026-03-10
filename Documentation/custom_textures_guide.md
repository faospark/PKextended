# Custom Textures Guide

## Overview

PKCore's custom texture system allows you to replace any game texture with your own PNG, JPG, TGA, or DDS files. This guide explains how the system works internally, how to structure your custom texture files, and how to take full advantage of the mod's capabilities.

---

## How It Works

### Initialization Process

1. **Startup Indexing**: When the game launches, PKCore scans the `BepInEx/PKCore/Textures/` folder for all supported image files
2. **Manifest Caching**: The texture index is saved to an XML manifest (`PKCore/Cache/texture_manifest.xml`) to speed up subsequent launches
3. **Harmony Patching**: The mod uses Harmony to intercept Unity engine texture/sprite setters
4. **Runtime Replacement**: When the game loads a texture, PKCore checks if a custom version exists and swaps it in real-time using the AssetLoader service (background thread I/O + main thread texture creation)

### Texture Matching

The mod matches textures by **filename**. When the game requests a texture, PKCore looks for an exact filename match first.

**File Naming:**
- **Exact match (recommended)**: Most textures use the full Unity-generated name:
  ```
  sactx-0-128x128-Uncompressed-ail_field_00_atlas-0bfc7460.png
  ```
  This includes the resolution, compression, base name, and content hash.
  
- **Simplified names (fallback)**: If no exact match is found, the system automatically strips the `sactx-` prefix and hash:
  ```
  sactx-0-128x128-Uncompressed-m_gat1_00_atlas-b4ac1ef3  →  m_gat1_00_atlas
  ```
  This allows you to use simplified names like `m_gat1_00_atlas.png` for convenience.

**Special Features:**
- **Controller prompts**: Supports variant suffixes for different controller types:
  - `_00`: Generic/PC (keyboard icons)
  - `_01`: PlayStation 4 (DS4)
  - `_02`: PlayStation 5 (DualSense)
  - `_03`: Xbox (native)
  - `_04`: Switch/Nintendo
  - `_05`: Custom (user-defined)
- **Game-specific textures**: Textures can be isolated to Suikoden 1 or 2 using folder structure
- **DDS priority**: If both `texture.dds` and `texture.png` exist, DDS is used

### Memory Management

- **Caching**: Successfully loaded textures are cached in memory to avoid redundant file reads
- **Scene Cleanup**: Non-persistent textures are automatically cleared from memory on scene transitions
- **Persistent Textures**: UI elements (window textures, save points, etc.) survive scene changes
- **DontDestroyOnLoad**: Custom textures are marked to persist across scene loads when needed

---

## Folder Structure

### Base Directory

All custom textures must be placed in:
```
<Game Root>/BepInEx/PKCore/Textures/
```

### Priority System (Layer Override)

PKCore uses a **three-tier priority system** where higher priority folders override lower ones:

#### **Layer 1: Base Folder (Lowest Priority)**
```
PKCore/Textures/
├── texture1.png
├── texture2.dds
└── texture3.jpg
```
- Generic textures that apply to both games
- Lowest priority - can be overridden by game-specific folders

#### **Layer 2: Game-Specific Folders (Medium Priority)**
```
PKCore/Textures/
├── GSD1/
│   ├── suikoden1_texture.png
│   └── shared_texture.png
└── GSD2/
    ├── suikoden2_texture.png
    └── shared_texture.png
```
- **GSD1/**: Textures that only load in Suikoden 1
- **GSD2/**: Textures that only load in Suikoden 2
- Overrides base folder textures
- Prevents cross-contamination between games

#### **Layer 3: 00-Mods Folder (HIGHEST Priority)**
```
PKCore/00-Mods/
└── MyMod/
    └── Textures/
        ├── my_custom_texture.png
        ├── GSD1/
        │   └── custom_s1_texture.png
        └── GSD2/
            └── custom_s2_texture.png
```
- **Highest priority** — always overrides base and game-specific folders
- Each named subfolder is an independent mod package
- Supports `GSD1/`/`GSD2/` isolation inside each mod's `Textures/` folder
- Also supports `Sound/` subfolders for custom audio (see [00-Mods Guide](00-mods_guide.md))

> For full details on using the `00-Mods/` folder — including sound support, multi-mod priority, and packaging — see the **[00-Mods Guide](00-mods_guide.md)**.

### Example Complete Structure

```
BepInEx/PKCore/Textures/
├── window_main.png              ← Base: Works in both games
├── GSD1/
│   ├── s1_hero_portrait.png     ← Only loads in Suikoden 1
│   └── castle_bg.png
├── GSD2/
│   ├── s2_hero_portrait.png     ← Only loads in Suikoden 2
│   └── castle_bg.png            ← Different version for S2
└── 00-Mods/
    ├── custom_window.png        ← User mod: Overrides window_main.png
    └── GSD2/
        └── castle_bg.png        ← User mod for S2: Highest priority
```

---

## Supported File Formats

### Image Formats

| Format | Extension | Notes |
|--------|-----------|-------|
| **PNG** | `.png` | Recommended for most textures with transparency |
| **DDS** | `.dds` | Pre-compressed, best performance (BC1/BC3/BC7) |
| **JPEG** | `.jpg`, `.jpeg` | Smaller file size, no transparency |
| **TGA** | `.tga` | Uncompressed, large files |

### DDS vs PNG: Performance Considerations

- **PNG/JPG/TGA**: Loaded as uncompressed RGBA32 textures (larger VRAM usage, slower loading)
- **DDS**: Pre-compressed format (BC1/BC3/BC7), smaller VRAM footprint, faster loading
- **File Priority**: If both `texture.dds` and `texture.png` exist in the same folder, **DDS is used**

**Recommendation**: Use DDS for large textures (backgrounds, high-res sprites) to reduce VRAM usage and improve load times. PNG is fine for small UI elements and textures requiring precise transparency.

### DDS Texture Compression Guide

PKCore supports the following DDS compression formats:

- **BC1 (DXT1)**: No alpha channel, 6:1 compression, smallest file size
- **BC3 (DXT5)**: Full alpha channel, 4:1 compression, good for transparency
- **BC7**: Highest quality, 4:1 compression, best color accuracy

**Why Use DDS?**
- Smaller VRAM usage (4-6x smaller than PNG)
- Faster loading (no decompression needed)
- GPU can use compressed data directly

**Creating DDS Files:**
1. Use tools like [NVIDIA Texture Tools](https://developer.nvidia.com/nvidia-texture-tools-exporter) or [Paint.NET with DDS plugin](https://github.com/dlemstra/Magick.NET)
2. Choose BC1 for opaque textures, BC3 for textures with alpha, BC7 for highest quality
3. Save with mipmaps enabled for best quality

---

## How to Use Custom Textures

### Finding Texture Names

**Method 1: Check Existing Texture Pack**
The easiest way is to check what's already in your `PKCore/Textures` folder:
```
Get-ChildItem "PKCore/Textures" -Recurse -File | Select-Object Name
```
Most textures (83%) use complete Unity-generated names with `sactx-` prefix and hash.

**Method 2: Enable Logging**
1. Edit `BepInEx/config/faospark.pkcore.cfg`
2. Set `LogReplaceableTextures = true`
3. Launch the game and check the BepInEx console or log file
4. You'll see entries like: `[Replaceable Texture] sactx-0-128x128-Uncompressed-texture_name-a1b2c3d4`

**MetFind Original Name**: Check your existing `PKCore/Textures` folder or use logging to get the exact texture name
2. **Export Original**: Use AssetStudio/AssetRipper to extract the original texture (optional)
3. **Edit**: Use Photoshop, GIMP, Aseprite, or your preferred image editor
4. **Maintain Aspect Ratio**: PKCore auto-scales textures, but matching original resolution is recommended
5. **Save**: Export as PNG (transparency) or DDS (performance)
6. **Name Exactly**: Use the COMPLETE filename from step 1:
   ```
   sactx-0-128x128-Uncompressed-ail_field_00_atlas-0bfc7460.png
   ```
   Or use simplified name (system will match via fallback):
   ```
   ail_field_00_atlas.png
   ```
### Creating Your Custom Texture

1. **Export Original**: Use AssetStudio/AssetRipper to extract the original texture
2. **Edit**: Use Photoshop, GIMP, Aseprite, or your preferred image editor
3. **Maintain Aspect Ratio**: PKCore auto-scales textures, but matching original resolution is recommended
4. **Save**: Export as PNG (transparency) or DDS (performance)
5. **Name Correctly**: Filename MUST match the internal texture name exactly (case-insensitive)

### Installing Your Texture

1. Place the file in the appropriate folder:
   ```
   PKCore/Textures/00-Mods/your_texture.png     ← Generic
   PKCore/Textures/00-Mods/GSD1/your_texture.png ← Suikoden 1 only
   PKCore/Textures/00-Mods/GSD2/your_texture.png ← Suikoden 2 only
   ```
2. Launch the game
3. Check the log for confirmation: `Replaced texture: your_texture`

---

## Advanced Features

### Texture Variants

Some textures support conditional variants based on configuration:

**Controller Prompts:**
- `button_a_00.png` → Generic/PC (keyboard icons)
- `button_a_01.png` → PlayStation 4 (DS4)
- `button_a_02.png` → PlayStation 5 (DualSense)
- `button_a_03.png` → Xbox (native)
- `button_a_04.png` → Switch/Nintendo
- `button_a_05.png` → Custom (user-defined)

The system automatically selects the correct variant based on your config setting.

**Save Point Colors:**
- Custom save point crystal textures based on `SavePointColor` config
- Automatic fallback to default if custom color variant not found

### Special Texture Categories

#### Window UI Textures
- **Pattern**: Filenames starting with `window_` or in `window-ui/` subfolder
- **Behavior**: Uses Point filtering instead of bilinear for crisp pixel-perfect rendering
- **Example**: `window_main.png`, `window_frame.png`

#### Persistent Textures
- **Pattern**: Textures matching persistent prefixes (`window_`, `t_obj_savePoint_ball`, `sactx`)
- **Behavior**: Kept in memory across scene transitions to prevent reloading
- **Purpose**: Reduces load times for frequently used UI elements

#### Bath Scene Textures
- **Pattern**: Filenames starting with `bath_`
- **Behavior**: Pre-loaded on startup for instant replacement
- **Instance Tracking**: Only replaces when BathBG GameObject is newly instantiated

### Game-Specific Isolation

Prevents Suikoden 1 textures from appearing in Suikoden 2 and vice versa:

```
PKCore/Textures/
├── GSD1/
│   └── hero.png      ← Only loads in Suikoden 1
├── GSD2/
│   └── hero.png      ← Only loads in Suikoden 2
└── 00-Mods/
    └── GSD1/
        └── hero.png  ← User override for S1 only
```

**How It Works:**
- At runtime, PKCore detects which game is active
- Textures in the opposite game's folder are blocked from loading
- Applies recursively to `00-Mods/GSD1/` and `00-Mods/GSD2/`

---

## Configuration Settings

### Related Config Options

Edit `BepInEx/config/faospark.pkcore.cfg`:

```ini
[Textures]
# Enable/disable custom texture system entirely (default: true)
EnableCustomTextures = true

# Log every replaceable texture name to console (default: false)
LogReplaceableTextures = false

# Enable detailed debugging logs (default: false)
DetailedLogs = false

# Enable manifest caching for faster startup (default: true)
# Set to false if textures aren't loading after adding new files
EnableTextureManifestCache = true
```

### Cache Management

**Manifest File**: `BepInEx/PKCore/Cache/texture_manifest.xml`

The manifest cache is automatically rebuilt when:
- New textures are added or removed
- Config settings change (controller prompts, save point color, etc.)
- Manifest version is updated by mod author

**Manual Cache Clear**:
1. Delete `PKCore/Cache/texture_manifest.xml`
2. Restart the game
3. Cache will rebuild (slightly slower first launch)

---

## Troubleshooting

### Texture Not Loading

**Check:**
1. ✅ Filename matches exactly (use logs to verify)
2. ✅ File is in correct folder (use GSD1/GSD2 for game-specific textures)
3. ✅ File format is supported (PNG, DDS, JPG, TGA)
4. ✅ No typos in filename extensions
5. ✅ Clear manifest cache if adding new textures (set `EnableTextureManifestCache = false` temporarily, or delete `PKCore/Cache/texture_manifest.xml`)

**Common Issues:**
- **Stale cache**: Manifest cache may not detect newly added files - clear cache and restart
- **Wrong folder**: Texture in GSD1 folder won't load in Suikoden 2
- **Case mismatch**: Filenames are case-insensitive, but verify spelling
- **Format**: Make sure the file isn't corrupted

### Performance Issues

**Symptoms:**
- Long startup times
- Stuttering when textures load
- High VRAM usage

**Solutions:**
1. **Use DDS**: Convert large PNG files to DDS format (4-6x smaller in VRAM)
2. **Enable Caching**: Set `EnableTextureManifestCache = true` (faster manifest loading)
3. **Reduce Texture Size**: Downscale ultra-high resolution textures if needed
4. **Check Logs**: Look for error messages indicating corrupt files

### Blurry or Stretched Textures

**Causes:**
- Incorrect aspect ratio
- Wrong resolution

**Solutions:**
1. Export original texture to check native resolution
2. Match width AND height of original
3. PKCore auto-scales, but matching resolution gives best results

---

## Performance Best Practices

### Optimization Tips

1. **Use DDS for Large Textures**
   - Backgrounds: Always use DDS (4-6x smaller VRAM usage)
   - Character sprites: DDS recommended for better performance
   - UI elements: PNG is fine for small icons and precise transparency

2. **Enable Manifest Caching**
   - Dramatically speeds up startup
   - Auto-rebuilds when textures change

3. **Match Original Resolutions**
   - Reduces scaling overhead
   - Maintains pixel-perfect quality

4. **Organize by Game**
   - Use GSD1/GSD2 folders to reduce indexing overhead
   - Prevents loading unused textures

5. **Clean Up Unused Textures**
   - Remove test files from Textures folder
   - Keep only what you'll use

---

## Technical Details

### Caching System

See [caching_mechanism.md](caching_mechanism.md) for detailed information.

**Key Points:**
- XML-based manifest stores texture index
- Manifest is hashed against config settings
- Invalidated automatically when config changes
- Stores file count and texture-to-path mappings

### Harmony Patches

Custom texture replacement is implemented via Harmony patches on:

- `UnityEngine.UI.Image.sprite` (setter)
- `UnityEngine.SpriteRenderer.sprite` (setter)
- `UnityEngine.SpriteAtlas` (sprite retrieval)
- Game-specific classes (`GRSpriteRenderer`, etc.)

**Patching Strategy:**
- **Prefix patches**: Intercept BEFORE game sets texture
- **Postfix patches**: Verify AFTER game sets texture
- **Replacement**: Swap original sprite/texture with custom version

### AssetLoader Service

The `AssetLoader.cs` module provides centralized, high-performance texture loading:

**Key Features:**
- **Asynchronous Loading**: Background thread file I/O prevents main thread blocking
- **Main Thread Queue**: Unity API calls (texture creation, Apply()) execute on main thread
- **Unified Loading**: Single code path for all texture formats (PNG, DDS, JPG, TGA)
- **DDS Optimization**: Automatic detection and handling via `DDSLoader.cs`
- **Filter Mode Logic**: Smart filter mode assignment (Point for UI, Bilinear for sprites)

**Loading Flow:**
1. `LoadTextureSync()` or `LoadTextureAsync()` called
2. Resolve path from `texturePathIndex` dictionary
3. Read file bytes on background thread (non-blocking)
4. Queue texture creation on Unity main thread
5. Apply format-specific settings (filter mode, aniso level, wrap mode)
6. Return processed Texture2D

**Why It Matters:**
- Eliminates stuttering from file I/O on main thread
- Provides consistent texture loading across all patches
- Centralizes format handling (PNG vs DDS logic in one place)

### Memory Architecture

```
┌─────────────────────────────────────────┐
│         texturePathIndex                │ ← Filename → File path mapping
│  (Loaded once at startup from manifest) │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│           AssetLoader.cs                │ ← Centralized loading service
│  - Background thread file I/O           │
│  - Main thread texture creation          │
│  - Format detection (PNG/DDS/JPG/TGA)    │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│       customTextureCache                │ ← Texture2D objects in memory
│  (Runtime cache, cleared on scene load) │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│        customSpriteCache                │ ← Sprite objects in memory
│  (Runtime cache, some persist scenes)   │
└─────────────────────────────────────────┘
```

**Component Responsibilities:**
- **texturePathIndex**: Fast lookup of texture file paths (XML manifest-backed)
- **AssetLoader**: Handles actual file loading and format processing
- **customTextureCache**: Caches loaded Texture2D objects to avoid redundant file reads
- **customSpriteCache**: Caches created Sprite objects with original pivot/PPU properties

---

## Examples

### Example 1: Replace a Character Portrait

**Goal**: Replace Tir's portrait with a custom version

1. Enable logging and find texture name: `portrait_tir_00`
2. Create your custom portrait in Photoshop (match original dimensions)
3. Save as `portrait_tir_00.png`
4. Place in `PKCore/Textures/00-Mods/GSD1/portrait_tir_00.png`
5. Launch game and verify replacement

### Example 2: Add Custom Controller Prompts

**Goal**: Use custom Xbox button icons

1. Create button icons: `button_a_03.png`, `button_b_03.png`, etc.
2. Place in `PKCore/Textures/00-Mods/`
3. Set `ControllerPromptType = Xbox` in config (maps to `_03` suffix)
4. Set `ForceControllerPrompts = true` to always show Xbox prompts

**Available Controller Types:**
- `PlayStation` or `PS4` → Uses `_01` suffix
- `PlayStation5` or `PS5` → Uses `_02` suffix  
- `Generic` or `PC` → Uses `_00` suffix (keyboard icons)
- `Xbox` → Uses `_03` suffix
- `Switch` or `Nintendo` → Uses `_04` suffix
- `Custom` → Uses `_05` suffix

### Example 3: Replace a Background (Performance)

**Goal**: Replace a large background texture efficiently

1. Find texture name: `bg_castle_exterior`
2. Extract original using AssetStudio
3. Edit in Photoshop, save as PNG
4. Convert to DDS using NVIDIA Texture Tools:
   - Format: BC1 (DXT1) for opaque backgrounds
   - Mipmaps: Yes
5. Save as `bg_castle_exterior.dds`
6. Place in `PKCore/Textures/00-Mods/GSD2/bg_castle_exterior.dds`

---

## FAQ

**Q: Can I use textures larger than the original?**  
A: Yes! PKCore auto-scales custom textures to maintain original display size while preserving detail.

**Q: Do I need to rebuild the cache after adding textures?**  
A: No, the cache auto-rebuilds if new files are detected.

**Q: Can I mix PNG and DDS files?**  
A: Yes, DDS takes priority if both formats exist for the same texture.

**Q: Will textures from GSD1 folder load in the launcher?**  
A: No, game-specific folders (GSD1/GSD2) only load in their respective games. Use the base folder for launcher textures.

**Q: How do I revert to original textures?**  
A: Delete the custom texture file or set `EnableCustomTextures = false` in config.

**Q: Can I share my custom textures?**  
A: Yes! Just share the files in your `00-Mods/` folder. Others can drop them in their own `00-Mods/` folder.

**Q: Why isn't my texture replacing in-game?**  
A: Check logs with `LogReplaceableTextures = true` to see if the texture is even being loaded by the game. Some textures may be hardcoded or loaded differently.

---

## Related Documentation

- [00-Mods Guide](00-mods_guide.md) - Using the 00-Mods folder for textures, sounds, and mod packaging
- [Configuration Guide](configuration_guide.md) - All config options explained
- [Caching Mechanism](caching_mechanism.md) - Deep dive into the caching system
- [NPC Portraits Walkthrough](walkthrough_npc_portraits.md) - Guide for custom NPC portraits

---

## Credits

Custom texture system designed and implemented by **faospark** for [Project Kyaro](https://www.nexusmods.com/suikoden1and2hdremaster/mods/6).

For support, visit the [Project Kyaro Nexus Mods page](https://www.nexusmods.com/suikoden1and2hdremaster/mods/6).
