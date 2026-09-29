using UnityEngine;

namespace Harvest
{
    // Bounded view kick, composed with mouse pitch by MarineController rather than stacked on a camera transform.
    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(MarineLoadout))]
    public sealed class WeaponRecoil : MonoBehaviour
    {
        [Min(0f)] public float RecoveryRate = 12f;
        [Min(0f)] public float MaximumPitch = 8f;
        [Min(0f)] public float MaximumYaw = 2f;
        public float Pitch { get; private set; }
        public float Yaw { get; private set; }
        MarineLoadout loadout;
        PrecisionAim aim;
        MarineArmor armor;
        WeaponDefinition previous;
        void Awake() { loadout = GetComponent<MarineLoadout>(); aim = GetComponent<PrecisionAim>(); armor = GetComponent<MarineArmor>(); }
        void OnEnable()
        {
            if (loadout == null) loadout = GetComponent<MarineLoadout>();
            loadout.ShotFired += Kick;
            loadout.Changed += OnLoadoutChanged;
            if (armor != null) armor.ArmorReset += Clear;
        }
        void OnDisable()
        {
            if (loadout != null) { loadout.ShotFired -= Kick; loadout.Changed -= OnLoadoutChanged; }
            if (armor != null) armor.ArmorReset -= Clear;
            Clear();
        }
        void OnLoadoutChanged()
        {
            if (loadout.Current != previous) { Clear(); previous = loadout.Current; }
        }
        void Clear() { Pitch = Yaw = 0f; }
        void Kick()
        {
            WeaponDefinition definition = loadout.Current;
            if (definition == null) return;
            float multiplier = aim != null && aim.IsAiming ? 0.65f : 1f;
            Pitch = Mathf.Min(MaximumPitch, Pitch + definition.RecoilPitch * multiplier);
            Yaw = Mathf.Clamp(Yaw + Random.Range(-definition.RecoilYaw, definition.RecoilYaw) * multiplier, -MaximumYaw, MaximumYaw);
        }
        void Update()
        {
            float decay = Mathf.Exp(-RecoveryRate * Time.deltaTime);
            Pitch *= decay; Yaw *= decay;
        }
    }
}
