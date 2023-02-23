#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

#include "Assets/SRP/Shared/hlsl/CommonMath.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

float3 BRDF(float3 lightColor, float3 lightDirection, float3 viewDirection, Surface surface)
{
    float roughness = PerceptualSmoothnessToRoughness(surface.smoothness);
    const float MIN_REFLECTIVITY = 0.04;
    float reflectivity = remap(0, 1, MIN_REFLECTIVITY, 1, surface.metallic);
    float diffusePortion = 1 - reflectivity;
    float specularPortion = reflectivity;
    float3 diffuseColor = diffusePortion * lightColor * surface.color;
    float3 specularColor = specularPortion * lightColor * lerp((float3)1, surface.color, surface.metallic);

    // https://catlikecoding.com/unity/tutorials/custom-srp/directional-lights/#3.9
    float3 h = normalize(lightDirection + viewDirection);
    float d = Square(saturate(dot(surface.normalWS, h))) * (Square(roughness) - 1) + 1.001;
    float n = roughness * 4 + 2;
    float r2 = Square(roughness);
    float d2 = Square(d);
    float specularStrength = r2 / (d2 * max(0.1, Square(saturate(dot(lightDirection, h)))) * n);

    float diffuseStrength = saturate(dot(surface.normalWS, lightDirection));
    // Reduce specular strength so the back of the object is not shiny
    specularStrength *= pow(diffuseStrength, 0.25);

    #if !defined(_ALPHA_ON_SPECULAR)
    // Premultiplied alpha on diffuse
    diffuseColor *= surface.alpha;
    #endif

    // return specularStrength;
    // return lightColor * surface.color * diffuseStrength;
    return diffuseColor * diffuseStrength + specularColor * specularStrength;
}


// Type: 0 = Spot, 1 = Directional, 2 = Point

float3 CalculateLighting(int lightIndex, Surface surface)
{
    int i = lightIndex;
    float3 lightAttenuation = 1;
    float3 surfaceToLight = _LightPositions[i].xyz - surface.positionWS;
    float3 L = normalize(surfaceToLight);
    float type = _LightTypes[i];
    float distAttenuation = type == 1 ?
        1 :
        1 / max(dot(surfaceToLight, surfaceToLight), 0.0001);
    lightAttenuation *= distAttenuation;
    
    float d = dot(_LightDirections[i].xyz, L);
    float spotAttenuation = type != 0 ?
        1 :
        Square(
        saturate((d - _LightSpotAngles[i].y) / (_LightSpotAngles[i].x - _LightSpotAngles[i].y)));
    
    lightAttenuation *= spotAttenuation;
    
    float3 lightColor = _LightColors[i].xyz;
    // Assume light direction is normalized
    lightColor *= lightAttenuation;

    // return lightColor;
    float3 lightDirection = type == 1 ?
        _LightDirections[i].xyz :
        L;
    float3 viewDirection = normalize(_WorldSpaceCameraPos - surface.positionWS);
    float3 outLighting = BRDF(lightColor, lightDirection,
                              viewDirection,
                              surface);
    return outLighting;
}


#endif
