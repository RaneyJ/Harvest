using System.Collections.Generic;
using UnityEngine;

namespace Harvest
{
    // Shared target identity for player, allies, and enemies. No input or behavior lives here.
    [RequireComponent(typeof(Vitality))]
    public sealed class CombatTarget : MonoBehaviour
    {
        public CombatTeam Team;
        public Vector3 AimOffset = new Vector3(0f, 0.45f, 0f);
        public static readonly List<CombatTarget> Active = new List<CombatTarget>();
        Suppression pressure;
        public Suppression Pressure => pressure != null ? pressure : (pressure = GetComponent<Suppression>());
        Vitality vitality;
        MarineController player;

        public bool IsAlive => vitality != null && vitality.IsAlive && isActiveAndEnabled;
        public Vector3 AimPosition => player != null && player.View != null ?
            player.View.transform.position : transform.position + AimOffset;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRegistry() => Active.Clear();
        void Awake() { vitality = GetComponent<Vitality>(); player = GetComponent<MarineController>(); pressure = GetComponent<Suppression>(); }
        void OnEnable() { if (!Active.Contains(this)) Active.Add(this); }
        void OnDisable() => Active.Remove(this);

        public bool CanSeeFrom(Vector3 origin)
        {
            Vector3 delta = AimPosition - origin;
            return Physics.Raycast(origin, delta.normalized, out RaycastHit hit, delta.magnitude + 0.1f,
                ~0, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<CombatTarget>() == this;
        }

        public static CombatTarget FindOpponent(CombatTeam team, Vector3 origin, float range)
        {
            CombatTarget best = null;
            float bestScore = float.MaxValue;
            foreach (CombatTarget candidate in Active)
            {
                if (candidate == null || candidate.Team == team || !candidate.IsAlive) continue;
                float distance = (candidate.AimPosition - origin).sqrMagnitude;
                if (distance > range * range) continue;
                // Visible threats take priority, but obscured enemies can still guide cover selection.
                float score = distance * (candidate.CanSeeFrom(origin) ? 1f : 4f);
                if (score < bestScore) { best = candidate; bestScore = score; }
            }
            return best;
        }
    }
}
