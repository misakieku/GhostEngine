using Ghost.Core;
using System.Runtime.CompilerServices;

namespace Ghost.RenderGraph.Concept;

internal enum RenderGraphResourceType : int
{
    Texture,
    Buffer,
    // AccelerationStructure,
    Count
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

    public TextureAccess(Identifier<RGTexture> id, AccessFlags accessFlags)
    {
        this.id = id;
        this.accessFlags = accessFlags;
    }
}

/// <summary>
/// Texture formats supported by the render graph.
/// </summary>
public enum TextureFormat : int
{
    RGBA8,
    RGBA16F,
    RGBA32F,
    Depth32F,
    Depth24Stencil8
}

/// <summary>
/// Descriptor for creating a texture resource.
/// </summary>
public readonly struct TextureDescriptor : IEquatable<TextureDescriptor>
{
    public readonly int width;
    public readonly int height;
    public readonly TextureFormat format;
    public readonly string name;

    public TextureDescriptor(int width, int height, TextureFormat format, string name)
    {
        this.width = width;
        this.height = height;
        this.format = format;
        this.name = name;
    }

    public readonly bool Equals(TextureDescriptor other)
    {
        return width == other.width &&
        height == other.height &&
        format == other.format &&
        name == other.name;
    }

    public override readonly bool Equals(object? obj) => obj is TextureDescriptor other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(width, height, format, name);

    public static bool operator ==(TextureDescriptor left, TextureDescriptor right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TextureDescriptor left, TextureDescriptor right)
    {
        return !(left == right);
    }
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

public readonly struct BufferDescriptor : IEquatable<BufferDescriptor>
{
    public readonly uint sizeInBytes;
    public readonly uint stride;
    public readonly BufferUsage usage;
    public readonly string name;

    public BufferDescriptor(uint sizeInBytes, uint stride, BufferUsage usage, string name)
    {
        this.sizeInBytes = sizeInBytes;
        this.stride = stride;
        this.usage = usage;
        this.name = name;
    }

    public readonly bool Equals(BufferDescriptor other)
    {
        return sizeInBytes == other.sizeInBytes &&
               stride == other.stride &&
               usage == other.usage &&
               name == other.name;
    }

    public override readonly bool Equals(object? obj) => obj is BufferDescriptor other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(sizeInBytes, name);

    public static bool operator ==(BufferDescriptor left, BufferDescriptor right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(BufferDescriptor left, BufferDescriptor right)
    {
        return !(left == right);
    }
}

/// <summary>
/// Base interface for pass data that can be stored in the blackboard.
/// </summary>
public interface IPassData
{
}
