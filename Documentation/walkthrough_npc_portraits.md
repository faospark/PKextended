# NPC Portrait System - Full Documentation

## Overview

The NPC Portrait System allows custom portrait injection for NPCs that don't have portraits in the base game. It features a flexible directory search system, character name mapping, expression variants, and automatic game-specific detection.

**Note:** Currently supports **Suikoden II only**. Suikoden I support is deferred for future implementation.

---

## Architecture

### Core Components

**1. PortraitVariants.cs** - Portrait discovery and variant management
- Recursively searches ALL folders under `PKCore/Textures/`
- Handles character name → portrait filename mappings
- Manages expression variants (angry, sad, happy, etc.)
- Provides unified API for portrait path resolution

**2. PortraitSystemPatch.cs** - Portrait injection and rendering
- Patches dialogue/message window system
- Loads textures and creates sprites on-demand
- Handles dialog text replacement and speaker name injection
- Integrates with PortraitVariants for all file discovery

---

## Directory Structure & Search Priority

### How It Works

The system searches **recursively** through all folders under `PKCore/Textures/` with the following priority:

**Priority Order:**
1. **GSD2 folders** (highest priority) - All folders under `GSD2/` and subdirectories
2. **GSD1 folders** (medium priority - deferred) - All folders under `GSD1/` and subdirectories  
3. **Root folders** (lowest priority) - All folders directly under `Textures/` and subdirectories

### Example Layouts

**Simple Layout:**
```
PKCore/Textures/
├── GSD2/NPCPortraits/          ← Suikoden II portraits (primary)
│   ├── fp_140.png              ← Luca Blight base portrait
│   ├── fp_140_laugh1.png       ← Luca laughing
│   ├── fp_140_shout.png        ← Luca shouting
│   └── fp_140_blood.png        ← Luca bloodied
└── NPCPortraits/               ← Shared/fallback portraits
    └── fp_219.png              ← Generic "?" portrait
```

**Flexible Layout (all valid):**
```
PKCore/Textures/
├── GSD2/
│   ├── Portraits/          ← Works! System searches recursively
│   ├── Characters/         ← Works!
│   └── Custom/Faces/       ← Works! Any folder name/depth
├── GSD2/CustomStuff/       ← Works!
└── SharedAssets/           ← Works! (lowest priority)
```

**Note:** GSD1 folders exist in the structure but S1 support is currently deferred.

**Key Points:**
- ✅ Use ANY folder names - no specific naming required
- ✅ Nest folders as deep as you want
- ✅ Place portraits anywhere under `Textures/GSD2/` (S2 active) or `Textures/GSD1/` (S1 deferred)
- ✅ Game-specific folders always override shared folders

---

## Character Name Mapping

### Purpose
Map character names to portrait filenames, enabling:
- Use of game asset IDs (e.g., `fp_053` for Luc)
- Consistent naming across different character references
- Support for characters with multiple names/aliases

### Configuration: `PKCore/Config/PortraitMappings.json`

**Real example from active config:**
```json
{
  "Luca": "fp_140"
}
```

### How It Works

**Without mapping:** `"Bonaparte"` → searches for `bonaparte.png`
**With mapping:** `"Luca"` → maps to `fp_140` → searches for `fp_140.png`

**Fallback:** If no mapping exists, uses the character name itself as the filename (backwards compatible).

---

## Expression Variants

### Purpose
Support multiple emotional expressions for the same character:
- `viktor_angry.png`, `viktor_sad.png`, `viktor_happy.png`
- Switch portraits dynamically based on dialogue context

### Configuration: `PKCore/Config/PortraitVariants.json`

**Real example from active config (Luca Blight expressions):**
```json
{
  "fp_140": {
    "neutral": "fp_140_neutral.png",
    "laugh1": "fp_140_laugh1.png",
    "laugh2": "fp_140_laugh2.png",
    "troll": "fp_140_troll.png",
    "shout": "fp_140_shout.png",
    "blood": "fp_140_blood.png",
    "bloodfinal": "fp_140_bloodfinal.png",
    "mad": "fp_140_mad.png",
    "sneaky": "fp_140_sneaky.png",
    "disgust1": "fp_140_disgust1.png"
  },
  "fp_001": {
    "highland": "fp_001_highland.png"
  }
}
```

### Usage in SpeakerOverrides

Use pipe syntax `|` to specify expressions:

**SpeakerOverrides.json (real examples):**
```json
{
  "message:1002160039": "Luca|laugh1",
  "message:1002160048": "Luca|shout",
  "message:1004090030": "Luca|blood",
  "message:1004090048": "Luca|bloodfinal",
  "message:1010100035": "Luca|troll",
  "message:1003300033": "Nash",
  "message:1002070055": "Bonaparte"
}
```

**Result:**
- `"Luca|laugh1"` → loads `fp_140_laugh1.png` (via mapping + variant)
- `"Luca|shout"` → loads `fp_140_shout.png` (via mapping + variant)
- `"Bonaparte"` → loads `bonaparte.png` (direct name)

---

## Search Flow & Fallbacks

### Complete Resolution Process

**Example:** Loading portrait for `"Luca|blood"`

1. **Parse expression:** `"Luca|blood"` → character=`"Luca"`, expression=`"blood"`
2. **Map name:** `"Luca"` → `"fp_140"` (via PortraitMappings.json)
3. **Try variant:** `"fp_140"` + `"blood"` → `"fp_140_blood.png"` (via PortraitVariants.json)
4. **Search directories (priority order):**
   - GSD2 folders (recursively) ← **Primary for S2**
   - GSD1 folders (recursively) ← Deferred
   - Root folders (recursively)
