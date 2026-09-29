using System;
using UnityEngine;

namespace Harvest
{
    [RequireComponent(typeof(CombatTarget))]
    public sealed class MeleeAttack : MonoBehaviour
    {
        [Min(0f)] public float Damage = 60f;
        [Min(0.1f)] public float Reach = 2.3f;
        [Range(0f, 90f)] public float HalfAngle = 55f;
        [Min(0.1f)] public float Cooldown = 0.65f;
        public event Action Swing;
        public float NextSwing { get; private set; }
        CombatTarget identity;

        void Awake() => identity = GetComponent<CombatTarget>();
        public bool TrySwing(Vector3 origin, Vector3 forward, out bool hitEnemy)
        {
            hitEnemy = false;
            if (!identity.IsAlive || Time.time < NextSwing || forward.sqrMagnitude < 0.001f) return false;
            NextSwing = Time.time + Cooldown;
            Swing?.Invoke();
            CombatTarget best = null;
            float nearest = float.MaxValue;
            foreach (CombatTarget target in CombatTarget.Active)
            {
                if (target == null || !target.IsAlive || target.Team == identity.Team) continue;
                Vector3 point = CombatGeometry.ClosestBodyPoint(target.GetComponent<Collider>(), origin, target.AimPosition);
                float distance = Vector3.Distance(origin, point);
                Vector3 toward = target.AimPosition - origin;
                if (distance > Reach || distance >= nearest ||
                    Vector3.Dot(forward.normalized, toward.normalized) < Mathf.Cos(HalfAngle * Mathf.Deg2Rad) ||
                    !target.CanSeeFrom(origin)) continue;
                best = target;
                nearest = distance;
            }
            hitEnemy = CombatDamage.Apply(best, identity.Team, Damage, origin);
            return true;
        }
    }
}
