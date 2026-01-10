using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Share.UI.Window;

namespace PKCore.Patches
{
    /// <summary>
    /// Disables mask textures applied to UI elements via material properties
    /// Works by replacing _Mask_Map textures with blank versions from PKCore/Textures
    /// Supports any mask texture (Face_Mask_01, etc.) if a corresponding blank texture exists
    /// </summary>
    public class DisableMask
    {
        private static bool _initialized = false;
        private static System.Collections.Generic.HashSet<string> _excludedMasks = new System.Collections.Generic.HashSet<string>();

        public static void Initialize(System.Collections.Generic.HashSet<string> excludedMasks = null)
        {
            if (_initialized)
                return;

            _initialized = true;
            _excludedMasks = excludedMasks ?? new System.Collections.Generic.HashSet<string>();
            
            if (_excludedMasks.Count > 0)
            {
                Plugin.Log.LogInfo($"[DisableMask] Initialized - will replace all mask textures EXCEPT: {string.Join(", ", _excludedMasks)}");
            }
            else
            {
                Plugin.Log.LogInfo("[DisableMask] Initialized - will replace all mask textures found in PKCore/Textures");
            }
        }

        private static System.Collections.Generic.Dictionary<string, Texture2D> _maskTextures = new System.Collections.Generic.Dictionary<string, Texture2D>();

        /// <summary>
        /// Load a mask texture from PKCore/Textures by name
        /// </summary>
        private static Texture2D LoadMaskTexture(string maskName)
        {
            // Check cache first
            if (_maskTextures.ContainsKey(maskName))
                return _maskTextures[maskName];

            string maskPath = System.IO.Path.Combine(
                BepInEx.Paths.GameRootPath,
                "PKCore", "Textures", $"{maskName}.png"
            );

            if (System.IO.File.Exists(maskPath))
            {
                byte[] fileData = System.IO.File.ReadAllBytes(maskPath);
                Texture2D maskTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                
                if (ImageConversion.LoadImage(maskTexture, fileData))
                {
                    maskTexture.name = $"{maskName}_Replacement";
                    UnityEngine.Object.DontDestroyOnLoad(maskTexture);
                    _maskTextures[maskName] = maskTexture;
                    Plugin.Log.LogInfo($"[DisableMask] Loaded replacement texture for '{maskName}': {maskTexture.width}x{maskTexture.height}");
                    return maskTexture;
                }
                else
                {
                    Plugin.Log.LogError($"[DisableMask] Failed to load texture data for '{maskName}'");
                }
            }

            // Return null if not found - no replacement available
            return null;
        }

