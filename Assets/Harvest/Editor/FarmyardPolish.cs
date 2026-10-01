using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace Harvest.Editor
{
    // Ground accents and bounded dressing remain independent of terrain, actors and encounter waves.
    public static class FarmyardPolish
    {
        public const string ProfilePath="Assets/Harvest/Data/Farmyard Art Profile.asset";
        const string Folder="Assets/Harvest/Art/Generated/Farmyard";
        public static void Build()
        {
            FarmhouseMeshLibrary.EnsureFolder(Folder);
            var profile=AssetDatabase.LoadAssetAtPath<FarmyardArtProfile>(ProfilePath);
            if(profile==null){profile=ScriptableObject.CreateInstance<FarmyardArtProfile>();AssetDatabase.CreateAsset(profile,ProfilePath);}
            foreach(GameObject prefab in new[]{profile.SupplyPrefab,profile.VegetationPrefab,profile.GroundDebrisPrefab}) Validate(prefab);
            var house=Object.FindFirstObjectByType<FarmhouseFoundation>();if(house==null) return;
            FarmhouseLayout l=house.Layout;var root=new GameObject("Farmyard visual polish").transform;
            var soil=AssetDatabase.LoadAssetAtPath<Material>("Assets/Harvest/Materials/Soil.mat");
            if(profile.EnableTracks) Tracks(root,Surface(soil));
            if(profile.EnableBoundaryVegetation) Vegetation(root,l,profile);
            if(profile.EnableYardSupplies)
            {
                Material timber=AssetDatabase.LoadAssetAtPath<Material>(FarmhouseMaterialLibrary.MaterialPath("Timber"));
                Material steel=AssetDatabase.LoadAssetAtPath<Material>(FarmhouseMaterialLibrary.MaterialPath("Steel"));
                foreach(Vector3 offset in new[]{new Vector3(-4.3f,0,-l.HalfDepth-l.PorchDepth-.6f),new Vector3(4.3f,0,-l.HalfDepth-l.PorchDepth-4f)})
                {
                    Vector3 position=l.WorldOrigin+offset;position.y=Height(position.x,position.z);
                    var cluster=new GameObject("Yard supply cluster").transform;cluster.SetParent(root,false);cluster.localPosition=position;
                    if(profile.SupplyPrefab!=null) Instance(profile.SupplyPrefab,cluster,Vector3.zero,0);
                    else
                    {
                        Box(cluster,"Seed transit crate",timber,new Vector3(0,.325f,0),new Vector3(1.30f,.65f,.70f));
                        Box(cluster,"Crate lid",timber,new Vector3(0,.675f,0),new Vector3(1.34f,.05f,.74f));
                        foreach(float x in new[]{-.43f,.43f})
                        {
                            Box(cluster,"Crate metal band",steel,new Vector3(x,.325f,-.359f),new Vector3(.035f,.65f,.018f));
                            Box(cluster,"Crate top band",steel,new Vector3(x,.704f,0),new Vector3(.035f,.014f,.74f));
                        }
                        foreach(float y in new[]{.16f,.32f,.48f}) Box(cluster,"Crate board join",steel,new Vector3(0,y,-.356f),new Vector3(1.23f,.012f,.012f));
                    }
                    var proxy=new GameObject("Yard supply collision");proxy.transform.SetParent(cluster,false);proxy.AddComponent<BoxCollider>().center=new Vector3(0,.35f,0);proxy.GetComponent<BoxCollider>().size=new Vector3(1.4f,.70f,.8f);
                }
            }
            if(profile.GroundDebrisPrefab!=null)
                foreach(Vector3 position in new[]{new Vector3(-28,-.05f,-18),new Vector3(11,-.05f,-21),new Vector3(28.8f,-.05f,11)})
                    Instance(profile.GroundDebrisPrefab,root,position,position.z*7);
        }
        static void Tracks(Transform root,Material material)
        {
            var v=new List<Vector3>();var t=new List<int>();
            foreach(float x in new[]{-2.65f,2.65f})
                for(int i=0;i<59;i++)
                {
                    float z=-51+i*2f,jitter=Mathf.Sin(i*.37f)*.08f;
                    Quad(v,t,new Vector3(x+jitter-.14f,.0415f,z),new Vector3(x+jitter-.14f,.0415f,z+1.93f),new Vector3(x+jitter+.14f,.0415f,z+1.93f),new Vector3(x+jitter+.14f,.0415f,z));
                }
            foreach(float z in new[]{-12.65f,-11.35f})
                for(int i=0;i<18;i++)
                {
                    float x=-16.7f+i*.51f,jitter=Mathf.Sin(i*.49f)*.04f;
                    Quad(v,t,new Vector3(x,.0615f,z+jitter-.12f),new Vector3(x,.0615f,z+jitter+.12f),new Vector3(x+.49f,.0615f,z+jitter+.12f),new Vector3(x+.49f,.0615f,z+jitter-.12f));
                }
            Save(root,"Road and access tyre wear",v,t,material);
        }
        static void Vegetation(Transform root,FarmhouseLayout l,FarmyardArtProfile profile)
        {
            var points=new List<Vector3>();
            foreach(int side in new[]{-1,1}) for(int i=0;i<34;i++)
            {
                float z=-49+i*3.4f,x=side*(9.92f+(i%3)*.19f);
                if(Clear(x,z,l)) points.Add(new Vector3(x,-.05f,z));
                x=side*(29.25f+(i%3)*.12f);
                if(Clear(x,z,l)) points.Add(new Vector3(x,-.05f,z+.9f));
            }
            if(profile.VegetationPrefab!=null)
            {for(int i=0;i<points.Count;i++) Instance(profile.VegetationPrefab,root,points[i],i*137.5f);return;}
            Material grass=WeaponModelGeometry.MaterialFor("Farm dry verge grass",new Color(.38f,.36f,.23f));
            var v=new List<Vector3>();var t=new List<int>();
            for(int i=0;i<points.Count;i++) for(int blade=0;blade<5;blade++)
            {
                float angle=(i*137.5f+blade*72)*Mathf.Deg2Rad,height=.24f+(i+blade)%5*.036f;
                Vector3 direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle)),across=Vector3.Cross(Vector3.up,direction)*.016f;
                Vector3 bottom=points[i]+direction*.04f,middle=bottom+Vector3.up*(height*.58f)+direction*.065f,tip=bottom+Vector3.up*height+direction*.11f;
                Triangle(v,t,bottom,middle-across,tip);Triangle(v,t,bottom,tip,middle+across);
                Triangle(v,t,bottom,tip,middle-across);Triangle(v,t,bottom,middle+across,tip);
            }
            Save(root,"Dry grass along field boundaries",v,t,grass);
        }
        static bool Clear(float x,float z,FarmhouseLayout l)
        {
            Vector3 p=new Vector3(x,0,z)-l.WorldOrigin;
            if(Mathf.Abs(p.x)<l.HalfWidth+.8f && p.z>-l.HalfDepth-l.PorchDepth-1 && p.z<l.HalfDepth+2.2f) return false;
            if(x>-17.4f && x<-6.6f && z>-14.4f && z<-9.6f) return false;
            return true;
        }
        static float Height(float x,float z) => x>=-17 && x<=-7 && z>=-14 && z<=-10?.06f:-.05f;
        static Material Surface(Material soil)
        {
            const string path="Assets/Harvest/Materials/Farm Tyre Wear.mat";Material material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material!=null) return material;
            material=new Material(soil){name="Farm Tyre Wear"};Color tint=soil.GetColor("_BaseColor");material.SetColor("_BaseColor",new Color(tint.r*.87f,tint.g*.87f,tint.b*.87f,tint.a));AssetDatabase.CreateAsset(material,path);return material;
        }
        static void Validate(GameObject prefab)
        {
            if(prefab==null) return;
            if(!AssetDatabase.Contains(prefab)||PrefabUtility.GetPrefabAssetType(prefab)==PrefabAssetType.NotAPrefab||AssetDatabase.GetAssetPath(prefab).Contains("/Generated/")||prefab.GetComponentInChildren<Collider>(true)!=null||prefab.GetComponentInChildren<MonoBehaviour>(true)!=null||prefab.GetComponentInChildren<Rigidbody>(true)!=null)
                throw new InvalidOperationException("Farmyard art must be an artist-owned static visual prefab without colliders, scripts or rigidbodies: "+prefab.name);
        }
        static void Instance(GameObject prefab,Transform parent,Vector3 point,float yaw)
        {
            var go=Object.Instantiate(prefab,parent);go.transform.localPosition=point;go.transform.localRotation=Quaternion.Euler(0,yaw,0);
        }
        static void Box(Transform root,string name,Material material,Vector3 point,Vector3 size)
        {
            var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=point;
            go.AddComponent<MeshFilter>().sharedMesh=FarmhouseTimberFinish.IsTimber(material)?FarmhouseTimberFinish.Box(size,.006f):FarmhouseMeshLibrary.Box(size,.003f);go.AddComponent<MeshRenderer>().sharedMaterial=material;
            GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic);
        }
        static void Quad(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c,Vector3 d)
        {Triangle(v,t,a,b,c);Triangle(v,t,a,c,d);}
        static void Triangle(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c)
        {int n=v.Count;v.Add(a);v.Add(b);v.Add(c);t.Add(n);t.Add(n+1);t.Add(n+2);}
        static void Save(Transform root,string name,List<Vector3> v,List<int> t,Material material)
        {
            string path=Folder+"/"+name.Replace(" ","_")+".asset";Mesh mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);bool create=mesh==null;if(create) mesh=new Mesh();
            mesh.Clear();mesh.name=name;mesh.SetVertices(v);mesh.SetTriangles(t,0);var uv=new List<Vector2>();foreach(Vector3 p in v) uv.Add(new Vector2(p.x,p.z));mesh.SetUVs(0,uv);
            mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.RecalculateTangents();if(create) AssetDatabase.CreateAsset(mesh,path);EditorUtility.SetDirty(mesh);
            var go=new GameObject(name);go.transform.SetParent(root,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;
            go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic);
        }
    }
}
