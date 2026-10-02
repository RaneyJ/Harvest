using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Harvest.Editor
{
    // A repeatable camera study using the same authored pose as the prototype integration.
    public static class ServiceRifleViewReview
    {
        const string Folder = "Assets/Harvest/Art/Weapons/MilitiaServiceRifle";
        [Serializable] sealed class Pose
        {
            public Vector3 Position, Euler;
            public float VerticalFov, NearClip, Aspect;
        }
        static Pose ReadPose()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(Folder + "/ViewPose.json");
            if (asset == null) throw new InvalidOperationException("Missing rifle camera pose.");
            var pose = JsonUtility.FromJson<Pose>(asset.text);
            if (pose == null || pose.VerticalFov < 30 || pose.VerticalFov > 100 || pose.NearClip <= 0 || pose.Aspect <= 0)
                throw new InvalidOperationException("Invalid rifle camera pose.");
            return pose;
        }

        [MenuItem("Harvest/Art/Build Service Rifle View Review")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            ServiceRifleArtSetup.Build();
            OpenReview(ServiceRifleArtSetup.ViewPrefabPath);
        }
        public static void OpenReview(string prefabPath)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Pose pose = ReadPose();
            HarvestRenderSetup.Ensure();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.fog = false; RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.25f, .25f, .25f);
            var camera = new GameObject("Rifle review camera — 75 degree vertical FOV").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.fieldOfView = pose.VerticalFov; camera.aspect = pose.Aspect;
            camera.nearClipPlane = pose.NearClip; camera.farClipPlane = 100;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.13f, .14f, .12f);
            var view = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), camera.transform);
            view.transform.localPosition = pose.Position; view.transform.localRotation = Quaternion.Euler(pose.Euler);
            var light = new GameObject("Neutral review key").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.5f;
            light.transform.rotation = Quaternion.Euler(35, -30, 0); light.shadows = LightShadows.Soft;
            var fill = new GameObject("Neutral review fill").AddComponent<Light>();
            fill.type = LightType.Directional; fill.intensity = .35f; fill.transform.rotation = Quaternion.Euler(20, 150, 0);
            if (view.GetComponent<AuthoredWeaponAnimation>() == null) Validate(view.transform, camera, pose);
            FarmhouseMeshLibrary.EnsureFolder("Assets/Harvest/Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/Harvest/Scenes/ServiceRifleViewReview.unity");
            Selection.activeGameObject = view;
            Debug.Log("Rifle camera review ready. Use a 16:9 Game view; inspect the neutral-lit materials, silhouette and near clipping. This is a review scene, not an encounter capture.");
        }

        [MenuItem("Harvest/Art/Apply Militia Rifle To Prototype")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/Harvest/Data/Service Rifle.asset");
            if (definition == null) throw new InvalidOperationException("Build The Line once to create its Service Rifle definition.");
            Pose pose = ReadPose(); ServiceRifleArtSetup.Build();
            Undo.RecordObject(definition, "Apply militia service rifle art");
            definition.ViewModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ServiceRifleArtSetup.ViewPrefabPath);
            definition.WorldModel = AssetDatabase.LoadAssetAtPath<GameObject>(ServiceRifleArtSetup.PrefabPath);
            definition.OverrideViewPose = true;
            definition.ViewPosition = pose.Position; definition.ViewEulerAngles = pose.Euler;
            EditorUtility.SetDirty(definition); AssetDatabase.SaveAssets();
            Debug.Log("Militia rifle art and first-person pose assigned. Rebuild The Line to refresh the player model. Authored NPC held models must be refreshed separately; existing NPC presentation is preserved.");
        }
        static void Validate(Transform view, Camera camera, Pose pose)
        {
            Transform muzzle = null;
            foreach (Transform child in view.GetComponentsInChildren<Transform>()) if (child.name == "Muzzle") { muzzle = child; break; }
            if (muzzle == null || view.InverseTransformPoint(muzzle.position).z < .5f)
                throw new InvalidOperationException("Imported rifle muzzle must point along local +Z.");
            float nearest = float.PositiveInfinity;
            foreach (float kick in new[] { 0f, .12f })
            {
                view.localPosition = pose.Position + new Vector3(0, 0, -kick);
                view.localRotation = Quaternion.Euler(pose.Euler) * Quaternion.Euler(kick > 0 ? -12 : 0, 0, 0);
                foreach (MeshFilter mesh in view.GetComponentsInChildren<MeshFilter>())
                {
                    Bounds b = mesh.sharedMesh.bounds;
                    foreach (int x in new[] { -1, 1 }) foreach (int y in new[] { -1, 1 }) foreach (int z in new[] { -1, 1 })
                    {
                        Vector3 point = mesh.transform.TransformPoint(b.center + Vector3.Scale(b.extents, new Vector3(x, y, z)));
                        nearest = Mathf.Min(nearest, camera.transform.InverseTransformPoint(point).z);
                    }
                }
            }
            view.localPosition = pose.Position; view.localRotation = Quaternion.Euler(pose.Euler);
            if (nearest <= camera.nearClipPlane) throw new InvalidOperationException("Rifle enters the camera near plane during recoil. Adjust ViewPose.json.");
            Debug.Log("Rifle view bounds pass near-plane check, including maximum existing recoil. Minimum depth: " + nearest.ToString("F3") + " m.");
        }
    }
}
