#ifndef CUSTOM_SHADOW_CASTER_PASS_INCLUDED
#define CUSTOM_SHADOW_CASTER_PASS_INCLUDED

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

Varyings ShadowCasterPassVertex(Attributes VIN)
{
    Varyings VOUT;
    VertexPositionInputs vertexPositionInput = GetVertexPositionInputs(VIN.positionOS);
    VOUT.positionCS = vertexPositionInput.positionCS;
    VOUT.uv = TRANSFORM_TEX(VIN.uv, _MainTex);
    return VOUT;
}

void ShadowCasterPassFragment(Varyings FIN)
{
    float4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, FIN.uv);
    #if defined(_ALPHA_CLIP)
    clip(baseColor.a - _ClipThreshold);
    #endif
}


#endif 