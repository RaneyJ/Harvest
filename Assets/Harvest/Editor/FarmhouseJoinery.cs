using UnityEditor;
using UnityEngine;

namespace Harvest.Editor
{
    // Visual finishes only. The foundation builder retains ownership of all collision and openings.
    public static class FarmhouseJoinery
    {
        public static void Skirting(Transform root, Material wood, FarmhouseLayout l, bool alongZ,
            float axis, int outside, float start, float end, float floor, string name)
        {
            if (end-start < .02f) return;
            float inside=axis-outside*(l.WallThickness*.5f+.018f);
            Box(root,name+" skirting",wood,Point(alongZ,inside,(start+end)*.5f,floor+.065f),Size(alongZ,end-start,.13f,.036f));
            Box(root,name+" skirting cap",wood,Point(alongZ,inside-outside*.004f,(start+end)*.5f,floor+.135f),Size(alongZ,end-start,.016f,.044f),.003f);
        }
        public static void Opening(Transform root, Material wood, FarmhouseLayout l, bool alongZ,
            float axis, int outside, float center, float width, float sill, float top, float floor, string name)
        {
            float inside=axis-outside*(l.WallThickness*.5f+.025f);
            float bottom=sill>0f?floor+sill:floor+l.GroundFloorTop;
            Box(root,name+" interior header",wood,Point(alongZ,inside,center,floor+top+.045f),Size(alongZ,width+.15f,.09f,.05f));
            foreach(int side in new[]{-1,1})
                Box(root,name+" interior casing "+side,wood,Point(alongZ,inside,center+side*(width*.5f+.0375f),(bottom+floor+top)*.5f),
                    Size(alongZ,.075f,floor+top-bottom,.05f));
            if(sill>0f)
            {
                // One board sits above the plaster reveal and serves both interior and exterior.
                Box(root,name+" deep timber sill",wood,Point(alongZ,axis,center,floor+sill+.025f),
                    Size(alongZ,width+.18f,.05f,l.WallThickness+.22f),.005f);
                Box(root,name+" sill apron",wood,Point(alongZ,axis-outside*(l.WallThickness*.5f+.021f),center,floor+sill-.035f),Size(alongZ,width+.08f,.07f,.045f));
            }
        }
        public static void Ceilings(Transform root, Material plaster, FarmhouseLayout l, float floorThickness)
        {
            float inner=l.StairwellRight,strip=inner+l.HalfWidth;
            float y=l.UpperFloorTop-floorThickness-.01f;
            Box(root,"Ground ceiling main",plaster,new Vector3((inner+l.HalfWidth)*.5f,y,0),new Vector3(l.HalfWidth-inner,.02f,l.HalfDepth*2f),.001f);
            Box(root,"Ground ceiling front",plaster,new Vector3(-l.HalfWidth+strip*.5f,y,(-l.HalfDepth+l.StairStart)*.5f),
                new Vector3(strip,.02f,l.StairStart+l.HalfDepth),.001f);
            Box(root,"Ground ceiling rear",plaster,new Vector3(-l.HalfWidth+strip*.5f,y,(l.StairEnd+l.HalfDepth)*.5f),
                new Vector3(strip,.02f,l.HalfDepth-l.StairEnd),.001f);
            Box(root,"Upper ceiling finish",plaster,new Vector3(0,l.EavesHeight-.13f,0),new Vector3(l.HalfWidth*2f,.02f,l.HalfDepth*2f),.001f);
        }
        public static void RoofEdges(Transform root, Material wood, Material steel, FarmhouseLayout l)
        {
            float pitch=l.RoofPitch*Mathf.Deg2Rad,span=l.HalfWidth+l.RoofOverhang;
            float edge=l.RidgeHeight-span*Mathf.Tan(pitch),depth=(l.HalfDepth+l.RoofOverhang)*2f;
            foreach(int side in new[]{-1,1})
            {
                Box(root,"Eave fascia "+side,wood,new Vector3(side*(span-.025f),edge-.09f,0),new Vector3(.05f,.18f,depth));
                float soffitWidth=span-l.HalfWidth-l.WallThickness*.5f;
                Box(root,"Eave soffit "+side,wood,new Vector3(side*(span-soffitWidth*.5f),edge-.025f,0),new Vector3(soffitWidth,.05f,depth),.004f);
                // Folded ridge wings sit above the roof's top plane and follow its pitch.
                float wing=.38f;
                GameObject cap=Box(root,"Folded ridge cap "+side,steel,new Vector3(side*wing*.5f,
                    l.RidgeHeight-wing*.5f*Mathf.Tan(pitch)+.2175f/Mathf.Cos(pitch),0),new Vector3(wing/Mathf.Cos(pitch),.025f,depth),.003f);
                cap.transform.localRotation=Quaternion.Euler(0,0,-side*l.RoofPitch);
                float gutterX=side*(span+.065f);
                Box(root,"Gutter base "+side,steel,new Vector3(gutterX,edge-.10f,0),new Vector3(.16f,.018f,depth),.003f);
                foreach(int lip in new[]{-1,1})
                    Box(root,"Gutter lip "+side+" "+lip,steel,new Vector3(gutterX+lip*.071f,edge-.05f,0),new Vector3(.018f,.10f,depth),.003f);
                float pipeTop=edge-.105f,pipeBottom=.15f;
                Box(root,"Rain downpipe "+side,steel,new Vector3(gutterX,(pipeTop+pipeBottom)*.5f,l.HalfDepth-.5f),new Vector3(.08f,pipeTop-pipeBottom,.08f),.012f);
                foreach(float fraction in new[]{.18f,.55f,.88f})
                {
                    float y=Mathf.Lerp(pipeBottom,pipeTop,fraction);
                    Box(root,"Downpipe wall bracket "+side+" "+fraction,steel,
                        new Vector3(side*(l.HalfWidth+l.WallThickness*.5f+span+.065f)*.5f,y,l.HalfDepth-.5f),
                        new Vector3(span+.065f-l.HalfWidth-l.WallThickness*.5f,.035f,.035f),.004f);
                }
                foreach(int end in new[]{-1,1})
                {
                    GameObject rake=Box(root,"Gable rake fascia "+side+" "+end,wood,
                        new Vector3(side*span*.5f,l.RidgeHeight-span*.5f*Mathf.Tan(pitch)-.08f,end*(l.HalfDepth+l.RoofOverhang-.025f)),
                        new Vector3(span/Mathf.Cos(pitch),.16f,.05f),.005f);
                    rake.transform.localRotation=Quaternion.Euler(0,0,-side*l.RoofPitch);
                    Box(root,"Gutter end cap "+side+" "+end,steel,new Vector3(gutterX,edge-.05f,end*(depth*.5f-.009f)),new Vector3(.16f,.10f,.018f),.003f);
                }
            }
        }
        static Vector3 Point(bool alongZ,float axis,float run,float y) => alongZ?new Vector3(axis,y,run):new Vector3(run,y,axis);
        static Vector3 Size(bool alongZ,float width,float height,float depth) => alongZ?new Vector3(depth,height,width):new Vector3(width,height,depth);
        static GameObject Box(Transform root,string name,Material material,Vector3 center,Vector3 size,float bevel=.005f)
        {
            var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=center;
            go.AddComponent<MeshFilter>().sharedMesh=FarmhouseTimberFinish.IsTimber(material)?FarmhouseTimberFinish.Box(size,bevel):FarmhouseMeshLibrary.Box(size,bevel);
            go.AddComponent<MeshRenderer>().sharedMaterial=material;
            GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic);
            return go;
        }
    }
}
