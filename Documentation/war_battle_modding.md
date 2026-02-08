# War Battle Modding Guide

## Overview

PKCore's War Battle system allows you to customize character stats, abilities, and special powers in **Suikoden 2 war battles**. This JSON-based modding system gives you complete control over how characters perform in strategic army battles.

> **Note**: This feature is **Suikoden 2 only** (GSD2). Suikoden 1 does not have war battles.

---

## Quick Start

### 1. Enable War Battle Modding

Edit `BepInEx/config/faospark.pkcore.cfg`:

```ini
[War Battle]
# Enable custom war battle abilities (default: true)
EnableWarAbilityMod = true
```

### 2. Configuration File Location

The war battle configuration file is located at:
```
<Game Root>/PKCore/Config/S2WarAbilities.json
```

**Full path example:**
```
D:\SteamLibrary\steamapps\common\Suikoden I and II HD Remaster\PKCore\Config\S2WarAbilities.json
```

### 3. Basic JSON Structure

```json
{
  "globalAbilities": [],
  "characterAbilities": {
    "3347": {
      "name": "Riou",
      "abilities": ["SP_SHINING_SHIELD", "SP_MOUNT", "SP_FLAME_SPEAR"],
      "attack": 12,
      "defense": 12
    }
  }
}
```

---

## How It Works

### Initialization

1. **Game Launch**: PKCore loads `S2WarAbilities.json` on startup
2. **War Battle Start**: When a war battle begins, the mod intercepts character initialization
3. **Ability Assignment**: Characters receive custom abilities and stats from the JSON config
4. **Usage Counts**: Each ability is given 9 uses per battle (default)

### Character Identification

Characters are identified by their **internal character ID**. This is NOT the same as their party slot or recruitment order.

**Finding Character IDs:**
- Enable `DetailedLogs = true` in config
- Start a war battle
- Check the log file for entries like: `Riou Character 3347 - ATK: 8, DEF: 9, Abilities: ...`
- The number after "Character" is the ID you use in JSON

---

## JSON Configuration

### Root Structure

```json
{
  "globalAbilities": [],        // Abilities to add to ALL characters
  "characterAbilities": {}      // Character-specific configurations
}
```

### Global Abilities (Advanced)

Apply abilities to **all characters** in war battles:

```json
{
  "globalAbilities": ["SP_MOUNT", "SP_CRITICAL"]
}
```

**Warning**: This can make battles unbalanced. Use sparingly.

### Character-Specific Configuration

Each character entry can customize:
- **Abilities** (up to 3)
- **Attack** stat
- **Defense** stat
- **Name** (for documentation only, not used by game)

#### Full Syntax

```json
"3347": {
  "name": "Riou",
  "abilities": ["SP_SHINING_SHIELD", "SP_MOUNT", "SP_FLAME_SPEAR"],
  "attack": 12,
  "defense": 12
}
```

#### Abilities Only

```json
"54": {
  "name": "Luc",
  "abilities": ["SP_MAGIC_WIND1", "SP_FLYING", "SP_NONE"]
}
```

#### Stats Only

```json
"4": {
  "name": "Viktor",
  "attack": 15,
  "defense": 10
}
```

#### Minimal Entry

```json
"93": {
  "name": "Juan",
  "abilities": ["SP_MEDICAL2"]
}
```
- Unspecified ability slots default to `SP_NONE`
- Stats remain unchanged if not specified

---

## Available Abilities

### Complete Ability List

