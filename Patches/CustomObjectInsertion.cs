using HarmonyLib;
using UnityEngine;
using System;
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
    
    // Cache for the current MapBGManagerHD instance
    private static object _currentMapBGManager = null;
    
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
        
        // Apply patches
        harmony.PatchAll(typeof(RefleshObjectPatch));
        harmony.PatchAll(typeof(MapBGManagerHDCachePatch));
    }
    
    // Called by MapBGManagerHDCachePatch to cache the instance
    internal static void SetMapBGManagerInstance(object instance)
    {
        _currentMapBGManager = instance;
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

            // Use cached MapBGManagerHD instance (captured by MapBGManagerHDCachePatch)
            object mapBGManager = _currentMapBGManager;
            if (mapBGManager == null)
            {
                Plugin.Log.LogWarning("[Custom Objects] MapBGManagerHD instance not yet cached - objects will not be registered");
            }
            else
            {
                Plugin.Log.LogInfo($"[Custom Objects] Using cached MapBGManagerHD instance");
            }

            // Find the "object" folder
            Transform objectFolder = FindObjectFolderInScene(sceneRoot.transform);
            if (objectFolder == null)
            {
                Plugin.Log.LogError("[Custom Objects] Could not find 'object' folder in scene");
                return;
            }

            // Create the custom objects
            CreateCustomObjectsForMap(mapId, objectFolder, mapBGManager);

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

    private static void CreateCustomObjectsForMap(string mapId, Transform objectFolder, object mapBGManager)
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
                var (obj, sprite, position, scale, rotation) = CreateSingleObject(objData, objectFolder);
                if (obj != null)
                {
                    successCount++;
                    
                    // Diagnostic logging for visibility debugging
                    if (Plugin.Config.DebugCustomObjects.Value)
                    {
                        Plugin.Log.LogInfo($"[Custom Objects] Object '{obj.name}' state:");
                        Plugin.Log.LogInfo($"  - Active: {obj.activeSelf}");
                        Plugin.Log.LogInfo($"  - Position: {obj.transform.position}");
                        Plugin.Log.LogInfo($"  - Local Position: {obj.transform.localPosition}");
                        Plugin.Log.LogInfo($"  - Parent: {obj.transform.parent?.name ?? "null"}");
                        
                        var sr = obj.GetComponent<SpriteRenderer>();
                        if (sr != null)
                        {
                            Plugin.Log.LogInfo($"  - SpriteRenderer enabled: {sr.enabled}");
                            Plugin.Log.LogInfo($"  - Sprite BEFORE re-assignment: {sr.sprite?.name ?? "null"}");
                            Plugin.Log.LogInfo($"  - Sorting Layer: {sr.sortingLayerName} ({sr.sortingLayerID})");
                            Plugin.Log.LogInfo($"  - Sorting Order: {sr.sortingOrder}");
                            Plugin.Log.LogInfo($"  - Color: {sr.color}");
                        }
                    }
                    
                    // Register with MapBGManagerHD if available
                    if (mapBGManager != null)
                    {
                        RegisterWithMapBGManager(obj, mapBGManager);
                    }
                    
                    // RE-ASSIGN SPRITE AFTER ALL INITIALIZATION
                    // This is the final fix - sprite gets cleared somewhere, so we re-assign it last
                    if (sprite != null)
                    {
                        var sr = obj.GetComponent<SpriteRenderer>();
                        if (sr != null)
                        {
                            sr.sprite = sprite;
                            Plugin.Log.LogInfo($"[Custom Objects] ✓✓ FINAL sprite re-assignment for {obj.name}: {sprite.name}");
                            
                            if (Plugin.Config.DebugCustomObjects.Value)
                            {
                                Plugin.Log.LogInfo($"  - Sprite AFTER final re-assignment: {sr.sprite?.name ?? "null"}");
                            }
                        }
                    }
                    
                    // RE-APPLY TRANSFORM AFTER ALL INITIALIZATION
                    // MapSpriteHD or native code may reset the transform, so we force it back
                    obj.transform.localPosition = position;
                    obj.transform.localScale = scale;
                    obj.transform.localRotation = rotation;
                    Plugin.Log.LogInfo($"[Custom Objects] ✓✓ FINAL transform re-application for {obj.name}:");
                    Plugin.Log.LogInfo($"  - Position: {position}");
                    Plugin.Log.LogInfo($"  - Scale: {scale}");
                    Plugin.Log.LogInfo($"  - Rotation: {rotation.eulerAngles}");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"[Custom Objects] Failed to create object {objData.Name}: {ex.Message}");
            }
        }
        
        Plugin.Log.LogInfo($"[Custom Objects] Successfully created {successCount}/{objects.Count} objects");
    }

    private static (GameObject obj, Sprite sprite, Vector3 position, Vector3 scale, Quaternion rotation) CreateSingleObject(DiscoveredObject data, Transform parent)
    {
        // Create GameObject
        GameObject customObj = new GameObject(data.Name);
        customObj.transform.SetParent(parent);
        
        // Store desired transform values to re-apply later (after MapSpriteHD interference)
        Vector3 desiredPosition;
        Vector3 desiredScale;
        Quaternion desiredRotation;
        
        // Transform
    if (data.Position != null)
        desiredPosition = data.Position.ToVector3();
    else
        desiredPosition = Vector3.zero;
        
    if (data.Scale != null)
    {
        var scale = data.Scale.ToVector3();
        // Prevent zero scale (causes invisible objects)
        if (scale.x == 0) scale.x = 1;
        if (scale.y == 0) scale.y = 1;
        if (scale.z == 0) scale.z = 1;
        desiredScale = scale;
    }
    else
        desiredScale = Vector3.one;
        
    desiredRotation = Quaternion.Euler(0, 0, data.Rotation);
    
    // Apply transform initially (will be re-applied later)
    customObj.transform.localPosition = desiredPosition;
    customObj.transform.localScale = desiredScale;
    customObj.transform.localRotation = desiredRotation;
        
        // Sorting & Layer
        if (data.Layer != 0) customObj.layer = data.Layer;
        if (!string.IsNullOrEmpty(data.Tag) && data.Tag != "Untagged" && !data.Tag.StartsWith("NativeID"))
        {
             try { customObj.tag = data.Tag; } catch {} // Ignore invalid tags
        }

        // SpriteRenderer
        SpriteRenderer sr = null;
        Sprite spriteToAssign = null; // Store sprite reference to assign AFTER MapSpriteHD component
        
        if (data.HasSpriteRenderer || !string.IsNullOrEmpty(data.Texture))
        {
            sr = customObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = data.SortingOrder;

            // Attempt to copy Sorting Layer AND Material from a sibling
            // This is critical because the map might render on a specific layer (e.g. "Background")
            // preventing our object from being seen if it's on "Default"
            var siblingSr = parent.GetComponentInChildren<SpriteRenderer>();
            if (siblingSr != null)
            {
                 // Copy the GameObject Layer (Physics/Rendering Layer) from sibling
                 // This is CRITICAL if the camera Culling Mask excludes "Default" (Layer 0)
                 if (customObj.layer == 0) // Only override if not set in JSON
                 {
                     customObj.layer = siblingSr.gameObject.layer;
                     Plugin.Log.LogInfo($"[Custom Objects] Copied GameObject Layer from {siblingSr.name}: {LayerMask.LayerToName(customObj.layer)} ({customObj.layer})");
                 }

                 sr.sortingLayerID = siblingSr.sortingLayerID;
                 
                 // Copy the material from the sibling to ensure compatibility
                 if (siblingSr.material != null)
                 {
                     sr.material = siblingSr.material;
                     Plugin.Log.LogInfo($"[Custom Objects] Copied material from {siblingSr.name}: {siblingSr.material.name} (shader: {siblingSr.material.shader.name})");
                 }
                 else
                 {
                     // Fallback if sibling has no material
                     sr.material = new Material(Shader.Find("Sprites/Default"));
                     Plugin.Log.LogInfo($"[Custom Objects] Sibling has no material, using Sprites/Default");
                 }

                 Plugin.Log.LogInfo($"[Custom Objects] Copied Sorting Layer from {siblingSr.name}: {sr.sortingLayerName} ({sr.sortingLayerID})");
            }
            else
            {
                 // No sibling found, force standard shader
                 sr.material = new Material(Shader.Find("Sprites/Default"));
                 Plugin.Log.LogInfo($"[Custom Objects] No sibling found, using Sprites/Default");
            }
            
            // DEBUG: Extreme Sorting Order to force on top of EVERYTHING
            sr.sortingOrder = 20000;

            
            // DEBUG: Extreme Z-offset to bring it WAY close to camera (but not behind it)
            // If camera is at Z=-10, this puts us at -9 relative to parent?
            // Actually, let's try Z = -5 first.
            var pos = customObj.transform.localPosition;
            customObj.transform.localPosition = new Vector3(pos.x, pos.y, -5.0f);

            
            // Handle texture - Load sprite but don't assign yet (assign after activation)
            if (!string.IsNullOrEmpty(data.Texture) && data.Texture.ToLower() != "none" && data.Texture != "Native")
            {
                spriteToAssign = LoadCustomSprite(data.Texture);
                if (spriteToAssign != null)
                {
                    Plugin.Log.LogInfo($"[Custom Objects] Loaded sprite '{spriteToAssign.name}' for {customObj.name}");
                }
                else
                {
                    Plugin.Log.LogWarning($"[Custom Objects] FAILED to load sprite for '{data.Texture}'.");
                    // Only use debug sprite if enabled
                    if (Plugin.Config.DebugCustomObjects.Value)
                    {
                        spriteToAssign = CreateDebugSprite();
                        spriteToAssign.name = "DEBUG_TEXTURE_FAIL";
                        sr.color = Color.red;
                    }
                }
            }
            
            // Fallback for "none" texture
            if (spriteToAssign == null && Plugin.Config.DebugCustomObjects.Value)
            {
                spriteToAssign = CreateDebugSprite();
                spriteToAssign.name = "DEBUG_NO_TEXTURE";
                sr.color = new Color(0, 1, 1, 0.5f); // Cyan
            }
        }
        
        // MapSpriteHD Component
        // This is needed for the game to recognize it as a managed sprite
        if (sr != null)
        {
            AddMapSpriteHD(customObj, sr, data);
        }


        // Active state - activate BEFORE assigning sprite (activation may clear sprite)
        customObj.SetActive(data.Active);
        
        // NOW assign the sprite AFTER object is activated
        if (sr != null && spriteToAssign != null)
        {
            sr.sprite = spriteToAssign;
            Plugin.Log.LogInfo($"[Custom Objects] ✓ Assigned sprite '{spriteToAssign.name}' to {customObj.name} AFTER activation");
            
            // DIAGNOSTIC: Verify sprite immediately after assignment
            Plugin.Log.LogInfo($"[Custom Objects] VERIFY: sr.sprite after final assignment: {(sr.sprite != null ? $"EXISTS (name='{sr.sprite.name}')" : "NULL")}");
            if (sr.sprite != null)
            {
                Plugin.Log.LogInfo($"[Custom Objects]   - Sprite texture: {(sr.sprite.texture != null ? $"{sr.sprite.texture.name} ({sr.sprite.texture.width}x{sr.sprite.texture.height})" : "NULL")}");
                Plugin.Log.LogInfo($"[Custom Objects]   - Sprite rect: {sr.sprite.rect}");
                Plugin.Log.LogInfo($"[Custom Objects]   - Sprite bounds: {sr.sprite.bounds}");
            }
        }
        else
        {
            if (spriteToAssign == null)
                Plugin.Log.LogWarning($"[Custom Objects] No sprite to assign for {customObj.name}");
        }
        
        // Return the created object, sprite, AND transform data for re-assignment
        return (customObj, spriteToAssign, desiredPosition, desiredScale, desiredRotation);
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
                
                // Set all required properties for game integration
                SetProperty(mapSpriteComponent, "hasSpriteRenderer", true);
                SetProperty(mapSpriteComponent, "spriteRenderer", sr);
                
                // Set Size (approximating from scale/collider)
                SetProperty(mapSpriteComponent, "Size", new Vector3(100, 100, 0.2f)); 
                
                // Set gameObject and transform references (may be needed by game)
                SetProperty(mapSpriteComponent, "gameObject", obj);
                SetProperty(mapSpriteComponent, "transform", obj.transform);
                
                Plugin.Log.LogInfo($"[Custom Objects] Added MapSpriteHD component to {obj.name}");
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

    private static System.Type FindMapBGManagerType()
    {
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var assemblyName = assembly.GetName().Name;
            if (assemblyName == "GSD2" || assemblyName == "GSDShare" || assemblyName == "GSD1")
            {
                var type = assembly.GetType("MapBGManagerHD");
                if (type != null) return type;
            }
        }
        return null;
    }

    private static object FindMapBGManagerHD(GameObject sceneRoot)
    {
        try
        {
            // First, check if the PARENT is bgManagerHD (common structure: bgManagerHD/vk08_00(Clone))
            if (sceneRoot.transform.parent != null && sceneRoot.transform.parent.name == "bgManagerHD")
            {
                var mapBGManagerType = FindMapBGManagerType();
                if (mapBGManagerType != null)
                {
                    var il2cppType = Il2CppType.From(mapBGManagerType);
                    // IMPORTANT: Return the component directly - it's already the IL2CPP wrapper
                    var component = sceneRoot.transform.parent.gameObject.GetComponent(il2cppType);
                    if (component != null)
                    {
                        Plugin.Log.LogInfo($"[Custom Objects] Found MapBGManagerHD instance (parent of scene)");
                        return component;
                    }
                }
            }
            
            // Fallback: Search for bgManagerHD as a child
            Transform bgManager = sceneRoot.transform.Find("bgManagerHD");
            if (bgManager == null)
            {
                // Try recursive search
                bgManager = FindBgManagerRecursive(sceneRoot.transform);
            }
            
            if (bgManager != null)
            {
                // Get the MapBGManagerHD component
                var mapBGManagerType = FindMapBGManagerType();
                if (mapBGManagerType != null)
                {
                    var il2cppType = Il2CppType.From(mapBGManagerType);
                    var component = bgManager.gameObject.GetComponent(il2cppType);
                    if (component != null)
                    {
                        Plugin.Log.LogInfo($"[Custom Objects] Found MapBGManagerHD instance (child of scene)");
                        return component;
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogWarning($"[Custom Objects] Error finding MapBGManagerHD: {ex.Message}");
        }
        
        return null;
    }

    private static Transform FindBgManagerRecursive(Transform parent)
    {
        if (parent.name == "bgManagerHD")
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            var result = FindBgManagerRecursive(parent.GetChild(i));
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static void RegisterWithMapBGManager(GameObject customObj, object mapBGManager)
    {
        try
        {
            var managerType = mapBGManager.GetType();
            Plugin.Log.LogInfo($"[Custom Objects] MapBGManager type: {managerType.FullName}");
            
            // Get the MapSpriteHD component from our custom object
            var mapSpriteHDType = FindMapSpriteHDType();
            if (mapSpriteHDType == null)
            {
                Plugin.Log.LogWarning($"[Custom Objects] Could not find MapSpriteHD type");
                return;
            }
            
            var il2cppType = Il2CppType.From(mapSpriteHDType);
            var componentPtr = customObj.GetComponent(il2cppType);
            
            if (componentPtr == null)
            {
                Plugin.Log.LogWarning($"[Custom Objects] No MapSpriteHD component on {customObj.name}");
                return;
            }
            
            // Create a properly typed IL2CPP object from the component pointer
            // The component is a Component type, but we need MapSpriteHD type
            object mapSpriteHD;
            try
            {
                // Use the IL2CPP pointer to create a properly typed instance
                var ptrProperty = componentPtr.GetType().GetProperty("Pointer");
                if (ptrProperty != null)
                {
                    var ptr = (IntPtr)ptrProperty.GetValue(componentPtr);
                    // Create instance using the constructor that takes IntPtr
                    mapSpriteHD = System.Activator.CreateInstance(mapSpriteHDType, ptr);
                    Plugin.Log.LogInfo($"[Custom Objects] Created typed MapSpriteHD instance from pointer");
                }
                else
                {
                    Plugin.Log.LogError($"[Custom Objects] Could not get Pointer property from component");
                    return;
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"[Custom Objects] Failed to create typed MapSpriteHD: {ex.Message}");
                return;
            }
            
            // Try property first (IL2CPP wrapper exposes as property)
            var spritesProp = managerType.GetProperty("sprites");
            if (spritesProp != null)
            {
                Plugin.Log.LogInfo($"[Custom Objects] Found 'sprites' property");
                var sprites = spritesProp.GetValue(mapBGManager);
                
                if (sprites == null)
                {
                    Plugin.Log.LogWarning($"[Custom Objects] 'sprites' list is null - initializing it ourselves");
                    
                    // The game hasn't initialized the list yet, so we'll create it
                    // Use Il2CppSystem.Collections.Generic.List<MapSpriteHD>
                    try
                    {
                        var listType = typeof(Il2CppSystem.Collections.Generic.List<>).MakeGenericType(mapSpriteHDType);
                        sprites = System.Activator.CreateInstance(listType);
                        
                        spritesProp.SetValue(mapBGManager, sprites);
                        
                        Plugin.Log.LogInfo($"[Custom Objects] ✓ Created new sprites list");
                    }
                    catch (System.Exception ex)
                    {
                        Plugin.Log.LogError($"[Custom Objects] Failed to create sprites list: {ex.Message}");
                        return;
                    }
                }
                
                if (sprites != null)
                {
                    // Don't cast to IList - IL2CPP types don't implement .NET interfaces
                    // Use reflection to call Add method directly
                    try
                    {
                        var addMethod = sprites.GetType().GetMethod("Add");
                        if (addMethod != null)
                        {
                            var countProp = sprites.GetType().GetProperty("Count");
                            var currentCount = countProp?.GetValue(sprites) ?? 0;
                            
                            Plugin.Log.LogInfo($"[Custom Objects] About to add to sprites list (current count: {currentCount})");
                            
                            // The mapSpriteHD is a Component, but we need to pass it as the IL2CPP object
                            // Don't wrap it - just pass the object directly since it's already an IL2CPP object
                            Plugin.Log.LogInfo($"[Custom Objects] mapSpriteHD type: {mapSpriteHD.GetType().FullName}");
                            addMethod.Invoke(sprites, new object[] { mapSpriteHD });
                            
                            var newCount = countProp?.GetValue(sprites) ?? 0;
                            Plugin.Log.LogInfo($"[Custom Objects] ✓ Registered {customObj.name} with MapBGManagerHD (sprites list, total: {newCount})");
                        }
                        else
                        {
                            Plugin.Log.LogError($"[Custom Objects] Could not find Add method on sprites list");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Plugin.Log.LogError($"[Custom Objects] Failed to add to sprites list: {ex.Message}");
                        Plugin.Log.LogError($"[Custom Objects] Stack trace: {ex.StackTrace}");
                    }
                }
                else
                {
                    Plugin.Log.LogError($"[Custom Objects] sprites is STILL NULL after creation attempt!");
                }
                
                return;
            }
            
            // Try field as fallback
            var spritesField = managerType.GetField("sprites", BindingFlags.Public | BindingFlags.Instance);
            if (spritesField != null)
            {
                Plugin.Log.LogInfo($"[Custom Objects] Found 'sprites' field");
                var sprites = spritesField.GetValue(mapBGManager) as System.Collections.IList;
                
                if (sprites == null)
                {
                    Plugin.Log.LogWarning($"[Custom Objects] 'sprites' field returned null");
                    return;
                }
                
                sprites.Add(mapSpriteHD);
                Plugin.Log.LogInfo($"[Custom Objects] ✓ Registered {customObj.name} with MapBGManagerHD (sprites list, total: {sprites.Count})");
                return;
            }
            
            // Diagnostic: List all available members
            Plugin.Log.LogWarning($"[Custom Objects] 'sprites' not found as property or field!");
            Plugin.Log.LogInfo($"[Custom Objects] Available properties:");
            foreach (var prop in managerType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Plugin.Log.LogInfo($"  - Property: {prop.Name} ({prop.PropertyType.Name})");
            }
            Plugin.Log.LogInfo($"[Custom Objects] Available fields:");
            foreach (var field in managerType.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                Plugin.Log.LogInfo($"  - Field: {field.Name} ({field.FieldType.Name})");
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"[Custom Objects] Failed to register {customObj.name}: {ex.Message}");
            if (Plugin.Config.DetailedTextureLog.Value)
            {
                Plugin.Log.LogError($"[Custom Objects] Stack trace: {ex.StackTrace}");
            }
        }
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

// Patch to capture the MapBGManagerHD instance after scene load completes
[HarmonyPatch(typeof(MapBGManagerHD), "Load")]
public static class MapBGManagerHDCachePatch
{
    [HarmonyPostfix]
    public static void Postfix(object __instance)
    {
        // Cache the MapBGManagerHD instance AFTER Load() completes - sprites list should be initialized
        CustomObjectInsertion.SetMapBGManagerInstance(__instance);
        Plugin.Log.LogInfo($"[Custom Objects] Cached MapBGManagerHD instance from Load() Postfix");
    }
}
