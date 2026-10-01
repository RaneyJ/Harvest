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
            if(profile.EnableTracks) Tracks(root,Surface(AssetDatabase.LoadAssetAtPath<Material>("Assets/Harvest/Materials/Natural freight gravel.mat")));
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
                    Instance(profile.GroundDebrisPrefab,root,new Vector3(position.x,Height(position.x,position.z),position.z),position.z*7);
        }
        static void Tracks(Transform root,Material material)
        {
            var v=new List<Vector3>();var t=new List<int>();var colors=new List<Color>();
            foreach(float centre in new[]{-2.65f,2.65f})
            {
                int first=v.Count;
                for(int row=0;row<=118;row++)
                {
                    float z=-49+row,jitter=(Mathf.PerlinNoise(13,z*.07f)-.5f)*.13f;
                    float width=.14f+(Mathf.PerlinNoise(39,z*.13f)-.5f)*.035f;
                    float pressure=.25f+Mathf.PerlinNoise(7,z*.12f)*.55f;
                    pressure*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-49,-45,z))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(64,69,z)));
                    for(int band=0;band<4;band++)
                    {
                        float offset=band==0?-width-.065f:band==1?-width:band==2?width:width+.065f;
                        float x=centre+jitter+offset;v.Add(new Vector3(x,FarmNaturalGround.SurfaceHeight(x,z)+.002f,z));
                        colors.Add(new Color(band==0||band==3?1:1-pressure,FarmNaturalGround.SurfaceVariation(x,z),0,1));
                    }
                }
                for(int row=0;row<118;row++)for(int band=0;band<3;band++) TrackCell(t,first+row*4+band,4);
            }
            foreach(float centre in new[]{-12.65f,-11.35f})
            {
                int first=v.Count;
                for(int row=0;row<=38;row++)
                {
                    float x=-16.5f+row*.25f,jitter=(Mathf.PerlinNoise(x*.2f,8)-.5f)*.06f;
                    float pressure=.5f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(-16.5f,-15.5f,x))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-8,-7,x)));
                    for(int band=0;band<4;band++)
                    {
                        float offset=band==0?-.19f:band==1?-.12f:band==2?.12f:.19f;
                        float z=centre+jitter+offset;v.Add(new Vector3(x,FarmNaturalGround.SurfaceHeight(x,z)+.002f,z));
                        colors.Add(new Color(band==0||band==3?1:1-pressure,FarmNaturalGround.SurfaceVariation(x,z),0,1));
                    }
                }
                for(int row=0;row<38;row++)for(int band=0;band<3;band++) TrackCell(t,first+row*4+band,4,true);
            }
            Save(root,"Road and access tyre wear",v,t,material,colors);
        }
        static void TrackCell(List<int> t,int a,int columns,bool flip=false)
        {
            t.Add(a);t.Add(a+(flip?1:columns));t.Add(a+(flip?columns:1));
            t.Add(a+1);t.Add(a+(flip?columns+1:columns));t.Add(a+(flip?columns:columns+1));
        }
        static void Vegetation(Transform root,FarmhouseLayout l,FarmyardArtProfile profile)
        {
            var points=new List<Vector3>();
            foreach(int side in new[]{-1,1}) for(int i=0;i<46;i++)
            {
                float patch=Mathf.PerlinNoise(i*.27f+side*13+40,71);
                if(patch<.32f)continue;
                float z=-49+i*2.5f+(Mathf.PerlinNoise(i*.71f+12,8)-.5f)*1.35f;
                foreach(float edge in new[]{10.35f,29.05f})
                {
                    float x=side*(edge+Mathf.PerlinNoise(i*.41f+21,edge)*.55f);
                    int clumps=patch>.64f?3:1;
                    for(int clump=0;clump<clumps;clump++)
                    {
                        float angle=(i*137.5f+clump*120)*Mathf.Deg2Rad;
                        float px=x+Mathf.Cos(angle)*clump*.13f,pz=z+Mathf.Sin(angle)*clump*.13f;
                        if(Clear(px,pz,l))points.Add(new Vector3(px,FarmNaturalGround.SurfaceHeight(px,pz),pz));
                    }
                }
            }
            if(profile.VegetationPrefab!=null)
            {for(int i=0;i<points.Count;i++) Instance(profile.VegetationPrefab,root,points[i],i*137.5f);return;}
            Material grass=WeaponModelGeometry.MaterialFor("Farm dry verge grass",new Color(.38f,.36f,.23f));
            var v=new List<Vector3>();var t=new List<int>();
            for(int i=0;i<points.Count;i++) for(int blade=0;blade<5;blade++)
            {
                float angle=(i*137.5f+blade*72)*Mathf.Deg2Rad,height=.24f+(i+blade)%5*.036f;
                Vector3 direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle)),across=Vector3.Cross(Vector3.up,direction)*.016f;
                Vector3 bottom=points[i]+direction*.04f;bottom.y=FarmNaturalGround.SurfaceHeight(bottom.x,bottom.z);
                Vector3 middle=bottom+Vector3.up*(height*.58f)+direction*.065f,tip=bottom+Vector3.up*height+direction*.11f;
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
        static float Height(float x,float z) => FarmNaturalGround.SurfaceHeight(x,z);
        static Material Surface(Material road)
        {
            const string path="Assets/Harvest/Materials/Farm Natural Tyre Wear.mat";
            Material material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material!=null)return material;
            material=new Material(road){name="Farm Natural Tyre Wear"};
            Color tint=road.GetColor("_BaseColor");material.SetColor("_BaseColor",new Color(tint.r*.86f,tint.g*.86f,tint.b*.86f,tint.a));
            material.SetTexture("_GroundMap",road.GetTexture("_SurfaceMap"));material.SetColor("_GroundTint",tint);
            material.SetFloat("_GroundScale",road.GetFloat("_WorldScale"));material.SetFloat("_GroundSmoothness",road.GetFloat("_Smoothness"));
            AssetDatabase.CreateAsset(material,path);return material;
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
        static void Save(Transform root,string name,List<Vector3> v,List<int> t,Material material,List<Color> colors=null)
        {
            string path=Folder+"/"+name.Replace(" ","_")+".asset";Mesh mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);bool create=mesh==null;if(create) mesh=new Mesh();
            mesh.Clear();mesh.name=name;mesh.SetVertices(v);mesh.SetTriangles(t,0);if(colors!=null)mesh.SetColors(colors);var uv=new List<Vector2>();foreach(Vector3 p in v) uv.Add(new Vector2(p.x,p.z));mesh.SetUVs(0,uv);
            mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.RecalculateTangents();if(create) AssetDatabase.CreateAsset(mesh,path);EditorUtility.SetDirty(mesh);
            var go=new GameObject(name);go.transform.SetParent(root,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;
            go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic);
        }
    }
}
