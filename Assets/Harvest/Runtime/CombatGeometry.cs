using UnityEngine;

namespace Harvest
{
    public static class CombatGeometry
    {
        public static Vector3 ClosestOnSegment(Vector3 start, Vector3 end, Vector3 point)
        {
            Vector3 segment = end - start;
            if (segment.sqrMagnitude < 0.000001f) return start;
            return start + segment * Mathf.Clamp01(Vector3.Dot(point - start, segment) / segment.sqrMagnitude);
        }
        public static Vector3 ClosestBodyPoint(Collider body, Vector3 point, Vector3 fallback)
        {
            if (body is CharacterController controller)
            {
                Vector3 scale = controller.transform.lossyScale;
                float radius = controller.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
                Vector3 center = controller.transform.TransformPoint(controller.center);
                float halfAxis = Mathf.Max(0f, controller.height * Mathf.Abs(scale.y) * 0.5f - radius);
                Vector3 axis = ClosestOnSegment(center - controller.transform.up * halfAxis,
                    center + controller.transform.up * halfAxis, point);
                Vector3 delta = point - axis;
                return delta.sqrMagnitude <= radius * radius ? point : axis + delta.normalized * radius;
            }
            return body != null ? body.ClosestPoint(point) : fallback;
        }
        public static bool BlastBlocked(Vector3 origin, CombatTarget target)
        {
            Vector3 delta = origin - target.AimPosition;
            foreach (RaycastHit hit in Physics.RaycastAll(target.AimPosition, delta.normalized,
                delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                // Bodies do not substitute for solid cover against a radial explosion.
                if (hit.collider.GetComponentInParent<CombatTarget>() == null &&
                    hit.collider.GetComponentInParent<GrenadeProjectile>() == null) return true;
            }
            return false;
        }
    }
}
