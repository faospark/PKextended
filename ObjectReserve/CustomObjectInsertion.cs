using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Reflection;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using PKCore.Models;
using PKCore.Utils;
using BepInEx;

namespace PKCore.Patches;

/// <summary>
/// Manages insertion of custom objects defined in JSON configuration
/// </summary>
public class CustomObjectInsertion
{
    private static HashSet<int> _processedScenes = new HashSet<int>();
    private static Dictionary<string, List<DiscoveredObject>> _loadedObjects = new Dictionary<string, List<DiscoveredObject>>();
    private static bool _configLoaded = false;
    
    // Path to the JSON file - trying multiple locations
    private static string GetConfigPath()
    {
        // 0. Try GameRoot/PKCore/CustomObjects/objects.json (User Preference)
        string gameRootPath = Path.Combine(Paths.GameRootPath, "PKCore", "CustomObjects", "objects.json");
        if (File.Exists(gameRootPath)) 
        {
             // Plugin.Log.LogInfo($"[Custom Objects] Found config in GameRoot: {gameRootPath}");
             return gameRootPath;
        }

        // 1. Try PKCore/CustomObjects/objects.json (standard BepInEx)
        string path = Path.Combine(Paths.PluginPath, "PKCore", "CustomObjects", "objects.json");
        if (File.Exists(path)) return path;
        
        // 2. Try PKCore/CustomObjects/ExistingMapObjects.json (fallback/dev)
        path = Path.Combine(Paths.PluginPath, "PKCore", "CustomObjects", "ExistingMapObjects.json");
        if (File.Exists(path)) return path;

        return null;
    }

    public static void Initialize(bool enabled, Harmony harmony)
    {
        if (!enabled) return;

        Plugin.Log.LogInfo("[Custom Objects] Initializing object insertion system...");
        
        LoadConfiguration();
        
        Plugin.Log.LogInfo($"[Custom Objects] Objects will be created when scenes are activated");
        
        // Apply RefleshObject patch
        harmony.PatchAll(typeof(RefleshObjectPatch));
    }

    private static void LoadConfiguration()
    {
        try
        {
            string configPath = GetConfigPath();
            if (string.IsNullOrEmpty(configPath))
            {
                Plugin.Log.LogWarning("[Custom Objects] No objects.json found. Creating sample...");
                string dir = Path.Combine(Paths.PluginPath, "PKCore", "CustomObjects");
                Directory.CreateDirectory(dir);
                return;
            }

            Plugin.Log.LogInfo($"[Custom Objects] Loading configuration from: {configPath}");
            string json = File.ReadAllText(configPath);

            try 
            {
                // Try deserializing as the full config structure (Maps -> Id -> Objects)
                var fullConfig = JsonSerializer.Deserialize<CustomObjectsConfig>(json);
                if (fullConfig?.Maps != null)
                {
                    _loadedObjects = new Dictionary<string, List<DiscoveredObject>>();
                    foreach (var kvp in fullConfig.Maps)
                    {
                        _loadedObjects[kvp.Key] = kvp.Value.Objects;
                    }
                }
            }
            catch (JsonException)
            {
                // Fallback: Try deserializing as flat dictionary (historical support: Id -> List<Obj>)
                try 
                {
                    _loadedObjects = JsonSerializer.Deserialize<Dictionary<string, List<DiscoveredObject>>>(json);
                }
                catch
                {
                    // Re-throw original if both fail, or log detailed error
                    throw; 
                }
            }
            
            if (_loadedObjects != null)
            {
                int totalObjects = _loadedObjects.Sum(x => x.Value.Count);
                Plugin.Log.LogInfo($"[Custom Objects] Loaded {totalObjects} objects for {_loadedObjects.Count} maps");
                _configLoaded = true;
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"[Custom Objects] Error loading configuration: {ex.Message}");
        }
    }

