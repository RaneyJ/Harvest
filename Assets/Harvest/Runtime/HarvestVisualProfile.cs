using UnityEngine;

namespace Harvest
{
    [CreateAssetMenu(menuName = "Harvest/Visual Profile")]
    public sealed class HarvestVisualProfile : ScriptableObject
    {
        public Color SunColor = new Color(1f, 0.82f, 0.64f);
        [Min(0f)] public float SunIntensity = 1.8f;
        public Vector3 SunRotation = new Vector3(28f, -55f, 0f);
        public Color AmbientSky = new Color(0.38f, 0.46f, 0.56f);
        public Color AmbientEquator = new Color(0.29f, 0.30f, 0.30f);
        public Color AmbientGround = new Color(0.15f, 0.13f, 0.11f);
        public Color FogColor = new Color(0.47f, 0.49f, 0.50f);
        [Range(0f, 0.03f)] public float FogDensity = 0.006f;
    }
}
