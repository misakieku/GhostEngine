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

    public Handle<GPUResource> Allocate(scoped in AllocationDesc desc, string? name = null)
    {
        var barrier = new ResourceBarrierData { layout = BarrierLayout.Common, access = BarrierAccess.NoAccess, sync = BarrierSync.None };
        // Passing a mock buffer desc for raw allocation representation
        var bufferDesc = new BufferDesc { Size = desc.Size, Usage = BufferUsage.None };
        return _database.AddMockResource(ResourceDesc.Buffer(bufferDesc), barrier, name);
    }

    public Handle<GPUBuffer> CreateBuffer(scoped in BufferDesc desc, string? name = null, CreationOptions options = default)
    {
        var barrier = new ResourceBarrierData { layout = BarrierLayout.Undefined, access = BarrierAccess.Common, sync = BarrierSync.None };
        var handle = _database.AddMockResource(ResourceDesc.Buffer(desc), barrier, name);
        return handle.AsBuffer();
    }

    public Identifier<Sampler> CreateSampler(scoped in SamplerDesc desc)
    {
        return _database.AddSampler(in desc, 1);
    }

    public Handle<GPUTexture> CreateTexture(scoped in TextureDesc desc, string? name = null, CreationOptions options = default, AdditionalTextureDesc additionalDesc = default)
    {
        var barrier = new ResourceBarrierData { layout = BarrierLayout.Common, access = BarrierAccess.Common, sync = BarrierSync.None };
        var handle = _database.AddMockResource(ResourceDesc.Texture(desc), barrier, name);
        return handle.AsTexture();
    }

    public ResourceSizeInfo GetSizeInfo(ResourceDesc desc)
    {
        return desc.Type switch
        {
            ResourceType.Buffer => new ResourceSizeInfo { Size = desc.BufferDescriptor.Size, Alignment = 256, Offset = 0 },
            ResourceType.Texture => new ResourceSizeInfo { Size = desc.TextureDescriptor.Width * desc.TextureDescriptor.Height * desc.TextureDescriptor.Format.GetBytesPerPixel() * desc.TextureDescriptor.Slice, Alignment = 512, Offset = 0 },
            _ => throw new ArgumentOutOfRangeException(nameof(desc.Type), "Unknown resource type")
        };
    }

    public void Dispose()
    {
    }
}
