# Custom Object Configuration Guide

PKCore allows you to insert custom static objects into game scenes using a simple JSON configuration file.

## Configuration File

The configuration file is located at:
`[Game Folder]/BepInEx/plugins/PKCore/CustomObjects/objects.json`

If this file does not exist, run the game once with `EnableCustomObjects = true` in `faospark.pkcore.cfg` to generate a default one.

## Configuration Options

### In `faospark.pkcore.cfg`:

- **EnableCustomObjects** (default: `true`) - Enable/disable custom object insertion system
- **DebugCustomObjects** (default: `true`) - Show magenta debug sprites when textures are missing
- **LogExistingMapObjects** (default: `false`) - Log existing map objects to `ExistingMapObjects.json` for reference

## JSON Structure

The configuration is grouped by **Map ID**. Inside each map, you define a list of **Objects**.

```json
{
  "Maps": {
    "vk07_01": {
      "Objects": [
        {
          "Name": "mercenary_fort_sign",
          "Texture": "custom_sign_01",
          "Position": { "X": 100, "Y": 50, "Z": 0 },
          "Scale": { "X": 1.5, "Y": 1.5, "Z": 1 },
          "Rotation": 0,
          "SortingOrder": 31
        }
      ]
    }
  }
}
```

## Map IDs

To find the Map ID:
1.  Check the logs (if diagnostics are enabled) when entering a scene.
2.  It is usually the scene name without `(Clone)`.
    *   Example: `vk07_01` (Mercenary Fortress)
    *   Example: `vk08_00` (Inn Room)
    *   Example: `kv01_02` (Gregminster)

## Object Properties

| Property | Type | Description | Default |
| :--- | :--- | :--- | :--- |
| **Name** | string | Unique name for the object (for debugging). | "custom_object" |
| **Texture** | string | Filename of the texture in `PKCore/Textures/` (without .png). | null |
| **Position** | Vector3 | `{ X, Y, Z }` coordinates. Relative to map's object folder. | `{0, 0, 0}` |
| **Scale** | Vector3 | `{ X, Y, Z }` size multiplier. | `{1, 1, 1}` |
| **Rotation** | float | Rotation around Z-axis (in degrees). | 0 |
| **SortingOrder** | int | Rendering layer priority. Higher = in front. | 10 |

### Collision & Sound (Planned)
*These properties exist in the config but may not be fully implemented yet.*

| Property | Type | Description |
| :--- | :--- | :--- |
| **HasCollision** | bool | If true, blocks player movement. |
| **ColliderSize** | Vector2 | `{ X, Y }` size of collision box. |
| **ColliderOffset** | Vector2 | `{ X, Y }` offset of collision box. |
| **WalkSound** | string | Sound file to play when walking over. |

## Debug Features

### Magenta Debug Sprites
When `DebugCustomObjects = true`, objects with missing textures will show as bright magenta squares. This helps you:
- Verify object placement
- Test sorting order
- Confirm objects are being created

Set to `false` to hide missing texture objects.

### Discovering Existing Objects
Enable `LogExistingMapObjects = true` to automatically log all existing map objects to:
`PKCore/CustomObjects/ExistingMapObjects.json`

This file shows you:
- Object names
- Texture names
- Exact positions, scales, rotations
- Sorting orders
- Layers and tags

**Use this to copy existing object configurations!**

After collecting the data you need, set `LogExistingMapObjects = false` to disable logging.

## Sample Configuration

See `sample_objects.json` in the CustomObjects folder for examples including:
- Multiple objects per map
- Different scales and rotations
- Collision settings
- Various sorting orders