| Ability Code | Description | Effect |
|--------------|-------------|--------|
| `SP_NONE` | No ability | Empty slot |
| `SP_MOUNT` | Cavalry | Movement Ability Up |
| `SP_AIMING` | Ranged Ability | Attack Enemy With 5 spaces|
| `SP_FLAME_SPEAR` | Fire Spear | Fire-based attack |
| `SP_SHINING_SHIELD` | Bright Shield | Large Heal AOE |
| `SP_HP_PLUS` | Heavy Armor | Defense plus 1  |
| `SP_CRITICAL` | Critical Hit | Chance for critical damage |
| `SP_SCOUT` | Scout | Reconnaissance ability |
| `SP_FOREST_WALK` | Forest Walk | Larger are movement through forests |
| `SP_MAGIC_WIND1` | Wind Magic Lv1 | Wind-based magic attack |
| `SP_MAGIC_FIRE1` | Fire Magic Lv1 | Fire-based magic attack |
| `SP_MAGIC_THUNDER1` | Thunder Magic Lv1 | Lightning-based attack |
| `SP_SEE_THROUGH` | See Through | Detect deception/traps |
| `SP_MEDICAL1` | Mend Self | Minor healing ability |
| `SP_MEDICAL2` | Heal | Heal 1 unit on a larger area |
| `SP_THROUGH_ROAD` | Evade | Provides a Chance in Avoiding Damage|
| `SP_BODY_GUARD` | Bodyguard | Protect nearby allies |
| `SP_CHEAR_UP` | Cheer Up | Gives Extra Turn |
| `SP_INVESTIGATION` | Investigation | Success Rates Disclosed |
| `SP_INVENTION` | Invention | Damage all Surrounding units including self |
| `SP_CONFUSED_FIGHT` | Melee | Increases Damage on Magic And Arm |
| `SP_FLYING` | Flying | Aerial unit |
| `SP_KIN_SLAYER` | Kin Slayer ⚠️ | DO NOT USE. Will Crash Game |
| `SP_FORCE_MOVE` | Force Move ⚠️ | Can force enemy movement |
| `SP_CHARGE` | Charge ⚠️ | Powerful charge attack (likely unused/cut content) |
| `SP_MAGIC_WIND2` | Wind Magic Lv2 ⚠️ | Do Not Use |

### Ability Slot Limits

- Each character can have **up to 3 abilities**
- Use `SP_NONE` to leave slots empty
- Abilities beyond slot 3 are ignored

### Unused Abilities

Some abilities are defined in game code but may not function:
- **`SP_CHARGE`**: Appears to be cut/unused content. May not have any effect in-game.

### Ability Usage

- Each ability starts with **9 uses** per battle
- Using an ability consumes 1 use
- When uses reach 0, the ability is unavailable for that battle

---

## Character ID Reference

### Main War Units

| Character ID | Name | ATK | DEF | Abilities |
|--------------|------|-----|-----|-----------|
| 3347 | Riou | 8 | 9 | SP_SHINING_SHIELD |
| 4 | Viktor | 8 | 7 | None |
| 118 | Ridley | 10 | 7 | SP_CRITICAL |
| 13 | Valeria | 8 | 8 | SP_MOUNT |
| 88 | Kiba | 7 | 12 | SP_HP_PLUS, SP_MOUNT |
| 35 | Hauser | 9 | 7 | SP_MOUNT |
| 12 | George | 11 | 8 | SP_CRITICAL |
| 120 | Maximillian | 6 | 7 | SP_MOUNT |
| 122 | Gilbert | 7 | 8 | None |
| 3 | Flik | 7 | 7 | SP_MOUNT |
| 102 | Teresa | 5 | 6 | SP_AIMING |
| 44 | Ayda | 6 | 5 | SP_MEDICAL1, SP_FOREST_WALK |
| 74 | Kasumi | 6 | 6 | SP_SCOUT |
| 123 | Boris | 9 | 8 | SP_SEE_THROUGH |
| 54 | Luc | 10 | 4 | SP_MAGIC_WIND1 |
| 62 | Mazus | 9 | 6 | SP_MAGIC_FIRE1 |

### Subunits

