# JSON Configuration System - Quick Guide

## ✅ What Changed

Your war ability mod now uses **JSON configuration** instead of hardcoded C# code!

### Benefits:
- ✅ **No rebuild needed** - just edit JSON and restart game
- ✅ **Easy to share** - send your config to friends
- ✅ **Clear examples** - built-in reference for all abilities
- ✅ **Validation** - automatic error checking

---

## 📁 Configuration File Location

```
GameRoot/PKCore/war_abilities.json
```

The file will be created automatically on first run in the same folder as PKCore.dll.
This keeps all mod files together in one place!

---

## 🎮 How to Use

### Example 1: Give Character #1 God-Mode Abilities

Edit `war_abilities.json`:

```json
{
  "globalAbilities": [],
  
  "characterAbilities": {
    "1": {
      "name": "Main Character",
      "abilities": [
        "SP_CRITICAL",
        "SP_HP_PLUS",
        "SP_CHARGE",
        "SP_FLYING"
      ]
    }
  }
}
```

### Example 2: Configure Multiple Characters

```json
{
  "globalAbilities": [],
  
  "characterAbilities": {
    "1": {
      "name": "Hero",
      "abilities": ["SP_CRITICAL", "SP_HP_PLUS", "SP_CHARGE"]
    },
    "5": {
      "name": "Mage",
      "abilities": ["SP_MAGIC_FIRE1", "SP_MAGIC_THUNDER1", "SP_MAGIC_WIND1"]
    },
    "10": {
      "name": "Healer",
      "abilities": ["SP_MEDICAL1", "SP_MEDICAL2", "SP_CHEAR_UP"]
    }
  }
}
```

### Example 3: Give ALL Characters Same Abilities

```json
{
  "globalAbilities": ["SP_CRITICAL", "SP_HP_PLUS"],
  
  "characterAbilities": {}
}
```

---

## 📋 Available Abilities

Check the `_availableAbilities` section in the JSON file for the complete list:

**Combat:** `SP_CRITICAL`, `SP_CHARGE`, `SP_HP_PLUS`, `SP_COUNTER`, `SP_BERSERK`, `SP_PROTECT`, `SP_ARCHERY`

**Magic:** `SP_MAGIC_FIRE1/2`, `SP_MAGIC_THUNDER1/2`, `SP_MAGIC_WIND1/2`

**Support:** `SP_MEDICAL1/2`, `SP_CHEAR_UP`, `SP_SCOUT`, `SP_FLYING`, `SP_SUPPLY`, `SP_CONFUSE`

**Special:** `SP_SPECIAL1-6`

---

## 🔍 Finding Character Indices

1. Enable the mod: `EnableWarAbilityMod = true`
2. Start a war battle
3. Check `LogOutput.log` for messages like:
   ```
   === WAR_CHARA_TYPE Created ===
   Character Name Index: 1
   ```
4. Use that number in your JSON config

---

## 🔄 Workflow

1. **Edit** `war_abilities.json`
2. **Save** the file
3. **Restart** the game
4. **Test** in a war battle
5. **Check logs** to verify

No rebuild needed! 🎉

---

## 🐛 Troubleshooting

**Config not loading?**
- Check file is at: `BepInEx/config/PKCore/war_abilities.json`
- Verify JSON syntax is valid (no trailing commas, proper quotes)
- Check `LogOutput.log` for parsing errors

**Abilities not applying?**
- Verify character index is correct (check logs)
- Make sure ability names match exactly (case-sensitive)
- Confirm `EnableWarAbilityMod = true` in main config

**JSON syntax errors?**
- Use a JSON validator online
- Check for missing commas or quotes
- Remove any comments (not standard JSON)
