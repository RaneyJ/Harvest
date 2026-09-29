using UnityEngine;

namespace Harvest
{
    public abstract class EnemyBehavior : MonoBehaviour
    {
        public float MoveSpeed = 2.6f;
        public float PreferredRange = 12f;
        public abstract void TryAttack(CovenantEnemy self, MarineController target, float distance, bool hasLineOfSight);
    }

}