    /// <summary>
    /// Try to create custom objects in the given scene GameObject
    /// Called from GameObjectPatch when a scene clone is activated
    /// </summary>
    public static void TryCreateCustomObjects(GameObject sceneRoot)
    {
        try
        {
            // Only create once per scene instance
            if (_processedScenes.Contains(sceneRoot.GetInstanceID()))
            {
                return;
            }
            
            if (!_configLoaded)
            {
                // Try loading again just in case it was created late
                LoadConfiguration();
                if (!_configLoaded) return;
            }

            string mapId = sceneRoot.name.Replace("(Clone)", "");
            
            if (!_loadedObjects.ContainsKey(mapId))
            {
                // No objects for this map
                return;
            }

            Plugin.Log.LogInfo($"[Custom Objects] Attempting to create objects in: {sceneRoot.name} ({mapId})");

            // Find the "object" folder
            Transform objectFolder = FindObjectFolderInScene(sceneRoot.transform);
            if (objectFolder == null)
            {
                Plugin.Log.LogError("[Custom Objects] Could not find 'object' folder in scene");
                return;
            }

            // Create the custom objects
            CreateCustomObjectsForMap(mapId, objectFolder);

            _processedScenes.Add(sceneRoot.GetInstanceID());
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"[Custom Objects] Error creating objects: {ex}");
        }
    }

    public static void ResetForNewScene()
    {
        _processedScenes.Clear();
        // Reload config on new scene allows live editing without restart
        LoadConfiguration();
    }


    private static Transform FindObjectFolderInScene(Transform sceneRoot)
    {
        // Search recursively in the scene root for "object" folder
        // For efficiency, we assume it's relatively shallow
        return FindObjectFolderRecursive(sceneRoot);
    }

    private static Transform FindObjectFolderRecursive(Transform parent)
    {
        if (parent.name == "object")
        {
            return parent;
        }

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

    private static void CreateCustomObjectsForMap(string mapId, Transform objectFolder)
    {
        List<DiscoveredObject> objects = _loadedObjects[mapId];
        Plugin.Log.LogInfo($"[Custom Objects] Creating {objects.Count} custom objects for {mapId}...");

        int successCount = 0;
        foreach (var objData in objects)
        {
            // Skip native objects (id != -1) unless explicitly overridden?
            // For now, allow everything from JSON. User is responsible for not duplicating natives if they don't want to.
            // Usually we filter out ones with NativeID != -1 if we only want purely custom ones, 
            // but the user might want to clone native objects. 
            
            try
            {
                CreateSingleObject(objData, objectFolder);
                successCount++;
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"[Custom Objects] Failed to create object {objData.Name}: {ex.Message}");
            }
        }
        
        Plugin.Log.LogInfo($"[Custom Objects] Successfully created {successCount}/{objects.Count} objects");
    }

    private static void CreateSingleObject(DiscoveredObject data, Transform parent)
    {
        // Create GameObject
        GameObject customObj = new GameObject(data.Name);
        customObj.transform.SetParent(parent);
        
        // Transform
        if (data.Position != null)
            customObj.transform.localPosition = data.Position.ToVector3();
            
        if (data.Scale != null)
            customObj.transform.localScale = data.Scale.ToVector3();
        else
            customObj.transform.localScale = Vector3.one;
            
        customObj.transform.localRotation = Quaternion.Euler(0, 0, data.Rotation);
        
        // Sorting & Layer
        if (data.Layer != 0) customObj.layer = data.Layer;
        if (!string.IsNullOrEmpty(data.Tag) && data.Tag != "Untagged" && !data.Tag.StartsWith("NativeID"))
        {
             try { customObj.tag = data.Tag; } catch {} // Ignore invalid tags
        }

        // SpriteRenderer
        SpriteRenderer sr = null;
        if (data.HasSpriteRenderer || !string.IsNullOrEmpty(data.Texture))
        {
            sr = customObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = data.SortingOrder;

            // Attempt to copy Sorting Layer from a sibling
            // This is critical because the map might render on a specific layer (e.g. "Background")
            // preventing our object from being seen if it's on "Default"
            var siblingSr = parent.GetComponentInChildren<SpriteRenderer>();
            if (siblingSr != null)
            {
                 sr.sortingLayerID = siblingSr.sortingLayerID;
                 
                 // CRITICAL FIX: The Game's shader (copied from sibling) likely hides the sprite
                 // because it expects specific Atlas UVs or properties we don't have.
                 // We force the standard Default shader to ensure the texture is rendered as-is.
                 sr.material = new Material(Shader.Find("Sprites/Default"));

                 Plugin.Log.LogInfo($"[Custom Objects] Copied Sorting Layer from {siblingSr.name}: {sr.sortingLayerName} ({sr.sortingLayerID})");
            }
            
            // DEBUG: Force positive sorting order because -480 puts us behind the background
            // even if the original object used it (likely on a different layer or with different Z).
            sr.sortingOrder = 100;

            
            // Force Z-offset to be in front but not too close to camera
            var pos = customObj.transform.localPosition;
            customObj.transform.localPosition = new Vector3(pos.x, pos.y, -0.5f); // -0.5 instead of -5

            
            // Handle texture
            if (!string.IsNullOrEmpty(data.Texture) && data.Texture.ToLower() != "none" && data.Texture != "Native")
            {
                Sprite sprite = LoadCustomSprite(data.Texture);
                if (sprite != null)
                {
                    sr.sprite = sprite;
                    // Removed debug prefix "SPRITE_" as requested
                    Plugin.Log.LogInfo($"[Custom Objects] Assigned sprite '{sprite.name}' to {customObj.name}");
                }
                else
                {
                    Plugin.Log.LogWarning($"[Custom Objects] FAILED to load sprite for '{data.Texture}'.");
                    // Only use debug sprite if enabled
                    if (Plugin.Config.DebugCustomObjects.Value)
                    {
                        sr.sprite = CreateDebugSprite();
                        sr.sprite.name = "DEBUG_TEXTURE_FAIL"; // Explicit name
                        sr.color = Color.red; // Red = Error (Not Magenta)
                    }
                }
            }
            
            // Fallback for "none" texture
            if (sr.sprite == null && Plugin.Config.DebugCustomObjects.Value)
            {
                sr.sprite = CreateDebugSprite();
                sr.sprite.name = "DEBUG_NO_TEXTURE";
                sr.color = new Color(0, 1, 1, 0.5f); // Cyan
            }
        }
        
        // MapSpriteHD Component
        // This is needed for the game to recognize it as a managed sprite
        if (sr != null)
        {
            AddMapSpriteHD(customObj, sr, data);
        }

        // Active state
        customObj.SetActive(data.Active);
    }
    
    private static void AddMapSpriteHD(GameObject obj, SpriteRenderer sr, DiscoveredObject data)
    {
        try
        {
            var mapSpriteHDType = FindMapSpriteHDType();
            if (mapSpriteHDType != null)
            {
                var il2cppType = Il2CppType.From(mapSpriteHDType);
                var mapSpriteComponent = obj.AddComponent(il2cppType);
                
                SetProperty(mapSpriteComponent, "hasSpriteRenderer", true);
                SetProperty(mapSpriteComponent, "spriteRenderer", sr);
                
                // Set Size (approximating from scale/collider)
                // Vector3 size = data.ColliderSize?.ToVector3() ?? new Vector3(100, 100, 0.2f);
                SetProperty(mapSpriteComponent, "Size", new Vector3(100, 100, 0.2f)); 
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogWarning($"[Custom Objects] Error adding MapSpriteHD: {ex.Message}");
        }
    }

    private static Sprite LoadCustomSprite(string textureName)
    {
        try
        {
            if (textureName.EndsWith(".png")) textureName = textureName.Substring(0, textureName.Length - 4);

            string texturesRoot = Path.Combine(Paths.GameRootPath, "PKCore", "Textures");
            string finalPath = null;
            
            string exactPath = Path.Combine(texturesRoot, textureName + ".png");
            if (File.Exists(exactPath))
            {
                finalPath = exactPath;
            }
            else
            {
                string[] foundFiles = Directory.GetFiles(texturesRoot, textureName + ".png", SearchOption.AllDirectories);
                if (foundFiles.Length > 0) finalPath = foundFiles[0];
            }

            if (!string.IsNullOrEmpty(finalPath))
            {
                Plugin.Log.LogInfo($"[Custom Objects] Logic: Reading bytes from {finalPath}");
                byte[] fileData = File.ReadAllBytes(finalPath);

                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                
                // Use ImageConversion for IL2CPP compatibility
                if (UnityEngine.ImageConversion.LoadImage(texture, fileData))
                {
                    texture.name = textureName;
                    texture.filterMode = FilterMode.Bilinear;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    
                    Sprite sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f
                    );
                    sprite.name = textureName;
                    return sprite;
                }
                else
                {
                    Plugin.Log.LogError($"[Custom Objects] ImageConversion failed for {textureName}");
                }
            }
            else
            {
                Plugin.Log.LogWarning($"[Custom Objects] File not found: {textureName}");
            }
        }
        catch (System.Exception ex) 
        { 
            Plugin.Log.LogError($"[Custom Objects] Exception loading {textureName}: {ex.Message}");
        }

        return null;
    }

    private static Sprite CreateDebugSprite()
    {
        Texture2D texture = new Texture2D(64, 64);
        Color[] pixels = new Color[64 * 64];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 100f);
    }

    private static System.Type FindMapSpriteHDType()
    {
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var assemblyName = assembly.GetName().Name;
            if (assemblyName == "GSD2" || assemblyName == "GSDShare" || assemblyName == "GSD1")
            {
                var type = assembly.GetType("MapSpriteHD");
                if (type != null) return type;
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

[HarmonyPatch(typeof(MapBGManagerHD), "RefleshObject")]
public static class RefleshObjectPatch
{
    [HarmonyPostfix]
    public static void Postfix(int id, Vector2 pos, bool isVisible, int an, int eventMapNo, bool isInitVisible)
    {
        // Diagnostic hook - currently silent
        if (Plugin.Config.EnableObjectDiagnostics.Value)
        {
              // Uncomment for debugging
              // Plugin.Log.LogInfo($"[RefleshObject Hook] ID={id}, Pos={pos}...");
        }
    }
}
