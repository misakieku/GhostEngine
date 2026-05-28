using Ghost.Core;
using Ghost.Engine.Streaming;
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
        // AllocationManager.Initialize(AllocationManagerDesc.Default);

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

        _assetManager = new AssetManager(_graphicsEngine.ResourceDatabase, _resourceManager, _provider, _processor, _jobScheduler);
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

        // AllocationManager.Dispose();
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
            CommandBuffer = _commandBuffer,
        };

        _processor.ProcessPendingUploads(ctx);

        await Task.Delay(1000, TestContext.CancellationToken);

        Assert.IsGreaterThanOrEqualTo((int)AssetState.Processing, entry.StateValue);

        // Trigger the completion of the upload and the transition to shader resource state.
        _processor.ProcessPendingUploads(ctx);

        Assert.IsGreaterThanOrEqualTo((int)AssetState.Ready, entry.StateValue);

        var (data, error) = _graphicsEngine.ResourceDatabase.GetResourceBarrierData(handle.AsResource());

        Assert.AreEqual(Error.None, error);
        Assert.AreEqual(BarrierAccess.ShaderResource, data.access);
        Assert.AreEqual(BarrierLayout.ShaderResource, data.layout);
        Assert.AreEqual(BarrierSync.AllShading, data.sync);
    }

    [TestMethod]
    public async Task AssetManager_ResolveMeshThenBackgroundUpload()
    {
        var assetID = Guid.NewGuid();
        _provider.AddMockMesh(assetID, readDelayMs: Random.Shared.Next(10, 50));

        var handle = _assetManager.ResolveMesh(assetID);
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
            CommandBuffer = _commandBuffer,
        };

        _processor.ProcessPendingUploads(ctx);

        await Task.Delay(1000, TestContext.CancellationToken);

        Assert.IsGreaterThanOrEqualTo((int)AssetState.Processing, entry.StateValue);

        _processor.ProcessPendingUploads(ctx);

        Assert.IsGreaterThanOrEqualTo((int)AssetState.Ready, entry.StateValue);
        Assert.IsTrue(_resourceManager.HasMesh(handle));

        ref readonly var mesh = ref _resourceManager.GetMeshReference(handle).GetValueOrThrow();
        var (vertexBarrier, vertexError) = _graphicsEngine.ResourceDatabase.GetResourceBarrierData(mesh.VertexBuffer.AsResource());
        var (indexBarrier, indexError) = _graphicsEngine.ResourceDatabase.GetResourceBarrierData(mesh.IndexBuffer.AsResource());
        var (meshDataBarrier, meshDataError) = _graphicsEngine.ResourceDatabase.GetResourceBarrierData(mesh.MeshDataBuffer.AsResource());

        Assert.AreEqual(Error.None, vertexError);
        Assert.AreEqual(Error.None, indexError);
        Assert.AreEqual(Error.None, meshDataError);
        Assert.IsTrue(vertexBarrier.access.HasFlag(BarrierAccess.VertexBuffer));
        Assert.IsTrue(indexBarrier.access.HasFlag(BarrierAccess.IndexBuffer));
        Assert.AreEqual(BarrierAccess.ShaderResource, meshDataBarrier.access);
    }

    [TestMethod]
    public async Task AssetManager_ReimportMeshKeepsStableHandle()
    {
        var assetID = Guid.NewGuid();
        _provider.AddMockMesh(assetID);

        var handle = _assetManager.ResolveMesh(assetID);
        await Task.Delay(1000, TestContext.CancellationToken);

        var ctx = new ResourceStreamingContext
        {
            ResourceManager = _resourceManager,
            ResourceDatabase = _graphicsEngine.ResourceDatabase,
            ResourceAllocator = _graphicsEngine.ResourceAllocator,
            CopyPipeline = _copyPipeline,
            CommandBuffer = _commandBuffer,
        };

        _processor.ProcessPendingUploads(ctx);
        _processor.ProcessPendingUploads(ctx);

        Assert.IsTrue(_assetManager.TryGetEntry(assetID, out var entry));
        Assert.AreEqual(AssetState.Ready, entry.State);

        _provider.AddMockMesh(assetID);
        _assetManager.ReimportAsset(assetID);
        await Task.Delay(1000, TestContext.CancellationToken);

        _processor.ProcessPendingUploads(ctx);
        _processor.ProcessPendingUploads(ctx);

        var reimportedHandle = _assetManager.ResolveMesh(assetID);
        Assert.AreEqual(handle, reimportedHandle);
        Assert.AreEqual(AssetState.Ready, entry.State);
    }
}
