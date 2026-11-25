using Ghost.Editor.Core.Commands;
using Ghost.Editor.Core.Menus;
using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Sync;
using Ghost.Engine.Components;
using Ghost.Entities;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Editor.Test;

[TestClass]
[DoNotParallelize]
public class EditorCoreWorkflowTests
{
    [TestMethod]
    public void EditorSyncBridge_ManagesEntitiesAndMainThreadActions()
    {
        using var scheduler = new JobScheduler(new JobSchedulerDesc { ThreadCount = 1, ThreadPriority = ThreadPriority.Normal, DependencyChainCapacity = 64 });
        using var world = World.Create(scheduler, 16);
        var entity = world.EntityManager.CreateEntity(LocalTransform.Identity);

        var bridge = new EditorSyncBridge { World = world };
        var propNode = new ValuePropertyNode("position", typeof(float3), null!, new float3(1, 2, 3), _ => { });
        var compNode = new ComponentNode("LocalTransform", typeof(LocalTransform), null!, new PropertyNode[] { propNode });
        var entityNode = new EntityNode(42, "PlayerCharacter", new ComponentNode[] { compNode }) { EcsEntity = entity };
        bridge.SetEntities(new[] { entityNode });

        Assert.AreEqual(1, bridge.EntityCount);
        Assert.AreSame(entityNode, bridge.FindEntityNode(42));

        var executed = false;
        bridge.EnqueueMainThreadAction(w =>
        {
            Assert.AreSame(world, w);
            executed = true;
        });
        bridge.DrainMainThreadActions(world);
        Assert.IsTrue(executed);
    }

    [TestMethod]
    public void EditorSelection_InstanceAndIsolated_ManageSelectionAndEvents()
    {
        var entityNode = new EntityNode(42, "PlayerCharacter", Array.Empty<ComponentNode>());
        var selection = EditorSelection.Instance;
        selection.Clear();
        var selectionFired = false;
        Action handler = () => selectionFired = true;
        selection.SelectionChanged += handler;

        try
        {
            selection.Select(entityNode);
            Assert.IsTrue(selectionFired);
            Assert.AreSame(entityNode, selection.ActiveEntity);
            Assert.HasCount(1, selection.SelectedObjects);

            selectionFired = false;
            selection.Clear();
            Assert.IsTrue(selectionFired);
            Assert.IsNull(selection.ActiveObject);
            Assert.IsEmpty(selection.SelectedObjects);
        }
        finally
        {
            selection.SelectionChanged -= handler;
            selection.Clear();
        }

        var isolated = new EditorSelection();
        var ent1 = new EntityNode(1, "Entity1", new List<ComponentNode>());
        var ent2 = new EntityNode(2, "Entity2", new List<ComponentNode>());
        isolated.Select(ent1);
        isolated.ToggleSelection(ent2);
        Assert.HasCount(2, isolated.SelectedObjects);
        Assert.Contains(ent1, isolated.SelectedObjects);
        Assert.Contains(ent2, isolated.SelectedObjects);

        isolated.Clear();
        Assert.IsEmpty(isolated.SelectedObjects);
    }

    [TestMethod]
    public void CommandHistory_ExecutesUndoesRedoesCompoundAndPropertyCallbacks()
    {
        var history = new CommandHistory(50);
        var value = 10;
        var testProp = new ValuePropertyNode("testVal", typeof(int), null!, value, v => value = (int)v!);

        history.Execute(new ModifyPropertyCommand(testProp, 10, 20));
        Assert.AreEqual(20, value);
        Assert.IsTrue(history.CanUndo);

        history.Undo();
        Assert.AreEqual(10, value);
        Assert.IsTrue(history.CanRedo);

        history.Redo();
        Assert.AreEqual(20, value);

        var cmdA = new ModifyPropertyCommand(testProp, 20, 30);
        var cmdB = new ModifyPropertyCommand(testProp, 30, 40);
        history.Execute(new CompoundCommand("Batch Edit", new IEditorCommand[] { cmdA, cmdB }));
        Assert.AreEqual(40, value);

        history.Undo();
        Assert.AreEqual(20, value);

        testProp.OnChange(100);
        Assert.AreEqual(100, value);
    }

