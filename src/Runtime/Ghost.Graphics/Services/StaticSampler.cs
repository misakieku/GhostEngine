using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.Services;

public sealed class StaticSampler : IDisposable
{
    private readonly IResourceDatabase _resourceDatabase;

    private UnsafeArray<Identifier<Sampler>> _samplers;

    private static UnsafeList<SamplerDesc> GetStaticSamplerDescs(uint maxAnisotropy, AllocationHandle allocationHandle)
    {
        var descs = new UnsafeList<SamplerDesc>((int)StaticSamplerType.Count, allocationHandle);

        descs.Add(new SamplerDesc
        {
            FilterMode = TextureFilterMode.Point,
            AddressU = TextureAddressMode.Clamp,
            AddressV = TextureAddressMode.Clamp,
            AddressW = TextureAddressMode.Clamp,
            MipLODBias = 0.0f,
            MaxAnisotropy = maxAnisotropy,
            ComparisonFunc = ComparisonFunction.Never,
            MinLOD = 0.0f,
            MaxLOD = float.MaxValue
        });

        descs.Add(new SamplerDesc
        {
            FilterMode = TextureFilterMode.Point,
            AddressU = TextureAddressMode.Repeat,
            AddressV = TextureAddressMode.Repeat,
            AddressW = TextureAddressMode.Repeat,
            MipLODBias = 0.0f,
            MaxAnisotropy = maxAnisotropy,
            ComparisonFunc = ComparisonFunction.Never,
            MinLOD = 0.0f,
            MaxLOD = float.MaxValue
        });

        descs.Add(new SamplerDesc
        {
            FilterMode = TextureFilterMode.Bilinear,
            AddressU = TextureAddressMode.Clamp,
            AddressV = TextureAddressMode.Clamp,
            AddressW = TextureAddressMode.Clamp,
            MipLODBias = 0.0f,
            MaxAnisotropy = maxAnisotropy,
            ComparisonFunc = ComparisonFunction.Never,
            MinLOD = 0.0f,
            MaxLOD = float.MaxValue
        });

        descs.Add(new SamplerDesc
        {
            FilterMode = TextureFilterMode.Bilinear,
            AddressU = TextureAddressMode.Repeat,
            AddressV = TextureAddressMode.Repeat,
            AddressW = TextureAddressMode.Repeat,
            MipLODBias = 0.0f,
            MaxAnisotropy = maxAnisotropy,
            ComparisonFunc = ComparisonFunction.Never,
            MinLOD = 0.0f,
            MaxLOD = float.MaxValue
        });

        descs.Add(new SamplerDesc
        {
            FilterMode = TextureFilterMode.Trilinear,
            AddressU = TextureAddressMode.Clamp,
            AddressV = TextureAddressMode.Clamp,
            AddressW = TextureAddressMode.Clamp,
            MipLODBias = 0.0f,
            MaxAnisotropy = maxAnisotropy,
            ComparisonFunc = ComparisonFunction.Never,
            MinLOD = 0.0f,
            MaxLOD = float.MaxValue
        });

        descs.Add(new SamplerDesc
        {
            FilterMode = TextureFilterMode.Trilinear,
            AddressU = TextureAddressMode.Repeat,
            AddressV = TextureAddressMode.Repeat,
            AddressW = TextureAddressMode.Repeat,
            MipLODBias = 0.0f,
            MaxAnisotropy = maxAnisotropy,
            ComparisonFunc = ComparisonFunction.Never,
            MinLOD = 0.0f,
            MaxLOD = float.MaxValue
        });

        descs.Add(new SamplerDesc
        {
            FilterMode = TextureFilterMode.Anisotropic,
            AddressU = TextureAddressMode.Clamp,
            AddressV = TextureAddressMode.Clamp,
            AddressW = TextureAddressMode.Clamp,
            MipLODBias = 0.0f,
            MaxAnisotropy = maxAnisotropy,
            ComparisonFunc = ComparisonFunction.Never,
            MinLOD = 0.0f,
            MaxLOD = float.MaxValue
        });

        descs.Add(new SamplerDesc
        {
            FilterMode = TextureFilterMode.Anisotropic,
            AddressU = TextureAddressMode.Repeat,
            AddressV = TextureAddressMode.Repeat,
            AddressW = TextureAddressMode.Repeat,
            MipLODBias = 0.0f,
            MaxAnisotropy = maxAnisotropy,
            ComparisonFunc = ComparisonFunction.Never,
            MinLOD = 0.0f,
            MaxLOD = float.MaxValue
        });

        descs.Add(new SamplerDesc
        {
            FilterMode = TextureFilterMode.Bilinear,
            AddressU = TextureAddressMode.Clamp,
            AddressV = TextureAddressMode.Clamp,
            AddressW = TextureAddressMode.Clamp,
            MipLODBias = 0.0f,
            MaxAnisotropy = maxAnisotropy,
            ComparisonFunc = ComparisonFunction.GreaterEqual,
            MinLOD = 0.0f,
            MaxLOD = float.MaxValue
        });

        return descs;
    }

    public StaticSampler(IResourceAllocator resourceAllocator, IResourceDatabase resourceDatabase, uint maxAnisotropy)
    {
        _resourceDatabase = resourceDatabase;
        _samplers = new UnsafeArray<Identifier<Sampler>>((int)StaticSamplerType.Count + 1, AllocationHandle.Persistent);

        maxAnisotropy = Math.Clamp(maxAnisotropy, 1, 16);
        using var scope = AllocationManager.CreateStackScope();
        using var descs = GetStaticSamplerDescs(maxAnisotropy, scope.AllocationHandle);

        Logger.DebugAssert(descs.Count == (int)StaticSamplerType.Count);

        for (var i = 0; i < descs.Count; i++)
        {
            _samplers[i] = resourceAllocator.CreateSampler(descs[i]);
        }

        Logger.DebugAssert(_samplers[0].Value == 1);

        _samplers[(int)StaticSamplerType.Count] = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetSampler(StaticSamplerType type)
    {
        return (uint)_samplers[(int)type].Value;
    }

    public void Dispose()
    {
        for (var i = 0; i < _samplers.Count; i++)
        {
            _resourceDatabase.ReleaseSampler(_samplers[i]);
        }

        _samplers.Dispose();
    }
}
