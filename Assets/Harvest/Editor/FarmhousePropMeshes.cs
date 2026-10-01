using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Harvest.Editor
{
    public static class FarmhousePropMeshes
    {
        const string Folder = "Assets/Harvest/Art/Generated/FarmhouseProps";
        // Closed profiles run from the underside center, around the outside/lip, to the inside center.
        public static Mesh Vessel(string name, float radius, float height, float thickness)
        {
            if (float.IsNaN(radius) || float.IsInfinity(radius) || float.IsNaN(height) || float.IsInfinity(height) ||
                float.IsNaN(thickness) || float.IsInfinity(thickness) || thickness <= 0f || radius <= thickness || height <= thickness)
                throw new ArgumentException("Vessel dimensions must be finite and leave room for the hollow interior.");
            FarmhouseMeshLibrary.EnsureFolder(Folder);
            string key = name + "_" + radius.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + "_" + height.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + "_" + thickness.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
            string path = Folder + "/" + key + ".asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (existing != null) return existing;
            var profile = new[] { new Vector2(0f,0f), new Vector2(radius,0f), new Vector2(radius,height),
                new Vector2(radius-thickness,height), new Vector2(radius-thickness,thickness), new Vector2(0f,thickness) };
            const int sides = 24;
            var vertices = new List<Vector3>(); var normals = new List<Vector3>();
            var uv = new List<Vector2>(); var triangles = new List<int>();
            float distance = 0f;
            for (int j = 0; j < profile.Length - 1; j++)
            {
                Vector2 delta = profile[j+1] - profile[j]; float length = delta.magnitude;
                Vector2 n = new Vector2(delta.y, -delta.x).normalized;
                for (int i = 0; i < sides; i++)
                {
                    int first = vertices.Count;
                    foreach (var pair in new[] { new Vector2(i,j), new Vector2(i+1,j), new Vector2(i+1,j+1), new Vector2(i,j+1) })
                    {
                        float angle = pair.x * Mathf.PI * 2f / sides; Vector2 point = profile[(int)pair.y];
                        vertices.Add(new Vector3(Mathf.Cos(angle)*point.x, point.y, Mathf.Sin(angle)*point.x));
                        normals.Add(new Vector3(Mathf.Cos(angle)*n.x, n.y, Mathf.Sin(angle)*n.x));
                        uv.Add(new Vector2(angle*radius, distance + (pair.y-j)*length));
                    }
                    Triangle(first,first+1,first+2,vertices,normals,triangles);
                    Triangle(first,first+2,first+3,vertices,normals,triangles);
                }
                distance += length;
            }
            // Pole quads contain one degenerate triangle. Remove their unused duplicate pole vertices
            // so all persisted vertices participate in valid triangles and receive valid tangents.
            var used = new bool[vertices.Count]; foreach (int index in triangles) used[index]=true;
            var remap=new int[vertices.Count]; int next=0;
            for(int i=0;i<used.Length;i++) if(used[i])
            { remap[i]=next;vertices[next]=vertices[i];normals[next]=normals[i];uv[next]=uv[i];next++; }
            vertices.RemoveRange(next,vertices.Count-next);normals.RemoveRange(next,normals.Count-next);uv.RemoveRange(next,uv.Count-next);
            for(int i=0;i<triangles.Count;i++) triangles[i]=remap[triangles[i]];
            var mesh = new Mesh { name = key };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0,uv); mesh.SetTriangles(triangles,0);
            mesh.RecalculateBounds();
            UnwrapParam.SetDefaults(out UnwrapParam unwrap); unwrap.packMargin = 0.02f;
            if (!Unwrapping.GenerateSecondaryUVSet(mesh,unwrap)) throw new InvalidOperationException("Prop lightmap unwrap failed: " + name);
            mesh.RecalculateTangents(); AssetDatabase.CreateAsset(mesh,path); return mesh;
        }
        static void Triangle(int a,int b,int c,List<Vector3> v,List<Vector3> n,List<int> indices)
        {
            Vector3 cross = Vector3.Cross(v[b]-v[a],v[c]-v[a]); if (cross.sqrMagnitude < 1e-12f) return;
            if (Vector3.Dot(cross,n[a]+n[b]+n[c]) < 0f) { int swap=b;b=c;c=swap; }
            indices.Add(a);indices.Add(b);indices.Add(c);
        }
    }
}
