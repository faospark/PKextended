# Object Insertion Research Guide
**For Suikoden I & II HD Remaster - PKCore Mod**

This guide will help you research and implement a custom object/character insertion system using **dnSpy** and **Unity Explorer**.

---

## Table of Contents
1. [Understanding bgManagerHD](#1-understanding-bgmanagerhd)
2. [Discovering Object Types](#2-discovering-object-types)
3. [Finding Texture Locations](#3-finding-texture-locations)
4. [Researching Interactivity Systems](#4-researching-interactivity-systems)
5. [Understanding Coordinate Systems](#5-understanding-coordinate-systems)
6. [Character Field Insertion](#6-character-field-insertion)
7. [Implementation Strategy](#7-implementation-strategy)

---

## 1. Understanding bgManagerHD

### What is bgManagerHD?
Based on your existing code (`GameObjectPatch.cs`), `bgManagerHD` is the primary scene manager that handles background and field objects in Suikoden HD Remaster.

### dnSpy Investigation Steps

#### Step 1: Locate the bgManagerHD Class
1. Open **dnSpy**
2. Load `Assembly-CSharp.dll` from the game directory
3. Search for `bgManagerHD` (Ctrl+Shift+K)
4. Look for these key areas:
   - **Fields**: What data does it store?
   - **Methods**: How does it spawn/manage objects?
   - **Awake/Start**: What initializes when a scene loads?

#### Step 2: Key Methods to Investigate
Look for methods like:
- `SpawnObject()` / `CreateObject()` / `InstantiateObject()`
- `LoadScene()` / `SetupScene()`
- `AddFieldObject()` / `RegisterObject()`
- Any method with "Prefab", "Spawn", "Create", or "Instantiate" in the name

#### Step 3: Unity Explorer Runtime Investigation
1. Launch the game with Unity Explorer
2. Navigate to a field/town scene
3. Search for `bgManagerHD` in the **Object Explorer**
4. Expand the GameObject hierarchy to see:
   - Child objects (what's being managed)
   - Components attached
   - Active fields and their values

### What to Document
Create a text file with:
```
bgManagerHD Research Notes
==========================

Class Location: Assembly-CSharp.dll -> [namespace].[class]

Key Fields:
- [field name] : [type] - [description/purpose]

Key Methods:
- [method name]([parameters]) - [what it does]

Object Hierarchy in Unity Explorer:
- bgManagerHD
  ├─ [child object 1]
  ├─ [child object 2]
  └─ ...
```

---

## 2. Discovering Object Types

### Using Unity Explorer (Runtime)

#### Step 1: Identify Existing Objects
1. In Unity Explorer, navigate to the scene hierarchy
2. Look for common object patterns:
   - NPCs (usually have `Character`, `NPC`, or specific names)
   - Props (barrels, crates, furniture)
   - Interactive objects (doors, chests, save points)
   - Decorative objects (plants, signs, etc.)

#### Step 2: Inspect Object Components
For each object type, document:
- **GameObject name** (e.g., `obj_barrel_01`)
- **Components** attached:
  - `SpriteRenderer` (for 2D sprites)
  - `BoxCollider2D` / `CircleCollider2D` (for collision)
  - Custom scripts (e.g., `InteractableObject`, `NPCController`)
  - `Animator` (for animations)

#### Step 3: Find Prefab References
In dnSpy, search for:
- `Resources.Load<GameObject>("path/to/prefab")`
- `Addressables.LoadAssetAsync<GameObject>("prefab_name")`
- Any field of type `GameObject` with names like `prefab`, `template`, `prototype`

### Object Type Categories to Research

| Category | Examples | Key Components |
|----------|----------|----------------|
| **NPCs** | Townspeople, merchants | `NPCController`, `DialogueSystem`, `Animator` |
| **Props** | Barrels, crates, furniture | `SpriteRenderer`, `Collider2D` |
| **Interactive** | Doors, chests, switches | `InteractableObject`, `EventTrigger` |
| **Decorative** | Plants, signs, effects | `SpriteRenderer`, `ParticleSystem` |
| **Field Characters** | Party members in field | `PlayerController`, `CharacterMovement` |

### Documentation Template
```
Object Type: [name]
===================

GameObject Path: [hierarchy path]
Prefab Name: [if applicable]

Components:
- [Component 1]: [purpose]
- [Component 2]: [purpose]

Sprite/Texture:
- Atlas: [atlas name]
- Sprite: [sprite name]
- Path: [texture path if standalone]

Interactivity:
- Can be interacted with: [yes/no]
- Trigger type: [click/proximity/auto]
- Script handling interaction: [script name]
```

---

## 3. Finding Texture Locations

### Method 1: SpriteRenderer Investigation (Unity Explorer)

1. Select an object in Unity Explorer
2. Find the `SpriteRenderer` component
3. Expand the `sprite` field:
   - **sprite.name**: The sprite name (e.g., `hp_obj_barrel_01`)
   - **sprite.texture**: The atlas/texture it comes from
   - **sprite.rect**: The UV coordinates within the atlas

### Method 2: Texture Atlas Research (dnSpy)

Based on your existing code (`SpriteAtlasPatch.cs`), the game uses Unity Sprite Atlases.

#### Find Atlas Loading Code:
```csharp
// Search for in dnSpy:
SpriteAtlas.GetSprite(string name)
Addressables.LoadAssetAsync<SpriteAtlas>(...)
Resources.Load<SpriteAtlas>(...)
```

#### Common Atlas Names (from your code):
- `hp_rbat` - Battle sprites
- `hp_furo` - Bath backgrounds
- `hp_telepo` - Teleport/save point sprites
- Look for `hp_field`, `hp_town`, `hp_obj` patterns

### Method 3: Addressables System

The game likely uses Unity's Addressables system:

1. In dnSpy, search for `Addressables.LoadAssetAsync`
2. Find calls that load textures/sprites
3. Note the **address/key** used (this is the texture identifier)

### Texture Location Documentation
```
Texture Research
================

Object: [object name]
Texture Type: [Atlas / Standalone]

If Atlas:
- Atlas Name: [e.g., hp_field_objects]
- Sprite Name: [e.g., obj_barrel_01]
- Loading Method: [Addressables / Resources / Direct]

If Standalone:
- Texture Name: [e.g., Eff_tex_Summon_10.png]
- Loading Path: [path in Addressables or Resources]

Custom Replacement:
- Place file at: BepInEx/plugins/PKCore/Textures/[sprite_name].png
```

---

## 4. Researching Interactivity Systems

### Finding Interaction Scripts

#### Step 1: Identify Interaction Components
In Unity Explorer, examine interactive objects (doors, chests, NPCs):
- Look for components with "Interact", "Event", "Trigger", "Action" in the name
- Common patterns: `InteractableObject`, `EventTrigger`, `NPCInteraction`

#### Step 2: dnSpy Deep Dive
Once you find the component name:
1. Search for the class in dnSpy
2. Look for methods like:
   - `OnInteract()` / `Interact()`
   - `OnTriggerEnter2D()` / `OnCollisionEnter2D()`
   - `ShowMessage()` / `DisplayText()`

#### Step 3: Message/Remark System
To trigger on-screen remarks, find:
- **UI Message Classes**: Search for `UIMessage`, `MessageWindow`, `DialogueBox`
- **Text Display Methods**: `ShowText()`, `DisplayMessage()`, `SetText()`

Example from your `NPCPortraitPatch.cs`:
```csharp
// The game uses UIMessageWindow for dialogue
UIMessageWindow.SetMessage(string text)
```

### Interactivity Research Template
```
Interactivity System
====================

Component: [e.g., InteractableObject]
Trigger Type: [Click / Proximity / Collision]

Key Methods:
- [method name]: [what it does]

Message Display:
- Class: [e.g., UIMessageWindow]
- Method: [e.g., ShowMessage(string text)]

Event System:
- Uses Unity Events: [yes/no]
- Custom event system: [description]

How to Hook:
- Harmony Patch Target: [class.method]
- Alternative: [add component at runtime]
```

---

## 5. Understanding Coordinate Systems

### Unity Coordinate Basics
- **X-axis**: Horizontal (left/right)
- **Y-axis**: Vertical (up/down) in 2D games
- **Z-axis**: Depth (usually for layering in 2D)

### Finding the Coordinate System

#### Method 1: Unity Explorer
1. Select various objects in a scene
2. Note their `Transform.position` values
3. Move around and observe how coordinates change
4. Document the pattern:
   ```
   Town Center: (0, 0, 0)
   North exit: (0, 10, 0)
   East building: (15, 0, 0)
   ```

#### Method 2: Player Position Tracking
In dnSpy, find:
- Player controller class (search for `Player`, `Hero`, `Character`)
- Look for `transform.position` or `GetPosition()` methods
- Create a patch to log player position in real-time

### Coordinate Documentation
```
Coordinate System
=================

Scene: [scene name]

Reference Points:
- Origin (0,0,0): [location description]
- X+ direction: [East/West/etc]
- Y+ direction: [North/South/Up/Down]
- Z values: [layering system, e.g., -1=background, 0=characters, 1=foreground]

Example Coordinates:
- [Location 1]: (x, y, z)
- [Location 2]: (x, y, z)

Grid Size: [if applicable, e.g., 1 unit = 1 tile]
```

---

## 6. Character Field Insertion

### Research Steps

#### Step 1: Find Character/NPC Classes
In dnSpy, search for:
- `Character` / `CharacterController` / `FieldCharacter`
- `NPC` / `NPCController` / `NPCManager`
- `PartyMember` / `PlayerCharacter`

#### Step 2: Identify Character Components
Essential components for field characters:
- **Movement**: `CharacterMovement`, `PathFollower`, `NavMeshAgent`
- **Animation**: `Animator` with animation controller
- **Rendering**: `SpriteRenderer` with character sprites
- **Collision**: `CapsuleCollider2D` or `BoxCollider2D`
- **AI/Behavior**: `NPCBehavior`, `AIController`

#### Step 3: Character Spawning System
Look for:
```csharp
// In dnSpy, search for methods like:
SpawnNPC(string npcId, Vector3 position)
CreateCharacter(CharacterData data)
InstantiateFieldCharacter(GameObject prefab, Transform parent)
```

#### Step 4: Character Data Structure
Find the data structure that defines characters:
- Character ID / Name
- Sprite/appearance data
- Stats (if applicable)
- Dialogue/behavior scripts

### Character Insertion Research Template
```
Character System
================

Character Class: [e.g., FieldCharacter]
NPC Manager: [e.g., NPCManager]

Spawning Method:
- Method: [class.method]
- Parameters: [list parameters]

Required Components:
- [Component 1]: [purpose]
- [Component 2]: [purpose]

Character Data:
- Data Class: [e.g., CharacterData]
- Key Fields: [id, name, sprite, etc.]

Animation System:
- Animator Controller: [path/name]
- Animation States: [idle, walk, talk, etc.]

Prefab Location:
- Addressables Key: [if using Addressables]
- Resources Path: [if using Resources]
```

---

## 7. Implementation Strategy

### Phase 1: Basic Object Spawning
1. Create `ObjectInsertionPatch.cs`
2. Hook into `bgManagerHD` initialization
3. Implement basic GameObject spawning at coordinates
4. Test with simple sprite objects

### Phase 2: Texture System Integration
1. Integrate with existing `CustomTexturePatch.cs`
2. Support custom textures for inserted objects
3. Handle atlas sprites vs standalone textures

### Phase 3: Interactivity
1. Add component attachment system
2. Implement message trigger system
3. Hook into existing UI message system

### Phase 4: Character Insertion
1. Research character prefab system
2. Implement character spawning
3. Handle animations and movement
4. Add AI/behavior options

### Configuration Structure (Proposed)
```ini
[Object Insertion]
EnableObjectInsertion = true
ConfigFilePath = BepInEx/plugins/PKCore/Objects/objects.json

[Character Insertion]
EnableCharacterInsertion = true
ConfigFilePath = BepInEx/plugins/PKCore/Characters/characters.json
```

### JSON Configuration Example
```json
{
  "objects": [
    {
      "id": "custom_barrel_01",
      "type": "prop",
      "scene": "town_gregminster",
      "position": { "x": 10.5, "y": 5.0, "z": 0 },
      "sprite": "obj_barrel_01",
      "texture": "custom_barrel.png",
      "interactable": true,
      "message": "It's a mysterious barrel.",
      "collision": true
    }
  ],
  "characters": [
    {
      "id": "custom_npc_01",
      "name": "Mysterious Stranger",
      "scene": "town_gregminster",
      "position": { "x": 15.0, "y": 8.0, "z": 0 },
      "sprite": "npc_generic_01",
      "texture": "custom_npc.png",
      "dialogue": "Hello, traveler!",
      "movement": "stationary"
    }
  ]
}
```

---

## Next Steps

### Immediate Actions:
1. ✅ Read this guide thoroughly
2. ⬜ Open dnSpy and locate `bgManagerHD` class
3. ⬜ Document bgManagerHD fields and methods
4. ⬜ Use Unity Explorer to examine object hierarchy in a town scene
5. ⬜ Create a research notes file with findings

### Research Checklist:
- ⬜ bgManagerHD class structure documented
- ⬜ At least 3 object types identified and documented
- ⬜ Texture loading system understood
- ⬜ Interaction/message system identified
- ⬜ Coordinate system mapped for one scene
- ⬜ Character spawning method found

### Questions to Answer:
1. How does bgManagerHD spawn objects when a scene loads?
2. What's the prefab system used (Addressables/Resources/other)?
3. How are textures assigned to objects?
4. What component handles player interaction with objects?
5. How does the message/dialogue system work?
6. What's the coordinate origin for each scene?
7. How are field characters different from NPCs?

---

## Tips for Success

### dnSpy Tips:
- Use **Ctrl+Shift+K** for class search
- Use **Ctrl+K** for member search within a class
- Right-click → "Analyze" to find all references to a method/field
- Use bookmarks (Ctrl+B) to mark important classes

### Unity Explorer Tips:
- Use the search function to quickly find GameObjects
- Inspector shows real-time values - very useful for coordinates
- You can modify values at runtime to test theories
- Take screenshots of hierarchies for reference

### Documentation Tips:
- Keep a separate text file for each research area
- Use consistent naming conventions
- Include screenshots from Unity Explorer
- Note the game version and mod version in your notes

---

## Support Resources

### Your Existing Code References:
- `GameObjectPatch.cs` - Shows bgManagerHD hooking
- `CustomTexturePatch.cs` - Texture loading system
- `NPCPortraitPatch.cs` - UI message system integration
- `SavePointPatch.cs` - Object component manipulation example

### Useful Unity Documentation:
- GameObject and Transform
- SpriteRenderer and Sprite
- Addressables System
- 2D Colliders and Triggers

---

**Good luck with your research! Update this document as you discover new information.**
