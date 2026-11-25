using BenchmarkDotNet.Attributes;
using Ghost.Engine.Streaming;
using Ghost.Graphics.RHI;
using System.Resources;

namespace Ghost.UnitTest.Benchmarks;

[MemoryDiagnoser]
public class AssetStreamingBenchmark
{
    private IResourceDatabase _resourceDatabase = null!;
    private IResourceAllocator _resourceAllocator = null!;
    private ResourceManager _resourceManager = null!;
    private IContentProvider _contentProvider = null!;
    private AssetManager _assetManager = null!;

    public void Setup()
    {
    }
}
