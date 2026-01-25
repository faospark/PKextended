# PKCore - Caching & Performance Guide

PKCore includes a sophisticated caching and optimization system designed to handle large texture packs (like Project Kyaro) without compromising game startup times or runtime performance.

## 1. Texture Manifest Caching

Scanning thousands of custom PNG files every time the game starts can significantly delay the "Press Start" screen. PKCore solves this with the **Texture Manifest Cache**.

### How it works
- **First Run**: PKCore performs a deep scan of your `PKCore/Textures/` folder and builds a complete index of every replaceable texture. This index is saved to `BepInEx/plugins/PKCore/Cache/texture_manifest.xml`.
- **Subsequent Runs**: PKCore simply loads the XML file. This reduces startup indexing time from several seconds down to ~20ms.

### Config-Aware Invalidation
The cache is "smart." It tracks specific configuration settings that affect which textures should be used. The cache will **automatically rebuild** if you change any of the following:
- `SavePointColor`
- `EnableProjectKyaroSprites`
- `ForceControllerPrompts`
- `ControllerPromptType`
- `MercFortFence`
- `S2ClassicSaveWindow`
- `TirRunTexture`
- `LoadLauncherUITextures`

### Manual Rebuild
If you add or remove textures while the game is closed and they aren't showing up, you can force a rebuild by:
1. Setting `EnableTextureManifestCache = false` in your config.
2. Deleting the `PKCore/Cache/` folder.

## 2. Runtime Texture Compression

Unity's standard `LoadImage` function creates uncompressed (RGBA32) textures. A large texture pack can easily consume several gigabytes of VRAM if left uncompressed, leading to crashes or stuttering.

### Automatic BC3 (DXT5) Compression
When a texture is first loaded, PKCore compresses it into a GPU-friendly format:
- **BC3 (DXT5)**: Used for textures with transparency (6:1 compression ratio).
- **BC1 (DXT1)**: Used for opaque textures (8:1 compression ratio).

### Quality Settings
You can control the trade-off between boot speed and visual quality:
- `TextureCompressionQuality = High`: Slower first-time load, better visual results.
- `TextureCompressionQuality = Normal`: Faster initial compression.

## 3. High-Performance DDS Loading

For the absolute best performance, PKCore supports pre-compressed **DDS** files.

- **Fastest Loading**: DDS files are already in the GPU's native format. They skip the expensive runtime compression step entirely.
- **Zero Stall**: Loading a 4K DDS texture is significantly faster than a PNG because it is copied directly to VRAM.
- **Usage**: Use tools like `texconv` to convert your PNG mods to DDS (BC3/DXT5 format with Mipmaps). Place them in the same folders as your PNGs. PKCore will prioritize `.dds` files over `.png` if both are present.

## 4. Intelligent Scene-Based Memory Caching

PKCore features a "Smart Memory" management system that tracks custom textures at runtime and optimizes VRAM usage based on your current location.

### How it works
- **Scene Tracking**: PKCore monitors which textures are loaded in specific game areas (e.g., world map vs. town).
- **Automatic Lifecycle Management**: When you move between major game scenes (like switching between Suikoden 1 and Suikoden 2), PKCore identifies textures that are no longer relevant and purges them from memory.
- **Persistence System**: Essential textures—such as UI borders, menu elements, dialog windows, and save points—are marked as **Persistent**. These remain in memory across all scene transitions to ensure the interface never "blinks" or stays un-skinned.
- **Leak Prevention**: By tracking usage and verifying if textures are still active on Unity renderers, the system prevents "memory creep," allowing long play sessions even with massive high-resolution texture packs.

### Config Options
- `EnableMemoryCaching = true`: Activates the scene-based purge system (Recommended).
- `DetailedTextureLog = true`: Logs exactly which textures are registered, marked persistent, or purged during scene transitions.

## 5. Performance Recommendations

| Scenario | Recommendation |
| :--- | :--- |
| **Normal Use** | Keep `EnableTextureManifestCache`, `EnableTextureCompression`, and `EnableMemoryCaching` as `true`. |
| **Low VRAM (4GB or less)** | Ensure `EnableTextureCompression = true` is active and `EnableMemoryCaching = true` to prevent out-of-memory crashes. |
| **Large Mod Packs** | Convert textures to **DDS** format to eliminate the compression pause when entering new scenes. |
| **Minimal Loading Times** | Use **DDS** files + `EnableTextureManifestCache = true`. |
| **Debugging** | Set `DetailedTextureLog = true` to see cache registration and scene purge events in the console. |

---
*Note: The Texture Manifest Cache is located in `PKCore/Cache/`, while Memory Caching is managed dynamically in RAM.*

