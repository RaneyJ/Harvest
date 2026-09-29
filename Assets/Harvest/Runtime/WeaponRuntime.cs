using UnityEngine;

namespace Harvest
{
    public enum CombatTeam { Marine, Covenant }

    // A shared firing and damage path. Input, enemy AI, and later allied marine AI call this.
    public static class WeaponRuntime
    {
        public static bool Fire(WeaponInstance weapon, Vector3 origin, Quaternion aim, CombatTeam team)
        {
            if (weapon == null || !weapon.TryConsumeShot()) return false;
            WeaponDefinition definition = weapon.Definition;
            bool hitOpponent = false;
            for (int i = 0; i < definition.Pellets; i++)
            {
                Vector3 direction = aim * (Quaternion.Euler(
                    Random.Range(-definition.SpreadDegrees, definition.SpreadDegrees),
                    Random.Range(-definition.SpreadDegrees, definition.SpreadDegrees), 0f) * Vector3.forward);
                if (definition.ShotKind == WeaponShotKind.PlasmaBolt)
                {
                    if (definition.ProjectilePrefab == null) continue;
                    PlasmaBolt bolt = Object.Instantiate(definition.ProjectilePrefab, origin + direction * 0.7f, Quaternion.identity);
                    bolt.Initialize(direction * definition.ProjectileSpeed, team,
                        definition.DamagePerPellet, definition.ShieldMultiplier);
                }
                else if (Physics.Raycast(origin, direction, out RaycastHit hit, definition.Range, ~0, QueryTriggerInteraction.Ignore))
                {
                    hitOpponent |= ApplyHit(hit.collider, team, definition.DamagePerPellet,
                        definition.ShieldMultiplier, origin);
                }
            }
            return hitOpponent;
        }

        public static bool ApplyHit(Collider collider, CombatTeam sourceTeam, float damage, float shieldMultiplier, Vector3 origin)
        {
            CovenantEnemy enemy = collider.GetComponentInParent<CovenantEnemy>();
            if (enemy != null)
            {
                if (sourceTeam == CombatTeam.Covenant) return false;
                enemy.ReceiveWeaponHit(damage, shieldMultiplier, origin);
                return true;
            }
            MarineArmor marine = collider.GetComponentInParent<MarineArmor>();
            if (marine != null)
            {
                if (sourceTeam == CombatTeam.Marine) return false;
                marine.ApplyDamage(damage);
                return true;
            }
            return false;
        }
    }
}
