using Ghost.Core;
using Ghost.Engine.Streaming;
using Ghost.Graphics.Services;
using Ghost.UnitTest.MockingEnvironment;
using Misaki.HighPerformance.Jobs;

namespace Ghost.UnitTest.Streaming;

[TestClass]
public unsafe class AssetManagerSchedulingAndProgressTests
{
    private static byte[] BuildEmptyPrefabPayload()
    {
        var header = new PrefabContentHeader
        {
            magic = PrefabContentHeader.MAGIC,
            version = PrefabContentHeader.VERSION,
            entityCount = 0,
            materialDependencyIndexCount = 0,
            componentTypeCount = 0,
            meshMaterialFixupCount = 0
        };

        var bytes = new byte[sizeof(PrefabContentHeader)];
        fixed (byte* p = bytes)
        {
            *(PrefabContentHeader*)p = header;
        }

        return bytes;
    }

    [TestMethod]
    public void Test_DiamondAndTriangleDAG_BalancesRefCountsAndUnloadsCleanly()
    {
        using var engine = new MockingGraphicsEngine();
        using var renderDevice = new MockingRenderDevice();
        using var resourceDatabase = new MockingResourceDatabase();
        using var pipelineLibrary = new MockingPipelineLibrary();
        using var resourceAllocator = new MockingResourceAllocator(resourceDatabase);
        using var shaderLibrary = new ShaderLibrary(null, pipelineLibrary, null);
        using var resourceManager = new ResourceManager(renderDevice, resourceAllocator, resourceDatabase);
        using var jobScheduler = new JobScheduler(new JobSchedulerDesc { ThreadCount = 2, ThreadPriority = ThreadPriority.Normal, DependencyChainCapacity = 1024 });

        using var contentProvider = new MockingContentProvider();
        var streamingProcessor = new ResourceStreamingProcessor();
        using var assetManager = new AssetManager(resourceDatabase, resourceManager, contentProvider, streamingProcessor, jobScheduler, engine, shaderLibrary, 2);

        var prefabBytes = BuildEmptyPrefabPayload();
        var sharedLeafGuid = Guid.NewGuid();
        var leaf1Guid = Guid.NewGuid();
        var leaf2Guid = Guid.NewGuid();
        var branch1Guid = Guid.NewGuid();
        var branch2Guid = Guid.NewGuid();
        var rootGuid = Guid.NewGuid();

        contentProvider.AddMockAsset(sharedLeafGuid, new MockingContentProvider.MockAssetData { type = AssetType.EntityPrefab, data = prefabBytes });
        contentProvider.AddMockAsset(leaf1Guid, new MockingContentProvider.MockAssetData { type = AssetType.EntityPrefab, data = prefabBytes });
        contentProvider.AddMockAsset(leaf2Guid, new MockingContentProvider.MockAssetData { type = AssetType.EntityPrefab, data = prefabBytes });
        contentProvider.AddMockAsset(branch1Guid, new MockingContentProvider.MockAssetData { type = AssetType.EntityPrefab, data = prefabBytes, dependencies = new[] { sharedLeafGuid, leaf1Guid } });
        contentProvider.AddMockAsset(branch2Guid, new MockingContentProvider.MockAssetData { type = AssetType.EntityPrefab, data = prefabBytes, dependencies = new[] { sharedLeafGuid, leaf2Guid } });
        // Triangle + diamond: root references sharedLeaf directly and via both branches
        contentProvider.AddMockAsset(rootGuid, new MockingContentProvider.MockAssetData { type = AssetType.EntityPrefab, data = prefabBytes, dependencies = new[] { sharedLeafGuid, branch1Guid, branch2Guid } });

        Assert.IsTrue(assetManager.TryResolveAsset(rootGuid, out var rootEntry));
        jobScheduler.Wait(rootEntry.LoadJobHandle);

        Assert.AreEqual(AssetState.Ready, rootEntry.State);
        Assert.AreEqual(1, rootEntry.RefCount);
        Assert.IsTrue(assetManager.TryGetEntry(branch1Guid, out var branch1));
        Assert.IsTrue(assetManager.TryGetEntry(branch2Guid, out var branch2));
        Assert.IsTrue(assetManager.TryGetEntry(sharedLeafGuid, out var sharedLeaf));
        Assert.IsTrue(assetManager.TryGetEntry(leaf1Guid, out var leaf1));
        Assert.IsTrue(assetManager.TryGetEntry(leaf2Guid, out var leaf2));

        Assert.AreEqual(1, branch1.RefCount);
        Assert.AreEqual(1, branch2.RefCount);
        Assert.AreEqual(3, sharedLeaf.RefCount);
        Assert.AreEqual(1, leaf1.RefCount);
        Assert.AreEqual(1, leaf2.RefCount);

        var progress = rootEntry.GetLoadProgress();
        Assert.AreEqual(6, progress.TotalCount);
        Assert.AreEqual(6, progress.ReadyCount);
        Assert.AreEqual(1.0f, progress.Progress);

        rootEntry.Release();

        Assert.IsFalse(assetManager.TryGetEntry(rootGuid, out _));
        Assert.IsFalse(assetManager.TryGetEntry(branch1Guid, out _));
        Assert.IsFalse(assetManager.TryGetEntry(branch2Guid, out _));
        Assert.IsFalse(assetManager.TryGetEntry(sharedLeafGuid, out _));
        Assert.IsFalse(assetManager.TryGetEntry(leaf1Guid, out _));
        Assert.IsFalse(assetManager.TryGetEntry(leaf2Guid, out _));
    }

