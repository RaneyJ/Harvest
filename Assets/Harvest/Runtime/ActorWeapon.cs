using UnityEngine;

namespace Harvest
{
    public sealed class ActorWeapon : MonoBehaviour
    {
        public CombatTeam Team;
        public WeaponDefinition StartingWeapon;
        public DroppedWeapon DropPrefab;
        public WeaponInstance Equipped { get; private set; }

        void Awake()
        {
            if (StartingWeapon != null) Equipped = new WeaponInstance(StartingWeapon);
        }

        void Update() => Equipped?.Tick();

        public bool TryFire(Vector3 origin, Vector3 direction, float spreadDegrees = 0f)
        {
            if (Equipped == null || direction.sqrMagnitude < 0.001f) return false;
            Quaternion aim = Quaternion.LookRotation(direction) * Quaternion.Euler(
                Random.Range(-spreadDegrees, spreadDegrees), Random.Range(-spreadDegrees, spreadDegrees), 0f);
            return WeaponRuntime.Fire(Equipped, origin, aim, Team);
        }

        public void Drop()
        {
            if (Equipped == null || DropPrefab == null) return;
            Equipped.PrepareNpcDrop();
            DroppedWeapon.Spawn(DropPrefab, Equipped,
                new Vector3(transform.position.x, 0.55f, transform.position.z));
            Equipped = null;
        }
    }
}
