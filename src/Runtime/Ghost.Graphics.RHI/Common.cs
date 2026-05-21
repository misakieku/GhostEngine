using Ghost.Core;
using Ghost.Core.Graphics;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.RHI;

/// <summary>
/// Represents a color with 4 bytes components.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 4)]
public struct Color32 : IEquatable<Color32>
{
    public byte r;
    public byte g;
    public byte b;
    public byte a;

    public Color32(byte r, byte g, byte b, byte a)
    {
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }

    public Color32(Color color)
        : this(color.R, color.G, color.B, color.A)
    {
    }

    public Color32(Color128 color128)
        : this((byte)(color128.r * 255.0f), (byte)(color128.g * 255.0f), (byte)(color128.b * 255.0f), (byte)(color128.a * 255.0f))
    {
    }

    public Color32(float4 v)
        : this((byte)(v.x * 255.0f), (byte)(v.y * 255.0f), (byte)(v.z * 255.0f), (byte)(v.w * 255.0f))
    {
    }

    public readonly bool Equals(Color32 other)
    {
        return r == other.r && g == other.g && b == other.b && a == other.a;
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is Color32 color && Equals(color);
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(r, g, b, a);
    }

    public static bool operator ==(Color32 left, Color32 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Color32 left, Color32 right)
    {
        return !(left == right);
    }
}

/// <summary>
/// Represents a color with 16 bytes components.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 16)]
public struct Color128 : IEquatable<Color128>
{
    public float r;
    public float g;
    public float b;
    public float a;

    public Color128(float r, float g, float b, float a)
    {
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }

    public Color128(Color color)
        : this(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f)
    {
    }

    public Color128(Color32 color32)
        : this(color32.r / 255.0f, color32.g / 255.0f, color32.b / 255.0f, color32.a / 255.0f)
    {
    }

    public Color128(float4 v)
        : this(v.x, v.y, v.z, v.w)
    {
    }

    public readonly bool Equals(Color128 other)
    {
        return r.Equals(other.r) && g.Equals(other.g) && b.Equals(other.b) && a.Equals(other.a);
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is Color128 color && Equals(color);
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(r, g, b, a);
    }

    public static bool operator ==(Color128 left, Color128 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Color128 left, Color128 right)
    {
        return !(left == right);
    }
}


[StructLayout(LayoutKind.Sequential)]
public record struct Vertex
{
    public static class Semantic
    {
        public const int COUNT = 5;

        public static readonly FixedText32 Position = new("POSITION"u8);
        public static readonly FixedText32 Normal = new("NORMAL"u8);
        public static readonly FixedText32 Tangent = new("TANGENT"u8);
        public static readonly FixedText32 Uv = new("TEXCOORD"u8);
        public static readonly FixedText32 Color = new("COLOR"u8);
    }

    public Color128 color;
    public float4 tangent;
    public float3 position;
    public float3 normal;
    public float2 uv;
}

public struct ResourceRange
{
    public nuint Start
    {
        get; set;
    }

    public nuint End
    {
        get; set;
    }
}

public readonly struct ShaderVariant;

public readonly struct ShaderPass
{
    public Key64<ShaderPass> Key
    {
        get; init;
    }

    public PipelineState DefaultState
    {
        get; init;
    }

    public LocalKeywordSet DefinedKeywords
    {
        get; init;
    }
}

