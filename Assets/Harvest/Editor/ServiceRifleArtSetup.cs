using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Harvest.Editor
{
    // Explicit art import step. Builds a reusable visual prefab without changing weapon data.
    public static class ServiceRifleArtSetup
    {
        const string Folder = "Assets/Harvest/Art/Weapons/MilitiaServiceRifle";
        public const string PrefabPath = Folder + "/Militia Service Rifle.prefab";
        public const string ViewPrefabPath = Folder + "/Militia Service Rifle View.prefab";

        [MenuItem("Harvest/Art/Build Militia Service Rifle Prefab")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            foreach (string name in new[] { "MilitiaServiceRifle", "MilitiaServiceRifle_LOD1" })
            {
                var importer = AssetImporter.GetAtPath(Folder + "/" + name + ".fbx") as ModelImporter;
                if (importer == null) throw new InvalidOperationException("Pull the service rifle FBX assets before building its prefab.");
                importer.globalScale = 1f;
                importer.useFileScale = true;
                importer.importAnimation = false;
                importer.addCollider = false;
                importer.importNormals = ModelImporterNormals.Import;
                importer.importTangents = ModelImporterTangents.CalculateMikk;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.SaveAndReimport();
            }
            Texture2D color = Texture("ServiceRifle_BaseColor", true, false);
            Texture2D normal = Texture("ServiceRifle_Normal", false, true);
            Texture2D mask = Texture("ServiceRifle_UnityMask", false, false);
            const string materialPath = Folder + "/Militia Service Rifle.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("URP Lit shader is required for service rifle materials.");
                material = new Material(shader) { name = "Militia Service Rifle" };
                material.SetTexture("_BaseMap", color);
                material.SetColor("_BaseColor", Color.white);
                material.SetTexture("_BumpMap", normal);
                material.SetFloat("_BumpScale", 1);
                material.SetTexture("_MetallicGlossMap", mask);
                material.SetFloat("_Metallic", 1);
                material.SetFloat("_Smoothness", 1);
                material.SetFloat("_SmoothnessTextureChannel", 0);
                material.SetTexture("_OcclusionMap", mask);
                material.EnableKeyword("_NORMALMAP");
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
                material.EnableKeyword("_OCCLUSIONMAP");
                AssetDatabase.CreateAsset(material, materialPath);
            }
            // A dedicated first-person prefab always uses the detailed mesh, without distance LOD switches.
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ViewPrefabPath) == null)
            {
                var viewRoot = new GameObject("Militia Service Rifle View");
                try
                {
                    Instance("MilitiaServiceRifle", viewRoot.transform, material);
                    PrefabUtility.SaveAsPrefabAsset(viewRoot, ViewPrefabPath);
                }
                finally { Object.DestroyImmediate(viewRoot); }
                AssetDatabase.SaveAssets();
            }
            // Existing prefab/material edits are retained; model references refresh on FBX import.
            GameObject saved = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (saved != null) { Selection.activeObject = saved; return; }
            var root = new GameObject("Militia Service Rifle");
            try
            {
                GameObject hero = Instance("MilitiaServiceRifle", root.transform, material);
                GameObject world = Instance("MilitiaServiceRifle_LOD1", root.transform, material);
                Renderer[] high = hero.GetComponentsInChildren<Renderer>();
                Renderer[] low = world.GetComponentsInChildren<Renderer>();
                if (high.Length == 0 || low.Length == 0) throw new InvalidOperationException("Service rifle render meshes are missing.");
                Bounds bounds = high[0].bounds;
                foreach (Renderer renderer in high) bounds.Encapsulate(renderer.bounds);
                if (bounds.size.z < .9f || bounds.size.z > 1.2f || bounds.size.y < .30f || bounds.size.y > .42f)
                    throw new InvalidOperationException("Service rifle import scale/orientation differs from the expected metre-scale +Z export.");
                var lod = root.AddComponent<LODGroup>();
                lod.SetLODs(new[] { new LOD(.45f, high), new LOD(.025f, low) });
                lod.RecalculateBounds();
                saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                Selection.activeObject = saved;
                Debug.Log("Militia service rifle art prefab ready. Review it in Unity, then assign it to the Service Rifle art references when approved.");
            }
            finally { Object.DestroyImmediate(root); }
        }
        static Texture2D Texture(string name, bool color, bool normal)
        {
            string path = Folder + "/" + name + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing rifle texture: " + path);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = color;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 2048;
            importer.anisoLevel = 4;
            importer.filterMode = FilterMode.Trilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static GameObject Instance(string name, Transform parent, Material material)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/" + name + ".fbx");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = material;
            return instance;
        }
    }
}
