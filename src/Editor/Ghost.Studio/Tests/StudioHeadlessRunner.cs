using Ghost.Core;
using Ghost.Editor.Core.Commands;
using Ghost.Editor.Core.Menus;
using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Sync;
using Ghost.Engine.Components;
using Ghost.Studio.Drawers;
using Ghost.Studio.Workspace;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Studio.Tests;

public static class StudioHeadlessRunner
{
    public static int Run()
    {
        Console.WriteLine("==================================================================");
        Console.WriteLine(" Running GhostEngine Studio Phase 1 & 2 Automated Headless Tests");
        Console.WriteLine("==================================================================");

        var passCount = 0;
        var totalCount = 8;

        // Test 1: EditorSyncBridge Snapshot Publishing & Mutation Enqueuing
        Console.Write("[1/8] Testing EditorSyncBridge snapshot publishing & mutation queue... ");
        var bridge = new EditorSyncBridge();
        var entity = new Ghost.Entities.Entity(42, 1);
        bridge.EnqueueMutation(entity, typeof(LocalTransform), "position", new float3(1, 2, 3));
        var mutations = new List<PendingPropertyMutation>();
        bridge.DrainMutations(mutations);
        if (mutations.Count != 1 || !mutations[0].Entity.Equals(entity))
        {
            throw new InvalidOperationException("Failed: Mutation was not queued or drained correctly.");
        }

        var propNode = new PropertyNode("position", typeof(float3), null!, new float3(1, 2, 3), _ => {});
        var compNode = new ComponentNode("LocalTransform", typeof(LocalTransform), null!, new PropertyNode[] { propNode });
        var entityNode = new EntityNode(42, "PlayerCharacter", new ComponentNode[] { compNode });
        var snapshot = new SceneSnapshot(new EntityNode[] { entityNode }, frameTimeMs: 8.3f, fps: 120.0f);
        bridge.PublishSnapshot(snapshot);
        if (bridge.LatestSnapshot?.Entities.Count != 1 || bridge.LatestSnapshot.Fps != 120.0f)
        {
            throw new InvalidOperationException("Failed: SceneSnapshot was not published correctly.");
        }
        Console.WriteLine("PASS");
        passCount++;

        // Test 2: EditorSelection Polymorphic Selection
        Console.Write("[2/8] Testing EditorSelection polymorphic selection & events... ");
        var selection = EditorSelection.Instance;
        selection.Clear();
        var selectionFired = false;
        Action handler = () => selectionFired = true;
        selection.SelectionChanged += handler;

        selection.Select(entityNode);
        if (!selectionFired || selection.ActiveEntity != entityNode || selection.SelectedObjects.Count != 1)
        {
            throw new InvalidOperationException("Failed: Entity selection did not fire or state incorrect.");
        }

        selectionFired = false;
        selection.Clear();
        if (!selectionFired || selection.ActiveObject != null || selection.SelectedObjects.Count != 0)
        {
            throw new InvalidOperationException("Failed: Clear selection failed.");
        }
        selection.SelectionChanged -= handler;
        Console.WriteLine("PASS");
        passCount++;

        // Test 3: CommandHistory Undo/Redo & Compound Commands
        Console.Write("[3/8] Testing CommandHistory undo, redo & compound commands... ");
        var history = new CommandHistory(50);
        var value = 10;
        var testProp = new PropertyNode("testVal", typeof(int), null!, value, v => value = (int)v!);

        var cmd1 = new ModifyPropertyCommand(testProp, 10, 20);
        history.Execute(cmd1);
        if (value != 20 || !history.CanUndo)
        {
            throw new InvalidOperationException("Failed: Execute command failed.");
        }

        history.Undo();
        if (value != 10 || !history.CanRedo)
        {
            throw new InvalidOperationException("Failed: Undo failed.");
        }

        history.Redo();
        if (value != 20)
        {
            throw new InvalidOperationException("Failed: Redo failed.");
        }

        // Compound command
        var cmdA = new ModifyPropertyCommand(testProp, 20, 30);
        var cmdB = new ModifyPropertyCommand(testProp, 30, 40);
        var compound = new CompoundCommand("Batch Edit", new IEditorCommand[] { cmdA, cmdB });
        history.Execute(compound);
        if (value != 40)
        {
            throw new InvalidOperationException("Failed: Compound command execution failed.");
        }

        history.Undo();
        if (value != 20)
        {
            throw new InvalidOperationException("Failed: Compound command undo failed.");
        }
        Console.WriteLine("PASS");
        passCount++;

        // Test 4: Extensible Drawer Registry & DrawerContext
        Console.Write("[4/8] Testing PropertyDrawerRegistry & ComponentDrawerRegistry resolution... ");
        var propRegistry = PropertyDrawerRegistry.Default;
        var float3Drawer = propRegistry.GetDrawer(typeof(float3));
        if (float3Drawer is not Float3PropertyDrawer)
        {
            throw new InvalidOperationException("Failed: float3 drawer resolution failed.");
        }

        var compRegistry = ComponentDrawerRegistry.Default;
        var transformDrawer = compRegistry.GetDrawer(typeof(LocalTransform));
        if (transformDrawer is not TransformComponentDrawer)
        {
            throw new InvalidOperationException("Failed: LocalTransform drawer resolution failed.");
        }

        var drawerContext = new DrawerContext(history, propRegistry);
        drawerContext.ModifyProperty(testProp, 20, 100);
        if (value != 100)
        {
            throw new InvalidOperationException("Failed: DrawerContext.ModifyProperty execution failed.");
        }
        Console.WriteLine("PASS");
        passCount++;

        // Test 5: StudioWorkspace DockManager Layout Verification
        Console.Write("[5/8] Testing StudioWorkspace DockManager hierarchy instantiation... ");
        var workspace = new StudioWorkspace();
        var dockManagerEl = workspace.Render();
        if (dockManagerEl is not Microsoft.UI.Reactor.Docking.DockManager dm || dm.Layout == null)
        {
            throw new InvalidOperationException("Failed: StudioWorkspace did not produce valid DockManager element.");
        }
        Console.WriteLine("PASS");
        passCount++;

        // Test 6: Entity & Component Commands (Create, Delete Subtree, Reparent Cycle Guard, Add/Remove Component + Undo)
        Console.Write("[6/8] Testing Entity & Component Commands with Undo/Redo... ");
        var cmdBridge = new EditorSyncBridge();
        var cmdSelection = new EditorSelection();
        var cmdHistory = new CommandHistory();
        cmdBridge.PublishSnapshot(new SceneSnapshot(Array.Empty<EntityNode>(), 16.6f, 60.0f));

        var createRoot = new CreateEntityCommand("ParentObj", null, new Type[] { typeof(LocalTransform) }, cmdBridge, cmdSelection);
        cmdHistory.Execute(createRoot);
        var parentObj = createRoot.CreatedEntity;

        var createChild = new CreateEntityCommand("ChildObj", parentObj.Id, new Type[] { typeof(LocalTransform) }, cmdBridge, cmdSelection);
        cmdHistory.Execute(createChild);
        var childObj = createChild.CreatedEntity;

        if (cmdBridge.LatestSnapshot?.Entities.Count != 2 || childObj.ParentId != parentObj.Id)
        {
            throw new InvalidOperationException("Failed: CreateEntityCommand did not add parent and child correctly.");
        }

        // Cycle prevention: reparenting parentObj under childObj must be ignored / blocked
        var cycleCmd = new ReparentEntityCommand(parentObj, childObj.Id, cmdBridge, cmdSelection);
        cmdHistory.Execute(cycleCmd);
        if (parentObj.ParentId != null)
        {
            throw new InvalidOperationException("Failed: ReparentEntityCommand allowed a parent-child cycle.");
        }

        // Valid reparent to root (null) & undo
        cmdHistory.Execute(new ReparentEntityCommand(childObj, null, cmdBridge, cmdSelection));
        if (childObj.ParentId != null)
        {
            throw new InvalidOperationException("Failed: ReparentEntityCommand failed to unparent childObj.");
        }
        cmdHistory.Undo();
        if (childObj.ParentId != parentObj.Id)
        {
            throw new InvalidOperationException("Failed: Undo of ReparentEntityCommand did not restore ParentId.");
        }

        // Add & Remove Component + Undo
        var addCompCmd = new AddComponentCommand(parentObj, typeof(Camera), cmdBridge, cmdSelection);
        cmdHistory.Execute(addCompCmd);
        var addedCamera = parentObj.Components.FirstOrDefault(c => c.ComponentType == typeof(Camera));
        if (addedCamera == null)
        {
            throw new InvalidOperationException("Failed: AddComponentCommand did not attach Camera component.");
        }

        cmdHistory.Execute(new RemoveComponentCommand(parentObj, addedCamera, cmdBridge, cmdSelection));
        if (parentObj.Components.Any(c => c.ComponentType == typeof(Camera)))
        {
            throw new InvalidOperationException("Failed: RemoveComponentCommand did not remove Camera component.");
        }
        cmdHistory.Undo();
        if (!parentObj.Components.Any(c => c.ComponentType == typeof(Camera)))
        {
            throw new InvalidOperationException("Failed: Undo of RemoveComponentCommand did not restore Camera component.");
        }

        // Delete parent deletes parent + child subtree; Undo restores both
        cmdHistory.Execute(new DeleteEntityCommand(parentObj, cmdBridge, cmdSelection));
        if (cmdBridge.LatestSnapshot?.Entities.Count != 0)
        {
            throw new InvalidOperationException("Failed: DeleteEntityCommand did not remove entire subtree.");
        }
        cmdHistory.Undo();
        if (cmdBridge.LatestSnapshot?.Entities.Count != 2)
        {
            throw new InvalidOperationException("Failed: Undo of DeleteEntityCommand did not restore subtree.");
        }
        Console.WriteLine("PASS");
        passCount++;

        // Test 7: Dynamic Reflection Menu Registry & Validation
        Console.Write("[7/8] Testing EditorMenuRegistry reflection discovery & validation... ");
        var emptyCtx = new MenuContext(null, cmdBridge, cmdHistory, new EditorSelection());
        var rootMenu = EditorMenuRegistry.Default.BuildMenu("Hierarchy", emptyCtx);
        var entityTopMenu = EditorMenuRegistry.Default.BuildMenu("Entity", emptyCtx);
        if (rootMenu.Count == 0 || entityTopMenu.Count == 0)
        {
            throw new InvalidOperationException("Failed: EditorMenuRegistry returned empty menu for Hierarchy or Entity.");
        }

        var createSubMenu = rootMenu.OfType<MenuSubMenuNode>().FirstOrDefault(n => n.Label == "Create");
        if (createSubMenu == null || !createSubMenu.Children.OfType<MenuActionNode>().Any(a => a.Label == "Camera") || !createSubMenu.Children.OfType<MenuSubMenuNode>().Any(s => s.Label == "Light"))
        {
            throw new InvalidOperationException("Failed: Create submenu missing EngineEntityMenuActions presets (Camera / Light).");
        }

        var deleteNoTarget = rootMenu.OfType<MenuActionNode>().FirstOrDefault(a => a.Label == "Delete");
        var unparentNoTarget = rootMenu.OfType<MenuActionNode>().FirstOrDefault(a => a.Label == "Unparent");
        if (deleteNoTarget != null || unparentNoTarget != null)
        {
            throw new InvalidOperationException("Failed: Delete/Unparent should be filtered out when TargetEntity is null.");
        }

        var childCtx = new MenuContext(childObj, cmdBridge, cmdHistory, new EditorSelection());
        var childMenu = EditorMenuRegistry.Default.BuildMenu("Hierarchy", childCtx);
        var deleteChild = childMenu.OfType<MenuActionNode>().FirstOrDefault(a => a.Label == "Delete");
        var unparentChild = childMenu.OfType<MenuActionNode>().FirstOrDefault(a => a.Label == "Unparent");
        if (deleteChild == null || !deleteChild.IsEnabled || unparentChild == null || !unparentChild.IsEnabled)
        {
            throw new InvalidOperationException("Failed: Delete/Unparent should be visible and enabled for child entity.");
        }
        if (!childMenu.OfType<MenuSeparatorNode>().Any())
        {
            throw new InvalidOperationException("Failed: MenuSeparatorNode missing between priority groups in entity menu.");
        }

        var parentCtx = new MenuContext(parentObj, cmdBridge, cmdHistory, new EditorSelection());
        var parentMenu = EditorMenuRegistry.Default.BuildMenu("Hierarchy", parentCtx);
        var unparentRoot = parentMenu.OfType<MenuActionNode>().FirstOrDefault(a => a.Label == "Unparent");
        if (unparentRoot != null)
        {
            throw new InvalidOperationException("Failed: Unparent should be filtered out for root entity with ParentId == null.");
        }
        Console.WriteLine("PASS");
        passCount++;

        // Test 8: ComponentNodeFactory Discovery & Phase 2 Drawers
        Console.Write("[8/8] Testing ComponentNodeFactory discovery & Phase 2 drawers... ");
        var discoveredTypes = ComponentNodeFactory.DiscoverComponentTypes();
        if (!discoveredTypes.Contains(typeof(LocalTransform)) || !discoveredTypes.Contains(typeof(Camera)) || !discoveredTypes.Contains(typeof(MeshInstance)) || !discoveredTypes.Contains(typeof(DirectionalLight)) || !discoveredTypes.Contains(typeof(PunctualLight)))
        {
            throw new InvalidOperationException("Failed: ComponentNodeFactory missing expected engine components.");
        }
        if (discoveredTypes.Contains(typeof(Hierarchy)))
        {
            throw new InvalidOperationException("Failed: ComponentNodeFactory should exclude [HideEditor] Hierarchy component.");
        }

        if (compRegistry.GetDrawer(typeof(Camera)) is not CameraComponentDrawer || compRegistry.GetDrawer(typeof(MeshInstance)) is not MeshInstanceDrawer)
        {
            throw new InvalidOperationException("Failed: CameraComponentDrawer or MeshInstanceDrawer resolution failed.");
        }
        var gateFitType = typeof(Camera).GetField(nameof(Camera.gateFit))!.FieldType;
        if (propRegistry.GetDrawer(typeof(float2)) is not Float2PropertyDrawer || propRegistry.GetDrawer(typeof(quaternion)) is not QuaternionPropertyDrawer)
        {
            throw new InvalidOperationException("Failed: Float2 or Quaternion PropertyDrawer resolution failed.");
        }
        if (propRegistry.GetDrawer(typeof(uint)) is not UIntPropertyDrawer || propRegistry.GetDrawer(gateFitType) is not EnumPropertyDrawer)
        {
            throw new InvalidOperationException("Failed: UInt or Enum PropertyDrawer resolution failed.");
        }
        Console.WriteLine("PASS");
        passCount++;

        Console.WriteLine("------------------------------------------------------------------");
        Console.WriteLine($" ALL {passCount}/{totalCount} STUDIO HEADLESS TESTS PASSED CLEANLY!");
        Console.WriteLine("==================================================================");
        return 0;
    }
}
