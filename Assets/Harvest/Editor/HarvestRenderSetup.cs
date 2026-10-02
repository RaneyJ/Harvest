using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Harvest.Editor
{
    // Project setup and in-place material migration are separate from encounter/gameplay authoring.
    public static class HarvestRenderSetup
    {
        const string Folder = "Assets/Harvest/Rendering";
        public static void Ensure()
        {
            Directory.CreateDirectory(Folder);
            string rendererPath = Folder + "/Harvest Renderer.asset";
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                renderer.name = "Harvest Renderer";
                renderer.renderingMode = RenderingMode.Forward;
                renderer.intermediateTextureMode = IntermediateTextureMode.Always;
                AssetDatabase.CreateAsset(renderer, rendererPath);
            }
            // Unity's renderer factory assigns this explicitly; a bare CreateInstance does not.
            if (renderer.postProcessData == null)
            {
                renderer.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(
                    "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
                if (renderer.postProcessData == null)
                    throw new System.InvalidOperationException("URP post-processing resources are missing. Let Package Manager finish importing URP, then rebuild.");
                renderer.SetDirty(); EditorUtility.SetDirty(renderer);
            }
            var feature = renderer.rendererFeatures.Find(item => item is SuppressionRendererFeature) as SuppressionRendererFeature;
            if (feature == null)
            {
                feature = ScriptableObject.CreateInstance<SuppressionRendererFeature>();
                feature.name = "Harvest suppression";
                feature.BlurShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Harvest/Shaders/SuppressionBlur.shader");
                AssetDatabase.AddObjectToAsset(feature, renderer);
                renderer.rendererFeatures.Add(feature);
                feature.Create();
                renderer.SetDirty();
                EditorUtility.SetDirty(renderer);
            }
            string pipelinePath = Folder + "/Harvest URP.asset";
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.name = "Harvest URP";
                pipeline.supportsHDR = true;
                pipeline.msaaSampleCount = 1;
                pipeline.shadowDistance = 85f;
                pipeline.shadowCascadeCount = 4;
                pipeline.mainLightShadowmapResolution = 2048;
                // URP exposes only an internal setter; configure its serialized editor setting.
                var settings = new SerializedObject(pipeline);
                SerializedProperty softShadows = settings.FindProperty("m_SoftShadowsSupported");
                if (softShadows == null)
                    throw new System.InvalidOperationException("URP soft-shadow setting was not found. Check the installed URP version.");
                softShadows.boolValue = true;
                settings.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }
            PlayerSettings.colorSpace = ColorSpace.Linear;
            GraphicsSettings.defaultRenderPipeline = pipeline;
            // Quality overrides otherwise silently keep the project on the old pipeline.
            int selected = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(selected, false);
            MigrateMaterials();
            AssetDatabase.SaveAssets();
        }
        static void MigrateMaterials()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Harvest" }))
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (material == null || material.shader == null) continue;
                string shader = material.shader.name;
                if (shader == "Sprites/Default") { material.shader = Shader.Find("Harvest/Combat Unlit"); EditorUtility.SetDirty(material); continue; }
                if (shader != "Standard") continue;
                Color color = material.color;
                Color emission = material.HasProperty("_EmissionColor") ? material.GetColor("_EmissionColor") : Color.black;
                float metallic = material.GetFloat("_Metallic"), smoothness = material.GetFloat("_Glossiness");
                bool transparent = material.GetFloat("_Mode") >= 2f;
                Texture albedo = material.GetTexture("_MainTex");
                Texture normal = material.GetTexture("_BumpMap");
                Vector2 scale = material.GetTextureScale("_MainTex"), offset = material.GetTextureOffset("_MainTex");
                material.shader = Shader.Find("Universal Render Pipeline/Lit");
                material.SetColor("_BaseColor", color);
                material.SetTexture("_BaseMap", albedo);
                material.SetTextureScale("_BaseMap", scale); material.SetTextureOffset("_BaseMap", offset);
                material.SetTexture("_BumpMap", normal);
                if (normal != null) material.EnableKeyword("_NORMALMAP");
                material.SetFloat("_Metallic", metallic); material.SetFloat("_Smoothness", smoothness);
                material.SetColor("_EmissionColor", emission);
                if (emission.maxColorComponent > 0f) material.EnableKeyword("_EMISSION");
                if (transparent) MakeTransparent(material);
                else
                {
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.SetFloat("_Surface", 0f); material.SetInt("_ZWrite", 1);
                    material.SetInt("_SrcBlend", (int)BlendMode.One); material.SetInt("_DstBlend", (int)BlendMode.Zero);
                    material.renderQueue = -1;
                }
                EditorUtility.SetDirty(material);
            }
        }
        public static void MakeTransparent(Material material)
        {
            material.SetFloat("_Surface", 1f); material.SetFloat("_Blend", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.DisableKeyword("_ALPHABLEND_ON");
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0); material.renderQueue = 3000;
        }
    }
}
