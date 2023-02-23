#ifndef CUSTOM_DEPTH_NORMAL_PASS_INCLUDED
#define CUSTOM_DEPTH_NORMAL_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        
struct Attributes
{
    float3 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
};

struct Varyings
{
    float3 positionWS   : VAR_SV_POSITION;
    // WARNING: Don't use GetNormalizedScreenSpaceUV(positionCS). It's wrong for unknown reasons.
    float4 positionCS   : SV_POSITION;
    float3 positionVS   : VAR_POSITION_VS;
    float4 positionNDC  : VAR_POSITION_NDC;
    float3 normalWS     : VAR_NORMAL;
    float3 normalVS     : VAR_NORMAL_VS;
};


Varyings DepthNormalVertex(Attributes VIN)
{
    Varyings VOUT;
    VertexPositionInputs vertexPositionInput = GetVertexPositionInputs(VIN.positionOS);
    VertexNormalInputs normalInput = GetVertexNormalInputs(VIN.normalOS, VIN.tangentOS);
    VOUT.positionCS = vertexPositionInput.positionCS;
    VOUT.positionWS = vertexPositionInput.positionWS;
    VOUT.positionVS = vertexPositionInput.positionVS;
    VOUT.positionNDC = vertexPositionInput.positionNDC;
    VOUT.normalWS = normalInput.normalWS;
    VOUT.normalVS = normalize(mul((float3x3)UNITY_MATRIX_V, VOUT.normalWS));
    return VOUT;
}



float4 DepthNormalFragment(Varyings FIN) : SV_TARGET
{
    // return 0;
    return float4(NormalizeNormalPerPixel(FIN.normalWS), 0.0);
}



#endif