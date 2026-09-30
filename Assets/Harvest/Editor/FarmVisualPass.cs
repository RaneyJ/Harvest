using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Harvest.Editor
{
    public static class FarmVisualPass
    {
        const string Folder = "Assets/Harvest/Rendering";
        public static void Build(Camera camera)
        {
            Directory.CreateDirectory(Folder);
            var profile = AssetDatabase.LoadAssetAtPath<HarvestVisualProfile>(Folder + "/Harvest Look.asset");
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<HarvestVisualProfile>();
                AssetDatabase.CreateAsset(profile, Folder + "/Harvest Look.asset");
            }
            HarvestSurfaceLibrary.Apply();
            Light sun = new GameObject("Harvest late afternoon sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.color = profile.SunColor; sun.intensity = profile.SunIntensity;
            sun.transform.rotation = Quaternion.Euler(profile.SunRotation);
            sun.shadows = LightShadows.Soft; sun.shadowBias = 0.035f; sun.shadowNormalBias = 0.25f;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = profile.AmbientSky;
            RenderSettings.ambientEquatorColor = profile.AmbientEquator;
            RenderSettings.ambientGroundColor = profile.AmbientGround;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = profile.FogColor; RenderSettings.fogDensity = profile.FogDensity;
            Material sky = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Harvest Sky.mat");
            if (sky == null)
            {
                sky = new Material(Shader.Find("Skybox/Procedural"));
                sky.SetFloat("_SunSize", 0.035f); sky.SetFloat("_AtmosphereThickness", 1.15f);
                sky.SetColor("_SkyTint", new Color(0.53f, 0.58f, 0.65f));
                sky.SetColor("_GroundColor", new Color(0.25f, 0.23f, 0.19f)); sky.SetFloat("_Exposure", 1.05f);
                AssetDatabase.CreateAsset(sky, Folder + "/Harvest Sky.mat");
            }
            RenderSettings.skybox = sky;
            camera.clearFlags = CameraClearFlags.Skybox; camera.backgroundColor = profile.FogColor;
            camera.allowHDR = true; camera.farClipPlane = 400f;
            var cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            cameraData.volumeLayerMask = ~0;
            Volume volume = new GameObject("Harvest color and atmosphere").AddComponent<Volume>();
            volume.isGlobal = true;
            // Persist editable Volume settings independently; rebuilds never reset artist tuning.
            VolumeProfile post = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Folder + "/Harvest Post.asset");
            if (post == null)
            {
                post = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(post, Folder + "/Harvest Post.asset");
                var tone = post.Add<Tonemapping>(); tone.mode.Override(TonemappingMode.ACES);
                var bloom = post.Add<Bloom>(); bloom.intensity.Override(0.18f); bloom.threshold.Override(1.25f); bloom.scatter.Override(0.55f);
                var color = post.Add<ColorAdjustments>(); color.postExposure.Override(0.15f); color.contrast.Override(8f); color.saturation.Override(-8f);
                var vignette = post.Add<Vignette>(); vignette.intensity.Override(0.12f); vignette.smoothness.Override(0.4f);
                foreach (var component in post.components) AssetDatabase.AddObjectToAsset(component, post);
                EditorUtility.SetDirty(post);
            }
            volume.sharedProfile = post;
            AddLamp(new Vector3(-16f, 2.65f, 2f), 1.4f, 6f);
            AddLamp(new Vector3(-21f, 5.6f, 2f), 1.1f, 5f);
            ReflectionProbe probe = new GameObject("Farmyard reflection probe").AddComponent<ReflectionProbe>();
            probe.transform.position = new Vector3(-15f, 3f, -3f);
            probe.size = new Vector3(45f, 18f, 42f);
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.resolution = 128; probe.farClipPlane = 150f; probe.boxProjection = true;
            Background();
            AssetDatabase.SaveAssets();
        }
        static void AddLamp(Vector3 position, float intensity, float range)
        {
            Light lamp = new GameObject("Farmhouse warm practical light").AddComponent<Light>();
            lamp.type = LightType.Point; lamp.transform.position = position;
            lamp.color = new Color(1f, 0.71f, 0.43f); lamp.intensity = intensity; lamp.range = range;
            lamp.shadows = LightShadows.None;
        }
        static void Background()
        {
            Transform root = new GameObject("Distant farm landscape — presentation only").transform;
            Material hills = AssetDatabase.LoadAssetAtPath<Material>("Assets/Harvest/Materials/Soil.mat");
            Material buildings = AssetDatabase.LoadAssetAtPath<Material>("Assets/Harvest/Materials/Concrete.mat");
            if (hills == null || buildings == null) return;
            // A visual ground apron joins the playable terrain to the horizon; never baked into navigation.
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Distant ground apron"; ground.transform.SetParent(root, false);
            ground.transform.position = new Vector3(0f, -0.45f, 0f);
            ground.transform.localScale = new Vector3(420f, 0.5f, 420f);
            ground.GetComponent<Renderer>().sharedMaterial = hills;
            ground.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            Object.DestroyImmediate(ground.GetComponent<Collider>());
            for (int i = 0; i < 7; i++)
            {
                GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
                building.name = "Distant agricultural settlement"; building.transform.SetParent(root, false);
                building.transform.position = new Vector3(-52f + i * 15f, 3f, 115f + (i % 3) * 8f);
                building.transform.localScale = new Vector3(8f + i % 3 * 2f, 6f, 12f);
                building.GetComponent<Renderer>().sharedMaterial = buildings;
                Object.DestroyImmediate(building.GetComponent<Collider>());
            }
        }
    }
}
