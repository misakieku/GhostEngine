using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Graphics.Data;

internal readonly struct TextureInfo
{
    public uint RegisterSlot
    {
        get; init;
    }

    public uint RootParameterIndex
    {
        get; init;
    }
}

internal readonly struct PropertyInfo
{
    public uint CBufferIndex
    {
        get; init;
    }

    public uint ByteOffset
    {
        get; init;
    }

    public uint Size
    {
        get; init;
    }
}

internal readonly struct CBufferInfo
{
    public uint Size
    {
        get; init;
    }

    public uint RegisterSlot
    {
        get; init;
    }
}

internal struct ShaderPass
{
}

/// <summary>
/// A representation of a GPU shader, including its metadata about its resources.
/// </summary>

// TODO: Multi pass and keyword support
public struct Shader : IIdentifierType
{
    private UnsafeList<CBufferInfo> _constantBuffers;
    private UnsafeList<PropertyInfo> _properties;
    // TODO: Possible to move this to unmanaged heap?
    private Dictionary<string, int> _propertyNameToIdMap;

    private bool _disposed;

    internal readonly UnsafeList<CBufferInfo> ConstantBuffers => _constantBuffers;
    internal readonly UnsafeList<PropertyInfo> Properties => _properties;
    internal readonly Dictionary<string, int> PropertyNameToIdMap => _propertyNameToIdMap;
    public Shader()
    {
        _constantBuffers = new(8, Allocator.Persistent);
        _properties = new(8, Allocator.Persistent);
        _propertyNameToIdMap = new(8);

        _disposed = false;
    }

    /// <summary>
    /// Gets a unique, stable ID for a shader property.
    /// </summary>
    /// <param name="propertyName">The name of the shader property.</param>
    /// <returns>The integer ID of the property, or -1 if not found.</returns>
    public readonly int GetPropertyId(string propertyName)
    {
        return _propertyNameToIdMap.TryGetValue(propertyName, out var id) ? id : -1;
    }

    internal void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _constantBuffers.Dispose();
        _properties.Dispose();

        _propertyNameToIdMap.Clear();
        _propertyNameToIdMap = null!;

        _disposed = true;
    }
}