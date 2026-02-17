# PKCore Codebase Overview

> **Last Updated:** 2026-01-31
> **Purpose:** High-level map of the PKCore "Core Engine" to ensure content awareness and avoid redundant implementation.

## 📂 Project Structure

### Core Infrastructure

| File | Description |
|------|-------------|
| `Plugin.cs` | Main entry point. Initializes configuration, logging, and applies patches based on `ModConfiguration`. |
| `ModConfiguration.cs` | Centralized configuration management using BepInEx config system. |
| `AssetLoader.cs` | Utilities for loading assets (textures, sprites) from disk. |
| `DDSLoader.cs` | Specialized loader for DDS texture alignment and parsing. |

### 🎨 Texture & Sprite System

*The heart of PKCore, handling texture replacement, caching, and loading.*

| File | Description |
|------|-------------|
| `CustomTexturePatch.cs` | Main logic for intercepting texture requests and swapping with custom assets. |
| `CustomTexturePersist.cs` | Handles persistence of custom texture states. |
| `GameObjectPatch.cs` | Handles GameObject.SetActive patches for texture replacement and UI refreshes (includes TopMenuPartyList refresh for MenuTopPartyStatus). |
| `TextureMemoryCachePatch.cs` | Smart memory management to prevent leaks and handle cache cleanup (referenced in `Caching.md`). |
| `SpriteFilteringPatch.cs` | Applies filtering modes (Point, Bilinear) to sprites based on config. |
| `GRSpriteRendererPatch.cs` | Patches specifically for `GRSpriteRenderer` (custom rendering component). |
| `UnitySpriteRendererPatch.cs` | Patches for standard Unity `SpriteRenderer`. |
| `SpriteAtlasPatch.cs` | Patches for Sprite.texture getter to replace atlas textures during runtime. |
| `MapTexturePatch.cs` | Handling for map-specific textures. |
| `PortraitIndexCache.cs` | Caching mechanism for portrait indexing to improve performance. |

### 🖥️ UI & Quality of Life

| File | Description |
|------|-------------|
| `MenuScale.cs` | Logic for scaling UI menus (referenced in "ScaledDownMenu" config). |
| `DialogBoxScalePatch.cs` | Adjusts dialog box sizing and layout. |
| `GlobalControllerPromptPatch.cs` | Custom controller prompts (Xbox, PS, etc.) and texture replacements. |
| `MouseCursorPatch.cs` | Logic to force mouse cursor visibility. |
| `BorderlessWindowPatch.cs` | Implementation of borderless window mode. |
| `ResolutionPatch.cs` | Custom resolution scaling logic. |
| `SaveWindowPatch.cs` | Enhancements for the save/load screen. |

### 🎮 Game Logic & Features

| File | Description |
|------|-------------|
| `PortraitSystemPatch.cs` | Core logic for injecting and managing NPC portraits in dialogs. |
| `TextDatabasePatch.cs` | Intercepts text retrieval, often used in tandem with NPC portraits. |
| `SavePointPatch.cs` | Enhancements to save point logic and interaction. |
| `BathTexturePatch.cs` | Specific handling for bath background textures with preloading system. |
| `CowTexturePatch.cs` | Animated cow texture replacement with continuous monitoring (Suikoden 1 Gregminster). |
| `DragonPatch.cs` | Dragon sprite texture replacement with MonoBehaviour monitoring. |
| `WarAbilityPatch.cs` | Modifications to war battles/abilities (Experimental). |
| `GameDetection.cs` | Detects current game state/context (referenced in recent refactors). |

### 🖌️ Rendering & Post-Processing

| File | Description |
|------|-------------|
| `SMAAPatch.cs` | Injectable SMAA anti-aliasing. |
| `DisableMask.cs` | System for replacing or disabling specific UI/Texture masks. |
| `DisableSpritePostProcessing.cs`| Disables native post-processing on sprites for cleaner visuals. |
| `DisableCustomPostEffect.cs` | Removes specific color grading/effects (e.g., historical filters). |

### 🧪 Experimental & Research

| File | Description |
|------|-------------|
| `CustomObjectInsertion.cs` | Logic for inserting new objects into scenes (Experimental). |
| `ParticleSystemResearch.cs` | Research/prototyping for particle system manipulation. |
| `S2SummonTextureSupport.cs` | Specific support for Suikoden 2 summons. |

## 🔗 Key Dependencies

- `BepInEx`: Modding framework.
- `Harmony`: Runtime patching library.
- `Il2CppInterop`: Interop for IL2CPP Unity games.

## ⚠️ Known Complexities

- **Texture Caching**: The system relies heavily on `TextureMemoryCachePatch` and `CustomTexturePatch` working in tandem. Modifying one often requires checking the other.
- **IL2CPP Limitations**: Use `Il2CppInterop` patterns for MonoBehaviours (`SavePointSpriteMonitor`, `CowMonitor`, `DragonMonitor`).
- **Config Flags**: Almost all patches are guarded by `ModConfiguration` flags in `Plugin.cs`. Always check config before implementation.
- **UI Refresh Patterns**: Some UI elements (like MenuTopPartyStatus) require forced refresh via GameObject toggle to trigger texture replacement. This is handled in `GameObjectPatch.cs` via `UIMainMenu.Open` patch.

## 🆕 Recent Additions (2026-01-31)

- **MenuTopPartyStatus Fix**: Integrated UI refresh pattern into `GameObjectPatch.cs` that hooks `UIMainMenu.Open` to force `TopMenuPartyList` refresh, ensuring party status background textures are applied correctly.
- **SpriteAtlasPatch**: Enhanced sprite atlas texture replacement system for UI elements.
- **Preload System**: Extended bath texture preloading pattern to MenuTopPartyStatus for instant texture replacement.
