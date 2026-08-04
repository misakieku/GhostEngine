using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.RenderGraphModule;

public enum RGResourceType : int
{
    Texture,
    Buffer,
    // AccelerationStructure,
    Count
}

/// <summary>
/// Specifies how texture dimensions are determined.
/// </summary>
public enum RGTextureSizeMode : byte
{
    /// <summary>
    /// Fixed pixel dimensions (width, height).
    /// </summary>
    Absolute,

    /// <summary>
    /// Scale relative to view state (scaleX * viewportWidth, scaleY * viewportHeight).
    /// </summary>
    Relative
}

/// <summary>
/// View state information for resolving relative texture sizes.
/// </summary>
public struct ViewState : IEquatable<ViewState>
{
    public uint viewportWidth;
    public uint viewportHeight;

    // For upscalers that need to know the original render target size before upscaling
    public uint actualWidth;
    public uint actualHeight;

    public ViewState(uint width, uint height, uint actualWidth, uint actualHeight)
    {
        viewportWidth = width;
        viewportHeight = height;
        this.actualWidth = actualWidth;
        this.actualHeight = actualHeight;
    }

    public readonly float2 CalculateScale(ViewState other)
    {
        return new float2(
            (float)viewportWidth / other.viewportWidth,
            (float)viewportHeight / other.viewportHeight
        );
    }

    public readonly bool Equals(ViewState other)
    {
        return viewportWidth == other.viewportWidth && viewportHeight == other.viewportHeight
            && actualWidth == other.actualWidth && actualHeight == other.actualHeight;
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is ViewState other && Equals(other);
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(viewportWidth, viewportHeight, actualWidth, actualHeight);
    }

    public static bool operator ==(ViewState left, ViewState right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ViewState left, ViewState right)
    {
        return !left.Equals(right);
    }
}

/// <summary>
/// Render graph texture descriptor with support for relative sizing and clear operations.
/// </summary>
public struct RGTextureDesc : IEquatable<RGTextureDesc>
{
    public RGTextureSizeMode sizeMode;

    // Size specification (union-like - only one set is used based on sizeMode)
    public uint width;          // For Absolute mode
    public uint height;         // For Absolute mode
    public float scaleX;        // For Relative mode
    public float scaleY;        // For Relative mode

    // Common texture properties
    public TextureFormat format;
    public TextureDimension dimension;
    public uint mipLevels;
    public uint slice;
    public TextureUsage usage;

    public bool clearAtFirstUse;
    public bool discardAtLastUse;

    // Clear operation support
    public Color128 clearColor;

    public float clearDepth;
    public byte clearStencil;

    /// <summary>
    /// Creates a texture descriptor with absolute dimensions.
    /// </summary>
    public static RGTextureDesc Absolute(
        uint width,
        uint height,
        TextureFormat format,
        Color128 clearColor = default,
        bool clearAtFirstUse = true,
        bool discardAtLastUse = true,
        TextureDimension dimension = TextureDimension.Texture2D,
        uint mipLevels = 1,
        uint slice = 1,
        TextureUsage usage = TextureUsage.RenderTarget | TextureUsage.ShaderResource)
    {
        return new RGTextureDesc
        {
            sizeMode = RGTextureSizeMode.Absolute,
            width = width,
            height = height,
            format = format,
            clearColor = clearColor,
            clearAtFirstUse = clearAtFirstUse,
            discardAtLastUse = discardAtLastUse,
            clearDepth = 1.0f,
            clearStencil = 0,
            dimension = dimension,
            mipLevels = mipLevels,
            slice = slice,
            usage = usage
        };
    }

    /// <summary>
    /// Creates a texture descriptor with relative dimensions (uniform scale).
    /// </summary>
    public static RGTextureDesc Relative(
        float scale,
        TextureFormat format,
        Color128 clearColor = default,
        bool clearAtFirstUse = true,
        bool discardAtLastUse = true,
        TextureDimension dimension = TextureDimension.Texture2D,
        uint mipLevels = 1,
        uint slice = 1,
        TextureUsage usage = TextureUsage.RenderTarget | TextureUsage.ShaderResource)
    {
        return new RGTextureDesc
        {
            sizeMode = RGTextureSizeMode.Relative,
            scaleX = scale,
            scaleY = scale,
            format = format,
            clearColor = clearColor,
            clearAtFirstUse = clearAtFirstUse,
            discardAtLastUse = discardAtLastUse,
            clearDepth = 1.0f,
            clearStencil = 0,
            dimension = dimension,
            mipLevels = mipLevels,
            slice = slice,
            usage = usage
        };
    }

