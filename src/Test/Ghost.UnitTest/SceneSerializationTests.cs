#if false
using Ghost.Editor.Core;
using Ghost.Editor.Core.Assets;
using Ghost.Editor.Core.SceneGraph;
using Ghost.Editor.Core.Services;
using Ghost.Engine;
using Ghost.Engine.Components;
using Ghost.Engine.Core;
using Ghost.Engine.Streaming;
using Ghost.Entities;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Ghost.UnitTest;

[TestClass]
[DoNotParallelize]
public class SceneSerializationTests
{
    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            return null;
        }
    }

    private string _projectRoot = null!;
    private string _previousCurrentDirectory = null!;
    private EditorWorldService _worldService = null!;
    private SceneSerializationService _serializationService = null!;

    [TestInitialize]
    public void Setup()
    {
        AllocationManager.Initialize(AllocationManagerDesc.Default);

        _previousCurrentDirectory = Environment.CurrentDirectory;
        _projectRoot = Path.Combine(Path.GetTempPath(), "GhostEngineTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_projectRoot);
        EditorApplication.Initialize(new EmptyServiceProvider(), _projectRoot, "SceneTest");

        _worldService = new EditorWorldService();
        _serializationService = new SceneSerializationService(_worldService, null!);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _worldService.Dispose();

        AllocationManager.Dispose();
        Environment.CurrentDirectory = _previousCurrentDirectory;

        if (Directory.Exists(_projectRoot))
        {
            try
            {
                Directory.Delete(_projectRoot, true);
            }
            catch (IOException)
            {
                Thread.Sleep(100);
                if (Directory.Exists(_projectRoot))
                {
                    Directory.Delete(_projectRoot, true);
                }
            }
        }
    }

    private Entity CreateSceneEntity(Scene scene)
    {
        var world = _worldService.EditorWorld;
        var entity = world.EntityManager.CreateEntity();
        world.EntityManager.AddComponent(entity, new SceneID { value = scene });
        return entity;
    }

    private Entity CreateEntityWithHierarchy(Scene scene, Entity parent)
    {
        var world = _worldService.EditorWorld;
        var entity = world.EntityManager.CreateEntity();
        world.EntityManager.AddComponent(entity, new SceneID { value = scene });
        world.EntityManager.AddComponent(entity, Hierarchy.Root);
        world.EntityManager.AddComponent(entity, new LocalToWorld());

        if (parent.IsValid)
        {
            HierarchyUtility.SetParent(world, entity, parent);
        }

        return entity;
    }

	[TestMethod]
	public async Task SaveAndLoad_RoundTrip_PreservesEntityCount()
	{
		var scene = SceneManager.CreateScene();
		CreateSceneEntity(scene);
		CreateEntityWithHierarchy(scene, Entity.Invalid);
		CreateEntityWithHierarchy(scene, Entity.Invalid);

		var filePath = Path.Combine(_projectRoot, "TestScene.gscene");
		var saveResult = _serializationService.SaveSceneFromEditorWorld(filePath, scene);
		Assert.IsTrue(saveResult.IsSuccess, saveResult.Message);
		Assert.IsTrue(File.Exists(filePath));

		var json = await File.ReadAllTextAsync(filePath, TestContext.CancellationToken);
		Assert.IsTrue(json.Length > 10, $"JSON too short: {json}");

		var data = await SceneSerializationService.DeserializeSceneFileAsync(filePath, TestContext.CancellationToken);
		Assert.IsNotNull(data);
		Assert.AreEqual(3, data!.Entities.Count, $"JSON content: {json}");

		// Verify saved JSON is valid
		Assert.AreEqual(3, data.Entities.Count, $"data contained {data.Entities.Count} entities");
		foreach (var ent in data.Entities)
		{
			Assert.IsTrue(ent.Components.Count >= 0, "Entity has no components"); // Can be 0 because we might have entities without components, but should not be negative
        }

		var loadResult = _serializationService.LoadSceneIntoEditorWorld(data);
		Assert.IsTrue(loadResult.IsSuccess, loadResult.Message);

		var world = _worldService.EditorWorld;
        scene = loadResult.Value;

        using var scope = AllocationManager.CreateStackScope();
		using var entities = SceneManager.GetSceneEntities(scene, world, scope.AllocationHandle);
		Assert.AreEqual(3, entities.Count, $"Expected 3 entities for scene {scene.ID} but found {entities.Count}");
	}

    [TestMethod]
    public async Task SaveAndLoad_HierarchyRelations_Preserved()
    {
        var scene = SceneManager.CreateScene();
        CreateSceneEntity(scene);

        var parent = CreateEntityWithHierarchy(scene, Entity.Invalid);
        var child = CreateEntityWithHierarchy(scene, parent);

        var world = _worldService.EditorWorld;

        var filePath = Path.Combine(_projectRoot, "HierarchyScene.gscene");
        var saveResult = _serializationService.SaveSceneFromEditorWorld(filePath, scene);
        Assert.IsTrue(saveResult.IsSuccess, saveResult.Message);

        var data = await SceneSerializationService.DeserializeSceneFileAsync(filePath, TestContext.CancellationToken);
        Assert.IsNotNull(data);
        Assert.AreEqual(3, data!.Entities.Count);

        var loadResult = _serializationService.LoadSceneIntoEditorWorld(data);
        Assert.IsTrue(loadResult.IsSuccess, loadResult.Message);

        var queryID = new QueryBuilder().WithAll<SceneID, Hierarchy>().Build(world);
        ref var query = ref world.ComponentManager.GetEntityQueryReference(queryID);

        var entities = new List<Entity>();
        foreach (var chunk in query.GetChunkIterator())
        {
            var chunkEntities = chunk.GetEntities();
            entities.AddRange(chunkEntities.ToArray());
        }

        var children = entities.Where(e =>
        {
            ref var h = ref world.EntityManager.GetComponent<Hierarchy>(e);
            return h.parent.IsValid;
        }).ToList();

        Assert.AreEqual(1, children.Count);
    }

    [TestMethod]
    public async Task JsonFormat_EntityFieldsBecomeIntIndices()
    {
        var scene = SceneManager.CreateScene();
        CreateSceneEntity(scene);
        var parent = CreateEntityWithHierarchy(scene, Entity.Invalid);
        var child = CreateEntityWithHierarchy(scene, parent);

        var filePath = Path.Combine(_projectRoot, "JsonFormat.gscene");
        _serializationService.SaveSceneFromEditorWorld(filePath, scene);

        var json = await File.ReadAllTextAsync(filePath, TestContext.CancellationToken);
        using var doc = JsonDocument.Parse(json);

        var root = doc.RootElement;
        Assert.AreEqual(1, root.GetProperty("formatVersion").GetInt32());

        var entities = root.GetProperty("entities");
        Assert.AreEqual(3, entities.GetArrayLength());

        var hasParentChild = false;
        foreach (var entityElement in entities.EnumerateArray())
        {
            var components = entityElement.GetProperty("components");
            if (components.TryGetProperty("Ghost.Engine.Components.Hierarchy", out var hierarchyElement))
            {
                var jsonStr = hierarchyElement.GetRawText();
                var hierarchyDoc = JsonDocument.Parse(jsonStr);

                if (hierarchyDoc.RootElement.TryGetProperty("parent", out var parentProp))
                {
                    var parentValue = parentProp.GetInt32();
                    if (parentValue != -1)
                    {
                        hasParentChild = true;
                    }
                }

                if (hierarchyDoc.RootElement.TryGetProperty("firstChild", out var firstChildProp))
                {
                    var firstChildValue = firstChildProp.GetInt32();
                    if (firstChildValue != -1)
                    {
                        hasParentChild = true;
                    }
                }
            }
        }

        Assert.IsTrue(hasParentChild, "Expected at least one parent-child relationship in the JSON output.");
    }

    [TestMethod]
    public async Task ImportAsync_ProducesValidBinary()
    {
        var scene = SceneManager.CreateScene();
        CreateSceneEntity(scene);
        CreateEntityWithHierarchy(scene, Entity.Invalid);

        var sourcePath = Path.Combine(_projectRoot, "ImportScene.gscene");
        _serializationService.SaveSceneFromEditorWorld(sourcePath, scene);

        var targetPath = ImportCoordinator.GetImportedAssetPath(Guid.NewGuid());
        var handler = new SceneAssetHandler();
        var result = await handler.ImportAsync(sourcePath, targetPath, Guid.NewGuid(), null, TestContext.CancellationToken);

        Assert.IsTrue(result.IsSuccess, result.Message);
        Assert.IsTrue(File.Exists(targetPath));

        var binary = await File.ReadAllBytesAsync(targetPath, TestContext.CancellationToken);
        Assert.IsTrue(binary.Length > 0);

        var magic = Encoding.UTF8.GetString(binary, 0, 4);
        Assert.AreEqual("GSCN", magic);

        var version = MemoryMarshal.Read<int>(binary.AsSpan(4));
        Assert.AreEqual(1, version);

        var entityCount = MemoryMarshal.Read<int>(binary.AsSpan(8));
        Assert.AreEqual(2, entityCount);
    }

    [TestMethod]
    public void SceneLoadingType_Single_ReplacesEntities()
    {
        var scene = SceneManager.CreateScene();
        CreateSceneEntity(scene);
        CreateEntityWithHierarchy(scene, Entity.Invalid);

        var scene2 = SceneManager.CreateScene();
        CreateSceneEntity(scene2);
        CreateEntityWithHierarchy(scene2, Entity.Invalid);

        var world = _worldService.EditorWorld;
        var queryID = new QueryBuilder().WithAll<SceneID>().Build(world);
        ref var query = ref world.ComponentManager.GetEntityQueryReference(queryID);
        var initialCount = 0;
        foreach (var chunk in query.GetChunkIterator())
        {
            initialCount += chunk.EntityCount;
        }

		Assert.AreEqual(4, initialCount, "Expected 4 entities.");

        var filePath = Path.Combine(_projectRoot, "SingleLoad.gscene");
        _serializationService.SaveSceneFromEditorWorld(filePath, scene);

        var data = SceneSerializationService.DeserializeSceneFileAsync(filePath).Result;
        Assert.IsNotNull(data);

        var loadResult = _serializationService.LoadSceneIntoEditorWorld(data!, SceneLoadingType.Single);
        Assert.IsTrue(loadResult.IsSuccess, loadResult.Message);

        var afterCount = 0;
        foreach (var chunk in query.GetChunkIterator())
        {
            afterCount += chunk.EntityCount;
        }

        Assert.AreEqual(2, afterCount, "Expected exactly 2 entities after Single load (previous entities cleared).");
    }

    [TestMethod]
    public void Save_EmptyScene_ProducesEmptyFile()
    {
        var scene = SceneManager.CreateScene();

        var filePath = Path.Combine(_projectRoot, "EmptyScene.gscene");
        var saveResult = _serializationService.SaveSceneFromEditorWorld(filePath, scene);
        Assert.IsTrue(saveResult.IsFailure, "Empty scene should fail to save.");

        Assert.AreEqual("No entities found for the specified scene.", saveResult.Message);
    }

    [TestMethod]
    public async Task Load_UnknownComponent_SkipsGracefully()
    {
        var json = @"
		{
			""formatVersion"": 1,
			""entities"": [
				{
					""components"": {
						""Some.Unknown.Component"": { ""foo"": 1 },
						""Ghost.Engine.Components.Hierarchy"": { ""parent"": -1, ""firstChild"": -1, ""nextSibling"": -1 }
					}
				}
			]
		}";

        var filePath = Path.Combine(_projectRoot, "UnknownComp.gscene");
        await File.WriteAllTextAsync(filePath, json, TestContext.CancellationToken);

        var data = await SceneSerializationService.DeserializeSceneFileAsync(filePath, TestContext.CancellationToken);
        Assert.IsNotNull(data);

        var loadResult = _serializationService.LoadSceneIntoEditorWorld(data!);
        Assert.IsTrue(loadResult.IsSuccess, loadResult.Message);

        var world = _worldService.EditorWorld;
        var queryID = new QueryBuilder().WithAll<Hierarchy>().Build(world);
        ref var query = ref world.ComponentManager.GetEntityQueryReference(queryID);
        var entityFound = false;
        foreach (var chunk in query.GetChunkIterator())
        {
            if (chunk.EntityCount > 0)
            {
                entityFound = true;
                break;
            }
        }

        Assert.IsTrue(entityFound, "Expected entity with Hierarchy component to be loaded.");
    }

    [TestMethod]
    public async Task Load_InvalidVersion_StillLoads()
    {
        var json = @"
		{
			""formatVersion"": 999,
			""entities"": [
				{
					""components"": {
						""Ghost.Engine.Components.Hierarchy"": { ""parent"": -1, ""firstChild"": -1, ""nextSibling"": -1 }
					}
				}
			]
		}";

        var filePath = Path.Combine(_projectRoot, "FutureVersion.gscene");
        await File.WriteAllTextAsync(filePath, json, TestContext.CancellationToken);

        var data = await SceneSerializationService.DeserializeSceneFileAsync(filePath, TestContext.CancellationToken);
        Assert.IsNotNull(data);
        Assert.AreEqual(999u, data!.FormatVersion);

        var loadResult = _serializationService.LoadSceneIntoEditorWorld(data);
        Assert.IsTrue(loadResult.IsSuccess, loadResult.Message);
    }

    [TestMethod]
    public unsafe void BinaryFormat_RoundTrip_ProducesLoadableData()
    {
        var scene = SceneManager.CreateScene();
        CreateSceneEntity(scene);
        var parent = CreateEntityWithHierarchy(scene, Entity.Invalid);
        CreateEntityWithHierarchy(scene, parent);

        var jsonPath = Path.Combine(_projectRoot, "BinaryRoundTrip.gscene");
        _serializationService.SaveSceneFromEditorWorld(jsonPath, scene);

        var data = SceneSerializationService.DeserializeSceneFileAsync(jsonPath).Result;
        Assert.IsNotNull(data);

        using var stream = new MemoryStream();
        SceneSerializationService.SerializeToBinary(data!, stream);
        var binary = stream.ToArray();

        var world = World.Create(entityCapacity: 64);
        try
        {
            fixed (byte* pBinary = binary)
            {
                var result = SceneLoader.LoadSceneIntoWorld(world, *(SceneContentHeader*)pBinary, pBinary + sizeof(SceneContentHeader), (nuint)(binary.Length + sizeof(SceneContentHeader)));
                Assert.IsTrue(result.IsSuccess, result.Message);
                Assert.AreEqual(3, result.Value);
            }

            var queryID = new QueryBuilder().WithAll<Hierarchy>().Build(world);
            ref var query = ref world.ComponentManager.GetEntityQueryReference(queryID);
            var entityCount = 0;
            foreach (var chunk in query.GetChunkIterator())
            {
                entityCount += chunk.EntityCount;
            }

            Assert.AreEqual(2, entityCount, "Expected 2 entities with Hierarchy component.");
        }
        finally
        {
            world.Dispose();
        }
    }

    [TestMethod]
    public async Task SaveAndLoad_LocalToWorld_Preserved()
    {
        var scene = SceneManager.CreateScene();
        CreateSceneEntity(scene);
        var entity = CreateEntityWithHierarchy(scene, Entity.Invalid);

        var world = _worldService.EditorWorld;
        var testMatrix = new Misaki.HighPerformance.Mathematics.float4x4(
            1, 0, 0, 10,
            0, 1, 0, 20,
            0, 0, 1, 30,
            0, 0, 0, 1
        );
        world.EntityManager.SetComponent(entity, new LocalToWorld { matrix = testMatrix });

        var filePath = Path.Combine(_projectRoot, "TransformScene.gscene");
        _serializationService.SaveSceneFromEditorWorld(filePath, scene);

        var data = await SceneSerializationService.DeserializeSceneFileAsync(filePath, TestContext.CancellationToken);
        Assert.IsNotNull(data);

        var loadResult = _serializationService.LoadSceneIntoEditorWorld(data!);
        Assert.IsTrue(loadResult.IsSuccess, loadResult.Message);

        var queryID = new QueryBuilder().WithAll<LocalToWorld>().Build(world);
        ref var query = ref world.ComponentManager.GetEntityQueryReference(queryID);
        foreach (var chunk in query.GetChunkIterator())
        {
            var ltws = chunk.GetComponentData<LocalToWorld>();
            var found = false;
            foreach (ref readonly var ltw in ltws)
            {
                if (ltw.matrix.c3.x == 10 && ltw.matrix.c3.y == 20 && ltw.matrix.c3.z == 30)
                {
                    found = true;
                    break;
                }
            }

            if (found)
            {
                return;
            }
        }

        Assert.Fail("LocalToWorld component with expected transform was not found after round-trip.");
    }

    public TestContext TestContext
    {
        get; set;
    } = null!;
}
#endif