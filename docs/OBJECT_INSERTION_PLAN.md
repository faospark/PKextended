# Object Insertion Feature - Implementation Plan

## Goal

Implement a system to insert custom objects and characters into Suikoden HD Remaster field scenes at specified coordinates. The system will hook into `MapBGManagerHD` to add objects to the scene after it loads.

## User Review Required

> [!IMPORTANT]
> **Research Phase First**
> Before implementing the full object insertion system, we should start with a **diagnostic patch** to log existing objects and understand their structure. This will help us:
> - Verify our understanding of `MapBGManagerHD`
> - See what properties `MapEventObjectHD` and `MapSpriteHD` have
> - Understand how objects are structured in real scenes
> - Test coordinates and positioning

**Question for you**: Should we start with the diagnostic logging patch first, or would you prefer to dive straight into the full implementation?

---

## Proposed Changes

### Phase 1: Diagnostic Logging (Recommended First Step)

#### [NEW] [Patches/MapBGManagerHDDiagnostics.cs](file:///d:/Appz/PKCore/Patches/MapBGManagerHDDiagnostics.cs)

**Purpose**: Log all objects in a scene to understand structure

**What it does**:
- Patches `MapBGManagerHD.Load()` with a postfix
- Logs counts of all object lists
- Logs detailed info about first few objects in each list
- Helps understand object structure before implementing insertion

