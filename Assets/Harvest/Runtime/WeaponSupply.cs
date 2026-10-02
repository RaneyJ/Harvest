using UnityEngine;

namespace Harvest
{
    // Creates serializable scene supplies using the same pickup and weapon-instance path as death drops.
    [RequireComponent(typeof(DroppedWeapon))]
    public sealed class WeaponSupply : MonoBehaviour
    {
        public WeaponDefinition Definition;
        void Start() { if (Definition != null) GetComponent<DroppedWeapon>().Initialize(new WeaponInstance(Definition)); }
    }
}
