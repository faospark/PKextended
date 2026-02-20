using System.Text.Json.Serialization;
using UnityEngine;

namespace PKCore.Models;

/// <summary>
/// Represents a discovered or custom map object.
/// Contains both Unity visual properties and native Suikoden EVENT_OBJ fields.
/// </summary>
public class DiscoveredObject
{
    // ─── Unity Visual Properties ───────────────────────────────────────────────
    public string Name { get; set; }
    public string Texture { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool HasSpriteRenderer { get; set; }
    
    public Vector3Config Position { get; set; }
    public Vector3Config Scale { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public float Rotation { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int SortingOrder { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Active { get; set; }

    // ─── Native EVENT_OBJ Fields ───────────────────────────────────────────────
    // These mirror the game's internal EVENT_OBJ struct from MAPEVDAT.eventobj.
    // Values are in native grid/tile space, NOT Unity world space.

    /// <summary>
    /// Index in the native eventobj array. -1 if not a native object.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int NativeIndex { get; set; } = -1;

    /// <summary>
    /// Object type. 1 = NPC/Human (EVENT_HUMAN), 2-4 = props/chests/triggers.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public byte ObjectType { get; set; }   // otyp

    /// <summary>
    /// Display flag. 1 = visible on map load, 0 = hidden.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public byte Disp { get; set; }         // disp

    /// <summary>
    /// Movement speed.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public byte Speed { get; set; }        // spd

    /// <summary>
    /// Walk/Wait type. Controls idle behavior (stand still, wander, pace, etc.).
    /// Not a collision weight.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public byte WalkType { get; set; }     // wt

    /// <summary>
    /// Starting animation number (e.g. face direction).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public byte AnimationNo { get; set; }  // ano

    /// <summary>
    /// Interaction type. How the player interacts (talk, inspect, etc.).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public byte InteractType { get; set; } // ityp

    /// <summary>
    /// Face/portrait/sprite ID.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public ushort FaceNo { get; set; }     // fpno

    /// <summary>
    /// Render group / depth alignment.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public ushort RenderGroup { get; set; } // ozok

    /// <summary>
    /// Render priority / Z-sort value.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public ushort Priority { get; set; }   // pri

    /// <summary>
    /// Native grid X coordinate.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int NativeX { get; set; }       // x

    /// <summary>
    /// Native grid Y coordinate.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int NativeY { get; set; }       // y

    /// <summary>
    /// Width in tile space. 0 = point object (NPC). >0 = trigger zone width.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int NativeW { get; set; }       // w

    /// <summary>
    /// Height in tile space. 0 = point object (NPC). >0 = trigger zone height.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int NativeH { get; set; }       // h

    // ─── Detection Flags (from Unity component scan) ───────────────────────────
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool HasCollision { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string ColliderType { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsMovable { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string RigidbodyType { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsInteractable { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string InteractableType { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string DialogText { get; set; }

    // ─── Legacy Native Tracking ────────────────────────────────────────────────
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int NativeID { get; set; } = -1;
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int EventMapNo { get; set; } = -1;
}
