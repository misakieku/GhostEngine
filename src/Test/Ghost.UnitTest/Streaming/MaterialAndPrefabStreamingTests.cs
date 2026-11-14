using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Engine.Components;
using Ghost.Engine.RenderPipeline;
using Ghost.Engine.Streaming;
using Ghost.Engine.Utilities;
using Ghost.Entities;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Ghost.UnitTest.MockingEnvironment;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;
using System.Text;

namespace Ghost.UnitTest.Streaming;

[TestClass]
public unsafe class MaterialAndPrefabStreamingTests
{
    [TestMethod]
    public void Test_MaterialAssetEntry_LoadsAndPatchesBindlessSrv()
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

        var shaderDesc = new GraphicsShaderDescriptor
        {
            Name = "TestShader",
            PropertyBufferSize = 96,
            Passes = Array.Empty<PassDescriptor>()
        };
        var shaderHandle = assetManager.ResourceManager.CreateShader(shaderDesc);
        var shaderGuid = Guid.NewGuid();

        var texGuid = Guid.NewGuid();
        contentProvider.AddMockTexture(texGuid);
        assetManager.TryResolveAsset(texGuid, out var texAsset);

        var matHeader = new MaterialContentHeader
        {
            magic = MaterialContentHeader.MAGIC,
            version = MaterialContentHeader.VERSION,
            shaderId = shaderGuid,
            propertyBufferSize = 96,
            textureFixupCount = 1,
            propertyBufferOffset = sizeof(MaterialContentHeader),
            textureFixupOffset = sizeof(MaterialContentHeader) + 96
        };

        var fixup = new MaterialTextureFixup { byteOffset = 16, textureGuid = texGuid };
        var totalSize = sizeof(MaterialContentHeader) + 96 + sizeof(MaterialTextureFixup);
        var matBytes = new byte[totalSize];

        fixed (byte* p = matBytes)
        {
            *(MaterialContentHeader*)p = matHeader;
            *(float3*)(p + sizeof(MaterialContentHeader)) = new float3(1.0f, 0.5f, 0.25f);
            *(MaterialTextureFixup*)(p + sizeof(MaterialContentHeader) + 96) = fixup;
        }

        var matGuid = Guid.NewGuid();
        var entry = new MaterialAssetEntry(assetManager, resourceDatabase, assetManager.ResourceManager, matGuid, Array.Empty<Guid>(), "TestMaterial");
        entry.AddRef();

        var dummyShaderEntry = new DummyShaderAssetEntry(assetManager, resourceDatabase, assetManager.ResourceManager, shaderGuid, shaderHandle, "DummyShader");
        assetManager.RegisterEntryForTesting(shaderGuid, dummyShaderEntry);

        fixed (byte* p = matBytes)
        {
            var payload = AssetPayload.FromMappedMemory(p, matBytes.Length);
            var result = entry.OnLoadContent(ref payload);
            Assert.IsTrue(result.IsSuccess, result.Message);
        }

        entry.State = AssetState.Ready;
        if (assetManager.TryGetEntry(texGuid, out var texEntry))
        {
            texEntry.State = AssetState.Ready;
        }

        Assert.IsTrue(entry.MaterialHandle.IsValid);
        var matRef = assetManager.ResourceManager.GetMaterialReference(entry.MaterialHandle);
        Assert.IsTrue(matRef.IsSuccess);

        var rawProps = matRef.Value.GetRawPropertyCache();
        Assert.AreEqual(96, rawProps.Length);
        fixed (byte* pRaw = rawProps)
        {
            var baseColor = *(float3*)pRaw;
            Assert.AreEqual(1.0f, baseColor.x);
            Assert.AreEqual(0.5f, baseColor.y);
            Assert.AreEqual(0.25f, baseColor.z);
        }

