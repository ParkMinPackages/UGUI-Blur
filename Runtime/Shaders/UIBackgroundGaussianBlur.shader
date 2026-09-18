Shader "Hidden/ParkMinPackages/UGUI/UIBackgroundGaussianBlur"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float4 _BlurRadius;

        half4 Gaussian(float2 uv, float2 direction, float radius)
        {
            if (radius < 0.5)
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

            float sigma = max(radius / 3.0, 0.5);
            int extent = min((int)ceil(radius), 32);
            float4 color = 0;
            float totalWeight = 0;
            for (int offset = -extent; offset <= extent; offset++)
            {
                float weight = exp(-0.5 * offset * offset / (sigma * sigma));
                color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + direction * offset) * weight;
                totalWeight += weight;
            }
            return color / totalWeight;
        }
        half4 Horizontal(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            return Gaussian(input.texcoord, float2(_BlitTexture_TexelSize.x, 0), _BlurRadius.x);
        }
        half4 Vertical(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            return Gaussian(input.texcoord, float2(0, _BlitTexture_TexelSize.y), _BlurRadius.y);
        }
        half4 Downsample(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 offset = _BlitTexture_TexelSize.xy * 0.5;
            return (SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord + offset)
                + SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord - offset)
                + SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord + float2(offset.x, -offset.y))
                + SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord + float2(-offset.x, offset.y))) * 0.25;
        }
        ENDHLSL

        Pass
        {
            Name "Horizontal"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Horizontal
            ENDHLSL
        }
        Pass
        {
            Name "Vertical"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Vertical
            ENDHLSL
        }
        Pass
        {
            Name "Downsample"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Downsample
            ENDHLSL
        }
    }
}
