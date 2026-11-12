using Ghost.Core;
using Ghost.Editor.Core.Commands;
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
        Console.WriteLine(" Running GhostEngine Studio Phase 1 Automated Headless Tests");
        Console.WriteLine("==================================================================");

        var passCount = 0;
        var totalCount = 5;

        // Test 1: EditorSyncBridge Snapshot Publishing & Mutation Enqueuing
        Console.Write("[1/5] Testing EditorSyncBridge snapshot publishing & mutation queue... ");
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
        var compNode = new ComponentNode("LocalTransform", typeof(LocalTransform), null!, [propNode]);
        var entityNode = new EntityNode(42, "PlayerCharacter", [compNode]);
        var snapshot = new SceneSnapshot([entityNode], frameTimeMs: 8.3f, fps: 120.0f);
        bridge.PublishSnapshot(snapshot);
        if (bridge.LatestSnapshot?.Entities.Count != 1 || bridge.LatestSnapshot.Fps != 120.0f)
        {
            throw new InvalidOperationException("Failed: SceneSnapshot was not published correctly.");
        }
        Console.WriteLine("PASS");
        passCount++;

        // Test 2: EditorSelection Polymorphic Selection
        Console.Write("[2/5] Testing EditorSelection polymorphic selection & events... ");
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
        Console.Write("[3/5] Testing CommandHistory undo, redo & compound commands... ");
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
        var compound = new CompoundCommand("Batch Edit", [cmdA, cmdB]);
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
        Console.Write("[4/5] Testing PropertyDrawerRegistry & ComponentDrawerRegistry resolution... ");
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
        Console.Write("[5/5] Testing StudioWorkspace DockManager hierarchy instantiation... ");
        var workspace = new StudioWorkspace();
        var dockManagerEl = workspace.Render();
        if (dockManagerEl is not Microsoft.UI.Reactor.Docking.DockManager dm || dm.Layout == null)
        {
            throw new InvalidOperationException("Failed: StudioWorkspace did not produce valid DockManager element.");
        }
        Console.WriteLine("PASS");
        passCount++;

        Console.WriteLine("------------------------------------------------------------------");
        Console.WriteLine($" ALL {passCount}/{totalCount} STUDIO HEADLESS TESTS PASSED CLEANLY!");
        Console.WriteLine("==================================================================");
        return 0;
    }
}
