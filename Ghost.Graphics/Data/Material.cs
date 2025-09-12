using Ghost.Graphics.D3D12;
using Ghost.Graphics.RHI;
using System.Numerics;
using System.Runtime.CompilerServices;
using Win32.Graphics.Direct3D12;

namespace Ghost.Graphics.Data;

/// <summary>
/// Material implementation for bindless rendering with SM 6.6 support
/// </summary>
public unsafe class Material : IDisposable
{
    private readonly CBufferCache[] _cbufferCaches;

    private bool _disposed;

    internal ReadOnlySpan<CBufferCache> CBufferCaches => _cbufferCaches;

    public Shader Shader
    {
        get; set;
    }

    public Material(Shader shader)
    {
        Shader = shader;

        if (shader.ConstantBuffers.Count > 0)
        {
            var maxSlot = shader.ConstantBuffers.Max(cb => cb.RegisterSlot);
            _cbufferCaches = new CBufferCache[maxSlot + 1];

            foreach (var cbufferInfo in shader.ConstantBuffers)
            {
                _cbufferCaches[cbufferInfo.RegisterSlot] = new CBufferCache(cbufferInfo.Size);
            }
        }
        else
        {
            _cbufferCaches = Array.Empty<CBufferCache>();
        }
    }

    /// <summary>
    /// Sets a float property in the material's constant buffer.
    /// </summary>
    /// <param name="propertyId">The ID of the property to set.</param>
    /// <param name="value">The value to set for the property.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetFloat(int propertyId, in float value)
    {
        WriteToCache(propertyId, in value);
    }

    /// <summary>
    /// Sets a float property in the material's constant buffer.
    /// </summary>
    /// <param name="propertyName">The name of the property to set.</param>
    /// <param name="value">The value to set for the property.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetFloat(string propertyName, in float value)
    {
        SetFloat(Shader.GetPropertyId(propertyName), in value);
    }

    /// <summary>
    /// Sets a uint property in the material's constant buffer (useful for texture indices).
    /// </summary>
    /// <param name="propertyId">The ID of the property to set.</param>
    /// <param name="value">The value to set for the property.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUInt(int propertyId, in uint value)
    {
        WriteToCache(propertyId, in value);
    }

    /// <summary>
    /// Sets a uint property in the material's constant buffer (useful for texture indices).
    /// </summary>
    /// <param name="propertyName">The name of the property to set.</param>
    /// <param name="value">The value to set for the property.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUInt(string propertyName, in uint value)
    {
        SetUInt(Shader.GetPropertyId(propertyName), in value);
    }

    /// <summary>
    /// Sets a Vector property in the material's constant buffer.
    /// </summary>
    /// <param name="propertyId">The ID of the property to set.</param>
    /// <param name="value">The value to set for the property.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetVector(int propertyId, in Vector4 value)
    {
        WriteToCache(propertyId, in value);
    }

    /// <summary>
    /// Sets a Vector property in the material's constant buffer.
    /// </summary>
    /// <param name="propertyName">The name of the property to set.</param>
    /// <param name="value">The value to set for the property.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetVector(string propertyName, in Vector4 value)
    {
        SetVector(Shader.GetPropertyId(propertyName), in value);
    }

    /// <summary>
    /// Sets a Matrix property in the material's constant buffer.
    /// </summary>
    /// <param name="propertyId">The ID of the property to set.</param>
    /// <param name="value">The value to set for the property.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetMatrix(int propertyId, in Matrix4x4 value)
    {
        WriteToCache(propertyId, in value);
    }

    /// <summary>
    /// Sets a Matrix property in the material's constant buffer.
    /// </summary>
    /// <param name="propertyName">The name of the property to set.</param>
    /// <param name="value">The value to set for the property.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetMatrix(string propertyName, in Matrix4x4 value)
    {
        SetMatrix(Shader.GetPropertyId(propertyName), in value);
    }

    /// <summary>
    /// Sets a texture index for a shader property (for bindless texture access)
    /// </summary>
    /// <param name="propertyName">The name of the shader property (e.g., "_TextureIndex1")</param>
    /// <param name="texture">The bindless texture to reference</param>
    public void SetTexture(string propertyName, Texture2D texture)
    {
        SetUInt(propertyName, texture.DescriptorIndex);
    }

    /// <summary>
    /// Sets the mesh buffer indices for bindless vertex and index buffer access
    /// </summary>
    /// <param name="mesh">The mesh whose buffer indices to set</param>
    /// <param name="vertexBufferIndexProperty">The name of the vertex buffer index property (e.g., "_VertexBufferIndex")</param>
    /// <param name="indexBufferIndexProperty">The name of the index buffer index property (e.g., "_IndexBufferIndex")</param>
    public void SetMeshBuffer(Mesh mesh, string vertexBufferIndexProperty = "_VertexBufferIndex", string indexBufferIndexProperty = "_IndexBufferIndex")
    {
        SetUInt(vertexBufferIndexProperty, mesh.VertexBufferDescriptorIndex);
        SetUInt(indexBufferIndexProperty, mesh.IndexBufferDescriptorIndex);
    }

    private unsafe void WriteToCache<T>(int propertyId, in T value)
        where T : unmanaged
    {
        if (propertyId == -1)
        {
            throw new ArgumentException("Property ID is invalid.");
        }

        var propInfo = Shader.Properties[propertyId];
        if (propInfo.Size != sizeof(T))
        {
            throw new ArgumentException($"Property '{propInfo.Name}' has a size mismatch. Expected {propInfo.Size} bytes, but got {sizeof(T)} bytes.");
        }

        var cache = _cbufferCaches[propInfo.CBufferIndex];

        Unsafe.WriteUnaligned(ref cache.CpuData[(int)propInfo.ByteOffset], value);
    }

    /// <summary>
    /// Uploads the material data to the GPU.
    /// </summary>
    public void UploadMaterialData()
    {
        foreach (var cache in _cbufferCaches)
        {
            cache.UploadToGpu();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var cache in _cbufferCaches)
        {
            cache.Dispose();
        }

        // NOTE: We don't dispose the textures here as they might be shared
        // The user is responsible for disposing BindlessTexture2D instances

        GC.SuppressFinalize(this);

        _disposed = true;
    }
}