    [TestMethod]
    public void Test_DeepDependencyChain_SchedulesIterativelyWithoutStackOverflow()
    {
        const int chainDepth = 600;

        using var engine = new MockingGraphicsEngine();
        using var renderDevice = new MockingRenderDevice();
        using var resourceDatabase = new MockingResourceDatabase();
        using var pipelineLibrary = new MockingPipelineLibrary();
        using var resourceAllocator = new MockingResourceAllocator(resourceDatabase);
        using var shaderLibrary = new ShaderLibrary(null, pipelineLibrary, null);
        using var resourceManager = new ResourceManager(renderDevice, resourceAllocator, resourceDatabase);
        using var jobScheduler = new JobScheduler(new JobSchedulerDesc { ThreadCount = 2, ThreadPriority = ThreadPriority.Normal, DependencyChainCapacity = 4096 });

        using var contentProvider = new MockingContentProvider();
        var streamingProcessor = new ResourceStreamingProcessor();
        using var assetManager = new AssetManager(resourceDatabase, resourceManager, contentProvider, streamingProcessor, jobScheduler, engine, shaderLibrary, 2);

        var prefabBytes = BuildEmptyPrefabPayload();
        var guids = new Guid[chainDepth];
        for (var i = chainDepth - 1; i >= 0; i--)
        {
            guids[i] = Guid.NewGuid();
            var deps = i == chainDepth - 1 ? Array.Empty<Guid>() : new[] { guids[i + 1] };
            contentProvider.AddMockAsset(guids[i], new MockingContentProvider.MockAssetData
            {
                type = AssetType.EntityPrefab,
                data = prefabBytes,
                dependencies = deps
            });
        }

        Assert.IsTrue(assetManager.TryResolveAsset(guids[0], out var rootEntry));
        jobScheduler.Wait(rootEntry.LoadJobHandle);

        Assert.AreEqual(AssetState.Ready, rootEntry.State);
        Assert.IsTrue(assetManager.TryGetEntry(guids[^1], out var leafEntry));
        Assert.AreEqual(AssetState.Ready, leafEntry.State);

        var progress = rootEntry.GetLoadProgress();
        Assert.AreEqual(chainDepth, progress.TotalCount);
        Assert.AreEqual(chainDepth, progress.ReadyCount);
        Assert.AreEqual(1.0f, progress.Progress);

        rootEntry.Release();
        for (var i = 0; i < chainDepth; i++)
        {
            Assert.IsFalse(assetManager.TryGetEntry(guids[i], out _));
        }
    }

