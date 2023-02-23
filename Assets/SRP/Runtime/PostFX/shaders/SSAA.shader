Shader "CustomSRP/PostFX/SSAA"
{
    Properties {}


    SubShader
    {
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Assets/SRP/Shared/hlsl/CommonMath.hlsl"
        ENDHLSL

        Pass
        {
            Name "SSAA Downsampling"
            Tags
            {
                "LightMode" = "CustomPostFX"
            }
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BlitFragment

            #include "PostFX.hlsl"

            TEXTURE2D(_BlitSource);
            float4 _BlitSource_TexelSize; // x = 1.0 / width, y = 1.0 / height, z = width, w = height
            SAMPLER(sampler_linear_clamp);

            int _SuperSampleScale;

            float4 BlitFragment(Varyings FIN) : SV_TARGET
            {
                float4 result = 0;
                float2 screenUV = FIN.screenUV;
                for(int i = 0; i < _SuperSampleScale; i++)
                {
                    for(int j = 0; j < _SuperSampleScale; j++)
                    {
                        float2 offset = float2(i, j) - float2(_SuperSampleScale - 1, _SuperSampleScale - 1) * 0.5f;
                        float weight = 1.0f / (_SuperSampleScale * _SuperSampleScale);
                        screenUV = FIN.screenUV + offset * _BlitSource_TexelSize.xy;
                        float noiseScale = _BlitSource_TexelSize.z / _SuperSampleScale * 5;
                        float2 randomOffset = float2(
                            Unity_SimpleNoise_float(screenUV, noiseScale),
                            Unity_SimpleNoise_float(screenUV.yx, noiseScale));
                        randomOffset -= 0.5;
                        randomOffset *= (_SuperSampleScale - 1);
                        screenUV += randomOffset * _BlitSource_TexelSize.xy; 
                        float4 sampled = SAMPLE_TEXTURE2D_LOD(_BlitSource, sampler_linear_clamp, screenUV, 0);
                        result += sampled * weight; 
                    }
                }
                return result;
            }
            ENDHLSL
        }
        
    }
}