    [TestMethod]
    public void EntityAndComponentCommands_SupportCreateReparentRenameComponentEditAndDeleteUndo()
    {
        var bridge = new EditorSyncBridge();
        var selection = new EditorSelection();
        var history = new CommandHistory();

        var createRoot = new CreateEntityCommand("ParentObj", null, new Type[] { typeof(LocalTransform) }, bridge, selection);
        history.Execute(createRoot);
        var parentObj = createRoot.CreatedEntity;

        var createChild = new CreateEntityCommand("ChildObj", parentObj.Id, new Type[] { typeof(LocalTransform) }, bridge, selection);
        history.Execute(createChild);
        var childObj = createChild.CreatedEntity;

        Assert.AreEqual(2, bridge.EntityCount);
        Assert.AreEqual(parentObj.Id, childObj.ParentId);

        // Reparenting parentObj under childObj must be blocked to prevent hierarchy cycles.
        history.Execute(new ReparentEntityCommand(parentObj, childObj.Id, bridge, selection));
        Assert.IsNull(parentObj.ParentId);

        history.Execute(new ReparentEntityCommand(childObj, null, bridge, selection));
        Assert.IsNull(childObj.ParentId);
        history.Undo();
        Assert.AreEqual(parentObj.Id, childObj.ParentId);

        EntityNode? renamedTarget = null;
        Action<EntityNode> onRenameRequested = e => renamedTarget = e;
        selection.RenameRequested += onRenameRequested;
        selection.RequestRename(parentObj);
        selection.RenameRequested -= onRenameRequested;
        Assert.AreSame(parentObj, renamedTarget);

        history.Execute(new RenameEntityCommand(parentObj, "  HeroRoot  ", bridge, selection));
        Assert.AreEqual("HeroRoot", parentObj.Name);
        history.Undo();
        Assert.AreEqual("ParentObj", parentObj.Name);
        history.Redo();
        Assert.AreEqual("HeroRoot", parentObj.Name);

        history.Execute(new AddComponentCommand(parentObj, typeof(Camera), bridge, selection));
        var addedCamera = parentObj.Components.FirstOrDefault(c => c.ComponentType == typeof(Camera));
        Assert.IsNotNull(addedCamera);

        history.Execute(new RemoveComponentCommand(parentObj, addedCamera, bridge, selection));
        Assert.IsFalse(parentObj.Components.Any(c => c.ComponentType == typeof(Camera)));
        history.Undo();
        Assert.IsTrue(parentObj.Components.Any(c => c.ComponentType == typeof(Camera)));

        history.Execute(new DeleteEntityCommand(parentObj, bridge, selection));
        Assert.IsEmpty(bridge.Entities);
        history.Undo();
        Assert.HasCount(2, bridge.Entities);
    }

