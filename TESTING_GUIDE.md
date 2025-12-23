# GRSpriteRendererPatch Testing Guide

## What Changed
Added new `GRSpriteRendererPatch.cs` with 3 sprite interception patches:
1. `sprite` setter - Primary sprite assignment
2. `SetForceSprite()` - Forced sprite assignments
3. `OnEnable()` - Late-enabled sprites

## What to Look For

### 1. Check Logs on Game Start
Look for this in BepInEx console/log:
```
[Info   : PKCore] GRSpriteRenderer patches initialized
```

### 2. Look for New Replacement Messages
With `DetailedTextureLog` enabled, you should see:
```
[Info   : PKCore] [GRSpriteRenderer] Replaced sprite: <sprite_name>
[Info   : PKCore] [GRSpriteRenderer] Force-replaced sprite: <sprite_name>
[Info   : PKCore] [GRSpriteRenderer] Replaced sprite on enable: <sprite_name>
```

### 3. Test Scenarios

#### Scenario A: Character Sprites
1. Load a game area with characters
2. **Expected:** Character sprites should be replaced
3. **Log to check:** `[GRSpriteRenderer] Replaced sprite: sactx_...`

#### Scenario B: Background Sprites
1. Enter different areas (town, dungeon, etc.)
2. **Expected:** Background sprites should be replaced
3. **Log to check:** `[GRSpriteRenderer] Replaced sprite: t_vg...` or similar

#### Scenario C: Battle Sprites
1. Enter a battle
2. **Expected:** Battle UI and character sprites replaced
3. **Log to check:** `[GRSpriteRenderer] Replaced sprite: hp_rbat...`

#### Scenario D: Late-Enabled Sprites (NEW!)
1. Open menus, dialogs, or UI elements
2. **Expected:** UI sprites that appear after scene load should be replaced
3. **Log to check:** `[GRSpriteRenderer] Replaced sprite on enable: ...`

### 4. Compare Before/After

#### Before (Old Patches Only)
- Some sprites might be missed
- Only `SpriteRenderer.sprite` setter was patched
- No coverage for `GRSpriteRenderer` specifically

#### After (With GRSpriteRendererPatch)
- Better coverage for game's custom sprite renderer
- Catches forced sprite assignments
- Catches late-enabled sprites

### 5. Success Indicators

✅ **Good Signs:**
- More `[GRSpriteRenderer]` log messages
- Sprites that were previously missed are now replaced
- No errors or exceptions in logs

❌ **Problem Signs:**
- Errors mentioning `GRSpriteRenderer`
- Sprites still not being replaced
- Game crashes or freezes

### 6. Quick Test Steps

1. **Build the mod:**
   ```powershell
   dotnet build
   ```

2. **Copy to game:**
   ```powershell
   Copy-Item "bin\Debug\net6.0\PKCore.dll" "D:\SteamLibrary\steamapps\common\Suikoden I and II HD Remaster\BepInEx\plugins\PKCore.dll" -Force
   ```

3. **Enable detailed logging** in config:
   ```ini
   [Textures]
   DetailedTextureLog = true
   ```

4. **Launch game and check console**

5. **Walk around, enter battles, open menus**

6. **Review BepInEx log file** after testing

## What You're Testing

### Primary Goal
**Does `GRSpriteRendererPatch` catch sprite assignments that were previously missed?**

### How to Tell
- Compare log output before/after
- Look for sprites that now show `[GRSpriteRenderer]` prefix
- Check if any custom textures that weren't working before now work

## Notes
- The new patches run **in addition to** existing `CustomTexturePatch` patches
- You might see some sprites logged twice (once by old patch, once by new)
- This is expected and not a problem - we can optimize later

## If Something Goes Wrong

1. **Check BepInEx log** for errors
2. **Disable the new patch** by commenting out in `Plugin.cs`:
   ```csharp
   // harmony.PatchAll(typeof(GRSpriteRendererPatch));
   // GRSpriteRendererPatch.Initialize();
   ```
3. **Report what happened** so we can fix it

## Expected Outcome
More sprites should be replaced correctly, especially:
- UI elements that appear dynamically
- Sprites in menus/dialogs
- Any sprites that were previously inconsistent
