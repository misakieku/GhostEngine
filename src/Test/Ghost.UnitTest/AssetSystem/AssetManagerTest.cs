using Ghost.Engine;
using Ghost.UnitTest.MockingEnvironment;
using Misaki.HighPerformance.Jobs;

namespace Ghost.UnitTest.AssetSystem;

[TestClass]
public class AssetManagerTest
{
    private MockingResourceDatabase _resourceDatabase = null!;
    private MockingResourceAllocator _resourceAllocator = null!;
    private MockingCommandBuffer _commandBuffer = null!;
    private MockingContentProvider _provider = null!;

    private ResourceStreamingProcessor _processor = null!;
    private JobScheduler _jobScheduler = null!;

    private AssetManager _assetManager = null!;

    [TestInitialize]
    public void Setup()
    {
        _resourceDatabase = new MockingResourceDatabase();
        _resourceAllocator = new MockingResourceAllocator(_resourceDatabase);
        _commandBuffer = new MockingCommandBuffer(_resourceDatabase);
        _provider = new MockingContentProvider();

        _processor = new ResourceStreamingProcessor();

        var schedulerDesc = new JobSchedulerDesc
        {
            ThreadCount = 1,
            ThreadPriority = ThreadPriority.Normal,
            DependencyChainCapacity = 1024,
        };
        _jobScheduler = new JobScheduler(in schedulerDesc);

        _assetManager = new AssetManager(_resourceDatabase, _provider, _processor, _jobScheduler);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _assetManager.Dispose();
        _jobScheduler.Dispose();
        _commandBuffer.Dispose();
        _resourceAllocator.Dispose();
        _resourceDatabase.Dispose();
    }

    [TestMethod]
    public void AssetManager_GetsAssetSuccessfully()
    {
        var assetID = Guid.NewGuid();
        _provider.AddMockTexture(assetID, readDelayMs: Random.Shared.Next(10, 50));

        var handle = _assetManager.ResolveTexture(assetID);
        Assert.IsTrue(handle.IsValid);
    }
}
