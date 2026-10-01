using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Palette=Harvest.Editor.FarmhouseInteriorDressing.Palette;

namespace Harvest.Editor
{
    // Static household dressing and restrained surface accents, independently replaceable by artists.
    internal static class FarmhouseLivedInDetails
    {
        const string Folder="Assets/Harvest/Art/Generated/FarmhouseHousehold";
        static GameObject Box(Transform root,string name,Material material,Vector3 point,Vector3 size,bool solid=false) =>
            FarmhouseInteriorDressing.Box(root,name,material,point,size,solid);
        public static void Entrance(Transform root,Palette p)
        {
            Plane(root,"Entrance rug",p.Cloth,new Vector3(0,.002f,0),new Vector2(1.6f,1.02f));
            foreach(int side in new[]{-1,1})
            {
                Plane(root,"Rug long border "+side,p.Linen,new Vector3(side*.72f,.003f,0),new Vector2(.035f,.90f));
                Plane(root,"Rug short border "+side,p.Linen,new Vector3(0,.003f,side*.44f),new Vector2(1.44f,.035f));
                for(int i=0;i<26;i++) Plane(root,"Rug fringe "+side+" "+i,p.Cloth,new Vector3(-.74f+i*.059f,.0015f,side*.532f),new Vector2(.016f,.044f));
            }
            for(int i=0;i<5;i++) Plane(root,"Rug center stripe "+i,p.Paint,new Vector3(0,.003f,-.25f+i*.125f),new Vector2(.68f,.016f));
            for(int i=0;i<3;i++)
            {
                GameObject paper=Plane(root,"Abandoned entry paper "+i,p.Paper,new Vector3(-1.30f-i*.13f,.002f,.68f+i*.14f),new Vector2(.21f,.29f));
                paper.transform.localRotation=Quaternion.Euler(0,-19+i*27,0);
                Plane(paper.transform,"Paper print "+i,p.Paint,new Vector3(0,.0006f,-.04f),new Vector2(.13f,.006f));
            }
        }
        public static void Dining(Transform root,Palette p)
        {
            Plane(root,"Dining folded napkin",p.Linen,new Vector3(-.70f,.832f,.22f),new Vector2(.39f,.36f));
            Mesh plate=FarmMachineryMeshes.Revolve("Household_enamel_plate",new Vector2(0,0),new Vector2(.11f,0),new Vector2(.17f,.014f),new Vector2(.17f,.024f),new Vector2(.15f,.024f),new Vector2(.10f,.006f),new Vector2(0,.006f));
            var dish=new GameObject("Enamel dinner plate");dish.transform.SetParent(root,false);dish.transform.localPosition=new Vector3(-.70f,.833f,.22f);
            dish.AddComponent<MeshFilter>().sharedMesh=plate;dish.AddComponent<MeshRenderer>().sharedMaterial=p.Ceramic;Static(dish);
            Box(root,"Dinner knife",p.Steel,new Vector3(-.44f,.833f,.22f),new Vector3(.022f,.006f,.24f));
            Box(root,"Knife grip",p.Wood,new Vector3(-.44f,.841f,.145f),new Vector3(.025f,.012f,.09f));
            Box(root,"Dinner fork stem",p.Steel,new Vector3(-.96f,.833f,.18f),new Vector3(.015f,.006f,.17f));
            for(int i=0;i<3;i++) Box(root,"Fork tine "+i,p.Steel,new Vector3(-.972f+i*.012f,.833f,.29f),new Vector3(.006f,.006f,.05f));
            var fallen=new GameObject("Fallen dining chair").transform;fallen.SetParent(root,false);fallen.localPosition=new Vector3(1.55f,.23f,-1.8f);fallen.localRotation=Quaternion.Euler(0,12,90);
            Box(fallen,"Fallen chair seat",p.Wood,new Vector3(0,.45f,0),new Vector3(.46f,.07f,.44f));
            for(int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2) Box(fallen,"Fallen chair leg "+x+" "+z,p.Wood,new Vector3(x*.17f,.2075f,z*.16f),new Vector3(.055f,.415f,.055f));
            foreach(int side in new[]{-1,1}) Box(fallen,"Fallen chair back post "+side,p.Wood,new Vector3(side*.18f,.68f,.18f),new Vector3(.05f,.54f,.05f));
            for(int i=0;i<3;i++) Box(fallen,"Fallen chair back slat "+i,p.Wood,new Vector3(0,.68f+i*.09f,.18f),new Vector3(.40f,.065f,.03f));
            var body=new GameObject("Fallen chair collision");body.transform.SetParent(fallen,false);body.AddComponent<BoxCollider>().center=new Vector3(0,.475f,.04f);body.GetComponent<BoxCollider>().size=new Vector3(.46f,.95f,.52f);
        }
        public static void Keepsakes(Transform root,Palette p)
        {
            Box(root,"Entry keepsake cabinet",p.Wood,new Vector3(0,.39f,0),new Vector3(.85f,.78f,.48f),true);
            Box(root,"Cabinet top",p.Wood,new Vector3(0,.805f,0),new Vector3(.90f,.05f,.53f));
            foreach(int side in new[]{-1,1})
            {
                Box(root,"Keepsake cabinet door "+side,p.Paint,new Vector3(side*.205f,.39f,-.255f),new Vector3(.395f,.69f,.03f));
                Box(root,"Cabinet pull "+side,p.Steel,new Vector3(side*.045f,.42f,-.277f),new Vector3(.025f,.09f,.015f));
            }
            var frame=new GameObject("Family keepsake frame").transform;frame.SetParent(root,false);frame.localPosition=new Vector3(-.12f,.83f,.09f);
            Box(frame,"Picture backing",p.Wood,new Vector3(0,.145f,0),new Vector3(.31f,.29f,.035f));
            Box(frame,"Picture paper",p.Paper,new Vector3(0,.145f,-.022f),new Vector3(.265f,.245f,.010f));
            Box(frame,"Illustrated farm horizon",p.Paint,new Vector3(0,.085f,-.031f),new Vector3(.24f,.07f,.008f));
            foreach(float x in new[]{-.078f,0f,.078f})
            {
                float height=x==0?.09f:.075f;
                Box(frame,"Family silhouette",p.Cloth,new Vector3(x,.105f+height*.5f,-.037f),new Vector3(.046f,height,.008f));
                Mesh head=FarmMachineryMeshes.Revolve("Keepsake_head",new Vector2(0,0),new Vector2(.019f,0),new Vector2(.019f,.005f),new Vector2(0,.005f));
                var go=new GameObject("Family portrait head");go.transform.SetParent(frame,false);go.transform.localPosition=new Vector3(x,.105f+height+.014f,-.042f);go.transform.localRotation=Quaternion.Euler(-90,0,0);
                go.AddComponent<MeshFilter>().sharedMesh=head;go.AddComponent<MeshRenderer>().sharedMaterial=p.Linen;Static(go);
            }
            FarmhouseInteriorDressing.Vessel(root,"Household ceramic jar",p.Ceramic,new Vector3(.28f,.83f,-.03f),.075f,.15f);
        }
        public static void Wear(Transform parent,FarmhouseLayout l,Material timber)
        {
            const string path="Assets/Harvest/Materials/Farmhouse Interior Worn Timber.mat";
            var finish=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(finish==null)
            {
                var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/Harvest/Materials/Farmhouse Plank Floor.mat");if(source==null) source=timber;
                finish=new Material(source){name="Farmhouse Interior Worn Timber"};Color color=source.GetColor("_BaseColor");finish.SetColor("_BaseColor",new Color(color.r*1.08f,color.g*1.08f,color.b*1.08f,color.a));finish.SetFloat("_BumpScale",.08f);AssetDatabase.CreateAsset(finish,path);
            }
            var root=new GameObject("Entry and stair surface wear").transform;root.SetParent(parent,false);
            for(int i=0;i<12;i++)
            {
                float x=((i*7)%11-5)*.15f,z=-l.HalfDepth+.47f+(i%4)*.20f;
                GameObject scuff=Plane(root,"Threshold wood scuff "+i,finish,new Vector3(x,l.GroundFloorTop+.001f,z),new Vector2(.012f+(i%3)*.004f,.12f+(i%4)*.035f));scuff.transform.localRotation=Quaternion.Euler(0,(i%3-1)*7,0);
            }
            for(int i=0;i<l.StairCount;i++)
                for(int j=0;j<3;j++) Plane(root,"Tread wear "+i+" "+j,finish,l.StairPoint(i)+new Vector3((j-1)*.18f,.001f,-.085f),new Vector2(.11f+(i%3)*.02f,.030f));
        }
        static GameObject Plane(Transform root,string name,Material material,Vector3 center,Vector2 size)
        {
            FarmhouseMeshLibrary.EnsureFolder(Folder);string key=size.x.ToString("F4",CultureInfo.InvariantCulture)+"_"+size.y.ToString("F4",CultureInfo.InvariantCulture),path=Folder+"/Plane_"+key+".asset";
            Mesh mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null)
            {
                float x=size.x*.5f,z=size.y*.5f;mesh=new Mesh{name="Household plane "+key};mesh.vertices=new[]{new Vector3(-x,0,-z),new Vector3(-x,0,z),new Vector3(x,0,z),new Vector3(x,0,-z)};
                mesh.normals=new[]{Vector3.up,Vector3.up,Vector3.up,Vector3.up};mesh.uv=new[]{new Vector2(0,0),new Vector2(0,size.y),size,new Vector2(size.x,0)};
                mesh.uv2=new[]{new Vector2(.02f,.02f),new Vector2(.02f,.98f),new Vector2(.98f,.98f),new Vector2(.98f,.02f)};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,path);
            }
            var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=center;go.AddComponent<MeshFilter>().sharedMesh=mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial=material;go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;Static(go);return go;
        }
        static void Static(GameObject go) => GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic);
    }
}
