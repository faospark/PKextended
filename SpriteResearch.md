# GRSpriteRenderer Research & Optimization Notes

## Overview
Research document for analyzing `GRSpriteRenderer` class in dnSpy to improve PKCore's sprite handling patches.

---

## Current Implementation

### Files Using GRSpriteRenderer:
1. **SpriteFilteringPatch.cs** - Texture filtering and anti-aliasing
2. **DisableSpritePostProcessing.cs** - Post-processing exclusion

### Current Patch Points:
- `GRSpriteRenderer.Awake()` - Lifecycle hook
- `GRSpriteRenderer.material` (setter) - Material assignment

### Known Properties:
- `_mat` - Material field (public/internal)
- `material` - Material property

---

## dnSpy Investigation Checklist

### Class Structure
- [ ] Full inheritance hierarchy (base class, interfaces)
- [ ] All public fields and properties
- [ ] All private/internal fields
- [ ] Constructor details
- [ ] Static members

### Sprite/Texture Handling
- [ ] Sprite property/field (if exists)
- [ ] Texture assignment methods
- [ ] Sprite loading/caching mechanisms
- [ ] Atlas handling

### Material Management
- [ ] How `_mat` is initialized
- [ ] Relationship between `_mat` and `material` property
- [ ] Other material-related methods
- [ ] Material caching/pooling

### Lifecycle Methods
- [ ] Awake() implementation details
- [ ] Start() method
- [ ] OnEnable() / OnDisable()
- [ ] Update() / LateUpdate()
- [ ] OnDestroy()

### Related Classes
- [ ] GRTexture (if exists)
- [ ] GRSprite (if exists)
- [ ] Manager classes for sprite rendering
- [ ] Custom shader/material classes

---

## Findings from dnSpy

### Class Definition
```csharp
public class GRSpriteRenderer : MonoBehaviour
// Located in: GSDShare.dll
// Namespace: (global namespace)
```

### Important Properties (Public)
```csharp
Vector2 size { get; }                    // Read-only sprite size
Sprite sprite { get; set; }              // Main sprite property ⭐ KEY PATCH POINT
Sprite normal { get; set; }              // Normal map sprite
bool flipX { get; set; }                 // Horizontal flip
bool flipY { get; set; }                 // Vertical flip
int sortingOrder { get; set; }           // Render order
Color color { get; set; }                // Sprite tint color
Material material { get; set; }          // Material property ⭐ CURRENT PATCH POINT
```

### Important Fields (Internal/Private)
```csharp
float _halfX, _halfY                                              // Half-size cache
Il2CppStructArray<Vector3> _vert                                  // Vertex array
Vector3 _parentPos                                                // Parent position
Vector4 _uvwh                                                     // UV coordinates
Sprite _sprite                                                    // Internal sprite reference ⭐
Dictionary<OPDSprite.MaterialType, Material> materialList         // Material cache
List<Material> _materials                                         // Materials list
List<Mesh> _meshes                                                // Meshes list
MaterialPropertyBlock _matPropBlock                               // Property block
Sprite _normal                                                    // Internal normal sprite
bool _flipX, _flipY                                               // Internal flip state
int _sortingOrder                                                 // Internal sort order
Color _color                                                      // Internal color
Material _mat                                                     // Internal material ⭐ DIRECT ACCESS
Mesh _mesh                                                        // Internal mesh
MeshRenderer _meshRenderer                                        // Mesh renderer component
```

### Important Methods
```csharp
// Sprite Management
void SetForceSprite(Sprite spr)              // Force set sprite (bypasses normal setter)
void UpdateMaterialSprite()                  // Updates material with current sprite ⭐
void SpriteToMesh(Sprite sprite)             // Converts sprite to mesh

// Lifecycle (Unity)
void Awake()                                 // Initialization ⭐ CURRENT PATCH POINT
void OnEnable()                              // When enabled ⭐ POTENTIAL PATCH POINT
void OnDisable()                             // When disabled
void OnDestroy()                             // Cleanup

// Rendering
void SetRenderingOrder(int num)              // Set render order
Mesh GetMesh()                               // Get current mesh
void SetVertex(Il2CppStructArray<Vector3> v) // Set vertices
void SetUVWH(int u, int v, int sw, int sh)   // Set UV coordinates
void SetSize(int sw, int sh)                 // Set sprite size
void UseMaterialPropertyBlock()              // Enable property block
void UpdateFlip()                            // Update flip state
```

