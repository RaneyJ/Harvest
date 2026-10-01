using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Harvest.Editor
{
    // Visual-only crop geometry. Chunking keeps field culling useful and preserves encounter lanes.
    public static class FarmGrainGeometry
    {
        const string Folder="Assets/Harvest/Art/Generated/FarmGrain";
        sealed class Plant
        {
            public readonly List<Vector3> Vertices=new List<Vector3>();
            public readonly List<Vector3> Normals=new List<Vector3>();
            public readonly List<Vector2> UV=new List<Vector2>();
            public readonly List<int>[] Triangles={new List<int>(),new List<int>()};
            public void Face(Vector3 a,Vector3 b,Vector3 c,int surface,Vector3? outward=null)
            {
                Vector3 normal=Vector3.Cross(b-a,c-a).normalized;
                if(outward.HasValue && Vector3.Dot(normal,outward.Value)<0) {Vector3 swap=b;b=c;c=swap;normal=-normal;}
                Vector3 along=(b-a).normalized,across=Vector3.Cross(normal,along);
                foreach(Vector3 point in new[]{a,b,c})
                {
                    Triangles[surface].Add(Vertices.Count);Vertices.Add(point);Normals.Add(normal);
                    UV.Add(new Vector2(Vector3.Dot(point,along),Vector3.Dot(point,across)));
                }
            }
            public void Blade(Vector3 root,Vector3 middle,Vector3 tip,float width)
            {
                Vector3 across=Vector3.Cross(tip-root,Vector3.forward).normalized*width;
                Vector3 left=middle-across,right=middle+across;
                Face(root,left,tip,0);Face(root,tip,right,0);Face(root,tip,left,0);Face(root,right,tip,0);
            }
            public void Kernel(Vector3 center)
            {
                Vector3 top=center+Vector3.up*.045f,bottom=center-Vector3.up*.045f;
                Vector3[] ring={center+Vector3.right*.018f,center+Vector3.forward*.014f,center-Vector3.right*.018f,center-Vector3.forward*.014f};
                for(int i=0;i<4;i++)
                {
                    Vector3 a=ring[i],b=ring[(i+1)%4];
                    Face(top,a,b,1,(top+a+b)/3-center);Face(bottom,b,a,1,(bottom+b+a)/3-center);
                }
            }
        }
        public static void Build()
        {
            FarmhouseMeshLibrary.EnsureFolder(Folder);
            Mesh plant=Template();
            Material stem=Surface("Harvest Grain Stem",new Color(.43f,.39f,.22f));
            Material head=Surface("Harvest Grain Head",new Color(.62f,.52f,.31f));
            var root=new GameObject("Grain fields — visual concealment");
            foreach(int side in new[]{-1,1}) for(int first=0;first<26;first+=7)
            {
                var parts=new[]{new List<CombineInstance>(),new List<CombineInstance>()};
                for(int row=first;row<Mathf.Min(first+7,26);row++) for(int column=0;column<9;column++)
                {
                    float x=side*(10.5f+column*2),z=-44+row*4.5f;
                    if(side<0 && z>-16 && z<10) continue;
                    if(side<0 && x>-17 && z>30 && z<46) continue;
                    if(side<0 && x<-18 && z>14 && z<24) continue;
                    if(side>0 && x>20 && z>43 && z<53) continue;
                    if(side>0 && x>20 && x<26 && z>20 && z<34) continue;
                    for(int stalk=0;stalk<4;stalk++)
                    {
                        int seed=(row*9+column)*4+stalk+(side+1)*701;
                        float height=.94f+Hash(seed+1)*.13f;
                        Vector3 position=new Vector3(x+stalk%2*.35f+(Hash(seed+2)-.5f)*.12f,-.05f,z+stalk/2*.5f+(Hash(seed+3)-.5f)*.12f);
                        position.y=FarmNaturalGround.SurfaceHeight(position.x,position.z);
                        Matrix4x4 transform=Matrix4x4.TRS(position,Quaternion.Euler((Hash(seed+4)-.5f)*6,Hash(seed+5)*360,side*3),new Vector3(1,height,1));
                        for(int surface=0;surface<2;surface++) parts[surface].Add(new CombineInstance{mesh=plant,subMeshIndex=surface,transform=transform});
                    }
                }
                if(parts[0].Count==0) continue;
                var interim=new Mesh[2];
                try
                {
                    for(int surface=0;surface<2;surface++)
                    {interim[surface]=new Mesh{indexFormat=IndexFormat.UInt32};interim[surface].CombineMeshes(parts[surface].ToArray(),true,true);}
                    string name="Grain "+side+" rows "+first,path=Folder+"/Field_"+side+"_"+first+".asset";
                    Mesh mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);bool create=mesh==null;if(create) mesh=new Mesh();
                    mesh.Clear();mesh.name=name;mesh.indexFormat=IndexFormat.UInt32;
                    mesh.CombineMeshes(new[]{new CombineInstance{mesh=interim[0],transform=Matrix4x4.identity},new CombineInstance{mesh=interim[1],transform=Matrix4x4.identity}},false,true);
                    mesh.RecalculateBounds();if(create) AssetDatabase.CreateAsset(mesh,path);EditorUtility.SetDirty(mesh);
                    var go=new GameObject(name);go.transform.SetParent(root.transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
                    go.AddComponent<MeshRenderer>().sharedMaterials=new[]{stem,head};GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic);
                }
                finally {foreach(Mesh mesh in interim) if(mesh!=null) Object.DestroyImmediate(mesh);}
            }
        }
        static float Hash(int seed)
        {
            unchecked {uint value=(uint)seed;value=(value^61u)^(value>>16);value*=9u;value^=value>>4;value*=0x27d4eb2du;value^=value>>15;return (value&0xffffu)/65535f;}
        }
        static Mesh Template()
        {
            var p=new Plant();
            for(int i=0;i<4;i++)
            {
                float a=i*Mathf.PI*.5f,b=(i+1)*Mathf.PI*.5f;
                Vector3 lowA=new Vector3(Mathf.Cos(a)*.005f,0,Mathf.Sin(a)*.005f),lowB=new Vector3(Mathf.Cos(b)*.005f,0,Mathf.Sin(b)*.005f);
                Vector3 highA=new Vector3(Mathf.Cos(a)*.003f,1.27f,Mathf.Sin(a)*.003f),highB=new Vector3(Mathf.Cos(b)*.003f,1.27f,Mathf.Sin(b)*.003f);
                Vector3 outward=new Vector3(Mathf.Cos((a+b)*.5f),0,Mathf.Sin((a+b)*.5f));
                p.Face(lowA,lowB,highB,0,outward);p.Face(lowA,highB,highA,0,outward);
            }
            p.Blade(new Vector3(0,.35f,0),new Vector3(.10f,.49f,.01f),new Vector3(.19f,.58f,.015f),.016f);
            p.Blade(new Vector3(0,.65f,0),new Vector3(-.08f,.78f,-.01f),new Vector3(-.16f,.87f,-.015f),.012f);
            for(int i=0;i<4;i++) foreach(int side in new[]{-1,1}) p.Kernel(new Vector3(side*.018f,1.05f+i*.052f,(i%2==0?1:-1)*.004f));
            foreach(int side in new[]{-1,1}) for(int i=0;i<2;i++)
            {
                Vector3 a=new Vector3(side*.014f,1.18f+i*.04f,0),b=a+Vector3.forward*.002f,c=new Vector3(side*(.032f+i*.01f),1.38f+i*.025f,.002f);
                p.Face(a,b,c,1);p.Face(a,c,b,1);
            }
            string path=Folder+"/GrainPlant.asset";Mesh mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);bool create=mesh==null;if(create) mesh=new Mesh();
            mesh.Clear();mesh.name="Harvest grain plant";mesh.SetVertices(p.Vertices);mesh.SetNormals(p.Normals);mesh.SetUVs(0,p.UV);mesh.subMeshCount=2;
            for(int surface=0;surface<2;surface++) mesh.SetTriangles(p.Triangles[surface],surface);
            mesh.RecalculateBounds();mesh.RecalculateTangents();if(create) AssetDatabase.CreateAsset(mesh,path);EditorUtility.SetDirty(mesh);return mesh;
        }
        static Material Surface(string name,Color color)
        {
            string path="Assets/Harvest/Materials/"+name+".mat";Material material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material!=null) return material;
            material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",.08f);
            AssetDatabase.CreateAsset(material,path);return material;
        }
    }
}
