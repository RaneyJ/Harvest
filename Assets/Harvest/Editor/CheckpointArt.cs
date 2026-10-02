using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace Harvest.Editor
{
    // Cover geometry remains the established solid volumes. Small fittings are visual only.
    public static class CheckpointArt
    {
        const string RootName="Checkpoint and parking art";
        public static void Build()
        {
            Transform root=Group(null,RootName,Vector3.zero);
            Material source=AssetDatabase.LoadAssetAtPath<Material>(FarmhouseMaterialLibrary.MaterialPath("Foundation"))??AssetDatabase.LoadAssetAtPath<Material>("Assets/Harvest/Materials/Concrete.mat");
            Material concrete=Finish("Checkpoint cast concrete",source,new Color(.88f,.86f,.80f));
            Material dirt=Finish("Checkpoint concrete road grime",source,new Color(.68f,.63f,.53f));
            Material steel=AssetDatabase.LoadAssetAtPath<Material>(FarmhouseMaterialLibrary.MaterialPath("Steel"))??WeaponModelGeometry.MaterialFor("Checkpoint steel",new Color(.22f,.24f,.23f),.5f);
            Material paint=WeaponModelGeometry.MaterialFor("Checkpoint faded yellow paint",new Color(.53f,.43f,.20f));
            Material dark=WeaponModelGeometry.MaterialFor("Checkpoint charcoal paint",new Color(.12f,.135f,.12f));
            Mesh eye=Eye();
            int id=0;
            MeshRenderer[] renderers=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            Array.Sort(renderers,(a,b)=>
            {
                int order=string.CompareOrdinal(a.name,b.name);
                if(order!=0)return order;
                order=a.transform.position.z.CompareTo(b.transform.position.z);
                return order!=0?order:a.transform.position.x.CompareTo(b.transform.position.x);
            });
            foreach(MeshRenderer renderer in renderers)
            {
                string name=renderer.name;
                if(name!="Checkpoint barricade left"&&name!="Checkpoint barricade right"&&name!="Reserve barricade"&&name!="Road barrier")continue;
                GameObject body=renderer.gameObject;Vector3 size=body.transform.localScale,h=size*.5f;
                BoxCollider collider=body.GetComponent<BoxCollider>();collider.center=Vector3.Scale(collider.center,size);collider.size=Vector3.Scale(collider.size,size);
                body.transform.localScale=Vector3.one;body.GetComponent<MeshFilter>().sharedMesh=FarmhouseMeshLibrary.Box(size,.025f);renderer.sharedMaterial=concrete;Static(body);
                Transform group=Group(root,name+" fittings",body.transform.position);
                foreach(float x in new[]{-size.x*.30f,size.x*.30f})
                {
                    var go=new GameObject("Embedded lifting eye");go.transform.SetParent(group,false);go.transform.localPosition=new Vector3(x,h.y+.025f,0);
                    go.transform.localRotation=Quaternion.Euler(90,0,0);go.AddComponent<MeshFilter>().sharedMesh=eye;go.AddComponent<MeshRenderer>().sharedMaterial=steel;Static(go);
                }
                foreach(int side in new[]{-1,1})
                {
                    float face=side*(h.z+.004f);
                    Box(group,"Recessed casting panel",dirt,new Vector3(0,.12f,face),new Vector3(size.x-.34f,size.y*.43f,.008f));
                    Box(group,"Faded safety band",dark,new Vector3(0,-.30f,side*(h.z+.013f)),new Vector3(size.x-.30f,.28f,.015f));
                    int stripes=Mathf.FloorToInt((size.x-.50f)/.38f);
                    for(int i=0;i<stripes;i++)
                        Box(group,"Worn diagonal safety paint",paint,new Vector3(-size.x*.5f+.38f+i*.38f,-.30f,side*(h.z+.026f)),new Vector3(.10f,.23f,.012f)).transform.localRotation=Quaternion.Euler(0,0,-32);
                    foreach(float x in new[]{-h.x+.12f,h.x-.12f})
                    {
                        Box(group,"Barrier end retaining strap",steel,new Vector3(x,0,side*(h.z+.017f)),new Vector3(.07f,size.y-.16f,.025f));
                        foreach(float y in new[]{-h.y+.18f,h.y-.18f})Box(group,"Retaining strap fastener",dark,new Vector3(x,y,side*(h.z+.034f)),new Vector3(.038f,.038f,.016f));
                    }
                    Label(group,"FREIGHT / "+(4+id).ToString("00"),new Vector3(0,.16f,side*(h.z+.014f)),side>0?180:0);
                }
                Wear(group,size,dirt,id++);
            }
            Parking(root);
        }
        [MenuItem("Harvest/Validate Checkpoint And Yard")]
        public static void Validate()
        {
            try
            {
                GameObject root=GameObject.Find(RootName);Require(root!=null,"Rebuild The Line for checkpoint art.");Physics.SyncTransforms();
                int checkedBodies=0;
                foreach(BoxCollider box in Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))
                {
                    Vector3 point,size;
                    switch(box.name)
                    {
                        case "Checkpoint barricade left":point=new Vector3(-5,.7f,-7);size=new Vector3(4.5f,1.4f,1.3f);break;
                        case "Checkpoint barricade right":point=new Vector3(5,.7f,-7);size=new Vector3(4.5f,1.4f,1.3f);break;
                        case "Reserve barricade":point=new Vector3(0,.7f,-13);size=new Vector3(3,1.4f,1.3f);break;
                        case "Road barrier":point=box.transform.position.z<10?new Vector3(3,.6f,8):new Vector3(-3,.6f,19);size=new Vector3(3,1.2f,1);break;
                        default:continue;
                    }
                    Require((box.bounds.center-point).sqrMagnitude<.000001f&&(box.bounds.size-size).sqrMagnitude<.000001f,"Established cover bounds changed: "+box.name);checkedBodies++;
                }
                Require(checkedBodies==5,"Expected five dressed concrete cover bodies.");
                foreach(MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
                {
                    Mesh mesh=filter.sharedMesh;Require(mesh!=null&&AssetDatabase.Contains(mesh)&&mesh.uv2.Length==mesh.vertexCount,"Missing persistent/lightmap-ready checkpoint mesh: "+filter.name);
                }
                var house=Object.FindFirstObjectByType<FarmhouseFoundation>();Require(house!=null,"Farmhouse missing.");
                foreach(Collider collider in root.GetComponentsInChildren<Collider>())
                {
                    Bounds b=collider.bounds;
                    Require(b.max.x<house.Layout.WorldOrigin.x-1,"Parking stop enters the central farmhouse approach.");
                    Require(b.size.y<=.14f,"Parking stop exceeds the intended step height.");
                    Require(Mathf.Abs(b.min.y-FarmNaturalGround.SurfaceHeight(b.center.x,b.center.z))<.003f,"Parking stop is not grounded.");
                }
                GameObject apron=GameObject.Find("Farmyard parking apron");Require(apron!=null,"Parking apron missing; rebuild The Line.");
                MeshCollider yard=apron.GetComponent<MeshCollider>();Require(yard!=null,"Parking apron collision missing.");
                Vector2 centre=FarmNaturalGround.YardCentre;
                Require(yard.Raycast(new Ray(new Vector3(centre.x,5,centre.y),Vector3.down),out RaycastHit hit,10)&&Mathf.Abs(hit.point.y-.025f)<.003f,"Parking apron grade/collision mismatch.");
                Debug.Log("Checkpoint and yard checks passed. Inspect concrete normals, fittings, parking circulation and the driveway seam in Game view.");
            }
            catch(Exception error){Debug.LogError("Checkpoint/yard check failed: "+error.Message);}
        }
        static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}

        static void Parking(Transform parent)
        {
            Vector2 centre=FarmNaturalGround.YardCentre;
            Transform root=Group(parent,"Farmhouse parking bays",Vector3.zero);
            Material wood=AssetDatabase.LoadAssetAtPath<Material>(FarmhouseMaterialLibrary.MaterialPath("Timber"))??WeaponModelGeometry.MaterialFor("Parking stop timber",new Color(.32f,.26f,.18f));
            Material steel=AssetDatabase.LoadAssetAtPath<Material>(FarmhouseMaterialLibrary.MaterialPath("Steel"))??WeaponModelGeometry.MaterialFor("Parking stop hardware",new Color(.21f,.23f,.20f),.4f);
            foreach(float offset in new[]{-5.6f,-2.6f})
            {
                float x=centre.x+offset,z=centre.y+2.8f,y=FarmNaturalGround.SurfaceHeight(x,z);
                GameObject stop=Box(root,"Timber parking stop",wood,new Vector3(x,y+.065f,z),new Vector3(2.6f,.13f,.18f));
                var collider=stop.AddComponent<BoxCollider>();collider.size=new Vector3(2.6f,.13f,.18f);
                foreach(float dx in new[]{-.96f,.96f})Box(root,"Parking stop anchor plate",steel,new Vector3(x+dx,y+.135f,z),new Vector3(.12f,.01f,.14f));
            }
        }
        static Mesh Eye()
        {
            var profile=new Vector2[17];for(int i=0;i<profile.Length;i++){float angle=i*Mathf.PI*2/16;profile[i]=new Vector2(.095f+Mathf.Cos(angle)*.014f,Mathf.Sin(angle)*.014f);}
            return FarmMachineryMeshes.Revolve("Checkpoint_lifting_eye",profile);
        }
        static void Wear(Transform root,Vector3 size,Material material,int seed)
        {
            var v=new List<Vector3>();var t=new List<int>();var uv=new List<Vector2>();
            foreach(int side in new[]{-1,1})for(int i=0;i<16;i++)
            {
                float x=Mathf.Sin(i*2.17f+seed*.39f)*(size.x*.5f-.22f),y=-size.y*.5f+.13f+(i%4)*.13f;
                float radius=.045f+(i%3)*.021f;int first=v.Count;
                Add(new Vector3(x,y,side*(size.z*.5f+.005f)));
                for(int k=0;k<9;k++){float a=k*Mathf.PI*2/9,r=radius*(.78f+.18f*Mathf.Sin(i+k*2.31f));Add(new Vector3(x+Mathf.Cos(a)*r*1.6f,y+Mathf.Sin(a)*r,side*(size.z*.5f+.005f)));}
                for(int k=0;k<9;k++){t.Add(first);t.Add(first+1+(side<0?(k+1)%9:k));t.Add(first+1+(side<0?k:(k+1)%9));}
            }
            const string folder="Assets/Harvest/Art/Generated/Checkpoint";FarmhouseMeshLibrary.EnsureFolder(folder);
            string path=folder+"/Barrier_wear_"+seed+".asset";Mesh mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);bool create=mesh==null;if(create)mesh=new Mesh();
            mesh.Clear();mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            if(!Unwrapping.GenerateSecondaryUVSet(mesh))throw new InvalidOperationException("Checkpoint wear UV unwrap failed.");if(create)AssetDatabase.CreateAsset(mesh,path);EditorUtility.SetDirty(mesh);
            var go=new GameObject("Concrete lower edge dirt and abrasion");go.transform.SetParent(root,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;
            go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;Static(go);
            void Add(Vector3 p){v.Add(p);uv.Add(new Vector2(p.x,p.y));}
        }
        static Material Finish(string name,Material source,Color tint)
        {
            string path="Assets/Harvest/Materials/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material!=null)return material;
            material=new Material(source){name=name};Color color=source.GetColor("_BaseColor");material.SetColor("_BaseColor",color*tint);AssetDatabase.CreateAsset(material,path);return material;
        }
        static Transform Group(Transform parent,string name,Vector3 position){var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;return go.transform;}
        static GameObject Box(Transform root,string name,Material material,Vector3 point,Vector3 size)
        {
            var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=point;
            go.AddComponent<MeshFilter>().sharedMesh=FarmhouseTimberFinish.IsTimber(material)?FarmhouseTimberFinish.Box(size,.003f):FarmhouseMeshLibrary.Box(size,.003f);
            go.AddComponent<MeshRenderer>().sharedMaterial=material;Static(go);return go;
        }
        static void Static(GameObject go)=>GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic);
        static void Label(Transform root,string words,Vector3 point,float yaw)
        {
            Transform go=Group(root,words,point);go.localRotation=Quaternion.Euler(0,yaw,0);
            var text=go.gameObject.AddComponent<TextMesh>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if(text.font!=null)go.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;
            text.text=words;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.fontSize=40;text.characterSize=.045f;text.color=new Color(.20f,.22f,.18f);
            go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
    }
}
