#ifndef CUSTOM_LIT_PASS_INCLUDED
#define CUSTOM_LIT_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Surface.hlsl"


TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

// For debugging
SAMPLER(sampler_linear_clamp);

int _LightCount;
float _VisibleLightToShadowedLightIndices[MAX_LIGHT_COUNT];
float _LightTypes[MAX_LIGHT_COUNT]; // 0 = Spot, 1 = Directional, 2 = Point
float4 _LightColors[MAX_LIGHT_COUNT];
float4 _LightDirections[MAX_LIGHT_COUNT];
float4 _LightPositions[MAX_LIGHT_COUNT];
float4 _LightSpotAngles[MAX_LIGHT_COUNT]; // x = cos(inner / 2), y = cos(outer / 2)

TEXTURE2D_SHADOW(_ShadowAtlas);
SAMPLER_CMP(sampler_ShadowAtlas);

int _ShadowedLightCount;
float4x4 _WorldToLightClipMatrices[MAX_SHADOWED_LIGHT_COUNT];
float4 _ShadowTileSizesAndOffsets[MAX_SHADOWED_LIGHT_COUNT];
float _ShadowStrengths[MAX_SHADOWED_LIGHT_COUNT];
float _NormalBiases[MAX_SHADOWED_LIGHT_COUNT];
float4 _ShadowColors[MAX_SHADOWED_LIGHT_COUNT];

float4 _ShadowAtlasSize; // x = width, y = height, z = 1 / width, w = 1 / height

#include "Lighting.hlsl"
#include "Shadowing.hlsl"

        
struct Attributes
{
    float3 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float4 uv           : TEXCOORD0;
};

struct Varyings
{
    float3 positionWS   : VAR_SV_POSITION;
    // WARNING: Don't use GetNormalizedScreenSpaceUV(positionCS). It's wrong for unknown reasons.
    float4 positionCS   : SV_POSITION;
    float4 positionNDC  : VAR_POSITION_NDC;
    float3 normalWS     : NORMAL;
    float2 uv           : TEXCOORD0;
};


Varyings LitPassVertex(Attributes VIN)
{
    Varyings VOUT;
    VertexPositionInputs vertexPositionInput = GetVertexPositionInputs(VIN.positionOS);
    VertexNormalInputs normalInput = GetVertexNormalInputs(VIN.normalOS, VIN.tangentOS);
    VOUT.positionCS = vertexPositionInput.positionCS;
    VOUT.positionWS = vertexPositionInput.positionWS;
    VOUT.positionNDC = vertexPositionInput.positionNDC;
    VOUT.normalWS = normalInput.normalWS;
    VOUT.uv = TRANSFORM_TEX(VIN.uv, _MainTex);
    return VOUT;
}


float4 LitPassFragment(Varyings FIN) : SV_TARGET
{
    float3 N = normalize(FIN.normalWS);
    float4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, FIN.uv);
    float2 screenUV = FIN.positionNDC.xy / FIN.positionNDC.w;
    
#if defined(_ALPHA_CLIP)
    clip(baseColor.a - _ClipThreshold);
    baseColor.a = 1;
#endif

    Surface surface;
    surface.positionWS = FIN.positionWS;
    surface.color = baseColor.rgb * _Tint.rgb;
    surface.alpha = baseColor.a * _Tint.a;
    surface.normalWS = N;
    surface.metallic = _Metallic;
    surface.smoothness = _Smoothness;
    surface.metallic = 0;
    float3 result = float3(0, 0, 0);
    
    for(int i = 0; i < _LightCount; i++)
    {
        float3 outLighting = CalculateLighting(i, surface);
        int i2 = int(_VisibleLightToShadowedLightIndices[i]);
        float4 shadow = i2 == -1 ? float4(1, 1, 1, 0) : SampleShadow(i2, surface);
        outLighting = lerp(outLighting, outLighting * shadow.rgb, shadow.a);
        result += outLighting;
    }
    /*
    // This is a highly stylized shadow. Basically we merge all shadows into one.
    for(int i2 = 0; i2 < _ShadowedLightCount; i2++)
    {
        float4 shadow = SampleShadow(i2, surface);
        outLighting = lerp(outLighting, outLighting * shadow.rgb, shadow.a);
    }
    */

    return float4(result, surface.alpha);

    
}


#endif