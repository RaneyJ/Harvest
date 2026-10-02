using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
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
        // Called before scene construction, so missing/import-invalid art fails before the scene is replaced.
        public static void EnsureAssets()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(ServiceRifleArtSetup.PrefabPath) == null)
                Build();
            var view = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var animation = view != null ? view.GetComponent<AuthoredWeaponAnimation>() : null;
            if (animation == null || animation.Player == null || animation.Idle == null || animation.Fire == null || animation.Reload == null ||
                !animation.Idle.legacy || !animation.Fire.legacy || !animation.Reload.legacy)
                throw new InvalidOperationException("Marine rifle view prefab is missing its animation references. Check Harvest > Art > Build Marine Rifle Arms Prefab.");
        }

        public static void AssignToDefinition(WeaponDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            var view = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var world = AssetDatabase.LoadAssetAtPath<GameObject>(ServiceRifleArtSetup.PrefabPath);
            if (view == null || world == null) throw new InvalidOperationException("Prepare the marine rifle art before assigning it.");
            definition.ViewModelPrefab = view;
            definition.WorldModel = world;
            ServiceRifleViewReview.ApplyPose(definition);
            EditorUtility.SetDirty(definition);
        }

        [MenuItem("Harvest/Art/Apply Animated Marine Rifle To Prototype")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/Harvest/Data/Service Rifle.asset");
            if (definition == null) throw new InvalidOperationException("Build The Line once to create its Service Rifle definition.");
            EnsureAssets();
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Integrate animated service rifle");
            Undo.RecordObject(definition, "Assign service rifle art");
            AssignToDefinition(definition);
            int views = 0;
            RefreshMarinePrefab(definition);
            foreach (WeaponView view in Object.FindObjectsByType<WeaponView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!view.gameObject.scene.IsValid() || EditorSceneManager.IsPreviewScene(view.gameObject.scene) || view.Definitions == null || view.Models == null) continue;
                for (int i = 0; i < view.Definitions.Length && i < view.Models.Length; i++)
                {
                    if (view.Definitions[i] != definition) continue;
                    GameObject previous = view.Models[i];
                    Transform parent = previous != null ? previous.transform.parent : view.transform;
                    GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(definition.ViewModelPrefab, parent);
                    Undo.RegisterCreatedObjectUndo(model, "Install animated service rifle");
                    model.transform.localPosition = definition.ViewPosition;
                    model.transform.localRotation = Quaternion.Euler(definition.ViewEulerAngles);
                    model.SetActive(previous == null || previous.activeSelf);
                    if (previous != null)
                    {
                        model.transform.SetSiblingIndex(previous.transform.GetSiblingIndex());
                        foreach (Transform child in model.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = previous.layer;
                    }
                    Undo.RecordObject(view, "Replace service rifle view reference");
                    view.Models[i] = model;
                    if (PrefabUtility.IsPartOfPrefabInstance(view)) PrefabUtility.RecordPrefabInstancePropertyModifications(view);
                    if (previous != null) Undo.DestroyObjectImmediate(previous);
                    EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
                    views++;
                }
            }
            Undo.CollapseUndoOperations(undoGroup);
            AssetDatabase.SaveAssets();
            Debug.Log("Service rifle integrated: " + views + " player view(s). The marine prefab and future dropped rifles use the rifle-only world model. Save the modified scene before Play. Future Build The Line runs assign this art automatically.");
        }

        public static bool RefreshMarineWorldView(AlliedMarine marine, WeaponDefinition definition)
        {
            if (marine == null) return false;
            ActorWeapon weapon = marine.GetComponent<ActorWeapon>();
            if (weapon == null || weapon.StartingWeapon != definition || definition.WorldModel == null) return false;
            Transform previous = marine.transform.Find("Held weapon");
            if (previous != null && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(previous.gameObject) == AssetDatabase.GetAssetPath(definition.WorldModel)) return false;
            var model = (GameObject)PrefabUtility.InstantiatePrefab(definition.WorldModel, marine.transform);
            model.name = "Held weapon";
            model.transform.localPosition = previous != null ? previous.localPosition : new Vector3(.38f, 1.25f, .45f);
            model.transform.localRotation = previous != null ? previous.localRotation : Quaternion.identity;
            model.transform.localScale = previous != null ? previous.localScale : Vector3.one;
            marine.GunVisual = model.transform;
            if (previous != null) Object.DestroyImmediate(previous.gameObject);
            return true;
        }

        static void RefreshMarinePrefab(WeaponDefinition definition)
        {
            const string path = "Assets/Harvest/Prefabs/Allied Marine.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return;
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                AlliedMarine marine = contents.GetComponent<AlliedMarine>();
                if (marine != null && RefreshMarineWorldView(marine, definition))
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
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

