# PKCore - Caching & Performance Guide

PKCore includes a sophisticated caching and optimization system designed to handle large texture packs (like Project Kyaro) without compromising game startup times or runtime performance.

## 1. Texture Manifest Caching

Scanning thousands of custom PNG files every time the game starts can significantly delay the "Press Start" screen. PKCore solves this with the **Texture Manifest Cache**.

### How it works
- **First Run**: PKCore performs a deep scan of your `PKCore/Textures/` folder and builds a complete index of every replaceable texture. This index is saved to `PKCore/Cache/texture_manifest.xml`.
- **Subsequent Runs**: PKCore simply loads the XML file. This reduces startup indexing time from several seconds down to ~20ms.

### Texture Directory Filtering
PKCore includes intelligent texture filtering that allows you to selectively disable specific texture categories:
- **Project Kyaro Sprites** (`EnableProjectKyaroSprites = false`): Disables textures in `\PKS1\` and `\PKS2\` folders
- **Launcher UI Textures** (`LoadLauncherUITextures = false`): Disables textures in `\Launcher-Mod\` folders  
- **Minimal UI Textures** (`MinimalUI = false`): Disables textures containing "minimal" in their path

When disabled, these custom textures are filtered out during index building, and the game uses original textures as fallback.

### Config-Aware Invalidation
The cache is "smart." It tracks specific configuration settings that affect which textures should be used. The cache will **automatically rebuild** if you change any of the following:
- `SavePointColor`
- `LoadLauncherUITextures`
- `EnableProjectKyaroSprites`
- `MinimalUI`
- `ForceControllerPrompts`
- `ControllerPromptType`
- `MercFortFence`
- `S2ClassicSaveWindow`
- `TirRunTexture`

### Manual Cache Control
- **Disable Caching**: Set `EnableTextureManifestCache = false` to force fresh scans every startup (useful for texture development)
- **Force Rebuild**: Delete the `PKCore/Cache/` folder or change any tracked config setting
- **Cache Location**: `PKCore/Cache/texture_manifest.xml`

## 2. High-Performance DDS Loading

For the absolute best performance, PKCore supports pre-compressed **DDS** files.

- **Fastest Loading**: DDS files are already in the GPU's native format. They skip expensive runtime processing entirely.
- **Priority System**: PKCore prioritizes `.dds` files over `.png` if both are present in the same folder
- **Formats Supported**: BC1 (DXT1) for opaque textures, BC3 (DXT5) for textures with transparency
- **Usage**: Use tools like `texconv` to convert your PNG mods to DDS format with mipmaps

## 3. Intelligent Scene-Based Memory Caching

PKCore features a "Smart Memory" management system that tracks custom textures at runtime and optimizes VRAM usage based on your current location.

### How it works
- **Scene Tracking**: PKCore monitors which textures are loaded in specific game areas (e.g., world map vs. town)
- **Automatic Lifecycle Management**: When you move between major game scenes, PKCore identifies textures that are no longer relevant and purges them from memory
- **Persistence System**: Essential textures—such as UI borders, menu elements, dialog windows, and save points—are marked as **Persistent** and remain in memory across all scene transitions
- **Leak Prevention**: By tracking usage and verifying if textures are still active on Unity renderers, the system prevents memory creep during long play sessions

### Config Options
- `EnableMemoryCaching = true`: Activates the scene-based purge system (Recommended)
- `DetailedTextureLog = true`: Logs texture operations and memory management events

## 4. Performance Recommendations

| Scenario | Recommendation |
| :--- | :--- |
| **Normal Use** | Keep `EnableTextureManifestCache = true` and `EnableMemoryCaching = true` |
| **Texture Development** | Set `EnableTextureManifestCache = false` to force fresh scans when adding/removing files |
| **Selective Modding** | Use texture filtering options (`EnableProjectKyaroSprites`, `LoadLauncherUITextures`, `MinimalUI`) to disable specific texture categories |
| **Low VRAM Systems** | Ensure `EnableMemoryCaching = true` to prevent out-of-memory issues |
| **Large Mod Packs** | Convert textures to **DDS** format to eliminate load-time processing |
| **Debugging** | Enable `DetailedTextureLog = true` to monitor cache and memory operations |

## 5. Texture Organization

PKCore supports the following folder structure:
```
PKCore/Textures/
├── *.png, *.dds          # Base game textures
├── GSD1/                 # Suikoden 1 specific textures
├── GSD2/                 # Suikoden 2 specific textures
├── 00-Mods/
│   ├── Launcher-Mod/     # Custom launcher UI (filterable)
│   ├── Minimal-UI-Mod/   # Minimal UI textures (filterable)
│   └── PKS1/, PKS2/      # Project Kyaro sprites (filterable)
├── NPCPortraits/         # Custom NPC portrait images
└── SavePoint/            # Save point orb color variants
```

The filtering system allows granular control over which texture categories are active without manually moving files.

---
*Note: The Texture Manifest Cache is located in `PKCore/Cache/`, while Memory Caching is managed dynamically in RAM. All texture filtering occurs during index building, not at runtime.*

