#ifndef GHOST_COLOR_HLSL
#define GHOST_COLOR_HLSL

float LinearToSRGB(float color)
{
    return (color <= 0.0031308) ? (color * 12.9232102) : 1.055 * pow(color, 1.0 / 2.4) - 0.055;
}

float2 LinearToSRGB(float2 color)
{
    return float2(LinearToSRGB(color.x), LinearToSRGB(color.y));
}

float3 LinearToSRGB(float3 color)
{
    return float3(LinearToSRGB(color.r), LinearToSRGB(color.g), LinearToSRGB(color.b));
}

float4 LinearToSRGB(float4 color)
{
    return float4(LinearToSRGB(color.rgb), color.a);
}

float3 FastLinearToSRGB(float3 color)
{
    return saturate(1.055 * pow(color, 0.416666667) - 0.055);
}

float4 FastLinearToSRGB(float4 color)
{
    float3 srgb = saturate(1.055 * pow(color.rgb, 0.416666667) - 0.055);
    return float4(srgb, color.a);
}

#endif // GHOST_COLOR_HLSL
