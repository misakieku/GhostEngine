using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.RHI;

/// <summary>
/// The layout of the root signature is:
/// <list space="bullet">
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
    // public const int GLOBAL_BUFFER_SLOT = 0;
    // public const int PER_VIEW_BUFFER_SLOT = 1;
    // public const int PER_OBJECT_BUFFER_SLOT = 2;
    // public const int PER_MATERIAL_BUFFER_SLOT = 3;

    // public const int TEXTURE_HEAP_SLOT = 0;
    // public const int SAMPLER_HEAP_SLOT = 0;

    public const int PUSH_CONSTANT_SLOT = 0;

    public const int ROOT_PARAMETER_COUNT = 1;
}

[StructLayout(LayoutKind.Sequential, Size = 16)]
public struct PushConstantsData
{
    public uint globalIndex;
    public uint viewIndex;
    public uint objectIndex;
    public uint materialIndex;
}

// The size should be 176 bytes (16-byte aligned)
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PerViewData
{
    public float4x4 viewMatrix;
    public float4x4 projectionMatrix;
    public float3 cameraPosition;
    public float nearClip;
    public float3 cameraDirection;
    public float farClip;
    public float4 screenSize; // xy: size, zw: 1/size
};

// The size should be 96 bytes (16-byte aligned)
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PerObjectData
{
    public float4x4 localToWorld;
    public float3 worldBoundsMin;
    public uint vertexBuffer;
    public float3 worldBoundsMax;
    public uint indexBuffer;
};
