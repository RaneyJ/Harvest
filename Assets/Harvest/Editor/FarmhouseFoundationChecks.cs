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
                Require(house != null, "Rebuild The Line before checking the farmhouse.");
                FarmhouseLayout l = house.Layout;
                FarmhouseFoundationBuilder.ValidateLayout(l);
                // Prefab connections and asset ownership are authoring checks, not runtime prerequisites.
                if (!Application.isPlaying)
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
                    Require(mesh != null, "Missing mesh: " + filter.name);
                    if (!Application.isPlaying)
                        Require(AssetDatabase.Contains(mesh), "Mesh must be a persistent asset: " + filter.name);
                    if (checkedMeshes.Add(mesh)) CheckMesh(mesh);
                }
                Physics.SyncTransforms();
                if (l.AuthoredPrefab == null) CheckWallSurfaces(house);
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
                if (l.AuthoredPrefab == null) {CheckDressingAccess(house);CheckRooms(house);}
                if (Application.isPlaying) CheckNavigation(house);
                Debug.Log("Farmhouse foundation checks passed: mesh geometry/UVs, generated wall coverage/UV continuity, hierarchy groups, collision separation, 1.9m body clearance, stairs and firing windows" +
                    (Application.isPlaying ? ", and downstairs-to-upstairs marine navigation." : ", persistent mesh assets and prefab connection. Run again in Play mode to also check marine navigation."));
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
        static void CheckWallSurfaces(FarmhouseFoundation house)
        {
            FarmhouseLayout l = house.Layout;
            var colliders = house.GetComponentsInChildren<BoxCollider>();
            foreach (string level in new[] { "Ground", "Upper" }) foreach (string side in new[] { "front", "rear", "road", "field" })
            {
                string name = level + " " + side + " wall";
                Transform wall = house.transform.Find("Shell/" + name);
                Require(wall != null, "Missing continuous facade: " + name + ". Rebuild version 25.");
                Require(wall.localPosition == Vector3.zero && wall.localRotation == Quaternion.identity, "Facade mesh must stay in house space: " + name);
                Mesh mesh = wall.GetComponent<MeshFilter>().sharedMesh;
                Vector3[] vertices = mesh.vertices, normals = mesh.normals; Vector2[] uv = mesh.uv;
                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector3 seed = Mathf.Abs(normals[i].y) < 0.95f ? Vector3.up : Vector3.forward;
                    Vector3 tangent = Vector3.Cross(seed, normals[i]).normalized;
                    Vector3 bitangent = Vector3.Cross(normals[i], tangent);
                    Vector2 expected = new Vector2(Vector3.Dot(vertices[i], tangent), Vector3.Dot(vertices[i], bitangent));
                    Require((uv[i] - expected).sqrMagnitude < 1e-8f, "Wall UV resets instead of continuing in house space: " + name);
                }
                bool alongZ = side == "road" || side == "field";
                int axis = alongZ ? 0 : 2, sign = side == "front" || side == "field" ? -1 : 1;
                float fixedAxis = sign * (alongZ ? l.HalfWidth : l.HalfDepth);
                float floor = level == "Ground" ? 0f : l.StoreyHeight;
                float length = alongZ ? l.HalfDepth * 2f - l.WallThickness : l.HalfWidth * 2f + l.WallThickness;
                Vector3 outward = Vector3.zero; outward[axis] = sign;
                int[] triangles = mesh.triangles;
                // Near the top/bottom and wall ends catches the former inset strips; the grid covers surrounds.
                for (int x = 0; x <= 40; x++) foreach (float y in new[] { 0.002f, 0.25f * l.StoreyHeight, 0.5f * l.StoreyHeight, 0.75f * l.StoreyHeight, l.StoreyHeight - 0.002f })
                {
                    float run = Mathf.Lerp(-length * 0.5f + 0.002f, length * 0.5f - 0.002f, x / 40f);
                    Vector3 point = alongZ ? new Vector3(fixedAxis, floor + y, run) : new Vector3(run, floor + y, fixedAxis);
                    bool solid = false;
                    foreach (BoxCollider collider in colliders)
                    {
                        if (!Mathf.Approximately(collider.size[axis], l.WallThickness) ||
                            Mathf.Abs(collider.transform.localPosition[axis] - fixedAxis) > 0.001f) continue;
                        Vector3 local = collider.transform.InverseTransformPoint(house.transform.TransformPoint(point));
                        if (new Bounds(collider.center, collider.size).Contains(local)) { solid = true; break; }
                    }
                    if (!solid) continue; // Windows and entrances remain genuinely open.
                    Vector3 origin = point + outward * (l.WallThickness * 0.5f + 0.02f);
                    bool covered = false;
                    for (int t = 0; t < triangles.Length && !covered; t += 3)
                        covered = RayTriangle(origin, -outward, vertices[triangles[t]], vertices[triangles[t+1]], vertices[triangles[t+2]], l.WallThickness + 0.04f);
                    Require(covered, "Rendered wall gap despite solid collision: " + name + " at " + point);
                }
            }
        }
        static bool RayTriangle(Vector3 origin, Vector3 direction, Vector3 a, Vector3 b, Vector3 c, float length)
        {
            Vector3 e1 = b - a, e2 = c - a, p = Vector3.Cross(direction, e2);
            float determinant = Vector3.Dot(e1, p); if (Mathf.Abs(determinant) < 1e-8f) return false;
            float inverse = 1f / determinant; Vector3 delta = origin - a;
            float u = Vector3.Dot(delta, p) * inverse; if (u < -1e-5f || u > 1f + 1e-5f) return false;
            Vector3 q = Vector3.Cross(delta, e1); float v = Vector3.Dot(direction, q) * inverse;
            if (v < -1e-5f || u + v > 1f + 1e-5f) return false;
            float distance = Vector3.Dot(e2, q) * inverse; return distance >= 0f && distance <= length;
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
        static void CheckDressingAccess(FarmhouseFoundation house)
        {
            FarmhouseLayout l=house.Layout;
            Require(house.transform.Find("Interior/Dressing") != null, "Missing dressing assembly. Rebuild version 26.");
            // Fixed circulation checks also catch placement edits that obstruct the central routes.
            for(float z=-l.HalfDepth+0.65f;z<l.HalfDepth-0.65f;z+=0.8f)
            {
                CheckBody(house,new Vector3(0f,l.GroundFloorTop,z),0f);
                CheckBody(house,new Vector3(0f,l.UpperFloorTop,z),0f);
            }
            for(float x=l.StairX;x<0f;x+=0.6f)
                CheckBody(house,new Vector3(x,l.UpperFloorTop,l.StairEnd+0.6f),0f);
            for(float x=0f;x<l.HalfWidth-0.8f;x+=0.6f)
                CheckBody(house,new Vector3(x,l.UpperFloorTop,1f),0f);
            foreach(Vector3 point in new[] {
                new Vector3(-3.6f,l.GroundFloorTop,l.HalfDepth-1.5f),
                new Vector3(2.7f,l.GroundFloorTop,l.HalfDepth-6.35f),
                new Vector3(-l.HalfWidth+2.9f,l.UpperFloorTop,l.HalfDepth-1.4f),
                new Vector3(l.HalfWidth-2.15f,l.UpperFloorTop,-l.HalfDepth+2.25f) })
                CheckBody(house,point,0f);
        }
        static void CheckRooms(FarmhouseFoundation house)
        {
            FarmhouseLayout l=house.Layout;
            Require(house.transform.Find("Interior/Rooms")!=null,"Missing room assembly. Rebuild scene 34.");
            foreach(FarmhouseRoom room in FarmhouseRoomPlan.Create(l))
            {
                Require(house.transform.Find("Interior/Rooms/"+room.Name)!=null,"Missing room: "+room.Name);
                Require(room.Right-room.Left>2f && room.Rear-room.Front>2f && room.DoorWidth>=1.5f,"Room dimensions or doorway are too narrow: "+room.Name);
                float start=room.DoorAlongZ?room.Front:room.Left,end=room.DoorAlongZ?room.Rear:room.Right;
                Require(room.DoorRun-room.DoorWidth*.5f>start+.10f && room.DoorRun+room.DoorWidth*.5f<end-.10f,"Door casing does not fit its partition: "+room.Name);
                CheckBody(house,room.Entry-room.Inward*.65f,0f);
                CheckBody(house,room.Entry,0f);
                CheckBody(house,room.Entry+room.Inward*.65f,0f);
                CheckSightline(house,room.Entry-room.Inward*.65f+Vector3.up*1.6f,room.Entry+room.Inward*.65f+Vector3.up*1.6f);
            }
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
            if(l.AuthoredPrefab==null) foreach(FarmhouseRoom room in FarmhouseRoomPlan.Create(l))
            {
                Vector3 target=house.transform.TransformPoint(room.Entry+room.Inward*.65f);
                Require(NavMesh.SamplePosition(target,out NavMeshHit roomHit,.35f,NavMesh.AllAreas),"Marine navigation is missing inside "+room.Name);
                Require(NavMesh.CalculatePath(start.position,roomHit.position,NavMesh.AllAreas,path) && path.status==NavMeshPathStatus.PathComplete,"Marine cannot enter "+room.Name);
            }
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    }
}
