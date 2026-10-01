using UnityEngine;
using Palette=Harvest.Editor.FarmhouseInteriorDressing.Palette;

namespace Harvest.Editor
{
    // Static, separately replaceable prop clusters drawn from the approved interior study.
    internal static class FarmhouseConceptDetails
    {
        static GameObject Box(Transform root,string name,Material material,Vector3 center,Vector3 size,bool solid=false,float bevel=.006f) =>
            FarmhouseInteriorDressing.Box(root,name,material,center,size,solid,bevel);
        static void Vessel(Transform root,string name,Material material,Vector3 point,float radius,float height) => FarmhouseInteriorDressing.Vessel(root,name,material,point,radius,height);
        public static void KitchenUtilities(Transform root,Palette p)
        {
            Box(root,"Farm control backing",p.Steel,new Vector3(.97f,1.97f,.34f),new Vector3(.70f,.52f,.07f));
            Box(root,"Farm control inset",p.Paint,new Vector3(.97f,1.97f,.295f),new Vector3(.62f,.44f,.026f));
            Box(root,"Crop status glass",p.Screen,new Vector3(.97f,1.97f,.273f),new Vector3(.51f,.33f,.018f));
            for(int i=0;i<4;i++)
            {
                Box(root,"Crop status row "+i,p.Cloth,new Vector3(.91f,2.07f-i*.055f,.259f),new Vector3(.29f-i*.035f,.012f,.010f));
                Box(root,"Control fastener "+i,p.Steel,new Vector3(.97f+(i%2==0?-1:1)*.30f,1.97f+(i<2?-1:1)*.21f,.276f),new Vector3(.026f,.026f,.012f));
            }
            FarmhouseRooms.Pipe(root,"Kitchen wall conduit",p.Steel,new Vector3(-1.4f,2.55f,.35f),new Vector3(1.5f,2.55f,.35f),.013f);
            FarmhouseRooms.Pipe(root,"Farm control conduit",p.Steel,new Vector3(.97f,2.23f,.35f),new Vector3(.97f,2.55f,.35f),.013f);
            foreach(float x in new[]{-1.2f,0f,1.2f})
                Box(root,"Conduit saddle "+x,p.Steel,new Vector3(x,2.55f,.365f),new Vector3(.065f,.07f,.04f));
        }
        public static void Pantry(Transform root,Palette p)
        {
            Box(root,"Pantry lower cupboard",p.Paint,new Vector3(0,.40f,0),new Vector3(2.7f,.80f,.42f),true,.012f);
            for(int i=0;i<3;i++)
            {
                float x=-.9f+i*.9f;
                Box(root,"Pantry cabinet door "+i,p.Wood,new Vector3(x,.43f,-.228f),new Vector3(.87f,.69f,.032f));
                Box(root,"Pantry cabinet pull "+i,p.Steel,new Vector3(x+.29f,.48f,-.255f),new Vector3(.018f,.12f,.018f));
            }
            for(int i=0;i<3;i++)
            {
                float y=1.22f+i*.51f;
                Box(root,"Pantry shelf "+i,p.Wood,new Vector3(0,y,.04f),new Vector3(2.7f,.055f,.34f));
                foreach(float x in new[]{-1.1f,1.1f})
                {Box(root,"Shelf wall strap "+i+" "+x,p.Steel,new Vector3(x,y-.10f,.205f),new Vector3(.035f,.24f,.025f));Box(root,"Shelf angle "+i+" "+x,p.Steel,new Vector3(x,y-.045f,.05f),new Vector3(.035f,.025f,.31f));}
                for(int j=0;j<4;j++)
                {
                    float x=-.97f+j*.63f,radius=.078f+(j%2)*.025f,height=.17f+(j%3)*.035f;
                    Jar(root,p,new Vector3(x,y+.0275f,.03f),radius,height,"Preserves "+i+" "+j);
                }
            }
            Vessel(root,"Water pitcher",p.Ceramic,new Vector3(-.88f,.80f,-.04f),.12f,.27f);
            Box(root,"Folded pantry towels",p.Linen,new Vector3(.40f,.845f,-.03f),new Vector3(.43f,.09f,.28f));
            Jar(root,p,new Vector3(1f,.80f,0),.10f,.20f,"Seed measure");
        }
        public static void Entry(Transform root,Palette p)
        {
            Box(root,"Entry coat rail",p.Wood,new Vector3(0,1.67f,-.018f),new Vector3(1.5f,.12f,.036f));
            for(int i=0;i<4;i++)
            {
                float x=-.56f+i*.37f;
                FarmhouseRooms.Pipe(root,"Coat hook stem "+i,p.Steel,new Vector3(x,1.66f,-.04f),new Vector3(x,1.66f,-.115f),.012f);
                FarmhouseRooms.Pipe(root,"Coat hook lip "+i,p.Steel,new Vector3(x,1.66f,-.115f),new Vector3(x,1.70f,-.115f),.012f);
            }
            Box(root,"Hung work coat",p.Cloth,new Vector3(-.36f,1.17f,-.16f),new Vector3(.34f,.70f,.065f),false,.023f);
            foreach(int side in new[]{-1,1})
            {
                GameObject sleeve=Box(root,"Coat sleeve "+side,p.Cloth,new Vector3(-.36f+side*.22f,1.26f,-.16f),new Vector3(.12f,.47f,.06f),false,.020f);
                sleeve.transform.localRotation=Quaternion.Euler(0,0,side*12);
            }
            Box(root,"Coat placket",p.Paint,new Vector3(-.36f,1.17f,-.199f),new Vector3(.025f,.60f,.015f));
            Box(root,"Entry bench",p.Wood,new Vector3(0,.44f,-.28f),new Vector3(1.5f,.08f,.40f),true,.012f);
            foreach(int side in new[]{-1,1}) Box(root,"Entry bench foot "+side,p.Wood,new Vector3(side*.57f,.20f,-.28f),new Vector3(.12f,.40f,.35f),true);
            Case(root,p,new Vector3(.37f,.48f,-.28f),new Vector3(.43f,.22f,.28f),"Household tool case",false);
            Box(root,"Boot tray",p.Rubber,new Vector3(-.35f,.02f,-.65f),new Vector3(.55f,.04f,.37f));
            for(int i=0;i<2;i++)
            {Box(root,"Entry boot toe "+i,p.Rubber,new Vector3(-.48f+i*.23f,.12f,-.65f),new Vector3(.17f,.16f,.29f),false,.018f);Box(root,"Entry boot cuff "+i,p.Rubber,new Vector3(-.48f+i*.23f,.23f,-.57f),new Vector3(.15f,.25f,.13f),false,.02f);}
        }
        public static void Bedroom(Transform root,Palette p)
        {
            Box(root,"Wardrobe body",p.Wood,new Vector3(0,.96f,0),new Vector3(1f,1.92f,.68f),true,.015f);
            Box(root,"Wardrobe crown",p.Wood,new Vector3(0,1.94f,0),new Vector3(1.06f,.07f,.72f));
            for(int side=-1;side<=1;side+=2)
            {
                Box(root,"Wardrobe door "+side,p.Paint,new Vector3(side*.245f,.98f,-.355f),new Vector3(.47f,1.78f,.035f));
                Box(root,"Wardrobe pull "+side,p.Steel,new Vector3(side*.055f,.96f,-.382f),new Vector3(.025f,.15f,.02f));
                foreach(float y in new[]{.47f,1.48f}) Box(root,"Wardrobe hinge "+side+" "+y,p.Steel,new Vector3(side*.47f,y,-.378f),new Vector3(.035f,.09f,.015f));
            }
            Box(root,"Folded bedding",p.Linen,new Vector3(0,2.06f,0),new Vector3(.70f,.17f,.49f),false,.024f);
        }
        public static void Office(Transform root,Palette p)
        {
            // A chair left off-center keeps a standing approach to the existing terminal clear.
            Vector3 chair=new Vector3(.65f,0,-.3f);
            Box(root,"Office chair seat",p.Wood,chair+new Vector3(0,.45f,0),new Vector3(.46f,.07f,.44f),true,.012f);
            for(int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2)
                Box(root,"Chair leg "+x+" "+z,p.Wood,chair+new Vector3(x*.17f,.2075f,z*.16f),new Vector3(.055f,.415f,.055f),true);
            foreach(int side in new[]{-1,1}) Box(root,"Chair back upright "+side,p.Wood,chair+new Vector3(side*.18f,.66f,.18f),new Vector3(.05f,.52f,.05f));
            for(int i=0;i<3;i++) Box(root,"Chair back slat "+i,p.Wood,chair+new Vector3(0,.66f+i*.09f,.18f),new Vector3(.40f,.065f,.03f));
            float shelfX=1.26f;
            foreach(int side in new[]{-1,1}) Box(root,"Office bookcase side "+side,p.Wood,new Vector3(shelfX,.87f,.57f+side*.56f),new Vector3(.34f,1.74f,.055f));
            for(int i=0;i<4;i++)
            {
                float y=.17f+i*.48f;
                Box(root,"Office bookshelf "+i,p.Wood,new Vector3(shelfX,y,.57f),new Vector3(.34f,.055f,1.12f));
                for(int j=0;j<4;j++)
                {
                    float z=.17f+j*.23f;
                    Box(root,"Farm ledger "+i+" "+j,p.Cloth,new Vector3(shelfX,y+.1475f,z),new Vector3(.23f,.24f,.055f));
                    Box(root,"Ledger spine label "+i+" "+j,p.Paper,new Vector3(shelfX-.12f,y+.17f,z),new Vector3(.008f,.10f,.035f));
                }
            }
            var envelope=new GameObject("Office bookcase collision");envelope.transform.SetParent(root,false);envelope.AddComponent<BoxCollider>().center=new Vector3(shelfX,.87f,.57f);envelope.GetComponent<BoxCollider>().size=new Vector3(.34f,1.74f,1.18f);
        }
        public static void Firing(Transform root,Palette p)
        {
            Case(root,p,new Vector3(-.43f,0,0),new Vector3(.62f,.55f,.48f),"Transit case A",true);
            Case(root,p,new Vector3(.38f,0,.02f),new Vector3(.67f,.42f,.46f),"Transit case B",true);
            Case(root,p,new Vector3(.38f,.42f,.02f),new Vector3(.54f,.28f,.40f),"Field radio case",true);
            Vessel(root,"Window enamel mug",p.Ceramic,new Vector3(-.22f,.55f,-.10f),.065f,.11f);
            Box(root,"Transit case notebook",p.Cloth,new Vector3(-.47f,.573f,.10f),new Vector3(.21f,.046f,.25f));
            Box(root,"Radio face",p.Rubber,new Vector3(.38f,.55f,-.191f),new Vector3(.39f,.17f,.02f));
            Box(root,"Radio display",p.Screen,new Vector3(.30f,.56f,-.207f),new Vector3(.15f,.07f,.012f));
            FarmhouseRooms.Pipe(root,"Radio antenna",p.Steel,new Vector3(.58f,.70f,.09f),new Vector3(.58f,1.03f,.09f),.007f);
        }
        static void Jar(Transform root,Palette p,Vector3 basePoint,float radius,float height,string name)
        {
            Mesh mesh=FarmMachineryMeshes.Revolve("Pantry_jar_"+radius.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+"_"+height.ToString("F3",System.Globalization.CultureInfo.InvariantCulture),
                new Vector2(0,0),new Vector2(radius*.85f,0),new Vector2(radius,height*.12f),new Vector2(radius,height*.80f),new Vector2(radius*.7f,height*.92f),new Vector2(radius*.7f,height),new Vector2(0,height));
            var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=basePoint;go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=p.Ceramic;
            UnityEditor.GameObjectUtility.SetStaticEditorFlags(go,UnityEditor.StaticEditorFlags.ContributeGI|UnityEditor.StaticEditorFlags.BatchingStatic);
            Box(root,name+" lid",p.Steel,basePoint+Vector3.up*(height+.006f),new Vector3(radius*1.5f,.012f,radius*1.5f));
            Box(root,name+" label",p.Paper,basePoint+new Vector3(0,height*.47f,-radius-.002f),new Vector3(radius*1.10f,height*.33f,.008f));
        }
        static void Case(Transform root,Palette p,Vector3 basePoint,Vector3 size,string name,bool solid)
        {
            Box(root,name+" body",p.Paint,basePoint+Vector3.up*(size.y*.5f),size,solid,.018f);
            Box(root,name+" lid",p.Paint,basePoint+Vector3.up*(size.y-.014f),new Vector3(size.x+.016f,.028f,size.z+.016f),false,.008f);
            foreach(int side in new[]{-1,1})
            {
                float x=side*(size.x*.5f-.10f);
                Box(root,name+" latch "+side,p.Steel,basePoint+new Vector3(x,size.y-.065f,-size.z*.5f-.012f),new Vector3(.042f,.085f,.025f));
                Box(root,name+" reinforcing strip "+side,p.Steel,basePoint+new Vector3(x,size.y*.5f,-size.z*.5f-.005f),new Vector3(.028f,size.y-.05f,.012f));
            }
            Box(root,name+" handle",p.Rubber,basePoint+new Vector3(0,size.y*.56f,-size.z*.5f-.035f),new Vector3(size.x*.30f,.035f,.035f));
            Box(root,name+" inventory plate",p.Paper,basePoint+new Vector3(0,size.y*.26f,-size.z*.5f-.010f),new Vector3(size.x*.23f,.065f,.012f));
        }
    }
}