| Character ID | Name | ATK | DEF | Abilities |
|--------------|------|-----|-----|-----------|
| 90 | Shu | 0 | 0 | SP_CRITICAL, SP_SEE_THROUGH |
| 86 | Apple | 0 | 0 | SP_SEE_THROUGH |
| 119 | Klaus | 0 | 0 | SP_SEE_THROUGH, SP_MOUNT |
| 19 | Nanami | 0 | 0 | SP_MEDICAL1 |
| 53 | Tsai | 0 | 0 | SP_FLAME_SPEAR |
| 33 | Miklotov | 11 | 6 | SP_MOUNT |
| 34 | Camus | 9 | 8 | SP_MOUNT |
| 16 | Shin | 0 | 0 | SP_CRITICAL |
| 94 | (Unknown) | 0 | 0 | SP_SEE_THROUGH |
| 3348 | Freed Y | 6 | 7 | None |
| 93 | Huan | 0 | 0 | SP_MEDICAL2 |
| 23 | Tuta | 0 | 0 | SP_MEDICAL1 |
| 11 | Humphrey | 0 | 0 | SP_HP_PLUS |
| 87 | Templeton | 0 | 0 | SP_THROUGH_ROAD |
| 117 | Jeane | 0 | 0 | SP_MAGIC_THUNDER1 |
| 39 | Tai Ho | 0 | 0 | None |
| 101 | Yam Koo | 0 | 0 | None |
| 47 | Oulan | 0 | 0 | SP_BODY_GUARD |
| 113 | Annallee | 0 | 0 | SP_CHEAR_UP |
| 14 | Pesmerga | 0 | 0 | SP_MOUNT |
| 15 | Lorelai | 0 | 0 | None |
| 97 | Emilia | 0 | 0 | SP_INVESTIGATION |
| 106 | Adlai | 0 | 0 | SP_INVENTION |
| 24 | Hanna | 0 | 0 | None |
| 55 | Chaco | 0 | 0 | SP_FLYING |
| 59 | Gijimu | 0 | 0 | SP_CONFUSED_FIGHT |
| 60 | Koyu | 0 | 0 | SP_MEDICAL1 |
| 61 | Lowen | 0 | 0 | SP_CONFUSED_FIGHT |

**Finding More IDs:**
1. Set `DetailedLogs = true` in config
2. Start a war battle with different characters
3. Check BepInEx log for character ID listings

---

## Example Configurations

### Example 1: Buff Riou

Make Riou a powerhouse with strong abilities and stats:

```json
{
  "globalAbilities": [],
  "characterAbilities": {
    "3347": {
      "name": "Riou",
      "abilities": ["SP_SHINING_SHIELD", "SP_MOUNT", "SP_CRITICAL"],
      "attack": 15,
      "defense": 15
    }
  }
}
```

### Example 2: Magic Team

Boost magic users for spell-heavy strategies:

```json
{
  "globalAbilities": [],
  "characterAbilities": {
    "54": {
      "name": "Luc",
      "abilities": ["SP_MAGIC_WIND2", "SP_FLYING", "SP_SEE_THROUGH"],
      "attack": 12,
      "defense": 8
    },
    "62": {
      "name": "Mazus",
      "abilities": ["SP_MAGIC_FIRE1", "SP_MAGIC_THUNDER1", "SP_FLYING"],
      "attack": 12,
      "defense": 8
    },
    "117": {
      "name": "Jeane",
      "abilities": ["SP_MAGIC_THUNDER1", "SP_MAGIC_WIND1", "SP_MEDICAL1"],
      "attack": 10,
      "defense": 8
    }
  }
}
```

### Example 3: Cavalry Squad

Create a highly mobile mounted force:

```json
{
  "globalAbilities": [],
  "characterAbilities": {
    "3347": {
      "name": "Riou",
      "abilities": ["SP_MOUNT", "SP_CRITICAL", "SP_FLAME_SPEAR"],
      "attack": 13,
      "defense": 12
    },
    "3": {
      "name": "Flik",
      "abilities": ["SP_MOUNT", "SP_CRITICAL", "SP_THROUGH_ROAD"],
      "attack": 12,
      "defense": 11
    },
    "4": {
      "name": "Viktor",
      "abilities": ["SP_MOUNT", "SP_FLAME_SPEAR", "SP_HP_PLUS"],
      "attack": 14,
      "defense": 10
    }
  }
}
```

### Example 4: Support & Healing

Focus on healer and support characters:

```json
{
  "globalAbilities": [],
  "characterAbilities": {
    "93": {
      "name": "Juan",
      "abilities": ["SP_MEDICAL2", "SP_CHEAR_UP", "SP_BODY_GUARD"],
      "defense": 12
    },
    "113": {
      "name": "Annallee",
      "abilities": ["SP_CHEAR_UP", "SP_MEDICAL1", "SP_SEE_THROUGH"],
      "defense": 10
    }
  }
}
```

### Example 5: Mixed Strategy

