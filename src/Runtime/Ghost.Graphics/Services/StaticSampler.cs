using Ghost.Core;
using Ghost.Graphics.RHI;

namespace Ghost.Graphics.Services;

public class StaticSampler : IDisposable
{
    private readonly IResourceDatabase _resourceDatabase;

    public Identifier<Sampler> LinearClamp
    {
        get;
    }

    public StaticSampler(IResourceAllocator resourceAllocator, IResourceDatabase resourceDatabase)
    {
        _resourceDatabase = resourceDatabase;

        LinearClamp = resourceAllocator.CreateSampler(new SamplerDesc
        {
            FilterMode = TextureFilterMode.Bilinear,
            AddressU = TextureAddressMode.Clamp,
            AddressV = TextureAddressMode.Clamp,
            AddressW = TextureAddressMode.Clamp,
            MipLODBias = 0.0f,
            MaxAnisotropy = 1,
            ComparisonFunc = ComparisonFunction.Never,
            MinLOD = 0.0f,
            MaxLOD = float.MaxValue
        });
    }

    public void Dispose()
    {
        _resourceDatabase.ReleaseSampler(LinearClamp);
        GC.SuppressFinalize(this);
    }
}