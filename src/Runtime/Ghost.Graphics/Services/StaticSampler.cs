using Ghost.Core;
using Ghost.Graphics.RHI;

namespace Ghost.Graphics.Services;

public sealed class StaticSampler : IDisposable
{
    private readonly IResourceDatabase _resourceDatabase;

    private readonly Identifier<Sampler> _linearClamp;
    private readonly Identifier<Sampler> _linearRepeat;

    public uint LinearClamp => (uint)_linearClamp.Value;
    public uint LinearRepeat => (uint)_linearRepeat.Value;

    public StaticSampler(IResourceAllocator resourceAllocator, IResourceDatabase resourceDatabase)
    {
        _resourceDatabase = resourceDatabase;

        _linearClamp = resourceAllocator.CreateSampler(new SamplerDesc
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

        _linearRepeat = resourceAllocator.CreateSampler(new SamplerDesc
        {
            FilterMode = TextureFilterMode.Bilinear,
            AddressU = TextureAddressMode.Repeat,
            AddressV = TextureAddressMode.Repeat,
            AddressW = TextureAddressMode.Repeat,
            MipLODBias = 0.0f,
            MaxAnisotropy = 1,
            ComparisonFunc = ComparisonFunction.Never,
            MinLOD = 0.0f,
            MaxLOD = float.MaxValue
        });
    }

    public void Dispose()
    {
        _resourceDatabase.ReleaseSampler(_linearClamp);
        _resourceDatabase.ReleaseSampler(_linearRepeat);
    }
}