public readonly struct PassAttachmentHash : IEquatable<PassAttachmentHash>
{
    public readonly UInt128 value;

    public PassAttachmentHash(ReadOnlySpan<TextureFormat> rtvFormats, TextureFormat dsvFormat)
    {
        if (rtvFormats.Length > 8)
        {
            throw new ArgumentException($"RTV formats length exceeds maximum supported count of {8}.");
        }

        // layout:
        // 0..64 8 RTV formats (8 bits each)
        // 64..72 DSV format (8 bits)

        var rtvPart = 0UL;
        for (var i = 0; i < rtvFormats.Length; i++)
        {
            rtvPart |= ((ulong)(byte)rtvFormats[i]) << (i * 8);
        }

        value = new UInt128(rtvPart, (ulong)dsvFormat);
    }

    public bool Equals(PassAttachmentHash other) => value == other.value;
    public override bool Equals(object? obj) => obj is PassAttachmentHash other && Equals(other);
    public override int GetHashCode() => value.GetHashCode();

    public static bool operator ==(PassAttachmentHash left, PassAttachmentHash right) => left.Equals(right);
    public static bool operator !=(PassAttachmentHash left, PassAttachmentHash right) => !(left == right);
}

public ref struct GraphicsPSODesc
{
    public ulong CompiledHash
    {
        get; set;
    }

    public Key64<ShaderVariant> VariantKey
    {
        get; set;
    }

    public PipelineState PipelineOption
    {
        get; set;
    }

    public ReadOnlySpan<TextureFormat> RtvFormats
    {
        get; set;
    }

    public TextureFormat DsvFormat
    {
        get; set;
    }

    public ReadOnlySpan<byte> AsCode
    {
        get; set;
    }

    public ReadOnlySpan<byte> MsCode
    {
        get; set;
    }

    public ReadOnlySpan<byte> PsCode
    {
        get; set;
    }
}

public ref struct ComputePSODesc
{
    public ulong CompiledHash
    {
        get; set;
    }

    public Key64<ShaderVariant> VariantKey
    {
        get; set;
    }

    public ReadOnlySpan<byte> CsCode
    {
        get; set;
    }
}

public readonly struct CBufferPropertyInfo
{
    public string Name
    {
        get; init;
    }

    public uint StartOffset
    {
        get; init;
    }

    public uint Size
    {
        get; init;
    }
}

public readonly struct CBufferInfo
{
    public string Name
    {
        get; init;
    }

    public uint RegisterSlot
    {
        get; init;
    }

    public uint RegisterSpace
    {
        get; init;
    }

    public uint SizeInBytes
    {
        get; init;
    }

    public IReadOnlyList<CBufferPropertyInfo>? Properties
    {
        get; init;
    }
}

public struct RenderDesc
{
    public float4x4 ViewMatrix
    {
        get; set;
    }

    public float4x4 ProjectionMatrix
    {
        get; set;
    }

    public float4 CameraPosition
    {
        get; set;
    }

    // The "Target" (Where to write pixels)
    public Handle<GPUTexture> Target
    {
        get; set;
    }

    public Handle<GPUTexture> DepthTarget
    {
        get; set;
    }

    public ScissorRectDesc Viewport
    {
        get; set;
    }

    //public RenderPathID RenderPath;
    //public LayerMask CullingMask;
}

public struct ViewportDesc
{
    public float X
    {
        get; set;
    }

    public float Y
    {
        get; set;
    }

    public float Width
    {
        get; set;
    }

    public float Height
    {
        get; set;
    }

    public float MinDepth
    {
        get; set;
    }

    public float MaxDepth
    {
        get; set;
    }

}

public struct ScissorRectDesc
{
    public uint Left
    {
        get; set;
    }

    public uint Top
    {
        get; set;
    }

    public uint Right
    {
        get; set;
    }

    public uint Bottom
    {
        get; set;
    }
}

public struct SubResourceData
{
    public unsafe void* pData;
    public uint rowPitch;
    public uint slicePitch;
}

public struct PassRenderTargetDesc
{
    public Handle<GPUTexture> Texture
    {
        get; set;
    }

    public Color128 ClearColor
    {
        get; set;
    }

    /// <summary>
    /// Specifies how to load the render target at the start of the render pass.
    /// </summary>
    public AttachmentLoadOp LoadOp
    {
        get; set;
    }

