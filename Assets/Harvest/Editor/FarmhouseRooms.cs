using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Harvest.Editor
{
    // Interior partitions are separate from the exterior shell and the existing stairs/floor collision.
    public static class FarmhouseRooms
    {
        public static void Build(Transform interior,Transform collision,FarmhouseLayout l,Material plaster,Material wood,Material steel)
        {
            var root=new GameObject("Rooms").transform;root.SetParent(interior,false);
            foreach(FarmhouseRoom room in FarmhouseRoomPlan.Create(l))
            {
                var group=new GameObject(room.Name).transform;group.SetParent(root,false);
                float ceiling=room.Kind==FarmhouseRoomKind.Utility?l.UpperFloorTop-.18f:l.EavesHeight-.14f;
                float height=ceiling-room.Floor;
                if(room.DoorAlongZ)
                {
                    Wall(group,collision,l,room,true,room.Left,room.Front,room.Rear,height,plaster,wood,steel,true);
                    Wall(group,collision,l,room,false,room.Rear,room.Left,room.Right,height,plaster,wood,steel,false);
                }
                else
                {
                    Wall(group,collision,l,room,false,room.Front,room.Left,room.Right,height,plaster,wood,steel,true);
                    float side=room.Kind==FarmhouseRoomKind.Bedroom?room.Right:room.Left;
                    Wall(group,collision,l,room,true,side,room.Front,room.Rear,height,plaster,wood,steel,false);
                }
            }
            Structure(root,collision,l,wood,steel);
        }
        static void Wall(Transform group,Transform collision,FarmhouseLayout l,FarmhouseRoom room,bool alongZ,float axis,float start,float end,float height,Material plaster,Material wood,Material steel,bool door)
        {
            var sections=new List<Bounds>();string name=room.Name+(alongZ?" side":" end");
            float half=FarmhouseRoomPlan.PartitionThickness*.5f;
            float left=door?room.DoorRun-room.DoorWidth*.5f:end,right=door?room.DoorRun+room.DoorWidth*.5f:end;
            Panel(start,left,room.Floor,height,"left");
            if(door)
            {
                Panel(right,end,room.Floor,height,"right");
                Panel(left,right,room.Floor+FarmhouseRoomPlan.DoorHeight,height-FarmhouseRoomPlan.DoorHeight,"lintel");
                foreach(int face in new[]{-1,1})
                {
                    float trimAxis=axis+face*(half+.022f);
                    Box(group,name+" door header "+face,wood,Point(alongZ,trimAxis,room.DoorRun,room.Floor+FarmhouseRoomPlan.DoorHeight+.045f),Size(alongZ,room.DoorWidth+.18f,.09f,.046f));
                    foreach(int side in new[]{-1,1})
                        Box(group,name+" door casing "+face+" "+side,wood,Point(alongZ,trimAxis,room.DoorRun+side*(room.DoorWidth*.5f+.04f),room.Floor+FarmhouseRoomPlan.DoorHeight*.5f),Size(alongZ,.08f,FarmhouseRoomPlan.DoorHeight,.046f));
                }
            }
            var go=new GameObject(name+" plaster");go.transform.SetParent(group,false);
            go.AddComponent<MeshFilter>().sharedMesh=FarmhouseMeshLibrary.Wall(name,sections);go.AddComponent<MeshRenderer>().sharedMaterial=plaster;
            GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic);
            // Foot-level trim follows actual wall sections; the doorway has no raised threshold.
            foreach(int face in new[]{-1,1})
            {
                Skirt(start,left,face);if(door) Skirt(right,end,face);
            }
            foreach(float run in new[]{start,end})
            {
                Box(group,name+" timber end "+run,wood,Point(alongZ,axis,run,room.Floor+height*.5f),Size(alongZ,.09f,height,FarmhouseRoomPlan.PartitionThickness+.02f));
                foreach(int face in new[]{-1,1})
                {
                    Box(group,name+" steel strap "+run+" "+face,steel,Point(alongZ,axis+face*(half+.021f),run,room.Floor+height-.16f),Size(alongZ,.075f,.25f,.012f));
                }
            }
            void Panel(float a,float b,float floor,float h,string suffix)
            {
                if(b-a<.01f||h<.01f) return;
                Vector3 size=Size(alongZ,b-a,h,FarmhouseRoomPlan.PartitionThickness),center=Point(alongZ,axis,(a+b)*.5f,floor+h*.5f);
                sections.Add(new Bounds(center,size));var proxy=new GameObject(name+" "+suffix+" collision");proxy.transform.SetParent(collision,false);proxy.transform.localPosition=center;proxy.AddComponent<BoxCollider>().size=size;
            }
            void Skirt(float a,float b,int face)
            {
                if(b-a<.02f) return;
                Box(group,name+" skirting "+face+" "+a,wood,Point(alongZ,axis+face*(half+.015f),(a+b)*.5f,room.Floor+.065f),Size(alongZ,b-a,.13f,.032f));
            }
        }
        static void Structure(Transform root,Transform collision,FarmhouseLayout l,Material wood,Material steel)
        {
            // Broad framed transition keeps the open kitchen/dining readable from the entrance.
            float portalLeft=l.StairwellRight+.30f,portalRight=l.HalfWidth-l.WallThickness*.5f-.10f;
            float y=l.UpperFloorTop-.34f;
            Box(root,"Kitchen dining portal lintel",wood,new Vector3((portalLeft+portalRight)*.5f,y,-1.3f),new Vector3(portalRight-portalLeft,.22f,.20f));
            foreach(float x in new[]{portalLeft,portalRight})
            {
                Box(root,"Portal timber upright "+x,wood,new Vector3(x,(l.GroundFloorTop+y)*.5f,-1.3f),new Vector3(.16f,y-l.GroundFloorTop,.18f));
                var proxy=new GameObject("Kitchen portal post collision "+x);proxy.transform.SetParent(collision,false);proxy.transform.localPosition=new Vector3(x,(l.GroundFloorTop+y)*.5f,-1.3f);proxy.AddComponent<BoxCollider>().size=new Vector3(.16f,y-l.GroundFloorTop,.18f);
                Box(root,"Portal steel bracket "+x,steel,new Vector3(x,y-.18f,-1.41f),new Vector3(.14f,.38f,.02f));
            }
            foreach(float z in new[]{-l.HalfDepth+2.25f,0f,l.HalfDepth-2.9f})
                Box(root,"Upper exposed ceiling beam "+z,wood,new Vector3(0,l.EavesHeight-.25f,z),new Vector3(l.HalfWidth*2f-l.WallThickness,.20f,.17f));
            float railX=l.StairX-l.StairWidth*.5f-.18f;
            Vector3 from=new Vector3(railX,l.TreadTop(0)+.85f,l.StairStart),to=new Vector3(railX,l.UpperFloorTop+.85f,l.StairEnd);
            Pipe(root,"Rounded stair handrail",steel,from,to,.028f);
            for(int i=0;i<4;i++)
            {
                Vector3 point=Vector3.Lerp(from,to,i/3f);
                Box(root,"Handrail wall plate "+i,steel,new Vector3(-l.HalfWidth+l.WallThickness*.5f+.01f,point.y-.08f,point.z),new Vector3(.025f,.18f,.11f));
                Pipe(root,"Handrail bracket "+i,steel,new Vector3(-l.HalfWidth+l.WallThickness*.5f+.02f,point.y-.055f,point.z),point-Vector3.up*.055f,.012f);
                Pipe(root,"Handrail support "+i,steel,point-Vector3.up*.055f,point,.012f);
            }
        }
        public static void Pipe(Transform root,string name,Material material,Vector3 from,Vector3 to,float radius)
        {
            float length=Vector3.Distance(from,to);
            Mesh mesh=FarmMachineryMeshes.Revolve("Interior_pipe_"+radius.ToString("F4",System.Globalization.CultureInfo.InvariantCulture)+"_"+length.ToString("F4",System.Globalization.CultureInfo.InvariantCulture),
                new Vector2(0,0),new Vector2(radius,0),new Vector2(radius,length),new Vector2(0,length));
            var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=from;go.transform.localRotation=Quaternion.FromToRotation(Vector3.up,to-from);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic);
        }
        static Vector3 Point(bool alongZ,float axis,float run,float y) => alongZ?new Vector3(axis,y,run):new Vector3(run,y,axis);
        static Vector3 Size(bool alongZ,float width,float height,float depth) => alongZ?new Vector3(depth,height,width):new Vector3(width,height,depth);
        static void Box(Transform root,string name,Material material,Vector3 center,Vector3 size)
        {
            var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=center;
            go.AddComponent<MeshFilter>().sharedMesh=FarmhouseTimberFinish.IsTimber(material)?FarmhouseTimberFinish.Box(size,.006f):FarmhouseMeshLibrary.Box(size,.003f);
            go.AddComponent<MeshRenderer>().sharedMaterial=material;GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic);
        }
    }
}
