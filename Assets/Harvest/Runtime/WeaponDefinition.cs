using UnityEngine;

namespace Harvest
{
    public enum WeaponShotKind { Hitscan, PlasmaBolt }

    [CreateAssetMenu(menuName = "Harvest/Weapon")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        public string DisplayName = "Rifle";
        public WeaponShotKind ShotKind;
        public bool UsesEnergy;
        [Min(1)] public int EnergyCapacity = 100;
        [Min(1)] public int EnergyPerShot = 3;
        [Min(1)] public int MagazineSize = 32;
        [Min(0)] public int StartingReserve = 128;
        [Min(0f)] public float DamagePerPellet = 24f;
        [Min(0f)] public float ShieldMultiplier = 1f;
        [Min(1)] public int Pellets = 1;
        [Min(0f)] public float SpreadDegrees;
        [Min(1f)] public float Range = 90f;
        [Min(0.01f)] public float FireInterval = 0.12f;
        [Min(0.01f)] public float ReloadSeconds = 1.8f;
        public bool Automatic = true;
        public PlasmaBolt ProjectilePrefab;
        [Min(1f)] public float ProjectileSpeed = 17f;
        public Material PickupMaterial;
        [Header("Charged shot")]
        public bool SupportsCharge;
        [Min(0.01f)] public float ChargeSeconds = 1f;
        [Min(1)] public int ChargedEnergyCost = 20;
        [Min(0f)] public float ChargedDamage = 40f;
    }
}
