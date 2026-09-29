using UnityEngine;

namespace Harvest
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class DroppedWeapon : MonoBehaviour
    {
        public Renderer Visual;
        public WeaponInstance Weapon { get; private set; }
        float availableAt;
        public bool IsAvailable => Weapon != null && Time.time >= availableAt;

        public static DroppedWeapon Spawn(DroppedWeapon prefab, WeaponInstance instance, Vector3 position)
        {
            DroppedWeapon dropped = Instantiate(prefab, position, Quaternion.identity);
            dropped.Initialize(instance);
            return dropped;
        }

        public void Initialize(WeaponInstance instance)
        {
            Weapon = instance;
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

        void OnGUI()
        {
            if (!IsAvailable || Camera.main == null) return;
            MarineController marine = FindFirstObjectByType<MarineController>();
            if (marine == null || Vector3.Distance(transform.position, marine.transform.position) > 3f) return;
            Vector3 point = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 0.7f);
            if (point.z > 0f)
                GUI.Box(new Rect(point.x - 82f, Screen.height - point.y, 164f, 38f),
                    $"[E] {Weapon.Definition.DisplayName}\n{Weapon.AmmoText}");
        }
    }
}
