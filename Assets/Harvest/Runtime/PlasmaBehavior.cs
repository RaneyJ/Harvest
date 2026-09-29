using UnityEngine;

namespace Harvest
{
    public abstract class PlasmaBehavior : EnemyBehavior
    {
        public PlasmaBolt BoltPrefab;
        public float FireInterval = 1.8f;
        public float BoltSpeed = 17f;
        public float AttackHeight = 1.2f;
        float nextAttack;

        public override void TryAttack(CovenantEnemy self, MarineController target, float distance, bool hasLineOfSight)
        {
            if (!hasLineOfSight || Time.time < nextAttack || BoltPrefab == null) return;
            Vector3 origin = self.transform.position + Vector3.up * AttackHeight;
            Vector3 direction = (target.View.transform.position - origin).normalized;
            PlasmaBolt bolt = Instantiate(BoltPrefab, origin + direction * 0.7f, Quaternion.identity);
            bolt.Velocity = direction * BoltSpeed;
            nextAttack = Time.time + FireInterval + Random.Range(-0.2f, 0.2f);
        }
    }

}
