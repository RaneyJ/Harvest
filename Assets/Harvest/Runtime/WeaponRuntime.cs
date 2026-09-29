using System.Collections.Generic;
using UnityEngine;

namespace Harvest
{
    public enum CombatTeam { Marine, Covenant }

    // A shared firing and damage path. Input, enemy AI, and later allied marine AI call this.
    public static class WeaponRuntime
    {
        public static bool Fire(WeaponInstance weapon, Vector3 origin, Quaternion aim, CombatTeam team, bool charged = false, Suppression shooter = null)
        {
            if (weapon == null || !weapon.TryConsumeShot(charged)) return false;
            WeaponDefinition definition = weapon.Definition;
            float damage = charged ? definition.ChargedDamage : definition.DamagePerPellet;
            float spread = definition.SpreadDegrees + (shooter != null ? shooter.AccuracyPenaltyDegrees : 0f);
            var exposures = definition.ShotKind == WeaponShotKind.Hitscan ? new Dictionary<Suppression, float>() : null;
            bool hitOpponent = false;
            for (int i = 0; i < definition.Pellets; i++)
            {
                Vector3 direction = aim * (Quaternion.Euler(
                    Random.Range(-spread, spread),
                    Random.Range(-spread, spread), 0f) * Vector3.forward);
                if (definition.ShotKind == WeaponShotKind.PlasmaBolt)
                {
                    if (definition.ProjectilePrefab == null) continue;
                    PlasmaBolt bolt = Object.Instantiate(definition.ProjectilePrefab, origin + direction * 0.7f, Quaternion.identity);
                    bolt.Initialize(direction * definition.ProjectileSpeed, team,
                        damage, definition.ShieldMultiplier, charged);
                    if (charged) bolt.transform.localScale *= 1.7f;
                }
                else
                {
                    Vector3 end = origin + direction * definition.Range;
                    if (Physics.Raycast(origin, direction, out RaycastHit hit, definition.Range, ~0, QueryTriggerInteraction.Ignore))
                    {
                        end = hit.point;
                        // Observe before damage so a lethal impact still uses the living target's body.
                        Suppression.ObserveSegment(origin, end, team, exposures, false);
                        hitOpponent |= ApplyHit(hit.collider, team, damage, definition.ShieldMultiplier, origin, charged);
                    }
                    else Suppression.ObserveSegment(origin, end, team, exposures, false);
                }
            }
            if (exposures != null) Suppression.ApplyExposures(exposures);
            return hitOpponent;
        }

        public static bool ApplyHit(Collider collider, CombatTeam sourceTeam, float damage, float shieldMultiplier, Vector3 origin, bool breaksShield = false)
        {
            return CombatDamage.Apply(collider.GetComponentInParent<CombatTarget>(), sourceTeam,
                damage, origin, shieldMultiplier, breaksShield);
        }
    }
}
