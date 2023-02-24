Shader "CustomSRP/PostFX/ScreenSpaceAO"
{
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    struct Attributes
    {
        float4 positionHCS : POSITION;
        float2 uv : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 uv : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings VertDefault(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

        // Note: The pass is setup with a mesh already in CS
        // Therefore, we can just output vertex position
        output.positionCS = float4(input.positionHCS.xyz, 1.0);

        #if UNITY_UV_STARTS_AT_TOP
        output.positionCS.y *= _ScaleBiasRt.x;
        #endif

        output.uv = input.uv;

        // Add a small epsilon to avoid artifacts when reconstructing the normals
        output.uv += 1.0e-6;

        return output;
    }

    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
        }

        Cull Off ZWrite Off ZTest Always

        // ------------------------------------------------------------------
        // Depth only passes
        // ------------------------------------------------------------------

        // 0 - Occlusion estimation with CameraDepthTexture
        Pass
        {
            Name "SSAO_Occlusion"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment SSAO
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_local _SOURCE_DEPTH _SOURCE_DEPTH_NORMALS
            #pragma multi_compile_local _RECONSTRUCT_NORMAL_LOW _RECONSTRUCT_NORMAL_MEDIUM _RECONSTRUCT_NORMAL_HIGH
            #pragma multi_compile_local _ _ORTHOGRAPHIC

            #include "Assets/SRP/Runtime/PostFX/shaders/ScreenSpaceAO.hlsl"
            ENDHLSL
        }

        // 1 - Horizontal Blur
        Pass
        {
            Name "SSAO_HorizontalBlur"

            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment HorizontalBlur
                #define BLUR_SAMPLE_CENTER_NORMAL
                #pragma multi_compile_local _ _ORTHOGRAPHIC
                #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
                #pragma multi_compile_local _SOURCE_DEPTH _SOURCE_DEPTH_NORMALS
                #include "Assets/SRP/Runtime/PostFX/shaders/ScreenSpaceAO.hlsl"
            ENDHLSL
        }

        // 2 - Vertical Blur
        Pass
        {
            Name "SSAO_VerticalBlur"

            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment VerticalBlur
                #include "Assets/SRP/Runtime/PostFX/shaders/ScreenSpaceAO.hlsl"
            ENDHLSL
        }

        // 3 - Final Blur
        Pass
        {
            Name "SSAO_FinalBlur"

            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment FinalBlur
                #include "Assets/SRP/Runtime/PostFX/shaders/ScreenSpaceAO.hlsl"
            ENDHLSL
        }

        // 4 - Combine
        Pass
        {
            Name "SSAO_Combine"

            Cull Off
            ZWrite Off

            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment FragCombine
                #define _SCREEN_SPACE_OCCLUSION

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

                TEXTURE2D(_BlitSource);
                SAMPLER(sampler_linear_clamp);

                half4 FragCombine(Varyings input) : SV_Target
                {
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                    AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(input.uv);
                    half occlusion = aoFactor.indirectAmbientOcclusion;
                    float4 copiedSample = SAMPLE_TEXTURE2D_LOD(_BlitSource, sampler_linear_clamp, input.uv, 0);
                    // return half4(aoFactor.directAmbientOcclusion, aoFactor.indirectAmbientOcclusion, 0, 1);
                    // return copiedSample;
                    return lerp(copiedSample, half4(0, 0, 0, 1), 1 - occlusion);
                    return half4(0.0, 0.0, 0.0, occlusion);
                }

            ENDHLSL
        }
    }
}