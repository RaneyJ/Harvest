using UnityEngine;

namespace Harvest
{
    [RequireComponent(typeof(CharacterController), typeof(Vitality))]
    public sealed class CovenantEnemy : MonoBehaviour
    {
        MarineController target;
        HarvestEncounter encounter;
        CharacterController controller;
        EnemyBehavior behavior;
        Vitality vitality;
        float verticalVelocity;

        public Vitality Vitality => vitality;

        void Awake()
        {
            target = FindFirstObjectByType<MarineController>();
            encounter = FindFirstObjectByType<HarvestEncounter>();
            controller = GetComponent<CharacterController>();
            behavior = GetComponent<EnemyBehavior>();
            vitality = GetComponent<Vitality>();
        }

        void OnEnable() => GetComponent<Vitality>().Died += Die;
        void OnDisable() => GetComponent<Vitality>().Died -= Die;

        void Update()
        {
            if (target == null || !target.Vitality.IsAlive || behavior == null ||
                (encounter != null && encounter.IsFinished)) return;
            Vector3 delta = target.transform.position - transform.position;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (distance < 0.01f) return;
            Vector3 direction = delta / distance;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 6f);
            Vector3 horizontal = distance > behavior.PreferredRange ? direction * behavior.MoveSpeed : Vector3.zero;
            verticalVelocity = controller.isGrounded ? -2f : verticalVelocity + Physics.gravity.y * Time.deltaTime;
            controller.Move((horizontal + Vector3.up * verticalVelocity) * Time.deltaTime);

            Vector3 origin = transform.position + Vector3.up * (behavior is BruteBehavior ? 1.6f : 1.2f);
            Vector3 aim = target.View.transform.position - origin;
            bool visible = Physics.Raycast(origin, aim.normalized, out RaycastHit hit, aim.magnitude + 0.2f) &&
                hit.collider.GetComponentInParent<MarineController>() == target;
            behavior.TryAttack(this, target, distance, visible);
        }

        void Die()
        {
            encounter?.EnemyKilled(this);
            Destroy(gameObject);
        }
    }
}
