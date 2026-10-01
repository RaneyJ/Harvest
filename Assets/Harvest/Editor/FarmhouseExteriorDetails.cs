using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Harvest.Editor
{
    // Static farm infrastructure and surface wear. Generated independently of combat systems.
    public static class FarmhouseExteriorDetails
    {
        const string Folder="Assets/Harvest/Art/Generated/FarmhouseExterior";
        public static void Build(Transform porch,Transform trim,Transform collision,FarmhouseLayout l,Material timber,Material steel,Material roofing,Material stone)
        {
            FarmhouseMeshLibrary.EnsureFolder(Folder);
            var details=new GameObject("Exterior details").transform;details.SetParent(trim,false);
            Mesh bolt=FarmMachineryMeshes.Revolve("Farmhouse_connection_bolt",new Vector2(0,0),new Vector2(.014f,0),new Vector2(.014f,.009f),new Vector2(.010f,.013f),new Vector2(0,.013f));
            Porch(porch,l,timber,steel,bolt);
            Service(details,collision,l,steel,roofing,stone,bolt);
            Windows(details,l,steel,bolt);
            Weather(details,l);
        }
        static void Porch(Transform porch,FarmhouseLayout l,Material timber,Material steel,Mesh bolt)
        {
            float width=l.HalfWidth*2f-1.2f,z=-l.HalfDepth-l.PorchDepth+.2f;
            foreach(float x in new[]{-width*.5f+.25f,-1.85f,1.85f,width*.5f-.25f})
            {
                Box(porch,"Porch bolted connection "+x,steel,new Vector3(x,2.69f,z-.12f),new Vector3(.17f,.32f,.02f));
                foreach(float y in new[]{2.58f,2.79f}) foreach(float dx in new[]{-.048f,.048f})
                    Bolt(porch,"Porch bolt",steel,bolt,new Vector3(x+dx,y,z-.131f),Vector3.back);
                foreach(int side in new[]{-1,1})
                    Box(porch,"Porch foot saddle "+x+" "+side,steel,new Vector3(x+side*.113f,.26f,z),new Vector3(.022f,.20f,.22f));
            }
            var lining=new GameObject("Porch timber soffit").transform;lining.SetParent(porch,false);
            lining.localPosition=new Vector3(0,3,-l.HalfDepth-l.PorchDepth*.5f);lining.localRotation=Quaternion.Euler(-5,0,0);
            float span=width+.50f;int boards=Mathf.CeilToInt(span/.24f);float boardWidth=span/boards;
            for(int i=0;i<boards;i++)
                Box(lining,"Soffit board "+i,timber,new Vector3(-span*.5f+(i+.5f)*boardWidth,-.082f,0),new Vector3(boardWidth-.002f,.020f,l.PorchDepth+.38f));
        }
        static void Service(Transform root,Transform collision,FarmhouseLayout l,Material steel,Material roofing,Material stone,Mesh bolt)
        {
            var canopy=new GameObject("Rear service canopy").transform;canopy.SetParent(root,false);
            canopy.localPosition=new Vector3(0,2.8f,l.HalfDepth+.83f);canopy.localRotation=Quaternion.Euler(7,0,0);
            Box(canopy,"Service canopy roofing",roofing,Vector3.zero,new Vector3(2.8f,.09f,1.6f));
            Box(canopy,"Service canopy front fascia",steel,new Vector3(0,-.025f,.80f),new Vector3(2.8f,.10f,.04f));
            foreach(float x in new[]{-1.2f,1.2f})
            {
                float wall=l.HalfDepth+l.WallThickness*.5f;
                Box(root,"Canopy wall plate "+x,steel,new Vector3(x,2.55f,wall+.018f),new Vector3(.14f,.44f,.036f));
                FarmhouseRooms.Pipe(root,"Canopy diagonal brace "+x,steel,new Vector3(x,2.36f,wall+.04f),new Vector3(x,2.685f,l.HalfDepth+1.35f),.021f);
                foreach(float y in new[]{2.42f,2.69f}) Bolt(root,"Canopy anchor",steel,bolt,new Vector3(x,y,wall+.037f),Vector3.forward);
            }
            Proxy(collision,"Rear canopy collision",new Vector3(0,2.8f,l.HalfDepth+.83f),new Vector3(2.8f,.09f,1.6f)).transform.localRotation=Quaternion.Euler(7,0,0);
            float tankX=l.HalfWidth-1.8f,tankZ=l.HalfDepth+1.3f,baseY=.055f;
            Box(root,"Cistern concrete pad",stone,new Vector3(tankX,-.005f,tankZ),new Vector3(1.5f,.12f,1.5f));
            Proxy(collision,"Cistern pad collision",new Vector3(tankX,-.005f,tankZ),new Vector3(1.5f,.12f,1.5f));
            Mesh tank=FarmMachineryMeshes.Revolve("Farmhouse_rain_cistern",new Vector2(0,0),new Vector2(.56f,0),new Vector2(.62f,.07f),new Vector2(.62f,1.74f),new Vector2(.56f,1.86f),new Vector2(.33f,1.98f),new Vector2(0,2.02f));
            Visual(root,"Rainwater cistern",tank,steel,new Vector3(tankX,baseY,tankZ));
            Proxy(collision,"Cistern collision",new Vector3(tankX,baseY+1.01f,tankZ),new Vector3(1.24f,2.02f,1.24f));
            foreach(float y in new[]{.18f,.72f,1.28f,1.71f})
            {
                Mesh ring=FarmMachineryMeshes.Revolve("Cistern_band",new Vector2(.621f,0),new Vector2(.638f,.009f),new Vector2(.638f,.032f),new Vector2(.621f,.041f));
                Visual(root,"Cistern reinforcing band "+y,ring,steel,new Vector3(tankX,baseY+y,tankZ));
            }
            Box(root,"Cistern lid handle",steel,new Vector3(tankX,baseY+2.058f,tankZ),new Vector3(.24f,.024f,.03f));
            foreach(float x in new[]{-.10f,.10f}) Box(root,"Lid handle foot "+x,steel,new Vector3(tankX+x,baseY+2.025f,tankZ),new Vector3(.025f,.043f,.03f));
            Vector3 inlet=new Vector3(tankX,1.25f,tankZ-.615f),wallInlet=new Vector3(tankX,1.25f,l.HalfDepth+l.WallThickness*.5f+.03f);
            FarmhouseRooms.Pipe(root,"Cistern wall inlet",steel,wallInlet,inlet,.025f);
            Box(root,"Inlet wall escutcheon",steel,new Vector3(tankX,1.25f,l.HalfDepth+l.WallThickness*.5f+.012f),new Vector3(.14f,.14f,.024f));
            FarmhouseRooms.Pipe(root,"Cistern drain riser",steel,new Vector3(tankX,.35f,tankZ-.625f),new Vector3(tankX,.35f,tankZ-.80f),.020f);
            FarmhouseRooms.Pipe(root,"Drain valve stem",steel,new Vector3(tankX,.35f,tankZ-.77f),new Vector3(tankX,.405f,tankZ-.77f),.009f);
            Box(root,"Drain valve grip",steel,new Vector3(tankX,.40f,tankZ-.77f),new Vector3(.14f,.018f,.025f));
        }
        static void Windows(Transform root,FarmhouseLayout l,Material steel,Mesh bolt)
        {
            foreach(float x in new[]{-3.8f,3.8f}) Corners(false,-l.HalfDepth,-1,x,2f,2.25f);
            Corners(true,l.HalfWidth,1,1,3.2f,2.2f);
            Corners(true,-l.HalfWidth,-1,3,2.4f,2.2f);
            Corners(false,-l.HalfDepth,-1,1,3.2f,l.UpperFloorTop+2.3f);
            Corners(false,l.HalfDepth,1,1,3.2f,l.UpperFloorTop+2.3f);
            Corners(true,l.HalfWidth,1,1,4f,l.UpperFloorTop+2.3f);
            Corners(true,-l.HalfWidth,-1,3,2.4f,l.UpperFloorTop+2.3f);
            void Corners(bool alongZ,float axis,int sign,float center,float width,float top)
            {
                Vector3 normal=alongZ?Vector3.right*sign:Vector3.forward*sign;
                float surface=axis+sign*(l.WallThickness*.5f+.111f);
                foreach(int side in new[]{-1,1})
                {
                    float run=center+side*width*.5f;
                    Vector3 point=alongZ?new Vector3(surface,top,run):new Vector3(run,top,surface);
                    Box(root,"Window header corner strap",steel,point,alongZ?new Vector3(.02f,.045f,.14f):new Vector3(.14f,.045f,.02f));
                    Box(root,"Window jamb corner strap",steel,point-Vector3.up*.04f,alongZ?new Vector3(.02f,.12f,.045f):new Vector3(.045f,.12f,.02f));
                    Bolt(root,"Window connection bolt",steel,bolt,point+normal*.011f,normal);
                }
            }
        }
        sealed class Patches
        {
            public readonly List<Vector3> V=new List<Vector3>(),N=new List<Vector3>();
            public readonly List<Vector2> UV=new List<Vector2>();public readonly List<int> T=new List<int>();
            public void Disc(Vector3 center,Vector3 normal,float radius,int seed,float elongation=1)
            {
                Vector3 along=Vector3.Cross(Vector3.up,normal).normalized,up=Vector3.up;
                int first=V.Count;Add(center,new Vector2(0,0));const int sides=11;
                for(int i=0;i<sides;i++)
                {
                    float angle=i*Mathf.PI*2/sides,r=radius*(.78f+.20f*Mathf.Sin(seed*1.31f+i*2.17f));
                    Vector2 uv=new Vector2(Mathf.Cos(angle)*r*elongation,Mathf.Sin(angle)*r);Add(center+along*uv.x+up*uv.y,uv);
                }
                for(int i=0;i<sides;i++){T.Add(first);T.Add(first+1+i);T.Add(first+1+(i+1)%sides);}
                void Add(Vector3 p,Vector2 uv){V.Add(p);N.Add(normal);UV.Add(uv);}
            }
        }
        static void Weather(Transform root,FarmhouseLayout l)
        {
            var chips=new Patches();var scorch=new Patches();
            foreach(int side in new[]{-1,1})
            {
                for(int i=0;i<17;i++)
                {
                    float x=Mathf.Lerp(-l.HalfWidth+.3f,l.HalfWidth-.3f,i/16f);
                    if(Mathf.Abs(x)>(side<0?1.35f:.95f)) chips.Disc(new Vector3(x,.30f+(i%3)*.14f,side*(l.HalfDepth+l.WallThickness*.5f+.004f)),Vector3.forward*side,.045f+(i%4)*.016f,i+side*31,1.3f);
                    float z=Mathf.Lerp(-l.HalfDepth+.35f,l.HalfDepth-.35f,i/16f);
                    chips.Disc(new Vector3(side*(l.HalfWidth+l.WallThickness*.5f+.004f),.32f+(i%4)*.10f,z),Vector3.right*side,.043f+(i%3)*.015f,i+side*73,1.2f);
                }
            }
            float wall=l.HalfWidth+l.WallThickness*.5f;
            // The opaque impact scar sits between window apertures; it doesn't change wall collision.
            scorch.Disc(new Vector3(wall+.004f,l.UpperFloorTop+.32f,-2.7f),Vector3.right,.31f,149,1.08f);
            chips.Disc(new Vector3(wall+.007f,l.UpperFloorTop+.32f,-2.7f),Vector3.right,.077f,197,1.1f);
            Save(root,"Plaster foot wear",chips,Surface("Exposed plaster",new Color(.30f,.29f,.24f)));
            Save(root,"Localized plasma scorch",scorch,Surface("Dry scorch",new Color(.095f,.09f,.075f)));
        }
        static void Save(Transform root,string name,Patches data,Material material)
        {
            string path=Folder+"/"+name.Replace(" ","_")+".asset";Mesh mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);bool create=mesh==null;if(create) mesh=new Mesh();
            mesh.Clear();mesh.name=name;mesh.SetVertices(data.V);mesh.SetNormals(data.N);mesh.SetUVs(0,data.UV);mesh.SetTriangles(data.T,0);mesh.RecalculateBounds();
            UnwrapParam.SetDefaults(out UnwrapParam unwrap);unwrap.packMargin=.01f;
            if(!Unwrapping.GenerateSecondaryUVSet(mesh,unwrap)) throw new InvalidOperationException("Exterior wear UV unwrap failed: "+name);
            mesh.RecalculateTangents();if(create) AssetDatabase.CreateAsset(mesh,path);EditorUtility.SetDirty(mesh);
            GameObject go=Visual(root,name,mesh,material,Vector3.zero);go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
        static Material Surface(string name,Color color)
        {
            string path="Assets/Harvest/Materials/Farmhouse Exterior "+name+".mat";Material material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material!=null) return material;
            material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",.05f);AssetDatabase.CreateAsset(material,path);return material;
        }
        static void Bolt(Transform root,string name,Material material,Mesh mesh,Vector3 point,Vector3 normal)
        {GameObject go=Visual(root,name,mesh,material,point);go.transform.localRotation=Quaternion.FromToRotation(Vector3.up,normal);}
        static GameObject Proxy(Transform collision,string name,Vector3 point,Vector3 size)
        {var go=new GameObject(name);go.transform.SetParent(collision,false);go.transform.localPosition=point;go.AddComponent<BoxCollider>().size=size;return go;}
        static GameObject Box(Transform root,string name,Material material,Vector3 point,Vector3 size) =>
            Visual(root,name,FarmhouseTimberFinish.IsTimber(material)?FarmhouseTimberFinish.Box(size,.003f):FarmhouseMeshLibrary.Box(size,.003f),material,point);
        static GameObject Visual(Transform root,string name,Mesh mesh,Material material,Vector3 point)
        {
            var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=point;go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;
            GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic);return go;
        }
    }
}
