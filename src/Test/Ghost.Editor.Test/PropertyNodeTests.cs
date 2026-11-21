using Ghost.Editor.Core.Commands;
using Ghost.Editor.Core.Sync;
using Ghost.Engine.Components;
using Ghost.Engine.Volumes;
using Ghost.Entities;
using Misaki.HighPerformance.Jobs;

namespace Ghost.Editor.Test;

[TestClass]
[DoNotParallelize]
public class PropertyNodeTests
{
    [TestMethod]
    public void ComponentNodeFactory_CreatesPolymorphicNodes_AndPreservesAuthoringStateOnRefresh()
    {
        using var scheduler = new JobScheduler(new JobSchedulerDesc { ThreadCount = 1, ThreadPriority = ThreadPriority.Normal, DependencyChainCapacity = 64 });
        using var world = World.Create(scheduler, 16);
        var targetEcs = world.EntityManager.CreateEntity();

        var bridge = new EditorSyncBridge();
        var history = new CommandHistory();

        var meshEntity = new EntityNode(1, "Mesh", Array.Empty<ComponentNode>());
        var targetEntity = new EntityNode(2, "Target", Array.Empty<ComponentNode>())
        {
            EcsEntity = targetEcs
        };
        bridge.PublishSnapshot(new SceneSnapshot(new[] { meshEntity, targetEntity }, 16.6f, 60.0f));

        var comp = ComponentNodeFactory.CreateComponentNode(typeof(MeshInstance), meshEntity, new MeshInstance(), bridge);
        meshEntity.Components = new[] { comp };

        var meshProp = comp.Properties.OfType<AssetPropertyNode>().Single(p => p.Name == nameof(MeshInstance.mesh));
        var paletteProp = comp.Properties.OfType<MaterialPalettePropertyNode>().Single(p => p.Name == nameof(MeshInstance.materialPalette));

        var meshGuid = Guid.NewGuid();
        history.Execute(new ModifyPropertyCommand(meshProp, meshProp.AssetGuid, meshGuid));
        Assert.AreEqual(meshGuid, meshProp.AssetGuid);
        Assert.AreEqual(meshGuid, meshProp.GetSerializableValue());

        meshProp.RefreshFromEcs(new MeshInstance(), typeof(MeshInstance).GetField(nameof(MeshInstance.mesh)));
        Assert.AreEqual(meshGuid, meshProp.AssetGuid);

        history.Undo();
        Assert.AreEqual(Guid.Empty, meshProp.AssetGuid);
        history.Redo();
        Assert.AreEqual(meshGuid, meshProp.AssetGuid);

        var matGuid1 = Guid.NewGuid();
        var matGuid2 = Guid.NewGuid();
        history.Execute(new ModifyPropertyCommand(paletteProp, paletteProp.MaterialGuids.ToArray(), new[] { matGuid1, matGuid2 }));
        CollectionAssert.AreEqual(new[] { matGuid1, matGuid2 }, (Guid[]?)paletteProp.GetSerializableValue());

        var hierComp = ComponentNodeFactory.CreateComponentNode(typeof(Hierarchy), meshEntity, Hierarchy.Root, bridge);
        var parentProp = hierComp.Properties.OfType<EntityRefPropertyNode>().Single(p => p.Name == nameof(Hierarchy.parent));
        parentProp.ApplyValue(targetEntity.Id);
        Assert.AreEqual(targetEntity.EditorGuid, parentProp.TargetEditorGuid);
        Assert.AreEqual(targetEntity.EcsEntity, parentProp.Value);

        var cloned = ComponentNodeFactory.CloneEntityNode(meshEntity, 99, "Mesh_Copy", null, bridge);
        var clonedMeshProp = cloned.Components[0].Properties.OfType<AssetPropertyNode>().Single(p => p.Name == nameof(MeshInstance.mesh));
        Assert.AreEqual(meshGuid, clonedMeshProp.AssetGuid);
    }

    [TestMethod]
    public void VolumeOverridesPropertyNode_SupportsAddEditUndoRedoAndContainerSync()
    {
        using var scheduler = new JobScheduler(new JobSchedulerDesc { ThreadCount = 1, ThreadPriority = ThreadPriority.Normal, DependencyChainCapacity = 64 });
        using var world = World.Create(scheduler, 16);
        var em = world.EntityManager;

        var discovered = VolumeTypeRegistry.DiscoverVolumeTypes();
        Assert.IsTrue(discovered.Any(a => a.SettingsType == typeof(BloomSettings)));

        var container = RenderVolumeDataContainer.Create();
        var ecsEntity = em.CreateEntity(RenderVolume.DefaultGlobal, container);

        var bridge = new EditorSyncBridge();
        var history = new CommandHistory();
        var entityNode = new EntityNode(1, "Global Volume", Array.Empty<ComponentNode>())
        {
            EcsEntity = ecsEntity
        };

        var volCompNode = ComponentNodeFactory.CreateVolumeComponentNode(entityNode, in em.GetComponent<RenderVolumeDataContainer>(ecsEntity), bridge);
        entityNode.Components = new List<ComponentNode> { volCompNode };
        var volumeProp = (VolumeOverridesPropertyNode)volCompNode.Properties[0];

        history.Execute(new AddVolumeOverrideCommand(volumeProp, typeof(BloomSettings)));
        Assert.HasCount(1, volumeProp.Overrides);
        var bloomEntry = volumeProp.Overrides[0];
        Assert.IsTrue(bloomEntry.IsExpanded);
        Assert.IsFalse(bloomEntry.FindField(nameof(BloomSettings.intensity))!.IsOverridden);

        bridge.DrainWorldActions(world);
        Assert.IsTrue(em.GetComponent<RenderVolumeDataContainer>(ecsEntity).TryGet<BloomSettings>(out var liveBloom));
        Assert.IsFalse(liveBloom.intensity.HasValue);

        history.Execute(new ModifyVolumeFieldCommand(volumeProp, typeof(BloomSettings), nameof(BloomSettings.intensity), false, BloomSettings.Default.intensity, true, 2.5f));
        bridge.DrainWorldActions(world);
        Assert.IsTrue(em.GetComponent<RenderVolumeDataContainer>(ecsEntity).TryGet<BloomSettings>(out liveBloom));
        Assert.AreEqual(2.5f, liveBloom.intensity);

        history.Undo();
        bridge.DrainWorldActions(world);
        Assert.IsTrue(em.GetComponent<RenderVolumeDataContainer>(ecsEntity).TryGet<BloomSettings>(out liveBloom));
        Assert.IsFalse(liveBloom.intensity.HasValue);

        history.Redo();
        bridge.DrainWorldActions(world);
        Assert.IsTrue(em.GetComponent<RenderVolumeDataContainer>(ecsEntity).TryGet<BloomSettings>(out liveBloom));
        Assert.AreEqual(2.5f, liveBloom.intensity);

        history.Execute(new RemoveComponentCommand(entityNode, volCompNode, bridge));
        bridge.DrainWorldActions(world);
        Assert.IsFalse(em.HasComponent<RenderVolumeDataContainer>(ecsEntity));

        history.Undo();
        bridge.DrainWorldActions(world);
        Assert.IsTrue(em.HasComponent<RenderVolumeDataContainer>(ecsEntity));
        Assert.IsTrue(em.GetComponent<RenderVolumeDataContainer>(ecsEntity).TryGet<BloomSettings>(out var restoredBloom));
        Assert.AreEqual(2.5f, restoredBloom.intensity);

        em.GetComponent<RenderVolumeDataContainer>(ecsEntity).Dispose();
    }
}
