using Ghost.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.RenderGraphModule;

internal enum RenderGraphResourceType : int
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
        return HashCode.Combine(viewportWidth, viewportHeight);
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
        return new Identifier<RGResource>(texture.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Identifier<RGResource> AsResource(this Identifier<RGBuffer> buffer)
    {
        return new Identifier<RGResource>(buffer.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Identifier<RGTexture> AsTexture(this Identifier<RGResource> resource)
    {
        return new Identifier<RGTexture>(resource.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Identifier<RGBuffer> AsBuffer(this Identifier<RGResource> resource)
    {
        return new Identifier<RGBuffer>(resource.Value);
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

/// <summary>
/// Tracks buffer access information.
/// </summary>
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

/// <summary>
/// Information about a render target attachment in a native render pass.
/// </summary>
internal struct RenderTargetInfo
{
    public Identifier<RGTexture> texture;
    public AccessFlags access;
    public AttachmentLoadOp loadOp;
    public AttachmentStoreOp storeOp;
    public Color128 clearColor;
}

/// <summary>
/// Information about a depth-stencil attachment in a native render pass.
/// </summary>
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
