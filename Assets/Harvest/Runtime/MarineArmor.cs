using System;
using UnityEngine;

namespace Harvest
{
    // Marine armor is a finite buffer. Only a pickup restores it; health heals slowly out of combat.
    [RequireComponent(typeof(Vitality))]
    public sealed class MarineArmor : MonoBehaviour
    {
        [Min(1f)] public float MaxArmor = 175f;
        [Min(0f)] public float HealthRegenDelay = 7f;
        [Min(0f)] public float HealthRegenPerSecond = 5f;

        public float Armor { get; private set; }
        public event Action Changed;
        public event Action ArmorBroken;
        public event Action<bool> Hit; // true when health was exposed
        public event Action<float> ArmorRestored;

        Vitality vitality;
        float lastDamageTime;

        void Awake()
        {
            vitality = GetComponent<Vitality>();
            ResetArmor();
        }

        public void ResetArmor()
        {
            Armor = MaxArmor;
            lastDamageTime = Time.time;
            Changed?.Invoke();
        }

        public void ApplyDamage(float amount)
        {
            if (amount <= 0f || !vitality.IsAlive) return;
            lastDamageTime = Time.time;
            float absorbed = Mathf.Min(Armor, amount);
            Armor -= absorbed;
            float exposed = amount - absorbed;
            if (absorbed > 0f && Armor <= 0f) ArmorBroken?.Invoke();
            if (exposed > 0f) vitality.ApplyDamage(exposed);
            Changed?.Invoke();
            Hit?.Invoke(exposed > 0f);
        }

        public float RestoreArmor(float amount)
        {
            if (!vitality.IsAlive || amount <= 0f) return 0f;
            float restored = Mathf.Min(amount, MaxArmor - Armor);
            if (restored <= 0f) return 0f;
            Armor += restored;
            Changed?.Invoke();
            ArmorRestored?.Invoke(restored);
            return restored;
        }

        void Update()
        {
            if (Time.time - lastDamageTime >= HealthRegenDelay)
                vitality.Heal(HealthRegenPerSecond * Time.deltaTime);
        }
    }
}
