using Ghost.Core;
using Ghost.Engine;
using Ghost.Engine.Components;
using Ghost.Engine.Input;
using Ghost.Engine.RenderPipeline;
using Ghost.Engine.Streaming;
using Ghost.Engine.Systems;
using Ghost.Engine.Utilities;
using Ghost.Engine.Volumes;
using Ghost.Entities;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.D3D12;
using Ghost.Graphics.RHI;
using Ghost.Studio.Backend;
using Ghost.Studio.Rendering;
using Ghost.Studio.Systems;
using Ghost.Studio.Tests;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.Mathematics;
using SDL;

namespace Ghost.Studio;

internal sealed class StudioLaunchProfile : IEngineLaunchProfile, IInputHandler
{
    private const uint FRAME_BUFFER_COUNT = 2;
    private const int INITIAL_WIDTH = 1600;
    private const int INITIAL_HEIGHT = 900;

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
    private IAssetEntry? _imguiShaderEntry;
    private IAssetEntry? _cubePrefabEntry;
    private World? _editorWorld;

    public EngineDesc GetEngineDesc()
    {
        return new EngineDesc
        {
            AllocationManagerDesc = AllocationManagerDesc.Default,
            WindowDesc = new WindowDesc
            {
                Width = INITIAL_WIDTH,
                Height = INITIAL_HEIGHT,
                Title = "GhostEngine Studio",
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
        _viewportTarget = new SceneViewportTarget();
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
        return new RuntimeContentProvider("BuiltInAssets/manifest.json");
    }

    public void OnEngineInitialized(EngineCore engine)
    {
        engine.InputManager.RelativeMouseMode = false;
        engine.InputManager.CursorVisible = true;

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

        SeedStarterEditorScene(engine, _editorWorld);

        _editorWorld.SystemManager.AddSystem(new EditorEcsSyncSystem());
        _editorWorld.SystemManager.AddSystem(new EditorImGuiSystem(_contextManager!, _inputBridge!, _postRenderCommand!, _viewportTarget!, _viewportInputState!));
        _editorWorld.SystemManager.AddSystem(new EditorCameraControllerSystem(_viewportInputState!));

        var renderGroup = new RenderSystemGroup();
        renderGroup.AddSystem(new EditorCameraRenderSystem(_viewportTarget!));
        renderGroup.SortSystems();
        _editorWorld.SystemManager.AddSystem(renderGroup);
    }

    private void SeedStarterEditorScene(EngineCore engine, World world)
    {
        if (engine.AssetManager.TryResolveAsset("Meshes/cube", out var cubeEntry))
        {
            _cubePrefabEntry = cubeEntry;
            PrefabUtility.Instantiate(world, cubeEntry, new float3(0.0f, 0.8f, 0.0f), quaternion.identity, new float3(0.8f));
            PrefabUtility.Instantiate(world, cubeEntry, new float3(0.0f, -0.15f, 0.0f), quaternion.identity, new float3(6.0f, 0.15f, 6.0f));
        }
        else
        {
            Logger.Warning("Starter mesh 'Meshes/cube' was not found in BuiltInAssets.");
        }

        world.EntityManager.CreateEntity(
            new DirectionalLight
            {
                color = new float3(1.0f, 0.96f, 0.90f),
                sunDiskAngularDiameter = math.radians(0.53f),
                intensity = 12_000.0f,
                castShadows = true,
                shadowResolution = 2048,
                lightingDimmer = 1.0f,
                shadowDimmer = 1.0f,
                diffuseDimmer = 1.0f,
                specularDimmer = 1.0f,
                normalBias = 0.75f,
                depthBias = 0.5f,
                shadowLodErrorScale = 3.0f,
                contactShadow = false,
            },
            new LocalToWorld
            {
                matrix = MathUtility.TRS(new float3(0.0f, 20.0f, 0.0f), quaternion.EulerXYZ(math.radians(new float3(48.0f, 215.0f, 0.0f))), float3.one),
            });

        world.EntityManager.CreateEntity(
            new PunctualLight
            {
                type = PunctualLightType.Point,
                unit = PunctualLightUnit.Lumen,
                color = new float3(0.45f, 0.70f, 1.0f),
                shadowResolution = 0,
                intensity = 3000.0f,
                range = 20.0f,
                innerSpotAngle = math.radians(15.0f),
                outerSpotAngle = math.radians(40.0f),
                nearPlane = 0.1f,
                normalBias = 0.75f,
                depthBias = 0.5f,
                shadowLodErrorScale = 3.0f,
                fadeDistance = 1000.0f,
                shadowFadeDistance = 1000.0f,
                sourceRadius = 0.15f,
                lightingDimmer = 1.0f,
                diffuseDimmer = 1.0f,
                specularDimmer = 1.0f,
                shadowDimmer = 1.0f,
            },
            new LocalToWorld
            {
                matrix = MathUtility.TRS(new float3(-3.5f, 3.0f, -3.5f), quaternion.identity, float3.one),
            });

        var exposure = ExposureSettings.Default;
        exposure.mode = ExposureMode.Fixed;
        exposure.fixedExposure = 13.5f;

        var volumeData = new RenderVolumeDataContainer(32);
        volumeData.Set(new EnvironmentSettings
        {
            skyType = SkyType.None,
        });
        volumeData.Set(exposure);
        volumeData.Set(new ShadowSettings
        {
            shadowMode = ShadowMode.ShadowMap,
            maxDistance = 50.0f,
        });

        world.EntityManager.CreateEntity(new RenderVolume { renderingLayerMask = RenderingLayerMask.All, isGlobal = true, weight = 1.0f, priority = 1 }, volumeData);
    }

    public void ProcessEvent(SDL_Event e)
    {
        _inputBridge?.ProcessEvent(e);
    }

    public void OnEngineShutdown(EngineCore engine)
    {
        _cubePrefabEntry?.Release();
        _cubePrefabEntry = null;

        _imguiShaderEntry?.Release();
        _imguiShaderEntry = null;

        _viewportTarget?.Dispose();
        _viewportTarget = null;

        _editorWorld?.Dispose();
        _editorWorld = null;

        _postRenderCommand?.Dispose();
        _postRenderCommand = null;

        _contextManager?.Dispose();
        _contextManager = null;
    }
}

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--headless-test")
        {
            return StudioHeadlessRunner.Run();
        }

        Logger.Impl.OnLogAdded += static log => Console.WriteLine($"[{log.Level}] {log.Message}");
        EngineRunner.Run(new StudioLaunchProfile());
        return 0;
    }
}
