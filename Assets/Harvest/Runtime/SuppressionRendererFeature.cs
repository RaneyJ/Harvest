using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

namespace Harvest
{
    public sealed class SuppressionRendererFeature : ScriptableRendererFeature
    {
        public Shader BlurShader;
        Material material;
        BlurPass pass;
        public override void Create()
        {
            CoreUtils.Destroy(material);
            material = null; pass = null;
            if (BlurShader == null || !BlurShader.isSupported) return;
            material = CoreUtils.CreateEngineMaterial(BlurShader);
            pass = new BlurPass(material);
        }
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (pass == null || renderingData.cameraData.cameraType != CameraType.Game) return;
            var state = renderingData.cameraData.camera.GetComponent<SuppressionScreenBlur>();
            if (state == null || !state.isActiveAndEnabled || state.VisiblePressure < 0.001f ||
                state.MaxBlend <= 0f || state.MaxBlurRadius <= 0f) return;
            renderer.EnqueuePass(pass);
        }
        protected override void Dispose(bool disposing) { CoreUtils.Destroy(material); material = null; pass = null; }
        sealed class BlurPass : ScriptableRenderPass
        {
            readonly Material material;
            public BlurPass(Material source)
            {
                material = source;
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
                ConfigureInput(ScriptableRenderPassInput.Color);
            }
            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                var resources = frameData.Get<UniversalResourceData>();
                var camera = frameData.Get<UniversalCameraData>().camera;
                var state = camera.GetComponent<SuppressionScreenBlur>();
                if (resources.isActiveTargetBackBuffer || state == null || !state.isActiveAndEnabled) return;
                TextureHandle source = resources.activeColorTexture;
                if (!source.IsValid()) return;
                var descriptor = graph.GetTextureDesc(source);
                descriptor.name = "Harvest suppressed view";
                descriptor.clearBuffer = false;
                TextureHandle destination = graph.CreateTexture(descriptor);
                // Snapshot per-camera values; don't mutate a shared material during graph recording.
                var properties = new MaterialPropertyBlock();
                properties.SetFloat("_Radius", state.VisiblePressure * state.MaxBlurRadius);
                properties.SetFloat("_Blend", state.VisiblePressure * state.MaxBlend);
                properties.SetVector("_SourceTexelSize", new Vector4(1f / Mathf.Max(1, descriptor.width),
                    1f / Mathf.Max(1, descriptor.height), descriptor.width, descriptor.height));
                // Bind the texture and optional integer properties explicitly. The released
                // URP helper owns its scale-vector binding; it exposes no scaleBiasPropertyID.
                var parameters = new RenderGraphUtils.BlitMaterialParameters(source, destination, material, 0);
                parameters.propertyBlock = properties;
                parameters.geometry = RenderGraphUtils.FullScreenGeometryType.ProceduralTriangle;
                parameters.sourceTexturePropertyID = Shader.PropertyToID("_BlitTexture");
                parameters.sourceSlicePropertyID = Shader.PropertyToID("_BlitTexArraySlice");
                parameters.sourceMipPropertyID = Shader.PropertyToID("_BlitMipLevel");
                parameters.destinationSlice = 0;
                parameters.destinationMip = 0;
                parameters.sourceSlice = -1;
                parameters.sourceMip = -1;
                parameters.numSlices = -1;
                parameters.numMips = 1;
                graph.AddBlitPass(parameters, passName: "Harvest suppression blur");
                state.LastScheduledRenderFrame = Time.frameCount;
                resources.cameraColor = destination;
            }
        }
    }
}
