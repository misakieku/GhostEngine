using Ghost.AssetForge.Core.Bakers;
using Ghost.AssetForge.Core.Models;
using Ghost.AssetForge.Core.Services;
using Ghost.Core;
using Ghost.Editor.Core.Commands;
using Ghost.Editor.Core.Serialization;
using Ghost.Editor.Core.Sync;
using Ghost.Engine.Components;
using Ghost.Entities;
using Ghost.Studio.Assets;
using Ghost.Studio.Backend;
using Ghost.Studio.Rendering;
using Ghost.Studio.Services;
using Ghost.Studio.UI;
using Hexa.NET.ImGui;
using Misaki.HighPerformance.Jobs;
using System.Numerics;

namespace Ghost.Editor.Test;

[TestClass]
[DoNotParallelize]
public class StudioIntegrationTests
{
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

    [TestMethod]
    public void EditorContentProviderAndCatalog_ResolveProjectAndBuiltInAssets()
    {
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

            var meta = projectService.GetOrCreateMetadata(gmatPath);
            Assert.IsNotNull(meta);
            projectService.SaveMetadata(gmatPath + ".meta", meta);

            var bakeService = new BakeService(projectService.GetContext(), bakerRegistry);
            var bakeResult = bakeService.BakeProjectAsync(new BakeConfig { ForceRebake = true }).GetAwaiter().GetResult();
            Assert.AreEqual(0, bakeResult.Failed);
            Assert.IsGreaterThan(0, bakeResult.Succeeded);

            var builtInManifest = Path.Combine(AppContext.BaseDirectory, "BuiltInAssets", "manifest.json");
            using var provider = new EditorContentProvider(projectService, builtInManifest);
            Assert.IsTrue(provider.HasAsset(meta.Id));
            Assert.AreEqual(AssetType.Material, provider.GetAssetType(meta.Id));
            Assert.AreEqual(meta.Id, provider.VirtualPathToGuid("Materials/Brick"));
            Assert.AreEqual("Materials/Brick", provider.VirtualPathFromGuid(meta.Id));

            var openRes = provider.OpenReadAsync(meta.Id);
            Assert.IsTrue(openRes.IsSuccess, openRes.Message);
            Assert.IsTrue(openRes.Value.IsValid);
            using (var payload = openRes.Value)
            {
                var header = payload.Read<MaterialContentHeader>(0);
                Assert.AreEqual(MaterialContentHeader.MAGIC, header.magic);
                Assert.AreEqual(MaterialContentHeader.VERSION, header.version);
            }

            var cubeMatGuid = provider.VirtualPathToGuid("Meshes/cube#Materials/Material_0");
            var cubeMatOpen = provider.OpenReadAsync(cubeMatGuid);
            Assert.IsTrue(cubeMatOpen.IsSuccess, cubeMatOpen.Message);
            Assert.IsTrue(cubeMatOpen.Value.IsValid);
            using (var cubeMatPayload = cubeMatOpen.Value)
            {
                var cubeHeader = cubeMatPayload.Read<MaterialContentHeader>(0);
                Assert.AreEqual(MaterialContentHeader.MAGIC, cubeHeader.magic);
                Assert.AreEqual(MaterialContentHeader.VERSION, cubeHeader.version);
            }

            var catalog = new EditorAssetCatalog(projectService, provider);
            Assert.IsTrue(catalog.TryGetByGuid(meta.Id, out var catEntry));
            Assert.AreEqual(AssetType.Material, catEntry.AssetType);
            Assert.IsFalse(catEntry.IsBuiltIn);

            var shaders = new List<EditorAssetCatalog.Entry>();
            catalog.GetAssets(AssetType.Shader, shaders);
            Assert.IsNotEmpty(shaders);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [TestMethod]
    public void EditorWindowManager_RoutesSupportedExtensionsAndFocusesExistingTab()
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

            Assert.IsFalse(mgr.OpenAsset(objPath));
            Assert.IsEmpty(mgr.AssetWindows);

            Assert.IsTrue(mgr.OpenAsset(mat1Path));
            Assert.IsTrue(mgr.OpenAsset(mat2Path));
            Assert.HasCount(2, mgr.AssetWindows);

            Assert.IsTrue(mgr.OpenAsset(mat1Path));
            Assert.HasCount(2, mgr.AssetWindows);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [TestMethod]
    public void MaterialEditorWindow_LoadsSavesRebakesAndResolvesSemantics()
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

            var meta = projectService.GetOrCreateMetadata(gmatPath);
            Assert.IsNotNull(meta);
            projectService.SaveMetadata(gmatPath + ".meta", meta);

            using var importService = new AssetImportService(projectService);
            using var bakedSignal = new ManualResetEventSlim(false);
            IReadOnlyList<Guid>? bakedGuids = null;
            importService.ProjectAssetsBaked += guids =>
            {
                bakedGuids = guids;
                bakedSignal.Set();
            };

            var catalog = new EditorAssetCatalog(projectService);
            using var mgr = new EditorWindowManager();
            mgr.RegisterAssetEditor(".gmat", path => new MaterialEditorWindow(path, projectService, importService, catalog));

            Assert.IsTrue(mgr.OpenAsset(gmatPath));
            Assert.HasCount(1, mgr.AssetWindows);
            Assert.IsInstanceOfType<MaterialEditorWindow>(mgr.AssetWindows[0]);
            var matWin = (MaterialEditorWindow)mgr.AssetWindows[0];

            Assert.IsFalse(matWin.IsDirty);
            Assert.IsTrue(matWin.Save());

            Assert.IsTrue(bakedSignal.Wait(TimeSpan.FromSeconds(10)));
            Assert.IsNotNull(bakedGuids);
            Assert.Contains(meta.Id, bakedGuids);

            var trackedBridge = new EditorSyncBridge();
            var trackedEntity = new EntityNode(10, "Tracked", Array.Empty<ComponentNode>());
            var trackedComp = new ComponentNode("MeshInstance", typeof(MeshInstance), trackedEntity, Array.Empty<PropertyNode>());
            var trackedMeshProp = new AssetPropertyNode("mesh", typeof(Handle<Ghost.Graphics.Core.Mesh>), trackedComp, null, static _ => { }, trackedBridge);
            var trackedPaletteProp = new MaterialPalettePropertyNode("materialPalette", typeof(Identifier<Ghost.Graphics.Services.MaterialPalette>), trackedComp, null, static _ => { }, trackedBridge);
            Assert.IsNotNull(trackedMeshProp);
            Assert.IsNotNull(trackedPaletteProp);
            trackedBridge.ReleaseAllTrackedResources();

            var semantics = MaterialPropertyPacker.GetShaderSemantics(Guid.Empty, projectService.AssetDirectories);
            Assert.IsNotNull(semantics);
            var expanded = MaterialPropertyPacker.CollectAllProperties(semantics);
            Assert.IsTrue(expanded.Any(p => p.name == "roughness"));
            Assert.IsTrue(expanded.Any(p => p.name == "baseColorMap"));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [TestMethod]
    public unsafe void StudioDockspace_DefaultLayoutAndReset_DockAllBuiltInPanels()
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

            Assert.AreNotEqual(0u, hier.LastDockId);
            Assert.AreNotEqual(0u, insp.LastDockId);
            Assert.AreNotEqual(0u, proj.LastDockId);
            Assert.AreNotEqual(0u, cons.LastDockId);
            Assert.AreNotEqual(0u, prof.LastDockId);

            dockspace.RequestResetLayout();
            ImGui.NewFrame();
            dockspace.BeginDockSpace();
            mgr.DrawAll(ctx);
            ImGui.Render();

            Assert.AreNotEqual(0u, hier.LastDockId);
            Assert.AreNotEqual(0u, insp.LastDockId);
            Assert.AreNotEqual(0u, proj.LastDockId);
            Assert.AreNotEqual(0u, cons.LastDockId);
            Assert.AreNotEqual(0u, prof.LastDockId);
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

    [TestMethod]
    public void EditorSceneSession_LoadsDefaultSceneAndUnpackPrefabSupportsUndo()
    {
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
            Assert.IsTrue(File.Exists(createdScenePath));
            Assert.IsTrue(File.Exists(createdScenePath + ".meta"));

            var visualRegistry = AssetVisualRegistry.CreateDefault();
            var tokens = EditorColorTokens.CreateDark();
            var sceneVisual = visualRegistry.Resolve(createdScenePath, SceneAssetDocument.EXTENSION, false, AssetViewMode.List, in tokens);
            Assert.AreEqual(EditorIcons.LAYERS, sceneVisual.IconGlyph);

            using var scheduler = new JobScheduler(new JobSchedulerDesc { ThreadCount = 1, ThreadPriority = ThreadPriority.Normal, DependencyChainCapacity = 64 });
            using var world = World.Create(scheduler, 64);

            var doc = new SceneAssetDocument
            {
                Entities = new List<SceneEntityRecord>
                {
                    new() { Guid = Guid.NewGuid(), Name = "BootMarker" }
                }
            };
            SceneSerializer.WriteDocumentToFile(createdScenePath, doc);

            projectService.SetDefaultScene("Scenes/Main.gscene");
            Assert.IsTrue(session.TryLoadDefaultSceneOnStartup(world));
            Assert.IsNotNull(bridge.LatestSnapshot);
            Assert.HasCount(1, bridge.LatestSnapshot.Entities);
            Assert.AreEqual("BootMarker", bridge.LatestSnapshot.Entities[0].Name);

            var instanceGuid = Guid.NewGuid();
            var prefabNode = bridge.LatestSnapshot.Entities[0];
            prefabNode.PrefabLink = new PrefabLinkInfo(instanceGuid, Guid.NewGuid(), 0);

            var history = new CommandHistory();
            history.Execute(new UnpackPrefabCommand(instanceGuid, bridge));
            bridge.DrainWorldActions(world);
            Assert.IsNull(prefabNode.PrefabLink);

            history.Undo();
            bridge.DrainWorldActions(world);
            Assert.IsNotNull(prefabNode.PrefabLink);
            Assert.AreEqual(instanceGuid, prefabNode.PrefabLink.InstanceGuid);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [TestMethod]
    public void EditorWindowManager_ProjectSwitchClosesAssetTabsAndReloadsDefaultScene()
    {
        var tempRootA = Path.Combine(Path.GetTempPath(), $"GhostStudioSwitchA_{Guid.NewGuid():N}");
        var tempRootB = Path.Combine(Path.GetTempPath(), $"GhostStudioSwitchB_{Guid.NewGuid():N}");
        try
        {
            using var bakerRegistry = new BakerRegistry();
            var projectService = new ProjectService(bakerRegistry);
            projectService.CreateProject(tempRootA, "ProjA");

            var matPath = Path.Combine(projectService.AssetDirectories[0], "MatA.gmat");
            File.WriteAllText(matPath, "{}");

            using var mgr = new EditorWindowManager();
            mgr.RegisterAssetEditor(".gmat", path => new DummyAssetEditorWindow(path, projectService));
            Assert.IsTrue(mgr.OpenAsset(matPath));
            Assert.HasCount(1, mgr.AssetWindows);

            var catalog = new EditorAssetCatalog(projectService);
            var bridge = new EditorSyncBridge();
            var session = new EditorSceneSession(projectService, catalog, bridge);

            using var scheduler = new JobScheduler(new JobSchedulerDesc { ThreadCount = 1, ThreadPriority = ThreadPriority.Normal, DependencyChainCapacity = 64 });
            using var world = World.Create(scheduler, 64);

            // Switch to Project B with its own DefaultScene
            projectService.CreateProject(tempRootB, "ProjB");
            var sceneBPath = Path.Combine(projectService.AssetDirectories[0], "Scenes", "StartB.gscene");
            Directory.CreateDirectory(Path.GetDirectoryName(sceneBPath)!);
            var docB = new SceneAssetDocument
            {
                Entities = new List<SceneEntityRecord>
                {
                    new() { Guid = Guid.NewGuid(), Name = "StartBEntity" }
                }
            };
            SceneSerializer.WriteDocumentToFile(sceneBPath, docB);
            projectService.SetDefaultScene("Scenes/StartB.gscene");

            mgr.CloseAllAssetWindows();
            Assert.IsEmpty(mgr.AssetWindows);

            session.HandleProjectSwitched(world);
            Assert.IsNotNull(bridge.LatestSnapshot);
            Assert.HasCount(1, bridge.LatestSnapshot.Entities);
            Assert.AreEqual("StartBEntity", bridge.LatestSnapshot.Entities[0].Name);
            Assert.IsFalse(bridge.AnySceneDirty);
        }
        finally
        {
            if (Directory.Exists(tempRootA))
            {
                Directory.Delete(tempRootA, recursive: true);
            }

            if (Directory.Exists(tempRootB))
            {
                Directory.Delete(tempRootB, recursive: true);
            }
        }
    }
}