    /// <summary>
    /// Creates a texture descriptor with relative dimensions (non-uniform scale).
    /// </summary>
    public static RGTextureDesc Relative(
        float scaleX,
        float scaleY,
        TextureFormat format,
        Color128 clearColor = default,
        bool clearAtFirstUse = true,
        bool discardAtLastUse = true,
        TextureDimension dimension = TextureDimension.Texture2D,
        uint mipLevels = 1,
        uint slice = 1,
        TextureUsage usage = TextureUsage.RenderTarget | TextureUsage.ShaderResource)
    {
        return new RGTextureDesc
        {
            sizeMode = RGTextureSizeMode.Relative,
            scaleX = scaleX,
            scaleY = scaleY,
            format = format,
            clearColor = clearColor,
            clearAtFirstUse = clearAtFirstUse,
            discardAtLastUse = discardAtLastUse,
            clearDepth = 1.0f,
            clearStencil = 0,
            dimension = dimension,
            mipLevels = mipLevels,
            slice = slice,
            usage = usage
        };
    }


    /// <summary>
    /// Creates a depth texture descriptor with relative dimensions.
    /// </summary>
    public static RGTextureDesc RelativeDepth(
        float scale,
        float clearDepth = 1.0f,
        byte clearStencil = 0,
        bool clearAtFirstUse = true,
        bool discardAtLastUse = true,
        TextureFormat format = TextureFormat.D32_Float,
        TextureUsage usage = TextureUsage.DepthStencil)
    {
        return new RGTextureDesc
        {
            sizeMode = RGTextureSizeMode.Relative,
            scaleX = scale,
            scaleY = scale,
            format = format,
            clearColor = default,
            clearDepth = clearDepth,
            clearStencil = clearStencil,
            clearAtFirstUse = clearAtFirstUse,
            discardAtLastUse = discardAtLastUse,
            dimension = TextureDimension.Texture2D,
            mipLevels = 1,
            slice = 1,
            usage = usage
        };
    }


    /// <summary>
    /// Converts to RHI TextureDesc using resolved dimensions.
    /// </summary>
    public readonly TextureDesc ToTextureDesc(uint resolvedWidth, uint resolvedHeight)
    {
        return new TextureDesc
        {
            Width = resolvedWidth,
            Height = resolvedHeight,
            Format = format,
            Dimension = dimension,
            MipLevels = mipLevels,
            Slice = slice,
            Usage = usage
        };
    }

    public readonly bool Equals(RGTextureDesc other)
    {
        return sizeMode == other.sizeMode &&
               format == other.format &&
               dimension == other.dimension &&
               mipLevels == other.mipLevels &&
               slice == other.slice &&
               usage == other.usage &&
               clearAtFirstUse == other.clearAtFirstUse &&
               discardAtLastUse == other.discardAtLastUse &&
               (sizeMode == RGTextureSizeMode.Absolute
                   ? width == other.width && height == other.height
                   : scaleX == other.scaleX && scaleY == other.scaleY);
    }


    public override readonly bool Equals(object? obj)
    {
        return obj is RGTextureDesc other && Equals(other);
    }

    public override readonly int GetHashCode()
    {
        if (sizeMode == RGTextureSizeMode.Absolute)
        {
            return HashCode.Combine(sizeMode, width, height, format, dimension, mipLevels, slice, usage);
        }
        else
        {
            return HashCode.Combine(sizeMode, scaleX, scaleY, format, dimension, mipLevels, slice, usage);
        }
    }

