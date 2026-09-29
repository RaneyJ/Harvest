using UnityEngine;

namespace Harvest
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class GrenadeProjectile : MonoBehaviour
    {
        public GrenadeDefinition Definition;
        public bool IsStuck { get; private set; }
        Rigidbody body;
        SphereCollider shell;
        CombatTarget owner;
        CombatTeam team;
        Transform anchor;
        Vector3 localAttachment;
        float detonatesAt;
        bool launched;
        bool exploded;

        void Awake() { body = GetComponent<Rigidbody>(); shell = GetComponent<SphereCollider>(); }
        public void Launch(GrenadeDefinition definition, CombatTarget thrower, Vector3 velocity)
        {
            Definition = definition;
            owner = thrower;
            team = thrower != null ? thrower.Team : CombatTeam.Marine;
            launched = true;
            detonatesAt = Time.time + definition.FuseSeconds;
            body.linearVelocity = velocity;
            body.angularVelocity = Random.insideUnitSphere * 8f;
            if (owner != null)
                foreach (Collider collider in owner.GetComponentsInChildren<Collider>())
                    Physics.IgnoreCollision(shell, collider);
        }
        void Update()
        {
            if (!launched || exploded) return;
            if (IsStuck)
            {
                if (anchor != null) transform.position = anchor.TransformPoint(localAttachment);
                else
                {
                    // Do not parent to a victim: its death must not delete the armed grenade.
                    IsStuck = false;
                    body.isKinematic = false;
                    body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    shell.enabled = true;
                }
            }
            if (Time.time >= detonatesAt) Explode();
        }
        void FixedUpdate()
        {
            if (!launched || exploded || IsStuck || Definition.Kind != GrenadeKind.Plasma) return;
            float radius = shell.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
            foreach (Collider collider in Physics.OverlapSphere(body.position, radius + 0.015f, ~0, QueryTriggerInteraction.Ignore))
            {
                CombatTarget target = collider.GetComponentInParent<CombatTarget>();
                if (target != null && target != owner && target.IsAlive)
                {
                    Vector3 contact = CombatGeometry.ClosestBodyPoint(collider, body.position, target.AimPosition);
                    Stick(collider.transform, contact);
                    return;
                }
            }
            // A swept contact check also catches CharacterControllers that do not reliably
            // issue Rigidbody collision callbacks, and stops at intervening solid geometry.
            Vector3 step = body.linearVelocity * Time.fixedDeltaTime + Physics.gravity * (Time.fixedDeltaTime * Time.fixedDeltaTime * 0.5f);
            if (step.sqrMagnitude < 0.000001f) return;
            RaycastHit[] hits = Physics.SphereCastAll(body.position, radius, step.normalized, step.magnitude,
                ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == shell || (owner != null && hit.collider.GetComponentInParent<CombatTarget>() == owner)) continue;
                CombatTarget target = hit.collider.GetComponentInParent<CombatTarget>();
                if (target != null && target.IsAlive) Stick(hit.collider.transform, hit.point + hit.normal * radius);
                break;
            }
        }
        void OnCollisionEnter(Collision collision)
        {
            if (!launched || exploded || IsStuck || Definition.Kind != GrenadeKind.Plasma) return;
            CombatTarget target = collision.collider.GetComponentInParent<CombatTarget>();
            if (target == null || target == owner || !target.IsAlive) return;
            Stick(collision.collider.transform, transform.position);
        }
        void Stick(Transform victim, Vector3 position)
        {
            IsStuck = true;
            anchor = victim;
            transform.position = position;
            localAttachment = victim.InverseTransformPoint(position);
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            body.isKinematic = true;
            shell.enabled = false;
            detonatesAt = Time.time + Definition.StickyFuseSeconds;
        }
        void Explode()
        {
            if (exploded) return;
            exploded = true;
            shell.enabled = false;
            GrenadeBlast.Apply(Definition, transform.position, team, owner);
            if (Definition.ExplosionPrefab != null)
            {
                GrenadeExplosionVisual visual = Instantiate(Definition.ExplosionPrefab, transform.position, Quaternion.identity);
                visual.Initialize(Definition.BlastColor, Definition.DamageRadius);
            }
            Destroy(gameObject);
        }
    }
}
