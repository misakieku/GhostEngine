using Ghost.AssetForge.Core.Services;
using Ghost.Core;
using Ghost.Editor.Core.Commands;
using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Serialization;
using Ghost.Editor.Core.Sync;
using Ghost.Engine;
using Ghost.Engine.Input;
using Ghost.Engine.RenderPipeline;
using Ghost.Engine.Streaming;
using Ghost.Entities;
using Ghost.Graphics;
using Ghost.Graphics.D3D12;
using Ghost.Graphics.RHI;
using Ghost.Studio.Assets;
using Ghost.Studio.Backend;
using Ghost.Studio.Platform;
using Ghost.Studio.Rendering;
using Ghost.Studio.Systems;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using SDL;

namespace Ghost.Studio;

internal sealed class StudioLaunchProfile : IEngineLaunchProfile, IInputHandler
{
    private const uint FRAME_BUFFER_COUNT = 2;
    private const int INITIAL_WIDTH = 1600;
    private const int INITIAL_HEIGHT = 900;

    private readonly ProjectService _projectService;

    private readonly GhostRenderPipelineSettings _renderPipelineSettings = new GhostRenderPipelineSettings
    {
        EnableRayTracing = false,
        MaxVisibleMeshletsOnScreen = 524_288,
        MeshletLodErrorThreshold = 1.0f,
        InstanceCullingThreshold = 2.0f,
        DebugMode = RenderPipelineDebugMode.None,
    };

    private ImGuiContextManager? _contextManager;
    private ImGuiSdlInputBridge? _inputBridge;
    private ImGuiPostRenderCommand? _postRenderCommand;
    private SceneViewportTarget? _viewportTarget;
    private EditorViewportInputState? _viewportInputState;
    private EditorContentProvider? _contentProvider;
    private IAssetEntry? _imguiShaderEntry;
    private World? _editorWorld;
    private EditorImGuiSystem? _imguiSystem;
    private EngineWindow? _engineWindow;
    private bool _closePromptActive;

    public StudioLaunchProfile(ProjectService projectService)
    {
        _projectService = projectService;
    }

    public EngineDesc GetEngineDesc()
    {
        var projectName = _projectService.CurrentProject?.Name;
        var title = string.IsNullOrWhiteSpace(projectName) ? "GhostEngine Studio" : $"GhostEngine Studio - {projectName}";

        return new EngineDesc
        {
            AllocationManagerDesc = AllocationManagerDesc.Default,
            WindowDesc = new WindowDesc
            {
                Width = INITIAL_WIDTH,
                Height = INITIAL_HEIGHT,
                Title = title,
                VSync = true,
            },
            JobSchedulerDesc = new JobSchedulerDesc
            {
                ThreadCount = Math.Max(2, Environment.ProcessorCount - 2),
                ThreadPriority = ThreadPriority.Normal,
                DependencyChainCapacity = 16384,
            },
        };
    }

