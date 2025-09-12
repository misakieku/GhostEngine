namespace Ghost.Graphics.Data;

internal readonly struct TextureInfo
{
    public required string Name
    {
        get; init;
    }

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
    public required string Name
    {
        get; init;
    }

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
    public required string Name
    {
        get; init;
    }

    public uint Size
    {
        get; init;
    }

    public uint RegisterSlot
    {
        get; init;
    }
}

/// <summary>
/// Bindless shader implementation using SM 6.6 with ResourceDescriptorHeap
/// and D3D12_ROOT_SIGNATURE_FLAG_CBV_SRV_UAV_HEAP_DIRECTLY_INDEXED
/// Enhanced to support both bindless and regular texture binding for hybrid materials
/// </summary>
public unsafe class Shader : IDisposable
{
    private readonly string _source;

    private readonly List<CBufferInfo> _constantBuffers = new();
    private readonly List<PropertyInfo> _properties = new();
    private readonly List<TextureInfo> _regularTextures = new(); // Add regular texture support
    private readonly Dictionary<string, int> _propertyNameToIdMap = new();

    private bool _disposed;

    internal string Source => _source;

    internal List<CBufferInfo> ConstantBuffers => _constantBuffers;
    internal List<PropertyInfo> Properties => _properties;
    internal List<TextureInfo> RegularTextures => _regularTextures;
    internal Dictionary<string, int> PropertyNameToIdMap => _propertyNameToIdMap;

    internal Shader(string shaderCode)
    {
        _source = shaderCode;
    }

    /// <summary>
    /// Gets a unique, stable ID for a shader property.
    /// </summary>
    /// <param name="propertyName">The name of the property (e.g., "_Color").</param>
    /// <returns>The integer ID of the property, or -1 if not found.</returns>
    public int GetPropertyId(string propertyName)
    {
        return _propertyNameToIdMap.TryGetValue(propertyName, out var id) ? id : -1;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _constantBuffers.Clear();
        _properties.Clear();
        _propertyNameToIdMap.Clear();
        _regularTextures.Clear();

        GC.SuppressFinalize(this);

        _disposed = true;
    }
}