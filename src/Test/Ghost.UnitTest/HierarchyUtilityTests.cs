using Ghost.Core;
using Ghost.Engine;
using Ghost.Engine.Components;
using Ghost.Entities;

namespace Ghost.UnitTest;

[TestClass]
[DoNotParallelize]
public class HierarchyUtilityTests
{
    private World _world = null!;

    [TestInitialize]
    public void Setup()
    {
        _world = World.Create(entityCapacity: 64);
    }

    [TestCleanup]
    public void Cleanup()
    {
        World.Destroy(_world.ID);
    }

    private Entity CreateHierarchyEntity()
    {
        var entity = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(entity, new Hierarchy
        {
            parent = Entity.Invalid,
            firstChild = Entity.Invalid,
            nextSibling = Entity.Invalid
        });
        return entity;
    }

    [TestMethod]
    public void SetParent_ChildBecomesChildOfParent()
    {
        var parent = CreateHierarchyEntity();
        var child = CreateHierarchyEntity();

        var result = HierarchyUtility.SetParent(_world, child, parent);

        Assert.AreEqual(Error.None, result);

        ref var childHierarchy = ref _world.EntityManager.GetComponent<Hierarchy>(child);
        Assert.AreEqual(parent, childHierarchy.parent);

        ref var parentHierarchy = ref _world.EntityManager.GetComponent<Hierarchy>(parent);
        Assert.AreEqual(child, parentHierarchy.firstChild);
    }

    [TestMethod]
    public void SetParent_SecondChildBecomesSibling()
    {
        var parent = CreateHierarchyEntity();
        var child1 = CreateHierarchyEntity();
        var child2 = CreateHierarchyEntity();

        HierarchyUtility.SetParent(_world, child1, parent);
        HierarchyUtility.SetParent(_world, child2, parent);

        ref var parentHierarchy = ref _world.EntityManager.GetComponent<Hierarchy>(parent);
        Assert.AreEqual(child2, parentHierarchy.firstChild);

        ref var child2Hierarchy = ref _world.EntityManager.GetComponent<Hierarchy>(child2);
        Assert.AreEqual(child1, child2Hierarchy.nextSibling);

        ref var child1Hierarchy = ref _world.EntityManager.GetComponent<Hierarchy>(child1);
        Assert.AreEqual(Entity.Invalid, child1Hierarchy.nextSibling);
    }

    [TestMethod]
    public void SetParent_ReparentFromOneParentToAnother()
    {
        var parent1 = CreateHierarchyEntity();
        var parent2 = CreateHierarchyEntity();
        var child = CreateHierarchyEntity();

        HierarchyUtility.SetParent(_world, child, parent1);
        HierarchyUtility.SetParent(_world, child, parent2);

        ref var childHierarchy = ref _world.EntityManager.GetComponent<Hierarchy>(child);
        Assert.AreEqual(parent2, childHierarchy.parent);

        ref var parent1Hierarchy = ref _world.EntityManager.GetComponent<Hierarchy>(parent1);
        Assert.AreEqual(Entity.Invalid, parent1Hierarchy.firstChild);

        ref var parent2Hierarchy = ref _world.EntityManager.GetComponent<Hierarchy>(parent2);
        Assert.AreEqual(child, parent2Hierarchy.firstChild);
    }

    [TestMethod]
    public void SetParent_SelfParentingIsRejected()
    {
        var entity = CreateHierarchyEntity();

        var result = HierarchyUtility.SetParent(_world, entity, entity);

        Assert.AreEqual(Error.InvalidArgument, result);
    }

    [TestMethod]
    public void SetParent_CycleIsRejected()
    {
        var parent = CreateHierarchyEntity();
        var child = CreateHierarchyEntity();

        HierarchyUtility.SetParent(_world, child, parent);

        var result = HierarchyUtility.SetParent(_world, parent, child);

        Assert.AreEqual(Error.InvalidArgument, result);
        Assert.AreEqual(parent, _world.EntityManager.GetComponent<Hierarchy>(child).parent);
    }

    [TestMethod]
    public void SetParent_GrandchildCycleIsRejected()
    {
        var grandparent = CreateHierarchyEntity();
        var parent = CreateHierarchyEntity();
        var child = CreateHierarchyEntity();

        HierarchyUtility.SetParent(_world, parent, grandparent);
        HierarchyUtility.SetParent(_world, child, parent);

        var result = HierarchyUtility.SetParent(_world, grandparent, child);

        Assert.AreEqual(Error.InvalidArgument, result);
    }

    [TestMethod]
    public void SetParent_EntityWithoutHierarchyComponentReturnsNotFound()
    {
        var parent = CreateHierarchyEntity();
        var child = _world.EntityManager.CreateEntity();

        var result = HierarchyUtility.SetParent(_world, child, parent);

        Assert.AreEqual(Error.NotFound, result);
    }

