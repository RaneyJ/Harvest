using System.Collections.Generic;
using UnityEngine;

namespace Harvest
{
    // Shared authoring coordinates for room geometry, dressing, light placement and review checks.
    // These descriptions have no runtime behavior or encounter dependencies.
    public enum FarmhouseRoomKind { Utility, Bedroom, Office }
    public readonly struct FarmhouseRoom
    {
        public readonly FarmhouseRoomKind Kind;
        public readonly string Name;
        public readonly float Left, Right, Front, Rear, Floor, DoorRun, DoorWidth;
        public readonly bool DoorAlongZ;
        public FarmhouseRoom(FarmhouseRoomKind kind,string name,float left,float right,float front,float rear,float floor,bool doorAlongZ,float doorRun,float doorWidth)
        {Kind=kind;Name=name;Left=left;Right=right;Front=front;Rear=rear;Floor=floor;DoorAlongZ=doorAlongZ;DoorRun=doorRun;DoorWidth=doorWidth;}
        public Vector3 Center => new Vector3((Left+Right)*.5f,Floor,(Front+Rear)*.5f);
        public Vector3 Entry => DoorAlongZ?new Vector3(Left,Floor,DoorRun):new Vector3(DoorRun,Floor,Front);
        public Vector3 Inward => DoorAlongZ?Vector3.right:Vector3.forward;
    }
    public static class FarmhouseRoomPlan
    {
        public const float PartitionThickness=.16f;
        public const float DoorHeight=2.45f;
        public static FarmhouseRoom[] Create(FarmhouseLayout l)
        {
            float inset=l.WallThickness*.5f;
            var rooms=new List<FarmhouseRoom>();
            rooms.Add(new FarmhouseRoom(FarmhouseRoomKind.Utility,"Utility pantry",2f,l.HalfWidth-inset,l.HalfDepth-2.5f,l.HalfDepth-inset,l.GroundFloorTop,false,
                (2f+l.HalfWidth-inset)*.5f,1.6f));
            // Long artist-tuned stairs can occupy the rear bay. Keep its bed open rather than blocking a landing.
            if(l.StairEnd+.85f<l.HalfDepth-2.5f)
                rooms.Add(new FarmhouseRoom(FarmhouseRoomKind.Bedroom,"Sleeping room",-l.HalfWidth+inset,-1.55f,l.HalfDepth-2.5f,l.HalfDepth-inset,l.UpperFloorTop,false,
                    (-l.HalfWidth+inset-1.55f)*.5f,1.6f));
            rooms.Add(new FarmhouseRoom(FarmhouseRoomKind.Office,"Farm office",2.8f,l.HalfWidth-inset,-l.HalfDepth+inset,-2.5f,l.UpperFloorTop,true,(-l.HalfDepth+inset-2.5f)*.5f,1.6f));
            return rooms.ToArray();
        }
    }
}
