using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Harvest.Editor
{
    public static class PrecisionRegressionChecks
    {
        [MenuItem("Harvest/Run Precision Checks (Play Mode)")]
        public static void Run()
        {
            if (!Application.isPlaying) { Debug.Log("Enter Play mode before running precision checks."); return; }
            GameObject actor = null, marine = null;
            WeaponDefinition definition = null;
            try
            {
                Vector3 center = new Vector3(3000f, 1f, 3000f);
                actor = new GameObject("Temporary precision target");
                actor.transform.position = center;
                CharacterController collider = actor.AddComponent<CharacterController>();
                collider.height = 2f; collider.radius = 0.42f;
                Vitality vitality = actor.AddComponent<Vitality>();
                vitality.MaxHealth = 300f; vitality.Restore();
                CombatTarget identity = actor.AddComponent<CombatTarget>();
                identity.Team = CombatTeam.Covenant;
                actor.AddComponent<PrecisionHitRegion>();
                Suppression pressure = actor.AddComponent<Suppression>();
                pressure.AddPressure(1f);
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.IsPrecision = true; definition.DamagePerPellet = 90f;
                definition.SpreadDegrees = 1.8f; definition.AdsSpreadDegrees = 0f;
                definition.FireInterval = 1.4f; definition.ShowTracers = false;
                Expect(WeaponRuntime.Spread(definition, pressure, true), 0f, "ADS ignores suppression spread");
                Expect(WeaponRuntime.Spread(definition, pressure, false), 6.8f, "Hipfire keeps suppression spread");
                definition.IsPrecision = false;
                Expect(WeaponRuntime.Spread(definition, pressure, true), 6.8f, "Non-precision guns cannot bypass pressure");
                definition.IsPrecision = true;
                Physics.SyncTransforms();
                Vector3 headOrigin = center + Vector3.up * 0.7f + Vector3.back * 5f;
                Vector3 bodyOrigin = center + Vector3.back * 5f;
                var weapon = new WeaponInstance(definition);
                if (!WeaponRuntime.Fire(weapon, bodyOrigin, Quaternion.identity, CombatTeam.Marine, false, null, true))
                    throw new Exception("Body shot should hit target.");
                Expect(vitality.Health, 210f, "Body hit deals normal rifle damage");
                if (WeaponRuntime.Fire(weapon, bodyOrigin, Quaternion.identity, CombatTeam.Marine, false, null, true))
                    throw new Exception("Bolt cooldown should reject an immediate follow-up.");
                Expect(weapon.Magazine, definition.MagazineSize - 1, "Cooldown consumes no extra ammunition");
                vitality.Restore();
                WeaponRuntime.Fire(new WeaponInstance(definition), headOrigin, Quaternion.identity, CombatTeam.Marine, false, null, true);
                Expect(vitality.Health, 0f, "Unshielded precision headshot kills");
                vitality.MaxShield = 60f; vitality.Restore();
                WeaponRuntime.Fire(new WeaponInstance(definition), headOrigin, Quaternion.identity, CombatTeam.Marine, false, null, true);
                Expect(vitality.Health, 270f, "Active shields prevent headshot execution on the shield-breaking shot");
                vitality.MaxShield = 0f; vitality.Restore();
                actor.AddComponent<BruteBehavior>();
                WeaponRuntime.Fire(new WeaponInstance(definition), headOrigin, Quaternion.identity, CombatTeam.Marine, false, null, true);
                Expect(vitality.Health, 210f, "Brute headshot receives normal damage");

                marine = new GameObject("Temporary precision marine");
                marine.AddComponent<Vitality>();
                MarineArmor armor = marine.AddComponent<MarineArmor>();
                PrecisionAim aim = marine.AddComponent<PrecisionAim>();
                aim.ProcessAimInput(true, true);
                if (!aim.IsAiming) throw new Exception("Allowed ADS request should aim.");
                armor.ApplyDamage(1f);
                if (aim.IsAiming) throw new Exception("Armor hit must interrupt ADS.");
                aim.ProcessAimInput(true, true);
                if (aim.IsAiming) throw new Exception("Held input must not re-enter ADS after a hit.");
                aim.ProcessAimInput(false, true);
                aim.ProcessAimInput(true, true);
                if (!aim.IsAiming) throw new Exception("Release then press should restore ADS.");
                Debug.Log("Precision checks passed: ADS/hipfire spread, body/head damage, shields, Brute exception, bolt cooldown/ammo, damage de-scope, and aim release latch.");
            }
            catch (Exception error) { Debug.LogException(error); }
            finally
            {
                if (actor != null) { actor.SetActive(false); Object.Destroy(actor); }
                if (marine != null) { marine.SetActive(false); Object.Destroy(marine); }
                if (definition != null) Object.Destroy(definition);
            }
        }
        static void Expect(float actual, float expected, string message)
        {
            if (Mathf.Abs(actual - expected) > 0.001f) throw new Exception($"{message}: expected {expected}, got {actual}.");
        }
    }
}
