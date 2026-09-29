using UnityEngine;

namespace Harvest
{
    [RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
    public sealed class GrenadePickup : MonoBehaviour
    {
        public GrenadeKind Kind;
        public Transform Visual;
        void Update() { if (Visual != null) Visual.Rotate(Vector3.up, 35f * Time.deltaTime); }
        void OnTriggerEnter(Collider other) => TryCollect(other);
        void OnTriggerStay(Collider other) => TryCollect(other);
        void TryCollect(Collider other)
        {
            if (other.GetComponentInParent<MarineController>() == null) return;
            GrenadeInventory inventory = other.GetComponentInParent<GrenadeInventory>();
            if (inventory == null || inventory.Refill(Kind) <= 0) return;
            // Disable immediately so multiple collider callbacks cannot consume this twice.
            GetComponent<Collider>().enabled = false;
            Destroy(gameObject);
        }
    }
}
