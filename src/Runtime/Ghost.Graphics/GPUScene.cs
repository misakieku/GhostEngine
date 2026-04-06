using Ghost.Core;
using Ghost.Graphics.RHI;
using System.Diagnostics;

namespace Ghost.Graphics;

public unsafe class GPUScene : IDisposable
{
    private readonly IResourceAllocator _resourceAllocator;
    private readonly IResourceDatabase _resourceDatabase;

    private Handle<GPUBuffer> _sceneBuffer;

    private bool _disposed;

    internal GPUScene(IResourceAllocator resourceAllocator, IResourceDatabase resourceDatabase, ulong initialCount)
    {
        _resourceAllocator = resourceAllocator;
        _resourceDatabase = resourceDatabase;

        var bufferDesc = new BufferDesc
        {
            Size = initialCount * (ulong)sizeof(InstanceData),
            Stride = (uint)sizeof(InstanceData),
            Usage = BufferUsage.Structured | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource,
            HeapType = HeapType.Default,
        };

        _sceneBuffer = _resourceAllocator.CreateBuffer(in bufferDesc, "SceneBuffer");

        Debug.Assert(_sceneBuffer.IsValid, "Failed to create GPUScene buffer.");
    }

    ~GPUScene()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _resourceDatabase.ReleaseResource(_sceneBuffer.AsResource());

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
