using HarmonyLib;
using UnityEngine;
using System.Collections;
using System.Reflection;
using System.Linq;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Il2CppInterop.Runtime.Injection;

namespace PKCore.Patches;

/// <summary>
/// Diagnostic patch for MapBGManagerHD to log object structure
/// Uses reflection to avoid IL2CPP type issues
/// </summary>
public class MapBGManagerHDDiagnostics
{
    private static bool _initialized = false;
    private static Harmony _harmony;
    private static bool _patched = false;

    public static void Initialize(bool enabled, Harmony harmony)
    {
        _initialized = enabled;
        _harmony = harmony;
        if (!enabled) return;

        Plugin.Log.LogInfo("[MapBGManagerHD Diagnostics] Starting type detection...");
        
        // Register the MonoBehaviour with IL2CPP
        ClassInjector.RegisterTypeInIl2Cpp<TypeDetectorBehaviour>();
        
        // Create a GameObject with our MonoBehaviour to run the coroutine
        var go = new GameObject("MapBGManagerHD_TypeDetector");
        GameObject.DontDestroyOnLoad(go);
        go.AddComponent<TypeDetectorBehaviour>().StartDetection(harmony);
    }

    /// <summary>
    /// MonoBehaviour helper to run coroutines
    /// </summary>
    private class TypeDetectorBehaviour : MonoBehaviour
    {
        public void StartDetection(Harmony harmony)
        {
            StartCoroutine(WaitForMapBGManagerType(harmony).WrapToIl2Cpp());
        }

        private IEnumerator WaitForMapBGManagerType(Harmony harmony)
        {
            // Wait a few seconds for the game to load
            yield return new WaitForSeconds(2f);

            int attempts = 0;
            while (!_patched && attempts < 30) // Try for up to 60 seconds
            {
                Plugin.Log.LogInfo($"[MapBGManagerHD Diagnostics] Checking for MapBGManagerHD type (attempt {attempts + 1})...");
                
                TryPatchMapBGManager(harmony);
                
                if (_patched)
                {
                    Plugin.Log.LogInfo("[MapBGManagerHD Diagnostics] Successfully patched!");
                    Destroy(gameObject); // Clean up
                    yield break;
                }

                attempts++;
                yield return new WaitForSeconds(2f);
            }

            if (!_patched)
            {
                Plugin.Log.LogWarning("[MapBGManagerHD Diagnostics] Failed to find MapBGManagerHD after 30 attempts. Make sure you're playing Suikoden II.");
            }
            
            Destroy(gameObject); // Clean up
        }
    }

