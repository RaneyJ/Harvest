using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Harvest.Editor
{
    public static class SliceLightingReview
    {
        public const string SettingsPath="Assets/Harvest/Rendering/Harvest Lighting.lighting";
        const string ScenePath="Assets/Harvest/Scenes/TheLine.unity";
        public static void Configure()
        {
            FarmhouseMeshLibrary.EnsureFolder("Assets/Harvest/Rendering");
            var settings=AssetDatabase.LoadAssetAtPath<LightingSettings>(SettingsPath);
            if(settings==null)
            {
                settings=new LightingSettings {name="Harvest Lighting",bakedGI=true,realtimeGI=false,
                    lightmapper=LightingSettings.Lightmapper.ProgressiveCPU,mixedBakeMode=MixedLightingMode.IndirectOnly,
                    lightmapResolution=24f,lightmapMaxSize=2048,lightmapPadding=4,directSampleCount=64,
                    indirectSampleCount=256,environmentSampleCount=256,maxBounces=3,
                    directionalityMode=LightmapsMode.CombinedDirectional,ao=true,aoMaxDistance=.35f,
                    aoExponentIndirect=.6f,aoExponentDirect=0f};
                AssetDatabase.CreateAsset(settings,SettingsPath);
            }
            Lightmapping.lightingSettings=settings; // Existing artist tuning in this asset is retained.
        }
        [MenuItem("Harvest/Lighting/Bake The Line Lighting")]
        public static void Bake()
        {
            if(!CanEdit() || Lightmapping.isRunning) return;
            Configure();
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            if(!Lightmapping.BakeAsync()) Debug.LogError("Lighting bake could not start. Check the Lighting window and Console.");
            else Debug.Log("Lighting bake started. Wait for Unity to finish, inspect the rooms, then run Bake The Line Reflections and save the scene. Rebuilding invalidates this scene's bake.");
        }
        [MenuItem("Harvest/Lighting/Bake The Line Reflections")]
        public static void BakeReflections()
        {
            if(!CanEdit() || Lightmapping.isRunning) return;
            if(LightmapSettings.lightmaps==null || LightmapSettings.lightmaps.Length==0)
            { Debug.LogWarning("Bake The Line Lighting first so reflections capture the finished bounce lighting.");return; }
            GameObject root=GameObject.Find(FarmhouseLighting.RootName);
            if(root==null) { Debug.LogError("Rebuild The Line to create the fixture/probe rig.");return; }
            const string folder="Assets/Harvest/Rendering/Reflections";FarmhouseMeshLibrary.EnsureFolder(folder);
            int count=0;
            var hidden=new List<Renderer>();
            foreach(Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                if(renderer.enabled && renderer.GetComponentInParent<CombatTarget>()!=null)
                { hidden.Add(renderer);renderer.enabled=false; }
            // Capture the environment, not frozen characters or the editor's first-person model stack.
            try
            {
                foreach(ReflectionProbe probe in root.GetComponentsInChildren<ReflectionProbe>())
                {
                    string path=folder+"/"+probe.name.Replace(" ","_")+".exr";
                    if(!Lightmapping.BakeReflectionProbe(probe,path)) { Debug.LogError("Reflection bake failed: "+probe.name);continue; }
                    AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                    Texture texture=AssetDatabase.LoadAssetAtPath<Texture>(path);
                    if(texture==null || texture.dimension!=TextureDimension.Cube)
                    { Debug.LogError("Reflection capture did not import as a cubemap: "+path);continue; }
                    probe.customBakedTexture=texture;probe.mode=ReflectionProbeMode.Custom;EditorUtility.SetDirty(probe);count++;
                }
            }
            finally {foreach(Renderer renderer in hidden) if(renderer!=null) renderer.enabled=true;}
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("Saved "+count+" reflection captures. Successful probes use saved cubemaps rather than runtime startup captures.");
        }
        [MenuItem("Harvest/Lighting/Validate The Line Lighting")]
        public static void Validate()
        {
            if(SceneManager.GetActiveScene().path!=ScenePath) {Debug.LogError("Open The Line to validate lighting.");return;}
            int errors=0;GameObject root=GameObject.Find(FarmhouseLighting.RootName);
            var house=Object.FindFirstObjectByType<FarmhouseFoundation>();
            if(house!=null && house.Layout!=null && house.Layout.AuthoredPrefab!=null)
            {Debug.Log("Artist-owned farmhouse assigned: review its lighting rig manually.");return;}
            if(root==null) {Debug.LogError("Fixture/probe rig is missing. Rebuild The Line.");return;}
            foreach(Light light in root.GetComponentsInChildren<Light>())
                if(light.shadows==LightShadows.None || light.lightmapBakeType!=LightmapBakeType.Mixed || light.range<=0f)
                {Debug.LogError("Practical light lacks shadows, mixed lighting or range: "+light.transform.parent.name);errors++;}
            var group=root.GetComponentInChildren<LightProbeGroup>();
            if(group==null || group.probePositions.Length<32) {Debug.LogError("Room/approach light probes are missing.");errors++;}
            var pipeline=GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if(pipeline==null || !pipeline.supportsAdditionalLightShadows || pipeline.additionalLightsRenderingMode!=LightRenderingMode.PerPixel)
            {Debug.LogError("URP additional lights need per-pixel lighting and shadows.");errors++;}
            foreach(ReflectionProbe probe in root.GetComponentsInChildren<ReflectionProbe>())
                if(!probe.boxProjection || (probe.mode==ReflectionProbeMode.Custom && probe.customBakedTexture==null))
                {Debug.LogError("Invalid room/reflection probe: "+probe.name);errors++;}
            if(Lightmapping.lightingSettings==null) {Debug.LogError("Persistent lighting settings are missing.");errors++;}
            if(LightmapSettings.lightmaps==null || LightmapSettings.lightmaps.Length==0)
                Debug.LogWarning("The scene has no baked lightmaps yet. Realtime lighting is a preview; run Bake The Line Lighting for bounce/contact shading.");
            if(errors==0) Debug.Log("Lighting setup checks passed. Inspect stair transitions, window shadows, props and moving actors after the bake; setup checks do not approve the rendered result.");
        }
        static bool CanEdit()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path!=ScenePath)
            {Debug.LogWarning("Open The Line outside Play mode before baking lighting.");return false;}
            return true;
        }
    }
}
