using UnityEngine;

namespace Harvest
{
    [RequireComponent(typeof(CharacterController), typeof(Vitality))]
    public sealed class CovenantEnemy : MonoBehaviour
    {
        CombatTarget target;
        float nextTargetSearch;
        HarvestEncounter encounter;
        CharacterController controller;
        EnemyBehavior behavior;
        Vitality vitality;
        float verticalVelocity;

        public Vitality Vitality => vitality;

        void Awake()
        {
            encounter = FindFirstObjectByType<HarvestEncounter>();
            controller = GetComponent<CharacterController>();
            behavior = GetComponent<EnemyBehavior>();
            vitality = GetComponent<Vitality>();
        }

        void OnEnable() => GetComponent<Vitality>().Died += Die;
        void OnDisable() => GetComponent<Vitality>().Died -= Die;

        void Update()
        {
            if (behavior == null || (encounter != null && encounter.IsFinished)) return;
            bool committed = behavior is BruteBehavior brute && brute.IsCommitted;
            if ((!committed && Time.time >= nextTargetSearch) || target == null || !target.IsAlive)
            {
                nextTargetSearch = Time.time + 0.6f;
                target = CombatTarget.FindOpponent(CombatTeam.Covenant,
                    transform.position + Vector3.up * 0.5f, 100f);
            }
            if (target == null) return;
            Vector3 delta = target.transform.position - transform.position;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (distance < 0.01f) return;
            Vector3 direction = delta / distance;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 6f);
            Vector3 horizontal = behavior.DesiredMovement(direction, distance);
            verticalVelocity = controller.isGrounded ? -2f : verticalVelocity + Physics.gravity.y * Time.deltaTime;
            controller.Move((horizontal + Vector3.up * verticalVelocity) * Time.deltaTime);

            Vector3 origin = transform.position + Vector3.up * (behavior is BruteBehavior ? 1.6f : 1.2f);
            bool visible = target.CanSeeFrom(origin);
            behavior.TryAttack(this, target, distance, visible);
        }

        public void ReceiveWeaponHit(float amount, float shieldMultiplier, Vector3 shotOrigin, bool breaksShield = false)
        {
            JackalBehavior jackal = behavior as JackalBehavior;
            bool flank = jackal != null && jackal.IsFlanked(shotOrigin);
            vitality.ApplyDamage(amount, shieldMultiplier, flank, breaksShield);
        }

        void Die()
        {
            GetComponent<ActorWeapon>()?.Drop();
            encounter?.EnemyKilled(this);
            Destroy(gameObject);
        }
    }
}
