using Ghost.AssetForge.Core.Bakers;
using Ghost.AssetForge.Core.Models;
using Ghost.AssetForge.Core.Services;
using Ghost.Core;
using Ghost.Editor.Core.Commands;
using Ghost.Editor.Core.Menus;
using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Serialization;
using Ghost.Editor.Core.Sync;
using Ghost.Engine;
using Ghost.Engine.Components;
using Ghost.Entities;
using Ghost.Studio.Assets;
using Ghost.Studio.Backend;
using Ghost.Studio.Rendering;
using Ghost.Studio.Services;
using Ghost.Studio.UI;
using Hexa.NET.ImGui;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.Mathematics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Ghost.Studio.Tests;

public static class StudioHeadlessRunner
{
    public static int Run()
    {
        Console.WriteLine("==================================================================");
        Console.WriteLine(" Running GhostEngine Studio Phase 1 & 2 Automated Headless Tests");
        Console.WriteLine("==================================================================");

        var passCount = 0;
        var totalCount = 14;

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

        var propNode = new ValuePropertyNode("position", typeof(float3), null!, new float3(1, 2, 3), _ => {});
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
        var testProp = new ValuePropertyNode("testVal", typeof(int), null!, value, v => value = (int)v!);

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

        // Test 4: Property mutation callback & undo stack integration
        Console.Write("[4/8] Testing PropertyNode mutation with CommandHistory... ");
        testProp.OnChange(100);
        if (value != 100)
        {
            throw new InvalidOperationException("Failed: PropertyNode OnChange did not update backing value.");
        }
        Console.WriteLine("PASS");
        passCount++;

        // Test 5: EditorSelection clear & multi-selection
        Console.Write("[5/8] Testing EditorSelection single & multi-select... ");
        var sel = new EditorSelection();
        var ent1 = new EntityNode(1, "Entity1", new List<ComponentNode>());
        var ent2 = new EntityNode(2, "Entity2", new List<ComponentNode>());
        sel.Select(ent1);
        sel.ToggleSelection(ent2);
        if (sel.SelectedObjects.Count != 2 || !sel.SelectedObjects.Contains(ent1) || !sel.SelectedObjects.Contains(ent2))
        {
            throw new InvalidOperationException("Failed: EditorSelection multi-select state mismatch.");
        }
        sel.Clear();
        if (sel.SelectedObjects.Count != 0)
        {
            throw new InvalidOperationException("Failed: EditorSelection Clear failed.");
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

        // Rename Entity + Undo/Redo + RequestRename event
        EntityNode? renamedTarget = null;
        Action<EntityNode> onRenameRequested = e => renamedTarget = e;
        cmdSelection.RenameRequested += onRenameRequested;
        cmdSelection.RequestRename(parentObj);
        cmdSelection.RenameRequested -= onRenameRequested;
        if (!ReferenceEquals(renamedTarget, parentObj))
        {
            throw new InvalidOperationException("Failed: EditorSelection.RequestRename did not fire RenameRequested.");
        }

        cmdHistory.Execute(new RenameEntityCommand(parentObj, "  HeroRoot  ", cmdBridge, cmdSelection));
        if (parentObj.Name != "HeroRoot")
        {
            throw new InvalidOperationException("Failed: RenameEntityCommand did not update entity name.");
        }
        cmdHistory.Undo();
        if (parentObj.Name != "ParentObj")
        {
            throw new InvalidOperationException("Failed: Undo of RenameEntityCommand did not restore entity name.");
        }
        cmdHistory.Redo();
        if (parentObj.Name != "HeroRoot")
        {
            throw new InvalidOperationException("Failed: Redo of RenameEntityCommand did not reapply entity name.");
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
        var renameChild = childMenu.OfType<MenuActionNode>().FirstOrDefault(a => a.Label == "Rename");
        var deleteChild = childMenu.OfType<MenuActionNode>().FirstOrDefault(a => a.Label == "Delete");
        var unparentChild = childMenu.OfType<MenuActionNode>().FirstOrDefault(a => a.Label == "Unparent");
        if (renameChild == null || !renameChild.IsEnabled || deleteChild == null || !deleteChild.IsEnabled || unparentChild == null || !unparentChild.IsEnabled)
        {
            throw new InvalidOperationException("Failed: Rename/Delete/Unparent should be visible and enabled for child entity.");
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

        // Test 8: ComponentNodeFactory Discovery & PropertyNode reflection
        Console.Write("[8/8] Testing ComponentNodeFactory discovery & HideEditor filtering... ");
        var discoveredTypes = ComponentNodeFactory.DiscoverComponentTypes();
        if (!discoveredTypes.Contains(typeof(LocalTransform)) || !discoveredTypes.Contains(typeof(Camera)) || !discoveredTypes.Contains(typeof(MeshInstance)) || !discoveredTypes.Contains(typeof(DirectionalLight)) || !discoveredTypes.Contains(typeof(PunctualLight)))
        {
            throw new InvalidOperationException("Failed: ComponentNodeFactory missing expected engine components.");
        }
        if (discoveredTypes.Contains(typeof(Hierarchy)))
        {
            throw new InvalidOperationException("Failed: ComponentNodeFactory should exclude [HideEditor] Hierarchy component.");
        }
        var camNode = ComponentNodeFactory.CreateComponentNode(typeof(Camera), parentObj, new Camera(), cmdBridge);
        if (camNode.Properties.Count == 0)
        {
            throw new InvalidOperationException("Failed: Camera component node has no discovered properties.");
        }
        Console.WriteLine("PASS");
        passCount++;

        // Test 9: EditorContentProvider & EditorAssetCatalog layered resolution
        Console.Write("[9/12] Testing EditorContentProvider & EditorAssetCatalog project cache resolution... ");
        TestEditorContentProviderAndCatalog();
        Console.WriteLine("PASS");
        passCount++;

        // Test 10: Polymorphic PropertyNode hierarchy, serialization values, & undo/redo
        Console.Write("[10/12] Testing polymorphic PropertyNode hierarchy & authoring GUID preservation... ");
        TestPolymorphicPropertyNodes();
        Console.WriteLine("PASS");
        passCount++;

        // Test 11: EditorWindowManager multi-tab & double-click routing
        Console.Write("[11/12] Testing EditorWindowManager multi-tab dispatch & no-op unregistered files... ");
        TestEditorWindowManager();
        Console.WriteLine("PASS");
        passCount++;

        // Test 12: MaterialEditorWindow load, shader semantics, save & rebake
        Console.Write("[12/13] Testing MaterialEditorWindow .gmat load, semantics & save... ");
        TestMaterialEditorWindow();
        Console.WriteLine("PASS");
        passCount++;

        // Test 13: StudioDockspace default layout docks all built-in windows on first launch
        Console.Write("[13/14] Testing StudioDockspace default layout initial docking... ");
        TestDefaultDockspaceLayout();
        Console.WriteLine("PASS");
        passCount++;

        // Test 14: EditorSceneSession create, save, open, DefaultScene boot & UnpackPrefabCommand
        Console.Write("[14/14] Testing EditorSceneSession save/open, DefaultScene & UnpackPrefabCommand... ");
        TestSceneSessionAndPrefabUnpack();
        Console.WriteLine("PASS");
        passCount++;

        Console.WriteLine("------------------------------------------------------------------");
        Console.WriteLine($" ALL {passCount}/{totalCount} STUDIO HEADLESS TESTS PASSED CLEANLY!");
        Console.WriteLine("==================================================================");
        return 0;
    }

    private static void TestEditorContentProviderAndCatalog()
    {
        AllocationManager.Initialize();
        var tempRoot = Path.Combine(Path.GetTempPath(), $"GhostStudioTest_{Guid.NewGuid():N}");
        try
        {
            using var bakerRegistry = new BakerRegistry();
            var projectService = new ProjectService(bakerRegistry);
            projectService.CreateProject(tempRoot, "TestProj");

            var matDir = Path.Combine(projectService.AssetDirectories[0], "Materials");
            Directory.CreateDirectory(matDir);
            var gmatPath = Path.Combine(matDir, "Brick.gmat");
            File.WriteAllText(gmatPath, """{ "Version": 1, "Properties": { "roughness": 0.35 } }""");

            var meta = projectService.GetOrCreateMetadata(gmatPath) ?? throw new InvalidOperationException("Failed to create .gmat metadata.");
            projectService.SaveMetadata(gmatPath + ".meta", meta);

            var bakeService = new BakeService(projectService.GetContext(), bakerRegistry);
            var bakeResult = bakeService.BakeProjectAsync(new BakeConfig { ForceRebake = true }).GetAwaiter().GetResult();
            if (bakeResult.Failed != 0 || bakeResult.Succeeded == 0)
            {
                throw new InvalidOperationException($"BakeProjectAsync failed: Succeeded={bakeResult.Succeeded}, Failed={bakeResult.Failed}");
            }

            var builtInManifest = Path.Combine(AppContext.BaseDirectory, "BuiltInAssets", "manifest.json");
            using var provider = new EditorContentProvider(projectService, builtInManifest);
            if (!provider.HasAsset(meta.Id) || provider.GetAssetType(meta.Id) != AssetType.Material)
            {
                throw new InvalidOperationException("EditorContentProvider did not resolve baked project .gmat.");
            }

            if (provider.VirtualPathToGuid("Materials/Brick") != meta.Id || provider.VirtualPathFromGuid(meta.Id) != "Materials/Brick")
            {
                throw new InvalidOperationException("EditorContentProvider virtual path mapping mismatch.");
            }

            var openRes = provider.OpenReadAsync(meta.Id);
            if (openRes.IsFailure || !openRes.Value.IsValid)
            {
                throw new InvalidOperationException($"EditorContentProvider.OpenReadAsync failed: {openRes.Message}");
            }

            using (var payload = openRes.Value)
            {
                var header = payload.Read<MaterialContentHeader>(0);
                if (header.magic != MaterialContentHeader.MAGIC || header.version != MaterialContentHeader.VERSION)
                {
                    throw new InvalidOperationException($"Invalid MaterialContentHeader: magic=0x{header.magic:X8}, version={header.version}");
                }
            }

            var cubeMatGuid = provider.VirtualPathToGuid("Meshes/cube#Materials/Material_0");
            var cubeMatOpen = provider.OpenReadAsync(cubeMatGuid);
            if (cubeMatOpen.IsFailure || !cubeMatOpen.Value.IsValid)
            {
                throw new InvalidOperationException("Failed to open built-in Meshes/cube#Materials/Material_0 payload.");
            }
            using (var cubeMatPayload = cubeMatOpen.Value)
            {
                var cubeHeader = cubeMatPayload.Read<MaterialContentHeader>(0);
                if (cubeHeader.magic != MaterialContentHeader.MAGIC || cubeHeader.version != MaterialContentHeader.VERSION)
                {
                    throw new InvalidOperationException($"Built-in Meshes/cube#Materials/Material_0 has stale version {cubeHeader.version} (expected {MaterialContentHeader.VERSION}).");
                }
            }

            var catalog = new EditorAssetCatalog(projectService, provider);
            if (!catalog.TryGetByGuid(meta.Id, out var catEntry) || catEntry.AssetType != AssetType.Material || catEntry.IsBuiltIn)
            {
                throw new InvalidOperationException("EditorAssetCatalog failed to index project .gmat.");
            }

            var shaders = new List<EditorAssetCatalog.Entry>();
            catalog.GetAssets(AssetType.Shader, shaders);
            if (shaders.Count == 0)
            {
                throw new InvalidOperationException("EditorAssetCatalog did not index built-in shaders from manifest.");
            }
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }

            AllocationManager.Dispose();
        }
    }

    private static void TestPolymorphicPropertyNodes()
    {
        var bridge = new EditorSyncBridge();
        var history = new CommandHistory();

        var meshEntity = new EntityNode(10, "MeshObj", Array.Empty<ComponentNode>());
        var targetEntity = new EntityNode(11, "TargetObj", Array.Empty<ComponentNode>())
        {
            EcsEntity = new Ghost.Entities.Entity(99, 1)
        };
        bridge.PublishSnapshot(new SceneSnapshot(new[] { meshEntity, targetEntity }, 16.6f, 60.0f));

        var meshComp = ComponentNodeFactory.CreateComponentNode(typeof(MeshInstance), meshEntity, new MeshInstance(), bridge);
        meshEntity.Components = new[] { meshComp };

        var meshProp = meshComp.Properties.OfType<AssetPropertyNode>().FirstOrDefault(p => p.Name == nameof(MeshInstance.mesh))
            ?? throw new InvalidOperationException("MeshInstance.mesh did not produce AssetPropertyNode.");
        var paletteProp = meshComp.Properties.OfType<MaterialPalettePropertyNode>().FirstOrDefault(p => p.Name == nameof(MeshInstance.materialPalette))
            ?? throw new InvalidOperationException("MeshInstance.materialPalette did not produce MaterialPalettePropertyNode.");

        var guidA = Guid.NewGuid();
        var guidB = Guid.NewGuid();
        history.Execute(new ModifyPropertyCommand(meshProp, meshProp.AssetGuid, guidA));
        if (meshProp.AssetGuid != guidA || !Equals(meshProp.GetSerializableValue(), guidA))
        {
            throw new InvalidOperationException("AssetPropertyNode failed to store authoring Guid.");
        }

        meshProp.RefreshFromEcs(new MeshInstance(), typeof(MeshInstance).GetField(nameof(MeshInstance.mesh)));
        if (meshProp.AssetGuid != guidA)
        {
            throw new InvalidOperationException("AssetPropertyNode authoring Guid was clobbered by RefreshFromEcs.");
        }

        history.Undo();
        if (meshProp.AssetGuid != Guid.Empty)
        {
            throw new InvalidOperationException("AssetPropertyNode undo failed.");
        }
        history.Redo();

        history.Execute(new ModifyPropertyCommand(paletteProp, paletteProp.MaterialGuids.ToArray(), new[] { guidA, guidB }));
        if (paletteProp.MaterialGuids.Count != 2 || paletteProp.MaterialGuids[1] != guidB)
        {
            throw new InvalidOperationException("MaterialPalettePropertyNode failed to store material slot GUIDs.");
        }

        var camEntity = new EntityNode(12, "HierarchyEntity", Array.Empty<ComponentNode>());
        var hierComp = ComponentNodeFactory.CreateComponentNode(typeof(Hierarchy), camEntity, Hierarchy.Root, bridge);
        var parentProp = hierComp.Properties.OfType<EntityRefPropertyNode>().FirstOrDefault(p => p.Name == nameof(Hierarchy.parent))
            ?? throw new InvalidOperationException("Hierarchy.parent did not produce EntityRefPropertyNode.");
        parentProp.ApplyValue(targetEntity.Id);
        if (parentProp.TargetEditorGuid != targetEntity.EditorGuid || !Equals(parentProp.Value, targetEntity.EcsEntity))
        {
            throw new InvalidOperationException("EntityRefPropertyNode failed to resolve target entity.");
        }

        var cloned = ComponentNodeFactory.CloneEntityNode(meshEntity, 20, "MeshObj_Copy", null, bridge);
        var clonedMeshProp = cloned.Components[0].Properties.OfType<AssetPropertyNode>().First();
        if (clonedMeshProp.AssetGuid != guidA)
        {
            throw new InvalidOperationException("CloneEntityNode did not preserve AssetPropertyNode authoring Guid.");
        }
    }

    private sealed class DummyAssetEditorWindow : AssetEditorWindow
    {
        public DummyAssetEditorWindow(string assetPath, ProjectService projectService)
            : base(assetPath, projectService, "Dummy")
        {
        }

        protected override void OnDrawAssetEditor(ImGuiContextManager contextManager)
        {
        }

        public override bool Save() => true;
    }

    private static void TestEditorWindowManager()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"GhostWinMgrTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        try
        {
            using var bakerRegistry = new BakerRegistry();
            var projectService = new ProjectService(bakerRegistry);
            projectService.CreateProject(tempRoot, "WinMgrProj");

            var assetDir = projectService.AssetDirectories[0];
            var objPath = Path.Combine(assetDir, "mesh.obj");
            var mat1Path = Path.Combine(assetDir, "mat1.gmat");
            var mat2Path = Path.Combine(assetDir, "mat2.gmat");
            File.WriteAllText(objPath, "# empty");
            File.WriteAllText(mat1Path, "{}");
            File.WriteAllText(mat2Path, "{}");

            using var mgr = new EditorWindowManager();
            mgr.RegisterAssetEditor(".gmat", path => new DummyAssetEditorWindow(path, projectService));

            if (mgr.OpenAsset(objPath) || mgr.AssetWindows.Count != 0)
            {
                throw new InvalidOperationException("Unregistered extension (.obj) should be a no-op on OpenAsset.");
            }

            if (!mgr.OpenAsset(mat1Path) || !mgr.OpenAsset(mat2Path) || mgr.AssetWindows.Count != 2)
            {
                throw new InvalidOperationException("OpenAsset should open distinct tabs for mat1 and mat2.");
            }

            if (!mgr.OpenAsset(mat1Path) || mgr.AssetWindows.Count != 2)
            {
                throw new InvalidOperationException("Re-opening an already open asset should focus existing tab without duplicating.");
            }
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static void TestMaterialEditorWindow()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"GhostMatEditorTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        try
        {
            using var bakerRegistry = new BakerRegistry();
            var projectService = new ProjectService(bakerRegistry);
            projectService.CreateProject(tempRoot, "MatEditorProj");

            var assetDir = projectService.AssetDirectories[0];
            var gmatPath = Path.Combine(assetDir, "Hero.gmat");
            File.WriteAllText(gmatPath, """
            {
              "Version": 1,
              "Properties": {
                "roughness": 0.25,
                "baseColor": [0.9, 0.2, 0.1]
              }
            }
            """);

            var meta = projectService.GetOrCreateMetadata(gmatPath) ?? throw new InvalidOperationException("Failed to create .gmat metadata.");
            projectService.SaveMetadata(gmatPath + ".meta", meta);

            using var importService = new AssetImportService(projectService);
            var bakedSignal = new ManualResetEventSlim(false);
            IReadOnlyList<Guid>? bakedGuids = null;
            importService.ProjectAssetsBaked += guids =>
            {
                bakedGuids = guids;
                bakedSignal.Set();
            };

            var catalog = new EditorAssetCatalog(projectService);
            using var mgr = new EditorWindowManager();
            mgr.RegisterAssetEditor(".gmat", path => new MaterialEditorWindow(path, projectService, importService, catalog));

            if (!mgr.OpenAsset(gmatPath) || mgr.AssetWindows.Count != 1 || mgr.AssetWindows[0] is not MaterialEditorWindow matWin)
            {
                throw new InvalidOperationException("EditorWindowManager did not open MaterialEditorWindow for .gmat.");
            }

            if (matWin.IsDirty || !matWin.Save())
            {
                throw new InvalidOperationException("MaterialEditorWindow initial dirty/save check failed.");
            }

            if (!bakedSignal.Wait(TimeSpan.FromSeconds(10)) || bakedGuids == null || !bakedGuids.Contains(meta.Id))
            {
                throw new InvalidOperationException("MaterialEditorWindow.Save did not queue live reimport and emit baked asset GUID.");
            }

            var trackedBridge = new EditorSyncBridge();
            var trackedEntity = new EntityNode(10, "Tracked", Array.Empty<ComponentNode>());
            var trackedMeshProp = new AssetPropertyNode("mesh", typeof(Handle<Ghost.Graphics.Core.Mesh>), new ComponentNode("MeshInstance", typeof(MeshInstance), trackedEntity, Array.Empty<PropertyNode>()), null, static _ => { }, trackedBridge);
            var trackedPaletteProp = new MaterialPalettePropertyNode("materialPalette", typeof(Identifier<Ghost.Graphics.Services.MaterialPalette>), new ComponentNode("MeshInstance", typeof(MeshInstance), trackedEntity, Array.Empty<PropertyNode>()), null, static _ => { }, trackedBridge);
            trackedBridge.ReleaseAllTrackedResources();

            var semantics = MaterialPropertyPacker.GetShaderSemantics(Guid.Empty, projectService.AssetDirectories)
                ?? throw new InvalidOperationException("StandardLit semantics failed to resolve.");
            var expanded = MaterialPropertyPacker.CollectAllProperties(semantics);
            if (!expanded.Any(p => p.name == "roughness") || !expanded.Any(p => p.name == "baseColorMap"))
            {
                throw new InvalidOperationException("StandardLit expanded properties missing expected roughness/baseColorMap fields.");
            }
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static unsafe void TestDefaultDockspaceLayout()
    {
        var iniPath = Path.Combine(AppContext.BaseDirectory, "imgui.ini");
        var hadIni = File.Exists(iniPath);
        string? backupIni = hadIni ? File.ReadAllText(iniPath) : null;
        if (hadIni)
        {
            File.Delete(iniPath);
        }

        try
        {
            using var ctx = new ImGuiContextManager();
            var io = ImGui.GetIO();
            io.DisplaySize = new Vector2(1600.0f, 900.0f);
            io.DeltaTime = 1.0f / 60.0f;

            var dockspace = new StudioDockspace();
            using var mgr = new EditorWindowManager();
            using var bakerRegistry = new BakerRegistry();
            var projectService = new ProjectService(bakerRegistry);
            var catalog = new EditorAssetCatalog(projectService);
            var hier = new HierarchyPanel(new EditorViewportInputState());
            var insp = new InspectorPanel(projectService, catalog);
            var proj = new ProjectPanel();
            var cons = new ConsolePanel();
            var prof = new ProfilerPanel();
            mgr.RegisterWindow(hier);
            mgr.RegisterWindow(insp);
            mgr.RegisterWindow(proj);
            mgr.RegisterWindow(cons);
            mgr.RegisterWindow(prof);

            ImGui.NewFrame();
            dockspace.EnsureIniLoaded();
            dockspace.BeginDockSpace();
            mgr.DrawAll(ctx);
            ImGui.Render();

            if (hier.LastDockId == 0 || insp.LastDockId == 0 || proj.LastDockId == 0 || cons.LastDockId == 0 || prof.LastDockId == 0)
            {
                throw new InvalidOperationException($"Default layout failed to dock windows on frame 1: hier={hier.LastDockId}, insp={insp.LastDockId}, proj={proj.LastDockId}, cons={cons.LastDockId}, prof={prof.LastDockId}");
            }

            dockspace.RequestResetLayout();
            ImGui.NewFrame();
            dockspace.BeginDockSpace();
            mgr.DrawAll(ctx);
            ImGui.Render();

            if (hier.LastDockId == 0 || insp.LastDockId == 0 || proj.LastDockId == 0 || cons.LastDockId == 0 || prof.LastDockId == 0)
            {
                throw new InvalidOperationException("Reset Default Layout failed to keep windows docked.");
            }
        }
        finally
        {
            if (File.Exists(iniPath))
            {
                File.Delete(iniPath);
            }

            if (hadIni && backupIni != null)
            {
                File.WriteAllText(iniPath, backupIni);
            }
        }
    }

    private static void TestSceneSessionAndPrefabUnpack()
    {
        AllocationManager.Initialize();
        var tempRoot = Path.Combine(Path.GetTempPath(), $"GhostStudioSceneTest_{Guid.NewGuid():N}");
        try
        {
            using var bakerRegistry = new BakerRegistry();
            var projectService = new ProjectService(bakerRegistry);
            projectService.CreateProject(tempRoot, "SceneProj");

            var scenesDir = Path.Combine(projectService.AssetDirectories[0], "Scenes");
            var catalog = new EditorAssetCatalog(projectService);
            var bridge = new EditorSyncBridge();
            var session = new EditorSceneSession(projectService, catalog, bridge);

            var createdScenePath = session.CreateEmptySceneAsset(scenesDir, "Main");
            if (!File.Exists(createdScenePath) || !File.Exists(createdScenePath + ".meta"))
            {
                throw new InvalidOperationException("CreateEmptySceneAsset did not create both .gscene and .meta.");
            }

            var visualRegistry = AssetVisualRegistry.CreateDefault();
            var tokens = EditorColorTokens.CreateDark();
            var sceneVisual = visualRegistry.Resolve(createdScenePath, SceneAssetDocument.EXTENSION, false, AssetViewMode.List, in tokens);
            if (sceneVisual.IconGlyph != EditorIcons.LAYERS)
            {
                throw new InvalidOperationException("AssetVisualRegistry did not resolve .gscene icon.");
            }

            using (var scheduler = new JobScheduler(new JobSchedulerDesc { ThreadCount = 1, ThreadPriority = ThreadPriority.Normal, DependencyChainCapacity = 64 }))
            using (var world = World.Create(scheduler, 64))
            {
                var doc = new SceneAssetDocument
                {
                    Entities = new List<SceneEntityRecord>
                    {
                        new() { Guid = Guid.NewGuid(), Name = "BootMarker" }
                    }
                };
                SceneSerializer.WriteDocumentToFile(createdScenePath, doc);

                projectService.SetDefaultScene("Scenes/Main.gscene");
                if (!session.TryLoadDefaultSceneOnStartup(world) || bridge.LatestSnapshot?.Entities.Count != 1 || bridge.LatestSnapshot.Entities[0].Name != "BootMarker")
                {
                    throw new InvalidOperationException("TryLoadDefaultSceneOnStartup failed to load configured DefaultScene.");
                }

                // Verify UnpackPrefabCommand + Undo toggles PrefabLink cleanly
                var instanceGuid = Guid.NewGuid();
                var prefabNode = bridge.LatestSnapshot.Entities[0];
                prefabNode.PrefabLink = new PrefabLinkInfo(instanceGuid, Guid.NewGuid(), 0);

                var history = new CommandHistory();
                history.Execute(new UnpackPrefabCommand(instanceGuid, bridge));
                bridge.DrainWorldActions(world);
                if (prefabNode.PrefabLink != null)
                {
                    throw new InvalidOperationException("UnpackPrefabCommand did not clear PrefabLink.");
                }

                history.Undo();
                bridge.DrainWorldActions(world);
                if (prefabNode.PrefabLink == null || prefabNode.PrefabLink.InstanceGuid != instanceGuid)
                {
                    throw new InvalidOperationException("Undo of UnpackPrefabCommand did not restore PrefabLink.");
                }
            }
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }

            AllocationManager.Dispose();
        }
    }
}