    /// <summary>
    /// Specifies how to store the render target at the end of the render pass.
    /// </summary>
    public AttachmentStoreOp StoreOp
    {
        get; set;
    }

}

public struct PassDepthStencilDesc
{
    public Handle<GPUTexture> Texture
    {
        get; set;
    }

    public float ClearDepth
    {
        get; set;
    }

    public byte ClearStencil
    {
        get; set;
    }

    /// <summary>
    /// Specifies how to load the depth buffer at the start of the render pass.
    /// </summary>
    public AttachmentLoadOp DepthLoadOp
    {
        get; set;
    }

    /// <summary>
    /// Specifies how to store the depth buffer at the end of the render pass.
    /// </summary>
    public AttachmentStoreOp DepthStoreOp
    {
        get; set;
    }

    /// <summary>
    /// Specifies how to load the stencil buffer at the start of the render pass.
    /// </summary>
    public AttachmentLoadOp StencilLoadOp
    {
        get; set;
    }

    /// <summary>
    /// Specifies how to store the stencil buffer at the end of the render pass.
    /// </summary>
    public AttachmentStoreOp StencilStoreOp
    {
        get; set;
    }

    public bool HasStencil
    {
        get; set;
    }
}

public struct TextureSubresource
{
    public uint MipLevel
    {
        get; set;
    }

    public uint ArrayLayer
    {
        get; set;
    }
}

public struct TextureRegion
{
    public TextureSubresource Subresource
    {
        get; set;
    }

    public uint X
    {
        get; set;
    }

    public uint Y
    {
        get; set;
    }

    public uint Z
    {
        get; set;
    }

    public uint Width
    {
        get; set;
    }

    public uint Height
    {
        get; set;
    }

    public uint Depth
    {
        get; set;
    }
}


public struct BarrierSubresourceRange
{
    public uint IndexOrFirstMipLevel
    {
        get; set;
    }

    public uint NumMipLevels
    {
        get; set;
    }

    public uint FirstArraySlice
    {
        get; set;
    }

    public uint NumArraySlices
    {
        get; set;
    }
}

public struct BarrierDesc
{
    public BarrierType Type
    {
        get; set;
    }

    public BarrierSync SyncAfter
    {
        get; set;
    }

    public BarrierAccess AccessAfter
    {
        get; set;
    }

    public BarrierLayout LayoutAfter
    {
        get; set;
    }

    public Handle<GPUResource> Resource
    {
        get; set;
    }

    public BarrierSubresourceRange Subresources
    {
        get; set;
    }

    public bool Discard
    {
        get; set;
    }

    public bool IsAliasing
    {
        get; set;
    }

    public static BarrierDesc Global(BarrierSync syncAfter, BarrierAccess accessAfter)
    {
        return new BarrierDesc
        {
            Type = BarrierType.Global,
            SyncAfter = syncAfter,
            AccessAfter = accessAfter
        };
    }

    public static BarrierDesc Buffer(Handle<GPUResource> resource, BarrierSync syncAfter, BarrierAccess accessAfter, bool isAliasing = false)
    {
        return new BarrierDesc
        {
            Type = BarrierType.Buffer,
            Resource = resource,
            SyncAfter = syncAfter,
            AccessAfter = accessAfter,
            IsAliasing = isAliasing
        };
    }

    public static BarrierDesc Buffer(Handle<GPUBuffer> resource, BarrierSync syncAfter, BarrierAccess accessAfter, bool isAliasing = false)
    {
        return new BarrierDesc
        {
            Type = BarrierType.Buffer,
            Resource = resource.AsResource(),
            SyncAfter = syncAfter,
            AccessAfter = accessAfter,
            IsAliasing = isAliasing
        };
    }

