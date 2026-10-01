using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Harvest.Editor
{
    // Prop modeling/placement is independent of the wall shell and encounter behavior.
    public static class FarmhouseInteriorDressing
    {
        public const string LayoutPath = "Assets/Harvest/Data/Farmhouse Dressing Layout.asset";
        const string PrefabFolder = "Assets/Harvest/Prefabs/Environment/FarmhouseProps";
        sealed class Palette { public Material Wood, Steel, Stone, Paint, Cloth, Linen, Ceramic, Rubber, Paper, Screen; }
        public static void Build(Transform interior, Transform collision, FarmhouseLayout house, Material wood, Material steel, Material stone)
        {
            FarmhouseMeshLibrary.EnsureFolder(PrefabFolder);
            FarmhouseMeshLibrary.EnsureFolder("Assets/Harvest/Data");
            FarmhouseMeshLibrary.EnsureFolder("Assets/Harvest/Materials");
            var settings = AssetDatabase.LoadAssetAtPath<FarmhouseDressingLayout>(LayoutPath);
            if (settings == null) { settings = ScriptableObject.CreateInstance<FarmhouseDressingLayout>(); AssetDatabase.CreateAsset(settings,LayoutPath); }
            var p = new Palette { Wood=wood,Steel=steel,Stone=stone,
                Paint=Material("Utility olive",new Color(.26f,.30f,.23f),.2f,.25f),
                Cloth=Material("Canvas",new Color(.30f,.34f,.30f),0f,.08f),
                Linen=Material("Linen",new Color(.65f,.64f,.57f),0f,.07f),
                Ceramic=Material("Enamel",new Color(.76f,.74f,.65f),0f,.38f),
                Rubber=Material("Rubber",new Color(.08f,.09f,.085f),0f,.08f),
                Paper=Material("Paper",new Color(.68f,.63f,.49f),0f,.08f),
                Screen=Material("Terminal screen",new Color(.07f,.15f,.13f),.05f,.3f) };
            var dressing = new GameObject("Dressing"); dressing.transform.SetParent(interior,false);
            Add("Kitchen",settings.Kitchen,new Vector3(-3.6f,house.GroundFloorTop,house.HalfDepth-.5f),0f,dressing.transform,collision,p,Kitchen);
            Add("Dining",settings.Dining,new Vector3(2.7f,house.GroundFloorTop,3.5f),0f,dressing.transform,collision,p,Dining);
            Add("Farm storage",settings.FarmStorage,new Vector3(house.HalfWidth-.65f,house.GroundFloorTop,-house.HalfDepth+2f),90f,dressing.transform,collision,p,Storage);
            Add("Sleeping",settings.Sleeping,new Vector3(-house.HalfWidth+1f,house.UpperFloorTop,house.HalfDepth-1.4f),0f,dressing.transform,collision,p,Sleeping);
            Add("Work desk",settings.WorkDesk,new Vector3(house.HalfWidth-2.3f,house.UpperFloorTop,-house.HalfDepth+1.2f),180f,dressing.transform,collision,p,Desk);
        }
        static void Add(string name,FarmhouseDressingCluster settings,Vector3 anchor,float yaw,Transform parent,Transform collision,Palette p,Action<Transform,Palette> model)
        {
            if (settings == null || !settings.Enabled) return;
            if (!Finite(settings.Offset.x) || !Finite(settings.Offset.y) || !Finite(settings.Offset.z) || !Finite(settings.YawOffset))
                throw new InvalidOperationException("Nonfinite dressing placement: "+name);
            GameObject asset=settings.AuthoredPrefab;
            if (asset != null && (!AssetDatabase.Contains(asset) || PrefabUtility.GetPrefabAssetType(asset)==PrefabAssetType.NotAPrefab ||
                AssetDatabase.GetAssetPath(asset).StartsWith(PrefabFolder,StringComparison.Ordinal)))
                throw new InvalidOperationException("Dressing overrides must be artist-owned prefabs outside "+PrefabFolder);
            if (asset == null)
            {
                var temp = new GameObject(name);
                try { model(temp.transform,p); asset=PrefabUtility.SaveAsPrefabAsset(temp,PrefabFolder+"/"+name.Replace(" ","")+" Generated.prefab"); }
                finally { Object.DestroyImmediate(temp); }
                if (asset == null) throw new InvalidOperationException("Failed to save dressing prop: "+name);
            }
            if (asset.GetComponentInChildren<Rigidbody>(true) != null || asset.GetComponentInChildren<CombatTarget>(true) != null)
                throw new InvalidOperationException("Use static prop prefabs for dressing overrides: "+name);
            // Copy the assembly; the source prefab remains protected and is refreshed on the next rebuild.
            var instance=Object.Instantiate(asset,parent); instance.name=name;
            if (PrefabUtility.IsPartOfPrefabInstance(instance))
                PrefabUtility.UnpackPrefabInstance(instance,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            instance.transform.localPosition=anchor+settings.Offset;
            instance.transform.localRotation=Quaternion.Euler(0f,yaw+settings.YawOffset,0f);
            foreach (Collider original in instance.GetComponentsInChildren<Collider>(true))
            {
                var proxy=new GameObject("Dressing "+name+" / "+original.name);
                proxy.transform.SetParent(collision,false);
                proxy.transform.position=original.transform.position; proxy.transform.rotation=original.transform.rotation;
                proxy.transform.localScale=original.transform.lossyScale;
                if (!ComponentUtility.CopyComponent(original) || !ComponentUtility.PasteComponentAsNew(proxy)) throw new InvalidOperationException("Cannot copy dressing collision: "+name);
                var surface=original.GetComponent<FootstepSurface>();
                if (surface != null) proxy.AddComponent<FootstepSurface>().Kind=surface.Kind;
                Object.DestroyImmediate(original);
            }
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static Material Material(string name,Color color,float metallic,float smoothness)
        {
            string path="Assets/Harvest/Materials/Farmhouse Prop "+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path); if (material!=null) return material;
            var shader=Shader.Find("Universal Render Pipeline/Lit");
            if (shader==null) throw new InvalidOperationException("URP Lit is unavailable for farmhouse props.");
            material=new Material(shader); material.SetColor("_BaseColor",color);
            material.SetFloat("_Metallic",metallic); material.SetFloat("_Smoothness",smoothness);
            AssetDatabase.CreateAsset(material,path); return material;
        }
        static GameObject Box(Transform root,string name,Material material,Vector3 center,Vector3 size,bool solid=false,float bevel=.006f)
        {
            var go=new GameObject(name); go.transform.SetParent(root,false); go.transform.localPosition=center;
            go.AddComponent<MeshFilter>().sharedMesh=FarmhouseMeshLibrary.Box(size,bevel);
            go.AddComponent<MeshRenderer>().sharedMaterial=material;
            GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic);
            if (solid)
            {
                var body=new GameObject(name+" collision");body.transform.SetParent(root,false);body.transform.localPosition=center;
                body.AddComponent<BoxCollider>().size=size;
                if (AssetDatabase.GetAssetPath(material) == FarmhouseMaterialLibrary.MaterialPath("Timber")) body.AddComponent<FootstepSurface>().Kind=FootstepSurfaceKind.Wood;
            }
            return go;
        }
        static void Vessel(Transform root,string name,Material material,Vector3 position,float radius,float height)
        {
            var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=position;
            go.AddComponent<MeshFilter>().sharedMesh=FarmhousePropMeshes.Vessel(name,radius,height,.012f);
            go.AddComponent<MeshRenderer>().sharedMaterial=material;
            GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic);
        }
        static void Kitchen(Transform root,Palette p)
        {
            Box(root,"Counter carcass",p.Wood,new Vector3(0,.48f,0),new Vector3(3f,.96f,.72f),true,.015f);
            Box(root,"Worktop",p.Stone,new Vector3(0,.99f,0),new Vector3(3.1f,.06f,.8f));
            for(int i=0;i<4;i++)
            {
                float x=-1.11f+i*.74f;
                Box(root,"Cabinet door "+i,p.Paint,new Vector3(x,.42f,-.37f),new Vector3(.70f,.71f,.035f));
                Box(root,"Drawer "+i,p.Paint,new Vector3(x,.85f,-.37f),new Vector3(.70f,.14f,.035f));
                Box(root,"Door pull "+i,p.Steel,new Vector3(x+.23f,.62f,-.405f),new Vector3(.025f,.12f,.025f));
                Box(root,"Drawer pull "+i,p.Steel,new Vector3(x,.85f,-.405f),new Vector3(.18f,.025f,.025f));
            }
            // A raised utility sink with a visible recessed basin; the cabinet surface is its bottom.
            Box(root,"Sink base",p.Steel,new Vector3(.48f,1.035f,0),new Vector3(.67f,.025f,.46f));
            for(int sign=-1;sign<=1;sign+=2)
            {
                Box(root,"Sink side "+sign,p.Steel,new Vector3(.48f+sign*.32f,1.11f,0),new Vector3(.035f,.15f,.46f));
                Box(root,"Sink end "+sign,p.Steel,new Vector3(.48f,1.11f,sign*.22f),new Vector3(.67f,.15f,.025f));
            }
            Box(root,"Tap stem",p.Steel,new Vector3(.48f,1.21f,.27f),new Vector3(.035f,.36f,.035f));
            Box(root,"Tap spout",p.Steel,new Vector3(.48f,1.37f,.19f),new Vector3(.035f,.035f,.19f));
            Box(root,"Upper cupboard",p.Wood,new Vector3(-.65f,1.96f,.195f),new Vector3(1.6f,.70f,.35f),true);
            for(int i=0;i<2;i++)
            {
                float x=-1.04f+i*.78f;
                Box(root,"Upper door "+i,p.Paint,new Vector3(x,1.96f,.005f),new Vector3(.74f,.65f,.025f));
                Box(root,"Upper pull "+i,p.Steel,new Vector3(x+(i==0?.28f:-.28f),1.91f,-.02f),new Vector3(.025f,.14f,.025f));
            }
            Vessel(root,"Cooking pot",p.Steel,new Vector3(-.95f,1.02f,.05f),.16f,.19f);
            Mug(root,p,new Vector3(-.46f,1.02f,-.14f));
            Box(root,"Cutting board",p.Wood,new Vector3(1.12f,1.035f,-.07f),new Vector3(.44f,.025f,.30f));
        }
        static void Dining(Transform root,Palette p)
        {
            Box(root,"Table top",p.Wood,new Vector3(0,.78f,0),new Vector3(2.2f,.10f,1.1f),true,.018f);
            for(int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2)
                Box(root,"Table leg "+x+" "+z,p.Wood,new Vector3(x*.87f,.365f,z*.40f),new Vector3(.11f,.73f,.11f),true);
            for(int side=-1;side<=1;side+=2)
            {
                Box(root,"Bench seat "+side,p.Wood,new Vector3(0,.44f,side*.89f),new Vector3(1.8f,.08f,.34f),true);
                for(int x=-1;x<=1;x+=2) Box(root,"Bench support "+side+" "+x,p.Wood,new Vector3(x*.63f,.20f,side*.89f),new Vector3(.14f,.40f,.28f),true);
            }
            Mug(root,p,new Vector3(-.58f,.83f,-.26f));
            Vessel(root,"Enamel bowl",p.Ceramic,new Vector3(.12f,.83f,.10f),.13f,.07f);
            Box(root,"Folded cloth",p.Cloth,new Vector3(.72f,.85f,.18f),new Vector3(.33f,.04f,.23f));
            Book(root,p,new Vector3(.40f,.84f,-.28f));
        }
        static void Storage(Transform root,Palette p)
        {
            for(int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2)
                Box(root,"Rack upright "+x+" "+z,p.Steel,new Vector3(x*.80f,.98f,z*.26f),new Vector3(.065f,1.96f,.065f));
            for(int i=0;i<3;i++)
            {
                float y=.18f+i*.61f;
                Box(root,"Rack shelf "+i,p.Wood,new Vector3(0,y,0),new Vector3(1.7f,.06f,.60f));
                for(int k=0;k<2;k++)
                {
                    float x=-.43f+k*.86f;
                    Box(root,"Seed bin "+i+" "+k,p.Paint,new Vector3(x,y+.22f,0),new Vector3(.69f,.37f,.46f));
                    Box(root,"Seed label "+i+" "+k,p.Paper,new Vector3(x,y+.25f,-.238f),new Vector3(.20f,.085f,.012f));
                    Box(root,"Bin handle "+i+" "+k,p.Steel,new Vector3(x,y+.12f,-.25f),new Vector3(.19f,.025f,.022f));
                }
            }
            // One coarse collision envelope catches grenades/bullets without six tiny bin colliders.
            var envelope=new GameObject("Rack contents collision");envelope.transform.SetParent(root,false);
            envelope.AddComponent<BoxCollider>().center=new Vector3(0,1f,0);envelope.GetComponent<BoxCollider>().size=new Vector3(1.72f,1.96f,.62f);
            Box(root,"Irrigation controller",p.Paint,new Vector3(0,2.20f,.05f),new Vector3(.60f,.30f,.22f));
            Box(root,"Controller dial",p.Ceramic,new Vector3(-.18f,2.21f,-.07f),new Vector3(.10f,.10f,.025f));
            Box(root,"Controller display",p.Screen,new Vector3(.12f,2.23f,-.07f),new Vector3(.22f,.12f,.015f));
        }
        static void Sleeping(Transform root,Palette p)
        {
            Box(root,"Cot frame",p.Steel,new Vector3(0,.36f,0),new Vector3(1.25f,.12f,2f),true);
            for(int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2)
                Box(root,"Cot leg "+x+" "+z,p.Steel,new Vector3(x*.51f,.15f,z*.83f),new Vector3(.055f,.30f,.055f),true);
            Box(root,"Mattress",p.Cloth,new Vector3(0,.49f,0),new Vector3(1.18f,.14f,1.93f),true,.025f);
            Box(root,"Folded blanket",p.Cloth,new Vector3(0,.59f,-.47f),new Vector3(1.16f,.065f,.77f),false,.014f);
            Box(root,"Pillow",p.Linen,new Vector3(0,.61f,.64f),new Vector3(.65f,.10f,.36f),false,.035f);
            Box(root,"Personal chest",p.Wood,new Vector3(1.02f,.26f,.55f),new Vector3(.62f,.52f,.62f),true,.015f);
            Box(root,"Chest lid",p.Wood,new Vector3(1.02f,.535f,.55f),new Vector3(.65f,.04f,.65f));
            Box(root,"Chest latch",p.Steel,new Vector3(1.02f,.46f,.22f),new Vector3(.06f,.10f,.035f));
            Book(root,p,new Vector3(1.04f,.575f,.48f));
            for(int i=0;i<2;i++)
            {
                Box(root,"Work boot toe "+i,p.Rubber,new Vector3(.81f+i*.23f,.08f,-.44f),new Vector3(.16f,.16f,.31f),false,.02f);
                Box(root,"Work boot cuff "+i,p.Rubber,new Vector3(.81f+i*.23f,.23f,-.36f),new Vector3(.14f,.30f,.13f),false,.015f);
            }
        }
        static void Desk(Transform root,Palette p)
        {
            Box(root,"Work desk top",p.Wood,new Vector3(0,.76f,0),new Vector3(1.8f,.09f,.74f),true,.015f);
            Box(root,"Desk storage",p.Paint,new Vector3(-.58f,.35f,0),new Vector3(.47f,.70f,.63f),true);
            for(int side=-1;side<=1;side+=2) Box(root,"Desk leg "+side,p.Steel,new Vector3(.69f,.35f,side*.26f),new Vector3(.05f,.70f,.05f),true);
            Box(root,"Terminal base",p.Paint,new Vector3(.32f,.85f,.13f),new Vector3(.56f,.09f,.38f));
            Box(root,"Terminal housing",p.Paint,new Vector3(.32f,1.10f,.20f),new Vector3(.57f,.42f,.19f));
            Box(root,"Terminal glass",p.Screen,new Vector3(.32f,1.11f,.095f),new Vector3(.45f,.29f,.02f));
            for(int i=0;i<3;i++) Box(root,"Terminal field rows "+i,p.Cloth,new Vector3(.29f,1.19f-i*.07f,.082f),new Vector3(.30f-i*.05f,.013f,.007f));
            Box(root,"Keyboard",p.Rubber,new Vector3(.32f,.82f,-.16f),new Vector3(.47f,.025f,.16f));
            Book(root,p,new Vector3(-.42f,.83f,-.03f));
            // Framed field plan: a few inset plot boundaries establish agricultural context.
            Box(root,"Field plan frame",p.Wood,new Vector3(-.47f,1.04f,.24f),new Vector3(.33f,.35f,.04f));
            Box(root,"Field plan paper",p.Paper,new Vector3(-.47f,1.04f,.214f),new Vector3(.28f,.30f,.012f));
            for(int i=0;i<3;i++) Box(root,"Plot boundary "+i,p.Paint,new Vector3(-.47f,1.12f-i*.075f,.205f),new Vector3(.22f,.008f,.008f));
        }
        static void Mug(Transform root,Palette p,Vector3 position)
        {
            Vessel(root,"Enamel mug",p.Ceramic,position,.067f,.12f);
            Box(root,"Mug handle top",p.Ceramic,position+new Vector3(.082f,.095f,0),new Vector3(.055f,.016f,.018f));
            Box(root,"Mug handle side",p.Ceramic,position+new Vector3(.109f,.064f,0),new Vector3(.016f,.075f,.018f));
            Box(root,"Mug handle bottom",p.Ceramic,position+new Vector3(.082f,.032f,0),new Vector3(.055f,.016f,.018f));
        }
        static void Book(Transform root,Palette p,Vector3 position)
        {
            Box(root,"Notebook pages",p.Paper,position,new Vector3(.21f,.035f,.29f));
            for(int sign=-1;sign<=1;sign+=2) Box(root,"Notebook cover "+sign,p.Cloth,position+Vector3.up*(sign*.022f),new Vector3(.23f,.009f,.31f));
            Box(root,"Notebook spine",p.Cloth,position+Vector3.left*.112f,new Vector3(.014f,.047f,.31f));
        }
    }
}
