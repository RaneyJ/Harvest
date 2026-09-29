using System.Collections.Generic;
using UnityEngine;

namespace Harvest
{
    // Per-combatant pressure, independent of health, input, AI, and presentation.
    [RequireComponent(typeof(CombatTarget))]
    public sealed class Suppression : MonoBehaviour
    {
        [Min(0.1f)] public float NearMissRadius = 2.5f;
        [Range(0f, 1f)] public float PressurePerShot = 0.16f;
        [Range(0f, 1f)] public float CoveredMultiplier = 0.25f;
        [Min(0f)] public float RecoveryDelay = 1.5f;
        [Min(0f)] public float RecoveryPerSecond = 0.25f;
        [Min(0f)] public float MaxAccuracyPenaltyDegrees = 5f;
        public float Value { get; private set; }
        public float AccuracyPenaltyDegrees => Value * MaxAccuracyPenaltyDegrees;

        CombatTarget identity;
        Collider body;
        float lastThreatTime;

        void Awake() { identity = GetComponent<CombatTarget>(); body = GetComponent<Collider>(); }
        public void ResetPressure() { Value = 0f; lastThreatTime = float.NegativeInfinity; }
        public void AddPressure(float amount)
        {
            if (!identity.IsAlive || !isActiveAndEnabled || amount <= 0f) return;
            Value = Mathf.Clamp01(Value + amount);
            lastThreatTime = Time.time;
        }
        void Update()
        {
            if (!identity.IsAlive) { ResetPressure(); return; }
            if (Time.time - lastThreatTime >= RecoveryDelay)
                Value = Mathf.MoveTowards(Value, 0f, RecoveryPerSecond * Time.deltaTime);
        }

        // Accumulate the strongest exposure per target for this shot. Hitscan pellets share a
        // dictionary; a moving projectile retains one, so frame rate cannot multiply pressure.
        public static void ObserveSegment(Vector3 start, Vector3 end, CombatTeam sourceTeam,
            Dictionary<Suppression, float> exposures, bool applyImmediately)
        {
            foreach (CombatTarget candidate in CombatTarget.Active)
            {
                if (candidate == null || !candidate.IsAlive || candidate.Team == sourceTeam) continue;
                Suppression pressure = candidate.Pressure;
                if (pressure == null || !pressure.isActiveAndEnabled) continue;
                float amount = pressure.Exposure(start, end);
                exposures.TryGetValue(pressure, out float previous);
                if (amount <= previous) continue;
                exposures[pressure] = amount;
                if (applyImmediately) pressure.AddPressure(amount - previous);
            }
        }
        public static void ApplyExposures(Dictionary<Suppression, float> exposures)
        {
            foreach (KeyValuePair<Suppression, float> entry in exposures)
                if (entry.Key != null) entry.Key.AddPressure(entry.Value);
        }
        float Exposure(Vector3 start, Vector3 end)
        {
            Vector3 onShot = ClosestOnSegment(start, end, identity.AimPosition);
            Vector3 onBody = ClosestBodyPoint(onShot);
            onShot = ClosestOnSegment(start, end, onBody);
            float distance = Vector3.Distance(onShot, onBody);
            if (distance > NearMissRadius) return 0f;
            float proximity = 1f - distance / Mathf.Max(0.1f, NearMissRadius);
            float amount = PressurePerShot * Mathf.Lerp(0.25f, 1f, proximity);
            // A nearby impact on cover still creates pressure, but solid geometry attenuates it.
            // Cast from the receiver so an impact exactly on a wall cannot start the ray
            // inside that wall and accidentally bypass its cover attenuation.
            Vector3 towardShot = onShot - identity.AimPosition;
            if (towardShot.sqrMagnitude > 0.001f && Physics.Raycast(identity.AimPosition,
                towardShot.normalized, out RaycastHit hit, towardShot.magnitude, ~0, QueryTriggerInteraction.Ignore) &&
                hit.collider.GetComponentInParent<CombatTarget>() != identity)
                amount *= CoveredMultiplier;
            return amount;
        }
        Vector3 ClosestBodyPoint(Vector3 point)
        {
            // CharacterController is not one of Physics.ClosestPoint's supported collider
            // shapes. Use its capsule directly, including the player's current crouch height.
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
            return body != null ? body.ClosestPoint(point) : identity.AimPosition;
        }
        public static Vector3 ClosestOnSegment(Vector3 start, Vector3 end, Vector3 point)
        {
            Vector3 segment = end - start;
            if (segment.sqrMagnitude < 0.000001f) return start;
            float t = Mathf.Clamp01(Vector3.Dot(point - start, segment) / segment.sqrMagnitude);
            return start + segment * t;
        }
    }
}
