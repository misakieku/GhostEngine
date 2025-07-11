using Ghost.Graphics.D3D12;
using Ghost.Graphics.Shading;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.Data;

public unsafe class Material : IDisposable
{
    private readonly CBufferCache[] _cbufferCaches;
    private readonly Texture2D?[] _textures;
    private readonly Dictionary<string, int> _textureNameToSlotMap;

    private bool _disposed;

    public Shader Shader
    {
        get;
        set;
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

        // Initialize texture storage
        if (shader.Textures.Count > 0)
        {
            var maxTextureSlot = shader.Textures.Max(t => t.RegisterSlot);
            _textures = new Texture2D?[maxTextureSlot + 1];
            _textureNameToSlotMap = new Dictionary<string, int>();

            foreach (var textureInfo in shader.Textures)
            {
                _textureNameToSlotMap.Add(textureInfo.Name, (int)textureInfo.RegisterSlot);
            }
        }
        else
        {
            _textures = Array.Empty<Texture2D?>();
            _textureNameToSlotMap = new Dictionary<string, int>();
        }
    }

    ~Material()
    {
        Dispose();
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
    /// <param name="propertyName">The ID of the property to set.</param>
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
    /// Sets a texture property in the material.
    /// </summary>
    /// <param name="textureId">The ID of the texture to set.</param>
    /// <param name="texture">The texture to set.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetTexture(int textureId, Texture2D? texture)
    {
        if (textureId == -1)
        {
            throw new ArgumentException("Texture ID is invalid.");
        }

        if (textureId >= _textures.Length)
        {
            throw new ArgumentException($"Texture ID {textureId} is out of range.");
        }

        _textures[textureId] = texture;
    }

    /// <summary>
    /// Sets a texture property in the material.
    /// </summary>
    /// <param name="textureName">The name of the texture to set.</param>
    /// <param name="texture">The texture to set.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetTexture(string textureName, Texture2D? texture)
    {
        if (!_textureNameToSlotMap.TryGetValue(textureName, out var slot))
        {
            throw new ArgumentException($"Texture '{textureName}' not found in shader.");
        }

        _textures[slot] = texture;
    }

    /// <summary>
    /// Gets a texture property from the material.
    /// </summary>
    /// <param name="textureId">The ID of the texture to get.</param>
    /// <returns>The texture, or null if not set.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Texture2D? GetTexture(int textureId)
    {
        if (textureId == -1 || textureId >= _textures.Length)
        {
            return null;
        }

        return _textures[textureId];
    }

    /// <summary>
    /// Gets a texture property from the material.
    /// </summary>
    /// <param name="textureName">The name of the texture to get.</param>
    /// <returns>The texture, or null if not set.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Texture2D? GetTexture(string textureName)
    {
        if (!_textureNameToSlotMap.TryGetValue(textureName, out var slot))
        {
            return null;
        }

        return _textures[slot];
    }

    private unsafe void WriteToCache<T>(int propertyId, in T value) where T : unmanaged
    {
        if (propertyId == -1)
        {
            throw new ArgumentException("Property ID is invalid.");
        }

        var propInfo = Shader.Properties[propertyId];
        if (propInfo.Size != sizeof(T))
        {
            throw new ArgumentException($"Property '{propInfo.Name}' has a size mismatch. Expected {sizeof(T)} bytes, but got {propInfo.Size} bytes.");
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

    internal void Bind(CommandList cmd)
    {
        // Bind constant buffers
        foreach (var cbufferInfo in Shader.ConstantBuffers)
        {
            var cache = _cbufferCaches[cbufferInfo.RegisterSlot];
            cmd.SetGraphicsRootConstantBufferView(cbufferInfo.RegisterSlot, cache.GpuResource.GPUAddress);
        }

        // Bind textures using descriptor table
        if (Shader.Textures.Count > 0)
        {
            // Get the first texture info to determine the root parameter index
            var textureInfo = Shader.Textures[0];
            var texture = _textures[0]; // Get the first texture
            
            if (texture != null)
            {
                // Set descriptor table for SRVs
                cmd.NativeCommandList.Ptr->SetGraphicsRootDescriptorTable(textureInfo.RootParameterIndex, texture.SRVDescriptor.GpuHandle);
            }
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

        GC.SuppressFinalize(this);

        _disposed = true;
    }
}