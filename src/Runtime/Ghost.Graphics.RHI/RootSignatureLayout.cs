using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.RHI;

public static class RootSignatureLayout
{
    public const int PUSH_CONSTANT_SLOT = 0;

    public const int ROOT_PARAMETER_COUNT = 1;
}

[StructLayout(LayoutKind.Sequential, Size = 20)]
public struct PushConstantsData
{
    public uint globalIndex;
    public uint viewIndex;
    public uint objectIndex;
    public uint instanceIndex;
    public uint materialIndex;
}

[StructLayout(LayoutKind.Sequential, Size = 20)]
public struct GlobalFrameData
{
    public uint viewBufferIndex;
    public uint instanceBufferIndex;
    public uint viewBufferCount;
    public uint instanceBufferCount;
    public uint userBufferIndex;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct InstanceData
{
    public float4x4 localToWorld;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
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

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct PerObjectData
{
    public float3 worldBoundsMin;
    public uint vertexBuffer;
    public float3 worldBoundsMax;
    public uint indexBuffer;

    public uint meshletBuffer;
    public uint meshletVerticesBuffer;
    public uint meshletTrianglesBuffer;
};
