# 00-Mods Folder Guide

## Overview

The `00-Mods/` folder is PKCore's **highest-priority override system**. It is a single location where you can drop texture and sound replacements without touching any base mod files. Multiple mod packages can coexist side by side, each in their own named subfolder.

---

## Location

```
<Game Root>/PKCore/00-Mods/
```

This folder sits next to `Textures/` and `Sound/`, not inside them:

```
PKCore/
├── Textures/          ← Base texture overrides (low priority)
├── Sound/             ← Base sound overrides (low priority)
└── 00-Mods/           ← Mod packages (HIGHEST priority)
    ├── MyTextureMod/
    ├── MySoundMod/
    └── MyCombinedMod/
```

---

## Priority System

All overrides in `00-Mods/` beat everything else:

| Priority | Source | Path |
|----------|--------|------|
| **1 (Lowest)** | Original game files | `StreamingAssets/` |
| **2** | Base texture overrides | `PKCore/Textures/` |
| **3** | Base sound overrides | `PKCore/Sound/` |
| **4 (Highest)** | Mod packages | `PKCore/00-Mods/<ModName>/` |

When multiple mods provide the same file, the **alphabetically last mod folder wins**. You can exploit this by prefixing your personal folder with `zz_` to ensure it always takes top priority.

---

## Mod Package Structure

Each mod lives in its own named subfolder. Inside that subfolder, assets are organised by type:

```
PKCore/00-Mods/
└── MyMod/
    ├── Textures/          ← Custom textures (PNG, DDS, JPG, TGA)
    │   ├── texture.png                ← Generic (both games)
    │   ├── GSD1/
    │   │   └── s1_only_texture.png    ← Suikoden 1 only
    │   └── GSD2/
    │       └── s2_only_texture.png    ← Suikoden 2 only
    └── Sound/             ← Custom sounds (ACB/AWB)
        ├── BGM2/
        │   └── BATTLE1.acb
        └── SEHD1/
            └── SE1.acb
```

> **Note:** The `Textures/` subfolder uses `GSD1/` and `GSD2/` game-specific isolation just like the base `PKCore/Textures/` folder. The `Sound/` subfolder mirrors the original `StreamingAssets/Sound/` directory structure.

---

## Custom Textures in 00-Mods

### Supported Formats

| Format | Extension | Notes |
|--------|-----------|-------|
| **PNG** | `.png` | Recommended for transparency |
| **DDS** | `.dds` | Best performance (BC1/BC3/BC7) |
| **JPEG** | `.jpg`, `.jpeg` | No transparency |
| **TGA** | `.tga` | Uncompressed |

If both `texture.dds` and `texture.png` exist for the same name, **DDS wins**.

### File Naming

Match the texture's internal Unity name exactly (case-insensitive):

- **Full name** (most reliable):
  ```
  sactx-0-128x128-Uncompressed-ail_field_00_atlas-0bfc7460.png
  ```
- **Simplified name** (fallback — strips `sactx-` prefix and hash automatically):
  ```
  ail_field_00_atlas.png
  ```

Enable `LogReplaceableTextures = true` in config to discover exact texture names at runtime.

### Game-Specific Textures

Place textures in a `GSD1/` or `GSD2/` subfolder inside your mod's `Textures/` folder to restrict them to one game:

```
MyMod/Textures/
├── shared_ui.png          ← Loads in both games
├── GSD1/
│   └── hero_portrait.png  ← Suikoden 1 only
└── GSD2/
    └── hero_portrait.png  ← Suikoden 2 only
```

### Example: Texture Mod

```
PKCore/00-Mods/
└── BetterPortraits/
    └── Textures/
        ├── GSD1/
        │   ├── portrait_tir_00.png
        │   └── portrait_gremio_00.png
        └── GSD2/
            ├── portrait_riou_00.png
            └── portrait_nanami_00.png
```

---

## Custom Sounds in 00-Mods

### Supported Formats

| Format | Notes |
|--------|-------|
| **ACB** | CriWare audio cue bank — required |
| **AWB** | CriWare audio wave bank — required alongside ACB for streaming audio |

### Folder Structure

Mirror the original `StreamingAssets/Sound/` structure exactly inside your mod's `Sound/` folder:

```
MyMod/Sound/
├── BGM2/
│   ├── BATTLE1.acb
│   └── BATTLE1.awb
└── SEHD1/
    └── SE1.acb
```

To find the correct subfolder names, browse `<Game Root>/<GameName>_Data/StreamingAssets/Sound/` in Explorer.

### Example: Sound Mod

```
PKCore/00-Mods/
└── CustomBGM/
    └── Sound/
        └── BGM2/
            ├── BATTLE1.acb
            └── BATTLE1.awb
```

When the game loads `BGM2/BATTLE1.acb`, PKCore automatically redirects to the mod's version.

---

## Combining Textures and Sounds

A single mod folder can contain both `Textures/` and `Sound/` subfolders:

```
PKCore/00-Mods/
└── TotalConversionMod/
    ├── Textures/
    │   ├── GSD1/
    │   │   └── ...
    │   └── GSD2/
    │       └── ...
    └── Sound/
        ├── BGM2/
        │   └── ...
        └── SEHD1/
            └── ...
```

---

## Multiple Mods Side by Side

All mod subfolders inside `00-Mods/` are loaded simultaneously. If two mods supply the same file, the **alphabetically last folder by name wins**:

```
PKCore/00-Mods/
├── AAA_BaseModPack/        ← Loaded first (lower priority)
│   └── Textures/
│       └── window_main.png
└── ZZZ_MyPersonalTweaks/   ← Loaded last (WINS)
    └── Textures/
        └── window_main.png  ← This version is used
```

**Tip:** Name your personal override folder with a `zz_` prefix to always have it win.

---

## Quick Setup Checklist

### Installing a Texture Mod

1. Create a folder under `PKCore/00-Mods/`, e.g. `MyMod/`
2. Create `MyMod/Textures/` (and optionally `GSD1/` / `GSD2/` subfolders)
3. Drop your `.png` / `.dds` files in with the correct names
4. Launch the game — no cache clearing needed (manifest auto-rebuilds)

### Installing a Sound Mod

1. Create a folder under `PKCore/00-Mods/`, e.g. `MySoundMod/`
2. Create `MySoundMod/Sound/<SubfolderMatchingGame>/`
3. Drop your `.acb` (and `.awb` if needed) files in
4. Launch the game — sound redirection is active immediately

---

## Troubleshooting

| Symptom | Likely Cause | Fix |
|---------|-------------|-----|
| Texture not replacing | Wrong filename or folder | Enable `LogReplaceableTextures = true` and check log |
| Texture loads in wrong game | Missing `GSD1/`/`GSD2/` subfolder | Move file into appropriate game subfolder |
| Sound not replacing | Path mismatch vs StreamingAssets/Sound | Compare your mod path against `StreamingAssets/Sound/` layout |
| Wrong mod winning | Alphabetical order | Rename your folder to start with `zz_` |
| Stale textures after adding files | Manifest cache | Delete `PKCore/Cache/texture_manifest.xml` and restart |

---

## Related Documentation

- [Custom Textures Guide](custom_textures_guide.md) — Full texture system documentation
- [Caching Mechanism](caching_mechanism.md) — How the texture manifest cache works
- [Configuration Guide](configuration_guide.md) — All config options
