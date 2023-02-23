#ifndef CUSTOM_SURFACE_INCLUDED
#define CUSTOM_SURFACE_INCLUDED

struct Surface
{
    float3 positionWS;
    float3 normalWS;
    float3 color;
    float alpha;
    float metallic;
    float smoothness;
};

#endif