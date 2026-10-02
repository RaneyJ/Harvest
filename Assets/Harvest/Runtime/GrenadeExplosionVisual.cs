using UnityEngine;

namespace Harvest
{
    public sealed class GrenadeExplosionVisual : MonoBehaviour
    {
        public Renderer Shell;
        public Light Flash;
        public float Lifetime = 0.4f;
        Color color;
        float radius;
        float started;
        MaterialPropertyBlock block;
        public void Initialize(Color tint, float blastRadius)
        {
            color = tint;
            radius = blastRadius;
            started = Time.time;
            block = new MaterialPropertyBlock();
            if (Flash != null) { Flash.color = tint; Flash.range = blastRadius * 2f; }
        }
        void Update()
        {
            float t = Mathf.Clamp01((Time.time - started) / Mathf.Max(0.01f, Lifetime));
            if (t >= 1f) { Destroy(gameObject); return; }
            transform.localScale = Vector3.one * Mathf.Lerp(0.3f, radius * 2f, t);
            if (Shell != null && block != null)
            {
                Color faded = color;
                faded.a = (1f - t) * 0.35f;
                block.SetColor("_BaseColor", faded);
                block.SetColor("_EmissionColor", color * (1f - t) * 2f);
                Shell.SetPropertyBlock(block);
            }
            if (Flash != null) Flash.intensity = (1f - t) * 4f;
        }
    }
}
