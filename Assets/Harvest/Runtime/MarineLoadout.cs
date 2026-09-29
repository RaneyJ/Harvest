using System;
using UnityEngine;

namespace Harvest
{
    // Input and presentation can be replaced without changing weapon state or hit resolution.
    public sealed class MarineLoadout : MonoBehaviour
    {
        public Camera View;
        public WeaponDefinition[] Weapons;
        public event Action Changed;
        public event Action HitEnemy;

        struct WeaponState
        {
            public int Magazine;
            public int Reserve;
            public float NextShot;
            public float ReloadUntil;
        }

        WeaponState[] states;
        int selected;
        Vitality vitality;
        HarvestEncounter encounter;

        public WeaponDefinition Current => Weapons != null && Weapons.Length > selected ? Weapons[selected] : null;
        public int SelectedIndex => selected;
        public string AmmoText
        {
            get
            {
                if (Current == null || states == null) return "--";
                WeaponState state = states[selected];
                return state.ReloadUntil > Time.time ? "RELOADING" : $"{state.Magazine} / {state.Reserve}";
            }
        }

        void Awake()
        {
            vitality = GetComponent<Vitality>();
            encounter = FindFirstObjectByType<HarvestEncounter>();
            ResetLoadout();
        }

        public void ResetLoadout()
        {
            if (Weapons == null) return;
            selected = 0;
            states = new WeaponState[Weapons.Length];
            for (int i = 0; i < Weapons.Length; i++)
            {
                states[i].Magazine = Weapons[i].MagazineSize;
                states[i].Reserve = Weapons[i].StartingReserve;
            }
            Changed?.Invoke();
        }

        void Update()
        {
            if (vitality == null || !vitality.IsAlive || (encounter != null && encounter.IsFinished) || Current == null) return;
            if (Input.GetKeyDown(KeyCode.Alpha1)) Select(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Select(1);
            float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f && Weapons.Length > 1)
                Select((selected + (scroll < 0f ? 1 : Weapons.Length - 1)) % Weapons.Length);
            if (Input.GetKeyDown(KeyCode.R)) StartReload();
            if (Cursor.lockState == CursorLockMode.Locked &&
                (Current.Automatic ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0))) Fire();
            for (int i = 0; i < states.Length; i++)
            {
                WeaponState state = states[i];
                if (state.ReloadUntil <= 0f || Time.time < state.ReloadUntil) continue;
                WeaponDefinition definition = Weapons[i];
                int amount = Mathf.Min(definition.MagazineSize - state.Magazine, state.Reserve);
                state.Magazine += amount;
                state.Reserve -= amount;
                state.ReloadUntil = 0f;
                states[i] = state;
                Changed?.Invoke();
            }
        }

        void Select(int index)
        {
            if (index < 0 || index >= Weapons.Length || index == selected) return;
            selected = index;
            Changed?.Invoke();
        }

        void StartReload()
        {
            WeaponState state = states[selected];
            if (state.ReloadUntil > 0f || state.Magazine >= Current.MagazineSize || state.Reserve <= 0) return;
            state.ReloadUntil = Time.time + Current.ReloadSeconds;
            states[selected] = state;
            Changed?.Invoke();
        }

        void Fire()
        {
            WeaponState state = states[selected];
            if (Time.time < state.NextShot || state.ReloadUntil > 0f) return;
            if (state.Magazine <= 0) { StartReload(); return; }
            WeaponDefinition weapon = Current;
            state.NextShot = Time.time + weapon.FireInterval;
            state.Magazine--;
            states[selected] = state;
            Changed?.Invoke();
            bool hitEnemy = false;
            for (int i = 0; i < weapon.Pellets; i++)
            {
                Vector3 direction = View.transform.forward;
                if (weapon.SpreadDegrees > 0f)
                    direction = View.transform.rotation *
                        (Quaternion.Euler(UnityEngine.Random.Range(-weapon.SpreadDegrees, weapon.SpreadDegrees),
                            UnityEngine.Random.Range(-weapon.SpreadDegrees, weapon.SpreadDegrees), 0f) * Vector3.forward);
                Ray ray = new Ray(View.transform.position, direction);
                if (!Physics.Raycast(ray, out RaycastHit hit, weapon.Range, ~0, QueryTriggerInteraction.Ignore)) continue;
                Vitality target = hit.collider.GetComponentInParent<Vitality>();
                if (target == null || target == vitality) continue;
                target.ApplyDamage(weapon.DamagePerPellet, weapon.ShieldMultiplier);
                hitEnemy = true;
            }
            if (hitEnemy) HitEnemy?.Invoke();
        }
    }
}
