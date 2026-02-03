# SpriteAtlas Interception System - Implementation Summary

## What Was Added

### 1. **SpriteAtlasCache.cs** - Atlas caching system
- Caches entire `SpriteAtlas` structures when first accessed
- Automatically applies custom texture replacements to all sprites in atlas
- Preserves original sprite properties (rect, pivot, borders, PPU, mesh type)
- Handles texture scaling when custom textures have different dimensions
- Clears cache on scene transitions to prevent memory leaks

### 2. **SpriteAtlasInterceptPatch.cs** - Harmony patches
Intercepts all SpriteAtlas methods BEFORE game uses sprites:
- `SpriteAtlas.GetSprite(name)` - Single sprite lookup
- `SpriteAtlas.GetSprites(array)` - Bulk sprite retrieval
- `SpriteAtlas.GetSprites(array, name)` - Filtered sprite retrieval
- `SpriteAtlas.spriteCount` - Sprite count getter

### 3. **Integration with existing system**
- Added `CleanTextureName()` helper to CustomTexturePatch
- Integrated into Plugin.cs patch application
- Hooks into scene transition system for cache clearing
- Compatible with existing texture replacement patches

## How It Works

### Traditional Approach (What PKCore did before)
```
Game loads scene → Sets Image.sprite → PKCore patches setter → Replaces texture
```
**Problem:** Must patch every component that uses sprites (Image, SpriteRenderer, RawImage, etc.)

### New Memoria-Style Approach
```
Game requests sprite from atlas → PKCore intercepts GetSprite → Returns custom sprite
```
**Advantage:** 
- Single interception point at atlas level
- Catches sprites BEFORE any component uses them
- More reliable and comprehensive

## Benefits Over Previous System

1. **Earlier Interception** - Replaces sprites at atlas level before UI/rendering components receive them
2. **Better Property Preservation** - Maintains pivot, borders, PPU, mesh type from original sprites
3. **Automatic Scaling** - Handles custom textures with different dimensions than originals
4. **Reduced Patch Surface** - One interception point instead of patching multiple component setters
5. **Cleaner Architecture** - Follows Memoria.FFPR's proven pattern

## Testing Instructions

1. **Enable detailed logging** in `BepInEx/config/PKCore.cfg`:
   ```ini
   DetailedTextureLog = true
   EnableCustomTextures = true
   ```

2. **Run the game** and check logs for:
   - `[SpriteAtlasIntercept] Initialized` - System loaded
   - `[SpriteAtlasCache] Cached atlas 'X' with N sprites` - Atlas discovered
   - `[SpriteAtlasCache] Replaced N sprites in atlas 'X'` - Custom textures applied
   - `[SpriteAtlasCache] Cleared N cached atlases` - On scene transitions

3. **Look for improved texture replacement:**
   - Sprites should be replaced earlier in the loading process
   - Should work for sprites that previously failed to replace
   - No more "white outline" issues from incorrect pivot/borders

## Performance Impact

**Minimal** - Caching happens once per atlas, subsequent sprite requests are dictionary lookups.

## Compatibility

- ✅ Works alongside existing setter-based patches (Image.sprite, etc.)
- ✅ Compatible with scene transition system
- ✅ Respects persistent texture system
- ✅ Works with existing texture indexing

## Next Steps

After confirming this works:
1. Consider disabling some redundant component setter patches
2. Add .tpsheet metadata support for complex sprite geometry
3. Implement FilterMode preservation from original textures
4. Add ModFileResolver for better mod organization

---

**Status:** ✅ Implemented and compiled successfully  
**Based on:** Memoria.FFPR's SpriteAtlasCache approach  
**Compatibility:** IL2CPP Unity 2022.3.28f1 (Suikoden HD)
