using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Harvest.Editor
{
    // Runs the real runtime exposure/weapon path in an isolated corner of the physics world.
    // No test-framework package is required. Existing encounter actors are out of range.
    public static class CombatRegressionChecks
    {
        [MenuItem("Harvest/Run Combat Regression Checks (Play Mode)")]
        public static void Run()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("Enter Play mode, then run Harvest > Run Combat Regression Checks (Play Mode).");
                return;
            }
            GameObject actor = null;
            GameObject wall = null;
            WeaponDefinition definition = null;
            try
            {
                Vector3 center = new Vector3(1000f, 1f, 1000f);
                actor = new GameObject("Temporary suppression regression actor");
                actor.transform.position = center;
                CharacterController body = actor.AddComponent<CharacterController>();
                body.height = 1.9f;
                body.radius = 0.4f;
                Vitality vitality = actor.AddComponent<Vitality>();
                CombatTarget identity = actor.AddComponent<CombatTarget>();
                identity.Team = CombatTeam.Covenant;
                identity.AimOffset = Vector3.zero;
                Suppression pressure = actor.AddComponent<Suppression>();
                Physics.SyncTransforms();

                var exposures = new Dictionary<Suppression, float>();
                Vector3 start = center + Vector3.left * 5f;
                Vector3 end = center + Vector3.right * 5f;
                Suppression.ObserveSegment(start, end, CombatTeam.Covenant, exposures, true);
                Expect(pressure.Value, 0f, "Friendly shots do not suppress");
                Suppression.ObserveSegment(start + Vector3.forward * 10f, end + Vector3.forward * 10f,
                    CombatTeam.Marine, exposures, true);
                Expect(pressure.Value, 0f, "Distant shots do not suppress");

                for (int frame = 0; frame < 120; frame++)
                    Suppression.ObserveSegment(start, end, CombatTeam.Marine, exposures, true);
                Expect(pressure.Value, pressure.PressurePerShot, "One projectile cannot stack pressure per frame");
                pressure.ResetPressure();
                exposures.Clear();
                Suppression.ObserveSegment(start + Vector3.forward * 1.5f, end + Vector3.forward * 1.5f,
                    CombatTeam.Marine, exposures, true);
                float nearMiss = pressure.Value;
                if (nearMiss <= 0f || nearMiss >= pressure.PressurePerShot)
                    throw new Exception("Near miss should contribute less than a direct shot.");
                Suppression.ObserveSegment(start, end, CombatTeam.Marine, exposures, true);
                Expect(pressure.Value, pressure.PressurePerShot, "A closer segment only adds its exposure difference");

                pressure.ResetPressure();
                exposures.Clear();
                Vector3 impact = center + Vector3.forward * 1.55f;
                Vector3 incoming = center + Vector3.forward * 5f;
                Suppression.ObserveSegment(incoming, impact, CombatTeam.Marine, exposures, true);
                float exposedImpact = pressure.Value;
                wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = "Temporary suppression regression cover";
                wall.transform.position = center + Vector3.forward * 1.3f;
                wall.transform.localScale = new Vector3(3f, 3f, 0.5f);
                Physics.SyncTransforms();
                pressure.ResetPressure();
                exposures.Clear();
                Suppression.ObserveSegment(incoming, impact, CombatTeam.Marine, exposures, true);
                Expect(pressure.Value, exposedImpact * pressure.CoveredMultiplier,
                    "A projectile ending exactly on the cover face is attenuated");
                wall.SetActive(false);
                Physics.SyncTransforms();

                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.Pellets = 8;
                definition.SpreadDegrees = 0f;
                pressure.ResetPressure();
                WeaponInstance gun = new WeaponInstance(definition);
                WeaponRuntime.Fire(gun, start, Quaternion.LookRotation(Vector3.right), CombatTeam.Marine);
                Expect(pressure.Value, pressure.PressurePerShot, "Shotgun pellets count as one pressure contribution");
                float afterShot = pressure.Value;
                WeaponRuntime.Fire(gun, start, Quaternion.LookRotation(Vector3.right), CombatTeam.Marine);
                Expect(pressure.Value, afterShot, "Cooldown rejection creates no suppression");
                definition.UsesEnergy = true;
                definition.EnergyCapacity = 1;
                definition.EnergyPerShot = 2;
                pressure.ResetPressure();
                WeaponRuntime.Fire(new WeaponInstance(definition), start,
                    Quaternion.LookRotation(Vector3.right), CombatTeam.Marine);
                Expect(pressure.Value, 0f, "Empty energy weapons create no suppression");

                for (int shot = 0; shot < 100; shot++) pressure.AddPressure(pressure.PressurePerShot);
                Expect(pressure.Value, 1f, "Sustained fire caps at maximum pressure");
                Expect(pressure.AccuracyPenaltyDegrees, pressure.MaxAccuracyPenaltyDegrees, "Accuracy loss is capped");
                pressure.ResetPressure();
                Expect(pressure.AccuracyPenaltyDegrees, 0f, "Reset clears accuracy loss");
                Expect(vitality.Health, vitality.MaxHealth, "Exposure itself does not damage health");

                Debug.Log("Combat regression checks passed: faction/range filters, per-projectile and per-pellet limits, cover impacts, cooldown/ammo rejection, pressure cap/reset.");
            }
            catch (Exception error) { Debug.LogException(error); }
            finally
            {
                if (actor != null) Object.Destroy(actor);
                if (wall != null) Object.Destroy(wall);
                if (definition != null) Object.Destroy(definition);
            }
        }
        static void Expect(float actual, float expected, string check)
        {
            if (Mathf.Abs(actual - expected) > 0.0001f)
                throw new Exception($"{check}: expected {expected}, got {actual}.");
        }
    }
}
