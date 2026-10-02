using UnityEngine;

namespace Harvest
{
    // Graybox capsule head region. Replace its geometry with authored hit zones when rigged models arrive.
    [RequireComponent(typeof(CombatTarget))]
    public sealed class PrecisionHitRegion : MonoBehaviour
    {
        [Range(0f, 1f)] public float HeadStart = 0.76f;
        public bool AllowsInstantHeadshot = true;
        public bool IsHead(Vector3 point)
        {
            Collider body = GetComponent<Collider>();
            if (body == null) return false;
            Bounds bounds = body.bounds;
            return point.y >= Mathf.Lerp(bounds.min.y, bounds.max.y, HeadStart) && point.y <= bounds.max.y + 0.01f;
        }
        public static float ResolveDamage(Collider collider, Vector3 point, float damage)
        {
            if (damage <= 0f) return damage;
            CombatTarget target = collider.GetComponentInParent<CombatTarget>();
            if (target == null || target.Team != CombatTeam.Covenant || !target.IsAlive) return damage;
            PrecisionHitRegion region = target.GetComponent<PrecisionHitRegion>();
            Vitality vitality = target.GetComponent<Vitality>();
            if (region == null || !region.AllowsInstantHeadshot || target.GetComponent<BruteBehavior>() != null ||
                vitality.Shield > 0f || !region.IsHead(point)) return damage;
            return Mathf.Max(damage, vitality.Health + 1f);
        }
    }
}
