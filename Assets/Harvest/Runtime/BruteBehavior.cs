using UnityEngine;

namespace Harvest
{
    public sealed class BruteBehavior : EnemyBehavior
    {
        public float AttackInterval = 1.2f;
        public float MeleeRange = 2.9f;
        public float MeleeDamage = 42f;
        float nextAttack;

        public override void TryAttack(CovenantEnemy self, MarineController target, float distance, bool hasLineOfSight)
        {
            if (distance >= MeleeRange || !hasLineOfSight || Time.time < nextAttack) return;
            target.Vitality.ApplyDamage(MeleeDamage);
            nextAttack = Time.time + AttackInterval;
        }
    }
}
