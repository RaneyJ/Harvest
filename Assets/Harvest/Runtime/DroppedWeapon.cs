using UnityEngine;

namespace Harvest
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class DroppedWeapon : MonoBehaviour
    {
        public Renderer Visual;
        public WeaponInstance Weapon { get; private set; }
        float availableAt;
        GameObject detailedModel;
        public bool IsAvailable => Weapon != null && Time.time >= availableAt;

        public static DroppedWeapon Spawn(DroppedWeapon prefab, WeaponInstance instance, Vector3 position, Transform owner = null)
        {
            DroppedWeapon dropped = Instantiate(prefab, SurfacePosition(position, owner), Quaternion.identity);
            dropped.Initialize(instance);
            return dropped;
        }

        public void Initialize(WeaponInstance instance)
        {
            Weapon = instance;
            if (detailedModel != null) Destroy(detailedModel);
            bool hasModel = instance.Definition.WorldModel != null;
            if (Visual != null) Visual.gameObject.SetActive(!hasModel);
            if (hasModel)
            {
                detailedModel = Instantiate(instance.Definition.WorldModel, transform, false);
                detailedModel.transform.localRotation = Quaternion.Euler(0f, 25f, 90f);
                detailedModel.transform.localScale = Vector3.one * 0.75f;
            }
            availableAt = Time.time + 0.35f;
            if (Visual != null && instance.Definition.PickupMaterial != null)
                Visual.sharedMaterial = instance.Definition.PickupMaterial;
            gameObject.name = instance.Definition.DisplayName + " drop";
        }

        public WeaponInstance Take()
        {
            if (!IsAvailable) return null;
            WeaponInstance taken = Weapon;
            Weapon = null;
            Destroy(gameObject);
            return taken;
        }

        public static Vector3 SurfacePosition(Vector3 desired, Transform owner = null)
        {
            if (owner != null)
            {
                Vector3 delta = desired - owner.position;
                RaycastHit[] lane = Physics.RaycastAll(owner.position, delta.normalized, delta.magnitude,
                    ~0, QueryTriggerInteraction.Ignore);
                System.Array.Sort(lane, (a, b) => a.distance.CompareTo(b.distance));
                foreach (RaycastHit hit in lane)
                {
                    if (hit.collider.transform.IsChildOf(owner)) continue;
                    desired = hit.point - delta.normalized * 0.35f; break;
                }
            }
            RaycastHit[] hits = Physics.RaycastAll(desired + Vector3.up * 0.6f, Vector3.down, 4f,
                ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if ((owner != null && hit.collider.transform.IsChildOf(owner)) ||
                    hit.collider.GetComponentInParent<CombatTarget>() != null || hit.normal.y < 0.5f) continue;
                return hit.point + Vector3.up * 0.25f;
            }
            return desired;
        }
    }
}
