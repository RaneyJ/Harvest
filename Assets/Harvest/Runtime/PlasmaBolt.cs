using UnityEngine;

namespace Harvest
{
    public sealed class PlasmaBolt : MonoBehaviour
    {
        public Vector3 Velocity;
        public int Damage = 15;
        public float Lifetime = 4f;
        float expires;

        void Start() => expires = Time.time + Lifetime;

        void Update()
        {
            if (Time.time > expires) { Destroy(gameObject); return; }
            Vector3 start = transform.position;
            Vector3 step = Velocity * Time.deltaTime;
            if (Physics.Raycast(start, step.normalized, out RaycastHit hit, step.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                hit.collider.GetComponentInParent<MarineController>()?.TakeDamage(Damage);
                Destroy(gameObject);
                return;
            }
            transform.position = start + step;
        }
    }
}
