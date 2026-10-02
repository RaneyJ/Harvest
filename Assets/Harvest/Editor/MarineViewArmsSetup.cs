using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Harvest.Editor
{
    public static class MarineViewArmsSetup
    {
        const string Folder = "Assets/Harvest/Art/Characters/MarineViewArms";
        const string ModelPath = Folder + "/MarineServiceRifleView.fbx";
        public const string PrefabPath = Folder + "/Marine Service Rifle Animated View.prefab";

        [MenuItem("Harvest/Art/Build Marine Rifle Arms Prefab")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            ServiceRifleArtSetup.Build();
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Pull the marine arms FBX before building its prefab.");
            importer.globalScale = 1; importer.useFileScale = true;
            importer.importAnimation = true; importer.animationType = ModelImporterAnimationType.Legacy;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.addCollider = false; importer.optimizeGameObjects = false;
            importer.SaveAndReimport();
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.loopTime = clip.name.EndsWith("Idle", StringComparison.Ordinal);
                clip.wrapMode = clip.loopTime ? WrapMode.Loop : WrapMode.ClampForever;
            }
            importer.clipAnimations = clips; importer.SaveAndReimport();
            AnimationClip[] imported = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
            AnimationClip Find(string name) => imported.FirstOrDefault(c => c.name == name || c.name.EndsWith("|" + name, StringComparison.Ordinal));
            AnimationClip idle = Find("Idle"), fire = Find("Fire"), reload = Find("Reload");
            if (idle == null || fire == null || reload == null)
                throw new InvalidOperationException("The marine FBX must contain Idle, Fire and Reload takes. Found: " + string.Join(", ", imported.Select(c => c.name)));
            Material arms = BuildMaterial();
            Material rifle = AssetDatabase.LoadAssetAtPath<Material>("Assets/Harvest/Art/Weapons/MilitiaServiceRifle/Militia Service Rifle.mat");
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null) { Selection.activeObject = existing; return; }
            var root = new GameObject("Marine Service Rifle Animated View");
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), root.transform);
                var animation = model.GetComponent<Animation>();
                if (animation == null) animation = model.AddComponent<Animation>();
                animation.playAutomatically = false; animation.cullingType = AnimationCullingType.AlwaysAnimate;
                foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
                {
                    renderer.sharedMaterial = renderer.name == "MarineArms" ? arms : rifle;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    if (renderer is SkinnedMeshRenderer skinned) skinned.updateWhenOffscreen = true;
                }
                var presentation = root.AddComponent<AuthoredWeaponAnimation>();
                presentation.Player = animation; presentation.Idle = idle; presentation.Fire = fire; presentation.Reload = reload;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath); AssetDatabase.SaveAssets();
                Debug.Log("Marine rifle view prefab built. Review the three clips and hand contacts before applying to The Line.");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [MenuItem("Harvest/Art/Build Marine Rifle Arms Review")]
        public static void Review()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Build(); ServiceRifleViewReview.OpenReview(PrefabPath);
        }
        [MenuItem("Harvest/Art/Apply Animated Marine Rifle To Prototype")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Build(); ServiceRifleViewReview.Apply();
            var definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/Harvest/Data/Service Rifle.asset");
            Undo.RecordObject(definition, "Apply animated marine rifle view");
            definition.ViewModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            EditorUtility.SetDirty(definition); AssetDatabase.SaveAssets();
            Debug.Log("Animated service rifle assigned. Rebuild The Line for the player view. World pickups and NPCs retain the separate rifle-only model.");
        }
        static Material BuildMaterial()
        {
            Texture2D color = Texture("Color", true, false), normal = Texture("Normal", false, true), mask = Texture("Mask", false, false);
            string path = Folder + "/Marine View Arms.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit is required for marine view arms.");
            material = new Material(shader) { name = "Marine View Arms" };
            material.SetTexture("_BaseMap", color); material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BumpMap", normal); material.SetFloat("_BumpScale", 1);
            material.SetTexture("_MetallicGlossMap", mask); material.SetFloat("_Metallic", 1); material.SetFloat("_Smoothness", 1);
            material.SetFloat("_SmoothnessTextureChannel", 0); material.SetTexture("_OcclusionMap", mask);
            material.EnableKeyword("_NORMALMAP"); material.EnableKeyword("_METALLICSPECGLOSSMAP"); material.EnableKeyword("_OCCLUSIONMAP");
            AssetDatabase.CreateAsset(material, path); return material;
        }
        static Texture2D Texture(string suffix, bool srgb, bool normal)
        {
            string path = Folder + "/MarineArms_" + suffix + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing arm texture: " + path);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = srgb; importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true; importer.maxTextureSize = 1024; importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
