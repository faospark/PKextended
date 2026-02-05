using HarmonyLib;
using UnityEngine;
using PKCore;
using EventOverlayClass;

namespace PKCore.Patches;

/// <summary>
/// Hooks into EventOverlayClass.OverlaySuikozu2 to intercept the creation of the map object.
/// Distinguishes between the Map Object and the Player Dot to apply textures correctly.
/// </summary>
public class SuikozuPatch
{
    private static bool _isRegistered = false;
    
    // Lazy registration - only register when first Suikozu map is created (GSD2 only)
    private static void EnsureRegistered()
    {
        if (_isRegistered) return;
        
        try 
        {
            Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<SuikozuTextureEnforcer>();
            if (Plugin.Config.DetailedTextureLog.Value)
                Plugin.Log.LogInfo("[SuikozuPatch] Registered SuikozuTextureEnforcer type (lazy-loaded on first world map open)");
            _isRegistered = true;
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogError($"[SuikozuPatch] Failed to register SuikozuTextureEnforcer: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Checks if an object is a Suikozu map object and attaches a monitor if needed
    /// Called from GameObjectPatch/scanners
    /// </summary>
    public static void CheckAndAttachMonitor(GameObject go)
    {
        if (go == null) return;

        bool isSuikozu = go.name.Contains("Suikozu", System.StringComparison.OrdinalIgnoreCase);

        // Also check if the object has a Suikozu texture assigned
        if (!isSuikozu)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null && renderer.material.mainTexture != null)
            {
                if (renderer.material.mainTexture.name.StartsWith("suikozu_", System.StringComparison.OrdinalIgnoreCase))
                {
                    isSuikozu = true;
                }
            }
        }

        if (isSuikozu)
        {
            EnsureRegistered();
            AttachTextureEnforcer(go, null);
        }
    }

    // Capture objects created and IMMEDIATELY attach the Smart Enforcer.
    // We attach to everything (Map and Dot) because the Smart Enforcer only activates 
    // if it detects a 'suikozu_' texture assignment.
    [HarmonyPatch(typeof(OverlaySuikozu2), nameof(OverlaySuikozu2.CreateMapObj))]
    [HarmonyPostfix]
    public static void CreateMapObj_Postfix(int type, GameObject __result)
    {
        if (__result == null) return;
        
        // Ensure type is registered before attaching component
        EnsureRegistered();
        
        if (Plugin.Config.DetailedTextureLog.Value)
            Plugin.Log.LogInfo($"[SuikozuPatch] CreateMapObj produced: {__result.name} (Type: {type}) - Attaching Smart Enforcer.");
        
        // Immediate attachment
        AttachTextureEnforcer(__result, null);
    }

    [HarmonyPatch(typeof(OverlaySuikozu2), nameof(OverlaySuikozu2.SuikozuInit))]
    [HarmonyPostfix]
    public static void SuikozuInit_Postfix(OverlaySuikozu2.WORLD_SUIKOZU __result)
    {
        if (Plugin.Config.DetailedTextureLog.Value)
            Plugin.Log.LogInfo("[SuikozuPatch] SuikozuInit completed. Identifying objects...");
        
        // 1. Identify MAP Object
        GameObject mapObj = OverlaySuikozu2.SuikozuObj;
        if (mapObj != null)
        {
            if (Plugin.Config.DetailedTextureLog.Value)
                Plugin.Log.LogInfo($"[SuikozuPatch] IDENTIFIED MAP OBJECT: {mapObj.name}");
            
            // We pass NULL as target name to let the Enforcer auto-detect whatever the game assigns (e.g. suikozu_03)
            ProcessMapObject(mapObj, null); 
        }
        else
        {
            Plugin.Log.LogWarning("[SuikozuPatch] OverlaySuikozu2.SuikozuObj (Map) is NULL.");
        }

        // 2. Identify DOT Object
        GameObject dotObj = OverlaySuikozu2.DotObj;
        if (dotObj != null)
        {
            if (Plugin.Config.DetailedTextureLog.Value)
                Plugin.Log.LogInfo($"[SuikozuPatch] IDENTIFIED DOT OBJECT: {dotObj.name}");
        }
    }

    private static void ProcessMapObject(GameObject obj, string targetTextureName)
    {
        // Attach Enforcer to the MAP object only
        AttachTextureEnforcer(obj, targetTextureName);
    }

    private static void AttachTextureEnforcer(GameObject obj, string targetName)
    {
        var enforcer = obj.GetComponent<SuikozuTextureEnforcer>();
        if (enforcer == null)
        {
            enforcer = obj.AddComponent<SuikozuTextureEnforcer>();
            if (Plugin.Config.DetailedTextureLog.Value)
                Plugin.Log.LogInfo($"[SuikozuPatch] Attached Smart Texture Enforcer to MAP {obj.name}. (Waiting for game to assign texture)");
        }
        
        enforcer.Initialize(targetName);
    }
}

public class SuikozuTextureEnforcer : MonoBehaviour
{
    private string _targetTextureName;
    private float _checkInterval = 0.5f; // Check every 0.5 seconds
    private float _timeSinceLastCheck = 0f;
    private bool _isLocked = false;

