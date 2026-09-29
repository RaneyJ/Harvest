using UnityEngine;

namespace Harvest
{
    public abstract class PlasmaBehavior : EnemyBehavior
    {
        public float AttackHeight = 1.2f;

        public override void TryAttack(CovenantEnemy self, CombatTarget target, float distance, bool hasLineOfSight)
        {
            if (!hasLineOfSight) return;
            Vector3 origin = self.transform.position + Vector3.up * AttackHeight;
            self.GetComponent<ActorWeapon>()?.TryFire(origin, (target.AimPosition - origin).normalized);
        }
    }
}
