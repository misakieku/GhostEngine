using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.RHI;

public static class RootSignatureLayout
{
    public const int PUSH_CONSTANT_SLOT = 0;

    public const int ROOT_PARAMETER_COUNT = 1;
}

[StructLayout(LayoutKind.Explicit, Size = 12)]
public struct PushConstantsData
{
    public const uint NUM_32BITS_VALUE = 12u / sizeof(uint);

    [FieldOffset(0)]
    public uint frameBuffer;
    [FieldOffset(4)]
    public uint viewBuffer;
    [FieldOffset(8)]
    public uint instanceIndex;
    [FieldOffset(8)]
    public uint propertyBuffer;

    public ReadOnlySpan<uint> AsUInts()
    {
        return MemoryMarshal.CreateReadOnlySpan(ref frameBuffer, (int)NUM_32BITS_VALUE);
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct FrameData
{
    public uint instanceBuffer;
    public uint userBuffer;
    public uint paletteOffsetBuffer;   // bindless index into PaletteOffsetBuffer
    public uint materialIndexBuffer;   // bindless index into MaterialIndexBuffer
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct InstanceData
{
    public float4x4 localToWorld;
    public uint meshBuffer;
    public uint materialPaletteIndex;  // index into PaletteOffsetBuffer (from MaterialPaletteStore)
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct ViewData
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
public struct MeshData
{
    public float3 worldBoundsMin;
    public uint vertexBuffer;
    public float3 worldBoundsMax;
    public uint indexBuffer;

    public uint meshletBuffer;
    public uint meshletVerticesBuffer;
    public uint meshletTrianglesBuffer;
    public uint meshletGroupBuffer;
    public uint meshletHierarchyBuffer;
    public uint meshletCount;
    public uint lodLevelCount;
    public uint materialSlotCount;     // number of material slots baked into this mesh's meshlets
};
