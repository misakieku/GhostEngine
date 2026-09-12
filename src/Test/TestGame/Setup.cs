using Ghost.Core;
using Ghost.Engine;
using Ghost.Engine.Components;
using Ghost.Engine.RenderPipeline;
using Ghost.Engine.Streaming;
using Ghost.Engine.Systems;
using Ghost.Engine.Utilities;
using Ghost.Entities;
using Ghost.Graphics.Core;
using Ghost.Graphics.D3D12;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.Mathematics;

namespace TestGame;

internal static class Setup
{
    private static World s_world = null!;
    private static IAssetEntry s_meshAsset = null!;
    private static IAssetEntry s_shaderAsset = null!;
    private static readonly GhostRenderPipelineSettings s_pipelineSettings = new GhostRenderPipelineSettings
    {
        DebugMode = CullDebugMode.Pass1VsPass2
    };

    [RuntimeConfiguration]
    public static EngineDesc InitEngineDesc()
    {
        return new EngineDesc
        {
            AllocationManagerDesc = AllocationManagerDesc.Default,
            WindowDesc = new WindowDesc { Width = 2560, Height = 1440, Title = "Ghost Engine" },
            JobSchedulerDesc = new JobSchedulerDesc
            {
                ThreadCount = Environment.ProcessorCount - 2,
                ThreadPriority = ThreadPriority.Normal,
                DependencyChainCapacity = 8192,
            },
            RenderDescFactory = static () => new EngineDesc.Render
            {
                FrameBufferCount = 2,
                GraphicsEngine = D3D12GraphicsEngineFactory.Create(new GraphicsEngineDesc { FrameBufferCount = 2 }),
                RenderPipelineSettings = s_pipelineSettings,
                ShaderCacheDirectory = "ShaderCache",
                ShaderCompilationBridge = null
            },
            ContentProviderFactory = static () => new RuntimeContentProvider("Assets/manifest.json")
        };
    }

    private static float RandomFloat(float min, float max)
    {
        return (float)(Random.Shared.NextSingle() * (max - min) + min);
    }

    [RuntimeInitialize]
    public static void Init(EngineCore engineCore)
    {
        EngineWindow.OnEvent += e =>
        {
            if (e.Type == SDL.SDL_EventType.SDL_EVENT_KEY_DOWN)
            {
                if (e.key.scancode == SDL.SDL_Scancode.SDL_SCANCODE_1)
                {
                    s_pipelineSettings.DebugMode = CullDebugMode.None;
                    Console.WriteLine("[Visual Mode] 0: None (Meshlets + Wireframe)");
                }
                else if (e.key.scancode == SDL.SDL_Scancode.SDL_SCANCODE_2)
                {
                    s_pipelineSettings.DebugMode = CullDebugMode.HZBDepth;
                    Console.WriteLine("[Visual Mode] 1: HZB Depth Buffer Heatmap");
                }
                else if (e.key.scancode == SDL.SDL_Scancode.SDL_SCANCODE_3)
                {
                    s_pipelineSettings.DebugMode = CullDebugMode.Pass1VsPass2;
                    Console.WriteLine("[Visual Mode] 2: Pass 1 (Cyan) vs Pass 2 (Gold)");
                }
            }
        };

        s_world = World.Create(engineCore.JobScheduler, 1024);

        using var scope = AllocationManager.CreateStackScope();
        using var camSet = new ComponentSet(scope.AllocationHandle, ComponentTypeID<Camera>.Value, ComponentTypeID<LocalToWorld>.Value);
        var cameraEntity = s_world.EntityManager.CreateEntity(camSet);

        s_world.EntityManager.SetComponent(cameraEntity, new Camera
        {
            swapChainIndex = 0,
            depthTarget = Handle<GPUTexture>.Invalid,
            nearClipPlane = 0.1f,
            farClipPlane = 1000.0f,
            focalLength = 20.0f,
            sensorSize = new float2(36.0f, 24.0f),
            gateFit = GateFit.Vertical,
            renderingLayerMask = RenderingLayerMask.All,
        });

        s_world.EntityManager.SetComponent(cameraEntity, new LocalToWorld
        {
            matrix = float4x4.TRS(new float3(0.0f, 1.0f, -5.0f), quaternion.identity, new float3(1.0f, 1.0f, 1.0f))
        });

        s_meshAsset = engineCore.AssetManager.ResolveAsset("Meshes/bunny");
        s_shaderAsset = engineCore.AssetManager.ResolveAsset("Shaders/test");

        var meshHandle = default(Handle<Mesh>);
        s_meshAsset.ReadAssetData(ref meshHandle);

        var shaderHandle = default(Handle<Shader>);
        s_shaderAsset.ReadAssetData(ref shaderHandle);

        // TODO: Create material from shader
        var mat = engineCore.RenderEngine.ResourceManager.CreateMaterial(shaderHandle);
        var materialPallette = engineCore.RenderEngine.ResourceManager.GetOrCreateMaterialPalette([mat]);

        using var meshSet = new ComponentSet(scope.AllocationHandle, ComponentTypeID<MeshInstance>.Value, ComponentTypeID<LocalToWorld>.Value);
        var entities = new Entity[3];
        s_world.EntityManager.CreateEntities(entities, meshSet);

        var positions = new float3[]
        {
            new float3(0.0f, 0.0f, -3.0f),   // Bunny 0: Front (occluder, distance 2m)
            new float3(0.0f, 0.0f, -1.5f),   // Bunny 1: Behind Bunny 0 (occluded, distance 3.5m)
            new float3(0.5f, 0.0f, -2.5f)    // Bunny 2: Side (unoccluded)
        };

        for (var i = 0; i < entities.Length; i++)
        {
            var entity = entities[i];
            s_world.EntityManager.SetComponent(entity, new MeshInstance
            {
                mesh = meshHandle,
                materialPalette = materialPallette,
                renderingLayerMask = RenderingLayerMask.All,
                shadowCastingMode = ShadowCastingMode.On,
                staticShadowCaster = true,
            });

            s_world.EntityManager.SetComponent(entity, new LocalToWorld
            {
                matrix = float4x4.TRS(positions[i], quaternion.identity, new float3(1.0f, 1.0f, 1.0f))
            });
        }

        s_world.SystemManager.AddSystem<RenderSystemGroup>();

        s_world.AddService(engineCore.RenderEngine);
    }

    [RuntimeShutdown]
    public static void Shutdown(EngineCore engineCore)
    {
        World.Destroy(s_world);

        s_meshAsset.Release();
        s_shaderAsset.Release();
    }
}
