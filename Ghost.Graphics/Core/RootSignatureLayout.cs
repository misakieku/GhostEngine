using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.Core;

/// <summary>
/// The layout of the root signature is:
/// <list type="bullet">
/// <item>
/// Global buffer (b0)
/// </item>
/// <item>
/// Per-view buffer (b1)
/// </item>
/// <item>
/// Per-object buffer (b2)
/// </item>
/// <item>
/// Per-material buffer (b3)
/// </item>
/// <item>
/// Descriptor table for bindless textures (t0)
/// </item>
/// <item>
/// Descriptor table for bindless samplers (s0)
/// </item>
/// </list>
/// </summary>
public static class RootSignatureLayout
{
    public const int GLOBAL_BUFFER_SLOT = 0;
    public const int PER_VIEW_BUFFER_SLOT = 1;
    public const int PER_OBJECT_BUFFER_SLOT = 2;
    public const int PER_MATERIAL_BUFFER_SLOT = 3;

    public const int TEXTURE_HEAP_SLOT = 0;
    public const int SAMPLER_HEAP_SLOT = 0;

    public const int ROOT_PARAMETER_COUNT =
#if USE_TRADITIONAL_BINDLESS
    6
#else
    4
#endif
    ;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PerObjectData
{
    public float4x4 localToWorld;
    public float3 worldBoundsMin;
    public uint vertexBuffer;
    public float3 worldBoundsMax;
    public uint indexBuffer;
};