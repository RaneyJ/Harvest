using UnityEngine;

namespace Harvest
{
    // Weapons, melee, and explosions share faction rules and armor/shield resolution.
    public static class CombatDamage
    {
        public const float CovenantDamageMultiplier = 1.15f;

        public static bool Apply(CombatTarget target, CombatTeam sourceTeam, float amount, Vector3 origin,
            float shieldMultiplier = 1f, bool breaksShield = false, CombatTarget selfDamageOwner = null)
        {
            if (target == null || !target.IsAlive || amount <= 0f ||
                (target.Team == sourceTeam && target != selfDamageOwner)) return false;
            if (sourceTeam == CombatTeam.Covenant) amount *= CovenantDamageMultiplier;
            CovenantEnemy enemy = target.GetComponent<CovenantEnemy>();
            if (enemy != null) enemy.ReceiveWeaponHit(amount, shieldMultiplier, origin, breaksShield);
            else if (target.GetComponent<MarineArmor>() is MarineArmor armor) armor.ApplyDamage(amount, origin);
            else target.GetComponent<Vitality>().ApplyDamage(amount, shieldMultiplier, false, breaksShield);
            return true;
        }
    }
}
