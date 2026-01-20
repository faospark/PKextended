using UnityEngine;

namespace PKCore.Patches
{
    /// <summary>
    /// Monitors the Dialog Window and enforces custom scaling.
    /// This ensures that even if animations try to reset the scale to 1.0, 
    /// our custom "Small/Medium" setting is reapplied immediately in LateUpdate.
    /// </summary>
    public class DialogMonitor : MonoBehaviour
    {
        private Transform _cachedTransform;
        private string _lastPreset = "";
        private float _targetScale = 1.0f;
        private float _targetYOffset = 0f;
        private bool _isActive = false;

        private void Awake()
        {
            _cachedTransform = transform;
        }

        private void OnEnable()
        {
            Plugin.Log.LogInfo("[DialogMonitor] Enabled. Enforcing scale...");
            RefreshTargetScale();
            ApplyTransform();
            _isActive = true;
        }

        private void OnDisable()
        {
            _isActive = false;
        }

        private void RefreshTargetScale()
        {
            string currentPreset = Plugin.Config.DialogBoxScale.Value;
            
            // Optimization: Only recalculate if config changed
            if (_lastPreset == currentPreset && _targetScale != 0f) return;

            _lastPreset = currentPreset;
            _targetScale = GetScaleFromPreset(currentPreset);
            
            // Calculate position offset based on scale
            // At 0.5 scale: -208 offset (very compact)
            // At 0.8 scale: -104 offset (smaller)
            // At 1.0 scale: 0 offset (no change)
            _targetYOffset = Mathf.Lerp(-208f, 0f, (_targetScale - 0.5f) / 0.5f);
            
            Plugin.Log.LogDebug($"[DialogMonitor] Recalculated target: Scale={_targetScale}, OffsetY={_targetYOffset}");
        }

        private float GetScaleFromPreset(string preset)
        {
            switch (preset.ToLower())
            {
                case "small": return 0.5f;
                case "medium": return 0.8f;
                case "large": default: return 1.0f;
            }
        }

        private void LateUpdate()
        {
            // Only enforce if we have a non-standard scale
            if (!_isActive || _targetScale >= 0.99f) return;

            // Check if transform has drifted from our target (e.g. by animation)
            // Using a small epsilon to avoid fighting floating point errors
            if (Mathf.Abs(_cachedTransform.localScale.x - _targetScale) > 0.01f ||
                Mathf.Abs(_cachedTransform.localPosition.y - _targetYOffset) > 0.1f)
            {
                ApplyTransform();
            }
        }

        private void ApplyTransform()
        {
            if (_targetScale >= 0.99f) return; // Don't touch if standard size

            // Force the scale
            _cachedTransform.localScale = new Vector3(_targetScale, _targetScale, 1f);

            // Force the position (keeping X and Z, modifying Y)
            Vector3 currentPos = _cachedTransform.localPosition;
            if (Mathf.Abs(currentPos.y - _targetYOffset) > 0.1f)
            {
                _cachedTransform.localPosition = new Vector3(currentPos.x, _targetYOffset, currentPos.z);
            }
        }
    }
}
