using UnityEditor;
using UnityEngine;

namespace Harvest.Editor
{
    // Grain follows the dimensioned timber, without altering topology, lightmap UVs or collision.
    public static class FarmhouseTimberFinish
    {
        public static bool IsTimber(Material material) => material!=null &&
            (AssetDatabase.GetAssetPath(material)==FarmhouseMaterialLibrary.MaterialPath("Timber") ||
             AssetDatabase.GetAssetPath(material).StartsWith("Assets/Harvest/Materials/Farmhouse Timber Finish ",System.StringComparison.Ordinal));
        public static Material Material(string kind,Material source,float normal)
        {
            string path="Assets/Harvest/Materials/Farmhouse Timber Finish "+kind+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null)
            {
                material=new Material(source){name="Farmhouse Timber Finish "+kind};
                material.SetFloat("_BumpScale",normal);
                Vector2 scale=source.GetTextureScale("_BaseMap");material.SetTextureScale("_BaseMap",new Vector2(scale.x,scale.y*.3f));
                AssetDatabase.CreateAsset(material,path);
            }
            else if(material.GetTexture("_BaseMap")==null && material.GetTexture("_BumpMap")==null && material.GetTexture("_MetallicGlossMap")==null && material.GetTexture("_OcclusionMap")==null && source.GetTexture("_BaseMap")!=null)
            {
                foreach(string property in new[]{"_BaseMap","_BumpMap","_MetallicGlossMap","_OcclusionMap"}) material.SetTexture(property,source.GetTexture(property));
                foreach(string keyword in new[]{"_NORMALMAP","_METALLICSPECGLOSSMAP","_OCCLUSIONMAP"}) if(source.IsKeywordEnabled(keyword)) material.EnableKeyword(keyword);
                if(material.GetTextureScale("_BaseMap")==new Vector2(1,.3f))
                {Vector2 scale=source.GetTextureScale("_BaseMap");material.SetTextureScale("_BaseMap",new Vector2(scale.x,scale.y*.3f));}
                Color old=material.GetColor("_BaseColor");
                if((new Vector3(old.r,old.g,old.b)-new Vector3(.30f,.24f,.16f)).sqrMagnitude<1e-6f) material.SetColor("_BaseColor",source.GetColor("_BaseColor"));
                if(Mathf.Approximately(material.GetFloat("_Smoothness"),.18f)) material.SetFloat("_Smoothness",source.GetFloat("_Smoothness"));
                EditorUtility.SetDirty(material);
            }
            return material;
        }
        public static Mesh Box(Vector3 size,float bevel)
        {
            Mesh source=FarmhouseMeshLibrary.Box(size,bevel);string path="Assets/Harvest/Art/Generated/Farmhouse/Timber_"+source.name+".asset";
            Mesh mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh!=null) return mesh;
            mesh=Object.Instantiate(source);mesh.name="Timber "+source.name;
            int major=size.x>=size.y && size.x>=size.z?0:(size.y>=size.z?1:2);
            Vector3 grain=Vector3.zero;grain[major]=1;
            Vector3[] vertices=mesh.vertices,normals=mesh.normals;var uv=new Vector2[vertices.Length];
            for(int i=0;i<vertices.Length;i++)
            {
                Vector3 normal=normals[i],along=grain-normal*Vector3.Dot(grain,normal);
                if(along.sqrMagnitude<.01f)
                {
                    int secondary=(major+1)%3,other=(major+2)%3;if(size[other]>size[secondary]) secondary=other;
                    Vector3 seed=Vector3.zero;seed[secondary]=1;along=seed-normal*Vector3.Dot(seed,normal);
                }
                along.Normalize();Vector3 across=Vector3.Cross(along,normal).normalized;
                uv[i]=new Vector2(Vector3.Dot(vertices[i],across),Vector3.Dot(vertices[i],along));
            }
            mesh.uv=uv;mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
    }
}
