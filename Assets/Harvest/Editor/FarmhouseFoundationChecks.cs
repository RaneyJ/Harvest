using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Harvest.Editor
{
    public static class FarmhouseFoundationChecks
    {
        [MenuItem("Harvest/Validate Farmhouse Foundation")]
        public static void Run()
        {
            try
            {
                var house = UnityEngine.Object.FindFirstObjectByType<FarmhouseFoundation>();
                Require(house != null, "Rebuild The Line (version 23) before checking the farmhouse.");
                FarmhouseLayout l = house.Layout;
                FarmhouseFoundationBuilder.ValidateLayout(l);
                Require(PrefabUtility.GetCorrespondingObjectFromSource(house.gameObject) != null, "Farmhouse should be a connected prefab instance.");
                foreach (string group in new[] { "Shell", "Roof", "Porch", "Interior", "Trim", "Collision" })
                    Require(house.transform.Find(group) != null, "Missing modular group: " + group);
                Transform collision = house.transform.Find("Collision");
                foreach (Collider collider in house.GetComponentsInChildren<Collider>())
                    Require(collider.transform.IsChildOf(collision), "Visual mesh has unintended collision: " + collider.name);
                var checkedMeshes = new HashSet<Mesh>();
                foreach (MeshFilter filter in house.GetComponentsInChildren<MeshFilter>())
                {
                    Require(filter.transform.localScale == Vector3.one, "Dimensioned mesh should not be scaled: " + filter.name);
                    Mesh mesh = filter.sharedMesh;
                    Require(mesh != null && AssetDatabase.Contains(mesh), "Mesh must be a persistent asset: " + filter.name);
                    if (checkedMeshes.Add(mesh)) CheckMesh(mesh);
                }
                Physics.SyncTransforms();
                foreach (Vector3 feet in new[] {
                    new Vector3(0f,l.GroundFloorTop,-l.HalfDepth-1.3f),
                    new Vector3(0f,l.GroundFloorTop,-l.HalfDepth+0.6f),
                    new Vector3(-2.1f,l.GroundFloorTop,l.StairStart-0.65f),
                    new Vector3(l.StairX,l.GroundFloorTop,l.StairStart-0.65f),
                    new Vector3(0f,l.GroundFloorTop,l.HalfDepth-0.5f),
                    new Vector3(0f,l.GroundFloorTop,l.HalfDepth+0.5f),
                    new Vector3(l.StairX,l.UpperFloorTop,l.StairEnd+0.6f),
                    new Vector3(l.StairwellRight+0.9f,l.UpperFloorTop,l.StairEnd+0.6f),
                    new Vector3(l.HalfWidth-0.9f,l.UpperFloorTop,1f),
                    new Vector3(1f,l.UpperFloorTop,-l.HalfDepth+0.9f) })
                    CheckBody(house, feet, 0f);
                for (int i = 0; i < l.StairCount; i++) CheckBody(house, l.StairPoint(i), l.Riser + 0.025f);
                // Standing eye level for the prototype's 1.9m capsule and 0.65m camera offset.
                CheckSightline(house, new Vector3(l.HalfWidth-0.9f,l.UpperFloorTop+1.6f,1f),
                    new Vector3(l.HalfWidth+2f,l.UpperFloorTop+1.6f,1f));
                CheckSightline(house, new Vector3(1f,l.UpperFloorTop+1.6f,-l.HalfDepth+0.9f),
                    new Vector3(1f,l.UpperFloorTop+1.6f,-l.HalfDepth-2f));
                if (Application.isPlaying) CheckNavigation(house);
                Debug.Log("Farmhouse foundation checks passed: persistent meshes/UVs, prefab groups, collision separation, 1.9m body clearance, stairs and firing windows" +
                    (Application.isPlaying ? ", and downstairs-to-upstairs marine navigation." : ". Run again in Play mode to also check marine navigation."));
            }
            catch (Exception error) { Debug.LogError("Farmhouse foundation check failed: " + error.Message); }
        }
        static void CheckMesh(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices, normals = mesh.normals;
            Vector2[] uv = mesh.uv, uv2 = mesh.uv2;
            Vector4[] tangents = mesh.tangents;
            Require(vertices.Length > 0 && normals.Length == vertices.Length && uv.Length == vertices.Length &&
                uv2.Length == vertices.Length && tangents.Length == vertices.Length, "Missing normals, tangents or UV channels: " + mesh.name);
            for (int i = 0; i < vertices.Length; i++)
            {
                Require(Finite(vertices[i].x) && Finite(vertices[i].y) && Finite(vertices[i].z) &&
                    Finite(uv[i].x) && Finite(uv[i].y) && Finite(uv2[i].x) && Finite(uv2[i].y) &&
                    Finite(tangents[i].x) && Finite(tangents[i].y) && Finite(tangents[i].z), "Nonfinite mesh data: " + mesh.name);
                Require(normals[i].sqrMagnitude > 0.9f && tangents[i].x*tangents[i].x + tangents[i].y*tangents[i].y + tangents[i].z*tangents[i].z > 0.5f,
                    "Invalid normal or tangent: " + mesh.name);
            }
            int[] indices = mesh.triangles;
            for (int i = 0; i < indices.Length; i += 3)
            {
                Vector3 cross = Vector3.Cross(vertices[indices[i+1]]-vertices[indices[i]], vertices[indices[i+2]]-vertices[indices[i]]);
                Require(cross.sqrMagnitude > 1e-12f && Vector3.Dot(cross.normalized, normals[indices[i]]) > 0.95f,
                    "Degenerate or inverted triangle: " + mesh.name);
            }
        }
        static void CheckBody(FarmhouseFoundation house, Vector3 localFeet, float allowedStep)
        {
            Vector3 feet = house.transform.TransformPoint(localFeet);
            const float radius = 0.39f, height = 1.9f;
            foreach (Collider hit in Physics.OverlapCapsule(feet + Vector3.up * (radius + 0.025f),
                feet + Vector3.up * (height - radius + 0.025f), radius, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.GetComponentInParent<CombatTarget>() != null) continue;
                // Adjacent stair risers are traversable contacts, not headroom blockers.
                if (hit.bounds.max.y <= feet.y + allowedStep + 0.01f) continue;
                throw new Exception("Body clearance blocked at " + localFeet + " by " + hit.name);
            }
            bool supported = false;
            foreach (RaycastHit hit in Physics.RaycastAll(feet + Vector3.up * 0.12f, Vector3.down, 0.25f, ~0, QueryTriggerInteraction.Ignore))
                if (hit.collider.transform.IsChildOf(house.transform)) supported = true;
            Require(supported, "Missing farmhouse floor under " + localFeet);
        }
        static void CheckSightline(FarmhouseFoundation house, Vector3 from, Vector3 to)
        {
            Vector3 origin = house.transform.TransformPoint(from), delta = house.transform.TransformPoint(to) - origin;
            foreach (RaycastHit hit in Physics.RaycastAll(origin, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
                Require(!hit.collider.transform.IsChildOf(house.transform), "Firing window is blocked by " + hit.collider.name);
        }
        static void CheckNavigation(FarmhouseFoundation house)
        {
            FarmhouseLayout l = house.Layout;
            Vector3 entry = house.transform.TransformPoint(new Vector3(0f,l.GroundFloorTop,-l.HalfDepth+0.8f));
            Vector3 upstairs = house.transform.TransformPoint(new Vector3(l.HalfWidth-0.9f,l.UpperFloorTop,1f));
            Require(NavMesh.SamplePosition(entry, out NavMeshHit start, 0.75f, NavMesh.AllAreas), "Marine navigation is missing at the farmhouse entrance.");
            Require(NavMesh.SamplePosition(upstairs, out NavMeshHit end, 0.75f, NavMesh.AllAreas), "Marine navigation is missing on the upper floor.");
            var path = new NavMeshPath();
            Require(NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete,
                "Marine cannot reach the upstairs window from the entrance.");
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    }
}
