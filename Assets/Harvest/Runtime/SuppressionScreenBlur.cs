using UnityEngine;

namespace Harvest
{
    // Camera-local state, consumed by the URP renderer feature. No gameplay in the render pass.
    [RequireComponent(typeof(Camera))]
    public sealed class SuppressionScreenBlur : MonoBehaviour
    {
        public Suppression State;
        public Shader BlurShader;
        // Retained for existing scene serialization; URP uses a single full-resolution pass.
        [HideInInspector] public int Downsample = 2;
        [Range(0f, 12f)] public float MaxBlurRadius = 2.5f;
        [Range(0f, 0.45f)] public float MaxBlend = 0.45f;
        [Min(0.1f)] public float FollowSpeed = 8f;
        public float VisiblePressure { get; private set; }
        public int LastScheduledRenderFrame { get; internal set; } = -1;
        [HideInInspector] public bool DiagnosticCopyOnly;
        void LateUpdate()
        {
            float target = State != null ? State.EffectStrength : 0f;
            if (target <= 0f) { VisiblePressure = 0f; return; }
            VisiblePressure = Mathf.Lerp(VisiblePressure, target, 1f - Mathf.Exp(-FollowSpeed * Time.deltaTime));
        }
        void OnDisable() => VisiblePressure = 0f;
    }
}
