using System;
using UnityEngine;

namespace Harvest
{
    [RequireComponent(typeof(CombatTarget))]
    public sealed class GrenadeInventory : MonoBehaviour
    {
        public const int CapacityPerType = 2;
        public GrenadeDefinition Frag;
        public GrenadeDefinition Plasma;
        public GrenadeKind Selected { get; private set; }
        public event Action Changed;
        readonly int[] counts = new int[2];
        CombatTarget identity;
        float nextThrow;

        void Awake() { identity = GetComponent<CombatTarget>(); ResetInventory(); }
        public int Count(GrenadeKind kind) => counts[(int)kind];
        public void ResetInventory()
        {
            counts[0] = counts[1] = CapacityPerType;
            Selected = GrenadeKind.Frag;
            nextThrow = 0f;
            Changed?.Invoke();
        }
        public void SelectNext()
        {
            Selected = Selected == GrenadeKind.Frag ? GrenadeKind.Plasma : GrenadeKind.Frag;
            Changed?.Invoke();
        }
        public int Refill(GrenadeKind kind)
        {
            int restored = CapacityPerType - counts[(int)kind];
            if (restored <= 0) return 0;
            counts[(int)kind] = CapacityPerType;
            Changed?.Invoke();
            return restored;
        }
        public bool TryThrow(Vector3 origin, Vector3 forward)
        {
            GrenadeDefinition definition = Selected == GrenadeKind.Frag ? Frag : Plasma;
            if (!identity.IsAlive || Time.time < nextThrow || Count(Selected) <= 0 ||
                definition == null || definition.Prefab == null || forward.sqrMagnitude < 0.001f) return false;
            forward.Normalize();
            Vector3 position = origin + forward * 0.55f;
            if (Physics.SphereCast(origin, 0.12f, forward, out RaycastHit hit, 0.55f, ~0, QueryTriggerInteraction.Ignore))
                position = origin + forward * Mathf.Max(0f, hit.distance - 0.02f);
            GrenadeProjectile grenade = Instantiate(definition.Prefab, position, Quaternion.identity);
            grenade.Launch(definition, identity, forward * definition.ThrowSpeed + Vector3.up * definition.UpwardBoost);
            counts[(int)Selected]--;
            nextThrow = Time.time + 0.7f;
            Changed?.Invoke();
            return true;
        }
    }
}