        entry.Release();
        texAsset?.Release();
        assetManager.RemoveEntry(shaderGuid);
        assetManager.RemoveEntry(texGuid);
    }

    [TestMethod]
    public void Test_PrefabAssetEntry_And_PrefabUtility_Instantiate()
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

        var meshDummy = default(Mesh);
        meshDummy.MeshDataBuffer = resourceDatabase.CreateEmpty(name: "Mesh_Data").AsBuffer();
        meshDummy.MeshBuffer = resourceDatabase.CreateEmpty(name: "Mesh_Buf").AsBuffer();
        var meshHandle = assetManager.ResourceManager.RegisterMesh(ref meshDummy);
        var meshGuid = Guid.NewGuid();

        var dummyMeshEntry = new DummyMeshAssetEntry(assetManager, resourceDatabase, assetManager.ResourceManager, meshGuid, meshHandle, "TestMesh");
        dummyMeshEntry.AddRef();
        assetManager.RegisterEntryForTesting(meshGuid, dummyMeshEntry);

        var shaderDesc = new GraphicsShaderDescriptor
        {
            Name = "TestShader",
            PropertyBufferSize = 96,
            Passes = Array.Empty<PassDescriptor>()
        };
        var shaderHandle = assetManager.ResourceManager.CreateShader(shaderDesc);
        var matHandle = assetManager.ResourceManager.CreateMaterial(shaderHandle, "TestMat");
        var matGuid = Guid.NewGuid();

        var dummyMatEntry = new DummyMaterialAssetEntry(assetManager, resourceDatabase, assetManager.ResourceManager, matGuid, matHandle, "TestMat");
        dummyMatEntry.AddRef();
        assetManager.RegisterEntryForTesting(matGuid, dummyMatEntry);

        var ms = new MemoryStream();
        var header = new PrefabContentHeader
        {
            magic = PrefabContentHeader.MAGIC,
            version = PrefabContentHeader.VERSION,
            entityCount = 2,
            componentTypeCount = 3,
            meshMaterialFixupCount = 1
        };

        var headerPlaceholder = new byte[sizeof(PrefabContentHeader)];
        ms.Write(headerPlaceholder);

        header.componentTypesOffset = ms.Position;
        var typeNames = new string[]
        {
            typeof(LocalTransform).FullName!,
            typeof(LocalToWorld).FullName!,
            typeof(MeshInstance).FullName!
        };
        for (var t = 0; t < typeNames.Length; t++)
        {
            var utf8 = Encoding.UTF8.GetBytes(typeNames[t]);
            var len = (ushort)utf8.Length;
            ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref len, 1)));
            ms.Write(utf8);
        }

        header.entitiesOffset = ms.Position;

        var e0Header = new PrefabEntityHeader { parentIndex = -1, firstChildIndex = 1, nextSiblingIndex = -1, componentCount = 2 };
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref e0Header, 1)));
        var c0_0 = new PrefabComponentHeader { componentTypeIndex = 0, dataSize = sizeof(LocalTransform) };
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref c0_0, 1)));
        var lt0 = new LocalTransform { position = float3.zero, rotation = quaternion.identity, scale = new float3(1.0f) };
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref lt0, 1)));
        var c0_1 = new PrefabComponentHeader { componentTypeIndex = 1, dataSize = sizeof(LocalToWorld) };
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref c0_1, 1)));
        var ltw0 = new LocalToWorld { matrix = float4x4.identity };
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref ltw0, 1)));

        var e1Header = new PrefabEntityHeader { parentIndex = 0, firstChildIndex = -1, nextSiblingIndex = -1, componentCount = 3 };
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref e1Header, 1)));
        var c1_0 = new PrefabComponentHeader { componentTypeIndex = 0, dataSize = sizeof(LocalTransform) };
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref c1_0, 1)));
        var lt1 = new LocalTransform { position = new float3(0.0f, 2.0f, 0.0f), rotation = quaternion.identity, scale = new float3(1.0f) };
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref lt1, 1)));
        var c1_1 = new PrefabComponentHeader { componentTypeIndex = 1, dataSize = sizeof(LocalToWorld) };
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref c1_1, 1)));
        var ltw1 = new LocalToWorld { matrix = MathUtility.TRS(new float3(0.0f, 2.0f, 0.0f), quaternion.identity, new float3(1.0f)) };
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref ltw1, 1)));
        var c1_2 = new PrefabComponentHeader { componentTypeIndex = 2, dataSize = sizeof(MeshInstance) };
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref c1_2, 1)));
        var mi1 = new MeshInstance { mesh = Handle<Mesh>.Invalid, materialPalette = new Identifier<MaterialPalette>(-1) };
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref mi1, 1)));

        header.meshMaterialFixupsOffset = ms.Position;
        var fixups = new PrefabMeshMaterialFixup[]
        {
            new() { entityIndex = 1, meshDependencyIndex = 0, materialCount = 1, materialDependencyStartIndex = 1 }
        };
        ms.Write(MemoryMarshal.AsBytes((Span<PrefabMeshMaterialFixup>)fixups));

        ms.Position = 0;
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref header, 1)));

        var prefabBytes = ms.ToArray();
        var prefabGuid = Guid.NewGuid();
        var prefabEntry = new PrefabAssetEntry(assetManager, resourceDatabase, assetManager.ResourceManager, prefabGuid, new[] { meshGuid, matGuid }, "TestPrefab");
        prefabEntry.AddRef();

        fixed (byte* p = prefabBytes)
        {
            var payload = AssetPayload.FromMappedMemory(p, prefabBytes.Length);
            var loadRes = prefabEntry.OnLoadContent(ref payload);
            Assert.IsTrue(loadRes.IsSuccess, loadRes.Message);
        }

        prefabEntry.State = AssetState.Ready;

        Assert.IsNotNull(prefabEntry.PrefabData);
        Assert.HasCount(2, prefabEntry.PrefabData.Entities);

        using var world = World.Create(jobScheduler, 100);

        var spawnRes = PrefabUtility.Instantiate(world, prefabEntry, new float3(5.0f, 10.0f, 15.0f), quaternion.identity, new float3(2.0f));
        Assert.IsTrue(spawnRes.IsSuccess, spawnRes.Message);

        var rootEntity = spawnRes.Value;
        Assert.IsTrue(rootEntity.IsValid);
        Assert.IsTrue(world.EntityManager.HasComponent<Hierarchy>(rootEntity));
        Assert.IsTrue(world.EntityManager.HasComponent<LocalTransform>(rootEntity));

        ref readonly var rootLt = ref world.EntityManager.GetComponent<LocalTransform>(rootEntity);
        Assert.AreEqual(5.0f, rootLt.position.x);
        Assert.AreEqual(10.0f, rootLt.position.y);
        Assert.AreEqual(15.0f, rootLt.position.z);
        Assert.AreEqual(2.0f, rootLt.scale.x);

        ref readonly var rootH = ref world.EntityManager.GetComponent<Hierarchy>(rootEntity);
        Assert.AreEqual(Entity.Invalid, rootH.parent);
        Assert.IsTrue(rootH.firstChild.IsValid);

        var childEntity = rootH.firstChild;
        Assert.IsTrue(world.EntityManager.HasComponent<Hierarchy>(childEntity));
        ref readonly var childH = ref world.EntityManager.GetComponent<Hierarchy>(childEntity);
        Assert.AreEqual(rootEntity, childH.parent);
        Assert.AreEqual(Entity.Invalid, childH.firstChild);

        Assert.IsTrue(world.EntityManager.HasComponent<MeshInstance>(childEntity));
        ref readonly var childMeshInst = ref world.EntityManager.GetComponent<MeshInstance>(childEntity);
        Assert.AreEqual(meshHandle, childMeshInst.mesh);
        Assert.IsTrue(childMeshInst.materialPalette.IsValid);

        prefabEntry.Release();
        assetManager.RemoveEntry(meshGuid);
        assetManager.RemoveEntry(matGuid);
    }

    [TestMethod]
    public void Test_PrefabUtility_SharedMaterialDependencies_And_PreservesMeshInstanceFields()
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

        var meshGuid1 = Guid.NewGuid();
        var meshGuid2 = Guid.NewGuid();
        var matGuid = Guid.NewGuid();

        var meshDummy1 = default(Mesh);
        meshDummy1.MeshDataBuffer = resourceDatabase.CreateEmpty(name: "Mesh1_Data").AsBuffer();
        meshDummy1.MeshBuffer = resourceDatabase.CreateEmpty(name: "Mesh1_Buf").AsBuffer();
        var meshHandle1 = assetManager.ResourceManager.RegisterMesh(ref meshDummy1);

        var meshDummy2 = default(Mesh);
        meshDummy2.MeshDataBuffer = resourceDatabase.CreateEmpty(name: "Mesh2_Data").AsBuffer();
        meshDummy2.MeshBuffer = resourceDatabase.CreateEmpty(name: "Mesh2_Buf").AsBuffer();
        var meshHandle2 = assetManager.ResourceManager.RegisterMesh(ref meshDummy2);

        var shaderDesc = new GraphicsShaderDescriptor
        {
            Name = "SharedTestShader",
            PropertyBufferSize = 96,
            Passes = Array.Empty<PassDescriptor>()
        };
        var shaderHandle = assetManager.ResourceManager.CreateShader(shaderDesc);
        var matHandle = assetManager.ResourceManager.CreateMaterial(shaderHandle, "SharedMat");

        var meshEntry1 = new DummyMeshAssetEntry(assetManager, resourceDatabase, resourceManager, meshGuid1, meshHandle1, "Mesh1");
        var meshEntry2 = new DummyMeshAssetEntry(assetManager, resourceDatabase, resourceManager, meshGuid2, meshHandle2, "Mesh2");
        var matEntry = new DummyMaterialAssetEntry(assetManager, resourceDatabase, resourceManager, matGuid, matHandle, "SharedMat");
        meshEntry1.AddRef();
        meshEntry2.AddRef();
        matEntry.AddRef();
        assetManager.RegisterEntryForTesting(meshGuid1, meshEntry1);
        assetManager.RegisterEntryForTesting(meshGuid2, meshEntry2);
        assetManager.RegisterEntryForTesting(matGuid, matEntry);

        var ms = new MemoryStream();
        var materialDependencyIndices = new int[] { 2, 2 };
        var header = new PrefabContentHeader
        {
            magic = PrefabContentHeader.MAGIC,
            version = PrefabContentHeader.VERSION,
            entityCount = 3,
            materialDependencyIndexCount = materialDependencyIndices.Length,
            componentTypeCount = 3,
            meshMaterialFixupCount = 2
        };

        ms.Write(new byte[sizeof(PrefabContentHeader)]);

        header.componentTypesOffset = ms.Position;
        var typeNames = new string[]
        {
            typeof(LocalTransform).FullName!,
            typeof(LocalToWorld).FullName!,
            typeof(MeshInstance).FullName!
        };
        for (var t = 0; t < typeNames.Length; t++)
        {
            var utf8 = Encoding.UTF8.GetBytes(typeNames[t]);
            var len = (ushort)utf8.Length;
            ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref len, 1)));
            ms.Write(utf8);
        }

        header.entitiesOffset = ms.Position;
        var rootHeader = new PrefabEntityHeader { parentIndex = -1, firstChildIndex = 1, nextSiblingIndex = -1, componentCount = 2 };
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref rootHeader, 1)));
        var rc0 = new PrefabComponentHeader { componentTypeIndex = 0, dataSize = sizeof(LocalTransform) };
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref rc0, 1)));
        var rlt = new LocalTransform { position = float3.zero, rotation = quaternion.identity, scale = new float3(1.0f) };
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref rlt, 1)));
        var rc1 = new PrefabComponentHeader { componentTypeIndex = 1, dataSize = sizeof(LocalToWorld) };
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref rc1, 1)));
        var rltw = new LocalToWorld { matrix = float4x4.identity };
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref rltw, 1)));

        for (var e = 0; e < 2; e++)
        {
            var eHeader = new PrefabEntityHeader { parentIndex = 0, firstChildIndex = -1, nextSiblingIndex = e == 0 ? 2 : -1, componentCount = 3 };
            ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref eHeader, 1)));
            var c0 = new PrefabComponentHeader { componentTypeIndex = 0, dataSize = sizeof(LocalTransform) };
            ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref c0, 1)));
            var lt = new LocalTransform { position = new float3(e * 2.0f, 0.0f, 0.0f), rotation = quaternion.identity, scale = new float3(1.0f) };
            ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref lt, 1)));
            var c1 = new PrefabComponentHeader { componentTypeIndex = 1, dataSize = sizeof(LocalToWorld) };
            ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref c1, 1)));
            var ltw = new LocalToWorld { matrix = MathUtility.TRS(lt.position, quaternion.identity, new float3(1.0f)) };
            ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref ltw, 1)));
            var c2 = new PrefabComponentHeader { componentTypeIndex = 2, dataSize = sizeof(MeshInstance) };
            ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref c2, 1)));
            var mi = new MeshInstance
            {
                mesh = Handle<Mesh>.Invalid,
                materialPalette = new Identifier<MaterialPalette>(-1),
                renderingLayerMask = 0xCAFEBABEu,
                shadowCastingMode = Ghost.Engine.ShadowCastingMode.On,
                staticShadowCaster = true
            };
            ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref mi, 1)));
        }

        header.meshMaterialFixupsOffset = ms.Position;
        var fixups = new PrefabMeshMaterialFixup[]
        {
            new() { entityIndex = 1, meshDependencyIndex = 0, materialCount = 1, materialDependencyStartIndex = 0 },
            new() { entityIndex = 2, meshDependencyIndex = 1, materialCount = 1, materialDependencyStartIndex = 1 }
        };
        ms.Write(MemoryMarshal.AsBytes((Span<PrefabMeshMaterialFixup>)fixups));

        header.materialDependencyIndicesOffset = ms.Position;
        ms.Write(MemoryMarshal.AsBytes((Span<int>)materialDependencyIndices));

        ms.Position = 0;
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref header, 1)));

        var prefabBytes = ms.ToArray();
        var prefabGuid = Guid.NewGuid();
        var prefabEntry = new PrefabAssetEntry(assetManager, resourceDatabase, assetManager.ResourceManager, prefabGuid, new[] { meshGuid1, meshGuid2, matGuid }, "SharedMatPrefab");
        prefabEntry.AddRef();

        fixed (byte* p = prefabBytes)
        {
            var payload = AssetPayload.FromMappedMemory(p, prefabBytes.Length);
            var loadRes = prefabEntry.OnLoadContent(ref payload);
            Assert.IsTrue(loadRes.IsSuccess, loadRes.Message);
        }
        prefabEntry.State = AssetState.Ready;

        using var world = World.Create(jobScheduler, 100);
        var spawnRes = PrefabUtility.Instantiate(world, prefabEntry);
        Assert.IsTrue(spawnRes.IsSuccess, spawnRes.Message);

        var rootEntity = spawnRes.Value;
        var e0 = world.EntityManager.GetComponent<Hierarchy>(rootEntity).firstChild;
        var e1 = world.EntityManager.GetComponent<Hierarchy>(e0).nextSibling;
        Assert.IsTrue(e0.IsValid);
        Assert.IsTrue(e1.IsValid);

        ref readonly var mi0 = ref world.EntityManager.GetComponent<MeshInstance>(e0);
        ref readonly var mi1 = ref world.EntityManager.GetComponent<MeshInstance>(e1);

        Assert.AreEqual(meshHandle1, mi0.mesh);
        Assert.AreEqual(meshHandle2, mi1.mesh);
        Assert.IsTrue(mi0.materialPalette.IsValid);
        Assert.AreNotEqual(new Identifier<MaterialPalette>(0), mi0.materialPalette);
        Assert.AreEqual(mi0.materialPalette, mi1.materialPalette);
        Assert.AreEqual((uint)0xCAFEBABEu, (uint)mi0.renderingLayerMask);
        Assert.AreEqual(Ghost.Engine.ShadowCastingMode.On, mi0.shadowCastingMode);
        Assert.IsTrue(mi0.staticShadowCaster);
        Assert.AreEqual((uint)0xCAFEBABEu, (uint)mi1.renderingLayerMask);
        Assert.AreEqual(Ghost.Engine.ShadowCastingMode.On, mi1.shadowCastingMode);
        Assert.IsTrue(mi1.staticShadowCaster);

        prefabEntry.Release();
        meshEntry1.Release();
        meshEntry2.Release();
        matEntry.Release();
        assetManager.RemoveEntry(meshGuid1);
        assetManager.RemoveEntry(meshGuid2);
        assetManager.RemoveEntry(matGuid);
    }

    [TestMethod]
    public void Test_PrefabUtility_Instantiate_WithParentEntity()
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

        var ms = new MemoryStream();
        var header = new PrefabContentHeader
        {
            magic = PrefabContentHeader.MAGIC,
            version = PrefabContentHeader.VERSION,
            entityCount = 1,
            componentTypeCount = 2,
            meshMaterialFixupCount = 0
        };

        ms.Write(new byte[sizeof(PrefabContentHeader)]);

        header.componentTypesOffset = ms.Position;
        var typeNames = new string[] { typeof(LocalTransform).FullName!, typeof(LocalToWorld).FullName! };
        for (var t = 0; t < typeNames.Length; t++)
        {
            var utf8 = Encoding.UTF8.GetBytes(typeNames[t]);
            var len = (ushort)utf8.Length;
            ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref len, 1)));
            ms.Write(utf8);
        }

        header.entitiesOffset = ms.Position;
        var eHeader = new PrefabEntityHeader { parentIndex = -1, firstChildIndex = -1, nextSiblingIndex = -1, componentCount = 2 };
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref eHeader, 1)));
        var c0 = new PrefabComponentHeader { componentTypeIndex = 0, dataSize = sizeof(LocalTransform) };
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref c0, 1)));
        var lt = new LocalTransform { position = new float3(1.0f, 2.0f, 3.0f), rotation = quaternion.identity, scale = new float3(1.0f) };
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref lt, 1)));
        var c1 = new PrefabComponentHeader { componentTypeIndex = 1, dataSize = sizeof(LocalToWorld) };
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref c1, 1)));
        var ltw = new LocalToWorld { matrix = float4x4.identity };
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref ltw, 1)));

        header.meshMaterialFixupsOffset = ms.Position;
        ms.Position = 0;
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref header, 1)));

        var prefabBytes = ms.ToArray();
        var prefabGuid = Guid.NewGuid();
        var prefabEntry = new PrefabAssetEntry(assetManager, resourceDatabase, assetManager.ResourceManager, prefabGuid, Array.Empty<Guid>(), "ChildPrefab");
        prefabEntry.AddRef();

        fixed (byte* p = prefabBytes)
        {
            var payload = AssetPayload.FromMappedMemory(p, prefabBytes.Length);
            var loadRes = prefabEntry.OnLoadContent(ref payload);
            Assert.IsTrue(loadRes.IsSuccess, loadRes.Message);
        }
        prefabEntry.State = AssetState.Ready;

        using var world = World.Create(jobScheduler, 100);

        var existingParent = world.EntityManager.CreateEntity();
        world.EntityManager.AddComponent(existingParent, new LocalTransform { position = new float3(10.0f, 0.0f, 0.0f), rotation = quaternion.identity, scale = new float3(1.0f) });
        world.EntityManager.AddComponent(existingParent, new LocalToWorld { matrix = MathUtility.TRS(new float3(10.0f, 0.0f, 0.0f), quaternion.identity, new float3(1.0f)) });

        var spawnRes = PrefabUtility.Instantiate(world, prefabEntry, existingParent);
        Assert.IsTrue(spawnRes.IsSuccess, spawnRes.Message);

        var childRoot = spawnRes.Value;
        Assert.IsTrue(world.EntityManager.HasComponent<Hierarchy>(childRoot));
        ref readonly var childH = ref world.EntityManager.GetComponent<Hierarchy>(childRoot);
        Assert.AreEqual(existingParent, childH.parent);

        Assert.IsTrue(world.EntityManager.HasComponent<Hierarchy>(existingParent));
        ref readonly var parentH = ref world.EntityManager.GetComponent<Hierarchy>(existingParent);
        Assert.AreEqual(childRoot, parentH.firstChild);

        prefabEntry.Release();
    }

    [TestMethod]
    public void Test_PrefabUtility_Instantiate_StandaloneSingleEntity()
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

        var ms = new MemoryStream();
        var header = new PrefabContentHeader
        {
            magic = PrefabContentHeader.MAGIC,
            version = PrefabContentHeader.VERSION,
            entityCount = 1,
            componentTypeCount = 2,
            meshMaterialFixupCount = 0
        };

        ms.Write(new byte[sizeof(PrefabContentHeader)]);

        header.componentTypesOffset = ms.Position;
        var typeNames = new string[] { typeof(LocalTransform).FullName!, typeof(LocalToWorld).FullName! };
        for (var t = 0; t < typeNames.Length; t++)
        {
            var utf8 = Encoding.UTF8.GetBytes(typeNames[t]);
            var len = (ushort)utf8.Length;
            ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref len, 1)));
            ms.Write(utf8);
        }

        header.entitiesOffset = ms.Position;
        var eHeader = new PrefabEntityHeader { parentIndex = -1, firstChildIndex = -1, nextSiblingIndex = -1, componentCount = 2 };
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref eHeader, 1)));
        var c0 = new PrefabComponentHeader { componentTypeIndex = 0, dataSize = sizeof(LocalTransform) };
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref c0, 1)));
        var lt = new LocalTransform { position = new float3(1.0f, 2.0f, 3.0f), rotation = quaternion.identity, scale = new float3(1.0f) };
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref lt, 1)));
        var c1 = new PrefabComponentHeader { componentTypeIndex = 1, dataSize = sizeof(LocalToWorld) };
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref c1, 1)));
        var ltw = new LocalToWorld { matrix = float4x4.identity };
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref ltw, 1)));

        header.meshMaterialFixupsOffset = ms.Position;
        ms.Position = 0;
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref header, 1)));

        var prefabBytes = ms.ToArray();
        var prefabGuid = Guid.NewGuid();
        var prefabEntry = new PrefabAssetEntry(assetManager, resourceDatabase, assetManager.ResourceManager, prefabGuid, Array.Empty<Guid>(), "SinglePrefab");
        prefabEntry.AddRef();

        fixed (byte* p = prefabBytes)
        {
            var payload = AssetPayload.FromMappedMemory(p, prefabBytes.Length);
            var loadRes = prefabEntry.OnLoadContent(ref payload);
            Assert.IsTrue(loadRes.IsSuccess, loadRes.Message);
        }
        prefabEntry.State = AssetState.Ready;

        using var world = World.Create(jobScheduler, 100);

        var spawnRes = PrefabUtility.Instantiate(world, prefabEntry);
        Assert.IsTrue(spawnRes.IsSuccess, spawnRes.Message);

        var entity = spawnRes.Value;
        Assert.IsTrue(entity.IsValid);
        Assert.IsFalse(world.EntityManager.HasComponent<Hierarchy>(entity));
        Assert.IsTrue(world.EntityManager.HasComponent<LocalTransform>(entity));
        Assert.IsTrue(world.EntityManager.HasComponent<LocalToWorld>(entity));

        // Test fire-and-forget instantiate by count (caller does not care about receiving entity handles)
        var countOnlyRes = PrefabUtility.Instantiate(world, prefabEntry, 10, new float3(5, 5, 5), quaternion.identity, new float3(1));
        Assert.IsTrue(countOnlyRes.IsSuccess, countOnlyRes.Message);
        Span<Entity> batchEntities = stackalloc Entity[20];
        var batchRes = PrefabUtility.Instantiate(world, prefabEntry, batchEntities, new float3(1, 2, 3), quaternion.identity, new float3(1));
        Assert.IsTrue(batchRes.IsSuccess, batchRes.Message);
        for (var i = 0; i < batchEntities.Length; i++)
        {
            var e = batchEntities[i];
            Assert.IsTrue(e.IsValid);
            Assert.IsTrue(world.EntityManager.HasComponent<LocalTransform>(e));
            Assert.IsTrue(world.EntityManager.HasComponent<LocalToWorld>(e));
            ref readonly var trans = ref world.EntityManager.GetComponent<LocalTransform>(e);
            Assert.AreEqual(1.0f, trans.position.x);
        }

        prefabEntry.Release();
    }

    [TestMethod]
    public void Test_SubAsset_DirectResolutionAndLoading()
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

        var meshGuid = Guid.NewGuid();
        contentProvider.AddMockMesh(meshGuid);
        contentProvider.RegisterPath("Meshes/dragon#dragon_original", meshGuid);

        var entry = assetManager.ResolveAsset("Meshes/dragon#dragon_original");
        Assert.IsNotNull(entry);
        Assert.AreEqual(AssetType.Mesh, entry.AssetType);
        Assert.AreEqual(meshGuid, entry.AssetId);

        entry.Release();
    }

    private sealed class DummyShaderAssetEntry : AssetEntry
    {
        private readonly Handle<Shader> _handle;
        public DummyShaderAssetEntry(AssetManager manager, IResourceDatabase resourceDatabase, ResourceManager resourceManager, Guid assetId, Handle<Shader> handle, string assetName)
            : base(manager, resourceDatabase, resourceManager, assetId, AssetType.Shader, Array.Empty<Guid>(), assetName)
        {
            _handle = handle;
            State = AssetState.Ready;
        }
        public override void ReadAssetData(Span<byte> dst) => MemoryMarshal.Write(dst, in _handle);
        public override void ReadAssetData<T>(ref T dst) => dst = System.Runtime.CompilerServices.Unsafe.BitCast<Handle<Shader>, T>(_handle);
    }

    private sealed class DummyMeshAssetEntry : AssetEntry
    {
        private readonly Handle<Mesh> _handle;
        public DummyMeshAssetEntry(AssetManager manager, IResourceDatabase resourceDatabase, ResourceManager resourceManager, Guid assetId, Handle<Mesh> handle, string assetName)
            : base(manager, resourceDatabase, resourceManager, assetId, AssetType.Mesh, Array.Empty<Guid>(), assetName)
        {
            _handle = handle;
            State = AssetState.Ready;
        }
        public override void ReadAssetData(Span<byte> dst) => MemoryMarshal.Write(dst, in _handle);
        public override void ReadAssetData<T>(ref T dst) => dst = System.Runtime.CompilerServices.Unsafe.BitCast<Handle<Mesh>, T>(_handle);
    }

    private sealed class DummyMaterialAssetEntry : AssetEntry
    {
        private readonly Handle<Material> _handle;
        public DummyMaterialAssetEntry(AssetManager manager, IResourceDatabase resourceDatabase, ResourceManager resourceManager, Guid assetId, Handle<Material> handle, string assetName)
            : base(manager, resourceDatabase, resourceManager, assetId, AssetType.Material, Array.Empty<Guid>(), assetName)
        {
            _handle = handle;
            State = AssetState.Ready;
        }
        public override void ReadAssetData(Span<byte> dst) => MemoryMarshal.Write(dst, in _handle);
        public override void ReadAssetData<T>(ref T dst) => dst = System.Runtime.CompilerServices.Unsafe.BitCast<Handle<Material>, T>(_handle);
    }
}
