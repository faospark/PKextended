using HarmonyLib;
using UnityEngine;
using System.Collections;
using System.Reflection;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;

namespace PKCore.Patches;

/// <summary>
/// Proof-of-concept: Inserts one custom static object into the scene
/// </summary>
public class CustomObjectInsertion
{
    private static bool _objectCreated = false;

    public static void Initialize(bool enabled, Harmony harmony)
    {
        if (!enabled) return;

        Plugin.Log.LogInfo("[Custom Objects] Initializing object insertion system...");
        Plugin.Log.LogInfo("[Custom Objects] Objects will be created when scenes are activated");
    }

    /// <summary>
    /// Try to create custom objects in the given scene GameObject
    /// Called from GameObjectPatch when a scene clone is activated
    /// </summary>
    public static void TryCreateCustomObjects(GameObject sceneRoot)
    {
        try
        {
            // Only create once per scene
            if (_objectCreated)
            {
                return;
            }

            Plugin.Log.LogInfo($"[Custom Objects] Attempting to create objects in: {sceneRoot.name}");

            // Find the "object" folder
            Transform objectFolder = FindObjectFolderInScene(sceneRoot.transform);
            if (objectFolder == null)
            {
                Plugin.Log.LogError("[Custom Objects] Could not find 'object' folder in scene");
                return;
            }

            Plugin.Log.LogInfo($"[Custom Objects] Found object folder: {objectFolder.name}");

            // Create the custom object
            CreateCustomObject(objectFolder);

            _objectCreated = true;
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"[Custom Objects] Error creating objects: {ex}");
        }
    }

    public static void ResetForNewScene()
    {
        _objectCreated = false;
    }


    private static Transform FindObjectFolderInScene(Transform sceneRoot)
    {
        // Search recursively in the scene root for "object" folder
        return FindObjectFolderRecursive(sceneRoot);
    }

    private static Transform FindObjectFolderRecursive(Transform parent)
    {
        // Check if this transform is named "object"
        if (parent.name == "object")
        {
            Plugin.Log.LogInfo($"[Custom Objects] Found 'object' folder at path: {GetTransformPath(parent)}");
            return parent;
        }

        // Search children recursively
        for (int i = 0; i < parent.childCount; i++)
        {
            var result = FindObjectFolderRecursive(parent.GetChild(i));
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static string GetTransformPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }

    private static void CreateCustomObject(Transform objectFolder)
    {
        Plugin.Log.LogInfo("[Custom Objects] Creating custom test object...");

        // Create GameObject
        GameObject customObj = new GameObject("custom_test_object");
        customObj.transform.SetParent(objectFolder);
        customObj.transform.localPosition = new Vector3(960, 540, 0); // Center of 1920x1080 screen
        customObj.transform.localRotation = Quaternion.identity;
        customObj.transform.localScale = new Vector3(20, 20, 1); // HUGE - 20x normal (2000x2000 pixels)

        Plugin.Log.LogInfo($"[Custom Objects] Created GameObject at position: {customObj.transform.position} with scale: {customObj.transform.localScale}");

        // Add SpriteRenderer
        SpriteRenderer spriteRenderer = customObj.AddComponent<SpriteRenderer>();
        spriteRenderer.color = Color.white;
        
        // Set sorting to render on top of everything
        spriteRenderer.sortingOrder = 9999;
        Plugin.Log.LogInfo("[Custom Objects] Set sorting order to 9999 (render on top of EVERYTHING)");

        // Try to load custom texture
        Sprite customSprite = LoadCustomSprite("custom_object_test");
        if (customSprite != null)
        {
            spriteRenderer.sprite = customSprite;
            Plugin.Log.LogInfo("[Custom Objects] ✓ Assigned custom sprite");
        }
        else
        {
            // Only create magenta debug sprite if debug mode is enabled
            if (Plugin.Config.DebugCustomObjects.Value)
            {
                // Fallback: create a magenta debug sprite
                Texture2D fallbackTexture = new Texture2D(100, 100);
                Color[] pixels = new Color[100 * 100];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = Color.magenta;
                }
                fallbackTexture.SetPixels(pixels);
                fallbackTexture.Apply();

                Sprite fallbackSprite = Sprite.Create(
                    fallbackTexture,
                    new Rect(0, 0, 100, 100),
                    new Vector2(0.5f, 0.5f),
                    1f
                );

                spriteRenderer.sprite = fallbackSprite;
                Plugin.Log.LogWarning("[Custom Objects] Using magenta debug sprite (custom_object_test.png not found)");
            }
            else
            {
                Plugin.Log.LogWarning("[Custom Objects] Texture not found and debug mode disabled, object will be invisible");
            }
        }

        // Add MapSpriteHD component
        try
        {
            var mapSpriteHDType = FindMapSpriteHDType();
            if (mapSpriteHDType != null)
            {
                // Convert System.Type to Il2CppSystem.Type
                var il2cppType = Il2CppType.From(mapSpriteHDType);
                var mapSpriteComponent = customObj.AddComponent(il2cppType);
                
                // Set properties using reflection
                SetProperty(mapSpriteComponent, "hasSpriteRenderer", true);
                SetProperty(mapSpriteComponent, "spriteRenderer", spriteRenderer);
                SetProperty(mapSpriteComponent, "Size", new Vector3(100, 100, 0.2f));
                
                Plugin.Log.LogInfo("[Custom Objects] ✓ Added MapSpriteHD component");
            }
            else
            {
                Plugin.Log.LogWarning("[Custom Objects] Could not find MapSpriteHD type, object may not render correctly");
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"[Custom Objects] Error adding MapSpriteHD: {ex.Message}");
        }

        // Explicitly activate the object
        customObj.SetActive(true);
        
        // Check parent hierarchy and log it
        Plugin.Log.LogInfo("[Custom Objects] Checking parent hierarchy:");
        Transform current = customObj.transform;
        int depth = 0;
        while (current != null && depth < 10)
        {
            Plugin.Log.LogInfo($"[Custom Objects]   [{depth}] {current.name} - Active: {current.gameObject.activeSelf}");
            current = current.parent;
            depth++;
        }
        
        // Log final state for debugging
        Plugin.Log.LogInfo($"[Custom Objects] Final state - Active: {customObj.activeSelf}, ActiveInHierarchy: {customObj.activeInHierarchy}");
        Plugin.Log.LogInfo($"[Custom Objects] Parent: {customObj.transform.parent.name}, Parent Active: {customObj.transform.parent.gameObject.activeSelf}");
        Plugin.Log.LogInfo($"[Custom Objects] SpriteRenderer enabled: {spriteRenderer.enabled}, Sprite: {spriteRenderer.sprite?.name ?? "null"}");
        Plugin.Log.LogInfo($"[Custom Objects] Layer: {customObj.layer}, Tag: {customObj.tag}");
        
        Plugin.Log.LogInfo("[Custom Objects] ✓ Custom object created successfully!");
    }

    private static Sprite LoadCustomSprite(string textureName)
    {
        try
        {
            // Try to use the existing texture loading system
            var textureManagerType = typeof(CustomTexturePatch);
            var loadTextureMethod = textureManagerType.GetMethod("LoadTexture", 
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

            if (loadTextureMethod != null)
            {
                var texture = loadTextureMethod.Invoke(null, new object[] { textureName }) as Texture2D;
                if (texture != null)
                {
                    // Create sprite from texture
                    Sprite sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        1f
                    );
                    return sprite;
                }
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogWarning($"[Custom Objects] Could not load texture '{textureName}': {ex.Message}");
        }

        return null;
    }

    private static System.Type FindMapSpriteHDType()
    {
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var assemblyName = assembly.GetName().Name;
            if (assemblyName == "GSD2" || assemblyName == "GSDShare" || assemblyName == "GSD1")
            {
                var type = assembly.GetType("MapSpriteHD");
                if (type != null)
                {
                    return type;
                }
            }
        }
        return null;
    }

    private static void SetProperty(object obj, string propertyName, object value)
    {
        var property = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property != null && property.CanWrite)
        {
            property.SetValue(obj, value);
        }
    }
}
