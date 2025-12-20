# PKCore - Migration Guide

**Quick Reference for Upgrading from PKextended (v1.5.x) to PKCore (v1.6.0)**

## Quick Steps

1. **Remove** old `PKextended.dll` from `BepInEx/plugins/`
2. **Add** new `PKCore.dll` to `BepInEx/plugins/`
3. **Rename** folder: `BepInEx/plugins/PKextended/` → `BepInEx/plugins/PKCore/` (if you have custom textures)
4. **Launch** game (new config will auto-generate)

## What Changed?

- Project renamed: **PKextended** → **PKCore**
- Textures folder: `PKextended/Textures/` → `PKCore/Textures/`
- Config file: `faospark.pkextended.cfg` → `faospark.pkcore.cfg`

See full migration guide for detailed instructions and troubleshooting.
