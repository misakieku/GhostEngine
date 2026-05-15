using Ghost.Editor.Core.SceneGraph;
using Ghost.Engine;
using Ghost.Engine.Components;
using Ghost.Engine.Core;
using Ghost.Entities;
using Misaki.HighPerformance.LowLevel.Buffer;

namespace Ghost.UnitTest;

[TestClass]
[DoNotParallelize]
public class SceneGraphBuilderTests
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

    private Entity CreateEntityWithScene(Scene scene)
    {
        var entity = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(entity, new SceneID { value = scene.ID });
        return entity;
    }

    private Entity CreateEntityWithSceneAndHierarchy(Scene scene, Entity parent)
    {
        var entity = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(entity, new SceneID { value = scene.ID });
        _world.EntityManager.AddComponent(entity, new Hierarchy
        {
            parent = Entity.Invalid,
            firstChild = Entity.Invalid,
            nextSibling = Entity.Invalid
        });

        if (parent.IsValid)
        {
            HierarchyUtility.SetParent(_world, entity, parent);
        }

        return entity;
    }

    [TestMethod]
    public void Build_EmptyWorldReturnsEmptyList()
    {
        var nodes = SceneGraphBuilder.Build(_world);

        Assert.AreEqual(0, nodes.Count);
    }

    [TestMethod]
    public void Build_OneSceneOneEntity_CreatesSceneNodeWithEntityChild()
    {
        var scene = SceneManager.CreateScene();
        CreateEntityWithScene(scene);

        var nodes = SceneGraphBuilder.Build(_world);

        Assert.AreEqual(1, nodes.Count);

        var sceneNode = nodes[0];
        Assert.AreEqual(scene, sceneNode.Scene);
        Assert.AreEqual(1, sceneNode.Children.Count);
        Assert.IsInstanceOfType<EntityNode>(sceneNode.Children[0]);
    }

    [TestMethod]
    public void Build_MultipleScenes_CreatesMultipleSceneNodes()
    {
        var scene1 = SceneManager.CreateScene();
        var scene2 = SceneManager.CreateScene();
        CreateEntityWithScene(scene1);
        CreateEntityWithScene(scene2);

        var nodes = SceneGraphBuilder.Build(_world);

        Assert.AreEqual(2, nodes.Count);
    }

    [TestMethod]
    public void Build_HierarchicalEntities_CreatesNestedEntityNodes()
    {
        var scene = SceneManager.CreateScene();
        var rootEntity = CreateEntityWithSceneAndHierarchy(scene, Entity.Invalid);
        var childEntity = CreateEntityWithSceneAndHierarchy(scene, rootEntity);

        var nodes = SceneGraphBuilder.Build(_world);

        Assert.AreEqual(1, nodes.Count);
        var sceneNode = nodes[0];
        Assert.AreEqual(1, sceneNode.Children.Count);

        var rootNode = (EntityNode)sceneNode.Children[0];
        Assert.AreEqual(rootEntity, rootNode.Entity);
        Assert.AreEqual(1, rootNode.Children.Count);

        var childNode = (EntityNode)rootNode.Children[0];
        Assert.AreEqual(childEntity, childNode.Entity);
    }

    [TestMethod]
    public void Build_EntitiesWithoutHierarchy_AreFlatChildren()
    {
        var scene = SceneManager.CreateScene();
        CreateEntityWithScene(scene);
        CreateEntityWithScene(scene);

        var nodes = SceneGraphBuilder.Build(_world);

        Assert.AreEqual(1, nodes.Count);
        var sceneNode = nodes[0];
        Assert.AreEqual(2, sceneNode.Children.Count);
    }

    [TestMethod]
    public void Build_SiblingOrder_PreservesChildOrder()
    {
        var scene = SceneManager.CreateScene();
        var parent = CreateEntityWithSceneAndHierarchy(scene, Entity.Invalid);
        var child1 = CreateEntityWithSceneAndHierarchy(scene, parent);
        var child2 = CreateEntityWithSceneAndHierarchy(scene, parent);

        var nodes = SceneGraphBuilder.Build(_world);

        var rootNode = (EntityNode)nodes[0].Children[0];
        Assert.AreEqual(2, rootNode.Children.Count);
        Assert.AreEqual(child2, ((EntityNode)rootNode.Children[0]).Entity);
        Assert.AreEqual(child1, ((EntityNode)rootNode.Children[1]).Entity);
    }

    [TestMethod]
    public void Build_DeepHierarchy_BuildsFullTree()
    {
        var scene = SceneManager.CreateScene();
        var level1 = CreateEntityWithSceneAndHierarchy(scene, Entity.Invalid);
        var level2 = CreateEntityWithSceneAndHierarchy(scene, level1);
        var level3 = CreateEntityWithSceneAndHierarchy(scene, level2);

        var nodes = SceneGraphBuilder.Build(_world);

        var n1 = (EntityNode)nodes[0].Children[0];
        Assert.AreEqual(1, n1.Children.Count);

        var n2 = (EntityNode)n1.Children[0];
        Assert.AreEqual(1, n2.Children.Count);

        var n3 = (EntityNode)n2.Children[0];
        Assert.AreEqual(level3, n3.Entity);
    }

    [TestMethod]
    public void Build_InvalidSceneEntitiesAreExcluded()
    {
        var entity = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(entity, new SceneID { value = Scene.INVALID_ID });

        var nodes = SceneGraphBuilder.Build(_world);

        Assert.AreEqual(0, nodes.Count);
    }
}
