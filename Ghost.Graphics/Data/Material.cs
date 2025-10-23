using Ghost.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.Data;

internal struct CBufferCache : IResourceReleasable
{
    private UnsafeArray<byte> _cpuData;
    private Handle<GraphicsBuffer> _gpuResource;
    private uint _alignedSize;

    public readonly UnsafeArray<byte> CpuData => _cpuData;
    public readonly Handle<GraphicsBuffer> GpuResource => _gpuResource;
    public readonly uint AlignedSize => _alignedSize;

    public unsafe CBufferCache(Handle<GraphicsBuffer> buffer, uint bufferSize)
    {
        _alignedSize = (bufferSize + 255u) & ~255u;

        _cpuData = new((int)AlignedSize, Allocator.Persistent);
        _gpuResource = buffer;
    }

    public void ReleaseResource(IResourceDatabase database)
    {
        _cpuData.Dispose();

        database.ReleaseResource(GpuResource.AsResource());
        _gpuResource = Handle<GraphicsBuffer>.Invalid;

        _alignedSize = 0;
    }
}

public struct Material : IResourceReleasable, IHandleType
{
    private Identifier<Shader> _shader;
    private UnsafeArray<CBufferCache> _materialPropertiesCache; // One per shader pass

    public readonly Identifier<Shader> Shader => _shader;

    internal ref CBufferCache GetPassCache(int passIndex)
    {
        return ref _materialPropertiesCache[passIndex];
    }

    public void SetShader(Identifier<Shader> shaderId, IResourceAllocator allocator, IResourceDatabase database)
    {
        if (!shaderId.IsValid)
        {
            throw new ArgumentException("Shader ID is invalid.");
        }

        _shader = shaderId;

        var shader = database.GetShaderReference(shaderId);
        _materialPropertiesCache = new UnsafeArray<CBufferCache>(shader.PassCount, Allocator.Persistent);
        for (var i = 0; i < shader.PassCount; i++)
        {
            var pass = database.GetShaderPass(shader.GetPassKey(i));
            var cbufferInfo = pass.PassPropertyInfo;

            var desc = new BufferDesc
            {
                Size = cbufferInfo.Size,
                Usage = BufferUsage.Constant,
                MemoryType = ResourceMemoryType.Default,
            };

            var buffer = allocator.CreateBuffer(ref desc);
            _materialPropertiesCache[i] = new CBufferCache(buffer, cbufferInfo.Size);
        }
    }

    void IResourceReleasable.ReleaseResource(IResourceDatabase database)
    {
        foreach (var cache in _materialPropertiesCache)
        {
            cache.ReleaseResource(database);
        }

        _materialPropertiesCache.Dispose();
    }
}

public ref struct MaterialAccessor
{
    private ref Material _materialData;
    private Shader _shader;

    private readonly IResourceDatabase _resourceDatabase;

    public MaterialAccessor(Handle<Material> material, IResourceDatabase resourceDatabase)
    {
        _resourceDatabase = resourceDatabase;

        _materialData = ref resourceDatabase.GetMaterialReference(material);
        _shader = resourceDatabase.GetShaderReference(_materialData.Shader);
    }

    private readonly unsafe void WriteToCache<T>(string propertyName, in T value)
        where T : unmanaged
    {
        foreach (var index in _shader.GetPropertyPassIndices(propertyName))
        {
            var passKey = _shader.GetPassKey(index);
            var pass = _resourceDatabase.GetShaderPass(passKey);
            var propertyInfo = pass.GetPropertyInfo(propertyName);

            if (propertyInfo.Size != sizeof(T))
            {
                throw new ArgumentException($"Property '{propertyName}' has a size mismatch. Expected {propertyInfo.Size} bytes, but got {sizeof(T)} bytes.");
            }

            ref var cache = ref _materialData.GetPassCache(index);
            Unsafe.WriteUnaligned(ref cache.CpuData[propertyInfo.ByteOffset], value);
        }
    }

    /// <summary>
    /// Sets a float property in the material's constant buffer.
    /// </summary>
    /// <param name="propertyName">The name of the property to set.</param>
    /// <param name="value">The value to set for the property.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void SetFloat(string propertyName, in float value)
    {
        WriteToCache(propertyName, in value);
    }

    /// <summary>
    /// Sets a uint property in the material's constant buffer (useful for texture indices).
    /// </summary>
    /// <param name="propertyName">The name of the property to set.</param>
    /// <param name="value">The value to set for the property.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void SetUInt(string propertyName, in uint value)
    {
        WriteToCache(propertyName, in value);
    }

    /// <summary>
    /// Sets a Vector property in the material's constant buffer.
    /// </summary>
    /// <param name="propertyName">The name of the property to set.</param>
    /// <param name="value">The value to set for the property.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void SetVector(string propertyName, in float4 value)
    {
        WriteToCache(propertyName, in value);
    }

    /// <summary>
    /// Sets a Matrix property in the material's constant buffer.
    /// </summary>
    /// <param name="propertyName">The name of the property to set.</param>
    /// <param name="value">The value to set for the property.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void SetMatrix(string propertyName, in float4x4 value)
    {
        WriteToCache(propertyName, in value);
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
        for (var i = 0; i < _shader.PassCount; i++)
        {
            ref var cache = ref _materialData.GetPassCache(i);
            cmb.Upload(cache.GpuResource, cache.CpuData.AsSpan());
        }
    }
}