    public static BarrierDesc Texture(Handle<GPUResource> resource, BarrierSync syncAfter, BarrierAccess accessAfter, BarrierLayout layoutAfter, BarrierSubresourceRange subresources = default, bool discard = false, bool isAliasing = false)
    {
        return new BarrierDesc
        {
            Type = BarrierType.Texture,
            Resource = resource,
            SyncAfter = syncAfter,
            AccessAfter = accessAfter,
            LayoutAfter = layoutAfter,
            Subresources = subresources,
            Discard = discard,
            IsAliasing = isAliasing
        };
    }

    public static BarrierDesc Texture(Handle<GPUTexture> resource, BarrierSync syncAfter, BarrierAccess accessAfter, BarrierLayout layoutAfter, BarrierSubresourceRange subresources = default, bool discard = false, bool isAliasing = false)
    {
        return new BarrierDesc
        {
            Type = BarrierType.Texture,
            Resource = resource.AsResource(),
            SyncAfter = syncAfter,
            AccessAfter = accessAfter,
            LayoutAfter = layoutAfter,
            Subresources = subresources,
            Discard = discard,
            IsAliasing = isAliasing
        };
    }

}

public record struct ResourceDesc
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct __union
    {
        [FieldOffset(0)]
        public TextureDesc textureDescription;
        [FieldOffset(0)]
        public BufferDesc bufferDescription;
    }

    private __union _desc;

    public ResourceType Type
    {
        get; init;
    }

    [UnscopedRef]
    public ref TextureDesc TextureDescriptor
    {
        get
        {
            Logger.DebugAssert(Type == ResourceType.Texture);
            return ref _desc.textureDescription;
        }
    }

    [UnscopedRef]
    public ref BufferDesc BufferDescriptor
    {
        get
        {
            Logger.DebugAssert(Type == ResourceType.Buffer);
            return ref _desc.bufferDescription;
        }
    }

    public static ResourceDesc Buffer(BufferDesc desc)
    {
        return new ResourceDesc
        {
            Type = ResourceType.Buffer,
            BufferDescriptor = desc
        };
    }

    public static ResourceDesc Texture(TextureDesc desc)
    {
        return new ResourceDesc
        {
            Type = ResourceType.Texture,
            TextureDescriptor = desc
        };
    }

    public bool Equals(ResourceDesc other)
    {
        if (Type != other.Type)
        {
            return false;
        }

        return Type switch
        {
            ResourceType.Texture => TextureDescriptor.Equals(other.TextureDescriptor),
            ResourceType.Buffer => BufferDescriptor.Equals(other.BufferDescriptor),
            _ => throw new InvalidOperationException($"Unknown resource type: {Type}")
        };
    }

    public override int GetHashCode()
    {
        return Type switch
        {
            ResourceType.Texture => HashCode.Combine(Type, TextureDescriptor),
            ResourceType.Buffer => HashCode.Combine(Type, BufferDescriptor),
            _ => throw new InvalidOperationException($"Unknown resource type: {Type}")
        };
    }

    public override string ToString()
    {
        return Type switch
        {
            ResourceType.Texture => $"Texture: {TextureDescriptor}",
            ResourceType.Buffer => $"Buffer: {BufferDescriptor}",
            _ => $"Unknown resource type: {Type}"
        };
    }
}

/// <summary>
/// Texture description
/// </summary>
public record struct TextureDesc
{
    /// <summary>
    /// Width of the texture
    /// </summary>
    public uint Width
    {
        get; set;
    }

    /// <summary>
    /// Height of the texture
    /// </summary>
    public uint Height
    {
        get; set;
    }

    /// <summary>
    /// Slice of the texture
    /// </summary>
    public uint Slice
    {
        get; set;
    }

    /// <summary>
    /// Texture Format
    /// </summary>
    public TextureFormat Format
    {
        get; set;
    }

    /// <summary>
    /// Texture dimension
    /// </summary>
    public TextureDimension Dimension
    {
        get;
        set;
    }

    /// <summary>
    /// Number of mip levels. 0 to generate full mip chain
    /// </summary>
    public uint MipLevels
    {
        get; set;
    }

