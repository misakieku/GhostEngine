using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Ghost.Graphics;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel.Buffer;

namespace Ghost.Engine.Streaming;

internal unsafe class ShaderAssetEntry : AssetEntry, ILoadableAssetEntry
{
    private MemoryBlock _rawData;
    private ShaderContentHeader _header;

    public ShaderAssetEntry(AssetManager manager, IResourceDatabase resourceDatabase, ResourceManager resourceManager, Guid assetId, Guid[] dependencies)
        : base(manager, resourceDatabase, resourceManager, assetId, AssetType.Shader, dependencies)
    {
    }

    public override void ReadAssetData(Span<byte> dst)
    {
        if (_rawData.IsCreated)
        {
            _rawData.AsSpan<byte>().CopyTo(dst);
        }
    }

    public override void ReadAssetData<T>(ref T dst)
    {
        if (typeof(T) == typeof(ShaderContentHeader))
        {
            dst = Unsafe.BitCast<ShaderContentHeader, T>(_header);
        }
    }

    protected override void OnReleaseResource()
    {
        if (_rawData.IsCreated)
        {
            _rawData.Dispose();
        }
    }

    public Result OnLoadContent(Stream contentStream)
    {
        if (contentStream.Length < sizeof(ShaderContentHeader))
        {
            return Result.Failure("Shader content stream is smaller than ShaderContentHeader.");
        }

        var header = contentStream.Read<ShaderContentHeader>();
        if (header.magic != ShaderContentHeader.MAGIC || header.version != ShaderContentHeader.VERSION)
        {
            return Result.Failure($"Invalid shader header (Magic: 0x{header.magic:X8}, Version: {header.version}). Expected Magic: 0x{ShaderContentHeader.MAGIC:X8}, Version: {ShaderContentHeader.VERSION}.");
        }

        contentStream.Position = 0;
        _rawData = contentStream.ReadMemory(AllocationHandle.Persistent);
        _header = header;

        return Result.Success();
    }
}