5. **If variant not found:** Fall back to default `fp_140.png`
6. **If default not found:** Fall back to `fp_219.png` (question mark)
7. **If all fail:** Log warning and return null

---

## Integration with Dialog System

The portrait system integrates with two JSON configuration files for dialog control:

### SpeakerOverrides.json
Maps dialog IDs to speaker names (with optional expressions):

**Real examples:**
```json
{
  "message:1002160039": "Luca|laugh1",
  "message:1004090030": "Luca|blood",
  "message:1003300033": "Nash",
  "message:1002070055": "Bonaparte",
  "message:1009150074": "Baby Dragon"
}
```

**Format:**
- `"message:1234567890"` - Dialog message ID (find with LogTextIDs enabled in config)
- Use pipe `|` for expressions: `"CharacterName|expression"`
- Can also match by original text (text-based matching)

### DialogOverrides.json
Replaces dialog text:

**Real example:**
```json
{
  "_comment": "Dialog text replacement by ID. Enable LogTextIDs to find IDs.",
  "add_message:1120": "©2026 Konami Digital Entertainment",
  "message:1002070055": "Piip!",
  "message:1002070058": "GWHAAACK!!!!"
}
```

**Bonaparte's duck sounds are replaced via these overrides!**

---

## Initialization & Auto-Reload

### On Game Launch
1. `PortraitVariants.Initialize()` - Discovers all portrait directories
2. `PortraitSystemPatch.Initialize()` - Creates default folders, preloads portraits
3. Loads JSON configs (PortraitMappings, PortraitVariants, SpeakerOverrides, DialogOverrides)

### On Game Switch
- `GameDetection.OnGameChanged` event triggers
- Portraits automatically reload with new priority
- Game-specific folders take precedence
- **Note:** S2 fully supported, S1 support deferred

---

## Example Scenarios

### Scenario 1: Simple NPC Portrait (No Mapping)
**Setup:**
```
Textures/GSD2/NPCPortraits/bonaparte.png
```
**Config:** None required (uses filename as character name)

**SpeakerOverrides.json:**
```json
{
  "message:1002070055": "Bonaparte"
}
```

**Result:** Bonaparte portrait appears when message 1002070055 displays

---

### Scenario 2: NPC with Multiple Expressions (Real Config)
**Setup:**
```
Textures/GSD2/NPCPortraits/fp_140.png
Textures/GSD2/NPCPortraits/fp_140_laugh1.png
Textures/GSD2/NPCPortraits/fp_140_shout.png
Textures/GSD2/NPCPortraits/fp_140_blood.png
Textures/GSD2/NPCPortraits/fp_140_bloodfinal.png
```

**PortraitMappings.json:**
```json
{
  "Luca": "fp_140"
}
```

**PortraitVariants.json:**
```json
{
  "fp_140": {
    "laugh1": "fp_140_laugh1.png",
    "shout": "fp_140_shout.png",
    "blood": "fp_140_blood.png",
    "bloodfinal": "fp_140_bloodfinal.png"
  }
}
```

**SpeakerOverrides.json:**
```json
{
  "message:1002160039": "Luca|laugh1",
  "message:1002160048": "Luca|shout",
  "message:1004090030": "Luca|blood",
  "message:1004090048": "Luca|bloodfinal"
}
```

**Result:**
- Message 1002160039 shows Luca laughing
- Message 1002160048 shows Luca shouting  
- Message 1004090030 shows Luca bloodied
- Message 1004090048 shows Luca in final bloodied state

**This is the actual setup for Luca Blight in Suikoden II!**

---

### Scenario 3: Finding Dialog IDs

**Enable logging in BepInEx config:**
```
[NPCPortraits]
LogTextIDs = true
```

**In-game console output:**
```
[DialogBoxScalePatch] message:1002160039 - Speaker: Luca - Text: "Hahaha!"
```

**Use this ID in SpeakerOverrides.json to assign portrait!**

---

## Development Notes

### Key Methods

**PortraitVariants.GetPortraitPath(characterName, expression)**
- Main entry point for portrait resolution
- Returns full file path or null
- Handles all mapping, variants, and fallbacks

**PortraitSystemPatch.LoadPortraitTexture(characterName, expression)**
- Loads PNG file as Texture2D
- Calls PortraitVariants.GetPortraitPath() internally
- Creates sprite with proper settings

### Performance
- Portraits preloaded on initialization
- Directory scan happens once at startup
- Sprite creation is lazy (on-demand)
- Base sprite (fp_129) cached permanently

### Logging
Enable detailed logs in mod config:
```
[PotraitSystem] Found: GSD2/NPCPortraits/fp_140_blood.png
[PortraitVariants] Using variant: Luca (blood) -> fp_140_blood.png
[PortraitVariants] Using default: Bonaparte -> bonaparte.png
[DialogBoxScalePatch] message:1002160039 - Speaker: Luca|laugh1 - Text: "Hahaha!"
```

---

## Migration from Old System

The old system required specific folder names. The new system is fully flexible:

**Old (rigid):**
```
Textures/NPCPortraits/GSD2/fp_140.png  ❌ No longer required
```

**New (flexible):**
```
Textures/GSD2/[AnyFolder]/fp_140.png  ✅ Works anywhere under GSD2/
```

Existing portraits will continue working - just move them to the recommended structure for clarity.

---

## Current Implementation Status

✅ **Suikoden II** - Fully implemented and active
- Portrait injection working
- Expression variants supported (see Luca Blight example)
- Dialog and speaker overrides functional
- Example configs available in game directory

⏳ **Suikoden I** - Deferred for future implementation
- Directory structure exists but S1 detection/loading paused
- Focus on S2 feature completion first
- Will be activated in future update