    /// <summary>
    /// Texture usage flags
    /// </summary>
    public TextureUsage Usage
    {
        get; set;
    }
}

public ref struct AdditionalTextureDesc
{
    public ReadOnlySpan<TextureFormat> CastableFormat
    {
        get; set;
    }
}

public record struct SamplerDesc
{
    public TextureFilterMode FilterMode
    {
        get; set;
    }

    public TextureAddressMode AddressU
    {
        get; set;
    }

    public TextureAddressMode AddressV
    {
        get; set;
    }

    public TextureAddressMode AddressW
    {
        get; set;
    }

    public ComparisonFunction ComparisonFunc
    {
        get; set;
    }

    public float MipLODBias
    {
        get; set;
    }

    public uint MaxAnisotropy
    {
        get; set;
    }

    public float MinLOD
    {
        get; set;
    }

    public float MaxLOD
    {
        get; set;
    }
}

public record struct BufferDesc
{
    public ulong Size
    {
        get; set;
    }

    public uint Stride
    {
        get; set;
    }

    public BufferUsage Usage
    {
        get; set;
    }

    public HeapType HeapType
    {
        get; set;
    }
}

public struct CommandError
{
    public int CommandIndex
    {
        get; set;
    }

    public string CommandName
    {
        get; set;
    }

    public Error Status
    {
        get; set;
    }
}

public struct IndirectArgumentDesc
{
    public struct VertexBufferDesc
    {
        public uint Slot
        {
            get; set;
        }
    }

    public struct ConstantDesc
    {
        public uint RootParameterIndex
        {
            get; set;
        }
        public uint DestOffsetIn32BitValues
        {
            get; set;
        }
        public uint Num32BitValuesToSet
        {
            get; set;
        }
    }

    public struct ConstantBufferViewDesc
    {
        public uint RootParameterIndex
        {
            get; set;
        }
    }

    public struct ShaderResourceViewDesc
    {
        public uint RootParameterIndex
        {
            get; set;
        }
    }

    public struct UnorderedAccessViewDesc
    {
        public uint RootParameterIndex
        {
            get; set;
        }
    }

    public struct IncrementingConstantDesc
    {
        public uint RootParameterIndex
        {
            get; set;
        }
        public uint DestOffsetIn32BitValues
        {
            get; set;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct __union
    {
        [FieldOffset(0)]
        public VertexBufferDesc vertexBuffer;
        [FieldOffset(0)]
        public ConstantDesc constant;
        [FieldOffset(0)]
        public ConstantBufferViewDesc constantBufferView;
        [FieldOffset(0)]
        public ShaderResourceViewDesc shaderResourceView;
        [FieldOffset(0)]
        public UnorderedAccessViewDesc unorderedAccessView;
        [FieldOffset(0)]
        public IncrementingConstantDesc incrementingConstant;
    }

    public IndirectArgumentType Type
    {
        get; set;
    }

    private __union _data;

    [UnscopedRef]
    public ref VertexBufferDesc VertexBuffer => ref _data.vertexBuffer;

    [UnscopedRef]
    public ref ConstantDesc Constant => ref _data.constant;

    [UnscopedRef]
    public ref ConstantBufferViewDesc ConstantBufferView => ref _data.constantBufferView;

    [UnscopedRef]
    public ref ShaderResourceViewDesc ShaderResourceView => ref _data.shaderResourceView;

    [UnscopedRef]
    public ref UnorderedAccessViewDesc UnorderedAccessView => ref _data.unorderedAccessView;

    [UnscopedRef]
    public ref IncrementingConstantDesc IncrementingConstant => ref _data.incrementingConstant;
}

public ref struct CommandSignatureDesc
{
    public uint Stride
    {
        get; set;
    }

    public ReadOnlySpan<IndirectArgumentDesc> Arguments
    {
        get; set;
    }
}

public unsafe struct ProgramIdentifier
{
    public void* pIdentifier;
}

public unsafe struct NodeCPUInput
{
    public uint entryPointIndex;
    public uint numRecords;
    public void* pRecords;
    public ulong recordStrideInBytes;
}

public unsafe struct MultiNodeCPUInput
{
    public uint numNodeInputs;
    public NodeCPUInput* pNodeInputs;
    public ulong nodeInputStrideInBytes;
}

public struct DispatchGraphDesc
{
    [StructLayout(LayoutKind.Explicit)]
    private struct __union
    {
        [FieldOffset(0)]
        public NodeCPUInput nodeCPUInput;
        [FieldOffset(0)]
        public ulong nodeGPUInput;
        [FieldOffset(0)]
        public MultiNodeCPUInput multiNodeCPUInput;
        [FieldOffset(0)]
        public ulong multiNodeGPUInput;
    }

