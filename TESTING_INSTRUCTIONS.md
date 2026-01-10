# War Ability Mod - Testing Instructions

## Current Status from Logs:
❌ Config file is empty (0 configurations loaded)
❌ Hook hasn't triggered yet (you're on the war map, not in a battle)

## Steps to Test:

### 1. Copy the config file:
```powershell
Copy-Item "d:\Appz\PKCore\war_abilities_example.json" "D:\SteamLibrary\steamapps\common\Suikoden I and II HD Remaster\BepInEx\plugins\war_abilities.json" -Force
```

### 2. Restart the game (to reload the config):
- Close Suikoden 2 completely
- Restart it
- Load your save

### 3. Start a WAR BATTLE (not just the war map):
- You need to actually engage in a battle on the war map
- The hook triggers when `w_chara.charaInit()` is called
- This happens when the battle **starts**, not when you're on the map

### 4. Check the logs:
```powershell
Get-Content "D:\SteamLibrary\steamapps\common\Suikoden I and II HD Remaster\BepInEx\LogOutput.log" | Select-String "War Ability"
```

## What You Should See:

If working correctly:
```
[Info   :    PKCore] Loaded 2 character configurations  ← Should NOT be 0
[Info   :    PKCore] Global abilities: 2                  ← Should NOT be 0
[Info   :    PKCore] [War Ability] w_chara.charaInit hook triggered!
[Info   :    PKCore] [War Ability] Character 0 - Current abilities: ...
[Info   :    PKCore] [War Ability] Ability modifications complete! Modified: X
```

## The Config File Contents:

This will:
- Give ALL characters "Magic" and "Holy Rune" abilities
- Character #1 (Hero) will get "Magic", "Holy Rune", "Charge", and "Flame Spear" instead

---

**TL;DR**: 
1. Run the copy command above
2. Restart the game
3. Start an ACTUAL war battle (not just walk on the map)
4. Check logs
