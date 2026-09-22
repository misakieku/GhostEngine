#ifndef GHOST_COLOR_HLSL
#define GHOST_COLOR_HLSL

float4 LinearToSRGB(float4 color)
{
    float3 srgb;
    srgb = saturate(color.rgb);
    srgb = pow(srgb, 1.0 / 2.2);
    return float4(srgb, color.a);
}

#endif // GHOST_COLOR_HLSL
