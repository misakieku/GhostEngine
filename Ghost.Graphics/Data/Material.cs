using Ghost.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.Data;

public struct Material : IHandleType
{
    internal CBufferCache _materialPropertiesCache;

    public Identifier<Shader> Shader
    {
        get; internal set;
    }

    internal readonly void Dispose()
    {
        _materialPropertiesCache.Dispose();
    }
}

public ref struct MaterialAccessor
{
    private ref Material _materialData;
    private ref Shader _shader;

    private readonly IResourceDatabase _resourceDatabase;

    internal MaterialAccessor(ref Material materialData, IResourceDatabase resourceDatabase)
    {
        _resourceDatabase = resourceDatabase;

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

        var cache = _materialData._materialPropertiesCache[propInfo.CBufferIndex];

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
    public readonly void SetFloat(string propertyName, in float value)
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
    public readonly void SetUInt(string propertyName, in uint value)
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
    public readonly void SetVector(string propertyName, in Vector4 value)
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
    public readonly void SetMatrix(string propertyName, in Matrix4x4 value)
    {
        SetMatrix(_shader.GetPropertyId(propertyName), in value);
    }

    /// <summary>
    /// Sets a texture index for a shader property (for bindless texture access)
    /// </summary>
    /// <param name="propertyName">The name of the shader property (e.g., "_TextureIndex1")</param>
    /// <param name="texture">The bindless texture to reference</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void SetTextureBindless(string propertyName, Handle<Texture> texture)
    {
        var bindlessIndex = _resourceDatabase.GetBindlessIndex(texture.AsResource());
        if (bindlessIndex == -1)
        {
            throw new ArgumentException("The provided texture does not have a valid bindless index. Ensure the texture is created with bindless support.");
        }

        SetUInt(propertyName, (uint)bindlessIndex);
    }

    /// <summary>
    /// Sets the mesh buffer indices for bindless vertex and index buffer access
    /// </summary>
    /// <param name="mesh">The mesh whose buffer indices to set</param>
    /// <param name="vertexBufferIndexProperty">The name of the vertex buffer index property</param>
    /// <param name="indexBufferIndexProperty">The name of the index buffer index property</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void SetBufferBindless(string propertyName, Handle<GraphicsBuffer> buffer)
    {
        var bindlessIndex = _resourceDatabase.GetBindlessIndex(buffer.AsResource());
        if (bindlessIndex == -1)
        {
            throw new ArgumentException("The provided buffer does not have a valid bindless index. Ensure the buffer is created with bindless support.");
        }

        SetUInt(propertyName, (uint)bindlessIndex);
    }

    /// <summary>
    /// Uploads all cached material data to the GPU using the specified command buffer.
    /// </summary>
    /// <param name="cmb">The command buffer used to perform the upload operations to the GPU. Cannot be null.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void UploadMaterialData(ICommandBuffer cmb)
    {
        foreach (var cache in _materialData._materialPropertiesCache)
        {
            cmb.Upload(cache.GpuResource, cache.CpuData.AsSpan());
        }
    }
}