**Key features**:
- Conditional on config setting `EnableObjectDiagnostics`
- Logs object IDs, names, positions, types
- Non-intrusive (read-only, doesn't modify anything)

---

### Phase 2: Object Insertion System

#### [NEW] [Patches/ObjectInsertionPatch.cs](file:///d:/Appz/PKCore/Patches/ObjectInsertionPatch.cs)

**Purpose**: Main patch for inserting custom objects

**What it does**:
- Hooks into `MapBGManagerHD.Load()` postfix
- Reads object configuration from JSON file
- Creates and inserts objects into appropriate lists
- Handles different object types (sprites, event objects, animated objects)

**Key methods**:
- `Load_Postfix()` - Main hook point
- `InjectCustomObjects()` - Reads config and creates objects
- `CreateEventObject()` - Creates `MapEventObjectHD` instances
- `CreateSprite()` - Creates `MapSpriteHD` instances

---

#### [MODIFY] [ModConfiguration.cs](file:///d:/Appz/PKCore/ModConfiguration.cs)

Add new configuration options:

```csharp
// Object Insertion Settings
public ConfigEntry<bool> EnableObjectInsertion { get; private set; }
public ConfigEntry<bool> EnableObjectDiagnostics { get; private set; }
public ConfigEntry<string> ObjectConfigPath { get; private set; }
```

**Configuration**:
```ini
[Object Insertion]
EnableObjectInsertion = false
EnableObjectDiagnostics = true  # For Phase 1
ObjectConfigPath = BepInEx/plugins/PKCore/Objects/objects.json
```

---

#### [MODIFY] [Plugin.cs](file:///d:/Appz/PKCore/Plugin.cs)

Add patch initialization in `ApplyPatches()`:

```csharp
// Object Insertion Diagnostics
if (Config.EnableObjectDiagnostics.Value)
{
    Log.LogInfo("Applying Object Diagnostics patches...");
    harmony.PatchAll(typeof(MapBGManagerHDDiagnostics));
}

// Object Insertion
if (Config.EnableObjectInsertion.Value)
{
    Log.LogInfo("Applying Object Insertion patches...");
    harmony.PatchAll(typeof(ObjectInsertionPatch));
    ObjectInsertionPatch.Initialize();
}
```

---

#### [NEW] [Objects/objects.json](file:///d:/Appz/PKCore/Objects/objects.json)

**Purpose**: Configuration file for custom objects

**Example structure**:
```json
{
  "objects": [
    {
      "scene": "town_gregminster",
      "type": "event_object",
      "id": 9000,
      "position": { "x": 10.5, "y": 5.0 },
      "sprite": "npc_generic_01",
      "texture": "custom_npc.png",
      "interactable": true,
      "message": "Hello, traveler!"
    },
    {
      "scene": "town_gregminster",
      "type": "sprite",
      "position": { "x": 15.0, "y": 8.0 },
      "sprite": "obj_barrel_01",
      "texture": "custom_barrel.png",
      "layer": 0
    }
  ]
}
```

---

## Research Dependencies

> [!WARNING]
> **Additional Research Needed**
> 
> Before Phase 2 implementation, we need to research:
> 
> 1. **MapEventObjectHD constructor** - How to create instances
> 2. **MapSpriteHD constructor** - How to create sprite instances  
> 3. **Object initialization** - What fields must be set
> 4. **Scene identification** - How to detect which scene is loading
> 5. **Coordinate system** - Verify coordinate ranges and scaling
> 
> **Phase 1 diagnostic logging will help answer these questions!**

---

## Verification Plan

### Phase 1: Diagnostic Logging

#### Test 1: Enable Diagnostics and Check Logs
```bash
# 1. Build the mod with diagnostic patch
# 2. Set config: EnableObjectDiagnostics = true
# 3. Launch game and enter a town (e.g., Gregminster)
# 4. Check BepInEx/LogOutput.log for object listings
```

**Expected output**:
```
[Info : PKCore] === MapBGManagerHD Scene Loaded ===
[Info : PKCore] Event Objects: 15
[Info : PKCore]   - Object 0: ID=100, Name=npc_merchant, Pos=(10.5, 5.0)
[Info : PKCore]   - Object 1: ID=101, Name=npc_guard, Pos=(15.0, 8.0)
[Info : PKCore] Sprites: 45
[Info : PKCore] Anim Objects: 3
```

**Success criteria**:
- Logs appear when entering town scenes
- Object counts are reasonable (>0)
- Position data is visible
- No errors or crashes

---

### Phase 2: Object Insertion

#### Test 2: Insert Simple Sprite Object
```bash
# 1. Create objects.json with one simple sprite
# 2. Set config: EnableObjectInsertion = true
# 3. Launch game and enter the specified scene
# 4. Verify object appears at specified coordinates
```

**Success criteria**:
- Custom object appears in scene
- Object is at correct position
- No crashes or errors
- Object persists during scene

#### Test 3: Insert Interactive Event Object
```bash
# 1. Add event object to objects.json
# 2. Launch game and interact with object
# 3. Verify message displays
```

**Success criteria**:
- Object is interactable
- Message displays correctly
- Interaction doesn't crash game

---

## Implementation Notes

### Why Start with Diagnostics?

1. **Understand Real Data**: See actual object structures from the game
2. **Verify Assumptions**: Confirm our analysis of `MapBGManagerHD` is correct
3. **Safe Testing**: Read-only operation, can't break anything
4. **Learn Coordinates**: See real coordinate values used in scenes
5. **Identify Scene Names**: Discover how scenes are identified

### Object Creation Challenges

We don't yet know:
- How to properly construct `MapEventObjectHD` instances
- What fields are required vs optional
- How the game initializes these objects
- Whether we need to call specific initialization methods

**The diagnostic patch will help us discover this information!**

---

## Next Steps After This Plan

1. **Review this plan** - Let me know if you want to proceed
2. **Implement Phase 1** - Create diagnostic patch
3. **Test and analyze logs** - Understand object structure
4. **Research object creation** - Use Unity Explorer + logs
5. **Implement Phase 2** - Build insertion system
6. **Test with simple objects** - Verify basic functionality
7. **Add advanced features** - Interactivity, animations, etc.

---

## Questions for You

1. Should we start with Phase 1 (diagnostics) or skip to Phase 2?
2. Do you have a specific scene you want to test in (e.g., Gregminster town)?
3. What type of object do you want to insert first (sprite, NPC, animated object)?
4. Do you want to research `MapEventObjectHD` in dnSpy first, or let the diagnostic patch guide us?