    public GraphicsDesc GetGraphicsDesc()
    {
        IGraphicsEngine graphicsEngine;
#if DEBUG
        var enableDebugLayer = true;
#else
        var enableDebugLayer = false;
#endif

        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041, 0))
        {
            graphicsEngine = D3D12GraphicsEngineFactory.Create(new GraphicsEngineDesc(), enableDebugLayer);
        }
        else
        {
            throw new PlatformNotSupportedException("Direct3D 12 is only supported on Windows 10 version 19041 or later.");
        }

        _contextManager = new ImGuiContextManager();
        var primaryDisplay = SDL3.SDL_GetPrimaryDisplay();
        var systemScale = primaryDisplay != 0 ? SDL3.SDL_GetDisplayContentScale(primaryDisplay) : 1.0f;
        if (systemScale > 0.0f)
        {
            _contextManager.SetDpiScale(systemScale);
        }

        _inputBridge = new ImGuiSdlInputBridge(_contextManager, INITIAL_WIDTH, INITIAL_HEIGHT);
        _postRenderCommand = new ImGuiPostRenderCommand(FRAME_BUFFER_COUNT);
        _viewportTarget = new SceneViewportTarget((int)FRAME_BUFFER_COUNT);
        _viewportInputState = new EditorViewportInputState();

        return new GraphicsDesc
        {
            FrameBufferCount = FRAME_BUFFER_COUNT,
            GraphicsEngine = graphicsEngine,
            RenderPipelineSettings = _renderPipelineSettings,
            PostRenderLoopCommands = new IRenderLoopCommand[] { _postRenderCommand },
        };
    }

    public IContentProvider GetContentProvider()
    {
        _contentProvider = new EditorContentProvider(_projectService, "BuiltInAssets/manifest.json");
        return _contentProvider;
    }

    public void OnEngineInitialized(EngineCore engine)
    {
        engine.InputManager.RelativeMouseMode = false;
        RawInputManager.CursorVisible = true;

        if (_postRenderCommand != null && engine.AssetManager.TryResolveAsset("Shaders/ImGui", out var shaderEntry))
        {
            _imguiShaderEntry = shaderEntry;
            _postRenderCommand.AttachShaderAsset(shaderEntry);
        }
        else
        {
            Logger.Error("Failed to resolve built-in shader 'Shaders/ImGui'.");
        }

        _editorWorld = World.Create(engine.JobScheduler, 1024);
        _editorWorld.AddService(engine.RenderEngine);
        _editorWorld.AddService(engine.AssetManager);
        _editorWorld.AddService(EditorSyncBridge.Instance.DirtyTracker);
        EditorSyncBridge.Instance.World = _editorWorld;
        EditorSyncBridge.Instance.AssetManager = engine.AssetManager;
        EditorSyncBridge.Instance.ResourceManager = engine.RenderEngine.ResourceManager;

        var assetCatalog = new Services.EditorAssetCatalog(_projectService, _contentProvider);
        var sceneSession = new Services.EditorSceneSession(_projectService, assetCatalog);
        if (!sceneSession.TryLoadDefaultSceneOnStartup(_editorWorld))
        {
            EditorSyncBridge.Instance.RegisterScene(0, null);
        }

        _imguiSystem = new EditorImGuiSystem(_contextManager!, _inputBridge!, _postRenderCommand!, _viewportTarget!, _viewportInputState!, _projectService, _contentProvider, assetCatalog, sceneSession,
            requestExit: () => _engineWindow?.RequestClose(),
            updateWindowTitle: UpdateWindowTitle,
            getWindowHandle: () => _engineWindow?.Handle ?? 0);
        _editorWorld.SystemManager.AddSystem(_imguiSystem);
        _editorWorld.SystemManager.AddSystem(new EditorCameraControllerSystem(_viewportInputState!));
        _editorWorld.SystemManager.AddSystem(new EditorRenderSystemGroup(_viewportTarget!));
    }

    public void OnWindowCreated(EngineWindow window)
    {
        _engineWindow = window;
    }

    public bool OnCloseRequested()
    {
        if (_closePromptActive)
        {
            return false;
        }

        _closePromptActive = true;
        try
        {
            return _imguiSystem == null || _imguiSystem.ConfirmCloseRequested();
        }
        finally
        {
            _closePromptActive = false;
        }
    }

    private void UpdateWindowTitle(string? projectName)
    {
        var title = string.IsNullOrWhiteSpace(projectName) ? "GhostEngine Studio" : $"GhostEngine Studio - {projectName}";
        _engineWindow?.SetTitle(title);
    }

    public void ProcessEvent(SDL_Event e)
    {
        _inputBridge?.ProcessEvent(e);
    }

    public void OnEngineShutdown(EngineCore engine)
    {
        if (_editorWorld != null)
        {
            SceneSerializer.UnloadCurrentScene(_editorWorld, EditorSyncBridge.Instance, EditorSelection.Instance, CommandHistory.Instance);
        }

        EditorSyncBridge.Instance.ReleaseAllTrackedResources();
        EditorSyncBridge.Instance.World = null;
        EditorSyncBridge.Instance.AssetManager = null;
        EditorSyncBridge.Instance.ResourceManager = null;
        EditorSelection.Instance.Clear();
        CommandHistory.Instance.Clear();

        _imguiShaderEntry?.Release();
        _imguiShaderEntry = null;

        _viewportTarget?.Dispose();
        _viewportTarget = null;

        _editorWorld?.Dispose();
        _editorWorld = null;
        _imguiSystem = null;
        _engineWindow = null;

        _postRenderCommand?.Dispose();
        _postRenderCommand = null;

        _contextManager?.Dispose();
        _contextManager = null;
    }
}

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        Logger.Impl.OnLogAdded += static log => Console.WriteLine($"[{log.Level}] {log.Message}");

        using var bakerRegistry = new BakerRegistry();
        var projectService = new ProjectService(bakerRegistry);

        var projectArg = ParseProjectArgument(args);
        if (!string.IsNullOrWhiteSpace(projectArg))
        {
            projectService.OpenProject(projectArg);
        }
        else if (OperatingSystem.IsWindows())
        {
            if (!NativeProjectDialogs.PromptStartupProject(projectService))
            {
                return 0;
            }
        }

        EngineRunner.Run(new StudioLaunchProfile(projectService));
        return 0;
    }

    private static string? ParseProjectArgument(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if ((args[i] == "--project" || args[i] == "-p") && i + 1 < args.Length)
            {
                return args[i + 1];
            }

            if (!args[i].StartsWith('-'))
            {
                return args[i];
            }
        }

        return null;
    }
}