### Notes
- **Custom Sprite Renderer**: This is NOT Unity's standard `SpriteRenderer` - it's a custom implementation using `MeshRenderer`
- **Sprite → Mesh Conversion**: Sprites are converted to meshes via `SpriteToMesh()` method
- **Material Management**: Has sophisticated material caching with `materialList` dictionary
- **Direct Field Access**: `_mat` and `_sprite` are directly accessible fields (not just properties)
- **Force Sprite Method**: `SetForceSprite()` exists for bypassing normal sprite assignment logic

---

## Optimization Opportunities

### Potential Improvements
1. **Patch `sprite` property setter instead of just `material`** - More direct interception point
2. **Use `OnEnable()` in addition to `Awake()`** - Catches sprites enabled after initialization
3. **Patch `SetForceSprite()` method** - Catches forced sprite assignments that bypass normal setter
4. **Patch `UpdateMaterialSprite()` method** - Intercepts when sprite is applied to material
5. **Access `_sprite` field directly** - Could read internal sprite state without triggering getters
6. **Leverage material caching** - Could optimize by checking `materialList` dictionary

### Better Patch Points
- **`GRSpriteRenderer.sprite` (setter)** ⭐ BEST - Direct sprite assignment
- **`GRSpriteRenderer.SetForceSprite()`** - Catches forced assignments
- **`GRSpriteRenderer.OnEnable()`** - Catches late-enabled sprites
- **`GRSpriteRenderer.UpdateMaterialSprite()`** - Intercepts sprite→material application
- Keep existing `material` setter patch as fallback

### Performance Considerations
- Material caching via `materialList` dictionary - we should respect this
- Mesh conversion happens in `SpriteToMesh()` - expensive operation
- `_matPropBlock` (MaterialPropertyBlock) - used for per-instance properties
- Multiple materials/meshes supported via `_materials` and `_meshes` lists

---

## Questions to Answer

1. **Does GRSpriteRenderer have a sprite property we can patch?**
   - ✅ **YES!** `sprite { get; set; }` property exists and is PUBLIC
   - This is a MUCH better patch point than just `material` setter

2. **Are there better lifecycle methods than Awake()?**
   - ✅ **YES!** `OnEnable()` would catch sprites enabled after scene load
   - `Awake()` is still good for initial setup
   - Should patch BOTH for complete coverage

3. **Is there texture/sprite caching we should be aware of?**
   - ✅ **YES!** `materialList` dictionary caches materials by type
   - `_materials` and `_meshes` lists store multiple instances
   - We should NOT break this caching system

4. **What's the exact relationship between _mat and material?**
   - `_mat` is the INTERNAL field (direct storage)
   - `material` is the PUBLIC property (getter/setter wrapper)
   - Setting `material` property updates `_mat` field
   - We can access `_mat` directly for reading without triggering setter

