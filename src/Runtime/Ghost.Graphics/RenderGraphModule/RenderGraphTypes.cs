using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Ghost.Graphics.RHI;
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
/// Render graph texture descriptor with support for relative sizing and clear operations.
/// </summary>
public record struct RGTextureDesc : IEquatable<RGTextureDesc>
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
    public TypelessFormatDesc typelessViewFormat;

    public bool clearAtFirstUse;
    public bool discardAtLastUse;

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
        TextureUsage usage = TextureUsage.RenderTarget | TextureUsage.ShaderResource,
        TypelessFormatDesc typelessViewFormat = default)
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
            clearDepth = 0.0f,
            clearStencil = 0,
            dimension = dimension,
            mipLevels = mipLevels,
            slice = slice,
            usage = usage,
            typelessViewFormat = typelessViewFormat
        };
    }

    public static RGTextureDesc AbsoluteDepth(
        uint width,
        uint height,
        TextureFormat format,
        float clearDepth = 0.0f,
        byte clearStencil = 0,
        bool clearAtFirstUse = true,
        bool discardAtLastUse = true,
        TextureUsage usage = TextureUsage.DepthStencil,
        TypelessFormatDesc typelessViewFormat = default)
    {
        return new RGTextureDesc
        {
            sizeMode = RGTextureSizeMode.Absolute,
            width = width,
            height = height,
            format = format,
            clearColor = default,
            clearDepth = clearDepth,
            clearStencil = clearStencil,
            clearAtFirstUse = clearAtFirstUse,
            discardAtLastUse = discardAtLastUse,
            dimension = TextureDimension.Texture2D,
            mipLevels = 1,
            slice = 1,
            usage = usage,
            typelessViewFormat = typelessViewFormat
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
        TextureUsage usage = TextureUsage.RenderTarget | TextureUsage.ShaderResource,
        TypelessFormatDesc typelessViewFormat = default)
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
            clearDepth = 0.0f,
            clearStencil = 0,
            dimension = dimension,
            mipLevels = mipLevels,
            slice = slice,
            usage = usage,
            typelessViewFormat = typelessViewFormat
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
        TextureUsage usage = TextureUsage.RenderTarget | TextureUsage.ShaderResource,
        TypelessFormatDesc typelessViewFormat = default)
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
            clearDepth = 0.0f,
            clearStencil = 0,
            dimension = dimension,
            mipLevels = mipLevels,
            slice = slice,
            usage = usage,
            typelessViewFormat = typelessViewFormat
        };
    }


    /// <summary>
    /// Creates a depth texture descriptor with relative dimensions.
    /// </summary>
    public static RGTextureDesc RelativeDepth(
        float scale,
        float clearDepth = 0.0f,
        byte clearStencil = 0,
        bool clearAtFirstUse = true,
        bool discardAtLastUse = true,
        TextureFormat format = TextureFormat.D32_Float,
        TextureUsage usage = TextureUsage.DepthStencil,
        TypelessFormatDesc typelessViewFormat = default)
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
            usage = usage,
            typelessViewFormat = typelessViewFormat
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
            Usage = usage,
            TypelessViewFormat = typelessViewFormat
        };
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
public enum RGFlags
{
    /// <summary>
    /// Default execution behavior without any special flags.
    /// </summary>
    Default = 0,
    /// <summary>
    /// Generate a detailed dump of the render graph execution for debugging and analysis.
    /// </summary>
    GenerateDump = 1 << 0,
    /// <summary>
    /// Record every structural segment on the Graphics queue and suppress internal cross-queue dependencies.
    /// </summary>
    ForceGraphics = 1 << 1,
    /// <summary>
    /// Prevent memory heap aliasing for resources, ensuring each resource has a unique memory allocation.
    /// </summary>
    NoAliasing = 1 << 2,
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

internal readonly ref struct RGSyncMarker
{
    public readonly CommandQueueType nextCommandBufferType;

    public readonly ReadOnlySpan<int> producerCommandBufferIds;

    public RGSyncMarker(CommandQueueType nextCommandBufferType, ReadOnlySpan<int> producerCommandBufferIds)
    {
        this.nextCommandBufferType = nextCommandBufferType;
        this.producerCommandBufferIds = producerCommandBufferIds;
    }
}

internal static class RGCommandStream
{
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RGSyncMarker ReadSyncMarker(ref SpanReader reader)
    {
        var nextType = reader.Read<CommandQueueType>();
        var count = reader.Read<int>();
        var ids = reader.ReadSpan<int>(count);
        return new RGSyncMarker(nextType, ids);
    }

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
