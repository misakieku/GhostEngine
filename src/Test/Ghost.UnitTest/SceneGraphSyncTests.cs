using Ghost.Core;
using Ghost.Editor.Core.SceneGraph;
using Ghost.Editor.Core.Services;
using Ghost.Engine;
using Ghost.Engine.Components;
using Ghost.Engine.Core;
using Ghost.Entities;

namespace Ghost.UnitTest;

[TestClass]
[DoNotParallelize]
public class SceneGraphSyncTests
{
    private EditorWorldService _worldService = null!;
    private SceneGraphSyncService _syncService = null!;

    [TestInitialize]
    public void Setup()
    {
        _worldService = new EditorWorldService();
        _syncService = new SceneGraphSyncService(_worldService, null!, null!);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _syncService.Dispose();
        _worldService.Dispose();
    }

    [TestMethod]
    public void SceneGraph_InitialBuild_PopulatesTreeCorrectly()
    {
        var world = _worldService.EditorWorld;
        var scene = SceneManager.CreateScene();

        // Create parent and child
        var parent = world.EntityManager.CreateEntity();
        world.EntityManager.AddSharedComponent(parent, new SceneID { value = scene.ID });
        world.EntityManager.AddComponent(parent, Hierarchy.Root);

        var child = world.EntityManager.CreateEntity();
        world.EntityManager.AddSharedComponent(child, new SceneID { value = scene.ID });
        world.EntityManager.AddComponent(child, Hierarchy.Root);

        HierarchyUtility.SetParent(world, child, parent);

        var names = new Dictionary<Entity, string>
        {
            { parent, "ParentEntity" },
            { child, "ChildEntity" }
        };

        world.AdvanceVersion();
        
        _worldService.RebuildSceneGraph(names);
        _worldService.FlushCommands();
        _worldService.FirePendingEvents();

        Assert.AreEqual(1, _worldService.RootNodes.Count);
        var sceneNode = _worldService.RootNodes[0];
        Assert.AreEqual(scene.ID, sceneNode.Scene.ID);

        Assert.AreEqual(1, sceneNode.Children.Count);
        var parentNode = (EntityNode)sceneNode.Children[0];
        Assert.AreEqual("ParentEntity", parentNode.Name);
        Assert.AreEqual(parent, parentNode.Entity);

        Assert.AreEqual(1, parentNode.Children.Count);
        var childNode = (EntityNode)parentNode.Children[0];
        Assert.AreEqual("ChildEntity", childNode.Name);
        Assert.AreEqual(child, childNode.Entity);
    }

    [TestMethod]
    public void SceneGraph_CreateEntity_AppendsToRootAutomatically()
    {
        var scene = SceneManager.CreateScene();

        _worldService.CreateEntity("NewEntity", scene.ID);
        _worldService.FlushCommands();
        _worldService.FirePendingEvents();

        Assert.AreEqual(1, _worldService.RootNodes.Count);
        var sceneNode = _worldService.RootNodes[0];
        Assert.AreEqual(1, sceneNode.Children.Count);
        var entityNode = (EntityNode)sceneNode.Children[0];
        var entity = entityNode.Entity;
        Assert.AreEqual("NewEntity", entityNode.Name);
        Assert.AreEqual(entity, entityNode.Entity);
    }

    [TestMethod]
    public void SceneGraph_DestroyEntity_RemovesFromTreeAutomatically()
    {
        var scene = SceneManager.CreateScene();

        _worldService.CreateEntity("NewEntity", scene.ID);
        _worldService.FlushCommands();
        _worldService.FirePendingEvents();
        
        var sceneNode = _worldService.RootNodes[0];
        var entity = ((EntityNode)sceneNode.Children[0]).Entity;
        Assert.AreEqual(1, sceneNode.Children.Count);

        _worldService.DestroyEntity(entity);
        _worldService.FlushCommands();
        _worldService.FirePendingEvents();

        Assert.AreEqual(0, sceneNode.Children.Count);
    }

    [TestMethod]
    public void SceneGraph_SetParent_MovesNodeInTree()
    {
        var scene = SceneManager.CreateScene();

        _worldService.CreateEntity("Parent", scene.ID);
        _worldService.CreateEntity("Child", scene.ID);
        _worldService.FlushCommands();
        _worldService.FirePendingEvents();

        var sceneNode = _worldService.RootNodes[0];
        Assert.AreEqual(2, sceneNode.Children.Count);
        
        var parent = ((EntityNode)sceneNode.Children[0]).Entity;
        var child = ((EntityNode)sceneNode.Children[1]).Entity;

        var err = _worldService.SetParent(child, parent);
        _worldService.FlushCommands();
        _worldService.FirePendingEvents();
        
        Assert.AreEqual(Error.None, err);

        Assert.AreEqual(1, sceneNode.Children.Count);
        var parentNode = (EntityNode)sceneNode.Children[0];
        Assert.AreEqual("Parent", parentNode.Name);

        Assert.AreEqual(1, parentNode.Children.Count);
        var childNode = (EntityNode)parentNode.Children[0];
        Assert.AreEqual("Child", childNode.Name);
        Assert.AreEqual(child, childNode.Entity);
    }

    [TestMethod]
    public void SceneGraph_RenameEntity_UpdatesNodeNameInstantly()
    {
        var scene = SceneManager.CreateScene();

        _worldService.CreateEntity("OriginalName", scene.ID);
        _worldService.FlushCommands();
        _worldService.FirePendingEvents();
        
        var sceneNode = _worldService.RootNodes[0];
        var entityNode = (EntityNode)sceneNode.Children[0];
        var entity = entityNode.Entity;

        Assert.AreEqual("OriginalName", entityNode.Name);

        _worldService.RenameEntity(entity, "NewName");
        _worldService.FlushCommands();
        _worldService.FirePendingEvents();

        Assert.AreEqual("NewName", entityNode.Name);
    }
}
