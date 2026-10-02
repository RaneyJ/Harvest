using UnityEditor;
using UnityEngine;

namespace Harvest.Editor
{
    // Editor-only, repeatable packed albedo/height tiles. Generated once, retained as assets.
    // No runtime generation; replacements and material tuning survive scene rebuilds.
    public static class FarmGroundTextures
    {
        const string Folder="Assets/Harvest/Rendering/Surfaces";
        const int Size=2048, Cells=48;
        public static string Path(bool gravel) => Folder+"/Ground "+(gravel?"Gravel":"Soil")+" Detail.asset";
        public static void Apply()
        {
            Texture2D gravel=Tile(true),soil=Tile(false);
            foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/Harvest/Materials"}))
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if(material.shader==null||material.shader.name!="Harvest/Farm Surface"||material.GetFloat("_GroundArtRevision")>=1)continue;
                bool changed=false;
                foreach(string property in new[]{"_SurfaceMap","_GroundMap"})
                {
                    string source=AssetDatabase.GetAssetPath(material.GetTexture(property));bool isGravel=source==Folder+"/Gravel.asset",isSoil=source==Folder+"/Soil.asset";
                    if(!isGravel&&!isSoil)continue;
                    material.SetTexture(property,isGravel?gravel:soil);
                    material.SetFloat(property=="_SurfaceMap"?"_WorldScale":"_GroundScale",.4f); // 2.5 metre physical tile.
                    material.SetFloat(property=="_SurfaceMap"?"_ReliefStrength":"_GroundReliefStrength",isGravel?.018f:.009f);
                    changed=true;
                }
                if(changed){material.SetFloat("_GroundArtRevision",1);EditorUtility.SetDirty(material);}
            }
        }
        static float Hash(int x,int y,int seed)
        {
            unchecked{uint h=(uint)(x*73856093)^(uint)(y*19349663)^(uint)(seed*83492791);h^=h>>13;h*=1274126177u;h^=h>>16;return(h&65535u)/65535f;}
        }
        static int Wrap(int value){value%=Cells;return value<0?value+Cells:value;}
        struct Stone { public float X,Y,Rx,Ry,Cos,Sin,Tone,Seed; }
        static Stone[] Stones()
        {
            var result=new Stone[Cells*Cells];
            for(int y=0;y<Cells;y++)for(int x=0;x<Cells;x++)
            {
                float angle=Hash(x,y,5)*Mathf.PI;
                result[y*Cells+x]=new Stone{X=.18f+Hash(x,y,3)*.64f,Y=.18f+Hash(x,y,4)*.64f,
                    Rx=.23f+Hash(x,y,6)*.31f,Ry=.20f+Hash(x,y,7)*.25f,Cos=Mathf.Cos(angle),Sin=Mathf.Sin(angle),Tone=.65f+Hash(x,y,8)*.38f,Seed=Hash(x,y,2)};
            }
            return result;
        }
        static Texture2D Tile(bool gravel)
        {
            string path=Path(gravel);var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(texture!=null)return texture;
            FarmhouseMeshLibrary.EnsureFolder(Folder);
            var pixels=new Color32[Size*Size];var stones=Stones();
            try
            {
                for(int y=0;y<Size;y++)
                {
                    if(y%32==0)EditorUtility.DisplayProgressBar("Building detailed farm ground",gravel?"Packed gravel albedo / height":"Packed soil albedo / height",y/(float)Size);
                    for(int x=0;x<Size;x++)
                    {
                        float u=x/(float)Size,v=y/(float)Size,gx=u*Cells,gy=v*Cells;
                        int cx=Mathf.FloorToInt(gx),cy=Mathf.FloorToInt(gy);float best=0,tone=.8f;
                        for(int oy=-1;oy<=1;oy++)for(int ox=-1;ox<=1;ox++)
                        {
                            int px=Wrap(cx+ox),py=Wrap(cy+oy);
                            Stone stoneData=stones[py*Cells+px];if(!gravel&&stoneData.Seed<.81f)continue;
                            float dx=gx-(cx+ox+stoneData.X),dy=gy-(cy+oy+stoneData.Y);
                            float a=(dx*stoneData.Cos+dy*stoneData.Sin)/stoneData.Rx,b=(-dx*stoneData.Sin+dy*stoneData.Cos)/stoneData.Ry;
                            float square=a*a+b*b;if(square>1.20f)continue;
                            float dist=Mathf.Sqrt(square)*(1+.075f*Mathf.Sin(Mathf.Atan2(b,a)*5+stoneData.Seed*17));
                            float stone=Mathf.Clamp01((1-dist)*5);
                            if(stone>best){best=stone;tone=stoneData.Tone;}
                        }
                        float broad=Periodic(u,v,4),fine=Periodic(u,v,137),grit=Hash(x,y,91);
                        float earth=.69f+broad*.18f+fine*.055f;
                        float stoneColor=tone*(.80f+fine*.13f)+(best>.9f?.035f:0);
                        float shade=Mathf.Lerp(earth,stoneColor,best)-Mathf.Clamp01(best*8)*Mathf.Clamp01(1-best*2)*.09f;
                        shade+=(grit-.5f)*.032f;
                        float height=.12f+fine*.06f+best*(gravel?.43f:.19f);
                        pixels[y*Size+x]=new Color(shade,shade*(gravel?.98f:.94f),shade*(gravel?.91f:.84f),height);
                    }
                }
                texture=new Texture2D(Size,Size,TextureFormat.RGBA32,true,false){name=gravel?"Ground Gravel Detail":"Ground Soil Detail",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=8};
                texture.SetPixels32(pixels);texture.Apply(true,false);AssetDatabase.CreateAsset(texture,path);return texture;
            }
            finally{EditorUtility.ClearProgressBar();}
        }
        static float Periodic(float u,float v,float frequency)
        {
            float a=Mathf.PerlinNoise(u*frequency+13,v*frequency+37),b=Mathf.PerlinNoise((u-1)*frequency+13,v*frequency+37);
            float c=Mathf.PerlinNoise(u*frequency+13,(v-1)*frequency+37),d=Mathf.PerlinNoise((u-1)*frequency+13,(v-1)*frequency+37);
            return Mathf.Lerp(Mathf.Lerp(a,b,u),Mathf.Lerp(c,d,u),v);
        }
    }
}
