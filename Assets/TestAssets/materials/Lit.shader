Shader "CustomSRP/Lit"
{
    Properties
    {
        _MainTex("Base Color Map", 2D) = "white" {}
        _Tint("Color", Color) = (0.5, 0.5, 0.5, 1)
        [Toggle(_ALPHA_CLIP)] _AlphaClip("Alpha Clip", Float) = 0
        _ClipThreshold("Clip Threshold", Range(0, 1)) = 0.5
        
        _Metallic("Metallic", Range(0, 1)) = 0
        _Smoothness("Smoothness", Range(0, 1)) = 0.5
        
        [Toggle(_ALPHA_ON_SPECULAR)] _AlphaOnSpecular("Alpha On Specular", Float) = 0
        
        // SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 5
        // OneMinusSrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 10

        [Enum(Off, 0, On, 1)] _ZWrite("ZWrite", Float) = 0
        
    }
    
    CustomEditor "CustomSRPShaderEditor"
    
    SubShader
    {
        HLSLINCLUDE
        #define MAX_LIGHT_COUNT 16 
        #define MAX_SHADOWED_LIGHT_COUNT 16
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        float4 _Tint;
        float _ClipThreshold;

        float _Metallic;
        float _Smoothness;
        
        CBUFFER_END
        
        ENDHLSL
        
        Pass {
            Name "Lit CustomLit"
            
            Tags {
                "LightMode" = "CustomLit"
            }
            Blend [_SrcBlend] [_DstBlend], One OneMinusSrcAlpha
            BlendOp Add
            ZWrite [_ZWrite]
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            #pragma shader_feature _ALPHA_CLIP
            #pragma shader_feature _ALPHA_ON_SPECULAR
            
            #pragma multi_compile _SHADOW_PCF2x2 _SHADOW_PCF3x3 _SHADOW_PCF5x5 _SHADOW_PCF7x7

            #include "LitPass.hlsl"
            
            ENDHLSL
        }
        
        Pass {
            Name "Lit ShadowCaster"
            
            Tags {
                "LightMode" = "ShadowCaster"
            }
            ZWrite On
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex ShadowCasterPassVertex
            #pragma fragment ShadowCasterPassFragment

            #pragma shader_feature _ALPHA_CLIP


            #include "ShadowCasterPass.hlsl"

            ENDHLSL
        }
        
        Pass {
            Name "Lit DepthNormal"
            
            Tags {
                "LightMode" = "DepthNormal"
            }
            ZWrite On
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthNormalVertex
            #pragma fragment DepthNormalFragment

            #include "Assets/SRP/Shared/hlsl/DepthNormalPass.hlsl"
            
            ENDHLSL
        }
            
    }
}
