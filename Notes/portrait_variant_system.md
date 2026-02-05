# Portrait Variant System

## Overview
The Portrait Variant System allows you to use different portrait expressions for the same character based on context. This is especially useful for built-in game portraits that use codes like `fp_053.png` instead of character names.

## System Components

### 1. PortraitMappings.json
Maps user-friendly character names to game portrait filenames.

**Location:** `PKCore/Config/PortraitMappings.json`

**Example:**
```json
{
  "Luc": "fp_053",
  "Viktor": "fp_001",
  "Flik": "fp_002",
  "Nanami": "fp_019",
  "Jowy": "fp_020"
}
```

### 2. PortraitVariants.json
Maps portrait filenames to expression variants.

**Location:** `PKCore/Config/PortraitVariants.json`

**Example:**
```json
{
  "fp_053": {
    "angry": "fp_053_angry.png",
    "sad": "fp_053_sad.png",
    "surprised": "fp_053_surprised.png"
  },
  "fp_001": {
    "determined": "fp_001_determined.png",
    "angry": "fp_001_angry.png"
  }
}
```

### 3. SpeakerOverrides.json (Enhanced)
Now supports expression syntax: `"CharacterName|expression"`

**Location:** `PKCore/Config/SpeakerOverrides.json`

**Example:**
```json
{
  "1004090030": "Luc|angry",
  "1004090045": "Viktor|determined",
  "1004090050": "Luc|sad",
  "1004090100": "Luc"
}
```

## How It Works

### Portrait Loading Priority

1. **With Expression:**
   - Text ID `1004090030` → Speaker Override: `"Luc|angry"`
   - Looks up `"Luc"` in PortraitMappings → `"fp_053"`
   - Looks up `"fp_053"` + `"angry"` in PortraitVariants → `"fp_053_angry.png"`
   - Loads `PKCore/Textures/NPCPortraits/fp_053_angry.png`

2. **Without Expression:**
   - Text ID `1004090100` → Speaker Override: `"Luc"`
   - Looks up `"Luc"` in PortraitMappings → `"fp_053"`
   - Loads `PKCore/Textures/NPCPortraits/fp_053.png` (default)

3. **Fallback Chain:**
   - If variant file doesn't exist → uses default portrait (`fp_053.png`)
   - If default doesn't exist → no portrait displayed
   - If no mapping exists → treats speaker name as portrait filename directly

### Directory Structure

```
PKCore/
├── Config/
│   ├── PortraitMappings.json
│   ├── PortraitVariants.json
│   └── SpeakerOverrides.json
└── Textures/
    ├── NPCPortraits/           # Shared portraits
    │   ├── fp_053.png          # Default Luc
    │   ├── fp_053_angry.png    # Angry variant
    │   ├── fp_053_sad.png      # Sad variant
    │   └── fp_001.png          # Default Viktor
    ├── GSD1/
    │   └── NPCPortraits/       # Suikoden 1 specific
    └── GSD2/
        └── NPCPortraits/       # Suikoden 2 specific
```

## Usage Examples

### Example 1: Using Built-In Portrait Variants

**Setup:**
1. Create portrait variants:
   - `fp_053.png` (default Luc)
   - `fp_053_angry.png` (angry Luc)
   
2. Configure PortraitMappings.json:
```json
{
  "Luc": "fp_053"
}
```

3. Configure PortraitVariants.json:
```json
{
  "fp_053": {
    "angry": "fp_053_angry.png"
  }
}
```

4. Set speaker override with expression:
```json
{
  "1004090030": "Luc|angry"
}
```

**Result:** Dialog with ID `1004090030` will display the angry Luc portrait.

### Example 2: Custom NPC Without Mapping

**Setup:**
1. Create portrait:
   - `TommyTheMerchant.png`

2. No mapping needed! Set speaker override directly:
```json
{
  "1004090050": "TommyTheMerchant"
}
```

**Result:** System will look for `TommyTheMerchant.png` directly (backwards compatible).

### Example 3: Multiple Expressions for One Character

**Setup:**
```json
// PortraitMappings.json
{
  "Luc": "fp_053"
}

// PortraitVariants.json
{
  "fp_053": {
    "normal": "fp_053.png",
    "angry": "fp_053_angry.png",
    "sad": "fp_053_sad.png",
    "surprised": "fp_053_surprised.png",
    "evil": "fp_053_evil.png"
  }
}

// SpeakerOverrides.json
{
  "1004090030": "Luc|evil",
  "1004090045": "Luc|angry",
  "1004090050": "Luc|sad",
  "1004090100": "Luc"  // Uses default fp_053.png
}
```

## Technical Details

### API Methods

**NPCPortraitPatch.ParseSpeakerOverride(string key)**
- Returns: `(string speakerName, string expression)`
- Parses speaker override format: `"Name|expression"`

**NPCPortraitPatch.GetPortraitPath(string speakerName, string expression = null)**
- Returns: Full file path to portrait PNG
- Handles character name mapping and variant resolution
- Implements fallback chain

**LoadPortraitTexture(string npcName, string expression = null)**
- Internal method
- Loads texture with variant support
- Searches GSD1 → GSD2 → Shared folders

### TextDatabasePatch Integration

The system automatically injects speaker tags with expressions:
- `<speaker:Luc>` - Default portrait
- `<speaker:Luc|angry>` - Variant portrait

These tags are then processed by the dialog system to load the correct portrait.

## Tips & Best Practices

1. **File Naming:** Use consistent naming for variants (e.g., `fp_053_angry.png`, `fp_053_sad.png`)
2. **Fallback Strategy:** Always provide a default portrait without expression
3. **Testing:** Enable `DetailedTextureLog` config to see portrait loading diagnostics
4. **Organization:** Group related portraits together in the same folder
5. **Portrait Size:** Match game's portrait dimensions (typically 256x256 or 512x512)

## Troubleshooting

**Portrait doesn't display:**
- Check `DetailedTextureLog` for loading errors
- Verify file paths and naming match exactly
- Ensure portrait file exists in NPCPortraits folder
- Check PortraitMappings.json for correct character name mapping

**Wrong portrait displays:**
- Verify SpeakerOverrides.json text ID is correct
- Check expression name matches PortraitVariants.json entry
- Use `LogTextIDs` config to identify correct text IDs

**Variant not working:**
- Ensure variant file exists in the correct folder
- Check PortraitVariants.json syntax
- Verify portrait filename mapping in PortraitMappings.json
- System will fall back to default portrait if variant is missing