    public void Initialize(string targetName)
    {
        _targetTextureName = targetName;
        if (!string.IsNullOrEmpty(_targetTextureName))
        {
            _isLocked = true;
            _timeSinceLastCheck = _checkInterval; // Trigger immediate check next update
        }
    }

    public void Update()
    {
        _timeSinceLastCheck += Time.deltaTime;
        if (_timeSinceLastCheck < _checkInterval) return;
        _timeSinceLastCheck = 0f;

        var renderer = GetComponent<MeshRenderer>();
        if (renderer == null) renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer == null) return;

        // Auto-Detection Phase
        if (!_isLocked)
        {
            if (renderer.material != null && renderer.material.mainTexture != null)
            {
                string texName = renderer.material.mainTexture.name;
                if (!string.IsNullOrEmpty(texName) && texName.StartsWith("suikozu_", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (Plugin.Config.DetailedTextureLog.Value)
                        Plugin.Log.LogInfo($"[SuikozuEnforcer] Auto-Detected Game Map: {texName}");
                    _targetTextureName = texName;
                    _isLocked = true;
                    // Immediately fix it
                    ForceAssign(renderer);
                }
            }
            return;
        }

        // Enforcement Phase
        if (string.IsNullOrEmpty(_targetTextureName)) return;

        // Check if fix needed
        bool needsFix = false;
        
        if (renderer.material == null || renderer.material.mainTexture == null)
        {
            needsFix = true;
        }
        else
        {
            // Check name matching
            // If the game swapped it back to original (non-custom), usually the name stays same in Unity?
            // Actually, we need to check if it's OUR custom texture (Wrap Mode is a good proxy)
            
            if (renderer.material.mainTexture.name != _targetTextureName)
            {
                    // Name mismatch?
                    // If it's another suikozu_XX, maybe the game switched maps? Update target?
                    if (renderer.material.mainTexture.name.StartsWith("suikozu_", System.StringComparison.OrdinalIgnoreCase))
                    {
                        Plugin.Log.LogInfo($"[SuikozuEnforcer] Map switched from {_targetTextureName} to {renderer.material.mainTexture.name}. Updating target.");
                        _targetTextureName = renderer.material.mainTexture.name;
                        ForceAssign(renderer);
                        return;
                    }
                    needsFix = true;
            }
            else if (renderer.material.mainTexture.wrapMode != TextureWrapMode.Repeat)
            {
                    // Texture matches name, but WrapMode implies it's the original asset (Clamp)
                    // or we haven't touched it yet.
                    needsFix = true; 
            }
        }

        if (needsFix)
        {
             // We do check every interval anyway, but let's force log
             // Plugin.Log.LogInfo($"[SuikozuEnforcer] Enforcing {_targetTextureName}...");
             ForceAssign(renderer);
        }
    }

    private void ForceAssign(Renderer renderer)
    {
         Sprite customSprite = CustomTexturePatch.LoadCustomSprite(_targetTextureName, null);
         if (customSprite != null && customSprite.texture != null)
         {
             if (renderer.material == null)
             {
                 // Try to preserve existing shader if possible via Shader.Find("GR/SpriteDefault") ?
                 // Or fallback
                 var shader = Shader.Find("GR/SpriteDefault");
                 if (shader == null) shader = Shader.Find("Unlit/Transparent");
                 if (shader == null) shader = Shader.Find("Sprites/Default");
                 if (shader != null) renderer.material = new Material(shader);
             }
             
             if (renderer.material != null)
             {
                 // CRITICAL: Set Repeat on the TEXTURE object
                 customSprite.texture.wrapMode = TextureWrapMode.Repeat; 
                 
                 renderer.material.mainTexture = customSprite.texture;
                 // Ensure name persists so we valid against it
                 renderer.material.mainTexture.name = _targetTextureName; 
                 
                 // Log once per lock?
                 // Plugin.Log.LogInfo($"[SuikozuEnforcer] Applied {_targetTextureName} (Repeat)");
             }
         }
    }
}