5. **Are there any performance-related fields we could optimize?**
   - `_halfX`, `_halfY` - cached half-sizes (don't recalculate)
   - `_vert` - vertex array (reused, don't recreate)
   - `materialList` - material cache (respect this!)
   - `_matPropBlock` - property block (efficient per-instance properties)

6. **How does it integrate with CustomTexturePatch sprite replacement?**
   - **CRITICAL**: We should patch `sprite` setter, NOT just `material`
   - `SetForceSprite()` bypasses normal setter - MUST patch this too
   - `UpdateMaterialSprite()` applies sprite to material - could patch here
   - Current patches miss direct sprite assignments! 

---

## Action Items

### High Priority
- [x] **Add `GRSpriteRenderer.sprite` setter patch to CustomTexturePatch** - ✅ DONE in `GRSpriteRendererPatch.cs`
- [x] **Add `GRSpriteRenderer.SetForceSprite()` patch** - ✅ DONE in `GRSpriteRendererPatch.cs`
- [x] **Add `GRSpriteRenderer.OnEnable()` patch** - ✅ DONE in `GRSpriteRendererPatch.cs`
- [ ] **Test if new patches reduce missed sprite replacements** - NEEDS TESTING

### Medium Priority
- [ ] **Consider patching `UpdateMaterialSprite()`** - Additional safety net
- [ ] **Optimize SpriteFilteringPatch to use `OnEnable()` too** - Better coverage
- [ ] **Review if we need both `Awake()` and `OnEnable()` patches** - Avoid redundancy
- [ ] **Check if `_sprite` field access is useful** - Direct state reading

### Low Priority
- [ ] **Investigate `materialList` caching** - Potential optimization
- [ ] **Research `SpriteToMesh()` timing** - Understanding conversion process
- [ ] **Document `_matPropBlock` usage** - Per-instance property optimization

### Implementation Notes
- ✅ Created `GRSpriteRendererPatch.cs` with all three high-priority patches
- ✅ Exposed helper methods in `CustomTexturePatch.cs` (made internal)
- ✅ Registered patch in `Plugin.cs`
- ✅ Build successful - ready for testing!

---

## Code Refactoring Ideas

### SpriteFilteringPatch.cs
```csharp
// ADD: OnEnable patch for better coverage
[HarmonyPatch(typeof(GRSpriteRenderer), nameof(GRSpriteRenderer.OnEnable))]
[HarmonyPostfix]
static void ApplySpriteFiltering_OnEnable(GRSpriteRenderer __instance)
{
    // Same logic as Awake patch - catches late-enabled sprites
    if (__instance == null || __instance._mat == null || _spriteAntiAliasingLevel <= 0)
        return;
    
    var material = __instance._mat;
    var texture = material.mainTexture;
    // ... apply filtering
}

// CONSIDER: Remove material setter patch if OnEnable + Awake covers everything
```

### DisableSpritePostProcessing.cs
```csharp
// ADD: OnEnable patch for consistency
[HarmonyPatch(typeof(GRSpriteRenderer), nameof(GRSpriteRenderer.OnEnable))]
[HarmonyPostfix]
static void SetSpriteLayer_OnEnable(GRSpriteRenderer __instance)
{
    if (!_isEnabled || __instance == null || __instance.gameObject == null)
        return;
    
    __instance.gameObject.layer = 31;
}

// KEEP: Both Awake and OnEnable for complete coverage
```

### CustomTexturePatch.cs Integration
```csharp
// NEW PATCH: Primary sprite interception point
[HarmonyPatch(typeof(GRSpriteRenderer), nameof(GRSpriteRenderer.sprite), MethodType.Setter)]
[HarmonyPrefix]
public static void GRSpriteRenderer_set_sprite_Prefix(GRSpriteRenderer __instance, ref Sprite value)
{
    if (value == null || !Plugin.Config.EnableCustomTextures.Value)
        return;
    
    string spriteName = value.name;
    LogReplaceableTexture(spriteName, "Sprite - GRSpriteRenderer");
    
    // Try to replace with custom sprite
    if (TryReplaceSprite(spriteName, ref value, value, logReplacement: true))
    {
        Plugin.Log.LogInfo($"[GRSpriteRenderer] Replaced sprite: {spriteName}");
    }
}

// NEW PATCH: Forced sprite assignments
[HarmonyPatch(typeof(GRSpriteRenderer), nameof(GRSpriteRenderer.SetForceSprite))]
[HarmonyPrefix]
public static void GRSpriteRenderer_SetForceSprite_Prefix(GRSpriteRenderer __instance, ref Sprite spr)
{
    if (spr == null || !Plugin.Config.EnableCustomTextures.Value)
        return;
    
    string spriteName = spr.name;
    LogReplaceableTexture(spriteName, "Sprite - ForceSet");
    
    // Try to replace with custom sprite
    if (TryReplaceSprite(spriteName, ref spr, spr, logReplacement: true))
    {
        Plugin.Log.LogInfo($"[GRSpriteRenderer] Force-replaced sprite: {spriteName}");
    }
}

// NEW PATCH: OnEnable for late-enabled sprites
[HarmonyPatch(typeof(GRSpriteRenderer), nameof(GRSpriteRenderer.OnEnable))]
[HarmonyPostfix]
public static void GRSpriteRenderer_OnEnable_Postfix(GRSpriteRenderer __instance)
{
    if (!Plugin.Config.EnableCustomTextures.Value || __instance == null)
        return;
    
    // Check if sprite needs replacement
    Sprite currentSprite = __instance._sprite; // Direct field access
    if (currentSprite != null && texturePathIndex.ContainsKey(currentSprite.name))
    {
        Sprite customSprite = LoadCustomSprite(currentSprite.name, currentSprite);
        if (customSprite != null)
        {
            __instance.sprite = customSprite; // Use property setter
            Plugin.Log.LogInfo($"[GRSpriteRenderer] Replaced sprite on enable: {currentSprite.name}");
        }
    }
}
```

---

## References
- dnSpy analysis date: 
- Game version: Suikoden I & II HD Remaster
- Related conversation: [Current conversation ID]
