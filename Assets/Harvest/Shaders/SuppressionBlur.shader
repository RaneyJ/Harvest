Shader "Hidden/Harvest/SuppressionBlur"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            float _Radius;
            float _Blend;
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 step = _BlitTexture_TexelSize.xy * _Radius;
                half4 original = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                half4 blurred = original * 0.25;
                blurred += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(step.x, 0)) * 0.125;
                blurred += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(step.x, 0)) * 0.125;
                blurred += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(0, step.y)) * 0.125;
                blurred += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(0, step.y)) * 0.125;
                blurred += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + step) * 0.0625;
                blurred += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - step) * 0.0625;
                blurred += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(step.x, -step.y)) * 0.0625;
                blurred += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-step.x, step.y)) * 0.0625;
                return lerp(original, blurred, saturate(_Blend));
            }
            ENDHLSL
        }
    }
}
