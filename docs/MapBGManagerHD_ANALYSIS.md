# MapBGManagerHD Analysis
**Based on Decompiled Code from dnSpy**

## Overview
`MapBGManagerHD` is the **main manager class** for field/map backgrounds and objects in Suikoden HD Remaster. This is your primary target for object insertion!

**Class Location**: `GSD2.dll` → `MapBGManagerHD`  
**Inherits From**: `MapBGManager`

---

## 🎯 Key Object Lists (CRITICAL FOR INSERTION)

These are the **lists that store all objects in a scene**. You'll need to add your custom objects to these lists!

### 1. **sprites** - `List<MapSpriteHD>`
```csharp
public List<MapSpriteHD> sprites
```
- **Purpose**: Stores all map sprites (background tiles, decorative sprites)
- **Type**: `MapSpriteHD` - investigate this class next
- **Usage**: Background graphics, tiles, static decorations

### 2. **eventObjects** - `List<MapEventObjectHD>` ⭐ MOST IMPORTANT
```csharp
public List<MapEventObjectHD> eventObjects
```
- **Purpose**: Stores all **interactive event objects** (NPCs, doors, chests, etc.)
- **Type**: `MapEventObjectHD` - this is likely what you want for interactive objects
- **Key Method**: `FindEventObject(int id)` - finds event objects by ID
- **Usage**: NPCs, interactive objects, anything that can trigger events

### 3. **animObjects** - `List<MapObjAnimHD>`
```csharp
public List<MapObjAnimHD> animObjects
```
- **Purpose**: Stores animated objects (save points, flags, animated decorations)
- **Type**: `MapObjAnimHD`
- **Usage**: Animated props, effects

### 4. **flagCheckObjects** - `List<MapFlagCheckObjectHD>`
```csharp
public List<MapFlagCheckObjectHD> flagCheckObjects
```
- **Purpose**: Objects that appear/disappear based on game flags
- **Type**: `MapFlagCheckObjectHD`
- **Usage**: Conditional objects (appear after story events)

### 5. **particles** - `List<ParticleSystem>`
```csharp
public List<ParticleSystem> particles
```
- **Purpose**: Particle effects (smoke, sparkles, etc.)
- **Type**: Unity's `ParticleSystem`
- **Usage**: Visual effects

### 6. **spriterendereres** - `List<SpriteRenderer>`
```csharp
public List<SpriteRenderer> spriterendereres
```
- **Purpose**: All sprite renderers in the scene
- **Type**: Unity's `SpriteRenderer`
- **Usage**: Rendering sprites

---

## 🔧 Key Methods for Object Management

### Object Refresh/Update
```csharp
public void RefleshObject(int id, Vector2 pos, bool isVisible, int an, int eventMapNo, bool isInitVisible)
```
- **Purpose**: Updates/refreshes an object's state
- **Parameters**:
  - `id`: Object ID
  - `pos`: Position (Vector2)
  - `isVisible`: Visibility flag
  - `an`: Animation number
  - `eventMapNo`: Event map number
  - `isInitVisible`: Initial visibility

### Finding Event Objects
```csharp
public MapEventObjectHD FindEventObject(int id)
```
- **Purpose**: Find an event object by its ID
- **Returns**: `MapEventObjectHD` or null
- **Usage**: Retrieve specific event objects

### Scene Loading
```csharp
public override IEnumerator Load()
```
- **Purpose**: Loads the map/scene
- **Returns**: IEnumerator (coroutine)
- **⭐ HOOK POINT**: Patch this method to inject your custom objects after scene loads!

### Initialization
```csharp
public void Init()
```
- **Purpose**: Initializes the manager
- **⭐ HOOK POINT**: Another potential injection point

### Frame Update
```csharp
public void FrameUpdate(bool _isPause)
```
- **Purpose**: Called every frame to update objects
- **Parameter**: `_isPause` - whether game is paused

### Map Setup
```csharp
public void SetMap(MAP map, GameObject obj)
```
- **Purpose**: Sets up the map data
- **Parameters**:
  - `map`: MAP data structure
  - `obj`: GameObject to attach to

---

## 🎮 Other Important Fields

### Asset Reference
```csharp
public GameObject asset
```
- **Purpose**: The main GameObject asset for this map
- **Usage**: Parent object for all map elements

### Waypoint Manager
```csharp
public WaypointMovementManager wpmanager
```
- **Purpose**: Manages NPC movement along waypoints
- **Usage**: NPC pathfinding and movement

### Audio
```csharp
public List<CriAtomSource> atomList
```
- **Purpose**: Audio sources for ambient sounds
- **Usage**: Background music, ambient sounds

### Night Mode
```csharp
public bool IsNight { get; set; }
public void SetNight()
public void ClearNight()
```
- **Purpose**: Day/night cycle management
- **Usage**: Changes lighting/shaders for night scenes

---

## 📍 Coordinate System Hints

