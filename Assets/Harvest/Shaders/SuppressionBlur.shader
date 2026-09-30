Shader "Hidden/Harvest/SuppressionBlur"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always
        // RGB overlays the already-rendered scene; framebuffer alpha is preserved.
        Blend SrcAlpha OneMinusSrcAlpha, Zero One
        HLSLINCLUDE
            #pragma target 3.5
            #pragma editor_sync_compilation
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            // Command-buffer global: radius, bounded opacity, inverse source width/height.
            float4 _HarvestSuppressionSettings;
            half4 CopyOverlay(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half3 color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, input.texcoord, 0).rgb;
                return half4(color, clamp(_HarvestSuppressionSettings.y, 0.0, 0.45));
            }
            half4 BlurOverlay(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 step = _HarvestSuppressionSettings.zw * _HarvestSuppressionSettings.x;
                half3 color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, 0).rgb * 0.25;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2(step.x, 0), 0).rgb * 0.125;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv - float2(step.x, 0), 0).rgb * 0.125;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2(0, step.y), 0).rgb * 0.125;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv - float2(0, step.y), 0).rgb * 0.125;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + step, 0).rgb * 0.0625;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv - step, 0).rgb * 0.0625;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2(step.x, -step.y), 0).rgb * 0.0625;
                color += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2(-step.x, step.y), 0).rgb * 0.0625;
                return half4(color, clamp(_HarvestSuppressionSettings.y, 0.0, 0.45));
            }
        ENDHLSL
        Pass
        {
            Name "Copy diagnostic"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment CopyOverlay
            ENDHLSL
        }
        Pass
        {
            Name "Suppression blur overlay"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment BlurOverlay
            ENDHLSL
        }
    }
}
