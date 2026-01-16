using System.Collections.Generic;
using UnityEngine;

namespace PKCore.Models;

public class CustomObjectsConfig
{
    // Map ID -> Configuration
    public Dictionary<string, MapObjectsConfig> Maps { get; set; } = new Dictionary<string, MapObjectsConfig>();
}

public class MapObjectsConfig
{
    public List<DiscoveredObject> Objects { get; set; } = new List<DiscoveredObject>();
}

public class ObjectDefinition
{
    public string Name { get; set; }
    public string Texture { get; set; }
    public Vector3Config Position { get; set; }
    public Vector3Config Scale { get; set; }
    public float Rotation { get; set; } = 0f;
    public int SortingOrder { get; set; } = 10;

    // Collision & Movement
    public bool HasCollision { get; set; } = false;
    public Vector2Config ColliderSize { get; set; }
    public Vector2Config ColliderOffset { get; set; }
    public bool IsWalkable { get; set; } = true;

    // Sound Effects
    public string WalkSound { get; set; }
    public string InteractSound { get; set; }
}

public class Vector3Config
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public Vector3 ToVector3() => new Vector3(X, Y, Z);
}

public class Vector2Config
{
    public float X { get; set; }
    public float Y { get; set; }

    public Vector2 ToVector2() => new Vector2(X, Y);
}
