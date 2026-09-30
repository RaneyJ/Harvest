using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace Harvest
{
    // Preserve the camera framebuffer; sample a separate copy and alpha-blend a limited overlay.
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
            static readonly Vector4 FullTexture = new Vector4(1f, 1f, 0f, 0f);
            sealed class CopyData { public TextureHandle Source; }
            sealed class OverlayData
            {
                public TextureHandle Source;
                public Material Material;
                public Vector4 Settings;
                public int ShaderPass;
            }
            public BlurPass(Material source)
            {
                material = source;
                // Work on scene color before grading/FXAA swap camera targets.
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
                ConfigureInput(ScriptableRenderPassInput.Color);
            }
            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                var resources = frameData.Get<UniversalResourceData>();
                var camera = frameData.Get<UniversalCameraData>().camera;
                var state = camera.GetComponent<SuppressionScreenBlur>();
                if (resources.isActiveTargetBackBuffer || state == null || !state.isActiveAndEnabled ||
                    state.VisiblePressure < 0.001f || state.MaxBlend <= 0f || state.MaxBlurRadius <= 0f) return;
                TextureHandle sceneColor = resources.activeColorTexture;
                if (!sceneColor.IsValid()) return;
                var descriptor = graph.GetTextureDesc(sceneColor);
                descriptor.name = "Harvest suppression scene copy";
                descriptor.clearBuffer = false;
                descriptor.msaaSamples = MSAASamples.None;
                TextureHandle copy = graph.CreateTexture(descriptor);
                using (var builder = graph.AddRasterRenderPass<CopyData>("Harvest suppression copy", out var data))
                {
                    data.Source = sceneColor;
                    builder.UseTexture(sceneColor, AccessFlags.Read);
                    builder.SetRenderAttachment(copy, 0, AccessFlags.WriteAll);
                    builder.SetRenderFunc((CopyData draw, RasterGraphContext context) =>
                        Blitter.BlitTexture(context.cmd, draw.Source, FullTexture, 0f, false));
                }
                using (var builder = graph.AddRasterRenderPass<OverlayData>("Harvest suppression overlay", out var data))
                {
                    data.Source = copy;
                    data.Material = material;
                    data.ShaderPass = state.DiagnosticCopyOnly ? 0 : 1;
                    data.Settings = new Vector4(state.VisiblePressure * Mathf.Max(0f, state.MaxBlurRadius),
                        state.VisiblePressure * Mathf.Clamp(state.MaxBlend, 0f, 0.45f),
                        1f / Mathf.Max(1, descriptor.width), 1f / Mathf.Max(1, descriptor.height));
                    builder.UseTexture(copy, AccessFlags.Read);
                    // ReadWrite is essential: blending must load the existing scene color.
                    builder.SetRenderAttachment(sceneColor, 0, AccessFlags.ReadWrite);
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc((OverlayData draw, RasterGraphContext context) =>
                    {
                        context.cmd.SetGlobalVector(Shader.PropertyToID("_HarvestSuppressionSettings"), draw.Settings);
                        Blitter.BlitTexture(context.cmd, draw.Source, FullTexture, draw.Material, draw.ShaderPass);
                    });
                }
                state.LastScheduledRenderFrame = Time.frameCount;
                // Keep cameraColor and its identity intact. URP's later passes use the original target.
            }
        }
    }
}