    private static void TryPatchMapBGManager(Harmony harmony)
    {
        try
        {
            // Find the MapBGManagerHD type in loaded assemblies
            var mapBGManagerType = FindMapBGManagerType();
            
            if (mapBGManagerType == null)
            {
                return; // Silently return, we'll try again
            }

            Plugin.Log.LogInfo($"[MapBGManagerHD Diagnostics] Found type: {mapBGManagerType.FullName} in assembly: {mapBGManagerType.Assembly.GetName().Name}");

            // Find the Load method
            var loadMethod = mapBGManagerType.GetMethod("Load", BindingFlags.Public | BindingFlags.Instance);
            
            if (loadMethod == null)
            {
                Plugin.Log.LogError("[MapBGManagerHD Diagnostics] Could not find Load() method");
                return;
            }

            Plugin.Log.LogInfo($"[MapBGManagerHD Diagnostics] Found Load() method, applying patch...");

            // Manually patch the method
            var postfix = typeof(MapBGManagerHDDiagnostics).GetMethod(nameof(Load_Postfix), BindingFlags.Static | BindingFlags.Public);
            harmony.Patch(loadMethod, postfix: new HarmonyMethod(postfix));

            _patched = true;
            Plugin.Log.LogInfo("[MapBGManagerHD Diagnostics] ✓ Successfully patched MapBGManagerHD.Load()");
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"[MapBGManagerHD Diagnostics] Failed to patch: {ex}");
        }
    }

    private static System.Type FindMapBGManagerType()
    {
        // Search in all loaded assemblies
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var assemblyName = assembly.GetName().Name;
                
                // Focus on game assemblies (GSD1, GSD2)
                if (assemblyName == "GSD1" || assemblyName == "GSD2")
                {
                    Plugin.Log.LogInfo($"[MapBGManagerHD Diagnostics] Scanning assembly: {assemblyName}");
                    
                    var types = assembly.GetTypes();
                    Plugin.Log.LogInfo($"[MapBGManagerHD Diagnostics] Found {types.Length} types in {assemblyName}");
                    
                    // Log types that contain "Map" and "Manager" in their name
                    var mapManagerTypes = types.Where(t => 
                        (t.Name.Contains("Map") || t.Name.Contains("BG")) && 
                        (t.Name.Contains("Manager") || t.Name.Contains("HD"))
                    ).ToList();
                    
                    if (mapManagerTypes.Any())
                    {
                        Plugin.Log.LogInfo($"[MapBGManagerHD Diagnostics] Found {mapManagerTypes.Count} potential map manager types:");
                        foreach (var type in mapManagerTypes)
                        {
                            Plugin.Log.LogInfo($"  - {type.FullName ?? type.Name}");
                        }
                    }
                    
                    // Try exact match first
                    var exactMatch = types.FirstOrDefault(t => t.Name == "MapBGManagerHD");
                    if (exactMatch != null)
                    {
                        return exactMatch;
                    }
                    
                    // Try partial matches
                    var partialMatch = types.FirstOrDefault(t => 
                        t.Name.Contains("MapBGManager") || 
                        t.Name == "bgManagerHD"
                    );
                    
                    if (partialMatch != null)
                    {
                        Plugin.Log.LogInfo($"[MapBGManagerHD Diagnostics] Using partial match: {partialMatch.Name}");
                        return partialMatch;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning($"[MapBGManagerHD Diagnostics] Error scanning assembly: {ex.Message}");
                continue;
            }
        }

        return null;
    }

    public static IEnumerator Load_Postfix(IEnumerator __result, object __instance)
    {
        // Let the original Load complete first
        while (__result.MoveNext())
        {
            yield return __result.Current;
        }

        // Now log the objects if diagnostics are enabled
        if (_initialized)
        {
            LogSceneObjects(__instance);
        }
    }

    private static void LogSceneObjects(object manager)
    {
        try
        {
            Plugin.Log.LogInfo("========================================");
            Plugin.Log.LogInfo("=== MapBGManagerHD Scene Loaded ===");
            Plugin.Log.LogInfo("========================================");

            var managerType = manager.GetType();

            // Get basic info using reflection
            var gameObjectProp = managerType.GetProperty("gameObject");
            var assetProp = managerType.GetProperty("asset");
            var isLoadedProp = managerType.GetProperty("isLoaded");
            var isNightProp = managerType.GetProperty("IsNight");
            var hdScaleProp = managerType.GetProperty("HDScale");

            var gameObject = gameObjectProp?.GetValue(manager) as GameObject;
            var asset = assetProp?.GetValue(manager) as GameObject;
            var isLoaded = isLoadedProp?.GetValue(manager);
            var isNight = isNightProp?.GetValue(manager);
            var hdScale = hdScaleProp?.GetValue(null); // static property

            Plugin.Log.LogInfo($"GameObject: {gameObject?.name ?? "null"}");
            Plugin.Log.LogInfo($"Asset: {asset?.name ?? "null"}");
            Plugin.Log.LogInfo($"Is Loaded: {isLoaded}");
            Plugin.Log.LogInfo($"HD Scale: {hdScale}");
            Plugin.Log.LogInfo($"Is Night: {isNight}");

            // Get object lists
            var eventObjectsProp = managerType.GetProperty("eventObjects");
            var spritesProp = managerType.GetProperty("sprites");
            var animObjectsProp = managerType.GetProperty("animObjects");
            var flagCheckObjectsProp = managerType.GetProperty("flagCheckObjects");
            var particlesProp = managerType.GetProperty("particles");
            var spriteRendereresProp = managerType.GetProperty("spriterendereres");

            var eventObjects = eventObjectsProp?.GetValue(manager) as IList;
            var sprites = spritesProp?.GetValue(manager) as IList;
            var animObjects = animObjectsProp?.GetValue(manager) as IList;
            var flagCheckObjects = flagCheckObjectsProp?.GetValue(manager) as IList;
            var particles = particlesProp?.GetValue(manager) as IList;
            var spriteRenderers = spriteRendereresProp?.GetValue(manager) as IList;

            // Log object counts
            Plugin.Log.LogInfo("----------------------------------------");
            Plugin.Log.LogInfo("Object Counts:");
            Plugin.Log.LogInfo($"  Event Objects: {eventObjects?.Count ?? 0}");
            Plugin.Log.LogInfo($"  Sprites: {sprites?.Count ?? 0}");
            Plugin.Log.LogInfo($"  Anim Objects: {animObjects?.Count ?? 0}");
            Plugin.Log.LogInfo($"  Flag Check Objects: {flagCheckObjects?.Count ?? 0}");
            Plugin.Log.LogInfo($"  Particles: {particles?.Count ?? 0}");
            Plugin.Log.LogInfo($"  Sprite Renderers: {spriteRenderers?.Count ?? 0}");

            // Log first few event objects in detail
            if (eventObjects != null && eventObjects.Count > 0)
            {
                Plugin.Log.LogInfo("----------------------------------------");
                Plugin.Log.LogInfo("Event Objects (first 5):");
                
                int count = System.Math.Min(5, eventObjects.Count);
                for (int i = 0; i < count; i++)
                {
                    var obj = eventObjects[i];
                    if (obj != null)
                    {
                        LogEventObject(obj, i);
                    }
                }
            }

            // Log first few sprites in detail
            if (sprites != null && sprites.Count > 0)
            {
                Plugin.Log.LogInfo("----------------------------------------");
                Plugin.Log.LogInfo("Sprites (first 5):");
                
                int count = System.Math.Min(5, sprites.Count);
                for (int i = 0; i < count; i++)
                {
                    var sprite = sprites[i];
                    if (sprite != null)
                    {
                        LogSprite(sprite, i);
                    }
                }
            }

            // Log first few animated objects
            if (animObjects != null && animObjects.Count > 0)
            {
                Plugin.Log.LogInfo("----------------------------------------");
                Plugin.Log.LogInfo("Animated Objects (first 5):");
                
                int count = System.Math.Min(5, animObjects.Count);
                for (int i = 0; i < count; i++)
                {
                    var animObj = animObjects[i];
                    if (animObj != null)
                    {
                        LogAnimObject(animObj, i);
                    }
                }
            }

            Plugin.Log.LogInfo("========================================");
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"Error in MapBGManagerHD diagnostics: {ex.Message}");
            Plugin.Log.LogError($"Stack trace: {ex.StackTrace}");
        }
    }

    private static void LogEventObject(object obj, int index)
    {
        try
        {
            var objType = obj.GetType();
            var gameObjectProp = objType.GetProperty("gameObject");
            var transformProp = objType.GetProperty("transform");
            
            var go = gameObjectProp?.GetValue(obj) as GameObject;
            var transform = transformProp?.GetValue(obj) as Transform;
            
            Plugin.Log.LogInfo($"  [{index}] Event Object:");
            Plugin.Log.LogInfo($"      Name: {go?.name ?? "null"}");
            Plugin.Log.LogInfo($"      Type: {objType.Name}");
            
            if (transform != null)
            {
                Plugin.Log.LogInfo($"      Position: ({transform.position.x:F2}, {transform.position.y:F2}, {transform.position.z:F2})");
                Plugin.Log.LogInfo($"      Local Position: ({transform.localPosition.x:F2}, {transform.localPosition.y:F2}, {transform.localPosition.z:F2})");
            }
            
            // Try to get sprite renderer if it has one
            var spriteRenderer = go?.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                Plugin.Log.LogInfo($"      Sprite: {spriteRenderer.sprite.name}");
                Plugin.Log.LogInfo($"      Sorting Order: {spriteRenderer.sortingOrder}");
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogWarning($"  [{index}] Error logging event object: {ex.Message}");
        }
    }

    private static void LogSprite(object sprite, int index)
    {
        try
        {
            var spriteType = sprite.GetType();
            var gameObjectProp = spriteType.GetProperty("gameObject");
            var transformProp = spriteType.GetProperty("transform");
            
            var go = gameObjectProp?.GetValue(sprite) as GameObject;
            var transform = transformProp?.GetValue(sprite) as Transform;
            
            Plugin.Log.LogInfo($"  [{index}] Sprite:");
            Plugin.Log.LogInfo($"      Name: {go?.name ?? "null"}");
            Plugin.Log.LogInfo($"      Type: {spriteType.Name}");
            
            if (transform != null)
            {
                Plugin.Log.LogInfo($"      Position: ({transform.position.x:F2}, {transform.position.y:F2}, {transform.position.z:F2})");
            }
            
            var spriteRenderer = go?.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                Plugin.Log.LogInfo($"      Sprite Name: {spriteRenderer.sprite.name}");
                Plugin.Log.LogInfo($"      Layer: {spriteRenderer.sortingLayerName}");
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogWarning($"  [{index}] Error logging sprite: {ex.Message}");
        }
    }

    private static void LogAnimObject(object animObj, int index)
    {
        try
        {
            var animType = animObj.GetType();
            var gameObjectProp = animType.GetProperty("gameObject");
            var transformProp = animType.GetProperty("transform");
            
            var go = gameObjectProp?.GetValue(animObj) as GameObject;
            var transform = transformProp?.GetValue(animObj) as Transform;
            
            Plugin.Log.LogInfo($"  [{index}] Anim Object:");
            Plugin.Log.LogInfo($"      Name: {go?.name ?? "null"}");
            Plugin.Log.LogInfo($"      Type: {animType.Name}");
            
            if (transform != null)
            {
                Plugin.Log.LogInfo($"      Position: ({transform.position.x:F2}, {transform.position.y:F2}, {transform.position.z:F2})");
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogWarning($"  [{index}] Error logging anim object: {ex.Message}");
        }
    }
}