    public static bool operator ==(RGTextureDesc left, RGTextureDesc right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(RGTextureDesc left, RGTextureDesc right)
    {
        return !left.Equals(right);
    }
}

public struct RGResource;
public struct RGTexture;
public struct RGBuffer;

public static class RGResourceExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Identifier<RGResource> AsResource(this Identifier<RGTexture> texture)
    {
        return Unsafe.BitCast<Identifier<RGTexture>, Identifier<RGResource>>(texture);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Identifier<RGResource> AsResource(this Identifier<RGBuffer> buffer)
    {
        return Unsafe.BitCast<Identifier<RGBuffer>, Identifier<RGResource>>(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Identifier<RGTexture> AsTexture(this Identifier<RGResource> resource)
    {
        return Unsafe.BitCast<Identifier<RGResource>, Identifier<RGTexture>>(resource);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Identifier<RGBuffer> AsBuffer(this Identifier<RGResource> resource)
    {
        return Unsafe.BitCast<Identifier<RGResource>, Identifier<RGBuffer>>(resource);
    }
}

internal readonly struct TextureAccess
{
    public readonly Identifier<RGTexture> id;
    public readonly AccessFlags accessFlags;
    public readonly ResourceBarrierData usage;

    public TextureAccess(Identifier<RGTexture> id, AccessFlags accessFlags, ResourceBarrierData usage)
    {
        this.id = id;
        this.accessFlags = accessFlags;
        this.usage = usage;
    }
}

[InlineArray(RHIUtility.MAX_RENDER_TARGETS)]
internal struct TextureAccessArray
{
    public TextureAccess access;
}

internal readonly struct BufferAccess
{
    public readonly Identifier<RGBuffer> id;
    public readonly AccessFlags accessFlags;
    public readonly ResourceBarrierData usage;

    public BufferAccess(Identifier<RGBuffer> id, AccessFlags accessFlags, ResourceBarrierData usage)
    {
        this.id = id;
        this.accessFlags = accessFlags;
        this.usage = usage;
    }
}

internal struct RenderTargetInfo
{
    public Identifier<RGTexture> texture;
    public AccessFlags access;
    public AttachmentLoadOp loadOp;
    public AttachmentStoreOp storeOp;
    public Color128 clearColor;
}

[InlineArray(RHIUtility.MAX_RENDER_TARGETS)]
internal struct RenderTargetInfoArray
{
    public RenderTargetInfo info;
}

internal struct DepthStencilInfo
{
    public Identifier<RGTexture> texture;
    public AccessFlags access;
    public AttachmentLoadOp loadOp;
    public AttachmentStoreOp storeOp;
    public AttachmentLoadOp stencilLoadOp;
    public AttachmentStoreOp stencilStoreOp;
    public float clearDepth;
    public byte clearStencil;
}

internal enum RGExecutionOpType : byte
{
    IssueBarriers = 0,
    BeginNativePass = 1,
    ExecutePass = 2,
    EndNativePass = 3,
    CommandBufferSyncPoint = 4,
}

[Flags]
public enum RGExecutionFlags
{
    /// <summary>
    /// Default execution behavior without any special flags.
    /// </summary>
    Default = 0,
    /// <summary>
    /// Generate a detailed dump of the render graph execution for debugging and analysis.
    /// </summary>
    GenerateDump = 1 << 0,
}

public sealed class RenderGraphDump
{
    public ulong GraphHash
    {
        get; init;
    }

    public bool IsCacheHit
    {
        get; init;
    }

    public ViewState ViewState
    {
        get; init;
    }

    // Memory Heap Aliasing Map
    public ulong TotalHeapSize
    {
        get; init;
    }

    public List<HeapBlockDumpInfo> MemoryBlocks
    {
        get; init;
    } = new();

    // Complete Pass List (with culling & merge info)
    public List<PassDumpInfo> Passes
    {
        get; init;
    } = new();

    // Disassembled Binary Command Stream
    public List<string> CommandStream
    {
        get; init;
    } = new();

    // Complete Resource List (with offsets & lifetimes)
    public List<ResourceDumpInfo> Resources
    {
        get; init;
    } = new();
}

/// <summary>
/// Describes why a render-graph pass was assigned to its effective command-buffer type.
/// </summary>
public enum RGQueueDecision : byte
{
    /// <summary>The pass does not request asynchronous Compute execution.</summary>
    AsyncNotRequested,

    /// <summary>The pass type cannot execute on an asynchronous Compute command buffer.</summary>
    IneligiblePassType,

    /// <summary>The pass was culled and has no effective command-buffer assignment.</summary>
    Culled,

    /// <summary>The dependency-window planner selected the pass for a Compute command buffer.</summary>
    AsyncComputeSelected,

    /// <summary>The async request was demoted because no legal overlap window was found.</summary>
    NoLegalOverlapWindow,
}

/// <summary>
/// Diagnostic description of one structural command-buffer boundary adjacent to a pass.
/// </summary>
public readonly struct PassSyncBoundaryDumpInfo
{
    /// <summary>Type of the command buffer that ends at the boundary.</summary>
    public CommandQueueType SourceType
    {
        get; init;
    }

    /// <summary>Type of the command buffer that begins at the boundary.</summary>
    public CommandQueueType DestinationType
    {
        get; init;
    }

    /// <summary>Relative producer command-buffer IDs required by the destination.</summary>
    public int[] ProducerCommandBufferIds
    {
        get; init;
    }

    /// <summary>Command-buffer types corresponding to <see cref="ProducerCommandBufferIds"/>.</summary>
    public CommandQueueType[] ProducerTypes
    {
        get; init;
    }
}

public readonly struct HeapBlockDumpInfo
{
    public ulong Offset
    {
        get; init;
    }

    public ulong Size
    {
        get; init;
    }

    public bool IsFree
    {
        get; init;
    }

    // Resources occupying this memory block
    public List<int> AliasedLogicalResources
    {
        get; init;
    }
}

public readonly struct PassDumpInfo
{
    public int Index
    {
        get; init;
    }

    public string Name
    {
        get; init;
    }

    public RenderPassType Type
    {
        get; init;
    }

    public bool IsCulled
    {
        get; init;
    }

    public bool AsyncCompute
    {
        get; init;
    }

    public bool AsyncRequested
    {
        get; init;
    }

    public CommandQueueType? EffectiveQueue
    {
        get; init;
    }

    /// <summary>Explains the effective command-buffer assignment.</summary>
    public RGQueueDecision QueueDecision
    {
        get; init;
    }

    /// <summary>Structural command-buffer boundary immediately before this pass, when present.</summary>
    public PassSyncBoundaryDumpInfo? SyncBoundaryBefore
    {
        get; init;
    }

    /// <summary>Structural command-buffer boundary immediately after this pass, when present.</summary>
    public PassSyncBoundaryDumpInfo? SyncBoundaryAfter
    {
        get; init;
    }

    public int NativePassIndex
    {
        get; init;
    }

    public List<int> ResourceReads
    {
        get; init;
    }

    public List<int> ResourceWrites
    {
        get; init;
    }

    public List<int> ResourceCreates
    {
        get; init;
    }
}

public readonly struct ResourceDumpInfo
{
    public int LogicalResourceId
    {
        get; init;
    }

    public Handle<GPUResource> BackingResource
    {
        get; init;
    }

    public string Name
    {
        get; init;
    }

    public RGResourceType Type
    {
        get; init;
    }

    public ulong SizeInBytes
    {
        get; init;
    }

    public bool IsImported
    {
        get; init;
    }

    public bool IsExtracted
    {
        get; init;
    }

    public ulong HeapOffset
    {
        get; init;
    }

    public int FirstUsePass
    {
        get; init;
    }

    public int LastUsePass
    {
        get; init;
    }

    public int[] ProducerPass
    {
        get; init;
    }

    public int[] ConsumerPasses
    {
        get; init;
    }

    // Other resources sharing the same heap offset range
    public List<int> AliasedWithResources
    {
        get; init;
    }
}

/// <summary>
/// Decoded payload of a <see cref="RGExecutionOpType.CommandBufferSyncPoint"/> command-stream entry.
/// </summary>
/// <remarks>
/// <para>The span points directly into the originating command bytes and is only valid for the lifetime of that buffer.</para>
/// <para>
/// Relative command-buffer IDs are assigned in stream order: the first command buffer has ID 0 and each
/// <see cref="RGExecutionOpType.CommandBufferSyncPoint"/> ends command buffer N and starts command buffer N + 1.
/// Every element of <see cref="ProducerCommandBufferIds"/> must therefore be strictly less than N + 1.
/// </para>
/// </remarks>
internal readonly ref struct RGSyncMarker
{
    /// <summary>Queue type of the command buffer that begins after this boundary.</summary>
    public readonly CommandQueueType NextCommandBufferType;

    /// <summary>
    /// Relative IDs of command buffers that must complete before the next command buffer may execute.
    /// Each ID is strictly less than the implicit ID assigned to the next command buffer.
    /// </summary>
    public readonly ReadOnlySpan<int> ProducerCommandBufferIds;

    public RGSyncMarker(CommandQueueType nextCommandBufferType, ReadOnlySpan<int> producerCommandBufferIds)
    {
        NextCommandBufferType = nextCommandBufferType;
        ProducerCommandBufferIds = producerCommandBufferIds;
    }
}

/// <summary>
/// Helpers for writing and reading <see cref="RGExecutionOpType.CommandBufferSyncPoint"/> entries in the
/// binary command stream used by <see cref="RenderGraphCompiler"/> and <see cref="RenderGraphExecutor"/>.
/// </summary>
internal static class RGCommandStream
{
    /// <summary>
    /// Writes a <see cref="RGExecutionOpType.CommandBufferSyncPoint"/> entry into <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">Destination buffer writer.</param>
    /// <param name="nextCommandBufferType">Queue type for the command buffer that starts after this boundary.</param>
    /// <param name="producerIds">Relative IDs of producer command buffers that the next command buffer depends on.</param>
    /// <param name="nextCommandBufferId">
    /// The implicit relative ID that will be assigned to the command buffer that starts after this marker.
    /// Used to validate that every element of <paramref name="producerIds"/> is a strictly earlier command buffer.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when any producer ID is greater than or equal to <paramref name="nextCommandBufferId"/>, or when
    /// producer IDs contain duplicates.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteSyncMarker(
        ref BufferWriter writer,
        CommandQueueType nextCommandBufferType,
        ReadOnlySpan<int> producerIds,
        int nextCommandBufferId)
    {
        ValidateProducerIds(producerIds, nextCommandBufferId);

        writer.Write(RGExecutionOpType.CommandBufferSyncPoint);
        writer.Write(nextCommandBufferType);
        writer.Write(producerIds.Length);
        writer.WriteSpan(producerIds);
    }

    /// <summary>
    /// Reads a <see cref="RGExecutionOpType.CommandBufferSyncPoint"/> payload from <paramref name="reader"/>.
    /// The opcode byte itself must already have been consumed by the caller.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RGSyncMarker ReadSyncMarker(ref SpanReader reader)
    {
        var nextType = reader.Read<CommandQueueType>();
        var count = reader.Read<int>();
        var ids = reader.ReadSpan<int>(count);
        return new RGSyncMarker(nextType, ids);
    }

    /// <summary>
    /// Validates that producer IDs form a legal dependency set for a sync marker whose next command buffer will
    /// receive the relative ID <paramref name="nextCommandBufferId"/>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when any ID is out of range or when the set contains duplicates.
    /// </exception>
    public static void ValidateProducerIds(ReadOnlySpan<int> producerIds, int nextCommandBufferId)
    {
        for (var i = 0; i < producerIds.Length; i++)
        {
            var id = producerIds[i];
            if (id < 0 || id >= nextCommandBufferId)
            {
                throw new ArgumentException(
                    $"Producer command-buffer ID {id} is out of range. " +
                    $"All IDs must be in [0, {nextCommandBufferId - 1}].");
            }

            for (var j = i + 1; j < producerIds.Length; j++)
            {
                if (producerIds[j] == id)
                {
                    throw new ArgumentException(
                        $"Duplicate producer command-buffer ID {id} in sync marker.");
                }
            }
        }
    }
}