    [TestMethod]
    public void RemoveParent_UnlinksChildFromParent()
    {
        var parent = CreateHierarchyEntity();
        var child = CreateHierarchyEntity();

        HierarchyUtility.SetParent(_world, child, parent);
        var result = HierarchyUtility.RemoveParent(_world, child);

        Assert.AreEqual(Error.None, result);

        ref var childHierarchy = ref _world.EntityManager.GetComponent<Hierarchy>(child);
        Assert.AreEqual(Entity.Invalid, childHierarchy.parent);
        Assert.AreEqual(Entity.Invalid, childHierarchy.nextSibling);

        ref var parentHierarchy = ref _world.EntityManager.GetComponent<Hierarchy>(parent);
        Assert.AreEqual(Entity.Invalid, parentHierarchy.firstChild);
    }

    [TestMethod]
    public void RemoveParent_MiddleChildMaintainsSiblingChain()
    {
        var parent = CreateHierarchyEntity();
        var child1 = CreateHierarchyEntity();
        var child2 = CreateHierarchyEntity();
        var child3 = CreateHierarchyEntity();

        HierarchyUtility.SetParent(_world, child1, parent);
        HierarchyUtility.SetParent(_world, child2, parent);
        HierarchyUtility.SetParent(_world, child3, parent);

        HierarchyUtility.RemoveParent(_world, child2);

        ref var child3Hierarchy = ref _world.EntityManager.GetComponent<Hierarchy>(child3);
        Assert.AreEqual(child1, child3Hierarchy.nextSibling);

        ref var child2Hierarchy = ref _world.EntityManager.GetComponent<Hierarchy>(child2);
        Assert.AreEqual(Entity.Invalid, child2Hierarchy.parent);
        Assert.AreEqual(Entity.Invalid, child2Hierarchy.nextSibling);
    }

    [TestMethod]
    public void RemoveParent_WhenNoParentReturnsNone()
    {
        var entity = CreateHierarchyEntity();

        var result = HierarchyUtility.RemoveParent(_world, entity);

        Assert.AreEqual(Error.None, result);
    }

    [TestMethod]
    public void IsAncestor_ReturnsTrueForDirectParent()
    {
        var parent = CreateHierarchyEntity();
        var child = CreateHierarchyEntity();

        HierarchyUtility.SetParent(_world, child, parent);

        Assert.IsTrue(HierarchyUtility.IsAncestor(_world, child, parent));
    }

    [TestMethod]
    public void IsAncestor_ReturnsTrueForGrandparent()
    {
        var grandparent = CreateHierarchyEntity();
        var parent = CreateHierarchyEntity();
        var child = CreateHierarchyEntity();

        HierarchyUtility.SetParent(_world, parent, grandparent);
        HierarchyUtility.SetParent(_world, child, parent);

        Assert.IsTrue(HierarchyUtility.IsAncestor(_world, child, grandparent));
    }

    [TestMethod]
    public void IsAncestor_ReturnsFalseForUnrelatedEntity()
    {
        var entity1 = CreateHierarchyEntity();
        var entity2 = CreateHierarchyEntity();

        Assert.IsFalse(HierarchyUtility.IsAncestor(_world, entity1, entity2));
    }

    [TestMethod]
    public void IsAncestor_ReturnsFalseForChild()
    {
        var parent = CreateHierarchyEntity();
        var child = CreateHierarchyEntity();

        HierarchyUtility.SetParent(_world, child, parent);

        Assert.IsFalse(HierarchyUtility.IsAncestor(_world, parent, child));
    }

    [TestMethod]
    public void DestroyEntityWithChildren_CascadeDestroysAllChildren()
    {
        var parent = CreateHierarchyEntity();
        var child1 = CreateHierarchyEntity();
        var child2 = CreateHierarchyEntity();
        var grandchild = CreateHierarchyEntity();

        HierarchyUtility.SetParent(_world, child1, parent);
        HierarchyUtility.SetParent(_world, child2, parent);
        HierarchyUtility.SetParent(_world, grandchild, child1);

        HierarchyUtility.DestroyEntityWithChildren(_world, parent);

        Assert.IsFalse(_world.EntityManager.Exists(parent));
        Assert.IsFalse(_world.EntityManager.Exists(child1));
        Assert.IsFalse(_world.EntityManager.Exists(child2));
        Assert.IsFalse(_world.EntityManager.Exists(grandchild));
    }

    [TestMethod]
    public void DestroyEntityWithChildren_DoesNotAffectUnrelatedEntities()
    {
        var parent = CreateHierarchyEntity();
        var child = CreateHierarchyEntity();
        var unrelated = CreateHierarchyEntity();

        HierarchyUtility.SetParent(_world, child, parent);
        HierarchyUtility.DestroyEntityWithChildren(_world, parent);

        Assert.IsTrue(_world.EntityManager.Exists(unrelated));
    }

    [TestMethod]
    public void RemoveEntity_DestroysSingleEntity()
    {
        var entity = CreateHierarchyEntity();

        var result = HierarchyUtility.RemoveEntity(_world, entity);

        Assert.AreEqual(Error.None, result);
        Assert.IsFalse(_world.EntityManager.Exists(entity));
    }
}
