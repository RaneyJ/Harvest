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
        [Range(0f, 0.95f)] public float EffectThreshold = 0.5f;
        [Range(0f, 1f)] public float ArmoredPressureMultiplier = 0.6f;
        [Min(1f)] public float ExposedPressureMultiplier = 1.25f;
        public float Value { get; private set; }
        // Pressure can accumulate silently. All effects share this remapped strength.
        public float EffectStrength => Value <= EffectThreshold ? 0f : Mathf.SmoothStep(0f, 1f,
            Mathf.Clamp01((Value - EffectThreshold) / Mathf.Max(0.01f, 1f - EffectThreshold)));
        public float AccuracyPenaltyDegrees => EffectStrength * MaxAccuracyPenaltyDegrees;

        CombatTarget identity;
        Collider body;
        MarineArmor armor;
        float lastThreatTime;

        void Awake() { identity = GetComponent<CombatTarget>(); body = GetComponent<Collider>(); armor = GetComponent<MarineArmor>(); }
        public void ResetPressure() { Value = 0f; lastThreatTime = float.NegativeInfinity; }
        public void AddPressure(float amount)
        {
            if (!identity.IsAlive || !isActiveAndEnabled || amount <= 0f) return;
            if (armor == null) armor = GetComponent<MarineArmor>();
            float multiplier = armor == null ? 1f : armor.Armor > 0f ? ArmoredPressureMultiplier : ExposedPressureMultiplier;
            Value = Mathf.Clamp01(Value + amount * multiplier);
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
        Vector3 ClosestBodyPoint(Vector3 point) => CombatGeometry.ClosestBodyPoint(body, point, identity.AimPosition);
        public static Vector3 ClosestOnSegment(Vector3 start, Vector3 end, Vector3 point) =>
            CombatGeometry.ClosestOnSegment(start, end, point);
    }
}
