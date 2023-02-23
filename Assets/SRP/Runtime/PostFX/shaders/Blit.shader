Shader "CustomSRP/PostFX/Blit"
{
    Properties {}


    SubShader
    {
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        ENDHLSL

        Pass
        {
            Name "Blit"
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
            SAMPLER(sampler_linear_clamp);

            float4 BlitFragment(Varyings FIN) : SV_TARGET
            {
                // return float4(FIN.screenUV.xy, 0.0, 1);
                float2 screenUV = FIN.screenUV;
                float4 copiedSample = SAMPLE_TEXTURE2D_LOD(_BlitSource, sampler_linear_clamp, screenUV, 0);
                return copiedSample;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "Blit AlphaBlend"
            Tags
            {
                "LightMode" = "CustomPostFX"
            }
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            BlendOp Add

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BlitFragment

            #include "PostFX.hlsl"

            TEXTURE2D(_BlitSource);
            SAMPLER(sampler_linear_clamp);

            float4 BlitFragment(Varyings FIN) : SV_TARGET
            {
                // return float4(FIN.screenUV.xy, 0.0, 1);
                float2 screenUV = FIN.screenUV;
                float4 copiedSample = SAMPLE_TEXTURE2D_LOD(_BlitSource, sampler_linear_clamp, screenUV, 0);
                return copiedSample;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "Blit AlphaBlendPreMultiplied"
            Tags
            {
                "LightMode" = "CustomPostFX"
            }
            Cull Off
            ZWrite Off
            Blend One OneMinusSrcAlpha
            BlendOp Add

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BlitFragment

            #include "PostFX.hlsl"

            TEXTURE2D(_BlitSource);
            SAMPLER(sampler_linear_clamp);

            float4 BlitFragment(Varyings FIN) : SV_TARGET
            {
                // return float4(FIN.screenUV.xy, 0.0, 1);
                float2 screenUV = FIN.screenUV;
                float4 copiedSample = SAMPLE_TEXTURE2D_LOD(_BlitSource, sampler_linear_clamp, screenUV, 0);
                return copiedSample;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "Blit Premultiply Alpha"
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
            SAMPLER(sampler_linear_clamp);

            float4 BlitFragment(Varyings FIN) : SV_TARGET
            {
                // return float4(FIN.screenUV.xy, 0.0, 1);
                float2 screenUV = FIN.screenUV;
                float4 copiedSample = SAMPLE_TEXTURE2D_LOD(_BlitSource, sampler_linear_clamp, screenUV, 0);
                return float4(copiedSample.rgb * copiedSample.a, copiedSample.a);
            }
            ENDHLSL
        } 
        
    }
}