    private __union _input;

    public GraphDispatchMode DispatchMode
    {
        get; set;
    }

    [UnscopedRef]
    public ref NodeCPUInput NodeCPUInput => ref _input.nodeCPUInput;
    [UnscopedRef]
    public ref ulong NodeGPUInput => ref _input.nodeGPUInput;
    [UnscopedRef]
    public ref MultiNodeCPUInput MultiNodeCPUInput => ref _input.multiNodeCPUInput;
    [UnscopedRef]
    public ref ulong MultiNodeGPUInput => ref _input.multiNodeGPUInput;
}

public struct WorkGraphMemoryRequirements
{
    public ulong MinSizeInBytes
    {
        get; set;
    }

    public ulong MaxSizeInBytes
    {
        get; set;
    }

    public uint SizeGranularityInBytes
    {
        get; set;
    }
}

public struct WorkGraphSubObjectDesc
{
    public string ProgramName
    {
        get; set;
    }

    public bool IncludeAllAvailableNodes
    {
        get; set;
    }
}

public struct SetProgramDesc
{
    public ProgramIdentifier ProgramIdentifier
    {
        get; set;
    }

    public SetWorkGraphFlags Flags
    {
        get; set;
    }


    // D3D12_GPU_VIRTUAL_ADDRESS_RANGE
    public ulong BackingMemoryAddress
    {
        get; set;
    }

    public ulong BackingMemorySize
    {
        get; set;
    }


    // D3D12_GPU_VIRTUAL_ADDRESS_RANGE_AND_STRIDE
    public ulong NodeLocalRootArgumentsTableAddress
    {
        get; set;
    }

    public ulong NodeLocalRootArgumentsTableSizeInBytes
    {
        get; set;
    }

    public ulong NodeLocalRootArgumentsTableStrideInBytes
    {
        get; set;
    }
}

public struct SwapChainDesc
{
    public uint Width
    {
        get; set;
    }

    public uint Height
    {
        get; set;
    }

    public float ScaleX
    {
        get; set;
    }

    public float ScaleY
    {
        get; set;
    }

    public TextureFormat Format
    {
        get; set;
    }

    public SwapChainTarget Target
    {
        get; set;
    }
}

public struct SwapChainTarget
{
    public SwapChainTargetType Type
    {
        get; set;
    }

    public nint WindowHandle
    {
        get; set;
    }

    public object? CompositionSurface
    {
        get; set;
    }

    public static SwapChainTarget FromWindowHandle(nint hwnd)
    {
        return new SwapChainTarget
        {
            Type = SwapChainTargetType.WindowHandle,
            WindowHandle = hwnd,
            CompositionSurface = 0
        };
    }

