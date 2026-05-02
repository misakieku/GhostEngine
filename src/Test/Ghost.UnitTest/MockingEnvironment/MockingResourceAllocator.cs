using Ghost.Core;
using Ghost.Graphics.RHI;

namespace Ghost.UnitTest.MockingEnvironment;

internal class MockingResourceAllocator : IResourceAllocator
{
    private readonly MockingResourceDatabase _database;

    public MockingResourceAllocator(MockingResourceDatabase database)
    {
        _database = database;
    }

    public Handle<GPUResource> Allocate(ref readonly AllocationDesc desc, string? name = null)
    {
        var barrier = new ResourceBarrierData { layout = BarrierLayout.Common, access = BarrierAccess.NoAccess, sync = BarrierSync.None };
        // Passing a mock buffer desc for raw allocation representation
        var bufferDesc = new BufferDesc { Size = desc.Size, Usage = BufferUsage.None };
        return _database.AddMockResource(ResourceDesc.Buffer(bufferDesc), barrier, name);
    }

    public Handle<GPUBuffer> CreateBuffer(ref readonly BufferDesc desc, string? name = null, CreationOptions options = default)
    {
        var barrier = new ResourceBarrierData { layout = BarrierLayout.Undefined, access = BarrierAccess.Common, sync = BarrierSync.None };
        var handle = _database.AddMockResource(ResourceDesc.Buffer(desc), barrier, name);
        return handle.AsBuffer();
    }

    public Identifier<Sampler> CreateSampler(ref readonly SamplerDesc desc)
    {
        return _database.AddSampler(in desc, 1);
    }

    public Handle<GPUTexture> CreateTexture(ref readonly TextureDesc desc, string? name = null, CreationOptions options = default, AdditionalTextureDesc additionalDesc = default)
    {
        var barrier = new ResourceBarrierData { layout = BarrierLayout.Common, access = BarrierAccess.Common, sync = BarrierSync.None };
        var handle = _database.AddMockResource(ResourceDesc.Texture(desc), barrier, name);
        return handle.AsTexture();
    }

    public ResourceSizeInfo GetSizeInfo(ResourceDesc desc)
    {
        return new ResourceSizeInfo
        {
            Size = 1048576, // 1MB mock
            Alignment = 65536, // 64KB aligned
            Offset = 0
        };
    }

    public void Dispose()
    {
        // Handled by dependency injection usually.
    }
}
