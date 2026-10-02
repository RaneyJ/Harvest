using UnityEngine;

namespace Harvest
{
    public static class GrenadeBlast
    {
        public static float Falloff(float distance, float radius) => radius <= 0f ? 0f : Mathf.Clamp01(1f - distance / radius);
        public static void Apply(GrenadeDefinition definition, Vector3 origin, CombatTeam sourceTeam, CombatTarget owner)
        {
            if (definition == null) return;
            // A snapshot tolerates deaths/unregistration during radial damage resolution.
            foreach (CombatTarget target in CombatTarget.Active.ToArray())
            {
                if (target == null || !target.IsAlive) continue;
                Vector3 point = CombatGeometry.ClosestBodyPoint(target.GetComponent<Collider>(), origin, target.AimPosition);
                float distance = Vector3.Distance(origin, point);
                if (distance > Mathf.Max(definition.DamageRadius, definition.SuppressionRadius)) continue;
                bool covered = CombatGeometry.BlastBlocked(origin, target);
                // Blast pressure affects both factions, including the thrower. Damage retains
                // friendly-fire protection for allies while allowing grenade self-damage.
                float pressure = definition.MaxPressure * Falloff(distance, definition.SuppressionRadius) *
                    (covered ? definition.CoveredPressureMultiplier : 1f);
                target.Pressure?.AddPressure(pressure);
                float damage = definition.MaxDamage * Falloff(distance, definition.DamageRadius) *
                    (covered ? definition.CoveredDamageMultiplier : 1f);
                CombatDamage.Apply(target, sourceTeam, damage, origin, definition.ShieldMultiplier, false, owner);
            }
        }
    }
}
