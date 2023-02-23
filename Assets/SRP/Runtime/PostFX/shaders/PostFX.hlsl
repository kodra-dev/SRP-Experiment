#ifndef CUSTOM_POST_FX_INCLUDED
#define CUSTOM_POST_FX_INCLUDED



struct Attributes
{
    float3 positionOS   : POSITION;
};

struct Varyings {
    float4 positionCS : SV_POSITION;
    // NOTE: positionCS can't be used in fragment shader, so we need to pass screenUV.
    float2 screenUV : VAR_SCREEN_UV;
};


Varyings DefaultPassVertex (Attributes VIN)
{
    Varyings VOUT;
    float4 positionCS = float4(VIN.positionOS.xy, 0.0, 1.0);
    VOUT.positionCS = positionCS;
    VOUT.screenUV = positionCS.xy * 0.5 + 0.5;
    if (_ProjectionParams.x < 0.0) {
         VOUT.screenUV.y = 1.0 - VOUT.screenUV.y;
    }
    return VOUT;
}




#endif
