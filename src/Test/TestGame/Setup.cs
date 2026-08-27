using Ghost.Core;
using Ghost.Engine;
using Ghost.Engine.Components;
using Ghost.Engine.RenderPipeline;
using Ghost.Engine.Streaming;
using Ghost.Engine.Systems;
using Ghost.Engine.Utilities;
using Ghost.Entities;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.D3D12;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.Mathematics;

namespace TestGame;

internal static class Setup
{
    private static World _world = null!;

    [RuntimeConfiguration]
    public static EngineDesc InitEngineDesc()
    {
        return new EngineDesc
        {
            AllocationManagerDesc = AllocationManagerDesc.Default,
            WindowDesc = new WindowDesc { Width = 800, Height = 600, Title = "Ghost Engine" },
            JobSchedulerDesc = new JobSchedulerDesc
            {
                ThreadCount = Environment.ProcessorCount - 2,
                ThreadPriority = ThreadPriority.Normal,
                DependencyChainCapacity = 8192,
            },
            RenderDescFactory = () => new EngineDesc.Render
            {
                FrameBufferCount = 2,
                GraphicsEngine = D3D12GraphicsEngineFactory.Create(new GraphicsEngineDesc { FrameBufferCount = 2 }),
                RenderPipelineSettings = new GhostRenderPipelineSettings(),
                ShaderCacheDirectory = "ShaderCache",
                ShaderCompilationBridge = null
            },
            ContentProviderFactory = () => new RuntimeContentProvider("Assets/manifest.json")
        };
    }

    [RuntimeInitialize]
    public static void Init(EngineCore engineCore)
    {
        _world = World.Create(engineCore.JobScheduler, 1024);

        using var scope = AllocationManager.CreateStackScope();
        using var camSet = new ComponentSet(scope.AllocationHandle, ComponentTypeID<Camera>.Value, ComponentTypeID<LocalToWorld>.Value);
        var cameraEntity = _world.EntityManager.CreateEntity(camSet);

        _world.EntityManager.SetComponent(cameraEntity, new Camera
        {
            swapChainIndex = 0,
            depthTarget = Handle<GPUTexture>.Invalid,
            nearClipPlane = 0.1f,
            farClipPlane = 1000.0f,
            focalLength = 50.0f,
            sensorSize = new float2(36.0f, 24.0f),
            gateFit = GateFit.Vertical,
            renderingLayerMask = RenderingLayerMask.All,
        });

        _world.EntityManager.SetComponent(cameraEntity, new LocalToWorld
        {
            matrix = float4x4.TRS(new float3(0.0f, 0.0f, -5.0f), quaternion.identity, new float3(1.0f, 1.0f, 1.0f))
        });

        _world.SystemManager.AddSystem<RenderSystemGroup>();

        _world.AddService(engineCore.RenderEngine);
    }

    [RuntimeShutdown]
    public static void Shutdown(EngineCore engineCore)
    {
        World.Destroy(_world);
    }
}
