using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Harvest.Editor
{
    // One combined mesh per floor section, with a world-aligned board grid and deterministic UV phase.
    public static class FarmhouseFloorboards
    {
        const float Width=.20f, Length=2.3f, Gap=.002f, Thickness=.025f;
        public static void Build(Transform parent,string name,Vector3 center,Vector3 size,Material timber)
        {
            Material floor=FloorMaterial(timber);
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
            float xmin=center.x-size.x*.5f,xmax=center.x+size.x*.5f;
            float zmin=center.z-size.z*.5f,zmax=center.z+size.z*.5f;
            float top=center.y+size.y*.5f;
            for(int row=Mathf.FloorToInt(xmin/Width);row*Width<xmax-.0001f;row++)
            {
                float x0=Mathf.Max(xmin,row*Width),x1=Mathf.Min(xmax,(row+1)*Width);
                if(x1-x0<.01f) continue; // Only tiny perimeter offcuts hidden beneath the wall.
                float stagger=Mod(row,3)*(Length/3f);
                int first=Mathf.FloorToInt((zmin-stagger)/Length);
                for(int board=first;;board++)
                {
                    float z0=Mathf.Max(zmin,board*Length+stagger),z1=Mathf.Min(zmax,(board+1)*Length+stagger);
                    if(z0>=zmax-.0001f) break;
                    if(z1-z0<.15f && z1<zmax-.0001f) { z1=Mathf.Min(zmax,z1+Length);board++; }
                    if(zmax-z1<.15f) z1=zmax;
                    if(z1-z0>.01f)
                    {
                        Vector3 dimensions=new Vector3(x1-x0-Gap,Thickness,z1-z0-Gap);
                        Mesh source=FarmhouseMeshLibrary.Box(dimensions,.0015f);
                        Vector3[] v=source.vertices,n=source.normals;Vector2[] u=source.uv;int[] t=source.triangles;
                        var remap=new Dictionary<int,int>();Vector2 phase=Phase(row,board);
                        Vector3 position=new Vector3((x0+x1)*.5f,top-Thickness*.5f,(z0+z1)*.5f);
                        // The backing fills the section. Keep only the plank top/chamfer faces visible above it.
                        for(int i=0;i<t.Length;i+=3)
                        {
                            if(v[t[i]].y<Thickness*.5f-.003f || v[t[i+1]].y<Thickness*.5f-.003f || v[t[i+2]].y<Thickness*.5f-.003f) continue;
                            for(int k=0;k<3;k++)
                            {
                                int old=t[i+k];
                                if(!remap.TryGetValue(old,out int next))
                                {
                                    next=vertices.Count;remap.Add(old,next);
                                    vertices.Add(v[old]+position);normals.Add(n[old]);uv.Add(u[old]+phase);
                                }
                                indices.Add(next);
                            }
                        }
                    }
                    if(z1>=zmax-.0001f) break;
                }
            }
            FarmhouseMeshLibrary.EnsureFolder("Assets/Harvest/Art/Generated/Farmhouse");
            string path="Assets/Harvest/Art/Generated/Farmhouse/Planks_"+name.Replace(" ","_")+".asset";
            Mesh mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);bool create=mesh==null;
            if(create) mesh=new Mesh();mesh.Clear();mesh.name="Planks "+name;mesh.indexFormat=IndexFormat.UInt32;
            mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();
            UnwrapParam.SetDefaults(out UnwrapParam unwrap);unwrap.packMargin=.002f;
            if(!Unwrapping.GenerateSecondaryUVSet(mesh,unwrap)) throw new InvalidOperationException("Floorboard lightmap unwrap failed: "+name);
            mesh.RecalculateTangents();if(create) AssetDatabase.CreateAsset(mesh,path);EditorUtility.SetDirty(mesh);
            Visual(parent,name+" staggered planks",mesh,floor,Vector3.zero);
            // The flat backing closes narrow joints; the unchanged full-depth collider owns movement.
            Visual(parent,name+" backing",FarmhouseMeshLibrary.Box(new Vector3(size.x,size.y-.003f,size.z),.0015f),timber,center-Vector3.up*.0015f);
        }
        static int Mod(int value,int modulus) => (value%modulus+modulus)%modulus;
        static Vector2 Phase(int row,int board)
        {
            unchecked
            {
                uint h=(uint)(row*73856093 ^ board*19349663 ^ 2701);
                h=(h^(h>>16))*0x7feb352du;h=(h^(h>>15))*0x846ca68bu;h^=h>>16;
                return new Vector2((h&65535u)/65536f*.5f,(h>>16)/65536f*2f);
            }
        }
        static Material FloorMaterial(Material timber)
        {
            FarmhouseMeshLibrary.EnsureFolder("Assets/Harvest/Materials");
            const string path="Assets/Harvest/Materials/Farmhouse Plank Floor.mat";
            var floor=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(floor==null)
            {
                floor=new Material(timber){name="Farmhouse Plank Floor"};
                if(floor.HasProperty("_BaseMap"))
                { Vector2 scale=floor.GetTextureScale("_BaseMap");floor.SetTextureScale("_BaseMap",new Vector2(scale.x,scale.y*.25f)); }
                AssetDatabase.CreateAsset(floor,path);return floor;
            }
            // A fresh clone may be built before the source installation; populate only empty map slots.
            if(floor.shader.name=="Universal Render Pipeline/Lit" && timber.shader.name=="Universal Render Pipeline/Lit" &&
                floor.GetTexture("_BaseMap")==null && floor.GetTexture("_BumpMap")==null && floor.GetTexture("_MetallicGlossMap")==null &&
                floor.GetTexture("_OcclusionMap")==null && timber.GetTexture("_BaseMap")!=null)
            {
                foreach(string property in new[]{"_BaseMap","_BumpMap","_MetallicGlossMap","_OcclusionMap"}) floor.SetTexture(property,timber.GetTexture(property));
                foreach(string keyword in new[]{"_NORMALMAP","_METALLICSPECGLOSSMAP","_OCCLUSIONMAP"})
                    if(timber.IsKeywordEnabled(keyword)) floor.EnableKeyword(keyword);
                if(floor.GetTextureScale("_BaseMap")==new Vector2(1f,.25f))
                { Vector2 scale=timber.GetTextureScale("_BaseMap");floor.SetTextureScale("_BaseMap",new Vector2(scale.x,scale.y*.25f)); }
                if(Mathf.Approximately(floor.GetFloat("_Smoothness"),.18f)) floor.SetFloat("_Smoothness",timber.GetFloat("_Smoothness"));
                foreach(string property in new[]{"_BumpScale","_OcclusionStrength"})
                    if(Mathf.Approximately(floor.GetFloat(property),1f)) floor.SetFloat(property,timber.GetFloat(property));
                Color old=floor.GetColor("_BaseColor");
                if((new Vector3(old.r,old.g,old.b)-new Vector3(.30f,.24f,.16f)).sqrMagnitude<1e-6f) floor.SetColor("_BaseColor",timber.GetColor("_BaseColor"));
                EditorUtility.SetDirty(floor);
            }
            return floor;
        }
        static void Visual(Transform parent,string name,Mesh mesh,Material material,Vector3 position)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;
            GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic);
        }
    }
}
