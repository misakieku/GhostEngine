using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;

namespace Ghost.Engine.RenderPipeline;

/// <summary>
/// GPU-ready memory layout for punctual (point/spot) lights.
/// Exactly 64 bytes (four float4 vectors), matching 1 GPU L1 cache line.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct GPUPunctualLight
{
    // Vector 0: Position & Range (16 bytes)
    public float3 positionWS;
    public float range;

    // Vector 1: Color, Intensity & Type Flags (16 bytes)
    public float3 color;
    public uint lightTypeAndFlags; // bits 0..3: type (0=Point, 1=Spot), bits 4..31: flags

    // Vector 2: Spot Direction & Cone Scale (16 bytes)
    public float3 directionWS;
    public float spotAngleScale; // 1.0 / max(0.001, cosInner - cosOuter)

    // Vector 3: Cone Offset, Range Attenuation, Shadows & Radius (16 bytes)
    public float spotAngleOffset; // -cosOuter * spotAngleScale
    public float invRangeSq;      // 1.0 / (range * range)
    public int shadowIndex;       // Index into shadow atlas / array (-1 if unshadowed)
    public float sourceRadius;    // Specular GGX normalization / contact shadow radius
}

/// <summary>
/// GPU-ready memory layout for the primary directional light (Sun/Moon).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct GPUDirectionalLight
{
    public float3 directionWS;
    public uint castShadows;
    public float3 color;
    public float shadowBiasMultiplier;
    public float4 cascadeSplits;   // View-space Z split planes for 4 CSM cascades
    public float4x4 shadowMatrix0; // CSM Cascade 0 View-Projection
    public float4x4 shadowMatrix1; // CSM Cascade 1 View-Projection
    public float4x4 shadowMatrix2; // CSM Cascade 2 View-Projection
    public float4x4 shadowMatrix3; // CSM Cascade 3 View-Projection
}
