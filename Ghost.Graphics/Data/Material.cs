using Ghost.Graphics.Contracts;
using Ghost.Graphics.Shading;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.Data;

public class Material : IDisposable
{
    private readonly Dictionary<string, CBufferCache> _cbufferCaches;

    private bool _disposed;

    public Shader Shader
    {
        get;
        set;
    }

    public Material(Shader shader)
    {
        Shader = shader;

        _cbufferCaches = new();
        foreach (var cbufferInfo in shader.ConstantBuffers.Values)
        {
            _cbufferCaches.Add(cbufferInfo.Name, new CBufferCache(cbufferInfo.Size));
        }
    }

    ~Material()
    {
        Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetFloat(string propertyName, in float value)
    {
        WriteToCache(propertyName, in value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetVector(string propertyName, in Vector4 value)
    {
        WriteToCache(propertyName, in value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetMatrix(string propertyName, in Matrix4x4 value)
    {
        WriteToCache(propertyName, in value);
    }

    private unsafe void WriteToCache<T>(string propertyName, in T value) where T : unmanaged
    {
        if (!Shader.Properties.TryGetValue(propertyName, out var propInfo))
        {
            throw new ArgumentException($"Property '{propertyName}' does not exist in the shader.", nameof(propertyName));
        }

        if (propInfo.Size != sizeof(T))
        {
            throw new ArgumentException($"Property '{propertyName}' has a size mismatch. Expected {sizeof(T)} bytes, but got {propInfo.Size} bytes.", nameof(propertyName));
        }

        var cache = _cbufferCaches[propInfo.CBufferName];

        Unsafe.WriteUnaligned(ref cache.CpuData[(int)propInfo.ByteOffset], value);
    }

    public void UploadAndBind(ICommandBuffer cmb)
    {
        foreach (var cache in _cbufferCaches.Values)
        {
            cache.UploadToGpu();
        }

        foreach (var (name, cache) in _cbufferCaches)
        {
            var cbufferInfo = Shader.ConstantBuffers[name];
            cmb.SetGraphicsRootConstantBufferView(cbufferInfo.RegisterSlot, cache.GpuResource.GPUAddress);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var cache in _cbufferCaches.Values)
        {
            cache.Dispose();
        }

        _cbufferCaches.Clear();

        GC.SuppressFinalize(this);

        _disposed = true;
    }
}