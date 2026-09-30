using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Harvest.Editor
{
    public static class VisualFoundationChecks
    {
        [MenuItem("Harvest/Validate Visual Foundation")]
        public static void Run()
        {
            int errors = 0;
            if (!(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset))
                Fail("Harvest URP asset is not the default pipeline. Rebuild The Line.", ref errors);
            if (QualitySettings.renderPipeline != null && !(QualitySettings.renderPipeline is UniversalRenderPipelineAsset))
                Fail("Active quality level overrides URP.", ref errors);
            if (PlayerSettings.colorSpace != ColorSpace.Linear) Fail("Linear color space is required.", ref errors);
            foreach (string name in new[] { "Harvest/Farm Surface", "Harvest/Combat Unlit", "Hidden/Harvest/SuppressionBlur", "Universal Render Pipeline/Lit" })
            {
                Shader shader = Shader.Find(name);
                if (shader == null || !shader.isSupported || ShaderUtil.ShaderHasError(shader)) Fail("Missing or failed shader: " + name, ref errors);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Harvest" }))
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (material == null) continue;
                if (material.shader == null || !material.shader.isSupported || ShaderUtil.ShaderHasError(material.shader))
                    Fail("Failed material shader: " + AssetDatabase.GetAssetPath(material), ref errors);
                if (material.shader != null && (material.shader.name == "Standard" || material.shader.name == "Sprites/Default"))
                    Fail("Unmigrated material: " + material.name, ref errors);
            }
            Camera camera = Camera.main;
            if (camera == null || camera.GetComponent<SuppressionScreenBlur>() == null)
                Fail("Open The Line; its main camera needs suppression state.", ref errors);
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Harvest/Rendering/Harvest Renderer.asset");
            if (renderer == null || !renderer.rendererFeatures.Exists(item => item is SuppressionRendererFeature feature && feature.isActive && feature.BlurShader != null))
                Fail("Suppression renderer feature is missing, disabled or unassigned.", ref errors);
            if (renderer != null && renderer.postProcessData == null) Fail("URP post-processing resources are unassigned.", ref errors);
            if (errors == 0) Debug.Log("Visual foundation checks passed. Still inspect shadows, plasma, suppression and farmhouse lighting in Play mode.");
        }
        static void Fail(string message, ref int errors) { errors++; Debug.LogError(message); }
    }
}
