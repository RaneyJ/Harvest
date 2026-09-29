using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Harvest
{
    // Authored cover slots keep squad members from crowding the same firing position.
    public sealed class CoverPoint : MonoBehaviour
    {
        public Vector3 PeekOffset = new Vector3(2.5f, 0f, 0f);
        public static readonly List<CoverPoint> Active = new List<CoverPoint>();
        AlliedMarine owner;
        public Vector3 PeekPosition => transform.TransformPoint(PeekOffset);
        public bool IsAvailableFor(AlliedMarine marine) => owner == null || owner == marine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRegistry() => Active.Clear();
        void OnEnable() { if (!Active.Contains(this)) Active.Add(this); }
        void OnDisable() { Active.Remove(this); owner = null; }
        public bool TryClaim(AlliedMarine marine)
        {
            if (!IsAvailableFor(marine)) return false;
            owner = marine;
            return true;
        }
        public void Release(AlliedMarine marine) { if (owner == marine) owner = null; }

        public bool ProtectsFrom(CombatTarget threat)
        {
            if (threat == null) return true;
            Vector3 hide = transform.position + Vector3.up * 0.75f;
            Vector3 delta = threat.AimPosition - hide;
            return Physics.Raycast(hide, delta.normalized, out RaycastHit hit, delta.magnitude,
                ~0, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<CombatTarget>() == null;
        }

        public bool HasCompletePath(Vector3 from)
        {
            var path = new NavMeshPath();
            return NavMesh.SamplePosition(transform.position, out NavMeshHit hide, 1f, NavMesh.AllAreas) &&
                NavMesh.SamplePosition(PeekPosition, out NavMeshHit peek, 1f, NavMesh.AllAreas) &&
                NavMesh.CalculatePath(from, hide.position, NavMesh.AllAreas, path) &&
                path.status == NavMeshPathStatus.PathComplete &&
                NavMesh.CalculatePath(hide.position, peek.position, NavMesh.AllAreas, path) &&
                path.status == NavMeshPathStatus.PathComplete;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.75f, 0.3f);
            Gizmos.DrawLine(transform.position, PeekPosition);
            Gizmos.DrawWireSphere(PeekPosition + Vector3.up * 1.35f, 0.3f);
        }
    }
}
