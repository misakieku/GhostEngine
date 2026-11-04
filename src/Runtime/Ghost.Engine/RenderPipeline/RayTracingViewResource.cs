using Ghost.Core;
using Ghost.Graphics.RHI;

namespace Ghost.Engine.RenderPipeline;

public sealed unsafe class RayTracingViewResource : IViewResource<RayTracingViewResource, RayTracingViewResource.Desc>
{
    public struct Desc
    {
        public ulong requiredSize;
    }

    private readonly IResourceAllocator _allocator;
    private readonly IResourceDatabase _database;

    public Handle<GPUBuffer> TLASBuffer { get; private set; }
    public Handle<GPUBuffer> TLASScratchBuffer { get; private set; }
    public Handle<GPUBuffer> InstanceDescBuffer { get; private set; }
    public ulong TLASBufferCapacity { get; private set; }
    public ulong TLASScratchCapacity { get; private set; }
    public ulong InstanceDescCapacity { get; private set; }
    public uint TLASBindlessIndex { get; set; }

    public static RayTracingViewResource Create(IResourceAllocator allocator, IResourceDatabase database)
    {
        return new RayTracingViewResource(allocator, database);
    }

    private RayTracingViewResource(IResourceAllocator allocator, IResourceDatabase database)
    {
        _allocator = allocator;
        _database = database;
    }

    public void ReleaseResource()
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

    public void EnsureResource(in Desc desc)
    {
        if (InstanceDescBuffer.IsValid && InstanceDescCapacity >= desc.requiredSize)
        {
            return;
        }

        if (InstanceDescBuffer.IsValid)
        {
            _database.ReleaseResource(InstanceDescBuffer.AsResource());
        }

        InstanceDescCapacity = Math.Max(desc.requiredSize, Math.Max(InstanceDescCapacity * 2, 65536ul));
        var bufDesc = new BufferDesc
        {
            Size = InstanceDescCapacity,
            Stride = (uint)sizeof(RaytracingInstanceDesc),
            Usage = BufferUsage.ShaderResource,
            HeapType = HeapType.Upload
        };

        InstanceDescBuffer = _allocator.CreateBuffer(in bufDesc, "TLAS_InstanceDescBuffer");
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

    public void Dispose()
    {
        ReleaseResource();
    }
}
