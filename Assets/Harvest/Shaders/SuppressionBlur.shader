Shader "Hidden/Harvest/SuppressionBlur"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _BlurredTex ("Blurred", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex;
        sampler2D _BlurredTex;
        float4 _MainTex_TexelSize;
        float4 _BlurredTex_TexelSize;
        float2 _BlurStep;
        float _Blend;

        half4 Blur(v2f_img input) : SV_Target
        {
            float2 uv = input.uv;
            half4 color = tex2D(_MainTex, uv) * 0.227027;
            color += tex2D(_MainTex, uv + _BlurStep * 1.384615) * 0.316216;
            color += tex2D(_MainTex, uv - _BlurStep * 1.384615) * 0.316216;
            color += tex2D(_MainTex, uv + _BlurStep * 3.230769) * 0.070270;
            color += tex2D(_MainTex, uv - _BlurStep * 3.230769) * 0.070270;
            return color;
        }
        half4 Composite(v2f_img input) : SV_Target
        {
            float2 blurredUV = input.uv;
            #if UNITY_UV_STARTS_AT_TOP
            if (_MainTex_TexelSize.y * _BlurredTex_TexelSize.y < 0)
                blurredUV.y = 1 - blurredUV.y;
            #endif
            return lerp(tex2D(_MainTex, input.uv), tex2D(_BlurredTex, blurredUV), saturate(_Blend));
        }
        ENDCG
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Blur
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Composite
            ENDCG
        }
    }
    Fallback Off
}
