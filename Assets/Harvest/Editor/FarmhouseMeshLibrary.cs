using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace Harvest.Editor
{
    // Mesh dimensions are baked into vertices: reusable geometry, meter-scale UV0, independent UV2.
    public static class FarmhouseMeshLibrary
    {
        const string Folder = "Assets/Harvest/Art/Generated/Farmhouse";
        static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();
        public static void BeginBuild() { Cache.Clear(); EnsureFolder(Folder); }
        public static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string parent = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string child = parent + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(child)) AssetDatabase.CreateFolder(parent, parts[i]);
                parent = child;
            }
        }
        static string Number(float value) => value.ToString("F4", CultureInfo.InvariantCulture);
        public static Mesh Box(Vector3 size, float bevel = 0.015f, bool rotateUV = false)
        {
            Vector3 h = size * 0.5f;
            bevel = Mathf.Clamp(bevel, 0.0001f, Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * 0.45f);
            string key = "Box_" + Number(size.x) + "_" + Number(size.y) + "_" + Number(size.z) + "_" + Number(bevel) + (rotateUV ? "_RotatedUV" : "");
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;
            Vector3 inner = h - Vector3.one * bevel;
            var data = new Geometry();
            for (int axis = 0; axis < 3; axis++)
                foreach (int sign in new[] { -1, 1 })
                {
                    int a = (axis + 1) % 3, b = (axis + 2) % 3;
                    Vector3 normal = Vector3.zero; normal[axis] = sign;
                    var points = new Vector3[4];
                    int[] sa = { -1, 1, 1, -1 }, sb = { -1, -1, 1, 1 };
                    for (int i = 0; i < 4; i++)
                    { points[i][axis] = sign * h[axis]; points[i][a] = sa[i] * inner[a]; points[i][b] = sb[i] * inner[b]; }
                    data.Face(normal, points);
                }
            // Twelve edge strips. Their normals catch light independently of the six broad faces.
            for (int a = 0; a < 3; a++)
                for (int b = a + 1; b < 3; b++)
                    foreach (int sa in new[] { -1, 1 })
                        foreach (int sb in new[] { -1, 1 })
                        {
                            int c = 3 - a - b;
                            Vector3 p = Vector3.zero, q = Vector3.zero, normal = Vector3.zero;
                            p[a] = sa * h[a]; p[b] = sb * inner[b];
                            q[a] = sa * inner[a]; q[b] = sb * h[b];
                            normal[a] = sa; normal[b] = sb;
                            Vector3 p0 = p, p1 = p, q0 = q, q1 = q;
                            p0[c] = q0[c] = -inner[c]; p1[c] = q1[c] = inner[c];
                            data.Face(normal.normalized, p0, p1, q1, q0);
                        }
            foreach (int x in new[] { -1, 1 })
                foreach (int y in new[] { -1, 1 })
                    foreach (int z in new[] { -1, 1 })
                    {
                        Vector3 p = Vector3.Scale(inner, new Vector3(x, y, z));
                        data.Face(new Vector3(x, y, z).normalized,
                            p + Vector3.right * (x * bevel), p + Vector3.up * (y * bevel), p + Vector3.forward * (z * bevel));
                    }
            if (rotateUV)
                for (int i = 0; i < data.UV.Count; i++) data.UV[i] = new Vector2(-data.UV[i].y, data.UV[i].x);
            return Save(key, data);
        }
        public static Mesh Gable(float width, float height, float thickness)
        {
            string key = "Gable_" + Number(width) + "_" + Number(height) + "_" + Number(thickness);
            if (Cache.TryGetValue(key, out Mesh cached)) return cached;
            float w = width * 0.5f, d = thickness * 0.5f;
            Vector3[] front = { new Vector3(-w,0f,-d), new Vector3(w,0f,-d), new Vector3(0f,height,-d) };
            Vector3[] back = { new Vector3(-w,0f,d), new Vector3(w,0f,d), new Vector3(0f,height,d) };
            var data = new Geometry();
            data.Face(Vector3.back, front); data.Face(Vector3.forward, back);
            for (int i = 0; i < 3; i++)
            {
                int next = (i + 1) % 3;
                Vector3 edge = front[next] - front[i];
                data.Face(new Vector3(edge.y, -edge.x, 0f).normalized, front[i], front[next], back[next], back[i]);
            }
            return Save(key, data);
        }
        static Mesh Save(string key, Geometry data)
        {
            string path = Folder + "/" + key + ".asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool create = mesh == null;
            if (create) mesh = new Mesh();
            mesh.Clear(); mesh.name = key;
            mesh.SetVertices(data.Vertices); mesh.SetNormals(data.Normals); mesh.SetUVs(0, data.UV);
            mesh.SetTriangles(data.Triangles, 0); mesh.RecalculateBounds();
            UnwrapParam.SetDefaults(out UnwrapParam unwrap);
            unwrap.packMargin = 0.02f;
            if (!Unwrapping.GenerateSecondaryUVSet(mesh, unwrap))
                throw new InvalidOperationException("Farmhouse lightmap UV generation failed: " + key);
            mesh.RecalculateTangents();
            if (create) AssetDatabase.CreateAsset(mesh, path);
            EditorUtility.SetDirty(mesh); Cache[key] = mesh; return mesh;
        }
        sealed class Geometry
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<Vector3> Normals = new List<Vector3>();
            public readonly List<Vector2> UV = new List<Vector2>();
            public readonly List<int> Triangles = new List<int>();
            public void Face(Vector3 normal, params Vector3[] points)
            {
                points = (Vector3[])points.Clone(); // Preserve input topology for callers sharing face vertices.
                if (Vector3.Dot(Vector3.Cross(points[1] - points[0], points[2] - points[0]), normal) < 0f)
                    Array.Reverse(points);
                Vector3 seed = Mathf.Abs(normal.y) < 0.95f ? Vector3.up : Vector3.forward;
                Vector3 tangent = Vector3.Cross(seed, normal).normalized;
                Vector3 bitangent = Vector3.Cross(normal, tangent);
                int first = Vertices.Count;
                foreach (Vector3 point in points)
                { Vertices.Add(point); Normals.Add(normal); UV.Add(new Vector2(Vector3.Dot(point, tangent), Vector3.Dot(point, bitangent))); }
                for (int i = 1; i < points.Length - 1; i++)
                { Triangles.Add(first); Triangles.Add(first + i); Triangles.Add(first + i + 1); }
            }
        }
    }
}
