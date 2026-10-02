using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Harvest.Editor
{
    // One deterministic height model for terrain, road meshes, vegetation and dressing.
    // Existing farm buildings and heavy prop pads remain level; contours live between them.
    public static class FarmNaturalGround
    {
        static Vector3[] roadSamples;
        public static Vector2 YardCentre { get; private set; } = new Vector2(-19.8f,-16.8f);
        static readonly Vector2 YardHalf=new Vector2(7.4f,5.2f);
        static Rect housePad = Rect.MinMaxRect(-29, -19, -10, 12);
        static readonly Rect[] Pads = {
            Rect.MinMaxRect(-29,13,-18,25), Rect.MinMaxRect(18,40,29,54),
            Rect.MinMaxRect(18,20,28,34), Rect.MinMaxRect(-18,29,-8,46),
            Rect.MinMaxRect(17,3,21,9), Rect.MinMaxRect(17,-27,21,-21),
            Rect.MinMaxRect(-30,-18,-26,-14) };
        const string Folder = "Assets/Harvest/Art/Generated/FarmGround";
        static float Smooth(float a, float b, float v) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(a, b, v));
        static float Noise(float x, float z, float scale, float seed) => Mathf.Clamp01(Mathf.PerlinNoise(x * scale + seed, z * scale + seed * .71f));

        public static void Build(Material soil, Material road)
        {
            roadSamples=null; ConfigureLayout();
            Terrain(soil); Road(BlendMaterial("Natural freight gravel", road, soil));
            Yard(BlendMaterial("Natural farmyard gravel",road,soil));
            Access(BlendMaterial("Natural farm access gravel", road, soil));
        }
        static void ConfigureLayout()
        {
            var layout = AssetDatabase.LoadAssetAtPath<FarmhouseLayout>(FarmhouseFoundationBuilder.LayoutPath);
            if (layout != null)
            {
                YardCentre=new Vector2(layout.WorldOrigin.x-.8f,layout.WorldOrigin.z-layout.HalfDepth-layout.PorchDepth-5.2f);
                housePad = Rect.MinMaxRect(layout.WorldOrigin.x-layout.HalfWidth-4, layout.WorldOrigin.z-layout.HalfDepth-layout.PorchDepth-7,
                    layout.WorldOrigin.x+layout.HalfWidth+3, layout.WorldOrigin.z+layout.HalfDepth+7);
            }
        }

        [MenuItem("Harvest/Validate Natural Ground")]
        public static void Validate()
        {
            try
            {
                ConfigureLayout();
                Shader shader=Shader.Find("Harvest/Farm Surface");
                Require(shader!=null&&shader.isSupported&&!ShaderUtil.ShaderHasError(shader),"Ground shader is missing or failed to compile.");
                Physics.SyncTransforms();
                foreach(string name in new[]{"Farm terrain","Freight road","Farm access track","Farmyard parking apron"})
                {
                    GameObject go=GameObject.Find(name);Require(go!=null,"Rebuild The Line: missing "+name);
                    MeshFilter filter=go.GetComponent<MeshFilter>();MeshCollider collider=go.GetComponent<MeshCollider>();
                    Require(filter!=null&&filter.sharedMesh!=null&&collider!=null&&collider.sharedMesh==filter.sharedMesh,"Visual/collision mesh mismatch: "+name);
                    Mesh mesh=filter.sharedMesh;Require(AssetDatabase.Contains(mesh)&&mesh.colors.Length==mesh.vertexCount&&mesh.uv2.Length==mesh.vertexCount,"Missing persistent ground mesh/colors/lightmap UVs: "+name);
                    Material material=go.GetComponent<MeshRenderer>().sharedMaterial;
                    Require(material!=null&&material.shader==shader&&material.GetFloat("_GroundBlendEnabled")>.5f&&material.GetTexture("_GroundMap")!=null,"Ground blend material is incomplete: "+name);
                    Vector3[] v=mesh.vertices;int[] t=mesh.triangles;
                    for(int i=0;i<t.Length;i+=3) Require(Vector3.Cross(v[t[i+1]]-v[t[i]],v[t[i+2]]-v[t[i]]).y>0,"Inverted or degenerate ground triangle: "+name);
                }
                Require(GameObject.Find("Road gravel shoulder")==null&&GameObject.Find("Faded road center marking")==null,"Old road slabs/markings remain. Rebuild the scene.");
                MeshCollider terrain=GameObject.Find("Farm terrain").GetComponent<MeshCollider>();
                for(int x=-60;x<=60;x+=12)for(int z=-60;z<=80;z+=10)
                    CheckSample(terrain,x+.31f,z+.27f,TerrainHeight(x+.31f,z+.27f));
                MeshCollider road=GameObject.Find("Freight road").GetComponent<MeshCollider>();
                for(int z=-45;z<=65;z+=10)foreach(float x in new[]{-5f,0,5f})CheckSample(road,x,z,.04f);
                MeshCollider access=GameObject.Find("Farm access track").GetComponent<MeshCollider>();
                foreach(float x in new[]{-16f,-12f,-9f,-7.05f})CheckSample(access,x,-12,SurfaceHeight(x,-12));
                MeshCollider yard=GameObject.Find("Farmyard parking apron").GetComponent<MeshCollider>();
                foreach(Vector2 offset in new[]{Vector2.zero,new Vector2(-5.6f,2.8f),new Vector2(-2.6f,2.8f),new Vector2(5,0),new Vector2(0,-4.5f)})
                    CheckSample(yard,YardCentre.x+offset.x,YardCentre.y+offset.y,YardHeight(YardCentre.x+offset.x,YardCentre.y+offset.y));
                foreach(Rect pad in Pads)Require(Mathf.Abs(TerrainHeight(pad.center.x,pad.center.y)+.05f)<.001f,"Heavy prop pad has moved.");
                Debug.Log("Natural ground checks passed. Inspect shoulder blending, crop contact and the access junction in Game view, then run the farmhouse Play mode/navigation checks.");
            }
            catch(Exception exception){Debug.LogError("Natural ground check failed: "+exception.Message);}
        }
        static void CheckSample(MeshCollider collider,float x,float z,float expected)
        {
            Require(collider.Raycast(new Ray(new Vector3(x,10,z),Vector3.down),out RaycastHit hit,25),"Ground collision missing at "+x+", "+z);
            Require(Mathf.Abs(hit.point.y-expected)<.003f,"Sampled ground differs from collision at "+x+", "+z);
        }
        static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}

        static float PadWeight(Rect pad, float x, float z)
        {
            float dx = Mathf.Max(pad.xMin-x, Mathf.Max(0, x-pad.xMax));
            float dz = Mathf.Max(pad.yMin-z, Mathf.Max(0, z-pad.yMax));
            return Smooth(0, 3, Mathf.Sqrt(dx*dx + dz*dz));
        }
        static float RawHeight(float x, float z)
        {
            float level = PadWeight(housePad, x, z);
            level=Mathf.Min(level,PadWeight(Rect.MinMaxRect(YardCentre.x-YardHalf.x-1,YardCentre.y-YardHalf.y-1,YardCentre.x+YardHalf.x+1,YardCentre.y+YardHalf.y+1),x,z));
            foreach (Rect pad in Pads) level = Mathf.Min(level, PadWeight(pad, x, z));
            // Fence footings and freight road stay at their established grade.
            level *= Smooth(.9f, 2.4f, Mathf.Abs(Mathf.Abs(x)-30));
            level *= Smooth(8.8f, 12, Mathf.Abs(x));
            float broad = (Noise(x,z,.034f,31)-.5f)*.30f;
            float detail = (Noise(x,z,.11f,72)-.5f)*.055f;
            float ditchCentre = 12.0f + (Noise(0,z,.043f,19)-.5f)*.75f;
            float ditch = Mathf.Exp(-Mathf.Pow((Mathf.Abs(x)-ditchCentre)/1.0f,2)) * .16f;
            float hills = Smooth(32,61,Mathf.Abs(x)) * (.7f + Noise(x,z,.022f,93)*3.8f);
            return -.05f + (broad+detail-ditch)*level + hills;
        }
        // Sample the actual one-metre triangulation, rather than the unsampled noise field.
        public static float TerrainHeight(float x, float z)
        {
            x = Mathf.Clamp(x,-65,65); z = Mathf.Clamp(z,-65,85);
            float x0 = Mathf.Min(64,Mathf.Floor(x)), z0 = Mathf.Min(84,Mathf.Floor(z));
            float u=x-x0,v=z-z0;
            float a=RawHeight(x0,z0),b=RawHeight(x0+1,z0),c=RawHeight(x0,z0+1),d=RawHeight(x0+1,z0+1);
            return u+v<=1 ? a+(b-a)*u+(c-a)*v : d+(c-d)*(1-u)+(b-d)*(1-v);
        }
        static void Terrain(Material soil)
        {
            const int columns=131, rows=151;
            var v=new List<Vector3>(columns*rows);var colors=new List<Color>(columns*rows);var t=new List<int>();
            for(int row=0;row<rows;row++) for(int column=0;column<columns;column++)
            {
                float x=-65+column,z=-65+row;
                v.Add(new Vector3(x,RawHeight(x,z),z));colors.Add(new Color(0,.94f+Noise(x,z,.052f,23)*.12f,0,1));
            }
            for(int row=0;row<rows-1;row++) for(int column=0;column<columns-1;column++) Cell(t,row*columns+column,columns);
            Material material=BlendMaterial("Natural field soil",soil,soil);
            Save("Farm terrain",v,t,colors,material,true);
        }
        const int Sections=13;
        static Vector3 RoadPoint(int section, float z)
        {
            float x;
            int side=section<6?-1:1;
            int band=Mathf.Abs(section-6);
            float n=(Noise(side*17,z,.049f,45)-.5f);
            switch(band)
            {
                case 0: x=0;break;
                case 1: x=2.65f;break;
                case 2: x=5.5f;break;
                case 3: x=7.3f+n*.20f;break;
                case 4: x=8.2f+n*.45f;break;
                case 5: x=9.4f+n*.75f;break;
                default: x=10.9f+n*.90f;break;
            }
            x*=side;
            float blend=Smooth(7.2f,10.6f,Mathf.Abs(x));
            // Broad travel lanes stay flat; shoulders taper continuously into actual ground.
            float y=Mathf.Lerp(.04f,TerrainHeight(x,z),blend);
            if(band==6) y=TerrainHeight(x,z);
            y=Mathf.Lerp(TerrainHeight(x,z),y,Smooth(-55,-50,z)*(1-Smooth(70,75,z)));
            return new Vector3(x,y,z);
        }
        static void Road(Material material)
        {
            var v=new List<Vector3>();var colors=new List<Color>();var t=new List<int>();
            for(int row=0;row<=130;row++) for(int section=0;section<Sections;section++)
            {
                Vector3 p=RoadPoint(section,-55+row);v.Add(p);
                float blend=section==0||section==12?1:Smooth(7.2f,10.6f,Mathf.Abs(p.x));
                blend=1-(1-blend)*Smooth(-55,-50,p.z)*(1-Smooth(70,75,p.z));
                colors.Add(new Color(blend,Mathf.Lerp(SurfaceVariation(p.x,p.z),TerrainVariation(p.x,p.z),blend),0,1));
            }
            for(int row=0;row<130;row++) for(int section=0;section<Sections-1;section++) Cell(t,row*Sections+section,Sections);
            roadSamples=v.ToArray();
            Save("Freight road",v,t,colors,material,true);
        }
        static float TerrainVariation(float x,float z)
        {
            float x0=Mathf.Floor(x),z0=Mathf.Floor(z),u=x-x0,v=z-z0;
            float a=.94f+Noise(x0,z0,.052f,23)*.12f,b=.94f+Noise(x0+1,z0,.052f,23)*.12f;
            float c=.94f+Noise(x0,z0+1,.052f,23)*.12f,d=.94f+Noise(x0+1,z0+1,.052f,23)*.12f;
            return u+v<=1?a+(b-a)*u+(c-a)*v:d+(c-d)*(1-u)+(b-d)*(1-v);
        }
        public static float SurfaceVariation(float x,float z) => .97f+Noise(x,z,.08f,56)*.055f;
        static float RoadHeight(float x,float z)
        {
            if(z<-55||z>75||Mathf.Abs(x)>11.5f) return TerrainHeight(x,z);
            if(Mathf.Abs(x)<=5.5f&&z>=-50&&z<=70)return .04f;
            float z0=Mathf.Min(74,Mathf.Floor(z));
            for(int section=0;section<Sections-1;section++)
            {
                int index=((int)z0+55)*Sections+section;
                Vector3 a=roadSamples==null?RoadPoint(section,z0):roadSamples[index];
                Vector3 b=roadSamples==null?RoadPoint(section+1,z0):roadSamples[index+1];
                Vector3 c=roadSamples==null?RoadPoint(section,z0+1):roadSamples[index+Sections];
                Vector3 d=roadSamples==null?RoadPoint(section+1,z0+1):roadSamples[index+Sections+1];
                if(TriangleHeight(x,z,a,c,b,out float h)||TriangleHeight(x,z,b,c,d,out h)) return h;
            }
            return TerrainHeight(x,z);
        }
        static bool TriangleHeight(float x,float z,Vector3 a,Vector3 b,Vector3 c,out float height)
        {
            float denominator=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);
            float u=((b.z-c.z)*(x-c.x)+(c.x-b.x)*(z-c.z))/denominator;
            float v=((c.z-a.z)*(x-c.x)+(a.x-c.x)*(z-c.z))/denominator;
            float w=1-u-v;height=a.y*u+b.y*v+c.y*w;
            return u>=-.00001f&&v>=-.00001f&&w>=-.00001f;
        }
        static float YardDistance(float x,float z)
        {
            Vector2 q=new Vector2(Mathf.Abs(x-YardCentre.x)-YardHalf.x+1.5f,Mathf.Abs(z-YardCentre.y)-YardHalf.y+1.5f);
            return new Vector2(Mathf.Max(q.x,0),Mathf.Max(q.y,0)).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-1.5f;
        }
        public static bool IsYard(float x,float z) => YardDistance(x,z)<1.25f;
        static float YardCoverage(float x,float z) => 1-Smooth(-.10f,.95f,YardDistance(x,z)+(Noise(x,z,.21f,67)-.5f)*.12f);
        static float YardPointHeight(float x,float z) => Mathf.Lerp(RoadHeight(x,z),.025f,YardCoverage(x,z));
        static float YardHeight(float x,float z)
        {
            if(!IsYard(x,z))return RoadHeight(x,z);
            float x0=YardCentre.x+Mathf.Floor((x-YardCentre.x)*2)*.5f,z0=YardCentre.y+Mathf.Floor((z-YardCentre.y)*2)*.5f;
            float u=(x-x0)*2,v=(z-z0)*2;
            float a=YardPointHeight(x0,z0),b=YardPointHeight(x0+.5f,z0),c=YardPointHeight(x0,z0+.5f),d=YardPointHeight(x0+.5f,z0+.5f);
            return u+v<=1?a+(b-a)*u+(c-a)*v:d+(c-d)*(1-u)+(b-d)*(1-v);
        }
        static void Yard(Material material)
        {
            const int columns=37,rows=27;
            var v=new List<Vector3>();var colors=new List<Color>();var t=new List<int>();
            for(int row=0;row<rows;row++)for(int column=0;column<columns;column++)
            {
                float x=YardCentre.x-9+column*.5f,z=YardCentre.y-6.5f+row*.5f,blend=1-YardCoverage(x,z);
                v.Add(new Vector3(x,YardPointHeight(x,z),z));
                colors.Add(new Color(blend,Mathf.Lerp(SurfaceVariation(x,z),TerrainVariation(x,z),blend),0,1));
            }
            for(int row=0;row<rows-1;row++)for(int column=0;column<columns-1;column++)
            {
                float x=YardCentre.x-9+(column+.5f)*.5f,z=YardCentre.y-6.5f+(row+.5f)*.5f;
                if(YardDistance(x,z)>1.05f)continue;
                Cell(t,row*columns+column,columns);
            }
            Save("Farmyard parking apron",v,t,colors,material,true);
        }
        static float AccessDistance(float x,float z)
        {
            Vector2 q=new Vector2(Mathf.Abs(x+12)-4.4f,Mathf.Abs(z+12)-1.4f);
            return new Vector2(Mathf.Max(q.x,0),Mathf.Max(q.y,0)).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-.6f;
        }
        static float AccessPointHeight(float x,float z)
        {
            float blend=Smooth(-.15f,.8f,AccessDistance(x,z)+(Noise(x,z,.27f,82)-.5f)*.10f);
            float top=Mathf.Lerp(.06f,RoadHeight(x,z),Smooth(-9,-7,x));
            return Mathf.Lerp(top,Mathf.Max(RoadHeight(x,z),YardHeight(x,z)),blend);
        }
        static void Access(Material material)
        {
            const int columns=24, rows=15;
            var v=new List<Vector3>();var colors=new List<Color>();var t=new List<int>();
            for(int row=0;row<rows;row++) for(int column=0;column<columns;column++)
            {
                float x=-18.5f+column*.5f,z=-15.5f+row*.5f;
                v.Add(new Vector3(x,AccessPointHeight(x,z),z));
                float blend=Smooth(-.15f,.8f,AccessDistance(x,z)+(Noise(x,z,.27f,82)-.5f)*.10f);
                // Match the road gravel where the junction joins the main carriageway.
                blend*=1-Smooth(-9.2f,-7,x);
                blend*=1-YardCoverage(x,z);
                colors.Add(new Color(blend,Mathf.Lerp(SurfaceVariation(x,z),TerrainVariation(x,z),blend),0,1));
            }
            for(int row=0;row<rows-1;row++) for(int column=0;column<columns-1;column++)
            {
                float x=-18.5f+column*.5f,z=-15.5f+row*.5f;
                if(AccessDistance(x+.25f,z+.25f)>.8f && x<-9.2f) continue;
                // The grid terminates at x=-7, inside the main carriageway.
                Cell(t,row*columns+column,columns);
            }
            Save("Farm access track",v,t,colors,material,true);
        }
        public static float SurfaceHeight(float x,float z)
        {
            float ground=Mathf.Max(RoadHeight(x,z),YardHeight(x,z));
            if(x>=-18.5f&&x<=-7&&z>=-15.5f&&z<=-8.5f&&AccessDistance(x,z)<=1.1f)
            {
                float x0=Mathf.Floor((x+18.5f)*2)*.5f-18.5f,z0=Mathf.Floor((z+15.5f)*2)*.5f-15.5f;
                float u=(x-x0)*2,v=(z-z0)*2;
                float a=AccessPointHeight(x0,z0),b=AccessPointHeight(x0+.5f,z0),c=AccessPointHeight(x0,z0+.5f),d=AccessPointHeight(x0+.5f,z0+.5f);
                ground=Mathf.Max(ground,u+v<=1?a+(b-a)*u+(c-a)*v:d+(c-d)*(1-u)+(b-d)*(1-v));
            }
            return ground;
        }
        static Material BlendMaterial(string name,Material source,Material soil)
        {
            string path="Assets/Harvest/Materials/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material!=null) return material;
            if(source.shader.name!="Harvest/Farm Surface" || soil.shader.name!="Harvest/Farm Surface")
                throw new InvalidOperationException("Natural ground requires the Harvest/Farm Surface starter road and soil. Keep authored replacements separate from generated ground materials.");
            material=new Material(source){name=name};
            material.SetTexture("_GroundMap",soil.GetTexture("_SurfaceMap"));material.SetColor("_GroundTint",soil.GetColor("_BaseColor"));
            material.SetFloat("_GroundScale",soil.GetFloat("_WorldScale"));material.SetFloat("_GroundSmoothness",soil.GetFloat("_Smoothness"));
            material.SetFloat("_GroundReliefStrength",soil.GetFloat("_ReliefStrength"));
            material.SetFloat("_GroundBlendEnabled",1);material.SetFloat("_VertexTintStrength",1);
            AssetDatabase.CreateAsset(material,path);return material;
        }
        static void Cell(List<int> t,int a,int columns)
        {t.Add(a);t.Add(a+columns);t.Add(a+1);t.Add(a+1);t.Add(a+columns);t.Add(a+columns+1);}
        static void Save(string name,List<Vector3> v,List<int> t,List<Color> colors,Material material,bool collision)
        {
            FarmhouseMeshLibrary.EnsureFolder(Folder);string path=Folder+"/Natural_"+name.Replace(" ","_")+".asset";
            Mesh mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);bool create=mesh==null;if(create)mesh=new Mesh();
            mesh.Clear();mesh.name=name;mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.SetColors(colors);
            var uv=new List<Vector2>();var uv2=new List<Vector2>();foreach(Vector3 p in v){uv.Add(new Vector2(p.x,p.z));uv2.Add(new Vector2((p.x+65)/130,(p.z+65)/150));}
            mesh.SetUVs(0,uv);mesh.SetUVs(1,uv2);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            if(create)AssetDatabase.CreateAsset(mesh,path);EditorUtility.SetDirty(mesh);
            var go=new GameObject(name);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;
            go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic);
            if(collision)go.AddComponent<MeshCollider>().sharedMesh=mesh;
        }
    }
}
