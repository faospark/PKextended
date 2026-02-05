# PKCore - Caching & Performance Guide

PKCore includes a sophisticated caching and optimization system designed to handle large texture packs (like Project Kyaro) without compromising game startup times or runtime performance.

## 1. Texture Manifest Caching

Scanning thousands of custom PNG files every time the game starts can significantly delay the "Press Start" screen. PKCore solves this with the **Texture Manifest Cache**.

### How it works
- **First Run**: PKCore performs a deep scan of your `PKCore/Textures/` folder and builds a complete index of every replaceable texture. This index is saved to `PKCore/Cache/texture_manifest.xml`.
- **Subsequent Runs**: PKCore simply loads the XML file. This reduces startup indexing time from several seconds down to ~20ms.
- **Config Hash Validation**: The manifest includes a "Config Hash" that tracks settings affecting texture selection (e.g., Save Point Color, Tir Run Animation). If you change these settings, the hash changes, automatically invalidating the cache and triggering a rebuild.

### Texture Directory Filtering
PKCore includes intelligent texture filtering that allows you to selectively disable specific texture categories for debugging or preference:
- **Launcher UI Textures** (`LoadLauncherUITextures = false`): Skips textures in `Launcher-Mod` folders.
- **Project Kyaro Sprites** (`EnableProjectKyaroSprites = false`): Skips textures in `\PKS1\` and `\PKS2\` folders. 
- **Minimal UI Textures** (`MinimalUI = false`): Skips textures containing "minimal" in their path.

### Manual Cache Control
- **Disable Caching**: Set `EnableTextureManifestCache = false` to force fresh scans every startup (useful for texture development).
- **Force Rebuild**: Delete the `PKCore/Cache/` folder or change any tracked config setting.
- **Cache Location**: `PKCore/Cache/texture_manifest.xml`

## 2. Load Order & Priority System

When multiple textures share the same name, PKCore uses a strict **3-Layer Priority System** to determine which file wins. This allows mods to override base textures without deleting them.

| Priority | Layer | Folder Location | Description |
| :--- | :--- | :--- | :--- |
| **1 (Low)** | **Base** | Root of `Textures/` | Basic textures and individual replacements. |
| **2 (Mid)** | **Game Specific** | `Textures/GSD1/`, `Textures/GSD2/` | Game-specific overrides. Only loaded when playing that specific game. |
| **3 (High)** | **Mods** | `Textures/00-Mods/` | **High Priority Overrides.** Files here ALWAYS win. Useful for total conversion packs or personal tweaks. |

**Rule of Thumb:** If you want to force a texture to load, put it in `00-Mods`.

## 3. High-Performance DDS Loading

For the absolute best performance, PKCore supports pre-compressed **DDS** files.

- **Fastest Loading**: DDS files are already in the GPU's native format. They skip expensive runtime processing entirely.
- **In-Place Replacement**: PKCore performs "Low-Level In-Place Replacement" for DDS files, copying raw pixel data directly into the game's original texture memory. This preserves all game references while swapping the visual content.
- **Priority**: `.dds` files take priority over `.png` if both valid files exist in the same priority layer.
- **Formats Supported**: BC1 (DXT1) for opaque textures, BC3 (DXT5) for textures with transparency.

## 4. Intelligent Scene-Based Memory Caching

PKCore features a "Smart Memory" management system that tracks custom textures at runtime and optimizes VRAM usage based on your current location.

### How it works
- **Scene Tracking**: PKCore monitors which textures are loaded in specific game areas (e.g., world map vs. town).
- **Automatic Cleanup**: When you move between major game scenes (e.g., entering a town from the world map), PKCore identifies textures that are no longer relevant and **purges** them from memory.
- **Persistence System**: Essential textures are marked as **Persistent** and are NEVER purged, ensuring UI stability.
  
**Persistent Patterns (Safe from Purge):**
- `window_` (UI Borders)
- `t_obj_savePoint` (Save Points)
- `menu` (Menu Elements)
- `ui` (General UI)
- `dialog` (Dialog Boxes)

### Config Options
- `EnableMemoryCaching = true`: Activates the scene-based purge system (Recommended).
- `DetailedTextureLog = true`: Logs texture operations, memory stats, and purge events to the console.

## 5. Performance Recommendations

| Scenario | Recommendation |
| :--- | :--- |
| **Normal Use** | Keep `EnableTextureManifestCache = true` and `EnableMemoryCaching = true` |
| **Texture Development** | Set `EnableTextureManifestCache = false` to force fresh scans when adding/removing files. |
| **Selective Modding** | Use `EnableProjectKyaroSprites = false` if you only want UI mods and not sprite replacements. |
| **Low VRAM Systems** | Ensure `EnableMemoryCaching = true` to prevent out-of-memory issues. |
| **Large Mod Packs** | Convert textures to **DDS** format to eliminate load-time processing and stutter. |

## 6. Texture Organization Template

PKCore supports the following recommended folder structure:
```
PKCore/Textures/
├── *.png, *.dds          # Base game textures (lowest priority)
├── GSD1/                 # Suikoden 1 specific textures (medium priority)
├── GSD2/                 # Suikoden 2 specific textures (medium priority)
├── 00-Mods/              # GLOBAL OVERRIDES (Highest Priority)
│   ├── Launcher-Mod/     # Custom launcher UI
│   ├── Minimal-UI-Mod/   # Minimal UI textures
│   └── PKS1/, PKS2/      # Project Kyaro sprites
├── NPCPortraits/         # Custom NPC portrait images
└── SavePoint/            # Save point orb color variants
```

***
*Note: The Texture Manifest Cache is located in `PKCore/Cache/`. To reset all caches, simple delete the `Cache` folder and restart the game.*
