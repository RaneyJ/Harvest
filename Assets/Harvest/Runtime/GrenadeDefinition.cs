using UnityEngine;

namespace Harvest
{
    public enum GrenadeKind { Frag, Plasma }

    [CreateAssetMenu(menuName = "Harvest/Grenade")]
    public sealed class GrenadeDefinition : ScriptableObject
    {
        public GrenadeKind Kind;
        public GrenadeProjectile Prefab;
        public GrenadeExplosionVisual ExplosionPrefab;
        [Min(0.1f)] public float FuseSeconds = 2.5f;
        [Min(0.1f)] public float StickyFuseSeconds = 2f;
        [Min(0f)] public float ThrowSpeed = 14f;
        [Min(0f)] public float UpwardBoost = 2.5f;
        [Min(0f)] public float MaxDamage = 180f;
        [Min(0.1f)] public float DamageRadius = 6f;
        [Min(0f)] public float ShieldMultiplier = 1f;
        [Min(0.1f)] public float SuppressionRadius = 12f;
        [Min(0f)] public float MaxPressure = 1.4f;
        [Range(0f, 1f)] public float CoveredDamageMultiplier = 0.25f;
        [Range(0f, 1f)] public float CoveredPressureMultiplier = 0.5f;
        public Color BlastColor = new Color(1f, 0.55f, 0.12f);
    }
}
