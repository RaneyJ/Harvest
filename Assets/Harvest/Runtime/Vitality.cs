using System;
using UnityEngine;

namespace Harvest
{
    // The only component that changes health or shields. A future host/server can own calls to ApplyDamage.
    public sealed class Vitality : MonoBehaviour
    {
        public float MaxHealth = 100f;
        public float MaxShield;
        public float ShieldRechargeDelay = 6f;
        public float ShieldRechargePerSecond = 18f;

        public float Health { get; private set; }
        public float Shield { get; private set; }
        public bool IsAlive => Health > 0f;
        public event Action Changed;
        public event Action Died;
        public event Action ShieldBroken;
        public event Action<bool, bool> Damaged; // shield hit, shield broken

        float lastDamageTime;

        void Awake() => Restore();

        public void Restore()
        {
            Health = MaxHealth;
            Shield = MaxShield;
            lastDamageTime = Time.time;
            Changed?.Invoke();
        }

        public void ApplyDamage(float amount, float shieldMultiplier = 1f, bool bypassShield = false, bool breaksShield = false)
        {
            if (!IsAlive || amount <= 0f) return;
            lastDamageTime = Time.time;
            float remaining = amount;
            bool shieldHit = !bypassShield && Shield > 0f && shieldMultiplier > 0f;
            bool brokeShield = false;
            if (!bypassShield && Shield > 0f && shieldMultiplier > 0f)
            {
                if (breaksShield)
                {
                    Shield = 0f;
                    remaining = 0f; // A shield-breaking impact does not overflow into health.
                }
                else
                {
                    float absorbed = Mathf.Min(Shield, remaining * shieldMultiplier);
                    Shield -= absorbed;
                    remaining -= absorbed / shieldMultiplier;
                }
                if (Shield <= 0f)
                {
                    brokeShield = true;
                    ShieldBroken?.Invoke();
                }
            }
            Health = Mathf.Max(0f, Health - remaining);
            Changed?.Invoke();
            Damaged?.Invoke(shieldHit, brokeShield);
            if (!IsAlive) Died?.Invoke();
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f || Health >= MaxHealth) return;
            Health = Mathf.Min(MaxHealth, Health + amount);
            Changed?.Invoke();
        }

        void Update()
        {
            if (!IsAlive || Shield >= MaxShield || Time.time - lastDamageTime < ShieldRechargeDelay) return;
            Shield = Mathf.Min(MaxShield, Shield + ShieldRechargePerSecond * Time.deltaTime);
            Changed?.Invoke();
        }
    }
}
