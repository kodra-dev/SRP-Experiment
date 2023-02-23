Shader "CustomSRP/Unlit"
{
    Properties
    {
        _MainTex("Base Color Map", 2D) = "white" {}
        _Tint("Color", Color) = (1,1,1,1)
        [Toggle(_ALPHA_CLIP)] _AlphaClip("Alpha Clip", Float) = 0
        _ClipThreshold("Clip Threshold", Range(0, 1)) = 0.5
        [Enum(Off, 0, On, 1)] _ZWrite("ZWrite", Float) = 0
    }
    SubShader
    {
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        float4 _Tint;
        float _ClipThreshold;
        CBUFFER_END
        
        ENDHLSL
        
        Pass {
            
            Tags
            {
                "LightMode" = "CustomUnlit"
            }

            Blend SrcAlpha OneMinusSrcAlpha
            BlendOp Add
            ZWrite [_ZWrite]
            
            HLSLPROGRAM

            #pragma vertex UnlitPassVertex
            #pragma fragment UnlitPassFragment

            #pragma shader_feature _ALPHA_CLIP

            #include "UnlitPass.hlsl"
            
            ENDHLSL
        }
    }
}
