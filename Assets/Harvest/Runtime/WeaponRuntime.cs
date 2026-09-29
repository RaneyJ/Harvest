using System.Collections.Generic;
using UnityEngine;

namespace Harvest
{
    public enum CombatTeam { Marine, Covenant }

    // A shared firing and damage path. Input, enemy AI, and later allied marine AI call this.
    public static class WeaponRuntime
    {
        public static event System.Action<WeaponDefinition, Vector3, Vector3> HitscanFired;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetPresentation() => HitscanFired = null;
        public static bool Fire(WeaponInstance weapon, Vector3 origin, Quaternion aim, CombatTeam team, bool charged = false, Suppression shooter = null, bool aimingDownSights = false)
        {
            if (weapon == null || !weapon.TryConsumeShot(charged)) return false;
            WeaponDefinition definition = weapon.Definition;
            float damage = charged ? definition.ChargedDamage : definition.DamagePerPellet;
            float spread = Spread(definition, shooter, aimingDownSights);
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
                        float hitDamage = definition.IsPrecision ? PrecisionHitRegion.ResolveDamage(hit.collider, hit.point, damage) : damage;
                        hitOpponent |= ApplyHit(hit.collider, team, hitDamage, definition.ShieldMultiplier, origin, charged);
                    }
                    else Suppression.ObserveSegment(origin, end, team, exposures, false);
                    HitscanFired?.Invoke(definition, origin, end);
                }
            }
            if (exposures != null) Suppression.ApplyExposures(exposures);
            return hitOpponent;
        }

        public static float Spread(WeaponDefinition definition, Suppression shooter, bool aimingDownSights) =>
            definition.IsPrecision && aimingDownSights ? definition.AdsSpreadDegrees :
            definition.SpreadDegrees + (shooter != null ? shooter.AccuracyPenaltyDegrees : 0f);

        public static bool ApplyHit(Collider collider, CombatTeam sourceTeam, float damage, float shieldMultiplier, Vector3 origin, bool breaksShield = false)
        {
            return CombatDamage.Apply(collider.GetComponentInParent<CombatTarget>(), sourceTeam,
                damage, origin, shieldMultiplier, breaksShield);
        }
    }
}
