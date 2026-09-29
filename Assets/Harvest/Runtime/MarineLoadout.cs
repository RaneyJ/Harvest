using System;
using UnityEngine;

namespace Harvest
{
    // Player input around the shared WeaponInstance/WeaponRuntime path.
    public sealed class MarineLoadout : MonoBehaviour
    {
        public Camera View;
        public WeaponDefinition[] Weapons;
        public DroppedWeapon DropPrefab;
        public event Action Changed;
        public event Action HitEnemy;

        WeaponInstance[] slots;
        int selected;
        WeaponInstance chargingWeapon;
        float chargeStarted;
        Vitality vitality;
        Suppression suppression;
        HarvestEncounter encounter;

        public WeaponDefinition Current => Equipped?.Definition;
        public WeaponInstance Equipped => slots != null && slots.Length > selected ? slots[selected] : null;
        public int SelectedIndex => selected;
        public bool IsCharging => chargingWeapon != null;
        public float ChargeFraction => !IsCharging ? 0f : Mathf.Clamp01(
            (Time.time - chargeStarted) / Mathf.Max(0.01f, chargingWeapon.Definition.ChargeSeconds));
        public string AmmoText => Equipped == null ? "--" : Equipped.AmmoText;

        void Awake()
        {
            vitality = GetComponent<Vitality>();
            suppression = GetComponent<Suppression>();
            encounter = FindFirstObjectByType<HarvestEncounter>();
            ResetLoadout();
        }

        void Start()
        {
            if (vitality != null) vitality.Died += DropEquippedOnDeath;
        }

        void OnDisable()
        {
            CancelCharge();
            if (vitality != null) vitality.Died -= DropEquippedOnDeath;
        }

        void DropEquippedOnDeath()
        {
            CancelCharge();
            if (Equipped == null || DropPrefab == null) return;
            Vector3 position = transform.position;
            position.y = 0.55f;
            DroppedWeapon.Spawn(DropPrefab, Equipped, position);
            slots[selected] = null;
            Changed?.Invoke();
        }

        public void ResetLoadout()
        {
            CancelCharge();
            if (Weapons == null) return;
            selected = 0;
            slots = new WeaponInstance[Weapons.Length];
            for (int i = 0; i < Weapons.Length; i++)
                if (Weapons[i] != null) slots[i] = new WeaponInstance(Weapons[i]);
            Changed?.Invoke();
        }

        void Update()
        {
            if (vitality == null || !vitality.IsAlive || (encounter != null && encounter.IsFinished))
            {
                CancelCharge();
                return;
            }
            if (Input.GetKeyDown(KeyCode.Alpha1)) Select(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Select(1);
            float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f && slots != null && slots.Length > 1)
                Select((selected + (scroll < 0f ? 1 : slots.Length - 1)) % slots.Length);
            if (Input.GetKeyDown(KeyCode.R) && Equipped != null && Equipped.BeginReload()) Changed?.Invoke();
            if (Input.GetKeyDown(KeyCode.E)) TryPickup();
            HandleFireInput();
            if (slots == null) return;
            foreach (WeaponInstance weapon in slots)
            {
                if (weapon == null) continue;
                bool reloading = weapon.ReloadUntil > 0f;
                weapon.Tick();
                if (reloading && weapon.ReloadUntil <= 0f) Changed?.Invoke();
            }
        }

        void HandleFireInput()
        {
            if (Cursor.lockState != CursorLockMode.Locked || Current == null || View == null)
            {
                CancelCharge();
                return;
            }
            if (!Current.SupportsCharge)
            {
                if (Current.Automatic ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0)) Fire(false);
                return;
            }
            if (Input.GetMouseButtonDown(0) && Time.time >= Equipped.NextShot)
            {
                chargingWeapon = Equipped;
                chargeStarted = Time.time;
            }
            if (!IsCharging) return;
            if (chargingWeapon != Equipped) { CancelCharge(); return; }
            if (Input.GetMouseButtonUp(0))
            {
                bool charged = ChargeFraction >= 1f && Equipped.CanCharge;
                CancelCharge();
                Fire(charged);
            }
            else if (!Input.GetMouseButton(0)) CancelCharge();
        }

        void Fire(bool charged)
        {
            if (!Current.UsesEnergy && Equipped.Magazine <= 0 && Equipped.BeginReload()) Changed?.Invoke();
            int before = Current.UsesEnergy ? Equipped.Energy : Equipped.Magazine;
            bool hit = WeaponRuntime.Fire(Equipped, View.transform.position, View.transform.rotation,
                CombatTeam.Marine, charged, suppression);
            int after = Current.UsesEnergy ? Equipped.Energy : Equipped.Magazine;
            if (before != after) Changed?.Invoke();
            if (hit) HitEnemy?.Invoke();
        }

        void CancelCharge() => chargingWeapon = null;

        void Select(int index)
        {
            if (slots == null || index < 0 || index >= slots.Length || index == selected) return;
            CancelCharge();
            selected = index;
            Changed?.Invoke();
        }

        void TryPickup()
        {
            if (slots == null || DropPrefab == null) return;
            Collider[] nearby = Physics.OverlapSphere(transform.position, 2.5f, ~0, QueryTriggerInteraction.Collide);
            DroppedWeapon closest = null;
            float best = float.MaxValue;
            foreach (Collider collider in nearby)
            {
                DroppedWeapon candidate = collider.GetComponentInParent<DroppedWeapon>();
                if (candidate == null || !candidate.IsAvailable) continue;
                float distance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (distance < best) { best = distance; closest = candidate; }
            }
            if (closest == null) return;
            WeaponInstance taken = closest.Take();
            if (taken == null) return;
            CancelCharge();
            WeaponInstance replaced = slots[selected];
            slots[selected] = taken;
            if (replaced != null)
            {
                Vector3 dropPosition = transform.position + transform.forward * 1.35f;
                dropPosition.y = 0.55f;
                DroppedWeapon.Spawn(DropPrefab, replaced, dropPosition);
            }
            Changed?.Invoke();
        }
    }
}
