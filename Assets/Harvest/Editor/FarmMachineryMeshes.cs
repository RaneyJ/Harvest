using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Harvest.Editor
{
    // Profiles are radius/height pairs, closed from underside pole around the exterior to the top pole.
    public static class FarmMachineryMeshes
    {
        public static Mesh Revolve(string name, params Vector2[] profile)
        {
            const string folder="Assets/Harvest/Art/Generated/FarmMachinery";
            FarmhouseMeshLibrary.EnsureFolder(folder);
            const int sides=48;
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
            float distance=0f;
            for(int j=0;j<profile.Length-1;j++)
            {
                Vector2 delta=profile[j+1]-profile[j];float length=delta.magnitude;
                if(length<.0001f) throw new ArgumentException("Zero-length machinery profile edge: "+name);
                Vector2 normal=new Vector2(delta.y,-delta.x).normalized;
                float uvRadius=Mathf.Max(profile[j].x,profile[j+1].x);
                for(int i=0;i<sides;i++)
                {
                    int first=vertices.Count;
                    foreach(Vector2 pair in new[]{new Vector2(i,j),new Vector2(i+1,j),new Vector2(i+1,j+1),new Vector2(i,j+1)})
                    {
                        float angle=pair.x*Mathf.PI*2f/sides;Vector2 point=profile[(int)pair.y];
                        vertices.Add(new Vector3(Mathf.Cos(angle)*point.x,point.y,Mathf.Sin(angle)*point.x));
                        normals.Add(new Vector3(Mathf.Cos(angle)*normal.x,normal.y,Mathf.Sin(angle)*normal.x));
                        uv.Add(new Vector2(angle*uvRadius,distance+(pair.y-j)*length));
                    }
                    Triangle(first,first+1,first+2,vertices,normals,indices);Triangle(first,first+2,first+3,vertices,normals,indices);
                }
                distance+=length;
            }
            var used=new bool[vertices.Count];foreach(int index in indices) used[index]=true;
            var remap=new int[vertices.Count];int next=0;
            for(int i=0;i<used.Length;i++) if(used[i])
            { remap[i]=next;vertices[next]=vertices[i];normals[next]=normals[i];uv[next]=uv[i];next++; }
            vertices.RemoveRange(next,vertices.Count-next);normals.RemoveRange(next,normals.Count-next);uv.RemoveRange(next,uv.Count-next);
            for(int i=0;i<indices.Count;i++) indices[i]=remap[indices[i]];
            string path=folder+"/"+name+".asset";Mesh mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);bool create=mesh==null;
            if(create) mesh=new Mesh();mesh.Clear();mesh.name=name;
            mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();
            UnwrapParam.SetDefaults(out UnwrapParam unwrap);unwrap.packMargin=.01f;
            if(!Unwrapping.GenerateSecondaryUVSet(mesh,unwrap)) throw new InvalidOperationException("Machinery UV unwrap failed: "+name);
            mesh.RecalculateTangents();if(create) AssetDatabase.CreateAsset(mesh,path);EditorUtility.SetDirty(mesh);return mesh;
        }
        static void Triangle(int a,int b,int c,List<Vector3> v,List<Vector3> n,List<int> indices)
        {
            Vector3 cross=Vector3.Cross(v[b]-v[a],v[c]-v[a]);if(cross.sqrMagnitude<1e-12f) return;
            if(Vector3.Dot(cross,n[a]+n[b]+n[c])<0f){int swap=b;b=c;c=swap;}
            indices.Add(a);indices.Add(b);indices.Add(c);
        }
    }
}
