using Ghost.Editor.Core.Commands;
using Ghost.Editor.Core.Sync;
using Ghost.Engine.Components;
using Ghost.Entities;
using Misaki.HighPerformance.Jobs;

namespace Ghost.Editor.Test;

[TestClass]
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
}