    public static SwapChainTarget FromCompositionSurface(object surface)
    {
        return new SwapChainTarget
        {
            Type = SwapChainTargetType.Composition,
            WindowHandle = 0,
            CompositionSurface = surface
        };
    }
}

public enum SwapChainTargetType
{
    WindowHandle,
    Composition
}

public enum BarrierType
{
    Global,
    Texture,
    Buffer
}

[Flags]
public enum BarrierSync
{
    None = 0x0,
    All = 0x1,
    Draw = 0x2,
    IndexInput = 0x4,
    VertexShading = 0x8,
    PixelShading = 0x10,
    DepthStencil = 0x20,
    RenderTarget = 0x40,
    ComputeShading = 0x80,
    Raytracing = 0x100,
    Copy = 0x200,
    Resolve = 0x400,
    ExecuteIndirect = 0x800,
    Predication = 0x800,
    AllShading = 0x1000,
    NonPixelShading = 0x2000,
    EmitRaytracingAccelerationStructurePostbuildInfo = 0x4000,
    ClearUnorderedAccessView = 0x8000,
    VideoDecode = 0x100000,
    VideoProcess = 0x200000,
    VideoEncode = 0x400000,
    BuildRaytracingAccelerationStructure = 0x800000,
    CopyRaytracingAccelerationStructure = 0x1000000,
    Split = unchecked((int)0x80000000)
}

[Flags]
public enum BarrierAccess
{
    Common = 0,
    VertexBuffer = 0x1,
    ConstantBuffer = 0x2,
    IndexBuffer = 0x4,
    RenderTarget = 0x8,
    UnorderedAccess = 0x10,
    DepthStencilWrite = 0x20,
    DepthStencilRead = 0x40,
    ShaderResource = 0x80,
    StreamOutput = 0x100,
    IndirectArgument = 0x200,
    Predication = 0x200,
    CopyDest = 0x400,
    CopySource = 0x800,
    ResolveDest = 0x1000,
    ResolveSource = 0x2000,
    RaytracingAccelerationStructureRead = 0x4000,
    RaytracingAccelerationStructureWrite = 0x8000,
    ShadingRateSource = 0x10000,
    VideoDecodeRead = 0x20000,
    VideoDecodeWrite = 0x40000,
    VideoProcessRead = 0x80000,
    VideoProcessWrite = 0x100000,
    VideoEncodeRead = 0x200000,
    VideoEncodeWrite = 0x400000,
    NoAccess = unchecked((int)0x80000000)
}

public enum BarrierLayout
{
    Undefined = -1,
    Common = 0,
    Present = 0,
    GenericRead = 1,
    RenderTarget = 2,
    UnorderedAccess = 3,
    DepthStencilWrite = 4,
    DepthStencilRead = 5,
    ShaderResource = 6,
    CopySource = 7,
    CopyDest = 8,
    ResolveSource = 9,
    ResolveDest = 10,
    ShadingRateSource = 11,
    VideoDecodeRead = 12,
    VideoDecodeWrite = 13,
    VideoProcessRead = 14,
    VideoProcessWrite = 15,
    VideoEncodeRead = 16,
    VideoEncodeWrite = 17,
    DirectQueueCommon = 18,
    DirectQueueGenericRead = 19,
    DirectQueueUnorderedAccess = 20,
    DirectQueueShaderResource = 21,
    DirectQueueCopySource = 22,
    DirectQueueCopyDest = 23,
    ComputeQueueCommon = 24,
    ComputeQueueGenericRead = 25,
    ComputeQueueUnorderedAccess = 26,
    ComputeQueueShaderResource = 27,
    ComputeQueueCopySource = 28,
    ComputeQueueCopyDest = 29,
    DirectQueueGenericReadComputeQueueAccessible = 31,
}

[Flags]
public enum ResourceState : int
{
    Auto = -1,
    Common = 0,
    VertexAndConstantBuffer = 1 << 0,
    IndexBuffer = 1 << 1,
    RenderTarget = 1 << 2,
    UnorderedAccess = 1 << 3,
    DepthWrite = 1 << 4,
    DepthRead = 1 << 5,
    PixelShaderResource = 1 << 6,
    CopyDest = 1 << 7,
    CopySource = 1 << 8,
    GenericRead = 1 << 9,
    IndirectArgument = 1 << 10,
    NonPixelShaderResource = 1 << 11,
    Present = 0,
}

public enum CommandQueueType
{
    Graphics,
    Compute,
    Copy
}

public enum CommandBufferType
{
    Graphics,
    Compute,
    Copy
}

public enum PipelineType
{
    Graphics,
    Compute
}

[Flags]
public enum RenderTargetCreationFlags
{
    None = 0,
    AllowUAV = 1 << 0,
    AllowMSAA = 1 << 1,
    DynamicallyResolution = 1 << 2,
    GenerateMips = 1 << 3
}

public enum ResourceType
{
    Texture,
    Buffer
}

[Flags]
public enum TextureUsage
{
    None = 0,
    ShaderResource = 1 << 0,
    RenderTarget = 1 << 1,
    DepthStencil = 1 << 2,
    UnorderedAccess = 1 << 3
}

public enum TextureDimension
{
    Unknown = -1,
    None = 0,
    Texture1D = 1,
    Texture2D = 2,
    Texture3D = 3,
    TextureCube = 4,
    Texture2DArray = 5,
    TextureCubeArray = 6
}

public enum RenderTargetType
{
    Color,
    Depth
}

// TODO: Support compressed formats (BCn, ASTC, ETC2, etc)
public enum TextureFormat
{
    Unknown,

