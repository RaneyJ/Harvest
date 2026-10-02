using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Harvest.Editor
{
    // Presentation geometry stays beyond the encounter boundary and has no navigation or collision.
    public static class DistantFarmLandscape
    {
        public static void Build(Transform root,Material soil,Material concrete)
        {
            Material roof=AssetDatabase.LoadAssetAtPath<Material>(FarmhouseMaterialLibrary.MaterialPath("Roofing"))??concrete;
            const int nx=43,nz=12;
            var vertices=new Vector3[nx*nz];var uv=new Vector2[vertices.Length];var triangles=new int[(nx-1)*(nz-1)*6];
            for(int z=0;z<nz;z++) for(int x=0;x<nx;x++)
            {
                float px=-210+x*10,pz=145+z*5;
                float fade=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,25,Mathf.Min(pz-145,200-pz)))*
                    Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,25,210-Mathf.Abs(px)));
                float height=fade*(2+14*Mathf.PerlinNoise(px*.009f+13,pz*.012f+37));
                vertices[z*nx+x]=new Vector3(px,-.19f+height,pz);uv[z*nx+x]=new Vector2(px*.08f,pz*.08f);
            }
            int index=0;
            for(int z=0;z<nz-1;z++) for(int x=0;x<nx-1;x++)
            {
                int a=z*nx+x;triangles[index++]=a;triangles[index++]=a+nx;triangles[index++]=a+1;
                triangles[index++]=a+1;triangles[index++]=a+nx;triangles[index++]=a+nx+1;
            }
            const string folder="Assets/Harvest/Art/Generated/FarmMachinery";FarmhouseMeshLibrary.EnsureFolder(folder);
            string path=folder+"/Distant_landscape.asset";Mesh mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);bool create=mesh==null;
            if(create) mesh=new Mesh();mesh.Clear();mesh.name="Distant landscape";
            mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.RecalculateTangents();
            if(create) AssetDatabase.CreateAsset(mesh,path);EditorUtility.SetDirty(mesh);
            Visual(root,"Rolling distant farm ground",mesh,soil,Vector3.zero);
            for(int i=0;i<7;i++)
            {
                Vector3 origin=new Vector3(-52+i*15,-.2f,115+(i%3)*8);float width=8+i%3*2,depth=12,height=5+(i%3)*.5f;
                Box(root,"Distant colony barn "+i,concrete,origin+Vector3.up*(height*.5f),new Vector3(width,height,depth));
                Visual(root,"Distant pitched barn roof "+i,FarmhouseMeshLibrary.Gable(width+.5f,1.5f+(i%2)*.4f,depth+.5f),roof,origin+Vector3.up*height);
                if(i%2==0)
                    Box(root,"Distant service annex "+i,concrete,origin+new Vector3(-width*.5f-1.5f,1.4f,1.8f),new Vector3(3,2.8f,5));
                if(i==1 || i==5)
                    Visual(root,"Distant silo "+i,FarmMachineryMeshes.Revolve("Distant_silo",new Vector2(0,0),new Vector2(1.5f,0),
                        new Vector2(1.5f,8.5f),new Vector2(0,9.3f)),roof,origin+new Vector3(width*.5f+2.5f,0,2));
            }
        }
        static void Box(Transform root,string name,Material material,Vector3 center,Vector3 size) =>
            Visual(root,name,FarmhouseMeshLibrary.Box(size,.025f),material,center);
        static void Visual(Transform root,string name,Mesh mesh,Material material,Vector3 position)
        {
            var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=position;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
            // Distant scenery uses ambient/probe lighting; don't spend the farmhouse lightmap atlas on it.
            GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic);
        }
    }
}
