Shader "CustomSRP/PostFX/Bloom"
{
    Properties {}


    SubShader
    {
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        
        #include "PostFX.hlsl"

        TEXTURE2D(_BlitSource);
        float4 _BlitSource_TexelSize; // x = 1/width, y = 1/height, z = width, w = height.

        TEXTURE2D(_ExtraSource1); // _BlitSource is the low-res glow, source1 is the "layer beneath" it
        float4 _ExtraSource1_TexelSize;

        // See https://catlikecoding.com/unity/tutorials/custom-srp/post-processing/#2.7
        // x = t, y = -t + t * k, z = 2 * t * k, w = 1 / (4 * t * k + 0.00001)
        float4 _BloomKneePrecomputed;

        float _BloomIntensity;

        TEXTURE2D(_BloomPrefiltered);
        
        SAMPLER(sampler_linear_clamp);
        ENDHLSL
        
        Pass
        {
            Name "Bloom Prefilter"
            
            Tags
            {
                "LightMode" = "CustomPostFX"
            }
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomPrefilterFragment 

            float kneedFactor(float b)
            {
                float x = _BloomKneePrecomputed.x;
                float y = _BloomKneePrecomputed.y;
                float z = _BloomKneePrecomputed.z;
                float w = _BloomKneePrecomputed.w;
            
                float s = clamp(b + y, 0, z);
                s *= s;
                s *= w;

                return max(s, b - x) / max(b, 0.00001);
            }

            float4 BloomPrefilterFragment(Varyings FIN) : SV_TARGET
            {
                float4 sampled = SAMPLE_TEXTURE2D_LOD(_BlitSource, sampler_linear_clamp, FIN.screenUV, 0);
                float brightness = max(sampled.r, max(sampled.g, sampled.b));
                float bloomFactor = kneedFactor(brightness);
                
                return float4(sampled.rgb * bloomFactor, sampled.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Horizontal Blur"
            
            Tags
            {
                "LightMode" = "CustomPostFX"
            }
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomHorizontalBlur


            float4 BloomHorizontalBlur(Varyings FIN) : SV_TARGET
            {
                // return float4(FIN.screenUV.xy, 0.0, 1);
                float4 result = float4(0, 0, 0, 0);
                float2 screenUV = FIN.screenUV;
                float offsets[] = {
                    -4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0
                };
                float weights[] = {
                    0.01621622, 0.05405405, 0.12162162, 0.19459459, 0.22702703,
                    0.19459459, 0.12162162, 0.05405405, 0.01621622
                };
                for(int i = 0; i < 9; i++)
                {
                    float2 uv = screenUV + float2(offsets[i] * _BlitSource_TexelSize.x, 0);
                    float4 sampled = SAMPLE_TEXTURE2D_LOD(_BlitSource, sampler_linear_clamp, uv, 0);
                    result += sampled * weights[i];
                }
                // return float4(0, 0, 1, 1);
                return result;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "Vertical Blur"
            
            Tags
            {
                "LightMode" = "CustomPostFX"
            }
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomVerticalBlur

            #include "PostFX.hlsl"


            float4 BloomVerticalBlur(Varyings FIN) : SV_TARGET
            {
                // return float4(FIN.screenUV.xy, 0.0, 1);
                float4 result = float4(0, 0, 0, 0);
                float2 screenUV = FIN.screenUV;
                float offsets[] = {
                    -4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0
                };
                float weights[] = {
                    0.01621622, 0.05405405, 0.12162162, 0.19459459, 0.22702703,
                    0.19459459, 0.12162162, 0.05405405, 0.01621622
                };
                for(int i = 0; i < 9; i++)
                {
                    float2 uv = screenUV + float2(0, offsets[i] * _BlitSource_TexelSize.y);
                    // float2 uv = screenUV;
                    float4 sampled = SAMPLE_TEXTURE2D_LOD(_BlitSource, sampler_linear_clamp, uv, 0);
                    result += sampled * weights[i];
                }
                // return float4(0, 0, 1, 1);
                return result;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "Combine Bloom"
            
            Tags
            {
                "LightMode" = "CustomPostFX"
            }
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DefaultPassVertex
            #pragma fragment CombineBloomFragment


            float4 CombineBloomFragment(Varyings FIN) : SV_TARGET
            {
                float4 sampledLow = SAMPLE_TEXTURE2D_LOD(_BlitSource, sampler_linear_clamp, FIN.screenUV, 0);
                float4 sampledHigh = SAMPLE_TEXTURE2D_LOD(_ExtraSource1, sampler_linear_clamp, FIN.screenUV, 0);
                
                return float4(sampledLow.rgb * _BloomIntensity + sampledHigh.rgb, sampledHigh.a);
                    
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "Combine Bloom Final"
            
            Tags
            {
                "LightMode" = "CustomPostFX"
            }
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DefaultPassVertex
            #pragma fragment CombineBloomFragment


            float4 CombineBloomFragment(Varyings FIN) : SV_TARGET
            {
                float4 sampledLow = SAMPLE_TEXTURE2D_LOD(_BlitSource, sampler_linear_clamp, FIN.screenUV, 0);
                float4 sampledHigh = SAMPLE_TEXTURE2D_LOD(_ExtraSource1, sampler_linear_clamp, FIN.screenUV, 0);

                // Convert it to premultiplied alpha
                return float4(sampledLow.rgb * _BloomIntensity + sampledHigh.rgb,
                              sampledHigh.a);
                    
            }
            ENDHLSL
        }
    }
}