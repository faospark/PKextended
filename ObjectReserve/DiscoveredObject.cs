using System;
using UnityEngine;

namespace PKCore.Models;

public class DiscoveredObject
{
    public string Name { get; set; }
    public string Texture { get; set; }
    public bool HasSpriteRenderer { get; set; }
    public Vector3Config Position { get; set; }
    public Vector3Config Scale { get; set; }
    public float Rotation { get; set; }
    public int SortingOrder { get; set; }
    public int Layer { get; set; }
    public string Tag { get; set; }
    public bool Active { get; set; }
    
    // Collision detection
    public bool HasCollision { get; set; }
    public string ColliderType { get; set; }
    
    // Movement detection
    public bool IsMovable { get; set; }
    public string RigidbodyType { get; set; }
    
    // Interactable detection
    public bool IsInteractable { get; set; }
    public string InteractableType { get; set; }
    
    // Dialog/Remark detection
    public string DialogText { get; set; }
    
    // Additional info
    public int ComponentCount { get; set; }
    
    // Native info
    public int NativeID { get; set; } = -1;
    public int EventMapNo { get; set; } = -1;
}
