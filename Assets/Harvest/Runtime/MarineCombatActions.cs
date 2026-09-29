using System;
using UnityEngine;

namespace Harvest
{
    [RequireComponent(typeof(MeleeAttack), typeof(GrenadeInventory))]
    public sealed class MarineCombatActions : MonoBehaviour
    {
        public Camera View;
        public event Action HitEnemy;
        public bool IsBusy => Time.time < busyUntil;
        MeleeAttack melee;
        GrenadeInventory grenades;
        Vitality vitality;
        HarvestEncounter encounter;
        float busyUntil;
        void Awake()
        {
            melee = GetComponent<MeleeAttack>();
            grenades = GetComponent<GrenadeInventory>();
            vitality = GetComponent<Vitality>();
            encounter = FindFirstObjectByType<HarvestEncounter>();
        }
        void Update()
        {
            if (View == null || !vitality.IsAlive || Cursor.lockState != CursorLockMode.Locked ||
                (encounter != null && encounter.IsFinished)) return;
            if (Input.GetKeyDown(KeyCode.Q)) grenades.SelectNext();
            if (IsBusy) return;
            if (Input.GetKeyDown(KeyCode.F) && melee.TrySwing(View.transform.position, View.transform.forward, out bool hit))
            {
                busyUntil = Time.time + 0.35f;
                if (hit) HitEnemy?.Invoke();
            }
            else if (Input.GetKeyDown(KeyCode.G) && grenades.TryThrow(View.transform.position, View.transform.forward))
                busyUntil = Time.time + 0.35f;
        }
    }
}