    R8_UNorm,
    R8_SNorm,
    R16_UNorm,
    R16_SNorm,
    R16_Float,
    R32_UInt,
    R32_SInt,

    R8G8_UNorm,
    R8G8_SNorm,
    R16G16_UNorm,
    R16G16_SNorm,
    R16G16_Float,
    R32G32_Float,

    R8G8B8A8_UNorm,
    R8G8B8A8_SNorm,
    B8G8R8A8_UNorm,

    R10G10B10A2_UNorm,

    R16G16B16A16_Float,
    R32G32B32A32_Float,

    D24_UNorm_S8_UInt,
    D32_Float,

    R32_Typeless,
    R24G8_Typeless,
}

[Flags]
public enum BufferUsage
{
    None = 0,
    Vertex = 1 << 0,
    Index = 1 << 1,
    IndirectArgument = 1 << 7,
    Constant = 1 << 2,
    ShaderResource = 1 << 3,
    UnorderedAccess = 1 << 4,
    Structured = 1 << 5,
    Raw = 1 << 6,
    Upload = 1 << 8,
    Readback = 1 << 9,
}

public enum IndexType
{
    UInt16,
    UInt32
}

public enum PrimitiveTopology
{
    Point,
    Line,
    Triangle,
}

public enum TextureFilterMode
{
    Point,
    Bilinear,
    Trilinear,
    Anisotropic
}

public enum TextureAddressMode
{
    Repeat,
    Mirror,
    Clamp,
    Border,
    MirrorOnce
}

public enum ComparisonFunction
{
    Never,
    Less,
    Equal,
    LessEqual,
    Greater,
    NotEqual,
    GreaterEqual,
    Always
}

public enum AttachmentLoadOp
{
    Load,
    Clear,
    DontCare,
    NoAccess
}

public enum AttachmentStoreOp
{
    Store,
    DontCare,
    NoAccess
}

public enum IndirectArgumentType
{
    Draw,
    DrawIndexed,
    Dispatch,
    VertexBufferView,
    IndexBufferView,
    Constant,
    ConstantBufferView,
    ShaderResourceView,
    UnorderedAccessView,
    DispatchRays,
    DispatchMesh,
    IncrementingConstant,
}

public enum GraphDispatchMode
{
    CPUInput,
    GPUInput,
    MultiCPUInput,
    MultiGPUInput
}

[Flags]
public enum SetWorkGraphFlags
{
    None,
    Initialize
}
