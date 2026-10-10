using Ghost.Core;
using Ghost.Core.Graphics;
using Misaki.HighPerformance.Mathematics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.RHI;

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

public readonly struct ShaderPass
{
    public Key64<ShaderPass> Key
    {
        get; init;
    }

    public ShaderStageMask StageMask
    {
        get;
        init;
    }

    public PipelineState DefaultState
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

    public ulong PassId
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

    public ulong PassId
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

public readonly struct ViewportState : IEquatable<ViewportState>
{
    public uint2 Size
    {
        get; init;
    }
    public uint2 ActualSize
    {
        get; init;
    }

    public ViewportState(uint width, uint height, uint actualWidth, uint actualHeight)
    {
        Size = new uint2(width, height);
        ActualSize = new uint2(actualWidth, actualHeight);
    }

    public readonly float2 CalculateScale(ViewportState other)
    {
        return new float2(
            (float)Size.x / other.Size.x,
            (float)Size.y / other.Size.y
        );
    }

    public readonly bool Equals(ViewportState other)
    {
        return Size.Equals(other.Size) && ActualSize.Equals(other.ActualSize);
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is ViewportState other && Equals(other);
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(Size, ActualSize);
    }

    public static bool operator ==(ViewportState left, ViewportState right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ViewportState left, ViewportState right)
    {
        return !left.Equals(right);
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

    public AttachmentLoadOp LoadOp
    {
        get; set;
    }

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

    public AttachmentLoadOp DepthLoadOp
    {
        get; set;
    }

    public AttachmentStoreOp DepthStoreOp
    {
        get; set;
    }

    public AttachmentLoadOp StencilLoadOp
    {
        get; set;
    }

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
    public const uint ALL_SUBRESOURCES = 0xFFFFFFFF;

    public uint IndexOrFirstMipLevel { get; set; }
    public uint NumMipLevels { get; set; }
    public uint FirstArraySlice { get; set; }
    public uint NumArraySlices { get; set; }
    public uint FirstPlane { get; set; }
    public uint NumPlanes { get; set; }

    public static BarrierSubresourceRange All => new BarrierSubresourceRange
    {
        IndexOrFirstMipLevel = ALL_SUBRESOURCES,
        NumMipLevels = 0,
        FirstArraySlice = 0,
        NumArraySlices = 0,
        FirstPlane = 0,
        NumPlanes = 0
    };

    public static BarrierSubresourceRange Single(uint subresourceIndex) => new BarrierSubresourceRange
    {
        IndexOrFirstMipLevel = subresourceIndex,
        NumMipLevels = 0,
        FirstArraySlice = 0,
        NumArraySlices = 0,
        FirstPlane = 0,
        NumPlanes = 0
    };

    public static BarrierSubresourceRange Range(uint firstMip, uint numMips, uint firstSlice = 0, uint numSlices = 1, uint firstPlane = 0, uint numPlanes = 1) => new BarrierSubresourceRange
    {
        IndexOrFirstMipLevel = firstMip,
        NumMipLevels = numMips,
        FirstArraySlice = firstSlice,
        NumArraySlices = numSlices,
        FirstPlane = firstPlane,
        NumPlanes = numPlanes
    };
}

public struct BarrierDesc
{
    [StructLayout(LayoutKind.Explicit)]
    public struct __additional_data
    {
        public struct BufferData
        {
            public ulong offset;
            public ulong size;
        }

        public struct TextureData
        {
            public BarrierSubresourceRange subresourceRange;
            public bool discard;
        }

        [FieldOffset(0)]
        public BufferData bufferData;
        [FieldOffset(0)]
        public TextureData textureData;
    }

    private __additional_data _additionalData;

    public BarrierType Type { get; set; }

    public BarrierSync SyncBefore { get; set; }

    public BarrierSync SyncAfter { get; set; }

    public BarrierAccess AccessBefore { get; set; }

    public BarrierAccess AccessAfter { get; set; }

    public BarrierLayout LayoutBefore { get; set; }

    public BarrierLayout LayoutAfter { get; set; }

    public Handle<GPUResource> Resource { get; set; }

    public bool Force { get; set; }

    public BarrierHandoffType Handoff { get; set; }

    [UnscopedRef]
    public ref ulong Offset => ref _additionalData.bufferData.offset;
    [UnscopedRef]
    public ref ulong Size => ref _additionalData.bufferData.size;

    [UnscopedRef]
    public ref BarrierSubresourceRange SubresourceRange => ref _additionalData.textureData.subresourceRange;
    [UnscopedRef]
    public ref bool Discard => ref _additionalData.textureData.discard;

    public static BarrierDesc Global(
        BarrierSync syncBefore,
        BarrierSync syncAfter,
        BarrierAccess accessBefore,
        BarrierAccess accessAfter)
    {
        return new BarrierDesc
        {
            Type = BarrierType.Global,
            SyncBefore = syncBefore,
            SyncAfter = syncAfter,
            AccessBefore = accessBefore,
            AccessAfter = accessAfter
        };
    }

    public static BarrierDesc Buffer(
        Handle<GPUBuffer> resource,
        ResourceBarrierData before,
        ResourceBarrierData after,
        BarrierHandoffType handoff = BarrierHandoffType.None,
        bool force = false,
        ulong offset = 0UL,
        ulong size = ulong.MaxValue)
    {
        return new BarrierDesc
        {
            Type = BarrierType.Buffer,
            Resource = resource.AsResource(),
            SyncBefore = before.sync,
            SyncAfter = after.sync,
            AccessBefore = before.access,
            AccessAfter = after.access,
            LayoutBefore = before.layout,
            LayoutAfter = after.layout,
            Handoff = handoff,
            Force = force,
            Offset = offset,
            Size = size
        };
    }

    public static BarrierDesc Buffer(
        Handle<GPUBuffer> resource,
        BarrierSync syncBefore,
        BarrierSync syncAfter,
        BarrierAccess accessBefore,
        BarrierAccess accessAfter,
        BarrierHandoffType handoff = BarrierHandoffType.None,
        bool force = false,
        ulong offset = 0UL,
        ulong size = ulong.MaxValue)
    {
        return new BarrierDesc
        {
            Type = BarrierType.Buffer,
            Resource = resource.AsResource(),
            SyncBefore = syncBefore,
            SyncAfter = syncAfter,
            AccessBefore = accessBefore,
            AccessAfter = accessAfter,
            LayoutBefore = BarrierLayout.Undefined,
            LayoutAfter = BarrierLayout.Undefined,
            Handoff = handoff,
            Force = force,
            Offset = offset,
            Size = size
        };
    }

    public static BarrierDesc Texture(
        Handle<GPUTexture> resource,
        ResourceBarrierData before,
        ResourceBarrierData after,
        BarrierSubresourceRange subresources = default,
        BarrierHandoffType handoff = BarrierHandoffType.None,
        bool discard = false,
        bool force = false)
    {
        return new BarrierDesc
        {
            Type = BarrierType.Texture,
            Resource = resource.AsResource(),
            SyncBefore = before.sync,
            SyncAfter = after.sync,
            AccessBefore = before.access,
            AccessAfter = after.access,
            LayoutBefore = before.layout,
            LayoutAfter = after.layout,
            Handoff = handoff,
            Force = force,
            SubresourceRange = subresources,
            Discard = discard
        };
    }

    public static BarrierDesc Texture(
        Handle<GPUTexture> resource,
        BarrierSync syncBefore,
        BarrierSync syncAfter,
        BarrierAccess accessBefore,
        BarrierAccess accessAfter,
        BarrierLayout layoutBefore,
        BarrierLayout layoutAfter,
        BarrierSubresourceRange subresources = default,
        BarrierHandoffType handoff = BarrierHandoffType.None,
        bool discard = false,
        bool force = false)
    {
        return new BarrierDesc
        {
            Type = BarrierType.Texture,
            Resource = resource.AsResource(),
            SyncBefore = syncBefore,
            SyncAfter = syncAfter,
            AccessBefore = accessBefore,
            AccessAfter = accessAfter,
            LayoutBefore = layoutBefore,
            LayoutAfter = layoutAfter,
            Handoff = handoff,
            Force = force,
            SubresourceRange = subresources,
            Discard = discard
        };
    }
}

public record struct ResourceDesc
{
    [StructLayout(LayoutKind.Explicit)]
    private struct __union
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

public record struct TypelessFormatDesc
{
    public TextureFormat Srv
    {
        get; set;
    }

    public TextureFormat Uav
    {
        get; set;
    }

    public TextureFormat Rtv
    {
        get; set;
    }

    public TextureFormat Dsv
    {
        get; set;
    }
}

public record struct TextureDesc
{
    public uint Width
    {
        get; set;
    }

    public uint Height
    {
        get; set;
    }

    public uint Slice
    {
        get; set;
    }

    public TextureFormat Format
    {
        get; set;
    }

    public TextureDimension Dimension
    {
        get;
        set;
    }

    public uint MipLevels
    {
        get; set;
    }

    public TextureUsage Usage
    {
        get; set;
    }

    public TypelessFormatDesc TypelessViewFormat
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

public struct CommandBufferState
{
    public bool IsRecording
    {
        get; set;
    }

    public int CommandCount
    {
        get; set;
    }

    public Error Error
    {
        get; set;
    }

    public string ErrorCommandName
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
    public fixed ulong opaqueData[4];
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe DispatchGraphDesc ForCPUInput(uint entryPointIndex, uint numRecords, void* pRecords = null, ulong recordStrideInBytes = 0)
    {
        return new DispatchGraphDesc
        {
            DispatchMode = GraphDispatchMode.CPUInput,
            NodeCPUInput = new NodeCPUInput
            {
                entryPointIndex = entryPointIndex,
                numRecords = numRecords,
                pRecords = pRecords,
                recordStrideInBytes = recordStrideInBytes,
            }
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe DispatchGraphDesc ForEmptyCPUInput(uint entryPointIndex, uint numRecords)
    {
        return ForCPUInput(entryPointIndex, numRecords, null, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DispatchGraphDesc ForGPUInput(ulong gpuAddress)
    {
        return new DispatchGraphDesc
        {
            DispatchMode = GraphDispatchMode.GPUInput,
            NodeGPUInput = gpuAddress
        };
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct WorkGraphDispatchGridRecord
{
    public uint gridX;
    public uint gridY;
    public uint gridZ;

    public WorkGraphDispatchGridRecord(uint x, uint y = 1, uint z = 1)
    {
        gridX = x;
        gridY = y;
        gridZ = z;
    }
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

public enum ProgramType
{
    GenericPipeline,
    RaytracingPipeline,
    WorkGraph
}

public struct SetWorkGraphDesc
{
    public ProgramIdentifier ProgramIdentifier
    {
        get; set;
    }

    public SetWorkGraphFlags Flags
    {
        get; set;
    }

    public ulong BackingMemoryAddress
    {
        get; set;
    }

    public ulong BackingMemorySize
    {
        get; set;
    }

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

public struct SetRaytracingPipelineDesc
{
    public ProgramIdentifier ProgramIdentifier
    {
        get; set;
    }
}

public struct SetGenericPipelineDesc
{
    public ProgramIdentifier ProgramIdentifier
    {
        get; set;
    }
}

public struct SetProgramDesc
{
    public ProgramType Type
    {
        get; set;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct __union
    {
        [FieldOffset(0)]
        public SetWorkGraphDesc workGraph;
        [FieldOffset(0)]
        public SetRaytracingPipelineDesc raytracingPipeline;
        [FieldOffset(0)]
        public SetGenericPipelineDesc genericPipeline;
    }

    private __union _desc;

    [UnscopedRef]
    public ref SetWorkGraphDesc WorkGraph => ref _desc.workGraph;

    [UnscopedRef]
    public ref SetRaytracingPipelineDesc RaytracingPipeline => ref _desc.raytracingPipeline;

    [UnscopedRef]
    public ref SetGenericPipelineDesc GenericPipeline => ref _desc.genericPipeline;

    public static SetProgramDesc ForWorkGraph(
        ProgramIdentifier identifier,
        ulong backingMemoryAddress,
        ulong backingMemorySize,
        SetWorkGraphFlags flags = SetWorkGraphFlags.None,
        ulong localRootTableAddress = 0,
        ulong localRootTableSize = 0,
        ulong localRootTableStride = 0)
    {
        return new SetProgramDesc
        {
            Type = ProgramType.WorkGraph,
            WorkGraph = new SetWorkGraphDesc
            {
                ProgramIdentifier = identifier,
                Flags = flags,
                BackingMemoryAddress = backingMemoryAddress,
                BackingMemorySize = backingMemorySize,
                NodeLocalRootArgumentsTableAddress = localRootTableAddress,
                NodeLocalRootArgumentsTableSizeInBytes = localRootTableSize,
                NodeLocalRootArgumentsTableStrideInBytes = localRootTableStride,
            }
        };
    }

    public static SetProgramDesc ForRaytracing(ProgramIdentifier identifier)
    {
        return new SetProgramDesc { Type = ProgramType.RaytracingPipeline, RaytracingPipeline = new SetRaytracingPipelineDesc { ProgramIdentifier = identifier } };
    }

    public static SetProgramDesc ForGeneric(ProgramIdentifier identifier)
    {
        return new SetProgramDesc
        {
            Type = ProgramType.GenericPipeline,
            GenericPipeline = new SetGenericPipelineDesc { ProgramIdentifier = identifier }
        };
    }
}

public struct SwapChainDesc
{
    public required uint Width
    {
        get; set;
    }

    public required uint Height
    {
        get; set;
    }

    public required float ScaleX
    {
        get; set;
    }

    public required float ScaleY
    {
        get; set;
    }

    public required TextureFormat Format
    {
        get; set;
    }

    public required SwapChainTarget Target
    {
        get; set;
    }
}

public struct SwapChainTarget
{
    public required SwapChainTargetType Type
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

/// <summary>Identifies a resource barrier's role in a cross-queue handoff.</summary>
public enum BarrierHandoffType : byte
{
    None,
    Release,
    Acquire
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
    R32_Float,
    R32_UInt,
    R32_SInt,

    R8G8_UNorm,
    R8G8_SNorm,
    R16G16_UNorm,
    R16G16_SNorm,
    R16G16_Float,
    R32G32_Float,
    R32G32_UInt,

    R8G8B8A8_SRGB,
    R8G8B8A8_UNorm,
    R8G8B8A8_SNorm,
    B8G8R8A8_UNorm,
    R11G11B10_Float,

    R10G10B10A2_UNorm,

    R16G16B16A16_Float,
    R32G32B32A32_Float,

    D24_UNorm_S8_UInt,
    D32_Float,

    R8G8B8A8_Typeless,
    R16G16B16A16_Typeless,
    R32G32B32A32_Typeless,
    R32_Typeless,
    R24G8_Typeless,

    BC1_UNorm,
    BC1_UNorm_SRGB,
    BC2_UNorm,
    BC2_UNorm_SRGB,
    BC3_UNorm,
    BC3_UNorm_SRGB,
    BC4_UNorm,
    BC4_SNorm,
    BC5_UNorm,
    BC5_SNorm,
    BC6H_UF16,
    BC6H_SF16,
    BC7_UNorm,
    BC7_UNorm_SRGB,
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
