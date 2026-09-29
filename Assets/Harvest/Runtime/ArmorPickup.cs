using UnityEngine;

namespace Harvest
{
    [RequireComponent(typeof(Collider))]
    public sealed class ArmorPickup : MonoBehaviour
    {
        [Min(1f)] public float ArmorAmount = 90f;
        public float BobHeight = 0.12f;
        public float BobSpeed = 2f;
        Vector3 origin;

        void Start() => origin = transform.position;

        void Update()
        {
            transform.position = origin + Vector3.up * (Mathf.Sin(Time.time * BobSpeed) * BobHeight);
            transform.Rotate(Vector3.up, 30f * Time.deltaTime);
        }

        void OnTriggerEnter(Collider other)
        {
            MarineArmor armor = other.GetComponentInParent<MarineArmor>();
            if (armor != null && armor.RestoreArmor(ArmorAmount) > 0f)
                Destroy(gameObject);
        }
    }
}
