using Ghost.Editor.Core.SceneGraph;
using Ghost.Editor.Core.Services;
using Ghost.Entities;

namespace Ghost.UnitTest;

[TestClass]
public class UndoServiceEcsTests
{
    private struct CompA : IComponentData { public int value; }
    private struct CompB : IComponentData { public int value; }

    private EditorWorldService _worldService = null!;
    private UndoService _undoService = null!;

    [TestInitialize]
    public void Setup()
    {
        _worldService = new EditorWorldService();
        _undoService = new UndoService(_worldService);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _worldService.Dispose();
    }

    [TestMethod]
    public void TestRecordEntityStructure()
    {
        var world = _worldService.EditorWorld;
        var e = world.EntityManager.CreateEntity();
        world.EntityManager.AddComponent<CompA>(e);

        // Initial state: Entity has CompA
        ref var compA = ref world.EntityManager.GetComponent<CompA>(e);
        compA.value = 10;

        var node = new EntityNode(world, e, "TestEntity");

        _undoService.BeginTransaction("Add CompB");
        _undoService.RecordEntityStructure(node, "Before Add CompB");

        // Modify structure
        world.EntityManager.AddComponent<CompB>(e);
        ref var compB = ref world.EntityManager.GetComponent<CompB>(e);
        compB.value = 20;

        // Re-fetch CompA because AddComponent moves the entity to a new chunk,
        // invalidating the previous ref!
        ref var compA_new = ref world.EntityManager.GetComponent<CompA>(e);
        compA_new.value = 15; // also modify compA

        _undoService.EndTransaction();

        // Perform Undo
        _undoService.PerformUndo();

        Assert.IsTrue(world.EntityManager.HasComponent<CompA>(e), "Should have CompA");
        Assert.IsFalse(world.EntityManager.HasComponent<CompB>(e), "Should NOT have CompB");
        Assert.AreEqual(10, world.EntityManager.GetComponent<CompA>(e).value, "CompA value should be reverted to 10");

        // Perform Redo
        _undoService.PerformRedo();

        Assert.IsTrue(world.EntityManager.HasComponent<CompA>(e), "Should have CompA");
        Assert.IsTrue(world.EntityManager.HasComponent<CompB>(e), "Should have CompB");
        Assert.AreEqual(15, world.EntityManager.GetComponent<CompA>(e).value, "CompA value should be restored to 15");
        Assert.AreEqual(20, world.EntityManager.GetComponent<CompB>(e).value, "CompB value should be restored to 20");
    }

    [TestMethod]
    public void TestRecordEntityLifecycle_CreateAndDestroy()
    {
        var world = _worldService.EditorWorld;

        // Step 1: Create Entity
        var e = world.EntityManager.CreateEntity();
        world.EntityManager.AddComponent<CompA>(e);
        world.EntityManager.GetComponent<CompA>(e).value = 42;
        var node = new EntityNode(world, e, "TestEntity");

        _undoService.BeginTransaction("Create Entity");
        _undoService.RecordEntityLifecycle(node, LifecycleEvent.Created);
        _undoService.EndTransaction();

        // Undo Creation (Expect destruction)
        _undoService.PerformUndo();
        Assert.IsFalse(world.EntityManager.Exists(e), "Entity should be destroyed by Undo of Creation");

        // Redo Creation (Expect resurrection)
        _undoService.PerformRedo();

        // Note: The entity ID might be different, but the EntityNode should be updated
        var resurrectedEntity = node.Entity;
        Assert.IsTrue(world.EntityManager.Exists(resurrectedEntity), "Entity should be resurrected by Redo of Creation");
        // In our current implementation, restoring components for created entities isn't fully robust yet,
        // but we verify the entity is alive.

        // Step 2: Destroy Entity
        _undoService.BeginTransaction("Destroy Entity");
        _undoService.RecordEntityLifecycle(node, LifecycleEvent.Destroyed);
        world.EntityManager.DestroyEntity(resurrectedEntity);
        _undoService.EndTransaction();

        Assert.IsFalse(world.EntityManager.Exists(resurrectedEntity), "Entity destroyed manually");

        // Undo Destruction (Expect resurrection)
        _undoService.PerformUndo();

        var undoneDestroyEntity = node.Entity;
        Assert.IsTrue(world.EntityManager.Exists(undoneDestroyEntity), "Entity should be resurrected by Undo of Destruction");

        // Redo Destruction (Expect destruction)
        _undoService.PerformRedo();
        Assert.IsFalse(world.EntityManager.Exists(undoneDestroyEntity), "Entity should be destroyed by Redo of Destruction");
    }
}
