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

// 6th-order polynomial approximation of the AgX 1D contrast sigmoid curve
float3 AgxDefaultContrastApprox(float3 x)
{
    float3 x2 = x * x;
    float3 x4 = x2 * x2;
    return 15.5 * (x4 * x2) - 40.14 * (x4 * x) + 31.96 * x4 - 6.868 * (x2 * x) + 0.4298 * x2 + 0.1191 * x - 0.00232;
}

// Applies AgX tonemapping to a linear sRGB HDR color.
// Returns display-referred sRGB color in [0, 1].
float3 AgxToneMapping(float3 color, bool punchy = false)
{
    // Row-major matrix definitions for HLSL mul(M, v)
    // Inset matrix: compresses primaries inwards into AgX working space
    static const float3x3 AGX_INSET_MATRIX = float3x3(
        0.842479065, 0.078433599, 0.079223727,
        0.042328242, 0.878468014, 0.079161666,
        0.042375655, 0.078433604, 0.879142973
    );
    
    // Outset matrix: maps from AgX space back to Rec.709 display space
    static const float3x3 AGX_OUTSET_MATRIX = float3x3(
         1.19687900, -0.09802088, -0.09902974,
        -0.05289685,  1.15190313, -0.09896161,
        -0.05297163, -0.09804045,  1.15107367
    );
    
    // Input matrix transform + clamp against non-positive values
    float3 val = max(mul(AGX_INSET_MATRIX, color), 1e-6);
    
    // Log2 encoding (-12.47393 EV to +6.49511 EV mapped to [0, 1])
    const float minEV = -12.47393;
    const float maxEV =  6.49511;
    val = saturate((log2(val) - minEV) / (maxEV - minEV));
    
    val = AgxDefaultContrastApprox(val);
    
    // ASC-CDL: uniform power 1.35 + 1.4 saturation
    if (punchy)
    {
        val = pow(saturate(val), 1.35);
        const float3 lumaWeights = float3(0.2126, 0.7152, 0.0722);
        float luma = dot(val, lumaWeights);
        val = luma + 1.4 * (val - luma);
    }
    
    // Inverse matrix transform (outset) back to display space
    val = mul(AGX_OUTSET_MATRIX, val);
    return saturate(val);
}

#endif // GHOST_COLOR_HLSL
