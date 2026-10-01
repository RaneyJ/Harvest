using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Harvest.Editor
{
    // Low visual aprons close abrupt surface edges; movement continues on existing colliders.
    public static class FarmGroundTransitions
    {
        public static void Build(Material soil)
        {
            var house=Object.FindFirstObjectByType<FarmhouseFoundation>();
            if(house!=null && house.Layout!=null && house.Layout.AuthoredPrefab==null)
            {
                FarmhouseLayout l=house.Layout;
                Ring("Farmhouse soil contact",l.WorldOrigin,new Vector2(l.HalfWidth+.095f,l.HalfDepth+.095f),.50f,.026f,soil);
                float width=l.HalfWidth*2f-1.2f;
                Ring("Porch soil contact",l.WorldOrigin+new Vector3(0,0,-l.HalfDepth-l.PorchDepth*.5f),new Vector2(width*.5f,l.PorchDepth*.5f),.30f,.005f,soil);
            }
            Ring("Access track feathered edges",new Vector3(-12,0,-12),new Vector2(5,2),.35f,.058f,soil);
            foreach(int side in new[]{-1,1})
                Strip("Road shoulder feather "+side,new Vector3(side*9.3f,0,-55),new Vector3(side*9.3f,0,75),Vector3.right*side,.45f,.038f,soil);
        }
        static void Ring(string name,Vector3 origin,Vector2 half,float width,float top,Material material)
        {
            var v=new List<Vector3>();var t=new List<int>();
            var inner=new[]{new Vector3(-half.x,top,-half.y),new Vector3(half.x,top,-half.y),new Vector3(half.x,top,half.y),new Vector3(-half.x,top,half.y)};
            var outer=new[]{new Vector3(-half.x-width,-.049f,-half.y-width),new Vector3(half.x+width,-.049f,-half.y-width),
                new Vector3(half.x+width,-.049f,half.y+width),new Vector3(-half.x-width,-.049f,half.y+width)};
            for(int i=0;i<4;i++) {int j=(i+1)%4;Quad(v,t,inner[i]+origin,inner[j]+origin,outer[j]+origin,outer[i]+origin);}
            Save(name,v,t,material);
        }
        static void Strip(string name,Vector3 from,Vector3 to,Vector3 outward,float width,float top,Material material)
        {
            var v=new List<Vector3>();var t=new List<int>();
            Quad(v,t,from+Vector3.up*top,to+Vector3.up*top,to+outward*width-Vector3.up*.049f,from+outward*width-Vector3.up*.049f);Save(name,v,t,material);
        }
        static void Quad(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c,Vector3 d)
        {
            int n=v.Count;v.Add(a);v.Add(b);v.Add(c);v.Add(d);
            bool flip=Vector3.Cross(b-a,c-a).y<0;
            t.Add(n);t.Add(n+(flip?2:1));t.Add(n+(flip?1:2));t.Add(n);t.Add(n+(flip?3:2));t.Add(n+(flip?2:3));
        }
        static void Save(string name,List<Vector3> v,List<int> t,Material material)
        {
            const string folder="Assets/Harvest/Art/Generated/FarmGround";FarmhouseMeshLibrary.EnsureFolder(folder);
            string path=folder+"/"+name.Replace(" ","_")+".asset";Mesh mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);bool create=mesh==null;
            if(create) mesh=new Mesh();mesh.Clear();mesh.name=name;mesh.SetVertices(v);mesh.SetTriangles(t,0);
            var uv=new List<Vector2>();foreach(Vector3 p in v) uv.Add(new Vector2(p.x*.08f,p.z*.08f));mesh.SetUVs(0,uv);
            mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.RecalculateTangents();if(create) AssetDatabase.CreateAsset(mesh,path);EditorUtility.SetDirty(mesh);
            var go=new GameObject(name);go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic);
        }
    }
}
