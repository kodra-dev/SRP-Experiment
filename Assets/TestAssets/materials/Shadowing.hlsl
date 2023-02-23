#ifndef CUSTOM_SHADOWING_INCLUDED
#define CUSTOM_SHADOWING_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

#include "Assets/SRP/Shared/hlsl/CommonMath.hlsl"

#if defined(_SHADOW_PCF3x3)
    #define PCF_FILTER_SAMPLES 4
    #define PCF_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_SHADOW_PCF5x5)
    #define PCF_FILTER_SAMPLES 9
    #define PCF_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_SHADOW_PCF7x7)
    #define PCF_FILTER_SAMPLES 16
    #define PCF_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

float4 SampleShadow(int shadowedLightIndex, Surface surface)
{
    float3 positionWS = surface.positionWS;
    float3 normalWS = surface.normalWS;
    float3 normalBias = normalWS * _NormalBiases[shadowedLightIndex];
    
    float4 positionLCS = mul(_WorldToLightClipMatrices[shadowedLightIndex], float4(positionWS + normalBias, 1.0));
    float4 shadowUV = float4(positionLCS.xyz / positionLCS.w, 1);
    shadowUV.xyz = shadowUV.xyz * 0.5 + 0.5;

    // return float4(shadowUV.xy, 0, 1);

    if(shadowUV.x < 0 || shadowUV.x > 1 || shadowUV.y < 0 || shadowUV.y > 1)
    {
        // Outside of shadow tile of this light
        return float4(1, 0, 1, 1);
    }
    
    float tileSizeX = _ShadowTileSizesAndOffsets[shadowedLightIndex].x;
    float tileSizeY = _ShadowTileSizesAndOffsets[shadowedLightIndex].y;
    float offsetX = _ShadowTileSizesAndOffsets[shadowedLightIndex].z;
    float offsetY = _ShadowTileSizesAndOffsets[shadowedLightIndex].w;

    shadowUV.x = shadowUV.x * tileSizeX + offsetX;
    shadowUV.y = shadowUV.y * tileSizeY + offsetY;
    // float depthFromShadowMap = SAMPLE_TEXTURE2D(_ShadowAtlas, sampler_linear_clamp, shadowUV.xy).r;
    #if defined(UNITY_REVERSED_Z)
        // depthFromShadowMap = 1 - depthFromShadowMap;
        // The one got reversed is depthFromShadowMap, not shadowUV.z
        // But we need to reverse shadowUV.z to use SAMPLE_TEXTURE2D_SHADOW
        shadowUV.z = 1 - shadowUV.z;
    #endif

    #if defined(PCF_FILTER_SETUP)
        float shadow = 0;
        float weights[PCF_FILTER_SAMPLES];
        float2 positions[PCF_FILTER_SAMPLES];
        float4 texelSize = _ShadowAtlasSize.zwxy; // texelSize.x = 1 / width, y = 1 / height, z = width, w = height
    
        PCF_FILTER_SETUP(texelSize, shadowUV.xy, weights, positions);
    
        float pre_noise_pattern_u[16] = {
            0.37685474, 0.85929169, 0.24575292, 0.72042299, 0.50139853, 0.30918004, 0.43760467, 0.6054144,
            0.12422273, 0.07708602, 0.93907091, 0.84674924, 0.6919152, 0.73699852, 0.90183272, 0.44844411
        };
        float pre_noise_pattern_v[16] = {
            0.28197131, 0.15995012, 0.32384344, 0.96962336, 0.00895075, 0.89763415, 0.70124823, 0.20560771,
            0.67901062, 0.98391764, 0.51227307, 0.47654018, 0.31293511, 0.17206599, 0.12676289, 0.32038124
        };
    
        for (int i = 0; i < PCF_FILTER_SAMPLES; i++) {
            // float2 randomOffset = float2(
            //     unity_noise_randomValue(positions[i].xy),
            //     unity_noise_randomValue(positions[i].yx));
            //
            float3 positionWS = surface.positionWS;
            int indexU = (int)(16.0*random4(floor(float4(positionWS*1000, i))));
            int indexV = (int)(16.0*random4(floor(float4(positionWS.yxz*1000, i))));
            // offsetIndex = int2(i, i);
            float2 randomOffset = float2(
                pre_noise_pattern_u[indexU],
                pre_noise_pattern_v[indexV]);

            // noisy anti aliasing
            // positions[i].xy += (randomOffset - 0.5) * 10 * texelSize.xy;
            shadow += weights[i]
                * SAMPLE_TEXTURE2D_SHADOW(_ShadowAtlas, sampler_ShadowAtlas, float3(positions[i].xy, shadowUV.z));
        }
    #else
        float shadow = SAMPLE_TEXTURE2D_SHADOW(_ShadowAtlas, sampler_ShadowAtlas, shadowUV);
    #endif
    
    shadow = 1 - shadow;
    shadow = shadow * _ShadowStrengths[shadowedLightIndex];
    return float4(_ShadowColors[shadowedLightIndex].rgb, shadow);
}

#endif