    [TestMethod]
    public void EditorMenuRegistry_DiscoversMenusAndAppliesTargetFilters()
    {
        var bridge = new EditorSyncBridge();
        var selection = new EditorSelection();
        var history = new CommandHistory();

        var createRoot = new CreateEntityCommand("ParentObj", null, new Type[] { typeof(LocalTransform) }, bridge, selection);
        history.Execute(createRoot);
        var parentObj = createRoot.CreatedEntity;

        var createChild = new CreateEntityCommand("ChildObj", parentObj.Id, new Type[] { typeof(LocalTransform) }, bridge, selection);
        history.Execute(createChild);
        var childObj = createChild.CreatedEntity;

        var emptyCtx = new MenuContext(null, bridge, history, new EditorSelection());
        var rootMenu = EditorMenuRegistry.Default.BuildMenu("Hierarchy", emptyCtx);
        var entityTopMenu = EditorMenuRegistry.Default.BuildMenu("Entity", emptyCtx);
        Assert.IsNotEmpty(rootMenu);
        Assert.IsNotEmpty(entityTopMenu);

        var createSubMenu = rootMenu.OfType<MenuSubMenuNode>().FirstOrDefault(n => n.Label == "Create");
        Assert.IsNotNull(createSubMenu);
        Assert.IsTrue(createSubMenu.Children.OfType<MenuActionNode>().Any(a => a.Label == "Camera"));
        Assert.IsTrue(createSubMenu.Children.OfType<MenuSubMenuNode>().Any(s => s.Label == "Light"));

        Assert.IsNull(rootMenu.OfType<MenuActionNode>().FirstOrDefault(a => a.Label == "Delete"));
        Assert.IsNull(rootMenu.OfType<MenuActionNode>().FirstOrDefault(a => a.Label == "Unparent"));

        var childCtx = new MenuContext(childObj, bridge, history, new EditorSelection());
        var childMenu = EditorMenuRegistry.Default.BuildMenu("Hierarchy", childCtx);
        var renameChild = childMenu.OfType<MenuActionNode>().FirstOrDefault(a => a.Label == "Rename");
        var deleteChild = childMenu.OfType<MenuActionNode>().FirstOrDefault(a => a.Label == "Delete");
        var unparentChild = childMenu.OfType<MenuActionNode>().FirstOrDefault(a => a.Label == "Unparent");
        Assert.IsNotNull(renameChild);
        Assert.IsTrue(renameChild.IsEnabled);
        Assert.IsNotNull(deleteChild);
        Assert.IsTrue(deleteChild.IsEnabled);
        Assert.IsNotNull(unparentChild);
        Assert.IsTrue(unparentChild.IsEnabled);
        Assert.IsTrue(childMenu.OfType<MenuSeparatorNode>().Any());

        var parentCtx = new MenuContext(parentObj, bridge, history, new EditorSelection());
        var parentMenu = EditorMenuRegistry.Default.BuildMenu("Hierarchy", parentCtx);
        Assert.IsNull(parentMenu.OfType<MenuActionNode>().FirstOrDefault(a => a.Label == "Unparent"));
    }

    [TestMethod]
    public void ComponentNodeFactory_DiscoversComponentsAndExcludesHideEditorTypes()
    {
        var bridge = new EditorSyncBridge();
        var parentObj = new EntityNode(1, "CameraHolder", Array.Empty<ComponentNode>());

        var discoveredTypes = ComponentNodeFactory.DiscoverComponentTypes();
        Assert.IsTrue(discoveredTypes.Contains(typeof(LocalTransform)));
        Assert.IsTrue(discoveredTypes.Contains(typeof(Camera)));
        Assert.IsTrue(discoveredTypes.Contains(typeof(MeshInstance)));
        Assert.IsTrue(discoveredTypes.Contains(typeof(DirectionalLight)));
        Assert.IsTrue(discoveredTypes.Contains(typeof(PunctualLight)));
        Assert.IsFalse(discoveredTypes.Contains(typeof(Hierarchy)));

        var camNode = ComponentNodeFactory.CreateComponentNode(typeof(Camera), parentObj, new Camera(), bridge);
        Assert.IsNotEmpty(camNode.Properties);
    }

