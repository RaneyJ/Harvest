using UnityEngine;

namespace Harvest
{
    // Built-in pipeline image effect. The shader is serialized so player builds retain it.
    [RequireComponent(typeof(Camera))]
    public sealed class SuppressionScreenBlur : MonoBehaviour
    {
        public Suppression State;
        public Shader BlurShader;
        [Range(1, 4)] public int Downsample = 2;
        [Range(0f, 12f)] public float MaxBlurRadius = 5f;
        [Range(0f, 1f)] public float MaxBlend = 0.85f;
        [Min(0.1f)] public float FollowSpeed = 8f;
        Material material;
        float visiblePressure;

        void OnEnable()
        {
            if (BlurShader != null && BlurShader.isSupported)
                material = new Material(BlurShader) { hideFlags = HideFlags.HideAndDontSave };
            else Debug.LogWarning("Suppression blur needs its supported shader. Rebuild The Line scene.", this);
        }
        void LateUpdate()
        {
            float target = State != null ? State.Value : 0f;
            visiblePressure = Mathf.Lerp(visiblePressure, target, 1f - Mathf.Exp(-FollowSpeed * Time.deltaTime));
        }
        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (material == null || visiblePressure < 0.001f || MaxBlend <= 0f || MaxBlurRadius <= 0f)
            {
                Graphics.Blit(source, destination);
                return;
            }
            int width = Mathf.Max(1, source.width / Mathf.Max(1, Downsample));
            int height = Mathf.Max(1, source.height / Mathf.Max(1, Downsample));
            RenderTexture horizontal = RenderTexture.GetTemporary(width, height, 0, source.format);
            RenderTexture vertical = RenderTexture.GetTemporary(width, height, 0, source.format);
            horizontal.filterMode = vertical.filterMode = FilterMode.Bilinear;
            horizontal.wrapMode = vertical.wrapMode = TextureWrapMode.Clamp;
            try
            {
                float radius = visiblePressure * MaxBlurRadius / 3.230769f;
                material.SetVector("_BlurStep", new Vector4(radius / width, 0f, 0f, 0f));
                Graphics.Blit(source, horizontal, material, 0);
                material.SetVector("_BlurStep", new Vector4(0f, radius / height, 0f, 0f));
                Graphics.Blit(horizontal, vertical, material, 0);
                material.SetTexture("_BlurredTex", vertical);
                material.SetFloat("_Blend", visiblePressure * MaxBlend);
                Graphics.Blit(source, destination, material, 1);
            }
            finally
            {
                RenderTexture.ReleaseTemporary(horizontal);
                RenderTexture.ReleaseTemporary(vertical);
            }
        }
        void OnDisable()
        {
            visiblePressure = 0f;
            if (material != null)
            {
                if (Application.isPlaying) Destroy(material);
                else DestroyImmediate(material);
                material = null;
            }
        }
    }
}
