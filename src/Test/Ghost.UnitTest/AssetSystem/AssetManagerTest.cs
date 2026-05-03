using Ghost.Core;
using Ghost.Engine;
using Ghost.Graphics;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Ghost.UnitTest.MockingEnvironment;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;

namespace Ghost.UnitTest.AssetSystem;

[TestClass]
[DoNotParallelize]
public class AssetManagerTest
{
    private MockingGraphicsEngine _graphicsEngine = null!;
    private MockingCommandBuffer _commandBuffer = null!;
    private MockingContentProvider _provider = null!;

    private AsyncCopyPipeline _copyPipeline = null!;
    private ResourceManager _resourceManager = null!;
    private ResourceStreamingProcessor _processor = null!;
    private JobScheduler _jobScheduler = null!;

    private AssetManager _assetManager = null!;

    public TestContext TestContext
    {
        get; set;
    }

    [TestInitialize]
    public void Setup()
    {
        AllocationManager.Initialize(AllocationManagerDesc.Default);

        _graphicsEngine = new MockingGraphicsEngine();
        _commandBuffer = (MockingCommandBuffer)_graphicsEngine.CreateCommandBuffer();
        _provider = new MockingContentProvider();

        _copyPipeline = new AsyncCopyPipeline(_graphicsEngine);
        _resourceManager = new ResourceManager(_graphicsEngine.Device, _graphicsEngine.ResourceAllocator, _graphicsEngine.ResourceDatabase);
        _processor = new ResourceStreamingProcessor();

        var schedulerDesc = new JobSchedulerDesc
        {
            ThreadCount = 1,
            ThreadPriority = ThreadPriority.Normal,
            DependencyChainCapacity = 64,
        };
        _jobScheduler = new JobScheduler(in schedulerDesc);

        _assetManager = new AssetManager(_graphicsEngine.ResourceDatabase, _provider, _processor, _jobScheduler);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _assetManager.Dispose();
        _jobScheduler.Dispose();
        _resourceManager.Dispose();
        _copyPipeline.Dispose();
        _commandBuffer.Dispose();
        _graphicsEngine.Dispose();

        AllocationManager.Dispose();
    }

    [TestMethod]
    public async Task AssetManager_ResolveTextureThenBackgroundUpload()
    {
        var assetID = Guid.NewGuid();
        _provider.AddMockTexture(assetID, readDelayMs: Random.Shared.Next(10, 50));

        var handle = _assetManager.ResolveTexture(assetID);
        Assert.IsTrue(handle.IsValid);

        Assert.IsTrue(_assetManager.TryGetEntry(assetID, out var entry));
        Assert.IsGreaterThanOrEqualTo((int)AssetState.Scheduled, entry.StateValue);

        await Task.Delay(1000, TestContext.CancellationToken);

        Assert.IsGreaterThanOrEqualTo((int)AssetState.Loaded, entry.StateValue);

        var ctx = new ResourceStreamingContext
        {
            ResourceManager = _resourceManager,
            ResourceDatabase = _graphicsEngine.ResourceDatabase,
            ResourceAllocator = _graphicsEngine.ResourceAllocator,
            CopyPipeline = _copyPipeline,
            GraphicsCommandBuffer = _commandBuffer,
        };

        _processor.ProcessPendingUploads(ctx);

        await Task.Delay(1000, TestContext.CancellationToken);

        Assert.IsGreaterThanOrEqualTo((int)AssetState.Uploading, entry.StateValue);

        // Trigger the completion of the upload and the transition to shader resource state.
        _processor.ProcessPendingUploads(ctx);

        Assert.IsGreaterThanOrEqualTo((int)AssetState.Ready, entry.StateValue);

        var (data, error) = _graphicsEngine.ResourceDatabase.GetResourceBarrierData(handle.AsResource());
        
        Assert.AreEqual(Error.None, error);
        Assert.AreEqual(BarrierAccess.ShaderResource, data.access);
        Assert.AreEqual(BarrierLayout.ShaderResource, data.layout);
        Assert.AreEqual(BarrierSync.AllShading, data.sync);
    }
}
