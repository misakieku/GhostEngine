using Ghost.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.Data;

public struct Material
{
    internal UnsafeArray<CBufferCache> _cBufferCaches;

    public Identifier<Shader> Shader
    {
        get; internal set;
    }
}

public ref struct MaterialAccessor
{
    private ref Material _materialData;
    private ref Shader _shader;

    public MaterialAccessor(ref Material materialData, IResourceDatabase resourceDatabase)
    {
        _materialData = ref materialData;
        _shader = ref resourceDatabase.GetShaderReference(materialData.Shader);
    }

    private readonly unsafe void WriteToCache<T>(int propertyId, in T value)
        where T : unmanaged
    {
        if (propertyId == -1)
        {
            throw new ArgumentException("Property ID is invalid.");
        }

        var propInfo = _shader.Properties[propertyId];
        if (propInfo.Size != sizeof(T))
        {
            throw new ArgumentException($"Property '{propertyId}' has a size mismatch. Expected {propInfo.Size} bytes, but got {sizeof(T)} bytes.");
        }

        var cache = _materialData._cBufferCaches[propInfo.CBufferIndex];

        Unsafe.WriteUnaligned(ref cache.CpuData[(int)propInfo.ByteOffset], value);
    }

    /// <summary>
    /// Sets a float property in the material's constant buffer.
    /// </summary>
    /// <param name="propertyId">The ID of the property to set.</param>
    /// <param name="value">The value to set for the property.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void SetFloat(int propertyId, in float value)
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
        SetFloat(_shader.GetPropertyId(propertyName), in value);
    }

    /// <summary>
    /// Sets a uint property in the material's constant buffer (useful for texture indices).
    /// </summary>
    /// <param name="propertyId">The ID of the property to set.</param>
    /// <param name="value">The value to set for the property.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void SetUInt(int propertyId, in uint value)
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
        SetUInt(_shader.GetPropertyId(propertyName), in value);
    }

    /// <summary>
    /// Sets a Vector property in the material's constant buffer.
    /// </summary>
    /// <param name="propertyId">The ID of the property to set.</param>
    /// <param name="value">The value to set for the property.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void SetVector(int propertyId, in Vector4 value)
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
        SetVector(_shader.GetPropertyId(propertyName), in value);
    }

    /// <summary>
    /// Sets a Matrix property in the material's constant buffer.
    /// </summary>
    /// <param name="propertyId">The ID of the property to set.</param>
    /// <param name="value">The value to set for the property.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void SetMatrix(int propertyId, in Matrix4x4 value)
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
        SetMatrix(_shader.GetPropertyId(propertyName), in value);
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
    public void SetMeshBuffer(MeshClass mesh, string vertexBufferIndexProperty = "_VertexBufferIndex", string indexBufferIndexProperty = "_IndexBufferIndex")
    {
        SetUInt(vertexBufferIndexProperty, mesh.VertexBufferDescriptorIndex);
        SetUInt(indexBufferIndexProperty, mesh.IndexBufferDescriptorIndex);
    }

    /// <summary>
    /// Uploads all cached material data to the GPU using the specified command buffer.
    /// </summary>
    /// <param name="cmb">The command buffer used to perform the upload operations to the GPU. Cannot be null.</param>
    public readonly void UploadMaterialData(ICommandBuffer cmb)
    {
        foreach (var cache in _materialData._cBufferCaches)
        {
            cmb.Upload(cache.GpuResource, cache.CpuData.AsSpan());
        }
    }
}

/// <summary>
/// Material implementation for bindless rendering with SM 6.6 support
/// </summary>
public unsafe class MaterialClass : IDisposable
{
    private readonly UnsafeArray<CBufferCache> _cbufferCaches;

    private bool _disposed;

    internal ReadOnlySpan<CBufferCache> CBufferCaches => _cbufferCaches.AsSpan();

    public Identifier<Shader> Shader
    {
        get; set;
    }

    public MaterialClass(Identifier<Shader> shader, IResourceAllocator resourceAllocator, IResourceDatabase resourceDatabase)
    {
        Shader = shader;

        var shaderResource = resourceDatabase.GetShader(shader);

        if (shaderResource.ConstantBuffers.Count > 0)
        {
            var maxSlot = shaderResource.ConstantBuffers.Max(cb => cb.RegisterSlot);
            _cbufferCaches = new UnsafeArray<CBufferCache>((int)maxSlot + 1, Allocator.Persistent);

            foreach (var cbufferInfo in shaderResource.ConstantBuffers)
            {
                var desc = new BufferDesc
                {
                    Size = cbufferInfo.Size,
                    Usage = BufferUsage.Constant,
                    MemoryType = MemoryType.Default,
                };

                var buffer = resourceAllocator.CreateBuffer(in desc);

                _cbufferCaches[cbufferInfo.RegisterSlot] = new CBufferCache(buffer, cbufferInfo.Size);
            }
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
    public void SetMeshBuffer(MeshClass mesh, string vertexBufferIndexProperty = "_VertexBufferIndex", string indexBufferIndexProperty = "_IndexBufferIndex")
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