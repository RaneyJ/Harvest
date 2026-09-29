using UnityEngine;

namespace Harvest
{
    // Travels with a weapon when it is dropped or picked up. AI and players share this state.
    public sealed class WeaponInstance
    {
        public WeaponDefinition Definition { get; }
        public int Magazine { get; private set; }
        public int Reserve { get; private set; }
        public int Energy { get; private set; }
        public float NextShot { get; private set; }
        public float ReloadUntil { get; private set; }

        public string AmmoText => Definition.UsesEnergy ? $"{Energy}%" :
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
            return true;
        }

        public bool TryConsumeShot()
        {
            if (Time.time < NextShot || ReloadUntil > 0f) return false;
            if (Definition.UsesEnergy)
            {
                if (Energy < Definition.EnergyPerShot) return false;
                Energy -= Definition.EnergyPerShot;
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
