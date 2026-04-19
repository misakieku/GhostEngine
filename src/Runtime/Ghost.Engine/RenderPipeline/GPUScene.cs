using Ghost.Core;
using Ghost.Graphics.RHI;

namespace Ghost.Engine.RenderPipeline;

internal unsafe class GPUScene : IDisposable
{
    private readonly IResourceAllocator _resourceAllocator;
    private readonly IResourceDatabase _resourceDatabase;

    private Handle<GPUBuffer> _sceneBuffer;
    private Handle<GPUBuffer> _instanceCounterBuffer;
    private uint _instanceCount;
    private uint _capacity;

    private uint _requiredResize;
    private bool _disposed;

    public Handle<GPUBuffer> SceneBuffer => _sceneBuffer;
    public Handle<GPUBuffer> InstanceCounterBuffer => _instanceCounterBuffer;
    public uint InstanceCount => Volatile.Read(ref _instanceCount);

    internal GPUScene(IResourceAllocator resourceAllocator, IResourceDatabase resourceDatabase, uint initialCount)
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

        var counterBufferDesc = new BufferDesc
        {
            Size = sizeof(uint),
            Stride = sizeof(uint),
            Usage = BufferUsage.Structured | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource,
            HeapType = HeapType.Default,
        };

        _sceneBuffer = _resourceAllocator.CreateBuffer(in bufferDesc, "SceneBuffer");
        Logger.DebugAssert(_sceneBuffer.IsValid, "Failed to create GPUScene buffer.");

        _instanceCounterBuffer = _resourceAllocator.CreateBuffer(in counterBufferDesc, "SceneInstanceCounterBuffer");
        Logger.DebugAssert(_instanceCounterBuffer.IsValid, "Failed to create GPUScene instance counter buffer.");

        _capacity = initialCount;
    }

    ~GPUScene()
    {
        Dispose();
    }

    public void ResizeIfNeeded(ICommandBuffer cmd)
    {
        if (_requiredResize == 0)
        {
            return;
        }

        var newCapacity = Math.Max(_capacity * 2, _capacity + _requiredResize);

        var newBufferDesc = new BufferDesc
        {
            Size = newCapacity * (ulong)sizeof(InstanceData),
            Stride = (uint)sizeof(InstanceData),
            Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource,
            HeapType = HeapType.Default,
        };

        var newBuffer = _resourceAllocator.CreateBuffer(in newBufferDesc, "SceneBuffer_Resized");
        Logger.DebugAssert(newBuffer.IsValid);

        // Copy existing data to the new buffer
        cmd.CopyBuffer(newBuffer, _sceneBuffer, 0, 0, _instanceCount * (ulong)sizeof(InstanceData));

        // Replace old buffer with the new one
        _resourceDatabase.ReleaseResource(_sceneBuffer.AsResource());
        _sceneBuffer = newBuffer;
        _capacity = newCapacity;

        _requiredResize = 0;
    }

    public uint AddInstance()
    {
        if (Volatile.Read(ref _instanceCount) >= _capacity)
        {
            Interlocked.Increment(ref _requiredResize);
        }

        var index = Interlocked.Increment(ref _instanceCount);
        return index;
    }

    public uint RemoveInstance(uint index)
    {
        if (index < 0 || index >= _capacity)
        {
            return uint.MaxValue;
        }

        // Return the last index. We will swap the last instance data with the removed index on gpu to keep the buffer compact.
        var last = Interlocked.Decrement(ref _instanceCount);
        Logger.DebugAssert(last >= 0);

        return last;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _resourceDatabase.ReleaseResource(_sceneBuffer.AsResource());
        _resourceDatabase.ReleaseResource(_instanceCounterBuffer.AsResource());

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