    [TestMethod]
    public void Test_GetLoadProgress_ReflectsWeightedDependencyStates()
    {
        using var engine = new MockingGraphicsEngine();
        using var renderDevice = new MockingRenderDevice();
        using var resourceDatabase = new MockingResourceDatabase();
        using var pipelineLibrary = new MockingPipelineLibrary();
        using var resourceAllocator = new MockingResourceAllocator(resourceDatabase);
        using var shaderLibrary = new ShaderLibrary(null, pipelineLibrary, null);
        using var resourceManager = new ResourceManager(renderDevice, resourceAllocator, resourceDatabase);
        using var jobScheduler = new JobScheduler(new JobSchedulerDesc { ThreadCount = 2, ThreadPriority = ThreadPriority.Normal, DependencyChainCapacity = 1024 });

        using var contentProvider = new MockingContentProvider();
        var streamingProcessor = new ResourceStreamingProcessor();
        using var assetManager = new AssetManager(resourceDatabase, resourceManager, contentProvider, streamingProcessor, jobScheduler, engine, shaderLibrary, 2);

        var tex1Guid = Guid.NewGuid();
        var tex2Guid = Guid.NewGuid();
        var rootGuid = Guid.NewGuid();

        contentProvider.AddMockTexture(tex1Guid);
        contentProvider.AddMockTexture(tex2Guid);
        contentProvider.AddMockAsset(rootGuid, new MockingContentProvider.MockAssetData
        {
            type = AssetType.EntityPrefab,
            data = BuildEmptyPrefabPayload(),
            dependencies = new[] { tex1Guid, tex2Guid }
        });

        Assert.IsTrue(assetManager.TryResolveAsset(rootGuid, out var rootEntry));
        jobScheduler.Wait(rootEntry.LoadJobHandle);

        Assert.IsTrue(assetManager.TryGetEntry(tex1Guid, out var tex1Entry));
        Assert.IsTrue(assetManager.TryGetEntry(tex2Guid, out var tex2Entry));

        // Textures remain at Loaded until GPU copy upload finishes
        Assert.AreEqual(AssetState.Ready, rootEntry.State);
        Assert.AreEqual(AssetState.Loaded, tex1Entry.State);
        Assert.AreEqual(AssetState.Loaded, tex2Entry.State);

        var midProgress = rootEntry.GetLoadProgress();
        Assert.AreEqual(3, midProgress.TotalCount);
        Assert.AreEqual(3, midProgress.LoadedCount);
        Assert.AreEqual(1, midProgress.ReadyCount);
        Assert.IsLessThan(1.0f, midProgress.Progress);
        Assert.IsGreaterThan(0.5f, midProgress.Progress);

        // Simulate one GPU upload finishing and one still Loading
        tex1Entry.State = AssetState.Ready;
        tex2Entry.State = AssetState.Loading;
        var partialProgress = rootEntry.GetLoadProgress();
        Assert.AreEqual(2, partialProgress.ReadyCount);
        Assert.AreEqual(2, partialProgress.LoadedCount);
        Assert.IsLessThan(1.0f, partialProgress.Progress);

        tex2Entry.State = AssetState.Ready;
        var doneProgress = rootEntry.GetLoadProgress();
        Assert.AreEqual(3, doneProgress.TotalCount);
        Assert.AreEqual(3, doneProgress.ReadyCount);
        Assert.AreEqual(1.0f, doneProgress.Progress);

        rootEntry.Release();
    }

    [TestMethod]
    public void Test_ResourceStreamingProcessor_MaxUploadsPerFrame_DefaultsAndUpdates()
    {
        var processor = new ResourceStreamingProcessor();
        Assert.AreEqual(ResourceStreamingProcessor.DEFAULT_MAX_UPLOADS_PER_FRAME, processor.MaxUploadsPerFrame);

        processor.MaxUploadsPerFrame = 32;
        Assert.AreEqual(32, processor.MaxUploadsPerFrame);
    }

    [TestMethod]
    public void Test_ReleaseWhileDependencyIsLoaded_DefersUnloadUntilTerminalState()
    {
        using var engine = new MockingGraphicsEngine();
        using var renderDevice = new MockingRenderDevice();
        using var resourceDatabase = new MockingResourceDatabase();
        using var pipelineLibrary = new MockingPipelineLibrary();
        using var resourceAllocator = new MockingResourceAllocator(resourceDatabase);
        using var shaderLibrary = new ShaderLibrary(null, pipelineLibrary, null);
        using var resourceManager = new ResourceManager(renderDevice, resourceAllocator, resourceDatabase);
        using var jobScheduler = new JobScheduler(new JobSchedulerDesc { ThreadCount = 2, ThreadPriority = ThreadPriority.Normal, DependencyChainCapacity = 1024 });

        using var contentProvider = new MockingContentProvider();
        var streamingProcessor = new ResourceStreamingProcessor();
        using var assetManager = new AssetManager(resourceDatabase, resourceManager, contentProvider, streamingProcessor, jobScheduler, engine, shaderLibrary, 2);

        var texGuid = Guid.NewGuid();
        var rootGuid = Guid.NewGuid();

        contentProvider.AddMockTexture(texGuid);
        contentProvider.AddMockAsset(rootGuid, new MockingContentProvider.MockAssetData
        {
            type = AssetType.EntityPrefab,
            data = BuildEmptyPrefabPayload(),
            dependencies = new[] { texGuid }
        });

        Assert.IsTrue(assetManager.TryResolveAsset(rootGuid, out var rootEntry));
        jobScheduler.Wait(rootEntry.LoadJobHandle);

        Assert.IsTrue(assetManager.TryGetEntry(texGuid, out var texEntry));
        Assert.AreEqual(AssetState.Ready, rootEntry.State);
        Assert.AreEqual(AssetState.Loaded, texEntry.State);

        // Releasing root while tex is still in AssetState.Loaded must defer unloading tex until tex reaches Ready/Failed.
        Assert.AreEqual(0, rootEntry.Release());
        Assert.IsFalse(assetManager.TryGetEntry(rootGuid, out _));
        Assert.IsTrue(assetManager.TryGetEntry(texGuid, out _));
        Assert.AreEqual(0, texEntry.RefCount);

        texEntry.State = AssetState.Ready;
        Assert.IsFalse(assetManager.TryGetEntry(texGuid, out _));
    }
}
