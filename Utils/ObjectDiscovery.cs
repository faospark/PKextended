using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine;
using PKCore.Models;
using BepInEx;

namespace PKCore.Utils;

/// <summary>
/// Discovers and logs existing map objects to help users copy object configurations
/// </summary>
public static class ObjectDiscovery
{
    private static HashSet<string> _discoveredMaps = new HashSet<string>();
    private static Dictionary<string, List<DiscoveredObject>> _discoveredObjects = new Dictionary<string, List<DiscoveredObject>>();
    
    public static void DiscoverObjectsInScene(GameObject sceneRoot)
    {
        try
        {
            string mapId = sceneRoot.name.Replace("(Clone)", "");
            
            // Skip if already discovered
            if (_discoveredMaps.Contains(mapId))
                return;
                
            Plugin.Log.LogInfo($"[ObjectDiscovery] Discovering objects in map: {mapId}");
            
            // Find the "object" folder
            Transform objectFolder = FindObjectFolder(sceneRoot.transform);
            if (objectFolder == null)
            {
                Plugin.Log.LogWarning($"[ObjectDiscovery] No 'object' folder found in {mapId}");
                return;
            }
            
            List<DiscoveredObject> objects = new List<DiscoveredObject>();
            
            // Discover all children in the object folder (use GetChild for IL2CPP compatibility)
            for (int i = 0; i < objectFolder.childCount; i++)
            {
                Transform child = objectFolder.GetChild(i);
                var discovered = DiscoverObject(child.gameObject);
                if (discovered != null)
                {
                    objects.Add(discovered);
                }
            }
            
            _discoveredObjects[mapId] = objects;
            _discoveredMaps.Add(mapId);
            
            Plugin.Log.LogInfo($"[ObjectDiscovery] Discovered {objects.Count} objects in {mapId}");
            
            // Save to file
            SaveDiscoveredObjects();
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[ObjectDiscovery] Error discovering objects: {ex}");
        }
    }
    
    private static DiscoveredObject DiscoverObject(GameObject obj)
    {
        try
        {
            // Get SpriteRenderer if it exists (but don't require it)
            var spriteRenderer = obj.GetComponent<SpriteRenderer>();
            bool hasSpriteRenderer = spriteRenderer != null;
            string textureName = spriteRenderer?.sprite?.texture?.name ?? "none";
            int sortingOrder = spriteRenderer?.sortingOrder ?? 0;
            
            // Check for collision, movement, and interactable components by name
            bool hasCollision = false;
            string colliderType = null;
            bool isMovable = false;
            string rigidbodyType = null;
            bool isInteractable = false;
            string interactableType = null;
            string dialogText = null;
            
            // Get all components and check their types
            var components = obj.GetComponents<Component>();
            foreach (var component in components)
            {
                if (component == null) continue;
                
                string typeName = component.GetType().Name;
                
                // Check for collision components
                if (typeName.Contains("Collider"))
                {
                    hasCollision = true;
                    if (colliderType == null)
                        colliderType = typeName;
                }
                
                // Check for Rigidbody (movable)
                if (typeName.Contains("Rigidbody"))
                {
                    isMovable = true;
                    rigidbodyType = typeName;
                }
                
                // Check for interactable patterns
                if (typeName.Contains("Interact", StringComparison.OrdinalIgnoreCase) ||
                    typeName.Contains("Event", StringComparison.OrdinalIgnoreCase) ||
                    typeName.Contains("Action", StringComparison.OrdinalIgnoreCase) ||
                    typeName.Contains("Trigger", StringComparison.OrdinalIgnoreCase))
                {
                    isInteractable = true;
                    if (interactableType == null)
                        interactableType = typeName;
                }
                
                // Try to find dialog/text content
                if (dialogText == null)
                {
                    try
                    {
                        var type = component.GetType();
                        
                        // Look for common text/dialog field names
                        var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        foreach (var field in fields)
                        {
                            string fieldName = field.Name.ToLower();
                            if (fieldName.Contains("text") || fieldName.Contains("dialog") || 
                                fieldName.Contains("message") || fieldName.Contains("remark"))
                            {
                                var value = field.GetValue(component);
                                if (value != null && value is string textValue && !string.IsNullOrEmpty(textValue))
                                {
                                    dialogText = textValue;
                                    break;
                                }
                            }
                        }
                        
                        // Also check properties
                        if (dialogText == null)
                        {
                            var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                            foreach (var prop in properties)
                            {
                                string propName = prop.Name.ToLower();
                                if (propName.Contains("text") || propName.Contains("dialog") || 
                                    propName.Contains("message") || propName.Contains("remark"))
                                {
                                    if (prop.CanRead)
                                    {
                                        var value = prop.GetValue(component);
                                        if (value != null && value is string textValue && !string.IsNullOrEmpty(textValue))
                                        {
                                            dialogText = textValue;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Ignore reflection errors
                    }
                }
            }
                
            return new DiscoveredObject
            {
                Name = obj.name,
                Texture = textureName,
                HasSpriteRenderer = hasSpriteRenderer,
                Position = new Vector3Config 
                { 
                    X = obj.transform.localPosition.x,
                    Y = obj.transform.localPosition.y,
                    Z = obj.transform.localPosition.z
                },
                Scale = new Vector3Config
                {
                    X = obj.transform.localScale.x,
                    Y = obj.transform.localScale.y,
                    Z = obj.transform.localScale.z
                },
                Rotation = obj.transform.localEulerAngles.z,
                SortingOrder = sortingOrder,
                Layer = obj.layer,
                Tag = obj.tag,
                Active = obj.activeSelf,
                HasCollision = hasCollision,
                ColliderType = colliderType,
                IsMovable = isMovable,
                RigidbodyType = rigidbodyType,
                IsInteractable = isInteractable,
                InteractableType = interactableType,
                DialogText = dialogText,
                ComponentCount = components.Length
            };
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[ObjectDiscovery] Error discovering object {obj.name}: {ex.Message}");
            return null;
        }
    }
    
    private static Transform FindObjectFolder(Transform parent)
    {
        if (parent.name == "object")
            return parent;
            
        for (int i = 0; i < parent.childCount; i++)
        {
            var result = FindObjectFolder(parent.GetChild(i));
            if (result != null)
                return result;
        }
        
        return null;
    }
    
    private static void SaveDiscoveredObjects()
    {
        try
        {
            // Save to game root PKCore folder (not BepInEx plugins folder)
            string gameRoot = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Paths.PluginPath)));
            string outputPath = Path.Combine(gameRoot, "PKCore", "CustomObjects", "ExistingMapObjects.json");
            
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            
            string json = JsonSerializer.Serialize(_discoveredObjects, options);
            File.WriteAllText(outputPath, json);
            
            Plugin.Log.LogInfo($"[ObjectDiscovery] Saved discovered objects to {outputPath}");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"[ObjectDiscovery] Error saving discovered objects: {ex}");
        }
    }
}

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
}