        /// <summary>
        /// Search for and replace mask textures in Image materials
        /// </summary>
        private static void DisableMaskInHierarchy(GameObject faceObject)
        {
            if (faceObject == null)
            {
                Plugin.Log.LogWarning("[DisableMask] faceObject is null!");
                return;
            }

            if (Plugin.Config.LogReplaceableTextures.Value)
                Plugin.Log.LogInfo($"[DisableMask] Searching in: {faceObject.name}");

            // Search all Image components
            var allImages = faceObject.GetComponentsInChildren<Image>(true);
            if (Plugin.Config.LogReplaceableTextures.Value)
                Plugin.Log.LogInfo($"[DisableMask] Found {allImages.Length} Image components to check");
            
            foreach (var image in allImages)
            {
                // Check if this Image has a material
                if (image.material != null)
                {
                    string materialName = image.material.name;
                    if (Plugin.Config.LogReplaceableTextures.Value)
                        Plugin.Log.LogInfo($"[DisableMask] Image '{image.gameObject.name}' uses material: {materialName}");
                    
                    // Check if it's a face material or any material with mask properties
                    if (materialName.Contains("UI_Message_Face") || materialName.Contains("Face"))
                    {
                        if (Plugin.Config.LogReplaceableTextures.Value)
                            Plugin.Log.LogInfo($"[DisableMask] Found face material: {materialName}");
                        
                        // Log all texture properties for debugging
                        var shader = image.material.shader;
                        int propertyCount = shader.GetPropertyCount();
                        if (Plugin.Config.LogReplaceableTextures.Value)
                            Plugin.Log.LogInfo($"[DisableMask] Material has {propertyCount} properties:");
                        
                        for (int i = 0; i < propertyCount; i++)
                        {
                            var propType = shader.GetPropertyType(i);
                            if (propType == UnityEngine.Rendering.ShaderPropertyType.Texture)
                            {
                                string propName = shader.GetPropertyName(i);
                                var tex = image.material.GetTexture(propName);
                                string texName = tex != null ? tex.name : "null";
                                if (Plugin.Config.LogReplaceableTextures.Value)
                                    Plugin.Log.LogInfo($"[DisableMask]   Texture property '{propName}': {texName}");
                                
                                // If we find a mask texture
                                if (tex != null && (tex.name.Contains("Face_Mask") || tex.name.Contains("_Mask") || propName == "_Mask_Map"))
                                {
                                    string maskTextureName = tex.name;
                                    
                                    // Check if this mask is excluded
                                    if (_excludedMasks.Contains(maskTextureName))
                                    {
                                        if (Plugin.Config.LogReplaceableTextures.Value)
                                            Plugin.Log.LogInfo($"[DisableMask] Skipping excluded mask: '{maskTextureName}'");
                                        continue;
                                    }
                                    
                                    // Try to load replacement texture
                                    Texture2D replacementTexture = LoadMaskTexture(maskTextureName);
                                    
                                    if (replacementTexture != null)
                                    {
                                        image.material.SetTexture(propName, replacementTexture);
                                        Plugin.Log.LogInfo($"[DisableMask] ✓ Replaced '{maskTextureName}' in property '{propName}'");
                                    }
                                    else
                                    {
                                        if (Plugin.Config.LogReplaceableTextures.Value)
                                            Plugin.Log.LogInfo($"[DisableMask] No replacement texture found for '{maskTextureName}'");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Patch UIMessage.Initialize to disable masks when message window is created
        /// </summary>
        [HarmonyPatch(typeof(UIMessage), nameof(UIMessage.Initialize))]
        [HarmonyPostfix]
        public static void Initialize_Postfix(UIMessage __instance)
        {
            Plugin.Log.LogInfo("[DisableMask] UIMessage.Initialize called");
            
            if (__instance.windowObject != null)
            {
                DisableMaskInHierarchy(__instance.windowObject);
            }
        }

        /// <summary>
        /// Patch UIMessage.SetFaceImage to disable masks after face is set
        /// </summary>
        [HarmonyPatch(typeof(UIMessage), nameof(UIMessage.SetFaceImage))]
        [HarmonyPostfix]
        public static void SetFaceImage_Postfix(UIMessage __instance)
        {
            if (Plugin.Config.LogReplaceableTextures.Value)
                Plugin.Log.LogInfo("[DisableMask] SetFaceImage called");
            
            if (__instance.windowObject != null)
            {
                DisableMaskInHierarchy(__instance.windowObject);
            }
        }

        /// <summary>
        /// Patch UIMessage.SetFaceImageClassic to disable masks after face is set
        /// </summary>
        [HarmonyPatch(typeof(UIMessage), nameof(UIMessage.SetFaceImageClassic))]
        [HarmonyPostfix]
        public static void SetFaceImageClassic_Postfix(UIMessage __instance)
        {
            if (Plugin.Config.LogReplaceableTextures.Value)
                Plugin.Log.LogInfo("[DisableMask] SetFaceImageClassic called");
            
            if (__instance.windowObject != null)
            {
                DisableMaskInHierarchy(__instance.windowObject);
            }
        }

        /// <summary>
        /// Patch UIMessage.PlayOpenAnimation to disable masks when dialog opens
        /// </summary>
        [HarmonyPatch(typeof(UIMessage), nameof(UIMessage.PlayOpenAnimation))]
        [HarmonyPostfix]
        public static void PlayOpenAnimation_Postfix(UIMessage __instance)
        {
            if (Plugin.Config.LogReplaceableTextures.Value)
                Plugin.Log.LogInfo("[DisableMask] PlayOpenAnimation called");
            
            if (__instance.windowObject != null)
            {
                DisableMaskInHierarchy(__instance.windowObject);
            }
        }
    }
}