    [TestMethod]
    public void SceneNode_SharesHierarchyNodeBaseAndSelectsInEditorSelection()
    {
        var bridge = new EditorSyncBridge();
        bridge.RegisterScene(0, @"C:\Proj\Assets\Main.gscene");
        bridge.RegisterScene(1, @"C:\Proj\Assets\Lighting.gscene");
        bridge.HasUnsavedSceneChanges = true;
        bridge.ActiveSceneId = 0;

        var entityA = new EntityNode(1, "PropA", Array.Empty<ComponentNode>()) { SceneId = 0 };
        var entityB = new EntityNode(2, "PropB", Array.Empty<ComponentNode>()) { SceneId = 1 };
        var entityC = new EntityNode(3, "PropC", Array.Empty<ComponentNode>()) { SceneId = 1 };

        Assert.IsInstanceOfType<HierarchyNode>(entityA);
        Assert.IsTrue(entityA.CanRename);

        var scenes = bridge.GetSceneNodes(new[] { entityA, entityB, entityC });
        Assert.HasCount(2, scenes);
        Assert.IsInstanceOfType<HierarchyNode>(scenes[0]);
        Assert.IsFalse(scenes[0].CanRename);
        Assert.AreEqual("Main", scenes[0].Name);
        Assert.IsTrue(scenes[0].IsDirty);
        Assert.AreEqual(1, scenes[0].SceneEntityCount);
        Assert.AreEqual("Lighting", scenes[1].Name);
        Assert.IsFalse(scenes[1].IsDirty);
        Assert.AreEqual(2, scenes[1].SceneEntityCount);

        var selection = new EditorSelection();
        selection.Select(scenes[0]);
        Assert.AreSame(scenes[0], selection.ActiveObject);
        Assert.IsNull(selection.ActiveEntity);
    }

    [TestMethod]
    public void ModifyPropertyAndRemoveComponent_UndoRedo_UpdatesEcsAndMarksDirtyTracker()
    {
        using var scheduler = new JobScheduler(new JobSchedulerDesc { ThreadCount = 1, ThreadPriority = ThreadPriority.Normal, DependencyChainCapacity = 64 });
        using var world = World.Create(scheduler, 16);
        var em = world.EntityManager;
        var entity = em.CreateEntity(LocalTransform.Identity, new LocalToWorld { matrix = float4x4.identity });

        var bridge = new EditorSyncBridge { World = world };
        var selection = new EditorSelection();
        var history = new CommandHistory(50);

        var placeholderNode = new EntityNode(0, "Placeholder", Array.Empty<ComponentNode>());
        var ltComp = ComponentNodeFactory.CreateComponentNode(typeof(LocalTransform), placeholderNode, LocalTransform.Identity, bridge);
        var realEntityNode = new EntityNode(10, "Cube", new List<ComponentNode> { ltComp }) { EcsEntity = entity };
        ltComp.EntityNode = realEntityNode;
        bridge.SetEntities(new[] { realEntityNode });

        var posProp = ltComp.FindProperty(nameof(LocalTransform.position))!;
        var dirtyTransforms = new List<Entity>();

        history.Execute(new ModifyPropertyCommand(posProp, float3.zero, new float3(5.0f, 2.0f, -1.0f)));
        bridge.DirtyTracker.DrainTransforms(dirtyTransforms);
        Assert.Contains(entity, dirtyTransforms);
        Assert.AreEqual(new float3(5.0f, 2.0f, -1.0f), em.GetComponent<LocalTransform>(entity).position);

        dirtyTransforms.Clear();
        history.Undo();
        bridge.DirtyTracker.DrainTransforms(dirtyTransforms);
        Assert.Contains(entity, dirtyTransforms);
        Assert.AreEqual(float3.zero, em.GetComponent<LocalTransform>(entity).position);

        dirtyTransforms.Clear();
        history.Redo();
        bridge.DirtyTracker.DrainTransforms(dirtyTransforms);
        Assert.Contains(entity, dirtyTransforms);
        Assert.AreEqual(new float3(5.0f, 2.0f, -1.0f), em.GetComponent<LocalTransform>(entity).position);

        dirtyTransforms.Clear();
        history.Execute(new RemoveComponentCommand(realEntityNode, ltComp, bridge, selection));
        Assert.IsFalse(em.HasComponent<LocalTransform>(entity));

        history.Undo();
        bridge.DirtyTracker.DrainTransforms(dirtyTransforms);
        Assert.IsTrue(em.HasComponent<LocalTransform>(entity));
        Assert.Contains(entity, dirtyTransforms);
    }
}
