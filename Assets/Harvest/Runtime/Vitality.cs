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

        float lastDamageTime;

        void Awake() => Restore();

        public void Restore()
        {
            Health = MaxHealth;
            Shield = MaxShield;
            lastDamageTime = Time.time;
            Changed?.Invoke();
        }

        public void ApplyDamage(float amount, float shieldMultiplier = 1f)
        {
            if (!IsAlive || amount <= 0f) return;
            lastDamageTime = Time.time;
            float remaining = amount;
            if (Shield > 0f && shieldMultiplier > 0f)
            {
                float absorbed = Mathf.Min(Shield, remaining * shieldMultiplier);
                Shield -= absorbed;
                remaining -= absorbed / shieldMultiplier;
                if (Shield <= 0f) ShieldBroken?.Invoke();
            }
            Health = Mathf.Max(0f, Health - remaining);
            Changed?.Invoke();
            if (!IsAlive) Died?.Invoke();
        }

        void Update()
        {
            if (!IsAlive || Shield >= MaxShield || Time.time - lastDamageTime < ShieldRechargeDelay) return;
            Shield = Mathf.Min(MaxShield, Shield + ShieldRechargePerSecond * Time.deltaTime);
            Changed?.Invoke();
        }
    }
}
