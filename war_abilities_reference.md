# War Abilities Reference

Complete list of all available war abilities you can use in `war_abilities.json`.

## Combat Abilities

### Offensive
- `SP_CHARGE` - Charge attack
- `SP_CRITICAL` - Critical hit ability
- `SP_AIMING` - Aimed shot ability
- `SP_FLAME_SPEAR` - Flame spear attack
- `SP_CONFUSED_FIGHT` - Confusion attack

### Magic Attacks
- `SP_MAGIC_FIRE1` - Fire magic (level 1)
- `SP_MAGIC_WIND1` - Wind magic (level 1)
- `SP_MAGIC_THUNDER1` - Thunder magic (level 1)

### Defensive
- `SP_SHINING_SHIELD` - Shield ability
- `SP_BODY_GUARD` - Bodyguard protection
- `SP_HP_PLUS` - HP boost

### Support
- `SP_MEDICAL1` - Healing (level 1)
- `SP_MEDICAL2` - Healing (level 2)
- `SP_CHEAR_UP` - Morale boost
- `SP_INVENTION` - Invention ability

## Movement Abilities

- `SP_MOUNT` - Mounted movement
- `SP_FLYING` - Flying movement
- `SP_FOREST_WALK` - Forest walking
- `SP_THROUGH_ROAD` - Road movement

## Tactical Abilities

- `SP_SEE_THROUGH` - See through fog of war
- `SP_SCOUT` - Scouting ability
- `SP_INVESTIGATION` - Investigation ability

## Special Abilities

- `SP_NONE` - No ability (empty slot)

## Usage Example

```json
{
  "globalAbilities": [],
  "characterAbilities": [
    {
      "name": "Hero",
      "abilities": [
        "SP_CHARGE",
        "SP_CRITICAL",
        "SP_MAGIC_FIRE1"
      ]
    },
    {
      "name": "Flik",
      "abilities": [
        "SP_CRITICAL",
        "SP_AIMING"
      ]
    }
  ]
}
```

## Notes

- Each character can have up to **3 abilities**
- Each ability has **9 uses** per battle (configurable)
- Use `"globalAbilities": []` to avoid applying abilities to all characters
- Characters not listed will keep their default game abilities
