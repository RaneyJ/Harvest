using UnityEngine;

namespace Harvest
{
    // Travels with a weapon when it is dropped or picked up. AI and players share this state.
    public sealed class WeaponInstance
    {
        public WeaponDefinition Definition { get; }
        public event System.Action<WeaponInstance> ReloadStarted;
        public int Magazine { get; private set; }
        public int Reserve { get; private set; }
        public int Energy { get; private set; }
        public float NextShot { get; private set; }
        public float ReloadUntil { get; private set; }

        public string AmmoText => Definition.UsesEnergy ? $"{Mathf.CeilToInt(100f * Energy / Mathf.Max(1, Definition.EnergyCapacity))}%" :
            ReloadUntil > Time.time ? "RELOADING" : $"{Magazine} / {Reserve}";

        public WeaponInstance(WeaponDefinition definition)
        {
            Definition = definition;
            Magazine = definition.MagazineSize;
            Reserve = definition.StartingReserve;
            Energy = definition.EnergyCapacity;
        }

        public void Tick()
        {
            if (Definition.UsesEnergy || ReloadUntil <= 0f || Time.time < ReloadUntil) return;
            int amount = Mathf.Min(Definition.MagazineSize - Magazine, Reserve);
            Magazine += amount;
            Reserve -= amount;
            ReloadUntil = 0f;
        }

        public bool BeginReload()
        {
            if (Definition.UsesEnergy || ReloadUntil > 0f || Magazine >= Definition.MagazineSize || Reserve <= 0) return false;
            ReloadUntil = Time.time + Definition.ReloadSeconds;
            ReloadStarted?.Invoke(this);
            return true;
        }

        // Only NPC death drops receive fresh ammunition. Ordinary transfers retain state.
        public void PrepareNpcDrop()
        {
            if (Definition.UsesEnergy) Energy = DropAmount(Definition.EnergyCapacity);
            else
            {
                Magazine = DropAmount(Definition.MagazineSize);
                Reserve = Definition.StartingReserve > 0 ? DropAmount(Definition.StartingReserve) : 0;
            }
            NextShot = 0f;
            ReloadUntil = 0f;
        }

        static int DropAmount(int capacity)
        {
            int minimum = Mathf.Max(1, Mathf.CeilToInt(capacity * 0.4f));
            int maximum = Mathf.Max(minimum, Mathf.FloorToInt(capacity * 0.7f));
            return Random.Range(minimum, maximum + 1);
        }

        public bool CanCharge => Definition.SupportsCharge && Definition.UsesEnergy &&
            Energy >= Definition.ChargedEnergyCost && Time.time >= NextShot && ReloadUntil <= 0f;

        public bool TryConsumeShot(bool charged = false)
        {
            if (Time.time < NextShot || ReloadUntil > 0f || (charged && !CanCharge)) return false;
            if (Definition.UsesEnergy)
            {
                int cost = charged ? Definition.ChargedEnergyCost : Definition.EnergyPerShot;
                if (Energy < cost) return false;
                Energy -= cost;
            }
            else
            {
                if (Magazine <= 0) { BeginReload(); return false; }
                Magazine--;
            }
            NextShot = Time.time + Definition.FireInterval;
            return true;
        }
    }
}
