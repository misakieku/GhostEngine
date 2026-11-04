using Ghost.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.RenderPipeline;

public sealed unsafe class RayTracingViewHistory : IViewHistory
{
    private readonly IResourceAllocator _allocator;
    private readonly IResourceDatabase _database;

    public Handle<GPUBuffer> TLASBuffer { get; private set; }
    public Handle<GPUBuffer> TLASScratchBuffer { get; private set; }
    public Handle<GPUBuffer> InstanceDescBuffer { get; private set; }
    public ulong TLASBufferCapacity { get; private set; }
    public ulong TLASScratchCapacity { get; private set; }
    public ulong InstanceDescCapacity { get; private set; }
    public uint TLASBindlessIndex { get; set; }

    public RayTracingViewHistory(IResourceAllocator allocator, IResourceDatabase database)
    {
        _allocator = allocator;
        _database = database;
    }

    public void EnsureInstanceDescBuffer(ulong requiredSize)
    {
        if (InstanceDescBuffer.IsValid && InstanceDescCapacity >= requiredSize)
        {
            return;
        }

        if (InstanceDescBuffer.IsValid)
        {
            _database.ReleaseResource(InstanceDescBuffer.AsResource());
        }

        InstanceDescCapacity = Math.Max(requiredSize, Math.Max(InstanceDescCapacity * 2, 65536ul));
        var desc = new BufferDesc
        {
            Size = InstanceDescCapacity,
            Stride = (uint)sizeof(RaytracingInstanceDesc),
            Usage = BufferUsage.ShaderResource,
            HeapType = HeapType.Upload
        };

        InstanceDescBuffer = _allocator.CreateBuffer(in desc, "TLAS_InstanceDescBuffer");
    }

    public void EnsureTLASBuffers(ulong requiredResultSize, ulong requiredScratchSize)
    {
        if (!TLASBuffer.IsValid || TLASBufferCapacity < requiredResultSize)
        {
            if (TLASBuffer.IsValid)
            {
                _database.ReleaseResource(TLASBuffer.AsResource());
            }

            TLASBufferCapacity = Math.Max(requiredResultSize, Math.Max(TLASBufferCapacity * 2, 65536ul));
            var desc = new BufferDesc
            {
                Size = TLASBufferCapacity,
                Stride = 1,
                Usage = BufferUsage.AccelerationStructure | BufferUsage.ShaderResource,
                HeapType = HeapType.Default
            };

            TLASBuffer = _allocator.CreateBuffer(in desc, "Scene_TLAS");
        }

        if (!TLASScratchBuffer.IsValid || TLASScratchCapacity < requiredScratchSize)
        {
            if (TLASScratchBuffer.IsValid)
            {
                _database.ReleaseResource(TLASScratchBuffer.AsResource());
            }

            TLASScratchCapacity = Math.Max(requiredScratchSize, Math.Max(TLASScratchCapacity * 2, 65536ul));
            var desc = new BufferDesc
            {
                Size = TLASScratchCapacity,
                Stride = 1,
                Usage = BufferUsage.UnorderedAccess,
                HeapType = HeapType.Default
            };

            TLASScratchBuffer = _allocator.CreateBuffer(in desc, "TLAS_ScratchBuffer");
        }
    }

    public void OnResize(uint2 newSize)
    {
    }

    public void Reset()
    {
        if (TLASBuffer.IsValid)
        {
            _database.ReleaseResource(TLASBuffer.AsResource());
            TLASBuffer = Handle<GPUBuffer>.Invalid;
            TLASBufferCapacity = 0;
        }

        if (TLASScratchBuffer.IsValid)
        {
            _database.ReleaseResource(TLASScratchBuffer.AsResource());
            TLASScratchBuffer = Handle<GPUBuffer>.Invalid;
            TLASScratchCapacity = 0;
        }

        if (InstanceDescBuffer.IsValid)
        {
            _database.ReleaseResource(InstanceDescBuffer.AsResource());
            InstanceDescBuffer = Handle<GPUBuffer>.Invalid;
            InstanceDescCapacity = 0;
        }

        TLASBindlessIndex = 0;
    }

    public void Dispose()
    {
        Reset();
    }
}
