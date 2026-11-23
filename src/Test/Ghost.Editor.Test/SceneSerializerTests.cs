using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Ghost.AssetForge.Core.Models;
using Ghost.AssetForge.Core.Services;
using Ghost.Core;
using Ghost.Editor.Core.Commands;
using Ghost.Editor.Core.Serialization;
using Ghost.Editor.Core.Sync;
using Ghost.Engine;
using Ghost.Engine.Components;
using Ghost.Engine.Streaming;
using Ghost.Engine.Utilities;
using Ghost.Engine.Volumes;
using Ghost.Entities;
using Ghost.Graphics.Core;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Editor.Test;

[TestClass]
[DoNotParallelize]
public unsafe class SceneSerializerTests
{
    public struct TestTargetRefComponent : IComponentData
    {
        public Entity target;
    }

    private static byte[] BuildTestPrefabPayload(float3 rootPos, float3 childPos)
    {
        var ms = new MemoryStream();
        var header = new PrefabContentHeader
        {
            magic = PrefabContentHeader.MAGIC,
            version = PrefabContentHeader.VERSION,
            entityCount = 2,
            componentTypeCount = 2,
            meshMaterialFixupCount = 0
        };

        ms.Write(new byte[sizeof(PrefabContentHeader)]);

        header.componentTypesOffset = ms.Position;
        var typeNames = new[] { typeof(LocalTransform).FullName!, typeof(LocalToWorld).FullName! };
        for (var i = 0; i < typeNames.Length; i++)
        {
            var utf8 = Encoding.UTF8.GetBytes(typeNames[i]);
            var len = (ushort)utf8.Length;
            ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref len, 1)));
            ms.Write(utf8);
        }

        header.entitiesOffset = ms.Position;

        var e0Header = new PrefabEntityHeader { parentIndex = -1, firstChildIndex = 1, nextSiblingIndex = -1, componentCount = 2 };
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref e0Header, 1)));
        WriteStructComponent(ms, 0, LocalTransform.FromPosition(rootPos));
        WriteStructComponent(ms, 1, new LocalToWorld { matrix = MathUtility.TRS(rootPos, quaternion.identity, float3.one) });

        var e1Header = new PrefabEntityHeader { parentIndex = 0, firstChildIndex = -1, nextSiblingIndex = -1, componentCount = 2 };
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref e1Header, 1)));
        WriteStructComponent(ms, 0, LocalTransform.FromPosition(childPos));
        WriteStructComponent(ms, 1, new LocalToWorld { matrix = MathUtility.TRS(childPos, quaternion.identity, float3.one) });

        header.meshMaterialFixupsOffset = ms.Position;
        ms.Position = 0;
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref header, 1)));
        return ms.ToArray();
    }

    private static void WriteStructComponent<T>(Stream ms, int typeIndex, in T value)
        where T : unmanaged
    {
        var compHeader = new PrefabComponentHeader { componentTypeIndex = typeIndex, dataSize = sizeof(T) };
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref compHeader, 1)));
        ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in value), 1)));
    }

    private static PrefabAssetEntry CreateLoadedPrefabEntry(Guid assetId, string name, byte[] bytes)
    {
        var entry = new PrefabAssetEntry(null!, null!, null!, assetId, Array.Empty<Guid>(), name);
        fixed (byte* p = bytes)
        {
            var payload = AssetPayload.FromMappedMemory(p, bytes.Length);
            var res = entry.OnLoadContent(ref payload);
            Assert.IsTrue(res.IsSuccess, res.Message);
        }

        entry.State = AssetState.Ready;
        return entry;
    }

    [TestMethod]
    public void SceneSerializer_RoundTripsStandaloneEntitiesVolumesHierarchyAndEntityRefs()
    {
        ComponentRegistry.GetOrRegisterComponentID<TestTargetRefComponent>();
        var tempDir = Path.Combine(Path.GetTempPath(), $"GhostSceneRoundTrip_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var scenePath = Path.Combine(tempDir, $"Main{SceneAssetDocument.EXTENSION}");
            using var scheduler = new JobScheduler(new JobSchedulerDesc { ThreadCount = 1, ThreadPriority = ThreadPriority.Normal, DependencyChainCapacity = 64 });
            using var world = World.Create(scheduler, 64);
            var em = world.EntityManager;
            var bridge = new EditorSyncBridge();

            // Persistent entity without SceneID (simulating EditorCamera / persistent manager)
            var persistentEntity = em.CreateEntity(LocalTransform.FromPosition(new float3(99.0f, 88.0f, 77.0f)));

            var parentEcs = em.CreateEntity(LocalTransform.FromPosition(new float3(1.0f, 2.0f, 3.0f)), new LocalToWorld { matrix = MathUtility.TRS(new float3(1.0f, 2.0f, 3.0f), quaternion.identity, float3.one) }, new GPUInstanceRef { gpuInstanceIndex = 42 }, Hierarchy.Root);
            em.AddSharedComponent(parentEcs, new SceneID { value = 0 });

            var childAEcs = em.CreateEntity(LocalTransform.FromPosition(new float3(10.0f, 0.0f, 0.0f)), new MeshInstance { renderingLayerMask = new RenderingLayerMask(0x000F0001u) }, Hierarchy.Root);
            em.AddSharedComponent(childAEcs, new SceneID { value = 0 });

            var childBEcs = em.CreateEntity(LocalTransform.FromPosition(new float3(20.0f, 0.0f, 0.0f)), new TestTargetRefComponent(), Hierarchy.Root);
            em.AddSharedComponent(childBEcs, new SceneID { value = 0 });

            // Note: SetParent prepends to firstChild, so attach ChildB first then ChildA to produce sibling order [ChildA, ChildB]
            HierarchyUtility.SetParent(world, childBEcs, parentEcs);
            HierarchyUtility.SetParent(world, childAEcs, parentEcs);

            var volContainer = RenderVolumeDataContainer.Create();
            var volumeEcs = em.CreateEntity(RenderVolume.DefaultGlobal, volContainer);
            em.AddSharedComponent(volumeEcs, new SceneID { value = 0 });

            var parentNode = new EntityNode(1, "ParentRoot", Array.Empty<ComponentNode>()) { EcsEntity = parentEcs };
            var parentLt = ComponentNodeFactory.CreateComponentNode(typeof(LocalTransform), parentNode, em.GetComponent<LocalTransform>(parentEcs), bridge);
            var parentLtw = ComponentNodeFactory.CreateComponentNode(typeof(LocalToWorld), parentNode, em.GetComponent<LocalToWorld>(parentEcs), bridge);
            var parentGpu = ComponentNodeFactory.CreateComponentNode(typeof(GPUInstanceRef), parentNode, em.GetComponent<GPUInstanceRef>(parentEcs), bridge);
            parentNode.Components = new[] { parentLt, parentLtw, parentGpu };

            var meshGuid = Guid.NewGuid();
            var mat1Guid = Guid.NewGuid();
            var mat2Guid = Guid.NewGuid();
            var childANode = new EntityNode(2, "ChildA", Array.Empty<ComponentNode>(), parentNode.Id) { EcsEntity = childAEcs };
            var childALt = ComponentNodeFactory.CreateComponentNode(typeof(LocalTransform), childANode, em.GetComponent<LocalTransform>(childAEcs), bridge);
            var childAMesh = ComponentNodeFactory.CreateComponentNode(typeof(MeshInstance), childANode, em.GetComponent<MeshInstance>(childAEcs), bridge);
            childAMesh.FindProperty(nameof(MeshInstance.mesh))!.ApplyValue(meshGuid);
            childAMesh.FindProperty(nameof(MeshInstance.materialPalette))!.ApplyValue(new[] { mat1Guid, mat2Guid });
            childANode.Components = new[] { childALt, childAMesh };

            var childBNode = new EntityNode(3, "ChildB", Array.Empty<ComponentNode>(), parentNode.Id) { EcsEntity = childBEcs };
            var childBLt = ComponentNodeFactory.CreateComponentNode(typeof(LocalTransform), childBNode, em.GetComponent<LocalTransform>(childBEcs), bridge);
            var childBRef = ComponentNodeFactory.CreateComponentNode(typeof(TestTargetRefComponent), childBNode, em.GetComponent<TestTargetRefComponent>(childBEcs), bridge);
            childANode.EditorGuid = Guid.NewGuid();
            childBNode.EditorGuid = Guid.NewGuid();
            var volNode = new EntityNode(4, "Global Volume", Array.Empty<ComponentNode>()) { EcsEntity = volumeEcs };
            var volComp = ComponentNodeFactory.CreateComponentNode(typeof(RenderVolume), volNode, em.GetComponent<RenderVolume>(volumeEcs), bridge);
            var volDataComp = ComponentNodeFactory.CreateVolumeComponentNode(volNode, in em.GetComponent<RenderVolumeDataContainer>(volumeEcs), bridge);
            var volumeOverridesProp = (VolumeOverridesPropertyNode)volDataComp.Properties[0];
            var skyboxGuid = Guid.NewGuid();
            volumeOverridesProp.SetOverride(typeof(ExposureSettings));
            volumeOverridesProp.SetFieldState(typeof(ExposureSettings), nameof(ExposureSettings.fixedExposure), true, 12.25f);
            volumeOverridesProp.SetOverride(typeof(HDRISettings));
            volumeOverridesProp.SetFieldState(typeof(HDRISettings), nameof(HDRISettings.texture), true, skyboxGuid);
            volNode.Components = new[] { volComp, volDataComp };

            var snapshot = new SceneSnapshot(new[] { parentNode, childANode, childBNode, volNode });
            bridge.PublishSnapshot(snapshot);
            childBRef.FindProperty(nameof(TestTargetRefComponent.target))!.ApplyValue(childANode.EditorGuid);
            childBNode.Components = new[] { childBLt, childBRef };

            var savedDoc = SceneSerializer.SaveToFile(scenePath, snapshot, bridge);
            Assert.HasCount(4, savedDoc.Entities);
            Assert.IsFalse(savedDoc.Entities[0].Components.Any(c => c.Type == typeof(GPUInstanceRef).FullName));
            Assert.IsTrue(savedDoc.Entities[0].Components.Any(c => c.Type == typeof(LocalToWorld).FullName));

            // Reload cleanly in Single mode
            var loadRes = SceneSerializer.LoadFromFile(world, bridge, scenePath, SceneLoadingType.Single);
            Assert.IsTrue(loadRes.IsSuccess, loadRes.Message);

            // Persistent entity without SceneID survived Single scene load
            Assert.IsTrue(em.Exists(persistentEntity));
            Assert.AreEqual(new float3(99.0f, 88.0f, 77.0f), em.GetComponent<LocalTransform>(persistentEntity).position);

            // Previous SceneID entities were destroyed
            Assert.IsFalse(em.Exists(parentEcs));
            Assert.IsFalse(em.Exists(childAEcs));
            Assert.IsFalse(em.Exists(volumeEcs));

            var reloaded = bridge.LatestSnapshot;
            Assert.IsNotNull(reloaded);
            Assert.HasCount(4, reloaded.Entities);

            var rParent = reloaded.Entities.Single(e => e.Name == "ParentRoot");
            var rChildA = reloaded.Entities.Single(e => e.Name == "ChildA");
            var rChildB = reloaded.Entities.Single(e => e.Name == "ChildB");
            var rVol = reloaded.Entities.Single(e => e.Name == "Global Volume");

            Assert.AreEqual(parentNode.EditorGuid, rParent.EditorGuid);
            Assert.AreEqual(childANode.EditorGuid, rChildA.EditorGuid);
            Assert.AreEqual(rParent.Id, rChildA.ParentId);
            Assert.AreEqual(rParent.Id, rChildB.ParentId);

            // Verify Hierarchy sibling order preserved [ChildA, ChildB]
            var firstChild = em.GetComponent<Hierarchy>(rParent.EcsEntity).firstChild;
            Assert.AreEqual(rChildA.EcsEntity, firstChild);
            var secondChild = em.GetComponent<Hierarchy>(firstChild).nextSibling;
            Assert.AreEqual(rChildB.EcsEntity, secondChild);

            // Verify MeshInstance GUIDs + RenderingLayerMask preserved
            var rMeshComp = rChildA.Components.Single(c => c.ComponentType == typeof(MeshInstance));
            Assert.AreEqual(meshGuid, ((AssetPropertyNode)rMeshComp.FindProperty(nameof(MeshInstance.mesh))!).AssetGuid);
            CollectionAssert.AreEqual(new[] { mat1Guid, mat2Guid }, ((MaterialPalettePropertyNode)rMeshComp.FindProperty(nameof(MeshInstance.materialPalette))!).MaterialGuids.ToArray());
            Assert.AreEqual(new RenderingLayerMask(0x000F0001u), em.GetComponent<MeshInstance>(rChildA.EcsEntity).renderingLayerMask);

            // Verify EntityRef bound to reloaded ChildA ECS handle
            var rRefComp = rChildB.Components.Single(c => c.ComponentType == typeof(TestTargetRefComponent));
            var rTargetProp = (EntityRefPropertyNode)rRefComp.FindProperty(nameof(TestTargetRefComponent.target))!;
            Assert.AreEqual(rChildA.EditorGuid, rTargetProp.TargetEditorGuid);
            Assert.AreEqual(rChildA.EcsEntity, em.GetComponent<TestTargetRefComponent>(rChildB.EcsEntity).target);

            // Verify RenderVolumeDataContainer overrides & texture GUID restored
            Assert.IsTrue(em.GetComponent<RenderVolumeDataContainer>(rVol.EcsEntity).TryGet<ExposureSettings>(out var rExp));
            Assert.AreEqual(12.25f, rExp.fixedExposure);
            var rVolOverrides = (VolumeOverridesPropertyNode)rVol.Components.Single(c => c.ComponentType == typeof(RenderVolumeDataContainer)).Properties[0];
            var envAssetNode = (AssetPropertyNode)rVolOverrides.Overrides.Single(o => o.SettingsType == typeof(HDRISettings)).FindField(nameof(HDRISettings.texture))!.ValueNode;
            Assert.AreEqual(skyboxGuid, envAssetNode.AssetGuid);

            em.GetComponent<RenderVolumeDataContainer>(rVol.EcsEntity).Dispose();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [TestMethod]
    public void SceneSerializer_LinksPrefabsAndPreservesSparseOverridesAcrossPrefabEdits()
    {
        using var scheduler = new JobScheduler(new JobSchedulerDesc { ThreadCount = 1, ThreadPriority = ThreadPriority.Normal, DependencyChainCapacity = 64 });
        using var world = World.Create(scheduler, 64);
        var em = world.EntityManager;
        var bridge = new EditorSyncBridge();

        var prefabGuid = Guid.NewGuid();
        var v1Bytes = BuildTestPrefabPayload(new float3(0.0f, 0.0f, 0.0f), new float3(0.0f, 1.0f, 0.0f));
        var prefabV1 = CreateLoadedPrefabEntry(prefabGuid, "Meshes/Vehicle", v1Bytes);

        var spawnedOk = EditorPrefabUtility.TrySpawnLinkedPrefab(world, prefabV1, bridge, float3.zero, quaternion.identity, float3.one, out var spawned, new PrefabSpawnOptions { ApplyTransform = false, RootName = "HeroVehicle" });
        Assert.IsTrue(spawnedOk);
        Assert.HasCount(2, spawned);

        // Override only root position (leaving child local position un-overridden) and add a DirectionalLight to root
        var rootEcs = spawned[0].Entity;
        var childEcs = spawned[1].Entity;
        em.SetComponent(rootEcs, LocalTransform.FromPosition(new float3(5.0f, 0.0f, 0.0f)));
        em.SetComponent(rootEcs, new LocalToWorld { matrix = MathUtility.TRS(new float3(5.0f, 0.0f, 0.0f), quaternion.identity, float3.one) });

        var rootNode = new EntityNode(bridge.AllocateEntityId(), "HeroVehicle", Array.Empty<ComponentNode>())
        {
            EditorGuid = spawned[0].EditorGuid,
            EcsEntity = rootEcs,
            PrefabLink = spawned[0].Link
        };
        var rootLt = ComponentNodeFactory.CreateComponentNode(typeof(LocalTransform), rootNode, em.GetComponent<LocalTransform>(rootEcs), bridge);
        var rootLtw = ComponentNodeFactory.CreateComponentNode(typeof(LocalToWorld), rootNode, em.GetComponent<LocalToWorld>(rootEcs), bridge);
        var addedLight = ComponentNodeFactory.CreateComponentNode(typeof(DirectionalLight), rootNode, DirectionalLight.Default, bridge);
        addedLight.FindProperty(nameof(DirectionalLight.intensity))!.ApplyValue(2500.0f);
        ComponentReflectionAccessor.TryAddOrSet(em, rootEcs, addedLight);
        rootNode.Components = new[] { rootLt, rootLtw, addedLight };

        var childNode = new EntityNode(bridge.AllocateEntityId(), spawned[1].Name, Array.Empty<ComponentNode>(), rootNode.Id)
        {
            EditorGuid = spawned[1].EditorGuid,
            EcsEntity = childEcs,
            PrefabLink = spawned[1].Link
        };
        var childLt = ComponentNodeFactory.CreateComponentNode(typeof(LocalTransform), childNode, em.GetComponent<LocalTransform>(childEcs), bridge);
        var childLtw = ComponentNodeFactory.CreateComponentNode(typeof(LocalToWorld), childNode, em.GetComponent<LocalToWorld>(childEcs), bridge);
        childNode.Components = new[] { childLt, childLtw };

        var snapshot = new SceneSnapshot(new[] { rootNode, childNode });
        var doc = SceneSerializer.SaveToDocument(snapshot, bridge, new Dictionary<Guid, IAssetEntry> { [prefabGuid] = prefabV1 });

        Assert.IsEmpty(doc.Entities);
        Assert.HasCount(1, doc.PrefabInstances);
        var prefabRec = doc.PrefabInstances[0];
        Assert.AreEqual(prefabGuid, prefabRec.PrefabGuid);
        Assert.IsNotNull(prefabRec.NameOverrides);
        Assert.AreEqual("HeroVehicle", prefabRec.NameOverrides[0]);
        Assert.IsNotNull(prefabRec.PropertyOverrides);
        Assert.IsTrue(prefabRec.PropertyOverrides.Any(o => o.TargetGuid == rootNode.EditorGuid && o.Field == nameof(LocalTransform.position)));
        Assert.IsFalse(prefabRec.PropertyOverrides.Any(o => o.TargetGuid == childNode.EditorGuid && o.Component == typeof(LocalTransform).FullName));
        Assert.IsNotNull(prefabRec.AddedComponents);
        Assert.IsTrue(prefabRec.AddedComponents.Any(a => a.TargetGuid == rootNode.EditorGuid && a.Component == typeof(DirectionalLight).FullName));

        // Update source prefab on disk (child offset moved from y=1 -> y=99) and reload scene
        var v2Bytes = BuildTestPrefabPayload(new float3(0.0f, 0.0f, 0.0f), new float3(0.0f, 99.0f, 0.0f));
        var prefabV2 = CreateLoadedPrefabEntry(prefabGuid, "Meshes/Vehicle", v2Bytes);

        var loadRes = SceneSerializer.LoadFromDocument(world, bridge, doc, SceneLoadingType.Single, guid => guid == prefabGuid ? prefabV2 : null);
        Assert.IsTrue(loadRes.IsSuccess, loadRes.Message);

        var reloaded = bridge.LatestSnapshot!;
        Assert.HasCount(2, reloaded.Entities);
        var rRoot = reloaded.Entities.Single(e => e.PrefabLink?.TemplateEntityIndex == 0);
        var rChild = reloaded.Entities.Single(e => e.PrefabLink?.TemplateEntityIndex == 1);

        Assert.AreEqual("HeroVehicle", rRoot.Name);
        Assert.AreEqual(new float3(5.0f, 0.0f, 0.0f), em.GetComponent<LocalTransform>(rRoot.EcsEntity).position);
        Assert.AreEqual(new float3(0.0f, 99.0f, 0.0f), em.GetComponent<LocalTransform>(rChild.EcsEntity).position);
        Assert.IsTrue(em.HasComponent<DirectionalLight>(rRoot.EcsEntity));
        Assert.AreEqual(2500.0f, em.GetComponent<DirectionalLight>(rRoot.EcsEntity).intensity);
    }

    [TestMethod]
    public void SceneSerializer_AdditiveLoadPreservesExistingSceneEntities()
    {
        using var scheduler = new JobScheduler(new JobSchedulerDesc { ThreadCount = 1, ThreadPriority = ThreadPriority.Normal, DependencyChainCapacity = 64 });
        using var world = World.Create(scheduler, 64);
        var bridge = new EditorSyncBridge();

        var docA = new SceneAssetDocument
        {
            Entities = new List<SceneEntityRecord>
            {
                new() { Guid = Guid.NewGuid(), Name = "SceneA_Entity" }
            }
        };
        var docB = new SceneAssetDocument
        {
            Entities = new List<SceneEntityRecord>
            {
                new() { Guid = Guid.NewGuid(), Name = "SceneB_Entity" }
            }
        };

        Assert.IsTrue(SceneSerializer.LoadFromDocument(world, bridge, docA, SceneLoadingType.Single).IsSuccess);
        Assert.HasCount(1, bridge.LatestSnapshot!.Entities);

        Assert.IsTrue(SceneSerializer.LoadFromDocument(world, bridge, docB, SceneLoadingType.Additive).IsSuccess);
        Assert.HasCount(2, bridge.LatestSnapshot!.Entities);
        Assert.IsTrue(bridge.LatestSnapshot.Entities.Any(e => e.Name == "SceneA_Entity"));
        Assert.IsTrue(bridge.LatestSnapshot.Entities.Any(e => e.Name == "SceneB_Entity"));
    }
}
