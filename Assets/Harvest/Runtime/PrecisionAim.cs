using UnityEngine;

namespace Harvest
{
    // Input and camera presentation only. Shared firing takes an explicit ADS flag.
    [DefaultExecutionOrder(-20)]
    [RequireComponent(typeof(MarineLoadout), typeof(MarineArmor))]
    public sealed class PrecisionAim : MonoBehaviour
    {
        public Camera View;
        public bool IsAiming { get; private set; }
        public float Blend { get; private set; }
        MarineLoadout loadout;
        MarineArmor armor;
        MarineCombatActions actions;
        Vitality vitality;
        HarvestEncounter encounter;
        float hipFov;
        bool releaseRequired;
        void Awake()
        {
            loadout = GetComponent<MarineLoadout>();
            armor = GetComponent<MarineArmor>();
            vitality = GetComponent<Vitality>();
        }
        void Start()
        {
            if (View == null) View = loadout.View;
            hipFov = View != null ? View.fieldOfView : 75f;
            actions = GetComponent<MarineCombatActions>();
            encounter = FindFirstObjectByType<HarvestEncounter>();
        }
        void OnEnable() { if (armor == null) armor = GetComponent<MarineArmor>(); armor.Hit += OnHit; armor.ArmorReset += ResetAim; }
        void OnDisable()
        {
            if (armor != null) { armor.Hit -= OnHit; armor.ArmorReset -= ResetAim; }
            ResetAim();
            if (View != null && hipFov > 0f) View.fieldOfView = hipFov;
        }
        void OnHit(bool healthExposed) => CancelAim();
        public void CancelAim() { IsAiming = false; releaseRequired = true; }
        void ResetAim() { IsAiming = false; Blend = 0f; releaseRequired = true; }
        void Update()
        {
            bool held = Input.GetMouseButton(1);
            bool available = vitality.IsAlive && Cursor.lockState == CursorLockMode.Locked &&
                loadout.Current != null && loadout.Current.IsPrecision && loadout.Equipped.ReloadUntil <= 0f &&
                (encounter == null || !encounter.IsFinished) && (actions == null || !actions.IsBusy) &&
                !Input.GetKey(KeyCode.LeftShift) && !Input.GetKeyDown(KeyCode.F) && !Input.GetKeyDown(KeyCode.G);
            ProcessAimInput(held, available);
        }
        public void ProcessAimInput(bool held, bool available)
        {
            if (!held) releaseRequired = false;
            if (!available && held) releaseRequired = true;
            IsAiming = available && held && !releaseRequired;
        }
        void LateUpdate()
        {
            Blend = Mathf.MoveTowards(Blend, IsAiming ? 1f : 0f, Time.deltaTime * 10f);
            if (View != null)
                View.fieldOfView = Mathf.Lerp(hipFov, loadout.Current != null && loadout.Current.IsPrecision ? loadout.Current.AdsFieldOfView : hipFov, Blend);
        }
    }
}
