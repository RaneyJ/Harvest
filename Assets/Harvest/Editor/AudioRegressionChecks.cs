using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Harvest.Editor
{
    public static class AudioRegressionChecks
    {
        [MenuItem("Harvest/Run Audio Event Checks (Play Mode)")]
        public static void Run()
        {
            if (!Application.isPlaying) { Debug.Log("Enter Play mode before running audio checks."); return; }
            WeaponDefinition definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            int shots = 0, reloads = 0;
            Action<WeaponDefinition, Vector3, Vector3, CombatTeam> onShot = (weapon, origin, direction, team) => { if (weapon == definition) shots++; };
            WeaponRuntime.ShotPresented += onShot;
            try
            {
                definition.MagazineSize = 2; definition.StartingReserve = 4;
                definition.Pellets = 8; definition.DamagePerPellet = 0f;
                definition.Range = 1f; definition.ShowTracers = false;
                WeaponInstance instance = new WeaponInstance(definition);
                instance.ReloadStarted += weapon => reloads++;
                if (instance.BeginReload()) throw new Exception("Full magazine must reject reload.");
                WeaponRuntime.Fire(instance, new Vector3(4000f, 20f, 4000f), Quaternion.identity, CombatTeam.Marine);
                if (shots != 1) throw new Exception("Shotgun must emit one shot sound event for eight pellets.");
                WeaponRuntime.Fire(instance, new Vector3(4000f, 20f, 4000f), Quaternion.identity, CombatTeam.Marine);
                if (shots != 1) throw new Exception("Cooldown failure must not emit another sound.");
                if (!instance.BeginReload() || reloads != 1) throw new Exception("Successful reload must emit one instance event.");
                if (instance.BeginReload() || reloads != 1) throw new Exception("Pending reload must not replay its sound.");
                WeaponRuntime.Fire(instance, new Vector3(4000f, 20f, 4000f), Quaternion.identity, CombatTeam.Marine);
                if (shots != 1) throw new Exception("Reloading weapon must not emit a shot sound.");
                Debug.Log("Audio checks passed: one shot event per trigger, cooldown/reload rejection silence, full-magazine rejection, and one reload event per start.");
            }
            catch (Exception error) { Debug.LogException(error); }
            finally { WeaponRuntime.ShotPresented -= onShot; Object.Destroy(definition); }
        }
    }
}
