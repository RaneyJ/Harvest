using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Harvest.Editor
{
    public static class GrenadeMeleeRegressionChecks
    {
        [MenuItem("Harvest/Run Grenade and Melee Checks (Play Mode)")]
        public static void Run()
        {
            if (!Application.isPlaying) { Debug.Log("Enter Play mode before running grenade/melee checks."); return; }
            var objects = new List<GameObject>();
            GrenadeDefinition definition = null;
            try
            {
                Vector3 center = new Vector3(2000f, 1f, 2000f);
                CombatTarget owner = Actor(objects, center, CombatTeam.Marine);
                CombatTarget near = Actor(objects, center + Vector3.forward * 2f, CombatTeam.Covenant);
                CombatTarget far = Actor(objects, center + Vector3.forward * 7f, CombatTeam.Covenant);
                CombatTarget ally = Actor(objects, center + Vector3.back * 2f, CombatTeam.Marine);
                definition = ScriptableObject.CreateInstance<GrenadeDefinition>();
                definition.MaxDamage = 0f;
                Physics.SyncTransforms();
                GrenadeBlast.Apply(definition, center, CombatTeam.Marine, owner);
                Expect(near.Pressure.Value, 1f, "Close blasts heavily suppress");
                if (far.Pressure.Value <= 0f || far.Pressure.Value >= near.Pressure.Value)
                    throw new Exception("Blast suppression should decrease with distance.");
                Expect(ally.Pressure.Value, 1f, "Blast pressure also affects allies");
                float uncovered = far.Pressure.Value;
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                objects.Add(wall);
                wall.transform.position = center + Vector3.forward * 4f;
                wall.transform.localScale = new Vector3(3f, 3f, 0.4f);
                Physics.SyncTransforms();
                far.Pressure.ResetPressure();
                GrenadeBlast.Apply(definition, center, CombatTeam.Marine, owner);
                Expect(far.Pressure.Value, uncovered * definition.CoveredPressureMultiplier, "Cover attenuates blast pressure");
                wall.SetActive(false);
                Physics.SyncTransforms();

                definition.MaxDamage = 100f;
                definition.DamageRadius = 6f;
                float ownerHealth = owner.GetComponent<Vitality>().Health;
                float allyHealth = ally.GetComponent<Vitality>().Health;
                float enemyHealth = near.GetComponent<Vitality>().Health;
                float distance = Vector3.Distance(center, CombatGeometry.ClosestBodyPoint(near.GetComponent<Collider>(), center, near.AimPosition));
                // A child collider must not cause duplicate damage to the same actor.
                GameObject child = new GameObject("Extra regression collider");
                child.transform.SetParent(near.transform, false);
                child.AddComponent<SphereCollider>().radius = 0.2f;
                Physics.SyncTransforms();
                GrenadeBlast.Apply(definition, center, CombatTeam.Marine, owner);
                Expect(owner.GetComponent<Vitality>().Health, ownerHealth - 100f, "Thrower receives self damage");
                Expect(ally.GetComponent<Vitality>().Health, allyHealth, "Allies retain friendly damage protection");
                Expect(near.GetComponent<Vitality>().Health, enemyHealth - 100f * GrenadeBlast.Falloff(distance, 6f), "One damage application per actor");
                far.gameObject.SetActive(false);
                ally.gameObject.SetActive(false);
                child.SetActive(false);
                near.transform.position = center + Vector3.forward * 1.8f;
                Physics.SyncTransforms();

                MeleeAttack melee = owner.gameObject.AddComponent<MeleeAttack>();
                float before = near.GetComponent<Vitality>().Health;
                if (!melee.TrySwing(center, Vector3.forward, out bool hit) || !hit)
                    throw new Exception("Visible opponent in melee range should be hit.");
                Expect(near.GetComponent<Vitality>().Health, before - melee.Damage, "Melee applies damage once");
                if (melee.TrySwing(center, Vector3.forward, out _)) throw new Exception("Melee cooldown should reject another swing.");
                CombatTarget blockedOwner = Actor(objects, center, CombatTeam.Marine);
                MeleeAttack blockedMelee = blockedOwner.gameObject.AddComponent<MeleeAttack>();
                wall.transform.position = center + Vector3.forward * 0.8f;
                wall.SetActive(true);
                Physics.SyncTransforms();
                if (!blockedMelee.TrySwing(center, Vector3.forward, out bool blockedHit) || blockedHit)
                    throw new Exception("Cover should block melee.");
                Expect(near.GetComponent<Vitality>().Health, before - melee.Damage, "Blocked melee applies no damage");
                blockedOwner.gameObject.SetActive(false);
                wall.SetActive(false);

                GrenadeInventory inventory = owner.gameObject.AddComponent<GrenadeInventory>();
                Expect(inventory.Count(GrenadeKind.Frag), 2f, "Frag starts at two");
                Expect(inventory.Count(GrenadeKind.Plasma), 2f, "Plasma starts at two");
                Expect(inventory.Refill(GrenadeKind.Frag), 0f, "Full inventory does not consume a pickup");
                inventory.Frag = definition;
                if (inventory.TryThrow(center, Vector3.right)) throw new Exception("Missing prefab should reject a throw.");
                Expect(inventory.Count(GrenadeKind.Frag), 2f, "Failed throw consumes nothing");
                GameObject prototype = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                objects.Add(prototype);
                prototype.transform.position = center + Vector3.up * 30f;
                prototype.transform.localScale = Vector3.one * 0.24f;
                prototype.AddComponent<Rigidbody>().useGravity = false;
                definition.Prefab = prototype.AddComponent<GrenadeProjectile>();
                if (!inventory.TryThrow(center, Vector3.right)) throw new Exception("Configured throw should succeed.");
                Expect(inventory.Count(GrenadeKind.Frag), 1f, "Throw consumes one selected grenade");
                if (inventory.TryThrow(center, Vector3.right)) throw new Exception("Throw cooldown should reject another grenade.");
                Expect(inventory.Count(GrenadeKind.Frag), 1f, "Cooldown rejection consumes nothing");
                Expect(inventory.Refill(GrenadeKind.Frag), 1f, "Pickup refills selected type to two");
                Expect(inventory.Count(GrenadeKind.Plasma), 2f, "Frag pickup does not change plasma count");
                inventory.SelectNext();
                if (inventory.Selected != GrenadeKind.Plasma) throw new Exception("Grenade selection should switch types.");
                Debug.Log("Grenade/melee checks passed: distance/cover suppression, self/friendly damage, actor deduplication, melee line of sight and cooldown, inventory cap, refill, selection, and throw rejection.");
            }
            catch (Exception error) { Debug.LogException(error); }
            finally
            {
                if (definition != null)
                {
                    foreach (GrenadeProjectile grenade in Object.FindObjectsByType<GrenadeProjectile>(FindObjectsSortMode.None))
                        if (grenade.Definition == definition) Object.Destroy(grenade.gameObject);
                    Object.Destroy(definition);
                }
                foreach (GameObject item in objects) if (item != null) { item.SetActive(false); Object.Destroy(item); }
            }
        }
        static CombatTarget Actor(List<GameObject> objects, Vector3 position, CombatTeam team)
        {
            GameObject root = new GameObject("Temporary grenade/melee actor");
            objects.Add(root);
            root.transform.position = position;
            CapsuleCollider body = root.AddComponent<CapsuleCollider>();
            body.height = 1.9f; body.radius = 0.4f;
            Vitality vitality = root.AddComponent<Vitality>();
            vitality.MaxHealth = 1000f; vitality.Restore();
            CombatTarget target = root.AddComponent<CombatTarget>();
            target.Team = team; target.AimOffset = Vector3.zero;
            root.AddComponent<Suppression>();
            return target;
        }
        static void Expect(float actual, float expected, string check)
        {
            if (Mathf.Abs(actual - expected) > 0.001f) throw new Exception($"{check}: expected {expected}, got {actual}.");
        }
    }
}
