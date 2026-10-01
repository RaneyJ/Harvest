using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Harvest.Editor
{
    // An isolated review space; no encounter scripts, fog, bloom or color grading.
    public static class FarmhouseMaterialReview
    {
        public const string ScenePath = "Assets/Harvest/Scenes/FarmhouseMaterialReview.unity";
        [MenuItem("Harvest/Build Farmhouse Material Review")]
        public static void Build()
        {
            if (Application.isPlaying || EditorApplication.isCompiling || FarmhouseMaterialLibrary.IsInstalling)
            { Debug.Log("Build the material review in Edit mode after the material installation completes."); return; }
            try
            {
                var manifest = FarmhouseMaterialLibrary.ReadManifest();
                var materials = new Material[manifest.Materials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = AssetDatabase.LoadAssetAtPath<Material>(FarmhouseMaterialLibrary.MaterialPath(manifest.Materials[i].Key));
                    if (materials[i] == null || materials[i].GetTexture("_BaseMap") == null)
                        throw new InvalidOperationException("Install farmhouse materials and rebuild The Line before opening this review.");
                }
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                HarvestRenderSetup.Ensure();
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                FarmhouseMeshLibrary.BeginBuild();
                RenderSettings.fog = false; RenderSettings.skybox = null;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.25f, 0.25f, 0.25f);
                var camera = new GameObject("Material review camera").AddComponent<Camera>();
                camera.tag = "MainCamera"; camera.transform.position = new Vector3(0f, 5f, -13f);
                camera.transform.LookAt(new Vector3(0f, 1.3f, 0f));
                camera.fieldOfView = 55f; camera.nearClipPlane = 0.1f; camera.farClipPlane = 60f;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
                camera.allowHDR = true;
                var light = new GameObject("Neutral white key").AddComponent<Light>();
                light.type = LightType.Directional; light.color = Color.white; light.intensity = 1.4f;
                light.shadows = LightShadows.Soft; light.transform.rotation = Quaternion.Euler(40f, -35f, 0f);
                var probe = new GameObject("Neutral reflection probe").AddComponent<ReflectionProbe>();
                probe.transform.position = new Vector3(0f, 2f, -1f); probe.size = new Vector3(22f, 10f, 18f);
                probe.mode = ReflectionProbeMode.Realtime; probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
                probe.resolution = 256; probe.clearFlags = ReflectionProbeClearFlags.SolidColor;
                probe.backgroundColor = new Color(0.25f, 0.25f, 0.25f); probe.cullingMask = ~0;
                probe.renderDynamicObjects = true;
                string floorPath = "Assets/Harvest/Materials/Farmhouse Review Neutral.mat";
                FarmhouseMeshLibrary.EnsureFolder("Assets/Harvest/Materials");
                Material floor = AssetDatabase.LoadAssetAtPath<Material>(floorPath);
                if (floor == null)
                {
                    floor = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    floor.SetColor("_BaseColor", new Color(0.28f, 0.28f, 0.28f)); floor.SetFloat("_Smoothness", 0.1f);
                    AssetDatabase.CreateAsset(floor, floorPath);
                }
                Box("Neutral floor", new Vector3(0f, -0.08f, 0f), new Vector3(18f, 0.16f, 8f), floor);
                for (int i = 0; i < materials.Length; i++)
                {
                    float x = (i - 2) * 3f;
                    var source = manifest.Materials[i]; var material = materials[i];
                    Box(source.Key + " 2m panel", new Vector3(x, 1.1f, 1f), new Vector3(2f, 2f, 0.08f), material);
                    Box(source.Key + " 1m block", new Vector3(x, 0.5f, -0.7f), Vector3.one, material);
                    var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.name = source.Key + " reflection sphere"; sphere.transform.position = new Vector3(x, 1.8f, -0.7f);
                    // Sphere is for highlight response; metre-scaled panels/blocks establish real-world tiling.
                    sphere.GetComponent<Renderer>().sharedMaterial = material;
                    UnityEngine.Object.DestroyImmediate(sphere.GetComponent<Collider>());
                    var label = new GameObject(source.Key + " label").AddComponent<TextMesh>();
                    label.text = source.Key + "\n" + source.WidthMeters.ToString("0.##") + "m tile";
                    label.transform.position = new Vector3(x, 2.7f, 0f);
                    label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    label.GetComponent<Renderer>().sharedMaterial = label.font.material;
                    label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
                    label.characterSize = 0.12f; label.fontSize = 48; label.color = Color.white;
                }
                FarmhouseMeshLibrary.EnsureFolder("Assets/Harvest/Scenes");
                AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene, ScenePath);
                probe.RenderProbe(); Selection.activeGameObject = camera.gameObject; SceneView.RepaintAll();
                Debug.Log("Saved farmhouse material review. Inspect 2m panels and 1m blocks for scale, spheres for reflections. This scene is not added to the game build.");
            }
            catch (Exception error) { Debug.LogError("Farmhouse material review failed: " + error.Message); }
        }
        static void Box(string name, Vector3 position, Vector3 size, Material material)
        {
            var go = new GameObject(name); go.transform.position = position;
            go.AddComponent<MeshFilter>().sharedMesh = FarmhouseMeshLibrary.Box(size, 0.015f);
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }
    }
}
