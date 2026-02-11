# Placeholder Text Replacement

PKCore automatically replaces special text placeholders in dialogs with actual names from your save file.

## Supported Placeholders

### Character Name Placeholders

| Placeholder | Replaced With | Example |
|-------------|---------------|---------|
| `♂㈱` | Suikoden 2 protagonist name | "Riou" |
| `♂⑩` | Suikoden 1 protagonist name (save transfer) | "Tir" |
| `♂①` | Suikoden 2 HQ name | "Dunan" |
| `♂②` | Suikoden 2 HQ name (alternate) | "Dunan" |
| `♂■` | Suikoden 1 HQ name (save transfer) | "Rocklake" |

### Native Text Formatting

The game uses special characters for formatting dialog text:

| Character | Function | Usage |
|-----------|----------|-------|
| `∠` | Line break | Forces text to continue on next line within dialog |
| `∨` | Page break | Waits for player input before continuing dialog |
| `◎` | Choice line | Marks a line where user input/choice is needed |

**Example with line breaks:**
```
"Welcome to ♂①, ♂㈱!∠This is your new home."
```

**Result:**
```
Welcome to Dunan, Riou!
This is your new home.
```

**Example with page break:**
```
"The result is ∨5 points."
```

**Result:**
```
The result is [Player presses button]
5 points.
```

**Example with choice line:**
```
"What will you do?◎ Attack◎ Defend◎ Run"
```

**Result:**
```
What will you do?
 > Attack
   Defend
   Run
```

> **Note:** These formatting characters are part of the game's native text format and are automatically handled by the game engine.

## How It Works

1. **Load Save File** - When you load a save, PKCore extracts custom names
2. **Automatic Replacement** - All dialog text is processed automatically
3. **No Configuration** - Feature is always enabled, no setup required

## Examples

### Basic Name Replacement

**Original dialog text:**
```
"Welcome to ♂①, ♂㈱!"
```

**After replacement (with your save data):**
```
"Welcome to Dunan, Riou!"
```

### With Line Breaks

**Original dialog text:**
```
"♂㈱, welcome to ♂①!∠Your journey begins here."
```

**After replacement:**
```
Riou, welcome to Dunan!
Your journey begins here.
```

## Default Names

If no save is loaded or names are missing, these defaults are used:

- S2 Protagonist: "Hero"
- S1 Protagonist: "Tir"
- S2 HQ: "Dunan"
- S1 HQ: "Liberation"

## Technical Details

### Save Data Fields

Names are extracted from these save file fields:

- `bozu_name` → S2 protagonist
- `macd_name` → S1 protagonist (save transfer)
- `base_name` → S2 HQ
- `m_base_name` → S1 HQ (save transfer)

### Debug Logging

Enable `LogTextIDs` in the config to see replacement logs:

```
[TextDebug] Replaced placeholders in [msg_001:5]
[SaveDataProcessor] Names refreshed - S2 Hero: 'Riou', S1 Hero: 'Tir'
```

## Troubleshooting

**Q: Placeholders not being replaced?**
- Ensure you've loaded a save file
- Check that names are set in your save
- Enable debug logging to verify

**Q: Wrong names appearing?**
- Reload your save file
- Check save data is not corrupted

**Q: Want to disable this feature?**
- This feature cannot be disabled (it's core functionality)
- Names will always use defaults if save data unavailable