### Vector2 Usage
The class uses `Vector2` for positions:
```csharp
public void SetLayerPosition(int layer, Vector2 v, Vector2 fv, bool isFloatScroll = false)
public void RefleshObject(int id, Vector2 pos, ...)
```
- **Coordinate Type**: `Vector2` (X, Y)
- **Likely 2D coordinate system** with layers for depth

### HD Scale
```csharp
public static int HDScale
```
- **Purpose**: Scaling factor for HD graphics
- **Usage**: Might need to multiply your coordinates by this value

---

## 🎯 Implementation Strategy

### Phase 1: Hook into Scene Loading
**Target Method**: `MapBGManagerHD.Load()`

Create a Harmony postfix patch:
```csharp
[HarmonyPatch(typeof(MapBGManagerHD), "Load")]
[HarmonyPostfix]
public static IEnumerator Load_Postfix(IEnumerator __result, MapBGManagerHD __instance)
{
    // Let original Load complete
    while (__result.MoveNext())
        yield return __result.Current;
    
    // Now inject your custom objects
    InjectCustomObjects(__instance);
}
```

### Phase 2: Add Objects to Lists
```csharp
private static void InjectCustomObjects(MapBGManagerHD manager)
{
    // Example: Add a custom event object
    MapEventObjectHD customObject = CreateCustomEventObject();
    manager.eventObjects.Add(customObject);
    
    // Example: Add a custom sprite
    MapSpriteHD customSprite = CreateCustomSprite();
    manager.sprites.Add(customSprite);
}
```

### Phase 3: Create Object Instances
You'll need to investigate these classes next:
- `MapEventObjectHD` - for interactive objects
- `MapSpriteHD` - for static sprites
- `MapObjAnimHD` - for animated objects

---

## 🔍 Next Research Steps

### 1. Investigate MapEventObjectHD
```
dnSpy → Search for "MapEventObjectHD"
```
Look for:
- Constructor parameters
- Required fields (position, sprite, ID)
- Interaction methods
- How to create instances

### 2. Investigate MapSpriteHD
```
dnSpy → Search for "MapSpriteHD"
```
Look for:
- How sprites are created
- Texture/sprite assignment
- Position and layer settings

### 3. Find Object Creation Examples
Search for where the game creates these objects:
```
dnSpy → Analyze → Find References to MapEventObjectHD constructor
```

### 4. Test with Unity Explorer
1. Launch game and enter a town
2. Find `MapBGManagerHD` in Unity Explorer
3. Expand `eventObjects` list
4. Examine an existing object's properties
5. Document the structure

---

## 💡 Quick Win: Log Existing Objects

Create a diagnostic patch to see what objects exist:

```csharp
[HarmonyPatch(typeof(MapBGManagerHD), "Load")]
[HarmonyPostfix]
public static IEnumerator Load_Postfix(IEnumerator __result, MapBGManagerHD __instance)
{
    while (__result.MoveNext())
        yield return __result.Current;
    
    // Log all event objects
    Plugin.Log.LogInfo($"=== MapBGManagerHD Objects ===");
    Plugin.Log.LogInfo($"Event Objects: {__instance.eventObjects.Count}");
    
    foreach (var obj in __instance.eventObjects)
    {
        Plugin.Log.LogInfo($"  - ID: {obj.GetInstanceID()}, Name: {obj.name}");
        // Log position, sprite, etc.
    }
    
    Plugin.Log.LogInfo($"Sprites: {__instance.sprites.Count}");
    Plugin.Log.LogInfo($"Anim Objects: {__instance.animObjects.Count}");
}
```

---

## 📋 Summary of Findings

| Feature | Class/Field | Purpose |
|---------|-------------|---------|
| **Interactive Objects** | `eventObjects` (List) | NPCs, doors, chests, interactive items |
| **Static Sprites** | `sprites` (List) | Background tiles, decorations |
| **Animated Objects** | `animObjects` (List) | Save points, flags, animated props |
| **Particles** | `particles` (List) | Visual effects |
| **Scene Loading** | `Load()` method | ⭐ Main hook point for injection |
| **Object Updates** | `RefleshObject()` | Update object state |
| **Find Objects** | `FindEventObject(int id)` | Retrieve objects by ID |
| **Coordinates** | `Vector2` | 2D position system |

---

## ✅ What You Now Know

1. ✅ **MapBGManagerHD** is the main scene manager
2. ✅ Objects are stored in **typed lists** (eventObjects, sprites, etc.)
3. ✅ **Load()** method is the perfect hook point
4. ✅ Coordinates use **Vector2** (X, Y)
5. ✅ **MapEventObjectHD** is the class for interactive objects
6. ✅ You can add objects by appending to the lists

---

## 🚀 Next Actions

1. **Investigate MapEventObjectHD class** in dnSpy
2. **Create a diagnostic patch** to log existing objects
3. **Test object creation** by instantiating a copy of an existing object
4. **Build the injection system** once you understand object structure

This is excellent progress! You've found the core of the object management system. 🎉
