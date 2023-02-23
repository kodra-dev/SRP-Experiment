#ifndef CUSTOM_UNLIT_PASS
#define CUSTOM_UNLIT_PASS

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

struct Attributes
{
    float3 positionOS   : POSITION;
    float4 uv           : TEXCOORD0;
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float2 uv           : TEXCOORD0;
};


Varyings UnlitPassVertex(Attributes VIN)
{
    const VertexPositionInputs vertexPositionInput = GetVertexPositionInputs(VIN.positionOS);
    Varyings VOUT;
    VOUT.positionCS = vertexPositionInput.positionCS;
    VOUT.uv = TRANSFORM_TEX(VIN.uv, _MainTex);
    return VOUT;
}


float4 UnlitPassFragment(Varyings IN) : SV_TARGET
{
    
    float4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
    
#if defined(_ALPHA_CLIP)
    clip(baseColor.a - _ClipThreshold);
    baseColor.a = 1;
#endif
    
    return baseColor * _Tint;
}


#endif