Balanced configuration with various roles:

```json
{
  "globalAbilities": [],
  "characterAbilities": {
    "3347": {
      "name": "Riou",
      "abilities": ["SP_SHINING_SHIELD", "SP_MOUNT", "SP_CRITICAL"],
      "attack": 12,
      "defense": 12
    },
    "12": {
      "name": "George",
      "abilities": ["SP_CRITICAL", "SP_SEE_THROUGH", "SP_MOUNT"],
      "attack": 13,
      "defense": 10
    },
    "54": {
      "name": "Luc",
      "abilities": ["SP_MAGIC_WIND2", "SP_FLYING", "SP_NONE"],
      "attack": 11,
      "defense": 8
    },
    "102": {
      "name": "Teresa",
      "abilities": ["SP_AIMING", "SP_SCOUT", "SP_MOUNT"],
      "attack": 10,
      "defense": 10
    }
  }
}
```

---

## Troubleshooting

### Changes Not Applied

**Symptoms:**
- Characters have default abilities/stats
- No log messages about custom configuration

**Solutions:**
1. ✅ Check `EnableWarAbilityMod = true` in config
2. ✅ Verify JSON file is at correct location: `PKCore/Config/S2WarAbilities.json`
3. ✅ Validate JSON syntax using [JSONLint](https://jsonlint.com/)
4. ✅ Ensure character IDs are strings (in quotes): `"3347"` not `3347`
5. ✅ Set `DetailedLogs = true` to see if config is loading
6. ✅ Restart the game completely after editing JSON

### Invalid JSON Syntax

**Common Errors:**

❌ **Missing comma:**
```json
{
  "3347": {
    "name": "Riou"      // Missing comma here
    "abilities": []
  }
}
```

✅ **Correct:**
```json
{
  "3347": {
    "name": "Riou",     // Comma added
    "abilities": []
  }
}
```

❌ **Trailing comma:**
```json
{
  "characterAbilities": {
    "3347": {},
  }  // Trailing comma before closing brace
}
```

✅ **Correct:**
```json
{
  "characterAbilities": {
    "3347": {}
  }  // No trailing comma
}
```

❌ **Unquoted keys/values:**
```json
{
  3347: {  // Keys must be strings
    abilities: [SP_MOUNT]  // Values must be strings
  }
}
```

✅ **Correct:**
```json
{
  "3347": {
    "abilities": ["SP_MOUNT"]
  }
}
```

### Wrong Character ID

**Symptoms:**
- Custom abilities work for some characters but not others
- Log shows "Character 0" or incorrect character names

**Solutions:**
1. Enable `DetailedLogs = true`
2. Check logs during war battle initialization
3. Look for lines like: `Riou Character 3347 - ATK: 8, DEF: 9`
4. Use the correct character ID from the log (3347 in this case)
5. Note: Character IDs are **not** the same as party slots

### Abilities Not Working

**Symptoms:**
- Character has abilities in stats, but they don't work in battle
- Abilities show 0 uses

**Solutions:**
1. ✅ Verify ability name is spelled correctly (case-sensitive)
2. ✅ Use only abilities from the [Available Abilities](#available-abilities) list
3. ✅ Ensure abilities are in the correct format: `"SP_MOUNT"` not `"Mount"` or `"MOUNT"`
4. ✅ Check for typos: `SP_MAGIC_THUNDER1` not `SP_THUNDER1`

### Game Crashes

**Symptoms:**
- Game crashes when war battle starts
- Crash after editing JSON

**Solutions:**
1. Validate JSON syntax at [JSONLint](https://jsonlint.com/)
2. Remove recently added entries to isolate problem
3. Check for invalid ability names (typos)
4. Ensure attack/defense values are numbers, not strings
5. Delete `S2WarAbilities.json` to reset to defaults, then rebuild

---

## Advanced Tips

### Stat Balance Guidelines

**Recommended Stat Ranges:**
- **Early Game**: ATK 8-10, DEF 7-9
- **Mid Game**: ATK 10-12, DEF 9-11
- **Late Game**: ATK 12-15, DEF 11-14
- **Overpowered**: ATK 18+, DEF 17+

**Avoid:**
- ATK/DEF values above 20 (breaks game balance)
- DEF 0 (makes character one-shot vFLAME_SPEARble)

### Ability Combinations

**Offensive Combos:**
- `SP_MOUNT` + `SP_CRITICAL` + `SP_CHARGE` = Maximum damage cavalry
- `SP_MAGIC_FIRE1` + `SP_MAGIC_THUNDER1` + `SP_FLYING` = Versatile mage

**Defensive Combos:**
- `SP_SHINING_SHIELD` + `SP_HP_PLUS` + `SP_BODY_GUARD` = Ultimate tank
- `SP_SEE_THROUGH` + `SP_SCOUT` + `SP_MOUNT` = Intel specialist

**Support Combos:**
- `SP_MEDICAL2` + `SP_CHEAR_UP` + `SP_BODY_GUARD` = Team support
- `SP_THROUGH_ROAD` + `SP_SCOUT` + `SP_FORCE_MOVE` = Tactical control

### Performance Considerations

- **File Size**: Keep JSON under 100KB for fast loading
- **Character Count**: Modding 20-30 characters has no performance impact
- **Validation**: Always test war battles after major changes

---

## Technical Details

### Configuration Loading

1. **Location Check**: PKCore looks for `PKCore/Config/S2WarAbilities.json`
2. **Migration**: Automatically migrates from old `PKCore/S2WarAbilities.json` location
3. **Parsing**: JSON deserialized into internal config objects
4. **Validation**: Ability names converted to enum values
5. **Storage**: Character configs cached in memory for quick access

### Harmony Patching

PKCore patches the `w_chara.charaInit` method in the `w_chara` class:

**Patch Points:**
- **Postfix on `charaInit`**: After game initializes war characters
- **Iterates all characters**: Applies custom config to each character
- **Ability replacement**: Overwrites game's default ability arrays
- **Stat modification**: Updates attack/defense values directly

### Memory Structure

```
WAR_CHARA_TYPE (Character Object)
├── nouryoku[]       ← Abilities array (3 slots)
├── kaisu[]          ← Current uses remaining
├── kaisu_max[]      ← Maximum uses (9 default)
├── attack           ← Attack stat
├── defense          ← Defense stat
└── name             ← Character ID
```

### Ability Usage System

- **Uses per Battle**: Each ability starts with 9 uses
- **Usage Tracking**: `kaisu` array decrements on use
- **Recharge**: Uses reset at battle start
- **Depletion**: When `kaisu[slot]` reaches 0, ability is disabled

---

## FAQ

**Q: Can I mod Suikoden 1 war battles?**  
A: No, Suikoden 1 doesn't have war battles. This feature is Suikoden 2 only.

**Q: Will this affect regular battles?**  
A: No, this only affects **war battles** (army vs army). Regular turn-based battles are unchanged.

**Q: Can I give a character more than 3 abilities?**  
A: No, the game engine only supports 3 ability slots per character.

**Q: Do stat boosts carry over between battles?**  
A: Yes, custom stats are permanent for that playthrough until you edit the JSON again.

**Q: Can I reset to default abilities?**  
A: Yes, delete the character entry from JSON or set abilities to original values.

**Q: Why does "Character 0" appear in logs?**  
A: These are enemy units or unrecruited characters. You typically don't modify these.

**Q: Can I share my custom configurations?**  
A: Yes! Just share your `S2WarAbilities.json` file. Others can place it in their `PKCore/Config/` folder.

**Q: Does this work with other mods?**  
A: Yes, compatible with Suikoden Fix and other BepInEx mods. Load order doesn't matter.

**Q: What happens if I specify an invalid ability?**  
A: PKCore logs a warning and ignores that ability entry. The character will have fewer abilities than intended.

---

## Related Documentation

- [Configuration Guide](configuration_guide.md) - All config options explained
- [Custom Textures Guide](custom_textures_guide.md) - Replace war battle sprites/textures

---

## Credits

War Battle modding system designed and implemented by **faospark** for [Project Kyaro](https://www.nexusmods.com/suikoden1and2hdremaster/mods/6).

For support, visit the [Project Kyaro Nexus Mods page](https://www.nexusmods.com/suikoden1and2hdremaster/mods/6).
